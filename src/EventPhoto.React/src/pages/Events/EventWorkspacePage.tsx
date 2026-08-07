import { useCallback, useState } from 'react';
import { useParams, Navigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { AnimatePresence, motion } from 'framer-motion';
import {
  Activity,
  Brain,
  Camera,
  HardDrive,
  Info,
  Loader2,
  QrCode,
  Settings,
  Shield,
} from 'lucide-react';
import { useGalleryHub } from '../../hooks/useGalleryHub';
import { workspaceApi } from '../../api/workspace';
import { EventHeader } from '../../components/event-workspace/EventHeader';
import { EventOverviewTab } from '../../components/event-workspace/EventOverviewTab';
import { EventGalleryTab } from '../../components/event-workspace/EventGalleryTab';
import { EventFaceRecognitionTab } from '../../components/event-workspace/EventFaceRecognitionTab';
import { EventWatermarkTab } from '../../components/event-workspace/EventWatermarkTab';
import { EventQrAccessTab } from '../../components/event-workspace/EventQrAccessTab';
import { EventAnalyticsTab } from '../../components/event-workspace/EventAnalyticsTab';
import { EventStorageTab } from '../../components/event-workspace/EventStorageTab';

type TabId = 'overview' | 'gallery' | 'face-recognition' | 'watermark' | 'qr-access' | 'analytics' | 'storage';

const TABS: { id: TabId; label: string; icon: React.ReactNode }[] = [
  { id: 'overview', label: 'Overview', icon: <Info className="h-4 w-4" /> },
  { id: 'gallery', label: 'Gallery', icon: <Camera className="h-4 w-4" /> },
  { id: 'face-recognition', label: 'Face Recognition', icon: <Brain className="h-4 w-4" /> },
  { id: 'watermark', label: 'Watermark', icon: <Shield className="h-4 w-4" /> },
  { id: 'qr-access', label: 'QR Access', icon: <QrCode className="h-4 w-4" /> },
  { id: 'analytics', label: 'Analytics', icon: <Activity className="h-4 w-4" /> },
  { id: 'storage', label: 'Storage', icon: <HardDrive className="h-4 w-4" /> },
];

export default function EventWorkspacePage() {
  const { eventId } = useParams<{ eventId: string }>();
  const [activeTab, setActiveTab] = useState<TabId>('overview');
  const queryClient = useQueryClient();

  // ── Invalidate all event-related queries when photos change ─────────────────
  const invalidatePhotoQueries = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['workspace', eventId] });
    void queryClient.invalidateQueries({ queryKey: ['analytics', eventId] });
    void queryClient.invalidateQueries({ queryKey: ['storage', eventId] });
    void queryClient.invalidateQueries({ queryKey: ['events'] });
    void queryClient.invalidateQueries({ queryKey: ['dashboard-overview'] });
    void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
  }, [eventId, queryClient]);

  // Real-time photo count updates via SignalR
  useGalleryHub(eventId ?? null, invalidatePhotoQueries, invalidatePhotoQueries);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['workspace', eventId],
    queryFn: () => workspaceApi.getWorkspace(eventId!),
    enabled: !!eventId,
    staleTime: 15_000,
    refetchInterval: 30_000,   // poll as fallback when SignalR is unavailable
  });

  const workspace = data?.data;

  if (!eventId) return <Navigate to="/admin/events" replace />;

  if (isLoading) {
    return (
      <div className="-m-4 min-h-full bg-slate-950 sm:-m-6">
        <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6">
          {/* Header skeleton */}
          <div className="mb-6 h-52 animate-pulse rounded-2xl bg-slate-900" />
          {/* Tab skeleton */}
          <div className="mb-6 flex gap-2">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="h-9 w-24 animate-pulse rounded-xl bg-slate-800" />
            ))}
          </div>
          {/* Content skeleton */}
          <div className="h-96 animate-pulse rounded-2xl bg-slate-900" />
        </div>
      </div>
    );
  }

  if (isError || !workspace) {
    return (
      <div className="-m-4 min-h-full bg-slate-950 sm:-m-6">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-center px-4 py-24 sm:px-6">
          <p className="text-lg font-semibold text-slate-300">Event not found</p>
          <p className="mt-1 text-sm text-slate-500">The event may have been deleted or you don't have access.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="-m-4 min-h-full bg-slate-950 sm:-m-6">
      <div className="mx-auto max-w-7xl space-y-5 px-4 pb-16 pt-4 sm:px-6">

        {/* Page header */}
        <EventHeader
          workspace={workspace}
          onNavigateTab={(tab) => setActiveTab(tab as TabId)}
        />

        {/* Tab bar */}
        <div className="scrollbar-none -mx-1 overflow-x-auto px-1">
          <div className="flex gap-1 rounded-2xl border border-slate-800 bg-slate-900 p-1.5 w-fit">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={[
                  'relative flex items-center gap-2 rounded-xl px-3.5 py-2 text-sm font-medium transition-colors whitespace-nowrap',
                  activeTab === tab.id
                    ? 'text-white'
                    : 'text-slate-500 hover:text-slate-300',
                ].join(' ')}
              >
                {activeTab === tab.id && (
                  <motion.div
                    layoutId="tab-indicator"
                    className="absolute inset-0 rounded-xl bg-slate-700"
                    transition={{ type: 'spring', stiffness: 380, damping: 32 }}
                  />
                )}
                <span className="relative z-10 flex items-center gap-2">
                  {tab.icon}
                  {tab.label}
                </span>
              </button>
            ))}
          </div>
        </div>

        {/* Tab content */}
        <AnimatePresence mode="wait">
          <motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -4 }}
            transition={{ duration: 0.2 }}
          >
            {activeTab === 'overview' && <EventOverviewTab workspace={workspace} />}
            {activeTab === 'gallery' && <EventGalleryTab workspace={workspace} />}
            {activeTab === 'face-recognition' && <EventFaceRecognitionTab workspace={workspace} />}
            {activeTab === 'watermark' && <EventWatermarkTab workspace={workspace} />}
            {activeTab === 'qr-access' && <EventQrAccessTab workspace={workspace} />}
            {activeTab === 'analytics' && <EventAnalyticsTab workspace={workspace} />}
            {activeTab === 'storage' && <EventStorageTab workspace={workspace} />}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}
