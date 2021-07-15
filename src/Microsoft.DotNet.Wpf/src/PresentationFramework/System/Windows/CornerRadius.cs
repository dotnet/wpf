// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Contains the CornerRadius (double x4) value type. 
//
//

using MS.Internal;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// CornerRadius is a value type used to describe the radius of a rectangle's corners (controlled independently).
    /// It contains four double structs each corresponding to a corner: TopLeft, TopRight, BottomLeft, BottomRight.
    /// The corner radii cannot be negative.
    /// </summary>
    [TypeConverter(typeof(CornerRadiusConverter))]
    public struct CornerRadius : IEquatable<CornerRadius>
    {        
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------
        #region Constructors
        /// <summary>
        /// This constructor builds a CornerRadius with a specified uniform double radius value on every corner.
        /// </summary>
        /// <param name="uniformRadius">The specified uniform radius.</param>
        public CornerRadius(double uniformRadius)
        {
            _topLeft = _topRight = _bottomLeft = _bottomRight = uniformRadius;
        }

        /// <summary>
        /// This constructor builds a CornerRadius with the specified doubles on each corner.
        /// </summary>
        /// <param name="topLeft">The thickness for the top left corner.</param>
        /// <param name="topRight">The thickness for the top right corner.</param>
        /// <param name="bottomRight">The thickness for the bottom right corner.</param>
        /// <param name="bottomLeft">The thickness for the bottom left corner.</param>
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            _topLeft = topLeft;
            _topRight = topRight;
            _bottomRight = bottomRight;
            _bottomLeft = bottomLeft;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------
        #region Public Methods

        /// <summary>
        /// This function compares to the provided object for type and value equality.
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>True if object is a CornerRadius and all sides of it are equal to this CornerRadius'.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CornerRadius)
            {
                CornerRadius otherObj = (CornerRadius)obj;
                return (this == otherObj);
            }
            return (false);
        }

        /// <summary>
        /// Compares this instance of CornerRadius with another instance.
        /// </summary>
        /// <param name="cornerRadius">CornerRadius instance to compare.</param>
        /// <returns><c>true</c>if this CornerRadius instance has the same value 
        /// and unit type as cornerRadius.</returns>
        public bool Equals(CornerRadius cornerRadius)
        {
            return (this == cornerRadius);
        }

        /// <summary>
        /// This function returns a hash code.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _topLeft.GetHashCode() ^ _topRight.GetHashCode() ^ _bottomLeft.GetHashCode() ^ _bottomRight.GetHashCode();
        }

        /// <summary>
        /// Converts this Thickness object to a string.
        /// </summary>
        /// <returns>String conversion.</returns>
        public override string ToString()
        {
            return CornerRadiusConverter.ToString(this, CultureInfo.InvariantCulture);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Operators
        //
        //-------------------------------------------------------------------
        #region Public Operators

        /// <summary>
        /// Overloaded operator to compare two CornerRadiuses for equality.
        /// </summary>
        /// <param name="cr1">First CornerRadius to compare</param>
        /// <param name="cr2">Second CornerRadius to compare</param>
        /// <returns>True if all sides of the CornerRadius are equal, false otherwise</returns>
        //  SEEALSO
        public static bool operator==(CornerRadius cr1, CornerRadius cr2)
        {
            return (    (cr1._topLeft     == cr2._topLeft     || (DoubleUtil.IsNaN(cr1._topLeft)     && DoubleUtil.IsNaN(cr2._topLeft)))
                    &&  (cr1._topRight    == cr2._topRight    || (DoubleUtil.IsNaN(cr1._topRight)    && DoubleUtil.IsNaN(cr2._topRight)))
                    &&  (cr1._bottomRight == cr2._bottomRight || (DoubleUtil.IsNaN(cr1._bottomRight) && DoubleUtil.IsNaN(cr2._bottomRight)))
                    &&  (cr1._bottomLeft  == cr2._bottomLeft  || (DoubleUtil.IsNaN(cr1._bottomLeft)  && DoubleUtil.IsNaN(cr2._bottomLeft)))
                    );
        }

        /// <summary>
        /// Overloaded operator to compare two CornerRadiuses for inequality.
        /// </summary>
        /// <param name="cr1">First CornerRadius to compare</param>
        /// <param name="cr2">Second CornerRadius to compare</param>
        /// <returns>False if all sides of the CornerRadius are equal, true otherwise</returns>
        //  SEEALSO
        public static bool operator!=(CornerRadius cr1, CornerRadius cr2)
        {
            return (!(cr1 == cr2));
        }

        #endregion Public Operators


        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>This property is the Length on the thickness' top left corner</summary>
        public double TopLeft
        { 
            get { return _topLeft; }
            set { _topLeft = value; }
        }

        /// <summary>This property is the Length on the thickness' top right corner</summary>
        public double TopRight
        { 
            get { return _topRight; }
            set { _topRight = value; }
        }

        /// <summary>This property is the Length on the thickness' bottom right corner</summary>
        public double BottomRight
        { 
            get { return _bottomRight; }
            set { _bottomRight = value; }
        }

        /// <summary>This property is the Length on the thickness' bottom left corner</summary>
        public double BottomLeft
        {
            get { return _bottomLeft; }
            set { _bottomLeft = value; }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods Properties
        //
        //-------------------------------------------------------------------

        #region Internal Methods Properties

        internal bool IsValid(bool allowNegative, bool allowNaN, bool allowPositiveInfinity, bool allowNegativeInfinity)
        {
            if (!allowNegative)
            {
                if (_topLeft < 0d || _topRight < 0d || _bottomLeft < 0d || _bottomRight < 0d)
                {
                    return (false);
                }
            }

            if (!allowNaN)
            {
                if (DoubleUtil.IsNaN(_topLeft) || DoubleUtil.IsNaN(_topRight) || DoubleUtil.IsNaN(_bottomLeft) || DoubleUtil.IsNaN(_bottomRight))
                {
                    return (false);
                }
            }

            if (!allowPositiveInfinity)
            {
                if (Double.IsPositiveInfinity(_topLeft) || Double.IsPositiveInfinity(_topRight) || Double.IsPositiveInfinity(_bottomLeft) || Double.IsPositiveInfinity(_bottomRight))
                {
                    return (false);
                }
            }

            if (!allowNegativeInfinity)
            {
                if (Double.IsNegativeInfinity(_topLeft) || Double.IsNegativeInfinity(_topRight) || Double.IsNegativeInfinity(_bottomLeft) || Double.IsNegativeInfinity(_bottomRight))
                {
                    return (false);
                }
            }

            return (true);
        }

        internal bool IsZero
        {
            get
            {
                return (    DoubleUtil.IsZero(_topLeft)
                        &&  DoubleUtil.IsZero(_topRight)
                        &&  DoubleUtil.IsZero(_bottomRight)
                        &&  DoubleUtil.IsZero(_bottomLeft)
                        );
            }
        }

        #endregion Internal Methods Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        private double _topLeft;
        private double _topRight;
        private double _bottomLeft;
        private double _bottomRight;
        #endregion
    }
}
