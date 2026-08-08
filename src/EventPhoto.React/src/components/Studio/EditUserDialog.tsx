import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import { apiClient } from '../../api/client';
import type { StudioUser } from '../../pages/Studio/StudioUsersPage';

const ROLES = ['StudioOwner', 'StudioManager', 'Operator'];

interface Props { user: StudioUser; onClose: () => void; }

export function EditUserDialog({ user, onClose }: Props) {
  const qc = useQueryClient();
  const [form, setForm] = useState({ fullName: user.fullName, email: user.email, phone: user.phone ?? '', role: user.role });
  const [error, setError] = useState('');

  const update = useMutation({
    mutationFn: (data: typeof form) => apiClient.put(`/studio/users/${user.id}`, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['studio-users'] }); toast.success('User updated.'); onClose(); },
    onError: (e: any) => setError(e?.response?.data?.error ?? 'Failed to update user.'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-md rounded-2xl border border-gray-700 bg-gray-900 shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-800 px-6 py-4">
          <h2 className="text-base font-semibold text-gray-100">Edit User — @{user.username}</h2>
          <button onClick={onClose}><X className="h-4 w-4 text-gray-500 hover:text-gray-300" /></button>
        </div>
        <form onSubmit={e => { e.preventDefault(); setError(''); update.mutate(form); }} className="p-6 space-y-4">
          {error && <p className="rounded-lg bg-red-900/30 border border-red-800 px-3 py-2 text-xs text-red-400">{error}</p>}
          {[
            { label: 'Full Name', key: 'fullName', type: 'text', required: true },
            { label: 'Email', key: 'email', type: 'email', required: true },
            { label: 'Phone (optional)', key: 'phone', type: 'tel', required: false },
          ].map(f => (
            <div key={f.key}>
              <label className="block text-xs font-medium text-gray-400 mb-1">{f.label}</label>
              <input type={f.type} required={f.required} value={(form as any)[f.key]}
                onChange={e => setForm(p => ({ ...p, [f.key]: e.target.value }))}
                className="w-full rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 text-sm text-gray-100 focus:border-primary-500 focus:outline-none" />
            </div>
          ))}
          <div>
            <label className="block text-xs font-medium text-gray-400 mb-1">Role</label>
            <select value={form.role} onChange={e => setForm(p => ({ ...p, role: e.target.value }))}
              className="w-full rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 text-sm text-gray-100 focus:border-primary-500 focus:outline-none">
              {ROLES.map(r => <option key={r} value={r}>{r.replace(/([A-Z])/g, ' $1').trim()}</option>)}
            </select>
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 rounded-lg border border-gray-700 px-4 py-2 text-sm text-gray-400 hover:text-gray-200">Cancel</button>
            <button type="submit" disabled={update.isPending} className="flex-1 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-500 disabled:opacity-50">
              {update.isPending ? 'Saving…' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
