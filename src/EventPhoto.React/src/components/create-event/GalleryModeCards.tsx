import { AnimatePresence, motion } from 'framer-motion';
import { AlertCircle, CheckCircle2, Grid3X3, Layers, ScanFace } from 'lucide-react';

export type GalleryMode = 'GalleryOnly' | 'FaceSearchOnly' | 'HybridMode';

const MODES: {
  id: GalleryMode;
  icon: React.ReactNode;
  title: string;
  description: string;
  badge?: string;
}[] = [
  {
    id: 'GalleryOnly',
    icon: <Grid3X3 className="h-5 w-5" />,
    title: 'Gallery Only',
    description: 'Guests browse all photos',
  },
  {
    id: 'FaceSearchOnly',
    icon: <ScanFace className="h-5 w-5" />,
    title: 'Face Search Only',
    description: 'Upload selfie to find photos',
    badge: 'AI',
  },
  {
    id: 'HybridMode',
    icon: <Layers className="h-5 w-5" />,
    title: 'Hybrid Mode',
    description: 'Browse + Face Search',
    badge: 'AI',
  },
];

interface GalleryModeCardsProps {
  value: GalleryMode;
  onChange: (mode: GalleryMode) => void;
  error?: string;
}

export function GalleryModeCards({ value, onChange, error }: GalleryModeCardsProps) {
  return (
    <div>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {MODES.map((mode) => {
          const isSelected = value === mode.id;
          return (
            <motion.button
              key={mode.id}
              type="button"
              onClick={() => onChange(mode.id)}
              whileHover={{ scale: 1.015 }}
              whileTap={{ scale: 0.985 }}
              className={`relative flex flex-col gap-2.5 rounded-xl border p-4 text-left outline-none transition-all focus-visible:ring-2 focus-visible:ring-indigo-500 ${
                isSelected
                  ? 'border-indigo-500 bg-indigo-500/10 shadow-[0_0_20px_rgba(99,102,241,0.18)]'
                  : 'border-slate-700 bg-slate-800/60 hover:border-slate-600 hover:bg-slate-800'
              }`}
              aria-pressed={isSelected}
              aria-label={`Select ${mode.title} gallery mode`}
            >
              {/* AI badge */}
              {mode.badge && (
                <span className="absolute right-3 top-3 rounded-full bg-violet-500/20 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-violet-400">
                  {mode.badge}
                </span>
              )}

              {/* Icon */}
              <span className={`transition-colors ${isSelected ? 'text-indigo-400' : 'text-slate-500'}`}>
                {mode.icon}
              </span>

              {/* Text */}
              <div>
                <p className={`text-sm font-semibold transition-colors ${isSelected ? 'text-slate-100' : 'text-slate-300'}`}>
                  {mode.title}
                </p>
                <p className="mt-0.5 text-xs text-slate-500">{mode.description}</p>
              </div>

              {/* Selection checkmark */}
              <AnimatePresence>
                {isSelected && (
                  <motion.span
                    key="check"
                    initial={{ scale: 0, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    exit={{ scale: 0, opacity: 0 }}
                    transition={{ type: 'spring', stiffness: 500, damping: 25 }}
                    className="absolute bottom-3 right-3 text-indigo-400"
                  >
                    <CheckCircle2 className="h-4 w-4" />
                  </motion.span>
                )}
              </AnimatePresence>
            </motion.button>
          );
        })}
      </div>
      {error && (
        <p className="mt-2 flex items-center gap-1.5 text-sm font-medium text-rose-400">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          {error}
        </p>
      )}
    </div>
  );
}
