/**
 * API client for communicating with the Copilot SDK backend.
 */
import axios, { AxiosInstance, AxiosError } from 'axios';
import {
  CopilotClientConfig,
  ClientStatusResponse,
  ClientConfigResponse,
  UpdateClientConfigRequest,
  PingRequest,
  PingResponse,
  CreateSessionRequest,
  ResumeSessionRequest,
  SessionInfoResponse,
  SessionListResponse,
  SendMessageRequest,
  SendMessageResponse,
  MessagesResponse,
  RefinePromptRequest,
  RefinePromptResponse,
  ModelsResponse,
  SystemPromptTemplatesResponse,
  SystemPromptTemplateContentResponse,
} from '../types';

/**
 * Error response from the API.
 */
export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
}

/**
 * Dev server response interface.
 */
export interface DevServerResponse {
  success: boolean;
  port: number;
  url: string;
  message: string;
}

/**
 * Dev server status response interface.
 */
export interface DevServerStatusResponse {
  isRunning: boolean;
  port?: number;
  url?: string;
}

/**
 * Base URL for the API.
 * Reads from REACT_APP_API_BASE_URL environment variable (set in .env).
 * Falls back to localhost for development.
 */
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5139/api/copilot';

/**
 * Axios instance with common configuration.
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Helper to extract error message from API response.
 */
function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<ApiError>;
    if (axiosError.response?.data?.detail) {
      return axiosError.response.data.detail;
    }
    if (axiosError.response?.data?.title) {
      return axiosError.response.data.title;
    }
    if (axiosError.message) {
      return axiosError.message;
    }
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred';
}

// #region Client Operations

/**
 * Get the current status of the Copilot client.
 */
export async function getClientStatus(): Promise<ClientStatusResponse> {
  try {
    const response = await apiClient.get<ClientStatusResponse>('/client/status');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Get the current configuration of the Copilot client.
 */
export async function getClientConfig(): Promise<ClientConfigResponse> {
  try {
    const response = await apiClient.get<ClientConfigResponse>('/client/config');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Update the Copilot client configuration.
 */
export async function updateClientConfig(config: UpdateClientConfigRequest): Promise<ClientConfigResponse> {
  try {
    const response = await apiClient.put<ClientConfigResponse>('/client/config', config);
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Start the Copilot client.
 */
export async function startClient(): Promise<ClientStatusResponse> {
  try {
    const response = await apiClient.post<ClientStatusResponse>('/client/start');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Stop the Copilot client gracefully.
 */
export async function stopClient(): Promise<ClientStatusResponse> {
  try {
    const response = await apiClient.post<ClientStatusResponse>('/client/stop');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Force stop the Copilot client.
 */
export async function forceStopClient(): Promise<ClientStatusResponse> {
  try {
    const response = await apiClient.post<ClientStatusResponse>('/client/force-stop');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Ping the Copilot client to check connectivity.
 */
export async function pingClient(request?: PingRequest): Promise<PingResponse> {
  try {
    const response = await apiClient.post<PingResponse>('/client/ping', request ?? {});
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region Session Operations

/**
 * Get list of all sessions.
 */
export async function listSessions(): Promise<SessionListResponse> {
  try {
    const response = await apiClient.get<SessionListResponse>('/sessions');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Create a new session.
 */
export async function createSession(request: CreateSessionRequest): Promise<SessionInfoResponse> {
  try {
    const response = await apiClient.post<SessionInfoResponse>('/sessions', request);
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Get a specific session by ID.
 */
export async function getSession(sessionId: string): Promise<SessionInfoResponse> {
  try {
    const response = await apiClient.get<SessionInfoResponse>(`/sessions/${encodeURIComponent(sessionId)}`);
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Delete a session.
 */
export async function deleteSession(sessionId: string): Promise<void> {
  try {
    await apiClient.delete(`/sessions/${encodeURIComponent(sessionId)}`);
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Resume an existing session.
 */
export async function resumeSession(sessionId: string, request?: ResumeSessionRequest): Promise<SessionInfoResponse> {
  try {
    const response = await apiClient.post<SessionInfoResponse>(
      `/sessions/${encodeURIComponent(sessionId)}/resume`,
      request ?? {}
    );
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region Message Operations

/**
 * Send a message to a session.
 */
export async function sendMessage(sessionId: string, request: SendMessageRequest): Promise<SendMessageResponse> {
  try {
    const response = await apiClient.post<SendMessageResponse>(
      `/sessions/${encodeURIComponent(sessionId)}/messages`,
      request
    );
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Get messages from a session.
 */
export async function getMessages(sessionId: string): Promise<MessagesResponse> {
  try {
    const response = await apiClient.get<MessagesResponse>(
      `/sessions/${encodeURIComponent(sessionId)}/messages`
    );
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Abort the current message processing in a session.
 */
export async function abortSession(sessionId: string): Promise<void> {
  try {
    await apiClient.post(`/sessions/${encodeURIComponent(sessionId)}/abort`);
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region Prompt Refinement Operations

/**
 * Refine a system message prompt using the Copilot LLM.
 * Sends the content to be expanded and improved as a clearer requirements statement.
 * @param request The refinement request containing the content to refine.
 * @returns The refinement response with the improved content.
 */
export async function refineSystemMessage(request: RefinePromptRequest): Promise<RefinePromptResponse> {
  try {
    console.log('[copilotApi] refineSystemMessage - sending request...');
    const response = await apiClient.post<RefinePromptResponse>('/refine-prompt', request);
    console.log('[copilotApi] refineSystemMessage - response received:', response.status);
    return response.data;
  } catch (error) {
    console.error('[copilotApi] refineSystemMessage - error:', error);
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region Models Operations

/**
 * Get the list of available AI models.
 * The list is cached on the server for one week.
 */
export async function getModels(): Promise<ModelsResponse> {
  try {
    const response = await apiClient.get<ModelsResponse>('/models');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Force refresh the cached models list.
 */
export async function refreshModels(): Promise<ModelsResponse> {
  try {
    const response = await apiClient.post<ModelsResponse>('/models/refresh');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region System Prompt Templates Operations

/**
 * Get the list of available system prompt templates.
 * Templates are folders in docs/system_prompts that contain a copilot-instructions.md file.
 */
export async function getSystemPromptTemplates(): Promise<SystemPromptTemplatesResponse> {
  try {
    const response = await apiClient.get<SystemPromptTemplatesResponse>('/system-prompt-templates');
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Get the content of a specific system prompt template.
 * @param templateName The name of the template (folder name).
 */
export async function getSystemPromptTemplateContent(templateName: string): Promise<SystemPromptTemplateContentResponse> {
  try {
    const response = await apiClient.get<SystemPromptTemplateContentResponse>(
      `/system-prompt-templates/${encodeURIComponent(templateName)}`
    );
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// #region Dev Server Operations

/**
 * Start the development server for a session's app.
 * @param sessionId The session ID.
 * @param appPath Optional override for the app path.
 */
export async function startDevServer(sessionId: string, appPath?: string): Promise<DevServerResponse> {
  try {
    const queryParams = appPath ? `?appPath=${encodeURIComponent(appPath)}` : '';
    const response = await apiClient.post<DevServerResponse>(
      `/sessions/${sessionId}/dev-server/start${queryParams}`
    );
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Stop the development server for a session.
 * @param sessionId The session ID.
 */
export async function stopDevServer(sessionId: string): Promise<void> {
  try {
    await apiClient.post(`/sessions/${sessionId}/dev-server/stop`);
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

/**
 * Get the status of the development server for a session.
 * @param sessionId The session ID.
 */
export async function getDevServerStatus(sessionId: string): Promise<DevServerStatusResponse> {
  try {
    const response = await apiClient.get<DevServerStatusResponse>(`/sessions/${sessionId}/dev-server/status`);
    return response.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

// #endregion

// Export the axios instance for advanced use cases
export { apiClient };
