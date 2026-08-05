import { useQuery } from '@tanstack/react-query';
import { BarChart2, Camera, Download, Edit, HardDrive, ScanFace, Shield, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';
import { dashboardApi } from '../../api/dashboard';
import { buildApiUrl } from '../../api/client';
import { Spinner } from '../UI/Spinner';

function FeatureBadge({ icon: Icon, label, active }: { icon: typeof Shield; label: string; active: boolean }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium ${active ? 'bg-emerald-500/20 text-emerald-300' : 'bg-slate-700/60 text-slate-500'}`}>
      <Icon className="h-3 w-3" />
      {label}
    </span>
  );
}

function MetricItem({ icon: Icon, label, value }: { icon: typeof Camera; label: string; value: string | number }) {
  return (
    <div className="flex items-center gap-3 rounded-xl bg-slate-800/50 px-4 py-3">
      <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-500/20">
        <Icon className="h-4 w-4 text-indigo-400" />
      </div>
      <div>
        <p className="text-xs text-slate-400">{label}</p>
        <p className="text-sm font-semibold text-white">{value}</p>
      </div>
    </div>
  );
}

export function EventSpotlight() {
  const { data, isLoading } = useQuery({
    queryKey: ['event-spotlight'],
    queryFn: () => dashboardApi.getSpotlight(),
    refetchInterval: 30_000,
    select: (r) => r.data,
  });

  if (isLoading) {
    return (
      <div className="flex h-48 items-center justify-center rounded-2xl bg-slate-900 border border-slate-800">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="flex h-48 flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-slate-700 bg-slate-900">
        <Camera className="h-10 w-10 text-slate-600" />
        <p className="text-sm text-slate-500">No events yet. Create your first event to get started.</p>
        <Link to="/admin/events/new" className="rounded-lg bg-indigo-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-indigo-500">
          Create Event
        </Link>
      </div>
    );
  }

  const thumbnailSrc = data.firstThumbnailUrl
    ? buildApiUrl(data.firstThumbnailUrl)
    : null;

  const typeGradient: Record<string, string> = {
    Wedding: 'from-rose-900 to-pink-950',
    Corporate: 'from-blue-900 to-slate-950',
    Birthday: 'from-amber-900 to-orange-950',
    default: 'from-indigo-900 to-slate-950',
  };
  const gradient = typeGradient[data.eventType] ?? typeGradient.default;

  return (
    <div className="relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900 animate-fade-in">
      {/* Background */}
      <div className="absolute inset-0">
        {thumbnailSrc ? (
          <img src={thumbnailSrc} alt="" className="h-full w-full object-cover opacity-20" />
        ) : (
          <div className={`h-full w-full bg-gradient-to-br ${gradient} opacity-60`} />
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-slate-900/95 via-slate-900/80 to-slate-900/40" />
      </div>

      {/* Header label */}
      <div className="absolute right-4 top-4 flex items-center gap-1.5 rounded-full bg-violet-600/90 px-3 py-1 backdrop-blur-sm">
        <Sparkles className="h-3 w-3 text-white" />
        <span className="text-xs font-semibold text-white">Event Spotlight</span>
      </div>

      <div className="relative p-6 sm:p-8">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <div className="mb-2 flex items-center gap-2">
              <span className={`inline-block h-2 w-2 rounded-full ${data.isActive ? 'bg-emerald-400 animate-pulse' : 'bg-slate-600'}`} />
              <span className="text-xs font-medium text-slate-400">{data.isActive ? 'Active' : 'Inactive'} · {data.eventType}</span>
            </div>
            <h2 className="text-2xl font-bold text-white sm:text-3xl">{data.name}</h2>
            {(data.clientName || data.venueName) && (
              <p className="mt-1 text-sm text-slate-400">
                {[data.clientName, data.venueName].filter(Boolean).join(' · ')}
              </p>
            )}

            <div className="mt-4 flex flex-wrap gap-2">
              <FeatureBadge icon={ScanFace} label="Face Search" active={data.faceRecognitionEnabled} />
              <FeatureBadge icon={Shield} label="Watermark" active={data.watermarkEnabled} />
            </div>
          </div>

          <div className="flex flex-wrap gap-3 lg:flex-col">
            <Link
              to={`/gallery/${data.eventId}`}
              target="_blank"
              className="flex items-center gap-1.5 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-lg transition-all hover:bg-indigo-500 active:scale-95"
            >
              <Camera className="h-4 w-4" /> Open Gallery
            </Link>
            <Link
              to={`/admin/events/${data.eventId}`}
              className="flex items-center gap-1.5 rounded-xl bg-slate-700 px-4 py-2 text-sm font-medium text-slate-200 transition-all hover:bg-slate-600 active:scale-95"
            >
              <Edit className="h-4 w-4" /> Edit Event
            </Link>
            <Link
              to="/admin/statistics"
              className="flex items-center gap-1.5 rounded-xl bg-slate-700 px-4 py-2 text-sm font-medium text-slate-200 transition-all hover:bg-slate-600 active:scale-95"
            >
              <BarChart2 className="h-4 w-4" /> Analytics
            </Link>
          </div>
        </div>

        <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <MetricItem icon={Camera} label="Photos" value={data.photoCount.toLocaleString()} />
          <MetricItem icon={Download} label="Downloads" value={data.totalDownloads.toLocaleString()} />
          <MetricItem icon={HardDrive} label="Storage" value={data.storageHuman} />
          <MetricItem icon={ScanFace} label="Face Search" value={data.faceRecognitionEnabled ? 'Enabled' : 'Disabled'} />
        </div>
      </div>
    </div>
  );
}
