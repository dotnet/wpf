// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Full text implementation of the specialized text line representing 
//             state of line up to the point where line break may occur
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Security;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Internal.PresentationCore;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Full text implementation of the specialized text line representing state 
    /// of line up to the point where line break may occur. Unlike a more tangible
    /// type like text line , breakpoint could not draw or performs hit-testing 
    /// operation. It could only reflect formatting result back to the client.
    /// </summary>
    internal sealed class FullTextBreakpoint : TextBreakpoint
    {
        private TextMetrics                         _metrics;           // full text metrics
        private SecurityCriticalDataForSet<IntPtr>  _ploline;           // native object representing this break
        private SecurityCriticalDataForSet<IntPtr>  _penaltyResource;   // unsafe handle to the internal factors used to determines penalty of the break. By default, the lifetime of this resource is managed by _ploline.
        private bool                                _isDisposed;        // flag indicates whether this object is disposed
        private bool                                _isLineTruncated;   // flag indicates whether the line produced at this breakpoint is truncated.


        /// <summary>
        /// Construct a list of potential breakpoints starting from the specified firstCharIndex
        /// </summary>
        /// <param name="paragraphCache">cache of paragraph content of all possible breaks within</param>
        /// <param name="firstCharIndex">index to first character where formatting starts</param>
        /// <param name="maxLineWidth">max format ideal width of the line being built</param>
        /// <param name="previousLineBreak">LineBreak property of the previous text line, or null if this is the first line in the paragraph</param>
        /// <param name="penaltyRestriction">constraint on what breakpoint is returned based on its implied calculated penalty</param>
        /// <param name="bestFitIndex">index of the best fit breakpoint in the returned collection</param>        
        /// <returns>a list of potential breakpoints starting from firstCharIndex</returns>
        internal static IList<TextBreakpoint> CreateMultiple(
            TextParagraphCache          paragraphCache,
            int                         firstCharIndex,
            int                         maxLineWidth,
            TextLineBreak               previousLineBreak,
            IntPtr                      penaltyRestriction,
            out int                     bestFitIndex            
            )
        {
            Invariant.Assert(paragraphCache != null);

            // grab full text state from paragraph cache
            FullTextState fullText = paragraphCache.FullText;
            Invariant.Assert(fullText != null);

            FormatSettings settings = fullText.TextStore.Settings;
            Invariant.Assert(settings != null);

            // update formatting parameters at line start
            settings.UpdateSettingsForCurrentLine(
                maxLineWidth,
                previousLineBreak, 
                (firstCharIndex == fullText.TextStore.CpFirst)
                );

            Invariant.Assert(settings.Formatter != null);

            // acquiring LS context
            TextFormatterContext context = settings.Formatter.AcquireContext(fullText, IntPtr.Zero);

            IntPtr previousBreakRecord = IntPtr.Zero;
            if (settings.PreviousLineBreak != null)
                previousBreakRecord = settings.PreviousLineBreak.BreakRecord.Value;

            // need not consider marker as tab since marker does not affect line metrics and it wasnt drawn.
            fullText.SetTabs(context);

            LsBreaks lsbreaks = new LsBreaks();

            LsErr lserr = context.CreateBreaks(
                fullText.GetBreakpointInternalCp(firstCharIndex),
                previousBreakRecord,
                paragraphCache.Ploparabreak.Value,  // para breaking session
                penaltyRestriction,
                ref lsbreaks, 
                out bestFitIndex
                );

            // get the exception in context before it is released
            Exception callbackException = context.CallbackException;
            
            // release the context
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
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.CreateBreaksFailure, lserr), lserr);
                }
            }

            // keep context alive at least till here
            GC.KeepAlive(context);

            TextBreakpoint[] breakpoints = new TextBreakpoint[lsbreaks.cBreaks];

            for (int i = 0; i < lsbreaks.cBreaks; i++)
            {
                breakpoints[i] = new FullTextBreakpoint(
                    fullText,
                    firstCharIndex,
                    maxLineWidth,
                    ref lsbreaks,
                    i   // the current break
                    );
            }

            return breakpoints;
        }


        /// <summary>
        /// Construct breakpoint from full text info
        /// </summary>
        private FullTextBreakpoint(
            FullTextState           fullText,
            int                     firstCharIndex,
            int                     maxLineWidth,
            ref LsBreaks            lsbreaks,
            int                     breakIndex
            ) : this()
        {
            // According to antons: PTS only uses the width of a feasible break to avoid
            // clipping in subpage. At the moment, there is no good solution as of how
            // PTS client would be able to compute this width efficiently using LS. 
            // The work around - although could be conceived would simply be too slow.
            // The width should therefore be set to the paragraph width for the time being.
            //
            // Client of text formatter would simply pass the value of TextBreakpoint.Width
            // back to PTS pfnFormatLineVariants call.
            LsLineWidths lineWidths = new LsLineWidths();
            lineWidths.upLimLine = maxLineWidth;
            lineWidths.upStartMainText = fullText.TextStore.Settings.TextIndent;
            lineWidths.upStartMarker = lineWidths.upStartMainText;
            lineWidths.upStartTrailing = lineWidths.upLimLine;
            lineWidths.upMinStartTrailing = lineWidths.upStartTrailing;

            // construct the correspondent text metrics
            unsafe
            {
                _metrics.Compute(
                    fullText,
                    firstCharIndex,
                    maxLineWidth,
                    null,   // collapsingSymbol
                    ref lineWidths,
                    &lsbreaks.plslinfoArray[breakIndex]
                    );

                _ploline = new SecurityCriticalDataForSet<IntPtr>(lsbreaks.pplolineArray[breakIndex]);

                // keep the line penalty handle
                _penaltyResource = new SecurityCriticalDataForSet<IntPtr>(lsbreaks.plinepenaltyArray[breakIndex]);

                if (lsbreaks.plslinfoArray[breakIndex].fForcedBreak != 0)
                    _isLineTruncated = true;
            }
        }


        /// <summary>
        /// Empty private constructor
        /// </summary>
        private FullTextBreakpoint()
        {
            _metrics = new TextMetrics();
        }


        /// <summary>
        /// Finalizing the break
        /// </summary>
        ~FullTextBreakpoint()
        {
            Dispose(false);
        }


        /// <summary>
        /// Disposing LS unmanaged memory for text line
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(_ploline.Value != IntPtr.Zero)
            {
                UnsafeNativeMethods.LoDisposeLine(_ploline.Value, !disposing);
                _ploline.Value = IntPtr.Zero;
                _penaltyResource.Value = IntPtr.Zero;
                _isDisposed = true;
                GC.KeepAlive(this);
            }
        }

        #region TextBreakpoint

        /// <summary>
        /// Client to acquire a state at the point where breakpoint is determined by line breaking process; 
        /// can be null when the line ends by the ending of the paragraph. Client may pass this
        /// value back to TextFormatter as an input argument to TextFormatter.FormatParagraphBreakpoints when 
        /// formatting the next set of breakpoints within the same paragraph.
        /// </summary>
        public override TextLineBreak GetTextLineBreak()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(SR.Get(SRID.TextBreakpointHasBeenDisposed));
            }
            return _metrics.GetTextLineBreak(_ploline.Value);
        }


        /// <summary>
        /// Client to get the handle of the internal factors that are used to determine penalty of this breakpoint.
        /// </summary>
        /// <remarks>
        /// Calling this method means that the client will now manage the lifetime of this unmanaged resource themselves using unsafe penalty handler.
        /// We would make a correspondent call to notify our unmanaged wrapper to release them from duty of managing this 
        /// resource. 
        /// </remarks>
        internal override SecurityCriticalDataForSet<IntPtr> GetTextPenaltyResource()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(SR.Get(SRID.TextBreakpointHasBeenDisposed));
            }

            LsErr lserr = UnsafeNativeMethods.LoRelievePenaltyResource(_ploline.Value);
            if (lserr != LsErr.None)
            {
                TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.RelievePenaltyResourceFailure, lserr), lserr);
            }

            return _penaltyResource;
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
            get { return _isLineTruncated; }
        }


        #endregion

        #region TextMetrics

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

        #endregion
    }
}
