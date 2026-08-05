import { useQuery } from '@tanstack/react-query';
import { Shield } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

function Ring({ percentage }: { percentage: number }) {
  const r = 28;
  const circ = 2 * Math.PI * r;
  const dash = (percentage / 100) * circ;
  return (
    <svg viewBox="0 0 72 72" className="h-20 w-20 -rotate-90">
      <circle cx="36" cy="36" r={r} strokeWidth="6" className="stroke-slate-800 fill-none" />
      <circle
        cx="36" cy="36" r={r}
        strokeWidth="6"
        className="fill-none stroke-violet-500 transition-all duration-700"
        strokeDasharray={`${dash} ${circ}`}
        strokeLinecap="round"
      />
    </svg>
  );
}

export function WatermarkAnalytics() {
  const { data, isLoading } = useQuery({
    queryKey: ['watermark-analytics'],
    queryFn: () => dashboardApi.getWatermarkAnalytics(),
    refetchInterval: 60_000,
    select: (r) => r.data,
  });

  const coverage = data?.coveragePercentage ?? 0;

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-4 flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-violet-500/20">
          <Shield className="h-4 w-4 text-violet-400" />
        </div>
        <h3 className="text-sm font-semibold text-slate-100">Watermark Analytics</h3>
      </div>

      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3].map((i) => <div key={i} className="h-10 rounded-lg bg-slate-800" />)}
        </div>
      ) : (
        <div className="flex items-center gap-5">
          <div className="relative flex-shrink-0">
            <Ring percentage={coverage} />
            <div className="absolute inset-0 flex flex-col items-center justify-center">
              <span className="text-sm font-bold text-violet-400">{coverage.toFixed(0)}%</span>
              <span className="text-[10px] text-slate-500">coverage</span>
            </div>
          </div>

          <div className="flex-1 space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-slate-400">Protected Events</span>
              <span className="font-semibold text-slate-100">{data?.eventsWithWatermark ?? 0} / {data?.totalEvents ?? 0}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Active & Protected</span>
              <span className="font-semibold text-emerald-400">{data?.activeWatermarkEvents ?? 0}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Protected Downloads</span>
              <span className="font-semibold text-violet-400">{(data?.protectedDownloads ?? 0).toLocaleString()}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
