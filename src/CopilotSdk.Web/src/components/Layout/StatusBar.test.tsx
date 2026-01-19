/**
 * Tests for the StatusBar component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { StatusBar } from './StatusBar';
import { CopilotClientProvider, SessionProvider } from '../../context';
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
    sessions: [],
    totalCount: 0,
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
      <CopilotClientProvider autoRefresh={false}>
        <SessionProvider autoConnectHub={false}>
          {ui}
        </SessionProvider>
      </CopilotClientProvider>
    </BrowserRouter>
  );
};

describe('StatusBar', () => {
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
});
