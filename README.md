# Copilot SDK Demo Application

A full-stack web application demonstrating the [GitHub Copilot SDK (.NET)](https://github.com/github/github-copilot-sdk). Provides a rich UI for managing Copilot connections, creating AI chat sessions, streaming responses in real time, executing custom tools, and more.

**Stack:** .NET 9 Web API &bull; React 19 &bull; SignalR &bull; SQLite

---

## Quick Start

**Prerequisites:** .NET 9.0 SDK &bull; Node.js 18+ &bull; GitHub Copilot CLI

```bash
# Clone and run everything
git clone https://github.com/philiv99/copilot-sdk.git
cd copilot-sdk/tools
.\start-app.bat          # Windows — starts backend (port 5139) + frontend (port 3000)
```

Or start individually:

```bash
# Backend
cd src/CopilotSdk.Api && dotnet run

# Frontend
cd src/CopilotSdk.Web && npm install && npm start
```

Default admin login: `admin` / `admin123`

---

## Features

### User Accounts & Roles

- **Register / Login / Logout** with local accounts
- Three roles: **Admin**, **Creator**, **Player**
  - Admins manage all users and see all sessions
  - Creators create and manage their own sessions
  - Players can view and play sessions with dev servers
- **Profile page** — update display name, change password, pick an avatar (12 preset emojis or custom upload)
- **Admin panel** — list, activate/deactivate, and reset passwords for users

### Copilot Client Management

- **Start / Stop / Force-Stop** the Copilot SDK connection from the UI
- **Ping** with latency display
- **Live connection status** indicator (auto-refreshes every 5 seconds)
- Configure: CLI path, port, stdio mode, log level, auto-start, auto-restart, working directory, environment variables
- **Auto-start on launch** via background hosted service

### Session Management

- **Create sessions** with a 4-tab modal:
  - **Basic** — session ID, model, streaming toggle
  - **System Message** — append/replace mode, free-text editor, template selector, agent team composer
  - **Tools** — available/excluded tool filters, custom tool definitions (name, description, typed parameters)
  - **Provider** — BYOK (Bring Your Own Key) configuration for custom API endpoints
- **List / Resume / Delete** sessions — role-filtered (admins see all, creators see their own)
- Tab-based session navigation — each open session gets its own chat tab

### Chat & Streaming

- **Send messages** with enqueue or immediate mode
- **Real-time streaming** — progressive text rendering via SignalR with a streaming cursor
- **File attachments** — attach files to messages for context
- **Abort** an in-progress response
- **Persisted chat history** — messages survive session resume
- **Reasoning display** — collapsible reasoning blocks for thinking models (o1, o3, etc.)
- **Tool execution cards** — inline display of tool calls and results
- **Code formatting** in assistant messages

### AI Agent Teams

- **12 built-in agents** — orchestrator, systems-analyst, coder, code-reviewer, code-tester, security-reviewer, QA, react-ui-developer, dotnet-api-developer, mapbox-developer, data-ingestor, mysql-persistence-expert
- **5 team presets** — full-dev-team, backend-squad, frontend-squad, data-pipeline, mapping-team
- **Compose system messages** from selected agents — choose a team preset or pick individual agents, add a workflow pattern (sequential/parallel/hub-spoke), and optionally layer a prompt template + custom content

### Prompt Refinement

- **AI-powered** refinement of system message content — click "Refine" or press `Ctrl+Shift+R`
- Focus options: clarity, detail, constraints, or all
- Undo with `Ctrl+Shift+Z`, iteration counter, character-count diff
- Rate-limited (10 requests / 60 seconds)

### System Prompt Templates

- **5 built-in templates**: app-dev, app-dev-with-AI, game-dev, game-dev-with-AI, pipeline-dev
- Select a template when creating a session to pre-populate the system message

### Model Selection

- **19 models** available out of the box — GPT-4o, GPT-4.1, Claude Sonnet/Opus, Gemini, o1, o3-mini, and more
- Dropdown selector with model descriptions

### Custom Tools

- Define custom tools at session creation (name, description, parameters with types)
- Built-in demo tools: `echo_tool`, `get_current_time`
- Tool execution shown inline in chat with start/complete events

### Dev Server Management

- **Start / Stop** a Vite dev server for session-built applications
- View server status (PID, port, URL)
- Auto-opens browser on start

### Event Log

- **Real-time event log panel** — see every SignalR event as it arrives
- Filter by event type, search, clear, expand JSON details

### UI & UX

- **Dark theme** throughout
- **Tab-based layout** — pinned Sessions tab + per-session chat tabs
- **Responsive design** — mobile sidebar toggle, hamburger menu, breakpoints at 768px / 480px
- **Toast notifications** — info / success / warning / error with auto-dismiss
- **Loading states** — spinners, skeleton loaders, overlays
- **Error boundary** with retry
- **Accessibility** — ARIA labels, roles, live regions, focus management

---

## Project Structure

```
src/
  CopilotSdk.Api/        .NET 9 Web API (port 5139)
  CopilotSdk.Web/        React 19 SPA (port 3000)
  data/                  SQLite database + legacy JSON
docs/
  agents/                Agent definitions (JSON + Markdown prompts)
  system_prompts/        System prompt templates
  teams/                 Team preset definitions
tests/
  CopilotSdk.Api.Tests/  xUnit backend tests
  (frontend tests)       Jest + React Testing Library (in CopilotSdk.Web)
tools/
  start-app.bat          Launch both backend and frontend
```

---

## Testing

```bash
# Backend (~398 tests)
cd tests/CopilotSdk.Api.Tests && dotnet test

# Frontend (~593 tests)
cd src/CopilotSdk.Web && npm test -- --watchAll=false
```

---

## API Overview

| Area | Endpoints | Description |
|------|-----------|-------------|
| Client | 7 | Status, config, start/stop, ping |
| Sessions | 12 | CRUD, resume, messages, history, abort, dev-server |
| Models | 2 | List models, refresh cache |
| Users | 16 | Register, login, profile, admin management, avatars |
| Agents & Teams | 5 | List agents/teams, compose system messages |
| Prompt Refinement | 1 | AI-powered prompt improvement |
| System Prompt Templates | 2 | List and retrieve templates |
| **SignalR Hub** | `/hubs/session` | Real-time streaming events |

---

## License

This project is provided as a proof of concept for demonstrating GitHub Copilot SDK capabilities.
