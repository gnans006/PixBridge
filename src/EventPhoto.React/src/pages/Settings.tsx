import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { settingsApi } from '../api/settings';
import { Button } from '../components/UI/Button';
import { Card } from '../components/UI/Card';
import { Input } from '../components/UI/Input';
import { Spinner } from '../components/UI/Spinner';

// Per-key validation rules
function validateSetting(key: string, value: string): string | null {
  const trimmed = value.trim();
  if (trimmed === '') return 'Value is required.';
  if (trimmed.length > 2000) return 'Value must not exceed 2000 characters.';

  if (key === 'download.rateLimit') {
    const n = Number(trimmed);
    if (!Number.isInteger(n) || n < 1 || n > 1000) return 'Must be a whole number between 1 and 1000.';
  }

  if (key === 'app.serverUrl') {
    try {
      const url = new URL(trimmed);
      if (!['http:', 'https:'].includes(url.protocol)) return 'Must be an http:// or https:// URL.';
    } catch {
      return 'Must be a valid URL (e.g. http://192.168.1.5:5173).';
    }
  }

  return null;
}

export default function Settings() {
  const queryClient = useQueryClient();
  const [edits, setEdits] = useState<Record<string, string>>({});
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

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

  const handleSave = (key: string, currentValue: string) => {
    const value = edits[key] ?? currentValue;
    const error = validateSetting(key, value);
    if (error) {
      setValidationErrors(prev => ({ ...prev, [key]: error }));
      return;
    }
    setValidationErrors(prev => { const next = { ...prev }; delete next[key]; return next; });
    mutation.mutate({ key, value: value.trim() });
  };

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
                error={validationErrors[setting.key]}
                onChange={(event) => {
                  const val = event.target.value;
                  setEdits(current => ({ ...current, [setting.key]: val }));
                  // Clear error on change
                  if (validationErrors[setting.key]) {
                    setValidationErrors(prev => { const next = { ...prev }; delete next[setting.key]; return next; });
                  }
                }}
              />
              {setting.description ? <p className="mt-1 text-xs text-gray-400">{setting.description}</p> : null}
            </div>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => handleSave(setting.key, setting.value)}
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
