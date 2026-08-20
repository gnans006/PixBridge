import { apiClient } from './client';
import type {
  AiStudioOverviewResponse,
  ProcessingQueueResponse,
  DeadLetterQueueResponse,
  EventAiHealthResponse,
  AiAnalyticsResponse,
} from '../types/aiDiscovery';

export const aiStudioApi = {
  getOverview: (): Promise<AiStudioOverviewResponse> =>
    apiClient.get('/ai-studio/overview').then(r => r.data.data),

  getProcessingQueue: (page = 1, pageSize = 25): Promise<ProcessingQueueResponse> =>
    apiClient.get('/ai-studio/queue', { params: { page, pageSize } }).then(r => r.data.data),

  getDeadLetterQueue: (page = 1, pageSize = 25): Promise<DeadLetterQueueResponse> =>
    apiClient.get('/ai-studio/dead-letter', { params: { page, pageSize } }).then(r => r.data.data),

  retryDeadLetterJob: (jobId: string): Promise<void> =>
    apiClient.post(`/ai-studio/dead-letter/${jobId}/retry`).then(() => undefined),

  ignoreDeadLetterJob: (jobId: string): Promise<void> =>
    apiClient.post(`/ai-studio/dead-letter/${jobId}/ignore`).then(() => undefined),

  getEventHealth: (page = 1, pageSize = 20): Promise<EventAiHealthResponse[]> =>
    apiClient.get('/ai-studio/event-health', { params: { page, pageSize } }).then(r => r.data.data ?? []),

  getAnalytics: (windowHours = 24): Promise<AiAnalyticsResponse> =>
    apiClient.get('/ai-studio/analytics', { params: { windowHours } }).then(r => r.data.data),
};
