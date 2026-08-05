import { useQuery } from '@tanstack/react-query';
import { Cpu, ImageIcon, ScanFace, XCircle } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

function ProcessingItem({ icon: Icon, label, count, color }: {
  icon: typeof Cpu; label: string; count: number; color: string;
}) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-slate-800/50 px-3 py-2.5">
      <div className="flex items-center gap-2">
        <Icon className={`h-4 w-4 ${color}`} />
        <span className="text-xs text-slate-300">{label}</span>
      </div>
      <span className={`text-sm font-bold ${count > 0 ? color : 'text-slate-500'}`}>
        {count.toLocaleString()}
      </span>
    </div>
  );
}

export function ProcessingStatusPanel() {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-overview'],
    queryFn: () => dashboardApi.getOverview(),
    refetchInterval: 15_000,
    select: (r) => r.data,
  });

  const items = [
    { icon: ImageIcon, label: 'Pending Thumbnails', count: data?.pendingThumbnails ?? 0, color: 'text-violet-400' },
    { icon: ScanFace, label: 'Pending Face Index', count: data?.pendingFaceIndexes ?? 0, color: 'text-amber-400' },
    { icon: XCircle, label: 'Failed Jobs', count: 0, color: 'text-rose-400' },
  ];

  const totalPending = (data?.pendingThumbnails ?? 0) + (data?.pendingFaceIndexes ?? 0);

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-violet-500/20">
            <Cpu className="h-4 w-4 text-violet-400" />
          </div>
          <h3 className="text-sm font-semibold text-slate-100">Processing Queue</h3>
        </div>
        {totalPending > 0 && (
          <span className="rounded-full bg-amber-500/20 px-2 py-0.5 text-xs font-medium text-amber-400">
            {totalPending} pending
          </span>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3].map((i) => <div key={i} className="h-10 rounded-xl bg-slate-800" />)}
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((item) => <ProcessingItem key={item.label} {...item} />)}
        </div>
      )}

      {!isLoading && totalPending === 0 && (
        <p className="mt-3 text-center text-xs text-emerald-400">
          ✓ All jobs complete
        </p>
      )}
    </div>
  );
}
