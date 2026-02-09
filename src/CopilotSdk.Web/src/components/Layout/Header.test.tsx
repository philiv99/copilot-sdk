/**
 * Tests for the Header component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { UserProvider } from '../../context/UserContext';
import { Header } from './Header';

// Mock the userApi module used by UserContext
jest.mock('../../api/userApi', () => ({
  getStoredUserId: jest.fn().mockReturnValue(null),
  setStoredUserId: jest.fn(),
  getCurrentUser: jest.fn().mockRejectedValue(new Error('Not logged in')),
  login: jest.fn(),
  logout: jest.fn(),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <UserProvider>
        {ui}
      </UserProvider>
    </BrowserRouter>
  );
};

describe('Header', () => {
  it('renders with default title', () => {
    renderWithProviders(<Header />);
    expect(screen.getByText('App Maker')).toBeInTheDocument();
  });

  it('renders with custom title', () => {
    renderWithProviders(<Header title="Custom Title" />);
    expect(screen.getByText('Custom Title')).toBeInTheDocument();
  });

  it('has the app-header test id', () => {
    renderWithProviders(<Header />);
    expect(screen.getByTestId('app-header')).toBeInTheDocument();
  });

  it('renders settings button when onSettingsClick is provided', () => {
    const handleClick = jest.fn();
    renderWithProviders(<Header onSettingsClick={handleClick} />);
    expect(screen.getByTestId('settings-button')).toBeInTheDocument();
  });

  it('does not render settings button when onSettingsClick is not provided', () => {
    renderWithProviders(<Header />);
    expect(screen.queryByTestId('settings-button')).not.toBeInTheDocument();
  });

  it('calls onSettingsClick when settings button is clicked', () => {
    const handleClick = jest.fn();
    renderWithProviders(<Header onSettingsClick={handleClick} />);
    
    fireEvent.click(screen.getByTestId('settings-button'));
    
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('has proper accessibility attributes on settings button', () => {
    renderWithProviders(<Header onSettingsClick={jest.fn()} />);
    
    const button = screen.getByTestId('settings-button');
    expect(button).toHaveAttribute('aria-label', 'Open client configuration');
  });
});
