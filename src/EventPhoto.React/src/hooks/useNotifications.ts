import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import type { HealthStatus } from '../types';

export interface AppNotification {
  id: string;
  type: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
}

const STORAGE_KEY = 'pds-dismissed-notifs';

function loadDismissed(): Set<string> {
  try {
    return new Set(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]') as string[]);
  } catch {
    return new Set();
  }
}

function saveDismissed(set: Set<string>) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify([...set]));
}

export function useNotifications() {
  const [dismissed, setDismissed] = useState<Set<string>>(loadDismissed);

  const { isError: apiOffline } = useQuery({
    queryKey: ['health-notif'],
    queryFn: async () => {
      const res = await apiClient.get<HealthStatus>('/health');
      return res.data;
    },
    refetchInterval: 30_000,
    retry: 1,
  });

  const all: AppNotification[] = [
    ...(apiOffline
      ? [{ id: 'api-offline', type: 'error' as const, title: 'Server Unreachable', message: 'The PixBridge API is not responding. Check that the server is running.' }]
      : []),
    { id: 'tip-profile', type: 'info' as const, title: 'Complete Your Studio Profile', message: 'Add contact details and social links under Studio → Studio Profile.' },
    { id: 'tip-branding', type: 'info' as const, title: 'Customize Your Brand Colors', message: 'Set your primary and accent colors under Studio → Branding.' },
  ];

  const notifications = all.filter(n => !dismissed.has(n.id));

  const dismiss = (id: string) => {
    setDismissed(prev => {
      const next = new Set(prev);
      next.add(id);
      saveDismissed(next);
      return next;
    });
  };

  const dismissAll = () => {
    const next = new Set(all.map(n => n.id));
    saveDismissed(next);
    setDismissed(next);
  };

  return { notifications, dismiss, dismissAll, count: notifications.length };
}
