/**
 * Tests for the AttachmentsPanel component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { AttachmentsPanel } from './AttachmentsPanel';
import { MessageAttachment } from '../../types';

describe('AttachmentsPanel', () => {
  const mockAttachments: MessageAttachment[] = [
    { type: 'file', displayName: 'test.ts', path: '/path/to/test.ts' },
    { type: 'uri', displayName: 'Example', uri: 'https://example.com' },
  ];

  describe('rendering', () => {
    it('renders the attachments panel', () => {
      render(<AttachmentsPanel attachments={mockAttachments} />);
      expect(screen.getByTestId('attachments-panel')).toBeInTheDocument();
    });

    it('renders nothing when readOnly and no attachments', () => {
      render(<AttachmentsPanel attachments={[]} readOnly={true} />);
      expect(screen.queryByTestId('attachments-panel')).not.toBeInTheDocument();
    });

    it('renders add button when no attachments and not readOnly', () => {
      render(<AttachmentsPanel attachments={[]} />);
      expect(screen.getByText('Add File')).toBeInTheDocument();
    });
  });

  describe('attachment display', () => {
    it('displays attachment names', () => {
      render(<AttachmentsPanel attachments={mockAttachments} />);
      expect(screen.getByText('test.ts')).toBeInTheDocument();
      expect(screen.getByText('Example')).toBeInTheDocument();
    });

    it('displays correct icons for file attachments', () => {
      render(<AttachmentsPanel attachments={[mockAttachments[0]]} />);
      expect(screen.getByText('📄')).toBeInTheDocument();
    });

    it('displays correct icons for URI attachments', () => {
      render(<AttachmentsPanel attachments={[mockAttachments[1]]} />);
      expect(screen.getByText('🔗')).toBeInTheDocument();
    });

    it('displays language when provided', () => {
      const attachmentWithLanguage: MessageAttachment[] = [
        { type: 'file', displayName: 'code.ts', language: 'typescript' },
      ];
      render(<AttachmentsPanel attachments={attachmentWithLanguage} />);
      expect(screen.getByText('typescript')).toBeInTheDocument();
    });

    it('displays line range when provided', () => {
      const attachmentWithLines: MessageAttachment[] = [
        { type: 'file', displayName: 'code.ts', startLine: 10, endLine: 20 },
      ];
      render(<AttachmentsPanel attachments={attachmentWithLines} />);
      expect(screen.getByText('Lines 10-20')).toBeInTheDocument();
    });
  });

  describe('remove functionality', () => {
    it('shows remove buttons when not readOnly', () => {
      const mockOnRemove = jest.fn();
      render(<AttachmentsPanel attachments={mockAttachments} onRemove={mockOnRemove} />);
      
      const removeButtons = screen.getAllByTitle('Remove attachment');
      expect(removeButtons).toHaveLength(2);
    });

    it('does not show remove buttons when readOnly', () => {
      render(<AttachmentsPanel attachments={mockAttachments} readOnly={true} />);
      expect(screen.queryByTitle('Remove attachment')).not.toBeInTheDocument();
    });

    it('calls onRemove with correct index when remove button clicked', () => {
      const mockOnRemove = jest.fn();
      render(<AttachmentsPanel attachments={mockAttachments} onRemove={mockOnRemove} />);
      
      const removeButtons = screen.getAllByTitle('Remove attachment');
      fireEvent.click(removeButtons[0]);
      
      expect(mockOnRemove).toHaveBeenCalledWith(0);
    });
  });

  describe('add functionality', () => {
    it('shows add button when not at max attachments', () => {
      render(<AttachmentsPanel attachments={[]} maxAttachments={5} />);
      expect(screen.getByText('Add File')).toBeInTheDocument();
    });

    it('does not show add button when at max attachments', () => {
      const manyAttachments: MessageAttachment[] = Array(5).fill(null).map((_, i) => ({
        type: 'file',
        displayName: `file${i}.ts`,
      }));
      render(<AttachmentsPanel attachments={manyAttachments} maxAttachments={5} />);
      expect(screen.queryByText('Add File')).not.toBeInTheDocument();
    });

    it('does not show add button when readOnly', () => {
      render(<AttachmentsPanel attachments={[]} readOnly={true} />);
      expect(screen.queryByText('Add File')).not.toBeInTheDocument();
    });

    it('shows attachment count hint', () => {
      render(<AttachmentsPanel attachments={mockAttachments} maxAttachments={10} />);
      expect(screen.getByText('2/10 attachments')).toBeInTheDocument();
    });
  });

  describe('file input', () => {
    it('has hidden file input', () => {
      render(<AttachmentsPanel attachments={[]} />);
      const fileInput = document.querySelector('input[type="file"]');
      expect(fileInput).toHaveClass('file-input-hidden');
    });

    it('file input accepts multiple files', () => {
      render(<AttachmentsPanel attachments={[]} />);
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toHaveAttribute('multiple');
    });
  });
});
