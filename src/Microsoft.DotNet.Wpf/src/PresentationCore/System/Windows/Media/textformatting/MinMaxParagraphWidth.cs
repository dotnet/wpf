// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  MinMaxParagraphWidth represents two values - the smallest and largest possible 
//             paragraph width that can fully contain specified text content.
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Windows;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// MinMaxParagraphWidth represents two values - the smallest and largest possible 
    /// paragraph width that can fully contain specified text content
    /// </summary>
    public struct MinMaxParagraphWidth : IEquatable<MinMaxParagraphWidth>
    {
        internal MinMaxParagraphWidth(
            double      minWidth,
            double      maxWidth
            )
        {
            _minWidth = minWidth;
            _maxWidth = maxWidth;
        }


        /// <summary>
        /// smallest paragraph width possible
        /// </summary>
        public double MinWidth
        {
            get { return _minWidth; }
        }


        /// <summary>
        /// largest paragraph width possible
        /// </summary>
        public double MaxWidth
        {
            get { return _maxWidth; }
        }

        /// <summary>
        /// Compute hash code
        /// </summary>
        public override int GetHashCode()
        {
            return _minWidth.GetHashCode() ^ _maxWidth.GetHashCode();
        }


        /// <summary>
        /// Test equality with the input MinMaxParagraphWidth value
        /// </summary>
        /// <param name="value">The MinMaxParagraphWidth value to test </param>
        public bool Equals(MinMaxParagraphWidth value)
        {
            return this == value;
        }


        /// <summary>
        /// Test equality with the input MinMaxParagraphWidth value
        /// </summary>
        /// <param name="obj">the object to test </param>
        public override bool Equals(object obj)
        {
            if (!(obj is MinMaxParagraphWidth))
                return false;
            return this == (MinMaxParagraphWidth)obj;
        }


        /// <summary>
        /// Compare two MinMaxParagraphWidth for equality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator ==(
            MinMaxParagraphWidth left,
            MinMaxParagraphWidth right
            )
        {
            return  left._minWidth == right._minWidth
                &&  left._maxWidth == right._maxWidth;
        }


        /// <summary>
        /// Compare two MinMaxParagraphWidth for inequality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator !=(
            MinMaxParagraphWidth left,
            MinMaxParagraphWidth right
            )
        {
            return !(left == right);
        }


        private double _minWidth;
        private double _maxWidth;
    }
}

