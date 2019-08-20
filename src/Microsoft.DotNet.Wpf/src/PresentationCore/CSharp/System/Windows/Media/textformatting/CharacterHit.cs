// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// 
//
// Description: The CharacterHit structure represents information about a character hit
// within a glyph run - the index of the first character that got hit and the information
// about leading or trailing edge.
//
//              See spec at "Glyph Run hit testing and caret placement API.htm#CharacterHit.doc"
// 
//
//

#region Using directives

using System;

#endregion

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// The CharacterHit structure represents information about a character hit within a glyph run
    /// - the index of the first character that got hit and the information about leading or trailing edge.
    /// </summary>
    public struct CharacterHit : IEquatable<CharacterHit>
    {
        /// <summary>
        /// Constructs a new CharacterHit structure.
        /// </summary>
        /// <param name="firstCharacterIndex">Index of the first character that got hit.</param>
        /// <param name="trailingLength">In case of leading edge this value is 0.
        /// In case of trailing edge this value is the number of codepoints until the next valid caret position.</param>
        public CharacterHit(int firstCharacterIndex, int trailingLength)
        {
            _firstCharacterIndex = firstCharacterIndex;
            _trailingLength = trailingLength;
        }

        /// <summary>
        /// Index of the first character that got hit.
        /// </summary>
        public int FirstCharacterIndex
        {
            get
            {
                return _firstCharacterIndex;
            }
        }

        /// <summary>
        /// In case of leading edge this value is 0.
        /// In case of trailing edge this value is the number of codepoints until the next valid caret position.
        /// </summary>
        public int TrailingLength
        {
            get
            {
                return _trailingLength;
            }
        }

        /// <summary>
        /// Checks whether two character hit objects are equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns true when the values of FirstCharacterIndex and TrailingLength are equal for both objects,
        /// and false otherwise.</returns>
        public static bool operator==(CharacterHit left, CharacterHit right)
        {
            return left._firstCharacterIndex == right._firstCharacterIndex &&
                left._trailingLength == right._trailingLength;
        }

        /// <summary>
        /// Checks whether two character hit objects are not equal.
        /// </summary>
        /// <param name="left">First object to compare.</param>
        /// <param name="right">Second object to compare.</param>
        /// <returns>Returns false when the values of FirstCharacterIndex and TrailingLength are equal for both objects,
        /// and true otherwise.</returns>
        public static bool operator!=(CharacterHit left, CharacterHit right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Checks whether an object is equal to another character hit object.
        /// </summary>
        /// <param name="obj">CharacterHit object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public bool Equals(CharacterHit obj)
        {
            return this == obj;
        }

        /// <summary>
        /// Checks whether an object is equal to another character hit object.
        /// </summary>
        /// <param name="obj">CharacterHit object to compare with.</param>
        /// <returns>Returns true when the object is equal to the input object,
        /// and false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is CharacterHit))
                return false;
            return this == (CharacterHit)obj;
        }

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return _firstCharacterIndex.GetHashCode() ^ _trailingLength.GetHashCode();
        }

        private int _firstCharacterIndex;
        private int _trailingLength;
    }
}
