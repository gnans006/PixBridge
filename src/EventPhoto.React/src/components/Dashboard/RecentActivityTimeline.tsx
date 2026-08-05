import { useQuery } from '@tanstack/react-query';
import { Download } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const s = Math.floor(diff / 1000);
  if (s < 60) return `${s}s ago`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

export function RecentActivityTimeline() {
  const { data, isLoading } = useQuery({
    queryKey: ['recent-activity'],
    queryFn: () => dashboardApi.getRecentActivity(20),
    refetchInterval: 15_000,
    select: (r) => r.data ?? [],
  });

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-4 flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-500/20">
          <Download className="h-4 w-4 text-indigo-400" />
        </div>
        <h3 className="text-sm font-semibold text-slate-100">Recent Activity</h3>
        {data?.length ? (
          <span className="ml-auto text-xs text-slate-500">{data.length} recent</span>
        ) : null}
      </div>

      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="flex items-center gap-3">
              <div className="h-7 w-7 flex-shrink-0 rounded-full bg-slate-800" />
              <div className="flex-1 space-y-1.5">
                <div className="h-2.5 w-3/4 rounded bg-slate-800" />
                <div className="h-2 w-1/2 rounded bg-slate-800" />
              </div>
            </div>
          ))}
        </div>
      ) : !data?.length ? (
        <div className="flex flex-col items-center gap-2 py-8">
          <Download className="h-8 w-8 text-slate-700" />
          <p className="text-xs text-slate-500">No downloads recorded yet</p>
        </div>
      ) : (
        <div className="relative space-y-0">
          {/* Timeline line */}
          <div className="absolute left-3.5 top-2 h-[calc(100%-16px)] w-px bg-slate-800" />

          {data.map((item, i) => (
            <div
              key={`${item.eventId}-${item.occurredAt}-${i}`}
              className="relative flex items-start gap-3 py-2 animate-fade-in"
              style={{ animationDelay: `${i * 30}ms` }}
            >
              {/* Dot */}
              <div className="relative flex-shrink-0 mt-0.5">
                <div className="h-7 w-7 rounded-full bg-indigo-500/20 border border-indigo-500/40 flex items-center justify-center z-10 relative">
                  <Download className="h-3 w-3 text-indigo-400" />
                </div>
              </div>

              <div className="flex-1 min-w-0 pt-0.5">
                <p className="text-xs font-medium text-slate-200 truncate">
                  Photo downloaded · <span className="text-indigo-400">{item.eventName}</span>
                </p>
                <p className="text-xs text-slate-500 mt-0.5 flex items-center gap-1.5">
                  <span>{timeAgo(item.occurredAt)}</span>
                  {item.ipAddress && (
                    <>
                      <span className="text-slate-700">·</span>
                      <span className="font-mono">{item.ipAddress}</span>
                    </>
                  )}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
