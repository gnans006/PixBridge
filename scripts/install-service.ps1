<#
.SYNOPSIS
    Installs PixBridge API and Worker as Windows Services using sc.exe.
.NOTES
    Run as Administrator.
    Writes a result log to $LogFile so the installer can detect failure.
    Performs rollback — removes both services if either fails to start.
#>

param(
    [string]$InstallDir = "C:\PixBridge",
    [string]$LogFile    = "$env:TEMP\pixbridge-setup-services.log"
)

$ErrorActionPreference = "Stop"
$installedServices = [System.Collections.Generic.List[string]]::new()

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $line = "[$(Get-Date -Format 'HH:mm:ss')] [$Level] $Message"
    $line | Tee-Object -FilePath $LogFile -Append | Write-Host -ForegroundColor $(if ($Level -eq "ERROR") { "Red" } elseif ($Level -eq "WARN") { "Yellow" } elseif ($Level -eq "OK") { "Green" } else { "Cyan" })
}

function Rollback {
    if ($installedServices.Count -eq 0) { return }
    Write-Log "Rolling back — removing partially installed services..." "WARN"
    foreach ($svc in $installedServices) {
        $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
        if ($s) {
            if ($s.Status -ne "Stopped") { & sc.exe stop $svc | Out-Null; Start-Sleep -Seconds 2 }
            & sc.exe delete $svc | Out-Null
            Write-Log "Removed service: $svc" "WARN"
        }
    }
}

function Remove-ServiceIfPresent {
    param([string]$Name)
    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service) { return }
    if ($service.Status -ne "Stopped") {
        Write-Log "Stopping existing service '$Name'..."
        & sc.exe stop $Name | Out-Null
        Start-Sleep -Seconds 3
    }
    & sc.exe delete $Name | Out-Null
    Start-Sleep -Seconds 2
    Write-Log "Removed existing service '$Name'."
}

function Install-PixBridgeService {
    param(
        [string]$Name,
        [string]$DisplayName,
        [string]$Description,
        [string]$ExecutablePath
    )

    # Pre-check: exe exists
    if (-not (Test-Path $ExecutablePath)) {
        throw "Executable not found: $ExecutablePath. The build may be incomplete."
    }

    Remove-ServiceIfPresent -Name $Name

    Write-Log "Creating service '$Name'..."
    $result = & sc.exe create $Name binPath= "`"$ExecutablePath`"" start= auto DisplayName= $DisplayName 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe create failed for '$Name'. Output: $result"
    }

    & sc.exe description $Name $Description | Out-Null

    Write-Log "Starting service '$Name'..."
    $startResult = & sc.exe start $Name 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe start failed for '$Name'. Output: $startResult"
    }

    # Wait up to 15 s for service to reach Running state
    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        $s = Get-Service -Name $Name -ErrorAction SilentlyContinue
        if ($s -and $s.Status -eq "Running") { break }
        Start-Sleep -Seconds 2
    }

    $s = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $s -or $s.Status -ne "Running") {
        # Grab last 20 lines from Windows Application event log for this service
        $events = Get-EventLog -LogName Application -Source "PixBridge*" -Newest 5 -ErrorAction SilentlyContinue |
                  Select-Object -ExpandProperty Message
        $hint = if ($events) { "`nEvent log: $($events -join ' | ')" } else { "" }
        throw "Service '$Name' failed to reach Running state (status: $($s.Status)).$hint"
    }

    $installedServices.Add($Name)
    Write-Log "Service '$Name' is Running." "OK"
}

# ── Pre-check: Admin rights ────────────────────────────────────────────────
"" | Set-Content $LogFile
Write-Log "=== PixBridge Service Installer ==="
Write-Log "Install directory: $InstallDir"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Log "This script must be run as Administrator." "ERROR"
    exit 1
}

# ── Pre-check: Port 5000 free ──────────────────────────────────────────────
$portInUse = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
if ($portInUse) {
    $proc = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
    $procName = if ($proc) { $proc.Name } else { "PID $($portInUse.OwningProcess)" }
    Write-Log "Port 5000 is already in use by '$procName'. Stop that process first." "ERROR"
    exit 1
}
Write-Log "Port 5000 is free."

# ── Install API service ────────────────────────────────────────────────────
try {
    Write-Log "Installing PixBridge API service..."
    $apiExe = Join-Path $InstallDir "api\EventPhoto.Api.exe"
    Install-PixBridgeService `
        -Name        "PixBridgeApi" `
        -DisplayName '"PixBridge API"' `
        -Description '"PixBridge Event Photo Sharing - API Server"' `
        -ExecutablePath $apiExe
} catch {
    Write-Log "API service installation failed: $_" "ERROR"
    Rollback
    exit 1
}

# ── Install Worker service ─────────────────────────────────────────────────
try {
    Write-Log "Installing PixBridge Worker service..."
    $workerExe = Join-Path $InstallDir "worker\EventPhoto.Worker.exe"
    Install-PixBridgeService `
        -Name        "PixBridgeWorker" `
        -DisplayName '"PixBridge Worker"' `
        -Description '"PixBridge - File Watcher and Thumbnail Processor"' `
        -ExecutablePath $workerExe
} catch {
    Write-Log "Worker service installation failed: $_" "ERROR"
    Rollback
    exit 1
}

Write-Log "=== Both services installed and running ===" "OK"
Write-Log "Admin panel: http://localhost:5000/admin" "OK"
Write-Log "Default credentials: admin / Admin@1234!" "OK"
