// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: ContainerParagraph represents continuous piece of backing 
//              storage and consists of other paragraphs. Collection of 
//              these paragraphs is stored  as double-linked list of 
//              Paragraph objects. 
//              A container paragraph is associated with a block element 
//              and can be hosted by a section or another container paragraph.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Security;              // SecurityCritical
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // ContainerParagraph represents continuous piece of backing storage and 
    // it consists of paragraphs. Collection of these paragraphs is stored 
    // as double-linked list of Paragraph objects. 
    // A container paragraph is associated with a block element and can be 
    // hosted by a section or another container paragraph.
    // ----------------------------------------------------------------------
    internal class ContainerParagraph : BaseParagraph, ISegment
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
        internal ContainerParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            BaseParagraph paraChild = _firstChild;
            while (paraChild != null)
            {
                BaseParagraph para = paraChild;
                paraChild = paraChild.Next;
                para.Dispose();
                para.Next = null;
                para.Previous = null;
            }
            _firstChild = _lastFetchedChild = null;
            base.Dispose();
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
        // ISegment.GetFirstPara
        //-------------------------------------------------------------------
        void ISegment.GetFirstPara(
            out int fSuccessful,                // OUT: does segment contain any paragraph?
            out IntPtr firstParaName)           // OUT: name of the first paragraph in segment
        {
            if (_ur != null)
            {
                // Determine if synchronization point has been reached. (If paras are deleted outright, first para may be sync para.
                int cpCurrent = TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterStart);

                if (_ur.SyncPara != null && cpCurrent == _ur.SyncPara.ParagraphStartCharacterPosition)
                {
                    _ur.SyncPara.Previous = null;
                    if (_ur.Next != null && _ur.Next.FirstPara == _ur.SyncPara)
                    {
                        _ur.SyncPara.SetUpdateInfo(_ur.Next.ChangeType, false);
                    }
                    else
                    {
                        _ur.SyncPara.SetUpdateInfo(PTS.FSKCHANGE.fskchNone, _ur.Next == null);
                    }

                    Invariant.Assert(_firstChild == null);
                    _firstChild = _ur.SyncPara;
                    _ur = _ur.Next;
                }
            }

            // If the first paragraph already exists, return it and exit.
            if (_firstChild != null)
            {
                // PTS may decide to swith to full format when in update mode.
                // In this case UpdGetFirstChangeInSegment will not be called.
                // Hence there is need to destroy all existing paragraphs.
                if (StructuralCache.CurrentFormatContext.IncrementalUpdate && 
                    _ur == null && 
                    NeedsUpdate() && 
                    !_firstParaValidInUpdateMode)
                {
                    // But in finite page scenarios the NameTable has been already cleared, 
                    // so there is no need to drop paragraphs.
                    if (!StructuralCache.CurrentFormatContext.FinitePage)
                    {
                        // Disconnect obsolete paragraphs.
                        BaseParagraph paraInvalid = _firstChild;
                        while (paraInvalid != null)
                        {
                            paraInvalid.Dispose();
                            paraInvalid = paraInvalid.Next;
                        }
                        _firstChild = null;
                    }
                    _firstParaValidInUpdateMode = true;
                }
                else
                {
                    // If in update mode, setup update info for the first para.
                    if (_ur != null && _ur.InProcessing && _ur.FirstPara == _firstChild)
                    {
                        _firstChild.SetUpdateInfo(PTS.FSKCHANGE.fskchInside, false);
                    }
                }
            }

#if TEXTPANELLAYOUTDEBUG
            bool cached = _firstChild != null;
#endif
            if (_firstChild == null)
            {
                // Determine paragraph type and create it.
                ITextPointer textPointer = TextContainerHelper.GetContentStart(StructuralCache.TextContainer, Element);
                _firstChild = GetParagraph(textPointer, false);

                // If in update mode, setup update info.
                if (_ur != null && _firstChild != null)
                {
                    _firstChild.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                }
            }

            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                _firstParaValidInUpdateMode = true;
            }

            // Initialize output parameters.
            _lastFetchedChild = _firstChild;
            fSuccessful = PTS.FromBoolean(_firstChild != null);
            firstParaName = (_firstChild != null) ? _firstChild.Handle : IntPtr.Zero;
#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                System.Text.StringBuilder msg = new System.Text.StringBuilder();
                msg.Append("ContPara.GetFirstPara, Found=" + fSuccessful);
                if (_firstChild != null)
                {
                    msg.Append(" Cached=" + cached + " Para=" + _firstChild.GetType().Name);
                }
                TextPanelDebug.Log(msg.ToString(), TextPanelDebug.Category.ContentChange);
            }
#endif
        }

        //-------------------------------------------------------------------
        // ISegment.GetNextPara
        //-------------------------------------------------------------------
        void ISegment.GetNextPara(
            BaseParagraph prevParagraph,        // IN:  current para
            out int fFound,                     // OUT: is there next paragraph?
            out IntPtr nextParaName)            // OUT: name of the next paragraph in section
        {
            if (_ur != null)
            {
                // Determine if synchronization point has been reached.
                int cpCurrent = prevParagraph.ParagraphEndCharacterPosition;
                if (_ur.SyncPara != null && cpCurrent == _ur.SyncPara.ParagraphStartCharacterPosition)
                {
                    _ur.SyncPara.Previous = prevParagraph;
                    prevParagraph.Next = _ur.SyncPara;
                    if (_ur.Next != null && _ur.Next.FirstPara == _ur.SyncPara)
                    {
                        _ur.SyncPara.SetUpdateInfo(_ur.Next.ChangeType, false);
                    }
                    else
                    {
                        _ur.SyncPara.SetUpdateInfo(PTS.FSKCHANGE.fskchNone, _ur.Next == null);
                    }

                    _ur = _ur.Next;
                }
                else
                {
                    Invariant.Assert(_ur.SyncPara == null || cpCurrent < _ur.SyncPara.ParagraphStartCharacterPosition);

                    // Skip all paragraphs before the beginning of the next UpdateRecord. 
                    // This situation may happen when we go to the next UpdateRecord after finding 
                    // synchronization point. It means that we have to run into _ur.FirstPara and all 
                    // paragraphs up to this point dont need to be updated.
                    if (!_ur.InProcessing && _ur.FirstPara != prevParagraph.Next && prevParagraph.Next != null)
                    {
                        prevParagraph.Next.SetUpdateInfo(PTS.FSKCHANGE.fskchNone, false);
                    }
                    // If updated paragraph return it
                    else if (_ur.FirstPara != null && _ur.FirstPara == prevParagraph.Next)
                    {
                        Debug.Assert(_ur.ChangeType == PTS.FSKCHANGE.fskchInside); // Inconsistent UpdateRecord data
                        _ur.InProcessing = true;
                        prevParagraph.Next.SetUpdateInfo(PTS.FSKCHANGE.fskchInside, false);
                    }
                }
}

            BaseParagraph nextParagraph = prevParagraph.Next;
#if TEXTPANELLAYOUTDEBUG
            bool cached = nextParagraph != null;
#endif
            if (nextParagraph == null)
            {
                // Determine paragraph type and create it
                ITextPointer textPointer = TextContainerHelper.GetTextPointerFromCP(StructuralCache.TextContainer, prevParagraph.ParagraphEndCharacterPosition, LogicalDirection.Forward);
                nextParagraph = GetParagraph(textPointer, true);

                // Add new paragraph to a linked list of paragraphs in the segment
                if (nextParagraph != null)
                {
                    nextParagraph.Previous = prevParagraph;
                    prevParagraph.Next = nextParagraph;
                    if (_changeType == PTS.FSKCHANGE.fskchInside)
                    {
                        nextParagraph.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                    }
                }
            }

            // Initialize output parameters
            if (nextParagraph != null)
            {
                fFound = PTS.True;
                nextParaName = nextParagraph.Handle;
                _lastFetchedChild = nextParagraph;
            }
            else
            {
                fFound = PTS.False;
                nextParaName = IntPtr.Zero;
                // Pages might be created in random order (assuming that structure is not
                // dirty). Because of that always update last fetched paragraph cache.
                _lastFetchedChild = prevParagraph;
                _ur = null; // Clear out any additional update record info for this segment.
            }
#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                System.Text.StringBuilder msg = new System.Text.StringBuilder();
                msg.Append("ContPara.GetNextPara, Found=" + fFound);
                if (nextParagraph != null)
                {
                    msg.Append(" Cached=" + cached + " Para=" + nextParagraph.GetType().Name);
                }
                TextPanelDebug.Log(msg.ToString(), TextPanelDebug.Category.ContentChange);
            }
#endif
        }

        //-------------------------------------------------------------------
        // ISegment.UpdGetFirstChangeInSegment
        //-------------------------------------------------------------------
        void ISegment.UpdGetFirstChangeInSegment(
            out int fFound,                     // OUT: anything changed?
            out int fChangeFirst,               // OUT: first paragraph changed?
            out IntPtr nmpBeforeChange)         // OUT: name of paragraph before the change if !fChangeFirst
        {
            Debug.Assert(_ur == null); // UpdateRecord has been already created.

            BuildUpdateRecord();

            fFound = PTS.FromBoolean(_ur != null);
            fChangeFirst = PTS.FromBoolean((_ur != null) && (_firstChild == null || _firstChild == _ur.FirstPara));
            if (PTS.ToBoolean(fFound) && !PTS.ToBoolean(fChangeFirst))
            {
                if (_ur.FirstPara == null)
                {
                    // Something has been added at the end of container paragraph. 
                    // Find the last paragraph.
                    BaseParagraph lastPara = _lastFetchedChild;
                    while (lastPara.Next != null)
                    {
                        lastPara = lastPara.Next;
                    }
                    nmpBeforeChange = lastPara.Handle;
                }
                else
                {
                    // Disconnect the first invalid paragraph from the list
                    if (_ur.ChangeType == PTS.FSKCHANGE.fskchNew)
                    {
                        _ur.FirstPara.Previous.Next = null;
                    }
                    nmpBeforeChange = _ur.FirstPara.Previous.Handle;
                }
            }
            else
            {
                nmpBeforeChange = IntPtr.Zero;
            }
            if (PTS.ToBoolean(fFound))
            {
                _ur.InProcessing = PTS.ToBoolean(fChangeFirst);
                _changeType = PTS.FSKCHANGE.fskchInside;
                _stopAsking = false;
            }
#if TEXTPANELLAYOUTDEBUG
            if (StructuralCache.CurrentFormatContext.IncrementalUpdate)
            {
                System.Text.StringBuilder msg = new System.Text.StringBuilder();
                msg.Append("ContPara.UpdGetFirstChangeInSegment, Found=" + fFound);
                if (PTS.ToBoolean(fFound))
                {
                    msg.Append(" First=" + fChangeFirst);
                    if (nmpBeforeChange != IntPtr.Zero)
                    {
                        msg.Append(" ParaBefore=" + PtsContext.HandleToObject(nmpBeforeChange).GetType().Name);
                    }
                }
                TextPanelDebug.Log(msg.ToString(), TextPanelDebug.Category.ContentChange);
            }
#endif
        }

        //-------------------------------------------------------------------
        // UpdGetSegmentChange
        //-------------------------------------------------------------------
        internal void UpdGetSegmentChange(
            out PTS.FSKCHANGE fskch)            // OUT: kind of change
        {
            Debug.Assert(StructuralCache.CurrentFormatContext.FinitePage || _ur != null); // For bottomless case UpdateRecord needs to be created in UpdGetFirstChangeInSegment.
            // During update of finite page, UpdGetFirstChangeInSegment is not called.
            // Hence needs to calculate and set update info on all children paragraphs.
            if (StructuralCache.CurrentFormatContext.FinitePage)
            {
                Debug.Assert(_ur == null);

                // Get list of dtrs for the container paragraph
                DtrList dtrs = StructuralCache.DtrsFromRange(
                    TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.BeforeStart), LastFormatCch);

                // Build update records
                if (dtrs != null)
                {
                    // The NameTable is cleaned from the start position of the first DTR.
                    // Hence all paragraphs after the first DTR are new.
                    int dcpContent = TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterStart);
                    DirtyTextRange dtr = dtrs[0];

                    // Find first paragraph affected by DTR, and set update info for it.
                    int dcpPara = dcpContent;
                    BaseParagraph para = _firstChild;
                    if (dcpPara < dtr.StartIndex)
                    {
                        while (para != null)
                        {
                            // We're looking for first affected para - We start with dco content. For
                            // all paras but TextParagraph, StartPosition/EndPosition is 
                            // |<Section></Section>|, so insertion at edge points is adding new paragraphs,
                            // not affecting current. For textpara, <Paragraph>|abcde|</Paragraph>, 
                            // insertion at edge points is a change inside for that text paragraph.
                            if (
                                dcpPara + para.LastFormatCch > dtr.StartIndex || 
                                ((dcpPara + para.LastFormatCch == dtr.StartIndex) && para is TextParagraph)
                               )
                            {
                                break; // the first paragraph is found
                            }
                            dcpPara += para.Cch;
                            para = para.Next;
                        }
                        if (para != null)
                        {
                            para.SetUpdateInfo(PTS.FSKCHANGE.fskchInside, false);
                        }
                    }
                    else
                    {
                        para.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                    }

                    // All following paragraph are new.
                    if (para != null)
                    {
                        para = para.Next;
                        while (para != null)
                        {
                            para.SetUpdateInfo(PTS.FSKCHANGE.fskchNew, false);
                            para = para.Next;
                        }
                    }

                    _changeType = PTS.FSKCHANGE.fskchInside;
                }
            }
            fskch = _changeType;
        }

        //-------------------------------------------------------------------
        // GetParaProperties
        //-------------------------------------------------------------------
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)                // OUT: paragraph properties
        {
            GetParaProperties(ref fspap, false);
            fspap.idobj = PtsHost.ContainerParagraphId;
        }

        //-------------------------------------------------------------------
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. ContainerParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            ContainerParaClient paraClient =  new ContainerParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518
        }

        //-------------------------------------------------------------------
        // FormatParaFinite
        //-------------------------------------------------------------------
        internal void FormatParaFinite(
            ContainerParaClient paraClient,     // IN:
            IntPtr pbrkrecIn,                   // IN:  break record---use if !IntPtr.Zero
            int fBRFromPreviousPage,            // IN:  break record was created on previous page
            int iArea,                          // IN:  column-span area index
            IntPtr footnoteRejector,            // IN:
            IntPtr geometry,                    // IN:  pointer to geometry
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
            uint fswdirSubtrack = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            Debug.Assert(iArea == 0);
            // Currently it is possible to get MCS and BR in following situation:
            // At the end of the page there is a paragraph with delayed figure, so the figure
            // gets delayed to the next page. But part of the next paragraph fits in the page,
            // so it gets broken. PTS creates BR with delayed figure and broken para.
            // PTS will format the next page starting from delayed figure, which can produce MCS.
            // So when the next paragraph is continued from BR, it has MCS.
            // This problem is currently investigated by PTS team: PTSLS bug 915.
            // For now, MCS gets ignored here.
            //ErrorHandler.Assert(pbrkrecIn == IntPtr.Zero || mcs == null, ErrorHandler.BrokenParaHasMcs);
            if (mcs != null && pbrkrecIn != IntPtr.Zero)
            {
                mcs = null;
            }

            int marginTop = 0;
            int marginBottom = 0;
            MarginCollapsingState mcsContainer = null;

            // Set clear property
            Invariant.Assert(Element is Block || Element is ListItem);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));


            // Take into accound MBPs and modify subtrack metrics,
            // and make sure that subtrack is at least 1 unit wide (cannot measure at width <= 0)
            PTS.FSRECT fsrcToFillSubtrack = fsrcToFill;
            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);

            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;

                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFillSubtrack, fswdirSubtrack, out fsrcToFillSubtrack));
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFill, fswdirSubtrack, out fsrcToFill));
                mbp.MirrorMargin();
            }

            fsrcToFillSubtrack.u  += mbp.MBPLeft;
            fsrcToFillSubtrack.du -= mbp.MBPLeft + mbp.MBPRight;
            fsrcToFillSubtrack.u  = Math.Max(Math.Min(fsrcToFillSubtrack.u, fsrcToFill.u + fsrcToFill.du - 1), fsrcToFill.u);
            fsrcToFillSubtrack.du = Math.Max(fsrcToFillSubtrack.du, 0);

            if (pbrkrecIn == IntPtr.Zero)
            {
                // Top margin collapsing. If suppresing top space, top margin is always 0.
                MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsContainer, out marginTop);
                if (PTS.ToBoolean(fSuppressTopSpace))
                {
                    marginTop = 0;
                }

                fsrcToFillSubtrack.v  += marginTop + mbp.BPTop;
                fsrcToFillSubtrack.dv -= marginTop + mbp.BPTop;
                fsrcToFillSubtrack.v = Math.Max(Math.Min(fsrcToFillSubtrack.v, fsrcToFill.v + fsrcToFill.dv - 1), fsrcToFill.v);
                fsrcToFillSubtrack.dv = Math.Max(fsrcToFillSubtrack.dv, 0);
            }

            // Format subtrack
            int dvrSubTrackTopSpace = 0;
            try
            {
                PTS.Validate(PTS.FsFormatSubtrackFinite(PtsContext.Context, pbrkrecIn, fBRFromPreviousPage, this.Handle, iArea, 
                    footnoteRejector, geometry, fEmptyOk, fSuppressTopSpace, fswdirSubtrack, ref fsrcToFillSubtrack, 
                    (mcsContainer != null) ? mcsContainer.Handle : IntPtr.Zero, fskclearIn, 
                    fsksuppresshardbreakbeforefirstparaIn, 
                    out fsfmtr, out pfspara, out pbrkrecOut, out dvrUsed, out fsbbox, out pmcsclientOut, out fskclearOut, 
                    out dvrSubTrackTopSpace), PtsContext);
            }
            finally
            {
                // Destroy top margin collapsing state (not needed anymore).
                if (mcsContainer != null)
                {
                    mcsContainer.Dispose();
                    mcsContainer = null;
                }
                // When possible in the future, remove this workaround for PTS uninitialized variable.
                if (dvrSubTrackTopSpace > PTS.dvBottomUndefined / 2)
                {
                    dvrSubTrackTopSpace = 0;
                }
            }

            // Take into accound MBPs and modify subtrack metrics
            dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrSubTrackTopSpace;
            dvrUsed += (fsrcToFillSubtrack.v - fsrcToFill.v);

            // Initialize subtrack metrics
            if (fsfmtr.kstop >= PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace)   // No progress or collision
            {
                dvrUsed = 0;
            }

            // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
            // If necessary, new MCS is created and passed back to PTS.
            if (pmcsclientOut != IntPtr.Zero)
            {
                mcsContainer = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcsContainer);
                pmcsclientOut = IntPtr.Zero;
            }

            if (fsfmtr.kstop == PTS.FSFMTRKSTOP.fmtrGoalReached)
            {
                // Bottom margin collapsing:
                // (a) retrieve mcs from the subtrack
                // (b) do margin collapsing; create a new margin collapsing state
                // There is no bottom margin collapsing if paragraph will be continued (output break record is not null).
                MarginCollapsingState mcsNew = null;
                MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsContainer, out mcsNew, out marginBottom);
                pmcsclientOut = (mcsNew != null) ? mcsNew.Handle : IntPtr.Zero;

                // If we exceed fill rectangle after adding bottom border and padding, clip them
                dvrUsed += marginBottom + mbp.BPBottom;
                dvrUsed = Math.Min(fsrcToFill.dv, dvrUsed);
            }

            // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
            // If necessary, new MCS is created and passed back to PTS.
            if (mcsContainer != null)
            {
                mcsContainer.Dispose();
                mcsContainer = null;
            }

            // Adjust fsbbox to account for margins
            fsbbox.fsrc.u -= mbp.MBPLeft;
            fsbbox.fsrc.du += mbp.MBPLeft + mbp.MBPRight;

            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubtrack, ref pageRect, ref fsbbox, fswdir, out fsbbox));
            }

            // Update information about first/last chunk
            paraClient.SetChunkInfo(pbrkrecIn == IntPtr.Zero, pbrkrecOut == IntPtr.Zero);
        }

        //-------------------------------------------------------------------
        // FormatParaBottomless
        //-------------------------------------------------------------------
        internal void FormatParaBottomless(
            ContainerParaClient paraClient,     // IN:
            int iArea,                          // IN:  column-span area index
            IntPtr geometry,                    // IN:  pointer to geometry
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
            uint fswdirSubtrack = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            Debug.Assert(iArea == 0);

            // Top margin collapsing.
            int marginTop;
            MarginCollapsingState mcsContainer;

            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsContainer, out marginTop);
            if (PTS.ToBoolean(fSuppressTopSpace))
            {
                marginTop = 0;
            }

            // Set clear property
            Invariant.Assert(Element is Block || Element is ListItem);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT fsrcToFillSubtrack = new PTS.FSRECT(urTrack, 0, durTrack, 0);
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFillSubtrack, fswdirSubtrack, out fsrcToFillSubtrack));

                urTrack = fsrcToFillSubtrack.u;
                durTrack = fsrcToFillSubtrack.du;
                mbp.MirrorMargin();
            }

            // Take into accound MBPs and modify subtrack metrics,
            // and make sure that subtrack is at least 1 unit wide (cannot measure at width <= 0)
            int urSubtrack, durSubtrack, vrSubtrack;
            int dvrSubTrackTopSpace = 0;
            urSubtrack  = Math.Max(Math.Min(urTrack + mbp.MBPLeft, urTrack + durTrack - 1), urTrack);
            durSubtrack = Math.Max(durTrack - (mbp.MBPLeft + mbp.MBPRight), 0);
            vrSubtrack  = vrTrack + (marginTop + mbp.BPTop);

            // Format subtrack
            try
            {
                PTS.Validate(PTS.FsFormatSubtrackBottomless(PtsContext.Context, this.Handle, iArea, 
                    geometry, fSuppressTopSpace, fswdirSubtrack, urSubtrack, durSubtrack, vrSubtrack, 
                    (mcsContainer != null) ? mcsContainer.Handle : IntPtr.Zero, fskclearIn, fInterruptable, 
                    out fsfmtrbl, out pfspara, out dvrUsed, out fsbbox, out pmcsclientOut, 
                    out fskclearOut, out dvrSubTrackTopSpace, out fPageBecomesUninterruptable), PtsContext);
            }
            finally
            {
                // Destroy top margin collapsing state (not needed anymore).
                if (mcsContainer != null)
                {
                    mcsContainer.Dispose();
                    mcsContainer = null;
                }
            }

            if (fsfmtrbl != PTS.FSFMTRBL.fmtrblCollision)
            {
                // Bottom margin collapsing:
                // (1) retrieve mcs from the subtrack
                // (2) do margin collapsing; create a new margin collapsing state
                if (pmcsclientOut != IntPtr.Zero)
                {
                    mcsContainer = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                    PTS.ValidateHandle(mcsContainer);
                    pmcsclientOut = IntPtr.Zero;
                }
                int marginBottom;
                MarginCollapsingState mcsNew;
                MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsContainer, out mcsNew, out marginBottom);
                pmcsclientOut = (mcsNew != null) ? mcsNew.Handle : IntPtr.Zero;

                // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
                // If necessary, new MCS is created and passed back to PTS.
                if (mcsContainer != null)
                {
                    mcsContainer.Dispose();
                    mcsContainer = null;
                }

                // Take into accound MBPs and modify subtrack metrics
                dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrSubTrackTopSpace;
                dvrUsed += (vrSubtrack - vrTrack) + marginBottom + mbp.BPBottom;
            }
            else
            {
                Debug.Assert(pmcsclientOut == IntPtr.Zero);
                pfspara = IntPtr.Zero;
                dvrTopSpace = 0;
            }

            // Adjust fsbbox to account for margins
            fsbbox.fsrc.u -= mbp.MBPLeft;
            fsbbox.fsrc.du += mbp.MBPLeft + mbp.MBPRight;
            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubtrack, ref pageRect, ref fsbbox, fswdir, out fsbbox));
            }

            // Update information about first/last chunk. In bottomless scenario
            // paragraph is not broken, so there is only one chunk.
            paraClient.SetChunkInfo(true, true);
        }

        //-------------------------------------------------------------------
        // UpdateBottomlessPara
        //-------------------------------------------------------------------
        internal void UpdateBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            ContainerParaClient paraClient,     // IN:
            int iArea,                          // IN:  column-span area index
            IntPtr pfsgeom,                     // IN:  pointer to geometry
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
            uint fswdirSubtrack = PTS.FlowDirectionToFswdir(((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty)));

            Debug.Assert(iArea == 0);

            // Top margin collapsing.
            int marginTop;
            MarginCollapsingState mcsContainer;

            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsContainer, out marginTop);
            if (PTS.ToBoolean(fSuppressTopSpace))
            {
                marginTop = 0;
            }

            // Set clear property
            Invariant.Assert(Element is Block || Element is ListItem);
            fskclearIn = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT fsrcToFillSubtrack = new PTS.FSRECT(urTrack, 0, durTrack, 0);
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(fswdir, ref pageRect, ref fsrcToFillSubtrack, fswdirSubtrack, out fsrcToFillSubtrack));

                urTrack = fsrcToFillSubtrack.u;
                durTrack = fsrcToFillSubtrack.du;
                mbp.MirrorMargin();
            }

            // Take into accound MBPs and modify subtrack metrics,
            // and make sure that subtrack is at least 1 unit wide (cannot measure at width <= 0)
            int urSubtrack, durSubtrack, vrSubtrack;
            int dvrSubTrackTopSpace = 0;
            urSubtrack  = Math.Max(Math.Min(urTrack + mbp.MBPLeft, urTrack + durTrack - 1), urTrack);
            durSubtrack = Math.Max(durTrack - (mbp.MBPLeft + mbp.MBPRight), 0);
            vrSubtrack  = vrTrack + (marginTop + mbp.BPTop);

            // Format subtrack
            try
            {
                PTS.Validate(PTS.FsUpdateBottomlessSubtrack(PtsContext.Context, pfspara, this.Handle, iArea,
                    pfsgeom, fSuppressTopSpace, fswdirSubtrack, urSubtrack, durSubtrack, vrSubtrack, 
                    (mcsContainer != null) ? mcsContainer.Handle : IntPtr.Zero, fskclearIn, fInterruptable, 
                    out fsfmtrbl, out dvrUsed, out fsbbox, out pmcsclientOut, 
                    out fskclearOut, out dvrSubTrackTopSpace, out fPageBecomesUninterruptable), PtsContext);
            }
            finally
            {
                // Destroy top margin collapsing state (not needed anymore).
                if (mcsContainer != null)
                {
                    mcsContainer.Dispose();
                    mcsContainer = null;
                }
            }

            if (fsfmtrbl != PTS.FSFMTRBL.fmtrblCollision)
            {
                // Bottom margin collapsing:
                // (1) retrieve mcs from the subtrack
                // (2) do margin collapsing; create a new margin collapsing state
                if (pmcsclientOut != IntPtr.Zero)
                {
                    mcsContainer = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                    PTS.ValidateHandle(mcsContainer);
                    pmcsclientOut = IntPtr.Zero;
                }
                int marginBottom;
                MarginCollapsingState mcsNew;
                MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcsContainer, out mcsNew, out marginBottom);
                pmcsclientOut = (mcsNew != null) ? mcsNew.Handle : IntPtr.Zero;

                // Since MCS returned by PTS is never passed back, destroy MCS provided by PTS.
                // If necessary, new MCS is created and passed back to PTS.
                if (mcsContainer != null)
                {
                    mcsContainer.Dispose();
                    mcsContainer = null;
                }

                // Take into accound MBPs and modify subtrack metrics
                dvrTopSpace = (mbp.BPTop != 0) ? marginTop : dvrSubTrackTopSpace;
                dvrUsed += (vrSubtrack - vrTrack) + marginBottom + mbp.BPBottom;
            }
            else
            {
                Debug.Assert(pmcsclientOut == IntPtr.Zero);
                pfspara = IntPtr.Zero;
                dvrTopSpace = 0;
            }

            // Adjust fsbbox to account for margins
            fsbbox.fsrc.u -= mbp.MBPLeft;
            fsbbox.fsrc.du += mbp.MBPLeft + mbp.MBPRight;
            if(fswdirSubtrack != fswdir)
            {
                PTS.FSRECT pageRect = StructuralCache.CurrentFormatContext.PageRect;
                PTS.Validate(PTS.FsTransformBbox(fswdirSubtrack, ref pageRect, ref fsbbox, fswdir, out fsbbox));
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
            BaseParagraph paraChild = _firstChild;
            while (paraChild != null)
            {
                paraChild.ClearUpdateInfo();
                paraChild = paraChild.Next;
            }
            base.ClearUpdateInfo();
            _ur = null;
            _firstParaValidInUpdateMode = false;
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
            BaseParagraph paraChild;

            int openEdgeCp = ParagraphStartCharacterPosition;
            if (startPosition <= openEdgeCp + TextContainerHelper.ElementEdgeCharacterLength) // If before or equal to content start, whole para content is invalid
            {
                paraChild = _firstChild;
                while (paraChild != null)
                {
                    BaseParagraph para = paraChild;
                    paraChild = paraChild.Next;
                    para.Dispose();
                    para.Next = null;
                    para.Previous = null;
                }
                _firstChild = _lastFetchedChild = null;
            }
            else
            {
                // Start enumeration from the end. Most likely content is added to the end.
                // This is very important for async loading of long documents.
                paraChild = _firstChild;

                // Move to the first paragraph that is affected by the DTR.
                while (paraChild != null)
                {
                    if ((paraChild.ParagraphStartCharacterPosition + paraChild.LastFormatCch) >= startPosition)
                    {
                        // Invalidate structure of this paragraph
                        if (!paraChild.InvalidateStructure(startPosition))
                        {
                            // Only part of this paragraph is invalid, 
                            // hence go to the next paragraph
                            paraChild = paraChild.Next;
                        }

                        // paraChild and all following paragraph are invalid.
                        // Disconnect them from the Name Table.
                        if (paraChild != null)
                        {
                            if (paraChild.Previous != null)
                            {
                                paraChild.Previous.Next = null;
                                _lastFetchedChild = paraChild.Previous;
                            }
                            else
                            {
                                _firstChild = _lastFetchedChild = null;
                            }
                            while (paraChild != null)
                            {
                                BaseParagraph para = paraChild;
                                paraChild = paraChild.Next;
                                para.Dispose();
                                para.Next = null;
                                para.Previous = null;
                            }
                        }
                        break;
                    }
                    paraChild = paraChild.Next;
                }
            }
            return (startPosition < openEdgeCp + TextContainerHelper.ElementEdgeCharacterLength);
        }
        
        // ------------------------------------------------------------------
        // Invalidate accumulated format caches.
        // ------------------------------------------------------------------
        internal override void InvalidateFormatCache()
        {
            BaseParagraph para = _firstChild;
            while (para != null)
            {
                para.InvalidateFormatCache();
                para = para.Next;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // ------------------------------------------------------------------
        // Determine paragraph type at the current tree position and create it.
        // If going out of scope, return null.
        //
        // Paragraph type is determined using following:
        // (1) if textPointer points to Text, TextParagraph
        // (2) if textPointer points to ElementEnd (end of TextElement):
        //     * if the same as Owner, NULL if fEmptyOk, TextPara otherwise
        // (3) if textPointer points to ElementStart (start of TextElement):
        //     * if block, ContainerParagraph
        //     * if inline, TextParagraph
        // (4) if textPointer points to UIElement:
        //     * if block, UIElementParagraph
        //     * if inline, TextParagraph
        // (5) if textPointer points to TextContainer.End, NULL
        // ------------------------------------------------------------------
        protected virtual BaseParagraph GetParagraph(ITextPointer textPointer, bool fEmptyOk)
        {
            BaseParagraph paragraph = null;

            switch (textPointer.GetPointerContext(LogicalDirection.Forward))
            {
                case TextPointerContext.Text:
                    // Text paragraph

                    // WORKAROUND FOR SCHEMA VALIDATION
                    if(textPointer.TextContainer.Start.CompareTo(textPointer) > 0)
                    {
                        if(!(Element is TextElement) || ((TextElement)Element).ContentStart != textPointer)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.TextSchema_TextIsNotAllowedInThisContext, Element.GetType().Name));
                        }
                    }

                    paragraph = new TextParagraph(Element, StructuralCache);
                    break;

                case TextPointerContext.ElementEnd:
                    // The end of TextElement
                    Invariant.Assert(textPointer is TextPointer);
                    Invariant.Assert(Element == ((TextPointer)textPointer).Parent);

                    if(!fEmptyOk)
                    {
                        paragraph = new TextParagraph(Element, StructuralCache);
                    }
                    break;

                case TextPointerContext.ElementStart:
                    // The beginning of TextElement
                    // * if block, ContainerParagraph
                    // * if inline, TextParagraph
                    Debug.Assert(textPointer is TextPointer);
                    TextElement element = ((TextPointer)textPointer).GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                    if (element is List)
                    {
                        paragraph = new ListParagraph(element, StructuralCache);
                    }
                    else if (element is Table)
                    {
                        paragraph = new TableParagraph(element, StructuralCache);
                    }
                    else if (element is BlockUIContainer)
                    {
                        paragraph = new UIElementParagraph(element, StructuralCache);
                    }
                    else if (element is Block || element is ListItem)
                    {
                        paragraph = new ContainerParagraph(element, StructuralCache);
                    }
                    else if (element is Inline) // Note this includes AnchoredBlocks - intentionally
                    {
                        paragraph = new TextParagraph(Element, StructuralCache);
                    }
                    else
                    {
                        // The only remaining TextElement classes are: TableRowGroup, TableRow, TableCell
                        // which should never go here.
                        Invariant.Assert(false);
                    }
                    break;

                case TextPointerContext.EmbeddedElement:
                    // Embedded UIElements are always part of TextParagraph.
                    // There is no possibility to make UIElement a block.
                    paragraph = new TextParagraph(Element, StructuralCache);
                    break;

                case TextPointerContext.None:
                    // End of tree case.
                    Invariant.Assert(textPointer.CompareTo(textPointer.TextContainer.End) == 0);

                    if (!fEmptyOk)
                    {
                        paragraph = new TextParagraph(Element, StructuralCache);
                    }
                    break;
            }

            if (paragraph != null)
            {
                StructuralCache.CurrentFormatContext.DependentMax = (TextPointer) textPointer;
            }

            return paragraph;
        }
        
        // ------------------------------------------------------------------
        // Does this paragraph needs update?
        // ------------------------------------------------------------------
        private bool NeedsUpdate()
        {
            // Get list of DTRs for the container paragraph. 
            // Starting from the first character of the container (BeforeEdge). 
            DtrList dtrs = StructuralCache.DtrsFromRange(ParagraphStartCharacterPosition, LastFormatCch);
            return (dtrs != null);
        }

        // ------------------------------------------------------------------
        // Build update record for the paragraph.
        // ------------------------------------------------------------------
        private void BuildUpdateRecord()
        {
            _ur = null;
            UpdateRecord ur;

            // Get list of dtrs for the container paragraph

            DtrList dtrs = StructuralCache.DtrsFromRange(ParagraphStartCharacterPosition, LastFormatCch);

            // Build update records
            if (dtrs != null)
            {
                UpdateRecord urPrev = null;
                for (int i = 0; i < dtrs.Length; i++)
                {
                    // Dtr start index has been scaled to be relative to our range.

                    int cpContent = TextContainerHelper.GetCPFromElement(StructuralCache.TextContainer, Element, ElementEdge.AfterStart);

                    ur = UpdateRecordFromDtr(dtrs, dtrs[i], cpContent);

                    // Link UpdateRecord to the previous one
                    if (urPrev == null)
                    {
                        _ur = ur;
                    }
                    else
                    {
                        urPrev.Next = ur;
                    }
                    urPrev = ur;
                }

                // There might be a case when 2 adjacent update records are overlapping 
                // the same paragraph. In this case they have to be merged.
                ur = _ur;
                while (ur.Next != null)
                {
                    // Determine if 2 UpdateRecords are overlapping the same paragraph.
                    // Because DTRs of update records are not overlapping it is enough 
                    // to compare last affected paragraph of the first UpdateRecord 
                    // with the first affected paragraph of the second UpdateRecord.
                    if (ur.SyncPara != null)
                    {
                        if (ur.SyncPara.Previous == ur.Next.FirstPara)
                        {
                            ur.MergeWithNext();
                            continue; // don't go to next, because it has been merged
                        }
                        else if (ur.SyncPara == ur.Next.FirstPara && ur.Next.ChangeType == PTS.FSKCHANGE.fskchNew)
                        {
                            ur.MergeWithNext();
                            continue; // don't go to next, because it has been merged
                        }
                    }
                    else
                    {
                        Debug.Assert(ur.Next.FirstPara == null || ur.Next.FirstPara.Next == null);
                        ur.MergeWithNext();
                        continue; // don't go to next, because it has been merged
                    }

                    // Try to merge next UpdateRecords
                    ur = ur.Next;
                }
            }

            // Disconnect obsolete paragraphs
            ur = _ur;
            while (ur != null && ur.FirstPara != null)
            {
                BaseParagraph paraInvalid = null;
                if (ur.ChangeType == PTS.FSKCHANGE.fskchInside)
                {
                    paraInvalid = ur.FirstPara.Next;
                    ur.FirstPara.Next = null;
                }
                else
                {
                    paraInvalid = ur.FirstPara;
                }
                while (paraInvalid != ur.SyncPara)
                {
                    if (paraInvalid.Next != null)
                    {
                        paraInvalid.Next.Previous = null;
                    }
                    if (paraInvalid.Previous != null)
                    {
                        paraInvalid.Previous.Next = null;
                    }
                    paraInvalid.Dispose();
                    paraInvalid = paraInvalid.Next;
                }
                ur = ur.Next;
            }

            // Initalize update state of the paragraph
            if (_ur != null)
            {
                // If the first UpdateRecord points to the first paragraph and
                // this paragraph is new, reinitialize first para.
                if (_ur.FirstPara == _firstChild && _ur.ChangeType == PTS.FSKCHANGE.fskchNew)
                {
                    _firstChild = null;
                }
            }
            _firstParaValidInUpdateMode = true;
        }

        // ------------------------------------------------------------------
        // Build UpdateRecord from DTR.
        // ------------------------------------------------------------------
        private UpdateRecord UpdateRecordFromDtr(
            DtrList dtrs,
            DirtyTextRange dtr,
            int dcpContent)
        {
            UpdateRecord ur = new UpdateRecord();

            // (1) Initialize DTR
            ur.Dtr = dtr;

            // (2) Find first paragraph affected by DTR
            BaseParagraph para = _firstChild;
            BaseParagraph paraPrev = null;
            // There might be gaps between paragraphs (example: content of List element, only 
            // nested Lists or ListItems are valid paragraphs, all other content is skipped).
            // For this reason always use para.ParagraphStartCharacterPosition to get the first
            // character position of the current paragraph.
            int dcpPara = dcpContent;
            if (dcpPara < ur.Dtr.StartIndex)
            {
                while (para != null)
                {
                    // We're looking for first affected para - We start with dco content. For
                    // all paras but TextParagraph, StartPosition/EndPosition is 
                    // |<Section></Section>|, so insertion at edge points is adding new paragraphs,
                    // not affecting current. For textpara, <Paragraph>|abcde|</Paragraph>, 
                    // insertion at edge points is a change inside for that text paragraph.
                    if (
                        dcpPara + para.LastFormatCch > ur.Dtr.StartIndex ||
                        (dcpPara + para.LastFormatCch == ur.Dtr.StartIndex && para is TextParagraph))
                    {
                        break; // the first paragraph is found
                    }

                    dcpPara += para.LastFormatCch;
                    paraPrev = para;
                    para = para.Next;
                }
            }
            // else the change is before the first paragraph
            ur.FirstPara = para;

            // (3) Determine change type for the fist affected paragraph
            if (para == null)
            {
                ur.ChangeType = PTS.FSKCHANGE.fskchNew;
            }
            else if (dcpPara < ur.Dtr.StartIndex)
            {
                ur.ChangeType = PTS.FSKCHANGE.fskchInside;
            }
            else
            {
                ur.ChangeType = PTS.FSKCHANGE.fskchNew;
            }

            // (4) Find synchronization point, the first paragraph after DTR
            ur.SyncPara = null;
            while (para != null)
            {
                if (   (dcpPara + para.LastFormatCch > ur.Dtr.StartIndex + ur.Dtr.PositionsRemoved)
                    || (dcpPara + para.LastFormatCch == ur.Dtr.StartIndex + ur.Dtr.PositionsRemoved && ur.ChangeType != PTS.FSKCHANGE.fskchNew))
                {
                    ur.SyncPara = para.Next;
                    break;
                }
                dcpPara += para.LastFormatCch;
                para = para.Next;
            }
            return ur;
        }

        #endregion Private Methods

        // ------------------------------------------------------------------
        // The first/last child paragraph.
        // ------------------------------------------------------------------
        private BaseParagraph _firstChild;
        private BaseParagraph _lastFetchedChild;

        // ------------------------------------------------------------------
        // Update record.
        // ------------------------------------------------------------------
        private UpdateRecord _ur;

        // ------------------------------------------------------------------
        // In update mode PTS may decide to do full recalc of the page.
        // In such case paragraphs of the main text segment are not valid anymore.
        // If this flag is true, cached paragraphs can be used in GetFirst/NextPara.
        // ------------------------------------------------------------------
        private bool _firstParaValidInUpdateMode;
    }
}

#pragma warning enable 1634, 1691

