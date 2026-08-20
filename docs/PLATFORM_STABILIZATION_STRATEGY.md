# PixBridge — Platform Foundation Stabilization Strategy

> **Classification:** Internal Architecture & Implementation Strategy  
> **Prepared by:** Principal Architecture Review  
> **Date:** 2026-08-20  
> **Status:** Architecture Approved — Implementation Pending  
> **Version:** 1.0

---

## Table of Contents

1. [Pre-Analysis: Complete System Understanding](#pre-analysis-complete-system-understanding)
2. [Fix #1 — Domain Event Dispatcher](#fix-1--domain-event-dispatcher)
3. [Fix #2 — Legacy Face Pipeline Removal](#fix-2--legacy-face-pipeline-removal)
4. [Fix #3 — Subscription Enforcement](#fix-3--subscription-enforcement)
5. [Implementation Order](#implementation-order)
6. [Cross-Cutting Concerns](#cross-cutting-concerns)

---

## Pre-Analysis: Complete System Understanding

### MediatR Pipeline (as-is)

```
Request arrives at Controller
        ↓
IMediator.Send(command)
        ↓
MediatR Pipeline Behaviors (ordered):
  1. LoggingBehavior<TRequest, TResponse>   → logs timing, slow warning at 500ms
  2. ValidationBehavior<TRequest, TResponse> → FluentValidation, throws on failure
        ↓
Command/Query Handler executes
        ↓
Handler calls UnitOfWork.SaveChangesAsync()
        ↓
AppDbContext.SaveChangesAsync():
  1. Collect AggregateRoot entries with DomainEvents
  2. ClearDomainEvents()           ← BUG: events cleared BEFORE save
  3. base.SaveChangesAsync()       ← save succeeds
  4. (events are gone — never dispatched)
```

### Current Worker Pipeline (as-is)

```
Registered Hosted Services (WorkerServiceExtensions.AddWorkerServices):
  ✅ FileWatcherService          → active
  ✅ ThumbnailProcessorService   → active
  ✅ AiDiscoveryPipelineService  → active
  ✅ DeadLetterProcessorService  → active
  ✅ SelfieRetentionService      → active
  ❌ FaceIndexingService         → NOT REGISTERED (dead code)
```

### Photo Ingestion Flow (as-is)

```
FileWatcherService
  → CreatePhotoCommand (active path)
    → Photo.Create() → FaceIndexStatus = NotRequired
    → event.IncrementPhotoCount()
    → SaveChanges

ThumbnailProcessorService (polls Pending thumbnails)
  → generates thumbnail via ImageSharp
  → photo.MarkThumbnailDone()
  → NotifyPhotoAddedAsync (SignalR)
  → EnqueuePhotoForAiDiscoveryCommand
    → checks event.EnableFaceRecognition
    → FaceProcessingJob.Create() → Status = Pending
    → SaveChanges

AiDiscoveryPipelineService (polls FaceProcessingJob.Status = Pending)
  → ProcessAiDiscoveryJobCommand
    → Stage 1: Detecting (Python InsightFace)
    → Stage 2: QualityChecking (FaceQualityService)
    → Stage 3: Embedding (ArcFace 512-dim)
    → Stage 4: Indexing (pgvector INSERT)
    → FinalizeJobAsync:
        photo.MarkFaceIndexCompleted(faceCount)
        SaveChanges
```

### Unused Legacy Code Inventory

```
NEVER DISPATCHED by any active service:
  IngestPhotoCommand         → IngestPhotoCommandHandler
  IngestPhotoCommandValidator
  QueueFaceIndexCommand      → QueueFaceIndexCommandHandler
  ProcessFaceIndexCommand    → ProcessFaceIndexCommandHandler

NOT REGISTERED as Hosted Service:
  FaceIndexingService

REPOSITORY METHOD — only called by FaceIndexingService:
  IPhotoRepository.GetPendingFaceIndexAsync()
  PhotoRepository.GetPendingFaceIndexAsync()
```

---

## Fix #1 — Domain Event Dispatcher

### 1.1 Current State

```csharp
// AppDbContext.SaveChangesAsync() — CURRENT (BROKEN)
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var aggregates = ChangeTracker.Entries<AggregateRoot>()
        .Where(entry => entry.Entity.DomainEvents.Count != 0)
        .Select(entry => entry.Entity)
        .ToList();

    // BUG: events cleared BEFORE save. If save fails, events are permanently lost.
    // BUG: events never dispatched — they are just cleared.
    aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
    return await base.SaveChangesAsync(cancellationToken);
}
```

**The Bug:**
- `ClearDomainEvents()` is called BEFORE `base.SaveChangesAsync()`.
- Events are permanently discarded.
- Even if save fails, events are lost (cannot retry).
- Zero domain event handlers ever execute.

**Impact of the bug:**

| Domain Event | Should Trigger | Currently Triggers |
|---|---|---|
| `PhotoCreatedEvent` | Audit log, analytics, notifications | Nothing |
| `EventCreatedEvent` | Audit log, user activity | Nothing |
| `EventDeactivatedEvent` | Cleanup logic, notifications | Nothing |
| `PhotoDeletedEvent` | Storage cleanup, embedding cleanup | Nothing |
| `FaceIndexCompletedEvent` | Analytics update, search readiness | Nothing |
| `FaceSearchCompletedEvent` | Analytics, guest notification | Nothing |

---

### 1.2 Architectural Risk

| Risk | Severity | Description |
|------|----------|-------------|
| Silent data loss | 🔴 Critical | All side effects tied to domain events are silently skipped |
| Audit gaps | 🔴 Critical | AuditLog entries dependent on domain events never written |
| Analytics stale | 🟡 High | Photo/search statistics missing domain-event-driven updates |
| Future feature blocker | 🔴 Critical | Any new feature built on domain events will silently fail |
| Debugging difficulty | 🟡 High | No errors, no warnings — events disappear silently |

---

### 1.3 Architectural Analysis: Why Events Must Dispatch AFTER Save

```
WRONG (current):                   WRONG (pre-save dispatch):
collect → clear → save             collect → dispatch → save
                                                      ↑
                                         save could fail →
                                         events were already processed →
                                         inconsistent state (ghost events)

CORRECT:
collect → save → dispatch → clear

Rule: "Events are a notification that something HAPPENED.
       If the save didn't succeed, it didn't happen."
```

---

### 1.4 Pattern Selection

Three viable patterns exist. Analysis:

#### Option A: Inject IPublisher into AppDbContext

```
AppDbContext takes IPublisher in constructor.
SaveChangesAsync: collect events → save → publish → clear.
```

**Pros:** Simple, minimal code, direct.  
**Cons:** Infrastructure (AppDbContext) takes dependency on MediatR. Acceptable since `IDomainEvent : INotification` already exists and `IPublisher` is the correct abstraction. MediatR is already an application concern, not a domain concern.

**Verdict: ✅ Recommended** — AppDbContext is already in Infrastructure layer which references Application layer interfaces. `IPublisher` is from MediatR.Contracts — appropriate.

#### Option B: Outbox Pattern (PostgreSQL-backed)

```
SaveChangesAsync: collect events → serialize to OutboxMessage table → save atomically → 
background OutboxProcessor reads OutboxMessages → dispatches via MediatR → deletes row.
```

**Pros:** Guaranteed delivery, survives process crashes, full audit trail.  
**Cons:** Requires new `OutboxMessage` entity, new migration, new background service, JSON serialization of events, more infrastructure. 30-40× more complexity.

**Verdict: ✅ Future Phase** — Implement now with Option A, migrate to Outbox if needed for high-volume or cross-service scenarios.

#### Option C: Interceptor Pattern (EF Core SaveChangesInterceptor)

```
Override ISaveChangesInterceptor.SavedChangesAsync → collect and dispatch after successful save.
```

**Pros:** No constructor injection, clean separation.  
**Cons:** Interceptors run after `base.SaveChangesAsync()` — correct timing. However, `IPublisher` still needs to be injected somehow into the interceptor, requiring DI registration as a scoped service. Slightly more complex DI setup.

**Verdict: ✅ Equally valid alternative** — cleaner EF-separation but same end result. Option A is simpler.

---

### 1.5 Recommended Design: Option A (IPublisher Injection)

#### Flow

```
SaveChangesAsync(cancellationToken)
        │
        ├─ 1. ChangeTracker → collect AggregateRoots with DomainEvents
        │     (snapshot events BEFORE save — idempotent if save fails)
        │
        ├─ 2. base.SaveChangesAsync(cancellationToken)
        │     ← only if this succeeds do we proceed
        │
        ├─ 3. foreach domainEvent in collected events:
        │     await _publisher.Publish(domainEvent, cancellationToken)
        │     (MediatR dispatches to all registered INotificationHandler<T>)
        │
        └─ 4. aggregates.ForEach(a => a.ClearDomainEvents())
              (clear AFTER successful dispatch)
```

#### Thread Safety Analysis

| Concern | Analysis | Safe? |
|---------|----------|-------|
| Multiple concurrent SaveChanges | Each HTTP request has its own scoped `AppDbContext` | ✅ Safe |
| Worker service SaveChanges | Worker creates scoped `IServiceScope` per operation → own `AppDbContext` | ✅ Safe |
| Event handler triggering another SaveChanges | Re-entrant call on same context → ChangeTracker snapshot is already complete | ✅ Safe (handlers use their own scope) |
| Double dispatch if handler throws | Events cleared only after all published — if handler throws, next call would re-dispatch | ⚠️ Mitigated by idempotent handlers |

#### Handler Idempotency Requirement

Domain event handlers **MUST be idempotent** because:
- If handler #3 throws after handlers #1 and #2 succeed, next request retry could re-dispatch all events
- Handlers should use `INSERT OR IGNORE` / `ON CONFLICT DO NOTHING` or check existence before inserting

#### Failure Handling Strategy

```
Scenario 1: Save fails (DB error)
  → base.SaveChangesAsync throws
  → events NOT dispatched
  → events NOT cleared (still on aggregate)
  → caller handles exception
  → Result: ✅ Consistent (no phantom events)

Scenario 2: Save succeeds, handler #1 dispatches fine, handler #2 throws
  → Transaction committed ✅
  → Handler #2 exception propagates
  → Events NOT cleared (partial dispatch)
  → Next retry: re-dispatches ALL events (handlers must be idempotent)
  → Result: ⚠️ Acceptable with idempotent handlers
  → Future: Outbox pattern eliminates this risk

Scenario 3: Save succeeds, all handlers succeed
  → Events cleared ✅
  → Result: ✅ Perfect
```

---

### 1.6 Application Layer Changes (Fix #1)

No changes to Application layer — just add handlers.

**New notification handlers to register:**

| Handler | Event | Action |
|---------|-------|--------|
| `EventCreatedAuditHandler` | `EventCreatedEvent` | Write AuditLog entry |
| `PhotoCreatedAuditHandler` | `PhotoCreatedEvent` | Write AuditLog entry |
| `PhotoDeletedCleanupHandler` | `PhotoDeletedEvent` | Queue embedding deletion |
| `EventDeactivatedHandler` | `EventDeactivatedEvent` | Write AuditLog entry |
| `FaceIndexCompletedHandler` | `FaceIndexCompletedEvent` | Update analytics (fire-and-forget) |

**Handler placement:** `EventPhoto.Application/{Domain}/EventHandlers/{EventName}Handler.cs`

---

### 1.7 Infrastructure Changes (Fix #1)

**AppDbContext.cs:**
- Add constructor parameter: `IPublisher publisher` (from MediatR.Contracts)
- Change `SaveChangesAsync`: collect events → save → publish → clear

**InfrastructureServiceExtensions.cs:**
- `IPublisher` is already registered by MediatR — no DI change needed
- `AppDbContext` is already scoped — publisher will be resolved from same scope

---

### 1.8 Database Changes (Fix #1)

No database changes required.

---

### 1.9 API / Worker Changes (Fix #1)

No changes to API or Worker — transparent to callers.

---

### 1.10 Performance Review (Fix #1)

| Concern | Analysis |
|---------|----------|
| Event dispatch overhead | Sub-millisecond per handler for simple audit/log writes |
| Handler count per save | Typically 1-3 handlers per save — negligible |
| Parallel dispatch | MediatR `Publish()` dispatches sequentially by default. Use `PublishStrategy` for parallel if needed |
| Memory | Events collected in `List<IDomainEvent>` — tiny footprint |
| DB round-trips | Each handler that writes to DB adds 1 round-trip — acceptable |

**Recommendation:** For high-throughput operations (bulk photo ingest), consider fire-and-forget for analytics handlers:
```
await Task.Run(() => _publisher.Publish(evt, CancellationToken.None));
// This separates analytics writes from the main request path
```

---

### 1.11 Migration Strategy (Fix #1)

1. Modify `AppDbContext.SaveChangesAsync` — single file change
2. Create handler skeletons in Application layer (audit, analytics)
3. Run all existing tests — behavior should be unchanged for handlers that don't exist yet
4. Deploy — domain events now dispatch silently to no handlers (no-op) until handlers are added
5. Incrementally add handlers per domain event

**Rollback:** Revert `AppDbContext.SaveChangesAsync` to original. Zero DB migration. Zero data risk.

---

### 1.12 Production Readiness Assessment (Fix #1)

| Dimension | Rating | Notes |
|-----------|--------|-------|
| Correctness | ✅ Critical Fix | Eliminates silent event loss |
| Safety | ✅ Safe | Additive change, backward compatible |
| Performance | ✅ Negligible overhead | Sub-ms per handler |
| Rollback | ✅ Trivial | One file, one method |
| Risk | ✅ Low | No migration, no schema change |

---

## Fix #2 — Legacy Face Pipeline Removal

### 2.1 Current State — Two Pipelines

#### Legacy Pipeline (INACTIVE)

```
IngestPhotoCommand (never dispatched from active code)
  ↓ photo.QueueForFaceIndexing() → Photo.FaceIndexStatus = Pending
  ↓ SaveChanges

FaceIndexingService (NOT REGISTERED — dead code)
  ↓ would poll: GetPendingFaceIndexAsync(batchSize)
  ↓ dispatches: ProcessFaceIndexCommand(photoId)

ProcessFaceIndexCommand
  ↓ faceRecognitionService.IndexPhotoAsync() (no quality scoring)
  ↓ FaceEmbedding.Create() (no QualityScore, no QualityTier, basic only)
  ↓ photo.MarkFaceIndexCompleted()
```

#### Active Pipeline (RUNNING)

```
CreatePhotoCommand (dispatched by FileWatcherService)
  ↓ Photo.Create() → FaceIndexStatus = NotRequired
  ↓ SaveChanges

ThumbnailProcessorService (polls Pending thumbnails, every 5s)
  ↓ generates thumbnail
  ↓ photo.MarkThumbnailDone()
  ↓ NotifyPhotoAddedAsync (SignalR)
  ↓ EnqueuePhotoForAiDiscoveryCommand(eventId, photoId)
    ↓ checks event.EnableFaceRecognition
    ↓ checks for existing active FaceProcessingJob (idempotent)
    ↓ FaceProcessingJob.Create(eventId, photoId)
    ↓ SaveChanges

AiDiscoveryPipelineService (polls FaceProcessingJob.Status = Pending, every 10s)
  ↓ bounded channel (cap 32) + concurrent processor
  ↓ ProcessAiDiscoveryJobCommand(jobId)
    ↓ Stage 1 Detecting: Python InsightFace HTTP call
    ↓ Stage 2 QualityChecking: FaceQualityService.Evaluate() → composite score
    ↓ Stage 3 Embedding: ArcFace 512-dim → FaceEmbedding.Create(with quality data)
    ↓ Stage 4 Indexing: embeddingRepository.AddRangeAsync() → pgvector
    ↓ FinalizeJobAsync:
        photo.MarkFaceIndexCompleted(faceCount)  ← uses Photo.FaceIndexStatus
        job.MarkCompleted()
        SaveChanges
    ↓ NotifyFaceIndexCompletedAsync (SignalR)
```

---

### 2.2 Dependency Analysis — What Can Be Removed

#### Files Safe to Delete

| File | Reason Safe |
|------|-------------|
| `EventPhoto.Worker/Services/FaceIndexing/FaceIndexingService.cs` | Not registered in `WorkerServiceExtensions`. Dead code. |
| `EventPhoto.Application/FaceSearch/Commands/ProcessFaceIndexCommand.cs` | Only ever dispatched by `FaceIndexingService`. No other caller. |
| `EventPhoto.Application/FaceSearch/Commands/QueueFaceIndexCommand.cs` | Only dispatched from `IngestPhotoCommandHandler`. |
| `EventPhoto.Application/Photos/Commands/IngestPhotoCommand.cs` | Not dispatched by any active service. `FileWatcherService` uses `CreatePhotoCommand`. |
| `EventPhoto.Application/Photos/Validators/IngestPhotoCommandValidator.cs` | Validator for `IngestPhotoCommand` — remove with command. |

#### Items to Retain (modified, not deleted)

| Item | Action | Reason |
|------|--------|--------|
| `IPhotoRepository.GetPendingFaceIndexAsync()` | Remove interface method | Only consumed by `FaceIndexingService` |
| `PhotoRepository.GetPendingFaceIndexAsync()` | Remove implementation | Same |
| `Photo.FaceIndexStatus` | **KEEP** | Still used by `ProcessAiDiscoveryJobCommand` via `photo.MarkFaceIndexCompleted()` |
| `Photo.FaceIndexRetryCount` | **KEEP** | Still updated by `Photo.MarkFaceIndexFailed()` in legacy path — remove `MarkFaceIndexFailed` references after cleanup |
| `Photo.QueueForFaceIndexing()` | **KEEP** | Still could be used — verify callers then remove |
| `Photo.MarkFaceIndexProcessing()` | **KEEP** | Called from `ProcessAiDiscoveryJobCommand` (new pipeline) |
| `FaceIndexStatus` enum values | **KEEP** | `NotRequired`, `Pending`, `Processing`, `Completed`, `Failed` all used by new pipeline |
| `IFaceRecognitionService` | **KEEP** | Used by BOTH old `ProcessFaceIndexCommand` and new `ProcessAiDiscoveryJobCommand`. After old removal, only new pipeline uses it. |

---

### 2.3 Risk Analysis — What Could Break

#### No-Break Confirmed:

| Check | Finding | Safe? |
|-------|---------|-------|
| `FaceIndexingService` registered? | ❌ NOT in `WorkerServiceExtensions.AddWorkerServices()` | ✅ No break |
| `IngestPhotoCommand` dispatched from FileWatcher? | FileWatcher uses `CreatePhotoCommand` only | ✅ No break |
| `IngestPhotoCommand` dispatched from any Controller? | No controller dispatches it | ✅ No break |
| `ProcessFaceIndexCommand` dispatched from AiDiscovery? | `AiDiscoveryPipelineService` dispatches `ProcessAiDiscoveryJobCommand` only | ✅ No break |
| `QueueFaceIndexCommand` dispatched from ThumbnailProcessor? | ThumbnailProcessor dispatches `EnqueuePhotoForAiDiscoveryCommand` only | ✅ No break |
| `GetPendingFaceIndexAsync` called from AiDiscoveryPipeline? | AiDiscoveryPipeline uses `FaceProcessingJobRepository` not `PhotoRepository` | ✅ No break |
| API endpoints depend on legacy commands? | No API controller dispatches any legacy commands | ✅ No break |
| `Photo.FaceIndexStatus.Pending` state currently set by IngestPhotoCommand? | `CreatePhotoCommand` does NOT call `photo.QueueForFaceIndexing()` → status stays `NotRequired` until AiDiscovery processes | ✅ No break |
| Existing `Photo` rows in DB with `FaceIndexStatus=Pending`? | These exist if `IngestPhotoCommand` was ever called historically | ⚠️ Data concern |

#### Historical Data Risk:

```
Risk: If any historical Photo rows have FaceIndexStatus = Pending
      (from IngestPhotoCommand ever being called in the past),
      those photos will NEVER get processed by any running service
      because:
        - FaceIndexingService is not running
        - AiDiscoveryPipeline only picks up FaceProcessingJob rows, not Photo.FaceIndexStatus

Mitigation: Run a one-time migration query:
  SELECT COUNT(*) FROM photos WHERE face_index_status = 1 (Pending)
    AND face_index_status NOT IN (SELECT photo_id FROM face_processing_jobs)
  → For any found: either create FaceProcessingJob entries or reset to NotRequired
```

---

### 2.4 Current Pipeline Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CURRENT STATE — DUAL PIPELINE                            │
│                                                                             │
│  ══════════ ACTIVE PIPELINE ══════════════════════════════════════════════  │
│                                                                             │
│  FileWatcherService → CreatePhotoCommand → Photo (FaceIndexStatus=NotReq)  │
│          │                                                                  │
│          ↓ (poll every 5s)                                                  │
│  ThumbnailProcessorService                                                  │
│    → thumbnail generated                                                    │
│    → EnqueuePhotoForAiDiscoveryCommand → FaceProcessingJob (Status=Pending) │
│          │                                                                  │
│          ↓ (poll every 10s, bounded channel cap=32)                         │
│  AiDiscoveryPipelineService → ProcessAiDiscoveryJobCommand                 │
│    → Detect → QualityCheck → Embed → Index → FaceEmbedding rows            │
│    → photo.MarkFaceIndexCompleted()  ← uses Photo.FaceIndexStatus          │
│                                                                             │
│  ══════════ DEAD PIPELINE (NOT RUNNING) ═════════════════════════════════  │
│                                                                             │
│  IngestPhotoCommand (never called)                                          │
│    → photo.QueueForFaceIndexing() → FaceIndexStatus = Pending              │
│          │                                                                  │
│          ↓ (poll every 10s — NOT REGISTERED)                               │
│  FaceIndexingService ← ❌ NOT IN WorkerServiceExtensions                   │
│    → ProcessFaceIndexCommand (no quality scoring, basic embeddings)         │
│    → FaceEmbedding.Create (missing QualityScore, QualityTier, Version)     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 2.5 Target Pipeline Diagram (After Fix #2)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TARGET STATE — SINGLE PIPELINE                           │
│                                                                             │
│  FileWatcherService → CreatePhotoCommand → Photo (FaceIndexStatus=NotReq)  │
│          │                                                                  │
│          ↓ (poll every 5s)                                                  │
│  ThumbnailProcessorService                                                  │
│    → thumbnail generated + SignalR notify                                   │
│    → EnqueuePhotoForAiDiscoveryCommand → FaceProcessingJob (Status=Pending) │
│          │                                                                  │
│          ↓ (poll every 10s, bounded channel cap=32)                         │
│  AiDiscoveryPipelineService → ProcessAiDiscoveryJobCommand                 │
│    Stage 1: Detecting    → Python InsightFace                               │
│    Stage 2: QualityCheck → FaceQualityService composite score               │
│    Stage 3: Embedding    → ArcFace 512-dim vector                          │
│    Stage 4: Indexing     → pgvector HNSW INSERT                            │
│    → photo.MarkFaceIndexCompleted(faceCount)                               │
│    → job.MarkCompleted()                                                    │
│    → SignalR: FaceIndexCompleted notification                               │
│                                                                             │
│  DeadLetterProcessorService → manual retry/ignore for failed jobs           │
│  SelfieRetentionService → periodic embedding TTL purge                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 2.6 Domain Layer Changes (Fix #2)

```
EventPhoto.Domain — NO ENTITY CHANGES

Keep:
  Photo.FaceIndexStatus (property + enum values)
  Photo.MarkFaceIndexProcessing()
  Photo.MarkFaceIndexCompleted()
  Photo.MarkFaceIndexFailed()       ← keep until confirmed unused
  Photo.QueueForFaceIndexing()      ← remove once IngestPhotoCommand deleted

Remove from IPhotoRepository interface:
  GetPendingFaceIndexAsync()
```

---

### 2.7 Application Layer Changes (Fix #2)

**Delete:**
```
EventPhoto.Application/Photos/Commands/IngestPhotoCommand.cs
EventPhoto.Application/Photos/Validators/IngestPhotoCommandValidator.cs
EventPhoto.Application/FaceSearch/Commands/QueueFaceIndexCommand.cs
EventPhoto.Application/FaceSearch/Commands/ProcessFaceIndexCommand.cs
```

**Verify remaining:**
```
EventPhoto.Application/FaceSearch/Commands/StartFaceSearchCommand.cs   → KEEP
EventPhoto.Application/FaceSearch/Commands/ExpireFaceSessionsCommand.cs → KEEP
EventPhoto.Application/AiDiscovery/Commands/*.cs                        → KEEP ALL
```

---

### 2.8 Infrastructure Layer Changes (Fix #2)

**PhotoRepository.cs:**
- Remove `GetPendingFaceIndexAsync()` implementation

**IPhotoRepository.cs (Domain Interface):**
- Remove `GetPendingFaceIndexAsync()` from interface

---

### 2.9 Worker Layer Changes (Fix #2)

**Delete:**
```
EventPhoto.Worker/Services/FaceIndexing/FaceIndexingService.cs
```

`WorkerServiceExtensions.cs` — no change needed (FaceIndexingService was never registered).

---

### 2.10 Data Migration (Fix #2)

**Pre-removal data safety query (run against production DB before deploying):**

```sql
-- Find photos stuck in Pending state with no FaceProcessingJob
-- face_index_status: 1 = Pending (from FaceIndexStatus enum)
SELECT p.id, p.file_name, p.event_id, p.face_index_status, p.created_at
FROM photos p
WHERE p.face_index_status = 1  -- Pending
  AND NOT EXISTS (
      SELECT 1 FROM face_processing_jobs fpj
      WHERE fpj.photo_id = p.id
        AND fpj.status NOT IN (3, 8, 9)  -- not Completed/DeadLettered/Ignored
  )
  AND p.is_deleted = false;

-- If any rows found, create FaceProcessingJob entries OR reset to NotRequired:
-- Option A: Create jobs (recommended if event.enable_face_recognition = true):
INSERT INTO face_processing_jobs (id, event_id, photo_id, status, retry_count, priority, created_at, updated_at)
SELECT gen_random_uuid(), p.event_id, p.id, 0, 0, 2, NOW(), NOW()
FROM photos p
WHERE p.face_index_status = 1
  AND NOT EXISTS (SELECT 1 FROM face_processing_jobs fpj WHERE fpj.photo_id = p.id);

-- Option B: Reset stuck photos to NotRequired
UPDATE photos SET face_index_status = 0, updated_at = NOW()  -- 0 = NotRequired
WHERE face_index_status = 1
  AND NOT EXISTS (SELECT 1 FROM face_processing_jobs fpj WHERE fpj.photo_id = p.id);
```

**No EF Core migration required** — no schema changes.

---

### 2.11 Fallback Strategy (Fix #2)

Since `FaceIndexingService` was never registered:
- There is no active behavior to roll back.
- If code deletion causes compilation errors, revert the file deletions.
- No runtime fallback needed — no running behavior changes.

---

### 2.12 Production Readiness Assessment (Fix #2)

| Dimension | Rating | Notes |
|-----------|--------|-------|
| Safety | ✅ Safe | Service was never running |
| Behavior change | ✅ None | Active pipeline unchanged |
| DB schema change | ✅ None | No migration |
| Rollback | ✅ Trivial | Restore deleted files |
| Risk | ✅ Very Low | Pure dead code removal |
| Prerequisite | ⚠️ Data check | Run orphaned-photo SQL query first |

---

## Fix #3 — Subscription Enforcement

### 3.1 Current State

```
Subscription entity: ✅ Modeled
  - Plan: Trial(5events/2users) | Starter(20/5) | Professional(100/∞) | Enterprise(∞)
  - State: Trial | Active | GracePeriod | Expired | Cancelled
  - MaxEvents, MaxUsersPerStudio fields
  - IsOperational property

Subscription API: ✅ Controller exists (GET + activate)
Subscription UI: ✅ SubscriptionPage.tsx exists

ENFORCEMENT: ❌ Zero runtime enforcement anywhere
  - CreateEventCommand: no limit check
  - CreateStudioUserCommand: no limit check
  - StartFaceSearchCommand: no plan check
  - No middleware blocks anything
  - Expired studio → full access to everything
```

---

### 3.2 Architectural Risk

| Risk | Severity | Description |
|------|----------|-------------|
| Revenue leakage | 🔴 Critical | Trial studios can create unlimited events |
| Commercial viability | 🔴 Critical | No enforcement = no commercial product |
| Security bypass | 🔴 High | Disabling frontend menus provides no real restriction |
| Audit failure | 🟡 High | Cannot track plan violations |
| Complexity | 🟡 Medium | Wrong pattern (scattered if/else) creates maintenance nightmare |

---

### 3.3 Business Rules (Definitive)

```
PLANS (Updated from assessment — align with product requirements):
  Trial         → 30 days, 5 events, 3 users, unlimited face searches
  ExtendedTrial → 45 days total (15 day extension, one-time), same limits
  Professional  → 100 events, 10 users, all features
  Premium       → Unlimited events, unlimited users, all features, priority AI

STATES:
  Trial         → Operational. Limits apply.
  Active        → Operational. Plan limits apply.
  GracePeriod   → Operational. Plan limits apply. Show renewal warning.
  Expired       → Restricted. Read-only. No creates. Show upgrade prompt.
  Cancelled     → Restricted. Read-only. No creates.

NEVER BLOCK (regardless of plan/state):
  ✅ Login / authentication
  ✅ Read any event (GET /api/events)
  ✅ Read any photo (GET /api/photos)
  ✅ Browse public galleries (/gallery/*)
  ✅ Download photos (already paid content)
  ✅ View subscription status
  ✅ View analytics / statistics
  ✅ View audit logs

BLOCK when limits exceeded or expired:
  🚫 Create Event  (limit: MaxEvents)
  🚫 Create Studio User  (limit: MaxUsersPerStudio)
  🚫 Start Face Search Session  (Expired/Cancelled state)
  🚫 Create Guest Upload Session  (Expired/Cancelled state)
  🚫 Future premium feature commands

BLOCK mechanics:
  → Return 402 Payment Required with clear error message
  → Include: currentCount, limit, plan, upgradeUrl
```

---

### 3.4 Why NOT Scattered `if(plan == Trial)` Checks

```
WRONG PATTERN:
  CreateEventCommandHandler:
    if (subscription.Plan == SubscriptionPlan.Trial && eventCount >= 5)
        return Failure("limit reached");
    
  CreateUserCommandHandler:
    if (subscription.Plan == SubscriptionPlan.Trial && userCount >= 3)
        return Failure("limit reached");

Problems:
  - Every future command needs the same check
  - Plan logic duplicated in N handlers
  - Adding a new plan means editing N files
  - Adding a new enforced command means adding a check (forgettable)
  - No central audit point
  - Business rules scattered across application layer
  - Testing requires testing each handler separately

CORRECT PATTERN:
  IFeatureManager.CanCreateEvent() → centralized
  One place to change plan limits
  One place to add new plans
  One place to test
  MediatR behavior enforces it transparently
```

---

### 3.5 IFeatureManager Design

#### Interface Design

```csharp
// Location: EventPhoto.Application/Common/Interfaces/IFeatureManager.cs

/// <summary>
/// Central subscription and feature enforcement service.
/// All gate-keeping logic lives here — no plan checks in command handlers.
///
/// Contract:
///   IsAllowed() → check only, no DB write
///   EnsureAllowed() → check + throws SubscriptionException if denied
///   Counts are lazily loaded and cached within the request scope
/// </summary>
public interface IFeatureManager
{
    // ── Event limits ──────────────────────────────────────────────────────────
    Task<FeatureCheckResult> CanCreateEventAsync(CancellationToken ct = default);

    // ── User limits ───────────────────────────────────────────────────────────
    Task<FeatureCheckResult> CanCreateUserAsync(CancellationToken ct = default);

    // ── AI / Face features ────────────────────────────────────────────────────
    Task<FeatureCheckResult> CanStartFaceSearchAsync(CancellationToken ct = default);
    Task<FeatureCheckResult> CanUseAiStudioAsync(CancellationToken ct = default);

    // ── Guest features ────────────────────────────────────────────────────────
    Task<FeatureCheckResult> CanCreateGuestUploadSessionAsync(CancellationToken ct = default);
    Task<FeatureCheckResult> CanUseGuestMemoriesAsync(CancellationToken ct = default);

    // ── Generic utility ───────────────────────────────────────────────────────

    /// <summary>Returns the current subscription for display purposes.</summary>
    Task<Subscription> GetSubscriptionAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns current usage counts for a given feature.
    /// Used by UI to display "3 of 5 events used" etc.
    /// </summary>
    Task<FeatureUsage> GetUsageAsync(FeatureKey feature, CancellationToken ct = default);
}
```

#### Result Types

```csharp
// Location: EventPhoto.Application/Common/Models/FeatureCheckResult.cs

public sealed record FeatureCheckResult(
    bool IsAllowed,
    string? DenialReason = null,
    int? CurrentCount = null,
    int? Limit = null,
    SubscriptionPlan Plan = default,
    string? UpgradeHint = null)
{
    public static FeatureCheckResult Allowed() => new(IsAllowed: true);

    public static FeatureCheckResult Denied(
        string reason,
        int? currentCount = null,
        int? limit = null,
        string? upgradeHint = null)
        => new(false, reason, currentCount, limit, upgradeHint: upgradeHint);
}

public sealed record FeatureUsage(
    FeatureKey Feature,
    int CurrentCount,
    int? Limit,
    bool IsUnlimited);

public enum FeatureKey
{
    Events,
    Users,
    FaceSearchSessions,
    GuestUploadSessions
}
```

---

### 3.6 Domain Changes (Fix #3)

#### Update SubscriptionPlan Enum

```
Current plans:   Trial, Starter, Professional, Enterprise
Required plans:  Trial, ExtendedTrial, Professional, Premium

Changes:
  - Rename Starter(1) → ExtendedTrial(1) OR add ExtendedTrial as new value
  - Rename Enterprise(3) → Premium(3) OR add Premium as new value
  - Keep numeric values for backward DB compatibility
  - IMPORTANT: Starter and Enterprise may have existing DB rows

Decision: Add new values, mark old ones Obsolete
  Trial         = 0  (unchanged)
  ExtendedTrial = 1  (new — was Starter)
  Professional  = 2  (unchanged)
  Premium       = 3  (new — was Enterprise)

[Obsolete] Starter    = 10  (preserve for existing DB rows)
[Obsolete] Enterprise = 11  (preserve for existing DB rows)
```

#### Update Subscription.PlanLimits()

```
Trial:         MaxEvents=5,   MaxUsers=3,  TrialDays=30
ExtendedTrial: MaxEvents=5,   MaxUsers=3,  TrialDays=45
Professional:  MaxEvents=100, MaxUsers=10, TrialDays=0 (licensed)
Premium:       MaxEvents=0,   MaxUsers=0,  TrialDays=0 (unlimited)
```

#### Add TrialExtension Domain Method

```csharp
// On Subscription entity:
public bool HasUsedTrialExtension { get; private set; }

public void ExtendTrial()
{
    if (Plan is not (SubscriptionPlan.Trial or SubscriptionPlan.ExtendedTrial))
        throw new DomainException("Trial extension is only available on Trial plans.");
    if (HasUsedTrialExtension)
        throw new DomainException("Trial extension has already been used.");
    if (State == SubscriptionState.Expired)
        throw new DomainException("Cannot extend an expired subscription.");

    Plan = SubscriptionPlan.ExtendedTrial;
    ExpiresAt = ExpiresAt?.AddDays(15) ?? DateTimeOffset.UtcNow.AddDays(15);
    GracePeriodEndsAt = ExpiresAt.Value.AddDays(7);
    HasUsedTrialExtension = true;
    (MaxEvents, MaxUsersPerStudio) = PlanLimits(Plan);
    Touch();
}
```

---

### 3.7 Application Layer Changes (Fix #3)

#### IFeatureManager Implementation Strategy

```
Location: EventPhoto.Infrastructure/Services/Subscription/FeatureManager.cs

Implements: IFeatureManager (defined in Application.Common.Interfaces)

Dependencies (scoped):
  ISubscriptionRepository  → get current subscription
  IEventRepository         → count active events
  IUserRepository          → count active users

Caching strategy within a single request:
  Use Lazy<Task<T>> per property to avoid N+1 within same request scope.
  The scoped lifetime means cache is request-scoped automatically.
```

#### New Commands (Subscription)

```
ExtendTrialCommand       → activates 15-day extension (one-time)
```

#### Enforcement Point: MediatR Pipeline Behavior

```
NEW: SubscriptionEnforcementBehavior<TRequest, TResponse>

Registration order (in ApplicationServiceExtensions):
  1. LoggingBehavior
  2. ValidationBehavior
  3. SubscriptionEnforcementBehavior   ← NEW (after validation, before handler)

Mechanism:
  - TRequest is checked against a registry of "gated commands"
  - If TRequest implements IRequiresFeature<TFeatureKey>, check IFeatureManager
  - If check fails, return Result.Failure (NOT throw — consistent with Result pattern)
  - Handler never executes
```

#### IRequiresFeature Marker Interface

```csharp
// Location: EventPhoto.Application/Common/Interfaces/IRequiresFeature.cs

/// <summary>
/// Marker interface on commands that require a subscription feature check.
/// The SubscriptionEnforcementBehavior reads this to determine what to check.
/// </summary>
public interface IRequiresFeature
{
    FeatureKey RequiredFeature { get; }
}

// Commands that implement this:
// CreateEventCommand       → FeatureKey.Events
// CreateStudioUserCommand  → FeatureKey.Users
// StartFaceSearchCommand   → FeatureKey.FaceSearchSessions
// CreateGuestUploadSessionCommand → FeatureKey.GuestUploadSessions
```

---

### 3.8 CQRS Integration Design (Fix #3)

#### The Enforcement Behavior

```
SubscriptionEnforcementBehavior<TRequest, TResponse>

Pipeline position: AFTER ValidationBehavior, BEFORE Handler

Logic:
  if TRequest does NOT implement IRequiresFeature:
    → pass through immediately (no-op for 99% of queries)

  if TRequest implements IRequiresFeature:
    → featureKey = request.RequiredFeature
    → result = await _featureManager.CanUse(featureKey)
    → if !result.IsAllowed:
        → log the denial (audit trail)
        → return Result.Failure (with plan info, upgrade hint)
        → handler never executes

Performance note:
  The IRequiresFeature check is an interface type check (is operator) — O(1), sub-nanosecond.
  Only gated commands hit IFeatureManager.
  IFeatureManager loads subscription once per request scope (lazy cached).
```

#### Command Modification Strategy

```
OPTION A: Commands implement IRequiresFeature directly
  → Clean interface — command declares its own requirement
  → Slightly pollutes the command record with infrastructure concern
  → Simple

OPTION B: External registry in behavior
  → Dictionary<Type, FeatureKey> in behavior constructor
  → Commands stay clean (pure CQRS)
  → Adding new command requires registering in dictionary (forgettable)
  → Less clean for extensibility

RECOMMENDATION: Option A
  Commands declare IRequiresFeature.
  It's a domain concern — "this operation requires a subscription feature".
  Clean, explicit, self-documenting.
```

---

### 3.9 API Layer Changes (Fix #3)

#### New Subscription Endpoints

```
POST /api/subscription/extend-trial     → ExtendTrialCommand
GET  /api/subscription                   → already exists

Response when blocked (HTTP 402 Payment Required):
{
  "success": false,
  "error": "Event limit reached for your Trial plan",
  "data": {
    "currentCount": 5,
    "limit": 5,
    "plan": "Trial",
    "upgradeHint": "Upgrade to Professional for up to 100 events",
    "upgradeUrl": "/admin/platform/subscription"
  }
}
```

#### Existing Controller Behavior

```
EventsController.Post():
  → mediator.Send(new CreateEventCommand(...))
  → SubscriptionEnforcementBehavior intercepts
  → Returns Result.Failure if limit exceeded
  → Controller returns 402 with structured error
  → Handler never executes
  
No change needed to controller code — the pipeline behavior handles it.
```

---

### 3.10 Infrastructure Changes (Fix #3)

#### FeatureManager Implementation Design

```csharp
// EventPhoto.Infrastructure/Services/Subscription/FeatureManager.cs

public sealed class FeatureManager : IFeatureManager
{
    // All dependencies are scoped → shared within one HTTP request
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<FeatureManager> _logger;

    // Lazy per-request caching (avoid N+1 on multiple calls within same request)
    private Subscription? _cachedSubscription;

    // Key decision: FAIL OPEN for operational continuity
    // If subscription cannot be loaded (DB down), log warning and ALLOW
    // This prevents a DB hiccup from locking out a studio
    private bool _failOpen = true;
}
```

#### Fail-Open vs Fail-Closed

| Approach | Behavior on DB error | Business Impact |
|----------|---------------------|----------------|
| Fail-Open (recommended) | Allow operation if subscription can't load | Studio keeps working; revenue risk minimal |
| Fail-Closed | Block all gated operations if subscription can't load | Studio locked out; support calls; bad UX |

**Decision: Fail-Open** with error logging. A studio with 5 events won't suddenly create 1000 events during a DB hiccup.

---

### 3.11 Database Changes (Fix #3)

#### Migration: Update SubscriptionPlan + Add HasUsedTrialExtension

```sql
-- Migration: AddExtendedTrialAndPlanRename
-- Purpose: Add HasUsedTrialExtension column, update MaxUsers default for Trial

ALTER TABLE subscriptions ADD COLUMN has_used_trial_extension boolean NOT NULL DEFAULT false;

-- Update Trial plan limits: MaxUsersPerStudio 2 → 3
UPDATE subscriptions SET max_users_per_studio = 3 WHERE plan = 0;  -- 0 = Trial

-- No data loss: existing Trial/Starter/Professional/Enterprise rows map to:
-- Trial(0) → Trial(0), Starter(1) → ExtendedTrial(1), 
-- Professional(2) → Professional(2), Enterprise(3) → Premium(3)
-- (same integer values, only the enum name changes in C#)
```

---

### 3.12 Worker Changes (Fix #3)

No worker changes needed. Background services (ThumbnailProcessorService, AiDiscoveryPipelineService) are internal operations not subject to subscription limits.

**Rationale:** Subscription gates new data creation. Processing photos that already exist is a committed resource — blocking processing would corrupt data state.

---

### 3.13 Security Review (Fix #3)

#### Attack Scenarios

| Attack Vector | Protection |
|---|---|
| User calls `POST /api/events` directly (bypasses UI) | ✅ MediatR behavior intercepts — no UI involvement |
| User modifies JWT claims | ✅ JWT is signed — tampering invalidates signature |
| User calls API with valid token but expired subscription | ✅ FeatureManager checks subscription state, not JWT claims |
| User finds undocumented endpoint | ✅ All command dispatch goes through MediatR pipeline |
| Race condition: 2 requests create event at limit simultaneously | ⚠️ Need DB-level constraint or check-then-act with count query + transaction |

#### Race Condition Mitigation

```
Without protection: two simultaneous CreateEvent requests at count=4 (limit=5)
  Request A: count() = 4 → allowed → creates event (count = 5)
  Request B: count() = 4 → allowed → creates event (count = 6 — OVER LIMIT)

Mitigation options:
  A: Serialized event creation (pessimistic lock) — impacts performance
  B: DB constraint: CHECK (SELECT count(*) < MaxEvents) — complex
  C: Accept off-by-one for N events (studios are unlikely to hit exact limit simultaneously)
     Log when count exceeds limit and trigger grace period
  D: Re-check count inside DB transaction after insert (SERIALIZABLE isolation)

RECOMMENDATION: Option C for V1 (practical), Option D for V2 if needed.
The business risk of being 1 event over limit is minimal vs the complexity of pessimistic locking.
```

---

### 3.14 Usage Tracking Design (Fix #3)

#### Current counts come from:

```
EventCount = SELECT COUNT(*) FROM events WHERE is_deleted = false
UserCount  = SELECT COUNT(*) FROM users WHERE is_active = true
```

#### Add cached count query approach

```csharp
// Within FeatureManager (request-scoped):
// Lazy load + cache count queries within same request
private int? _cachedEventCount;
private int? _cachedUserCount;

// On CanCreateEventAsync():
//   1. GetSubscriptionAsync() → check IsOperational
//   2. GetCurrentEventCount() → lazy load if not cached
//   3. Compare against subscription.MaxEvents (0 = unlimited)
```

**Performance:** Two DB queries per gated operation, cached across multiple calls within same request. Sub-millisecond for simple count queries on indexed tables.

---

### 3.15 Future Subscription Readiness (Fix #3)

| Future Need | Readiness After Fix #3 |
|---|---|
| Add new plan | Add enum value + `PlanLimits()` case — 2 files |
| Add new gated feature | Implement `IRequiresFeature` on command + add `CanUse*` method — 3 files |
| Time-based feature flags | Add `ExpiresAt` to feature check + check in FeatureManager |
| Per-event feature flags | Extend `FeatureCheckResult` with event-level context |
| Remote feature flags (LaunchDarkly) | Replace `FeatureManager` implementation without changing interface |
| Payment webhook → activate | Add `ActivateSubscriptionCommand` handler (already exists) |
| Plan downgrade | Add `DowngradeCommand` with data safety checks |
| Multi-tenant | FeatureManager becomes tenant-aware — interface unchanged |

---

### 3.16 Migration Strategy (Fix #3)

```
Phase 1: Domain + DB (no visible behavior change)
  1. Add HasUsedTrialExtension column (migration)
  2. Update SubscriptionPlan enum
  3. Update Subscription.PlanLimits() 
  4. Add Subscription.ExtendTrial() domain method
  5. Deploy — no enforcement yet

Phase 2: Application + Infrastructure (no API change yet)
  6. Implement IFeatureManager interface in Application layer
  7. Implement FeatureManager in Infrastructure layer
  8. Register in DI
  9. Add IRequiresFeature to gated commands
  10. Deploy — FeatureManager exists but not in pipeline yet

Phase 3: Pipeline Behavior (enforcement goes live)
  11. Add SubscriptionEnforcementBehavior
  12. Register in ApplicationServiceExtensions (after ValidationBehavior)
  13. Test all enforcement paths
  14. Deploy with feature flag (ApplySubscriptionEnforcement = true/false in appsettings)
  15. Enable enforcement flag

Phase 4: API + UI
  16. Add ExtendTrial endpoint
  17. Update React SubscriptionPage to show usage meters
  18. Add 402 error handling in React global error handler
```

---

### 3.17 Rollback Strategy (Fix #3)

| Phase | Rollback |
|-------|---------|
| Phase 1 (DB) | Add rollback migration to remove column |
| Phase 2 (code) | Remove FeatureManager + interface (no runtime effect) |
| Phase 3 (behavior) | Remove from pipeline registration OR set `ApplySubscriptionEnforcement = false` |
| Phase 4 (API/UI) | Revert UI changes |

**Critical:** Phase 3 rollback is the most important. The `appsettings.json` feature flag approach allows instant rollback without deployment.

---

### 3.18 Production Readiness Assessment (Fix #3)

| Dimension | Rating | Notes |
|-----------|--------|-------|
| Business impact | 🔴 Critical fix | Enables commercial launch |
| Security | ✅ Backend enforced | UI cannot be bypassed |
| Performance | ✅ Minimal overhead | 2 DB count queries, cached per-request |
| Extensibility | ✅ Excellent | New plans/features require minimal changes |
| Rollback | ✅ Feature-flagged | Instant rollback without redeploy |
| Risk | 🟡 Medium | Must test all command paths before enabling |

---

## Implementation Order

### Why This Order Matters

```
Fix #1 (Domain Events) must come FIRST because:
  - Fix #3 relies on SubscriptionEnforcementBehavior
  - The enforcement behavior may raise domain events (usage alerts)
  - Domain events must be wired before they're used

Fix #2 (Legacy Pipeline Removal) is independent but:
  - Simplifies the codebase before adding new subscription code
  - Reduces confusion between old and new pipelines

Fix #3 (Subscription) comes LAST because:
  - Requires stable domain event system (Fix #1)
  - Should be done on clean codebase (Fix #2)
  - Has the most business risk — needs thorough testing
```

### Recommended Implementation Sprint Plan

```
Sprint 1 (1-2 days):
  ✦ Fix #1: Wire domain event dispatcher in AppDbContext
  ✦ Fix #1: Create initial set of event handlers (audit for key events)
  ✦ Fix #1: Deploy + verify events flowing in logs

Sprint 2 (0.5 days):
  ✦ Fix #2: Run orphaned-photo SQL query on production
  ✦ Fix #2: Delete legacy files (FaceIndexingService, legacy commands)
  ✦ Fix #2: Remove GetPendingFaceIndexAsync from interface + implementation
  ✦ Fix #2: Deploy + verify AI pipeline still works

Sprint 3 (2-3 days):
  ✦ Fix #3: DB migration (HasUsedTrialExtension, plan enum update)
  ✦ Fix #3: Update Subscription domain entity (ExtendTrial, updated limits)
  ✦ Fix #3: Implement IFeatureManager + FeatureManager
  ✦ Fix #3: Add IRequiresFeature to gated commands
  ✦ Fix #3: Implement SubscriptionEnforcementBehavior
  ✦ Fix #3: Add ExtendTrial API endpoint
  ✦ Fix #3: Deploy with enforcement flag OFF
  ✦ Fix #3: QA all enforcement scenarios
  ✦ Fix #3: Enable enforcement flag
```

---

## Cross-Cutting Concerns

### Error Response Standardization

All three fixes should return consistent error shapes:

```json
// Standard error (400):
{ "success": false, "error": "Event name is required." }

// Subscription enforcement (402):
{
  "success": false,
  "error": "Event limit reached",
  "subscriptionError": {
    "plan": "Trial",
    "feature": "Events",
    "currentCount": 5,
    "limit": 5,
    "upgradeRequired": true,
    "upgradeUrl": "/admin/platform/subscription"
  }
}
```

### Audit Logging Strategy

With Fix #1 (domain events) wired:
- Every `EventCreatedEvent` → write `AuditLog` entry
- Every `EventDeactivatedEvent` → write `AuditLog` entry
- Every subscription enforcement denial → write `AuditLog` entry (policy violation)
- Every `SubscriptionActivatedEvent` (new) → write `AuditLog` entry

### Performance Baseline (all three fixes combined)

| Operation | Added Overhead | Acceptable? |
|-----------|---------------|-------------|
| CreateEvent (allowed) | +2 DB queries (subscription check, event count) | ✅ <5ms |
| CreateEvent (denied) | +2 DB queries, returns early | ✅ <5ms |
| Any read operation | Zero overhead (not gated) | ✅ None |
| Photo ingest (domain events) | +1 DB write per handler | ✅ <2ms |
| AI pipeline tick | Zero overhead | ✅ None |
| SaveChanges (no events) | Zero overhead | ✅ None |

### Testing Strategy

```
Fix #1: Unit test AppDbContext.SaveChangesAsync
  → Verify events dispatched after save
  → Verify events NOT dispatched if save throws
  → Verify events cleared after dispatch

Fix #2: Integration test AI pipeline
  → Upload photo → confirm FaceProcessingJob created
  → Confirm no error from missing FaceIndexingService

Fix #3: Integration test enforcement
  → Create 5 events on Trial → 6th should return 402
  → Create 3 users on Trial → 4th should return 402
  → Activate Professional → 6th event creation allowed
  → Expire subscription → CreateEvent returns 402
  → GET /api/events → always returns 200 (read allowed)
  → Bypass via direct HTTP POST with valid JWT → still blocked
```

---

*End of Platform Foundation Stabilization Strategy*

---

**Document metadata:**

| Field | Value |
|-------|-------|
| Document type | Architecture & Implementation Strategy |
| Status | Approved for implementation — awaiting code review |
| Codebase | PixBridge (EventPhoto.*) |
| Fixes | 3 critical production readiness fixes |
| Estimated effort | 4-5 developer days total |
| DB migrations required | 1 (Fix #3 only) |
| Breaking changes | None (all backward compatible) |
| Rollback available | Yes, all three fixes |
