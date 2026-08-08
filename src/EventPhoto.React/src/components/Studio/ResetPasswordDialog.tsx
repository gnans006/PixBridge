import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import { apiClient } from '../../api/client';
import type { StudioUser } from '../../pages/Studio/StudioUsersPage';

interface Props { user: StudioUser; onClose: () => void; }

export function ResetPasswordDialog({ user, onClose }: Props) {
  const [form, setForm] = useState({ newPassword: '', confirmPassword: '' });
  const [error, setError] = useState('');

  const reset = useMutation({
    mutationFn: (data: typeof form) => apiClient.post(`/studio/users/${user.id}/reset-password`, data),
    onSuccess: () => { toast.success('Password reset successfully.'); onClose(); },
    onError: (e: any) => setError(e?.response?.data?.error ?? 'Failed to reset password.'),
  });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (form.newPassword !== form.confirmPassword) { setError('Passwords do not match.'); return; }
    setError('');
    reset.mutate(form);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-sm rounded-2xl border border-gray-700 bg-gray-900 shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-800 px-6 py-4">
          <h2 className="text-base font-semibold text-gray-100">Reset Password — @{user.username}</h2>
          <button onClick={onClose}><X className="h-4 w-4 text-gray-500 hover:text-gray-300" /></button>
        </div>
        <form onSubmit={submit} className="p-6 space-y-4">
          {error && <p className="rounded-lg bg-red-900/30 border border-red-800 px-3 py-2 text-xs text-red-400">{error}</p>}
          <div>
            <label className="block text-xs font-medium text-gray-400 mb-1">New Password</label>
            <input type="password" required value={form.newPassword} onChange={e => setForm(p => ({ ...p, newPassword: e.target.value }))}
              className="w-full rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 text-sm text-gray-100 focus:border-primary-500 focus:outline-none" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-400 mb-1">Confirm Password</label>
            <input type="password" required value={form.confirmPassword} onChange={e => setForm(p => ({ ...p, confirmPassword: e.target.value }))}
              className="w-full rounded-lg border border-gray-700 bg-gray-800 px-3 py-2 text-sm text-gray-100 focus:border-primary-500 focus:outline-none" />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 rounded-lg border border-gray-700 px-4 py-2 text-sm text-gray-400 hover:text-gray-200">Cancel</button>
            <button type="submit" disabled={reset.isPending} className="flex-1 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-500 disabled:opacity-50">
              {reset.isPending ? 'Resetting…' : 'Reset Password'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
