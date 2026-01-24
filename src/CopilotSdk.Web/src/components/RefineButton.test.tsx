/**
 * Tests for the RefineButton component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { RefineButton } from './RefineButton';

describe('RefineButton', () => {
  const mockOnClick = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the button', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByTestId('refine-button')).toBeInTheDocument();
    });

    it('displays "Refine" text when not refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByText('Refine')).toBeInTheDocument();
    });

    it('displays "Refining..." text when refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      expect(screen.getByText('Refining...')).toBeInTheDocument();
    });

    it('has correct aria-label when not refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByLabelText('Refine prompt')).toBeInTheDocument();
    });

    it('has correct aria-label when refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      expect(screen.getByLabelText('Refining prompt...')).toBeInTheDocument();
    });

    it('sets aria-busy when refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      expect(screen.getByTestId('refine-button')).toHaveAttribute('aria-busy', 'true');
    });

    it('does not set aria-busy when not refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByTestId('refine-button')).toHaveAttribute('aria-busy', 'false');
    });
  });

  describe('iteration count badge', () => {
    it('does not show badge when iterationCount is 0', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} iterationCount={0} />);
      expect(screen.queryByTestId('refine-iteration-badge')).not.toBeInTheDocument();
    });

    it('does not show badge when iterationCount is undefined', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.queryByTestId('refine-iteration-badge')).not.toBeInTheDocument();
    });

    it('shows badge with count when iterationCount > 0', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} iterationCount={3} />);
      const badge = screen.getByTestId('refine-iteration-badge');
      expect(badge).toBeInTheDocument();
      expect(badge).toHaveTextContent('3');
    });

    it('hides badge while refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} iterationCount={3} />);
      expect(screen.queryByTestId('refine-iteration-badge')).not.toBeInTheDocument();
    });

    it('shows correct title for single iteration', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} iterationCount={1} />);
      const badge = screen.getByTestId('refine-iteration-badge');
      expect(badge).toHaveAttribute('title', 'Refined 1 time');
    });

    it('shows correct title for multiple iterations', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} iterationCount={5} />);
      const badge = screen.getByTestId('refine-iteration-badge');
      expect(badge).toHaveAttribute('title', 'Refined 5 times');
    });
  });

  describe('disabled state', () => {
    it('is disabled when disabled prop is true', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} disabled={true} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('is disabled when isRefining is true', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('is disabled when both disabled and isRefining are true', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} disabled={true} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('is not disabled when both disabled and isRefining are false', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} disabled={false} />);
      expect(screen.getByTestId('refine-button')).not.toBeDisabled();
    });
  });

  describe('click handling', () => {
    it('calls onClick when clicked', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      fireEvent.click(screen.getByTestId('refine-button'));
      expect(mockOnClick).toHaveBeenCalledTimes(1);
    });

    it('does not call onClick when disabled', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} disabled={true} />);
      fireEvent.click(screen.getByTestId('refine-button'));
      expect(mockOnClick).not.toHaveBeenCalled();
    });

    it('does not call onClick when refining', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      fireEvent.click(screen.getByTestId('refine-button'));
      expect(mockOnClick).not.toHaveBeenCalled();
    });
  });

  describe('styling', () => {
    it('has refining class when isRefining is true', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={true} />);
      expect(screen.getByTestId('refine-button')).toHaveClass('refining');
    });

    it('does not have refining class when isRefining is false', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByTestId('refine-button')).not.toHaveClass('refining');
    });

    it('applies custom className', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} className="custom-class" />);
      expect(screen.getByTestId('refine-button')).toHaveClass('custom-class');
    });
  });

  describe('tooltip', () => {
    it('has default title', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByTestId('refine-button')).toHaveAttribute(
        'title',
        'Refine prompt using AI to expand and improve the content'
      );
    });

    it('accepts custom title', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} title="Custom tooltip" />);
      expect(screen.getByTestId('refine-button')).toHaveAttribute('title', 'Custom tooltip');
    });
  });

  describe('button type', () => {
    it('has type="button" to prevent form submission', () => {
      render(<RefineButton onClick={mockOnClick} isRefining={false} />);
      expect(screen.getByTestId('refine-button')).toHaveAttribute('type', 'button');
    });
  });
});
