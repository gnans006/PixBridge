import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Settings2 } from 'lucide-react';
import type { ApplicationSettings } from '../../../api/applicationSettings';
import { useUpdateApplicationSettings } from '../../../hooks/useApplicationSettings';
import { Button } from '../../UI/Button';

interface Props {
  settings: ApplicationSettings;
}

interface FormValues {
  defaultEventGalleryMode: string;
  enableWatermarkByDefault: boolean;
  enableFaceRecognitionByDefault: boolean;
}

const galleryModes = [
  { value: 'GalleryOnly', label: 'Gallery Only', description: 'Guests browse all photos freely.' },
  { value: 'FaceSearchOnly', label: 'Face Search Only', description: 'Guests find their own photos via face search.' },
  { value: 'Hybrid', label: 'Hybrid', description: 'Guests can browse or use face search.' },
];

export function DefaultsTab({ settings }: Props) {
  const { mutate, isPending } = useUpdateApplicationSettings();

  const { register, handleSubmit, reset, watch, setValue, formState: { isDirty } } = useForm<FormValues>({
    defaultValues: {
      defaultEventGalleryMode: settings.defaultEventGalleryMode,
      enableWatermarkByDefault: settings.enableWatermarkByDefault,
      enableFaceRecognitionByDefault: settings.enableFaceRecognitionByDefault,
    },
  });

  useEffect(() => {
    reset({
      defaultEventGalleryMode: settings.defaultEventGalleryMode,
      enableWatermarkByDefault: settings.enableWatermarkByDefault,
      enableFaceRecognitionByDefault: settings.enableFaceRecognitionByDefault,
    });
  }, [settings, reset]);

  const selectedMode = watch('defaultEventGalleryMode');

  const onSubmit = (values: FormValues) => {
    mutate({
      ...settings,
      defaultEventGalleryMode: values.defaultEventGalleryMode,
      enableWatermarkByDefault: values.enableWatermarkByDefault,
      enableFaceRecognitionByDefault: values.enableFaceRecognitionByDefault,
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-6 flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-500/10">
            <Settings2 className="h-5 w-5 text-amber-400" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-white">Event Defaults</h2>
            <p className="text-xs text-slate-400">
              Applied automatically when creating new events. Can be overridden per event.
            </p>
          </div>
        </div>

        <div className="space-y-6">
          {/* Gallery mode selector */}
          <div>
            <label className="mb-3 block text-xs font-medium text-slate-300">Default Gallery Mode</label>
            <div className="grid gap-3 sm:grid-cols-3">
              {galleryModes.map((mode) => (
                <button
                  key={mode.value}
                  type="button"
                  onClick={() => setValue('defaultEventGalleryMode', mode.value, { shouldDirty: true })}
                  className={`flex flex-col gap-1 rounded-xl border p-4 text-left transition-all ${
                    selectedMode === mode.value
                      ? 'border-indigo-500 bg-indigo-500/10'
                      : 'border-slate-700 bg-slate-800/50 hover:border-slate-600'
                  }`}
                >
                  <span className="text-sm font-medium text-white">{mode.label}</span>
                  <span className="text-xs text-slate-400">{mode.description}</span>
                </button>
              ))}
            </div>
          </div>

          {/* Toggle rows */}
          <div className="divide-y divide-slate-800">
            {[
              {
                field: 'enableWatermarkByDefault' as const,
                label: 'Enable Watermark by Default',
                description: 'New events will have watermarking pre-enabled.',
              },
              {
                field: 'enableFaceRecognitionByDefault' as const,
                label: 'Enable Face Recognition by Default',
                description: 'New events will have face recognition pre-enabled.',
              },
            ].map(({ field, label, description }) => (
              <div key={field} className="flex items-center justify-between py-4">
                <div>
                  <p className="text-sm font-medium text-white">{label}</p>
                  <p className="text-xs text-slate-400">{description}</p>
                </div>
                <label className="relative inline-flex cursor-pointer items-center">
                  <input type="checkbox" className="peer sr-only" {...register(field)} />
                  <div className="peer h-6 w-11 rounded-full bg-slate-700 after:absolute after:left-[2px]
                    after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all
                    peer-checked:bg-indigo-600 peer-checked:after:translate-x-full" />
                </label>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={!isDirty || isPending}>
          {isPending ? 'Saving…' : 'Save Defaults'}
        </Button>
      </div>
    </form>
  );
}
