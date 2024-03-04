// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// This Source Code is partially based on reverse engineering of the Windows Operating System,
// and is intended for use on Windows systems only.
// This Source Code is partially based on the source code provided by the .NET Foundation.

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
        var dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

        if (!Utility.IsOSWindows11Insider1OrNewer)
        {
            dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        _ = Dwmapi.DwmSetWindowAttribute(handle, dwAttribute, ref pvAttribute, Marshal.SizeOf(typeof(int)));

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
        var dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

        if (!Utility.IsOSWindows11Insider1OrNewer)
        {
            dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        _ = Dwmapi.DwmSetWindowAttribute(handle, dwAttribute, ref pvAttribute, Marshal.SizeOf(typeof(int)));

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

        var backdropPvAttribute = (int)UnsafeReflection.Cast(backgroundType);

        if (backdropPvAttribute == (int)Dwmapi.DWMSBT.DWMSBT_DISABLE)
        {
            return false;
        }

        _ = Dwmapi.DwmSetWindowAttribute(
            handle,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return true;
    }

    /// <summary>
    /// Tries to determine whether the provided <see cref="Window"/> has applied legacy backdrop effect.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <param name="backdropType">Background backdrop type.</param>
    public static bool IsWindowHasBackdrop(IntPtr handle, WindowBackdropType backdropType)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var pvAttribute = 0x0;

        _ = Dwmapi.DwmGetWindowAttribute(
            handle,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref pvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return pvAttribute == (int)UnsafeReflection.Cast(backdropType);
    }

    /// <summary>
    /// Tries to determine whether the provided <see cref="Window"/> has applied legacy Mica effect.
    /// </summary>
    /// <param name="window">Window to check.</param>
    public static bool IsWindowHasLegacyMica(Window window) =>
        GetHandle(window, out IntPtr windowHandle) && IsWindowHasLegacyMica(windowHandle);

    /// <summary>
    /// Tries to determine whether the provided handle has applied legacy Mica effect.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    public static bool IsWindowHasLegacyMica(IntPtr handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var pvAttribute = 0x0;

        _ = Dwmapi.DwmGetWindowAttribute(
            handle,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
            ref pvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return pvAttribute == 0x1;
    }

    /// <summary>
    /// Tries to apply legacy Mica effect for the selected <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window to which the effect is to be applied.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowLegacyMicaEffect(Window window) =>
        GetHandle(window, out IntPtr windowHandle) && ApplyWindowLegacyMicaEffect(windowHandle);

    /// <summary>
    /// Tries to apply legacy Mica effect for the selected <see cref="Window"/>.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowLegacyMicaEffect(IntPtr handle)
    {
        var backdropPvAttribute = 0x1; //Enable

        _ = Dwmapi.DwmSetWindowAttribute(
            handle,
            Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
            ref backdropPvAttribute,
            Marshal.SizeOf(typeof(int))
        );

        return true;
    }

    /// <summary>
    /// Tries to apply legacy Acrylic effect for the selected <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window to which the effect is to be applied.</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowLegacyAcrylicEffect(Window window) =>
        GetHandle(window, out IntPtr windowHandle) && ApplyWindowLegacyAcrylicEffect(windowHandle);

    /// <summary>
    /// Tries to apply legacy Acrylic effect for the selected <see cref="Window"/>.
    /// </summary>
    /// <param name="handle">Window handle</param>
    /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
    public static bool ApplyWindowLegacyAcrylicEffect(IntPtr handle)
    {
        var accentPolicy = new ACCENT_POLICY
        {
            nAccentState = ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            nColor = 0x990000 & 0xFFFFFF
        };

        var accentStructSize = Marshal.SizeOf(accentPolicy);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);

        Marshal.StructureToPtr(accentPolicy, accentPtr, false);

        var data = new WINCOMPATTRDATA
        {
            Attribute = WCA.WCA_ACCENT_POLICY,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        _ = NativeMethods.SetWindowCompositionAttribute(handle, ref data);

        Marshal.FreeHGlobal(accentPtr);

        return true;
    }

    /// <summary>
    /// Tries to get currently selected Window accent color.
    /// </summary>
    public static Color GetDwmColor()
    {
        try
        {
            Dwmapi.DwmGetColorizationParameters(out var dwmParams);
            var values = BitConverter.GetBytes(dwmParams.clrColor);

            return Color.FromArgb(255, values[2], values[1], values[0]);
        }
        catch
        {
            var colorizationColorValue = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM",
                "ColorizationColor",
                null
            );

            if (colorizationColorValue is not null)
            {
                try
                {
                    var colorizationColor = (uint)(int)colorizationColorValue;
                    var values = BitConverter.GetBytes(colorizationColor);

                    return Color.FromArgb(255, values[2], values[1], values[0]);
                }
                catch { }
            }
        }

        return GetDefaultWindowsAccentColor();
    }

    /// <summary>
    /// Checks whether the DWM composition is enabled.
    /// </summary>
    public static bool IsCompositionEnabled()
    {
        _ = Dwmapi.DwmIsCompositionEnabled(out var isEnabled);

        return isEnabled == 0x1;
    }

    /// <summary>
    /// Checks if provided pointer represents existing window.
    /// </summary>
    public static bool IsValidWindow(IntPtr hWnd)
    {
        return NativeMethods.IsWindow(hWnd);
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

    private static IntPtr SetWindowLong(IntPtr handle, GWL nIndex, long windowStyleLong)
    {
        if (IntPtr.Size == 4)
        {
            return new IntPtr(NativeMethods.SetWindowLongPtr(handle, nIndex, (int)windowStyleLong));
        }

        return NativeMethods.SetWindowLongPtr(handle, nIndex, (IntPtr)windowStyleLong);
    }

    private static Color GetDefaultWindowsAccentColor()
    {
        // Windows default accent color
        // https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-themes-windowcolor#values
        return Color.FromArgb(0xff, 0x00, 0x78, 0xd7);
    }
}
