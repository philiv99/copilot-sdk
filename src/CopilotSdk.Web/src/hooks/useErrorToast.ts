/**
 * Custom hook to show toast notifications on errors from contexts.
 */
import { useEffect, useRef } from 'react';
import { useToast } from '../components/Toast';

/**
 * Hook that shows a toast notification when an error occurs.
 * Useful for displaying context errors as toast notifications.
 */
export function useErrorToast(error: string | null, options?: { title?: string; duration?: number }) {
  const toast = useToast();
  const previousErrorRef = useRef<string | null>(null);

  useEffect(() => {
    // Only show toast for new errors (not when error is cleared or same error)
    if (error && error !== previousErrorRef.current) {
      toast.error(error, {
        title: options?.title || 'Error',
        duration: options?.duration || 5000,
      });
    }
    previousErrorRef.current = error;
  }, [error, toast, options?.title, options?.duration]);
}

/**
 * Hook that shows a success toast notification.
 */
export function useSuccessToast() {
  const toast = useToast();
  return (message: string, title?: string) => {
    toast.success(message, { title, duration: 3000 });
  };
}

export default useErrorToast;
