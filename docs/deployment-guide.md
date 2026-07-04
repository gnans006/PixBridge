# PixBridge Deployment Guide

## Production Deployment (Windows)

### Step 1: Prerequisites

1. Install **PostgreSQL 15+**
   - Download: https://www.postgresql.org/download/windows/
   - Add PostgreSQL `bin` folder to the system PATH (for example, `C:\Program Files\PostgreSQL\15\bin`)

2. Ensure the laptop Wi-Fi profile is set to **Private**:
   - Windows Settings → Network & Internet → Wi-Fi → click the connected network name → set **Network profile** to **Private**

3. Allow port **5000** through **Windows Firewall** (run as Administrator):
   ```powershell
   New-NetFirewallRule -DisplayName "PixBridge API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
   ```

> **No static IP required.** PixBridge auto-detects the LAN IP on every startup, updates the `app.serverUrl` database setting, and regenerates all QR codes.

### Step 2: Build Release

```powershell
cd C:\CLS\PixBridge
.\scripts\build-release.ps1 -Version "1.0.0"
```

Output:
- `publish\api\` — API server files
- `publish\worker\` — Worker service files
- `publish\installer\` — installer output after compiling `scripts\installer.iss`

### Step 3: Deploy

Either use the Inno Setup installer (recommended) or manual deployment.

**Manual deployment:**
```powershell
New-Item -ItemType Directory -Force -Path C:\PixBridge | Out-Null
Copy-Item -Recurse -Force publish\api C:\PixBridge\api
Copy-Item -Recurse -Force publish\worker C:\PixBridge\worker
Copy-Item -Recurse -Force scripts C:\PixBridge\scripts

powershell -ExecutionPolicy Bypass -File C:\PixBridge\scripts\setup-postgresql.ps1
powershell -ExecutionPolicy Bypass -File C:\PixBridge\scripts\install-service.ps1 -InstallDir C:\PixBridge
```

### Step 4: Verify

```powershell
Get-Service PixBridgeApi, PixBridgeWorker
Invoke-WebRequest http://localhost:5000/api/health
Start-Process "http://localhost:5000/admin"
```

### Step 5: First Login

1. Open `http://localhost:5000/admin`
2. Login with `admin` / `Admin@1234!`
3. Change the password immediately
4. Create your first event

## Wi-Fi Setup for Guests

1. Connect the laptop to the event Wi-Fi router (no internet required)
2. Ensure the Wi-Fi profile is set to **Private** (see Step 1)
3. Start PixBridge — the LAN IP is auto-detected; all QR codes are updated automatically
4. Print or display the event QR code for guests

Guests:
1. Connect to the event Wi-Fi
2. Scan the QR code — it links to `http://<laptop-LAN-IP>:5000/gallery/<event-id>`
3. Browse and download photos

## Building the Installer

1. Install **Inno Setup 6.x**
2. Open `scripts\installer.iss`
3. Build the installer in Inno Setup Compiler
4. Collect the generated `.exe` from `publish\installer\`

## EF Core Migrations (Development)

```powershell
cd C:\CLS\PixBridge

dotnet ef migrations add <MigrationName> --project src\EventPhoto.Infrastructure --startup-project src\EventPhoto.Api
dotnet ef database update --project src\EventPhoto.Infrastructure --startup-project src\EventPhoto.Api
```
