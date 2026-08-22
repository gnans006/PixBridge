import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { QrCode, RefreshCw, Download, Search, ExternalLink } from 'lucide-react';
import toast from 'react-hot-toast';
import { eventsApi } from '../../api/events';
import { useConfirm } from '../../hooks/useConfirm';
import { useEvents } from '../../hooks/useEvents';
import type { EventResponse } from '../../types';

export default function QrCenterPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [refreshingId, setRefreshingId] = useState<string | null>(null);

  const { data: events = [], isLoading } = useEvents();
  const filtered = events.filter((e) =>
    e.name.toLowerCase().includes(search.toLowerCase()) ||
    (e.clientName ?? '').toLowerCase().includes(search.toLowerCase()),
  );

  const refreshMutation = useMutation({
    mutationFn: (id: string) => eventsApi.refreshQr(id),
    onSuccess: () => {
      toast.success('QR code refreshed');
      // Bust the img cache by invalidating events
      queryClient.invalidateQueries({ queryKey: ['events'] });
      setRefreshingId(null);
    },
    onError: () => {
      toast.error('Failed to refresh QR code');
      setRefreshingId(null);
    },
  });

  const handleRefresh = (id: string) => {
    setRefreshingId(id);
    refreshMutation.mutate(id);
  };

  const handleDownload = (event: EventResponse) => {
    const url = eventsApi.getQrCodeUrl(event.id);
    const a = document.createElement('a');
    a.href = url;
    a.download = `QR-${event.name.replace(/\s+/g, '_')}.png`;
    a.click();
  };

  return (
    <div className="min-h-screen bg-slate-950 px-6 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-1">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-500/10 text-indigo-400">
            <QrCode className="h-5 w-5" />
          </span>
          <h1 className="text-2xl font-bold text-slate-100">QR Center</h1>
        </div>
        <p className="text-sm text-slate-500 ml-12">
          Download and manage QR codes for all your events
        </p>
      </div>

      {/* Search */}
      <div className="mb-6 relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-500 pointer-events-none" />
        <input
          type="text"
          placeholder="Search events…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full rounded-xl border border-slate-700 bg-slate-800 pl-9 pr-4 py-2.5 text-sm text-slate-100 placeholder:text-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
        />
      </div>

      {/* Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="animate-pulse rounded-2xl border border-slate-800 bg-slate-900 p-5 h-72" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-24 text-slate-500">
          <QrCode className="h-12 w-12 mb-3 opacity-30" />
          <p className="text-sm">{search ? 'No events match your search' : 'No events found'}</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filtered.map((event) => (
            <QrCard
              key={event.id}
              event={event}
              isRefreshing={refreshingId === event.id}
              onRefresh={() => handleRefresh(event.id)}
              onDownload={() => handleDownload(event)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ── QR Card ───────────────────────────────────────────────────────────────────

interface QrCardProps {
  event: EventResponse;
  isRefreshing: boolean;
  onRefresh: () => void;
  onDownload: () => void;
}

function QrCard({ event, isRefreshing, onRefresh, onDownload }: QrCardProps) {
  const [imgBust, setImgBust] = useState(() => Date.now());
  const confirm = useConfirm();
  const qrUrl = eventsApi.getQrCodeUrl(event.id, imgBust);
  const galleryUrl = `/gallery/${event.id}`;

  const handleRefresh = async () => {
    const ok = await confirm({
      title: 'Regenerate QR Code?',
      message: 'The current QR code will stop working immediately. Any printed materials using this QR will need to be reprinted.',
      confirmLabel: 'Regenerate',
      variant: 'warning',
    });
    if (!ok) return;
    onRefresh();
    setTimeout(() => setImgBust(Date.now()), 1500);
  };

  return (
    <div className="flex flex-col rounded-2xl border border-slate-800 bg-slate-900 overflow-hidden hover:border-slate-700 transition-colors">
      {/* QR Image */}
      <div className="flex items-center justify-center bg-slate-800/50 p-6">
        <div className="rounded-xl bg-white p-3">
          <img
            src={qrUrl}
            alt={`QR code for ${event.name}`}
            className="h-28 w-28 object-contain"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
        </div>
      </div>

      {/* Info */}
      <div className="flex flex-1 flex-col p-4 gap-1">
        <p className="text-sm font-semibold text-slate-100 truncate" title={event.name}>
          {event.name}
        </p>
        <p className="text-xs text-slate-500">
          {event.eventType} · {new Date(event.eventDate).toLocaleDateString()}
        </p>
        {event.clientName && (
          <p className="text-xs text-slate-600 truncate">{event.clientName}</p>
        )}
        <span className={`mt-1 inline-flex w-fit items-center rounded-full px-2 py-0.5 text-[10px] font-medium ${
          event.isActive
            ? 'bg-emerald-500/10 text-emerald-400'
            : 'bg-slate-700 text-slate-500'
        }`}>
          {event.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-1 border-t border-slate-800 p-3">
        <button
          onClick={onDownload}
          title="Download QR code"
          className="flex flex-1 items-center justify-center gap-1.5 rounded-lg py-1.5 text-xs font-medium text-slate-300 hover:bg-slate-800 transition-colors"
        >
          <Download className="h-3.5 w-3.5" />
          Download
        </button>
        <button
          onClick={handleRefresh}
          disabled={isRefreshing}
          title="Regenerate QR code"
          className="flex flex-1 items-center justify-center gap-1.5 rounded-lg py-1.5 text-xs font-medium text-slate-300 hover:bg-slate-800 transition-colors disabled:opacity-50"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isRefreshing ? 'animate-spin' : ''}`} />
          Refresh
        </button>
        <a
          href={galleryUrl}
          target="_blank"
          rel="noopener noreferrer"
          title="Open gallery"
          className="flex items-center justify-center rounded-lg p-1.5 text-slate-500 hover:bg-slate-800 hover:text-slate-300 transition-colors"
        >
          <ExternalLink className="h-3.5 w-3.5" />
        </a>
      </div>
    </div>
  );
}
