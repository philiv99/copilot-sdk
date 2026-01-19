/**
 * Tests for the Toast notification components.
 */
import React from 'react';
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { ToastProvider, useToast } from './Toast';

// Test component to access toast functions
const TestToastConsumer: React.FC<{ 
  action: 'info' | 'success' | 'warning' | 'error' | 'custom';
  message?: string;
  options?: { title?: string; duration?: number };
}> = ({ action, message = 'Test message', options }) => {
  const toast = useToast();
  
  const handleClick = () => {
    switch (action) {
      case 'info':
        toast.info(message, options);
        break;
      case 'success':
        toast.success(message, options);
        break;
      case 'warning':
        toast.warning(message, options);
        break;
      case 'error':
        toast.error(message, options);
        break;
      case 'custom':
        toast.addToast(message, options);
        break;
    }
  };
  
  return <button onClick={handleClick}>Show Toast</button>;
};

describe('ToastProvider', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders children', () => {
    render(
      <ToastProvider>
        <div>Test content</div>
      </ToastProvider>
    );
    
    expect(screen.getByText('Test content')).toBeInTheDocument();
  });

  it('shows toast when addToast is called', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="info" message="Info message" />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    expect(screen.getByText('Info message')).toBeInTheDocument();
  });

  it('shows success toast', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="success" message="Success!" />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    expect(screen.getByText('Success!')).toBeInTheDocument();
    expect(screen.getByTestId('toast')).toHaveClass('toast-success');
  });

  it('shows warning toast', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="warning" message="Warning!" />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    expect(screen.getByText('Warning!')).toBeInTheDocument();
    expect(screen.getByTestId('toast')).toHaveClass('toast-warning');
  });

  it('shows error toast', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="error" message="Error occurred!" />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    expect(screen.getByText('Error occurred!')).toBeInTheDocument();
    expect(screen.getByTestId('toast')).toHaveClass('toast-error');
  });

  it('shows toast with title', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="info" message="Message" options={{ title: 'Title' }} />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    expect(screen.getByText('Title')).toBeInTheDocument();
    expect(screen.getByText('Message')).toBeInTheDocument();
  });

  it('auto-dismisses toast after duration', async () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="info" message="Auto dismiss" options={{ duration: 1000 }} />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Auto dismiss')).toBeInTheDocument();
    
    act(() => {
      jest.advanceTimersByTime(1500);
    });
    
    await waitFor(() => {
      expect(screen.queryByText('Auto dismiss')).not.toBeInTheDocument();
    });
  });

  it('dismisses toast when dismiss button is clicked', async () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="info" message="Dismissable" options={{ duration: 0 }} />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    expect(screen.getByText('Dismissable')).toBeInTheDocument();
    
    const dismissButton = screen.getByLabelText('Dismiss notification');
    fireEvent.click(dismissButton);
    
    // Wait for animation
    act(() => {
      jest.advanceTimersByTime(500);
    });
    
    await waitFor(() => {
      expect(screen.queryByText('Dismissable')).not.toBeInTheDocument();
    });
  });

  it('respects maxToasts limit', () => {
    render(
      <ToastProvider maxToasts={2}>
        <TestToastConsumer action="info" message="Toast 1" />
      </ToastProvider>
    );
    
    const button = screen.getByText('Show Toast');
    
    // Add 3 toasts
    fireEvent.click(button); // Toast 1
    fireEvent.click(button); // Toast 2
    fireEvent.click(button); // Toast 3 (should remove Toast 1)
    
    const toasts = screen.getAllByTestId('toast');
    expect(toasts).toHaveLength(2);
  });

  it('has correct accessibility attributes', () => {
    render(
      <ToastProvider>
        <TestToastConsumer action="info" message="Accessible toast" />
      </ToastProvider>
    );
    
    fireEvent.click(screen.getByText('Show Toast'));
    
    const toast = screen.getByTestId('toast');
    expect(toast).toHaveAttribute('role', 'alert');
    expect(toast).toHaveAttribute('aria-live', 'assertive');
  });
});

describe('useToast hook', () => {
  it('throws error when used outside provider', () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => {});
    
    const TestComponent = () => {
      useToast();
      return null;
    };
    
    expect(() => render(<TestComponent />)).toThrow(
      'useToast must be used within a ToastProvider'
    );
    
    consoleError.mockRestore();
  });
});
