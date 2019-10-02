// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Complex implementation of TextLine
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal;
using MS.Internal.Shaping;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace MS.Internal.TextFormatting
{
    /// <remarks>
    /// Make FullTextLine nested type of TextMetrics to allow full access to TextMetrics private members
    /// </remarks>
    internal partial struct TextMetrics : ITextMetrics
    {
        /// <summary>
        /// Complex implementation of TextLine
        ///
        /// TextLine implementation around
        ///     o   Line Services
        ///     o   OpenType Services Library
        ///     o   Implementation of Unicode Bidirectional algorithm
        ///     o   Complex script itemizer
        ///     o   Composite font with generic glyph hunting algorithm
        /// </summary>
        internal class FullTextLine : TextLine
        {
            private TextMetrics                         _metrics;                       // Text metrics
            private int                                 _cpFirst;                       // character index to the first charcter of the line
            private int                                 _depthQueryMax;                 // maximum depth of reversals used in querying
            private int                                 _paragraphWidth;                // paragraph width
            private int                                 _textMinWidthAtTrailing;        // smallest text width excluding trailing whitespaces
            private SecurityCriticalDataForSet<IntPtr>  _ploline;                       // actual LS line
            private SecurityCriticalDataForSet<IntPtr>  _ploc;                          // actual LS context
            private Overhang                            _overhang;                      // overhang metrics
            private StatusFlags                         _statusFlags;                   // status flags of the line

            private SpanVector                          _plsrunVector;                  // plsrun span vector indexed by lscp
            private ArrayList                           _lsrunsMainText;                // list of lsrun of main text
            private ArrayList                           _lsrunsMarkerText;              // list of lsrun of marker text

            private FullTextState                       _fullText;                      // full text state kept for collapsing purpose (only have it when StatusFlags.HasOverflowed is set)
            private FormattedTextSymbols                _collapsingSymbol;              // line-end collapsing symbol
            private TextCollapsedRange                  _collapsedRange;                // line-end collapsed range

            private TextSource                          _textSource;                    // Text Source of the main text for the line
            private TextDecorationCollection            _paragraphTextDecorations;      // Paragraph-level text decorations (or null if none)
            private Brush                               _defaultTextDecorationsBrush;   // Default brush for paragraph text decorations
            private TextFormattingMode                  _textFormattingMode;            // The TextFormattingMode of the line (Ideal or Display).

            [Flags]
            private enum StatusFlags
            {
                None                = 0,
                IsDisposed          = 0x00000001,
                HasOverflowed       = 0x00000002,
                BoundingBoxComputed = 0x00000004,
                RightToLeft         = 0x00000008,
                HasCollapsed        = 0x00000010,
                KeepState           = 0x00000020,
                IsTruncated         = 0x00000040,
                IsJustified         = 0x00000080, // Indicates whether the text alignment is set to justified
                                                  // This flag is needed later to decide how the metrics
                                                  // will be rounded in display mode when converted
                                                  // from ideal to real values.
            }

            private enum CaretDirection
            {
                Forward,
                Backward,
                Backspace
            }

            /// <summary>
            /// Constructing a FullTextLine
            /// </summary>
            /// <param name="settings">text formatting settings</param>
            /// <param name="cpFirst">Line's first cp</param>
            /// <param name="lineLength">character length of the line</param>
            /// <param name="paragraphWidth">paragraph width</param>
            /// <param name="lineFlags">line formatting control flags</param>
            internal FullTextLine(
                FormatSettings          settings,
                int                     cpFirst,
                int                     lineLength,
                int                     paragraphWidth,
                LineFlags               lineFlags
                )
                : this(settings.TextFormattingMode, settings.Pap.Justify, settings.TextSource.PixelsPerDip)
            {
                if (    (lineFlags & LineFlags.KeepState) != 0
                    ||  settings.Pap.AlwaysCollapsible)
                {
                    _statusFlags |= StatusFlags.KeepState;
                }

                int finiteFormatWidth = settings.GetFiniteFormatWidth(paragraphWidth);

                FullTextState fullText = FullTextState.Create(settings, cpFirst, finiteFormatWidth);

                // formatting the line
                FormatLine(
                    fullText,
                    cpFirst,
                    lineLength,
                    fullText.FormatWidth,
                    finiteFormatWidth,
                    paragraphWidth,
                    lineFlags,
                    null    // collapsingSymbol
                    );
            }


            /// <summary>
            /// Finalizing full text line
            /// </summary>
            ~FullTextLine()
            {
                DisposeInternal(true);
            }


            /// <summary>
            /// Releasing the line's unmanaged resource
            /// </summary>
            public override void Dispose()
            {
                DisposeInternal(false);
                GC.SuppressFinalize(this);
            }


            /// <summary>
            /// Disposing LS unmanaged memory for text line
            /// </summary>
            private void DisposeInternal(bool finalizing)
            {
                if (_ploline.Value != System.IntPtr.Zero)
                {
                    UnsafeNativeMethods.LoDisposeLine(_ploline.Value, finalizing);

                    _ploline.Value = System.IntPtr.Zero;
                    GC.KeepAlive(this);
                }
            }


            /// <summary>
            /// Empty private constructor
            /// </summary>
            private FullTextLine(TextFormattingMode textFormattingMode, bool justify, double pixelsPerDip) : base(pixelsPerDip)
            {
                _textFormattingMode = textFormattingMode;
                if (justify)
                {
                    _statusFlags |= StatusFlags.IsJustified;
                }
                _metrics = new TextMetrics();
                _metrics._pixelsPerDip = pixelsPerDip;
                _ploline = new SecurityCriticalDataForSet<IntPtr>(IntPtr.Zero);
            }


            /// <summary>
            /// format text line using LS
            /// </summary>
            /// <param name="fullText">state of the full text backing store</param>
            /// <param name="cpFirst">first cp to format</param>
            /// <param name="lineLength">character length of the line</param>
            /// <param name="formatWidth">width used to format</param>
            /// <param name="finiteFormatWidth">width used to detect overflowing of format result</param>
            /// <param name="paragraphWidth">paragraph width</param>
            /// <param name="lineFlags">line formatting control flags</param>
            /// <param name="collapsingSymbol">line end collapsing symbol</param>
            private void FormatLine(
                FullTextState           fullText,
                int                     cpFirst,
                int                     lineLength,
                int                     formatWidth,
                int                     finiteFormatWidth,
                int                     paragraphWidth,
                LineFlags               lineFlags,
                FormattedTextSymbols    collapsingSymbol
                )
            {
                _metrics._formatter = fullText.Formatter;
                Debug.Assert(_metrics._formatter != null);

                TextStore store = fullText.TextStore;
                TextStore markerStore = fullText.TextMarkerStore;
                FormatSettings settings = store.Settings;
                ParaProp pap = settings.Pap;

                _paragraphTextDecorations = pap.TextDecorations;
                if (_paragraphTextDecorations != null)
                {
                    if (_paragraphTextDecorations.Count != 0)
                    {
                        _defaultTextDecorationsBrush = pap.DefaultTextDecorationsBrush;
                    }
                    else
                    {
                        _paragraphTextDecorations = null;
                    }
                }

                // acquiring LS context
                TextFormatterContext context = _metrics._formatter.AcquireContext(fullText, IntPtr.Zero);

                LsLInfo plslineInfo = new LsLInfo();
                LsLineWidths lineWidths = new LsLineWidths();

                fullText.SetTabs(context);

                int lscpLineLength = 0; // line length in LSCP
                if (lineLength > 0)
                {
                    // line length is previously known (e.g. during optimal paragraph formatting),
                    // prefetch lsruns up to the specified line length.
                    lscpLineLength = PrefetchLSRuns(store, cpFirst, lineLength);
                }

                IntPtr ploline;
                LsErr lserr = context.CreateLine(
                    cpFirst,
                    lscpLineLength,
                    formatWidth,
                    lineFlags,
                    IntPtr.Zero,    // single-line formatting does not require break record
                    out ploline,
                    out plslineInfo,
                    out _depthQueryMax,
                    out lineWidths
                    );

                // Did we exceed the LineServices maximum line width?
                if (lserr == LsErr.TooLongParagraph)
                {
                    // Determine where to insert a fake line break. FullTextState.CpMeasured
                    // is a reasonable estimate since we know the nominal widths up to that
                    // point fit within the margin.
                    int cpLimit = fullText.CpMeasured;
                    int subtract = 1;

                    for (;;)
                    {
                        // The line must contain at least one character position.
                        if (cpLimit < 1)
                        {
                            cpLimit = 1;
                        }

                        store.InsertFakeLineBreak(cpLimit);

                        lserr = context.CreateLine(
                            cpFirst,
                            lscpLineLength,
                            formatWidth,
                            lineFlags,
                            IntPtr.Zero,    // single-line formatting does not require break record
                            out ploline,
                            out plslineInfo,
                            out _depthQueryMax,
                            out lineWidths
                            );

                        if (lserr != LsErr.TooLongParagraph || cpLimit == 1)
                        {
                            // We're done or can't chop off any more text.
                            break;
                        }
                        else
                        {
                            // Chop off more text and try again. Double the amount of
                            // text we chop off each time so we retry too many times.
                            cpLimit = fullText.CpMeasured - subtract;
                            subtract *= 2;
                        }
                    }
                }

                _ploline.Value = ploline;

                // get the exception in context before it is released
                Exception callbackException = context.CallbackException;

                // release the context
                context.Release();

                if(lserr != LsErr.None)
                {
                    GC.SuppressFinalize(this);
                    if(callbackException != null)
                    {
                        // rethrow exception thrown in callbacks
                        throw WrapException(callbackException);
                    }
                    else
                    {
                        // throw with LS error codes
                        TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.CreateLineFailure, lserr), lserr);
                    }
                }

                // keep context alive at least till here
                GC.KeepAlive(context);

                unsafe
                {
                    // construct text metrics for the line
                    _metrics.Compute(
                        fullText,
                        cpFirst,
                        paragraphWidth,
                        collapsingSymbol,
                        ref lineWidths,
                        &plslineInfo
                        );
                }

                // keep record for min width as we may be formatting min/max
                _textMinWidthAtTrailing = lineWidths.upMinStartTrailing - _metrics._textStart;

                if (collapsingSymbol != null)
                {
                    _collapsingSymbol = collapsingSymbol;
                    _textMinWidthAtTrailing += TextFormatterImp.RealToIdeal(collapsingSymbol.Width);
                }
                else
                {
                    // overflow detection for potential collapsible line
                    if (_metrics._textStart + _metrics._textWidthAtTrailing > finiteFormatWidth)
                    {
                        bool hasOverflowed = true;
                        if (_textFormattingMode == TextFormattingMode.Display)
                        {
                            // apply display-mode rounding before checking for overflow
                            double realWidth = Width;
                            double realFormatWidth = _metrics._formatter.IdealToReal(finiteFormatWidth, PixelsPerDip);
                            hasOverflowed = (TextFormatterImp.CompareReal(realWidth, realFormatWidth, PixelsPerDip, _textFormattingMode) > 0);
                        }

                        if (hasOverflowed)
                        {
                            // line has overflowed
                            _statusFlags |= StatusFlags.HasOverflowed;

                            // let's keep the full text state around. We'll need it later for collapsing
                            _fullText = fullText;
                        }
                    }
                }

                if (    fullText != null
                    &&  (   fullText.KeepState
                        ||  (_statusFlags & StatusFlags.KeepState) != 0
                        )
                    )
                {
                    // the state of full text is to be kept after formatting is done
                    _fullText = fullText;
                }

                // retain all line properties for interactive operations
                _ploc = context.Ploc;
                _cpFirst = cpFirst;
                _paragraphWidth = paragraphWidth;

                if (pap.RightToLeft)
                    _statusFlags |= StatusFlags.RightToLeft;

                if (plslineInfo.fForcedBreak != 0)
                    _statusFlags |= StatusFlags.IsTruncated;

                // retain the state of plsruns
                _plsrunVector = store.PlsrunVector;
                _lsrunsMainText = store.LsrunList;

                if (markerStore != null)
                    _lsrunsMarkerText = markerStore.LsrunList;

                // we store the text source in the line in case drawing code calls
                // the TextSource to find out the text effect index.
                // Note: Remove this when we remove text effect index callback.
                _textSource = settings.TextSource;
            }


            /// <summary>
            /// Wraps a caught exception in a new exception object of the same type, if possible.
            /// Otherwise just return the original exception.
            /// </summary>
            private static Exception WrapException(Exception caughtException)
            {
                // We're going to try to create a new exception of the same type as caughtException.
                Type t = caughtException.GetType();

                // Make sure the type is public to avoid MethodAccessException in partial trust.
                if (t.IsPublic)
                {
                    // Look for a public instance constructor with signature ctor(Exception)
                    ConstructorInfo constructor = t.GetConstructor(
                        new Type[] { typeof(Exception) }
                        );
                    if (constructor != null)
                    {
                        return (Exception)constructor.Invoke(
                            new object[] { caughtException }
                            );
                    }

                    // Look for a public instance constructor with signature ctor(string,Exception)
                    constructor = t.GetConstructor(
                        new Type[] { typeof(string), typeof(Exception) }
                        );
                    if (constructor != null)
                    {
                        return (Exception)constructor.Invoke(
                            new object[] { caughtException.Message, caughtException }
                            );
                    }
                }

                // We couldn't find an appropriate constructor so fall back to returning the original
                // exception object. We don't want to wrap the exception in some arbitrary exception type
                // because the client may have thrown the exception and may want to catch it higher up
                // the stack.
                //
                // The disadvantage of throwing the same object again is the original stack is lost. This
                // makes debugging harder (partially mitigated by the stack trace string available via
                // the Data property -- but only in full dumps), and means Watson errors will be bucketized
                // only based on the current stack, i.e., FormatLine. Hopefully this case will be rare.
                //
                return caughtException;
            }


            /// <summary>
            /// Append line end collapsing symbol
            /// </summary>
            private void AppendCollapsingSymbol(
                FormattedTextSymbols    symbol
                )
            {
                Debug.Assert(_collapsingSymbol == null && symbol != null);

                _collapsingSymbol = symbol;
                int symbolIdealWidth = TextFormatterImp.RealToIdeal(symbol.Width);
                _metrics.AppendCollapsingSymbolWidth(symbolIdealWidth);
                _textMinWidthAtTrailing += symbolIdealWidth;
            }


            /// <summary>
            /// Prefetch the lsruns up to the point of the specified line length and map
            /// the specified length to the corresponding LSCP length.
            /// </summary>
            /// <remarks>
            /// See comment in the remark section of FullTextState.GetBreakpointInternalCp.
            /// </remarks>
            private int PrefetchLSRuns(
                TextStore   store,
                int         cpFirst,
                int         lineLength
                )
            {
                Debug.Assert(lineLength > 0);

                LSRun lsrun;
                int prefetchLength = 0;
                int lscpLineLength = 0;

                int lastSpanLength = 0;
                int lastRunLength = 0;

                do
                {
                    Plsrun plsrun;
                    int lsrunOffset;
                    int lsrunLength;

                    lsrun = store.FetchLSRun(
                        cpFirst + lscpLineLength,
                        _textFormattingMode,
                        false,
                        out plsrun,
                        out lsrunOffset,
                        out lsrunLength
                        );

                    if (lineLength == prefetchLength && lsrun.Type == Plsrun.Reverse)
                    {
                        break;
                    }

                    lastSpanLength = lsrunLength;
                    lastRunLength = lsrun.Length;

                    lscpLineLength += lastSpanLength;
                    prefetchLength += lastRunLength;
} while (   !TextStore.IsNewline(lsrun.Type)
                        &&  lineLength >= prefetchLength
                    );

                // calibrate the LSCP length to the LSCP equivalence of the last CP of the line

                if (prefetchLength == lineLength || lastSpanLength == lastRunLength)
                    return lscpLineLength - prefetchLength + lineLength;

                Invariant.Assert(prefetchLength - lineLength == lastRunLength);
                return lscpLineLength - lastSpanLength;
            }



            /// <summary>
            /// Draw line
            /// </summary>
            /// <param name="drawingContext">drawing context</param>
            /// <param name="origin">drawing origin</param>
            /// <param name="inversion">indicate the inversion of the drawing surface</param>
            public override void Draw(
                DrawingContext      drawingContext,
                Point               origin,
                InvertAxes          inversion
                )
            {
                if (drawingContext == null)
                {
                    throw new ArgumentNullException("drawingContext");
                }

                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                MatrixTransform antiInversion = TextFormatterImp.CreateAntiInversionTransform(
                    inversion,
                    _metrics._formatter.IdealToReal(_paragraphWidth, PixelsPerDip),
                    _metrics._formatter.IdealToReal(_metrics._height, PixelsPerDip)
                    );

                if (antiInversion == null)
                {
                    DrawTextLine(drawingContext, origin, null);
                }
                else
                {
                    // Apply anti-inversion transform to correct the visual
                    drawingContext.PushTransform(antiInversion);
                    try
                    {
                        DrawTextLine(drawingContext, origin, antiInversion);
                    }
                    finally
                    {
                        drawingContext.Pop();
                    }
                }
            }

            /// <summary>
            /// Draw complex text line
            /// </summary>
            /// <param name="drawingContext">drawing surface</param>
            /// <param name="origin">offset to the line origin</param>
            /// <param name="antiInversion">anti-inversion transform applied on the surface</param>
            private void DrawTextLine(
                DrawingContext      drawingContext,
                Point               origin,
                MatrixTransform     antiInversion
                )
            {
                Rect boundingBox = Rect.Empty;

                if (_ploline.Value != System.IntPtr.Zero)
                {
                    TextFormatterContext context;
                    LsErr lserr = LsErr.None;
                    LSRECT rect = new LSRECT(0, 0, _metrics._textWidthAtTrailing, _metrics._height);

                    // DrawingState needs to be properly disposed after performing actual drawing operations.
                    using (DrawingState drawingState = new DrawingState(drawingContext, origin, antiInversion, this))
                    {
                        context = _metrics._formatter.AcquireContext(
                            drawingState,
                            _ploc.Value
                            );

                        // set the collector and send the line to LS to draw

                        context.EmptyBoundingBox();

                        // LS line reference origin
                        LSPOINT lsRefOrigin = new LSPOINT(0, _metrics._baselineOffset);

                        lserr = UnsafeNativeMethods.LoDisplayLine(
                            _ploline.Value,
                            ref lsRefOrigin,
                            1,      // 0 - opaque, 1 - transparent
                            ref rect
                            );
                    }

                    boundingBox = context.BoundingBox;

                    // get the exception in context before it is released
                    Exception callbackException = context.CallbackException;

                    context.Release();

                    if(lserr != LsErr.None)
                    {
                        if(callbackException != null)
                        {
                            // rethrow exception thrown in callbacks
                            throw callbackException;
                        }
                        else
                        {
                            // throw with LS error codes
                            TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.CreateLineFailure, lserr), lserr);
                        }
                    }

                    // keep context alive at least til here
                    GC.KeepAlive(context);
                }

                if (_collapsingSymbol != null)
                {
                    // draw collapsing symbol if any
                    Point vectorToOrigin = new Point();
                    if (antiInversion != null)
                    {
                        vectorToOrigin = origin;
                        origin.X = origin.Y = 0;
                    }

                    boundingBox.Union(DrawCollapsingSymbol(drawingContext, origin, vectorToOrigin));
                }

                BuildOverhang(origin, boundingBox);
                _statusFlags |= StatusFlags.BoundingBoxComputed;
            }


            /// <summary>
            /// Draw line end collapsing symbol
            /// </summary>
            private Rect DrawCollapsingSymbol(
                DrawingContext drawingContext,
                Point          lineOrigin,
                Point          vectorToLineOrigin
                )
            {
                int symbolIdealWidth = TextFormatterImp.RealToIdeal(_collapsingSymbol.Width);

                Point symbolOrigin = LSRun.UVToXY(
                    lineOrigin,
                    vectorToLineOrigin,
                    LSLineUToParagraphU(_metrics._textStart + _metrics._textWidthAtTrailing - symbolIdealWidth),
                    _metrics._baselineOffset,
                    this
                    );

                return _collapsingSymbol.Draw(drawingContext, symbolOrigin);
            }


            /// <summary>
            /// Make sure the bounding box is calculated
            /// </summary>
            private void CheckBoundingBox()
            {
                if ((_statusFlags & StatusFlags.BoundingBoxComputed) == 0)
                {
                    DrawTextLine(null, new Point(0, 0), null);
                }
                Debug.Assert((_statusFlags & StatusFlags.BoundingBoxComputed) != 0);
            }



            /// <summary>
            /// Client to collapse the line to fit for display
            /// </summary>
            /// <param name="collapsingPropertiesList">a list of collapsing properties</param>
            public override TextLine Collapse(
                params TextCollapsingProperties[]   collapsingPropertiesList
                )
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                if (    !HasOverflowed
                    &&  (_statusFlags & StatusFlags.KeepState) == 0)
                {
                    // Attempt to collapse a non-overflowed line results in the original line returned
                    return this;
                }

                if (collapsingPropertiesList == null || collapsingPropertiesList.Length == 0)
                    throw new ArgumentNullException("collapsingPropertiesList");

                TextCollapsingProperties collapsingProp = collapsingPropertiesList[0];
                double constraintWidth = collapsingProp.Width;

                if (TextFormatterImp.CompareReal(constraintWidth, Width, PixelsPerDip, _textFormattingMode) > 0)
                {
                    // constraining width is greater than original line width, no collapsing neeeded.
                    return this;
                }

                FormattedTextSymbols symbol = null;

                if (collapsingProp.Symbol != null)
                {
                    // create formatted collapsing symbol
                    symbol = new FormattedTextSymbols(
                        _metrics._formatter.GlyphingCache,
                        collapsingProp.Symbol,
                        RightToLeft,
                        TextFormatterImp.ToIdeal,
                        (float)PixelsPerDip,
                        _textFormattingMode,
                        false
                        );
                    constraintWidth -= symbol.Width;
                }

                Debug.Assert(_fullText != null);

                FullTextLine line = new TextMetrics.FullTextLine(_textFormattingMode, IsJustified, PixelsPerDip);

                // collapsing preserves original line metrics
                Debug.Assert(_metrics._height > 0);
                line._metrics._formatter = _metrics._formatter;
                line._metrics._height = _metrics._height;
                line._metrics._baselineOffset = _metrics._baselineOffset;

                if (constraintWidth > 0)
                {
                    // format main text line with constraint width

                    int finiteFormatWidth = _fullText.TextStore.Settings.GetFiniteFormatWidth(
                        TextFormatterImp.RealToIdeal(constraintWidth)
                        );

                    bool forceWrap = _fullText.ForceWrap;
                    _fullText.ForceWrap = true;

                    if ((_statusFlags & StatusFlags.KeepState) != 0)
                    {
                        // inherit this flag so the collapsed line retains full text state too.
                        line._statusFlags |= StatusFlags.KeepState;
                    }

                    line.FormatLine(
                        _fullText,
                        _cpFirst,
                        0,  // no line length limit
                        finiteFormatWidth,
                        finiteFormatWidth,
                        _paragraphWidth,    // collapsed line is still bound to the original paragraph width
                        (collapsingProp.Style == TextCollapsingStyle.TrailingCharacter ? LineFlags.BreakAlways : LineFlags.None),
                        symbol
                        );

                    _fullText.ForceWrap = forceWrap;

                    line._metrics._cchDepend = 0; // no dependency
                }
                else if (symbol != null)
                {
                    line.AppendCollapsingSymbol(symbol);
                }

                if (line._metrics._cchLength < Length)
                {
                    line._collapsedRange = new TextCollapsedRange(
                        _cpFirst + line._metrics._cchLength,
                        Length - line._metrics._cchLength,
                        Width - line.Width
                        );

                    // collapsed line has the original length
                    line._metrics._cchLength = Length;
                }

                // mark the indication flags signify collapsing
                line._statusFlags |= StatusFlags.HasCollapsed;
                line._statusFlags &= ~StatusFlags.HasOverflowed;

                return line;
            }



            /// <summary>
            /// Client to get a collection of collapsed character ranges after a line has been collapsed
            /// </summary>
            public override IList<TextCollapsedRange> GetTextCollapsedRanges()
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                if (_collapsedRange == null)
                    return null;

                Debug.Assert(HasCollapsed);
                return new TextCollapsedRange[] { _collapsedRange };
            }



            /// <summary>
            /// Client to get the character hit corresponding to the specified
            /// distance from the beginning of the line.
            /// </summary>
            /// <param name="distance">distance in text flow direction from the beginning of the line</param>
            /// <returns>character hit</returns>
            public override CharacterHit GetCharacterHitFromDistance(
                double      distance
                )
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                return CharacterHitFromDistance(ParagraphUToLSLineU(TextFormatterImp.RealToIdeal(distance)));
            }


            /// <summary>
            /// Get character hit from specified hittest distance relative to line start
            /// </summary>
            private CharacterHit CharacterHitFromDistance(int hitTestDistance)
            {
                // assuming the first cp of the line
                CharacterHit characterHit = new CharacterHit(_cpFirst, 0);

                if(_ploline.Value == IntPtr.Zero)
                {
                    // Returning the first cp for the empty line
                    return characterHit;
                }

                if (    HasCollapsed
                    &&  _collapsedRange != null
                    &&  _collapsingSymbol != null
                    )
                {
                    int lineEndDistance = _metrics._textStart + _metrics._textWidthAtTrailing;
                    int rangeWidth = TextFormatterImp.RealToIdeal(_collapsingSymbol.Width);

                    if (hitTestDistance >= lineEndDistance - rangeWidth)
                    {
                        if (lineEndDistance - hitTestDistance < rangeWidth / 2)
                        {
                            // The hit-test distance is within the trailing edge of the collapsed range,
                            // return the character hit at the beginning of the range.
                            return new CharacterHit(_collapsedRange.TextSourceCharacterIndex, _collapsedRange.Length);
                        }

                        // The hit-test distance is within the leading edge of the collapsed range,
                        // return the character hit at the beginning of the range.
                        return new CharacterHit(_collapsedRange.TextSourceCharacterIndex, 0);
                    }
                }

                LsTextCell lsTextCell;
                LsQSubInfo[] sublineInfo = new LsQSubInfo[_depthQueryMax];
                int actualSublineCount;

                QueryLinePointPcp(
                    new Point(hitTestDistance, 0),
                    sublineInfo,
                    out actualSublineCount,
                    out lsTextCell
                    );

                if (actualSublineCount > 0 && lsTextCell.dupCell > 0)
                {
                    // the last subline contains the run that owns the querying lscp
                    LSRun lsrun = GetRun((Plsrun)sublineInfo[actualSublineCount - 1].plsrun);

                    // Assuming caret stops at every codepoint.
                    //
                    // LsTextCell.lscpEndCell is the index to the last lscp still in the cell.
                    // The number of LSCP within the text cell is equal to the number of CP.
                    int caretStopCount = lsTextCell.lscpEndCell + 1 - lsTextCell.lscpStartCell;

                    int codepointsToNextCaretStop = lsrun.IsHitTestable ? 1 : lsrun.Length;

                    if (    lsrun.IsHitTestable
                        &&  (   lsrun.HasExtendedCharacter
                            ||  lsrun.NeedsCaretInfo)
                        )
                    {
                        // A hit-testable run with caret stops at every cluster boundaries,
                        // e.g. run with combining mark, with extended characters or complex scripts such as Thai
                        codepointsToNextCaretStop = caretStopCount;
                        caretStopCount = 1;
                    }

                    // All the UV coordinate in subline are in main direction. If the last subline where
                    // we hittest runs in the opposite direction, the logical advance from text cell start cp
                    // will be negative value.
                    int direction = (sublineInfo[actualSublineCount - 1].lstflowSubLine == sublineInfo[0].lstflowSubLine) ? 1 : -1;
                    hitTestDistance = (hitTestDistance - lsTextCell.pointUvStartCell.x) * direction;

                    Invariant.Assert(caretStopCount > 0);
                    int wholeAdvance = lsTextCell.dupCell / caretStopCount;
                    int remainingAdvance = lsTextCell.dupCell % caretStopCount;

                    for (int i = 0; i < caretStopCount; i++)
                    {
                        int caretAdvance = wholeAdvance;
                        if (remainingAdvance > 0)
                        {
                            caretAdvance++;
                            remainingAdvance--;
                        }

                        if (hitTestDistance <= caretAdvance)
                        {
                            if (hitTestDistance > caretAdvance / 2)
                            {
                                // hittest at the trailing edge of the current caret stop
                                return new CharacterHit(GetExternalCp(lsTextCell.lscpStartCell) + i, codepointsToNextCaretStop);
                            }

                            // hittest at the leading edge of the current caret stop
                            return new CharacterHit(GetExternalCp(lsTextCell.lscpStartCell) + i, 0);
                        }

                        hitTestDistance -= caretAdvance;
                    }

                    // hittest beyond the last caret stop, return the trailing edge of the last caret stop
                    return new CharacterHit(GetExternalCp(lsTextCell.lscpStartCell) + caretStopCount - 1, codepointsToNextCaretStop);
                }

                return characterHit;
            }



            /// <summary>
            /// Client to get the distance from the beginning of the line from the specified character hit.
            /// </summary>
            /// <param name="characterHit">index to text source's character store</param>
            /// <returns>distance in text flow direction from the beginning of the line.</returns>
            public override double GetDistanceFromCharacterHit(
                CharacterHit    characterHit
                )
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _metrics._cchLength);

                return _metrics._formatter.IdealToReal(LSLineUToParagraphU(DistanceFromCharacterHit(characterHit)), PixelsPerDip);
            }



            /// <summary>
            /// Get hittest distance relative to line start from specified character hit
            /// </summary>
            private int DistanceFromCharacterHit(CharacterHit characterHit)
            {
                int hitTestDistance = 0;

                if (_ploline.Value == IntPtr.Zero)
                {
                    // Returning start of the line for empty line
                    return hitTestDistance;
                }

                if (characterHit.FirstCharacterIndex >= _cpFirst + _metrics._cchLength)
                {
                    // Returning line width for character hit beyond the last caret stop
                    return _metrics._textStart + _metrics._textWidthAtTrailing;
                }

                if (    HasCollapsed
                    &&  _collapsedRange != null
                    &&  characterHit.FirstCharacterIndex >= _collapsedRange.TextSourceCharacterIndex
                    )
                {
                    // The current character hit is beyond the beginning of the collapsed range
                    int lineEndDistance = _metrics._textStart + _metrics._textWidthAtTrailing;

                    if (    characterHit.FirstCharacterIndex >= _collapsedRange.TextSourceCharacterIndex + _collapsedRange.Length
                        ||  characterHit.TrailingLength != 0
                        || _collapsingSymbol == null
                        )
                    {
                        // The current character hit either hits outside,
                        // or it's at the trailing edge of the collapsed range
                        return lineEndDistance;
                    }

                    return lineEndDistance - TextFormatterImp.RealToIdeal(_collapsingSymbol.Width);
                }

                int actualSublineCount;
                LsTextCell lsTextCell;
                LsQSubInfo[] sublineInfo = new LsQSubInfo[_depthQueryMax];

                int lscpCurrent = GetInternalCp(characterHit.FirstCharacterIndex);

                QueryLineCpPpoint(
                    lscpCurrent,
                    sublineInfo,
                    out actualSublineCount,
                    out lsTextCell
                    );

                if (actualSublineCount > 0)
                {
                    return lsTextCell.pointUvStartCell.x + GetDistanceInsideTextCell(
                        lscpCurrent,
                        characterHit.TrailingLength != 0,
                        sublineInfo,
                        actualSublineCount,
                        ref lsTextCell
                        );
                }

                return hitTestDistance;
            }


            /// <summary>
            /// Get distance from the start of text cell to the specified lscp
            /// </summary>
            private int GetDistanceInsideTextCell(
                int                 lscpCurrent,
                bool                isTrailing,
                LsQSubInfo[]        sublineInfo,
                int                 actualSublineCount,
                ref LsTextCell      lsTextCell
                )
            {
                int distanceInCell = 0;

                // All the UV coordinate in subline are in main direction. If the last subline where
                // we hittest runs in the opposite direction, the logical advance from text cell start cp
                // will be negative value.
                int direction = (sublineInfo[actualSublineCount - 1].lstflowSubLine == sublineInfo[0].lstflowSubLine) ? 1 : -1;

                // LsTextCell.lscpEndCell is the index to the last lscp still in the cell.
                // The number of LSCP within the text cell is equal to the number of CP.
                //
                // Assuming caret stops at every codepoint in the run.
                int caretStopCount = lsTextCell.lscpEndCell + 1 - lsTextCell.lscpStartCell;
                int codepointsFromStartCell = lscpCurrent - lsTextCell.lscpStartCell;

                // the last subline contains the run that owns the querying lscp
                LSRun lsrun = GetRun((Plsrun)sublineInfo[actualSublineCount - 1].plsrun);

                if (    lsrun.IsHitTestable
                    &&  (   lsrun.HasExtendedCharacter
                        ||  lsrun.NeedsCaretInfo)
                    )
                {
                    // A hit-testable run with caret stops at every cluster boundaries,
                    // e.g. run with combining mark, with extended characters or complex scripts such as Thai
                    caretStopCount = 1;
                }

                Invariant.Assert(caretStopCount > 0);
                int wholeAdvance = lsTextCell.dupCell / caretStopCount;
                int remainingAdvance = lsTextCell.dupCell % caretStopCount;

                for (int i = 1; i <= caretStopCount; i++)
                {
                    int caretAdvance = wholeAdvance;
                    if (remainingAdvance > 0)
                    {
                        caretAdvance++;
                        remainingAdvance--;
                    }

                    if (codepointsFromStartCell < i)
                    {
                        if (isTrailing)
                        {
                            // hit-test at the trailing edge of the current caret stop, include the current caret advance
                            return (distanceInCell + caretAdvance) * direction;
                        }

                        // hit-test at the leading edge of the current caret stop, return the accumulated distance
                        return distanceInCell * direction;
                    }

                    distanceInCell += caretAdvance;
                }

                // hit-test beyond the last caret stop, return the total accumated distance up to the trailing edge of the last caret stop.
                return distanceInCell * direction;
            }


            /// <summary>
            /// Client to get the next character hit for caret navigation
            /// </summary>
            /// <param name="characterHit">the current character hit</param>
            /// <returns>the next character hit</returns>
            public override CharacterHit GetNextCaretCharacterHit(
                CharacterHit    characterHit
                )
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _metrics._cchLength);

                if (_ploline.Value == System.IntPtr.Zero)
                {
                    return characterHit;
                }

                int caretStopIndex;
                int offsetToNextCaretStopIndex;

                bool found = GetNextOrPreviousCaretStop(
                    characterHit.FirstCharacterIndex,
                    CaretDirection.Forward,
                    out caretStopIndex,
                    out offsetToNextCaretStopIndex
                    );

                if (!found)
                {
                    // The current index is beyond the last caret stop.
                    return characterHit;
                }

                if (caretStopIndex <= characterHit.FirstCharacterIndex && characterHit.TrailingLength != 0)
                {
                    // We treat trailing length of the current character hit as a flag on the way in.
                    // A non-zero value indicates that it is on the trailing edge of the current
                    // caret stop. At this point, the current caret stop fully encloses the input index,
                    // and the input is at the trailing edge. In this case, we move it to the trailing
                    // edge of the next caret stop.
                    found = GetNextOrPreviousCaretStop(
                        caretStopIndex + offsetToNextCaretStopIndex,
                        CaretDirection.Forward,
                        out caretStopIndex,
                        out offsetToNextCaretStopIndex
                        );

                    if (!found)
                    {
                        // This current index is beyond the last caret stop
                        return characterHit;
                    }

                    return new CharacterHit(caretStopIndex, offsetToNextCaretStopIndex);
                }

                // If the current character hit is at the leading edge,
                // move it to trailing edge of the current caret stop.
                return new CharacterHit(caretStopIndex, offsetToNextCaretStopIndex);
            }


            /// <summary>
            /// Client to get the previous character hit for caret navigation
            /// </summary>
            /// <param name="characterHit">the current character hit</param>
            /// <returns>the previous character hit</returns>
            public override CharacterHit GetPreviousCaretCharacterHit(
                CharacterHit characterHit
                )
            {
                return GetPreviousCaretCharacterHitByBehavior(characterHit, CaretDirection.Backward);
            }


            /// <summary>
            /// Client to get the previous character hit after backspacing
            /// </summary>
            /// <param name="characterHit">the current character hit</param>
            /// <returns>the character hit after backspacing</returns>
            public override CharacterHit GetBackspaceCaretCharacterHit(
                CharacterHit characterHit
                )
            {
                return GetPreviousCaretCharacterHitByBehavior(characterHit, CaretDirection.Backspace);
            }


            /// <summary>
            /// Calculate previous caret character hit based on the caret action
            /// </summary>
            private CharacterHit GetPreviousCaretCharacterHitByBehavior(
                CharacterHit    characterHit,
                CaretDirection  direction
                )
            {
                Debug.Assert(direction == CaretDirection.Backward || direction == CaretDirection.Backspace);

                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _metrics._cchLength);

                if (_ploline.Value == IntPtr.Zero)
                {
                    return characterHit;
                }

                if (    characterHit.FirstCharacterIndex == _cpFirst
                    &&  characterHit.TrailingLength == 0)
                {
                    // We are already at the beginning of the line
                    return characterHit;
                }

                int caretStopIndex;
                int offsetToNextCaretStopIndex;

                bool found = GetNextOrPreviousCaretStop(
                    characterHit.FirstCharacterIndex,
                    direction,
                    out caretStopIndex,
                    out offsetToNextCaretStopIndex
                    );

                if (!found)
                {
                    // The current index is before the 1st caret stop.
                    return characterHit;
                }

                if (    offsetToNextCaretStopIndex != 0
                    &&  characterHit.TrailingLength == 0
                    &&  caretStopIndex != _cpFirst
                    &&  caretStopIndex >= characterHit.FirstCharacterIndex
                    )
                {
                    // If the current character hit is at the leading edge and it is not at the first caret stop,
                    // move it to leading edge of the previous caret stop. At this point, the current character stop
                    // fully encloses the input index and the input is at the leading edge.
                    found = GetNextOrPreviousCaretStop(
                        caretStopIndex - 1, // position at the character immediately preceding the current caret stop
                        direction,
                        out caretStopIndex,
                        out offsetToNextCaretStopIndex
                        );

                    if (!found)
                    {
                        // The current index is before the 1st caret stop.
                        return characterHit;
                    }
                }

                // The current chracter hit is either beyond the last caret stop,
                // or it's at the trailing edge of the current caret stop,
                // or the current index is at the leading edge of the first caret stop.
                //
                // In such cases, move to the leading edge of the closest caret stop.
                return new CharacterHit(caretStopIndex, 0);
            }


            /// <summary>
            /// Given a specified current character index, calculate the character index
            /// to the closest caret stop before or at the current index; and the number
            /// of codepoints from the closest caret stop to the next caret stop.
            /// </summary>
            private bool GetNextOrPreviousCaretStop(
                int                      currentIndex,
                CaretDirection           direction,
                out int                  caretStopIndex,
                out int                  offsetToNextCaretStopIndex
                )
            {
                caretStopIndex = currentIndex;
                offsetToNextCaretStopIndex = 0;

                if (    HasCollapsed
                    &&  _collapsedRange != null
                    &&  currentIndex >= _collapsedRange.TextSourceCharacterIndex
                    )
                {
                    // current index is within collapsed range,
                    caretStopIndex = _collapsedRange.TextSourceCharacterIndex;

                    if (currentIndex < _collapsedRange.TextSourceCharacterIndex + _collapsedRange.Length)
                        offsetToNextCaretStopIndex = _collapsedRange.Length;

                    return true;
                }

                LsQSubInfo[] sublineInfo = new LsQSubInfo[_depthQueryMax];
                LsTextCell lsTextCell = new LsTextCell();

                int lscpVisisble = GetInternalCp(currentIndex);
                bool found = FindNextOrPreviousVisibleCp(lscpVisisble, direction, out lscpVisisble);

                if (!found)
                {
                    return false; // there is no caret stop anymore in the given direction.
                }

                int actualSublineCount;

                QueryLineCpPpoint(
                    lscpVisisble,
                    sublineInfo,
                    out actualSublineCount,
                    out lsTextCell
                    );

                // Locate the current caret stop
                caretStopIndex = GetExternalCp(lsTextCell.lscpStartCell);

                if (    actualSublineCount > 0
                    &&  lscpVisisble >= lsTextCell.lscpStartCell
                    &&  lscpVisisble <= lsTextCell.lscpEndCell
                    )
                {
                    // the last subline contains the run that owns the querying lscp
                    LSRun lsrun = GetRun((Plsrun)sublineInfo[actualSublineCount - 1].plsrun);

                    if (lsrun.IsHitTestable)
                    {
                        if (    lsrun.HasExtendedCharacter
                            ||  (direction != CaretDirection.Backspace && lsrun.NeedsCaretInfo)
                            )
                        {
                            // LsTextCell.lscpEndCell is the index to the last lscp still in the cell.
                            // The number of LSCP within the text cell is equal to the number of CP.
                            offsetToNextCaretStopIndex = lsTextCell.lscpEndCell + 1 - lsTextCell.lscpStartCell;
                        }
                        else
                        {
                            // caret stops before every codepoint
                            caretStopIndex = GetExternalCp(lscpVisisble);
                            offsetToNextCaretStopIndex = 1;
                        }
                    }
                    else
                    {
                        // run is not hit-testable, caret navigation is not allowed in the run,
                        // the next caret stop is therefore either at the end of the run or the end of the line whichever reached first.
                        offsetToNextCaretStopIndex = Math.Min(Length, lsrun.Length - caretStopIndex + lsrun.OffsetToFirstCp + _cpFirst);
                    }
                }

                return true;
            }

            /// <summary>
            /// Search from the given lscp (inclusive) towards the specified direction for the
            /// closest navigable cp. Return true is one such cp is found, false otherwise.
            /// </summary>
            private bool FindNextOrPreviousVisibleCp(
                int             lscp,
                CaretDirection  direction,
                out int         lscpVisisble
                )
            {
                lscpVisisble = lscp;

                SpanRider plsrunSpanRider = new SpanRider(_plsrunVector);

                if (direction == CaretDirection.Forward)
                {
                    while (lscpVisisble < _metrics._lscpLim)
                    {
                        plsrunSpanRider.At(lscpVisisble - _cpFirst);
                        LSRun run = GetRun((Plsrun) plsrunSpanRider.CurrentElement);

                        // When scanning forward, only trailine edges of visiable content are navigable.
                        if (run.IsVisible)
                        {
                            return true;
                        }

                        lscpVisisble += plsrunSpanRider.Length; // move to start of next span
                    }
                }
                else
                {
                    Debug.Assert(direction == CaretDirection.Backward || direction == CaretDirection.Backspace);

                    // lscpCurrent can be right after the end of the line, we snap it back to be at the end of the line.
                    lscpVisisble = Math.Min(lscpVisisble, _metrics._lscpLim - 1);
                    while (lscpVisisble >= _cpFirst)
                    {
                        plsrunSpanRider.At(lscpVisisble - _cpFirst);
                        LSRun run = GetRun((Plsrun) plsrunSpanRider.CurrentElement);

                        // When scanning backward, visiable content has caret stop at its leading edge.
                        if (run.IsVisible)
                        {
                            return true;
                        }

                        // When scanning backward, the newline sequence has caret stop at its leading edge.
                        if (run.IsNewline)
                        {
                            // set navigable cp at the start of newline sequence.
                            lscpVisisble = _cpFirst + plsrunSpanRider.CurrentSpanStart;
                            return true;
                        }

                        lscpVisisble = _cpFirst + plsrunSpanRider.CurrentSpanStart - 1; // move to the end of previous span
                    }
                }

                lscpVisisble = lscp;
                return false;
            }

            /// <summary>
            /// Create zerowidth bounds with line height
            /// </summary>
            private TextBounds[] CreateDegenerateBounds()
            {
                return new TextBounds[]
                {
                    new TextBounds(
                        new Rect(0, 0, 0, Height),
                        (RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight),
                        null    // runBounds
                        )
                };
            }


            /// <summary>
            /// Create bounds of collapsing symbol
            /// </summary>
            private TextBounds CreateCollapsingSymbolBounds()
            {
                Debug.Assert(_collapsingSymbol != null);

                return new TextBounds(
                    new Rect(Start + Width - _collapsingSymbol.Width, 0, _collapsingSymbol.Width, Height),
                    (RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight),
                    null
                    );
            }


            /// <summary>
            /// Client to get an array of bounding rectangles of a range of characters within a text line.
            /// </summary>
            /// <param name="firstTextSourceCharacterIndex">index of first character of specified range</param>
            /// <param name="textLength">number of characters of the specified range</param>
            /// <returns>an array of bounding rectangles.</returns>
            public override IList<TextBounds> GetTextBounds(
                int     firstTextSourceCharacterIndex,
                int     textLength
                )
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                if(textLength == 0)
                {
                    throw new ArgumentOutOfRangeException("textLength", SR.Get(SRID.ParameterMustBeGreaterThanZero));
                }

                if(textLength < 0)
                {
                    firstTextSourceCharacterIndex += textLength;
                    textLength = -textLength;
                }

                if(firstTextSourceCharacterIndex < _cpFirst)
                {
                    textLength += (firstTextSourceCharacterIndex - _cpFirst);
                    firstTextSourceCharacterIndex = _cpFirst;
                }

                if(firstTextSourceCharacterIndex > _cpFirst + _metrics._cchLength - textLength)
                {
                    textLength = (_cpFirst + _metrics._cchLength - firstTextSourceCharacterIndex);
                }

                if (_ploline.Value == IntPtr.Zero)
                {
                    return CreateDegenerateBounds();
                }

                Point position = new Point(0,0);


                // get first cp sublines & text cell

                int firstDepth;
                LsTextCell firstTextCell;
                LsQSubInfo[] firstSublines = new LsQSubInfo[_depthQueryMax];

                int lscpFirst = GetInternalCp(firstTextSourceCharacterIndex);

                QueryLineCpPpoint(
                    lscpFirst,
                    firstSublines,
                    out firstDepth,
                    out firstTextCell
                    );

                if(firstDepth <= 0)
                {
                    // this happens for empty line (line containing only EOP)
                    return CreateDegenerateBounds();
                }


                // get last cp sublines & text cell

                int lastDepth;
                LsTextCell lastTextCell;
                LsQSubInfo[] lastSublines = new LsQSubInfo[_depthQueryMax];

                int lscpEnd = GetInternalCp(firstTextSourceCharacterIndex + textLength - 1);

                QueryLineCpPpoint(
                    lscpEnd,
                    lastSublines,
                    out lastDepth,
                    out lastTextCell
                    );

                if(lastDepth <= 0)
                {
                    // This should never happen but if it does, we still cant throw here.
                    // We must return something even though it's a degenerate bounds or
                    // client hittesting code will just crash.
                    Debug.Assert(false);
                    return CreateDegenerateBounds();
                }

                // check if collapsing symbol is wholely selected
                bool collapsingSymbolSelected =
                    (   _collapsingSymbol != null
                    &&  _collapsedRange != null
                    &&  firstTextSourceCharacterIndex < _collapsedRange.TextSourceCharacterIndex
                    &&  firstTextSourceCharacterIndex + textLength - _collapsedRange.TextSourceCharacterIndex > _collapsedRange.Length / 2
                    );

                TextBounds[] bounds = null;
                ArrayList boundsList = null;

                // By default, if the hittested CP is visible, then we want cpFirst to hit
                // on the leading edge of the first visible cp, and cpEnd to hit on the trailing edge of the
                // last visible cp.
                bool isCpFirstTrailing = false;
                bool isCpEndTrailing = true;

                if (lscpFirst > firstTextCell.lscpEndCell)
                {
                   // when cpFirst is after the last visible cp, then it hits the trailing edge of that cp
                   isCpFirstTrailing = true;
                }

                if (lscpEnd < lastTextCell.lscpStartCell)
                {
                   // when cpEnd is before the first visible cp, then it hits the leading edge of that cp
                   isCpEndTrailing = false;
                }

                if (firstDepth == lastDepth && firstSublines[firstDepth - 1].lscpFirstSubLine == lastSublines[lastDepth - 1].lscpFirstSubLine)
                {
                    // first and last cp are within the same subline

                    int count = collapsingSymbolSelected ? 2 : 1;

                    bounds = new TextBounds[count];
                    bounds[0] =
                        new TextBounds(
                            LSRun.RectUV(
                                position,
                                new LSPOINT(
                                    LSLineUToParagraphU(
                                        GetDistanceInsideTextCell(
                                            lscpFirst,
                                            isCpFirstTrailing,
                                            firstSublines,
                                            firstDepth,
                                            ref firstTextCell
                                            ) + firstTextCell.pointUvStartCell.x
                                        ),
                                    0
                                    ),
                                new LSPOINT(
                                    LSLineUToParagraphU(
                                        GetDistanceInsideTextCell(
                                            lscpEnd,
                                            isCpEndTrailing,
                                            lastSublines,
                                            lastDepth,
                                            ref lastTextCell
                                            ) + lastTextCell.pointUvStartCell.x
                                        ),
                                    _metrics._height
                                    ),
                                this
                                ),
                            Convert.LsTFlowToFlowDirection(firstSublines[firstDepth - 1].lstflowSubLine),
                            CalculateTextRunBounds(lscpFirst, lscpEnd + 1)
                            );

                    if (count > 1)
                    {
                        bounds[1] = CreateCollapsingSymbolBounds();
                    }
                }
                else
                {
                    // first and last cp are not in the same subline.
                    boundsList = new ArrayList(2);

                    int lscpCurrent = lscpFirst;

                    // The hittested cp can be outside of the returned sublines when it is a hidden cp.
                    // We should not pass beyond the end of the returned sublines.
                    int lscpEndInSubline = Math.Min(
                        lscpEnd,
                        lastSublines[lastDepth - 1].lscpFirstSubLine + lastSublines[lastDepth - 1].lsdcpSubLine - 1
                        );

                    int currentDistance = GetDistanceInsideTextCell(
                        lscpFirst,
                        isCpFirstTrailing,
                        firstSublines,
                        firstDepth,
                        ref firstTextCell
                    ) + firstTextCell.pointUvStartCell.x;

                    int baseLevelDepth;

                    CollectTextBoundsToBaseLevel(
                        boundsList,
                        ref lscpCurrent,
                        ref currentDistance,
                        firstSublines,
                        firstDepth,
                        lscpEndInSubline,
                        out baseLevelDepth
                    );

                    if (baseLevelDepth < lastDepth)
                    {
                        CollectTextBoundsFromBaseLevel(
                            boundsList,
                            ref lscpCurrent,
                            ref currentDistance,
                            lastSublines,
                            lastDepth,
                            baseLevelDepth
                        );
                    }

                    // Collect the bounds from the start of the immediate enclosing subline of the last LSCP
                    // to the hittested text cell.
                    AddValidTextBounds(
                        boundsList,
                        new TextBounds(
                            LSRun.RectUV(
                                position,
                                new LSPOINT(
                                    LSLineUToParagraphU(currentDistance),
                                    0
                                ),
                                new LSPOINT(
                                    LSLineUToParagraphU(
                                        GetDistanceInsideTextCell(
                                            lscpEnd,
                                            isCpEndTrailing,
                                            lastSublines,
                                            lastDepth,
                                            ref lastTextCell
                                            ) + lastTextCell.pointUvStartCell.x
                                        ),
                                    _metrics._height
                                ),
                                this
                            ),
                            Convert.LsTFlowToFlowDirection(lastSublines[lastDepth - 1].lstflowSubLine),
                            CalculateTextRunBounds(lscpCurrent, lscpEnd + 1)
                        )
                    );
                }

                if (bounds == null)
                {
                    Debug.Assert(boundsList != null);
                    if (boundsList.Count > 0)
                    {
                        if (collapsingSymbolSelected)
                        {
                            // add one more for collapsed symbol
                            AddValidTextBounds(boundsList, CreateCollapsingSymbolBounds());
                        }

                        bounds = new TextBounds[boundsList.Count];
                        for (int i = 0; i < boundsList.Count; i++)
                        {
                            bounds[i] = (TextBounds)boundsList[i];
                        }
                    }
                    else
                    {
                        // No non-zerowidth bounds detected, fallback to the position of first cp
                        // This can happen if hidden run is hittest'd.

                        int u =  LSLineUToParagraphU(
                            GetDistanceInsideTextCell(
                                lscpFirst,
                                isCpFirstTrailing,
                                firstSublines,
                                firstDepth,
                                ref firstTextCell
                                ) + firstTextCell.pointUvStartCell.x
                            );

                        bounds = new TextBounds[]
                        {
                            new TextBounds(
                                LSRun.RectUV(
                                    position,
                                    new LSPOINT(u, 0),
                                    new LSPOINT(u, _metrics._height),
                                    this
                                    ),
                                Convert.LsTFlowToFlowDirection(firstSublines[firstDepth - 1].lstflowSubLine),
                                null
                                )
                        };
                    }
                }

                return bounds;
            }

            /// <summary>
            /// Base level is the highest subline's level that both sets of sublines have in common.
            /// This method starts collecting text bounds at the specified LSCP. The first bounds being
            /// collected is the one from the LSCP to the end of its immediate enclosing subline. The
            /// subsequent bounds are from the end of run to the end of subline of the lower level until
            /// it reaches the base level.
            /// </summary>
            private void CollectTextBoundsToBaseLevel(
                ArrayList    boundsList,
                ref int      lscpCurrent,
                ref int      currentDistance,
                LsQSubInfo[] sublines,
                int          sublineDepth,
                int          lscpEnd,
                out int      baseLevelDepth
                )
            {
                baseLevelDepth = sublineDepth;

                if (lscpEnd < sublines[sublineDepth - 1].lscpFirstSubLine + sublines[sublineDepth - 1].lsdcpSubLine)
                {
                    // The immedidate enclosing subline already contains the lscp end. It means we are already
                    // at base level.
                    return;
                }

                // Collect text bounds from the current lscp to the end of the immediate enclosing subline.
                AddValidTextBounds(
                    boundsList,
                    new TextBounds(
                        LSRun.RectUV(
                            new Point(0, 0),
                            new LSPOINT(LSLineUToParagraphU(currentDistance), 0),
                            new LSPOINT(
                                LSLineUToParagraphU(GetEndOfSublineDistance(sublines, sublineDepth - 1)),
                                _metrics._height
                            ),
                            this
                        ),
                        Convert.LsTFlowToFlowDirection(sublines[sublineDepth - 1].lstflowSubLine),
                        CalculateTextRunBounds(lscpCurrent, sublines[sublineDepth - 1].lscpFirstSubLine + sublines[sublineDepth - 1].lsdcpSubLine)
                    )
                );

                // Collect text bounds from end of run to the end of subline at lower levels until we reach the
                // common level. We reach common level when the subline at that level contains the lscpEnd.
                for (   baseLevelDepth = sublineDepth - 1;
                        baseLevelDepth > 0 && (lscpEnd >= sublines[baseLevelDepth - 1].lscpFirstSubLine + sublines[baseLevelDepth - 1].lsdcpSubLine);
                        baseLevelDepth--
                    )
                {
                    int sublineIndex = baseLevelDepth - 1;
                    AddValidTextBounds(
                        boundsList,
                        new TextBounds(
                            LSRun.RectUV(
                                new Point(0, 0),
                                new LSPOINT(
                                    LSLineUToParagraphU(GetEndOfRunDistance(sublines, sublineIndex)),
                                    0
                                ),
                                new LSPOINT(
                                    LSLineUToParagraphU(GetEndOfSublineDistance(sublines, sublineIndex)),
                                    _metrics._height
                                ),
                                this
                            ),
                            Convert.LsTFlowToFlowDirection(sublines[sublineIndex].lstflowSubLine),
                            CalculateTextRunBounds(
                                sublines[sublineIndex].lscpFirstRun + sublines[sublineIndex].lsdcpRun,
                                sublines[sublineIndex].lscpFirstSubLine + sublines[sublineIndex].lsdcpSubLine
                            )
                        )
                    );
                }

                // base level depth must be at least 1 because both cp at least share the main line.
                Invariant.Assert(baseLevelDepth >= 1);

                // Move the current LSCP and distance to the end of run on the base level subline.
                lscpCurrent = sublines[baseLevelDepth - 1].lscpFirstRun + sublines[baseLevelDepth - 1].lsdcpRun;
                currentDistance = GetEndOfRunDistance(sublines, baseLevelDepth - 1);
            }

            /// <summary>
            /// Base level is the highest subline's level that both sets of sublines have in common.
            /// This method starts collecting text bounds at the specified LSCP. The first bounds being collected
            /// is the one from the LSCP to the start of run at the base level subline. The subsequent bounds are
            /// from the start of the higher level subline to the start of the run within the same subline, until
            /// it reaches the immediate enclosing subline of the last LSCP.
            /// </summary>
            private void CollectTextBoundsFromBaseLevel(
                ArrayList    boundsList,
                ref int      lscpCurrent,
                ref int      currentDistance,
                LsQSubInfo[] sublines,
                int          sublineDepth,
                int          baseLevelDepth
                )
            {
                // lscpCurrent is after the run end of the 1st cp. It must not be in the run of the last cp
                // because the two runs don't overlap at above base level.
                Invariant.Assert(lscpCurrent <= sublines[baseLevelDepth - 1].lscpFirstRun);

                // Collect the text bounds from the LSCP to the start of run at the base level subline.
                AddValidTextBounds(
                    boundsList,
                    new TextBounds(
                        LSRun.RectUV(
                            new Point(0, 0),
                            new LSPOINT(LSLineUToParagraphU(currentDistance), 0),
                            new LSPOINT(
                                LSLineUToParagraphU(sublines[baseLevelDepth - 1].pointUvStartRun.x),
                                _metrics._height
                            ),
                            this
                        ),
                        Convert.LsTFlowToFlowDirection(sublines[baseLevelDepth - 1].lstflowSubLine),
                        CalculateTextRunBounds(lscpCurrent, sublines[baseLevelDepth - 1].lscpFirstRun)
                     )
                );

                // Collect text bounds from start of subline to start of run at higher level sublines until it
                // reaches the immediate enclosing subline of the last LSCP.
                for (int i = baseLevelDepth; i < sublineDepth - 1; i++)
                {
                    AddValidTextBounds(
                        boundsList,
                        new TextBounds(
                            LSRun.RectUV(
                                new Point(0, 0),
                                new LSPOINT(LSLineUToParagraphU(sublines[i].pointUvStartSubLine.x), 0),
                                new LSPOINT(
                                    LSLineUToParagraphU(sublines[i].pointUvStartRun.x),
                                    _metrics._height
                                ),
                                this
                            ),
                            Convert.LsTFlowToFlowDirection(sublines[i].lstflowSubLine),
                            CalculateTextRunBounds(
                                sublines[i].lscpFirstSubLine,
                                sublines[i].lscpFirstRun
                            )
                        )
                    );
                };

                // Move the current LSCP and distance to the start of the immediate enclosing subline.
                lscpCurrent = sublines[sublineDepth - 1].lscpFirstSubLine;
                currentDistance = sublines[sublineDepth - 1].pointUvStartSubLine.x;
            }

            /// <summary>
            /// Return the ending edge of the subline relative to its own flow direction
            /// </summary>
            private int GetEndOfSublineDistance(
                LsQSubInfo[] sublines,
                int          index
                )
            {
                return sublines[index].pointUvStartSubLine.x +
                    ( sublines[index].lstflowSubLine == sublines[0].lstflowSubLine ?
                          sublines[index].dupSubLine
                        : -sublines[index].dupSubLine
                    );
            }

            /// <summary>
            /// Return the ending edge of the run relative to its own flow direction.
            /// </summary>
            private int GetEndOfRunDistance(
                LsQSubInfo[] sublines,
                int          index
                )
            {
                return sublines[index].pointUvStartRun.x +
                    ( sublines[index].lstflowSubLine == sublines[0].lstflowSubLine ?
                          sublines[index].dupRun
                        : -sublines[index].dupRun
                    );
            }

            /// <summary>
            /// Add non-zero geometry bounds to the bounds list
            /// </summary>
            private void AddValidTextBounds(
                ArrayList      boundsList,
                TextBounds     bounds
                )
            {
                if (bounds.Rectangle.Width != 0 && bounds.Rectangle.Height != 0)
                {
                    boundsList.Add(bounds);
                }
            }



            /// <summary>
            /// Compute bounds of runs within the specified range of lscp
            /// </summary>
            private IList<TextRunBounds> CalculateTextRunBounds(int lscpFirst, int lscpEnd)
            {
                if (lscpEnd <= lscpFirst)
                {
                    // It is possible that we'll get a legitimate case when lscpFirst is
                    // actually greater. That's what happen when the client hittest a hidden
                    // run that follows a reverse block. Since it is a hidden run, LS has
                    // to yield the closest non-hidden place which may be the run preceding
                    // the hidden text. 
                    return null;
                }

                int lscp = lscpFirst;
                int cchLeft = lscpEnd - lscpFirst;
                SpanRider plsrunSpanRider = new SpanRider(_plsrunVector);

                Point position = new Point(0, 0);
                IList<TextRunBounds> boundsList = new List<TextRunBounds>(2);

                while(cchLeft > 0)
                {
                    plsrunSpanRider.At(lscp - _cpFirst);
                    Plsrun plsrun = (Plsrun)plsrunSpanRider.CurrentElement;
                    int cch = Math.Min(plsrunSpanRider.Length, cchLeft);

                    if(TextStore.IsContent(plsrun))
                    {
                        LSRun lsrun = GetRun(plsrun);

                        if(     lsrun.Type == Plsrun.Text
                            ||  lsrun.Type == Plsrun.InlineObject)
                        {
                            int cp = GetExternalCp(lscp);
                            int cchBounds = cch;

                            if (    HasCollapsed
                                &&  _collapsedRange != null
                                &&  cp <= _collapsedRange.TextSourceCharacterIndex
                                &&  cp + cchBounds >= _collapsedRange.TextSourceCharacterIndex
                                &&  cp + cchBounds < _collapsedRange.TextSourceCharacterIndex + _collapsedRange.Length)
                            {
                                // Limit the run bounds to only non-collapsed text,
                                // we deal with collapsed text separately as it might have different flow direction.
                                cchBounds = _collapsedRange.TextSourceCharacterIndex - cp;
                            }

                            if (cchBounds > 0)
                            {
                                TextRunBounds bounds = new TextRunBounds(
                                    LSRun.RectUV(
                                        position,
                                        new LSPOINT(
                                            LSLineUToParagraphU(
                                                DistanceFromCharacterHit(new CharacterHit(cp, 0))
                                                ),
                                            _metrics._baselineOffset - lsrun.BaselineOffset + lsrun.BaselineMoveOffset
                                            ),
                                        new LSPOINT(
                                            LSLineUToParagraphU(
                                                DistanceFromCharacterHit(new CharacterHit(cp + cchBounds - 1, 1))
                                                ),
                                            _metrics._baselineOffset - lsrun.BaselineOffset + lsrun.BaselineMoveOffset + lsrun.Height
                                            ),
                                        this
                                        ),
                                    cp,
                                    cp + cchBounds,
                                    lsrun.TextRun
                                    );
                                boundsList.Add(bounds);
                            }
                        }
                    }

                    cchLeft -= cch;
                    lscp += cch;
                }
                return boundsList.Count > 0 ? boundsList : null;
            }



            /// <summary>
            /// Client to get a collection of TextRun objects within a line
            /// </summary>
            public override IList<TextSpan<TextRun>> GetTextRunSpans()
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                if (_plsrunVector == null)
                {
                    // return empty textspan when the line doesn't contain text runs.
                    return Array.Empty<TextSpan<TextRun>>();
                }

                IList<TextSpan<TextRun>> lsrunList = new List<TextSpan<TextRun>>(2);

                TextRun lastTextRun = null;
                int cchAcc = 0;
                int cchLeft = _metrics._cchLength;

                for (int i = 0; i < _plsrunVector.Count && cchLeft > 0; i++)
                {
                    Span plsrunSpan = _plsrunVector[i];

                    int cch = CpCount(plsrunSpan);
                    cch = Math.Min(cch, cchLeft);

                    if (cch > 0)
                    {
                        TextRun textRun = ((LSRun)GetRun((Plsrun)plsrunSpan.element)).TextRun;
                        Debug.Assert(textRun != null);

                        if (lastTextRun != null && textRun != lastTextRun)
                        {
                            Debug.Assert(cchAcc > 0);
                            lsrunList.Add(new TextSpan<TextRun>(cchAcc, lastTextRun));
                            cchAcc = 0;
                        }

                        lastTextRun = textRun;
                        cchAcc += cch;
                        cchLeft -= cch;
                    }
                }

                if (lastTextRun != null)
                {
                    Debug.Assert(cchAcc > 0);
                    lsrunList.Add(new TextSpan<TextRun>(cchAcc, lastTextRun));
                }

                Debug.Assert(cchLeft == 0);
                return lsrunList;
            }


            /// <summary>
            /// Client to get IndexedGlyphRuns enumerable to enumerate each IndexedGlyphRun object
            /// in the line. Through IndexedGlyphRun client can obtain glyph information of
            /// a text source character.
            /// </summary>
            public override IEnumerable<IndexedGlyphRun> GetIndexedGlyphRuns()
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                IEnumerable<IndexedGlyphRun> result = null;

                if (_ploline.Value != System.IntPtr.Zero)
                {
                    TextFormatterContext context = _metrics._formatter.AcquireContext(
                        new DrawingState(null, new Point(0, 0), null, this),
                        _ploc.Value
                        );

                    //
                    // Kick off line enumeration
                    //
                    LsErr lserr = LsErr.None;

                    LSPOINT point = new LSPOINT(0, 0);
                    lserr = UnsafeNativeMethods.LoEnumLine(
                        _ploline.Value,   // line
                        false,      // reverse enumeration
                        false,      // geometry needed
                        ref point   // starting point
                        );

                    // result
                    result = context.IndexedGlyphRuns;

                    // get the exception in context before it is released
                    Exception callbackException = context.CallbackException;

                    // clear the context
                    context.ClearIndexedGlyphRuns();
                    context.Release();

                    if (lserr != LsErr.None)
                    {
                        if (callbackException != null)
                        {
                            // rethrow exception thrown in callbacks
                            throw callbackException;
                        }
                        else
                        {
                            // throw with LS error codes
                            TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.EnumLineFailure, lserr), lserr);
                        }
                    }
                }

                return result;
            }


            /// <summary>
            /// Client to acquire a state at the point where line is broken by line breaking process;
            /// can be null when the line ends by the ending of the paragraph. Client may pass this
            /// value back to TextFormatter as an input argument to TextFormatter.FormatLine when
            /// formatting the next line within the same paragraph.
            /// </summary>
            public override TextLineBreak GetTextLineBreak()
            {
                if ((_statusFlags & StatusFlags.IsDisposed) != 0)
                {
                    throw new ObjectDisposedException(SR.Get(SRID.TextLineHasBeenDisposed));
                }

                if ((_statusFlags & StatusFlags.HasCollapsed) != 0)
                {
                    // collapsed line has no line break state to transfer
                    return null;
                }

                return _metrics.GetTextLineBreak(IntPtr.Zero);
            }


            /// <summary>
            /// Client to get the number of whitespace characters at the end of the line.
            /// </summary>
            public override int TrailingWhitespaceLength
            {
                get
                {
                    // figure out number of trailing whitespaces

                    if(_metrics._textWidth == _metrics._textWidthAtTrailing)
                    {
                        // LS doesnt see any trailing space (last character in a line or character before
                        // EOP is not space). We count only the length of EOP run that we count as our
                        // trailing whitespace.
                        return _metrics._cchNewline;
                    }
                    else
                    {
                        // LS sees some trailing spaces (last character in a line or character before EOP
                        // is space). We calculate number of trailing spaces based on the cp following
                        // the last non-trailing space recognized by LS.
                        CharacterHit characterHit = CharacterHitFromDistance(_metrics._textWidthAtTrailing + _metrics._textStart);
                        return _cpFirst + _metrics._cchLength - characterHit.FirstCharacterIndex - characterHit.TrailingLength;
                    }
                }
            }


            /// <summary>
            /// Client to get the number of text source positions of this line
            /// </summary>
            public override int Length
            {
                get { return _metrics.Length; }
            }


            /// <summary>
            /// Client to get the number of characters following the last character
            /// of the line that may trigger reformatting of the current line.
            /// </summary>
            public override int DependentLength
            {
                get { return _metrics.DependentLength; }
            }


            /// <summary>
            /// Client to get the number of newline characters at line end
            /// </summary>
            public override int NewlineLength
            {
                get { return _metrics.NewlineLength; }
            }


            /// <summary>
            /// Client to get distance from paragraph start to line start
            /// </summary>
            public override double Start
            {
                get { return _metrics.Start; }
            }


            /// <summary>
            /// Client to get the total width of this line
            /// </summary>
            public override double Width
            {
                get { return _metrics.Width; }
            }


            /// <summary>
            /// Client to get the total width of this line including width of whitespace characters at the end of the line.
            /// </summary>
            public override double WidthIncludingTrailingWhitespace
            {
                get { return _metrics.WidthIncludingTrailingWhitespace; }
            }


            /// <summary>
            /// Client to get the height of the line
            /// </summary>
            public override double Height
            {
                get { return _metrics.Height; }
            }


            /// <summary>
            /// Client to get the height of the text (or other content) in the line; this property may differ from the Height
            /// property if the client specified the line height
            /// </summary>
            public override double TextHeight
            {
                get { return _metrics.TextHeight; }
            }


            /// <summary>
            /// Client to get the distance from top to baseline of this text line
            /// </summary>
            public override double Baseline
            {
                get { return _metrics.Baseline; }
            }


            /// <summary>
            /// Client to get the distance from the top of the text (or other content) to the baseline of this text line;
            /// this property may differ from the Baseline property if the client specified the line height
            /// </summary>
            public override double TextBaseline
            {
                get { return _metrics.TextBaseline; }
            }


            /// <summary>
            /// Client to get the distance from the before edge of line height
            /// to the baseline of marker of the line if any.
            /// </summary>
            public override double MarkerBaseline
            {
                get { return _metrics.MarkerBaseline; }
            }


            /// <summary>
            /// Client to get the overall height of the list items marker of the line if any.
            /// </summary>
            public override double MarkerHeight
            {
                get { return _metrics.MarkerHeight; }
            }


            /// <summary>
            /// Client to get the height of the actual black of the line
            /// </summary>
            public override double Extent
            {
                get
                {
                    CheckBoundingBox();
                    return _overhang.Extent;
                }
            }


            /// <summary>
            /// Client to get the distance covering all black preceding the leading edge of the line.
            /// </summary>
            public override double OverhangLeading
            {
                get
                {
                    CheckBoundingBox();
                    return _overhang.Leading;
                }
            }


            /// <summary>
            /// Client to get the distance covering all black following the trailing edge of the line.
            /// </summary>
            public override double OverhangTrailing
            {
                get
                {
                    CheckBoundingBox();
                    return _overhang.Trailing;
                }
            }


            /// <summary>
            /// Client to get the distance from the after edge of line height to the after edge of the extent of the line.
            /// </summary>
            public override double OverhangAfter
            {
                get
                {
                    CheckBoundingBox();
                    return _overhang.Extent - Height - _overhang.Before;
                }
            }


            /// <summary>
            /// Client to get a boolean value indicates whether content of the line overflows
            /// the specified paragraph width.
            /// </summary>
            public override bool HasOverflowed
            {
                get { return (_statusFlags & StatusFlags.HasOverflowed) != 0; }
            }


            /// <summary>
            /// Client to get a boolean value indicates whether a line has been collapsed
            /// </summary>
            public override bool HasCollapsed
            {
                get { return (_statusFlags & StatusFlags.HasCollapsed) != 0; }
            }


            /// <summary>
            /// Client to get a Boolean flag indicating whether the line is truncated in the
            /// middle of a word. This flag is set only when TextParagraphProperties.TextWrapping
            /// is set to TextWrapping.Wrap and a single word is longer than the formatting
            /// paragraph width. In such situation, TextFormatter truncates the line in the middle
            /// of the word to honor the desired behavior specified by TextWrapping.Wrap setting.
            /// </summary>
            public override bool IsTruncated
            {
                get { return (_statusFlags & StatusFlags.IsTruncated) != 0; }
            }


            /// <summary>
            /// Client to get the index of the first cp of the line.
            /// </summary>
            public int CpFirst
            {
                get { return _cpFirst; }
            }

            /// <summary>
            /// Text source of the main text for the line
            /// </summary>
            public TextSource TextSource
            {
                get { return _textSource; }
            }


            /// <summary>
            /// Wrapper to LoQueryLinePointPcp
            /// </summary>
            private void QueryLinePointPcp(
                Point               ptQuery,
                LsQSubInfo[]        subLineInfo,
                out int             actualDepthQuery,
                out LsTextCell      lsTextCell
                )
            {
                Debug.Assert(_ploline.Value != IntPtr.Zero);

                LsErr lserr = LsErr.None;
                lsTextCell = new LsTextCell();
                unsafe
                {
                    fixed(LsQSubInfo* plsqsubl = subLineInfo)
                    {
                        LSPOINT pt = new LSPOINT((int)ptQuery.X, (int)ptQuery.Y);
                        lserr = UnsafeNativeMethods.LoQueryLinePointPcp(
                            _ploline.Value,
                            ref pt,
                            subLineInfo.Length,
                            (System.IntPtr)plsqsubl,
                            out actualDepthQuery,
                            out lsTextCell
                            );
                    }
                }

                if(lserr != LsErr.None)
                {
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.QueryLineFailure, lserr), lserr);
                }

                if (lsTextCell.lscpEndCell < lsTextCell.lscpStartCell)
                {
                    // When hit-testing is done on a generated hyphen of a hyphenated word, LS can only tell
                    // the start LSCP and not the end LSCP. Argurably this is LS 








                    lsTextCell.lscpEndCell = lsTextCell.lscpStartCell;
                }
            }



            /// <summary>
            /// Wrapper to LoQueryLineCpPpoint
            /// </summary>
            private void QueryLineCpPpoint(
                int                 lscpQuery,
                LsQSubInfo[]        subLineInfo,
                out int             actualDepthQuery,
                out LsTextCell      lsTextCell
                )
            {
                Debug.Assert(_ploline.Value != IntPtr.Zero);

                LsErr lserr = LsErr.None;

                lsTextCell = new LsTextCell();

                // Never hit LS with any LSCP beyond its last, the result is unreliable and varies between drops.
                int lscpValidQuery = (lscpQuery < _metrics._lscpLim ? lscpQuery : _metrics._lscpLim - 1);

                unsafe
                {
                    fixed(LsQSubInfo* plsqsubl = subLineInfo)
                    {
                        lserr = UnsafeNativeMethods.LoQueryLineCpPpoint(
                            _ploline.Value,
                            lscpValidQuery,
                            subLineInfo.Length,
                            (System.IntPtr)plsqsubl,
                            out actualDepthQuery,
                            out lsTextCell
                            );
                    }
                }

                if(lserr != LsErr.None)
                {
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.QueryLineFailure, lserr), lserr);
                }

                if (lsTextCell.lscpEndCell < lsTextCell.lscpStartCell)
                {
                    // When hit-testing is done on a generated hyphen of a hyphenated word, LS can only tell
                    // the start LSCP and not the end LSCP. Argurably this is LS bug. In such situation they
                    // should assume the end LSCP being the last LSCP of the line.
                    //
                    // However our code assumes that LS must tell both and the text cell must have size greater
                    // than one codepoint. We count on that to reliably advance the caret position.
                    //
                    // We assume that the next caret stop in this case is always
                    // the next codepoint.
                    lsTextCell.lscpEndCell = lsTextCell.lscpStartCell;
                }
            }



            /// <summary>
            /// Convert a U distance relative to start of LS line to U distance relative
            /// to the leading edge of paragraph.
            /// </summary>
            /// <param name="u">a U distance relative to start of ploline</param>
            /// <returns>another U distance relative to paragraph start</returns>
            internal int LSLineUToParagraphU(int u)
            {
                return u + _metrics._paragraphToText - _metrics._textStart;
            }


            /// <summary>
            /// Convert a U distance relative to the leading edge of paragraph
            /// to U distance relative to start of LS line.
            /// </summary>
            /// <param name="u">a U distance relative to paragraph start</param>
            /// <returns>another U distance relative to start of ploline</returns>
            internal int ParagraphUToLSLineU(int u)
            {
                return u - _metrics._paragraphToText + _metrics._textStart;
            }

            internal int BaselineOffset
            {
                get { return _metrics._baselineOffset; }
            }

            internal int ParagraphWidth
            {
                get { return _paragraphWidth; }
            }

            internal double MinWidth
            {
                get { return _metrics._formatter.IdealToReal(_textMinWidthAtTrailing + _metrics._textStart, PixelsPerDip); }
            }

            internal bool RightToLeft
            {
                get { return (_statusFlags & StatusFlags.RightToLeft) != 0; }
            }

            internal TextFormatterImp Formatter
            {
                get { return _metrics._formatter; }
            }

            internal bool IsJustified
            {
                get { return (_statusFlags & StatusFlags.IsJustified) != 0; }
            }

            internal TextDecorationCollection TextDecorations
            {
                get { return _paragraphTextDecorations; }
            }

            internal Brush DefaultTextDecorationsBrush
            {
                get { return _defaultTextDecorationsBrush; }
            }

#if DEBUG
            internal FullTextState FullTextState
            {
                get { return _fullText; }
            }
            #endif


            private void BuildOverhang(Point origin, Rect boundingBox)
            {
                if(boundingBox.IsEmpty)
                {
                    _overhang.Leading = _overhang.Trailing = 0;
                    _overhang.Before = 0;
                    _overhang.Extent = 0;
                }
                else
                {
                    // Move the bounding box to the coordinate relative to the line drawing origin.
                    // The following computation of overhang values need to be done independent to
                    // drawing origin.
                    boundingBox.X -= origin.X;
                    boundingBox.Y -= origin.Y;

                    if (RightToLeft)
                    {
                        double paragraphWidth = _metrics._formatter.IdealToReal(_paragraphWidth, PixelsPerDip);

                        _overhang.Leading = paragraphWidth - Start - boundingBox.Right;
                        _overhang.Trailing = boundingBox.Left - (paragraphWidth - Start - Width);
                    }
                    else
                    {
                        _overhang.Leading = boundingBox.Left - Start;
                        _overhang.Trailing = Start + Width - boundingBox.Right;
                    }

                    _overhang.Extent = boundingBox.Bottom - boundingBox.Top;
                    _overhang.Before = -boundingBox.Top;
                }
            }


            /// <summary>
            /// Overhang metrics
            /// </summary>
            private struct Overhang
            {
                internal double  Leading;
                internal double  Trailing;
                internal double  Extent;
                internal double  Before;
            }


            #region lsrun/cp mapping

            /// <summary>
            /// Map text source CP to internal LSCP
            /// </summary>
            internal int GetInternalCp(int cp)
            {
                int lscp = _cpFirst;
                int cpTarget = cp;
                cp = lscp;

                foreach(Span span in _plsrunVector)
                {
                    int ccp = CpCount(span);

                    if(ccp > 0)
                    {
                        if(cp + ccp > cpTarget)
                        {
                            lscp += (ccp == span.length ? cpTarget - cp : 0);
                            break;
                        }

                        cp += ccp;
                    }

                    lscp += span.length;
                }
                return lscp;
            }


            /// <summary>
            /// Map internal LSCP to text source cp
            /// </summary>
            internal int GetExternalCp(int lscp)
            {
                if (lscp >= _metrics._lscpLim)
                {
                    if (_collapsedRange != null)
                        return _collapsedRange.TextSourceCharacterIndex;

                    return _cpFirst + _metrics._cchLength;
                }

                int offsetToFirstCp;

                SpanRider plsrunSpanRider = new SpanRider(_plsrunVector);

                // skip lscp until we find one with valid map
                do
                {
                    plsrunSpanRider.At(lscp - _cpFirst);
                    offsetToFirstCp = GetRun(((Plsrun)plsrunSpanRider.CurrentElement)).OffsetToFirstCp;
} while(offsetToFirstCp < 0 && ++lscp < _metrics._lscpLim);

                return offsetToFirstCp + lscp - plsrunSpanRider.CurrentSpanStart;
            }


            /// <summary>
            /// Count actual cp of an lsrun
            /// </summary>
            /// <param name="plsrunSpan">span of plsrun</param>
            /// <returns>lsrun actual cp</returns>
            internal int CpCount(Span plsrunSpan)
            {
                Plsrun plsrun = (Plsrun)plsrunSpan.element;

                plsrun = TextStore.ToIndex(plsrun);

                // Inline object, text or linebreak yields as many cp as what client specifies.
                // lsrun known only to LS e.g. reverse, yields no actual cp,
                if(plsrun >= Plsrun.FormatAnchor)
                {
                    LSRun lsrun = GetRun(plsrun);
                    return lsrun.Length;
                }
                return 0;
            }


            /// <summary>
            /// Get LSRun from plsrun
            /// </summary>
            internal LSRun GetRun(Plsrun plsrun)
            {
                ArrayList lsruns = _lsrunsMainText;

                if (TextStore.IsMarker(plsrun))
                {
                    lsruns = _lsrunsMarkerText;
                }

                plsrun = TextStore.ToIndex(plsrun);

                return (LSRun)(
                    TextStore.IsContent(plsrun) ?
                    lsruns[(int)(plsrun - Plsrun.FormatAnchor)] :
                    TextStore.ControlRuns[(int)plsrun]
                    );
            }

            #endregion
        }
    }
}

