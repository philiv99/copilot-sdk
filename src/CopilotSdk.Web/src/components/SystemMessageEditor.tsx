/**
 * System message editor component for configuring session system prompts.
 */
import React from 'react';
import { SystemMessageConfig } from '../types';
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
  };

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

          <div className="system-message-text">
            <label htmlFor="system-message-content" className="text-label">
              Content:
            </label>
            <textarea
              id="system-message-content"
              className="system-message-textarea"
              value={config.content}
              onChange={handleContentChange}
              placeholder={placeholder}
              disabled={disabled}
              rows={6}
              data-testid="system-message-content"
            />
            <span className="char-count">{config.content.length} characters</span>
          </div>
        </div>
      )}
    </div>
  );
}

export default SystemMessageEditor;
