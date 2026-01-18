# Copilot SDK

A proof of concept application demonstrating a full-stack solution with a React frontend and .NET Web API backend.

## Solution Structure

This .NET solution contains two projects:

- **Frontend** - A React.js single-page application
- **Backend** - A .NET Web API providing RESTful services

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 or later)
- [Node.js](https://nodejs.org/) (18.x or later)
- npm or yarn package manager

## Getting Started

### Backend

1. Navigate to the backend project directory
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Run the API:
   ```bash
   dotnet run
   ```

### Frontend

1. Navigate to the frontend project directory
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the development server:
   ```bash
   npm start
   ```

## Development

- The backend API runs on `https://localhost:5001` (or `http://localhost:5000`)
- The React frontend runs on `http://localhost:3000`

## License

This project is for demonstration purposes.
