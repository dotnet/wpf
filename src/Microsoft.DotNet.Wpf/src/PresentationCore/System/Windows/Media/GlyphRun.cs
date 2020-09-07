// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: The GlyphRun object represents a sequence of glyphs from a single face of a single font at a single size,
// and with a single rendering style.
//
//              See specs at
// Glyphs%20element%20and%20GlyphRun%20object.htm
// Glyph%20Run%20hit%20testing%20and%20caret%20placement%20API.htm
//
//
//

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Composition;
using System.Windows.Media.TextFormatting;
using System.Windows.Markup;
using System.Runtime.InteropServices;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.TextFormatting;
using MS.Internal.Text.TextInterface;
using MS.Utility;
using System.Security;
using System.Windows.Interop;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// The GlyphRun object represents a sequence of glyphs from a single face of a single font at a single size,
    /// and with a single rendering style.
    /// </summary>
    /// <remarks>
    ///  Consider adding [XmlLangProperty("Language")] 
    /// </remarks>
    public class GlyphRun : DUCE.IResource, ISupportInitialize
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Construct an uninitialized GlyphRun object. Caller should call ISupportInitialize.BeginInit()
        /// to begin initialization and call ISupportInitialize.EndInit() to finish the initialization.
        /// The GlyphRun does not support all the operations until it is fully initialized.
        /// </summary>
        [Obsolete("Use the PixelsPerDip override", false)]
        public GlyphRun()
        {
        }

        /// <summary>
        /// Construct an uninitialized GlyphRun object. Caller should call ISupportInitialize.BeginInit()
        /// to begin initialization and call ISupportInitialize.EndInit() to finish the initialization.
        /// The GlyphRun does not support all the operations until it is fully initialized.
        /// <param name="pixelsPerDip">PixelsPerDip of the screen on which this is to be drawn (96ths of an inch).</param>
        /// </summary>
        public GlyphRun(float pixelsPerDip)
        {
            _pixelsPerDip = pixelsPerDip;
        }

        /// <summary>
        /// Constructs a new GlyphRun object for per monitor DPI aware applications
        /// </summary>
        /// <param name="glyphTypeface">GlyphTypeface of the GlyphRun object </param>
        /// <param name="bidiLevel">Bidi level of the GlyphRun object        </param>
        /// <param name="isSideways">Set to true to display the GlyphRun sideways</param>
        /// <param name="renderingEmSize">Font rendering size in drawing surface units (96ths of an inch).</param>
        /// <param name="pixelsPerDip">PixelsPerDip of the screen on which this is to be drawn (96ths of an inch).</param>
        /// <param name="glyphIndices">The list of font indices that represent glyphs in this run.</param>
        /// <param name="baselineOrigin">Origin of the first glyph in the run.
        /// The glyph is placed so that the leading edge of its advance vector
        /// and its baseline intersect this point.
        ///  </param>
        /// <param name="advanceWidths">The list of advance widths, one for each glyph in GlyphIndices.
        /// The nominal origin of the nth glyph (n > 0) in the run is the nominal origin
        /// of the n-1th glyph plus the n-1th advance width added along the runs advance vector.
        /// Base glyphs generally have a non-zero advance width, combining glyphs generally have a zero advance width.
        /// </param>
        /// <param name="glyphOffsets">The list of glyph offsets. Added to the nominal glyph origin calculated above to generate the final origin for the glyph.
        /// Base glyphs generally have a glyph offset of (0,0), combining glyphs generally have an offset
        /// that places them correctly on top of the nearest preceeding base glyph.
        /// </param>
        /// <param name="characters">Characters represented by this glyphrun</param>
        /// <param name="deviceFontName">
        /// Identifies a specific device font for which the GlyphRun has been optimized. When a GlyphRun is
        /// being rendered on a device that has built-in support for this named font, then the GlyphRun should be rendered using a
        /// possibly device specific mechanism for selecting that font, and by sending the Unicode codepoints rather than the
        /// glyph indices. When rendering onto a device that does not include built-in support for the named font,
        /// this property should be ignored.
        /// </param>
        /// <param name="clusterMap">The list that maps characters in the glyph run to glyph indices.
        /// There is one entry per character in Characters list.
        /// Each value gives the offset of the first glyph in GlyphIndices
        /// that represents the corresponding character in Characters.
        /// Where multiple characters map to a single glyph, or to a glyph group
        /// that cannot be broken down to map exactly to individual characters,
        /// the entries for all the characters have the same value:
        /// the offset of the first glyph that represents this group of characters.
        /// If the list is null or empty, sequential 1 to 1 mapping is assumed.
        /// </param>
        /// <param name="caretStops">A list of caret stops for the glyphs</param>
        /// <param name="language">Language of the GlyphRun</param>
        [CLSCompliant(false)]
        public GlyphRun(
            GlyphTypeface glyphTypeface,
            int bidiLevel,
            bool isSideways,
            double renderingEmSize,
            float pixelsPerDip,
            IList<ushort> glyphIndices,
            Point baselineOrigin,
            IList<double> advanceWidths,
            IList<Point> glyphOffsets,
            IList<char> characters,
            string deviceFontName,
            IList<ushort> clusterMap,
            IList<bool> caretStops,
            XmlLanguage language
            )
        {
            // Suppress PRESharp warning that glyphIndices and advanceWidths are not validated and can be null.
            // They can indeed be null, but that's perfectly OK. An explicit null check in the constructor is
            // not required.
#pragma warning suppress 56506
            Initialize(
                glyphTypeface,
                bidiLevel,
                isSideways,
                renderingEmSize,
                pixelsPerDip,
                glyphIndices,
                baselineOrigin,
                advanceWidths,
                glyphOffsets,
                characters,
                deviceFontName,
                clusterMap,
                caretStops,
                language,
                TextFormattingMode.Ideal
                );

            // GlyphRunFlags.CacheInkBounds enanbles ink bounding box caching. Bounding box caching would cost
            // 32 bytes per GlyphRun. We do not want to enable it for all cases possible working set increase.

            // For Line layout, ink bounding box is only used a few times, so caching is disabled because it will
            // go through TryCreate below. Memory cost: 1 pointer.

            // For loading XPS in which bounding box calculation are called a lot in hit testing, Glyphs.cs will
            // call this constructor, which enables caching. Memory cost: 1 pointer + boxed Rect.

            // If we late decide it's worthwhile to cache for all, memory cost can be reduced to one Rect (32-bytes).
            // If we decide single precision is good enough, it can be reduced to 16 bytes.
            _flags |= GlyphRunFlags.CacheInkBounds;
        }

        /// <summary>
        /// Constructs a new GlyphRun object.
        /// </summary>
        /// <param name="glyphTypeface">GlyphTypeface of the GlyphRun object </param>
        /// <param name="bidiLevel">Bidi level of the GlyphRun object        </param>
        /// <param name="isSideways">Set to true to display the GlyphRun sideways</param>
        /// <param name="renderingEmSize">Font rendering size in drawing surface units (96ths of an inch).</param>
        /// <param name="glyphIndices">The list of font indices that represent glyphs in this run.</param>
        /// <param name="baselineOrigin">Origin of the first glyph in the run.
        /// The glyph is placed so that the leading edge of its advance vector
        /// and its baseline intersect this point.
        ///  </param>
        /// <param name="advanceWidths">The list of advance widths, one for each glyph in GlyphIndices.
        /// The nominal origin of the nth glyph (n > 0) in the run is the nominal origin
        /// of the n-1th glyph plus the n-1th advance width added along the runs advance vector.
        /// Base glyphs generally have a non-zero advance width, combining glyphs generally have a zero advance width.
        /// </param>
        /// <param name="glyphOffsets">The list of glyph offsets. Added to the nominal glyph origin calculated above to generate the final origin for the glyph.
        /// Base glyphs generally have a glyph offset of (0,0), combining glyphs generally have an offset
        /// that places them correctly on top of the nearest preceeding base glyph.
        /// </param>
        /// <param name="characters">Characters represented by this glyphrun</param>
        /// <param name="deviceFontName">
        /// Identifies a specific device font for which the GlyphRun has been optimized. When a GlyphRun is
        /// being rendered on a device that has built-in support for this named font, then the GlyphRun should be rendered using a
        /// possibly device specific mechanism for selecting that font, and by sending the Unicode codepoints rather than the
        /// glyph indices. When rendering onto a device that does not include built-in support for the named font,
        /// this property should be ignored.
        /// </param>
        /// <param name="clusterMap">The list that maps characters in the glyph run to glyph indices.
        /// There is one entry per character in Characters list.
        /// Each value gives the offset of the first glyph in GlyphIndices
        /// that represents the corresponding character in Characters.
        /// Where multiple characters map to a single glyph, or to a glyph group
        /// that cannot be broken down to map exactly to individual characters,
        /// the entries for all the characters have the same value:
        /// the offset of the first glyph that represents this group of characters.
        /// If the list is null or empty, sequential 1 to 1 mapping is assumed.
        /// </param>
        /// <param name="caretStops">A list of caret stops for the glyphs</param>
        /// <param name="language">Language of the GlyphRun</param>
        [CLSCompliant(false)]
        [Obsolete("Use the PixelsPerDip override", false)]
        public GlyphRun(
            GlyphTypeface           glyphTypeface,
            int                     bidiLevel,
            bool                    isSideways,
            double                  renderingEmSize,
            IList<ushort>           glyphIndices,
            Point                   baselineOrigin,
            IList<double>           advanceWidths,
            IList<Point>            glyphOffsets,
            IList<char>             characters,
            string                  deviceFontName,
            IList<ushort>           clusterMap,
            IList<bool>             caretStops,
            XmlLanguage             language
            )
        {
            // Suppress PRESharp warning that glyphIndices and advanceWidths are not validated and can be null.
            // They can indeed be null, but that's perfectly OK. An explicit null check in the constructor is
            // not required.
#pragma warning suppress 56506
            Initialize(
                glyphTypeface,
                bidiLevel,
                isSideways,
                renderingEmSize,
                Util.PixelsPerDip,
                glyphIndices,
                baselineOrigin,
                advanceWidths,
                glyphOffsets,
                characters,
                deviceFontName,
                clusterMap,
                caretStops,
                language,
                TextFormattingMode.Ideal
                );

            // GlyphRunFlags.CacheInkBounds enanbles ink bounding box caching. Bounding box caching would cost
            // 32 bytes per GlyphRun. We do not want to enable it for all cases possible working set increase.

            // For Line layout, ink bounding box is only used a few times, so caching is disabled because it will
            // go through TryCreate below. Memory cost: 1 pointer.

            // For loading XPS in which bounding box calculation are called a lot in hit testing, Glyphs.cs will
            // call this constructor, which enables caching. Memory cost: 1 pointer + boxed Rect.

            // If we late decide it's worthwhile to cache for all, memory cost can be reduced to one Rect (32-bytes).
            // If we decide single precision is good enough, it can be reduced to 16 bytes.
            _flags |= GlyphRunFlags.CacheInkBounds;
        }

        /// <summary>
        /// Creates a new GlyphRun object. This method is similar to the constructor with
        /// the same argument list except that it returns null instead of throwing an
        /// exception if the GlyphRun area or a coordinate exceed the maximum value.
        /// </summary>
        internal static GlyphRun TryCreate(
            GlyphTypeface           glyphTypeface,
            int                     bidiLevel,
            bool                    isSideways,
            double                  renderingEmSize,
            float                   pixelsPerDip,
            IList<ushort>           glyphIndices,
            Point                   baselineOrigin,
            IList<double>           advanceWidths,
            IList<Point>            glyphOffsets,
            IList<char>             characters,
            string                  deviceFontName,
            IList<ushort>           clusterMap,
            IList<bool>             caretStops,
            XmlLanguage             language,
            TextFormattingMode          textLayout
            )
        {
            GlyphRun glyphRun = new GlyphRun(pixelsPerDip);

            // Suppress PRESharp warning that glyphIndices and advanceWidths are not validated and can be null.
            // They can indeed be null, but that's perfectly OK. An explicit null check in the constructor is
            // not required.
#pragma warning suppress 56506
            glyphRun.Initialize(
                glyphTypeface,
                bidiLevel,
                isSideways,
                renderingEmSize,
                pixelsPerDip,
                glyphIndices,
                baselineOrigin,
                advanceWidths,
                glyphOffsets,
                characters,
                deviceFontName,
                clusterMap,
                caretStops,
                language,
                textLayout
                );

            // Cached GlyphRun bounds are needed to pass to the render thread
            glyphRun._flags |= GlyphRunFlags.CacheInkBounds;

            if (glyphRun.IsInitialized)
                return glyphRun;
            else
                return null;
        }

        private void Initialize(
            GlyphTypeface           glyphTypeface,
            int                     bidiLevel,
            bool                    isSideways,
            double                  renderingEmSize,
            float                   pixelsPerDip,
            IList<ushort>           glyphIndices,
            Point                   baselineOrigin,
            IList<double>           advanceWidths,
            IList<Point>            glyphOffsets,
            IList<char>             characters,
            string                  deviceFontName,
            IList<ushort>           clusterMap,
            IList<bool>             caretStops,
            XmlLanguage             language,
            TextFormattingMode      textFormattingMode
            )
        {
            // The default branch prediction rules for modern processors specify that forward branches
            // are not to be taken. If the branch is in fact taken, all of the speculatively executed code
            // must be discarded, the processor pipeline flushed, and then reloaded. This results in a
            // processor stall of at least 42 cycles for the P4 Northwood for each mis-predicted branch.
            // The deeper the processor pipeline the higher the cost, i.e. Prescott processors.
            // Checking for multiple incorrect parameters in a method with high call count like this one can
            // easily add significant overhead for no reason. Note that the C# compiler should be able to make
            // reasonable assumptions about branches that throw exceptions, but the current whidbey
            // implemenation is weak in this regard. Also the current IBC tools are unable to add branch
            // prediction hints to improve behavior based on run time information. Also note that adding
            // branch prediction hints increases code size by a byte per branch and doing this in every
            // method that is coded without default branch prediction behavior in mind would add an
            // unacceptable amount of working set.
            if ((glyphTypeface != null) &&
                (glyphIndices != null) &&
                (advanceWidths != null) &&
                (renderingEmSize >= 0.0) &&
                (glyphIndices.Count > 0) &&
                (glyphIndices.Count <= MaxGlyphCount) &&
                (advanceWidths.Count == glyphIndices.Count) &&
                ((glyphOffsets == null) || ((glyphOffsets != null) && (glyphOffsets.Count != 0) && (glyphOffsets.Count == glyphIndices.Count))))
            {
                _textFormattingMode = textFormattingMode;
                // Set member variables here,
                // so that GlyphRun properties can be calculated in advanced validation code.
                _glyphIndices = glyphIndices;
                _characters = characters;
                _clusterMap = clusterMap;
                _baselineOrigin = baselineOrigin;
                _renderingEmSize = renderingEmSize;
                _advanceWidths = advanceWidths;
                _glyphOffsets = glyphOffsets;
                _glyphTypeface = glyphTypeface;
                _flags = (isSideways ? GlyphRunFlags.IsSideways : GlyphRunFlags.None);
                _bidiLevel = bidiLevel;
                _caretStops = caretStops;
                _language = language;
                _deviceFontName = deviceFontName;
                _pixelsPerDip = pixelsPerDip;

                if (characters != null && characters.Count != 0)
                {
                    if (clusterMap != null && clusterMap.Count != 0)
                    {
                        if (clusterMap.Count == characters.Count)
                        {
                            // Perform some simple cluster map validation.
                            // First entry should be zero, the entries should be monotonic and shouldn't point outside of the glyph indices range.
                            if (clusterMap[0] == 0)
                            {
                                int glyphCount = GlyphCount;
                                int mapCount = clusterMap.Count;
                                ushort previous = clusterMap[0];

                                for (int i = 1; i < mapCount; ++i)
                                {
                                    ushort current = clusterMap[i];
                                    if ((current >= previous) && (current < glyphCount))
                                    {
                                        previous = current;
                                    }
                                    else
                                    {
                                        if (clusterMap[i] < clusterMap[i - 1])
                                            throw new ArgumentException(SR.Get(SRID.ClusterMapEntriesShouldNotDecrease), "clusterMap");

                                        if (clusterMap[i] >= GlyphCount)
                                            throw new ArgumentException(SR.Get(SRID.ClusterMapEntryShouldPointWithinGlyphIndices), "clusterMap");
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException(SR.Get(SRID.ClusterMapFirstEntryMustBeZero), "clusterMap");
                            }
                        }
                        else
                        {
                            throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsShouldBeEqualTo, characters.Count), "clusterMap");
                        }
                    }
                    else
                    {
                        if (GlyphCount != characters.Count)
                            throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsShouldBeEqualTo, GlyphCount), "clusterMap");
                    }
                }

                if (caretStops != null && caretStops.Count != 0)
                {
                    if (caretStops.Count != CodepointCount + 1)
                        throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsShouldBeEqualTo, CodepointCount + 1), "caretStops");
                }

                if (isSideways && (bidiLevel & 1) != 0)
                    throw new ArgumentException(SR.Get(SRID.SidewaysRTLTextIsNotSupported));

                // NOTE:  In previous versions this function would estimate the size
                // of this glyph run's bitmaps and compare it against the theoretical
                // maximum size allowed before rendering falls back to using geometry.
                // This was done in order to produce a managed exception where we might
                // hit overflow or memory allocation issues in native code.
                // We no longer own the code the produces these bitmaps so we can't reliably
                // avoid the issue here any longer.
            }
            else
            {
                if (DoubleUtil.IsNaN(renderingEmSize))
                    throw new ArgumentOutOfRangeException("renderingEmSize", SR.Get(SRID.ParameterValueCannotBeNaN));

                if (renderingEmSize < 0.0)
                    throw new ArgumentOutOfRangeException("renderingEmSize", SR.Get(SRID.ParameterValueCannotBeNegative));

                if (glyphTypeface == null)
                    throw new ArgumentNullException("glyphTypeface");

                if (glyphIndices == null)
                    throw new ArgumentNullException("glyphIndices");

                if (glyphIndices.Count <= 0)
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeGreaterThanZero), "glyphIndices");

                if (glyphIndices.Count > MaxGlyphCount)
                {
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeLessOrEqualTo, MaxGlyphCount), "glyphIndices");
                }

                if (advanceWidths == null)
                    throw new ArgumentNullException("advanceWidths");

                if (advanceWidths.Count != glyphIndices.Count)
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsShouldBeEqualTo, glyphIndices.Count), "advanceWidths");

                if (glyphOffsets != null && glyphOffsets.Count != 0 && glyphOffsets.Count != glyphIndices.Count)
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsShouldBeEqualTo, glyphIndices.Count), "glyphOffsets");

                // We should've caught all invalid cases above and thrown appropriate exceptions.
                Invariant.Assert(false);
            }

            IsInitialized = true; // The glyphrun is completely initialized
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Given a character hit, computes the offset from the leading edge of the glyph run
        /// to the leading or trailing edge of a caret stop containing the character hit.
        /// If the glyph run is not hit testable, the distance of 0.0 is returned.
        /// </summary>
        /// <param name="characterHit">Character hit to compute the distance to.</param>
        /// <returns>The offset from the leading edge of the glyph run
        /// to the leading or trailing edge of a caret stop containing the character hit.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The input character hit is outside of the range specified by the glyph run Unicode string.
        /// </exception>
        public double GetDistanceFromCaretCharacterHit(CharacterHit characterHit)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            IList<bool> caretStops = CaretStops != null && CaretStops.Count != 0 ? CaretStops : new DefaultCaretStopList(CodepointCount);
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
                throw new ArgumentOutOfRangeException("characterHit");

            int caretStopIndex, codePointsUntilNextStop;
            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out caretStopIndex,
                out codePointsUntilNextStop);

            // Not a hit testable glyph run.
            if (caretStopIndex == -1)
                return 0.0;

            // Trailing edge of a caret stop that doesn't have a corresponding valid next caret stop.
            if (codePointsUntilNextStop == -1 && characterHit.TrailingLength != 0)
            {
                return 0.0;
            }

            // Code point we are measuring the distance to.
            int caretCodePoint = characterHit.TrailingLength == 0 ? caretStopIndex : caretStopIndex + codePointsUntilNextStop;

            double distance = 0.0;

            // Sum up glyph advance widths until the caret code point.

            IList<ushort> clusterMap = ClusterMap;
            if (clusterMap == null)
                clusterMap = new DefaultClusterMap(CodepointCount);

            int clusterCodepointStart = 0;
            int currentCodepoint = clusterCodepointStart;

            IList<double> advances = AdvanceWidths;
            for (;;)
            {
                ++currentCodepoint;

                if (currentCodepoint >= clusterMap.Count || clusterMap[currentCodepoint] != clusterMap[clusterCodepointStart])
                {
                    // We reached the beginning of the next cluster or the end of the glyph run.
                    // If the codepoint is within the cluster, calculate the partial width and abort the loop.
                    // If the codepoint is past the cluster, accumulate the whole cluster advance width and move on.

                    double clusterWidth = 0;
                    int clusterGlyphEnd;
                    if (currentCodepoint >= clusterMap.Count)
                        clusterGlyphEnd = advances.Count;
                    else
                        clusterGlyphEnd = clusterMap[currentCodepoint];

                    for (int i = clusterMap[clusterCodepointStart]; i < clusterGlyphEnd; ++i)
                        clusterWidth += advances[i];

                    if (caretCodePoint < currentCodepoint || currentCodepoint >= clusterMap.Count)
                    {
                        // The caret code point is within a cluster or we are past the end of the run,
                        // sum all glyph advance widths in the cluster
                        // and multiply the result by (caretCodePoint / number of codepoints in the cluster).
                        clusterWidth *= (double)(caretCodePoint - clusterCodepointStart) / (currentCodepoint - clusterCodepointStart);
                        distance += clusterWidth;
                        break;
                    }

                    // The codepoint is past the cluster, accumulate the whole cluster advance width and move on.
                    distance += clusterWidth;
                    clusterCodepointStart = currentCodepoint;
                }
            }

            return distance;
        }

        /// <summary>
        /// Given an offset from the leading edge of the glyph run, computes the caret character hit
        /// that contains the offset. The out bool IsInside parameter describes whether the character hit
        /// is inside the glyph run. If the hit is outside the glyph run, the character hit represents
        /// the closest caret character hit within the glyph run.
        /// </summary>
        /// <param name="distance">Distance to compute character hit for.</param>
        /// <param name="isInside">isInside is set to true when the character hit
        /// is inside the glyph run, and to false otherwise.</param>
        /// <returns>The character hit that is closest to the input distance.</returns>
        public CharacterHit GetCaretCharacterHitFromDistance(double distance, out bool isInside)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            // Navigate the caret stop array and find a pair of caret stops that contains the distance.

            IList<double> advances = AdvanceWidths;
            IList<bool> caretStops = CaretStops != null && CaretStops.Count != 0 ? CaretStops : new DefaultCaretStopList(CodepointCount);
            IList<ushort> clusterMap = ClusterMap;
            if (clusterMap == null)
                clusterMap = new DefaultClusterMap(CodepointCount);

            // The following two variables describe the closest caret stop to the left of the input distance.
            int firstStopIndex = -1;
            double firstStopAdvance = 0.0;

            // The following variable describes the closest caret stop to the right of the input distance.
            int secondStopIndex = -1;

            // Accumulated advance width just before the current cluster.
            double currentAdvance = 0.0;

            // Start index of the cluster we're in.
            int currentClusterStart = 0;

            // Since the caretStops array contains clusterMap.Count + 1 elements,
            // we need to be careful before dereferencing i in the loop body.
            for (int i = 1; i < caretStops.Count; ++i)
            {
                if (i < clusterMap.Count && clusterMap[i] == clusterMap[currentClusterStart])
                    continue;

                // We reached the end of an (n:m) cluster.
                // First, accumulate the overall cluster advance width by summing m glyph advances.

                ushort lastGlyphInCluster = i < clusterMap.Count ? clusterMap[i] : (ushort)advances.Count;

                Debug.Assert(clusterMap[currentClusterStart] < lastGlyphInCluster);

                double clusterAdvance = 0.0;
                for (int j = clusterMap[currentClusterStart]; j < lastGlyphInCluster; ++j)
                    clusterAdvance += advances[j];

                // The overall advance is divided evenly by n code points.
                clusterAdvance /= i - currentClusterStart;

                // Go through the individual caret stops and compare them against the input distance
                for (int j = currentClusterStart; j < i; ++j)
                {
                    if (caretStops[j])
                    {
                        if (currentAdvance <= distance)
                        {
                            firstStopIndex = j;
                            firstStopAdvance = currentAdvance;
                        }
                        else
                        {
                            // We found a caret stop to the right of the input distance,
                            // so we're done with enumerating.
                            secondStopIndex = j;
                            goto SecondStopFound;
                        }
                    }
                    currentAdvance += clusterAdvance;
                }
                currentClusterStart = i;
            }

            // The last iteration is interesting. Because inside the above loop we only look at the caret stops up until i-1,
            // and there may or may not be a caret stop at the end of a glyph run,
            // we need to check the last caret stop value and the distance.
            // The code before SecondStopFound is essentially the reduced version of the loop body above when i == caretStops.Count.
            // We could modify the loop, but this would result in additional special cases.
            if (caretStops[caretStops.Count - 1])
            {
                if (currentAdvance > distance)
                    secondStopIndex = caretStops.Count - 1;
            }

        SecondStopFound:
            // First stop is described by firstStopIndex, firstStopAdvance.
            // Second stop is described by secondStopIndex, currentAdvance.

            // If both indices are equal to -1, then all caret stop entries except the very last one are set to false.
            // If the last one is also set to false, the glyph run is not hit testable at all.
            // If the last one is set to true, we return CharacterHit corresponding to that last caret stop.
            if (firstStopIndex == -1 && secondStopIndex == -1)
            {
                isInside = false;
                if (caretStops[caretStops.Count - 1])
                    return new CharacterHit(caretStops.Count - 1, 0);
                else
                    return new CharacterHit(0, 0);
            }

            // Check for case when the first stop is not valid.
            // This happens when the hit is to the left of the first caret stop.
            if (firstStopIndex == -1)
            {
                isInside = false;

                // Leading edge of the second stop.
                return new CharacterHit(secondStopIndex, 0);
            }

            // Check for case when the second stop is not valid.
            // This happens when the hit is to the right of the last caret stop.
            if (secondStopIndex == -1)
            {
                isInside = false;

                // Trailing edge of the first stop.
                return new CharacterHit(firstStopIndex, caretStops.Count - 1 - firstStopIndex);
            }

            isInside = true;

            if (distance <= (firstStopAdvance + currentAdvance) / 2.0)
            {
                // Leading edge of the first stop.
                return new CharacterHit(firstStopIndex, 0);
            }
            else
            {
                // Trailing edge of the first stop.
                return new CharacterHit(firstStopIndex, secondStopIndex - firstStopIndex);
            }
        }

        /// <summary>
        /// Computes the next valid caret character hit in the logical direction.
        /// If no further navigation is possible, the returned hit result is the same as input value.
        /// </summary>
        /// <param name="characterHit">The character hit to compute next hit value for.</param>
        /// <returns>The next valid caret character hit in the logical direction, or
        /// the input value if no further navigation is possible.</returns>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            IList<bool> caretStops = CaretStops != null && CaretStops.Count != 0 ? CaretStops : new DefaultCaretStopList(CodepointCount);
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
                throw new ArgumentOutOfRangeException("characterHit");

            int caretStopIndex, codePointsUntilNextStop;
            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out caretStopIndex,
                out codePointsUntilNextStop);

            // Not a hit testable run, or no next caret code point.
            if (caretStopIndex == -1 || codePointsUntilNextStop == -1)
                return characterHit;

            // If we are at the leading edge, move to the trailing edge of the same code point.
            if (characterHit.TrailingLength == 0)
                return new CharacterHit(caretStopIndex, codePointsUntilNextStop);

            // If the next caret stop is within the glyph run,
            // move to the trailing edge of it.
            int nextCaretStopIndex, nextCodePointsUntilNextStop;
            FindNearestCaretStop(
                caretStopIndex + codePointsUntilNextStop,
                caretStops,
                out nextCaretStopIndex,
                out nextCodePointsUntilNextStop);

            // See if the next caret stop is within the glyph run.
            // If not, no navigation is possible.
            if (nextCodePointsUntilNextStop == -1)
                return characterHit;

            return new CharacterHit(nextCaretStopIndex, nextCodePointsUntilNextStop);
        }

        /// <summary>
        /// Computes the previous valid caret character hit in the logical direction.
        /// If no further navigation is possible, the returned hit result is the same as input value.
        /// </summary>
        /// <param name="characterHit">The character hit to compute previous hit value for.</param>
        /// <returns>The previous valid caret character hit in the logical direction, or
        /// the input value if no further navigation is possible.</returns>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            IList<bool> caretStops = CaretStops != null && CaretStops.Count != 0 ? CaretStops : new DefaultCaretStopList(CodepointCount);
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
                throw new ArgumentOutOfRangeException("characterHit");

            int caretStopIndex, codePointsUntilNextStop;
            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out caretStopIndex,
                out codePointsUntilNextStop);

            if (caretStopIndex == -1)
                return characterHit;

            // If we are at the trailing edge, move to the leading edge of the same code point.
            if (characterHit.TrailingLength != 0)
                return new CharacterHit(caretStopIndex, 0);

            // Find the previous caret stop.
            int previousCaretStopIndex;
            FindNearestCaretStop(
                caretStopIndex - 1,
                caretStops,
                out previousCaretStopIndex,
                out codePointsUntilNextStop);

            // No previous hit, return the original one.
            if (previousCaretStopIndex == -1 || previousCaretStopIndex == caretStopIndex)
                return characterHit;

            return new CharacterHit(previousCaretStopIndex, 0);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        public float PixelsPerDip
        {
            get
            {
                CheckInitialized();
                return _pixelsPerDip;
            }
            set
            {
                CheckInitializing();
                _pixelsPerDip = value;
            }
        }

        /// <summary>
        /// Advance width from origin of first glyph to far alignment edge of last glyph.
        /// </summary>
        private double AdvanceWidth
        {
            get
            {
                double advance = 0;
                if (_advanceWidths != null)
                {
                    foreach(double glyphAdvance in _advanceWidths)
                        advance += glyphAdvance;
                }

                return advance;
            }
        }

        /// <summary>
        /// Distance from the GlyphRun origin to the top of the alignment box.
        /// </summary>
        private double Ascent
        {
            get
            {
                // for sideways text, origin is in the middle of the character cell
                if (IsSideways)
                    return _renderingEmSize * _glyphTypeface.Height / 2.0;
                return _glyphTypeface.Baseline * _renderingEmSize;
            }
        }

        /// <summary>
        /// Distance from top to bottom of alignment box.
        /// </summary>
        private double Height
        {
            get
            {
                return _glyphTypeface.Height * _renderingEmSize;
            }
        }

        /// <summary>
        /// The baseline origin of the glyph run
        /// </summary>
        public Point BaselineOrigin
        {
            get
            {
                CheckInitialized();
                return _baselineOrigin;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                _baselineOrigin = value;
            }
        }

        /// <summary>
        /// Em size used for rendering.
        /// </summary>
        public double FontRenderingEmSize
        {
            get
            {
                CheckInitialized();
                return _renderingEmSize;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                _renderingEmSize = value;
            }
        }

        /// <summary>
        /// Returns GlyphTypeface for this object.
        /// </summary>
        public GlyphTypeface GlyphTypeface
        {
            get
            {
                CheckInitialized();
                return _glyphTypeface;
            }

            set
            {
                CheckInitializing(); // This can only be set during initialization.

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _glyphTypeface = value;
            }
        }

        /// <summary>
        /// Determines LTR/RTL reading order and bidi nesting.
        /// </summary>
        /// <value>The value of bidirectional nesting level.</value>
        public int BidiLevel
        {
            get
            {
                CheckInitialized();
                return _bidiLevel;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                _bidiLevel = value;
            }
        }

        /// <summary>
        /// Returns whether the glyph run is left to right or right to left.
        /// </summary>
        /// <value>true for LTR, false for RTL.</value>
        private bool IsLeftToRight
        {
            get
            {
                return (_bidiLevel & 1) == 0;
            }
        }

        /// <summary>
        /// Specifies whether to rotate characters/glyphs 90 degrees anti-clockwise
        /// and use vertical baseline positioning metrics.
        /// </summary>
        /// <value>true if the rotation should be applied, false otherwise.</value>
        public bool IsSideways
        {
            get
            {
                CheckInitialized();
                return (_flags & GlyphRunFlags.IsSideways) != 0;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                if (value)
                {
                    _flags |= GlyphRunFlags.IsSideways;
                }
                else
                {
                    _flags &= (~GlyphRunFlags.IsSideways);
                }
            }
        }

        /// <summary>
        /// Returns caret stops list for this GlyphRun or null if there is a caret stop for every UTF16 codepoint.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(BoolIListConverter))]
        public IList<bool> CaretStops
        {
            get
            {
                CheckInitialized();
                return _caretStops;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.

                // The list can be null, empty or non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                _caretStops = value;
            }
        }

        /// <summary>
        /// Returns whether there are any valid caret character hits within the glyph run.
        /// </summary>
        public bool IsHitTestable
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphRun

                if (CaretStops == null || CaretStops.Count == 0)
                {
                    // When CaretStops property is omitted, there is a caret stop for every UTF16 code point.
                    return true;
                }

                foreach (bool caretStop in CaretStops)
                {
                    if (caretStop)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The list that maps characters in the glyph run to glyph indices.
        /// There is one entry per character in Characters list.
        /// Each value gives the offset of the first glyph in GlyphIndices
        /// that represents the corresponding character in Characters.
        /// Where multiple characters map to a single glyph, or to a glyph group
        /// that cannot be broken down to map exactly to individual characters,
        /// the entries for all the characters have the same value:
        /// the offset of the first glyph that represents this group of characters.
        /// If the list is null or empty, sequential 1 to 1 mapping is assumed.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(UShortIListConverter))]
        public IList<ushort> ClusterMap
        {
            get
            {
                CheckInitialized();
                return _clusterMap;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.

                // The list can be null, empty or non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                _clusterMap = value;
            }
        }

        /// <summary>
        /// Returns the list of UTF16 code points that represent the Unicode content of the glyph run.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(CharIListConverter))]
        public IList<char> Characters
        {
            get
            {
                CheckInitialized();
                return _characters;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                // The list can be null, empty or non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                _characters = value;
            }
        }

        /// <summary>
        /// Array of 16 bit glyph numbers that represent this run.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(UShortIListConverter))]
        public IList<ushort> GlyphIndices
        {
            get
            {
                CheckInitialized();
                return _glyphIndices;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.

                // The list must be non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Count <= 0)
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeGreaterThanZero), "value");

                _glyphIndices = value;
            }
        }

        /// <summary>
        /// The list of advance widths, one for each glyph in GlyphIndices.
        /// The nominal origin of the nth glyph in the run (n>0) is the nominal origin
        /// of the n-1th glyph plus the n-1th advance width added along the runs advance vector.
        /// Base glyphs generally have a non-zero advance width, combining glyphs generally have a zero advance width.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(DoubleIListConverter))]
        public IList<double> AdvanceWidths
        {
            get
            {
                CheckInitialized();
                return _advanceWidths;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.

                // The list must be non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Count <= 0)
                    throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeGreaterThanZero), "value");

                _advanceWidths = value;
            }
        }

        /// <summary>
        /// Array of glyph offsets. Added to the nominal glyph origin calculated above to generate the final origin for the glyph.
        /// Base glyphs generally have a glyph offset of (0,0), combining glyphs generally have an offset
        /// that places them correctly on top of the nearest preceeding base glyph.
        /// </summary>
        [CLSCompliant(false)]
        [TypeConverter(typeof(PointIListConverter))]
        public IList<Point> GlyphOffsets
        {
            get
            {
                CheckInitialized();
                return _glyphOffsets;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                // The list can be null, empty or non-empty list.
                // The consistency with other lists would be checked at EndInit() time.
                _glyphOffsets = value;
            }
        }

        /// <summary>
        /// Returns the language associated with the glyph run.
        /// </summary>
        public XmlLanguage Language
        {
            get
            {
                CheckInitialized();
                return _language;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                _language = value;
            }
        }


        /// <summary>
        /// Identifies a specific device font for which the GlyphRun has been optimized. When a GlyphRun is
        /// being rendered on a device that has built-in support for this named font, then the GlyphRun should be rendered using a
        /// possibly device specific mechanism for selecting that font, and by sending the Unicode codepoints rather than the
        /// glyph indices. When rendering onto a device that does not include built-in support for the named font,
        /// this property should be ignored.
        /// </summary>
        public string DeviceFontName
        {
            get
            {
                CheckInitialized();
                return _deviceFontName;
            }
            set
            {
                CheckInitializing(); // This can only be set during initialization.
                _deviceFontName = value;
            }
        }

        #endregion Public Properties

        /// <summary>
        /// Glyph offsets
        /// The array is indexed starting with InitialGlyph
        /// </summary>
        internal Point GetGlyphOffset(int i)
        {
            if (_glyphOffsets == null || _glyphOffsets.Count == 0)
                return new Point(0, 0);
            return _glyphOffsets[i];
        }

        internal int GlyphCount
        {
            get
            {
                return _glyphIndices.Count;
            }
        }


        internal int CodepointCount
        {
            get
            {
                if (_characters != null && _characters.Count != 0)
                    return _characters.Count;
                if (_clusterMap != null && _clusterMap.Count != 0)
                    return _clusterMap.Count;
                return _glyphIndices.Count;
            }
        }

        #region Drawing and measurements

        /// <summary>
        /// Computes ink bounding box for the glyph run.
        /// The rectangle is relative to the glyph run origin.
        /// </summary>
        /// <returns> The ink bounding box of the glyph run </returns>
        public Rect ComputeInkBoundingBox()
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            if ((_flags & GlyphRunFlags.CacheInkBounds) != 0)
            {
                if (_inkBoundingBox != null)
                {
                    return (Rect)_inkBoundingBox;
                }
            }

            int glyphIndicesCount = _glyphIndices.Count;

            ushort[] glyphIndices = BufferCache.GetUShorts(glyphIndicesCount);
            _glyphIndices.CopyTo(glyphIndices, 0);

            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics = BufferCache.GetGlyphMetrics(glyphIndicesCount);

            _glyphTypeface.GetGlyphMetrics(glyphIndices,
                                           glyphIndicesCount,
                                           _renderingEmSize,
                                           _pixelsPerDip,
                                           _textFormattingMode,
                                           IsSideways,
                                           glyphMetrics);

            BufferCache.ReleaseUShorts(glyphIndices);
            glyphIndices = null;

            Rect bounds;

            // Special casing Left to Right layout with no italics allows an implementation that is
            // 12 times faster than the general case. Other combinations of Left to Right and
            // sideways layout also presents optimization opportunities that need to be implemented.
            // Italics is used infrequently, so adding the additional 8 routines necessary to handle italics
            // in combination with the other 4 routines is not justified.

            if (IsLeftToRight && !IsSideways)
            {
                bounds = ComputeInkBoundingBoxLtoR(glyphMetrics);
            }
            else
            {
                double accAdvance = 0;

                // We don't use Rect and Rect.Union to accumulate the bounding box
                // because this function is a hot spot and Rect methods perform extra checks that we don't need.

                double accLeft = double.PositiveInfinity;
                double accTop = double.PositiveInfinity;
                double accRight = double.NegativeInfinity;
                double accBottom = double.NegativeInfinity;

                double designToEm = _renderingEmSize / _glyphTypeface.DesignEmHeight;

                for (int i = 0; i < GlyphCount; ++i)
                {
                    EmGlyphMetrics emGlyphMetrics = new EmGlyphMetrics(glyphMetrics[i], designToEm, _pixelsPerDip, _textFormattingMode);

                    if (TextFormattingMode.Display == _textFormattingMode)
                    {
                        // Workaround for short or narrow glyphs - see comment in
                        // AdjustAdvanceForDisplayLayout
                        emGlyphMetrics.AdvanceHeight = AdjustAdvanceForDisplayLayout(
                            emGlyphMetrics.AdvanceHeight,
                            emGlyphMetrics.TopSideBearing,
                            emGlyphMetrics.BottomSideBearing);
                        emGlyphMetrics.AdvanceWidth = AdjustAdvanceForDisplayLayout(
                            emGlyphMetrics.AdvanceWidth,
                            emGlyphMetrics.LeftSideBearing,
                            emGlyphMetrics.RightSideBearing);
                    }

                    Point glyphOffset = GetGlyphOffset(i);
                    double originX;
                    if (IsLeftToRight)
                    {
                        originX = accAdvance + glyphOffset.X;
                    }
                    else
                    {
                        // no languages support sideways and right to left in the same run
                        Debug.Assert(!IsSideways);

                        originX = -accAdvance - (emGlyphMetrics.AdvanceWidth + glyphOffset.X);
                    }

                    accAdvance += _advanceWidths[i];

                    double horBaselineOriginY = -glyphOffset.Y;

                    double left, right, bottom, top;

                    if (IsSideways)
                    {
                        horBaselineOriginY += emGlyphMetrics.AdvanceWidth / 2.0;

                        bottom = horBaselineOriginY - emGlyphMetrics.LeftSideBearing;
                        top = horBaselineOriginY - emGlyphMetrics.AdvanceWidth + emGlyphMetrics.RightSideBearing;
                        left = originX + emGlyphMetrics.TopSideBearing;
                        right = left + emGlyphMetrics.AdvanceHeight - emGlyphMetrics.TopSideBearing - emGlyphMetrics.BottomSideBearing;
                    }
                    else
                    {
                        left = originX + emGlyphMetrics.LeftSideBearing;
                        right = originX + emGlyphMetrics.AdvanceWidth - emGlyphMetrics.RightSideBearing;
                        bottom = horBaselineOriginY + emGlyphMetrics.Baseline;
                        top = bottom - emGlyphMetrics.AdvanceHeight + emGlyphMetrics.TopSideBearing + emGlyphMetrics.BottomSideBearing;
                    }

                    // skip blank glyphs, as they don't contain ink
                    if (left + InkMetricsEpsilon >= right ||
                        top + InkMetricsEpsilon >= bottom)
                        continue;

                    if (accLeft > left)
                        accLeft = left;

                    if (accTop > top)
                        accTop = top;

                    if (accRight < right)
                        accRight = right;

                    if (accBottom < bottom)
                        accBottom = bottom;
                }

                if (accLeft > accRight)
                {
                    bounds = Rect.Empty;
                }
                else
                {
                    bounds = new Rect(
                        accLeft,
                        accTop,
                        accRight - accLeft,
                        accBottom - accTop
                    );
                }
            }

            BufferCache.ReleaseGlyphMetrics(glyphMetrics);

            //
            // GlyphRun.ComputeInkBoundingBox() does not produce a large enough rectangle
            // for Display formatted text
            // For some reason the assumptions
            // we make here about calculating the ink bounding box are not true for
            // display mode text as they are for ideal mode text. The bounding box
            // calculated using Display metrics for a Display formatted text run are
            // not large enough. This results in artifacts in rendering, and (slightly)
            // inaccurate hit testing. Inflate the bounds for now as a work around
            //
            // This also occurs for Ideal mode, for certain font/fontsize combinations.
            //
            // The amount of inflation depends on the fontsize, so that scaling
            // the result doesn't cause false hit-testing far away from the text
            // But inflate by at most 1px.
            if (CoreCompatibilityPreferences.GetIncludeAllInkInBoundingBox())
            {
                if (!bounds.IsEmpty)
                {
                    // Inflate bounds
                    double inflation = Math.Min(_renderingEmSize / 7.0, 1.0);
                    bounds.Inflate(inflation, inflation);
                }
            }
            else // user opted out of the fix - this is the 4.0 code
            {
                if (TextFormattingMode.Display == _textFormattingMode && !bounds.IsEmpty)
                {
                    // Inflate bounds
                    bounds.Inflate(1.0, 1.0);
                }
            }

            if ((_flags & GlyphRunFlags.CacheInkBounds) != 0)
            {
                _inkBoundingBox = bounds;
            }

            return bounds;
        }

        private double AdjustAdvanceForDisplayLayout(double advance,
                                                     double oneSideBearing,
                                                     double otherSideBearing)
        {
            // AdvanceHeight is used to compute the bounding box. In some case, eg. the dash
            // character '-', the bounding box is computed to be empty in Display
            // TextFormattingMode (because the metrics are rounded to be pixel aligned) and so the
            // dash is not rendered!
            //
            // Thus we coerce ah to be at least 1 pixel greater than tsb + bsb to gurantee that all
            // glyphs will be rendered (with non-zero bounding box).
            //
            // Note: A side effect to this is that spaces will now be processed when rendering.
            // That is, if the bounding box was empty the rendering engine will not process the
            // text for rendering. But now even spaces will be processed but will be rendered as
            // empty space.

            // This problem also applies to the width of some characters, such as '.', ':', and 'l'
            // The fix is the same: coerce AdvanceWidth to be at least
            // LeftSideBearing + RightSideBearing + 1 pixels.

            return Math.Max(advance, oneSideBearing + otherSideBearing + 1);
        }

        private Rect ComputeInkBoundingBoxLtoR(MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics)
        {
            // We don't use Rect and Rect.Union to accumulate the bounding box
            // because this function is a hot spot and Rect methods perform extra checks that we don't need.

            double accLeft = double.PositiveInfinity;
            double accTop = double.PositiveInfinity;
            double accRight = double.NegativeInfinity;
            double accBottom = double.NegativeInfinity;
            double accAdvance = 0;

            double designToEm = _renderingEmSize / _glyphTypeface.DesignEmHeight;

            int glyphCount = GlyphCount;

            for (int i = 0; i < glyphCount; ++i)
            {
                EmGlyphMetrics emGlyphMetrics = new EmGlyphMetrics(glyphMetrics[i], designToEm, _pixelsPerDip, _textFormattingMode);

                if (TextFormattingMode.Display == _textFormattingMode)
                {
                    // Workaround for short or narrow glyphs - see comment in
                    // AdjustAdvanceForDisplayLayout
                    emGlyphMetrics.AdvanceHeight = AdjustAdvanceForDisplayLayout(
                        emGlyphMetrics.AdvanceHeight,
                        emGlyphMetrics.TopSideBearing,
                        emGlyphMetrics.BottomSideBearing);
                    emGlyphMetrics.AdvanceWidth = AdjustAdvanceForDisplayLayout(
                        emGlyphMetrics.AdvanceWidth,
                        emGlyphMetrics.LeftSideBearing,
                        emGlyphMetrics.RightSideBearing);
                }

                if (GlyphOffsets != null)
                {
                    Point glyphOffset = GetGlyphOffset(i);

                    double originX = accAdvance + glyphOffset.X;

                    accAdvance += _advanceWidths[i];

                    double horBaselineOriginY = -glyphOffset.Y;

                    double left, right, bottom, top;

                    left = originX + emGlyphMetrics.LeftSideBearing;
                    right = originX + emGlyphMetrics.AdvanceWidth - emGlyphMetrics.RightSideBearing;
                    bottom = horBaselineOriginY + emGlyphMetrics.Baseline;
                    top = bottom - emGlyphMetrics.AdvanceHeight + emGlyphMetrics.TopSideBearing + emGlyphMetrics.BottomSideBearing;

                    // skip blank glyphs, as they don't contain ink
                    if (left + InkMetricsEpsilon >= right ||
                        top + InkMetricsEpsilon >= bottom)
                        continue;

                    if (accLeft > left)
                        accLeft = left;

                    if (accTop > top)
                        accTop = top;

                    if (accRight < right)
                        accRight = right;

                    if (accBottom < bottom)
                        accBottom = bottom;
                }
                else
                {
                    double left, right, top;

                    left = accAdvance + emGlyphMetrics.LeftSideBearing;
                    right = accAdvance + emGlyphMetrics.AdvanceWidth - emGlyphMetrics.RightSideBearing;
                    top = emGlyphMetrics.Baseline - emGlyphMetrics.AdvanceHeight + emGlyphMetrics.TopSideBearing + emGlyphMetrics.BottomSideBearing;

                    accAdvance += _advanceWidths[i];

                    // skip blank glyphs, as they don't contain ink
                    if (left + InkMetricsEpsilon >= right ||
                        top + InkMetricsEpsilon >= emGlyphMetrics.Baseline)
                        continue;

                    if (accLeft > left)
                        accLeft = left;

                    if (accTop > top)
                        accTop = top;

                    if (accRight < right)
                        accRight = right;

                    if (accBottom < emGlyphMetrics.Baseline)
                        accBottom = emGlyphMetrics.Baseline;
                }
            }

            if (accLeft > accRight)
                return Rect.Empty;

            return new Rect(
                    accLeft,
                    accTop,
                    accRight - accLeft,
                    accBottom - accTop
                );
        }

        /// <summary>
        /// Obtains geometry for the glyph run.
        /// </summary>
        /// <returns>The geometry returned contains the combined geometry of all glyphs in the glyph run.
        /// Overlapping contours are merged by performing a Boolean union operation.</returns>
        public Geometry BuildGeometry()
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            GeometryGroup accumulatedGeometry = null;
            double accAdvance = 0;
            for (int i = 0; i < GlyphCount; ++i)
            {
                ushort glyphIndex = _glyphIndices[i];

                double originX;
                if (IsLeftToRight)
                {
                    originX = accAdvance;
                    originX += GetGlyphOffset(i).X;
                }
                else
                {
                    // no languages support sideways and right to left in the same run
                    Debug.Assert(!IsSideways);

                    double nominalAdvance = TextFormatterImp.RoundDip(_glyphTypeface.GetAdvanceWidth(glyphIndex, _pixelsPerDip, _textFormattingMode, IsSideways) * _renderingEmSize,
                        _pixelsPerDip, _textFormattingMode);

                    originX = -accAdvance;
                    originX -= (nominalAdvance + GetGlyphOffset(i).X);
                }
                accAdvance += _advanceWidths[i];

                double originY = -GetGlyphOffset(i).Y;

                Geometry glyphGeometry = _glyphTypeface.ComputeGlyphOutline(glyphIndex, IsSideways, _renderingEmSize);
                if (glyphGeometry.IsEmpty())
                    continue;

                // transform glyphGeometry to the glyph origin
                glyphGeometry.Transform = new TranslateTransform(originX + _baselineOrigin.X, originY + _baselineOrigin.Y);

                if (accumulatedGeometry == null)
                {
                    accumulatedGeometry = new GeometryGroup();
                    accumulatedGeometry.FillRule = FillRule.Nonzero;
                }

                accumulatedGeometry.Children.Add(glyphGeometry.GetOutlinedPathGeometry(RelativeFlatteningTolerance, ToleranceType.Relative));
            }
            // Make sure to always return Geometry.Empty from public methods for empty geometries.
            if (accumulatedGeometry == null || accumulatedGeometry.IsEmpty())
                return Geometry.Empty;
            return accumulatedGeometry;
        }

        /// <summary>
        /// Computes the alignment box for the glyph run.
        /// The alignment box is relative to origin.
        /// </summary>
        /// <returns>The alignment box for the glyph run.</returns>
        public Rect ComputeAlignmentBox()
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun

            // cache AdvanceWidth value in a local variable because it involves a loop
            double advanceWidth = AdvanceWidth;

            // AdvanceWidth could be negative, but Rect.Width cannot
            // be negative. 
            bool extendToRight = IsLeftToRight;
            if (advanceWidth < 0.0)
            {
                extendToRight = !extendToRight;
                advanceWidth = -advanceWidth;
            }

            if (extendToRight)
            {
                return new Rect(
                    0,
                    -Ascent,
                    advanceWidth,
                    Height
                    );
            }
            else
            {
                return new Rect(
                    -advanceWidth,
                    -Ascent,
                    advanceWidth,
                    Height
                    );
            }
        }

        /// <summary>
        /// Temporary helper to draw a glyph run background.
        /// We hope to remove all uses of it, as fundamentally this is not the right way
        /// to handle background drawing.
        /// </summary>
        internal void EmitBackground(DrawingContext dc, Brush backgroundBrush)
        {
            double advanceWidth;

            // AdvanceWidth could be negative, but Rect.Width cannot
            // be negative. Don't paint the
            // background - it would paint over earlier glyphs.
            if (backgroundBrush != null && (advanceWidth = AdvanceWidth) > 0.0)
            {
                Rect backgroundRect;

                if (IsLeftToRight)
                {
                    backgroundRect = new Rect(
                        _baselineOrigin.X,
                        _baselineOrigin.Y - Ascent,
                        advanceWidth,
                        Height
                        );
                }
                else
                {
                    backgroundRect = new Rect(
                        _baselineOrigin.X - advanceWidth,
                        _baselineOrigin.Y - Ascent,
                        advanceWidth,
                        Height
                        );
                }

                dc.DrawRectangle(
                    backgroundBrush,
                    null,
                    backgroundRect
                    );
            }
        }

        /// <summary>
        /// Helper that scales a raw dwrite GlyphMetrics into em space.
        /// </summary>
        private struct EmGlyphMetrics
        {
            internal EmGlyphMetrics(MS.Internal.Text.TextInterface.GlyphMetrics glyphMetrics, double designToEm, double pixelsPerDip, TextFormattingMode textFormattingMode)
            {
                if (TextFormattingMode.Display == textFormattingMode)
                {
                    this.AdvanceWidth = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceWidth, pixelsPerDip);
                    this.AdvanceHeight = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceHeight, pixelsPerDip);
                    this.LeftSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.LeftSideBearing, pixelsPerDip);
                    this.RightSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.RightSideBearing, pixelsPerDip);
                    this.TopSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.TopSideBearing, pixelsPerDip);
                    this.BottomSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.BottomSideBearing, pixelsPerDip);
                    this.Baseline = TextFormatterImp.RoundDipForDisplayMode(designToEm * GlyphTypeface.BaselineHelper(glyphMetrics), pixelsPerDip);
                }
                else
                {
                    this.AdvanceWidth = designToEm * glyphMetrics.AdvanceWidth;
                    this.AdvanceHeight = designToEm * glyphMetrics.AdvanceHeight;
                    this.LeftSideBearing = designToEm * glyphMetrics.LeftSideBearing;
                    this.RightSideBearing = designToEm * glyphMetrics.RightSideBearing;
                    this.TopSideBearing = designToEm * glyphMetrics.TopSideBearing;
                    this.BottomSideBearing = designToEm * glyphMetrics.BottomSideBearing;
                    this.Baseline = designToEm * GlyphTypeface.BaselineHelper(glyphMetrics);
                }
            }

            internal double LeftSideBearing;
            internal double AdvanceWidth;
            internal double RightSideBearing;
            internal double TopSideBearing;
            internal double AdvanceHeight;
            internal double BottomSideBearing;
            internal double Baseline;
        }

        #endregion Drawing and measurements

        #region DUCE.IResource implementation


        /// <summary>
        /// A structure to keep two scaling ratios fetched from given Matrix.
        /// </summary>
        internal struct Scale
        {
            internal Scale(ref Matrix matrix)
            {
                double m11 = matrix.M11;
                double m12 = matrix.M12;
                double m21 = matrix.M21;
                double m22 = matrix.M22;

                // Calculate redundant data.
                _baseVectorX = Math.Sqrt(m11 * m11 + m12 * m12);

                // Check for wrong matrix.
                if (DoubleUtil.IsNaN(_baseVectorX))
                    _baseVectorX = 0;

                _baseVectorY = _baseVectorX == 0 ? 0 : Math.Abs(m11 * m22 - m12 * m21) / _baseVectorX;
                if (DoubleUtil.IsNaN(_baseVectorY))
                    _baseVectorY = 0;
            }

            internal bool IsValid
            {
                get
                {
                    return _baseVectorX != 0 && _baseVectorY != 0;
                }
            }

            internal bool IsSame(ref Scale scale)
            {
                //
                // allow some imprecision that can appear because
                // of matrix computations.
                //
                return _baseVectorX * 0.999999999 <= scale._baseVectorX &&
                       _baseVectorX * 1.000000001 >= scale._baseVectorX &&
                       _baseVectorY * 0.999999999 <= scale._baseVectorY &&
                       _baseVectorY * 1.000000001 >= scale._baseVectorY;
            }

            internal double _baseVectorX, _baseVectorY;
        }

        private DUCE.MultiChannelResource _mcr = new DUCE.MultiChannelResource();

        /// <summary>
        /// Generate a series of requests to create or update
        /// slave glyph run resource and all depending data.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun
            using (CompositionEngineLock.Acquire())
            {
                if (_mcr.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_GLYPHRUN))
                {
                    CreateOnChannel(channel);
                }

                return _mcr.GetHandle(channel);
            }
}

        /// <summary>
        /// Generates request to delete slave glyph run resource.
        /// </summary>
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun
            using (CompositionEngineLock.Acquire())
            {
                _mcr.ReleaseOnChannel(channel);
            }
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        void DUCE.IResource.RemoveChildFromParent(DUCE.IResource parent, DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.Get3DHandle(DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns current resource handle, allocated recently by AddRefOnChannel.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphRun
            return _mcr.GetHandle(channel);
        }

        int DUCE.IResource.GetChannelCount()
        {
            return _mcr.GetChannelCount();
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return _mcr.GetChannel(index);
        }

        /// <summary>
        /// Send to channel command sequence to create slave resource.
        /// </summary>
        private void CreateOnChannel(DUCE.Channel channel)
        {
            Debug.Assert(_glyphTypeface != null);

            int glyphCount = GlyphCount;

            //
            // The InkBoundingBox + the Origin produce the true InkBoundingBox.
            //
            // Not sure why the bounding box code doesn't adjust for this when you
            // ask for the bounding box, instead everything
            // that is interested in the bounding box has to do this calculation.
            //


            Rect adjustedInkBoundingBox = ComputeInkBoundingBox();

            if (!adjustedInkBoundingBox.IsEmpty)
            {
                adjustedInkBoundingBox.Offset((Vector)BaselineOrigin);
            }

            DUCE.MILCMD_GLYPHRUN_CREATE command;
            command.Type = MILCMD.MilCmdGlyphRunCreate;
            command.Handle = _mcr.GetHandle(channel);
            command.GlyphRunFlags = ComposeFlags();
            command.Origin.X = (float)_baselineOrigin.X;
            command.Origin.Y = (float)_baselineOrigin.Y;
            command.MuSize = (float)_renderingEmSize;
            command.ManagedBounds = (Rect)adjustedInkBoundingBox;
            command.GlyphCount = checked((UInt16)glyphCount);
            command.BidiLevel = checked((UInt16)_bidiLevel);
            command.pIDWriteFont = (UInt64)_glyphTypeface.GetDWriteFontAddRef;
            command.DWriteTextMeasuringMethod = (UInt16)DWriteTypeConverter.
                                                        Convert(_textFormattingMode);

            // Advances
            // Offsets
            // Change HasYPositions to HasOffsets

            // BidiLevel
            // Fix font file name (remove first 4 characters)

            unsafe {
                // calculate variable data size

                // glyph indices
                int varDataSize = glyphCount * sizeof(ushort);

                // advance widths
                varDataSize += glyphCount * sizeof(float);

                // offsets
                if (_glyphOffsets != null && _glyphOffsets.Count != 0)
                {
                    varDataSize += glyphCount * (2 * sizeof(float));
                }

                channel.BeginCommand(
                    (byte*)&command,
                    sizeof(DUCE.MILCMD_GLYPHRUN_CREATE),
                    varDataSize
                    );

                // Send indices
                // Send advances
                // [optional] Send offsets

                {
                    // transmit glyph indices
                    {
                        if (glyphCount <= MaxStackAlloc / sizeof(ushort))
                        {
                            // glyph count small enough, send all data at once
                            ushort* pGlyphIndices = stackalloc ushort[glyphCount];

                            for (int i = 0; i < glyphCount; ++i)
                            {
                                pGlyphIndices[i] = _glyphIndices[i];
                            }
                            channel.AppendCommandData((byte*)pGlyphIndices, glyphCount * sizeof(ushort));
                        }
                        else
                        {
                            // glyph count is not small, use per-glyph transmitting
                            for (int i = 0; i < glyphCount; ++i)
                            {
                                ushort glyphIndex = _glyphIndices[i];
                                channel.AppendCommandData((byte*)&glyphIndex, sizeof(ushort));
                            }
                        }
                    }

                    // transmit advance widths
                    {
                        if (glyphCount <= MaxStackAlloc / sizeof(float))
                        {
                            float *pAdvanceWidths = stackalloc float[glyphCount];

                            for (int i = 0; i < glyphCount; i++)
                            {
                                pAdvanceWidths[i] = (float)_advanceWidths[i];
                            }
                            channel.AppendCommandData((byte*)pAdvanceWidths, glyphCount * sizeof(float));
                        }
                        else
                        {
                            for (int i = 0; i < glyphCount; i++)
                            {
                                float advanceWidth = (float)_advanceWidths[i];
                                channel.AppendCommandData((byte*)&advanceWidth, sizeof(float));
                            }
                        }
                    }

                    // offsets
                    {
                        if (_glyphOffsets != null && _glyphOffsets.Count != 0)
                        {
                            if (glyphCount <= MaxStackAlloc / (2 * sizeof(float)))
                            {
                                float *pOffsets = stackalloc float[2*glyphCount];

                                for (int i = 0; i < glyphCount; i++)
                                {
                                    pOffsets[2*i] = (float)_glyphOffsets[i].X;
                                    pOffsets[2*i+1] = (float)_glyphOffsets[i].Y;
                                }
                                channel.AppendCommandData((byte*)pOffsets, 2 * glyphCount * sizeof(float));
                            }
                            else
                            {
                                for (int i = 0; i < glyphCount; i++)
                                {
                                    float x = (float)_glyphOffsets[i].X;
                                    float y = (float)_glyphOffsets[i].Y;
                                    channel.AppendCommandData((byte*)&x, sizeof(float));
                                    channel.AppendCommandData((byte*)&y, sizeof(float));
                                }
                            }
                        }
                    }
}
                channel.EndCommand();
            }
        }


        /// <summary>
        /// Gather flags that affect:
        ///  - glyph run rendering
        ///  - glyph rasterization
        ///  - the way how glyph run data is packed
        /// </summary>
        private UInt16 ComposeFlags()
        {
            UInt16 flags = 0;

            if (IsSideways)
                flags |= (UInt16)MilGlyphRun.Sideways;

            if (_glyphOffsets != null && _glyphOffsets.Count != 0)
                flags |= (UInt16)MilGlyphRun.HasOffsets;

            return flags;
        }

        #endregion DUCE.IResource implementation

        #region Hit testing

        /// <summary>
        /// Given a code point index in the caret stop array, finds the nearest pair of caret stops.
        /// </summary>
        /// <param name="characterIndex">Character index to start the search from. Doesn't have to be snapped.</param>
        /// <param name="caretStops">GlyphRun CaretStops array. Guaranteed to be non-null.</param>
        /// <param name="caretStopIndex">Nearest caret stop index, or -1 if there are no caret stops.</param>
        /// <param name="codePointsUntilNextStop">Code points until the next caret stop, or -1 if there is no next caret stop.</param>
        private void FindNearestCaretStop(
            int         characterIndex,
            IList<bool> caretStops,
            out int     caretStopIndex,
            out int     codePointsUntilNextStop)
        {
            caretStopIndex = -1;
            codePointsUntilNextStop = -1;

            if (characterIndex < 0 || characterIndex >= caretStops.Count)
                return;

            // Find the closest caret stop at the character index or to the left of it.
            for (int i = characterIndex; i >= 0; --i)
            {
                if (caretStops[i])
                {
                    caretStopIndex = i;
                    break;
                }
            }

            // Couldn't find a caret stop at the character index or to the left of it.
            // Search to the right.
            if (caretStopIndex == -1)
            {
                for (int i = characterIndex + 1; i < caretStops.Count; ++i)
                {
                    if (caretStops[i])
                    {
                        caretStopIndex = i;
                        break;
                    }
                }
            }

            // No caret stops found, the glyph run is not hit testable.
            if (caretStopIndex == -1)
            {
                return;
            }

            for (int lastStop = caretStopIndex + 1; lastStop < caretStops.Count; ++lastStop)
            {
                if (caretStops[lastStop])
                {
                    // There is a next caret stop.
                    codePointsUntilNextStop = lastStop - caretStopIndex;
                    return;
                }
            }

            // There is no next caret stop.
        }


        /// <summary>
        /// This class implements behavior of a Boolean list that contains all true values.
        /// This allows us to have a single code path in hit testing API.
        /// </summary>
        private class DefaultCaretStopList : IList<bool>
        {
            public DefaultCaretStopList(int codePointCount)
            {
                _count = codePointCount + 1;
            }

            #region IList<bool> Members

            public int IndexOf(bool item)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, bool item)
            {
                throw new NotSupportedException();
            }

            public bool this[int index]
            {
                get
                {
                    return true;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region ICollection<bool> Members

            public void Add(bool item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(bool item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(bool[] array, int arrayIndex)
            {
                throw new NotSupportedException();
            }

            public int Count
            {
                get { return _count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(bool item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<bool> Members

            IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }

            #endregion

            private int _count;
        }

        /// <summary>
        /// This class implements behavior of a 1:1 cluster map.
        /// This allows us to have a single code path in hit testing API.
        /// </summary>
        private class DefaultClusterMap : IList<ushort>
        {
            public DefaultClusterMap(int count)
            {
                _count = count;
            }

            #region IList<ushort> Members

            public int IndexOf(ushort item)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, ushort item)
            {
                throw new NotSupportedException();
            }

            public ushort this[int index]
            {
                get
                {
                    return (ushort)index;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region ICollection<ushort> Members

            public void Add(ushort item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(ushort item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(ushort[] array, int arrayIndex)
            {
                throw new NotSupportedException();
            }

            public int Count
            {
                get { return _count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(ushort item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<ushort> Members

            IEnumerator<ushort> IEnumerable<ushort>.GetEnumerator()
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }

            #endregion

            private int _count;
        }

        #endregion Hit testing

        #region ISupportInitialize interface for Xaml serialization

        void ISupportInitialize.BeginInit()
        {
            if (IsInitialized)
            {
                // Cannot initialize a GlyphRun that is completely initialized.
                throw new InvalidOperationException(SR.Get(SRID.OnlyOneInitialization));
            }

            if (IsInitializing)
            {
                // Cannot initialize a GlyphRun that is already being initialized.
                throw new InvalidOperationException(SR.Get(SRID.InInitialization));
            }

            IsInitializing = true;
        }

        void ISupportInitialize.EndInit()
        {
            if (!IsInitializing)
            {
                // Cannot EndInit a GlyphRun that is not being initialized.
                throw new InvalidOperationException(SR.Get(SRID.NotInInitialization));
            }

            //
            // Fully initilize the GlyphRun. The method will check for consistency
            // between all the properties.
            //
            Initialize(
                _glyphTypeface,
                _bidiLevel,
                (_flags & GlyphRunFlags.IsSideways) != 0,
                _renderingEmSize,
                _pixelsPerDip,
                _glyphIndices,
                _baselineOrigin,
                // In case the layout mode is not Ideal then we cannot use ThousandthOfEmReal* since ThousandthOfEmReal* internally stores doubles as integers and hence there is some lost percision
                // that can result in glyphs that were pixel aligned be not so. This is not important for ideal layout but is of great importance for compatible with layout.
                (_advanceWidths == null ? null : ((_textFormattingMode != TextFormattingMode.Ideal) ? (IList<double>)(new List<double>()) : (IList<double>)(new ThousandthOfEmRealDoubles(_renderingEmSize, _advanceWidths)))),
                (_glyphOffsets == null ? null : ((_textFormattingMode != TextFormattingMode.Ideal) ? (IList<Point>)(new List<Point>()) : (IList<Point>)(new ThousandthOfEmRealPoints(_renderingEmSize, _glyphOffsets)))),
                _characters,
                _deviceFontName,
                _clusterMap,
                _caretStops,
                _language,
                TextFormattingMode.Ideal
                );

            // User should be able to fix errors that are only caught at EndInit() time. So set Initializing flag to
            // false after Initialization succeeds.
            IsInitializing = false;
        }

        private void CheckInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.Get(SRID.InitializationIncomplete));
            }

            // Ensure the bits are set consistently. The object cannot be in both states.
            Debug.Assert(!IsInitializing);
        }

        private void CheckInitializing()
        {
            if (!IsInitializing)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotInInitialization));
            }

            // Ensure the bits are set consistently. The object cannot be in both states.
            Debug.Assert(!IsInitialized);
        }

        private bool IsInitializing
        {
            get { return (_flags & GlyphRunFlags.IsInitializing) != 0; }
            set
            {
                if (value)
                {
                    _flags |= GlyphRunFlags.IsInitializing;
                }
                else
                {
                    _flags &= (~GlyphRunFlags.IsInitializing);
                }
            }
        }

        private bool IsInitialized
        {
            get { return (_flags & GlyphRunFlags.IsInitialized) != 0; }
            set
            {
                if (value)
                {
                    _flags |= GlyphRunFlags.IsInitialized;
                }
                else
                {
                    _flags &= (~GlyphRunFlags.IsInitialized);
                }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Enumerations
        //
        //------------------------------------------------------

        #region Private Enumerations

        /// <summary>
        /// Glyph run flags.
        /// </summary>
        [Flags]
        private enum GlyphRunFlags : byte
        {
            /// <summary>
            /// No flags set.
            /// It also represents the state in which the GlyphRun has not been initialized.
            /// At this state, all operations on the object would cause InvalidOperationException.
            /// The object can only transit to 'IsInitializing' state with BeginInit() call.
            /// </summary>
            None                = 0x00,

            /// <summary>
            /// Set to display the GlyphRun sideways.
            /// </summary>
            IsSideways          = 0x01,

            /// <summary>
            /// The state in which the GlyphRun object is fully initialized. At this state the object
            /// is fully functional. There is no valid transition out of the state.
            /// </summary>
            IsInitialized       = 0x08,

            /// <summary>
            /// The state in which the GlyphRun is being initialized. At this state, user can
            /// set values into the required properties. The object can only transit to 'IsInitialized' state
            /// with EndInit() call.
            /// </summary>
            IsInitializing      = 0x10,

            /// <summary>
            /// Caching ink bounds
            /// </summary>
            CacheInkBounds      = 0x20,
        }

        #endregion Private Enumerations


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        private Point               _baselineOrigin;

        private GlyphRunFlags       _flags;
        private double              _renderingEmSize;
        private IList<ushort>       _glyphIndices;
        private IList<double>       _advanceWidths;
        private IList<Point>        _glyphOffsets;
        private int                 _bidiLevel;
        private GlyphTypeface       _glyphTypeface;
        private IList<char>         _characters;
        private IList<ushort>       _clusterMap;
        private IList<bool>         _caretStops;
        private XmlLanguage         _language;
        private string              _deviceFontName;
        private object              _inkBoundingBox;    // Used when CacheInkBounds is on
        private TextFormattingMode      _textFormattingMode;
        private float               _pixelsPerDip = MS.Internal.FontCache.Util.PixelsPerDip;

        // the sine of 20 degrees
        private const double        Sin20 = 0.34202014332566873304409961468226;

        // This is the precision that is used to decide that glyph metrics are equal,
        // for example when detecting blank glyphs.
        // The chosen value is greater than typical floating point precision loss
        // but smaller than typical design font unit (1/1024th or 1/2048th).
        private const double        InkMetricsEpsilon = 0.0000001;

        // Dummy font hinting size
        private const double        DefaultFontHintingSize = 12.0;

        // Tolerance for flattening Bezier curves when calling GetOutlinedPathGeometry.
        internal static double        RelativeFlatteningTolerance = 0.01;

        // The constants that delimit glyph run size.
        internal const int MaxGlyphCount = 0xFFFF;
        internal const int MaxStackAlloc = 1024;

        #endregion Private Fields
    }
}

