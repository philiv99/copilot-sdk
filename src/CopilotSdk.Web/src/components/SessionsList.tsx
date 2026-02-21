/**
 * Sessions list component displaying all sessions with actions.
 */
import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSession, useUser } from '../context';
import { SessionInfoResponse } from '../types';
import { Spinner, CardSkeleton } from './Loading';
import { startDevServer, stopDevServer, getDevServerStatus } from '../api/copilotApi';
import './SessionsList.css';

/**
 * Tracks a running dev server for a session.
 */
interface RunningServer {
  pid: number;
  url: string;
}

/**
 * Props for the SessionsList component.
 */
export interface SessionsListProps {
  /** Whether to show the create session button. */
  showCreateButton?: boolean;
  /** Callback when create button is clicked. */
  onCreateClick?: () => void;
  /** Whether to show in compact mode (for sidebar). */
  compact?: boolean;
  /** Callback when a session is clicked (overrides default navigation). */
  onSessionClick?: (session: SessionInfoResponse) => void;
}

/**
 * Get status icon for a session.
 */
function getStatusIcon(status: string): string {
  switch (status.toLowerCase()) {
    case 'active':
      return '🟢';
    case 'idle':
      return '🟡';
    case 'error':
      return '🔴';
    case 'deleted':
      return '⚫';
    default:
      return '⚪';
  }
}

/**
 * Format date for display.
 */
function formatDate(dateString?: string): string {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
}

/**
 * Format relative time.
 */
function formatRelativeTime(dateString?: string): string {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSecs = Math.floor(diffMs / 1000);
  const diffMins = Math.floor(diffSecs / 60);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffSecs < 60) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString();
}

/**
 * Sessions list component.
 */
export function SessionsList({ showCreateButton = true, onCreateClick, compact = false, onSessionClick }: SessionsListProps) {
  const navigate = useNavigate();
  const {
    sessions,
    activeSessionId,
    isLoading,
    error,
    refreshSessions,
    selectSession,
    deleteSession,
    resumeSession,
    clearError,
  } = useSession();
  const { isCreatorOrAdmin } = useUser();

  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [resumingId, setResumingId] = useState<string | null>(null);
  const [playingId, setPlayingId] = useState<string | null>(null);
  const [stoppingId, setStoppingId] = useState<string | null>(null);
  const [runningServers, setRunningServers] = useState<Record<string, RunningServer>>({});

  // On mount (and when sessions change), check each session for an already-running dev server
  useEffect(() => {
    if (!sessions || sessions.length === 0) return;
    let cancelled = false;
    (async () => {
      const updates: Record<string, RunningServer> = {};
      await Promise.all(
        sessions.map(async (s) => {
          try {
            const status = await getDevServerStatus(s.sessionId);
            if (status.isRunning && status.pid && status.port) {
              updates[s.sessionId] = {
                pid: status.pid,
                url: status.url ?? `http://localhost:${status.port}`,
              };
            }
          } catch {
            // Ignore — server may not be running
          }
        })
      );
      if (!cancelled) {
        setRunningServers(prev => ({ ...prev, ...updates }));
      }
    })();
    return () => { cancelled = true; };
  }, [sessions]);

  // Handle session click to select and navigate
  const handleSessionClick = useCallback(async (session: SessionInfoResponse) => {
    // Use custom click handler if provided
    if (onSessionClick) {
      onSessionClick(session);
      return;
    }
    try {
      await selectSession(session.sessionId);
      navigate(`/sessions/${session.sessionId}`);
    } catch {
      // Error handled by context
    }
  }, [selectSession, navigate, onSessionClick]);

  // Handle delete button click
  const handleDelete = useCallback(async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    if (window.confirm('Are you sure you want to delete this session?')) {
      setDeletingId(sessionId);
      try {
        await deleteSession(sessionId);
      } catch {
        // Error handled by context
      } finally {
        setDeletingId(null);
      }
    }
  }, [deleteSession]);

  // Handle resume button click
  const handleResume = useCallback(async (e: React.MouseEvent, session: SessionInfoResponse) => {
    e.stopPropagation();
    setResumingId(session.sessionId);
    try {
      await resumeSession(session.sessionId);
      await selectSession(session.sessionId);
      navigate(`/sessions/${session.sessionId}`);
    } catch {
      // Error handled by context
    } finally {
      setResumingId(null);
    }
  }, [resumeSession, selectSession, navigate]);

  // Handle play button click - starts dev server and waits for PID response
  const handlePlay = useCallback(async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    setPlayingId(sessionId);

    try {
      const result = await startDevServer(sessionId);

      if (result.success && result.pid) {
        // Server started — track PID so button switches to Stop
        setRunningServers(prev => ({
          ...prev,
          [sessionId]: { pid: result.pid, url: result.url },
        }));
      } else {
        // Server failed to start — show the error, nothing else
        alert(`Failed to start app: ${result.message || 'Unknown error'}`);
      }
    } catch (error) {
      console.error('Error starting dev server:', error);
      alert(`Error starting app: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setPlayingId(null);
    }
  }, []);

  // Handle stop button click - stops the dev server by PID
  const handleStop = useCallback(async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    setStoppingId(sessionId);

    try {
      const server = runningServers[sessionId];
      if (!server) {
        // No tracked server — clean up UI
        setRunningServers(prev => {
          const next = { ...prev };
          delete next[sessionId];
          return next;
        });
        return;
      }

      const result = await stopDevServer(sessionId, server.pid);
      // Whether stopped or not, clean up UI state
      setRunningServers(prev => {
        const next = { ...prev };
        delete next[sessionId];
        return next;
      });

      if (!result.stopped) {
        console.warn('Stop returned not stopped:', result.message);
      }
    } catch (error) {
      console.error('Error stopping dev server:', error);
      alert(`Error stopping app: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setStoppingId(null);
    }
  }, [runningServers]);

  // Handle refresh button click
  const handleRefresh = useCallback(async () => {
    try {
      await refreshSessions();
    } catch {
      // Error handled by context
    }
  }, [refreshSessions]);

  if (compact) {
    // Compact mode for sidebar
    return (
      <div className="sessions-list sessions-list-compact" data-testid="sessions-list">
        <div className="sessions-list-header">
          <h4 className="sessions-list-title">Sessions</h4>
          <div className="sessions-list-actions">
            <button
              className="sessions-list-action-btn"
              onClick={handleRefresh}
              disabled={isLoading}
              title="Refresh sessions"
              aria-label="Refresh sessions"
            >
              🔄
            </button>
            {showCreateButton && onCreateClick && isCreatorOrAdmin && (
              <button
                className="sessions-list-action-btn sessions-list-create-btn"
                onClick={onCreateClick}
                title="Create new session"
                aria-label="Create new session"
              >
                ➕
              </button>
            )}
          </div>
        </div>

        {error && (
          <div className="sessions-list-error">
            <span>{error}</span>
            <button onClick={clearError} className="error-dismiss">×</button>
          </div>
        )}

        {isLoading && sessions.length === 0 ? (
          <div className="sessions-list-loading">
            <Spinner size="small" label="Loading sessions..." />
          </div>
        ) : sessions.length === 0 ? (
          <div className="sessions-list-empty">
            <p>No sessions yet</p>
            {showCreateButton && onCreateClick && isCreatorOrAdmin && (
              <button className="create-session-link" onClick={onCreateClick}>
                Create your first session
              </button>
            )}
          </div>
        ) : (
          <ul className="sessions-list-items">
            {sessions.map((session) => (
              <li
                key={session.sessionId}
                className={`session-item-compact ${activeSessionId === session.sessionId ? 'active' : ''}`}
                onClick={() => handleSessionClick(session)}
                data-testid={`session-item-${session.sessionId}`}
              >
                <span className="session-status-icon">{getStatusIcon(session.status)}</span>
                <span className="session-id-compact" title={session.sessionId}>
                  {session.sessionId.length > 20 
                    ? `${session.sessionId.substring(0, 20)}...` 
                    : session.sessionId}
                </span>
                <span className="session-time-compact">{formatRelativeTime(session.lastActivityAt || session.createdAt)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  }

  // Full mode for sessions page
  return (
    <div className="sessions-list" data-testid="sessions-list">
      <div className="sessions-list-header">
        <h3 className="sessions-list-title">All Sessions</h3>
        <div className="sessions-list-actions">
          <button
            className="btn btn-secondary"
            onClick={handleRefresh}
            disabled={isLoading}
          >
            {isLoading ? <><Spinner size="small" /> Refreshing...</> : 'Refresh'}
          </button>
          {showCreateButton && onCreateClick && isCreatorOrAdmin && (
            <button
              className="btn btn-primary"
              onClick={onCreateClick}
            >
              Create Session
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="sessions-list-error">
          <span>{error}</span>
          <button onClick={clearError} className="error-dismiss">×</button>
        </div>
      )}

      {isLoading && sessions.length === 0 ? (
        <div className="sessions-list-loading">
          <Spinner size="medium" label="Loading sessions..." centered />
          <p>Loading sessions...</p>
        </div>
      ) : sessions.length === 0 ? (
        <div className="sessions-list-empty-large">
          <div className="empty-icon">💬</div>
          <h4>No Sessions</h4>
          <p>Create your first session to start chatting with Copilot.</p>
          {showCreateButton && onCreateClick && isCreatorOrAdmin && (
            <button className="btn btn-primary" onClick={onCreateClick}>
              Create Session
            </button>
          )}
        </div>
      ) : (
        <div className="sessions-table-container">
          <table className="sessions-table" data-testid="sessions-table">
            <thead>
              <tr>
                <th>Status</th>
                <th>Session ID</th>
                <th>Creator</th>
                <th>Model</th>
                <th>Messages</th>
                <th>Created</th>
                <th>Last Activity</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {sessions.map((session) => (
                <tr
                  key={session.sessionId}
                  className={`session-row ${activeSessionId === session.sessionId ? 'active' : ''}`}
                  onClick={() => handleSessionClick(session)}
                  data-testid={`session-row-${session.sessionId}`}
                >
                  <td className="session-status-cell">
                    <span className="session-status-badge" data-status={session.status.toLowerCase()}>
                      {getStatusIcon(session.status)} {session.status}
                    </span>
                  </td>
                  <td className="session-id-cell" title={session.sessionId}>
                    {session.sessionId}
                  </td>
                  <td className="session-creator-cell">
                    {session.creatorDisplayName || session.creatorUserId || '—'}
                  </td>
                  <td className="session-model-cell">{session.model}</td>
                  <td className="session-messages-cell">{session.messageCount}</td>
                  <td className="session-date-cell">{formatDate(session.createdAt)}</td>
                  <td className="session-date-cell">{formatDate(session.lastActivityAt)}</td>
                  <td className="session-actions-cell">
                    <div className="session-action-buttons">
                      {runningServers[session.sessionId] ? (
                        <button
                          className="btn btn-sm btn-warning"
                          onClick={(e) => handleStop(e, session.sessionId)}
                          disabled={stoppingId === session.sessionId}
                          title="Stop the running app"
                          aria-label="Stop app"
                        >
                          {stoppingId === session.sessionId ? '⏳ Stopping...' : '⏹️ Stop'}
                        </button>
                      ) : (
                        <button
                          className="btn btn-sm btn-success"
                          onClick={(e) => handlePlay(e, session.sessionId)}
                          disabled={playingId === session.sessionId}
                          title="Start app and open in browser"
                          aria-label="Play app"
                        >
                          {playingId === session.sessionId ? '⏳ Starting...' : '▶️ Play'}
                        </button>
                      )}
                      {isCreatorOrAdmin && (
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={(e) => handleResume(e, session)}
                          disabled={resumingId === session.sessionId || session.status.toLowerCase() === 'active'}
                          title={session.status.toLowerCase() === 'active' ? 'Session is already active' : 'Resume session'}
                        >
                          {resumingId === session.sessionId ? '...' : 'Resume'}
                        </button>
                      )}
                      {isCreatorOrAdmin && (
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={(e) => handleDelete(e, session.sessionId)}
                          disabled={deletingId === session.sessionId}
                          title="Delete session"
                        >
                          {deletingId === session.sessionId ? '...' : 'Delete'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default SessionsList;
