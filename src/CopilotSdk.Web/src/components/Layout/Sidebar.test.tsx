/**
 * Tests for the Sidebar component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { Sidebar } from './Sidebar';
import { SessionProvider, CopilotClientProvider } from '../../context';
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
  listSessions: jest.fn().mockResolvedValue({
    sessions: [
      {
        sessionId: 'session-1',
        model: 'gpt-4',
        streaming: false,
        createdAt: new Date().toISOString(),
        status: 'Active',
        messageCount: 5,
      },
      {
        sessionId: 'session-2',
        model: 'gpt-4',
        streaming: true,
        createdAt: new Date().toISOString(),
        status: 'Idle',
        messageCount: 3,
      },
    ],
    totalCount: 2,
  }),
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
      state: 'Connected',
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
      <CopilotClientProvider autoRefresh={false}>
        <SessionProvider autoConnectHub={false}>
          {ui}
        </SessionProvider>
      </CopilotClientProvider>
    </BrowserRouter>
  );
};

describe('Sidebar', () => {
  it('renders the sidebar', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByTestId('app-sidebar')).toBeInTheDocument();
  });

  it('renders default navigation items', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Client Config')).toBeInTheDocument();
    // Check for Sessions nav link (there are two "Sessions" - nav and header)
    expect(screen.getAllByText('Sessions').length).toBeGreaterThanOrEqual(1);
  });

  it('renders sessions header', () => {
    renderWithProviders(<Sidebar />);
    // Check the sessions section exists via the header
    expect(screen.getByRole('heading', { name: 'Sessions' })).toBeInTheDocument();
  });

  it('renders custom navigation items', () => {
    const customNavItems = [
      { path: '/custom', label: 'Custom Page' },
    ];
    renderWithProviders(<Sidebar navItems={customNavItems} />);
    expect(screen.getByText('Custom Page')).toBeInTheDocument();
  });
});
