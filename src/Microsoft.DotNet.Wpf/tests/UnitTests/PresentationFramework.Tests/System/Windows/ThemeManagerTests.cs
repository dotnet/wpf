// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows;

public sealed class ThemeManagerTests
{
    /// <summary>A window with its own theme style key, like RibbonWindow.</summary>
    private sealed class KeyedWindow : Window
    {
        public static readonly ComponentResourceKey s_styleKey = new(typeof(KeyedWindow), "KeyedWindowStyle");

        public KeyedWindow()
        {
            DefaultStyleKey = s_styleKey;
        }
    }

    [WpfFact]
    public void ApplyStyleOnWindow_LooksTheStyleUpByTheWindowDefaultStyleKey()
    {
        var window = new KeyedWindow();
        var keyedStyle = new Style(typeof(KeyedWindow));
        var plainWindowStyle = new Style(typeof(Window));
        window.Resources[KeyedWindow.s_styleKey] = keyedStyle;
        window.Resources[typeof(Window)] = plainWindowStyle;

        ThemeManager.ApplyStyleOnWindow(window, useLightColors: true);

        // The derived window keeps its own theme style instead of getting the plain Window one.
        window.Style.Should().BeSameAs(keyedStyle);
    }

    [WpfFact]
    public void ApplyStyleOnWindow_UsesTheWindowKeyForAPlainWindow()
    {
        var window = new Window();
        var plainWindowStyle = new Style(typeof(Window));
        window.Resources[typeof(Window)] = plainWindowStyle;

        ThemeManager.ApplyStyleOnWindow(window, useLightColors: true);

        window.Style.Should().BeSameAs(plainWindowStyle);
    }

    [WpfFact]
    public void ApplyStyleOnWindow_DoesNotOverrideAnExplicitStyle()
    {
        var window = new Window();
        var explicitStyle = new Style(typeof(Window));
        window.Style = explicitStyle;
        window.Resources[typeof(Window)] = new Style(typeof(Window));

        ThemeManager.ApplyStyleOnWindow(window, useLightColors: true);

        window.Style.Should().BeSameAs(explicitStyle);
    }
}
