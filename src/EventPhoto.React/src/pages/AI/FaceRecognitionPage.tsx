import { useQuery } from '@tanstack/react-query';
import { ScanFace, Brain, Clock, Layers, Wifi, WifiOff, Loader2 } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';
import { PageShell } from '../../components/UI/PageShell';
import { Card } from '../../components/UI/Card';
import { SkeletonCard } from '../../components/UI/Skeleton';
import { EmptyState } from '../../components/UI/EmptyState';

function StatBlock({ label, value, color = 'text-pds-text' }: { label: string; value: string | number; color?: string }) {
  return (
    <div className="rounded-xl bg-pds-elevated px-4 py-3">
      <p className="text-xs text-pds-text-muted">{label}</p>
      <p className={`mt-1 text-2xl font-bold ${color}`}>{value}</p>
    </div>
  );
}

function ProgressBar({ value, max }: { value: number; max: number }) {
  const pct = max > 0 ? Math.min((value / max) * 100, 100) : 0;
  return (
    <div className="h-1.5 w-full overflow-hidden rounded-full bg-pds-elevated">
      <div
        className="h-full rounded-full bg-pds-primary transition-all duration-700"
        style={{ width: `${pct}%` }}
      />
    </div>
  );
}

function ServiceStatusBadge() {
  const { data, isLoading } = useQuery({
    queryKey: ['face-service-health'],
    queryFn: () => dashboardApi.getFaceServiceHealth(),
    refetchInterval: 15_000,
    retry: false,
  });

  if (isLoading) return null;

  const status = data?.status ?? 'offline';
  const configs = {
    ready:   { icon: Wifi,    label: 'AI Service Ready',   cls: 'text-pds-success bg-pds-success/10 border-pds-success/30' },
    loading: { icon: Loader2, label: 'Model Loading…',     cls: 'text-pds-warning bg-pds-warning/10 border-pds-warning/30' },
    offline: { icon: WifiOff, label: 'AI Service Offline', cls: 'text-pds-danger  bg-pds-danger/10  border-pds-danger/30'  },
  } as const;
  const { icon: Icon, label, cls } = configs[status];

  return (
    <span className={`flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium ${cls}`}>
      <Icon className={`h-3.5 w-3.5 ${status === 'loading' ? 'animate-spin' : ''}`} />
      {label}
      {data?.version && status === 'ready' && (
        <span className="opacity-60">v{data.version}</span>
      )}
    </span>
  );
}

export default function FaceRecognitionPage() {
  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['face-analytics'],
    queryFn: () => dashboardApi.getFaceAnalytics(),
    refetchInterval: 60_000,
    select: (r) => r.data,
  });

  return (
    <PageShell
      title="Face Recognition"
      description="AI-powered face indexing status and per-production analytics."
      actions={
        <div className="flex items-center gap-3">
          <ServiceStatusBadge />
          <button
            onClick={() => refetch()}
            className="flex items-center gap-2 rounded-lg border border-pds-border bg-pds-elevated px-3 py-2 text-sm font-medium text-pds-text-2 hover:bg-pds-card hover:text-pds-text transition-colors"
          >
            <ScanFace className="h-4 w-4" />
            Refresh
          </button>
        </div>
      }
    >
      {isLoading ? (
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            {[1, 2, 3].map(i => <div key={i} className="h-20 pds-shimmer rounded-xl" />)}
          </div>
          <SkeletonCard />
        </div>
      ) : isError ? (
        <Card padding>
          <p className="text-sm text-pds-danger">
            Could not load face recognition data. Make sure the AI service is running.
          </p>
        </Card>
      ) : (
        <div className="space-y-6">
          {/* Stats */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <StatBlock
              label="Total Indexed Faces"
              value={(data?.totalIndexedFaces ?? 0).toLocaleString()}
              color="text-pds-primary"
            />
            <StatBlock
              label="AI-Enabled Productions"
              value={data?.eventsWithFaceSearch ?? 0}
            />
            <StatBlock
              label="Pending Photos"
              value={data?.totalPendingPhotos ?? 0}
              color={data?.totalPendingPhotos ? 'text-pds-warning' : 'text-pds-text'}
            />
          </div>

          {/* Per-event breakdown */}
          <Card padding>
            <div className="mb-4 flex items-center gap-2">
              <Layers className="h-4 w-4 text-pds-primary" />
              <h2 className="text-sm font-semibold text-pds-text">Production Breakdown</h2>
            </div>

            {!data?.eventBreakdown?.length ? (
              <EmptyState
                icon={Brain}
                title="No AI Productions"
                description="Enable face recognition on a production to start indexing faces."
              />
            ) : (
              <div className="space-y-4">
                {data.eventBreakdown.map((item) => (
                  <div key={item.eventId}>
                    <div className="mb-1.5 flex items-center justify-between">
                      <span className="text-sm text-pds-text-2 truncate max-w-xs">{item.eventName}</span>
                      <div className="flex items-center gap-4 text-xs text-pds-text-muted flex-none ml-4">
                        <span className="flex items-center gap-1">
                          <ScanFace className="h-3.5 w-3.5 text-pds-primary" />
                          {item.faceEmbeddings.toLocaleString()} faces
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-3.5 w-3.5" />
                          {item.photoCount.toLocaleString()} photos
                        </span>
                      </div>
                    </div>
                    <ProgressBar
                      value={item.faceEmbeddings}
                      max={Math.max(...data.eventBreakdown.map(e => e.faceEmbeddings), 1)}
                    />
                  </div>
                ))}
              </div>
            )}
          </Card>

          {/* Info */}
          <Card padding>
            <div className="flex items-start gap-3">
              <Brain className="h-5 w-5 text-pds-accent flex-none mt-0.5" />
              <div className="space-y-1">
                <p className="text-sm font-semibold text-pds-text">How Face Recognition Works</p>
                <p className="text-sm text-pds-text-muted leading-relaxed">
                  When a photo is imported, the AI worker detects and indexes all faces automatically.
                  Guests can upload a selfie on the QR landing page to instantly find their photos.
                  Indexing happens in the background — higher pending counts mean the worker is processing a large batch.
                </p>
              </div>
            </div>
          </Card>
        </div>
      )}
    </PageShell>
  );
}
