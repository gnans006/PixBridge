<#
.SYNOPSIS
    Installs PixBridge API and Worker as Windows Services using sc.exe.
.NOTES
    Run as Administrator.
#>

param(
    [string]$InstallDir = "C:\PixBridge"
)

$ErrorActionPreference = "Stop"

function Ensure-ServiceAbsent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service) {
        return
    }

    if ($service.Status -ne "Stopped") {
        & sc.exe stop $Name | Out-Null
        Start-Sleep -Seconds 2
    }

    & sc.exe delete $Name | Out-Null
    Start-Sleep -Seconds 2
}

function Install-Service {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        [Parameter(Mandatory = $true)]
        [string]$Description,
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath
    )

    if (-not (Test-Path $ExecutablePath)) {
        throw "Executable not found: $ExecutablePath"
    }

    Ensure-ServiceAbsent -Name $Name

    & sc.exe create $Name binPath= "`"$ExecutablePath`"" start= auto DisplayName= $DisplayName | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create service '$Name'."
    }

    & sc.exe description $Name $Description | Out-Null
    & sc.exe start $Name | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start service '$Name'."
    }
}

Write-Host "Installing PixBridge API service..." -ForegroundColor Cyan
$apiExe = Join-Path $InstallDir "api\EventPhoto.Api.exe"
Install-Service `
    -Name "PixBridgeApi" `
    -DisplayName '"PixBridge API"' `
    -Description '"PixBridge Event Photo Sharing Platform - API Server"' `
    -ExecutablePath $apiExe
Write-Host "PixBridgeApi service installed and started." -ForegroundColor Green

Write-Host "Installing PixBridge Worker service..." -ForegroundColor Cyan
$workerExe = Join-Path $InstallDir "worker\EventPhoto.Worker.exe"
Install-Service `
    -Name "PixBridgeWorker" `
    -DisplayName '"PixBridge Worker"' `
    -Description '"PixBridge - File Watcher and Thumbnail Processor"' `
    -ExecutablePath $workerExe
Write-Host "PixBridgeWorker service installed and started." -ForegroundColor Green

Write-Host "`nPixBridge services installed successfully!" -ForegroundColor Green
Write-Host "Access the admin panel at: http://192.168.10.10/admin"
Write-Host "Default credentials: admin / Admin@1234!"
