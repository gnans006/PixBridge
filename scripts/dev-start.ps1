#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts PixBridge in development mode.
    Opens three separate terminal windows:
      - EventPhoto.Api   → http://localhost:5000  (+ http://0.0.0.0:5000 for LAN)
      - EventPhoto.Worker
      - React Vite dev   → http://localhost:5173

.USAGE
    .\scripts\dev-start.ps1
    .\scripts\dev-start.ps1 -NoWorker     # skip the Worker process
    .\scripts\dev-start.ps1 -NoReact      # skip the React dev server
    .\scripts\dev-start.ps1 -ApiOnly      # API only
#>

param(
    [switch]$NoWorker,
    [switch]$NoReact,
    [switch]$ApiOnly
)

$ErrorActionPreference = 'Stop'
$Root       = Split-Path $PSScriptRoot -Parent
$ApiProj    = Join-Path $Root "src\EventPhoto.Api\EventPhoto.Api.csproj"
$WorkerProj = Join-Path $Root "src\EventPhoto.Worker\EventPhoto.Worker.csproj"
$ReactDir   = Join-Path $Root "src\EventPhoto.React"

function Write-Step([string]$msg) {
    Write-Host "`n━━━  $msg  ━━━" -ForegroundColor Cyan
}

Write-Host "`n━━━  PixBridge Dev Start  ━━━" -ForegroundColor Magenta

# ── Pre-flight: stop anything already running ─────────────────────────────────
Write-Step "Stopping any existing processes"
foreach ($name in @('EventPhoto.Api', 'EventPhoto.Worker')) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "  Stopping $name..." -ForegroundColor Yellow
        $procs | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}
# Kill Vite dev server on :5173 if running
$viteProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $null -ne (Get-NetTCPConnection -OwningProcess $_.Id -LocalPort 5173 -ErrorAction SilentlyContinue)
}
if ($viteProcs) {
    Write-Host "  Stopping Vite dev server..." -ForegroundColor Yellow
    $viteProcs | Stop-Process -Force
    Start-Sleep -Milliseconds 400
}

# ── Start API ─────────────────────────────────────────────────────────────────
Write-Step "Starting API → http://localhost:5000"
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "Write-Host 'PixBridge API' -ForegroundColor Cyan; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project `"$ApiProj`"" `
    -WindowStyle Normal
Write-Host "  API window opened." -ForegroundColor Green

# ── Start Worker ──────────────────────────────────────────────────────────────
if (-not $NoWorker -and -not $ApiOnly) {
    Write-Step "Starting Worker"
    Start-Sleep -Milliseconds 1000   # brief delay so API binds first
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge Worker' -ForegroundColor Yellow; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project `"$WorkerProj`"" `
        -WindowStyle Normal
    Write-Host "  Worker window opened." -ForegroundColor Green
}

# ── Start React ───────────────────────────────────────────────────────────────
if (-not $NoReact -and -not $ApiOnly) {
    Write-Step "Starting React dev server → http://localhost:5173"
    Start-Sleep -Milliseconds 500
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge React (Vite)' -ForegroundColor Magenta; Set-Location `"$ReactDir`"; npm run dev" `
        -WindowStyle Normal
    Write-Host "  React window opened." -ForegroundColor Green
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host @"

━━━  All services starting  ━━━

  API          → http://localhost:5000
  Swagger      → http://localhost:5000/swagger
  React (dev)  → http://localhost:5173

  LAN guests   → http://<your-ip>:5000  (auto-detected on API startup)

  Use  .\scripts\dev-stop.ps1  to stop everything.

"@ -ForegroundColor Green
