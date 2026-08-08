import { useQuery } from '@tanstack/react-query';
import { BarChart2 } from 'lucide-react';
import { eventsApi } from '../api/events';
import { statisticsApi } from '../api/statistics';
import { Card } from '../components/UI/Card';
import { PageShell } from '../components/UI/PageShell';
import { SkeletonCard } from '../components/UI/Skeleton';
import { EmptyState } from '../components/UI/EmptyState';

export default function Statistics() {
  const { data: events, isLoading, isError } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const response = await eventsApi.getAll();
      return response.data ?? [];
    },
  });

  return (
    <PageShell
      title="Insights"
      description="Per-production statistics: photo counts, downloads, face matches, and storage."
    >
      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map(i => <SkeletonCard key={i} />)}
        </div>
      ) : isError ? (
        <Card padding className="text-center text-sm text-pds-danger">
          Could not load statistics. The server may be unavailable.
        </Card>
      ) : !events?.length ? (
        <EmptyState
          icon={BarChart2}
          title="No Productions Found"
          description="Create a production to start tracking statistics."
        />
      ) : (
        <div className="space-y-4">
          {events.map(e => (
            <EventStatsCard key={e.id} eventId={e.id} eventName={e.name} />
          ))}
        </div>
      )}
    </PageShell>
  );
}

function EventStatsCard({ eventId, eventName }: { eventId: string; eventName: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['event-stats', eventId],
    queryFn: async () => {
      const response = await statisticsApi.getEventStats(eventId);
      return response.data;
    },
  });

  return (
    <Card className="p-5">
      <h3 className="mb-3 font-semibold text-pds-text">{eventName}</h3>
      {isLoading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {[1,2,3].map(i => <div key={i} className="h-10 pds-shimmer rounded-lg" />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3">
          {[
            { label: 'Photos',    value: data?.totalPhotos ?? 0 },
            { label: 'Downloads', value: data?.totalDownloads ?? 0 },
            { label: 'Storage',   value: data?.totalSizeHuman ?? '0 B' },
          ].map(({ label, value }) => (
            <div key={label} className="rounded-xl bg-pds-elevated px-3 py-2.5">
              <p className="text-xs text-pds-text-muted">{label}</p>
              <p className="mt-0.5 text-base font-semibold text-pds-text">{value}</p>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}
