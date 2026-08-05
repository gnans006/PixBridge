import { useQuery } from '@tanstack/react-query';
import { Camera, ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import { eventsApi } from '../../api/events';
import { EventCard } from './EventCard';

export function EventGallery() {
  const { data, isLoading } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const r = await eventsApi.getAll();
      return r.data ?? [];
    },
    refetchInterval: 30_000,
  });

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-slate-400">Events</h2>
        <Link
          to="/admin/events"
          className="flex items-center gap-1 text-xs font-medium text-indigo-400 transition-colors hover:text-indigo-300"
        >
          View all <ChevronRight className="h-3.5 w-3.5" />
        </Link>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-52 animate-pulse rounded-2xl border border-slate-800 bg-slate-900" />
          ))}
        </div>
      ) : !data?.length ? (
        <div className="flex h-40 flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-slate-700">
          <Camera className="h-8 w-8 text-slate-600" />
          <p className="text-sm text-slate-500">No events yet</p>
          <Link to="/admin/events/new" className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500">
            Create First Event
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {data.map((event) => (
            <EventCard key={event.id} event={event} />
          ))}
        </div>
      )}
    </div>
  );
}
