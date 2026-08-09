#Requires -RunAsAdministrator
# Quick installer: registers PixBridgeFaceRecognition Windows service via NSSM.
# Run once from an ELEVATED PowerShell terminal:
#   Set-ExecutionPolicy Bypass -Scope Process -Force
#   .\install-face-service.ps1

$svcName  = "PixBridgeFaceRecognition"
$python   = "C:\Users\Gnanavel.N\AppData\Local\Programs\Python\Python311\python.exe"
$svcSrc   = "C:\CLS\PixBridge\src\PixBridge.FaceRecognition"
$nssmExe  = "C:\nssm\nssm.exe"

if (!(Test-Path $nssmExe))  { Write-Host "NSSM not found at $nssmExe" -ForegroundColor Red; exit 1 }
if (!(Test-Path $python))   { Write-Host "Python not found at $python" -ForegroundColor Red; exit 1 }
if (!(Test-Path $svcSrc))   { Write-Host "FaceRecognition source not found at $svcSrc" -ForegroundColor Red; exit 1 }

New-Item -ItemType Directory -Force -Path "$svcSrc\logs" | Out-Null

$existing = & $nssmExe status $svcName 2>&1
if ($existing -match "SERVICE_STOPPED|SERVICE_RUNNING") {
    Write-Host "$svcName already registered (status: $existing). Stopping and removing..." -ForegroundColor Yellow
    & $nssmExe stop $svcName 2>&1 | Out-Null
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

$status = & $nssmExe status $svcName 2>&1
Write-Host "Service status: $status" -ForegroundColor $(if ($status -match "RUNNING") { "Green" } else { "Yellow" })

if ($status -match "RUNNING") {
    try {
        $health = Invoke-RestMethod "http://localhost:8080/health" -TimeoutSec 10
        Write-Host "Health check: model_loaded=$($health.model_loaded)" -ForegroundColor Green
    } catch {
        Write-Host "Service started — model still warming up (can take ~60s on first run)." -ForegroundColor Yellow
        Write-Host "Check: Invoke-RestMethod http://localhost:8080/health" -ForegroundColor DarkGray
    }
}
