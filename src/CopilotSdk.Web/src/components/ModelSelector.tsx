/**
 * Model selector component for choosing the AI model.
 */
import React from 'react';
import './ModelSelector.css';

/**
 * Available models for selection.
 */
export const AVAILABLE_MODELS = [
  { value: 'gpt-4o', label: 'GPT-4o', description: 'Most capable model for complex tasks' },
  { value: 'gpt-4o-mini', label: 'GPT-4o Mini', description: 'Fast and efficient for simpler tasks' },
  { value: 'o1-preview', label: 'O1 Preview', description: 'Advanced reasoning model' },
  { value: 'o1-mini', label: 'O1 Mini', description: 'Efficient reasoning model' },
  { value: 'claude-3.5-sonnet', label: 'Claude 3.5 Sonnet', description: 'Balanced performance and speed' },
  { value: 'claude-3-opus', label: 'Claude 3 Opus', description: 'Most capable Claude model' },
] as const;

/**
 * Props for the ModelSelector component.
 */
export interface ModelSelectorProps {
  /** Currently selected model. */
  value: string;
  /** Callback when model changes. */
  onChange: (model: string) => void;
  /** Whether the selector is disabled. */
  disabled?: boolean;
  /** Additional CSS class. */
  className?: string;
  /** Whether to show model descriptions. */
  showDescriptions?: boolean;
  /** Label text. */
  label?: string;
}

/**
 * Model selector component.
 */
export function ModelSelector({
  value,
  onChange,
  disabled = false,
  className = '',
  showDescriptions = false,
  label = 'Model',
}: ModelSelectorProps) {
  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onChange(e.target.value);
  };

  const selectedModel = AVAILABLE_MODELS.find((m) => m.value === value);

  return (
    <div className={`model-selector ${className}`} data-testid="model-selector">
      <label className="model-selector-label" htmlFor="model-select">
        {label}
      </label>
      <select
        id="model-select"
        className="model-selector-select"
        value={value}
        onChange={handleChange}
        disabled={disabled}
        data-testid="model-select"
      >
        {AVAILABLE_MODELS.map((model) => (
          <option key={model.value} value={model.value}>
            {model.label}
          </option>
        ))}
      </select>
      {showDescriptions && selectedModel && (
        <p className="model-selector-description" data-testid="model-description">
          {selectedModel.description}
        </p>
      )}
    </div>
  );
}

export default ModelSelector;
