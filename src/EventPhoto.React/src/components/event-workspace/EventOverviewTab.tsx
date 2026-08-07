import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { CheckCircle, Edit3, Loader2, RotateCcw, Save } from 'lucide-react';
import toast from 'react-hot-toast';
import { workspaceApi } from '../../api/workspace';
import { apiError } from '../../utils/errorHandler';
import type { EventWorkspaceResponse, UpdateEventOverviewRequest } from '../../types';

const EVENT_TYPES = ['Wedding', 'Reception', 'Birthday', 'Corporate', 'Concert', 'Outdoor', 'Other'];

interface EventOverviewTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventOverviewTab({ workspace }: EventOverviewTabProps) {
  const qc = useQueryClient();
  const [dirty, setDirty] = useState(false);

  const defaults: UpdateEventOverviewRequest = {
    name: workspace.name,
    eventType: workspace.eventType,
    eventDate: workspace.eventDate,
    description: workspace.description ?? '',
    venueName: workspace.venueName ?? '',
    clientName: workspace.clientName ?? '',
  };

  const { register, handleSubmit, reset, formState: { errors }, watch } = useForm<UpdateEventOverviewRequest>({
    defaultValues: defaults,
  });

  // Track dirty state
  watch(() => setDirty(true));

  const mutation = useMutation({
    mutationFn: (data: UpdateEventOverviewRequest) =>
      workspaceApi.updateOverview(workspace.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['workspace', workspace.id] });
      qc.invalidateQueries({ queryKey: ['events'] });
      toast.success('Event overview saved.');
      setDirty(false);
    },
    onError: (e) => apiError(e, 'Failed to save event overview.'),
  });

  const onSubmit = handleSubmit((data) => mutation.mutate(data));

  const handleDiscard = () => {
    reset(defaults);
    setDirty(false);
  };

  return (
    <div className="space-y-6">
      {/* Editable fields card */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-2">
          <Edit3 className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Event Information</h2>
        </div>

        <form onSubmit={onSubmit} className="space-y-5">
          {/* Name + Type row */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Event Name" error={errors.name?.message} required>
              <input
                {...register('name', { required: 'Event name is required', minLength: { value: 2, message: 'Min 2 characters' }, maxLength: { value: 200, message: 'Max 200 characters' } })}
                className={inputCls(!!errors.name)}
                placeholder="Summer Wedding 2026"
              />
            </Field>
            <Field label="Event Type" error={errors.eventType?.message} required>
              <select {...register('eventType', { required: 'Event type is required' })} className={inputCls(!!errors.eventType)}>
                {EVENT_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </Field>
          </div>

          {/* Date + Venue row */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Event Date" error={errors.eventDate?.message} required>
              <input
                {...register('eventDate', { required: 'Event date is required' })}
                type="date"
                className={inputCls(!!errors.eventDate)}
              />
            </Field>
            <Field label="Venue Name" error={errors.venueName?.message}>
              <input
                {...register('venueName')}
                className={inputCls(false)}
                placeholder="Grand Ballroom, New York"
              />
            </Field>
          </div>

          {/* Client Name */}
          <Field label="Client Name" error={errors.clientName?.message}>
            <input
              {...register('clientName')}
              className={inputCls(false)}
              placeholder="Jane & John Smith"
            />
          </Field>

          {/* Description */}
          <Field label="Description" error={errors.description?.message}>
            <textarea
              {...register('description', { maxLength: { value: 2000, message: 'Max 2000 characters' } })}
              rows={4}
              className={`${inputCls(!!errors.description)} resize-none`}
              placeholder="Beautiful outdoor ceremony with 150 guests…"
            />
          </Field>

          {/* Save / Discard */}
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
                  <RotateCcw className="h-3.5 w-3.5" /> Discard Changes
                </button>
                <button
                  type="submit"
                  disabled={mutation.isPending}
                  className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2 text-sm font-semibold text-white transition-colors hover:bg-indigo-500 disabled:opacity-60"
                >
                  {mutation.isPending ? (
                    <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</>
                  ) : (
                    <><Save className="h-4 w-4" /> Save Changes</>
                  )}
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </div>

      {/* Read-only metadata card */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-2">
          <CheckCircle className="h-4 w-4 text-slate-500" />
          <h2 className="text-sm font-semibold text-white">System Information</h2>
          <span className="ml-auto rounded-full bg-slate-800 px-2.5 py-0.5 text-xs text-slate-500">Read only</span>
        </div>
        <dl className="grid grid-cols-2 gap-x-6 gap-y-4 sm:grid-cols-3">
          <ReadField label="Event ID" value={workspace.id} mono />
          <ReadField label="Created" value={new Date(workspace.createdAt).toLocaleDateString()} />
          <ReadField label="Photo Count" value={workspace.photoCount.toLocaleString()} />
          <ReadField label="Total Downloads" value={workspace.totalDownloads.toLocaleString()} />
          <ReadField label="Storage Used" value={workspace.totalSize} />
        </dl>
      </div>
    </div>
  );
}

function Field({ label, error, required, children }: {
  label: string;
  error?: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
        {label} {required && <span className="text-rose-400">*</span>}
      </label>
      {children}
      {error && <p className="text-xs text-rose-400">{error}</p>}
    </div>
  );
}

function ReadField({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wider text-slate-500">{label}</dt>
      <dd className={`mt-1 text-sm text-slate-300 ${mono ? 'truncate font-mono text-xs' : ''}`}>{value}</dd>
    </div>
  );
}

function inputCls(hasError: boolean) {
  return [
    'w-full rounded-xl border bg-slate-800 px-3.5 py-2.5 text-sm text-white placeholder-slate-500',
    'transition-colors focus:outline-none focus:ring-2',
    hasError
      ? 'border-rose-500 focus:ring-rose-500/40'
      : 'border-slate-700 focus:border-indigo-500 focus:ring-indigo-500/20',
  ].join(' ');
}
