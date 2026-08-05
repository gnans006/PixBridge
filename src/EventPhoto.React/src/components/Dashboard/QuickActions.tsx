import type { LucideIcon } from 'lucide-react';
import { Activity, BarChart2, PlusCircle, ScanFace, Settings, Shield } from 'lucide-react';
import { Link } from 'react-router-dom';

interface ActionTile {
  to: string;
  label: string;
  description: string;
  icon: LucideIcon;
  gradient: string;
  textColor: string;
}

const actions: ActionTile[] = [
  {
    to: '/admin/events/new',
    label: 'Create Event',
    description: 'Set up a new photography event',
    icon: PlusCircle,
    gradient: 'from-indigo-600 to-violet-700',
    textColor: 'text-indigo-100',
  },
  {
    to: '/admin/events',
    label: 'Browse Events',
    description: 'View and manage all events',
    icon: Activity,
    gradient: 'from-violet-600 to-purple-700',
    textColor: 'text-violet-100',
  },
  {
    to: '/admin/statistics',
    label: 'Statistics',
    description: 'Per-event analytics & reports',
    icon: BarChart2,
    gradient: 'from-blue-600 to-indigo-700',
    textColor: 'text-blue-100',
  },
  {
    to: '/admin/events',
    label: 'Face Search',
    description: 'AI face recognition events',
    icon: ScanFace,
    gradient: 'from-amber-600 to-orange-700',
    textColor: 'text-amber-100',
  },
  {
    to: '/admin/health',
    label: 'System Health',
    description: 'Monitor services & status',
    icon: Shield,
    gradient: 'from-emerald-600 to-teal-700',
    textColor: 'text-emerald-100',
  },
  {
    to: '/admin/settings',
    label: 'Settings',
    description: 'Studio branding & config',
    icon: Settings,
    gradient: 'from-slate-600 to-slate-700',
    textColor: 'text-slate-100',
  },
];

export function QuickActions() {
  return (
    <div>
      <h2 className="mb-4 text-sm font-semibold uppercase tracking-wider text-slate-400">
        Quick Actions
      </h2>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        {actions.map((action) => (
          <Link
            key={action.to + action.label}
            to={action.to}
            className="group flex flex-col gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-4 transition-all duration-200 hover:-translate-y-0.5 hover:border-slate-700 hover:shadow-lg hover:shadow-black/20 active:scale-95"
          >
            <div className={`flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br ${action.gradient} shadow-md`}>
              <action.icon className="h-5 w-5 text-white" />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-100">{action.label}</p>
              <p className={`mt-0.5 text-xs ${action.textColor} opacity-70`}>{action.description}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
