import { apiClient } from './client';

export interface GuestUploadSession {
  id: string;
  eventId: string;
  sessionCode: string;
  title: string | null;
  photoCount: number;
  status: 'Active' | 'Closed';
  createdAt: string;
  closedAt: string | null;
}

export interface GuestUploadItem {
  id: string;
  eventId: string;
  sessionId: string;
  originalFileName: string;
  storedPath: string;
  thumbnailPath: string | null;
  fileSizeBytes: number;
  contentType: string;
  uploadedAt: string;
  moderationStatus: 'Pending' | 'Approved' | 'Rejected';
  rejectionReason: string | null;
}

export const guestUploadApi = {
  getSessions: (eventId: string): Promise<GuestUploadSession[]> =>
    apiClient.get(`/events/${eventId}/guest-uploads/sessions`).then(r => r.data.data),

  createSession: (eventId: string, title?: string): Promise<GuestUploadSession> =>
    apiClient.post(`/events/${eventId}/guest-uploads/sessions`, { title }).then(r => r.data.data),

  closeSession: (eventId: string, sessionId: string): Promise<void> =>
    apiClient.post(`/events/${eventId}/guest-uploads/sessions/${sessionId}/close`),

  getUploads: (eventId: string, status?: string): Promise<GuestUploadItem[]> => {
    const params = status ? `?status=${status}` : '';
    return apiClient.get(`/events/${eventId}/guest-uploads${params}`).then(r => r.data.data);
  },

  moderate: (eventId: string, uploadId: string, approve: boolean, rejectionReason?: string): Promise<void> =>
    apiClient.patch(`/events/${eventId}/guest-uploads/${uploadId}`, { approve, rejectionReason }),

  /** Public — no auth required. Uploads a file via a session code. */
  submitPhoto: (eventId: string, sessionCode: string, file: File): Promise<GuestUploadItem> => {
    const fd = new FormData();
    fd.append('file', file);
    return apiClient.post(`/events/${eventId}/guest-uploads/submit/${sessionCode}`, fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data.data);
  },
};
