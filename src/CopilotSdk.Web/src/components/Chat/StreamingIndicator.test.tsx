/**
 * Tests for the StreamingIndicator component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { StreamingIndicator } from './StreamingIndicator';

describe('StreamingIndicator', () => {
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
});
