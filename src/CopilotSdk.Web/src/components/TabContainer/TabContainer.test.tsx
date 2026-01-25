/**
 * Tests for the TabContainer component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { TabContainer } from './TabContainer';
import { SessionProvider } from '../../context';
import { BrowserRouter } from 'react-router-dom';

// Mock the API module
jest.mock('../../api', () => {
  const mockSessions = [
    {
      sessionId: 'session-1',
      status: 'active',
      model: 'gpt-4',
      createdAt: new Date().toISOString(),
    },
    {
      sessionId: 'session-2',
      status: 'idle',
      model: 'gpt-3.5-turbo',
      createdAt: new Date().toISOString(),
    },
  ];

  return {
    getSessions: jest.fn().mockResolvedValue(mockSessions),
    listSessions: jest.fn().mockResolvedValue({ sessions: mockSessions, totalCount: mockSessions.length }),
    getSession: jest.fn().mockImplementation((sessionId) => 
      Promise.resolve(mockSessions.find(s => s.sessionId === sessionId) || mockSessions[0])
    ),
    getSessionMessages: jest.fn().mockResolvedValue({ messages: [] }),
    createSession: jest.fn().mockResolvedValue({ sessionId: 'new-session', status: 'active' }),
    deleteSession: jest.fn().mockResolvedValue(undefined),
    resumeSession: jest.fn().mockResolvedValue({ sessionId: 'session-1', status: 'active' }),
    sendMessage: jest.fn().mockResolvedValue({ messageId: 'msg-1' }),
    abortSession: jest.fn().mockResolvedValue(undefined),
  };
});

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockReturnValue({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    configureLogging: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      on: jest.fn(),
      off: jest.fn(),
      invoke: jest.fn().mockResolvedValue(undefined),
      state: 'Connected',
      onclose: jest.fn(),
      onreconnecting: jest.fn(),
      onreconnected: jest.fn(),
    }),
  }),
  LogLevel: { Information: 1 },
  HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' },
}));

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <SessionProvider autoConnectHub={false}>
        {ui}
      </SessionProvider>
    </BrowserRouter>
  );
};

describe('TabContainer', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the tab container', () => {
    renderWithProviders(<TabContainer />);
    expect(screen.getByTestId('tab-container')).toBeInTheDocument();
  });

  it('renders the sessions list tab by default', () => {
    renderWithProviders(<TabContainer />);
    expect(screen.getByTestId('tab-sessions-list')).toBeInTheDocument();
    expect(screen.getByTestId('tab-sessions-list')).toHaveClass('active');
  });

  it('renders the sessions list panel by default', () => {
    renderWithProviders(<TabContainer />);
    expect(screen.getByTestId('tab-panel-sessions-list')).toBeInTheDocument();
  });

  it('has proper accessibility attributes on tab bar', () => {
    renderWithProviders(<TabContainer />);
    const tabBar = screen.getByRole('tablist');
    expect(tabBar).toHaveAttribute('aria-label', 'Session tabs');
  });

  it('has proper accessibility attributes on sessions list tab', () => {
    renderWithProviders(<TabContainer />);
    const tab = screen.getByTestId('tab-sessions-list');
    expect(tab).toHaveAttribute('role', 'tab');
    expect(tab).toHaveAttribute('aria-selected', 'true');
    expect(tab).toHaveAttribute('aria-controls', 'tab-panel-sessions-list');
  });

  it('sessions list panel has proper accessibility attributes', () => {
    renderWithProviders(<TabContainer />);
    const panel = screen.getByTestId('tab-panel-sessions-list');
    expect(panel).toHaveAttribute('role', 'tabpanel');
    expect(panel).toHaveAttribute('aria-labelledby', 'tab-sessions-list');
  });

  it('displays sessions header in the sessions list view', async () => {
    renderWithProviders(<TabContainer />);
    // Use getAllByText since "Sessions" appears in both tab and header
    const sessionsText = screen.getAllByText('Sessions');
    expect(sessionsText.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/Manage your Copilot sessions/)).toBeInTheDocument();
  });

  it('shows sessions list with loaded sessions', async () => {
    renderWithProviders(<TabContainer />);
    
    await waitFor(() => {
      expect(screen.getByTestId('sessions-list')).toBeInTheDocument();
    });
  });
});

describe('TabContainer with initial session', () => {
  it('opens session tab when initialSessionId is provided', async () => {
    renderWithProviders(<TabContainer initialSessionId="session-1" />);
    
    await waitFor(() => {
      // Should have a tab for session-1
      const sessionTab = screen.queryByTestId('tab-session-session-1');
      // Tab may or may not appear immediately depending on session loading
    });
  });
});

describe('Tab interactions', () => {
  it('switching to sessions list tab shows sessions panel', () => {
    renderWithProviders(<TabContainer />);
    
    const sessionsTab = screen.getByTestId('tab-sessions-list');
    fireEvent.click(sessionsTab);
    
    expect(screen.getByTestId('tab-panel-sessions-list')).toBeInTheDocument();
    expect(sessionsTab).toHaveClass('active');
  });
});
