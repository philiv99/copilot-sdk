/**
 * Route guard component that checks user role before rendering children.
 */
import React from 'react';
import { Navigate } from 'react-router-dom';
import { useUser } from '../../context/UserContext';
import { UserRole } from '../../types/user.types';

interface ProtectedRouteProps {
  children: React.ReactNode;
  /** Minimum role required to access this route. */
  requiredRole?: UserRole;
}

/**
 * Protects routes by requiring authentication and optionally a minimum role.
 * Redirects to /login if not authenticated.
 * Shows a forbidden message if insufficient role.
 */
export function ProtectedRoute({ children, requiredRole }: ProtectedRouteProps) {
  const { isAuthenticated, hasRole, state } = useUser();

  if (!state.isInitialized) {
    return (
      <div className="loading-auth" data-testid="auth-loading" role="status">
        <p>Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && !hasRole(requiredRole)) {
    return (
      <div className="forbidden-page" data-testid="forbidden-page" role="alert">
        <h2>Access Denied</h2>
        <p>You do not have permission to access this page.</p>
      </div>
    );
  }

  return <>{children}</>;
}
