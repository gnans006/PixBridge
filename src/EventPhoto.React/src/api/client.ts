import axios from 'axios';
import { authStore } from '../store/authStore';

export const API_BASE = import.meta.env.VITE_API_BASE ?? '/api';

export function buildApiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${API_BASE}${path.startsWith('/') ? path : `/${path}`}`;
}

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = authStore.getToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLoginEndpoint = (error.config?.url as string | undefined)?.includes('/auth/login');
    if (error.response?.status === 401 && !isLoginEndpoint) {
      // Session expired or token invalid — clear and redirect.
      // Never redirect on the login endpoint itself (wrong credentials returns 401 too).
      authStore.clearAuth();
      window.location.assign('/login');
    }

    return Promise.reject(error);
  },
);
