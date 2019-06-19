// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implementation of text shapeable symbols for characters
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.TextFormatting;
using MS.Internal.Shaping;
using MS.Internal.Text.TextInterface;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// A specialized TextShapeableSymbols implemented by TextFormatter to represent
    /// a collection of glyphs from a physical typeface.
    /// </summary>
    internal sealed class TextShapeableCharacters : TextShapeableSymbols
    {
        private CharacterBufferRange    _characterBufferRange;
        private TextFormattingMode      _textFormattingMode;
        private bool                    _isSideways;
        private TextRunProperties       _properties;
        private double                  _emSize;    // after-scaled

        private ItemProps               _textItem;
        private ShapeTypeface           _shapeTypeface;
        private bool                    _nullShape;

        #region Constructors

        /// <summary>
        /// Construct a shapeable characters object
        /// </summary>
        /// <remarks>
        /// The shapeTypeface parameter can be null if and only if CheckFastPathNominalGlyphs
        /// has previously returned true.
        /// </remarks>
        internal TextShapeableCharacters(
            CharacterBufferRange    characterRange,
            TextRunProperties       properties,
            double                  emSize,
            ItemProps               textItem,
            ShapeTypeface           shapeTypeface,
            bool                    nullShape,
            TextFormattingMode      textFormattingMode,
            bool isSideways
            )
        {
            _isSideways = isSideways;
            _textFormattingMode = textFormattingMode;
            _characterBufferRange = characterRange;
            _properties = properties;
            _emSize = emSize;
            _textItem = textItem;
            _shapeTypeface = shapeTypeface;
            _nullShape = nullShape;
        }

        #endregion

        #region TextRun implementation

        /// <summary>
        /// Character reference
        /// </summary>
        public sealed override CharacterBufferReference CharacterBufferReference
        {
            get
            {
                return _characterBufferRange.CharacterBufferReference;
            }
        }


        /// <summary>
        /// Character length of the run
        /// </summary>
        public sealed override int Length
        {
            get
            {
                return _characterBufferRange.Length;
            }
        }


        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public sealed override TextRunProperties Properties
        {
            get
            {
                return _properties;
            }
        }

        #endregion


        #region TextShapeableSymbols implementation

        /// <summary>
        /// Compute a shaped glyph run object from specified glyph-based info
        /// </summary>
        internal sealed override GlyphRun ComputeShapedGlyphRun(
            Point                    origin,
            char[]                   characterString,
            ushort[]                 clusterMap,
            ushort[]                 glyphIndices,
            IList<double>            glyphAdvances,
            IList<Point>             glyphOffsets,
            bool                     rightToLeft,
            bool                     sideways
            )
        {
            Invariant.Assert(_shapeTypeface != null);
            Invariant.Assert(glyphIndices   != null);
            // Device fonts are only used through the LS non-glyphed code path. Only when a DigitCulture is set
            // will a potential device font be ignored and come through shaping.
            Invariant.Assert(_shapeTypeface.DeviceFont == null  || _textItem.DigitCulture != null);

            bool[] caretStops = null;

            if (    clusterMap != null
                &&  (HasExtendedCharacter || NeedsCaretInfo)
                )
            {
                caretStops = new bool[clusterMap.Length + 1];

                // caret stops at cluster boundaries, the first and the last entries are always set
                caretStops[0] = true;
                caretStops[clusterMap.Length] = true;

                ushort lastGlyph = clusterMap[0];

                for (int i = 1; i < clusterMap.Length; i++)
                {
                    ushort glyph = clusterMap[i];

                    if (glyph != lastGlyph)
                    {
                        caretStops[i] = true;
                        lastGlyph = glyph;
                    }
                }
            }

            return GlyphRun.TryCreate(
                _shapeTypeface.GlyphTypeface,
                (rightToLeft ? 1 : 0),
                sideways,
                _emSize,
                (float)_properties.PixelsPerDip,
                glyphIndices,
                origin,
                glyphAdvances,
                glyphOffsets,
                characterString,
                null,
                clusterMap,
                caretStops,
                XmlLanguage.GetLanguage(CultureMapper.GetSpecificCulture(_properties.CultureInfo).IetfLanguageTag),
                _textFormattingMode
                );
        }

        private GlyphTypeface GetGlyphTypeface(out bool nullFont)
        {
            GlyphTypeface glyphTypeface;

            if (_shapeTypeface == null)
            {
                // We're in the optimized path where the GlyphTypeface depends only
                // on the Typeface, not on the particular input characters.
                Typeface typeface = _properties.Typeface;

                // Get the GlyphTypeface.
                glyphTypeface = typeface.TryGetGlyphTypeface();

                // If Typeface does not specify *any* valid font family, then we use
                // the GlyphTypeface for Arial but only to display missing glyphs.
                nullFont = typeface.NullFont;
            }
            else
            {
                // Font linking has mapped the input to a specific GlyphTypeface.
                glyphTypeface = _shapeTypeface.GlyphTypeface;

                // If font linking could not find *any* physical font family, then we
                // use the GlyphTypeface for Arial but only to display missing glyphs.
                nullFont = _nullShape;
            }

            Invariant.Assert(glyphTypeface != null);
            return glyphTypeface;
        }

        /// <summary>
        /// Compute unshaped glyph run object from the specified character-based info
        /// </summary>
        internal sealed override GlyphRun ComputeUnshapedGlyphRun(
            Point         origin,
            char[]        characterString,
            IList<double> characterAdvances
            )
        {
            bool nullFont;
            GlyphTypeface glyphTypeface = GetGlyphTypeface(out nullFont);
            Invariant.Assert(glyphTypeface != null);

            return glyphTypeface.ComputeUnshapedGlyphRun(
                origin,
                new CharacterBufferRange(
                    characterString,
                    0,  // offsetToFirstChar
                    characterString.Length
                    ),
                characterAdvances,
                _emSize,
                (float)_properties.PixelsPerDip,
                _properties.FontHintingEmSize,
                nullFont,
                CultureMapper.GetSpecificCulture(_properties.CultureInfo),
                (_shapeTypeface == null  ||  _shapeTypeface.DeviceFont == null) ? null : _shapeTypeface.DeviceFont.Name,
                _textFormattingMode
            );
        }


        /// <summary>
        /// Draw glyph run to the drawing surface
        /// </summary>
        internal sealed override void Draw(
            DrawingContext      drawingContext,
            Brush               foregroundBrush,
            GlyphRun            glyphRun
            )
        {
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");

            glyphRun.EmitBackground(drawingContext, _properties.BackgroundBrush);

            drawingContext.DrawGlyphRun(
                foregroundBrush != null ? foregroundBrush : _properties.ForegroundBrush,
                glyphRun
                );
        }

        internal override double EmSize
        {
            get
            {
                return _emSize;
            }
        }

        internal override MS.Internal.Text.TextInterface.ItemProps ItemProps
        {
            get
            {
                return _textItem;
            }
        }

        /// <summary>
        /// Get advance widths of unshaped characters
        /// </summary>
        internal sealed override unsafe void GetAdvanceWidthsUnshaped(
            char*         characterString,
            int           characterLength,
            double        scalingFactor,
            int*          advanceWidthsUnshaped
            )
        {
            if (!IsShapingRequired)
            {
                if (    (_shapeTypeface            != null)
                    &&  (_shapeTypeface.DeviceFont != null))
                {
                    // Use device font to compute advance widths
                    _shapeTypeface.DeviceFont.GetAdvanceWidths(
                        characterString,
                        characterLength,
                        _emSize * scalingFactor,
                        advanceWidthsUnshaped
                    );
                }
                else
                {
                    bool nullFont;
                    GlyphTypeface glyphTypeface = GetGlyphTypeface(out nullFont);
                    Invariant.Assert(glyphTypeface != null);

                    glyphTypeface.GetAdvanceWidthsUnshaped(
                        characterString,
                        characterLength,
                        _emSize,
                        (float)_properties.PixelsPerDip,
                        scalingFactor,
                        advanceWidthsUnshaped,
                        nullFont,
                        _textFormattingMode,
                        _isSideways
                        );
                }
            }
            else
            {
                GlyphTypeface glyphTypeface = _shapeTypeface.GlyphTypeface;

                Invariant.Assert(glyphTypeface != null);
                Invariant.Assert(characterLength > 0);

                CharacterBufferRange newBuffer = new CharacterBufferRange(characterString, characterLength);
                MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics = BufferCache.GetGlyphMetrics(characterLength);

                glyphTypeface.GetGlyphMetricsOptimized(newBuffer,
                                                       _emSize,
                                                       (float)_properties.PixelsPerDip,
                                                       _textFormattingMode,
                                                       _isSideways,
                                                       glyphMetrics
                                                       );

                if (_textFormattingMode == TextFormattingMode.Display &&
                    TextFormatterContext.IsSpecialCharacter(*characterString))
                {
                    // If the run starts with a special character (in
                    // the LineServices sense), we apply display-mode rounding now,
                    // as we won't get another chance.   This assumes that the characters
                    // in a run are either all special or all non-special;  that assumption
                    // is valid in the current LS implementation.
                    double designToEm = _emSize / glyphTypeface.DesignEmHeight;
                    double pixelsPerDip = _properties.PixelsPerDip;

                    for (int i = 0; i < characterLength; i++)
                    {
                        advanceWidthsUnshaped[i] = (int)Math.Round(TextFormatterImp.RoundDipForDisplayMode(glyphMetrics[i].AdvanceWidth * designToEm, pixelsPerDip) * scalingFactor);
                    }
                }
                else
                {
                    // For the normal case, rounding is applied later on when LS
                    // invokes the callback GetGlyphPositions, so that adjustments
                    // due to justification and shaping are taken into account.
                    double designToEm = _emSize * scalingFactor / glyphTypeface.DesignEmHeight;

                    for (int i = 0; i < characterLength; i++)
                    {
                        advanceWidthsUnshaped[i] = (int)Math.Round(glyphMetrics[i].AdvanceWidth * designToEm);
                    }
                }


                BufferCache.ReleaseGlyphMetrics(glyphMetrics);
            }
        }

        /// <summary>
        /// This is needed to decide whether we should pass a max cluster size to LS.
        /// We pass a max cluster size to LS to perform line breaking correctly.
        /// Passing a max cluster size larger than necessary will have impact on perf.
        /// </summary>
        internal sealed override bool NeedsMaxClusterSize
        {
            get
            {
                //  We will need to pass a max cluster size to LS if:
                //  1) the script is not Latin (hence more likely to formed into ligature)
                //  2) or the run contains combining mark or extended characters
                if (!_textItem.IsLatin ||
                    _textItem.HasCombiningMark ||
                    _textItem.HasExtendedCharacter
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Return value indicates whether two runs can shape together
        /// </summary>
        internal sealed override bool CanShapeTogether(
            TextShapeableSymbols   shapeable
            )
        {
            TextShapeableCharacters charShape = shapeable as TextShapeableCharacters;

            if (charShape == null)
                return false;

            return
                    _shapeTypeface.Equals(charShape._shapeTypeface)
                // Extended characters need to be shaped by surrogate shaper. They cannot be shaped together with non-exteneded characters.
                && (_textItem.HasExtendedCharacter) == (charShape._textItem.HasExtendedCharacter)
                && _emSize == charShape._emSize
                && (
                    _properties.CultureInfo == null ?
                        charShape._properties.CultureInfo == null
                      : _properties.CultureInfo.Equals(charShape._properties.CultureInfo)
                    )
                && _nullShape == charShape._nullShape
                && (_textItem.CanShapeTogether(charShape._textItem));
        }


        /// <summary>
        /// Indicate whether run cannot be treated as simple characters because shaping is required.
        ///
        /// The following cases use simple rendering without shaping:
        ///   o  No _shapeTypeface. This happens in very simple rendering cases.
        ///   o  Non-Unicode (i.e. symbol) fonts.
        ///   o  When using a device font.
        ///
        /// Note that the presence of a device font in _shapeTypeface.DeviceFont implies use of
        /// a device font in all cases except where digit substitution applies. This special
        /// case occurs because the cached result per codepoint of TypefaceMap must include the device font
        /// for non-western digits in order to support device font rendering of the non-Western
        /// digit Unicode codepoints. The device font is not used however when the non-Western digits
        /// are displayed as a result of digit substitution from backing store Western digits.
        /// </summary>
        internal sealed override bool IsShapingRequired
        {
            get
            {
                return
                        (_shapeTypeface != null)                 // Can't use shaping without a shape typeface
                    &&  (    (_shapeTypeface.DeviceFont == null) // Can't use shaping when rendering with a device font
                         ||  (_textItem.DigitCulture != null))   //   -- unless substituting digits
                    &&  (!IsSymbol);                             // Can't use shaping for symbol (non-Unicode) fonts
            }
        }


        /// <summary>
        /// A Boolean value indicates whether additional info is required for caret positioning
        /// </summary>
        internal sealed override bool NeedsCaretInfo
        {
            get
            {
                return (_textItem.HasCombiningMark)
                    || (_textItem.NeedsCaretInfo);
            }
        }


        /// <summary>
        /// A Boolean value indicates whether run has extended character
        /// </summary>
        internal sealed override bool HasExtendedCharacter
        {
            get
            {
                return _textItem.HasExtendedCharacter;
            }
        }


        /// <summary>
        /// Run height
        /// </summary>
        internal sealed override double Height
        {
            get
            {
                return _properties.Typeface.LineSpacing(_properties.FontRenderingEmSize, 1, _properties.PixelsPerDip, _textFormattingMode);
            }
        }


        /// <summary>
        /// Distance from top to baseline
        /// </summary>
        internal sealed override double Baseline
        {
            get
            {
                return _properties.Typeface.Baseline(_properties.FontRenderingEmSize, 1, _properties.PixelsPerDip, _textFormattingMode);
            }
        }


        /// <summary>
        /// Distance from baseline to underline position relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal sealed override double UnderlinePosition
        {
            get
            {
                return _properties.Typeface.UnderlinePosition;
            }
        }


        /// <summary>
        /// Underline thickness relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal sealed override double UnderlineThickness
        {
            get
            {
                return _properties.Typeface.UnderlineThickness;
            }
        }


        /// <summary>
        /// Distance from baseline to strike-through position relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal sealed override double StrikethroughPosition
        {
            get
            {
                return _properties.Typeface.StrikethroughPosition;
            }
        }


        /// <summary>
        /// strike-through thickness relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal sealed override double StrikethroughThickness
        {
            get
            {
                return _properties.Typeface.StrikethroughThickness;
            }
        }

        #endregion


        /// <summary>
        /// Whether all characters in this run are non-Unicode character (symbol)
        /// </summary>
        internal bool IsSymbol
        {
            get
            {
                if (_shapeTypeface != null)
                    return _shapeTypeface.GlyphTypeface.Symbol;

                return _properties.Typeface.Symbol;
            }
        }

        internal override GlyphTypeface GlyphTypeFace
        {
            get
            {
                if (_shapeTypeface != null)
                    return _shapeTypeface.GlyphTypeface;

                return _properties.Typeface.TryGetGlyphTypeface();
            }
        }

        /// <summary>
        /// Returns maximum possible cluster size for the run.  Normally, this
        /// is 8 characters, but Indic scripts require this to be 15.
        /// </summary>
        internal const ushort DefaultMaxClusterSize = 8;
        private  const ushort IndicMaxClusterSize = 15;
        internal sealed override ushort MaxClusterSize
        {
            get
            {
                if (_textItem.IsIndic)
                {
                    return IndicMaxClusterSize;
                }

                return DefaultMaxClusterSize;
            }
}
    }
}

