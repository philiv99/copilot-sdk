# Copilot Instructions

## Project Overview

This is a proof of concept full-stack application with a .NET solution containing two projects:
- A React.js frontend
- A .NET Web API backend

The application exercises all features of the GitHub Copilot SDK (.NET). See `.github/app-plans.md` for the comprehensive implementation plan.

## Architecture

- **Frontend**: React.js SPA communicating with the backend via REST APIs and SignalR for real-time events
- **Backend**: .NET Web API exposing RESTful endpoints, wrapping the GitHub.Copilot.SDK

## Coding Guidelines

### Backend (.NET)

- Follow C# naming conventions (PascalCase for public members, camelCase for private fields with underscore prefix)
- Use async/await for all I/O operations
- Implement proper error handling with appropriate HTTP status codes
- Use dependency injection for services
- Follow RESTful API design principles
- Add XML documentation comments to public APIs

### Frontend (React)

- Use functional components with hooks
- Follow component-based architecture
- Use meaningful component and variable names
- Handle loading and error states appropriately
- Keep components small and focused on a single responsibility

## API Communication

- Frontend communicates with backend via HTTP REST calls
- Use appropriate HTTP methods (GET, POST, PUT, DELETE)
- Handle API errors gracefully in the frontend
- Use SignalR for real-time session event streaming

## Testing

- Write unit tests for backend services and controllers
- Write component tests for React components
- Aim for meaningful test coverage on business logic
- All tests must pass before a phase is considered complete

## Security Considerations

- Validate all user inputs on both frontend and backend
- Use HTTPS in production
- Implement proper CORS configuration
- Never expose sensitive data in API responses (especially API keys for BYOK)

---

## Implementation Plan Execution

**CRITICAL**: Always follow the phased implementation plan in `.github/app-plans.md`. Work through phases sequentially.

### General Rules

1. **One Phase at a Time**: Complete each phase fully before moving to the next
2. **Todo List Management**: Use the `manage_todo_list` tool to track progress within each phase
3. **Unit Tests Required**: Each phase must include unit tests; phase is not complete until tests pass
4. **All Tests Must Pass**: Run all existing tests after each phase; fix any regressions
5. **Review at Phase End**: Review both `app-plans.md` and this file to ensure accuracy
6. **Update checklist in this file**: **IMPORTANT** always update the checklist in this file to reflect phase completion

### Phase Execution Workflow

For each phase:
1. Read the phase requirements from `app-plans.md`
2. Create a todo list with all tasks for the phase
3. Implement each task, marking progress in the todo list
4. Write unit tests for new functionality
5. Run all tests and ensure they pass
6. Update `app-plans.md` and `copilot-instructions.md` to mark the phase complete and note any deviations
7. Review this instructions file for accuracy

---

## Phase 1: Backend Foundation ✅ COMPLETED 2026-01-18

**Goal**: Set up project structure, domain models, and CopilotClientManager singleton with basic client endpoints.

### Tasks
- [x] 1.1 Create folder structure in `CopilotSdk.Api`:
  - `Controllers/`
  - `Models/Requests/`
  - `Models/Responses/`
  - `Models/Domain/`
  - `Services/`
  - `Managers/`
  - `Middleware/`
- [x] 1.2 Add NuGet package reference to `GitHub.Copilot.SDK`
- [x] 1.3 Create domain models in `Models/Domain/`:
  - `CopilotClientConfig.cs`
  - `ClientStatus.cs`
- [x] 1.4 Create request/response models:
  - `Models/Requests/UpdateClientConfigRequest.cs`
  - `Models/Requests/PingRequest.cs`
  - `Models/Responses/ClientStatusResponse.cs`
  - `Models/Responses/PingResponse.cs`
- [x] 1.5 Create `Managers/CopilotClientManager.cs` singleton:
  - Manages `CopilotClient` lifecycle
  - Implements `IAsyncDisposable`
  - Provides `StartAsync`, `StopAsync`, `ForceStopAsync`, `PingAsync`
  - Exposes `State` and `Config` properties
- [x] 1.6 Create `Services/ICopilotClientService.cs` interface
- [x] 1.7 Create `Services/CopilotClientService.cs` implementation
- [x] 1.8 Create `Controllers/CopilotClientController.cs`:
  - `GET /api/copilot/client/status`
  - `GET /api/copilot/client/config`
  - `PUT /api/copilot/client/config`
  - `POST /api/copilot/client/start`
  - `POST /api/copilot/client/stop`
  - `POST /api/copilot/client/force-stop`
  - `POST /api/copilot/client/ping`
- [x] 1.9 Create `Middleware/ErrorHandlingMiddleware.cs`
- [x] 1.10 Update `Program.cs` with DI registration and middleware
- [x] 1.11 Create test project `CopilotSdk.Api.Tests` if not exists
- [x] 1.12 Write unit tests for `CopilotClientService`
- [x] 1.13 Write unit tests for `CopilotClientController`
- [x] 1.14 Run all tests and verify they pass
- [x] 1.15 Update `app-plans.md` to mark Phase 1 complete

---

## Phase 2: Session Management ✅ COMPLETED 2026-01-18

**Goal**: Implement session creation, listing, deletion, resumption, and the SessionService.

### Tasks
- [x] 2.1 Create session-related domain models in `Models/Domain/`:
  - `SessionConfig.cs` (includes SystemMessageConfig, ProviderConfig, ToolDefinition, ToolParameter)
  - `SessionInfo.cs`
  - `SessionMetadata.cs`
- [x] 2.2 Create request/response models:
  - `Models/Requests/CreateSessionRequest.cs`
  - `Models/Requests/ResumeSessionRequest.cs`
  - `Models/Responses/SessionInfoResponse.cs`
  - `Models/Responses/SessionListResponse.cs`
- [x] 2.3 Create `Managers/SessionManager.cs`:
  - Track active sessions with `ConcurrentDictionary`
  - Store session metadata (config, creation time, last activity)
- [x] 2.4 Extend `CopilotClientManager` with session methods:
  - `CreateSessionAsync`
  - `ResumeSessionAsync`
  - `ListSessionsAsync`
  - `DeleteSessionAsync`
- [x] 2.5 Create `Services/ISessionService.cs` interface
- [x] 2.6 Create `Services/SessionService.cs` implementation
- [x] 2.7 Create `Controllers/SessionsController.cs`:
  - `GET /api/copilot/sessions`
  - `POST /api/copilot/sessions`
  - `GET /api/copilot/sessions/{sessionId}`
  - `DELETE /api/copilot/sessions/{sessionId}`
  - `POST /api/copilot/sessions/{sessionId}/resume`
- [x] 2.8 Update `Program.cs` with new DI registrations
- [x] 2.9 Write unit tests for `SessionManager`
- [x] 2.10 Write unit tests for `SessionService`
- [x] 2.11 Write unit tests for `SessionsController`
- [x] 2.12 Run all tests and verify they pass (66 tests passing)
- [x] 2.13 Update `app-plans.md` to mark Phase 2 complete

---

## Phase 3: Messaging and Events ✅ COMPLETED 2026-01-18

**Goal**: Implement message sending, retrieval, abortion, and real-time event streaming via SignalR.

### Tasks
- [x] 3.1 Create message-related models:
  - `Models/Domain/SessionEvent.cs` (and all event data types)
  - `Models/Domain/MessageAttachment.cs`
  - `Models/Requests/SendMessageRequest.cs`
  - `Models/Responses/SendMessageResponse.cs`
  - `Models/Responses/MessagesResponse.cs`
- [x] 3.2 Add SignalR NuGet package
- [x] 3.3 Create `Hubs/SessionHub.cs`:
  - `JoinSession` method
  - `LeaveSession` method
  - Handle disconnection cleanup
- [x] 3.4 Create `EventHandlers/SessionEventDispatcher.cs`:
  - Map SDK events to DTOs
  - Dispatch events to SignalR groups
  - Handle streaming delta events separately
- [x] 3.5 Update `SessionManager` to set up event handlers on sessions
- [x] 3.6 Extend `ISessionService` with messaging methods:
  - `SendMessageAsync`
  - `GetMessagesAsync`
  - `AbortAsync`
- [x] 3.7 Implement messaging methods in `SessionService`
- [x] 3.8 Add messaging endpoints to `SessionsController`:
  - `POST /api/copilot/sessions/{sessionId}/messages`
  - `GET /api/copilot/sessions/{sessionId}/messages`
  - `POST /api/copilot/sessions/{sessionId}/abort`
- [x] 3.9 Update `Program.cs` to map SignalR hub
- [x] 3.10 Write unit tests for `SessionEventDispatcher`
- [x] 3.11 Write unit tests for messaging methods in `SessionService`
- [x] 3.12 Write unit tests for new `SessionsController` endpoints
- [x] 3.13 Run all tests and verify they pass (98 tests passing)
- [x] 3.14 Update `app-plans.md` to mark Phase 3 complete

---

## Phase 4: Custom Tools Service ✅ COMPLETED 2026-01-18

**Goal**: Implement the tool execution service for registering and executing custom tools.

### Tasks
- [x] 4.1 Add `Microsoft.Extensions.AI` NuGet package
- [x] 4.2 Create `Services/IToolExecutionService.cs` interface:
  - `RegisterTool`
  - `UnregisterTool`
  - `ExecuteToolAsync`
  - `GetRegisteredTools`
  - `BuildAIFunctions` (converts definitions to AIFunction list)
- [x] 4.3 Create `Services/ToolExecutionService.cs` implementation
- [x] 4.4 Update `SessionService.CreateSessionAsync` to:
  - Accept tool definitions from request
  - Use `ToolExecutionService` to build `AIFunction` list
  - Pass tools to SDK `SessionConfig`
- [x] 4.5 Create sample/demo tools for testing:
  - `echo_tool` - echoes input back
  - `get_current_time` - returns current time
  - Created `Tools/DemoTools.cs` with handlers
  - Created `Tools/ToolResults.cs` with result types
- [x] 4.6 Write unit tests for `ToolExecutionService`
- [x] 4.7 Write integration test verifying tools are passed to session
- [x] 4.8 Run all tests and verify they pass (145 tests passing)
- [x] 4.9 Update `app-plans.md` to mark Phase 4 complete

---

## Phase 5: Frontend Core Setup ✅ COMPLETED 2026-01-18

**Goal**: Set up React project structure, API client, SignalR connection, and basic layout.

### Tasks
- [x] 5.1 Install required npm packages:
  - `@microsoft/signalr`
  - `axios` (or use fetch)
  - `react-router-dom` v6.30.0 (downgraded from v7 for Jest compatibility)
- [x] 5.2 Create folder structure in `CopilotSdk.Web/src/`:
  - `api/` - API client functions
  - `hooks/` - Custom React hooks
  - `components/` - Reusable UI components
  - `views/` - Page-level components
  - `types/` - TypeScript interfaces
  - `context/` - React context providers
- [x] 5.3 Create TypeScript types in `types/`:
  - `client.types.ts` - Client config and status types
  - `session.types.ts` - Session-related types
  - `message.types.ts` - Message and event types
- [x] 5.4 Create API client in `api/`:
  - `copilotApi.ts` - Functions for all REST endpoints
- [x] 5.5 Create SignalR hook in `hooks/`:
  - `useSessionHub.ts` - Manages SignalR connection and events
- [x] 5.6 Create context providers in `context/`:
  - `CopilotClientContext.tsx` - Client state management
  - `SessionContext.tsx` - Active session state
- [x] 5.7 Create basic layout components:
  - `components/Layout/Header.tsx`
  - `components/Layout/Sidebar.tsx`
  - `components/Layout/StatusBar.tsx`
  - `components/Layout/MainLayout.tsx`
- [x] 5.8 Update `App.tsx` with layout and routing structure
- [x] 5.9 Configure proxy in `package.json` or setup CORS for dev
- [x] 5.10 Write component tests for layout components
- [x] 5.11 Run all tests (frontend and backend) and verify they pass (19 frontend + 145 backend = 164 total)
- [x] 5.12 Update `app-plans.md` to mark Phase 5 complete

---

## Phase 6: Frontend Views - Dashboard and Client Config ✅ COMPLETED 2026-01-18

**Goal**: Implement Dashboard and Client Configuration views.

### Tasks
- [x] 6.1 Create Dashboard view `views/DashboardView.tsx`:
  - Connection status card
  - Quick actions (Start/Stop Client)
  - Recent sessions list
  - Ping button with latency display
- [x] 6.2 Create Client Configuration view `views/ClientConfigView.tsx`:
  - Connection settings form (CliPath, CliUrl, Port, UseStdio)
  - Behavior settings (AutoStart, AutoRestart, LogLevel)
  - Process settings (Cwd, CliArgs, Environment variables)
  - Save/Start/Stop/Force Stop/Ping action buttons
- [x] 6.3 Create supporting components:
  - `components/ConnectionStatusIndicator.tsx`
  - `components/EnvironmentVariableEditor.tsx`
- [x] 6.4 Wire up API calls for client operations
- [x] 6.5 Add navigation between Dashboard and Client Config
- [x] 6.6 Write component tests for Dashboard view
- [x] 6.7 Write component tests for Client Config view
- [x] 6.8 Run all tests and verify they pass (107 frontend + 145 backend = 252 total)
- [x] 6.9 Update `app-plans.md` to mark Phase 6 complete

---

## Phase 7: Frontend Views - Session Management ✅ COMPLETED 2026-01-18

**Goal**: Implement Create Session modal and Sessions List in sidebar.

### Tasks
- [x] 7.1 Create Sessions List component `components/SessionsList.tsx`:
  - Display all sessions with status indicators
  - Resume/Delete actions per session
  - Refresh button
  - Supports compact (sidebar) and full (table) modes
- [x] 7.2 Create Create Session Modal `components/CreateSessionModal.tsx`:
  - Basic settings (SessionId, Model selector, Streaming toggle)
  - System message configuration (Mode, Content)
  - Tool configuration (Available/Excluded tools)
  - Custom tools definition UI
  - BYOK provider configuration (optional)
  - 4-tab interface for organized configuration
- [x] 7.3 Create supporting components:
  - `components/ModelSelector.tsx` - with available models list and descriptions
  - `components/ToolDefinitionEditor.tsx` - for defining custom tools with parameters
  - `components/SystemMessageEditor.tsx` - with Append/Replace modes and toggle
  - `components/ProviderConfigEditor.tsx` - for BYOK provider configuration
- [x] 7.4 Integrate SessionsList into Sidebar (compact mode)
- [x] 7.5 Wire up Create Session API calls via SessionContext
- [x] 7.6 Handle session creation success (navigate to chat view)
- [x] 7.7 Write component tests for SessionsList
- [x] 7.8 Write component tests for CreateSessionModal and all supporting components
- [x] 7.9 Run all tests and verify they pass (220 frontend + 145 backend = 365 total)
- [x] 7.10 Update `app-plans.md` to mark Phase 7 complete

---

## Phase 8: Frontend Views - Session Chat ✅ COMPLETED 2026-01-18

**Goal**: Implement the Session Chat view with real-time streaming.

### Tasks
- [x] 8.1 Create Session Chat view `views/SessionChatView.tsx`:
  - Session header with info and actions
  - Chat history display
  - Message input with mode selector
  - Send and Abort buttons
- [x] 8.2 Create chat message components:
  - `components/Chat/ChatHistory.tsx`
  - `components/Chat/UserMessage.tsx`
  - `components/Chat/AssistantMessage.tsx`
  - `components/Chat/ToolExecutionCard.tsx`
  - `components/Chat/ReasoningCollapsible.tsx`
  - `components/Chat/StreamingIndicator.tsx`
- [x] 8.3 Create message input components:
  - `components/Chat/MessageInput.tsx`
  - `components/Chat/AttachmentsPanel.tsx`
- [x] 8.4 Integrate SignalR for real-time events:
  - Join session group on view mount
  - Handle `OnSessionEvent` for message updates
  - Handle `OnStreamingDelta` for progressive rendering
  - Leave session group on unmount
- [x] 8.5 Implement streaming message accumulation
- [x] 8.6 Wire up Send/Abort API calls
- [x] 8.7 Write component tests for chat components
- [x] 8.8 Run all tests and verify they pass (363 frontend + 145 backend = 508 total)
- [x] 8.9 Update `app-plans.md` to mark Phase 8 complete

---

## Phase 9: Advanced Features and Polish ✅ COMPLETED 2026-01-18

**Goal**: Implement file attachments, event log viewer, error handling UI, and polish.

### Tasks
- [x] 9.1 Implement file attachment handling:
  - File picker in AttachmentsPanel (already existed from Phase 8)
  - Display attached files
  - Send attachments with messages
- [x] 9.2 Create Event Log Panel `components/EventLogPanel.tsx`:
  - Real-time event display
  - Filter by event type
  - Search functionality
  - Clear log button
  - Collapsible event details with JSON display
- [x] 9.3 Improve error handling:
  - Global error boundary (`ErrorBoundary.tsx`)
  - Toast notifications for errors (`Toast.tsx`, `ToastProvider`, `useToast`)
  - Created `useErrorToast` hook for context error integration
  - Inline error displays in components
- [x] 9.4 Add loading states:
  - Loading spinners for async operations (`Spinner` component with 3 sizes)
  - Skeleton loaders where appropriate (`Skeleton`, `CardSkeleton`, `TableSkeleton`, `ChatMessageSkeleton`)
  - Loading overlay component (`LoadingOverlay`)
  - Integrated into DashboardView and SessionsList
- [x] 9.5 Responsive design improvements:
  - Mobile sidebar toggle (hamburger menu)
  - Responsive breakpoints (768px, 480px)
  - Sidebar overlay on mobile
  - Updated Header, Sidebar, MainLayout for mobile support
- [x] 9.6 Accessibility review and fixes:
  - ARIA labels throughout components
  - Role attributes for interactive elements
  - Screen reader support for loading states
- [x] 9.7 Write any remaining component tests:
  - EventLogPanel.test.tsx (13 tests)
  - ErrorBoundary.test.tsx (7 tests)
  - Toast.test.tsx (17 tests)
  - Loading.test.tsx (22 tests)
- [x] 9.8 Run full test suite and verify all pass (422 frontend + 145 backend = 567 total)
- [x] 9.9 Update `app-plans.md` to mark Phase 9 complete
- [x] 9.10 Final review of all documentation

---

## Phase 10: Integration Testing and Documentation ✅ COMPLETED 2026-01-18

**Goal**: End-to-end testing and final documentation.

### Tasks
- [x] 10.1 Create integration tests that exercise full flow:
  - Client configuration and status tests
  - Session creation with basic config, custom tools, BYOK, system message
  - Session listing, retrieval, and deletion
  - Session resume with configuration
  - Messaging with attachments and abort functionality
  - Demo tool execution (echo_tool, get_current_time)
  - Custom tool registration and execution
  - AIFunction building from tool definitions
  - 35 integration tests with proper mocking (controller→service coordination)
- [x] 10.2 Update README.md with:
  - Project overview and features
  - Prerequisites (GitHub Copilot CLI, .NET 9.0, Node.js 18+)
  - Solution structure diagram
  - How to run (quick start scripts and manual setup)
  - Complete API documentation table
  - SignalR real-time events
  - Architecture diagrams (backend and frontend)
  - Configuration options (client and session)
  - Testing instructions and coverage table
  - Custom tool development guide
  - Event handling examples
  - Implementation phases summary
- [x] 10.3 Verify all start scripts work (`tools/start-app.bat`, `start-backend.bat`, `start-frontend.bat`)
  - Backend builds and runs successfully
  - Frontend builds successfully
- [x] 10.4 Final review of `app-plans.md` - marked Phase 10 complete with notes
- [x] 10.5 Final review of this instructions file - marked Phase 10 complete
- [x] 10.6 Run full test suite one final time (180 backend + 422 frontend = 602 total tests passing)

---

## Progress Tracking

After completing each phase, update the checkbox status in this file and in `app-plans.md`. Use the following format in `app-plans.md` under the "Next Steps" section:

```markdown
## Implementation Progress

- [x] Phase 1: Backend Foundation - Completed YYYY-MM-DD
- [ ] Phase 2: Session Management
- [ ] Phase 3: Messaging and Events
...
```

## Important Reminders

1. **Always read `app-plans.md`** before starting any phase
2. **Use `manage_todo_list`** tool to track tasks within each phase
3. **Run tests frequently** - after each significant change
4. **Commit logical units** - each phase should be one or more clean commits
5. **Update documentation** - keep plans and instructions in sync with reality
6. **Ask for clarification** if requirements are ambiguous
