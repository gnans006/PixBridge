import { useCallback, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, Wifi, ZoomIn } from 'lucide-react';
import { useParams } from 'react-router-dom';
import { eventsApi } from '../api/events';
import { photosApi } from '../api/photos';
import { Spinner } from '../components/UI/Spinner';
import { useGalleryHub } from '../hooks/useGalleryHub';
import type { NewPhotoEvent } from '../hooks/useGalleryHub';
import type { PhotoResponse } from '../types';
import { formatDateTime } from '../utils/format';

export default function Gallery() {
  const { eventId } = useParams<{ eventId: string }>();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [lightbox, setLightbox] = useState<PhotoResponse | null>(null);

  const { data: eventData } = useQuery({
    queryKey: ['event', eventId],
    queryFn: async () => {
      const response = await eventsApi.getById(eventId!);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  const { data: photosData, isLoading } = useQuery({
    queryKey: ['photos', eventId, page],
    queryFn: async () => {
      const response = await photosApi.getByEvent(eventId!, page, 50);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  const onNewPhoto = useCallback(
    (_photo: NewPhotoEvent) => {
      void queryClient.invalidateQueries({ queryKey: ['photos', eventId, 1] });
      void queryClient.invalidateQueries({ queryKey: ['event', eventId] });
      if (page !== 1) {
        setPage(1);
      }
    },
    [eventId, page, queryClient],
  );

  const { isConnected } = useGalleryHub(eventId ?? null, onNewPhoto);

  return (
    <div className="min-h-screen bg-gray-900">
      <div className="border-b border-gray-800 bg-gray-950 px-6 py-4">
        <div className="mx-auto flex max-w-7xl items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-white">{eventData?.name ?? 'Gallery'}</h1>
            {eventData ? <p className="text-sm text-gray-400">{eventData.photoCount} photos · {eventData.totalSize}</p> : null}
          </div>
          <div className={`flex items-center gap-2 text-sm ${isConnected ? 'text-green-400' : 'text-gray-500'}`}>
            <Wifi className="h-4 w-4" />
            <span>{isConnected ? 'Live' : 'Connecting...'}</span>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-4 py-6">
        {isLoading ? (
          <div className="flex justify-center py-20">
            <Spinner size="lg" />
          </div>
        ) : null}

        {!isLoading && photosData?.items.length === 0 ? (
          <div className="py-20 text-center text-gray-500">
            <p className="text-xl">No photos yet.</p>
            <p className="mt-2 text-sm">Photos will appear here as the photographer uploads them.</p>
          </div>
        ) : null}

        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
          {photosData?.items.map((photo) => (
            <button
              key={photo.id}
              type="button"
              className="group relative aspect-square overflow-hidden rounded-lg bg-gray-800"
              onClick={() => setLightbox(photo)}
            >
              <img
                src={photosApi.getThumbnailUrl(photo.id)}
                alt={photo.fileName}
                className="h-full w-full object-cover transition-transform group-hover:scale-105"
                loading="lazy"
              />
              <div className="absolute inset-0 flex items-center justify-center bg-black/0 opacity-0 transition-all group-hover:bg-black/40 group-hover:opacity-100">
                <ZoomIn className="h-6 w-6 text-white" />
              </div>
            </button>
          ))}
        </div>

        {photosData && photosData.totalPages > 1 ? (
          <div className="mt-8 flex justify-center gap-2">
            <button
              type="button"
              disabled={!photosData.hasPreviousPage}
              onClick={() => setPage((currentPage) => currentPage - 1)}
              className="rounded-lg bg-gray-700 px-4 py-2 text-white transition-colors hover:bg-gray-600 disabled:opacity-40"
            >
              Previous
            </button>
            <span className="px-4 py-2 text-sm text-gray-400">
              Page {photosData.page} of {photosData.totalPages}
            </span>
            <button
              type="button"
              disabled={!photosData.hasNextPage}
              onClick={() => setPage((currentPage) => currentPage + 1)}
              className="rounded-lg bg-gray-700 px-4 py-2 text-white transition-colors hover:bg-gray-600 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        ) : null}
      </div>

      {lightbox ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/95 p-4" onClick={() => setLightbox(null)} role="presentation">
          <div className="relative w-full max-w-5xl" onClick={(event) => event.stopPropagation()} role="presentation">
            <img
              src={lightbox.originalUrl || photosApi.getDownloadUrl(lightbox.id)}
              alt={lightbox.fileName}
              className="max-h-[80vh] w-full rounded-lg object-contain"
            />
            <div className="absolute bottom-0 left-0 right-0 flex items-center justify-between rounded-b-lg bg-gradient-to-t from-black/80 p-4">
              <div>
                <p className="text-sm font-medium text-white">{lightbox.fileName}</p>
                <p className="text-xs text-gray-400">{formatDateTime(lightbox.capturedAt)}</p>
              </div>
              <a
                href={photosApi.getDownloadUrl(lightbox.id)}
                download={lightbox.fileName}
                onClick={(event) => event.stopPropagation()}
                className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm text-white transition-colors hover:bg-primary-700"
              >
                <Download className="h-4 w-4" />
                Download
              </a>
            </div>
            <button
              type="button"
              onClick={() => setLightbox(null)}
              className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/80"
            >
              ✕
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
