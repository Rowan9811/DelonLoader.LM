use crate::{
    constants, debug_enabled,
    errors::{logerr::LogError, DynErr},
};
use colored::Colorize;
use std::{io::Write, ffi::{CString, c_char}, sync::Arc};

#[cfg(target_os = "android")]
use android_liblog_sys::{__android_log_write, LogPriority};

#[derive(Debug)]
#[repr(u8)]
pub enum LogLevel {
    Info,
    Warning,
    Error,
    Debug,
}

lazy_static::lazy_static! {
    static ref MELON_LOADER_TAG: Arc<CString> = {
        CString::new("MelonLoader").expect("CString conversion failed").into()
    };
}

impl std::convert::TryFrom<u8> for LogLevel {
    type Error = DynErr;

    fn try_from(value: u8) -> Result<Self, <LogLevel as std::convert::TryFrom<u8>>::Error> {
        match value {
            0 => Ok(LogLevel::Info),
            1 => Ok(LogLevel::Warning),
            2 => Ok(LogLevel::Error),
            3 => Ok(LogLevel::Debug),
            _ => Err("Invalid value for enum `LogLevel` possible: [1, 2, 3]".into()),
        }
    }
}

macro_rules! log_path {
    () => {
        $crate::melonenv::paths::BASE_DIR.clone().join("MelonLoader").join("Latest-Bootstrap.log")
    };
}

pub fn init() -> Result<(), DynErr> {
    let log_file = log_path!();

    if log_file.exists() {
        std::fs::remove_file(log_file).map_err(|_| LogError::FailedToDeleteOldLog)?;
    }

    Ok(())
}

fn write(msg: &str) -> Result<(), DynErr> {
    let log_file = log_path!();

    let mut file = std::fs::OpenOptions::new()
        .write(true)
        .append(true)
        .create(true)
        .open(&log_file)
        .map_err(|_| LogError::FailedToWriteToLog)?;

    let message = format!("{}\r\n", msg);

    file.write_all(message.as_bytes())
        .map_err(|_| LogError::FailedToWriteToLog)?;

    Ok(())
}

/// logs to console and file, should not be used, use the log! macro instead
pub fn log_console_file(level: LogLevel, message: &str) -> Result<(), LogError> {
    match level {
        LogLevel::Info => {
            // [19:11:50.321] message
            let console_string = message;

            let file_string = format!("[{}] {}", timestamp(), message);

            crate::log_console!(level, "{}", console_string);
            write(&file_string).map_err(|_| LogError::FailedToWriteToLog)?;
        }
        LogLevel::Warning => {
            //[19:11:50.321] [WARNING] message
            let console_string = message;

            let file_string = format!("[{}] [WARNING] {}", timestamp(), message);

            crate::log_console!(level, "{}", console_string.bright_yellow());

            write(&file_string).map_err(|_| LogError::FailedToWriteToLog)?;
        }
        LogLevel::Error => {
            //[19:11:50.321] [ERROR] message
            let log_string = format!("[{}] [ERROR] {}", timestamp(), message);
            crate::log_console!(level, "{}", message.color(constants::RED));
            write(&log_string).map_err(|_| LogError::FailedToWriteToLog)?;
        }
        LogLevel::Debug => {
            if !debug_enabled!() {
                return Ok(());
            }
            //[19:11:50.321] [DEBUG] message
            let console_string = message;

            let file_string = format!("[{}] [DEBUG] {}", timestamp(), message);

            crate::log_console!(level, "{}", console_string);
            write(&file_string).map_err(|_| LogError::FailedToWriteToLog)?;
        }
    }

    Ok(())
}

/// Fetches the current time, and formats it.
///
/// returns a String, formatted as 15:23:24:123
fn timestamp() -> String {
    let now = chrono::Local::now();
    let time = now.time();

    time.format("%H:%M:%S.%3f").to_string()
}

pub unsafe fn log_console_interop(input: *const c_char) {
    let input = std::ffi::CStr::from_ptr(input).to_string_lossy();
    crate::log_console!(LogLevel::Info, "{}", input);
}

#[macro_export]
macro_rules! log_console {
    ($level:expr, $($arg:tt)*) => {
        let format = std::ffi::CString::new(format!($($arg)*)).unwrap();

        let prio = match $level {
            LogLevel::Error => LogPriority::ERROR,
            LogLevel::Warning => LogPriority::WARN,
            LogLevel::Info => LogPriority::INFO,
            LogLevel::Debug => LogPriority::DEBUG,
        };

        unsafe {
            __android_log_write(prio as _, MELON_LOADER_TAG.clone().as_ptr(), format.as_ptr());
        }
    };
}

/// Logs a message to the console and log file
///
/// # Example
/// ```
/// log!("Hello World!")?;
/// ```
/// log! returns a Result<(), Box<LogError>>, so please handle this.
#[macro_export]
macro_rules! log {
    //case 1: empty
    () => {
        _ = $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Info, "")
    };

    //case 3: multiple arguments
    ($($arg:tt)*) => {{
        _ = $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Info, &format_args!($($arg)*).to_string())
    }};
}

/// Logs a message to the console and log file, with a warning prefix
///
/// # Example
/// ```
/// warn!("Hello World!")?;
/// ```
///
/// warn! returns a Result<(), Box<LogError>>, so please handle this.
#[macro_export]
macro_rules! warn {
    //case 1: empty
    () => {
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Warning, "")
    };

    //case 3: multiple arguments
    ($($arg:tt)*) => {{
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Warning, &format_args!($($arg)*).to_string())
    }};
}

/// Logs a message to the console and log file, with an error prefix
///
/// # Example
/// ```
/// error!("Hello World!")?;
/// ```
///
/// error! returns a Result<(), Box<LogError>>, so please handle this.
#[macro_export]
macro_rules! error {
    //case 1: empty
    () => {
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Error, "")
    };

    //case 3: multiple arguments
    ($($arg:tt)*) => {{
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Error, &format_args!($($arg)*).to_string())
    }};
}

/// Logs a message to the console and log file, with a debug prefix
///
/// # Example
/// ```
/// debug!("Hello World!")?;
/// ```
///
/// debug! returns a Result<(), Box<LogError>>, so please handle this.
#[macro_export]
macro_rules! debug {
    //case 1: empty
    () => {
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Debug, "")
    };

    //case 3: multiple arguments
    ($($arg:tt)*) => {{
        $crate::logging::logger::log_console_file($crate::logging::logger::LogLevel::Debug, &format_args!($($arg)*).to_string())
    }};
}

#[macro_export]
macro_rules! cstr {
    ($s:expr) => {
        std::ffi::CString::new($s)?.as_ptr()
    };

    //format str
    ($($arg:tt)*) => {
       std::ffi::CString::new(format!($($arg)*))?.as_ptr()
    };
}
