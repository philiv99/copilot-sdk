/**
 * Create session modal component for configuring and creating new sessions.
 */
import React, { useState, useCallback } from 'react';
import { CreateSessionRequest, ToolDefinition, SystemMessageConfig, ProviderConfig } from '../types';
import { ModelSelector, AVAILABLE_MODELS } from './ModelSelector';
import { SystemMessageEditor } from './SystemMessageEditor';
import { ToolDefinitionEditor } from './ToolDefinitionEditor';
import { ProviderConfigEditor } from './ProviderConfigEditor';
import './CreateSessionModal.css';

/**
 * Props for the CreateSessionModal component.
 */
export interface CreateSessionModalProps {
  /** Whether the modal is open. */
  isOpen: boolean;
  /** Callback when modal is closed. */
  onClose: () => void;
  /** Callback when session is created successfully. */
  onSessionCreated: (sessionId: string) => void;
  /** Function to create a session. */
  createSession: (request: CreateSessionRequest) => Promise<{ sessionId: string }>;
  /** Whether a session is being created. */
  isCreating?: boolean;
}

/**
 * Tab type for modal sections.
 */
type TabType = 'basic' | 'system' | 'tools' | 'provider';

/**
 * Create session modal component.
 */
export function CreateSessionModal({
  isOpen,
  onClose,
  onSessionCreated,
  createSession,
  isCreating = false,
}: CreateSessionModalProps) {
  // Form state
  const [sessionId, setSessionId] = useState('');
  const [model, setModel] = useState<string>(AVAILABLE_MODELS[0].value);
  const [streaming, setStreaming] = useState(true);
  const [systemMessage, setSystemMessage] = useState<SystemMessageConfig | undefined>(undefined);
  const [tools, setTools] = useState<ToolDefinition[]>([]);
  const [provider, setProvider] = useState<ProviderConfig | undefined>(undefined);
  
  // UI state
  const [activeTab, setActiveTab] = useState<TabType>('basic');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset form when modal opens
  const resetForm = useCallback(() => {
    setSessionId('');
    setModel(AVAILABLE_MODELS[0].value);
    setStreaming(true);
    setSystemMessage(undefined);
    setTools([]);
    setProvider(undefined);
    setActiveTab('basic');
    setError(null);
  }, []);

  // Handle close
  const handleClose = useCallback(() => {
    if (!isSubmitting && !isCreating) {
      resetForm();
      onClose();
    }
  }, [isSubmitting, isCreating, resetForm, onClose]);

  // Validate session ID format (alphanumeric, hyphens, underscores only)
  const isValidSessionId = useCallback((id: string): boolean => {
    if (!id) return true; // Empty is valid (will be auto-generated)
    // Session IDs must be alphanumeric with optional hyphens and underscores
    return /^[a-zA-Z0-9_-]+$/.test(id);
  }, []);

  // Handle form submission
  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      // Validate session ID format
      const trimmedSessionId = sessionId.trim();
      if (trimmedSessionId && !isValidSessionId(trimmedSessionId)) {
        setError('Session ID can only contain letters, numbers, hyphens (-), and underscores (_). No spaces allowed.');
        setIsSubmitting(false);
        return;
      }

      const request: CreateSessionRequest = {
        model,
        streaming,
      };

      // Only include optional fields if they have values
      if (trimmedSessionId) {
        request.sessionId = trimmedSessionId;
      }
      if (systemMessage) {
        request.systemMessage = systemMessage;
      }
      if (tools.length > 0) {
        // Validate tools
        const invalidTool = tools.find((t) => !t.name.trim() || !t.description.trim());
        if (invalidTool) {
          setError('All tools must have a name and description');
          setIsSubmitting(false);
          return;
        }
        request.tools = tools;
      }
      if (provider) {
        request.provider = provider;
      }

      const result = await createSession(request);
      resetForm();
      onSessionCreated(result.sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create session');
    } finally {
      setIsSubmitting(false);
    }
  }, [model, streaming, sessionId, systemMessage, tools, provider, createSession, resetForm, onSessionCreated, isValidSessionId]);

  // Handle backdrop click
  const handleBackdropClick = useCallback((e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      handleClose();
    }
  }, [handleClose]);

  // Handle escape key
  React.useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        handleClose();
      }
    };
    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, handleClose]);

  if (!isOpen) {
    return null;
  }

  const isLoading = isSubmitting || isCreating;

  return (
    <div className="modal-backdrop" onClick={handleBackdropClick} data-testid="create-session-modal">
      <div className="modal-container" role="dialog" aria-labelledby="modal-title">
        <div className="modal-header">
          <h2 id="modal-title" className="modal-title">Create New Session</h2>
          <button
            className="modal-close-btn"
            onClick={handleClose}
            disabled={isLoading}
            aria-label="Close modal"
          >
            ×
          </button>
        </div>

        {/* Tab navigation */}
        <div className="modal-tabs">
          <button
            className={`modal-tab ${activeTab === 'basic' ? 'active' : ''}`}
            onClick={() => setActiveTab('basic')}
            type="button"
          >
            Basic
          </button>
          <button
            className={`modal-tab ${activeTab === 'system' ? 'active' : ''}`}
            onClick={() => setActiveTab('system')}
            type="button"
          >
            System Message
          </button>
          <button
            className={`modal-tab ${activeTab === 'tools' ? 'active' : ''}`}
            onClick={() => setActiveTab('tools')}
            type="button"
          >
            Tools {tools.length > 0 && <span className="tab-badge">{tools.length}</span>}
          </button>
          <button
            className={`modal-tab ${activeTab === 'provider' ? 'active' : ''}`}
            onClick={() => setActiveTab('provider')}
            type="button"
          >
            Provider {provider && <span className="tab-badge">✓</span>}
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="modal-body">
            {error && (
              <div className="modal-error" data-testid="modal-error">
                <span>{error}</span>
                <button type="button" onClick={() => setError(null)} className="error-dismiss">×</button>
              </div>
            )}

            {/* Basic Settings Tab */}
            {activeTab === 'basic' && (
              <div className="tab-content" data-testid="basic-tab">
                <div className="form-field">
                  <label htmlFor="session-id" className="form-label">
                    Session ID (Optional)
                  </label>
                  <input
                    id="session-id"
                    type="text"
                    className={`form-input ${sessionId && !isValidSessionId(sessionId) ? 'input-error' : ''}`}
                    value={sessionId}
                    onChange={(e) => setSessionId(e.target.value)}
                    placeholder="Auto-generated if empty"
                    disabled={isLoading}
                    data-testid="session-id-input"
                    pattern="[a-zA-Z0-9_\-]*"
                  />
                  {sessionId && !isValidSessionId(sessionId) ? (
                    <span className="form-hint form-hint-error">
                      Only letters, numbers, hyphens (-), and underscores (_) allowed
                    </span>
                  ) : (
                    <span className="form-hint">
                      Leave empty to auto-generate a unique ID
                    </span>
                  )}
                </div>

                <ModelSelector
                  value={model}
                  onChange={setModel}
                  disabled={isLoading}
                  showDescriptions={true}
                />

                <div className="form-field">
                  <label className="form-checkbox-label">
                    <input
                      type="checkbox"
                      checked={streaming}
                      onChange={(e) => setStreaming(e.target.checked)}
                      disabled={isLoading}
                      data-testid="streaming-checkbox"
                    />
                    <span>Enable Streaming Responses</span>
                  </label>
                  <span className="form-hint">
                    Stream responses in real-time as they are generated
                  </span>
                </div>
              </div>
            )}

            {/* System Message Tab */}
            {activeTab === 'system' && (
              <div className="tab-content" data-testid="system-tab">
                <SystemMessageEditor
                  value={systemMessage}
                  onChange={setSystemMessage}
                  disabled={isLoading}
                />
              </div>
            )}

            {/* Tools Tab */}
            {activeTab === 'tools' && (
              <div className="tab-content" data-testid="tools-tab">
                <ToolDefinitionEditor
                  tools={tools}
                  onChange={setTools}
                  disabled={isLoading}
                />
              </div>
            )}

            {/* Provider Tab */}
            {activeTab === 'provider' && (
              <div className="tab-content" data-testid="provider-tab">
                <ProviderConfigEditor
                  value={provider}
                  onChange={setProvider}
                  disabled={isLoading}
                />
              </div>
            )}
          </div>

          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={isLoading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isLoading}
              data-testid="create-session-submit"
            >
              {isLoading ? 'Creating...' : 'Create Session'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default CreateSessionModal;
