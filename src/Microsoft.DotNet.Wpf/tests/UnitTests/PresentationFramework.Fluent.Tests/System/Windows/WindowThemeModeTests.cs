// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

namespace Fluent.UITests.ThemeModeTests;

public class WindowThemeModeTests
{
    [WpfTheory]
    [MemberData(nameof(ThemeModes))]
    public void Window_ThemeMode_Initialization(ThemeMode themeMode)
    {
        if (themeMode == ThemeMode.None) return;

        Window window = new Window() { ThemeMode = themeMode };
        window.ApplyTemplate();

        Verify_WindowProperties(window, themeMode);
    }

    [WpfFact]
    public void Window_ThemeMode_Default()
    {
        Window window = new Window();
        window.ApplyTemplate();

        window.ThemeMode.Value.Should().Be("None");
        window.Background.Should().BeNull();
        window.Resources.MergedDictionaries.Should().HaveCount(0);
    }

    [WpfTheory]
    [MemberData(nameof(ThemeModePairs))]
    public void Window_ThemeMode_Switch(ThemeMode themeMode, ThemeMode newThemeMode)
    {
        if (themeMode == newThemeMode) return;

        Window window = new Window();
        window.Show();
        window.ThemeMode = themeMode;
        Verify_WindowProperties(window, themeMode);
        Verify_WindowResources(window, themeMode);

        window.ThemeMode = newThemeMode;
        Verify_WindowProperties(window, newThemeMode);
        Verify_WindowResources(window, newThemeMode);

        window.Close();
    }

    #region Helper Methods

    private void Verify_WindowProperties(Window window, ThemeMode themeMode)
    {
        if (themeMode == ThemeMode.None)
        {
            window.Background.Should().BeNull();
            window.Foreground.ToString().Should().Be(Brushes.Black.ToString());
            window.Resources.MergedDictionaries.Should().HaveCount(0);
            return;
        }

        window.Background.Should().Be(Brushes.Transparent);

    }

    // private void Verify_ApplicationResources(Application application, ThemeMode themeMode)
    // {
    //     if (themeMode == ThemeMode.None)
    //     {
    //         application.Resources.MergedDictionaries.Should().BeEmpty();
    //         return;
    //     }

    //     application.Resources.MergedDictionaries.Should().HaveCount(1);
    //     Uri source = application.Resources.MergedDictionaries[0].Source;
    //     source.AbsoluteUri.ToString()
    //         .Should().EndWith(s_fluentThemeResourceDictionaryMap[themeMode]);
    // }

    private void Verify_WindowResources(Window window, ThemeMode themeMode)
    {
        if (themeMode == ThemeMode.None)
        {
            window.Resources.MergedDictionaries.Should().BeEmpty();
            return;
        }

        window.Resources.MergedDictionaries.Should().HaveCount(1);

        Uri source = window.Resources.MergedDictionaries[0].Source;
        source.AbsoluteUri.ToString()
            .Should().Be(s_fluentThemeResourceDictionaryMap[themeMode]);
    }

    #endregion

    #region Test Data

    public static IEnumerable<object[]> ThemeModes => new List<object[]>
    {
        new object[] { ThemeMode.None },
        new object[] { ThemeMode.System },
        new object[] { ThemeMode.Light },
        new object[] { ThemeMode.Dark }
    };

    public static IEnumerable<object[]> ThemeModePairs => new List<object[]>
    {
        new object[] { ThemeMode.None, ThemeMode.None },
        new object[] { ThemeMode.None, ThemeMode.Light },
        new object[] { ThemeMode.None, ThemeMode.Dark },
        new object[] { ThemeMode.None, ThemeMode.System },
        new object[] { ThemeMode.Light, ThemeMode.None },
        new object[] { ThemeMode.Light, ThemeMode.Light },
        new object[] { ThemeMode.Light, ThemeMode.Dark },
        new object[] { ThemeMode.Light, ThemeMode.System },
        new object[] { ThemeMode.Dark, ThemeMode.None },
        new object[] { ThemeMode.Dark, ThemeMode.Light },
        new object[] { ThemeMode.Dark, ThemeMode.Dark },
        new object[] { ThemeMode.Dark, ThemeMode.System },
        new object[] { ThemeMode.System, ThemeMode.None },
        new object[] { ThemeMode.System, ThemeMode.Light },
        new object[] { ThemeMode.System, ThemeMode.Dark },
        new object[] { ThemeMode.System, ThemeMode.System }
    };

    private static readonly Dictionary<ThemeMode, string> s_fluentThemeResourceDictionaryMap
        = new Dictionary<ThemeMode, string>
            {
                { ThemeMode.None, ""},
                { ThemeMode.System, "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml"},
                { ThemeMode.Light, "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Light.xaml"},
                { ThemeMode.Dark, "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Dark.xaml"},
            };

    #endregion
}
