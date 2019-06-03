// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Holds the information regarding the matched text from text search
//

using System;
using System.Windows;

namespace System.Windows.Controls
{
    internal class MatchedTextInfo
    {
        /// <summary>
        /// For no match case
        /// </summary>
        static MatchedTextInfo()
        {
            s_NoMatch = new MatchedTextInfo(-1, null, 0, 0);
        }
        internal MatchedTextInfo(int matchedItemIndex, string matchedText, int matchedPrefixLength, int textExcludingPrefixLength)
        {
            _matchedItemIndex = matchedItemIndex;
            _matchedText = matchedText;
            _matchedPrefixLength = matchedPrefixLength;
            _textExcludingPrefixLength = textExcludingPrefixLength;
        }

        #region Internal Properties

        /// <summary>
        /// No match from text search
        /// </summary>
        internal static MatchedTextInfo NoMatch
        {
            get
            {
                return s_NoMatch;
            }
        }

        /// <summary>
        /// Matched text from text search
        /// </summary>
        internal string MatchedText
        {
            get
            {
                return _matchedText;
            }
        }

        /// <summary>
        /// Index of the matched item
        /// </summary>
        internal int MatchedItemIndex
        {
            get
            {
                return _matchedItemIndex;
            }
        }

        /// <summary>
        /// Length of the matched prefix
        /// </summary>
        internal int MatchedPrefixLength
        {
            get
            {
                return _matchedPrefixLength;
            }
        }

        /// <summary>
        /// Length of the text excluding prefix
        /// </summary>
        internal int TextExcludingPrefixLength
        {
            get
            {
                return _textExcludingPrefixLength;
            }
        }

        #endregion Internal Properties

        #region Private Fields

        private readonly string _matchedText;
        private readonly int _matchedItemIndex;
        private readonly int _matchedPrefixLength;
        private readonly int _textExcludingPrefixLength;
        private static MatchedTextInfo s_NoMatch;

        #endregion Private Fields
    }
}