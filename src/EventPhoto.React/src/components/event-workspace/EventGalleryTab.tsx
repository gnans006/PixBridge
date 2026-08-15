import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { Eye, Globe, Loader2, Lock, RotateCcw, Save, Users } from 'lucide-react';
import toast from 'react-hot-toast';
import { workspaceApi } from '../../api/workspace';
import { apiError } from '../../utils/errorHandler';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';
import type { EventWorkspaceResponse, UpdateGallerySettingsRequest } from '../../types';

interface EventGalleryTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventGalleryTab({ workspace }: EventGalleryTabProps) {
  const qc = useQueryClient();
  const [dirty, setDirty] = useState(false);
  const { data: appSettings } = useApplicationSettings();

  const defaults: UpdateGallerySettingsRequest = {
    allowGalleryBrowsing: workspace.allowGalleryBrowsing,
    allowFaceSearch: workspace.allowFaceSearch,
    restrictDownloadsToMatchedPhotos: workspace.restrictDownloadsToMatchedPhotos,
    galleryRecentCount: workspace.galleryRecentCount,
  };

  const { register, handleSubmit, reset, watch, setValue, formState: { errors } } = useForm<UpdateGallerySettingsRequest>({
    defaultValues: defaults,
  });

  const allowFaceSearch = watch('allowFaceSearch');
  const allowGalleryBrowsing = watch('allowGalleryBrowsing');
  const restrictDownloads = watch('restrictDownloadsToMatchedPhotos');

  // Track dirty
  useEffect(() => {
    const subscription = watch(() => setDirty(true));
    return () => subscription.unsubscribe();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watch]);

  const mutation = useMutation({
    mutationFn: (data: UpdateGallerySettingsRequest) =>
      workspaceApi.updateGallerySettings(workspace.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['workspace', workspace.id] });
      qc.invalidateQueries({ queryKey: ['events'] });
      toast.success('Gallery settings saved.');
      setDirty(false);
    },
    onError: (e) => apiError(e, 'Failed to save gallery settings.'),
  });

  const onSubmit = handleSubmit((data) => mutation.mutate(data));

  const handleDiscard = () => {
    reset(defaults);
    setDirty(false);
  };

  // Derive gallery mode label
  let mode = 'Gallery Only';
  if (allowGalleryBrowsing && allowFaceSearch) mode = 'Hybrid (Gallery + Face Search)';
  else if (!allowGalleryBrowsing && allowFaceSearch) mode = 'Face Search Only';

  // Gallery URL — use PublicBaseUrl so the displayed link matches what guests see
  // (router IP / custom domain), not the admin's local LAN address.
  const origin = appSettings?.publicBaseUrl?.replace(/\/$/, '') ?? window.location.origin;
  const galleryUrl = `${origin}/gallery/${workspace.id}`;

  return (
    <div className="space-y-6">
      {/* Mode preview */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-1 flex items-center gap-2">
          <Eye className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Current Guest Experience</h2>
        </div>
        <p className="mb-4 text-xs text-slate-500">
          What guests see when they open the gallery link.
        </p>
        <div className="flex items-center gap-3 rounded-xl border border-indigo-500/30 bg-indigo-500/10 px-4 py-3">
          <Globe className="h-5 w-5 text-indigo-400" />
          <div>
            <p className="text-sm font-semibold text-indigo-300">{mode}</p>
            <a
              href={galleryUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-0.5 block truncate text-xs text-slate-400 hover:text-indigo-400"
            >
              {galleryUrl}
            </a>
          </div>
        </div>
      </div>

      {/* Settings card */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-2">
          <Users className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Access Settings</h2>
        </div>

        <form onSubmit={onSubmit} className="space-y-6">
          <div className="space-y-3">
            <ToggleRow
              title="Allow Gallery Browsing"
              description="Guests can browse all photos without uploading a selfie."
              checked={allowGalleryBrowsing}
              onChange={(v) => {
                setValue('allowGalleryBrowsing', v, { shouldDirty: true });
                // At least one must be enabled
                if (!v && !allowFaceSearch) {
                  setValue('allowFaceSearch', true, { shouldDirty: true });
                }
                setDirty(true);
              }}
            />

            <ToggleRow
              title="Allow Face Search"
              description={
                workspace.enableFaceRecognition
                  ? 'Guests can upload a selfie to find their photos using AI.'
                  : 'Requires Face Recognition to be enabled on the Face Recognition tab.'
              }
              checked={allowFaceSearch}
              disabled={!workspace.enableFaceRecognition}
              onChange={(v) => {
                setValue('allowFaceSearch', v, { shouldDirty: true });
                if (!v && !allowGalleryBrowsing) {
                  setValue('allowGalleryBrowsing', true, { shouldDirty: true });
                }
                setDirty(true);
              }}
            />

            <ToggleRow
              title="Restrict Downloads to Matched Photos"
              description="When face search is used, guests can only download photos matched to their selfie."
              checked={watch('restrictDownloadsToMatchedPhotos')}
              disabled={!allowFaceSearch}
              onChange={(v) => { setValue('restrictDownloadsToMatchedPhotos', v, { shouldDirty: true }); setDirty(true); }}
            />
          </div>

          {/* Recent count */}
          <div className="space-y-1.5">
            <label className="flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                Recent Photos Limit
              </span>
              <span className="text-xs text-slate-500">Leave blank to show all photos</span>
            </label>
            <input
              {...register('galleryRecentCount', {
                setValueAs: (v) => (v === '' ? undefined : Number(v)),
                min: { value: 1, message: 'Must be at least 1' },
                max: { value: 1000, message: 'Max 1000' },
              })}
              type="number"
              min={1}
              max={1000}
              placeholder="e.g. 100"
              className="w-40 rounded-xl border border-slate-700 bg-slate-800 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
            />
            {errors.galleryRecentCount && (
              <p className="text-xs text-rose-400">{errors.galleryRecentCount.message}</p>
            )}
          </div>

          <AnimatePresence>
            {dirty && (
              <motion.div
                initial={{ opacity: 0, y: 4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: 4 }}
                className="flex items-center justify-end gap-3 border-t border-slate-800 pt-4"
              >
                <button
                  type="button"
                  onClick={handleDiscard}
                  disabled={mutation.isPending}
                  className="inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium text-slate-400 transition-colors hover:text-white disabled:opacity-50"
                >
                  <RotateCcw className="h-3.5 w-3.5" /> Discard
                </button>
                <button
                  type="submit"
                  disabled={mutation.isPending}
                  className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2 text-sm font-semibold text-white transition-colors hover:bg-indigo-500 disabled:opacity-60"
                >
                  {mutation.isPending ? <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</> : <><Save className="h-4 w-4" /> Save Settings</>}
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </div>

      {/* Download policy info */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-5">
        <div className="flex items-start gap-3">
          <Lock className="mt-0.5 h-4 w-4 shrink-0 text-amber-400" />
          <div>
            <p className="text-sm font-semibold text-white">Download Policy</p>
            <p className="mt-1 text-xs text-slate-400">
              {restrictDownloads
                ? 'Guests who used face search can only download photos where their face was detected.'
                : 'All guests can download any photo in the gallery.'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function ToggleRow({
  title,
  description,
  checked,
  disabled,
  onChange,
}: {
  title: string;
  description: string;
  checked: boolean;
  disabled?: boolean;
  onChange?: (v: boolean) => void;
  [key: string]: unknown;
}) {
  return (
    <div className={`flex items-start justify-between gap-4 rounded-xl border border-slate-700/50 p-4 transition-colors ${disabled ? 'opacity-50' : 'hover:border-slate-600'}`}>
      <div>
        <p className="text-sm font-semibold text-white">{title}</p>
        <p className="mt-0.5 text-xs text-slate-400">{description}</p>
      </div>
      <button
        type="button"
        disabled={disabled}
        onClick={() => onChange?.(!checked)}
        className={[
          'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200',
          checked ? 'bg-indigo-600' : 'bg-slate-700',
          disabled ? 'cursor-not-allowed' : '',
        ].join(' ')}
      >
        <span
          className={[
            'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow transition duration-200',
            checked ? 'translate-x-5' : 'translate-x-0',
          ].join(' ')}
        />
      </button>
    </div>
  );
}
