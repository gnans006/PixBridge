import { Camera, LogOut, User } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';

export function Navbar() {
  const { user, logout } = useAuth();

  return (
    <nav className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6 shadow-sm">
      <div className="flex items-center gap-2">
        <Camera className="h-6 w-6 text-primary-600" />
        <span className="text-xl font-bold text-gray-900">PixBridge</span>
      </div>
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <User className="h-4 w-4" />
          <span>{user?.username ?? 'Administrator'}</span>
        </div>
        <button type="button" onClick={logout} className="flex items-center gap-1 text-sm text-gray-500 transition-colors hover:text-red-600">
          <LogOut className="h-4 w-4" />
          <span>Logout</span>
        </button>
      </div>
    </nav>
  );
}
