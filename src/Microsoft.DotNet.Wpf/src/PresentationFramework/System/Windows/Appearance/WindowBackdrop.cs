// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using System.Windows.Appearance;
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
    public static bool IsSupported(WindowBackdropType backdropType)
    {
        return backdropType switch
        {
            WindowBackdropType.Auto => Utility.IsOSWindows11Insider1OrNewer,
            WindowBackdropType.Tabbed => Utility.IsOSWindows11Insider1OrNewer,
            WindowBackdropType.Mica => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.Acrylic => Utility.IsOSWindows7OrNewer,
            WindowBackdropType.None => true,
            _ => false
        };
    }

    /// <summary>
    /// Applies backdrop effect to the selected <see cref="System.Windows.Window"/>.
    /// </summary>
    /// <param name="window">Selected window.</param>
    /// <returns><see langword="true"/> if the operation was successfull, otherwise <see langword="false"/>.</returns>
    public static bool ApplyBackdrop(System.Windows.Window window, WindowBackdropType backdropType)
    {
        if (window is null)
        {
            return false;
        }

        if (window.IsLoaded)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;

            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            return ApplyBackdrop(windowHandle, backdropType);
        }

        window.Loaded += (sender, _) =>
        {
            IntPtr windowHandle =
                new WindowInteropHelper(sender as System.Windows.Window ?? null)?.Handle ?? IntPtr.Zero;

            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            ApplyBackdrop(windowHandle, backdropType);
        };

        return true;
    }

    /// <summary>
    /// Applies backdrop effect to the selected handle.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <returns><see langword="true"/> if the operation was successfull, otherwise <see langword="false"/>.</returns>
    public static bool ApplyBackdrop(IntPtr hWnd, WindowBackdropType backdropType)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(hWnd))
        {
            return false;
        }

        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
        {
            _ = UnsafeNativeMethodsWindow.ApplyWindowDarkMode(hWnd);
        }
        else
        {
            _ = UnsafeNativeMethodsWindow.RemoveWindowDarkMode(hWnd);
        }

        // BUG - This is causing TitleBar caption to be removed for normal windows
        //_ = UnsafeNativeMethodsWindow.RemoveWindowCaption(hWnd);

        // 22H1
        if (!Utility.IsOSWindows11Insider1OrNewer)
        {
            if (backdropType != WindowBackdropType.None)
            {
                return ApplyLegacyMicaBackdrop(hWnd);
            }

            return false;
        }

        switch (backdropType)
        {
            case WindowBackdropType.Auto:
                return ApplyDwmwWindowAttrubute(hWnd, Dwmapi.DWMSBT.DWMSBT_AUTO);

            case WindowBackdropType.Mica:
                return ApplyDwmwWindowAttrubute(hWnd, Dwmapi.DWMSBT.DWMSBT_MAINWINDOW);

            case WindowBackdropType.Acrylic:
                return ApplyDwmwWindowAttrubute(hWnd, Dwmapi.DWMSBT.DWMSBT_TRANSIENTWINDOW);

            case WindowBackdropType.Tabbed:
                return ApplyDwmwWindowAttrubute(hWnd, Dwmapi.DWMSBT.DWMSBT_TABBEDWINDOW);
        }

        return ApplyDwmwWindowAttrubute(hWnd, Dwmapi.DWMSBT.DWMSBT_DISABLE);
    }

    /// <summary>
    /// Tries to remove backdrop effects if they have been applied to the <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window from which the effect should be removed.</param>
    public static bool RemoveBackdrop(System.Windows.Window window)
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
    public static bool RemoveBackdrop(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        _ = RestoreContentBackground(hWnd);

        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(hWnd))
        {
            return false;
        }

        var pvAttribute = 0; // Disable
        var backdropPvAttribute = (int)Dwmapi.DWMSBT.DWMSBT_DISABLE;

        _ = Dwmapi.DwmSetWindowAttribute(
            hWnd,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
            ref pvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        _ = Dwmapi.DwmSetWindowAttribute(
            hWnd,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return true;
    }

    /// <summary>
    /// Tries to remove background from <see cref="Window"/> and it's composition area.
    /// </summary>
    /// <param name="window">Window to manipulate.</param>
    /// <returns><see langword="true"/> if operation was successful.</returns>
    public static bool RemoveBackground(System.Windows.Window window)
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
        if (windowSource?.Handle != IntPtr.Zero && windowSource?.CompositionTarget != null)
        {
            windowSource.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        return true;
    }

    private static bool ApplyDwmwWindowAttrubute(IntPtr hWnd, Dwmapi.DWMSBT dwmSbt)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(hWnd))
        {
            return false;
        }

        var backdropPvAttribute = (int)dwmSbt;

        var dwmApiResult = Dwmapi.DwmSetWindowAttribute(
            hWnd,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return new HRESULT((uint)dwmApiResult) == HRESULT.S_OK;
    }

    private static bool ApplyLegacyMicaBackdrop(IntPtr hWnd)
    {
        var backdropPvAttribute = 1; //Enable

        // TODO: Validate HRESULT
        var dwmApiResult = Dwmapi.DwmSetWindowAttribute(
            hWnd,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return new HRESULT((uint)dwmApiResult) == HRESULT.S_OK;
    }

    private static bool ApplyLegacyAcrylicBackdrop(IntPtr hWnd)
    {
        throw new NotImplementedException();
    }

    private static bool RestoreContentBackground(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(hWnd))
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
            if (backgroundBrush is not SolidColorBrush)
            {
                backgroundBrush = GetFallbackBackgroundBrush();
            }

            window.Background = (SolidColorBrush)backgroundBrush;
        }

        return true;
    }

    private static Brush GetFallbackBackgroundBrush()
    {
        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.HighContrast)
        {
            switch (ApplicationThemeManager.GetSystemTheme())
            {
                case SystemTheme.HC1:
                    return new SolidColorBrush(Color.FromArgb(0xFF, 0x2D, 0x32, 0x36));
                case SystemTheme.HC2:
                    return new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                case SystemTheme.HCBlack:
                    return new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
                case SystemTheme.HCWhite:
                default:
                    return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFA, 0xEF));
            }
        }
        else if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
        }
        else
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA));
        }
    }
}
