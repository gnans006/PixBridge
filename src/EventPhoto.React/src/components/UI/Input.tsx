import type { InputHTMLAttributes } from 'react';
import { forwardRef } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  helper?: string;
  error?: string;
  leftIcon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>((
  { label, helper, error, leftIcon, className = '', ...props },
  ref
) => (
  <div className="flex flex-col gap-1.5">
    {label && (
      <label className="text-xs font-medium text-pds-text-2 uppercase tracking-wide">
        {label}
      </label>
    )}
    <div className="relative">
      {leftIcon && (
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-pds-text-muted">
          {leftIcon}
        </span>
      )}
      <input
        ref={ref}
        className={[
          'w-full rounded-xl border bg-pds-elevated px-3 py-2.5 text-sm text-pds-text',
          'placeholder:text-pds-text-muted transition-colors duration-150',
          'focus:outline-none focus:border-pds-primary focus:bg-pds-card',
          error ? 'border-pds-danger' : 'border-pds-border hover:border-pds-primary/50',
          leftIcon ? 'pl-9' : '',
          className,
        ].filter(Boolean).join(' ')}
        {...props}
      />
    </div>
    {helper && !error && <p className="text-xs text-pds-text-muted">{helper}</p>}
    {error && <p className="text-xs text-pds-danger">{error}</p>}
  </div>
));

Input.displayName = 'Input';
