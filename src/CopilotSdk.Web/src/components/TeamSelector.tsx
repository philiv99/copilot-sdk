/**
 * TeamSelector component for choosing a predefined team preset.
 */
import React from 'react';
import { TeamDefinition } from '../types';

export interface TeamSelectorProps {
  /** Available team presets. */
  teams: TeamDefinition[];
  /** Currently selected team ID. */
  selectedTeamId: string | null;
  /** Callback when a team is selected. */
  onSelect: (teamId: string | null) => void;
  /** Whether the selector is disabled. */
  disabled?: boolean;
}

export function TeamSelector({ teams, selectedTeamId, onSelect, disabled = false }: TeamSelectorProps) {
  return (
    <div className="team-selector" data-testid="team-selector">
      <label className="form-label">Team Preset</label>
      <div className="team-selector-grid">
        <button
          type="button"
          className={`team-card ${selectedTeamId === null ? 'team-card-selected' : ''}`}
          onClick={() => onSelect(null)}
          disabled={disabled}
          aria-pressed={selectedTeamId === null}
          data-testid="team-card-custom"
        >
          <div className="team-card-icon">🛠️</div>
          <div className="team-card-content">
            <div className="team-card-name">Custom Team</div>
            <div className="team-card-description">Pick agents individually below</div>
          </div>
        </button>
        {teams.map(team => (
          <button
            key={team.id}
            type="button"
            className={`team-card ${selectedTeamId === team.id ? 'team-card-selected' : ''}`}
            onClick={() => onSelect(team.id)}
            disabled={disabled}
            aria-pressed={selectedTeamId === team.id}
            data-testid={`team-card-${team.id}`}
          >
            <div className="team-card-icon">{team.icon}</div>
            <div className="team-card-content">
              <div className="team-card-name">{team.name}</div>
              <div className="team-card-description">{team.description}</div>
              <div className="team-card-workflow">
                <span className="workflow-badge">{team.workflowPattern}</span>
                <span className="workflow-agents">{team.agents.length} agents</span>
              </div>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}

export default TeamSelector;
