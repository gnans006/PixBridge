import {
  BarChart2, Edit, ExternalLink, ImageIcon, MoreVertical,
  Power, QrCode, RefreshCw, Trash2,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { eventsApi } from '../../api/events';
import type { EventResponse } from '../../types';
import { useConfirm } from '../../hooks/useConfirm';

interface EventCardMenuProps {
  event: EventResponse;
  onDelete: (id: string) => void;
  onToggleActive: (id: string, activate: boolean) => void;
  onRefreshQr: (id: string) => void;
  refreshingQrId: string | null;
}

export function EventCardMenu({
  event, onDelete, onToggleActive, onRefreshQr, refreshingQrId,
}: EventCardMenuProps) {
  const [open, setOpen] = useState(false);
  const confirm = useConfirm();
  const menuRef = useRef<HTMLDivElement>(null);
  const btnRef  = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (
        menuRef.current?.contains(e.target as Node) ||
        btnRef.current?.contains(e.target as Node)
      ) return;
      setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const toggle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setOpen((o) => !o);
  };
  const close = () => setOpen(false);

  const row = 'flex w-full items-center gap-3 px-4 py-2 text-sm text-slate-300 transition-colors hover:bg-slate-800 hover:text-white';

  return (
    <div className="relative">
      <button
        ref={btnRef}
        type="button"
        onClick={toggle}
        aria-label="More options"
        aria-expanded={open}
        className="flex h-7 w-7 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-700/80 hover:text-slate-200"
      >
        <MoreVertical className="h-4 w-4" />
      </button>

      {open ? (
        <div
          ref={menuRef}
          role="menu"
          className="absolute right-0 top-9 z-50 min-w-[200px] rounded-xl border border-slate-700 bg-slate-900 py-1.5 shadow-2xl shadow-black/60"
        >
          <Link to={`/admin/events/${event.id}`} onClick={close} className={row} role="menuitem">
            <Edit className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            Open Event
          </Link>

          <a
            href={`/gallery/${event.id}`}
            target="_blank"
            rel="noreferrer"
            onClick={close}
            className={row}
            role="menuitem"
          >
            <ImageIcon className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            View Gallery
            <ExternalLink className="ml-auto h-3 w-3 text-slate-600" />
          </a>

          <Link to="/admin/statistics" onClick={close} className={row} role="menuitem">
            <BarChart2 className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            Analytics
          </Link>

          <a
            href={eventsApi.getQrCodeUrl(event.id)}
            target="_blank"
            rel="noreferrer"
            onClick={close}
            className={row}
            role="menuitem"
          >
            <QrCode className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            QR Code
            <ExternalLink className="ml-auto h-3 w-3 text-slate-600" />
          </a>

          <button
            type="button"
            disabled={refreshingQrId === event.id}
            onClick={async () => {
                const ok = await confirm({
                  title: 'Regenerate QR Code?',
                  message: 'The current QR code will stop working immediately. Any printed materials using this QR will need to be reprinted.',
                  confirmLabel: 'Regenerate',
                  variant: 'warning',
                });
                if (!ok) return;
                onRefreshQr(event.id);
                close();
              }}
            className={`${row} disabled:opacity-50`}
            role="menuitem"
          >
            <RefreshCw
              className={`h-3.5 w-3.5 shrink-0 text-slate-400 ${refreshingQrId === event.id ? 'animate-spin' : ''}`}
            />
            Refresh QR
          </button>

          <div className="my-1.5 border-t border-slate-800" />

          <button
            type="button"
            onClick={() => { onToggleActive(event.id, !event.isActive); close(); }}
            className={row}
            role="menuitem"
          >
            <Power className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            {event.isActive ? 'Deactivate' : 'Activate'}
          </button>

          <div className="my-1.5 border-t border-slate-800" />

          <button
            type="button"
            onClick={() => { onDelete(event.id); close(); }}
            className="flex w-full items-center gap-3 px-4 py-2 text-sm text-rose-400 transition-colors hover:bg-rose-950/50 hover:text-rose-300"
            role="menuitem"
          >
            <Trash2 className="h-3.5 w-3.5 shrink-0" />
            Delete Event
          </button>
        </div>
      ) : null}
    </div>
  );
}
