/**
 * Event Log Panel - Real-time display of session events with filtering and search.
 */
import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { SessionEvent, SessionEventType } from '../types';
import './EventLogPanel.css';

/**
 * Props for the EventLogPanel component.
 */
export interface EventLogPanelProps {
  /** List of events to display. */
  events: SessionEvent[];
  /** Callback when the log is cleared. */
  onClear?: () => void;
  /** Whether the panel is collapsed. */
  collapsed?: boolean;
  /** Callback when collapsed state changes. */
  onToggleCollapse?: () => void;
  /** Maximum height in pixels. */
  maxHeight?: number;
}

/**
 * Event type filter options.
 */
const EVENT_TYPE_FILTERS: { value: SessionEventType | 'all'; label: string; color: string }[] = [
  { value: 'all', label: 'All Events', color: '#9ca3af' },
  { value: 'user.message', label: 'User Messages', color: '#60a5fa' },
  { value: 'assistant.message', label: 'Assistant Messages', color: '#34d399' },
  { value: 'assistant.message_delta', label: 'Streaming', color: '#a78bfa' },
  { value: 'assistant.reasoning', label: 'Reasoning', color: '#fbbf24' },
  { value: 'assistant.reasoning_delta', label: 'Reasoning Stream', color: '#f59e0b' },
  { value: 'tool.execution_start', label: 'Tool Start', color: '#f472b6' },
  { value: 'tool.execution_complete', label: 'Tool Complete', color: '#ec4899' },
  { value: 'assistant.turn_start', label: 'Turn Start', color: '#6ee7b7' },
  { value: 'assistant.turn_end', label: 'Turn End', color: '#10b981' },
  { value: 'assistant.usage', label: 'Usage', color: '#8b5cf6' },
  { value: 'session.start', label: 'Session Start', color: '#14b8a6' },
  { value: 'session.idle', label: 'Session Idle', color: '#94a3b8' },
  { value: 'session.error', label: 'Errors', color: '#ef4444' },
  { value: 'abort', label: 'Abort', color: '#fb923c' },
];

/**
 * Get color for an event type.
 */
function getEventTypeColor(type: SessionEventType): string {
  const filter = EVENT_TYPE_FILTERS.find((f) => f.value === type);
  return filter?.color || '#9ca3af';
}

/**
 * Get icon for an event type.
 */
function getEventTypeIcon(type: SessionEventType): string {
  switch (type) {
    case 'user.message':
      return '👤';
    case 'assistant.message':
      return '🤖';
    case 'assistant.message_delta':
      return '📝';
    case 'assistant.reasoning':
      return '🧠';
    case 'assistant.reasoning_delta':
      return '💭';
    case 'tool.execution_start':
      return '🔧';
    case 'tool.execution_complete':
      return '✅';
    case 'assistant.turn_start':
      return '▶️';
    case 'assistant.turn_end':
      return '⏹️';
    case 'assistant.usage':
      return '📊';
    case 'session.start':
      return '🚀';
    case 'session.idle':
      return '💤';
    case 'session.error':
      return '❌';
    case 'abort':
      return '🛑';
    default:
      return '📌';
  }
}

/**
 * Format timestamp for display.
 */
function formatTimestamp(timestamp: string): string {
  try {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3,
    } as Intl.DateTimeFormatOptions);
  } catch {
    return timestamp;
  }
}

/**
 * Format event data for display.
 */
function formatEventData(event: SessionEvent): string {
  if (!event.data) return '';
  
  try {
    // Extract relevant fields based on event type
    const data = event.data;
    
    switch (event.type) {
      case 'user.message':
        return (data as any).prompt?.substring(0, 100) + ((data as any).prompt?.length > 100 ? '...' : '') || '';
      case 'assistant.message':
        return (data as any).content?.substring(0, 100) + ((data as any).content?.length > 100 ? '...' : '') || '';
      case 'assistant.message_delta':
        return (data as any).deltaContent?.substring(0, 50) + ((data as any).deltaContent?.length > 50 ? '...' : '') || '';
      case 'assistant.reasoning':
        return (data as any).content?.substring(0, 100) + ((data as any).content?.length > 100 ? '...' : '') || '';
      case 'assistant.reasoning_delta':
        return (data as any).deltaContent?.substring(0, 50) + ((data as any).deltaContent?.length > 50 ? '...' : '') || '';
      case 'tool.execution_start':
        return `${(data as any).toolName} - ${(data as any).toolCallId?.substring(0, 8)}`;
      case 'tool.execution_complete':
        const result = (data as any).result;
        return `${(data as any).toolCallId?.substring(0, 8)}: ${typeof result === 'string' ? result.substring(0, 50) : JSON.stringify(result).substring(0, 50)}`;
      case 'assistant.usage':
        return `Prompt: ${(data as any).promptTokens || 0}, Completion: ${(data as any).completionTokens || 0}`;
      case 'session.error':
        return (data as any).error || (data as any).message || '';
      default:
        return JSON.stringify(data).substring(0, 100);
    }
  } catch {
    return '';
  }
}

/**
 * Single event log entry component.
 */
function EventLogEntry({ event }: { event: SessionEvent }) {
  const [expanded, setExpanded] = useState(false);
  const color = getEventTypeColor(event.type);
  const icon = getEventTypeIcon(event.type);
  const summary = formatEventData(event);

  return (
    <div
      className={`event-log-entry ${expanded ? 'expanded' : ''}`}
      style={{ borderLeftColor: color }}
      onClick={() => setExpanded(!expanded)}
      role="button"
      tabIndex={0}
      aria-expanded={expanded}
      onKeyDown={(e) => e.key === 'Enter' && setExpanded(!expanded)}
    >
      <div className="event-log-entry-header">
        <span className="event-icon">{icon}</span>
        <span className="event-time">{formatTimestamp(event.timestamp)}</span>
        <span className="event-type" style={{ color }}>
          {event.type}
        </span>
        {event.ephemeral && <span className="event-ephemeral" title="Ephemeral event">⚡</span>}
        <span className="event-expand-indicator">{expanded ? '▼' : '▶'}</span>
      </div>
      {summary && (
        <div className="event-log-entry-summary">{summary}</div>
      )}
      {expanded && event.data && (
        <div className="event-log-entry-details">
          <pre>{JSON.stringify(event.data, null, 2)}</pre>
        </div>
      )}
    </div>
  );
}

/**
 * Event Log Panel component.
 */
export function EventLogPanel({
  events,
  onClear,
  collapsed = false,
  onToggleCollapse,
  maxHeight = 400,
}: EventLogPanelProps) {
  const [filter, setFilter] = useState<SessionEventType | 'all'>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [autoScroll, setAutoScroll] = useState(true);
  const logContainerRef = useRef<HTMLDivElement>(null);

  // Filter events based on type and search query
  const filteredEvents = useMemo(() => {
    return events.filter((event) => {
      // Type filter
      if (filter !== 'all' && event.type !== filter) {
        return false;
      }

      // Search filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        const typeMatch = event.type.toLowerCase().includes(query);
        const dataMatch = event.data
          ? JSON.stringify(event.data).toLowerCase().includes(query)
          : false;
        return typeMatch || dataMatch;
      }

      return true;
    });
  }, [events, filter, searchQuery]);

  // Auto-scroll to bottom when new events arrive
  useEffect(() => {
    if (autoScroll && logContainerRef.current) {
      logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight;
    }
  }, [filteredEvents, autoScroll]);

  // Handle scroll to detect if user scrolled up
  const handleScroll = useCallback(() => {
    if (!logContainerRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = logContainerRef.current;
    const isAtBottom = scrollHeight - scrollTop - clientHeight < 50;
    setAutoScroll(isAtBottom);
  }, []);

  // Event counts by type
  const eventCounts = useMemo(() => {
    const counts: Record<string, number> = { all: events.length };
    for (const event of events) {
      counts[event.type] = (counts[event.type] || 0) + 1;
    }
    return counts;
  }, [events]);

  if (collapsed) {
    return (
      <div className="event-log-panel collapsed" data-testid="event-log-panel">
        <button
          type="button"
          className="event-log-toggle"
          onClick={onToggleCollapse}
          aria-label="Expand event log"
        >
          📋 Event Log ({events.length})
        </button>
      </div>
    );
  }

  return (
    <div className="event-log-panel" data-testid="event-log-panel">
      {/* Header */}
      <div className="event-log-header">
        <div className="event-log-title">
          <span>📋 Event Log</span>
          <span className="event-count">{filteredEvents.length} / {events.length}</span>
        </div>
        <div className="event-log-actions">
          <button
            type="button"
            className={`auto-scroll-btn ${autoScroll ? 'active' : ''}`}
            onClick={() => setAutoScroll(!autoScroll)}
            title={autoScroll ? 'Auto-scroll enabled' : 'Auto-scroll disabled'}
            aria-pressed={autoScroll}
          >
            ⬇️
          </button>
          {onClear && (
            <button
              type="button"
              className="clear-btn"
              onClick={onClear}
              title="Clear log"
              disabled={events.length === 0}
            >
              🗑️
            </button>
          )}
          {onToggleCollapse && (
            <button
              type="button"
              className="collapse-btn"
              onClick={onToggleCollapse}
              title="Collapse panel"
              aria-label="Collapse event log"
            >
              ➖
            </button>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="event-log-filters">
        <div className="filter-row">
          {/* Search input */}
          <input
            type="text"
            className="event-search"
            placeholder="Search events..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            aria-label="Search events"
          />
          
          {/* Type filter dropdown */}
          <select
            className="event-type-filter"
            value={filter}
            onChange={(e) => setFilter(e.target.value as SessionEventType | 'all')}
            aria-label="Filter by event type"
          >
            {EVENT_TYPE_FILTERS.map(({ value, label }) => (
              <option key={value} value={value}>
                {label} {eventCounts[value] ? `(${eventCounts[value]})` : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Event list */}
      <div
        ref={logContainerRef}
        className="event-log-entries"
        style={{ maxHeight }}
        onScroll={handleScroll}
        role="log"
        aria-live="polite"
        aria-label="Event log entries"
      >
        {filteredEvents.length === 0 ? (
          <div className="event-log-empty">
            {events.length === 0 ? 'No events yet' : 'No events match your filters'}
          </div>
        ) : (
          filteredEvents.map((event) => (
            <EventLogEntry key={event.id} event={event} />
          ))
        )}
      </div>
    </div>
  );
}

export default EventLogPanel;
