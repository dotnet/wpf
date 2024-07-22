using System;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Security;
using MS.Internal;
using MS.Utility;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows
{
    internal readonly struct FluentThemeState : IEquatable<FluentThemeState>
    {

        public FluentThemeState(string themeName, bool useLightColors, Color accentColor)
        {
            _themeName = themeName;
            _useLightColors = useLightColors;
            _accentColor = accentColor;
        }

        public FluentThemeState(string themeName, bool useLightColors)
        {
            _themeName = themeName;
            _useLightColors = useLightColors;
            _accentColor = SystemColors.AccentColor;
        }

        public string ThemeName => _themeName;
        public bool UseLightColors => _useLightColors;
        public Color AccentColor => _accentColor;

        private readonly string _themeName;
        private readonly bool _useLightColors;
        private readonly Color _accentColor;     


        public bool Equals(FluentThemeState other)
        {
            return string.Equals(ThemeName, other.ThemeName, StringComparison.Ordinal) &&
                   UseLightColors == other.UseLightColors &&
                   AccentColor == other.AccentColor;
        }   

        public override bool Equals(object obj)
        {
            return obj is FluentThemeState other && Equals(other);
        }

        public static bool operator ==(FluentThemeState left, FluentThemeState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FluentThemeState left, FluentThemeState right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ToString());
        }


        public override string ToString()
        {
            return $"ThemeName: {ThemeName}, UseLightColors: {UseLightColors}, AccentColor: {AccentColor}";
        }
    }
}