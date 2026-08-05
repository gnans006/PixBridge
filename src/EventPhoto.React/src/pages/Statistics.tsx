import { useQuery } from '@tanstack/react-query';
import { AlertTriangle } from 'lucide-react';
import { eventsApi } from '../api/events';
import { statisticsApi } from '../api/statistics';
import { Card } from '../components/UI/Card';
import { Spinner } from '../components/UI/Spinner';

export default function Statistics() {
  const { data: events, isLoading, isError } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const response = await eventsApi.getAll();
      return response.data ?? [];
    },
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold text-gray-900">Statistics</h1>
        <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <AlertTriangle className="h-4 w-4 shrink-0 text-amber-600" />
          <span>Could not load event statistics. The server may be unavailable.</span>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Statistics</h1>
      <div className="space-y-4">
        {events?.map((eventItem) => (
          <EventStatsCard key={eventItem.id} eventId={eventItem.id} eventName={eventItem.name} />
        ))}
      </div>
    </div>
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
      <h3 className="mb-3 font-semibold text-gray-900">{eventName}</h3>
      {isLoading ? (
        <Spinner size="sm" />
      ) : (
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3">
          <div>
            <p className="text-gray-500">Photos</p>
            <p className="font-medium">{data?.totalPhotos ?? 0}</p>
          </div>
          <div>
            <p className="text-gray-500">Downloads</p>
            <p className="font-medium">{data?.totalDownloads ?? 0}</p>
          </div>
          <div>
            <p className="text-gray-500">Storage</p>
            <p className="font-medium">{data?.totalSizeHuman ?? '0 B'}</p>
          </div>
        </div>
      )}
    </Card>
  );
}
