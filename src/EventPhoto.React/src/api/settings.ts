import { apiClient } from './client';
import type { ApiResponse, SystemSetting } from '../types';

export const settingsApi = {
  async getAll() {
    const response = await apiClient.get<ApiResponse<SystemSetting[]>>('/settings');
    return response.data;
  },
  async update(key: string, value: string) {
    const response = await apiClient.put<ApiResponse<null>>(`/settings/${key}`, { value });
    return response.data;
  },
};
