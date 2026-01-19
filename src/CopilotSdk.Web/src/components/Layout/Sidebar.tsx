/**
 * Sidebar component for navigation.
 */
import React, { useState, useCallback, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useSession } from '../../context';
import { SessionsList } from '../SessionsList';
import { CreateSessionModal } from '../CreateSessionModal';
import './Sidebar.css';

/**
 * Navigation item definition.
 */
export interface NavItem {
  /** Route path. */
  path: string;
  /** Display label. */
  label: string;
  /** Icon (optional). */
  icon?: React.ReactNode;
}

/**
 * Props for the Sidebar component.
 */
export interface SidebarProps {
  /** Navigation items. */
  navItems?: NavItem[];
  /** Whether the sidebar is open (for mobile). */
  isOpen?: boolean;
  /** Callback to close the sidebar (for mobile). */
  onClose?: () => void;
}

/**
 * Default navigation items.
 */
const defaultNavItems: NavItem[] = [
  { path: '/', label: 'Dashboard', icon: '📊' },
  { path: '/config', label: 'Client Config', icon: '⚙️' },
  { path: '/sessions', label: 'Sessions', icon: '💬' },
];

/**
 * Sidebar component with navigation and session list.
 */
export function Sidebar({ navItems = defaultNavItems, isOpen = false, onClose }: SidebarProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { createSession, isLoading } = useSession();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Close sidebar on route change (mobile)
  useEffect(() => {
    if (isOpen && onClose) {
      onClose();
    }
  }, [location.pathname, isOpen, onClose]);

  const handleCreateClick = useCallback(() => {
    setIsCreateModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsCreateModalOpen(false);
  }, []);

  const handleSessionCreated = useCallback((sessionId: string) => {
    setIsCreateModalOpen(false);
    navigate(`/sessions/${sessionId}`);
  }, [navigate]);

  const handleNavClick = useCallback(() => {
    // Close sidebar on navigation (mobile)
    if (onClose) {
      onClose();
    }
  }, [onClose]);

  return (
    <>
      <aside 
        className={`app-sidebar ${isOpen ? 'open' : ''}`} 
        data-testid="app-sidebar"
        role="navigation"
        aria-label="Main navigation"
      >
        {/* Close button for mobile */}
        {onClose && (
          <button
            type="button"
            className="sidebar-close-btn"
            onClick={onClose}
            aria-label="Close navigation"
          >
            ✕
          </button>
        )}

        <nav className="sidebar-nav">
          <ul className="nav-list" role="menubar">
            {navItems.map((item) => (
              <li key={item.path} className="nav-item" role="none">
                <NavLink
                  to={item.path}
                  className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
                  onClick={handleNavClick}
                  role="menuitem"
                >
                  {item.icon && <span className="nav-icon" aria-hidden="true">{item.icon}</span>}
                  <span className="nav-label">{item.label}</span>
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="sidebar-sessions">
          <SessionsList
            compact={true}
            showCreateButton={true}
            onCreateClick={handleCreateClick}
          />
        </div>
      </aside>

      <CreateSessionModal
        isOpen={isCreateModalOpen}
        onClose={handleModalClose}
        onSessionCreated={handleSessionCreated}
        createSession={createSession}
        isCreating={isLoading}
      />
    </>
  );
}


export default Sidebar;
