import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Upload, Calendar, Camera, ArrowRight, Loader2 } from 'lucide-react';
import { eventsApi } from '../../api/events';

export default function GuestUploadsOverviewPage() {
  const navigate = useNavigate();

  const { data: response, isLoading, isError } = useQuery({
    queryKey: ['events-list'],
    queryFn: () => eventsApi.getAll(),
  });

  const events = response?.data ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-100 flex items-center gap-2">
          <Upload className="h-6 w-6 text-indigo-400" />
          Guest Uploads
        </h1>
        <p className="text-slate-400 text-sm mt-1">
          Select an event to manage guest upload sessions and moderate submitted photos.
        </p>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-16 text-slate-500">
          <Loader2 className="h-6 w-6 animate-spin mr-2" />
          <span className="text-sm">Loading events…</span>
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="rounded-xl border border-red-900/40 bg-red-950/20 p-6 text-center text-sm text-red-400">
          Failed to load events. Please refresh the page.
        </div>
      )}

      {/* Empty */}
      {!isLoading && !isError && events.length === 0 && (
        <div className="rounded-xl border border-slate-800 bg-slate-900 p-10 text-center text-slate-500">
          <Upload className="h-8 w-8 mx-auto mb-3 opacity-40" />
          <p className="text-sm">No events found. Create an event first to enable guest uploads.</p>
        </div>
      )}

      {/* Event grid */}
      {events.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {events.map(event => (
            <button
              key={event.id}
              onClick={() => navigate(`/admin/experiences/guest-uploads/${event.id}`)}
              className="group text-left rounded-xl border border-slate-800 bg-slate-900 p-5 hover:border-indigo-700/60 hover:bg-slate-800/70 transition-all duration-200"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-slate-100 truncate group-hover:text-indigo-300 transition-colors">
                    {event.name}
                  </p>
                  {event.clientName && (
                    <p className="text-xs text-slate-500 mt-0.5 truncate">{event.clientName}</p>
                  )}
                </div>
                <span
                  className={`shrink-0 text-xs font-medium px-2 py-0.5 rounded-full border ${
                    event.isActive
                      ? 'text-green-400 bg-green-400/10 border-green-400/20'
                      : 'text-slate-500 bg-slate-800 border-slate-700'
                  }`}
                >
                  {event.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                <span className="flex items-center gap-1">
                  <Calendar className="h-3 w-3" />
                  {new Date(event.eventDate).toLocaleDateString()}
                </span>
                <span className="flex items-center gap-1">
                  <Camera className="h-3 w-3" />
                  {event.photoCount.toLocaleString()} photos
                </span>
              </div>

              <div className="mt-4 flex items-center justify-end text-xs text-indigo-400 group-hover:text-indigo-300 transition-colors">
                Manage uploads
                <ArrowRight className="h-3.5 w-3.5 ml-1 group-hover:translate-x-0.5 transition-transform" />
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
