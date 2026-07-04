#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Configures Windows to allow mobile devices on the same WiFi to reach PixBridge.
    Must be run as Administrator.
#>

$ErrorActionPreference = 'Stop'

Write-Host "━━━  PixBridge Network Fix  ━━━" -ForegroundColor Cyan

# 1. Find the WiFi adapter
$wifi = Get-NetConnectionProfile | Where-Object { $_.InterfaceAlias -like '*Wi*' -or $_.InterfaceAlias -like '*WiFi*' } | Select-Object -First 1

if (-not $wifi) {
    Write-Host "  [!] No WiFi adapter found. Are you connected to WiFi?" -ForegroundColor Red
    exit 1
}

Write-Host "`n  WiFi adapter  : $($wifi.InterfaceAlias)"
Write-Host "  Current profile: $($wifi.NetworkCategory)"

# 2. Switch from Public → Private so inbound rules are honoured
if ($wifi.NetworkCategory -eq 'Public') {
    Write-Host "`n  Switching WiFi from Public → Private..." -ForegroundColor Yellow
    Set-NetConnectionProfile -InterfaceAlias $wifi.InterfaceAlias -NetworkCategory Private
    Write-Host "  ✅  WiFi is now Private" -ForegroundColor Green
} else {
    Write-Host "  ✅  WiFi already Private – no change needed" -ForegroundColor Green
}

# 3. Remove any stale PixBridge rules first
@("PixBridge API", "PixBridge React Dev") | ForEach-Object {
    $existing = netsh advfirewall firewall show rule name=$_ 2>&1
    if ($existing -notmatch "No rules match") {
        netsh advfirewall firewall delete rule name=$_ | Out-Null
        Write-Host "  Removed old rule: $_" -ForegroundColor DarkGray
    }
}

# 4. Open port 5000 (API + built React SPA) on Private profile
netsh advfirewall firewall add rule `
    name="PixBridge API" `
    dir=in action=allow protocol=TCP localport=5000 `
    profile=private | Out-Null
Write-Host "  ✅  Firewall rule added: port 5000 (Private)" -ForegroundColor Green

# 5. Get current WiFi IP
$lanIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias $wifi.InterfaceAlias).IPAddress
Write-Host "`n  Your WiFi IP   : $lanIp" -ForegroundColor Cyan
Write-Host "  Guest gallery  : http://$($lanIp):5000/gallery/<eventId>" -ForegroundColor Cyan
Write-Host "  Admin UI       : http://$($lanIp):5000" -ForegroundColor Cyan

Write-Host "`n  NEXT STEPS:"
Write-Host "  1. In Settings → set app.serverUrl to  http://$($lanIp):5000"
Write-Host "  2. Click Refresh QR on each event"
Write-Host "  3. Scan QR from phone (same WiFi)" -ForegroundColor Green

Write-Host "`n━━━  Done  ━━━`n" -ForegroundColor Cyan
