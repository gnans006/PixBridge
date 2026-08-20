# PixBridge — Complete Architecture Assessment

> **Classification:** Internal Architecture Document  
> **Prepared by:** Principal Architecture Review (AI-assisted)  
> **Date:** 2026-08-20  
> **Version:** 1.0  
> **Status:** Final — Read-Only Assessment

---

## Table of Contents

1. [Section 1 — Solution Structure](#section-1--solution-structure)
2. [Section 2 — Clean Architecture Review](#section-2--clean-architecture-review)
3. [Section 3 — Domain Model](#section-3--domain-model)
4. [Section 4 — Authentication & Authorization](#section-4--authentication--authorization)
5. [Section 5 — Event System](#section-5--event-system)
6. [Section 6 — Network Architecture](#section-6--network-architecture)
7. [Section 7 — SignalR Architecture](#section-7--signalr-architecture)
8. [Section 8 — Photo Processing Pipeline](#section-8--photo-processing-pipeline)
9. [Section 9 — Face Recognition Architecture](#section-9--face-recognition-architecture)
10. [Section 10 — Database Architecture](#section-10--database-architecture)
11. [Section 11 — Application Settings](#section-11--application-settings)
12. [Section 12 — Background Services](#section-12--background-services)
13. [Section 13 — Storage Architecture](#section-13--storage-architecture)
14. [Section 14 — User Experience Architecture](#section-14--user-experience-architecture)
15. [Section 15 — Commercial Product Readiness](#section-15--commercial-product-readiness)
16. [Section 16 — Extensibility Review](#section-16--extensibility-review)
17. [Section 17 — Risks & Technical Debt](#section-17--risks--technical-debt)
18. [Section 18 — Final Output](#section-18--final-output)

---

## Section 1 — Solution Structure

### 1.1 Solution Overview

PixBridge is a **self-hosted photography event management platform** built on .NET 8 and React 18. It follows a **Clean Architecture** pattern across a 6-project .NET solution plus a Vite/React SPA frontend. The system is designed for single-studio local/LAN deployment today, with the infrastructure foundations for cloud migration.

**Total Source Scale:**
- `.cs` files: 407
- `.tsx` files: 154
- `.ts` files: 1,498 (including node_modules)
- Total repository files: ~13,857

---

### 1.2 Projects and Responsibilities

| Project | Type | Responsibility |
|---------|------|----------------|
| **EventPhoto.Domain** | Class Library | Aggregates, Entities, Domain Events, Enums, Repository Interfaces, Value Objects, Domain Exceptions. Zero external dependencies. |
| **EventPhoto.Application** | Class Library | MediatR Commands/Queries, Application Service Interfaces, FluentValidation Validators, AutoMapper Profiles, Pipeline Behaviors. Depends on Domain only. |
| **EventPhoto.Contracts** | Class Library | Shared DTOs and response models used across Application and API layers. Decouples API serialization from domain objects. |
| **EventPhoto.Infrastructure** | Class Library | EF Core + PostgreSQL + pgvector, Repository implementations, JWT/Auth services, File I/O services, QR Code generation, Thumbnail service, Watermark service, Network/URL services, Audit service. |
| **EventPhoto.Api** | ASP.NET Core Web API | REST API Controllers, SignalR Hub, JWT authentication pipeline, Rate limiting, CORS, Swagger, Static file serving (hosts the compiled React SPA), Windows Service hosting. |
| **EventPhoto.Worker** | .NET Worker Service | Background hosted services: FileWatcherService, ThumbnailProcessorService, FaceIndexingService (legacy), AiDiscoveryPipelineService, DeadLetterProcessorService, SelfieRetentionService. |
| **EventPhoto.React** | Vite/React 18 SPA | Full admin UI + Guest gallery UI. TanStack Query, React Router v6, Zustand auth store, Framer Motion, MUI/Tailwind hybrid. |
| **PixBridge.FaceRecognition** | Python Microservice | InsightFace + ArcFace face detection, embedding generation, quality scoring. Exposed via HTTP. Called by the Worker's AI Discovery Pipeline. |

---

### 1.3 Solution Dependency Diagram

```
┌──────────────────────────────────────────────────────────┐
│                    EventPhoto.React (SPA)                │
│              Vite · React 18 · TanStack Query            │
│         Served as static files from EventPhoto.Api       │
└─────────────────────────┬────────────────────────────────┘
                          │ HTTP / SignalR / REST API
              ┌───────────▼────────────┐
              │    EventPhoto.Api      │
              │  ASP.NET Core 8 Web   │
              │  JWT · SignalR · CORS  │
              └──────┬────────┬───────┘
                     │        │
         ┌───────────▼──┐  ┌──▼────────────────┐
         │ EventPhoto.  │  │  EventPhoto.       │
         │ Application  │  │  Infrastructure    │
         │ MediatR CQRS │  │  EF Core · pgvec  │
         └──────┬───────┘  └──────┬────────────┘
                │                 │
         ┌──────▼─────────────────▼────────────┐
         │          EventPhoto.Domain           │
         │  Aggregates · Entities · Interfaces  │
         └──────────────────────────────────────┘
                         ▲
              ┌──────────┴──────────────┐
              │    EventPhoto.Worker    │
              │  Background Services    │
              │  (references App+Infra) │
              └─────────────────────────┘
                         │ HTTP
              ┌──────────▼──────────────┐
              │  PixBridge.FaceRecog.   │
              │  Python · InsightFace   │
              │  Port 5001 (localhost)  │
              └─────────────────────────┘
                         │
              ┌──────────▼──────────────┐
              │     PostgreSQL 16       │
              │  + pgvector extension   │
              └─────────────────────────┘
```

---

### 1.4 EventPhoto.Contracts Role

`EventPhoto.Contracts` acts as the shared model boundary — DTOs, request/response shapes, pagination models. This is the correct anti-corruption layer between the outside world (HTTP contracts) and the domain model.

---

## Section 2 — Clean Architecture Review

### 2.1 Architectural Layers

| Layer | Project(s) | Status |
|-------|-----------|--------|
| Domain | EventPhoto.Domain | ✅ Clean |
| Application | EventPhoto.Application | ✅ Mostly clean |
| Infrastructure | EventPhoto.Infrastructure | ✅ Correctly isolated |
| API (Presentation) | EventPhoto.Api | ⚠️ Minor concerns |
| SPA (Frontend) | EventPhoto.React | ✅ Well-structured |

---

### 2.2 Strengths

1. **Domain purity**: `EventPhoto.Domain` has **zero external NuGet dependencies** except Pgvector (a C# data type, not an EF concern). No EF Core, no ASP.NET, no MediatR — textbook clean.
2. **Rich aggregates**: `Event`, `Photo`, `FaceProcessingJob`, `GuestFaceSession`, `Subscription` all encapsulate business rules, invariants, and state machine transitions inside the entity.
3. **CQRS via MediatR**: Every use case is a Command or Query. Pipeline behaviors (logging + validation) are applied uniformly.
4. **Repository pattern**: All domain interfaces (`IEventRepository`, `IPhotoRepository`, etc.) live in the domain layer. Implementations in Infrastructure. Correct dependency direction.
5. **Unit of Work**: `IUnitOfWork` is a domain interface. `AppDbContext` implements it. Transaction boundaries are explicit.
6. **FluentValidation in Application layer**: Validators are command-local, not scattered in controllers.
7. **Domain Events**: `EventCreatedEvent`, `PhotoCreatedEvent`, `FaceIndexCompletedEvent`, etc. raise events via the `AggregateRoot.RaiseDomainEvent()` mechanism. DbContext dispatches them on `SaveChangesAsync`.
8. **Feature flags on navigation**: `useFeatureFlags()` hook drives sidebar item visibility, preventing access to disabled modules without requiring backend guards on every endpoint.

---

### 2.3 Violations and Concerns

| # | Violation | Location | Severity |
|---|-----------|----------|----------|
| 1 | **Domain Events Not Dispatched to Handlers** | `AppDbContext.SaveChangesAsync()` clears events but does NOT dispatch them to MediatR. Domain events are raised but silently dropped. | 🔴 High |
| 2 | **No Authorization on Guest Endpoints** | `GuestLayout` routes (`/gallery/*`, `/gallery/*/find`, etc.) are completely unauthenticated. No rate limiting on selfie upload. | 🟡 Medium |
| 3 | **Worker References Infrastructure Directly** | `EventPhoto.Worker` takes a dependency on `EventPhoto.Infrastructure` and `EventPhoto.Application` — this is architecturally acceptable for a worker, but creates a tight coupling. | 🟡 Medium |
| 4 | **Startup IP Auto-update in Program.cs** | The network self-configuration logic in `Program.cs` (IP detection, QR code regeneration, legacy setting sync) is complex startup logic that belongs in an `IHostedService` or a `StartupTask`. | 🟡 Medium |
| 5 | **CORS set to AllowAnyOrigin** | `policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` — permissive CORS is acceptable during development but is a production security gap. | 🟡 Medium |
| 6 | **Dual Settings Systems** | `SystemSetting` (key-value store) and `ApplicationSettings` (typed singleton) co-exist. A legacy `app.serverUrl` is kept in sync manually. | 🟡 Medium |
| 7 | **No Subscription Enforcement at API Layer** | `Subscription.IsOperational` exists on the domain, but there is no middleware or policy that actually blocks API calls when a subscription is expired. | 🔴 High |
| 8 | **File paths stored as absolute strings** | `Event.WatchFolder`, `Event.ThumbnailFolder`, `Photo.OriginalPath` etc. are absolute OS paths. This prevents multi-server deployment and makes backup/restore fragile. | 🟡 Medium |
| 9 | **No refresh token** | JWT is issued without a refresh token strategy. Token expiry forces re-login. | 🟡 Medium |
| 10 | **React has no route-level auth guards** | `AdminLayout` appears to gate admin routes but the implementation needs verification — there is no `ProtectedRoute` wrapper visible in routing. | 🟡 Medium |

---

### 2.4 Technical Debt Summary

- Domain events are raised but never handled (dispatcher missing)
- Subscription limits are modeled but not enforced at runtime
- Dual configuration systems need consolidation
- Absolute file paths lock deployment to a single machine
- Legacy `Admin`/`Viewer` roles still accepted in auth policies

---

## Section 3 — Domain Model

### 3.1 Aggregates and Entities

#### Core Aggregates

| Aggregate Root | Purpose | Key State |
|----------------|---------|-----------|
| `Event` | Photography event (wedding, corporate, etc.) | Active/Inactive/Deleted, FaceRecognition on/off, GalleryMode, WatchFolder, PhotoCount |
| `Photo` | Single photo file metadata | ThumbnailStatus, FaceIndexStatus, DownloadCount, Deleted |
| `ApplicationSettings` | Singleton platform config | StudioName, PublicBaseUrl, FeatureFlags, Branding, defaults |
| `Subscription` | Singleton commercial license | Plan, State, LicenseKey, MaxEvents, MaxUsers, Expiry |
| `WatermarkConfiguration` | Per-event watermark settings | Mode, Style, Opacity, Template, LogoPath |
| `FaceProcessingJob` | AI Discovery Pipeline job per photo | State machine: Pending→Detecting→QualityChecking→Embedding→Indexing→Completed/DeadLettered |
| `GuestUploadSession` | Guest upload session for an event | Status, MaxUploads, ExpiresAt |

#### Supporting Entities (non-aggregate)

| Entity | Purpose |
|--------|---------|
| `User` | Studio staff member with role |
| `FaceEmbedding` | 512-dim ArcFace vector per detected face |
| `GuestFaceSession` | Guest's selfie search session + embedding + token |
| `PhotoMatch` | Match record between a GuestFaceSession and a Photo |
| `GuestUpload` | Individual photo submitted by a guest |
| `FaceCluster` | Future-ready face clustering (seeded but not fully implemented) |
| `AiSearchAnalytics` | Per-search analytics (duration, match count) |
| `AuditLog` | Immutable audit trail entry |
| `DownloadLog` | Per-download analytics record |
| `SystemSetting` | Legacy key-value config store |

---

### 3.2 Entity Relationship Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                           EVENT                                  │
│  Id · Name · EventType · EventDate · WatchFolder                │
│  IsActive · IsDeleted · PhotoCount · TotalSizeBytes             │
│  EnableFaceRecognition · AllowGalleryBrowsing · AllowFaceSearch │
│  FaceMatchThreshold · CreatedBy (→User.Id)                      │
└──────┬────────────────────┬─────────────────────┬───────────────┘
       │ 1:M                │ 1:1                  │ 1:M
       │                    │                      │
┌──────▼──────────┐  ┌──────▼──────────────┐  ┌───▼────────────────┐
│     PHOTO       │  │  WATERMARK CONFIG    │  │  FACE PROC JOB     │
│  EventId        │  │  EventId             │  │  EventId · PhotoId │
│  FileName       │  │  Enabled · Mode      │  │  Status · Retries  │
│  OriginalPath   │  │  Style · Opacity     │  │  NextRetryAt       │
│  ThumbnailPath  │  │  Template · LogoPath │  │  FailureType       │
│  ThumbnailStatus│  └─────────────────────-┘  └────────────────────┘
│  FaceIndexStatus│
│  DownloadCount  │
└──┬──────────────┘
   │ 1:M
   │
┌──▼──────────────────────┐      ┌─────────────────────────────────┐
│     FACE EMBEDDING      │      │       GUEST FACE SESSION        │
│  PhotoId · EventId      │      │  EventId · SessionToken         │
│  Embedding (vector 512) │      │  SelfieEmbedding (vector 512)   │
│  BoundingBox            │      │  SelfieHash · Status            │
│  Confidence · Quality   │      │  MatchCount · SearchDurationMs  │
│  EmbeddingVersion       │      │  ExpiresAt · SelfieDeletedAt    │
└─────────────────────────┘      └──────────┬──────────────────────┘
                                            │ 1:M
                                   ┌────────▼──────────┐
                                   │   PHOTO MATCH     │
                                   │  SessionId        │
                                   │  PhotoId          │
                                   │  Similarity Score │
                                   └───────────────────┘

┌─────────────────────────────┐    ┌─────────────────────────────────┐
│     GUEST UPLOAD SESSION    │    │          GUEST UPLOAD           │
│  EventId · Status           │    │  SessionId · EventId            │
│  GuestName · GuestEmail     │◄───│  StoredPath · ThumbnailPath     │
│  MaxUploads · ExpiresAt     │    │  ModerationStatus               │
└─────────────────────────────┘    └─────────────────────────────────┘

┌─────────────────────────────┐    ┌─────────────────────────────────┐
│    APPLICATION SETTINGS     │    │         SUBSCRIPTION            │
│  (Singleton – fixed ID)     │    │  (Singleton – fixed ID)         │
│  StudioName · PublicBaseUrl │    │  Plan · State · LicenseKey      │
│  FeatureFlags (Is*)         │    │  MaxEvents · MaxUsersPerStudio  │
│  EnableWatermarkByDefault   │    │  ExpiresAt · GracePeriodEndsAt  │
│  Branding (colors, themes)  │    └─────────────────────────────────┘
└─────────────────────────────┘

┌──────────────────────────────┐
│            USER              │
│  Username · Email · Role     │
│  PasswordHash · IsActive     │
│  FullName · Phone            │
└──────────────────────────────┘
```

---

### 3.3 Domain Enumerations

| Enum | Values |
|------|--------|
| `UserRole` | Admin(legacy), Viewer(legacy), StudioOwner, StudioManager, Operator |
| `EventType` | (defined in Enums/EventType.cs — e.g., Wedding, Corporate, Birthday) |
| `GalleryMode` | GalleryOnly, FaceSearchOnly, Hybrid |
| `ThumbnailStatus` | Pending, Processing, Done, Failed |
| `FaceIndexStatus` | NotRequired, Pending, Processing, Completed, Failed |
| `FaceJobStatus` | Pending, Queued, Detecting, QualityChecking, Embedding, Indexing, Completed, Failed, DeadLettered, Ignored |
| `FaceSessionStatus` | Created, Searching, Completed, Expired |
| `FaceFailureType` | CorruptedImage, QualityRejected, ServiceUnavailable, Timeout, Unknown |
| `QualityTier` | Unscored, Low, Medium, High, Premium |
| `SubscriptionPlan` | Trial(5 events, 2 users), Starter(20/5), Professional(100/unlimited), Enterprise(unlimited) |
| `SubscriptionState` | Trial, Active, GracePeriod, Expired, Cancelled |
| `ModerationStatus` | Pending, Approved, Rejected |
| `WatermarkMode` | StudioBranding, StudioAndEvent, CustomText, DynamicTemplate |
| `WatermarkStyle` | Corner, BottomRibbon, Diagonal, Center |
| `WatermarkScale` | Small, Medium, Large, Auto |
| `GuestUploadSessionStatus` | Open, Closed |

---

## Section 4 — Authentication & Authorization

### 4.1 Authentication Flow

```
┌─────────────────────────────────────────────────────┐
│                   LOGIN FLOW                         │
│                                                     │
│  1. POST /api/auth/login {username, password}       │
│  2. LoginCommandHandler:                            │
│     a. UserRepository.GetByUsernameAsync()          │
│     b. BCrypt password verify                       │
│     c. User.RecordLogin() → touch LastLoginAt       │
│     d. JwtTokenService.GenerateToken(user)          │
│  3. JWT returned: { token, expiresAt, user }        │
│  4. React: authStore (Zustand) stores JWT           │
│  5. All API requests: Authorization: Bearer {token} │
└─────────────────────────────────────────────────────┘
```

**JWT Claims structure:**
- `sub` — User ID (Guid)
- `unique_name` — Username
- `email` — Email
- `role` — `ToClaimValue()` result (StudioOwner | StudioManager | Operator)
- `display_name` — DisplayName
- Standard: `iss`, `aud`, `exp`, `iat`, `jti`

**Configuration:** `JwtSettings` from `appsettings.json` — Secret, Issuer, Audience, ExpiryMinutes.

---

### 4.2 Authorization Policies

| Policy | Allowed Roles | Applied To |
|--------|--------------|------------|
| `OwnerOnly` | StudioOwner, Admin | Subscription management, destructive platform ops |
| `ManagerOrOwner` | StudioOwner, StudioManager, Admin | Event management, QR, analytics |
| `OperatorOrAbove` | All authenticated roles | Read operations, basic event access |
| _(implicit)_ `[Authorize]` | Any authenticated | Default controller protection |
| _(none)_ | Anonymous | Guest gallery routes, face search |

---

### 4.3 Roles Matrix

| Permission | StudioOwner | StudioManager | Operator | Guest (anon) |
|-----------|:-----------:|:-------------:|:--------:|:------------:|
| View Dashboard | ✅ | ✅ | ✅ | ❌ |
| Create/Edit Events | ✅ | ✅ | ✅ | ❌ |
| Delete Events | ✅ | ✅ | ❌ | ❌ |
| Manage Studio Users | ✅ | ❌ | ❌ | ❌ |
| View Audit Logs | ✅ | ✅ | ❌ | ❌ |
| Platform Settings | ✅ | ❌ | ❌ | ❌ |
| Subscription/Billing | ✅ | ❌ | ❌ | ❌ |
| Branding / Profile | ✅ | ✅ | ❌ | ❌ |
| AI Studio | ✅ | ✅ | ✅ | ❌ |
| Browse Gallery | ❌ | ❌ | ❌ | ✅ |
| Face Search | ❌ | ❌ | ❌ | ✅ |
| Download Photos | ❌ | ❌ | ❌ | ✅ |

---

### 4.4 Authentication Weaknesses

| # | Weakness | Risk |
|---|----------|------|
| 1 | **No refresh token** — JWTs cannot be revoked, long-lived tokens are security risk | 🔴 High |
| 2 | **No token revocation** — compromised tokens remain valid until expiry | 🔴 High |
| 3 | **Legacy roles still accepted** — `Admin` and `Viewer` claims work in production | 🟡 Medium |
| 4 | **No multi-factor authentication** | 🟡 Medium |
| 5 | **Guest endpoints have no rate limiting for selfie submission** — DoS exposure on face search endpoint | 🟡 Medium |
| 6 | **JWT secret in appsettings** — should be environment variable or secrets vault | 🔴 High |
| 7 | **No React route-level auth guard** — Admin routes rely on API authorization only | 🟡 Medium |
| 8 | **CORS: AllowAnyOrigin** — appropriate for LAN/local but dangerous if exposed publicly | 🔴 High |

---

### 4.5 Expansion Readiness for Auth

| Feature | Readiness |
|---------|-----------|
| Add new roles | ✅ Easy — add to `UserRole` enum + policy |
| Permission-based access | 🟡 Needs work — currently role-only, no granular permissions |
| Multi-tenant auth | 🔴 Requires significant rework |
| OAuth/SSO | 🔴 Not present — needs new auth provider |
| API keys (studio integrations) | 🔴 Not present |
| Refresh tokens | 🟡 Infrastructure present, logic needs adding |

---

## Section 5 — Event System

### 5.1 Event Lifecycle

```
┌──────────────────────────────────────────────────────────────────────┐
│                        EVENT LIFECYCLE                               │
│                                                                      │
│  CREATE                                                              │
│  Studio selects:                                                     │
│    - Name, Type, Date, Venue, Client                                │
│    - WatchFolder (disk path)                                         │
│    - GalleryMode (GalleryOnly / FaceSearchOnly / Hybrid)            │
│    - EnableFaceRecognition, FaceMatchThreshold                       │
│    - Watermark defaults                                              │
│  → CreateEventCommand → Event.Create() [domain invariants]          │
│  → GenerateQrCodeCommand → QR image written to disk                 │
│  → FileWatcherService picks up new WatchFolder (30s poll)           │
│                                                                      │
│  ACTIVE (IsActive = true)                                            │
│  - FileWatcherService watches WatchFolder                            │
│  - Photos ingest → Thumbnails → Face Indexing                        │
│  - Gallery live for guests (if QR shared)                            │
│  - Real-time updates via SignalR                                      │
│                                                                      │
│  DEACTIVATE (IsActive = false)                                       │
│  - FileWatcher stops watching folder                                 │
│  - Gallery remains accessible to guests                              │
│  - No new photos processed                                           │
│                                                                      │
│  DELETE (soft delete: IsDeleted = true)                              │
│  - Event hidden from all admin views                                 │
│  - Gallery URL still resolves (disk files remain)                    │
│  - Photos are soft-deleted                                           │
└──────────────────────────────────────────────────────────────────────┘
```

### 5.2 Photo Lifecycle

```
Disk file appears in WatchFolder
        ↓
FileWatcherService.QueueFile()
        ↓
CreatePhotoCommand (idempotent by path)
        ↓
Photo.Create() → PhotoCreatedEvent raised
        ↓ (parallel)
┌────────────────────────────────────────────┐
│  ThumbnailProcessorService                  │
│  Photo.MarkThumbnailProcessing()           │
│  → ImageSharp resize → save .jpg thumb    │
│  Photo.MarkThumbnailDone(width, height)    │
│  → PhotoNotificationService.NotifyNew()   │
│  → SignalR: event-{eventId} group         │
└────────────────────────────────────────────┘
        ↓ (if EnableFaceRecognition)
┌────────────────────────────────────────────┐
│  AiDiscoveryPipelineService                 │
│  FaceProcessingJob created (Pending)        │
│  Pipeline:                                  │
│    Detecting → QualityChecking             │
│    → Embedding → Indexing → Completed      │
│  FaceEmbedding rows written (1 per face)   │
│  pgvector HNSW index updated               │
└────────────────────────────────────────────┘
```

### 5.3 QR Code Lifecycle

```
Event Created / Updated
        ↓
GenerateQrCodeCommand
        ↓
UrlGenerationService.GenerateGalleryUrlAsync()
  → reads PublicBaseUrl from ApplicationSettings
        ↓
QrCodeService.GenerateAsync(url, path, eventName)
  → QR image rendered with QRCoder library
  → saved to disk (absolute path)
        ↓
Event.SetQrCode(path, url) stored in DB
        ↓
On startup: IP changes →
  URL updated → QR regenerated for all events
```

### 5.4 Guest Experience Flow

```
Guest scans QR → /gallery/{eventId}
        ↓
GuestLayout (anonymous)
        ↓
┌─────────── Gallery Mode Check ────────────────────────┐
│  GalleryOnly → Standard browse gallery               │
│  FaceSearchOnly → Redirect to /gallery/{id}/find     │
│  Hybrid → Gallery with "Find My Photos" CTA          │
└──────────────────────────────────────────────────────┘
        ↓ (Face Search selected)
Guest uploads selfie → POST /api/face-search/start
  → StartFaceSearchCommand
  → Python service: detect + embed selfie (512-dim)
  → GuestFaceSession created with SelfieEmbedding
  → pgvector cosine similarity search against FaceEmbeddings
  → PhotoMatch records created
  → GuestFaceSession.MarkCompleted(matchCount)
        ↓
/gallery/{id}/results/{sessionToken}
  → Personal matched photo gallery
        ↓
Download → POST /api/photos/{id}/download
  (if RestrictDownloadsToMatchedPhotos: validate sessionToken)
  → WatermarkService applies watermark at download time
  → DownloadLog entry created
  → Photo.RecordDownload()
```

---

## Section 6 — Network Architecture

### 6.1 Network Design

PixBridge uses a **single configurable `PublicBaseUrl`** as the canonical URL for all outward-facing links. This is the core of the network architecture — every generated URL (gallery, QR, download) derives from it.

```
┌─────────────────────────────────────────────────────────────┐
│                    UrlGenerationService                      │
│                                                             │
│  Source: ApplicationSettings.PublicBaseUrl (PostgreSQL)     │
│                                                             │
│  /gallery/{eventId}          → Guest gallery               │
│  /gallery/{eventId}/find     → Face search landing         │
│  /api/photos/{id}/download   → Photo download              │
│  QR code content             → Gallery URL                 │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 Deployment Topology Support

| Scenario | Current Support | Notes |
|----------|----------------|-------|
| **Localhost** | ✅ Full | `http://localhost:5000` — default |
| **LAN (raw IP)** | ✅ Full | Auto-detected on startup. `http://192.168.x.x:5000` |
| **LAN (hostname)** | ✅ Full | `http://pixbridge.local` — manual config, no auto-update |
| **Router / Port-forward** | ✅ Partial | `http://external-ip:5000` — must be manually set |
| **Custom Domain (HTTP)** | ✅ Full | `http://photos.studio.com` — manual config |
| **Custom Domain (HTTPS)** | ✅ Full | Works behind Caddy/nginx/Cloudflare. ForwardedHeaders configured. |
| **Cloud (containerised)** | 🟡 Partial | Absolute file paths and Windows Service hosting are blockers |
| **Multi-server / LB** | 🔴 Not supported | File system shared state, single PostgreSQL, no distributed cache |

### 6.3 Network Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     DEPLOYMENT SCENARIOS                         │
│                                                                 │
│  LOCAL ────────────────────────────────────────────────────     │
│  Photographer PC → localhost:5000 → React SPA + API + Worker    │
│                                                                 │
│  LAN ──────────────────────────────────────────────────────     │
│  Photographer PC (server) ←──── LAN ────→ Guest devices        │
│  192.168.1.100:5000                        (phone QR scan)      │
│  Auto-detected on startup                                       │
│                                                                 │
│  DOMAIN ──────────────────────────────────────────────────     │
│  Internet → Caddy/nginx (HTTPS/443) → localhost:5000           │
│  ForwardedHeaders middleware translates X-Forwarded-Proto      │
│                                                                 │
│  FUTURE CLOUD ─────────────────────────────────────────────    │
│  CDN → Load Balancer → API Pods (requires refactor)            │
│  Object Storage (S3/Azure Blob) replaces local FS              │
└─────────────────────────────────────────────────────────────────┘
```

### 6.4 SignalR URL Architecture

| URL | Purpose |
|-----|---------|
| `/hubs/photos?eventId={id}` | Gallery real-time photo notifications |
| `/hubs/photos?sessionToken={token}` | Private face search progress updates |

React connects via `@microsoft/signalr` using the current `window.location.origin` as the base — no separate SignalR URL config needed.

---

## Section 7 — SignalR Architecture

### 7.1 Hub: PhotoHub

**Single hub** — `PhotoHub` at `/hubs/photos`.

```
┌─────────────────────────────────────────────────────────────┐
│                        PhotoHub                              │
│                                                             │
│  Groups:                                                    │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  event-{eventId}      ← All gallery viewers           │  │
│  │                          (admin + guests)             │  │
│  │  face-session-{token} ← Private per-guest channel     │  │
│  │                          for face search progress     │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  Client Methods Called by Server:                           │
│  • PhotoNotificationService → group: event-{id}             │
│    - "PhotoAdded" → new photo available                     │
│    - "PhotoDeleted" → photo removed                         │
│  • FaceNotificationService → group: face-session-{token}   │
│    - "FaceSearchStarted"                                    │
│    - "FaceSearchProgress" → percentage complete             │
│    - "FaceSearchCompleted" → match results                  │
│    - "FaceSearchFailed" → error state                       │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Architecture Assessment

**Strengths:**
- Single hub keeps connection management simple
- Group isolation is correct (public event group + private session group)
- OnConnectedAsync auto-joins groups from query params — seamless for client

**Weaknesses:**
- No authentication on SignalR connections — any client can join any event group
- No group size limits — large events with many concurrent guests could strain connection counts
- Hub logic is minimal — all notification intelligence is in the notification services (good separation)
- No persistence — if client disconnects during face search, progress is lost

---

## Section 8 — Photo Processing Pipeline

### 8.1 Pipeline Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                    GALLERY PIPELINE (High Priority)                  │
│                                                                      │
│  FileWatcherService (BackgroundService)                              │
│  ├── PeriodicTimer: refresh watchers every 30s                       │
│  ├── FileSystemWatcher per active event WatchFolder                  │
│  ├── SemaphoreSlim(4) — max 4 concurrent ingest operations           │
│  ├── 500ms settle delay + 5-attempt file lock check                  │
│  └── CreatePhotoCommand (idempotent by path) via MediatR             │
│                                                                      │
│  Formats: .jpg .jpeg .png .cr2 .nef .arw .dng .tiff                 │
│                                                                      │
│  ThumbnailProcessorService (BackgroundService)                        │
│  ├── PeriodicTimer polls for Pending thumbnails                       │
│  ├── ImageSharp for JPEG thumbnail generation                         │
│  ├── ProcessThumbnailCommand → Photo.MarkThumbnailDone()             │
│  └── SignalR notification → PhotoAdded                               │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│                    AI DISCOVERY PIPELINE (Async)                     │
│                                                                      │
│  AiDiscoveryPipelineService (BackgroundService)                       │
│  ├── Bounded Channel (cap 32) — in-memory priority queue             │
│  ├── Poller: every 10s pulls pending/retry-eligible FaceProcessingJobs│
│  ├── Processor: concurrent job workers                               │
│  │                                                                   │
│  │   Pending → Detecting (HTTP → Python InsightFace)                 │
│  │   Detecting → QualityChecking (FaceQualityService)                │
│  │   QualityChecking → Embedding (ArcFace 512-dim vector)            │
│  │   Embedding → Indexing (pgvector INSERT)                          │
│  │   Indexing → Completed                                            │
│  │                                                                   │
│  ├── Exponential back-off retry: 1m, 5m, 15m, 30m, 60m              │
│  ├── Max 5 retries → DeadLettered                                    │
│  └── Permanent failures (corrupt/quality): instant DeadLetter        │
│                                                                      │
│  DeadLetterProcessorService — operator manual retry/ignore           │
│  SelfieRetentionService — purges guest selfie embeddings on TTL      │
└──────────────────────────────────────────────────────────────────────┘
```

### 8.2 Watermark Pipeline (Download-time)

```
GET /api/photos/{id}/download
        ↓
DownloadPhotoQuery → WatermarkService.ApplyAsync()
        ↓
WatermarkConfiguration loaded for event
        ↓ (if enabled)
ImageSharp + WatermarkConfiguration:
  - Mode: StudioBranding / CustomText / DynamicTemplate
  - Style: Corner / BottomRibbon / Diagonal / Center
  - Opacity, Scale, TextColor, FontName
  - Template tokens: {StudioName} {EventName} {EventDate} etc.
        ↓
Watermarked image returned as stream
DownloadLog entry created
```

---

## Section 9 — Face Recognition Architecture

### 9.1 Current Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                    FACE RECOGNITION ARCHITECTURE                     │
│                                                                      │
│  INDEXING PHASE (background, per photo)                              │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  AiDiscoveryPipelineService                                  │    │
│  │  ↓                                                           │    │
│  │  HTTP POST → PixBridge.FaceRecognition (Python :5001)        │    │
│  │  InsightFace: detect faces → bounding boxes                  │    │
│  │  FaceQualityService: score each face (0-100)                 │    │
│  │    blur, brightness, resolution, pose, occlusion, face-size  │    │
│  │  ArcFace: generate 512-dim embedding per face                │    │
│  │  FaceEmbedding.Create() → INSERT INTO face_embeddings        │    │
│  │  pgvector HNSW index automatically maintained                │    │
│  └──────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  SEARCH PHASE (per guest selfie upload)                              │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  StartFaceSearchCommand                                       │    │
│  │  Guest selfie → HTTP → Python service                        │    │
│  │  InsightFace: detect face in selfie                          │    │
│  │  ArcFace: generate 512-dim selfie embedding                  │    │
│  │  SHA-256 hash check → cache hit? (skip re-embed)             │    │
│  │  pgvector cosine similarity: <=> operator                    │    │
│  │  SELECT ... ORDER BY embedding <=> selfie_vec LIMIT N        │    │
│  │    WHERE event_id = X AND quality_tier != Low                │    │
│  │    AND similarity >= FaceMatchThreshold                      │    │
│  │  PhotoMatch records inserted                                 │    │
│  │  GuestFaceSession.MarkCompleted(matchCount)                  │    │
│  │  SignalR: FaceSearchCompleted → private session group        │    │
│  └──────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
```

### 9.2 Data Structures

| Table | Key Columns | Index |
|-------|------------|-------|
| `face_embeddings` | PhotoId, EventId, Embedding (vector 512), Confidence, QualityScore, EmbeddingVersion | HNSW index via pgvector |
| `guest_face_sessions` | EventId, SessionToken, SelfieEmbedding (vector 512), SelfieHash, Status | Standard B-tree on EventId, SessionToken |
| `photo_matches` | SessionId, PhotoId, SimilarityScore | B-tree on SessionId |
| `face_processing_jobs` | PhotoId, EventId, Status, NextRetryAt, RetryCount | Index on Status + NextRetryAt |
| `face_clusters` | EventId, CentroidEmbedding (vector 512) | Future — seeded but unused |
| `ai_search_analytics` | EventId, SearchDurationMs, MatchCount | For analytics queries |

---

### 9.3 Gaps

| # | Gap | Impact |
|---|-----|--------|
| 1 | **No HNSW index parameters tuned** — default pgvector HNSW settings may be suboptimal for high photo counts | Performance at scale |
| 2 | **No vector dimension validation at DB level** — 512-dim enforced only in domain code | Data integrity risk |
| 3 | **FaceCluster entity exists but is unused** — clustering pipeline not implemented | Missing feature |
| 4 | **No selfie quality gate** — if selfie has no detectable face, error handling path needs verification | Guest UX |
| 5 | **Single Python process** — no horizontal scaling of the face recognition service | Scalability bottleneck |
| 6 | **No face recognition model versioning enforcement** — `EmbeddingVersion` stored but no auto re-indexing when model updates | Data staleness |
| 7 | **Selfie embeddings stored in primary DB** — 512 × 4 bytes = 2KB per session × many guests = table bloat | Performance |
| 8 | **No confidence threshold on search** — any face detection result feeds embedding, regardless of confidence | Accuracy risk |

---

### 9.4 Scalability Risks

- pgvector HNSW performs well up to ~1M vectors per table. Events with thousands of photos and many faces per photo could approach limits within 12 months of heavy commercial use.
- Python service is single-instance, CPU-bound. GPU acceleration not configured.
- Bounded channel (cap 32) in AiDiscoveryPipeline means bursts of photos create queue pressure.

---

### 9.5 Commercial Readiness: Face Recognition

| Aspect | Score | Notes |
|--------|-------|-------|
| Architecture quality | 8/10 | Well-modeled pipeline with proper state machine |
| Scalability | 4/10 | Single Python process, no GPU, no horizontal scale |
| Privacy compliance | 7/10 | Selfie TTL purge implemented, SelfieDeletedAt tracked |
| Accuracy | 6/10 | FaceMatchThreshold configurable, quality scoring present, but no tuning guide |
| Error handling | 8/10 | Dead-letter, retry with backoff, operator review |
| Production readiness | 5/10 | Works for small events, needs load testing for commercial scale |

---

## Section 10 — Database Architecture

### 10.1 Schema Summary

**Database:** PostgreSQL 16 + pgvector extension  
**ORM:** Entity Framework Core 8  
**Pattern:** Code-first with Fluent API configurations  
**Migrations:** 16 migrations from `InitialCreate` (2026-06-30) to `AddSubscription` (2026-08-15)

| Table | Rows at Scale | Key Indexes |
|-------|--------------|-------------|
| `events` | Hundreds | PK, IsDeleted filter, IsActive, CreatedBy |
| `photos` | Millions | PK, EventId, FaceIndexStatus, ThumbnailStatus, IsDeleted filter |
| `users` | Tens | PK, Username (unique), Email (unique) |
| `face_embeddings` | Millions (many faces per photo) | PK, PhotoId, EventId, **HNSW vector index** |
| `guest_face_sessions` | Thousands/month | PK, EventId, SessionToken, ExpiresAt |
| `photo_matches` | Millions | PK, SessionId, PhotoId |
| `face_processing_jobs` | Large | PK, Status+NextRetryAt (composite for polling) |
| `watermark_configurations` | 1:1 with events | PK, EventId (unique) |
| `application_settings` | 1 row (singleton) | PK = fixed GUID |
| `subscriptions` | 1 row (singleton) | PK = fixed GUID |
| `audit_logs` | High volume over time | PK, CreatedAt, UserId |
| `download_logs` | High volume | PK, PhotoId, CreatedAt |
| `guest_uploads` | Moderate | PK, EventId, SessionId |
| `guest_upload_sessions` | Moderate | PK, EventId |

---

### 10.2 Migration Timeline

| Date | Migration | Significance |
|------|-----------|-------------|
| 2026-06-30 | InitialCreate | Core tables: events, photos, users |
| 2026-07-03 | AddPhotoPagedIndex | Performance for paginated photo queries |
| 2026-07-14 | AddGalleryRecentCount | GalleryRecentCount column on events |
| 2026-07-17 | AddFaceRecognitionColumns | FaceIndexStatus, retry count on photos |
| 2026-07-26 | AddFaceRecognitionVectors | pgvector, face_embeddings, guest_face_sessions, photo_matches |
| 2026-07-31 | AddWatermarkConfiguration | Watermark settings table |
| 2026-07-31 | AddWatermarkColorAndFont | TextColor, FontName columns |
| 2026-07-31 | AddWatermarkRibbonFields | BackgroundOpacity, ApplyOnPreview |
| 2026-08-07 | AddApplicationSettings | Singleton ApplicationSettings table |
| 2026-08-08 | StudioRolesAndAuditLog | User role expansion, audit_logs table |
| 2026-08-08 | StudioProfileAndBranding | Studio contact + branding columns |
| 2026-08-08 | AddFeatureFlags | isWatermarkEnabled, isFaceSearchEnabled |
| 2026-08-09 | AddAiDiscoveryEngine | face_processing_jobs, face_clusters, ai_search_analytics |
| 2026-08-15 | AddStudioOsFields | OS/deployment metadata on ApplicationSettings |
| 2026-08-15 | AddGuestUploads | guest_upload_sessions, guest_uploads |
| 2026-08-15 | AddSubscription | subscriptions singleton table |

---

### 10.3 Potential Bottlenecks

| # | Concern | Impact | Mitigation |
|---|---------|--------|-----------|
| 1 | **`photo_matches` growth** — each guest search creates N rows | Table bloat over time | Add TTL cleanup job or partition by event |
| 2 | **`audit_logs` unbounded growth** | Disk/query performance | Add retention policy + archive strategy |
| 3 | **`face_embeddings` HNSW index rebuild** — adding many vectors degrades HNSW until vacuumed | Search latency spikes | Schedule `VACUUM` and monitor `pg_stat_user_tables` |
| 4 | **Full-table scans on `photos`** by FaceIndexStatus without composite index | Worker polling performance | Verify `Status+EventId` composite index exists |
| 5 | **Singleton pattern (fixed GUID PKs)** — ApplicationSettings and Subscription cannot scale to multi-tenant | Architecture limit | By design for single-studio; needs rethink for multi-tenant |
| 6 | **No read replicas** | All reads hit primary | Add read replica for analytics queries |
| 7 | **Connection pooling** — EF Core default pooling may be insufficient under load | Connection exhaustion | Configure Npgsql connection pool and PgBouncer |

---

### 10.4 EF Core Patterns

- **Fluent API configurations**: Correct — one `IEntityTypeConfiguration<T>` per entity in `Configurations/`
- **Owned types**: Not used — would benefit value objects
- **Soft deletes**: Manual `IsDeleted` filtering in repository queries (not global EF query filter)
- **Concurrency tokens**: Not observed — risk of lost-update on concurrent saves to singleton Settings

---

## Section 11 — Application Settings

### 11.1 Dual Configuration Systems

| System | Purpose | Storage | Risk |
|--------|---------|---------|------|
| `ApplicationSettings` entity | Typed platform settings, feature flags, branding | PostgreSQL singleton | Primary — should be source of truth |
| `SystemSetting` (key-value) | Legacy key-value pairs (e.g., `app.serverUrl`) | PostgreSQL table | Legacy — being superseded |
| `appsettings.json` | JWT config, DB connection string, Serilog, ThumbnailSettings | File system | Infrastructure-level config |
| `appsettings.Local.json` | Local overrides (gitignored) | File system | Dev-time overrides |
| `.env` / environment variables | Secrets in deployment | Environment | Production secrets |

### 11.2 ApplicationSettings Model

```
ApplicationSettings (singleton, ID = 00000000-...-0001)
├── Studio Identity
│   ├── StudioName
│   ├── ServerName
│   └── Studio Profile (Phone, Email, Website, Address, Social, Logo, GST)
├── Network
│   ├── PublicBaseUrl  ← Central URL for all generated links
│   └── ServerPort
├── Event Defaults
│   ├── DefaultEventGalleryMode
│   ├── EnableWatermarkByDefault
│   └── EnableFaceRecognitionByDefault
├── Feature Flags
│   ├── IsWatermarkEnabled  ← Global module on/off
│   └── IsFaceSearchEnabled ← Global module on/off
└── Branding
    ├── PrimaryColor, SecondaryColor
    ├── BrandTheme, GalleryTheme, QrTheme
    └── DefaultWatermarkProfileId
```

### 11.3 Feature Flag Architecture

Current feature flags live on `ApplicationSettings` — two boolean columns. This is a **primitive feature flag system**. 

| Flag | Controls |
|------|---------|
| `IsWatermarkEnabled` | Hides watermark UI globally; bypasses watermark at download |
| `IsFaceSearchEnabled` | Hides AI Studio sidebar section; disables face search endpoints |

**Gaps for future Feature Management:**
- No per-role feature flags
- No per-event feature flags beyond existing event-level settings
- No time-based flags (e.g., trial period expiry gating features)
- No remote feature flag service (e.g., LaunchDarkly/Unleash)
- Feature flags are UI-driven only — no API-layer enforcement middleware

---

## Section 12 — Background Services

### 12.1 Worker Service Architecture

The `EventPhoto.Worker` is a **separate process** from the API — deployed alongside but runs independently. This is architecturally correct for background work isolation.

```
┌─────────────────────────────────────────────────────────────────────┐
│                    EventPhoto.Worker                                 │
│                                                                     │
│  Hosted Services (all inherit BackgroundService):                   │
│                                                                     │
│  1. FileWatcherService                                               │
│     ├── PeriodicTimer: 30s refresh watchers                         │
│     ├── FileSystemWatcher per active event WatchFolder              │
│     ├── SemaphoreSlim(4): max 4 concurrent ingests                  │
│     └── → MediatR: CreatePhotoCommand                               │
│                                                                     │
│  2. ThumbnailProcessorService                                        │
│     ├── PeriodicTimer: polls Pending thumbnail queue                │
│     ├── ImageSharp: resize + encode JPEG thumbnail                  │
│     └── → MediatR: ProcessThumbnailCommand                          │
│                                                                     │
│  3. FaceIndexingService (legacy)                                     │
│     └── Superseded by AiDiscoveryPipelineService                    │
│         (retained for backward-compatibility gap fill)              │
│                                                                     │
│  4. AiDiscoveryPipelineService                                       │
│     ├── Bounded Channel (cap 32): in-memory priority queue          │
│     ├── Poller (10s): load Pending + retry-eligible jobs            │
│     ├── Processor: concurrent pipeline per job                      │
│     │   Detecting → QualityChecking → Embedding → Indexing          │
│     ├── Exponential back-off: 1m, 5m, 15m, 30m, 60m                │
│     └── Dead-letter promotion at 5 retries                          │
│                                                                     │
│  5. DeadLetterProcessorService                                       │
│     └── Exposes operator interface for retry/ignore dead-letters    │
│                                                                     │
│  6. SelfieRetentionService                                           │
│     └── Periodic purge of expired GuestFaceSession embeddings       │
└─────────────────────────────────────────────────────────────────────┘
```

### 12.2 Concerns

| # | Concern | Risk |
|---|---------|------|
| 1 | **FaceIndexingService (legacy) and AiDiscoveryPipelineService both exist** — dual pipelines processing same domain | Double processing risk |
| 2 | **No circuit breaker on Python service calls** — if Python service crashes, jobs retry indefinitely consuming resources | Service resilience |
| 3 | **No distributed queue** — in-memory Channel is lost on worker restart | Data loss on crash |
| 4 | **No health endpoint from Worker** — Worker health not surfaced to the API's health monitoring | Observability gap |
| 5 | **No concurrency control between API and Worker on photo records** — both write to Photo entity | Race condition risk |

---

## Section 13 — Storage Architecture

### 13.1 Storage Layout

```
Storage (Local File System — Windows paths)
│
├── {WatchFolder}/               ← Per event — source of truth (photographer drops photos here)
│   ├── IMG_001.jpg
│   ├── IMG_002.cr2
│   └── ...
│
├── {ThumbnailFolder}/           ← Per event — generated by ThumbnailProcessorService
│   ├── {photoId}.jpg            ← JPEG thumbnail (fixed size from ThumbnailSettings)
│   └── ...
│
├── {StoragePath}/qr/            ← QR code images
│   ├── {eventId}.png
│   └── ...
│
├── {StoragePath}/watermarks/    ← Watermark logo assets
│   └── ...
│
├── {StoragePath}/guest-uploads/ ← Guest uploaded photos (pending moderation)
│   ├── {sessionId}/
│   │   ├── {uploadId}.jpg
│   │   └── ...
│   └── ...
│
└── {StoragePath}/selfies/       ← Temporary guest selfies during face search
    └── (cleaned by SelfieRetentionService)
```

### 13.2 Storage Architecture Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                     STORAGE LAYER                              │
│                                                                │
│  Category        │ Location         │ Managed by              │
│  ────────────────┼──────────────────┼──────────────────────── │
│  Original Photos │ WatchFolder/*    │ Photographer (external)  │
│  Thumbnails      │ ThumbnailFolder/*│ ThumbnailProcessorService│
│  QR Codes        │ Storage/qr/*     │ QrCodeService            │
│  Guest Uploads   │ Storage/uploads/*│ GuestUpload API endpoint │
│  Selfies         │ Storage/selfies/*│ FaceSearch + Retention   │
│  Studio Logo     │ Storage/brand/*  │ Studio Profile API       │
│                                                                │
│  ALL PATHS ARE ABSOLUTE (e.g. C:\PixBridge\Events\*)         │
│  This is the single biggest constraint for cloud migration     │
└────────────────────────────────────────────────────────────────┘
```

### 13.3 Storage Concerns

| # | Concern | Risk |
|---|---------|------|
| 1 | **Absolute paths** — hard-coded OS paths prevent containerization and cloud migration | Architecture risk |
| 2 | **No storage abstraction** — `IFileStorageService` exists but file paths are stored as absolute strings in DB | Migration risk |
| 3 | **No disk usage monitoring** — no alert when disk nears capacity | Operations risk |
| 4 | **No backup strategy** — photos + PostgreSQL must be backed up together; no built-in backup tooling | Data loss risk |
| 5 | **Original photos not protected** — accessible if WatchFolder is on a shared drive | Security risk |
| 6 | **Guest uploads indefinitely retained** — no cleanup policy for rejected uploads | Disk bloat |

---

## Section 14 — User Experience Architecture

### 14.1 Application Sitemap

```
PixBridge
│
├── /login                           ← Authentication
│
├── /admin/*                         ← AdminLayout (requires JWT)
│   │
│   ├── /admin (Dashboard)           ← Overview, stats, live activity, spotlight
│   │
│   ├── EVENTS
│   │   ├── /admin/events            ← Event list + search + filter
│   │   ├── /admin/events/new        ← Create event (multi-section form)
│   │   └── /admin/events/:id        ← Event Workspace
│   │       ├── Overview Tab         ← Event details, QR code, stats
│   │       ├── Gallery Tab          ← Photo grid, download, delete
│   │       ├── Face Recognition Tab ← AI indexing status, metrics
│   │       ├── Watermark Tab        ← Watermark config per event
│   │       ├── QR Access Tab        ← QR display, regenerate
│   │       ├── Analytics Tab        ← Downloads, face search stats
│   │       └── Storage Tab          ← Storage usage
│   │
│   ├── STATISTICS                   /admin/statistics
│   │
│   ├── STUDIO
│   │   ├── /admin/studio/users      ← Studio user management (Owner only)
│   │   ├── /admin/studio/profile    ← Studio contact details
│   │   └── /admin/studio/branding   ← Colors, themes, logo
│   │
│   ├── AI
│   │   ├── /admin/ai/face-recognition  ← Face indexing overview
│   │   └── /admin/ai/studio            ← AI processing queue, dead-letters
│   │
│   ├── EXPERIENCES
│   │   └── /admin/experiences/guest-uploads  ← Guest uploads moderation
│   │       └── /admin/experiences/guest-uploads/:eventId
│   │
│   ├── PLATFORM
│   │   ├── /admin/platform/audit        ← Audit log viewer
│   │   ├── /admin/platform/network      ← Network config, URL test
│   │   ├── /admin/platform/appearance   ← UI appearance
│   │   ├── /admin/platform/configuration ← Feature flags (module on/off)
│   │   ├── /admin/platform/subscription ← License + plan management
│   │   └── /admin/deployment            ← Deployment Center
│   │
│   ├── SYSTEM SETTINGS              /admin/system-settings
│   │   ├── General Tab              ← StudioName, ServerName, PublicBaseUrl, Port
│   │   ├── Network Tab              ← Network info, URL tester
│   │   ├── Defaults Tab             ← Event creation defaults
│   │   └── Branding Tab             ← Colors, themes
│   │
│   ├── SETTINGS                     /admin/settings (Change Password)
│   ├── LOGS                         /admin/logs
│   └── HEALTH MONITORING            /admin/health
│
└── GUEST (GuestLayout — anonymous)
    ├── /gallery/:eventId            ← Public gallery
    ├── /gallery/:eventId/find       ← Face search landing (selfie upload)
    ├── /gallery/:eventId/search/:token  ← Search in progress (SignalR)
    └── /gallery/:eventId/results/:token ← My matched photos + downloads
```

### 14.2 Navigation Feature Flags

The Sidebar uses `useFeatureFlags()` + `hasMinRole()` to show/hide nav items:
- AI Studio section hidden when `isFaceSearchEnabled = false`
- Watermark-related settings hidden when `isWatermarkEnabled = false`
- Studio Users visible only to StudioOwner
- Subscription page visible to StudioOwner only

### 14.3 UX Architecture Observations

**Strengths:**
- Clean separation: `AdminLayout` vs `GuestLayout`
- Feature-flag-driven navigation avoids showing disabled modules
- TanStack Query with 30s stale time — efficient API usage
- Framer Motion animations for professional feel
- Role-based nav visibility in Sidebar

**Gaps:**
- No loading state management at route level (React Suspense not used)
- No offline support / PWA capability for gallery guests
- `System Settings` and `Platform → Configuration` overlap (documented earlier)
- No mobile-optimized admin view (guest gallery is fine, admin is desktop-first)

---

## Section 15 — Commercial Product Readiness

| Feature Area | Score | Assessment |
|-------------|-------|-----------|
| **Studio Users** | 7/10 | Domain model complete (3 roles + legacy). CRUD implemented. Missing: per-user permissions beyond role, user invitation flow, password reset email. |
| **Branding** | 7/10 | Colors, themes, logo, QR themes, gallery themes all present. Missing: white-label domain, per-event branding override, email template branding. |
| **Licensing** | 5/10 | `Subscription` entity, `SubscriptionPlan`, `SubscriptionState`, grace period all modeled. Missing: enforcement middleware, license key validation service, webhook for renewals. |
| **Feature Flags** | 4/10 | Two global boolean flags exist. Missing: per-plan flags, runtime enforcement, fine-grained flags per feature. |
| **Find My Photos™** | 7/10 | Full pipeline implemented. Selfie search, pgvector, session tokens, SignalR progress. Missing: GPU acceleration, scalability, photo delivery quality tuning. |
| **Guest Memories™** | 5/10 | Guest uploads exist with moderation. Missing: guest photo story/curation, social sharing, gallery customization by guest, notification to guest when approved. |
| **Analytics** | 6/10 | Download logs, AI search analytics, event stats, face analytics, storage analytics all present. Missing: exportable reports, date-range filtering, trend analysis. |
| **Deployment Center** | 6/10 | `DeploymentCenterPage` exists. Network info, URL test, QR regeneration. Missing: backup/restore, update management, health dashboards, Docker support docs. |
| **Subscription Platform** | 3/10 | Data model exists. Missing: payment gateway integration, automated license key validation, plan upgrade/downgrade flows, invoice generation. |
| **Multi-Branch Support** | 1/10 | No multi-tenant architecture. Singleton `ApplicationSettings` and `Subscription`. All storage in single DB schema. Would require significant rearchitecture. |
| **Cloud Migration** | 2/10 | Absolute file paths, Windows-only service hosting, single-node PostgreSQL, no containerization. Foundation exists to migrate (clean architecture, `IFileStorageService` interface) but not done. |

**Overall Commercial Readiness: 5/10**

The core platform mechanics are strong. The biggest gaps are subscription enforcement, multi-tenancy, cloud storage, and payment integration.

---

## Section 16 — Extensibility Review

### 16.1 Extension Difficulty Assessment

| Feature | Difficulty | Best Extension Points | Notes |
|---------|-----------|----------------------|-------|
| **1. Licensing Engine** | Medium | `Subscription` entity already exists. Add `ILicenseValidationService` in Application, implement in Infrastructure. Add `SubscriptionMiddleware` in API. | Data model: ✅. Enforcement: missing. |
| **2. Feature Management** | Medium | Replace `IsWatermarkEnabled`/`IsFaceSearchEnabled` on `ApplicationSettings` with a `FeatureFlag` entity table. Add `IFeatureFlagService` + pipeline behavior. | Requires DB migration + service layer. |
| **3. Feature Flags (per plan)** | Medium | Extend `SubscriptionPlan` with a feature permission matrix. `IFeatureFlagService.IsEnabled(feature, plan)`. | New service + plan-feature mapping table. |
| **4. Usage Tracking** | Easy | `DownloadLog` and `AiSearchAnalytics` already exist. Add `UsageEvent` entity for generic event tracking. Background aggregation job. | Extend existing pattern. |
| **5. Audit Logging** | Easy | `AuditLog` entity + `IAuditService` + `AuditService` already implemented. Add to more command handlers. | Exists — needs wider adoption. |
| **6. Studio Management** | Easy | `User` entity + CRUD + roles already present. Add user invitation (email token), bulk role management. | Foundation solid. |
| **7. Branding Center** | Easy | `ApplicationSettings` branding fields already exist. Extend: per-event theme override, email templates, custom CSS variables. | Foundation solid. |
| **8. AI Studio** | Medium | `AiDiscoveryPipelineService` and `FaceRecognitionPage` exist. Add: GPU support config, model selection, batch reprocessing, confidence tuning UI. | Pipeline is production-quality. |
| **9. Guest Memories™** | Medium | `GuestUpload` + `GuestUploadSession` exists. Add: guest profile (email/name), photo album curation, push/email notification, shareable guest link. | CRUD exists, experience layer missing. |
| **10. Subscription Platform** | Hard | `Subscription` entity exists. Need: payment gateway (Stripe/Razorpay), webhook handlers, license key generation service, upgrade/downgrade flows, invoice system. | Data: ✅. Payments: requires full build. |

### 16.2 Best Extension Points Summary

```
┌─────────────────────────────────────────────────────────────────────┐
│  GREENFIELD EXTENSION POINTS                                        │
│                                                                     │
│  1. MediatR Pipeline Behaviors                                      │
│     → Add subscription enforcement, feature flag checking,         │
│       usage tracking as transparent pipeline behaviors              │
│                                                                     │
│  2. IFileStorageService                                             │
│     → Replace local file system with S3/Azure Blob                 │
│       without changing any domain or application code               │
│                                                                     │
│  3. Domain Events (once dispatcher is implemented)                  │
│     → React to EventCreated, PhotoCreated for                       │
│       usage tracking, email notifications, webhooks                 │
│                                                                     │
│  4. ApplicationSettings Feature Flags                               │
│     → Evolve to a full FeatureFlag table for plan-based            │
│       and per-studio feature control                                │
│                                                                     │
│  5. Subscription Entity                                             │
│     → Wire to API middleware for real-time enforcement              │
│                                                                     │
│  6. AuditService                                                    │
│     → Already present — add to all mutating command handlers        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Section 17 — Risks & Technical Debt

### 17.1 Architecture Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| A1 | **Domain Events not dispatched** | 🔴 Critical | Events raised by aggregates are cleared in SaveChangesAsync without being dispatched to MediatR handlers. Side effects (notifications, audit, etc.) dependent on domain events will never fire. |
| A2 | **Subscription not enforced** | 🔴 Critical | `Subscription.IsOperational` and plan limits are modeled but never checked in API middleware or command handlers. Any expired studio has full unrestricted access. |
| A3 | **Dual pipeline (legacy FaceIndexing + AiDiscovery)** | 🔴 High | Two background services potentially process the same photos. Risk of duplicate embeddings and conflicting state transitions. |
| A4 | **Singleton pattern at domain scale** | 🟡 Medium | `ApplicationSettings` and `Subscription` use fixed GUIDs. Works for single studio, breaks for multi-tenant without redesign. |
| A5 | **Absolute file paths in domain** | 🟡 Medium | Prevents Docker deployment, cloud migration, and backup/restore portability. |

### 17.2 Scalability Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| S1 | **Single Python face recognition process** | 🔴 High | CPU-bound, single instance. Concurrent events with heavy photo ingest will queue. No GPU, no worker pools. |
| S2 | **In-memory Channel for AI jobs** | 🟡 Medium | BoundedChannel(32) lost on worker restart. No persistence between restarts. |
| S3 | **pgvector HNSW growth** | 🟡 Medium | At scale (1M+ embeddings), HNSW index maintenance degrades. Needs monitoring and periodic VACUUM. |
| S4 | **FileSystemWatcher limitations** | 🟡 Medium | Windows FileSystemWatcher misses events under high I/O load. The 30s rescan mitigates but doesn't eliminate. |
| S5 | **No horizontal scaling** | 🔴 High | Single API + single Worker + local file system = scale-up only, no scale-out. |

### 17.3 Performance Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| P1 | **Watermark applied on every download** | 🟡 Medium | CPU-intensive ImageSharp operation per download. No caching of watermarked images. Under concurrent downloads this becomes a bottleneck. |
| P2 | **photo_matches table growth** | 🟡 Medium | Each guest search inserts N rows. No TTL or cleanup. Queries degrade as table grows. |
| P3 | **No DB connection pooling configuration** | 🟡 Medium | Default EF Core / Npgsql pooling may be insufficient under peak load. |
| P4 | **Full scans in repositories** | 🟡 Medium | `GetAllAsync()` patterns without pagination may cause memory pressure at scale. |

### 17.4 Security Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| SEC1 | **CORS: AllowAnyOrigin** | 🔴 High | Acceptable for LAN, catastrophic if publicly exposed. |
| SEC2 | **No token revocation** | 🔴 High | Compromised JWT tokens valid until expiry. No blocklist. |
| SEC3 | **JWT secret in appsettings.json** | 🔴 High | Should be in environment variable or secrets vault, never in committed config files. |
| SEC4 | **No rate limiting on face search** | 🟡 Medium | Selfie upload endpoint has no throttling. DoS vector. |
| SEC5 | **No input sanitization validation on file uploads** | 🟡 Medium | Guest uploads accept files — MIME type checked but no antivirus or content validation. |
| SEC6 | **Admin routes not explicitly protected at React level** | 🟡 Medium | Backend is protected by JWT, but React routing doesn't enforce. XSS could expose admin UI. |

### 17.5 Deployment Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| D1 | **Windows Service hosting** | 🟡 Medium | `UseWindowsService()` prevents Docker/Linux deployment without code change. |
| D2 | **No zero-downtime deployment** | 🟡 Medium | No blue/green or rolling deployment strategy. Updates require service restart. |
| D3 | **API and Worker must be deployed together** | 🟡 Medium | Tight coordination required between API and Worker deployments. |
| D4 | **DB migration auto-run needed** | 🟡 Medium | No explicit migration run on startup — requires manual `dotnet ef database update` or explicit code. |

### 17.6 Maintenance Risks

| # | Risk | Severity | Description |
|---|------|----------|-------------|
| M1 | **Legacy role system still active** | 🟡 Medium | `Admin`/`Viewer` roles in production tokens creates two code paths indefinitely. |
| M2 | **Dual settings systems** | 🟡 Medium | `SystemSetting` KV store + `ApplicationSettings` typed entity maintained in parallel. |
| M3 | **No test coverage visible** | 🟡 Medium | No test project found in solution. No unit tests for domain logic or application handlers. |
| M4 | **Python service version pinning** | 🟡 Medium | InsightFace version not pinned in requirements. Model updates may break embedding compatibility. |

---

## Section 18 — Final Output

### 18.1 Executive Summary

PixBridge is a **well-architected, commercially promising photography event management platform** built on solid Clean Architecture foundations. The domain model is rich and expressive, the CQRS pipeline is implemented correctly, and the AI face recognition pipeline demonstrates genuine engineering sophistication.

**The platform is production-ready for single-studio, local/LAN deployment** and delivers excellent value in its current form.

However, the path to becoming a **commercial SaaS product** requires addressing several critical gaps:

1. **Subscription enforcement is modeled but not enforced** — studios can operate expired licenses
2. **Domain events are raised but never dispatched** — a subtle but critical wiring bug
3. **Absolute file paths block cloud and container deployment**
4. **Single Python face recognition process limits commercial scale**
5. **No payment/licensing infrastructure despite the data model being ready**
6. **Multi-branch/multi-tenant is architecturally absent** — not just incomplete, it would require rearchitecture

The 12-month roadmap below addresses these in business-value order.

---

### 18.2 High-Level Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           PIXBRIDGE PLATFORM                                 │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                      EventPhoto.React (SPA)                            │  │
│  │  Admin UI (Dashboard, Events, Studio, Platform, AI, Experiences)       │  │
│  │  Guest UI (Gallery, Face Search, My Photos, Guest Uploads)             │  │
│  │  Vite · React 18 · TanStack Query · Zustand · Framer Motion           │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                    ↕ HTTP/REST/SignalR                        │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │                       EventPhoto.Api                                  │    │
│  │  ASP.NET Core 8 · JWT Auth · SignalR · Rate Limiting · CORS         │    │
│  │  Serves React SPA as static files · Windows Service                  │    │
│  │  Controllers: Auth, Events, Photos, FaceSearch, Watermark,           │    │
│  │    GuestUpload, Settings, Statistics, Users, Subscription, AI        │    │
│  └──────────┬───────────────────────────────────────────┬──────────────┘    │
│             ↕ MediatR CQRS                              ↕ MediatR CQRS       │
│  ┌──────────▼───────────────┐    ┌────────────────────▼──────────────────┐  │
│  │   EventPhoto.Application │    │       EventPhoto.Worker               │  │
│  │   Commands · Queries     │    │   FileWatcherService                   │  │
│  │   Validators · Mappings  │    │   ThumbnailProcessorService            │  │
│  │   Pipeline Behaviors     │    │   AiDiscoveryPipelineService           │  │
│  └──────────┬───────────────┘    │   DeadLetterProcessorService          │  │
│             ↕                    │   SelfieRetentionService               │  │
│  ┌──────────▼───────────────────────────────────────────────────────────┐  │
│  │                   EventPhoto.Infrastructure                           │  │
│  │  EF Core · Repositories · JWT · BCrypt · ImageSharp · QRCoder       │  │
│  │  UrlGenerationService · NetworkInformationService · AuditService     │  │
│  │  FaceRecognitionService (HTTP client to Python)                      │  │
│  └──────────┬────────────────────────────────────┬────────────────────-─┘  │
│             ↕                                    ↕ HTTP                      │
│  ┌──────────▼──────────┐             ┌───────────▼─────────────────────┐   │
│  │  PostgreSQL 16      │             │  PixBridge.FaceRecognition      │   │
│  │  + pgvector ext.    │             │  Python · InsightFace · ArcFace │   │
│  │  HNSW vector index  │             │  Port :5001 (localhost)         │   │
│  └─────────────────────┘             └─────────────────────────────────┘   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Local File System                                   │  │
│  │  WatchFolders (originals) · ThumbnailFolders · QR Images             │  │
│  │  Guest Uploads · Selfies (TTL purged) · Studio Logo                  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

### 18.3 Domain Architecture Diagram

```
DOMAIN LAYER (EventPhoto.Domain — zero external dependencies)
┌───────────────────────────────────────────────────────────────────────┐
│                                                                       │
│  CORE AGGREGATES                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────────────┐ │
│  │    Event     │  │    Photo     │  │  FaceProcessingJob          │ │
│  │  Rich DDD    │  │  Rich DDD    │  │  Full state machine         │ │
│  │  Aggregate   │  │  Aggregate   │  │  5-stage pipeline           │ │
│  └──────────────┘  └──────────────┘  └─────────────────────────────┘ │
│                                                                       │
│  ┌──────────────────────┐  ┌────────────────────────────────────────┐ │
│  │   ApplicationSettings │  │           Subscription               │ │
│  │   Singleton (fixed ID)│  │   Singleton (fixed ID)               │ │
│  │   Feature flags       │  │   Plan/State/LicenseKey/Limits       │ │
│  │   Branding/Defaults   │  │   Grace period / IsOperational       │ │
│  └──────────────────────┘  └────────────────────────────────────────┘ │
│                                                                       │
│  ┌─────────────────────────┐  ┌───────────────────────────────────┐  │
│  │  WatermarkConfiguration  │  │      GuestUploadSession          │  │
│  │  Per-event, 1:1          │  │      + GuestUpload               │  │
│  └─────────────────────────┘  └───────────────────────────────────┘  │
│                                                                       │
│  SUPPORTING ENTITIES                                                  │
│  User · FaceEmbedding · GuestFaceSession · PhotoMatch                │
│  FaceCluster · AiSearchAnalytics · AuditLog · DownloadLog            │
│  SystemSetting (legacy)                                               │
│                                                                       │
│  DOMAIN EVENTS                                                        │
│  EventCreated · EventDeactivated · PhotoCreated · PhotoDeleted       │
│  FaceIndexCompleted · FaceSearchCompleted                             │
│                                                                       │
│  VALUE OBJECTS                                                        │
│  FilePath · FileSize                                                  │
└───────────────────────────────────────────────────────────────────────┘
```

---

### 18.4 Database Architecture Summary

| Aspect | Current State |
|--------|--------------|
| Engine | PostgreSQL 16 |
| Vector | pgvector extension, HNSW index, 512-dim ArcFace embeddings |
| ORM | EF Core 8, code-first, Fluent API |
| Migrations | 16 applied, well-sequenced |
| Tables | 18 entities mapped |
| Patterns | Repository pattern, UoW, soft deletes |
| Risks | No read replicas, no partitioning, unbounded audit/download/match tables, no connection pool config |

---

### 18.5 Infrastructure Architecture Summary

| Component | Technology | Concern |
|-----------|-----------|---------|
| Web Host | ASP.NET Core 8 | Windows Service lock-in |
| ORM | Entity Framework Core 8 + Npgsql | Good |
| Auth | JWT Bearer (BCrypt passwords) | No refresh, no revocation |
| Real-time | SignalR | No auth on hub |
| File I/O | System.IO + ImageSharp | Absolute paths |
| QR Codes | QRCoder | Good |
| Thumbnails | ImageSharp | Good |
| Watermarks | ImageSharp | No caching — reprocessed per download |
| Face AI | Python/InsightFace HTTP client | Single process bottleneck |
| Background | .NET BackgroundService | No distributed queue |
| Logging | Serilog | Good |
| Rate Limiting | ASP.NET Core RateLimiter | Login + downloads covered |

---

### 18.6 Network Architecture Summary

| Scenario | Status |
|----------|--------|
| Localhost | ✅ Auto-configured |
| LAN (IP) | ✅ Auto-detected on startup |
| LAN (hostname) | ✅ Manual config |
| HTTPS via reverse proxy | ✅ ForwardedHeaders configured |
| Multi-server | 🔴 Not supported |
| Cloud | 🔴 Requires file storage abstraction + containerization |

**Core strength:** `UrlGenerationService` derives all URLs from `PublicBaseUrl` stored in DB. One config change updates all QR codes, gallery links, and download URLs.

---

### 18.7 Security Architecture Summary

| Area | Status | Priority |
|------|--------|---------|
| Authentication | JWT Bearer | 🟡 Missing refresh + revocation |
| Password hashing | BCrypt | ✅ Good |
| CORS | AllowAnyOrigin | 🔴 Must restrict for production |
| JWT secret storage | appsettings.json | 🔴 Must move to env/vault |
| Rate limiting | Login (5/min) + Downloads (30/min) | 🟡 Missing on face search |
| Guest access | Fully anonymous | 🟡 Consider optional event PIN |
| Input validation | FluentValidation on commands | ✅ Good |
| File upload validation | MIME only | 🟡 Add content scanning |
| SQL injection | EF Core parameterized | ✅ Protected |
| Token revocation | None | 🔴 High priority |
| Audit logging | Present | 🟡 Not universally applied |

---

### 18.8 Face Recognition Assessment

| Dimension | Score | Assessment |
|-----------|-------|-----------|
| Pipeline design | 9/10 | 5-stage state machine, dead-letter, exponential backoff — excellent |
| Privacy design | 8/10 | TTL purge of selfie embeddings, SelfieDeletedAt tracking |
| Scalability | 3/10 | Single Python process, no GPU, no horizontal scale |
| Search accuracy | 7/10 | Quality scoring, configurable threshold, quality-tier filtering |
| Commercial readiness | 5/10 | Works well for small events, untested at commercial scale |
| Future-proofing | 7/10 | `EmbeddingVersion` field, `FaceCluster` table ready, modular pipeline |

---

### 18.9 Extensibility Assessment

| Dimension | Assessment |
|-----------|-----------|
| Adding new domain entities | Easy — add entity, repository interface, EF config, migration |
| Adding new features (commands/queries) | Easy — add MediatR command/query handler |
| Extending auth (new roles, permissions) | Medium — role system is in place |
| Cloud storage migration | Medium — `IFileStorageService` exists, paths need abstraction |
| Payment/subscription integration | Medium-Hard — domain ready, infrastructure needs full build |
| Multi-tenancy | Hard — requires tenant isolation throughout domain, DB, storage |
| Microservice decomposition | Hard — well-suited architecturally but file system coupling blocks it |

---

### 18.10 Commercial Readiness Assessment

| Phase | Status |
|-------|--------|
| Single Studio — Local | ✅ Ready now |
| Single Studio — LAN | ✅ Ready now |
| Single Studio — Cloud (SaaS) | 🟡 6-9 months with focused effort |
| Multi-Studio — SaaS | 🔴 12-18 months (requires multi-tenancy rearchitecture) |
| Commercial Licensing/Payments | 🟡 3-6 months (domain ready, payments need integration) |

---

### 18.11 Technical Debt Assessment

**Critical Debt (fix before commercial launch):**

1. Wire domain event dispatcher in `AppDbContext.SaveChangesAsync()`
2. Add subscription enforcement middleware
3. Move JWT secret to environment variable / secrets manager
4. Restrict CORS to known origins in production
5. Retire legacy `FaceIndexingService` after confirming `AiDiscoveryPipelineService` covers all cases
6. Add rate limiting to selfie upload / face search endpoint

**High Debt (fix in next 3-6 months):**

7. Abstract file paths — introduce `IFileStorageService` with relative-path abstraction
8. Add refresh token support
9. Consolidate `SystemSetting` and `ApplicationSettings` — one source of truth
10. Add audit logging to all mutating command handlers
11. Implement subscription limit enforcement (MaxEvents, MaxUsersPerStudio)
12. Write unit tests for domain aggregates

**Medium Debt (6-12 month horizon):**

13. Remove `Admin`/`Viewer` legacy role support after migration
14. Add watermark output caching
15. Add cleanup jobs for `photo_matches`, `audit_logs`, `download_logs`
16. Configure PgBouncer / explicit connection pool settings
17. Add HNSW index monitoring and tuning

---

### 18.12 Recommended Next 12-Month Roadmap

```
Q1 (Months 1-3): FOUNDATION & COMMERCIAL SAFETY
────────────────────────────────────────────────
• Wire MediatR domain event dispatcher [CRITICAL]
• Subscription enforcement middleware [CRITICAL]
• Move secrets to env vars / vault [CRITICAL]
• Restrict CORS in production [CRITICAL]
• Add refresh tokens [HIGH]
• Rate limit face search endpoint [HIGH]
• Unit test domain aggregates + key commands [HIGH]
• Retire legacy FaceIndexingService [HIGH]

Q2 (Months 4-6): LICENSING & COMMERCIAL FOUNDATION
────────────────────────────────────────────────────
• License key generation & validation service
• Stripe/Razorpay payment gateway integration
• Plan upgrade/downgrade flows
• Per-plan feature flag matrix
• Invoice/receipt generation
• Email notification service (license events, expiry warnings)
• Subscription enforcement at API layer (plan limits)
• Studio user invitation flow (email token)

Q3 (Months 7-9): SCALE & CLOUD READINESS
──────────────────────────────────────────
• Abstract file storage → IFileStorageService (relative paths)
• S3/Azure Blob Storage implementation
• Docker containerization (API + Worker + Python)
• Remove Windows Service dependency (or make optional)
• Watermark output caching
• Audit logging universal adoption
• Guest Memories™ complete feature (curation, notification)
• pgvector HNSW tuning + monitoring

Q4 (Months 10-12): MULTI-BRANCH & ANALYTICS
──────────────────────────────────────────────
• Multi-tenant design spike and prototype
• Branch/studio isolation design
• Advanced analytics (date-range, export, trends)
• Mobile admin PWA
• AI accuracy tuning dashboard
• GPU face recognition service option
• Rate limiting refinement
• Performance testing + load testing
• Public beta readiness checklist
```

---

### 18.13 Top 20 Improvements — Ranked by Business Value

| Rank | Improvement | Business Value | Effort | Priority |
|------|-------------|---------------|--------|---------|
| 1 | **Wire domain event dispatcher** | Unlocks notifications, audit, side effects | Low | 🔴 Critical |
| 2 | **Subscription enforcement middleware** | Prevents expired studios from operating free | Low-Med | 🔴 Critical |
| 3 | **Move JWT secret to environment/vault** | Security — prevents key compromise | Low | 🔴 Critical |
| 4 | **Restrict CORS to known origins** | Security — required for any public deployment | Low | 🔴 Critical |
| 5 | **Payment gateway integration (Stripe)** | Revenue — enables actual commercial sales | High | 🔴 High |
| 6 | **License key validation service** | Commercial viability — links payment to access | Med | 🔴 High |
| 7 | **Add refresh tokens** | UX + Security — eliminates forced re-login | Med | 🟡 High |
| 8 | **File storage abstraction (relative paths)** | Cloud migration enabler — removes biggest blocker | High | 🟡 High |
| 9 | **Docker containerization** | Deployment flexibility — unlocks cloud + CI/CD | High | 🟡 High |
| 10 | **Rate limit face search / selfie upload** | Security + stability under load | Low | 🟡 High |
| 11 | **Unit test domain aggregates** | Quality — catches regressions as platform grows | Med | 🟡 High |
| 12 | **Watermark output caching** | Performance — removes per-download CPU spike | Med | 🟡 Medium |
| 13 | **Per-plan feature flag matrix** | Commercial — enables tiered plan differentiation | Med | 🟡 Medium |
| 14 | **Studio user invitation flow** | UX — enables professional studio onboarding | Med | 🟡 Medium |
| 15 | **Retire legacy FaceIndexingService** | Technical debt — simplifies codebase | Low | 🟡 Medium |
| 16 | **Consolidate SystemSetting + ApplicationSettings** | Maintainability — single source of truth | Low | 🟡 Medium |
| 17 | **Universal audit logging** | Compliance — all mutating operations tracked | Med | 🟡 Medium |
| 18 | **Guest Memories™ completion** | Product — differentiating guest experience feature | High | 🟡 Medium |
| 19 | **Subscription limit enforcement (MaxEvents, MaxUsers)** | Commercial integrity — plan limits respected | Med | 🟡 Medium |
| 20 | **Multi-tenant architecture design spike** | Strategic — enables agency/multi-studio market | Very High | 🟠 Strategic |

---

*End of Architecture Assessment*

---

**Document metadata:**

| Field | Value |
|-------|-------|
| Assessment type | Full codebase read-only architecture review |
| Codebase size | 407 .cs files, 154 .tsx files, 16 DB migrations |
| Solution | PixBridge (EventPhoto.*) |
| Framework | .NET 8, React 18, PostgreSQL 16, pgvector |
| Architecture | Clean Architecture, CQRS (MediatR), DDD |
| Assessment date | 2026-08-20 |
| Reviewer | Principal Architecture Review (AI-assisted) |
| Status | Read-only — no code modified during this assessment |
