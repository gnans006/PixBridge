import { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Archive,
  ChevronDown,
  Loader2,
  Power,
  PowerOff,
  QrCode,
  ScanFace,
  Trash2,
} from 'lucide-react';
import { eventsApi } from '../../api/events';
import { workspaceApi } from '../../api/workspace';
import { apiError } from '../../utils/errorHandler';
import toast from 'react-hot-toast';
import { Modal } from '../UI/Modal';
import { Button } from '../UI/Button';
import { useEffect } from 'react';

interface EventActionsDropdownProps {
  eventId: string;
  isActive: boolean;
}

export function EventActionsDropdown({ eventId, isActive }: EventActionsDropdownProps) {
  const [open, setOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['workspace', eventId] });
    qc.invalidateQueries({ queryKey: ['events'] });
    qc.invalidateQueries({ queryKey: ['dashboard-overview'] });
  };

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const toggleMutation = useMutation({
    mutationFn: (activate: boolean) => eventsApi.toggleActive(eventId, activate),
    onSuccess: () => { invalidate(); toast.success(isActive ? 'Event deactivated.' : 'Event activated.'); },
    onError: (e) => apiError(e, 'Failed to update event status.'),
  });

  const refreshQrMutation = useMutation({
    mutationFn: () => eventsApi.refreshQr(eventId),
    onSuccess: () => { invalidate(); toast.success('QR code refreshed.'); },
    onError: (e) => apiError(e, 'Failed to refresh QR code.'),
  });

  const rebuildMutation = useMutation({
    mutationFn: () => workspaceApi.rebuildFaceIndex(eventId),
    onSuccess: (r) => {
      invalidate();
      toast.success(`Queued ${r.data ?? 0} photos for face indexing.`);
    },
    onError: (e) => apiError(e, 'Failed to rebuild face index.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => eventsApi.delete(eventId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['events'] });
      qc.invalidateQueries({ queryKey: ['dashboard-overview'] });
      toast.success('Event deleted.');
      navigate('/admin/events');
    },
    onError: (e) => apiError(e, 'Failed to delete event.'),
  });

  const isBusy = toggleMutation.isPending || refreshQrMutation.isPending || rebuildMutation.isPending;

  return (
    <>
      <div ref={menuRef} className="relative">
        <button
          onClick={() => setOpen((v) => !v)}
          className="inline-flex items-center gap-2 rounded-xl bg-white/10 px-3.5 py-2 text-sm font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/20"
        >
          More Actions <ChevronDown className="h-3.5 w-3.5" />
        </button>

        {open && (
          <div className="absolute right-0 z-50 mt-2 w-52 origin-top-right rounded-xl border border-slate-700 bg-slate-800 py-1 shadow-2xl shadow-black/60">
            <MenuItem
              icon={isActive ? <PowerOff className="h-4 w-4" /> : <Power className="h-4 w-4" />}
              label={isActive ? 'Deactivate Event' : 'Activate Event'}
              loading={toggleMutation.isPending}
              disabled={isBusy}
              onClick={() => { toggleMutation.mutate(!isActive); setOpen(false); }}
            />
            <MenuItem
              icon={<QrCode className="h-4 w-4" />}
              label="Refresh QR Code"
              loading={refreshQrMutation.isPending}
              disabled={isBusy}
              onClick={() => { refreshQrMutation.mutate(); setOpen(false); }}
            />
            <MenuItem
              icon={<ScanFace className="h-4 w-4" />}
              label="Rebuild Face Index"
              loading={rebuildMutation.isPending}
              disabled={isBusy}
              onClick={() => { rebuildMutation.mutate(); setOpen(false); }}
            />
            <div className="my-1 border-t border-slate-700" />
            <MenuItem
              icon={<Archive className="h-4 w-4" />}
              label="Archive Event"
              disabled={isBusy}
              onClick={() => { toggleMutation.mutate(false); setOpen(false); }}
            />
            <MenuItem
              icon={<Trash2 className="h-4 w-4" />}
              label="Delete Event"
              danger
              disabled={isBusy}
              onClick={() => { setOpen(false); setConfirmDelete(true); }}
            />
          </div>
        )}
      </div>

      {/* Delete confirmation modal */}
      <Modal
        isOpen={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        title="Delete Event"
      >
        <p className="text-sm text-slate-300">
          This will permanently delete the event and all associated data including photos, downloads,
          and face index entries. This action cannot be undone.
        </p>
        <div className="mt-5 flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setConfirmDelete(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            onClick={() => deleteMutation.mutate()}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? 'Deleting…' : 'Delete Event'}
          </Button>
        </div>
      </Modal>
    </>
  );
}

function MenuItem({
  icon,
  label,
  loading,
  disabled,
  danger,
  onClick,
}: {
  icon: React.ReactNode;
  label: string;
  loading?: boolean;
  disabled?: boolean;
  danger?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={[
        'flex w-full items-center gap-2.5 px-4 py-2 text-sm transition-colors',
        danger
          ? 'text-rose-400 hover:bg-rose-500/10 hover:text-rose-300'
          : 'text-slate-300 hover:bg-slate-700 hover:text-white',
        disabled ? 'cursor-not-allowed opacity-50' : '',
      ].join(' ')}
    >
      {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : icon}
      {label}
    </button>
  );
}
