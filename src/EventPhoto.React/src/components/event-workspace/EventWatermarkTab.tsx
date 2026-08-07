import { useEffect, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AnimatePresence, motion } from 'framer-motion';
import { Loader2, RotateCcw, Save, Shield } from 'lucide-react';
import toast from 'react-hot-toast';
import { watermarkApi } from '../../api/watermark';
import { apiError } from '../../utils/errorHandler';
import type { EventWorkspaceResponse, UpsertWatermarkConfigRequest } from '../../types';
import { WatermarkPreview } from './WatermarkPreview';

const MODES = [
  { value: 'Disabled', label: 'Disabled' },
  { value: 'StudioBranding', label: 'Studio Branding' },
  { value: 'EventBranding', label: 'Event Branding' },
  { value: 'StudioAndEvent', label: 'Studio + Event' },
  { value: 'CustomText', label: 'Custom Text' },
  { value: 'DynamicTemplate', label: 'Dynamic Template' },
];

const STYLES = [
  { value: 'Corner', label: 'Corner' },
  { value: 'Center', label: 'Center' },
  { value: 'Diagonal', label: 'Diagonal' },
  { value: 'RepeatedPattern', label: 'Repeated Pattern' },
  { value: 'BottomRibbon', label: 'Bottom Ribbon' },
];

const SCALES = [
  { value: 'Small', label: 'Small' },
  { value: 'Medium', label: 'Medium' },
  { value: 'Large', label: 'Large' },
  { value: 'Auto', label: 'Auto' },
];

interface EventWatermarkTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventWatermarkTab({ workspace }: EventWatermarkTabProps) {
  const qc = useQueryClient();
  const [dirty, setDirty] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['watermark', workspace.id],
    queryFn: () => watermarkApi.getConfig(workspace.id),
  });
  const existing = data?.data;

  const defaultValues: UpsertWatermarkConfigRequest = {
    enabled: existing?.enabled ?? false,
    mode: existing?.mode ?? 'StudioBranding',
    style: existing?.style ?? 'Corner',
    opacity: existing?.opacity ?? 0.6,
    scale: existing?.scale ?? 'Medium',
    customText: existing?.customText ?? '',
    template: existing?.template ?? '{StudioName} · {EventName}',
    logoPath: existing?.logoPath ?? '',
    includeStudioName: existing?.includeStudioName ?? true,
    includeEventName: existing?.includeEventName ?? false,
    includeDownloadDate: existing?.includeDownloadDate ?? false,
    applyOnDownload: existing?.applyOnDownload ?? true,
    textColor: existing?.textColor ?? '#FFFFFF',
    fontName: existing?.fontName ?? '',
    backgroundOpacity: existing?.backgroundOpacity ?? 0.4,
    applyOnPreview: existing?.applyOnPreview ?? false,
  };

  const { register, handleSubmit, reset, control, watch } = useForm<UpsertWatermarkConfigRequest>({
    defaultValues,
  });

  // Re-init after data loads
  useEffect(() => {
    if (existing) reset(defaultValues);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [existing?.updatedAt]);

  const watched = useWatch({ control });

  // Track dirty only on actual user edits — not on mount or after reset()
  useEffect(() => {
    const subscription = watch(() => setDirty(true));
    return () => subscription.unsubscribe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watch]);

  const mutation = useMutation({
    mutationFn: (data: UpsertWatermarkConfigRequest) =>
      watermarkApi.upsertConfig(workspace.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['watermark', workspace.id] });
      qc.invalidateQueries({ queryKey: ['workspace', workspace.id] });
      toast.success('Watermark settings saved.');
      setDirty(false);
    },
    onError: (e) => apiError(e, 'Failed to save watermark settings.'),
  });

  const onSubmit = handleSubmit((data) => mutation.mutate(data));
  const handleDiscard = () => { reset(defaultValues); setDirty(false); };

  const currentMode = watched.mode ?? 'StudioBranding';

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      {/* Left: settings */}
      <div className="space-y-6">
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <div className="mb-5 flex items-center gap-2">
            <Shield className="h-4 w-4 text-amber-400" />
            <h2 className="text-sm font-semibold text-white">Watermark Configuration</h2>
          </div>

          <form onSubmit={onSubmit} className="space-y-5">
            {/* Enable toggle */}
            <div className="flex items-center justify-between rounded-xl border border-slate-700/50 p-4">
              <div>
                <p className="text-sm font-semibold text-white">Enable Watermark</p>
                <p className="text-xs text-slate-400">Applied at download time to protect photos.</p>
              </div>
              <Controller
                control={control}
                name="enabled"
                render={({ field }) => (
                  <BoolToggle
                    checked={!!field.value}
                    onChange={(v) => { field.onChange(v); setDirty(true); }}
                    color="amber"
                  />
                )}
              />
            </div>

            <Controller
              control={control}
              name="mode"
              render={({ field }) => (
                <SelectField
                  label="Watermark Mode"
                  value={field.value ?? 'StudioBranding'}
                  onChange={(e) => { field.onChange(e.target.value); setDirty(true); }}
                  options={MODES}
                />
              )}
            />
            <Controller
              control={control}
              name="style"
              render={({ field }) => (
                <SelectField
                  label="Style / Position"
                  value={field.value ?? 'Corner'}
                  onChange={(e) => { field.onChange(e.target.value); setDirty(true); }}
                  options={STYLES}
                />
              )}
            />
            <Controller
              control={control}
              name="scale"
              render={({ field }) => (
                <SelectField
                  label="Scale"
                  value={field.value ?? 'Medium'}
                  onChange={(e) => { field.onChange(e.target.value); setDirty(true); }}
                  options={SCALES}
                />
              )}
            />

            {/* Opacity */}
            <div className="space-y-2">
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
                Opacity — <span className="text-amber-400">{Math.round((watched.opacity ?? 0.6) * 100)}%</span>
              </label>
              <input {...register('opacity', { valueAsNumber: true, onChange: () => setDirty(true) })} type="range" min={0.05} max={1} step={0.05} className="w-full accent-amber-500" />
            </div>

            {/* Background opacity */}
            <div className="space-y-2">
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
                Background Opacity — <span className="text-amber-400">{Math.round((watched.backgroundOpacity ?? 0.4) * 100)}%</span>
              </label>
              <input {...register('backgroundOpacity', { valueAsNumber: true, onChange: () => setDirty(true) })} type="range" min={0} max={1} step={0.05} className="w-full accent-amber-500" />
            </div>

            {/* Text color */}
            <Controller
              control={control}
              name="textColor"
              render={({ field }) => (
                <div className="flex items-center gap-3">
                  <label className="text-xs font-semibold uppercase tracking-wider text-slate-400">Text Color</label>
                  <input
                    type="color"
                    value={field.value ?? '#FFFFFF'}
                    onChange={(e) => { field.onChange(e.target.value); setDirty(true); }}
                    className="h-8 w-14 cursor-pointer rounded border-0 bg-transparent"
                  />
                  <input
                    value={field.value ?? '#FFFFFF'}
                    onChange={(e) => { field.onChange(e.target.value); setDirty(true); }}
                    className="w-24 rounded-lg border border-slate-700 bg-slate-800 px-2.5 py-1.5 font-mono text-xs text-white focus:border-amber-500 focus:outline-none"
                  />
                </div>
              )}
            />

            {/* Custom text (when mode = CustomText) */}
            {currentMode === 'CustomText' && (
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">Custom Text</label>
                <input {...register('customText', { onChange: () => setDirty(true) })} className="w-full rounded-xl border border-slate-700 bg-slate-800 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-amber-500 focus:outline-none" placeholder="© 2026 My Studio" />
              </div>
            )}

            {/* Template (when mode = DynamicTemplate) */}
            {currentMode === 'DynamicTemplate' && (
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">Template</label>
                <input {...register('template', { onChange: () => setDirty(true) })} className="w-full rounded-xl border border-slate-700 bg-slate-800 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-amber-500 focus:outline-none" placeholder="{StudioName} · {EventName}" />
                <p className="text-xs text-slate-500">Tokens: {'{StudioName}'} {'{EventName}'} {'{EventDate}'} {'{DownloadDate}'}</p>
              </div>
            )}

            {/* Toggles row */}
            <div className="grid grid-cols-3 gap-3">
              {(
                [
                  { name: 'includeStudioName', label: 'Include Studio' },
                  { name: 'includeEventName', label: 'Include Event' },
                  { name: 'applyOnDownload', label: 'On Download' },
                ] as const
              ).map(({ name, label }) => (
                <Controller
                  key={name}
                  control={control}
                  name={name}
                  render={({ field }) => (
                    <SmallToggle
                      label={label}
                      checked={!!field.value}
                      onChange={(v) => { field.onChange(v); setDirty(true); }}
                    />
                  )}
                />
              ))}
            </div>

            <AnimatePresence>
              {dirty && (
                <motion.div
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: 4 }}
                  className="flex items-center justify-end gap-3 border-t border-slate-800 pt-4"
                >
                  <button type="button" onClick={handleDiscard} disabled={mutation.isPending} className="inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium text-slate-400 hover:text-white disabled:opacity-50">
                    <RotateCcw className="h-3.5 w-3.5" /> Discard
                  </button>
                  <button type="submit" disabled={mutation.isPending} className="inline-flex items-center gap-2 rounded-xl bg-amber-600 px-5 py-2 text-sm font-semibold text-white hover:bg-amber-500 disabled:opacity-60">
                    {mutation.isPending ? <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</> : <><Save className="h-4 w-4" /> Save Watermark</>}
                  </button>
                </motion.div>
              )}
            </AnimatePresence>
          </form>
        </div>
      </div>

      {/* Right: live preview */}
      <div className="space-y-4 lg:sticky lg:top-6 lg:self-start">
        <WatermarkPreview
          config={watched as UpsertWatermarkConfigRequest}
          eventName={workspace.name}
        />
        <p className="text-center text-xs text-slate-500">
          Preview updates as you change settings above.
        </p>
      </div>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

// Controlled select — value and onChange are always passed via Controller's field object.
function SelectField({ label, options, ...props }: { label: string; options: { value: string; label: string }[] } & React.SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="space-y-1.5">
      <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">{label}</label>
      <select
        {...props}
        className="w-full rounded-xl border border-slate-700 bg-slate-800 px-3.5 py-2.5 text-sm text-white focus:border-amber-500 focus:outline-none"
      >
        {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  );
}

function BoolToggle({ checked, onChange, color = 'amber' }: { checked: boolean; onChange: (v: boolean) => void; color?: string }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className={`relative inline-flex h-6 w-11 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ${checked ? `bg-${color}-600` : 'bg-slate-700'}`}
    >
      <span className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow transition duration-200 ${checked ? 'translate-x-5' : 'translate-x-0'}`} />
    </button>
  );
}

function SmallToggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className={`flex flex-col items-center gap-1.5 rounded-xl border p-3 text-center transition-colors ${checked ? 'border-amber-500/40 bg-amber-500/10 text-amber-400' : 'border-slate-700 text-slate-500 hover:border-slate-600'}`}
    >
      <div className={`h-4 w-4 rounded-full border-2 ${checked ? 'border-amber-400 bg-amber-400' : 'border-slate-600'}`} />
      <span className="text-xs font-medium">{label}</span>
    </button>
  );
}
