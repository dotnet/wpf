// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// <summary>
    /// ThemeMode structure describes the Fluent theme mode to be applied
    /// on Application or a Window.
    /// </summary>
    /// <remarks>
    /// This is an experimental API and may be modified or removed in future releases.
    /// Since this is an experimental API, rather than creating a new instance of ThemeMode,
    /// use the static properties Light, Dark, System and None.
    /// </remarks>
    [Experimental("WPF0001")]
    public readonly struct ThemeMode : IEquatable<ThemeMode>
    {
        /// <summary>
        /// Predefined theme mode: None.
        /// This is the default value for <see cref="Application.ThemeMode"/> and <see cref="Window.ThemeMode"/>
        /// and it means that Fluent theme will not be applied on the Application or Window.
        /// </summary>
        /// <remarks>
        /// In case, when <see cref="Application.ThemeMode"/> is not set to None,
        /// even if <see cref="Window.ThemeMode"/> is set to None, the Fluent theme will be applied on the Window.
        /// </remarks>
        public static ThemeMode None => new ThemeMode();

        /// <summary>
        /// Predefined theme mode: Light.
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent Light theme will be applied.
        /// </summary>
        public static ThemeMode Light => new ThemeMode("Light");
        
        /// <summary>
        /// Predefined theme mode: Dark.
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent Dark theme will be applied.
        /// </summary>
        public static ThemeMode Dark => new ThemeMode("Dark");

        /// <summary>
        /// Perdefined theme mode : System.
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent theme will be applied based on the system theme.
        /// </summary>
        public static ThemeMode System => new ThemeMode("System");

        /// <summary>
        /// Gets the value of the ThemeMode.
        /// </summary>
        public string Value => _value ?? "None";

        /// <summary>
        /// Creates a new ThemeMode object with the specified value.
        /// </summary>
        /// <param name="value">Name of theme mode</param>
        public ThemeMode(string value) => _value = value;
        
        /// <summary>
        /// Checks whether the ThemeMode object is equal to another ThemeMode object.
        /// </summary>
        /// <param name="other">ThemeMode object to compare with</param>
        /// <returns>
        /// Returns true when the ThemeMode objects are equal, and false otherwise.
        /// </returns>
        public bool Equals(ThemeMode other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>
        /// Checks whether the object is equal to another ThemeMode object.
        /// </summary>
        /// <param name="obj">ThemeMode object to compare with.</param>
        /// <returns>
        /// Returns true when the ThemeMode objects are equal, and false otherwise.
        /// </returns>
        public override bool Equals(object obj) => obj is ThemeMode other && Equals(other);

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value != null ? StringComparer.Ordinal.GetHashCode(_value) : 0;

        /// <summary>
        /// Checks whether two ThemeMode objects are equal.
        /// </summary>
        /// <param name="left">First ThemeMode object to compare</param>
        /// <param name="right">Second ThemeMode object to compare</param>
        /// <returns>
        /// Returns true when both the ThemeMode objects are equal, 
        /// and false otherwise.
        /// </returns>
        public static bool operator ==(ThemeMode left, ThemeMode right) => left.Equals(right);

        /// <summary>
        /// Checks whether two ThemeMode objects are not equal.
        /// </summary>
        /// <param name="left">First ThemeMode object to compare</param>
        /// <param name="right">Second ThemeMode object to compare</param>
        /// <returns>
        /// Returns true when the ThemeMode objects are not equal, 
        /// and false otherwise.
        /// </returns>
        public static bool operator !=(ThemeMode left, ThemeMode right) => !left.Equals(right);

        /// <summary>
        /// Creates a string representation of the ThemeMode object.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString() => Value;

        private readonly string _value;
    }
}
