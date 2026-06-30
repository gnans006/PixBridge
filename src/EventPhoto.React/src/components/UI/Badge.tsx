interface BadgeProps {
  label: string;
  color?: 'green' | 'red' | 'yellow' | 'blue' | 'gray';
}

export function Badge({ label, color = 'gray' }: BadgeProps) {
  const className = {
    green: 'bg-green-100 text-green-800',
    red: 'bg-red-100 text-red-800',
    yellow: 'bg-yellow-100 text-yellow-800',
    blue: 'bg-blue-100 text-blue-800',
    gray: 'bg-gray-100 text-gray-700',
  }[color];

  return <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${className}`}>{label}</span>;
}
