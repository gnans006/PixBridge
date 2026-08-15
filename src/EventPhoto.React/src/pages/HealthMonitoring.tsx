import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, XCircle, RefreshCw, Server, Database, HardDrive, Cpu, ExternalLink } from 'lucide-react';
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

async function fetchHealth(): Promise<HealthStatus> {
  const res = await apiClient.get<HealthStatus>('/health');
  return res.data;
}

function StatusIndicator({ ok, label }: { ok: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2">
      {ok ? (
        <CheckCircle2 className="h-5 w-5 text-green-500" />
      ) : (
        <XCircle className="h-5 w-5 text-red-500" />
      )}
      <span className={`text-sm font-medium ${ok ? 'text-green-700' : 'text-red-700'}`}>{label}</span>
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
  // Read from ApplicationSettings (authoritative source) instead of legacy app.serverUrl
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

  const isHealthy = !isLoading && !isError && data?.status === 'healthy';

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Health Monitoring</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            System status · Auto-refreshes every 15 seconds
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
          <Button variant="secondary" size="sm" onClick={() => refetch()}>
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
              {isLoading ? 'Checking...' : isHealthy ? 'All Systems Operational' : 'Service Degraded'}
            </p>
            <p className="text-sm text-gray-500">
              {data ? `Server: ${data.server} · ${new Date(data.timestamp).toLocaleString()}` : '—'}
            </p>
          </div>
          <div className="ml-auto">
            <Badge
              label={isLoading ? 'Checking' : isHealthy ? 'Healthy' : 'Unhealthy'}
              color={isLoading ? 'gray' : isHealthy ? 'green' : 'red'}
            />
          </div>
        </div>
      </Card>

      {/* Component checks */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Card className="p-5">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Service Status</h2>
          {isLoading ? (
            <Spinner size="sm" />
          ) : (
            <div className="space-y-3">
              <StatusIndicator ok={isHealthy} label="PixBridge API" />
              <StatusIndicator ok={isHealthy} label="SignalR Hub (/hubs/photos)" />
              <StatusIndicator ok={isHealthy} label="File Storage Service" />
              <StatusIndicator ok={isHealthy} label="Thumbnail Processor" />
              <StatusIndicator ok={isHealthy} label="File Watcher Service" />
            </div>
          )}
        </Card>

        <Card className="p-5">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Endpoints</h2>
          <div className="space-y-2 text-sm">
            {[
              { method: 'GET', path: '/api/health', desc: 'Liveness probe' },
              { method: 'GET', path: '/api/health/services', desc: 'Deep service health' },
              { method: 'GET', path: '/api/deployment/status', desc: 'Deployment mode' },
              { method: 'GET', path: '/api/events', desc: 'Event list' },
              { method: 'WS',  path: '/hubs/photos', desc: 'SignalR hub' },
            ].map(ep => (
              <div key={ep.path} className="flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2">
                <span className={`text-xs font-bold rounded px-1.5 py-0.5 ${ep.method === 'WS' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'}`}>
                  {ep.method}
                </span>
                <code className="text-gray-700 flex-1">{ep.path}</code>
                <span className="text-gray-400 text-xs">{ep.desc}</span>
                {isHealthy && ep.method !== 'WS' && (
                  <CheckCircle2 className="h-3 w-3 text-green-500" />
                )}
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard icon={Server}   label="API Server"  value={data?.server ?? '—'} sub="Kestrel on :5000"   color="blue"   />
        <MetricCard icon={Database} label="Database"    value="PostgreSQL"           sub="Local instance"    color="green"  />
        <MetricCard icon={Cpu}      label="Runtime"     value=".NET 8"               sub="Self-contained"    color="purple" />
        <MetricCard icon={HardDrive} label="Storage"   value="Local Disk"            sub="Photos + QR codes" color="orange" />
      </div>

      {/* Network info — reads from PublicBaseUrl, not legacy app.serverUrl */}
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
