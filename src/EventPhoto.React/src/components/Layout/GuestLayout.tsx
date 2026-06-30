import { Camera } from 'lucide-react';
import { Outlet } from 'react-router-dom';

export function GuestLayout() {
  return (
    <div className="min-h-screen bg-gray-900">
      <header className="flex items-center gap-3 bg-gray-950 px-6 py-4 shadow-md">
        <Camera className="h-6 w-6 text-primary-400" />
        <span className="text-xl font-bold text-white">PixBridge</span>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
