// ── AI Discovery Engine — TypeScript types ─────────────────────────────────────

export interface AiStudioOverviewResponse {
  totalPhotosIndexed: number;
  totalFacesIndexed: number;
  pendingJobs: number;
  processingJobs: number;
  failedJobs: number;
  deadLetteredJobs: number;
  queueDepth: number;
  averageSearchDurationMs: number;
  searchSuccessRatePercent: number;
  totalSearchesLast24H: number;
  isPipelineHealthy: boolean;
  pipelineStatusMessage: string;
  generatedAt: string;
}

export interface ProcessingQueueItemResponse {
  jobId: string;
  eventId: string;
  eventName: string;
  photoId: string;
  fileName: string;
  status: string;
  retryCount: number;
  createdAt: string;
  startedAt: string | null;
  nextRetryAt: string | null;
}

export interface ProcessingQueueResponse {
  items: ProcessingQueueItemResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  pendingCount: number;
  processingCount: number;
  failedCount: number;
}

export interface DeadLetterJobResponse {
  jobId: string;
  eventId: string;
  eventName: string;
  photoId: string;
  fileName: string;
  thumbnailUrl: string;
  status: string;
  failureType: string | null;
  lastError: string | null;
  retryCount: number;
  createdAt: string;
  completedAt: string | null;
}

export interface DeadLetterQueueResponse {
  items: DeadLetterJobResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface EventAiHealthResponse {
  eventId: string;
  eventName: string;
  totalPhotos: number;
  facesIndexed: number;
  pendingJobs: number;
  failedJobs: number;
  deadLetteredJobs: number;
  indexCompletionPercent: number;
  averageSearchDurationMs: number;
  searchSuccessRatePercent: number;
  totalSearches: number;
  isIndexComplete: boolean;
}

export interface TopEventAnalyticsItem {
  eventId: string;
  eventName: string;
  searchCount: number;
  successRatePercent: number;
}

export interface AiAnalyticsResponse {
  totalSearches: number;
  successfulSearches: number;
  successRatePercent: number;
  averageSearchDurationMs: number;
  averageMatchesFound: number;
  topEvents: TopEventAnalyticsItem[];
  hourlyVolume: HourlySearchVolumeItem[];
}

export interface HourlySearchVolumeItem {
  hour: string;
  searchCount: number;
  successCount: number;
}

// ── Face Search Match types ────────────────────────────────────────────────────

export type ConfidenceCategory = 'Excellent' | 'Strong' | 'Possible';

export interface FaceSearchMatchResponse {
  photoId: string;
  thumbnailUrl: string;
  downloadUrl: string;
  similarityScore: number;
  capturedAt: string;
  fileName: string;
  confidenceLabel: string;
  confidenceCategory: ConfidenceCategory;
}
