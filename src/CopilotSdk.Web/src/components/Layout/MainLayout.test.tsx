/**
 * Tests for the MainLayout component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { MainLayout } from './MainLayout';
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

describe('MainLayout', () => {
  it('renders the main layout', () => {
    renderWithProviders(
      <MainLayout>
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.getByTestId('main-layout')).toBeInTheDocument();
  });

  it('renders children in the content area', () => {
    renderWithProviders(
      <MainLayout>
        <div data-testid="test-content">Test Content</div>
      </MainLayout>
    );
    expect(screen.getByTestId('test-content')).toBeInTheDocument();
    expect(screen.getByTestId('layout-content')).toContainElement(screen.getByTestId('test-content'));
  });

  it('renders header with custom title', () => {
    renderWithProviders(
      <MainLayout title="Custom App Title">
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.getByText('Custom App Title')).toBeInTheDocument();
  });

  it('renders sidebar by default', () => {
    renderWithProviders(
      <MainLayout>
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.getByTestId('app-sidebar')).toBeInTheDocument();
  });

  it('hides sidebar when showSidebar is false', () => {
    renderWithProviders(
      <MainLayout showSidebar={false}>
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.queryByTestId('app-sidebar')).not.toBeInTheDocument();
  });

  it('renders status bar by default', () => {
    renderWithProviders(
      <MainLayout>
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.getByTestId('app-status-bar')).toBeInTheDocument();
  });

  it('hides status bar when showStatusBar is false', () => {
    renderWithProviders(
      <MainLayout showStatusBar={false}>
        <div>Test Content</div>
      </MainLayout>
    );
    expect(screen.queryByTestId('app-status-bar')).not.toBeInTheDocument();
  });
});
