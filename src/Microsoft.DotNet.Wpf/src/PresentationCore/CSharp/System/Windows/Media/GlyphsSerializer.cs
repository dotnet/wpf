// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using MS.Internal;
using MS.Win32;
using Microsoft.Win32.SafeHandles;

using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;

using MS.Internal.PresentationCore;

namespace System.Windows.Media
{
    /// <summary>
    ///
    /// </summary>
    [FriendAccessAllowed]   // used by System.Printing.dll
    internal class GlyphsSerializer
    {
        #region public methods
        /// <summary>
        ///
        /// </summary>
        /// <param name="glyphRun"></param>
        public GlyphsSerializer(GlyphRun glyphRun)
        {
            if (glyphRun == null)
            {
                throw new ArgumentNullException("glyphRun");
            }

            _glyphTypeface = glyphRun.GlyphTypeface;
            _milToEm = EmScaleFactor / glyphRun.FontRenderingEmSize;

            _sideways = glyphRun.IsSideways;

            _characters = glyphRun.Characters;
            _caretStops = glyphRun.CaretStops;

            // the first value in the cluster map can be non-zero, in which case it's applied as an offset to all
            // subsequent entries in the cluster map
            _clusters = glyphRun.ClusterMap;
            if (_clusters != null)
                _glyphClusterInitialOffset = _clusters[0];

            _indices = glyphRun.GlyphIndices;
            _advances = glyphRun.AdvanceWidths;
            _offsets = glyphRun.GlyphOffsets;

            // "100,50,,0;".Length is a capacity estimate for an individual glyph
            _glyphStringBuider = new StringBuilder(10);

            // string length * _glyphStringBuider.Capacity is an estimate for the whole string
            _indicesStringBuider = new StringBuilder(
                Math.Max( 
                    (_characters == null ? 0 : _characters.Count), 
                    _indices.Count
                ) 
                * _glyphStringBuider.Capacity
            );
        }

        /// <summary>
        /// Encode glyph run glyph information into Indices, UnicodeString and CaretStops string.
        /// </summary>
        public void ComputeContentStrings(out string characters, out string indices, out string caretStops)
        {
            if (_clusters != null)
            {
                // the algorithm works by finding (n:m) clusters and appending m glyphs for each cluster
                int characterIndex;
                int glyphClusterStart = 0;
                int charClusterStart = 0;
                bool forceNewCluster = true;

                for (characterIndex = 0; characterIndex < _clusters.Count; ++characterIndex)
                {
                    if (forceNewCluster)
                    {
                        glyphClusterStart = _clusters[characterIndex];
                        charClusterStart = characterIndex;
                        forceNewCluster = false;
                        continue;
                    }

                    if (_clusters[characterIndex] != glyphClusterStart)
                    {
                        // end of cluster, flush it
                        Debug.Assert(_clusters[characterIndex] > glyphClusterStart);
                        AddCluster(glyphClusterStart - _glyphClusterInitialOffset, _clusters[characterIndex] - _glyphClusterInitialOffset, charClusterStart, characterIndex);

                        // start a new cluster
                        glyphClusterStart = _clusters[characterIndex];
                        charClusterStart = characterIndex;
                    }
                    // otherwise, we are still within a cluster
                }

                // flush the last cluster
                Debug.Assert(_indices.Count > glyphClusterStart - _glyphClusterInitialOffset);
                AddCluster(glyphClusterStart - _glyphClusterInitialOffset, _indices.Count, charClusterStart, characterIndex);
            }
            else
            {
                // zero cluster map means 1:1 mapping
                Debug.Assert(_characters == null || _characters.Count == 0 || _indices.Count == _characters.Count);
                for (int i = 0; i < _indices.Count; ++i)
                    AddCluster(i, i + 1, i, i + 1);
            }

            // remove trailing semicolons
            RemoveTrailingCharacters(_indicesStringBuider, GlyphSeparator);
            indices = _indicesStringBuider.ToString();

            if (_characters == null || _characters.Count == 0)
            {
                characters = string.Empty;
            }
            else
            {
                StringBuilder builder = new StringBuilder(_characters.Count);
                foreach(char ch in _characters)
                {
                    builder.Append(ch);
                }

                characters = builder.ToString();
            }

            caretStops = CreateCaretStopsString();
        }

        #endregion public methods

        #region private methods
        private void RemoveTrailingCharacters(StringBuilder sb, char trailingCharacter)
        {
            int length = sb.Length;
            int trailingCharIndex = length - 1;

            while (trailingCharIndex >= 0)
            {
                if (sb[trailingCharIndex] != trailingCharacter)
                    break;

                --trailingCharIndex;
            }

            sb.Length = trailingCharIndex + 1;
        }

        private void AddGlyph(int glyph, int sourceCharacter)
        {
            Debug.Assert(_glyphStringBuider.Length == 0);

            // glyph index
            ushort fontIndex = _indices[glyph];
            ushort glyphIndexFromCmap;

            if (sourceCharacter == -1 ||
                !_glyphTypeface.CharacterToGlyphMap.TryGetValue(sourceCharacter, out glyphIndexFromCmap) ||
                fontIndex != glyphIndexFromCmap)
            {
                _glyphStringBuider.Append(fontIndex.ToString(CultureInfo.InvariantCulture));
            }

            _glyphStringBuider.Append(GlyphSubEntrySeparator);

            // advance width
            int normalizedAdvance = (int)Math.Round(_advances[glyph] * _milToEm);
            double fontAdvance = _sideways ? _glyphTypeface.AdvanceHeights[fontIndex] : _glyphTypeface.AdvanceWidths[fontIndex];
            if (normalizedAdvance != (int)Math.Round(fontAdvance * EmScaleFactor))
            {
                _glyphStringBuider.Append(normalizedAdvance.ToString(CultureInfo.InvariantCulture));
            }

            _glyphStringBuider.Append(GlyphSubEntrySeparator);

            // u,v offset
            if (_offsets != null)
            {
                // u offset
                int offset = (int)Math.Round(_offsets[glyph].X * _milToEm);

                if (offset != 0)
                    _glyphStringBuider.Append(offset.ToString(CultureInfo.InvariantCulture));

                _glyphStringBuider.Append(GlyphSubEntrySeparator);

                // v offset
                offset = (int)Math.Round(_offsets[glyph].Y * _milToEm);
                if (offset != 0)
                    _glyphStringBuider.Append(offset.ToString(CultureInfo.InvariantCulture));

                _glyphStringBuider.Append(GlyphSubEntrySeparator);
            }

            // flags are not implemented yet
            // remove trailing commas
            RemoveTrailingCharacters(_glyphStringBuider, GlyphSubEntrySeparator);
            _glyphStringBuider.Append(GlyphSeparator);
            _indicesStringBuider.Append(_glyphStringBuider.ToString());

            // reset for next glyph
            _glyphStringBuider.Length = 0;
        }

        private void AddCluster(int glyphClusterStart, int glyphClusterEnd, int charClusterStart, int charClusterEnd)
        {
            int charactersInCluster = charClusterEnd - charClusterStart;
            int glyphsInCluster = glyphClusterEnd - glyphClusterStart;

            // no source character to deduce glyph properties from
            int sourceCharacter = -1;

            // the format is ... [(CharacterClusterSize[:GlyphClusterSize])] GlyphIndex ...
            if (glyphsInCluster != 1)
            {
                _indicesStringBuider.AppendFormat(CultureInfo.InvariantCulture, "({0}:{1})", charactersInCluster, glyphsInCluster);
            }
            else
            {
                if (charactersInCluster != 1)
                    _indicesStringBuider.AppendFormat(CultureInfo.InvariantCulture, "({0})", charactersInCluster);
                else
                {
                    // 1:1 cluster, we can omit (n:m) specification and possibly deduce some
                    // glyph properties from character
                    if (_characters != null && _characters.Count != 0)
                        sourceCharacter = _characters[charClusterStart];
                }
            }

            for (int glyph = glyphClusterStart; glyph < glyphClusterEnd; ++glyph)
            {
                AddGlyph(glyph, sourceCharacter);
            }
        }

        private string CreateCaretStopsString()
        {
            if (_caretStops == null)
                return String.Empty;

            // Since the trailing 0xF (i.e. all true) entries in the caret stop specifications can be omitted,
            // we can limit the caret stop list walk until the last nibble that contains 'false'.
            
            int caretStopStringLength = 0;
            int lastCaretStop = 0;
            for (int i = _caretStops.Count - 1; i >= 0; --i)
            {
                if (!_caretStops[i])
                {
                    caretStopStringLength = (i + 4) / 4;

                    // lastCaretStop to consider when building, the rest will correpond to 0xF entries
                    lastCaretStop = Math.Min(i | 3, _caretStops.Count - 1);

                    break;
                }
            }

            // All values are set to true, so we don't have to include caret stop string at all.
            if (caretStopStringLength == 0)
                return String.Empty;

            StringBuilder sb = new StringBuilder(caretStopStringLength);

            byte mask = 0x8;
            byte accumulatedValue = 0;

            for (int i = 0; i <= lastCaretStop; ++i)
            {
                if (_caretStops[i])
                    accumulatedValue |= mask;

                if (mask != 1)
                    mask >>= 1;
                else
                {
                    sb.AppendFormat("{0:x1}", accumulatedValue);
                    accumulatedValue = 0;
                    mask = 0x8;
                }
            }
            if (mask != 0x8)
                sb.AppendFormat("{0:x1}", accumulatedValue);

            Debug.Assert(caretStopStringLength == sb.ToString().Length);

            return sb.ToString();
        }

        #endregion private methods
        #region private data
        private GlyphTypeface _glyphTypeface;

        private IList<char> _characters;

        private double _milToEm;

        private bool _sideways;

        private int _glyphClusterInitialOffset;

        private IList<ushort> _clusters;

        private IList<ushort> _indices;

        private IList<double> _advances;

        private IList<Point> _offsets;

        private IList<bool>     _caretStops;

        private StringBuilder _indicesStringBuider;

        private StringBuilder _glyphStringBuider;

        private const char GlyphSubEntrySeparator = ',';

        private const char GlyphSeparator = ';';

        private const double EmScaleFactor = 100.0;
        #endregion region private data
   }
}
