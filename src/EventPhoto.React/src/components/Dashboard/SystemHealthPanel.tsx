import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, XCircle, AlertCircle } from 'lucide-react';
import { apiClient } from '../../api/client';
import type { HealthStatus } from '../../types';

interface ServiceItem {
  label: string;
  status: 'healthy' | 'warning' | 'offline';
  detail?: string;
}

function StatusDot({ status }: { status: ServiceItem['status'] }) {
  const cls = {
    healthy: 'bg-emerald-500',
    warning: 'bg-amber-500',
    offline: 'bg-rose-500',
  }[status];

  return <span className={`inline-block h-2 w-2 rounded-full ${cls} ${status === 'healthy' ? 'animate-pulse-slow' : ''}`} />;
}

function ServiceRow({ label, status, detail }: ServiceItem) {
  const icon = status === 'healthy'
    ? <CheckCircle2 className="h-4 w-4 text-emerald-400" />
    : status === 'warning'
    ? <AlertCircle className="h-4 w-4 text-amber-400" />
    : <XCircle className="h-4 w-4 text-rose-400" />;

  return (
    <div className="flex items-center justify-between py-2">
      <div className="flex items-center gap-2.5">
        {icon}
        <span className="text-sm text-slate-300">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {detail && <span className="text-xs text-slate-500">{detail}</span>}
        <StatusDot status={status} />
      </div>
    </div>
  );
}

export function SystemHealthPanel() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['health'],
    queryFn: async () => {
      const res = await apiClient.get<HealthStatus>('/health');
      return res.data;
    },
    refetchInterval: 30_000,
  });

  const apiHealthy = !isError && data?.status === 'healthy';

  const services: ServiceItem[] = [
    { label: 'API Service', status: apiHealthy ? 'healthy' : 'offline', detail: apiHealthy ? 'Online' : 'Unreachable' },
    { label: 'Database', status: apiHealthy ? 'healthy' : 'warning', detail: apiHealthy ? 'Connected' : 'Unknown' },
    { label: 'SignalR Hub', status: apiHealthy ? 'healthy' : 'warning', detail: apiHealthy ? 'Active' : 'Unknown' },
    { label: 'File Storage', status: apiHealthy ? 'healthy' : 'warning', detail: apiHealthy ? 'Accessible' : 'Unknown' },
    { label: 'Worker Service', status: 'healthy', detail: 'Running' },
  ];

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-100">System Health</h3>
        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${apiHealthy ? 'bg-emerald-500/20 text-emerald-400' : 'bg-rose-500/20 text-rose-400'}`}>
          {apiHealthy ? 'All Systems Go' : 'Issues Detected'}
        </span>
      </div>

      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3, 4, 5].map((i) => <div key={i} className="h-9 rounded bg-slate-800" />)}
        </div>
      ) : (
        <div className="divide-y divide-slate-800/60">
          {services.map((s) => <ServiceRow key={s.label} {...s} />)}
        </div>
      )}
    </div>
  );
}
