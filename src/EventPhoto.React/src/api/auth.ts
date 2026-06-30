import { apiClient } from './client';
import type { ApiResponse, AuthUser, LoginRequest, LoginResponse } from '../types';

export const authApi = {
  async login(data: LoginRequest) {
    const response = await apiClient.post<ApiResponse<LoginResponse>>('/auth/login', data);
    return response.data;
  },
  async me() {
    const response = await apiClient.get<ApiResponse<AuthUser>>('/auth/me');
    return response.data;
  },
  async changePassword(data: { currentPassword: string; newPassword: string; confirmNewPassword: string }) {
    const response = await apiClient.post<ApiResponse<null>>('/auth/change-password', data);
    return response.data;
  },
};
