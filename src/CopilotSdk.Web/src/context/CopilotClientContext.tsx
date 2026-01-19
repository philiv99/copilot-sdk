/**
 * React context for managing Copilot client state.
 */
import React, { createContext, useContext, useReducer, useCallback, useEffect, ReactNode } from 'react';
import {
  CopilotClientConfig,
  ClientStatusResponse,
  PingResponse,
  ConnectionState,
} from '../types';
import * as api from '../api';

// #region State Types

/**
 * State for the Copilot client context.
 */
interface CopilotClientState {
  /** Current client status. */
  status: ClientStatusResponse | null;
  /** Current client configuration. */
  config: CopilotClientConfig | null;
  /** Whether data is being loaded. */
  isLoading: boolean;
  /** Last error that occurred. */
  error: string | null;
  /** Last ping result. */
  lastPing: PingResponse | null;
}

/**
 * Initial state.
 */
const initialState: CopilotClientState = {
  status: null,
  config: null,
  isLoading: false,
  error: null,
  lastPing: null,
};

// #endregion

// #region Actions

type CopilotClientAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_STATUS_SUCCESS'; payload: ClientStatusResponse }
  | { type: 'FETCH_CONFIG_SUCCESS'; payload: CopilotClientConfig }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'CLEAR_ERROR' }
  | { type: 'PING_SUCCESS'; payload: PingResponse }
  | { type: 'SET_LOADING'; payload: boolean };

/**
 * Reducer for the Copilot client context.
 */
function copilotClientReducer(state: CopilotClientState, action: CopilotClientAction): CopilotClientState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null };
    case 'FETCH_STATUS_SUCCESS':
      return { ...state, status: action.payload, isLoading: false };
    case 'FETCH_CONFIG_SUCCESS':
      return { ...state, config: action.payload, isLoading: false };
    case 'FETCH_ERROR':
      return { ...state, error: action.payload, isLoading: false };
    case 'CLEAR_ERROR':
      return { ...state, error: null };
    case 'PING_SUCCESS':
      return { ...state, lastPing: action.payload };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    default:
      return state;
  }
}

// #endregion

// #region Context

/**
 * Context value type.
 */
interface CopilotClientContextValue extends CopilotClientState {
  /** Refresh client status. */
  refreshStatus: () => Promise<void>;
  /** Refresh client configuration. */
  refreshConfig: () => Promise<void>;
  /** Update client configuration. */
  updateConfig: (config: Partial<CopilotClientConfig>) => Promise<void>;
  /** Start the client. */
  startClient: () => Promise<void>;
  /** Stop the client. */
  stopClient: () => Promise<void>;
  /** Force stop the client. */
  forceStopClient: () => Promise<void>;
  /** Ping the client. */
  pingClient: (message?: string) => Promise<PingResponse>;
  /** Clear any error. */
  clearError: () => void;
  /** Derived: Whether the client is connected. */
  isConnected: boolean;
  /** Derived: Current connection state. */
  connectionState: ConnectionState;
}

const CopilotClientContext = createContext<CopilotClientContextValue | undefined>(undefined);

// #endregion

// #region Provider

/**
 * Props for the CopilotClientProvider.
 */
interface CopilotClientProviderProps {
  children: ReactNode;
  /** Whether to auto-refresh status on mount. */
  autoRefresh?: boolean;
  /** Refresh interval in milliseconds. */
  refreshInterval?: number;
}

/**
 * Provider component for the Copilot client context.
 */
export function CopilotClientProvider({
  children,
  autoRefresh = true,
  refreshInterval = 5000,
}: CopilotClientProviderProps) {
  const [state, dispatch] = useReducer(copilotClientReducer, initialState);

  // Refresh client status
  const refreshStatus = useCallback(async () => {
    try {
      dispatch({ type: 'FETCH_START' });
      const status = await api.getClientStatus();
      dispatch({ type: 'FETCH_STATUS_SUCCESS', payload: status });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to fetch status' });
    }
  }, []);

  // Refresh client configuration
  const refreshConfig = useCallback(async () => {
    try {
      dispatch({ type: 'FETCH_START' });
      const config = await api.getClientConfig();
      dispatch({ type: 'FETCH_CONFIG_SUCCESS', payload: config });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to fetch config' });
    }
  }, []);

  // Update client configuration
  const updateConfig = useCallback(async (config: Partial<CopilotClientConfig>) => {
    try {
      dispatch({ type: 'FETCH_START' });
      const updated = await api.updateClientConfig(config);
      dispatch({ type: 'FETCH_CONFIG_SUCCESS', payload: updated });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to update config' });
      throw err;
    }
  }, []);

  // Start the client
  const startClient = useCallback(async () => {
    try {
      dispatch({ type: 'SET_LOADING', payload: true });
      const status = await api.startClient();
      dispatch({ type: 'FETCH_STATUS_SUCCESS', payload: status });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to start client' });
      throw err;
    }
  }, []);

  // Stop the client
  const stopClient = useCallback(async () => {
    try {
      dispatch({ type: 'SET_LOADING', payload: true });
      const status = await api.stopClient();
      dispatch({ type: 'FETCH_STATUS_SUCCESS', payload: status });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to stop client' });
      throw err;
    }
  }, []);

  // Force stop the client
  const forceStopClient = useCallback(async () => {
    try {
      dispatch({ type: 'SET_LOADING', payload: true });
      const status = await api.forceStopClient();
      dispatch({ type: 'FETCH_STATUS_SUCCESS', payload: status });
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to force stop client' });
      throw err;
    }
  }, []);

  // Ping the client
  const pingClient = useCallback(async (message?: string) => {
    try {
      const result = await api.pingClient(message ? { message } : undefined);
      dispatch({ type: 'PING_SUCCESS', payload: result });
      return result;
    } catch (err) {
      dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Failed to ping client' });
      throw err;
    }
  }, []);

  // Clear error
  const clearError = useCallback(() => {
    dispatch({ type: 'CLEAR_ERROR' });
  }, []);

  // Auto-refresh status on mount and at interval
  useEffect(() => {
    if (autoRefresh) {
      // Initial fetch
      refreshStatus();
      refreshConfig();

      // Set up interval
      const intervalId = setInterval(() => {
        refreshStatus();
      }, refreshInterval);

      return () => clearInterval(intervalId);
    }
  }, [autoRefresh, refreshInterval, refreshStatus, refreshConfig]);

  // Derived values
  const isConnected = state.status?.isConnected ?? false;
  const connectionState: ConnectionState = (state.status?.connectionState as ConnectionState) ?? 'Disconnected';

  const value: CopilotClientContextValue = {
    ...state,
    refreshStatus,
    refreshConfig,
    updateConfig,
    startClient,
    stopClient,
    forceStopClient,
    pingClient,
    clearError,
    isConnected,
    connectionState,
  };

  return (
    <CopilotClientContext.Provider value={value}>
      {children}
    </CopilotClientContext.Provider>
  );
}

// #endregion

// #region Hook

/**
 * Hook to access the Copilot client context.
 */
export function useCopilotClient(): CopilotClientContextValue {
  const context = useContext(CopilotClientContext);
  if (context === undefined) {
    throw new Error('useCopilotClient must be used within a CopilotClientProvider');
  }
  return context;
}

// #endregion
