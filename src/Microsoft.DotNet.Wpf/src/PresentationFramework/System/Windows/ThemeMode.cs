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
    /// Describes the Fluent theme mode to apply to an application or window.
    /// </summary>
    /// <remarks>
    /// This is an experimental API and may be modified or removed in future releases.
    /// Since this is an experimental API, rather than creating a new instance of ThemeMode,
    /// use the static properties Light, Dark, System, and None.
    /// </remarks>
    [Experimental("WPF0001")]
    public readonly struct ThemeMode : IEquatable<ThemeMode>
    {
        /// <summary>
        /// Gets the None predefined theme mode.
        /// </summary>
        /// <remarks>
        /// This is the default value for <see cref="Application.ThemeMode"/> and <see cref="Window.ThemeMode"/>
        /// and it means that Fluent theme will not be applied on the Application or Window.
        ///
        /// When <see cref="Application.ThemeMode"/> is not set to <see cref="None"/>,
        /// even if <see cref="Window.ThemeMode"/> is set to <see cref="None">, the Fluent theme will be applied on the window.
        /// </remarks>
        public static ThemeMode None => new ThemeMode();

        /// <summary>
        /// Gets the Light predefined theme mode.
        /// </summary>
        /// <remarks>
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent Light theme will be applied.
        /// </remarks>
        public static ThemeMode Light => new ThemeMode("Light");
        
        /// <summary>
        /// Gets the Dark predefined theme mode.
        /// </summary>
        /// <remarks>
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent Dark theme will be applied.
        /// </remarks>
        public static ThemeMode Dark => new ThemeMode("Dark");

        /// <summary>
        /// Get the System predefined theme mode.
        /// </summary>
        /// <remarks>
        /// Whenever this mode is set on <see cref="Application"/> or <see cref="Window"/>,
        /// Fluent theme will be applied based on the system theme.
        /// </remarks>
        public static ThemeMode System => new ThemeMode("System");

        /// <summary>
        /// Gets the value of the ThemeMode.
        /// </summary>
        public string Value => _value ?? "None";

        /// <summary>
        /// Creates a new ThemeMode object with the specified value.
        /// </summary>
        /// <param name="value">The name of the theme mode</param>
        public ThemeMode(string value) => _value = value;

        /// <summary>
        /// Checks whether this instance is equal to another ThemeMode object.
        /// </summary>
        /// <param name="other">ThemeMode object to compare with</param>
        /// <returns>
        /// <see langword="true"/> if the ThemeMode objects are equal; <see langword="false"/> otherwise.
        /// </returns>
        public bool Equals(ThemeMode other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>
        /// Checks whether this instance  is equal to another ThemeMode object.
        /// </summary>
        /// <param name="obj">ThemeMode object to compare with.</param>
        /// <returns>
        /// <see langword="true"/> if the ThemeMode objects are equal; <see langword="false"/> otherwise.
        /// </returns>
        public override bool Equals(object obj) => obj is ThemeMode other && Equals(other);

        /// <summary>
        /// Computes the hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value != null ? StringComparer.Ordinal.GetHashCode(_value) : 0;

        /// <summary>
        /// Checks whether two ThemeMode objects are equal.
        /// </summary>
        /// <param name="left">The first ThemeMode object to compare.</param>
        /// <param name="right">The second ThemeMode object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the ThemeMode objects are equal; <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(ThemeMode left, ThemeMode right) => left.Equals(right);

        /// <summary>
        /// Checks whether two ThemeMode objects are not equal.
        /// </summary>
        /// <param name="left">The first ThemeMode object to compare.</param>
        /// <param name="right">The second ThemeMode object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the ThemeMode objects are not equal; <see langword="false"/> otherwise.
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
