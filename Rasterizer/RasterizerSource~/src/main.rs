use std::fs;
use std::io::{self, Write};
use std::path::{Path, PathBuf};
use std::process;

use sdfx_rasterizer::{
    compare_to_original, print_reduction, rasterize_svg, vectorize_image_full, SvgStats,
    VectorizeParams,
};

fn input_dir() -> PathBuf {
    std::env::current_dir().unwrap().join("input")
}

fn output_dir() -> PathBuf {
    std::env::current_dir().unwrap().join("output")
}

fn ensure_directories() -> io::Result<()> {
    fs::create_dir_all(input_dir())?;
    fs::create_dir_all(output_dir())?;
    Ok(())
}

fn ask(query: &str) -> io::Result<String> {
    print!("{query}");
    io::stdout().flush()?;
    let mut buf = String::new();
    io::stdin().read_line(&mut buf)?;
    Ok(buf.trim().to_string())
}

fn ask_number<T>(query: &str, default: T) -> io::Result<T>
where
    T: std::str::FromStr + std::fmt::Display,
{
    let ans = ask(query)?;
    if ans.is_empty() {
        return Ok(default);
    }
    match ans.parse::<T>() {
        Ok(value) => Ok(value),
        Err(_) => {
            println!("Invalid number, using default: {default}");
            Ok(default)
        }
    }
}

fn params_from_prompts() -> io::Result<VectorizeParams> {
    println!("\n--- Configure Optimization & Simplification ---");
    println!("(Press Enter to use defaults)");

    let color_mode = match ask("Color mode - `color` or `bw` (default: color): ")?
        .to_lowercase()
        .as_str()
    {
        "bw" | "binary" | "b" => 1,
        _ => 0,
    };

    let curve_mode =
        match ask("Curve fitting mode - `pixel`, `polygon`, `spline` (default: spline): ")?
            .to_lowercase()
            .as_str()
        {
            "pixel" | "none" => 2,
            "polygon" | "poly" => 1,
            _ => 0,
        };

    Ok(VectorizeParams {
        color_mode,
        curve_mode,
        filter_speckle: ask_number("Filter Speckle (speckle removal, default: 4): ", 4u32)?,
        corner_threshold: ask_number(
            "Corner Threshold (corner angle in degrees, default: 60): ",
            60i32,
        )?,
        splice_threshold: ask_number(
            "Splice Threshold (spline simplification angle in degrees, default: 45): ",
            45i32,
        )?,
        precision: ask_number(
            "Output precision (decimal places, lower = smaller file, default: 2): ",
            2u8,
        )?,
        simplify_tolerance: ask_number(
            "Simplify tolerance (px; 0 = off, higher = fewer curves, default: 3.0): ",
            3.0f64,
        )?,
        min_similarity: ask_number(
            "Minimum similarity to preserve (%, 0 = off; auto-simplifies down to it, default: 98): ",
            98.0f64,
        )?,
    })
}

fn main() {
    if let Err(err) = run() {
        eprintln!("An unexpected error occurred: {err}");
        process::exit(1);
    }
}

fn run() -> io::Result<()> {
    println!("\n--- VTracer Vectorizer & Simplifier ---\n");
    ensure_directories()?;

    let mut files: Vec<String> = fs::read_dir(input_dir())?
        .filter_map(|entry| entry.ok())
        .filter(|entry| entry.path().is_file())
        .filter_map(|entry| entry.file_name().into_string().ok())
        .filter(|name| !name.starts_with('.'))
        .collect();
    files.sort();

    if files.is_empty() {
        println!("No files found in the \"input\" directory.");
        println!(
            "Please add an image (PNG, JPG, BMP) to {} and try again.",
            input_dir().display()
        );
        return Ok(());
    }

    println!("Available files in ./input:");
    for (index, file) in files.iter().enumerate() {
        println!("  [{}] {}", index + 1, file);
    }

    let file_index = loop {
        let ans = ask(&format!("\nSelect a file to process (1-{}): ", files.len()))?;
        match ans.parse::<usize>() {
            Ok(n) if n >= 1 && n <= files.len() => break n - 1,
            _ => println!("Invalid selection. Please enter a valid number."),
        }
    };

    let selected_file = &files[file_index];
    let input_path = input_dir().join(selected_file);
    let params = params_from_prompts()?;

    println!("\nProcessing with options:");
    println!(
        "  color_mode:       {}",
        if params.color_mode == 1 {
            "bw"
        } else {
            "color"
        }
    );
    println!(
        "  mode:             {}",
        match params.curve_mode {
            1 => "polygon",
            2 => "pixel",
            _ => "spline",
        }
    );
    println!("  filter_speckle:   {}", params.filter_speckle);
    println!("  corner_threshold: {}", params.corner_threshold);
    println!("  splice_threshold: {}", params.splice_threshold);

    let stem = Path::new(selected_file)
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or("output");
    let raw_path = output_dir().join(format!("{stem}.simplified.svg"));
    let min_path = output_dir().join(format!("{stem}.min.svg"));

    println!("\nTracing {selected_file}...");
    if params.min_similarity > 0.0 {
        println!(
            "\nAuto-simplifying as far as possible while similarity >= {:.2}%...",
            params.min_similarity
        );
        println!("(sweeping filter_speckle + curve tolerance in parallel to minimize primitives)");
    } else if params.simplify_tolerance > 0.0 {
        println!(
            "\nSimplifying geometry (fit tolerance {} px)...",
            params.simplify_tolerance
        );
    }

    let result = match vectorize_image_full(&input_path, &params) {
        Ok(result) => result,
        Err(msg) => {
            eprintln!("Error during vectorization: {msg}");
            process::exit(1);
        }
    };

    let raw_stats = SvgStats::from_svg(&result.raw_svg);
    println!("\n--- Raw VTracer output ---");
    raw_stats.print();
    fs::write(&raw_path, &result.raw_svg)?;

    if params.min_similarity <= 0.0 && params.simplify_tolerance > 0.0 {
        println!("\n--- After geometric simplification ---");
        SvgStats::from_svg(&result.optimized_svg).print();
    }

    println!("\nOptimizing with usvg (precision {})...", params.precision);
    let opt_stats = SvgStats::from_svg(&result.optimized_svg);
    fs::write(&min_path, &result.optimized_svg)?;

    println!("\n--- Optimized output ---");
    opt_stats.print();
    print_reduction(&raw_stats, &opt_stats);

    println!("\nRasterizing optimized SVG and comparing to original...");
    let raster_path = output_dir().join(format!("{stem}.min.png"));
    let compare_path = output_dir().join(format!("{stem}.compare.png"));

    let comparison = rasterize_svg(&result.optimized_svg, &raster_path)
        .and_then(|_| compare_to_original(&input_path, &raster_path, &compare_path));

    println!("\nSuccess! Files written:");
    println!("  raw:        {}", raw_path.display());
    println!("  optimized:  {}", min_path.display());
    match comparison {
        Ok(()) => {
            println!("  raster:     {}", raster_path.display());
            println!("  comparison: {}", compare_path.display());
        }
        Err(e) => eprintln!("  (comparison step failed: {e})"),
    }

    Ok(())
}
