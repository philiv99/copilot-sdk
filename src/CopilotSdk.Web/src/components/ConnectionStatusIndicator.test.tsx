/**
 * Tests for the ConnectionStatusIndicator component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { ConnectionStatusIndicator } from './ConnectionStatusIndicator';

describe('ConnectionStatusIndicator', () => {
  describe('rendering', () => {
    it('renders with connected state', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      expect(screen.getByTestId('connection-status-indicator')).toBeInTheDocument();
      expect(screen.getByTestId('status-dot')).toBeInTheDocument();
      expect(screen.getByTestId('status-label')).toHaveTextContent('Connected');
    });

    it('renders with connecting state', () => {
      render(<ConnectionStatusIndicator state="Connecting" />);
      expect(screen.getByTestId('status-label')).toHaveTextContent('Connecting...');
    });

    it('renders with error state', () => {
      render(<ConnectionStatusIndicator state="Error" />);
      expect(screen.getByTestId('status-label')).toHaveTextContent('Error');
    });

    it('renders with disconnected state', () => {
      render(<ConnectionStatusIndicator state="Disconnected" />);
      expect(screen.getByTestId('status-label')).toHaveTextContent('Disconnected');
    });
  });

  describe('state classes', () => {
    it('applies connected class when connected', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('connected');
    });

    it('applies connecting class when connecting', () => {
      render(<ConnectionStatusIndicator state="Connecting" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('connecting');
    });

    it('applies error class when error', () => {
      render(<ConnectionStatusIndicator state="Error" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('error');
    });

    it('applies disconnected class when disconnected', () => {
      render(<ConnectionStatusIndicator state="Disconnected" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('disconnected');
    });
  });

  describe('size variants', () => {
    it('applies small size class', () => {
      render(<ConnectionStatusIndicator state="Connected" size="small" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('small');
    });

    it('applies medium size class by default', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('medium');
    });

    it('applies large size class', () => {
      render(<ConnectionStatusIndicator state="Connected" size="large" />);
      const indicator = screen.getByTestId('connection-status-indicator');
      expect(indicator).toHaveClass('large');
    });
  });

  describe('label visibility', () => {
    it('shows label by default', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      expect(screen.getByTestId('status-label')).toBeInTheDocument();
    });

    it('hides label when showLabel is false', () => {
      render(<ConnectionStatusIndicator state="Connected" showLabel={false} />);
      expect(screen.queryByTestId('status-label')).not.toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('has role status', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      expect(screen.getByRole('status')).toBeInTheDocument();
    });

    it('has aria-label with connection status', () => {
      render(<ConnectionStatusIndicator state="Connected" />);
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        'Connection status: Connected'
      );
    });
  });
});
