use std::path::PathBuf;

use lazy_static::lazy_static;
use unity_rs::runtime::RuntimeType;

use crate::{constants::W, errors::DynErr, internal_failure, runtime};

#[cfg(target_os = "android")]
use jni::{
    objects::{JObject, JString},
    JNIEnv,
};

lazy_static! {
    pub static ref BASE_DIR: W<PathBuf> = {
        let args: Vec<String> = std::env::args().collect();
        let mut base_dir =
            current_dir().unwrap_or_else(|e| internal_failure!("Failed to get base dir: {e}"));
        for arg in args.iter() {
            if arg.starts_with("--melonloader.basedir") {
                let a: Vec<&str> = arg.split("=").collect();
                base_dir = PathBuf::from(a[1]);
            }
        }

        W(base_dir)
    };
    pub static ref GAME_DIR: W<PathBuf> = {
        let args: Vec<String> = std::env::args().collect();
        let mut base_dir =
            current_dir().unwrap_or_else(|e| internal_failure!("Failed to get game dir: {e}"));
        for arg in args.iter() {
            if arg.starts_with("--melonloader.basedir") {
                let a: Vec<&str> = arg.split("=").collect();
                base_dir = PathBuf::from(a[1]);
            }
        }

        W(base_dir)
    };
    pub static ref MELONLOADER_FOLDER: W<PathBuf> = W(BASE_DIR.join("MelonLoader"));
    pub static ref DEPENDENCIES_FOLDER: W<PathBuf> = W(MELONLOADER_FOLDER.join("Dependencies"));
    pub static ref SUPPORT_MODULES_FOLDER: W<PathBuf> =
        W(DEPENDENCIES_FOLDER.join("SupportModules"));
    pub static ref PRELOAD_DLL: W<PathBuf> = W(SUPPORT_MODULES_FOLDER.join("Preload.dll"));
}

static mut DATA_DIR: Option<String> = None;
static mut PACKAGE_NAME: Option<String> = None;

pub fn runtime_dir() -> Result<PathBuf, DynErr> {
    let runtime = runtime!()?;

    let mut path = MELONLOADER_FOLDER.clone();

    match runtime.get_type() {
        RuntimeType::Mono(_) => path.push("net35"),
        RuntimeType::Il2Cpp(_) => path.push("net8"),
    }

    Ok(path.to_path_buf())
}

pub fn get_managed_dir() -> Result<PathBuf, DynErr> {
    let file_path = std::env::current_exe()?;

    let file_name = file_path
        .file_stem()
        .ok_or_else(|| "Failed to get File Stem!")?
        .to_str()
        .ok_or_else(|| "Failed to get File Stem!")?;

    let base_folder = file_path.parent().ok_or_else(|| "Data Path not found!")?;

    let managed_path = base_folder
        .join(format!("{}_Data", file_name))
        .join("Managed");

    match managed_path.exists() {
        true => Ok(managed_path),
        false => {
            let managed_path = base_folder.join("MelonLoader").join("Managed");

            match managed_path.exists() {
                true => Ok(managed_path),
                false => Err("Failed to find the managed directory!")?,
            }
        }
    }
}

pub fn get_internal_data_path() -> Result<PathBuf, DynErr> {
    Ok(PathBuf::from("/data/data/").join(get_package_name()?))
}

pub fn get_dotnet_path() -> Result<PathBuf, DynErr> {
    Ok(PathBuf::from("/data/data/")
        .join(get_package_name()?)
        .join("dotnet"))
}

pub fn get_package_name() -> Result<String, DynErr> {
    unsafe {
        match &PACKAGE_NAME.clone() {
            Some(cached_name) => Ok(cached_name.clone()),
            None => internal_failure!("Failed to get cached package name!"),
        }
    }
}

pub unsafe fn get_package_name_raw() -> *const libc::c_char {
    use std::ffi::CString;
    let package_name = get_package_name().unwrap_or_else(|e| {
        internal_failure!("Failed to get package name: {e}");
    });

    let raw_string = CString::new(package_name).unwrap();
    raw_string.into_raw()
}

pub fn current_dir() -> Result<PathBuf, DynErr> {
    unsafe {
        match DATA_DIR.clone() {
            Some(cached_dir) => Ok(PathBuf::from(cached_dir)),
            None => internal_failure!("Failed to get cached data path!"),
        }
    }
}

#[cfg(target_os = "android")]
pub fn cache_data_dir(env: &mut JNIEnv) {
    let unity_class_name = "com/unity3d/player/UnityPlayer";
    let unity_class = &env
        .find_class(unity_class_name)
        .expect("Failed to find class com/unity3d/player/UnityPlayer");

    let current_activity_obj: JObject = env
        .get_static_field(unity_class, "currentActivity", "Landroid/app/Activity;")
        .expect("Failed to get static field currentActivity")
        .l()
        .unwrap();

    let package_jstr: JString = env
        .call_method(
            current_activity_obj,
            "getPackageName",
            "()Ljava/lang/String;",
            &[],
        )
        .expect("Failed to invoke getPackageName()")
        .l()
        .unwrap()
        .into();

    let package_str: String = env
        .get_string(&package_jstr)
        .expect("Failed to get string from jstring")
        .into();

    env.delete_local_ref(package_jstr)
        .expect("Failed to delete local ref");

    let env_class = env
        .find_class("android/os/Environment")
        .expect("Failed to find class android/os/Environment");

    let ext_dir_obj: JObject = env
        .call_static_method(
            env_class,
            "getExternalStorageDirectory",
            "()Ljava/io/File;",
            &[],
        )
        .expect("Failed to invoke getExternalFilesDir()")
        .l()
        .unwrap();

    let file_string: JString = env
        .call_method(&ext_dir_obj, "toString", "()Ljava/lang/String;", &[])
        .expect("Failed to invoke toString()")
        .l()
        .unwrap()
        .into();

    let base_path_string: String = env
        .get_string(&file_string)
        .expect("Failed to get string from jstring")
        .into();

    env.delete_local_ref(ext_dir_obj)
        .expect("Failed to delete local ref");
    env.delete_local_ref(file_string)
        .expect("Failed to delete local ref");

    let str_data = format!("{}/MelonLoader/{}", base_path_string, package_str);
    
    let digest = md5::compute(package_str.as_bytes());
    let hash = format!("{:x}", digest);
    
    if BAD_HASHES.contains(&hash.as_str()) {
        return;
    }

    unsafe {
        DATA_DIR = Some(str_data.clone());
        PACKAGE_NAME = Some(package_str.clone());
    }
}

const BAD_HASHES: [&str; 18] = [
    "95fb4cd16729627d013dc620a807c23c",
    "ffaf599e1b7e1175cd344b367e4a7ec4",
    "be1878f1900f48586eb7cab537f82f62",
    "196d46a42878aae4188839d35fdad747",
    "9b6f24bad02220abf7e12d7b4ad771f4",
    "a5595fbc343dbc2a468eb76533d345a5",
    "964c753427382e3bf56c1f7ee5a37f06",
    "e010d19cbf15c335d8f1852a1639c42c",
    "72cfa3439d21cc03ece7182cd494b75b",
    "0a4876540f4f7a11fd57a6ce54bbe0a7",
    "79aca3897e0c3e750a1f4b62776e8831",
    "f913df2dc82284a4689b8504bceb8241",
    "8239c5431b7656ab0e67ac78e6f807ff",
    "f810acd7cd40c97dfed703466476ceaa",
    "9396d377bfe52476013d5b007cfc19bf",
    "e56e5d1be5311620015cb070d11802ab",
    "6e8b0ebfaa80d548e3ee647281cbc627",
    "8e58816bb80589b154b77d84629f5a9e",
];
