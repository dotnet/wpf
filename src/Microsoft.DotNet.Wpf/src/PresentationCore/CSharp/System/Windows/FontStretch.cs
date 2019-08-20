// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: FontStretch structure. 
//

using System;
using System.ComponentModel;
using System.Diagnostics;

using MS.Internal;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows 
{
    /// <summary>
    /// FontStretch structure describes relative change from the normal aspect ratio
    /// as specified by a font designer for the glyphs in a font.
    /// </summary>
    [TypeConverter(typeof(FontStretchConverter))]
    [Localizability(LocalizationCategory.None)]
    public struct FontStretch : IFormattable
    {
        internal FontStretch(int stretch)
        {
            Debug.Assert(1 <= stretch && stretch <= 9);

            // We want the default zero value of new FontStretch() to correspond to FontStretches.Normal.
            // Therefore, the _stretch value is shifted by 5 relative to the OpenType stretch value.
            _stretch = stretch - 5;
        }

        /// <summary>
        /// Creates a new FontStretch object that corresponds to the OpenType usWidthClass value.
        /// </summary>
        /// <param name="stretchValue">An integer value between 1 and 9 that corresponds
        /// to the usWidthClass definition in the OpenType specification.</param>
        /// <returns>A new FontStretch object that corresponds to the stretchValue parameter.</returns>
        // Important note: when changing this method signature please make sure to update FontStretchConverter accordingly.
        public static FontStretch FromOpenTypeStretch(int stretchValue)
        {
            if (stretchValue < 1 || stretchValue > 9)
                throw new ArgumentOutOfRangeException("stretchValue", SR.Get(SRID.ParameterMustBeBetween, 1, 9));
            return new FontStretch(stretchValue);
        }

        /// <summary>
        /// Obtains OpenType usWidthClass value that corresponds to the FontStretch object.
        /// </summary>
        /// <returns>An integer value between 1 and 9 that corresponds
        /// to the usWidthClass definition in the OpenType specification.</returns>
        // Important note: when changing this method signature please make sure to update FontStretchConverter accordingly.
        public int ToOpenTypeStretch()
        {
            Debug.Assert(1 <= RealStretch && RealStretch <= 9);
            return RealStretch;
        }

        /// <summary>
        /// Compares two font stretch values and returns an indication of their relative values.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>A 32-bit signed integer indicating the lexical relationship between the two comparands.
        /// When the return value is less than zero this means that left is less than right.
        /// When the return value is zero this means that left is equal to right.
        /// When the return value is greater than zero this means that left is greater than right.
        /// </returns>
        public static int Compare(FontStretch left, FontStretch right)
        {
            return left._stretch - right._stretch;
        }

        /// <summary>
        /// Checks whether a font stretch is less than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is less than right, false otherwise.</returns>
        public static bool operator<(FontStretch left, FontStretch right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Checks whether a font stretch is less or equal than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is less or equal than right, false otherwise.</returns>
        public static bool operator<=(FontStretch left, FontStretch right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        /// Checks whether a font stretch is greater than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is greater than right, false otherwise.</returns>
        public static bool operator>(FontStretch left, FontStretch right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Checks whether a font stretch is greater or equal than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is greater or equal than right, false otherwise.</returns>
        public static bool operator>=(FontStretch left, FontStretch right)
        {
            return Compare(left, right) >= 0;
        }

        /// <summary>
        /// Checks whether two font stretch objects are equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns true when the font stretch values are equal for both objects,
        /// and false otherwise.</returns>
        public static bool operator==(FontStretch left, FontStretch right)
        {
            return Compare(left, right) == 0;
        }

        /// <summary>
        /// Checks whether two font stretch objects are not equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns false when the font stretch values are equal for both objects,
        /// and true otherwise.</returns>
        public static bool operator!=(FontStretch left, FontStretch right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Checks whether the object is equal to another FontStretch object.
        /// </summary>
        /// <param name="obj">FontStretch object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public bool Equals(FontStretch obj)
        {
            return this == obj;
        }

        /// <summary>
        /// Checks whether an object is equal to another character hit object.
        /// </summary>
        /// <param name="obj">FontStretch object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FontStretch))
                return false;
            return this == (FontStretch)obj;
        }

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return RealStretch;
        }

        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null, null);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        private string ConvertToString(string format, IFormatProvider provider)
        {
            string convertedValue;
            if (!FontStretches.FontStretchToString(RealStretch, out convertedValue))
            {
                // This can happen only if _stretch member is corrupted.
                Invariant.Assert(false);
            }
            return convertedValue;
        }

        /// <summary>
        /// We want the default zero value of new FontStretch() to correspond to FontStretches.Normal.
        /// Therefore, _stretch value is shifted by 5 relative to the OpenType stretch value.
        /// </summary>
        private int RealStretch
        {
            get
            {
                return _stretch + 5;
            }
        }

        private int _stretch;
    }
}

