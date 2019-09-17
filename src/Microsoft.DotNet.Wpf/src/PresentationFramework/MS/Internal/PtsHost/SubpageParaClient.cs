// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: SubpageParaClient is responsible for handling display
//              related data of subpages.
//


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // SubpageParaClient is responsible for handling display related data 
    // of subpages.
    // ----------------------------------------------------------------------
    internal class SubpageParaClient : BaseParaClient
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph associated with this object.
        // ------------------------------------------------------------------
        internal SubpageParaClient(SubpageParagraph paragraph) : base(paragraph)
        {
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <remarks>
        /// This method is called by PTS to notify that this para client is not is use anymore.
        /// </remarks>
        public override void Dispose()
        {
            _visual = null;

            if (_paraHandle.Value != IntPtr.Zero)
            {
                PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, _paraHandle.Value));
                _paraHandle.Value = IntPtr.Zero;
            }

            base.Dispose();
            GC.SuppressFinalize(this);
        }


        // ------------------------------------------------------------------
        // Arrange paragraph.
        // ------------------------------------------------------------------
        protected override void OnArrange()
        {
            base.OnArrange();

            ((SubpageParagraph)Paragraph).UpdateSegmentLastFormatPositions();

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ThisFlowDirection != PageFlowDirection)
            {
                mbp.MirrorBP();
            }

            if (!IsFirstChunk)
            {
                mbp.Border = new Thickness(mbp.Border.Left, 0.0, mbp.Border.Right, mbp.Border.Bottom);
                mbp.Padding = new Thickness(mbp.Padding.Left, 0.0, mbp.Padding.Right, mbp.Padding.Bottom);
            }

            if (!IsLastChunk) 
            {
                mbp.Border = new Thickness(mbp.Border.Left, mbp.Border.Top, mbp.Border.Right, 0.0);
                mbp.Padding = new Thickness(mbp.Padding.Left, mbp.Padding.Top, mbp.Padding.Right, 0.0);
            }


            _contentRect.u = _rect.u + mbp.BPLeft;
            _contentRect.du = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.du - mbp.BPRight - mbp.BPLeft);
            _contentRect.v = _rect.v + mbp.BPTop;
            _contentRect.dv = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.dv - mbp.BPBottom - mbp.BPTop);

            _paddingRect.u = _rect.u + mbp.BorderLeft;
            _paddingRect.du = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.du - mbp.BorderRight - mbp.BorderLeft);
            _paddingRect.v = _rect.v + mbp.BorderTop;
            _paddingRect.dv = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.dv - mbp.BorderBottom - mbp.BorderTop);

            // Arrange subpage content. Subpage content may be simple or complex -
            // depending of set of features used in the content of the subpage.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                _pageContextOfThisPage.PageRect = new PTS.FSRECT(subpageDetails.u.simple.trackdescr.fsrc);

                // (1) simple subpage (contains only one track)

                // Exceptions don't need to pop, as the top level arrange context will be nulled out if thrown.
                Paragraph.StructuralCache.CurrentArrangeContext.PushNewPageData(_pageContextOfThisPage, subpageDetails.u.simple.trackdescr.fsrc,
                                                                                Paragraph.StructuralCache.CurrentArrangeContext.FinitePage);

                PtsHelper.ArrangeTrack(PtsContext, ref subpageDetails.u.simple.trackdescr, subpageDetails.u.simple.fswdir);

                Paragraph.StructuralCache.CurrentArrangeContext.PopPageData();
            }
            else
            {
                _pageContextOfThisPage.PageRect = new PTS.FSRECT(subpageDetails.u.complex.fsrc);

                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                if (subpageDetails.u.complex.cBasicColumns != 0)
                {
                    // Retrieve description for each column.
                    PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                    PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                    // Arrange each track
                    for (int index = 0; index < arrayColumnDesc.Length; index++)
                    {
                        // Exceptions don't need to pop, as the top level arrange context will be nulled out if thrown.
                        Paragraph.StructuralCache.CurrentArrangeContext.PushNewPageData(_pageContextOfThisPage, arrayColumnDesc[index].fsrc,
                                                                                        Paragraph.StructuralCache.CurrentArrangeContext.FinitePage);

                        PtsHelper.ArrangeTrack(PtsContext, ref arrayColumnDesc[index], subpageDetails.u.complex.fswdir);

                        Paragraph.StructuralCache.CurrentArrangeContext.PopPageData();
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Hit tests to the correct IInputElement within the paragraph
        // that the mouse is over.
        // ------------------------------------------------------------------
        internal override IInputElement InputHitTest(PTS.FSPOINT pt)
        {
            IInputElement ie = null;

            if(_pageContextOfThisPage.FloatingElementList != null)
            {
                for(int index = 0; index < _pageContextOfThisPage.FloatingElementList.Count && ie == null; index++)
                {
                    BaseParaClient floatingElement = _pageContextOfThisPage.FloatingElementList[index];

                    ie = floatingElement.InputHitTest(pt);
                }
            }

            if(ie == null)
            {
                if(Rect.Contains(pt))
                {
                    if(ContentRect.Contains(pt))
                    {
                        pt = new PTS.FSPOINT(pt.u - ContentRect.u, pt.v - ContentRect.v);

                        // Query subpage details
                        PTS.FSSUBPAGEDETAILS subpageDetails;
                        PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

                        // Hittest subpage content. Subpage content may be simple or complex -
                        // depending of set of features used in the content of the page.
                        // (1) simple subpage (contains only one track)
                        // (2) complex subpage (contains columns)
                        if (PTS.ToBoolean(subpageDetails.fSimple))
                        {
                            ie = PtsHelper.InputHitTestTrack(PtsContext, pt, ref subpageDetails.u.simple.trackdescr);
                        }
                        else
                        {
                            // (2) complex page (contains columns)
                            // cBasicColumns == 0, means that subpage content is empty
                            if (subpageDetails.u.complex.cBasicColumns != 0)
                            {
                                // Retrieve description for each column.
                                PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                                PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                                // Arrange each track
                                for (int index = 0; index < arrayColumnDesc.Length && ie == null; index++)
                                {
                                    ie = PtsHelper.InputHitTestTrack(PtsContext, pt, ref arrayColumnDesc[index]);
                                }
                            }
                        }
                    }

                    if(ie == null)
                    {
                        ie = Paragraph.Element as IInputElement;
                    }
                }
            }

            return ie;
        }

        // ------------------------------------------------------------------
        // Gets ArrayList of rectangles for ContentElement e if it is
        // found
        // start: int representing start offset of e.
        // length: int representing number of positions occupied by e.
        // ------------------------------------------------------------------
        internal override List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            List<Rect> rectangles = new List<Rect>();
            if (Paragraph.Element as ContentElement == e)
            {
                // We have found the element. Return rectangles for this paragraph.
                GetRectanglesForParagraphElement(out rectangles);
            }
            else
            {
                // Query subpage details
                PTS.FSSUBPAGEDETAILS subpageDetails;
                PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

                // Check subpage content. Subpage content may be simple or complex -
                // depending of set of features used in the content of the page.
                // (1) simple subpage (contains only one track)
                // (2) complex subpage (contains columns)
                if (PTS.ToBoolean(subpageDetails.fSimple))
                {
                    // (1) simple subpage (contains only one track)
                    rectangles = PtsHelper.GetRectanglesInTrack(PtsContext, e, start, length, ref subpageDetails.u.simple.trackdescr);
                }
                else
                {
                    // (2) complex page (contains columns)
                    // cBasicColumns == 0, means that subpage content is empty
                    if (subpageDetails.u.complex.cBasicColumns != 0)
                    {
                        // Retrieve description for each column.
                        PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                        PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                        // Arrange each track
                        for (int index = 0; index < arrayColumnDesc.Length; index++)
                        {
                            List<Rect> trackRectangles = PtsHelper.GetRectanglesInTrack(PtsContext, e, start, length, ref arrayColumnDesc[index]);
                            Invariant.Assert(trackRectangles != null);
                            if (trackRectangles.Count != 0)
                            {
                                rectangles.AddRange(trackRectangles);
                            }
                        }
                    }
                }

                rectangles = PtsHelper.OffsetRectangleList(rectangles, TextDpi.FromTextDpi(ContentRect.u), TextDpi.FromTextDpi(ContentRect.v));
            }

            // Rectangles must be non-null
            Invariant.Assert(rectangles != null);
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Validate visual node associated with paragraph.
        //
        //      fskupdInherited - inherited update info
        //      fswdir - inherited flow direction
        // ------------------------------------------------------------------
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            // Draw border and background info.
            MbpInfo mbpInfo = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ThisFlowDirection != PageFlowDirection)
            {
                mbpInfo.MirrorBP();
            }

            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);
            Visual.DrawBackgroundAndBorder(backgroundBrush, mbpInfo.BorderBrush, mbpInfo.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);

            ContainerVisual pageContentVisual;
            ContainerVisual floatingElementsVisual;

            if(_visual.Children.Count != 2)
            {
                _visual.Children.Clear();
                _visual.Children.Add(new ContainerVisual());
                _visual.Children.Add(new ContainerVisual());
            }

            pageContentVisual = (ContainerVisual)_visual.Children[0];
            floatingElementsVisual = (ContainerVisual)_visual.Children[1];


            // Subpage content may be simple or complex -
            // depending of set of features used in the content of the subpage.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                // (1) simple subpage (contains only one track)
                PTS.FSKUPDATE fskupd = subpageDetails.u.simple.trackdescr.fsupdinf.fskupd;
                if (fskupd == PTS.FSKUPDATE.fskupdInherited)
                {
                    fskupd = fskupdInherited;
                }
                VisualCollection visualChildren = pageContentVisual.Children;
                if (fskupd == PTS.FSKUPDATE.fskupdNew)
                {
                    visualChildren.Clear();
                    visualChildren.Add(new ContainerVisual());
                }
                // For complex subpage SectionVisual is added. So, when morphing
                // complex subpage to simple one, remove SectionVisual.
                else if (visualChildren.Count == 1 && visualChildren[0] is SectionVisual)
                {
                    visualChildren.Clear();
                    visualChildren.Add(new ContainerVisual());
                }
                Debug.Assert(visualChildren.Count == 1 && visualChildren[0] is ContainerVisual);
                ContainerVisual trackVisual = (ContainerVisual)visualChildren[0];

                PtsHelper.UpdateTrackVisuals(PtsContext, trackVisual.Children, fskupdInherited, ref subpageDetails.u.simple.trackdescr);
            }
            else
            {
                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                bool emptySubpage = (subpageDetails.u.complex.cBasicColumns == 0);
                if (!emptySubpage)
                {
                    // Retrieve description for each column.
                    PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                    PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                    emptySubpage = (arrayColumnDesc.Length == 0);
                    if (!emptySubpage)
                    {
                        PTS.FSKUPDATE fskupd = fskupdInherited;
                        ErrorHandler.Assert(fskupd != PTS.FSKUPDATE.fskupdShifted, ErrorHandler.UpdateShiftedNotValid);
                        Debug.Assert(fskupd != PTS.FSKUPDATE.fskupdNoChange);

                        // For complex subpage SectionVisual is added. So, when morphing
                        // simple subpage to complex one, remove ParagraphVisual.
                        VisualCollection visualChildren = pageContentVisual.Children;
                        if (visualChildren.Count == 0)
                        {
                            visualChildren.Add(new SectionVisual());
                        }
                        else if (!(visualChildren[0] is SectionVisual))
                        {
                            visualChildren.Clear();
                            visualChildren.Add(new SectionVisual());
                        }
                        Debug.Assert(visualChildren.Count == 1 && visualChildren[0] is SectionVisual);
                        SectionVisual sectionVisual = (SectionVisual)visualChildren[0];

                        // Draw column rules.
                        ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(Paragraph.Element);
                        sectionVisual.DrawColumnRules(ref arrayColumnDesc, TextDpi.FromTextDpi(subpageDetails.u.complex.fsrc.v), TextDpi.FromTextDpi(subpageDetails.u.complex.fsrc.dv), columnProperties);

                        visualChildren = sectionVisual.Children;
                        if (fskupd == PTS.FSKUPDATE.fskupdNew)
                        {
                            visualChildren.Clear();
                            for (int index = 0; index < arrayColumnDesc.Length; index++)
                            {
                                visualChildren.Add(new ContainerVisual());
                            }
                        }
                        ErrorHandler.Assert(visualChildren.Count == arrayColumnDesc.Length, ErrorHandler.ColumnVisualCountMismatch);
                        for (int index = 0; index < arrayColumnDesc.Length; index++)
                        {
                            ContainerVisual trackVisual = (ContainerVisual)visualChildren[index];

                            PtsHelper.UpdateTrackVisuals(PtsContext, trackVisual.Children, fskupdInherited, ref arrayColumnDesc[index]);
                        }
                    }
                }
                if (emptySubpage)
                {
                    // There is no content, remove all existing visuals.
                    _visual.Children.Clear();
                }
            }

            pageContentVisual.Offset = new PTS.FSVECTOR(ContentRect.u, ContentRect.v).FromTextDpi();
            floatingElementsVisual.Offset = new PTS.FSVECTOR(ContentRect.u, ContentRect.v).FromTextDpi();

            PTS.FSRECT clipRect = new PTS.FSRECT(_paddingRect.u - _contentRect.u, _paddingRect.v - _contentRect.v, _paddingRect.du, _paddingRect.dv);
            PtsHelper.ClipChildrenToRect(_visual, clipRect.FromTextDpi());
            PtsHelper.UpdateFloatingElementVisuals(floatingElementsVisual, _pageContextOfThisPage.FloatingElementList);
}


        // ------------------------------------------------------------------
        // Updates viewport
        // ------------------------------------------------------------------
        internal override void UpdateViewport(ref PTS.FSRECT viewport)
        {
            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            PTS.FSRECT viewportSubpage = new PTS.FSRECT();

            viewportSubpage.u = viewport.u - ContentRect.u;
            viewportSubpage.v = viewport.v - ContentRect.v;
            viewportSubpage.du = viewport.du;
            viewportSubpage.dv = viewport.dv;

            // Subpage content may be simple or complex -
            // depending of set of features used in the content of the subpage.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                PtsHelper.UpdateViewportTrack(PtsContext, ref subpageDetails.u.simple.trackdescr, ref viewportSubpage);
            }
            else
            {
                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                bool emptySubpage = (subpageDetails.u.complex.cBasicColumns == 0);
                if (!emptySubpage)
                {
                    // Retrieve description for each column.
                    PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                    PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                    emptySubpage = (arrayColumnDesc.Length == 0);
                    if (!emptySubpage)
                    {
                        for (int index = 0; index < arrayColumnDesc.Length; index++)
                        {
                            PtsHelper.UpdateViewportTrack(PtsContext, ref arrayColumnDesc[index], ref viewportSubpage);
                        }
                    }
                }
            }
        }


        // ------------------------------------------------------------------
        // Create paragraph result representing this paragraph.
        // ------------------------------------------------------------------
        internal override ParagraphResult CreateParagraphResult()
        {
            return new SubpageParagraphResult(this);
        }

        // ------------------------------------------------------------------
        // Return TextContentRange for the content of the paragraph.
        // ------------------------------------------------------------------
        internal override TextContentRange GetTextContentRange()
        {
            TextContentRange textContentRange;

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            // Subpage content may be simple or complex -
            // depending of set of features used in the content of the page.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                // (1) simple subpage (contains only one track)
                textContentRange = PtsHelper.TextContentRangeFromTrack(PtsContext, subpageDetails.u.simple.trackdescr.pfstrack);
            }
            else
            {
                textContentRange = new TextContentRange();

                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                if (subpageDetails.u.complex.cBasicColumns != 0)
                {
                    // Retrieve description for each column.
                    PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                    PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                    // Arrange each track
                    for (int index = 0; index < arrayColumnDesc.Length; index++)
                    {
                        // Merge TextContentRanges for all columns
                        textContentRange.Merge(PtsHelper.TextContentRangeFromTrack(PtsContext, arrayColumnDesc[index].pfstrack));
                    }
                }
            }

            TextElement elementOwner = this.Paragraph.Element as TextElement;
            // If the first paragraph is the first paragraph in the container and it is the first chunk, 
            // include start position of this element.
            if (_isFirstChunk)
            {
                textContentRange.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                    elementOwner, ElementEdge.BeforeStart));
            }

            // If the last paragraph is the last paragraph in the container and it is the last chunk, 
            // include end position of this element.
            if (_isLastChunk)
            {
                textContentRange.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                    elementOwner, ElementEdge.AfterEnd));
            }

            Invariant.Assert(textContentRange != null);
            return textContentRange;
        }

        /// <summary>
        /// Returns a new collection of ColumnResults for the subpage. Will always 
        /// have at least one column.
        /// </summary>
        /// <param name="hasTextContent">
        /// True if any column in the subpage has text content, i.e. does not contain only figures/floaters
        /// </param>
        internal ReadOnlyCollection<ColumnResult> GetColumnResults(out bool hasTextContent)
        {
            List<ColumnResult> columnResults = new List<ColumnResult>(0);
            Vector contentOffset = new Vector();

            // hasTextContent is set to true if any of the columns in the subpage has text content. This is determined by checking the columns' 
            // paragraph collections
            hasTextContent = false;

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            // Subpage content may be simple or complex -
            // depending of set of features used in the content of the page.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                // (1) simple subpage (contains only one track)
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, subpageDetails.u.simple.trackdescr.pfstrack, out trackDetails));
                if (trackDetails.cParas > 0)
                {
                    columnResults = new List<ColumnResult>(1);
                    ColumnResult columnResult = new ColumnResult(this, ref subpageDetails.u.simple.trackdescr, contentOffset);
                    columnResults.Add(columnResult);
                    if (columnResult.HasTextContent)
                    {
                        hasTextContent = true;
                    }
                }
            }
            else
            {
                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                if (subpageDetails.u.complex.cBasicColumns != 0)
                {
                    // Retrieve description for each column.
                    PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                    PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);

                    columnResults = new List<ColumnResult>(subpageDetails.u.complex.cBasicColumns);
                    for (int i = 0; i < arrayColumnDesc.Length; i++)
                    {
                        PTS.FSTRACKDETAILS trackDetails;
                        PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, arrayColumnDesc[i].pfstrack, out trackDetails));
                        if (trackDetails.cParas > 0)
                        {
                            ColumnResult columnResult = new ColumnResult(this, ref arrayColumnDesc[i], contentOffset);
                            columnResults.Add(columnResult);
                            if (columnResult.HasTextContent)
                            {
                                hasTextContent = true;
                            }
                        }
                    }
                }
            }

            return new ReadOnlyCollection<ColumnResult>(columnResults);
        }

        // ------------------------------------------------------------------
        // Returns a collection of ParagraphResults for the column's paragraphs.
        //
        //      pfstrack - Pointer to PTS track representing a column.
        //      parentOffset - Parent offset from the top of the page.
        //      hasTextContent - true if any of the children paras has text content
        // ------------------------------------------------------------------
        internal ReadOnlyCollection<ParagraphResult> GetParagraphResultsFromColumn(IntPtr pfstrack, Vector parentOffset, out bool hasTextContent)
        {
            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, pfstrack, out trackDetails));
            hasTextContent = false;

            if (trackDetails.cParas == 0) { return null; }

            PTS.FSPARADESCRIPTION[] arrayParaDesc;
            PtsHelper.ParaListFromTrack(PtsContext, pfstrack, ref trackDetails, out arrayParaDesc);

            List<ParagraphResult> paragraphResults = new List<ParagraphResult>(arrayParaDesc.Length);
            for (int i = 0; i < arrayParaDesc.Length; i++)
            {
                BaseParaClient paraClient = PtsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                ParagraphResult paragraphResult = paraClient.CreateParagraphResult();
                if (paragraphResult.HasTextContent)
                {
                    hasTextContent = true;
                }
                paragraphResults.Add(paragraphResult);
            }
            return new ReadOnlyCollection<ParagraphResult>(paragraphResults);
        }

        // ------------------------------------------------------------------
        // Retrieves text range for contents of the column represented by
        // 'pfstrack'.
        //
        //      pfstrack - Pointer to PTS track representing a column.
        // ------------------------------------------------------------------
        internal TextContentRange GetTextContentRangeFromColumn(IntPtr pfstrack)
        {
            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, pfstrack, out trackDetails));

            // Combine ranges from all nested paragraphs.
            TextContentRange textContentRange = new TextContentRange();
            if (trackDetails.cParas != 0)
            {
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                PtsHelper.ParaListFromTrack(PtsContext, pfstrack, ref trackDetails, out arrayParaDesc);

                // Merge TextContentRanges for all paragraphs
                BaseParaClient paraClient;
                for (int i = 0; i < arrayParaDesc.Length; i++)
                {
                    paraClient = PtsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);
                    textContentRange.Merge(paraClient.GetTextContentRange());
                }
            }
            return textContentRange;
        }

        // ------------------------------------------------------------------
        // Update information about first/last chunk.
        // ------------------------------------------------------------------
        internal void SetChunkInfo(bool isFirstChunk, bool isLastChunk)
        {
            _isFirstChunk = isFirstChunk;
            _isLastChunk = isLastChunk;
        }

        // ------------------------------------------------------------------
        // Is this the first chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsFirstChunk { get { return _isFirstChunk; } }
        private bool _isFirstChunk;

        // ------------------------------------------------------------------
        // Is this the last chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsLastChunk { get { return _isLastChunk; } }
        private bool _isLastChunk;

        // Floating element list
        internal ReadOnlyCollection<ParagraphResult> FloatingElementResults
        {
            get
            {
                List<ParagraphResult> floatingElements = new List<ParagraphResult>(0);
                List<BaseParaClient> floatingElementList = _pageContextOfThisPage.FloatingElementList;
                if (floatingElementList != null)
                {
                    for (int i = 0; i < floatingElementList.Count; i++)
                    {
                        ParagraphResult paragraphResult = floatingElementList[i].CreateParagraphResult();
                        floatingElements.Add(paragraphResult);
                    }
                }
                return new ReadOnlyCollection<ParagraphResult>(floatingElements);
            }
        }

        // ------------------------------------------------------------------
        // Rect of content in page coordinate system
        // ------------------------------------------------------------------
        internal PTS.FSRECT ContentRect { get { return _contentRect; } }

        private PTS.FSRECT _contentRect;
        private PTS.FSRECT _paddingRect;

        private PageContext _pageContextOfThisPage = new PageContext(); 
    }
}
