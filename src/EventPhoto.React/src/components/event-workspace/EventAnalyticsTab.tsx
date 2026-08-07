import { useQuery } from '@tanstack/react-query';
import { workspaceApi } from '../../api/workspace';
import type { EventWorkspaceResponse } from '../../types';
import { Activity, BarChart3, Camera, Clock, Download, HardDrive, Loader2 } from 'lucide-react';

interface EventAnalyticsTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventAnalyticsTab({ workspace }: EventAnalyticsTabProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['analytics', workspace.id],
    queryFn: () => workspaceApi.getAnalytics(workspace.id),
    refetchInterval: 30_000,
  });

  const analytics = data?.data;

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
      </div>
    );
  }

  if (isError || !analytics) {
    return (
      <div className="flex h-40 items-center justify-center rounded-2xl border border-slate-800 bg-slate-900">
        <p className="text-sm text-slate-500">Failed to load analytics.</p>
      </div>
    );
  }

  const maxDay = Math.max(...analytics.downloadsLast30Days.map((d) => d.count), 1);

  return (
    <div className="space-y-6">
      {/* KPIs */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <KpiCard icon={<Camera className="h-5 w-5" />} label="Total Photos" value={analytics.totalPhotos.toLocaleString()} color="indigo" />
        <KpiCard icon={<Download className="h-5 w-5" />} label="Total Downloads" value={analytics.totalDownloads.toLocaleString()} color="violet" />
        <KpiCard icon={<Activity className="h-5 w-5" />} label="Downloads Today" value={analytics.todayDownloads.toLocaleString()} color="emerald" />
        <KpiCard icon={<HardDrive className="h-5 w-5" />} label="Storage" value={analytics.storageHuman} color="amber" />
      </div>

      {/* Downloads chart */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-4 flex items-center gap-2">
          <BarChart3 className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Downloads — Last 30 Days</h2>
        </div>

        <div className="flex items-end gap-px" style={{ height: '100px' }}>
          {analytics.downloadsLast30Days.map((day) => {
            const height = maxDay > 0 ? Math.max(2, (day.count / maxDay) * 100) : 2;
            return (
              <div
                key={day.date}
                className="group relative flex-1"
                style={{ height: '100px' }}
                title={`${day.date}: ${day.count} downloads`}
              >
                <div
                  className="absolute bottom-0 w-full rounded-sm bg-indigo-500/50 transition-colors group-hover:bg-indigo-400"
                  style={{ height: `${height}%` }}
                />
              </div>
            );
          })}
        </div>

        <div className="mt-2 flex justify-between text-xs text-slate-500">
          <span>{analytics.downloadsLast30Days[0]?.date?.slice(5)}</span>
          <span>{analytics.downloadsLast30Days[analytics.downloadsLast30Days.length - 1]?.date?.slice(5)}</span>
        </div>
      </div>

      {/* Recent activity */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-4 flex items-center gap-2">
          <Clock className="h-4 w-4 text-slate-400" />
          <h2 className="text-sm font-semibold text-white">Recent Downloads</h2>
        </div>

        {analytics.recentActivity.length === 0 ? (
          <p className="py-8 text-center text-sm text-slate-500">No downloads yet.</p>
        ) : (
          <div className="overflow-hidden rounded-xl border border-slate-700/50">
            {analytics.recentActivity.map((item, i) => (
              <div
                key={i}
                className="flex items-center justify-between border-b border-slate-700/30 px-4 py-2.5 last:border-b-0 hover:bg-slate-800/50"
              >
                <div className="flex items-center gap-3">
                  <Download className="h-3.5 w-3.5 text-slate-500" />
                  <span className="font-mono text-xs text-slate-400">{item.photoId.slice(0, 8)}…</span>
                </div>
                <div className="flex items-center gap-4 text-xs text-slate-500">
                  {item.ipAddress && <span className="hidden sm:block">{item.ipAddress}</span>}
                  <span>{new Date(item.downloadedAt).toLocaleTimeString()}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function KpiCard({
  icon,
  label,
  value,
  color,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  color: 'indigo' | 'violet' | 'emerald' | 'amber';
}) {
  const colorMap = {
    indigo: 'text-indigo-400 bg-indigo-500/10',
    violet: 'text-violet-400 bg-violet-500/10',
    emerald: 'text-emerald-400 bg-emerald-500/10',
    amber: 'text-amber-400 bg-amber-500/10',
  };
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className={`mb-3 inline-flex rounded-xl p-2.5 ${colorMap[color]}`}>
        <span className={colorMap[color].split(' ')[0]}>{icon}</span>
      </div>
      <p className="text-xl font-bold text-white">{value}</p>
      <p className="mt-0.5 text-xs text-slate-400">{label}</p>
    </div>
  );
}
