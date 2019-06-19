// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: SubpageParagraph represents a PTS subpage.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // SubpageParagraph represents a PTS subpage.
    // ----------------------------------------------------------------------
    internal class SubpageParagraph : BaseParagraph
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
        internal SubpageParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            base.Dispose();

            if(_mainTextSegment != null)
            {
                _mainTextSegment.Dispose();
                _mainTextSegment = null;
            }
            GC.SuppressFinalize(this);
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
            fspap.idobj = PtsHost.SubpageParagraphId;
            // Create the main text segment
            if (_mainTextSegment == null)
            {
                _mainTextSegment = new ContainerParagraph(_element, _structuralCache);
            }
        }

        //-------------------------------------------------------------------
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
            SubpageParaClient paraClient;

#pragma warning disable 6518
            // Disable PRESharp warning 6518. SubpageParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            paraClient =  new SubpageParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518
        }

        //-------------------------------------------------------------------
        // FormatParaFinite
        //-------------------------------------------------------------------
        internal unsafe void FormatParaFinite(
            SubpageParaClient paraClient,       // IN:
            IntPtr pbrkrecIn,                   // IN:  break record---use if !NULL
            int fBRFromPreviousPage,            // IN:  break record was created on previous page
            IntPtr footnoteRejector,            // IN:
            int fEmptyOk,                       // IN:  is it OK not to add anything?
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            ref PTS.FSRECT fsrcToFill,          // IN:  rectangle to fill
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out IntPtr pbrkrecOut,              // OUT: pointer to the para break record
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace)                // OUT: top space due to collapsed margin
        {
            uint fswdirSubpage = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            int subpageWidth, subpageHeight;
            int cColumns;
            int marginTop, marginBottom;
            MbpInfo mbp;
            MarginCollapsingState mcsSubpage, mcsBottom;
            PTS.FSRECT fsrcSubpageMargin;
            PTS.FSCOLUMNINFO [] columnInfoCollection;

            // Currently it is possible to get MCS and BR in following situation:
            // At the end of the page there is a paragraph with delayed figure, so the figure
            // gets delayed to the next page. But part of the next paragraph fits in the page,
            // so it gets broken. PTS creates BR with delayed figure and broken para.
            // PTS will format the next page starting from delayed figure, which can produce MCS.
            // So when the next paragraph is continued from BR, it has MCS.
            // This problem is currently investigated by PTS team: PTSLS bug 915.
            // For now, MCS gets ignored here.
            //Debug.Assert(pbrkrecIn == IntPtr.Zero || mcs == null, "Broken paragraph cannot have margin collapsing state.");
            if (mcs != null && pbrkrecIn != IntPtr.Zero)
            {
                mcs = null;
            }

            // Initialize the subpage size and its margin.
            fsrcSubpageMargin = new PTS.FSRECT();
            subpageWidth = fsrcToFill.du;
            subpageHeight = fsrcToFill.dv;

            // Set clear property
            Invariant.Assert(Element is TableCell || Element is AnchoredBlock);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            // Take into account MBPs and modify subpage metrics,
            // and make sure that subpage is at least 1 unit wide (cannot measure at width <= 0)
            marginTop = 0;
            mcsSubpage = null;
            
            // Get MBP info. Since subpage height and width must be at least 1, the max size for MBP is subpage dimensions less 1
            mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip); 

            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFill, fswdirSubpage, out fsrcToFill));
                mbp.MirrorMargin();
            }

            subpageWidth = Math.Max(1, subpageWidth - (mbp.MBPLeft + mbp.MBPRight));
            if (pbrkrecIn == IntPtr.Zero)
            {
                // Top margin collapsing. If suppresing top space, top margin is always 0.
                MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsSubpage, out marginTop);
                if (PTS.ToBoolean(fSuppressTopSpace))
                {
                    marginTop = 0;
                }
                subpageHeight = Math.Max(1, subpageHeight - (marginTop + mbp.BPTop));
                // Destroy top margin collapsing state (not needed anymore).
                if (mcsSubpage != null)
                {
                    mcsSubpage.Dispose();
                    mcsSubpage = null;
                }
            }
            else
            {
                Debug.Assert(fSuppressTopSpace == 1, "Top space should be always suppressed at the top of broken paragraph.");
            }
            fsrcSubpageMargin.du = subpageWidth;
            fsrcSubpageMargin.dv = subpageHeight;

            // Initialize column info
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(_element);
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(_element);
            double pageFontSize = (double)_structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            FontFamily pageFontFamily = (FontFamily)_structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);

            // Get columns info, setting ownerIsFlowDocument flag to false. A flow document should not be formatted as a subpage and we
            // do not want default column widths to be set on TableCells and floaters
            cColumns = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, false);
            columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PtsHelper.GetColumnsInfo(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, cColumns, rgColumnInfo, false);
            }

            // Format subpage
            StructuralCache.CurrentFormatContext.PushNewPageData(new Size(TextDpi.FromTextDpi(subpageWidth), TextDpi.FromTextDpi(subpageHeight)),
                                                                 new Thickness(), 
                                                                 false, 
                                                                 true);

            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PTS.Validate(PTS.FsCreateSubpageFinite(PtsContext.Context, pbrkrecIn, fBRFromPreviousPage, _mainTextSegment.Handle,
                    footnoteRejector, fEmptyOk, fSuppressTopSpace, fswdir, subpageWidth, subpageHeight,
                    ref fsrcSubpageMargin, cColumns, rgColumnInfo, PTS.False,
                    0, null, null, 0, null, null, PTS.FromBoolean(false), 
                    fsksuppresshardbreakbeforefirstparaIn, 
                    out fsfmtr, out pfspara, out pbrkrecOut, out dvrUsed, out fsbbox, out pmcsclientOut, out dvrTopSpace), PtsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();

            fskclearOut = PTS.FSKCLEAR.fskclearNone;

            if (PTS.ToBoolean(fsbbox.fDefined))
            {
                // Workaround for PTS bug 860: get max of the page rect and 
                // bounding box of the page.
                dvrUsed = Math.Max(dvrUsed, fsbbox.fsrc.dv + fsbbox.fsrc.v);
                fsrcToFill.du = Math.Max(fsrcToFill.du, fsbbox.fsrc.du + fsbbox.fsrc.u);
            }

            if (pbrkrecIn == IntPtr.Zero) // if first chunk
            {
                // Take into account MBPs and modify subpage metrics
                dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrTopSpace;
                dvrUsed += (marginTop + mbp.BPTop);
            }

            if (pmcsclientOut != IntPtr.Zero)
            {
                mcsSubpage = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcsSubpage);
                pmcsclientOut = IntPtr.Zero;
            }

            // Initialize subpage metrics
            if (fsfmtr.kstop >= PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace)   // No progress or collision
            {
                dvrUsed = dvrTopSpace = 0;
                //pbrkrecOut = IntPtr.Zero;
                //pfspara = IntPtr.Zero;
                //pmcsclientOut = IntPtr.Zero;
            }
            else
            {
                if (fsfmtr.kstop == PTS.FSFMTRKSTOP.fmtrGoalReached)
                {
                    // Bottom margin collapsing:
                    // (a) retrieve mcs from the subpage
                    // (b) do margin collapsing; create a new margin collapsing state
                    // There is no bottom margin collapsing if paragraph will be continued (output break record is not null).
                    MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsSubpage, out mcsBottom, out marginBottom);
                    pmcsclientOut = (mcsBottom != null) ? mcsBottom.Handle : IntPtr.Zero;

                    if (pmcsclientOut == IntPtr.Zero) // if last chunk
                        dvrUsed += marginBottom + mbp.BPBottom;
                }

                // Update bounding box
                fsbbox.fsrc.u = fsrcToFill.u + mbp.MarginLeft;
                fsbbox.fsrc.v = fsrcToFill.v + dvrTopSpace;
                fsbbox.fsrc.du = Math.Max(fsrcToFill.du - (mbp.MarginLeft + mbp.MarginRight), 0);
                fsbbox.fsrc.dv = Math.Max(dvrUsed - dvrTopSpace, 0);
            }

            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubpage, ref pageRect, ref fsbbox, fswdir, out fsbbox));
            }


            // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
            // If necessary, new MCS is created and passed back to PTS (see above).
            if (mcsSubpage != null)
            {
                mcsSubpage.Dispose();
                mcsSubpage = null;
            }


            // Update information about first/last chunk
            paraClient.SetChunkInfo(pbrkrecIn == IntPtr.Zero, pbrkrecOut == IntPtr.Zero);
        }

        //-------------------------------------------------------------------
        // FormatParaBottomless
        //-------------------------------------------------------------------
        internal unsafe void FormatParaBottomless(
            SubpageParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            int urTrack,                        // IN:  ur of bottomless rectangle to fill
            int durTrack,                       // IN:  dur of bottomless rectangle to fill
            int vrTrack,                        // IN:  vr of bottomless rectangle to fill
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin;
            int cColumns;
            int marginTop, marginBottom;
            MbpInfo mbp;
            MarginCollapsingState mcsSubpage, mcsBottom;
            PTS.FSCOLUMNINFO[] columnInfoCollection;
            uint fswdirSubpage = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            // Initialize the subpage size and its margin.
            subpageWidth = durTrack;
            urSubpageMargin = vrSubpageMargin = 0;


            // Set clear property
            Invariant.Assert(Element is TableCell || Element is AnchoredBlock);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            // Take into account MBPs and modify subpage metrics,
            // and make sure that subpage is at least 1 unit wide (cannot measure at width <= 0)
            // NOTE: Do not suppress top space for bottomles pages.
            mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);

            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT fsrcToFillSubpage = new PTS.FSRECT(urTrack, 0, durTrack, 0);
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFillSubpage, fswdirSubpage, out fsrcToFillSubpage));

                urTrack = fsrcToFillSubpage.u;
                durTrack = fsrcToFillSubpage.du;

                mbp.MirrorMargin();
            }

            subpageWidth = Math.Max(1, subpageWidth - (mbp.MBPLeft + mbp.MBPRight));
            MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsSubpage, out marginTop);
            // Destroy top margin collapsing state (not needed anymore).
            if (mcsSubpage != null)
            {
                mcsSubpage.Dispose();
                mcsSubpage = null;
            }
            durSubpageMargin = subpageWidth;

            // Initialize column info
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(_element);
            // For bottomles spara, limit line height to the height of the current format context in structural cache.
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(_element);
            double pageFontSize = (double)_structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            FontFamily pageFontFamily = (FontFamily)_structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);

            // Get columns info, setting ownerIsFlowDocument flag to false. A flow document should not be formatted as a subpage and we
            // do not want default column widths to be set on TableCells and floaters
            cColumns = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, false);
            columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PtsHelper.GetColumnsInfo(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, cColumns, rgColumnInfo, false);
            }

            // Create subpage

            StructuralCache.CurrentFormatContext.PushNewPageData(new Size(TextDpi.FromTextDpi(subpageWidth), TextDpi.MaxWidth),
                                                                 new Thickness(), 
                                                                 false, 
                                                                 false);

            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PTS.Validate(PTS.FsCreateSubpageBottomless(PtsContext.Context, _mainTextSegment.Handle, fSuppressTopSpace,
                    fswdir, subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin,
                    cColumns, rgColumnInfo, 0, null, null, 0, null, null, PTS.FromBoolean(_isInterruptible), 
                    out fsfmtrbl, out pfspara, out dvrUsed, out fsbbox, out pmcsclientOut, out dvrTopSpace,
                    out fPageBecomesUninterruptable), PtsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();

            fskclearOut = PTS.FSKCLEAR.fskclearNone;

            if (fsfmtrbl != PTS.FSFMTRBL.fmtrblCollision)
            {
                // Bottom margin collapsing:
                // (1) retrieve mcs from the subtrack
                // (2) do margin collapsing; create a new margin collapsing state
                if (pmcsclientOut != IntPtr.Zero)
                {
                    mcsSubpage = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                    PTS.ValidateHandle(mcsSubpage);
                    pmcsclientOut = IntPtr.Zero;
                }
                MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsSubpage, out mcsBottom, out marginBottom);
                pmcsclientOut = (mcsBottom != null) ? mcsBottom.Handle : IntPtr.Zero;

                // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
                // If necessary, new MCS is created and passed back to PTS.
                if (mcsSubpage != null)
                {
                    mcsSubpage.Dispose();
                    mcsSubpage = null;
                }

                if (PTS.ToBoolean(fsbbox.fDefined))
                {
                    // Workaround for PTS bug 860: get max of the page rect and 
                    // bounding box of the page.
                    dvrUsed = Math.Max(dvrUsed, fsbbox.fsrc.dv + fsbbox.fsrc.v);
                    durTrack = Math.Max(durTrack, fsbbox.fsrc.du + fsbbox.fsrc.u);
                }

                // Take into account MBPs and modify subtrack metrics
                dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrTopSpace;
                dvrUsed += (marginTop + mbp.BPTop) + (marginBottom + mbp.BPBottom);

                // Update bounding box
                fsbbox.fsrc.u = urTrack + mbp.MarginLeft;
                fsbbox.fsrc.v = vrTrack + dvrTopSpace;
                fsbbox.fsrc.du = Math.Max(durTrack - (mbp.MarginLeft + mbp.MarginRight), 0);
                fsbbox.fsrc.dv = Math.Max(dvrUsed - dvrTopSpace, 0);
            }
            else
            {
                Debug.Assert(pmcsclientOut == IntPtr.Zero);
                pfspara = IntPtr.Zero;
                dvrUsed = dvrTopSpace = 0;
            }


            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubpage, ref pageRect, ref fsbbox, fswdir, out fsbbox));
            }

            // Update information about first/last chunk. In bottomless scenario
            // paragraph is not broken, so there is only one chunk.
            paraClient.SetChunkInfo(true, true);
        }

        //-------------------------------------------------------------------
        // UpdateBottomlessPara
        //-------------------------------------------------------------------
        internal unsafe void UpdateBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            SubpageParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            int urTrack,                        // IN:  u of bootomless rectangle to fill
            int durTrack,                       // IN:  du of bootomless rectangle to fill
            int vrTrack,                        // IN:  v of bootomless rectangle to fill
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin;
            int cColumns;
            int marginTop, marginBottom;
            MbpInfo mbp;
            MarginCollapsingState mcsSubpage, mcsBottom;
            PTS.FSCOLUMNINFO[] columnInfoCollection;
            uint fswdirSubpage = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            // Initialize the subpage size and its margin.
            subpageWidth = durTrack;
            urSubpageMargin = vrSubpageMargin = 0;

            // Set clear property
            Invariant.Assert(Element is TableCell || Element is AnchoredBlock);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            // Take into account MBPs and modify subpage metrics,
            // and make sure that subpage is at least 1 unit wide (cannot measure at width <= 0)
            // NOTE: Do not suppress top space for bottomles pages.
            mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);

            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT fsrcToFillSubpage = new PTS.FSRECT(urTrack, 0, durTrack, 0);
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFillSubpage, fswdirSubpage, out fsrcToFillSubpage));

                urTrack = fsrcToFillSubpage.u;
                durTrack = fsrcToFillSubpage.du;

                mbp.MirrorMargin();
            }

            subpageWidth = Math.Max(1, subpageWidth - (mbp.MBPLeft + mbp.MBPRight));
            MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsSubpage, out marginTop);
            // Destroy top margin collapsing state (not needed anymore).
            if (mcsSubpage != null)
            {
                mcsSubpage.Dispose();
                mcsSubpage = null;
            }
            durSubpageMargin = subpageWidth;

            // Initialize column info
            // For bottomles spara, limit line height to the height of the current format context in structural cache.
            ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(_element);
            double lineHeight = DynamicPropertyReader.GetLineHeightValue(_element);
            double pageFontSize = (double)_structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
            FontFamily pageFontFamily = (FontFamily)_structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);

            // Get columns info, setting ownerIsFlowDocument flag to false. A flow document should not be formatted as a subpage and we
            // do not want default column widths to be set on TableCells and floaters
            cColumns = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, false);
            columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PtsHelper.GetColumnsInfo(columnProperties, lineHeight, TextDpi.FromTextDpi(subpageWidth), pageFontSize, pageFontFamily, cColumns, rgColumnInfo, false);
            }

            StructuralCache.CurrentFormatContext.PushNewPageData(new Size(TextDpi.FromTextDpi(subpageWidth), TextDpi.MaxWidth),
                                                                 new Thickness(), 
                                                                 true, 
                                                                 false);

            // Create subpage
            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PTS.Validate(PTS.FsUpdateBottomlessSubpage(PtsContext.Context, pfspara, _mainTextSegment.Handle, fSuppressTopSpace,
                    fswdir, subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin,
                    cColumns, rgColumnInfo, 0, null, null, 0, null, null, PTS.FromBoolean(true),
                    out fsfmtrbl, out dvrUsed, out fsbbox, out pmcsclientOut, out dvrTopSpace,
                    out fPageBecomesUninterruptable), PtsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();

            fskclearOut = PTS.FSKCLEAR.fskclearNone;

            if (fsfmtrbl != PTS.FSFMTRBL.fmtrblCollision)
            {
                // Bottom margin collapsing:
                // (1) retrieve mcs from the subtrack
                // (2) do margin collapsing; create a new margin collapsing state
                if (pmcsclientOut != IntPtr.Zero)
                {
                    mcsSubpage = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                    PTS.ValidateHandle(mcsSubpage);
                    pmcsclientOut = IntPtr.Zero;
                }
                MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsSubpage, out mcsBottom, out marginBottom);
                pmcsclientOut = (mcsBottom != null) ? mcsBottom.Handle : IntPtr.Zero;

                // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
                // If necessary, new MCS is created and passed back to PTS.
                if (mcsSubpage != null)
                {
                    mcsSubpage.Dispose();
                    mcsSubpage = null;
                }

                if (PTS.ToBoolean(fsbbox.fDefined))
                {
                    // Workaround for PTS bug 860: get max of the page rect and 
                    // bounding box of the page.
                    dvrUsed = Math.Max(dvrUsed, fsbbox.fsrc.dv + fsbbox.fsrc.v);
                    durTrack = Math.Max(durTrack, fsbbox.fsrc.du + fsbbox.fsrc.u);
                }

                // Take into account MBPs and modify subtrack metrics
                dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrTopSpace;
                dvrUsed += (marginTop + mbp.BPTop) + (marginBottom + mbp.BPBottom);

                // Update bounding box
                fsbbox.fsrc.u = urTrack + mbp.MarginLeft;
                fsbbox.fsrc.v = vrTrack + dvrTopSpace;
                fsbbox.fsrc.du = Math.Max(durTrack - (mbp.MarginLeft + mbp.MarginRight), 0);
                fsbbox.fsrc.dv = Math.Max(dvrUsed - dvrTopSpace, 0);
            }
            else
            {
                Debug.Assert(pmcsclientOut == IntPtr.Zero);
                pfspara = IntPtr.Zero;
                dvrUsed = dvrTopSpace = 0;
            }


            if(fswdirSubpage != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubpage, ref pageRect, ref fsbbox, fswdir, out fsbbox));
            }

            // Update information about first/last chunk. In bottomless scenario
            // paragraph is not broken, so there is only one chunk.
            paraClient.SetChunkInfo(true, true);
        }

        #endregion PTS callbacks

        // ------------------------------------------------------------------
        // 
        //  Internal Methods
        //
        // ------------------------------------------------------------------

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
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //-------------------------------------------------------------------
        // Main text segment.
        //-------------------------------------------------------------------
        private BaseParagraph _mainTextSegment;
        protected bool _isInterruptible = true;

        #endregion Private Fields
    }
}

#pragma warning enable 1634, 1691

