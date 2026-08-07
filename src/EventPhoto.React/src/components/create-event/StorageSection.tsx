import type { UseFormRegister, FieldErrors } from 'react-hook-form';
import { AlertCircle, FolderOpen, HardDrive } from 'lucide-react';
import { SectionCard } from './SectionCard';

const LABEL_CLASS = 'mb-2 block text-sm font-semibold text-slate-200';

function inputCls(hasError: boolean, extra = '') {
  return [
    'w-full rounded-xl border bg-slate-800 px-3 py-2.5 font-mono text-sm text-slate-100 placeholder:font-sans placeholder:text-slate-500 transition-colors focus:outline-none focus:ring-2',
    hasError
      ? 'border-rose-500 focus:border-rose-500 focus:ring-rose-500/40'
      : 'border-slate-700 focus:border-indigo-500 focus:ring-indigo-500/40',
    extra,
  ]
    .filter(Boolean)
    .join(' ');
}

function FieldError({ id, message }: { id: string; message: string }) {
  return (
    <p id={id} className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-rose-400">
      <AlertCircle className="h-3.5 w-3.5 shrink-0" />
      {message}
    </p>
  );
}

interface StorageSectionProps {
  register: UseFormRegister<any>;
  errors: FieldErrors<any>;
  /** Passed from EventForm after submit — triggers auto-expand to reveal hidden errors */
  hasError?: boolean;
}

export function StorageSection({ register, errors, hasError }: StorageSectionProps) {
  return (
    <SectionCard
      icon={<HardDrive className="h-4 w-4" />}
      title="Storage Configuration"
      subtitle="Where photos are watched and stored"
      defaultOpen={false}
      hasError={hasError}
    >
      <div className="space-y-5">
        {/* Watch Folder */}
        <div>
          <label htmlFor="watch-folder" className={LABEL_CLASS}>
            Watch Folder Path <span className="text-rose-400">*</span>
          </label>
          <div className="relative">
            <FolderOpen className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
            <input
              id="watch-folder"
              type="text"
              placeholder="e.g. D:\Events\Wedding_2026"
              maxLength={512}
              {...register('watchFolder')}
              className={inputCls(!!errors.watchFolder, 'pl-10')}
              aria-describedby={errors.watchFolder ? 'watch-folder-error' : 'watch-folder-hint'}
              spellCheck={false}
              autoComplete="off"
            />
          </div>
          {errors.watchFolder ? (
            <FieldError id="watch-folder-error" message={errors.watchFolder.message as string} />
          ) : (
            <p id="watch-folder-hint" className="mt-1.5 text-xs text-slate-500">
              PixBridge monitors this folder for new photos. Use an absolute path without trailing slash.
            </p>
          )}
        </div>

        {/* Gallery Recent Count */}
        <div>
          <label htmlFor="gallery-recent-count" className={LABEL_CLASS}>
            Gallery Recent Image Count
          </label>
          <input
            id="gallery-recent-count"
            type="number"
            min={1}
            max={1000}
            placeholder="Leave blank to show all photos"
            {...register('galleryRecentCount', { valueAsNumber: true })}
            className={inputCls(!!errors.galleryRecentCount)}
            aria-describedby={errors.galleryRecentCount ? 'count-error' : 'count-hint'}
          />
          {errors.galleryRecentCount ? (
            <FieldError id="count-error" message={errors.galleryRecentCount.message as string} />
          ) : (
            <p id="count-hint" className="mt-1.5 text-xs text-slate-500">
              Limits the gallery to the N most recently captured photos. Leave blank to show all.
            </p>
          )}
        </div>
      </div>
    </SectionCard>
  );
}
