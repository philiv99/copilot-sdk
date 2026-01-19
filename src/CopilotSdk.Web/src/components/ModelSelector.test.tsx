/**
 * Tests for the ModelSelector component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ModelSelector, AVAILABLE_MODELS } from './ModelSelector';

describe('ModelSelector', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the model selector', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.getByTestId('model-selector')).toBeInTheDocument();
    });

    it('renders label', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.getByText('Model')).toBeInTheDocument();
    });

    it('renders custom label', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} label="Select Model" />);
      expect(screen.getByText('Select Model')).toBeInTheDocument();
    });

    it('renders all model options', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      const select = screen.getByTestId('model-select');
      
      AVAILABLE_MODELS.forEach((model) => {
        expect(select).toContainHTML(model.label);
      });
    });

    it('renders with correct selected value', () => {
      render(<ModelSelector value="gpt-4o-mini" onChange={mockOnChange} />);
      const select = screen.getByTestId('model-select') as HTMLSelectElement;
      expect(select.value).toBe('gpt-4o-mini');
    });
  });

  describe('description', () => {
    it('does not show description by default', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.queryByTestId('model-description')).not.toBeInTheDocument();
    });

    it('shows description when showDescriptions is true', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} showDescriptions={true} />);
      expect(screen.getByTestId('model-description')).toBeInTheDocument();
    });

    it('shows correct description for selected model', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} showDescriptions={true} />);
      expect(screen.getByText('Most capable model for complex tasks')).toBeInTheDocument();
    });
  });

  describe('interactions', () => {
    it('calls onChange when selection changes', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      
      const select = screen.getByTestId('model-select');
      fireEvent.change(select, { target: { value: 'gpt-4o-mini' } });
      
      expect(mockOnChange).toHaveBeenCalledWith('gpt-4o-mini');
    });
  });

  describe('disabled state', () => {
    it('disables the select when disabled prop is true', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('model-select')).toBeDisabled();
    });
  });

  describe('custom className', () => {
    it('applies custom className', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} className="custom-class" />);
      expect(screen.getByTestId('model-selector')).toHaveClass('custom-class');
    });
  });
});
