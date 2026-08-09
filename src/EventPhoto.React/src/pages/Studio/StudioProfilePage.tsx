import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Building2, Globe, Phone, Mail, MapPin, AtSign, Share2, MessageCircle, Save, Loader2 } from 'lucide-react';
import toast from 'react-hot-toast';
import { applicationSettingsApi, type UpdateStudioProfileRequest } from '../../api/applicationSettings';
import { useUpdateApplicationSettings } from '../../hooks/useApplicationSettings';

function Field({
  label, icon: Icon, value, onChange, type = 'text', placeholder,
}: {
  label: string; icon: React.ElementType; value: string; onChange: (v: string) => void;
  type?: string; placeholder?: string;
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-400 mb-1.5">{label}</label>
      <div className="relative">
        <Icon className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-500" />
        <input
          type={type}
          value={value}
          onChange={e => onChange(e.target.value)}
          placeholder={placeholder}
          className="w-full rounded-lg border border-gray-700 bg-gray-800 pl-9 pr-3 py-2.5 text-sm text-gray-100 placeholder-gray-600 focus:border-primary-500 focus:outline-none transition-colors"
        />
      </div>
    </div>
  );
}

export default function StudioProfilePage() {
  const qc = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['application-settings'],
    queryFn: applicationSettingsApi.get,
  });

  const [studioName, setStudioName] = useState('');
  const [form, setForm] = useState<UpdateStudioProfileRequest>({
    phone: '', email: '', website: '', address: '',
    instagram: '', facebook: '', whatsApp: '', logoPath: '',
  });

  useEffect(() => {
    if (settings) {
      setStudioName(settings.studioName ?? '');
      setForm({
        phone:     settings.phone     ?? '',
        email:     settings.email     ?? '',
        website:   settings.website   ?? '',
        address:   settings.address   ?? '',
        instagram: settings.instagram ?? '',
        facebook:  settings.facebook  ?? '',
        whatsApp:  settings.whatsApp  ?? '',
        logoPath:  settings.logoPath  ?? '',
      });
    }
  }, [settings]);

  const f = (key: keyof UpdateStudioProfileRequest) => (v: string) =>
    setForm(p => ({ ...p, [key]: v }));

  const updateGeneral = useUpdateApplicationSettings();

  const save = useMutation({
    mutationFn: async () => {
      // Save studio name if changed
      if (settings && studioName !== settings.studioName) {
        await updateGeneral.mutateAsync({
          studioName,
          serverName: settings.serverName,
          publicBaseUrl: settings.publicBaseUrl,
          serverPort: settings.serverPort,
          defaultEventGalleryMode: settings.defaultEventGalleryMode,
          enableWatermarkByDefault: settings.enableWatermarkByDefault,
          enableFaceRecognitionByDefault: settings.enableFaceRecognitionByDefault,
          isWatermarkEnabled: settings.isWatermarkEnabled,
          isFaceSearchEnabled: settings.isFaceSearchEnabled,
        });
      }
      return applicationSettingsApi.updateStudioProfile({
        phone:     form.phone     || undefined,
        email:     form.email     || undefined,
        website:   form.website   || undefined,
        address:   form.address   || undefined,
        instagram: form.instagram || undefined,
        facebook:  form.facebook  || undefined,
        whatsApp:  form.whatsApp  || undefined,
        logoPath:  form.logoPath  || undefined,
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['application-settings'] });
      toast.success('Studio profile saved.');
    },
    onError: () => toast.error('Failed to save studio profile.'),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <Loader2 className="h-6 w-6 animate-spin text-gray-500" />
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-2xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Building2 className="h-6 w-6 text-primary-400" />
          <div>
            <h1 className="text-2xl font-bold text-gray-100">Studio Profile</h1>
            <p className="mt-0.5 text-sm text-gray-400">
              Public identity and contact details shown to clients.
            </p>
          </div>
        </div>
        <button
          onClick={() => save.mutate()}
          disabled={save.isPending}
          className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-500 disabled:opacity-50 transition-colors"
        >
          {save.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          {save.isPending ? 'Saving…' : 'Save Profile'}
        </button>
      </div>

      {/* Studio Identity */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">Studio Identity</h2>
        <div>
          <label className="block text-xs font-medium text-gray-400 mb-1.5">Studio Name</label>
          <div className="relative">
            <Building2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-500" />
            <input
              type="text"
              value={studioName}
              onChange={e => setStudioName(e.target.value)}
              placeholder="e.g. ABC Digital Studio"
              className="w-full rounded-lg border border-gray-700 bg-gray-800 pl-9 pr-3 py-2.5 text-sm text-gray-100 placeholder-gray-600 focus:border-primary-500 focus:outline-none transition-colors"
            />
          </div>
          <p className="mt-1 text-xs text-gray-500">Shown in the navbar, watermarks, and guest gallery.</p>
        </div>
      </div>

      {/* Contact */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">Contact Information</h2>
        <Field label="Phone Number" icon={Phone} value={form.phone ?? ''} onChange={f('phone')} type="tel" placeholder="+91 98765 43210" />
        <Field label="Email Address" icon={Mail} value={form.email ?? ''} onChange={f('email')} type="email" placeholder="studio@example.com" />
        <Field label="Website" icon={Globe} value={form.website ?? ''} onChange={f('website')} placeholder="https://yourstudio.com" />
        <div>
          <label className="block text-xs font-medium text-gray-400 mb-1.5">Address</label>
          <div className="relative">
            <MapPin className="absolute left-3 top-3 h-4 w-4 text-gray-500" />
            <textarea
              rows={3}
              value={form.address ?? ''}
              onChange={e => f('address')(e.target.value)}
              placeholder="123 Studio Lane, City, State, PIN"
              className="w-full rounded-lg border border-gray-700 bg-gray-800 pl-9 pr-3 py-2.5 text-sm text-gray-100 placeholder-gray-600 focus:border-primary-500 focus:outline-none transition-colors resize-none"
            />
          </div>
        </div>
      </div>

      {/* Social */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-4">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">Social Media</h2>
        <Field label="Instagram" icon={AtSign} value={form.instagram ?? ''} onChange={f('instagram')} placeholder="https://instagram.com/yourstudio" />
        <Field label="Facebook" icon={Share2} value={form.facebook ?? ''} onChange={f('facebook')} placeholder="https://facebook.com/yourstudio" />
        <Field label="WhatsApp" icon={MessageCircle} value={form.whatsApp ?? ''} onChange={f('whatsApp')} type="tel" placeholder="+91 98765 43210" />
      </div>
    </div>
  );
}

