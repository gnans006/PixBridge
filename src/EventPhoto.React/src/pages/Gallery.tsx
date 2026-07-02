import { useCallback, useEffect, useMemo, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, CheckSquare, ChevronLeft, ChevronRight, Download, Square, Wifi, X, ZoomIn } from 'lucide-react';
import { useParams } from 'react-router-dom';
import { eventsApi } from '../api/events';
import { photosApi } from '../api/photos';
import { Spinner } from '../components/UI/Spinner';
import { useGalleryHub } from '../hooks/useGalleryHub';
import type { NewPhotoEvent } from '../hooks/useGalleryHub';
import { formatDateTime } from '../utils/format';

export default function Gallery() {
  const { eventId } = useParams<{ eventId: string }>();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [lightboxIndex, setLightboxIndex] = useState(0);
  const [isSelectMode, setIsSelectMode] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());

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

  const photos = useMemo(() => photosData?.items ?? [], [photosData]);
  const lightboxPhoto = lightboxOpen && photos.length > lightboxIndex ? photos[lightboxIndex] : null;

  // Lightbox navigation
  const closeLightbox = useCallback(() => setLightboxOpen(false), []);
  const goPrev = useCallback(() => setLightboxIndex(i => (i - 1 + photos.length) % photos.length), [photos.length]);
  const goNext = useCallback(() => setLightboxIndex(i => (i + 1) % photos.length), [photos.length]);
  const openLightbox = useCallback(
    (id: string) => {
      const index = photos.findIndex(p => p.id === id);
      if (index >= 0) { setLightboxIndex(index); setLightboxOpen(true); }
    },
    [photos],
  );

  useEffect(() => {
    if (!lightboxOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closeLightbox();
      else if (e.key === 'ArrowLeft') goPrev();
      else if (e.key === 'ArrowRight') goNext();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [lightboxOpen, closeLightbox, goPrev, goNext]);

  // Selection helpers
  const toggleSelect = useCallback((id: string) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }, []);
  const selectAll = useCallback(() => setSelected(new Set(photos.map(p => p.id))), [photos]);
  const clearSelection = useCallback(() => setSelected(new Set()), []);
  const exitSelectMode = useCallback(() => { setIsSelectMode(false); setSelected(new Set()); }, []);
  const downloadSelected = useCallback(() => {
    Array.from(selected).forEach((id, i) => {
      setTimeout(() => {
        const a = document.createElement('a');
        a.href = photosApi.getDownloadUrl(id);
        a.download = '';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      }, i * 300);
    });
    exitSelectMode();
  }, [selected, exitSelectMode]);
  const allSelected = photos.length > 0 && selected.size === photos.length;

  return (
    <div className="min-h-screen bg-gray-900">
      {/* Sticky header */}
      <div className="sticky top-0 z-30 border-b border-gray-800 bg-gray-950/95 px-6 py-4 backdrop-blur-sm">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
          <div>
            <h1 className="text-xl font-bold text-white">{eventData?.name ?? 'Gallery'}</h1>
            {eventData ? (
              <p className="text-sm text-gray-400">{eventData.photoCount} photos · {eventData.totalSize}</p>
            ) : null}
          </div>
          <div className="flex items-center gap-3">
            <div className={`flex items-center gap-2 text-sm ${isConnected ? 'text-green-400' : 'text-gray-500'}`}>
              <Wifi className="h-4 w-4" />
              <span className="hidden sm:inline">{isConnected ? 'Live' : 'Connecting…'}</span>
            </div>
            {photos.length > 0 ? (
              isSelectMode ? (
                <button type="button" onClick={exitSelectMode}
                  className="flex items-center gap-1.5 rounded-lg border border-gray-600 px-3 py-1.5 text-sm text-gray-400 transition-colors hover:border-gray-500 hover:text-white">
                  <X className="h-4 w-4" /> Cancel
                </button>
              ) : (
                <button type="button" onClick={() => setIsSelectMode(true)}
                  className="flex items-center gap-1.5 rounded-lg border border-gray-700 px-3 py-1.5 text-sm text-gray-300 transition-colors hover:border-gray-500 hover:text-white">
                  <CheckSquare className="h-4 w-4" /> Select
                </button>
              )
            ) : null}
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-4 py-6">
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner size="lg" /></div>
        ) : null}

        {!isLoading && photos.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-32 text-center">
            <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-gray-800">
              <ZoomIn className="h-8 w-8 text-gray-600" />
            </div>
            <p className="text-xl font-medium text-gray-400">No photos yet</p>
            <p className="mt-2 text-sm text-gray-600">Photos will appear here automatically as the photographer uploads them.</p>
          </div>
        ) : null}

        {photos.length > 0 ? (
          <div className="grid grid-cols-2 gap-1.5 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
            {photos.map(photo => {
              const isChosen = selected.has(photo.id);
              return (
                <button key={photo.id} type="button"
                  onClick={() => (isSelectMode ? toggleSelect(photo.id) : openLightbox(photo.id))}
                  className={[
                    'group relative aspect-square overflow-hidden rounded-lg bg-gray-800 transition-all',
                    isSelectMode && isChosen ? 'scale-[0.96] ring-2 ring-primary-500 ring-offset-2 ring-offset-gray-900' : '',
                    isSelectMode && !isChosen ? 'opacity-70 hover:opacity-100' : '',
                    !isSelectMode ? 'hover:brightness-110' : '',
                  ].filter(Boolean).join(' ')}
                >
                  <img src={photosApi.getThumbnailUrl(photo.id)} alt={photo.fileName}
                    className="h-full w-full object-cover transition-transform group-hover:scale-105" loading="lazy" />
                  {!isSelectMode ? (
                    <div className="absolute inset-0 flex items-center justify-center bg-black/0 opacity-0 transition-all group-hover:bg-black/40 group-hover:opacity-100">
                      <ZoomIn className="h-6 w-6 text-white drop-shadow-md" />
                    </div>
                  ) : null}
                  {isSelectMode ? (
                    <div className={['absolute right-2 top-2 flex h-5 w-5 items-center justify-center rounded-full border-2 shadow transition-all',
                      isChosen ? 'border-primary-500 bg-primary-500' : 'border-white/80 bg-black/30'].join(' ')}>
                      {isChosen ? <Check className="h-3 w-3 text-white" strokeWidth={3} /> : null}
                    </div>
                  ) : null}
                </button>
              );
            })}
          </div>
        ) : null}

        {photosData && photosData.totalPages > 1 ? (
          <div className="mt-8 flex justify-center gap-2">
            <button type="button" disabled={!photosData.hasPreviousPage} onClick={() => setPage(p => p - 1)}
              className="flex items-center gap-1 rounded-lg bg-gray-700 px-4 py-2 text-sm text-white transition-colors hover:bg-gray-600 disabled:opacity-40">
              <ChevronLeft className="h-4 w-4" /> Previous
            </button>
            <span className="px-4 py-2 text-sm text-gray-400">{photosData.page} / {photosData.totalPages}</span>
            <button type="button" disabled={!photosData.hasNextPage} onClick={() => setPage(p => p + 1)}
              className="flex items-center gap-1 rounded-lg bg-gray-700 px-4 py-2 text-sm text-white transition-colors hover:bg-gray-600 disabled:opacity-40">
              Next <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        ) : null}

        {isSelectMode ? <div className="h-20" /> : null}
      </div>

      {/* Selection toolbar — fixed bottom */}
      {isSelectMode ? (
        <div className="fixed bottom-0 left-0 right-0 z-40 border-t border-gray-700 bg-gray-950/95 backdrop-blur-sm">
          <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-6 py-3">
            <div className="flex items-center gap-4 text-sm">
              <span className="font-medium text-white">
                {selected.size} {selected.size === 1 ? 'photo' : 'photos'} selected
              </span>
              <button type="button" onClick={allSelected ? clearSelection : selectAll}
                className="flex items-center gap-1 text-primary-400 transition-colors hover:text-primary-300">
                {allSelected
                  ? <><Square className="h-4 w-4" /> Deselect all</>
                  : <><CheckSquare className="h-4 w-4" /> Select all</>}
              </button>
            </div>
            <div className="flex gap-2">
              <button type="button" onClick={exitSelectMode}
                className="rounded-lg border border-gray-700 px-4 py-2 text-sm text-gray-400 transition-colors hover:text-white">
                Cancel
              </button>
              <button type="button" onClick={downloadSelected} disabled={selected.size === 0}
                className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-40">
                <Download className="h-4 w-4" />
                Download ({selected.size})
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {/* Lightbox */}
      {lightboxPhoto ? (
        <div role="presentation" className="fixed inset-0 z-50 flex items-center bg-black/95" onClick={closeLightbox}>
          {photos.length > 1 ? (
            <button type="button" onClick={e => { e.stopPropagation(); goPrev(); }}
              className="absolute left-3 top-1/2 z-10 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/60 text-white transition-colors hover:bg-black/90">
              <ChevronLeft className="h-5 w-5" />
            </button>
          ) : null}

          <div role="presentation" className="mx-14 flex w-full flex-col items-center gap-4" onClick={e => e.stopPropagation()}>
            <img
              src={lightboxPhoto.originalUrl || photosApi.getDownloadUrl(lightboxPhoto.id)}
              alt={lightboxPhoto.fileName}
              className="max-h-[78vh] max-w-full rounded-xl object-contain shadow-2xl"
            />
            <div className="flex w-full max-w-2xl items-center justify-between rounded-xl bg-gray-900/80 px-4 py-3 backdrop-blur-sm">
              <div>
                <p className="text-sm font-medium text-white">{lightboxPhoto.fileName}</p>
                <p className="text-xs text-gray-400">
                  {formatDateTime(lightboxPhoto.capturedAt)} · {lightboxIndex + 1} / {photos.length}
                </p>
              </div>
              <a href={photosApi.getDownloadUrl(lightboxPhoto.id)} download={lightboxPhoto.fileName}
                onClick={e => e.stopPropagation()}
                className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700">
                <Download className="h-4 w-4" /> Download
              </a>
            </div>
          </div>

          <button type="button" onClick={closeLightbox}
            className="absolute right-4 top-4 z-20 flex h-9 w-9 items-center justify-center rounded-full bg-black/60 text-white transition-colors hover:bg-black/90">
            <X className="h-4 w-4" />
          </button>

          {photos.length > 1 ? (
            <button type="button" onClick={e => { e.stopPropagation(); goNext(); }}
              className="absolute right-3 top-1/2 z-10 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/60 text-white transition-colors hover:bg-black/90">
              <ChevronRight className="h-5 w-5" />
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
