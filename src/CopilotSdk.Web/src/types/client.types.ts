/**
 * TypeScript types for Copilot client configuration and status.
 */

/**
 * Configuration for the Copilot client connection.
 */
export interface CopilotClientConfig {
  /** Path to the Copilot CLI executable. */
  cliPath?: string;
  /** Additional arguments to pass to the CLI. */
  cliArgs?: string[];
  /** URL of an existing Copilot CLI server to connect to. */
  cliUrl?: string;
  /** Port number for TCP connection. */
  port: number;
  /** Whether to use stdio for communication. */
  useStdio: boolean;
  /** Log level for the CLI server. */
  logLevel: string;
  /** Whether to automatically start the client on first operation. */
  autoStart: boolean;
  /** Whether to automatically restart the client on connection failure. */
  autoRestart: boolean;
  /** Working directory for the CLI process. */
  cwd?: string;
  /** Environment variables to pass to the CLI process. */
  environment?: Record<string, string>;
}

/**
 * Represents the current status of the Copilot client.
 */
export interface ClientStatus {
  /** Current connection state (Disconnected, Connecting, Connected, Error). */
  connectionState: string;
  /** Whether the client is currently connected. */
  isConnected: boolean;
  /** Timestamp when the client connected, if connected. */
  connectedAt?: string;
  /** Error message if the client is in an error state. */
  error?: string;
}

/**
 * Response model for client status queries.
 */
export interface ClientStatusResponse {
  /** Current connection state (Disconnected, Connecting, Connected, Error). */
  connectionState: string;
  /** Whether the client is currently connected. */
  isConnected: boolean;
  /** Timestamp when the client connected, if connected. */
  connectedAt?: string;
  /** Error message if the client is in an error state. */
  error?: string;
}

/**
 * Response model for client configuration queries.
 */
export interface ClientConfigResponse extends CopilotClientConfig {}

/**
 * Request model for updating client configuration.
 */
export interface UpdateClientConfigRequest extends Partial<CopilotClientConfig> {}

/**
 * Request model for ping operation.
 */
export interface PingRequest {
  /** Optional message to send with the ping. */
  message?: string;
}

/**
 * Response model for ping operation.
 */
export interface PingResponse {
  /** Whether the ping was successful. */
  success: boolean;
  /** Response message from the server. */
  message: string;
  /** Round-trip latency in milliseconds. */
  latencyMs: number;
}

/**
 * Connection state enum values.
 */
export type ConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Error';
