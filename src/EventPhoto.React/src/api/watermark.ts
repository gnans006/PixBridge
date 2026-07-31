import { apiClient } from './client';
import type {
  ApiResponse,
  UpsertWatermarkConfigRequest,
  WatermarkConfigResponse,
} from '../types';

export const watermarkApi = {
  async getConfig(eventId: string) {
    const response = await apiClient.get<ApiResponse<WatermarkConfigResponse>>(
      `/events/${eventId}/watermark-config`,
    );
    return response.data;
  },

  async upsertConfig(eventId: string, data: UpsertWatermarkConfigRequest) {
    const response = await apiClient.put<ApiResponse<WatermarkConfigResponse>>(
      `/events/${eventId}/watermark-config`,
      data,
    );
    return response.data;
  },
};
