mod ffi;

use std::fs;
use std::path::{Path, PathBuf};

use image::RgbaImage;
use visioncortex::PathSimplifyMode;
use vtracer::{convert_image_to_svg, ColorMode, Config, Hierarchical};

const PROBE_MAX_DIM: u32 = 512;

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct VectorizeParams {
    pub color_mode: u8,
    pub curve_mode: u8,
    pub filter_speckle: u32,
    pub corner_threshold: i32,
    pub splice_threshold: i32,
    pub precision: u8,
    pub simplify_tolerance: f64,
    pub min_similarity: f64,
}

impl Default for VectorizeParams {
    fn default() -> Self {
        Self {
            color_mode: 0,
            curve_mode: 0,
            filter_speckle: 4,
            corner_threshold: 60,
            splice_threshold: 45,
            precision: 2,
            simplify_tolerance: 3.0,
            min_similarity: 98.0,
        }
    }
}

impl VectorizeParams {
    fn to_config(self) -> Config {
        let color_mode = if self.color_mode == 1 {
            ColorMode::Binary
        } else {
            ColorMode::Color
        };

        let mode = match self.curve_mode {
            1 => PathSimplifyMode::Polygon,
            2 => PathSimplifyMode::None,
            _ => PathSimplifyMode::Spline,
        };

        Config {
            color_mode,
            hierarchical: Hierarchical::Stacked,
            filter_speckle: self.filter_speckle as usize,
            color_precision: 6,
            layer_difference: 16,
            mode,
            corner_threshold: self.corner_threshold,
            length_threshold: 4.0,
            max_iterations: 10,
            splice_threshold: self.splice_threshold,
            path_precision: Some(2),
        }
    }
}

pub struct VectorizeResult {
    pub raw_svg: String,
    pub optimized_svg: String,
}

enum ProbeSource<'a> {
    Path(&'a Path),
    Rgba(&'a RgbaImage),
}

pub fn vectorize_rgba(
    pixels: &[u8],
    width: u32,
    height: u32,
    params: &VectorizeParams,
) -> Result<(String, usize), String> {
    let expected = (width as usize)
        .checked_mul(height as usize)
        .and_then(|n| n.checked_mul(4))
        .ok_or_else(|| "invalid image dimensions".to_string())?;

    if pixels.len() != expected {
        return Err(format!(
            "expected {expected} RGBA bytes, got {}",
            pixels.len()
        ));
    }

    let mut img = RgbaImage::new(width, height);
    for (pixel, chunk) in img.pixels_mut().zip(pixels.chunks_exact(4)) {
        pixel.0 = [chunk[0], chunk[1], chunk[2], chunk[3]];
    }

    let temp_png = temp_png_path();
    img.save(&temp_png).map_err(|e| e.to_string())?;

    let result = run_pipeline(&temp_png, params, ProbeSource::Rgba(&img)).map(|output| {
        let path_count = SvgStats::from_svg(&output.optimized_svg).paths;
        (output.optimized_svg, path_count)
    });

    let _ = fs::remove_file(&temp_png);
    result
}

pub fn vectorize_image_path(
    input_path: &Path,
    params: &VectorizeParams,
) -> Result<(String, usize), String> {
    let output = run_pipeline(input_path, params, ProbeSource::Path(input_path))?;
    let path_count = SvgStats::from_svg(&output.optimized_svg).paths;
    Ok((output.optimized_svg, path_count))
}

pub fn vectorize_image_full(
    input_path: &Path,
    params: &VectorizeParams,
) -> Result<VectorizeResult, String> {
    run_pipeline(input_path, params, ProbeSource::Path(input_path))
}

fn temp_png_path() -> PathBuf {
    std::env::temp_dir().join(format!("sdfx_vectorize_{}.png", std::process::id()))
}

fn run_pipeline(
    input_path: &Path,
    params: &VectorizeParams,
    probe_source: ProbeSource<'_>,
) -> Result<VectorizeResult, String> {
    let config = params.to_config();
    let raw_svg = trace_svg(input_path, config.clone(), 0)?;

    let simplified_svg = if params.min_similarity > 0.0 {
        autotune_svg(
            input_path,
            &raw_svg,
            &config,
            probe_source,
            params.min_similarity,
            params.simplify_tolerance,
        )?
    } else if params.simplify_tolerance > 0.0 {
        simplify_svg(&raw_svg, params.simplify_tolerance)
    } else {
        raw_svg.clone()
    };

    let optimized = optimize_svg(&simplified_svg, params.precision)?;

    Ok(VectorizeResult {
        raw_svg,
        optimized_svg: optimized,
    })
}

type AutotuneCandidate = Option<(String, f64, f64, usize)>;

fn autotune_svg(
    input_path: &Path,
    raw_svg: &str,
    config: &Config,
    probe_source: ProbeSource<'_>,
    min_similarity: f64,
    tolerance: f64,
) -> Result<String, String> {
    let base_fs = config.filter_speckle.max(1);
    let mut candidates: Vec<usize> =
        vec![config.filter_speckle, base_fs * 2, base_fs * 4, base_fs * 8];
    candidates.retain(|&v| v <= 256);
    candidates.sort_unstable();
    candidates.dedup();

    let probe_original = make_probe_original(raw_svg, probe_source)?;

    let results: Vec<(usize, AutotuneCandidate)> = std::thread::scope(|scope| {
        let handles: Vec<_> = candidates
            .iter()
            .enumerate()
            .map(|(idx, &fs)| {
                let cfg = config.clone();
                let probe_original = &probe_original;
                scope.spawn(move || {
                    let traced = if fs == cfg.filter_speckle {
                        raw_svg.to_string()
                    } else {
                        let mut c = cfg.clone();
                        c.filter_speckle = fs;
                        match trace_svg(input_path, c, idx) {
                            Ok(s) => s,
                            Err(_) => return (fs, None),
                        }
                    };
                    match autotune_by_similarity(&traced, probe_original, min_similarity, tolerance)
                    {
                        Ok((svg, tol, sim)) if sim + 1e-6 >= min_similarity => {
                            let prims = SvgStats::from_svg(&svg).primitives();
                            (fs, Some((svg, tol, sim, prims)))
                        }
                        _ => (fs, None),
                    }
                })
            })
            .collect();
        handles.into_iter().map(|h| h.join().unwrap()).collect()
    });

    let mut best: Option<(String, usize, f64, f64, usize)> = None;
    for (fs, result) in results {
        match result {
            Some((svg, tol, sim, prims)) => {
                println!(
                    "  filter_speckle {fs:>3}: {prims} primitives (tol {tol:.2} px, {sim:.2}%)"
                );
                if best.as_ref().is_none_or(|b| prims < b.4) {
                    best = Some((svg, fs, tol, sim, prims));
                }
            }
            None => println!("  filter_speckle {fs:>3}: can't meet the floor"),
        }
    }

    if let Some((_, fs, tol, sim, prims)) = &best {
        println!(
            "\n--- Best: filter_speckle {fs}, tolerance {tol:.2} px, ~{sim:.2}% probe similarity, {prims} primitives ---"
        );
        SvgStats::from_svg(&best.as_ref().unwrap().0).print();
    }

    Ok(best
        .map(|(svg, _, _, _, _)| svg)
        .unwrap_or_else(|| raw_svg.to_string()))
}

fn make_probe_original(raw_svg: &str, source: ProbeSource<'_>) -> Result<RgbaImage, String> {
    let base = render_svg_to_image(raw_svg, PROBE_MAX_DIM)?;
    let (pw, ph) = base.dimensions();
    let rgba = match source {
        ProbeSource::Path(path) => image::open(path).map_err(|e| e.to_string())?.to_rgba8(),
        ProbeSource::Rgba(img) => img.clone(),
    };
    Ok(image::imageops::resize(
        &rgba,
        pw,
        ph,
        image::imageops::FilterType::Triangle,
    ))
}

fn optimize_svg(svg: &str, precision: u8) -> Result<String, String> {
    use resvg::usvg;

    let options = usvg::Options::default();
    let tree = usvg::Tree::from_str(svg, &options).map_err(|e| e.to_string())?;

    let write_options = usvg::WriteOptions {
        coordinates_precision: precision,
        transforms_precision: precision,
        indent: usvg::Indent::None,
        attributes_indent: usvg::Indent::None,
        ..usvg::WriteOptions::default()
    };
    Ok(tree.to_string(&write_options))
}

pub fn simplify_svg(svg: &str, tolerance: f64) -> String {
    let mut out = String::with_capacity(svg.len());
    let mut rest = svg;
    while let Some(i) = rest.find("d=\"") {
        out.push_str(&rest[..i + 3]);
        let after = &rest[i + 3..];
        match after.find('"') {
            Some(j) => {
                out.push_str(&simplify_path_data(&after[..j], tolerance));
                out.push('"');
                rest = &after[j + 1..];
            }
            None => {
                out.push_str(after);
                return out;
            }
        }
    }
    out.push_str(rest);
    out
}

fn simplify_path_data(d: &str, tolerance: f64) -> String {
    use flo_curves::bezier::{fit_curve_cubic, BezierCurve, BezierCurveFactory, Curve};
    use flo_curves::geo::{Coord2, Coordinate};
    use svgtypes::{PathParser, PathSegment};

    fn flush(out: &mut String, start: Coord2, curves: &[Curve<Coord2>], closed: bool, tol: f64) {
        if curves.is_empty() {
            return;
        }
        push_move(out, start);

        let steps = 10;
        let mut points: Vec<Coord2> = Vec::with_capacity(curves.len() * steps + 1);
        points.push(start);
        for curve in curves {
            for step in 1..=steps {
                let t = step as f64 / steps as f64;
                points.push(curve.point_at_pos(t));
            }
        }

        let fitted = if points.len() >= 3 {
            let n = points.len();
            let start_tangent = (points[1] - points[0]).to_unit_vector();
            let end_tangent = (points[n - 2] - points[n - 1]).to_unit_vector();
            fit_curve_cubic::<Curve<Coord2>>(&points, &start_tangent, &end_tangent, tol)
        } else {
            Vec::new()
        };

        if !fitted.is_empty() && fitted.len() < curves.len() {
            for curve in &fitted {
                push_cubic(out, curve);
            }
        } else {
            for curve in curves {
                push_cubic(out, curve);
            }
        }
        if closed {
            out.push_str(" Z");
        }
    }

    let mut out = String::with_capacity(d.len());
    let mut current = Coord2(0.0, 0.0);
    let mut start = Coord2(0.0, 0.0);
    let mut curves: Vec<Curve<Coord2>> = Vec::new();
    let mut closed = false;
    let mut first_subpath = true;

    for segment in PathParser::from(d) {
        let segment = match segment {
            Ok(s) => s,
            Err(_) => return d.to_string(),
        };
        match segment {
            PathSegment::MoveTo { abs: true, x, y } => {
                if !first_subpath {
                    if !out.is_empty() {
                        out.push(' ');
                    }
                    flush(&mut out, start, &curves, closed, tolerance);
                }
                first_subpath = false;
                curves.clear();
                closed = false;
                start = Coord2(x, y);
                current = start;
            }
            PathSegment::CurveTo {
                abs: true,
                x1,
                y1,
                x2,
                y2,
                x,
                y,
            } => {
                let end = Coord2(x, y);
                curves.push(Curve::from_points(
                    current,
                    (Coord2(x1, y1), Coord2(x2, y2)),
                    end,
                ));
                current = end;
            }
            PathSegment::ClosePath { .. } => {
                closed = true;
            }
            _ => return d.to_string(),
        }
    }

    if !out.is_empty() {
        out.push(' ');
    }
    flush(&mut out, start, &curves, closed, tolerance);
    out
}

fn push_move(out: &mut String, p: flo_curves::geo::Coord2) {
    out.push('M');
    out.push_str(&fmt_num(p.0));
    out.push(' ');
    out.push_str(&fmt_num(p.1));
}

fn push_cubic(out: &mut String, curve: &flo_curves::bezier::Curve<flo_curves::geo::Coord2>) {
    use flo_curves::bezier::BezierCurve;
    let (c1, c2) = curve.control_points();
    let end = curve.end_point();
    out.push_str(" C");
    for (x, y) in [(c1.0, c1.1), (c2.0, c2.1), (end.0, end.1)] {
        out.push_str(&fmt_num(x));
        out.push(' ');
        out.push_str(&fmt_num(y));
        out.push(' ');
    }
    out.pop();
}

fn fmt_num(v: f64) -> String {
    let s = format!("{v:.4}");
    let trimmed = s.trim_end_matches('0').trim_end_matches('.');
    if trimmed.is_empty() || trimmed == "-0" {
        "0".to_string()
    } else {
        trimmed.to_string()
    }
}

fn trace_svg(input_path: &Path, config: Config, id: usize) -> Result<String, String> {
    let scratch = std::env::temp_dir().join(format!("sdfx_vectorizer_scratch_{id}.svg"));
    convert_image_to_svg(input_path, &scratch, config)?;
    let svg = fs::read_to_string(&scratch).map_err(|e| e.to_string())?;
    let _ = fs::remove_file(&scratch);
    Ok(svg)
}

fn autotune_by_similarity(
    raw_svg: &str,
    probe_original: &RgbaImage,
    floor: f64,
    start_tol: f64,
) -> Result<(String, f64, f64), String> {
    const MAX_TOL: f64 = 512.0;

    let probe = |svg: &str| -> f64 {
        match render_svg_to_image(svg, PROBE_MAX_DIM) {
            Ok(img) => image_similarity(probe_original, &img),
            Err(_) => 0.0,
        }
    };

    let mut best_svg = raw_svg.to_string();
    let mut best_tol = 0.0;
    let mut best_sim = probe(raw_svg);

    if best_sim < floor {
        return Ok((best_svg, best_tol, best_sim));
    }

    let mut tol = if start_tol > 0.0 { start_tol } else { 1.0 };
    let mut bad_tol: Option<f64> = None;
    while tol <= MAX_TOL {
        let svg = simplify_svg(raw_svg, tol);
        let sim = probe(&svg);
        if sim >= floor {
            best_svg = svg;
            best_tol = tol;
            best_sim = sim;
            tol *= 1.5;
        } else {
            bad_tol = Some(tol);
            break;
        }
    }

    if let Some(hi0) = bad_tol {
        let mut lo = best_tol;
        let mut hi = hi0;
        for _ in 0..8 {
            let mid = (lo + hi) / 2.0;
            let svg = simplify_svg(raw_svg, mid);
            let sim = probe(&svg);
            if sim >= floor {
                lo = mid;
                best_svg = svg;
                best_tol = mid;
                best_sim = sim;
            } else {
                hi = mid;
            }
        }
    }

    Ok((best_svg, best_tol, best_sim))
}

fn render_svg_to_image(svg: &str, max_dim: u32) -> Result<RgbaImage, String> {
    use resvg::{tiny_skia, usvg};

    let options = usvg::Options::default();
    let tree = usvg::Tree::from_str(svg, &options).map_err(|e| e.to_string())?;

    let size = tree.size();
    let (sw, sh) = (size.width(), size.height());
    let scale = (max_dim as f32 / sw.max(sh)).min(1.0);
    let pw = ((sw * scale).round() as u32).max(1);
    let ph = ((sh * scale).round() as u32).max(1);

    let mut pixmap = tiny_skia::Pixmap::new(pw, ph).ok_or("invalid probe dimensions")?;
    let transform = tiny_skia::Transform::from_scale(pw as f32 / sw, ph as f32 / sh);
    resvg::render(&tree, transform, &mut pixmap.as_mut());

    let mut img = RgbaImage::new(pw, ph);
    for (i, px) in pixmap.pixels().iter().enumerate() {
        let color = px.demultiply();
        let x = i as u32 % pw;
        let y = i as u32 / pw;
        img.put_pixel(
            x,
            y,
            image::Rgba([color.red(), color.green(), color.blue(), color.alpha()]),
        );
    }
    Ok(img)
}

fn image_similarity(a: &RgbaImage, b: &RgbaImage) -> f64 {
    let count = (a.width() as u64) * (a.height() as u64) * 4;
    if count == 0 {
        return 0.0;
    }
    let mut sum_abs = 0.0f64;
    for (pa, pb) in a.pixels().zip(b.pixels()) {
        for c in 0..4 {
            sum_abs += (pa.0[c] as f64 - pb.0[c] as f64).abs();
        }
    }
    (1.0 - (sum_abs / count as f64) / 255.0) * 100.0
}

pub fn rasterize_svg(svg: &str, out_png: &Path) -> Result<(u32, u32), String> {
    use resvg::{tiny_skia, usvg};

    let options = usvg::Options::default();
    let tree = usvg::Tree::from_str(svg, &options).map_err(|e| e.to_string())?;

    let size = tree.size().to_int_size();
    let (width, height) = (size.width(), size.height());
    let mut pixmap = tiny_skia::Pixmap::new(width, height).ok_or("invalid raster dimensions")?;

    resvg::render(&tree, tiny_skia::Transform::default(), &mut pixmap.as_mut());
    pixmap.save_png(out_png).map_err(|e| e.to_string())?;
    Ok((width, height))
}

pub fn compare_to_original(
    original_path: &Path,
    raster_png: &Path,
    out_png: &Path,
) -> Result<(), String> {
    use image::{imageops, Rgba, RgbaImage};

    let original = image::open(original_path)
        .map_err(|e| format!("failed to open original: {e}"))?
        .to_rgba8();
    let mut raster = image::open(raster_png)
        .map_err(|e| format!("failed to open raster: {e}"))?
        .to_rgba8();

    let (ow, oh) = original.dimensions();

    if raster.dimensions() != (ow, oh) {
        raster = imageops::resize(&raster, ow, oh, imageops::FilterType::Triangle);
    }

    let mut sum_sq = 0.0f64;
    let mut sum_abs = 0.0f64;
    let channels = (ow as u64) * (oh as u64) * 4;
    for (po, pr) in original.pixels().zip(raster.pixels()) {
        for c in 0..4 {
            let diff = po.0[c] as f64 - pr.0[c] as f64;
            sum_sq += diff * diff;
            sum_abs += diff.abs();
        }
    }
    let mse = sum_sq / channels as f64;
    let mae = sum_abs / channels as f64;
    let psnr = if mse == 0.0 {
        f64::INFINITY
    } else {
        10.0 * (255.0f64 * 255.0 / mse).log10()
    };
    let similarity = (1.0 - mae / 255.0) * 100.0;

    println!("\n--- Comparison to original ---");
    println!("  dimensions:  {ow}x{oh}");
    println!("  MSE:         {mse:.2}");
    println!("  MAE:         {mae:.2}");
    if psnr.is_finite() {
        println!("  PSNR:        {psnr:.2} dB");
    } else {
        println!("  PSNR:        inf (identical)");
    }
    println!("  similarity:  {similarity:.2}%");

    let target_h: u32 = oh.clamp(1, 512);
    let target_w = ((ow as f64) * (target_h as f64 / oh as f64)).round() as u32;
    let target_w = target_w.max(1);
    let left = imageops::resize(
        &original,
        target_w,
        target_h,
        imageops::FilterType::Triangle,
    );
    let right = imageops::resize(&raster, target_w, target_h, imageops::FilterType::Triangle);

    let gap: u32 = 12;
    let mut canvas =
        RgbaImage::from_pixel(target_w * 2 + gap, target_h, Rgba([255, 255, 255, 255]));
    imageops::overlay(&mut canvas, &left, 0, 0);
    imageops::overlay(&mut canvas, &right, (target_w + gap) as i64, 0);
    canvas
        .save(out_png)
        .map_err(|e| format!("failed to write comparison: {e}"))?;

    Ok(())
}

pub struct SvgStats {
    pub bytes: usize,
    pub paths: usize,
    pub coordinates: usize,
    pub subpaths: usize,
    pub lines: usize,
    pub cubics: usize,
    pub quadratics: usize,
    pub arcs: usize,
    pub closes: usize,
}

impl SvgStats {
    pub fn from_svg(svg: &str) -> Self {
        let bytes = svg.len();
        let paths = svg.matches("<path").count();
        let mut coordinates = 0;
        let mut subpaths = 0;
        let mut lines = 0;
        let mut cubics = 0;
        let mut quadratics = 0;
        let mut arcs = 0;
        let mut closes = 0;

        for d in d_attributes(svg) {
            let mut in_number = false;
            for c in d.chars() {
                if c.is_ascii_alphabetic() {
                    in_number = false;
                    match c.to_ascii_uppercase() {
                        'M' => subpaths += 1,
                        'L' | 'H' | 'V' => lines += 1,
                        'C' | 'S' => cubics += 1,
                        'Q' | 'T' => quadratics += 1,
                        'A' => arcs += 1,
                        'Z' => closes += 1,
                        _ => {}
                    }
                } else if c.is_ascii_digit() || matches!(c, '.' | '-' | '+') {
                    if !in_number {
                        coordinates += 1;
                        in_number = true;
                    }
                } else {
                    in_number = false;
                }
            }
        }

        SvgStats {
            bytes,
            paths,
            coordinates,
            subpaths,
            lines,
            cubics,
            quadratics,
            arcs,
            closes,
        }
    }

    pub fn primitives(&self) -> usize {
        self.lines + self.cubics + self.quadratics + self.arcs
    }

    pub fn print(&self) {
        println!(
            "  size:        {} bytes ({:.1} KiB)",
            self.bytes,
            self.bytes as f64 / 1024.0
        );
        println!("  paths:       {}", self.paths);
        println!("  coordinates: {}", self.coordinates);
        println!("  primitives:  {}", self.primitives());
        println!(
            "    subpaths={} lines={} cubics={} quadratics={} arcs={} closes={}",
            self.subpaths, self.lines, self.cubics, self.quadratics, self.arcs, self.closes
        );
    }
}

fn d_attributes(svg: &str) -> Vec<&str> {
    let mut out = Vec::new();
    let mut rest = svg;
    while let Some(i) = rest.find("d=\"") {
        let after = &rest[i + 3..];
        match after.find('"') {
            Some(j) => {
                out.push(&after[..j]);
                rest = &after[j + 1..];
            }
            None => break,
        }
    }
    out
}

pub fn print_reduction(before: &SvgStats, after: &SvgStats) {
    let pct = |a: usize, b: usize| -> f64 {
        if a == 0 {
            0.0
        } else {
            (1.0 - b as f64 / a as f64) * 100.0
        }
    };
    println!("\n--- Reduction ---");
    println!(
        "  size:        -{:.1}% ({} -> {} bytes)",
        pct(before.bytes, after.bytes),
        before.bytes,
        after.bytes
    );
    println!(
        "  coordinates: -{:.1}% ({} -> {})",
        pct(before.coordinates, after.coordinates),
        before.coordinates,
        after.coordinates
    );
    println!(
        "  primitives:  -{:.1}% ({} -> {})",
        pct(before.primitives(), after.primitives()),
        before.primitives(),
        after.primitives()
    );
}
