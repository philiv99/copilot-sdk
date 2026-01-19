/**
 * Tests for the MessageInput component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { MessageInput } from './MessageInput';

describe('MessageInput', () => {
  const mockOnSend = jest.fn();
  const mockOnAbort = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the message input container', () => {
      render(<MessageInput onSend={mockOnSend} />);
      expect(screen.getByTestId('message-input')).toBeInTheDocument();
    });

    it('renders textarea with default placeholder', () => {
      render(<MessageInput onSend={mockOnSend} />);
      expect(screen.getByPlaceholderText('Type your message...')).toBeInTheDocument();
    });

    it('renders textarea with custom placeholder', () => {
      render(<MessageInput onSend={mockOnSend} placeholder="Ask anything..." />);
      expect(screen.getByPlaceholderText('Ask anything...')).toBeInTheDocument();
    });

    it('renders mode selector', () => {
      render(<MessageInput onSend={mockOnSend} />);
      expect(screen.getByText('Queue')).toBeInTheDocument();
      expect(screen.getByText('Now')).toBeInTheDocument();
    });

    it('renders send button', () => {
      render(<MessageInput onSend={mockOnSend} />);
      expect(screen.getByRole('button', { name: /send/i })).toBeInTheDocument();
    });

    it('renders attachments panel when allowAttachments is true', () => {
      render(<MessageInput onSend={mockOnSend} allowAttachments={true} />);
      expect(screen.getByTestId('attachments-panel')).toBeInTheDocument();
    });
  });

  describe('text input', () => {
    it('updates value when typing', () => {
      render(<MessageInput onSend={mockOnSend} />);
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      
      fireEvent.change(textarea, { target: { value: 'Hello world' } });
      
      expect(textarea.value).toBe('Hello world');
    });

    it('disables textarea when disabled prop is true', () => {
      render(<MessageInput onSend={mockOnSend} disabled={true} />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('disables textarea when processing', () => {
      render(<MessageInput onSend={mockOnSend} isProcessing={true} />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });
  });

  describe('mode selection', () => {
    it('defaults to enqueue mode', () => {
      render(<MessageInput onSend={mockOnSend} />);
      const queueButton = screen.getByText('Queue');
      expect(queueButton).toHaveClass('active');
    });

    it('can switch to immediate mode', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      fireEvent.click(screen.getByText('Now'));
      
      expect(screen.getByText('Now')).toHaveClass('active');
      expect(screen.getByText('Queue')).not.toHaveClass('active');
    });

    it('disables mode buttons when processing', () => {
      render(<MessageInput onSend={mockOnSend} isProcessing={true} />);
      expect(screen.getByText('Queue')).toBeDisabled();
      expect(screen.getByText('Now')).toBeDisabled();
    });
  });

  describe('send functionality', () => {
    it('calls onSend when send button is clicked', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test message' } });
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      
      expect(mockOnSend).toHaveBeenCalledWith('Test message', 'enqueue', []);
    });

    it('calls onSend with correct mode', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      fireEvent.click(screen.getByText('Now')); // Switch to immediate mode
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test' } });
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      
      expect(mockOnSend).toHaveBeenCalledWith('Test', 'immediate', []);
    });

    it('clears input after sending', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      fireEvent.change(textarea, { target: { value: 'Test message' } });
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      
      expect(textarea.value).toBe('');
    });

    it('does not send when message is empty', () => {
      render(<MessageInput onSend={mockOnSend} />);
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      expect(mockOnSend).not.toHaveBeenCalled();
    });

    it('does not send when message is only whitespace', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: '   ' } });
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      
      expect(mockOnSend).not.toHaveBeenCalled();
    });

    it('disables send button when empty', () => {
      render(<MessageInput onSend={mockOnSend} />);
      expect(screen.getByRole('button', { name: /send/i })).toBeDisabled();
    });

    it('disables send button when processing', () => {
      render(<MessageInput onSend={mockOnSend} onAbort={mockOnAbort} isProcessing={true} />);
      
      // When processing with onAbort, abort button is shown instead of send
      expect(screen.queryByRole('button', { name: /send/i })).not.toBeInTheDocument();
    });
  });

  describe('keyboard shortcuts', () => {
    it('sends message on Enter key', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test' } });
      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });
      
      expect(mockOnSend).toHaveBeenCalled();
    });

    it('does not send on Shift+Enter', () => {
      render(<MessageInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test' } });
      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: true });
      
      expect(mockOnSend).not.toHaveBeenCalled();
    });
  });

  describe('abort functionality', () => {
    it('shows abort button when processing', () => {
      render(<MessageInput onSend={mockOnSend} onAbort={mockOnAbort} isProcessing={true} />);
      expect(screen.getByRole('button', { name: /abort/i })).toBeInTheDocument();
    });

    it('does not show abort button when not processing', () => {
      render(<MessageInput onSend={mockOnSend} onAbort={mockOnAbort} isProcessing={false} />);
      expect(screen.queryByRole('button', { name: /abort/i })).not.toBeInTheDocument();
    });

    it('calls onAbort when abort button is clicked', () => {
      render(<MessageInput onSend={mockOnSend} onAbort={mockOnAbort} isProcessing={true} />);
      fireEvent.click(screen.getByRole('button', { name: /abort/i }));
      expect(mockOnAbort).toHaveBeenCalled();
    });

    it('does not show abort button when onAbort is not provided', () => {
      render(<MessageInput onSend={mockOnSend} isProcessing={true} />);
      expect(screen.queryByRole('button', { name: /abort/i })).not.toBeInTheDocument();
    });
  });
});
