# PixBridge — Detect, stop, and restart any combination of API / Worker / React / Face Recognition
# Usage: .\restart.ps1                  → stop & restart everything
#        .\restart.ps1 -ApiOnly         → stop & restart API only
#        .\restart.ps1 -WorkerOnly      → stop & restart Worker only
#        .\restart.ps1 -ReactOnly       → stop & restart React dev server only
#        .\restart.ps1 -FaceOnly        → stop & restart Face Recognition service only
#        .\restart.ps1 -NoReact         → restart API + Worker + Face, leave React alone
#        .\restart.ps1 -NoFace          → restart API + Worker + React, leave Face alone

param(
    [switch]$ApiOnly,
    [switch]$WorkerOnly,
    [switch]$ReactOnly,
    [switch]$FaceOnly,
    [switch]$NoReact,
    [switch]$NoFace
)

$root = $PSScriptRoot
$reactDir = Join-Path $root "src\EventPhoto.React"
$apiProj   = Join-Path $root "src\EventPhoto.Api\EventPhoto.Api.csproj"
$workerProj = Join-Path $root "src\EventPhoto.Worker\EventPhoto.Worker.csproj"

# ──────────────────────────────────────────────
# Helpers
# ──────────────────────────────────────────────

function Get-RunningDotnetServices {
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match "EventPhoto" -or $_.MainWindowTitle -match "EventPhoto" }
}

function Test-ApiRunning   { $null -ne (Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue) }
function Test-ReactRunning { $null -ne (Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue) }
function Test-FaceRunning  { $null -ne (Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue) }

function Show-ServiceStatus {
    $apiStatus    = if (Test-ApiRunning)   { "RUNNING :5000" } else { "stopped" }
    $reactStatus  = if (Test-ReactRunning) { "RUNNING :5173" } else { "stopped" }
    $faceSvc      = Get-Service "PixBridgeFaceRecognition" -ErrorAction SilentlyContinue
    $faceStatus   = if ($faceSvc -and $faceSvc.Status -eq 'Running') { "RUNNING :8080" } else { "stopped" }
    $dotnetCount  = @(Get-Process -Name "dotnet" -ErrorAction SilentlyContinue).Count
    $nodeCount    = @(Get-Process -Name "node"   -ErrorAction SilentlyContinue).Count

    Write-Host "`n  Current state:" -ForegroundColor DarkGray
    Write-Host "    dotnet processes : $dotnetCount  (API=$apiStatus)" -ForegroundColor DarkGray
    Write-Host "    node   processes : $nodeCount    (React=$reactStatus)" -ForegroundColor DarkGray
    Write-Host "    face recognition : $faceStatus" -ForegroundColor DarkGray
}

# ──────────────────────────────────────────────
# Stop functions
# ──────────────────────────────────────────────

function Stop-DotnetServices {
    $procs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "  Stopping $($procs.Count) dotnet process(es)..." -ForegroundColor Yellow
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 800
        Write-Host "  Dotnet processes stopped." -ForegroundColor Green
    } else {
        Write-Host "  No dotnet processes running." -ForegroundColor DarkGray
    }
}

function Stop-ReactServer {
    # Kill node processes that are listening on port 5173 (Vite dev server)
    $viteProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue |
        Where-Object {
            $conn = Get-NetTCPConnection -OwningProcess $_.Id -LocalPort 5173 -ErrorAction SilentlyContinue
            $null -ne $conn
        }

    if ($viteProcs) {
        Write-Host "  Stopping Vite dev server (PID $($viteProcs.Id -join ', '))..." -ForegroundColor Yellow
        $viteProcs | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
        Write-Host "  React dev server stopped." -ForegroundColor Green
    } else {
        Write-Host "  React dev server not running on :5173." -ForegroundColor DarkGray
    }
}

function Stop-FaceService {
    $svc = Get-Service "PixBridgeFaceRecognition" -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq 'Running') {
        Write-Host "  Stopping Face Recognition service..." -ForegroundColor Yellow
        Stop-Service "PixBridgeFaceRecognition" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 1500
        Write-Host "  Face Recognition service stopped." -ForegroundColor Green
    } else {
        Write-Host "  Face Recognition service not running." -ForegroundColor DarkGray
    }
}

# ──────────────────────────────────────────────
# Start functions
# ──────────────────────────────────────────────

function Start-FaceService {
    $svc = Get-Service "PixBridgeFaceRecognition" -ErrorAction SilentlyContinue
    if ($null -eq $svc) {
        Write-Host "  PixBridgeFaceRecognition service not installed." -ForegroundColor Red
        Write-Host "  Run: .\scripts\setup-face-search.ps1  to install it." -ForegroundColor Red
        return
    }
    Write-Host "  Starting Face Recognition service  →  http://localhost:8080" -ForegroundColor Cyan
    Start-Service "PixBridgeFaceRecognition" -ErrorAction SilentlyContinue
    # Give it a few seconds then check health (model loading can take ~30s on first boot)
    Start-Sleep -Seconds 4
    try {
        $health = Invoke-RestMethod "http://localhost:8080/health" -TimeoutSec 5 -ErrorAction Stop
        if ($health.model_loaded) {
            Write-Host "  Face Recognition ready (model loaded)." -ForegroundColor Green
        } else {
            Write-Host "  Face Recognition started — model still warming up." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  Face Recognition started — warming up (model load takes ~30s)." -ForegroundColor Yellow
        Write-Host "  Check: Invoke-RestMethod http://localhost:8080/health" -ForegroundColor DarkGray
    }
}

function Start-Api {
    Write-Host "  Starting API  →  http://localhost:5000" -ForegroundColor Cyan
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge API' -ForegroundColor Cyan; dotnet run --project `"$apiProj`" --environment Development" `
        -WindowStyle Normal
    Write-Host "  API window opened." -ForegroundColor Green
}

function Start-Worker {
    Write-Host "  Starting Worker..." -ForegroundColor Cyan
    $workerDir = Split-Path $workerProj -Parent
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge Worker' -ForegroundColor Cyan; Set-Location `"$workerDir`"; dotnet run --project `"$workerProj`" --environment Development" `
        -WindowStyle Normal
    Write-Host "  Worker window opened." -ForegroundColor Green
}

function Start-React {
    Write-Host "  Starting React  →  http://localhost:5173" -ForegroundColor Cyan
    Start-Process powershell -ArgumentList "-NoExit", "-Command",
        "Write-Host 'PixBridge React' -ForegroundColor Cyan; Set-Location `"$reactDir`"; npm run dev" `
        -WindowStyle Normal
    Write-Host "  React window opened." -ForegroundColor Green
}

# ──────────────────────────────────────────────
# Main
# ──────────────────────────────────────────────

Write-Host "`n━━━  PixBridge Restart  ━━━" -ForegroundColor Magenta
Show-ServiceStatus

if ($ReactOnly) {
    Write-Host "`n[React only]" -ForegroundColor Magenta
    Stop-ReactServer
    Start-React

} elseif ($ApiOnly) {
    Write-Host "`n[API only]" -ForegroundColor Magenta
    Stop-DotnetServices
    Start-Api

} elseif ($WorkerOnly) {
    Write-Host "`n[Worker only]" -ForegroundColor Magenta
    Stop-DotnetServices
    Start-Worker

} elseif ($FaceOnly) {
    Write-Host "`n[Face Recognition only]" -ForegroundColor Magenta
    Stop-FaceService
    Start-FaceService

} elseif ($NoReact) {
    Write-Host "`n[API + Worker + Face, React unchanged]" -ForegroundColor Magenta
    Stop-DotnetServices
    Stop-FaceService
    Start-FaceService
    Start-Api
    Start-Sleep -Seconds 2
    Start-Worker

} elseif ($NoFace) {
    Write-Host "`n[API + Worker + React, Face unchanged]" -ForegroundColor Magenta
    Stop-DotnetServices
    Stop-ReactServer
    Start-Api
    Start-Sleep -Seconds 2
    Start-Worker
    Start-Sleep -Seconds 1
    Start-React

} else {
    Write-Host "`n[Full restart: API + Worker + React + Face Recognition]" -ForegroundColor Magenta
    Stop-DotnetServices
    Stop-ReactServer
    Stop-FaceService
    Start-FaceService
    Start-Api
    Start-Sleep -Seconds 2
    Start-Worker
    Start-Sleep -Seconds 1
    Start-React
}

Write-Host "`n✅  Done. Services are starting in separate windows." -ForegroundColor Green
Write-Host "   API health  : http://localhost:5000/api/health" -ForegroundColor DarkGray
Write-Host "   React UI    : http://localhost:5173" -ForegroundColor DarkGray
Write-Host "   Face health : http://localhost:8080/health" -ForegroundColor DarkGray
