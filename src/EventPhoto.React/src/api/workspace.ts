import { apiClient, buildApiUrl } from './client';
import type {
  ApiResponse,
  EventAnalyticsResponse,
  EventWorkspaceResponse,
  FaceRecognitionMetricsResponse,
  StorageMetricsResponse,
  UpdateEventOverviewRequest,
  UpdateFaceRecognitionSettingsRequest,
  UpdateGallerySettingsRequest,
} from '../types';
import type { EventResponse } from '../types';

export const workspaceApi = {
  /** GET /api/events/:id/workspace — consolidated workspace data */
  async getWorkspace(eventId: string) {
    const r = await apiClient.get<ApiResponse<EventWorkspaceResponse>>(
      `/events/${eventId}/workspace`,
    );
    return r.data;
  },

  /** GET /api/events/:id/workspace/analytics */
  async getAnalytics(eventId: string) {
    const r = await apiClient.get<ApiResponse<EventAnalyticsResponse>>(
      `/events/${eventId}/workspace/analytics`,
    );
    return r.data;
  },

  /** GET /api/events/:id/workspace/face-recognition/metrics */
  async getFaceMetrics(eventId: string) {
    const r = await apiClient.get<ApiResponse<FaceRecognitionMetricsResponse>>(
      `/events/${eventId}/workspace/face-recognition/metrics`,
    );
    return r.data;
  },

  /** GET /api/events/:id/workspace/storage */
  async getStorage(eventId: string) {
    const r = await apiClient.get<ApiResponse<StorageMetricsResponse>>(
      `/events/${eventId}/workspace/storage`,
    );
    return r.data;
  },

  /** PUT /api/events/:id/workspace/overview */
  async updateOverview(eventId: string, data: UpdateEventOverviewRequest) {
    const r = await apiClient.put<ApiResponse<EventResponse>>(
      `/events/${eventId}/workspace/overview`,
      data,
    );
    return r.data;
  },

  /** PUT /api/events/:id/workspace/gallery-settings */
  async updateGallerySettings(eventId: string, data: UpdateGallerySettingsRequest) {
    const r = await apiClient.put<ApiResponse<EventResponse>>(
      `/events/${eventId}/workspace/gallery-settings`,
      data,
    );
    return r.data;
  },

  /** PUT /api/events/:id/workspace/face-recognition */
  async updateFaceRecognition(eventId: string, data: UpdateFaceRecognitionSettingsRequest) {
    const r = await apiClient.put<ApiResponse<EventResponse>>(
      `/events/${eventId}/workspace/face-recognition`,
      data,
    );
    return r.data;
  },

  /** POST /api/events/:id/workspace/face-recognition/rebuild */
  async rebuildFaceIndex(eventId: string) {
    const r = await apiClient.post<ApiResponse<number>>(
      `/events/${eventId}/workspace/face-recognition/rebuild`,
    );
    return r.data;
  },

  /** Returns the public gallery URL for an event */
  getGalleryUrl(eventId: string) {
    return buildApiUrl(`/gallery/${eventId}`).replace('/api', '');
  },

  /** Returns the QR code image URL */
  getQrImageUrl(eventId: string, bust?: number) {
    const base = buildApiUrl(`/events/${eventId}/qrcode`);
    return bust ? `${base}?t=${bust}` : base;
  },
};
