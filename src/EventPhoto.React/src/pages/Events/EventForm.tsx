import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ChevronLeft } from 'lucide-react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { eventsApi } from '../../api/events';
import { Button } from '../../components/UI/Button';
import { Card } from '../../components/UI/Card';
import { Input } from '../../components/UI/Input';

const eventTypes = ['Wedding', 'Reception', 'Birthday', 'Corporate', 'Outdoor', 'Other'] as const;

const today = new Date();
const minDate = new Date(today.getFullYear() - 10, today.getMonth(), today.getDate());
const maxDate = new Date(today.getFullYear() + 5, today.getMonth(), today.getDate());

const schema = z.object({
  name: z
    .string()
    .min(1, 'Event name is required')
    .min(2, 'Event name must be at least 2 characters')
    .max(200, 'Event name must not exceed 200 characters'),
  eventType: z.enum(eventTypes).refine(v => eventTypes.includes(v as typeof eventTypes[number]), 'Please select a valid event type'),
  eventDate: z
    .string()
    .min(1, 'Event date is required')
    .refine(d => !isNaN(Date.parse(d)), 'Event date is invalid')
    .refine(d => new Date(d) >= minDate, 'Event date cannot be more than 10 years in the past')
    .refine(d => new Date(d) <= maxDate, 'Event date cannot be more than 5 years in the future'),
  watchFolder: z
    .string()
    .min(1, 'Watch folder path is required')
    .min(3, 'Watch folder path is too short')
    .max(512, 'Watch folder path must not exceed 512 characters')
    .refine(p => !p.includes('..'), 'Path must not contain traversal sequences (..)'),
  description: z.string().max(2000, 'Description must not exceed 2000 characters').optional(),
  venueName: z
    .string()
    .refine(v => !v || v.length >= 2, 'Venue name must be at least 2 characters')
    .refine(v => !v || v.length <= 200, 'Venue name must not exceed 200 characters')
    .optional(),
  clientName: z
    .string()
    .refine(v => !v || v.length >= 2, 'Client name must be at least 2 characters')
    .refine(v => !v || v.length <= 200, 'Client name must not exceed 200 characters')
    .optional(),
  galleryRecentCount: z
    .number()
    .int('Must be a whole number')
    .min(1, 'Must be at least 1')
    .max(1000, 'Must not exceed 1000')
    .optional(),
  // Face Recognition — no .default() here; defaults are set in useForm defaultValues
  enableFaceRecognition: z.boolean(),
  allowGalleryBrowsing: z.boolean(),
  allowFaceSearch: z.boolean(),
  restrictDownloadsToMatchedPhotos: z.boolean(),
  faceMatchThreshold: z.number().min(0).max(1),
}).refine(
  d => d.allowGalleryBrowsing || d.allowFaceSearch,
  { message: 'At least one of Allow Gallery Browsing or Allow Face Search must be enabled.', path: ['allowGalleryBrowsing'] },
).refine(
  d => !d.allowFaceSearch || d.enableFaceRecognition,
  { message: 'Allow Face Search requires Enable Face Recognition to be turned on.', path: ['allowFaceSearch'] },
);

type FormData = z.infer<typeof schema>;

export default function EventForm() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      eventType: 'Wedding',
      enableFaceRecognition: false,
      allowGalleryBrowsing: true,
      allowFaceSearch: false,
      restrictDownloadsToMatchedPhotos: false,
      faceMatchThreshold: 0.75,
    },
  });

  const enableFaceRecognition = watch('enableFaceRecognition');

  const mutation = useMutation({
    mutationFn: eventsApi.create,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      toast.success('Event created successfully!');
      navigate('/admin/events');
    },
    onError: (error: unknown) => {
      const apiError = (error as { response?: { data?: { error?: string } } })?.response?.data?.error;
      toast.error(apiError ?? 'Failed to create event.');
    },
  });

  const onSubmit = (data: FormData) => mutation.mutate({
    ...data,
    galleryRecentCount: data.galleryRecentCount ?? undefined,
    enableFaceRecognition: data.enableFaceRecognition,
    allowGalleryBrowsing: data.allowGalleryBrowsing,
    allowFaceSearch: data.allowFaceSearch,
    restrictDownloadsToMatchedPhotos: data.restrictDownloadsToMatchedPhotos,
    faceMatchThreshold: data.faceMatchThreshold,
  });

  return (
    <div className="max-w-2xl space-y-6">
      <Link
        to="/admin/events"
        className="inline-flex items-center gap-1 text-sm text-gray-500 transition-colors hover:text-gray-900"
      >
        <ChevronLeft className="h-4 w-4" />
        Back to Events
      </Link>
      <h1 className="text-2xl font-bold text-gray-900">New Event</h1>
      <Card className="p-6">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input label="Event Name *" {...register('name')} error={errors.name?.message} />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Event Type *</label>
            <select
              {...register('eventType')}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              {eventTypes.map((eventType) => (
                <option key={eventType} value={eventType}>
                  {eventType}
                </option>
              ))}
            </select>
          </div>
          <Input label="Event Date *" type="date" {...register('eventDate')} error={errors.eventDate?.message} />
          <Input
            label="Watch Folder Path *"
            placeholder="e.g. D:\\Events\\Wedding_2024"
            {...register('watchFolder')}
            error={errors.watchFolder?.message}
          />
          <Input label="Client Name" placeholder="e.g. John Smith" {...register('clientName')} error={errors.clientName?.message} />
          <Input label="Venue Name" placeholder="e.g. Grand Ballroom" {...register('venueName')} error={errors.venueName?.message} />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Description</label>
            <textarea
              {...register('description')}
              rows={3}
              maxLength={2000}
              placeholder="Optional notes about the event…"
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            {errors.description ? <p className="mt-1 text-xs text-red-600">{errors.description.message}</p> : null}
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Gallery Recent Image Count</label>
            <input
              type="number"
              min={1}
              max={1000}
              placeholder="e.g. 20 — leave blank to show all photos"
              {...register('galleryRecentCount', { valueAsNumber: true })}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            {errors.galleryRecentCount
              ? <p className="mt-1 text-xs text-red-600">{errors.galleryRecentCount.message}</p>
              : <p className="mt-1 text-xs text-gray-500">Limits the gallery to the N most recently captured photos. Leave blank to show all.</p>}
          </div>
          {/* ── Face Recognition ─────────────────────────────────────── */}
          <div className="rounded-xl border border-indigo-100 bg-indigo-50 p-4 space-y-3">
            <h3 className="text-sm font-semibold text-indigo-800">🔍 Face Recognition</h3>

            <label className="flex items-center gap-3 cursor-pointer">
              <input type="checkbox" {...register('enableFaceRecognition')} className="h-4 w-4 rounded text-indigo-600" />
              <span className="text-sm text-gray-700">Enable Face Recognition</span>
            </label>

            {enableFaceRecognition && (
              <div className="pl-4 space-y-2 border-l-2 border-indigo-200">
                <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Gallery Access</p>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input type="checkbox" {...register('allowGalleryBrowsing')} className="h-4 w-4 rounded text-indigo-600" />
                  <span className="text-sm text-gray-700">Allow Gallery Browsing</span>
                </label>
                {errors.allowGalleryBrowsing && (
                  <p className="text-xs text-red-500">{errors.allowGalleryBrowsing.message}</p>
                )}

                <label className="flex items-center gap-3 cursor-pointer">
                  <input type="checkbox" {...register('allowFaceSearch')} className="h-4 w-4 rounded text-indigo-600" />
                  <span className="text-sm text-gray-700">Allow Face Search (selfie upload)</span>
                </label>
                {errors.allowFaceSearch && (
                  <p className="text-xs text-red-500">{errors.allowFaceSearch.message}</p>
                )}

                <p className="text-xs text-gray-500 font-medium uppercase tracking-wide pt-1">Downloads</p>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input type="checkbox" {...register('restrictDownloadsToMatchedPhotos')} className="h-4 w-4 rounded text-indigo-600" />
                  <span className="text-sm text-gray-700">Restrict Downloads to Matched Photos</span>
                </label>

                <p className="text-xs text-gray-500 font-medium uppercase tracking-wide pt-1">Match Threshold</p>
                <div className="flex items-center gap-3">
                  <input
                    type="range"
                    min={0.5}
                    max={0.99}
                    step={0.01}
                    {...register('faceMatchThreshold', { valueAsNumber: true })}
                    className="flex-1 accent-indigo-600"
                  />
                  <span className="w-12 text-sm text-center font-mono text-indigo-700">
                    {(watch('faceMatchThreshold') ?? 0.75).toFixed(2)}
                  </span>
                </div>
                <p className="text-xs text-gray-400">
                  Higher = stricter matching. Recommended: 0.75 – 0.85
                </p>
              </div>
            )}
          </div>

          <div className="flex gap-3 pt-2">
            <Button type="submit" isLoading={mutation.isPending}>
              Create Event
            </Button>
            <Button type="button" variant="secondary" onClick={() => navigate(-1)}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
