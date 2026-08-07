import type { EventResponse } from '../../types';
import { buildApiUrl } from '../../api/client';
import { BarChart2, Camera, Download, Edit, ScanFace, Shield } from 'lucide-react';
import { Link } from 'react-router-dom';

interface Props {
  event: EventResponse;
}

const TYPE_GRADIENTS: Record<string, string> = {
  Wedding: 'from-rose-800 via-pink-900 to-slate-900',
  Corporate: 'from-blue-800 via-indigo-900 to-slate-900',
  Birthday: 'from-amber-800 via-orange-900 to-slate-900',
  Concert: 'from-purple-800 via-violet-900 to-slate-900',
  default: 'from-indigo-800 via-violet-900 to-slate-900',
};

export function EventCard({ event }: Props) {
  const gradient = TYPE_GRADIENTS[event.eventType] ?? TYPE_GRADIENTS.default;

  return (
    <div className="group relative flex flex-col overflow-hidden rounded-2xl border border-slate-800 bg-slate-900 transition-all duration-300 hover:-translate-y-0.5 hover:border-slate-700 hover:shadow-xl hover:shadow-black/40">
      {/* Cover */}
      <div className={`relative h-36 bg-gradient-to-br ${gradient}`}>
        <div className="absolute inset-0 flex items-center justify-center">
          <Camera className="h-10 w-10 text-white/20" />
        </div>

        {/* Status badge */}
        <div className="absolute left-3 top-3">
          <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ${event.isActive ? 'bg-emerald-500/90 text-white' : 'bg-slate-700 text-slate-300'}`}>
            <span className={`h-1.5 w-1.5 rounded-full ${event.isActive ? 'bg-white animate-pulse' : 'bg-slate-400'}`} />
            {event.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>

        {/* Type badge */}
        <div className="absolute right-3 top-3">
          <span className="rounded-full bg-black/40 px-2 py-0.5 text-xs text-slate-300 backdrop-blur-sm">
            {event.eventType}
          </span>
        </div>

        {/* Action buttons — always visible, lift slightly on hover */}
        <div className="absolute inset-x-0 bottom-0 flex gap-1.5 bg-gradient-to-t from-black/80 via-black/40 to-transparent px-3 pb-2.5 pt-8 transition-transform duration-200 group-hover:-translate-y-1">
          <Link
            to={`/gallery/${event.id}`}
            target="_blank"
            onClick={(e) => e.stopPropagation()}
            className="flex flex-1 items-center justify-center gap-1 rounded-lg bg-indigo-600/90 py-1 text-xs font-medium text-white hover:bg-indigo-500"
          >
            <Camera className="h-3 w-3" /> Open
          </Link>
          <Link
            to={`/admin/events/${event.id}`}
            onClick={(e) => e.stopPropagation()}
            className="flex flex-1 items-center justify-center gap-1 rounded-lg bg-slate-700/90 py-1 text-xs font-medium text-slate-200 hover:bg-slate-600"
          >
            <Edit className="h-3 w-3" /> Edit
          </Link>
          <Link
            to="/admin/statistics"
            onClick={(e) => e.stopPropagation()}
            className="flex flex-1 items-center justify-center gap-1 rounded-lg bg-slate-700/90 py-1 text-xs font-medium text-slate-200 hover:bg-slate-600"
          >
            <BarChart2 className="h-3 w-3" /> Stats
          </Link>
        </div>
      </div>

      {/* Body */}
      <div className="flex flex-1 flex-col p-4">
        <Link to={`/admin/events/${event.id}`} className="group/link">
          <h3 className="line-clamp-1 text-sm font-semibold text-slate-100 transition-colors group-hover/link:text-indigo-400">
            {event.name}
          </h3>
        </Link>
        <p className="mt-0.5 text-xs text-slate-500">
          {new Date(event.eventDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
          {event.clientName ? ` · ${event.clientName}` : ''}
        </p>

        {/* Metrics row */}
        <div className="mt-3 flex items-center gap-3 text-xs text-slate-400">
          <span className="flex items-center gap-1">
            <Camera className="h-3 w-3" /> {event.photoCount.toLocaleString()}
          </span>
          <span className="flex items-center gap-1">
            <Download className="h-3 w-3" /> {event.totalSize}
          </span>
        </div>

        {/* Feature badges */}
        <div className="mt-3 flex flex-wrap gap-1.5">
          {event.enableFaceRecognition && (
            <span className="flex items-center gap-0.5 rounded-full bg-amber-500/20 px-2 py-0.5 text-xs text-amber-400">
              <ScanFace className="h-3 w-3" /> AI
            </span>
          )}
          {event.allowGalleryBrowsing && (
            <span className="flex items-center gap-0.5 rounded-full bg-indigo-500/20 px-2 py-0.5 text-xs text-indigo-400">
              <Camera className="h-3 w-3" /> Gallery
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
