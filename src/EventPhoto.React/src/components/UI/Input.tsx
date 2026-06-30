import type { InputHTMLAttributes } from 'react';
import { forwardRef } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(({ label, error, className = '', ...props }, ref) => (
  <div className="flex flex-col gap-1">
    {label ? <label className="text-sm font-medium text-gray-700">{label}</label> : null}
    <input
      ref={ref}
      className={`rounded-lg border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 ${
        error ? 'border-red-400 focus:ring-red-400' : 'border-gray-300'
      } ${className}`}
      {...props}
    />
    {error ? <p className="text-xs text-red-600">{error}</p> : null}
  </div>
));

Input.displayName = 'Input';
