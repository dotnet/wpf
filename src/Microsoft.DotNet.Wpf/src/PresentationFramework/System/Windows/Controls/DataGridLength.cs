// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     DataGridLength is the type used for various length properties in DataGrid
    ///     that support a variety of descriptive sizing modes in addition to numerical
    ///     values.
    /// </summary>
    [TypeConverter(typeof(DataGridLengthConverter))]
    public struct DataGridLength : IEquatable<DataGridLength>
    {
        #region Constructors

        /// <summary>
        ///     Initializes as an absolute value in pixels.
        /// </summary>
        /// <param name="pixels">
        ///     Specifies the number of 'device-independent pixels' (96 pixels-per-inch).
        /// </param>
        /// <exception cref="ArgumentException">
        ///     If <c>pixels</c> parameter is <c>double.NaN</c>
        ///     or <c>pixels</c> parameter is <c>double.NegativeInfinity</c>
        ///     or <c>pixels</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public DataGridLength(double pixels)
            : this(pixels, DataGridLengthUnitType.Pixel)
        {
        }

        /// <summary>
        ///     Initializes to a specified value and unit.
        /// </summary>
        /// <param name="value">The value to hold.</param>
        /// <param name="type">The unit of <c>value</c>.</param>
        /// <remarks> 
        ///     <c>value</c> is ignored unless <c>type</c> is
        ///     <c>DataGridLengthUnitType.Pixel</c> or
        ///     <c>DataGridLengthUnitType.Star</c>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///     If <c>value</c> parameter is <c>double.NaN</c>
        ///     or <c>value</c> parameter is <c>double.NegativeInfinity</c>
        ///     or <c>value</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public DataGridLength(double value, DataGridLengthUnitType type)
            : this(value, type, (type == DataGridLengthUnitType.Pixel ? value : Double.NaN), (type == DataGridLengthUnitType.Pixel ? value : Double.NaN))
        {
        }

        /// <summary>
        ///     Initializes to a specified value and unit.
        /// </summary>
        /// <param name="value">The value to hold.</param>
        /// <param name="type">The unit of <c>value</c>.</param>
        /// <param name="desiredValue"></param>
        /// <param name="displayValue"></param>
        /// <remarks> 
        ///     <c>value</c> is ignored unless <c>type</c> is
        ///     <c>DataGridLengthUnitType.Pixel</c> or
        ///     <c>DataGridLengthUnitType.Star</c>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///     If <c>value</c> parameter is <c>double.NaN</c>
        ///     or <c>value</c> parameter is <c>double.NegativeInfinity</c>
        ///     or <c>value</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public DataGridLength(double value, DataGridLengthUnitType type, double desiredValue, double displayValue)
        {
            if (DoubleUtil.IsNaN(value) || Double.IsInfinity(value))
            {
                throw new ArgumentException(
                    SR.Get(SRID.DataGridLength_Infinity),
                    "value");
            }

            if (type != DataGridLengthUnitType.Auto &&
                type != DataGridLengthUnitType.Pixel &&
                type != DataGridLengthUnitType.Star &&
                type != DataGridLengthUnitType.SizeToCells &&
                type != DataGridLengthUnitType.SizeToHeader)
            {
                throw new ArgumentException(
                    SR.Get(SRID.DataGridLength_InvalidType), 
                    "type");
            }

            if (Double.IsInfinity(desiredValue))
            {
                throw new ArgumentException(
                    SR.Get(SRID.DataGridLength_Infinity), 
                    "desiredValue");
            }

            if (Double.IsInfinity(displayValue))
            {
                throw new ArgumentException(
                    SR.Get(SRID.DataGridLength_Infinity),
                    "displayValue");
            }

            _unitValue = (type == DataGridLengthUnitType.Auto) ? AutoValue : value;
            _unitType = type;

            _desiredValue = desiredValue;
            _displayValue = displayValue;
        }

        #endregion Constructors

        #region Public Methods 

        /// <summary>
        /// Overloaded operator, compares 2 DataGridLength's.
        /// </summary>
        /// <param name="gl1">first DataGridLength to compare.</param>
        /// <param name="gl2">second DataGridLength to compare.</param>
        /// <returns>true if specified DataGridLengths have same value 
        /// and unit type.</returns>
        public static bool operator ==(DataGridLength gl1, DataGridLength gl2)
        {
            return gl1.UnitType == gl2.UnitType 
                   && gl1.Value == gl2.Value 
                   && ((gl1.DesiredValue == gl2.DesiredValue) || (DoubleUtil.IsNaN(gl1.DesiredValue) && DoubleUtil.IsNaN(gl2.DesiredValue)))
                   && ((gl1.DisplayValue == gl2.DisplayValue) || (DoubleUtil.IsNaN(gl1.DisplayValue) && DoubleUtil.IsNaN(gl2.DisplayValue)));
        }

        /// <summary>
        /// Overloaded operator, compares 2 DataGridLength's.
        /// </summary>
        /// <param name="gl1">first DataGridLength to compare.</param>
        /// <param name="gl2">second DataGridLength to compare.</param>
        /// <returns>true if specified DataGridLengths have either different value or 
        /// unit type.</returns>
        public static bool operator !=(DataGridLength gl1, DataGridLength gl2)
        {
            return gl1.UnitType != gl2.UnitType 
                   || gl1.Value != gl2.Value
                   || ((gl1.DesiredValue != gl2.DesiredValue) && !(DoubleUtil.IsNaN(gl1.DesiredValue) && DoubleUtil.IsNaN(gl2.DesiredValue)))
                   || ((gl1.DisplayValue != gl2.DisplayValue) && !(DoubleUtil.IsNaN(gl1.DisplayValue) && DoubleUtil.IsNaN(gl2.DisplayValue)));
        }

        /// <summary>
        /// Compares this instance of DataGridLength with another object.
        /// </summary>
        /// <param name="obj">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this DataGridLength instance has the same value 
        /// and unit type as oCompare.</returns>
        public override bool Equals(object obj)
        {
            if (obj is DataGridLength)
            {
                DataGridLength l = (DataGridLength)obj;
                return this == l;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compares this instance of DataGridLength with another instance.
        /// </summary>
        /// <param name="other">Grid length instance to compare.</param>
        /// <returns><c>true</c>if this DataGridLength instance has the same value 
        /// and unit type as gridLength.</returns>
        public bool Equals(DataGridLength other)
        {
            return this == other;
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return (int)_unitValue + (int)_unitType + (int)_desiredValue + (int)_displayValue;
        }

        /// <summary>
        ///     Returns <c>true</c> if this DataGridLength instance holds 
        ///     an absolute (pixel) value.
        /// </summary>
        public bool IsAbsolute 
        { 
            get 
            { 
                return _unitType == DataGridLengthUnitType.Pixel; 
            } 
        }

        /// <summary>
        ///     Returns <c>true</c> if this DataGridLength instance is 
        ///     automatic (not specified).
        /// </summary>
        public bool IsAuto 
        { 
            get 
            { 
                return _unitType == DataGridLengthUnitType.Auto; 
            } 
        }

        /// <summary>
        ///     Returns <c>true</c> if this DataGridLength instance holds a weighted proportion
        ///     of available space.
        /// </summary>
        public bool IsStar 
        { 
            get 
            { 
                return _unitType == DataGridLengthUnitType.Star; 
            } 
        }

        /// <summary>
        ///     Returns <c>true</c> if this instance is to size to the cells of a column or row.
        /// </summary>
        public bool IsSizeToCells
        {
            get { return _unitType == DataGridLengthUnitType.SizeToCells; }
        }

        /// <summary>
        ///     Returns <c>true</c> if this instance is to size to the header of a column or row.
        /// </summary>
        public bool IsSizeToHeader
        {
            get { return _unitType == DataGridLengthUnitType.SizeToHeader; }
        }

        /// <summary>
        ///     Returns value part of this DataGridLength instance.
        /// </summary>
        public double Value 
        { 
            get 
            { 
                return (_unitType == DataGridLengthUnitType.Auto) ? AutoValue : _unitValue; 
            } 
        }

        /// <summary>
        ///     Returns unit type of this DataGridLength instance.
        /// </summary>
        public DataGridLengthUnitType UnitType 
        { 
            get 
            { 
                return _unitType; 
            } 
        }

        /// <summary>
        ///     Returns the desired value of this instance.
        /// </summary>
        public double DesiredValue 
        { 
            get 
            { 
                return _desiredValue; 
            } 
        }

        /// <summary>
        ///     Returns the display value of this instance.
        /// </summary>
        public double DisplayValue 
        { 
            get 
            { 
                return _displayValue; 
            } 
        }

        /// <summary>
        ///     Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return DataGridLengthConverter.ConvertToString(this, CultureInfo.InvariantCulture);
        }
        
        #endregion

        #region Pre-defined values

        /// <summary>
        ///     Returns a value initialized to mean "auto."
        /// </summary>
        public static DataGridLength Auto
        {
            get { return _auto; }
        }

        /// <summary>
        ///     Returns a value initialized to mean "size to cells."
        /// </summary>
        public static DataGridLength SizeToCells
        {
            get { return _sizeToCells; }
        }

        /// <summary>
        ///     Returns a value initialized to mean "size to header."
        /// </summary>
        public static DataGridLength SizeToHeader
        {
            get { return _sizeToHeader; }
        }

        #endregion

        #region Implicit Conversions

        /// <summary>
        ///     Allows for values of type double to be implicitly converted
        ///     to DataGridLength.
        /// </summary>
        /// <param name="doubleValue">The number of pixels to represent.</param>
        /// <returns>The DataGridLength representing the requested number of pixels.</returns>
        public static implicit operator DataGridLength(double value)
        {
            return new DataGridLength(value);
        }

        #endregion

        #region Fields

        private double _unitValue; // unit value storage
        private DataGridLengthUnitType _unitType; // unit type storage
        private double _desiredValue; // desired value storage
        private double _displayValue; // display value storage
        
        private const double AutoValue = 1.0;

        // static instance of Auto DataGridLength
        private static readonly DataGridLength _auto = new DataGridLength(AutoValue, DataGridLengthUnitType.Auto, 0d, 0d);
        private static readonly DataGridLength _sizeToCells = new DataGridLength(AutoValue, DataGridLengthUnitType.SizeToCells, 0d, 0d);
        private static readonly DataGridLength _sizeToHeader = new DataGridLength(AutoValue, DataGridLengthUnitType.SizeToHeader, 0d, 0d);

        #endregion
    }
}
