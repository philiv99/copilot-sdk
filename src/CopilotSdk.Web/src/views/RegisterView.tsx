/**
 * Registration view - allows new users to create an account.
 */
import React, { useState, useCallback, FormEvent } from 'react';
import { Link, useNavigate, Navigate } from 'react-router-dom';
import { useUser } from '../context/UserContext';
import { AvatarPicker } from '../components/Auth/AvatarPicker';
import './RegisterView.css';

/**
 * Registration page component.
 */
export function RegisterView() {
  const { register, state, isAuthenticated, clearError } = useUser();
  const navigate = useNavigate();

  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [avatarType, setAvatarType] = useState<string>('Default');
  const [avatarData, setAvatarData] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationError, setValidationError] = useState('');

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const validateForm = (): boolean => {
    if (!username.trim()) {
      setValidationError('Username is required.');
      return false;
    }
    if (username.trim().length < 3) {
      setValidationError('Username must be at least 3 characters.');
      return false;
    }
    if (!email.trim()) {
      setValidationError('Email is required.');
      return false;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setValidationError('Please enter a valid email address.');
      return false;
    }
    if (!displayName.trim()) {
      setValidationError('Display name is required.');
      return false;
    }
    if (password.length < 6) {
      setValidationError('Password must be at least 6 characters.');
      return false;
    }
    if (password !== confirmPassword) {
      setValidationError('Passwords do not match.');
      return false;
    }
    setValidationError('');
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    clearError();

    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      const success = await register({
        username: username.trim(),
        email: email.trim(),
        displayName: displayName.trim(),
        password,
        confirmPassword,
        avatarType,
        avatarData: avatarData || undefined,
      });
      if (success) {
        navigate('/');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAvatarChange = (type: string, data?: string | null) => {
    setAvatarType(type);
    setAvatarData(data || '');
  };

  const displayError = validationError || state.error;

  return (
    <div className="register-view" data-testid="register-view">
      <div className="register-card">
        <div className="register-header">
          <h1>Create Account</h1>
          <p>Join App Maker to get started</p>
        </div>

        {displayError && (
          <div className="register-error" role="alert" data-testid="register-error">
            {displayError}
          </div>
        )}

        <form onSubmit={handleSubmit} className="register-form" data-testid="register-form">
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="reg-username">Username *</label>
              <input
                type="text"
                id="reg-username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Choose a username"
                autoComplete="username"
                autoFocus
                required
                disabled={isSubmitting}
                data-testid="register-username"
              />
            </div>
            <div className="form-group">
              <label htmlFor="reg-email">Email *</label>
              <input
                type="email"
                id="reg-email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
                autoComplete="email"
                required
                disabled={isSubmitting}
                data-testid="register-email"
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="reg-displayname">Display Name *</label>
            <input
              type="text"
              id="reg-displayname"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Your display name"
              required
              disabled={isSubmitting}
              data-testid="register-displayname"
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="reg-password">Password *</label>
              <input
                type="password"
                id="reg-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Min. 6 characters"
                autoComplete="new-password"
                required
                disabled={isSubmitting}
                data-testid="register-password"
              />
            </div>
            <div className="form-group">
              <label htmlFor="reg-confirm">Confirm Password *</label>
              <input
                type="password"
                id="reg-confirm"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Confirm your password"
                autoComplete="new-password"
                required
                disabled={isSubmitting}
                data-testid="register-confirm-password"
              />
            </div>
          </div>

          <div className="form-group">
            <label>Avatar</label>
            <AvatarPicker
              selectedType={avatarType}
              selectedData={avatarData}
              onChange={handleAvatarChange}
            />
          </div>

          <button
            type="submit"
            className="register-submit-btn"
            disabled={isSubmitting}
            data-testid="register-submit"
          >
            {isSubmitting ? 'Creating Account...' : 'Create Account'}
          </button>
        </form>

        <div className="register-footer">
          <span>Already have an account? </span>
          <Link to="/login" className="register-link" data-testid="login-link">
            Sign in
          </Link>
        </div>
      </div>
    </div>
  );
}
