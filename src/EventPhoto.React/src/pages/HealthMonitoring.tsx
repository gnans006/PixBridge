import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, XCircle, AlertTriangle, RefreshCw, Server, Database, HardDrive, ExternalLink, Brain } from 'lucide-react';
import { apiClient } from '../api/client';
import { useApplicationSettings } from '../hooks/useApplicationSettings';
import { Card } from '../components/UI/Card';
import { Badge } from '../components/UI/Badge';
import { Spinner } from '../components/UI/Spinner';
import { Button } from '../components/UI/Button';

interface HealthStatus {
  status: string;
  server: string;
  timestamp: string;
}

type HealthState = 'Healthy' | 'Degraded' | 'Offline';

interface ComponentHealth {
  name: string;
  status: HealthState;
  responseMs: number | null;
  detail: string | null;
}

interface ServiceHealthResult {
  database: ComponentHealth;
  aiService: ComponentHealth;
  storage: ComponentHealth;
  qrService: ComponentHealth;
  checkedAt: string;
}

async function fetchHealth(): Promise<HealthStatus> {
  const res = await apiClient.get<HealthStatus>('/health');
  return res.data;
}

async function fetchDeepHealth(): Promise<ServiceHealthResult> {
  const res = await apiClient.get<{ data: ServiceHealthResult }>('/health/services');
  return res.data.data;
}

function StatusIndicator({ ok, degraded, label }: { ok: boolean; degraded?: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2">
      {ok ? (
        <CheckCircle2 className="h-5 w-5 text-green-500" />
      ) : degraded ? (
        <AlertTriangle className="h-5 w-5 text-yellow-500" />
      ) : (
        <XCircle className="h-5 w-5 text-red-500" />
      )}
      <span className={`text-sm font-medium ${ok ? 'text-green-700' : degraded ? 'text-yellow-700' : 'text-red-700'}`}>
        {label}
      </span>
    </div>
  );
}

function ComponentHealthRow({ component }: { component: ComponentHealth | undefined }) {
  if (!component) return null;
  const ok = component.status === 'Healthy';
  const degraded = component.status === 'Degraded';
  return (
    <div className="flex items-start gap-2">
      {ok ? (
        <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 shrink-0" />
      ) : degraded ? (
        <AlertTriangle className="h-4 w-4 text-yellow-500 mt-0.5 shrink-0" />
      ) : (
        <XCircle className="h-4 w-4 text-red-500 mt-0.5 shrink-0" />
      )}
      <div className="flex-1 min-w-0">
        <p className={`text-sm font-medium ${ok ? 'text-green-700' : degraded ? 'text-yellow-700' : 'text-red-700'}`}>
          {component.name}
          {component.responseMs != null && (
            <span className="ml-2 text-xs font-normal text-gray-400">{component.responseMs}ms</span>
          )}
        </p>
        {component.detail && (
          <p className="text-xs text-gray-500 mt-0.5 break-words">{component.detail}</p>
        )}
      </div>
    </div>
  );
}

function MetricCard({ icon: Icon, label, value, sub, color = 'blue' }: {
  icon: typeof Server;
  label: string;
  value: string | number;
  sub?: string;
  color?: string;
}) {
  const colorMap: Record<string, string> = {
    blue: 'bg-blue-500',
    green: 'bg-green-500',
    orange: 'bg-orange-500',
    purple: 'bg-purple-500',
    indigo: 'bg-indigo-500',
  };
  return (
    <Card className="p-5">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="text-xl font-bold text-gray-900 mt-1">{value}</p>
          {sub && <p className="text-xs text-gray-400 mt-0.5">{sub}</p>}
        </div>
        <div className={`h-10 w-10 rounded-xl flex items-center justify-center ${colorMap[color] ?? colorMap.blue}`}>
          <Icon className="h-5 w-5 text-white" />
        </div>
      </div>
    </Card>
  );
}

function NetworkConfig() {
  const { data: settings } = useApplicationSettings();
  const publicBaseUrl = settings?.publicBaseUrl ?? '—';

  let lanDisplay = '—';
  let portDisplay = '—';
  try {
    const u = new URL(publicBaseUrl);
    lanDisplay  = u.hostname;
    portDisplay = `${u.port || (u.protocol === 'https:' ? '443' : '80')} (${u.protocol.replace(':', '').toUpperCase()})`;
  } catch { /* ignore — malformed URL */ }

  const items = [
    { label: 'Public Base URL',   value: publicBaseUrl },
    { label: 'Host',              value: lanDisplay },
    { label: 'Port',              value: portDisplay },
    { label: 'Guest Gallery',     value: publicBaseUrl !== '—' ? `${publicBaseUrl}/gallery/…` : '—' },
    { label: 'SignalR Hub',       value: '/hubs/photos (relative)' },
    { label: 'Admin Panel',       value: '/admin' },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
      {items.map(item => (
        <div key={item.label} className="rounded-lg bg-gray-50 px-4 py-3">
          <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{item.label}</p>
          <p className="font-mono text-gray-900 mt-1 break-all">{item.value}</p>
        </div>
      ))}
    </div>
  );
}

export default function HealthMonitoring() {
  const { data, isLoading, isError, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['health'],
    queryFn: fetchHealth,
    refetchInterval: 15_000,
    retry: 1,
  });

  const { data: deep, isLoading: deepLoading } = useQuery({
    queryKey: ['health-services'],
    queryFn: fetchDeepHealth,
    refetchInterval: 30_000,
    retry: 1,
  });

  const isHealthy = !isLoading && !isError && data?.status === 'healthy';

  const allComponents = deep ? [deep.database, deep.aiService, deep.storage, deep.qrService] : [];
  const hasOffline = allComponents.some(c => c.status === 'Offline');
  const hasDegraded = !hasOffline && allComponents.some(c => c.status === 'Degraded');
  const deepStatus = !deep ? null : hasOffline ? 'Offline' : hasDegraded ? 'Degraded' : 'Healthy';

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Health Monitoring</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            System status · Auto-refreshes every 15s
            {dataUpdatedAt ? ` · Last check: ${new Date(dataUpdatedAt).toLocaleTimeString()}` : ''}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <a
            href="/admin/deployment"
            className="inline-flex items-center gap-1.5 rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-600 hover:border-gray-300 hover:text-gray-900 transition-colors"
          >
            <ExternalLink className="h-3.5 w-3.5" />
            Deployment Center
          </a>
          <Button variant="secondary" size="sm" onClick={() => { refetch(); }}>
            <RefreshCw className="h-4 w-4" /> Check Now
          </Button>
        </div>
      </div>

      {/* Overall status banner */}
      <Card className={`p-5 border-2 ${isLoading ? 'border-gray-200' : isHealthy ? 'border-green-300 bg-green-50' : 'border-red-300 bg-red-50'}`}>
        <div className="flex items-center gap-3">
          {isLoading ? (
            <Spinner size="sm" />
          ) : isHealthy ? (
            <CheckCircle2 className="h-8 w-8 text-green-500" />
          ) : (
            <XCircle className="h-8 w-8 text-red-500" />
          )}
          <div>
            <p className={`text-lg font-bold ${isLoading ? 'text-gray-500' : isHealthy ? 'text-green-800' : 'text-red-800'}`}>
              {isLoading ? 'Checking...' : isHealthy ? 'API Online' : 'Service Degraded'}
            </p>
            <p className="text-sm text-gray-500">
              {data ? `Server: ${data.server} · ${new Date(data.timestamp).toLocaleString()}` : '—'}
            </p>
          </div>
          <div className="ml-auto flex items-center gap-2">
            <Badge
              label={isLoading ? 'Checking' : isHealthy ? 'Healthy' : 'Unhealthy'}
              color={isLoading ? 'gray' : isHealthy ? 'green' : 'red'}
            />
            {deepStatus && (
              <Badge
                label={`Deep: ${deepStatus}`}
                color={deepStatus === 'Healthy' ? 'green' : deepStatus === 'Degraded' ? 'yellow' : 'red'}
              />
            )}
          </div>
        </div>
      </Card>

      {/* Component checks */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {/* Basic service checks */}
        <Card className="p-5">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Core Services</h2>
          {isLoading ? (
            <Spinner size="sm" />
          ) : (
            <div className="space-y-3">
              <StatusIndicator ok={isHealthy} label="PixBridge API" />
              <StatusIndicator ok={isHealthy} label="SignalR Hub (/hubs/photos)" />
              <StatusIndicator ok={isHealthy} label="File Watcher Service" />
              <StatusIndicator ok={isHealthy} label="Thumbnail Processor" />
            </div>
          )}
        </Card>

        {/* Deep health (requires auth) */}
        <Card className="p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Infrastructure Health</h2>
            {deepLoading && <Spinner size="sm" />}
          </div>
          {!deep && !deepLoading ? (
            <p className="text-xs text-gray-400">Deep health check requires StudioOwner access.</p>
          ) : (
            <div className="space-y-3">
              <ComponentHealthRow component={deep?.database} />
              <ComponentHealthRow component={deep?.aiService} />
              <ComponentHealthRow component={deep?.storage} />
              <ComponentHealthRow component={deep?.qrService} />
            </div>
          )}
        </Card>
      </div>

      {/* Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard icon={Server}   label="API Server"   value={data?.server ?? '—'} sub="Kestrel :5000"      color="blue"   />
        <MetricCard icon={Database} label="Database"     value={deep?.database.status ?? 'PostgreSQL'}
          sub={deep?.database.responseMs != null ? `${deep.database.responseMs}ms` : 'Local instance'}
          color={deep?.database.status === 'Healthy' ? 'green' : 'orange'} />
        <MetricCard icon={Brain}    label="AI Face Recog" value={deep?.aiService.status ?? '—'}
          sub={deep?.aiService.responseMs != null ? `${deep.aiService.responseMs}ms` : 'Python :5001'}
          color={deep?.aiService.status === 'Healthy' ? 'indigo' : 'orange'} />
        <MetricCard icon={HardDrive} label="Storage"     value={deep?.storage.detail?.split(' free')[0] ?? '—'}
          sub="Application drive"
          color={deep?.storage.status === 'Healthy' ? 'green' : 'orange'} />
      </div>

      {/* Endpoints reference */}
      <Card className="p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Endpoint Reference</h2>
        <div className="space-y-2 text-sm">
          {[
            { method: 'GET', path: '/api/health',           desc: 'Liveness probe (anonymous)' },
            { method: 'GET', path: '/api/health/services',  desc: 'Deep health (OwnerOnly)' },
            { method: 'GET', path: '/api/deployment/status', desc: 'Deployment mode' },
            { method: 'GET', path: '/api/events',           desc: 'Event list' },
            { method: 'WS',  path: '/hubs/photos',          desc: 'SignalR hub' },
          ].map(ep => (
            <div key={ep.path} className="flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2">
              <span className={`text-xs font-bold rounded px-1.5 py-0.5 ${ep.method === 'WS' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'}`}>
                {ep.method}
              </span>
              <code className="text-gray-700 flex-1">{ep.path}</code>
              <span className="text-gray-400 text-xs">{ep.desc}</span>
              {isHealthy && ep.method !== 'WS' && ep.path !== '/api/health/services' && (
                <CheckCircle2 className="h-3 w-3 text-green-500" />
              )}
            </div>
          ))}
        </div>
      </Card>

      {/* Network config */}
      <Card className="p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Network Configuration</h2>
        <NetworkConfig />
      </Card>

      {isError && (
        <div className="rounded-xl bg-red-50 border border-red-200 p-4 text-sm text-red-700">
          <strong>Cannot reach the API server.</strong> Check that the PixBridgeApi service is running and port 5000 is accessible.
        </div>
      )}
    </div>
  );
}
