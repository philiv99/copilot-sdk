/**
 * Tests for the ToolDefinitionEditor component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ToolDefinitionEditor } from './ToolDefinitionEditor';
import { ToolDefinition } from '../types';

describe('ToolDefinitionEditor', () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('rendering', () => {
    it('renders the editor', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} />);
      expect(screen.getByTestId('tool-definition-editor')).toBeInTheDocument();
    });

    it('renders title', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} />);
      expect(screen.getByText('Custom Tools')).toBeInTheDocument();
    });

    it('renders add tool button', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} />);
      expect(screen.getByTestId('add-tool-btn')).toBeInTheDocument();
    });

    it('shows empty state when no tools', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} />);
      expect(screen.getByText('No custom tools defined.')).toBeInTheDocument();
    });
  });

  describe('with tools', () => {
    const testTools: ToolDefinition[] = [
      {
        name: 'get_weather',
        description: 'Get current weather',
        parameters: [
          { name: 'city', type: 'string', description: 'City name', required: true },
        ],
      },
      {
        name: 'search',
        description: 'Search the web',
        parameters: [],
      },
    ];

    it('renders all tools', () => {
      render(<ToolDefinitionEditor tools={testTools} onChange={mockOnChange} />);
      expect(screen.getByTestId('tool-item-0')).toBeInTheDocument();
      expect(screen.getByTestId('tool-item-1')).toBeInTheDocument();
    });

    it('shows tool names in headers', () => {
      render(<ToolDefinitionEditor tools={testTools} onChange={mockOnChange} />);
      expect(screen.getByText('get_weather')).toBeInTheDocument();
      expect(screen.getByText('search')).toBeInTheDocument();
    });

    it('shows parameter count in headers', () => {
      render(<ToolDefinitionEditor tools={testTools} onChange={mockOnChange} />);
      expect(screen.getByText('1 params')).toBeInTheDocument();
      expect(screen.getByText('0 params')).toBeInTheDocument();
    });
  });

  describe('adding tools', () => {
    it('adds a new tool when add button clicked', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('add-tool-btn'));
      
      expect(mockOnChange).toHaveBeenCalledWith([
        { name: '', description: '', parameters: [] },
      ]);
    });

    it('adds tool to existing list', () => {
      const existingTool: ToolDefinition = { name: 'existing', description: 'test', parameters: [] };
      render(<ToolDefinitionEditor tools={[existingTool]} onChange={mockOnChange} />);
      
      fireEvent.click(screen.getByTestId('add-tool-btn'));
      
      expect(mockOnChange).toHaveBeenCalledWith([
        existingTool,
        { name: '', description: '', parameters: [] },
      ]);
    });
  });

  describe('removing tools', () => {
    it('removes a tool when remove button clicked', () => {
      const tools: ToolDefinition[] = [
        { name: 'tool1', description: 'desc1', parameters: [] },
        { name: 'tool2', description: 'desc2', parameters: [] },
      ];
      render(<ToolDefinitionEditor tools={tools} onChange={mockOnChange} />);
      
      // Click the first remove button
      const removeButtons = screen.getAllByLabelText('Remove tool');
      fireEvent.click(removeButtons[0]);
      
      expect(mockOnChange).toHaveBeenCalledWith([tools[1]]);
    });
  });

  describe('expanding tools', () => {
    const tool: ToolDefinition = { name: 'test', description: 'test', parameters: [] };

    it('expands tool when header clicked', () => {
      render(<ToolDefinitionEditor tools={[tool]} onChange={mockOnChange} />);
      
      const toolItem = screen.getByTestId('tool-item-0');
      fireEvent.click(toolItem.querySelector('.tool-item-header')!);
      
      expect(screen.getByTestId('tool-name-0')).toBeInTheDocument();
      expect(screen.getByTestId('tool-description-0')).toBeInTheDocument();
    });

    it('collapses tool when header clicked again', () => {
      render(<ToolDefinitionEditor tools={[tool]} onChange={mockOnChange} />);
      
      const toolItem = screen.getByTestId('tool-item-0');
      const header = toolItem.querySelector('.tool-item-header')!;
      
      // Expand
      fireEvent.click(header);
      expect(screen.getByTestId('tool-name-0')).toBeInTheDocument();
      
      // Collapse
      fireEvent.click(header);
      expect(screen.queryByTestId('tool-name-0')).not.toBeInTheDocument();
    });
  });

  describe('editing tools', () => {
    const tool: ToolDefinition = { name: 'test', description: 'desc', parameters: [] };

    const expandTool = () => {
      const toolItem = screen.getByTestId('tool-item-0');
      fireEvent.click(toolItem.querySelector('.tool-item-header')!);
    };

    it('updates tool name', () => {
      render(<ToolDefinitionEditor tools={[tool]} onChange={mockOnChange} />);
      expandTool();
      
      const nameInput = screen.getByTestId('tool-name-0');
      fireEvent.change(nameInput, { target: { value: 'new_name' } });
      
      expect(mockOnChange).toHaveBeenCalledWith([
        { name: 'new_name', description: 'desc', parameters: [] },
      ]);
    });

    it('updates tool description', () => {
      render(<ToolDefinitionEditor tools={[tool]} onChange={mockOnChange} />);
      expandTool();
      
      const descInput = screen.getByTestId('tool-description-0');
      fireEvent.change(descInput, { target: { value: 'new description' } });
      
      expect(mockOnChange).toHaveBeenCalledWith([
        { name: 'test', description: 'new description', parameters: [] },
      ]);
    });
  });

  describe('parameters', () => {
    const tool: ToolDefinition = { name: 'test', description: 'desc', parameters: [] };

    const expandTool = () => {
      const toolItem = screen.getByTestId('tool-item-0');
      fireEvent.click(toolItem.querySelector('.tool-item-header')!);
    };

    it('adds parameter when add button clicked', () => {
      render(<ToolDefinitionEditor tools={[tool]} onChange={mockOnChange} />);
      expandTool();
      
      fireEvent.click(screen.getByTestId('add-param-btn-0'));
      
      expect(mockOnChange).toHaveBeenCalledWith([
        {
          name: 'test',
          description: 'desc',
          parameters: [{ name: '', type: 'string', description: '', required: false }],
        },
      ]);
    });

    it('renders existing parameters', () => {
      const toolWithParam: ToolDefinition = {
        name: 'test',
        description: 'desc',
        parameters: [{ name: 'param1', type: 'string', description: 'A param', required: true }],
      };
      render(<ToolDefinitionEditor tools={[toolWithParam]} onChange={mockOnChange} />);
      expandTool();
      
      expect(screen.getByTestId('param-item-0-0')).toBeInTheDocument();
    });

    it('removes parameter when remove button clicked', () => {
      const toolWithParam: ToolDefinition = {
        name: 'test',
        description: 'desc',
        parameters: [{ name: 'param1', type: 'string', description: 'A param', required: true }],
      };
      render(<ToolDefinitionEditor tools={[toolWithParam]} onChange={mockOnChange} />);
      expandTool();
      
      const removeBtn = screen.getByLabelText('Remove parameter');
      fireEvent.click(removeBtn);
      
      expect(mockOnChange).toHaveBeenCalledWith([
        { name: 'test', description: 'desc', parameters: [] },
      ]);
    });
  });

  describe('disabled state', () => {
    it('disables add tool button', () => {
      render(<ToolDefinitionEditor tools={[]} onChange={mockOnChange} disabled={true} />);
      expect(screen.getByTestId('add-tool-btn')).toBeDisabled();
    });
  });
});
