using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Internal;

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

    /// <summary>
    /// Static member indicating theme which is currently applied.
    /// </summary>
    private static string _currentApplicationTheme = "C:\\windows\\resources\\Themes\\aero.theme";

    private static bool _appInitContainsNewTheme = false;

    private static readonly string _themeDictionaryName = "PresentationFramework.Win11.xaml";

    #endregion

    #region Constructor

    static ThemeColorization()
    {
        foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
        {
            if(mergedDictionary.Source != null && mergedDictionary.Source.ToString().EndsWith(_themeDictionaryName))
            {
                _appInitContainsNewTheme = true;
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

    internal static bool AppInitContainsNewTheme
    {
        get { return _appInitContainsNewTheme; }
    }

    internal static string CurrentApplicationTheme
    {
        get { return _currentApplicationTheme; }
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
        string themeToApply = GetNewTheme();
        
        Window currentWindow = Application.Current.MainWindow;

        if (themeToApply.Contains("dark") && Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "dark.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.Dark, WindowBackdropType.Mica, false);
        }
        else if(themeToApply.Contains("hcwhite") && Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "hcwhite.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
        }
        else if(themeToApply.Contains("hcblack") && Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "hcblack.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
        }
        else if (themeToApply.Contains("hc1") && Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "hc1.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
        }
        else if (themeToApply.Contains("hc2") && Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "hc2.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.HighContrast, WindowBackdropType.None, false);
        }
        else if (Utilities.IsOSWindows11OrNewer)
        {
            UpdateApplicationResources(new Uri("pack://application:,,,/PresentationFramework.Win11;component/Resources/Theme/" + "light.xaml", UriKind.Absolute));
            WindowBackgroundManager.UpdateBackground(currentWindow, ApplicationTheme.Light, WindowBackdropType.Mica, false);
        }

        _currentApplicationTheme = themeToApply;
    }

    /// <summary>
    /// Fetches registry value
    /// </summary>
    /// <returns>string indicating the current theme</returns>
    internal static string GetNewTheme()
    {
        string newTheme = Registry.GetValue(
            _regThemeKey,
            "CurrentTheme",
            "aero.theme"
            ) as string
            ?? String.Empty;

        return newTheme;
    }
    #endregion
}