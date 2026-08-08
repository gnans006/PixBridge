import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Sparkles, Save, Loader2, Monitor, Sun, Moon } from 'lucide-react';
import toast from 'react-hot-toast';
import { applicationSettingsApi, type UpdateBrandingRequest } from '../../api/applicationSettings';

const THEMES = [
  { value: 'dark',   label: 'Dark',   icon: Moon  },
  { value: 'light',  label: 'Light',  icon: Sun   },
  { value: 'system', label: 'System', icon: Monitor },
] as const;

function ColorSwatch({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  const isValid = /^#[0-9a-fA-F]{3,6}$/.test(value);
  return (
    <div>
      <label className="block text-xs font-medium text-gray-400 mb-1.5">{label}</label>
      <div className="flex items-center gap-3">
        {/* native color picker */}
        <input
          type="color"
          value={isValid ? value : '#6366f1'}
          onChange={e => onChange(e.target.value)}
          className="h-10 w-14 cursor-pointer rounded-lg border border-gray-700 bg-transparent p-0.5"
        />
        {/* hex text input */}
        <input
          type="text"
          value={value}
          onChange={e => onChange(e.target.value)}
          maxLength={7}
          placeholder="#6366f1"
          className={`flex-1 rounded-lg border px-3 py-2.5 text-sm font-mono bg-gray-800 focus:outline-none transition-colors ${
            isValid ? 'border-gray-700 text-gray-100 focus:border-primary-500' : 'border-red-600 text-red-400'
          }`}
        />
        {/* preview chip */}
        <div
          className="h-9 w-9 flex-none rounded-lg border border-gray-700"
          style={{ backgroundColor: isValid ? value : 'transparent' }}
        />
      </div>
    </div>
  );
}

export default function BrandingPage() {
  const qc = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['application-settings'],
    queryFn: applicationSettingsApi.get,
  });

  const [form, setForm] = useState<UpdateBrandingRequest>({
    primaryColor:   '#6366f1',
    secondaryColor: '#8b5cf6',
    brandTheme:     'dark',
    defaultWatermarkProfileId: undefined,
  });

  useEffect(() => {
    if (settings) {
      setForm({
        primaryColor:             settings.primaryColor   || '#6366f1',
        secondaryColor:           settings.secondaryColor || '#8b5cf6',
        brandTheme:               settings.brandTheme     || 'dark',
        defaultWatermarkProfileId: settings.defaultWatermarkProfileId ?? undefined,
      });
    }
  }, [settings]);

  const save = useMutation({
    mutationFn: () => applicationSettingsApi.updateBranding(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['application-settings'] });
      toast.success('Branding settings saved.');
    },
    onError: () => toast.error('Failed to save branding settings.'),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <Loader2 className="h-6 w-6 animate-spin text-gray-500" />
      </div>
    );
  }

  const primaryValid   = /^#[0-9a-fA-F]{3,6}$/.test(form.primaryColor);
  const secondaryValid = /^#[0-9a-fA-F]{3,6}$/.test(form.secondaryColor);
  const canSave = primaryValid && secondaryValid && !save.isPending;

  return (
    <div className="space-y-6 max-w-2xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Sparkles className="h-6 w-6 text-primary-400" />
          <div>
            <h1 className="text-2xl font-bold text-gray-100">Branding Center</h1>
            <p className="mt-0.5 text-sm text-gray-400">
              Define your studio's visual identity across all guest touchpoints.
            </p>
          </div>
        </div>
        <button
          onClick={() => save.mutate()}
          disabled={!canSave}
          className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-500 disabled:opacity-50 transition-colors"
        >
          {save.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          {save.isPending ? 'Saving…' : 'Save Branding'}
        </button>
      </div>

      {/* Colors */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-5">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">Brand Colors</h2>
        <ColorSwatch
          label="Primary Color"
          value={form.primaryColor}
          onChange={v => setForm(p => ({ ...p, primaryColor: v }))}
        />
        <ColorSwatch
          label="Secondary Color"
          value={form.secondaryColor}
          onChange={v => setForm(p => ({ ...p, secondaryColor: v }))}
        />

        {/* Live preview */}
        <div className="rounded-lg border border-gray-700 overflow-hidden">
          <div className="px-4 py-2 text-xs text-gray-500 border-b border-gray-700 bg-gray-800/50">Preview</div>
          <div className="flex items-center gap-3 p-4 bg-gray-950">
            <div className="h-8 w-8 rounded-full" style={{ backgroundColor: form.primaryColor }} />
            <div className="flex-1 space-y-1.5">
              <div className="h-2.5 rounded-full w-32" style={{ backgroundColor: form.primaryColor }} />
              <div className="h-2 rounded-full w-20 opacity-60" style={{ backgroundColor: form.secondaryColor }} />
            </div>
            <div className="h-8 px-3 rounded-md flex items-center text-xs font-medium text-white" style={{ backgroundColor: form.primaryColor }}>
              Button
            </div>
          </div>
        </div>
      </div>

      {/* Theme */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">UI Theme</h2>
        <div className="grid grid-cols-3 gap-3">
          {THEMES.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              onClick={() => setForm(p => ({ ...p, brandTheme: value }))}
              className={`flex flex-col items-center gap-2 rounded-xl border-2 p-4 transition-colors ${
                form.brandTheme === value
                  ? 'border-primary-500 bg-primary-500/10 text-primary-400'
                  : 'border-gray-700 text-gray-500 hover:border-gray-600 hover:text-gray-400'
              }`}
            >
              <Icon className="h-5 w-5" />
              <span className="text-xs font-medium">{label}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

