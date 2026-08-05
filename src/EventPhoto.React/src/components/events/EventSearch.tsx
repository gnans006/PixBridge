import { Search, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

interface EventSearchProps {
  value: string;
  onChange: (value: string) => void;
}

export function EventSearch({ value, onChange }: EventSearchProps) {
  const [local, setLocal] = useState(value);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const onChangeRef = useRef(onChange);
  const inputRef = useRef<HTMLInputElement>(null);

  // Keep the ref current without re-triggering the debounce effect
  useEffect(() => {
    onChangeRef.current = onChange;
  });

  // Debounce: fire onChange 300 ms after the user stops typing
  useEffect(() => {
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => onChangeRef.current(local), 300);
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [local]);

  // Sync external reset (e.g., parent clears the filter)
  useEffect(() => {
    if (value === '') setLocal('');
  }, [value]);

  return (
    <div className="relative">
      <Search className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-500" />
      <input
        ref={inputRef}
        type="text"
        value={local}
        onChange={(e) => setLocal(e.target.value)}
        placeholder="Search events, clients, venues, event types..."
        className="w-full rounded-2xl border border-slate-700 bg-slate-900 py-3.5 pl-12 pr-12 text-sm text-slate-100 placeholder-slate-500 shadow-sm transition-all duration-200 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
      />
      {local ? (
        <button
          type="button"
          onClick={() => { setLocal(''); onChangeRef.current(''); inputRef.current?.focus(); }}
          className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full p-0.5 text-slate-400 transition-colors hover:bg-slate-700 hover:text-slate-200"
          aria-label="Clear search"
        >
          <X className="h-4 w-4" />
        </button>
      ) : null}
    </div>
  );
}
