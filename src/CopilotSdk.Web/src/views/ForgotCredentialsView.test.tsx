/**
 * Tests for the ForgotCredentialsView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ForgotCredentialsView } from './ForgotCredentialsView';

const mockForgotUsername = jest.fn();
const mockForgotPassword = jest.fn();

jest.mock('../api/userApi', () => ({
  forgotUsername: (...args: any[]) => mockForgotUsername(...args),
  forgotPassword: (...args: any[]) => mockForgotPassword(...args),
}));

const renderForgotView = () => {
  return render(
    <BrowserRouter>
      <ForgotCredentialsView />
    </BrowserRouter>
  );
};

describe('ForgotCredentialsView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockForgotUsername.mockResolvedValue('If an account exists with this email, instructions have been sent.');
    mockForgotPassword.mockResolvedValue('If an account exists, password reset instructions have been sent.');
  });

  it('renders the forgot credentials view', () => {
    renderForgotView();
    expect(screen.getByTestId('forgot-credentials-view')).toBeInTheDocument();
    expect(screen.getByText('Account Recovery')).toBeInTheDocument();
  });

  it('renders two tabs', () => {
    renderForgotView();
    expect(screen.getByTestId('tab-username')).toBeInTheDocument();
    expect(screen.getByTestId('tab-password')).toBeInTheDocument();
  });

  it('shows username tab by default', () => {
    renderForgotView();
    expect(screen.getByTestId('panel-username')).toBeInTheDocument();
  });

  it('switches to password tab on click', () => {
    renderForgotView();
    fireEvent.click(screen.getByTestId('tab-password'));
    expect(screen.getByTestId('panel-password')).toBeInTheDocument();
  });

  it('renders forgot username form', () => {
    renderForgotView();
    expect(screen.getByTestId('username-form')).toBeInTheDocument();
    expect(screen.getByTestId('forgot-email')).toBeInTheDocument();
    expect(screen.getByTestId('forgot-username-submit')).toBeInTheDocument();
  });

  it('renders forgot password form on password tab', () => {
    renderForgotView();
    fireEvent.click(screen.getByTestId('tab-password'));
    expect(screen.getByTestId('password-form')).toBeInTheDocument();
    expect(screen.getByTestId('forgot-username-or-email')).toBeInTheDocument();
    expect(screen.getByTestId('forgot-password-submit')).toBeInTheDocument();
  });

  it('disables username submit when email is empty', () => {
    renderForgotView();
    expect(screen.getByTestId('forgot-username-submit')).toBeDisabled();
  });

  it('submits forgot username request', async () => {
    renderForgotView();
    fireEvent.change(screen.getByTestId('forgot-email'), { target: { value: 'test@test.com' } });
    fireEvent.click(screen.getByTestId('forgot-username-submit'));

    await waitFor(() => {
      expect(mockForgotUsername).toHaveBeenCalledWith({ email: 'test@test.com' });
      expect(screen.getByTestId('username-message')).toBeInTheDocument();
    });
  });

  it('submits forgot password request', async () => {
    renderForgotView();
    fireEvent.click(screen.getByTestId('tab-password'));
    fireEvent.change(screen.getByTestId('forgot-username-or-email'), { target: { value: 'testuser' } });
    fireEvent.click(screen.getByTestId('forgot-password-submit'));

    await waitFor(() => {
      expect(mockForgotPassword).toHaveBeenCalledWith({ usernameOrEmail: 'testuser' });
      expect(screen.getByTestId('password-message')).toBeInTheDocument();
    });
  });

  it('shows generic message even on API error (username)', async () => {
    mockForgotUsername.mockRejectedValueOnce(new Error('Network error'));
    renderForgotView();
    fireEvent.change(screen.getByTestId('forgot-email'), { target: { value: 'test@test.com' } });
    fireEvent.click(screen.getByTestId('forgot-username-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('username-message')).toBeInTheDocument();
    });
  });

  it('shows generic message even on API error (password)', async () => {
    mockForgotPassword.mockRejectedValueOnce(new Error('Network error'));
    renderForgotView();
    fireEvent.click(screen.getByTestId('tab-password'));
    fireEvent.change(screen.getByTestId('forgot-username-or-email'), { target: { value: 'test@test.com' } });
    fireEvent.click(screen.getByTestId('forgot-password-submit'));

    await waitFor(() => {
      expect(screen.getByTestId('password-message')).toBeInTheDocument();
    });
  });

  it('renders back to login link', () => {
    renderForgotView();
    expect(screen.getByTestId('back-to-login')).toBeInTheDocument();
  });

  it('has proper accessibility attributes on tabs', () => {
    renderForgotView();
    expect(screen.getByTestId('tab-username')).toHaveAttribute('role', 'tab');
    expect(screen.getByTestId('tab-password')).toHaveAttribute('role', 'tab');
    expect(screen.getByTestId('tab-username')).toHaveAttribute('aria-selected', 'true');
  });
});
