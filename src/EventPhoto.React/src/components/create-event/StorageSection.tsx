import { useState, useEffect, useRef, useCallback } from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue, UseFormWatch } from 'react-hook-form';
import {
  AlertCircle,
  CheckCircle2,
  FolderOpen,
  HardDrive,
  Loader2,
  AlertTriangle,
  Usb,
  Network,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { SectionCard } from './SectionCard';
import { systemApi, type PathValidationResult, type DriveInfo } from '../../api/system';

const LABEL_CLASS = 'mb-2 block text-sm font-semibold text-slate-200';
const DEBOUNCE_MS = 500; // validate 500ms after user stops typing

function inputCls(state: 'idle' | 'valid' | 'warning' | 'error', extra = '') {
  const ring = {
    idle:    'border-slate-700 focus:border-indigo-500 focus:ring-indigo-500/40',
    valid:   'border-emerald-500 focus:border-emerald-500 focus:ring-emerald-500/40',
    warning: 'border-amber-500  focus:border-amber-500  focus:ring-amber-500/40',
    error:   'border-rose-500   focus:border-rose-500   focus:ring-rose-500/40',
  }[state];
  return [
    'w-full rounded-xl border bg-slate-800 px-3 py-2.5 font-mono text-sm text-slate-100',
    'placeholder:font-sans placeholder:text-slate-500 transition-colors focus:outline-none focus:ring-2',
    ring,
    extra,
  ].filter(Boolean).join(' ');
}

// ── Status badge below the path input ────────────────────────────────────────

function PathStatus({
  loading,
  result,
  formError,
}: {
  loading: boolean;
  result: PathValidationResult | null;
  formError?: string;
}) {
  if (loading) {
    return (
      <p className="mt-1.5 flex items-center gap-1.5 text-sm text-slate-400">
        <Loader2 className="h-3.5 w-3.5 animate-spin shrink-0" />
        Validating path…
      </p>
    );
  }

  // Prefer form-level schema errors (e.g. "too short", "path traversal")
  if (formError) {
    return (
      <p className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-rose-400">
        <AlertCircle className="h-3.5 w-3.5 shrink-0" />
        {formError}
      </p>
    );
  }

  if (!result) {
    return (
      <p className="mt-1.5 text-xs text-slate-500">
        PixBridge monitors this folder for new photos. Use an absolute path (e.g. D:\Events\Wedding_2026).
      </p>
    );
  }

  if (!result.isValid) {
    return (
      <p className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-rose-400">
        <AlertCircle className="h-3.5 w-3.5 shrink-0" />
        {result.error}
      </p>
    );
  }

  return (
    <div className="mt-1.5 space-y-1">
      {/* Primary status line */}
      <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-400">
        <CheckCircle2 className="h-3.5 w-3.5 shrink-0" />
        {result.exists
          ? `Folder found on ${result.driveLabel ?? result.driveType ?? 'disk'}`
          : `Folder will be created automatically on ${result.driveLabel ?? result.driveType ?? 'disk'}`}
      </p>

      {/* Removable / network warning */}
      {result.warning && (
        <p className="flex items-start gap-1.5 text-xs text-amber-400">
          <AlertTriangle className="h-3.5 w-3.5 shrink-0 mt-0.5" />
          {result.warning}
        </p>
      )}
    </div>
  );
}

// ── Drive picker ──────────────────────────────────────────────────────────────

function DriveIcon({ type }: { type: string }) {
  if (type === 'Removable') return <Usb className="h-4 w-4 text-amber-400" />;
  if (type === 'Network')   return <Network className="h-4 w-4 text-blue-400" />;
  return <HardDrive className="h-4 w-4 text-slate-400" />;
}

function DrivePicker({
  onSelect,
}: {
  onSelect: (letter: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const [drives, setDrives] = useState<DriveInfo[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await systemApi.getDrives();
      setDrives(result);
    } catch {
      setDrives([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const toggle = () => {
    if (!open) load();
    setOpen(v => !v);
  };

  return (
    <div className="relative">
      <button
        type="button"
        onClick={toggle}
        className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-slate-800/60 px-3 py-1.5 text-xs text-slate-300 hover:border-indigo-500 hover:text-indigo-300 transition-colors"
        title="Browse available drives"
      >
        <HardDrive className="h-3.5 w-3.5" />
        Browse drives
        {open ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
      </button>

      {open && (
        <div className="absolute left-0 top-full z-20 mt-1 w-72 rounded-xl border border-slate-700 bg-slate-850 bg-slate-900 shadow-2xl">
          <div className="border-b border-slate-700 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500">
            Available Drives
          </div>

          {loading ? (
            <div className="flex items-center gap-2 px-3 py-3 text-sm text-slate-400">
              <Loader2 className="h-4 w-4 animate-spin" /> Loading…
            </div>
          ) : drives.length === 0 ? (
            <div className="px-3 py-3 text-sm text-slate-500">No drives found.</div>
          ) : (
            <ul className="max-h-56 overflow-y-auto">
              {drives.map(d => (
                <li key={d.letter}>
                  <button
                    type="button"
                    onClick={() => { onSelect(d.letter); setOpen(false); }}
                    className="flex w-full items-center gap-3 px-3 py-2.5 text-left hover:bg-slate-800 transition-colors"
                  >
                    <DriveIcon type={d.type} />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-sm font-semibold text-slate-200">
                          {d.letter}
                        </span>
                        <span className="truncate text-xs text-slate-400">{d.label}</span>
                      </div>
                      <div className="flex items-center gap-1.5 text-xs text-slate-500">
                        <span>{d.freeFormatted} free</span>
                        {d.type !== 'Fixed' && (
                          <span className={`rounded px-1 py-px text-[10px] font-medium ${
                            d.type === 'Removable' ? 'bg-amber-900/50 text-amber-300'
                            : d.type === 'Network' ? 'bg-blue-900/50 text-blue-300'
                            : 'bg-slate-700 text-slate-400'
                          }`}>
                            {d.type}
                          </span>
                        )}
                      </div>
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

// ── Main StorageSection ───────────────────────────────────────────────────────

interface StorageSectionProps {
  register: UseFormRegister<any>;
  errors: FieldErrors<any>;
  watch: UseFormWatch<any>;
  setValue: UseFormSetValue<any>;
  /** Passed from EventForm after submit — triggers auto-expand to reveal hidden errors */
  hasError?: boolean;
  /** When editing an existing event, pass its ID so we skip the self-conflict check. */
  excludeEventId?: string;
}

export function StorageSection({
  register,
  errors,
  watch,
  setValue,
  hasError,
  excludeEventId,
}: StorageSectionProps) {
  const watchedFolder: string = watch('watchFolder') ?? '';

  const [validating, setValidating] = useState(false);
  const [validationResult, setValidationResult] = useState<PathValidationResult | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Derive input state for border colour
  const inputState = (() => {
    if (errors.watchFolder) return 'error' as const;
    if (validating)         return 'idle' as const;
    if (!validationResult)  return 'idle' as const;
    if (!validationResult.isValid) return 'error' as const;
    if (validationResult.warning)  return 'warning' as const;
    return 'valid' as const;
  })();

  // Debounced server-side validation — fires 500 ms after user stops typing.
  // Only triggers once client-side Zod passes (path not empty, no .., right length).
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);

    // Skip server call if client-side already rejects it
    if (!watchedFolder || watchedFolder.length < 3 || watchedFolder.includes('..')) {
      setValidationResult(null);
      return;
    }

    debounceRef.current = setTimeout(async () => {
      setValidating(true);
      try {
        const result = await systemApi.validatePath(watchedFolder, excludeEventId);
        setValidationResult(result);
      } catch {
        // Network error — don't block the form, just clear result
        setValidationResult(null);
      } finally {
        setValidating(false);
      }
    }, DEBOUNCE_MS);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watchedFolder, excludeEventId]);

  // When user picks a drive, prefix the current path or set a starter path
  const handleDriveSelect = (letter: string) => {
    const current = watchedFolder.trim();
    if (!current) {
      setValue('watchFolder', `${letter}\\`, { shouldDirty: true, shouldValidate: true });
    } else {
      // Replace the drive portion of an existing path
      const withoutDrive = current.replace(/^[A-Za-z]:\\?/, '');
      setValue('watchFolder', `${letter}\\${withoutDrive}`, { shouldDirty: true, shouldValidate: true });
    }
  };

  return (
    <SectionCard
      icon={<HardDrive className="h-4 w-4" />}
      title="Storage Configuration"
      subtitle="Where photos are watched and stored"
      hasError={hasError}
    >
      <div className="space-y-5">
        {/* Watch Folder */}
        <div>
          <div className="mb-2 flex items-center justify-between">
            <label htmlFor="watch-folder" className={LABEL_CLASS} style={{ marginBottom: 0 }}>
              Watch Folder Path <span className="text-rose-400">*</span>
            </label>
            <DrivePicker onSelect={handleDriveSelect} />
          </div>

          <div className="relative mt-2">
            <FolderOpen className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
            <input
              id="watch-folder"
              type="text"
              placeholder="e.g. D:\Events\Wedding_2026"
              maxLength={512}
              {...register('watchFolder')}
              className={inputCls(inputState, 'pl-10')}
              spellCheck={false}
              autoComplete="off"
            />
          </div>

          <PathStatus
            loading={validating}
            result={validationResult}
            formError={errors.watchFolder?.message as string | undefined}
          />
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
            className={inputCls(errors.galleryRecentCount ? 'error' : 'idle')}
            aria-describedby={errors.galleryRecentCount ? 'count-error' : 'count-hint'}
          />
          {errors.galleryRecentCount ? (
            <p id="count-error" className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-rose-400">
              <AlertCircle className="h-3.5 w-3.5 shrink-0" />
              {errors.galleryRecentCount.message as string}
            </p>
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

