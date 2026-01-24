# Copilot SDK Demo Application

A full-stack proof of concept application demonstrating the [GitHub Copilot SDK (.NET)](https://github.com/github/github-copilot-sdk). This application provides a web-based interface for interacting with GitHub Copilot programmatically.

## Purpose

This demo showcases how to integrate the GitHub Copilot SDK into a web application, enabling developers to:

- **Manage Copilot connections** — Start, stop, and monitor Copilot client lifecycle
- **Create chat sessions** — Configure sessions with custom models, system prompts, and tool definitions
- **Send and receive messages** — Real-time streaming responses with progressive text rendering
- **Execute custom tools** — Register and run custom tool functions that Copilot can invoke
- **Attach files** — Provide file context with messages
- **Use your own API keys** — BYOK (Bring Your Own Key) support for custom providers
- **Refine system prompts** — AI-powered prompt refinement to improve and expand system message content

## How It Works

The application consists of a .NET Web API backend that wraps the Copilot SDK, and a React frontend for user interaction.

1. **Connect** — The backend establishes a connection to GitHub Copilot via the CLI
2. **Create Session** — Users configure a session with their preferred model and settings
3. **Chat** — Messages are sent through the API; responses stream back in real-time via SignalR
4. **Tools** — When Copilot invokes a registered tool, the backend executes it and returns results

All SDK features are exposed through REST endpoints and real-time SignalR events, making it easy to understand how each capability works.

## Features

### System Message Refinement

The application includes an AI-powered prompt refinement feature that helps users improve their system message content. When creating a new session, users can click the "Refine" button next to the system message content textarea to:

- **Expand** brief instructions into comprehensive requirements
- **Clarify** ambiguous requirements
- **Add** specific technical constraints and considerations
- **Structure** content logically with clear sections

**Keyboard Shortcut:** Press `Ctrl+Shift+R` to trigger refinement.

**API Endpoint:** `POST /api/copilot/refine-prompt`

```json
// Request
{
  "content": "Build a task management app",
  "context": "Web application for small teams",
  "refinementFocus": "detail"
}

// Response
{
  "refinedContent": "You are an AI assistant helping to build...",
  "originalContent": "Build a task management app",
  "iterationCount": 1,
  "success": true,
  "errorMessage": null
}
```

**Refinement Focus Options:**
- `clarity` — Make requirements crystal clear and unambiguous
- `detail` — Add comprehensive details and specific examples
- `constraints` — Define technical constraints, boundaries, and limitations
- `all` — Focus on all aspects equally

**Meta-Prompt Template:**

The refinement service uses the following template to instruct the LLM:

```
You are a prompt engineering expert. Your task is to improve and expand the following 
system message content that will be used to instruct an AI assistant for building an application.

ORIGINAL CONTENT:
---
{user_content}
---

Please refine this content by:
1. Clarifying any ambiguous requirements
2. Adding specific, actionable instructions
3. Including relevant technical constraints or considerations
4. Structuring the content logically with clear sections
5. Adding helpful context about expected behaviors
6. Ensuring the tone is appropriate for an AI system prompt

Respond ONLY with the improved system message content.
```

## License

This project is provided as a proof of concept for demonstrating GitHub Copilot SDK capabilities.
