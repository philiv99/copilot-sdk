/**
 * Tests for the ChatHistory component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { ChatHistory } from './ChatHistory';
import { SessionEvent } from '../../types';

describe('ChatHistory', () => {
  const createUserMessageEvent = (id: string, content: string): SessionEvent => ({
    id,
    type: 'user.message',
    timestamp: '2026-01-18T10:00:00Z',
    data: { content },
  });

  const createAssistantMessageEvent = (id: string, messageId: string, content: string): SessionEvent => ({
    id,
    type: 'assistant.message',
    timestamp: '2026-01-18T10:01:00Z',
    data: { messageId, content },
  });

  describe('rendering', () => {
    it('renders the chat history container', () => {
      render(<ChatHistory events={[]} />);
      expect(screen.getByTestId('chat-history')).toBeInTheDocument();
    });

    it('shows empty state when no events', () => {
      render(<ChatHistory events={[]} />);
      expect(screen.getByText('No messages yet. Start a conversation!')).toBeInTheDocument();
    });

    it('does not show empty state when processing', () => {
      render(<ChatHistory events={[]} isProcessing={true} />);
      expect(screen.queryByText('No messages yet. Start a conversation!')).not.toBeInTheDocument();
    });
  });

  describe('message rendering', () => {
    it('renders user messages', () => {
      const events = [createUserMessageEvent('e1', 'Hello!')];
      render(<ChatHistory events={events} />);
      expect(screen.getByTestId('user-message')).toBeInTheDocument();
      expect(screen.getByText('Hello!')).toBeInTheDocument();
    });

    it('renders assistant messages', () => {
      const events = [createAssistantMessageEvent('e1', 'msg1', 'Hi there!')];
      render(<ChatHistory events={events} />);
      expect(screen.getByTestId('assistant-message')).toBeInTheDocument();
      expect(screen.getByText('Hi there!')).toBeInTheDocument();
    });

    it('renders multiple messages in order', () => {
      const events = [
        createUserMessageEvent('e1', 'Hello!'),
        createAssistantMessageEvent('e2', 'msg1', 'Hi there!'),
        createUserMessageEvent('e3', 'How are you?'),
      ];
      render(<ChatHistory events={events} />);
      
      const userMessages = screen.getAllByTestId('user-message');
      const assistantMessages = screen.getAllByTestId('assistant-message');
      
      expect(userMessages).toHaveLength(2);
      expect(assistantMessages).toHaveLength(1);
    });
  });

  describe('reasoning events', () => {
    it('renders reasoning collapsibles', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'assistant.reasoning',
          timestamp: '2026-01-18T10:00:00Z',
          data: { reasoningId: 'r1', content: 'Let me think about this...' },
        },
      ];
      render(<ChatHistory events={events} />);
      expect(screen.getByTestId('reasoning-collapsible')).toBeInTheDocument();
    });
  });

  describe('tool execution events', () => {
    it('renders tool execution cards', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'tool.execution_start',
          timestamp: '2026-01-18T10:00:00Z',
          data: { toolCallId: 'tc1', toolName: 'read_file', displayName: 'Read File' },
        },
      ];
      render(<ChatHistory events={events} />);
      expect(screen.getByTestId('tool-execution-card')).toBeInTheDocument();
    });

    it('pairs tool start and complete events', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'tool.execution_start',
          timestamp: '2026-01-18T10:00:00Z',
          data: { toolCallId: 'tc1', toolName: 'read_file', displayName: 'Read File' },
        },
        {
          id: 'e2',
          type: 'tool.execution_complete',
          timestamp: '2026-01-18T10:00:01Z',
          data: { toolCallId: 'tc1', toolName: 'read_file', result: 'file content', duration: 100 },
        },
      ];
      render(<ChatHistory events={events} />);
      
      // Only one card should be rendered (start event)
      const cards = screen.getAllByTestId('tool-execution-card');
      expect(cards).toHaveLength(1);
    });
  });

  describe('error events', () => {
    it('renders error messages', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'session.error',
          timestamp: '2026-01-18T10:00:00Z',
          data: { errorType: 'NetworkError', message: 'Connection lost' },
        },
      ];
      render(<ChatHistory events={events} />);
      expect(screen.getByTestId('error-message')).toBeInTheDocument();
      expect(screen.getByText('Connection lost')).toBeInTheDocument();
    });
  });

  describe('streaming state', () => {
    it('passes streaming content to assistant messages', () => {
      const events = [createAssistantMessageEvent('e1', 'msg1', '')];
      const streamingContent = new Map([['msg1', 'Streaming text...']]);
      const streamingMessageIds = new Set(['msg1']);
      
      render(
        <ChatHistory
          events={events}
          streamingContent={streamingContent}
          streamingMessageIds={streamingMessageIds}
        />
      );
      
      expect(screen.getByText('Streaming text...')).toBeInTheDocument();
    });

    it('marks executing tools', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'tool.execution_start',
          timestamp: '2026-01-18T10:00:00Z',
          data: { toolCallId: 'tc1', toolName: 'test_tool' },
        },
      ];
      const executingToolIds = new Set(['tc1']);
      
      render(<ChatHistory events={events} executingToolIds={executingToolIds} />);
      
      expect(screen.getByTestId('tool-execution-card')).toHaveClass('executing');
    });
  });

  describe('processing indicator', () => {
    it('shows processing indicator when isProcessing is true', () => {
      const events = [createUserMessageEvent('e1', 'Hello!')];
      render(<ChatHistory events={events} isProcessing={true} />);
      expect(screen.getByTestId('streaming-indicator')).toBeInTheDocument();
    });

    it('does not show processing indicator when isProcessing is false', () => {
      const events = [createUserMessageEvent('e1', 'Hello!')];
      render(<ChatHistory events={events} isProcessing={false} />);
      expect(screen.queryByTestId('streaming-indicator')).not.toBeInTheDocument();
    });
  });

  describe('skipped events', () => {
    it('skips delta events (handled by streaming)', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'assistant.message_delta',
          timestamp: '2026-01-18T10:00:00Z',
          data: { messageId: 'msg1', deltaContent: 'partial' },
        },
      ];
      render(<ChatHistory events={events} />);
      
      // Should show empty state since delta events are skipped
      expect(screen.getByText('No messages yet. Start a conversation!')).toBeInTheDocument();
    });

    it('skips turn events', () => {
      const events: SessionEvent[] = [
        {
          id: 'e1',
          type: 'assistant.turn_start',
          timestamp: '2026-01-18T10:00:00Z',
          data: { turnId: 't1' },
        },
        {
          id: 'e2',
          type: 'assistant.turn_end',
          timestamp: '2026-01-18T10:01:00Z',
          data: { turnId: 't1' },
        },
      ];
      render(<ChatHistory events={events} />);
      
      expect(screen.getByText('No messages yet. Start a conversation!')).toBeInTheDocument();
    });
  });
});
