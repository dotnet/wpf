// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: FloaterParagraph class provides a wrapper floater objects.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Security;              // SecurityCritical
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // FloaterParagraph class provides a wrapper floater objects.
    // ----------------------------------------------------------------------
    internal sealed class FloaterParagraph : FloaterBaseParagraph
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
        internal FloaterParagraph(TextElement element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // UpdGetParaChange - Floater paragraph is always new
        // ------------------------------------------------------------------
        internal override void UpdGetParaChange(
            out PTS.FSKCHANGE fskch,            // OUT: kind of change
            out int fNoFurtherChanges)          // OUT: no changes after?
        {
            base.UpdGetParaChange(out fskch, out fNoFurtherChanges);

            fskch = PTS.FSKCHANGE.fskchNew;
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
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. FloaterParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            FloaterParaClient paraClient =  new FloaterParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518

            // Create the main text segment
            if (_mainTextSegment == null)
            {
                _mainTextSegment = new ContainerParagraph(Element, StructuralCache);
            }
        }

        internal override void CollapseMargin(
            BaseParaClient paraClient,          // IN:
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            uint fswdir,                        // IN:  current direction (of the track, in which margin collapsing is happening)
            bool suppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr)                        // OUT: dvr, calculated based on margin collapsing state
        {
            // Floaters are not participating in margin collapsing.
            // Top space is always suppressed
            dvr = 0;            
        }

        //-------------------------------------------------------------------
        // GetFloaterProperties
        //-------------------------------------------------------------------
        internal override void GetFloaterProperties(
            uint fswdirTrack,                       // IN:  direction of track
            out PTS.FSFLOATERPROPS fsfloaterprops)  // OUT: properties of the floater
        {
            fsfloaterprops = new PTS.FSFLOATERPROPS();
            fsfloaterprops.fFloat   = PTS.True;                     // Floater
            fsfloaterprops.fskclear = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            // Get floater alignment from HorizontalAlignment of the floater element.
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    fsfloaterprops.fskfloatalignment = PTS.FSKFLOATALIGNMENT.fskfloatalignMax;
                    break;
                case HorizontalAlignment.Center:
                    fsfloaterprops.fskfloatalignment = PTS.FSKFLOATALIGNMENT.fskfloatalignCenter;
                    break;
                case HorizontalAlignment.Left:
                default:
                    fsfloaterprops.fskfloatalignment = PTS.FSKFLOATALIGNMENT.fskfloatalignMin;
                    break;
            }


            fsfloaterprops.fskwr = PTS.WrapDirectionToFskwrap(WrapDirection);

            // Always delay floaters if there is no progress.
            fsfloaterprops.fDelayNoProgress = PTS.True;
        }

        //-------------------------------------------------------------------
        // FormatFloaterContentFinite
        //-------------------------------------------------------------------
        internal override void FormatFloaterContentFinite(
            FloaterBaseParaClient paraClient,       // IN:
            IntPtr pbrkrecIn,                   // IN:  break record---use if !IntPtr.Zero
            int fBRFromPreviousPage,            // IN:  break record was created on previous page
            IntPtr footnoteRejector,            // IN: 
            int fEmptyOk,                       // IN:  is it OK not to add anything?
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of Track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out IntPtr pbrkrecOut,              // OUT: pointer to the floater content break record
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            uint fswdirPara = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            int subpageWidth, subpageHeight;
            int dvrTopSpace;
            int cColumns;
            PTS.FSRECT fsrcSubpageMargin;
            PTS.FSCOLUMNINFO[] columnInfoCollection;
            IntPtr pmcsclientOut;
            double specifiedWidth;
            MbpInfo mbp;

            Invariant.Assert(paraClient is FloaterParaClient);

            // If horizontal alignment is Stretch and we are not formatting at max width,
            // we cannot proceed.
            if (IsFloaterRejected(PTS.ToBoolean(fAtMaxWidth), TextDpi.FromTextDpi(durAvailable)))
            {
                durFloaterWidth = dvrFloaterHeight = 0;
                cPolygons = cVertices = 0;
                fsfmtr = new PTS.FSFMTR();
                fsfmtr.kstop = PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace;
                fsfmtr.fContainsItemThatStoppedBeforeFootnote = PTS.False;
                fsfmtr.fForcedProgress = PTS.False;
                fsbbox = new PTS.FSBBOX();
                fsbbox.fDefined = PTS.False;
                pbrkrecOut = IntPtr.Zero;
                pfsFloatContent = IntPtr.Zero;
            }
            else
            {
                // When formatting bottomless page, PTS may format paragraphs as finite. This happens
                // in case of multiple columns. In this case make sure that height is not too big.
                if (!StructuralCache.CurrentFormatContext.FinitePage)
                {
                    if (Double.IsInfinity(StructuralCache.CurrentFormatContext.PageHeight))
                    {
                        if (dvrAvailable > PTS.dvBottomUndefined / 2)
                        {
                            dvrAvailable = Math.Min(dvrAvailable, PTS.dvBottomUndefined / 2);
                            fEmptyOk = PTS.False;
                        }
                    }
                    else
                    {
                        dvrAvailable = Math.Min(dvrAvailable, TextDpi.ToTextDpi(StructuralCache.CurrentFormatContext.PageHeight));
                    }
                }

                // Initialize the subpage size. PTS subpage margin is always set to 0 for Floaters.
                // If width on floater is specified, use the specified value.
                // Margin, border and padding of the floater is extracted from available subpage height
                mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
                
                // We do not mirror margin as it's used to dist text left and right, and is unnecessary.
                // Clip Floater.Width to available width
                specifiedWidth = CalculateWidth(TextDpi.FromTextDpi(durAvailable));
                AdjustDurAvailable(specifiedWidth, ref durAvailable, out subpageWidth);
                subpageHeight = Math.Max(1, dvrAvailable - (mbp.MBPTop + mbp.MBPBottom));
                fsrcSubpageMargin = new PTS.FSRECT();
                fsrcSubpageMargin.du = subpageWidth;
                fsrcSubpageMargin.dv = subpageHeight;

                // Initialize column info. Floater always has just 1 column.
                cColumns = 1;
                columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
                columnInfoCollection[0].durBefore = 0;
                columnInfoCollection[0].durWidth = subpageWidth;

                // Format subpage
                CreateSubpageFiniteHelper(PtsContext, pbrkrecIn, fBRFromPreviousPage, _mainTextSegment.Handle,
                    footnoteRejector, fEmptyOk, PTS.True, fswdir, subpageWidth, subpageHeight,
                    ref fsrcSubpageMargin, cColumns, columnInfoCollection, PTS.False, fsksuppresshardbreakbeforefirstparaIn,
                    out fsfmtr, out pfsFloatContent, 
                    out pbrkrecOut, out dvrFloaterHeight, out fsbbox, out pmcsclientOut, out dvrTopSpace);

                // Initialize subpage metrics
                if (fsfmtr.kstop >= PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace)   // No progress or collision
                {
                    Debug.Assert(pmcsclientOut == IntPtr.Zero);
                    durFloaterWidth = dvrFloaterHeight = 0;
                    cPolygons = cVertices = 0;
                    //pbrkrecpara = IntPtr.Zero;
                }
                else
                {
                    // PTS subpage does not support autosizing, but Floater needs to autosize to its
                    // content. To workaround this problem, second format of subpage is performed, if 
                    // necessary. It means that if the width of bounding box is smaller than subpage's
                    // width, second formatting is performed.
                    // However, if HorizontalAlignment is set to Stretch we should not reformat because floater
                    // should be at max width
                    if (PTS.ToBoolean(fsbbox.fDefined))
                    {
                        if(fsbbox.fsrc.du < subpageWidth && Double.IsNaN(specifiedWidth) && HorizontalAlignment != HorizontalAlignment.Stretch)
                        {
                            // There is a need to reformat PTS subpage, so destroy any resourcces allocated by PTS
                            // during previous formatting.
                            if (pfsFloatContent != IntPtr.Zero)
                            {
                                PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, pfsFloatContent), PtsContext);
                                pfsFloatContent = IntPtr.Zero;
                            }
                            if (pbrkrecOut != IntPtr.Zero)
                            {
                                PTS.Validate(PTS.FsDestroySubpageBreakRecord(PtsContext.Context, pbrkrecOut), PtsContext);
                                pbrkrecOut = IntPtr.Zero;
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
                            fsrcSubpageMargin.du = subpageWidth;
                            fsrcSubpageMargin.dv = subpageHeight;
                            columnInfoCollection[0].durWidth = subpageWidth;
                            CreateSubpageFiniteHelper(PtsContext, pbrkrecIn, fBRFromPreviousPage, _mainTextSegment.Handle,
                                 footnoteRejector, fEmptyOk, PTS.True, fswdir, subpageWidth, subpageHeight,
                                ref fsrcSubpageMargin, cColumns, columnInfoCollection, PTS.False, fsksuppresshardbreakbeforefirstparaIn,
                                out fsfmtr, out pfsFloatContent,
                                out pbrkrecOut, out dvrFloaterHeight, out fsbbox, out pmcsclientOut, out dvrTopSpace);
                        }
                    }
                    else
                    {
                        subpageWidth = TextDpi.ToTextDpi(TextDpi.MinWidth);
                    }

                    // Destroy objects created by PTS, but not used here.
                    if (pmcsclientOut != IntPtr.Zero)
                    {
                        MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                        PTS.ValidateHandle(mcs);
                        mcs.Dispose();
                        pmcsclientOut = IntPtr.Zero;
                    }

                    // Get the size of the floater. For height PTS already reports calculated value.
                    // But width is the same as subpage width. Add margin values here since we do not use 
                    // distance to text anymore
                    durFloaterWidth = subpageWidth + mbp.MBPLeft + mbp.MBPRight;

                    // Add back all MBP values since we do not use dist to text
                    dvrFloaterHeight += mbp.MBPTop + mbp.MBPBottom;
                    // Check if floater width fits in available width. It may exceed available width because borders and
                    // padding are added.


                    fsbbox.fsrc.u = 0;
                    fsbbox.fsrc.v = 0;
                    fsbbox.fsrc.du = durFloaterWidth;
                    fsbbox.fsrc.dv = dvrFloaterHeight;
                    fsbbox.fDefined = PTS.True;

                    // Tight wrap is disabled for now.
                    cPolygons = cVertices = 0;

                    if(durFloaterWidth > durAvailable || dvrFloaterHeight > dvrAvailable)
                    {
                        if(PTS.ToBoolean(fEmptyOk))
                        {
                            // Get rid of any previous formatting 
                            if (pfsFloatContent != IntPtr.Zero)
                            {
                                PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, pfsFloatContent), PtsContext);
                                pfsFloatContent = IntPtr.Zero;
                            }
                            if (pbrkrecOut != IntPtr.Zero)
                            {
                                PTS.Validate(PTS.FsDestroySubpageBreakRecord(PtsContext.Context, pbrkrecOut), PtsContext);
                                pbrkrecOut = IntPtr.Zero;
                            }
                            cPolygons = cVertices = 0;
                            fsfmtr.kstop = PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace;
                        }
                        else
                        {
                            fsfmtr.fForcedProgress = PTS.True;
                        }
                    }
                }
            }

            // Update handle to PTS subpage.
            ((FloaterParaClient)paraClient).SubpageHandle = pfsFloatContent;
        }

        //-------------------------------------------------------------------
        // FormatFloaterContentBottomless
        //-------------------------------------------------------------------
        internal override void FormatFloaterContentBottomless(
            FloaterBaseParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            uint fswdirPara = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            Invariant.Assert(paraClient is FloaterParaClient);

            int subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin;
            int dvrTopSpace, fPageBecomesUninterruptable;
            int cColumns;
            PTS.FSCOLUMNINFO[] columnInfoCollection;
            IntPtr pmcsclientOut;
            MbpInfo mbp;
            double specifiedWidth;
            
            // If horizontal alignment is Stretch and we are not formatting at max width,
            // we cannot proceed.
            if (IsFloaterRejected(PTS.ToBoolean(fAtMaxWidth), TextDpi.FromTextDpi(durAvailable)))
            {
                // Set foater width, height to be greater than available values to signal to PTS that floater does not fit in the space
                durFloaterWidth = durAvailable + 1;
                dvrFloaterHeight = dvrAvailable + 1;
                cPolygons = cVertices = 0;
                fsfmtrbl = PTS.FSFMTRBL.fmtrblInterrupted;
                fsbbox = new PTS.FSBBOX();
                fsbbox.fDefined = PTS.False;
                pfsFloatContent = IntPtr.Zero;
            }
            else
            {
                // Initialize the subpage size. PTS subpage margin is always set to 0 for Floaters.
                // If width on floater is specified, use the specified value.
                // Margin, border and padding of the floater is extracted from available subpage width.
                mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);

                specifiedWidth = CalculateWidth(TextDpi.FromTextDpi(durAvailable));
                AdjustDurAvailable(specifiedWidth, ref durAvailable, out subpageWidth);
                durSubpageMargin = subpageWidth;
                urSubpageMargin = vrSubpageMargin = 0;

                // Initialize column info. Floater always has just 1 column.
                cColumns = 1;
                columnInfoCollection = new PTS.FSCOLUMNINFO[cColumns];
                columnInfoCollection[0].durBefore = 0;
                columnInfoCollection[0].durWidth = subpageWidth;

                // Create subpage
                InvalidateMainTextSegment();
                CreateSubpageBottomlessHelper(PtsContext, _mainTextSegment.Handle, PTS.True,
                    fswdir, subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin,
                    cColumns, columnInfoCollection,
                    out fsfmtrbl, out pfsFloatContent, out dvrFloaterHeight, out fsbbox, out pmcsclientOut, 
                    out dvrTopSpace, out fPageBecomesUninterruptable);

                if (fsfmtrbl != PTS.FSFMTRBL.fmtrblCollision)
                {
                    // PTS subpage does not support autosizing, but Floater needs to autosize to its
                    // content. To workaround this problem, second format of subpage is performed, if 
                    // necessary. It means that if the width of bounding box is smaller than subpage's
                    // width, second formatting is performed.
                    // However, if HorizontalAlignment is set to Stretch we should not reformat because
                    // floater should be at full column width
                    if (PTS.ToBoolean(fsbbox.fDefined))
                    {
                        if(fsbbox.fsrc.du < subpageWidth && Double.IsNaN(specifiedWidth) && HorizontalAlignment != HorizontalAlignment.Stretch)
                        {
                            // There is a need to reformat PTS subpage, so destroy any resourcces allocated by PTS
                            // during previous formatting.
                            if (pfsFloatContent != IntPtr.Zero)
                            {
                                PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, pfsFloatContent), PtsContext);
                            }
                            if (pmcsclientOut != IntPtr.Zero)
                            {
                                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                                PTS.ValidateHandle(mcs);
                                mcs.Dispose();
                                pmcsclientOut = IntPtr.Zero;
                            }
                            // Create subpage with new width.
                            subpageWidth = durSubpageMargin = fsbbox.fsrc.du + 1; // add 1/300px to avoid rounding errors
                            columnInfoCollection[0].durWidth = subpageWidth;
                            CreateSubpageBottomlessHelper(PtsContext, _mainTextSegment.Handle, PTS.True,
                                fswdir, subpageWidth, urSubpageMargin, durSubpageMargin, vrSubpageMargin,
                                cColumns, columnInfoCollection,
                                out fsfmtrbl, out pfsFloatContent, out dvrFloaterHeight, out fsbbox, out pmcsclientOut,
                                out dvrTopSpace, out fPageBecomesUninterruptable);
                        }
                    }
                    else
                    {
                        subpageWidth = TextDpi.ToTextDpi(TextDpi.MinWidth);
                    }

                    // Destroy objects created by PTS, but not used here.
                    if (pmcsclientOut != IntPtr.Zero)
                    {
                        MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                        PTS.ValidateHandle(mcs);
                        mcs.Dispose();
                        pmcsclientOut = IntPtr.Zero;
                    }

                    // Get the size of the floater. For height PTS already reports calculated value.
                    // But width is the same as subpage width. 
                    durFloaterWidth = subpageWidth + mbp.MBPLeft + mbp.MBPRight;

                    dvrFloaterHeight += mbp.MBPTop + mbp.MBPBottom;

                    // Check if floater width fits in available width. It may exceed available width because borders
                    // and padding are added.
                    if ( dvrFloaterHeight > dvrAvailable ||
                         (durFloaterWidth > durAvailable && !PTS.ToBoolean(fAtMaxWidth))
                       )
                    {
                        // Get rid of any previous formatting 
                        if (pfsFloatContent != IntPtr.Zero)
                        {
                            PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, pfsFloatContent), PtsContext);
                        }

                        Debug.Assert(pmcsclientOut == IntPtr.Zero);
                        cPolygons = cVertices = 0;
                        pfsFloatContent = IntPtr.Zero;
                    }
                    else
                    {
                        // Width and height are OK, format floater
                        // Adjust bounding box to cover entire floater.
                        fsbbox.fsrc.u = 0;
                        fsbbox.fsrc.v = 0;
                        fsbbox.fsrc.du = durFloaterWidth;
                        fsbbox.fsrc.dv = dvrFloaterHeight;

                        // Tight wrap is disabled for now.
                        cPolygons = cVertices = 0;
                    }
                }
                else
                {
                    Debug.Assert(pmcsclientOut == IntPtr.Zero);
                    durFloaterWidth = dvrFloaterHeight = 0;
                    cPolygons = cVertices = 0;
                    pfsFloatContent = IntPtr.Zero;
                }
            }

            // Update handle to PTS subpage.
            ((FloaterParaClient)paraClient).SubpageHandle = pfsFloatContent;
        }

        //-------------------------------------------------------------------
        // UpdateBottomlessFloaterContent
        //-------------------------------------------------------------------
        internal override void UpdateBottomlessFloaterContent(
            FloaterBaseParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            IntPtr pfsFloatContent,             // IN:  floater content (in UIElementParagraph, this is an alias to the paraClient)
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            fsfmtrbl = default(PTS.FSFMTRBL); 
            durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();

            Invariant.Assert(false, "No appropriate handling for update in attached object floater.");
        }


        //-------------------------------------------------------------------
        // GetMCSClientAfterFloater
        //-------------------------------------------------------------------
        internal override void GetMCSClientAfterFloater(
            uint fswdirTrack,                   // IN:  direction of Track
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            out IntPtr pmcsclientOut)           // OUT: MCSCLIENT that floater will return to track
        {
                        // Floaters margins are added during formatting.
            if (mcs != null)
            {
                pmcsclientOut = mcs.Handle;
            }
            else
            {
                pmcsclientOut = IntPtr.Zero;
            }
        }

        #endregion PTS callbacks

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

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

        /// <summary>
        /// Adjust the width available for the Floater.
        /// </summary>
        private void AdjustDurAvailable(double specifiedWidth, ref int durAvailable, out int subpageWidth)
        {
            // If width on floater is specified, use the specified value.
            // Use page size from current format context to limit MBP
            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            if (!Double.IsNaN(specifiedWidth))
            {
                TextDpi.EnsureValidPageWidth(ref specifiedWidth);
                // If specified width is greater than available width, do not exceed available width
                int durSpecified = TextDpi.ToTextDpi(specifiedWidth);
                if ((durSpecified + mbp.MarginRight + mbp.MarginLeft) <= durAvailable)
                {
                    // Set durAvailable to durSpecified plus margins, which will be added later.
                    // Set subpage width to specified width less border and padding
                    durAvailable = durSpecified + mbp.MarginLeft + mbp.MarginRight;
                    subpageWidth = Math.Max(1, durSpecified - (mbp.BPLeft + mbp.BPRight));
                 }
                 else
                 {
                     // Use durAvailable, less MBP to set subpage width. We cannot set figure to specified width
                     subpageWidth = Math.Max(1, durAvailable - (mbp.MBPLeft + mbp.MBPRight));
                 }
            }            
            else
            {
                // No width specified. Use durAvailable, less MBP to set subpage width. 
                subpageWidth = Math.Max(1, durAvailable - (mbp.MBPLeft + mbp.MBPRight));
            }
        }

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
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
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
                    fsksuppresshardbreakbeforefirstparaIn, 
                    out fsfmtr, out pSubPage, out brParaOut, out dvrUsed, out fsBBox, out pfsMcsClient, out topSpace), ptsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();
        }

        //-------------------------------------------------------------------
        // CreateSubpageBottomlessHelper
        // NOTE: This helper is useful for debugging the caller of this function
        //       because the debugger cannot show local variables in unsafe methods.
        //-------------------------------------------------------------------
        private unsafe void CreateSubpageBottomlessHelper(
            PtsContext ptsContext,              // IN:  ptr to FS context
            IntPtr nSeg,                        // IN:  name of the segment to start from
            int fSuppressTopSpace,              // IN:  suppress top space?
            uint fswdir,                        // IN:  fswdir
            int lWidth,                         // IN:  width of subpage
            int urMargin,                       // IN:  ur of margin
            int durMargin,                      // IN:  dur of margin
            int vrMargin,                       // IN:  vr of margin
            int cColumns,                       // IN:  number of columns
            PTS.FSCOLUMNINFO[] columnInfoCollection, // IN:  array of column info
            out PTS.FSFMTRBL pfsfmtr,           // OUT: why formatting was stopped
            out IntPtr ppSubPage,               // OUT: ptr to the subpage
            out int pdvrUsed,                   // OUT: dvrUsed
            out PTS.FSBBOX pfsBBox,             // OUT: subpage bbox
            out IntPtr pfsMcsClient,            // OUT: margin collapsing state at the bottom
            out int pTopSpace,                  // OUT: top space due to collapsed margins
            out int fPageBecomesUninterruptible)// OUT: interruption is prohibited from now on
        {
            // Exceptions don't need to pop, as the top level measure context will be nulled out if thrown.
            StructuralCache.CurrentFormatContext.PushNewPageData(new Size(TextDpi.FromTextDpi(lWidth), TextDpi.MaxWidth),
                                                                 new Thickness(), 
                                                                 false, 
                                                                 false);

            fixed (PTS.FSCOLUMNINFO* rgColumnInfo = columnInfoCollection)
            {
                PTS.Validate(PTS.FsCreateSubpageBottomless(ptsContext.Context, nSeg, fSuppressTopSpace,
                    fswdir, lWidth, urMargin, durMargin, vrMargin,
                    cColumns, rgColumnInfo, 0, null, null, 0, null, null, PTS.False,
                    out pfsfmtr, out ppSubPage, out pdvrUsed, out pfsBBox, out pfsMcsClient,
                    out pTopSpace, out fPageBecomesUninterruptible), ptsContext);
            }

            StructuralCache.CurrentFormatContext.PopPageData();
        }

        /// <summary>
        /// Invalidates main text segment for bottomless pages if DTR list for this para is non-null.
        /// </summary>
        private void InvalidateMainTextSegment()
        {
            DtrList dtrs = StructuralCache.DtrsFromRange(ParagraphStartCharacterPosition, LastFormatCch);
            if (dtrs != null && dtrs.Length > 0)
            {
                _mainTextSegment.InvalidateStructure(dtrs[0].StartIndex);
            }
        }

        //-------------------------------------------------------------------
        // HorizontalAlignment
        // Returns the horizontal alignment of this floater. Either calculated from the Floater or Figure properties.
        //-------------------------------------------------------------------
        private HorizontalAlignment HorizontalAlignment
        {
            get
            {
                if(Element is Floater)
                {
                    return ((Floater)Element).HorizontalAlignment;
                }

                Figure figure = (Figure) Element;
                FigureHorizontalAnchor horizontalAnchor = figure.HorizontalAnchor;

                if(horizontalAnchor == FigureHorizontalAnchor.PageLeft || 
                   horizontalAnchor == FigureHorizontalAnchor.ContentLeft || 
                   horizontalAnchor == FigureHorizontalAnchor.ColumnLeft)
                {
                    return HorizontalAlignment.Left;
                }
                else if(horizontalAnchor == FigureHorizontalAnchor.PageRight || 
                   horizontalAnchor == FigureHorizontalAnchor.ContentRight || 
                   horizontalAnchor == FigureHorizontalAnchor.ColumnRight)
                {
                    return HorizontalAlignment.Right;
                }
                else if(horizontalAnchor == FigureHorizontalAnchor.PageCenter || 
                   horizontalAnchor == FigureHorizontalAnchor.ContentCenter || 
                   horizontalAnchor == FigureHorizontalAnchor.ColumnCenter)
                {
                    return HorizontalAlignment.Center;
                }
                else
                {
                    Debug.Assert(false, "Unknown type of anchor.");
                    return HorizontalAlignment.Center;
                }
            }
        }

        //-------------------------------------------------------------------
        // WrapDirection
        // Returns the wrap direction of this floater. Either calculated from the Floater or Figure properties.
        //-------------------------------------------------------------------
        private WrapDirection WrapDirection
        {
            get
            {
                if(Element is Floater)
                {
                    // Wrap text on both sides of the floater in all cases except where alignment is stretch, in which
                    // case text must not be wrapped on either side
                    if (HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        return WrapDirection.None;
                    }
                    else
                    {
                        return WrapDirection.Both;
                    }
                }
                else
                {
                    Figure figure = (Figure) Element;

                    return figure.WrapDirection;
                }
            }
        }



        //-------------------------------------------------------------------
        //  Calculates the width of a floater - Returns NaN for auto sized floaters
        //-------------------------------------------------------------------
        private double CalculateWidth(double spaceAvailable)
        {
            if(Element is Floater)
            {
                return (double)((Floater)Element).Width;
            }
            else
            {
                bool isWidthAuto;

                double desiredSize = FigureHelper.CalculateFigureWidth(StructuralCache, (Figure)Element, 
                                                                       ((Figure)Element).Width, 
                                                                       out isWidthAuto);

                if(isWidthAuto)
                {
                    return Double.NaN;
                }

                return Math.Min(desiredSize, spaceAvailable);
            }
        }       

        //-------------------------------------------------------------------
        //  Determines whether a floater should be rejected or not
        //-------------------------------------------------------------------
        private bool IsFloaterRejected(bool fAtMaxWidth, double availableSpace)
        {
            if(fAtMaxWidth)
            {
                return false;
            }

            if(Element is Floater && HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                return false;
            }
            else if(Element is Figure)
            {
                FigureLength figureLength = ((Figure)Element).Width;
                if(figureLength.IsAuto)
                {
                    return false;
                }
                if(figureLength.IsAbsolute && figureLength.Value < availableSpace)
                {
                    return false;
                }
            }
            return true;
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

