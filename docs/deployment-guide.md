# PixBridge Deployment Guide

## Production Deployment (Windows)

### Step 1: Prerequisites

1. Install **PostgreSQL 15+**
   - Download: https://www.postgresql.org/download/windows/
   - Add PostgreSQL `bin` folder to the system PATH (for example, `C:\Program Files\PostgreSQL\15\bin`)

2. Set laptop **static IP**: `192.168.10.10`
   - Network Settings → Wi-Fi Adapter → IPv4 → Manual
   - IP: 192.168.10.10, Subnet: 255.255.255.0, Gateway: 192.168.10.1

3. Allow port 80 through **Windows Firewall**:
   ```powershell
   New-NetFirewallRule -DisplayName "PixBridge HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
   ```

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
Invoke-WebRequest http://localhost/api/health
Start-Process "http://localhost/admin"
```

### Step 5: First Login

1. Open `http://192.168.10.10/admin`
2. Login with `admin` / `Admin@1234!`
3. Change the password immediately
4. Create your first event

## Wi-Fi Router Setup

1. Connect the laptop to the router via Ethernet or Wi-Fi
2. Set the laptop IP to **192.168.10.10** (static)
3. Connect the router to the event Wi-Fi network (no internet needed)
4. Print or display the event QR code for guests

Guests:
1. Connect to the event Wi-Fi
2. Scan the QR code or browse to `http://192.168.10.10`
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
