// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

// 
//
// Description: DecoderFallbackWithFailureFlag is used when the developer wants Encoding.GetChars() method to fail
//              without throwing an exception when decoding cannot be performed.
//  Usage pattern is:
//      DecoderFallbackWithFailureFlag fallback = new DecoderFallbackWithFailureFlag();
//      Encoding e = Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, fallback);
//      e.GetChars(bytesToDecode);
//      if (fallback.HasFailed)
//      {
//          // Perform fallback and reset the failure flag.
//          fallback.HasFailed = false;
//      }
//
//  
//
//
//---------------------------------------------------------------------------

using System.Text;

namespace MS.Internal
{
    /// <summary>
    /// This class is similar to the standard DecoderExceptionFallback class,
    /// except that for performance reasons it sets a Boolean failure flag
    /// instead of throwing exception.
    /// </summary>
    internal class DecoderFallbackWithFailureFlag : DecoderFallback
    {
        public DecoderFallbackWithFailureFlag()
        { }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new FallbackBuffer(this);
        }

        /// <summary>
        /// The maximum number of characters this instance can return.
        /// </summary>
        public override int MaxCharCount
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns whether decoding failed.
        /// </summary>
        public bool HasFailed
        {
            get
            {
                return _hasFailed;
            }
            set
            {
                _hasFailed = value;
            }
        }

        private bool _hasFailed; // false by default

        /// <summary>
        /// A special implementation of DecoderFallbackBuffer that sets the failure flag
        /// in the parent DecoderFallbackWithFailureFlag class.
        /// </summary>
        private class FallbackBuffer : DecoderFallbackBuffer
        {
            public FallbackBuffer(DecoderFallbackWithFailureFlag parent)
            {
                _parent = parent;
            }

            /// <summary>
            /// Indicates whether a substitute string can be emitted if an input byte sequence cannot be decoded.
            /// Parameters specify an input byte sequence, and the index position of a byte in the input. 
            /// </summary>
            /// <param name="bytesUnknown">An input array of bytes.</param>
            /// <param name="index">The index position of a byte in bytesUnknown.</param>
            /// <returns>true if a string exists that can be inserted in the output
            /// instead of decoding the byte specified in bytesUnknown;
            /// false if the input byte should be ignored.</returns>
            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                _parent.HasFailed = true;
                return false;
            }

            /// <summary>
            /// Retrieves the next character in the fallback buffer.
            /// </summary>
            /// <returns>The next Unicode character in the fallback buffer.</returns>
            public override char GetNextChar()
            {
                return (char)0;
            }

            /// <summary>
            /// Prepares the GetNextChar method to retrieve the preceding character in the fallback buffer.
            /// </summary>
            /// <returns>true if the MovePrevious operation was successful; otherwise, false.</returns>
            public override bool MovePrevious()
            {
                return false;
            }

            /// <summary>
            /// Gets the number of characters in this instance of DecoderFallbackBuffer that remain to be processed.
            /// </summary>
            public override int Remaining
            {
                get
                {
                    return 0;
                }
            }

            private DecoderFallbackWithFailureFlag _parent;
        }
    }
}
