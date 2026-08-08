import type { HTMLAttributes } from 'react';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'elevated' | 'surface';
  padding?: boolean;
}

export function Card({ variant = 'default', padding = false, className = '', children, ...props }: CardProps) {
  const variants: Record<string, string> = {
    default:  'bg-pds-card border-pds-border',
    elevated: 'bg-pds-elevated border-pds-border',
    surface:  'bg-pds-surface border-pds-border',
  };
  return (
    <div
      className={`rounded-2xl border ${variants[variant]} ${padding ? 'p-6' : ''} ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}
