// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: GlyphInfoList class


using System;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// Glyph info list
    /// </summary>
    /// <remarks>
    /// A bundle of several per-glyph result data of GetGlyphs API.
    /// All array members have the same number of elements. They
    /// grow and shrink at the same degree at the same time.
    /// </remarks>
    internal class GlyphInfoList
    {
        internal GlyphInfoList(int capacity, int leap, bool justify)
        {
            _glyphs         = new UshortList(capacity, leap);
            _glyphFlags     = new UshortList(capacity, leap);
            _firstChars     = new UshortList(capacity, leap);
            _ligatureCounts = new UshortList(capacity, leap);
        }

        /// <summary>
        /// Length of the current run
        /// </summary>
        public int Length
        {
            get { return _glyphs.Length; }
        }

        /// <summary>
        /// Offset of current sublist in storage
        /// </summary>
        internal int Offset
        {
            get { return _glyphs.Offset; }
        }

        /// <summary>
        /// Validate glyph info length
        /// </summary>
        [Conditional("DEBUG")]
        internal void ValidateLength(int cch)
        {
            Debug.Assert(_glyphs.Offset + _glyphs.Length == cch, "Invalid glyph length!");
        }

        /// <summary>
        /// Limit range of accessing glyphinfo
        /// </summary>
        public void SetRange(int index, int length)
        {
            _glyphs.SetRange(index, length);
            _glyphFlags.SetRange(index, length);
            _firstChars.SetRange(index, length);
            _ligatureCounts.SetRange(index, length);
        }

        /// <summary>
        /// Set glyph run length (use with care)
        /// </summary>
        public void SetLength(int length)
        {
            _glyphs.Length = length;
            _glyphFlags.Length = length;
            _firstChars.Length = length;
            _ligatureCounts.Length = length;
        }

        public void Insert(int index, int Count)
        {
            _glyphs.Insert(index, Count);
            _glyphFlags.Insert(index, Count);
            _firstChars.Insert(index, Count);
            _ligatureCounts.Insert(index, Count);
        }

        public void Remove(int index, int Count)
        {
            _glyphs.Remove(index, Count);
            _glyphFlags.Remove(index, Count);
            _firstChars.Remove(index, Count);
            _ligatureCounts.Remove(index, Count);
        }

        public UshortList Glyphs
        {
            get { return _glyphs; }
        }

        public UshortList GlyphFlags
        {
            get { return _glyphFlags; }
        }

        public UshortList FirstChars
        {
            get { return _firstChars; }
        }

        public UshortList LigatureCounts
        {
            get { return _ligatureCounts; }
        }

        private UshortList  _glyphs;
        private UshortList  _glyphFlags;
        private UshortList  _firstChars;
        private UshortList  _ligatureCounts;
    }

    [Flags]
    internal enum GlyphFlags : ushort
    {
        /**** [Bit 0-7 OpenType flags] ****/

        // Value 0-4 used. Value 5 - 15 Reserved
        Unassigned              = 0x0000,
        Base                    = 0x0001,
        Ligature                = 0x0002,
        Mark                    = 0x0003,
        Component               = 0x0004,
        Unresolved              = 0x0007,
        GlyphTypeMask           = 0x0007,

        // Bit 4-5 used. Bit 7 Reserved
        Substituted             = 0x0010,
        Positioned              = 0x0020,
        NotChanged              = 0x0000,

        // reserved for OTLS internal use
        // inside one call
        CursiveConnected        = 0x0040,

        //bit 7 - reserved

        /**** [Bit 8-15 Avalon flags] ****/

        ClusterStart            = 0x0100,   // First glyph of cluster
        Diacritic               = 0x0200,   // Diacritic
        ZeroWidth               = 0x0400,   // Blank, ZWJ, ZWNJ etc, with no width
        Missing                 = 0x0800,   // Missing glyph
        InvalidBase             = 0x1000,   // Glyph of U+25cc indicating invalid base glyph
    }
}
