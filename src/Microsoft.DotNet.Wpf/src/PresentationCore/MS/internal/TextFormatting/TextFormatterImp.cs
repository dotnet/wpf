// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Text formatter implementation
//
//


using System;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.Shaping;
using MS.Internal.Text.TextInterface;
using MS.Internal.FontCache;

#if !OPTIMALBREAK_API
using MS.Internal.PresentationCore;
#endif


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Implementation of TextFormatter
    /// </summary>
    internal sealed class TextFormatterImp : TextFormatter
    {
        private FrugalStructList<TextFormatterContext>  _contextList;               // LS context free list
        private bool                                    _multipleContextProhibited; // prohibit multiple contexts within the same formatter
        private GlyphingCache                           _glyphingCache;             // Glyphing cache for font linking process
        private TextFormattingMode                      _textFormattingMode;
        private TextAnalyzer                            _textAnalyzer;              // TextAnalyzer used for shaping process

        private const int MaxGlyphingCacheCapacity = 16;

        /// <summary>
        /// Construct an instance of TextFormatter implementation
        /// </summary>
        internal TextFormatterImp(TextFormattingMode textFormattingMode)
            : this(null, textFormattingMode)
        { }

        /// <summary>
        /// Construct an instance of TextFormatter implementation
        /// </summary>
        internal TextFormatterImp() : this(null, TextFormattingMode.Ideal)
        {}

        /// <summary>
        /// Construct an instance of TextFormatter implementation with the specified context
        /// </summary>
        /// <param name="soleContext"></param>
        /// <remarks>
        /// TextFormatter created via this special ctor takes a specified context and uses it as the only known
        /// context within its entire lifetime. It prohibits reentering of TextFormatter during formatting as only
        /// one context is allowed. This restriction is critical to the optimal break algorithm supported by the current
        /// version of PTLS.
        /// </remarks>
        internal TextFormatterImp(TextFormatterContext soleContext, TextFormattingMode textFormattingMode)
        {
            _textFormattingMode = textFormattingMode;

            if (soleContext != null)
                _contextList.Add(soleContext);

            _multipleContextProhibited = (_contextList.Count != 0);
        }


        /// <summary>
        /// Finalizing text formatter
        /// </summary>
        ~TextFormatterImp()
        {
            CleanupInternal();
        }


        /// <summary>
        /// Release all unmanaged LS contexts
        /// </summary>
        public override void Dispose()
        {
            CleanupInternal();
            base.Dispose();
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Release all unmanaged LS contexts
        /// </summary>
        private void CleanupInternal()
        {
            for (int i = 0; i < _contextList.Count; i++)
            {
                _contextList[i].Destroy();
            }
            _contextList.Clear();
        }


        /// <summary>
        /// Client to format a text line that fills a paragraph in the document.
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">LineBreak property of the previous text line, or null if this is the first line in the paragraph</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
        public override TextLine FormatLine(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak
            )
        {
            return FormatLineInternal(
                textSource,
                firstCharIndex,
                0,   // lineLength
                paragraphWidth,
                paragraphProperties,
                previousLineBreak,
                new TextRunCache()  // local cache, only live within this call
                );
        }



        /// <summary>
        /// Client to format a text line that fills a paragraph in the document.
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">LineBreak property of the previous text line, or null if this is the first line in the paragraph</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
        public override TextLine FormatLine(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            )
        {
            return FormatLineInternal(
                textSource,
                firstCharIndex,
                0,   // lineLength
                paragraphWidth,
                paragraphProperties,
                previousLineBreak,
                textRunCache
                );
        }



        /// <summary>
        /// Client to reconstruct a previously formatted text line
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="lineLength">character length of the line</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">LineBreak property of the previous text line, or null if this is the first line in the paragraph</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
#if OPTIMALBREAK_API
        public override TextLine RecreateLine(
#else
        internal override TextLine RecreateLine(
#endif
            TextSource                  textSource,
            int                         firstCharIndex,
            int                         lineLength,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            )
        {
            return FormatLineInternal(
                textSource,
                firstCharIndex,
                lineLength,
                paragraphWidth,
                paragraphProperties,
                previousLineBreak,
                textRunCache
                );
        }



        /// <summary>
        /// Format and produce a text line either with or without previously known
        /// line break point.
        /// </summary>
        private TextLine FormatLineInternal(
            TextSource                  textSource,
            int                         firstCharIndex,
            int                         lineLength,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            )
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordText, EventTrace.Level.Verbose, EventTrace.Event.WClientStringBegin, "TextFormatterImp.FormatLineInternal Start");

            // prepare formatting settings
            FormatSettings settings = PrepareFormatSettings(
                textSource,
                firstCharIndex,
                paragraphWidth,
                paragraphProperties,
                previousLineBreak,
                textRunCache,
                (lineLength != 0),  // Do optimal break if break is given
                true,    // isSingleLineFormatting
                _textFormattingMode
                );

            TextLine textLine = null;

            if (    !settings.Pap.AlwaysCollapsible
                &&  previousLineBreak == null
                &&  lineLength <= 0
                )
            {
                // simple text line.
                textLine = SimpleTextLine.Create(
                    settings,
                    firstCharIndex,
                    RealToIdealFloor(paragraphWidth),
                    textSource.PixelsPerDip
                    ) as TextLine;
            }

            if (textLine == null)
            {
                // content is complex, creating complex line
                textLine = new TextMetrics.FullTextLine(
                    settings,
                    firstCharIndex,
                    lineLength,
                    RealToIdealFloor(paragraphWidth),
                    LineFlags.None
                    ) as TextLine;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordText, EventTrace.Level.Verbose, EventTrace.Event.WClientStringEnd, "TextFormatterImp.FormatLineInternal End");

            return textLine;
        }



        /// <summary>
        /// Client to ask for the possible smallest and largest paragraph width that can fully contain the passing text content
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <returns>min max paragraph width</returns>
        public override MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource                  textSource,
            int                         firstCharIndex,
            TextParagraphProperties     paragraphProperties
            )
        {
            return FormatMinMaxParagraphWidth(
                textSource,
                firstCharIndex,
                paragraphProperties,
                new TextRunCache()  // local cache, only live within this call
                );
        }



        /// <summary>
        /// Client to ask for the possible smallest and largest paragraph width that can fully contain the passing text content
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>min max paragraph width</returns>
        public override MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource                  textSource,
            int                         firstCharIndex,
            TextParagraphProperties     paragraphProperties,
            TextRunCache                textRunCache
            )
        {
            // prepare formatting settings
            FormatSettings settings = PrepareFormatSettings(
                textSource,
                firstCharIndex,
                0,      // infinite paragraphWidth
                paragraphProperties,
                null,   // always format the whole paragraph - no previousLineBreak
                textRunCache,
                false,  // optimalBreak
                true,   // isSingleLineFormatting
                _textFormattingMode
                );

            // create specialized line specifically for min/max calculation
            TextMetrics.FullTextLine line = new TextMetrics.FullTextLine(
                settings,
                firstCharIndex,
                0,  // lineLength
                0,  // paragraph width has no significant meaning in min/max calculation
                (LineFlags.KeepState | LineFlags.MinMax)
                );

            // line width in this case is the width of a line when the entire paragraph is laid out
            // as a single long line.
            MinMaxParagraphWidth minMax = new MinMaxParagraphWidth(line.MinWidth, line.Width);
            line.Dispose();
            return minMax;
        }

        internal TextFormattingMode TextFormattingMode
        {
            get
            {
                return _textFormattingMode;
            }
        }

        /// <summary>
        /// Client to cache information about a paragraph to be used during optimal paragraph line formatting
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">text formatting state at the point where the previous line in the paragraph
        /// was broken by the text formatting process, as specified by the TextLine.LineBreak property for the previous
        /// line; this parameter can be null, and will always be null for the first line in a paragraph.</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
#if OPTIMALBREAK_API
        public override TextParagraphCache CreateParagraphCache(
#else
        internal override TextParagraphCache CreateParagraphCache(
#endif
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            )
        {
            // prepare formatting settings
            FormatSettings settings = PrepareFormatSettings(
                textSource,
                firstCharIndex,
                paragraphWidth,
                paragraphProperties,
                previousLineBreak,
                textRunCache,
                true,   // optimalBreak
                false,  // !isSingleLineFormatting
                _textFormattingMode
                );

            //
            // Optimal paragraph formatting session specific check
            //
            if (!settings.Pap.Wrap && settings.Pap.OptimalBreak)
            {
                // Optimal paragraph must wrap.
                throw new ArgumentException(SR.Get(SRID.OptimalParagraphMustWrap));
            }

            // create paragraph content cache object
            return new TextParagraphCache(
                settings,
                firstCharIndex,
                RealToIdeal(paragraphWidth)
                );
        }



        /// <summary>
        /// Validate all the relevant text formatting initial settings and package them
        /// </summary>
        private FormatSettings PrepareFormatSettings(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache,
            bool                        useOptimalBreak,
            bool                        isSingleLineFormatting,
            TextFormattingMode              textFormattingMode
            )
        {
            VerifyTextFormattingArguments(
                textSource,
                firstCharIndex,
                paragraphWidth,
                paragraphProperties,
                textRunCache
                );

            if (textRunCache.Imp == null)
            {
                // No run cache object available, create one
                textRunCache.Imp = new TextRunCacheImp();
            }

            // initialize formatting settings
            return new FormatSettings(
                this,
                textSource,
                textRunCache.Imp,
                new ParaProp(this, paragraphProperties, useOptimalBreak),
                previousLineBreak,
                isSingleLineFormatting,
                textFormattingMode,
                false
                );
        }



        /// <summary>
        /// Verify all text formatting arguments
        /// </summary>
        private void VerifyTextFormattingArguments(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextRunCache                textRunCache
            )
        {
            if (textSource == null)
                throw new ArgumentNullException("textSource");

            if (textRunCache == null)
                throw new ArgumentNullException("textRunCache");

            if (paragraphProperties == null)
                throw new ArgumentNullException("paragraphProperties");

            if (paragraphProperties.DefaultTextRunProperties == null)
                throw new ArgumentNullException("paragraphProperties.DefaultTextRunProperties");

            if (paragraphProperties.DefaultTextRunProperties.Typeface == null)
                throw new ArgumentNullException("paragraphProperties.DefaultTextRunProperties.Typeface");

            if (DoubleUtil.IsNaN(paragraphWidth))
                throw new ArgumentOutOfRangeException("paragraphWidth", SR.Get(SRID.ParameterValueCannotBeNaN));

            if (double.IsInfinity(paragraphWidth))
                throw new ArgumentOutOfRangeException("paragraphWidth", SR.Get(SRID.ParameterValueCannotBeInfinity));

            if (    paragraphWidth < 0
                || paragraphWidth > Constants.RealInfiniteWidth)
            {
                throw new ArgumentOutOfRangeException("paragraphWidth", SR.Get(SRID.ParameterMustBeBetween, 0, Constants.RealInfiniteWidth));
            }

            double realMaxFontRenderingEmSize = Constants.RealInfiniteWidth / Constants.GreatestMutiplierOfEm;

            if (    paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize < 0
                ||  paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize > realMaxFontRenderingEmSize)
            {
                throw new ArgumentOutOfRangeException("paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize", SR.Get(SRID.ParameterMustBeBetween, 0, realMaxFontRenderingEmSize));
            }

            if (paragraphProperties.Indent > Constants.RealInfiniteWidth)
                throw new ArgumentOutOfRangeException("paragraphProperties.Indent", SR.Get(SRID.ParameterCannotBeGreaterThan, Constants.RealInfiniteWidth));

            if (paragraphProperties.LineHeight > Constants.RealInfiniteWidth)
                throw new ArgumentOutOfRangeException("paragraphProperties.LineHeight", SR.Get(SRID.ParameterCannotBeGreaterThan, Constants.RealInfiniteWidth));

            if (   paragraphProperties.DefaultIncrementalTab < 0
                || paragraphProperties.DefaultIncrementalTab > Constants.RealInfiniteWidth)
            {
                throw new ArgumentOutOfRangeException("paragraphProperties.DefaultIncrementalTab", SR.Get(SRID.ParameterMustBeBetween, 0, Constants.RealInfiniteWidth));
            }
        }


        /// <summary>
        /// Validate the input character hit
        /// </summary>
        internal static void VerifyCaretCharacterHit(
            CharacterHit    characterHit,
            int             cpFirst,
            int             cchLength
            )
        {
            if (    characterHit.FirstCharacterIndex < cpFirst
                ||  characterHit.FirstCharacterIndex > cpFirst + cchLength)
            {
                throw new ArgumentOutOfRangeException("cpFirst", SR.Get(SRID.ParameterMustBeBetween, cpFirst, cpFirst + cchLength));
            }

            if (characterHit.TrailingLength < 0)
            {
                throw new ArgumentOutOfRangeException("cchLength", SR.Get(SRID.ParameterCannotBeNegative));
            }
        }



        /// <summary>
        /// Acquire a free TextFormatter context for complex line operation
        /// </summary>
        /// <param name="owner">object that becomes the owner of LS context once acquired</param>
        /// <param name="ploc">matching PLOC</param>
        /// <returns>Active LS context</returns>
        /// <SecurityNotes>
        /// Critical - this sets the owner of the context
        /// Safe     - this doesn't expose critical info
        /// </SecurityNotes>
        internal TextFormatterContext AcquireContext(
            object      owner,
            IntPtr      ploc
            )
        {
            Invariant.Assert(owner != null);

            TextFormatterContext context = null;

            int c;
            int contextCount = _contextList.Count;

            for (c = 0; c < contextCount; c++)
            {
                context = (TextFormatterContext)_contextList[c];

                if (ploc == IntPtr.Zero)
                {
                    if(context.Owner == null)
                        break;
                }
                else if (ploc == context.Ploc.Value)
                {
                    // LS requires that we use the exact same context for line
                    // destruction or hittesting (part of the reason is that LS
                    // actually caches some run info in the context). So here
                    // we use the actual PLSC as the context signature so we
                    // locate the one we want.

                    Debug.Assert(context.Owner == null);
                    break;
                }
            }

            if (c == contextCount)
            {
                if (contextCount == 0 || !_multipleContextProhibited)
                {
                    //  no free one exists, create a new one
                    context = new TextFormatterContext();
                    _contextList.Add(context);
                }
                else
                {
                    // This instance of TextFormatter only allows a single context, reentering the
                    // same TextFormatter in this case is not allowed.
                    //
                    // This requirement is currently enforced only during optimal break computation.
                    // Client implementing nesting of optimal break content inside another must create
                    // a separate TextFormatter instance for each content in different nesting level.
                    throw new InvalidOperationException(SR.Get(SRID.TextFormatterReentranceProhibited));
                }
            }

            Debug.Assert(context != null);

            context.Owner = owner;
            return context;
        }


        /// <summary>
        /// Create an anti-inversion transform from the inversion flags.
        /// The result is used to correct glyph bitmap on an output to
        /// a drawing surface with the specified inversions applied on.
        /// </summary>
        internal static MatrixTransform CreateAntiInversionTransform(
            InvertAxes  inversion,
            double      paragraphWidth,
            double      lineHeight
            )
        {
            if (inversion == InvertAxes.None)
            {
                // avoid creating unncessary pressure on GC when anti-transform is not needed.
                return null;
            }

            double m11 = 1;
            double m22 = 1;
            double offsetX = 0;
            double offsetY = 0;

            if ((inversion & InvertAxes.Horizontal) != 0)
            {
                m11 = -m11;
                offsetX = paragraphWidth;
            }

            if ((inversion & InvertAxes.Vertical) != 0)
            {
                m22 = -m22;
                offsetY = lineHeight;
            }

            return new MatrixTransform(m11, 0, 0, m22, offsetX, offsetY);
        }

        /// <summary>
        /// Compare text formatter real values - since values are rounded in Display mode, comparison
        /// must also round and only return true if one rounded value is greater than the other.
        /// </summary>
        /// <param name="x">First value to compare.</param>
        /// <param name="y">Second value to compare.</param>
        /// <param name="mode">Text formatting mode.</param>
        /// <returns>1 if x greater than y, -1 if x less than y, 0 if x == y</returns>
        internal static int CompareReal(double x, double y, double pixelsPerDip, TextFormattingMode mode)
        {
            double xDisplay = x;
            double yDisplay = y;

            if (mode == TextFormattingMode.Display)
            {
                xDisplay = RoundDipForDisplayMode(x, pixelsPerDip);
                yDisplay = RoundDipForDisplayMode(y, pixelsPerDip);
            }

            if (xDisplay > yDisplay)
            {
                return 1;
            }

            if (xDisplay < yDisplay)
            {
                return -1;
            }

            return 0;
        }

        internal static double RoundDip(double value, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            if (TextFormattingMode.Display == textFormattingMode)
            {
                return RoundDipForDisplayMode(value, pixelsPerDip);
            }
            else
            {
                return value;
            }
        }

        internal static double RoundDipForDisplayMode(double value, double pixelsPerDip)
        {
            return RoundDipForDisplayMode(value, pixelsPerDip, MidpointRounding.ToEven);
        }

        private static double RoundDipForDisplayMode(double value, double pixelsPerDip, MidpointRounding midpointRounding)
        {
            return Math.Round(value * pixelsPerDip, midpointRounding) / pixelsPerDip;
        }

        /// <summary>
        /// The default behavior of Math.Round() leads to undesirable behavior
        /// When used for display mode justified text, where we can find 
        /// characters belonging to the same word jumping sideways.
        /// A word can break among several GlyphRuns. So we need consistent
        /// rounding of the width of the GlyphRuns. If the width of one GlyphRun
        /// rounds up and the next GlyphRun rounds down then we see characters 
        /// overlapping and so on.
        /// It is too late to change the behavior of our rounding universally
        /// so we are making the change targeted to Display mode + Justified text
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static double RoundDipForDisplayModeJustifiedText(double value, double pixelsPerDip)
        {
            return RoundDipForDisplayMode(value, pixelsPerDip, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Scale LS ideal resolution value to real value
        /// </summary>
        internal static double IdealToRealWithNoRounding(double i)
        {
            return i * Constants.DefaultIdealToReal;
        }

        /// <summary>
        /// Scale LS ideal resolution value to real value
        /// </summary>
        internal double IdealToReal(double i, double pixelsPerDip)
        {
            double value = IdealToRealWithNoRounding(i);
            if (_textFormattingMode == TextFormattingMode.Display)
            {
                value = RoundDipForDisplayMode(value, pixelsPerDip);
            }

            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, Constants.DefaultIdealToReal);
            }

            return value;
        }

        /// <summary>
        /// Scale real value to LS ideal resolution
        /// </summary>
        internal static int RealToIdeal(double i)
        {
            int value = (int)Math.Round(i * ToIdeal);
            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, 1);
            }
            return value;
        }

        /// <summary>
        /// Scale the real value to LS ideal resolution
        /// Use the floor value of the scale value
        /// </summary>
        /// <remarks>
        /// Using Math.Round may result in a line larger than
        /// the actual given paragraph width. For example,
        /// round tripping 100.112 with factor 300 becomes 100.1133...
        /// Using floor to ensure we never go beyond paragraph width
        /// </remarks>
        internal static int RealToIdealFloor(double i)
        {
            int value = (int)Math.Floor(i * ToIdeal);
            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, 1);
            }
            return value;
        }

        /// <summary>
        /// Real to ideal value scaling factor
        /// </summary>
        internal static double ToIdeal
        {
            get { return Constants.DefaultRealToIdeal; }
        }

        /// <summary>
        /// Return the GlyphingCache associated with this TextFormatterImp object.
        /// GlyphingCache stores the mapping from Unicode scalar value to the physical font that is
        /// used to display it.
        /// </summary>
        internal GlyphingCache GlyphingCache
        {
            get
            {
                if (_glyphingCache == null)
                {
                    _glyphingCache = new GlyphingCache(MaxGlyphingCacheCapacity);
                }

                return _glyphingCache;
            }
        }

        /// <summary>
        /// Return the TextAnalyzer associated with this TextFormatterImp object.
        /// TextAnalyzer is used in shaping process.
        /// </summary>
        internal TextAnalyzer TextAnalyzer
        {
            get
            {
                if (_textAnalyzer == null)
                {
                    _textAnalyzer = DWriteFactory.Instance.CreateTextAnalyzer();
                }

                return _textAnalyzer;
            }
        }
    }
}

