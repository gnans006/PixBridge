import { useCallback, useState } from 'react';
import { authApi } from '../api/auth';
import { authStore } from '../store/authStore';
import type { LoginRequest } from '../types';

export function useAuth() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = useCallback(async (data: LoginRequest) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.login(data);
      if (response.success && response.data) {
        authStore.setAuth(response.data);
        return true;
      }
      // Backend returned a structured failure (wrong credentials, inactive account, etc.)
      setError(response.error ?? 'Incorrect username or password. Please try again.');
      return false;
    } catch (err: unknown) {
      // Distinguish network/server errors from auth errors
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        setError('Incorrect username or password. Please try again.');
      } else if (status === 429) {
        setError('Too many login attempts. Please wait a moment and try again.');
      } else if (status === 0 || status === undefined) {
        setError('Cannot reach the server. Check your network connection.');
      } else {
        setError('An unexpected error occurred. Please try again.');
      }
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    authStore.clearAuth();
    window.location.assign('/login');
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return {
    login,
    logout,
    clearError,
    isLoading,
    error,
    isAuthenticated: authStore.isAuthenticated(),
    user: authStore.getUser(),
  };
}
