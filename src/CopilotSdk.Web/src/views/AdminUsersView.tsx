/**
 * Admin Users view - allows admins to manage all users.
 */
import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../context/UserContext';
import { UserAvatar } from '../components/Auth/UserAvatar';
import { RoleBadge } from '../components/Auth/RoleBadge';
import * as userApi from '../api/userApi';
import { UserResponse, UserRole } from '../types';
import './AdminUsersView.css';

/**
 * Admin user management view.
 */
export function AdminUsersView() {
  const { isAdmin } = useUser();
  const navigate = useNavigate();

  const [users, setUsers] = useState<UserResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionMessage, setActionMessage] = useState('');
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editRole, setEditRole] = useState<string>('');
  const [confirmResetId, setConfirmResetId] = useState<string | null>(null);
  const [tempPassword, setTempPassword] = useState<string | null>(null);

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const response = await userApi.getAllUsers();
      setUsers(response.users);
    } catch (err) {
      setError('Failed to load users.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleRoleChange = async (userId: string, newRole: string) => {
    setActionMessage('');
    try {
      await userApi.adminUpdateUser(userId, { role: newRole });
      setActionMessage(`Role updated to ${newRole}.`);
      setEditingUserId(null);
      await loadUsers();
    } catch (err) {
      setError('Failed to update role.');
    }
  };

  const handleToggleActive = async (user: UserResponse) => {
    setActionMessage('');
    try {
      if (user.isActive) {
        await userApi.deactivateUser(user.id);
        setActionMessage(`${user.username} has been deactivated.`);
      } else {
        await userApi.activateUser(user.id);
        setActionMessage(`${user.username} has been activated.`);
      }
      await loadUsers();
    } catch (err) {
      setError(`Failed to ${user.isActive ? 'deactivate' : 'activate'} user.`);
    }
  };

  const handleResetPassword = async (userId: string) => {
    setActionMessage('');
    setTempPassword(null);
    try {
      const result = await userApi.resetPassword(userId);
      setTempPassword(result.temporaryPassword);
      setConfirmResetId(null);
      setActionMessage('Password has been reset.');
    } catch (err) {
      setError('Failed to reset password.');
    }
  };

  if (!isAdmin) {
    return (
      <div className="admin-view" data-testid="admin-forbidden">
        <div className="admin-container">
          <h2>Access Denied</h2>
          <p>You do not have permission to access this page.</p>
          <button type="button" className="btn-primary" onClick={() => navigate('/')}>
            Go Back
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-view" data-testid="admin-users-view">
      <div className="admin-container">
        <button
          type="button"
          className="back-link"
          onClick={() => navigate('/')}
          data-testid="admin-back"
        >
          ← Back to App
        </button>

        <div className="admin-header">
          <h2>User Management</h2>
          <button
            type="button"
            className="btn-secondary"
            onClick={loadUsers}
            disabled={isLoading}
            data-testid="admin-refresh"
          >
            🔄 Refresh
          </button>
        </div>

        {error && (
          <div className="admin-error" role="alert" data-testid="admin-error">
            {error}
            <button type="button" onClick={() => setError('')} aria-label="Dismiss error">✕</button>
          </div>
        )}

        {actionMessage && (
          <div className="admin-success" role="status" data-testid="admin-success">
            {actionMessage}
            <button type="button" onClick={() => setActionMessage('')} aria-label="Dismiss">✕</button>
          </div>
        )}

        {tempPassword && (
          <div className="admin-warning" role="alert" data-testid="admin-temp-password">
            <strong>Temporary Password:</strong>{' '}
            <code>{tempPassword}</code>
            <p>Please share this with the user securely. They should change it immediately.</p>
            <button type="button" onClick={() => setTempPassword(null)} aria-label="Dismiss">✕</button>
          </div>
        )}

        {isLoading ? (
          <div className="admin-loading" data-testid="admin-loading">Loading users...</div>
        ) : (
          <div className="admin-table-container">
            <table className="admin-table" data-testid="admin-users-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id} className={!u.isActive ? 'row-inactive' : ''} data-testid={`user-row-${u.id}`}>
                    <td className="user-cell">
                      <UserAvatar
                        avatarType={u.avatarType}
                        avatarData={u.avatarData}
                        displayName={u.displayName}
                        size="small"
                      />
                      <div>
                        <span className="user-displayname">{u.displayName}</span>
                        <span className="user-username">@{u.username}</span>
                      </div>
                    </td>
                    <td>{u.email}</td>
                    <td>
                      {editingUserId === u.id ? (
                        <div className="role-editor">
                          <select
                            value={editRole}
                            onChange={(e) => setEditRole(e.target.value)}
                            data-testid={`role-select-${u.id}`}
                          >
                            <option value="Player">Player</option>
                            <option value="Creator">Creator</option>
                            <option value="Admin">Admin</option>
                          </select>
                          <button
                            type="button"
                            className="btn-sm btn-primary"
                            onClick={() => handleRoleChange(u.id, editRole)}
                            data-testid={`role-save-${u.id}`}
                          >
                            ✓
                          </button>
                          <button
                            type="button"
                            className="btn-sm btn-secondary"
                            onClick={() => setEditingUserId(null)}
                          >
                            ✕
                          </button>
                        </div>
                      ) : (
                        <span
                          className="role-clickable"
                          onClick={() => { setEditingUserId(u.id); setEditRole(u.role); }}
                          title="Click to change role"
                          data-testid={`role-badge-${u.id}`}
                        >
                          <RoleBadge role={u.role as UserRole} />
                        </span>
                      )}
                    </td>
                    <td>
                      <span className={`status-badge ${u.isActive ? 'status-active' : 'status-inactive'}`}>
                        {u.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                    <td className="actions-cell">
                      <button
                        type="button"
                        className={`btn-sm ${u.isActive ? 'btn-warning' : 'btn-success'}`}
                        onClick={() => handleToggleActive(u)}
                        title={u.isActive ? 'Deactivate user' : 'Activate user'}
                        data-testid={`toggle-active-${u.id}`}
                      >
                        {u.isActive ? '🚫' : '✅'}
                      </button>
                      {confirmResetId === u.id ? (
                        <span className="confirm-reset">
                          <span>Reset?</span>
                          <button
                            type="button"
                            className="btn-sm btn-danger"
                            onClick={() => handleResetPassword(u.id)}
                            data-testid={`confirm-reset-${u.id}`}
                          >
                            Yes
                          </button>
                          <button
                            type="button"
                            className="btn-sm btn-secondary"
                            onClick={() => setConfirmResetId(null)}
                          >
                            No
                          </button>
                        </span>
                      ) : (
                        <button
                          type="button"
                          className="btn-sm btn-secondary"
                          onClick={() => setConfirmResetId(u.id)}
                          title="Reset password"
                          data-testid={`reset-password-${u.id}`}
                        >
                          🔑
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {users.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty-message">No users found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        <div className="admin-footer">
          Total: {users.length} user{users.length !== 1 ? 's' : ''}
        </div>
      </div>
    </div>
  );
}
