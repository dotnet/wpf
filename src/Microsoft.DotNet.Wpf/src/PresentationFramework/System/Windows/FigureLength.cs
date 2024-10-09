// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Figure length implementation
//
//
//

using MS.Internal;
using System.ComponentModel;
using System.Globalization;
using MS.Internal.PtsHost.UnsafeNativeMethods;     // PTS restrictions

namespace System.Windows
{
    /// <summary>
    /// FigureUnitType enum is used to indicate what kind of value the 
    /// FigureLength is holding.
    /// </summary>
    public enum FigureUnitType 
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
        /// The value is expressed as fraction of column width.
        /// </summary>
        Column,

        /// <summary>
        /// The value is expressed as a fraction of content width.
        /// </summary>
        Content,

        /// <summary>
        /// The value is expressed as a fraction of page width.
        /// </summary>
        Page,
    }

    /// <summary>
    /// FigureLength is the type used for height and width on figure element.
    /// </summary>
    [TypeConverter(typeof(FigureLengthConverter))]
    public readonly struct FigureLength : IEquatable<FigureLength>
    {
        /// <summary>
        /// Represents the <see cref="Value"/> of this instance; 1.0 if <see cref="FigureUnitType"/> is <see cref="FigureUnitType.Auto"/>.
        /// </summary>
        private readonly double _unitValue;
        /// <summary>
        /// Represents the <see cref="FigureUnitType"/> of this instance.
        /// </summary>
        private readonly FigureUnitType _unitType;

        /// <summary>
        /// Constructor, initializes the <see cref="FigureLength"/> as absolute value in pixels.
        /// </summary>
        /// <param name="pixels">Specifies the number of 'device-independent pixels' 
        /// (96 pixels-per-inch).</param>
        /// <exception cref="ArgumentException">
        /// If <c>pixels</c> parameter is <c>double.NaN</c> or <c>pixels</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>pixels</c> parameter is <c>double.PositiveInfinity</c> or <c>value</c> parameter is <c>negative</c>.
        /// </exception>
        public FigureLength(double pixels) : this(pixels, FigureUnitType.Pixel) { }

        /// <summary>
        /// Constructor, initializes the <see cref="FigureLength"/> and specifies what kind of value it will hold.
        /// </summary>
        /// <param name="value">Value to be stored by this <see cref="FigureLength"/> instance.</param>
        /// <param name="type">Type of the value to be stored by this <see cref="FigureLength"/> instance.</param>
        /// <remarks> 
        /// If the <paramref name="type"/> parameter is <see cref="FigureUnitType.Auto"/>, 
        /// then the value passed in <paramref name="value"/> is ignored and replaced with 1.0.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <c>pixels</c> parameter is <c>double.NaN</c> or <c>pixels</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>pixels</c> parameter is <c>double.PositiveInfinity</c> or <c>value</c> parameter is <c>negative</c>.
        /// </exception>
        public FigureLength(double value, FigureUnitType type)
        {
            // Check value
            if (double.IsNaN(value))
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterNoNaN, nameof(value)));
            else if (double.IsInfinity(value))
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterNoInfinity, nameof(value)));
            else if (value < 0.0)
                throw new ArgumentOutOfRangeException(SR.Format(SR.InvalidCtorParameterNoNegative, nameof(value)));

            // Check unitType
            if (type is FigureUnitType.Content or FigureUnitType.Page)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            else if (type is FigureUnitType.Column)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, PTS.Restrictions.tscColumnRestriction);
            else if (type is FigureUnitType.Pixel)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Math.Min(1000000, PTS.MaxPageSize));
            else if (type is FigureUnitType.Auto)
                value = 1.0; // Value is ignored in case of "Auto" and defaulted to "1.0"
            else
                throw new ArgumentException(SR.Format(SR.InvalidCtorParameterUnknownFigureUnitType, nameof(type)));

            _unitValue = value;
            _unitType = type;
        }

        /// <summary>
        /// Compares two <see cref="FigureLength"/> structures for equality.
        /// </summary>
        /// <param name="fl1">The first <see cref="FigureLength"/> to compare.</param>
        /// <param name="fl2">The second <see cref="FigureLength"/> to compare.</param>
        /// <returns><see langword="true"/> if specified <see cref="FigureLength"/>s have same
        /// <see cref="Value"/> and <see cref="FigureUnitType"/>.</returns>
        public static bool operator ==(FigureLength fl1, FigureLength fl2)
        {
            return fl1.FigureUnitType == fl2.FigureUnitType && fl1.Value == fl2.Value;
        }

        /// <summary>
        /// Compares two <see cref="FigureLength"/> structures for inequality.
        /// </summary>
        /// <param name="fl1">The first <see cref="FigureLength"/> to compare.</param>
        /// <param name="fl2">The second <see cref="FigureLength"/> to compare.</param>
        /// <returns><see langword="true"/> if specified <see cref="FigureLength"/>s differ
        /// in <see cref="Value"/> or <see cref="FigureUnitType"/>.</returns>
        public static bool operator !=(FigureLength fl1, FigureLength fl2)
        {
            return !(fl1 == fl2);
        }

        /// <summary>
        /// Compares this instance of <see cref="FigureLength"/> with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><see langword="true"/> if this <see cref="FigureLength"/> instance has the same value
        /// and unit type as <paramref name="oCompare"/>.</returns>
        public override bool Equals(object oCompare)
        {
            return oCompare is FigureLength figureLength && Equals(figureLength);
        }

        /// <summary>
        /// Compares this instance of <see cref="FigureLength"/> with another <see cref="FigureLength"/>.
        /// </summary>
        /// <param name="figureLength">FigureLength to compare.</param>
        /// <returns><see langword="true"/> if this <see cref="FigureLength"/> instance has the same value 
        /// and unit type as <paramref name="figureLength"/>.</returns>
        public bool Equals(FigureLength figureLength)
        {
            return this == figureLength;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="FigureLength"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="FigureLength"/> structure.</returns>
        public override int GetHashCode()
        {
            return (int)_unitValue + (int)_unitType;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="FigureLength"/> instance holds an absolute (pixel) value.
        /// </summary>
        public bool IsAbsolute { get { return _unitType == FigureUnitType.Pixel; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="FigureLength"/> instance is automatic (not specified).
        /// </summary>
        public bool IsAuto { get { return _unitType == FigureUnitType.Auto; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="FigureLength"/> instance is column relative.
        /// </summary>
        public bool IsColumn { get { return _unitType == FigureUnitType.Column; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="FigureLength"/> instance is content relative.
        /// </summary>
        public bool IsContent { get { return _unitType == FigureUnitType.Content; } }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="FigureLength"/> instance is page relative.
        /// </summary>
        public bool IsPage { get { return _unitType == FigureUnitType.Page; } }

        /// <summary>
        /// Returns value part of this <see cref="FigureLength"/> instance.
        /// </summary>
        public double Value { get { return _unitValue; } }

        /// <summary>
        /// Returns unit type of this <see cref="FigureLength"/> instance.
        /// </summary>
        public FigureUnitType FigureUnitType { get { return _unitType; } }

        /// <summary>
        /// Returns the <see langword="string"/> representation of this <see cref="FigureLength"/>.
        /// </summary>
        /// <returns>A <see langword="string"/> representation of this <see cref="FigureLength"/>.</returns>
        public override string ToString()
        {
            return FigureLengthConverter.ToString(this, CultureInfo.InvariantCulture);
        }
    }
}
