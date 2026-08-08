import type { ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';
import { FolderOpen } from 'lucide-react';
import { Button } from './Button';

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: { label: string; onClick: () => void };
  actionHref?: string;
  children?: ReactNode;
}

export function EmptyState({ icon: Icon = FolderOpen, title, description, action, children }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-pds-border bg-pds-card/40 px-6 py-16 text-center">
      <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-pds-elevated">
        <Icon className="h-6 w-6 text-pds-text-muted" />
      </div>
      <h3 className="text-base font-semibold text-pds-text">{title}</h3>
      {description && (
        <p className="mt-1.5 max-w-sm text-sm text-pds-text-muted leading-relaxed">{description}</p>
      )}
      {action && (
        <div className="mt-5">
          <Button onClick={action.onClick}>{action.label}</Button>
        </div>
      )}
      {children && <div className="mt-5">{children}</div>}
    </div>
  );
}
