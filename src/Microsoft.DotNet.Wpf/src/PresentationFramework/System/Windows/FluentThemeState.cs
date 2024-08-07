using System;
using System.Windows;
using System.Windows.Media;

namespace System.Windows
{
    internal readonly record struct FluentThemeState
    {
        public FluentThemeState(string themeName, bool useLightColors)
        {
            ThemeName = themeName;
            UseLightColors = useLightColors;
            AccentColor = SystemColors.AccentColor;
        }

        public string ThemeName {get; init;}
        public bool UseLightColors {get; init;}
        public Color AccentColor {get; init;}

        public override string ToString()
        {
            return $"ThemeName: {ThemeName}, UseLightColors: {UseLightColors}, AccentColor: {AccentColor}";
        }
    }
}