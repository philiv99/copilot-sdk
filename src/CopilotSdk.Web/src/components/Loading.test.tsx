/**
 * Tests for the Loading components.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import {
  Spinner,
  Skeleton,
  LoadingOverlay,
  CardSkeleton,
  TableSkeleton,
  ChatMessageSkeleton,
} from './Loading';

describe('Spinner', () => {
  it('renders spinner with default size', () => {
    render(<Spinner />);
    expect(screen.getByTestId('spinner')).toBeInTheDocument();
    expect(screen.getByTestId('spinner')).toHaveClass('spinner-medium');
  });

  it('renders spinner with small size', () => {
    render(<Spinner size="small" />);
    expect(screen.getByTestId('spinner')).toHaveClass('spinner-small');
  });

  it('renders spinner with large size', () => {
    render(<Spinner size="large" />);
    expect(screen.getByTestId('spinner')).toHaveClass('spinner-large');
  });

  it('has accessibility label', () => {
    render(<Spinner label="Loading data..." />);
    const wrapper = screen.getByRole('status');
    expect(wrapper).toHaveAttribute('aria-label', 'Loading data...');
  });

  it('renders centered when centered prop is true', () => {
    render(<Spinner centered />);
    const wrapper = screen.getByRole('status');
    expect(wrapper).toHaveClass('centered');
  });
});

describe('Skeleton', () => {
  it('renders skeleton with default dimensions', () => {
    render(<Skeleton />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toBeInTheDocument();
    expect(skeleton).toHaveStyle({ width: '100%', height: '1rem' });
  });

  it('renders skeleton with custom dimensions', () => {
    render(<Skeleton width="200px" height="50px" />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveStyle({ width: '200px', height: '50px' });
  });

  it('renders skeleton with numeric dimensions', () => {
    render(<Skeleton width={150} height={30} />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveStyle({ width: '150px', height: '30px' });
  });

  it('renders circular skeleton', () => {
    render(<Skeleton circle width={40} height={40} />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveStyle({ borderRadius: '50%' });
  });

  it('applies custom className', () => {
    render(<Skeleton className="custom-class" />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveClass('custom-class');
  });

  it('is hidden from accessibility tree', () => {
    render(<Skeleton />);
    const skeleton = screen.getByTestId('skeleton');
    expect(skeleton).toHaveAttribute('aria-hidden', 'true');
  });
});

describe('LoadingOverlay', () => {
  it('does not render when isLoading is false', () => {
    render(<LoadingOverlay isLoading={false} />);
    expect(screen.queryByTestId('loading-overlay')).not.toBeInTheDocument();
  });

  it('renders when isLoading is true', () => {
    render(<LoadingOverlay isLoading={true} />);
    expect(screen.getByTestId('loading-overlay')).toBeInTheDocument();
  });

  it('displays custom message', () => {
    render(<LoadingOverlay isLoading={true} message="Saving changes..." />);
    // Message appears in both the p tag and as screen reader text
    const messages = screen.getAllByText('Saving changes...');
    expect(messages.length).toBeGreaterThan(0);
    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuetext', 'Saving changes...');
  });

  it('applies blur class when blur prop is true', () => {
    render(<LoadingOverlay isLoading={true} blur={true} />);
    expect(screen.getByTestId('loading-overlay')).toHaveClass('blur');
  });

  it('does not apply blur class when blur prop is false', () => {
    render(<LoadingOverlay isLoading={true} blur={false} />);
    expect(screen.getByTestId('loading-overlay')).not.toHaveClass('blur');
  });

  it('applies dark class when dark prop is true', () => {
    render(<LoadingOverlay isLoading={true} dark={true} />);
    expect(screen.getByTestId('loading-overlay')).toHaveClass('dark');
  });

  it('has progressbar role', () => {
    render(<LoadingOverlay isLoading={true} message="Loading" />);
    const overlay = screen.getByTestId('loading-overlay');
    expect(overlay).toHaveAttribute('role', 'progressbar');
    expect(overlay).toHaveAttribute('aria-valuetext', 'Loading');
  });
});

describe('CardSkeleton', () => {
  it('renders card skeleton', () => {
    render(<CardSkeleton />);
    expect(screen.getByTestId('card-skeleton')).toBeInTheDocument();
  });

  it('renders specified number of lines', () => {
    render(<CardSkeleton lines={5} />);
    const skeleton = screen.getByTestId('card-skeleton');
    const bodySkeletons = skeleton.querySelectorAll('.card-skeleton-body .skeleton');
    expect(bodySkeletons).toHaveLength(5);
  });

  it('renders avatar when showAvatar is true', () => {
    render(<CardSkeleton showAvatar={true} />);
    const skeleton = screen.getByTestId('card-skeleton');
    const avatarSkeleton = skeleton.querySelector('.card-skeleton-header .skeleton');
    expect(avatarSkeleton).toHaveStyle({ borderRadius: '50%' });
  });

  it('renders action button when showAction is true', () => {
    render(<CardSkeleton showAction={true} />);
    const skeleton = screen.getByTestId('card-skeleton');
    const actions = skeleton.querySelector('.card-skeleton-actions');
    expect(actions).toBeInTheDocument();
  });
});

describe('TableSkeleton', () => {
  it('renders table skeleton', () => {
    render(<TableSkeleton />);
    expect(screen.getByTestId('table-skeleton')).toBeInTheDocument();
  });

  it('renders specified number of rows', () => {
    render(<TableSkeleton rows={3} />);
    const skeleton = screen.getByTestId('table-skeleton');
    const rows = skeleton.querySelectorAll('.table-skeleton-row');
    expect(rows).toHaveLength(3);
  });

  it('renders specified number of columns', () => {
    render(<TableSkeleton columns={6} />);
    const skeleton = screen.getByTestId('table-skeleton');
    const headerCells = skeleton.querySelectorAll('.table-skeleton-header .skeleton');
    expect(headerCells).toHaveLength(6);
  });
});

describe('ChatMessageSkeleton', () => {
  it('renders chat message skeleton', () => {
    render(<ChatMessageSkeleton />);
    expect(screen.getByTestId('chat-message-skeleton')).toBeInTheDocument();
  });

  it('renders assistant message by default', () => {
    render(<ChatMessageSkeleton />);
    const skeleton = screen.getByTestId('chat-message-skeleton');
    expect(skeleton).toHaveClass('assistant');
  });

  it('renders user message when isUser is true', () => {
    render(<ChatMessageSkeleton isUser={true} />);
    const skeleton = screen.getByTestId('chat-message-skeleton');
    expect(skeleton).toHaveClass('user');
  });
});
