// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;
using System.Windows.Appearance;
using System.Windows.Navigation;

namespace System.Windows;

internal static class ThemeManager2
{

    internal static void OnSystemThemeChanged()
    {
        bool? isAppAccessible = VerifyApplicationAccess();
        if (isAppAccessible != null)
        {
            if (!isAppAccessible.Value)
            {
                Application.Current.Dispatcher.BeginInvoke(OnSystemThemeChangedCore);
                return;
            }
        }

        OnSystemThemeChangedCore();
    }

    private static void OnSystemThemeChangedCore()
    {
        if (IsFluentThemeEnabled)
        {
            SkipAppThemeModeSyncing = true;

            try
            {
                ThemeMode currThemeMode = Application.Current.ThemeMode;
                bool useLightColors = GetUseLightColors(currThemeMode);
                FluentThemeState newFluentThemeState = new FluentThemeState(currThemeMode.Value, useLightColors);

                bool skipAppResourceUpdate = s_currentFluentThemeState == newFluentThemeState;
                ApplyFluentOnApplication(currThemeMode, skipAppResourceUpdate);
            }
            finally
            {
                SkipAppThemeModeSyncing = false;
            }
        }

        UpdateWindowsOnSystemThemeChange();
    }

    internal static void OnApplicationThemeChanged(ThemeMode oldThemeMode, ThemeMode newThemeMode)
    {
        SkipAppThemeModeSyncing = true;

        try
        {
            if (newThemeMode == ThemeMode.None && newThemeMode != oldThemeMode)
            {
                RemoveFluentFromApplication();
                s_currentFluentThemeState = new FluentThemeState("None", false);
                return;
            }

            bool useLightColors = GetUseLightColors(newThemeMode);
            ApplyFluentOnApplication(newThemeMode);
            s_currentFluentThemeState = new FluentThemeState(newThemeMode.Value, useLightColors);
        }
        finally
        {
            SkipAppThemeModeSyncing = false;
        }
    }

    internal static void OnWindowThemeChanged(Window window, ThemeMode oldThemeMode, ThemeMode newThemeMode)
    {
        if (newThemeMode == ThemeMode.None && newThemeMode != oldThemeMode)
        {
            RemoveFluentFromWindow(window);
            ApplyStyleOnWindow(window);
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

        if (window.ThemeMode != themeMode)
        {
            window.ThemeMode = themeMode;
        }
    }

    internal static void ApplyStyleOnWindow(Window window)
    {
        if (!IsFluentThemeEnabled && window.ThemeMode == ThemeMode.None)
            return;

        bool useLightColors = true;

        if (window.ThemeMode != ThemeMode.None)
        {
            useLightColors = GetUseLightColors(window.ThemeMode);
        }
        else
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    useLightColors = GetUseLightColors(Application.Current.ThemeMode);
                });
            }
            else
            {
                useLightColors = GetUseLightColors(Application.Current.ThemeMode);
            }
        }

        ApplyImmersiveWindowStyle(window, useLightColors);
    }

    private static void RemoveFluentFromApplication()
    {
        if (Application.Current == null) return;

        IEnumerable<int> indices = FindAllFluentThemeResourceDictionaryIndices(Application.Current.Resources);
        foreach (int index in indices)
            Application.Current.Resources.MergedDictionaries.RemoveAt(index);

        ResetWindowsAppearance();
    }

    private static void ApplyFluentOnApplication(ThemeMode themeMode, bool skipResourceUpdate = false)
    {
        if (Application.Current == null) return;

        if (!skipResourceUpdate)
            AddOrUpdateThemeResources(Application.Current.Resources, GetThemeDictionary(themeMode));

        bool useLightColors = GetUseLightColors(themeMode);
        UpdateWindowsAppearance(useLightColors);
    }

    private static void ResetWindowsAppearance()
    {
        WindowCollection appWindows = Application.Current.WindowsInternal.Clone();
        WindowCollection nonAppWindows = Application.Current.NonAppWindowsInternal.Clone();

        foreach (Window window in appWindows)
        {
            if (window.ThemeMode == ThemeMode.None)
            {
                DisableImmersiveWindowStyle(window);
            }
        }

        foreach (Window window in nonAppWindows)
        {
            window.Dispatcher.Invoke(() => DisableImmersiveWindowStyle(window));
        }
    }

    private static void UpdateWindowsAppearance(bool useLightColors)
    {
        WindowCollection appWindows = Application.Current.WindowsInternal.Clone();
        WindowCollection nonAppWindows = Application.Current.NonAppWindowsInternal.Clone();

        foreach (Window window in appWindows)
        {
            if (window.ThemeMode == ThemeMode.None)
            {
                ApplyImmersiveWindowStyle(window, useLightColors);
            }
        }

        foreach (Window window in nonAppWindows)
        {
            window.Dispatcher.Invoke(() =>
            {
                if (window.ThemeMode == ThemeMode.None)
                    ApplyImmersiveWindowStyle(window, useLightColors);
            });
        }
    }

    private static void UpdateWindowsOnSystemThemeChange()
    {
        foreach (Window window in FluentEnabledWindows)
        {
            if (!window.Dispatcher.CheckAccess())
            {
                window.Dispatcher.Invoke(() => ApplyFluentOnWindow(window));
                continue;
            }

            ApplyFluentOnWindow(window);
        }
    }

    // Run from window dispatcher only - all the window functions below
    // RemoveFluentFromWindow now does not take care of applying application's
    // immersive style on window.
    private static void RemoveFluentFromWindow(Window window)
    {
        if (window is null || window.IsDisposed) return;

        IEnumerable<int> indices = FindAllFluentThemeResourceDictionaryIndices(window.Resources);
        foreach (int index in indices)
            window.Resources.MergedDictionaries.RemoveAt(index);

        DisableImmersiveWindowStyle(window);
        FluentEnabledWindows.Remove(window);
    }

    private static void ApplyFluentOnWindow(Window window)
    {
        if (window is null || window.IsDisposed)
            return;

        bool useLightColors = GetUseLightColors(window.ThemeMode);
        AddOrUpdateThemeResources(window.Resources, GetThemeDictionary(window.ThemeMode));
        ApplyImmersiveWindowStyle(window, useLightColors);

        if (!FluentEnabledWindows.HasItem(window))
            FluentEnabledWindows.Add(window);
    }

    private static void DisableImmersiveWindowStyle(Window window)
    {
        if (window is null || window.IsDisposed) return;

        window.SetImmersiveDarkMode(false);
        WindowBackdropManager.SetBackdrop(window, WindowBackdropType.None);
    }

    private static void ApplyImmersiveWindowStyle(Window window, bool useLightColors)
    {
        if (window is null || window.IsDisposed)
            return;

        EnsureFluentWindowStyle(window);
        window.SetImmersiveDarkMode(!useLightColors);

        bool isHighContrast = SystemParameters.HighContrast;
        WindowBackdropManager.SetBackdrop(window, isHighContrast ? WindowBackdropType.None : WindowBackdropType.MainWindow);
    }

    private static void EnsureFluentWindowStyle(Window window)
    {
        if (window.Style == null)
        {
            if (window is NavigationWindow)
                window.SetResourceReference(FrameworkElement.StyleProperty, typeof(NavigationWindow));
            else
                window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
        }
    }


    #region Internal Properties
    internal static bool IsFluentThemeEnabled
    {
        get
        {
            bool? isAccessible = VerifyApplicationAccess();
            if (isAccessible == null)
            {
                // If the application or application dispatcher is null.
                return false;
            }

            if (!isAccessible.Value)
            {
                bool isFluentEnabled = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    isFluentEnabled = Application.Current.ThemeMode != ThemeMode.None;
                });

                return isFluentEnabled;
            }

            return Application.Current.ThemeMode != ThemeMode.None;
        }
    }

    internal static bool SkipAppThemeModeSyncing { get; set; } = false;
    internal static bool IgnoreWindowResourcesChange { get; set; } = false;
    internal static WindowCollection FluentEnabledWindows { get; set; } = new WindowCollection();
    internal const double DefaultFluentFontSizeFactor = 14.0 / 12.0;

    #endregion

    private static bool? VerifyApplicationAccess()
    {
        // If the application is not accessible, we cannot change its resources.
        // This is a safeguard to prevent cross-thread access issues.
        if (Application.Current == null || Application.Current.Dispatcher == null)
            return null;

        if (Application.Current.Dispatcher.CheckAccess())
            return true;

        return false;
    }

    // bool? VerifyWindowAccess(Window window)
    // {
    //     if (window == null)
    //         return null;

    //     if (window.Dispatcher.CheckAccess())
    //         return true;

    //     // If the window is not accessible, we cannot change its resources.
    //     // This is a safeguard to prevent cross-thread access issues.
    //     return false;
    // }

    #region Interal Helper Methods

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

        if (SystemParameters.HighContrast)
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
        bool? isAppAccessible = VerifyApplicationAccess();
        if (isAppAccessible == null)
            return false;

        if (!isAppAccessible.Value)
        {
            bool isFluentThemeDictionaryIncluded = false;
            Application.Current.Dispatcher.Invoke(() => {
                isFluentThemeDictionaryIncluded = IsFluentThemeDictionaryIncluded();
            });

            return isFluentThemeDictionaryIncluded;
        }

        return LastIndexOfFluentThemeDictionary(Application.Current.Resources) != -1;
    }

    #endregion

    #region Private Helper Methods

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

    private const string FluentLightDictionary = "Fluent.Light.xaml";
    private const string FluentDarkDictionary = "Fluent.Dark.xaml";
    private const string FluentHCDictionary = "Fluent.HC.xaml";

    private const string RegPersonalizeKeyPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

    private static FluentThemeState s_currentFluentThemeState = new FluentThemeState("None", false);

    #endregion

}