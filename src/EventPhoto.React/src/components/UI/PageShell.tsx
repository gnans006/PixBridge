import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';

// ── Breadcrumb ────────────────────────────────────────────────────────────────

const LABELS: Record<string, string> = {
  admin:          'Dashboard',
  events:         'Productions',
  new:            'New Production',
  statistics:     'Insights',
  logs:           'System Logs',
  health:         'Health Monitor',
  settings:       'Settings',
  'system-settings': 'System Settings',
  studio:         'Studio',
  users:          'Users',
  profile:        'Profile',
  branding:       'Branding',
  platform:       'Platform',
  audit:          'Audit Logs',
  network:        'Network',
  appearance:     'Appearance',
};

function useBreadcrumbs() {
  const { pathname } = useLocation();
  const parts = pathname.split('/').filter(Boolean);
  const crumbs: { label: string; to: string }[] = [];
  let path = '';
  for (const part of parts) {
    path += `/${part}`;
    // Skip UUID segments
    if (/^[0-9a-f-]{36}$/i.test(part)) continue;
    crumbs.push({ label: LABELS[part] ?? part, to: path });
  }
  return crumbs;
}

// ── PageShell ─────────────────────────────────────────────────────────────────

interface PageShellProps {
  /** Page title rendered as h1. */
  title: string;
  /** Optional short description below the title. */
  description?: string;
  /** Slot for action buttons (e.g. "Create Production"). */
  actions?: ReactNode;
  /** Page body content. */
  children: ReactNode;
  /** Hide the breadcrumb (e.g. on Dashboard). */
  hideBreadcrumb?: boolean;
}

export function PageShell({ title, description, actions, children, hideBreadcrumb }: PageShellProps) {
  const crumbs = useBreadcrumbs();

  return (
    <div className="flex flex-col gap-6 animate-fade-in">
      {/* Breadcrumb */}
      {!hideBreadcrumb && crumbs.length > 1 && (
        <nav aria-label="Breadcrumb">
          <ol className="flex items-center gap-1.5 text-xs text-pds-text-muted">
            {crumbs.map((c, i) => (
              <li key={c.to} className="flex items-center gap-1.5">
                {i > 0 && <ChevronRight className="h-3 w-3 flex-none" />}
                {i < crumbs.length - 1 ? (
                  <Link to={c.to} className="hover:text-pds-text transition-colors">
                    {c.label}
                  </Link>
                ) : (
                  <span className="text-pds-text-2 font-medium">{c.label}</span>
                )}
              </li>
            ))}
          </ol>
        </nav>
      )}

      {/* Page header */}
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-3xl font-bold tracking-tight text-pds-text">{title}</h1>
          {description && (
            <p className="mt-1.5 text-sm text-pds-text-muted leading-relaxed">{description}</p>
          )}
        </div>
        {actions && <div className="flex items-center gap-2 flex-shrink-0">{actions}</div>}
      </div>

      {/* Content */}
      <div>{children}</div>
    </div>
  );
}
