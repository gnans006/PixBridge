#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stops all running PixBridge services (API, Worker, React dev server).

.USAGE
    .\scripts\dev-stop.ps1
    .\scripts\dev-stop.ps1 -ApiOnly      # stop API + Worker only
    .\scripts\dev-stop.ps1 -ReactOnly    # stop React dev server only
#>

param(
    [switch]$ApiOnly,
    [switch]$ReactOnly
)

$ErrorActionPreference = 'SilentlyContinue'

Write-Host "`n━━━  PixBridge Stop  ━━━" -ForegroundColor Magenta

if (-not $ReactOnly) {
    # Stop API and Worker (dotnet processes)
    foreach ($name in @('EventPhoto.Api', 'EventPhoto.Worker')) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            Write-Host "  Stopping $name (PID $($procs.Id -join ', '))..." -ForegroundColor Yellow
            $procs | Stop-Process -Force
        } else {
            Write-Host "  $name — not running." -ForegroundColor DarkGray
        }
    }

    # Also catch any dotnet processes with PixBridge titles
    $dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcs) {
        Write-Host "  Stopping $($dotnetProcs.Count) remaining dotnet process(es)..." -ForegroundColor Yellow
        $dotnetProcs | Stop-Process -Force
    }
    Start-Sleep -Milliseconds 600
}

if (-not $ApiOnly) {
    # Stop Vite dev server on :5173
    $viteProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
        $null -ne (Get-NetTCPConnection -OwningProcess $_.Id -LocalPort 5173 -ErrorAction SilentlyContinue)
    }
    if ($viteProcs) {
        Write-Host "  Stopping React dev server (PID $($viteProcs.Id -join ', '))..." -ForegroundColor Yellow
        $viteProcs | Stop-Process -Force
    } else {
        Write-Host "  React dev server — not running on :5173." -ForegroundColor DarkGray
    }
}

Write-Host "`n✔  All PixBridge services stopped." -ForegroundColor Green
