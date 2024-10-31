use lazy_static::lazy_static;
use libc::size_t;
use netcorehost::{pdcstr, hostfxr::Hostfxr};
use unity_rs::{il2cpp::types::Il2CppThread, runtime::Runtime};
use std::{
    ffi::{c_char, c_void},
    ptr::{addr_of, addr_of_mut, null_mut},
    sync::RwLock,
};

use crate::{
    core_android, debug, errors::{dotneterr::DotnetErr, DynErr}, icalls, logging::logger, melonenv, utils::{self, strings::wide_str}
};

/// These are functions that MelonLoader.NativeHost.dll will fill in, once we call LoadStage1.
/// Interacting with the .net runtime is a pain, so it's a lot easier to just have it give us pointers like this directly.
#[repr(C)]
#[derive(Debug)]
pub struct HostImports {
    pub load_assembly_get_ptr: fn(isize, isize, isize, *mut *mut c_void),

    pub initialize: fn(),
    pub pre_start: fn(),
    pub start: fn(),
}

/// These are functions that we will pass to MelonLoader.NativeHost.dll.
/// CoreCLR does not have internal calls like mono does, so we have to pass these ourselves.
/// They are stored in Managed, and are accessed by MelonLoader for hooking.
#[repr(C)]
#[derive(Debug)]
pub struct HostExports {
    pub hook_attach: unsafe fn(*mut *mut c_void, *mut c_void),
    pub hook_detach: unsafe fn(*mut *mut c_void, *mut c_void),
    pub log_console: unsafe fn(*const c_char),
    pub get_java_vm: unsafe fn() -> *mut *const c_void,
    pub get_package_name: unsafe fn() -> *const c_char,
}

// Initializing the host imports as a static variable. Later on this is replaced with a filled in version of the struct.
lazy_static! {
    pub static ref IMPORTS: RwLock<HostImports> = RwLock::new(HostImports {
        load_assembly_get_ptr: |_, _, _, _| {},
        initialize: || {},
        pre_start: || {},
        start: || {},
    });
}

pub fn init() -> Result<(), DynErr> {
    let runtime_dir = melonenv::paths::runtime_dir()?;

    let hostfxr_path = melonenv::paths::get_dotnet_path()?.join("host/fxr/8.0.6/libhostfxr.so");
    let hostfxr = Hostfxr::load_from_path(hostfxr_path).map_err(|_| DotnetErr::FailedHostFXRLoad)?;

    let config_path = runtime_dir.join("MelonLoader.runtimeconfig.json");
    if !config_path.exists() {
        return Err(DotnetErr::RuntimeConfig.into());
    }

    let dotnet_path = melonenv::paths::get_dotnet_path()?;

    let context = hostfxr.initialize_for_runtime_config_with_dotnet_root(
        utils::strings::pdcstr(config_path)?,
        utils::strings::pdcstr(dotnet_path.to_path_buf())?)?;

    let loader = context.get_delegate_loader_for_assembly(utils::strings::pdcstr(
        runtime_dir.join("MelonLoader.NativeHost.dll"),
    )?)?;

    let init = loader.get_function_with_unmanaged_callers_only::<fn(*mut HostImports)>(
        pdcstr!("MelonLoader.NativeHost.NativeEntryPoint, MelonLoader.NativeHost"),
        pdcstr!("LoadStage1"),
    )?;

    let mut imports = HostImports {
        load_assembly_get_ptr: |_, _, _, _| {},
        initialize: || {},
        pre_start: || {},
        start: || {},
    };

    let mut exports = HostExports {
        hook_attach: icalls::bootstrap_interop::attach,
        hook_detach: icalls::bootstrap_interop::detach,
        log_console: logger::log_console_interop,
        get_java_vm: core_android::get_raw_java_vm,
        get_package_name: crate::melonenv::paths::get_package_name_raw,
    };

    apply_mono_patches()?;

    debug!("[Dotnet] Invoking LoadStage1")?;
    //MelonLoader.NativeHost will fill in the HostImports struct with pointers to functions
    init(addr_of_mut!(imports));

    debug!("[Dotnet] Reloading NativeHost into correct load context and getting LoadStage2 pointer")?;

    //a function pointer to be filled
    let mut init_stage_two = null_mut::<c_void>();

    //have to make all strings utf16 for C# to understand, of course they can only be passed as IntPtrs
    (imports.load_assembly_get_ptr)(
        wide_str(runtime_dir.join("MelonLoader.NativeHost.dll"))?.as_ptr() as isize,
        wide_str("MelonLoader.NativeHost.NativeEntryPoint, MelonLoader.NativeHost")?.as_ptr()
            as isize,
        wide_str("LoadStage2")?.as_ptr() as isize,
        addr_of_mut!(init_stage_two),
    );

    debug!("[Dotnet] Invoking LoadStage2")?;

    //turn the function pointer into a function we can invoke
    let init_stage_two: fn(*mut HostImports, *mut HostExports) =
        unsafe { std::mem::transmute(init_stage_two) };
    init_stage_two(addr_of_mut!(imports), addr_of_mut!(exports));

    if addr_of!(imports.initialize).is_null() {
        Err("Failed to get HostImports::Initialize!")?
    }

    (imports.initialize)();

    *IMPORTS.try_write()? = imports;

    Ok(())
}

pub fn pre_start() -> Result<(), DynErr> {
    let imports = IMPORTS.try_read()?;

    (imports.pre_start)();

    Ok(())
}

pub fn start() -> Result<(), DynErr> {
    let imports = IMPORTS.try_read()?;

    (imports.start)();

    Ok(())
}

fn apply_mono_patches() -> Result<(), DynErr> {
    debug!("[Dotnet] Applying Mono runtime patches")?;

    let lib = crate::mono_lib!()?;
    
    let set_level_string = unsafe { std::mem::transmute::<*mut c_void, fn(*const c_char)>(lib.get_export_ptr("mono_trace_set_level_string")?) };
    let warning = std::ffi::CString::new("warning").unwrap();
    set_level_string(warning.as_ptr());
    let set_mask_string = unsafe { std::mem::transmute::<*mut c_void, fn(*const c_char)>(lib.get_export_ptr("mono_trace_set_mask_string")?) };
    let all = std::ffi::CString::new("all").unwrap();
    set_mask_string(all.as_ptr());

    debug!("[Dotnet] Enabled Mono logging")?;

    type MonoUnhandledExceptionFunc = Option<unsafe extern "C" fn(exc: *mut unity_rs::mono::types::MonoObject, user_data: *mut c_void)>;
    let install_unhandled_exception_hook = unsafe { std::mem::transmute::<*mut c_void, fn(MonoUnhandledExceptionFunc, *mut c_void)>(lib.get_export_ptr("mono_install_unhandled_exception_hook")?) };
    install_unhandled_exception_hook(Some(mono_unhandled_exception), null_mut());

    debug!("[Dotnet] Installed unhandled exception hook")?;

    let thread_suspend_reload = lib.exports.mono_melonloader_set_thread_checker.as_ref().unwrap();
    thread_suspend_reload(mono_check_thread);

    debug!("[Dotnet] Installed thread checker")?;

    Ok(())
}

unsafe extern "C" fn mono_unhandled_exception(exc: *mut unity_rs::mono::types::MonoObject, user_data: *mut c_void) {
    let _ = user_data;
    if (exc as usize) == 0 {
        return;
    }

    let lib = crate::mono_lib!().unwrap();
    let print_unhandled_exception = std::mem::transmute::<*mut c_void, fn(*mut unity_rs::mono::types::MonoObject)>(lib.get_export_ptr("mono_print_unhandled_exception").unwrap());
    print_unhandled_exception(exc);
}

fn mono_check_thread(tid: u64) -> bool {
    debug!("[Dotnet] Checking thread {:#x}", tid).unwrap();

    let runtime = crate::runtime!().unwrap();

    
    let mut size: usize = 0;
    let get_all_attached_threads = unsafe { std::mem::transmute::<*mut c_void, fn(size: *mut size_t) -> *const *const Il2CppThread>(runtime.get_export_ptr("il2cpp_thread_get_all_attached_threads").unwrap()) };
    let threads = get_all_attached_threads(addr_of_mut!(size));
    let threads_slice = unsafe { std::slice::from_raw_parts(threads, size) };

    for i in 0..size {
        let thread_id = unsafe { *(*threads_slice[i]).internal_thread }.tid;
        debug!("[Dotnet] Attached IL2CPP thread {:#x}", thread_id).unwrap();
        if thread_id == tid {
            return false;
        }
    }

    true
}