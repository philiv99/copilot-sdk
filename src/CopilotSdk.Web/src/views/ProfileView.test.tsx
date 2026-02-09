/**
 * Tests for the ProfileView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ProfileView } from './ProfileView';

const mockUser = {
  id: '1',
  username: 'testuser',
  email: 'test@test.com',
  displayName: 'Test User',
  role: 'Player' as const,
  avatarType: 'Default' as const,
  avatarData: null,
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  lastLoginAt: '2024-01-15T12:00:00Z',
};

const mockUpdateProfile = jest.fn();
const mockChangePassword = jest.fn();
const mockClearError = jest.fn();

jest.mock('../context/UserContext', () => ({
  useUser: () => ({
    state: { currentUser: mockUser, isLoading: false, isInitialized: true, error: null },
    updateProfile: mockUpdateProfile,
    changePassword: mockChangePassword,
    clearError: mockClearError,
    isAuthenticated: true,
    isAdmin: false,
    hasRole: jest.fn(),
  }),
}));

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

const renderProfileView = () => {
  return render(
    <BrowserRouter>
      <ProfileView />
    </BrowserRouter>
  );
};

describe('ProfileView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUpdateProfile.mockResolvedValue(true);
    mockChangePassword.mockResolvedValue(true);
  });

  it('renders the profile view', () => {
    renderProfileView();
    expect(screen.getByTestId('profile-view')).toBeInTheDocument();
    expect(screen.getByText('My Profile')).toBeInTheDocument();
  });

  it('displays user summary', () => {
    renderProfileView();
    expect(screen.getByTestId('profile-summary')).toBeInTheDocument();
    expect(screen.getByText('Test User')).toBeInTheDocument();
    // @testuser appears in multiple places; verify at least one exists
    const usernameElements = screen.getAllByText('@testuser');
    expect(usernameElements.length).toBeGreaterThan(0);
  });

  it('renders edit profile section', () => {
    renderProfileView();
    expect(screen.getByTestId('profile-edit-section')).toBeInTheDocument();
    expect(screen.getByTestId('profile-form')).toBeInTheDocument();
  });

  it('renders change password section', () => {
    renderProfileView();
    expect(screen.getByTestId('password-section')).toBeInTheDocument();
    expect(screen.getByTestId('password-form')).toBeInTheDocument();
  });

  it('renders account info section', () => {
    renderProfileView();
    expect(screen.getByTestId('account-info-section')).toBeInTheDocument();
  });

  it('pre-fills form fields with user data', () => {
    renderProfileView();
    expect(screen.getByTestId('profile-displayname')).toHaveValue('Test User');
    expect(screen.getByTestId('profile-email')).toHaveValue('test@test.com');
  });

  it('saves profile changes', async () => {
    renderProfileView();
    fireEvent.change(screen.getByTestId('profile-displayname'), { target: { value: 'Updated Name' } });
    fireEvent.click(screen.getByTestId('profile-save'));

    await waitFor(() => {
      expect(mockUpdateProfile).toHaveBeenCalledWith(expect.objectContaining({
        displayName: 'Updated Name',
      }));
    });
  });

  it('shows success after profile update', async () => {
    renderProfileView();
    fireEvent.click(screen.getByTestId('profile-save'));

    await waitFor(() => {
      expect(screen.getByTestId('profile-success')).toHaveTextContent('Profile updated');
    });
  });

  it('shows error for empty display name', async () => {
    renderProfileView();
    fireEvent.change(screen.getByTestId('profile-displayname'), { target: { value: '' } });
    fireEvent.click(screen.getByTestId('profile-save'));

    await waitFor(() => {
      expect(screen.getByTestId('profile-error')).toHaveTextContent('Display name is required');
    });
  });

  it('changes password successfully', async () => {
    renderProfileView();
    fireEvent.change(screen.getByTestId('current-password'), { target: { value: 'oldpass' } });
    fireEvent.change(screen.getByTestId('new-password'), { target: { value: 'newpass123' } });
    fireEvent.change(screen.getByTestId('confirm-new-password'), { target: { value: 'newpass123' } });
    fireEvent.click(screen.getByTestId('password-save'));

    await waitFor(() => {
      expect(mockChangePassword).toHaveBeenCalledWith({
        currentPassword: 'oldpass',
        newPassword: 'newpass123',
        confirmNewPassword: 'newpass123',
      });
    });
  });

  it('shows error for short new password', async () => {
    renderProfileView();
    fireEvent.change(screen.getByTestId('current-password'), { target: { value: 'oldpass' } });
    fireEvent.change(screen.getByTestId('new-password'), { target: { value: '12345' } });
    fireEvent.change(screen.getByTestId('confirm-new-password'), { target: { value: '12345' } });
    fireEvent.click(screen.getByTestId('password-save'));

    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toHaveTextContent('at least 6 characters');
    });
  });

  it('shows error for password mismatch', async () => {
    renderProfileView();
    fireEvent.change(screen.getByTestId('current-password'), { target: { value: 'oldpass' } });
    fireEvent.change(screen.getByTestId('new-password'), { target: { value: 'newpass123' } });
    fireEvent.change(screen.getByTestId('confirm-new-password'), { target: { value: 'different' } });
    fireEvent.click(screen.getByTestId('password-save'));

    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toHaveTextContent('do not match');
    });
  });

  it('navigates back when back button clicked', () => {
    renderProfileView();
    fireEvent.click(screen.getByTestId('profile-back'));
    expect(mockNavigate).toHaveBeenCalledWith('/');
  });

  it('displays role badge', () => {
    renderProfileView();
    const badges = screen.getAllByTestId('role-badge');
    expect(badges.length).toBeGreaterThan(0);
  });
});
