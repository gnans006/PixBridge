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

      setError(response.error ?? 'Login failed.');
      return false;
    } catch {
      setError('An unexpected error occurred.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    authStore.clearAuth();
    window.location.assign('/login');
  }, []);

  return {
    login,
    logout,
    isLoading,
    error,
    isAuthenticated: authStore.isAuthenticated(),
    user: authStore.getUser(),
  };
}
