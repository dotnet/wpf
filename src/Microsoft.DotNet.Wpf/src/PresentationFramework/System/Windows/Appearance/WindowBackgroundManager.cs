// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Standard;

namespace System.Windows.Appearance;

/// <summary>
/// Facilitates the management of the window background.
/// </summary>
/// <example>
/// <code lang="csharp">
/// WindowBackgroundManager.UpdateBackground(
///     observedWindow.RootVisual,
///     currentApplicationTheme,
///     observedWindow.Backdrop,
///     observedWindow.ForceBackgroundReplace
/// );
/// </code>
/// </example>
internal static class WindowBackgroundManager
{
    /// <summary>
    /// Tries to apply dark theme to <see cref="Window"/>.
    /// </summary>
    public static void ApplyDarkThemeToWindow(Window window)
    {
        if (window is null)
        {
            return;
        }

        if (window.IsLoaded)
        {
            _ = UnsafeNativeMethodsWindow.ApplyUseImmersiveDarkMode(window, true);
        }

        window.Loaded += (sender, _) => UnsafeNativeMethodsWindow.ApplyUseImmersiveDarkMode(sender as Window, true);
    }

    /// <summary>
    /// Tries to remove dark theme from <see cref="Window"/>.
    /// </summary>
    public static void RemoveDarkThemeFromWindow(Window window)
    {
        if (window is null)
        {
            return;
        }

        if (window.IsLoaded)
        {
            _ = UnsafeNativeMethodsWindow.ApplyUseImmersiveDarkMode(window, false);
        }

        window.Loaded += (sender, _) => UnsafeNativeMethodsWindow.ApplyUseImmersiveDarkMode(sender as Window, false);
    }

    /// <summary>
    /// Forces change to application background. Required if custom background effect was previously applied.
    /// </summary>
    public static void UpdateBackground(
        Window window,
        ApplicationTheme applicationTheme,
        WindowBackdropType backdrop,
        bool forceBackground
    )
    {
        if (window is null)
        {
            return;
        }

        if(Utility.IsOSWindows11Insider1OrNewer && window.AllowsTransparency == false)
        {
            _ = WindowBackdrop.RemoveBackdrop(window);

            if (applicationTheme == ApplicationTheme.HighContrast)
            {
                backdrop = WindowBackdropType.None;
            }
            else
            {
                _ = WindowBackdrop.RemoveBackground(window);
            }

            _ = WindowBackdrop.ApplyBackdrop(window, backdrop); 
        }

        if (applicationTheme is ApplicationTheme.Dark)
        {
            ApplyDarkThemeToWindow(window);
        }
        else
        {
            RemoveDarkThemeFromWindow(window);
        }
    }
}
