/**
 * Tests for the ClientConfigModal component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ClientConfigModal } from './ClientConfigModal';
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

describe('ClientConfigModal', () => {
  it('does not render when isOpen is false', () => {
    renderWithProviders(<ClientConfigModal isOpen={false} onClose={jest.fn()} />);
    expect(screen.queryByTestId('client-config-modal')).not.toBeInTheDocument();
  });

  it('renders when isOpen is true', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    expect(screen.getByTestId('client-config-modal')).toBeInTheDocument();
  });

  it('renders the modal backdrop', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    expect(screen.getByTestId('client-config-modal-backdrop')).toBeInTheDocument();
  });

  it('renders the modal header with title', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    expect(screen.getByText('Client Configuration')).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const handleClose = jest.fn();
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={handleClose} />);
    
    fireEvent.click(screen.getByTestId('client-config-modal-close'));
    
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('calls onClose when backdrop is clicked', () => {
    const handleClose = jest.fn();
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={handleClose} />);
    
    fireEvent.click(screen.getByTestId('client-config-modal-backdrop'));
    
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('does not call onClose when modal content is clicked', () => {
    const handleClose = jest.fn();
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={handleClose} />);
    
    fireEvent.click(screen.getByTestId('client-config-modal'));
    
    expect(handleClose).not.toHaveBeenCalled();
  });

  it('calls onClose when Escape key is pressed', () => {
    const handleClose = jest.fn();
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={handleClose} />);
    
    fireEvent.keyDown(document, { key: 'Escape' });
    
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('has proper accessibility attributes', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    
    const backdrop = screen.getByTestId('client-config-modal-backdrop');
    expect(backdrop).toHaveAttribute('role', 'dialog');
    expect(backdrop).toHaveAttribute('aria-modal', 'true');
    expect(backdrop).toHaveAttribute('aria-labelledby', 'client-config-modal-title');
  });

  it('renders close button with proper aria-label', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    
    const closeButton = screen.getByTestId('client-config-modal-close');
    expect(closeButton).toHaveAttribute('aria-label', 'Close configuration modal');
  });

  it('renders the ClientConfigView inside the modal', () => {
    renderWithProviders(<ClientConfigModal isOpen={true} onClose={jest.fn()} />);
    expect(screen.getByTestId('client-config-view')).toBeInTheDocument();
  });
});
