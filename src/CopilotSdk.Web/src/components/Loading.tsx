/**
 * Loading components - Spinners, skeletons, and loading overlays.
 */
import React from 'react';
import './Loading.css';

/**
 * Props for the Spinner component.
 */
export interface SpinnerProps {
  /** Size of the spinner. */
  size?: 'small' | 'medium' | 'large';
  /** Optional label for accessibility. */
  label?: string;
  /** Whether to center the spinner. */
  centered?: boolean;
}

/**
 * Spinner component for loading states.
 */
export function Spinner({ size = 'medium', label = 'Loading...', centered = false }: SpinnerProps) {
  return (
    <div className={`spinner-wrapper ${centered ? 'centered' : ''}`} role="status" aria-label={label}>
      <div className={`spinner spinner-${size}`} data-testid="spinner">
        <div className="spinner-ring"></div>
        <div className="spinner-ring"></div>
        <div className="spinner-ring"></div>
        <div className="spinner-ring"></div>
      </div>
      <span className="visually-hidden">{label}</span>
    </div>
  );
}

/**
 * Props for the Skeleton component.
 */
export interface SkeletonProps {
  /** Width of the skeleton. */
  width?: string | number;
  /** Height of the skeleton. */
  height?: string | number;
  /** Border radius. */
  borderRadius?: string | number;
  /** Whether this is a circular skeleton (avatar). */
  circle?: boolean;
  /** Additional class names. */
  className?: string;
}

/**
 * Skeleton loading placeholder component.
 */
export function Skeleton({
  width = '100%',
  height = '1rem',
  borderRadius = '0.25rem',
  circle = false,
  className = '',
}: SkeletonProps) {
  const style: React.CSSProperties = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
    borderRadius: circle ? '50%' : typeof borderRadius === 'number' ? `${borderRadius}px` : borderRadius,
  };

  return (
    <div
      className={`skeleton ${className}`}
      style={style}
      data-testid="skeleton"
      aria-hidden="true"
    />
  );
}

/**
 * Props for the LoadingOverlay component.
 */
export interface LoadingOverlayProps {
  /** Whether the overlay is visible. */
  isLoading: boolean;
  /** Optional loading message. */
  message?: string;
  /** Whether to blur the background. */
  blur?: boolean;
  /** Whether to show a dark overlay. */
  dark?: boolean;
}

/**
 * Full-screen or container loading overlay.
 */
export function LoadingOverlay({
  isLoading,
  message = 'Loading...',
  blur = true,
  dark = true,
}: LoadingOverlayProps) {
  if (!isLoading) return null;

  return (
    <div
      className={`loading-overlay ${blur ? 'blur' : ''} ${dark ? 'dark' : ''}`}
      data-testid="loading-overlay"
      role="progressbar"
      aria-valuetext={message}
    >
      <div className="loading-overlay-content">
        <Spinner size="large" label={message} />
        {message && <p className="loading-overlay-message">{message}</p>}
      </div>
    </div>
  );
}

/**
 * Props for CardSkeleton.
 */
export interface CardSkeletonProps {
  /** Number of lines in the body. */
  lines?: number;
  /** Whether to show an avatar. */
  showAvatar?: boolean;
  /** Whether to show an action button skeleton. */
  showAction?: boolean;
}

/**
 * Pre-built skeleton for card-like content.
 */
export function CardSkeleton({ lines = 3, showAvatar = false, showAction = false }: CardSkeletonProps) {
  return (
    <div className="card-skeleton" data-testid="card-skeleton">
      <div className="card-skeleton-header">
        {showAvatar && <Skeleton width={40} height={40} circle />}
        <div className="card-skeleton-title-area">
          <Skeleton width="60%" height="1rem" />
          <Skeleton width="40%" height="0.75rem" />
        </div>
      </div>
      <div className="card-skeleton-body">
        {Array.from({ length: lines }).map((_, i) => (
          <Skeleton key={i} width={i === lines - 1 ? '70%' : '100%'} height="0.875rem" />
        ))}
      </div>
      {showAction && (
        <div className="card-skeleton-actions">
          <Skeleton width={80} height="2rem" borderRadius="0.375rem" />
        </div>
      )}
    </div>
  );
}

/**
 * Props for TableSkeleton.
 */
export interface TableSkeletonProps {
  /** Number of rows. */
  rows?: number;
  /** Number of columns. */
  columns?: number;
}

/**
 * Pre-built skeleton for table content.
 */
export function TableSkeleton({ rows = 5, columns = 4 }: TableSkeletonProps) {
  return (
    <div className="table-skeleton" data-testid="table-skeleton">
      {/* Header */}
      <div className="table-skeleton-header">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} height="1rem" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="table-skeleton-row">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} height="0.875rem" width={colIndex === 0 ? '80%' : '60%'} />
          ))}
        </div>
      ))}
    </div>
  );
}

/**
 * Chat message skeleton.
 */
export function ChatMessageSkeleton({ isUser = false }: { isUser?: boolean }) {
  return (
    <div className={`chat-message-skeleton ${isUser ? 'user' : 'assistant'}`} data-testid="chat-message-skeleton">
      {!isUser && <Skeleton width={32} height={32} circle />}
      <div className="chat-message-skeleton-content">
        <Skeleton width={isUser ? '60%' : '40%'} height="0.75rem" />
        <Skeleton width={isUser ? '80%' : '95%'} height="0.875rem" />
        <Skeleton width={isUser ? '40%' : '70%'} height="0.875rem" />
      </div>
    </div>
  );
}

export default Spinner;
