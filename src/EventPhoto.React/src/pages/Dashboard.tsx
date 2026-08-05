import { HeroBanner } from '../components/Dashboard/HeroBanner';
import { EventSpotlight } from '../components/Dashboard/EventSpotlight';
import { StatsGrid } from '../components/Dashboard/StatsGrid';
import { QuickActions } from '../components/Dashboard/QuickActions';
import { EventGallery } from '../components/Dashboard/EventGallery';
import { FaceRecognitionAnalytics } from '../components/Dashboard/FaceRecognitionAnalytics';
import { WatermarkAnalytics } from '../components/Dashboard/WatermarkAnalytics';
import { StorageAnalytics } from '../components/Dashboard/StorageAnalytics';
import { RecentActivityTimeline } from '../components/Dashboard/RecentActivityTimeline';
import { SystemHealthPanel } from '../components/Dashboard/SystemHealthPanel';
import { LiveConnectionsPanel } from '../components/Dashboard/LiveConnectionsPanel';
import { ProcessingStatusPanel } from '../components/Dashboard/ProcessingStatusPanel';

export default function Dashboard() {
  return (
    <div className="-m-4 sm:-m-6 min-h-full bg-slate-950 px-4 py-6 sm:px-6">
      <div className="mx-auto max-w-screen-2xl space-y-6">

        {/* ── 1. Hero Banner ──────────────────────────────────────────── */}
        <HeroBanner />

        {/* ── 2. Event Spotlight ──────────────────────────────────────── */}
        <EventSpotlight />

        {/* ── 3. KPI Stats Grid ───────────────────────────────────────── */}
        <StatsGrid />

        {/* ── 4. Quick Actions ────────────────────────────────────────── */}
        <QuickActions />

        {/* ── 5. Event Gallery ────────────────────────────────────────── */}
        <EventGallery />

        {/* ── 6 & 7. AI + Watermark Analytics ────────────────────────── */}
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <FaceRecognitionAnalytics />
          <WatermarkAnalytics />
        </div>

        {/* ── 8. Storage Analytics ────────────────────────────────────── */}
        <StorageAnalytics />

        {/* ── 9, 10, 11, 12. Activity + Health + Live + Processing ────── */}
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
          {/* Activity timeline — wider */}
          <div className="lg:col-span-2">
            <RecentActivityTimeline />
          </div>
          {/* Right panels */}
          <div className="space-y-6 lg:col-span-3">
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
              <SystemHealthPanel />
              <LiveConnectionsPanel />
              <ProcessingStatusPanel />
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}

