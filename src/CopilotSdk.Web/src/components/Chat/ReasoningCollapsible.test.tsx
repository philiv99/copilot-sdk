/**
 * Tests for the ReasoningCollapsible component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ReasoningCollapsible } from './ReasoningCollapsible';

describe('ReasoningCollapsible', () => {
  const defaultProps = {
    content: 'This is my reasoning process.',
    reasoningId: 'reasoning-1',
  };

  describe('rendering', () => {
    it('renders the reasoning collapsible', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      expect(screen.getByTestId('reasoning-collapsible')).toBeInTheDocument();
    });

    it('does not render when content is empty and not streaming', () => {
      render(<ReasoningCollapsible content="" reasoningId="test" />);
      expect(screen.queryByTestId('reasoning-collapsible')).not.toBeInTheDocument();
    });

    it('renders when streaming even with empty content', () => {
      render(<ReasoningCollapsible content="" reasoningId="test" isStreaming={true} />);
      expect(screen.getByTestId('reasoning-collapsible')).toBeInTheDocument();
    });

    it('renders header with icon', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      expect(screen.getByText('💭')).toBeInTheDocument();
    });

    it('shows "Thinking" title when not streaming', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      expect(screen.getByText('Thinking')).toBeInTheDocument();
    });

    it('shows "Thinking..." title when streaming', () => {
      render(<ReasoningCollapsible {...defaultProps} isStreaming={true} />);
      expect(screen.getByText('Thinking...')).toBeInTheDocument();
    });

    it('sets data-reasoning-id attribute', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      expect(screen.getByTestId('reasoning-collapsible')).toHaveAttribute('data-reasoning-id', 'reasoning-1');
    });
  });

  describe('collapse/expand behavior', () => {
    it('is collapsed by default', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      expect(screen.queryByText(defaultProps.content)).not.toBeInTheDocument();
    });

    it('starts expanded when defaultExpanded is true', () => {
      render(<ReasoningCollapsible {...defaultProps} defaultExpanded={true} />);
      expect(screen.getByText(defaultProps.content)).toBeInTheDocument();
    });

    it('expands when header is clicked', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      
      fireEvent.click(screen.getByRole('button'));
      
      expect(screen.getByText(defaultProps.content)).toBeInTheDocument();
    });

    it('collapses when header is clicked twice', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      
      const header = screen.getByRole('button');
      fireEvent.click(header); // Expand
      fireEvent.click(header); // Collapse
      
      expect(screen.queryByText(defaultProps.content)).not.toBeInTheDocument();
    });

    it('has aria-expanded attribute', () => {
      render(<ReasoningCollapsible {...defaultProps} />);
      const button = screen.getByRole('button');
      
      expect(button).toHaveAttribute('aria-expanded', 'false');
      
      fireEvent.click(button);
      expect(button).toHaveAttribute('aria-expanded', 'true');
    });
  });

  describe('styling', () => {
    it('has expanded class when expanded', () => {
      render(<ReasoningCollapsible {...defaultProps} defaultExpanded={true} />);
      expect(screen.getByTestId('reasoning-collapsible')).toHaveClass('expanded');
    });

    it('has streaming class when streaming', () => {
      render(<ReasoningCollapsible {...defaultProps} isStreaming={true} />);
      expect(screen.getByTestId('reasoning-collapsible')).toHaveClass('streaming');
    });
  });
});
