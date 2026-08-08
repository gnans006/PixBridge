import { Palette } from 'lucide-react';
import type { ApplicationSettings } from '../../../api/applicationSettings';

interface Props {
  settings: ApplicationSettings;
}

export function BrandingTab({ settings: _settings }: Props) {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <div className="mb-6 flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-pink-500/10">
          <Palette className="h-5 w-5 text-pink-400" />
        </div>
        <div>
          <h2 className="text-sm font-semibold text-white">Branding</h2>
          <p className="text-xs text-slate-400">
            Studio logo and watermark profile used as defaults on new events.
          </p>
        </div>
      </div>

      <div className="flex h-40 items-center justify-center rounded-xl border border-dashed border-slate-700">
        <p className="text-sm text-slate-500">Branding configuration — coming soon.</p>
      </div>
    </div>
  );
}
