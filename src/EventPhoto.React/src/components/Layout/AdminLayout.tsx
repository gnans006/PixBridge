import { useEffect, useRef, useState } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { authStore } from '../../store/authStore';
import { ErrorBoundary } from '../UI/ErrorBoundary';
import { Navbar } from './Navbar';
import { Sidebar } from './Sidebar';

export function AdminLayout() {
  // Start open on desktop, closed on mobile
  const [sidebarOpen, setSidebarOpen] = useState(
    () => typeof window !== 'undefined' && window.innerWidth >= 768,
  );
  const { pathname } = useLocation();
  const mainRef = useRef<HTMLElement>(null);

  // Reset scroll to top on every navigation
  useEffect(() => {
    mainRef.current?.scrollTo({ top: 0, behavior: 'instant' });
  }, [pathname]);

  if (!authStore.isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen flex-col">
      <Navbar sidebarOpen={sidebarOpen} onMenuToggle={() => setSidebarOpen(o => !o)} />
      <div className="flex flex-1 overflow-hidden">
        {/* Fading backdrop — always in DOM, opacity transitions smoothly in/out */}
        <div
          role="presentation"
          onClick={() => setSidebarOpen(false)}
          className={[
            'fixed inset-0 z-20 bg-black/50 md:hidden',
            'transition-opacity duration-300 ease-in-out',
            sidebarOpen ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none',
          ].join(' ')}
        />
        <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
        <main ref={mainRef} className="flex-1 overflow-y-auto bg-gray-50 p-4 sm:p-6">
          <ErrorBoundary key={pathname}>
            <Outlet />
          </ErrorBoundary>
        </main>
      </div>
    </div>
  );
}
