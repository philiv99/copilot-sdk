/**
 * StatusBar component for displaying application status.
 */
import React from 'react';
import { useCopilotClient, useSession } from '../../context';
import './StatusBar.css';

/**
 * Props for the StatusBar component.
 */
export interface StatusBarProps {
  /** Additional content to display. */
  children?: React.ReactNode;
}

/**
 * StatusBar component showing connection and session info.
 */
export function StatusBar({ children }: StatusBarProps) {
  const { isConnected, connectionState, lastPing, error: clientError } = useCopilotClient();
  const { activeSession, hubConnectionState, isSending, error: sessionError } = useSession();

  const error = clientError || sessionError;

  return (
    <footer className="app-status-bar" data-testid="app-status-bar">
      <div className="status-bar-left">
        <span className="status-item">
          <span className={`status-badge ${isConnected ? 'badge-success' : 'badge-error'}`}>
            Client: {connectionState}
          </span>
        </span>
        <span className="status-item">
          <span className={`status-badge ${hubConnectionState === 'Connected' ? 'badge-success' : 'badge-warning'}`}>
            Hub: {hubConnectionState}
          </span>
        </span>
        {lastPing && (
          <span className="status-item">
            <span className="status-badge badge-info">
              Ping: {lastPing.latencyMs}ms
            </span>
          </span>
        )}
      </div>

      <div className="status-bar-center">
        {error && (
          <span className="status-error" data-testid="status-error">
            ⚠️ {error}
          </span>
        )}
        {isSending && (
          <span className="status-sending">
            Sending message...
          </span>
        )}
        {children}
      </div>

      <div className="status-bar-right">
        {activeSession && (
          <span className="status-item">
            <span className="status-badge badge-default">
              Session: {activeSession.sessionId}
            </span>
          </span>
        )}
      </div>
    </footer>
  );
}

export default StatusBar;
