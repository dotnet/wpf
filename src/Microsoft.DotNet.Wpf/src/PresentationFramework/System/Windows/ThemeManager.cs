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
        // TODO : Temprorary way of checking if setting Fluent theme enabled flag. Provide a property for theme switch.
        if (Application.Current != null)
        {
            string dictionarySource;
            foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (mergedDictionary.Source != null)
                {
                    dictionarySource = mergedDictionary.Source.ToString();

                    if (dictionarySource.EndsWith("Fluent.Light.xaml", StringComparison.OrdinalIgnoreCase)
                        || dictionarySource.EndsWith("Fluent.Dark.xaml", StringComparison.OrdinalIgnoreCase)
                        || dictionarySource.EndsWith("Fluent.HC.xaml", StringComparison.OrdinalIgnoreCase)
                        || dictionarySource.EndsWith("Fluent.xaml", StringComparison.OrdinalIgnoreCase))
                    
                    _isFluentThemeEnabled = true;
                    break;
                }
            }
        }
    }

    #endregion

    #region Internal Methods

    internal static void InitializeFluentTheme()
    {
        if(IsFluentThemeEnabled && !_isFluentThemeInitialized)
        {
            _currentApplicationTheme = GetSystemTheme();
            _currentUseLightMode = IsSystemThemeLight();

            var themeColorResourceUri = GetFluentThemeResourceUri(_currentApplicationTheme, _currentUseLightMode);
            AddOrUpdateThemeResources(themeColorResourceUri);

            _isFluentThemeInitialized = true;
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
        Color systemAccentColor = AccentColorHelper.SystemAccentColor;
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
                requestedAccentColor != _currentSystemAccentColor)
        {

            Uri dictionaryUri = GetFluentThemeResourceUri(requestedTheme, requestedUseLightMode);
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
            _currentSystemAccentColor = requestedAccentColor;
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
        if (window == null || !Utility.IsOSWindows11OrNewer)
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
    ///  Update the Fluent theme resources with the values in new dictionary.
    /// </summary>
    /// <param name="dictionaryUri"></param>
    private static void AddOrUpdateThemeResources(Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };

        FindFluentThemeResourceDictionary(out ResourceDictionary fluentDictionary);
        
        if (fluentDictionary != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(fluentDictionary);
        }

        Application.Current.Resources.MergedDictionaries.Add(newDictionary);
    }

    #endregion

    #endregion

    #region Internal Properties

    internal static bool IsFluentThemeEnabled => _isFluentThemeEnabled;
    // TODO : Find a better way to deal with different default font sizes for different themes.
    internal static double DefaultFluentThemeFontSize => 14;

    #endregion

    #region Private Methods

    private static Uri GetFluentThemeResourceUri(string systemTheme, bool useLightMode)
    {
        string themeFileName = "Fluent." + (useLightMode ? "Light" : "Dark") + ".xaml";

        if(SystemParameters.HighContrast)
        {
            themeFileName = "Fluent.HC.xaml";
        }

        return new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/" + themeFileName, UriKind.Absolute);
    }  

    private static void FindFluentThemeResourceDictionary(out ResourceDictionary fluentDictionary)
    {
        fluentDictionary = null;

        if (Application.Current == null) return;

        foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (mergedDictionary.Source != null)
            {
                if (mergedDictionary.Source.ToString().Contains(fluentThemeResoruceDictionaryUri))
                {
                    fluentDictionary = mergedDictionary;
                    break;
                }
            }
        }
    }

    #endregion

    #region Private Members

    private static readonly string _regThemeKeyPath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes";

    private static readonly string _regPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    private static readonly string fluentThemeResoruceDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/";

    private static string _currentApplicationTheme;

    private static bool _currentUseLightMode = true;

    private static bool _isFluentThemeEnabled = false;

    private static bool _isFluentThemeInitialized = false;

    private static Color _currentSystemAccentColor = AccentColorHelper.SystemAccentColor;

    #endregion
}