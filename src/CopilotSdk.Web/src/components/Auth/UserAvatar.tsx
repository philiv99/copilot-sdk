/**
 * Avatar display component for showing user avatars.
 */
import React from 'react';
import { AvatarType } from '../../types/user.types';
import './UserAvatar.css';

interface UserAvatarProps {
  avatarType: AvatarType;
  avatarData?: string | null;
  displayName?: string;
  size?: 'small' | 'medium' | 'large';
}

/** Preset avatar emoji map. */
const presetEmojis: Record<string, string> = {
  default: '👤',
  astronaut: '🧑‍🚀',
  robot: '🤖',
  ninja: '🥷',
  wizard: '🧙',
  pirate: '🏴‍☠️',
  alien: '👽',
  cat: '🐱',
  dog: '🐶',
  dragon: '🐉',
  unicorn: '🦄',
  phoenix: '🔥',
};

/**
 * Displays a user's avatar based on their avatar type and data.
 */
export function UserAvatar({ avatarType, avatarData, displayName, size = 'medium' }: UserAvatarProps) {
  const sizeClass = `user-avatar user-avatar--${size}`;

  if (avatarType === 'Custom' && avatarData) {
    return (
      <div className={sizeClass} data-testid="user-avatar" title={displayName}>
        <img
          src={avatarData.startsWith('data:') ? avatarData : `data:image/png;base64,${avatarData}`}
          alt={displayName || 'User avatar'}
          className="user-avatar__image"
        />
      </div>
    );
  }

  const emoji = avatarType === 'Preset' && avatarData
    ? (presetEmojis[avatarData] || '👤')
    : '👤';

  return (
    <div className={sizeClass} data-testid="user-avatar" title={displayName}>
      <span className="user-avatar__emoji" role="img" aria-label={displayName || 'User avatar'}>
        {emoji}
      </span>
    </div>
  );
}
