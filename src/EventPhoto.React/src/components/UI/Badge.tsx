interface BadgeProps {
  label: string;
  color?: 'green' | 'red' | 'yellow' | 'blue' | 'gray' | 'purple' | 'indigo';
  dot?: boolean;
}

export function Badge({ label, color = 'gray', dot = false }: BadgeProps) {
  const colors: Record<string, string> = {
    green:  'bg-pds-success/15 text-pds-success ring-1 ring-pds-success/30',
    red:    'bg-pds-danger/15 text-pds-danger ring-1 ring-pds-danger/30',
    yellow: 'bg-pds-warning/15 text-pds-warning ring-1 ring-pds-warning/30',
    blue:   'bg-blue-500/15 text-blue-400 ring-1 ring-blue-500/30',
    indigo: 'bg-pds-primary/15 text-pds-primary ring-1 ring-pds-primary/30',
    purple: 'bg-pds-accent/15 text-pds-accent ring-1 ring-pds-accent/30',
    gray:   'bg-pds-elevated text-pds-text-muted ring-1 ring-pds-border',
  };
  const dotColors: Record<string, string> = {
    green: 'bg-pds-success', red: 'bg-pds-danger', yellow: 'bg-pds-warning',
    blue: 'bg-blue-400', indigo: 'bg-pds-primary', purple: 'bg-pds-accent', gray: 'bg-pds-text-muted',
  };

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${colors[color]}`}>
      {dot && <span className={`h-1.5 w-1.5 rounded-full flex-none ${dotColors[color]}`} />}
      {label}
    </span>
  );
}
