// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Thickness (double x4) value type. 
//

using MS.Internal;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// Thickness is a value type used to describe the thickness of frame around a rectangle.
    /// It contains four doubles each corresponding to a side: Left, Top, Right, Bottom.
    /// </summary>
    [TypeConverter(typeof(ThicknessConverter))]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public struct Thickness : IEquatable<Thickness>
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors
        /// <summary>
        /// This constructur builds a Thickness with a specified value on every side.
        /// </summary>
        /// <param name="uniformLength">The specified uniform length.</param>
        public Thickness(double uniformLength)
        {
            _Left = _Top = _Right = _Bottom = uniformLength;
        }

        /// <summary>
        /// This constructor builds a Thickness with the specified number of pixels on each side.
        /// </summary>
        /// <param name="left">The thickness for the left side.</param>
        /// <param name="top">The thickness for the top side.</param>
        /// <param name="right">The thickness for the right side.</param>
        /// <param name="bottom">The thickness for the bottom side.</param>
        public Thickness(double left, double top, double right, double bottom)
        {
            _Left = left;
            _Top = top;
            _Right = right;
            _Bottom = bottom;
        }


        #endregion


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
        /// <returns>True if object is a Thickness and all sides of it are equal to this Thickness'.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Thickness)
            {
                Thickness otherObj = (Thickness)obj;
                return (this == otherObj);
            }
            return (false);
        }

        /// <summary>
        /// Compares this instance of Thickness with another instance.
        /// </summary>
        /// <param name="thickness">Thickness instance to compare.</param>
        /// <returns><c>true</c>if this Thickness instance has the same value 
        /// and unit type as thickness.</returns>
        public bool Equals(Thickness thickness)
        {
            return (this == thickness);
        }

        /// <summary>
        /// This function returns a hash code.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _Left.GetHashCode() ^ _Top.GetHashCode() ^ _Right.GetHashCode() ^ _Bottom.GetHashCode();
        }

        /// <summary>
        /// Converts this Thickness object to a string.
        /// </summary>
        /// <returns>String conversion.</returns>
        public override string ToString()
        {
            return ThicknessConverter.ToString(this, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts this Thickness object to a string.
        /// </summary>
        /// <returns>String conversion.</returns>
        internal string ToString(CultureInfo cultureInfo)
        {
            return ThicknessConverter.ToString(this, cultureInfo);
        }

        internal bool IsZero
        {
            get
            {
                return      DoubleUtil.IsZero(Left) 
                        &&  DoubleUtil.IsZero(Top) 
                        &&  DoubleUtil.IsZero(Right) 
                        &&  DoubleUtil.IsZero(Bottom);
            }
        }

        internal bool IsUniform
        {
            get
            {
                return     DoubleUtil.AreClose(Left, Top)
                        && DoubleUtil.AreClose(Left, Right)
                        && DoubleUtil.AreClose(Left, Bottom);
            }
        }

        /// <summary>
        /// Verifies if this Thickness contains only valid values
        /// The set of validity checks is passed as parameters.
        /// </summary>
        /// <param name='allowNegative'>allows negative values</param>
        /// <param name='allowNaN'>allows Double.NaN</param>
        /// <param name='allowPositiveInfinity'>allows Double.PositiveInfinity</param>
        /// <param name='allowNegativeInfinity'>allows Double.NegativeInfinity</param>
        /// <returns>Whether or not the thickness complies to the range specified</returns>
        internal bool IsValid(bool allowNegative, bool allowNaN, bool allowPositiveInfinity, bool allowNegativeInfinity)
        {
            if(!allowNegative)
            {
                if(Left < 0d || Right < 0d || Top < 0d || Bottom < 0d)
                    return false;
            }

            if(!allowNaN)
            {
                if(DoubleUtil.IsNaN(Left) || DoubleUtil.IsNaN(Right) || DoubleUtil.IsNaN(Top) || DoubleUtil.IsNaN(Bottom))
                    return false;
            }

            if(!allowPositiveInfinity)
            {
                if(Double.IsPositiveInfinity(Left) || Double.IsPositiveInfinity(Right) || Double.IsPositiveInfinity(Top) || Double.IsPositiveInfinity(Bottom))
                {
                    return false;
                }
            }

            if(!allowNegativeInfinity)
            {
                if(Double.IsNegativeInfinity(Left) || Double.IsNegativeInfinity(Right) || Double.IsNegativeInfinity(Top) || Double.IsNegativeInfinity(Bottom))
                {
                    return false;
                }
            }

            return true;            
        }

        /// <summary>
        /// Compares two thicknesses for fuzzy equality.  This function
        /// helps compensate for the fact that double values can 
        /// acquire error when operated upon
        /// </summary>
        /// <param name='thickness'>The thickness to compare to this</param>
        /// <returns>Whether or not the two points are equal</returns>
        internal bool IsClose(Thickness thickness)
        {
            return (    DoubleUtil.AreClose(Left, thickness.Left)
                    &&  DoubleUtil.AreClose(Top, thickness.Top)
                    &&  DoubleUtil.AreClose(Right, thickness.Right)
                    &&  DoubleUtil.AreClose(Bottom, thickness.Bottom));
        }

        /// <summary>
        /// Compares two thicknesses for fuzzy equality.  This function
        /// helps compensate for the fact that double values can 
        /// acquire error when operated upon
        /// </summary>
        /// <param name='thickness0'>The first thickness to compare</param>
        /// <param name='thickness1'>The second thickness to compare</param>
        /// <returns>Whether or not the two thicknesses are equal</returns>
        static internal bool AreClose(Thickness thickness0, Thickness thickness1)
        {
            return thickness0.IsClose(thickness1);
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Public Operators
        //
        //-------------------------------------------------------------------

        #region Public Operators

        /// <summary>
        /// Overloaded operator to compare two Thicknesses for equality.
        /// </summary>
        /// <param name="t1">first Thickness to compare</param>
        /// <param name="t2">second Thickness to compare</param>
        /// <returns>True if all sides of the Thickness are equal, false otherwise</returns>
        //  SEEALSO
        public static bool operator==(Thickness t1, Thickness t2)
        {
            return (    (t1._Left   == t2._Left   || (DoubleUtil.IsNaN(t1._Left)   && DoubleUtil.IsNaN(t2._Left)))
                    &&  (t1._Top    == t2._Top    || (DoubleUtil.IsNaN(t1._Top)    && DoubleUtil.IsNaN(t2._Top)))
                    &&  (t1._Right  == t2._Right  || (DoubleUtil.IsNaN(t1._Right)  && DoubleUtil.IsNaN(t2._Right)))
                    &&  (t1._Bottom == t2._Bottom || (DoubleUtil.IsNaN(t1._Bottom) && DoubleUtil.IsNaN(t2._Bottom)))
                    );
        }

        /// <summary>
        /// Overloaded operator to compare two Thicknesses for inequality.
        /// </summary>
        /// <param name="t1">first Thickness to compare</param>
        /// <param name="t2">second Thickness to compare</param>
        /// <returns>False if all sides of the Thickness are equal, true otherwise</returns>
        //  SEEALSO
        public static bool operator!=(Thickness t1, Thickness t2)
        {
            return (!(t1 == t2));
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>This property is the Length on the thickness' left side</summary>
        public double Left
        { 
            get { return _Left; }
            set { _Left = value; }
        }

        /// <summary>This property is the Length on the thickness' top side</summary>
        public double Top
        { 
            get { return _Top; }
            set { _Top = value; }
        }

        /// <summary>This property is the Length on the thickness' right side</summary>
        public double Right
        { 
            get { return _Right; }
            set { _Right = value; }
        }

        /// <summary>This property is the Length on the thickness' bottom side</summary>
        public double Bottom
        { 
            get { return _Bottom; }
            set { _Bottom = value; }
        }
        #endregion

        //-------------------------------------------------------------------
        //
        //  INternal API
        //
        //-------------------------------------------------------------------

        #region Internal API

        internal Size Size
        {
            get
            {
                return new Size(_Left + _Right, _Top + _Bottom);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private double _Left;
        private double _Top;
        private double _Right;
        private double _Bottom;

        #endregion
    }
}
