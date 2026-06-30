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
  • PixBridge Server                                  • Opens http://192.168.10.10
  • PostgreSQL                                        • Scans QR Code
  • Photo Storage (Disk)                              • Views / Downloads Photos
  • Kestrel on :80
```

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
| Database | PostgreSQL (local) |
| File Watching | FileSystemWatcher |
| Image Processing | SixLabors.ImageSharp |
| Auth | JWT Bearer Tokens |
| Logging | Serilog → File + Console |
| Frontend | React 18 + Vite + TypeScript |
| Styling | TailwindCSS |
| State / Data | TanStack Query |
| HTTP Client | Axios |
| Forms | React Hook Form + Zod |
| QR Code | QRCoder (.NET) |

## Security Model

- Admin: JWT-authenticated (studio staff)
- Guests: No authentication required — read-only gallery access via LAN
- All traffic stays within LAN (no internet)
- Path traversal protection on all file endpoints
- Rate limiting on download endpoints
- Role-based authorization: Admin vs Guest

## Deployment Model

- Self-contained .NET 8 publish (no runtime required on host)
- Inno Setup installer for Windows
- Kestrel listens on http://0.0.0.0:80
- Static IP: 192.168.10.10
- PostgreSQL installed locally via installer