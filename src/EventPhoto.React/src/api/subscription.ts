import { apiClient } from './client';

export interface Subscription {
  plan: 'Trial' | 'Starter' | 'Professional' | 'Enterprise';
  state: 'Trial' | 'Active' | 'GracePeriod' | 'Expired' | 'Cancelled';
  licenseKey: string | null;
  studioEmail: string | null;
  activatedAt: string | null;
  expiresAt: string | null;
  gracePeriodEndsAt: string | null;
  maxEvents: number;
  maxUsersPerStudio: number;
  isOperational: boolean;
  gracePeriodDaysRemaining: number;
  notes: string | null;
}

export interface ActivateSubscriptionRequest {
  licenseKey: string;
  studioEmail: string;
  plan: string;
  expiresAt: string;
}

export const subscriptionApi = {
  get: (): Promise<Subscription> =>
    apiClient.get('/subscription').then(r => r.data.data),

  activate: (req: ActivateSubscriptionRequest): Promise<Subscription> =>
    apiClient.post('/subscription/activate', req).then(r => r.data.data),
};
