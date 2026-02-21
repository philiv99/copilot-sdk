/**
 * Streaming indicator component to show when content is being streamed.
 */
import React, { useState, useEffect, useRef } from 'react';
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
 * Formats elapsed seconds into a human-readable string (e.g. "5s", "1m 23s").
 */
function formatElapsed(seconds: number): string {
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}m ${secs}s`;
}

/**
 * Animated indicator showing that content is being streamed, with an elapsed time counter.
 */
export function StreamingIndicator({ isStreaming, label = 'Thinking' }: StreamingIndicatorProps) {
  const [elapsed, setElapsed] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (isStreaming) {
      setElapsed(0);
      intervalRef.current = setInterval(() => {
        setElapsed((prev) => prev + 1);
      }, 1000);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      setElapsed(0);
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [isStreaming]);

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
      <span className="streaming-elapsed" data-testid="streaming-elapsed">
        {formatElapsed(elapsed)}
      </span>
    </div>
  );
}

export default StreamingIndicator;
