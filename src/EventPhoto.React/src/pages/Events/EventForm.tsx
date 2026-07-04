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
  eventType: z.enum(eventTypes, { errorMap: () => ({ message: 'Please select a valid event type' }) }),
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
