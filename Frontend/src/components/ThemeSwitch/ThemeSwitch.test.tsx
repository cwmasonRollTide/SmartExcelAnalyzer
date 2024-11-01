import React from 'react';
import '@testing-library/jest-dom';
import ThemeSwitch from './ThemeSwitch';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ThemeModeEnum } from './ThemeMode'; // Update the path to the correct module
import { describe, expect, it, jest } from '@jest/globals';

describe('ThemeSwitch', () => {
  it('renders the theme switch', async () => {
    render(<ThemeSwitch themeMode={{ mode: ThemeModeEnum.LIGHT }} setThemeMode={jest.fn()} />);
    expect(await screen.findByRole('checkbox')).toBeTruthy();
  });

  it('calls onToggle when clicked', async () => {
    const onToggleMock = jest.fn();
    render(<ThemeSwitch themeMode={{ mode: ThemeModeEnum.LIGHT }} setThemeMode={onToggleMock} />);
    fireEvent.click(await screen.findByRole('checkbox'));
    await waitFor(() => expect(onToggleMock).toHaveBeenCalled());
  });
});
