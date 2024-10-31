mod il2cpp;

use crate::{
    debug,
    errors::DynErr,
    runtime,
    internal_failure,
};
use std::ffi::c_void;
use unity_rs::runtime::RuntimeType;

use super::NativeHook;

pub fn hook() -> Result<(), DynErr> {
    let runtime = runtime!()?;

    match runtime.get_type() {
        RuntimeType::Mono(_) => internal_failure!("Mono is unsupported."),

        RuntimeType::Il2Cpp(_) => {
            debug!("Attaching hook to il2cpp_init")?;

            let init_function = runtime.get_export_ptr("il2cpp_init")?;
            let detour = il2cpp::detour as usize;

            let mut init_hook = il2cpp::INIT_HOOK.try_write()?;
            *init_hook = NativeHook::new(init_function, detour as *mut c_void);

            init_hook.hook()?;
        }
    };

    Ok(())
}
