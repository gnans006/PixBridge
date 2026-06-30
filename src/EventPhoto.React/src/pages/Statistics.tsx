import { useQuery } from '@tanstack/react-query';
import { eventsApi } from '../api/events';
import { statisticsApi } from '../api/statistics';
import { Card } from '../components/UI/Card';
import { Spinner } from '../components/UI/Spinner';
import { formatDateTime } from '../utils/format';

export default function Statistics() {
  const { data: events, isLoading } = useQuery({
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
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-5">
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
          <div>
            <p className="text-gray-500">Pending Thumbs</p>
            <p className="font-medium">{data?.thumbnailsPending ?? 0}</p>
          </div>
          <div>
            <p className="text-gray-500">Last Photo</p>
            <p className="font-medium">{data?.lastPhotoAt ? formatDateTime(data.lastPhotoAt) : 'N/A'}</p>
          </div>
        </div>
      )}
    </Card>
  );
}
