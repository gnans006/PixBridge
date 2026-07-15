import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, CheckSquare, ChevronLeft, ChevronRight, Download, Square, Wifi, X, ZoomIn } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { eventsApi } from '../api/events';
import { photosApi } from '../api/photos';
import { Spinner } from '../components/UI/Spinner';
import { useGalleryHub } from '../hooks/useGalleryHub';
import type { DeletedPhotoEvent, NewPhotoEvent } from '../hooks/useGalleryHub';
import { formatDateTime } from '../utils/format';

export default function Gallery() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
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
    refetchInterval: 15_000,
  });

  // When galleryRecentCount is configured, always show page 1 with that page size so the
  // server enforces the limit (photos are ordered by captured_at DESC).
  const effectivePageSize = eventData?.galleryRecentCount ?? 50;
  const isRecentCountMode = Boolean(eventData?.galleryRecentCount);

  const { data: photosData, isLoading } = useQuery({
    queryKey: ['photos', eventId, page, effectivePageSize],
    queryFn: async () => {
      const response = await photosApi.getByEvent(eventId!, isRecentCountMode ? 1 : page, effectivePageSize);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  const onNewPhoto = useCallback(
    (_photo: NewPhotoEvent) => {
      void queryClient.invalidateQueries({ queryKey: ['photos', eventId] });
      void queryClient.invalidateQueries({ queryKey: ['event', eventId] });
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
      if (page !== 1) {
        setPage(1);
      }
    },
    [eventId, page, queryClient],
  );

  const onPhotoDeleted = useCallback(
    (_event: DeletedPhotoEvent) => {
      void queryClient.invalidateQueries({ queryKey: ['photos', eventId] });
      void queryClient.invalidateQueries({ queryKey: ['event', eventId] });
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
    },
    [eventId, queryClient],
  );

  const { isConnected } = useGalleryHub(eventId ?? null, onNewPhoto, onPhotoDeleted);

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
    Array.from(selected).forEach((id) => {
      window.open(photosApi.getDownloadUrl(id), '_blank');
    });
    exitSelectMode();
  }, [selected, exitSelectMode]);
  const allSelected = photos.length > 0 && selected.size === photos.length;

  // Long press (mobile) — enter select mode and select the pressed photo
  const longPressTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const didLongPress = useRef(false);

  // Swipe support for lightbox
  const swipeTouchStartX = useRef<number | null>(null);

  const handleTouchStart = useCallback((id: string) => {
    didLongPress.current = false;
    longPressTimer.current = setTimeout(() => {
      didLongPress.current = true;
      setIsSelectMode(true);
      setSelected(new Set([id]));
      if (navigator.vibrate) navigator.vibrate(50);
    }, 500);
  }, []);

  const handleTouchEnd = useCallback(() => {
    if (longPressTimer.current) {
      clearTimeout(longPressTimer.current);
      longPressTimer.current = null;
    }
  }, []);

  const handleTouchMove = useCallback(() => {
    if (longPressTimer.current) {
      clearTimeout(longPressTimer.current);
      longPressTimer.current = null;
    }
  }, []);

  return (
    <div className="min-h-screen bg-gray-900">
      {/* Sticky header */}
      <div className="sticky top-0 z-30 border-b border-gray-800 bg-gray-950/95 px-3 py-3 backdrop-blur-sm sm:px-6 sm:py-4">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-2 sm:gap-4">
          <div className="min-w-0 flex-1">
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="mb-0.5 inline-flex items-center gap-1 text-xs text-gray-400 transition-colors hover:text-white"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
              Back
            </button>
            <h1 className="truncate text-base font-bold text-white sm:text-xl">{eventData?.name ?? 'Gallery'}</h1>
            {eventData ? (
              <p className="truncate text-xs text-gray-400 sm:text-sm">{eventData.photoCount} photos · {eventData.totalSize}</p>
            ) : null}
          </div>
          <div className="flex shrink-0 items-center gap-2 sm:gap-3">
            <div className={`flex items-center gap-1 text-xs sm:gap-2 sm:text-sm ${isConnected ? 'text-green-400' : 'text-gray-500'}`}>
              <Wifi className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
              <span className="hidden sm:inline">{isConnected ? 'Live' : 'Connecting…'}</span>
            </div>
            {photos.length > 0 ? (
              isSelectMode ? (
                <>
                  <button type="button" onClick={allSelected ? clearSelection : selectAll}
                    className="flex items-center gap-1 rounded-lg bg-primary-600 px-2 py-1.5 text-xs font-medium text-white transition-colors hover:bg-primary-700 sm:gap-1.5 sm:px-3 sm:text-sm">
                    {allSelected
                      ? <><Square className="h-3.5 w-3.5 sm:h-4 sm:w-4" /><span className="hidden xs:inline sm:inline"> Deselect all</span></>
                      : <><CheckSquare className="h-3.5 w-3.5 sm:h-4 sm:w-4" /><span className="hidden xs:inline sm:inline"> Select all</span></>}
                  </button>
                  <button type="button" onClick={exitSelectMode}
                    className="flex items-center gap-1 rounded-lg border border-gray-600 px-2 py-1.5 text-xs text-gray-400 transition-colors hover:border-gray-500 hover:text-white sm:gap-1.5 sm:px-3 sm:text-sm">
                    <X className="h-3.5 w-3.5 sm:h-4 sm:w-4" /><span className="hidden sm:inline"> Cancel</span>
                  </button>
                </>
              ) : (
                <button type="button" onClick={() => setIsSelectMode(true)}
                  className="flex items-center gap-1 rounded-lg border border-gray-700 px-2 py-1.5 text-xs text-gray-300 transition-colors hover:border-gray-500 hover:text-white sm:gap-1.5 sm:px-3 sm:text-sm">
                  <CheckSquare className="h-3.5 w-3.5 sm:h-4 sm:w-4" /><span className="hidden sm:inline"> Select</span>
                </button>
              )
            ) : null}
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-4 py-6">
        {isRecentCountMode && photos.length > 0 ? (
          <div className="mb-4 flex items-center gap-2 rounded-lg border border-amber-700/50 bg-amber-900/30 px-4 py-2 text-sm text-amber-300">
            <span>Showing the {eventData!.galleryRecentCount} most recent photo{eventData!.galleryRecentCount === 1 ? '' : 's'}.</span>
          </div>
        ) : null}

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
            {eventData?.watchFolder ? (
              <p className="mt-3 rounded-lg bg-gray-800 px-4 py-2 font-mono text-xs text-gray-400">
                Drop photos into: {eventData.watchFolder}
              </p>
            ) : null}
          </div>
        ) : null}

        {photos.length > 0 ? (
          <div className="grid grid-cols-2 gap-1.5 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
            {photos.map(photo => {
              const isChosen = selected.has(photo.id);
              return (
                <button key={photo.id} type="button"
                  onClick={() => { if (didLongPress.current) { didLongPress.current = false; return; } isSelectMode ? toggleSelect(photo.id) : openLightbox(photo.id); }}
                  onTouchStart={() => handleTouchStart(photo.id)}
                  onTouchEnd={handleTouchEnd}
                  onTouchMove={handleTouchMove}
                  onContextMenu={e => e.preventDefault()}
                  className={[
                    'group relative aspect-square overflow-hidden rounded-lg bg-gray-800 transition-all select-none',  
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

        {photosData && photosData.totalPages > 1 && !isRecentCountMode ? (
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
          <div className="mx-auto max-w-7xl px-3 py-2 sm:px-6 sm:py-3">
            {/* Row 1 — count + select all */}
            <div className="flex items-center justify-between gap-2 sm:hidden">
              <span className="text-sm font-medium text-white">
                {selected.size} {selected.size === 1 ? 'photo' : 'photos'} selected
              </span>
              <button type="button" onClick={allSelected ? clearSelection : selectAll}
                className="flex items-center gap-1 text-sm text-primary-500 transition-colors hover:text-primary-100">
                {allSelected ? <><Square className="h-4 w-4" /> Deselect all</> : <><CheckSquare className="h-4 w-4" /> Select all</>}
              </button>
            </div>
            {/* Row 2 (mobile) / single row (desktop) */}
            <div className="mt-2 flex items-center justify-between gap-2 sm:mt-0">
              {/* Desktop-only left side */}
              <div className="hidden items-center gap-4 text-sm sm:flex">
                <span className="font-medium text-white">
                  {selected.size} {selected.size === 1 ? 'photo' : 'photos'} selected
                </span>
                <button type="button" onClick={allSelected ? clearSelection : selectAll}
                  className="flex items-center gap-1 text-primary-500 transition-colors hover:text-primary-100">
                  {allSelected ? <><Square className="h-4 w-4" /> Deselect all</> : <><CheckSquare className="h-4 w-4" /> Select all</>}
                </button>
              </div>
              {/* Actions — always visible */}
              <div className="flex w-full gap-2 sm:w-auto">
                <button type="button" onClick={exitSelectMode}
                  className="flex-1 rounded-lg border border-gray-700 px-3 py-2 text-sm text-gray-400 transition-colors hover:text-white sm:flex-none sm:px-4">
                  Cancel
                </button>
                <button type="button" onClick={downloadSelected} disabled={selected.size === 0}
                  className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-primary-600 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-40 sm:flex-none sm:px-4">
                  <Download className="h-4 w-4" />
                  Download ({selected.size})
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {/* Lightbox */}
      {lightboxPhoto ? (
        <div
          role="presentation"
          className="fixed inset-0 z-50 flex items-center bg-black/95"
          onClick={closeLightbox}
          onTouchStart={e => { swipeTouchStartX.current = e.touches[0].clientX; }}
          onTouchEnd={e => {
            if (swipeTouchStartX.current === null) return;
            const dx = e.changedTouches[0].clientX - swipeTouchStartX.current;
            if (Math.abs(dx) > 50) { dx < 0 ? goNext() : goPrev(); }
            swipeTouchStartX.current = null;
          }}
        >
          {/* Close button */}
          <button type="button" onClick={closeLightbox}
            className="absolute right-3 top-3 z-20 flex h-9 w-9 items-center justify-center rounded-full bg-black/70 text-white transition-colors hover:bg-black/90 sm:right-4 sm:top-4">
            <X className="h-4 w-4" />
          </button>

          {photos.length > 1 ? (
            <button type="button" onClick={e => { e.stopPropagation(); goPrev(); }}
              className="absolute left-2 top-1/2 z-10 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/60 text-white transition-colors hover:bg-black/90 sm:left-3">
              <ChevronLeft className="h-5 w-5" />
            </button>
          ) : null}

          <div role="presentation" className="mx-4 flex w-full flex-col items-center gap-3 sm:mx-14 sm:gap-4" onClick={e => e.stopPropagation()}>
            <img
              src={lightboxPhoto.originalUrl || photosApi.getDownloadUrl(lightboxPhoto.id)}
              alt={lightboxPhoto.fileName}
              className="max-h-[75vh] max-w-full rounded-lg object-contain shadow-2xl sm:rounded-xl"
            />
            <div className="flex w-full max-w-2xl items-center justify-between gap-2 rounded-xl bg-gray-900/80 px-3 py-2 backdrop-blur-sm sm:px-4 sm:py-3">
              <div className="min-w-0">
                <p className="truncate text-xs font-medium text-white sm:text-sm">{lightboxPhoto.fileName}</p>
                <p className="text-xs text-gray-400">
                  {formatDateTime(lightboxPhoto.capturedAt)} · {lightboxIndex + 1} / {photos.length}
                </p>
              </div>
              <button
                type="button"
                onClick={e => { e.stopPropagation(); window.open(photosApi.getDownloadUrl(lightboxPhoto.id), '_blank'); }}
                className="flex shrink-0 items-center gap-1.5 rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-primary-700 sm:gap-2 sm:px-4 sm:py-2 sm:text-sm">
                <Download className="h-3.5 w-3.5 sm:h-4 sm:w-4" /><span className="hidden sm:inline">Download</span>
              </button>
            </div>
          </div>

          {photos.length > 1 ? (
            <button type="button" onClick={e => { e.stopPropagation(); goNext(); }}
              className="absolute right-2 top-1/2 z-10 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/60 text-white transition-colors hover:bg-black/90 sm:right-3">
              <ChevronRight className="h-5 w-5" />
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
