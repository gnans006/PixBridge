import { useState, useRef, useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { KeyRound, X, Eye, EyeOff, Loader2, CheckCircle2 } from 'lucide-react';
import toast from 'react-hot-toast';
import { authApi } from '../../api/auth';

interface Props {
  onClose: () => void;
}

export function ChangePasswordModal({ onClose }: Props) {
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNext, setShowNext] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const firstRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    firstRef.current?.focus();
    const esc = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', esc);
    return () => document.removeEventListener('keydown', esc);
  }, [onClose]);

  const mutation = useMutation({
    mutationFn: () => authApi.changePassword({
      currentPassword: current,
      newPassword: next,
      confirmNewPassword: confirm,
    }),
    onSuccess: () => {
      toast.success('Password changed successfully.');
      onClose();
    },
    onError: (err: { message?: string }) => {
      setError(err?.message ?? 'Failed to change password. Check your current password.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!current || !next || !confirm) {
      setError('All fields are required.');
      return;
    }
    if (next.length < 6) {
      setError('New password must be at least 6 characters.');
      return;
    }
    if (next !== confirm) {
      setError('Passwords do not match.');
      return;
    }
    mutation.mutate();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="relative z-10 w-full max-w-sm mx-4 rounded-2xl border border-pds-border bg-pds-elevated shadow-pds-modal">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-pds-border px-5 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-pds-primary/15">
              <KeyRound className="h-4 w-4 text-pds-primary" />
            </div>
            <span className="text-sm font-semibold text-pds-text">Change Password</span>
          </div>
          <button
            onClick={onClose}
            className="flex h-7 w-7 items-center justify-center rounded-lg text-pds-text-muted hover:bg-pds-card hover:text-pds-text transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-5 space-y-4">
          {/* Current password */}
          <div>
            <label className="mb-1.5 block text-xs font-medium text-pds-text-2">Current Password</label>
            <div className="relative">
              <input
                ref={firstRef}
                type={showCurrent ? 'text' : 'password'}
                value={current}
                onChange={e => setCurrent(e.target.value)}
                placeholder="Enter current password"
                className="w-full rounded-xl border border-pds-border bg-pds-card px-3 py-2.5 pr-10 text-sm text-pds-text placeholder-pds-text-muted focus:border-pds-primary focus:outline-none transition-colors"
              />
              <button
                type="button"
                tabIndex={-1}
                onClick={() => setShowCurrent(v => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-pds-text-muted hover:text-pds-text-2"
              >
                {showCurrent ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>

          {/* New password */}
          <div>
            <label className="mb-1.5 block text-xs font-medium text-pds-text-2">New Password</label>
            <div className="relative">
              <input
                type={showNext ? 'text' : 'password'}
                value={next}
                onChange={e => setNext(e.target.value)}
                placeholder="Min. 6 characters"
                className="w-full rounded-xl border border-pds-border bg-pds-card px-3 py-2.5 pr-10 text-sm text-pds-text placeholder-pds-text-muted focus:border-pds-primary focus:outline-none transition-colors"
              />
              <button
                type="button"
                tabIndex={-1}
                onClick={() => setShowNext(v => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-pds-text-muted hover:text-pds-text-2"
              >
                {showNext ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>

          {/* Confirm password */}
          <div>
            <label className="mb-1.5 block text-xs font-medium text-pds-text-2">Confirm New Password</label>
            <div className="relative">
              <input
                type="password"
                value={confirm}
                onChange={e => setConfirm(e.target.value)}
                placeholder="Repeat new password"
                className="w-full rounded-xl border border-pds-border bg-pds-card px-3 py-2.5 pr-9 text-sm text-pds-text placeholder-pds-text-muted focus:border-pds-primary focus:outline-none transition-colors"
              />
              {confirm && next && (
                <CheckCircle2
                  className={`absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 transition-colors ${confirm === next ? 'text-pds-success' : 'text-pds-danger'}`}
                />
              )}
            </div>
          </div>

          {error && (
            <p className="rounded-xl border border-pds-danger/30 bg-pds-danger/10 px-3 py-2 text-xs text-pds-danger">
              {error}
            </p>
          )}

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-xl border border-pds-border bg-pds-card py-2.5 text-sm font-medium text-pds-text-2 hover:bg-pds-elevated hover:text-pds-text transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-pds-primary py-2.5 text-sm font-medium text-white hover:bg-pds-primary-hov disabled:opacity-50 transition-colors"
            >
              {mutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {mutation.isPending ? 'Saving…' : 'Update Password'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
