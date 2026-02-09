/**
 * Tests for Auth components: UserAvatar, RoleBadge, UserMenu, ProtectedRoute, AvatarPicker.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { UserAvatar } from './UserAvatar';
import { RoleBadge } from './RoleBadge';
import { AvatarPicker } from './AvatarPicker';
import { ProtectedRoute } from './ProtectedRoute';

// #region UserAvatar Tests

describe('UserAvatar', () => {
  it('renders default avatar', () => {
    render(<UserAvatar avatarType="Default" displayName="Test" />);
    expect(screen.getByTestId('user-avatar')).toBeInTheDocument();
    expect(screen.getByText('👤')).toBeInTheDocument();
  });

  it('renders preset avatar', () => {
    render(<UserAvatar avatarType="Preset" avatarData="robot" displayName="Test" />);
    expect(screen.getByText('🤖')).toBeInTheDocument();
  });

  it('renders custom avatar as image', () => {
    render(<UserAvatar avatarType="Custom" avatarData="base64data" displayName="Test" />);
    const img = screen.getByAltText('Test');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', expect.stringContaining('base64data'));
  });

  it('renders with small size', () => {
    render(<UserAvatar avatarType="Default" displayName="Test" size="small" />);
    const avatar = screen.getByTestId('user-avatar');
    expect(avatar).toHaveClass('user-avatar--small');
  });

  it('renders with large size', () => {
    render(<UserAvatar avatarType="Default" displayName="Test" size="large" />);
    const avatar = screen.getByTestId('user-avatar');
    expect(avatar).toHaveClass('user-avatar--large');
  });

  it('falls back to default for unknown preset', () => {
    render(<UserAvatar avatarType="Preset" avatarData="nonexistent" displayName="Test" />);
    expect(screen.getByText('👤')).toBeInTheDocument();
  });
});

// #endregion

// #region RoleBadge Tests

describe('RoleBadge', () => {
  it('renders admin badge', () => {
    render(<RoleBadge role="Admin" />);
    expect(screen.getByTestId('role-badge')).toBeInTheDocument();
    expect(screen.getByText('Admin')).toBeInTheDocument();
  });

  it('renders creator badge', () => {
    render(<RoleBadge role="Creator" />);
    expect(screen.getByText('Creator')).toBeInTheDocument();
  });

  it('renders player badge', () => {
    render(<RoleBadge role="Player" />);
    expect(screen.getByText('Player')).toBeInTheDocument();
  });

  it('renders compact mode', () => {
    render(<RoleBadge role="Admin" compact />);
    expect(screen.getByText('A')).toBeInTheDocument();
  });

  it('has admin class for admin role', () => {
    render(<RoleBadge role="Admin" />);
    expect(screen.getByTestId('role-badge')).toHaveClass('role-badge--admin');
  });

  it('has creator class for creator role', () => {
    render(<RoleBadge role="Creator" />);
    expect(screen.getByTestId('role-badge')).toHaveClass('role-badge--creator');
  });

  it('has player class for player role', () => {
    render(<RoleBadge role="Player" />);
    expect(screen.getByTestId('role-badge')).toHaveClass('role-badge--player');
  });

  it('has aria-label', () => {
    render(<RoleBadge role="Admin" />);
    expect(screen.getByTestId('role-badge')).toHaveAttribute('aria-label', 'Role: Admin');
  });
});

// #endregion

// #region AvatarPicker Tests

describe('AvatarPicker', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the avatar picker', () => {
    render(<AvatarPicker selectedType="Default" onChange={mockOnChange} />);
    expect(screen.getByTestId('avatar-picker')).toBeInTheDocument();
  });

  it('renders preset avatars', () => {
    render(<AvatarPicker selectedType="Default" onChange={mockOnChange} />);
    // Should show preset buttons
    const buttons = screen.getAllByRole('radio');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('calls onChange when a preset is selected', () => {
    render(<AvatarPicker selectedType="Default" onChange={mockOnChange} />);
    const presetButtons = screen.getAllByRole('radio');
    fireEvent.click(presetButtons[2]); // Click a non-default preset
    expect(mockOnChange).toHaveBeenCalled();
  });

  it('renders upload button', () => {
    render(<AvatarPicker selectedType="Default" onChange={mockOnChange} />);
    expect(screen.getByTestId('upload-avatar-btn')).toBeInTheDocument();
  });

  it('highlights selected preset', () => {
    render(<AvatarPicker selectedType="Preset" selectedData="robot" onChange={mockOnChange} />);
    // The selected preset should have active class
    const buttons = screen.getAllByRole('radio');
    const selectedButton = buttons.find(btn => btn.getAttribute('aria-checked') === 'true');
    expect(selectedButton).toBeTruthy();
  });
});

// #endregion

// #region ProtectedRoute Tests

const mockUseUser = jest.fn();
jest.mock('../../context/UserContext', () => ({
  useUser: () => mockUseUser(),
}));

describe('ProtectedRoute', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows loading when not initialized', () => {
    mockUseUser.mockReturnValue({
      isAuthenticated: false,
      hasRole: jest.fn(),
      state: { isInitialized: false },
    });

    render(
      <BrowserRouter>
        <ProtectedRoute><div>Protected Content</div></ProtectedRoute>
      </BrowserRouter>
    );
    expect(screen.getByTestId('auth-loading')).toBeInTheDocument();
  });

  it('redirects to login when not authenticated', () => {
    mockUseUser.mockReturnValue({
      isAuthenticated: false,
      hasRole: jest.fn(),
      state: { isInitialized: true },
    });

    render(
      <BrowserRouter>
        <ProtectedRoute><div>Protected Content</div></ProtectedRoute>
      </BrowserRouter>
    );
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders children when authenticated', () => {
    mockUseUser.mockReturnValue({
      isAuthenticated: true,
      hasRole: jest.fn().mockReturnValue(true),
      state: { isInitialized: true },
    });

    render(
      <BrowserRouter>
        <ProtectedRoute><div>Protected Content</div></ProtectedRoute>
      </BrowserRouter>
    );
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('shows forbidden when role check fails', () => {
    mockUseUser.mockReturnValue({
      isAuthenticated: true,
      hasRole: jest.fn().mockReturnValue(false),
      state: { isInitialized: true },
    });

    render(
      <BrowserRouter>
        <ProtectedRoute requiredRole="Admin"><div>Admin Content</div></ProtectedRoute>
      </BrowserRouter>
    );
    expect(screen.getByTestId('forbidden-page')).toBeInTheDocument();
    expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
  });

  it('renders children when role check passes', () => {
    mockUseUser.mockReturnValue({
      isAuthenticated: true,
      hasRole: jest.fn().mockReturnValue(true),
      state: { isInitialized: true },
    });

    render(
      <BrowserRouter>
        <ProtectedRoute requiredRole="Admin"><div>Admin Content</div></ProtectedRoute>
      </BrowserRouter>
    );
    expect(screen.getByText('Admin Content')).toBeInTheDocument();
  });
});

// #endregion
