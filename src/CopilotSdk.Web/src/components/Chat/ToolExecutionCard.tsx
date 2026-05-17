/**
 * Card component for displaying tool execution status and results.
 */
import React, { useEffect, useState } from 'react';
import { ToolExecutionStartData, ToolExecutionCompleteData } from '../../types';
import './ToolExecutionCard.css';

/**
 * Props for the ToolExecutionCard component.
 */
export interface ToolExecutionCardProps {
  /** Tool execution start data (optional - may not be available for persisted history). */
  startData?: ToolExecutionStartData;
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

const summarizeArguments = (args: unknown): string | null => {
  if (!args || typeof args !== 'object') {
    return null;
  }

  const values = args as Record<string, unknown>;
  const candidates = [
    values.description,
    values.command,
    values.path,
    values.filePath,
    values.cwd,
  ];

  const summary = candidates.find((value) => typeof value === 'string' && value.trim().length > 0);
  if (typeof summary !== 'string') {
    return null;
  }

  return summary.length > 220 ? `${summary.slice(0, 217)}...` : summary;
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

  // Get tool info from startData or completeData (for persisted history)
  const toolCallId = startData?.toolCallId || completeData?.toolCallId || '';
  const toolName = startData?.toolName || completeData?.toolName || 'Unknown Tool';
  const displayName = startData?.displayName || toolName;
  const args = startData?.arguments;
  const argumentSummary = summarizeArguments(args);

  const hasError = !!completeData?.error;
  const statusIcon = getStatusIcon(isExecuting, hasError);

  useEffect(() => {
    if (isExecuting || hasError) {
      setIsExpanded(true);
    }
  }, [isExecuting, hasError]);

  return (
    <div
      className={`tool-execution-card ${isExecuting ? 'executing' : ''} ${hasError ? 'error' : ''}`}
      data-testid="tool-execution-card"
      data-tool-call-id={toolCallId}
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
        {argumentSummary && <span className="tool-status">{argumentSummary}</span>}
        {completeData?.duration && (
          <span className="tool-duration">{completeData.duration}ms</span>
        )}
        <span className={`tool-chevron ${isExpanded ? 'expanded' : ''}`}>▼</span>
      </button>

      {isExpanded && (
        <div className="tool-details">
          {args !== undefined && args !== null && (
            <div className="tool-section">
              <div className="tool-section-label">Arguments</div>
              <pre className="tool-code">{formatArguments(args)}</pre>
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
