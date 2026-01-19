/**
 * Provider configuration editor for BYOK (Bring Your Own Key) scenarios.
 */
import React from 'react';
import { ProviderConfig } from '../types';
import './ProviderConfigEditor.css';

/**
 * Props for the ProviderConfigEditor component.
 */
export interface ProviderConfigEditorProps {
  /** Current provider configuration. */
  value: ProviderConfig | undefined;
  /** Callback when configuration changes. */
  onChange: (config: ProviderConfig | undefined) => void;
  /** Whether the editor is disabled. */
  disabled?: boolean;
}

/**
 * Available provider types.
 */
const PROVIDER_TYPES = [
  { value: 'openai', label: 'OpenAI' },
  { value: 'azure', label: 'Azure OpenAI' },
  { value: 'anthropic', label: 'Anthropic' },
  { value: 'custom', label: 'Custom' },
] as const;

/**
 * Available wire API formats.
 */
const WIRE_API_FORMATS = [
  { value: 'openai', label: 'OpenAI Compatible' },
  { value: 'azure', label: 'Azure' },
  { value: 'anthropic', label: 'Anthropic' },
] as const;

/**
 * Default provider config.
 */
const defaultConfig: ProviderConfig = {
  type: 'openai',
  baseUrl: '',
  apiKey: '',
  wireApi: 'openai',
};

/**
 * Provider configuration editor component.
 */
export function ProviderConfigEditor({ value, onChange, disabled = false }: ProviderConfigEditorProps) {
  const config = value || defaultConfig;
  const isEnabled = value !== undefined;

  const handleToggle = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.checked) {
      onChange(defaultConfig);
    } else {
      onChange(undefined);
    }
  };

  const handleFieldChange = (field: keyof ProviderConfig, fieldValue: string) => {
    if (value) {
      onChange({ ...value, [field]: fieldValue || undefined });
    }
  };

  return (
    <div className="provider-config-editor" data-testid="provider-config-editor">
      <div className="provider-config-header">
        <label className="provider-config-toggle">
          <input
            type="checkbox"
            checked={isEnabled}
            onChange={handleToggle}
            disabled={disabled}
            data-testid="provider-config-toggle"
          />
          <span className="toggle-label">Use Custom Provider (BYOK)</span>
        </label>
      </div>

      {isEnabled && (
        <div className="provider-config-content">
          <div className="provider-warning">
            <span className="warning-icon">⚠️</span>
            <span>
              API keys are transmitted to the backend. Only use this in trusted environments.
            </span>
          </div>

          <div className="provider-field">
            <label htmlFor="provider-type" className="provider-field-label">
              Provider Type
            </label>
            <select
              id="provider-type"
              className="provider-field-select"
              value={config.type}
              onChange={(e) => handleFieldChange('type', e.target.value)}
              disabled={disabled}
              data-testid="provider-type"
            >
              {PROVIDER_TYPES.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div className="provider-field">
            <label htmlFor="provider-base-url" className="provider-field-label">
              Base URL
            </label>
            <input
              id="provider-base-url"
              type="url"
              className="provider-field-input"
              value={config.baseUrl || ''}
              onChange={(e) => handleFieldChange('baseUrl', e.target.value)}
              placeholder="https://api.openai.com/v1"
              disabled={disabled}
              data-testid="provider-base-url"
            />
            <span className="provider-field-hint">
              API endpoint URL (leave empty for default)
            </span>
          </div>

          <div className="provider-field">
            <label htmlFor="provider-api-key" className="provider-field-label">
              API Key
            </label>
            <input
              id="provider-api-key"
              type="password"
              className="provider-field-input"
              value={config.apiKey || ''}
              onChange={(e) => handleFieldChange('apiKey', e.target.value)}
              placeholder="sk-..."
              disabled={disabled}
              autoComplete="off"
              data-testid="provider-api-key"
            />
            <span className="provider-field-hint">
              Your provider API key
            </span>
          </div>

          <div className="provider-field">
            <label htmlFor="provider-bearer-token" className="provider-field-label">
              Bearer Token (Optional)
            </label>
            <input
              id="provider-bearer-token"
              type="password"
              className="provider-field-input"
              value={config.bearerToken || ''}
              onChange={(e) => handleFieldChange('bearerToken', e.target.value)}
              placeholder="Optional bearer token"
              disabled={disabled}
              autoComplete="off"
              data-testid="provider-bearer-token"
            />
            <span className="provider-field-hint">
              Use instead of API key if your provider requires bearer authentication
            </span>
          </div>

          <div className="provider-field">
            <label htmlFor="provider-wire-api" className="provider-field-label">
              Wire API Format
            </label>
            <select
              id="provider-wire-api"
              className="provider-field-select"
              value={config.wireApi || 'openai'}
              onChange={(e) => handleFieldChange('wireApi', e.target.value)}
              disabled={disabled}
              data-testid="provider-wire-api"
            >
              {WIRE_API_FORMATS.map((format) => (
                <option key={format.value} value={format.value}>
                  {format.label}
                </option>
              ))}
            </select>
            <span className="provider-field-hint">
              API format used by your provider
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

export default ProviderConfigEditor;
