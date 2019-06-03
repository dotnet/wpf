// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: FontStyle structure. 


using System;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    /// FontStyle structure describes the style of a font face, such as Normal, Italic or Oblique.
    /// </summary>
    [TypeConverter(typeof(FontStyleConverter))]
    [Localizability(LocalizationCategory.None)]
    public struct FontStyle : IFormattable
    {
        internal FontStyle(int style)
        {
            Debug.Assert(0 <= style && style <= 2);

            _style = style;
        }

        /// <summary>
        /// Checks whether two font weight objects are equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns true when the font weight values are equal for both objects,
        /// and false otherwise.</returns>
        public static bool operator ==(FontStyle left, FontStyle right)
        {
            return left._style == right._style;
        }

        /// <summary>
        /// Checks whether two font weight objects are not equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns false when the font weight values are equal for both objects,
        /// and true otherwise.</returns>
        public static bool operator !=(FontStyle left, FontStyle right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Checks whether the object is equal to another FontStyle object.
        /// </summary>
        /// <param name="obj">FontStyle object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public bool Equals(FontStyle obj)
        {
            return this == obj;
        }

        /// <summary>
        /// Checks whether an object is equal to another character hit object.
        /// </summary>
        /// <param name="obj">FontStyle object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FontStyle))
                return false;
            return this == (FontStyle)obj;
        }

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return _style;
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
        /// This method is used only in type converter for construction via reflection.
        /// </summary>
        /// <returns>THe value of _style member.</returns>
        internal int GetStyleForInternalConstruction()
        {
            Debug.Assert(0 <= _style && _style <= 2);
            return _style;
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
            if (_style == 0)
                return "Normal";
            if (_style == 1)
                return "Oblique";
            Debug.Assert(_style == 2);
            return "Italic";
        }

        private int _style;
    }
}

