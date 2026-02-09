# Copilot SDK Demo Application — Technical Overview

## 1. Introduction

This document provides a technical overview of the Copilot SDK Demo Application, a full-stack proof of concept that wraps the [GitHub Copilot SDK (.NET)](https://github.com/github/github-copilot-sdk) behind a REST API and exposes it through a React single-page application. The application demonstrates programmatic interaction with GitHub Copilot, including client lifecycle management, session orchestration, real-time streaming, custom tool execution, file attachments, BYOK provider support, and AI-powered prompt refinement.

The codebase is structured as a .NET 9 solution with two projects and a shared test project:

| Component | Technology | Location |
|-----------|-----------|----------|
| Backend API | ASP.NET Core 9, SignalR, Microsoft.Extensions.AI | `src/CopilotSdk.Api/` |
| Frontend SPA | React 19, TypeScript, SignalR client, Axios | `src/CopilotSdk.Web/` |
| Backend Tests | xUnit 2.9, Moq 4.20 | `tests/CopilotSdk.Api.Tests/` |
| Data Persistence | JSON files on disk | `src/data/` |

The backend runs on `http://localhost:5139`; the React dev server on `http://localhost:3000`.

---

## 2. High-Level Architecture

```
┌──────────────────────────────────┐
│        React SPA (port 3000)     │
│  ┌───────┐  ┌───────┐  ┌──────┐ │
│  │Context│  │ Views │  │Hooks │ │
│  │Provs  │  │+Comps │  │      │ │
│  └───┬───┘  └───┬───┘  └──┬───┘ │
│      │          │          │     │
│  ┌───▼──────────▼──────────▼───┐ │
│  │   copilotApi.ts (Axios)     │ │
│  │   useSessionHub (SignalR)   │ │
│  └──────────┬──────────┬──────┘ │
└─────────────┼──────────┼────────┘
      REST    │   WebSocket│
              ▼          ▼
┌──────────────────────────────────┐
│   ASP.NET Core API (port 5139)   │
│                                  │
│  ┌──────────────────────────┐    │
│  │      Controllers         │    │
│  │  Client │ Sessions │ ... │    │
│  └────────┬─────────────────┘    │
│           ▼                      │
│  ┌──────────────────────────┐    │
│  │       Services            │    │
│  │ CopilotClientService     │    │
│  │ SessionService            │    │
│  │ ToolExecutionService      │    │
│  │ PromptRefinementService   │    │
│  │ ModelsService             │    │
│  │ PersistenceService        │    │
│  └────────┬─────────────────┘    │
│           ▼                      │
│  ┌──────────────────────────┐    │
│  │       Managers            │    │
│  │ CopilotClientManager     │    │
│  │ SessionManager            │    │
│  └────────┬─────────────────┘    │
│           ▼                      │
│  ┌──────────────────────────┐    │
│  │  GitHub Copilot SDK       │    │
│  │  (CopilotClient,         │    │
│  │   CopilotSession)        │    │
│  └──────────────────────────┘    │
│                                  │
│  ┌──────────────────────────┐    │
│  │ SessionEventDispatcher    │────┼──► SignalR Hub ──► Browser
│  └──────────────────────────┘    │
│                                  │
│  ┌──────────────────────────┐    │
│  │ PersistenceService        │────┼──► src/data/*.json
│  └──────────────────────────┘    │
└──────────────────────────────────┘
```

### Request Flow

1. **REST calls** — The React app issues HTTP requests via `copilotApi.ts` (Axios) to controller endpoints prefixed `/api/copilot/`.
2. **Controller → Service → Manager** — Controllers delegate to scoped services, which coordinate singletons (`CopilotClientManager`, `SessionManager`) and the underlying SDK.
3. **Real-time events** — The SDK fires session events (messages, deltas, tool executions) which are captured by `SessionEventDispatcher`, mapped to DTOs, and pushed via SignalR to any browser clients that have joined the relevant session group.
4. **Persistence** — Session metadata and message history are written to JSON files under `src/data/sessions/`. Client configuration is persisted to `src/data/client-config.json`.

---

## 3. Backend Architecture

### 3.1 Solution & Project Structure

```
CopilotSdk.Api/
├── Controllers/             5 API controllers
│   ├── CopilotClientController    Client lifecycle endpoints
│   ├── SessionsController         Session CRUD + messaging
│   ├── ModelsController           Available AI models
│   ├── PromptRefinementController Prompt refinement via LLM
│   └── SystemPromptTemplatesController  Template management
├── Services/                Service layer (scoped + singletons)
│   ├── CopilotClientService       Wraps CopilotClientManager
│   ├── SessionService             Orchestrates session operations
│   ├── ToolExecutionService       Custom tool registration/execution
│   ├── PromptRefinementService    Ephemeral-session-based prompt refinement
│   ├── ModelsService              Cached model list retrieval
│   ├── PersistenceService         JSON file I/O for sessions & config
│   ├── DevServerService           Vite dev server process management
│   ├── SystemPromptTemplateService Template file discovery
│   └── CopilotClientHostedService IHostedService for auto-start/stop
├── Managers/                Singletons managing SDK object lifecycles
│   ├── CopilotClientManager       CopilotClient instance holder
│   ├── ICopilotClientManager      Minimal interface for hosted service
│   └── SessionManager             Active sessions + persistence bridge
├── EventHandlers/
│   └── SessionEventDispatcher     SDK events → SignalR DTOs
├── Hubs/
│   └── SessionHub                 SignalR hub for session events
├── Middleware/
│   ├── ErrorHandlingMiddleware    Global exception → ProblemDetails
│   └── RateLimitingMiddleware     IP-based rate limiting (/refine-prompt)
├── Models/
│   ├── Domain/                    Internal domain models
│   ├── Requests/                  API request DTOs
│   └── Responses/                 API response DTOs
└── Tools/
    ├── DemoTools.cs               echo_tool, get_current_time definitions
    └── ToolResults.cs             Result record types
```

### 3.2 Dependency Injection & Lifetimes

Registrations happen in `Program.cs`:

| Registration | Lifetime | Purpose |
|---|---|---|
| `CopilotClientManager` | Singleton | Owns the SDK `CopilotClient` instance |
| `ICopilotClientManager` | Singleton (→ same instance) | Narrow interface for `CopilotClientHostedService` |
| `SessionManager` | Singleton | Tracks active `CopilotSession` objects + persistence |
| `SessionEventDispatcher` | Singleton | Receives SDK events, forwards via SignalR |
| `IToolExecutionService` | Singleton | Tool registry with `ConcurrentDictionary` |
| `IPersistenceService` | Singleton | File-based JSON persistence |
| `IDevServerService` | Singleton | Manages Vite child processes |
| `ICopilotClientService` | Scoped | Per-request thin wrapper over `CopilotClientManager` |
| `ISessionService` | Scoped | Per-request orchestration for session ops |
| `IPromptRefinementService` | Scoped | Ephemeral session for prompt refinement |
| `IModelsService` | Scoped | Model list retrieval with `IMemoryCache` |
| `ISystemPromptTemplateService` | Scoped | File-system template discovery |
| `CopilotClientHostedService` | HostedService | Auto-start on app launch, graceful shutdown |

### 3.3 CopilotClientManager (Singleton)

The heart of the backend — manages the single `CopilotClient` SDK instance.

**Key responsibilities:**
- Builds `CopilotClientOptions` from `CopilotClientConfig` domain model.
- Provides `StartAsync`, `StopAsync`, `ForceStopAsync`, `PingAsync`.
- Exposes `CreateSessionAsync`, `ResumeSessionAsync`, `ListSessionsAsync`, `DeleteSessionAsync` — all of which delegate to the SDK client.
- Thread-safe via `lock(_lock)` around client access.
- Persists config changes through `IPersistenceService`.

Configuration is initially loaded from `appsettings.json` (`CopilotClient` section) and overridden by `src/data/client-config.json` if it exists.

### 3.4 SessionManager (Singleton)

Tracks active SDK `CopilotSession` objects in a `ConcurrentDictionary` and manages their metadata on disk.

**Key responsibilities:**
- `RegisterSessionAsync` — stores the live session object + persists metadata JSON.
- `SetupEventHandler` — subscribes the session to `SessionEventDispatcher` via `session.On(handler)`.
- `GetMetadataAsync` / `GetAllMetadataAsync` — reads from persistence (no in-memory cache).
- `IncrementMessageCountAsync`, `UpdateSummaryAsync` — read-modify-write patterns against persisted JSON.
- `RemoveSessionAsync` — disposes event subscription, removes from active dictionary and disk.

### 3.5 SessionService (Scoped)

The primary orchestration layer for session operations. Called by `SessionsController`.

- **CreateSession** — builds `SessionConfig`, optionally builds `AIFunction` collection via `ToolExecutionService`, delegates to `CopilotClientManager.CreateSessionAsync`, registers in `SessionManager`.
- **SendMessage** — retrieves the active `CopilotSession` from `SessionManager`, converts attachments to SDK format, calls `session.SendAsync()`.
- **ListSessions** — reads all metadata from persistence, annotates with active/inactive status.
- **GetPersistedHistoryAsync** — loads full message history from disk for a given session.

### 3.6 SessionEventDispatcher

The bridge between SDK events and the browser:

1. **SDK fires event** → `SessionEventHandler` delegate (created by `CreateHandler(sessionId)`) receives it.
2. Handler is fire-and-forget (`_ = DispatchEventAsync(...)`) to avoid blocking SDK processing.
3. `DispatchEventAsync` determines if it's a delta event (streaming) or a regular event.
4. Maps SDK event types to DTOs using pattern matching (`SessionEvent switch { ... }`).
5. Sends via SignalR: `SendSessionEventAsync` for full events, `SendStreamingDeltaAsync` for deltas.
6. Persists significant events (assistant messages, tool executions, user messages) to `SessionManager` for history.

**Supported event types:** `SessionStart`, `SessionIdle`, `SessionError`, `UserMessage`, `AssistantMessage`, `AssistantMessageDelta`, `AssistantReasoning`, `AssistantReasoningDelta`, `AssistantTurnStart`, `AssistantTurnEnd`, `AssistantUsage`, `ToolExecutionStart`, `ToolExecutionComplete`, `Abort`.

### 3.7 ToolExecutionService (Singleton)

Manages a registry of custom tools using `ConcurrentDictionary<string, RegisteredTool>`.

- **RegisterTool** — stores a `ToolDefinition` + `Func<IDictionary<string, object?>, CancellationToken, Task<object?>>` handler.
- **BuildAIFunctions** — converts `ToolDefinition` objects into `AIFunction` instances that the SDK can invoke. Uses `AIFunctionFactory.Create` internally.
- Two demo tools are provided: `echo_tool` (echoes input) and `get_current_time` (returns formatted timestamp).

### 3.8 PromptRefinementService

Uses an ephemeral `CopilotSession` to refine system message content via LLM:
1. Creates a short-lived session (`refinement-{guid}`) with `claude-opus-4.5`.
2. Sends a meta-prompt built from a template, the user's original content, optional context, and refinement focus (clarity/detail/constraints/all).
3. Subscribes to session events, accumulates the response, and returns the refined text.
4. Deletes the ephemeral session on completion.

Rate-limited to 10 requests per 60 seconds per IP via `RateLimitingMiddleware`.

### 3.9 PersistenceService

File-based persistence using JSON serialization:
- **Data directory**: `src/data/` (configurable via `Persistence:DataDirectory` in `appsettings.json`).
- **Client config**: `src/data/client-config.json`.
- **Sessions**: `src/data/sessions/{session-id}.json` — each file contains full `PersistedSessionData` (config, metadata, messages).
- Uses `SemaphoreSlim` for write serialization.
- File names are sanitized to remove invalid characters.

### 3.10 Middleware

| Middleware | Purpose |
|---|---|
| `ErrorHandlingMiddleware` | Catches unhandled exceptions, maps to `ProblemDetails` JSON with appropriate HTTP status codes (400 for `InvalidOperationException`/`ArgumentException`, 404 for `KeyNotFoundException`, 408 for `OperationCanceledException`, 500 for everything else). |
| `RateLimitingMiddleware` | IP-based sliding window limiter (10 req/60s) applied only to `/api/copilot/refine-prompt`. Uses `ConcurrentDictionary<string, RateLimitInfo>` for tracking. |

### 3.11 REST API Surface

| Method | Route | Controller | Description |
|--------|-------|-----------|-------------|
| GET | `/api/copilot/client/status` | CopilotClient | Client connection status |
| GET | `/api/copilot/client/config` | CopilotClient | Current client configuration |
| PUT | `/api/copilot/client/config` | CopilotClient | Update client configuration |
| POST | `/api/copilot/client/start` | CopilotClient | Start the Copilot client |
| POST | `/api/copilot/client/stop` | CopilotClient | Graceful stop |
| POST | `/api/copilot/client/force-stop` | CopilotClient | Immediate stop |
| POST | `/api/copilot/client/ping` | CopilotClient | Health check with latency |
| GET | `/api/copilot/sessions` | Sessions | List all sessions |
| POST | `/api/copilot/sessions` | Sessions | Create a session |
| GET | `/api/copilot/sessions/{id}` | Sessions | Get session info |
| DELETE | `/api/copilot/sessions/{id}` | Sessions | Delete a session |
| POST | `/api/copilot/sessions/{id}/resume` | Sessions | Resume a session |
| POST | `/api/copilot/sessions/{id}/messages` | Sessions | Send a message |
| GET | `/api/copilot/sessions/{id}/messages` | Sessions | Get live messages/events |
| GET | `/api/copilot/sessions/{id}/history` | Sessions | Get persisted message history |
| POST | `/api/copilot/sessions/{id}/abort` | Sessions | Abort processing |
| GET | `/api/copilot/models` | Models | List available AI models |
| POST | `/api/copilot/models/refresh` | Models | Force-refresh model cache |
| POST | `/api/copilot/refine-prompt` | PromptRefinement | AI-powered prompt refinement |
| GET | `/api/copilot/system-prompt-templates` | SystemPromptTemplates | List prompt templates |
| GET | `/api/copilot/system-prompt-templates/{name}` | SystemPromptTemplates | Get template content |

### 3.12 SignalR Hub

**Endpoint:** `/hubs/session`

**Client → Server methods:**
- `JoinSession(sessionId)` — subscribe to a session's event group.
- `LeaveSession(sessionId)` — unsubscribe from a session's event group.

**Server → Client methods:**
- `OnSessionEvent(sessionId, eventDto)` — full event notification.
- `OnStreamingDelta(deltaDto)` — streaming text chunk.
- `JoinedSession(sessionId)` / `LeftSession(sessionId)` — confirmation callbacks.

Connection uses automatic reconnection with exponential backoff (1s, 2s, 4s, ... up to 32s).

---

## 4. Frontend Architecture

### 4.1 Technology Stack

- **React 19** with functional components and hooks.
- **TypeScript 4.9** with strict typing.
- **react-router-dom 6.30** for client-side routing.
- **Axios** for REST API calls.
- **@microsoft/signalr 10** for real-time event streaming.
- **Create React App** (react-scripts 5) for build toolchain.

### 4.2 Directory Structure

```
src/
├── api/
│   └── copilotApi.ts          Centralized API client (all REST calls)
├── types/
│   ├── client.types.ts        Client config/status types
│   ├── session.types.ts       Session config/info types
│   ├── message.types.ts       Events, messages, streaming types
│   └── refinement.types.ts    Prompt refinement types
├── context/
│   ├── CopilotClientContext   Client state (status, config, ping)
│   └── SessionContext         Session state (list, active, events, hub)
├── hooks/
│   ├── useSessionHub          SignalR connection management
│   ├── usePromptRefinement    Prompt refinement API hook
│   └── useErrorToast          Error-to-toast bridge
├── views/
│   ├── SessionChatView        Main chat interface
│   ├── ClientConfigView       Client configuration form (now modal)
│   └── SessionsView           Session listing view
└── components/
    ├── Layout/
    │   ├── MainLayout         App shell with header + tab container
    │   ├── Header             Top bar with settings gear
    │   ├── Sidebar            (legacy, superseded by TabContainer)
    │   └── StatusBar          Footer with connection info
    ├── TabContainer/          Tabbed session management
    ├── ClientConfigModal/     Settings modal dialog
    ├── Chat/
    │   ├── ChatHistory        Scrollable message list
    │   ├── UserMessage        User message bubble
    │   ├── AssistantMessage   Assistant message bubble
    │   ├── MessageInput       Text input with send/abort
    │   ├── AttachmentsPanel   File attachment picker
    │   ├── ToolExecutionCard  Tool call display
    │   ├── ReasoningCollapsible  Collapsible reasoning block
    │   └── StreamingIndicator    Typing/loading indicator
    ├── CreateSessionModal     Multi-tab session creation dialog
    ├── SessionsList           Session list with status indicators
    ├── ModelSelector          AI model dropdown
    ├── SystemMessageEditor    System prompt editor with modes
    ├── ToolDefinitionEditor   Custom tool parameter editor
    ├── ProviderConfigEditor   BYOK provider configuration
    ├── RefineButton           AI refinement trigger
    ├── EventLogPanel          Real-time event log viewer
    ├── ConnectionStatusIndicator  Status dot component
    ├── EnvironmentVariableEditor  Key-value editor
    ├── ErrorBoundary          React error boundary
    ├── Toast                  Toast notification system
    └── Loading                Spinners and skeletons
```

### 4.3 State Management

The application uses **React Context + `useReducer`** for global state. There is no external state management library.

**CopilotClientContext** — manages:
- Client status polling (5-second auto-refresh interval).
- Client configuration (get/update).
- Start/stop/ping operations.
- Loading and error states.

**SessionContext** — manages:
- Session list (loaded from API).
- Active session selection.
- Session event accumulation (from SignalR).
- Streaming content accumulation (delta events merged progressively).
- Message sending and abort operations.
- SignalR hub connection lifecycle (via `useSessionHub`).

### 4.4 Real-Time Event Handling

The `useSessionHub` hook manages the SignalR connection:

1. On mount (if `autoConnect`), establishes connection to `/hubs/session`.
2. Registers handlers for `OnSessionEvent` and `OnStreamingDelta`.
3. When a session is selected, calls `joinSession(sessionId)` on the hub.
4. Incoming events flow into `SessionContext` via dispatch callbacks.
5. **Streaming accumulation**: Delta events are accumulated by `messageId` in the component state. When a full `AssistantMessageEvent` arrives, it replaces the accumulated content.

Reconnection uses exponential backoff. The hook tracks `Disconnected | Connecting | Connected | Reconnecting` states.

### 4.5 UI Layout

The application uses a **tab-based layout**:
- A fixed header with app title and settings gear icon.
- The main content area contains a `TabContainer` component.
  - The first (pinned) tab is "Sessions" — shows the session list with create/resume/delete actions.
  - Additional tabs open per active session, showing `SessionChatView`.
- A status bar at the bottom shows connection state.
- Client configuration is presented as a modal dialog (triggered from the header gear icon).

### 4.6 Routing

Routes are simple:
- `/` — redirects to `MainLayout` with session list.
- `/sessions` — session list tab.
- `/sessions/:sessionId` — opens session in a tab.

---

## 5. Data Flow Examples

### 5.1 Creating a Session and Sending a Message

```
Browser                    API                     SDK
  │                         │                       │
  ├─ POST /sessions ────────►                       │
  │  {model, streaming,     │                       │
  │   systemMessage, tools} │                       │
  │                         ├─ createSessionAsync ──►
  │                         │   (builds SessionConfig│
  │                         │    + AIFunctions)      │
  │                         │                       │
  │                         │◄── CopilotSession ────┤
  │                         │                       │
  │                         ├─ registerSession      │
  │                         │   (persist to disk)   │
  │                         │                       │
  │◄── 201 {sessionId} ────┤                       │
  │                         │                       │
  ├─ SignalR: JoinSession ──►                       │
  │                         │                       │
  ├─ POST /sessions/X/messages ─►                   │
  │  {prompt, attachments}  │                       │
  │                         ├─ session.SendAsync ───►
  │                         │                       │
  │◄── 200 {accepted} ─────┤                       │
  │                         │                       │
  │                         │   SDK fires events:   │
  │                         │◄─ UserMessageEvent ───┤
  │                         │◄─ AssistantDelta(s) ──┤
  │                         │◄─ AssistantMessage ───┤
  │                         │◄─ SessionIdleEvent ───┤
  │                         │                       │
  │  Events dispatched via  │                       │
  │◄═══ SignalR push ═══════┤                       │
  │  (progressive rendering)│                       │
```

### 5.2 Tool Execution

```
SDK calls registered tool
  │
  ├─ ToolExecutionStartEvent ──► SignalR ──► Browser (shows spinner)
  │
  ├─ ToolExecutionService.ExecuteToolAsync
  │     (looks up handler in ConcurrentDictionary)
  │     (invokes handler with arguments)
  │     (returns result to SDK)
  │
  ├─ ToolExecutionCompleteEvent ──► SignalR ──► Browser (shows result)
```

---

## 6. Testing

### 6.1 Backend Tests

- **Framework**: xUnit 2.9 + Moq 4.20.
- **Test project**: `tests/CopilotSdk.Api.Tests/`.
- **Coverage**: Controllers, services, managers, event dispatcher, integration scenarios.
- **Pattern**: Tests mock the layer below (e.g., service tests mock managers; controller tests mock services).

| Test File | Scope |
|-----------|-------|
| `CopilotClientControllerTests` | All client lifecycle endpoints |
| `CopilotClientServiceTests` | Service-layer client operations |
| `CopilotClientManagerTests` | Manager-level client state |
| `SessionsControllerTests` | All session endpoints including messaging |
| `SessionServiceTests` | Session orchestration logic |
| `SessionManagerTests` | Session tracking and persistence |
| `SessionEventDispatcherTests` | Event mapping and SignalR dispatch |
| `ToolExecutionServiceTests` | Tool registration, execution, AIFunction building |
| `ToolIntegrationTests` | End-to-end tool lifecycle |
| `ModelsControllerTests` | Models endpoint |
| `ModelsServiceTests` | Model caching and retrieval |
| `PersistenceServiceTests` | JSON file I/O |
| `PromptRefinementControllerTests` | Refinement endpoint validation |
| `PromptRefinementServiceTests` | Refinement service logic |
| `CopilotClientHostedServiceTests` | Auto-start/stop lifecycle |
| `IntegrationTests` | Cross-layer integration scenarios |

### 6.2 Frontend Tests

- **Framework**: Jest + React Testing Library.
- **Pattern**: Component rendering tests, user interaction tests, mock API calls.
- Every component and view has a corresponding `.test.tsx` file.
- Custom hooks have dedicated test files (e.g., `usePromptRefinement.test.ts`).

---

## 7. Configuration

### Backend Configuration

| Setting | Source | Default |
|---------|--------|---------|
| `CopilotClient:AutoStart` | `appsettings.Development.json` | `true` |
| `Persistence:DataDirectory` | `appsettings.json` | `"data"` (relative to src/) |
| Listen URL | `launchSettings.json` | `http://localhost:5139` |
| CORS | `Program.cs` | Allows `http://localhost:3000` |

### Frontend Configuration

| Setting | Source | Default |
|---------|--------|---------|
| API Base URL | `copilotApi.ts` | `http://localhost:5139/api/copilot` |
| SignalR Hub URL | `useSessionHub.ts` | `http://localhost:5139/hubs/session` |
| Status polling interval | `CopilotClientProvider` prop | 5000ms |

---

## Appendix A: Gaps and Technical Debt

### A.1 ~~Hardcoded URLs in Frontend~~ ✅ RESOLVED 2026-02-09

**Problem**: API base URL (`http://localhost:5139`) and SignalR hub URL were hardcoded strings in `copilotApi.ts` and `useSessionHub.ts`.

**Resolution**: Extracted URLs to environment variables `REACT_APP_API_BASE_URL` and `REACT_APP_SIGNALR_URL`, read via `process.env` with localhost fallbacks. Created `.env` (committed with defaults) and `.env.example` (template for custom environments). Changes in `copilotApi.ts` and `useSessionHub.ts`.

---

### A.2 No Authentication or Authorization

**Problem**: All API endpoints are unauthenticated. Any client on the network can control the Copilot client, create sessions, and send messages.

**Impact**: Unsuitable for shared or production environments. BYOK API keys sent to the backend are unprotected in transit (no HTTPS in dev) and at rest (written in plain text to JSON files).

**Recommendation**:
1. Add a simple authentication mechanism — at minimum, a shared secret / bearer token.
2. Enable HTTPS for development (the `https` launch profile exists but is not default).
3. For BYOK credentials, encrypt sensitive fields before persisting them (e.g., via `DataProtection` APIs), or never persist API keys at all (keep them in-memory only).

---

### A.3 PersistenceService Write Contention

**Problem**: `PersistenceService` uses a single `SemaphoreSlim(1, 1)` for all write operations across all sessions and client config. This means a write to session A blocks a concurrent write to session B.

**Impact**: Under concurrent messaging to multiple sessions, writes serialize and become a bottleneck.

**Recommendation**: Use per-session write locks. Replace the single `_writeLock` with a `ConcurrentDictionary<string, SemaphoreSlim>` keyed by session ID (plus a separate one for client config). This eliminates cross-session contention while maintaining per-session consistency. Estimated effort: ~1 hour.

---

### A.4 Inconsistent Config Update Pattern (Fire-and-Forget)

**Problem**: In `CopilotClientManager.UpdateConfig()`, persistence is fire-and-forget (`_ = PersistConfigAsync()`), while `UpdateConfigAsync()` awaits it. The `CopilotClientService.UpdateConfig()` calls the non-async version.

**Impact**: A controller response confirming config update may return before persistence actually succeeds. If the process crashes immediately after, the config change is lost.

**Recommendation**: Change `CopilotClientService.UpdateConfig` to call `UpdateConfigAsync` and propagate the async throughout the controller method. Remove the fire-and-forget `UpdateConfig` or mark it `[Obsolete]`.

---

### A.5 SessionManager Read-Modify-Write Without Optimistic Concurrency

**Problem**: Methods like `IncrementMessageCountAsync` and `UpdateSummaryAsync` perform read-modify-write against JSON files without any concurrency control beyond the global write lock. Two concurrent `IncrementMessageCountAsync` calls could both read count=5 and write count=6.

**Impact**: Message counts and metadata can be inconsistent under concurrent writes.

**Recommendation**: Implement either:
- An `ETag`-style optimistic concurrency check (store a version number in the JSON, reject writes with stale versions), or
- Per-session `SemaphoreSlim` locks (ties into A.3 above).

---

### A.6 No Request Validation Library

**Problem**: Input validation is performed manually in controllers (e.g., checking `string.IsNullOrWhiteSpace(request.Model)`). There is no use of `FluentValidation` or data annotations beyond basic nullable checks.

**Impact**: Validation logic is scattered across controllers, inconsistent in coverage, and harder to test independently.

**Recommendation**: Add `FluentValidation.AspNetCore` and create validator classes for each request DTO. Register validators in DI and let the validation pipeline handle rejections automatically. This centralizes validation rules and makes them unit-testable. Estimated effort: 2-3 hours.

---

### A.7 ~~Models List is Static / Hardcoded Fallback~~ ✅ RESOLVED 2026-02-09

**Problem**: `ModelsService` contained a hardcoded `DefaultModels` list as fallback when the SDK doesn't provide a models endpoint or the client is disconnected. This list went stale as new models were released.

**Resolution**: Moved the models list to an external `models.json` configuration file (`src/CopilotSdk.Api/models.json`) that can be updated without recompiling. Added a `lastUpdated` timestamp to the JSON file and exposed it as `ModelsLastUpdated` on the `ModelsResponse` DTO. The service loads models from the config file at runtime, falling back to a minimal 3-model hardcoded list only if the file is missing or corrupt. The config file ships with 19 models aligned with the current GitHub Copilot model picker (including claude-opus-4.6). Frontend `ModelSelector` fallback also updated to match.

---

### A.8 No Structured Logging / Correlation IDs

**Problem**: Log messages use ad-hoc formatting (e.g., `[REFINE EVENT]`, `[REFINE CONTROLLER]`). There is no correlation ID propagated across requests.

**Impact**: Difficult to trace a single user operation across controller → service → manager → event dispatcher in log output.

**Recommendation**:
1. Use structured logging with named properties consistently (already partially done with `{SessionId}`, `{Model}` etc.).
2. Add a `CorrelationId` middleware that reads/generates an `X-Correlation-ID` header and stores it in `HttpContext.Items`. Include it in all log scopes.
3. Remove the `[REFINE ...]` prefixes in favour of the logger category name which already identifies the class.

---

### A.9 Frontend Error Handling is Inconsistent

**Problem**: Some API calls throw errors that bubble up to context error state, while others are caught and swallowed silently. The `ErrorBoundary` catches render errors, but async errors in event handlers or API calls may not be surfaced to the user.

**Impact**: Users may experience silent failures (e.g., session creation fails but no notification appears).

**Recommendation**: Establish a consistent error-handling pattern:
1. All API calls in context providers should catch errors and dispatch to both the context error state and the toast system (via `useErrorToast`).
2. Add error boundaries at the view level (not just the app root) for more granular recovery.
3. Consider a global `axios` response interceptor for common error handling (401, 500, network errors).

---

### A.10 No API Versioning

**Problem**: All endpoints are unversioned (`/api/copilot/...`).

**Impact**: Breaking changes to the API contract would require all clients to update simultaneously, with no ability to run old and new frontends side by side.

**Recommendation**: Add URL-based API versioning (e.g., `/api/v1/copilot/...`) using `Asp.Versioning.Http` NuGet package. This is a straightforward change to route prefixes. Estimated effort: 1-2 hours.

---

## Appendix B: Architectural Improvement Suggestions

### B.1 Replace JSON File Persistence with SQLite

**Current state**: All persistence is JSON files written to disk under `src/data/`.

**Problem**: File I/O with `SemaphoreSlim` is fragile under concurrency, doesn't support queries, and doesn't handle partial writes atomically.

**Recommendation**: Replace `PersistenceService` with a SQLite-backed implementation using Entity Framework Core (or Dapper). Benefits:
- Atomic writes via transactions.
- Queryable session metadata (filter, sort, paginate).
- Concurrent read/write without manual locking.
- Migration support for schema evolution.

Tables: `ClientConfig`, `Sessions`, `Messages`, `ToolDefinitions`.

Estimated effort: 1-2 days. The `IPersistenceService` interface already provides a clean abstraction for swapping implementations.

---

### B.2 Extract Event Mapping to a Separate Concern

**Current state**: `SessionEventDispatcher` handles both event mapping (SDK type → DTO) and dispatch (SignalR sending).

**Recommendation**: Split into:
- `ISessionEventMapper` — pure mapping logic, easily unit-testable.
- `SessionEventDispatcher` — only responsible for SignalR dispatch and persistence.

This improves testability and follows the Single Responsibility Principle.

---

### B.3 Add OpenAPI/Swagger Documentation

**Current state**: `app.MapOpenApi()` is called in development, but no Swagger UI is configured.

**Recommendation**: Add Swashbuckle or the built-in OpenAPI UI middleware. The controllers already have `[ProducesResponseType]` attributes, so the generated docs would be immediately useful. Add `[SwaggerOperation]` descriptions where XML comments exist. This gives developers a live, interactive API reference.

```csharp
// Add to Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Copilot SDK API"));
}
```

---

### B.4 Add Health Checks Endpoint

**Recommendation**: Add ASP.NET Core health checks (`/health`) that verify:
- Copilot client connection state.
- Persistence directory is writable.
- SignalR hub is operational.

This enables integration with monitoring systems and container orchestrators.

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<CopilotClientHealthCheck>("copilot-client")
    .AddCheck<PersistenceHealthCheck>("persistence");
```

---

### B.5 Improve Frontend Build for Production

**Current state**: The frontend uses Create React App (CRA), which is in maintenance mode.

**Recommendation**: Migrate to Vite for the frontend build tool. Benefits include faster HMR, smaller bundle sizes, and active maintenance. Alternatively, if staying with CRA, configure production builds to be served by the .NET backend (static file middleware) to eliminate the CORS configuration entirely.

If moving to Vite:
1. `npm create vite@latest -- --template react-ts`
2. Migrate existing `src/` files.
3. Update `package.json` scripts.
4. Remove CRA-specific files (`react-scripts`, `setupTests.ts` pattern).

---

### B.6 Add Integration / E2E Tests

**Current state**: Integration tests exist in the backend (`IntegrationTests.cs`) but mock the SDK layer. There are no end-to-end browser tests.

**Recommendation**:
1. Add Playwright or Cypress tests that exercise the full stack (browser → API → SDK mock).
2. Create a `WebApplicationFactory<Program>` based integration test setup for the API that tests the real HTTP pipeline (middleware, serialization, routing) without mocking at the controller level.

---

### B.7 Consider Cancellation Token Propagation

**Problem**: Some async methods accept `CancellationToken` but don't pass it through to all downstream calls. For example, `SessionService.DeleteSessionAsync` passes the token to `_clientManager.DeleteSessionAsync` but `RemoveSessionAsync` uses `default`.

**Recommendation**: Audit all async method chains and ensure `CancellationToken` is forwarded consistently. This enables proper request cancellation when clients disconnect.

---

## Appendix C: Minor Code Quality Items

| Item | Location | Fix |
|------|----------|-----|
| `using` alias clutter | `CopilotClientManager.cs` has 7 using aliases for SDK types | Consider a shared `SdkAliases.cs` or use fully qualified names in the few places they're needed |
| Duplicate model hierarchies | `PersistedModels.cs` mirrors `Domain/` models closely | Use AutoMapper or manual mapping with fewer dedicated persisted types |
| `DevServerService` is Windows-only | Uses `cmd.exe /c npm run dev` | Use `Process` with cross-platform detection or remove in favour of external dev server management |
| `SystemPromptTemplateService` path traversal | Relies on `Path.GetFileName()` sanitization only | Add additional validation (regex whitelist for template names) |
| Console.warn in hooks | `useSessionHub.ts` uses `console.warn` directly | Route through a logging utility or remove in production builds |
| `any` casts in `SessionContext` | `(e.data as any)?.messageId` pattern | Create proper type guard functions |
| Missing `IAsyncDisposable` on `SessionManager` | Active sessions hold SDK objects but no disposal | Implement `IAsyncDisposable` to clean up sessions on shutdown |
| `findLastIndex` polyfill | Custom implementation in `SessionContext.tsx` | Use the native `Array.prototype.findLastIndex()` (available in ES2023, supported by target browsers) |
