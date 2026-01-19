/**
 * Tests for the Header component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { Header } from './Header';
import { CopilotClientProvider } from '../../context';
import { BrowserRouter } from 'react-router-dom';

// Mock the API module
jest.mock('../../api', () => ({
  getClientStatus: jest.fn().mockResolvedValue({
    connectionState: 'Connected',
    isConnected: true,
    connectedAt: new Date().toISOString(),
  }),
  getClientConfig: jest.fn().mockResolvedValue({
    cliPath: '/usr/bin/copilot',
    port: 0,
    useStdio: true,
    logLevel: 'info',
    autoStart: true,
    autoRestart: true,
  }),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <CopilotClientProvider autoRefresh={false}>
        {ui}
      </CopilotClientProvider>
    </BrowserRouter>
  );
};

describe('Header', () => {
  it('renders with default title', () => {
    renderWithProviders(<Header />);
    expect(screen.getByText('Copilot SDK')).toBeInTheDocument();
  });

  it('renders with custom title', () => {
    renderWithProviders(<Header title="Custom Title" />);
    expect(screen.getByText('Custom Title')).toBeInTheDocument();
  });

  it('has the app-header test id', () => {
    renderWithProviders(<Header />);
    expect(screen.getByTestId('app-header')).toBeInTheDocument();
  });

  it('displays status indicator', () => {
    renderWithProviders(<Header />);
    expect(screen.getByTestId('status-indicator')).toBeInTheDocument();
  });
});
