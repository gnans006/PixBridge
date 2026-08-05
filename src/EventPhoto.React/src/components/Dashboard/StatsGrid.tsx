import { useQuery } from '@tanstack/react-query';
import type { LucideIcon } from 'lucide-react';
import { Activity, Calendar, Download, HardDrive, Images, ScanFace } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';
import { useCountUp } from '../../hooks/useCountUp';

interface KpiCardProps {
  label: string;
  value: number;
  subValue?: string;
  icon: LucideIcon;
  gradient: string;
  iconBg: string;
  delay?: string;
}

function KpiCard({ label, value, subValue, icon: Icon, gradient, iconBg, delay = '0ms' }: KpiCardProps) {
  const animated = useCountUp(value);

  return (
    <div
      className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900 p-5 transition-all duration-300 hover:-translate-y-0.5 hover:border-slate-700 hover:shadow-xl hover:shadow-black/30 animate-slide-up"
      style={{ animationDelay: delay }}
    >
      <div className={`pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100 ${gradient}`} />

      <div className="relative flex items-start justify-between">
        <div className="flex-1">
          <p className="text-xs font-medium uppercase tracking-wider text-slate-400">{label}</p>
          <p className="mt-2 text-3xl font-bold text-white">
            {subValue ? subValue : animated.toLocaleString()}
          </p>
        </div>
        <div className={`flex h-11 w-11 items-center justify-center rounded-xl ${iconBg}`}>
          <Icon className="h-5 w-5 text-white" />
        </div>
      </div>
    </div>
  );
}

function KpiSkeleton() {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5 animate-pulse">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="h-3 w-24 rounded bg-slate-800" />
          <div className="mt-3 h-8 w-16 rounded bg-slate-800" />
        </div>
        <div className="h-11 w-11 rounded-xl bg-slate-800" />
      </div>
    </div>
  );
}

export function StatsGrid() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['dashboard-overview'],
    queryFn: () => dashboardApi.getOverview(),
    refetchInterval: 30_000,
    select: (r) => r.data,
  });

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }).map((_, i) => <KpiSkeleton key={i} />)}
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
            <p className="text-xs font-medium uppercase tracking-wider text-slate-600">—</p>
            <p className="mt-2 text-3xl font-bold text-slate-700">—</p>
          </div>
        ))}
      </div>
    );
  }

  const cards: KpiCardProps[] = [
    {
      label: 'Active Events',
      value: data.activeEvents,
      icon: Calendar,
      gradient: 'bg-gradient-to-br from-emerald-500/10 to-transparent',
      iconBg: 'bg-emerald-600',
      delay: '0ms',
    },
    {
      label: 'Total Photos',
      value: data.totalPhotos,
      icon: Images,
      gradient: 'bg-gradient-to-br from-violet-500/10 to-transparent',
      iconBg: 'bg-violet-600',
      delay: '60ms',
    },
    {
      label: 'Downloads Today',
      value: data.downloadsToday,
      icon: Download,
      gradient: 'bg-gradient-to-br from-indigo-500/10 to-transparent',
      iconBg: 'bg-indigo-600',
      delay: '120ms',
    },
    {
      label: 'Total Downloads',
      value: data.totalDownloads,
      icon: Activity,
      gradient: 'bg-gradient-to-br from-blue-500/10 to-transparent',
      iconBg: 'bg-blue-600',
      delay: '180ms',
    },
    {
      label: 'Indexed Faces',
      value: data.totalFaceEmbeddings,
      icon: ScanFace,
      gradient: 'bg-gradient-to-br from-amber-500/10 to-transparent',
      iconBg: 'bg-amber-600',
      delay: '240ms',
    },
    {
      label: 'Storage Used',
      value: 0,
      subValue: data.totalSizeHuman,
      icon: HardDrive,
      gradient: 'bg-gradient-to-br from-slate-500/10 to-transparent',
      iconBg: 'bg-slate-600',
      delay: '300ms',
    },
  ];

  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
      {cards.map((card) => (
        <KpiCard key={card.label} {...card} />
      ))}
    </div>
  );
}
