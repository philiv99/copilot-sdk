/**
 * Forgot credentials view - stubs for forgot username and forgot password flows.
 */
import React, { useState, FormEvent } from 'react';
import { Link } from 'react-router-dom';
import * as userApi from '../api/userApi';
import './ForgotCredentialsView.css';

type ActiveTab = 'username' | 'password';

/**
 * Forgot username/password page with tabbed interface.
 */
export function ForgotCredentialsView() {
  const [activeTab, setActiveTab] = useState<ActiveTab>('username');

  // Forgot username
  const [email, setEmail] = useState('');
  const [usernameMessage, setUsernameMessage] = useState('');
  const [usernameError, setUsernameError] = useState('');
  const [isUsernameSubmitting, setIsUsernameSubmitting] = useState(false);

  // Forgot password
  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [passwordMessage, setPasswordMessage] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [isPasswordSubmitting, setIsPasswordSubmitting] = useState(false);

  const handleForgotUsername = async (e: FormEvent) => {
    e.preventDefault();
    setUsernameMessage('');
    setUsernameError('');

    if (!email.trim()) {
      setUsernameError('Email is required.');
      return;
    }

    setIsUsernameSubmitting(true);
    try {
      const result = await userApi.forgotUsername({ email: email.trim() });
      setUsernameMessage(result || 'If an account exists with this email, instructions have been sent.');
    } catch (err) {
      setUsernameMessage('If an account exists with this email, instructions have been sent.');
    } finally {
      setIsUsernameSubmitting(false);
    }
  };

  const handleForgotPassword = async (e: FormEvent) => {
    e.preventDefault();
    setPasswordMessage('');
    setPasswordError('');

    if (!usernameOrEmail.trim()) {
      setPasswordError('Username or email is required.');
      return;
    }

    setIsPasswordSubmitting(true);
    try {
      const result = await userApi.forgotPassword({ usernameOrEmail: usernameOrEmail.trim() });
      setPasswordMessage(result || 'If an account exists, password reset instructions have been sent.');
    } catch (err) {
      setPasswordMessage('If an account exists, password reset instructions have been sent.');
    } finally {
      setIsPasswordSubmitting(false);
    }
  };

  return (
    <div className="forgot-view" data-testid="forgot-credentials-view">
      <div className="forgot-card">
        <div className="forgot-header">
          <h1>Account Recovery</h1>
          <p>Recover your username or reset your password</p>
        </div>

        <div className="forgot-tabs" role="tablist">
          <button
            type="button"
            className={`forgot-tab ${activeTab === 'username' ? 'forgot-tab--active' : ''}`}
            onClick={() => setActiveTab('username')}
            role="tab"
            aria-selected={activeTab === 'username'}
            data-testid="tab-username"
          >
            Forgot Username
          </button>
          <button
            type="button"
            className={`forgot-tab ${activeTab === 'password' ? 'forgot-tab--active' : ''}`}
            onClick={() => setActiveTab('password')}
            role="tab"
            aria-selected={activeTab === 'password'}
            data-testid="tab-password"
          >
            Forgot Password
          </button>
        </div>

        {activeTab === 'username' && (
          <div role="tabpanel" data-testid="panel-username">
            {usernameMessage && (
              <div className="forgot-success" role="status" data-testid="username-message">
                {usernameMessage}
              </div>
            )}
            {usernameError && (
              <div className="forgot-error" role="alert" data-testid="username-error">
                {usernameError}
              </div>
            )}
            <form onSubmit={handleForgotUsername} className="forgot-form" data-testid="username-form">
              <div className="form-group">
                <label htmlFor="forgot-email">Email Address</label>
                <input
                  type="email"
                  id="forgot-email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Enter your email address"
                  disabled={isUsernameSubmitting}
                  required
                  data-testid="forgot-email"
                />
              </div>
              <p className="forgot-hint">
                If an account with this email exists, we will send your username to this address.
              </p>
              <button
                type="submit"
                className="forgot-submit-btn"
                disabled={isUsernameSubmitting || !email.trim()}
                data-testid="forgot-username-submit"
              >
                {isUsernameSubmitting ? 'Sending...' : 'Send Username Reminder'}
              </button>
            </form>
          </div>
        )}

        {activeTab === 'password' && (
          <div role="tabpanel" data-testid="panel-password">
            {passwordMessage && (
              <div className="forgot-success" role="status" data-testid="password-message">
                {passwordMessage}
              </div>
            )}
            {passwordError && (
              <div className="forgot-error" role="alert" data-testid="password-error">
                {passwordError}
              </div>
            )}
            <form onSubmit={handleForgotPassword} className="forgot-form" data-testid="password-form">
              <div className="form-group">
                <label htmlFor="forgot-username-or-email">Username or Email</label>
                <input
                  type="text"
                  id="forgot-username-or-email"
                  value={usernameOrEmail}
                  onChange={(e) => setUsernameOrEmail(e.target.value)}
                  placeholder="Enter your username or email"
                  disabled={isPasswordSubmitting}
                  required
                  data-testid="forgot-username-or-email"
                />
              </div>
              <p className="forgot-hint">
                If an account exists, we will send password reset instructions.
              </p>
              <button
                type="submit"
                className="forgot-submit-btn"
                disabled={isPasswordSubmitting || !usernameOrEmail.trim()}
                data-testid="forgot-password-submit"
              >
                {isPasswordSubmitting ? 'Sending...' : 'Send Password Reset'}
              </button>
            </form>
          </div>
        )}

        <div className="forgot-footer">
          <Link to="/login" className="forgot-link" data-testid="back-to-login">
            ← Back to Sign In
          </Link>
        </div>
      </div>
    </div>
  );
}
