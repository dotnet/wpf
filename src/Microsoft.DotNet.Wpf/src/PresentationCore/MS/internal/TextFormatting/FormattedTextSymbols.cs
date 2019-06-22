// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 
//  Formatting a single style, single reading direction text symbols
//
//

using System;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.Text.TextInterface;
using MS.Internal.Shaping;
using System.Globalization;
using MS.Internal.FontCache;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Formatted form of TextSymbols
    /// </summary>
    internal sealed class FormattedTextSymbols
    {
        private Glyphs[]    _glyphs;
        private bool        _rightToLeft;
        private TextFormattingMode _textFormattingMode;
        private bool _isSideways;

        /// <summary>
        /// Construct a formatted run
        /// </summary>
        public FormattedTextSymbols(
            GlyphingCache glyphingCache,
            TextRun       textSymbols,
            bool          rightToLeft,
            double        scalingFactor,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            bool isSideways
            )
        {
            _textFormattingMode = textFormattingMode;
            _isSideways = isSideways;
            ITextSymbols  symbols = textSymbols as ITextSymbols;

            Debug.Assert(symbols != null);

            // break down a single text run into pieces
            IList<TextShapeableSymbols> shapeables = symbols.GetTextShapeableSymbols(
                glyphingCache,
                textSymbols.CharacterBufferReference,
                textSymbols.Length,
                rightToLeft,  // This is a bool indicating the RTL
                              // based on the bidi level of text (if applicable). 
                              // For FormattedTextSymbols it is equal to paragraph flow direction.

                rightToLeft,  // This is the flow direction of the paragraph as 
                              // specified by the user. DWrite needs the paragraph
                              // flow direction of the paragraph 
                              // while WPF algorithms need the RTL of the text based on 
                              // Bidi if possible.

                null, // cultureInfo
                null,  // textModifierScope
                _textFormattingMode,
                _isSideways
                );

            Debug.Assert(shapeables != null && shapeables.Count > 0);

            _rightToLeft = rightToLeft;
            _glyphs = new Glyphs[shapeables.Count];

            CharacterBuffer charBuffer = textSymbols.CharacterBufferReference.CharacterBuffer;
            int offsetToFirstChar = textSymbols.CharacterBufferReference.OffsetToFirstChar;

            int i = 0;
            int ich = 0;

            while (i < shapeables.Count)
            {
                TextShapeableSymbols current = shapeables[i] as TextShapeableSymbols;
                Debug.Assert(current != null);

                int cch = current.Length;
                int j;

                // make a separate character buffer for glyphrun persistence
                char[] charArray = new char[cch];
                for (j = 0; j < cch; j++)
                    charArray[j] = charBuffer[offsetToFirstChar + ich + j];

                if (current.IsShapingRequired)
                {
                    ushort[] clusterMap;
                    ushort[] glyphIndices;
                    int[] glyphAdvances;
                    GlyphOffset[] glyphOffsets;


                    // Note that we dont check for the chance of having multiple
                    // shapeables shaped together here since we're dealing with
                    // single-style text. There is virtually no chance to require
                    // for adjacent runs to shape together. We rely on TextSymbols
                    // to reduce duplication of the itemized shapeables for performance.
                    unsafe
                    {
                        fixed (char* fixedCharArray = &charArray[0])
                        {
                            MS.Internal.Text.TextInterface.TextAnalyzer textAnalyzer = MS.Internal.FontCache.DWriteFactory.Instance.CreateTextAnalyzer();

                            GlyphTypeface glyphTypeface = current.GlyphTypeFace;
                            DWriteFontFeature[][] fontFeatures;
                            uint[] fontFeatureRanges;
                            uint unsignedCch = checked((uint)cch);
                            LSRun.CompileFeatureSet(current.Properties.TypographyProperties, unsignedCch, out fontFeatures, out fontFeatureRanges);
                       

                            textAnalyzer.GetGlyphsAndTheirPlacements(
                                fixedCharArray,
                                unsignedCch,
                                glyphTypeface.FontDWrite,
                                glyphTypeface.BlankGlyphIndex,
                                false,   // no sideway support yet
                                         /************************************************************************************************/
                                         // Should we break down the runs to know whats the Bidi for every range of characters?
                                rightToLeft,
                                current.Properties.CultureInfo,
                                /************************************************************************************************/
                                fontFeatures,
                                fontFeatureRanges,
                                current.Properties.FontRenderingEmSize,
                                scalingFactor,
                                pixelsPerDip,
                                _textFormattingMode,
                                current.ItemProps,
                                out clusterMap,
                                out glyphIndices,
                                out glyphAdvances,
                                out glyphOffsets
                                );
                        }
                        _glyphs[i] = new Glyphs(
                           current,
                           charArray,
                           glyphAdvances,
                           clusterMap,
                           glyphIndices,
                           glyphOffsets,
                           scalingFactor
                           );
                    }
}
                else
                {
                    // shaping not required, 
                    // bypass glyphing process altogether
                    int[] nominalAdvances = new int[charArray.Length];
                    
                    unsafe
                    {
                        fixed (char* fixedCharArray = &charArray[0])
                        fixed (int* fixedNominalAdvances = &nominalAdvances[0])
                        {
                            current.GetAdvanceWidthsUnshaped(
                                fixedCharArray,
                                cch,
                                scalingFactor, // format resolution specified per em,
                                fixedNominalAdvances
                                );
                        }
                    }

                    _glyphs[i] = new Glyphs(
                        current,
                        charArray,
                        nominalAdvances,
                        scalingFactor
                        );
                }

                i++;
                ich += cch;
            }
        }


        /// <summary>
        /// Total formatted width
        /// </summary>
        public double Width
        {
            get 
            {
                Debug.Assert(_glyphs != null);

                double width = 0;
                foreach (Glyphs glyphs in _glyphs)
                {
                    width += glyphs.Width;
                }
                return width;
            }
        }


        /// <summary>
        /// Draw all formatted glyphruns
        /// </summary>
        /// <returns>drawing bounding box</returns>
        public Rect Draw(
            DrawingContext drawingContext,
            Point          currentOrigin
            )
        {
            Rect inkBoundingBox = Rect.Empty;

            Debug.Assert(_glyphs != null);

            foreach (Glyphs glyphs in _glyphs)
            {
                GlyphRun glyphRun = glyphs.CreateGlyphRun(currentOrigin, _rightToLeft);
                Rect boundingBox;

                if (glyphRun != null)
                {
                    boundingBox = glyphRun.ComputeInkBoundingBox();                    

                    if (drawingContext != null)
                    {
                        // Emit glyph run background. 
                        glyphRun.EmitBackground(drawingContext, glyphs.BackgroundBrush);

                        drawingContext.PushGuidelineY1(currentOrigin.Y);
                        try 
                        {
                            drawingContext.DrawGlyphRun(glyphs.ForegroundBrush, glyphRun);
                        }
                        finally 
                        {
                            drawingContext.Pop();
                        }
                    }
                }
                else
                {
                    boundingBox = Rect.Empty;
                }

                if (!boundingBox.IsEmpty)
                {
                    // glyph run's ink bounding box is relative to its origin
                    boundingBox.X += glyphRun.BaselineOrigin.X;
                    boundingBox.Y += glyphRun.BaselineOrigin.Y;
                }

                // accumulate overall ink bounding box
                inkBoundingBox.Union(boundingBox);

                if (_rightToLeft)
                {
                    currentOrigin.X -= glyphs.Width;
                }
                else
                {
                    currentOrigin.X += glyphs.Width;
                }
            }

            return inkBoundingBox;
        }


        /// <summary>
        /// All glyph properties used during GlyphRun construction
        /// </summary>
        /// <remarks>
        /// We should be able to get rid off this type and just store GlyphRuns
        /// once GlyphRun gets refactor'd so that it contains no drawing time 
        /// positioning data inside.
        /// </remarks>
        private sealed class Glyphs
        {
            private TextShapeableSymbols     _shapeable;
            private char[]                   _charArray;
            private ushort[]                 _clusterMap;
            private ushort[]                 _glyphIndices;
            private double[]                 _glyphAdvances;
            private IList<Point>             _glyphOffsets;
            private double                   _width;


            /// <summary>
            /// Construct a nominal description of glyph data
            /// </summary>
            internal Glyphs(
                TextShapeableSymbols    shapeable,
                char[]                  charArray,
                int[]                  nominalAdvances,
                double                  scalingFactor
                ) :
                this(
                    shapeable,
                    charArray,
                    nominalAdvances,
                    null,   // clusterMap
                    null,   // glyphIndices                
                    null,   // glyphOffsets
                    scalingFactor
                    )
            {}


            /// <summary>
            /// Construct a full description of glyph data
            /// </summary>
            internal Glyphs(
                TextShapeableSymbols     shapeable,
                char[]                   charArray,
                int[]                   glyphAdvances,
                ushort[]                 clusterMap,
                ushort[]                 glyphIndices,
                GlyphOffset[]            glyphOffsets,
                double                   scalingFactor
                )
            {
                _shapeable = shapeable;
                _charArray = charArray;

                // create double array for glyph run creation, because Shaping is all done in 
                // ideal units. FormattedTextSymbol is used to draw text collapsing symbols 
                // which usually contains very few glyphs. Using double[] and Point[] directly
                // is more efficient. 
                _glyphAdvances = new double[glyphAdvances.Length];

                double ToReal = 1.0 / scalingFactor;
                
                for (int i = 0; i < glyphAdvances.Length; i++)
                {
                    _glyphAdvances[i] = glyphAdvances[i] * ToReal;
                    _width += _glyphAdvances[i];                
}

                if (glyphIndices != null)
                {
                    _clusterMap = clusterMap;

                    if (glyphOffsets != null)
                    {
                        _glyphOffsets  = new PartialArray<Point>(new Point[glyphOffsets.Length]);                    
                    
                        for (int i = 0; i < glyphOffsets.Length; i++)
                        {
                            _glyphOffsets[i] = new Point(
                                glyphOffsets[i].du * ToReal,
                                glyphOffsets[i].dv * ToReal
                                );
                        }
                    }

                    Debug.Assert(glyphAdvances.Length <= glyphIndices.Length);

                    if (glyphAdvances.Length != glyphIndices.Length)
                    {
                        _glyphIndices = new ushort[glyphAdvances.Length];

                        for (int i = 0; i < glyphAdvances.Length; i++)
                        {
                            _glyphIndices[i] = glyphIndices[i];
                        }
                    }
                    else
                    {
                        _glyphIndices = glyphIndices;
                    }
                }
            }


            /// <summary>
            /// Total formatted width
            /// </summary>
            public double Width
            {
                get { return _width; }
            }


            /// <summary>
            /// Construct a GlyphRun object given the specified drawing origin
            /// </summary>
            internal GlyphRun CreateGlyphRun(
                Point       currentOrigin,
                bool        rightToLeft
                )
            {
                if (!_shapeable.IsShapingRequired)
                {
                    return _shapeable.ComputeUnshapedGlyphRun(
                        currentOrigin,
                        _charArray,
                        _glyphAdvances
                        );
                }

                return _shapeable.ComputeShapedGlyphRun(
                    currentOrigin,
                    _charArray,
                    _clusterMap,
                    _glyphIndices,
                    _glyphAdvances,
                    _glyphOffsets,
                    rightToLeft,
                    false   // sideways not yet supported
                    );
            }

            public Brush ForegroundBrush
            {
                get { return _shapeable.Properties.ForegroundBrush; }
            }

            public Brush BackgroundBrush
            {
                get { return _shapeable.Properties.BackgroundBrush; }
            }
        }
    }
}
