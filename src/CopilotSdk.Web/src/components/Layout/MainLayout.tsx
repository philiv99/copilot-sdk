/**
 * Main layout component that wraps the application.
 */
import React, { useState, useCallback } from 'react';
import { Header } from './Header';
import { StatusBar } from './StatusBar';
import { ClientConfigModal } from '../ClientConfigModal';
import { TabContainer } from '../TabContainer';
import './MainLayout.css';

/**
 * Props for the MainLayout component.
 */
export interface MainLayoutProps {
  /** Title for the header. */
  title?: string;
  /** Whether to show the status bar. */
  showStatusBar?: boolean;
  /** Initial session ID to open (from URL). */
  initialSessionId?: string;
}

/**
 * Main layout component providing the application shell.
 * Uses a tab-based layout for session management.
 */
export function MainLayout({
  title,
  showStatusBar = true,
  initialSessionId,
}: MainLayoutProps) {
  const [isConfigModalOpen, setIsConfigModalOpen] = useState(false);

  const handleSettingsClick = useCallback(() => {
    setIsConfigModalOpen(true);
  }, []);

  const handleCloseConfigModal = useCallback(() => {
    setIsConfigModalOpen(false);
  }, []);

  return (
    <div className="main-layout" data-testid="main-layout">
      <Header title={title} onSettingsClick={handleSettingsClick} />
      <div className="layout-body">
        <main 
          className="layout-content" 
          data-testid="layout-content"
          role="main"
          aria-label="Main content"
        >
          <TabContainer initialSessionId={initialSessionId} />
        </main>
      </div>
      {showStatusBar && <StatusBar />}
      
      {/* Client Config Modal */}
      <ClientConfigModal
        isOpen={isConfigModalOpen}
        onClose={handleCloseConfigModal}
      />
    </div>
  );
}

export default MainLayout;
