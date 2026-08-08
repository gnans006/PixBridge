import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { applicationSettingsApi, type UpdateApplicationSettingsRequest } from '../api/applicationSettings';
import { apiError } from '../utils/errorHandler';

export const APP_SETTINGS_QUERY_KEY = ['app-settings'] as const;
export const NETWORK_INFO_QUERY_KEY = ['network-info'] as const;

export function useApplicationSettings() {
  return useQuery({
    queryKey: APP_SETTINGS_QUERY_KEY,
    queryFn: () => applicationSettingsApi.get(),
    staleTime: 60_000,
  });
}

export function useNetworkInformation(port = 5000) {
  return useQuery({
    queryKey: [...NETWORK_INFO_QUERY_KEY, port],
    queryFn: () => applicationSettingsApi.getNetworkInfo(port),
    refetchInterval: 30_000,
    retry: 1,
  });
}

export function useUpdateApplicationSettings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateApplicationSettingsRequest) =>
      applicationSettingsApi.update(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: APP_SETTINGS_QUERY_KEY });
      toast.success('Settings saved successfully.');
    },
    onError: (err) => apiError(err, 'Failed to save settings.'),
  });
}

export function useTestPublicUrl() {
  return useMutation({
    mutationFn: (url: string) => applicationSettingsApi.testPublicUrl(url),
  });
}
