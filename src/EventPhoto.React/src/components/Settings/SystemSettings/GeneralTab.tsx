import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Building2, Server } from 'lucide-react';
import type { ApplicationSettings } from '../../../api/applicationSettings';
import { useUpdateApplicationSettings } from '../../../hooks/useApplicationSettings';
import { Button } from '../../UI/Button';
import { Input } from '../../UI/Input';

interface Props {
  settings: ApplicationSettings;
}

interface FormValues {
  studioName: string;
  serverName: string;
}

export function GeneralTab({ settings }: Props) {
  const { mutate, isPending } = useUpdateApplicationSettings();

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<FormValues>({
    defaultValues: {
      studioName: settings.studioName,
      serverName: settings.serverName,
    },
  });

  // Sync when parent settings refresh
  useEffect(() => {
    reset({ studioName: settings.studioName, serverName: settings.serverName });
  }, [settings, reset]);

  const onSubmit = (values: FormValues) => {
    mutate({
      ...settings,
      studioName: values.studioName,
      serverName: values.serverName,
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-6 flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-500/10">
            <Building2 className="h-5 w-5 text-indigo-400" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-white">Studio Identity</h2>
            <p className="text-xs text-slate-400">
              These details appear in watermarks, QR codes, and the guest gallery.
            </p>
          </div>
        </div>

        <div className="space-y-5">
          <div>
            <label className="mb-1.5 block text-xs font-medium text-slate-300">Studio Name</label>
            <Input
              {...register('studioName', {
                required: 'Studio name is required.',
                maxLength: { value: 200, message: 'Must not exceed 200 characters.' },
              })}
              placeholder="e.g. ABC Digital Studio"
            />
            {errors.studioName && (
              <p className="mt-1 text-xs text-red-400">{errors.studioName.message}</p>
            )}
          </div>

          <div>
            <label className="mb-1.5 block text-xs font-medium text-slate-300">
              <Server className="mr-1 inline h-3.5 w-3.5 text-slate-500" />
              Server Name
            </label>
            <Input
              {...register('serverName', {
                required: 'Server name is required.',
                maxLength: { value: 100, message: 'Must not exceed 100 characters.' },
              })}
              placeholder="e.g. PIXBRIDGE-SERVER"
            />
            {errors.serverName && (
              <p className="mt-1 text-xs text-red-400">{errors.serverName.message}</p>
            )}
            <p className="mt-1 text-xs text-slate-500">
              Displayed in the network panel. Defaults to the machine name.
            </p>
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={!isDirty || isPending}>
          {isPending ? 'Saving…' : 'Save Changes'}
        </Button>
      </div>
    </form>
  );
}
