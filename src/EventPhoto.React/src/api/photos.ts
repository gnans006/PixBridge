import { apiClient, buildApiUrl } from './client';
import type { ApiResponse, PagedResult, PhotoResponse } from '../types';

export const photosApi = {
  async getByEvent(eventId: string, page = 1, pageSize = 50) {
    const response = await apiClient.get<ApiResponse<PagedResult<PhotoResponse>>>(`/photos/event/${eventId}`, {
      params: { page, pageSize },
    });
    return response.data;
  },
  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<PhotoResponse>>(`/photos/${id}`);
    return response.data;
  },
  getThumbnailUrl(id: string) {
    return buildApiUrl(`/photos/${id}/thumbnail`);
  },
  getDownloadUrl(id: string) {
    return buildApiUrl(`/photos/${id}/download`);
  },
  async delete(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/photos/${id}`);
    return response.data;
  },
};
