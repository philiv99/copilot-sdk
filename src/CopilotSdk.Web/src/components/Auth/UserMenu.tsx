/**
 * User menu dropdown shown in the header.
 */
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../../context/UserContext';
import { UserAvatar } from './UserAvatar';
import './UserMenu.css';

/**
 * Dropdown menu for user actions (profile, admin, logout).
 */
export function UserMenu() {
  const { state, logout, isAdmin } = useUser();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const user = state.currentUser;

  // Close menu on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleToggle = useCallback(() => {
    setIsOpen(prev => !prev);
  }, []);

  const handleNavigate = useCallback((path: string) => {
    setIsOpen(false);
    navigate(path);
  }, [navigate]);

  const handleLogout = useCallback(async () => {
    setIsOpen(false);
    await logout();
    navigate('/login');
  }, [logout, navigate]);

  if (!user) return null;

  return (
    <div className="user-menu" ref={menuRef} data-testid="user-menu">
      <button
        type="button"
        className="user-menu__trigger"
        onClick={handleToggle}
        aria-expanded={isOpen}
        aria-haspopup="true"
        aria-label="User menu"
        data-testid="user-menu-trigger"
      >
        <UserAvatar
          avatarType={user.avatarType}
          avatarData={user.avatarData}
          displayName={user.displayName}
          size="small"
        />
        <span className="user-menu__name">{user.displayName}</span>
        <span className="user-menu__caret" aria-hidden="true">▾</span>
      </button>

      {isOpen && (
        <div className="user-menu__dropdown" role="menu" data-testid="user-menu-dropdown">
          <div className="user-menu__header">
            <strong>{user.displayName}</strong>
            <span className="user-menu__role">{user.role}</span>
          </div>
          <div className="user-menu__divider" />
          <button
            type="button"
            className="user-menu__item"
            role="menuitem"
            onClick={() => handleNavigate('/profile')}
            data-testid="user-menu-profile"
          >
            👤 My Profile
          </button>
          {isAdmin && (
            <button
              type="button"
              className="user-menu__item"
              role="menuitem"
              onClick={() => handleNavigate('/admin/users')}
              data-testid="user-menu-admin"
            >
              🛡️ Manage Users
            </button>
          )}
          <div className="user-menu__divider" />
          <button
            type="button"
            className="user-menu__item user-menu__item--danger"
            role="menuitem"
            onClick={handleLogout}
            data-testid="user-menu-logout"
          >
            🚪 Logout
          </button>
        </div>
      )}
    </div>
  );
}
