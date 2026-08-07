import { useEffect, useState, type ReactNode } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronDown } from 'lucide-react';

interface SectionCardProps {
  icon: ReactNode;
  title: string;
  subtitle?: string;
  badge?: ReactNode;
  defaultOpen?: boolean;
  collapsible?: boolean;
  /** When true the section auto-expands so error-highlighted fields become visible */
  hasError?: boolean;
  children: ReactNode;
}

export function SectionCard({
  icon,
  title,
  subtitle,
  badge,
  defaultOpen = true,
  collapsible = true,
  hasError = false,
  children,
}: SectionCardProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  // Auto-expand when this section has errors so the red-bordered field is visible
  useEffect(() => {
    if (hasError) setIsOpen(true);
  }, [hasError]);

  return (
    <motion.div
      layout="position"
      className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900"
    >
      <button
        type="button"
        className="flex w-full items-center gap-3 px-5 py-4 text-left transition-colors hover:bg-slate-800/40 focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-indigo-500"
        onClick={() => collapsible && setIsOpen((o) => !o)}
        aria-expanded={isOpen}
        disabled={!collapsible}
      >
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-indigo-500/10 text-indigo-400">
          {icon}
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-slate-100">{title}</p>
          {subtitle && <p className="mt-0.5 truncate text-xs text-slate-400">{subtitle}</p>}
        </div>
        {badge && <div className="shrink-0">{badge}</div>}
        {collapsible && (
          <motion.span
            animate={{ rotate: isOpen ? 180 : 0 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
            className="shrink-0 text-slate-500"
          >
            <ChevronDown className="h-4 w-4" />
          </motion.span>
        )}
      </button>

      <AnimatePresence initial={false}>
        {isOpen && (
          <motion.div
            key="content"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.22, ease: 'easeInOut' }}
            style={{ overflow: 'hidden' }}
          >
            <div className="border-t border-slate-800 px-5 pb-5 pt-4">{children}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
}
