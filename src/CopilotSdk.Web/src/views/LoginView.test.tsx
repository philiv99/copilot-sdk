/**
 * Tests for the LoginView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { LoginView } from './LoginView';
import { UserProvider } from '../context/UserContext';

// Mock navigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

// Use jest.fn() exported via module factory to avoid hoisting issues
const mockLoginFn = jest.fn();
jest.mock('../api/userApi', () => ({
  getStoredUserId: jest.fn().mockReturnValue(null),
  setStoredUserId: jest.fn(),
  getCurrentUser: jest.fn().mockRejectedValue(new Error('Not logged in')),
  login: (...args: any[]) => mockLoginFn(...args),
  logout: jest.fn(),
}));

const renderLoginView = () => {
  return render(
    <BrowserRouter>
      <UserProvider>
        <LoginView />
      </UserProvider>
    </BrowserRouter>
  );
};

describe('LoginView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the login form', () => {
    renderLoginView();
    expect(screen.getByTestId('login-view')).toBeInTheDocument();
    expect(screen.getByTestId('login-form')).toBeInTheDocument();
    expect(screen.getByText('Sign in to your account')).toBeInTheDocument();
  });

  it('renders username and password inputs', () => {
    renderLoginView();
    expect(screen.getByTestId('login-username')).toBeInTheDocument();
    expect(screen.getByTestId('login-password')).toBeInTheDocument();
  });

  it('renders submit button', () => {
    renderLoginView();
    expect(screen.getByTestId('login-submit')).toBeInTheDocument();
    expect(screen.getByTestId('login-submit')).toHaveTextContent('Sign In');
  });

  it('renders links to register and forgot credentials', () => {
    renderLoginView();
    expect(screen.getByTestId('register-link')).toBeInTheDocument();
    expect(screen.getByTestId('forgot-link')).toBeInTheDocument();
  });

  it('disables submit when fields are empty', () => {
    renderLoginView();
    expect(screen.getByTestId('login-submit')).toBeDisabled();
  });

  it('enables submit when both fields are filled', () => {
    renderLoginView();
    fireEvent.change(screen.getByTestId('login-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('login-password'), { target: { value: 'password123' } });
    expect(screen.getByTestId('login-submit')).not.toBeDisabled();
  });

  it('calls login and navigates on success', async () => {
    mockLoginFn.mockResolvedValueOnce({
      success: true,
      message: 'OK',
      user: { id: '1', username: 'testuser', displayName: 'Test', role: 'Player', email: 'test@test.com', avatarType: 'Default', isActive: true, createdAt: '', updatedAt: '' },
    });

    renderLoginView();
    fireEvent.change(screen.getByTestId('login-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('login-password'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByTestId('login-submit'));

    await waitFor(() => {
      expect(mockLoginFn).toHaveBeenCalledWith({ username: 'testuser', password: 'password123' });
    });
  });

  it('shows error on failed login', async () => {
    mockLoginFn.mockResolvedValueOnce({
      success: false,
      message: 'Invalid credentials.',
      user: null,
    });

    renderLoginView();
    fireEvent.change(screen.getByTestId('login-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('login-password'), { target: { value: 'wrong' } });
    fireEvent.click(screen.getByTestId('login-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('login-error')).toBeInTheDocument();
    });
  });

  it('has proper accessibility attributes', () => {
    renderLoginView();
    expect(screen.getByLabelText('Username')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });
});
