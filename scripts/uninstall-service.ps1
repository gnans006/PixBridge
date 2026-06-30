<#
.SYNOPSIS
    Stops and removes PixBridge Windows Services.
.NOTES
    Run as Administrator.
#>

$ErrorActionPreference = "Stop"

function Remove-ServiceIfPresent {
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
}

Write-Host "Stopping and removing PixBridge services..." -ForegroundColor Yellow
Remove-ServiceIfPresent -Name "PixBridgeApi"
Remove-ServiceIfPresent -Name "PixBridgeWorker"
Write-Host "PixBridge services removed." -ForegroundColor Green
