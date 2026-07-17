import { useParams } from 'react-router-dom';
import { useInfiniteQuery } from '@tanstack/react-query';
import { faceSearchApi, type FaceSearchMatch } from '../api/faceSearch';

/**
 * MyPhotosGalleryPage — displays matched photos for a completed face-search session.
 * Supports infinite scroll loading, thumbnail view, and contextual download button.
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

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p className="text-gray-500">Loading your photos...</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p className="text-red-500">Session expired or not found. Please search again.</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 px-4 py-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-gray-800">Your Photos</h1>
          <p className="text-gray-500 text-sm mt-1">
            {totalMatches === 0
              ? 'No photos matched your selfie.'
              : `Found ${totalMatches} photo${totalMatches !== 1 ? 's' : ''} of you`}
          </p>
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
                  className="px-6 py-2 rounded-lg bg-indigo-600 text-white text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  {isFetchingNextPage ? 'Loading...' : 'Load More'}
                </button>
              </div>
            )}
          </>
        ) : (
          <div className="text-center py-16 text-gray-400">
            <div className="text-5xl mb-4">😔</div>
            <p>We couldn't find any photos of you in this event.</p>
            <p className="text-sm mt-2">Try uploading a clearer selfie with good lighting.</p>
          </div>
        )}
      </div>
    </div>
  );
}

function PhotoCard({ match }: { match: FaceSearchMatch }) {
  const pct = Math.round(match.similarityScore * 100);

  return (
    <div className="relative group rounded-xl overflow-hidden shadow-sm bg-white">
      <img
        src={match.thumbnailUrl}
        alt={match.fileName}
        className="w-full aspect-square object-cover"
        loading="lazy"
      />
      {/* Similarity badge */}
      <div className="absolute top-1 right-1 bg-black/60 text-white text-xs px-1.5 py-0.5 rounded-full">
        {pct}%
      </div>
      {/* Download overlay */}
      <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition flex items-end justify-center pb-3 opacity-0 group-hover:opacity-100">
        <a
          href={match.downloadUrl}
          download={match.fileName}
          className="bg-white text-gray-800 text-xs font-semibold px-3 py-1.5 rounded-full shadow hover:bg-gray-100"
          onClick={e => e.stopPropagation()}
        >
          ⬇ Download
        </a>
      </div>
    </div>
  );
}
