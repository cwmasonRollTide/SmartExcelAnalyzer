import React from 'react';
import { ThemeMode } from '../../interfaces/ThemeMode';
/**
 * A switch component to toggle the theme mode of the frontend application.
 * Simple translation of the MUI switch component.
 * @param themeMode - The current theme mode
 * @param setThemeMode - The function to set the theme mode
 * @returns
 */
declare const ThemeSwitch: React.FC<{
    themeMode: ThemeMode;
    setThemeMode: (themeMode: ThemeMode) => void;
}>;
export default ThemeSwitch;
