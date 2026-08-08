import { Network, Loader2 } from 'lucide-react';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';
import { NetworkTab } from '../../components/Settings/SystemSettings/NetworkTab';

export default function NetworkPage() {
  const { data: settings, isLoading, isError } = useApplicationSettings();

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
      </div>
    );
  }

  if (isError || !settings) {
    return (
      <div className="rounded-2xl border border-red-500/20 bg-red-500/5 p-6 text-center">
        <p className="text-sm text-red-400">Failed to load network settings.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Network className="h-6 w-6 text-primary-400" />
        <div>
          <h1 className="text-2xl font-bold text-gray-100">Network</h1>
          <p className="mt-0.5 text-sm text-gray-400">
            Server IP addresses, public URL configuration, and connectivity testing.
          </p>
        </div>
      </div>

      <NetworkTab settings={settings} />
    </div>
  );
}
