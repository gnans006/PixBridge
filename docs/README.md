# PixBridge — Event Photo Sharing Platform

PixBridge is a fully offline, LAN-based event photo sharing platform for photography studios. A laptop running PixBridge acts as the photo server — photographers drop photos into a folder and guests instantly see them on their phones via Wi-Fi.

---

## Quick Start

### Prerequisites
- Windows 10/11 (64-bit)
- PostgreSQL 15+ installed and in PATH
- .NET 8 SDK (development only)
- Node.js 20+ (development only)

### Production Install
1. Run the PixBridge installer (`PixBridge-Setup-1.0.0.exe`)
2. Follow the wizard — it will create the database and install services automatically
3. Access the admin panel at **http://localhost:5000/admin**
4. Default credentials: `admin` / `Admin@1234!` ← **change on first login**

### Development Setup
```powershell
# 1. Clone/copy repository to C:\CLS\PixBridge

# 2. Create the database
cd C:\CLS\PixBridge\scripts
.\setup-postgresql.ps1

# 3. Run the API (auto-applies migrations and seeds data)
cd C:\CLS\PixBridge
dotnet run --project src\EventPhoto.Api

# 4. In another terminal — run the Worker
dotnet run --project src\EventPhoto.Worker

# 5. In another terminal — run the React dev server
cd src\EventPhoto.React
npm run dev
# Dev server: http://localhost:5173 (proxies to API on :5000)
```

---

## Architecture

```
[Camera / SD Card]
       │
       ▼
[Photographer's Laptop]
  ┌─────────────────────────────────────┐
  │  PixBridge                          │
  │  ┌───────────┐  ┌────────────────┐  │
  │  │ API       │  │ Worker         │  │
  │  │ :5000     │  │ FileWatcher    │  │
  │  │ React UI  │  │ ThumbnailGen   │  │
  │  │ SignalR   │  └────────────────┘  │
  │  └───────────┘                      │
  │  ┌─────────────┐  ┌──────────────┐  │
  │  │ PostgreSQL  │  │ Photo Disk   │  │
  │  │ (metadata)  │  │ Storage      │  │
  │  └─────────────┘  └──────────────┘  │
  └─────────────────────────────────────┘
       │
       │  Wi-Fi (same network)
       │
  [Guest Devices]
  Opens: http://<LAN-IP>:5000
  Scans QR Code → Gallery
```

---

## Network Setup

| Component | IP / Port |
|-----------|-----------|
| PixBridge Server | Auto-detected LAN IP |
| API / Gallery | `http://<LAN-IP>:5000` (port 5000) |
| SignalR Hub | `http://<LAN-IP>:5000/hubs/photos` |
| Admin Panel | `http://<LAN-IP>:5000/admin` |

**The LAN IP is auto-detected** on every API startup — no static IP required. The `app.serverUrl` setting in the database is updated automatically and all QR codes are regenerated. To override manually: Admin → Settings → `app.serverUrl`.

**WiFi profile must be set to Private** on the laptop for Windows Firewall to allow inbound connections from phone:
- Windows Settings → Network & Internet → Wi-Fi → click network name → **Private**
- Run once as Administrator: `netsh advfirewall firewall add rule name="PixBridge API" dir=in action=allow protocol=TCP localport=5000 profile=private`

---

## How It Works

1. **Photographer** connects camera/SD card and copies photos to the event's watch folder (for example, `D:\Events\Wedding_2024\`)
2. **FileWatcherService** detects new files instantly via `FileSystemWatcher`
3. **ThumbnailProcessorService** generates 400×400 JPEG thumbnails using ImageSharp
4. **SignalR** broadcasts new photo events to all connected gallery clients
5. **Guests** see new photos appear in real time without refreshing

---

## Admin Features

| Feature | Path |
|---------|------|
| Dashboard | /admin |
| Create Event | /admin/events/new |
| Event List | /admin/events (search + pagination) |
| Event Detail | /admin/events/:id |
| Statistics | /admin/statistics |
| Logs | /admin/logs |
| Health Monitor | /admin/health |
| Settings | /admin/settings |
| QR Code | GET /api/events/{id}/qrcode |
| Refresh QR | POST /api/events/{id}/qrcode/refresh |

---

## API Reference

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/auth/login | No | Admin login |
| GET | /api/events | No | List events |
| POST | /api/events | Admin | Create event |
| GET | /api/photos/event/{id} | No | Gallery photos (paged) |
| GET | /api/photos/{id}/thumbnail | No | Thumbnail image |
| GET | /api/photos/{id}/download | No | Download original |
| GET | /api/statistics/dashboard | Admin | Dashboard stats |
| GET | /api/health | No | Health check |
| WS | /hubs/photos?eventId={id} | No | Real-time updates |

Full API docs available at **http://localhost:5000/swagger** in development.

---

## Configuration

Edit `appsettings.json` in the API install folder:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=pixbridge;Username=postgres;Password=<your-password>;"
  },
  "Jwt": {
    "Secret": "",
    "ExpiryHours": 8
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

> `Jwt.Secret` must be set via the `Jwt__Secret` environment variable in production (minimum 32 characters). The app will refuse to start if it is empty.

System settings (including `app.serverUrl`) can also be changed from the Admin → Settings panel.

---

## File Structure

```
C:\PixBridge\
  api\
    EventPhoto.Api.exe
    appsettings.json
    wwwroot\
    logs\
  worker\
    EventPhoto.Worker.exe
    appsettings.json
    logs\
  scripts\
    setup-postgresql.ps1
    install-service.ps1
    uninstall-service.ps1
  docs\
    deployment-guide.md
  README.md
```

---

## Security Notes

- Change the default admin password immediately after installation
- Change the JWT secret in `appsettings.json` before going to production
- Guest gallery is intentionally unauthenticated (read-only, LAN only)
- All traffic stays inside the local Wi-Fi network — no internet required
- Rate limiting: 30 downloads/minute per IP address

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Photos not appearing | Check the watch folder path in Event settings. Ensure the Worker service is running. |
| Thumbnails pending | Check `worker\logs\` for thumbnail processing errors. |
| Can't access from phone | Ensure WiFi profile is **Private** (not Public). Open port 5000 in Windows Firewall as Administrator. |
| QR code has wrong IP | Admin → Events → click ↺ Refresh QR. Or update `app.serverUrl` in Settings first. |
| Database connection error | Check PostgreSQL is running and the connection string in `appsettings.json`. |
| Admin login failed | Verify the seeded admin account exists and review API logs. |
| Mobile browser cache | The `/api/events/{id}/qrcode` endpoint sends `Cache-Control: no-store` — hard-refresh the page on mobile. |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8, ASP.NET Core, Kestrel |
| Real-time | ASP.NET Core SignalR |
| CQRS | MediatR |
| Validation | FluentValidation |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL |
| Images | SixLabors.ImageSharp |
| Auth | JWT Bearer (HS256) |
| Logging | Serilog |
| Frontend | React 18, Vite, TypeScript |
| Styling | TailwindCSS |
| Data Fetching | TanStack Query |

---

## License

Commercial software. All rights reserved.
