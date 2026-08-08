import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Search, X, FolderOpen, ChevronRight,
  Bell, Info, AlertTriangle, XCircle, CheckCircle2,
} from 'lucide-react';
import { eventsApi } from '../../api/events';
import type { EventResponse } from '../../types';
import type { AppNotification } from '../../hooks/useNotifications';

// ─── Search Panel ────────────────────────────────────────────────────────────

interface SearchPanelProps {
  onClose: () => void;
}

export function SearchPanel({ onClose }: SearchPanelProps) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState(0);
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);

  const { data: events = [] } = useQuery({
    queryKey: ['events'],
    queryFn: async () => {
      const r = await eventsApi.getAll();
      return r.data ?? [];
    },
    staleTime: 30_000,
  });

  const filtered: EventResponse[] = query.trim()
    ? events.filter(
        e =>
          e.name.toLowerCase().includes(query.toLowerCase()) ||
          e.clientName?.toLowerCase().includes(query.toLowerCase()) ||
          e.venueName?.toLowerCase().includes(query.toLowerCase()),
      )
    : events.slice(0, 6);

  useEffect(() => { setSelected(0); }, [query]);

  useEffect(() => {
    inputRef.current?.focus();
    const handle = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { onClose(); return; }
      if (e.key === 'ArrowDown') { e.preventDefault(); setSelected(s => Math.min(s + 1, filtered.length - 1)); }
      if (e.key === 'ArrowUp')   { e.preventDefault(); setSelected(s => Math.max(s - 1, 0)); }
      if (e.key === 'Enter' && filtered[selected]) {
        navigate(`/admin/events/${filtered[selected].id}`);
        onClose();
      }
    };
    document.addEventListener('keydown', handle);
    return () => document.removeEventListener('keydown', handle);
  }, [filtered, selected, navigate, onClose]);

  const go = (event: EventResponse) => {
    navigate(`/admin/events/${event.id}`);
    onClose();
  };

  return (
    /* Compact fixed dropdown — anchored left, 76px below top of page */
    <div className="absolute right-4 top-2 z-40 w-96 rounded-xl border border-pds-border bg-pds-elevated shadow-pds-modal overflow-hidden">
      {/* Search input row */}
      <div className="flex items-center gap-2 px-3 py-2.5 border-b border-pds-border">
        <Search className="h-3.5 w-3.5 flex-none text-pds-text-muted" />
        <input
          ref={inputRef}
          value={query}
          onChange={e => setQuery(e.target.value)}
          placeholder="Search productions…"
          className="flex-1 bg-transparent text-sm text-pds-text placeholder-pds-text-muted focus:outline-none"
        />
        <button
          onClick={onClose}
          className="flex h-6 w-6 flex-none items-center justify-center rounded-lg text-pds-text-muted hover:bg-pds-card hover:text-pds-text transition-colors"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </div>

      {/* Results */}
      <div className="max-h-64 overflow-y-auto">
        {filtered.length > 0 ? (
          <ul className="p-1.5 space-y-0.5">
            {filtered.map((event, i) => (
              <li key={event.id}>
                <button
                  onClick={() => go(event)}
                  onMouseEnter={() => setSelected(i)}
                  className={`flex w-full items-center gap-2.5 px-2.5 py-2 text-left rounded-lg transition-colors ${
                    i === selected ? 'bg-pds-primary/10' : 'hover:bg-pds-card'
                  }`}
                >
                  <FolderOpen className={`h-3.5 w-3.5 flex-none ${i === selected ? 'text-pds-primary' : 'text-pds-text-muted'}`} />
                  <div className="min-w-0 flex-1">
                    <p className={`truncate text-xs font-medium ${i === selected ? 'text-pds-text' : 'text-pds-text-2'}`}>
                      {event.name}
                    </p>
                    {(event.clientName || event.eventDate) && (
                      <p className="truncate text-[11px] text-pds-text-muted">
                        {[event.clientName, event.eventDate && new Date(event.eventDate).toLocaleDateString()].filter(Boolean).join(' · ')}
                      </p>
                    )}
                  </div>
                  <ChevronRight className={`h-3.5 w-3.5 flex-none ${i === selected ? 'text-pds-primary' : 'text-pds-border'}`} />
                </button>
              </li>
            ))}
          </ul>
        ) : query.trim() ? (
          <p className="px-3 py-3 text-xs text-pds-text-muted">No productions matching "{query}"</p>
        ) : null}
      </div>

      {/* Footer hint */}
      <div className="flex items-center gap-3 border-t border-pds-border px-3 py-2">
        <span className="flex items-center gap-1 text-[10px] text-pds-text-muted">
          <kbd className="rounded border border-pds-border bg-pds-card px-1 py-0.5 font-mono">↑↓</kbd> navigate
        </span>
        <span className="flex items-center gap-1 text-[10px] text-pds-text-muted">
          <kbd className="rounded border border-pds-border bg-pds-card px-1 py-0.5 font-mono">↵</kbd> open
        </span>
        <span className="flex items-center gap-1 text-[10px] text-pds-text-muted">
          <kbd className="rounded border border-pds-border bg-pds-card px-1 py-0.5 font-mono">Esc</kbd> close
        </span>
      </div>
    </div>
  );
}

// ─── Notifications Panel ─────────────────────────────────────────────────────

const NOTIF_ICON: Record<AppNotification['type'], React.ReactNode> = {
  info:    <Info           className="h-3.5 w-3.5 text-pds-primary" />,
  warning: <AlertTriangle  className="h-3.5 w-3.5 text-pds-warning" />,
  error:   <XCircle        className="h-3.5 w-3.5 text-pds-danger" />,
  success: <CheckCircle2   className="h-3.5 w-3.5 text-pds-success" />,
};

const NOTIF_BG: Record<AppNotification['type'], string> = {
  info:    'border-pds-primary/20 bg-pds-primary/5',
  warning: 'border-pds-warning/20 bg-pds-warning/5',
  error:   'border-pds-danger/20  bg-pds-danger/5',
  success: 'border-pds-success/20 bg-pds-success/5',
};

interface NotificationsPanelProps {
  notifications: AppNotification[];
  onDismiss: (id: string) => void;
  onDismissAll: () => void;
  onClose: () => void;
}

export function NotificationsPanel({ notifications, onDismiss, onDismissAll, onClose }: NotificationsPanelProps) {
  return (
    /* Fixed overlay — sits below the 72px header, does NOT push content */
    <div className="absolute right-4 top-2 z-40 w-80 rounded-xl border border-pds-border bg-pds-elevated shadow-pds-modal overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-2.5 border-b border-pds-border">
        <div className="flex items-center gap-2">
          <Bell className="h-3.5 w-3.5 text-pds-text-muted" />
          <span className="text-xs font-semibold text-pds-text">Notifications</span>
          {notifications.length > 0 && (
            <span className="rounded-full bg-pds-primary px-1.5 py-0.5 text-[9px] font-bold text-white leading-none">
              {notifications.length}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {notifications.length > 1 && (
            <button
              onClick={onDismissAll}
              className="text-[11px] text-pds-text-muted hover:text-pds-text-2 transition-colors"
            >
              Clear all
            </button>
          )}
          <button
            onClick={onClose}
            className="flex h-6 w-6 items-center justify-center rounded-lg text-pds-text-muted hover:bg-pds-card hover:text-pds-text transition-colors"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* Items */}
      <div className="max-h-72 overflow-y-auto">
        {notifications.length === 0 ? (
          <div className="flex items-center gap-2 px-3 py-3 text-xs text-pds-text-muted">
            <CheckCircle2 className="h-3.5 w-3.5 text-pds-success flex-none" />
            All clear — no notifications.
          </div>
        ) : (
          <div className="p-2 space-y-1.5">
            {notifications.map(n => (
              <div
                key={n.id}
                className={`flex items-start gap-2.5 rounded-lg border px-3 py-2.5 ${NOTIF_BG[n.type]}`}
              >
                <div className="flex-none mt-0.5">{NOTIF_ICON[n.type]}</div>
                <div className="min-w-0 flex-1">
                  <p className="text-xs font-semibold text-pds-text leading-tight">{n.title}</p>
                  <p className="mt-0.5 text-[11px] text-pds-text-muted leading-relaxed">{n.message}</p>
                </div>
                <button
                  onClick={() => onDismiss(n.id)}
                  className="flex h-5 w-5 flex-none items-center justify-center rounded text-pds-text-muted hover:bg-pds-elevated hover:text-pds-text transition-colors mt-0.5"
                  aria-label="Dismiss"
                >
                  <X className="h-3 w-3" />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
