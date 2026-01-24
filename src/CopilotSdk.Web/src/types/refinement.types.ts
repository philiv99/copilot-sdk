/**
 * TypeScript types for prompt refinement feature.
 */

/**
 * Focus areas for prompt refinement.
 */
export type RefinementFocus = 'clarity' | 'detail' | 'constraints' | 'all';

/**
 * Request to refine a system message prompt.
 */
export interface RefinePromptRequest {
  /** The original content to refine. */
  content: string;
  /** Optional additional context about the application being built. */
  context?: string;
  /** Optional focus area for the refinement. */
  refinementFocus?: RefinementFocus;
}

/**
 * Response from the prompt refinement API.
 */
export interface RefinePromptResponse {
  /** The refined/improved content. */
  refinedContent: string;
  /** The original content that was submitted. */
  originalContent: string;
  /** Number of refinement iterations performed. */
  iterationCount: number;
  /** Whether the refinement was successful. */
  success: boolean;
  /** Error message if refinement failed. */
  errorMessage?: string;
}
