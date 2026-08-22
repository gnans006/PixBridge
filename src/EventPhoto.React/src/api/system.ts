import { apiClient } from './client';

export interface NetworkInfo {
  hostname: string;
  primaryIp: string;
  allIpAddresses: string[];
  port: number;
  accessibleUrl: string;
  publicBaseUrl: string;
  qrBaseUrl: string;
  serverTime: string;
}

export interface PathValidationResult {
  isValid: boolean;
  exists: boolean;
  willBeCreated: boolean;
  driveType: string | null;
  driveLabel: string | null;
  warning: string | null;
  error: string | null;
}

export interface DriveInfo {
  letter: string;
  label: string;
  type: string;       // "Fixed" | "Removable" | "Network" | "Ram" | "Unknown"
  totalBytes: number;
  freeBytes: number;
  freeFormatted: string;
}

export interface CacheEventStats {
  eventId: string;
  sizeBytes: number;
  sizeFormatted: string;
  fileCount: number;
}

export interface CacheStats {
  totalSizeBytes: number;
  totalFileCount: number;
  maxSizeBytes: number;
  totalSizeFormatted: string;
  maxSizeFormatted: string;
  cacheDirectory: string;
  events: CacheEventStats[];
}

export const systemApi = {
  async getNetworkInfo() {
    const response = await apiClient.get<{ data: NetworkInfo }>('/system/network');
    return response.data.data;
  },

  async validatePath(path: string, excludeEventId?: string): Promise<PathValidationResult> {
    const response = await apiClient.post<{ data: PathValidationResult }>('/system/validate-path', {
      path,
      excludeEventId: excludeEventId ?? null,
    });
    return response.data.data;
  },

  async getDrives(): Promise<DriveInfo[]> {
    const response = await apiClient.get<{ data: DriveInfo[] }>('/system/drives');
    return response.data.data;
  },

  async getCacheStats(): Promise<CacheStats> {
    const response = await apiClient.get<{ data: CacheStats }>('/system/cache/stats');
    return response.data.data;
  },

  async clearEventCache(eventId: string): Promise<void> {
    await apiClient.delete(`/system/cache/event/${eventId}`);
  },

  async clearAllCache(): Promise<void> {
    await apiClient.delete('/system/cache');
  },
};

