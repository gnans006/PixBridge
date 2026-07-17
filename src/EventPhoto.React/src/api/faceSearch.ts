import { apiClient as client } from './client';
import type { ApiResponse } from '../types';

export interface GuestGalleryConfig {
  eventId: string;
  eventName: string;
  allowGalleryBrowsing: boolean;
  allowFaceSearch: boolean;
  faceRecognitionEnabled: boolean;
  restrictDownloadsToMatchedPhotos: boolean;
  galleryMode: 'GalleryOnly' | 'FaceSearchOnly' | 'Hybrid';
}

export interface FaceSearchStatus {
  sessionToken: string;
  status: 'Created' | 'Searching' | 'Completed' | 'Expired';
  matchCount: number;
  expiresAt: string;
  errorMessage?: string;
}

export interface FaceSearchMatch {
  photoId: string;
  thumbnailUrl: string;
  downloadUrl: string;
  similarityScore: number;
  capturedAt: string;
  fileName: string;
}

export interface FaceSearchResults {
  sessionToken: string;
  totalMatches: number;
  matches: FaceSearchMatch[];
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export const faceSearchApi = {
  /** Returns gallery mode config for the event landing page. */
  getGalleryConfig: (eventId: string) =>
    client.get<ApiResponse<GuestGalleryConfig>>(`/face-search/events/${eventId}/config`),

  /** Uploads selfie and starts face-search session. */
  startSearch: (eventId: string, selfie: File, threshold?: number) => {
    const form = new FormData();
    form.append('selfie', selfie);
    const params = threshold !== undefined ? `?threshold=${threshold}` : '';
    return client.post<ApiResponse<FaceSearchStatus>>(
      `/face-search/events/${eventId}/search${params}`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
  },

  /** Polls session status (fallback when SignalR is unavailable). */
  getStatus: (sessionToken: string) =>
    client.get<ApiResponse<FaceSearchStatus>>(`/face-search/${sessionToken}/status`),

  /** Returns paged matched photos for a completed session. */
  getResults: (sessionToken: string, page = 1, pageSize = 50) =>
    client.get<ApiResponse<FaceSearchResults>>(`/face-search/${sessionToken}/results`, {
      params: { page, pageSize },
    }),
};
