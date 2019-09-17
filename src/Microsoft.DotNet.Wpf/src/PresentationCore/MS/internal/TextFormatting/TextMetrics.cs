// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Full text implementation of ITextMetrics
//
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Security;
using MS.Internal.FontCache;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace MS.Internal.TextFormatting
{
    internal partial struct TextMetrics : ITextMetrics
    {
        private TextFormatterImp        _formatter;                 // text formatter formatting this metrics
        private int                     _lscpLim;                   // number of LSCP in the line (for boundary condition handling)
        private int                     _cchLength;                 // actual character count    
        private int                     _cchDepend;                 // number of chars after linebreak that triggers reformatting this line
        private int                     _cchNewline;                // number of chars of newline symbol
        private int                     _height;                    // line height
        private int                     _textHeight;                // measured height of text within the line
        private int                     _baselineOffset;            // offset from top of line height to baseline    
        private int                     _textAscent;                // offset from top of text height to baseline
        private int                     _textStart;                 // distance from LS origin to text start
        private int                     _textWidth;                 // text start to end
        private int                     _textWidthAtTrailing;       // text start to end excluding trailing whitespaces    
        private int                     _paragraphToText;           // paragraph start to text start
        private LSRun                   _lastRun;                   // Last Text LSRun
        private double                  _pixelsPerDip;              // PixelsPerDip

        /// <summary>
        /// Construct text metrics from full text info
        /// </summary>
        /// <remarks>
        /// 
        /// When the application formats a line of text. It starts from the leading edge of the paragraph - the reference position
        /// called "Paragraph Start". It gives the width of the paragraph or "Paragraph Width" to TextFormatter as one of the main 
        /// parameters to TextFormatter.FormatLine method. It may also provide additional info about how it wants the line to look 
        /// like. The following are all of such info and how the formatting process is carried on inside TextFormatter.
        /// 
        /// 
        /// *** Indent/Paragraph Indent ***
        /// The application may specify "Indent" - the distance from the beginning of the line to the beginning of the text in that
        /// line. The value is sent to TextFormatter via [TextParagraphProperties.Indent]. It may also specify "Paragraph Indent"  
        /// - the distance from the beginning of the paragraph to the beginning of the line [TextParagraphProperties.ParagraphIndent]. 
        /// The usage of paragraph indent is to offset the beginning of the line relative to the paragraph starting point, while 
        /// indent is to offset the beginning of text realtive to the line starting point. Paragraph indent is not included as part 
        /// of the line width while indent is. 
        /// 
        /// 
        /// *** Text Alignment ***
        /// "Text Alignment" [TextParagraphProperties.TextAlignment] may be specified to align the leading, center or trailing edge
        /// of the line to the leading, center or trailing edge of the paragraph excluding paragraph indent. 
        /// 
        /// 
        /// *** Bullet/Auto-numbering ***
        /// The application may also specify "bullet" (or "marker") for the line. Marker does not affect the layout measurement of the
        /// line. Line with marker has the same line width with the line that has not. The presence of marker however affects the 
        /// pixel-wise black width of the line. The application specifies the distance from the beginning of the line to the trailing
        /// edge of the marker symbol via the property [TextMarkerProperties.Offset]. The application can create the visual effect of 
        /// having marker embedded inside the body of paragraph text (so-called "marker inside") by specifying a positive indent so 
        /// that the text starts after the beginning of the line and a positive smaller amount of marker offset to place the marker 
        /// symbol at between the beginning of the line and the beginning of the text. The "marker outside" visual effect can 
        /// also be achieved in a similar manner by specifying zero or positive indent value with negative marker offset value.
        /// 
        /// 
        /// *** Formatted Line Properties ***
        /// Once the line formatting process is completed and a line is returned to the application. The application determines the 
        /// distance from the paragraph starting point to the actual beginning of the line by looking at the "Line Start" property of
        /// the text line [TextLine.Start]. The "Width" of the line can be determined, naturally, from the property [TextLine.Width]. 
        /// The property value [TextLine.OverhangLeading] represents the distance from the beginning of the line, or the line's alignment 
        /// point, to the first leading pixel of that line so-called the "Black Start". The property [TextLine.OverhangTrailing]
        /// is the distance from the last trailing pixel of the line to the trailing edge alignment point of the line. The application
        /// uses these "overhang" or "overshoot" values to ensure proper positioning of text that avoids pixel clipping of the
        /// glyph image. A less sophisticated application may provide reasonable leading and trailing margin around the text line
        /// and ignores these properties altogether.
        /// 
        /// 
        /// *** Hit-Testing ***
        /// The application may also perform hit-testing by calling methods on TextLine. All the distances involved in hit-testing 
        /// operations are distances from the paragraph start, not from the line start. Marker symbol on its own is not hit-testable. 
        /// 
        /// 
        /// *** Tabs ***
        /// The application may specify tab stops - an array of positions to where text aligns. Each tab stop may have different 
        /// "Tab Alignment". The left, center and right tab alignment aligns the tab stop position to the leading, center and the 
        /// trailing edge of the text following the tab character. "Tab Leader" may also be specified to fill the distance occupied
        /// by the presence of tab character with the symbol of choice. Tab stops is specified thru the property [TextParagraph.Tabs].
        /// In the absence of tab stops, the application may assume an automatic tab stop - so called "Incremental Tab" specified by
        /// the property [TextParagraphProperties.DefaultIncrementalTab]. The property could be overridden, by default the value
        /// is set by TextFormatter to 4 em of the paragraph's default font.
        /// 
        /// 
        /// *** Line Services Properties ***
        /// TextFormatter relies on LS to calculate the distance from the beginning of the line to the beginning of text or "Text Start"
        /// and keep it in the private property [this._textStart]. This value is non-zero when 1) the line starts with indentation or 
        /// 2) the line starts with marker - either bullet or auto-numbering symbol. 
        /// 
        /// In case of the line with marker, LS also produces the distance from the beginning of the line to the beginning of the marker 
        /// symbol, but TextFormatter does not retain that distance because marker is outside the line. The application is assumed 
        /// responsibility to make sure the marker symbol is not going to be clipped out. The application achieves that by manipulating 
        /// the indent value along with the marker offset value. 
        /// 
        /// TextFormatter also retains the total "Text Width" value computed by LS in the private property [this._textWidth]. This 
        /// is the distance from the beginning of the text to the end including all trailing whitespaces at the end of the line. The
        /// similar value but with trailing whitespaces excluded is kept in the private property [this._textWidthAtTrailing].
        /// 
        /// TextFormatter starts formatting a LS line by assuming the beginning of the line being at an imaginary origin. It then
        /// places the starting point of the content depending on whether the line has either marker symbol or indent. The actual 
        /// mechanism for the placement is in FetchLineProps callback where the value [LsLineProps.durLeft] represents the distance 
        /// relative to the line's origin where actual content begins. The distances can either be positive or negative. Negative 
        /// distance runs in the reverse direction from the direction of text flow. When a negative indent or marker offset is 
        /// specified, durLeft is set to negative distance relative to line start.
        /// 
        /// TextFormatter however does not rely on LS for the whole line's text alignment. It always formats LS as if the line is 
        /// left-aligned. Once the distances of the line are received, it aligns the whole line according to the text alignment setting
        /// specified by the application, outside the LS call. The result of this aligning process is a distance from the beginning of 
        /// the paragraph to the beginning of text and is kept in a private property [this._paragraphToText].
        /// 
        /// </remarks>
        internal unsafe void Compute(
            FullTextState           fullText,
            int                     firstCharIndex,
            int                     paragraphWidth,
            FormattedTextSymbols    collapsingSymbol,
            ref LsLineWidths        lineWidths,
            LsLInfo*                plsLineInfo
            )
        {
            _formatter = fullText.Formatter;
            TextStore store = fullText.TextStore;

            _pixelsPerDip = store.Settings.TextSource.PixelsPerDip;
            // obtain position of important distances
            _textStart = lineWidths.upStartMainText;
            _textWidthAtTrailing = lineWidths.upStartTrailing;
            _textWidth = lineWidths.upLimLine;

            // append line end collapsing symbol if any
            if (collapsingSymbol != null)
            {
                AppendCollapsingSymbolWidth(TextFormatterImp.RealToIdeal(collapsingSymbol.Width));
            }

            // make all widths relative to text start
            _textWidth -= _textStart;
            _textWidthAtTrailing -= _textStart;

            // keep the newline character count if any
            _cchNewline = store.CchEol;

            // count text and dependant characters
            _lscpLim = plsLineInfo->cpLimToContinue;
            _lastRun = fullText.CountText(_lscpLim, firstCharIndex, out _cchLength);

            Debug.Assert(_cchLength > 0);

            if (  plsLineInfo->endr != LsEndRes.endrEndPara 
               && plsLineInfo->endr != LsEndRes.endrSoftCR)
            {
                // endrEndPara denotes that the line ends at paragraph end. It is a result of submitting Paragraph Separator to LS.
                // endrSoftCR denotes end of line but not end of paragraph. This is a result of submitting Line Separator to LS.
                _cchNewline = 0;

                if (plsLineInfo->dcpDepend >= 0)
                {
                    // According to SergeyGe [2/16/2006], dcpDepend reported from LS cannot made precise when considering
                    // the line ending with hyphenation - this is because LS does not have the knowledge about the amount 
                    // of text, after the hyphenation point, being examined by its client during the process of finding
                    // the right place to hyphenate. LS client must therefore take into account the number of lookahead 
                    // LSCP examined by hyphenator when computing the correct dcpDepend for the line. In our implementation
                    // it would just mean we take the max of the two values. 
                    int lscpFirstIndependence = Math.Max(
                        plsLineInfo->cpLimToContinue + plsLineInfo->dcpDepend,
                        fullText.LscpHyphenationLookAhead
                        );

                    fullText.CountText(lscpFirstIndependence, firstCharIndex, out _cchDepend);
                    _cchDepend -= _cchLength;
                }
            }

            ParaProp pap = store.Pap;

            if (_height <= 0)
            {
                // if height has not been settled, 
                // calculate line height and baseline offset
                if(pap.LineHeight > 0)
                {
                    // Host specifies line height, honor it.
                    _height = pap.LineHeight;
                    _baselineOffset = (int)Math.Round(
                        _height
                        * pap.DefaultTypeface.Baseline(pap.EmSize, Constants.DefaultIdealToReal, _pixelsPerDip, fullText.TextFormattingMode)
                        / pap.DefaultTypeface.LineSpacing(pap.EmSize, Constants.DefaultIdealToReal, _pixelsPerDip, fullText.TextFormattingMode)
                        );
                }

                if(plsLineInfo->dvrMultiLineHeight == int.MaxValue)
                {
                    // Line is empty so text height and text baseline are based on the default typeface;
                    // it doesn't make sense even for an emtpy line to have zero text height
                    _textAscent = (int)Math.Round(pap.DefaultTypeface.Baseline(pap.EmSize, Constants.DefaultIdealToReal, _pixelsPerDip, fullText.TextFormattingMode));
                    _textHeight = (int)Math.Round(pap.DefaultTypeface.LineSpacing(pap.EmSize, Constants.DefaultIdealToReal, _pixelsPerDip, fullText.TextFormattingMode));
                }
                else
                {
                    _textAscent = plsLineInfo->dvrAscent;
                    _textHeight = _textAscent + plsLineInfo->dvrDescent;

                    if (fullText.VerticalAdjust)
                    {
                        // Line requires vertical repositioning of text runs
                        store.AdjustRunsVerticalOffset(
                            plsLineInfo->cpLimToContinue - firstCharIndex, 
                            _height,
                            _baselineOffset,
                            out _textHeight,
                            out _textAscent
                            );
                    }
                }

                // if the client hasn't specified a line height then the line height and baseline
                // are the same as the text height and text baseline
                if (_height <= 0)
                {
                    _height = _textHeight;
                    _baselineOffset = _textAscent;
                }
            }

            // Text alignment aligns the line to correspondent paragraph alignment start edge
            switch(pap.Align)
            {
                case TextAlignment.Right: 

                    // alignment rule: 
                    //   "The sum of paragraph start to line start and line width is equal to paragraph width"
                    //
                    //        PTL + LW = PW
                    //        (PTT - LTT) + (LTT + TW) = PW
                    // (thus) PTT = PW - TW
                    _paragraphToText = paragraphWidth - _textWidthAtTrailing;
                    break;

                case TextAlignment.Center: 

                    // alignment rule: 
                    //   "The sum of paragraph start to line start and half the line width is equal to half the paragraph width"
                    //
                    //        PTL + 0.5*LW = 0.5*PW
                    //        (PTT - LTT) + 0.5*(LTT + TW) = 0.5*PW
                    // (thus) PTT = 0.5 * (PW + LTT - TW)
                    _paragraphToText = (int)Math.Round((paragraphWidth + _textStart - _textWidthAtTrailing) * 0.5);
                    break;

                default:

                    // alignment rule: 
                    //   "Paragraph start to line start is paragraph indent"
                    //
                    //        PTL = PI
                    //        PTT - LTT = PI
                    // (thus) PTT = PI + LTT
                    _paragraphToText = pap.ParagraphIndent + _textStart;
                    break;
            }
        }

        /// <summary>
        /// Client to acquire a state at the point where line is broken by line breaking process; 
        /// can be null when the line ends by the ending of the paragraph. Client may pass this 
        /// value back to TextFormatter as an input argument to TextFormatter.FormatLine when 
        /// formatting the next line within the same paragraph.
        /// </summary>
        /// <remarks>
        /// TextLineBreak is a finalizable object which may contain a reference to an unmanaged
        /// structure called break record. Break record can be acquired thru ploline. This method
        /// acquire break record only when the passing ploline object is not NULL. 
        /// 
        /// Not all situations requires break record. Single-line formatting without complex text
        /// object does not need it, but optimal break session does. For performance reason, we 
        /// should not produce break record unnecessarily because it makes TextLineBreak become
        /// finalizable, which therefore unnecessarily put additional pressure to GC since each
        /// finalizable object wakes finalizer thread and requires double GC collections.
        /// </remarks>
        internal TextLineBreak GetTextLineBreak(IntPtr ploline)
        {
            IntPtr pbreakrec = IntPtr.Zero;

            if (ploline != IntPtr.Zero)
            {
                LsErr lserr = UnsafeNativeMethods.LoAcquireBreakRecord(ploline, out pbreakrec);

                if (lserr != LsErr.None)
                {
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.AcquireBreakRecordFailure, lserr), lserr);
                }
            }

            if (    _lastRun != null 
                &&  _lastRun.TextModifierScope != null 
                &&  !(_lastRun.TextRun is TextEndOfParagraph))
            {
                return new TextLineBreak(
                    _lastRun.TextModifierScope, 
                    new SecurityCriticalDataForSet<IntPtr>(pbreakrec)
                    );
            }
            return (pbreakrec != IntPtr.Zero) ? new TextLineBreak(null, new SecurityCriticalDataForSet<IntPtr>(pbreakrec)) : null;
        }


        /// <summary>
        /// Append the ideal width of line end collapsing symbol
        /// </summary>
        private void AppendCollapsingSymbolWidth(
            int symbolIdealWidth
            )
        {
            _textWidth += symbolIdealWidth;
            _textWidthAtTrailing += symbolIdealWidth;
        }


        /// <summary>
        /// Client to get the number of text source positions of this line
        /// </summary>
        public int Length
        {
            get { return _cchLength; }
        }


        /// <summary>
        /// Client to get the number of characters following the last character 
        /// of the line that may trigger reformatting of the current line.
        /// </summary>
        public int DependentLength
        {
            get { return _cchDepend; }
        }


        /// <summary>
        /// Client to get the number of newline characters at line end
        /// </summary>
        public int NewlineLength 
        { 
            get { return _cchNewline; }
        }


        /// <summary>
        /// Client to get distance from paragraph start to line start
        /// </summary>
        public double Start
        {
            get { return _formatter.IdealToReal(_paragraphToText - _textStart, _pixelsPerDip); }
        }


        /// <summary>
        /// Client to get the total width of this line
        /// </summary>
        public double Width
        {
            get { return _formatter.IdealToReal(_textWidthAtTrailing + _textStart, _pixelsPerDip); }
        }


        /// <summary>
        /// Client to get the total width of this line including width of whitespace characters at the end of the line.
        /// </summary>
        public double WidthIncludingTrailingWhitespace
        {
            get { return _formatter.IdealToReal(_textWidth + _textStart, _pixelsPerDip); }
        }


        /// <summary>
        /// Client to get the height of the line
        /// </summary>
        public double Height 
        { 
            get { return _formatter.IdealToReal(_height, _pixelsPerDip); } 
        }


        /// <summary>
        /// Client to get the height of the text (or other content) in the line; this property may differ from the Height
        /// property if the client specified the line height
        /// </summary>
        public double TextHeight
        {
            get { return _formatter.IdealToReal(_textHeight, _pixelsPerDip); }
        }


        /// <summary>
        /// Client to get the distance from top to baseline of this text line
        /// </summary>
        public double Baseline
        { 
            get { return _formatter.IdealToReal(_baselineOffset, _pixelsPerDip); } 
        }


        /// <summary>
        /// Client to get the distance from the top of the text (or other content) to the baseline of this text line;
        /// this property may differ from the Baseline property if the client specified the line height
        /// </summary>
        public double TextBaseline
        {
            get { return _formatter.IdealToReal(_textAscent, _pixelsPerDip); }
        }


        /// <summary>
        /// Client to get the distance from the before edge of line height 
        /// to the baseline of marker of the line if any.
        /// </summary>
        public double MarkerBaseline 
        { 
            get { return Baseline; } 
        }


        /// <summary>
        /// Client to get the overall height of the list items marker of the line if any.
        /// </summary>
        public double MarkerHeight 
        { 
            get { return Height; } 
        }
    }
}

