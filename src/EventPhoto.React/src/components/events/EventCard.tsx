import { motion } from 'framer-motion';
import { Camera, HardDrive, ImageIcon, ScanFace } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { EventResponse } from '../../types';
import { formatDate } from '../../utils/format';
import { EventCardMenu } from './EventCardMenu';

const TYPE_GRADIENTS: Record<string, string> = {
  Wedding:   'from-rose-800   via-pink-900   to-slate-950',
  Corporate: 'from-blue-800   via-indigo-900 to-slate-950',
  Birthday:  'from-amber-800  via-orange-900 to-slate-950',
  Concert:   'from-purple-800 via-violet-900 to-slate-950',
  Reception: 'from-emerald-800 via-teal-900  to-slate-950',
  Outdoor:   'from-green-800  via-emerald-900 to-slate-950',
};
const DEFAULT_GRADIENT = 'from-indigo-800 via-violet-900 to-slate-950';

const cardVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: (i: number) => ({
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.4,
      delay: Math.min(i * 0.05, 0.4),
      ease: [0.25, 0.1, 0.25, 1] as [number, number, number, number],
    },
  }),
};

interface EventCardProps {
  event: EventResponse;
  onDelete: (id: string) => void;
  onToggleActive: (id: string, activate: boolean) => void;
  onRefreshQr: (id: string) => void;
  refreshingQrId: string | null;
  view?: 'gallery' | 'compact';
  index?: number;
}

export function EventCard({
  event, onDelete, onToggleActive, onRefreshQr, refreshingQrId, view = 'gallery', index = 0,
}: EventCardProps) {
  const gradient = TYPE_GRADIENTS[event.eventType] ?? DEFAULT_GRADIENT;

  /* ── Compact row view ──────────────────────────────────────────────── */
  if (view === 'compact') {
    return (
      <motion.div
        custom={index}
        variants={cardVariants}
        initial="hidden"
        animate="visible"
        className="group flex items-center gap-4 rounded-2xl border border-slate-800 bg-slate-900 p-4 transition-all duration-200 hover:border-slate-700 hover:bg-slate-800/50 hover:shadow-lg hover:shadow-black/30"
      >
        {/* Mini cover */}
        <div className={`relative h-14 w-20 shrink-0 overflow-hidden rounded-xl bg-gradient-to-br ${gradient}`}>
          <Camera className="absolute inset-0 m-auto h-5 w-5 text-white/20" />
        </div>

        {/* Info */}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <Link
              to={`/admin/events/${event.id}`}
              className="line-clamp-1 font-semibold text-slate-100 transition-colors hover:text-indigo-400"
            >
              {event.name}
            </Link>
            <span
              className={`shrink-0 rounded-full px-2 py-px text-[10px] font-semibold ${
                event.isActive
                  ? 'bg-emerald-500/20 text-emerald-400'
                  : 'bg-slate-700 text-slate-400'
              }`}
            >
              {event.isActive ? '● Active' : 'Inactive'}
            </span>
          </div>
          <p className="mt-0.5 truncate text-xs text-slate-500">
            {formatDate(event.eventDate)}{event.clientName ? ` · ${event.clientName}` : ''}
          </p>
          <div className="mt-1.5 flex items-center gap-3 text-xs text-slate-400">
            <span className="flex items-center gap-1">
              <Camera className="h-3 w-3" /> {event.photoCount.toLocaleString()}
            </span>
            <span className="flex items-center gap-1">
              <HardDrive className="h-3 w-3" /> {event.totalSize}
            </span>
            {event.enableFaceRecognition ? (
              <span className="flex items-center gap-1 text-amber-400">
                <ScanFace className="h-3 w-3" /> AI
              </span>
            ) : null}
          </div>
        </div>

        <EventCardMenu
          event={event}
          onDelete={onDelete}
          onToggleActive={onToggleActive}
          onRefreshQr={onRefreshQr}
          refreshingQrId={refreshingQrId}
        />
      </motion.div>
    );
  }

  /* ── Gallery card view ─────────────────────────────────────────────── */
  return (
    <motion.div
      custom={index}
      variants={cardVariants}
      initial="hidden"
      animate="visible"
      whileHover={{ scale: 1.025, y: -4, transition: { type: 'spring', stiffness: 320, damping: 22 } }}
      className="group relative flex flex-col overflow-hidden rounded-2xl border border-slate-800 bg-slate-900 hover:border-slate-600 hover:shadow-2xl hover:shadow-black/60"
    >
      {/* Cover ─────────────────────────────────────────────────────── */}
      <div className={`relative h-44 overflow-hidden bg-gradient-to-br ${gradient}`}>
        {/* Decorative icon zooms on hover */}
        <Camera className="absolute inset-0 m-auto h-12 w-12 text-white/10 transition-transform duration-500 group-hover:scale-125" />

        {/* Gradient overlay darkens on hover */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent transition-opacity duration-300 group-hover:opacity-80" />

        {/* Top-left: live status badge */}
        <div className="absolute left-3 top-3 z-10">
          <span
            className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold backdrop-blur-sm ${
              event.isActive
                ? 'bg-emerald-500/85 text-white'
                : 'bg-slate-800/80 text-slate-400'
            }`}
          >
            <span
              className={`h-1.5 w-1.5 rounded-full ${
                event.isActive ? 'animate-pulse bg-white' : 'bg-slate-500'
              }`}
            />
            {event.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>

        {/* Top-right: context menu */}
        <div className="absolute right-2.5 top-2.5 z-10">
          <EventCardMenu
            event={event}
            onDelete={onDelete}
            onToggleActive={onToggleActive}
            onRefreshQr={onRefreshQr}
            refreshingQrId={refreshingQrId}
          />
        </div>

        {/* Bottom hover action bar — slides up on hover */}
        <div className="absolute inset-x-0 bottom-0 flex translate-y-1 gap-2 bg-gradient-to-t from-black/85 via-black/40 to-transparent px-3 pb-3 pt-14 opacity-0 transition-all duration-250 group-hover:translate-y-0 group-hover:opacity-100">
          <a
            href={`/gallery/${event.id}`}
            target="_blank"
            rel="noreferrer"
            onClick={(e) => e.stopPropagation()}
            className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-indigo-600/90 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-indigo-500"
          >
            <ImageIcon className="h-3 w-3" /> Gallery
          </a>
          <Link
            to={`/admin/events/${event.id}`}
            onClick={(e) => e.stopPropagation()}
            className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-slate-700/90 py-1.5 text-xs font-semibold text-slate-200 backdrop-blur-sm transition-colors hover:bg-slate-600"
          >
            <Camera className="h-3 w-3" /> Manage
          </Link>
        </div>
      </div>

      {/* Body ──────────────────────────────────────────────────────── */}
      <div className="flex flex-1 flex-col p-4">
        {/* Name */}
        <Link to={`/admin/events/${event.id}`} className="group/title">
          <h3 className="line-clamp-1 font-semibold text-slate-100 transition-colors group-hover/title:text-indigo-400">
            {event.name}
          </h3>
        </Link>

        {/* Date + client */}
        <p className="mt-1 truncate text-xs text-slate-500">
          {formatDate(event.eventDate)}
          {event.clientName ? ` · ${event.clientName}` : ''}
        </p>

        {/* Type + feature badges */}
        <div className="mt-2.5 flex flex-wrap items-center gap-1.5">
          <span className="rounded-full bg-slate-800 px-2 py-px text-[11px] text-slate-400">
            {event.eventType}
          </span>
          {event.enableFaceRecognition ? (
            <span className="flex items-center gap-1 rounded-full bg-amber-500/15 px-2 py-px text-[11px] font-medium text-amber-400">
              <ScanFace className="h-2.5 w-2.5" /> AI
            </span>
          ) : null}
          {event.allowGalleryBrowsing ? (
            <span className="flex items-center gap-1 rounded-full bg-indigo-500/15 px-2 py-px text-[11px] font-medium text-indigo-400">
              <ImageIcon className="h-2.5 w-2.5" /> Gallery
            </span>
          ) : null}
        </div>

        {/* Metrics footer */}
        <div className="mt-3 flex items-center gap-4 border-t border-slate-800 pt-3 text-xs text-slate-400">
          <span className="flex items-center gap-1.5">
            <Camera className="h-3.5 w-3.5 text-slate-500" />
            <span className="font-medium text-slate-300">{event.photoCount.toLocaleString()}</span>
          </span>
          <span className="flex items-center gap-1.5">
            <HardDrive className="h-3.5 w-3.5 text-slate-500" />
            <span className="font-medium text-slate-300">{event.totalSize}</span>
          </span>
        </div>
      </div>
    </motion.div>
  );
}
