import { BarChart2, Calendar, Camera, Download, Edit, ExternalLink, HardDrive, ScanFace, Shield } from 'lucide-react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import type { EventSpotlightResponse } from '../../types';
import { formatDate } from '../../utils/format';

const TYPE_GRADIENTS: Record<string, string> = {
  Wedding:   'from-rose-800   via-pink-900   to-slate-950',
  Corporate: 'from-blue-800   via-indigo-900 to-slate-950',
  Birthday:  'from-amber-800  via-orange-900 to-slate-950',
  Concert:   'from-purple-800 via-violet-900 to-slate-950',
  Reception: 'from-emerald-800 via-teal-900  to-slate-950',
  Outdoor:   'from-green-800  via-emerald-900 to-slate-950',
};
const DEFAULT_GRADIENT = 'from-indigo-800 via-violet-900 to-slate-950';

interface EventSpotlightProps {
  data: EventSpotlightResponse;
}

export function EventSpotlight({ data }: EventSpotlightProps) {
  const gradient = TYPE_GRADIENTS[data.eventType] ?? DEFAULT_GRADIENT;

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5, ease: [0.25, 0.1, 0.25, 1] }}
      className="relative min-h-72 overflow-hidden rounded-2xl border border-slate-800"
    >
      {/* ── Background layer ─────────────────────────────────── */}
      <div className={`absolute inset-0 bg-gradient-to-br ${gradient}`} aria-hidden="true">
        {data.firstThumbnailUrl ? (
          <img
            src={data.firstThumbnailUrl}
            alt=""
            className="absolute inset-0 h-full w-full object-cover opacity-25 mix-blend-luminosity"
          />
        ) : null}
      </div>

      {/* Gradient overlays */}
      <div className="absolute inset-0 bg-gradient-to-r  from-slate-950/90 via-slate-950/60 to-slate-950/10" />
      <div className="absolute inset-0 bg-gradient-to-t  from-slate-950/70 via-transparent to-transparent" />

      {/* ── Content ──────────────────────────────────────────── */}
      <div className="relative flex flex-col gap-6 p-6 md:flex-row md:items-end md:justify-between md:p-8 md:pt-10">
        {/* Left: info */}
        <div className="max-w-2xl flex-1">
          {/* Label row */}
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 rounded-full bg-indigo-500/20 px-3 py-1 text-xs font-semibold text-indigo-300">
              ✦ Featured Event
            </span>
            {data.isActive ? (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/20 px-2.5 py-1 text-xs font-semibold text-emerald-400">
                <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400" />
                Live
              </span>
            ) : null}
          </div>

          {/* Title */}
          <h2 className="mt-3 text-3xl font-bold leading-tight text-white md:text-4xl">
            {data.name}
          </h2>

          {/* Meta */}
          <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-slate-400">
            <span className="flex items-center gap-1.5">
              <Calendar className="h-3.5 w-3.5" />
              {formatDate(data.eventDate)}
            </span>
            {data.clientName ? <span>· {data.clientName}</span> : null}
            {data.venueName  ? <span>· {data.venueName}</span>  : null}
          </div>

          {/* Stats row */}
          <div className="mt-5 flex flex-wrap gap-6 text-sm">
            <div className="flex items-center gap-2 text-slate-200">
              <Camera className="h-4 w-4 text-indigo-400" />
              <span className="font-semibold">{data.photoCount.toLocaleString()}</span>
              <span className="text-slate-500">photos</span>
            </div>
            <div className="flex items-center gap-2 text-slate-200">
              <Download className="h-4 w-4 text-emerald-400" />
              <span className="font-semibold">{data.totalDownloads.toLocaleString()}</span>
              <span className="text-slate-500">downloads</span>
            </div>
            <div className="flex items-center gap-2 text-slate-200">
              <HardDrive className="h-4 w-4 text-violet-400" />
              <span className="font-semibold">{data.storageHuman}</span>
              <span className="text-slate-500">storage</span>
            </div>
          </div>

          {/* Status badges */}
          <div className="mt-4 flex flex-wrap gap-2">
            {data.faceRecognitionEnabled ? (
              <span className="flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-xs font-medium text-amber-400">
                <ScanFace className="h-3.5 w-3.5" /> Face AI
              </span>
            ) : null}
            {data.watermarkEnabled ? (
              <span className="flex items-center gap-1.5 rounded-full bg-slate-700/60 px-3 py-1 text-xs font-medium text-slate-300">
                <Shield className="h-3.5 w-3.5" /> Watermarked
              </span>
            ) : null}
            <span className="rounded-full bg-slate-700/60 px-3 py-1 text-xs font-medium text-slate-300">
              {data.eventType}
            </span>
          </div>
        </div>

        {/* Right: actions */}
        <div className="flex shrink-0 flex-row flex-wrap gap-2 md:flex-col md:flex-nowrap">
          <a
            href={`/gallery/${data.eventId}`}
            target="_blank"
            rel="noreferrer"
            className="flex items-center justify-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-600/25 transition-colors hover:bg-indigo-500"
          >
            <ExternalLink className="h-4 w-4" /> Open Gallery
          </a>
          <Link
            to="/admin/statistics"
            className="flex items-center justify-center gap-2 rounded-xl border border-slate-600 bg-slate-800/60 px-5 py-2.5 text-sm font-medium text-slate-200 backdrop-blur-sm transition-colors hover:bg-slate-700 hover:text-white"
          >
            <BarChart2 className="h-4 w-4" /> Analytics
          </Link>
          <Link
            to={`/admin/events/${data.eventId}`}
            className="flex items-center justify-center gap-2 rounded-xl border border-slate-600 bg-slate-800/60 px-5 py-2.5 text-sm font-medium text-slate-200 backdrop-blur-sm transition-colors hover:bg-slate-700 hover:text-white"
          >
            <Edit className="h-4 w-4" /> Edit
          </Link>
        </div>
      </div>
    </motion.div>
  );
}
