/**
 * Main application component.
 */
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { CopilotClientProvider, SessionProvider } from './context';
import { MainLayout, ErrorBoundary, ToastProvider } from './components';
import { ClientConfigView, SessionsView } from './views';
import './App.css';

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
              <MainLayout title="Copilot SDK">
                <Routes>
                  <Route path="/" element={<Navigate to="/sessions" replace />} />
                  <Route path="/config" element={<ClientConfigView />} />
                  <Route path="/sessions" element={<SessionsView />} />
                  <Route path="/sessions/:sessionId" element={<SessionsView />} />
                </Routes>
              </MainLayout>
            </SessionProvider>
          </CopilotClientProvider>
        </ToastProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
