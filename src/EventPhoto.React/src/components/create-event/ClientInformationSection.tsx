import type { UseFormRegister, FieldErrors } from 'react-hook-form';
import { AlertCircle, Users } from 'lucide-react';
import { SectionCard } from './SectionCard';

const LABEL_CLASS = 'mb-2 block text-sm font-semibold text-slate-200';

function inputCls(hasError: boolean, extra = '') {
  return [
    'w-full rounded-xl border bg-slate-800 px-3 py-2.5 text-sm text-slate-100 placeholder:text-slate-500 transition-colors focus:outline-none focus:ring-2',
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

interface ClientInformationSectionProps {
  register: UseFormRegister<any>;
  errors: FieldErrors<any>;
}

export function ClientInformationSection({ register, errors }: ClientInformationSectionProps) {
  return (
    <SectionCard
      icon={<Users className="h-4 w-4" />}
      title="Client Information"
      subtitle="Client, venue and notes"
      defaultOpen
    >
      <div className="space-y-4">
        {/* Row: Client Name + Venue Name */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label htmlFor="client-name" className={LABEL_CLASS}>
              Client Name
            </label>
            <input
              id="client-name"
              type="text"
              placeholder="e.g. John & Sarah Smith"
              maxLength={200}
              {...register('clientName')}
              className={inputCls(!!errors.clientName)}
              aria-describedby={errors.clientName ? 'client-name-error' : undefined}
            />
            {errors.clientName && (
              <FieldError id="client-name-error" message={errors.clientName.message as string} />
            )}
          </div>

          <div>
            <label htmlFor="venue-name" className={LABEL_CLASS}>
              Venue Name
            </label>
            <input
              id="venue-name"
              type="text"
              placeholder="e.g. The Grand Ballroom"
              maxLength={200}
              {...register('venueName')}
              className={inputCls(!!errors.venueName)}
              aria-describedby={errors.venueName ? 'venue-name-error' : undefined}
            />
            {errors.venueName && (
              <FieldError id="venue-name-error" message={errors.venueName.message as string} />
            )}
          </div>
        </div>

        {/* Description */}
        <div>
          <label htmlFor="description" className={LABEL_CLASS}>
            Description
          </label>
          <textarea
            id="description"
            rows={3}
            maxLength={2000}
            placeholder="Optional notes about the event, package, or special requests…"
            {...register('description')}
            className={inputCls(!!errors.description, 'resize-none leading-relaxed')}
            aria-describedby={errors.description ? 'desc-error' : undefined}
          />
          {errors.description ? (
            <FieldError id="desc-error" message={errors.description.message as string} />
          ) : null}
        </div>
      </div>
    </SectionCard>
  );
}
