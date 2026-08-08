import { QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminLayout } from './components/Layout/AdminLayout';
import { GuestLayout } from './components/Layout/GuestLayout';
import Dashboard from './pages/Dashboard';
import EventWorkspacePage from './pages/Events/EventWorkspacePage';
import EventForm from './pages/Events/EventForm';
import EventList from './pages/Events/EventList';
import Gallery from './pages/Gallery';
import FaceSearchPage from './pages/FaceSearchPage';
import SearchProgressPage from './pages/SearchProgressPage';
import MyPhotosGalleryPage from './pages/MyPhotosGalleryPage';
import Login from './pages/Login';
import Settings from './pages/Settings';
import Statistics from './pages/Statistics';
import Logs from './pages/Logs';
import HealthMonitoring from './pages/HealthMonitoring';
import SystemSettings from './pages/SystemSettings';
import { apiError } from './utils/errorHandler';

const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      // Only surface the toast when there is no stale data to fall back on.
      // Background refetch failures while stale data is displayed are silent.
      if (query.state.data === undefined) {
        apiError(error);
      }
    },
  }),
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      throwOnError: false,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
        <Toaster
          position="top-left"
          gutter={8}
          toastOptions={{
            duration: 5000,
            style: {
              background: '#0f172a',
              color: '#f1f5f9',
              border: '1px solid #1e293b',
              borderRadius: '12px',
              fontSize: '13px',
              fontWeight: 500,
              padding: '10px 16px',
              boxShadow: '0 8px 32px rgba(0,0,0,0.5)',
              maxWidth: '380px',
            },
            success: {
              iconTheme: { primary: '#22c55e', secondary: '#0f172a' },
            },
            error: {
              style: {
                background: '#0f172a',
                border: '1px solid #dc2626',
              },
              iconTheme: { primary: '#ef4444', secondary: '#0f172a' },
            },
          }}
        />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route element={<GuestLayout />}>
            {/* Standard gallery browse */}
            <Route path="/gallery/:eventId" element={<Gallery />} />
            {/* Face search landing — smart redirect based on gallery mode */}
            <Route path="/gallery/:eventId/find" element={<FaceSearchPage />} />
            {/* Face search in progress */}
            <Route path="/gallery/:eventId/search/:sessionToken" element={<SearchProgressPage />} />
            {/* Personal matched gallery */}
            <Route path="/gallery/:eventId/results/:sessionToken" element={<MyPhotosGalleryPage />} />
          </Route>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Dashboard />} />
            <Route path="events" element={<EventList />} />
            <Route path="events/new" element={<EventForm />} />
            <Route path="events/:eventId" element={<EventWorkspacePage />} />
            <Route path="statistics" element={<Statistics />} />
            <Route path="logs" element={<Logs />} />
            <Route path="health" element={<HealthMonitoring />} />
            <Route path="settings" element={<Settings />} />
            <Route path="system-settings" element={<SystemSettings />} />
          </Route>
          <Route path="/" element={<Navigate to="/admin" replace />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
