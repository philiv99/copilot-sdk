/**
 * Card component for displaying tool execution status and results.
 */
import React, { useState } from 'react';
import { ToolExecutionStartData, ToolExecutionCompleteData } from '../../types';
import './ToolExecutionCard.css';

/**
 * Props for the ToolExecutionCard component.
 */
export interface ToolExecutionCardProps {
  /** Tool execution start data. */
  startData: ToolExecutionStartData;
  /** Tool execution complete data (if completed). */
  completeData?: ToolExecutionCompleteData;
  /** Whether the tool is currently executing. */
  isExecuting?: boolean;
}

/**
 * Format tool arguments for display.
 */
const formatArguments = (args: unknown): string => {
  if (!args) return '';
  try {
    return JSON.stringify(args, null, 2);
  } catch {
    return String(args);
  }
};

/**
 * Format tool result for display.
 */
function formatResult(result: unknown): string {
  if (result === null || result === undefined) return 'null';
  try {
    if (typeof result === 'string') return result;
    return JSON.stringify(result, null, 2);
  } catch {
    return String(result);
  }
}

/**
 * Get icon for tool status.
 */
function getStatusIcon(isExecuting: boolean, hasError: boolean): string {
  if (isExecuting) return '⚙️';
  if (hasError) return '❌';
  return '✅';
}

/**
 * Card showing tool execution status, arguments, and results.
 */
export function ToolExecutionCard({
  startData,
  completeData,
  isExecuting = false,
}: ToolExecutionCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  const hasError = !!completeData?.error;
  const statusIcon = getStatusIcon(isExecuting, hasError);
  const displayName = startData.displayName || startData.toolName;

  return (
    <div
      className={`tool-execution-card ${isExecuting ? 'executing' : ''} ${hasError ? 'error' : ''}`}
      data-testid="tool-execution-card"
      data-tool-call-id={startData.toolCallId}
    >
      <button
        className="tool-header"
        onClick={() => setIsExpanded(!isExpanded)}
        type="button"
        aria-expanded={isExpanded}
      >
        <span className="tool-icon">{statusIcon}</span>
        <span className="tool-name">{displayName}</span>
        {isExecuting && <span className="tool-status">Running...</span>}
        {completeData?.duration && (
          <span className="tool-duration">{completeData.duration}ms</span>
        )}
        <span className={`tool-chevron ${isExpanded ? 'expanded' : ''}`}>▼</span>
      </button>

      {isExpanded && (
        <div className="tool-details">
          {startData.arguments !== undefined && startData.arguments !== null && (
            <div className="tool-section">
              <div className="tool-section-label">Arguments</div>
              <pre className="tool-code">{formatArguments(startData.arguments)}</pre>
            </div>
          )}

          {completeData && (
            <div className="tool-section">
              <div className="tool-section-label">
                {hasError ? 'Error' : 'Result'}
              </div>
              <pre className={`tool-code ${hasError ? 'error' : ''}`}>
                {hasError ? completeData.error : formatResult(completeData.result)}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default ToolExecutionCard;
