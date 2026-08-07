import { motion, AnimatePresence } from 'framer-motion';
import { Brain, Download, ScanFace, Zap } from 'lucide-react';
import type { UseFormWatch, UseFormSetValue } from 'react-hook-form';
import { SectionCard } from './SectionCard';

interface AiFeaturesSectionProps {
  watch: UseFormWatch<any>;
  setValue: UseFormSetValue<any>;
  faceSearchRequired: boolean; // true when gallery mode forces face recognition on
}

interface ToggleRowProps {
  id: string;
  icon: React.ReactNode;
  label: string;
  description: string;
  checked: boolean;
  onChange: (v: boolean) => void;
  disabled?: boolean;
  disabledReason?: string;
}

function ToggleRow({ id, icon, label, description, checked, onChange, disabled, disabledReason }: ToggleRowProps) {
  return (
    <div
      className={`flex items-start gap-3 rounded-xl border p-3.5 transition-colors ${
        checked ? 'border-indigo-500/40 bg-indigo-500/5' : 'border-slate-700 bg-slate-800/40'
      } ${disabled ? 'opacity-60' : ''}`}
    >
      <span className={`mt-0.5 shrink-0 ${checked ? 'text-indigo-400' : 'text-slate-500'}`}>{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-slate-200">{label}</p>
        <p className="mt-0.5 text-xs text-slate-500">
          {disabled && disabledReason ? disabledReason : description}
        </p>
      </div>
      <button
        type="button"
        id={id}
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-900 disabled:cursor-not-allowed ${
          checked ? 'bg-indigo-600' : 'bg-slate-700'
        }`}
        aria-label={label}
      >
        <motion.span
          layout
          transition={{ type: 'spring', stiffness: 600, damping: 30 }}
          className={`inline-block h-4 w-4 rounded-full bg-white shadow ${checked ? 'translate-x-4' : 'translate-x-0'}`}
        />
      </button>
    </div>
  );
}

export function AiFeaturesSection({ watch, setValue, faceSearchRequired }: AiFeaturesSectionProps) {
  const enableFaceRecognition = watch('enableFaceRecognition') as boolean;
  const restrictDownloads = watch('restrictDownloadsToMatchedPhotos') as boolean;
  const threshold = (watch('faceMatchThreshold') as number) ?? 0.75;

  return (
    <SectionCard
      icon={<Brain className="h-4 w-4" />}
      title="AI Features"
      subtitle="Face recognition and download controls"
      defaultOpen={false}
    >
      <div className="space-y-3">
        {/* 2-column grid for main toggles */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <ToggleRow
            id="toggle-face-recognition"
            icon={<ScanFace className="h-4 w-4" />}
            label="Enable Face Search"
            description="AI indexes faces after thumbnail generation"
            checked={enableFaceRecognition}
            onChange={(v) => setValue('enableFaceRecognition', v, { shouldValidate: true })}
            disabled={faceSearchRequired}
            disabledReason="Required by the selected gallery mode"
          />

          <ToggleRow
            id="toggle-auto-index"
            icon={<Zap className="h-4 w-4" />}
            label="Auto Index Photos"
            description="New photos are indexed automatically in the background"
            checked={enableFaceRecognition}
            onChange={() => {/* auto-indexing is tied to face recognition */}}
            disabled
            disabledReason="Tied to Face Search — enabled together"
          />

          <ToggleRow
            id="toggle-restrict-downloads"
            icon={<Download className="h-4 w-4" />}
            label="Download Restriction"
            description="Guests can only download their matched photos"
            checked={restrictDownloads}
            onChange={(v) => setValue('restrictDownloadsToMatchedPhotos', v, { shouldValidate: true })}
            disabled={!enableFaceRecognition}
            disabledReason="Requires Face Search to be enabled"
          />
        </div>

        {/* Match Threshold slider — shown only when face recognition is active */}
        <AnimatePresence>
          {enableFaceRecognition && (
            <motion.div
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: 'auto' }}
              exit={{ opacity: 0, height: 0 }}
              transition={{ duration: 0.2, ease: 'easeInOut' }}
              className="overflow-hidden"
            >
              <div className="rounded-xl border border-slate-700 bg-slate-800/40 p-4">
                <div className="mb-3 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-200">Match Threshold</p>
                    <p className="mt-0.5 text-xs text-slate-500">Higher = stricter matching. Recommended: 0.75 – 0.85</p>
                  </div>
                  <span className="rounded-lg bg-indigo-500/15 px-2.5 py-1 font-mono text-sm font-bold text-indigo-400">
                    {threshold.toFixed(2)}
                  </span>
                </div>
                <input
                  type="range"
                  min={0.5}
                  max={0.99}
                  step={0.01}
                  value={threshold}
                  onChange={(e) =>
                    setValue('faceMatchThreshold', parseFloat(e.target.value), { shouldValidate: true })
                  }
                  className="w-full accent-indigo-500"
                  aria-label="Face match threshold"
                />
                <div className="mt-1.5 flex justify-between text-[10px] text-slate-600">
                  <span>0.50 (loose)</span>
                  <span>0.99 (strict)</span>
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Info hint */}
        <p className="flex items-start gap-1.5 text-xs text-slate-500">
          <Zap className="mt-0.5 h-3 w-3 shrink-0 text-amber-500" />
          Photos are analyzed in the background after thumbnail generation completes.
        </p>
      </div>
    </SectionCard>
  );
}
