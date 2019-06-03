// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: FontWeight structure. 
//


using System;
using System.ComponentModel;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows 
{
    /// <summary>
    /// FontWeight structure describes the degree of blackness or thickness of strokes of characters in a font.
    /// </summary>
    [TypeConverter(typeof(FontWeightConverter))]
    [Localizability(LocalizationCategory.None)]
    public struct FontWeight : IFormattable
    {
        internal FontWeight(int weight)
        {
            Debug.Assert(1 <= weight && weight <= 999);

            // We want the default zero value of new FontWeight() to correspond to FontWeights.Normal.
            // Therefore, the _weight value is shifted by 400 relative to the OpenType weight value.
            _weight = weight - 400;
        }

        /// <summary>
        /// Creates a new FontWeight object that corresponds to the OpenType usWeightClass value.
        /// </summary>
        /// <param name="weightValue">An integer value between 1 and 999 that corresponds
        /// to the usWeightClass definition in the OpenType specification.</param>
        /// <returns>A new FontWeight object that corresponds to the weightValue parameter.</returns>
        // Important note: when changing this method signature please make sure to update FontWeightConverter accordingly.
        public static FontWeight FromOpenTypeWeight(int weightValue)
        {
            if (weightValue < 1 || weightValue > 999)
                throw new ArgumentOutOfRangeException("weightValue", SR.Get(SRID.ParameterMustBeBetween, 1, 999));
            return new FontWeight(weightValue);
        }

        /// <summary>
        /// Obtains OpenType usWeightClass value that corresponds to the FontWeight object.
        /// </summary>
        /// <returns>An integer value between 1 and 999 that corresponds
        /// to the usWeightClass definition in the OpenType specification.</returns>
        // Important note: when changing this method signature please make sure to update FontWeightConverter accordingly.
        public int ToOpenTypeWeight()
        {
            return RealWeight;
        }

        /// <summary>
        /// Compares two font weight values and returns an indication of their relative values.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>A 32-bit signed integer indicating the lexical relationship between the two comparands.
        /// When the return value is less than zero this means that left is less than right.
        /// When the return value is zero this means that left is equal to right.
        /// When the return value is greater than zero this means that left is greater than right.
        /// </returns>
        public static int Compare(FontWeight left, FontWeight right)
        {
            return left._weight - right._weight;
        }

        /// <summary>
        /// Checks whether a font weight is less than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is less than right, false otherwise.</returns>
        public static bool operator<(FontWeight left, FontWeight right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Checks whether a font weight is less or equal than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is less or equal than right, false otherwise.</returns>
        public static bool operator<=(FontWeight left, FontWeight right)
        {
            return Compare(left, right) <= 0;
        }

        /// <summary>
        /// Checks whether a font weight is greater than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is greater than right, false otherwise.</returns>
        public static bool operator>(FontWeight left, FontWeight right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Checks whether a font weight is greater or equal than another.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>True if left is greater or equal than right, false otherwise.</returns>
        public static bool operator>=(FontWeight left, FontWeight right)
        {
            return Compare(left, right) >= 0;
        }

        /// <summary>
        /// Checks whether two font weight objects are equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns true when the font weight values are equal for both objects,
        /// and false otherwise.</returns>
        public static bool operator==(FontWeight left, FontWeight right)
        {
            return Compare(left, right) == 0;
        }

        /// <summary>
        /// Checks whether two font weight objects are not equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns false when the font weight values are equal for both objects,
        /// and true otherwise.</returns>
        public static bool operator!=(FontWeight left, FontWeight right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Checks whether the object is equal to another FontWeight object.
        /// </summary>
        /// <param name="obj">FontWeight object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public bool Equals(FontWeight obj)
        {
            return this == obj;
        }

        /// <summary>
        /// Checks whether an object is equal to another character hit object.
        /// </summary>
        /// <param name="obj">FontWeight object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FontWeight))
                return false;
            return this == (FontWeight)obj;
        }

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return RealWeight;
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
            if (!FontWeights.FontWeightToString(RealWeight, out convertedValue))
            {
                // This can happen if _weight value is not a multiple of 100.
                return RealWeight.ToString(provider);
            }
            return convertedValue;
        }

        /// <summary>
        /// We want the default zero value of new FontWeight() to correspond to FontWeights.Normal.
        /// Therefore, _weight value is shifted by 400 relative to the OpenType weight value.
        /// </summary>
        private int RealWeight
        {
            get
            {
                return _weight + 400;
            }
        }

        private int _weight;
    }
}

