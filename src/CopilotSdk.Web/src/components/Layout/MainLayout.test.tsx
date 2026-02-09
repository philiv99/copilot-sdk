/**
 * Tests for the MainLayout component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MainLayout } from './MainLayout';
import { CopilotClientProvider, SessionProvider, UserProvider } from '../../context';
import { BrowserRouter } from 'react-router-dom';

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
  getSessions: jest.fn().mockResolvedValue([]),
  listSessions: jest.fn().mockResolvedValue({
    sessions: [],
    totalCount: 0,
  }),
}));

// Mock the userApi module used by UserContext
jest.mock('../../api/userApi', () => ({
  getStoredUserId: jest.fn().mockReturnValue(null),
  setStoredUserId: jest.fn(),
  getCurrentUser: jest.fn().mockRejectedValue(new Error('Not logged in')),
  login: jest.fn(),
  logout: jest.fn(),
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

describe('MainLayout', () => {
  it('renders the main layout', () => {
    renderWithProviders(<MainLayout />);
    expect(screen.getByTestId('main-layout')).toBeInTheDocument();
  });

  it('renders header with custom title', () => {
    renderWithProviders(<MainLayout title="Custom App Title" />);
    expect(screen.getByText('Custom App Title')).toBeInTheDocument();
  });

  it('renders the tab container', () => {
    renderWithProviders(<MainLayout />);
    expect(screen.getByTestId('tab-container')).toBeInTheDocument();
  });

  it('renders status bar by default', () => {
    renderWithProviders(<MainLayout />);
    expect(screen.getByTestId('app-status-bar')).toBeInTheDocument();
  });

  it('hides status bar when showStatusBar is false', () => {
    renderWithProviders(<MainLayout showStatusBar={false} />);
    expect(screen.queryByTestId('app-status-bar')).not.toBeInTheDocument();
  });

  it('renders settings button in header', () => {
    renderWithProviders(<MainLayout />);
    expect(screen.getByTestId('settings-button')).toBeInTheDocument();
  });

  it('opens config modal when settings button is clicked', async () => {
    renderWithProviders(<MainLayout />);
    
    fireEvent.click(screen.getByTestId('settings-button'));
    
    await waitFor(() => {
      expect(screen.getByTestId('client-config-modal')).toBeInTheDocument();
    });
  });

  it('closes config modal when close button is clicked', async () => {
    renderWithProviders(<MainLayout />);
    
    // Open modal
    fireEvent.click(screen.getByTestId('settings-button'));
    
    await waitFor(() => {
      expect(screen.getByTestId('client-config-modal')).toBeInTheDocument();
    });
    
    // Close modal
    fireEvent.click(screen.getByTestId('client-config-modal-close'));
    
    await waitFor(() => {
      expect(screen.queryByTestId('client-config-modal')).not.toBeInTheDocument();
    });
  });
});
