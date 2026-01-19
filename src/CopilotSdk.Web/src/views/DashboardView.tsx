/**
 * Dashboard view - main overview of the Copilot SDK client status.
 */
import React, { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useCopilotClient, useSession } from '../context';
import { ConnectionStatusIndicator, Spinner, CardSkeleton } from '../components';
import './DashboardView.css';

/**
 * Dashboard view component showing client status, quick actions, and recent sessions.
 */
export function DashboardView() {
  const {
    status,
    config,
    isLoading,
    error,
    lastPing,
    connectionState,
    isConnected,
    startClient,
    stopClient,
    forceStopClient,
    pingClient,
    clearError,
  } = useCopilotClient();

  const { sessions, refreshSessions, isLoading: isSessionsLoading } = useSession();

  const [isPinging, setIsPinging] = useState(false);
  const [pingError, setPingError] = useState<string | null>(null);

  // Handle ping button click
  const handlePing = useCallback(async () => {
    setIsPinging(true);
    setPingError(null);
    try {
      await pingClient();
    } catch (err) {
      setPingError(err instanceof Error ? err.message : 'Ping failed');
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

  // Format date for display
  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  // Get recent sessions (max 5)
  const recentSessions = sessions.slice(0, 5);

  return (
    <div className="dashboard-view" data-testid="dashboard-view">
      <h2>Dashboard</h2>
      <p>Welcome to the Copilot SDK application.</p>

      {/* Error Display */}
      {error && (
        <div className="dashboard-error" data-testid="dashboard-error">
          <span className="error-message">{error}</span>
          <button className="error-dismiss" onClick={clearError} aria-label="Dismiss error">
            ×
          </button>
        </div>
      )}

      {/* Status Card */}
      <div className="dashboard-card" data-testid="status-card">
        <div className="card-header">
          <h3>Connection Status</h3>
          <ConnectionStatusIndicator state={connectionState} />
        </div>
        <div className="card-content">
          <div className="status-details">
            <div className="status-item">
              <span className="status-label">State:</span>
              <span className="status-value" data-testid="connection-state">{connectionState}</span>
            </div>
            <div className="status-item">
              <span className="status-label">Connected:</span>
              <span className="status-value" data-testid="is-connected">{isConnected ? 'Yes' : 'No'}</span>
            </div>
            <div className="status-item">
              <span className="status-label">Connected At:</span>
              <span className="status-value">{formatDate(status?.connectedAt)}</span>
            </div>
            {status?.error && (
              <div className="status-item status-error">
                <span className="status-label">Error:</span>
                <span className="status-value">{status.error}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Quick Actions Card */}
      <div className="dashboard-card" data-testid="quick-actions-card">
        <div className="card-header">
          <h3>Quick Actions</h3>
        </div>
        <div className="card-content">
          <div className="quick-actions">
            <button
              className="action-button primary"
              onClick={handleStart}
              disabled={isLoading || isConnected}
              data-testid="start-button"
            >
              {isLoading ? 'Starting...' : 'Start Client'}
            </button>
            <button
              className="action-button secondary"
              onClick={handleStop}
              disabled={isLoading || !isConnected}
              data-testid="stop-button"
            >
              {isLoading ? 'Stopping...' : 'Stop Client'}
            </button>
            <button
              className="action-button danger"
              onClick={handleForceStop}
              disabled={isLoading || !isConnected}
              data-testid="force-stop-button"
            >
              Force Stop
            </button>
            <Link to="/config" className="action-button outline" data-testid="config-link">
              Configure
            </Link>
          </div>
        </div>
      </div>

      {/* Ping Card */}
      <div className="dashboard-card" data-testid="ping-card">
        <div className="card-header">
          <h3>Connectivity Test</h3>
        </div>
        <div className="card-content">
          <div className="ping-section">
            <button
              className="action-button primary"
              onClick={handlePing}
              disabled={isPinging || !isConnected}
              data-testid="ping-button"
            >
              {isPinging ? 'Pinging...' : 'Ping Server'}
            </button>
            {lastPing && (
              <div className="ping-result" data-testid="ping-result">
                <span className={`ping-status ${lastPing.success ? 'success' : 'failed'}`}>
                  {lastPing.success ? '✓' : '✗'}
                </span>
                <span className="ping-latency">{lastPing.latencyMs}ms</span>
                <span className="ping-message">{lastPing.message}</span>
              </div>
            )}
            {pingError && (
              <div className="ping-error" data-testid="ping-error">
                {pingError}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Recent Sessions Card */}
      <div className="dashboard-card" data-testid="sessions-card">
        <div className="card-header">
          <h3>Recent Sessions</h3>
          <button
            className="refresh-button"
            onClick={refreshSessions}
            disabled={isSessionsLoading}
            aria-label="Refresh sessions"
            data-testid="refresh-sessions-button"
          >
            {isSessionsLoading ? <Spinner size="small" /> : '↻'}
          </button>
        </div>
        <div className="card-content">
          {isSessionsLoading && sessions.length === 0 ? (
            <div className="sessions-loading" data-testid="sessions-loading">
              <CardSkeleton lines={2} />
              <CardSkeleton lines={2} />
              <CardSkeleton lines={2} />
            </div>
          ) : recentSessions.length === 0 ? (
            <p className="no-sessions" data-testid="no-sessions">No sessions found. Create a new session to get started.</p>
          ) : (
            <ul className="sessions-list" data-testid="sessions-list">
              {recentSessions.map((session) => (
                <li key={session.sessionId} className="session-item">
                  <Link to={`/sessions/${session.sessionId}`} className="session-link">
                    <span className="session-id">{session.sessionId}</span>
                    <span className="session-model">{session.model}</span>
                    <span className={`session-status ${session.status.toLowerCase()}`}>
                      {session.status}
                    </span>
                    <span className="session-messages">{session.messageCount} messages</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
          <div className="sessions-footer">
            <Link to="/sessions" className="view-all-link" data-testid="view-all-sessions">
              View All Sessions →
            </Link>
          </div>
        </div>
      </div>

      {/* Configuration Summary Card */}
      {config && (
        <div className="dashboard-card" data-testid="config-summary-card">
          <div className="card-header">
            <h3>Configuration Summary</h3>
            <Link to="/config" className="edit-link">Edit</Link>
          </div>
          <div className="card-content">
            <div className="config-summary">
              <div className="config-item">
                <span className="config-label">Port:</span>
                <span className="config-value">{config.port}</span>
              </div>
              <div className="config-item">
                <span className="config-label">Use Stdio:</span>
                <span className="config-value">{config.useStdio ? 'Yes' : 'No'}</span>
              </div>
              <div className="config-item">
                <span className="config-label">Auto Start:</span>
                <span className="config-value">{config.autoStart ? 'Yes' : 'No'}</span>
              </div>
              <div className="config-item">
                <span className="config-label">Auto Restart:</span>
                <span className="config-value">{config.autoRestart ? 'Yes' : 'No'}</span>
              </div>
              <div className="config-item">
                <span className="config-label">Log Level:</span>
                <span className="config-value">{config.logLevel}</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default DashboardView;
