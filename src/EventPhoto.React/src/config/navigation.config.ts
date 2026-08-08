import {
  LayoutDashboard,
  CalendarDays,
  Users,
  QrCode,
  ScanFace,
  BarChart2,
  Building2,
  Settings,
  Network,
  ScrollText,
  Activity,
  Sliders,
  FolderOpen,
  Shield,
  Sparkles,
  Palette,
  type LucideIcon,
} from 'lucide-react';

export type NavigationRole = 'StudioOwner' | 'StudioManager' | 'Operator' | 'Admin' | 'Viewer';

export interface NavItem {
  label: string;
  to: string;
  icon: LucideIcon;
  end?: boolean;
  /** Minimum role required. If omitted the item is visible to all authenticated users. */
  minRole?: NavigationRole;
}

export interface NavSection {
  id: string;
  label: string;
  items: NavItem[];
  /** If set, section is hidden for roles not in this list */
  allowedRoles?: NavigationRole[];
}

// Role hierarchy — higher index = higher access
const ROLE_HIERARCHY: NavigationRole[] = [
  'Viewer',
  'Operator',
  'StudioManager',
  'StudioOwner',
  'Admin',
];

export function hasMinRole(userRole: string, minRole: NavigationRole): boolean {
  const userIdx = ROLE_HIERARCHY.indexOf(userRole as NavigationRole);
  const minIdx  = ROLE_HIERARCHY.indexOf(minRole);
  return userIdx >= minIdx;
}

export const NAVIGATION: NavSection[] = [
  // ── Command Center ─────────────────────────────────────────────────────────
  {
    id: 'command',
    label: 'Command Center',
    items: [
      { label: 'Dashboard',     to: '/admin',            icon: LayoutDashboard, end: true },
    ],
  },

  // ── Productions ────────────────────────────────────────────────────────────
  {
    id: 'productions',
    label: 'Productions',
    items: [
      { label: 'All Productions', to: '/admin/events',     icon: FolderOpen },
      { label: 'New Production',  to: '/admin/events/new', icon: CalendarDays },
    ],
  },

  // ── Experiences ────────────────────────────────────────────────────────────
  {
    id: 'experiences',
    label: 'Experiences',
    items: [
      { label: 'QR Center', to: '/admin/experiences/qr', icon: QrCode },
    ],
  },

  // ── AI Studio ──────────────────────────────────────────────────────────────
  {
    id: 'ai',
    label: 'AI Studio',
    items: [
      { label: 'Face Recognition', to: '/admin/ai/face-recognition', icon: ScanFace, minRole: 'StudioManager' },
    ],
    allowedRoles: ['StudioOwner', 'StudioManager', 'Admin'],
  },

  // ── Insights ───────────────────────────────────────────────────────────────
  {
    id: 'insights',
    label: 'Insights',
    items: [
      { label: 'Statistics', to: '/admin/statistics', icon: BarChart2, minRole: 'StudioManager' },
    ],
    allowedRoles: ['StudioOwner', 'StudioManager', 'Admin'],
  },

  // ── Studio ─────────────────────────────────────────────────────────────────
  {
    id: 'studio',
    label: 'Studio',
    items: [
      { label: 'Studio Users',  to: '/admin/studio/users',    icon: Users,     minRole: 'StudioOwner' },
      { label: 'Studio Profile',to: '/admin/studio/profile',  icon: Building2, minRole: 'StudioOwner' },
      { label: 'Branding',      to: '/admin/studio/branding', icon: Sparkles,  minRole: 'StudioOwner' },
      { label: 'Settings',      to: '/admin/settings',        icon: Settings,  minRole: 'StudioManager' },
    ],
    allowedRoles: ['StudioOwner', 'StudioManager', 'Admin'],
  },

  // ── Platform ───────────────────────────────────────────────────────────────
  {
    id: 'platform',
    label: 'Platform',
    items: [
      { label: 'System Settings', to: '/admin/system-settings', icon: Sliders,   minRole: 'StudioOwner' },
      { label: 'Network',         to: '/admin/platform/network',     icon: Network,   minRole: 'StudioOwner' },
      { label: 'Appearance',       to: '/admin/platform/appearance',  icon: Palette,   minRole: 'StudioOwner' },
      { label: 'Audit Logs',      to: '/admin/platform/audit',  icon: Shield,    minRole: 'StudioOwner' },
      { label: 'Logs',            to: '/admin/logs',            icon: ScrollText,minRole: 'StudioOwner' },
      { label: 'Health Monitor',  to: '/admin/health',          icon: Activity,  minRole: 'StudioOwner' },
    ],
    allowedRoles: ['StudioOwner', 'Admin'],
  },
];
