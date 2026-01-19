/**
 * Tests for the ProviderConfigEditor component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ProviderConfigEditor } from './ProviderConfigEditor';
import { ProviderConfig } from '../types';

describe('ProviderConfigEditor', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the editor', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-config-editor')).toBeInTheDocument();
    });

    it('renders toggle checkbox', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-config-toggle')).toBeInTheDocument();
    });

    it('shows toggle unchecked when value is undefined', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-config-toggle')).not.toBeChecked();
    });

    it('shows toggle checked when value is defined', () => {
      const config: ProviderConfig = { type: 'openai' };
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-config-toggle')).toBeChecked();
    });
  });

  describe('when disabled', () => {
    it('hides content area when toggle is unchecked', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} />);
      expect(screen.queryByTestId('provider-type')).not.toBeInTheDocument();
    });
  });

  describe('when enabled', () => {
    const enabledConfig: ProviderConfig = {
      type: 'openai',
      baseUrl: 'https://api.example.com',
      apiKey: 'sk-test',
      wireApi: 'openai',
    };

    it('shows warning message', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByText(/API keys are transmitted/)).toBeInTheDocument();
    });

    it('shows provider type selector', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-type')).toBeInTheDocument();
    });

    it('shows base URL input', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-base-url')).toBeInTheDocument();
    });

    it('shows API key input', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-api-key')).toBeInTheDocument();
    });

    it('shows bearer token input', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-bearer-token')).toBeInTheDocument();
    });

    it('shows wire API selector', () => {
      render(<ProviderConfigEditor value={enabledConfig} onChange={mockOnChange} />);
      expect(screen.getByTestId('provider-wire-api')).toBeInTheDocument();
    });
  });

  describe('toggle interactions', () => {
    it('enables editor when toggle is checked', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('provider-config-toggle'));
      
      expect(mockOnChange).toHaveBeenCalledWith({
        type: 'openai',
        baseUrl: '',
        apiKey: '',
        wireApi: 'openai',
      });
    });

    it('disables editor when toggle is unchecked', () => {
      const config: ProviderConfig = { type: 'openai' };
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('provider-config-toggle'));
      
      expect(mockOnChange).toHaveBeenCalledWith(undefined);
    });
  });

  describe('field updates', () => {
    const config: ProviderConfig = { type: 'openai', baseUrl: '', apiKey: '', wireApi: 'openai' };

    it('updates provider type', () => {
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-type'), { target: { value: 'azure' } });
      
      expect(mockOnChange).toHaveBeenCalledWith(expect.objectContaining({ type: 'azure' }));
    });

    it('updates base URL', () => {
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-base-url'), {
        target: { value: 'https://new-url.com' },
      });
      
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({ baseUrl: 'https://new-url.com' })
      );
    });

    it('updates API key', () => {
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-api-key'), {
        target: { value: 'new-api-key' },
      });
      
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({ apiKey: 'new-api-key' })
      );
    });

    it('updates bearer token', () => {
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-bearer-token'), {
        target: { value: 'new-token' },
      });
      
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({ bearerToken: 'new-token' })
      );
    });

    it('updates wire API', () => {
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-wire-api'), { target: { value: 'azure' } });
      
      expect(mockOnChange).toHaveBeenCalledWith(expect.objectContaining({ wireApi: 'azure' }));
    });

    it('clears optional field to undefined when empty', () => {
      const configWithUrl: ProviderConfig = { ...config, baseUrl: 'https://test.com' };
      render(<ProviderConfigEditor value={configWithUrl} onChange={mockOnChange} />);
      
      fireEvent.change(screen.getByTestId('provider-base-url'), { target: { value: '' } });
      
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({ baseUrl: undefined })
      );
    });
  });

  describe('disabled state', () => {
    it('disables toggle when disabled prop is true', () => {
      render(<ProviderConfigEditor value={undefined} onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('provider-config-toggle')).toBeDisabled();
    });

    it('disables all inputs when disabled prop is true', () => {
      const config: ProviderConfig = { type: 'openai' };
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} disabled={true} />);
      
      expect(screen.getByTestId('provider-type')).toBeDisabled();
      expect(screen.getByTestId('provider-base-url')).toBeDisabled();
      expect(screen.getByTestId('provider-api-key')).toBeDisabled();
    });
  });

  describe('provider types', () => {
    it('renders all provider type options', () => {
      const config: ProviderConfig = { type: 'openai' };
      render(<ProviderConfigEditor value={config} onChange={mockOnChange} />);
      
      const select = screen.getByTestId('provider-type');
      expect(select).toContainHTML('OpenAI');
      expect(select).toContainHTML('Azure OpenAI');
      expect(select).toContainHTML('Anthropic');
      expect(select).toContainHTML('Custom');
    });
  });
});
