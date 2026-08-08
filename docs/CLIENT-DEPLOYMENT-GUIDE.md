# PixBridge — Client Deployment Guide

> **Audience:** This guide covers everything from preparing the installer on the developer's machine to a fully working PixBridge installation on the client's machine, including face search setup.

---

## Table of Contents

1. [Part A — Developer: Build the Installer](#part-a--developer-build-the-installer)
2. [Part B — Client Machine Prerequisites](#part-b--client-machine-prerequisites)
   - [System Requirements](#21-system-requirements)
   - [Install PostgreSQL 17](#22-install-postgresql-17)
   - [Install Python 3.11 (Face Search)](#23-install-python-311-face-search)
   - [Install NSSM (Face Search Service)](#24-install-nssm-face-search-service)
3. [Part C — Run the PixBridge Installer](#part-c--run-the-pixbridge-installer)
4. [Part D — Face Search Setup](#part-d--face-search-setup)
5. [Part E — Network & Wi-Fi Setup for Guests](#part-e--network--wi-fi-setup-for-guests)
6. [Part F — First Login & Initial Configuration](#part-f--first-login--initial-configuration)
7. [Part G — Verification Checklist](#part-g--verification-checklist)
8. [Part H — Troubleshooting & Repair](#part-h--troubleshooting--repair)

---

## Part A — Developer: Build the Installer

> Run these steps **once on the developer's machine** to produce the `PixBridge-Setup-1.0.0.exe` file that you hand to the client.

### A.1 Prerequisites on the Developer's Machine

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0 or 9.0 | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Node.js | 22 LTS (recommended) | https://nodejs.org/en/download/ |
| Git | any | https://git-scm.com/download/win |

Inno Setup (needed to compile the `.exe` installer) is **downloaded and installed automatically** by the build script if internet access is available.

### A.2 Run the Build Script

Open **PowerShell as Administrator** and run:

```powershell
cd C:\CLS\PixBridge
.\scripts\build-release.ps1 -Version "1.0.0"
```

The script will:
1. Verify .NET SDK and Node.js are present
2. Check `appsettings.Production.json` for a valid JWT secret
3. Build the React frontend (`npm ci` + `npm run build`)
4. Publish API and Worker as self-contained Windows executables
5. Auto-install Inno Setup 6 if not present (requires internet)
6. Compile `installer.iss` → `publish\installer\PixBridge-Setup-1.0.0.exe`

**Output file to give the client:**
```
C:\CLS\PixBridge\publish\installer\PixBridge-Setup-1.0.0.exe
```

---

## Part B — Client Machine Prerequisites

> These steps must be completed **on the client's machine** before running the PixBridge installer. Do them in order.

### 2.1 System Requirements

| Requirement | Minimum | Recommended |
|-------------|---------|-------------|
| OS | Windows 10 (1809+) | Windows 11 22H2+ |
| RAM | 4 GB | 8 GB |
| Disk Space | 5 GB free | 10 GB free (for photos) |
| CPU | 64-bit, 2 cores | 4+ cores (for face search) |
| Network | Wi-Fi adapter | Wi-Fi adapter |

> **Note:** No internet connection is required during events. The machine only needs LAN connectivity.

---

### 2.2 Install PostgreSQL 17

PostgreSQL 17 is recommended because the face search feature requires the **pgvector** extension, which is easiest to build and install on PostgreSQL 17.

**Step 1:** Download the installer from the official source:

```
https://www.enterprisedb.com/downloads/postgres-postgresql-downloads
```

Select **Version 17.x** → **Windows x86-64**.

**Step 2:** Run the installer. During setup:

| Installer Screen | What to select / enter |
|-----------------|----------------------|
| Components | ✅ PostgreSQL Server ✅ Command Line Tools (required) — pgAdmin is optional |
| Data Directory | Leave as default (`C:\Program Files\PostgreSQL\17\data`) |
| Password | Set a password for the `postgres` superuser. **Write this down** — you will need it during PixBridge setup. |
| Port | `5432` (default — do not change) |
| Locale | Default locale |

**Step 3:** Add PostgreSQL `bin` to the System PATH.

1. Press `Win + S` → search **"Edit the system environment variables"** → open it
2. Click **Environment Variables**
3. Under **System variables**, select `Path` → click **Edit**
4. Click **New** → add: `C:\Program Files\PostgreSQL\17\bin`
5. Click **OK** on all windows
6. **Restart PowerShell** (or reboot) for PATH to take effect

**Step 4:** Verify the installation:

```powershell
psql --version
# Expected output: psql (PostgreSQL) 17.x
```

> **Important:** Do NOT create the PixBridge database manually. The PixBridge installer creates the database, user, and tables automatically.

---

### 2.3 Install Python 3.11 (Face Search)

Python is required only if you want the **face search feature** (finding guest photos by selfie). If face search is not needed, skip this section.

**Step 1:** Download Python 3.11:

```
https://www.python.org/downloads/release/python-3119/
```

Select **Windows installer (64-bit)**.

**Step 2:** Run the installer. **Important options:**

- ✅ **Check** "Add Python 3.11 to PATH" (on the first screen — do this before clicking Install Now)
- Click **Install Now** (or Customize if you want to change the install folder)

**Step 3:** Verify:

```powershell
python --version
# Expected: Python 3.11.x

pip --version
# Expected: pip 24.x from ... (python 3.11)
```

---

### 2.4 Install NSSM (Face Search Service)

NSSM (Non-Sucking Service Manager) is used to run the Python face recognition service as a Windows background service. Required only if face search is enabled.

**Step 1:** Download NSSM:

```
https://nssm.cc/download
```

Download the latest release ZIP (e.g., `nssm-2.24.zip`).

**Step 2:** Extract the ZIP. Inside it you'll find:
```
nssm-2.24\
  win64\
    nssm.exe    ← this is the one to use on 64-bit Windows
```

**Step 3:** Copy `nssm.exe` to a permanent location:

```powershell
# Run as Administrator
New-Item -ItemType Directory -Force -Path "C:\Tools" | Out-Null
Copy-Item "C:\Downloads\nssm-2.24\win64\nssm.exe" "C:\Tools\nssm.exe"
```

**Step 4:** Add `C:\Tools` to System PATH (same way as PostgreSQL above).

**Step 5:** Verify:

```powershell
nssm version
# Expected: NSSM 2.24 (...)
```

---

## Part C — Run the PixBridge Installer

### C.1 Run the Installer

1. Copy `PixBridge-Setup-1.0.0.exe` to the client machine (USB drive, shared folder, etc.)
2. **Right-click** → **Run as Administrator** (this is required)
3. Follow the installer wizard:

| Wizard Screen | What to do |
|--------------|-----------|
| Welcome | Click **Next** |
| License | Accept → **Next** |
| Install Location | Default: `C:\Program Files\PixBridge` — change if needed |
| Select Tasks | Leave all 3 tasks checked (recommended): |
| | ✅ Install PixBridge as Windows Services (auto-start) |
| | ✅ Configure firewall and set Wi-Fi profile to Private |
| | ✅ Create a desktop shortcut to the Admin Panel |
| Ready to Install | Click **Install** |

### C.2 What the Installer Does Automatically

During installation, the following happens (shown in the progress bar):

1. **Copies all files** to the install directory (`C:\Program Files\PixBridge\`)
2. **Sets up PostgreSQL:**
   - Creates database: `pixbridge`
   - Creates database user: `pixbridge` / password: `pixbridge123`
   - Runs all database migrations (tables, indexes, seed data)
3. **Configures network:**
   - Adds Windows Firewall rule for port `5000` (API)
   - Sets the connected Wi-Fi profile to **Private** (required for LAN access)
4. **Installs Windows Services:**
   - `PixBridgeApi` — the API server (starts on port `5000`)
   - `PixBridgeWorker` — background job processor
   - Both are set to **auto-start** when Windows boots

### C.3 After Installation Completes

The installer shows a summary dialog with:
- **Admin URL:** `http://localhost:5000/admin`
- **Guest URL:** `http://<LAN-IP>:5000` (auto-detected)
- **Default credentials:** `admin` / `Admin@1234!`

It also opens the admin panel automatically in your default browser.

---

## Part D — Face Search Setup

> Run this **after** the PixBridge installer has finished successfully. Requires Python 3.11 and NSSM from [Part B](#part-b--client-machine-prerequisites).

Open **PowerShell as Administrator** and run:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\Program Files\PixBridge\scripts\setup-face-search.ps1"
```

### What this script does:

1. Installs the pgvector PostgreSQL extension (for storing face embeddings)
2. Runs the face recognition database migration (adds the `face_embeddings` table + HNSW index)
3. Creates a Python virtual environment at `C:\Program Files\PixBridge\face-recognition\.venv`
4. Installs Python dependencies (`insightface`, `fastapi`, `opencv`, etc.)
5. Downloads the **InsightFace buffalo_l model** (~300 MB) on first run — **internet required for this step only**
6. Installs the face recognition service as a Windows service (`PixBridgeFaceRecognition`) via NSSM

### Verify face search is working:

```powershell
# Check service status
Get-Service PixBridgeFaceRecognition

# Check health endpoint
Invoke-WebRequest http://localhost:8080/health | Select-Object -ExpandProperty Content
# Expected: {"status":"ok","model":"buffalo_l","ready":true}
```

> **Offline use:** After the first setup (model downloaded), face search works completely offline. No internet is needed during events.

---

## Part E — Network & Wi-Fi Setup for Guests

PixBridge is designed for **offline event photography** — guests connect to the same Wi-Fi router as the PixBridge laptop and scan a QR code to view their photos.

### E.1 Router Setup

- Any Wi-Fi router works (no internet needed on the router)
- Connect the PixBridge laptop to the router via Wi-Fi or Ethernet
- The laptop's Wi-Fi profile must be set to **Private** (done automatically by the installer)

### E.2 Verify LAN Access

After connecting to the event Wi-Fi:

```powershell
# Get the current LAN IP
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "169.*" -and $_.IPAddress -ne "127.0.0.1" }).IPAddress
```

Open a browser on a **guest's phone** (connected to the same Wi-Fi) and navigate to:
```
http://<LAN-IP>:5000
```

If it loads, the network is set up correctly.

### E.3 QR Code Regeneration

PixBridge automatically detects the LAN IP on every service startup and regenerates all QR codes. No manual action is needed when moving between locations or changing routers.

To force a QR code refresh (e.g., after changing router):

```powershell
Restart-Service PixBridgeApi
```

---

## Part F — First Login & Initial Configuration

### F.1 Open the Admin Panel

```
http://localhost:5000/admin
```

**Default credentials:**

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@1234!` |

### F.2 Mandatory First Steps

1. **Change the admin password:**
   Settings → Account → Change Password

2. **Set the studio name:**
   Settings → General → Studio Name
   *(This appears on watermarks and the gallery header)*

3. **Configure watermark settings** (optional):
   Settings → Watermark → enable/customize position, opacity, text

4. **Create your first event:**
   Events → New Event → fill in event name, date, and gallery settings

### F.3 Invite Guests to a Gallery

1. Go to **Events** → select an event → **QR Access** tab
2. The QR code is auto-generated — display it at the event
3. Guests scan it with their phone camera → opens the gallery instantly
4. For face search: guests tap "Find My Photos" → take a selfie → see only their photos

---

## Part G — Verification Checklist

Run these checks after installation to confirm everything is working:

```powershell
# 1. Both Windows services are Running
Get-Service PixBridgeApi, PixBridgeWorker | Select-Object Name, Status

# 2. API health check
(Invoke-WebRequest http://localhost:5000/api/health).Content
# Expected: {"status":"Healthy",...}

# 3. PostgreSQL database exists
psql -U pixbridge -d pixbridge -c "SELECT version();"
# Enter password: pixbridge123

# 4. Face search service (if installed)
Get-Service PixBridgeFaceRecognition | Select-Object Name, Status
(Invoke-WebRequest http://localhost:8080/health).Content

# 5. Admin panel loads
Start-Process "http://localhost:5000/admin"
```

All services should show `Status: Running`.

---

## Part H — Troubleshooting & Repair

### H.1 Quick Repair (Recommended First Step)

If something isn't working after installation, the included repair script re-runs only the failed step:

```powershell
# Open PowerShell as Administrator, then:
Set-ExecutionPolicy Bypass -Scope Process -Force

# Repair everything (database + services + network):
& "C:\Program Files\PixBridge\scripts\repair.ps1"

# Repair only the database:
& "C:\Program Files\PixBridge\scripts\repair.ps1" -Step DB

# Repair only the Windows services:
& "C:\Program Files\PixBridge\scripts\repair.ps1" -Step Services

# Repair only the firewall/network:
& "C:\Program Files\PixBridge\scripts\repair.ps1" -Step Network
```

---

### H.2 Common Problems & Fixes

#### Problem: Services not starting

```powershell
# Check service status
Get-Service PixBridgeApi, PixBridgeWorker

# View Windows event log for errors
Get-EventLog -LogName Application -Source "PixBridgeApi" -Newest 10 | Select-Object TimeGenerated, Message

# Manually start services
Start-Service PixBridgeApi
Start-Service PixBridgeWorker
```

---

#### Problem: Cannot connect to database

**Symptom:** API starts but returns `500 Internal Server Error` or health check shows `Unhealthy`.

```powershell
# 1. Check PostgreSQL service is running
Get-Service | Where-Object { $_.Name -like "postgresql*" }

# 2. Start it if stopped
Start-Service postgresql-x64-17

# 3. Test connection
psql -U pixbridge -d pixbridge -c "SELECT 1"
# Password: pixbridge123

# 4. If connection fails, re-run DB setup
& "C:\Program Files\PixBridge\scripts\repair.ps1" -Step DB -PgPassword "your_postgres_superuser_password"
```

---

#### Problem: Guests cannot open the gallery from their phones

```powershell
# 1. Confirm firewall rule exists
Get-NetFirewallRule -DisplayName "PixBridge*" | Select-Object DisplayName, Enabled, Direction

# 2. Add the firewall rule if missing
New-NetFirewallRule -DisplayName "PixBridge API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private

# 3. Confirm Wi-Fi profile is set to Private
(Get-NetConnectionProfile).NetworkCategory
# Must be: Private

# 4. Set to Private if it shows Public
Set-NetConnectionProfile -NetworkCategory Private
```

---

#### Problem: Port 5000 already in use

```powershell
# Find what is using port 5000
netstat -ano | findstr ":5000"

# Get the process name from the PID shown above
Get-Process -Id <PID>

# If it's another app, stop it or change PixBridge port in:
# C:\Program Files\PixBridge\api\appsettings.json → Kestrel.Endpoints.Http.Url
```

---

#### Problem: Installer failed mid-way

Run the repair script from a PowerShell Admin prompt:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\Program Files\PixBridge\scripts\repair.ps1"
```

The repair script reads the installation logs and reruns only the failed steps.

---

#### Problem: Face search not finding photos

1. Confirm the face recognition service is running:
   ```powershell
   Get-Service PixBridgeFaceRecognition
   (Invoke-WebRequest http://localhost:8080/health).Content
   ```

2. Confirm the buffalo_l model is downloaded:
   ```
   C:\Program Files\PixBridge\face-recognition\.insightface\models\buffalo_l\
   ```
   If the folder is empty or missing, the service will attempt to download the model on next restart (~300 MB, internet required once).

3. Re-index photos for an event:
   Admin Panel → Events → [Event Name] → Face Recognition → **Re-index All Photos**

---

### H.3 Uninstall PixBridge

```powershell
# Option 1: Use Windows Add/Remove Programs
# Control Panel → Programs → PixBridge → Uninstall

# Option 2: Manual uninstall script (run as Administrator)
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\Program Files\PixBridge\scripts\uninstall-service.ps1"

# Then delete the install folder
Remove-Item "C:\Program Files\PixBridge" -Recurse -Force
```

> **Database is NOT deleted by uninstall.** To also remove the database:
> ```powershell
> psql -U postgres -c "DROP DATABASE IF EXISTS pixbridge;"
> psql -U postgres -c "DROP ROLE IF EXISTS pixbridge;"
> ```

---

## Quick Reference

| Item | Value |
|------|-------|
| Admin URL | `http://localhost:5000/admin` |
| Guest Gallery URL | `http://<LAN-IP>:5000` |
| Default admin login | `admin` / `Admin@1234!` |
| API port | `5000` |
| Face recognition port | `8080` |
| Database name | `pixbridge` |
| Database user | `pixbridge` |
| Database password | `pixbridge123` |
| API service name | `PixBridgeApi` |
| Worker service name | `PixBridgeWorker` |
| Face search service | `PixBridgeFaceRecognition` |
| Install directory | `C:\Program Files\PixBridge` |
| Log directory | `C:\Program Files\PixBridge\logs` |
| Repair script | `C:\Program Files\PixBridge\scripts\repair.ps1` |
| DB setup log | `C:\Program Files\PixBridge\logs\setup-db.log` |
| Services setup log | `C:\Program Files\PixBridge\logs\setup-services.log` |

---

*PixBridge — Offline Event Photo Sharing Platform*
