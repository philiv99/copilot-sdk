/**
 * TeamConfigTab component — the "Team" tab content in the Create Session modal.
 * Combines TeamSelector, AgentCard grid, and workflow pattern picker.
 */
import React from 'react';
import { AgentDefinition, TeamDefinition } from '../types';
import { TeamSelector } from './TeamSelector';
import { AgentCard } from './AgentCard';
import './TeamConfigTab.css';

export interface TeamConfigTabProps {
  /** Available agents. */
  agents: AgentDefinition[];
  /** Available teams. */
  teams: TeamDefinition[];
  /** Currently selected team ID (null = custom). */
  selectedTeamId: string | null;
  /** Currently selected agent IDs. */
  selectedAgentIds: string[];
  /** Current workflow pattern. */
  workflowPattern: string;
  /** Whether the tab is disabled. */
  disabled?: boolean;
  /** Loading state. */
  loading?: boolean;
  /** Error message. */
  error?: string | null;
  /** Callback when a team is selected. */
  onSelectTeam: (teamId: string | null) => Promise<void>;
  /** Callback when an agent is toggled. */
  onToggleAgent: (agentId: string) => void;
  /** Callback when workflow pattern changes. */
  onWorkflowPatternChange: (pattern: string) => void;
  /** Callback to clear selection. */
  onClearSelection: () => void;
}

const WORKFLOW_PATTERNS = [
  { value: 'sequential', label: 'Sequential', description: 'Agents work in order, each building on the previous' },
  { value: 'parallel', label: 'Parallel', description: 'Agents work simultaneously on their specialties' },
  { value: 'hub-spoke', label: 'Hub & Spoke', description: 'Orchestrator coordinates all other agents' },
];

export function TeamConfigTab({
  agents,
  teams,
  selectedTeamId,
  selectedAgentIds,
  workflowPattern,
  disabled = false,
  loading = false,
  error = null,
  onSelectTeam,
  onToggleAgent,
  onWorkflowPatternChange,
  onClearSelection,
}: TeamConfigTabProps) {
  if (loading) {
    return (
      <div className="team-config-loading" data-testid="team-config-loading">
        <div className="spinner" />
        <span>Loading agents and teams...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="team-config-error" data-testid="team-config-error">
        <span>⚠️ {error}</span>
      </div>
    );
  }

  const traditionalAgents = agents.filter(a => a.category === 'traditional');
  const specialistAgents = agents.filter(a => a.category === 'specialist');

  return (
    <div className="team-config-tab" data-testid="team-tab">
      {/* Team Preset Selector */}
      <TeamSelector
        teams={teams}
        selectedTeamId={selectedTeamId}
        onSelect={(teamId) => { onSelectTeam(teamId); }}
        disabled={disabled}
      />

      {/* Workflow Pattern */}
      <div className="workflow-pattern-section">
        <label className="form-label">Workflow Pattern</label>
        <div className="workflow-pattern-options">
          {WORKFLOW_PATTERNS.map(wp => (
            <label
              key={wp.value}
              className={`workflow-option ${workflowPattern === wp.value ? 'workflow-option-selected' : ''}`}
            >
              <input
                type="radio"
                name="workflowPattern"
                value={wp.value}
                checked={workflowPattern === wp.value}
                onChange={() => onWorkflowPatternChange(wp.value)}
                disabled={disabled}
              />
              <span className="workflow-option-label">{wp.label}</span>
              <span className="workflow-option-desc">{wp.description}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Agent Grid */}
      <div className="agent-grid-section">
        <div className="agent-grid-header">
          <label className="form-label">
            Agents ({selectedAgentIds.length} selected)
          </label>
          {selectedAgentIds.length > 0 && (
            <button
              type="button"
              className="btn btn-sm btn-text"
              onClick={onClearSelection}
              disabled={disabled}
            >
              Clear All
            </button>
          )}
        </div>

        {traditionalAgents.length > 0 && (
          <>
            <h4 className="agent-category-label">Traditional Roles</h4>
            <div className="agent-grid">
              {traditionalAgents.map(agent => (
                <AgentCard
                  key={agent.id}
                  agent={agent}
                  selected={selectedAgentIds.includes(agent.id)}
                  onToggle={onToggleAgent}
                  disabled={disabled}
                />
              ))}
            </div>
          </>
        )}

        {specialistAgents.length > 0 && (
          <>
            <h4 className="agent-category-label">Specialist Roles</h4>
            <div className="agent-grid">
              {specialistAgents.map(agent => (
                <AgentCard
                  key={agent.id}
                  agent={agent}
                  selected={selectedAgentIds.includes(agent.id)}
                  onToggle={onToggleAgent}
                  disabled={disabled}
                />
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export default TeamConfigTab;
