import { Droplets, Settings } from 'lucide-react';
import { motion } from 'framer-motion';
import type { UpsertWatermarkConfigRequest, WatermarkMode, WatermarkStyle } from '../../types';
import { SectionCard } from './SectionCard';

const MODE_LABELS: Record<WatermarkMode, string> = {
  Disabled: 'Disabled',
  StudioBranding: 'Studio Branding',
  EventBranding: 'Event Branding',
  StudioAndEvent: 'Studio + Event Branding',
  CustomText: 'Custom Text',
  DynamicTemplate: 'Dynamic Template',
};

const STYLE_LABELS: Record<WatermarkStyle, string> = {
  Corner: 'Corner',
  Center: 'Center',
  Diagonal: 'Diagonal',
  RepeatedPattern: 'Repeated Pattern',
  BottomRibbon: 'Bottom Ribbon',
};

interface WatermarkSectionProps {
  config: UpsertWatermarkConfigRequest;
  onToggleEnabled: (v: boolean) => void;
  onOpenModal: () => void;
}

export function WatermarkSection({ config, onToggleEnabled, onOpenModal }: WatermarkSectionProps) {
  const badge = (
    <span
      className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${
        config.enabled
          ? 'bg-emerald-500/20 text-emerald-400'
          : 'bg-slate-700 text-slate-400'
      }`}
    >
      {config.enabled ? 'Enabled' : 'Off'}
    </span>
  );

  return (
    <SectionCard
      icon={<Droplets className="h-4 w-4" />}
      title="Studio Branding & Watermark"
      subtitle="Applied in-memory at download time"
      defaultOpen={false}
      badge={badge}
    >
      <div className="space-y-4">
        {/* Enable toggle row */}
        <div className="flex items-center justify-between rounded-xl border border-slate-700 bg-slate-800/40 p-4">
          <div>
            <p className="text-sm font-medium text-slate-200">Enable Watermark</p>
            <p className="text-xs text-slate-500">Protect downloads with studio branding</p>
          </div>
          <button
            type="button"
            role="switch"
            aria-checked={config.enabled}
            onClick={() => onToggleEnabled(!config.enabled)}
            className={`relative inline-flex h-6 w-11 rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 ${
              config.enabled ? 'bg-indigo-600' : 'bg-slate-700'
            }`}
            aria-label="Toggle watermark"
          >
            <motion.span
              layout
              transition={{ type: 'spring', stiffness: 600, damping: 30 }}
              className={`inline-block h-5 w-5 rounded-full bg-white shadow ${config.enabled ? 'translate-x-5' : 'translate-x-0'}`}
            />
          </button>
        </div>

        {/* Config summary */}
        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-xl border border-slate-800 bg-slate-800/30 p-3">
            <p className="text-[10px] font-medium uppercase tracking-wide text-slate-500">Mode</p>
            <p className="mt-1 text-sm font-semibold text-slate-200">{MODE_LABELS[config.mode]}</p>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-800/30 p-3">
            <p className="text-[10px] font-medium uppercase tracking-wide text-slate-500">Style</p>
            <p className="mt-1 text-sm font-semibold text-slate-200">{STYLE_LABELS[config.style]}</p>
          </div>
        </div>

        {/* Configure button */}
        <button
          type="button"
          onClick={onOpenModal}
          className="flex w-full items-center justify-center gap-2 rounded-xl border border-slate-700 bg-slate-800/60 py-2.5 text-sm font-medium text-slate-300 transition-colors hover:border-indigo-500/50 hover:bg-indigo-500/5 hover:text-indigo-300 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
        >
          <Settings className="h-4 w-4" />
          Configure Watermark
        </button>
      </div>
    </SectionCard>
  );
}
