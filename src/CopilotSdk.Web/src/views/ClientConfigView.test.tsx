/**
 * Tests for the ClientConfigView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ClientConfigView } from './ClientConfigView';
import { CopilotClientProvider } from '../context';
import * as api from '../api';

// Mock the API module
jest.mock('../api', () => ({
  getClientStatus: jest.fn(),
  getClientConfig: jest.fn(),
  updateClientConfig: jest.fn(),
  startClient: jest.fn(),
  stopClient: jest.fn(),
  forceStopClient: jest.fn(),
  pingClient: jest.fn(),
}));

const mockedApi = api as jest.Mocked<typeof api>;

const defaultStatus = {
  connectionState: 'Connected',
  isConnected: true,
  connectedAt: new Date().toISOString(),
};

const defaultConfig = {
  cliPath: '/usr/bin/copilot',
  cliArgs: ['--verbose'],
  cliUrl: 'http://localhost:8080',
  port: 3000,
  useStdio: true,
  logLevel: 'info',
  autoStart: true,
  autoRestart: false,
  cwd: '/home/user',
  environment: { NODE_ENV: 'development' },
};

const renderConfigView = async () => {
  let result;
  await act(async () => {
    result = render(
      <BrowserRouter>
        <CopilotClientProvider autoRefresh={true} refreshInterval={60000}>
          <ClientConfigView />
        </CopilotClientProvider>
      </BrowserRouter>
    );
  });
  // Wait for API calls to resolve
  await act(async () => {
    await Promise.resolve();
  });
  return result;
};

describe('ClientConfigView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockedApi.getClientStatus.mockResolvedValue(defaultStatus);
    mockedApi.getClientConfig.mockResolvedValue(defaultConfig);
  });

  describe('rendering', () => {
    it('renders the config view', async () => {
      await renderConfigView();
      expect(screen.getByTestId('client-config-view')).toBeInTheDocument();
    });

    it('renders the page title', async () => {
      await renderConfigView();
      expect(screen.getByText('Client Configuration')).toBeInTheDocument();
    });

    it('renders back link to dashboard', async () => {
      await renderConfigView();
      expect(screen.getByTestId('back-link')).toHaveAttribute('href', '/');
    });

    it('renders connection settings section', async () => {
      await renderConfigView();
      expect(screen.getByTestId('connection-settings')).toBeInTheDocument();
    });

    it('renders behavior settings section', async () => {
      await renderConfigView();
      expect(screen.getByTestId('behavior-settings')).toBeInTheDocument();
    });

    it('renders process settings section', async () => {
      await renderConfigView();
      expect(screen.getByTestId('process-settings')).toBeInTheDocument();
    });

    it('renders action buttons section', async () => {
      await renderConfigView();
      expect(screen.getByTestId('config-actions')).toBeInTheDocument();
    });
  });

  describe('form fields', () => {
    it('populates CLI path from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('cli-path-input')).toHaveValue(
          '/usr/bin/copilot'
        );
      });
    });

    it('populates CLI URL from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('cli-url-input')).toHaveValue(
          'http://localhost:8080'
        );
      });
    });

    it('populates port from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('port-input')).toHaveValue(3000);
      });
    });

    it('populates use stdio from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('use-stdio-checkbox')).toBeChecked();
      });
    });

    it('populates log level from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('log-level-select')).toHaveValue('info');
      });
    });

    it('populates auto start from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('auto-start-checkbox')).toBeChecked();
      });
    });

    it('populates auto restart from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('auto-restart-checkbox')).not.toBeChecked();
      });
    });

    it('populates working directory from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('cwd-input')).toHaveValue('/home/user');
      });
    });

    it('populates CLI args from config', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('cli-args-input')).toHaveValue('--verbose');
      });
    });
  });

  describe('form interactions', () => {
    it('enables save button when form is modified', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('save-button')).toBeDisabled();
      });

      await act(async () => {
        fireEvent.change(screen.getByTestId('cli-path-input'), {
          target: { value: '/new/path' },
        });
      });

      await waitFor(() => {
        expect(screen.getByTestId('save-button')).not.toBeDisabled();
      });
    });

    it('enables reset button when form is modified', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('reset-button')).toBeDisabled();
      });

      await act(async () => {
        fireEvent.change(screen.getByTestId('port-input'), {
          target: { value: '4000' },
        });
      });

      await waitFor(() => {
        expect(screen.getByTestId('reset-button')).not.toBeDisabled();
      });
    });

    it('calls updateClientConfig when save clicked', async () => {
      mockedApi.updateClientConfig.mockResolvedValue(defaultConfig);
      await renderConfigView();

      await waitFor(() => {
        expect(screen.getByTestId('cli-path-input')).toHaveValue(
          '/usr/bin/copilot'
        );
      });

      await act(async () => {
        fireEvent.change(screen.getByTestId('cli-path-input'), {
          target: { value: '/new/path' },
        });
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('save-button'));
      });

      await waitFor(() => {
        expect(mockedApi.updateClientConfig).toHaveBeenCalled();
      });
    });

    it('shows success message after save', async () => {
      mockedApi.updateClientConfig.mockResolvedValue(defaultConfig);
      await renderConfigView();

      await waitFor(() => {
        expect(screen.getByTestId('cli-path-input')).toHaveValue(
          '/usr/bin/copilot'
        );
      });

      await act(async () => {
        fireEvent.change(screen.getByTestId('cli-path-input'), {
          target: { value: '/new/path' },
        });
      });

      await act(async () => {
        fireEvent.click(screen.getByTestId('save-button'));
      });

      await waitFor(() => {
        expect(screen.getByTestId('config-success')).toBeInTheDocument();
      });
    });

    it('resets form when reset button clicked', async () => {
      await renderConfigView();

      await waitFor(() => {
        expect(screen.getByTestId('cli-path-input')).toHaveValue(
          '/usr/bin/copilot'
        );
      });

      await act(async () => {
        fireEvent.change(screen.getByTestId('cli-path-input'), {
          target: { value: '/new/path' },
        });
      });

      expect(screen.getByTestId('cli-path-input')).toHaveValue('/new/path');

      await act(async () => {
        fireEvent.click(screen.getByTestId('reset-button'));
      });

      await waitFor(() => {
        expect(screen.getByTestId('cli-path-input')).toHaveValue(
          '/usr/bin/copilot'
        );
      });
    });
  });

  describe('client actions', () => {
    it('calls startClient when start button clicked', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      mockedApi.startClient.mockResolvedValue(defaultStatus);
      await renderConfigView();

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
      await renderConfigView();

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
      await renderConfigView();

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

    it('calls pingClient when ping button clicked', async () => {
      mockedApi.pingClient.mockResolvedValue({
        success: true,
        message: 'Pong',
        latencyMs: 42,
      });
      await renderConfigView();

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

    it('disables start button when connected', async () => {
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('start-button')).toBeDisabled();
      });
    });

    it('disables stop button when disconnected', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('stop-button')).toBeDisabled();
      });
    });

    it('disables ping button when disconnected', async () => {
      mockedApi.getClientStatus.mockResolvedValue({
        connectionState: 'Disconnected',
        isConnected: false,
      });
      await renderConfigView();
      await waitFor(() => {
        expect(screen.getByTestId('ping-button')).toBeDisabled();
      });
    });
  });

  describe('error handling', () => {
    it('can display error message from context', async () => {
      // Error is initially null, so no error display
      await renderConfigView();
      expect(screen.queryByTestId('config-error')).not.toBeInTheDocument();
    });
  });

  describe('log level options', () => {
    it('has all log level options', async () => {
      await renderConfigView();
      await waitFor(() => {
        const select = screen.getByTestId('log-level-select');
        expect(select).toBeInTheDocument();
      });

      const options = screen.getAllByRole('option');
      const logLevelOptions = options.map((o) => o.textContent);
      expect(logLevelOptions).toContain('Debug');
      expect(logLevelOptions).toContain('Info');
      expect(logLevelOptions).toContain('Warn');
      expect(logLevelOptions).toContain('Error');
    });
  });
});
