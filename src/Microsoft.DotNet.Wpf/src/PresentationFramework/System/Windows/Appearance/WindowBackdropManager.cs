// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal;
using Standard;
using Windows.Win32.Foundation;

// ReSharper disable once CheckNamespace
namespace System.Windows.Appearance;

internal static class WindowBackdropManager
{
    internal static bool IsSupported(WindowBackdropType backdropType)
    {
        return backdropType switch
        {
            WindowBackdropType.Auto => Utility.IsWindows11_22H2OrNewer,
            WindowBackdropType.TabbedWindow => Utility.IsWindows11_22H2OrNewer,
            WindowBackdropType.MainWindow => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.TransientWindow => Utility.IsOSWindows7OrNewer,
            WindowBackdropType.None => true,
            _ => false
        };
    }

    internal static bool SetBackdrop(Window window, WindowBackdropType backdropType)
    {
        if (window is null ||
                !IsSupported(backdropType) ||
                window.AllowsTransparency ||
                IsBackdropEnabled == false)
        {
            return false;
        }

        if(!ThemeManager.IsFluentThemeEnabled && window.ThemeMode == ThemeMode.None)
        {
            return false;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        return SetBackdropCore(handle, backdropType);
    }

    #region Private Methods

    private static bool SetBackdropCore(IntPtr hwnd, WindowBackdropType backdropType)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (backdropType == WindowBackdropType.None)
        {
            RestoreBackground(hwnd);
            return RemoveBackdrop(hwnd);
        }

        RemoveBackground(hwnd);
        return ApplyBackdrop(hwnd, backdropType);
    }

    private static bool ApplyBackdrop(IntPtr hwnd, WindowBackdropType backdropType)
    {
        UpdateGlassFrame(hwnd, backdropType);

        var backdropPvAttribute = backdropType switch
        {
            WindowBackdropType.Auto => DWMSBT.DWMSBT_TABBEDWINDOW,
            WindowBackdropType.TabbedWindow => DWMSBT.DWMSBT_TABBEDWINDOW,
            WindowBackdropType.MainWindow => DWMSBT.DWMSBT_MAINWINDOW,
            WindowBackdropType.TransientWindow => DWMSBT.DWMSBT_TRANSIENTWINDOW,
            _ => DWMSBT.DWMSBT_NONE
        };

        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hwnd, backdropPvAttribute);
        return dwmResult == HRESULT.S_OK;
    }

    private static bool RemoveBackdrop(IntPtr hwnd)
    {
        UpdateGlassFrame(hwnd, WindowBackdropType.None);

        var backdropPvAttribute = DWMSBT.DWMSBT_NONE;
        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hwnd, backdropPvAttribute);
        return dwmResult == HRESULT.S_OK;
    }

    private static bool RemoveBackground(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            var windowSource = HwndSource.FromHwnd(hwnd);
            if (windowSource.CompositionTarget != null)
            {
                // TODO : Save the previous background color and reapply in RestoreBackground 
                windowSource.CompositionTarget.BackgroundColor = Colors.Transparent;
                return true;
            }
        }
        return false;
    }

    private static bool RestoreBackground(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            var windowSource = HwndSource.FromHwnd(hwnd);
            if (windowSource?.Handle != IntPtr.Zero && windowSource.CompositionTarget != null)
            {
                windowSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
                return true;
            }
        }
        return false;
    }

    private static bool UpdateGlassFrame(IntPtr hwnd, WindowBackdropType backdropType)
    {
        MARGINS margins = new MARGINS();
        if (backdropType != WindowBackdropType.None)
        {
            margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        }

        return NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins) == HRESULT.S_OK;
    }

    #endregion

    #region Internal Properties

    internal static bool IsBackdropEnabled => _isBackdropEnabled ??= Utility.IsWindows11_22H2OrNewer && 
                                                                        !FrameworkAppContextSwitches.DisableFluentThemeWindowBackdrop;

    private static bool? _isBackdropEnabled = null;

    #endregion

}
