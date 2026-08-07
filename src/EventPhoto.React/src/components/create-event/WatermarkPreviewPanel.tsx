import { WatermarkPreview } from '../event-workspace/WatermarkPreview';
import type { UpsertWatermarkConfigRequest } from '../../types';

interface WatermarkPreviewPanelProps {
  config: UpsertWatermarkConfigRequest;
  eventName: string;
}

export function WatermarkPreviewPanel({ config, eventName }: WatermarkPreviewPanelProps) {
  return (
    <div className="flex h-full flex-col gap-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-200">Live Preview</p>
        <span className="rounded-full bg-emerald-500/15 px-2 py-0.5 text-xs font-medium text-emerald-400">
          Updates instantly
        </span>
      </div>
      <div className="flex-1">
        <WatermarkPreview config={config} eventName={eventName || 'Event Name'} studioName="PixBridge Studio" />
      </div>
      <div className="rounded-xl border border-slate-700 bg-slate-800/60 p-3">
        <p className="text-xs font-medium text-slate-300">How watermarks work</p>
        <p className="mt-1 text-xs leading-relaxed text-slate-500">
          Watermarks are applied <strong className="text-slate-400">in-memory at download time</strong> — your
          original files are never modified. Configure opacity, placement and branding to match your studio style.
        </p>
      </div>
    </div>
  );
}
