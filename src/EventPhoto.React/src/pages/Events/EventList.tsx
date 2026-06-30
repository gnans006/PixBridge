import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Eye, Plus, Power, QrCode, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';
import { Link } from 'react-router-dom';
import { eventsApi } from '../../api/events';
import { Badge } from '../../components/UI/Badge';
import { Button } from '../../components/UI/Button';
import { Card } from '../../components/UI/Card';
import { Spinner } from '../../components/UI/Spinner';
import { formatDate } from '../../utils/format';

export default function EventList() {
  const queryClient = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const response = await eventsApi.getAll();
      return response.data ?? [];
    },
  });

  const deleteMutation = useMutation({
    mutationFn: eventsApi.delete,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      toast.success('Event deleted.');
    },
    onError: () => toast.error('Failed to delete event.'),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) => eventsApi.toggleActive(id, activate),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events'] });
      toast.success('Event updated.');
    },
    onError: () => toast.error('Failed to update event.'),
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Events</h1>
        <Link to="/admin/events/new">
          <Button>
            <Plus className="h-4 w-4" />
            New Event
          </Button>
        </Link>
      </div>

      {data?.length === 0 ? <Card className="p-12 text-center text-gray-500">No events yet. Create your first event to get started.</Card> : null}

      <div className="space-y-3">
        {data?.map((eventItem) => (
          <Card key={eventItem.id} className="p-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <Link to={`/admin/events/${eventItem.id}`} className="font-semibold text-gray-900 transition-colors hover:text-primary-600">
                    {eventItem.name}
                  </Link>
                  <Badge label={eventItem.isActive ? 'Active' : 'Inactive'} color={eventItem.isActive ? 'green' : 'red'} />
                  <Badge label={eventItem.eventType} color="blue" />
                </div>
                <p className="text-sm text-gray-500">
                  {formatDate(eventItem.eventDate)} · {eventItem.clientName ?? 'No client'} · {eventItem.photoCount} photos · {eventItem.totalSize}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Link to={`/gallery/${eventItem.id}`} target="_blank">
                  <Button variant="ghost" size="sm">
                    <Eye className="h-4 w-4" />
                  </Button>
                </Link>
                <a href={eventsApi.getQrCodeUrl(eventItem.id)} target="_blank" rel="noreferrer">
                  <Button variant="ghost" size="sm">
                    <QrCode className="h-4 w-4" />
                  </Button>
                </a>
                <Button variant="ghost" size="sm" onClick={() => toggleMutation.mutate({ id: eventItem.id, activate: !eventItem.isActive })}>
                  <Power className="h-4 w-4" />
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  onClick={() => {
                    if (window.confirm('Delete this event?')) {
                      deleteMutation.mutate(eventItem.id);
                    }
                  }}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
