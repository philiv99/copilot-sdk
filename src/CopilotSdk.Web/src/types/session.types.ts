/**
 * TypeScript types for session management.
 */

/**
 * System message configuration for a session.
 */
export interface SystemMessageConfig {
  /** Mode for applying the system message: "Append" or "Replace". */
  mode: 'Append' | 'Replace';
  /** The content of the system message. */
  content: string;
}

/**
 * Configuration for a custom API provider (BYOK).
 */
export interface ProviderConfig {
  /** Type of provider (e.g., "openai", "azure"). */
  type: string;
  /** Base URL for the provider's API. */
  baseUrl?: string;
  /** API key for authentication. */
  apiKey?: string;
  /** Bearer token for authentication. */
  bearerToken?: string;
  /** Wire API format (e.g., "openai", "azure"). */
  wireApi?: string;
}

/**
 * Parameter definition for a custom tool.
 */
export interface ToolParameter {
  /** Name of the parameter. */
  name: string;
  /** Type of the parameter (e.g., "string", "number", "boolean", "object", "array"). */
  type: string;
  /** Description of what the parameter is used for. */
  description: string;
  /** Whether this parameter is required. */
  required: boolean;
}

/**
 * Custom tool definition for use in sessions.
 */
export interface ToolDefinition {
  /** Unique name of the tool. */
  name: string;
  /** Description of what the tool does. */
  description: string;
  /** List of parameters the tool accepts. */
  parameters?: ToolParameter[];
}

/**
 * Request model for creating a new session.
 */
export interface CreateSessionRequest {
  /** Optional custom session ID. */
  sessionId?: string;
  /** Model to use for the session. */
  model: string;
  /** Whether to enable streaming responses. */
  streaming: boolean;
  /** System message configuration. */
  systemMessage?: SystemMessageConfig;
  /** List of tool names that are available for this session. */
  availableTools?: string[];
  /** List of tool names to exclude from this session. */
  excludedTools?: string[];
  /** Custom provider configuration for BYOK scenarios. */
  provider?: ProviderConfig;
  /** Custom tool definitions for this session. */
  tools?: ToolDefinition[];
}

/**
 * Request model for resuming a session.
 */
export interface ResumeSessionRequest {
  /** Model to use when resuming. */
  model?: string;
  /** Whether to enable streaming. */
  streaming?: boolean;
}

/**
 * Session information response.
 */
export interface SessionInfoResponse {
  /** Unique identifier for the session. */
  sessionId: string;
  /** Model being used for this session. */
  model: string;
  /** Whether streaming is enabled for this session. */
  streaming: boolean;
  /** When the session was created. */
  createdAt: string;
  /** When the session was last active. */
  lastActivityAt?: string;
  /** Current status of the session. */
  status: string;
  /** Number of messages in the session. */
  messageCount: number;
  /** Summary of the session conversation. */
  summary?: string;
  /** ID of the user who created this session. */
  creatorUserId?: string;
  /** Display name of the user who created this session. */
  creatorDisplayName?: string;
}

/**
 * Response containing a list of sessions.
 */
export interface SessionListResponse {
  /** List of sessions. */
  sessions: SessionInfoResponse[];
  /** Total number of sessions. */
  totalCount: number;
}

/**
 * Session status values.
 */
export type SessionStatus = 'Active' | 'Idle' | 'Error' | 'Deleted';

/**
 * System prompt template information.
 */
export interface SystemPromptTemplate {
  /** The unique name of the template (folder name). */
  name: string;
  /** A display-friendly name. */
  displayName: string;
}

/**
 * Response containing a list of system prompt templates.
 */
export interface SystemPromptTemplatesResponse {
  /** The list of available templates. */
  templates: SystemPromptTemplate[];
}

/**
 * Response containing the content of a system prompt template.
 */
export interface SystemPromptTemplateContentResponse {
  /** The name of the template. */
  name: string;
  /** The content of the template (copilot-instructions.md). */
  content: string;
}
