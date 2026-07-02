# PixBridge — Stop all running processes, rebuild, and relaunch API + Worker
# Usage: .\restart.ps1
#        .\restart.ps1 -ApiOnly
#        .\restart.ps1 -WorkerOnly

param(
    [switch]$ApiOnly,
    [switch]$WorkerOnly
)

$root = $PSScriptRoot

function Stop-AllEventPhotoProcesses {
    Write-Host "⏹  Stopping all EventPhoto processes..." -ForegroundColor Yellow
    Get-Process | Where-Object { $_.Name -match "^EventPhoto" } | ForEach-Object {
        Write-Host "   Killing $($_.Name) (PID $($_.Id))"
        $_ | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Milliseconds 800
    Write-Host "   Done." -ForegroundColor Green
}

function Start-Api {
    Write-Host "`n▶  Starting API..." -ForegroundColor Cyan
    $apiProj = Join-Path $root "src\EventPhoto.Api\EventPhoto.Api.csproj"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$apiProj`"" -WindowStyle Normal
    Write-Host "   API window opened." -ForegroundColor Green
}

function Start-Worker {
    Write-Host "`n▶  Starting Worker..." -ForegroundColor Cyan
    $workerProj = Join-Path $root "src\EventPhoto.Worker\EventPhoto.Worker.csproj"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$workerProj`"" -WindowStyle Normal
    Write-Host "   Worker window opened." -ForegroundColor Green
}

Stop-AllEventPhotoProcesses

if ($WorkerOnly) {
    Start-Worker
} elseif ($ApiOnly) {
    Start-Api
} else {
    Start-Api
    Start-Sleep -Seconds 2
    Start-Worker
}

Write-Host "`n✅  Done. Check the opened windows for startup logs." -ForegroundColor Green
Write-Host "   API health: http://localhost:5000/api/health" -ForegroundColor DarkGray
