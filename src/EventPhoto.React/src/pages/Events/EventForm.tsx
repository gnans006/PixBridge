import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { AlertCircle, ArrowLeft, PlusCircle } from 'lucide-react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { eventsApi } from '../../api/events';
import { watermarkApi } from '../../api/watermark';
import { apiError } from '../../utils/errorHandler';
import type { UpsertWatermarkConfigRequest } from '../../types';

import { EventIdentitySection } from '../../components/create-event/EventIdentitySection';
import { ClientInformationSection } from '../../components/create-event/ClientInformationSection';
import { GalleryExperienceSection } from '../../components/create-event/GalleryExperienceSection';
import { AiFeaturesSection } from '../../components/create-event/AiFeaturesSection';
import { WatermarkSection } from '../../components/create-event/WatermarkSection';
import { StorageSection } from '../../components/create-event/StorageSection';
import { EventSummarySidebar } from '../../components/create-event/EventSummarySidebar';
import { CreateEventWatermarkModal } from '../../components/create-event/CreateEventWatermarkModal';
import type { GalleryMode } from '../../components/create-event/GalleryModeCards';

// ── Validation schema (unchanged payload) ─────────────────────────────────────

const eventTypes = ['Wedding', 'Reception', 'Birthday', 'Corporate', 'Outdoor', 'Other'] as const;

const today = new Date();
const minDate = new Date(today.getFullYear() - 10, today.getMonth(), today.getDate());
const maxDate = new Date(today.getFullYear() + 5, today.getMonth(), today.getDate());

const schema = z
  .object({
    name: z
      .string()
      .min(1, 'Event name is required')
      .min(2, 'Event name must be at least 2 characters')
      .max(200, 'Event name must not exceed 200 characters'),
    eventType: z
      .enum(eventTypes)
      .refine((v) => eventTypes.includes(v as (typeof eventTypes)[number]), 'Please select a valid event type'),
    eventDate: z
      .string()
      .min(1, 'Event date is required')
      .refine((d) => !isNaN(Date.parse(d)), 'Event date is invalid')
      .refine((d) => new Date(d) >= minDate, 'Event date cannot be more than 10 years in the past')
      .refine((d) => new Date(d) <= maxDate, 'Event date cannot be more than 5 years in the future'),
    watchFolder: z
      .string()
      .min(1, 'Watch folder path is required')
      .min(3, 'Watch folder path is too short')
      .max(512, 'Watch folder path must not exceed 512 characters')
      .refine((p) => !p.includes('..'), 'Path must not contain traversal sequences (..)'),
    description: z.string().max(2000, 'Description must not exceed 2000 characters').optional(),
    venueName: z
      .string()
      .refine((v) => !v || v.length >= 2, 'Venue name must be at least 2 characters')
      .refine((v) => !v || v.length <= 200, 'Venue name must not exceed 200 characters')
      .optional(),
    clientName: z
      .string()
      .refine((v) => !v || v.length >= 2, 'Client name must be at least 2 characters')
      .refine((v) => !v || v.length <= 200, 'Client name must not exceed 200 characters')
      .optional(),
    galleryRecentCount: z
      .number()
      .int('Must be a whole number')
      .min(1, 'Must be at least 1')
      .max(1000, 'Must not exceed 1000')
      .optional(),
    enableFaceRecognition: z.boolean(),
    allowGalleryBrowsing: z.boolean(),
    allowFaceSearch: z.boolean(),
    restrictDownloadsToMatchedPhotos: z.boolean(),
    faceMatchThreshold: z.number().min(0).max(1),
  })
  .refine((d) => d.allowGalleryBrowsing || d.allowFaceSearch, {
    message: 'At least one of Allow Gallery Browsing or Allow Face Search must be enabled.',
    path: ['allowGalleryBrowsing'],
  })
  .refine((d) => !d.allowFaceSearch || d.enableFaceRecognition, {
    message: 'Allow Face Search requires Enable Face Recognition to be turned on.',
    path: ['allowFaceSearch'],
  });

type FormData = z.infer<typeof schema>;

// ── Default watermark config ───────────────────────────────────────────────────

const DEFAULT_WATERMARK_CONFIG: UpsertWatermarkConfigRequest = {
  enabled: false,
  mode: 'StudioAndEvent',
  style: 'BottomRibbon',
  opacity: 0.7,
  scale: 'Medium',
  customText: '',
  template: '{StudioName} · {EventName}',
  logoPath: '',
  includeStudioName: true,
  includeEventName: true,
  includeDownloadDate: false,
  applyOnDownload: true,
  textColor: '#FFFFFF',
  fontName: 'Montserrat',
  backgroundOpacity: 0.4,
  applyOnPreview: false,
};

function resolveGalleryMode(browsing: boolean, faceSearch: boolean): GalleryMode {
  if (browsing && faceSearch) return 'HybridMode';
  if (!browsing && faceSearch) return 'FaceSearchOnly';
  return 'GalleryOnly';
}

// ── Component ──────────────────────────────────────────────────────────────────

export default function EventForm() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [coverImage, setCoverImage] = useState<string | null>(null);
  const [watermarkConfig, setWatermarkConfig] = useState<UpsertWatermarkConfigRequest>(DEFAULT_WATERMARK_CONFIG);
  const [watermarkModalOpen, setWatermarkModalOpen] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitted },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    mode: 'onTouched',       // validate after blur; no premature errors while typing
    reValidateMode: 'onChange', // re-validate on change after first touch
    defaultValues: {
      eventType: 'Wedding',
      enableFaceRecognition: false,
      allowGalleryBrowsing: true,
      allowFaceSearch: false,
      restrictDownloadsToMatchedPhotos: false,
      faceMatchThreshold: 0.75,
    },
  });

  const watchedName = watch('name') ?? '';
  const watchedType = watch('eventType') ?? 'Wedding';
  const watchedDate = watch('eventDate') ?? '';
  const watchedFolder = watch('watchFolder') ?? '';
  const allowBrowsing = watch('allowGalleryBrowsing');
  const allowFaceSearch = watch('allowFaceSearch');
  const enableFaceRecognition = watch('enableFaceRecognition');

  // Sidebar shows "Ready" as soon as the three required fields are filled
  const isReady = !!watchedName.trim() && !!watchedDate && !!watchedFolder.trim();

  const galleryMode = resolveGalleryMode(allowBrowsing, allowFaceSearch);
  const faceSearchRequired = galleryMode === 'FaceSearchOnly' || galleryMode === 'HybridMode';

  const handleGalleryModeChange = (mode: GalleryMode) => {
    switch (mode) {
      case 'GalleryOnly':
        setValue('allowGalleryBrowsing', true, { shouldValidate: true });
        setValue('allowFaceSearch', false, { shouldValidate: true });
        break;
      case 'FaceSearchOnly':
        setValue('allowGalleryBrowsing', false, { shouldValidate: true });
        setValue('allowFaceSearch', true, { shouldValidate: true });
        setValue('enableFaceRecognition', true, { shouldValidate: true });
        break;
      case 'HybridMode':
        setValue('allowGalleryBrowsing', true, { shouldValidate: true });
        setValue('allowFaceSearch', true, { shouldValidate: true });
        setValue('enableFaceRecognition', true, { shouldValidate: true });
        break;
    }
  };

  const mutation = useMutation({
    mutationFn: eventsApi.create,
    onSuccess: async (response) => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      const eventId = response?.data?.id;
      if (eventId && watermarkConfig.enabled) {
        try {
          await watermarkApi.upsertConfig(eventId, watermarkConfig);
        } catch {
          toast.error('Watermark setup failed — configure it from the event settings.', { duration: 6000 });
        }
      }
      toast.success('Event created successfully!');
      if (eventId) {
        navigate(`/admin/events/${eventId}`);
      } else {
        navigate('/admin/events');
      }
    },
    onError: (error: unknown) => {
      apiError(error, 'Failed to create event.');
    },
  });

  const onSubmit = (data: FormData) =>
    mutation.mutate({
      ...data,
      galleryRecentCount: data.galleryRecentCount ?? undefined,
    });

  const galleryModeError =
    (errors.allowGalleryBrowsing?.message as string | undefined) ||
    (errors.allowFaceSearch?.message as string | undefined);

  return (
    <>
      <form
        onSubmit={handleSubmit(onSubmit)}
        noValidate
        className="-m-4 min-h-screen bg-slate-950 px-4 py-8 sm:-m-6 sm:px-6"
      >
        <div className="mx-auto max-w-[1400px]">
          {/* Page header */}
          <div className="mb-8">
            <Link
              to="/admin/events"
              className="inline-flex items-center gap-1.5 text-sm text-slate-500 transition-colors hover:text-slate-300"
            >
              <ArrowLeft className="h-4 w-4" />
              Back to Events
            </Link>
            <h1 className="mt-3 text-2xl font-bold text-slate-100">New Event</h1>
            <p className="mt-1 text-sm text-slate-400">
              Set up a new photography event with gallery access, AI face recognition and studio branding.
            </p>
          </div>

          {/* 70/30 grid */}
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_340px]">
            {/* Left column */}
            <div className="space-y-4">
              <EventIdentitySection
                register={register}
                watch={watch}
                setValue={setValue}
                errors={errors}
                coverImage={coverImage}
                onCoverImageChange={setCoverImage}
              />

              <ClientInformationSection register={register} errors={errors} />

              <GalleryExperienceSection
                value={galleryMode}
                onChange={handleGalleryModeChange}
                error={galleryModeError}
              />

              <AiFeaturesSection
                watch={watch}
                setValue={setValue}
                faceSearchRequired={faceSearchRequired}
              />

              <WatermarkSection
                config={watermarkConfig}
                onToggleEnabled={(v) => setWatermarkConfig((c) => ({ ...c, enabled: v }))}
                onOpenModal={() => setWatermarkModalOpen(true)}
              />

              <StorageSection
                register={register}
                errors={errors}
                hasError={isSubmitted && !!(errors.watchFolder || errors.galleryRecentCount)}
              />

              {/* Submit row */}
              <motion.div layout="position" className="flex flex-wrap items-center gap-4 pt-2">
                <motion.button
                  type="submit"
                  disabled={mutation.isPending}
                  whileHover={{ scale: mutation.isPending ? 1 : 1.02 }}
                  whileTap={{ scale: mutation.isPending ? 1 : 0.98 }}
                  className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-[0_0_20px_rgba(99,102,241,0.35)] transition-colors hover:bg-indigo-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-950 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {mutation.isPending ? (
                    <>
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                      Creating…
                    </>
                  ) : (
                    <>
                      <PlusCircle className="h-4 w-4" />
                      Create Event
                    </>
                  )}
                </motion.button>

                <Link
                  to="/admin/events"
                  className="rounded-xl border border-slate-700 px-5 py-2.5 text-sm font-medium text-slate-400 transition-colors hover:border-slate-600 hover:text-slate-200"
                >
                  Cancel
                </Link>
              </motion.div>

              {/* Validation error banner — only shown after the first submit attempt */}
              {isSubmitted && Object.keys(errors).length > 0 && !mutation.isPending && (
                <motion.div
                  initial={{ opacity: 0, y: 6 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="flex items-start gap-3 rounded-xl border border-rose-500/40 bg-rose-500/10 px-4 py-3"
                >
                  <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-rose-400" />
                  <div>
                    <p className="text-sm font-semibold text-rose-300">Some fields need attention</p>
                    <p className="mt-0.5 text-xs text-rose-400/80">
                      Fields with a red border above contain errors. Fix them before creating the event.
                    </p>
                  </div>
                </motion.div>
              )}
            </div>

            {/* Right column — sticky summary (desktop only) */}
            <div className="hidden lg:block">
              <EventSummarySidebar
                coverImage={coverImage}
                name={watchedName}
                eventType={watchedType}
                eventDate={watchedDate}
                galleryMode={galleryMode}
                faceRecognitionEnabled={enableFaceRecognition}
                watermarkEnabled={watermarkConfig.enabled}
                isReady={isReady}
              />
            </div>
          </div>
        </div>
      </form>

      <CreateEventWatermarkModal
        isOpen={watermarkModalOpen}
        onClose={() => setWatermarkModalOpen(false)}
        config={watermarkConfig}
        onChange={setWatermarkConfig}
        eventName={watchedName}
      />
    </>
  );
}
