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
    public static bool ApplyUseImmersiveDarkMode(Window window, bool useImmersiveDarkMode) =>
        GetHandle(window, out IntPtr windowHandle) && ApplyUseImmersiveDarkMode(windowHandle, useImmersiveDarkMode);

    public static bool ApplyUseImmersiveDarkMode(IntPtr handle, bool useImmersiveDarkMode)
    {
        if (handle == IntPtr.Zero || !NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var dwmResult = NativeMethods.DwmSetWindowAttributeUseImmersiveDarkMode(handle, useImmersiveDarkMode);
        return dwmResult == HRESULT.S_OK;
    }

    /// <summary>
    /// Tries to apply selected backdrop type for window handle.
    /// </summary>
    /// <param name="handle">Selected window handle.</param>
    /// <param name="backgroundType">Backdrop type.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowBackdrop(IntPtr handle, WindowBackdropType backgroundType)
    {
        if (handle == IntPtr.Zero || !NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var backdropPvAttribute = ConvertWindowBackdropToDWMSBT(backgroundType);

        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(
            handle, backdropPvAttribute);

        return dwmResult == HRESULT.S_OK;
    }

    public static Standard.DWMSBT ConvertWindowBackdropToDWMSBT(WindowBackdropType backgroundType)
    {
        return backgroundType switch
        {
            WindowBackdropType.Auto => Standard.DWMSBT.DWMSBT_AUTO,
            WindowBackdropType.MainWindow => Standard.DWMSBT.DWMSBT_MAINWINDOW,
            WindowBackdropType.TransientWindow => Standard.DWMSBT.DWMSBT_TRANSIENTWINDOW,
            WindowBackdropType.TabbedWindow => Standard.DWMSBT.DWMSBT_TABBEDWINDOW,
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
