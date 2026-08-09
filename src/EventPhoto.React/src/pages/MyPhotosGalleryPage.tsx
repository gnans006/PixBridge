import { useParams } from 'react-router-dom';
import { useInfiniteQuery } from '@tanstack/react-query';
import { faceSearchApi, type FaceSearchMatch } from '../api/faceSearch';
import { Download, CheckCircle2 } from 'lucide-react';

/**
 * MyPhotosGalleryPage — "Find My Photos™" results gallery.
 * Displays matched photos with confidence categories (not raw scores).
 * Supports infinite scroll, bulk download, and premium UI experience.
 */
export default function MyPhotosGalleryPage() {
  const { sessionToken } = useParams<{ sessionToken: string }>();
  const PAGE_SIZE = 50;

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
  } = useInfiniteQuery({
    queryKey: ['myPhotos', sessionToken],
    queryFn: ({ pageParam = 1 }) =>
      faceSearchApi.getResults(sessionToken!, pageParam, PAGE_SIZE).then(r => r.data.data!),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
    enabled: !!sessionToken,
  });

  const allMatches = data?.pages.flatMap(p => p.matches) ?? [];
  const totalMatches = data?.pages[0]?.totalMatches ?? 0;
  const searchDurationMs = data?.pages[0]?.searchDurationMs ?? 0;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-slate-950">
        <div className="text-center">
          <div className="h-10 w-10 rounded-full border-2 border-indigo-500 border-t-transparent animate-spin mx-auto mb-4" />
          <p className="text-slate-400 text-sm">AI is finding your photos…</p>
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-slate-950">
        <p className="text-red-400">Session expired or not found. Please search again.</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-950 px-4 py-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-8 text-center">
          {totalMatches > 0 ? (
            <>
              <div className="inline-flex items-center gap-2 rounded-full bg-emerald-500/10 border border-emerald-500/20 px-4 py-1.5 mb-4">
                <CheckCircle2 className="h-4 w-4 text-emerald-400" />
                <span className="text-sm text-emerald-400 font-medium">
                  AI Found Your Photos
                  {searchDurationMs > 0 && ` in ${(searchDurationMs / 1000).toFixed(1)}s`}
                </span>
              </div>
              <h1 className="text-3xl font-bold text-slate-100">
                {totalMatches} Photo{totalMatches !== 1 ? 's' : ''} Found
              </h1>
              <p className="text-slate-500 text-sm mt-2">
                Tap any photo to download. Tap "Download All" to save everything at once.
              </p>
              {/* Download all */}
              {allMatches.length > 0 && (
                <div className="mt-4 flex justify-center gap-3">
                  <a
                    href={`/api/photos/bulk-download?sessionToken=${sessionToken}`}
                    className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 hover:bg-indigo-500 px-5 py-2.5 text-sm font-semibold text-white transition-colors"
                  >
                    <Download className="h-4 w-4" />
                    Download All
                  </a>
                </div>
              )}
            </>
          ) : (
            <>
              <h1 className="text-2xl font-bold text-slate-100">No Photos Found</h1>
              <p className="text-slate-500 text-sm mt-2">
                We couldn't find any photos of you in this event. Try a clearer selfie with good lighting.
              </p>
            </>
          )}
        </div>

        {/* Grid */}
        {allMatches.length > 0 ? (
          <>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
              {allMatches.map((match: FaceSearchMatch) => (
                <PhotoCard key={match.photoId} match={match} />
              ))}
            </div>

            {/* Load more */}
            {hasNextPage && (
              <div className="mt-8 flex justify-center">
                <button
                  onClick={() => fetchNextPage()}
                  disabled={isFetchingNextPage}
                  className="px-6 py-2.5 rounded-xl bg-slate-800 border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 transition-colors"
                >
                  {isFetchingNextPage ? 'Loading…' : 'Load More Photos'}
                </button>
              </div>
            )}
          </>
        ) : (
          <div className="text-center py-16">
            <div className="text-5xl mb-4">🔍</div>
            <p className="text-slate-400">Try uploading a clearer selfie with good lighting.</p>
          </div>
        )}
      </div>
    </div>
  );
}

// Confidence badge styles — guests see labels, never raw numbers
const CONFIDENCE_STYLES: Record<string, { badge: string; dot: string }> = {
  Excellent: { badge: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30', dot: 'bg-emerald-400' },
  Strong: { badge: 'bg-indigo-500/20 text-indigo-400 border-indigo-500/30', dot: 'bg-indigo-400' },
  Possible: { badge: 'bg-slate-700 text-slate-400 border-slate-600', dot: 'bg-slate-500' },
};

function PhotoCard({ match }: { match: FaceSearchMatch }) {
  const category = (match as any).confidenceCategory ?? 'Possible';
  const label = (match as any).confidenceLabel ?? 'Possible Match';
  const styles = CONFIDENCE_STYLES[category] ?? CONFIDENCE_STYLES.Possible;

  return (
    <div className="relative group rounded-xl overflow-hidden bg-slate-900 border border-slate-800">
      <img
        src={match.thumbnailUrl}
        alt={match.fileName}
        className="w-full aspect-square object-cover"
        loading="lazy"
      />

      {/* Confidence badge — NO raw percentage */}
      <div className={`absolute top-2 left-2 flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-xs font-medium ${styles.badge}`}>
        <span className={`h-1.5 w-1.5 rounded-full ${styles.dot}`} />
        {label}
      </div>

      {/* Download overlay */}
      <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-3">
        <a
          href={match.downloadUrl}
          download={match.fileName}
          className="flex items-center gap-1.5 w-full justify-center rounded-lg bg-white/90 hover:bg-white text-slate-900 text-xs font-semibold px-3 py-2 transition-colors"
          onClick={e => e.stopPropagation()}
        >
          <Download className="h-3 w-3" />
          Download
        </a>
      </div>
    </div>
  );
}

