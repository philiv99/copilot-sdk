/**
 * Streaming indicator component to show when content is being streamed.
 */
import React from 'react';
import './StreamingIndicator.css';

/**
 * Props for the StreamingIndicator component.
 */
export interface StreamingIndicatorProps {
  /** Whether streaming is active. */
  isStreaming: boolean;
  /** Optional label to display. */
  label?: string;
}

/**
 * Animated indicator showing that content is being streamed.
 */
export function StreamingIndicator({ isStreaming, label = 'Thinking' }: StreamingIndicatorProps) {
  if (!isStreaming) {
    return null;
  }

  return (
    <div className="streaming-indicator" data-testid="streaming-indicator">
      <div className="streaming-dots">
        <span className="dot" />
        <span className="dot" />
        <span className="dot" />
      </div>
      <span className="streaming-label">{label}</span>
    </div>
  );
}

export default StreamingIndicator;
