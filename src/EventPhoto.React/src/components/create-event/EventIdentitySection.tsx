import type { UseFormRegister, UseFormWatch, UseFormSetValue, FieldErrors } from 'react-hook-form';
import { AlertCircle, Calendar, Tag, Type } from 'lucide-react';
import { EventCoverUploader } from './EventCoverUploader';
import { SectionCard } from './SectionCard';

const EVENT_TYPES = [
  { value: 'Wedding', icon: '💍' },
  { value: 'Reception', icon: '🥂' },
  { value: 'Birthday', icon: '🎂' },
  { value: 'Corporate', icon: '💼' },
  { value: 'Outdoor', icon: '🌿' },
  { value: 'Other', icon: '📷' },
] as const;

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

function FieldError({ message }: { message: string }) {
  return (
    <p className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-rose-400">
      <AlertCircle className="h-3.5 w-3.5 shrink-0" />
      {message}
    </p>
  );
}

interface EventIdentitySectionProps {
  register: UseFormRegister<any>;
  watch: UseFormWatch<any>;
  setValue: UseFormSetValue<any>;
  errors: FieldErrors<any>;
  coverImage: string | null;
  onCoverImageChange: (url: string | null) => void;
}

export function EventIdentitySection({
  register,
  watch,
  setValue,
  errors,
  coverImage,
  onCoverImageChange,
}: EventIdentitySectionProps) {
  const selectedType = watch('eventType') as string;

  return (
    <SectionCard
      icon={<Tag className="h-4 w-4" />}
      title="Event Identity"
      subtitle="Name, type and date"
      defaultOpen
      collapsible={false}
    >
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-[180px_1fr]">
        {/* Cover image uploader */}
        <EventCoverUploader value={coverImage} onChange={onCoverImageChange} />

        {/* Right column: fields */}
        <div className="space-y-4">
          {/* Event Name */}
          <div>
            <label htmlFor="event-name" className={LABEL_CLASS}>
              Event Name <span className="text-rose-400">*</span>
            </label>
            <div className="relative">
              <Type className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-500" />
              <input
                id="event-name"
                type="text"
                placeholder="e.g. Arun ❤️ Priya Wedding"
                maxLength={200}
                {...register('name')}
                className={inputCls(!!errors.name, 'pl-9')}
                aria-describedby={errors.name ? 'name-error' : undefined}
              />
            </div>
            {errors.name && <FieldError message={errors.name.message as string} />}
          </div>

          {/* Event Type — pill selector */}
          <div>
            <label className={LABEL_CLASS}>
              Event Type <span className="text-rose-400">*</span>
            </label>
            <div className="flex flex-wrap gap-2" role="group" aria-label="Select event type">
              {EVENT_TYPES.map(({ value, icon }) => {
                const isActive = selectedType === value;
                return (
                  <button
                    key={value}
                    type="button"
                    onClick={() => setValue('eventType', value, { shouldValidate: true })}
                    className={`flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium transition-all ${
                      isActive
                        ? 'bg-indigo-600 text-white shadow-[0_0_10px_rgba(99,102,241,0.4)]'
                        : 'bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-slate-200'
                    }`}
                    aria-pressed={isActive}
                  >
                    <span>{icon}</span>
                    {value}
                  </button>
                );
              })}
            </div>
            {errors.eventType && <FieldError message={errors.eventType.message as string} />}
          </div>

          {/* Event Date */}
          <div>
            <label htmlFor="event-date" className={LABEL_CLASS}>
              Event Date <span className="text-rose-400">*</span>
            </label>
            <div className="relative">
              <Calendar className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-500" />
              <input
                id="event-date"
                type="date"
                {...register('eventDate')}
                className={inputCls(!!errors.eventDate, 'pl-9 [color-scheme:dark]')}
                aria-describedby={errors.eventDate ? 'date-error' : undefined}
              />
            </div>
            {errors.eventDate && <FieldError message={errors.eventDate.message as string} />}
          </div>
        </div>
      </div>
    </SectionCard>
  );
}
