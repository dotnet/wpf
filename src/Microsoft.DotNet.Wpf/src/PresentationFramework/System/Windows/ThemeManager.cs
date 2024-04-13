using Standard;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Interop;

namespace System.Windows;

internal static class ThemeManager
{

    #region Constructor

    static ThemeManager()
    {
        // TODO : Temprorary way of checking if setting FluentWindows theme enabled flag. Provide a property for theme switch.
        if (Application.Current != null)
        {
            foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (mergedDictionary.Source != null && mergedDictionary.Source.ToString().EndsWith("FluentWindows.xaml"))
                {
                    _isFluentWindowsThemeEnabled = true;
                    break;
                }
            }
        }
    }

    #endregion

    #region Internal Methods

    internal static void InitializeFluentWindowsTheme()
    {
        if(IsFluentWindowsThemeEnabled && !_isFluentWindowsThemeInitialized)
        {
            _currentApplicationTheme = GetSystemTheme();
            _currentUseLightMode = IsSystemThemeLight();

            var themeColorResourceUri = GetFluentWindowThemeColorResourceUri(_currentApplicationTheme, _currentUseLightMode);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = themeColorResourceUri });

            DwmColorization.UpdateAccentColors();
            _isFluentWindowsThemeInitialized = true;
        }
    }

    /// <summary>
    ///    Apply the system theme one window.
    /// </summary>
    /// <param name="forceUpdate"></param>
    internal static void ApplySystemTheme(Window window, bool forceUpdate = false)
    {
        ApplySystemTheme(new List<Window> { window }, forceUpdate);
    }

    /// <summary>
    ///   Apply the system theme to a list of windows.
    ///   If windows is not provided, apply the theme to all windows in the application.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="forceUpdate"></param>
    internal static void ApplySystemTheme(IEnumerable windows = null, bool forceUpdate = false)
    {
        if(windows == null)
        {
            // If windows is not provided, apply the theme to all windows in the application.
            windows = Application.Current?.Windows;
            
            if(windows == null)
            {
                return;
            }
        }

        string systemTheme = GetSystemTheme();
        bool useLightMode = IsSystemThemeLight();
        Color systemAccentColor = DwmColorization.GetSystemAccentColor();
        ApplyTheme(windows , systemTheme, useLightMode, systemAccentColor, forceUpdate);
    }

    /// <summary>
    ///  Apply the requested theme and color mode to the windows.
    ///  Checks if any update is needed before applying the changes.
    /// </summary>
    /// <param name="windows"></param>
    /// <param name="requestedTheme"></param>
    /// <param name="requestedUseLightMode"></param>
    /// <param name="requestedAccentColor"></param>
    /// <param name="forceUpdate"></param>
    private static void ApplyTheme(
        IEnumerable windows, 
        string requestedTheme, 
        bool requestedUseLightMode,
        Color requestedAccentColor, 
        bool forceUpdate = false)
    {
        if(forceUpdate || 
                requestedTheme != _currentApplicationTheme || 
                requestedUseLightMode != _currentUseLightMode ||
                DwmColorization.GetSystemAccentColor() != DwmColorization.CurrentApplicationAccentColor)
        {
            DwmColorization.UpdateAccentColors();

            Uri dictionaryUri = GetFluentWindowThemeColorResourceUri(requestedTheme, requestedUseLightMode);
            AddOrUpdateThemeResources(dictionaryUri);

            foreach(Window window in windows)
            {
                if(window == null)
                {
                    continue;
                }
                
                SetImmersiveDarkMode(window, !requestedUseLightMode);
                WindowBackdropManager.SetBackdrop(window, SystemParameters.HighContrast ? WindowBackdropType.None : WindowBackdropType.MainWindow);
            }

            _currentApplicationTheme = requestedTheme;
            _currentUseLightMode = requestedUseLightMode;
        }
    }

    /// <summary>
    ///  Set the immersive dark mode windowattribute for the window.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="useDarkMode"></param>
    /// <returns></returns>
    private static bool SetImmersiveDarkMode(Window window, bool useDarkMode)
    {
        if (window == null)
        {
            return false;
        }

        IntPtr handle = new WindowInteropHelper(window).Handle;

        if (handle != IntPtr.Zero)
        {
            var dwmResult = NativeMethods.DwmSetWindowAttributeUseImmersiveDarkMode(handle, useDarkMode);
            return dwmResult == HRESULT.S_OK;
        }

        return false;
    }

    #region Helper Methods

    /// <summary>
    ///   Reads the CurrentTheme registry key to fetch the system theme.
    ///   This along with UseLightTheme is used to determine the theme and color mode.
    /// </summary>
    /// <returns></returns>
    internal static string GetSystemTheme()
    {
        string systemTheme = Registry.GetValue(_regThemeKeyPath,
            "CurrentTheme", null) as string ?? "aero.theme";

        return systemTheme;
    }
   
    /// <summary>
    ///   Reads the AppsUseLightTheme registry key to fetch the color mode.
    ///   If the key is not present, it reads the SystemUsesLightTheme key.
    /// </summary>
    /// <returns></returns>
    internal static bool IsSystemThemeLight()
    {
        var useLightTheme = Registry.GetValue(_regPersonalizeKeyPath,
            "AppsUseLightTheme", null) as int?;

        if (useLightTheme == null)
        {
            useLightTheme = Registry.GetValue(_regPersonalizeKeyPath,
                "SystemUsesLightTheme", null) as int?;
        }

        return useLightTheme != null && useLightTheme != 0;
    }

    /// <summary>
    ///  Update the FluentWindows theme resources with the values in new dictionary.
    /// </summary>
    /// <param name="dictionaryUri"></param>
    private static void AddOrUpdateThemeResources(Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        ResourceDictionary currentDictionary = Application.Current?.Resources;
        foreach (var key in newDictionary.Keys)
        {
            if (currentDictionary.Contains(key))
            {
                currentDictionary[key] = newDictionary[key];
            }
            else
            {
                currentDictionary.Add(key, newDictionary[key]);
            }
        }
    }

    #endregion

    #endregion

    #region Internal Properties

    internal static bool IsFluentWindowsThemeEnabled => _isFluentWindowsThemeEnabled;

    #endregion

    #region Private Methods

    private static Uri GetFluentWindowThemeColorResourceUri(string systemTheme, bool useLightMode)
    {
        string themeColorFileName = useLightMode ? "light.xaml" : "dark.xaml";

        if(SystemParameters.HighContrast)
        {
            themeColorFileName = systemTheme switch
            {
                string s when s.Contains("hcblack") => "hcblack.xaml",
                string s when s.Contains("hcwhite") => "hcwhite.xaml",
                string s when s.Contains("hc1") => "hc1.xaml",
                _ => "hc2.xaml"
            };
        }

        return new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + themeColorFileName, UriKind.Absolute);
    }

    #endregion

    #region Private Members

    private static readonly string _regThemeKeyPath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes";

    private static readonly string _regPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    private static string _currentApplicationTheme;

    private static bool _currentUseLightMode = true;

    private static bool _isFluentWindowsThemeEnabled = false;

    private static bool _isFluentWindowsThemeInitialized = false;

    #endregion
}