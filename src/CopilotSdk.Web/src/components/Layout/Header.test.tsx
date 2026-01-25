/**
 * Tests for the Header component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { Header } from './Header';

describe('Header', () => {
  it('renders with default title', () => {
    render(<Header />);
    expect(screen.getByText('App Maker')).toBeInTheDocument();
  });

  it('renders with custom title', () => {
    render(<Header title="Custom Title" />);
    expect(screen.getByText('Custom Title')).toBeInTheDocument();
  });

  it('has the app-header test id', () => {
    render(<Header />);
    expect(screen.getByTestId('app-header')).toBeInTheDocument();
  });

  it('renders settings button when onSettingsClick is provided', () => {
    const handleClick = jest.fn();
    render(<Header onSettingsClick={handleClick} />);
    expect(screen.getByTestId('settings-button')).toBeInTheDocument();
  });

  it('does not render settings button when onSettingsClick is not provided', () => {
    render(<Header />);
    expect(screen.queryByTestId('settings-button')).not.toBeInTheDocument();
  });

  it('calls onSettingsClick when settings button is clicked', () => {
    const handleClick = jest.fn();
    render(<Header onSettingsClick={handleClick} />);
    
    fireEvent.click(screen.getByTestId('settings-button'));
    
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('has proper accessibility attributes on settings button', () => {
    render(<Header onSettingsClick={jest.fn()} />);
    
    const button = screen.getByTestId('settings-button');
    expect(button).toHaveAttribute('aria-label', 'Open client configuration');
  });
});
