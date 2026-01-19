/**
 * Sessions view - Manage Copilot sessions.
 */
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useSession } from '../context';
import { SessionsList, CreateSessionModal } from '../components';
import { SessionChatView } from './SessionChatView';
import './SessionsView.css';

/**
 * Sessions view component.
 */
export function SessionsView() {
  const { sessionId } = useParams<{ sessionId?: string }>();
  const navigate = useNavigate();
  const {
    activeSession,
    activeSessionId,
    activeSessionEvents,
    selectSession,
    createSession,
    isLoading,
    isSending,
    error,
    clearError,
  } = useSession();

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Select session from URL param
  useEffect(() => {
    if (sessionId && sessionId !== activeSessionId) {
      selectSession(sessionId).catch(() => {
        // Error handled by context
      });
    }
  }, [sessionId, activeSessionId, selectSession]);

  const handleCreateClick = () => {
    setIsCreateModalOpen(true);
  };

  const handleModalClose = () => {
    setIsCreateModalOpen(false);
  };

  const handleSessionCreated = (newSessionId: string) => {
    setIsCreateModalOpen(false);
    navigate(`/sessions/${newSessionId}`);
  };

  // If we have an active session, show the session chat view
  if (activeSession && activeSessionId) {
    return (
      <SessionChatView
        session={activeSession}
        events={activeSessionEvents}
        isSending={isSending}
        error={error}
        onClearError={clearError}
      />
    );
  }

  // Show sessions list view
  return (
    <div className="sessions-view" data-testid="sessions-view">
      <div className="sessions-view-header">
        <h2>Sessions</h2>
        <p>Manage your Copilot sessions and create new conversations.</p>
      </div>

      <SessionsList
        showCreateButton={true}
        onCreateClick={handleCreateClick}
        compact={false}
      />

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

export default SessionsView;

