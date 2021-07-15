// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//              Abstract classes FixedPageInfo and GlyphRunInfo for retrieving
//              glyph run information from a fixed-format page.
//

using System;

namespace MS.Internal
{
    /// <summary>
    /// A FixedPageInfo is a random-access sequence of GlyphRunInfo's.
    /// </summary>
    internal abstract class FixedPageInfo
    {
        /// <summary>
        /// Get the glyph run at zero-based position 'position'.
        /// </summary>
        /// <remarks>
        /// Returns null for a nonexistent position. No exception raised.
        /// </remarks>
        internal abstract GlyphRunInfo GlyphRunAtPosition(int position);

        /// <summary>
        /// Indicates the number of glyph runs on the page.
        /// </summary>
        internal abstract int GlyphRunCount { get; }
    }

    /// <summary>
    /// A GlyphRunInfo provides metric information on a glyph run.
    /// This abstract class can be implemented in a number of ways.
    /// For example, a GlyphRunInfo can encapsulate a Glyphs DOM node,
    /// or a MIL object representing a Glyphs element.
    /// </summary>
    internal abstract class GlyphRunInfo
    {
        //------------------------------------------------------
        //
        //   Internal Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //   Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties 
        /// <summary>
        /// The start point of the segment [StartPosition, EndPosition],
        /// which runs along the baseline of the glyph run.
        /// </summary>
        /// <remarks>
        /// The point is given in page coordinates.
        /// Subclasses may return Double.NaN in either coordinate when the input glyph run is invalid.
        /// </remarks>
        internal abstract System.Windows.Point StartPosition { get; }

        /// <summary>
        /// The end point of the segment [StartPosition, EndPosition],
        /// which runs along the baseline of the glyph run.
        /// </summary>
        /// <remarks>
        /// The point is given in page coordinates.
        /// Subclasses may return Double.NaN in either coordinate when the input glyph run is invalid.
        /// </remarks>
        internal abstract System.Windows.Point EndPosition { get; }


        /// <summary>
        /// The font width in ems.
        /// </summary>
        /// <remarks>
        /// This is provided for the purpose of evaluating distances along the baseline OR a perpendicular
        /// to the baseline.
        /// When a font is displayed sideways, what is given here is still the width of the font.
        /// It is up to the client code to decide whether to use the width or height for measuring
        /// distances between glyph runs.
        /// </remarks>
        internal abstract double WidthEmFontSize { get; }

        /// <summary>
        /// The font height in ems.
        /// </summary>
        /// <remarks>
        /// This is provided for the purpose of evaluating distances along the baseline OR a perpendicular
        /// to the baseline.
        /// When a font is displayed sideways, what is given here is still the height of the font.
        /// It is up to the client code to decide whether to use the width or height for measuring
        /// deviations from the current baseline.
        /// </remarks>
        internal abstract double HeightEmFontSize { get; }

        /// <summary>
        /// Whether glyphs are individually rotated 270 degrees (so as to face downwards in vertical text layout).
        /// </summary>
        /// <remarks>
        /// This feature is designed for ideograms and should not make sense for latin characters.
        /// </remarks>
        internal abstract bool GlyphsHaveSidewaysOrientation { get; }

        /// <summary>
        /// 0 for left-to-right and 1 for right-to-left.
        /// </summary>
        internal abstract int BidiLevel { get; }

        /// <summary>
        /// The glyph run's language id.
        /// </summary>
        internal abstract uint LanguageID { get; }

        /// <summary>
        /// The glyph run's contents as a string of unicode symbols.
        /// </summary>
        internal abstract string UnicodeString { get; }

        #endregion Internal Properties 
    }
}


