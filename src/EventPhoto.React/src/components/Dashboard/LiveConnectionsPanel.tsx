import { useQuery } from '@tanstack/react-query';
import { Wifi, Activity, Calendar, Download } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

export function LiveConnectionsPanel() {
  const { data, dataUpdatedAt } = useQuery({
    queryKey: ['dashboard-overview'],
    queryFn: () => dashboardApi.getOverview(),
    refetchInterval: 15_000,
    select: (r) => r.data,
  });

  const lastRefresh = dataUpdatedAt
    ? new Date(dataUpdatedAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
    : '—';

  const stats = [
    { icon: Calendar, label: 'Active Events', value: data?.activeEvents ?? 0, color: 'text-emerald-400' },
    { icon: Download, label: 'Downloads Today', value: data?.downloadsToday ?? 0, color: 'text-indigo-400' },
    { icon: Activity, label: 'Pending Processing', value: (data?.pendingThumbnails ?? 0) + (data?.pendingFaceIndexes ?? 0), color: 'text-amber-400' },
  ];

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-500/20">
            <Wifi className="h-4 w-4 text-emerald-400" />
          </div>
          <h3 className="text-sm font-semibold text-slate-100">Live Status</h3>
        </div>
        <span className="flex items-center gap-1 text-xs text-emerald-400">
          <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
          Live
        </span>
      </div>

      <div className="space-y-3">
        {stats.map(({ icon: Icon, label, value, color }) => (
          <div key={label} className="flex items-center justify-between rounded-xl bg-slate-800/50 px-3 py-2.5">
            <div className="flex items-center gap-2">
              <Icon className={`h-4 w-4 ${color}`} />
              <span className="text-xs text-slate-300">{label}</span>
            </div>
            <span className={`text-sm font-bold ${color}`}>{value.toLocaleString()}</span>
          </div>
        ))}
      </div>

      <p className="mt-3 text-center text-xs text-slate-600">
        Last updated: {lastRefresh}
      </p>
    </div>
  );
}
