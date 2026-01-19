/**
 * Tests for the CreateSessionModal component.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { CreateSessionModal } from './CreateSessionModal';

describe('CreateSessionModal', () => {
  const mockOnClose = jest.fn();
  const mockOnSessionCreated = jest.fn();
  const mockCreateSession = jest.fn();

  const defaultProps = {
    isOpen: true,
    onClose: mockOnClose,
    onSessionCreated: mockOnSessionCreated,
    createSession: mockCreateSession,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockCreateSession.mockResolvedValue({ sessionId: 'test-session-id' });
  });

  describe('rendering', () => {
    it('renders the modal when isOpen is true', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('create-session-modal')).toBeInTheDocument();
      expect(screen.getByText('Create New Session')).toBeInTheDocument();
    });

    it('does not render when isOpen is false', () => {
      render(<CreateSessionModal {...defaultProps} isOpen={false} />);
      expect(screen.queryByTestId('create-session-modal')).not.toBeInTheDocument();
    });

    it('renders all tab buttons', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByText('Basic')).toBeInTheDocument();
      expect(screen.getByText('System Message')).toBeInTheDocument();
      expect(screen.getByText('Tools')).toBeInTheDocument();
      expect(screen.getByText('Provider')).toBeInTheDocument();
    });
  });

  describe('tab navigation', () => {
    it('shows basic tab by default', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('basic-tab')).toBeInTheDocument();
    });

    it('switches to system message tab when clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      fireEvent.click(screen.getByText('System Message'));
      expect(screen.getByTestId('system-tab')).toBeInTheDocument();
    });

    it('switches to tools tab when clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      fireEvent.click(screen.getByText('Tools'));
      expect(screen.getByTestId('tools-tab')).toBeInTheDocument();
    });

    it('switches to provider tab when clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      fireEvent.click(screen.getByText('Provider'));
      expect(screen.getByTestId('provider-tab')).toBeInTheDocument();
    });
  });

  describe('basic settings', () => {
    it('renders session ID input', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('session-id-input')).toBeInTheDocument();
    });

    it('renders model selector', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('model-selector')).toBeInTheDocument();
    });

    it('renders streaming checkbox', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('streaming-checkbox')).toBeInTheDocument();
    });

    it('streaming checkbox is checked by default', () => {
      render(<CreateSessionModal {...defaultProps} />);
      expect(screen.getByTestId('streaming-checkbox')).toBeChecked();
    });
  });

  describe('form submission', () => {
    it('creates session with default values', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith({
          model: 'gpt-4o',
          streaming: true,
        });
      });
    });

    it('creates session with custom session ID', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'my-custom-session' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith(
          expect.objectContaining({
            sessionId: 'my-custom-session',
          })
        );
      });
    });

    it('calls onSessionCreated with session ID on success', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockOnSessionCreated).toHaveBeenCalledWith('test-session-id');
      });
    });

    it('shows error message on failure', async () => {
      mockCreateSession.mockRejectedValue(new Error('Network error'));
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(screen.getByTestId('modal-error')).toBeInTheDocument();
        expect(screen.getByText('Network error')).toBeInTheDocument();
      });
    });

    it('disables submit button while creating', async () => {
      mockCreateSession.mockImplementation(() => new Promise(() => {})); // Never resolves
      render(<CreateSessionModal {...defaultProps} isCreating={true} />);
      
      expect(screen.getByTestId('create-session-submit')).toBeDisabled();
    });
  });

  describe('modal interactions', () => {
    it('calls onClose when close button clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByLabelText('Close modal'));
      
      expect(mockOnClose).toHaveBeenCalled();
    });

    it('calls onClose when cancel button clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByText('Cancel'));
      
      expect(mockOnClose).toHaveBeenCalled();
    });

    it('calls onClose when backdrop clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('create-session-modal'));
      
      expect(mockOnClose).toHaveBeenCalled();
    });

    it('does not call onClose when modal content clicked', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByText('Create New Session'));
      
      expect(mockOnClose).not.toHaveBeenCalled();
    });

    it('calls onClose on escape key', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.keyDown(document, { key: 'Escape' });
      
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  describe('streaming toggle', () => {
    it('can toggle streaming off', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('streaming-checkbox'));
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith(
          expect.objectContaining({
            streaming: false,
          })
        );
      });
    });
  });

  describe('model selection', () => {
    it('can change model selection', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const select = screen.getByTestId('model-select');
      fireEvent.change(select, { target: { value: 'gpt-4o-mini' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith(
          expect.objectContaining({
            model: 'gpt-4o-mini',
          })
        );
      });
    });
  });

  describe('system message tab', () => {
    it('renders system message editor when tab selected', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByText('System Message'));
      
      expect(screen.getByTestId('system-message-editor')).toBeInTheDocument();
    });
  });

  describe('tools tab', () => {
    it('renders tool definition editor when tab selected', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByText('Tools'));
      
      expect(screen.getByTestId('tool-definition-editor')).toBeInTheDocument();
    });
  });

  describe('provider tab', () => {
    it('renders provider config editor when tab selected', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByText('Provider'));
      
      expect(screen.getByTestId('provider-config-editor')).toBeInTheDocument();
    });
  });

  describe('loading state', () => {
    it('shows loading text on submit button while creating', () => {
      render(<CreateSessionModal {...defaultProps} isCreating={true} />);
      
      expect(screen.getByText('Creating...')).toBeInTheDocument();
    });

    it('disables close button while creating', () => {
      render(<CreateSessionModal {...defaultProps} isCreating={true} />);
      
      expect(screen.getByLabelText('Close modal')).toBeDisabled();
    });
  });

  describe('session ID validation', () => {
    it('accepts valid session IDs with alphanumeric characters', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'ValidSession123' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith(
          expect.objectContaining({
            sessionId: 'ValidSession123',
          })
        );
      });
    });

    it('accepts session IDs with hyphens and underscores', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'my-session_id-123' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith(
          expect.objectContaining({
            sessionId: 'my-session_id-123',
          })
        );
      });
    });

    it('shows error for session ID with spaces', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'Hello World' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(screen.getByTestId('modal-error')).toBeInTheDocument();
        expect(screen.getByText(/only contain letters, numbers, hyphens/i)).toBeInTheDocument();
      });
      expect(mockCreateSession).not.toHaveBeenCalled();
    });

    it('shows inline validation hint for invalid session ID', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'invalid session!' } });
      
      expect(screen.getByText(/Only letters, numbers, hyphens \(-\), and underscores \(_\) allowed/)).toBeInTheDocument();
    });

    it('shows error for session ID with special characters', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'test@session!' } });
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(screen.getByTestId('modal-error')).toBeInTheDocument();
      });
      expect(mockCreateSession).not.toHaveBeenCalled();
    });

    it('applies error styling to invalid session ID input', () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      const sessionIdInput = screen.getByTestId('session-id-input');
      fireEvent.change(sessionIdInput, { target: { value: 'invalid session' } });
      
      expect(sessionIdInput).toHaveClass('input-error');
    });

    it('allows empty session ID (auto-generated)', async () => {
      render(<CreateSessionModal {...defaultProps} />);
      
      fireEvent.click(screen.getByTestId('create-session-submit'));
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith({
          model: 'gpt-4o',
          streaming: true,
        });
      });
    });
  });
});
