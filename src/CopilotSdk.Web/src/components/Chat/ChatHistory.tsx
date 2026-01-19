/**
 * Component for displaying the chat history.
 */
import React, { useRef, useEffect } from 'react';
import {
  SessionEvent,
  UserMessageData,
  AssistantMessageData,
  AssistantReasoningData,
  ToolExecutionStartData,
  ToolExecutionCompleteData,
  SessionErrorData,
} from '../../types';
import { UserMessage } from './UserMessage';
import { AssistantMessage } from './AssistantMessage';
import { ReasoningCollapsible } from './ReasoningCollapsible';
import { ToolExecutionCard } from './ToolExecutionCard';
import { StreamingIndicator } from './StreamingIndicator';
import './ChatHistory.css';

/**
 * Props for the ChatHistory component.
 */
export interface ChatHistoryProps {
  /** List of session events to display. */
  events: SessionEvent[];
  /** Map of message ID to accumulated streaming content. */
  streamingContent?: Map<string, string>;
  /** Map of reasoning ID to accumulated streaming content. */
  streamingReasoning?: Map<string, string>;
  /** Set of message IDs currently being streamed. */
  streamingMessageIds?: Set<string>;
  /** Set of reasoning IDs currently being streamed. */
  streamingReasoningIds?: Set<string>;
  /** Set of tool call IDs currently executing. */
  executingToolIds?: Set<string>;
  /** Whether to auto-scroll to the bottom on new messages. */
  autoScroll?: boolean;
  /** Whether the session is currently processing. */
  isProcessing?: boolean;
}

/**
 * Group events by their parent/related relationships.
 */
interface ProcessedEvent {
  type: 'user' | 'assistant' | 'reasoning' | 'tool' | 'error' | 'system';
  event: SessionEvent;
  toolComplete?: SessionEvent;
}

/**
 * Process events into a display-friendly format.
 */
function processEvents(events: SessionEvent[]): ProcessedEvent[] {
  const processed: ProcessedEvent[] = [];
  const toolCompleteMap = new Map<string, SessionEvent>();

  // First pass: collect tool completions
  for (const event of events) {
    if (event.type === 'tool.execution_complete') {
      const data = event.data as ToolExecutionCompleteData;
      toolCompleteMap.set(data.toolCallId, event);
    }
  }

  // Second pass: process events
  for (const event of events) {
    switch (event.type) {
      case 'user.message':
        processed.push({ type: 'user', event });
        break;

      case 'assistant.message':
        processed.push({ type: 'assistant', event });
        break;

      case 'assistant.reasoning':
        processed.push({ type: 'reasoning', event });
        break;

      case 'tool.execution_start': {
        const data = event.data as ToolExecutionStartData;
        const completeEvent = toolCompleteMap.get(data.toolCallId);
        processed.push({ type: 'tool', event, toolComplete: completeEvent });
        break;
      }

      case 'session.error':
        processed.push({ type: 'error', event });
        break;

      // Skip these event types in the chat display
      case 'assistant.message_delta':
      case 'assistant.reasoning_delta':
      case 'tool.execution_complete':
      case 'assistant.turn_start':
      case 'assistant.turn_end':
      case 'assistant.usage':
      case 'session.start':
      case 'session.idle':
      case 'abort':
        // These are either handled by their parent events or are not visual
        break;

      default:
        // Show unknown events as system messages
        processed.push({ type: 'system', event });
    }
  }

  return processed;
}

/**
 * Render an error event.
 */
function ErrorMessage({ data }: { data: SessionErrorData }) {
  return (
    <div className="error-message-card" data-testid="error-message">
      <span className="error-icon">⚠️</span>
      <div className="error-content">
        <div className="error-type">{data.errorType}</div>
        <div className="error-text">{data.message}</div>
      </div>
    </div>
  );
}

/**
 * Component displaying the chat history with all message types.
 */
export function ChatHistory({
  events,
  streamingContent = new Map(),
  streamingReasoning = new Map(),
  streamingMessageIds = new Set(),
  streamingReasoningIds = new Set(),
  executingToolIds = new Set(),
  autoScroll = true,
  isProcessing = false,
}: ChatHistoryProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom when events change
  useEffect(() => {
    if (autoScroll && bottomRef.current) {
      bottomRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [events, autoScroll, streamingContent.size]);

  const processedEvents = processEvents(events);

  if (processedEvents.length === 0 && !isProcessing) {
    return (
      <div className="chat-history empty" data-testid="chat-history">
        <div className="empty-state">
          <span className="empty-icon">💬</span>
          <p className="empty-text">No messages yet. Start a conversation!</p>
        </div>
      </div>
    );
  }

  return (
    <div className="chat-history" ref={containerRef} data-testid="chat-history">
      <div className="chat-messages">
        {processedEvents.map((processed) => {
          const { event } = processed;

          switch (processed.type) {
            case 'user':
              return (
                <UserMessage
                  key={event.id}
                  data={event.data as UserMessageData}
                  timestamp={event.timestamp}
                />
              );

            case 'assistant': {
              const data = event.data as AssistantMessageData;
              const isStreaming = streamingMessageIds.has(data.messageId);
              const content = streamingContent.get(data.messageId);
              return (
                <AssistantMessage
                  key={event.id}
                  data={data}
                  timestamp={event.timestamp}
                  isStreaming={isStreaming}
                  streamingContent={content}
                />
              );
            }

            case 'reasoning': {
              const data = event.data as AssistantReasoningData;
              const isStreaming = streamingReasoningIds.has(data.reasoningId);
              const content = streamingReasoning.get(data.reasoningId) ?? data.content;
              return (
                <ReasoningCollapsible
                  key={event.id}
                  content={content}
                  reasoningId={data.reasoningId}
                  isStreaming={isStreaming}
                />
              );
            }

            case 'tool': {
              const startData = event.data as ToolExecutionStartData;
              const completeData = processed.toolComplete?.data as ToolExecutionCompleteData | undefined;
              const isExecuting = executingToolIds.has(startData.toolCallId);
              return (
                <ToolExecutionCard
                  key={event.id}
                  startData={startData}
                  completeData={completeData}
                  isExecuting={isExecuting}
                />
              );
            }

            case 'error':
              return (
                <ErrorMessage key={event.id} data={event.data as SessionErrorData} />
              );

            case 'system':
              return (
                <div key={event.id} className="system-message" data-testid="system-message">
                  <span className="system-event-type">{event.type}</span>
                </div>
              );

            default:
              return null;
          }
        })}

        {isProcessing && (
          <div className="processing-indicator">
            <StreamingIndicator isStreaming={true} label="Processing" />
          </div>
        )}
      </div>
      
      <div ref={bottomRef} className="scroll-anchor" />
    </div>
  );
}

export default ChatHistory;
