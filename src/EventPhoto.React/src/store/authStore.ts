import type { AuthUser, LoginResponse } from '../types';

const TOKEN_KEY = 'pixbridge_token';
const USER_KEY = 'pixbridge_user';

export const authStore = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },
  getUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      localStorage.removeItem(USER_KEY);
      return null;
    }
  },
  setAuth(response: LoginResponse) {
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({
        username: response.username,
        role: response.role,
      } satisfies AuthUser),
    );
  },
  clearAuth() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
  isAuthenticated(): boolean {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return false;
    try {
      // Decode payload (second segment) without a library — just check exp claim
      const payload = JSON.parse(atob(token.split('.')[1])) as { exp?: number };
      if (typeof payload.exp === 'number' && payload.exp * 1000 < Date.now()) {
        // Token has expired — clean up immediately
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        return false;
      }
      return true;
    } catch {
      // Malformed token
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      return false;
    }
  },
};
