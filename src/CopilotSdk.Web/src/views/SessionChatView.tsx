/**
 * Session Chat View - Main chat interface for a Copilot session.
 */
import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSession } from '../context';
import {
  SessionInfoResponse,
  SessionEvent,
  MessageAttachment,
  StreamingDelta,
  AssistantMessageData,
  AssistantMessageDeltaData,
  AssistantReasoningData,
  AssistantReasoningDeltaData,
  ToolExecutionStartData,
  ToolExecutionCompleteData,
} from '../types';
import { ChatHistory, MessageInput, EventLogPanel } from '../components';
import './SessionChatView.css';

/**
 * Props for the SessionChatView component.
 */
export interface SessionChatViewProps {
  /** The active session info. */
  session: SessionInfoResponse;
  /** Session events. */
  events: SessionEvent[];
  /** Whether a message is being sent. */
  isSending: boolean;
  /** Error message if any. */
  error: string | null;
  /** Clear error. */
  onClearError: () => void;
}

/**
 * Custom hook for managing streaming content accumulation.
 */
function useStreamingAccumulator(events: SessionEvent[]) {
  // Track accumulated streaming content for messages
  const [streamingContent, setStreamingContent] = useState<Map<string, string>>(new Map());
  // Track accumulated streaming content for reasoning
  const [streamingReasoning, setStreamingReasoning] = useState<Map<string, string>>(new Map());
  // Track which messages are currently streaming
  const [streamingMessageIds, setStreamingMessageIds] = useState<Set<string>>(new Set());
  // Track which reasoning blocks are currently streaming
  const [streamingReasoningIds, setStreamingReasoningIds] = useState<Set<string>>(new Set());
  // Track which tools are currently executing
  const [executingToolIds, setExecutingToolIds] = useState<Set<string>>(new Set());

  // Process events to extract streaming state
  useEffect(() => {
    const newStreamingContent = new Map<string, string>();
    const newStreamingReasoning = new Map<string, string>();
    const activeMessageIds = new Set<string>();
    const activeReasoningIds = new Set<string>();
    const activeToolIds = new Set<string>();
    const completedToolIds = new Set<string>();
    const completedMessageIds = new Set<string>();

    // Process events in order to build streaming state
    for (const event of events) {
      switch (event.type) {
        case 'assistant.message': {
          const data = event.data as AssistantMessageData;
          completedMessageIds.add(data.messageId);
          // Full message received - no longer streaming
          newStreamingContent.delete(data.messageId);
          break;
        }

        case 'assistant.message_delta': {
          const data = event.data as AssistantMessageDeltaData;
          if (!completedMessageIds.has(data.messageId)) {
            activeMessageIds.add(data.messageId);
            const current = newStreamingContent.get(data.messageId) || '';
            newStreamingContent.set(data.messageId, current + data.deltaContent);
          }
          break;
        }

        case 'assistant.reasoning': {
          const data = event.data as AssistantReasoningData;
          // Full reasoning received - no longer streaming
          newStreamingReasoning.delete(data.reasoningId);
          break;
        }

        case 'assistant.reasoning_delta': {
          const data = event.data as AssistantReasoningDeltaData;
          activeReasoningIds.add(data.reasoningId);
          const current = newStreamingReasoning.get(data.reasoningId) || '';
          newStreamingReasoning.set(data.reasoningId, current + data.deltaContent);
          break;
        }

        case 'tool.execution_start': {
          const data = event.data as ToolExecutionStartData;
          if (!completedToolIds.has(data.toolCallId)) {
            activeToolIds.add(data.toolCallId);
          }
          break;
        }

        case 'tool.execution_complete': {
          const data = event.data as ToolExecutionCompleteData;
          completedToolIds.add(data.toolCallId);
          activeToolIds.delete(data.toolCallId);
          break;
        }

        case 'assistant.turn_end':
          // Turn ended - clear active streaming
          activeMessageIds.clear();
          activeReasoningIds.clear();
          break;
      }
    }

    // Only update state if there are actual changes
    setStreamingContent(newStreamingContent);
    setStreamingReasoning(newStreamingReasoning);
    setStreamingMessageIds(activeMessageIds);
    setStreamingReasoningIds(activeReasoningIds);
    setExecutingToolIds(activeToolIds);
  }, [events]);

  return {
    streamingContent,
    streamingReasoning,
    streamingMessageIds,
    streamingReasoningIds,
    executingToolIds,
  };
}

/**
 * Session chat view component.
 */
export function SessionChatView({
  session,
  events,
  isSending,
  error,
  onClearError,
}: SessionChatViewProps) {
  const navigate = useNavigate();
  const { sendMessage, abortSession, refreshMessages } = useSession();

  // Event log panel state
  const [showEventLog, setShowEventLog] = useState(false);
  const [eventLogCollapsed, setEventLogCollapsed] = useState(false);

  // Get streaming state
  const {
    streamingContent,
    streamingReasoning,
    streamingMessageIds,
    streamingReasoningIds,
    executingToolIds,
  } = useStreamingAccumulator(events);

  // Determine if currently processing
  const isProcessing = useMemo(() => {
    return isSending || streamingMessageIds.size > 0 || streamingReasoningIds.size > 0 || executingToolIds.size > 0;
  }, [isSending, streamingMessageIds.size, streamingReasoningIds.size, executingToolIds.size]);

  // Handle sending a message
  const handleSend = useCallback(
    async (prompt: string, mode: 'enqueue' | 'immediate', attachments: MessageAttachment[]) => {
      try {
        await sendMessage({
          prompt,
          mode,
          attachments: attachments.length > 0 ? attachments : undefined,
        });
      } catch (err) {
        // Error is handled by context
        console.error('Failed to send message:', err);
      }
    },
    [sendMessage]
  );

  // Handle abort
  const handleAbort = useCallback(async () => {
    try {
      await abortSession();
    } catch (err) {
      console.error('Failed to abort:', err);
    }
  }, [abortSession]);

  // Format session status
  const getStatusClass = (status: string): string => {
    switch (status.toLowerCase()) {
      case 'active':
        return 'status-active';
      case 'idle':
        return 'status-idle';
      case 'error':
        return 'status-error';
      default:
        return '';
    }
  };

  return (
    <div className="session-chat-view" data-testid="session-chat-view">
      {/* Session header */}
      <header className="session-chat-header">
        <div className="session-header-info">
          <h2 className="session-title">{session.sessionId}</h2>
          <div className="session-meta">
            <span className={`session-status-badge ${getStatusClass(session.status)}`}>
              {session.status}
            </span>
            <span className="session-model">{session.model}</span>
            <span className="session-streaming">
              {session.streaming ? '🔴 Streaming' : '📨 Batch'}
            </span>
            <span className="session-messages">{session.messageCount} messages</span>
          </div>
        </div>
        <div className="session-header-actions">
          <button
            type="button"
            className={`btn btn-icon ${showEventLog ? 'active' : ''}`}
            onClick={() => setShowEventLog(!showEventLog)}
            title={showEventLog ? 'Hide event log' : 'Show event log'}
            aria-pressed={showEventLog}
          >
            📋
          </button>
          <button
            type="button"
            className="btn btn-icon"
            onClick={() => refreshMessages()}
            title="Refresh messages"
            disabled={isProcessing}
          >
            🔄
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/sessions')}
          >
            Back to List
          </button>
        </div>
      </header>

      {/* Error display */}
      {error && (
        <div className="session-chat-error">
          <span className="error-text">{error}</span>
          <button type="button" className="error-dismiss" onClick={onClearError}>
            ×
          </button>
        </div>
      )}

      {/* Main content area with chat and optional event log */}
      <div className={`session-chat-content ${showEventLog ? 'with-event-log' : ''}`}>
        {/* Chat area */}
        <div className="session-chat-main">
          {/* Chat history */}
          <ChatHistory
            events={events}
            streamingContent={streamingContent}
            streamingReasoning={streamingReasoning}
            streamingMessageIds={streamingMessageIds}
            streamingReasoningIds={streamingReasoningIds}
            executingToolIds={executingToolIds}
            autoScroll={true}
            isProcessing={isProcessing}
          />

          {/* Message input */}
          <MessageInput
            onSend={handleSend}
            onAbort={handleAbort}
            isProcessing={isProcessing}
            disabled={session.status === 'Error'}
            placeholder={
              session.status === 'Error'
                ? 'Session is in error state'
                : 'Type your message... (Enter to send, Shift+Enter for new line)'
            }
            allowAttachments={true}
          />
        </div>

        {/* Event log panel */}
        {showEventLog && (
          <div className="session-event-log-container">
            <EventLogPanel
              events={events}
              collapsed={eventLogCollapsed}
              onToggleCollapse={() => setEventLogCollapsed(!eventLogCollapsed)}
              maxHeight={500}
            />
          </div>
        )}
      </div>
    </div>
  );
}

export default SessionChatView;
