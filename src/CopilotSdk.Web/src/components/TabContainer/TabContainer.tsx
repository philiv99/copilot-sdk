/**
 * TabContainer component for managing sessions with tabs.
 */
import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSession } from '../../context';
import { SessionInfoResponse } from '../../types';
import { SessionsList, CreateSessionModal } from '..';
import { SessionChatView } from '../../views/SessionChatView';
import './TabContainer.css';

/**
 * Tab representing an open session.
 */
interface SessionTab {
  sessionId: string;
  title: string;
}

/**
 * TabContainer component props.
 */
export interface TabContainerProps {
  /** Initial session ID to open (from URL). */
  initialSessionId?: string;
}

/**
 * TabContainer manages a tabbed interface for sessions.
 * - First tab is always the Sessions List
 * - Additional tabs are opened for active sessions
 */
export function TabContainer({ initialSessionId }: TabContainerProps) {
  const navigate = useNavigate();
  const {
    sessions,
    activeSession,
    activeSessionId,
    activeSessionEvents,
    selectSession,
    clearActiveSession,
    createSession,
    isLoading,
    isSending,
    error,
    clearError,
  } = useSession();

  // Track open session tabs
  const [openTabs, setOpenTabs] = useState<SessionTab[]>(() => {
    // Initialize with initial session if provided
    if (initialSessionId) {
      const session = sessions.find(s => s.sessionId === initialSessionId);
      return session ? [{ sessionId: initialSessionId, title: session.sessionId }] : [];
    }
    return [];
  });

  // Currently selected tab: 'sessions-list' or a sessionId
  const [activeTab, setActiveTab] = useState<string>(initialSessionId || 'sessions-list');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Open a session in a new tab or switch to existing tab
  const openSessionTab = useCallback(async (sessionId: string) => {
    // Check if tab already exists
    const existingTab = openTabs.find(tab => tab.sessionId === sessionId);
    
    if (!existingTab) {
      // Add new tab
      const session = sessions.find(s => s.sessionId === sessionId);
      const newTab: SessionTab = {
        sessionId,
        title: sessionId,
      };
      setOpenTabs(prev => [...prev, newTab]);
    }

    // Select the session and switch to its tab
    try {
      await selectSession(sessionId);
      setActiveTab(sessionId);
      navigate(`/sessions/${sessionId}`, { replace: true });
    } catch (err) {
      // Error handled by context
    }
  }, [openTabs, sessions, selectSession, navigate]);

  // Close a session tab
  const closeSessionTab = useCallback((sessionId: string, e?: React.MouseEvent) => {
    if (e) {
      e.stopPropagation();
    }

    setOpenTabs(prev => prev.filter(tab => tab.sessionId !== sessionId));

    // If we're closing the active tab, switch to sessions list
    if (activeTab === sessionId) {
      clearActiveSession();
      setActiveTab('sessions-list');
      navigate('/sessions', { replace: true });
    }
  }, [activeTab, clearActiveSession, navigate]);

  // Switch to sessions list tab
  const switchToSessionsList = useCallback(() => {
    clearActiveSession();
    setActiveTab('sessions-list');
    navigate('/sessions', { replace: true });
  }, [clearActiveSession, navigate]);

  // Handle session click from sessions list
  const handleSessionClick = useCallback((session: SessionInfoResponse) => {
    openSessionTab(session.sessionId);
  }, [openSessionTab]);

  // Handle create session
  const handleCreateClick = useCallback(() => {
    setIsCreateModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsCreateModalOpen(false);
  }, []);

  const handleSessionCreated = useCallback((sessionId: string) => {
    setIsCreateModalOpen(false);
    openSessionTab(sessionId);
  }, [openSessionTab]);

  // Get display title for a session tab
  const getTabTitle = useCallback((sessionId: string) => {
    // Truncate long session IDs
    if (sessionId.length > 20) {
      return sessionId.substring(0, 17) + '...';
    }
    return sessionId;
  }, []);

  return (
    <div className="tab-container" data-testid="tab-container">
      {/* Tab Bar */}
      <div className="tab-bar" role="tablist" aria-label="Session tabs">
        {/* Sessions List Tab */}
        <button
          className={`tab ${activeTab === 'sessions-list' ? 'active' : ''}`}
          onClick={switchToSessionsList}
          role="tab"
          aria-selected={activeTab === 'sessions-list'}
          aria-controls="tab-panel-sessions-list"
          data-testid="tab-sessions-list"
        >
          <span className="tab-icon" aria-hidden="true">📋</span>
          <span className="tab-label">Sessions</span>
        </button>

        {/* Session Tabs */}
        {openTabs.map((tab) => (
          <button
            key={tab.sessionId}
            className={`tab session-tab ${activeTab === tab.sessionId ? 'active' : ''}`}
            onClick={() => openSessionTab(tab.sessionId)}
            role="tab"
            aria-selected={activeTab === tab.sessionId}
            aria-controls={`tab-panel-${tab.sessionId}`}
            data-testid={`tab-session-${tab.sessionId}`}
          >
            <span className="tab-icon" aria-hidden="true">💬</span>
            <span className="tab-label" title={tab.sessionId}>
              {getTabTitle(tab.sessionId)}
            </span>
            <button
              className="tab-close"
              onClick={(e) => closeSessionTab(tab.sessionId, e)}
              aria-label={`Close ${tab.sessionId} tab`}
              data-testid={`tab-close-${tab.sessionId}`}
            >
              ×
            </button>
          </button>
        ))}
      </div>

      {/* Tab Panels */}
      <div className="tab-panels">
        {/* Sessions List Panel */}
        {activeTab === 'sessions-list' && (
          <div
            id="tab-panel-sessions-list"
            className="tab-panel"
            role="tabpanel"
            aria-labelledby="tab-sessions-list"
            data-testid="tab-panel-sessions-list"
          >
            <div className="sessions-list-view">
              <div className="sessions-list-header">
                <h2>Sessions</h2>
                <p>Manage your Copilot sessions. Click a session to open it in a new tab.</p>
              </div>
              <SessionsList
                showCreateButton={true}
                onCreateClick={handleCreateClick}
                compact={false}
                onSessionClick={handleSessionClick}
              />
            </div>
          </div>
        )}

        {/* Session Chat Panels */}
        {openTabs.map((tab) => (
          activeTab === tab.sessionId && (
            <div
              key={tab.sessionId}
              id={`tab-panel-${tab.sessionId}`}
              className="tab-panel"
              role="tabpanel"
              aria-labelledby={`tab-session-${tab.sessionId}`}
              data-testid={`tab-panel-${tab.sessionId}`}
            >
              {activeSession && activeSessionId === tab.sessionId ? (
                <SessionChatView
                  session={activeSession}
                  events={activeSessionEvents}
                  isSending={isSending}
                  error={error}
                  onClearError={clearError}
                />
              ) : (
                <div className="session-loading" data-testid="session-loading">
                  <div className="session-loading-content">
                    <div className="session-loading-spinner" />
                    <p>Loading session...</p>
                  </div>
                </div>
              )}
            </div>
          )
        ))}
      </div>

      {/* Create Session Modal */}
      <CreateSessionModal
        isOpen={isCreateModalOpen}
        onClose={handleModalClose}
        onSessionCreated={handleSessionCreated}
        createSession={createSession}
        isCreating={isLoading}
      />
    </div>
  );
}

export default TabContainer;
