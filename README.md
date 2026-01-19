# Copilot SDK Demo Application

A full-stack proof of concept application demonstrating all features of the [GitHub Copilot SDK (.NET)](https://github.com/github/github-copilot-sdk). This application provides a web-based interface for managing Copilot client connections, creating sessions, sending messages, and handling real-time streaming events.

## 🚀 Features

### Core Functionality
- **Client Management**: Start, stop, configure, and monitor the Copilot client
- **Session Management**: Create, list, resume, and delete chat sessions
- **Real-time Messaging**: Send prompts with streaming responses via SignalR
- **Custom Tools**: Register and execute custom tool definitions
- **File Attachments**: Attach files to messages for context
- **BYOK Support**: Bring Your Own Key with custom provider configuration

### SDK Features Demonstrated
- All `CopilotClient` lifecycle methods (StartAsync, StopAsync, ForceStopAsync, PingAsync)
- Session configuration options (Model, Streaming, SystemMessage, Tools, Provider)
- Event subscription and handling (UserMessage, AssistantMessage, ToolExecution, etc.)
- Streaming delta events for progressive text rendering
- Custom tool definitions with AIFunctionFactory integration

## 📋 Prerequisites

Before running this application, you'll need:

- [.NET SDK 9.0](https://dotnet.microsoft.com/download) or later
- [Node.js 18.x](https://nodejs.org/) or later
- [GitHub Copilot CLI](https://github.com/github/copilot-cli) - Must be installed and authenticated
- A GitHub account with Copilot access

### Installing GitHub Copilot CLI

```bash
# Install via npm
npm install -g @github/copilot-cli

# Authenticate
copilot-cli auth
```

## 📁 Solution Structure

```
copilot-sdk/
├── src/
│   ├── CopilotSdk.Api/          # .NET Web API Backend
│   │   ├── Controllers/          # REST API controllers
│   │   ├── Services/             # Business logic services
│   │   ├── Managers/             # SDK wrapper managers
│   │   ├── Hubs/                 # SignalR hub for real-time events
│   │   ├── EventHandlers/        # SDK event dispatchers
│   │   ├── Models/               # Request/Response DTOs
│   │   └── Tools/                # Demo custom tools
│   └── CopilotSdk.Web/          # React Frontend
│       └── src/
│           ├── api/              # API client functions
│           ├── components/       # Reusable UI components
│           ├── views/            # Page-level views
│           ├── hooks/            # Custom React hooks
│           ├── context/          # React context providers
│           └── types/            # TypeScript interfaces
├── tests/
│   └── CopilotSdk.Api.Tests/    # Backend unit tests
└── tools/                        # Start scripts
```

## 🏃 Getting Started

### Quick Start (Recommended)

Use the provided start scripts from the repository root:

```bash
# Windows - Start both backend and frontend
.\tools\start-app.bat

# Or start individually:
.\tools\start-backend.bat
.\tools\start-frontend.bat
```

### Manual Setup

#### Backend (.NET API)

```bash
cd src/CopilotSdk.Api
dotnet restore
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

#### Frontend (React)

```bash
cd src/CopilotSdk.Web
npm install
npm start
```

The frontend will be available at `http://localhost:3000`

## 🔌 API Endpoints

### Client Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/copilot/client/status` | Get current client connection status |
| GET | `/api/copilot/client/config` | Get current client configuration |
| PUT | `/api/copilot/client/config` | Update client configuration |
| POST | `/api/copilot/client/start` | Start the Copilot client |
| POST | `/api/copilot/client/stop` | Gracefully stop the client |
| POST | `/api/copilot/client/force-stop` | Force stop the client |
| POST | `/api/copilot/client/ping` | Health check ping |

### Session Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/copilot/sessions` | List all sessions |
| POST | `/api/copilot/sessions` | Create a new session |
| GET | `/api/copilot/sessions/{id}` | Get session details |
| DELETE | `/api/copilot/sessions/{id}` | Delete a session |
| POST | `/api/copilot/sessions/{id}/resume` | Resume an existing session |

### Messaging

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/copilot/sessions/{id}/messages` | Send a message |
| GET | `/api/copilot/sessions/{id}/messages` | Get session history |
| POST | `/api/copilot/sessions/{id}/abort` | Abort current operation |

### Real-time Events (SignalR)

Connect to `/hubs/session` for real-time event streaming:

```typescript
// SignalR methods
JoinSession(sessionId: string)    // Join a session group
LeaveSession(sessionId: string)   // Leave a session group

// Events received
OnSessionEvent(event: SessionEventDto)       // Session lifecycle events
OnStreamingDelta(delta: StreamingDeltaDto)   // Streaming text deltas
```

## 🏗️ Architecture

### Backend Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Controllers                           │
│  (CopilotClientController, SessionsController)              │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                         Services                             │
│  (ICopilotClientService, ISessionService,                   │
│   IToolExecutionService)                                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                         Managers                             │
│  (CopilotClientManager, SessionManager)                     │
│  - Singleton lifecycle management                            │
│  - SDK instance wrapping                                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│               GitHub.Copilot.SDK                             │
│  (CopilotClient, Session, Events)                           │
└─────────────────────────────────────────────────────────────┘
```

### Frontend Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                          Views                               │
│  (DashboardView, ClientConfigView, SessionChatView)         │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    Context Providers                         │
│  (CopilotClientContext, SessionContext)                     │
└───────────────┬─────────────────────┬───────────────────────┘
                │                     │
┌───────────────▼───────┐   ┌─────────▼───────────────────────┐
│      REST API         │   │        SignalR Hub              │
│   (axios/fetch)       │   │   (useSessionHub hook)          │
└───────────────────────┘   └─────────────────────────────────┘
```

## 🛠️ Configuration

### Client Configuration Options

```json
{
  "cliPath": "copilot-cli",       // Path to Copilot CLI executable
  "port": 0,                       // Port (0 = auto-select)
  "useStdio": true,                // Use stdio communication
  "logLevel": "info",              // Logging level
  "autoStart": true,               // Auto-start on creation
  "autoRestart": true,             // Auto-restart on crash
  "cwd": "/app",                   // Working directory
  "cliArgs": ["--verbose"],        // Additional CLI arguments
  "environment": {}                // Environment variables
}
```

### Session Configuration Options

```json
{
  "sessionId": "my-session",       // Custom session ID (optional)
  "model": "gpt-5",                // Model to use
  "streaming": true,               // Enable streaming responses
  "systemMessage": {
    "mode": "Append",              // "Append" or "Replace"
    "content": "Custom context"
  },
  "availableTools": ["tool1"],     // Whitelist tools
  "excludedTools": ["tool2"],      // Blacklist tools
  "tools": [...],                  // Custom tool definitions
  "provider": {                    // BYOK configuration
    "type": "openai",
    "baseUrl": "https://...",
    "apiKey": "sk-..."
  }
}
```

## 🧪 Testing

### Run All Backend Tests

```bash
cd tests/CopilotSdk.Api.Tests
dotnet test
```

### Run All Frontend Tests

```bash
cd src/CopilotSdk.Web
npm test
```

### Test Categories

- **Unit Tests**: Service and controller logic testing with mocks
- **Integration Tests**: End-to-end flow testing through controller layer
- **Component Tests**: React component rendering and interaction tests

## 📊 Test Coverage

| Category | Tests | Status |
|----------|-------|--------|
| Backend Unit Tests | 145 | ✅ Passing |
| Backend Integration Tests | 35 | ✅ Passing |
| Frontend Component Tests | 422 | ✅ Passing |
| **Total** | **602** | **✅ All Passing** |

## 🔧 Development

### Adding Custom Tools

1. Create a tool definition:
```csharp
var tool = new ToolDefinition
{
    Name = "my_tool",
    Description = "My custom tool",
    Parameters = new List<ToolParameter>
    {
        new() { Name = "input", Type = "string", Required = true }
    }
};
```

2. Register with handler:
```csharp
toolService.RegisterTool(tool, async (args, ct) =>
{
    var input = args["input"]?.ToString();
    return new { result = $"Processed: {input}" };
});
```

### Event Handling

Subscribe to session events via SignalR:

```typescript
connection.on("OnSessionEvent", (event: SessionEventDto) => {
    switch (event.type) {
        case "AssistantMessage":
            // Handle complete message
            break;
        case "ToolExecutionStart":
            // Handle tool execution start
            break;
    }
});

connection.on("OnStreamingDelta", (delta: StreamingDeltaDto) => {
    // Handle streaming text delta
    appendToCurrentMessage(delta.content);
});
```

## 📝 Implementation Phases

This application was built in 10 phases following the plan in [.github/app-plans.md](.github/app-plans.md):

1. ✅ Backend Foundation - Project structure, client management
2. ✅ Session Management - Session CRUD operations
3. ✅ Messaging and Events - SignalR integration, event streaming
4. ✅ Custom Tools Service - Tool registration and execution
5. ✅ Frontend Core Setup - React structure, API client, SignalR hook
6. ✅ Dashboard and Client Config - Management views
7. ✅ Session Management UI - Session list and creation modal
8. ✅ Session Chat View - Real-time chat interface
9. ✅ Advanced Features and Polish - Error handling, loading states, accessibility
10. ✅ Integration Testing and Documentation - E2E tests, README

## 📄 License

This project is provided as a proof of concept for demonstrating GitHub Copilot SDK capabilities.

## 🤝 Contributing

This is a demo application. For contributions to the actual GitHub Copilot SDK, please see the [official repository](https://github.com/github/github-copilot-sdk).

This project is for demonstration purposes.
