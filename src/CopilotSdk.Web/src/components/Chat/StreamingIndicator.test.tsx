/**
 * Tests for the StreamingIndicator component.
 */
import React from 'react';
import { render, screen, act } from '@testing-library/react';
import { StreamingIndicator } from './StreamingIndicator';

describe('StreamingIndicator', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('rendering', () => {
    it('renders when isStreaming is true', () => {
      render(<StreamingIndicator isStreaming={true} />);
      expect(screen.getByTestId('streaming-indicator')).toBeInTheDocument();
    });

    it('does not render when isStreaming is false', () => {
      render(<StreamingIndicator isStreaming={false} />);
      expect(screen.queryByTestId('streaming-indicator')).not.toBeInTheDocument();
    });

    it('renders default label "Thinking"', () => {
      render(<StreamingIndicator isStreaming={true} />);
      expect(screen.getByText('Thinking')).toBeInTheDocument();
    });

    it('renders custom label', () => {
      render(<StreamingIndicator isStreaming={true} label="Processing" />);
      expect(screen.getByText('Processing')).toBeInTheDocument();
    });

    it('renders animated dots', () => {
      render(<StreamingIndicator isStreaming={true} />);
      const indicator = screen.getByTestId('streaming-indicator');
      const dots = indicator.querySelectorAll('.dot');
      expect(dots).toHaveLength(3);
    });
  });

  describe('elapsed timer', () => {
    it('shows 0s initially when streaming', () => {
      render(<StreamingIndicator isStreaming={true} />);
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('0s');
    });

    it('increments elapsed time every second', () => {
      render(<StreamingIndicator isStreaming={true} />);

      act(() => { jest.advanceTimersByTime(3000); });
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('3s');

      act(() => { jest.advanceTimersByTime(2000); });
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('5s');
    });

    it('shows minutes and seconds for longer durations', () => {
      render(<StreamingIndicator isStreaming={true} />);

      act(() => { jest.advanceTimersByTime(75000); });
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('1m 15s');
    });

    it('resets elapsed time when streaming stops and restarts', () => {
      const { rerender } = render(<StreamingIndicator isStreaming={true} />);

      act(() => { jest.advanceTimersByTime(5000); });
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('5s');

      rerender(<StreamingIndicator isStreaming={false} />);
      expect(screen.queryByTestId('streaming-elapsed')).not.toBeInTheDocument();

      rerender(<StreamingIndicator isStreaming={true} />);
      expect(screen.getByTestId('streaming-elapsed')).toHaveTextContent('0s');
    });

    it('does not show elapsed timer when not streaming', () => {
      render(<StreamingIndicator isStreaming={false} />);
      expect(screen.queryByTestId('streaming-elapsed')).not.toBeInTheDocument();
    });
  });
});
