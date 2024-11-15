// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Grid length implementation
//
//              See spec at http://avalon/layout/Specs/Star%20LengthUnit.mht
//
//

using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// GridUnitType enum is used to indicate what kind of value the 
    /// GridLength is holding.
    /// </summary>
    // Note: Keep the GridUnitType enum in sync with the string representation 
    //       of units (GridLengthConverter._unitString). 
    public enum GridUnitType 
    {
        /// <summary>
        /// The value indicates that content should be calculated without constraints. 
        /// </summary>
        Auto = 0,
        /// <summary>
        /// The value is expressed as a pixel.
        /// </summary>
        Pixel, 
        /// <summary>
        /// The value is expressed as a weighted proportion of available space.
        /// </summary>
        Star,
    }

    /// <summary>
    /// <see cref="GridLength"/> is the type used for various length-like properties in the system, 
    /// that explicitly support <see cref="GridUnitType.Star"/> unit type. For example, "Width", "Height" 
    /// properties of <see cref="ColumnDefinition"/> and <see cref="RowDefinition"/> used by <see cref="Grid"/>.
    /// </summary>
    [TypeConverter(typeof(GridLengthConverter))]
    public readonly struct GridLength : IEquatable<GridLength>
    {
        /// <summary>
        /// Represents the <see cref="Value"/> of this instance; 1.0 if <see cref="Windows.GridUnitType"/> is <see cref="GridUnitType.Auto"/>.
        /// </summary>
        private readonly double _unitValue;
        /// <summary>
        /// Represents the <see cref="GridUnitType"/> of this instance.
        /// </summary>
        private readonly GridUnitType _unitType;

        /// <summary>
        /// Constructor, initializes the <see cref="GridLength"/> as absolute value in pixels.
        /// </summary>
        /// <param name="pixels">Specifies the number of 'device-independent pixels' 
        /// (96 pixels-per-inch).</param>
        /// <exception cref="ArgumentException">
        /// If <c>pixels</c> parameter is <c>double.NaN</c>
        /// or <c>pixels</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>pixels</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public GridLength(double pixels) : this(pixels, GridUnitType.Pixel) { }

        /// <summary>
        /// Constructor, initializes the <see cref="GridLength"/> and specifies what kind of value it will hold.
        /// </summary>
        /// <param name="value">Value to be stored by this <see cref="GridLength"/> instance.</param>
        /// <param name="type">Type of the value to be stored by this <see cref="GridLength"/> instance.</param>
        /// <remarks> 
        /// If the <paramref name="type"/> parameter is <see cref="GridUnitType.Auto"/>, 
        /// then passed in <paramref name="value"/> is ignored and replaced with 1.0.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <c>value</c> parameter is <c>double.NaN</c>
        /// or <c>value</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>value</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public GridLength(double value, GridUnitType type)
        {
            // Check value
            if (double.IsNaN(value))
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterNoNaN, nameof(value)));
            else if (double.IsInfinity(value))
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterNoInfinity, nameof(value)));

            // Check unitType
            if (type is GridUnitType.Pixel or GridUnitType.Star)
                _unitValue = value;
            else if (type is GridUnitType.Auto)
                _unitValue = 1.0; // Value is ignored in case of "Auto" and defaulted to "1.0"
            else
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterUnknownGridUnitType, nameof(type)));

            _unitType = type;
        }

        /// <summary>
        /// Compares two <see cref="GridLength"/> structures for equality.
        /// </summary>
        /// <param name="gl1">The first <see cref="GridLength"/> to compare.</param>
        /// <param name="gl2">The second <see cref="GridLength"/> to compare.</param>
        /// <returns><see langword="true"/> if specified <see cref="GridLength"/>s have the same
        /// <see cref="Value"/> and <see cref="GridUnitType"/> values.</returns>
        public static bool operator ==(GridLength gl1, GridLength gl2)
        {
            return gl1.GridUnitType == gl2.GridUnitType && gl1.Value == gl2.Value;
        }

        /// <summary>
        /// Compares two<see cref="GridLength"/> structures for inequality.
        /// </summary>
        /// <param name="gl1">The first <see cref="GridLength"/> to compare.</param>
        /// <param name="gl2">The second <see cref="GridLength"/> to compare.</param>
        /// <returns><see langword="true"/> if specified <see cref="GridLength"/>s differ
        /// in either <see cref="Value"/> or <see cref="GridUnitType"/>.</returns>
        public static bool operator !=(GridLength gl1, GridLength gl2)
        {
            return !(gl1 == gl2);
        }

        /// <summary>
        /// Compares this instance of <see cref="GridLength"/> with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><see langword="true"/> if this <see cref="GridLength"/> instance has the same value
        /// and unit type as <paramref name="oCompare"/>, <see langword="false"/> otherwise.</returns>
        public override bool Equals(object oCompare)
        {
            return oCompare is GridLength gridLength && Equals(gridLength);
        }

        /// <summary>
        /// Compares this instance of <see cref="GridLength"/> with another <see cref="GridLength"/> instance.
        /// </summary>
        /// <param name="gridLength">The <see cref="GridLength"/> instance to compare.</param>
        /// <returns><see langword="true"/> if this <see cref="GridLength"/> instance has the same value 
        /// and unit type as <paramref name="gridLength"/>, <see langword="false"/> otherwise.</returns>
        public bool Equals(GridLength gridLength)
        {
            return this == gridLength;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="GridLength"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="GridLength"/> structure.</returns>
        public override int GetHashCode()
        {
            return (int)_unitValue + (int)_unitType;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="GridLength"/> instance holds an absolute (pixel) value.
        /// </summary>
        public bool IsAbsolute { get { return _unitType == GridUnitType.Pixel; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="GridLength"/> instance is automatic (not specified).
        /// </summary>
        public bool IsAuto { get { return _unitType == GridUnitType.Auto; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="GridLength"/> instance holds weighted propertion of available space.
        /// </summary>
        public bool IsStar { get { return _unitType == GridUnitType.Star; } }

        /// <summary>
        /// Returns value part of this <see cref="GridLength"/> instance.
        /// </summary>
        public double Value { get { return _unitValue; } }

        /// <summary>
        /// Returns unit type of this <see cref="GridLength"/> instance.
        /// </summary>
        public GridUnitType GridUnitType { get { return _unitType; } }

        /// <summary>
        /// Returns the <see cref="string"/> representation of this <see cref="GridLength"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of this <see cref="GridLength"/>.</returns>
        public override string ToString()
        {
            return GridLengthConverter.ToString(this, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns initialized instace of <see cref="GridLength"/> with <see cref="GridUnitType.Auto"/> value.
        /// </summary>
        public static GridLength Auto
        {
            get { return s_auto; }
        }

        /// <summary>
        /// Pre-initialized instance, used by <see cref="Auto"/> property.
        /// </summary>
        private static readonly GridLength s_auto = new(1.0, GridUnitType.Auto);

    }
}
