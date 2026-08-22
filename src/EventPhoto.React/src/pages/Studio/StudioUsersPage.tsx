import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, UserCheck, UserX, KeyRound, Shield, HardDrive, Trash2, AlertTriangle, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import { apiClient } from '../../api/client';
import { systemApi } from '../../api/system';
import type { CacheStats } from '../../api/system';
import type { ApiResponse } from '../../types';
import { CreateUserDialog } from '../../components/Studio/CreateUserDialog';
import { EditUserDialog } from '../../components/Studio/EditUserDialog';
import { ResetPasswordDialog } from '../../components/Studio/ResetPasswordDialog';
import { useConfirm } from '../../hooks/useConfirm';
import { useEvents } from '../../hooks/useEvents';

export interface StudioUser {
  id: string;
  fullName: string;
  username: string;
  email: string;
  phone?: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

function roleBadge(role: string) {
  const map: Record<string, string> = {
    StudioOwner:   'bg-amber-500/20 text-amber-400 ring-amber-500/30',
    StudioManager: 'bg-blue-500/20 text-blue-400 ring-blue-500/30',
    Operator:      'bg-emerald-500/20 text-emerald-400 ring-emerald-500/30',
  };
  return map[role] ?? 'bg-gray-500/20 text-gray-400 ring-gray-500/30';
}

export default function StudioUsersPage() {
  const qc = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editUser, setEditUser] = useState<StudioUser | null>(null);
  const [resetUser, setResetUser] = useState<StudioUser | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['studio-users'],
    queryFn: () => apiClient.get<ApiResponse<StudioUser[]>>('/studio/users').then(r => r.data.data ?? []),
  });

  const toggleActive = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      apiClient.patch(`/studio/users/${id}/${activate ? 'activate' : 'deactivate'}`),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['studio-users'] }); toast.success('User updated.'); },
    onError: () => toast.error('Failed to update user.'),
  });

  const users = data ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">Studio Users</h1>
          <p className="mt-1 text-sm text-gray-400">Manage staff accounts and role assignments.</p>
        </div>
        <button
          onClick={() => setCreateOpen(true)}
          className="flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-500 transition-colors"
        >
          <Plus className="h-4 w-4" /> Add User
        </button>
      </div>

      {/* Table */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-gray-500 text-sm">Loading users…</div>
        ) : users.length === 0 ? (
          <div className="p-8 text-center text-gray-500 text-sm">No studio users found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 text-left text-xs text-gray-500 uppercase tracking-wider">
                <th className="px-5 py-3">Name</th>
                <th className="px-5 py-3">Username</th>
                <th className="px-5 py-3">Role</th>
                <th className="px-5 py-3">Status</th>
                <th className="px-5 py-3">Last Login</th>
                <th className="px-5 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-800">
              {users.map(user => (
                <tr key={user.id} className="hover:bg-gray-800/40 transition-colors">
                  <td className="px-5 py-3.5">
                    <div>
                      <p className="font-medium text-gray-200">{user.fullName || user.username}</p>
                      <p className="text-xs text-gray-500">{user.email}</p>
                    </div>
                  </td>
                  <td className="px-5 py-3.5 text-gray-400 font-mono text-xs">@{user.username}</td>
                  <td className="px-5 py-3.5">
                    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ${roleBadge(user.role)}`}>
                      <Shield className="h-3 w-3" /> {user.role.replace(/([A-Z])/g, ' $1').trim()}
                    </span>
                  </td>
                  <td className="px-5 py-3.5">
                    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${user.isActive ? 'bg-emerald-500/10 text-emerald-400' : 'bg-red-500/10 text-red-400'}`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-3.5 text-gray-500 text-xs">
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}
                  </td>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center justify-end gap-1.5">
                      <button title="Edit" onClick={() => setEditUser(user)}
                        className="rounded-md p-1.5 text-gray-500 hover:bg-gray-700 hover:text-gray-300 transition-colors">
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      <button title="Reset password" onClick={() => setResetUser(user)}
                        className="rounded-md p-1.5 text-gray-500 hover:bg-gray-700 hover:text-gray-300 transition-colors">
                        <KeyRound className="h-3.5 w-3.5" />
                      </button>
                      <button
                        title={user.isActive ? 'Deactivate' : 'Activate'}
                        onClick={() => toggleActive.mutate({ id: user.id, activate: !user.isActive })}
                        className={`rounded-md p-1.5 transition-colors ${user.isActive ? 'text-gray-500 hover:bg-red-900/30 hover:text-red-400' : 'text-gray-500 hover:bg-emerald-900/30 hover:text-emerald-400'}`}>
                        {user.isActive ? <UserX className="h-3.5 w-3.5" /> : <UserCheck className="h-3.5 w-3.5" />}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Cache Management */}
      <CacheManagementCard />

      {createOpen && <CreateUserDialog onClose={() => setCreateOpen(false)} />}
      {editUser   && <EditUserDialog user={editUser} onClose={() => setEditUser(null)} />}
      {resetUser  && <ResetPasswordDialog user={resetUser} onClose={() => setResetUser(null)} />}
    </div>
  );
}

// ── Cache Management Card ─────────────────────────────────────────────────────

function CacheManagementCard() {
  const qc = useQueryClient();
  const confirm = useConfirm();
  const { data: events = [] } = useEvents();
  const [selectedEventId, setSelectedEventId] = useState<string>('');

  const { data: stats, isLoading, refetch } = useQuery<CacheStats>({
    queryKey: ['cache-stats'],
    queryFn: () => systemApi.getCacheStats(),
    staleTime: 30_000,
  });

  const clearAll = useMutation({
    mutationFn: () => systemApi.clearAllCache(),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['cache-stats'] });
      toast.success('Watermark cache cleared.');
    },
    onError: () => toast.error('Failed to clear cache. Check server logs.'),
  });

  const clearEvent = useMutation({
    mutationFn: (eventId: string) => systemApi.clearEventCache(eventId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['cache-stats'] });
      setSelectedEventId('');
      toast.success('Event cache cleared.');
    },
    onError: () => toast.error('Failed to clear event cache.'),
  });

  async function handleClearAll() {
    const ok = await confirm({
      title: 'Clear Entire Watermark Cache?',
      message:
        'This will permanently delete all pre-processed watermarked files across every event. ' +
        'Guests may experience slower download speeds until the cache rebuilds automatically on next download.',
      confirmLabel: 'Yes, Clear All',
      variant: 'danger',
    });
    if (ok) clearAll.mutate();
  }

  async function handleClearEvent() {
    if (!selectedEventId) return;
    const event = events.find(e => e.id === selectedEventId);
    const ok = await confirm({
      title: 'Clear Event Cache?',
      message: `Cached watermarked files for "${event?.name ?? 'this event'}" will be deleted. ` +
        'They will regenerate automatically on next download.',
      confirmLabel: 'Clear Cache',
      variant: 'warning',
    });
    if (ok) clearEvent.mutate(selectedEventId);
  }

  const usagePercent = stats
    ? Math.min(100, Math.round((stats.totalSizeBytes / stats.maxSizeBytes) * 100))
    : 0;

  return (
    <div className="rounded-xl border border-gray-800 bg-gray-900 p-6 space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-500/10">
            <HardDrive className="h-4 w-4 text-indigo-400" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-gray-100">Watermark Cache</h2>
            <p className="text-xs text-gray-500">Pre-processed files that speed up repeat downloads</p>
          </div>
        </div>
        <button
          onClick={() => void refetch()}
          className="rounded-md p-1.5 text-gray-500 hover:bg-gray-800 hover:text-gray-300 transition-colors"
          title="Refresh stats"
        >
          <RefreshCw className="h-3.5 w-3.5" />
        </button>
      </div>

      {/* Warning banner */}
      <div className="flex items-start gap-2 rounded-lg border border-amber-800/50 bg-amber-900/20 px-3 py-2.5 text-xs text-amber-300">
        <AlertTriangle className="h-3.5 w-3.5 mt-0.5 shrink-0" />
        <span>
          Clearing the cache does not delete original photos. Watermarks regenerate automatically
          on the next download. Clearing during a live event may temporarily slow guest downloads.
        </span>
      </div>

      {/* Stats */}
      {isLoading ? (
        <div className="text-xs text-gray-500 animate-pulse">Loading cache statistics…</div>
      ) : stats ? (
        <div className="space-y-3">
          <div className="flex items-center justify-between text-xs">
            <span className="text-gray-400">
              {stats.totalSizeFormatted} used &nbsp;·&nbsp; {stats.totalFileCount} files
            </span>
            <span className="text-gray-500">{usagePercent}% of {stats.maxSizeFormatted}</span>
          </div>
          {/* Usage bar */}
          <div className="h-1.5 w-full rounded-full bg-gray-800 overflow-hidden">
            <div
              className={`h-full rounded-full transition-all duration-500 ${
                usagePercent > 85 ? 'bg-red-500' : usagePercent > 60 ? 'bg-amber-500' : 'bg-indigo-500'
              }`}
              style={{ width: `${usagePercent}%` }}
            />
          </div>
          <p className="text-xs text-gray-600 truncate" title={stats.cacheDirectory}>
            📁 {stats.cacheDirectory}
          </p>
        </div>
      ) : (
        <div className="text-xs text-gray-500">Cache statistics unavailable.</div>
      )}

      <hr className="border-gray-800" />

      {/* Clear by event */}
      <div className="space-y-2">
        <label className="block text-xs font-medium text-gray-400">Clear cache for a specific event</label>
        <div className="flex gap-2">
          <select
            value={selectedEventId}
            onChange={e => setSelectedEventId(e.target.value)}
            className="flex-1 rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 text-sm text-gray-100 focus:border-indigo-500 focus:outline-none"
          >
            <option value="">Select an event…</option>
            {events.map(event => {
              const eventStat = stats?.events.find(s => s.eventId === event.id);
              return (
                <option key={event.id} value={event.id}>
                  {event.name}
                  {eventStat ? ` — ${eventStat.sizeFormatted} (${eventStat.fileCount} files)` : ''}
                </option>
              );
            })}
          </select>
          <button
            onClick={handleClearEvent}
            disabled={!selectedEventId || clearEvent.isPending}
            className="flex items-center gap-1.5 rounded-lg border border-amber-700 bg-amber-900/20 px-3 py-2 text-xs font-medium text-amber-300 hover:bg-amber-900/40 disabled:cursor-not-allowed disabled:opacity-40 transition-colors"
          >
            <Trash2 className="h-3.5 w-3.5" />
            {clearEvent.isPending ? 'Clearing…' : 'Clear'}
          </button>
        </div>
      </div>

      {/* Clear all */}
      <div className="flex items-center justify-between rounded-lg border border-red-900/40 bg-red-950/20 px-4 py-3">
        <div>
          <p className="text-xs font-medium text-red-300">Clear Entire Cache</p>
          <p className="text-xs text-gray-500 mt-0.5">Removes all pre-processed files across all events</p>
        </div>
        <button
          onClick={handleClearAll}
          disabled={clearAll.isPending || (stats?.totalFileCount === 0)}
          className="flex items-center gap-1.5 rounded-lg bg-red-700/30 border border-red-700/50 px-3 py-1.5 text-xs font-medium text-red-300 hover:bg-red-700/50 disabled:cursor-not-allowed disabled:opacity-40 transition-colors"
        >
          <Trash2 className="h-3.5 w-3.5" />
          {clearAll.isPending ? 'Clearing…' : 'Clear All'}
        </button>
      </div>
    </div>
  );
}
