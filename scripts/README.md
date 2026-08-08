# PixBridge Scripts

Run all scripts from the **solution root** (`C:\CLS\PixBridge`):

```powershell
.\scripts\<script-name>.ps1
```

---

## Daily Use

### `dev-start.ps1`
Starts the full development environment — opens 3 terminal windows:
- **API** → `http://localhost:5000`
- **Worker** → background photo processing
- **React** → `http://localhost:5173` (Vite dev server with hot reload)

```powershell
.\scripts\dev-start.ps1                # start everything
.\scripts\dev-start.ps1 -NoWorker      # skip Worker
.\scripts\dev-start.ps1 -NoReact       # skip React dev server
.\scripts\dev-start.ps1 -ApiOnly       # API only
```

---

### `dev-stop.ps1`
Stops all running PixBridge processes (API, Worker, Vite dev server).

```powershell
.\scripts\dev-stop.ps1                 # stop everything
.\scripts\dev-stop.ps1 -ApiOnly        # stop API + Worker only
.\scripts\dev-stop.ps1 -ReactOnly      # stop React dev server only
```

---

### `lan-start.ps1`
**Event day script.** Builds the React frontend into `wwwroot`, then starts the API and Worker.  
Guests connected to the same WiFi can access `http://<server-ip>:5000` or scan a QR code.  
The API auto-detects the LAN IP and regenerates all QR codes on startup.

```powershell
.\scripts\lan-start.ps1                # build React + start API + Worker
.\scripts\lan-start.ps1 -SkipBuild     # skip React build (use existing wwwroot)
.\scripts\lan-start.ps1 -NoWorker      # API only, no Worker
```

---

### `db-migrate.ps1`
Applies pending EF Core database migrations. Run this after `git pull` when new migrations are included.  
Automatically stops running processes before building to avoid DLL file locks.

```powershell
.\scripts\db-migrate.ps1               # stop services + apply migrations
.\scripts\db-migrate.ps1 -SkipStopServices   # apply without stopping services
```

---

## Setup & Installation

### `../setup.ps1`  *(solution root)*
**First-time machine setup.** Run this on a fresh machine after cloning the repo.  
Performs: NuGet restore → EF migrations → npm install.

```powershell
.\setup.ps1                            # full setup
.\setup.ps1 -SkipMigration            # skip DB migration
.\setup.ps1 -SkipNpm                  # skip npm install
.\setup.ps1 -Build                    # also compile .NET + React after setup
```

---

### `setup-postgresql.ps1`
Installs and configures PostgreSQL for PixBridge. Run once on a new machine before `setup.ps1`.

```powershell
.\scripts\setup-postgresql.ps1
```

---

### `setup-face-search.ps1`
Sets up the Python face recognition service (`PixBridge.FaceRecognition`).  
Installs Python dependencies and configures the service endpoint.

```powershell
.\scripts\setup-face-search.ps1
```

---

### `install-service.ps1`
Installs the API and Worker as Windows Services so they start automatically with Windows.  
Must be run as Administrator.

```powershell
.\scripts\install-service.ps1
```

---

### `uninstall-service.ps1`
Removes the PixBridge Windows Services.  
Must be run as Administrator.

```powershell
.\scripts\uninstall-service.ps1
```

---

## Network & Deployment

### `fix-network-access.ps1`
Fixes Windows Firewall rules so guest devices on the same WiFi can reach the API on port 5000.  
Run this if guests cannot connect even though the API is running.  
Must be run as Administrator.

```powershell
.\scripts\fix-network-access.ps1
```

---

### `build-release.ps1`
Builds a production release package ready for distribution or Inno Setup installer.  
Outputs to `/publish` folder.

```powershell
.\scripts\build-release.ps1            # build version 1.0.0
.\scripts\build-release.ps1 -Version "1.2.0"
```

---

## Quick Reference

| Situation | Script |
|---|---|
| Fresh machine / first time | `.\setup.ps1` |
| Pulled new code with migrations | `.\scripts\db-migrate.ps1` |
| Start coding | `.\scripts\dev-start.ps1` |
| Stop everything | `.\scripts\dev-stop.ps1` |
| Event day — guests need access | `.\scripts\lan-start.ps1` |
| Guests can't connect (firewall) | `.\scripts\fix-network-access.ps1` |
| Run as Windows service | `.\scripts\install-service.ps1` |
| Package for distribution | `.\scripts\build-release.ps1` |
