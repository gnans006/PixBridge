#Requires -RunAsAdministrator
# Quick installer: registers PixBridgeFaceRecognition Windows service via NSSM.
# Run once from an ELEVATED PowerShell terminal:
#   Set-ExecutionPolicy Bypass -Scope Process -Force
#   .\install-face-service.ps1

$svcName  = "PixBridgeFaceRecognition"
$svcSrc   = Join-Path $PSScriptRoot "src\PixBridge.FaceRecognition"

# ── Resolve Python ─────────────────────────────────────────────────────────────
$python = $null
foreach ($candidate in @(
    (Get-Command python  -ErrorAction SilentlyContinue)?.Source,
    (Get-Command python3 -ErrorAction SilentlyContinue)?.Source,
    (& { try { (py -3 -c "import sys; print(sys.executable)") } catch { $null } })
)) {
    if ($candidate -and (Test-Path $candidate)) { $python = $candidate; break }
}
# Fallback: scan common user-install paths
if (!$python) {
    $python = Get-ChildItem "C:\Users\*\AppData\Local\Programs\Python\Python3*\python.exe" `
        -ErrorAction SilentlyContinue | Sort-Object -Descending | Select-Object -ExpandProperty FullName -First 1
}
if (!$python) {
    Write-Host "ERROR: Python 3 not found. Install Python 3.11+ and retry." -ForegroundColor Red; exit 1
}
Write-Host "  Python: $python" -ForegroundColor Cyan

# ── Resolve NSSM ──────────────────────────────────────────────────────────────
$nssmExe = (Get-Command nssm -ErrorAction SilentlyContinue)?.Source

if (!$nssmExe) {
    # 1. Repo bundle — works offline
    $bundled = Join-Path $PSScriptRoot "tools\nssm\nssm.exe"
    if (Test-Path $bundled) {
        New-Item -ItemType Directory -Force -Path "C:\nssm" | Out-Null
        Copy-Item $bundled "C:\nssm\nssm.exe" -Force
        $nssmExe = "C:\nssm\nssm.exe"
        Write-Host "  NSSM installed from repo bundle." -ForegroundColor Green
    }
}

if (!$nssmExe) {
    # 2. Common install paths
    foreach ($p in @("C:\nssm\nssm.exe","C:\nssm\win64\nssm.exe","C:\tools\nssm\nssm.exe","C:\ProgramData\chocolatey\lib\nssm\tools\nssm.exe")) {
        if (Test-Path $p) { $nssmExe = $p; break }
    }
}

if (!$nssmExe) {
    # 3. Download fallback
    Write-Host "  NSSM not found — downloading from nssm.cc..." -ForegroundColor Yellow
    try {
        $zip = "$env:TEMP\nssm.zip"
        Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile $zip -UseBasicParsing -ErrorAction Stop
        Expand-Archive $zip -DestinationPath "$env:TEMP\nssm-pkg" -Force
        $found = Get-ChildItem "$env:TEMP\nssm-pkg" -Filter "nssm.exe" -Recurse |
            Where-Object { $_.FullName -match "win64" } | Select-Object -ExpandProperty FullName -First 1
        if (!$found) { throw "nssm.exe not found in downloaded archive" }
        New-Item -ItemType Directory -Force -Path "C:\nssm" | Out-Null
        Copy-Item $found "C:\nssm\nssm.exe" -Force
        $nssmExe = "C:\nssm\nssm.exe"
        Write-Host "  NSSM downloaded to $nssmExe" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: Could not obtain NSSM: $_" -ForegroundColor Red
        Write-Host "Manual fix: copy tools\nssm\nssm.exe to C:\nssm\nssm.exe and re-run." -ForegroundColor Yellow
        exit 1
    }
}
Write-Host "  NSSM: $nssmExe" -ForegroundColor Cyan

if (!(Test-Path $svcSrc)) { Write-Host "ERROR: FaceRecognition source not found at $svcSrc" -ForegroundColor Red; exit 1 }

New-Item -ItemType Directory -Force -Path "$svcSrc\logs" | Out-Null

# Use Get-Service instead of parsing NSSM output — NSSM writes to the console
# directly so 2>&1 captures nothing reliable.
if ($null -ne (Get-Service $svcName -ErrorAction SilentlyContinue)) {
    Write-Host "$svcName already exists. Stopping and removing to re-register..." -ForegroundColor Yellow
    & $nssmExe stop   $svcName 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    & $nssmExe remove $svcName confirm 2>&1 | Out-Null
    Start-Sleep -Seconds 1
}

Write-Host "Registering $svcName..." -ForegroundColor Cyan
& $nssmExe install $svcName $python "$svcSrc\run.py"
& $nssmExe set $svcName AppDirectory   $svcSrc
& $nssmExe set $svcName AppStdout      "$svcSrc\logs\nssm-stdout.log"
& $nssmExe set $svcName AppStderr      "$svcSrc\logs\nssm-stderr.log"
& $nssmExe set $svcName AppRotateFiles 1
& $nssmExe set $svcName AppRotateSeconds 86400
& $nssmExe set $svcName Start          SERVICE_AUTO_START
& $nssmExe set $svcName Description    "PixBridge Face Recognition FastAPI service (InsightFace ArcFace)"

Write-Host "Starting $svcName..." -ForegroundColor Cyan
& $nssmExe start $svcName 2>&1 | Out-Null
Start-Sleep -Seconds 5

$svcObj = Get-Service $svcName -ErrorAction SilentlyContinue
$status  = $svcObj?.Status
Write-Host "Service status: $status" -ForegroundColor $(if ($status -eq 'Running') { 'Green' } else { 'Yellow' })

if ($status -eq 'Running') {
    try {
        $health = Invoke-RestMethod "http://localhost:8080/health" -TimeoutSec 10
        Write-Host "Health check: model_loaded=$($health.model_loaded)" -ForegroundColor Green
    } catch {
        Write-Host "Service started — model still warming up (can take ~60s on first run)." -ForegroundColor Yellow
        Write-Host "Check: Invoke-RestMethod http://localhost:8080/health" -ForegroundColor DarkGray
    }
}
