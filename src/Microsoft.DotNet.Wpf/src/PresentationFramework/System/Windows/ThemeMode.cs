using System;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Security;
using MS.Internal;
using MS.Utility;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows
{
    [Experimental("WPF0001")]
    public readonly struct ThemeMode : IEquatable<ThemeMode>
    {
        public static ThemeMode None => new ThemeMode();
        public static ThemeMode Light => new ThemeMode("Light");
        public static ThemeMode Dark => new ThemeMode("Dark");
        public static ThemeMode System => new ThemeMode("System");


        public string Value => _value ?? "None";

        public ThemeMode(string value) => _value = value;
        
        public bool Equals(ThemeMode other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ThemeMode other && Equals(other);

        public override int GetHashCode() => _value != null ? StringComparer.Ordinal.GetHashCode(_value) : 0;

        public static bool operator ==(ThemeMode left, ThemeMode right) => left.Equals(right);

        public static bool operator !=(ThemeMode left, ThemeMode right) => !left.Equals(right);

        public override string ToString() => Value;

        private readonly string _value;
    }
}
