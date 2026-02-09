/**
 * Tests for the UserContext / UserProvider.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { UserProvider, useUser } from './UserContext';

// Mock the userApi module
const mockGetStoredUserId = jest.fn();
const mockSetStoredUserId = jest.fn();
const mockGetCurrentUser = jest.fn();
const mockLoginApi = jest.fn();
const mockLogoutApi = jest.fn();
const mockRegisterApi = jest.fn();
const mockUpdateProfileApi = jest.fn();
const mockChangePasswordApi = jest.fn();

jest.mock('../api/userApi', () => ({
  getStoredUserId: (...args: any[]) => mockGetStoredUserId(...args),
  setStoredUserId: (...args: any[]) => mockSetStoredUserId(...args),
  getCurrentUser: (...args: any[]) => mockGetCurrentUser(...args),
  login: (...args: any[]) => mockLoginApi(...args),
  logout: (...args: any[]) => mockLogoutApi(...args),
  register: (...args: any[]) => mockRegisterApi(...args),
  updateProfile: (...args: any[]) => mockUpdateProfileApi(...args),
  changePassword: (...args: any[]) => mockChangePasswordApi(...args),
}));

const mockUser = {
  id: '1', username: 'testuser', email: 'test@test.com', displayName: 'Test User',
  role: 'Player' as const, avatarType: 'Default' as const, avatarData: null,
  isActive: true, createdAt: '', updatedAt: '',
};

/** Helper component that exposes context values for testing. */
function TestConsumer() {
  const { state, isAuthenticated, isAdmin, isCreatorOrAdmin, hasRole, login, logout, register } = useUser();
  return (
    <div>
      <span data-testid="is-authenticated">{String(isAuthenticated)}</span>
      <span data-testid="is-admin">{String(isAdmin)}</span>
      <span data-testid="is-creator-or-admin">{String(isCreatorOrAdmin)}</span>
      <span data-testid="is-initialized">{String(state.isInitialized)}</span>
      <span data-testid="is-loading">{String(state.isLoading)}</span>
      <span data-testid="error">{state.error || ''}</span>
      <span data-testid="display-name">{state.currentUser?.displayName || ''}</span>
      <span data-testid="has-admin-role">{String(hasRole('Admin'))}</span>
      <button data-testid="login-btn" onClick={() => login({ username: 'test', password: 'pass' })}>Login</button>
      <button data-testid="logout-btn" onClick={() => logout()}>Logout</button>
      <button data-testid="register-btn" onClick={() => register({
        username: 'new', email: 'new@test.com', displayName: 'New',
        password: 'pass123', confirmPassword: 'pass123',
      })}>Register</button>
    </div>
  );
}

const renderWithProvider = () => {
  return render(
    <UserProvider>
      <TestConsumer />
    </UserProvider>
  );
};

describe('UserContext', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockGetStoredUserId.mockReturnValue(null);
    mockLogoutApi.mockResolvedValue(undefined);
  });

  it('initializes with no user when no stored ID', async () => {
    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-initialized')).toHaveTextContent('true');
    });
    expect(screen.getByTestId('is-authenticated')).toHaveTextContent('false');
  });

  it('loads user from stored ID on mount', async () => {
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockResolvedValue(mockUser);
    
    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('display-name')).toHaveTextContent('Test User');
    });
  });

  it('clears stored ID if getCurrentUser fails', async () => {
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockRejectedValue(new Error('Not found'));

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-initialized')).toHaveTextContent('true');
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('false');
    });
    expect(mockSetStoredUserId).toHaveBeenCalledWith(null);
  });

  it('logs in successfully', async () => {
    mockLoginApi.mockResolvedValue({ success: true, message: 'OK', user: mockUser });
    
    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-initialized')).toHaveTextContent('true');
    });

    await act(async () => {
      fireEvent.click(screen.getByTestId('login-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('display-name')).toHaveTextContent('Test User');
    });
  });

  it('shows error on failed login', async () => {
    mockLoginApi.mockResolvedValue({ success: false, message: 'Invalid credentials.', user: null });
    
    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-initialized')).toHaveTextContent('true');
    });

    await act(async () => {
      fireEvent.click(screen.getByTestId('login-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('error')).toHaveTextContent('Invalid credentials');
    });
  });

  it('logs out successfully', async () => {
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockResolvedValue(mockUser);

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('true');
    });

    await act(async () => {
      fireEvent.click(screen.getByTestId('logout-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('false');
    });
  });

  it('registers successfully and auto-logs in', async () => {
    mockRegisterApi.mockResolvedValue({ ...mockUser, id: '2', username: 'new' });

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-initialized')).toHaveTextContent('true');
    });

    await act(async () => {
      fireEvent.click(screen.getByTestId('register-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated')).toHaveTextContent('true');
    });
    expect(mockSetStoredUserId).toHaveBeenCalledWith('2');
  });

  it('computes isAdmin correctly for admin user', async () => {
    const adminUser = { ...mockUser, role: 'Admin' as const };
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockResolvedValue(adminUser);

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-admin')).toHaveTextContent('true');
      expect(screen.getByTestId('has-admin-role')).toHaveTextContent('true');
    });
  });

  it('computes isCreatorOrAdmin for creator user', async () => {
    const creatorUser = { ...mockUser, role: 'Creator' as const };
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockResolvedValue(creatorUser);

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('is-creator-or-admin')).toHaveTextContent('true');
      expect(screen.getByTestId('is-admin')).toHaveTextContent('false');
    });
  });

  it('hasRole returns false for insufficient role', async () => {
    mockGetStoredUserId.mockReturnValue('1');
    mockGetCurrentUser.mockResolvedValue(mockUser); // Player role

    renderWithProvider();
    await waitFor(() => {
      expect(screen.getByTestId('has-admin-role')).toHaveTextContent('false');
    });
  });

  it('throws when useUser is called outside provider', () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => {
      render(<TestConsumer />);
    }).toThrow('useUser must be used within a UserProvider');
    consoleError.mockRestore();
  });
});
