export function Spinner({ size = 'md' }: { size?: 'sm' | 'md' | 'lg' }) {
  const className = {
    sm: 'h-4 w-4',
    md: 'h-8 w-8',
    lg: 'h-12 w-12',
  }[size];

  return <div className={`animate-spin rounded-full border-2 border-gray-300 border-t-primary-600 ${className}`} />;
}
