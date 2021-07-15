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
    /// FigureLength is the type used for height and width on figure element
    /// </summary>
    [TypeConverter(typeof(FigureLengthConverter))]
    public struct FigureLength : IEquatable<FigureLength>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor, initializes the FigureLength as absolute value in pixels.
        /// </summary>
        /// <param name="pixels">Specifies the number of 'device-independent pixels' 
        /// (96 pixels-per-inch).</param>
        /// <exception cref="ArgumentException">
        /// If <c>pixels</c> parameter is <c>double.NaN</c>
        /// or <c>pixels</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>pixels</c> parameter is <c>double.PositiveInfinity</c>.
        /// or <c>value</c> parameter is <c>negative</c>.
        /// </exception>
        public FigureLength(double pixels)
            : this(pixels, FigureUnitType.Pixel)
        {
        }

        /// <summary>
        /// Constructor, initializes the FigureLength and specifies what kind of value 
        /// it will hold.
        /// </summary>
        /// <param name="value">Value to be stored by this FigureLength 
        /// instance.</param>
        /// <param name="type">Type of the value to be stored by this FigureLength 
        /// instance.</param>
        /// <remarks> 
        /// If the <c>type</c> parameter is <c>FigureUnitType.Auto</c>, 
        /// then passed in value is ignored and replaced with <c>0</c>.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <c>value</c> parameter is <c>double.NaN</c>
        /// or <c>value</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>value</c> parameter is <c>double.PositiveInfinity</c>.
        /// or <c>value</c> parameter is <c>negative</c>.
        /// </exception>
        public FigureLength(double value, FigureUnitType type)
        {
            double maxColumns = PTS.Restrictions.tscColumnRestriction;
            double maxPixel = Math.Min(1000000, PTS.MaxPageSize);

            if (DoubleUtil.IsNaN(value))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCtorParameterNoNaN, "value"));
            }
            if (double.IsInfinity(value))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCtorParameterNoInfinity, "value"));
            }
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.InvalidCtorParameterNoNegative, "value"));
            }
            if (    type != FigureUnitType.Auto
                &&  type != FigureUnitType.Pixel
                &&  type != FigureUnitType.Column
                &&  type != FigureUnitType.Content
                &&  type != FigureUnitType.Page   )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCtorParameterUnknownFigureUnitType, "type"));
            }
            if(value > 1.0 && (type == FigureUnitType.Content || type == FigureUnitType.Page))
            {
                throw new ArgumentOutOfRangeException("value");
            }
            if (value > maxColumns && type == FigureUnitType.Column)
            {
                throw new ArgumentOutOfRangeException("value");
            }
            if (value > maxPixel && type == FigureUnitType.Pixel)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            _unitValue = (type == FigureUnitType.Auto) ? 0.0 : value;
            _unitType = type;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods 

        /// <summary>
        /// Overloaded operator, compares 2 FigureLengths.
        /// </summary>
        /// <param name="fl1">first FigureLength to compare.</param>
        /// <param name="fl2">second FigureLength to compare.</param>
        /// <returns>true if specified FigureLengths have same value 
        /// and unit type.</returns>
        public static bool operator == (FigureLength fl1, FigureLength fl2)
        {
            return (    fl1.FigureUnitType == fl2.FigureUnitType 
                    &&  fl1.Value == fl2.Value  );
        }

        /// <summary>
        /// Overloaded operator, compares 2 FigureLengths.
        /// </summary>
        /// <param name="fl1">first FigureLength to compare.</param>
        /// <param name="fl2">second FigureLength to compare.</param>
        /// <returns>true if specified FigureLengths have either different value or 
        /// unit type.</returns>
        public static bool operator != (FigureLength fl1, FigureLength fl2)
        {
            return (    fl1.FigureUnitType != fl2.FigureUnitType 
                    ||  fl1.Value != fl2.Value  );
        }

        /// <summary>
        /// Compares this instance of FigureLength with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this FigureLength instance has the same value 
        /// and unit type as oCompare.</returns>
        override public bool Equals(object oCompare)
        {
            if(oCompare is FigureLength)
            {
                FigureLength l = (FigureLength)oCompare;
                return (this == l);
            }
            else
                return false;
        }

        /// <summary>
        /// Compares this instance of FigureLength with another object.
        /// </summary>
        /// <param name="figureLength">FigureLength to compare.</param>
        /// <returns><c>true</c>if this FigureLength instance has the same value 
        /// and unit type as figureLength.</returns>
        public bool Equals(FigureLength figureLength)
        {
            return (this == figureLength);
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return ((int)_unitValue + (int)_unitType);
        }

        /// <summary>
        /// Returns <c>true</c> if this FigureLength instance holds 
        /// an absolute (pixel) value.
        /// </summary>
        public bool IsAbsolute { get { return (_unitType == FigureUnitType.Pixel); } }

        /// <summary>
        /// Returns <c>true</c> if this FigureLength instance is 
        /// automatic (not specified).
        /// </summary>
        public bool IsAuto { get { return (_unitType == FigureUnitType.Auto); } }

        /// <summary>
        /// Returns <c>true</c> if this FigureLength instance is column relative.
        /// </summary>
        public bool IsColumn { get { return (_unitType == FigureUnitType.Column); } }

        /// <summary>
        /// Returns <c>true</c> if this FigureLength instance is content relative.
        /// </summary>
        public bool IsContent { get { return (_unitType == FigureUnitType.Content); } }

        /// <summary>
        /// Returns <c>true</c> if this FigureLength instance is page relative.
        /// </summary>
        public bool IsPage { get { return (_unitType == FigureUnitType.Page); } }

        /// <summary>
        /// Returns value part of this FigureLength instance.
        /// </summary>
        public double Value { get { return ((_unitType == FigureUnitType.Auto) ? 1.0 : _unitValue); } }

        /// <summary>
        /// Returns unit type of this FigureLength instance.
        /// </summary>
        public FigureUnitType FigureUnitType { get { return (_unitType); } }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return FigureLengthConverter.ToString(this, CultureInfo.InvariantCulture);
        }
        
        #endregion Public Methods 

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 
        private double _unitValue;      //  unit value storage
        private FigureUnitType _unitType; //  unit type storage
        #endregion Private Fields 
    }
}
