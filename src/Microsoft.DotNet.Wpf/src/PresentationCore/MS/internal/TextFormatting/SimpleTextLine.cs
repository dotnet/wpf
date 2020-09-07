// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Light-weight implementation of TextLine
//
//

using System;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MS.Internal.Shaping;
using MS.Internal.FontCache;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Light-weight implementation of TextLine
    ///
    /// Support following functionalities
    ///    o    Non-complex script text metrics through font CMAP/HMTX
    ///    o    Multiple character formats, each limited to single font face
    ///    o    Simple text underlining for individual run (no averaging)
    ///
    /// In the event that either the incoming text or formatting is more
    /// complicated than what this implementation can handle. The .ctor
    /// simply stops and leaves this.Valid flag unset. The caller examines
    /// this flag and lets the full path takes over if needed.
    /// </summary>
    internal class SimpleTextLine : TextLine
    {
        private SimpleRun[]             _runs;                  // contained runs
        private int                     _cpFirst;               // line first cp
        private int                     _cpLength;              // all characters
        private int                     _cpLengthEOT;           // newline characters
        private double                  _widthAtTrailing;       // width excluding trailing space
        private double                  _width;                 // whole width
        private double                  _paragraphWidth;        // paragraph width
        private double                  _height;                // line height
        private double                  _offset;                // offset to the first character
        private int                     _idealOffsetUnRounded;  // unrounded offset to the first character in ideal units.
                                                                // This offset is not snapped to pixels in Display mode.
                                                                // The reason we use this variable is to achieve similar
                                                                // results to those obtained from full shaping.
                                                                // In computing the baseline origin, the full shaping
                                                                // path rounds the sum of the offsets.
                                                                // Thus we need to keep track of the unrounded offset using
                                                                // this variable.
        private double                  _baselineOffset;        // offset to baseline
        private int                     _trailing;              // trailing spaces
        private Rect                    _boundingBox;           // line bounding rectangle
        private StatusFlags             _statusFlags;           // status flags
        private FormatSettings          _settings;              // formatting settings (only kept in an overflowed line for collapsing purpose only)


        [Flags]
        private enum StatusFlags
        {
            None                = 0,
            BoundingBoxComputed = 0x00000001,   // bounding box has been computed
            HasOverflowed       = 0x00000002,   // line width overflows paragraph width
        }



        /// <summary>
        /// Creating a lightweight text line
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="cpFirst">First cp of the line</param>
        /// <param name="paragraphWidth">paragraph width</param>
        /// <returns>TextLine instance</returns>
        /// <remarks>
        /// This method breaks line using Ideal width such that it will be
        /// consistent with FullTextLine
        /// </remarks>
        static public TextLine  Create(
            FormatSettings          settings,
            int                     cpFirst,
            int                     paragraphWidth,
            double                  pixelsPerDip
            )
        {
            ParaProp pap = settings.Pap;

            if(    pap.RightToLeft
                || pap.Justify
                || (   pap.FirstLineInParagraph
                    && pap.TextMarkerProperties != null)
                || settings.TextIndent != 0
                || pap.ParagraphIndent != 0
                || pap.LineHeight > 0
                || pap.AlwaysCollapsible
                || (pap.TextDecorations != null && pap.TextDecorations.Count != 0)
                )
            {
                // unsupported paragraph properties
                return null;
            }

            int cp = cpFirst;
            int nonHiddenLength = 0;    // length of non-hidden runs seen so far

            // paragraphWidth == 0 means the format width is unlimited.
            int widthLeft = (pap.Wrap && paragraphWidth > 0) ? paragraphWidth : int.MaxValue;
            int idealRunOffsetUnRounded = 0;

            SimpleRun prev = null;

            SimpleRun run = SimpleRun.Create(
                settings,
                cp,
                cpFirst,
                widthLeft,
                paragraphWidth,
                idealRunOffsetUnRounded,
                pixelsPerDip
                );


            if(run == null)
            {
                // fail to create run e.g. complex content encountered
                return null;
            }
            else if(!run.EOT && run.IdealWidth <= widthLeft)
            {
                // create next run
                cp += run.Length;
                widthLeft               -= run.IdealWidth;
                idealRunOffsetUnRounded += run.IdealWidth;
                prev = run;

                run = SimpleRun.Create(
                    settings,
                    cp,
                    cpFirst,
                    widthLeft,
                    paragraphWidth,
                    idealRunOffsetUnRounded,
                    pixelsPerDip
                    );

                if(run == null)
                {
                    return null;
                }
            }


            int trailing = 0;
            ArrayList runs = new ArrayList(2);

            if(prev != null)
            {
                AddRun(runs, prev, ref nonHiddenLength);
            }

            do
            {
                if(!run.EOT && run.IdealWidth > widthLeft)
                {
                    // linebreaking required, even simple text requires classification-based linebreaking,
                    // we'll now let LS handle this line.
                    return null;
                }

                AddRun(runs, run, ref nonHiddenLength);

                // As a security mitigation, we impose a limit on the length of a single line
                // (see comments for TextStore.MaxCharactersPerLine) - only non-hidden
                // runs count against this limit.   If the line exceeds the limit,
                // use FullTextLine instead of SimpleTextLine - this assures consistency
                // in cases such as collapsing a line.
                if (nonHiddenLength >= TextStore.MaxCharactersPerLine)
                {
                    return null;
                }

                prev = run;
                cp += run.Length;
                widthLeft               -= run.IdealWidth;
                idealRunOffsetUnRounded += run.IdealWidth;

                if(run.EOT)
                {
                    // we're done
                    break;
                }

                run = SimpleRun.Create(
                    settings,
                    cp,
                    cpFirst,
                    widthLeft,
                    paragraphWidth,
                    idealRunOffsetUnRounded,
                    pixelsPerDip
                    );

                if(    run == null
                    || (   run.Underline != null
                        && prev != null
                        && prev.Underline != null
                        && !prev.IsUnderlineCompatible(run))
                    )
                {
                    // fail to create run or
                    // runs cannot support averaging underline
                    return null;
                }
} while(true);

            int trailingSpaceWidth = 0;

            CollectTrailingSpaces(
                runs,
                settings.Formatter,
                ref trailing,
                ref trailingSpaceWidth
                );

            // create a simple line
            return new SimpleTextLine(
                settings,
                cpFirst,
                paragraphWidth,
                runs,
                ref trailing,
                ref trailingSpaceWidth,
                pixelsPerDip
                ) as TextLine;
        }



        /// <summary>
        /// Constructing a lightweight text line
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="cpFirst">line first cp</param>
        /// <param name="paragraphWidth">paragraph width</param>
        /// <param name="runs">collection of simple runs</param>
        /// <param name="trailing">line trailing spaces</param>
        /// <param name="trailingSpaceWidth">line trailing spaces width</param>
        /// <Remarks>
        /// SimpleTextLine is constructed with Ideal width such that the line breaking
        /// behavior is consistent with the FullTextLine
        /// </Remarks>
        public SimpleTextLine(
            FormatSettings          settings,
            int                     cpFirst,
            int                     paragraphWidth,
            ArrayList               runs,
            ref int                 trailing,
            ref int                 trailingSpaceWidth,
            double pixelsPerDip
            ) : base(pixelsPerDip)
        {
            // Compute line metrics
            int count = 0;

            _settings = settings;

            double realAscent = 0;
            double realDescent = 0;
            double realHeight = 0;

            ParaProp pap = settings.Pap;
            TextFormatterImp formatter = settings.Formatter;

            int idealWidth = 0;
            while(count < runs.Count)
            {
                SimpleRun run = (SimpleRun)runs[count];

                if(run.Length > 0)
                {
                    if(run.EOT)
                    {
                        // EOT run has no effect on height, it is part of trailing spaces
                        trailing += run.Length;
                        _cpLengthEOT += run.Length;
                    }
                    else
                    {
                        realHeight = Math.Max(realHeight, run.Height);
                        realAscent = Math.Max(realAscent, run.Baseline);
                        realDescent = Math.Max(realDescent, run.Height - run.Baseline);
                    }

                    _cpLength += run.Length;
                    idealWidth += run.IdealWidth;
                }
                count++;
            }

            // Roundtrip run baseline and height to take its precision back to the specified formatting resolution.
            //
            // We have to do this to guarantee sameness of line alignment metrics produced by fast and full path.
            // This is critical for TextBlock/TextFlow. They rely on the fact that line created during Measure must
            // yield the same metrics as one created during Render, while there is no guarantee that the paragraph
            // properties of that same line remains the same in both timings e.g. Measure may not specify
            // justification (which results in us formatting the line in fast path), while Render might
            // (which results in us formatting that same line in full path).

            _baselineOffset = formatter.IdealToReal(TextFormatterImp.RealToIdeal(realAscent), PixelsPerDip);

            if (realAscent + realDescent == realHeight)
            {
                _height = formatter.IdealToReal(TextFormatterImp.RealToIdeal(realHeight), PixelsPerDip);
            }
            else
            {
                _height = formatter.IdealToReal(TextFormatterImp.RealToIdeal(realAscent) + TextFormatterImp.RealToIdeal(realDescent), PixelsPerDip);
            }

            if(_height <= 0)
            {
                //  line is empty (containing only EOP)
                //  we need to work out the line height

                // It needs to be exactly the same as in full path.
                _height = formatter.IdealToReal((int)Math.Round(pap.DefaultTypeface.LineSpacing(pap.EmSize, Constants.DefaultIdealToReal, PixelsPerDip, _settings.TextFormattingMode)), PixelsPerDip);
                _baselineOffset = formatter.IdealToReal((int)Math.Round(pap.DefaultTypeface.Baseline(pap.EmSize, Constants.DefaultIdealToReal, PixelsPerDip, _settings.TextFormattingMode)), PixelsPerDip);
            }

            // Initialize the array of runs and set the TrimTrailingUnderline flag
            // for runs that contain trailing spaces at the end of the line.
            _runs = new SimpleRun[count];
            for(int i = count - 1, t = trailing; i >= 0; --i)
            {
                SimpleRun run = (SimpleRun)runs[i];

                if (t > 0)
                {
                    run.TrimTrailingUnderline = true;
                    t -= run.Length;
                }

                _runs[i] = run;
            }

            _cpFirst = cpFirst;
            _trailing = trailing;

            int idealWidthAtTrailing = idealWidth - trailingSpaceWidth;

            if(pap.Align != TextAlignment.Left)
            {
                switch(pap.Align)
                {
                    case TextAlignment.Right:
                        _idealOffsetUnRounded = paragraphWidth - idealWidthAtTrailing;
                        _offset = formatter.IdealToReal(_idealOffsetUnRounded, PixelsPerDip);
                        break;
                    case TextAlignment.Center:
                        // exactly consistent with FullTextLine
                        _idealOffsetUnRounded = (int)Math.Round((paragraphWidth - idealWidthAtTrailing) * 0.5);
                        _offset = formatter.IdealToReal(_idealOffsetUnRounded, PixelsPerDip);
                        break;
                }
            }

            // converting all the ideal values to real values
            _width = formatter.IdealToReal(idealWidth, PixelsPerDip);
            _widthAtTrailing = formatter.IdealToReal(idealWidthAtTrailing, PixelsPerDip);
            _paragraphWidth = formatter.IdealToReal(paragraphWidth, PixelsPerDip);

            // paragraphWidth == 0 means format width is unlimited and hence not overflowable.
            // we keep paragraphWidth for alignment calculation
            if (paragraphWidth > 0 && _widthAtTrailing > _paragraphWidth)
            {
                _statusFlags |= StatusFlags.HasOverflowed;
            }
        }


        /// <summary>
        /// Nothing to release
        /// </summary>
        public override void Dispose() {}


        /// <summary>
        /// Scanning the run list backward to collect run's trailing spaces.
        /// </summary>
        /// <param name="runs">current runs in the line</param>
        /// <param name="formatter">formatter</param>
        /// <param name="trailing">trailing spaces</param>
        /// <param name="trailingSpaceWidth">trailing spaces width in ideal values</param>
        static private void CollectTrailingSpaces(
            ArrayList           runs,
            TextFormatterImp    formatter,
            ref int             trailing,
            ref int             trailingSpaceWidth
            )
        {
            int left = runs != null ? runs.Count : 0;

            SimpleRun run = null;
            bool continueCollecting = true;

            while(left > 0 && continueCollecting)
            {
                run = (SimpleRun)runs[--left];

                continueCollecting = run.CollectTrailingSpaces(
                    formatter,
                    ref trailing,
                    ref trailingSpaceWidth
                    );
            }
        }


        /// <summary>
        /// Collecting glyph runs
        /// </summary>
        static private void AddRun(
            ArrayList       runs,
            SimpleRun       run,
            ref int         nonHiddenLength
            )
        {
            if(run.Length > 0)
            {
                // dont add 0-length run
                runs.Add(run);

                if (!run.Ghost)
                {
                    nonHiddenLength += run.Length;
                }
            }
        }



        /// <summary>
        /// Get distance from line start to the specified cp
        /// </summary>
        private double DistanceFromCp(int currentIndex)
        {
            Invariant.Assert(currentIndex >= _cpFirst);

            int idealAdvance = 0;
            int dcp = currentIndex - _cpFirst;

            foreach(SimpleRun run in _runs)
            {
                idealAdvance += run.DistanceFromDcp(dcp);

                if(dcp <= run.Length)
                {
                    break;
                }

                dcp -= run.Length;
            }

            return _settings.Formatter.IdealToReal(idealAdvance + _idealOffsetUnRounded, PixelsPerDip);
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

            MatrixTransform antiInversion = TextFormatterImp.CreateAntiInversionTransform(
                inversion,
                _paragraphWidth,
                _height
                );

            if (antiInversion == null)
            {
                DrawTextLine(drawingContext, origin);
            }
            else
            {
                // Apply anti-inversion transform to correct the visual
                drawingContext.PushTransform(antiInversion);
                try
                {
                    DrawTextLine(drawingContext, origin);
                }
                finally
                {
                    drawingContext.Pop();
                }
            }
        }



        /// <summary>
        /// Client to collapse the line to fit for display
        /// </summary>
        /// <param name="collapsingPropertiesList">a list of collapsing properties</param>
        public override TextLine Collapse(
            params TextCollapsingProperties[]   collapsingPropertiesList
            )
        {
            if (!HasOverflowed)
                return this;

            Invariant.Assert(_settings != null);

            // instantiate a collapsible full text line, collapse it and return the collapsed line
            TextMetrics.FullTextLine textLine = new TextMetrics.FullTextLine(
                _settings,
                _cpFirst,
                0,  // lineLength
                TextFormatterImp.RealToIdeal(_paragraphWidth),
                LineFlags.None
                );

            // When in TextFormattingMode.Display the math processing performed by SimpleTextLine 
            // involves some rounding operations because of which the decision to collapse the text may 
            // not be unanimous amongst SimpleTextLine and FullTextLine. There are several watson 
            // crash reports that are testament to this theory. Hence we 
            // now allow the case where FullTextLine concludes that it doesnt need to collapse the 
            // text even though SimpleTextLine thought it should.

            if (textLine.HasOverflowed)
            {
                TextLine collapsedTextLine = textLine.Collapse(collapsingPropertiesList);
                if (collapsedTextLine != textLine)
                {
                    // if collapsed line is genuinely new,
                    // Dispose its maker as we no longer need it around, dispose it explicitly
                    // to reduce unnecessary finalization of this intermediate line.
                    textLine.Dispose();
                }
                return collapsedTextLine;
            }

            return textLine;
        }


        /// <summary>
        /// Make sure the bounding box is calculated
        /// </summary>
        private void CheckBoundingBox()
        {
            if ((_statusFlags & StatusFlags.BoundingBoxComputed) == 0)
            {
                DrawTextLine(null, new Point(0, 0));
            }
            Debug.Assert((_statusFlags & StatusFlags.BoundingBoxComputed) != 0);
        }


        /// <summary>
        /// Draw a simple text line
        /// </summary>
        /// <returns>a drawing bounding box</returns>
        private void DrawTextLine(
            DrawingContext drawingContext,
            Point          origin
            )
        {
            if (_runs.Length <= 0)
            {
                _boundingBox = Rect.Empty;
                _statusFlags |= StatusFlags.BoundingBoxComputed;
                return;
            }

            int idealXRelativeToOrigin = _idealOffsetUnRounded;
            double y = origin.Y + Baseline;

            if (drawingContext != null)
            {
                drawingContext.PushGuidelineY1(y);
            }

            Rect boundingBox = Rect.Empty;

            try
            {
                foreach (SimpleRun run in _runs)
                {
                    boundingBox.Union(
                        run.Draw(
                            drawingContext,
                            _settings.Formatter.IdealToReal(idealXRelativeToOrigin, PixelsPerDip) + origin.X,
                            y,
                            false
                            )
                        );

                    idealXRelativeToOrigin += run.IdealWidth;
                }
            }
            finally
            {
                if (drawingContext != null)
                {
                    drawingContext.Pop();
                }
            }

            if(boundingBox.IsEmpty)
            {
                boundingBox = new Rect(Start, 0, 0, 0);
            }
            else
            {
                boundingBox.X -= origin.X;
                boundingBox.Y -= origin.Y;
            }

            _boundingBox = boundingBox;
            _statusFlags |= StatusFlags.BoundingBoxComputed;
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
            int idealAdvance = TextFormatterImp.RealToIdeal(distance) - _idealOffsetUnRounded;
            int first = _cpFirst;

            if (idealAdvance < 0)
            {
                // hit happens before the line, return the first position
                return new CharacterHit(_cpFirst, 0);
            }

            // process hit that happens within the line
            SimpleRun run = null;
            CharacterHit runIndex = new CharacterHit();

            for(int i = 0; i < _runs.Length;  i++)
            {
                run = (SimpleRun)_runs[i];

                if (!run.EOT)
                {
                    // move forward to start of next non-EOT run
                    first += runIndex.TrailingLength;
                    runIndex = run.DcpFromDistance(idealAdvance);
                    first += runIndex.FirstCharacterIndex;
                }

                if (idealAdvance <= run.IdealWidth)
                {
                    break;
                }

                idealAdvance -= run.IdealWidth;
            }
            return new CharacterHit(first, runIndex.TrailingLength);
        }


        /// <summary>
        /// Client to get the distance from the beginning of the line from the specified
        /// character hit.
        /// </summary>
        /// <param name="characterHit">character hit of the character to query the distance.</param>
        /// <returns>distance in text flow direction from the beginning of the line.</returns>
        public override double GetDistanceFromCharacterHit(
            CharacterHit    characterHit
            )
        {
            TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _cpLength);
            return DistanceFromCp(characterHit.FirstCharacterIndex + (characterHit.TrailingLength != 0 ? 1 : 0));
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
            TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _cpLength);

            int nextVisisbleCp;
            bool navigableCpFound;
            if (characterHit.TrailingLength == 0)
            {
                navigableCpFound = FindNextVisibleCp(characterHit.FirstCharacterIndex, out nextVisisbleCp);
                if (navigableCpFound)
                {
                    // Move from leading to trailing edge
                    return new CharacterHit(nextVisisbleCp, 1);
                }
            }

            navigableCpFound = FindNextVisibleCp(characterHit.FirstCharacterIndex + 1, out nextVisisbleCp);
            if (navigableCpFound)
            {
                // Move from trailing edge of current character to trailing edge of next
                return new CharacterHit(nextVisisbleCp, 1);
            }

            // Can't move, we're after the last character
            return characterHit;
        }


        /// <summary>
        /// Client to get the previous character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the previous character hit</returns>
        public override CharacterHit GetPreviousCaretCharacterHit(
            CharacterHit    characterHit
            )
        {
            TextFormatterImp.VerifyCaretCharacterHit(characterHit, _cpFirst, _cpLength);
            int previousVisisbleCp;
            bool navigableCpFound;

            int cpHit = characterHit.FirstCharacterIndex;
            bool trailingHit = (characterHit.TrailingLength != 0);

            // Input can be right after the end of the current line. Snap it to be at the end of the line.
            if (cpHit >= _cpFirst + _cpLength)
            {
                cpHit = _cpFirst + _cpLength - 1;
                trailingHit = true;
            }

            if (trailingHit)
            {
                navigableCpFound = FindPreviousVisibleCp(cpHit, out previousVisisbleCp);
                if (navigableCpFound)
                {
                    // Move from trailing to leading edge
                    return new CharacterHit(previousVisisbleCp, 0);
                }
            }

            navigableCpFound = FindPreviousVisibleCp(cpHit - 1, out previousVisisbleCp);
            if (navigableCpFound)
            {
                // Move from leading edge of current character to leading edge of previous
                return new CharacterHit(previousVisisbleCp, 0);
            }

            // Can't move, we're before the first character
            return characterHit;
        }


        /// <summary>
        /// Client to get the previous character hit after backspacing
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the character hit after backspacing</returns>
        public override CharacterHit GetBackspaceCaretCharacterHit(
            CharacterHit    characterHit
            )
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
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
            if (textLength == 0)
            {
                throw new ArgumentOutOfRangeException("textLength", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            if (textLength < 0)
            {
                firstTextSourceCharacterIndex += textLength;
                textLength = -textLength;
            }

            if (firstTextSourceCharacterIndex < _cpFirst)
            {
                textLength += (firstTextSourceCharacterIndex - _cpFirst);
                firstTextSourceCharacterIndex = _cpFirst;
            }

            if (firstTextSourceCharacterIndex + textLength > _cpFirst + _cpLength)
            {
                textLength = _cpFirst + _cpLength - firstTextSourceCharacterIndex;
            }


            double x1 = GetDistanceFromCharacterHit(
                new CharacterHit(firstTextSourceCharacterIndex, 0)
                );

            double x2 = GetDistanceFromCharacterHit(
                new CharacterHit(firstTextSourceCharacterIndex + textLength, 0)
                );

            IList<TextRunBounds> boundsList = null;
            int dcp = firstTextSourceCharacterIndex - _cpFirst;
            int ich = 0;

            boundsList = new List<TextRunBounds>(2);

            foreach(SimpleRun run in _runs)
            {
                if(     !run.EOT
                    &&  !run.Ghost
                    &&  ich + run.Length > dcp)
                {
                    if(ich >= dcp + textLength)
                        break;

                    int first = Math.Max(ich, dcp) + _cpFirst;
                    int afterLast = Math.Min(ich + run.Length, dcp + textLength) + _cpFirst;

                    boundsList.Add(
                        new TextRunBounds(
                            new Rect(
                                new Point(
                                    DistanceFromCp(first),
                                    _baselineOffset - run.Baseline
                                    ),
                                new Point(
                                    DistanceFromCp(afterLast),
                                    _baselineOffset - run.Baseline + run.Height
                                    )
                                ),
                            first,
                            afterLast,
                            run.TextRun
                            )
                        );
                }
                ich += run.Length;
            }

            return new TextBounds[]
            {
                new TextBounds(
                    new Rect(
                        x1,
                        0,
                        x2 - x1,
                        _height
                        ),
                    FlowDirection.LeftToRight,
                    (boundsList == null || boundsList.Count == 0 ? null : boundsList)
                )
            };
        }


        /// <summary>
        /// Client to get a collection of TextRun objects within a line
        /// </summary>
        public override IList<TextSpan<TextRun>> GetTextRunSpans()
        {
            TextSpan<TextRun>[] textRunSpans = new TextSpan<TextRun>[_runs.Length];

            for (int i = 0; i < _runs.Length; i++)
            {
                textRunSpans[i] = new TextSpan<TextRun>(_runs[i].Length, _runs[i].TextRun);
            }

            return textRunSpans;
        }

        /// <summary>
        /// Client to get a IEnumerable&lt;IndexedGlyphRun&gt; to enumerate GlyphRuns
        /// within in a line
        /// </summary>
        public override IEnumerable<IndexedGlyphRun> GetIndexedGlyphRuns()
        {
            List<IndexedGlyphRun> indexedGlyphRuns = new List<IndexedGlyphRun>(_runs.Length);

            // create each GlyphRun at Point(0, 0)
            Point start = new Point(0, 0);
            int currentCp = _cpFirst;

            foreach(SimpleRun run in _runs)
            {
                if (run.Length > 0 && !run.Ghost)
                {
                    IList<double> displayGlyphAdvances;

                    if (_settings.TextFormattingMode == TextFormattingMode.Ideal)
                    {
                        displayGlyphAdvances = new ThousandthOfEmRealDoubles(run.EmSize, run.NominalAdvances.Length);
                        for (int i = 0; i < displayGlyphAdvances.Count; i++)
                        {
                            // convert ideal glyph advance width to real width for displaying
                            displayGlyphAdvances[i] = _settings.Formatter.IdealToReal(run.NominalAdvances[i], PixelsPerDip);
                        }
                    }
                    else
                    {
                        displayGlyphAdvances = new List<double>(run.NominalAdvances.Length);
                        for (int i = 0; i < run.NominalAdvances.Length; i++)
                        {
                            // convert ideal glyph advance width to real width for displaying
                            displayGlyphAdvances.Add(_settings.Formatter.IdealToReal(run.NominalAdvances[i], PixelsPerDip));
                        }
                    }



                    GlyphTypeface glyphTypeface = run.Typeface.TryGetGlyphTypeface();
                    Invariant.Assert(glyphTypeface != null);

                    // this simple run has GlyphRun
                    GlyphRun glyphRun = glyphTypeface.ComputeUnshapedGlyphRun(
                        start,
                        new CharacterBufferRange(run.CharBufferReference, run.Length),
                        displayGlyphAdvances,
                        run.EmSize,
                        (float)PixelsPerDip,
                        run.TextRun.Properties.FontHintingEmSize,
                        run.Typeface.NullFont,
                        CultureMapper.GetSpecificCulture(run.TextRun.Properties.CultureInfo),
                        null,   // device font name
                        _settings.TextFormattingMode
                        );

                    if (glyphRun != null)
                    {
                        indexedGlyphRuns.Add(
                            new IndexedGlyphRun(
                                currentCp,
                                run.Length,
                                glyphRun
                            )
                         );
                    }
                }

                currentCp += run.Length;
            }

            return indexedGlyphRuns;
        }


        /// <summary>
        /// Client to acquire a settings at the point where line is broken by line breaking process;
        /// can be null when the line ends by the ending of the paragraph. Client may pass this
        /// value back to TextFormatter as an input argument to TextFormatter.FormatLine when
        /// formatting the next line within the same paragraph.
        /// </summary>
        public override TextLineBreak GetTextLineBreak()
        {
            // No line break implemented in simple text
            return null;
        }


        /// <summary>
        /// Client to get a collection of collapsed character ranges after a line has been collapsed
        /// </summary>
        public override IList<TextCollapsedRange> GetTextCollapsedRanges()
        {
            // A collapsed line is never implemented as simple text line
            Invariant.Assert(!HasCollapsed);
            return null;
        }

        /// <summary>
        /// Client to get the number of text source positions of this line
        /// </summary>
        public override int Length
        {
            get { return _cpLength; }
        }


        /// <summary>
        /// Client to get the number of whitespace characters at the end of the line.
        /// </summary>
        public override int TrailingWhitespaceLength
        {
            get { return _trailing; }
        }


        /// <summary>
        /// Client to get the number of characters following the last character
        /// of the line that may trigger reformatting of the current line.
        /// </summary>
        public override int DependentLength
        {
            get { return 0; }
        }


        /// <summary>
        /// Client to get the number of newline characters at line end
        /// </summary>
        public override int NewlineLength
        {
            get { return _cpLengthEOT; }
        }


        /// <summary>
        /// Client to get distance from paragraph start to line start
        /// </summary>
        public override double Start
        {
            get { return _offset; }
        }


        /// <summary>
        /// Client to get the total width of this line
        /// </summary>
        public override double Width
        {
            get { return _widthAtTrailing; }
        }


        /// <summary>
        /// Client to get the total width of this line including width of whitespace characters at the end of the line.
        /// </summary>
        public override double WidthIncludingTrailingWhitespace
        {
            get { return _width; }
        }


        /// <summary>
        /// Client to get the height of the line
        /// </summary>
        public override double Height
        {
            get { return _height; }
        }


        /// <summary>
        /// Client to get the height of the text (or other content) in the line; this property may differ from the Height
        /// property if the client specified the line height
        /// </summary>
        public override double TextHeight
        {
            // simple path assumes no client-specified line height, i.e., TextParagraphProperties.LineHeight <= 0
            get { return _height; }
        }


        /// <summary>
        /// Client to get the height of the actual black of the line
        /// </summary>
        public override double Extent
        {
            get
            {
                CheckBoundingBox();
                return _boundingBox.Bottom - _boundingBox.Top;
            }
        }


        /// <summary>
        /// Client to get the distance from top to baseline of this text line
        /// </summary>
        public override double Baseline
        {
            get { return _baselineOffset; }
        }


        /// <summary>
        /// Client to get the distance from the top of the text (or other content) to the baseline of this text line;
        /// this property may differ from the Baseline property if the client specified the line height
        /// </summary>
        public override double TextBaseline
        {
            // simple path assumes no client-specified line height, i.e., TextParagraphProperties.LineHeight <= 0
            get { return _baselineOffset; }
        }


        /// <summary>
        /// Client to get the distance from the before edge of line height
        /// to the baseline of marker of the line if any.
        /// </summary>
        public override double MarkerBaseline
        {
            get { return Baseline; }
        }


        /// <summary>
        /// Client to get the overall height of the list items marker of the line if any.
        /// </summary>
        public override double MarkerHeight
        {
            get { return Height; }
        }


        /// <summary>
        /// Client to get the distance covering all black preceding the leading edge of the line.
        /// </summary>
        public override double OverhangLeading
        {
            get
            {
                CheckBoundingBox();
                return _boundingBox.Left - Start;
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
                return Start + Width - _boundingBox.Right;
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
                return _boundingBox.Bottom - Height;
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
            // A collapsed line is never implemented as simple text line
            get { return false; }
        }

        /// <summary>
        /// Search forward from the given cp index (inclusive) to find the next navigable cp index.
        /// Return true if one such cp is found, false otherwise.
        /// </summary>
        private bool FindNextVisibleCp(int cp, out int cpVisible)
        {
            cpVisible = cp;
            if (cp >= _cpFirst + _cpLength)
            {
                return false; // Cannot go forward anymore
            }

            int cpRunStart, runIndex;
            GetRunIndexAtCp(cp, out runIndex, out cpRunStart);

            while (runIndex < _runs.Length)
            {
                // When navigating forward, only the trailing edge of visible content is
                // navigable.
                if (_runs[runIndex].IsVisible && !_runs[runIndex].EOT)
                {
                    cpVisible = Math.Max(cpRunStart, cp);
                    return true;
                }

                cpRunStart += _runs[runIndex++].Length;
            }

            return false;
        }

        /// <summary>
        /// Search backward from the given cp index (inclusive) to find the previous navigable cp index.
        /// Return true if one such cp is found, false otherwise.
        /// </summary>
        private bool FindPreviousVisibleCp(int cp, out int cpVisible)
        {
            cpVisible = cp;
            if (cp < _cpFirst)
            {
                return false; // Cannot go backward anymore.
            }

            int cpRunEnd, runIndex;
            // Position the cpRunEnd at the end of the span that contains the given cp
            GetRunIndexAtCp(cp, out runIndex, out cpRunEnd);
            cpRunEnd += _runs[runIndex].Length - 1;

            while (runIndex >= 0)
            {
                // Visible content has caret stops at its leading edge.
                if (_runs[runIndex].IsVisible && !_runs[runIndex].EOT)
                {
                    cpVisible = Math.Min(cpRunEnd, cp);
                    return true;
                }

                // Newline sequence has caret stops at its leading edge.
                if (_runs[runIndex].EOT)
                {
                    // Get the cp index at the beginning of the newline sequence.
                    cpVisible = cpRunEnd - _runs[runIndex].Length + 1;
                    return true;
                }

                cpRunEnd -= _runs[runIndex--].Length;
            }

            return false;
        }

        private void GetRunIndexAtCp(
            int cp,
            out int runIndex,
            out int cpRunStart
            )
        {
            Invariant.Assert(cp >= _cpFirst && cp < _cpFirst + _cpLength);
            cpRunStart= _cpFirst;
            runIndex = 0;

            // Find the span that contains the given cp
            while (runIndex < _runs.Length && cpRunStart + _runs[runIndex].Length <= cp)
            {
                cpRunStart += _runs[runIndex++].Length;
            }
        }
    }


    /// <summary>
    /// Simple text run
    /// </summary>
    internal sealed class SimpleRun
    {
        public CharacterBufferReference CharBufferReference;    // character buffer reference
        public int                      Length;                 // CP length
        public int[]                    NominalAdvances;        // nominal glyph advance widths in ideal units
        public int                      IdealWidth;             // Ideal width of the line. Use ideal width to be consistent with FullTextLine in linebreaking
        public TextRun                  TextRun;                // text run
        public TextDecoration           Underline;              // only support single underline
        public Flags                    RunFlags;               // run flags

        private TextFormatterImp _textFormatterImp;
        private double _pixelsPerDip;

        [Flags]
        internal enum Flags : ushort
        {
            None                  = 0,
            EOT                   = 0x0001,   // end-of-text mark
            Ghost                 = 0x0002,   // non-existence run - only consume cp
            TrimTrailingUnderline = 0x0004,   // trailing whitespace should not be underlined
            Tab                   = 0x0008,   // run representing Tab character
        }

        internal bool EOT
        {
            get { return (RunFlags & Flags.EOT) != 0; }
        }

        internal bool Ghost
        {
            get { return (RunFlags & Flags.Ghost) != 0; }
        }

        internal bool Tab
        {
            get { return (RunFlags & Flags.Tab) != 0; }
        }

        internal bool TrimTrailingUnderline
        {
            get { return (RunFlags & Flags.TrimTrailingUnderline) != 0; }
            set
            {
                if (value)
                {
                    RunFlags |= Flags.TrimTrailingUnderline;
                }
                else
                {
                    RunFlags &= ~Flags.TrimTrailingUnderline;
                }
            }
        }

        internal double Baseline
        {
            get
            {
                if (Ghost || EOT)
                    return 0;

                return TextRun.Properties.Typeface.Baseline(TextRun.Properties.FontRenderingEmSize, 1, _pixelsPerDip, _textFormatterImp.TextFormattingMode);
            }
        }

        internal double Height
        {
            get
            {
                if (Ghost || EOT)
                    return 0;

                return TextRun.Properties.Typeface.LineSpacing(TextRun.Properties.FontRenderingEmSize, 1, _pixelsPerDip, _textFormatterImp.TextFormattingMode);
            }
        }

        internal Typeface Typeface
        {
            get { return TextRun.Properties.Typeface; }
        }

        internal double EmSize
        {
            get { return TextRun.Properties.FontRenderingEmSize; }
        }

        internal bool IsVisible
        {
            get { return this.TextRun is TextCharacters; }
        }

        internal SimpleRun(TextFormatterImp textFormatterImp, double pixelsPerDip)
        {
            _textFormatterImp = textFormatterImp;
            _pixelsPerDip = pixelsPerDip;
        }


        /// <summary>
        /// Creating a simple text run
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="cp">first cp of the run</param>
        /// <param name="cpFirst">first cp of the line</param>
        /// <param name="widthLeft">maxium run width</param>
        /// <param name="widthMax">maximum column width</param>
        /// <param name="idealRunOffsetUnRounded">run's offset from the beginning of the line</param>
        /// <returns>a SimpleRun object</returns>
        static public SimpleRun Create(
            FormatSettings          settings,
            int                     cp,
            int                     cpFirst,
            int                     widthLeft,
            int                     widthMax,
            int                     idealRunOffsetUnRounded,
            double                  pixelsPerDip
            )
        {
            TextRun textRun;
            int runLength;

            CharacterBufferRange charBufferRange = settings.FetchTextRun(
                cp,
                cpFirst,
                out textRun,
                out runLength
                );

            return Create(
                settings,
                charBufferRange,
                textRun,
                cp,
                cpFirst,
                runLength,
                widthLeft,
                idealRunOffsetUnRounded,
                pixelsPerDip
                );
        }



        /// <summary>
        /// Creating a simple text run
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="charString">character string associated to textrun</param>
        /// <param name="textRun">text run</param>
        /// <param name="cp">first cp of the run</param>
        /// <param name="cpFirst">first cp of the line</param>
        /// <param name="runLength">run length</param>
        /// <param name="widthLeft">maximum run width</param>
        /// <param name="idealRunOffsetUnRounded">run's offset from the beginning of the line</param>
        /// <returns>a SimpleRun object</returns>
        static public SimpleRun Create(
            FormatSettings          settings,
            CharacterBufferRange    charString,
            TextRun                 textRun,
            int                     cp,
            int                     cpFirst,
            int                     runLength,
            int                     widthLeft,
            int                     idealRunOffsetUnRounded,
            double                  pixelsPerDip
            )
        {
            SimpleRun run = null;

            if (textRun is TextCharacters)
            {
                if (    textRun.Properties.BaselineAlignment != BaselineAlignment.Baseline
                    ||  (textRun.Properties.TextEffects != null && textRun.Properties.TextEffects.Count != 0)
                    )
                {
                    // fast path does not handle the following conditions
                    //  o  non-default baseline alignment
                    //  o  text drawing effect
                    return null;
                }

                TextDecorationCollection textDecorations = textRun.Properties.TextDecorations;

                if (    textDecorations != null
                    &&  textDecorations.Count != 0
                    &&  !textDecorations.ValueEquals(TextDecorations.Underline))
                {
                    // we only support a single underline
                    return null;
                }

                settings.DigitState.SetTextRunProperties(textRun.Properties);
                if (settings.DigitState.RequiresNumberSubstitution)
                {
                    // don't support number substitution in fast path
                    return null;
                }

                bool canProcessTabsInSimpleShapingPath = CanProcessTabsInSimpleShapingPath(
                                                                settings.Pap,
                                                                settings.Formatter.TextFormattingMode
                                                                );

                if (charString[0] == TextStore.CharCarriageReturn)
                {
                    // CR in the middle of text stream treated as explicit paragraph break
                    // simple hard line break
                    runLength = 1;
                    if (charString.Length > 1 && charString[1] == TextStore.CharLineFeed)
                    {
                        runLength = 2;
                    }
                    // This path handles the case where the backing store breaks the text run in between
                    // a Carriage Return and a Line Feed. So we fetch the next run to check whether the next
                    // character is a line feed.
                    else if (charString.Length == 1)
                    {
                        // Prefetch to check for line feed.
                        TextRun newRun;
                        int newRunLength;
                        CharacterBufferRange newBufferRange = settings.FetchTextRun(
                            cp + 1,
                            cpFirst,
                            out newRun,
                            out newRunLength
                            );

                        if (newBufferRange.Length > 0 && newBufferRange[0] == TextStore.CharLineFeed)
                        {
                            // Merge the 2 runs.
                            int lengthOfRun = 2;
                            char[] characterArray = new char[lengthOfRun];
                            characterArray[0] = TextStore.CharCarriageReturn;
                            characterArray[1] = TextStore.CharLineFeed;
                            TextRun mergedTextRun = new TextCharacters(characterArray, 0, lengthOfRun, textRun.Properties);
                            return new SimpleRun(lengthOfRun, mergedTextRun, (Flags.EOT | Flags.Ghost), settings.Formatter, pixelsPerDip);
                        }
}
                    return new SimpleRun(runLength, textRun, (Flags.EOT | Flags.Ghost), settings.Formatter, pixelsPerDip);
                }
                else if (charString[0] == TextStore.CharLineFeed)
                {
                    // LF in the middle of text stream treated as explicit paragraph break
                    // simple hard line break
                    runLength = 1;
                    return new SimpleRun(runLength, textRun, (Flags.EOT | Flags.Ghost), settings.Formatter, pixelsPerDip);
                }
                else if (canProcessTabsInSimpleShapingPath && charString[0] == TextStore.CharTab)
                {
                    return CreateSimpleRunForTab(settings,
                                                 textRun,
                                                 idealRunOffsetUnRounded,
                                                 pixelsPerDip);
                }

                // attempt to create a simple run for text
                run = CreateSimpleTextRun(
                    charString,
                    textRun,
                    settings.Formatter,
                    widthLeft,
                    settings.Pap.EmergencyWrap,
                    canProcessTabsInSimpleShapingPath,
                    pixelsPerDip
                    );

                if (run == null)
                {
                    // fail to create simple text run, the run content is too complex
                    return null;
                }

                // Check for underline condition
                if (textDecorations != null && textDecorations.Count == 1 )
                {
                    run.Underline = textDecorations[0];
                }
            }
            else if (textRun is TextEndOfLine)
            {
                run = new SimpleRun(runLength, textRun, (Flags.EOT | Flags.Ghost), settings.Formatter, pixelsPerDip);
            }
            else if (textRun is TextHidden)
            {
                // hidden run
                run = new SimpleRun(runLength, textRun, Flags.Ghost, settings.Formatter, pixelsPerDip);
            }

            return run;
        }

        /// <summary>
        /// Returns a simple text run that represents a Tab.
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="textRun">text run</param>
        /// <param name="idealRunOffsetUnRounded">run's offset from the beginning of the line</param>
        static private SimpleRun CreateSimpleRunForTab(
            FormatSettings settings,
            TextRun textRun,
            int idealRunOffsetUnRounded,
            double pixelsPerDip
            )
        {
            if (settings == null || textRun == null || textRun.Properties == null || textRun.Properties.Typeface == null)
            {
                return null;
            }

            GlyphTypeface glyphTypeface = textRun.Properties.Typeface.TryGetGlyphTypeface();

            // Check whether the font has the space character. If not then we have to go through
            // font fallback.
            // We are not calling CreateSimpleTextRun() because CheckFastPathNominalGlyphs()
            // can fail if a font has TypographicAvailabilities. We are simply rendering a space
            // so we don't realy care about TypographicFeatures. This is a perf optimization.
            if (glyphTypeface == null || !glyphTypeface.HasCharacter(' '))
            {
                return null;
            }

            // The full shaping path converts tabs to spaces.
            // Note: In order to get exactly the same metrics as we did in FullTextLine (specifically ink bounding box)
            // we need to "Draw" a space in place of a Tab (previously we were just ignoring the Tab and rendering nothing)
            // which turned out to give different overhang and extent values than those returned using the full shaping path.
            // So in order to avoid vertical jiggling when a line is changed from SimpleTextLine to FullTextLine by adding/removing
            // a complex character, we need to do the same thing as the full shaping path and draw a space for each tab.
            TextRun modifedTextRun = new TextCharacters(" ", textRun.Properties);
            CharacterBufferRange characterBufferRange = new CharacterBufferRange(modifedTextRun);
            SimpleRun run = new SimpleRun(1, modifedTextRun, Flags.Tab, settings.Formatter, pixelsPerDip);
            run.CharBufferReference = characterBufferRange.CharacterBufferReference;
            run.TextRun.Properties.Typeface.GetCharacterNominalWidthsAndIdealWidth(
                    characterBufferRange,
                    run.EmSize,
                    (float)pixelsPerDip,
                    TextFormatterImp.ToIdeal,
                    settings.Formatter.TextFormattingMode,
                    false,
                    out run.NominalAdvances
                    );

            int idealIncrementalTab = TextFormatterImp.RealToIdeal(settings.Pap.DefaultIncrementalTab);

            // Here we get the next tab stop without snapping the metrics to pixels.
            // We do the pixel snapping on the final position of the tab stop (and not on the IncrementalTab)
            // to achieve the same results as those in full shaping.
            int idealNextTabStopUnRounded = ((idealRunOffsetUnRounded / idealIncrementalTab) + 1) * idealIncrementalTab;

            run.IdealWidth = run.NominalAdvances[0] = idealNextTabStopUnRounded - idealRunOffsetUnRounded;
            return run;
        }

        /// <summary>
        /// Returns whether the conditions are met to make it possible to process tabs
        /// in the simple shaping path.
        /// </summary>
        static private bool CanProcessTabsInSimpleShapingPath(
            ParaProp           textParagraphProperties,
            TextFormattingMode textFormattingMode
            )
        {
            return (textParagraphProperties.Tabs == null && textParagraphProperties.DefaultIncrementalTab > 0);
        }

        /// <summary>
        /// Create simple run of text,
        /// returning null if the specified text run cannot be correctly formatted as simple run
        /// </summary>
        static internal SimpleRun CreateSimpleTextRun(
            CharacterBufferRange    charBufferRange,
            TextRun                 textRun,
            TextFormatterImp        formatter,
            int                     widthLeft,
            bool                    emergencyWrap,
            bool                    breakOnTabs,
            double                  pixelsPerDip
            )
        {
            Invariant.Assert(textRun is TextCharacters);

            SimpleRun run = new SimpleRun(formatter, pixelsPerDip);
            run.CharBufferReference = charBufferRange.CharacterBufferReference;
            run.TextRun = textRun;

            if (!run.TextRun.Properties.Typeface.CheckFastPathNominalGlyphs(
                charBufferRange,
                run.EmSize,
                (float)pixelsPerDip,
                1.0,
                formatter.IdealToReal(widthLeft, pixelsPerDip),
                !emergencyWrap,
                false,
                CultureMapper.GetSpecificCulture(run.TextRun.Properties.CultureInfo),
                formatter.TextFormattingMode,
                false,          //No support for isSideways
                breakOnTabs,
                out run.Length
                ))
            {
                // Getting nominal glyphs is not supported by the font,
                // or it is but it results in low typographic quality text
                // e.g. OpenType support is not utilized.
                return null;
            }

            run.TextRun.Properties.Typeface.GetCharacterNominalWidthsAndIdealWidth(
                new CharacterBufferRange(run.CharBufferReference, run.Length),
                run.EmSize,
                (float)pixelsPerDip,
                TextFormatterImp.ToIdeal,
                formatter.TextFormattingMode,
                false,
                out run.NominalAdvances,
                out run.IdealWidth
                );

            return run;
        }



        /// <summary>
        /// Construct simple text run
        /// </summary>
        /// <param name="length">run length</param>
        /// <param name="textRun">text run</param>
        /// <param name="flags">run flags</param>
        private SimpleRun(
            int              length,
            TextRun          textRun,
            Flags            flags,
            TextFormatterImp textFormatterImp,
            double pixelsPerDip
            )
        {
            Length = length;
            TextRun = textRun;
            RunFlags = flags;
            _textFormatterImp = textFormatterImp;
            _pixelsPerDip = pixelsPerDip;
        }


        /// <summary>
        /// Draw a simple run
        /// </summary>
        /// <returns>drawing bounding box</returns>
        internal Rect Draw(
            DrawingContext      drawingContext,
            double              x,
            double              y,
            bool                visiCodePath
            )
        {
            if (Length <= 0 || this.Ghost)
            {
                return Rect.Empty;  // nothing to draw
            }

            Brush foregroundBrush = TextRun.Properties.ForegroundBrush;

            if(visiCodePath && foregroundBrush is SolidColorBrush)
            {
                Color color = ((SolidColorBrush)foregroundBrush).Color;
                foregroundBrush = new SolidColorBrush(Color.FromArgb(
                    (byte)(color.A>>2), // * 0.25
                    color.R,
                    color.G,
                    color.B
                    ));
            }

            Rect inkBoundingBox;

            IList<double> displayGlyphAdvances;
            if (_textFormatterImp.TextFormattingMode == TextFormattingMode.Ideal)
            {
                displayGlyphAdvances = new ThousandthOfEmRealDoubles(EmSize, NominalAdvances.Length);
                for (int i = 0; i < displayGlyphAdvances.Count; i++)
                {
                    // convert ideal glyph advance width to real width for displaying.
                    displayGlyphAdvances[i] = _textFormatterImp.IdealToReal(NominalAdvances[i], _pixelsPerDip);
                }
            }
            else
            {
                displayGlyphAdvances = new List<double>(NominalAdvances.Length);
                for (int i = 0; i < NominalAdvances.Length; i++)
                {
                    // convert ideal glyph advance width to real width for displaying.
                    displayGlyphAdvances.Add(_textFormatterImp.IdealToReal(NominalAdvances[i], _pixelsPerDip));
                }
            }

            CharacterBufferRange charBufferRange = new CharacterBufferRange(CharBufferReference, Length);

            GlyphTypeface glyphTypeface = Typeface.TryGetGlyphTypeface();
            Invariant.Assert(glyphTypeface != null);

            GlyphRun glyphRun = glyphTypeface.ComputeUnshapedGlyphRun(
                new Point(x, y),
                charBufferRange,
                displayGlyphAdvances,
                EmSize,
                (float)_pixelsPerDip,
                TextRun.Properties.FontHintingEmSize,
                Typeface.NullFont,
                CultureMapper.GetSpecificCulture(TextRun.Properties.CultureInfo),
                null,  // device font name
                _textFormatterImp.TextFormattingMode
              );

            if (glyphRun != null)
            {
                inkBoundingBox = glyphRun.ComputeInkBoundingBox();
            }
            else
            {
                inkBoundingBox = Rect.Empty;
            }

            if (!inkBoundingBox.IsEmpty)
            {
                // glyph run's ink bounding box is relative to its origin
                inkBoundingBox.X += glyphRun.BaselineOrigin.X;
                inkBoundingBox.Y += glyphRun.BaselineOrigin.Y;
            }

            if (drawingContext != null)
            {
                if (glyphRun != null)
                {
                    glyphRun.EmitBackground(drawingContext, TextRun.Properties.BackgroundBrush);
                    drawingContext.DrawGlyphRun(foregroundBrush, glyphRun);
                }


                // draw underline here
                if (Underline != null)
                {
                    // Determine number of characters to underline. We don't underline trailing spaces
                    // if the TrimTrailingUnderline flag is set.
                    int underlineLength = Length;
                    if (TrimTrailingUnderline)
                    {
                        while (underlineLength > 0 && IsSpace(charBufferRange[underlineLength - 1]))
                        {
                            --underlineLength;
                        }
                    }

                    // Determine the width of the underline.
                    double dxUnderline = 0;
                    for (int i = 0; i < underlineLength; ++i)
                    {
                        dxUnderline += _textFormatterImp.IdealToReal(NominalAdvances[i], _pixelsPerDip);
                    }

                    // We know only TextDecoration.Underline will be handled in Simple Path.
                    double offset = -Typeface.UnderlinePosition * EmSize;
                    double penThickness = Typeface.UnderlineThickness * EmSize;

                    Point lineOrigin = new Point(x, y + offset);

                    Rect underlineRect = new Rect(
                            lineOrigin.X,
                            lineOrigin.Y - penThickness * 0.5,
                            dxUnderline,
                            penThickness
                        );

                    // Apply the pair of guidelines: one for baseline and another
                    // for top edge of undelining line. Both will be snapped to pixel grid.
                    // Guideline pairing algorithm detects the case when these two
                    // guidelines happen to be close to one another and provides
                    // synchronous snapping, so that the gap between baseline and
                    // undelining line does not depend on the position of text line.
                    drawingContext.PushGuidelineY2(y, lineOrigin.Y - penThickness * 0.5 - y);

                    try
                    {
                        drawingContext.DrawRectangle(
                            foregroundBrush,
                            null,               // pen
                            underlineRect
                            );
                    }
                    finally
                    {
                        drawingContext.Pop();
                    }

                    // underline pen thickness is always positive in fast path
                    inkBoundingBox.Union(
                        underlineRect
                        );
                }
            }

            return inkBoundingBox;
        }


        /// <summary>
        /// Scan backward to collect trailing spaces of the run
        /// </summary>
        /// <param name="formatter">formatter</param>
        /// <param name="trailing">trailing spaces</param>
        /// <param name="trailingSpaceWidth">trailing spaces width</param>
        /// <returns>continue collecting the previous run?</returns>
        internal bool CollectTrailingSpaces(
            TextFormatterImp formatter,
            ref int          trailing,
            ref int          trailingSpaceWidth
            )
        {
            // As we are collecting trailing space cp, we also collect the trailing space width.
            // In Full text line, TrailingSpaceWidth = ToReal(Sumof(ToIdeal(glyphsWidths));
            // we do the same thing here so that trailing space width is exactly the same
            // as Full Text Line.
            if(Ghost)
            {
                if(!EOT)
                {
                    trailing += Length;
                    trailingSpaceWidth += IdealWidth;
                }
                return true;
            }
            // A Tab does not contribute to trailing space calculations.
            else if (Tab)
            {
                return false;
            }

            int offsetToFirstChar = CharBufferReference.OffsetToFirstChar;
            CharacterBuffer charBuffer = CharBufferReference.CharacterBuffer;
            int dcp = Length;

            if (dcp > 0 && IsSpace(charBuffer[offsetToFirstChar + dcp - 1]))
            {
                // scan backward to find the first blank following a non-blank
                while (dcp > 0 && IsSpace(charBuffer[offsetToFirstChar + dcp - 1]))
                {
                    // summing the ideal value of each glyph
                    trailingSpaceWidth += NominalAdvances[dcp - 1];
                    dcp--;
                    trailing++;
                }

                return dcp == 0;
            }

            return false;
        }

        private static bool IsSpace(char ch)
        {
            if (TextStore.IsSpace(ch))
                return true;

            int charClass = (int)Classification.GetUnicodeClassUTF16(ch);
            return Classification.CharAttributeOf(charClass).BiDi == DirectionClass.WhiteSpace;
        }


        internal bool IsUnderlineCompatible(SimpleRun nextRun)
        {
            return     Typeface.Equals(nextRun.Typeface)
                    && EmSize == nextRun.EmSize
                    && Baseline == nextRun.Baseline;
        }


        internal int DistanceFromDcp(int dcp)
        {
            if (Ghost || Tab)
            {
                return dcp <= 0 ? 0 : IdealWidth;
            }

            if (dcp > Length)
            {
                dcp = Length;
            }

            int idealDistance = 0;

            for(int i = 0; i < dcp; i++)
            {
                idealDistance += NominalAdvances[i];
            }

            return idealDistance;
        }


        internal CharacterHit DcpFromDistance(int idealDistance)
        {
            if (Ghost)
            {
                return (EOT || idealDistance <= 0) ? new CharacterHit() : new CharacterHit(Length, 0);
            }

            if (Length <= 0)
            {
                return new CharacterHit();
            }

            int dcp = 0;
            int currentIdealAdvance = 0;

            // A Tab cannot be treated as a Ghost run since Ghost runs are just skipped in hit testing while a Tab should not.
            // In case of a Tab, currentIdealAdvance = IdealWidth / Length. The division by Length
            // is for future robustness only. Today a Tab run contains only 1 tab and hence its Length is 1. However, this code
            // should not have knowledge of this info and hence we divide by Length in case in the future this implementation
            // detail changed.
            while (dcp < Length && idealDistance >= (Tab ? (currentIdealAdvance = IdealWidth / Length)
                                                         : (currentIdealAdvance = NominalAdvances[dcp])))
            {
                idealDistance -= currentIdealAdvance;
                dcp++;
            }

            if (dcp < Length)
            {
                // hit occurs in this run
                return new CharacterHit(dcp, (idealDistance > currentIdealAdvance / 2 ? 1 : 0));
            }

            // hit doesn't occur in this run
            return new CharacterHit(Length - 1, 1);
        }
    }
}
