import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Activity,
  AlertTriangle,
  Brain,
  CheckCircle2,
  ChevronRight,
  Clock,
  Database,
  Globe,
  HardDrive,
  Info,
  Loader2,
  Network,
  QrCode,
  RefreshCw,
  Server,
  Shield,
  Wifi,
  XCircle,
  Zap,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { deploymentApi, type ComponentHealth, type DeploymentMode, type HealthStatus } from '../../api/deployment';

// ── Helpers ────────────────────────────────────────────────────────────────────

const MODE_META: Record<DeploymentMode, { icon: typeof Globe; color: string; badge: string }> = {
  Localhost: { icon: Server,  color: 'text-slate-400',  badge: 'bg-slate-700 text-slate-300' },
  Lan:       { icon: Wifi,   color: 'text-blue-400',   badge: 'bg-blue-500/20 text-blue-300 border border-blue-500/30' },
  Router:    { icon: Network, color: 'text-amber-400',  badge: 'bg-amber-500/20 text-amber-300 border border-amber-500/30' },
  Domain:    { icon: Globe,   color: 'text-emerald-400', badge: 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30' },
};

const HEALTH_META: Record<HealthStatus, { icon: typeof CheckCircle2; color: string; dot: string }> = {
  Healthy:  { icon: CheckCircle2, color: 'text-emerald-400', dot: 'bg-emerald-400' },
  Degraded: { icon: AlertTriangle, color: 'text-amber-400',  dot: 'bg-amber-400'  },
  Offline:  { icon: XCircle,      color: 'text-red-400',     dot: 'bg-red-400'     },
};

function HealthDot({ status }: { status: HealthStatus }) {
  const { dot } = HEALTH_META[status];
  return (
    <span className={`inline-block h-2 w-2 rounded-full ${dot} ${status === 'Healthy' ? '' : 'animate-pulse'}`} />
  );
}

function ServiceCard({ health }: { health: ComponentHealth }) {
  const meta = HEALTH_META[health.status];

  const iconMap: Record<string, typeof Database> = {
    PostgreSQL:          Database,
    'AI Face Recognition': Brain,
    Storage:             HardDrive,
    'QR Service':        QrCode,
  };
  const CardIcon = iconMap[health.name] ?? Activity;

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-800">
            <CardIcon className="h-5 w-5 text-slate-400" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-200">{health.name}</p>
            {health.detail && (
              <p className="mt-0.5 text-xs text-slate-500 max-w-xs">{health.detail}</p>
            )}
          </div>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          <div className={`flex items-center gap-1.5 text-xs font-semibold ${meta.color}`}>
            <HealthDot status={health.status} />
            {health.status}
          </div>
          {health.responseMs !== null && (
            <span className="text-xs text-slate-600 font-mono">{health.responseMs} ms</span>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Main page ──────────────────────────────────────────────────────────────────

export default function DeploymentCenterPage() {
  const qc = useQueryClient();
  const [regenCount, setRegenCount] = useState<number | null>(null);

  const { data: statusData, isLoading: statusLoading, refetch: refetchStatus } = useQuery({
    queryKey: ['deployment-status'],
    queryFn: () => deploymentApi.getStatus().then(r => r.data.data!),
    refetchInterval: 30_000,
  });

  const { data: healthData, isLoading: healthLoading, refetch: refetchHealth } = useQuery({
    queryKey: ['deployment-services'],
    queryFn: () => deploymentApi.getServiceHealth().then(r => r.data.data!),
    refetchInterval: 15_000,
  });

  const regenMutation = useMutation({
    mutationFn: () => deploymentApi.regenerateAllQr().then(r => r.data.data!),
    onSuccess: (count) => {
      setRegenCount(count);
      toast.success(`QR codes regenerated for ${count} event(s).`);
      void refetchStatus();
      void qc.invalidateQueries({ queryKey: ['workspace'] });
    },
    onError: () => toast.error('Failed to regenerate QR codes.'),
  });

  const handleRefreshAll = () => {
    void refetchStatus();
    void refetchHealth();
  };

  const status     = statusData;
  const deployment = status?.deployment;
  const health     = healthData;
  const modeMeta   = deployment ? MODE_META[deployment.mode] : null;
  const ModeIcon   = modeMeta?.icon ?? Server;

  const allHealthy = health && [
    health.database, health.aiService, health.storage, health.qrService,
  ].every(c => c.status === 'Healthy');

  return (
    <div className="space-y-6">
      {/* ── Header ── */}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-white">Deployment Center</h1>
          <p className="mt-1 text-sm text-slate-400">
            Network topology · Service health · QR management · Proxy validation
          </p>
        </div>
        <button
          onClick={handleRefreshAll}
          className="inline-flex items-center gap-2 rounded-xl border border-slate-700 px-4 py-2 text-sm font-medium text-slate-300 transition-colors hover:border-indigo-500 hover:text-white"
        >
          <RefreshCw className={`h-4 w-4 ${statusLoading || healthLoading ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {/* ── Deployment Mode Banner ── */}
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
      >
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            {statusLoading ? (
              <Loader2 className="h-8 w-8 animate-spin text-slate-500" />
            ) : (
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-800">
                <ModeIcon className={`h-6 w-6 ${modeMeta?.color ?? 'text-slate-400'}`} />
              </div>
            )}
            <div>
              <div className="flex items-center gap-2 mb-1">
                <p className="text-lg font-bold text-white">
                  {statusLoading ? '—' : deployment?.modeLabel ?? '—'}
                </p>
                {deployment && (
                  <span className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${modeMeta?.badge ?? ''}`}>
                    {deployment.mode}
                  </span>
                )}
              </div>
              <p className="text-sm text-slate-400 max-w-lg">
                {statusLoading ? 'Loading...' : deployment?.modeDescription ?? '—'}
              </p>
            </div>
          </div>
          {/* Overall service health pill */}
          {health && (
            <div className={`flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold ${
              allHealthy
                ? 'bg-emerald-500/10 border border-emerald-500/20 text-emerald-400'
                : 'bg-amber-500/10 border border-amber-500/20 text-amber-400'
            }`}>
              {allHealthy
                ? <><CheckCircle2 className="h-4 w-4" /> All Services Healthy</>
                : <><AlertTriangle className="h-4 w-4" /> Service Issues Detected</>}
            </div>
          )}
        </div>

        {/* HTTPS warning */}
        {deployment?.httpsWarning && (
          <div className="mt-4 flex items-start gap-3 rounded-xl border border-amber-500/20 bg-amber-500/5 px-4 py-3">
            <Shield className="h-4 w-4 text-amber-400 mt-0.5 shrink-0" />
            <p className="text-xs text-amber-300">
              <strong>HTTPS recommended.</strong> Your deployment is internet-accessible but serving over HTTP.
              JWT tokens are transmitted in plaintext. Configure Caddy or nginx with TLS for production use.
            </p>
          </div>
        )}
      </motion.div>

      {/* ── Three-column grid ── */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">

        {/* ── Network Identity ── */}
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
          <div className="mb-4 flex items-center gap-2">
            <Network className="h-4 w-4 text-indigo-400" />
            <h2 className="text-sm font-semibold text-white">Network Identity</h2>
          </div>
          {statusLoading ? (
            <Loader2 className="h-5 w-5 animate-spin text-slate-500" />
          ) : status ? (
            <div className="space-y-2.5 text-xs">
              <InfoRow label="Public Base URL"     value={deployment?.publicBaseUrl ?? '—'} mono />
              <InfoRow label="LAN IP"              value={status.lanIpAddress} mono />
              <InfoRow label="Server Port"         value={String(status.serverPort)} mono />
              <InfoRow label="HTTPS"               value={deployment?.isHttps ? 'Yes' : 'No'} />
              <InfoRow label="Explicit Port"        value={deployment?.hasExplicitPort ? 'Yes' : 'No'} />
              {deployment?.isReverseProxyDetected && (
                <InfoRow label="Reverse Proxy"     value={deployment.detectedProxy ?? 'Detected'} />
              )}
            </div>
          ) : null}
        </div>

        {/* ── QR Summary ── */}
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
          <div className="mb-4 flex items-center gap-2">
            <QrCode className="h-4 w-4 text-indigo-400" />
            <h2 className="text-sm font-semibold text-white">QR Code Status</h2>
          </div>
          {statusLoading ? (
            <Loader2 className="h-5 w-5 animate-spin text-slate-500" />
          ) : status ? (
            <>
              <div className="space-y-2.5 text-xs mb-4">
                <InfoRow label="Total Events"       value={String(status.totalEvents)} />
                <InfoRow label="Events with QR"     value={String(status.eventsWithQr)} />
                <InfoRow
                  label="Missing QR Files"
                  value={String(status.eventsWithMissingQr)}
                  highlight={status.eventsWithMissingQr > 0}
                />
              </div>
              <AnimatePresence>
                {regenCount !== null && (
                  <motion.div
                    initial={{ opacity: 0, y: 4 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0 }}
                    className="mb-3 flex items-center gap-2 rounded-lg bg-emerald-500/10 border border-emerald-500/20 px-3 py-2 text-xs text-emerald-400"
                  >
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    {regenCount} QR code(s) regenerated
                  </motion.div>
                )}
              </AnimatePresence>
              <button
                onClick={() => { setRegenCount(null); regenMutation.mutate(); }}
                disabled={regenMutation.isPending}
                className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 px-4 py-2.5 text-sm font-semibold text-white transition-colors"
              >
                {regenMutation.isPending
                  ? <><Loader2 className="h-4 w-4 animate-spin" /> Regenerating…</>
                  : <><RefreshCw className="h-4 w-4" /> Regenerate All QR Codes</>}
              </button>
              <p className="mt-2 text-center text-xs text-slate-600">
                Uses current Public Base URL
              </p>
            </>
          ) : null}
        </div>

        {/* ── Last checked ── */}
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
          <div className="mb-4 flex items-center gap-2">
            <Clock className="h-4 w-4 text-indigo-400" />
            <h2 className="text-sm font-semibold text-white">Diagnostics</h2>
          </div>
          <div className="space-y-3 text-xs">
            {status && (
              <div className="rounded-lg bg-slate-800/50 px-3 py-2">
                <p className="text-slate-500">Deployment check</p>
                <p className="text-slate-300 mt-0.5 font-mono">
                  {new Date(status.checkedAt).toLocaleTimeString()}
                </p>
              </div>
            )}
            {health && (
              <div className="rounded-lg bg-slate-800/50 px-3 py-2">
                <p className="text-slate-500">Service health check</p>
                <p className="text-slate-300 mt-0.5 font-mono">
                  {new Date(health.checkedAt).toLocaleTimeString()}
                </p>
              </div>
            )}
            <div className="rounded-lg bg-slate-800/50 px-3 py-2">
              <p className="text-slate-500">Auto-refresh</p>
              <p className="text-slate-300 mt-0.5">Deployment: 30s · Services: 15s</p>
            </div>
            {deployment?.isReverseProxyDetected && (
              <div className="flex items-start gap-2 rounded-lg border border-indigo-500/20 bg-indigo-500/5 px-3 py-2">
                <Zap className="h-3.5 w-3.5 text-indigo-400 mt-0.5 shrink-0" />
                <p className="text-indigo-300">
                  <strong>{deployment.detectedProxy}</strong> reverse proxy detected. ForwardedHeaders middleware is active.
                </p>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ── Service Health Cards ── */}
      <div>
        <div className="mb-4 flex items-center gap-2">
          <Activity className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Service Registry</h2>
          {healthLoading && <Loader2 className="h-3.5 w-3.5 animate-spin text-slate-500" />}
        </div>
        {health ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ServiceCard health={health.database} />
            <ServiceCard health={health.aiService} />
            <ServiceCard health={health.storage} />
            <ServiceCard health={health.qrService} />
          </div>
        ) : healthLoading ? (
          <div className="flex h-32 items-center justify-center rounded-2xl border border-slate-800 bg-slate-900">
            <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
          </div>
        ) : null}
      </div>

      {/* ── Reverse Proxy Guide ── */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-4 flex items-center gap-2">
          <Info className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Reverse Proxy Configuration Guide</h2>
        </div>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3 text-xs">
          <ProxyGuide
            name="Caddy"
            description="Auto TLS via Let's Encrypt. Simplest production setup."
            config={`photos.yourdomain.com {\n  reverse_proxy localhost:5000\n}`}
          />
          <ProxyGuide
            name="nginx"
            description="Manual TLS certificate required. Include Upgrade headers for SignalR."
            config={`server {\n  listen 443 ssl;\n  server_name photos.yourdomain.com;\n  location / {\n    proxy_pass http://localhost:5000;\n    proxy_http_version 1.1;\n    proxy_set_header Upgrade $http_upgrade;\n    proxy_set_header Connection "upgrade";\n    proxy_set_header X-Forwarded-Proto https;\n  }\n}`}
          />
          <ProxyGuide
            name="IIS ARR"
            description="Windows Server reverse proxy. Enable WebSocket support in IIS."
            config={`<!-- web.config -->\n<rewrite>\n  <rules>\n    <rule name="PixBridge">\n      <match url="(.*)" />\n      <action type="Rewrite"\n        url="http://localhost:5000/{R:1}" />\n    </rule>\n  </rules>\n</rewrite>`}
          />
        </div>
        <div className="mt-4 flex items-start gap-2 rounded-xl border border-slate-700 bg-slate-800/50 px-4 py-3">
          <Shield className="h-4 w-4 text-indigo-400 mt-0.5 shrink-0" />
          <p className="text-xs text-slate-400">
            After configuring a reverse proxy, set <strong className="text-slate-200">Public Base URL</strong> to your HTTPS domain in{' '}
            <a href="/admin/system-settings" className="text-indigo-400 hover:underline">System Settings → Network</a>.
            QR codes will auto-regenerate when you save.
          </p>
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────────────────────────────

function InfoRow({
  label,
  value,
  mono = false,
  highlight = false,
}: {
  label: string;
  value: string;
  mono?: boolean;
  highlight?: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="text-slate-500 shrink-0">{label}</span>
      <span className={`text-right break-all ${mono ? 'font-mono' : ''} ${highlight ? 'text-amber-400 font-semibold' : 'text-slate-300'}`}>
        {value}
      </span>
    </div>
  );
}

function ProxyGuide({
  name,
  description,
  config,
}: {
  name: string;
  description: string;
  config: string;
}) {
  return (
    <div className="rounded-xl border border-slate-700 bg-slate-800/50 p-4">
      <div className="mb-2 flex items-center gap-2">
        <ChevronRight className="h-3 w-3 text-indigo-400" />
        <p className="font-semibold text-slate-200 text-xs">{name}</p>
      </div>
      <p className="mb-3 text-slate-500">{description}</p>
      <pre className="overflow-x-auto rounded-lg bg-slate-900 p-3 text-xs text-emerald-400 leading-relaxed whitespace-pre-wrap font-mono">
        {config}
      </pre>
    </div>
  );
}
