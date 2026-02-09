/**
 * Profile view - allows users to view/edit their profile, change password, and update avatar.
 */
import React, { useState, useCallback, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../context/UserContext';
import { AvatarPicker } from '../components/Auth/AvatarPicker';
import { UserAvatar } from '../components/Auth/UserAvatar';
import { RoleBadge } from '../components/Auth/RoleBadge';
import { UserRole } from '../types';
import './ProfileView.css';

/**
 * User profile view component.
 */
export function ProfileView() {
  const { state, updateProfile, changePassword, clearError } = useUser();
  const navigate = useNavigate();
  const user = state.currentUser;

  // Profile form
  const [displayName, setDisplayName] = useState(user?.displayName || '');
  const [email, setEmail] = useState(user?.email || '');
  const [avatarType, setAvatarType] = useState(user?.avatarType || 'Default');
  const [avatarData, setAvatarData] = useState(user?.avatarData || '');
  const [profileSuccess, setProfileSuccess] = useState('');
  const [profileError, setProfileError] = useState('');
  const [isProfileSaving, setIsProfileSaving] = useState(false);

  // Password form
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [isPasswordSaving, setIsPasswordSaving] = useState(false);

  if (!user) {
    return null;
  }

  const handleProfileSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setProfileSuccess('');
    setProfileError('');
    clearError();

    if (!displayName.trim()) {
      setProfileError('Display name is required.');
      return;
    }
    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setProfileError('Please enter a valid email address.');
      return;
    }

    setIsProfileSaving(true);
    try {
      const success = await updateProfile({
        displayName: displayName.trim(),
        email: email.trim(),
        avatarType,
        avatarData: avatarData || null,
      });
      if (success) {
        setProfileSuccess('Profile updated successfully.');
      } else {
        setProfileError(state.error || 'Failed to update profile.');
      }
    } catch (error) {
      setProfileError('Failed to update profile.');
    } finally {
      setIsProfileSaving(false);
    }
  };

  const handlePasswordSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setPasswordSuccess('');
    setPasswordError('');

    if (!currentPassword) {
      setPasswordError('Current password is required.');
      return;
    }
    if (newPassword.length < 6) {
      setPasswordError('New password must be at least 6 characters.');
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setPasswordError('New passwords do not match.');
      return;
    }

    setIsPasswordSaving(true);
    try {
      const success = await changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      });
      if (success) {
        setPasswordSuccess('Password changed successfully.');
        setCurrentPassword('');
        setNewPassword('');
        setConfirmNewPassword('');
      } else {
        setPasswordError(state.error || 'Failed to change password.');
      }
    } catch (error) {
      setPasswordError('Failed to change password.');
    } finally {
      setIsPasswordSaving(false);
    }
  };

  const handleAvatarChange = (type: string, data?: string | null) => {
    setAvatarType(type as any);
    setAvatarData(data || '');
  };

  return (
    <div className="profile-view" data-testid="profile-view">
      <div className="profile-container">
        <button
          type="button"
          className="back-link"
          onClick={() => navigate('/')}
          data-testid="profile-back"
        >
          ← Back to App
        </button>

        <h2>My Profile</h2>

        {/* Profile Info Summary */}
        <div className="profile-summary" data-testid="profile-summary">
          <UserAvatar
            avatarType={user.avatarType}
            avatarData={user.avatarData}
            displayName={user.displayName}
            size="large"
          />
          <div className="profile-summary-info">
            <h3>{user.displayName}</h3>
            <span className="profile-username">@{user.username}</span>
            <RoleBadge role={user.role as UserRole} />
          </div>
        </div>

        {/* Profile Edit Section */}
        <section className="profile-section" data-testid="profile-edit-section">
          <h3>Edit Profile</h3>

          {profileSuccess && (
            <div className="profile-success" role="status" data-testid="profile-success">
              {profileSuccess}
            </div>
          )}
          {profileError && (
            <div className="profile-error" role="alert" data-testid="profile-error">
              {profileError}
            </div>
          )}

          <form onSubmit={handleProfileSubmit} data-testid="profile-form">
            <div className="form-group">
              <label htmlFor="profile-displayname">Display Name</label>
              <input
                type="text"
                id="profile-displayname"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                disabled={isProfileSaving}
                data-testid="profile-displayname"
              />
            </div>

            <div className="form-group">
              <label htmlFor="profile-email">Email</label>
              <input
                type="email"
                id="profile-email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isProfileSaving}
                data-testid="profile-email"
              />
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
              className="btn-primary"
              disabled={isProfileSaving}
              data-testid="profile-save"
            >
              {isProfileSaving ? 'Saving...' : 'Save Changes'}
            </button>
          </form>
        </section>

        {/* Change Password Section */}
        <section className="profile-section" data-testid="password-section">
          <h3>Change Password</h3>

          {passwordSuccess && (
            <div className="profile-success" role="status" data-testid="password-success">
              {passwordSuccess}
            </div>
          )}
          {passwordError && (
            <div className="profile-error" role="alert" data-testid="password-error">
              {passwordError}
            </div>
          )}

          <form onSubmit={handlePasswordSubmit} data-testid="password-form">
            <div className="form-group">
              <label htmlFor="current-password">Current Password</label>
              <input
                type="password"
                id="current-password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                disabled={isPasswordSaving}
                data-testid="current-password"
              />
            </div>

            <div className="form-group">
              <label htmlFor="new-password">New Password</label>
              <input
                type="password"
                id="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Min. 6 characters"
                disabled={isPasswordSaving}
                data-testid="new-password"
              />
            </div>

            <div className="form-group">
              <label htmlFor="confirm-new-password">Confirm New Password</label>
              <input
                type="password"
                id="confirm-new-password"
                value={confirmNewPassword}
                onChange={(e) => setConfirmNewPassword(e.target.value)}
                disabled={isPasswordSaving}
                data-testid="confirm-new-password"
              />
            </div>

            <button
              type="submit"
              className="btn-primary"
              disabled={isPasswordSaving}
              data-testid="password-save"
            >
              {isPasswordSaving ? 'Changing...' : 'Change Password'}
            </button>
          </form>
        </section>

        {/* Account Info */}
        <section className="profile-section profile-info-section" data-testid="account-info-section">
          <h3>Account Info</h3>
          <dl className="info-list">
            <div className="info-item">
              <dt>Username</dt>
              <dd>@{user.username}</dd>
            </div>
            <div className="info-item">
              <dt>Role</dt>
              <dd><RoleBadge role={user.role as UserRole} /></dd>
            </div>
            <div className="info-item">
              <dt>Member Since</dt>
              <dd>{new Date(user.createdAt).toLocaleDateString()}</dd>
            </div>
            {user.lastLoginAt && (
              <div className="info-item">
                <dt>Last Login</dt>
                <dd>{new Date(user.lastLoginAt).toLocaleString()}</dd>
              </div>
            )}
          </dl>
        </section>
      </div>
    </div>
  );
}
