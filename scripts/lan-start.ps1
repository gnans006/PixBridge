#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds the React frontend and starts the API + Worker for LAN/event mode.
    Guests connect via http://<server-ip>:5000 — no Vite dev server needed.

.USAGE
    .\scripts\lan-start.ps1
    .\scripts\lan-start.ps1 -SkipBuild    # skip React build (use existing wwwroot)
    .\scripts\lan-start.ps1 -NoWorker     # start API only (no Worker)
#>

param(
    [switch]$SkipBuild,
    [switch]$NoWorker
)

$ErrorActionPreference = 'Stop'
$Root       = Split-Path $PSScriptRoot -Parent
$ReactDir   = Join-Path $Root "src\EventPhoto.React"
$ApiProj    = Join-Path $Root "src\EventPhoto.Api\EventPhoto.Api.csproj"
$WorkerProj = Join-Path $Root "src\EventPhoto.Worker\EventPhoto.Worker.csproj"
$Wwwroot    = Join-Path $Root "src\EventPhoto.Api\wwwroot"

function Write-Step([string]$msg) {
    Write-Host "`n━━━  $msg  ━━━" -ForegroundColor Cyan
}

Write-Host "`n━━━  PixBridge LAN Start (Event Mode)  ━━━" -ForegroundColor Magenta

# ── Stop any running processes ────────────────────────────────────────────────
Write-Step "Stopping existing processes"
foreach ($name in @('EventPhoto.Api', 'EventPhoto.Worker')) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "  Stopping $name..." -ForegroundColor Yellow
        $procs | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}
# Stop Vite dev server if running (not needed in LAN mode)
$viteProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $null -ne (Get-NetTCPConnection -OwningProcess $_.Id -LocalPort 5173 -ErrorAction SilentlyContinue)
}
if ($viteProcs) {
    Write-Host "  Stopping Vite dev server (not needed in LAN mode)..." -ForegroundColor Yellow
    $viteProcs | Stop-Process -Force
    Start-Sleep -Milliseconds 400
}

# ── Build React into wwwroot ──────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Step "Building React frontend → wwwroot"
    Push-Location $ReactDir
    npm run build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  React build failed." -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host "  React build complete → $Wwwroot" -ForegroundColor Green
} else {
    if (-not (Test-Path (Join-Path $Wwwroot "index.html"))) {
        Write-Host "  WARNING: wwwroot/index.html not found. Run without -SkipBuild first." -ForegroundColor Yellow
    } else {
        Write-Host "  [Skipped] React build — using existing wwwroot." -ForegroundColor DarkGray
    }
}

# ── Detect LAN IP for display ─────────────────────────────────────────────────
$lanIp = (
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
    Select-Object -First 1
).IPAddress ?? "192.168.x.x"

# ── Start API ─────────────────────────────────────────────────────────────────
Write-Step "Starting API → http://0.0.0.0:5000"
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "Write-Host 'PixBridge API [LAN MODE]' -ForegroundColor Green; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project `"$ApiProj`"" `
    -WindowStyle Normal
Write-Host "  API window opened." -ForegroundColor Green

# ── Start Worker ──────────────────────────────────────────────────────────────
if (-not $NoWorker) {
    Write-Step "Starting Worker"
    Start-Sleep -Milliseconds 1500    # wait for API to bind its port first
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge Worker [LAN MODE]' -ForegroundColor Green; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project `"$WorkerProj`"" `
        -WindowStyle Normal
    Write-Host "  Worker window opened." -ForegroundColor Green
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host @"

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  PixBridge is starting in LAN / Event Mode
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Local access  → http://localhost:5000
  LAN access    → http://${lanIp}:5000

  Guests on the same WiFi can open:
    http://${lanIp}:5000
  or scan a QR code from any event.

  API auto-detects the LAN IP and updates all QR codes on startup.

  To stop:  .\scripts\dev-stop.ps1

"@ -ForegroundColor Green

# ── Optionally open browser (admin Swagger) ───────────────────────────────────
Write-Host "  Opening Swagger in 4 seconds..." -ForegroundColor DarkGray
Start-Sleep -Seconds 4
Start-Process "http://localhost:5000/swagger"
