/**
 * StatusBar component for displaying application status with quick actions.
 */
import React, { useState, useCallback } from 'react';
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
 * StatusBar component showing connection status, quick actions, and session info.
 */
export function StatusBar({ children }: StatusBarProps) {
  const {
    isConnected,
    connectionState,
    lastPing,
    isLoading,
    error: clientError,
    startClient,
    stopClient,
    forceStopClient,
    pingClient,
  } = useCopilotClient();
  const { activeSession, hubConnectionState, isSending, error: sessionError } = useSession();

  const [isPinging, setIsPinging] = useState(false);

  const error = clientError || sessionError;

  // Handle ping button click
  const handlePing = useCallback(async () => {
    setIsPinging(true);
    try {
      await pingClient();
    } catch {
      // Error is handled by context
    } finally {
      setIsPinging(false);
    }
  }, [pingClient]);

  // Handle start button click
  const handleStart = useCallback(async () => {
    try {
      await startClient();
    } catch {
      // Error is handled by context
    }
  }, [startClient]);

  // Handle stop button click
  const handleStop = useCallback(async () => {
    try {
      await stopClient();
    } catch {
      // Error is handled by context
    }
  }, [stopClient]);

  // Handle force stop button click
  const handleForceStop = useCallback(async () => {
    try {
      await forceStopClient();
    } catch {
      // Error is handled by context
    }
  }, [forceStopClient]);

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
        <div className="status-bar-actions" data-testid="status-bar-actions">
          {!isConnected ? (
            <button
              className="status-action-btn start"
              onClick={handleStart}
              disabled={isLoading}
              title="Start Client"
              data-testid="status-start-btn"
            >
              ▶ Start
            </button>
          ) : (
            <>
              <button
                className="status-action-btn stop"
                onClick={handleStop}
                disabled={isLoading}
                title="Stop Client"
                data-testid="status-stop-btn"
              >
                ⏹ Stop
              </button>
              <button
                className="status-action-btn force-stop"
                onClick={handleForceStop}
                disabled={isLoading}
                title="Force Stop Client"
                data-testid="status-force-stop-btn"
              >
                ⏏ Force
              </button>
            </>
          )}
          <button
            className="status-action-btn ping"
            onClick={handlePing}
            disabled={isPinging || !isConnected}
            title="Ping Server"
            data-testid="status-ping-btn"
          >
            {isPinging ? '...' : '📡 Ping'}
          </button>
        </div>
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
