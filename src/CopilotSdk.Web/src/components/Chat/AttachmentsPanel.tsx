/**
 * Panel for displaying and managing message attachments.
 */
import React from 'react';
import { MessageAttachment } from '../../types';
import './AttachmentsPanel.css';

/**
 * Props for the AttachmentsPanel component.
 */
export interface AttachmentsPanelProps {
  /** List of attachments. */
  attachments: MessageAttachment[];
  /** Callback when an attachment is removed. */
  onRemove?: (index: number) => void;
  /** Callback when files are added. */
  onAdd?: (files: FileList) => void;
  /** Whether the panel is in read-only mode. */
  readOnly?: boolean;
  /** Maximum number of attachments allowed. */
  maxAttachments?: number;
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
    case 'code':
      return '💻';
    default:
      return '📎';
  }
}

/**
 * Format file size for display.
 */
function formatFileSize(bytes?: number): string {
  if (!bytes) return '';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Single attachment item component.
 */
function AttachmentItem({
  attachment,
  index,
  onRemove,
  readOnly,
}: {
  attachment: MessageAttachment;
  index: number;
  onRemove?: (index: number) => void;
  readOnly?: boolean;
}) {
  const icon = getAttachmentIcon(attachment.type);
  const displayName = attachment.displayName || attachment.path || attachment.uri || 'Attachment';
  const hasLineRange = attachment.startLine !== undefined && attachment.endLine !== undefined;

  return (
    <div className="attachment-item" title={attachment.path || attachment.uri}>
      <span className="attachment-item-icon">{icon}</span>
      <div className="attachment-item-info">
        <span className="attachment-item-name">{displayName}</span>
        <span className="attachment-item-meta">
          {attachment.language && <span className="attachment-language">{attachment.language}</span>}
          {hasLineRange && (
            <span className="attachment-lines">
              Lines {attachment.startLine}-{attachment.endLine}
            </span>
          )}
          {attachment.mimeType && <span className="attachment-mime">{attachment.mimeType}</span>}
        </span>
      </div>
      {!readOnly && onRemove && (
        <button
          type="button"
          className="attachment-remove-btn"
          onClick={() => onRemove(index)}
          title="Remove attachment"
          aria-label={`Remove ${displayName}`}
        >
          ×
        </button>
      )}
    </div>
  );
}

/**
 * Panel for displaying and managing attachments.
 */
export function AttachmentsPanel({
  attachments,
  onRemove,
  onAdd,
  readOnly = false,
  maxAttachments = 10,
}: AttachmentsPanelProps) {
  const fileInputRef = React.useRef<HTMLInputElement>(null);

  const handleAddClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      onAdd?.(e.target.files);
      // Reset input so the same file can be selected again
      e.target.value = '';
    }
  };

  const canAdd = !readOnly && attachments.length < maxAttachments;

  if (attachments.length === 0 && readOnly) {
    return null;
  }

  return (
    <div className="attachments-panel" data-testid="attachments-panel">
      {attachments.length > 0 && (
        <div className="attachments-list">
          {attachments.map((attachment, index) => (
            <AttachmentItem
              key={index}
              attachment={attachment}
              index={index}
              onRemove={onRemove}
              readOnly={readOnly}
            />
          ))}
        </div>
      )}

      {canAdd && (
        <div className="attachments-actions">
          <button
            type="button"
            className="add-attachment-btn"
            onClick={handleAddClick}
            title="Add attachment"
          >
            <span className="add-icon">+</span>
            <span className="add-label">Add File</span>
          </button>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            className="file-input-hidden"
            onChange={handleFileChange}
            accept="*/*"
          />
          <span className="attachment-hint">
            {attachments.length}/{maxAttachments} attachments
          </span>
        </div>
      )}
    </div>
  );
}

export default AttachmentsPanel;
