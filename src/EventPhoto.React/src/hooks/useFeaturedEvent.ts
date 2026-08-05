import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '../api/dashboard';

export function useFeaturedEvent() {
  return useQuery({
    queryKey: ['spotlight'],
    queryFn: async () => {
      const res = await dashboardApi.getSpotlight();
      return res.data ?? null;
    },
    refetchInterval: 30_000,
  });
}
