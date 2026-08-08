import { useEffect, useRef, useState } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { authStore } from '../../store/authStore';
import { ErrorBoundary } from '../UI/ErrorBoundary';
import { Navbar } from './Navbar';
import { Sidebar } from './Sidebar';
import { SearchPanel, NotificationsPanel } from './SubHeader';
import { useNotifications } from '../../hooks/useNotifications';

type ActivePanel = 'search' | 'notifications' | null;

export function AdminLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(
    () => typeof window !== 'undefined' && window.innerWidth >= 768,
  );
  const [sidebarCollapsed, setSidebarCollapsed] = useState(
    () => localStorage.getItem('pds-sidebar-collapsed') === 'true',
  );
  const [activePanel, setActivePanel] = useState<ActivePanel>(null);
  const { pathname } = useLocation();
  const mainRef = useRef<HTMLElement>(null);
  const { notifications, dismiss, dismissAll, count: notifCount } = useNotifications();

  useEffect(() => {
    mainRef.current?.scrollTo({ top: 0, behavior: 'instant' });
    setActivePanel(null); // close panels on navigation
  }, [pathname]);

  const toggleCollapse = () => {
    setSidebarCollapsed(c => {
      const next = !c;
      localStorage.setItem('pds-sidebar-collapsed', String(next));
      return next;
    });
  };

  const togglePanel = (panel: ActivePanel) =>
    setActivePanel(p => (p === panel ? null : panel));

  if (!authStore.isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen flex-col bg-pds-bg">
      <Navbar
        sidebarOpen={sidebarOpen}
        onMenuToggle={() => setSidebarOpen(o => !o)}
        onCollapseToggle={toggleCollapse}
        onSearchToggle={() => togglePanel('search')}
        onNotifToggle={() => togglePanel('notifications')}
        notifCount={notifCount}
        searchActive={activePanel === 'search'}
        notifActive={activePanel === 'notifications'}
      />

      <div className="flex flex-1 overflow-hidden">
        {/* Mobile backdrop */}
        <div
          role="presentation"
          onClick={() => setSidebarOpen(false)}
          className={[
            'fixed inset-0 z-20 bg-black/60 backdrop-blur-sm md:hidden',
            'transition-opacity duration-300 ease-in-out',
            sidebarOpen ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none',
          ].join(' ')}
        />
        <Sidebar
          open={sidebarOpen}
          collapsed={sidebarCollapsed}
          onClose={() => setSidebarOpen(false)}
          onToggleCollapse={toggleCollapse}
        />
        {/* Content column — panels are anchored here so they never overlap the sidebar */}
        <div className="relative flex flex-1 flex-col overflow-hidden">
          {/* Zero-height overlay anchor: lets absolute panels float over content without shifting layout */}
          {activePanel && (
            <div className="relative h-0 overflow-visible">
              {activePanel === 'search' && (
                <SearchPanel onClose={() => setActivePanel(null)} />
              )}
              {activePanel === 'notifications' && (
                <NotificationsPanel
                  notifications={notifications}
                  onDismiss={dismiss}
                  onDismissAll={dismissAll}
                  onClose={() => setActivePanel(null)}
                />
              )}
            </div>
          )}
          <main
            ref={mainRef}
            className="flex-1 overflow-y-auto bg-pds-bg p-5 sm:p-7"
          >
            <ErrorBoundary key={pathname}>
              <Outlet />
            </ErrorBoundary>
          </main>
        </div>
      </div>
    </div>
  );
}
