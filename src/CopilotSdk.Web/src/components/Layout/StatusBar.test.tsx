/**
 * Tests for the StatusBar component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { StatusBar } from './StatusBar';
import { CopilotClientProvider, SessionProvider, UserProvider } from '../../context';
import { BrowserRouter } from 'react-router-dom';
import * as api from '../../api';

// Mock the user API module
jest.mock('../../api/userApi', () => ({
  getStoredUserId: jest.fn().mockReturnValue(null),
  setStoredUserId: jest.fn(),
  getCurrentUser: jest.fn().mockRejectedValue(new Error('Not authenticated')),
  login: jest.fn(),
  logout: jest.fn().mockResolvedValue(undefined),
  register: jest.fn(),
  updateProfile: jest.fn(),
  changePassword: jest.fn(),
}));

// Mock the API module
jest.mock('../../api', () => ({
  getClientStatus: jest.fn().mockResolvedValue({
    connectionState: 'Connected',
    isConnected: true,
  }),
  getClientConfig: jest.fn().mockResolvedValue({
    cliPath: '/usr/bin/copilot',
    port: 0,
    useStdio: true,
    logLevel: 'info',
    autoStart: true,
    autoRestart: true,
  }),
  listSessions: jest.fn().mockResolvedValue({
    sessions: [],
    totalCount: 0,
  }),
  startClient: jest.fn().mockResolvedValue({ connectionState: 'Connected', isConnected: true }),
  stopClient: jest.fn().mockResolvedValue({ connectionState: 'Disconnected', isConnected: false }),
  forceStopClient: jest.fn().mockResolvedValue({ connectionState: 'Disconnected', isConnected: false }),
  pingClient: jest.fn().mockResolvedValue({ success: true, message: 'pong', latencyMs: 10 }),
}));

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockReturnValue({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    configureLogging: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      on: jest.fn(),
      off: jest.fn(),
      invoke: jest.fn().mockResolvedValue(undefined),
      onreconnecting: jest.fn(),
      onreconnected: jest.fn(),
      onclose: jest.fn(),
      state: 'Disconnected',
    }),
  }),
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  },
  LogLevel: {
    Information: 1,
  },
}));

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <UserProvider>
        <CopilotClientProvider autoRefresh={false}>
          <SessionProvider autoConnectHub={false}>
            {ui}
          </SessionProvider>
        </CopilotClientProvider>
      </UserProvider>
    </BrowserRouter>
  );
};

describe('StatusBar', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the status bar', () => {
    renderWithProviders(<StatusBar />);
    expect(screen.getByTestId('app-status-bar')).toBeInTheDocument();
  });

  it('displays client status', () => {
    renderWithProviders(<StatusBar />);
    expect(screen.getByText(/Client:/)).toBeInTheDocument();
  });

  it('displays hub status', () => {
    renderWithProviders(<StatusBar />);
    expect(screen.getByText(/Hub:/)).toBeInTheDocument();
  });

  it('renders children content', () => {
    renderWithProviders(
      <StatusBar>
        <span data-testid="custom-content">Custom Content</span>
      </StatusBar>
    );
    expect(screen.getByTestId('custom-content')).toBeInTheDocument();
  });

  it('renders quick action buttons', () => {
    renderWithProviders(<StatusBar />);
    expect(screen.getByTestId('status-bar-actions')).toBeInTheDocument();
  });

  it('renders ping button', () => {
    renderWithProviders(<StatusBar />);
    expect(screen.getByTestId('status-ping-btn')).toBeInTheDocument();
  });

  it('shows start button when disconnected', async () => {
    (api.getClientStatus as jest.Mock).mockResolvedValueOnce({
      connectionState: 'Disconnected',
      isConnected: false,
    });
    renderWithProviders(<StatusBar />);
    await waitFor(() => {
      expect(screen.getByTestId('status-start-btn')).toBeInTheDocument();
    });
  });

  it('calls startClient when start button is clicked', async () => {
    (api.getClientStatus as jest.Mock).mockResolvedValueOnce({
      connectionState: 'Disconnected',
      isConnected: false,
    });
    renderWithProviders(<StatusBar />);
    
    await waitFor(() => {
      expect(screen.getByTestId('status-start-btn')).toBeInTheDocument();
    });
    
    fireEvent.click(screen.getByTestId('status-start-btn'));
    
    await waitFor(() => {
      expect(api.startClient).toHaveBeenCalled();
    });
  });
});
