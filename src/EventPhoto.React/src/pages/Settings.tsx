import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { settingsApi } from '../api/settings';
import { Button } from '../components/UI/Button';
import { Card } from '../components/UI/Card';
import { Input } from '../components/UI/Input';
import { Spinner } from '../components/UI/Spinner';

export default function Settings() {
  const queryClient = useQueryClient();
  const [edits, setEdits] = useState<Record<string, string>>({});
  const { data, isLoading } = useQuery({
    queryKey: ['settings'],
    queryFn: async () => {
      const response = await settingsApi.getAll();
      return response.data ?? [];
    },
  });

  const mutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) => settingsApi.update(key, value),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['settings'] });
      toast.success('Setting saved.');
    },
    onError: () => toast.error('Failed to save.'),
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">System Settings</h1>
      <Card className="space-y-4 p-6">
        {data?.map((setting) => (
          <div key={setting.key} className="flex items-end gap-3">
            <div className="flex-1">
              <Input
                label={setting.key}
                defaultValue={setting.value}
                onChange={(event) => setEdits((current) => ({ ...current, [setting.key]: event.target.value }))}
              />
              {setting.description ? <p className="mt-1 text-xs text-gray-400">{setting.description}</p> : null}
            </div>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => mutation.mutate({ key: setting.key, value: edits[setting.key] ?? setting.value })}
              isLoading={mutation.isPending}
            >
              Save
            </Button>
          </div>
        ))}
      </Card>
    </div>
  );
}
