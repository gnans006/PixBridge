import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminLayout } from './components/Layout/AdminLayout';
import { GuestLayout } from './components/Layout/GuestLayout';
import Dashboard from './pages/Dashboard';
import EventDetail from './pages/Events/EventDetail';
import EventForm from './pages/Events/EventForm';
import EventList from './pages/Events/EventList';
import Gallery from './pages/Gallery';
import Login from './pages/Login';
import Settings from './pages/Settings';
import Statistics from './pages/Statistics';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Toaster position="top-right" toastOptions={{ duration: 3000 }} />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route element={<GuestLayout />}>
            <Route path="/gallery/:eventId" element={<Gallery />} />
          </Route>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Dashboard />} />
            <Route path="events" element={<EventList />} />
            <Route path="events/new" element={<EventForm />} />
            <Route path="events/:eventId" element={<EventDetail />} />
            <Route path="statistics" element={<Statistics />} />
            <Route path="settings" element={<Settings />} />
          </Route>
          <Route path="/" element={<Navigate to="/admin" replace />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
