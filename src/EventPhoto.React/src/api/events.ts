import { apiClient, buildApiUrl } from './client';
import type { ApiResponse, CreateEventRequest, EventResponse } from '../types';

export const eventsApi = {
  async getAll() {
    const response = await apiClient.get<ApiResponse<EventResponse[]>>('/events');
    return response.data;
  },
  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<EventResponse>>(`/events/${id}`);
    return response.data;
  },
  async create(data: CreateEventRequest) {
    const response = await apiClient.post<ApiResponse<EventResponse>>('/events', data);
    return response.data;
  },
  async update(id: string, data: Partial<CreateEventRequest>) {
    const response = await apiClient.put<ApiResponse<EventResponse>>(`/events/${id}`, data);
    return response.data;
  },
  async delete(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/events/${id}`);
    return response.data;
  },
  async toggleActive(id: string, activate: boolean) {
    const response = await apiClient.patch<ApiResponse<EventResponse>>(`/events/${id}/active`, null, {
      params: { activate },
    });
    return response.data;
  },
  getQrCodeUrl(id: string, bust?: number) {
    const url = buildApiUrl(`/events/${id}/qrcode`);
    return bust ? `${url}?t=${bust}` : url;
  },
  async refreshQr(id: string) {
    const response = await apiClient.post<void>(`/events/${id}/qrcode/refresh`);
    return response.data;
  },
};
