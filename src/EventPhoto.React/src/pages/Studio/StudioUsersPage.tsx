import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, UserCheck, UserX, KeyRound, Shield } from 'lucide-react';
import toast from 'react-hot-toast';
import { apiClient } from '../../api/client';
import type { ApiResponse } from '../../types';
import { CreateUserDialog } from '../../components/Studio/CreateUserDialog';
import { EditUserDialog } from '../../components/Studio/EditUserDialog';
import { ResetPasswordDialog } from '../../components/Studio/ResetPasswordDialog';

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

      {createOpen && <CreateUserDialog onClose={() => setCreateOpen(false)} />}
      {editUser   && <EditUserDialog user={editUser} onClose={() => setEditUser(null)} />}
      {resetUser  && <ResetPasswordDialog user={resetUser} onClose={() => setResetUser(null)} />}
    </div>
  );
}
