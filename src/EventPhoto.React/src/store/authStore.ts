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
    return Boolean(localStorage.getItem(TOKEN_KEY));
  },
};
