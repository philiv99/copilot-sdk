/**
 * Visual indicator for a user's role.
 */
import React from 'react';
import { UserRole } from '../../types/user.types';
import './RoleBadge.css';

interface RoleBadgeProps {
  role: UserRole;
  /** Compact mode shows abbreviated text */
  compact?: boolean;
}

const roleConfig: Record<string, { label: string; shortLabel: string; icon: string; className: string }> = {
  Admin: { label: 'Admin', shortLabel: 'A', icon: '🛡️', className: 'role-badge--admin' },
  Creator: { label: 'Creator', shortLabel: 'C', icon: '✏️', className: 'role-badge--creator' },
  Player: { label: 'Player', shortLabel: 'P', icon: '🎮', className: 'role-badge--player' },
};

/**
 * Displays a colored badge indicating user role.
 */
export function RoleBadge({ role, compact = false }: RoleBadgeProps) {
  const config = roleConfig[role] ?? roleConfig['Player'];

  return (
    <span
      className={`role-badge ${config.className}`}
      title={config.label}
      aria-label={`Role: ${config.label}`}
      data-testid="role-badge"
    >
      <span className="role-badge__icon" aria-hidden="true">{config.icon}</span>
      <span className="role-badge__label">
        {compact ? config.shortLabel : config.label}
      </span>
    </span>
  );
}
