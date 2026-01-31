/**
 * System message editor component for configuring session system prompts.
 */
import React, { useEffect, useCallback, useRef, useState } from 'react';
import { SystemMessageConfig, SystemPromptTemplate } from '../types';
import { usePromptRefinement } from '../hooks';
import { RefineButton } from './RefineButton';
import { getSystemPromptTemplates, getSystemPromptTemplateContent } from '../api/copilotApi';
import './SystemMessageEditor.css';

/**
 * Props for the SystemMessageEditor component.
 */
export interface SystemMessageEditorProps {
  /** Current system message configuration. */
  value: SystemMessageConfig | undefined;
  /** Callback when configuration changes. */
  onChange: (config: SystemMessageConfig | undefined) => void;
  /** Whether the editor is disabled. */
  disabled?: boolean;
  /** Placeholder text for the content textarea. */
  placeholder?: string;
}

/**
 * Default system message config.
 */
const defaultConfig: SystemMessageConfig = {
  mode: 'Append',
  content: '',
};

/**
 * System message editor component.
 */
export function SystemMessageEditor({
  value,
  onChange,
  disabled = false,
  placeholder = 'Enter custom instructions for the AI assistant...',
}: SystemMessageEditorProps) {
  const config = value || defaultConfig;
  const isEnabled = value !== undefined;
  const { 
    refine, 
    isRefining, 
    error, 
    iterationCount, 
    clearError, 
    cancel, 
    previousContent,
    canUndo,
    lastResponse
  } = usePromptRefinement();
  
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const previousLengthRef = useRef<number | null>(null);

  // Template state
  const [templates, setTemplates] = useState<SystemPromptTemplate[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<string>('');
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false);
  const [isLoadingContent, setIsLoadingContent] = useState(false);
  const [templateError, setTemplateError] = useState<string | null>(null);

  // Load templates when component mounts or becomes enabled
  useEffect(() => {
    if (isEnabled && templates.length === 0) {
      loadTemplates();
    }
  }, [isEnabled]);

  const loadTemplates = async () => {
    setIsLoadingTemplates(true);
    setTemplateError(null);
    try {
      const response = await getSystemPromptTemplates();
      setTemplates(response.templates);
    } catch (err) {
      setTemplateError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setIsLoadingTemplates(false);
    }
  };

  const handleTemplateSelect = async (templateName: string) => {
    setSelectedTemplate(templateName);
    setTemplateError(null);
    
    if (!templateName) {
      return;
    }

    setIsLoadingContent(true);
    try {
      const response = await getSystemPromptTemplateContent(templateName);
      if (value) {
        onChange({ ...value, content: response.content });
        announceToScreenReader(`Template "${templateName}" loaded successfully.`);
      }
    } catch (err) {
      setTemplateError(err instanceof Error ? err.message : 'Failed to load template content');
    } finally {
      setIsLoadingContent(false);
    }
  };

  // Calculate character count change
  const charCountChange = lastResponse && previousLengthRef.current !== null
    ? config.content.length - previousLengthRef.current
    : null;

  const handleToggle = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.checked) {
      onChange(defaultConfig);
    } else {
      onChange(undefined);
    }
  };

  const handleModeChange = (mode: 'Append' | 'Replace') => {
    if (value) {
      onChange({ ...value, mode });
    }
  };

  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    if (value) {
      onChange({ ...value, content: e.target.value });
    }
    // Clear any previous refinement error when user types
    if (error) {
      clearError();
    }
    // Reset character count change when user manually edits
    previousLengthRef.current = null;
  };

  const handleRefine = useCallback(async () => {
    if (!value || !value.content.trim()) {
      return;
    }
    
    // Store previous length for character count change
    previousLengthRef.current = value.content.length;
    
    const refinedContent = await refine(value.content);
    if (refinedContent) {
      onChange({ ...value, content: refinedContent });
      // Announce to screen readers
      announceToScreenReader('Refinement complete. Content has been updated.');
    }
  }, [value, onChange, refine]);

  const handleUndo = useCallback(() => {
    if (canUndo && previousContent && value) {
      onChange({ ...value, content: previousContent });
      previousLengthRef.current = null;
      announceToScreenReader('Undo complete. Content restored to previous version.');
    }
  }, [canUndo, previousContent, value, onChange]);

  const handleCancel = useCallback(() => {
    cancel();
    announceToScreenReader('Refinement cancelled.');
  }, [cancel]);

  // Keyboard shortcut: Ctrl+Shift+R to refine
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.shiftKey && e.key === 'R') {
        e.preventDefault();
        if (!disabled && !isRefining && config.content.trim()) {
          handleRefine();
        }
      }
      // Ctrl+Z while focused on textarea and canUndo is available
      if (e.ctrlKey && e.key === 'z' && canUndo && document.activeElement === textareaRef.current) {
        // Don't prevent default - let the browser handle normal undo
        // Only handle our custom undo if shift is also pressed
        if (e.shiftKey) {
          e.preventDefault();
          handleUndo();
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [disabled, isRefining, config.content, handleRefine, canUndo, handleUndo]);

  return (
    <div className="system-message-editor" data-testid="system-message-editor">
      <div className="system-message-header">
        <label className="system-message-toggle">
          <input
            type="checkbox"
            checked={isEnabled}
            onChange={handleToggle}
            disabled={disabled}
            data-testid="system-message-toggle"
          />
          <span className="toggle-label">Custom System Message</span>
        </label>
      </div>

      {isEnabled && (
        <div className="system-message-content">
          <div className="system-message-mode">
            <span className="mode-label">Mode:</span>
            <div className="mode-options">
              <label className="mode-option">
                <input
                  type="radio"
                  name="systemMessageMode"
                  value="Append"
                  checked={config.mode === 'Append'}
                  onChange={() => handleModeChange('Append')}
                  disabled={disabled}
                />
                <span className="mode-option-label">Append</span>
                <span className="mode-option-description">Add to default system message</span>
              </label>
              <label className="mode-option">
                <input
                  type="radio"
                  name="systemMessageMode"
                  value="Replace"
                  checked={config.mode === 'Replace'}
                  onChange={() => handleModeChange('Replace')}
                  disabled={disabled}
                />
                <span className="mode-option-label">Replace</span>
                <span className="mode-option-description">Override default system message</span>
              </label>
            </div>
          </div>

          {/* Template selector */}
          <div className="system-message-template">
            <label htmlFor="template-selector" className="template-label">
              Load from Template:
            </label>
            <div className="template-selector-wrapper">
              <select
                id="template-selector"
                className="template-selector"
                value={selectedTemplate}
                onChange={(e) => handleTemplateSelect(e.target.value)}
                disabled={disabled || isLoadingTemplates || isLoadingContent}
                data-testid="template-selector"
                aria-describedby={templateError ? 'template-error' : undefined}
              >
                <option value="">
                  {isLoadingTemplates ? 'Loading templates...' : '-- Select a template --'}
                </option>
                {templates.map((template) => (
                  <option key={template.name} value={template.name}>
                    {template.displayName}
                  </option>
                ))}
              </select>
              {isLoadingContent && (
                <span className="template-loading-indicator" aria-hidden="true">
                  Loading...
                </span>
              )}
            </div>
            {templateError && (
              <div id="template-error" className="template-error" role="alert" data-testid="template-error">
                <span className="error-icon" aria-hidden="true">⚠</span>
                <span className="error-text">{templateError}</span>
              </div>
            )}
          </div>

          <div className="system-message-text">
            <div className="text-label-row">
              <label htmlFor="system-message-content" className="text-label">
                Content:
              </label>
              <div className="text-actions">
                {canUndo && !isRefining && (
                  <button
                    type="button"
                    className="undo-button"
                    onClick={handleUndo}
                    title="Undo last refinement (Ctrl+Shift+Z)"
                    aria-label="Undo last refinement"
                    data-testid="undo-button"
                  >
                    <UndoIcon />
                    <span className="undo-button-text">Undo</span>
                  </button>
                )}
                {isRefining && (
                  <button
                    type="button"
                    className="cancel-button"
                    onClick={handleCancel}
                    title="Cancel refinement"
                    aria-label="Cancel refinement"
                    data-testid="cancel-refine-button"
                  >
                    <CancelIcon />
                    <span className="cancel-button-text">Cancel</span>
                  </button>
                )}
                <RefineButton
                  onClick={handleRefine}
                  isRefining={isRefining}
                  disabled={disabled || !config.content.trim()}
                  iterationCount={iterationCount}
                  title="Refine prompt using AI to expand and improve the content (Ctrl+Shift+R)"
                />
              </div>
            </div>
            <div className={`textarea-wrapper ${isRefining ? 'refining' : ''}`}>
              <textarea
                ref={textareaRef}
                id="system-message-content"
                className="system-message-textarea"
                value={config.content}
                onChange={handleContentChange}
                placeholder={placeholder}
                disabled={disabled || isRefining}
                rows={6}
                data-testid="system-message-content"
                aria-describedby="char-count-info"
              />
              {isRefining && (
                <div 
                  className="refining-overlay" 
                  data-testid="refining-overlay"
                  role="status"
                  aria-live="polite"
                >
                  <span className="refining-text">Refining your prompt...</span>
                </div>
              )}
            </div>
            {error && (
              <div className="refine-error" role="alert" data-testid="refine-error">
                <span className="error-icon" aria-hidden="true">⚠</span>
                <span className="error-text">{error}</span>
                <button 
                  type="button" 
                  className="error-dismiss" 
                  onClick={clearError}
                  aria-label="Dismiss error"
                >
                  ×
                </button>
              </div>
            )}
            <div className="char-count-row" id="char-count-info">
              <span className="char-count">{config.content.length} characters</span>
              {charCountChange !== null && charCountChange !== 0 && (
                <span 
                  className={`char-count-change ${charCountChange > 0 ? 'positive' : 'negative'}`}
                  data-testid="char-count-change"
                >
                  ({charCountChange > 0 ? '+' : ''}{charCountChange})
                </span>
              )}
            </div>
          </div>
        </div>
      )}
      
      {/* Screen reader only announcements */}
      <div 
        id="sr-announcements" 
        className="sr-only" 
        aria-live="polite" 
        aria-atomic="true"
      />
    </div>
  );
}

/**
 * Announces a message to screen readers.
 */
function announceToScreenReader(message: string) {
  const announcer = document.getElementById('sr-announcements');
  if (announcer) {
    announcer.textContent = message;
    // Clear after announcement
    setTimeout(() => {
      announcer.textContent = '';
    }, 1000);
  }
}

/**
 * Undo icon component.
 */
function UndoIcon() {
  return (
    <svg
      className="undo-icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M3 7v6h6" />
      <path d="M21 17a9 9 0 0 0-9-9 9 9 0 0 0-6 2.3L3 13" />
    </svg>
  );
}

/**
 * Cancel icon component.
 */
function CancelIcon() {
  return (
    <svg
      className="cancel-icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="10" />
      <line x1="15" y1="9" x2="9" y2="15" />
      <line x1="9" y1="9" x2="15" y2="15" />
    </svg>
  );
}

export default SystemMessageEditor;
