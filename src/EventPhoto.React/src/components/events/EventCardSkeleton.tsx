interface EventCardSkeletonProps {
  view?: 'gallery' | 'compact';
}

export function EventCardSkeleton({ view = 'gallery' }: EventCardSkeletonProps) {
  if (view === 'compact') {
    return (
      <div className="flex items-center gap-4 rounded-2xl border border-slate-800 bg-slate-900 p-4">
        <div className="h-14 w-20 shrink-0 animate-pulse rounded-xl bg-slate-800" />
        <div className="flex-1 space-y-2">
          <div className="h-4 w-2/3 animate-pulse rounded-lg bg-slate-800" />
          <div className="h-3 w-2/5 animate-pulse rounded-lg bg-slate-800/70" />
          <div className="h-3 w-1/3 animate-pulse rounded-lg bg-slate-800/50" />
        </div>
        <div className="h-7 w-7 animate-pulse rounded-lg bg-slate-800" />
      </div>
    );
  }

  return (
    <div className="flex flex-col overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
      {/* Cover shimmer */}
      <div className="h-44 shimmer-effect" />

      {/* Body */}
      <div className="space-y-3 p-4">
        <div className="h-4 w-3/4 animate-pulse rounded-lg bg-slate-800" />
        <div className="h-3 w-1/2 animate-pulse rounded-lg bg-slate-800/70" />
        <div className="flex gap-1.5">
          <div className="h-5 w-16 animate-pulse rounded-full bg-slate-800/60" />
          <div className="h-5 w-12 animate-pulse rounded-full bg-slate-800/40" />
        </div>
        <div className="flex gap-3 border-t border-slate-800 pt-3">
          <div className="h-3 w-1/4 animate-pulse rounded-lg bg-slate-800/50" />
          <div className="h-3 w-1/4 animate-pulse rounded-lg bg-slate-800/50" />
        </div>
      </div>
    </div>
  );
}
