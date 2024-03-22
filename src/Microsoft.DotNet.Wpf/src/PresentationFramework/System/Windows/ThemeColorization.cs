using Standard;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections.Generic;

namespace System.Windows;

internal static class ThemeColorization
{
    // ------------------------------------------------
    //
    // Members
    //
    // ------------------------------------------------
    #region Private Members
    /// <summary>
    /// Registry Path containing current theme information.
    /// </summary>
    private static readonly string _regThemeKey = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes";

    private static readonly string _appThemeKey = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    /// <summary>
    /// Static member indicating theme which is currently applied.
    /// </summary>
    private static string _currentApplicationTheme;

    private static string _currentAppsTheme = "Light";

    private static bool _isFluentWindowsThemeEnabled = false;

    private static readonly string _themeDictionaryName = "FluentWindows.xaml";

    private static List<Uri> _themeDictionaryUris = new List<Uri>();

    #endregion

    #region Constructor

    static ThemeColorization()
    {
        foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
        {
            if(mergedDictionary.Source != null && mergedDictionary.Source.ToString().EndsWith(_themeDictionaryName))
            {
                _isFluentWindowsThemeEnabled = true;
                break;
            }
        }

        _currentApplicationTheme = GetSystemTheme();
    }

    #endregion

    // ------------------------------------------------
    //
    // Methods
    //
    // ------------------------------------------------
    #region internal Methods

    internal static bool IsThemeDictionaryLoaded(Uri dictionaryUri)
    {
        return _themeDictionaryUris.Contains(dictionaryUri);
    }

    internal static void AddThemeDictionary(ResourceDictionary dictionary)
    {
        if (!IsThemeDictionaryLoaded(dictionary.Source))
        {
            Application.Current.Resources.MergedDictionaries.Add(dictionary);
            _themeDictionaryUris.Add(dictionary.Source);
        }
    }

    internal static bool IsFluentWindowsThemeEnabled
    {
        get { return _isFluentWindowsThemeEnabled; }
    }
    
    internal static void UpdateApplicationTheme()
    {
        string themeToApply = GetSystemTheme();
        string appsThemeToApply = GetAppsTheme();

        Color currentApplicationAccentColor = DwmColorization.CurrentApplicationAccentColor;
        Color accentColorToApply = DwmColorization.GetSystemAccentColor();

        if (themeToApply != _currentApplicationTheme || accentColorToApply != currentApplicationAccentColor || appsThemeToApply != _currentAppsTheme)
        {
            DwmColorization.UpdateAccentColors();
            ApplyTheme();
        }
    }

    /// <summary>
    /// Updates the application resources with the specified <see cref="ResourceDictionary"/>.
    /// </summary>
    /// <param name="newDictionary">The new <see cref="ResourceDictionary"/> to update the application resources with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newDictionary"/> is null.</exception>
    internal static void UpdateApplicationResources(Uri dictionaryUri)
    {
        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));

        ResourceDictionary newDictionary = new ResourceDictionary();
        newDictionary.Source = dictionaryUri;

        foreach (var key in newDictionary.Keys)
        {
            if (Application.Current.Resources.Contains(key))
            {
                if (!object.Equals(Application.Current.Resources[key], newDictionary[key]))
                {
                    Application.Current.Resources[key] = newDictionary[key];
                }
            }
            else
            {
                Application.Current.Resources.Add(key, newDictionary[key]);
            }
        }
    }

    /// <summary>
    /// Updates the value of resources based on Type of theme applied in resource dictionary
    /// </summary>
    internal static void ApplyTheme()
    {
        var themeToApply = GetSystemTheme();
        var resourceUri = GetApplicationThemeUri(themeToApply, out ApplicationTheme applicationTheme);
        var backdrop = applicationTheme == ApplicationTheme.HighContrast ? WindowBackdropType.None : WindowBackdropType.Mica;
        
        if(Utility.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(resourceUri);
            foreach (Window window in Application.Current.Windows)
            {        
                WindowBackgroundManager.UpdateBackground(window, applicationTheme, WindowBackdropType.Mica, false);
            }
        }

        _currentApplicationTheme = themeToApply;
        _currentAppsTheme = GetAppsTheme();
    }

    private static Uri GetApplicationThemeUri(string systemTheme, out ApplicationTheme applicationTheme)
    {
        string themeFileName = "light.xaml";

        if(IsThemeDark())
        {  
            applicationTheme = ApplicationTheme.Dark;
            themeFileName = "dark.xaml";
        }
        else if(SystemParameters.HighContrast)
        {
            applicationTheme = ApplicationTheme.HighContrast;
            
            if(systemTheme.Contains("hcwhite"))
            {
                themeFileName = "hcwhite.xaml";
            }
            else if(systemTheme.Contains("hcblack"))
            {
                themeFileName = "hcblack.xaml";
            }
            else if(systemTheme.Contains("hc1"))
            {
                themeFileName = "hc1.xaml";
            }
            else
            {
                themeFileName = "hc2.xaml";
            }
        }
        else
        {
            applicationTheme = ApplicationTheme.Light;
            themeFileName = "light.xaml";
        }

        return new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + themeFileName, UriKind.Absolute);
    }

    internal static void ApplyTheme(Windows.Window window)
    {
        var themeToApply = GetSystemTheme();
        var resourceUri = GetApplicationThemeUri(themeToApply, out ApplicationTheme applicationTheme);
        var backdrop = applicationTheme == ApplicationTheme.HighContrast ? WindowBackdropType.None : WindowBackdropType.Mica;
        
        if(Utility.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(resourceUri);
            WindowBackgroundManager.UpdateBackground(window, applicationTheme, WindowBackdropType.Mica, false);
        }    

        _currentApplicationTheme = themeToApply;
        _currentAppsTheme = GetAppsTheme();
    
    }

    /// <summary>
    /// Fetches registry value
    /// </summary>
    /// <returns>string indicating the current theme</returns>
    internal static string GetSystemTheme()
    {
        string systemTheme = Registry.GetValue(
            _regThemeKey,
            "CurrentTheme",
            "aero.theme"
            ) as string
            ?? String.Empty;

        return systemTheme;
    }

    internal static string GetAppsTheme()
    {
        return IsThemeDark() ? "Dark" : "Light";
    }
    #endregion


    internal static bool IsThemeDark()
    {
        var appsUseLightTheme = Registry.GetValue(
                                _appThemeKey,
                                "AppsUseLightTheme",
                                null) as int?;

        if (appsUseLightTheme == null)
        {
            return Registry.GetValue(
                _appThemeKey,
                "SystemUsesLightTheme",
                null) as int? == 0 ? true : false;
        }

        return appsUseLightTheme == 0 ? true : false;
    }
}
