import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search, X, FolderOpen, Calendar, ChevronRight } from 'lucide-react';
import { eventsApi } from '../../api/events';
import type { EventResponse } from '../../types';

interface Props {
  onClose: () => void;
}

export function EventSearchModal({ onClose }: Props) {
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
    : events.slice(0, 8);

  useEffect(() => { setSelected(0); }, [query]);

  useEffect(() => {
    inputRef.current?.focus();
    const handle = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { onClose(); return; }
      if (e.key === 'ArrowDown') { e.preventDefault(); setSelected(s => Math.min(s + 1, filtered.length - 1)); }
      if (e.key === 'ArrowUp') { e.preventDefault(); setSelected(s => Math.max(s - 1, 0)); }
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
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh]">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose} />

      {/* Panel */}
      <div className="relative z-10 w-full max-w-xl mx-4 rounded-2xl border border-pds-border bg-pds-elevated shadow-pds-modal overflow-hidden">
        {/* Input */}
        <div className="flex items-center gap-3 border-b border-pds-border px-4 py-3.5">
          <Search className="h-4 w-4 flex-none text-pds-text-muted" />
          <input
            ref={inputRef}
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder="Search productions…"
            className="flex-1 bg-transparent text-sm text-pds-text placeholder-pds-text-muted focus:outline-none"
          />
          <button
            onClick={onClose}
            className="flex h-6 w-6 flex-none items-center justify-center rounded-md border border-pds-border text-[10px] font-medium text-pds-text-muted hover:text-pds-text transition-colors"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>

        {/* Results */}
        <div className="max-h-[50vh] overflow-y-auto">
          {filtered.length === 0 ? (
            <div className="py-10 text-center">
              <FolderOpen className="mx-auto h-8 w-8 text-pds-text-muted opacity-40" />
              <p className="mt-2 text-sm text-pds-text-muted">No productions found</p>
            </div>
          ) : (
            <ul className="p-2">
              {filtered.map((event, i) => (
                <li key={event.id}>
                  <button
                    onClick={() => go(event)}
                    onMouseEnter={() => setSelected(i)}
                    className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left transition-colors ${
                      i === selected ? 'bg-pds-primary/10' : 'hover:bg-pds-card'
                    }`}
                  >
                    <div className={`flex h-8 w-8 flex-none items-center justify-center rounded-lg ${
                      i === selected ? 'bg-pds-primary/20' : 'bg-pds-elevated'
                    }`}>
                      <FolderOpen className={`h-4 w-4 ${i === selected ? 'text-pds-primary' : 'text-pds-text-muted'}`} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className={`truncate text-sm font-medium ${i === selected ? 'text-pds-text' : 'text-pds-text-2'}`}>
                        {event.name}
                      </p>
                      <div className="flex items-center gap-2 text-xs text-pds-text-muted">
                        {event.clientName && <span className="truncate">{event.clientName}</span>}
                        {event.eventDate && (
                          <span className="flex items-center gap-1 flex-none">
                            <Calendar className="h-3 w-3" />
                            {new Date(event.eventDate).toLocaleDateString()}
                          </span>
                        )}
                      </div>
                    </div>
                    <ChevronRight className={`h-4 w-4 flex-none transition-colors ${i === selected ? 'text-pds-primary' : 'text-pds-border'}`} />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Footer hint */}
        <div className="flex items-center gap-4 border-t border-pds-border px-4 py-2.5">
          <div className="flex items-center gap-1.5 text-[10px] text-pds-text-muted">
            <kbd className="rounded border border-pds-border bg-pds-card px-1.5 py-0.5 font-mono">↑↓</kbd>
            <span>Navigate</span>
          </div>
          <div className="flex items-center gap-1.5 text-[10px] text-pds-text-muted">
            <kbd className="rounded border border-pds-border bg-pds-card px-1.5 py-0.5 font-mono">↵</kbd>
            <span>Open</span>
          </div>
          <div className="flex items-center gap-1.5 text-[10px] text-pds-text-muted">
            <kbd className="rounded border border-pds-border bg-pds-card px-1.5 py-0.5 font-mono">Esc</kbd>
            <span>Close</span>
          </div>
        </div>
      </div>
    </div>
  );
}
