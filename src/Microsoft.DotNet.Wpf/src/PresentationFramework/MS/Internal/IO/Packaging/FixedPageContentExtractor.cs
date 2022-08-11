// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Given a DOM node for a fixed page, enumerates its text content.
//

using System;
using System.Xml;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Implements a sequence of (textContent, precedingDelimiter) pairs for
    /// a fixed page node.
    /// </summary>
    internal class FixedPageContentExtractor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Initialize a FixedPageContentExtractor from a DOM node.
        /// </summary>
        internal FixedPageContentExtractor(XmlNode fixedPage)
        {
            _fixedPageInfo = new XmlFixedPageInfo(fixedPage);
            _nextGlyphRun = 0;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        /// <summary>
        /// Return the content of the next glyph run, with a boolean indication
        /// whether it is separated by a space form the preceding glyph run.
        /// </summary>
        internal string NextGlyphContent(out bool inline, out uint lcid)
        {
            // Right now, we use the simplest possible heuristic for
            // spacing glyph runs: All pairs of adjacent glyph runs are assumed
            // to be separated by a word break.
            inline = false;
            lcid = 0;

            // End of page?
            if (_nextGlyphRun >= _fixedPageInfo.GlyphRunCount)
            {
                return null;
            }

            // Retrieve inline, lcid and return value from the next glyph run info.
            GlyphRunInfo glyphRunInfo = _fixedPageInfo.GlyphRunAtPosition(_nextGlyphRun);
            lcid = glyphRunInfo.LanguageID;

            // Point to the next glyph run for the next call and return.
            ++_nextGlyphRun;
            return glyphRunInfo.UnicodeString;
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        /// <summary>
        /// Indicates whether no more content can be returned.
        /// </summary>
        internal bool AtEndOfPage
        {
            get
            {
                return _nextGlyphRun >= _fixedPageInfo.GlyphRunCount;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields
        private XmlFixedPageInfo _fixedPageInfo;
        private int _nextGlyphRun;
        #endregion Private Fields
    }
}
