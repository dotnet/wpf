using System;
using System.Diagnostics;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Internal;
using System.Runtime.InteropServices;

namespace System.Windows;
internal static class DwmColorization
{
    /// <summary>
    /// The Accent Color that is currently applied to the application.
    /// </summary>
    private static Color _currentApplicationAccentColor = Color.FromArgb(255, 0, 120, 212);

    internal static Color CurrentApplicationAccentColor
    {
        get { return _currentApplicationAccentColor; }
    }

    [DllImport("UXTheme.dll")]
    private static extern int GetUserColorPreference(in IMMERSIVE_COLOR_PREFERENCE colorPreference, bool alwaysFalse);

    [DllImport("UXTheme.dll")]
    private static extern uint GetColorFromPreference(in IMMERSIVE_COLOR_PREFERENCE colorPreference, IMMERSIVE_COLOR_TYPE colorType, bool isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE cacheMode);

    [StructLayout(LayoutKind.Sequential)]
    private struct IMMERSIVE_COLOR_PREFERENCE
    {
        public uint crStartColor;
        public uint crAccentColor;
    }

    private enum IMMERSIVE_COLOR_TYPE
    {
        IMCLR_SystemAccentLight1 = 5,
        IMCLR_SystemAccentLight2 = 6,
        IMCLR_SystemAccentLight3 = 7,
        IMCLR_SystemAccentDark1 = 3,
        IMCLR_SystemAccentDark2 = 2,
        IMCLR_SystemAccentDark3 = 1,
        IMCLR_SystemAccent = 4
    }

    private enum IMMERSIVE_HC_CACHE_MODE
    {
        IHCM_USE_CACHED_VALUE = 0,
        IHCM_REFRESH = 1
    }

    /// <summary>
    /// Gets the system accent color.
    /// </summary>
    /// <returns>Updated <see cref="System.Windows.Media.Color"/> Accent Color.</returns>
    internal static Color GetSystemAccentColor()
    {
        bool isHighContrastEnabled = SystemParameters.HighContrast;

        IMMERSIVE_COLOR_PREFERENCE colorPreference = new IMMERSIVE_COLOR_PREFERENCE();

        int err = GetUserColorPreference(in colorPreference, false);

        if (err != 0)
        {
            return Color.FromArgb(255, 0, 120, 212);
        }
        else
        {
            uint AccentColor = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccent, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);

            return Color.FromArgb(0xff, (byte)AccentColor, (byte)(AccentColor >> 8), (byte)(AccentColor >> 16));
        }
    }

    /// <summary>
    /// Computes the current Accent Colors and calls for updating of accent color values in resource dictionary
    /// </summary>
    internal static void UpdateAccentColors()
    {
        Color systemAccent = GetSystemAccentColor();

        Color primaryAccent;
        Color secondaryAccent;
        Color tertiaryAccent;

        bool isDarkTheme = ThemeColorization.IsThemeDark();
        bool isHighContrastEnabled = SystemParameters.HighContrast;

        IMMERSIVE_COLOR_PREFERENCE colorPreference = new IMMERSIVE_COLOR_PREFERENCE();

        int err = GetUserColorPreference(in colorPreference, false);

        if(err != 0) 
        {
            primaryAccent = secondaryAccent = tertiaryAccent = systemAccent;
        }
        else
        {
            uint Accent1, Accent2, Accent3;

            if (isDarkTheme)
            {
                Accent1 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentDark1, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
                Accent2 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentDark2, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
                Accent3 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentDark3, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
            }
            else
            {
                Accent1 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentLight1, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
                Accent2 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentLight2, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
                Accent3 = GetColorFromPreference(in colorPreference, IMMERSIVE_COLOR_TYPE.IMCLR_SystemAccentLight3, isHighContrastEnabled, IMMERSIVE_HC_CACHE_MODE.IHCM_REFRESH);
            }

            primaryAccent = Color.FromArgb(0xff, (byte)Accent1, (byte)(Accent1 >> 8), (byte)(Accent1 >> 16));
            secondaryAccent = Color.FromArgb(0xff, (byte)Accent2, (byte)(Accent2 >> 8), (byte)(Accent2 >> 16));
            tertiaryAccent = Color.FromArgb(0xff, (byte)Accent3, (byte)(Accent3 >> 8), (byte)(Accent3 >> 16));
        }

        UpdateColorResources(systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);

        _currentApplicationAccentColor = systemAccent;
    }

    /// <summary>
    /// Updates application resources.
    /// </summary>        
    private static void UpdateColorResources(
        Color systemAccent,
        Color primaryAccent,
        Color secondaryAccent,
        Color tertiaryAccent)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine("INFO | SystemAccentColor: " + systemAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorPrimary: " + primaryAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorSecondary: " + secondaryAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorTertiary: " + tertiaryAccent, "System.Windows.Accent");
#endif

        if (ThemeColorization.IsThemeDark())
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("INFO | Text on accent is DARK", "System.Windows.Accent");
#endif
            Application.Current.Resources["TextOnAccentFillColorPrimary"] = Color.FromArgb(
                0xFF,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = Color.FromArgb(
                0x80,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = Color.FromArgb(
                0x77,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = Color.FromArgb(
                0x00,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["AccentTextFillColorDisabled"] = Color.FromArgb(
                0x5D,
                0x00,
                0x00,
                0x00
            );
        }
        else
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("INFO | Text on accent is LIGHT", "System.Windows.Accent");
#endif
            Application.Current.Resources["TextOnAccentFillColorPrimary"] = Color.FromArgb(
                0xFF,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = Color.FromArgb(
                0x80,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = Color.FromArgb(
                0x87,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = Color.FromArgb(
                0xFF,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["AccentTextFillColorDisabled"] = Color.FromArgb(
                0x5D,
                0xFF,
                0xFF,
                0xFF
            );
        }

        Application.Current.Resources["SystemAccentColor"] = systemAccent;
        Application.Current.Resources["SystemAccentColorPrimary"] = primaryAccent;
        Application.Current.Resources["SystemAccentColorSecondary"] = secondaryAccent;
        Application.Current.Resources["SystemAccentColorTertiary"] = tertiaryAccent;

        Application.Current.Resources["SystemAccentBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["SystemFillColorAttentionBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentFillColorSelectedTextBackgroundBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["AccentFillColorDefaultBrush"] = ToBrush(secondaryAccent);

        Application.Current.Resources["AccentFillColorSecondaryBrush"] = ToBrush(secondaryAccent, 0.9);
        Application.Current.Resources["AccentFillColorTertiaryBrush"] = ToBrush(secondaryAccent, 0.8);
    }

    /// <summary>
    /// Converts the color of type Int32 to type ByteColor
    /// </summary>
    /// <param name="color">The Int32 color to be converted to corresponding ByteColor</param>
    /// <returns>Corresponding <see cref="System.Windows.ByteColor"/></returns>
    private static ByteColor ParseDWordColor(Int32 color)
    {
        Byte
            a = (byte)((color >> 24) & 0xFF),
            b = (byte)((color >> 16) & 0xFF),
            g = (byte)((color >> 8) & 0xFF),
            r = (byte)((color >> 0) & 0xFF);

        ByteColor current = new ByteColor(a, r, g, b);

        return current;
    }

    /// <summary>
    /// Creates a <see cref="SolidColorBrush"/> from a <see cref="System.Windows.Media.Color"/>.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <returns>Brush converted to color.</returns>
    private static SolidColorBrush ToBrush(Color color)
    {
        return new SolidColorBrush(color);
    }

    /// <summary>
    /// Creates a <see cref="SolidColorBrush"/> from a <see cref="System.Windows.Media.Color"/> with defined brush opacity.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <param name="opacity">Degree of opacity.</param>
    /// <returns>Brush converted to color with modified opacity.</returns>
    private static SolidColorBrush ToBrush(Color color, double opacity)
    {
        return new SolidColorBrush { Color = color, Opacity = opacity };
    }
}