// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: TextParagraph is a Paragraph representing continuous sequence
//              of lines.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// TextParagraph is a Paragraph representing continuous sequence of lines
    /// and it is using built-in PTS text paragraph handler to do formatting. 
    /// </summary>
    internal sealed class TextParagraph : BaseParagraph
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="element">
        /// Element associated with paragraph.
        /// </param>
        /// <param name="structuralCache">
        /// Content's structural cache
        /// </param>
        internal TextParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  IDisposable Methods
        //
        //-------------------------------------------------------------------

        #region IDisposable Methods

        /// <summary>
        /// IDisposable.Dispose 
        /// </summary>
        public override void Dispose()
        {
            if(_attachedObjects != null)
            {
                foreach(AttachedObject obj in _attachedObjects)
                {
                    obj.Dispose();
                }
                _attachedObjects = null;
            }

            if (_inlineObjects != null)
            {
                foreach (InlineObject obj in _inlineObjects)
                {
                    obj.Dispose();
                }
                _inlineObjects = null;
            }

            base.Dispose();
        }

        #endregion IDisposable Methods

        // ------------------------------------------------------------------
        //
        //  PTS callbacks
        //
        // ------------------------------------------------------------------

        #region PTS callbacks

        /// <summary>
        /// GetParaProperties 
        /// </summary>
        /// <param name="fspap">
        /// OUT: paragraph properties
        /// </param>
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)                 
        {
            // Paragraph associated with the element (SegmentParagraph) will
            // take care of break control.
            fspap.fKeepWithNext      = PTS.False;
            fspap.fBreakPageBefore   = PTS.False;
            fspap.fBreakColumnBefore = PTS.False;

            GetParaProperties(ref fspap, true); // true means ignore element props
            fspap.idobj = PTS.fsidobjText;
        }

        /// <summary>
        /// CreateParaclient 
        /// </summary>
        /// <param name="paraClientHandle">
        /// OUT: Para client handle, opaque to PTS paragraph client
        /// </param>
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)         
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. TextParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            TextParaClient paraClient = new TextParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518
        }

        /// <summary>
        /// GetTextProperties 
        /// </summary>
        /// <param name="iArea">
        /// IN: column-span area index
        /// </param>
        /// <param name="fstxtprops">
        /// OUT: text paragraph properties
        /// </param>
        internal void GetTextProperties(
            int iArea,                          
            ref PTS.FSTXTPROPS fstxtprops)      
        {
            Debug.Assert(iArea == 0);

            fstxtprops.fswdir = PTS.FlowDirectionToFswdir((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty));
            fstxtprops.dcpStartContent = 0;
            fstxtprops.fKeepTogether = PTS.FromBoolean(DynamicPropertyReader.GetKeepTogether(Element));
            fstxtprops.cMinLinesAfterBreak = DynamicPropertyReader.GetMinOrphanLines(Element);
            fstxtprops.cMinLinesBeforeBreak = DynamicPropertyReader.GetMinWidowLines(Element);
            fstxtprops.fDropCap = PTS.False;
            fstxtprops.fVerticalGrid = PTS.False;
            fstxtprops.fOptimizeParagraph = PTS.FromBoolean(IsOptimalParagraph);

            fstxtprops.fAvoidHyphenationAtTrackBottom = PTS.False;
            fstxtprops.fAvoidHyphenationOnLastChainElement = PTS.False;
            fstxtprops.cMaxConsecutiveHyphens = int.MaxValue;
        }


        /// <summary>
        /// CreateOptimalBreakSession
        /// </summary>
        /// <param name="textParaClient">
        /// Para client
        /// </param>
        /// <param name="dcpStart">
        /// Start of line to format
        /// </param>
        /// <param name="durTrack">
        /// dur of track (dur line?)
        /// </param>
        /// <param name="lineBreakRecord">
        /// Break record for a given line
        /// </param>
        /// <param name="optimalBreakSession">
        /// OUT: Break session
        /// </param>
        /// <param name="isParagraphJustified">
        /// OUT: Is paragraph fully justified
        /// </param>
        internal void CreateOptimalBreakSession(
            TextParaClient textParaClient, 
            int dcpStart,                          
            int durTrack,
            LineBreakRecord lineBreakRecord,
            out OptimalBreakSession optimalBreakSession,
            out bool isParagraphJustified)
        {
            _textRunCache = new TextRunCache();
            TextFormatter textFormatter = StructuralCache.TextFormatterHost.TextFormatter;
            TextLineBreak textLineBreak = lineBreakRecord != null ? lineBreakRecord.TextLineBreak : null;

            OptimalTextSource optimalTextSource = new OptimalTextSource(StructuralCache.TextFormatterHost, ParagraphStartCharacterPosition, durTrack, textParaClient, _textRunCache);
            StructuralCache.TextFormatterHost.Context = optimalTextSource;

            TextParagraphCache paragraphCache = textFormatter.CreateParagraphCache(StructuralCache.TextFormatterHost, 
                                                                                   dcpStart, 
                                                                                   TextDpi.FromTextDpi(durTrack), 
                                                                                   GetLineProperties(true, dcpStart), 
                                                                                   textLineBreak,
                                                                                   _textRunCache);

            StructuralCache.TextFormatterHost.Context = null;

            optimalBreakSession = new OptimalBreakSession(this, textParaClient, paragraphCache, optimalTextSource);
            isParagraphJustified = ((TextAlignment)Element.GetValue(Block.TextAlignmentProperty)) == TextAlignment.Justify;
}


        /// <summary>
        /// Get Number Footnotes 
        /// </summary>
        /// <param name="fsdcpStart">
        /// IN: dcp at the beginning of the range
        /// </param>
        /// <param name="fsdcpLim">
        /// IN: dcp at the end of the range
        /// </param>
        /// <param name="nFootnote">
        /// OUT: number of footnote references in the range
        /// </param>
        internal void GetNumberFootnotes(
            int fsdcpStart,                     
            int fsdcpLim,                        
            out int nFootnote)                   
        {
            nFootnote = 0;
        }

        /// <summary>
        /// Format Bottom Text 
        /// </summary>
        /// <param name="iArea">
        /// IN: column-span area index
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="lastLine">
        /// IN: last formatted line
        /// </param>
        /// <param name="dvrLine">
        /// IN: height of last line
        /// </param>
        /// <param name="mcsClient">
        /// OUT: margin collapsing state at bottom of text
        /// </param>
        internal void FormatBottomText(
            int iArea,                           
            uint fswdir,                         
            Line lastLine,                       
            int dvrLine,                         
            out IntPtr mcsClient)                
        {
            Invariant.Assert(iArea == 0);

            // Text paragraph does not handle margin collapsing. Margin collapsing
            // is done on container paragraph level.
            mcsClient = IntPtr.Zero;
        }

        /// <summary>
        /// Determines whether it's desirable or valid to interrupt formatting after this line. 
        /// Returns true if formatting can be interrupted and false if not.
        /// </summary>
        /// <param name="dcpCur">
        /// dcp of current line
        /// </param>
        /// <param name="vrCur">
        /// vr value of current line
        /// </param>
        internal bool InterruptFormatting(int dcpCur, int vrCur)
        {
            BackgroundFormatInfo backgroundFormatInfo = StructuralCache.BackgroundFormatInfo;

            if (!BackgroundFormatInfo.IsBackgroundFormatEnabled)
            {
                return false;
            }
            if (StructuralCache.CurrentFormatContext.FinitePage)
            {
                return false;
            }

            // Must format at least this much
            if (vrCur < TextDpi.ToTextDpi(double.IsPositiveInfinity(backgroundFormatInfo.ViewportHeight) ? 500 : backgroundFormatInfo.ViewportHeight))
            {
                return false;
            }

            if (backgroundFormatInfo.BackgroundFormatStopTime > DateTime.UtcNow)
            {
                return false;
            }

            if (!backgroundFormatInfo.DoesFinalDTRCoverRestOfText)
            {
                return false;
            }

            if ((dcpCur + ParagraphStartCharacterPosition) <= backgroundFormatInfo.LastCPUninterruptible)
            {
                return false;
            }

            StructuralCache.BackgroundFormatInfo.CPInterrupted = dcpCur + ParagraphStartCharacterPosition;

            return true;
        }



        /// <summary>
        /// FormatLineVariants - Find all possible stopping points for this line
        /// </summary>
        /// <param name="textParaClient">
        /// IN: Text para client for TextParagraph
        /// </param>
        /// <param name="textParagraphCache">
        /// IN: Paragraph cache
        /// </param>
        /// <param name="optimalTextSource">
        /// IN: Text Source for optimal
        /// </param>
        /// <param name="dcp">
        /// IN: dcp at the beginning of the line
        /// </param>
        /// <param name="textLineBreak">
        /// IN: line break record
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="urStartLine">
        /// IN: position at the beginning of the line
        /// </param>
        /// <param name="durLine">
        /// IN: maximum width of line
        /// </param>
        /// <param name="allowHyphenation">
        /// IN: allow hyphenation of the line?
        /// </param>
        /// <param name="clearOnLeft">
        /// IN: is clear on left side
        /// </param>
        /// <param name="clearOnRight">
        /// IN: is clear on right side
        /// </param>
        /// <param name="treatAsFirstInPara">
        /// IN: treat line as first line in paragraph
        /// </param>
        /// <param name="treatAsLastInPara">
        /// IN: treat line as last line in paragraph
        /// </param>
        /// <param name="suppressTopSpace">
        /// IN: suppress empty space at the top of page
        /// </param>
        /// <param name="lineVariantRestriction">
        /// IN: line variant restriction handle (pts / ls communication)
        /// </param>
        /// <param name="iLineBestVariant">
        /// OUT: index of the bset line variant (pts / ls communication)
        /// </param>        
        internal System.Collections.Generic.IList<TextBreakpoint> 
                    FormatLineVariants(TextParaClient textParaClient, 
                                       TextParagraphCache textParagraphCache,
                                       OptimalTextSource optimalTextSource,
                                       int dcp, 
                                       TextLineBreak textLineBreak,
                                       uint fswdir, 
                                       int urStartLine, 
                                       int durLine, 
                                       bool allowHyphenation, 
                                       bool clearOnLeft,
                                       bool clearOnRight,
                                       bool treatAsFirstInPara, 
                                       bool treatAsLastInPara, 
                                       bool suppressTopSpace,
                                       IntPtr lineVariantRestriction, 
                                       out int iLineBestVariant)
                                                                                     
        {
            StructuralCache.TextFormatterHost.Context = optimalTextSource;            

            System.Collections.Generic.IList<TextBreakpoint> textBreakpoints = textParagraphCache.FormatBreakpoints(
                dcp, 
                textLineBreak, 
                lineVariantRestriction, 
                TextDpi.FromTextDpi(durLine), 
                out iLineBestVariant
                );

            StructuralCache.TextFormatterHost.Context = null;

            return textBreakpoints;
        }

        /// <summary>
        /// ReconstructLineVariant - Reconstruct line variants for a given line.
        /// </summary>
        /// <param name="paraClient">
        /// IN: Text para client for TextParagraph
        /// </param>
        /// <param name="iArea">
        /// IN: column-span area index
        /// </param>
        /// <param name="dcp">
        /// IN: dcp at the beginning of the line
        /// </param>
        /// <param name="pbrlineIn">
        /// IN: client's line break record
        /// </param>
        /// <param name="dcpLineIn">
        /// IN: dcp of line to format
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="urStartLine">
        /// IN: position at the beginning of the line
        /// </param>
        /// <param name="durLine">
        /// IN: maximum width of line
        /// </param>
        /// <param name="urStartTrack">
        /// IN: position at the beginning of the track
        /// </param>
        /// <param name="durTrack">
        /// IN: width of track
        /// </param>
        /// <param name="urPageLeftMargin">
        /// IN: left margin of the page
        /// </param>
        /// <param name="fAllowHyphenation">
        /// IN: allow hyphenation of the line?
        /// </param>
        /// <param name="fClearOnLeft">
        /// IN: is clear on left side
        /// </param>
        /// <param name="fClearOnRight">
        /// IN: is clear on right side
        /// </param>
        /// <param name="fTreatAsFirstInPara">
        /// IN: treat line as first line in paragraph
        /// </param>
        /// <param name="fTreatAsLastInPara">
        /// IN: treat line as last line in paragraph
        /// </param>
        /// <param name="fSuppressTopSpace">
        /// IN: suppress empty space at the top of page
        /// </param>
        /// <param name="lineHandle">
        /// OUT: pointer to line created by client
        /// </param>
        /// <param name="dcpLine">
        /// OUT: dcp consumed by the line
        /// </param>
        /// <param name="ppbrlineOut">
        /// OUT: client's line break record
        /// </param>
        /// <param name="fForcedBroken">
        /// OUT: was line force-broken?
        /// </param>
        /// <param name="fsflres">
        /// OUT: result of formatting
        /// </param>
        /// <param name="dvrAscent">
        /// OUT: ascent of the line
        /// </param>
        /// <param name="dvrDescent">
        /// OUT: descent of the line
        /// </param>
        /// <param name="urBBox">
        /// OUT: ur of the line's ink
        /// </param>
        /// <param name="durBBox">
        /// OUT: dur of of the line's ink
        /// </param>
        /// <param name="dcpDepend">
        /// OUT: number of chars after line break that were considered
        /// </param>
        /// <param name="fReformatNeighborsAsLastLine">
        /// OUT: should line segments be reformatted?
        /// </param>
        internal void ReconstructLineVariant(
            TextParaClient paraClient,          
            int iArea,                           
            int dcp,                             
            IntPtr pbrlineIn,                    
            int dcpLineIn,
            uint fswdir,                         
            int urStartLine,                     
            int durLine,                         
            int urStartTrack,                    
            int durTrack,                        
            int urPageLeftMargin,                
            bool fAllowHyphenation,              
            bool fClearOnLeft,                   
            bool fClearOnRight,                  
            bool fTreatAsFirstInPara,            
            bool fTreatAsLastInPara,             
            bool fSuppressTopSpace,              
            out IntPtr lineHandle,               
            out int dcpLine,                     
            out IntPtr ppbrlineOut,              
            out int fForcedBroken,               
            out PTS.FSFLRES fsflres,             
            out int dvrAscent,                   
            out int dvrDescent,                  
            out int urBBox,                      
            out int durBBox,                     
            out int dcpDepend,                   
            out int fReformatNeighborsAsLastLine) 
        {
            // NOTE: there is no empty space added at the top of lines, so fSuppressTopSpace is never used.

            Invariant.Assert(iArea == 0);

            StructuralCache.CurrentFormatContext.OnFormatLine();

            // Create and format line
#pragma warning disable 6518
            // Disable PRESharp warning 6518. Line is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
            // calls DestroyLine to get rid of it. DestroyLine will call Dispose() on the object
            // and remove it from HandleMapper.
            Line line = new Line(StructuralCache.TextFormatterHost, paraClient, ParagraphStartCharacterPosition);
#pragma warning restore 6518

            Line.FormattingContext ctx = new Line.FormattingContext(true, fClearOnLeft, fClearOnRight, _textRunCache);
            ctx.LineFormatLengthTarget = dcpLineIn;
            FormatLineCore(line, pbrlineIn, ctx, dcp, durLine, durTrack, fTreatAsFirstInPara, dcp);

            // Retrieve line properties
            lineHandle = line.Handle;
            dcpLine = line.SafeLength;

            TextLineBreak textLineBreak = line.GetTextLineBreak();

            if(textLineBreak != null)
            {
#pragma warning disable 56518
                // Disable PRESharp warning 6518. Line is an UnmamangedHandle, that adds itself
                // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
                // calls DestroyLineBreakRecord to get rid of it. DestroyLineBreakRecord will call Dispose() on the object
                // and remove it from HandleMapper.
                LineBreakRecord lineBreakRecord = new LineBreakRecord(PtsContext, textLineBreak);
#pragma warning disable 56518

                ppbrlineOut = lineBreakRecord.Handle;
            }
            else
            {
                ppbrlineOut = IntPtr.Zero;
            }

            fForcedBroken = PTS.FromBoolean(line.IsTruncated);
            fsflres = line.FormattingResult;
            dvrAscent = line.Baseline;
            dvrDescent = line.Height - line.Baseline;
            urBBox = urStartLine + line.Start;
            durBBox = line.Width;
            dcpDepend = line.DependantLength;
            fReformatNeighborsAsLastLine = PTS.False;
            // NOTE: When LL provides support for line height, following descent
            //       calculation will go away.
            CalcLineAscentDescent(dcp, ref dvrAscent, ref dvrDescent);

            // Ensure we don't include the paragraph break synthetic run into our DcpDepend calculation.
            // We can only trim to total text length, as _cch may not be calculated at this time, and if it's uncalculated, then ParagraphEndCharacterPosition
            // is potentially incorrect. All of this needs to be reviewed WRT TextSchema.
            int dcpDependAbsolute = ParagraphStartCharacterPosition + dcp + line.ActualLength + dcpDepend;
            int textSize = StructuralCache.TextContainer.SymbolCount;
            if (dcpDependAbsolute > textSize)
                dcpDependAbsolute = textSize;

            StructuralCache.CurrentFormatContext.DependentMax = StructuralCache.TextContainer.CreatePointerAtOffset(dcpDependAbsolute, LogicalDirection.Backward);

#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                TextPanelDebug.Log("TextPara.FormatLine, Start=" + dcp + " Cch=" + dcpLine, TextPanelDebug.Category.ContentChange);
            }
#endif
        }


        /// <summary>
        /// Format line
        /// </summary>
        /// <param name="paraClient">
        /// IN: Text para client for TextParagraph
        /// </param>
        /// <param name="iArea">
        /// IN: column-span area index
        /// </param>
        /// <param name="dcp">
        /// IN: dcp at the beginning of the line
        /// </param>
        /// <param name="pbrlineIn">
        /// IN: client's line break record
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="urStartLine">
        /// IN: position at the beginning of the line
        /// </param>
        /// <param name="durLine">
        /// IN: maximum width of line
        /// </param>
        /// <param name="urStartTrack">
        /// IN: position at the beginning of the track
        /// </param>
        /// <param name="durTrack">
        /// IN: width of track
        /// </param>
        /// <param name="urPageLeftMargin">
        /// IN: left margin of the page
        /// </param>
        /// <param name="fAllowHyphenation">
        /// IN: allow hyphenation of the line?
        /// </param>
        /// <param name="fClearOnLeft">
        /// IN: is clear on left side
        /// </param>
        /// <param name="fClearOnRight">
        /// IN: is clear on right side
        /// </param>
        /// <param name="fTreatAsFirstInPara">
        /// IN: treat line as first line in paragraph
        /// </param>
        /// <param name="fTreatAsLastInPara">
        /// IN: treat line as last line in paragraph
        /// </param>
        /// <param name="fSuppressTopSpace">
        /// IN: suppress empty space at the top of page
        /// </param>
        /// <param name="lineHandle">
        /// OUT: pointer to line created by client
        /// </param>
        /// <param name="dcpLine">
        /// OUT: dcp consumed by the line
        /// </param>
        /// <param name="ppbrlineOut">
        /// OUT: client's line break record
        /// </param>
        /// <param name="fForcedBroken">
        /// OUT: was line force-broken?
        /// </param>
        /// <param name="fsflres">
        /// OUT: result of formatting
        /// </param>
        /// <param name="dvrAscent">
        /// OUT: ascent of the line
        /// </param>
        /// <param name="dvrDescent">
        /// OUT: descent of the line
        /// </param>
        /// <param name="urBBox">
        /// OUT: ur of the line's ink
        /// </param>
        /// <param name="durBBox">
        /// OUT: dur of of the line's ink
        /// </param>
        /// <param name="dcpDepend">
        /// OUT: number of chars after line break that were considered
        /// </param>
        /// <param name="fReformatNeighborsAsLastLine">
        /// OUT: should line segments be reformatted?
        /// </param>
        internal void FormatLine(
            TextParaClient paraClient,          
            int iArea,                           
            int dcp,                             
            IntPtr pbrlineIn,                    
            uint fswdir,                         
            int urStartLine,                     
            int durLine,                         
            int urStartTrack,                    
            int durTrack,                        
            int urPageLeftMargin,                
            bool fAllowHyphenation,              
            bool fClearOnLeft,                   
            bool fClearOnRight,                  
            bool fTreatAsFirstInPara,            
            bool fTreatAsLastInPara,             
            bool fSuppressTopSpace,              
            out IntPtr lineHandle,               
            out int dcpLine,                     
            out IntPtr ppbrlineOut,              
            out int fForcedBroken,               
            out PTS.FSFLRES fsflres,             
            out int dvrAscent,                   
            out int dvrDescent,                  
            out int urBBox,                      
            out int durBBox,                     
            out int dcpDepend,                   
            out int fReformatNeighborsAsLastLine) 
        {
            // NOTE: there is no empty space added at the top of lines, so fSuppressTopSpace is never used.

            Invariant.Assert(iArea == 0);

            StructuralCache.CurrentFormatContext.OnFormatLine();

            // Create and format line
#pragma warning disable 6518
            // Disable PRESharp warning 6518. Line is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
            // calls DestroyLine to get rid of it. DestroyLine will call Dispose() on the object
            // and remove it from HandleMapper.
            Line line = new Line(StructuralCache.TextFormatterHost, paraClient, ParagraphStartCharacterPosition);
#pragma warning restore 6518
            Line.FormattingContext ctx = new Line.FormattingContext(true, fClearOnLeft, fClearOnRight, _textRunCache);
            FormatLineCore(line, pbrlineIn, ctx, dcp, durLine, durTrack, fTreatAsFirstInPara, dcp);

            // Retrieve line properties
            lineHandle = line.Handle;
            dcpLine = line.SafeLength;

            TextLineBreak textLineBreak = line.GetTextLineBreak();

            if(textLineBreak != null)
            {
#pragma warning disable 56518
                // Disable PRESharp warning 6518. Line is an UnmamangedHandle, that adds itself
                // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
                // calls DestroyLineBreakRecord to get rid of it. DestroyLineBreakRecord will call Dispose() on the object
                // and remove it from HandleMapper.
                LineBreakRecord lineBreakRecord = new LineBreakRecord(PtsContext, textLineBreak);
#pragma warning restore 56518

                ppbrlineOut = lineBreakRecord.Handle;
            }
            else
            {
                ppbrlineOut = IntPtr.Zero;
            }

            fForcedBroken = PTS.FromBoolean(line.IsTruncated);
            fsflres = line.FormattingResult;
            dvrAscent = line.Baseline;
            dvrDescent = line.Height - line.Baseline;
            urBBox = urStartLine + line.Start;
            durBBox = line.Width;
            dcpDepend = line.DependantLength;
            fReformatNeighborsAsLastLine = PTS.False;
            // NOTE: When LL provides support for line height, following descent
            //       calculation will go away.
            CalcLineAscentDescent(dcp, ref dvrAscent, ref dvrDescent);

            // Ensure we don't include the paragraph break synthetic run into our DcpDepend calculation.
            // We can only trim to total text length, as _cch may not be calculated at this time, and if it's uncalculated, then ParagraphEndCharacterPosition
            // is potentially incorrect. All of this needs to be reviewed WRT TextSchema.
            int dcpDependAbsolute = ParagraphStartCharacterPosition + dcp + line.ActualLength + dcpDepend;
            int textSize = StructuralCache.TextContainer.SymbolCount;
            if (dcpDependAbsolute > textSize)
                dcpDependAbsolute = textSize;

            StructuralCache.CurrentFormatContext.DependentMax = StructuralCache.TextContainer.CreatePointerAtOffset(dcpDependAbsolute, LogicalDirection.Backward);

#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                TextPanelDebug.Log("TextPara.FormatLine, Start=" + dcp + " Cch=" + dcpLine, TextPanelDebug.Category.ContentChange);
            }
#endif
        }


        /// <summary>
        /// UpdGetChangeInText 
        /// </summary>
        /// <param name="dcpStart">
        /// OUT: start of change
        /// </param>
        /// <param name="ddcpOld">
        /// OUT: number of chars in old range
        /// </param>
        /// <param name="ddcpNew">
        /// OUT: number of chars in new range
        /// </param>
        internal void UpdGetChangeInText(
            out int dcpStart,                    
            out int ddcpOld,                     
            out int ddcpNew)                     
        {
            // Get dtr list for the text presenter
            DtrList dtrs = StructuralCache.DtrsFromRange(ParagraphStartCharacterPosition, LastFormatCch);
            if (dtrs != null)
            {
                // Union all dtrs. Note: there are no overlapping entries in the list of DTRs.
                dcpStart = dtrs[0].StartIndex - ParagraphStartCharacterPosition;
                ddcpNew  = dtrs[0].PositionsAdded;
                ddcpOld  = dtrs[0].PositionsRemoved;
                if (dtrs.Length > 1)
                {
                    for (int i = 1; i < dtrs.Length; i++)
                    {
                        int delta = dtrs[i].StartIndex - dtrs[i-1].StartIndex;
                        ddcpNew += delta + dtrs[i].PositionsAdded;
                        ddcpOld += delta + dtrs[i].PositionsRemoved;
                    }
                }

                // Get rid of embedded objects within dirty range and
                // update dcp of all object which are following dirty range
                // For finite page paragraph is reformatted from scrach, so all caches
                // are up to date and PTS needs only info about the change.
                if (!StructuralCache.CurrentFormatContext.FinitePage)
                {
                    UpdateEmbeddedObjectsCache(ref _attachedObjects, dcpStart, ddcpOld, ddcpNew - ddcpOld);
                    UpdateEmbeddedObjectsCache(ref _inlineObjects, dcpStart, ddcpOld, ddcpNew - ddcpOld);
                }

                Invariant.Assert(dcpStart >= 0 && Cch >= dcpStart && LastFormatCch >= dcpStart);

                // Max out at possible number of chars in old and new ranges, adding one for EOP dcp.
                ddcpOld = Math.Min(ddcpOld, (LastFormatCch - dcpStart) + 1);
                ddcpNew = Math.Min(ddcpNew, (Cch - dcpStart) + 1);
            }
            else
            {
                // PTS may call this callback for paragraph which has not been changed in
                // case of complex page layout (with figures). Return 0 to notify that
                // there is no change.
                dcpStart = ddcpOld = ddcpNew = 0;
            }
#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                TextPanelDebug.Log("TextPara.UpdGetChangeInText, Start=" + dcpStart + " Old=" + ddcpOld + " New=" + ddcpNew, TextPanelDebug.Category.ContentChange);
            }
#endif
        }

        /// <summary>
        /// Get Dvr Advance 
        /// </summary>
        /// <param name="dcp">
        /// IN: dcp at the beginning of the line
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="dvr">
        /// OUT: advance amount in tight wrap
        /// </param>
        internal void GetDvrAdvance(
            int dcp,                            
            uint fswdir,                        
            out int dvr)                         
        {
            EnsureLineProperties();

            // When tight wrap is enabled, PTS may not fit line that starts at 'dcp'
            // at the current vertical offset.
            // In this situation PTS asks the client by how much it needs to
            // advance in vertical direction to try again.
            // For optimal results 1px should be enough, but this has performance hit.
            // Word decide to use height a character in the paragraph font. The similar
            // logic is used here. Advance by value of the default line height.
            dvr = TextDpi.ToTextDpi(_lineProperties.CalcLineAdvanceForTextParagraph(this, dcp, _lineProperties.DefaultTextRunProperties.FontRenderingEmSize));
        }

        /// <summary>
        /// Walks the text tree from a given dcp and skips over figure and floater elements, returning the dcp after the last one.
        /// </summary>
        internal int GetLastDcpAttachedObjectBeforeLine(int dcpFirst)
        {
            ITextPointer textPointer = TextContainerHelper.GetTextPointerFromCP(StructuralCache.TextContainer, ParagraphStartCharacterPosition + dcpFirst, LogicalDirection.Forward);
            ITextPointer textPointerContentStart = TextContainerHelper.GetContentStart(StructuralCache.TextContainer, Element);

            while(textPointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
            {
                TextElement element = ((TextPointer)textPointer).GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);

                if(!(element is Figure) && !(element is Floater))
                {
                    break;
                }

                textPointer.MoveByOffset(element.SymbolCount);
            }
            return textPointerContentStart.GetOffsetToPosition(textPointer);
        }

        /// <summary>
        /// Returns the text elements for a given dcp range.
        /// </summary>
        private List<TextElement> GetAttachedObjectElements(int dcpFirst, int dcpLast)
        {
            List<TextElement> attachedElements = new List<TextElement>();
            ITextPointer textPointerContentStart = TextContainerHelper.GetContentStart(StructuralCache.TextContainer, Element);
            ITextPointer textPointer = TextContainerHelper.GetTextPointerFromCP(StructuralCache.TextContainer, ParagraphStartCharacterPosition + dcpFirst, LogicalDirection.Forward);

            if(dcpLast > this.Cch)
            {
                dcpLast = this.Cch; // Remove end of paragraph run cp.
            }

            while(textPointerContentStart.GetOffsetToPosition(textPointer) < dcpLast)
            {
                if(textPointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    TextElement element = ((TextPointer)textPointer).GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);

                    if(element is Figure || element is Floater)
                    {
                        attachedElements.Add(element);
                        textPointer.MoveByOffset(element.SymbolCount);
                    }
                    else
                    {
                        textPointer.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
}
                else
                {
                    textPointer.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }

            return attachedElements;
        }

        /// <summary>
        /// Returns the count of attached objects over a given dcp range
        /// </summary>
        internal int GetAttachedObjectCount(int dcpFirst, int dcpLast)
        {
            List<TextElement> textElements = GetAttachedObjectElements(dcpFirst, dcpLast);

            if(textElements.Count == 0)
            {
                SubmitAttachedObjects(dcpFirst, dcpLast, null);
            }

            return textElements.Count;
        }

        /// <summary>
        /// Returns the attached object list for a dcp range by wrapping floating text elements in Figure/FloaterObject tags.
        /// </summary>
        internal List<AttachedObject> GetAttachedObjects(int dcpFirst, int dcpLast)
        {
            ITextPointer textPointerContentStart = TextContainerHelper.GetContentStart(StructuralCache.TextContainer, Element);
            List<AttachedObject> attachedObjects = new List<AttachedObject>();
            List<TextElement> textElements = GetAttachedObjectElements(dcpFirst, dcpLast);

            for(int index = 0; index < textElements.Count; index++)
            {
                TextElement textElement = textElements[index];

                if(textElement is Figure && StructuralCache.CurrentFormatContext.FinitePage)
                {
#pragma warning disable 6518
                    // Disable PRESharp warning 6518. FigureParagraph is passed to attached objects
                    // which will do following:
                    // a) store this object in TextParagraph._floaters collection. Later when
                    //    TextParagraph is disposed, all objects in _floaters collection will be
                    //    also disposed.
                    // b) call Dispose() on this object, if it already exists in TextParagraph._floaters
                    //    collection.
                    // c) call Dipose() on this object during layout pass following removal of floater.

                    FigureParagraph figurePara = new FigureParagraph(textElement, StructuralCache);

#pragma warning restore 6518

                    if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
                    {
                        figurePara.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                    }
                    FigureObject figureObject = new FigureObject(textPointerContentStart.GetOffsetToPosition(textElement.ElementStart), figurePara);
                    attachedObjects.Add(figureObject);
                }
                else
                {
#pragma warning disable 6518
                    // Disable PRESharp warning 6518. FigureParagraph is passed to attached objects
                    // which will do following:
                    // a) store this object in TextParagraph._floaters collection. Later when
                    //    TextParagraph is disposed, all objects in _floaters collection will be
                    //    also disposed.
                    // b) call Dispose() on this object, if it already exists in TextParagraph._floaters
                    //    collection.
                    // c) call Dipose() on this object during layout pass following removal of floater.

                    FloaterParagraph floaterPara = new FloaterParagraph(textElement, StructuralCache);

#pragma warning restore 6518

                    if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
                    {
                        floaterPara.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                    }
                    FloaterObject floaterObject = new FloaterObject(textPointerContentStart.GetOffsetToPosition(textElement.ElementStart), floaterPara);
                    attachedObjects.Add(floaterObject);
                }
            }

            // If it were 0, should have been submitted when count was queried.
            if(attachedObjects.Count != 0)
            {
                SubmitAttachedObjects(dcpFirst, dcpLast, attachedObjects);
            }

            return attachedObjects;
        }


        #endregion PTS callbacks

        // ------------------------------------------------------------------
        //
        //  Internal Methods
        //
        // ------------------------------------------------------------------

        #region Internal Methods
        
        /// <summary>
        /// Submit inline objects for specified range to the cache. All existing
        /// inline objects in this particular range will be removed. 
        /// </summary>
        /// <param name="dcpStart">
        /// Dcp of the beginning of the range to update. 
        /// </param>
        /// <param name="dcpLim">
        /// Dcp of the end of the range to update.
        /// </param>
        /// <param name="inlineObjects">
        /// Array of inline objects.
        /// </param>
        internal void SubmitInlineObjects(int dcpStart, int dcpLim, List<InlineObject> inlineObjects)
        {
            SubmitEmbeddedObjects(ref _inlineObjects, dcpStart, dcpLim, inlineObjects);
        }

        /// <summary>
        /// Submit floaters for specified range to the cache. All existing
        /// floaters in this particular range will be removed. 
        /// </summary>
        /// <param name="dcpStart">
        /// Dcp of the beginning of the range to update. 
        /// </param>
        /// <param name="dcpLim">
        /// Dcp of the end of the range to update.
        /// </param>
        /// <param name="attachedObjects">
        /// Array of attached objects.
        /// </param>
        internal void SubmitAttachedObjects(int dcpStart, int dcpLim, List<AttachedObject> attachedObjects)
        {
            SubmitEmbeddedObjects(ref _attachedObjects, dcpStart, dcpLim, attachedObjects);
        }

        /// <summary>
        /// Returns a list of inline object from specifed range 
        /// </summary>
        /// <param name="dcpStart">
        /// Dcp of the beginning of the range.
        /// </param>
        /// <param name="dcpLast">
        /// Dcp of the end of the range.
        /// </param>
        internal List<InlineObject> InlineObjectsFromRange(int dcpStart, int dcpLast)
        {
            List<InlineObject> objects = null;
            if (_inlineObjects != null)
            {
                objects = new List<InlineObject>(_inlineObjects.Count);
                for (int i = 0; i < _inlineObjects.Count; i++)
                {
                    InlineObject obj = _inlineObjects[i];
                    if (obj.Dcp >= dcpStart && obj.Dcp < dcpLast)
                    {
                        objects.Add(obj);
                    }
                    else if (obj.Dcp >= dcpLast)
                    {
                        // No reason to continue
                        break;
                    }
                }
            }

            if(objects == null || objects.Count == 0)
            {
                return null;
            }

            return objects;
        }

        /// <summary>
        /// Calculate and return line advance distance. This functionality will go away
        /// when TextFormatter will be able to handle line height/stacking. 
        /// </summary>
        /// <param name="dcp">
        /// dcp of the line
        /// </param>
        /// <param name="dvrAscent">
        /// Calculated dvr ascent 
        /// </param>
        /// <param name="dvrDescent">
        /// Calculated dvr descent
        /// </param>
        internal void CalcLineAscentDescent(int dcp, ref int dvrAscent, ref int dvrDescent)
        {
            EnsureLineProperties();

            int thisLineAdvance = dvrAscent + dvrDescent;
            int calculatedLineAdvance = TextDpi.ToTextDpi(_lineProperties.CalcLineAdvanceForTextParagraph(this, dcp, TextDpi.FromTextDpi(thisLineAdvance)));

            if(thisLineAdvance != calculatedLineAdvance)
            {
                double scale = (1.0 * calculatedLineAdvance) / (1.0 * thisLineAdvance);
                dvrAscent = (int) (dvrAscent * scale);
                dvrDescent = (int) (dvrDescent * scale);
            }
        }

        /// <summary>
        /// Set update info. Those flags are used later by PTS to decide
        /// if paragraph needs to be updated and when to stop asking for
        /// update information. 
        /// </summary>
        /// <param name="fskch">
        /// Type of change within the paragraph.
        /// </param>
        /// <param name="stopAsking">
        /// Synchronization point is reached?
        /// </param>
        internal override void SetUpdateInfo(PTS.FSKCHANGE fskch, bool stopAsking)
        {
            base.SetUpdateInfo(fskch, stopAsking);

            if (fskch == PTS.FSKCHANGE.fskchInside)
            {
                // Update _cch so we always have correct value during update process
                _textRunCache = new TextRunCache();
                _lineProperties = null;
            }
        }

        /// <summary>
        /// Clear previously accumulated update info. 
        /// </summary>
        internal override void ClearUpdateInfo()
        {
            base.ClearUpdateInfo();

            // Clear update info of all floaters and figures
            if (_attachedObjects != null)
            {
                for (int index=0; index < _attachedObjects.Count; index++)
                {
                    _attachedObjects[index].Para.ClearUpdateInfo();
                }
            }
        }

        /// <summary>
        /// Invalidate content's structural cache. Returns true if entire paragraph is invalid. 
        /// </summary>
        /// <param name="startPosition">
        /// Position to start invalidation from.
        /// </param>
        internal override bool InvalidateStructure(int startPosition)
        {
            // Change must be inside text paragraph.
            Invariant.Assert(ParagraphEndCharacterPosition >= startPosition);

            bool invalid = false;

            // Thre are 2 situations:
            // 1) the beginning is startPosition, in this case paragraph content is valid only if
            //    the first element in the para is figure/floater and the element owner has not
            //    been changed.
            // 2) startPosition is in the middle of paragraph, in this case paragraph content is valid.
            if (ParagraphStartCharacterPosition == startPosition)
            {
                // 1) the beginning is startPosition, in this case paragraph content is valid only if
                //    the first element in the para is figure/floater and the element owner has not
                //    been changed.

                invalid = true;

                // Get element owner of the first figure or floater, whichever comes first
                AnchoredBlock objectElement = null;

                if(_attachedObjects != null && _attachedObjects.Count > 0)
                {
                    objectElement = (AnchoredBlock)(_attachedObjects[0].Element);
                }

                // If figure/floater starts at the beginning of paragraph and element owner did
                // not change, treat the paragraph as valid.
                if (objectElement != null)
                {
                    if (startPosition == objectElement.ElementStartOffset)
                    {
                        StaticTextPointer position = TextContainerHelper.GetStaticTextPointerFromCP(StructuralCache.TextContainer, startPosition);
                        if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                        {
                            invalid = (objectElement != position.GetAdjacentElement(LogicalDirection.Forward));
                        }
                        // else invalid
                    }
                    // else invalid
                }
                // else invalid
            }
            // else
            // 2) startPosition is in the middle of paragraph, in this case paragraph content is valid.

            // Invalidate text format caches.
            InvalidateTextFormatCache();

            // Invalidate structure of floaters and figures
            if (_attachedObjects != null)
            {
                for (int index=0; index < _attachedObjects.Count; index++)
                {
                    BaseParagraph attachedObjectPara = _attachedObjects[index].Para;

                    if(attachedObjectPara.ParagraphEndCharacterPosition >= startPosition)
                    {
                        attachedObjectPara.InvalidateStructure(startPosition);
                    }
                }
            }

            return invalid;
        }

        /// <summary>
        /// Invalidate accumulated format caches.
        /// </summary>
        internal override void InvalidateFormatCache()
        {
            InvalidateTextFormatCache();

            // Invalidate structure of floaters and figures
            if (_attachedObjects != null)
            {
                for (int index=0; index < _attachedObjects.Count; index++)
                {
                    _attachedObjects[index].Para.InvalidateFormatCache();
                }
            }
        }

        /// <summary>
        /// Invalidate format cache.
        /// </summary>
        internal void InvalidateTextFormatCache()
        {
            _textRunCache = new TextRunCache();
            _lineProperties = null;
        }

        /// <summary>
        /// Format text line. 
        /// </summary>
        /// <param name="line">
        /// Text line to format
        /// </param>
        /// <param name="pbrLineIn">
        /// Break record of line
        /// </param>
        /// <param name="ctx">
        /// TextFormatter context
        /// </param>
        /// <param name="dcp">
        /// Dcp of line start
        /// </param>
        /// <param name="width">
        /// Width of line
        /// </param>
        /// <param name="firstLine">
        /// First line in paragraph?
        /// </param>
        /// <param name="dcpLine">
        /// Character position where the line starts.
        /// </param>
        internal void FormatLineCore(Line line, IntPtr pbrLineIn, Line.FormattingContext ctx, int dcp, int width, bool firstLine, int dcpLine)
        {
            FormatLineCore(line, pbrLineIn, ctx, dcp, width, -1, firstLine, dcpLine);
        }

        /// <summary>
        /// Format text line.  Includes track width, needed during Measure to
        /// size inline elements. 
        /// </summary>
        /// <param name="line">
        /// Text line to be formatted
        /// </param>
        /// <param name="pbrLineIn">
        /// Break record of line
        /// </param>
        /// <param name="ctx">
        /// TextFormatter context
        /// </param>
        /// <param name="dcp">
        /// dcp of line start
        /// </param>
        /// <param name="width">
        /// Line width
        /// </param>
        /// <param name="trackWidth">
        /// Requested width of track
        /// </param>
        /// <param name="firstLine">
        /// First line in paragraph?
        /// </param>
        /// <param name="dcpLine">
        /// Character position where the line starts.
        /// </param>
        internal void FormatLineCore(Line line, IntPtr pbrLineIn, Line.FormattingContext ctx, int dcp, int width, int trackWidth, bool firstLine, int dcpLine)
        {
            TextDpi.EnsureValidLineWidth(ref width);
            _currentLine = line;

            TextLineBreak textLineBreak = null;
            if(pbrLineIn != IntPtr.Zero)
            {
                LineBreakRecord lineBreakRecord = PtsContext.HandleToObject(pbrLineIn) as LineBreakRecord;
                PTS.ValidateHandle(lineBreakRecord);

                textLineBreak = lineBreakRecord.TextLineBreak;
            }

            try
            {
                line.Format(ctx, dcp, width, trackWidth, GetLineProperties(firstLine, dcpLine), textLineBreak);
            }
            finally
            {
                _currentLine = null;
            }
        }

        /// <summary>
        /// Measure child UIElement and return its size.
        /// </summary>
        /// <param name="inlineObject">
        /// InlineObjectRun with child UIElement to measure
        /// </param>
        internal Size MeasureChild(InlineObjectRun inlineObject)
        {
            if(_currentLine == null)
            {
                return ((OptimalTextSource)StructuralCache.TextFormatterHost.Context).MeasureChild(inlineObject);
            }
            else
            {
                return _currentLine.MeasureChild(inlineObject);               
            }
        }   

        /// <summary>
        /// Returns true if there's anything complicated about this para - figures, floaters
        /// or inline objects. Returns false if para contains simple text.
        /// </summary>
        internal bool HasFiguresFloatersOrInlineObjects()
        {
            if(HasFiguresOrFloaters() || (_inlineObjects != null && _inlineObjects.Count > 0))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if figures or floaters exist for this para
        /// </summary>
        internal bool HasFiguresOrFloaters()
        {
            return _attachedObjects != null && _attachedObjects.Count > 0;
        }


        /// <summary>
        /// Updates text content range with attached object list. Subtracts out all of the known figures and floaters
        /// Ranges, then adds back in the ranges for the para clients.
        /// </summary>
        internal void UpdateTextContentRangeFromAttachedObjects(TextContentRange textContentRange, int dcpFirst, int dcpLast, PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc)
        {
            int cpCur = dcpFirst;

            for(int index = 0; _attachedObjects != null && index < _attachedObjects.Count; index++)                
            {
                AttachedObject attachedObject = _attachedObjects[index];

                int startContentPosition = attachedObject.Para.ParagraphStartCharacterPosition;
                int paraCch = attachedObject.Para.Cch;

                if(startContentPosition >= cpCur && startContentPosition < dcpLast)
                {
                    textContentRange.Merge(new TextContentRange(cpCur, startContentPosition, StructuralCache.TextContainer));
                    cpCur = startContentPosition + paraCch; // Skip past para content range
                }

                if(dcpLast < cpCur)
                {
                    break;
                }
            }

            if(cpCur < dcpLast)
            {
                textContentRange.Merge(new TextContentRange(cpCur, dcpLast, StructuralCache.TextContainer));
            }

            for(int index = 0; arrayAttachedObjectDesc != null && index < arrayAttachedObjectDesc.Length; index++)
            {
                PTS.FSATTACHEDOBJECTDESCRIPTION attachedObject = arrayAttachedObjectDesc[index];
                BaseParaClient paraClient;

                paraClient = PtsContext.HandleToObject(arrayAttachedObjectDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);

                textContentRange.Merge(paraClient.GetTextContentRange());
            }
        }

        /// <summary>
        /// Handler for DesiredSizeChanged raised by UIElement island.
        /// </summary>
        internal void OnUIElementDesiredSizeChanged(object sender, DesiredSizeChangedEventArgs e)
        {
            StructuralCache.FormattingOwner.OnChildDesiredSizeChanged(e.Child);
        }

        #endregion Internal Methods

        // ------------------------------------------------------------------
        //
        //  Internal Properties
        //
        // ------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Run cache used by text formatter.
        /// </summary>
        internal TextRunCache TextRunCache 
        { 
            get 
            { 
                return _textRunCache; 
            } 
        }

        /// <summary>
        /// Text paragraph properties.
        /// </summary>
        internal LineProperties Properties
        {
            get
            {
                EnsureLineProperties();
                return _lineProperties;
            }
        }

        /// <summary>
        /// Optimal paragraph flag
        /// </summary>
        internal bool IsOptimalParagraph 
        { 
            get 
            {
                return StructuralCache.IsOptimalParagraphEnabled && GetLineProperties(false, 0).TextWrapping != TextWrapping.NoWrap; 
            } 
        }

        #endregion Internal Properties

        // ------------------------------------------------------------------
        //
        //  Private Methods
        //
        // ------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Refetch and cache line properties, if needed. 
        /// </summary>
        private void EnsureLineProperties()
        {
            // We also need to recreate line properties if DPI has changed.
            if (_lineProperties == null || (_lineProperties != null && _lineProperties.DefaultTextRunProperties.PixelsPerDip != StructuralCache.TextFormatterHost.PixelsPerDip))
            {
                // For default text properties always set background to null.
                // REASON: If element associated with the text run is block element, ignore background
                //         brush, because it is handled by paragraph itself.

                // This textProperties object is eventually used in creation of LineProperties, which leads to creation of a TextMarkerSource. TextMarkerSource relies on PixelsPerDip
                // from TextProperties, therefore it must be set here properly.

                TextProperties defaultTextProperties = new TextProperties(Element, StaticTextPointer.Null, false /* inline objects */, false /* get background */,
                    StructuralCache.TextFormatterHost.PixelsPerDip);
                
                _lineProperties = new LineProperties(Element, StructuralCache.FormattingOwner, defaultTextProperties, null); // No marker properties

                bool isHyphenationEnabled = (bool) Element.GetValue(Block.IsHyphenationEnabledProperty);
                if(isHyphenationEnabled)
                {
                    _lineProperties.Hyphenator = StructuralCache.Hyphenator;
                }
            }
        }

        /// <summary>
        /// Submit embedded objects for specified range. All existing objects
        /// in this particular range will be removed. 
        /// </summary>
        /// <param name="objectsCached">
        /// Array of cached embedded objects.
        /// </param>
        /// <param name="dcpStart">
        /// Dcp of the beginning of the range to update.
        /// </param>
        /// <param name="dcpLim">
        /// Dcp of the end of the range to update.
        /// </param>
        /// <param name="objectsNew">
        /// Array of new embedded objects.
        /// </param>
        private void SubmitEmbeddedObjects<T>(ref List<T> objectsCached, int dcpStart, int dcpLim, List<T> objectsNew) where T : EmbeddedObject
        {
            ErrorHandler.Assert(objectsNew == null || (objectsNew[0].Dcp >= dcpStart && objectsNew[objectsNew.Count-1].Dcp <= dcpLim), ErrorHandler.SubmitInvalidList);

            // Make sure that cached objects array exists
            if (objectsCached == null)
            {
                if (objectsNew == null) 
                {
                    // Nothing to do
                    return; 
                } 
                objectsCached = new List<T>(objectsNew.Count);
            }

            // Find affected range of cached objects
            int end = objectsCached.Count;
            while (end > 0 && objectsCached[end-1].Dcp >= dcpLim) --end;
            int start = end;
            while (start > 0 && objectsCached[start-1].Dcp >= dcpStart) --start;

            // There are 3 situations, which may happen when submitting embedded objects:
            // (1) Only remove obsolete objects (no objects to add)
            // (2) Only add new objects (no objects are obsolete)
            // (3) Merge new objects into existing list (may add and remove objects)
            if (objectsNew == null)
            {
                // (1) Only remove obsolete objects (no objects to add)
                for (int index = start; index < end; index++)
                {
                    objectsCached[index].Dispose();
                }
                objectsCached.RemoveRange(start, end - start);
            }
            else if (end == start)
            {
                // (2) Only add new objects (no objects are obsolete)
                objectsCached.InsertRange(start, objectsNew);
            }
            else
            {
                // (3) Merge new objects into existing list (may add and remove objects)
                int idxNew = 0;
                while (start < end)
                {
                    // Iterate through list of existing objects, which are affected by the
                    // change range. There are 2 possibilities:
                    // (1) There is matching object in the list of new objects.
                    // (2) The object is obsolete, it should be removed.
                    T oldEmbeddedObject = objectsCached[start];
                    int idx = idxNew;
                    while (idx < objectsNew.Count)
                    {
                        T newEmbeddedObject = objectsNew[idx];
                        if (oldEmbeddedObject.Element == newEmbeddedObject.Element)
                        {
                            // (1) There is matching object in the list of new objects.
                            //     In this case:
                            //     * insert all preceding objects from new object list
                            //     * update object information
                            if (idx > idxNew)
                            {
                                objectsCached.InsertRange(start, objectsNew.GetRange(idxNew, idx - idxNew));
                                end   += idx - idxNew;
                                start += idx - idxNew;
                            }
                            oldEmbeddedObject.Update(newEmbeddedObject);
                            objectsNew[idx] = oldEmbeddedObject;
                            idxNew = idx + 1;
                            ++start;
                            // Dispose unused EmbeddedObject
                            newEmbeddedObject.Dispose();
                            break;
                        }
                        ++idx;
                    }
                    if (idx >= objectsNew.Count)
                    {
                        // (2) The object is obsolete - remove it.
                        objectsCached[start].Dispose();
                        objectsCached.RemoveAt(start);
                        --end;
                    }
                }
                if (idxNew < objectsNew.Count)
                {
                    // If we have any objects left in the new object list, insert them.
                    objectsCached.InsertRange(end, objectsNew.GetRange(idxNew, objectsNew.Count - idxNew));
                }
            }
        }

        //-------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------

        /// <summary>
        /// Get rid of embedded objects within dirty range and update dcp of
        /// all object which are following dirty range.
        /// </summary>
        /// <param name="objectsCached">
        /// array of cached embedded objects
        /// </param>
        /// <param name="dcpStart">
        /// dcp of the beginning of the range to update
        /// </param>
        /// <param name="cchDeleted">
        /// number of characters deleted
        /// </param>
        /// <param name="cchDiff">
        /// difference in characters count
        /// </param>
        private void UpdateEmbeddedObjectsCache<T>(
            ref List<T> objectsCached,     
            int dcpStart,                    
            int cchDeleted,                  
            int cchDiff) where T : EmbeddedObject
        {
            if (objectsCached != null)
            {
                // Find the first and last affected object
                int first = 0;
                while (first < objectsCached.Count && objectsCached[first].Dcp < dcpStart) 
                { 
                    ++first; 
                }
                int last = first;
                while (last < objectsCached.Count && objectsCached[last].Dcp < dcpStart + cchDeleted) 
                { 
                    ++last; 
                }

                // Remove obsolete embedded objects
                if (first != last)
                {
                    for (int index = first; index < last; index++)
                    {
                        objectsCached[index].Dispose();
                    }
                    objectsCached.RemoveRange(first, last - first);
                }

                // Update dcp of all objects following dirty range
                while (last < objectsCached.Count)
                {
                    objectsCached[last].Dcp += cchDiff;
                    ++last;
                }

                if (objectsCached.Count == 0)
                {
                    objectsCached = null;
                }
            }
        }

        /// <summary>
        /// Get line properties
        /// </summary>
        /// <param name="firstLine">
        /// First line in paragraph? 
        /// </param>
        /// <param name="dcpLine">
        /// Character position where line starts.
        /// </param>
        private TextParagraphProperties GetLineProperties(bool firstLine, int dcpLine)
        {
            EnsureLineProperties();

            if (firstLine && _lineProperties.HasFirstLineProperties)
            {
                // There are 2 situations, where PTS claims that it is the first line.
                // a) first complex composite line - only the first element
                //    should be treated as the first line.
                // b) paragraph nesting - if there are 2 or more TextParagraphs representing
                //    the same Element, only the first one has the first line properties.
                // In those cases need to ignore first line properties.
                if (dcpLine != 0)
                {
                    // a) first complex composite line - only the first element
                    //    should be treated as the first line.
                    firstLine = false;
                }
                else
                {
                    // b) paragraph nesting - if there are 2 or more TextParagraphs representing
                    //    the same Element, only the first one has the first line properties.
                    int cpElement = TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterStart);
                    if (cpElement < this.ParagraphStartCharacterPosition)
                    {
                        firstLine = false;
                    }
                }

                // If we still have the first line, use first line properties.
                if (firstLine)
                {
                    return _lineProperties.FirstLineProps;
                }
            }

            return _lineProperties;
        }

        #endregion Private Methods

        // ------------------------------------------------------------------
        //
        //  Private Fields
        //
        // ------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// List of attached objects.
        /// </summary>
        private List<AttachedObject> _attachedObjects;
#if DEBUG
        internal List<AttachedObject> AttachedObjectDbg { get { return _attachedObjects; } }
#endif

        /// <summary>
        /// List of inline objects.
        /// </summary>
        private List<InlineObject> _inlineObjects;

        /// <summary>
        /// Line properties 
        /// </summary>
        private LineProperties _lineProperties;

        /// <summary>
        /// Run cache used by text formatter. 
        /// </summary>
        private TextRunCache _textRunCache = new TextRunCache();

        /// <summary>
        /// Currently formatted line. Valid only during line formatting. 
        /// </summary>
        private Line _currentLine;

        #endregion Private Fields
    }
}

#pragma warning enable 1634, 1691

