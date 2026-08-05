import { Camera, LogOut, User } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

interface NavbarProps {
  onMenuToggle?: () => void;
  sidebarOpen?: boolean;
}

export function Navbar({ onMenuToggle, sidebarOpen }: NavbarProps) {
  const { user, logout } = useAuth();

  return (
    <nav className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-4 shadow-sm sm:px-6">
      <div className="flex items-center gap-3">
        {/* Animated hamburger — three bars morph into an ✕ when sidebar is open */}
        <button
          type="button"
          onClick={onMenuToggle}
          className="flex h-9 w-9 flex-col items-center justify-center gap-1.5 rounded-lg text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-900"
          aria-label={sidebarOpen ? 'Close menu' : 'Open menu'}
        >
          <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300 origin-center${sidebarOpen ? ' translate-y-2 rotate-45' : ''}`} />
          <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300${sidebarOpen ? ' opacity-0 scale-x-0' : ''}`} />
          <span className={`block h-0.5 w-5 rounded-full bg-current transition-all duration-300 origin-center${sidebarOpen ? ' -translate-y-2 -rotate-45' : ''}`} />
        </button>
        <Link to="/admin" className="flex items-center gap-2 transition-opacity hover:opacity-80">
          <Camera className="h-6 w-6 text-primary-600" />
          <span className="text-lg font-bold text-gray-900 sm:text-xl">PixBridge</span>
        </Link>
      </div>
      <div className="flex items-center gap-3 sm:gap-4">
        <div className="hidden items-center gap-2 text-sm text-gray-600 sm:flex">
          <User className="h-4 w-4" />
          <span>{user?.username ?? 'Administrator'}</span>
        </div>
        <button type="button" onClick={logout} className="flex items-center gap-1 text-sm text-gray-500 transition-colors hover:text-red-600">
          <LogOut className="h-4 w-4" />
          <span className="hidden sm:inline">Logout</span>
        </button>
      </div>
    </nav>
  );
}
