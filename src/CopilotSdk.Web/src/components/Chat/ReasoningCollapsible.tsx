/**
 * Collapsible component for displaying assistant reasoning.
 */
import React, { useState } from 'react';
import './ReasoningCollapsible.css';

/**
 * Props for the ReasoningCollapsible component.
 */
export interface ReasoningCollapsibleProps {
  /** The reasoning content to display. */
  content: string;
  /** Unique identifier for this reasoning block. */
  reasoningId?: string;
  /** Whether the reasoning is still being streamed. */
  isStreaming?: boolean;
  /** Whether to start expanded. */
  defaultExpanded?: boolean;
}

/**
 * Collapsible panel for displaying assistant reasoning/thinking.
 */
export function ReasoningCollapsible({
  content,
  reasoningId,
  isStreaming = false,
  defaultExpanded = false,
}: ReasoningCollapsibleProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  const toggleExpanded = () => {
    setIsExpanded(!isExpanded);
  };

  // Don't render if there's no content
  if (!content && !isStreaming) {
    return null;
  }

  return (
    <div
      className={`reasoning-collapsible ${isExpanded ? 'expanded' : ''} ${isStreaming ? 'streaming' : ''}`}
      data-testid="reasoning-collapsible"
      data-reasoning-id={reasoningId}
    >
      <button
        className="reasoning-header"
        onClick={toggleExpanded}
        type="button"
        aria-expanded={isExpanded}
      >
        <span className="reasoning-icon">💭</span>
        <span className="reasoning-title">
          {isStreaming ? 'Thinking...' : 'Thinking'}
        </span>
        <span className={`reasoning-chevron ${isExpanded ? 'expanded' : ''}`}>
          ▼
        </span>
      </button>
      
      {isExpanded && (
        <div className="reasoning-content">
          <pre className="reasoning-text">{content || '...'}</pre>
        </div>
      )}
    </div>
  );
}

export default ReasoningCollapsible;
