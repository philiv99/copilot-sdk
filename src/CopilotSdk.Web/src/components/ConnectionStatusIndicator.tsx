/**
 * Connection status indicator component.
 * Displays a visual indicator of the current connection state.
 */
import React from 'react';
import { ConnectionState } from '../types';
import './ConnectionStatusIndicator.css';

/**
 * Props for the ConnectionStatusIndicator component.
 */
interface ConnectionStatusIndicatorProps {
  /** Current connection state. */
  state: ConnectionState;
  /** Optional size variant. */
  size?: 'small' | 'medium' | 'large';
  /** Whether to show the label. */
  showLabel?: boolean;
}

/**
 * Get the CSS class for the current state.
 */
function getStateClass(state: ConnectionState): string {
  switch (state) {
    case 'Connected':
      return 'connected';
    case 'Connecting':
      return 'connecting';
    case 'Error':
      return 'error';
    case 'Disconnected':
    default:
      return 'disconnected';
  }
}

/**
 * Get the label for the current state.
 */
function getStateLabel(state: ConnectionState): string {
  switch (state) {
    case 'Connected':
      return 'Connected';
    case 'Connecting':
      return 'Connecting...';
    case 'Error':
      return 'Error';
    case 'Disconnected':
    default:
      return 'Disconnected';
  }
}

/**
 * Connection status indicator component.
 */
export function ConnectionStatusIndicator({
  state,
  size = 'medium',
  showLabel = true,
}: ConnectionStatusIndicatorProps) {
  const stateClass = getStateClass(state);
  const label = getStateLabel(state);

  return (
    <div
      className={`connection-status-indicator ${stateClass} ${size}`}
      data-testid="connection-status-indicator"
      role="status"
      aria-label={`Connection status: ${label}`}
    >
      <span className="status-dot" data-testid="status-dot" />
      {showLabel && (
        <span className="status-label" data-testid="status-label">
          {label}
        </span>
      )}
    </div>
  );
}

export default ConnectionStatusIndicator;
