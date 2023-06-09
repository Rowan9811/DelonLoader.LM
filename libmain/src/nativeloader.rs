use jni::{
    objects::{JClass, JString},
    sys::{jboolean, JavaVM},
    JNIEnv,
};
use std::path::PathBuf;

use crate::utils::{libs::load_lib, self};

#[no_mangle]
fn load(env: JNIEnv, _: JClass, _: JString) -> jboolean {
    let unity_lib = load_lib(&PathBuf::from("libunity.so"))
        .expect("Couldn't load libunity!");

    let on_load: utils::libs::NativeMethod<fn(*mut JavaVM, *mut libc::c_void)> = unity_lib.sym("JNI_OnLoad")
        .expect("Couldn't find JNI_OnLoad!");

    (on_load)(env.get_java_vm().expect("msg").get_java_vm_pointer(), std::ptr::null_mut());

    return 1;
}

#[no_mangle]
fn unload(_: JNIEnv, _: JClass) {
    info!("unload");
}