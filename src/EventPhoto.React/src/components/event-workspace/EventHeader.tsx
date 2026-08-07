import { motion } from 'framer-motion';
import {
  Activity,
  ArrowLeft,
  Camera,
  CheckCircle,
  Download,
  ExternalLink,
  HardDrive,
  QrCode,
  ScanFace,
  Shield,
  XCircle,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import type { EventWorkspaceResponse } from '../../types';
import { EventActionsDropdown } from './EventActionsDropdown';

interface EventHeaderProps {
  workspace: EventWorkspaceResponse;
  onNavigateTab: (tab: string) => void;
}

const gradientMap: Record<string, string> = {
  Wedding: 'from-rose-900 via-pink-950 to-slate-950',
  Corporate: 'from-blue-900 via-indigo-950 to-slate-950',
  Birthday: 'from-amber-900 via-orange-950 to-slate-950',
  Concert: 'from-purple-900 via-violet-950 to-slate-950',
  Reception: 'from-emerald-900 via-teal-950 to-slate-950',
  Outdoor: 'from-green-900 via-emerald-950 to-slate-950',
};

function getGradient(type: string) {
  return gradientMap[type] ?? 'from-indigo-900 via-violet-950 to-slate-950';
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export function EventHeader({ workspace, onNavigateTab }: EventHeaderProps) {
  const gradient = getGradient(workspace.eventType);

  return (
    <motion.div
      initial={{ opacity: 0, y: -12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
      className={`relative rounded-2xl bg-gradient-to-br ${gradient} border border-white/5`}
    >
      {/* Subtle noise texture overlay */}
      <div className="pointer-events-none absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIzMDAiIGhlaWdodD0iMzAwIj48ZmlsdGVyIGlkPSJub2lzZSI+PGZlVHVyYnVsZW5jZSB0eXBlPSJmcmFjdGFsTm9pc2UiIGJhc2VGcmVxdWVuY3k9IjAuNjUiIG51bU9jdGF2ZXM9IjMiIHN0aXRjaFRpbGVzPSJzdGl0Y2giLz48ZmVDb2xvck1hdHJpeCB0eXBlPSJzYXR1cmF0ZSIgdmFsdWVzPSIwIi8+PC9maWx0ZXI+PHJlY3Qgd2lkdGg9IjMwMCIgaGVpZ2h0PSIzMDAiIGZpbHRlcj0idXJsKCNub2lzZSkiIG9wYWNpdHk9IjAuMDQiLz48L3N2Zz4=')] opacity-40" />

      <div className="relative px-6 pt-5 pb-6">
        {/* Back navigation */}
        <Link
          to="/admin/events"
          className="mb-5 inline-flex items-center gap-1.5 text-xs font-medium text-white/50 transition-colors hover:text-white/80"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Events
        </Link>

        <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
          {/* Left: identity */}
          <div className="min-w-0 flex-1">
            {/* Status badge */}
            <div className="mb-3 flex flex-wrap items-center gap-2">
              {workspace.isActive ? (
                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/15 px-2.5 py-1 text-xs font-semibold text-emerald-400 ring-1 ring-emerald-500/25">
                  <CheckCircle className="h-3 w-3" /> Active
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 rounded-full bg-slate-500/20 px-2.5 py-1 text-xs font-semibold text-slate-400 ring-1 ring-slate-500/30">
                  <XCircle className="h-3 w-3" /> Inactive
                </span>
              )}
              <span className="rounded-full bg-white/10 px-2.5 py-1 text-xs font-medium text-white/70">
                {workspace.eventType}
              </span>
              {workspace.enableFaceRecognition && (
                <span className="inline-flex items-center gap-1 rounded-full bg-violet-500/15 px-2.5 py-1 text-xs font-semibold text-violet-400 ring-1 ring-violet-500/25">
                  <ScanFace className="h-3 w-3" /> AI Search
                </span>
              )}
              {workspace.watermarkEnabled && (
                <span className="inline-flex items-center gap-1 rounded-full bg-amber-500/15 px-2.5 py-1 text-xs font-semibold text-amber-400 ring-1 ring-amber-500/25">
                  <Shield className="h-3 w-3" /> Watermark
                </span>
              )}
            </div>

            <h1 className="truncate text-2xl font-bold tracking-tight text-white sm:text-3xl">
              {workspace.name}
            </h1>

            <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-white/60">
              {workspace.clientName && (
                <span className="font-medium text-white/80">{workspace.clientName}</span>
              )}
              {workspace.venueName && <span>{workspace.venueName}</span>}
              <span>{formatDate(workspace.eventDate)}</span>
            </div>

            {/* Stats strip */}
            <div className="mt-5 flex flex-wrap gap-4">
              <StatPill icon={<Camera className="h-3.5 w-3.5" />} label="Photos" value={workspace.photoCount.toLocaleString()} onClick={() => onNavigateTab('gallery')} />
              <StatPill icon={<Download className="h-3.5 w-3.5" />} label="Downloads" value={workspace.totalDownloads.toLocaleString()} onClick={() => onNavigateTab('analytics')} />
              <StatPill icon={<HardDrive className="h-3.5 w-3.5" />} label="Storage" value={workspace.totalSize} onClick={() => onNavigateTab('storage')} />
            </div>
          </div>

          {/* Right: actions */}
          <div className="flex shrink-0 flex-wrap items-start gap-2 sm:flex-col sm:items-end">
            <a
              href={`/gallery/${workspace.id}`}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 rounded-xl bg-white/10 px-3.5 py-2 text-sm font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/20"
            >
              <ExternalLink className="h-4 w-4" /> Open Gallery
            </a>
            <button
              onClick={() => onNavigateTab('qr-access')}
              className="inline-flex items-center gap-2 rounded-xl bg-white/10 px-3.5 py-2 text-sm font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/20"
            >
              <QrCode className="h-4 w-4" /> View QR
            </button>
            <button
              onClick={() => onNavigateTab('analytics')}
              className="inline-flex items-center gap-2 rounded-xl bg-white/10 px-3.5 py-2 text-sm font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/20"
            >
              <Activity className="h-4 w-4" /> Analytics
            </button>
            <EventActionsDropdown eventId={workspace.id} isActive={workspace.isActive} />
          </div>
        </div>
      </div>
    </motion.div>
  );
}

function StatPill({
  icon,
  label,
  value,
  onClick,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  onClick?: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className="group flex items-center gap-2 rounded-xl bg-black/20 px-3 py-2 text-sm backdrop-blur-sm transition-colors hover:bg-black/30"
    >
      <span className="text-white/50 group-hover:text-white/70">{icon}</span>
      <span className="font-bold text-white">{value}</span>
      <span className="text-white/50">{label}</span>
    </button>
  );
}
