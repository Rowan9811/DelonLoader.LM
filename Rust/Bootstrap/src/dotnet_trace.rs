use std::ffi::CString;
use android_liblog_sys::__android_log_write;

pub unsafe fn redirect_stderr() {
    std::env::set_var("COREHOST_TRACE", "1");
    std::env::set_var("COREHOST_TRACE_VERBOSITY", "3");

    let mut pipes: [libc::c_int; 2] = [0; 2];
    libc::pipe(pipes.as_mut_ptr());
    libc::dup2(pipes[1], libc::STDERR_FILENO);
    let r_cstr = CString::new("r").unwrap();
    let input_file = libc::fdopen(pipes[0], r_cstr.as_ptr());
    let mut read_buffer = [0; 512];
    
    let tag = CString::new("MelonLoader").unwrap();
    loop {
        libc::fgets(read_buffer.as_mut_ptr() as *mut libc::c_char, read_buffer.len() as i32, input_file);
        __android_log_write(4, tag.as_ptr(), read_buffer.as_ptr() as *const libc::c_char);
    }
}

pub unsafe fn redirect_stdout() {
    std::env::set_var("COREHOST_TRACE", "1");
    std::env::set_var("COREHOST_TRACE_VERBOSITY", "3");

    let mut pipes: [libc::c_int; 2] = [0; 2];
    libc::pipe(pipes.as_mut_ptr());
    libc::dup2(pipes[1], libc::STDOUT_FILENO);
    let r_cstr = CString::new("r").unwrap();
    let input_file = libc::fdopen(pipes[0], r_cstr.as_ptr());
    let mut read_buffer = [0; 512];
    
    let tag = CString::new("MelonLoader").unwrap();
    loop {
        libc::fgets(read_buffer.as_mut_ptr() as *mut libc::c_char, read_buffer.len() as i32, input_file);
        __android_log_write(4, tag.as_ptr(), read_buffer.as_ptr() as *const libc::c_char);
    }
}