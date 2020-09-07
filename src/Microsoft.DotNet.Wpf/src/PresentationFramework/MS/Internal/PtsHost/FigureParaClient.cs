// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: FigureParaClient class is responsible for handling display 
//              related data of paragraphs associated with figures.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // UIElementParaClient class is responsible for handling display related 
    // data of paragraphs associated with figures.
    // ----------------------------------------------------------------------
    internal sealed class FigureParaClient : BaseParaClient
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph associated with this object.
        // ------------------------------------------------------------------
        internal FigureParaClient(FigureParagraph paragraph) : base(paragraph)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            if (SubpageHandle != IntPtr.Zero)
            {
                PTS.Validate(PTS.FsDestroySubpage(PtsContext.Context, SubpageHandle), PtsContext);
                SubpageHandle = IntPtr.Zero;
            }

            if(_pageContext != null)
            {
                _pageContext.RemoveFloatingParaClient(this);
            }

            base.Dispose();
        }

        // ------------------------------------------------------------------
        // Arrange paragraph.
        // ------------------------------------------------------------------
        protected override void OnArrange()
        {
            base.OnArrange();

            ((FigureParagraph)Paragraph).UpdateSegmentLastFormatPositions();

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            _pageContext.AddFloatingParaClient(this);

            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ThisFlowDirection != PageFlowDirection)
            {
                mbp.MirrorBP();
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
        // Arrange figure
        //
        //      rcFigure - rectangle of the figure
        //      rcHostPara - rectangle of the host text paragraph.
        // ------------------------------------------------------------------
        internal void ArrangeFigure(PTS.FSRECT rcFigure, PTS.FSRECT rcHostPara, uint fswdirParent, PageContext pageContext)
        {
            // Set paragraph rectangle (relative to the page)
            _rect = rcFigure;

            // Adjust rect to account for margins
            // Add margin values to rect offsets and subtract them from rect widths
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
            _rect.v += mbp.MarginTop;
            _rect.dv -= mbp.MarginTop + mbp.MarginBottom;
            _rect.u += mbp.MarginLeft;
            _rect.du -= mbp.MarginLeft + mbp.MarginRight;

            _pageContext = pageContext;

            // Cache flow directions
            _flowDirectionParent = PTS.FswdirToFlowDirection(fswdirParent);
            _flowDirection = (FlowDirection)Paragraph.Element.GetValue(FrameworkElement.FlowDirectionProperty);

            // Do paragraph specifc arrange
            OnArrange();
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
                // Query subpage details
                PTS.FSSUBPAGEDETAILS subpageDetails;
                PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

                if(Rect.Contains(pt))
                {
                    if(ContentRect.Contains(pt))
                    {
                        pt = new PTS.FSPOINT(pt.u - ContentRect.u, pt.v - ContentRect.v);

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
        // Returns ArrayList of rectangles for the given ContentElement 
        // if it is found. Returns null otherwise.
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

                // Check subpage content for element. Subpage content may be simple or complex -
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
                                // Add rectangles found in this track to all rectangles
                                rectangles.AddRange(trackRectangles);
                            }
                        }
                    }
                }

                rectangles = PtsHelper.OffsetRectangleList(rectangles, TextDpi.FromTextDpi(ContentRect.u), TextDpi.FromTextDpi(ContentRect.v));
            }

            Invariant.Assert(rectangles != null);
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Validate visual node associated with paragraph.
        //
        //      fskupdInherited - inherited update info
        // ------------------------------------------------------------------
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Figure is always reported as NEW. Override PTS inherited value.
            fskupdInherited = PTS.FSKUPDATE.fskupdNew;

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            // Obtain all mbp info
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ThisFlowDirection != PageFlowDirection)
            {
                mbp.MirrorBP();
            }

            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);
            Visual.DrawBackgroundAndBorder(backgroundBrush, mbp.BorderBrush, mbp.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);


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
                else if (visualChildren.Count == 1 && !(visualChildren[0] is ContainerVisual))
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
        // Create paragraph result representing this paragraph.
        // ------------------------------------------------------------------
        internal override ParagraphResult CreateParagraphResult()
        {
            return new FigureParagraphResult(this);
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
                    Invariant.Assert(arrayColumnDesc.Length == 1);

                    // Arrange each track
                    for (int index = 0; index < arrayColumnDesc.Length; index++)
                    {
                        // Merge TextContentRanges for all columns
                        textContentRange.Merge(PtsHelper.TextContentRangeFromTrack(PtsContext, arrayColumnDesc[index].pfstrack));
                    }
                }
            }

            // If the first paragraph is the first paragraph in the paraclient and it is the first chunk, 
            // include start position of this element.
            if (IsFirstChunk)
            {
                textContentRange.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                    Paragraph.Element as TextElement, ElementEdge.BeforeStart));
            }

            // If the last paragraph is the last paragraph in the paraclient and it is the last chunk, 
            // include end position of this element.
            if (IsLastChunk)
            {
                textContentRange.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                    Paragraph.Element as TextElement, ElementEdge.AfterEnd));
            }

            return textContentRange;
        }

        // ------------------------------------------------------------------
        // Returns a new colleciton of ParagraphResults for the contained paragraphs.
        // ------------------------------------------------------------------
        private ReadOnlyCollection<ParagraphResult> GetChildrenParagraphResults(out bool hasTextContent)
        {
            List<ParagraphResult> paragraphResults;

            // Query subpage details
            PTS.FSSUBPAGEDETAILS subpageDetails;
            PTS.Validate(PTS.FsQuerySubpageDetails(PtsContext.Context, _paraHandle.Value, out subpageDetails));

            // hasTextContent is set to true if any of the children paragraphs has text content, not just attached objects
            hasTextContent = false;

            // Subpage content may be simple or complex -
            // depending of set of features used in the content of the page.
            // (1) simple subpage (contains only one track)
            // (2) complex subpage (contains columns)
            if (PTS.ToBoolean(subpageDetails.fSimple))
            {
                // (1) simple subpage (contains only one track)
                // Get track details
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, subpageDetails.u.simple.trackdescr.pfstrack, out trackDetails));

                if (trackDetails.cParas == 0) 
                {
                    return new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));  
                }

                // Get list of paragraphs
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                PtsHelper.ParaListFromTrack(PtsContext, subpageDetails.u.simple.trackdescr.pfstrack, ref trackDetails, out arrayParaDesc);

                paragraphResults = new List<ParagraphResult>(arrayParaDesc.Length);
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
            }
            else
            {
                // (2) complex page (contains columns)
                // cBasicColumns == 0, means that subpage content is empty
                if (subpageDetails.u.complex.cBasicColumns == 0) 
                {
                    return new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));
                }

                // Retrieve description for each column.
                PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                PtsHelper.TrackListFromSubpage(PtsContext, _paraHandle.Value, ref subpageDetails, out arrayColumnDesc);
                Debug.Assert(arrayColumnDesc.Length == 1);

                // Get track details
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, arrayColumnDesc[0].pfstrack, out trackDetails));

                if (trackDetails.cParas == 0) 
                {
                    return new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));
                }

                // Get list of paragraphs
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                PtsHelper.ParaListFromTrack(PtsContext, arrayColumnDesc[0].pfstrack, ref trackDetails, out arrayParaDesc);

                paragraphResults = new List<ParagraphResult>(arrayParaDesc.Length);
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
            }
            return new ReadOnlyCollection<ParagraphResult>(paragraphResults);
        }

        /// <summary>
        /// Returns a new collection of ColumnResults for the subpage. Will always 
        /// have at least one column.
        /// </summary>
        /// <param name="hasTextContent">
        /// True if any column in the figure has text content, i.e. does not contain only figures/floaters
        /// </param>
        internal ReadOnlyCollection<ColumnResult> GetColumnResults(out bool hasTextContent)
        {
            List<ColumnResult> columnResults = new List<ColumnResult>(0);
            Vector contentOffset = new Vector();

            // hasTextContent is set to true if any of the columns has text content, not just attached object. A column has text content
            // if any of its paragraph results has text content.
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

                    // Figures are held at one column; just add the first one
                    columnResults = new List<ColumnResult>(1);
                    PTS.FSTRACKDETAILS trackDetails;
                    PTS.Validate(PTS.FsQueryTrackDetails(PtsContext.Context, arrayColumnDesc[0].pfstrack, out trackDetails));
                    if (trackDetails.cParas > 0)
                    {                            
                        ColumnResult columnResult = new ColumnResult(this, ref arrayColumnDesc[0], contentOffset);
                        columnResults.Add(columnResult);
                        if (columnResult.HasTextContent)
                        {
                            hasTextContent = true;
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
            // Figure has only one column. This is the same as getting children paragraphs.
            return GetChildrenParagraphResults(out hasTextContent);
        }

        // ------------------------------------------------------------------
        // Retrieves text range for contents of the column represented by
        // 'pfstrack'.
        //
        //      pfstrack - Pointer to PTS track representing a column.
        // ------------------------------------------------------------------
        internal TextContentRange GetTextContentRangeFromColumn(IntPtr pfstrack)
        {
            // Figure has only one column.
            return GetTextContentRange();
        }

        /// <summary>
        /// Returns tight bounding path geometry.
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            Geometry geometry = null;

            // Figure always has one column, so we can skip getting a column from the text position range
            Invariant.Assert(columns != null && columns.Count <= 1, "Columns collection is null.");
            Invariant.Assert(floatingElements != null, "Floating element collection is null.");
            ReadOnlyCollection<ParagraphResult> paragraphs = (columns.Count > 0) ? columns[0].Paragraphs : new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));

            if (paragraphs.Count > 0 || floatingElements.Count > 0)
            {
                geometry = TextDocumentView.GetTightBoundingGeometryFromTextPositionsHelper(paragraphs, floatingElements, startPosition, endPosition, TextDpi.FromTextDpi(_dvrTopSpace), visibleRect);

                //  restrict geometry to the figure's content rect boundary.
                //  because of end-of-line / end-of-para simulation calculated geometry could be larger. 
                Rect viewport = new Rect(0, 0, TextDpi.FromTextDpi(_contentRect.du), TextDpi.FromTextDpi(_contentRect.dv));
                CaretElement.ClipGeometryByViewport(ref geometry, viewport);
            }
            return (geometry);
        }

        // ------------------------------------------------------------------
        // Handle to PTS subpage object.
        // ------------------------------------------------------------------
        internal IntPtr SubpageHandle
        {
            get { return _paraHandle.Value; }

            set { _paraHandle.Value = value; }
        }

        // ------------------------------------------------------------------
        // Rect of content in page coordinate system
        // ------------------------------------------------------------------
        internal PTS.FSRECT ContentRect { get { return _contentRect; } }

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

        private PTS.FSRECT _contentRect;
        private PTS.FSRECT _paddingRect;

        // Page context this para client provides.
        private PageContext _pageContextOfThisPage = new PageContext(); 
    }
}
