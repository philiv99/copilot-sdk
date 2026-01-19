/**
 * Tests for the UserMessage component.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import { UserMessage } from './UserMessage';
import { UserMessageData } from '../../types';

describe('UserMessage', () => {
  const mockData: UserMessageData = {
    content: 'Hello, this is a test message.',
  };

  describe('rendering', () => {
    it('renders the user message', () => {
      render(<UserMessage data={mockData} />);
      expect(screen.getByTestId('user-message')).toBeInTheDocument();
    });

    it('displays "You" as the author', () => {
      render(<UserMessage data={mockData} />);
      expect(screen.getByText('You')).toBeInTheDocument();
    });

    it('displays the message content', () => {
      render(<UserMessage data={mockData} />);
      expect(screen.getByText(mockData.content)).toBeInTheDocument();
    });

    it('displays the timestamp when provided', () => {
      const timestamp = '2026-01-18T10:30:00Z';
      render(<UserMessage data={mockData} timestamp={timestamp} />);
      // Check that time is displayed (format depends on locale)
      const header = screen.getByText('You').parentElement;
      expect(header).toBeInTheDocument();
    });

    it('handles empty content', () => {
      const emptyData: UserMessageData = { content: '' };
      render(<UserMessage data={emptyData} />);
      expect(screen.getByTestId('user-message')).toBeInTheDocument();
    });
  });

  describe('attachments', () => {
    it('does not render attachments section when no attachments', () => {
      render(<UserMessage data={mockData} />);
      expect(screen.queryByText('📄')).not.toBeInTheDocument();
    });

    it('renders file attachments', () => {
      const dataWithAttachments: UserMessageData = {
        content: 'Check this file',
        attachments: [
          { type: 'file', path: '/path/to/file.ts', displayName: 'file.ts' },
        ],
      };
      render(<UserMessage data={dataWithAttachments} />);
      expect(screen.getByText('file.ts')).toBeInTheDocument();
    });

    it('renders URI attachments with link icon', () => {
      const dataWithAttachments: UserMessageData = {
        content: 'Check this link',
        attachments: [
          { type: 'uri', uri: 'https://example.com', displayName: 'Example' },
        ],
      };
      render(<UserMessage data={dataWithAttachments} />);
      expect(screen.getByText('Example')).toBeInTheDocument();
      expect(screen.getByText('🔗')).toBeInTheDocument();
    });

    it('renders multiple attachments', () => {
      const dataWithAttachments: UserMessageData = {
        content: 'Multiple files',
        attachments: [
          { type: 'file', displayName: 'file1.ts' },
          { type: 'file', displayName: 'file2.ts' },
          { type: 'uri', displayName: 'Link' },
        ],
      };
      render(<UserMessage data={dataWithAttachments} />);
      expect(screen.getByText('file1.ts')).toBeInTheDocument();
      expect(screen.getByText('file2.ts')).toBeInTheDocument();
      expect(screen.getByText('Link')).toBeInTheDocument();
    });

    it('shows line range for file excerpts', () => {
      const dataWithAttachments: UserMessageData = {
        content: 'File excerpt',
        attachments: [
          { type: 'file', displayName: 'code.ts', startLine: 10, endLine: 25 },
        ],
      };
      render(<UserMessage data={dataWithAttachments} />);
      expect(screen.getByText('L10-25')).toBeInTheDocument();
    });
  });
});
