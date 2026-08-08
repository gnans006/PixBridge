import type { ButtonHTMLAttributes } from 'react';
import { Loader2 } from 'lucide-react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
}

export function Button({
  variant = 'primary',
  size = 'md',
  isLoading = false,
  leftIcon,
  children,
  className = '',
  disabled,
  type,
  ...props
}: ButtonProps) {
  const base =
    'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-all duration-150 ' +
    'focus:outline-none focus-visible:ring-2 focus-visible:ring-pds-primary focus-visible:ring-offset-2 ' +
    'focus-visible:ring-offset-pds-bg disabled:pointer-events-none disabled:opacity-40 select-none';

  const variants: Record<string, string> = {
    primary:   'bg-pds-primary text-white hover:bg-pds-primary-hov shadow-pds-glow-sm hover:shadow-pds-glow active:scale-[0.98]',
    secondary: 'border border-pds-border bg-pds-elevated text-pds-text-2 hover:bg-pds-card hover:text-pds-text hover:border-pds-primary/50 active:scale-[0.98]',
    ghost:     'text-pds-text-muted hover:bg-pds-elevated hover:text-pds-text active:scale-[0.98]',
    danger:    'bg-pds-danger text-white hover:opacity-90 active:scale-[0.98]',
  };

  const sizes: Record<string, string> = {
    sm: 'h-8 px-3 text-xs',
    md: 'h-9 px-4 text-sm',
    lg: 'h-11 px-6 text-base',
  };

  return (
    <button
      type={type ?? 'button'}
      className={`${base} ${variants[variant]} ${sizes[size]} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : (leftIcon ?? null)}
      {children}
    </button>
  );
}
