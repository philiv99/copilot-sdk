/**
 * Custom hook for managing prompt refinement state and API calls.
 */
import { useState, useCallback, useRef, useEffect } from 'react';
import { refineSystemMessage } from '../api/copilotApi';
import { RefinePromptRequest, RefinePromptResponse, RefinementFocus } from '../types';

/** Default timeout for refinement requests (30 seconds). */
const DEFAULT_TIMEOUT_MS = 30000;

/**
 * Options for the refine function.
 */
export interface RefineOptions {
  /** Optional additional context about the application being built. */
  context?: string;
  /** Optional focus area for the refinement. */
  refinementFocus?: RefinementFocus;
  /** Timeout in milliseconds for the refinement request. Defaults to 30000ms. */
  timeout?: number;
}

/**
 * State returned by the usePromptRefinement hook.
 */
export interface UsePromptRefinementState {
  /** Whether a refinement request is in progress. */
  isRefining: boolean;
  /** Error message if refinement failed. */
  error: string | null;
  /** Total number of refinement iterations performed. */
  iterationCount: number;
  /** The last successful response from the API. */
  lastResponse: RefinePromptResponse | null;
  /** The original content before the last refinement (for undo). */
  previousContent: string | null;
}

/**
 * Return type for the usePromptRefinement hook.
 */
export interface UsePromptRefinementResult extends UsePromptRefinementState {
  /**
   * Refine the given content.
   * @param content The content to refine.
   * @param options Optional refinement options.
   * @returns The refined content, or null if refinement failed.
   */
  refine: (content: string, options?: RefineOptions) => Promise<string | null>;
  /** Clear the current error. */
  clearError: () => void;
  /** Reset the state to initial values. */
  reset: () => void;
  /** Cancel the current refinement request. */
  cancel: () => void;
  /** Check if undo is available. */
  canUndo: boolean;
}

/**
 * Custom hook for prompt refinement functionality.
 * Manages the state of refinement requests and provides a simple API
 * for refining system message content.
 * 
 * @example
 * ```tsx
 * const { refine, isRefining, error, iterationCount, cancel, canUndo } = usePromptRefinement();
 * 
 * const handleRefine = async () => {
 *   const refined = await refine(content, { refinementFocus: 'clarity' });
 *   if (refined) {
 *     setContent(refined);
 *   }
 * };
 * ```
 */
export function usePromptRefinement(): UsePromptRefinementResult {
  const [isRefining, setIsRefining] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [iterationCount, setIterationCount] = useState(0);
  const [lastResponse, setLastResponse] = useState<RefinePromptResponse | null>(null);
  const [previousContent, setPreviousContent] = useState<string | null>(null);
  
  // Abort controller for cancellation
  const abortControllerRef = useRef<AbortController | null>(null);
  // Track if component is mounted for cleanup
  const isMountedRef = useRef(true);

  // Cleanup on unmount - cancel any in-progress requests
  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  const cancel = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
      setIsRefining(false);
      setError('Refinement was cancelled');
    }
  }, []);

  const refine = useCallback(async (content: string, options?: RefineOptions): Promise<string | null> => {
    // Validate input
    if (!content || content.trim().length === 0) {
      setError('Content cannot be empty');
      return null;
    }

    // Cancel any existing request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new abort controller
    abortControllerRef.current = new AbortController();
    const signal = abortControllerRef.current.signal;

    setIsRefining(true);
    setError(null);

    // Set up timeout
    const timeoutMs = options?.timeout ?? DEFAULT_TIMEOUT_MS;
    const timeoutId = setTimeout(() => {
      if (abortControllerRef.current && !signal.aborted) {
        abortControllerRef.current.abort();
      }
    }, timeoutMs);

    try {
      const request: RefinePromptRequest = {
        content,
        context: options?.context,
        refinementFocus: options?.refinementFocus,
      };

      console.log('[usePromptRefinement] Sending refine request:', { contentLength: content.length, options });

      // Check if aborted before making request
      if (signal.aborted) {
        throw new Error('Request was cancelled');
      }

      console.log('[usePromptRefinement] Calling API...');
      const response = await refineSystemMessage(request);
      console.log('[usePromptRefinement] API response:', { success: response.success, error: response.errorMessage, refinedLength: response.refinedContent?.length });
      
      // Check if still mounted and not cancelled
      if (!isMountedRef.current || signal.aborted) {
        return null;
      }

      if (response.success) {
        setPreviousContent(content);
        setIterationCount(response.iterationCount);
        setLastResponse(response);
        return response.refinedContent;
      } else {
        // Map error messages to user-friendly text
        const userFriendlyError = mapToUserFriendlyError(response.errorMessage);
        setError(userFriendlyError);
        setLastResponse(response);
        return null;
      }
    } catch (err) {
      console.error('[usePromptRefinement] Error:', err);
      // Don't update state if component unmounted or request was intentionally cancelled
      if (!isMountedRef.current) {
        return null;
      }

      const errorMessage = getErrorMessage(err);
      console.log('[usePromptRefinement] Mapped error message:', errorMessage);
      setError(errorMessage);
      return null;
    } finally {
      clearTimeout(timeoutId);
      if (isMountedRef.current) {
        setIsRefining(false);
      }
      abortControllerRef.current = null;
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const reset = useCallback(() => {
    // Cancel any in-progress request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    setIsRefining(false);
    setError(null);
    setIterationCount(0);
    setLastResponse(null);
    setPreviousContent(null);
  }, []);

  return {
    refine,
    isRefining,
    error,
    iterationCount,
    lastResponse,
    previousContent,
    clearError,
    reset,
    cancel,
    canUndo: previousContent !== null,
  };
}

/**
 * Maps API error messages to user-friendly text.
 */
function mapToUserFriendlyError(errorMessage?: string | null): string {
  if (!errorMessage) {
    return 'Refinement failed. Please try again.';
  }

  const lowerError = errorMessage.toLowerCase();

  if (lowerError.includes('not connected')) {
    return 'Copilot is not connected. Please start the Copilot client first.';
  }
  if (lowerError.includes('timeout') || lowerError.includes('timed out')) {
    return 'The request took too long. Please try again with simpler content.';
  }
  if (lowerError.includes('network') || lowerError.includes('fetch')) {
    return 'Network error. Please check your connection and try again.';
  }
  if (lowerError.includes('cancelled') || lowerError.includes('aborted')) {
    return 'Refinement was cancelled.';
  }

  return errorMessage;
}

/**
 * Extracts a user-friendly error message from an error object.
 */
function getErrorMessage(err: unknown): string {
  if (err instanceof Error) {
    if (err.name === 'AbortError' || err.message.includes('aborted')) {
      return 'Refinement was cancelled.';
    }
    if (err.message.includes('timeout') || err.message.includes('Timeout')) {
      return 'The request timed out. Please try again.';
    }
    if (err.message.includes('network') || err.message.includes('Network') || err.message.includes('fetch')) {
      return 'Network error. Please check your connection and try again.';
    }
    return mapToUserFriendlyError(err.message);
  }
  return 'An unexpected error occurred. Please try again.';
}

export default usePromptRefinement;
