# PixBridge — Master Project Reference
## 1. Project Overview
**Project:** PixBridge  
**Purpose:** Fully offline, LAN-based Event Photo Sharing Platform for photography studios  
**Repo:** https://github.com/gnans006/PixBridge  
**Solution file:** `PixBridge.sln`  
**Root path:** `C:\CLS\PixBridge`

PixBridge is a local-network photo sharing system for event studios. It watches folders for new photos, stores metadata in PostgreSQL, generates thumbnails, exposes guest galleries over LAN, and gives admins an operational UI for events, settings, logs, health, and statistics. The runtime is split into:
- `EventPhoto.Api` — REST API + SignalR hub + SPA host
- `EventPhoto.Worker` — file watcher + thumbnail processor
- `EventPhoto.React` — frontend source, built into API `wwwroot`

Key traits:
- No internet required for normal operation
- Guests access galleries over LAN or QR code
- Admins use JWT-authenticated UI
- Photos are discovered automatically from watched folders
- Deploys as Windows Services

## 2. Business Requirements  
- Run fully inside a venue LAN
- Support multiple event types and independent watch folders
- Let admins create, update, activate, deactivate, and delete events
- Generate per-event QR codes that point guests to the gallery
- Detect new image files automatically
- Generate thumbnails for fast browsing
- Let guests browse and download without accounts
- Track downloads for analytics
- Expose admin dashboard, event stats, logs, health, and settings
- Publish as self-contained Windows x64 binaries

## 3. System Architecture
PixBridge follows layered backend architecture plus a separate worker and React frontend.
### 3.1 Architecture Diagram (ASCII)
```text
Guests/Admins on LAN
        │  HTTP / SignalR
        ▼
┌─────────────────────────────┐
│      EventPhoto.Api         │
│ ASP.NET Core Web API        │
│ Controllers + Swagger       │
│ PhotoHub + SPA static host  │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│   EventPhoto.Application    │
│ CQRS, MediatR, Validators,  │
│ AutoMapper, service ports   │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│     EventPhoto.Domain       │
│ Entities, value objects,    │
│ events, Result<T>, repos    │
└──────────────┬──────────────┘
               ▲
               │ implements
┌──────────────┴──────────────┐
│  EventPhoto.Infrastructure  │
│ EF Core, PostgreSQL, repos, │
│ JWT, BCrypt, QR, thumbnails │
└──────────────┬──────────────┘
               │
               ▼
          PostgreSQL

┌─────────────────────────────┐
│     EventPhoto.Worker       │
│ FileWatcherService          │
│ ThumbnailProcessorService   │
└──────────────┬──────────────┘
               │
               ▼
      Local event folders
```
### 3.2 Clean Architecture Layer Map
Dependency direction:
```text
API ─────► Application ─────► Domain
Worker ──► Application ─────► Domain
Infrastructure ─────────────► Application + Domain
Contracts: shared DTO layer
React: consumes HTTP/SignalR only
```
Rules:
- Domain depends on nothing above it
- Application contains use cases, not HTTP or EF code
- Infrastructure implements repository/service abstractions
- API owns HTTP/auth/middleware/SignalR/static hosting
- Worker owns background polling/watching
### 3.3 Photo Flow (end-to-end pipeline)
1. Admin creates an `Event`
2. Event stores `WatchFolder`, `ThumbnailFolder`, and QR metadata
3. Worker refreshes active events every 30 seconds
4. `FileWatcherService` creates a `FileSystemWatcher` per active watch folder
5. New image appears in a watch folder
6. Worker validates extension and waits until the file is readable
7. Worker dispatches `CreatePhotoCommand`
8. Application stores `Photo` with `ThumbnailStatus.Pending`
9. `ThumbnailProcessorService` polls every 5 seconds in batches of 10
10. Worker loads thumbnail settings and `app.serverUrl` from `SystemSettings`
11. `ThumbnailService` generates JPEG thumbnail via ImageSharp
12. Photo is marked `Done` with width/height or `Failed` on error
13. Notification is sent through `IPhotoNotificationService`
14. API implementation broadcasts `photo:new` or `photo:deleted` through SignalR
15. Guest gallery receives live updates via `PhotoHub`
16. Downloads hit `/api/photos/{id}/download`, increment counters, and create `DownloadLog`
### 3.4 Network Topology
- Host laptop static IP: `192.168.10.10`
- API port: `80`
- Kestrel bind: `http://0.0.0.0:80`
- Guest entry URL: `http://192.168.10.10`
- All traffic stays inside LAN
- Windows Service names: `PixBridgeApi`, `PixBridgeWorker`

## 4. Solution Structure (every file with purpose)
### 4.1 .NET Projects
#### Root files
| File(s) | Purpose |
|---|---|
| `PixBridge.sln` | Main .NET solution; contains `EventPhoto.Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`, `Worker` |
| `NuGet.Config` | Clears default package sources and points to `https://api.nuget.org/v3/index.json` |
| `.gitattributes`, `.gitignore` | Git behavior and ignore rules |
#### EventPhoto.Api
| File(s) | Purpose |
|---|---|
| `src\EventPhoto.Api\EventPhoto.Api.csproj` | API project definition |
| `Program.cs` | API bootstrap, DI, middleware, Swagger, rate limiting, static file hosting, DB seeding |
| `Extensions\JwtExtensions.cs` | `AddJwtAuthentication(...)`, `AddSwaggerWithJwt()` |
| `Middleware\ExceptionHandlingMiddleware.cs` | Converts exceptions to JSON `ApiResponse` payloads |
| `Hubs\PhotoHub.cs` | SignalR hub; event group join/leave logic |
| `Services\PhotoNotificationService.cs` | `IPhotoNotificationService` implementation using `IHubContext<PhotoHub>` |
| `Controllers\AuthController.cs` | `/api/auth/login`, `/change-password`, `/me` |
| `Controllers\EventsController.cs` | Event CRUD, activation toggle, QR code endpoint |
| `Controllers\PhotosController.cs` | Gallery listing, single photo, thumbnail, download, delete |
| `Controllers\StatisticsController.cs` | Dashboard and per-event stats |
| `Controllers\SettingsController.cs` | List/update settings |
| `Controllers\HealthController.cs` | Anonymous health check |
| `Controllers\LogsController.cs` | Recent Serilog log reader for admin UI |
| `appsettings.json`, `appsettings.Development.json` | API runtime configuration |
| `EventPhoto.Api.http` | Local HTTP scratch file |
| `Properties\launchSettings.json` | Launch profiles |
| `wwwroot\index.html`, `wwwroot\favicon.svg`, `wwwroot\icons.svg`, `wwwroot\assets\index-DQ9Qm9Wz.js`, `wwwroot\assets\index-Bfay1h3M.css` | Built React SPA assets served by API |
#### EventPhoto.Application
| File(s) | Purpose |
|---|---|
| `EventPhoto.Application.csproj` | Application project definition |
| `DependencyInjection.cs` | Compatibility wrapper delegating to extension registration |
| `Extensions\ApplicationServiceExtensions.cs` | Registers MediatR, FluentValidation, AutoMapper, pipeline behaviors |
| `Common\Behaviors\ValidationBehavior.cs` | Runs validators before handlers |
| `Common\Behaviors\LoggingBehavior.cs` | Logs request type and elapsed time; warns above 500ms |
| `Common\Interfaces\IPasswordHasher.cs`, `IJwtTokenService.cs`, `IFileStorageService.cs`, `IThumbnailService.cs`, `IQrCodeService.cs`, `IPhotoNotificationService.cs` | Primary service interfaces |
| `Common\Interfaces\ITokenService.cs`, `IImageService.cs`, `IFileService.cs`, `IPasswordService.cs` | Additional non-primary interfaces |
| `Common\Models\PagedResult.cs`, `DownloadResult.cs` | Shared application models |
| `Auth\Commands\LoginCommand.cs`, `ChangePasswordCommand.cs`, validators | Authentication use cases |
| `Events\Commands\CreateEventCommand.cs`, `UpdateEventCommand.cs`, `DeleteEventCommand.cs`, `ToggleEventActiveCommand.cs`, `ActivateEventCommand.cs`, `DeactivateEventCommand.cs`, `GenerateQrCodeCommand.cs`, validator | Event write use cases |
| `Events\Queries\GetEventsQuery.cs`, `GetEventByIdQuery.cs`, `GetAllActiveEventsQuery.cs`, `GetEventWithPhotosQuery.cs` | Event read use cases |
| `Photos\Commands\CreatePhotoCommand.cs`, `DeletePhotoCommand.cs`, `IngestPhotoCommand.cs`, `ProcessThumbnailCommand.cs`, `Validators\IngestPhotoCommandValidator.cs` | Photo write/background use cases |
| `Photos\Queries\GetPhotosByEventQuery.cs`, `GetPhotoByIdQuery.cs`, `DownloadPhotoQuery.cs` | Photo read/download use cases |
| `Settings\Commands\UpdateSettingCommand.cs`, `UpsertSettingCommand.cs` | Setting write use cases |
| `Settings\Queries\GetAllSettingsQuery.cs`, `GetSettingByKeyQuery.cs` | Setting read use cases |
| `Statistics\Queries\GetDashboardStatsQuery.cs`, `GetEventStatisticsQuery.cs` | Stats read use cases |
| `Mappings\MappingProfile.cs` | Main entity -> DTO mappings |
| `Mappings\EventMappingProfile.cs`, `PhotoMappingProfile.cs`, `SettingMappingProfile.cs` | Placeholder profiles retained for structure compatibility |
#### EventPhoto.Domain
| File(s) | Purpose |
|---|---|
| `EventPhoto.Domain.csproj` | Domain project definition |
| `Common\Entity.cs` | Base entity with `Id`, `CreatedAt`, `UpdatedAt`, `Touch()` |
| `Common\AggregateRoot.cs` | Base aggregate with domain event collection |
| `Common\IDomainEvent.cs` | MediatR-based event marker |
| `Common\Result.cs` | `Result` / `Result<T>` pattern |
| `Entities\User.cs`, `Event.cs`, `Photo.cs`, `DownloadLog.cs`, `SystemSetting.cs` | Core domain model |
| `Enums\EventType.cs`, `ThumbnailStatus.cs`, `UserRole.cs` | Enumerations |
| `Events\EventCreatedEvent.cs`, `EventDeactivatedEvent.cs`, `PhotoCreatedEvent.cs`, `PhotoDeletedEvent.cs` | Domain event records |
| `Exceptions\DomainException.cs`, `BusinessRuleException.cs`, `NotFoundException.cs` | Domain and app-level exception types |
| `Interfaces\IEventRepository.cs`, `IPhotoRepository.cs`, `IUserRepository.cs`, `IDownloadLogRepository.cs`, `ISystemSettingRepository.cs`, `IUnitOfWork.cs` | Persistence contracts |
| `ValueObjects\FilePath.cs`, `FileSize.cs` | Validated file path and file size value objects |
#### EventPhoto.Infrastructure
| File(s) | Purpose |
|---|---|
| `EventPhoto.Infrastructure.csproj` | Infrastructure project definition |
| `DependencyInjection.cs` | Compatibility wrapper |
| `Extensions\InfrastructureServiceExtensions.cs` | Registers DbContext, repositories, infra services, Npgsql switch |
| `Persistence\AppDbContext.cs` | EF Core DbContext, `DbSet`s, `IUnitOfWork` implementation |
| `Persistence\AppDbContextFactory.cs` | Design-time EF CLI factory |
| `Persistence\AppDbContextSeeder.cs` | First-run migrations + admin/settings seed |
| `Persistence\UnitOfWork.cs` | Concrete unit-of-work |
| `Persistence\Configurations\UserConfiguration.cs`, `EventConfiguration.cs`, `PhotoConfiguration.cs`, `DownloadLogConfiguration.cs`, `SystemSettingConfiguration.cs` | EF Fluent API mappings |
| `Persistence\Repositories\EventRepository.cs`, `PhotoRepository.cs`, `UserRepository.cs`, `DownloadLogRepository.cs`, `SystemSettingRepository.cs` | Repository implementations |
| `Persistence\Migrations\20260630174634_InitialCreate.cs`, `20260630174634_InitialCreate.Designer.cs`, `AppDbContextModelSnapshot.cs` | EF migration artifacts |
| `Services\Auth\JwtTokenService.cs`, `PasswordHasher.cs` | JWT and BCrypt services |
| `Services\FileSystem\FileStorageService.cs`, `FileService.cs` | File-system services |
| `Services\QrCode\QrCodeService.cs` | QR PNG generation |
| `Services\Thumbnails\ThumbnailService.cs` | Thumbnail generation |
| `Settings\JwtSettings.cs`, `ThumbnailSettings.cs` | Options classes |
#### EventPhoto.Contracts
| File(s) | Purpose |
|---|---|
| `EventPhoto.Contracts.csproj` | Contracts project definition |
| `Common\ApiResponse.cs`, `PagedResponse.cs` | Response envelope types |
| `Requests\Auth\LoginRequest.cs`, `ChangePasswordRequest.cs` | Auth requests |
| `Requests\Events\CreateEventRequest.cs`, `UpdateEventRequest.cs` | Event requests |
| `Requests\Photos\GetPhotosRequest.cs` | Photo query request |
| `Requests\Settings\UpdateSettingRequest.cs` | Setting update request |
| `Responses\Auth\AuthResponse.cs`, `LoginResponse.cs` | Auth responses |
| `Responses\Events\EventResponse.cs`, `EventSummaryResponse.cs` | Event responses |
| `Responses\Photos\PhotoResponse.cs`, `PhotoSummaryResponse.cs` | Photo responses |
| `Responses\Settings\SystemSettingResponse.cs` | Setting response |
| `Responses\Statistics\DashboardStatsResponse.cs`, `EventStatisticsResponse.cs` | Statistics responses |
#### EventPhoto.Worker
| File(s) | Purpose |
|---|---|
| `EventPhoto.Worker.csproj` | Worker project definition |
| `Program.cs` | Worker bootstrap, Serilog, DI, hosted services |
| `Extensions\WorkerServiceExtensions.cs` | Registers hosted services |
| `Services\FileWatcher\FileWatcherService.cs` | Watches event folders and creates photos |
| `Services\ThumbnailProcessor\ThumbnailProcessorService.cs` | Processes pending/failed thumbnails |
| `Services\NoOpPhotoNotificationService.cs` | Worker-only stub for `IPhotoNotificationService` |
| `appsettings.json`, `appsettings.Development.json` | Worker runtime config |
| `Properties\launchSettings.json` | Worker launch profiles |
### 4.2 React Frontend
| File(s) | Purpose |
|---|---|
| `src\EventPhoto.React\package.json`, `package-lock.json` | Frontend dependencies and scripts |
| `vite.config.ts` | Dev proxy and production output to API `wwwroot` |
| `tailwind.config.js`, `postcss.config.js` | CSS build config |
| `tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json` | TypeScript config |
| `.gitignore`, `.oxlintrc.json`, `README.md`, `index.html` | Frontend metadata, lint, HTML shell |
| `public\favicon.svg`, `public\icons.svg` | Static public assets |
| `src\main.tsx` | React bootstrap |
| `src\App.tsx` | BrowserRouter + QueryClientProvider + route tree |
| `src\index.css` | Global CSS/Tailwind entry |
| `src\api\auth.ts`, `events.ts`, `photos.ts`, `settings.ts`, `statistics.ts`, `client.ts` | Axios client and backend access modules |
| `src\hooks\useAuth.ts`, `useGalleryHub.ts` | Auth and SignalR hooks |
| `src\store\authStore.ts` | localStorage auth store |
| `src\types\index.ts` | TypeScript interfaces for API models |
| `src\utils\format.ts` | Date/time/size formatting helpers |
| `src\components\Layout\AdminLayout.tsx`, `GuestLayout.tsx`, `Navbar.tsx`, `Sidebar.tsx` | Shell components |
| `src\components\UI\Button.tsx`, `Input.tsx`, `Card.tsx`, `Badge.tsx`, `Modal.tsx`, `Spinner.tsx` | Reusable UI components |
| `src\pages\Login.tsx`, `Gallery.tsx`, `Dashboard.tsx`, `Statistics.tsx`, `Logs.tsx`, `HealthMonitoring.tsx`, `Settings.tsx` | Top-level pages |
| `src\pages\Events\EventList.tsx`, `EventForm.tsx`, `EventDetail.tsx` | Event administration pages |
### 4.3 Scripts & Docs
| File(s) | Purpose |
|---|---|
| `scripts\build-release.ps1` | Builds React and publishes API + Worker into `publish\` |
| `scripts\setup-postgresql.ps1` | Creates DB/user and grants permissions |
| `scripts\install-service.ps1` | Installs `PixBridgeApi` and `PixBridgeWorker` Windows Services |
| `scripts\uninstall-service.ps1` | Stops/removes services |
| `scripts\installer.iss` | Inno Setup installer script |
| `docs\README.md`, `architecture.md`, `database-schema.sql`, `deployment-guide.md`, `icon.ico` | Supplemental docs and installer icon |

## 5. Domain Model
### 5.1 Entities
#### User
| Property | Type |
|---|---|
| `Id` | `Guid` |
| `Username` | `string` |
| `Email` | `string` |
| `PasswordHash` | `string` |
| `Role` | `UserRole` |
| `IsActive` | `bool` |
| `LastLoginAt` | `DateTimeOffset?` |
| `CreatedAt` | `DateTimeOffset` |
| `UpdatedAt` | `DateTimeOffset` |
#### Event
| Property | Type |
|---|---|
| `Id` | `Guid` |
| `Name` | `string` |
| `Description` | `string?` |
| `EventType` | `EventType` |
| `EventDate` | `DateOnly` |
| `VenueName` | `string?` |
| `ClientName` | `string?` |
| `WatchFolder` | `string` |
| `ThumbnailFolder` | `string` |
| `QrCodePath` | `string?` |
| `QrCodeUrl` | `string?` |
| `IsActive` | `bool` |
| `IsDeleted` | `bool` |
| `PhotoCount` | `int` |
| `TotalSizeBytes` | `long` |
| `CreatedBy` | `Guid` |
Important behavior: `Create(...)`, `SetQrCode(...)`, `Activate()`, `Deactivate()`, `Delete()`, `IncrementPhotoCount(...)`, `DecrementPhotoCount(...)`, `Update(...)`.
#### Photo
| Property | Type |
|---|---|
| `Id` | `Guid` |
| `EventId` | `Guid` |
| `FileName` | `string` |
| `OriginalPath` | `string` |
| `ThumbnailPath` | `string` |
| `FileSizeBytes` | `long` |
| `MimeType` | `string` |
| `Width` | `int?` |
| `Height` | `int?` |
| `TakenAt` | `DateTimeOffset?` |
| `CapturedAt` | `DateTimeOffset` |
| `DownloadCount` | `int` |
| `IsDeleted` | `bool` |
| `ThumbnailStatus` | `ThumbnailStatus` |
| `CreatedAt` | `DateTimeOffset` |
| `UpdatedAt` | `DateTimeOffset` |
Important behavior: `Create(...)`, `MarkThumbnailProcessing()`, `MarkThumbnailDone(...)`, `MarkThumbnailFailed()`, `RecordDownload()`, `Delete()`.
#### DownloadLog
| Property | Type |
|---|---|
| `Id` | `Guid` |
| `PhotoId` | `Guid` |
| `EventId` | `Guid` |
| `IpAddress` | `string?` |
| `UserAgent` | `string?` |
| `DownloadedAt` | `DateTimeOffset` |
#### SystemSetting
| Property | Type |
|---|---|
| `Id` | `Guid` |
| `Key` | `string` |
| `Value` | `string` |
| `Description` | `string?` |
| `UpdatedAt` | `DateTimeOffset` |
### 5.2 Enums
| Enum | Members |
|---|---|
| `EventType` | `Wedding=1`, `Reception=2`, `Birthday=3`, `Corporate=4`, `Outdoor=5`, `Other=6` |
| `ThumbnailStatus` | `Pending=0`, `Processing=1`, `Done=2`, `Failed=3` |
| `UserRole` | `Admin=1`, `Viewer=2` |
### 5.3 Value Objects
| Value Object | Rules |
|---|---|
| `FilePath` | Non-empty, rejects `..`, normalizes with `Path.GetFullPath`, protects against traversal |
| `FileSize` | Non-negative only, exposes `ToHumanReadable()` |
### 5.4 Domain Events
| Event | Payload |
|---|---|
| `PhotoCreatedEvent` | `(PhotoId, EventId, FileName)` |
| `PhotoDeletedEvent` | `(PhotoId, EventId)` |
| `EventCreatedEvent` | `(EventId, EventName)` |
| `EventDeactivatedEvent` | `(EventId)` |
All implement `IDomainEvent`, which implements MediatR `INotification`.
### 5.5 Repository Interfaces
`IEventRepository`, `IPhotoRepository`, `IUserRepository`, `IDownloadLogRepository`, `ISystemSettingRepository`, `IUnitOfWork`
### 5.6 Common Patterns (Result<T>, AggregateRoot, Entity)
| Pattern | Meaning |
|---|---|
| `Result.Success()` / `Result.Failure(error)` | Non-generic operation outcome |
| `Result.Success(value)` / `Result.Failure<T>(error)` | Generic operation outcome |
| `AggregateRoot` | Base for `Event`, `Photo`; owns domain event list and `RaiseDomainEvent()` / `ClearDomainEvents()` |
| `Entity` | Base with `Id`, `CreatedAt`, `UpdatedAt`, `Touch()` |

## 6. Application Layer
### 6.1 Commands
| Area | Commands |
|---|---|
| Auth | `LoginCommand`, `ChangePasswordCommand` |
| Events | `CreateEventCommand`, `UpdateEventCommand`, `DeleteEventCommand`, `ToggleEventActiveCommand`, `ActivateEventCommand`, `DeactivateEventCommand`, `GenerateQrCodeCommand` |
| Photos | `CreatePhotoCommand`, `DeletePhotoCommand`, `IngestPhotoCommand`, `ProcessThumbnailCommand` |
| Settings | `UpdateSettingCommand`, `UpsertSettingCommand` |
### 6.2 Queries
| Area | Queries |
|---|---|
| Events | `GetEventsQuery`, `GetEventByIdQuery`, `GetAllActiveEventsQuery`, `GetEventWithPhotosQuery` |
| Photos | `GetPhotosByEventQuery`, `GetPhotoByIdQuery`, `DownloadPhotoQuery` |
| Settings | `GetAllSettingsQuery`, `GetSettingByKeyQuery` |
| Statistics | `GetDashboardStatsQuery`, `GetEventStatisticsQuery` |
### 6.3 Validators
Known validators:
- `LoginCommandValidator`
- `ChangePasswordCommandValidator`
- `CreateEventCommandValidator`
- `IngestPhotoCommandValidator`
Validation runs automatically in the MediatR pipeline before handlers execute.
### 6.4 Pipeline Behaviors
| Behavior | Purpose |
|---|---|
| `ValidationBehavior<TRequest,TResponse>` | Resolves all validators for the request and throws `ValidationException` on failure |
| `LoggingBehavior<TRequest,TResponse>` | Logs request type and elapsed milliseconds; warns when request takes more than `500ms` |
### 6.5 AutoMapper Profiles
Main mapping profile: `Mappings\MappingProfile.cs`
Mappings:
- `Event -> EventResponse`
- `Event -> EventSummaryResponse`
- `Photo -> PhotoResponse`
- `Photo -> PhotoSummaryResponse`
- `SystemSetting -> SystemSettingResponse`
Important mapping behavior:
- Enum values use `ToString()`
- `PhotoResponse.ThumbnailUrl = /api/photos/{id}/thumbnail`
- `PhotoResponse.OriginalUrl = /api/photos/{id}/download`
- Event total size is humanized
Placeholder profiles retained for structure compatibility:
- `EventMappingProfile`
- `PhotoMappingProfile`
- `SettingMappingProfile`
### 6.6 Service Interfaces
Primary interfaces in `Common\Interfaces`:
- `IPasswordHasher`
- `IJwtTokenService`
- `IFileStorageService`
- `IThumbnailService`
- `IQrCodeService`
- `IPhotoNotificationService`
Also present but not primary:
- `ITokenService`
- `IImageService`
- `IFileService`
- `IPasswordService`

## 7. Infrastructure Layer
### 7.1 Database (EF Core + PostgreSQL)
Backend persistence uses EF Core 8 with Npgsql. `AppDbContext` exposes:
- `DbSet<Event> Events`
- `DbSet<Photo> Photos`
- `DbSet<User> Users`
- `DbSet<DownloadLog> DownloadLogs`
- `DbSet<SystemSetting> SystemSettings`
`AppDbContext` also implements `IUnitOfWork`. `OnModelCreating` applies all entity configurations from the assembly. `SaveChangesAsync` gathers `AggregateRoot` instances with domain events, clears them, then persists changes atomically.
Important runtime switch in `InfrastructureServiceExtensions`:
```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```
### 7.2 Entity Configurations
Configuration classes:
- `UserConfiguration`
- `EventConfiguration`
- `PhotoConfiguration`
- `DownloadLogConfiguration`
- `SystemSettingConfiguration`
Documented rules to preserve:
- `EventConfiguration` has soft-delete query filter `!IsDeleted`
- `PhotoConfiguration` has soft-delete query filter `!IsDeleted`
Notable EF details:
- `EventConfiguration` maps `_photos` backing field
- `Event` -> `Photo` uses FK `EventId`
- `PhotoConfiguration` makes `OriginalPath` unique
- `PhotoConfiguration` indexes `{ ThumbnailStatus, IsDeleted }`
### 7.3 Repositories
Concrete implementations:
- `EventRepository`
- `PhotoRepository`
- `UserRepository`
- `DownloadLogRepository`
- `SystemSettingRepository`
- `UnitOfWork`
Repository responsibilities:
- Abstract EF Core from handlers/controllers
- Apply query logic and persistence semantics
- Return domain entities, not HTTP models
### 7.4 Services
| Service | Implementation details |
|---|---|
| JWT | `JwtTokenService`, HS256 JWT |
| Password hashing | `PasswordHasher`, BCrypt work factor `12` |
| File storage | `FileStorageService`, local disk |
| File helper | `FileService` |
| Thumbnailing | `ThumbnailService`, ImageSharp, resize Max mode, JPEG output |
| QR generation | `QrCodeService`, QRCoder, PNG, `ECCLevel.Q`, pixel size `10` |
| Settings classes | `JwtSettings`, `ThumbnailSettings` |
### 7.5 Seeder (default data)
`AppDbContextSeeder.SeedAsync(...)`:
1. Applies pending migrations
2. Seeds default admin if no users exist
3. Seeds 8 default `SystemSetting` records if none exist
Default credentials:
- Admin username: `admin`
- Admin password: `Admin@1234!`
Seeded settings:
| Key | Value |
|---|---|
| `app.name` | `PixBridge` |
| `app.serverUrl` | `http://192.168.10.10` |
| `gallery.pageSize` | `50` |
| `thumbnail.width` | `400` |
| `thumbnail.height` | `400` |
| `thumbnail.quality` | `85` |
| `download.rateLimit` | `30` |
| `watcher.extensions` | `.jpg,.jpeg,.png,.cr2,.nef,.arw,.dng,.tiff` |
### 7.6 Migrations
Current migration file:
- `src/EventPhoto.Infrastructure/Persistence/Migrations/20260630174634_InitialCreate.cs`
Add migration:
```powershell
dotnet ef migrations add <Name> --project src/EventPhoto.Infrastructure --startup-project src/EventPhoto.Api
```
Apply migration:
```powershell
dotnet ef database update --project src/EventPhoto.Infrastructure --startup-project src\EventPhoto.Api
```

## 8. API Layer
### 8.1 All Endpoints (complete reference table)
| Controller | Endpoint | Method | Auth | Purpose |
|---|---|---|---|---|
| `AuthController` | `/api/auth/login` | `POST` | anon | JWT login |
| `AuthController` | `/api/auth/change-password` | `POST` | Admin | Change current password |
| `AuthController` | `/api/auth/me` | `GET` | auth | Current claims snapshot |
| `EventsController` | `/api/events` | `GET` | anon | List events |
| `EventsController` | `/api/events/{id}` | `GET` | anon | Single event |
| `EventsController` | `/api/events` | `POST` | Admin | Create event |
| `EventsController` | `/api/events/{id}` | `PUT` | Admin | Update event |
| `EventsController` | `/api/events/{id}` | `DELETE` | Admin | Soft-delete event |
| `EventsController` | `/api/events/{id}/active?activate=` | `PATCH` | Admin | Activate/deactivate |
| `EventsController` | `/api/events/{id}/qrcode` | `GET` | anon | Serve QR PNG |
| `PhotosController` | `/api/photos/event/{eventId}?page&pageSize` | `GET` | anon | Paged gallery photos |
| `PhotosController` | `/api/photos/{id}` | `GET` | anon | Photo metadata |
| `PhotosController` | `/api/photos/{id}/thumbnail` | `GET` | anon | Thumbnail stream |
| `PhotosController` | `/api/photos/{id}/download` | `GET` | anon + RL | Original photo download |
| `PhotosController` | `/api/photos/{id}` | `DELETE` | Admin | Soft-delete photo |
| `StatisticsController` | `/api/statistics/dashboard` | `GET` | Admin | Dashboard stats |
| `StatisticsController` | `/api/statistics/events/{eventId}` | `GET` | Admin | Per-event stats |
| `SettingsController` | `/api/settings` | `GET` | Admin | All settings |
| `SettingsController` | `/api/settings/{key}` | `PUT` | Admin | Update setting |
| `HealthController` | `/api/health` | `GET` | anon | Health probe |
| `LogsController` | `/api/logs/recent?limit=200` | `GET` | Admin | Recent log entries |
### 8.2 Authentication (JWT)
Configured by `AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)` in `JwtExtensions.cs`.
Behavior:
- Reads `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`
- Uses `JwtBearerDefaults.AuthenticationScheme`
- Validates signing key, issuer, audience, lifetime
- `ClockSkew = TimeSpan.Zero`
- Supports SignalR token via `access_token` query string for `/hubs/*`
JWT configuration values:
- Issuer: `PixBridge`
- Audience: `PixBridgeClients`
- Expiry: `8` hours in `appsettings.json`
### 8.3 SignalR Hub
Hub:
- `PhotoHub` at `/hubs/photos`
Client group model:
- Auto-join via `?eventId=`
- Or call `JoinEvent(eventId)` / `LeaveEvent(eventId)`
- Group name format: `event-{eventId}`
Broadcast events:
- `photo:new`
- `photo:deleted`
API notification bridge:
- `PhotoNotificationService` implements `IPhotoNotificationService` using `IHubContext<PhotoHub>`
### 8.4 Middleware
Custom middleware:
- `ExceptionHandlingMiddleware`
Exception translation:
- `ValidationException` -> `400`
- `NotFoundException` -> `404`
- `BusinessRuleException` -> `422`
- `DomainException` -> `400`
- Unknown exception -> `500`
Built-in pipeline pieces:
- Serilog request logging
- CORS
- Response caching
- Authentication
- Authorization
- Static files
- SPA fallback
### 8.5 Rate Limiting
Configured in `Program.cs`:
- limiter name: `"downloads"`
- type: fixed window
- `PermitLimit = 30`
- `Window = 1 minute`
- `QueueProcessingOrder = OldestFirst`
- `QueueLimit = 5`
- rejection code: `429`
Applied on:
- `GET /api/photos/{id}/download`
### 8.6 Program.cs Pipeline
Service registration sequence:
1. `builder.Host.UseWindowsService(...)`
2. `builder.Host.UseSerilog()`
3. `builder.Services.AddApplicationServices()`
4. `builder.Services.AddInfrastructureServices(builder.Configuration)`
5. `builder.Services.AddSignalR()`
6. `builder.Services.AddScoped<IPhotoNotificationService, PhotoNotificationService>()`
7. `builder.Services.AddJwtAuthentication(builder.Configuration)`
8. `builder.Services.AddAuthorization()`
9. `builder.Services.AddControllers()`
10. `builder.Services.AddSwaggerWithJwt()`
11. `builder.Services.AddCors(...)`
12. `builder.Services.AddResponseCaching()`
13. `builder.Services.AddRateLimiter(...)`
Runtime sequence:
1. build app
2. run `AppDbContextSeeder.SeedAsync(...)`
3. `UseSerilogRequestLogging()`
4. `UseMiddleware<ExceptionHandlingMiddleware>()`
5. `UseSwagger()`, `UseSwaggerUI(...)`
6. `UseCors()`
7. `UseResponseCaching()`
8. `UseRateLimiter()`
9. `UseAuthentication()`
10. `UseAuthorization()`
11. `UseDefaultFiles()`
12. `UseStaticFiles()`
13. `MapControllers()`
14. `MapHub<PhotoHub>("/hubs/photos")`
15. `MapFallbackToFile("index.html")` when `wwwroot\index.html` exists

## 9. Worker Service
### 9.1 FileWatcherService
Location: `src\EventPhoto.Worker\Services\FileWatcher\FileWatcherService.cs`
Characteristics:
- Inherits `BackgroundService`
- Refreshes active events every `30s`
- Creates a `FileSystemWatcher` per active `WatchFolder`
- Handles `Created` and `Renamed`
- Uses `ConcurrentDictionary<string, byte>` to prevent duplicate processing
Accepted extensions:
- `.jpg`, `.jpeg`, `.png`, `.cr2`, `.nef`, `.arw`, `.dng`, `.tiff`
Processing flow:
1. load active events
2. add/remove watchers to match active folders
3. on file event, validate extension
4. wait briefly
5. retry open-for-read up to 5 times
6. calculate MIME type
7. dispatch `CreatePhotoCommand`
8. log success/failure
### 9.2 ThumbnailProcessorService
Location: `src\EventPhoto.Worker\Services\ThumbnailProcessor\ThumbnailProcessorService.cs`
Characteristics:
- Inherits `BackgroundService`
- Interval: every `5s`
- Batch size: `10`
- Processes `Pending` and `Failed` rows
Flow:
1. load settings: `thumbnail.width`, `thumbnail.height`, `thumbnail.quality`, `app.serverUrl`
2. query repository for thumbnail work
3. mark photo `Processing`
4. generate thumbnail
5. mark photo `Done` and set width/height
6. notify through `IPhotoNotificationService`
7. on exception mark `Failed`
Worker notification rule:
- Worker registers `NoOpPhotoNotificationService`
- Worker has no SignalR hub
- API process owns actual real-time broadcasting

## 10. React Frontend
### 10.1 Pages & Routing
Routes from `src\EventPhoto.React\src\App.tsx`:
| Route | Component | Purpose |
|---|---|---|
| `/login` | `Login.tsx` | JWT login form |
| `/gallery/:eventId` | `Gallery.tsx` | Guest gallery with live updates |
| `/admin` | `Dashboard.tsx` | Dashboard landing |
| `/admin/events` | `EventList.tsx` | CRUD list |
| `/admin/events/new` | `EventForm.tsx` | Create event |
| `/admin/events/:eventId` | `EventDetail.tsx` | Event detail/stats/photo preview |
| `/admin/statistics` | `Statistics.tsx` | Per-event stats view |
| `/admin/logs` | `Logs.tsx` | Live log viewer |
| `/admin/health` | `HealthMonitoring.tsx` | Service/network status |
| `/admin/settings` | `Settings.tsx` | Edit system settings |
Route wrappers:
- `GuestLayout`
- `AdminLayout`
### 10.2 API Layer (Axios)
Files:
- `api\client.ts`
- `api\auth.ts`
- `api\events.ts`
- `api\photos.ts`
- `api\statistics.ts`
- `api\settings.ts`
`client.ts` behavior:
- `API_BASE = import.meta.env.VITE_API_BASE ?? '/api'`
- Adds `Authorization: Bearer <token>` when token exists
- On `401`, clears auth and redirects to `/login`
### 10.3 State Management
State model:
- server state via React Query
- auth persisted in localStorage
- local component state where sufficient
`authStore.ts` keys:
- `pixbridge_token`
- `pixbridge_user`
Stored user data:
- `username`
- `role`
### 10.4 Real-Time (SignalR hook)
Hook: `useGalleryHub(eventId, onNewPhoto)`
Behavior:
- hub URL: `VITE_HUB_BASE ?? '/hubs/photos'`
- passes `?eventId=`
- uses access token factory when token exists
- automatic reconnect
- handles `photo:new`
- returns `{ isConnected }`
### 10.5 Components
Layout components:
- `AdminLayout`
- `GuestLayout`
- `Navbar`
- `Sidebar`
Reusable UI:
- `Button`
- `Input`
- `Card`
- `Badge`
- `Modal`
- `Spinner`
Helpers:
- `utils/format.ts` with `formatDate`, `formatDateTime`, `formatFileSize`
### 10.6 Build & Vite Config
`vite.config.ts`:
- proxies `/api` -> `http://localhost:5000`
- proxies `/hubs` -> `http://localhost:5000` with `ws: true`
- build output: `../EventPhoto.Api/wwwroot`
- `emptyOutDir: true`
Meaning:
- dev server talks to local API on port 5000
- production frontend is physically hosted by API project

## 11. Database Reference
### 11.1 Tables
| Table | Purpose |
|---|---|
| `users` | admin/viewer identities |
| `events` | event metadata, watch folders, QR data |
| `photos` | photo metadata, paths, thumbnail state |
| `download_logs` | download audit trail |
| `system_settings` | editable runtime settings |
Important links:
- `events.created_by -> users.id`
- `photos.event_id -> events.id`
- `download_logs.photo_id -> photos.id`
- `download_logs.event_id -> events.id`
Soft-delete columns:
- `events.is_deleted`
- `photos.is_deleted`
### 11.2 System Settings
| Key | Default Value | Description |
|---|---|---|
| `app.name` | `PixBridge` | Application display name |
| `app.serverUrl` | `http://192.168.10.10` | LAN URL guests use |
| `gallery.pageSize` | `50` | Photos per page |
| `thumbnail.width` | `400` | Max thumbnail width px |
| `thumbnail.height` | `400` | Max thumbnail height px |
| `thumbnail.quality` | `85` | JPEG quality 1-100 |
| `download.rateLimit` | `30` | Max downloads/IP/minute |
| `watcher.extensions` | `.jpg,.jpeg,.png,.cr2,.nef,.arw,.dng,.tiff` | Watched extensions |
### 11.3 Migration Commands
```powershell
dotnet ef migrations add <Name> --project src/EventPhoto.Infrastructure --startup-project src/EventPhoto.Api
dotnet ef database update --project src/EventPhoto.Infrastructure --startup-project src\EventPhoto.Api
```

## 12. Configuration Reference
### 12.1 appsettings.json (complete)
```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=pixbridge;Username=pixbridge;Password=pixbridge123;" },
  "Jwt": { "Secret": "PixBridge-Super-Secret-Key-2024-MinLength-32-Chars!", "Issuer": "PixBridge", "Audience": "PixBridgeClients", "ExpiryHours": 8 },
  "Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning", "System": "Warning" } }, "WriteTo": [{"Name":"Console"},{"Name":"File","Args":{"path":"logs/pixbridge-.log","rollingInterval":"Day","retainedFileCountLimit":14}}] },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:80" } } }
}
```
### 12.2 appsettings.Development.json
Actual API development file:
```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=pixbridge_dev;Username=postgres;Password=postgres;" },
  "Jwt": { "Secret": "PixBridge-Dev-Secret-Key-2024-MinLength-32-Chars!", "Issuer": "PixBridge", "Audience": "PixBridgeClients", "ExpiryHours": 24 },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5000" } } }
}
```
### 12.3 Worker appsettings.json
Worker production:
```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=pixbridge;Username=pixbridge;Password=pixbridge123;" },
  "Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning", "System": "Warning" } }, "WriteTo": [{"Name":"Console"},{"Name":"File","Args":{"path":"logs/worker-.log","rollingInterval":"Day","retainedFileCountLimit":14}}] }
}
```
Worker development:
```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=pixbridge_dev;Username=postgres;Password=postgres;" },
  "Serilog": { "MinimumLevel": { "Default": "Debug", "Override": { "Microsoft": "Warning", "System": "Warning" } }, "WriteTo": [{"Name":"Console"},{"Name":"File","Args":{"path":"logs/worker-dev-.log","rollingInterval":"Day","retainedFileCountLimit":14}}] }
}
```
### 12.4 Environment Variables
Frontend env vars used by code:
| Variable | Purpose | Default |
|---|---|---|
| `VITE_API_BASE` | Axios base path | `/api` |
| `VITE_HUB_BASE` | SignalR hub base path | `/hubs/photos` |
Operational note:
- backend behavior is primarily config-file driven; production secrets should be moved out of committed files
### 12.5 NuGet.Config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

## 13. Local Development Setup (step-by-step)
### 13.1 Prerequisites
- Windows
- .NET 8 SDK
- Node.js/npm
- PostgreSQL
- PowerShell
### 13.2 PostgreSQL Setup
From repo root:
```powershell
Set-Location C:\CLS\PixBridge
powershell -ExecutionPolicy Bypass -File .\scripts\setup-postgresql.ps1
```
Default DB credentials:
- username=`pixbridge`
- password=`pixbridge123`
- database=`pixbridge`
Development config also references:
- username=`postgres`
- password=`postgres`
- database=`pixbridge_dev`
### 13.3 Running the API
```powershell
Set-Location C:\CLS\PixBridge
dotnet run --project .\src\EventPhoto.Api\EventPhoto.Api.csproj
```
### 13.4 Running the Worker
```powershell
Set-Location C:\CLS\PixBridge
dotnet run --project .\src\EventPhoto.Worker\EventPhoto.Worker.csproj
```
### 13.5 Running the React Dev Server
```powershell
Set-Location C:\CLS\PixBridge\src\EventPhoto.React
npm install
npm run dev
```
### 13.6 First Login & Verify
1. Start PostgreSQL
2. Start API
3. Start Worker
4. Open admin UI
5. Login with `admin` / `Admin@1234!`
6. Create an event
7. Drop a valid image into its `WatchFolder`
8. Verify a `Photo` row is created
9. Verify thumbnail is generated
10. Verify gallery updates and photo download works

## 14. Deployment Guide
### 14.1 Build for Production
Release script:
```powershell
Set-Location C:\CLS\PixBridge
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 1.0.0
```
What it does:
1. builds React
2. publishes `EventPhoto.Api`
3. publishes `EventPhoto.Worker`
4. copies scripts/docs into `publish\`
Publish style:
- self-contained
- `win-x64`
- single-file
### 14.2 Windows Service Installation
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-service.ps1 -InstallDir "C:\PixBridge"
```
Installed services:
- `PixBridgeApi`
- `PixBridgeWorker`
### 14.3 PostgreSQL Production Setup
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-postgresql.ps1
```
Then run API once or start service so migrations and seed execute.
### 14.4 Network Configuration (Static IP + Firewall)
- configure static IP `192.168.10.10`
- keep API reachable on port `80`
- allow inbound TCP 80 in firewall
- ensure guests are on same LAN/Wi-Fi
### 14.5 Inno Setup Installer
Installer source:
- `scripts\installer.iss`
Expected packaged exe:
- `PixBridge-Setup-1.0.0.exe`
Installer behavior:
- copies API/Worker
- copies setup scripts and docs
- can install services
- runs PostgreSQL setup
- opens admin panel
### 14.6 Post-Deploy Checklist
- [ ] PostgreSQL installed and reachable
- [ ] DB/user created
- [ ] JWT secret changed for production
- [ ] admin password changed
- [ ] firewall allows port 80
- [ ] API service running
- [ ] Worker service running
- [ ] QR/gallery URL reachable from guest device
- [ ] event ingestion works
- [ ] thumbnail generation works

## 15. Future Enhancements Guide (how to add features)
### 15.1 Adding a New API Feature (step-by-step pattern)
1. Update Domain entity/value object/event if business rules change
2. Add Application command/query + validator
3. Add/update Contracts request/response DTOs
4. Extend Infrastructure repositories/services/configurations
5. Add migration if schema changed
6. Add controller endpoint
7. Add frontend API module usage + route/page if needed
8. Validate end-to-end
### 15.2 Adding a New React Page
1. Create component under `src\EventPhoto.React\src\pages`
2. Add route in `src\App.tsx`
3. Add API function in `src\api`
4. Add/update TS interfaces in `src\types\index.ts`
5. If admin-only, place under `/admin` and use `AdminLayout`
6. If guest-facing, use `GuestLayout` or standalone route
### 15.3 Adding a New Domain Entity
1. Add entity in `src\EventPhoto.Domain\Entities`
2. Add repository interface in `Domain\Interfaces`
3. Add `DbSet<T>` to `AppDbContext`
4. Add EF configuration in `Infrastructure\Persistence\Configurations`
5. Implement repository in `Infrastructure\Persistence\Repositories`
6. Register repository in `AddInfrastructureServices(...)`
7. Add migration
8. Add Application/API/React pieces
### 15.4 Adding a New System Setting
1. Add default seed entry in `AppDbContextSeeder`
2. Read it through `ISystemSettingRepository`
3. Expose it through existing Settings UI and API if needed
4. Keep runtime constants and DB-stored setting semantics aligned
### 15.5 Cloud Sync Extension Points
Natural extension points:
- `IFileStorageService` for alternate storage
- `IPhotoNotificationService` for extra outbound channels
- repository layer for sync metadata
- worker for background sync jobs
- `SystemSettings` for tenant/sync configuration
Rule: cloud sync should remain optional; local capture and LAN gallery must still work offline.

## 16. Security Checklist
- [ ] Change default JWT secret in production
- [ ] Change default admin password
- [ ] Keep admin endpoints restricted to authenticated admins
- [ ] Preserve file path validation / traversal protection
- [ ] Keep guest endpoints anonymous only where intentional
- [ ] Keep download rate limiting active
- [ ] Avoid leaking internal filesystem details unnecessarily
- [ ] Ensure service account has only required filesystem permissions
- [ ] Rotate DB credentials for production
- [ ] Avoid committing real production secrets

## 17. Troubleshooting Guide
| Symptom | Likely Cause | Fix/Check |
|---|---|---|
| API startup fails | DB unavailable / bad connection string | check PostgreSQL and `DefaultConnection` |
| `dotnet ef` fails | wrong startup project or design-time connection mismatch | use documented EF commands; verify `AppDbContextFactory` |
| Login fails | seed not run or password changed | inspect API logs and `users` table |
| Event saves but no photos appear | worker not running or watch folder wrong | check `PixBridgeWorker` and event `WatchFolder` |
| Photos created but thumbnails remain pending | thumbnail processor not running | inspect worker logs |
| Photos marked failed | ImageSharp/file access issue | inspect worker logs and file permissions |
| Gallery loads but no live updates | hub connection issue | check `/hubs/photos`, browser console, SignalR settings |
| Download endpoint returns 429 | rate limit reached | wait 1 minute or adjust design |
| QR code 404 | QR file missing or path invalid | regenerate QR code for event |
| SPA route 404 after publish | missing `wwwroot\index.html` | rebuild React and redeploy API assets |
| Logs page empty | no log files or wrong content root | check `logs\` under service content root |
| Service installs but does not start | publish path wrong or permissions issue | verify `EventPhoto.Api.exe` / `EventPhoto.Worker.exe` paths |

## 18. Package Versions Reference
### .NET / NuGet
#### Api
| Package | Version |
|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.11` |
| `Serilog.AspNetCore` | `10.0.0` |
| `Serilog.Sinks.Console` | `6.1.1` |
| `Serilog.Sinks.File` | `7.0.0` |
| `Swashbuckle.AspNetCore` | `6.6.2` |
| `Microsoft.Extensions.Hosting.WindowsServices` | `8.0.1` |
| `Microsoft.EntityFrameworkCore.Design` | `8.0.11` |
| `Microsoft.AspNetCore.OpenApi` | `8.0.22` |
#### Application
| Package | Version |
|---|---|
| `MediatR` | `14.1.0` |
| `FluentValidation` | `12.1.1` |
| `FluentValidation.DependencyInjectionExtensions` | `12.1.1` |
| `AutoMapper` | `16.1.1` |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.9` |
| `Microsoft.Extensions.Options` | `10.0.9` |
#### Infrastructure
| Package | Version |
|---|---|
| `Microsoft.EntityFrameworkCore` | `8.0.11` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `8.0.11` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.11` |
| `System.IdentityModel.Tokens.Jwt` | `8.19.1` |
| `BCrypt.Net-Next` | `4.2.0` |
| `SixLabors.ImageSharp` | `3.1.7` |
| `QRCoder` | `1.8.0` |
| `Microsoft.EntityFrameworkCore.Design` | `8.0.11` |
#### Worker
| Package | Version |
|---|---|
| `Microsoft.Extensions.Hosting` | `8.0.1` |
| `Microsoft.Extensions.Hosting.WindowsServices` | `8.0.1` |
| `Serilog.AspNetCore` | `10.0.0` |
| `Serilog.Sinks.Console` | `6.1.1` |
| `Serilog.Sinks.File` | `7.0.0` |
| `Microsoft.Extensions.Http` | `10.0.9` |
### React Packages
| Package | Version |
|---|---|
| `react` | `18.3.1` |
| `react-dom` | `18.3.1` |
| `react-router-dom` | `6.30.4` |
| `@tanstack/react-query` | `5.101.2` |
| `axios` | `1.18.1` |
| `@microsoft/signalr` | `10.0.0` |
| `react-hook-form` | `7.80.0` |
| `zod` | `4.4.3` |
| `@hookform/resolvers` | `5.4.0` |
| `react-hot-toast` | `2.6.0` |
| `lucide-react` | `1.22.0` |
### React Dev Packages
| Package | Version |
|---|---|
| `vite` | `8.1.1` |
| `tailwindcss` | `3.4.19` |
| `postcss` | `8.5.16` |
| `autoprefixer` | `10.5.2` |
| `typescript` | `6.0.2` |

## 19. Agentic Context (for AI agents)
This section is the high-signal operating guide for AI agents changing PixBridge.
### Exact DI registration pattern (what registers what, in which extension method)
**Application:** `src\EventPhoto.Application\Extensions\ApplicationServiceExtensions.cs`
```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
```
Registers:
- MediatR from Application assembly
- FluentValidation validators from Application assembly
- `IPipelineBehavior<,> -> LoggingBehavior<,>`
- `IPipelineBehavior<,> -> ValidationBehavior<,>`
- AutoMapper profiles from Application assembly
**Infrastructure:** `src\EventPhoto.Infrastructure\Extensions\InfrastructureServiceExtensions.cs`
```csharp
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
```
Registers:
- `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`
- `AddDbContext<AppDbContext>(UseNpgsql(...))`
- `IUnitOfWork -> UnitOfWork`
- `IEventRepository -> EventRepository`
- `IPhotoRepository -> PhotoRepository`
- `IUserRepository -> UserRepository`
- `IDownloadLogRepository -> DownloadLogRepository`
- `ISystemSettingRepository -> SystemSettingRepository`
- `IJwtTokenService -> JwtTokenService`
- `IPasswordHasher -> PasswordHasher`
- `IThumbnailService -> ThumbnailService`
- `IQrCodeService -> QrCodeService`
- `IFileStorageService -> FileStorageService`
**API:** `src\EventPhoto.Api\Program.cs`
Registers:
- `AddApplicationServices()`
- `AddInfrastructureServices(builder.Configuration)`
- `AddSignalR()`
- `AddScoped<IPhotoNotificationService, PhotoNotificationService>()`
- `AddJwtAuthentication(builder.Configuration)`
- `AddAuthorization()`
- `AddControllers()`
- `AddSwaggerWithJwt()`
- `AddCors(...)`
- `AddResponseCaching()`
- `AddRateLimiter(...)`
**Worker:** `src\EventPhoto.Worker\Program.cs` + `Extensions\WorkerServiceExtensions.cs`
Registers:
- `AddApplicationServices()`
- `AddInfrastructureServices(builder.Configuration)`
- `AddWorkerServices()`
- `AddHttpClient()`
- `AddSingleton<IPhotoNotificationService, NoOpPhotoNotificationService>()`
`AddWorkerServices()` registers:
- `FileWatcherService`
- `ThumbnailProcessorService`
### Naming conventions
- Namespace root: `EventPhoto.*`
- Commands: `<Verb><Entity>Command`
- Queries: `Get...Query`
- Validators: `<RequestName>Validator`
- Repositories: `I<Entity>Repository` + `<Entity>Repository`
- EF configurations: `<Entity>Configuration`
- Frontend component files: `PascalCase.tsx`
- Frontend route/page files: `PascalCase.tsx`
- Contracts live in `EventPhoto.Contracts`, not API
- Feature folders in Application: `Auth`, `Events`, `Photos`, `Settings`, `Statistics`
### The Result<T> pattern — how handlers return results and how controllers unwrap them
Handlers should return `Result` or `Result<T>` for expected business outcomes:
```csharp
return Result.Success(value);
return Result.Failure<T>("error");
```
Controllers unwrap like this:
```csharp
var result = await _mediator.Send(command, cancellationToken);
if (result.IsFailure)
{
    return BadRequest(ApiResponse<T>.Fail(result.Error)); // or NotFound/Unauthorized
}
return Ok(ApiResponse<T>.Ok(result.Value));
```
Rules:
- Do not return raw entities from controllers
- Do not hide failures with nulls when `Result<T>` is expected
- Use exceptions for validation/domain/unexpected failures, not routine control flow
### How to add a new command end-to-end (Domain → Application → Infrastructure → API → React)
1. **Domain**
   - add/extend entity behavior in `src\EventPhoto.Domain\Entities`
   - add value object or domain event if required
2. **Application**
   - add `Commands\<NewCommand>.cs`
   - add validator if input constraints exist
   - inject only abstractions
   - return `Result`/`Result<T>`
3. **Infrastructure**
   - extend repository/service implementations
   - update EF configuration if persistence shape changes
   - add migration if schema changed
   - register new interface implementation in `AddInfrastructureServices(...)`
4. **API**
   - add controller action
   - map request DTO to command
   - convert `Result` to `ApiResponse`
5. **React**
   - add API call module entry
   - add/update TS types
   - add UI interaction/page/route
   - invalidate/refetch relevant React Query data
### Key architectural rules (what depends on what, forbidden dependencies)
Allowed:
- API -> Application, Infrastructure, Contracts
- Worker -> Application, Infrastructure
- Infrastructure -> Application + Domain
- Application -> Domain + Contracts
Forbidden:
- Domain -> Infrastructure/API/React
- Application -> API
- Controllers -> direct EF Core access
- React -> backend internals outside HTTP/SignalR contracts
Behavioral rules:
- Business invariants belong in Domain
- Use cases belong in Application handlers
- Persistence details belong in Infrastructure
- HTTP/auth/middleware belong in API
- Long-running background filesystem work belongs in Worker
### Where to find things (which file/folder for each type of change)
| Change | Location |
|---|---|
| Add endpoint | `src\EventPhoto.Api\Controllers` |
| Change auth or Swagger | `src\EventPhoto.Api\Extensions\JwtExtensions.cs`, `Program.cs` |
| Change exception handling | `src\EventPhoto.Api\Middleware\ExceptionHandlingMiddleware.cs` |
| Change SignalR group behavior | `src\EventPhoto.Api\Hubs\PhotoHub.cs` |
| Change photo broadcasts | `src\EventPhoto.Api\Services\PhotoNotificationService.cs` |
| Add command/query | `src\EventPhoto.Application\<Feature>\Commands` or `Queries` |
| Add validation | `src\EventPhoto.Application\...\*Validator.cs` |
| Add DTO | `src\EventPhoto.Contracts\Requests` / `Responses` |
| Change entities | `src\EventPhoto.Domain\Entities` |
| Change value objects | `src\EventPhoto.Domain\ValueObjects` |
| Add repository method | Domain interface + Infrastructure repository implementation |
| Change EF mapping | `src\EventPhoto.Infrastructure\Persistence\Configurations` |
| Change DbContext | `src\EventPhoto.Infrastructure\Persistence\AppDbContext.cs` |
| Change seeding | `src\EventPhoto.Infrastructure\Persistence\AppDbContextSeeder.cs` |
| Change migration | `src\EventPhoto.Infrastructure\Persistence\Migrations` |
| Change folder watching | `src\EventPhoto.Worker\Services\FileWatcher\FileWatcherService.cs` |
| Change thumbnail processing | `src\EventPhoto.Worker\Services\ThumbnailProcessor\ThumbnailProcessorService.cs` |
| Change frontend routing | `src\EventPhoto.React\src\App.tsx` |
| Change frontend API calls | `src\EventPhoto.React\src\api` |
| Change auth persistence | `src\EventPhoto.React\src\store\authStore.ts` |
| Change real-time gallery hook | `src\EventPhoto.React\src\hooks\useGalleryHub.ts` |
| Change deployment scripts | `scripts\*.ps1`, `scripts\installer.iss` |
### Common gotchas
1. **EF Core query filters**  
   Event and Photo are documented with `!IsDeleted` filters. If you need deleted rows, be explicit and careful.
2. **Npgsql timestamp behavior switch**  
   `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` is part of current runtime assumptions. Do not remove casually.
3. **NoOp pattern in Worker**  
   Worker resolves `IPhotoNotificationService` to `NoOpPhotoNotificationService`. Real SignalR broadcasting happens in API only.
4. **React build target**  
   Frontend build overwrites API `wwwroot`. That is expected.
5. **Seeder side effects**  
   API startup runs migrations and seeds defaults; startup failures often mean DB/config issues.
6. **Rate limit design**  
   `download.rateLimit` exists in DB settings, but current limiter is configured statically in `Program.cs`. Keep design and implementation aligned if made dynamic.
7. **SignalR token transport**  
   JWT bearer setup extracts hub token from query string `access_token` for `/hubs/*`. Preserve this if changing auth.
8. **Path normalization**  
   Domain normalizes filesystem paths with `Path.GetFullPath`. Preserve absolute-path semantics.
### Minimal safe modification strategy
- Start from the user-visible requirement
- Identify the layer boundary first
- Make the smallest correct end-to-end change
- Preserve `Result<T>` + `ApiResponse<T>` conventions
- Preserve DI symmetry between interfaces and implementations
- If schema changes, add migration and config notes
- If UI changes, update API call, TS type, and route together
- If worker logic changes, verify idempotency and permissions assumptions
### AI quick summary
- PixBridge is an offline LAN event gallery platform
- API and Worker are separate .NET 8 processes
- React builds into API `wwwroot`
- Domain owns business rules
- Application owns CQRS + validation + `Result<T>`
- Infrastructure owns EF/Npgsql/JWT/BCrypt/QR/thumbnail/file implementations
- API owns controllers, middleware, JWT auth, SignalR, Swagger
- Worker owns watched-folder ingestion and thumbnail processing
- Soft delete, path safety, LAN deployment, and no-op worker notifications are core conventions
