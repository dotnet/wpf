// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Media;
using Standard;

namespace System.Windows.Interop;

/// <summary>
/// A set of dangerous methods to modify the appearance.
/// </summary>
internal static class UnsafeNativeMethodsWindow
{

    /// <summary>
    /// Tries to remove ImmersiveDarkMode effect from the <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window to which the effect is to be applied.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool RemoveWindowDarkMode(Window window) =>
        GetHandle(window, out IntPtr windowHandle) && RemoveWindowDarkMode(windowHandle);

    /// <summary>
    /// Tries to remove ImmersiveDarkMode effect from the window handle.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool RemoveWindowDarkMode(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var pvAttribute = 0x0; // Disable
        var dwAttribute = DWMWA.USE_IMMERSIVE_DARK_MODE;

        if (!Utility.IsOSWindows11Insider1OrNewer)
        {
            dwAttribute = DWMWA.USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
        }

        _ = NativeMethods.DwmSetWindowAttribute(handle, dwAttribute, ref pvAttribute, Marshal.SizeOf(typeof(int)));

        return true;
    }

    /// <summary>
    /// Tries to apply ImmersiveDarkMode effect for the <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window to which the effect is to be applied.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowDarkMode(Window window) =>
        GetHandle(window, out IntPtr windowHandle) && ApplyWindowDarkMode(windowHandle);

    /// <summary>
    /// Tries to apply ImmersiveDarkMode effect for the window handle.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowDarkMode(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var pvAttribute = 0x1; // Enable
        var dwAttribute = DWMWA.USE_IMMERSIVE_DARK_MODE;

        if (!Utility.IsOSWindows11Insider1OrNewer)
        {
            dwAttribute = DWMWA.USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
        }

        _ = NativeMethods.DwmSetWindowAttribute(handle, dwAttribute, ref pvAttribute, Marshal.SizeOf(typeof(int)));

        return true;
    }

    /// <summary>
    /// Tries to apply selected backdrop type for window handle.
    /// </summary>
    /// <param name="handle">Selected window handle.</param>
    /// <param name="backgroundType">Backdrop type.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowBackdrop(IntPtr handle, WindowBackdropType backgroundType)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var backdropPvAttribute = (int)ConvertWindowBackdropToDWMSBT(backgroundType);

        if (backdropPvAttribute == (int)Standard.DWMSBT.DWMSBT_NONE)
        {
            return false;
        }

        _ = NativeMethods.DwmSetWindowAttribute(
            handle,
            DWMWA.SYSTEMBACKDROP_TYPE,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return true;
    }

    public static Standard.DWMSBT ConvertWindowBackdropToDWMSBT(WindowBackdropType backgroundType)
    {
        return backgroundType switch
        {
            WindowBackdropType.Auto => Standard.DWMSBT.DWMSBT_AUTO,
            WindowBackdropType.Mica => Standard.DWMSBT.DWMSBT_MAINWINDOW,
            WindowBackdropType.Acrylic => Standard.DWMSBT.DWMSBT_TRANSIENTWINDOW,
            WindowBackdropType.Tabbed => Standard.DWMSBT.DWMSBT_TABBEDWINDOW,
            _ => Standard.DWMSBT.DWMSBT_NONE
        };
    }

    /// <summary>
    /// Tries to get the pointer to the window handle.
    /// </summary>
    /// <returns><see langword="true"/> if the handle is not <see cref="IntPtr.Zero"/>.</returns>
    private static bool GetHandle(Window window, out IntPtr windowHandle)
    {
        if (window is null)
        {
            windowHandle = IntPtr.Zero;

            return false;
        }

        windowHandle = new WindowInteropHelper(window).Handle;

        return windowHandle != IntPtr.Zero;
    }
}
