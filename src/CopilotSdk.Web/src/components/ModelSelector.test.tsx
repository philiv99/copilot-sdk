/**
 * Tests for the ModelSelector component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ModelSelector, AVAILABLE_MODELS } from './ModelSelector';
import * as copilotApi from '../api/copilotApi';

// Mock the API module
jest.mock('../api/copilotApi', () => ({
  getModels: jest.fn(),
}));

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: jest.fn((key: string) => store[key] || null),
    setItem: jest.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: jest.fn((key: string) => {
      delete store[key];
    }),
    clear: jest.fn(() => {
      store = {};
    }),
  };
})();
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

describe('ModelSelector', () => {
  const mockOnChange = jest.fn();
  const mockGetModels = copilotApi.getModels as jest.Mock;

  const mockModelsResponse = {
    models: [
      { value: 'gpt-4o', label: 'GPT-4o', description: 'Most capable model for complex tasks' },
      { value: 'gpt-4o-mini', label: 'GPT-4o Mini', description: 'Fast and efficient for simpler tasks' },
      { value: 'claude-sonnet-4', label: 'Claude Sonnet 4', description: 'Balanced performance and speed' },
    ],
    cachedAt: new Date().toISOString(),
    expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    localStorageMock.clear();
    mockGetModels.mockResolvedValue(mockModelsResponse);
  });

  describe('rendering', () => {
    it('renders the model selector', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.getByTestId('model-selector')).toBeInTheDocument();
    });

    it('renders label', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.getByText('Model')).toBeInTheDocument();
    });

    it('renders custom label', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} label="Select Model" />);
      expect(screen.getByText('Select Model')).toBeInTheDocument();
    });

    it('renders with default models initially', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      const select = screen.getByTestId('model-select');
      expect(select).toBeInTheDocument();
    });

    it('renders with correct selected value', () => {
      render(<ModelSelector value="gpt-4o-mini" onChange={mockOnChange} />);
      const select = screen.getByTestId('model-select') as HTMLSelectElement;
      expect(select.value).toBe('gpt-4o-mini');
    });
  });

  describe('API fetching', () => {
    it('fetches models from API when no cache exists', async () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      
      await waitFor(() => {
        expect(mockGetModels).toHaveBeenCalledTimes(1);
      });
    });

    it('uses cached models when valid cache exists', async () => {
      const cachedData = {
        models: mockModelsResponse.models,
        cachedAt: Date.now(),
      };
      localStorageMock.getItem.mockReturnValue(JSON.stringify(cachedData));

      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);

      // Give time for useEffect to run
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(mockGetModels).not.toHaveBeenCalled();
    });

    it('fetches models when cache is expired', async () => {
      const expiredCacheData = {
        models: mockModelsResponse.models,
        cachedAt: Date.now() - (8 * 24 * 60 * 60 * 1000), // 8 days ago
      };
      localStorageMock.getItem.mockReturnValue(JSON.stringify(expiredCacheData));

      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);

      await waitFor(() => {
        expect(mockGetModels).toHaveBeenCalledTimes(1);
      });
    });

    it('saves models to cache after fetching', async () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);

      await waitFor(() => {
        expect(localStorageMock.setItem).toHaveBeenCalled();
      });
    });
  });

  describe('description', () => {
    it('does not show description by default', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      expect(screen.queryByTestId('model-description')).not.toBeInTheDocument();
    });

    it('shows description when showDescriptions is true', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} showDescriptions={true} />);
      expect(screen.getByTestId('model-description')).toBeInTheDocument();
    });

    it('shows correct description for selected model', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} showDescriptions={true} />);
      expect(screen.getByText('Most capable model for complex tasks')).toBeInTheDocument();
    });
  });

  describe('interactions', () => {
    it('calls onChange when selection changes', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);
      
      const select = screen.getByTestId('model-select');
      fireEvent.change(select, { target: { value: 'gpt-4o-mini' } });
      
      expect(mockOnChange).toHaveBeenCalledWith('gpt-4o-mini');
    });
  });

  describe('disabled state', () => {
    it('disables the select when disabled prop is true', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('model-select')).toBeDisabled();
    });
  });

  describe('custom className', () => {
    it('applies custom className', () => {
      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} className="custom-class" />);
      expect(screen.getByTestId('model-selector')).toHaveClass('custom-class');
    });
  });

  describe('error handling', () => {
    it('shows error message when API fails', async () => {
      mockGetModels.mockRejectedValue(new Error('API Error'));

      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);

      await waitFor(() => {
        expect(screen.getByTestId('model-error')).toBeInTheDocument();
      });
    });

    it('keeps using default models when API fails', async () => {
      mockGetModels.mockRejectedValue(new Error('API Error'));

      render(<ModelSelector value="gpt-4o" onChange={mockOnChange} />);

      await waitFor(() => {
        expect(screen.getByTestId('model-select')).toBeInTheDocument();
      });

      // Should still have options from default models
      const select = screen.getByTestId('model-select') as HTMLSelectElement;
      expect(select.options.length).toBeGreaterThan(0);
    });
  });

  describe('AVAILABLE_MODELS export', () => {
    it('exports default models for backward compatibility', () => {
      expect(AVAILABLE_MODELS).toBeDefined();
      expect(Array.isArray(AVAILABLE_MODELS)).toBe(true);
      expect(AVAILABLE_MODELS.length).toBeGreaterThan(0);
    });
  });
});
