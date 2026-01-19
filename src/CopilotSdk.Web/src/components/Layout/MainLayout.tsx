/**
 * Main layout component that wraps the application.
 */
import React, { useState, useCallback } from 'react';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { StatusBar } from './StatusBar';
import './MainLayout.css';

/**
 * Props for the MainLayout component.
 */
export interface MainLayoutProps {
  /** Main content to display. */
  children: React.ReactNode;
  /** Title for the header. */
  title?: string;
  /** Whether to show the sidebar. */
  showSidebar?: boolean;
  /** Whether to show the status bar. */
  showStatusBar?: boolean;
}

/**
 * Main layout component providing the application shell.
 */
export function MainLayout({
  children,
  title,
  showSidebar = true,
  showStatusBar = true,
}: MainLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const toggleSidebar = useCallback(() => {
    setSidebarOpen((prev) => !prev);
  }, []);

  const closeSidebar = useCallback(() => {
    setSidebarOpen(false);
  }, []);

  return (
    <div className="main-layout" data-testid="main-layout">
      <Header title={title} onMenuClick={toggleSidebar} showMenuButton={showSidebar} />
      <div className="layout-body">
        {showSidebar && (
          <>
            <Sidebar isOpen={sidebarOpen} onClose={closeSidebar} />
            {sidebarOpen && (
              <div 
                className="sidebar-overlay"
                onClick={closeSidebar}
                aria-hidden="true"
              />
            )}
          </>
        )}
        <main 
          className="layout-content" 
          data-testid="layout-content"
          role="main"
          aria-label="Main content"
        >
          {children}
        </main>
      </div>
      {showStatusBar && <StatusBar />}
    </div>
  );
}

export default MainLayout;
