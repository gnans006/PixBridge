import { EventCardSkeleton } from './EventCardSkeleton';

interface EventGallerySkeletonProps {
  view?: 'gallery' | 'compact';
  count?: number;
}

export function EventGallerySkeleton({ view = 'gallery', count = 8 }: EventGallerySkeletonProps) {
  if (view === 'compact') {
    return (
      <div className="space-y-3">
        {Array.from({ length: count }, (_, i) => (
          <EventCardSkeleton key={i} view="compact" />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }, (_, i) => (
        <EventCardSkeleton key={i} view="gallery" />
      ))}
    </div>
  );
}
