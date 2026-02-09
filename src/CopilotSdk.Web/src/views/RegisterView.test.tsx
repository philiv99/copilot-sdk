/**
 * Tests for the RegisterView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { RegisterView } from './RegisterView';
import { UserProvider } from '../context/UserContext';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

const mockRegisterFn = jest.fn();
jest.mock('../api/userApi', () => ({
  getStoredUserId: jest.fn().mockReturnValue(null),
  setStoredUserId: jest.fn(),
  getCurrentUser: jest.fn().mockRejectedValue(new Error('Not logged in')),
  register: (...args: any[]) => mockRegisterFn(...args),
  login: jest.fn(),
  logout: jest.fn(),
}));

const renderRegisterView = () => {
  return render(
    <BrowserRouter>
      <UserProvider>
        <RegisterView />
      </UserProvider>
    </BrowserRouter>
  );
};

describe('RegisterView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the registration form', () => {
    renderRegisterView();
    expect(screen.getByTestId('register-view')).toBeInTheDocument();
    expect(screen.getByTestId('register-form')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Create Account' })).toBeInTheDocument();
  });

  it('renders all input fields', () => {
    renderRegisterView();
    expect(screen.getByTestId('register-username')).toBeInTheDocument();
    expect(screen.getByTestId('register-email')).toBeInTheDocument();
    expect(screen.getByTestId('register-displayname')).toBeInTheDocument();
    expect(screen.getByTestId('register-password')).toBeInTheDocument();
    expect(screen.getByTestId('register-confirm-password')).toBeInTheDocument();
  });

  it('renders the avatar picker', () => {
    renderRegisterView();
    expect(screen.getByTestId('avatar-picker')).toBeInTheDocument();
  });

  it('renders link to login', () => {
    renderRegisterView();
    expect(screen.getByTestId('login-link')).toBeInTheDocument();
  });

  it('shows validation error for short username', async () => {
    renderRegisterView();
    fireEvent.change(screen.getByTestId('register-username'), { target: { value: 'ab' } });
    fireEvent.change(screen.getByTestId('register-email'), { target: { value: 'test@test.com' } });
    fireEvent.change(screen.getByTestId('register-displayname'), { target: { value: 'Test' } });
    fireEvent.change(screen.getByTestId('register-password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByTestId('register-confirm-password'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByTestId('register-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('register-error')).toHaveTextContent('at least 3 characters');
    });
  });

  it('shows validation error for invalid email', async () => {
    renderRegisterView();
    fireEvent.change(screen.getByTestId('register-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('register-email'), { target: { value: 'invalid' } });
    fireEvent.change(screen.getByTestId('register-displayname'), { target: { value: 'Test' } });
    fireEvent.change(screen.getByTestId('register-password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByTestId('register-confirm-password'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByTestId('register-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('register-error')).toHaveTextContent('valid email');
    });
  });

  it('shows validation error for short password', async () => {
    renderRegisterView();
    fireEvent.change(screen.getByTestId('register-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('register-email'), { target: { value: 'test@test.com' } });
    fireEvent.change(screen.getByTestId('register-displayname'), { target: { value: 'Test' } });
    fireEvent.change(screen.getByTestId('register-password'), { target: { value: '12345' } });
    fireEvent.change(screen.getByTestId('register-confirm-password'), { target: { value: '12345' } });
    fireEvent.click(screen.getByTestId('register-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('register-error')).toHaveTextContent('at least 6 characters');
    });
  });

  it('shows validation error for password mismatch', async () => {
    renderRegisterView();
    fireEvent.change(screen.getByTestId('register-username'), { target: { value: 'testuser' } });
    fireEvent.change(screen.getByTestId('register-email'), { target: { value: 'test@test.com' } });
    fireEvent.change(screen.getByTestId('register-displayname'), { target: { value: 'Test' } });
    fireEvent.change(screen.getByTestId('register-password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByTestId('register-confirm-password'), { target: { value: 'different' } });
    fireEvent.click(screen.getByTestId('register-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('register-error')).toHaveTextContent('do not match');
    });
  });

  it('calls register API on valid submission', async () => {
    mockRegisterFn.mockResolvedValueOnce({
      id: '1', username: 'newuser', displayName: 'New User', role: 'Player',
      email: 'new@test.com', avatarType: 'Default', isActive: true, createdAt: '', updatedAt: '',
    });

    renderRegisterView();
    fireEvent.change(screen.getByTestId('register-username'), { target: { value: 'newuser' } });
    fireEvent.change(screen.getByTestId('register-email'), { target: { value: 'new@test.com' } });
    fireEvent.change(screen.getByTestId('register-displayname'), { target: { value: 'New User' } });
    fireEvent.change(screen.getByTestId('register-password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByTestId('register-confirm-password'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByTestId('register-submit'));

    await waitFor(() => {
      expect(mockRegisterFn).toHaveBeenCalledWith(expect.objectContaining({
        username: 'newuser',
        email: 'new@test.com',
      }));
    });
  });
});
