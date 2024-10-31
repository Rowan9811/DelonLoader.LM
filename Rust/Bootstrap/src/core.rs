use crate::{console, errors::DynErr, hooks, internal_failure};

#[no_mangle]
fn startup() {
    init().unwrap_or_else(|e| {
        internal_failure!("Failed to initialize MelonLoader: {}", e.to_string());
    })
}

fn init() -> Result<(), DynErr> {
    console::init()?;

    hooks::init_hook::hook()?;

    console::null_handles()?;

    Ok(())
}

pub fn shutdown() {
    std::process::exit(0);
}
