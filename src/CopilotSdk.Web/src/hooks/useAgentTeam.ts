/**
 * Custom hook for managing agent and team state.
 */
import { useState, useEffect, useCallback } from 'react';
import { AgentDefinition, TeamDefinition, TeamDetailResponse, ComposeTeamMessageResponse } from '../types';
import { getAgents, getTeams, getTeamDetail, composeTeamMessage } from '../api/copilotApi';

interface UseAgentTeamState {
  /** All available agents. */
  agents: AgentDefinition[];
  /** All available team presets. */
  teams: TeamDefinition[];
  /** Currently selected team ID. */
  selectedTeamId: string | null;
  /** Currently selected agent IDs. */
  selectedAgentIds: string[];
  /** Current workflow pattern. */
  workflowPattern: string;
  /** Whether agents/teams are loading. */
  loading: boolean;
  /** Error message if any. */
  error: string | null;
  /** Preview of the composed system message. */
  composedPreview: string | null;
  /** Whether a compose preview is loading. */
  previewLoading: boolean;
}

interface UseAgentTeamActions {
  /** Select a team preset (loads its agents). */
  selectTeam: (teamId: string | null) => Promise<void>;
  /** Toggle an individual agent selection. */
  toggleAgent: (agentId: string) => void;
  /** Set the workflow pattern. */
  setWorkflowPattern: (pattern: string) => void;
  /** Clear all selections. */
  clearSelection: () => void;
  /** Preview the composed system message. */
  previewComposition: (templateName?: string, customContent?: string) => Promise<void>;
  /** Refresh agents and teams from the server. */
  refresh: () => Promise<void>;
}

export type UseAgentTeamReturn = UseAgentTeamState & UseAgentTeamActions;

export function useAgentTeam(): UseAgentTeamReturn {
  const [agents, setAgents] = useState<AgentDefinition[]>([]);
  const [teams, setTeams] = useState<TeamDefinition[]>([]);
  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null);
  const [selectedAgentIds, setSelectedAgentIds] = useState<string[]>([]);
  const [workflowPattern, setWorkflowPattern] = useState<string>('sequential');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [composedPreview, setComposedPreview] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [agentResponse, teamResponse] = await Promise.all([
        getAgents(),
        getTeams()
      ]);
      setAgents(agentResponse.agents);
      setTeams(teamResponse.teams);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load agents and teams');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const selectTeam = useCallback(async (teamId: string | null) => {
    setSelectedTeamId(teamId);
    setComposedPreview(null);

    if (!teamId) {
      setSelectedAgentIds([]);
      setWorkflowPattern('sequential');
      return;
    }

    try {
      const detail: TeamDetailResponse = await getTeamDetail(teamId);
      setSelectedAgentIds(detail.team.agents);
      setWorkflowPattern(detail.team.workflowPattern);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load team details');
    }
  }, []);

  const toggleAgent = useCallback((agentId: string) => {
    setSelectedTeamId(null); // Clear team selection when manually toggling agents
    setComposedPreview(null);
    setSelectedAgentIds(prev =>
      prev.includes(agentId)
        ? prev.filter(id => id !== agentId)
        : [...prev, agentId]
    );
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedTeamId(null);
    setSelectedAgentIds([]);
    setWorkflowPattern('sequential');
    setComposedPreview(null);
  }, []);

  const previewComposition = useCallback(async (templateName?: string, customContent?: string) => {
    if (selectedAgentIds.length === 0) {
      setComposedPreview(null);
      return;
    }

    setPreviewLoading(true);
    try {
      const response: ComposeTeamMessageResponse = await composeTeamMessage({
        agentIds: selectedAgentIds,
        workflowPattern,
        templateName,
        customContent,
      });
      setComposedPreview(response.composedContent);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to compose preview');
    } finally {
      setPreviewLoading(false);
    }
  }, [selectedAgentIds, workflowPattern]);

  return {
    agents,
    teams,
    selectedTeamId,
    selectedAgentIds,
    workflowPattern,
    loading,
    error,
    composedPreview,
    previewLoading,
    selectTeam,
    toggleAgent,
    setWorkflowPattern,
    clearSelection,
    previewComposition,
    refresh: loadData,
  };
}
