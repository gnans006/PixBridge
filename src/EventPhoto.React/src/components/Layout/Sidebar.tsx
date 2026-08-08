import { BarChart2, CalendarDays, LayoutDashboard, Settings, ScrollText, Activity, Sliders } from 'lucide-react';
import { NavLink } from 'react-router-dom';

const links = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/events', label: 'Events', icon: CalendarDays, end: false },
  { to: '/admin/statistics', label: 'Statistics', icon: BarChart2, end: false },
  { to: '/admin/logs', label: 'Logs', icon: ScrollText, end: false },
  { to: '/admin/health', label: 'Health', icon: Activity, end: false },
  { to: '/admin/settings', label: 'Settings', icon: Settings, end: false },
  { to: '/admin/system-settings', label: 'System', icon: Sliders, end: false },
];

interface SidebarProps {
  open?: boolean;
  onClose?: () => void;
}

export function Sidebar({ open, onClose }: SidebarProps) {
  return (
    <aside
      className={[
        // Base
        'z-30 flex flex-col flex-shrink-0 bg-gray-900',
        // Mobile: absolute overlay; Desktop: in-flow (relative)
        'absolute md:relative',
        // Mobile has a fixed width so the transform slide works; Desktop animates the width itself
        'w-56',
        // Mobile: animate transform; Desktop: animate width — each only at its own breakpoint
        'transition-transform md:transition-[width] duration-300 ease-in-out',
        open
          ? [
              'translate-x-0 overflow-y-auto',
              'md:w-56',
              // Drop-shadow when floating over mobile content; subtle divider on desktop
              'shadow-2xl shadow-black/40 md:shadow-none md:border-r md:border-gray-800',
            ].join(' ')
          : '-translate-x-full overflow-hidden md:w-0',
      ].join(' ')}
    >
      <div className="flex-1 py-6">
        <nav className="space-y-1 px-3">
          {links.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive ? 'bg-primary-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'
                }`
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>
      </div>
    </aside>
  );
}
