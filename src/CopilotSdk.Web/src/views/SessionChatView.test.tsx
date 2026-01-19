/**
 * Tests for the SessionChatView component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { SessionChatView } from './SessionChatView';
import { SessionInfoResponse, SessionEvent } from '../types';

// Mock the useSession hook
const mockSendMessage = jest.fn();
const mockAbortSession = jest.fn();
const mockRefreshMessages = jest.fn();

jest.mock('../context', () => ({
  useSession: () => ({
    sendMessage: mockSendMessage,
    abortSession: mockAbortSession,
    refreshMessages: mockRefreshMessages,
  }),
}));

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

describe('SessionChatView', () => {
  const mockSession: SessionInfoResponse = {
    sessionId: 'test-session-123',
    model: 'gpt-4o',
    streaming: true,
    createdAt: '2026-01-18T10:00:00Z',
    status: 'Active',
    messageCount: 5,
  };

  const mockEvents: SessionEvent[] = [
    {
      id: 'e1',
      type: 'user.message',
      timestamp: '2026-01-18T10:00:00Z',
      data: { content: 'Hello!' },
    },
    {
      id: 'e2',
      type: 'assistant.message',
      timestamp: '2026-01-18T10:00:01Z',
      data: { messageId: 'msg1', content: 'Hi there! How can I help?' },
    },
  ];

  const defaultProps = {
    session: mockSession,
    events: mockEvents,
    isSending: false,
    error: null,
    onClearError: jest.fn(),
  };

  const renderComponent = (props = {}) => {
    return render(
      <MemoryRouter>
        <SessionChatView {...defaultProps} {...props} />
      </MemoryRouter>
    );
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the session chat view', () => {
      renderComponent();
      expect(screen.getByTestId('session-chat-view')).toBeInTheDocument();
    });

    it('displays session ID', () => {
      renderComponent();
      expect(screen.getByText('test-session-123')).toBeInTheDocument();
    });

    it('displays session status', () => {
      renderComponent();
      expect(screen.getByText('Active')).toBeInTheDocument();
    });

    it('displays model name', () => {
      renderComponent();
      expect(screen.getByText('gpt-4o')).toBeInTheDocument();
    });

    it('displays streaming indicator', () => {
      renderComponent();
      expect(screen.getByText('🔴 Streaming')).toBeInTheDocument();
    });

    it('displays batch indicator when not streaming', () => {
      renderComponent({ session: { ...mockSession, streaming: false } });
      expect(screen.getByText('📨 Batch')).toBeInTheDocument();
    });

    it('displays message count', () => {
      renderComponent();
      expect(screen.getByText('5 messages')).toBeInTheDocument();
    });

    it('renders chat history', () => {
      renderComponent();
      expect(screen.getByTestId('chat-history')).toBeInTheDocument();
    });

    it('renders message input', () => {
      renderComponent();
      expect(screen.getByTestId('message-input')).toBeInTheDocument();
    });
  });

  describe('header actions', () => {
    it('has refresh button', () => {
      renderComponent();
      expect(screen.getByTitle('Refresh messages')).toBeInTheDocument();
    });

    it('calls refreshMessages when refresh button clicked', () => {
      renderComponent();
      fireEvent.click(screen.getByTitle('Refresh messages'));
      expect(mockRefreshMessages).toHaveBeenCalled();
    });

    it('has back button', () => {
      renderComponent();
      expect(screen.getByText('Back to List')).toBeInTheDocument();
    });

    it('navigates to sessions list when back button clicked', () => {
      renderComponent();
      fireEvent.click(screen.getByText('Back to List'));
      expect(mockNavigate).toHaveBeenCalledWith('/sessions');
    });
  });

  describe('error display', () => {
    it('does not show error when there is none', () => {
      renderComponent();
      expect(screen.queryByText('×')).not.toBeInTheDocument();
    });

    it('shows error when provided', () => {
      renderComponent({ error: 'Something went wrong' });
      expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    });

    it('calls onClearError when dismiss button clicked', () => {
      const mockOnClearError = jest.fn();
      renderComponent({ error: 'Error', onClearError: mockOnClearError });
      
      fireEvent.click(screen.getByText('×'));
      
      expect(mockOnClearError).toHaveBeenCalled();
    });
  });

  describe('message sending', () => {
    it('calls sendMessage when message is submitted', async () => {
      mockSendMessage.mockResolvedValue({ accepted: true });
      renderComponent();
      
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test message' } });
      fireEvent.click(screen.getByRole('button', { name: /send/i }));
      
      await waitFor(() => {
        expect(mockSendMessage).toHaveBeenCalledWith({
          prompt: 'Test message',
          mode: 'enqueue',
          attachments: undefined,
        });
      });
    });

    it('disables input when session is in error state', () => {
      renderComponent({ session: { ...mockSession, status: 'Error' } });
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('shows error placeholder when session is in error state', () => {
      renderComponent({ session: { ...mockSession, status: 'Error' } });
      expect(screen.getByPlaceholderText('Session is in error state')).toBeInTheDocument();
    });
  });

  describe('abort functionality', () => {
    it('calls abortSession when abort is triggered', async () => {
      mockAbortSession.mockResolvedValue(undefined);
      renderComponent({ isSending: true });
      
      fireEvent.click(screen.getByRole('button', { name: /abort/i }));
      
      await waitFor(() => {
        expect(mockAbortSession).toHaveBeenCalled();
      });
    });
  });

  describe('session status styling', () => {
    it('applies active status class', () => {
      renderComponent({ session: { ...mockSession, status: 'Active' } });
      expect(screen.getByText('Active')).toHaveClass('status-active');
    });

    it('applies idle status class', () => {
      renderComponent({ session: { ...mockSession, status: 'Idle' } });
      expect(screen.getByText('Idle')).toHaveClass('status-idle');
    });

    it('applies error status class', () => {
      renderComponent({ session: { ...mockSession, status: 'Error' } });
      expect(screen.getByText('Error')).toHaveClass('status-error');
    });
  });

  describe('messages display', () => {
    it('displays user messages', () => {
      renderComponent();
      expect(screen.getByText('Hello!')).toBeInTheDocument();
    });

    it('displays assistant messages', () => {
      renderComponent();
      expect(screen.getByText('Hi there! How can I help?')).toBeInTheDocument();
    });

    it('shows empty state when no events', () => {
      renderComponent({ events: [] });
      expect(screen.getByText('No messages yet. Start a conversation!')).toBeInTheDocument();
    });
  });
});
