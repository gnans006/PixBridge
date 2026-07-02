import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { eventsApi } from '../../api/events';
import { Button } from '../../components/UI/Button';
import { Card } from '../../components/UI/Card';
import { Input } from '../../components/UI/Input';

const eventTypes = ['Wedding', 'Reception', 'Birthday', 'Corporate', 'Outdoor', 'Other'] as const;

const schema = z.object({
  name: z.string().min(1, 'Event name is required').max(200),
  eventType: z.enum(eventTypes),
  eventDate: z.string().min(1, 'Event date is required'),
  watchFolder: z.string().min(1, 'Watch folder path is required'),
  description: z.string().optional(),
  venueName: z.string().optional(),
  clientName: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

export default function EventForm() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { eventType: 'Wedding' },
  });

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

  const onSubmit = (data: FormData) => mutation.mutate(data);

  return (
    <div className="max-w-2xl space-y-6">
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
          <Input label="Client Name" {...register('clientName')} />
          <Input label="Venue Name" {...register('venueName')} />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Description</label>
            <textarea
              {...register('description')}
              rows={3}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
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
