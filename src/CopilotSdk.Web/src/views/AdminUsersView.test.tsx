/**
 * Tests for the AdminUsersView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AdminUsersView } from './AdminUsersView';

const mockUsers = [
  {
    id: '1', username: 'admin', email: 'admin@test.com', displayName: 'Admin User',
    role: 'Admin' as const, avatarType: 'Default' as const, avatarData: null,
    isActive: true, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z', lastLoginAt: null,
  },
  {
    id: '2', username: 'player1', email: 'player@test.com', displayName: 'Player One',
    role: 'Player' as const, avatarType: 'Preset' as const, avatarData: 'robot',
    isActive: true, createdAt: '2024-01-05T00:00:00Z', updatedAt: '2024-01-05T00:00:00Z', lastLoginAt: null,
  },
  {
    id: '3', username: 'inactive', email: 'inactive@test.com', displayName: 'Inactive User',
    role: 'Creator' as const, avatarType: 'Default' as const, avatarData: null,
    isActive: false, createdAt: '2024-01-10T00:00:00Z', updatedAt: '2024-01-10T00:00:00Z', lastLoginAt: null,
  },
];

const mockIsAdmin = jest.fn().mockReturnValue(true);

jest.mock('../context/UserContext', () => ({
  useUser: () => ({
    state: { currentUser: { id: '1', role: 'Admin' }, isInitialized: true, isLoading: false, error: null },
    isAuthenticated: true,
    isAdmin: mockIsAdmin(),
    isCreatorOrAdmin: true,
    hasRole: jest.fn().mockReturnValue(true),
  }),
}));

const mockGetAllUsers = jest.fn();
const mockAdminUpdateUser = jest.fn();
const mockDeactivateUser = jest.fn();
const mockActivateUser = jest.fn();
const mockResetPassword = jest.fn();

jest.mock('../api/userApi', () => ({
  getAllUsers: (...args: any[]) => mockGetAllUsers(...args),
  adminUpdateUser: (...args: any[]) => mockAdminUpdateUser(...args),
  deactivateUser: (...args: any[]) => mockDeactivateUser(...args),
  activateUser: (...args: any[]) => mockActivateUser(...args),
  resetPassword: (...args: any[]) => mockResetPassword(...args),
}));

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

const renderAdminView = () => {
  return render(
    <BrowserRouter>
      <AdminUsersView />
    </BrowserRouter>
  );
};

describe('AdminUsersView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockIsAdmin.mockReturnValue(true);
    mockGetAllUsers.mockResolvedValue({ users: mockUsers, totalCount: 3 });
  });

  it('renders the admin view', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-users-view')).toBeInTheDocument();
    });
  });

  it('displays loading state', () => {
    mockGetAllUsers.mockReturnValue(new Promise(() => {})); // never resolves
    renderAdminView();
    expect(screen.getByTestId('admin-loading')).toBeInTheDocument();
  });

  it('loads and displays users in table', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-users-table')).toBeInTheDocument();
    });
    expect(screen.getByText('Admin User')).toBeInTheDocument();
    expect(screen.getByText('Player One')).toBeInTheDocument();
    expect(screen.getByText('Inactive User')).toBeInTheDocument();
  });

  it('shows total user count', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByText('Total: 3 users')).toBeInTheDocument();
    });
  });

  it('renders refresh button', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-refresh')).toBeInTheDocument();
    });
  });

  it('refreshes users when refresh is clicked', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-refresh')).toBeInTheDocument();
    });
    
    mockGetAllUsers.mockResolvedValue({ users: mockUsers, totalCount: 3 });
    fireEvent.click(screen.getByTestId('admin-refresh'));
    
    await waitFor(() => {
      expect(mockGetAllUsers).toHaveBeenCalledTimes(2);
    });
  });

  it('shows active/inactive status badges', async () => {
    renderAdminView();
    await waitFor(() => {
      const activeElements = screen.getAllByText('Active');
      expect(activeElements.length).toBe(2);
      expect(screen.getByText('Inactive')).toBeInTheDocument();
    });
  });

  it('toggles user active status (deactivate)', async () => {
    mockDeactivateUser.mockResolvedValue(undefined);
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('toggle-active-1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('toggle-active-1'));

    await waitFor(() => {
      expect(mockDeactivateUser).toHaveBeenCalledWith('1');
    });
  });

  it('toggles user active status (activate)', async () => {
    mockActivateUser.mockResolvedValue(undefined);
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('toggle-active-3')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('toggle-active-3'));

    await waitFor(() => {
      expect(mockActivateUser).toHaveBeenCalledWith('3');
    });
  });

  it('shows role badge for each user', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('role-badge-1')).toBeInTheDocument();
      expect(screen.getByTestId('role-badge-2')).toBeInTheDocument();
    });
  });

  it('shows confirm dialog for password reset', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('reset-password-2')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('reset-password-2'));

    expect(screen.getByTestId('confirm-reset-2')).toBeInTheDocument();
    expect(screen.getByText('Reset?')).toBeInTheDocument();
  });

  it('resets password on confirmation', async () => {
    mockResetPassword.mockResolvedValue({ message: 'Reset', temporaryPassword: 'TempPass123' });
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('reset-password-2')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('reset-password-2'));
    fireEvent.click(screen.getByTestId('confirm-reset-2'));

    await waitFor(() => {
      expect(mockResetPassword).toHaveBeenCalledWith('2');
      expect(screen.getByTestId('admin-temp-password')).toBeInTheDocument();
    });
  });

  it('shows error on API failure', async () => {
    mockGetAllUsers.mockRejectedValueOnce(new Error('Server error'));
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-error')).toBeInTheDocument();
    });
  });

  it('shows access denied for non-admin', () => {
    mockIsAdmin.mockReturnValue(false);
    renderAdminView();
    expect(screen.getByTestId('admin-forbidden')).toBeInTheDocument();
    expect(screen.getByText('Access Denied')).toBeInTheDocument();
  });

  it('navigates back when back button clicked', async () => {
    renderAdminView();
    await waitFor(() => {
      expect(screen.getByTestId('admin-back')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('admin-back'));
    expect(mockNavigate).toHaveBeenCalledWith('/');
  });
});
