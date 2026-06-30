import { apiClient } from './client';
import type { ApiResponse, DashboardStatsResponse, EventStatisticsResponse } from '../types';

export const statisticsApi = {
  async getDashboard() {
    const response = await apiClient.get<ApiResponse<DashboardStatsResponse>>('/statistics/dashboard');
    return response.data;
  },
  async getEventStats(eventId: string) {
    const response = await apiClient.get<ApiResponse<EventStatisticsResponse>>(`/statistics/events/${eventId}`);
    return response.data;
  },
};
