/** Skeleton shimmers for the Create Event page. Never use spinners. */
export function CreateEventSkeleton() {
  return (
    <div className="min-h-screen bg-slate-950 px-4 py-8 sm:px-6">
      <div className="mx-auto max-w-[1400px]">
        {/* Back + title */}
        <div className="mb-8 space-y-3">
          <div className="shimmer-effect h-4 w-24 rounded-lg" />
          <div className="shimmer-effect h-8 w-48 rounded-xl" />
          <div className="shimmer-effect h-4 w-72 rounded-lg" />
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_340px]">
          {/* Left column */}
          <div className="space-y-4">
            {[176, 128, 96, 80, 72, 72].map((h, i) => (
              <div key={i} className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
                <div className="flex items-center gap-3 px-5 py-4">
                  <div className="shimmer-effect h-9 w-9 rounded-xl" />
                  <div className="space-y-1.5">
                    <div className="shimmer-effect h-4 w-32 rounded-lg" />
                    <div className="shimmer-effect h-3 w-48 rounded-lg" />
                  </div>
                </div>
                <div className="border-t border-slate-800 px-5 pb-5 pt-4">
                  <div className={`shimmer-effect w-full rounded-xl`} style={{ height: h }} />
                </div>
              </div>
            ))}
          </div>

          {/* Right sidebar */}
          <EventSummarySkeleton />
        </div>
      </div>
    </div>
  );
}

export function EventSummarySkeleton() {
  return (
    <div className="space-y-4">
      <div className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
        <div className="shimmer-effect h-36 w-full" />
        <div className="space-y-3 p-4">
          {[48, 48, 48].map((h, i) => (
            <div key={i} className={`shimmer-effect w-full rounded-xl`} style={{ height: h }} />
          ))}
          <div className="shimmer-effect h-6 w-32 rounded-full" />
        </div>
        <div className="border-t border-slate-800 px-4 py-3">
          <div className="shimmer-effect h-5 w-40 rounded-lg" />
        </div>
      </div>
      <div className="shimmer-effect h-28 w-full rounded-2xl" />
    </div>
  );
}

export function WatermarkPreviewSkeleton() {
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div className="shimmer-effect h-4 w-24 rounded-lg" />
        <div className="shimmer-effect h-5 w-28 rounded-full" />
      </div>
      <div className="shimmer-effect h-64 w-full rounded-xl" />
      <div className="shimmer-effect h-20 w-full rounded-xl" />
    </div>
  );
}
