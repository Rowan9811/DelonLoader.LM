use unity_rs::{common::method::MethodPointer, runtime::FerrexRuntime};

use crate::{core_android, debug, errors::DynErr, logging::logger, melonenv::paths};

mod melon_utils;
pub mod bootstrap_interop;
mod mono_library;
mod resolve_internals;
mod preload;

pub fn init(runtime: &FerrexRuntime) -> Result<(), DynErr> {
    debug!("Initializing internal calls")?;

    runtime.add_internal_call("MelonLoader.MelonUtils::IsGame32Bit", melon_utils::is_32_bit as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.BootstrapInterop::NativeHookAttach", bootstrap_interop::attach as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.BootstrapInterop::NativeHookDetach", bootstrap_interop::detach as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.BootstrapInterop::NativeLogConsole", logger::log_console_interop as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.BootstrapInterop::NativeGetJavaVM", core_android::get_raw_java_vm as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.BootstrapInterop::NativeGetPackageName", paths::get_package_name_raw as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.MonoInternals.MonoLibrary::GetLibPtr", mono_library::get_lib_ptr as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.MonoInternals.MonoLibrary::CastManagedAssemblyPtr", mono_library::cast_assembly_ptr as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.MonoInternals.MonoLibrary::GetRootDomainPtr", mono_library::get_domain_ptr as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.MonoInternals.ResolveInternals.AssemblyManager::InstallHooks", resolve_internals::install_hooks as MethodPointer)?;
    runtime.add_internal_call("MelonLoader.Support.Preload::GetManagedDirectory", preload::get_managed_dir as MethodPointer)?;

    Ok(())
}