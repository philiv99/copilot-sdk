/**
 * Main application component.
 */
import React from 'react';
import { BrowserRouter, Routes, Route, useParams } from 'react-router-dom';
import { CopilotClientProvider, SessionProvider, UserProvider } from './context';
import { MainLayout, ErrorBoundary, ToastProvider, ProtectedRoute } from './components';
import { LoginView, RegisterView, ProfileView, AdminUsersView, ForgotCredentialsView } from './views';
import './App.css';

/**
 * Wrapper component that extracts sessionId from URL and passes to MainLayout.
 */
function MainLayoutWithParams() {
  const { sessionId } = useParams<{ sessionId?: string }>();
  return <MainLayout title="App Maker" initialSessionId={sessionId} />;
}

/**
 * Main App component with routing and providers.
 */
function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <ToastProvider maxToasts={5}>
          <UserProvider>
            <CopilotClientProvider autoRefresh={true} refreshInterval={5000}>
              <SessionProvider autoConnectHub={true}>
                <Routes>
                  {/* Public routes */}
                  <Route path="/login" element={<LoginView />} />
                  <Route path="/register" element={<RegisterView />} />
                  <Route path="/forgot-credentials" element={<ForgotCredentialsView />} />

                  {/* Protected routes */}
                  <Route path="/" element={<ProtectedRoute><MainLayoutWithParams /></ProtectedRoute>} />
                  <Route path="/sessions" element={<ProtectedRoute><MainLayoutWithParams /></ProtectedRoute>} />
                  <Route path="/sessions/:sessionId" element={<ProtectedRoute><MainLayoutWithParams /></ProtectedRoute>} />
                  <Route path="/profile" element={<ProtectedRoute><ProfileView /></ProtectedRoute>} />
                  <Route path="/admin/users" element={<ProtectedRoute requiredRole="Admin"><AdminUsersView /></ProtectedRoute>} />
                </Routes>
              </SessionProvider>
            </CopilotClientProvider>
          </UserProvider>
        </ToastProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
