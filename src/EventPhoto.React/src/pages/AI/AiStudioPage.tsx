import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Brain,
  Activity,
  AlertTriangle,
  CheckCircle2,
  RefreshCw,
  Search,
  BarChart3,
  Server,
  Zap,
  RotateCcw,
  XCircle,
  Layers,
  TrendingUp,
} from 'lucide-react';
import { aiStudioApi } from '../../api/aiStudio';
import toast from 'react-hot-toast';
import type { DeadLetterJobResponse, EventAiHealthResponse } from '../../types/aiDiscovery';

// ─────────────────────────────────────────────────────────────────────────────
// Utility helpers
// ─────────────────────────────────────────────────────────────────────────────

function fmtMs(ms: number) {
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

function fmtPct(pct: number) {
  return `${pct.toFixed(1)}%`;
}

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'bg-slate-700 text-slate-300',
    Queued: 'bg-indigo-900/60 text-indigo-300',
    Detecting: 'bg-blue-900/60 text-blue-300',
    QualityChecking: 'bg-purple-900/60 text-purple-300',
    Embedding: 'bg-violet-900/60 text-violet-300',
    Indexing: 'bg-cyan-900/60 text-cyan-300',
    Completed: 'bg-emerald-900/60 text-emerald-300',
    Failed: 'bg-amber-900/60 text-amber-300',
    DeadLettered: 'bg-red-900/60 text-red-300',
    Ignored: 'bg-slate-800 text-slate-500',
  };
  return map[status] ?? 'bg-slate-700 text-slate-400';
}

// ─────────────────────────────────────────────────────────────────────────────
// Sub-components
// ─────────────────────────────────────────────────────────────────────────────

function MetricCard({
  label,
  value,
  icon: Icon,
  accent = 'indigo',
  sub,
}: {
  label: string;
  value: string | number;
  icon: React.ElementType;
  accent?: string;
  sub?: string;
}) {
  const accentMap: Record<string, string> = {
    indigo: 'text-indigo-400 bg-indigo-500/10 border-indigo-500/20',
    emerald: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20',
    amber: 'text-amber-400 bg-amber-500/10 border-amber-500/20',
    red: 'text-red-400 bg-red-500/10 border-red-500/20',
    violet: 'text-violet-400 bg-violet-500/10 border-violet-500/20',
    cyan: 'text-cyan-400 bg-cyan-500/10 border-cyan-500/20',
  };
  const cls = accentMap[accent] ?? accentMap.indigo;

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-slate-500 font-medium uppercase tracking-wider">{label}</p>
          <p className="mt-1 text-2xl font-bold text-slate-100">{value}</p>
          {sub && <p className="mt-0.5 text-xs text-slate-500">{sub}</p>}
        </div>
        <div className={`rounded-lg border p-2 ${cls}`}>
          <Icon className="h-4 w-4" />
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab: Overview
// ─────────────────────────────────────────────────────────────────────────────

function OverviewTab() {
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['ai-studio-overview'],
    queryFn: aiStudioApi.getOverview,
    refetchInterval: 15_000,
  });

  if (isLoading || !data) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="rounded-xl border border-slate-800 bg-slate-900 p-4 animate-pulse">
            <div className="h-3 bg-slate-800 rounded w-24 mb-2" />
            <div className="h-7 bg-slate-800 rounded w-16" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Pipeline health banner */}
      <div
        className={`flex items-center gap-3 rounded-xl border px-4 py-3 ${
          data.isPipelineHealthy
            ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-400'
            : 'border-red-500/20 bg-red-500/5 text-red-400'
        }`}
      >
        {data.isPipelineHealthy ? (
          <CheckCircle2 className="h-5 w-5 shrink-0" />
        ) : (
          <AlertTriangle className="h-5 w-5 shrink-0" />
        )}
        <span className="text-sm font-medium">{data.pipelineStatusMessage}</span>
        <button
          onClick={() => refetch()}
          className="ml-auto text-slate-500 hover:text-slate-300 transition-colors"
        >
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {/* Metrics grid */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        <MetricCard
          label="Photos Indexed"
          value={data.totalPhotosIndexed.toLocaleString()}
          icon={Layers}
          accent="indigo"
        />
        <MetricCard
          label="Faces Indexed"
          value={data.totalFacesIndexed.toLocaleString()}
          icon={Brain}
          accent="violet"
        />
        <MetricCard
          label="Queue Depth"
          value={data.queueDepth}
          icon={Server}
          accent={data.queueDepth > 50 ? 'amber' : 'indigo'}
          sub={`${data.pendingJobs} pending · ${data.processingJobs} processing`}
        />
        <MetricCard
          label="Failed Jobs"
          value={data.failedJobs}
          icon={AlertTriangle}
          accent={data.failedJobs > 0 ? 'amber' : 'emerald'}
        />
        <MetricCard
          label="Dead Letter"
          value={data.deadLetteredJobs}
          icon={XCircle}
          accent={data.deadLetteredJobs > 0 ? 'red' : 'emerald'}
        />
        <MetricCard
          label="Searches (24h)"
          value={data.totalSearchesLast24H.toLocaleString()}
          icon={Search}
          accent="cyan"
        />
        <MetricCard
          label="Success Rate"
          value={fmtPct(data.searchSuccessRatePercent)}
          icon={TrendingUp}
          accent={data.searchSuccessRatePercent >= 90 ? 'emerald' : data.searchSuccessRatePercent >= 70 ? 'amber' : 'red'}
        />
        <MetricCard
          label="Avg Search Time"
          value={fmtMs(data.averageSearchDurationMs)}
          icon={Zap}
          accent={data.averageSearchDurationMs <= 2000 ? 'emerald' : 'amber'}
          sub="Target: < 2 seconds"
        />
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab: Dead Letter Queue
// ─────────────────────────────────────────────────────────────────────────────

function DeadLetterTab() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['ai-studio-dead-letter', page],
    queryFn: () => aiStudioApi.getDeadLetterQueue(page),
  });

  const retryMutation = useMutation({
    mutationFn: (jobId: string) => aiStudioApi.retryDeadLetterJob(jobId),
    onSuccess: () => {
      toast.success('Job queued for retry.');
      qc.invalidateQueries({ queryKey: ['ai-studio-dead-letter'] });
      qc.invalidateQueries({ queryKey: ['ai-studio-overview'] });
    },
    onError: () => toast.error('Failed to retry job.'),
  });

  const ignoreMutation = useMutation({
    mutationFn: (jobId: string) => aiStudioApi.ignoreDeadLetterJob(jobId),
    onSuccess: () => {
      toast.success('Job marked as ignored.');
      qc.invalidateQueries({ queryKey: ['ai-studio-dead-letter'] });
    },
    onError: () => toast.error('Failed to ignore job.'),
  });

  if (isLoading) {
    return <div className="text-slate-500 text-sm">Loading dead-letter queue…</div>;
  }

  if (!data || data.items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <CheckCircle2 className="h-12 w-12 mb-3 text-emerald-600/40" />
        <p className="text-sm font-medium">Dead-letter queue is empty.</p>
        <p className="text-xs text-slate-600 mt-1">All AI processing jobs are healthy.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="text-xs text-slate-500 mb-3">
        {data.totalCount} job{data.totalCount !== 1 ? 's' : ''} in dead-letter queue
      </div>
      {data.items.map((job: DeadLetterJobResponse) => (
        <div
          key={job.jobId}
          className="rounded-xl border border-slate-800 bg-slate-900 p-4"
        >
          <div className="flex items-start gap-4">
            {job.thumbnailUrl && (
              <img
                src={job.thumbnailUrl}
                alt={job.fileName}
                className="h-14 w-14 rounded-lg object-cover shrink-0 bg-slate-800"
                onError={e => (e.currentTarget.style.display = 'none')}
              />
            )}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-sm font-medium text-slate-200 truncate">{job.fileName}</span>
                <span className={`text-xs rounded-full px-2 py-0.5 font-medium ${statusBadge(job.status)}`}>
                  {job.status}
                </span>
                {job.failureType && (
                  <span className="text-xs text-slate-500 bg-slate-800 rounded px-2 py-0.5">
                    {job.failureType}
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-500 mt-0.5">{job.eventName}</p>
              {job.lastError && (
                <p className="text-xs text-red-400/80 mt-1 line-clamp-2">{job.lastError}</p>
              )}
              <p className="text-xs text-slate-600 mt-1">
                {job.retryCount} attempt{job.retryCount !== 1 ? 's' : ''} ·{' '}
                {new Date(job.createdAt).toLocaleString()}
              </p>
            </div>
            <div className="flex gap-2 shrink-0">
              <button
                onClick={() => retryMutation.mutate(job.jobId)}
                disabled={retryMutation.isPending}
                className="flex items-center gap-1.5 rounded-lg border border-indigo-600/40 bg-indigo-600/10 px-3 py-1.5 text-xs font-medium text-indigo-400 hover:bg-indigo-600/20 transition-colors disabled:opacity-50"
              >
                <RotateCcw className="h-3 w-3" />
                Retry
              </button>
              <button
                onClick={() => ignoreMutation.mutate(job.jobId)}
                disabled={ignoreMutation.isPending}
                className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-400 hover:text-slate-200 transition-colors disabled:opacity-50"
              >
                <XCircle className="h-3 w-3" />
                Ignore
              </button>
            </div>
          </div>
        </div>
      ))}

      {/* Pagination */}
      {data.totalCount > data.pageSize && (
        <div className="flex justify-center gap-2 pt-2">
          <button
            disabled={page <= 1}
            onClick={() => setPage(p => p - 1)}
            className="rounded-lg border border-slate-700 bg-slate-800 px-3 py-1.5 text-xs text-slate-400 disabled:opacity-40"
          >
            Previous
          </button>
          <span className="text-xs text-slate-500 self-center">Page {page}</span>
          <button
            disabled={!data.hasNextPage}
            onClick={() => setPage(p => p + 1)}
            className="rounded-lg border border-slate-700 bg-slate-800 px-3 py-1.5 text-xs text-slate-400 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab: Event AI Health
// ─────────────────────────────────────────────────────────────────────────────

function EventHealthTab() {
  const { data, isLoading } = useQuery({
    queryKey: ['ai-studio-event-health'],
    queryFn: () => aiStudioApi.getEventHealth(),
    refetchInterval: 30_000,
  });

  if (isLoading) return <div className="text-slate-500 text-sm">Loading event health…</div>;
  if (!data || data.length === 0) {
    return <div className="text-slate-500 text-sm py-8 text-center">No events found.</div>;
  }

  return (
    <div className="space-y-3">
      {data.map((ev: EventAiHealthResponse) => (
        <div key={ev.eventId} className="rounded-xl border border-slate-800 bg-slate-900 p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="text-sm font-semibold text-slate-100 truncate">{ev.eventName}</span>
                {ev.isIndexComplete ? (
                  <span className="text-xs bg-emerald-900/60 text-emerald-300 rounded-full px-2 py-0.5">
                    Index Complete
                  </span>
                ) : (
                  <span className="text-xs bg-amber-900/60 text-amber-300 rounded-full px-2 py-0.5">
                    Indexing
                  </span>
                )}
              </div>
              {/* Index progress bar */}
              <div className="mt-2 flex items-center gap-2">
                <div className="flex-1 h-1.5 rounded-full bg-slate-800 overflow-hidden">
                  <div
                    className="h-full rounded-full bg-indigo-500 transition-all duration-500"
                    style={{ width: `${ev.indexCompletionPercent}%` }}
                  />
                </div>
                <span className="text-xs text-slate-500 w-12 text-right shrink-0">
                  {ev.indexCompletionPercent.toFixed(0)}%
                </span>
              </div>
              <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                <span>{ev.totalPhotos} photos · {ev.facesIndexed} faces indexed</span>
                {ev.pendingJobs > 0 && <span className="text-amber-400">{ev.pendingJobs} pending</span>}
                {ev.failedJobs > 0 && <span className="text-amber-400">{ev.failedJobs} failed</span>}
                {ev.deadLetteredJobs > 0 && <span className="text-red-400">{ev.deadLetteredJobs} dead-lettered</span>}
              </div>
            </div>
            <div className="text-right shrink-0 space-y-1">
              <div className="text-xs text-slate-500">
                Searches: <span className="text-slate-300">{ev.totalSearches}</span>
              </div>
              <div className="text-xs text-slate-500">
                Success: <span
                  className={ev.searchSuccessRatePercent >= 90 ? 'text-emerald-400' : 'text-amber-400'}
                >
                  {fmtPct(ev.searchSuccessRatePercent)}
                </span>
              </div>
              <div className="text-xs text-slate-500">
                Avg: <span className="text-slate-300">{fmtMs(ev.averageSearchDurationMs)}</span>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab: Analytics
// ─────────────────────────────────────────────────────────────────────────────

function AnalyticsTab() {
  const [window, setWindow] = useState(24);
  const { data, isLoading } = useQuery({
    queryKey: ['ai-studio-analytics', window],
    queryFn: () => aiStudioApi.getAnalytics(window),
    refetchInterval: 60_000,
  });

  if (isLoading || !data) return <div className="text-slate-500 text-sm">Loading analytics…</div>;

  return (
    <div className="space-y-6">
      {/* Time window selector */}
      <div className="flex gap-2">
        {[6, 24, 48, 168].map(h => (
          <button
            key={h}
            onClick={() => setWindow(h)}
            className={`rounded-lg px-3 py-1.5 text-xs font-medium transition-colors ${
              window === h
                ? 'bg-indigo-600 text-white'
                : 'border border-slate-700 bg-slate-800 text-slate-400 hover:text-slate-200'
            }`}
          >
            {h < 24 ? `${h}h` : h === 168 ? '7d' : `${h / 24}d`}
          </button>
        ))}
      </div>

      {/* Key metrics */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="Total Searches" value={data.totalSearches.toLocaleString()} icon={Search} accent="indigo" />
        <MetricCard
          label="Success Rate"
          value={fmtPct(data.successRatePercent)}
          icon={TrendingUp}
          accent={data.successRatePercent >= 90 ? 'emerald' : 'amber'}
        />
        <MetricCard label="Avg Duration" value={fmtMs(data.averageSearchDurationMs)} icon={Zap} accent="cyan" />
        <MetricCard label="Avg Matches" value={data.averageMatchesFound.toFixed(1)} icon={Layers} accent="violet" />
      </div>

      {/* Top events */}
      {data.topEvents.length > 0 && (
        <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
          <h3 className="text-sm font-semibold text-slate-300 mb-3">Top Events by Search Volume</h3>
          <div className="space-y-2">
            {data.topEvents.map((ev, i) => (
              <div key={ev.eventId} className="flex items-center gap-3">
                <span className="text-xs text-slate-600 w-4">{i + 1}</span>
                <span className="flex-1 text-sm text-slate-300 truncate">{ev.eventName}</span>
                <span className="text-xs text-slate-500">{ev.searchCount} searches</span>
                <span
                  className={`text-xs font-medium ${
                    ev.successRatePercent >= 90 ? 'text-emerald-400' : 'text-amber-400'
                  }`}
                >
                  {fmtPct(ev.successRatePercent)}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Main AI Studio Page
// ─────────────────────────────────────────────────────────────────────────────

const TABS = [
  { id: 'overview', label: 'Overview', icon: Activity },
  { id: 'health', label: 'Event Health', icon: Server },
  { id: 'dead-letter', label: 'Failed Jobs', icon: AlertTriangle },
  { id: 'analytics', label: 'Analytics', icon: BarChart3 },
] as const;

type TabId = (typeof TABS)[number]['id'];

export default function AiStudioPage() {
  const [activeTab, setActiveTab] = useState<TabId>('overview');

  return (
    <div className="min-h-screen bg-slate-950 px-4 py-6 md:px-8">
      <div className="mx-auto max-w-7xl space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-indigo-500/10 border border-indigo-500/20 p-2.5">
              <Brain className="h-6 w-6 text-indigo-400" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-100">AI Studio</h1>
              <p className="text-xs text-slate-500 mt-0.5">
                Find My Photos™ · AI Discovery Engine
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-600">ArcFace 512-dim · pgvector HNSW</span>
          </div>
        </div>

        {/* Tab navigation */}
        <div className="flex gap-1 rounded-xl border border-slate-800 bg-slate-900 p-1">
          {TABS.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`relative flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors flex-1 justify-center ${
                activeTab === tab.id
                  ? 'text-slate-100'
                  : 'text-slate-500 hover:text-slate-300'
              }`}
            >
              {activeTab === tab.id && (
                <motion.div
                  layoutId="ai-studio-tab"
                  className="absolute inset-0 rounded-lg bg-slate-800"
                  transition={{ type: 'spring', stiffness: 400, damping: 35 }}
                />
              )}
              <tab.icon className="relative h-4 w-4" />
              <span className="relative hidden sm:inline">{tab.label}</span>
            </button>
          ))}
        </div>

        {/* Tab content */}
        <AnimatePresence mode="wait">
          <motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.15 }}
          >
            {activeTab === 'overview' && <OverviewTab />}
            {activeTab === 'health' && <EventHealthTab />}
            {activeTab === 'dead-letter' && <DeadLetterTab />}
            {activeTab === 'analytics' && <AnalyticsTab />}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}
