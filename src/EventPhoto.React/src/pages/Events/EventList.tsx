import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, ChevronLeft, ChevronRight, Eye, Plus, Power, QrCode, RefreshCw, Search, Trash2, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { Link } from 'react-router-dom';
import { eventsApi } from '../../api/events';
import { Badge } from '../../components/UI/Badge';
import { Button } from '../../components/UI/Button';
import { Card } from '../../components/UI/Card';
import { Modal } from '../../components/UI/Modal';
import { Spinner } from '../../components/UI/Spinner';
import { formatDate } from '../../utils/format';

const PAGE_SIZE = 8;

export default function EventList() {
  const queryClient = useQueryClient();
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const response = await eventsApi.getAll();
      return response.data ?? [];
    },
    refetchInterval: 10_000,
  });

  const confirmEvent = confirmDeleteId ? (data ?? []).find(e => e.id === confirmDeleteId) : null;

  // Client-side search filter
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return data ?? [];
    return (data ?? []).filter(e =>
      e.name.toLowerCase().includes(q) ||
      (e.clientName ?? '').toLowerCase().includes(q) ||
      e.eventType.toLowerCase().includes(q) ||
      e.eventDate.toString().includes(q),
    );
  }, [data, search]);

  // Reset to page 1 whenever search changes
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  const handleSearch = (value: string) => {
    setSearch(value);
    setPage(1);
  };

  const deleteMutation = useMutation({
    mutationFn: eventsApi.delete,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
      toast.success('Event deleted.');
    },
    onError: () => toast.error('Failed to delete event.'),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) => eventsApi.toggleActive(id, activate),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
      toast.success('Event updated.');
    },
    onError: () => toast.error('Failed to update event.'),
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Events</h1>
        <div className="flex items-center gap-2">
          <button
            type="button"
            title="Refresh photo counts and sizes"
            onClick={() => { void refetch(); void queryClient.invalidateQueries({ queryKey: ['events'] }); }}
            disabled={isFetching}
            className="flex items-center gap-1.5 rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-600 transition-colors hover:border-gray-400 hover:text-gray-900 disabled:opacity-50"
          >
            <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>
          <Link to="/admin/events/new">
            <Button>
              <Plus className="h-4 w-4" />
              New Event
            </Button>
          </Link>
        </div>
      </div>

      {/* Search bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
        <input
          type="text"
          placeholder="Search by event name, client, type or date…"
          value={search}
          onChange={e => handleSearch(e.target.value)}
          className="w-full rounded-lg border border-gray-300 bg-white py-2.5 pl-10 pr-10 text-sm text-gray-900 placeholder-gray-400 transition-colors focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20"
        />
        {search ? (
          <button
            type="button"
            title="Clear search"
            onClick={() => handleSearch('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
          >
            <X className="h-4 w-4" />
          </button>
        ) : null}
      </div>

      {/* Results info */}
      {search ? (
        <p className="text-sm text-gray-500">
          {filtered.length === 0
            ? 'No events match your search.'
            : `${filtered.length} event${filtered.length === 1 ? '' : 's'} found`}
        </p>
      ) : null}

      {/* Empty state */}
      {(data ?? []).length === 0 ? (
        <Card className="p-12 text-center text-gray-500">No events yet. Create your first event to get started.</Card>
      ) : null}

      {/* Event cards */}
      <div className="space-y-3">
        {paged.map((eventItem) => (
          <Card key={eventItem.id} className="p-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="min-w-0 space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/admin/events/${eventItem.id}`} className="font-semibold text-gray-900 transition-colors hover:text-primary-600">
                    {eventItem.name}
                  </Link>
                  <Badge label={eventItem.isActive ? 'Active' : 'Inactive'} color={eventItem.isActive ? 'green' : 'red'} />
                  <Badge label={eventItem.eventType} color="blue" />
                </div>
                <p className="text-sm text-gray-500">
                  {formatDate(eventItem.eventDate)} · {eventItem.clientName ?? 'No client'} · {eventItem.photoCount} photos · {eventItem.totalSize}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                <Link to={`/gallery/${eventItem.id}`} target="_blank" title="Open gallery">
                  <Button variant="ghost" size="sm"><Eye className="h-4 w-4" /></Button>
                </Link>
                <a href={eventsApi.getQrCodeUrl(eventItem.id)} target="_blank" rel="noreferrer" title="Download QR code">
                  <Button variant="ghost" size="sm"><QrCode className="h-4 w-4" /></Button>
                </a>
                <Button variant="ghost" size="sm"
                  title={eventItem.isActive ? 'Deactivate event' : 'Activate event'}
                  onClick={() => toggleMutation.mutate({ id: eventItem.id, activate: !eventItem.isActive })}>
                  <Power className="h-4 w-4" />
                </Button>
                <Button variant="danger" size="sm" title="Delete event"
                  onClick={() => setConfirmDeleteId(eventItem.id)}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 ? (
        <div className="flex items-center justify-between border-t border-gray-200 pt-4">
          <p className="text-sm text-gray-500">
            Page {safePage} of {totalPages} · {filtered.length} event{filtered.length === 1 ? '' : 's'}
          </p>
          <div className="flex items-center gap-1">
            <button type="button" title="Previous page"
              disabled={safePage === 1}
              onClick={() => setPage(p => p - 1)}
              className="flex h-8 w-8 items-center justify-center rounded-lg border border-gray-300 text-gray-600 transition-colors hover:bg-gray-50 disabled:opacity-40">
              <ChevronLeft className="h-4 w-4" />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
              <button key={p} type="button"
                onClick={() => setPage(p)}
                className={`flex h-8 w-8 items-center justify-center rounded-lg border text-sm font-medium transition-colors ${
                  p === safePage
                    ? 'border-primary-600 bg-primary-600 text-white'
                    : 'border-gray-300 text-gray-600 hover:bg-gray-50'
                }`}>
                {p}
              </button>
            ))}
            <button type="button" title="Next page"
              disabled={safePage === totalPages}
              onClick={() => setPage(p => p + 1)}
              className="flex h-8 w-8 items-center justify-center rounded-lg border border-gray-300 text-gray-600 transition-colors hover:bg-gray-50 disabled:opacity-40">
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      ) : null}

      {/* Delete confirmation modal */}
      <Modal isOpen={Boolean(confirmDeleteId)} title="Delete Event" onClose={() => setConfirmDeleteId(null)}>
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
