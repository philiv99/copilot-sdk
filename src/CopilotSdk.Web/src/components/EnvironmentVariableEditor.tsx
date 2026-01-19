/**
 * Environment variable editor component.
 * Allows adding, editing, and removing environment variables.
 */
import React, { useState, useCallback } from 'react';
import './EnvironmentVariableEditor.css';

/**
 * Props for the EnvironmentVariableEditor component.
 */
interface EnvironmentVariableEditorProps {
  /** Current environment variables. */
  variables: Record<string, string>;
  /** Callback when variables change. */
  onChange: (variables: Record<string, string>) => void;
  /** Whether the editor is disabled. */
  disabled?: boolean;
}

/**
 * Environment variable editor component.
 */
export function EnvironmentVariableEditor({
  variables,
  onChange,
  disabled = false,
}: EnvironmentVariableEditorProps) {
  const [newKey, setNewKey] = useState('');
  const [newValue, setNewValue] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Convert variables object to array for display
  const entries = Object.entries(variables);

  // Handle adding a new variable
  const handleAdd = useCallback(() => {
    setError(null);

    const trimmedKey = newKey.trim();
    if (!trimmedKey) {
      setError('Variable name is required');
      return;
    }

    if (variables.hasOwnProperty(trimmedKey)) {
      setError('Variable already exists');
      return;
    }

    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(trimmedKey)) {
      setError('Invalid variable name. Use letters, numbers, and underscores only.');
      return;
    }

    onChange({
      ...variables,
      [trimmedKey]: newValue,
    });

    setNewKey('');
    setNewValue('');
  }, [newKey, newValue, variables, onChange]);

  // Handle updating a variable value
  const handleUpdate = useCallback(
    (key: string, value: string) => {
      onChange({
        ...variables,
        [key]: value,
      });
    },
    [variables, onChange]
  );

  // Handle removing a variable
  const handleRemove = useCallback(
    (key: string) => {
      const { [key]: _, ...rest } = variables;
      onChange(rest);
    },
    [variables, onChange]
  );

  // Handle key press in input fields
  const handleKeyPress = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        handleAdd();
      }
    },
    [handleAdd]
  );

  return (
    <div className="env-var-editor" data-testid="env-var-editor">
      {/* Existing Variables */}
      {entries.length > 0 && (
        <div className="env-var-list" data-testid="env-var-list">
          {entries.map(([key, value]) => (
            <div key={key} className="env-var-item" data-testid={`env-var-item-${key}`}>
              <span className="env-var-key">{key}</span>
              <input
                type="text"
                className="env-var-value"
                value={value}
                onChange={(e) => handleUpdate(key, e.target.value)}
                disabled={disabled}
                placeholder="Value"
                aria-label={`Value for ${key}`}
              />
              <button
                type="button"
                className="env-var-remove"
                onClick={() => handleRemove(key)}
                disabled={disabled}
                aria-label={`Remove ${key}`}
                title="Remove variable"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Add New Variable */}
      <div className="env-var-add" data-testid="env-var-add">
        <input
          type="text"
          className="env-var-new-key"
          value={newKey}
          onChange={(e) => {
            setNewKey(e.target.value);
            setError(null);
          }}
          onKeyPress={handleKeyPress}
          disabled={disabled}
          placeholder="Variable name"
          aria-label="New variable name"
          data-testid="env-var-new-key"
        />
        <input
          type="text"
          className="env-var-new-value"
          value={newValue}
          onChange={(e) => setNewValue(e.target.value)}
          onKeyPress={handleKeyPress}
          disabled={disabled}
          placeholder="Value"
          aria-label="New variable value"
          data-testid="env-var-new-value"
        />
        <button
          type="button"
          className="env-var-add-button"
          onClick={handleAdd}
          disabled={disabled || !newKey.trim()}
          aria-label="Add variable"
          data-testid="env-var-add-button"
        >
          Add
        </button>
      </div>

      {/* Error Message */}
      {error && (
        <div className="env-var-error" data-testid="env-var-error" role="alert">
          {error}
        </div>
      )}

      {/* Empty State */}
      {entries.length === 0 && (
        <p className="env-var-empty" data-testid="env-var-empty">
          No environment variables defined. Add one above.
        </p>
      )}
    </div>
  );
}

export default EnvironmentVariableEditor;
