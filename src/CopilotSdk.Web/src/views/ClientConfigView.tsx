/**
 * Client configuration view - allows configuring the Copilot SDK client settings.
 */
import React, { useState, useCallback, useEffect } from 'react';
import { useCopilotClient } from '../context';
import { ConnectionStatusIndicator, EnvironmentVariableEditor } from '../components';
import { CopilotClientConfig } from '../types';
import './ClientConfigView.css';

/**
 * Log level options.
 */
const LOG_LEVELS = ['debug', 'info', 'warn', 'error'] as const;

/**
 * Props for ClientConfigView.
 */
export interface ClientConfigViewProps {
  /** Whether the view is rendered inside a modal. */
  isModal?: boolean;
}

/**
 * Client configuration view component.
 */
export function ClientConfigView({ isModal = false }: ClientConfigViewProps) {
  const {
    config,
    status,
    isLoading,
    error,
    connectionState,
    isConnected,
    refreshConfig,
    updateConfig,
    startClient,
    stopClient,
    forceStopClient,
    pingClient,
    clearError,
  } = useCopilotClient();

  // Local form state
  const [formData, setFormData] = useState<Partial<CopilotClientConfig>>({});
  const [isDirty, setIsDirty] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isPinging, setIsPinging] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Initialize form data from config
  useEffect(() => {
    if (config) {
      setFormData({
        cliPath: config.cliPath || '',
        cliArgs: config.cliArgs || [],
        cliUrl: config.cliUrl || '',
        port: config.port,
        useStdio: config.useStdio,
        logLevel: config.logLevel,
        autoStart: config.autoStart,
        autoRestart: config.autoRestart,
        cwd: config.cwd || '',
        environment: config.environment || {},
      });
      setIsDirty(false);
    }
  }, [config]);

  // Handle input change
  const handleChange = useCallback(
    (field: keyof CopilotClientConfig, value: string | number | boolean | string[] | Record<string, string>) => {
      setFormData((prev) => ({
        ...prev,
        [field]: value,
      }));
      setIsDirty(true);
      setSaveSuccess(false);
    },
    []
  );

  // Handle CLI args change (comma-separated)
  const handleCliArgsChange = useCallback((value: string) => {
    const args = value
      .split(',')
      .map((arg) => arg.trim())
      .filter((arg) => arg !== '');
    handleChange('cliArgs', args);
  }, [handleChange]);

  // Handle save
  const handleSave = useCallback(async () => {
    setIsSaving(true);
    setSaveSuccess(false);
    try {
      await updateConfig(formData);
      setIsDirty(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch {
      // Error is handled by context
    } finally {
      setIsSaving(false);
    }
  }, [formData, updateConfig]);

  // Handle reset
  const handleReset = useCallback(() => {
    if (config) {
      setFormData({
        cliPath: config.cliPath || '',
        cliArgs: config.cliArgs || [],
        cliUrl: config.cliUrl || '',
        port: config.port,
        useStdio: config.useStdio,
        logLevel: config.logLevel,
        autoStart: config.autoStart,
        autoRestart: config.autoRestart,
        cwd: config.cwd || '',
        environment: config.environment || {},
      });
      setIsDirty(false);
      setSaveSuccess(false);
    }
  }, [config]);

  // Handle start
  const handleStart = useCallback(async () => {
    try {
      await startClient();
    } catch {
      // Error is handled by context
    }
  }, [startClient]);

  // Handle stop
  const handleStop = useCallback(async () => {
    try {
      await stopClient();
    } catch {
      // Error is handled by context
    }
  }, [stopClient]);

  // Handle force stop
  const handleForceStop = useCallback(async () => {
    try {
      await forceStopClient();
    } catch {
      // Error is handled by context
    }
  }, [forceStopClient]);

  // Handle ping
  const handlePing = useCallback(async () => {
    setIsPinging(true);
    try {
      await pingClient();
    } catch {
      // Error is handled by context
    } finally {
      setIsPinging(false);
    }
  }, [pingClient]);

  return (
    <div className="client-config-view" data-testid="client-config-view">
      {!isModal && (
        <div className="config-header">
          <div className="config-header-left">
            <h2>Client Configuration</h2>
            <p>Configure the Copilot SDK client settings.</p>
          </div>
          <div className="config-header-right">
            <ConnectionStatusIndicator state={connectionState} />
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="config-error" data-testid="config-error">
          <span className="error-message">{error}</span>
          <button className="error-dismiss" onClick={clearError} aria-label="Dismiss error">
            ×
          </button>
        </div>
      )}

      {/* Success Message */}
      {saveSuccess && (
        <div className="config-success" data-testid="config-success">
          Configuration saved successfully.
        </div>
      )}

      {/* Connection Settings Section */}
      <section className="config-section" data-testid="connection-settings">
        <h3>Connection Settings</h3>
        <div className="config-form">
          <div className="form-group">
            <label htmlFor="cliPath">CLI Path</label>
            <input
              id="cliPath"
              type="text"
              value={formData.cliPath || ''}
              onChange={(e) => handleChange('cliPath', e.target.value)}
              placeholder="Path to Copilot CLI executable"
              data-testid="cli-path-input"
            />
            <span className="form-help">Path to the GitHub Copilot CLI executable.</span>
          </div>

          <div className="form-group">
            <label htmlFor="cliUrl">CLI URL</label>
            <input
              id="cliUrl"
              type="text"
              value={formData.cliUrl || ''}
              onChange={(e) => handleChange('cliUrl', e.target.value)}
              placeholder="http://localhost:8080"
              data-testid="cli-url-input"
            />
            <span className="form-help">URL of an existing Copilot CLI server to connect to.</span>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="port">Port</label>
              <input
                id="port"
                type="number"
                value={formData.port || 0}
                onChange={(e) => handleChange('port', parseInt(e.target.value, 10) || 0)}
                min={0}
                max={65535}
                data-testid="port-input"
              />
              <span className="form-help">Port number for TCP connection.</span>
            </div>

            <div className="form-group">
              <label htmlFor="useStdio">Use Stdio</label>
              <div className="checkbox-wrapper">
                <input
                  id="useStdio"
                  type="checkbox"
                  checked={formData.useStdio || false}
                  onChange={(e) => handleChange('useStdio', e.target.checked)}
                  data-testid="use-stdio-checkbox"
                />
                <span className="checkbox-label">Enable stdio communication</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Behavior Settings Section */}
      <section className="config-section" data-testid="behavior-settings">
        <h3>Behavior Settings</h3>
        <div className="config-form">
          <div className="form-group">
            <label htmlFor="logLevel">Log Level</label>
            <select
              id="logLevel"
              value={formData.logLevel || 'info'}
              onChange={(e) => handleChange('logLevel', e.target.value)}
              data-testid="log-level-select"
            >
              {LOG_LEVELS.map((level) => (
                <option key={level} value={level}>
                  {level.charAt(0).toUpperCase() + level.slice(1)}
                </option>
              ))}
            </select>
            <span className="form-help">Log level for the CLI server output.</span>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="autoStart">Auto Start</label>
              <div className="checkbox-wrapper">
                <input
                  id="autoStart"
                  type="checkbox"
                  checked={formData.autoStart || false}
                  onChange={(e) => handleChange('autoStart', e.target.checked)}
                  data-testid="auto-start-checkbox"
                />
                <span className="checkbox-label">Automatically start on first operation</span>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="autoRestart">Auto Restart</label>
              <div className="checkbox-wrapper">
                <input
                  id="autoRestart"
                  type="checkbox"
                  checked={formData.autoRestart || false}
                  onChange={(e) => handleChange('autoRestart', e.target.checked)}
                  data-testid="auto-restart-checkbox"
                />
                <span className="checkbox-label">Automatically restart on failure</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Process Settings Section */}
      <section className="config-section" data-testid="process-settings">
        <h3>Process Settings</h3>
        <div className="config-form">
          <div className="form-group">
            <label htmlFor="cwd">Working Directory</label>
            <input
              id="cwd"
              type="text"
              value={formData.cwd || ''}
              onChange={(e) => handleChange('cwd', e.target.value)}
              placeholder="/path/to/working/directory"
              data-testid="cwd-input"
            />
            <span className="form-help">Working directory for the CLI process.</span>
          </div>

          <div className="form-group">
            <label htmlFor="cliArgs">CLI Arguments</label>
            <input
              id="cliArgs"
              type="text"
              value={(formData.cliArgs || []).join(', ')}
              onChange={(e) => handleCliArgsChange(e.target.value)}
              placeholder="--arg1, --arg2=value"
              data-testid="cli-args-input"
            />
            <span className="form-help">Additional arguments (comma-separated).</span>
          </div>

          <div className="form-group">
            <label>Environment Variables</label>
            <EnvironmentVariableEditor
              variables={formData.environment || {}}
              onChange={(env) => handleChange('environment', env)}
              disabled={isLoading}
            />
          </div>
        </div>
      </section>

      {/* Action Buttons */}
      <section className="config-actions" data-testid="config-actions">
        <div className="actions-left">
          <button
            className="action-button primary"
            onClick={handleSave}
            disabled={isLoading || isSaving || !isDirty}
            data-testid="save-button"
          >
            {isSaving ? 'Saving...' : 'Save Configuration'}
          </button>
          <button
            className="action-button secondary"
            onClick={handleReset}
            disabled={isLoading || !isDirty}
            data-testid="reset-button"
          >
            Reset Changes
          </button>
          <button
            className="action-button outline"
            onClick={refreshConfig}
            disabled={isLoading}
            data-testid="refresh-button"
          >
            Refresh
          </button>
        </div>
        <div className="actions-right">
          <button
            className="action-button success"
            onClick={handleStart}
            disabled={isLoading || isConnected}
            data-testid="start-button"
          >
            Start Client
          </button>
          <button
            className="action-button secondary"
            onClick={handleStop}
            disabled={isLoading || !isConnected}
            data-testid="stop-button"
          >
            Stop Client
          </button>
          <button
            className="action-button danger"
            onClick={handleForceStop}
            disabled={isLoading || !isConnected}
            data-testid="force-stop-button"
          >
            Force Stop
          </button>
          <button
            className="action-button outline"
            onClick={handlePing}
            disabled={isPinging || !isConnected}
            data-testid="ping-button"
          >
            {isPinging ? 'Pinging...' : 'Ping'}
          </button>
        </div>
      </section>
    </div>
  );
}

export default ClientConfigView;
