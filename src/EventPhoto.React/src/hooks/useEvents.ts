import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';
import toast from 'react-hot-toast';
import { eventsApi } from '../api/events';
import type { EventResponse } from '../types';
import { apiError } from '../utils/errorHandler';

export const EVENTS_QUERY_KEY = ['events'] as const;

export function useEvents() {
  return useQuery({
    queryKey: EVENTS_QUERY_KEY,
    queryFn: async () => {
      const res = await eventsApi.getAll();
      return res.data ?? [];
    },
    refetchInterval: 15_000,
  });
}

export function useEventMutations() {
  const queryClient = useQueryClient();

  function invalidateAll() {
    void queryClient.invalidateQueries({ queryKey: EVENTS_QUERY_KEY });
    void queryClient.invalidateQueries({ queryKey: ['dashboard-overview'] });
    void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
    void queryClient.invalidateQueries({ queryKey: ['spotlight'] });
  }

  const deleteMutation = useMutation({
    mutationFn: eventsApi.delete,
    onSuccess: () => { invalidateAll(); toast.success('Event deleted.'); },
    onError: (error) => apiError(error, 'Failed to delete event.'),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      eventsApi.toggleActive(id, activate),
    onSuccess: () => { invalidateAll(); toast.success('Event updated.'); },
    onError: (error) => apiError(error, 'Failed to update event.'),
  });

  const refreshQrMutation = useMutation({
    mutationFn: (id: string) => eventsApi.refreshQr(id),
    onSuccess: () => { invalidateAll(); toast.success('QR code refreshed.'); },
    onError: (error) => apiError(error, 'Failed to refresh QR code.'),
  });

  return { deleteMutation, toggleMutation, refreshQrMutation };
}

export function useFilteredEvents(
  events: EventResponse[] | undefined,
  search: string,
  filter: string,
) {
  return useMemo(() => {
    const all = events ?? [];
    const q = search.trim().toLowerCase();

    let result = q
      ? all.filter(
          (e) =>
            e.name.toLowerCase().includes(q) ||
            (e.clientName ?? '').toLowerCase().includes(q) ||
            (e.venueName ?? '').toLowerCase().includes(q) ||
            e.eventType.toLowerCase().includes(q),
        )
      : all;

    switch (filter) {
      case 'active':
        result = result.filter((e) => e.isActive);
        break;
      case 'inactive':
        result = result.filter((e) => !e.isActive);
        break;
      case 'face-search':
        result = result.filter((e) => e.enableFaceRecognition);
        break;
      case 'gallery':
        result = result.filter((e) => e.allowGalleryBrowsing);
        break;
      default:
        if (filter && filter !== 'all') {
          result = result.filter((e) => e.eventType === filter);
        }
    }

    return result;
  }, [events, search, filter]);
}

export function useEventTypes(events: EventResponse[] | undefined): string[] {
  return useMemo(() => {
    const types = new Set((events ?? []).map((e) => e.eventType));
    return Array.from(types).sort();
  }, [events]);
}
