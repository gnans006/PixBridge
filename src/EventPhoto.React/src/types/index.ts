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
  thumbnailsPending: number;
  thumbnailsFailed: number;
  lastPhotoAt?: string;
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
