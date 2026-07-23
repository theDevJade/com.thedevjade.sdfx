use std::ffi::CString;
use std::os::raw::{c_char, c_int};

use crate::{vectorize_rgba, VectorizeParams};

pub const SDFX_OK: c_int = 0;
pub const SDFX_ERR_NULL: c_int = -1;
pub const SDFX_ERR_INVALID: c_int = -2;
pub const SDFX_ERR_TRACE: c_int = -3;

#[no_mangle]
pub unsafe extern "C" fn sdfx_vectorize_rgba(
    pixels: *const u8,
    width: u32,
    height: u32,
    params: *const VectorizeParams,
    out_svg: *mut *mut c_char,
    out_path_count: *mut i32,
) -> c_int {
    if pixels.is_null() || params.is_null() || out_svg.is_null() || out_path_count.is_null() {
        return SDFX_ERR_NULL;
    }

    if width == 0 || height == 0 {
        return SDFX_ERR_INVALID;
    }

    let byte_len = (width as usize)
        .checked_mul(height as usize)
        .and_then(|n| n.checked_mul(4));
    let Some(byte_len) = byte_len else {
        return SDFX_ERR_INVALID;
    };

    let slice = std::slice::from_raw_parts(pixels, byte_len);
    let params_ref = &*params;

    match vectorize_rgba(slice, width, height, params_ref) {
        Ok((svg, path_count)) => match CString::new(svg) {
            Ok(c_str) => {
                *out_svg = c_str.into_raw();
                *out_path_count = path_count as i32;
                SDFX_OK
            }
            Err(_) => SDFX_ERR_INVALID,
        },
        Err(_) => SDFX_ERR_TRACE,
    }
}

#[no_mangle]
pub unsafe extern "C" fn sdfx_string_free(s: *mut c_char) {
    if !s.is_null() {
        drop(CString::from_raw(s));
    }
}
