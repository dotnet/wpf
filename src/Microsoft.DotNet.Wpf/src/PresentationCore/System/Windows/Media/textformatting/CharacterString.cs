// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  A range of character buffer
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Diagnostics;
using System.Security;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// A string of characters
    /// </summary>
    public struct CharacterBufferRange : IEquatable<CharacterBufferRange>
    {
        private CharacterBufferReference    _charBufferRef;
        private int                         _length;

        #region Constructor


        /// <summary>
        /// Construct character buffer reference from character array
        /// </summary>
        /// <param name="characterArray">character array</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        /// <param name="characterLength">character length</param>
        public CharacterBufferRange(
            char[]      characterArray,
            int         offsetToFirstChar,
            int         characterLength
            )
            : this(
                new CharacterBufferReference(characterArray, offsetToFirstChar),
                characterLength
                )
        {}


        /// <summary>
        /// Construct character buffer reference from string
        /// </summary>
        /// <param name="characterString">character string</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        /// <param name="characterLength">character length</param>
        public CharacterBufferRange(
            string      characterString,
            int         offsetToFirstChar,
            int         characterLength
            )
            : this(
                new CharacterBufferReference(characterString, offsetToFirstChar),
                characterLength
                )
        {}


        /// <summary>
        /// Construct character buffer reference from unsafe character string
        /// </summary>
        /// <param name="unsafeCharacterString">pointer to character string</param>
        /// <param name="characterLength">character length</param>
        [CLSCompliant(false)]
        public unsafe CharacterBufferRange(
            char*       unsafeCharacterString,
            int         characterLength
            )
            : this(
                new CharacterBufferReference(unsafeCharacterString, characterLength), 
                characterLength
                )
        {}


        /// <summary>
        /// Construct a character string from character buffer reference
        /// </summary>
        /// <param name="characterBufferReference">character buffer reference</param>
        /// <param name="characterLength">number of characters</param>
        internal CharacterBufferRange(
            CharacterBufferReference    characterBufferReference,
            int                         characterLength
            )
        {
            if (characterLength < 0)
            {
                throw new ArgumentOutOfRangeException("characterLength", SR.Get(SRID.ParameterCannotBeNegative));
            }

            int maxLength = (characterBufferReference.CharacterBuffer != null) ?
                characterBufferReference.CharacterBuffer.Count - characterBufferReference.OffsetToFirstChar :
                0;

            if (characterLength > maxLength)
            {
                throw new ArgumentOutOfRangeException("characterLength", SR.Get(SRID.ParameterCannotBeGreaterThan, maxLength));
            }

            _charBufferRef = characterBufferReference;
            _length = characterLength;
        }


        /// <summary>
        /// Construct a character string from part of another character string
        /// </summary>
        internal CharacterBufferRange(
            CharacterBufferRange    characterBufferRange,
            int                     offsetToFirstChar,
            int                     characterLength
            ) :
            this (
                characterBufferRange.CharacterBuffer, 
                characterBufferRange.OffsetToFirstChar + offsetToFirstChar,
                characterLength
                )
        {}


        /// <summary>
        /// Construct a character string object from string
        /// </summary>
        internal CharacterBufferRange(
            string charString
            ) : 
            this(
                new StringCharacterBuffer(charString),
                0,
                charString.Length
                )
        {}


        /// <summary>
        /// Construct character buffer from memory buffer
        /// </summary>
        internal CharacterBufferRange(
            CharacterBuffer     charBuffer,
            int                 offsetToFirstChar,
            int                 characterLength
            ) : 
            this(
                new CharacterBufferReference(charBuffer, offsetToFirstChar),
                characterLength
                )
        {}


        /// <summary>
        /// Construct a character string object by extracting text info from text run
        /// </summary>
        internal CharacterBufferRange(TextRun textRun)
        {
            _charBufferRef = textRun.CharacterBufferReference;
            _length = textRun.Length;
        }

        #endregion


        /// <summary>
        /// Compute hash code
        /// </summary>
        public override int GetHashCode()
        {
            return _charBufferRef.GetHashCode() ^ _length;
        }


        /// <summary>
        /// Test equality with the input object
        /// </summary>
        /// <param name="obj"> The object to test </param>
        public override bool Equals(object obj)
        {
            if (obj is CharacterBufferRange)
            {
                return Equals((CharacterBufferRange)obj);
            }
            return false;
        }


        /// <summary>
        /// Test equality with the input CharacterBufferRange
        /// </summary>
        /// <param name="value"> The CharacterBufferRange value to test </param>
        public bool Equals(CharacterBufferRange value)
        {
            return  _charBufferRef.Equals(value._charBufferRef)
                &&  _length == value._length;
        }



        /// <summary>
        /// Compare two CharacterBufferRange for equality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator == (
            CharacterBufferRange  left,
            CharacterBufferRange  right
            )
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two CharacterBufferRange for inequality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator != (
            CharacterBufferRange  left,
            CharacterBufferRange  right
            )
        {
            return !(left == right);
        }

        /// <summary>
        /// reference to the character buffer of a string
        /// </summary>
        public CharacterBufferReference CharacterBufferReference
        {
            get { return _charBufferRef; }
        }


        /// <summary>
        /// number of characters in text source character store
        /// </summary>
        public int Length
        {
            get { return _length; }
        }


        /// <summary>
        /// Getting an empty character string
        /// </summary>
        public static CharacterBufferRange Empty
        {
            get { return new CharacterBufferRange(); }
        }


        /// <summary>
        /// Indicate whether the character string object contains no information
        /// </summary>
        internal bool IsEmpty
        {
            get { return _charBufferRef.CharacterBuffer == null || _length <= 0; }
        }


        /// <summary>
        /// character memory buffer
        /// </summary>
        internal CharacterBuffer CharacterBuffer
        {
            get { return _charBufferRef.CharacterBuffer; }
        }


        /// <summary>
        /// character offset relative to the beginning of buffer to 
        /// the first character of the run.
        /// </summary>
        internal int OffsetToFirstChar
        {
            get { return _charBufferRef.OffsetToFirstChar; }
        }


        /// <summary>
        /// Return a character from the range, index is relative to the beginning of the range
        /// </summary>
        internal char this[int index]
        {
            get
            {
                Invariant.Assert(index >= 0 && index < _length);
                return _charBufferRef.CharacterBuffer[_charBufferRef.OffsetToFirstChar + index];
            }
        }
    }
}

