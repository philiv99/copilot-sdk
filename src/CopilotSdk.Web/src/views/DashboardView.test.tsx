/**
 * Tests for the DashboardView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { DashboardView } from './DashboardView';
import { CopilotClientProvider, SessionProvider } from '../context';
import * as api from '../api';

// Mock the API module
jest.mock('../api', () => ({
  getClientStatus: jest.fn(),
  getClientConfig: jest.fn(),
  startClient: jest.fn(),
  stopClient: jest.fn(),
  forceStopClient: jest.fn(),
  pingClient: jest.fn(),
  listSessions: jest.fn(),
  createSession: jest.fn(),
  deleteSession: jest.fn(),
  resumeSession: jest.fn(),
  getSession: jest.fn(),
  sendMessage: jest.fn(),
  abortSession: jest.fn(),
  getMessages: jest.fn(),
}));

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockReturnValue({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      on: jest.fn(),
      off: jest.fn(),
      invoke: jest.fn().mockResolvedValue(undefined),
      state: 'Disconnected',
      onclose: jest.fn(),
      onreconnecting: jest.fn(),
      onreconnected: jest.fn(),
    }),
  }),
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  },
}));

const mockedApi = api as jest.Mocked<typeof api>;

const defaultStatus = {
  connectionState: 'Connected',
  isConnected: true,
  connectedAt: new Date().toISOString(),
};

const defaultConfig = {
  cliPath: '/usr/bin/copilot',
  port: 0,
  useStdio: true,
  logLevel: 'info',
  autoStart: true,
  autoRestart: true,
};

const defaultSessions = [
  {
    sessionId: 'session-1',
    model: 'gpt-4',
    streaming: true,
    createdAt: new Date().toISOString(),
    status: 'Active',
    messageCount: 5,
  },
  {
    sessionId: 'session-2',
    model: 'gpt-3.5-turbo',
    streaming: false,
    createdAt: new Date().toISOString(),
    status: 'Idle',
    messageCount: 2,
  },
];

const renderDashboard = async () => {
  let result;
  await act(async () => {
    result = render(
      <BrowserRouter>
        <CopilotClientProvider autoRefresh={true} refreshInterval={60000}>
          <SessionProvider autoConnectHub={false}>
            <DashboardView />
          </SessionProvider>
        </CopilotClientProvider>
      </BrowserRouter>
    );
  });
  // Wait for all API calls to resolve (status, config, sessions)
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
  return result;
};

describe('DashboardView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockedApi.getClientStatus.mockResolvedValue(defaultStatus);
    mockedApi.getClientConfig.mockResolvedValue(defaultConfig);
    mockedApi.listSessions.mockResolvedValue({
      sessions: defaultSessions,
      totalCount: defaultSessions.length,
    });
  });

  describe('rendering', () => {
    it('renders the dashboard view', async () => {
      await renderDashboard();
      expect(screen.getByTestId('dashboard-view')).toBeInTheDocument();
    });

    it('renders the welcome message', async () => {
      await renderDashboard();
      expect(screen.getByText('Dashboard')).toBeInTheDocument();
      expect(
        screen.getByText('Welcome to the Copilot SDK application.')
      ).toBeInTheDocument();
    });

    it('renders status card', async () => {
      await renderDashboard();
      expect(screen.getByTestId('status-card')).toBeInTheDocument();
    });

    it('renders quick actions card', async () => {
      await renderDashboard();
      expect(screen.getByTestId('quick-actions-card')).toBeInTheDocument();
    });

    it('renders ping card', async () => {
      await renderDashboard();
      expect(screen.getByTestId('ping-card')).toBeInTheDocument();
    });

    it('renders sessions card', async () => {
      await renderDashboard();
      expect(screen.getByTestId('sessions-card')).toBeInTheDocument();
    });
  });

  describe('connection status', () => {
    it('displays connection state', async () => {
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('connection-state')).toHaveTextContent(
          'Connected'
        );
      });
    });

    it('displays is connected status', async () => {
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('is-connected')).toHaveTextContent('Yes');
      });
    });

    it('displays disconnected status when not connected', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('is-connected')).toHaveTextContent('No');
      });
    });
  });

  describe('quick actions', () => {
    it('renders start button', async () => {
      await renderDashboard();
      expect(screen.getByTestId('start-button')).toBeInTheDocument();
    });

    it('renders stop button', async () => {
      await renderDashboard();
      expect(screen.getByTestId('stop-button')).toBeInTheDocument();
    });

    it('renders force stop button', async () => {
      await renderDashboard();
      expect(screen.getByTestId('force-stop-button')).toBeInTheDocument();
    });

    it('renders configure link', async () => {
      await renderDashboard();
      expect(screen.getByTestId('config-link')).toBeInTheDocument();
      expect(screen.getByTestId('config-link')).toHaveAttribute('href', '/config');
    });

    it('calls startClient when start button clicked', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      mockedApi.startClient.mockResolvedValue(defaultStatus);
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('start-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('start-button'));
      });

      await waitFor(() => {
        expect(mockedApi.startClient).toHaveBeenCalled();
      });
    });

    it('calls stopClient when stop button clicked', async () => {
      mockedApi.stopClient.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('stop-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('stop-button'));
      });

      await waitFor(() => {
        expect(mockedApi.stopClient).toHaveBeenCalled();
      });
    });

    it('calls forceStopClient when force stop button clicked', async () => {
      mockedApi.forceStopClient.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('force-stop-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('force-stop-button'));
      });

      await waitFor(() => {
        expect(mockedApi.forceStopClient).toHaveBeenCalled();
      });
    });

    it('disables start button when connected', async () => {
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('start-button')).toBeDisabled();
      });
    });

    it('disables stop button when disconnected', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('stop-button')).toBeDisabled();
      });
    });
  });

  describe('ping functionality', () => {
    it('renders ping button', async () => {
      await renderDashboard();
      expect(screen.getByTestId('ping-button')).toBeInTheDocument();
    });

    it('calls pingClient when ping button clicked', async () => {
      mockedApi.pingClient.mockResolvedValue({
        success: true,
        message: 'Pong',
        latencyMs: 42,
      });
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('ping-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('ping-button'));
      });

      await waitFor(() => {
        expect(mockedApi.pingClient).toHaveBeenCalled();
      });
    });

    it('displays ping result after successful ping', async () => {
      mockedApi.pingClient.mockResolvedValue({
        success: true,
        message: 'Pong',
        latencyMs: 42,
      });
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('ping-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('ping-button'));
      });

      await waitFor(() => {
        expect(screen.getByTestId('ping-result')).toBeInTheDocument();
        expect(screen.getByText('42ms')).toBeInTheDocument();
      });
    });

    it('disables ping button when not connected', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('ping-button')).toBeDisabled();
      });
    });
  });

  describe('sessions list', () => {
    it('displays recent sessions', async () => {
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('sessions-list')).toBeInTheDocument();
      });
    });

    it('displays no sessions message when empty', async () => {
      mockedApi.listSessions.mockResolvedValue({
        sessions: [],
        totalCount: 0,
      });
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('no-sessions')).toBeInTheDocument();
      });
    });

    it('renders view all sessions link', async () => {
      await renderDashboard();
      expect(screen.getByTestId('view-all-sessions')).toHaveAttribute(
        'href',
        '/sessions'
      );
    });

    it('renders refresh sessions button', async () => {
      await renderDashboard();
      expect(screen.getByTestId('refresh-sessions-button')).toBeInTheDocument();
    });
  });

  describe('configuration summary', () => {
    it('displays configuration summary card when config is loaded', async () => {
      await renderDashboard();
      await waitFor(() => {
        expect(screen.getByTestId('config-summary-card')).toBeInTheDocument();
      });
    });
  });

  describe('error handling', () => {
    it('can display error message from context', async () => {
      // Error is initially null, so no error display
      await renderDashboard();
      expect(screen.queryByTestId('dashboard-error')).not.toBeInTheDocument();
    });

    it('shows ping error when ping fails', async () => {
      mockedApi.pingClient.mockRejectedValue(new Error('Ping failed'));
      await renderDashboard();

      await waitFor(() => {
        expect(screen.getByTestId('ping-button')).not.toBeDisabled();
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('ping-button'));
      });

      await waitFor(() => {
        expect(screen.getByTestId('ping-error')).toBeInTheDocument();
      });
    });
  });
});
