/**
 * Login view - allows users to sign in with username and password.
 */
import React, { useState, useCallback, FormEvent } from 'react';
import { Link, useNavigate, Navigate } from 'react-router-dom';
import { useUser } from '../context/UserContext';
import './LoginView.css';

/**
 * Login page component.
 */
export function LoginView() {
  const { login, state, isAuthenticated, clearError } = useUser();
  const navigate = useNavigate();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    clearError();

    if (!username.trim() || !password) {
      return;
    }

    setIsSubmitting(true);
    try {
      const success = await login({ username: username.trim(), password });
      if (success) {
        navigate('/');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-view" data-testid="login-view">
      <div className="login-card">
        <div className="login-header">
          <h1>App Maker</h1>
          <p>Sign in to your account</p>
        </div>

        {state.error && (
          <div className="login-error" role="alert" data-testid="login-error">
            {state.error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form" data-testid="login-form">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              autoComplete="username"
              autoFocus
              required
              disabled={isSubmitting}
              data-testid="login-username"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              autoComplete="current-password"
              required
              disabled={isSubmitting}
              data-testid="login-password"
            />
          </div>

          <button
            type="submit"
            className="login-submit-btn"
            disabled={isSubmitting || !username.trim() || !password}
            data-testid="login-submit"
          >
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <div className="login-footer">
          <Link to="/register" className="login-link" data-testid="register-link">
            Create an account
          </Link>
          <Link to="/forgot-credentials" className="login-link" data-testid="forgot-link">
            Forgot username or password?
          </Link>
        </div>
      </div>
    </div>
  );
}
