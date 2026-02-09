/**
 * React context for managing session state.
 */
import React, { createContext, useContext, useReducer, useCallback, useEffect, useRef, ReactNode } from 'react';
import {
  SessionInfoResponse,
  CreateSessionRequest,
  SendMessageRequest,
  SendMessageResponse,
  SessionEvent,
  StreamingDelta,
} from '../types';
import * as api from '../api';
import { useSessionHub, HubConnectionState } from '../hooks';
import { useUser } from './UserContext';

// #region State Types

/**
 * State for the session context.
 */
interface SessionState {
  /** List of available sessions. */
  sessions: SessionInfoResponse[];
  /** Currently active session ID. */
  activeSessionId: string | null;
  /** Currently active session info. */
  activeSession: SessionInfoResponse | null;
  /** Events for the active session. */
  activeSessionEvents: SessionEvent[];
  /** Whether data is being loaded. */
  isLoading: boolean;
  /** Whether a message is being sent. */
  isSending: boolean;
  /** Last error that occurred. */
  error: string | null;
  /** SignalR hub connection state. */
  hubConnectionState: HubConnectionState;
}

/**
 * Initial state.
 */
const initialState: SessionState = {
  sessions: [],
  activeSessionId: null,
  activeSession: null,
  activeSessionEvents: [],
  isLoading: false,
  isSending: false,
  error: null,
  hubConnectionState: 'Disconnected',
};

// #endregion

// #region Actions

type SessionAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SESSIONS_SUCCESS'; payload: SessionInfoResponse[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'CLEAR_ERROR' }
  | { type: 'SET_ACTIVE_SESSION'; payload: { sessionId: string; session: SessionInfoResponse } }
  | { type: 'CLEAR_ACTIVE_SESSION' }
  | { type: 'SET_ACTIVE_SESSION_EVENTS'; payload: SessionEvent[] }
  | { type: 'ADD_SESSION_EVENT'; payload: SessionEvent }
  | { type: 'UPDATE_STREAMING_CONTENT'; payload: StreamingDelta }
  | { type: 'SESSION_CREATED'; payload: SessionInfoResponse }
  | { type: 'SESSION_DELETED'; payload: string }
  | { type: 'SET_SENDING'; payload: boolean }
  | { type: 'SET_HUB_CONNECTION_STATE'; payload: HubConnectionState }
  | { type: 'RESET_SESSIONS' };

/**
 * Reducer for the session context.
 */
function sessionReducer(state: SessionState, action: SessionAction): SessionState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null };
    case 'FETCH_SESSIONS_SUCCESS':
      return { ...state, sessions: action.payload, isLoading: false };
    case 'FETCH_ERROR':
      return { ...state, error: action.payload, isLoading: false, isSending: false };
    case 'CLEAR_ERROR':
      return { ...state, error: null };
    case 'SET_ACTIVE_SESSION':
      return {
        ...state,
        activeSessionId: action.payload.sessionId,
        activeSession: action.payload.session,
        isLoading: false,
      };
    case 'CLEAR_ACTIVE_SESSION':
      return {
        ...state,
        activeSessionId: null,
        activeSession: null,
        activeSessionEvents: [],
      };
    case 'SET_ACTIVE_SESSION_EVENTS':
      return { ...state, activeSessionEvents: action.payload };
    case 'ADD_SESSION_EVENT':
      return {
        ...state,
        activeSessionEvents: [...state.activeSessionEvents, action.payload],
      };
    case 'UPDATE_STREAMING_CONTENT':
      // Handle streaming delta by updating the last matching event
      return {
        ...state,
        activeSessionEvents: updateStreamingEvent(state.activeSessionEvents, action.payload),
      };
    case 'SESSION_CREATED':
      return {
        ...state,
        sessions: [...state.sessions, action.payload],
        isLoading: false,
      };
    case 'SESSION_DELETED':
      return {
        ...state,
        sessions: state.sessions.filter((s) => s.sessionId !== action.payload),
        activeSessionId: state.activeSessionId === action.payload ? null : state.activeSessionId,
        activeSession: state.activeSessionId === action.payload ? null : state.activeSession,
        activeSessionEvents: state.activeSessionId === action.payload ? [] : state.activeSessionEvents,
      };
    case 'SET_SENDING':
      return { ...state, isSending: action.payload };
    case 'SET_HUB_CONNECTION_STATE':
      return { ...state, hubConnectionState: action.payload };
    case 'RESET_SESSIONS':
      return { ...initialState };
    default:
      return state;
  }
}

/**
 * Helper to find the last index matching a predicate (ES2023 findLastIndex polyfill).
 */
function findLastIndex<T>(arr: T[], predicate: (value: T, index: number, array: T[]) => boolean): number {
  for (let i = arr.length - 1; i >= 0; i--) {
    if (predicate(arr[i], i, arr)) {
      return i;
    }
  }
  return -1;
}

/**
 * Helper to update streaming content in events.
 */
function updateStreamingEvent(events: SessionEvent[], delta: StreamingDelta): SessionEvent[] {
  // For streaming, we accumulate content in the existing event or add a new one
  // This is a simplified implementation - in production, you'd want more sophisticated handling
  const lastEventIndex = findLastIndex(
    events,
    (e: SessionEvent) =>
      (e.type === 'assistant.message_delta' || e.type === 'assistant.reasoning_delta') &&
      (e.data as any)?.messageId === delta.id
  );

  if (lastEventIndex >= 0) {
    const updatedEvents = [...events];
    const lastEvent = updatedEvents[lastEventIndex];
    if (lastEvent.data) {
      updatedEvents[lastEventIndex] = {
        ...lastEvent,
        data: {
          ...(lastEvent.data as any),
          deltaContent: ((lastEvent.data as any).deltaContent || '') + delta.content,
        },
      };
    }
    return updatedEvents;
  }

  return events;
}

// #endregion

// #region Context

/**
 * Context value type.
 */
interface SessionContextValue extends SessionState {
  /** Refresh the list of sessions. */
  refreshSessions: () => Promise<void>;
  /** Create a new session. */
  createSession: (request: CreateSessionRequest) => Promise<SessionInfoResponse>;
  /** Select a session as active. */
  selectSession: (sessionId: string) => Promise<void>;
  /** Clear the active session. */
  clearActiveSession: () => void;
  /** Delete a session. */
  deleteSession: (sessionId: string) => Promise<void>;
  /** Resume a session. */
  resumeSession: (sessionId: string) => Promise<SessionInfoResponse>;
  /** Send a message to the active session. */
  sendMessage: (request: SendMessageRequest) => Promise<SendMessageResponse>;
  /** Abort the active session. */
  abortSession: () => Promise<void>;
  /** Refresh messages for the active session. */
  refreshMessages: () => Promise<void>;
  /** Connect to the SignalR hub. */
  connectHub: () => Promise<void>;
  /** Disconnect from the SignalR hub. */
  disconnectHub: () => Promise<void>;
  /** Clear any error. */
  clearError: () => void;
}

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

// #endregion

// #region Provider

/**
 * Props for the SessionProvider.
 */
interface SessionProviderProps {
  children: ReactNode;
  /** Whether to auto-connect to the SignalR hub on mount. */
  autoConnectHub?: boolean;
}

/**
 * Provider component for the session context.
 */
export function SessionProvider({ children, autoConnectHub = true }: SessionProviderProps) {
  const [state, dispatch] = useReducer(sessionReducer, initialState);
  const { state: userState } = useUser();
  const prevUserIdRef = useRef<string | null | undefined>(undefined);

  // Handle session events from SignalR
  const handleSessionEvent = useCallback((sessionId: string, event: SessionEvent) => {
    // Only add events for the active session
    if (sessionId === state.activeSessionId) {
      dispatch({ type: 'ADD_SESSION_EVENT', payload: event });
    }
  }, [state.activeSessionId]);

  // Handle streaming deltas from SignalR
  const handleStreamingDelta = useCallback((delta: StreamingDelta) => {
    if (delta.sessionId === state.activeSessionId) {
      dispatch({ type: 'UPDATE_STREAMING_CONTENT', payload: delta });
    }
  }, [state.activeSessionId]);

  // Handle hub connection state changes
  const handleConnectionStateChange = useCallback((hubState: HubConnectionState) => {
    dispatch({ type: 'SET_HUB_CONNECTION_STATE', payload: hubState });
  }, []);

  // Handle hub errors - log but don't set as main error to avoid UI disruption
  const handleHubError = useCallback((error: Error) => {
    // Only log hub errors - the connection state change is enough to indicate issues
    // This prevents SignalR reconnection errors from disrupting the main UI
    console.warn('SignalR hub error:', error.message);
  }, []);

  // Initialize SignalR hub
  const {
    connectionState: hubConnectionState,
    connect: connectHub,
    disconnect: disconnectHub,
    joinSession: joinSessionHub,
    leaveSession: leaveSessionHub,
  } = useSessionHub({
    autoConnect: autoConnectHub,
    onSessionEvent: handleSessionEvent,
    onStreamingDelta: handleStreamingDelta,
    onConnectionStateChange: handleConnectionStateChange,
    onError: handleHubError,
  });

  // Refresh sessions list
  const refreshSessions = useCallback(async () => {
    try {
      dispatch({ type: 'FETCH_START' });
      const response = await api.listSessions();
      dispatch({ type: 'FETCH_SESSIONS_SUCCESS', payload: response.sessions });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to fetch sessions' });
    }
  }, []);

  // Create a new session
  const createSession = useCallback(async (request: CreateSessionRequest): Promise<SessionInfoResponse> => {
    try {
      dispatch({ type: 'FETCH_START' });
      const session = await api.createSession(request);
      dispatch({ type: 'SESSION_CREATED', payload: session });
      return session;
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to create session' });
      throw err;
    }
  }, []);

  // Select a session as active
  const selectSession = useCallback(async (sessionId: string) => {
    try {
      // Leave previous session hub group
      if (state.activeSessionId && state.activeSessionId !== sessionId) {
        try {
          await leaveSessionHub(state.activeSessionId);
        } catch {
          // Ignore errors when leaving
        }
      }

      dispatch({ type: 'FETCH_START' });
      let session = await api.getSession(sessionId);
      
      // Auto-resume inactive sessions so messages can be sent
      if (session.status?.toLowerCase() === 'inactive') {
        session = await api.resumeSession(sessionId);
        // Update the session in the list
        dispatch({ type: 'FETCH_SESSIONS_SUCCESS', payload: 
          state.sessions.map((s) => (s.sessionId === sessionId ? session : s))
        });
      }
      
      const messages = await api.getMessages(sessionId);

      dispatch({ type: 'SET_ACTIVE_SESSION', payload: { sessionId, session } });
      dispatch({ type: 'SET_ACTIVE_SESSION_EVENTS', payload: messages.events });

      // Join the session hub group
      try {
        await joinSessionHub(sessionId);
      } catch {
        // Ignore errors when joining - hub may not be connected
      }
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to select session' });
      throw err;
    }
  }, [state.activeSessionId, state.sessions, joinSessionHub, leaveSessionHub]);

  // Clear active session
  const clearActiveSession = useCallback(() => {
    if (state.activeSessionId) {
      leaveSessionHub(state.activeSessionId).catch(() => {
        // Ignore errors
      });
    }
    dispatch({ type: 'CLEAR_ACTIVE_SESSION' });
  }, [state.activeSessionId, leaveSessionHub]);

  // Delete a session
  const deleteSession = useCallback(async (sessionId: string) => {
    try {
      await api.deleteSession(sessionId);
      dispatch({ type: 'SESSION_DELETED', payload: sessionId });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to delete session' });
      throw err;
    }
  }, []);

  // Resume a session
  const resumeSession = useCallback(async (sessionId: string): Promise<SessionInfoResponse> => {
    try {
      dispatch({ type: 'FETCH_START' });
      const session = await api.resumeSession(sessionId);
      
      // Update the session in the list
      dispatch({ type: 'FETCH_SESSIONS_SUCCESS', payload: 
        state.sessions.map((s) => (s.sessionId === sessionId ? session : s))
      });
      
      return session;
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to resume session' });
      throw err;
    }
  }, [state.sessions]);

  // Send a message to the active session
  const sendMessage = useCallback(async (request: SendMessageRequest): Promise<SendMessageResponse> => {
    if (!state.activeSessionId) {
      throw new Error('No active session');
    }

    try {
      dispatch({ type: 'SET_SENDING', payload: true });
      const response = await api.sendMessage(state.activeSessionId, request);
      dispatch({ type: 'SET_SENDING', payload: false });
      return response;
    } catch (err) {
      dispatch({ type: 'SET_SENDING', payload: false });
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to send message' });
      throw err;
    }
  }, [state.activeSessionId]);

  // Abort the active session
  const abortSession = useCallback(async () => {
    if (!state.activeSessionId) {
      return;
    }

    try {
      await api.abortSession(state.activeSessionId);
      dispatch({ type: 'SET_SENDING', payload: false });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to abort session' });
      throw err;
    }
  }, [state.activeSessionId]);

  // Refresh messages for active session
  const refreshMessages = useCallback(async () => {
    if (!state.activeSessionId) {
      return;
    }

    try {
      const messages = await api.getMessages(state.activeSessionId);
      dispatch({ type: 'SET_ACTIVE_SESSION_EVENTS', payload: messages.events });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to refresh messages' });
    }
  }, [state.activeSessionId]);

  // Clear error
  const clearError = useCallback(() => {
    dispatch({ type: 'CLEAR_ERROR' });
  }, []);

  // Clear and refresh sessions when the authenticated user changes (login/logout)
  useEffect(() => {
    const currentUserId = userState.currentUser?.id ?? null;

    // Skip the very first render (initial undefined)
    if (prevUserIdRef.current === undefined) {
      prevUserIdRef.current = currentUserId;
      // Initial load
      if (currentUserId) {
        refreshSessions();
      }
      return;
    }

    // User changed
    if (currentUserId !== prevUserIdRef.current) {
      prevUserIdRef.current = currentUserId;

      // Reset all session state first
      dispatch({ type: 'RESET_SESSIONS' });

      // If a new user logged in, fetch their sessions
      if (currentUserId) {
        refreshSessions();
      }
    }
  }, [userState.currentUser?.id, refreshSessions]);

  const value: SessionContextValue = {
    ...state,
    hubConnectionState,
    refreshSessions,
    createSession,
    selectSession,
    clearActiveSession,
    deleteSession,
    resumeSession,
    sendMessage,
    abortSession,
    refreshMessages,
    connectHub,
    disconnectHub,
    clearError,
  };

  return (
    <SessionContext.Provider value={value}>
      {children}
    </SessionContext.Provider>
  );
}

// #endregion

// #region Hook

/**
 * Hook to access the session context.
 */
export function useSession(): SessionContextValue {
  const context = useContext(SessionContext);
  if (context === undefined) {
    throw new Error('useSession must be used within a SessionProvider');
  }
  return context;
}

// #endregion
