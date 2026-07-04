import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarDays, ChevronLeft, Download, FolderOpen, Images, QrCode, RefreshCw, UserRound, X } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'react-hot-toast';
import { Link, useParams } from 'react-router-dom';
import { eventsApi } from '../../api/events';
import { photosApi } from '../../api/photos';
import { statisticsApi } from '../../api/statistics';
import { Badge } from '../../components/UI/Badge';
import { Button } from '../../components/UI/Button';
import { Card } from '../../components/UI/Card';
import { Spinner } from '../../components/UI/Spinner';
import { formatDate, formatDateTime } from '../../utils/format';

export default function EventDetail() {
  const { eventId } = useParams<{ eventId: string }>();
  const queryClient = useQueryClient();
  const [showQrModal, setShowQrModal] = useState(false);
  const [qrBust, setQrBust] = useState(() => Date.now());

  const refreshQrMutation = useMutation({
    mutationFn: () => eventsApi.refreshQr(eventId!),
    onSuccess: () => {
      setQrBust(Date.now());
      void queryClient.invalidateQueries({ queryKey: ['event', eventId] });
      toast.success('QR code refreshed.');
    },
    onError: () => toast.error('Failed to refresh QR code.'),
  });

  const { data: eventData, isLoading: isEventLoading } = useQuery({
    queryKey: ['event', eventId],
    queryFn: async () => {
      const response = await eventsApi.getById(eventId!);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  const { data: statsData, isLoading: isStatsLoading } = useQuery({
    queryKey: ['event-stats', eventId],
    queryFn: async () => {
      const response = await statisticsApi.getEventStats(eventId!);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  const { data: photosData, isLoading: isPhotosLoading } = useQuery({
    queryKey: ['event-photos-preview', eventId],
    queryFn: async () => {
      const response = await photosApi.getByEvent(eventId!, 1, 8);
      return response.data;
    },
    enabled: Boolean(eventId),
  });

  if (isEventLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!eventData) {
    return <div className="rounded-xl bg-red-50 p-4 text-sm text-red-700">Event not found.</div>;
  }

  return (
    <>
    <div className="space-y-6">
      {/* Back navigation */}
      <Link
        to="/admin/events"
        className="inline-flex items-center gap-1 text-sm text-gray-500 transition-colors hover:text-gray-900"
      >
        <ChevronLeft className="h-4 w-4" />
        Back to Events
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="mb-2 flex items-center gap-2">
            <h1 className="text-2xl font-bold text-gray-900">{eventData.name}</h1>
            <Badge label={eventData.isActive ? 'Active' : 'Inactive'} color={eventData.isActive ? 'green' : 'red'} />
            <Badge label={eventData.eventType} color="blue" />
          </div>
          <p className="max-w-3xl text-sm text-gray-500">{eventData.description || 'No description provided.'}</p>
        </div>
        <div className="flex gap-2">
          <Link to={`/gallery/${eventData.id}`} target="_blank">
            <Button>Open Gallery</Button>
          </Link>
          <Button variant="secondary" onClick={() => setShowQrModal(true)}>
            <QrCode className="h-4 w-4" />
            QR Code
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="p-5 lg:col-span-2">
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Event Details</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <InfoRow icon={CalendarDays} label="Event date" value={formatDate(eventData.eventDate)} />
            <InfoRow icon={UserRound} label="Client" value={eventData.clientName ?? 'Not specified'} />
            <InfoRow icon={FolderOpen} label="Watch folder" value={eventData.watchFolder} />
            <InfoRow icon={Images} label="Photos" value={`${eventData.photoCount} photos`} />
          </div>
        </Card>

        <Card className="p-5">
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Statistics</h2>
          {isStatsLoading ? (
            <Spinner size="sm" />
          ) : (
            <div className="space-y-3 text-sm">
              <StatRow label="Total downloads" value={statsData?.totalDownloads ?? 0} />
              <StatRow label="Storage" value={statsData?.totalSizeHuman ?? eventData.totalSize} />
              <StatRow label="Pending thumbs" value={statsData?.thumbnailsPending ?? 0} />
              <StatRow label="Failed thumbs" value={statsData?.thumbnailsFailed ?? 0} />
              <StatRow label="Last photo" value={statsData?.lastPhotoAt ? formatDateTime(statsData.lastPhotoAt) : 'N/A'} />
            </div>
          )}
        </Card>
      </div>

      <Card className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-900">Latest Photos</h2>
          <Link to={`/gallery/${eventData.id}`} className="text-sm font-medium text-primary-600 hover:text-primary-700">
            View full gallery
          </Link>
        </div>
        {isPhotosLoading ? (
          <Spinner size="sm" />
        ) : photosData?.items.length ? (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-8">
            {photosData.items.map((photo) => (
              <div key={photo.id} className="overflow-hidden rounded-xl bg-gray-100">
                <img src={photosApi.getThumbnailUrl(photo.id)} alt={photo.fileName} className="aspect-square h-full w-full object-cover" />
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No photos captured yet.</p>
        )}
      </Card>
    </div>

    {/* QR Code Modal */}
    {showQrModal ? (
      <div
        role="presentation"
        className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm"
        onClick={() => setShowQrModal(false)}
      >
        <div
          role="dialog"
          aria-modal
          className="relative w-full max-w-sm rounded-2xl bg-white shadow-2xl"
          onClick={e => e.stopPropagation()}
        >
          <button
            type="button"
            onClick={() => setShowQrModal(false)}
            className="absolute right-3 top-3 flex h-7 w-7 items-center justify-center rounded-full text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
          >
            <X className="h-4 w-4" />
          </button>

          {/* Coloured header */}
          <div className="rounded-t-2xl bg-gradient-to-br from-teal-600 to-teal-800 px-6 py-5 text-center">
            <p className="text-xs font-semibold uppercase tracking-widest text-teal-200">Event Gallery</p>
            <h2 className="mt-1 text-lg font-bold text-white">{eventData.name}</h2>
            {eventData.venueName ? (
              <p className="mt-0.5 text-sm text-teal-200">{eventData.venueName}</p>
            ) : null}
          </div>

          {/* QR image */}
          <div className="flex justify-center px-6 py-5">
            <div className="rounded-xl border border-gray-200 bg-gray-50 p-3">
              <img
                src={eventsApi.getQrCodeUrl(eventData.id, qrBust)}
                alt="QR Code"
                className="h-52 w-52 object-contain"
              />
            </div>
          </div>

          <p className="mb-4 text-center text-xs text-gray-500">
            Scan with phone camera to open the gallery
          </p>

          <div className="flex gap-2 rounded-b-2xl border-t border-gray-100 px-6 py-4">
            <button
              type="button"
              onClick={() => refreshQrMutation.mutate()}
              disabled={refreshQrMutation.isPending}
              className="flex items-center justify-center gap-1.5 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 disabled:opacity-50"
              title="Regenerate QR with current server IP"
            >
              <RefreshCw className={`h-4 w-4 ${refreshQrMutation.isPending ? 'animate-spin' : ''}`} />
              Refresh
            </button>
            <a
              href={eventsApi.getQrCodeUrl(eventData.id, qrBust)}
              download={`qr-${eventData.name}.png`}
              className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-teal-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-teal-700"
            >
              <Download className="h-4 w-4" />
              Download
            </a>
            <Link
              to={`/gallery/${eventData.id}`}
              target="_blank"
              rel="noreferrer"
              className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              Open Gallery
            </Link>
          </div>
        </div>
      </div>
    ) : null}
    </>
  );
}

function InfoRow({ icon: Icon, label, value }: { icon: typeof CalendarDays; label: string; value: string }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-gray-50 p-4">
      <div className="mb-2 flex items-center gap-2 text-gray-500">
        <Icon className="h-4 w-4" />
        <span className="text-xs font-medium uppercase tracking-wide">{label}</span>
      </div>
      <p className="break-all text-sm font-medium text-gray-900">{value}</p>
    </div>
  );
}

function StatRow({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2">
      <span className="text-gray-500">{label}</span>
      <span className="font-medium text-gray-900">{value}</span>
    </div>
  );
}
