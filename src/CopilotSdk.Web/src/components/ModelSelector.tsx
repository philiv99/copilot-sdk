/**
 * Model selector component for choosing the AI model.
 * Fetches available models from the backend API with caching.
 */
import React, { useState, useEffect } from 'react';
import { getModels } from '../api/copilotApi';
import { ModelInfo } from '../types';
import './ModelSelector.css';

/**
 * Default models used as fallback when API is unavailable.
 */
const DEFAULT_MODELS: ModelInfo[] = [
  { value: 'gpt-4o', label: 'GPT-4o', description: 'Most capable model for complex tasks' },
  { value: 'gpt-4o-mini', label: 'GPT-4o Mini', description: 'Fast and efficient for simpler tasks' },
  { value: 'claude-opus-4.5', label: 'Claude Opuse 4.5', description: 'Best performance and speed' },
];

/**
 * Cache key for storing models in localStorage.
 */
const MODELS_CACHE_KEY = 'copilot_models_cache';

/**
 * Cache duration in milliseconds (1 week).
 */
const CACHE_DURATION_MS = 7 * 24 * 60 * 60 * 1000;

/**
 * Cached models structure.
 */
interface CachedModels {
  models: ModelInfo[];
  cachedAt: number;
}

/**
 * Load cached models from localStorage.
 */
function loadCachedModels(): ModelInfo[] | null {
  try {
    const cached = localStorage.getItem(MODELS_CACHE_KEY);
    if (cached) {
      const parsed: CachedModels = JSON.parse(cached);
      const now = Date.now();
      if (now - parsed.cachedAt < CACHE_DURATION_MS) {
        return parsed.models;
      }
    }
  } catch {
    // Ignore parsing errors
  }
  return null;
}

/**
 * Save models to localStorage cache.
 */
function saveCachedModels(models: ModelInfo[]): void {
  try {
    const cached: CachedModels = {
      models,
      cachedAt: Date.now(),
    };
    localStorage.setItem(MODELS_CACHE_KEY, JSON.stringify(cached));
  } catch {
    // Ignore storage errors
  }
}

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
 * Fetches available models from the API and caches them for 1 week.
 */
export function ModelSelector({
  value,
  onChange,
  disabled = false,
  className = '',
  showDescriptions = false,
  label = 'Model',
}: ModelSelectorProps) {
  const [models, setModels] = useState<ModelInfo[]>(() => loadCachedModels() || DEFAULT_MODELS);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Check if we have valid cached models
    const cached = loadCachedModels();
    if (cached) {
      setModels(cached);
      return;
    }

    // Fetch models from API
    const fetchModels = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await getModels();
        if (response.models && response.models.length > 0) {
          setModels(response.models);
          saveCachedModels(response.models);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load models');
        // Keep using current models (cached or default)
      } finally {
        setIsLoading(false);
      }
    };

    fetchModels();
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onChange(e.target.value);
  };

  const selectedModel = models.find((m) => m.value === value);

  return (
    <div className={`model-selector ${className}`} data-testid="model-selector">
      <label className="model-selector-label" htmlFor="model-select">
        {label}
        {isLoading && <span className="model-selector-loading"> (loading...)</span>}
      </label>
      <select
        id="model-select"
        className="model-selector-select"
        value={value}
        onChange={handleChange}
        disabled={disabled || isLoading}
        data-testid="model-select"
      >
        {models.map((model) => (
          <option key={model.value} value={model.value}>
            {model.label}
          </option>
        ))}
      </select>
      {error && (
        <p className="model-selector-error" data-testid="model-error">
          {error}
        </p>
      )}
      {showDescriptions && selectedModel && (
        <p className="model-selector-description" data-testid="model-description">
          {selectedModel.description}
        </p>
      )}
    </div>
  );
}

/**
 * Export available models for backward compatibility and testing.
 * Note: This is now a function that returns the default models.
 * For the actual models, use the ModelSelector component which fetches from API.
 */
export const AVAILABLE_MODELS = DEFAULT_MODELS;

export default ModelSelector;
