/**
 * ClientConfigModal component - 80% screen coverage modal for client configuration.
 */
import React, { useCallback, useEffect } from 'react';
import { ClientConfigView } from '../../views/ClientConfigView';
import './ClientConfigModal.css';

/**
 * Props for the ClientConfigModal component.
 */
export interface ClientConfigModalProps {
  /** Whether the modal is open. */
  isOpen: boolean;
  /** Callback when the modal should close. */
  onClose: () => void;
}

/**
 * Modal component for displaying client configuration.
 * Takes up 80% of the screen.
 */
export function ClientConfigModal({ isOpen, onClose }: ClientConfigModalProps) {
  // Handle escape key to close modal
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen) {
        onClose();
      }
    },
    [isOpen, onClose]
  );

  // Handle backdrop click
  const handleBackdropClick = useCallback(
    (event: React.MouseEvent<HTMLDivElement>) => {
      if (event.target === event.currentTarget) {
        onClose();
      }
    },
    [onClose]
  );

  // Add event listener for escape key
  useEffect(() => {
    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
      // Prevent body scroll when modal is open
      document.body.style.overflow = 'hidden';
    }
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [isOpen, handleKeyDown]);

  if (!isOpen) {
    return null;
  }

  return (
    <div
      className="client-config-modal-backdrop"
      onClick={handleBackdropClick}
      data-testid="client-config-modal-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="client-config-modal-title"
    >
      <div className="client-config-modal" data-testid="client-config-modal">
        <div className="client-config-modal-header">
          <h2 id="client-config-modal-title">Client Configuration</h2>
          <button
            className="client-config-modal-close"
            onClick={onClose}
            aria-label="Close configuration modal"
            data-testid="client-config-modal-close"
          >
            ×
          </button>
        </div>
        <div className="client-config-modal-content">
          <ClientConfigView isModal={true} />
        </div>
      </div>
    </div>
  );
}

export default ClientConfigModal;
