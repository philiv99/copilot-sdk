/**
 * Main application component.
 */
import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { CopilotClientProvider, SessionProvider } from './context';
import { MainLayout, ErrorBoundary, ToastProvider } from './components';
import { DashboardView, ClientConfigView, SessionsView } from './views';
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
                  <Route path="/" element={<DashboardView />} />
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
