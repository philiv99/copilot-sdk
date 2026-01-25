/**
 * Header component for the application.
 */
import React from 'react';
import './Header.css';

/**
 * Props for the Header component.
 */
export interface HeaderProps {
  /** Title to display in the header. */
  title?: string;
  /** Callback when settings button is clicked. */
  onSettingsClick?: () => void;
}

/**
 * Header component displaying the app title and settings button.
 */
export function Header({ title = 'Copilot SDK', onSettingsClick }: HeaderProps) {
  return (
    <header className="app-header" data-testid="app-header" role="banner">
      <div className="header-left">
        <div className="header-title">
          <h1>{title}</h1>
        </div>
      </div>
      <div className="header-right">
        {onSettingsClick && (
          <button
            type="button"
            className="settings-btn"
            onClick={onSettingsClick}
            aria-label="Open client configuration"
            data-testid="settings-button"
          >
            <span className="settings-icon" aria-hidden="true">⚙️</span>
          </button>
        )}
      </div>
    </header>
  );
}

export default Header;
