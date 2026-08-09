import { useState } from 'react';
import { SlidersHorizontal, ScanFace, Stamp, Info } from 'lucide-react';
import toast from 'react-hot-toast';
import { useApplicationSettings, useUpdateApplicationSettings } from '../../hooks/useApplicationSettings';

interface FeatureCardProps {
  icon: React.ReactNode;
  title: string;
  description: string;
  enabled: boolean;
  onChange: (val: boolean) => void;
  impact: string;
}

function FeatureCard({ icon, title, description, enabled, onChange, impact }: FeatureCardProps) {
  return (
    <div className={`rounded-xl border p-5 transition-all duration-200 ${
      enabled ? 'border-pds-border bg-pds-surface' : 'border-pds-border/50 bg-pds-surface/50'
    }`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className={`mt-0.5 flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg transition-colors ${
            enabled ? 'bg-pds-primary/15 text-pds-primary' : 'bg-pds-elevated text-pds-text-muted'
          }`}>
            {icon}
          </div>
          <div>
            <h3 className={`text-sm font-semibold ${enabled ? 'text-pds-text' : 'text-pds-text-muted'}`}>
              {title}
            </h3>
            <p className="mt-0.5 text-xs text-pds-text-muted">{description}</p>
            <p className={`mt-2 flex items-center gap-1 text-[11px] ${enabled ? 'text-pds-text-2' : 'text-pds-text-muted/60'}`}>
              <Info className="h-3 w-3 flex-shrink-0" />
              {impact}
            </p>
          </div>
        </div>

        {/* Toggle */}
        <button
          type="button"
          role="switch"
          aria-checked={enabled}
          onClick={() => onChange(!enabled)}
          className={`relative mt-0.5 h-6 w-11 flex-shrink-0 rounded-full transition-colors duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-pds-primary ${
            enabled ? 'bg-pds-primary' : 'bg-pds-elevated border border-pds-border'
          }`}
        >
          <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform duration-200 ${
            enabled ? 'translate-x-5' : 'translate-x-0.5'
          }`} />
        </button>
      </div>
    </div>
  );
}

export function ConfigurationPage() {
  const { data: settings, isLoading } = useApplicationSettings();
  const update = useUpdateApplicationSettings();

  const [isFaceSearchEnabled, setIsFaceSearchEnabled] = useState<boolean | null>(null);
  const [isWatermarkEnabled, setIsWatermarkEnabled]   = useState<boolean | null>(null);

  // Derive current values: local override takes priority, fallback to server
  const faceSearch  = isFaceSearchEnabled  ?? settings?.isFaceSearchEnabled  ?? true;
  const watermark   = isWatermarkEnabled   ?? settings?.isWatermarkEnabled   ?? true;

  const isDirty = isFaceSearchEnabled !== null || isWatermarkEnabled !== null;

  const handleSave = async () => {
    if (!settings) return;
    try {
      await update.mutateAsync({
        studioName:                  settings.studioName,
        serverName:                  settings.serverName,
        publicBaseUrl:               settings.publicBaseUrl,
        serverPort:                  settings.serverPort,
        defaultEventGalleryMode:     settings.defaultEventGalleryMode,
        enableWatermarkByDefault:    settings.enableWatermarkByDefault,
        enableFaceRecognitionByDefault: settings.enableFaceRecognitionByDefault,
        isWatermarkEnabled:  watermark,
        isFaceSearchEnabled: faceSearch,
      });
      toast.success('Configuration saved');
      setIsFaceSearchEnabled(null);
      setIsWatermarkEnabled(null);
    } catch {
      toast.error('Failed to save configuration');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map(i => (
          <div key={i} className="h-28 animate-pulse rounded-xl bg-pds-elevated" />
        ))}
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-pds-primary/15 text-pds-primary">
          <SlidersHorizontal className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-pds-text">Configuration</h1>
          <p className="text-xs text-pds-text-muted">
            Enable or disable modules for this studio. Changes apply immediately after saving.
          </p>
        </div>
      </div>

      {/* Production features */}
      <section className="space-y-3">
        <h2 className="text-[11px] font-bold uppercase tracking-widest text-pds-text-2">
          Production Features
        </h2>

        <FeatureCard
          icon={<ScanFace className="h-4.5 w-4.5" />}
          title="Face Search / AI Studio"
          description="AI-powered face recognition, photo matching, and the AI Studio navigation section."
          enabled={faceSearch}
          onChange={setIsFaceSearchEnabled}
          impact={faceSearch
            ? 'AI Studio appears in sidebar. Face search is available on productions.'
            : 'AI Studio is hidden from sidebar. Face search options are removed from productions.'}
        />

        <FeatureCard
          icon={<Stamp className="h-4.5 w-4.5" />}
          title="Watermark"
          description="Apply studio watermarks to exported photos. Includes watermark settings on productions."
          enabled={watermark}
          onChange={setIsWatermarkEnabled}
          impact={watermark
            ? 'Watermark settings are visible on productions and exports.'
            : 'Watermark options are hidden from productions and export settings.'}
        />
      </section>

      {/* Save bar */}
      <div className={`flex items-center justify-between rounded-xl border px-4 py-3 transition-all duration-200 ${
        isDirty
          ? 'border-pds-primary/40 bg-pds-primary/5'
          : 'border-pds-border bg-pds-elevated'
      }`}>
        <p className="text-xs text-pds-text-muted">
          {isDirty ? 'You have unsaved changes.' : 'All changes are saved.'}
        </p>
        <button
          type="button"
          disabled={!isDirty || update.isPending}
          onClick={handleSave}
          className="rounded-lg bg-pds-primary px-4 py-1.5 text-xs font-semibold text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {update.isPending ? 'Saving…' : 'Save Configuration'}
        </button>
      </div>
    </div>
  );
}
