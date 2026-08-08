import { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Camera, LogOut, ChevronDown, User, KeyRound, Search, Bell } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';
import { ChangePasswordModal } from '../UI/ChangePasswordModal';

const LABELS: Record<string, string> = {
  admin: 'Dashboard', events: 'Productions', new: 'New Production',
  statistics: 'Insights', logs: 'System Logs', health: 'Health Monitor',
  settings: 'Settings', 'system-settings': 'System Settings',
  studio: 'Studio', users: 'Users', profile: 'Profile', branding: 'Branding',
  platform: 'Platform', audit: 'Audit Logs', network: 'Network', appearance: 'Appearance',
  ai: 'AI Studio', 'face-recognition': 'Face Recognition',
};

function PageTitle() {
  const { pathname } = useLocation();
  const parts = pathname.split('/').filter(Boolean).filter(p => !/^[0-9a-f-]{36}$/i.test(p));
  const last = parts[parts.length - 1] ?? 'admin';
  return <span className="text-sm font-semibold text-pds-text">{LABELS[last] ?? last}</span>;
}

interface NavbarProps {
  onMenuToggle?: () => void;
  sidebarOpen?: boolean;
  onCollapseToggle?: () => void;
  onSearchToggle?: () => void;
  onNotifToggle?: () => void;
  notifCount?: number;
  searchActive?: boolean;
  notifActive?: boolean;
}

export function Navbar({
  onMenuToggle, sidebarOpen, onCollapseToggle,
  onSearchToggle, onNotifToggle, notifCount = 0,
  searchActive, notifActive,
}: NavbarProps) {
  const { user, logout } = useAuth();
  const { data: appSettings } = useApplicationSettings();
  const studioName = appSettings?.studioName ?? 'PixBridge';
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [showChangePassword, setShowChangePassword] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handler(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  // Global ⌘K / Ctrl+K shortcut
  useEffect(() => {
    function handler(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        onSearchToggle?.();
      }
    }
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [onSearchToggle]);

  const roleLabel = user?.role?.replace(/([A-Z])/g, ' $1').trim() ?? 'User';

  return (
    <>
      <header className="flex h-18 flex-shrink-0 items-center justify-between border-b border-pds-border bg-pds-surface px-4 sm:px-6 z-40 sticky top-0">
        {/* Left */}
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={onMenuToggle}
            className="flex h-9 w-9 flex-col items-center justify-center gap-1.5 rounded-lg text-pds-text-muted transition-colors hover:bg-pds-elevated hover:text-pds-text md:hidden"
            aria-label={sidebarOpen ? 'Close menu' : 'Open menu'}
          >
            <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300 origin-center${sidebarOpen ? ' translate-y-2 rotate-45' : ''}`} />
            <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300${sidebarOpen ? ' opacity-0 scale-x-0' : ''}`} />
            <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300 origin-center${sidebarOpen ? ' -translate-y-2 -rotate-45' : ''}`} />
          </button>
          <button
            type="button"
            onClick={onCollapseToggle}
            className="hidden md:flex h-9 w-9 flex-col items-center justify-center gap-1.5 rounded-lg text-pds-text-muted transition-colors hover:bg-pds-elevated hover:text-pds-text"
            aria-label="Toggle sidebar"
          >
            <span className="block h-0.5 w-5 rounded-full bg-current" />
            <span className="block h-0.5 w-3.5 rounded-full bg-current self-start ml-[5px]" />
            <span className="block h-0.5 w-5 rounded-full bg-current" />
          </button>
          <Link to="/admin" className="flex items-center gap-2 transition-opacity hover:opacity-80">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-pds-primary/20">
              <Camera className="h-4 w-4 text-pds-primary" />
            </div>
            <span className="hidden text-base font-bold text-pds-text sm:block">{studioName}</span>
          </Link>
          <div className="hidden items-center gap-2 md:flex">
            <span className="text-pds-border select-none">/</span>
            <PageTitle />
          </div>
        </div>

        {/* Right */}
        <div className="flex items-center gap-2">
          {/* Search toggle */}
          <button
            type="button"
            onClick={onSearchToggle}
            className={`hidden items-center gap-2 rounded-lg border px-3 py-1.5 text-sm transition-colors sm:flex ${
              searchActive
                ? 'border-pds-primary bg-pds-primary/10 text-pds-primary'
                : 'border-pds-border bg-pds-elevated text-pds-text-muted hover:border-pds-primary/50 hover:text-pds-text'
            }`}
          >
            <Search className="h-3.5 w-3.5" />
            <span className="text-xs">Search</span>
            <kbd className="ml-1 rounded border border-pds-border bg-pds-card px-1.5 py-0.5 text-[10px] font-mono text-pds-text-muted">⌘K</kbd>
          </button>

          {/* Notifications toggle */}
          <button
            type="button"
            onClick={onNotifToggle}
            className={`relative flex h-9 w-9 items-center justify-center rounded-lg transition-colors ${
              notifActive
                ? 'bg-pds-primary/10 text-pds-primary'
                : 'text-pds-text-muted hover:bg-pds-elevated hover:text-pds-text'
            }`}
          >
            <Bell className="h-4 w-4" />
            {notifCount > 0 && !notifActive && (
              <span className="absolute right-1.5 top-1.5 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-pds-primary text-[9px] font-bold text-white leading-none">
                {notifCount > 9 ? '9+' : notifCount}
              </span>
            )}
          </button>

          {/* User menu */}
          <div className="relative" ref={menuRef}>
            <button
              type="button"
              onClick={() => setUserMenuOpen(o => !o)}
              className="flex items-center gap-2 rounded-lg border border-pds-border bg-pds-elevated px-2.5 py-1.5 transition-colors hover:border-pds-primary/50 hover:bg-pds-card"
            >
              <div className="flex h-6 w-6 items-center justify-center rounded-full bg-pds-primary/20 text-pds-primary">
                <User className="h-3.5 w-3.5" />
              </div>
              <span className="hidden text-sm font-medium text-pds-text sm:block">
                {user?.username ?? 'User'}
              </span>
              <ChevronDown className={`h-3.5 w-3.5 text-pds-text-muted transition-transform duration-200 ${userMenuOpen ? 'rotate-180' : ''}`} />
            </button>

            {userMenuOpen && (
              <div className="absolute right-0 top-full mt-2 w-56 rounded-xl border border-pds-border bg-pds-elevated shadow-pds-modal z-50 overflow-hidden">
                <div className="border-b border-pds-border px-4 py-3">
                  <p className="text-sm font-semibold text-pds-text">{user?.username ?? 'User'}</p>
                  <p className="text-xs text-pds-text-muted">{roleLabel}</p>
                </div>
                <div className="p-1">
                  <button
                    type="button"
                    onClick={() => { setShowChangePassword(true); setUserMenuOpen(false); }}
                    className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-pds-text-2 hover:bg-pds-card hover:text-pds-text transition-colors"
                  >
                    <KeyRound className="h-4 w-4" />
                    Change Password
                  </button>
                  <button
                    type="button"
                    onClick={() => { logout(); setUserMenuOpen(false); }}
                    className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-pds-danger hover:bg-pds-danger/10 transition-colors"
                  >
                    <LogOut className="h-4 w-4" />
                    Sign Out
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </header>

      {showChangePassword && <ChangePasswordModal onClose={() => setShowChangePassword(false)} />}
    </>
  );
}
