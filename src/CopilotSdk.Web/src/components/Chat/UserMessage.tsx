/**
 * Component for displaying a user message in the chat.
 */
import React from 'react';
import { UserMessageData, MessageAttachment } from '../../types';
import './UserMessage.css';

/**
 * Props for the UserMessage component.
 */
export interface UserMessageProps {
  /** The user message data. */
  data: UserMessageData;
  /** When the message was sent. */
  timestamp?: string;
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
 * Get icon for attachment type.
 */
function getAttachmentIcon(type: string): string {
  switch (type.toLowerCase()) {
    case 'file':
      return '📄';
    case 'uri':
      return '🔗';
    case 'image':
      return '🖼️';
    default:
      return '📎';
  }
}

/**
 * Render attachment item.
 */
function AttachmentItem({ attachment }: { attachment: MessageAttachment }) {
  const icon = getAttachmentIcon(attachment.type);
  const displayName = attachment.displayName || attachment.path || attachment.uri || 'Attachment';
  
  return (
    <div className="user-attachment" title={attachment.path || attachment.uri}>
      <span className="attachment-icon">{icon}</span>
      <span className="attachment-name">{displayName}</span>
      {attachment.startLine && attachment.endLine && (
        <span className="attachment-lines">
          L{attachment.startLine}-{attachment.endLine}
        </span>
      )}
    </div>
  );
}

/**
 * Component displaying a user's message in the chat.
 */
export function UserMessage({ data, timestamp }: UserMessageProps) {
  const content = data.content || '';
  const attachments = data.attachments || [];

  return (
    <div className="user-message" data-testid="user-message">
      <div className="user-message-header">
        <span className="user-message-author">You</span>
        {timestamp && <span className="user-message-time">{formatTime(timestamp)}</span>}
      </div>
      
      <div className="user-message-content">
        {content}
      </div>

      {attachments.length > 0 && (
        <div className="user-message-attachments">
          {attachments.map((attachment, index) => (
            <AttachmentItem key={index} attachment={attachment} />
          ))}
        </div>
      )}
    </div>
  );
}

export default UserMessage;
