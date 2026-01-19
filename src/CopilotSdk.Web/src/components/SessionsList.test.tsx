/**
 * Tests for the SessionsList component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { SessionsList } from './SessionsList';
import { SessionInfoResponse } from '../types';

// Mock the context
const mockSelectSession = jest.fn();
const mockDeleteSession = jest.fn();
const mockResumeSession = jest.fn();
const mockRefreshSessions = jest.fn();
const mockClearError = jest.fn();

jest.mock('../context', () => ({
  useSession: () => ({
    sessions: mockSessions,
    activeSessionId: mockActiveSessionId,
    isLoading: mockIsLoading,
    error: mockError,
    selectSession: mockSelectSession,
    deleteSession: mockDeleteSession,
    resumeSession: mockResumeSession,
    refreshSessions: mockRefreshSessions,
    clearError: mockClearError,
  }),
}));

// Mock navigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

// Mock data
let mockSessions: SessionInfoResponse[] = [];
let mockActiveSessionId: string | null = null;
let mockIsLoading = false;
let mockError: string | null = null;

const testSessions: SessionInfoResponse[] = [
  {
    sessionId: 'session-1',
    model: 'gpt-4o',
    streaming: true,
    createdAt: '2026-01-18T10:00:00Z',
    lastActivityAt: '2026-01-18T11:00:00Z',
    status: 'Active',
    messageCount: 5,
  },
  {
    sessionId: 'session-2',
    model: 'gpt-4o-mini',
    streaming: false,
    createdAt: '2026-01-18T09:00:00Z',
    status: 'Idle',
    messageCount: 3,
  },
  {
    sessionId: 'session-3',
    model: 'claude-3.5-sonnet',
    streaming: true,
    createdAt: '2026-01-17T12:00:00Z',
    status: 'Error',
    messageCount: 0,
  },
];

const renderWithRouter = (ui: React.ReactElement) => {
  return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe('SessionsList', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockSessions = [...testSessions];
    mockActiveSessionId = null;
    mockIsLoading = false;
    mockError = null;
  });

  describe('rendering', () => {
    it('renders the sessions list', () => {
      renderWithRouter(<SessionsList />);
      expect(screen.getByTestId('sessions-list')).toBeInTheDocument();
    });

    it('renders all sessions in the table', () => {
      renderWithRouter(<SessionsList />);
      expect(screen.getByTestId('sessions-table')).toBeInTheDocument();
      expect(screen.getByTestId('session-row-session-1')).toBeInTheDocument();
      expect(screen.getByTestId('session-row-session-2')).toBeInTheDocument();
      expect(screen.getByTestId('session-row-session-3')).toBeInTheDocument();
    });

    it('shows empty state when no sessions', () => {
      mockSessions = [];
      renderWithRouter(<SessionsList />);
      expect(screen.getByText('No Sessions')).toBeInTheDocument();
    });

    it('shows loading state', () => {
      mockSessions = [];
      mockIsLoading = true;
      renderWithRouter(<SessionsList />);
      // Loading text appears multiple times (in paragraph and as screen reader label)
      const loadingText = screen.getAllByText('Loading sessions...');
      expect(loadingText.length).toBeGreaterThan(0);
    });

    it('shows error message', () => {
      mockError = 'Failed to load sessions';
      renderWithRouter(<SessionsList />);
      expect(screen.getByText('Failed to load sessions')).toBeInTheDocument();
    });
  });

  describe('compact mode', () => {
    it('renders in compact mode for sidebar', () => {
      renderWithRouter(<SessionsList compact={true} />);
      expect(screen.getByTestId('sessions-list')).toHaveClass('sessions-list-compact');
    });

    it('shows create button when enabled', () => {
      const handleCreateClick = jest.fn();
      renderWithRouter(
        <SessionsList compact={true} showCreateButton={true} onCreateClick={handleCreateClick} />
      );
      const createBtn = screen.getByLabelText('Create new session');
      expect(createBtn).toBeInTheDocument();
    });

    it('calls onCreateClick when create button clicked', () => {
      const handleCreateClick = jest.fn();
      renderWithRouter(
        <SessionsList compact={true} showCreateButton={true} onCreateClick={handleCreateClick} />
      );
      fireEvent.click(screen.getByLabelText('Create new session'));
      expect(handleCreateClick).toHaveBeenCalled();
    });
  });

  describe('session actions', () => {
    it('calls selectSession when session row clicked', async () => {
      mockSelectSession.mockResolvedValue(undefined);
      renderWithRouter(<SessionsList />);
      
      fireEvent.click(screen.getByTestId('session-row-session-1'));
      
      await waitFor(() => {
        expect(mockSelectSession).toHaveBeenCalledWith('session-1');
      });
    });

    it('navigates to session after selection', async () => {
      mockSelectSession.mockResolvedValue(undefined);
      renderWithRouter(<SessionsList />);
      
      fireEvent.click(screen.getByTestId('session-row-session-1'));
      
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/sessions/session-1');
      });
    });

    it('calls refreshSessions when refresh button clicked', async () => {
      mockRefreshSessions.mockResolvedValue(undefined);
      renderWithRouter(<SessionsList />);
      
      fireEvent.click(screen.getByText('Refresh'));
      
      await waitFor(() => {
        expect(mockRefreshSessions).toHaveBeenCalled();
      });
    });

    it('calls deleteSession when delete button clicked and confirmed', async () => {
      mockDeleteSession.mockResolvedValue(undefined);
      window.confirm = jest.fn().mockReturnValue(true);
      
      renderWithRouter(<SessionsList />);
      
      // Find the delete button for session-1
      const deleteButtons = screen.getAllByText('Delete');
      fireEvent.click(deleteButtons[0]);
      
      await waitFor(() => {
        expect(window.confirm).toHaveBeenCalled();
        expect(mockDeleteSession).toHaveBeenCalledWith('session-1');
      });
    });

    it('does not delete when not confirmed', async () => {
      window.confirm = jest.fn().mockReturnValue(false);
      
      renderWithRouter(<SessionsList />);
      
      const deleteButtons = screen.getAllByText('Delete');
      fireEvent.click(deleteButtons[0]);
      
      expect(window.confirm).toHaveBeenCalled();
      expect(mockDeleteSession).not.toHaveBeenCalled();
    });

    it('calls resumeSession when resume button clicked', async () => {
      mockResumeSession.mockResolvedValue({ sessionId: 'session-2' });
      mockSelectSession.mockResolvedValue(undefined);
      
      renderWithRouter(<SessionsList />);
      
      // Find the resume button for session-2 (which is Idle)
      const resumeButtons = screen.getAllByText('Resume');
      fireEvent.click(resumeButtons[1]); // Second session is idle
      
      await waitFor(() => {
        expect(mockResumeSession).toHaveBeenCalledWith('session-2');
      });
    });
  });

  describe('active session highlighting', () => {
    it('highlights the active session row', () => {
      mockActiveSessionId = 'session-1';
      renderWithRouter(<SessionsList />);
      
      const row = screen.getByTestId('session-row-session-1');
      expect(row).toHaveClass('active');
    });

    it('does not highlight inactive session rows', () => {
      mockActiveSessionId = 'session-1';
      renderWithRouter(<SessionsList />);
      
      const row = screen.getByTestId('session-row-session-2');
      expect(row).not.toHaveClass('active');
    });
  });

  describe('error handling', () => {
    it('clears error when dismiss button clicked', () => {
      mockError = 'Test error';
      renderWithRouter(<SessionsList />);
      
      fireEvent.click(screen.getByText('×'));
      
      expect(mockClearError).toHaveBeenCalled();
    });
  });

  describe('create button', () => {
    it('shows create button in full mode', () => {
      const handleCreateClick = jest.fn();
      renderWithRouter(<SessionsList showCreateButton={true} onCreateClick={handleCreateClick} />);
      
      expect(screen.getByText('Create Session')).toBeInTheDocument();
    });

    it('calls onCreateClick when create button clicked in full mode', () => {
      const handleCreateClick = jest.fn();
      renderWithRouter(<SessionsList showCreateButton={true} onCreateClick={handleCreateClick} />);
      
      fireEvent.click(screen.getByText('Create Session'));
      
      expect(handleCreateClick).toHaveBeenCalled();
    });
  });
});
