# Copilot SDK App Plans

This document outlines the comprehensive plan for implementing a full-stack application that exercises all features of the GitHub Copilot SDK (.NET).

## Table of Contents

1. [SDK Features Summary](#sdk-features-summary)
2. [Data Models](#data-models)
3. [UI Layout Plan](#ui-layout-plan)
4. [RESTful API Endpoints](#restful-api-endpoints)
5. [Backend Logic & Structure](#backend-logic--structure)

---

## SDK Features Summary

Based on the `github-copilot-sdk/dotnet/README.md`, the SDK provides:

### CopilotClient Features
- **Client Configuration**: CliPath, CliArgs, CliUrl, Port, UseStdio, LogLevel, AutoStart, AutoRestart, Cwd, Environment, Logger
- **Connection Management**: StartAsync, StopAsync, ForceStopAsync
- **Session Management**: CreateSessionAsync, ResumeSessionAsync, ListSessionsAsync, DeleteSessionAsync
- **Health Check**: PingAsync
- **State Tracking**: ConnectionState property

### Session Features
- **Session Configuration**: SessionId, Model, Tools, SystemMessage, AvailableTools, ExcludedTools, Provider (BYOK), Streaming
- **Messaging**: SendAsync with Prompt, Attachments, Mode (enqueue/immediate)
- **Event Subscription**: On() for event handling
- **Control**: AbortAsync, GetMessagesAsync, DisposeAsync

### Event Types
- UserMessageEvent
- AssistantMessageEvent
- AssistantMessageDeltaEvent (streaming)
- AssistantReasoningEvent
- AssistantReasoningDeltaEvent (streaming)
- ToolExecutionStartEvent
- ToolExecutionCompleteEvent
- SessionStartEvent
- SessionIdleEvent
- SessionErrorEvent

### Advanced Features
- **Custom Tools**: AIFunctionFactory.Create for type-safe tool definitions
- **System Message Customization**: Append/Replace modes
- **Multiple Sessions**: Independent concurrent sessions
- **File Attachments**: Attach files to messages
- **BYOK (Bring Your Own Key)**: Custom API provider configuration

---

## Data Models

### Backend Models (C#)

```csharp
// ============================================
// Client Configuration Models
// ============================================

public class CopilotClientConfig
{
    public string? CliPath { get; set; }
    public string[]? CliArgs { get; set; }
    public string? CliUrl { get; set; }
    public int Port { get; set; } = 0;
    public bool UseStdio { get; set; } = true;
    public string LogLevel { get; set; } = "info";
    public bool AutoStart { get; set; } = true;
    public bool AutoRestart { get; set; } = true;
    public string? Cwd { get; set; }
    public Dictionary<string, string>? Environment { get; set; }
}

public class ClientStatus
{
    public string ConnectionState { get; set; } = "Disconnected";
    public bool IsConnected { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public string? Error { get; set; }
}

// ============================================
// Session Configuration Models
// ============================================

public class CreateSessionRequest
{
    public string? SessionId { get; set; }
    public string Model { get; set; } = "gpt-5";
    public bool Streaming { get; set; } = false;
    public SystemMessageConfig? SystemMessage { get; set; }
    public List<string>? AvailableTools { get; set; }
    public List<string>? ExcludedTools { get; set; }
    public ProviderConfig? Provider { get; set; }
    public List<ToolDefinition>? Tools { get; set; }
}

public class SystemMessageConfig
{
    public string Mode { get; set; } = "Append"; // "Append" or "Replace"
    public string Content { get; set; } = string.Empty;
}

public class ProviderConfig
{
    public string Type { get; set; } = "openai";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ToolParameter>? Parameters { get; set; }
}

public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
}

// ============================================
// Session Response Models
// ============================================

public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool Streaming { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Active";
}

public class SessionMetadata
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}

// ============================================
// Message Models
// ============================================

public class SendMessageRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Mode { get; set; } = "enqueue"; // "enqueue" or "immediate"
    public List<MessageAttachment>? Attachments { get; set; }
}

public class MessageAttachment
{
    public string Type { get; set; } = "file";
    public string Path { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class SendMessageResponse
{
    public string MessageId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}

// ============================================
// Event Models
// ============================================

public class SessionEvent
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

public class UserMessageData
{
    public string Content { get; set; } = string.Empty;
    public List<MessageAttachment>? Attachments { get; set; }
}

public class AssistantMessageData
{
    public string Content { get; set; } = string.Empty;
    public string? DeltaContent { get; set; } // For streaming
}

public class AssistantReasoningData
{
    public string Content { get; set; } = string.Empty;
    public string? DeltaContent { get; set; } // For streaming
}

public class ToolExecutionData
{
    public string ToolName { get; set; } = string.Empty;
    public string? ToolId { get; set; }
    public object? Arguments { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}

public class SessionErrorData
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}

// ============================================
// Ping/Health Models
// ============================================

public class PingRequest
{
    public string? Message { get; set; }
}

public class PingResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long LatencyMs { get; set; }
}
```

### Frontend Models (TypeScript)

```typescript
// ============================================
// Client Configuration Types
// ============================================

interface CopilotClientConfig {
  cliPath?: string;
  cliArgs?: string[];
  cliUrl?: string;
  port?: number;
  useStdio?: boolean;
  logLevel?: string;
  autoStart?: boolean;
  autoRestart?: boolean;
  cwd?: string;
  environment?: Record<string, string>;
}

interface ClientStatus {
  connectionState: 'Disconnected' | 'Connecting' | 'Connected' | 'Error';
  isConnected: boolean;
  connectedAt?: string;
  error?: string;
}

// ============================================
// Session Configuration Types
// ============================================

interface CreateSessionRequest {
  sessionId?: string;
  model: string;
  streaming?: boolean;
  systemMessage?: SystemMessageConfig;
  availableTools?: string[];
  excludedTools?: string[];
  provider?: ProviderConfig;
  tools?: ToolDefinition[];
}

interface SystemMessageConfig {
  mode: 'Append' | 'Replace';
  content: string;
}

interface ProviderConfig {
  type: string;
  baseUrl?: string;
  apiKey?: string;
}

interface ToolDefinition {
  name: string;
  description: string;
  parameters?: ToolParameter[];
}

interface ToolParameter {
  name: string;
  type: string;
  description: string;
  required?: boolean;
}

// ============================================
// Session Types
// ============================================

interface SessionInfo {
  sessionId: string;
  model: string;
  streaming: boolean;
  createdAt: string;
  status: string;
}

interface SessionMetadata {
  sessionId: string;
  createdAt?: string;
  lastActivityAt?: string;
  messageCount: number;
}

// ============================================
// Message Types
// ============================================

interface SendMessageRequest {
  prompt: string;
  mode?: 'enqueue' | 'immediate';
  attachments?: MessageAttachment[];
}

interface MessageAttachment {
  type: 'file';
  path: string;
  displayName?: string;
}

interface SendMessageResponse {
  messageId: string;
  success: boolean;
  error?: string;
}

// ============================================
// Event Types
// ============================================

interface SessionEvent {
  type: string;
  timestamp: string;
  data?: unknown;
}

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system' | 'tool';
  content: string;
  timestamp: string;
  attachments?: MessageAttachment[];
  toolExecution?: ToolExecutionData;
  reasoning?: string;
  isStreaming?: boolean;
}

interface ToolExecutionData {
  toolName: string;
  toolId?: string;
  arguments?: unknown;
  result?: unknown;
  error?: string;
}

// ============================================
// Ping/Health Types
// ============================================

interface PingResponse {
  success: boolean;
  message?: string;
  latencyMs: number;
}
```

---

## UI Layout Plan

### Application Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                        Header / App Bar                          │
│  [Logo] Copilot SDK Explorer    [Connection Status] [Settings]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐  ┌─────────────────────────────────────┐  │
│  │                  │  │                                     │  │
│  │  Sidebar         │  │  Main Content Area                  │  │
│  │  ───────────     │  │  ─────────────────                  │  │
│  │                  │  │                                     │  │
│  │  [+ New Session] │  │  (Session Chat / Configuration /    │  │
│  │                  │  │   Dashboard based on selection)     │  │
│  │  Sessions List   │  │                                     │  │
│  │  • Session 1     │  │                                     │  │
│  │  • Session 2     │  │                                     │  │
│  │  • Session 3     │  │                                     │  │
│  │                  │  │                                     │  │
│  │  ───────────     │  │                                     │  │
│  │  Client Config   │  │                                     │  │
│  │  [Configure]     │  │                                     │  │
│  │                  │  │                                     │  │
│  └──────────────────┘  └─────────────────────────────────────┘  │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                        Status Bar                                │
│  Connection: ● Connected | Sessions: 3 | Server: localhost:8080  │
└─────────────────────────────────────────────────────────────────┘
```

### Page/View Components

#### 1. Dashboard View (Home)
- Client connection status card
- Quick actions (Start/Stop Client, Create Session)
- Recent sessions list
- Server health indicator (Ping)
- Statistics (total sessions, messages sent, etc.)

#### 2. Client Configuration View
```
┌─────────────────────────────────────────────────────────────┐
│  Client Configuration                                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Connection Settings                                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ CLI Path:        [________________] [Browse]         │   │
│  │ CLI URL:         [________________] (for remote)     │   │
│  │ Port:            [0________] (0 = random)            │   │
│  │ Use Stdio:       [✓]                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Behavior Settings                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Auto Start:      [✓]                                 │   │
│  │ Auto Restart:    [✓]                                 │   │
│  │ Log Level:       [info ▼]                            │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Process Settings                                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Working Directory: [________________] [Browse]       │   │
│  │ CLI Arguments:     [________________]                │   │
│  │ Environment Variables:                               │   │
│  │   [+ Add Variable]                                   │   │
│  │   KEY1 = VALUE1  [×]                                 │   │
│  │   KEY2 = VALUE2  [×]                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Actions                                                    │
│  [Save Configuration] [Start Client] [Stop Client]          │
│  [Force Stop] [Ping Server]                                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### 3. Create Session View / Modal
```
┌─────────────────────────────────────────────────────────────┐
│  Create New Session                                    [×]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Basic Settings                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Session ID:      [________________] (optional)       │   │
│  │ Model:           [gpt-5 ▼]                           │   │
│  │                  • gpt-5                             │   │
│  │                  • claude-sonnet-4.5                 │   │
│  │                  • (custom)                          │   │
│  │ Enable Streaming: [✓]                                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  System Message Configuration                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Mode:            [Append ▼]                          │   │
│  │ Content:                                             │   │
│  │ ┌─────────────────────────────────────────────────┐ │   │
│  │ │ <workflow_rules>                                │ │   │
│  │ │ - Always check for security vulnerabilities     │ │   │
│  │ │ </workflow_rules>                               │ │   │
│  │ └─────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Tool Configuration                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Available Tools: (comma-separated or select)         │   │
│  │ [________________] [Add from List]                   │   │
│  │ Excluded Tools:                                      │   │
│  │ [________________] [Add from List]                   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Custom Tools (Optional)                                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [+ Add Custom Tool]                                  │   │
│  │ ┌─────────────────────────────────────────────┐     │   │
│  │ │ Name: [lookup_issue]        [×]             │     │   │
│  │ │ Description: [Fetch issue details]          │     │   │
│  │ │ Parameters: [+ Add Parameter]               │     │   │
│  │ │   id (string, required): Issue identifier   │     │   │
│  │ └─────────────────────────────────────────────┘     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  BYOK Provider (Optional)                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [✓] Use Custom Provider                              │   │
│  │ Type:     [openai ▼]                                 │   │
│  │ Base URL: [https://api.openai.com/v1]                │   │
│  │ API Key:  [••••••••••••••••]                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│                          [Cancel] [Create Session]          │
└─────────────────────────────────────────────────────────────┘
```

#### 4. Session Chat View
```
┌─────────────────────────────────────────────────────────────┐
│  Session: abc-123-def    Model: gpt-5    [⚙ Config] [🗑 Delete]│
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │                   Chat History                        │   │
│  │  ───────────────────────────────────────────────────  │   │
│  │                                                       │   │
│  │  [User] 10:30 AM                                      │   │
│  │  What is 2+2?                                         │   │
│  │                                                       │   │
│  │  [Assistant] 10:30 AM                                 │   │
│  │  2 + 2 equals 4.                                      │   │
│  │  ┌─ Reasoning (collapsed) ─────────────────────┐     │   │
│  │  │ This is a simple arithmetic calculation...   │     │   │
│  │  └──────────────────────────────────────────────┘     │   │
│  │                                                       │   │
│  │  [Tool] 10:31 AM                                      │   │
│  │  ┌─ lookup_issue ────────────────────────────┐       │   │
│  │  │ Arguments: { "id": "123" }                 │       │   │
│  │  │ Result: { "title": "Bug fix", ... }        │       │   │
│  │  └────────────────────────────────────────────┘       │   │
│  │                                                       │   │
│  │  [Assistant] 10:31 AM                                 │   │
│  │  I found the issue. The title is "Bug fix"...         │   │
│  │                                                       │   │
│  │  ● Streaming response...                              │   │
│  │  The answer to your question is...                    │   │
│  │                                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Attachments:                                          │   │
│  │ [📎 file.cs] [×]   [+ Add File]                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [                    Message Input                  ] │   │
│  │ Mode: [enqueue ▼]                     [Send] [Abort] │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### 5. Sessions List View (Sidebar Detail)
```
┌───────────────────────┐
│  Sessions             │
│  ─────────────────    │
│  [+ New Session]      │
│  [⟳ Refresh List]     │
│                       │
│  ┌─────────────────┐  │
│  │ ● Session-001   │  │
│  │   gpt-5         │  │
│  │   Created: 10:30│  │
│  │   [Resume][Del] │  │
│  └─────────────────┘  │
│                       │
│  ┌─────────────────┐  │
│  │ ○ Session-002   │  │
│  │   claude-4.5    │  │
│  │   Created: 10:25│  │
│  │   [Resume][Del] │  │
│  └─────────────────┘  │
│                       │
│  ● = Active           │
│  ○ = Inactive         │
└───────────────────────┘
```

#### 6. Events/Log Viewer (Optional Panel)
```
┌─────────────────────────────────────────────────────────────┐
│  Event Log                                    [Clear] [⚙]   │
├─────────────────────────────────────────────────────────────┤
│  [Filter: All ▼] [Search: ________]                         │
│                                                             │
│  10:31:45 SessionStartEvent      session-001                │
│  10:31:46 UserMessageEvent       "What is 2+2?"             │
│  10:31:47 AssistantMessageDelta  "2 + "                     │
│  10:31:47 AssistantMessageDelta  "2 equals "                │
│  10:31:48 AssistantMessageDelta  "4."                       │
│  10:31:48 AssistantMessageEvent  "2 + 2 equals 4."          │
│  10:31:48 SessionIdleEvent       session-001                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### React Component Hierarchy

```
App
├── Header
│   ├── Logo
│   ├── ConnectionStatusIndicator
│   └── SettingsButton
├── Sidebar
│   ├── CreateSessionButton
│   ├── SessionsList
│   │   └── SessionListItem (per session)
│   └── ClientConfigButton
├── MainContent
│   ├── DashboardView (default)
│   │   ├── ConnectionStatusCard
│   │   ├── QuickActionsCard
│   │   ├── RecentSessionsCard
│   │   └── StatisticsCard
│   ├── ClientConfigView
│   │   ├── ConnectionSettingsForm
│   │   ├── BehaviorSettingsForm
│   │   ├── ProcessSettingsForm
│   │   └── ActionButtons
│   ├── SessionChatView
│   │   ├── SessionHeader
│   │   ├── ChatHistory
│   │   │   ├── UserMessage
│   │   │   ├── AssistantMessage
│   │   │   │   └── ReasoningCollapsible
│   │   │   ├── ToolExecutionCard
│   │   │   └── StreamingIndicator
│   │   ├── AttachmentsPanel
│   │   └── MessageInput
│   │       ├── TextArea
│   │       ├── ModeSelector
│   │       ├── SendButton
│   │       └── AbortButton
│   └── EventLogPanel (optional, collapsible)
├── CreateSessionModal
│   ├── BasicSettingsSection
│   ├── SystemMessageSection
│   ├── ToolConfigSection
│   ├── CustomToolsSection
│   └── ProviderSection
└── StatusBar
    ├── ConnectionStatus
    ├── SessionCount
    └── ServerInfo
```

---

## RESTful API Endpoints

### Base URL: `/api/copilot`

### Client Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/client/status` | Get current client connection status |
| GET | `/client/config` | Get current client configuration |
| PUT | `/client/config` | Update client configuration |
| POST | `/client/start` | Start the Copilot client |
| POST | `/client/stop` | Stop the Copilot client gracefully |
| POST | `/client/force-stop` | Force stop the Copilot client |
| POST | `/client/ping` | Ping the server to check connectivity |

### Session Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/sessions` | List all sessions |
| POST | `/sessions` | Create a new session |
| GET | `/sessions/{sessionId}` | Get session details |
| DELETE | `/sessions/{sessionId}` | Delete a session |
| POST | `/sessions/{sessionId}/resume` | Resume an existing session |

### Session Messaging

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/sessions/{sessionId}/messages` | Send a message to a session |
| GET | `/sessions/{sessionId}/messages` | Get all messages/events from a session |
| POST | `/sessions/{sessionId}/abort` | Abort the currently processing message |

### Real-time Events (SignalR Hub)

| Hub | Method | Description |
|-----|--------|-------------|
| `/hubs/session` | JoinSession | Join a session's event stream |
| `/hubs/session` | LeaveSession | Leave a session's event stream |
| `/hubs/session` | OnSessionEvent | Receive session events (server-to-client) |
| `/hubs/session` | OnStreamingDelta | Receive streaming deltas (server-to-client) |

### API Endpoint Details

#### GET `/api/copilot/client/status`
**Response:**
```json
{
  "connectionState": "Connected",
  "isConnected": true,
  "connectedAt": "2026-01-18T10:30:00Z",
  "error": null
}
```

#### PUT `/api/copilot/client/config`
**Request:**
```json
{
  "cliPath": "/usr/local/bin/copilot",
  "cliUrl": null,
  "port": 0,
  "useStdio": true,
  "logLevel": "info",
  "autoStart": true,
  "autoRestart": true,
  "cwd": "/app",
  "cliArgs": ["--verbose"],
  "environment": {
    "COPILOT_DEBUG": "true"
  }
}
```

#### POST `/api/copilot/client/ping`
**Request:**
```json
{
  "message": "hello"
}
```
**Response:**
```json
{
  "success": true,
  "message": "hello",
  "latencyMs": 15
}
```

#### POST `/api/copilot/sessions`
**Request:**
```json
{
  "sessionId": "my-custom-session",
  "model": "gpt-5",
  "streaming": true,
  "systemMessage": {
    "mode": "Append",
    "content": "<rules>Always be helpful</rules>"
  },
  "availableTools": ["file_read", "file_write"],
  "excludedTools": ["dangerous_tool"],
  "tools": [
    {
      "name": "lookup_issue",
      "description": "Fetch issue details from tracker",
      "parameters": [
        {
          "name": "id",
          "type": "string",
          "description": "Issue identifier",
          "required": true
        }
      ]
    }
  ],
  "provider": {
    "type": "openai",
    "baseUrl": "https://api.openai.com/v1",
    "apiKey": "sk-..."
  }
}
```
**Response:**
```json
{
  "sessionId": "my-custom-session",
  "model": "gpt-5",
  "streaming": true,
  "createdAt": "2026-01-18T10:30:00Z",
  "status": "Active"
}
```

#### GET `/api/copilot/sessions`
**Response:**
```json
{
  "sessions": [
    {
      "sessionId": "session-001",
      "createdAt": "2026-01-18T10:30:00Z",
      "lastActivityAt": "2026-01-18T10:35:00Z",
      "messageCount": 5
    },
    {
      "sessionId": "session-002",
      "createdAt": "2026-01-18T10:25:00Z",
      "lastActivityAt": "2026-01-18T10:28:00Z",
      "messageCount": 3
    }
  ]
}
```

#### POST `/api/copilot/sessions/{sessionId}/messages`
**Request:**
```json
{
  "prompt": "What is 2+2?",
  "mode": "enqueue",
  "attachments": [
    {
      "type": "file",
      "path": "/path/to/file.cs",
      "displayName": "MyFile.cs"
    }
  ]
}
```
**Response:**
```json
{
  "messageId": "msg-abc-123",
  "success": true,
  "error": null
}
```

#### GET `/api/copilot/sessions/{sessionId}/messages`
**Response:**
```json
{
  "events": [
    {
      "type": "SessionStartEvent",
      "timestamp": "2026-01-18T10:30:00Z",
      "data": {}
    },
    {
      "type": "UserMessageEvent",
      "timestamp": "2026-01-18T10:30:01Z",
      "data": {
        "content": "What is 2+2?"
      }
    },
    {
      "type": "AssistantMessageEvent",
      "timestamp": "2026-01-18T10:30:02Z",
      "data": {
        "content": "2 + 2 equals 4."
      }
    },
    {
      "type": "SessionIdleEvent",
      "timestamp": "2026-01-18T10:30:02Z",
      "data": {}
    }
  ]
}
```

---

## Backend Logic & Structure

### Project Structure

```
CopilotSdk.Api/
├── Controllers/
│   ├── CopilotClientController.cs    # Client management endpoints
│   └── SessionsController.cs         # Session management endpoints
├── Hubs/
│   └── SessionHub.cs                 # SignalR hub for real-time events
├── Models/
│   ├── Requests/
│   │   ├── CreateSessionRequest.cs
│   │   ├── SendMessageRequest.cs
│   │   ├── UpdateClientConfigRequest.cs
│   │   └── PingRequest.cs
│   ├── Responses/
│   │   ├── ClientStatusResponse.cs
│   │   ├── SessionInfoResponse.cs
│   │   ├── SessionListResponse.cs
│   │   ├── SendMessageResponse.cs
│   │   ├── MessagesResponse.cs
│   │   └── PingResponse.cs
│   └── Domain/
│       ├── CopilotClientConfig.cs
│       ├── SessionConfig.cs
│       ├── SystemMessageConfig.cs
│       ├── ProviderConfig.cs
│       ├── ToolDefinition.cs
│       └── SessionEvent.cs
├── Services/
│   ├── ICopilotClientService.cs      # Interface for client management
│   ├── CopilotClientService.cs       # Implementation
│   ├── ISessionService.cs            # Interface for session management
│   ├── SessionService.cs             # Implementation
│   ├── IToolExecutionService.cs      # Interface for custom tool handling
│   └── ToolExecutionService.cs       # Implementation
├── Managers/
│   ├── CopilotClientManager.cs       # Singleton managing CopilotClient lifecycle
│   └── SessionManager.cs             # Manages active sessions
├── EventHandlers/
│   └── SessionEventDispatcher.cs     # Routes SDK events to SignalR
├── Middleware/
│   └── ErrorHandlingMiddleware.cs    # Global error handling
└── Program.cs                        # App configuration
```

### Service Layer Design

#### ICopilotClientService
```csharp
public interface ICopilotClientService
{
    Task<ClientStatus> GetStatusAsync();
    CopilotClientConfig GetConfig();
    Task UpdateConfigAsync(CopilotClientConfig config);
    Task StartAsync();
    Task StopAsync();
    Task ForceStopAsync();
    Task<PingResponse> PingAsync(string? message = null);
}
```

#### ISessionService
```csharp
public interface ISessionService
{
    Task<SessionInfo> CreateSessionAsync(CreateSessionRequest request);
    Task<SessionInfo> ResumeSessionAsync(string sessionId, ResumeSessionConfig? config = null);
    Task<IReadOnlyList<SessionMetadata>> ListSessionsAsync();
    Task<SessionInfo?> GetSessionAsync(string sessionId);
    Task DeleteSessionAsync(string sessionId);
    Task<SendMessageResponse> SendMessageAsync(string sessionId, SendMessageRequest request);
    Task<IReadOnlyList<SessionEvent>> GetMessagesAsync(string sessionId);
    Task AbortAsync(string sessionId);
    void SubscribeToEvents(string sessionId, Action<SessionEvent> handler);
    void UnsubscribeFromEvents(string sessionId, Action<SessionEvent> handler);
}
```

#### IToolExecutionService
```csharp
public interface IToolExecutionService
{
    void RegisterTool(ToolDefinition definition, Func<object, Task<object>> handler);
    void UnregisterTool(string toolName);
    Task<object> ExecuteToolAsync(string toolName, object arguments);
    IReadOnlyList<ToolDefinition> GetRegisteredTools();
}
```

### CopilotClientManager (Singleton)

```csharp
public class CopilotClientManager : IAsyncDisposable
{
    private CopilotClient? _client;
    private CopilotClientConfig _config;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, CopilotSession> _activeSessions = new();
    
    public ConnectionState State => _client?.State ?? ConnectionState.Disconnected;
    
    public async Task StartAsync(CopilotClientConfig? config = null);
    public async Task StopAsync();
    public async Task ForceStopAsync();
    public async Task<PingResponse> PingAsync(string? message = null);
    
    public async Task<CopilotSession> CreateSessionAsync(SessionConfig config);
    public async Task<CopilotSession> ResumeSessionAsync(string sessionId, ResumeSessionConfig? config = null);
    public async Task<IReadOnlyList<SessionMetadata>> ListSessionsAsync();
    public async Task DeleteSessionAsync(string sessionId);
    
    public CopilotSession? GetSession(string sessionId);
    
    public async ValueTask DisposeAsync();
}
```

### SessionManager

```csharp
public class SessionManager
{
    private readonly ConcurrentDictionary<string, ActiveSessionInfo> _sessions = new();
    private readonly IHubContext<SessionHub> _hubContext;
    
    public void TrackSession(string sessionId, CopilotSession session, SessionConfig config);
    public void UntrackSession(string sessionId);
    public ActiveSessionInfo? GetSessionInfo(string sessionId);
    public IReadOnlyList<ActiveSessionInfo> GetAllSessions();
    
    // Event routing
    public void SetupEventHandlers(string sessionId, CopilotSession session);
    private async Task DispatchEventAsync(string sessionId, SessionEvent evt);
}

public class ActiveSessionInfo
{
    public string SessionId { get; set; }
    public CopilotSession Session { get; set; }
    public SessionConfig Config { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsProcessing { get; set; }
}
```

### SessionEventDispatcher

```csharp
public class SessionEventDispatcher
{
    private readonly IHubContext<SessionHub> _hubContext;
    
    public async Task DispatchAsync(string sessionId, object evt)
    {
        var eventDto = MapToDto(evt);
        
        // Send to all clients subscribed to this session
        await _hubContext.Clients
            .Group($"session-{sessionId}")
            .SendAsync("OnSessionEvent", eventDto);
        
        // Handle streaming deltas separately for better UX
        if (evt is AssistantMessageDeltaEvent delta)
        {
            await _hubContext.Clients
                .Group($"session-{sessionId}")
                .SendAsync("OnStreamingDelta", new
                {
                    sessionId,
                    deltaContent = delta.Data.DeltaContent,
                    timestamp = DateTime.UtcNow
                });
        }
    }
    
    private SessionEventDto MapToDto(object evt) { /* mapping logic */ }
}
```

### SignalR Hub

```csharp
[Authorize]
public class SessionHub : Hub
{
    private readonly SessionManager _sessionManager;
    
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }
    
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Cleanup logic
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Controller Implementation Examples

#### CopilotClientController
```csharp
[ApiController]
[Route("api/copilot/client")]
public class CopilotClientController : ControllerBase
{
    private readonly ICopilotClientService _clientService;
    
    [HttpGet("status")]
    public async Task<ActionResult<ClientStatusResponse>> GetStatus()
    {
        var status = await _clientService.GetStatusAsync();
        return Ok(status);
    }
    
    [HttpGet("config")]
    public ActionResult<CopilotClientConfig> GetConfig()
    {
        return Ok(_clientService.GetConfig());
    }
    
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateClientConfigRequest request)
    {
        await _clientService.UpdateConfigAsync(request.ToConfig());
        return NoContent();
    }
    
    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        await _clientService.StartAsync();
        return Ok(new { message = "Client started successfully" });
    }
    
    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await _clientService.StopAsync();
        return Ok(new { message = "Client stopped successfully" });
    }
    
    [HttpPost("force-stop")]
    public async Task<IActionResult> ForceStop()
    {
        await _clientService.ForceStopAsync();
        return Ok(new { message = "Client force stopped" });
    }
    
    [HttpPost("ping")]
    public async Task<ActionResult<PingResponse>> Ping([FromBody] PingRequest? request)
    {
        var response = await _clientService.PingAsync(request?.Message);
        return Ok(response);
    }
}
```

#### SessionsController
```csharp
[ApiController]
[Route("api/copilot/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    
    [HttpGet]
    public async Task<ActionResult<SessionListResponse>> ListSessions()
    {
        var sessions = await _sessionService.ListSessionsAsync();
        return Ok(new SessionListResponse { Sessions = sessions });
    }
    
    [HttpPost]
    public async Task<ActionResult<SessionInfoResponse>> CreateSession([FromBody] CreateSessionRequest request)
    {
        var session = await _sessionService.CreateSessionAsync(request);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.SessionId }, session);
    }
    
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<SessionInfoResponse>> GetSession(string sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null) return NotFound();
        return Ok(session);
    }
    
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        await _sessionService.DeleteSessionAsync(sessionId);
        return NoContent();
    }
    
    [HttpPost("{sessionId}/resume")]
    public async Task<ActionResult<SessionInfoResponse>> ResumeSession(string sessionId)
    {
        var session = await _sessionService.ResumeSessionAsync(sessionId);
        return Ok(session);
    }
    
    [HttpPost("{sessionId}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        string sessionId, 
        [FromBody] SendMessageRequest request)
    {
        var response = await _sessionService.SendMessageAsync(sessionId, request);
        return Ok(response);
    }
    
    [HttpGet("{sessionId}/messages")]
    public async Task<ActionResult<MessagesResponse>> GetMessages(string sessionId)
    {
        var events = await _sessionService.GetMessagesAsync(sessionId);
        return Ok(new MessagesResponse { Events = events });
    }
    
    [HttpPost("{sessionId}/abort")]
    public async Task<IActionResult> AbortMessage(string sessionId)
    {
        await _sessionService.AbortAsync(sessionId);
        return Ok(new { message = "Abort requested" });
    }
}
```

### Dependency Injection Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<CopilotClientManager>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddScoped<ICopilotClientService, CopilotClientService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IToolExecutionService, ToolExecutionService>();

// Add SignalR
builder.Services.AddSignalR();

// Add controllers
builder.Services.AddControllers();

// Add CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("ReactApp");
app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

// Ensure client manager is disposed on shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    var manager = app.Services.GetRequiredService<CopilotClientManager>();
    await manager.DisposeAsync();
});

app.Run();
```

### Error Handling Middleware

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (StreamJsonRpc.RemoteInvocationException ex)
        {
            _logger.LogError(ex, "JSON-RPC error");
            await WriteErrorResponse(context, 502, "Copilot CLI error", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
        {
            await WriteErrorResponse(context, 503, "Client not connected", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorResponse(context, 404, "Not found", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, 500, "Internal server error", ex.Message);
        }
    }
    
    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail
        });
    }
}
```

---

## Feature Checklist

### SDK Features to Exercise

- [x] **Client Configuration** - All options exposed via API and UI
- [x] **Connection Management** - Start/Stop/ForceStop via API
- [x] **Session Creation** - Full config support (Model, Streaming, SystemMessage, Tools, Provider)
- [x] **Session Resumption** - Resume existing sessions
- [x] **Session Listing** - List all available sessions
- [x] **Session Deletion** - Delete sessions
- [x] **Message Sending** - With prompt, mode, and attachments
- [x] **Event Streaming** - Real-time events via SignalR
- [x] **Streaming Responses** - Delta events for progressive rendering
- [x] **Message Abortion** - Cancel in-progress messages
- [x] **Message History** - Retrieve all events from a session
- [x] **Custom Tools** - Define and register custom tools
- [x] **System Message Customization** - Append/Replace modes
- [x] **BYOK Provider** - Custom API provider configuration
- [x] **Tool Filtering** - Available/Excluded tools
- [x] **File Attachments** - Attach files to messages
- [x] **Health Check** - Ping endpoint
- [x] **Error Handling** - Proper error responses

---

## Next Steps

See `.github/copilot-instructions.md` for detailed task breakdowns for each phase.

### Phase Summary

1. **Phase 1: Backend Foundation** - Project structure, domain models, CopilotClientManager, client endpoints
2. **Phase 2: Session Management** - SessionManager, SessionService, SessionsController
3. **Phase 3: Messaging and Events** - Message sending, SignalR hub, event streaming
4. **Phase 4: Custom Tools Service** - ToolExecutionService, AIFunction integration
5. **Phase 5: Frontend Core Setup** - React structure, API client, SignalR hook, layout
6. **Phase 6: Frontend Views - Dashboard and Client Config** - Dashboard, configuration views
7. **Phase 7: Frontend Views - Session Management** - Session list, create session modal
8. **Phase 8: Frontend Views - Session Chat** - Chat view with real-time streaming
9. **Phase 9: Advanced Features and Polish** - File attachments, event log, error handling
10. **Phase 10: Integration Testing and Documentation** - E2E tests, README, final review

---

## Implementation Progress

Track completion status here. Update after each phase is complete.

- [x] Phase 1: Backend Foundation - Completed 2026-01-18
- [x] Phase 2: Session Management - Completed 2026-01-18
- [x] Phase 3: Messaging and Events - Completed 2026-01-18
- [x] Phase 4: Custom Tools Service - Completed 2026-01-18
- [x] Phase 5: Frontend Core Setup - Completed 2026-01-18
- [x] Phase 6: Frontend Views - Dashboard and Client Config - Completed 2026-01-18
- [x] Phase 7: Frontend Views - Session Management - Completed 2026-01-18
- [x] Phase 8: Frontend Views - Session Chat - Completed 2026-01-18
- [x] Phase 9: Advanced Features and Polish - Completed 2026-01-18
- [x] Phase 10: Integration Testing and Documentation - Completed 2026-01-18

### Phase Completion Log

| Phase | Status | Completed | Notes |
|-------|--------|-----------|-------|
| 1 | Complete | 2026-01-18 | Created folder structure, domain models, CopilotClientManager, service layer, controller, middleware, and 22 unit tests |
| 2 | Complete | 2026-01-18 | Created session domain models (SessionConfig, SystemMessageConfig, ProviderConfig, ToolDefinition, ToolParameter, SessionInfo, SessionMetadata), request/response models, SessionManager, extended CopilotClientManager with session methods, ISessionService, SessionService, SessionsController, and 44 new unit tests (66 total passing) |
| 3 | Complete | 2026-01-18 | Created message-related models (SessionEvent DTOs, MessageAttachment, SendMessageRequest, SendMessageResponse, MessagesResponse), added SignalR, created SessionHub, SessionEventDispatcher, extended SessionManager with event handlers, extended ISessionService and SessionService with messaging methods (SendMessage, GetMessages, Abort), added messaging endpoints to controller, and 32 new unit tests (98 total passing) |
| 4 | Complete | 2026-01-18 | Added Microsoft.Extensions.AI package, created IToolExecutionService and ToolExecutionService for managing custom tools, updated SessionService to build AIFunctions from tool definitions, created DemoTools (echo_tool, get_current_time) with proper result types (EchoToolResult, GetCurrentTimeResult), and 47 new unit/integration tests (145 total passing) |
| 5 | Complete | 2026-01-18 | Installed npm packages (@microsoft/signalr, axios, react-router-dom v6.30.0), created TypeScript types (client, session, message), API client with all REST endpoints, useSessionHub SignalR hook, context providers (CopilotClientContext, SessionContext), layout components (Header, Sidebar, StatusBar, MainLayout), placeholder views (Dashboard, ClientConfig, Sessions), updated App.tsx with routing and providers, configured proxy, and 19 frontend component tests passing |
| 6 | Complete | 2026-01-18 | Implemented DashboardView with connection status card, quick actions (Start/Stop/Force Stop), ping functionality with latency display, recent sessions list (max 5), configuration summary; implemented ClientConfigView with connection settings form (cliPath, cliUrl, port, useStdio), behavior settings (logLevel, autoStart, autoRestart), process settings (cwd, cliArgs, environment variables via EnvironmentVariableEditor); created ConnectionStatusIndicator component; created EnvironmentVariableEditor component with add/update/remove functionality; wired up API calls through context providers; added comprehensive styling with dark theme; and 88 new frontend component tests (107 frontend + 145 backend = 252 total tests passing) |
| 7 | Complete | 2026-01-18 | Created SessionsList component with compact (sidebar) and full (table) modes displaying sessions with status indicators, relative timestamps, and action buttons (resume/delete/refresh); created CreateSessionModal with 4-tab interface (Basic settings, System Message, Tools, Provider); created supporting components (ModelSelector with available models list and descriptions, SystemMessageEditor with Append/Replace modes and content textarea, ToolDefinitionEditor for defining custom tools with parameters, ProviderConfigEditor for BYOK provider configuration with warning); integrated SessionsList into Sidebar with compact mode; integrated CreateSessionModal into both Sidebar and SessionsView; updated SessionsView with placeholder for session detail (ready for Phase 8); wired up all API calls through SessionContext; and 113 new frontend component tests (220 frontend + 145 backend = 365 total tests passing) |
| 8 | Complete | 2026-01-18 | Created 8 chat message components: StreamingIndicator (animated dots), ReasoningCollapsible (expandable O1 reasoning panel), ToolExecutionCard (tool execution display with status/args/results), UserMessage (user bubbles with attachments), AssistantMessage (assistant responses with code formatting and streaming cursor), ChatHistory (message container with auto-scroll and event processing), AttachmentsPanel (file attachment management), MessageInput (text input with mode selector and send/abort); created SessionChatView with session header, useStreamingAccumulator hook for real-time event processing, and full integration with SessionContext; updated SessionsView to render chat view when session selected; all components styled with dark theme and comprehensive tests (143 new frontend tests, 363 frontend + 145 backend = 508 total tests passing) |
| 9 | Complete | 2026-01-18 | Created EventLogPanel with real-time event display, type filtering, search, auto-scroll, and collapsible details; created ErrorBoundary component for catching JavaScript errors with retry functionality; created Toast notification system (ToastProvider, useToast hook) with info/success/warning/error types and auto-dismiss; created Loading components (Spinner, Skeleton, LoadingOverlay, CardSkeleton, TableSkeleton, ChatMessageSkeleton); added responsive design with mobile sidebar toggle, hamburger menu, and overlay; added extensive ARIA attributes for accessibility; updated DashboardView, SessionsList, and SessionChatView to use new loading and error components; created useErrorToast hook for context error integration; added 59 new frontend tests (422 frontend + 145 backend = 567 total tests passing) |
| 10 | Complete | 2026-01-18 | Created 35 integration tests with proper mocking (controller→service coordination tests for client config, session CRUD, messaging, tool execution); updated README.md with comprehensive documentation (features, prerequisites, architecture, API reference, test coverage); verified start scripts work (backend and frontend both build successfully); total test count: 180 backend + 422 frontend = 602 total tests passing |
