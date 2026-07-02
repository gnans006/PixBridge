import { BarChart2, CalendarDays, LayoutDashboard, Settings, ScrollText, Activity } from 'lucide-react';
import { NavLink } from 'react-router-dom';

const links = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/events', label: 'Events', icon: CalendarDays, end: false },
  { to: '/admin/statistics', label: 'Statistics', icon: BarChart2, end: false },
  { to: '/admin/logs', label: 'Logs', icon: ScrollText, end: false },
  { to: '/admin/health', label: 'Health', icon: Activity, end: false },
  { to: '/admin/settings', label: 'Settings', icon: Settings, end: false },
];

export function Sidebar() {
  return (
    <aside className="flex h-full w-56 flex-col overflow-y-auto bg-gray-900">
      <div className="flex-1 py-6">
        <nav className="space-y-1 px-3">
          {links.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
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
