using Microsoft.Win32;
using System.Windows.Appearance;
using System.Windows.Navigation;

namespace System.Windows;

internal static class ThemeManager
{
    #region Internal Methods

    internal static void OnSystemThemeChanged()
    {
        if (IsFluentThemeEnabled)
        {
            SkipAppThemeModeSyncing = true;

            try
            {

                bool useLightColors = GetUseLightColors(Application.Current.ThemeMode);

                FluentThemeState newFluentThemeState = new FluentThemeState(Application.Current.ThemeMode.Value, useLightColors);

                if (s_currentFluentThemeState != newFluentThemeState)
                {
                    AddOrUpdateThemeResources(Application.Current.Resources, GetThemeDictionary(Application.Current.ThemeMode));
                }
                
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.ThemeMode == ThemeMode.None)
                    {
                        ApplyStyleOnWindow(window, useLightColors);
                    }
                    else
                    {
                        ApplyFluentOnWindow(window);
                    }
                }

                s_currentFluentThemeState = newFluentThemeState;
            }
            finally
            {
                SkipAppThemeModeSyncing = false;
            }

        }
        else
        {
            foreach (Window window in FluentEnabledWindows)
            {
                if (window == null || window.IsDisposed)
                    continue;

                if (window.ThemeMode == ThemeMode.None)
                {
                    RemoveFluentFromWindow(window);
                }
                else
                {
                    ApplyFluentOnWindow(window);
                }
            }
        }
    }

    internal static void OnApplicationThemeChanged(ThemeMode oldThemeMode, ThemeMode newThemeMode)
    {
        SkipAppThemeModeSyncing = true;

        try
        {
            if (newThemeMode == ThemeMode.None)
            {
                if (oldThemeMode != newThemeMode)
                {
                    RemoveFluentFromApplication();
                    s_currentFluentThemeState = new FluentThemeState("None", false);
                }
                return;
            }

            bool useLightColors = GetUseLightColors(newThemeMode);
            AddOrUpdateThemeResources(Application.Current.Resources, GetThemeDictionary(newThemeMode));

            foreach (Window window in Application.Current.Windows)
            {
                // Replace this with a check for the window theme
                if (!FluentEnabledWindows.HasItem(window))
                {
                    ApplyStyleOnWindow(window, useLightColors);
                }
            }

            s_currentFluentThemeState = new FluentThemeState(newThemeMode.Value, useLightColors);
        }
        finally
        {
            SkipAppThemeModeSyncing = false;
        }
    }

    internal static void OnWindowThemeChanged(Window window, ThemeMode oldThemeMode, ThemeMode newThemeMode)
    {
        if (newThemeMode == ThemeMode.None)
        {
            if (newThemeMode != oldThemeMode)
            {
                RemoveFluentFromWindow(window);
            }
            return;
        }

        ApplyFluentOnWindow(window);
    }

    internal static bool SyncApplicationThemeMode()
    {
        ThemeMode themeMode = GetThemeModeFromResourceDictionary(Application.Current.Resources);

        if (Application.Current.ThemeMode != themeMode)
        {
            Application.Current.ThemeMode = themeMode;
            return themeMode == ThemeMode.None ? false : true;
        }
        
        return false;
    }

    internal static void SyncWindowThemeMode(Window window)
    {
        ThemeMode themeMode = GetThemeModeFromResourceDictionary(window.Resources);

        if(window.ThemeMode != themeMode)
        {
            window.ThemeMode = themeMode;
        }
    }

    internal static void ApplyStyleOnWindow(Window window)
    {
        if (!IsFluentThemeEnabled && window.ThemeMode == ThemeMode.None)
            return;

        bool useLightColors;

        if (window.ThemeMode != ThemeMode.None)
        {
            useLightColors = GetUseLightColors(window.ThemeMode);
        }
        else
        {
            useLightColors = GetUseLightColors(Application.Current.ThemeMode);
        }

        ApplyStyleOnWindow(window, useLightColors);
    }

    internal static bool IsValidThemeMode(ThemeMode themeMode)
    {
        return themeMode == ThemeMode.None
                    || themeMode == ThemeMode.Light
                    || themeMode == ThemeMode.Dark
                    || themeMode == ThemeMode.System;
    }

    internal static ResourceDictionary GetThemeDictionary(ThemeMode themeMode)
    {
        if (themeMode == ThemeMode.None)
            return null;

        if ( SystemParameters.HighContrast)
        {
            return new ResourceDictionary() { Source = new Uri(FluentThemeResourceDictionaryUri + "Fluent.HC.xaml", UriKind.Absolute) };
        }

        ResourceDictionary rd = null;
        bool useLightColors = GetUseLightColors(themeMode);
        
        if (themeMode == ThemeMode.System)
        {
            rd = new ResourceDictionary() { Source = new Uri(FluentThemeResourceDictionaryUri + "Fluent.xaml", UriKind.Absolute) };

            var colorFileName = useLightColors ? "Light.xaml" : "Dark.xaml";
            Uri dictionaryUri = new Uri(FluentColorDictionaryUri + colorFileName, UriKind.Absolute);
            rd.MergedDictionaries.Insert(0, new ResourceDictionary() { Source = dictionaryUri });            
        }
        else
        {
            var themeFileName = useLightColors ? "Fluent.Light.xaml" : "Fluent.Dark.xaml";
            rd = new ResourceDictionary() { Source = new Uri(FluentThemeResourceDictionaryUri + themeFileName, UriKind.Absolute) };
        }

        return rd;
    }

    internal static bool IsFluentThemeDictionaryIncluded()
    {
        return Application.Current != null && LastIndexOfFluentThemeDictionary(Application.Current.Resources) != -1;
    }

    #endregion


    #region Private Methods

    private static void RemoveFluentFromApplication()
    {
        if (Application.Current == null)
            return;

        IEnumerable<int> indices = FindAllFluentThemeResourceDictionaryIndices(Application.Current.Resources);

        foreach (int index in indices)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);
        }

        foreach (Window window in Application.Current.Windows)
        {
            if (!FluentEnabledWindows.HasItem(window))
            {
                RemoveStyleFromWindow(window);
            }
        }
    }

    private static void RemoveFluentFromWindow(Window window)
    {
        if (window == null || window.IsDisposed)
            return;

        IEnumerable<int> indices = FindAllFluentThemeResourceDictionaryIndices(window.Resources);

        foreach (int index in indices)
        {
            window.Resources.MergedDictionaries.RemoveAt(index);
        }

        RemoveStyleFromWindow(window);
        FluentEnabledWindows.Remove(window);
    }

    private static void ApplyFluentOnWindow(Window window)
    {
        if (window == null || window.IsDisposed)
            return;

        bool useLightColors = GetUseLightColors(window.ThemeMode);
        AddOrUpdateThemeResources(window.Resources, GetThemeDictionary(window.ThemeMode));
        ApplyStyleOnWindow(window, useLightColors);

        if (!FluentEnabledWindows.HasItem(window))
        {
            FluentEnabledWindows.Add(window);
        }
    }

    private static void RemoveStyleFromWindow(Window window)
    {
        if (window == null || window.IsDisposed)
            return;

        if (IsFluentThemeEnabled || window.ThemeMode != ThemeMode.None)
        {
            bool useLightColors = GetUseLightColors(Application.Current.ThemeMode);
            window.SetImmersiveDarkMode(!useLightColors);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.MainWindow);
        }
        else
        {
            // TODO : Remove the styles from windows which have BackdropDisabledWidowStyle
            window.SetImmersiveDarkMode(false);
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
        }

    }

    private static void ApplyStyleOnWindow(Window window, bool useLightColors)
    {
        if (window == null || window.IsDisposed)
            return;

        // We only apply Style on window, if the Window.Style has not already been set to avoid overriding users setting. 
        if (window.Style == null)
        {
            if(window is NavigationWindow)
            {
                window.SetResourceReference(FrameworkElement.StyleProperty, typeof(NavigationWindow));
            }
            else
            {
                window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            }            
        }

        window.SetImmersiveDarkMode(!useLightColors);

        if (SystemParameters.HighContrast)
        {
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
        }
        else
        {
            WindowBackdropManager.SetBackdrop(window, WindowBackdropType.MainWindow);
        }
    }

    #endregion


    #region Internal Properties

    internal static bool IsFluentThemeEnabled
    {
        get
        {
            if (Application.Current == null)
                return false;
            return Application.Current.ThemeMode != ThemeMode.None;
        }
    }

    internal static bool SkipAppThemeModeSyncing { get; set; } = false;

    internal static bool IgnoreWindowResourcesChange { get; set; } = false;

    internal const double DefaultFluentFontSizeFactor = 14.0 / 12.0 ;

    internal static WindowCollection FluentEnabledWindows { get; set; } = new WindowCollection();

    #endregion


    #region Helper Methods

    private static bool GetUseLightColors(ThemeMode themeMode)
    {
        // Is this needed ?
        if (themeMode == ThemeMode.None)
        {
            return true;
        }

        // Do we need to add a check for ThemeMode.None theme?
        return themeMode == ThemeMode.Light || (themeMode == ThemeMode.System && IsSystemThemeLight());
    }

    private static ThemeMode GetThemeModeFromResourceDictionary(ResourceDictionary rd)
    {
        ThemeMode themeMode = ThemeMode.None;

        if (rd == null)
            return themeMode;

        int index = LastIndexOfFluentThemeDictionary(rd);

        if (index != -1)
        {
            themeMode = GetThemeModeFromSourceUri(rd.MergedDictionaries[index].Source);
        }

        return themeMode;
    }

    private static ThemeMode GetThemeModeFromSourceUri(Uri source)
    {
        if (source == null)
            return ThemeMode.None;

        string sourceString = source.ToString();
        if (sourceString.EndsWith(FluentLightDictionary, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeMode.Light;
        }
        else if (sourceString.EndsWith(FluentDarkDictionary, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeMode.Dark;
        }
        else
        {
            return ThemeMode.System;
        }
    }

    private static void AddOrUpdateThemeResources(ResourceDictionary rd, ResourceDictionary newDictionary)
    {
        if (rd == null)
            return;

        ArgumentNullException.ThrowIfNull(newDictionary);

        int index = LastIndexOfFluentThemeDictionary(rd);

        IgnoreWindowResourcesChange = true;

        if (index >= 0)
        {
            rd.MergedDictionaries[index] = newDictionary;
        }
        else
        {
            rd.MergedDictionaries.Insert(0, newDictionary);
        }

        IgnoreWindowResourcesChange = false;
    }

    private static int LastIndexOfFluentThemeDictionary(ResourceDictionary rd)
    {
        // Throwing here because, here we are passing application or window resources,
        // and even though when the field is null, a new RD is created and returned.
        ArgumentNullException.ThrowIfNull(rd);

        for (int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (rd.MergedDictionaries[i].Source != null)
            {
                if (rd.MergedDictionaries[i].Source.ToString().StartsWith(FluentThemeResourceDictionaryUri,
                                                                            StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static IEnumerable<int> FindAllFluentThemeResourceDictionaryIndices(ResourceDictionary rd)
    {
        ArgumentNullException.ThrowIfNull(rd, nameof(rd));

        List<int> indices = new List<int>();

        for (int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (rd.MergedDictionaries[i].Source != null)
            {
                if (rd.MergedDictionaries[i].Source.ToString().StartsWith(FluentThemeResourceDictionaryUri,
                                                                            StringComparison.OrdinalIgnoreCase))
                {
                    indices.Add(i);
                }
            }
        }

        return indices;
    }

    private static bool IsSystemThemeLight()
    {
        var useLightTheme = Registry.GetValue(RegPersonalizeKeyPath,
            "AppsUseLightTheme", null) as int?;

        if (useLightTheme == null)
        {
            useLightTheme = Registry.GetValue(RegPersonalizeKeyPath,
                "SystemUsesLightTheme", null) as int?;
        }

        return useLightTheme != null && useLightTheme != 0;
    }

    #endregion


    #region Private Fields
    private const string FluentColorDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Resources/Theme/";
    private const string FluentThemeResourceDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/";
    private const string RegPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
    private const string FluentLightDictionary = "Fluent.Light.xaml";
    private const string FluentDarkDictionary = "Fluent.Dark.xaml";
    private static FluentThemeState s_currentFluentThemeState = new FluentThemeState("None", false);

    #endregion
}
