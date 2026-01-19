/**
 * Tests for the SystemMessageEditor component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { SystemMessageEditor } from './SystemMessageEditor';
import { SystemMessageConfig } from '../types';

describe('SystemMessageEditor', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the editor', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-editor')).toBeInTheDocument();
    });

    it('renders toggle checkbox', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-toggle')).toBeInTheDocument();
    });

    it('shows toggle unchecked when value is undefined', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-toggle')).not.toBeChecked();
    });

    it('shows toggle checked when value is defined', () => {
      const config: SystemMessageConfig = { mode: 'Append', content: 'Test' };
      render(<SystemMessageEditor value={config} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-toggle')).toBeChecked();
    });
  });

  describe('when disabled', () => {
    it('hides content area when toggle is unchecked', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.queryByTestId('system-message-content')).not.toBeInTheDocument();
    });
  });

  describe('when enabled', () => {
    const enabledConfig: SystemMessageConfig = { mode: 'Append', content: 'Test content' };

    it('shows content area', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-content')).toBeInTheDocument();
    });

    it('shows mode options', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByText('Append')).toBeInTheDocument();
      expect(screen.getByText('Replace')).toBeInTheDocument();
    });

    it('shows content textarea', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('system-message-content')).toBeInTheDocument();
    });

    it('shows character count', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByText('12 characters')).toBeInTheDocument();
    });
  });

  describe('toggle interactions', () => {
    it('enables editor when toggle is checked', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('system-message-toggle'));
      
      expect(mockOnChange).toHaveBeenCalledWith({ mode: 'Append', content: '' });
    });

    it('disables editor when toggle is unchecked', () => {
      const config: SystemMessageConfig = { mode: 'Append', content: 'Test' };
      render(<SystemMessageEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('system-message-toggle'));
      
      expect(mockOnChange).toHaveBeenCalledWith(undefined);
    });
  });

  describe('mode selection', () => {
    it('selects Append mode', () => {
      const config: SystemMessageConfig = { mode: 'Replace', content: 'Test' };
      render(<SystemMessageEditor value={config} onChange={mockOnChange} />);
      
      // Use getByRole with radio and value to find the specific radio button
      const appendRadio = screen.getByRole('radio', { name: /append/i });
      fireEvent.click(appendRadio);
      
      expect(mockOnChange).toHaveBeenCalledWith({ mode: 'Append', content: 'Test' });
    });

    it('selects Replace mode', () => {
      const config: SystemMessageConfig = { mode: 'Append', content: 'Test' };
      render(<SystemMessageEditor value={config} onChange={mockOnChange} />);
      
      // Use getByRole with radio and value to find the specific radio button
      const replaceRadio = screen.getByRole('radio', { name: /replace/i });
      fireEvent.click(replaceRadio);
      
      expect(mockOnChange).toHaveBeenCalledWith({ mode: 'Replace', content: 'Test' });
    });
  });

  describe('content editing', () => {
    it('updates content when typed', () => {
      const config: SystemMessageConfig = { mode: 'Append', content: '' };
      render(<SystemMessageEditor value={config} onChange={mockOnChange} />);
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'New content' } });
      
      expect(mockOnChange).toHaveBeenCalledWith({ mode: 'Append', content: 'New content' });
    });
  });

  describe('disabled state', () => {
    it('disables toggle when disabled prop is true', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('system-message-toggle')).toBeDisabled();
    });
  });
});
