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
3. Access the admin panel at **http://localhost/admin** or **http://192.168.10.10/admin**
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
  │  │ :80       │  │ FileWatcher    │  │
  │  │ React UI  │  │ ThumbnailGen   │  │
  │  │ SignalR   │  └────────────────┘  │
  │  └───────────┘                      │
  │  ┌─────────────┐  ┌──────────────┐  │
  │  │ PostgreSQL  │  │ Photo Disk   │  │
  │  │ (metadata)  │  │ Storage      │  │
  │  └─────────────┘  └──────────────┘  │
  └─────────────────────────────────────┘
       │
       │  Wi-Fi Router (192.168.10.0/24)
       │
  [Guest Devices]
  Opens: http://192.168.10.10
  Scans QR Code → Gallery
```

---

## Network Setup

| Component | IP / Port |
|-----------|-----------|
| PixBridge Server | 192.168.10.10 |
| API / Gallery | http://192.168.10.10 (port 80) |
| SignalR Hub | http://192.168.10.10/hubs/photos |
| Admin Panel | http://192.168.10.10/admin |

**Laptop:** Set static IP `192.168.10.10` on the Wi-Fi adapter connected to the event router.

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
| Event List | /admin/events |
| Statistics | /admin/statistics |
| Settings | /admin/settings |
| QR Code | GET /api/events/{id}/qrcode |

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
    "DefaultConnection": "Host=localhost;Database=pixbridge;Username=pixbridge;Password=pixbridge123;"
  },
  "Jwt": {
    "Secret": "YOUR-SECRET-KEY-MIN-32-CHARS",
    "ExpiryHours": 8
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      }
    }
  }
}
```

System settings can also be changed from the Admin → Settings panel.

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
| Can't access from phone | Ensure the laptop has IP 192.168.10.10 and Windows Firewall allows port 80. |
| Database connection error | Check PostgreSQL is running and the connection string in `appsettings.json`. |
| Admin login failed | Verify the seeded admin account exists and review API logs. |

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
