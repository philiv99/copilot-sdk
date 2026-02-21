/**
 * AgentCard component for displaying an agent in the team selection grid.
 */
import React from 'react';
import { AgentDefinition } from '../types';

export interface AgentCardProps {
  /** Agent definition to display. */
  agent: AgentDefinition;
  /** Whether this agent is selected. */
  selected: boolean;
  /** Callback when card is clicked. */
  onToggle: (agentId: string) => void;
  /** Whether the card is disabled. */
  disabled?: boolean;
}

export function AgentCard({ agent, selected, onToggle, disabled = false }: AgentCardProps) {
  return (
    <button
      type="button"
      className={`agent-card ${selected ? 'agent-card-selected' : ''}`}
      onClick={() => !disabled && onToggle(agent.id)}
      disabled={disabled}
      aria-pressed={selected}
      aria-label={`${agent.name} - ${agent.description}`}
      data-testid={`agent-card-${agent.id}`}
    >
      <div className="agent-card-icon">{agent.icon}</div>
      <div className="agent-card-content">
        <div className="agent-card-name">{agent.name}</div>
        <div className="agent-card-description">{agent.description}</div>
        <div className="agent-card-tags">
          {agent.tags.slice(0, 3).map(tag => (
            <span key={tag} className="agent-card-tag">{tag}</span>
          ))}
        </div>
      </div>
      {selected && <div className="agent-card-check" aria-hidden="true">✓</div>}
    </button>
  );
}

export default AgentCard;
