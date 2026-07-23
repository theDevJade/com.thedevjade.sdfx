$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root
$env:CARGO_TARGET_DIR = Join-Path $Root "target"

Write-Host "Building sdfx-rasterizer (release)..."
cargo build --release
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$SourceDll = Join-Path $Root "target\release\sdfx_rasterizer.dll"
$DestDir = Join-Path $Root "..\Native~\win-x64"
$DestDll = Join-Path $DestDir "sdfx_rasterizer.dll"

if (-not (Test-Path $SourceDll)) {
    Write-Error "Expected DLL not found: $SourceDll"
}

New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
Copy-Item -Force $SourceDll $DestDll

Write-Host "Copied to $DestDll"
Write-Host "Note: Native~ is ignored by Unity import; the Editor loads this DLL only after consent."
