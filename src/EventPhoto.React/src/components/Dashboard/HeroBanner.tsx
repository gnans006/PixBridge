import { useQuery } from '@tanstack/react-query';
import { CalendarCheck, Clock, PlusCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { dashboardApi } from '../../api/dashboard';
import { authStore } from '../../store/authStore';
import { useCountUp } from '../../hooks/useCountUp';

function StatPill({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="flex flex-col items-center rounded-xl bg-white/10 px-4 py-2 backdrop-blur-sm">
      <span className="text-xl font-bold text-white">{value}</span>
      <span className="mt-0.5 text-xs font-medium text-indigo-200">{label}</span>
    </div>
  );
}

/** Shown while the overview data is being fetched for the first time. */
function StatPillSkeleton({ width = 'w-8' }: { width?: string }) {
  return (
    <div className="flex flex-col items-center rounded-xl bg-white/10 px-4 py-2 backdrop-blur-sm">
      <div className={`h-7 ${width} animate-pulse rounded-md bg-white/25`} />
      <div className="mt-1.5 h-2.5 w-16 animate-pulse rounded bg-white/15" />
    </div>
  );
}

export function HeroBanner() {
  const user = authStore.getUser();
  const now = new Date();
  const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  const dateStr = now.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-overview'],
    queryFn: () => dashboardApi.getOverview(),
    refetchInterval: 30_000,
    select: (r) => r.data,
  });

  // Initialise at the current value (not 0) so a remount with warm cache
  // never flashes back to zero before animating upward.
  const activeEvents   = useCountUp(data?.activeEvents   ?? 0);
  const totalPhotos    = useCountUp(data?.totalPhotos    ?? 0);
  const downloadsToday = useCountUp(data?.downloadsToday ?? 0);
  const totalDownloads = useCountUp(data?.totalDownloads ?? 0);

  return (
    <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 via-indigo-600 to-purple-700 p-6 sm:p-8">
      {/* Decorative blobs */}
      <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/5 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-12 -left-12 h-48 w-48 rounded-full bg-purple-400/10 blur-2xl" />

      {/* Header row */}
      <div className="relative flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="mb-1 flex items-center gap-2 text-indigo-200">
            <Clock className="h-4 w-4" />
            <span className="text-sm font-medium">{timeStr} · {dateStr}</span>
          </div>
          <h1 className="text-2xl font-bold text-white sm:text-3xl">
            Welcome back,{' '}
            {isLoading ? (
              <span className="inline-block h-8 w-28 animate-pulse rounded-lg bg-white/20 align-middle" />
            ) : (
              <span className="text-violet-200">{user?.username ?? 'Admin'}</span>
            )}
          </h1>
          <p className="mt-1 text-sm text-indigo-200">Your studio command centre is ready.</p>
        </div>

        <Link
          to="/admin/events/new"
          className="inline-flex w-fit items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-indigo-700 shadow-lg transition-all hover:bg-indigo-50 hover:shadow-xl active:scale-95"
        >
          <PlusCircle className="h-4 w-4" />
          New Event
        </Link>
      </div>

      {/* Stat pills row */}
      <div className="relative mt-6 flex flex-wrap gap-3">
        {isLoading ? (
          <>
            <StatPillSkeleton width="w-6"  />
            <StatPillSkeleton width="w-12" />
            <StatPillSkeleton width="w-6"  />
            <StatPillSkeleton width="w-14" />
          </>
        ) : (
          <>
            <StatPill label="Active Events"    value={activeEvents} />
            <StatPill label="Total Photos"     value={totalPhotos.toLocaleString()} />
            <StatPill label="Downloads Today"  value={downloadsToday} />
            <StatPill label="Total Downloads"  value={totalDownloads.toLocaleString()} />
            {data && (
              <div className="ml-auto flex items-center gap-1.5 rounded-xl bg-white/10 px-3 py-2 backdrop-blur-sm">
                <CalendarCheck className="h-4 w-4 text-emerald-300" />
                <span className="text-xs font-medium text-indigo-100">
                  {data.eventsWithFaceSearch} face-search · {data.eventsWithWatermark} watermarked
                </span>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
