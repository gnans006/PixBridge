import { motion, AnimatePresence } from 'framer-motion';
import {
  Bot,
  Calendar,
  CheckCircle2,
  Droplets,
  Image,
  Layers,
  Tag,
} from 'lucide-react';
import type { GalleryMode } from './GalleryModeCards';

const GALLERY_MODE_LABELS: Record<GalleryMode, string> = {
  GalleryOnly: 'Gallery Only',
  FaceSearchOnly: 'Face Search Only',
  HybridMode: 'Hybrid Mode',
};

const EVENT_TYPE_ICONS: Record<string, string> = {
  Wedding: '💍',
  Reception: '🥂',
  Birthday: '🎂',
  Corporate: '💼',
  Outdoor: '🌿',
  Other: '📷',
};

interface SummaryRowProps {
  icon: React.ReactNode;
  label: string;
  value: string;
  highlight?: boolean;
}

function SummaryRow({ icon, label, value, highlight }: SummaryRowProps) {
  return (
    <motion.div
      layout
      className={`flex items-start gap-3 rounded-xl p-3 transition-colors ${
        highlight ? 'bg-indigo-500/8' : 'bg-slate-800/40'
      }`}
    >
      <span className="mt-0.5 shrink-0 text-slate-400">{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-[11px] text-slate-500">{label}</p>
        <p className="mt-0.5 truncate text-sm font-medium text-slate-200">{value || '—'}</p>
      </div>
    </motion.div>
  );
}

function StatusPill({ active, activeLabel, inactiveLabel }: { active: boolean; activeLabel: string; inactiveLabel: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
        active ? 'bg-emerald-500/15 text-emerald-400' : 'bg-slate-700 text-slate-500'
      }`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${active ? 'bg-emerald-400' : 'bg-slate-500'}`} />
      {active ? activeLabel : inactiveLabel}
    </span>
  );
}

interface EventSummarySidebarProps {
  coverImage: string | null;
  name: string;
  eventType: string;
  eventDate: string;
  galleryMode: GalleryMode;
  faceRecognitionEnabled: boolean;
  watermarkEnabled: boolean;
  isReady: boolean;
}

export function EventSummarySidebar({
  coverImage,
  name,
  eventType,
  eventDate,
  galleryMode,
  faceRecognitionEnabled,
  watermarkEnabled,
  isReady,
}: EventSummarySidebarProps) {
  const formattedDate = eventDate
    ? (() => {
        const d = new Date(eventDate + 'T00:00:00');
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
      })()
    : '';

  const typeIcon = EVENT_TYPE_ICONS[eventType] ?? '📷';

  return (
    <div className="sticky top-8 space-y-4">
      {/* Summary card */}
      <div className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
        {/* Cover image or placeholder */}
        <div className="relative h-36 bg-gradient-to-br from-indigo-900/40 via-slate-800 to-violet-900/30">
          <AnimatePresence>
            {coverImage && (
              <motion.img
                key={coverImage}
                src={coverImage}
                alt="Event cover"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.3 }}
                className="absolute inset-0 h-full w-full object-cover"
              />
            )}
          </AnimatePresence>
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900/90 via-slate-900/30 to-transparent" />
          {!coverImage && (
            <div className="absolute inset-0 flex items-center justify-center">
              <Image className="h-10 w-10 text-slate-700" />
            </div>
          )}
          <div className="absolute bottom-3 left-4 right-4">
            <p className="truncate text-base font-bold text-white drop-shadow">
              {name || 'Event Name'}
            </p>
          </div>
        </div>

        {/* Summary rows */}
        <div className="space-y-1.5 p-4">
          <SummaryRow
            icon={<span className="text-sm">{typeIcon}</span>}
            label="Event Type"
            value={eventType || 'Not selected'}
          />
          <SummaryRow
            icon={<Calendar className="h-4 w-4" />}
            label="Event Date"
            value={formattedDate || 'Not set'}
          />
          <SummaryRow
            icon={<Layers className="h-4 w-4" />}
            label="Gallery Mode"
            value={GALLERY_MODE_LABELS[galleryMode]}
          />

          {/* Status pills */}
          <div className="flex flex-wrap gap-2 pt-1">
            <div className="flex items-center gap-1.5">
              <Bot className="h-3.5 w-3.5 text-slate-500" />
              <StatusPill active={faceRecognitionEnabled} activeLabel="Face Search" inactiveLabel="No AI" />
            </div>
            <div className="flex items-center gap-1.5">
              <Droplets className="h-3.5 w-3.5 text-slate-500" />
              <StatusPill active={watermarkEnabled} activeLabel="Watermark On" inactiveLabel="No Watermark" />
            </div>
          </div>
        </div>

        {/* Ready indicator */}
        <div className="border-t border-slate-800 px-4 py-3">
          <AnimatePresence mode="wait">
            {isReady ? (
              <motion.div
                key="ready"
                initial={{ opacity: 0, y: 4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                className="flex items-center gap-2"
              >
                <CheckCircle2 className="h-4 w-4 text-emerald-400" />
                <p className="text-sm font-medium text-emerald-400">Ready to Create</p>
              </motion.div>
            ) : (
              <motion.div
                key="not-ready"
                initial={{ opacity: 0, y: 4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                className="flex items-center gap-2"
              >
                <Tag className="h-4 w-4 text-slate-500" />
                <p className="text-sm text-slate-500">Fill in the required fields</p>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>

      {/* Tips card */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-4">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Quick Tips</p>
        <ul className="space-y-2">
          {[
            'Watermark can be configured before or after creating the event.',
            'Face search indexes run in the background after thumbnail generation.',
            'Watch folder is monitored automatically for new photos.',
          ].map((tip) => (
            <li key={tip} className="flex items-start gap-2 text-xs text-slate-500">
              <span className="mt-0.5 text-indigo-500">•</span>
              {tip}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
