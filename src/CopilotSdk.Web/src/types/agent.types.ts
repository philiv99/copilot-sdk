/**
 * TypeScript types for agent and team management.
 */

/**
 * Agent definition loaded from docs/agents/{id}/agent.json.
 */
export interface AgentDefinition {
  /** Unique identifier for the agent. */
  id: string;
  /** Display name of the agent. */
  name: string;
  /** Role identifier (e.g., "orchestrator", "coder"). */
  role: string;
  /** Brief description of the agent's purpose. */
  description: string;
  /** Emoji icon for display. */
  icon: string;
  /** Tags for categorization/filtering. */
  tags: string[];
  /** Category: "traditional" or "specialist". */
  category: string;
}

/**
 * Team preset definition loaded from docs/teams/{id}.json.
 */
export interface TeamDefinition {
  /** Unique identifier for the team. */
  id: string;
  /** Display name of the team. */
  name: string;
  /** Brief description of the team's purpose. */
  description: string;
  /** Emoji icon for display. */
  icon: string;
  /** List of agent IDs in this team. */
  agents: string[];
  /** Workflow pattern: "sequential", "parallel", or "hub-spoke". */
  workflowPattern: string;
  /** Human-readable description of the workflow. */
  workflowDescription: string;
}

/**
 * Response containing a list of available agents.
 */
export interface AgentListResponse {
  agents: AgentDefinition[];
}

/**
 * Response containing agent details including prompt content.
 */
export interface AgentDetailResponse {
  /** Agent metadata. */
  agent: AgentDefinition;
  /** The agent's system prompt content (from prompt.md). */
  promptContent: string;
}

/**
 * Response containing a list of available team presets.
 */
export interface TeamListResponse {
  teams: TeamDefinition[];
}

/**
 * Response containing team details with resolved agent definitions.
 */
export interface TeamDetailResponse {
  /** Team definition. */
  team: TeamDefinition;
  /** Resolved agent definitions for agents in this team. */
  resolvedAgents: AgentDefinition[];
}

/**
 * Request to compose a team system message.
 */
export interface ComposeTeamMessageRequest {
  /** Optional system prompt template name to use as the base. */
  templateName?: string;
  /** List of agent IDs to include in the composition. */
  agentIds: string[];
  /** Workflow pattern: "sequential", "parallel", or "hub-spoke". */
  workflowPattern: string;
  /** Optional custom content to append. */
  customContent?: string;
}

/**
 * Response from composing a team system message.
 */
export interface ComposeTeamMessageResponse {
  /** The fully composed system message content. */
  composedContent: string;
  /** Number of agents included. */
  agentCount: number;
  /** The workflow pattern used. */
  workflowPattern: string;
}
