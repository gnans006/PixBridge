import { useQuery } from '@tanstack/react-query';
import { workspaceApi } from '../../api/workspace';
import type { EventWorkspaceResponse } from '../../types';
import { FolderOpen, HardDrive, Image, Info, Loader2 } from 'lucide-react';

interface EventStorageTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventStorageTab({ workspace }: EventStorageTabProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['storage', workspace.id],
    queryFn: () => workspaceApi.getStorage(workspace.id),
    refetchInterval: 30_000,
  });

  const storage = data?.data;

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
      </div>
    );
  }

  if (isError || !storage) {
    return (
      <div className="flex h-40 items-center justify-center rounded-2xl border border-slate-800 bg-slate-900">
        <p className="text-sm text-slate-500">Failed to load storage metrics.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Metrics grid */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <MetricCard
          icon={<HardDrive className="h-5 w-5" />}
          label="Total Storage"
          value={storage.sizeHuman}
          sub={`${storage.sizeBytes.toLocaleString()} bytes`}
          color="indigo"
        />
        <MetricCard
          icon={<Image className="h-5 w-5" />}
          label="Photos"
          value={storage.photoCount.toLocaleString()}
          sub="Original files"
          color="violet"
        />
        <MetricCard
          icon={<Image className="h-5 w-5" />}
          label="Thumbnails"
          value={storage.thumbnailCount.toLocaleString()}
          sub="Generated thumbnails"
          color="emerald"
        />
      </div>

      {/* Folder paths */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-2">
          <FolderOpen className="h-4 w-4 text-slate-400" />
          <h2 className="text-sm font-semibold text-white">Storage Paths</h2>
          <span className="ml-auto rounded-full bg-slate-800 px-2.5 py-0.5 text-xs text-slate-500">Read only</span>
        </div>

        <div className="space-y-4">
          <PathRow label="Watch Folder (Photos)" path={storage.watchFolder} />
          <PathRow label="Thumbnail Folder" path={storage.thumbnailFolder} />
        </div>
      </div>

      {/* Move storage wizard notice */}
      <div className="rounded-2xl border border-amber-500/20 bg-amber-500/5 p-5">
        <div className="flex items-start gap-3">
          <Info className="mt-0.5 h-4 w-4 shrink-0 text-amber-400" />
          <div>
            <p className="text-sm font-semibold text-amber-300">Need to change storage location?</p>
            <p className="mt-1 text-xs text-amber-500/80">
              Storage paths cannot be edited directly. To move this event's photos to a different
              location, use the <strong>Move Storage Wizard</strong> from the event actions menu.
              The wizard ensures all photos and thumbnails are safely transferred without data loss.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function MetricCard({
  icon,
  label,
  value,
  sub,
  color,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub: string;
  color: 'indigo' | 'violet' | 'emerald';
}) {
  const colorMap = {
    indigo: 'text-indigo-400 bg-indigo-500/10',
    violet: 'text-violet-400 bg-violet-500/10',
    emerald: 'text-emerald-400 bg-emerald-500/10',
  };
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <div className={`mb-3 inline-flex rounded-xl p-2.5 ${colorMap[color]}`}>
        <span className={colorMap[color].split(' ')[0]}>{icon}</span>
      </div>
      <p className="text-xl font-bold text-white">{value}</p>
      <p className="mt-0.5 text-xs font-medium text-slate-300">{label}</p>
      <p className="mt-0.5 text-xs text-slate-500">{sub}</p>
    </div>
  );
}

function PathRow({ label, path }: { label: string; path: string }) {
  return (
    <div className="space-y-1.5">
      <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">{label}</p>
      <div className="rounded-xl border border-slate-700/50 bg-slate-800/50 px-4 py-3">
        <p className="break-all font-mono text-xs text-slate-300">{path}</p>
      </div>
    </div>
  );
}
