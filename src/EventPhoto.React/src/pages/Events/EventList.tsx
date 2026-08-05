// ─── EventsPage — premium photography events gallery ────────────────────────
// This file is the /admin/events route. It replaces the old CRUD list with a
// full dark-theme studio experience: hero header, stats bar, event spotlight,
// command search, filter chips, view toggle, and an animated card gallery.
// ────────────────────────────────────────────────────────────────────────────
import { AlertTriangle, ChevronLeft, ChevronRight, LayoutGrid, List, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { EmptyEventsState } from '../../components/events/EmptyEventsState';
import { EventCard } from '../../components/events/EventCard';
import { EventFilters } from '../../components/events/EventFilters';
import { EventGallerySkeleton } from '../../components/events/EventGallerySkeleton';
import { EventSearch } from '../../components/events/EventSearch';
import { Button } from '../../components/UI/Button';
import { Modal } from '../../components/UI/Modal';
import { useEventMutations, useEventTypes, useEvents, useFilteredEvents } from '../../hooks/useEvents';

const PAGE_SIZE = 12;
type ViewMode = 'gallery' | 'compact';

// ── Main page component ──────────────────────────────────────────────────────
export default function EventList() {
  const [search,          setSearch]          = useState('');
  const [filter,          setFilter]          = useState('all');
  const [view,            setView]            = useState<ViewMode>('gallery');
  const [page,            setPage]            = useState(1);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  // ── Data ──────────────────────────────────────────────────────────────────
  const { data: events, isLoading, isError } = useEvents();
  const { deleteMutation, toggleMutation, refreshQrMutation } = useEventMutations();

  // ── Derived state ─────────────────────────────────────────────────────────
  const eventTypes   = useEventTypes(events);
  const filtered     = useFilteredEvents(events, search, filter);
  const totalPages   = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage     = Math.min(page, totalPages);
  const paged        = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);
  const confirmEvent = confirmDeleteId ? (events ?? []).find(e => e.id === confirmDeleteId) : null;

  const handleSearch = (v: string) => { setSearch(v); setPage(1); };
  const handleFilter = (f: string) => { setFilter(f); setPage(1); };

  const refreshingQrId = refreshQrMutation.isPending
    ? (refreshQrMutation.variables ?? null)
    : null;

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <div className="-m-4 min-h-full bg-slate-950 sm:-m-6">
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-6 pb-16 sm:px-6">

        {/* ── Error banner ─────────────────────────────────────────────── */}
        {isError ? (
          <div className="flex items-center gap-3 rounded-xl border border-amber-800/50 bg-amber-900/20 px-4 py-3 text-sm text-amber-300">
            <AlertTriangle className="h-4 w-4 shrink-0 text-amber-400" />
            Could not load events. Showing cached data or retrying…
          </div>
        ) : null}

        {/* ── Search + Filters ─────────────────────────────────────────── */}
        <div className="space-y-3">
          <EventSearch value={search} onChange={handleSearch} />
          <EventFilters
            activeFilter={filter}
            onFilterChange={handleFilter}
            eventTypes={eventTypes}
            events={events ?? []}
          />
        </div>

        {/* ── View toggle + results count + New Event ─────────────────── */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-400">
            {isLoading ? (
              <span className="inline-block h-4 w-28 animate-pulse rounded bg-slate-800" />
            ) : (
              <>
                <span className="font-semibold text-slate-200">{filtered.length}</span>
                {' event'}{filtered.length !== 1 ? 's' : ''}
                {search || filter !== 'all' ? ' found' : ' total'}
              </>
            )}
          </p>
          <div className="flex items-center gap-2">
            <div className="flex items-center gap-px rounded-xl border border-slate-800 bg-slate-900 p-1">
              <button
                type="button"
                onClick={() => setView('gallery')}
                aria-label="Gallery view"
                title="Gallery view"
                className={`rounded-lg p-2 transition-colors ${view === 'gallery' ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-slate-200'}`}
              >
                <LayoutGrid className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={() => setView('compact')}
                aria-label="Compact view"
                title="Compact view"
                className={`rounded-lg p-2 transition-colors ${view === 'compact' ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-slate-200'}`}
              >
                <List className="h-4 w-4" />
              </button>
            </div>
            <Link
              to="/admin/events/new"
              className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-md shadow-indigo-600/20 transition-colors hover:bg-indigo-500"
            >
              <Plus className="h-4 w-4" />
              New Event
            </Link>
          </div>
        </div>

        {/* ── 6. Event Gallery ─────────────────────────────────────────── */}
        {isLoading ? (
          <EventGallerySkeleton view={view} />
        ) : (events ?? []).length === 0 ? (
          <EmptyEventsState hasSearch={false} />
        ) : filtered.length === 0 ? (
          <EmptyEventsState hasSearch={true} />
        ) : view === 'gallery' ? (
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {paged.map((event, i) => (
              <EventCard
                key={event.id}
                event={event}
                index={i}
                view="gallery"
                onDelete={setConfirmDeleteId}
                onToggleActive={(id, activate) => toggleMutation.mutate({ id, activate })}
                onRefreshQr={(id) => refreshQrMutation.mutate(id)}
                refreshingQrId={refreshingQrId}
              />
            ))}
          </div>
        ) : (
          <div className="space-y-3">
            {paged.map((event, i) => (
              <EventCard
                key={event.id}
                event={event}
                index={i}
                view="compact"
                onDelete={setConfirmDeleteId}
                onToggleActive={(id, activate) => toggleMutation.mutate({ id, activate })}
                onRefreshQr={(id) => refreshQrMutation.mutate(id)}
                refreshingQrId={refreshingQrId}
              />
            ))}
          </div>
        )}

        {/* ── 7. Pagination ────────────────────────────────────────────── */}
        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-slate-800 pt-6">
            <p className="text-sm text-slate-500">
              Page {safePage} of {totalPages} · {filtered.length} events
            </p>
            <div className="flex items-center gap-1">
              <button
                type="button"
                title="Previous page"
                disabled={safePage === 1}
                onClick={() => setPage(p => p - 1)}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-slate-700 text-slate-400 transition-colors hover:border-slate-600 hover:bg-slate-800 hover:text-slate-200 disabled:opacity-40"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => i + 1).map(p => (
                <button
                  key={p}
                  type="button"
                  onClick={() => setPage(p)}
                  className={`flex h-9 w-9 items-center justify-center rounded-lg border text-sm font-medium transition-colors ${
                    p === safePage
                      ? 'border-indigo-600 bg-indigo-600 text-white'
                      : 'border-slate-700 text-slate-400 hover:border-slate-600 hover:bg-slate-800 hover:text-white'
                  }`}
                >
                  {p}
                </button>
              ))}
              <button
                type="button"
                title="Next page"
                disabled={safePage === totalPages}
                onClick={() => setPage(p => p + 1)}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-slate-700 text-slate-400 transition-colors hover:border-slate-600 hover:bg-slate-800 hover:text-slate-200 disabled:opacity-40"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        ) : null}
      </div>

      {/* ── Delete confirmation modal ─────────────────────────────────── */}
      <Modal
        isOpen={Boolean(confirmDeleteId)}
        title="Delete Event"
        onClose={() => setConfirmDeleteId(null)}
      >
        <div className="flex flex-col items-center gap-4 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-7 w-7 text-red-600" />
          </div>
          <div>
            <p className="text-base font-semibold text-gray-900">
              Delete &ldquo;{confirmEvent?.name}&rdquo;?
            </p>
            <p className="mt-1 text-sm text-gray-500">
              This will permanently delete the event and all its data. This action cannot be undone.
            </p>
          </div>
          <div className="flex w-full gap-3 pt-1">
            <Button variant="secondary" className="flex-1" onClick={() => setConfirmDeleteId(null)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              className="flex-1"
              isLoading={deleteMutation.isPending}
              onClick={() => {
                if (confirmDeleteId) {
                  deleteMutation.mutate(confirmDeleteId, {
                    onSettled: () => setConfirmDeleteId(null),
                  });
                }
              }}
            >
              Delete
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

