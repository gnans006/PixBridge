import { useQuery } from '@tanstack/react-query';
import { ScanFace } from 'lucide-react';
import { dashboardApi } from '../../api/dashboard';

function ProgressBar({ value, max, color = 'bg-indigo-500' }: { value: number; max: number; color?: string }) {
  const pct = max > 0 ? Math.min((value / max) * 100, 100) : 0;
  return (
    <div className="h-1.5 w-full overflow-hidden rounded-full bg-slate-800">
      <div className={`h-full rounded-full transition-all duration-700 ${color}`} style={{ width: `${pct}%` }} />
    </div>
  );
}

export function FaceRecognitionAnalytics() {
  const { data, isLoading } = useQuery({
    queryKey: ['face-analytics'],
    queryFn: () => dashboardApi.getFaceAnalytics(),
    refetchInterval: 60_000,
    select: (r) => r.data,
  });

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className="mb-4 flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-500/20">
          <ScanFace className="h-4 w-4 text-amber-400" />
        </div>
        <h3 className="text-sm font-semibold text-slate-100">Face Recognition</h3>
      </div>

      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3].map((i) => <div key={i} className="h-10 rounded-lg bg-slate-800" />)}
        </div>
      ) : (
        <>
          <div className="mb-4 grid grid-cols-3 gap-3">
            <div className="rounded-xl bg-slate-800/60 p-3 text-center">
              <p className="text-lg font-bold text-amber-400">{(data?.totalIndexedFaces ?? 0).toLocaleString()}</p>
              <p className="text-xs text-slate-400">Indexed Faces</p>
            </div>
            <div className="rounded-xl bg-slate-800/60 p-3 text-center">
              <p className="text-lg font-bold text-slate-100">{data?.eventsWithFaceSearch ?? 0}</p>
              <p className="text-xs text-slate-400">AI Events</p>
            </div>
            <div className="rounded-xl bg-slate-800/60 p-3 text-center">
              <p className="text-lg font-bold text-rose-400">{data?.totalPendingPhotos ?? 0}</p>
              <p className="text-xs text-slate-400">Pending</p>
            </div>
          </div>

          {data?.eventBreakdown.length ? (
            <div className="space-y-3">
              {data.eventBreakdown.map((item) => (
                <div key={item.eventId}>
                  <div className="mb-1 flex items-center justify-between">
                    <span className="text-xs text-slate-300 truncate max-w-[160px]">{item.eventName}</span>
                    <span className="text-xs font-medium text-amber-400">{item.faceEmbeddings.toLocaleString()} faces</span>
                  </div>
                  <ProgressBar value={item.faceEmbeddings} max={Math.max(...data.eventBreakdown.map(e => e.faceEmbeddings), 1)} color="bg-amber-500" />
                </div>
              ))}
            </div>
          ) : (
            <p className="text-center text-xs text-slate-500 py-4">No face-search events configured</p>
          )}
        </>
      )}
    </div>
  );
}
