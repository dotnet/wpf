// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}