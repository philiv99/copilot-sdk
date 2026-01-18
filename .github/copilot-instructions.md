# Copilot Instructions

## Project Overview

This is a proof of concept full-stack application with a .NET solution containing two projects:
- A React.js frontend
- A .NET Web API backend

## Architecture

- **Frontend**: React.js SPA communicating with the backend via REST APIs
- **Backend**: .NET Web API exposing RESTful endpoints

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

## Testing

- Write unit tests for backend services and controllers
- Write component tests for React components
- Aim for meaningful test coverage on business logic

## Security Considerations

- Validate all user inputs on both frontend and backend
- Use HTTPS in production
- Implement proper CORS configuration
- Never expose sensitive data in API responses
