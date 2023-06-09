use jni::{
    objects::{JClass, JString},
    sys::{jboolean, JavaVM},
    JNIEnv,
};
use std::path::PathBuf;

use crate::utils::{libs::load_lib, self};

#[no_mangle]
fn load(env: JNIEnv, _: JClass, _: JString) -> jboolean {
    load_bootstrap(&env);
    load_lib_unity(&env);
    return 1;
}

#[no_mangle]
fn unload(_: JNIEnv, _: JClass) {
    info!("unload");
}

fn load_bootstrap(env: &JNIEnv) {
    let bootstrap_lib = load_lib(&PathBuf::from("libBootstrap.so"))
        .expect("Couldn't load libBootstrap!");

    let on_load: utils::libs::NativeMethod<fn(*mut JavaVM, *mut libc::c_void)> = bootstrap_lib
        .sym("JNI_OnLoad")
        .expect("Couldn't find JNI_OnLoad!");

    (on_load)(env.get_java_vm().expect("msg").get_java_vm_pointer(), std::ptr::null_mut());

    let initialize: utils::libs::NativeMethod<fn()> = bootstrap_lib
        .sym("Initialize")
        .expect("Couldn't find Initialize!");

    (initialize)();
}

fn load_lib_unity(env: &JNIEnv) {
    let unity_lib = load_lib(&PathBuf::from("libunity.so"))
        .expect("Couldn't load libunity!");

    let on_load: utils::libs::NativeMethod<fn(*mut JavaVM, *mut libc::c_void)> = unity_lib
        .sym("JNI_OnLoad")
        .expect("Couldn't find JNI_OnLoad!");

    (on_load)(
        env.get_java_vm().expect("Failed to get JavaVM from JNIEnv").get_java_vm_pointer(),
        std::ptr::null_mut()
    );
}