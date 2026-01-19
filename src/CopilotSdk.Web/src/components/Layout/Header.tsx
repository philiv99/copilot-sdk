/**
 * Header component for the application.
 */
import React from 'react';
import { useCopilotClient } from '../../context';
import './Header.css';

/**
 * Props for the Header component.
 */
export interface HeaderProps {
  /** Title to display in the header. */
  title?: string;
  /** Callback when menu button is clicked (mobile). */
  onMenuClick?: () => void;
  /** Whether to show the menu button. */
  showMenuButton?: boolean;
}

/**
 * Header component displaying the app title and connection status.
 */
export function Header({ title = 'Copilot SDK', onMenuClick, showMenuButton = true }: HeaderProps) {
  const { connectionState, isConnected } = useCopilotClient();

  const statusClass = isConnected ? 'status-connected' : 'status-disconnected';
  const statusText = connectionState;

  return (
    <header className="app-header" data-testid="app-header" role="banner">
      <div className="header-left">
        {showMenuButton && (
          <button
            type="button"
            className="menu-toggle-btn"
            onClick={onMenuClick}
            aria-label="Toggle navigation menu"
            aria-expanded="false"
          >
            <span className="menu-icon">☰</span>
          </button>
        )}
        <div className="header-title">
          <h1>{title}</h1>
        </div>
      </div>
      <div className="header-status">
        <span className={`status-indicator ${statusClass}`} data-testid="status-indicator" role="status">
          <span className="status-dot" aria-hidden="true" />
          <span className="status-text">{statusText}</span>
        </span>
      </div>
    </header>
  );
}

export default Header;
