export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  username: string;
  role: string;
  expiresAt: string;
}

export interface AuthUser {
  id?: string;
  username: string;
  role: string;
}

export interface EventResponse {
  id: string;
  name: string;
  description?: string;
  eventType: string;
  eventDate: string;
  venueName?: string;
  clientName?: string;
  watchFolder: string;
  qrCodeUrl?: string;
  isActive: boolean;
  photoCount: number;
  totalSize: string;
  createdAt: string;
  galleryRecentCount?: number;
  enableFaceRecognition: boolean;
  allowGalleryBrowsing: boolean;
  allowFaceSearch: boolean;
  restrictDownloadsToMatchedPhotos: boolean;
  faceMatchThreshold: number;
}

export interface CreateEventRequest {
  name: string;
  eventType: string;
  eventDate: string;
  watchFolder: string;
  description?: string;
  venueName?: string;
  clientName?: string;
  galleryRecentCount?: number;
  enableFaceRecognition?: boolean;
  allowGalleryBrowsing?: boolean;
  allowFaceSearch?: boolean;
  restrictDownloadsToMatchedPhotos?: boolean;
  faceMatchThreshold?: number;
}

export interface PhotoResponse {
  id: string;
  eventId: string;
  fileName: string;
  thumbnailUrl: string;
  originalUrl: string;
  fileSizeBytes: number;
  width?: number;
  height?: number;
  takenAt?: string;
  capturedAt: string;
  downloadCount: number;
  thumbnailStatus: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface DashboardStatsResponse {
  totalEvents: number;
  activeEvents: number;
  totalPhotos: number;
  totalDownloads: number;
  totalStorageBytes: number;
  totalStorageHuman: string;
}

export interface EventStatisticsResponse {
  eventId: string;
  eventName: string;
  totalPhotos: number;
  totalDownloads: number;
  totalSizeBytes: number;
  totalSizeHuman: string;
}

export interface SystemSetting {
  id: string;
  key: string;
  value: string;
  description?: string;
}

// ── Watermark ────────────────────────────────────────────────────────────────

export type WatermarkMode =
  | 'Disabled'
  | 'StudioBranding'
  | 'EventBranding'
  | 'StudioAndEvent'
  | 'CustomText'
  | 'DynamicTemplate';

export type WatermarkStyle = 'Corner' | 'Center' | 'Diagonal' | 'RepeatedPattern' | 'BottomRibbon';

export type WatermarkScale = 'Small' | 'Medium' | 'Large' | 'Auto';

export interface WatermarkConfigResponse {
  id: string;
  eventId: string;
  enabled: boolean;
  mode: WatermarkMode;
  style: WatermarkStyle;
  opacity: number;
  scale: WatermarkScale;
  customText?: string;
  template?: string;
  logoPath?: string;
  includeStudioName: boolean;
  includeEventName: boolean;
  includeDownloadDate: boolean;
  applyOnDownload: boolean;
  textColor: string;
  fontName?: string;
  backgroundOpacity: number;
  applyOnPreview: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertWatermarkConfigRequest {
  enabled: boolean;
  mode: WatermarkMode;
  style: WatermarkStyle;
  opacity: number;
  scale: WatermarkScale;
  customText?: string;
  template?: string;
  logoPath?: string;
  includeStudioName: boolean;
  includeEventName: boolean;
  includeDownloadDate: boolean;
  applyOnDownload: boolean;
  textColor: string;
  fontName?: string;
  backgroundOpacity: number;
  applyOnPreview: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  validationErrors?: Record<string, string[]>;
}

// ── Dashboard Command Centre ─────────────────────────────────────────────────

export interface DashboardOverviewResponse {
  activeEvents: number;
  totalEvents: number;
  totalPhotos: number;
  downloadsToday: number;
  totalDownloads: number;
  totalSizeBytes: number;
  totalSizeHuman: string;
  pendingThumbnails: number;
  pendingFaceIndexes: number;
  totalFaceEmbeddings: number;
  eventsWithFaceSearch: number;
  eventsWithWatermark: number;
}

export interface EventSpotlightResponse {
  eventId: string;
  name: string;
  eventType: string;
  eventDate: string;
  clientName?: string;
  venueName?: string;
  photoCount: number;
  totalDownloads: number;
  storageBytes: number;
  storageHuman: string;
  faceRecognitionEnabled: boolean;
  watermarkEnabled: boolean;
  isActive: boolean;
  firstThumbnailUrl?: string;
}

export interface RecentActivityItem {
  activityType: string;
  eventId: string;
  eventName: string;
  photoId?: string;
  occurredAt: string;
  ipAddress?: string;
}

export interface StorageEventItem {
  eventId: string;
  eventName: string;
  sizeBytes: number;
  sizeHuman: string;
  photoCount: number;
}

export interface StorageAnalyticsResponse {
  totalSizeBytes: number;
  totalSizeHuman: string;
  eventCount: number;
  topEvents: StorageEventItem[];
}

export interface FaceAnalyticsEventItem {
  eventId: string;
  eventName: string;
  photoCount: number;
  faceEmbeddings: number;
}

export interface FaceAnalyticsResponse {
  totalIndexedFaces: number;
  totalPendingPhotos: number;
  eventsWithFaceSearch: number;
  eventBreakdown: FaceAnalyticsEventItem[];
}

export interface WatermarkAnalyticsResponse {
  eventsWithWatermark: number;
  totalEvents: number;
  totalDownloads: number;
  protectedDownloads: number;
  coveragePercentage: number;
  activeWatermarkEvents: number;
}

// ── Event Workspace ──────────────────────────────────────────────────────────

export interface EventWorkspaceResponse {
  id: string;
  name: string;
  description?: string;
  eventType: string;
  eventDate: string;
  venueName?: string;
  clientName?: string;
  watchFolder: string;
  thumbnailFolder: string;
  qrCodeUrl?: string;
  isActive: boolean;
  photoCount: number;
  totalSizeBytes: number;
  totalSize: string;
  totalDownloads: number;
  createdAt: string;
  galleryRecentCount?: number;
  allowGalleryBrowsing: boolean;
  allowFaceSearch: boolean;
  restrictDownloadsToMatchedPhotos: boolean;
  enableFaceRecognition: boolean;
  faceMatchThreshold: number;
  watermarkEnabled: boolean;
}

export interface DailyDownloadCount {
  date: string;
  count: number;
}

export interface RecentDownloadItem {
  photoId: string;
  ipAddress?: string;
  downloadedAt: string;
}

export interface EventAnalyticsResponse {
  eventId: string;
  eventName: string;
  totalPhotos: number;
  totalDownloads: number;
  todayDownloads: number;
  storageSizeBytes: number;
  storageHuman: string;
  downloadsLast30Days: DailyDownloadCount[];
  recentActivity: RecentDownloadItem[];
}

export interface FaceRecognitionMetricsResponse {
  eventId: string;
  enabled: boolean;
  matchThreshold: number;
  totalPhotos: number;
  indexedFaces: number;
  indexedPhotos: number;
  pendingPhotos: number;
  failedPhotos: number;
}

export interface StorageMetricsResponse {
  eventId: string;
  watchFolder: string;
  thumbnailFolder: string;
  sizeBytes: number;
  sizeHuman: string;
  photoCount: number;
  thumbnailCount: number;
}

export interface UpdateEventOverviewRequest {
  name: string;
  eventType: string;
  eventDate: string;
  description?: string;
  venueName?: string;
  clientName?: string;
}

export interface UpdateGallerySettingsRequest {
  allowGalleryBrowsing: boolean;
  allowFaceSearch: boolean;
  restrictDownloadsToMatchedPhotos: boolean;
  galleryRecentCount?: number;
}

export interface UpdateFaceRecognitionSettingsRequest {
  enableFaceRecognition: boolean;
  faceMatchThreshold: number;
  allowFaceSearch: boolean;
}

export interface HealthStatus {
  status: string;
  server: string;
  timestamp: string;
}
