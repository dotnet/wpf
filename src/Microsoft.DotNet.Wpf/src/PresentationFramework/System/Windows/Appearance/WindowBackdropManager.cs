using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal;
using Standard;
using HRESULT = Standard.HRESULT;

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
        if (window is null || window.AllowsTransparency)
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

        if(!IsSupported(backdropType) || IsBackdropEnabled == false)
        {
            return SetBackdropCore(handle, WindowBackdropType.None);
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
            WindowBackdropType.Auto => Standard.DWMSBT.DWMSBT_TABBEDWINDOW,
            WindowBackdropType.TabbedWindow => Standard.DWMSBT.DWMSBT_TABBEDWINDOW,
            WindowBackdropType.MainWindow => Standard.DWMSBT.DWMSBT_MAINWINDOW,
            WindowBackdropType.TransientWindow => Standard.DWMSBT.DWMSBT_TRANSIENTWINDOW,
            _ => Standard.DWMSBT.DWMSBT_NONE
        };

        var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hwnd, backdropPvAttribute);
        return dwmResult == HRESULT.S_OK;
    }

    private static bool RemoveBackdrop(IntPtr hwnd)
    {
        UpdateGlassFrame(hwnd, WindowBackdropType.None);

        var backdropPvAttribute = Standard.DWMSBT.DWMSBT_NONE;
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
            HwndSource windowSource = HwndSource.FromHwnd(hwnd);
            if (windowSource?.Handle != IntPtr.Zero && windowSource.CompositionTarget != null)
            {
                // If the window is in light mode, set the background color to #FFFAFAFA and for dark mode set it to #FF202020
                windowSource.CompositionTarget.BackgroundColor = WindowOnLightMode(windowSource) ?
                    (Color)ColorConverter.ConvertFromString(_lightWindowBackgroundCompositionColor) : (Color)ColorConverter.ConvertFromString(_darkWindowBackgroundCompositionColor);;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// This method checks if the window associated with the hwndSource should be in light mode or not depending on Window's ThemeMode, Application's ThemeMode and System's Theme.
    /// </summary>
    /// <param name="hwndSource"></param>
    /// <returns>True if window should be in light mode, false otherwise</returns>
    private static bool WindowOnLightMode(HwndSource hwndSource)
    {
        Window window = hwndSource.RootVisual as Window;

        if(window is null)
        {
            // We were unconditionally assuming that windowSource needs to have the mode as light even if window was null earlier. Doing the same here to ensure parity.
            return true;
        }

        if (window.ThemeMode == ThemeMode.Light ||
            (window.ThemeMode == ThemeMode.System && ThemeManager.IsSystemThemeLight()) ||
            (window.ThemeMode == ThemeMode.None && Application.Current?.ThemeMode == ThemeMode.Light) ||
            (window.ThemeMode == ThemeMode.None && Application.Current?.ThemeMode == ThemeMode.System && ThemeManager.IsSystemThemeLight()))
        {
            return true;
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

        var dwmApiResult = NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);
        return new HRESULT((uint)dwmApiResult) == HRESULT.S_OK;
    }

    #endregion

    #region Internal Properties

    internal static bool IsBackdropEnabled => _isBackdropEnabled ??= Utility.IsWindows11_22H2OrNewer &&
                                                                        !FrameworkAppContextSwitches.DisableFluentThemeWindowBackdrop;

    private static bool? _isBackdropEnabled = null;

    private const string _lightWindowBackgroundCompositionColor = "#FFFAFAFA";

    private const string _darkWindowBackgroundCompositionColor = "#FF202020";

    #endregion

}
