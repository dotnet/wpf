// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#endif

    /// <summary>
    ///   RibbonControlLength is used to represent widths in RibbonControlSizeDefinition for layout
    ///   purposes. This class is similar to GridLength in the framework.
    /// </summary>
    [TypeConverter(typeof(RibbonControlLengthConverter))]
    public struct RibbonControlLength : IEquatable<RibbonControlLength>
    {
        #region Private Fields

        private double _unitValue;
        private RibbonControlLengthUnitType _unitType;
        private static RibbonControlLength _auto = new RibbonControlLength(1.0, RibbonControlLengthUnitType.Auto);

        #endregion

        #region Constructors

        /// <summary>
        ///   Constructor, initializes the RibbonControlLength as absolute value in pixels.
        /// </summary>
        public RibbonControlLength(double pixels)
            : this(pixels, RibbonControlLengthUnitType.Pixel)
        {
        }

        /// <summary>
        ///   Constructor, initializes the RibbonControlLength and specifies what kind of value 
        ///   it will hold.
        /// </summary>
        public RibbonControlLength(double value, RibbonControlLengthUnitType type)
        {
            if (DoubleUtil.IsNaN(value))
            {
                throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidCtorParameterNoNaN, "value"));
            }

            if (type == RibbonControlLengthUnitType.Star && double.IsInfinity(value))
            {
                throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidCtorParameterNoInfinityForStarSize, "value"));
            }

            if (type != RibbonControlLengthUnitType.Auto
                && type != RibbonControlLengthUnitType.Pixel
                && type != RibbonControlLengthUnitType.Item
                && type != RibbonControlLengthUnitType.Star)
            {
                throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidCtorParameterUnknownRibbonControlLengthUnitType, "type"));
            }

            _unitValue = (type == RibbonControlLengthUnitType.Auto) ? 0.0 : value;
            _unitType = type;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Overloaded operator, compares 2 RibbonControlLengths.
        /// </summary>
        public static bool operator ==(RibbonControlLength length1, RibbonControlLength length2)
        {
            return (length1.RibbonControlLengthUnitType == length2.RibbonControlLengthUnitType &&
                length1.Value == length2.Value);
        }

        /// <summary>
        ///   Overloaded operator, compares 2 RibbonControlLengths.
        /// </summary>
        public static bool operator !=(RibbonControlLength length1, RibbonControlLength length2)
        {
            return (length1.RibbonControlLengthUnitType != length2.RibbonControlLengthUnitType ||
                length1.Value != length2.Value);
        }

        /// <summary>
        ///   Compares this instance of RibbonControlLength with another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is RibbonControlLength)
            {
                RibbonControlLength length = (RibbonControlLength)obj;
                return (this == length);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///   Compares this instance of RibbonControlLength with another instance.
        /// </summary>
        public bool Equals(RibbonControlLength other)
        {
            return (this == other);
        }

        /// <summary>
        ///   <see cref="Object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            return ((int)_unitValue + (int)_unitType);
        }

        /// <summary>
        ///   Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return RibbonControlLengthConverter.ToString(this, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Returns <c>true</c> if this RibbonControlLength instance holds 
        ///   an absolute (pixel or logical) value.
        /// </summary>
        public bool IsAbsolute
        {
            get { return (_unitType == RibbonControlLengthUnitType.Pixel || _unitType == RibbonControlLengthUnitType.Item); } 
        }

        /// <summary>
        ///   Returns <c>true</c> if this RibbonControlLength instance is 
        ///   automatic (not specified).
        /// </summary>
        public bool IsAuto
        {
            get { return (_unitType == RibbonControlLengthUnitType.Auto); } 
        }

        /// <summary>
        ///   Returns <c>true</c> if this RibbonControlLength instance holds weighted proportion 
        ///   of available space.
        /// </summary>
        public bool IsStar
        {
            get { return (_unitType == RibbonControlLengthUnitType.Star); } 
        }

        /// <summary>
        ///   Returns value part of this RibbonControlLength instance.
        /// </summary>
        public double Value 
        {
            get { return ((_unitType == RibbonControlLengthUnitType.Auto) ? 1.0 : _unitValue); }
        }

        /// <summary>
        ///   Returns unit type of this RibbonControlLength instance.
        /// </summary>
        public RibbonControlLengthUnitType RibbonControlLengthUnitType 
        {
            get { return _unitType; } 
        }

        /// <summary>
        ///   Returns initialized Auto RibbonControlLength value.
        /// </summary>
        public static RibbonControlLength Auto 
        {
            get { return _auto; } 
        }

        #endregion
    }
}
