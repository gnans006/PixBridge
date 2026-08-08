import { apiClient } from './client';
import type { ApiResponse } from '../types';

export interface ApplicationSettings {
  id: string;
  studioName: string;
  serverName: string;
  publicBaseUrl: string;
  serverPort: number;
  defaultEventGalleryMode: 'GalleryOnly' | 'FaceSearchOnly' | 'Hybrid';
  enableWatermarkByDefault: boolean;
  enableFaceRecognitionByDefault: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateApplicationSettingsRequest {
  studioName: string;
  serverName: string;
  publicBaseUrl: string;
  serverPort: number;
  defaultEventGalleryMode: string;
  enableWatermarkByDefault: boolean;
  enableFaceRecognitionByDefault: boolean;
}

export interface NetworkInformation {
  hostName: string;
  machineName: string;
  primaryIpAddress: string;
  port: number;
  allIpAddresses: string[];
  accessibleLanUrl: string;
  isLanReachable: boolean;
}

export interface TestPublicUrlResult {
  isReachable: boolean;
  statusCode: number | null;
  responseTimeMs: number | null;
  errorMessage: string | null;
}

export const applicationSettingsApi = {
  async get(): Promise<ApplicationSettings> {
    const res = await apiClient.get<ApiResponse<ApplicationSettings>>('/settings/application');
    return res.data.data!;
  },

  async update(data: UpdateApplicationSettingsRequest): Promise<void> {
    await apiClient.put('/settings/application', data);
  },

  async getNetworkInfo(port = 5000): Promise<NetworkInformation> {
    const res = await apiClient.get<ApiResponse<NetworkInformation>>(
      `/settings/network-info?port=${port}`,
    );
    return res.data.data!;
  },

  async testPublicUrl(url: string): Promise<TestPublicUrlResult> {
    const res = await apiClient.post<ApiResponse<TestPublicUrlResult>>(
      '/settings/test-public-url',
      { url },
    );
    return res.data.data!;
  },
};
