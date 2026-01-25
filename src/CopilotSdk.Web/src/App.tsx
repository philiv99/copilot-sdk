/**
 * Main application component.
 */
import React from 'react';
import { BrowserRouter, Routes, Route, useParams } from 'react-router-dom';
import { CopilotClientProvider, SessionProvider } from './context';
import { MainLayout, ErrorBoundary, ToastProvider } from './components';
import './App.css';

/**
 * Wrapper component that extracts sessionId from URL and passes to MainLayout.
 */
function MainLayoutWithParams() {
  const { sessionId } = useParams<{ sessionId?: string }>();
  return <MainLayout title="Copilot SDK" initialSessionId={sessionId} />;
}

/**
 * Main App component with routing and providers.
 */
function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <ToastProvider maxToasts={5}>
          <CopilotClientProvider autoRefresh={true} refreshInterval={5000}>
            <SessionProvider autoConnectHub={true}>
              <Routes>
                <Route path="/" element={<MainLayoutWithParams />} />
                <Route path="/sessions" element={<MainLayoutWithParams />} />
                <Route path="/sessions/:sessionId" element={<MainLayoutWithParams />} />
              </Routes>
            </SessionProvider>
          </CopilotClientProvider>
        </ToastProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
