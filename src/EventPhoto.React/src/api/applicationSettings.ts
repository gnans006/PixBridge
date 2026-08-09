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
  // Feature flags
  isWatermarkEnabled: boolean;
  isFaceSearchEnabled: boolean;
  // Phase 6 — Studio Profile
  phone?: string;
  email?: string;
  website?: string;
  address?: string;
  instagram?: string;
  facebook?: string;
  whatsApp?: string;
  logoPath?: string;
  // Phase 7 — Branding
  primaryColor: string;
  secondaryColor: string;
  brandTheme: string;
  defaultWatermarkProfileId?: string;
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
  isWatermarkEnabled: boolean;
  isFaceSearchEnabled: boolean;
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

export interface UpdateStudioProfileRequest {
  phone?: string;
  email?: string;
  website?: string;
  address?: string;
  instagram?: string;
  facebook?: string;
  whatsApp?: string;
  logoPath?: string;
}

export interface UpdateBrandingRequest {
  primaryColor: string;
  secondaryColor: string;
  brandTheme: string;
  defaultWatermarkProfileId?: string | null;
}

export const applicationSettingsApi = {
  async get(): Promise<ApplicationSettings> {
    const res = await apiClient.get<ApiResponse<ApplicationSettings>>('/settings/application');
    return res.data.data!;
  },

  async update(data: UpdateApplicationSettingsRequest): Promise<void> {
    await apiClient.put('/settings/application', data);
  },

  async updateStudioProfile(data: UpdateStudioProfileRequest): Promise<void> {
    await apiClient.put('/settings/studio-profile', data);
  },

  async updateBranding(data: UpdateBrandingRequest): Promise<void> {
    await apiClient.put('/settings/branding', data);
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
