import { useQuery } from '@tanstack/react-query';
import { HardDrive } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

function StorageBar({ sizeBytes, totalBytes, name, sizeHuman }: {
  sizeBytes: number; totalBytes: number; name: string; sizeHuman: string;
}) {
  const pct = totalBytes > 0 ? Math.min((sizeBytes / totalBytes) * 100, 100) : 0;
  const colors = ['bg-indigo-500', 'bg-violet-500', 'bg-blue-500', 'bg-cyan-500', 'bg-teal-500', 'bg-emerald-500', 'bg-amber-500', 'bg-rose-500'];
  const idx = Math.abs(name.split('').reduce((a, c) => a + c.charCodeAt(0), 0)) % colors.length;
  const color = colors[idx];

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between text-xs">
        <span className="truncate max-w-[160px] text-slate-300">{name}</span>
        <span className="ml-2 flex-shrink-0 text-slate-400">{sizeHuman}</span>
      </div>
      <div className="h-1.5 w-full overflow-hidden rounded-full bg-slate-800">
        <div
          className={`h-full rounded-full transition-all duration-700 ${color}`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}

export function StorageAnalytics() {
  const { data, isLoading } = useQuery({
    queryKey: ['storage-analytics'],
    queryFn: () => dashboardApi.getStorageAnalytics(),
    refetchInterval: 60_000,
    select: (r) => r.data,
  });

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-700">
            <HardDrive className="h-4 w-4 text-slate-300" />
          </div>
          <h3 className="text-sm font-semibold text-slate-100">Storage Analytics</h3>
        </div>
        {data && (
          <span className="text-xs font-medium text-slate-400">
            {data.eventCount} events · {data.totalSizeHuman} total
          </span>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-4 animate-pulse">
          {[1, 2, 3, 4].map((i) => <div key={i} className="h-8 rounded bg-slate-800" />)}
        </div>
      ) : !data?.topEvents.length ? (
        <p className="text-center text-xs text-slate-500 py-6">No storage data available</p>
      ) : (
        <div className="space-y-4">
          {data.topEvents.map((item) => (
            <StorageBar
              key={item.eventId}
              name={item.eventName}
              sizeBytes={item.sizeBytes}
              totalBytes={data.totalSizeBytes}
              sizeHuman={item.sizeHuman}
            />
          ))}
        </div>
      )}
    </div>
  );
}
