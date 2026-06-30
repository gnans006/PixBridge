CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Username" VARCHAR(100) NOT NULL UNIQUE,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Admin',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastLoginAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Events" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT NULL,
    "EventType" VARCHAR(50) NOT NULL,
    "EventDate" DATE NOT NULL,
    "VenueName" VARCHAR(200) NULL,
    "ClientName" VARCHAR(200) NULL,
    "WatchFolder" TEXT NOT NULL,
    "ThumbnailFolder" TEXT NOT NULL,
    "QrCodePath" TEXT NULL,
    "QrCodeUrl" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "PhotoCount" INT NOT NULL DEFAULT 0,
    "TotalSizeBytes" BIGINT NOT NULL DEFAULT 0,
    "CreatedBy" UUID NOT NULL REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Events_EventDate" ON "Events"("EventDate");
CREATE INDEX "IX_Events_IsActive" ON "Events"("IsActive") WHERE "IsDeleted" = FALSE;

CREATE TABLE "Photos" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" UUID NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "FileName" VARCHAR(255) NOT NULL,
    "OriginalPath" TEXT NOT NULL,
    "ThumbnailPath" TEXT NOT NULL,
    "FileSizeBytes" BIGINT NOT NULL,
    "MimeType" VARCHAR(100) NOT NULL,
    "Width" INT NULL,
    "Height" INT NULL,
    "TakenAt" TIMESTAMPTZ NULL,
    "CapturedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "DownloadCount" INT NOT NULL DEFAULT 0,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "ThumbnailStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Photos_EventId" ON "Photos"("EventId");
CREATE INDEX "IX_Photos_CapturedAt" ON "Photos"("CapturedAt" DESC);
CREATE INDEX "IX_Photos_ThumbnailStatus" ON "Photos"("ThumbnailStatus") WHERE "ThumbnailStatus" <> 'Done';

CREATE TABLE "DownloadLogs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "PhotoId" UUID NOT NULL REFERENCES "Photos"("Id") ON DELETE CASCADE,
    "EventId" UUID NOT NULL REFERENCES "Events"("Id") ON DELETE CASCADE,
    "IpAddress" VARCHAR(50) NULL,
    "UserAgent" TEXT NULL,
    "DownloadedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_DownloadLogs_PhotoId" ON "DownloadLogs"("PhotoId");
CREATE INDEX "IX_DownloadLogs_EventId" ON "DownloadLogs"("EventId");
CREATE INDEX "IX_DownloadLogs_DownloadedAt" ON "DownloadLogs"("DownloadedAt" DESC);

CREATE TABLE "SystemSettings" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Key" VARCHAR(100) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Description" TEXT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO "SystemSettings" ("Id", "Key", "Value", "Description") VALUES
    (gen_random_uuid(), 'app.name', 'PixBridge', 'Application display name'),
    (gen_random_uuid(), 'app.serverUrl', 'http://192.168.10.10', 'LAN URL guests use to connect'),
    (gen_random_uuid(), 'gallery.pageSize', '50', 'Photos per page in gallery'),
    (gen_random_uuid(), 'thumbnail.width', '400', 'Thumbnail max width in pixels'),
    (gen_random_uuid(), 'thumbnail.height', '400', 'Thumbnail max height in pixels'),
    (gen_random_uuid(), 'thumbnail.quality', '85', 'Thumbnail JPEG quality (1-100)'),
    (gen_random_uuid(), 'download.rateLimit', '30', 'Max downloads per IP per minute'),
    (gen_random_uuid(), 'watcher.extensions', '.jpg,.jpeg,.png,.cr2,.nef,.arw,.dng,.tiff', 'File extensions to watch');
