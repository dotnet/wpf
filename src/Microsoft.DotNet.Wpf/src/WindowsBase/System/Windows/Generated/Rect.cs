// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.WindowsBase;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;
using System.Windows.Converters;
using System.Windows;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows
{
    [Serializable]
    [TypeConverter(typeof(RectConverter))]
    [ValueSerializer(typeof(RectValueSerializer))] // Used by MarkupWriter
    partial struct Rect : IFormattable
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods




        /// <summary>
        /// Compares two Rect instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Rect instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='rect1'>The first Rect to compare</param>
        /// <param name='rect2'>The second Rect to compare</param>
        public static bool operator == (Rect rect1, Rect rect2)
        {
            return rect1.X == rect2.X &&
                   rect1.Y == rect2.Y &&
                   rect1.Width == rect2.Width &&
                   rect1.Height == rect2.Height;
        }

        /// <summary>
        /// Compares two Rect instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Rect instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='rect1'>The first Rect to compare</param>
        /// <param name='rect2'>The second Rect to compare</param>
        public static bool operator != (Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }
        /// <summary>
        /// Compares two Rect instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two Rect instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='rect1'>The first Rect to compare</param>
        /// <param name='rect2'>The second Rect to compare</param>
        public static bool Equals (Rect rect1, Rect rect2)
        {
            if (rect1.IsEmpty)
            {
                return rect2.IsEmpty;
            }
            else
            {
                return rect1.X.Equals(rect2.X) &&
                       rect1.Y.Equals(rect2.Y) &&
                       rect1.Width.Equals(rect2.Width) &&
                       rect1.Height.Equals(rect2.Height);
            }
        }

        /// <summary>
        /// Equals - compares this Rect with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of Rect and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if ((null == o) || !(o is Rect))
            {
                return false;
            }

            Rect value = (Rect)o;
            return Rect.Equals(this,value);
        }

        /// <summary>
        /// Equals - compares this Rect with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The Rect to compare to "this"</param>
        public bool Equals(Rect value)
        {
            return Rect.Equals(this, value);
        }
        /// <summary>
        /// Returns the HashCode for this Rect
        /// </summary>
        /// <returns>
        /// int - the HashCode for this Rect
        /// </returns>
        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return X.GetHashCode() ^
                       Y.GetHashCode() ^
                       Width.GetHashCode() ^
                       Height.GetHashCode();
            }
        }

        /// <summary>
        /// Parse - returns an instance converted from the provided string using
        /// the culture "en-US"
        /// <param name="source"> string with Rect data </param>
        /// </summary>
        public static Rect Parse(string source)
        {
            IFormatProvider formatProvider = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            TokenizerHelper th = new TokenizerHelper(source, formatProvider);

            Rect value;

            String firstToken = th.NextTokenRequired();

            // The token will already have had whitespace trimmed so we can do a
            // simple string compare.
            if (firstToken == "Empty")
            {
                value = Empty;
            }
            else
            {
                value = new Rect(
                    Convert.ToDouble(firstToken, formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider));
            }

            // There should be no more tokens in this string.
            th.LastTokenRequired();

            return value;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------




        #region Public Properties



        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods





        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties


        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
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
        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);
            return String.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
                                 separator,
                                 _x,
                                 _y,
                                 _width,
                                 _height);
        }



        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties



        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields


        internal double _x;
        internal double _y;
        internal double _width;
        internal double _height;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------




        #endregion Constructors
    }
}
