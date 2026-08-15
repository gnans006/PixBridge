import { apiClient } from './client';
import type { ApiResponse } from '../types';

// ── Deployment status ──────────────────────────────────────────────────────────

export type DeploymentMode = 'Localhost' | 'Lan' | 'Router' | 'Domain';

export interface DeploymentStatus {
  mode: DeploymentMode;
  publicBaseUrl: string;
  isHttps: boolean;
  hasExplicitPort: boolean;
  isReverseProxyDetected: boolean;
  detectedProxy: string | null;
  modeLabel: string;
  modeDescription: string;
  isInternetAccessible: boolean;
  httpsWarning: boolean;
}

export interface DeploymentStatusResult {
  deployment: DeploymentStatus;
  lanIpAddress: string;
  allLanIpAddresses: string[];
  serverPort: number;
  totalEvents: number;
  eventsWithQr: number;
  eventsWithMissingQr: number;
  checkedAt: string;
}

// ── Service health ─────────────────────────────────────────────────────────────

export type HealthStatus = 'Healthy' | 'Degraded' | 'Offline';

export interface ComponentHealth {
  name: string;
  status: HealthStatus;
  responseMs: number | null;
  detail: string | null;
}

export interface ServiceHealthResult {
  database: ComponentHealth;
  aiService: ComponentHealth;
  storage: ComponentHealth;
  qrService: ComponentHealth;
  checkedAt: string;
}

// ── API ────────────────────────────────────────────────────────────────────────

export const deploymentApi = {
  getStatus: () =>
    apiClient.get<ApiResponse<DeploymentStatusResult>>('/deployment/status'),

  getServiceHealth: () =>
    apiClient.get<ApiResponse<ServiceHealthResult>>('/deployment/services'),

  regenerateAllQr: () =>
    apiClient.post<ApiResponse<number>>('/deployment/regenerate-qr'),
};
