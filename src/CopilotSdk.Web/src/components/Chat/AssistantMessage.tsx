/**
 * Component for displaying an assistant message in the chat.
 */
import React from 'react';
import { AssistantMessageData, ToolRequest } from '../../types';
import { StreamingIndicator } from './StreamingIndicator';
import './AssistantMessage.css';

/**
 * Props for the AssistantMessage component.
 */
export interface AssistantMessageProps {
  /** The assistant message data. */
  data: AssistantMessageData;
  /** When the message was sent. */
  timestamp?: string;
  /** Whether this message is still being streamed. */
  isStreaming?: boolean;
  /** Accumulated streaming content (for delta messages). */
  streamingContent?: string;
}

/**
 * Format a timestamp for display.
 */
function formatTime(timestamp?: string): string {
  if (!timestamp) return '';
  const date = new Date(timestamp);
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/**
 * Simple markdown-like rendering for code blocks.
 */
function renderContent(content: string): React.ReactNode {
  if (!content) return null;

  // Split by code blocks
  const parts = content.split(/(```[\s\S]*?```)/g);
  
  return parts.map((part, index) => {
    if (part.startsWith('```')) {
      // Extract language and code
      const match = part.match(/```(\w+)?\n?([\s\S]*?)```/);
      if (match) {
        const language = match[1] || '';
        const code = match[2] || '';
        return (
          <pre key={index} className="code-block" data-language={language}>
            <code>{code.trim()}</code>
          </pre>
        );
      }
    }
    
    // Handle inline code
    const inlineParts = part.split(/(`[^`]+`)/g);
    return (
      <span key={index}>
        {inlineParts.map((inlinePart, inlineIndex) => {
          if (inlinePart.startsWith('`') && inlinePart.endsWith('`')) {
            return (
              <code key={inlineIndex} className="inline-code">
                {inlinePart.slice(1, -1)}
              </code>
            );
          }
          return inlinePart;
        })}
      </span>
    );
  });
}

/**
 * Render tool request badges.
 */
function ToolRequestBadges({ toolRequests }: { toolRequests: ToolRequest[] }) {
  if (!toolRequests || toolRequests.length === 0) return null;
  
  return (
    <div className="tool-request-badges">
      {toolRequests.map((request) => (
        <span key={request.toolCallId} className="tool-request-badge" title={request.toolCallId}>
          🔧 {request.toolName}
        </span>
      ))}
    </div>
  );
}

/**
 * Component displaying an assistant's message in the chat.
 */
export function AssistantMessage({
  data,
  timestamp,
  isStreaming = false,
  streamingContent,
}: AssistantMessageProps) {
  // Use streaming content if available, otherwise use the complete content
  const content = streamingContent ?? data.content ?? '';
  const toolRequests = data.toolRequests || [];

  return (
    <div className="assistant-message" data-testid="assistant-message" data-message-id={data.messageId}>
      <div className="assistant-message-header">
        <span className="assistant-message-author">Copilot</span>
        {timestamp && <span className="assistant-message-time">{formatTime(timestamp)}</span>}
      </div>
      
      <div className="assistant-message-content">
        {content ? (
          <div className="assistant-message-text">
            {renderContent(content)}
          </div>
        ) : isStreaming ? (
          <StreamingIndicator isStreaming={true} label="Generating response" />
        ) : null}
        
        {isStreaming && content && (
          <span className="streaming-cursor">▊</span>
        )}
      </div>

      <ToolRequestBadges toolRequests={toolRequests} />
    </div>
  );
}

export default AssistantMessage;
