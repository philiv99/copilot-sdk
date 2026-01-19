/**
 * Tests for the AssistantMessage component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { AssistantMessage } from './AssistantMessage';
import { AssistantMessageData } from '../../types';

describe('AssistantMessage', () => {
  const mockData: AssistantMessageData = {
    messageId: 'msg-1',
    content: 'Hello! How can I help you today?',
  };

  describe('rendering', () => {
    it('renders the assistant message', () => {
      render(<AssistantMessage data={mockData} />);
      expect(screen.getByTestId('assistant-message')).toBeInTheDocument();
    });

    it('displays "Copilot" as the author', () => {
      render(<AssistantMessage data={mockData} />);
      expect(screen.getByText('Copilot')).toBeInTheDocument();
    });

    it('displays the message content', () => {
      render(<AssistantMessage data={mockData} />);
      expect(screen.getByText('Hello! How can I help you today?')).toBeInTheDocument();
    });

    it('sets data-message-id attribute', () => {
      render(<AssistantMessage data={mockData} />);
      expect(screen.getByTestId('assistant-message')).toHaveAttribute('data-message-id', 'msg-1');
    });

    it('displays timestamp when provided', () => {
      const timestamp = '2026-01-18T14:30:00Z';
      render(<AssistantMessage data={mockData} timestamp={timestamp} />);
      const header = screen.getByText('Copilot').parentElement;
      expect(header).toBeInTheDocument();
    });
  });

  describe('streaming content', () => {
    it('uses streaming content when provided', () => {
      render(
        <AssistantMessage
          data={mockData}
          isStreaming={true}
          streamingContent="Streaming..."
        />
      );
      expect(screen.getByText('Streaming...')).toBeInTheDocument();
    });

    it('shows streaming indicator when streaming with no content', () => {
      const emptyData: AssistantMessageData = { messageId: 'msg-2', content: '' };
      render(<AssistantMessage data={emptyData} isStreaming={true} />);
      expect(screen.getByTestId('streaming-indicator')).toBeInTheDocument();
    });

    it('shows cursor when streaming with content', () => {
      render(
        <AssistantMessage
          data={mockData}
          isStreaming={true}
          streamingContent="Partial response..."
        />
      );
      expect(screen.getByText('▊')).toBeInTheDocument();
    });

    it('does not show cursor when not streaming', () => {
      render(<AssistantMessage data={mockData} isStreaming={false} />);
      expect(screen.queryByText('▊')).not.toBeInTheDocument();
    });
  });

  describe('code rendering', () => {
    it('renders inline code', () => {
      const codeData: AssistantMessageData = {
        messageId: 'msg-3',
        content: 'Use the `console.log()` function.',
      };
      render(<AssistantMessage data={codeData} />);
      expect(screen.getByText('console.log()')).toBeInTheDocument();
    });

    it('renders code blocks', () => {
      const codeData: AssistantMessageData = {
        messageId: 'msg-4',
        content: 'Here is some code:\n```typescript\nconst x = 1;\n```',
      };
      render(<AssistantMessage data={codeData} />);
      expect(screen.getByText('const x = 1;')).toBeInTheDocument();
    });
  });

  describe('tool requests', () => {
    it('does not show badges when no tool requests', () => {
      render(<AssistantMessage data={mockData} />);
      expect(screen.queryByText('🔧')).not.toBeInTheDocument();
    });

    it('shows tool request badges', () => {
      const dataWithTools: AssistantMessageData = {
        messageId: 'msg-5',
        content: 'Let me search for that.',
        toolRequests: [
          { toolCallId: 'tc-1', toolName: 'search_files' },
        ],
      };
      render(<AssistantMessage data={dataWithTools} />);
      expect(screen.getByText('🔧 search_files')).toBeInTheDocument();
    });

    it('shows multiple tool request badges', () => {
      const dataWithTools: AssistantMessageData = {
        messageId: 'msg-6',
        content: 'Running tools...',
        toolRequests: [
          { toolCallId: 'tc-1', toolName: 'read_file' },
          { toolCallId: 'tc-2', toolName: 'write_file' },
        ],
      };
      render(<AssistantMessage data={dataWithTools} />);
      expect(screen.getByText('🔧 read_file')).toBeInTheDocument();
      expect(screen.getByText('🔧 write_file')).toBeInTheDocument();
    });
  });
});
