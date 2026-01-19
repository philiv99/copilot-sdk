/**
 * TypeScript types for messages and events.
 */

/**
 * Message attachment DTO.
 */
export interface MessageAttachment {
  /** Type of attachment (e.g., "file", "uri"). */
  type: string;
  /** Path to the file (for file attachments). */
  path?: string;
  /** URI reference (for URI attachments). */
  uri?: string;
  /** Display name for the attachment. */
  displayName?: string;
  /** Start line for file excerpts. */
  startLine?: number;
  /** End line for file excerpts. */
  endLine?: number;
  /** MIME type of the attachment. */
  mimeType?: string;
  /** Language identifier for code files. */
  language?: string;
}

/**
 * Request to send a message to a session.
 */
export interface SendMessageRequest {
  /** The message prompt to send. */
  prompt: string;
  /** Message mode: "enqueue" (default) or "immediate". */
  mode?: 'enqueue' | 'immediate';
  /** Optional attachments to include with the message. */
  attachments?: MessageAttachment[];
}

/**
 * Response after sending a message.
 */
export interface SendMessageResponse {
  /** Whether the message was accepted. */
  accepted: boolean;
  /** ID of the message that was sent. */
  messageId?: string;
  /** Error message if not accepted. */
  error?: string;
}

/**
 * Response containing session messages/events.
 */
export interface MessagesResponse {
  /** List of session events. */
  events: SessionEvent[];
  /** Total number of events. */
  totalCount: number;
}

/**
 * Base session event structure.
 */
export interface SessionEvent {
  /** Unique identifier for this event. */
  id: string;
  /** Type of the event. */
  type: SessionEventType;
  /** When this event occurred. */
  timestamp: string;
  /** Parent event ID if this event is related to another event. */
  parentId?: string;
  /** Whether this event is ephemeral (not persisted). */
  ephemeral?: boolean;
  /** Event-specific data payload. */
  data?: SessionEventData;
}

/**
 * All possible event types.
 */
export type SessionEventType =
  | 'user.message'
  | 'assistant.message'
  | 'assistant.message_delta'
  | 'assistant.reasoning'
  | 'assistant.reasoning_delta'
  | 'assistant.turn_start'
  | 'assistant.turn_end'
  | 'assistant.usage'
  | 'tool.execution_start'
  | 'tool.execution_complete'
  | 'session.start'
  | 'session.idle'
  | 'session.error'
  | 'abort';

/**
 * Union type for all event data types.
 */
export type SessionEventData =
  | UserMessageData
  | AssistantMessageData
  | AssistantMessageDeltaData
  | AssistantReasoningData
  | AssistantReasoningDeltaData
  | AssistantTurnStartData
  | AssistantTurnEndData
  | AssistantUsageData
  | ToolExecutionStartData
  | ToolExecutionCompleteData
  | SessionStartData
  | SessionIdleData
  | SessionErrorData
  | AbortData;

// #region User Message Data

/**
 * Data for user.message events.
 */
export interface UserMessageData {
  /** The content of the user's message. */
  content: string;
  /** Transformed content after processing. */
  transformedContent?: string;
  /** Attachments included with the message. */
  attachments?: MessageAttachment[];
  /** Source of the message. */
  source?: string;
}

// #endregion

// #region Assistant Message Data

/**
 * Data for assistant.message events.
 */
export interface AssistantMessageData {
  /** Unique identifier for this message. */
  messageId: string;
  /** The content of the assistant's message. */
  content: string;
  /** Tool requests made by the assistant. */
  toolRequests?: ToolRequest[];
  /** Parent tool call ID if this message is part of a tool execution. */
  parentToolCallId?: string;
}

/**
 * Data for assistant.message_delta events (streaming).
 */
export interface AssistantMessageDeltaData {
  /** Unique identifier for this message. */
  messageId: string;
  /** The delta content being streamed. */
  deltaContent: string;
  /** Total response size in bytes so far. */
  totalResponseSizeBytes?: number;
  /** Parent tool call ID if this message is part of a tool execution. */
  parentToolCallId?: string;
}

/**
 * Represents a tool request from the assistant.
 */
export interface ToolRequest {
  /** Unique ID for this tool call. */
  toolCallId: string;
  /** Name of the tool being requested. */
  toolName: string;
  /** Arguments to pass to the tool. */
  arguments?: unknown;
}

// #endregion

// #region Assistant Reasoning Data

/**
 * Data for assistant.reasoning events.
 */
export interface AssistantReasoningData {
  /** Unique identifier for this reasoning block. */
  reasoningId: string;
  /** The reasoning content. */
  content: string;
}

/**
 * Data for assistant.reasoning_delta events (streaming).
 */
export interface AssistantReasoningDeltaData {
  /** Unique identifier for this reasoning block. */
  reasoningId: string;
  /** The delta content being streamed. */
  deltaContent: string;
}

// #endregion

// #region Tool Execution Data

/**
 * Data for tool.execution_start events.
 */
export interface ToolExecutionStartData {
  /** Unique ID for this tool call. */
  toolCallId: string;
  /** Name of the tool being executed. */
  toolName: string;
  /** Arguments passed to the tool. */
  arguments?: unknown;
  /** Display name of the tool. */
  displayName?: string;
}

/**
 * Data for tool.execution_complete events.
 */
export interface ToolExecutionCompleteData {
  /** Unique ID for this tool call. */
  toolCallId: string;
  /** Name of the tool that was executed. */
  toolName: string;
  /** The result of the tool execution. */
  result?: unknown;
  /** Error message if the tool execution failed. */
  error?: string;
  /** Duration of the tool execution in milliseconds. */
  duration?: number;
}

// #endregion

// #region Session Events Data

/**
 * Data for session.start events.
 */
export interface SessionStartData {
  /** The session ID. */
  sessionId: string;
  /** Session version. */
  version: number;
  /** Producer of the session. */
  producer: string;
  /** Copilot version. */
  copilotVersion: string;
  /** When the session started. */
  startTime: string;
  /** The selected model for the session. */
  selectedModel?: string;
}

/**
 * Data for session.error events.
 */
export interface SessionErrorData {
  /** Type of error. */
  errorType: string;
  /** Error message. */
  message: string;
  /** Stack trace if available. */
  stack?: string;
}

/**
 * Data for session.idle events.
 */
export interface SessionIdleData {
  // Empty - session.idle events have no data payload
}

/**
 * Data for assistant.turn_start events.
 */
export interface AssistantTurnStartData {
  /** Unique identifier for this turn. */
  turnId: string;
}

/**
 * Data for assistant.turn_end events.
 */
export interface AssistantTurnEndData {
  /** Unique identifier for this turn. */
  turnId: string;
}

/**
 * Data for assistant.usage events.
 */
export interface AssistantUsageData {
  /** Model used. */
  model?: string;
  /** Number of input tokens. */
  inputTokens?: number;
  /** Number of output tokens. */
  outputTokens?: number;
  /** Number of cache read tokens. */
  cacheReadTokens?: number;
  /** Number of cache write tokens. */
  cacheWriteTokens?: number;
  /** Cost of the API call. */
  cost?: number;
  /** Duration of the API call. */
  duration?: number;
}

// #endregion

/**
 * Data for abort events.
 */
export interface AbortData {
  /** Reason for the abort. */
  reason?: string;
  /** Source of the abort request. */
  source?: string;
}

/**
 * Streaming delta event (sent via SignalR).
 */
export interface StreamingDelta {
  /** Session ID this delta belongs to. */
  sessionId: string;
  /** Type of delta. */
  type: 'message' | 'reasoning';
  /** ID of the message or reasoning block. */
  id: string;
  /** The delta content. */
  content: string;
  /** Total response size in bytes so far. */
  totalBytes?: number;
}
