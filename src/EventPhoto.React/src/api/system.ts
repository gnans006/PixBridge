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

export const systemApi = {
  async getNetworkInfo() {
    const response = await apiClient.get<{ data: NetworkInfo }>('/system/network');
    return response.data.data;
  },
};
