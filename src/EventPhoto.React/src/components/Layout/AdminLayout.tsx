import { Navigate, Outlet } from 'react-router-dom';
import { authStore } from '../../store/authStore';
import { Navbar } from './Navbar';
import { Sidebar } from './Sidebar';

export function AdminLayout() {
  if (!authStore.isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen flex-col">
      <Navbar />
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto bg-gray-50 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
