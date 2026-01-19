/**
 * Tests for the ToolExecutionCard component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ToolExecutionCard } from './ToolExecutionCard';
import { ToolExecutionStartData, ToolExecutionCompleteData } from '../../types';

describe('ToolExecutionCard', () => {
  const mockStartData: ToolExecutionStartData = {
    toolCallId: 'tool-call-1',
    toolName: 'test_tool',
    arguments: { param1: 'value1', param2: 42 },
    displayName: 'Test Tool',
  };

  const mockCompleteData: ToolExecutionCompleteData = {
    toolCallId: 'tool-call-1',
    toolName: 'test_tool',
    result: { success: true, data: 'result' },
    duration: 150,
  };

  describe('rendering', () => {
    it('renders the tool execution card', () => {
      render(<ToolExecutionCard startData={mockStartData} />);
      expect(screen.getByTestId('tool-execution-card')).toBeInTheDocument();
    });

    it('displays the tool display name', () => {
      render(<ToolExecutionCard startData={mockStartData} />);
      expect(screen.getByText('Test Tool')).toBeInTheDocument();
    });

    it('displays the tool name when no display name', () => {
      const startData = { ...mockStartData, displayName: undefined };
      render(<ToolExecutionCard startData={startData} />);
      expect(screen.getByText('test_tool')).toBeInTheDocument();
    });

    it('sets data-tool-call-id attribute', () => {
      render(<ToolExecutionCard startData={mockStartData} />);
      expect(screen.getByTestId('tool-execution-card')).toHaveAttribute('data-tool-call-id', 'tool-call-1');
    });
  });

  describe('status display', () => {
    it('shows running status when executing', () => {
      render(<ToolExecutionCard startData={mockStartData} isExecuting={true} />);
      expect(screen.getByText('Running...')).toBeInTheDocument();
    });

    it('shows duration when complete', () => {
      render(<ToolExecutionCard startData={mockStartData} completeData={mockCompleteData} />);
      expect(screen.getByText('150ms')).toBeInTheDocument();
    });

    it('shows gear icon when executing', () => {
      render(<ToolExecutionCard startData={mockStartData} isExecuting={true} />);
      expect(screen.getByText('⚙️')).toBeInTheDocument();
    });

    it('shows check icon when completed successfully', () => {
      render(<ToolExecutionCard startData={mockStartData} completeData={mockCompleteData} />);
      expect(screen.getByText('✅')).toBeInTheDocument();
    });

    it('shows error icon when completed with error', () => {
      const errorComplete = { ...mockCompleteData, error: 'Something went wrong' };
      render(<ToolExecutionCard startData={mockStartData} completeData={errorComplete} />);
      expect(screen.getByText('❌')).toBeInTheDocument();
    });
  });

  describe('collapse/expand behavior', () => {
    it('is collapsed by default', () => {
      render(<ToolExecutionCard startData={mockStartData} completeData={mockCompleteData} />);
      expect(screen.queryByText('Arguments')).not.toBeInTheDocument();
    });

    it('expands when header is clicked', () => {
      render(<ToolExecutionCard startData={mockStartData} completeData={mockCompleteData} />);
      
      fireEvent.click(screen.getByRole('button'));
      
      expect(screen.getByText('Arguments')).toBeInTheDocument();
      expect(screen.getByText('Result')).toBeInTheDocument();
    });

    it('shows arguments when expanded', () => {
      render(<ToolExecutionCard startData={mockStartData} />);
      
      fireEvent.click(screen.getByRole('button'));
      
      expect(screen.getByText(/"param1": "value1"/)).toBeInTheDocument();
    });

    it('shows result when expanded and completed', () => {
      render(<ToolExecutionCard startData={mockStartData} completeData={mockCompleteData} />);
      
      fireEvent.click(screen.getByRole('button'));
      
      expect(screen.getByText(/"success": true/)).toBeInTheDocument();
    });

    it('shows error when expanded and failed', () => {
      const errorComplete = { ...mockCompleteData, error: 'Tool execution failed', result: undefined };
      render(<ToolExecutionCard startData={mockStartData} completeData={errorComplete} />);
      
      fireEvent.click(screen.getByRole('button'));
      
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByText('Tool execution failed')).toBeInTheDocument();
    });
  });

  describe('styling', () => {
    it('has executing class when executing', () => {
      render(<ToolExecutionCard startData={mockStartData} isExecuting={true} />);
      expect(screen.getByTestId('tool-execution-card')).toHaveClass('executing');
    });

    it('has error class when completed with error', () => {
      const errorComplete = { ...mockCompleteData, error: 'Error' };
      render(<ToolExecutionCard startData={mockStartData} completeData={errorComplete} />);
      expect(screen.getByTestId('tool-execution-card')).toHaveClass('error');
    });
  });
});
