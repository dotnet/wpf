// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: FigureParagraph class provides a wrapper for nested UIElements, 
//              which are treated by PTS as figures.
//              
//              Figures now are finite only.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Security;              // SecurityCritical
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // FigureParagraph class provides a wrapper for nested UIElements, which 
    // are treated by PTS as figures.
    // ----------------------------------------------------------------------
    internal sealed class FigureParagraph : BaseParagraph
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      element - Element associated with paragraph.
        //      structuralCache - Content's structural cache
        // ------------------------------------------------------------------
        internal FigureParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            base.Dispose();

            if (_mainTextSegment != null)
            {
                _mainTextSegment.Dispose();
                _mainTextSegment = null;
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  PTS callbacks
        //
        //-------------------------------------------------------------------

        #region PTS callbacks

        //-------------------------------------------------------------------
        // GetParaProperties
        //-------------------------------------------------------------------
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)                // OUT: paragraph properties
        {
            GetParaProperties(ref fspap, false);
            fspap.idobj = PTS.fsidobjFigure;
        }

        //-------------------------------------------------------------------
        // GetParaProperties
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. FigureParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            FigureParaClient paraClient =  new FigureParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518

            // Create the main text segment
            if (_mainTextSegment == null)
            {
                _mainTextSegment = new ContainerParagraph(Element, StructuralCache);
            }
        }

        //-------------------------------------------------------------------
        // GetFigureProperties
        //-------------------------------------------------------------------
        internal void GetFigureProperties(
            FigureParaClient paraClient,        // IN:
            int fInTextLine,                    // IN:  it is attached to text line
            uint fswdir,                        // IN:  current direction
            int fBottomUndefined,               // IN:  bottom of page is not defined
            out int dur,                        // OUT: width of figure
            out int dvr,                        // OUT: height of figure
            out PTS.FSFIGUREPROPS fsfigprops,   // OUT: figure attributes
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices,                  // OUT: total number of vertices in all polygons
            out int durDistTextLeft,            // OUT: distance to text from MinU side
            out int durDistTextRight,           // OUT: distance to text from MaxU side
            out int dvrDistTextTop,             // OUT: distance to text from MinV side
            out int dvrDistTextBottom)          // OUT: distance to text from MaxV side
        {
            Invariant.Assert(StructuralCache.CurrentFormatContext.FinitePage);

            uint fswdirPara = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            IntPtr pfsFigureContent;
            PTS.FSBBOX fsbbox;
            int cColumns;
            int dvrTopSpace;
            PTS.FSCOLUMNINFO[] columnInfoCollection;
            IntPtr pmcsclientOut;
            MbpInfo mbp;

            Figure element = (Figure)Element;

            // Initialize the subpage size. PTS subpage margin is always set to 0 for Figures.
            // If width on figure is specified, use the specified value.
            // Border and padding of the figure is extracted from available subpage width.
            // We use StructuralCache.CurrentFormatContext's page dimensions as limiting values for figure MBP
            mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);            
            // We do not mirror margin as it's used to dist text left and right, and is unnecessary.

            durDistTextLeft = durDistTextRight = dvrDistTextTop = dvrDistTextBottom = 0;
            
            // Calculate specified width. IsAuto flag is needed because Auto is formatted the same way as column and will
            // not return Double.NaN.
            bool isWidthAuto;
            double specifiedWidth = FigureHelper.CalculateFigureWidth(StructuralCache, element, 
                                                                      element.Width, 
                                                                      out isWidthAuto);


            double anchorLimitedWidth = LimitTotalWidthFromAnchor(specifiedWidth, TextDpi.FromTextDpi(mbp.MarginLeft + mbp.MarginRight));
            int subpageWidth = Math.Max(1, TextDpi.ToTextDpi(anchorLimitedWidth) - (mbp.BPLeft + mbp.BPRight));

            // Calculate figure height, IsAuto flag is used as specifiedHeight will never be NaN.
            bool isHeightAuto;
            double specifiedHeight = FigureHelper.CalculateFigureHeight(StructuralCache, element, 
                                                                        element.Height,
                                                                        out isHeightAuto);

            double anchorLimitedHeight = LimitTotalHeightFromAnchor(specifiedHeight, TextDpi.FromTextDpi(mbp.MarginTop + mbp.MarginBottom));
            int subpageHeight = Math.Max(1, TextDpi.ToTextDpi(anchorLimitedHeight) - (mbp.BPTop + mbp.BPBottom));

            // Initialize column info. Figure always has just 1 column.
            cColumns = 1;
            columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
            columnInfoCollection[0].durBefore = 0;
            columnInfoCollection[0].durWidth = subpageWidth;

            // Create subpage
            {
                PTS.FSFMTR fsfmtr;
                IntPtr brParaOut;
                PTS.FSRECT marginRect = new PTS.FSRECT(0, 0, subpageWidth, subpageHeight);

                CreateSubpageFiniteHelper(PtsContext, IntPtr.Zero, PTS.False, _mainTextSegment.Handle, IntPtr.Zero, PTS.False, PTS.True, 
                    fswdir, subpageWidth, subpageHeight, ref marginRect,
                    cColumns, columnInfoCollection, PTS.False, 
                    out fsfmtr, out pfsFigureContent, out brParaOut, out dvr, out fsbbox, out pmcsclientOut, 
                    out dvrTopSpace);

                if(brParaOut != IntPtr.Zero)
                {
                    PTS.Validate(PTS.FsDestroySubpageBreakRecord(PtsContext.Context, brParaOut));
                }
            }

            // PTS subpage does not support autosizing, but Figure needs to autosize to its
            // content. To workaround this problem, second format of subpage is performed, if 
            // necessary. It means that if the width of bounding box is smaller than subpage's
            // width, second formatting is performed.

            if(PTS.ToBoolean(fsbbox.fDefined))
            {
                if (fsbbox.fsrc.du < subpageWidth && isWidthAuto)
                {
                    // There is a need to reformat PTS subpage, so destroy any resourcces allocated by PTS
                    // during previous formatting.
                    if (pfsFigureContent != IntPtr.Zero)
                    {
                        PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, pfsFigureContent), PtsContext);
                    }
                    if (pmcsclientOut != IntPtr.Zero)
                    {
                        MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                        PTS.ValidateHandle(mcs);
                        mcs.Dispose();
                        pmcsclientOut = IntPtr.Zero;
                    }
                    // Create subpage with new width.
                    subpageWidth = fsbbox.fsrc.du + 1; // add 1/300px to avoid rounding errors
                    columnInfoCollection[0].durWidth = subpageWidth;

                    // Create subpage
                    PTS.FSFMTR fsfmtr;
                    IntPtr brParaOut;
                    PTS.FSRECT marginRect = new PTS.FSRECT(0, 0, subpageWidth, subpageHeight);

                    CreateSubpageFiniteHelper(PtsContext, IntPtr.Zero, PTS.False, _mainTextSegment.Handle, IntPtr.Zero, PTS.False, PTS.True, 
                        fswdir, subpageWidth, subpageHeight, ref marginRect,
                        cColumns, columnInfoCollection, PTS.False, 
                        out fsfmtr, out pfsFigureContent, out brParaOut, out dvr, out fsbbox, out pmcsclientOut, 
                        out dvrTopSpace);

                    if(brParaOut != IntPtr.Zero)
                    {
                        PTS.Validate(PTS.FsDestroySubpageBreakRecord(PtsContext.Context, brParaOut));
                    }
                }
            }
            else
            {
                subpageWidth = TextDpi.ToTextDpi(TextDpi.MinWidth);
            }


            // Get the size of the figure. For height PTS already reports calculated value.
            // But width is the same as subpage width. Include margins in dur since we are not using
            // distance to text anymore.
            dur = subpageWidth + mbp.MBPLeft + mbp.MBPRight;

            // Destroy objects created by PTS, but not used here.
            if (pmcsclientOut != IntPtr.Zero)
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                mcs.Dispose();
                pmcsclientOut = IntPtr.Zero;
            }

            dvr += mbp.MBPTop + mbp.MBPBottom;
            if(!isHeightAuto)
            { 
                // Replace height with explicit height if specified, adding margins in addition to height 
                // Border and padding are included in specified height but margins are external
                dvr = TextDpi.ToTextDpi(anchorLimitedHeight) + mbp.MarginTop + mbp.MarginBottom;
            }

            FigureHorizontalAnchor horzAnchor = element.HorizontalAnchor;
            FigureVerticalAnchor vertAnchor = element.VerticalAnchor;

            fsfigprops.fskrefU = (PTS.FSKREF)(((int)horzAnchor) / 3);
            fsfigprops.fskrefV = (PTS.FSKREF)(((int)vertAnchor) / 3);
            fsfigprops.fskalfU = (PTS.FSKALIGNFIG)(((int)horzAnchor) % 3);
            fsfigprops.fskalfV = (PTS.FSKALIGNFIG)(((int)vertAnchor) % 3);

            // PTS does not allow to anchor delayed figures to 'Character'
            if (!PTS.ToBoolean(fInTextLine))
            {
                if (fsfigprops.fskrefU == PTS.FSKREF.fskrefChar)
                {
                    fsfigprops.fskrefU = PTS.FSKREF.fskrefMargin;
                    fsfigprops.fskalfU = PTS.FSKALIGNFIG.fskalfMin;
                }
                if (fsfigprops.fskrefV == PTS.FSKREF.fskrefChar)
                {
                    fsfigprops.fskrefV = PTS.FSKREF.fskrefMargin;
                    fsfigprops.fskalfV = PTS.FSKALIGNFIG.fskalfMin;
                }
            }

            // Always wrap text on both sides of the floater.
            fsfigprops.fskwrap       = PTS.WrapDirectionToFskwrap(element.WrapDirection);
            fsfigprops.fNonTextPlane = PTS.False;
            fsfigprops.fAllowOverlap = PTS.False;
            fsfigprops.fDelayable = PTS.FromBoolean(element.CanDelayPlacement);

            // Tight wrap is disabled for now.
            cPolygons = cVertices = 0;

            // Update handle to PTS subpage.
            ((FigureParaClient)paraClient).SubpageHandle = pfsFigureContent;
        }

        //-------------------------------------------------------------------
        // GetFigurePolygons
        //-------------------------------------------------------------------
        internal unsafe void GetFigurePolygons(
            FigureParaClient paraClient,        // IN:
            uint fswdir,                        // IN:  current direction
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            PTS.FSPOINT* rgfspt,                // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough)               // OUT: fill text in empty areas within obstacles?
        {
            Debug.Assert(false, "Tight wrap is not currently supported.");
            ccVertices = cfspt = fWrapThrough = 0;
        }

        //-------------------------------------------------------------------
        // CalcFigurePosition
        //-------------------------------------------------------------------
        internal void CalcFigurePosition(
            FigureParaClient paraClient,        // IN:
            uint fswdir,                        // IN:  current direction
            ref PTS.FSRECT fsrcPage,            // IN:  page rectangle
            ref PTS.FSRECT fsrcMargin,          // IN:  rectangle within page margins
            ref PTS.FSRECT fsrcTrack,           // IN:  track rectangle
            ref PTS.FSRECT fsrcFigurePreliminary,// IN:  prelim figure rect calculated from figure props
            int fMustPosition,                  // IN:  must find position in this track?
            int fInTextLine,                    // IN:  it is attached to text line
            out int fPushToNextTrack,           // OUT: push to next track?
            out PTS.FSRECT fsrcFlow,            // OUT: FlowAround rectangle
            out PTS.FSRECT fsrcOverlap,         // OUT: Overlap rectangle
            out PTS.FSBBOX fsbbox,              // OUT: bbox
            out PTS.FSRECT fsrcSearch)          // OUT: search area for overlap
        {
            Figure element = (Figure)Element;

            // If overlapping happens, let PTS find another position withing
            // the track rectangle.

            FigureHorizontalAnchor horizAnchor = element.HorizontalAnchor;
            FigureVerticalAnchor vertAnchor = element.VerticalAnchor;

            fsrcSearch = CalculateSearchArea(horizAnchor, vertAnchor, ref fsrcPage, ref fsrcMargin, ref fsrcTrack, ref fsrcFigurePreliminary);

            if(vertAnchor == FigureVerticalAnchor.ParagraphTop && 
               fsrcFigurePreliminary.v != fsrcMargin.v && // If we're not at the top of the column
               ( (fsrcFigurePreliminary.v + fsrcFigurePreliminary.dv) > (fsrcTrack.v + fsrcTrack.dv) ) && // And we exceed column height
               !PTS.ToBoolean(fMustPosition)) // Can delay placement is handled by figure properties.
            {
                fPushToNextTrack = PTS.True;
            }
            else
            {
                fPushToNextTrack = PTS.False;
            }


            // Use rectangle proposed by PTS and make sure that figure fits completely in the page.

            fsrcFlow = fsrcFigurePreliminary;

            if(FigureHelper.IsHorizontalColumnAnchor(horizAnchor))
            {
                fsrcFlow.u += CalculateParagraphToColumnOffset(horizAnchor, fsrcFigurePreliminary);
            }
            
            // Apply horizontal and vertical offsets. Offsets are limited by page height and width
            fsrcFlow.u += TextDpi.ToTextDpi(element.HorizontalOffset);
            fsrcFlow.v += TextDpi.ToTextDpi(element.VerticalOffset);

            // Overlap rectangle is the same as flow around rect
            fsrcOverlap = fsrcFlow;


            /* If we're anchored to column/content left or right, inflate our overlap width to prevent from aligning two figures right next to one another
            by incorporating column gap information */
            if(!FigureHelper.IsHorizontalPageAnchor(horizAnchor) && 
               horizAnchor != FigureHorizontalAnchor.ColumnCenter &&
               horizAnchor != FigureHorizontalAnchor.ContentCenter)
            {
                double columnWidth, gap, rule;
                int cColumns;

                FigureHelper.GetColumnMetrics(StructuralCache, out cColumns, out columnWidth, out gap, out rule);

                int duColumnWidth = TextDpi.ToTextDpi(columnWidth);
                int duGapWidth = TextDpi.ToTextDpi(gap);
                int duColumnWidthWithGap = duColumnWidth + duGapWidth;
                int fullColumns = (fsrcOverlap.du / duColumnWidthWithGap);
                int duRoundedToNearestColumn = ((fullColumns + 1) * duColumnWidthWithGap) - duGapWidth;

                fsrcOverlap.du = duRoundedToNearestColumn; // Round overlap rect to nearest column

                if(horizAnchor == FigureHorizontalAnchor.ContentRight || 
                   horizAnchor == FigureHorizontalAnchor.ColumnRight)
                {
                    fsrcOverlap.u = (fsrcFlow.u + fsrcFlow.du + duGapWidth) - fsrcOverlap.du;
                }

                // Force search rect to only work vertically within overlap space.
                fsrcSearch.u = fsrcOverlap.u;
                fsrcSearch.du = fsrcOverlap.du;
            }

            // Bounding box is equal to actual size of the figure.
            fsbbox = new PTS.FSBBOX();
            fsbbox.fDefined = PTS.True;
            fsbbox.fsrc = fsrcFlow;
        }
        
        #endregion PTS callbacks


        #region Internal Methods

        // ------------------------------------------------------------------
        // Clear previously accumulated update info.
        // ------------------------------------------------------------------
        internal override void ClearUpdateInfo()
        {
            if (_mainTextSegment != null)
            {
                _mainTextSegment.ClearUpdateInfo();
            }
            base.ClearUpdateInfo();
        }

        // ------------------------------------------------------------------
        // Invalidate content's structural cache.
        //
        //      startPosition - Position to start invalidation from.
        //
        // Returns: 'true' if entire paragraph is invalid.
        // ------------------------------------------------------------------
        internal override bool InvalidateStructure(int startPosition)
        {
            Debug.Assert(ParagraphEndCharacterPosition >= startPosition);
            if (_mainTextSegment != null)
            {
                if (_mainTextSegment.InvalidateStructure(startPosition))
                {
                    _mainTextSegment.Dispose();
                    _mainTextSegment = null;
                }
            }
            return (_mainTextSegment == null);
        }
        
        // ------------------------------------------------------------------
        // Invalidate accumulated format caches.
        // ------------------------------------------------------------------
        internal override void InvalidateFormatCache()
        {
            if (_mainTextSegment != null)
            {
                _mainTextSegment.InvalidateFormatCache();
            }
        }

        /// <summary>
        /// Update number of characters consumed by the main text segment. 
        /// </summary>
        internal void UpdateSegmentLastFormatPositions()
        {
            _mainTextSegment.UpdateLastFormatPositions();
        }


        #endregion Internal Methods


        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        //-------------------------------------------------------------------
        // CreateSubpageFiniteHelper
        // NOTE: This helper is useful for debugging the caller of this function
        //       because the debugger cannot show local variables in unsafe methods.
        //-------------------------------------------------------------------
        private unsafe void CreateSubpageFiniteHelper(
            PtsContext ptsContext,              // IN:  ptr to FS context
            IntPtr brParaIn,                    // IN:  break record---use if !NULL
            int fFromPreviousPage,              // IN:  break record was created on previous page
            IntPtr nSeg,                        // IN:  name of the segment to start from-if pointer to break rec is NULL
            IntPtr pFtnRej,                     // IN:  pftnrej
            int fEmptyOk,                       // IN:  fEmptyOK
            int fSuppressTopSpace,              // IN:  fSuppressTopSpace
            uint fswdir,                        // IN:  fswdir
            int lWidth,                         // IN:  width of subpage
            int lHeight,                        // IN:  height of subpage
            ref PTS.FSRECT rcMargin,            // IN:  rectangle within subpage margins
            int cColumns,                       // IN:  number of columns
            PTS.FSCOLUMNINFO[] columnInfoCollection, // IN:  array of column info
            int fApplyColumnBalancing,          // IN:  apply column balancing?
            out PTS.FSFMTR fsfmtr,              // OUT: why formatting was stopped
            out IntPtr pSubPage,                // OUT: ptr to the subpage
            out IntPtr brParaOut,               // OUT: break record of the subpage
            out int dvrUsed,                    // OUT: dvrUsed
            out PTS.FSBBOX fsBBox,              // OUT: subpage bbox
            out IntPtr pfsMcsClient,            // OUT: margin collapsing state at the bottom
            out int topSpace)                   // OUT: top space due to collapsed margins
        {
            // Exceptions don't need to pop, as the top level measure context will be nulled out if thrown.
            StructuralCache.CurrentFormatContext.PushNewPageData(new Size(TextDpi.FromTextDpi(lWidth), TextDpi.FromTextDpi(lHeight)),
                                                                 new Thickness(), 
                                                                 false, 
                                                                 true);

            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PTS.Validate(PTS.FsCreateSubpageFinite(ptsContext.Context, brParaIn, fFromPreviousPage, nSeg,
                    pFtnRej, fEmptyOk, fSuppressTopSpace, fswdir, lWidth, lHeight,
                    ref rcMargin, cColumns, rgColumnInfo, PTS.False,
                    0, null, null, 0, null, null, PTS.False, 
                    PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA.fsksuppresshardbreakbeforefirstparaNone, 
                    out fsfmtr, out pSubPage, out brParaOut, out dvrUsed, out fsBBox, out pfsMcsClient, out topSpace), ptsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();
        }

        // ------------------------------------------------------------------
        // Determines what offset is required to convert a paragraph aligned figure into a column aligned figure.
        // ------------------------------------------------------------------
        private int CalculateParagraphToColumnOffset(FigureHorizontalAnchor horizontalAnchor, PTS.FSRECT fsrcInColumn)
        {
            Invariant.Assert(FigureHelper.IsHorizontalColumnAnchor(horizontalAnchor));

            int uComparisonPoint;

            // Depending on anchoring, only the anchored edge (center) is guaranteed to be inside of the column, so finding affected column 
            // requires us to compare against the anchored edge U position.
            if(horizontalAnchor == FigureHorizontalAnchor.ColumnLeft)
            {
                uComparisonPoint = fsrcInColumn.u;
            } 
            else if(horizontalAnchor == FigureHorizontalAnchor.ColumnRight)
            {
                uComparisonPoint = fsrcInColumn.u + fsrcInColumn.du - 1; // du is non-inclusive
            }
            else
            {
                uComparisonPoint = fsrcInColumn.u + (fsrcInColumn.du / 2) - 1; // du is non-inclusive
            }


            double columnWidth, gap, rule;
            int cColumns;

            FigureHelper.GetColumnMetrics(StructuralCache, out cColumns, out columnWidth, out gap, out rule);

            Invariant.Assert(cColumns > 0);

            int duColumnTotal = TextDpi.ToTextDpi(columnWidth + gap);
            int affectedColumn = (uComparisonPoint - StructuralCache.CurrentFormatContext.PageMarginRect.u) / duColumnTotal;
            int columnLeft = StructuralCache.CurrentFormatContext.PageMarginRect.u + affectedColumn * duColumnTotal;
            int columnDU = TextDpi.ToTextDpi(columnWidth);

            int totalMarginLeft = columnLeft - fsrcInColumn.u;
            int totalMarginRight = (columnLeft + columnDU) - (fsrcInColumn.u + fsrcInColumn.du);

            if(horizontalAnchor == FigureHorizontalAnchor.ColumnLeft)
            {
                return totalMarginLeft;
            }
            else if(horizontalAnchor == FigureHorizontalAnchor.ColumnRight)
            {
                return totalMarginRight;
            }
            else
            {
                return (totalMarginRight + totalMarginLeft) / 2;
            }
        }

        // ------------------------------------------------------------------
        // Determines the max total width for this figure element, subtracts the element margins to determine the maximum size the 
        // Subpage can be formatted at.
        // ------------------------------------------------------------------
        private double LimitTotalWidthFromAnchor(double width, double elementMarginWidth)
        {
            Figure element = (Figure)Element;
            FigureHorizontalAnchor horizAnchor = element.HorizontalAnchor;

            double maxTotalWidth = 0.0;
            // Value is in pixels. Now we limit value to max out depending on anchoring.
            if(FigureHelper.IsHorizontalPageAnchor(horizAnchor))
            {
                maxTotalWidth = StructuralCache.CurrentFormatContext.PageWidth;
            }
            else if(FigureHelper.IsHorizontalContentAnchor(horizAnchor))
            {
                Thickness pageMargin = StructuralCache.CurrentFormatContext.PageMargin;
                maxTotalWidth = StructuralCache.CurrentFormatContext.PageWidth - pageMargin.Left - pageMargin.Right;
            }
            else
            {
                double columnWidth, gap, rule;
                int cColumns;

                FigureHelper.GetColumnMetrics(StructuralCache, out cColumns, out columnWidth, out gap, out rule);

                maxTotalWidth = columnWidth;
            }
           
            if((width + elementMarginWidth) > maxTotalWidth)
            {
                width = Math.Max(TextDpi.MinWidth, maxTotalWidth - elementMarginWidth);
            }

            return width;
        }

        // ------------------------------------------------------------------
        // Determines the max total height for this figure element, subtracts the element margins to determine the maximum size the 
        // Subpage can be formatted at.
        // ------------------------------------------------------------------
        private double LimitTotalHeightFromAnchor(double height, double elementMarginHeight)
        {
            Figure element = (Figure)Element;
            FigureVerticalAnchor vertAnchor = element.VerticalAnchor;

            double maxTotalHeight = 0.0;
            // Value is in pixels. Now we limit value to max out depending on anchoring.
            if(FigureHelper.IsVerticalPageAnchor(vertAnchor))
            {
                maxTotalHeight = StructuralCache.CurrentFormatContext.PageHeight;
            }
            else
            {
                Thickness pageMargin = StructuralCache.CurrentFormatContext.PageMargin;
                maxTotalHeight = StructuralCache.CurrentFormatContext.PageHeight - pageMargin.Top - pageMargin.Bottom;
            }

            if((height + elementMarginHeight) > maxTotalHeight)
            {
                height = Math.Max(TextDpi.MinWidth, maxTotalHeight - elementMarginHeight);
            }

            return height;
        }


        /// <summary>
        /// Returns an appropriate search rectangle for collision based on anchor properties.
        /// </summary>
        private PTS.FSRECT CalculateSearchArea(FigureHorizontalAnchor horizAnchor, FigureVerticalAnchor vertAnchor, ref PTS.FSRECT fsrcPage, ref PTS.FSRECT fsrcMargin, ref PTS.FSRECT fsrcTrack, ref PTS.FSRECT fsrcFigurePreliminary)
        {
            PTS.FSRECT fsrcSearch;

            if(FigureHelper.IsHorizontalPageAnchor(horizAnchor))
            {
                fsrcSearch.u = fsrcPage.u;
                fsrcSearch.du = fsrcPage.du;
            }
            else if(FigureHelper.IsHorizontalContentAnchor(horizAnchor))
            {
                fsrcSearch.u = fsrcMargin.u;
                fsrcSearch.du = fsrcMargin.du;
            }
            else
            {
                fsrcSearch.u = fsrcTrack.u;
                fsrcSearch.du = fsrcTrack.du;
            }

            if(FigureHelper.IsVerticalPageAnchor(vertAnchor))
            {
                fsrcSearch.v = fsrcPage.v;
                fsrcSearch.dv = fsrcPage.dv;
            }
            else if(FigureHelper.IsVerticalContentAnchor(vertAnchor))
            {
                fsrcSearch.v = fsrcMargin.v;
                fsrcSearch.dv = fsrcMargin.dv;
            }
            else
            {
                fsrcSearch.v = fsrcFigurePreliminary.v;
                fsrcSearch.dv = (fsrcTrack.v + fsrcTrack.dv) - fsrcFigurePreliminary.v;
            }

            return fsrcSearch;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // ------------------------------------------------------------------
        // Main text segment.
        // ------------------------------------------------------------------
        private BaseParagraph _mainTextSegment;

        #endregion Private Fields
    }
}

#pragma warning enable 1634, 1691

