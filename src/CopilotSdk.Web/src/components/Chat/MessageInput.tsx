/**
 * Component for message input with send/abort functionality.
 */
import React, { useState, useRef, useCallback, KeyboardEvent } from 'react';
import { MessageAttachment } from '../../types';
import { AttachmentsPanel } from './AttachmentsPanel';
import './MessageInput.css';

/**
 * Props for the MessageInput component.
 */
export interface MessageInputProps {
  /** Callback when a message is submitted. */
  onSend: (prompt: string, mode: 'enqueue' | 'immediate', attachments: MessageAttachment[]) => void;
  /** Callback when abort is requested. */
  onAbort?: () => void;
  /** Whether a message is currently being processed. */
  isProcessing?: boolean;
  /** Whether the input is disabled. */
  disabled?: boolean;
  /** Placeholder text for the input. */
  placeholder?: string;
  /** Whether to allow attachments. */
  allowAttachments?: boolean;
  /** Maximum number of attachments. */
  maxAttachments?: number;
}

/**
 * Convert File to MessageAttachment.
 */
async function fileToAttachment(file: File): Promise<MessageAttachment> {
  return {
    type: 'file',
    displayName: file.name,
    mimeType: file.type || 'application/octet-stream',
    // Note: In a real implementation, you'd read and base64 encode the file content
    // or upload it to a server and get a reference
    path: file.name,
  };
}

/**
 * Message input component with mode selector and attachments.
 */
export function MessageInput({
  onSend,
  onAbort,
  isProcessing = false,
  disabled = false,
  placeholder = 'Type your message...',
  allowAttachments = true,
  maxAttachments = 10,
}: MessageInputProps) {
  const [message, setMessage] = useState('');
  const [mode, setMode] = useState<'enqueue' | 'immediate'>('enqueue');
  const [attachments, setAttachments] = useState<MessageAttachment[]>([]);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-resize textarea
  const adjustTextareaHeight = useCallback(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      textarea.style.height = 'auto';
      const maxHeight = 200;
      textarea.style.height = `${Math.min(textarea.scrollHeight, maxHeight)}px`;
    }
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMessage(e.target.value);
    adjustTextareaHeight();
  };

  const handleSend = useCallback(() => {
    const trimmedMessage = message.trim();
    if (!trimmedMessage || isProcessing || disabled) return;

    onSend(trimmedMessage, mode, attachments);
    setMessage('');
    setAttachments([]);
    
    // Reset textarea height
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  }, [message, mode, attachments, isProcessing, disabled, onSend]);

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // Submit on Enter (without Shift)
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleAddAttachments = useCallback(async (files: FileList) => {
    const newAttachments: MessageAttachment[] = [];
    for (let i = 0; i < files.length; i++) {
      if (attachments.length + newAttachments.length >= maxAttachments) break;
      const attachment = await fileToAttachment(files[i]);
      newAttachments.push(attachment);
    }
    setAttachments([...attachments, ...newAttachments]);
  }, [attachments, maxAttachments]);

  const handleRemoveAttachment = useCallback((index: number) => {
    setAttachments(attachments.filter((_, i) => i !== index));
  }, [attachments]);

  const canSend = message.trim().length > 0 && !isProcessing && !disabled;

  return (
    <div className="message-input-container" data-testid="message-input">
      {/* Attachments panel */}
      {allowAttachments && (attachments.length > 0 || !isProcessing) && (
        <AttachmentsPanel
          attachments={attachments}
          onAdd={handleAddAttachments}
          onRemove={handleRemoveAttachment}
          readOnly={isProcessing}
          maxAttachments={maxAttachments}
        />
      )}

      {/* Input area */}
      <div className={`message-input-area ${attachments.length > 0 ? 'has-attachments' : ''}`}>
        <textarea
          ref={textareaRef}
          className="message-textarea"
          value={message}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled || isProcessing}
          rows={1}
          aria-label="Message input"
        />

        <div className="message-input-actions">
          {/* Mode selector */}
          <div className="mode-selector">
            <button
              type="button"
              className={`mode-btn ${mode === 'enqueue' ? 'active' : ''}`}
              onClick={() => setMode('enqueue')}
              disabled={isProcessing}
              title="Enqueue mode: Message waits in queue"
            >
              Queue
            </button>
            <button
              type="button"
              className={`mode-btn ${mode === 'immediate' ? 'active' : ''}`}
              onClick={() => setMode('immediate')}
              disabled={isProcessing}
              title="Immediate mode: Message processed immediately"
            >
              Now
            </button>
          </div>

          {/* Send/Abort buttons */}
          <div className="action-buttons">
            {isProcessing && onAbort ? (
              <button
                type="button"
                className="abort-btn"
                onClick={onAbort}
                aria-label="Abort"
                title="Abort current operation"
              >
                <span className="btn-icon">⬜</span>
                <span className="btn-label">Stop</span>
              </button>
            ) : (
              <button
                type="button"
                className="send-btn"
                onClick={handleSend}
                disabled={!canSend}
                aria-label="Send message"
                title={canSend ? 'Send message (Enter)' : 'Type a message to send'}
              >
                <span className="btn-icon">➤</span>
                <span className="btn-label">Send</span>
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default MessageInput;
