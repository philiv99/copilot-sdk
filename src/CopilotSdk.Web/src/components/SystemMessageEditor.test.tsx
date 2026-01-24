/**
 * Tests for the SystemMessageEditor component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { SystemMessageEditor } from './SystemMessageEditor';
import { SystemMessageConfig } from '../types';
import * as copilotApi from '../api/copilotApi';

// Mock the API module
jest.mock('../api/copilotApi');
const mockedApi = copilotApi as jest.Mocked<typeof copilotApi>;

describe('SystemMessageEditor', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    // Default mock implementation for refineSystemMessage
    mockedApi.refineSystemMessage.mockResolvedValue({
      refinedContent: 'Refined content',
      originalContent: 'Original content',
      iterationCount: 1,
      success: true,
    });
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

  describe('refine button', () => {
    const enabledConfig: SystemMessageConfig = { mode: 'Append', content: 'Test content to refine' };

    it('renders refine button when system message is enabled', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('refine-button')).toBeInTheDocument();
    });

    it('does not render refine button when system message is disabled', () => {
      render(<SystemMessageEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.queryByTestId('refine-button')).not.toBeInTheDocument();
    });

    it('disables refine button when content is empty', () => {
      const emptyConfig: SystemMessageConfig = { mode: 'Append', content: '' };
      render(<SystemMessageEditor value={emptyConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('disables refine button when content is whitespace only', () => {
      const whitespaceConfig: SystemMessageConfig = { mode: 'Append', content: '   ' };
      render(<SystemMessageEditor value={whitespaceConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('enables refine button when content has text', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('refine-button')).not.toBeDisabled();
    });

    it('disables refine button when editor is disabled', () => {
      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('refine-button')).toBeDisabled();
    });

    it('calls API and updates content on successful refinement', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: 'Improved and refined content',
        originalContent: 'Test content to refine',
        iterationCount: 1,
        success: true,
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(mockedApi.refineSystemMessage).toHaveBeenCalledWith({
          content: 'Test content to refine',
          context: undefined,
          refinementFocus: undefined,
        });
      });

      await waitFor(() => {
        expect(mockOnChange).toHaveBeenCalledWith({
          ...enabledConfig,
          content: 'Improved and refined content',
        });
      });
    });

    it('shows loading state while refining', async () => {
      let resolvePromise: (value: any) => void;
      mockedApi.refineSystemMessage.mockReturnValue(
        new Promise((resolve) => {
          resolvePromise = resolve;
        })
      );

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByText('Refining...')).toBeInTheDocument();
        expect(screen.getByTestId('refining-overlay')).toBeInTheDocument();
      });

      await waitFor(async () => {
        resolvePromise!({
          refinedContent: 'Refined',
          originalContent: 'Test content to refine',
          iterationCount: 1,
          success: true,
        });
      });
    });

    it('disables textarea while refining', async () => {
      let resolvePromise: (value: any) => void;
      mockedApi.refineSystemMessage.mockReturnValue(
        new Promise((resolve) => {
          resolvePromise = resolve;
        })
      );

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      const textarea = screen.getByRole('textbox');
      expect(textarea).not.toBeDisabled();
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(textarea).toBeDisabled();
      });

      await waitFor(async () => {
        resolvePromise!({
          refinedContent: 'Refined',
          originalContent: 'Test content to refine',
          iterationCount: 1,
          success: true,
        });
      });
    });

    it('shows error message on failed refinement', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Test content to refine',
        iterationCount: 0,
        success: false,
        errorMessage: 'Copilot client is not connected',
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-error')).toBeInTheDocument();
        // User-friendly error message is shown
        expect(screen.getByText('Copilot is not connected. Please start the Copilot client first.')).toBeInTheDocument();
      });
    });

    it('allows dismissing error message', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Test content to refine',
        iterationCount: 0,
        success: false,
        errorMessage: 'Test error',
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-error')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByLabelText('Dismiss error'));

      await waitFor(() => {
        expect(screen.queryByTestId('refine-error')).not.toBeInTheDocument();
      });
    });

    it('clears error when user types', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Test content to refine',
        iterationCount: 0,
        success: false,
        errorMessage: 'Test error',
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-error')).toBeInTheDocument();
      });

      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'New content' } });

      await waitFor(() => {
        expect(screen.queryByTestId('refine-error')).not.toBeInTheDocument();
      });
    });

    it('shows iteration count badge after refinement', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: 'Refined content',
        originalContent: 'Test content to refine',
        iterationCount: 2,
        success: true,
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-iteration-badge')).toBeInTheDocument();
        expect(screen.getByTestId('refine-iteration-badge')).toHaveTextContent('2');
      });
    });

    it('handles API errors gracefully', async () => {
      mockedApi.refineSystemMessage.mockRejectedValue(new Error('Network error'));

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-error')).toBeInTheDocument();
        // User-friendly error message is shown
        expect(screen.getByText('Network error. Please check your connection and try again.')).toBeInTheDocument();
      });
    });

    it('does not call onChange if refinement returns no content', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Test content to refine',
        iterationCount: 0,
        success: false,
        errorMessage: 'Failed',
      });

      render(<SystemMessageEditor value={enabledConfig} onChange={mockOnChange} />);
      
      // Clear initial calls
      mockOnChange.mockClear();
      
      fireEvent.click(screen.getByTestId('refine-button'));
      
      await waitFor(() => {
        expect(screen.getByTestId('refine-error')).toBeInTheDocument();
      });

      // onChange should not have been called for content update
      expect(mockOnChange).not.toHaveBeenCalled();
    });
  });
});
