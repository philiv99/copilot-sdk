/**
 * Tests for the EventLogPanel component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { EventLogPanel } from './EventLogPanel';
import { SessionEvent } from '../types';

// Mock events for testing
const createMockEvent = (
  type: SessionEvent['type'],
  id: string,
  data?: Record<string, unknown>
): SessionEvent => ({
  id,
  type,
  timestamp: new Date().toISOString(),
  data: data as SessionEvent['data'],
});

const mockEvents: SessionEvent[] = [
  createMockEvent('user.message', '1', { prompt: 'Hello world' }),
  createMockEvent('assistant.message', '2', { content: 'Hi there!', messageId: 'msg1' }),
  createMockEvent('tool.execution_start', '3', { toolName: 'test_tool', toolCallId: 'call1' }),
  createMockEvent('tool.execution_complete', '4', { toolCallId: 'call1', result: 'success' }),
  createMockEvent('session.error', '5', { error: 'Test error message' }),
];

describe('EventLogPanel', () => {
  it('renders the event log panel', () => {
    render(<EventLogPanel events={mockEvents} />);
    expect(screen.getByTestId('event-log-panel')).toBeInTheDocument();
  });

  it('displays event count', () => {
    render(<EventLogPanel events={mockEvents} />);
    expect(screen.getByText(/5\s*\/\s*5/)).toBeInTheDocument();
  });

  it('renders events', () => {
    render(<EventLogPanel events={mockEvents} />);
    expect(screen.getByText('user.message')).toBeInTheDocument();
    expect(screen.getByText('assistant.message')).toBeInTheDocument();
    expect(screen.getByText('tool.execution_start')).toBeInTheDocument();
  });

  it('shows empty message when no events', () => {
    render(<EventLogPanel events={[]} />);
    expect(screen.getByText('No events yet')).toBeInTheDocument();
  });

  it('filters events by type', () => {
    render(<EventLogPanel events={mockEvents} />);
    
    const filterSelect = screen.getByLabelText('Filter by event type');
    fireEvent.change(filterSelect, { target: { value: 'user.message' } });
    
    expect(screen.getByText('user.message')).toBeInTheDocument();
    expect(screen.queryByText('assistant.message')).not.toBeInTheDocument();
  });

  it('searches events', () => {
    render(<EventLogPanel events={mockEvents} />);
    
    const searchInput = screen.getByLabelText('Search events');
    fireEvent.change(searchInput, { target: { value: 'error' } });
    
    expect(screen.getByText('session.error')).toBeInTheDocument();
    expect(screen.queryByText('user.message')).not.toBeInTheDocument();
  });

  it('expands event to show details', () => {
    render(<EventLogPanel events={mockEvents} />);
    
    const firstEvent = screen.getByText('user.message').closest('.event-log-entry');
    expect(firstEvent).toBeInTheDocument();
    
    fireEvent.click(firstEvent!);
    
    // Should show detailed JSON
    expect(screen.getByText(/"prompt"/)).toBeInTheDocument();
  });

  it('calls onClear when clear button is clicked', () => {
    const handleClear = jest.fn();
    render(<EventLogPanel events={mockEvents} onClear={handleClear} />);
    
    const clearButton = screen.getByTitle('Clear log');
    fireEvent.click(clearButton);
    
    expect(handleClear).toHaveBeenCalled();
  });

  it('toggles collapse state', () => {
    const handleToggle = jest.fn();
    render(
      <EventLogPanel 
        events={mockEvents} 
        collapsed={false} 
        onToggleCollapse={handleToggle} 
      />
    );
    
    const collapseButton = screen.getByTitle('Collapse panel');
    fireEvent.click(collapseButton);
    
    expect(handleToggle).toHaveBeenCalled();
  });

  it('renders collapsed view', () => {
    const handleToggle = jest.fn();
    render(
      <EventLogPanel 
        events={mockEvents} 
        collapsed={true} 
        onToggleCollapse={handleToggle} 
      />
    );
    
    expect(screen.getByText(/Event Log \(5\)/)).toBeInTheDocument();
    // Should not show filter or search when collapsed
    expect(screen.queryByLabelText('Search events')).not.toBeInTheDocument();
  });

  it('toggles auto-scroll', () => {
    render(<EventLogPanel events={mockEvents} />);
    
    const autoScrollButton = screen.getByTitle('Auto-scroll enabled');
    expect(autoScrollButton).toHaveClass('active');
    
    fireEvent.click(autoScrollButton);
    
    expect(screen.getByTitle('Auto-scroll disabled')).not.toHaveClass('active');
  });

  it('shows no match message when filter yields no results', () => {
    render(<EventLogPanel events={mockEvents} />);
    
    const searchInput = screen.getByLabelText('Search events');
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });
    
    expect(screen.getByText("No events match your filters")).toBeInTheDocument();
  });
});
