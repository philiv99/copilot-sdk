/**
 * Tests for the usePromptRefinement hook.
 */
import { renderHook, act, waitFor } from '@testing-library/react';
import { usePromptRefinement } from './usePromptRefinement';
import * as copilotApi from '../api/copilotApi';

// Mock the API module
jest.mock('../api/copilotApi');
const mockedApi = copilotApi as jest.Mocked<typeof copilotApi>;

describe('usePromptRefinement', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have correct initial values', () => {
      const { result } = renderHook(() => usePromptRefinement());

      expect(result.current.isRefining).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.iterationCount).toBe(0);
      expect(result.current.lastResponse).toBeNull();
      expect(result.current.previousContent).toBeNull();
      expect(result.current.canUndo).toBe(false);
    });
  });

  describe('refine function', () => {
    it('should return refined content on successful refinement', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: 'Refined content here',
        originalContent: 'Original content',
        iterationCount: 1,
        success: true,
      });

      const { result } = renderHook(() => usePromptRefinement());

      let refinedContent: string | null = null;
      await act(async () => {
        refinedContent = await result.current.refine('Original content');
      });

      expect(refinedContent).toBe('Refined content here');
      expect(result.current.isRefining).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.iterationCount).toBe(1);
      expect(result.current.lastResponse).toEqual({
        refinedContent: 'Refined content here',
        originalContent: 'Original content',
        iterationCount: 1,
        success: true,
      });
      expect(result.current.previousContent).toBe('Original content');
      expect(result.current.canUndo).toBe(true);
    });

    it('should set isRefining to true while refining', async () => {
      let resolvePromise: (value: any) => void;
      mockedApi.refineSystemMessage.mockReturnValue(
        new Promise((resolve) => {
          resolvePromise = resolve;
        })
      );

      const { result } = renderHook(() => usePromptRefinement());

      act(() => {
        result.current.refine('Test content');
      });

      expect(result.current.isRefining).toBe(true);

      await act(async () => {
        resolvePromise!({
          refinedContent: 'Refined',
          originalContent: 'Test content',
          iterationCount: 1,
          success: true,
        });
      });

      expect(result.current.isRefining).toBe(false);
    });

    it('should return null and set error when refinement fails', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Original content',
        iterationCount: 0,
        success: false,
        errorMessage: 'Copilot client is not connected',
      });

      const { result } = renderHook(() => usePromptRefinement());

      let refinedContent: string | null = null;
      await act(async () => {
        refinedContent = await result.current.refine('Original content');
      });

      expect(refinedContent).toBeNull();
      // User-friendly error message mapping
      expect(result.current.error).toBe('Copilot is not connected. Please start the Copilot client first.');
      expect(result.current.isRefining).toBe(false);
    });

    it('should handle API errors with user-friendly messages', async () => {
      mockedApi.refineSystemMessage.mockRejectedValue(new Error('Network error'));

      const { result } = renderHook(() => usePromptRefinement());

      let refinedContent: string | null = null;
      await act(async () => {
        refinedContent = await result.current.refine('Original content');
      });

      expect(refinedContent).toBeNull();
      expect(result.current.error).toBe('Network error. Please check your connection and try again.');
      expect(result.current.isRefining).toBe(false);
    });

    it('should return null and set error for empty content', async () => {
      const { result } = renderHook(() => usePromptRefinement());

      let refinedContent: string | null = null;
      await act(async () => {
        refinedContent = await result.current.refine('');
      });

      expect(refinedContent).toBeNull();
      expect(result.current.error).toBe('Content cannot be empty');
      expect(mockedApi.refineSystemMessage).not.toHaveBeenCalled();
    });

    it('should return null and set error for whitespace-only content', async () => {
      const { result } = renderHook(() => usePromptRefinement());

      let refinedContent: string | null = null;
      await act(async () => {
        refinedContent = await result.current.refine('   ');
      });

      expect(refinedContent).toBeNull();
      expect(result.current.error).toBe('Content cannot be empty');
      expect(mockedApi.refineSystemMessage).not.toHaveBeenCalled();
    });

    it('should pass options to the API', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: 'Refined',
        originalContent: 'Test',
        iterationCount: 1,
        success: true,
      });

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('Test', {
          context: 'Building a todo app',
          refinementFocus: 'clarity',
        });
      });

      expect(mockedApi.refineSystemMessage).toHaveBeenCalledWith({
        content: 'Test',
        context: 'Building a todo app',
        refinementFocus: 'clarity',
      });
    });

    it('should accumulate iteration count across multiple refinements', async () => {
      mockedApi.refineSystemMessage
        .mockResolvedValueOnce({
          refinedContent: 'Refined once',
          originalContent: 'Original',
          iterationCount: 1,
          success: true,
        })
        .mockResolvedValueOnce({
          refinedContent: 'Refined twice',
          originalContent: 'Refined once',
          iterationCount: 2,
          success: true,
        });

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('Original');
      });

      expect(result.current.iterationCount).toBe(1);

      await act(async () => {
        await result.current.refine('Refined once');
      });

      expect(result.current.iterationCount).toBe(2);
    });
  });

  describe('clearError function', () => {
    it('should clear the error', async () => {
      mockedApi.refineSystemMessage.mockRejectedValue(new Error('Test error'));

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('content');
      });

      expect(result.current.error).toBe('Test error');

      act(() => {
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });
  });

  describe('reset function', () => {
    it('should reset all state to initial values', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: 'Refined',
        originalContent: 'Original',
        iterationCount: 3,
        success: true,
      });

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('Original');
      });

      expect(result.current.iterationCount).toBe(3);
      expect(result.current.lastResponse).not.toBeNull();
      expect(result.current.previousContent).toBe('Original');
      expect(result.current.canUndo).toBe(true);

      act(() => {
        result.current.reset();
      });

      expect(result.current.isRefining).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.iterationCount).toBe(0);
      expect(result.current.lastResponse).toBeNull();
      expect(result.current.previousContent).toBeNull();
      expect(result.current.canUndo).toBe(false);
    });
  });

  describe('cancel function', () => {
    it('should cancel in-progress refinement', async () => {
      let resolvePromise: (value: any) => void;
      mockedApi.refineSystemMessage.mockReturnValue(
        new Promise((resolve) => {
          resolvePromise = resolve;
        })
      );

      const { result } = renderHook(() => usePromptRefinement());

      act(() => {
        result.current.refine('Test content');
      });

      expect(result.current.isRefining).toBe(true);

      act(() => {
        result.current.cancel();
      });

      expect(result.current.isRefining).toBe(false);
      expect(result.current.error).toBe('Refinement was cancelled');
    });
  });

  describe('error handling', () => {
    it('should handle non-Error thrown values', async () => {
      mockedApi.refineSystemMessage.mockRejectedValue('String error');

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('content');
      });

      expect(result.current.error).toBe('An unexpected error occurred. Please try again.');
    });

    it('should use default error message when API returns success false without message', async () => {
      mockedApi.refineSystemMessage.mockResolvedValue({
        refinedContent: '',
        originalContent: 'Original',
        iterationCount: 0,
        success: false,
      });

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('Original');
      });

      expect(result.current.error).toBe('Refinement failed. Please try again.');
    });

    it('should map timeout errors to user-friendly messages', async () => {
      mockedApi.refineSystemMessage.mockRejectedValue(new Error('Request timeout'));

      const { result } = renderHook(() => usePromptRefinement());

      await act(async () => {
        await result.current.refine('content');
      });

      expect(result.current.error).toBe('The request timed out. Please try again.');
    });
  });
});
