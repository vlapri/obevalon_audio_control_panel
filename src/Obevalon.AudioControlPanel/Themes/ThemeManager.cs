// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows;
using AppThemeMode = Obevalon.AudioControlPanel.Core.Settings.ThemeMode;
using WpfThemeMode = System.Windows.ThemeMode;

namespace Obevalon.AudioControlPanel.Themes;

/// <summary>
/// Applies the light/dark theme using the WPF Fluent theme. "Auto" follows the Windows app theme
/// and switches automatically when it changes; title bars follow the theme too.
/// </summary>
internal static class ThemeManager
{
    public static void Apply(AppThemeMode mode)
    {
        Application.Current.ThemeMode = mode switch
        {
            AppThemeMode.Light => WpfThemeMode.Light,
            AppThemeMode.Dark => WpfThemeMode.Dark,
            _ => WpfThemeMode.System,
        };
    }
}
