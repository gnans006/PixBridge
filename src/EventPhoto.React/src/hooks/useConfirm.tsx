import { createContext, useContext, useRef, useState, useCallback, type ReactNode } from 'react';

// ── Types ─────────────────────────────────────────────────────────────────────

export type ConfirmVariant = 'danger' | 'warning' | 'info';

export interface ConfirmOptions {
  title: string;
  message?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: ConfirmVariant;
}

interface ConfirmState extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

interface ConfirmContextValue {
  confirm: (options: ConfirmOptions | string) => Promise<boolean>;
}

// ── Context ───────────────────────────────────────────────────────────────────

const ConfirmContext = createContext<ConfirmContextValue | null>(null);

// ── Provider ──────────────────────────────────────────────────────────────────

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<ConfirmState | null>(null);
  // Keep a stable ref to avoid stale closures in the confirm function
  const resolverRef = useRef<((v: boolean) => void) | null>(null);

  const confirm = useCallback((options: ConfirmOptions | string): Promise<boolean> => {
    const opts: ConfirmOptions =
      typeof options === 'string' ? { title: options } : options;

    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve;
      setState({ ...opts, resolve });
    });
  }, []);

  const handleClose = (confirmed: boolean) => {
    resolverRef.current?.(confirmed);
    resolverRef.current = null;
    setState(null);
  };

  return (
    <ConfirmContext.Provider value={{ confirm }}>
      {children}
      {state && <ConfirmDialog state={state} onClose={handleClose} />}
    </ConfirmContext.Provider>
  );
}

// ── Hook ──────────────────────────────────────────────────────────────────────

export function useConfirm(): (options: ConfirmOptions | string) => Promise<boolean> {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error('useConfirm must be used inside <ConfirmProvider>');
  return ctx.confirm;
}

// ── Dialog Component ──────────────────────────────────────────────────────────

const VARIANT_STYLES: Record<ConfirmVariant, {
  icon: string;
  iconBg: string;
  iconColor: string;
  confirmBtn: string;
}> = {
  danger: {
    icon: '🗑',
    iconBg: 'bg-rose-500/10',
    iconColor: 'text-rose-400',
    confirmBtn: 'bg-rose-600 hover:bg-rose-500 focus-visible:ring-rose-500',
  },
  warning: {
    icon: '⚠',
    iconBg: 'bg-amber-500/10',
    iconColor: 'text-amber-400',
    confirmBtn: 'bg-amber-600 hover:bg-amber-500 focus-visible:ring-amber-500',
  },
  info: {
    icon: 'ℹ',
    iconBg: 'bg-indigo-500/10',
    iconColor: 'text-indigo-400',
    confirmBtn: 'bg-indigo-600 hover:bg-indigo-500 focus-visible:ring-indigo-500',
  },
};

function ConfirmDialog({
  state,
  onClose,
}: {
  state: ConfirmState;
  onClose: (confirmed: boolean) => void;
}) {
  const variant = state.variant ?? 'warning';
  const styles = VARIANT_STYLES[variant];

  // Close on backdrop click
  const handleBackdrop = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) onClose(false);
  };

  // Close on Escape
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') onClose(false);
    if (e.key === 'Enter') onClose(true);
  };

  return (
    <div
      role="presentation"
      className="fixed inset-0 z-[100] flex items-center justify-center p-4"
      style={{ backgroundColor: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(2px)' }}
      onClick={handleBackdrop}
      onKeyDown={handleKeyDown}
    >
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirm-title"
        aria-describedby={state.message ? 'confirm-message' : undefined}
        className="w-full max-w-md rounded-2xl border border-slate-700 bg-slate-900 shadow-2xl
                   animate-in fade-in zoom-in-95 duration-150"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Body */}
        <div className="flex gap-4 p-6">
          {/* Icon */}
          <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-lg
                            ${styles.iconBg} ${styles.iconColor}`}>
            {styles.icon}
          </span>

          {/* Text */}
          <div className="min-w-0">
            <h2 id="confirm-title" className="text-sm font-semibold text-slate-100">
              {state.title}
            </h2>
            {state.message && (
              <p id="confirm-message" className="mt-1.5 text-sm leading-relaxed text-slate-400">
                {state.message}
              </p>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-2 border-t border-slate-800 px-6 py-4">
          <button
            type="button"
            autoFocus
            onClick={() => onClose(false)}
            className="rounded-xl border border-slate-700 px-4 py-2 text-sm font-medium
                       text-slate-300 transition-colors hover:bg-slate-800
                       focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-500"
          >
            {state.cancelLabel ?? 'Cancel'}
          </button>
          <button
            type="button"
            onClick={() => onClose(true)}
            className={`rounded-xl px-4 py-2 text-sm font-medium text-white shadow-sm
                        transition-colors focus:outline-none focus-visible:ring-2
                        focus-visible:ring-offset-2 focus-visible:ring-offset-slate-900
                        ${styles.confirmBtn}`}
          >
            {state.confirmLabel ?? 'Confirm'}
          </button>
        </div>
      </div>
    </div>
  );
}
