import { apiClient } from './client';
import type {
  ApiResponse,
  DashboardOverviewResponse,
  EventSpotlightResponse,
  FaceAnalyticsResponse,
  RecentActivityItem,
  StorageAnalyticsResponse,
  WatermarkAnalyticsResponse,
} from '../types';

export const dashboardApi = {
  async getOverview() {
    const res = await apiClient.get<ApiResponse<DashboardOverviewResponse>>(
      '/statistics/dashboard/overview',
    );
    return res.data;
  },

  async getSpotlight() {
    const res = await apiClient.get<ApiResponse<EventSpotlightResponse | null>>(
      '/statistics/dashboard/spotlight',
    );
    return res.data;
  },

  async getRecentActivity(count = 20) {
    const res = await apiClient.get<ApiResponse<RecentActivityItem[]>>(
      `/statistics/activity/recent?count=${count}`,
    );
    return res.data;
  },

  async getStorageAnalytics() {
    const res = await apiClient.get<ApiResponse<StorageAnalyticsResponse>>(
      '/statistics/storage',
    );
    return res.data;
  },

  async getFaceAnalytics() {
    const res = await apiClient.get<ApiResponse<FaceAnalyticsResponse>>(
      '/statistics/face-recognition',
    );
    return res.data;
  },

  async getWatermarkAnalytics() {
    const res = await apiClient.get<ApiResponse<WatermarkAnalyticsResponse>>(
      '/statistics/watermark',
    );
    return res.data;
  },
};
