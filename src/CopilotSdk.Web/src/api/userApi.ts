/**
 * API client for user management endpoints.
 */
import axios, { AxiosInstance, AxiosError } from 'axios';
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  UpdateProfileRequest,
  AdminUpdateUserRequest,
  ChangePasswordRequest,
  ForgotUsernameRequest,
  ForgotPasswordRequest,
  UserResponse,
  UserListResponse,
  AvatarPresetsResponse,
  PasswordResetResponse,
} from '../types/user.types';

/**
 * Base URL for the user API.
 */
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL
  ? process.env.REACT_APP_API_BASE_URL.replace('/api/copilot', '/api/users')
  : 'http://localhost:5139/api/users';

/**
 * Local storage key for user ID.
 */
const USER_ID_KEY = 'copilot-sdk-user-id';

/**
 * Get the stored user ID from localStorage.
 */
export function getStoredUserId(): string | null {
  return localStorage.getItem(USER_ID_KEY);
}

/**
 * Store the user ID in localStorage.
 */
export function setStoredUserId(userId: string | null): void {
  if (userId) {
    localStorage.setItem(USER_ID_KEY, userId);
  } else {
    localStorage.removeItem(USER_ID_KEY);
  }
}

/**
 * Axios instance for user API calls.
 */
const userClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor to add X-User-Id header to all requests
userClient.interceptors.request.use((config) => {
  const userId = getStoredUserId();
  if (userId) {
    config.headers['X-User-Id'] = userId;
  }
  return config;
});

/**
 * Extract error message from API response.
 */
function extractError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ error?: string; message?: string }>;
    if (axiosError.response?.data?.error) {
      return axiosError.response.data.error;
    }
    if (axiosError.response?.data?.message) {
      return axiosError.response.data.message;
    }
    return axiosError.message;
  }
  return 'An unexpected error occurred.';
}

// ─── Public API Functions ─────────────────────────────────────────────────────

/** Register a new user account. */
export async function register(request: RegisterRequest): Promise<UserResponse> {
  try {
    const response = await userClient.post<UserResponse>('/register', request);
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Login with username and password. */
export async function login(request: LoginRequest): Promise<LoginResponse> {
  try {
    const response = await userClient.post<LoginResponse>('/login', request);
    if (response.data.success && response.data.user) {
      setStoredUserId(response.data.user.id);
    }
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Logout the current user. */
export async function logout(): Promise<void> {
  try {
    await userClient.post('/logout');
  } catch {
    // Ignore errors on logout
  }
  setStoredUserId(null);
}

/** Get the current user's profile. */
export async function getCurrentUser(): Promise<UserResponse | null> {
  try {
    const response = await userClient.get<UserResponse>('/me');
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      return null;
    }
    throw new Error(extractError(error));
  }
}

/** Update the current user's profile. */
export async function updateProfile(request: UpdateProfileRequest): Promise<UserResponse> {
  try {
    const response = await userClient.put<UserResponse>('/me', request);
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Change the current user's password. */
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  try {
    await userClient.put('/me/password', request);
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Update the current user's avatar. */
export async function updateAvatar(avatarType: string, avatarData?: string | null): Promise<UserResponse> {
  try {
    const response = await userClient.put<UserResponse>('/me/avatar', { avatarType, avatarData });
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Get all users (admin only). */
export async function getAllUsers(activeOnly?: boolean): Promise<UserListResponse> {
  try {
    const params = activeOnly !== undefined ? { activeOnly } : {};
    const response = await userClient.get<UserListResponse>('/', { params });
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Get a user by ID (admin only). */
export async function getUserById(userId: string): Promise<UserResponse> {
  try {
    const response = await userClient.get<UserResponse>(`/${userId}`);
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Admin update a user. */
export async function adminUpdateUser(userId: string, request: AdminUpdateUserRequest): Promise<UserResponse> {
  try {
    const response = await userClient.put<UserResponse>(`/${userId}`, request);
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Deactivate a user (admin only). */
export async function deactivateUser(userId: string): Promise<void> {
  try {
    await userClient.delete(`/${userId}`);
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Activate a user (admin only). */
export async function activateUser(userId: string): Promise<void> {
  try {
    await userClient.post(`/${userId}/activate`);
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Reset a user's password (admin only). */
export async function resetPassword(userId: string): Promise<PasswordResetResponse> {
  try {
    const response = await userClient.post<PasswordResetResponse>(`/${userId}/reset-password`);
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Forgot username (stub). */
export async function forgotUsername(request: ForgotUsernameRequest): Promise<string> {
  try {
    const response = await userClient.post<{ message: string }>('/forgot-username', request);
    return response.data.message;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Forgot password (stub). */
export async function forgotPassword(request: ForgotPasswordRequest): Promise<string> {
  try {
    const response = await userClient.post<{ message: string }>('/forgot-password', request);
    return response.data.message;
  } catch (error) {
    throw new Error(extractError(error));
  }
}

/** Get available avatar presets. */
export async function getAvatarPresets(): Promise<AvatarPresetsResponse> {
  try {
    const response = await userClient.get<AvatarPresetsResponse>('/avatars/presets');
    return response.data;
  } catch (error) {
    throw new Error(extractError(error));
  }
}
