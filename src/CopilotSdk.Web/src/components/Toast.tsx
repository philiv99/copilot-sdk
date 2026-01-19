/**
 * Toast notification component and context for displaying notifications.
 */
import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  ReactNode,
} from 'react';
import './Toast.css';

/**
 * Toast notification types.
 */
export type ToastType = 'info' | 'success' | 'warning' | 'error';

/**
 * Toast notification data.
 */
export interface Toast {
  /** Unique identifier. */
  id: string;
  /** Toast message. */
  message: string;
  /** Toast type. */
  type: ToastType;
  /** Duration in milliseconds (0 for permanent). */
  duration: number;
  /** Optional title. */
  title?: string;
  /** Optional action button. */
  action?: {
    label: string;
    onClick: () => void;
  };
}

/**
 * Options for creating a toast.
 */
export interface ToastOptions {
  /** Toast type. Defaults to 'info'. */
  type?: ToastType;
  /** Duration in milliseconds. Defaults to 5000. */
  duration?: number;
  /** Optional title. */
  title?: string;
  /** Optional action button. */
  action?: {
    label: string;
    onClick: () => void;
  };
}

/**
 * Toast context value.
 */
interface ToastContextValue {
  /** Current toasts. */
  toasts: Toast[];
  /** Add a toast notification. */
  addToast: (message: string, options?: ToastOptions) => string;
  /** Remove a toast by ID. */
  removeToast: (id: string) => void;
  /** Clear all toasts. */
  clearAll: () => void;
  /** Shorthand for info toast. */
  info: (message: string, options?: Omit<ToastOptions, 'type'>) => string;
  /** Shorthand for success toast. */
  success: (message: string, options?: Omit<ToastOptions, 'type'>) => string;
  /** Shorthand for warning toast. */
  warning: (message: string, options?: Omit<ToastOptions, 'type'>) => string;
  /** Shorthand for error toast. */
  error: (message: string, options?: Omit<ToastOptions, 'type'>) => string;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

/**
 * Generate unique ID for toasts.
 */
let toastIdCounter = 0;
function generateToastId(): string {
  return `toast-${++toastIdCounter}-${Date.now()}`;
}

/**
 * Get icon for toast type.
 */
function getToastIcon(type: ToastType): string {
  switch (type) {
    case 'info':
      return 'ℹ️';
    case 'success':
      return '✅';
    case 'warning':
      return '⚠️';
    case 'error':
      return '❌';
  }
}

/**
 * Single toast item component.
 */
function ToastItem({
  toast,
  onDismiss,
}: {
  toast: Toast;
  onDismiss: (id: string) => void;
}) {
  const [isExiting, setIsExiting] = useState(false);

  // Handle auto-dismiss
  useEffect(() => {
    if (toast.duration === 0) return;

    const timer = setTimeout(() => {
      setIsExiting(true);
    }, toast.duration - 300); // Start exit animation 300ms before removal

    const removeTimer = setTimeout(() => {
      onDismiss(toast.id);
    }, toast.duration);

    return () => {
      clearTimeout(timer);
      clearTimeout(removeTimer);
    };
  }, [toast.id, toast.duration, onDismiss]);

  const handleDismiss = () => {
    setIsExiting(true);
    setTimeout(() => {
      onDismiss(toast.id);
    }, 300);
  };

  return (
    <div
      className={`toast toast-${toast.type} ${isExiting ? 'toast-exit' : ''}`}
      role="alert"
      aria-live="assertive"
      data-testid="toast"
    >
      <div className="toast-icon">{getToastIcon(toast.type)}</div>
      <div className="toast-content">
        {toast.title && <div className="toast-title">{toast.title}</div>}
        <div className="toast-message">{toast.message}</div>
        {toast.action && (
          <button
            type="button"
            className="toast-action"
            onClick={() => {
              toast.action?.onClick();
              handleDismiss();
            }}
          >
            {toast.action.label}
          </button>
        )}
      </div>
      <button
        type="button"
        className="toast-dismiss"
        onClick={handleDismiss}
        aria-label="Dismiss notification"
      >
        ×
      </button>
    </div>
  );
}

/**
 * Toast container component.
 */
function ToastContainer({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: string) => void }) {
  if (toasts.length === 0) return null;

  return (
    <div className="toast-container" data-testid="toast-container" role="region" aria-label="Notifications">
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  );
}

/**
 * Props for the ToastProvider component.
 */
export interface ToastProviderProps {
  children: ReactNode;
  /** Maximum number of toasts to display. */
  maxToasts?: number;
}

/**
 * Toast provider component.
 */
export function ToastProvider({ children, maxToasts = 5 }: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const addToast = useCallback(
    (message: string, options: ToastOptions = {}): string => {
      const id = generateToastId();
      const toast: Toast = {
        id,
        message,
        type: options.type || 'info',
        duration: options.duration ?? 5000,
        title: options.title,
        action: options.action,
      };

      setToasts((prev) => {
        const updated = [...prev, toast];
        // Remove oldest if we exceed max
        if (updated.length > maxToasts) {
          return updated.slice(-maxToasts);
        }
        return updated;
      });

      return id;
    },
    [maxToasts]
  );

  const clearAll = useCallback(() => {
    setToasts([]);
  }, []);

  // Shorthand methods
  const info = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => addToast(message, { ...options, type: 'info' }),
    [addToast]
  );

  const success = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => addToast(message, { ...options, type: 'success' }),
    [addToast]
  );

  const warning = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => addToast(message, { ...options, type: 'warning' }),
    [addToast]
  );

  const error = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => addToast(message, { ...options, type: 'error' }),
    [addToast]
  );

  const value: ToastContextValue = {
    toasts,
    addToast,
    removeToast,
    clearAll,
    info,
    success,
    warning,
    error,
  };

  return (
    <ToastContext.Provider value={value}>
      {children}
      <ToastContainer toasts={toasts} onDismiss={removeToast} />
    </ToastContext.Provider>
  );
}

/**
 * Hook to access toast functionality.
 */
export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
}

export default ToastProvider;
