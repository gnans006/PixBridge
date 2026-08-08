import { motion, AnimatePresence } from 'framer-motion';
import { X, Droplets, Layers, Palette, Type, Info } from 'lucide-react';
import type { UpsertWatermarkConfigRequest, WatermarkMode, WatermarkStyle, WatermarkScale } from '../../types';
import { WatermarkPreviewPanel } from './WatermarkPreviewPanel';

// ── Constants ─────────────────────────────────────────────────────────────────

const MODES: { value: WatermarkMode; label: string; description: string }[] = [
  { value: 'StudioBranding', label: 'Studio Branding', description: 'Studio name + logo' },
  { value: 'EventBranding', label: 'Event Branding', description: 'Event name + date' },
  { value: 'StudioAndEvent', label: 'Studio + Event', description: 'Combined branding' },
  { value: 'CustomText', label: 'Custom Text', description: 'Your own text' },
  { value: 'DynamicTemplate', label: 'Dynamic Template', description: 'Token-based template' },
];

const STYLES: { value: WatermarkStyle; label: string; icon: string }[] = [
  { value: 'BottomRibbon', label: 'Bottom Ribbon', icon: '▬' },
  { value: 'Corner', label: 'Corner', icon: '↘' },
  { value: 'Center', label: 'Center', icon: '⊕' },
  { value: 'Diagonal', label: 'Diagonal', icon: '↗' },
  { value: 'RepeatedPattern', label: 'Repeated', icon: '⊞' },
];

const SCALES: { value: WatermarkScale; label: string }[] = [
  { value: 'Small', label: 'Small (3%)' },
  { value: 'Medium', label: 'Medium (5%)' },
  { value: 'Large', label: 'Large (8%)' },
  { value: 'Auto', label: 'Auto' },
];

const TOKENS = ['{StudioName}', '{EventName}', '{EventDate}', '{DownloadDate}'];

const SWATCHES = [
  { hex: '#ffffff', label: 'White' },
  { hex: '#000000', label: 'Black' },
  { hex: '#fbbf24', label: 'Gold' },
  { hex: '#f97316', label: 'Orange' },
  { hex: '#3b82f6', label: 'Blue' },
  { hex: '#a855f7', label: 'Purple' },
];

// ── Sub-components ─────────────────────────────────────────────────────────────

function ConfigSection({
  icon,
  title,
  children,
}: {
  icon?: React.ReactNode;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div className="mb-2 flex items-center gap-1.5">
        {icon && <span className="text-slate-400">{icon}</span>}
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">{title}</p>
      </div>
      {children}
    </div>
  );
}

const INPUT_CLASS =
  'w-full rounded-lg border border-slate-700 bg-slate-800 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500';

// ── Props ──────────────────────────────────────────────────────────────────────

interface CreateEventWatermarkModalProps {
  isOpen: boolean;
  onClose: () => void;
  config: UpsertWatermarkConfigRequest;
  onChange: (config: UpsertWatermarkConfigRequest) => void;
  eventName: string;
  studioName?: string;
}

// ── Component ──────────────────────────────────────────────────────────────────

export function CreateEventWatermarkModal({
  isOpen,
  onClose,
  config,
  onChange,
  eventName,
  studioName,
}: CreateEventWatermarkModalProps) {
  const set = <K extends keyof UpsertWatermarkConfigRequest>(
    key: K,
    value: UpsertWatermarkConfigRequest[K],
  ) => onChange({ ...config, [key]: value });

  const isBrandingMode =
    config.mode === 'StudioBranding' ||
    config.mode === 'EventBranding' ||
    config.mode === 'StudioAndEvent';

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.18 }}
            className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm"
            onClick={onClose}
            aria-hidden="true"
          />

          {/* Dialog */}
          <motion.div
            key="dialog"
            initial={{ opacity: 0, scale: 0.96, y: 12 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 12 }}
            transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4"
            role="presentation"
          >
            <div
              className="relative flex max-h-[90vh] w-full max-w-5xl flex-col overflow-hidden rounded-2xl border border-slate-700 bg-slate-900 shadow-2xl"
              role="dialog"
              aria-modal="true"
              aria-labelledby="wm-modal-title"
              onClick={(e) => e.stopPropagation()}
            >
              {/* Header */}
              <div className="flex items-center justify-between border-b border-slate-800 px-6 py-4">
                <div className="flex items-center gap-3">
                  <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-500/10 text-indigo-400">
                    <Droplets className="h-4 w-4" />
                  </span>
                  <div>
                    <h2 id="wm-modal-title" className="text-sm font-semibold text-slate-100">
                      Studio Branding &amp; Watermark
                    </h2>
                    <p className="text-xs text-slate-500">Configure how your brand appears on downloaded photos</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  aria-label="Close watermark configuration"
                  className="rounded-lg p-1.5 text-slate-500 transition-colors hover:bg-slate-800 hover:text-slate-300 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>

              {/* Body — 50/50 split */}
              <div className="flex min-h-0 flex-1 overflow-hidden">
                {/* LEFT: Configuration */}
                <div className="flex w-1/2 flex-col gap-5 overflow-y-auto border-r border-slate-800 p-5">
                  {/* Enable toggle */}
                  <div className="flex items-center justify-between rounded-xl border border-slate-700 bg-slate-800/60 p-4">
                    <div>
                      <p className="text-sm font-medium text-slate-200">Enable Watermarking</p>
                      <p className="text-xs text-slate-500">Applied to all downloads for this event</p>
                    </div>
                    <button
                      type="button"
                      role="switch"
                      aria-checked={config.enabled}
                      onClick={() => set('enabled', !config.enabled)}
                      className={`relative inline-flex h-6 w-11 rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 ${
                        config.enabled ? 'bg-indigo-600' : 'bg-slate-700'
                      }`}
                      aria-label="Toggle watermarking"
                    >
                      <motion.span
                        layout
                        transition={{ type: 'spring', stiffness: 600, damping: 30 }}
                        className={`inline-block h-5 w-5 rounded-full bg-white shadow ${config.enabled ? 'translate-x-5' : 'translate-x-0'}`}
                      />
                    </button>
                  </div>

                  {/* Watermark Mode */}
                  <ConfigSection icon={<Layers className="h-3.5 w-3.5" />} title="Watermark Mode">
                    <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                      {MODES.map((m) => (
                        <button
                          key={m.value}
                          type="button"
                          onClick={() => set('mode', m.value)}
                          className={`rounded-lg border p-3 text-left text-xs transition-all ${
                            config.mode === m.value
                              ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                              : 'border-slate-700 text-slate-400 hover:border-slate-600 hover:bg-slate-800/60'
                          }`}
                        >
                          <p className="font-semibold">{m.label}</p>
                          <p className="mt-0.5 text-slate-500">{m.description}</p>
                        </button>
                      ))}
                    </div>
                  </ConfigSection>

                  {/* Custom Text */}
                  {config.mode === 'CustomText' && (
                    <ConfigSection icon={<Type className="h-3.5 w-3.5" />} title="Custom Text">
                      <input
                        type="text"
                        value={config.customText ?? ''}
                        onChange={(e) => set('customText', e.target.value)}
                        maxLength={500}
                        placeholder="© My Photography Studio"
                        className={INPUT_CLASS}
                      />
                    </ConfigSection>
                  )}

                  {/* Dynamic Template */}
                  {config.mode === 'DynamicTemplate' && (
                    <ConfigSection icon={<Type className="h-3.5 w-3.5" />} title="Template">
                      <textarea
                        value={config.template ?? ''}
                        onChange={(e) => set('template', e.target.value)}
                        maxLength={1000}
                        rows={2}
                        placeholder="{StudioName} · {EventName}"
                        className={`${INPUT_CLASS} resize-none`}
                      />
                      <div className="mt-2 flex flex-wrap gap-1">
                        {TOKENS.map((t) => (
                          <button
                            key={t}
                            type="button"
                            onClick={() => set('template', (config.template ?? '') + t)}
                            className="rounded bg-slate-700 px-2 py-0.5 text-[11px] text-slate-300 hover:bg-indigo-500/20 hover:text-indigo-300"
                          >
                            {t}
                          </button>
                        ))}
                      </div>
                    </ConfigSection>
                  )}

                  {/* Branding inclusions */}
                  {isBrandingMode && (
                    <ConfigSection icon={<Info className="h-3.5 w-3.5" />} title="Include in Watermark">
                      <div className="space-y-2">
                        {(config.mode === 'StudioBranding' || config.mode === 'StudioAndEvent') && (
                          <CheckboxRow
                            label="Studio Name"
                            checked={config.includeStudioName}
                            onChange={(v) => set('includeStudioName', v)}
                          />
                        )}
                        {(config.mode === 'EventBranding' || config.mode === 'StudioAndEvent') && (
                          <CheckboxRow
                            label="Event Name"
                            checked={config.includeEventName}
                            onChange={(v) => set('includeEventName', v)}
                          />
                        )}
                        <CheckboxRow
                          label="Download Date"
                          checked={config.includeDownloadDate}
                          onChange={(v) => set('includeDownloadDate', v)}
                        />
                      </div>
                    </ConfigSection>
                  )}

                  {/* Style */}
                  <ConfigSection icon={<Layers className="h-3.5 w-3.5" />} title="Placement Style">
                    <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
                      {STYLES.map((s) => (
                        <button
                          key={s.value}
                          type="button"
                          onClick={() => set('style', s.value)}
                          className={`rounded-lg border p-2 text-center transition-all ${
                            config.style === s.value
                              ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                              : 'border-slate-700 text-slate-400 hover:border-slate-600 hover:bg-slate-800'
                          }`}
                        >
                          <div className="text-base leading-none">{s.icon}</div>
                          <div className="mt-1 text-[10px] leading-tight">{s.label}</div>
                        </button>
                      ))}
                    </div>
                  </ConfigSection>

                  {/* Opacity */}
                  <ConfigSection icon={<Droplets className="h-3.5 w-3.5" />} title={`Opacity — ${Math.round(config.opacity * 100)}%`}>
                    <input
                      type="range"
                      min={0}
                      max={1}
                      step={0.05}
                      value={config.opacity}
                      onChange={(e) => set('opacity', parseFloat(e.target.value))}
                      className="w-full accent-indigo-500"
                    />
                    <div className="mt-1 flex justify-between text-[11px] text-slate-600">
                      <span>Invisible</span>
                      <span>Solid</span>
                    </div>
                  </ConfigSection>

                  {/* Ribbon Background — only for BottomRibbon */}
                  {config.style === 'BottomRibbon' && (
                    <ConfigSection title={`Ribbon Background — ${Math.round(config.backgroundOpacity * 100)}%`}>
                      <input
                        type="range"
                        min={0}
                        max={1}
                        step={0.05}
                        value={config.backgroundOpacity}
                        onChange={(e) => set('backgroundOpacity', parseFloat(e.target.value))}
                        className="w-full accent-slate-500"
                      />
                    </ConfigSection>
                  )}

                  {/* Scale */}
                  <ConfigSection title="Text Size">
                    <div className="grid grid-cols-4 gap-2">
                      {SCALES.map((s) => (
                        <button
                          key={s.value}
                          type="button"
                          onClick={() => set('scale', s.value)}
                          className={`rounded-lg border py-2 text-xs transition-all ${
                            config.scale === s.value
                              ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                              : 'border-slate-700 text-slate-400 hover:border-slate-600'
                          }`}
                        >
                          {s.label}
                        </button>
                      ))}
                    </div>
                  </ConfigSection>

                  {/* Text Colour */}
                  <ConfigSection icon={<Palette className="h-3.5 w-3.5" />} title="Text Colour">
                    <div className="flex items-center gap-3">
                      <input
                        type="color"
                        value={config.textColor ?? '#ffffff'}
                        onChange={(e) => set('textColor', e.target.value)}
                        className="h-9 w-12 cursor-pointer rounded-lg border border-slate-700 bg-transparent p-0.5"
                        aria-label="Watermark text colour"
                      />
                      <span className="font-mono text-sm text-slate-300">
                        {(config.textColor ?? '#ffffff').toUpperCase()}
                      </span>
                      <div className="flex gap-1.5">
                        {SWATCHES.map(({ hex, label }) => (
                          <button
                            key={hex}
                            type="button"
                            title={label}
                            onClick={() => set('textColor', hex)}
                            className={`h-5 w-5 rounded-full border-2 transition-transform hover:scale-110 ${
                              config.textColor?.toLowerCase() === hex ? 'border-indigo-400' : 'border-slate-600'
                            }`}
                            style={{ backgroundColor: hex }}
                          />
                        ))}
                      </div>
                    </div>
                  </ConfigSection>

                  {/* Apply on Preview toggle */}
                  <label className="flex cursor-pointer items-center justify-between rounded-xl border border-slate-700 bg-slate-800/40 p-3.5">
                    <div>
                      <p className="text-sm font-medium text-slate-200">Apply on Preview</p>
                      <p className="text-xs text-slate-500">Show watermark in gallery lightbox</p>
                    </div>
                    <input
                      type="checkbox"
                      checked={config.applyOnPreview}
                      onChange={(e) => set('applyOnPreview', e.target.checked)}
                      className="sr-only"
                    />
                    <div
                      className={`relative inline-flex h-5 w-9 rounded-full transition-colors ${
                        config.applyOnPreview ? 'bg-indigo-600' : 'bg-slate-700'
                      }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 translate-y-0.5 rounded-full bg-white shadow transition-transform ${
                          config.applyOnPreview ? 'translate-x-4' : 'translate-x-0.5'
                        }`}
                      />
                    </div>
                  </label>
                </div>

                {/* RIGHT: Live Preview */}
                <div className="flex w-1/2 flex-col overflow-y-auto p-5">
                  <WatermarkPreviewPanel config={config} eventName={eventName} studioName={studioName} />
                </div>
              </div>

              {/* Footer */}
              <div className="flex items-center justify-end gap-3 border-t border-slate-800 px-6 py-4">
                <button
                  type="button"
                  onClick={onClose}
                  className="rounded-xl border border-slate-700 px-4 py-2 text-sm font-medium text-slate-300 transition-colors hover:bg-slate-800"
                >
                  Done
                </button>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}

// ── CheckboxRow helper ─────────────────────────────────────────────────────────

function CheckboxRow({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex cursor-pointer items-center gap-2 text-sm text-slate-300">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-3.5 w-3.5 rounded border-slate-600 bg-slate-800 accent-indigo-500"
      />
      {label}
    </label>
  );
}
