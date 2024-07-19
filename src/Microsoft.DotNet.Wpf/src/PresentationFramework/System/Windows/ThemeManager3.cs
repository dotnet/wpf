using Standard;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Appearance;

namespace System.Windows;

internal static class ThemeManager3
{


    #region Internal Methods

    internal static void OnSystemThemeChanged()
    {
        if(IsFluentThemeEnabled)
        {
            bool useLightColors = GetUseLightColors(Application.Current.ThemeMode);
            var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
            AddOrUpdateThemeResources(Application.Current.Resources, fluentThemeResourceUri);

            foreach(Window window in Application.Current.Windows)
            {
                if(window.ThemeMode == ThemeMode.None)
                {
                    ApplyStyleOnWindow(window, useLightColors);
                }
                else
                {
                    ApplyFluentOnWindow(window);
                }
            }
        }
        else
        {
            foreach(Window window in FluentEnabledWindows)
            {
                if(window == null || window.IsDisposed) continue;

                if(window.ThemeMode == ThemeMode.None)
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
        IgnoreAppResourcesChange = true;

        try
        {
            if(newThemeMode == ThemeMode.None && oldThemeMode != newThemeMode)
            {
                RemoveFluentFromApplication();
                return;
            }

            bool useLightColors = GetUseLightColors(newThemeMode);
            var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
            AddOrUpdateThemeResources(Application.Current.Resources, fluentThemeResourceUri);

            foreach(Window window in Application.Current.Windows)
            {
                // Replace this with a check for the window theme
                if(!FluentEnabledWindows.HasItem(window))
                {
                    ApplyStyleOnWindow(window, useLightColors);
                }
            }
        }
        finally
        {
            IgnoreAppResourcesChange = false;
        }
    }

    internal static void OnWindowThemeChanged(Window window, ThemeMode oldThemeMode, ThemeMode newThemeMode)
    {
        if(newThemeMode == ThemeMode.None)
        {
            if(newThemeMode != oldThemeMode)
            {
                RemoveFluentFromWindow(window);
            }
            return;
        }

        ApplyFluentOnWindow(window);
    }

    internal static void OnAppResourcesChanged()
    {

    }

    internal static void LoadDefferedApplicationTheme()
    {

    }

    internal static void ApplyStyleOnWindow(Window window)
    {
        if(!IsFluentThemeEnabled && window.ThemeMode == ThemeMode.None) return;

        bool useLightColors = GetUseLightColors(Application.Current.ThemeMode);
        if(window.ThemeMode != ThemeMode.None)
        {
            useLightColors = GetUseLightColors(window.ThemeMode);
        }
        ApplyStyleOnWindow(window, useLightColors);
    }

    internal static bool IsValidThemeMode(ThemeMode themeMode)
    {
        return themeMode == ThemeMode.None || themeMode == ThemeMode.Light || themeMode == ThemeMode.Dark || themeMode == ThemeMode.System;
    }

    #endregion


    #region Private Methods

    private static void RemoveFluentFromApplication()
    {
        if(Application.Current == null) return;

        int index = FindLastFluentThemeResourceDictionaryIndex(Application.Current.Resources);

        if(index != -1)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);
        }

        foreach(Window window in Application.Current.Windows)
        {
            if(!FluentEnabledWindows.HasItem(window))
            {
                RemoveStyleFromWindow(window);
            }
        }
    }

    private static void RemoveFluentFromWindow(Window window)
    {
        if(window == null || window.IsDisposed) return;

        int index = FindLastFluentThemeResourceDictionaryIndex(window.Resources);

        if(index != -1)
        {
            window.Resources.MergedDictionaries.RemoveAt(index);
        }

        RemoveStyleFromWindow(window);
        FluentEnabledWindows.Remove(window);
    }

    private static void ApplyFluentOnWindow(Window window)
    {
        if(window == null || window.IsDisposed) return;
        
        bool useLightColors = GetUseLightColors(window.ThemeMode);
        var fluentThemeResourceUri = GetFluentThemeResourceUri(useLightColors);
        AddOrUpdateThemeResources(window.Resources, fluentThemeResourceUri);
        ApplyStyleOnWindow(window, useLightColors);

        if(!FluentEnabledWindows.HasItem(window))
        {
            FluentEnabledWindows.Add(window);
        }
    }

    private static void RemoveStyleFromWindow(Window window)
    {
        if(window == null || window.IsDisposed) return;

        if(IsFluentThemeEnabled || window.ThemeMode != ThemeMode.None)
        {
            bool useLightColors = GetUseLightColors(Application.Current.ThemeMode);
            window.SetImmersiveDarkMode(!useLightColors);
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
        if(window == null || window.IsDisposed) return;

        // Add a check to see if the window already has a non-implicit style set.
        window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
        window.SetImmersiveDarkMode(!useLightColors);

        if(SystemParameters.HighContrast)
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
            if(Application.Current == null) return false;
            return Application.Current.ThemeMode != ThemeMode.None;
        }
    }

    internal static bool IgnoreAppResourcesChange { get; set; } = false;

    internal static double DefaultFluentThemeFontSize => 14;

    internal static WindowCollection FluentEnabledWindows { get; set; }

    #endregion


    #region Helper Methods

    private static bool GetUseLightColors(ThemeMode themeMode)
    {
        // Is this needed ?
        if(themeMode == ThemeMode.None)
        {
            return true;
        }

        // Do we need to add a check for ThemeMode.None theme?
        return themeMode == ThemeMode.Light || (themeMode == ThemeMode.System && IsSystemThemeLight());
    }

    private static bool ResourceDictionaryContainsFluentDictionary(ResourceDictionary rd, out ThemeMode themeMode)
    {
        themeMode = ThemeMode.None;
        if (rd == null) return false;

        int index = FindLastFluentThemeResourceDictionaryIndex(rd);

        if(index != -1)
        {
            string dictionarySource = rd.MergedDictionaries[index].Source.ToString();
            if(dictionarySource.EndsWith("Fluent.Light.xaml", StringComparison.OrdinalIgnoreCase))
            {
                themeMode = ThemeMode.Light;
            }
            else if(dictionarySource.EndsWith("Fluent.Dark.xaml", StringComparison.OrdinalIgnoreCase))
            {
                themeMode = ThemeMode.Dark;
            }
            else
            {
                themeMode = ThemeMode.System;
            }
            return true;
        }
        return false;
    }

    private static Uri GetFluentThemeResourceUri(bool useLightMode)
    {
        string themeFileName = "Fluent." + (useLightMode ? "Light" : "Dark") + ".xaml";

        if(SystemParameters.HighContrast)
        {
            themeFileName = "Fluent.HC.xaml";
        }

        return new Uri(fluentThemeResoruceDictionaryUri + themeFileName, UriKind.Absolute);

    }

    private static void AddOrUpdateThemeResources(ResourceDictionary rd, Uri dictionaryUri)
    {
        if (rd == null) return;

        ArgumentNullException.ThrowIfNull(dictionaryUri, nameof(dictionaryUri));
        
        var newDictionary = new ResourceDictionary() { Source = dictionaryUri };
        int index = FindLastFluentThemeResourceDictionaryIndex(rd);

        if (index >= 0)
        {
            rd.MergedDictionaries[index] = newDictionary;
        }
        else
        {
            rd.MergedDictionaries.Insert(0, newDictionary);
        }
    }

    private static int FindLastFluentThemeResourceDictionaryIndex(ResourceDictionary rd)
    {
        // Return -1 or throw ?
        // Throwing here because, here we are passing application or window resources,
        // and even though when the field is null, a new RD is created and returned.
        ArgumentNullException.ThrowIfNull(rd, nameof(rd));

        for(int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if(rd.MergedDictionaries[i].Source != null)
            {
                if(rd.MergedDictionaries[i].Source.ToString().StartsWith(fluentThemeResoruceDictionaryUri))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static bool IsSystemThemeLight()
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

    #endregion


    #region Private Fields

    // private static bool _isFluentThemeEnabled;
    // private static bool _deferThemeLoading;

    private static readonly string fluentThemeResoruceDictionaryUri = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/";
    private static readonly string _regPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
    
    private static WindowCollection _fluentEnabledWindows = new WindowCollection();

    #endregion
}