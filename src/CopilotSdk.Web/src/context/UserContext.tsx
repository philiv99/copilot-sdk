/**
 * React context for managing user authentication and profile state.
 */
import React, { createContext, useContext, useReducer, useCallback, useEffect, ReactNode } from 'react';
import {
  UserResponse,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  ChangePasswordRequest,
  UserRole,
} from '../types';
import * as userApiModule from '../api/userApi';

// #region State Types

/** State for the user context. */
interface UserState {
  /** Currently authenticated user. */
  currentUser: UserResponse | null;
  /** Whether auth state is being loaded. */
  isLoading: boolean;
  /** Whether initial auth check is complete. */
  isInitialized: boolean;
  /** Last error that occurred. */
  error: string | null;
}

// #endregion

// #region Actions

type UserAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_USER'; payload: UserResponse | null }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_INITIALIZED' }
  | { type: 'CLEAR_ERROR' };

// #endregion

// #region Reducer

const initialState: UserState = {
  currentUser: null,
  isLoading: false,
  isInitialized: false,
  error: null,
};

function userReducer(state: UserState, action: UserAction): UserState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    case 'SET_USER':
      return { ...state, currentUser: action.payload, error: null };
    case 'SET_ERROR':
      return { ...state, error: action.payload, isLoading: false };
    case 'SET_INITIALIZED':
      return { ...state, isInitialized: true, isLoading: false };
    case 'CLEAR_ERROR':
      return { ...state, error: null };
    default:
      return state;
  }
}

// #endregion

// #region Context

interface UserContextValue {
  /** Current user state. */
  state: UserState;
  /** Login with username and password. */
  login: (request: LoginRequest) => Promise<boolean>;
  /** Register a new account. */
  register: (request: RegisterRequest) => Promise<boolean>;
  /** Logout the current user. */
  logout: () => Promise<void>;
  /** Update own profile. */
  updateProfile: (request: UpdateProfileRequest) => Promise<boolean>;
  /** Change own password. */
  changePassword: (request: ChangePasswordRequest) => Promise<boolean>;
  /** Refresh current user data. */
  refreshUser: () => Promise<void>;
  /** Clear the current error. */
  clearError: () => void;
  /** Check if user has at least the given role. */
  hasRole: (role: UserRole) => boolean;
  /** Whether user is authenticated. */
  isAuthenticated: boolean;
  /** Whether user is admin. */
  isAdmin: boolean;
  /** Whether user is creator or admin. */
  isCreatorOrAdmin: boolean;
}

const UserContext = createContext<UserContextValue | null>(null);

// #endregion

// #region Provider

interface UserProviderProps {
  children: ReactNode;
}

const roleHierarchy: Record<UserRole, number> = {
  Player: 0,
  Creator: 1,
  Admin: 2,
};

export function UserProvider({ children }: UserProviderProps) {
  const [state, dispatch] = useReducer(userReducer, initialState);

  // Check for existing session on mount
  useEffect(() => {
    const checkAuth = async () => {
      const storedUserId = userApiModule.getStoredUserId();
      if (storedUserId) {
        dispatch({ type: 'SET_LOADING', payload: true });
        try {
          const user = await userApiModule.getCurrentUser();
          dispatch({ type: 'SET_USER', payload: user });
        } catch {
          userApiModule.setStoredUserId(null);
          dispatch({ type: 'SET_USER', payload: null });
        }
      }
      dispatch({ type: 'SET_INITIALIZED' });
    };
    checkAuth();
  }, []);

  const login = useCallback(async (request: LoginRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true });
    dispatch({ type: 'CLEAR_ERROR' });
    try {
      const response = await userApiModule.login(request);
      if (response.success && response.user) {
        dispatch({ type: 'SET_USER', payload: response.user });
        dispatch({ type: 'SET_LOADING', payload: false });
        return true;
      }
      dispatch({ type: 'SET_ERROR', payload: response.message || 'Login failed.' });
      return false;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Login failed.';
      dispatch({ type: 'SET_ERROR', payload: message });
      return false;
    }
  }, []);

  const register = useCallback(async (request: RegisterRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true });
    dispatch({ type: 'CLEAR_ERROR' });
    try {
      const user = await userApiModule.register(request);
      // Auto-login after registration
      userApiModule.setStoredUserId(user.id);
      dispatch({ type: 'SET_USER', payload: user });
      dispatch({ type: 'SET_LOADING', payload: false });
      return true;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Registration failed.';
      dispatch({ type: 'SET_ERROR', payload: message });
      return false;
    }
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    await userApiModule.logout();
    dispatch({ type: 'SET_USER', payload: null });
  }, []);

  const updateProfile = useCallback(async (request: UpdateProfileRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true });
    try {
      const updated = await userApiModule.updateProfile(request);
      dispatch({ type: 'SET_USER', payload: updated });
      dispatch({ type: 'SET_LOADING', payload: false });
      return true;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Update failed.';
      dispatch({ type: 'SET_ERROR', payload: message });
      return false;
    }
  }, []);

  const changePassword = useCallback(async (request: ChangePasswordRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true });
    try {
      await userApiModule.changePassword(request);
      dispatch({ type: 'SET_LOADING', payload: false });
      return true;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Password change failed.';
      dispatch({ type: 'SET_ERROR', payload: message });
      return false;
    }
  }, []);

  const refreshUser = useCallback(async (): Promise<void> => {
    try {
      const user = await userApiModule.getCurrentUser();
      dispatch({ type: 'SET_USER', payload: user });
    } catch {
      // Silently fail
    }
  }, []);

  const clearError = useCallback(() => {
    dispatch({ type: 'CLEAR_ERROR' });
  }, []);

  const hasRole = useCallback((role: UserRole): boolean => {
    if (!state.currentUser) return false;
    return roleHierarchy[state.currentUser.role] >= roleHierarchy[role];
  }, [state.currentUser]);

  const value: UserContextValue = {
    state,
    login,
    register,
    logout,
    updateProfile,
    changePassword,
    refreshUser,
    clearError,
    hasRole,
    isAuthenticated: !!state.currentUser,
    isAdmin: state.currentUser?.role === 'Admin',
    isCreatorOrAdmin: state.currentUser?.role === 'Admin' || state.currentUser?.role === 'Creator',
  };

  return (
    <UserContext.Provider value={value}>
      {children}
    </UserContext.Provider>
  );
}

// #endregion

// #region Hook

/**
 * Hook to access user context.
 */
export function useUser(): UserContextValue {
  const context = useContext(UserContext);
  if (!context) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return context;
}

// #endregion
