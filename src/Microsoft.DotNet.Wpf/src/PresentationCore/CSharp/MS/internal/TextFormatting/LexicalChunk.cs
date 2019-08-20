// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Lexical chunk represents the character analysis of a piece of raw character string.
//
//


using System;
using System.Windows.Media.TextFormatting;
using MS.Internal.Generic;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Lexical chunk represents the character analysis of a piece of raw character string.
    /// It contains the analyzing result of the string by the lexical services component i.e. 
    /// word-breaker or hyphenator or both. 
    /// 
    /// The number of character indices represented by a chunk may not map exactly to the same number
    /// of LSCP in the LS character position space. This is because two adjacent character indices in 
    /// a chunk may be mapped by two non-adjacent LSCP in the LS positions. Between two LSRun could 
    /// exist a hidden-run which occupies real LSCP but represents no actual displayable character. 
    /// 
    /// The mapping between the character offsets and the offsets to the correspondent LSCP is retained
    /// in a span vector that is indexed by the character offsets.
    /// </summary>
    internal struct LexicalChunk
    {
        private TextLexicalBreaks       _breaks;        // lexical breaks of chunk characters
        private SpanVector<int>         _ichVector;     // spans of offsets to the ich-correspondence LSCP


        internal TextLexicalBreaks Breaks
        {
            get { return _breaks; }
        }


        /// <summary>
        /// Boolean value indicates whether this chunk contains no valid break info
        /// </summary>
        internal bool IsNoBreak
        {
            get { return _breaks == null; }
        }


        /// <summary>
        /// Contruct lexical chunk from character analysis
        /// </summary>
        internal LexicalChunk(
            TextLexicalBreaks   breaks,
            SpanVector<int>     ichVector
            )
        {
            Invariant.Assert(breaks != null);
            _breaks = breaks;
            _ichVector = ichVector;
        }


        /// <summary>
        /// Convert the specified LSCP to character index
        /// </summary>
        internal int LSCPToCharacterIndex(int lsdcp)
        {
            if (_ichVector.Count > 0)
            {
                int ich = 0;
                int cchLast = 0;
                int lsdcpLast = 0;

                for (int i = 0; i < _ichVector.Count; i++)
                {
                    MS.Internal.Generic.Span<int> span = _ichVector[i];
                    int lsdcpCurrent = span.Value;

                    if (lsdcpCurrent > lsdcp)
                    {
                        return ich - cchLast + Math.Min(cchLast, lsdcp - lsdcpLast);
                    }

                    ich += span.Length;
                    cchLast = span.Length;
                    lsdcpLast = lsdcpCurrent;
                }

                return ich - cchLast + Math.Min(cchLast, lsdcp - lsdcpLast);
            }

            return lsdcp;
        }


        /// <summary>
        /// Convert the specified character index to LSCP
        /// </summary>
        internal int CharacterIndexToLSCP(int ich)
        {
            if (_ichVector.Count > 0)
            {
                SpanRider<int> ichRider = new SpanRider<int>(_ichVector);
                ichRider.At(ich);
                return ichRider.CurrentValue + ich - ichRider.CurrentSpanStart;
            }

            return ich;
        }
    }
}

