// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

using Standard;
// ReSharper disable once CheckNamespace
namespace System.Windows.Appearance;

/// <summary>
/// Applies the chosen backdrop effect to the selected window.
/// </summary>
internal static class WindowBackdrop
{
    /// <summary>
    /// Checks whether the selected backdrop type is supported on current platform.
    /// </summary>
    /// <returns><see langword="true"/> if the selected backdrop type is supported on current platform.</returns>
    internal static bool IsSupported(WindowBackdropType backdropType)
    {
        return backdropType switch
        {
            WindowBackdropType.Auto => Utility.IsOSWindows11Insider1OrNewer,
            WindowBackdropType.TabbedWindow => Utility.IsOSWindows11Insider1OrNewer,
            WindowBackdropType.MainWindow => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.TransientWindow => Utility.IsOSWindows7OrNewer,
            WindowBackdropType.None => true,
            _ => false
        };
    }

    /// <summary>
    /// Applies backdrop effect to the selected <see cref="System.Windows.Window"/>.
    /// </summary>
    /// <param name="window">Selected window.</param>
    /// <returns><see langword="true"/> if the operation was successfull, otherwise <see langword="false"/>.</returns>
    internal static bool ApplyBackdrop(System.Windows.Window window, WindowBackdropType backdropType)
    {
        if (window is null)
        {
            return false;
        }

        if (window.IsLoaded)
        {
            return ApplyBackdropCore(window, backdropType);
        }

        RoutedEventHandler loadedHandler = null;
        loadedHandler = (sender, _) =>
        {
            IntPtr windowHandle =
                new WindowInteropHelper(sender as System.Windows.Window)?.Handle ?? IntPtr.Zero;

            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            ApplyBackdropCore(sender as System.Windows.Window, backdropType);
            window.Loaded -= loadedHandler;
        };

        window.Loaded += loadedHandler;

        return true;
    }

    /// <summary>
    /// Applies backdrop effect to the selected handle.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <returns><see langword="true"/> if the operation was successfull, otherwise <see langword="false"/>.</returns>
    private static bool ApplyBackdropCore(System.Windows.Window window, WindowBackdropType backdropType)
    {
        IntPtr hWnd = new WindowInteropHelper(window).Handle;

        if (hWnd == IntPtr.Zero || !IsSupported(backdropType))
        {
            return false;
        }

        UpdateGlassFrame(hWnd, backdropType);

        switch (backdropType)
        {
            case WindowBackdropType.Auto:
                return ApplyDwmWindowAttribute(hWnd, Standard.DWMSBT.DWMSBT_AUTO);

            case WindowBackdropType.MainWindow:
                return ApplyDwmWindowAttribute(hWnd, Standard.DWMSBT.DWMSBT_MAINWINDOW);

            case WindowBackdropType.TransientWindow:
                return ApplyDwmWindowAttribute(hWnd, Standard.DWMSBT.DWMSBT_TRANSIENTWINDOW);

            case WindowBackdropType.TabbedWindow:
                return ApplyDwmWindowAttribute(hWnd, Standard.DWMSBT.DWMSBT_TABBEDWINDOW);
        }

        return ApplyDwmWindowAttribute(hWnd, Standard.DWMSBT.DWMSBT_NONE);
    }

    /// <summary>
    /// Tries to remove backdrop effects if they have been applied to the <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window from which the effect should be removed.</param>
    internal static bool RemoveBackdrop(System.Windows.Window window)
    {
        if (window is null)
        {
            return false;
        }

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;

        return RemoveBackdrop(windowHandle);
    }

    /// <summary>
    /// Tries to remove all effects if they have been applied to the <c>hWnd</c>.
    /// </summary>
    /// <param name="hWnd">Pointer to the window handle.</param>
    internal static bool RemoveBackdrop(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        _ = RestoreContentBackground(hWnd);

        var backdropPvAttribute = Standard.DWMSBT.DWMSBT_NONE;
        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hWnd, backdropPvAttribute);
        return dwmResult == HRESULT.S_OK;
    }

    /// <summary>
    /// Tries to remove background from <see cref="Window"/> and it's composition area.
    /// </summary>
    /// <param name="window">Window to manipulate.</param>
    /// <returns><see langword="true"/> if operation was successful.</returns>
    internal static bool RemoveBackground(System.Windows.Window window)
    {
        if (window is null)
        {
            return false;
        }

        // Remove background from visual root
        window.SetCurrentValue(System.Windows.Controls.Control.BackgroundProperty, Brushes.Transparent);

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;

        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        var windowSource = HwndSource.FromHwnd(windowHandle);

        // Remove background from client area
        if (windowSource?.CompositionTarget != null)
        {
            windowSource.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        return true;
    }

    private static bool UpdateGlassFrame(IntPtr hWnd, WindowBackdropType backdropType)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        MARGINS margins = new MARGINS();
        if(backdropType != WindowBackdropType.None)
        {
            margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };                    
        }

        var dwmApiResult = NativeMethods.DwmExtendFrameIntoClientArea(hWnd, ref margins);

        return new HRESULT((uint)dwmApiResult) == HRESULT.S_OK;
    }

    private static bool ApplyDwmWindowAttribute(IntPtr hWnd, Standard.DWMSBT dwmSbt)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hWnd, dwmSbt);
        return dwmResult == HRESULT.S_OK;
    }

    private static bool RestoreContentBackground(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        var windowSource = HwndSource.FromHwnd(hWnd);

        // Restore client area background
        if (windowSource?.Handle != IntPtr.Zero && windowSource?.CompositionTarget != null)
        {
            windowSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
        }

        if (windowSource?.RootVisual is System.Windows.Window window)
        {
            var backgroundBrush = window.Resources["ApplicationBackgroundBrush"];

            // Manual fallback
            if (backgroundBrush is not Brush)
            {
                backgroundBrush = GetFallbackBackgroundBrush();
            }

            window.Background = (SolidColorBrush)backgroundBrush;
        }

        return true;
    }

    private static Brush GetFallbackBackgroundBrush()
    {
        if(SystemParameters.HighContrast)
        {
            string currentTheme = ThemeColorization.GetSystemTheme();
            if(currentTheme.Contains("hc1"))
            {
                return new SolidColorBrush(Color.FromArgb(0xFF, 0x2D, 0x32, 0x36));
            }
            else if(currentTheme.Contains("hc2"))
            {
                return new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
            }
            else if(currentTheme.Contains("hcblack"))
            {
                return new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
            }
            else
            {
                return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFA, 0xEF));
            }
        }
        
        if(ThemeColorization.IsThemeDark())
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
        }
        else
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA));
        }
    }

}
