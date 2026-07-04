---
title: "PixBridge Architecture"
project: "PixBridge"
docType: "architecture"
created: "2026-06-30"
---

# PixBridge – Architecture Overview

## System Overview

PixBridge is a fully offline, LAN-based Event Photo Sharing Platform designed for photography studios. A laptop acts as the photo server; guests connect via Wi-Fi and browse photos in real time through a browser.

## Network Topology

```
[Photographer's Camera / SD Card]
          │
          ▼
[Photographer's Laptop] ──── Wi-Fi Router ──── [Guest Devices (Phone/Tablet/Laptop)]
  • PixBridge Server                                  • Opens http://<LAN-IP>:5000
  • PostgreSQL                                        • Scans QR Code
  • Photo Storage (Disk)                              • Views / Downloads Photos
  • Kestrel on :5000
```

> **IP is auto-detected.** On every startup the API reads the active Wi-Fi adapter IP, updates the `app.serverUrl` setting in the database, and regenerates all QR codes automatically. No static IP configuration required.

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                        EventPhoto.Api                        │
│          Controllers │ SignalR Hubs │ Middleware             │
│                ↕ depends on ↕                                │
├────────────────────────────┬────────────────────────────────┤
│   EventPhoto.Application   │   EventPhoto.Infrastructure     │
│  Commands │ Queries │ CQRS │  EF Core │ PostgreSQL           │
│  MediatR │ Validators      │  Repositories │ File Services   │
│          ↕ depends on ↕    │         ↕ depends on ↕          │
├────────────────────────────┴────────────────────────────────┤
│                       EventPhoto.Domain                      │
│         Entities │ Value Objects │ Domain Events             │
│                   Enums │ Interfaces                         │
├─────────────────────────────────────────────────────────────┤
│                      EventPhoto.Contracts                    │
│              DTOs │ Request / Response Models                │
├─────────────────────────────────────────────────────────────┤
│                      EventPhoto.Worker                       │
│         FileSystemWatcher │ Thumbnail Generator              │
│                BackgroundService                             │
└─────────────────────────────────────────────────────────────┘
```

## Photo Upload Flow

```
Photographer copies photos to Event Folder
          │
          ▼
FileSystemWatcher (EventPhoto.Worker)
  detects new .jpg / .png / .cr2 etc.
          │
          ▼
ThumbnailProcessor
  • Resize to 400×400 thumbnail (ImageSharp)
  • Save thumbnail to /thumbs/<eventId>/
          │
          ▼
PhotoCreated Command → MediatR → PhotoCommandHandler
  • Save metadata to PostgreSQL
  • Raise PhotoCreatedDomainEvent
          │
          ▼
SignalR Hub broadcasts "photo:new" event
          │
          ▼
React Gallery updates in real time
  • New thumbnail appears without page reload
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8, Kestrel |
| Real-time | SignalR |
| CQRS | MediatR |
| Validation | FluentValidation |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 18 (local) |
| File Watching | FileSystemWatcher |
| Image Processing | SixLabors.ImageSharp |
| Auth | JWT Bearer (HS256, BCrypt.Net workFactor 12) |
| Logging | Serilog → File + Console |
| Frontend | React 18 + Vite + TypeScript |
| Styling | TailwindCSS |
| State / Data | TanStack Query v5 |
| HTTP Client | Axios |
| Forms | React Hook Form + Zod |
| QR Code | QRCoder (.NET) |

## Security Model

- Admin: JWT-authenticated (HS256, 8 hr expiry), BCrypt password hashing (workFactor 12)
- Guests: No authentication required — read-only gallery access via LAN
- All traffic stays within LAN (no internet)
- Path traversal protection on all file and folder endpoints
- Rate limiting: login endpoint 5 req/min (brute-force protection), downloads 30 req/min
- Frontend validates JWT `exp` claim — auto-logs out on token expiry
- Password complexity enforced: uppercase, lowercase, digit, special character
- Role-based authorization: Admin vs Guest
- JWT secret stored only in `appsettings.Development.json` or `Jwt__Secret` env var (never in production config)

## QR Code Lifecycle

1. **Event created** → QR PNG generated immediately using current `app.serverUrl`
2. **API restarts** → LAN IP auto-detected; if changed, `app.serverUrl` updated and all QR codes regenerated
3. **Manual refresh** → Admin → Events → ↺ Refresh QR button calls `POST /api/events/{id}/qrcode/refresh`
4. **QR image served** with `Cache-Control: no-store` so phones always fetch the latest file

## Deployment Model

- Self-contained .NET 8 publish (no runtime required on host)
- Inno Setup installer for Windows
- Kestrel listens on `http://0.0.0.0:5000`
- LAN IP auto-detected on startup; stored in `app.serverUrl` DB setting
- PostgreSQL 18 installed locally via installer
- React built into `wwwroot/` — served by Kestrel at port 5000 (no separate Node process in production)