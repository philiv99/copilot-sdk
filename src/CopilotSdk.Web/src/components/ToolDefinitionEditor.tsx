/**
 * Tool definition editor component for creating custom tools.
 */
import React, { useState, useCallback } from 'react';
import { ToolDefinition, ToolParameter } from '../types';
import './ToolDefinitionEditor.css';

/**
 * Props for the ToolDefinitionEditor component.
 */
export interface ToolDefinitionEditorProps {
  /** List of tool definitions. */
  tools: ToolDefinition[];
  /** Callback when tools change. */
  onChange: (tools: ToolDefinition[]) => void;
  /** Whether the editor is disabled. */
  disabled?: boolean;
}

/**
 * Parameter types available for tool parameters.
 */
const PARAMETER_TYPES = ['string', 'number', 'boolean', 'object', 'array'] as const;

/**
 * Empty tool template.
 */
const createEmptyTool = (): ToolDefinition => ({
  name: '',
  description: '',
  parameters: [],
});

/**
 * Empty parameter template.
 */
const createEmptyParameter = (): ToolParameter => ({
  name: '',
  type: 'string',
  description: '',
  required: false,
});

/**
 * Tool definition editor component.
 */
export function ToolDefinitionEditor({ tools, onChange, disabled = false }: ToolDefinitionEditorProps) {
  const [expandedTools, setExpandedTools] = useState<Set<number>>(new Set());

  // Toggle tool expansion
  const toggleExpanded = useCallback((index: number) => {
    setExpandedTools((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(index)) {
        newSet.delete(index);
      } else {
        newSet.add(index);
      }
      return newSet;
    });
  }, []);

  // Add a new tool
  const handleAddTool = useCallback(() => {
    const newTools = [...tools, createEmptyTool()];
    onChange(newTools);
    setExpandedTools((prev) => new Set(prev).add(newTools.length - 1));
  }, [tools, onChange]);

  // Remove a tool
  const handleRemoveTool = useCallback((index: number) => {
    const newTools = tools.filter((_, i) => i !== index);
    onChange(newTools);
    setExpandedTools((prev) => {
      const newSet = new Set<number>();
      prev.forEach((i) => {
        if (i < index) newSet.add(i);
        else if (i > index) newSet.add(i - 1);
      });
      return newSet;
    });
  }, [tools, onChange]);

  // Update a tool
  const handleToolChange = useCallback((index: number, field: keyof ToolDefinition, value: any) => {
    const newTools = [...tools];
    newTools[index] = { ...newTools[index], [field]: value };
    onChange(newTools);
  }, [tools, onChange]);

  // Add a parameter to a tool
  const handleAddParameter = useCallback((toolIndex: number) => {
    const newTools = [...tools];
    const tool = newTools[toolIndex];
    newTools[toolIndex] = {
      ...tool,
      parameters: [...(tool.parameters || []), createEmptyParameter()],
    };
    onChange(newTools);
  }, [tools, onChange]);

  // Remove a parameter from a tool
  const handleRemoveParameter = useCallback((toolIndex: number, paramIndex: number) => {
    const newTools = [...tools];
    const tool = newTools[toolIndex];
    newTools[toolIndex] = {
      ...tool,
      parameters: (tool.parameters || []).filter((_, i) => i !== paramIndex),
    };
    onChange(newTools);
  }, [tools, onChange]);

  // Update a parameter
  const handleParameterChange = useCallback((
    toolIndex: number,
    paramIndex: number,
    field: keyof ToolParameter,
    value: any
  ) => {
    const newTools = [...tools];
    const tool = newTools[toolIndex];
    const params = [...(tool.parameters || [])];
    params[paramIndex] = { ...params[paramIndex], [field]: value };
    newTools[toolIndex] = { ...tool, parameters: params };
    onChange(newTools);
  }, [tools, onChange]);

  return (
    <div className="tool-definition-editor" data-testid="tool-definition-editor">
      <div className="tool-editor-header">
        <h4 className="tool-editor-title">Custom Tools</h4>
        <button
          type="button"
          className="btn btn-sm btn-secondary"
          onClick={handleAddTool}
          disabled={disabled}
          data-testid="add-tool-btn"
        >
          + Add Tool
        </button>
      </div>

      {tools.length === 0 ? (
        <div className="tool-editor-empty">
          <p>No custom tools defined.</p>
          <p className="tool-editor-hint">
            Custom tools allow the AI to execute specific functions during the conversation.
          </p>
        </div>
      ) : (
        <div className="tool-list">
          {tools.map((tool, toolIndex) => (
            <div
              key={toolIndex}
              className={`tool-item ${expandedTools.has(toolIndex) ? 'expanded' : ''}`}
              data-testid={`tool-item-${toolIndex}`}
            >
              <div className="tool-item-header" onClick={() => toggleExpanded(toolIndex)}>
                <span className="tool-expand-icon">{expandedTools.has(toolIndex) ? '▼' : '▶'}</span>
                <span className="tool-item-name">{tool.name || '(Unnamed tool)'}</span>
                <span className="tool-param-count">
                  {tool.parameters?.length || 0} params
                </span>
                <button
                  type="button"
                  className="tool-remove-btn"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleRemoveTool(toolIndex);
                  }}
                  disabled={disabled}
                  title="Remove tool"
                  aria-label="Remove tool"
                >
                  ×
                </button>
              </div>

              {expandedTools.has(toolIndex) && (
                <div className="tool-item-content">
                  <div className="tool-field">
                    <label className="tool-field-label">Name</label>
                    <input
                      type="text"
                      className="tool-field-input"
                      value={tool.name}
                      onChange={(e) => handleToolChange(toolIndex, 'name', e.target.value)}
                      placeholder="e.g., get_weather"
                      disabled={disabled}
                      data-testid={`tool-name-${toolIndex}`}
                    />
                    <span className="tool-field-hint">Unique identifier (snake_case recommended)</span>
                  </div>

                  <div className="tool-field">
                    <label className="tool-field-label">Description</label>
                    <textarea
                      className="tool-field-textarea"
                      value={tool.description}
                      onChange={(e) => handleToolChange(toolIndex, 'description', e.target.value)}
                      placeholder="Describe what this tool does..."
                      disabled={disabled}
                      rows={2}
                      data-testid={`tool-description-${toolIndex}`}
                    />
                    <span className="tool-field-hint">Help the AI understand when to use this tool</span>
                  </div>

                  <div className="tool-parameters">
                    <div className="tool-parameters-header">
                      <span className="tool-parameters-title">Parameters</span>
                      <button
                        type="button"
                        className="btn btn-xs btn-secondary"
                        onClick={() => handleAddParameter(toolIndex)}
                        disabled={disabled}
                        data-testid={`add-param-btn-${toolIndex}`}
                      >
                        + Add
                      </button>
                    </div>

                    {(tool.parameters?.length || 0) === 0 ? (
                      <div className="tool-parameters-empty">No parameters</div>
                    ) : (
                      <div className="tool-parameters-list">
                        {tool.parameters?.map((param, paramIndex) => (
                          <div
                            key={paramIndex}
                            className="tool-parameter-item"
                            data-testid={`param-item-${toolIndex}-${paramIndex}`}
                          >
                            <div className="param-row">
                              <input
                                type="text"
                                className="param-name-input"
                                value={param.name}
                                onChange={(e) =>
                                  handleParameterChange(toolIndex, paramIndex, 'name', e.target.value)
                                }
                                placeholder="Name"
                                disabled={disabled}
                              />
                              <select
                                className="param-type-select"
                                value={param.type}
                                onChange={(e) =>
                                  handleParameterChange(toolIndex, paramIndex, 'type', e.target.value)
                                }
                                disabled={disabled}
                              >
                                {PARAMETER_TYPES.map((type) => (
                                  <option key={type} value={type}>
                                    {type}
                                  </option>
                                ))}
                              </select>
                              <label className="param-required-label">
                                <input
                                  type="checkbox"
                                  checked={param.required}
                                  onChange={(e) =>
                                    handleParameterChange(toolIndex, paramIndex, 'required', e.target.checked)
                                  }
                                  disabled={disabled}
                                />
                                Required
                              </label>
                              <button
                                type="button"
                                className="param-remove-btn"
                                onClick={() => handleRemoveParameter(toolIndex, paramIndex)}
                                disabled={disabled}
                                title="Remove parameter"
                                aria-label="Remove parameter"
                              >
                                ×
                              </button>
                            </div>
                            <input
                              type="text"
                              className="param-description-input"
                              value={param.description}
                              onChange={(e) =>
                                handleParameterChange(toolIndex, paramIndex, 'description', e.target.value)
                              }
                              placeholder="Parameter description"
                              disabled={disabled}
                            />
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default ToolDefinitionEditor;
