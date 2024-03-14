using Standard;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;

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
    private static string _currentApplicationTheme = "C:\\windows\\resources\\Themes\\aero.theme";

    private static string _currentAppsTheme = "Light";

    private static bool _isFluentWindowsThemeEnabled = false;

    private static readonly string _themeDictionaryName = "FluentWindows.xaml";

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
    }

    #endregion

    // ------------------------------------------------
    //
    // Methods
    //
    // ------------------------------------------------
    #region internal Methods

    internal static bool IsFluentWindowsThemeEnabled
    {
        get { return _isFluentWindowsThemeEnabled; }
    }
    
    internal static void UpdateApplicationTheme()
    {
        string themeToApply = GetSystemTheme();
        string appsThemeToApply = GetAppsTheme();

        Color currentApplicationAccentColor = DWMColorization.CurrentApplicationAccentColor;
        Color accentColorToApply = DWMColorization.GetSystemAccentColor();

        if (themeToApply != _currentApplicationTheme || accentColorToApply != currentApplicationAccentColor || appsThemeToApply != _currentAppsTheme)
        {
            DWMColorization.UpdateAccentColors();
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
        if (dictionaryUri == null)
        throw new ArgumentNullException(nameof(dictionaryUri));

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
        string themeToApply = GetSystemTheme();

        Window currentWindow = Application.Current.MainWindow;

        if (IsThemeDark() && Utility.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "dark.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.Dark, WindowBackdropType.Mica, false);
        }
        else if(IsThemeHighContrast() && Utility.IsOSWindows11OrNewer) 
        {
            if(themeToApply.Contains("hcwhite"))
            {
                UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "hcwhite.xaml", UriKind.Absolute));
                WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
            }
            else if(themeToApply.Contains("hcblack"))
            {
                UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "hcblack.xaml", UriKind.Absolute));
                WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
            }
            else if (themeToApply.Contains("hc1"))
            {
                UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "hc1.xaml", UriKind.Absolute));
                WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
            }
            else
            {
                UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "hc2.xaml", UriKind.Absolute));
                WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
            }
        }
        else if (Utility.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.FluentWindows;component/Resources/Theme/" + "light.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.Light, WindowBackdropType.Mica, false);
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
        return Registry.GetValue(
            _appThemeKey, 
            "AppsUseLightTheme", 
            null) as int? == 0 ? true : false;
    }

    internal static bool IsThemeHighContrast()
    {
        string currentTheme = ThemeColorization.GetSystemTheme();

        if(currentTheme != null)
        {
            return currentTheme.Contains("hc");
        }

        return false;
    }
}