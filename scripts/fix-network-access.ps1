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
    profile=any | Out-Null
Write-Host "  ✅  Firewall rule added: port 5000 (all profiles)" -ForegroundColor Green

# 5. Get current WiFi IP (prefer the adapter we already found, skip virtual adapters)
$wifiIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias $wifi.InterfaceAlias -ErrorAction SilentlyContinue).IPAddress
if (-not $wifiIp) {
    $wifiIp = (Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object { $_.IPAddress -match '^192\.168\.' -or $_.IPAddress -match '^10\.' } |
        Where-Object { (Get-NetAdapter -InterfaceIndex $_.InterfaceIndex -ErrorAction SilentlyContinue).PhysicalMediaType -ne '' } |
        Select-Object -First 1).IPAddress
}
$publicBaseUrl = "http://$($wifiIp):5000"

Write-Host "`n  Your WiFi IP     : $wifiIp" -ForegroundColor Cyan
Write-Host "  Public Base URL  : $publicBaseUrl" -ForegroundColor Green
Write-Host "  Guest gallery    : $publicBaseUrl/gallery/<eventId>" -ForegroundColor Cyan
Write-Host "  Admin UI         : $publicBaseUrl/admin" -ForegroundColor Cyan

Write-Host "`n  ⚠️  IMPORTANT — QR codes will stay wrong until you do this:" -ForegroundColor Yellow
Write-Host "  1. Open  $publicBaseUrl/admin" -ForegroundColor White
Write-Host "  2. Go to  Platform → Network" -ForegroundColor White
Write-Host "  3. Set 'Public Base URL' to  $publicBaseUrl" -ForegroundColor White
Write-Host "  4. Save — QR codes regenerate automatically" -ForegroundColor White

Write-Host "`n━━━  Done  ━━━`n" -ForegroundColor Cyan
