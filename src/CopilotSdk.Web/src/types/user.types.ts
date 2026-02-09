/**
 * TypeScript types for user management.
 */

/** User roles in the application. */
export type UserRole = 'Admin' | 'Creator' | 'Player';

/** Avatar type options. */
export type AvatarType = 'Default' | 'Preset' | 'Custom';

/** User response from the API. */
export interface UserResponse {
  id: string;
  username: string;
  email: string;
  displayName: string;
  role: UserRole;
  avatarType: AvatarType;
  avatarData?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string | null;
}

/** Login request. */
export interface LoginRequest {
  username: string;
  password: string;
}

/** Login response. */
export interface LoginResponse {
  success: boolean;
  message: string;
  user?: UserResponse | null;
}

/** Registration request. */
export interface RegisterRequest {
  username: string;
  email: string;
  displayName: string;
  password: string;
  confirmPassword: string;
  avatarType?: string;
  avatarData?: string;
}

/** Update profile request (self). */
export interface UpdateProfileRequest {
  displayName?: string | null;
  email?: string | null;
  avatarType?: string | null;
  avatarData?: string | null;
}

/** Admin update user request. */
export interface AdminUpdateUserRequest {
  displayName?: string | null;
  email?: string | null;
  role?: string | null;
  isActive?: boolean | null;
  avatarType?: string | null;
  avatarData?: string | null;
}

/** Change password request. */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

/** Forgot username request. */
export interface ForgotUsernameRequest {
  email: string;
}

/** Forgot password request. */
export interface ForgotPasswordRequest {
  usernameOrEmail: string;
}

/** User list response (admin). */
export interface UserListResponse {
  users: UserResponse[];
  totalCount: number;
}

/** Avatar preset item. */
export interface AvatarPresetItem {
  name: string;
  label: string;
  emoji: string;
}

/** Avatar presets response. */
export interface AvatarPresetsResponse {
  presets: AvatarPresetItem[];
}

/** Password reset result. */
export interface PasswordResetResponse {
  message: string;
  temporaryPassword: string;
}
