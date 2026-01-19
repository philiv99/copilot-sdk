/**
 * React hook for managing SignalR connection to the SessionHub.
 */
import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { SessionEvent, StreamingDelta } from '../types';

/**
 * SignalR hub URL - direct communication with backend.
 */
const HUB_URL = 'http://localhost:5139/hubs/session';

/**
 * Connection state for the SignalR hub.
 */
export type HubConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting';

/**
 * Options for the useSessionHub hook.
 */
export interface UseSessionHubOptions {
  /** Whether to automatically connect on mount. */
  autoConnect?: boolean;
  /** Callback when a session event is received. */
  onSessionEvent?: (sessionId: string, event: SessionEvent) => void;
  /** Callback when a streaming delta is received. */
  onStreamingDelta?: (delta: StreamingDelta) => void;
  /** Callback when connection state changes. */
  onConnectionStateChange?: (state: HubConnectionState) => void;
  /** Callback when an error occurs. */
  onError?: (error: Error) => void;
}

/**
 * Return type for the useSessionHub hook.
 */
export interface UseSessionHubResult {
  /** Current connection state. */
  connectionState: HubConnectionState;
  /** Connect to the hub. */
  connect: () => Promise<void>;
  /** Disconnect from the hub. */
  disconnect: () => Promise<void>;
  /** Join a session group to receive events. */
  joinSession: (sessionId: string) => Promise<void>;
  /** Leave a session group. */
  leaveSession: (sessionId: string) => Promise<void>;
  /** Set of session IDs currently joined. */
  joinedSessions: Set<string>;
  /** Last error that occurred. */
  error: Error | null;
}

/**
 * Hook for managing SignalR connection to the SessionHub.
 */
export function useSessionHub(options: UseSessionHubOptions = {}): UseSessionHubResult {
  const {
    autoConnect = false,
    onSessionEvent,
    onStreamingDelta,
    onConnectionStateChange,
    onError,
  } = options;

  const [connectionState, setConnectionState] = useState<HubConnectionState>('Disconnected');
  const [joinedSessions, setJoinedSessions] = useState<Set<string>>(new Set());
  const [error, setError] = useState<Error | null>(null);

  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const joinedSessionsRef = useRef<Set<string>>(new Set());

  // Update connection state and notify listener
  const updateConnectionState = useCallback((state: HubConnectionState) => {
    setConnectionState(state);
    onConnectionStateChange?.(state);
  }, [onConnectionStateChange]);

  // Handle errors
  const handleError = useCallback((err: Error) => {
    setError(err);
    onError?.(err);
  }, [onError]);

  // Create the SignalR connection
  const createConnection = useCallback(() => {
    if (connectionRef.current) {
      return connectionRef.current;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        withCredentials: true, // Required for cross-origin requests with CORS
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 1, 2, 4, 8, 16, 32 seconds, max 32
          if (retryContext.previousRetryCount >= 7) {
            return 32000;
          }
          return Math.pow(2, retryContext.previousRetryCount) * 1000;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Handle session events
    connection.on('OnSessionEvent', (sessionId: string, event: SessionEvent) => {
      onSessionEvent?.(sessionId, event);
    });

    // Handle streaming deltas
    connection.on('OnStreamingDelta', (delta: StreamingDelta) => {
      onStreamingDelta?.(delta);
    });

    // Handle connection state changes
    connection.onreconnecting((error) => {
      updateConnectionState('Reconnecting');
      if (error) {
        handleError(new Error(`Connection lost: ${error.message}`));
      }
    });

    connection.onreconnected(async (connectionId) => {
      updateConnectionState('Connected');
      setError(null);
      
      // Rejoin all sessions after reconnection
      for (const sessionId of joinedSessionsRef.current) {
        try {
          await connection.invoke('JoinSession', sessionId);
        } catch (err) {
          console.error(`Failed to rejoin session ${sessionId}:`, err);
        }
      }
    });

    connection.onclose((error) => {
      updateConnectionState('Disconnected');
      if (error) {
        handleError(new Error(`Connection closed: ${error.message}`));
      }
    });

    connectionRef.current = connection;
    return connection;
  }, [onSessionEvent, onStreamingDelta, updateConnectionState, handleError]);

  // Connect to the hub
  const connect = useCallback(async () => {
    try {
      const connection = createConnection();
      
      if (connection.state === signalR.HubConnectionState.Connected) {
        return;
      }

      if (connection.state === signalR.HubConnectionState.Connecting) {
        // Wait for existing connection attempt
        return;
      }

      updateConnectionState('Connecting');
      setError(null);

      await connection.start();
      updateConnectionState('Connected');
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to connect');
      handleError(error);
      updateConnectionState('Disconnected');
      throw error;
    }
  }, [createConnection, updateConnectionState, handleError]);

  // Disconnect from the hub
  const disconnect = useCallback(async () => {
    const connection = connectionRef.current;
    if (!connection) {
      return;
    }

    try {
      // Leave all sessions before disconnecting
      for (const sessionId of joinedSessionsRef.current) {
        try {
          await connection.invoke('LeaveSession', sessionId);
        } catch (err) {
          console.error(`Failed to leave session ${sessionId}:`, err);
        }
      }

      await connection.stop();
      joinedSessionsRef.current.clear();
      setJoinedSessions(new Set());
      updateConnectionState('Disconnected');
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to disconnect');
      handleError(error);
      throw error;
    }
  }, [updateConnectionState, handleError]);

  // Join a session group
  const joinSession = useCallback(async (sessionId: string) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    if (joinedSessionsRef.current.has(sessionId)) {
      return; // Already joined
    }

    try {
      await connection.invoke('JoinSession', sessionId);
      joinedSessionsRef.current.add(sessionId);
      setJoinedSessions(new Set(joinedSessionsRef.current));
    } catch (err) {
      const error = err instanceof Error ? err : new Error(`Failed to join session ${sessionId}`);
      handleError(error);
      throw error;
    }
  }, [handleError]);

  // Leave a session group
  const leaveSession = useCallback(async (sessionId: string) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    if (!joinedSessionsRef.current.has(sessionId)) {
      return; // Not joined
    }

    try {
      await connection.invoke('LeaveSession', sessionId);
      joinedSessionsRef.current.delete(sessionId);
      setJoinedSessions(new Set(joinedSessionsRef.current));
    } catch (err) {
      const error = err instanceof Error ? err : new Error(`Failed to leave session ${sessionId}`);
      handleError(error);
      throw error;
    }
  }, [handleError]);

  // Auto-connect on mount if enabled
  useEffect(() => {
    let isMounted = true;
    
    if (autoConnect) {
      // Small delay to handle React StrictMode double-mounting
      const timeoutId = setTimeout(() => {
        if (isMounted) {
          connect().catch(() => {
            // Error is already handled in connect()
          });
        }
      }, 100);
      
      return () => {
        isMounted = false;
        clearTimeout(timeoutId);
        if (connectionRef.current) {
          connectionRef.current.stop().catch(() => {
            // Ignore errors on cleanup
          });
          connectionRef.current = null;
        }
      };
    }

    // Cleanup on unmount
    return () => {
      isMounted = false;
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {
          // Ignore errors on cleanup
        });
        connectionRef.current = null;
      }
    };
  }, [autoConnect, connect]);

  return {
    connectionState,
    connect,
    disconnect,
    joinSession,
    leaveSession,
    joinedSessions,
    error,
  };
}
