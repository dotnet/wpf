// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Globalization;

using System.Security;
using MS.Internal.Shaping;
using MS.Internal.FontCache;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

using MS.Internal.Text.TextInterface;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Run represented by a plsrun value dispatched to LS during FetchRun
    /// </summary>
    internal sealed class LSRun
    {
        private TextRunInfo             _runInfo;                   // TextRun Info of the text
        private Plsrun                  _type;                      // Plsrun used as run type
        private int                     _offsetToFirstCp;           // dcp from line's cpFirst
        private int                     _textRunLength;             // textrun length
        private CharacterBufferRange    _charBufferRange;           // character buffer range
        private int                     _baselineOffset;            // distance from top to baseline
        private int                     _height;                    // height
        private int                     _baselineMoveOffset;        // run is moved by this offset from baseline
        private int                     _emSize;                    // run ideal EM size
        private TextShapeableSymbols    _shapeable;                 // shapeable run
        private ushort                  _charFlags;                 // character attribute flags
        private byte                    _bidiLevel;                 // resolved bidi level
        private IList<TextEffect>       _textEffects;               // TextEffects that should be applied for this run


        /// <summary>
        /// Construct an lsrun
        /// </summary>
        internal LSRun(
            TextRunInfo             runInfo,
            IList<TextEffect>       textEffects,
            Plsrun                  type,
            int                     offsetToFirstCp,
            int                     textRunLength,
            int                     emSize,
            ushort                  charFlags,
            CharacterBufferRange    charBufferRange,
            TextShapeableSymbols    shapeable,
            double                  realToIdeal,
            byte                    bidiLevel
            ) : 
            this(
                runInfo,
                textEffects,
                type,
                offsetToFirstCp,
                textRunLength,
                emSize,
                charFlags,
                charBufferRange,
                (shapeable != null ? (int)Math.Round(shapeable.Baseline * realToIdeal) : 0),
                (shapeable != null ? (int)Math.Round(shapeable.Height * realToIdeal) : 0),
                shapeable,
                bidiLevel
                )
        {}


        /// <summary>
        /// Construct an lsrun
        /// </summary>
        private LSRun(
            TextRunInfo             runInfo,
            IList<TextEffect>       textEffects,            
            Plsrun                  type,
            int                     offsetToFirstCp,
            int                     textRunLength,
            int                     emSize,
            ushort                  charFlags,
            CharacterBufferRange    charBufferRange,
            int                     baselineOffset,
            int                     height,
            TextShapeableSymbols    shapeable,
            byte                    bidiLevel
            )
        {
            _runInfo = runInfo;
            _type = type;
            _offsetToFirstCp = offsetToFirstCp;
            _textRunLength = textRunLength;
            _emSize = emSize;
            _charFlags = charFlags;
            _charBufferRange = charBufferRange;
            _baselineOffset = baselineOffset;
            _height = height;
            _bidiLevel = bidiLevel;
            _shapeable = shapeable;
            _textEffects = textEffects;
        }


        /// <summary>
        /// Construct an lsrun for a constant control char
        /// </summary>
        internal LSRun(
            Plsrun      type,
            IntPtr      controlChar
            ) :
            this(
                null,   // text run info
                type,
                controlChar,
                0,      // textRunLength
                -1,     // offsetToFirstChar
                0                
                )
        {}


        /// <summary>
        /// Construct an lsrun
        /// </summary>
        /// <param name="runInfo">TextRunInfo</param>
        /// <param name="type">plsrun type</param>
        /// <param name="controlChar">control character</param>
        /// <param name="textRunLength">text run length</param>
        /// <param name="offsetToFirstCp">character offset to the first cp</param>
        /// <param name="bidiLevel">bidi level of this run</param>
        internal LSRun(
            TextRunInfo             runInfo,
            Plsrun                  type,
            IntPtr                  controlChar,
            int                     textRunLength,
            int                     offsetToFirstCp,
            byte                    bidiLevel
            )
        {
            unsafe
            {
                _runInfo = runInfo;
                _type = type;
                _charBufferRange = new CharacterBufferRange((char*)controlChar, 1);
                _textRunLength = textRunLength;
                _offsetToFirstCp = offsetToFirstCp;
                _bidiLevel = bidiLevel;
            }
        }


        internal void Truncate(int newLength)
        {
            _charBufferRange = new CharacterBufferRange(
                _charBufferRange.CharacterBufferReference,
                newLength
                );

            _textRunLength = newLength;
        }


        /// <summary>
        /// A Boolean value indicates whether hit-testing is allowed within the run
        /// </summary>
        internal bool IsHitTestable
        {
            get
            {
                return _type == Plsrun.Text;
            }
        }

        /// <summary>
        /// A Boolean value indicates whether this run contains visible content. 
        /// </summary>
        internal bool IsVisible
        {
            get 
            {
                return (_type == Plsrun.Text || _type == Plsrun.InlineObject); 
            }
        }

        /// <summary>
        /// A Boolean value indicates whether this run is End-Of-Line marker.
        /// </summary>
        internal bool IsNewline
        {
            get 
            {
                return (_type == Plsrun.LineBreak || _type == Plsrun.ParaBreak);
            }
        }

        /// <summary>
        /// A Boolean value indicates whether additional info is required for caret positioning
        /// </summary>
        internal bool NeedsCaretInfo
        {
            get
            {
                return _shapeable != null && _shapeable.NeedsCaretInfo;
            }
        }


        /// <summary>
        /// A Boolean value indicates whether run has extended character
        /// </summary>
        internal bool HasExtendedCharacter
        {
            get
            {
                return _shapeable != null && _shapeable.HasExtendedCharacter;
            }
        }


        /// <summary>
        /// Draw glyphrun
        /// </summary>
        /// <param name="drawingContext">The drawing context to draw into </param>
        /// <param name="foregroundBrush"> 
        /// The foreground brush of the glyphrun. Pass in "null" to draw the 
        /// glyph run with the foreground in TextRunProperties.
        /// </param>
        /// <param name="glyphRun">The GlyphRun to be drawn </param>
        /// <returns>bounding rectangle of drawn glyphrun</returns>
        /// <Remarks>
        /// TextEffect drawing code may use a different foreground brush for the text.
        /// </Remarks>
        internal Rect DrawGlyphRun(
            DrawingContext  drawingContext, 
            Brush           foregroundBrush,
            GlyphRun        glyphRun
            )
        {
            Debug.Assert(_shapeable != null);
            
            Rect inkBoundingBox = glyphRun.ComputeInkBoundingBox();

            if (!inkBoundingBox.IsEmpty)
            {
                // glyph run's ink bounding box is relative to its origin
                inkBoundingBox.X += glyphRun.BaselineOrigin.X;
                inkBoundingBox.Y += glyphRun.BaselineOrigin.Y;
            }

            if (drawingContext != null)
            {
                int pushCount = 0;              // the number of push we do
                try 
                {
                    if (_textEffects != null)
                    {                
                        // we need to push in the same order as they are set
                        for (int i = 0; i < _textEffects.Count; i++)
                        {
                            // get the text effect by its index
                            TextEffect textEffect = _textEffects[i];

                            if (textEffect.Transform != null && textEffect.Transform != Transform.Identity)
                            {
                                drawingContext.PushTransform(textEffect.Transform);
                                pushCount++;
                            }

                            if (textEffect.Clip != null)
                            {
                                drawingContext.PushClip(textEffect.Clip);
                                pushCount++;
                            }

                            if (textEffect.Foreground != null)
                            {
                                // remember the out-most non-null brush
                                // this brush will be used to draw the glyph run
                                foregroundBrush = textEffect.Foreground;
                            }
                        }
                    }

                    _shapeable.Draw(drawingContext, foregroundBrush, glyphRun);                
                }
                finally 
                {
                    for (int i = 0; i < pushCount; i++)
                    {
                        drawingContext.Pop();
                    }
                }
            }

            return inkBoundingBox;
        }


        /// <summary>
        /// Map a UV real coordinate to an XY real coordinate
        /// </summary>
        /// <param name="origin">line drawing origin XY</param>
        /// <param name="vectorToOrigin">vector to line origin UV</param>
        /// <param name="u">real distance in text flow direction</param>
        /// <param name="v">real distance in paragraph flow direction</param>
        /// <param name="line">container line</param>
        internal static Point UVToXY(
            Point                       origin,  
            Point                       vectorToOrigin,
            double                      u,
            double                      v,
            TextMetrics.FullTextLine    line
            )
        {
            Point xy;
            origin.Y += vectorToOrigin.Y;

            if (line.RightToLeft)
            {
                xy = new Point(line.Formatter.IdealToReal(line.ParagraphWidth, line.PixelsPerDip) - vectorToOrigin.X - u + origin.X, v + origin.Y);
            }
            else
            {
                xy = new Point(u + vectorToOrigin.X + origin.X, v + origin.Y);
            }

            return xy;
        }



        /// <summary>
        /// Map a UV ideal coordinate to an XY real coordinate
        /// </summary>
        /// <param name="origin">line drawing origin</param>
        /// <param name="vectorToOrigin">vector to line origin UV</param>
        /// <param name="u">ideal distance in text flow direction</param>
        /// <param name="v">ideal distance in paragraph flow direction</param>
        /// <param name="line">container line</param>
        internal static Point UVToXY(
            Point           origin,        
            Point           vectorToOrigin,
            int             u,                 
            int             v,
            TextMetrics.FullTextLine    line
            )
        {
            Point xy;
            origin.Y += vectorToOrigin.Y;

            if (line.RightToLeft)
            {
                xy = new Point(line.Formatter.IdealToReal(line.ParagraphWidth - u, line.PixelsPerDip) - vectorToOrigin.X + origin.X, line.Formatter.IdealToReal(v, line.PixelsPerDip) + origin.Y);
            }
            else
            {
                xy = new Point(line.Formatter.IdealToReal(u, line.PixelsPerDip) + vectorToOrigin.X + origin.X, line.Formatter.IdealToReal(v, line.PixelsPerDip) + origin.Y);
            }

            return xy;
        }

        /// <summary>
        /// Map a UV ideal coordinate to an XY ideal coordinate
        /// </summary>
        /// <param name="origin">line drawing origin</param>
        /// <param name="vectorToOrigin">vector to line origin UV</param>
        /// <param name="u">ideal distance in text flow direction</param>
        /// <param name="v">ideal distance in paragraph flow direction</param>
        /// <param name="line">container line</param>
        /// <param name="nominalX">ideal X origin</param>
        /// <param name="nominalY">ideal Y origin</param>
        internal static void UVToNominalXY(
            Point origin,
            Point vectorToOrigin,
            int u,
            int v,
            TextMetrics.FullTextLine line,
            out int nominalX,
            out int nominalY
            )
        {
            origin.Y += vectorToOrigin.Y;

            if (line.RightToLeft)
            {
                nominalX = line.ParagraphWidth - u + TextFormatterImp.RealToIdeal(-vectorToOrigin.X + origin.X);
            }
            else
            {
                nominalX = u + TextFormatterImp.RealToIdeal(vectorToOrigin.X + origin.X);
            }

            nominalY = v + TextFormatterImp.RealToIdeal(origin.Y);
        }

        /// <summary>
        /// Create a rectangle of the two specified UV coordinates
        /// </summary>
        /// <param name="origin">line drawing origin</param>
        /// <param name="topLeft">logical top-left point</param>
        /// <param name="bottomRight">logical bottom-right point</param>
        /// <param name="line">container line</param>
        internal static Rect RectUV(
            Point           origin,        
            LSPOINT         topLeft,
            LSPOINT         bottomRight,
            TextMetrics.FullTextLine    line
            )
        {
            int dx = topLeft.x - bottomRight.x;
            if(dx == 1 || dx == -1)
            {
                // in certain situation LS can be off by 1
                bottomRight.x = topLeft.x;
            }

            Rect rect = new Rect(
                new Point(line.Formatter.IdealToReal(topLeft.x, line.PixelsPerDip), line.Formatter.IdealToReal(topLeft.y, line.PixelsPerDip)),
                new Point(line.Formatter.IdealToReal(bottomRight.x, line.PixelsPerDip), line.Formatter.IdealToReal(bottomRight.y, line.PixelsPerDip))
                );

            if(DoubleUtil.AreClose(rect.TopLeft.X, rect.BottomRight.X))
            {
                rect.Width = 0;
            }

            if(DoubleUtil.AreClose(rect.TopLeft.Y, rect.BottomRight.Y))
            {
                rect.Height = 0;
            }

            return rect;
        }

        /// <summary>
        /// Move text run's baseline by the specified value
        /// </summary>
        /// <param name="baselineMoveOffset">offset to be moved away from baseline</param>
        internal void Move(int baselineMoveOffset)
        {
            _baselineMoveOffset += baselineMoveOffset;
        }

        internal byte BidiLevel
        {
            get { return _bidiLevel; }
        }

        internal bool IsSymbol
        {
            get 
            {
                TextShapeableCharacters shapeable = _shapeable as TextShapeableCharacters;
                return shapeable != null && shapeable.IsSymbol;
            }
        }

        internal int OffsetToFirstCp
        {
            get { return _offsetToFirstCp; }
        }

        internal int Length
        {
            get { return _textRunLength; }
        }

        internal TextModifierScope TextModifierScope
        {
            get { return _runInfo.TextModifierScope; }
        }

        internal Plsrun Type
        {
            get { return _type; }
        }

        internal ushort CharacterAttributeFlags
        {
            get { return _charFlags; }
        }

        internal CharacterBuffer CharacterBuffer
        {
            get { return _charBufferRange.CharacterBuffer; }
        }

        internal int StringLength
        {
            get { return _charBufferRange.Length; }
        }

        internal int OffsetToFirstChar
        {
            get { return _charBufferRange.OffsetToFirstChar; }
        }

        internal TextRun TextRun
        {
            get { return _runInfo.TextRun; }
        }

        internal TextShapeableSymbols Shapeable
        {
            get { return _shapeable; }
        }

        internal int BaselineOffset
        {
            get { return _baselineOffset; }
            set { _baselineOffset = value; }
        }

        internal int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        internal int Descent
        {
            get { return Height - BaselineOffset; }
        }

        internal TextRunProperties RunProp
        {
            get 
            {
                return _runInfo.Properties;
            }
        }

        internal CultureInfo TextCulture
        {
            get
            {
                return CultureMapper.GetSpecificCulture(RunProp != null ? RunProp.CultureInfo : null);
            }
        }

        internal int EmSize
        {
            get { return _emSize; }
        }

        internal int BaselineMoveOffset
        {
            get { return _baselineMoveOffset; }
        }
        
        /// <summary>
        /// required set of features that will be added to every feature set
        /// It will be used if nothing is set in typogrpahy porperties
        /// </summary>
        
        private enum CustomOpenTypeFeatures
        {
            AlternativeFractions                     ,
            PetiteCapitalsFromCapitals               ,
            SmallCapitalsFromCapitals                ,
            ContextualAlternates                     ,
            CaseSensitiveForms                       ,
            ContextualLigatures                      ,
            CapitalSpacing                           ,
            ContextualSwash                          ,
            CursivePositioning                       ,
            DiscretionaryLigatures                   ,
            ExpertForms                              ,
            Fractions                                ,
            FullWidth                                ,
            HalfForms                                ,
            HalantForms                              ,
            AlternateHalfWidth                       ,
            HistoricalForms                          ,
            HorizontalKanaAlternates                 ,
            HistoricalLigatures                      ,
            HojoKanjiForms                           ,
            HalfWidth                                ,
            JIS78Forms                               ,
            JIS83Forms                               ,
            JIS90Forms                               ,
            JIS04Forms                               ,
            Kerning                                  ,
            StandardLigatures                        ,
            LiningFigures                            ,
            MathematicalGreek                        ,
            AlternateAnnotationForms                 ,
            NLCKanjiForms                            ,
            OldStyleFigures                          ,
            Ordinals                                 ,
            ProportionalAlternateWidth               ,
            PetiteCapitals                           ,
            ProportionalFigures                      ,
            ProportionalWidths                       ,
            QuarterWidths                            ,
            RubyNotationForms                        ,
            StylisticAlternates                      ,
            ScientificInferiors                      ,
            SmallCapitals                            ,
            SimplifiedForms                          ,
            StylisticSet1                            ,
            StylisticSet2                            ,
            StylisticSet3                            ,
            StylisticSet4                            ,
            StylisticSet5                            ,
            StylisticSet6                            ,
            StylisticSet7                            ,
            StylisticSet8                            ,
            StylisticSet9                            ,
            StylisticSet10                           ,
            StylisticSet11                           ,
            StylisticSet12                           ,
            StylisticSet13                           ,
            StylisticSet14                           ,
            StylisticSet15                           ,
            StylisticSet16                           ,
            StylisticSet17                           ,
            StylisticSet18                           ,
            StylisticSet19                           ,
            StylisticSet20                           ,
            Subscript                                ,
            Superscript                              ,
            Swash                                    ,
            Titling                                  ,
            TraditionalNameForms                     ,
            TabularFigures                           ,
            TraditionalForms                         ,
            ThirdWidths                              ,
            Unicase                                  ,
            SlashedZero                              ,
            Count
        }               

        private const ushort FeatureNotEnabled = 0xffff;

        private static DWriteFontFeature[] CreateDWriteFontFeatures(TextRunTypographyProperties textRunTypographyProperties)
        {
            if (textRunTypographyProperties != null)
            {
                if (textRunTypographyProperties.CachedFeatureSet != null)
                {
                    return textRunTypographyProperties.CachedFeatureSet;
                }
                else
                {
                    List<DWriteFontFeature> fontFeatures = new List<DWriteFontFeature>((int)CustomOpenTypeFeatures.Count);

                    if (textRunTypographyProperties.CapitalSpacing)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.CapitalSpacing, 1));
                    }
                    if (textRunTypographyProperties.CaseSensitiveForms)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.CaseSensitiveForms, 1));
                    }
                    if (textRunTypographyProperties.ContextualAlternates)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ContextualAlternates, 1));
                    }
                    if (textRunTypographyProperties.ContextualLigatures)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ContextualLigatures, 1));
                    }
                    if (textRunTypographyProperties.DiscretionaryLigatures)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.DiscretionaryLigatures, 1));
                    }
                    if (textRunTypographyProperties.HistoricalForms)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.HistoricalForms, 1));
                    }
                    if (textRunTypographyProperties.HistoricalLigatures)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.HistoricalLigatures, 1));
                    }
                    if (textRunTypographyProperties.Kerning)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Kerning, 1));
                    }
                    if (textRunTypographyProperties.MathematicalGreek)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.MathematicalGreek, 1));
                    }
                    if (textRunTypographyProperties.SlashedZero)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.SlashedZero, 1));
                    }
                    if (textRunTypographyProperties.StandardLigatures)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StandardLigatures, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet1)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet1, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet10)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet10, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet11)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet11, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet12)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet12, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet13)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet13, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet14)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet14, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet15)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet15, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet16)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet16, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet17)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet17, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet18)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet18, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet19)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet19, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet2)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet2, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet20)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet20, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet3)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet3, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet4)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet4, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet5)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet5, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet6)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet6, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet7)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet7, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet8)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet8, 1));
                    }
                    if (textRunTypographyProperties.StylisticSet9)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticSet9, 1));
                    }
                    if (textRunTypographyProperties.EastAsianExpertForms)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ExpertForms, 1));
                    }

                    if (textRunTypographyProperties.AnnotationAlternates > 0)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.AlternateAnnotationForms, checked((uint)textRunTypographyProperties.AnnotationAlternates)));
                    }
                    if (textRunTypographyProperties.ContextualSwashes > 0)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ContextualSwash, checked((uint)textRunTypographyProperties.ContextualSwashes)));
                    }
                    if (textRunTypographyProperties.StylisticAlternates > 0)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.StylisticAlternates, checked((uint)textRunTypographyProperties.StylisticAlternates)));
                    }
                    if (textRunTypographyProperties.StandardSwashes > 0)
                    {
                        fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Swash, checked((uint)textRunTypographyProperties.StandardSwashes)));
                    }

                    switch (textRunTypographyProperties.Capitals)
                    {
                        case FontCapitals.AllPetiteCaps: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.PetiteCapitals, 1));
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.PetiteCapitalsFromCapitals, 1));
                            break;
                        case FontCapitals.AllSmallCaps: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.SmallCapitals, 1));
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.SmallCapitalsFromCapitals, 1));
                            break;
                        case FontCapitals.PetiteCaps: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.PetiteCapitals, 1));
                            break;
                        case FontCapitals.SmallCaps: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.SmallCapitals, 1));
                            break;
                        case FontCapitals.Titling: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Titling, 1));
                            break;
                        case FontCapitals.Unicase: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Unicase, 1));
                            break;
                    }

                    switch (textRunTypographyProperties.EastAsianLanguage)
                    {
                        case FontEastAsianLanguage.Simplified: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.SimplifiedForms, 1));
                            break;
                        case FontEastAsianLanguage.Traditional: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.TraditionalForms, 1));
                            break;
                        case FontEastAsianLanguage.TraditionalNames: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.TraditionalNameForms, 1));
                            break;
                        case FontEastAsianLanguage.NlcKanji: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.NLCKanjiForms, 1));
                            break;
                        case FontEastAsianLanguage.HojoKanji: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.HojoKanjiForms, 1));
                            break;
                        case FontEastAsianLanguage.Jis78: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.JIS78Forms, 1));
                            break;
                        case FontEastAsianLanguage.Jis83: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.JIS83Forms, 1));
                            break;
                        case FontEastAsianLanguage.Jis90: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.JIS90Forms, 1));
                            break;
                        case FontEastAsianLanguage.Jis04: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.JIS04Forms, 1));
                            break;
                    }

                    switch (textRunTypographyProperties.Fraction)
                    {
                        case FontFraction.Stacked: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.AlternativeFractions, 1));
                            break;
                        case FontFraction.Slashed: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Fractions, 1));
                            break;
                    }

                    switch (textRunTypographyProperties.NumeralAlignment)
                    {
                        case FontNumeralAlignment.Proportional: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ProportionalFigures, 1));
                            break;
                        case FontNumeralAlignment.Tabular: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.TabularFigures, 1));
                            break;
                    }

                    switch (textRunTypographyProperties.NumeralStyle)
                    {
                        case FontNumeralStyle.Lining: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.LiningFigures, 1));
                            break;
                        case FontNumeralStyle.OldStyle: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.OldStyleFigures, 1));
                            break;
                    }

                    switch (textRunTypographyProperties.Variants)
                    {
                        case FontVariants.Inferior: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ScientificInferiors, 1));
                            break;
                        case FontVariants.Ordinal: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Ordinals, 1));
                            break;
                        case FontVariants.Ruby: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.RubyNotationForms, 1));
                            break;
                        case FontVariants.Subscript: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Subscript, 1));
                            break;
                        case FontVariants.Superscript: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.Superscript, 1));
                            break;
                    }
                    
                    switch (textRunTypographyProperties.EastAsianWidths)
                    {
                        case FontEastAsianWidths.Proportional:
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ProportionalWidths, 1));
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ProportionalAlternateWidth, 1));
                            break;
                        case FontEastAsianWidths.Full: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.FullWidth, 1));
                            break;
                        case FontEastAsianWidths.Half:
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.HalfWidth, 1));
                            fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.AlternateHalfWidth, 1));
                            break;
                        case FontEastAsianWidths.Third: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.ThirdWidths, 1));
                            break;
                        case FontEastAsianWidths.Quarter: fontFeatures.Add(new DWriteFontFeature(Text.TextInterface.DWriteFontFeatureTag.QuarterWidths, 1));
                            break;
                    }

                    textRunTypographyProperties.CachedFeatureSet = fontFeatures.ToArray();
                    return textRunTypographyProperties.CachedFeatureSet;
                }

            }
            return null;
        }

        /// <summary>
        /// Compile feature set from the linked list of LSRuns.
        /// TypographyProperties should be either all null or all not-null.
        /// First is used for internal purposes, also can be used by simple clients.
        /// </summary>
        internal static unsafe void CompileFeatureSet(
            LSRun[]                   lsruns,
            int*                      pcchRuns,
            uint                      totalLength,
            out DWriteFontFeature[][] fontFeatures,
            out uint[]                fontFeatureRanges
            )
        {           
            Debug.Assert(lsruns != null && lsruns.Length > 0 && lsruns[0] != null);

            //
            //  Quick check for null properties
            //  Run properties should be all null or all not null
            //   
            if (lsruns[0].RunProp.TypographyProperties == null)
            {
                for (int i = 1; i < lsruns.Length; i++)
                {
                    if (lsruns[i].RunProp.TypographyProperties != null)
                    {
                        throw new ArgumentException(SR.Get(SRID.CompileFeatureSet_InvalidTypographyProperties));
                    }
                }

                fontFeatures      = null;
                fontFeatureRanges = null;
                return;
            }
            //End of quick check. We will process custom features now.
                

            fontFeatures      = new DWriteFontFeature[lsruns.Length][];
            fontFeatureRanges = new uint[lsruns.Length];

            for (int i = 0; i < lsruns.Length; i++)
            {
                TextRunTypographyProperties properties = lsruns[i].RunProp.TypographyProperties;
                fontFeatures[i] = CreateDWriteFontFeatures(properties);
                fontFeatureRanges[i] = checked((uint)pcchRuns[i]);
            }            
        }

        /// <summary>
        /// Compile feature set from the linked list of LSRuns.
        /// TypographyProperties should be either all null or all not-null.
        /// First is used for internal purposes, also can be used by simple clients.
        /// </summary>
        internal static void CompileFeatureSet(    
            TextRunTypographyProperties textRunTypographyProperties,
            uint totalLength,
            out DWriteFontFeature[][] fontFeatures,
            out uint[] fontFeatureRanges
            )
        {
            //
            //  Quick check for null properties
            //  Run properties should be all null or all not null
            //   
            if (textRunTypographyProperties == null)
            {
                fontFeatures = null;
                fontFeatureRanges = null;
            }
            else
            { 
                // End of quick check. We will process custom features now.

                fontFeatures = new DWriteFontFeature[1][];
                fontFeatureRanges = new uint[1];
                fontFeatures[0] = CreateDWriteFontFeatures(textRunTypographyProperties);
                fontFeatureRanges[0] = totalLength;
            }
        }        
    }
}
