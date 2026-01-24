/**
 * RefineButton component for triggering prompt refinement.
 */
import React from 'react';
import './RefineButton.css';

/**
 * Props for the RefineButton component.
 */
export interface RefineButtonProps {
  /** Callback when the button is clicked. */
  onClick: () => void;
  /** Whether refinement is currently in progress. */
  isRefining: boolean;
  /** Whether the button should be disabled (independent of isRefining). */
  disabled?: boolean;
  /** Number of refinement iterations performed (shown as badge). */
  iterationCount?: number;
  /** Optional tooltip text. */
  title?: string;
  /** Optional className for custom styling. */
  className?: string;
}

/**
 * Spinner component for loading state.
 */
function Spinner() {
  return (
    <svg
      className="refine-button-spinner"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
    >
      <circle
        cx="12"
        cy="12"
        r="10"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeDasharray="31.416"
        strokeDashoffset="10"
      />
    </svg>
  );
}

/**
 * Wand icon for the refine button.
 */
function WandIcon() {
  return (
    <svg
      className="refine-button-icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M15 4V2" />
      <path d="M15 16v-2" />
      <path d="M8 9h2" />
      <path d="M20 9h2" />
      <path d="M17.8 11.8L19 13" />
      <path d="M15 9h0" />
      <path d="M17.8 6.2L19 5" />
      <path d="M3 21l9-9" />
      <path d="M12.2 6.2L11 5" />
    </svg>
  );
}

/**
 * A button component for triggering prompt refinement.
 * Shows a loading spinner when refinement is in progress.
 */
export function RefineButton({
  onClick,
  isRefining,
  disabled = false,
  iterationCount,
  title = 'Refine prompt using AI to expand and improve the content',
  className = '',
}: RefineButtonProps) {
  const isDisabled = disabled || isRefining;

  return (
    <button
      type="button"
      className={`refine-button ${isRefining ? 'refining' : ''} ${className}`}
      onClick={onClick}
      disabled={isDisabled}
      title={title}
      aria-label={isRefining ? 'Refining prompt...' : 'Refine prompt'}
      aria-busy={isRefining}
      data-testid="refine-button"
    >
      {isRefining ? <Spinner /> : <WandIcon />}
      <span className="refine-button-text">
        {isRefining ? 'Refining...' : 'Refine'}
      </span>
      {iterationCount !== undefined && iterationCount > 0 && !isRefining && (
        <span 
          className="refine-button-badge" 
          title={`Refined ${iterationCount} time${iterationCount > 1 ? 's' : ''}`}
          data-testid="refine-iteration-badge"
        >
          {iterationCount}
        </span>
      )}
    </button>
  );
}

export default RefineButton;
