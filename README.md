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

## How It Works

The application consists of a .NET Web API backend that wraps the Copilot SDK, and a React frontend for user interaction.

1. **Connect** — The backend establishes a connection to GitHub Copilot via the CLI
2. **Create Session** — Users configure a session with their preferred model and settings
3. **Chat** — Messages are sent through the API; responses stream back in real-time via SignalR
4. **Tools** — When Copilot invokes a registered tool, the backend executes it and returns results

All SDK features are exposed through REST endpoints and real-time SignalR events, making it easy to understand how each capability works.

## License

This project is provided as a proof of concept for demonstrating GitHub Copilot SDK capabilities.
