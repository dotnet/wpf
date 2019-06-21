// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Helper services to query PTS objects.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Helper services to query PTS objects.
    // ----------------------------------------------------------------------
    internal static class PtsHelper
    {
        // ------------------------------------------------------------------
        //
        // Visual Helpers
        //
        // ------------------------------------------------------------------

        #region Visual Helpers

        // ------------------------------------------------------------------
        // Update mirroring transform.
        // ------------------------------------------------------------------
        internal static void UpdateMirroringTransform(FlowDirection parentFD, FlowDirection childFD, ContainerVisual visualChild, double width)
        {
            // Set mirroring transform if necessary, or clear it just in case it was set in the previous
            // format process.
            if (parentFD != childFD)
            {
                MatrixTransform transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, width, 0.0);
                visualChild.Transform = transform;
                visualChild.SetValue(FrameworkElement.FlowDirectionProperty, childFD);
            }
            else
            {
                visualChild.Transform = null;
                visualChild.ClearValue(FrameworkElement.FlowDirectionProperty);
            }
        }

        // ------------------------------------------------------------------
        // Clips visual children to a specified rect
        // ------------------------------------------------------------------
        internal static void ClipChildrenToRect(ContainerVisual visual, Rect rect)
        {
            VisualCollection visualChildren = visual.Children;

            for(int index = 0; index < visualChildren.Count; index++)
            {
                ((ContainerVisual)visualChildren[index]).Clip = new RectangleGeometry(rect);
            }
        }

        // ------------------------------------------------------------------
        // Syncs a floating element para list with a visual collection
        // ------------------------------------------------------------------
        internal static void UpdateFloatingElementVisuals(ContainerVisual visual, List<BaseParaClient> floatingElementList)
        {
            VisualCollection visualChildren = visual.Children;
            int visualIndex = 0;

            if(floatingElementList == null || floatingElementList.Count == 0)
            {
                visualChildren.Clear();
            }
            else
            {
                for(int index = 0; index < floatingElementList.Count; index++)
                {
                    Visual paraVisual = floatingElementList[index].Visual;

                    while(visualIndex < visualChildren.Count && visualChildren[visualIndex] != paraVisual)
                    {
                        visualChildren.RemoveAt(visualIndex);
                    }

                    if(visualIndex == visualChildren.Count)
                    {
                        visualChildren.Add(paraVisual);
                    }

                    visualIndex++;
                }


                if(visualChildren.Count > floatingElementList.Count)
                {
                    visualChildren.RemoveRange(floatingElementList.Count, visualChildren.Count - floatingElementList.Count);
                }
            }
        }

        #endregion Visual Helpers

        // ------------------------------------------------------------------
        //
        // Arrange Helpers
        //
        // ------------------------------------------------------------------

        #region Arrange Helpers

        //-------------------------------------------------------------------
        // Arrange PTS track.
        //-------------------------------------------------------------------
        internal static void ArrangeTrack(
            PtsContext ptsContext,
            ref PTS.FSTRACKDESCRIPTION trackDesc,
            uint fswdirTrack)
        {
            // There is possibility to get empty track. (example: large figures)
            if (trackDesc.pfstrack != IntPtr.Zero)
            {
                // Get track details
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, trackDesc.pfstrack, out trackDetails));

                // There is possibility to get empty track.
                if (trackDetails.cParas != 0)
                {
                    // Get list of paragraphs
                    PTS.FSPARADESCRIPTION[] arrayParaDesc;
                    ParaListFromTrack(ptsContext, trackDesc.pfstrack, ref trackDetails, out arrayParaDesc);

                    // Arrange paragraphs
                    ArrangeParaList(ptsContext, trackDesc.fsrc, arrayParaDesc, fswdirTrack);
                }
            }
        }

        // ------------------------------------------------------------------
        // Arrange para list.
        // ------------------------------------------------------------------
        internal static void ArrangeParaList(
            PtsContext ptsContext,
            PTS.FSRECT rcTrackContent,
            PTS.FSPARADESCRIPTION [] arrayParaDesc,
            uint fswdirTrack)
        {
            // For each paragraph, do following:
            // (1) Retrieve ParaClient object
            // (2) Arrange and update paragraph metrics
            int dvrPara = 0;
            for (int index = 0; index < arrayParaDesc.Length; index++)
            {
                // (1) Retrieve ParaClient object
                BaseParaClient paraClient = ptsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);

                // Convert to appropriate page coordinates.
                if(index == 0)
                {
                    uint fswdirPage = PTS.FlowDirectionToFswdir(paraClient.PageFlowDirection);

                    if(fswdirTrack != fswdirPage)
                    {
                        PTS.FSRECT pageRect = paraClient.Paragraph.StructuralCache.CurrentArrangeContext.PageContext.PageRect;
                        PTS.Validate(PTS.FsTransformRectangle(fswdirTrack, ref pageRect, ref rcTrackContent, fswdirPage, out rcTrackContent));
                    }
                }

                // (2) Arrange and update paragraph metrics
                int dvrTopSpace = arrayParaDesc[index].dvrTopSpace;
                PTS.FSRECT rcPara = rcTrackContent;
                rcPara.v += dvrPara + dvrTopSpace;
                rcPara.dv = arrayParaDesc[index].dvrUsed - dvrTopSpace;

                paraClient.Arrange(arrayParaDesc[index].pfspara, rcPara, dvrTopSpace, fswdirTrack);
                dvrPara += arrayParaDesc[index].dvrUsed;
            }
        }

        #endregion Arrange Helpers

        // ------------------------------------------------------------------
        //
        // Update Visual Helpers
        //
        // ------------------------------------------------------------------

        #region Update Visual Helpers

        //-------------------------------------------------------------------
        // Update PTS track (column) visuals.
        //-------------------------------------------------------------------
        internal static void UpdateTrackVisuals(
            PtsContext ptsContext,
            VisualCollection visualCollection,
            PTS.FSKUPDATE fskupdInherited,
            ref PTS.FSTRACKDESCRIPTION trackDesc)
        {
            PTS.FSKUPDATE fskupd = trackDesc.fsupdinf.fskupd;
            if (trackDesc.fsupdinf.fskupd == PTS.FSKUPDATE.fskupdInherited)
            {
                fskupd = fskupdInherited;
            }

            // If there is no change, visual information is valid
            if (fskupd == PTS.FSKUPDATE.fskupdNoChange) { return; }
            ErrorHandler.Assert(fskupd != PTS.FSKUPDATE.fskupdShifted, ErrorHandler.UpdateShiftedNotValid);

            bool emptyTrack = (trackDesc.pfstrack == IntPtr.Zero);
            if (!emptyTrack)
            {
                // Get track details
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, trackDesc.pfstrack, out trackDetails));

                emptyTrack = (trackDetails.cParas == 0);
                if (!emptyTrack)
                {
                    // Get list of paragraphs
                    PTS.FSPARADESCRIPTION[] arrayParaDesc;
                    ParaListFromTrack(ptsContext, trackDesc.pfstrack, ref trackDetails, out arrayParaDesc);

                    // Update visuals for list of paragraphs
                    UpdateParaListVisuals(ptsContext, visualCollection, fskupd, arrayParaDesc);
                }
            }

            // There is possibility to get empty track. (example: large figures)
            if (emptyTrack)
            {
                // There is no content, remove all existing children visuals.
                visualCollection.Clear();
            }
        }

        // ------------------------------------------------------------------
        // Update visuals for list of paragraphs.
        // ------------------------------------------------------------------
        internal static void UpdateParaListVisuals(
            PtsContext ptsContext,
            VisualCollection visualCollection,
            PTS.FSKUPDATE fskupdInherited,
            PTS.FSPARADESCRIPTION [] arrayParaDesc)
        {
            // For each paragraph, do following:
            // (1) Retrieve ParaClient object
            // (3) Update visual, if necessary
            for (int index = 0; index < arrayParaDesc.Length; index++)
            {
                // (1) Retrieve ParaClient object
                BaseParaClient paraClient = ptsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);

                // (2) Update visual, if necessary
                PTS.FSKUPDATE fskupd = arrayParaDesc[index].fsupdinf.fskupd;
                if (fskupd == PTS.FSKUPDATE.fskupdInherited)
                {
                    fskupd = fskupdInherited;
                }
                if (fskupd == PTS.FSKUPDATE.fskupdNew)
                {
                    // Disconnect visual from its old parent, if necessary.
                    Visual currentParent = VisualTreeHelper.GetParent(paraClient.Visual) as Visual;
                    if(currentParent != null)
                    {
                        ContainerVisual parent = currentParent as ContainerVisual;
                        Invariant.Assert(parent != null, "parent should always derives from ContainerVisual");
                        parent.Children.Remove(paraClient.Visual);                         
                    }                                          

                    // New paragraph - insert new visual node
                    visualCollection.Insert(index, paraClient.Visual);

                    paraClient.ValidateVisual(fskupd);
                }
                else
                {
                    // Remove visuals for non-existing paragraphs
                    while (visualCollection[index] != paraClient.Visual)
                    {
                        visualCollection.RemoveAt(index);
                        Invariant.Assert(index < visualCollection.Count);
                    }

                    if(fskupd == PTS.FSKUPDATE.fskupdChangeInside || fskupd == PTS.FSKUPDATE.fskupdShifted)
                    {
                        paraClient.ValidateVisual(fskupd);
                    }
                }
            }
            // Remove obsolete visuals
            if (arrayParaDesc.Length < visualCollection.Count)
            {
                visualCollection.RemoveRange(arrayParaDesc.Length, visualCollection.Count - arrayParaDesc.Length);
            }
        }

        #endregion Update Visual Helpers


        #region Update Viewport Helpers

        //-------------------------------------------------------------------
        // Update viewport for track
        //-------------------------------------------------------------------
        internal static void UpdateViewportTrack(
            PtsContext ptsContext,
            ref PTS.FSTRACKDESCRIPTION trackDesc,
            ref PTS.FSRECT viewport)
        {
            // There is possibility to get empty track. (example: large figures)
            if (trackDesc.pfstrack != IntPtr.Zero)
            {
                // Get track details
                PTS.FSTRACKDETAILS trackDetails;
                PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, trackDesc.pfstrack, out trackDetails));

                // There is possibility to get empty track.
                if (trackDetails.cParas != 0)
                {
                    // Get list of paragraphs
                    PTS.FSPARADESCRIPTION[] arrayParaDesc;
                    ParaListFromTrack(ptsContext, trackDesc.pfstrack, ref trackDetails, out arrayParaDesc);

                    // Arrange paragraphs
                    UpdateViewportParaList(ptsContext, arrayParaDesc, ref viewport);
                }
            }
        }

        // ------------------------------------------------------------------
        // Update viewport for para list
        // ------------------------------------------------------------------
        internal static void UpdateViewportParaList(
            PtsContext ptsContext,
            PTS.FSPARADESCRIPTION [] arrayParaDesc,
            ref PTS.FSRECT viewport)
        {
            for (int index = 0; index < arrayParaDesc.Length; index++)
            {
                // (1) Retrieve ParaClient object
                BaseParaClient paraClient = ptsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);

                paraClient.UpdateViewport(ref viewport);
            }
        }

        #endregion Arrange Helpers

        // ------------------------------------------------------------------
        //
        // HitTest Helpers
        //
        // ------------------------------------------------------------------

        #region HitTest Helpers

        // ------------------------------------------------------------------
        // Hit tests to the correct IInputElement within the track that the
        // mouse is over.
        // ------------------------------------------------------------------
        internal static IInputElement InputHitTestTrack(
            PtsContext ptsContext,
            PTS.FSPOINT pt,
            ref PTS.FSTRACKDESCRIPTION trackDesc)
        {
            // There is possibility to get empty track. (example: large figures)
            if (trackDesc.pfstrack == IntPtr.Zero) { return null; }

            IInputElement ie = null;

            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, trackDesc.pfstrack, out trackDetails));

            // There might be possibility to get empty track, skip the track
            // in such case.
            if (trackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                ParaListFromTrack(ptsContext, trackDesc.pfstrack, ref trackDetails, out arrayParaDesc);

                // Hittest list of paragraphs
                ie = InputHitTestParaList(ptsContext, pt, ref trackDesc.fsrc, arrayParaDesc);
            }

            return ie;
        }

        // ------------------------------------------------------------------
        // Hit tests to the correct IInputElement within the list of
        // paragraphs that the mouse is over.
        // ------------------------------------------------------------------
        internal static IInputElement InputHitTestParaList(
            PtsContext ptsContext,
            PTS.FSPOINT pt,
            ref PTS.FSRECT rcTrack,                     // track's rectangle
            PTS.FSPARADESCRIPTION [] arrayParaDesc)
        {
            IInputElement ie = null;

            for (int index = 0; index < arrayParaDesc.Length && ie == null; index++)
            {
                BaseParaClient paraClient = ptsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);

                if(paraClient.Rect.Contains(pt))
                {
                    ie = paraClient.InputHitTest(pt);
                }
            }
            return ie;
        }

        #endregion HitTest Helpers

        // ------------------------------------------------------------------
        //
        // GetRectangles Helpers
        //
        // ------------------------------------------------------------------

        #region GetRectangles Helpers

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles for the ContentElement e within
        // the specified track. If e is not found or if track contains nothing,
        // returns empty list
        //
        //      start: int representing start offset of e
        //      length: int representing number of positions occupied by e
        // ------------------------------------------------------------------
        internal static List<Rect> GetRectanglesInTrack(
            PtsContext ptsContext,
            ContentElement e,
            int start,
            int length,
            ref PTS.FSTRACKDESCRIPTION trackDesc)
        {
            List<Rect> rectangles = new List<Rect>();
            // There is possibility to get empty track. (example: large figures)
            if (trackDesc.pfstrack == IntPtr.Zero)
            {
                // TRack is empty. Return empty list.
                return rectangles;
            }

            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, trackDesc.pfstrack, out trackDetails));

            // There might be possibility to get empty track, skip the track
            // in such case.
            if (trackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                ParaListFromTrack(ptsContext, trackDesc.pfstrack, ref trackDetails, out arrayParaDesc);

                // Check list of paragraphs for element
                rectangles = GetRectanglesInParaList(ptsContext, e, start, length, arrayParaDesc);
            }
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles for the ContentElement e within the
        // list of paragraphs. If the element is not found, returns empty list.
        //      start: int representing start offset of e
        //      length: int representing number of positions occupied by e
        // ------------------------------------------------------------------
        internal static List<Rect> GetRectanglesInParaList(
            PtsContext ptsContext,
            ContentElement e,
            int start,
            int length,
            PTS.FSPARADESCRIPTION[] arrayParaDesc)
        {
            List<Rect> rectangles = new List<Rect>();

            for (int index = 0; index < arrayParaDesc.Length; index++)
            {
                BaseParaClient paraClient = ptsContext.HandleToObject(arrayParaDesc[index].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                if (start < paraClient.Paragraph.ParagraphEndCharacterPosition)
                {
                    // Element lies within the paraClient boundaries.
                    rectangles = paraClient.GetRectangles(e, start, length);

                    // Rectangles collection should not be null for consistency
                    Invariant.Assert(rectangles != null);
                    if (rectangles.Count != 0)
                    {
                        // Element cannot span more than one para client in the same track, so we stop
                        // if the element is found and the rectangles are calculated
                        break;
                    }
                }
            }
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Returns List of rectangles offset by x/y values.
        // ------------------------------------------------------------------
        internal static List<Rect> OffsetRectangleList(List<Rect> rectangleList, double xOffset, double yOffset)
        {
            List<Rect> offsetRectangles = new List<Rect>(rectangleList.Count);

            for(int index = 0; index < rectangleList.Count; index++)
            {
                Rect rect = rectangleList[index];

                rect.X += xOffset;
                rect.Y += yOffset;

                offsetRectangles.Add(rect);
            }

            return offsetRectangles;
        }

        #endregion GetRectangles Helpers

        // ------------------------------------------------------------------
        //
        // Query Helpers
        //
        // ------------------------------------------------------------------

        #region Query Helpers

        // ------------------------------------------------------------------
        // Retrieve section list from page.
        // ------------------------------------------------------------------
        internal static unsafe void SectionListFromPage(
            PtsContext ptsContext,
            IntPtr page,
            ref PTS.FSPAGEDETAILS pageDetails,
            out PTS.FSSECTIONDESCRIPTION [] arraySectionDesc)
        {
            arraySectionDesc = new PTS.FSSECTIONDESCRIPTION [pageDetails.u.complex.cSections];
            int sectionCount;
            fixed (PTS.FSSECTIONDESCRIPTION* rgSectionDesc = arraySectionDesc)
            {
                PTS.Validate(PTS.FsQueryPageSectionList(ptsContext.Context, page, pageDetails.u.complex.cSections,
                    rgSectionDesc, out sectionCount));
            }
            ErrorHandler.Assert(pageDetails.u.complex.cSections == sectionCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve track (column) list from subpage.
        // ------------------------------------------------------------------
        internal static unsafe void TrackListFromSubpage(
            PtsContext ptsContext,
            IntPtr subpage,
            ref PTS.FSSUBPAGEDETAILS subpageDetails,
            out PTS.FSTRACKDESCRIPTION [] arrayTrackDesc)
        {
            arrayTrackDesc = new PTS.FSTRACKDESCRIPTION [subpageDetails.u.complex.cBasicColumns];
            int trackCount;
            fixed (PTS.FSTRACKDESCRIPTION* rgTrackDesc = arrayTrackDesc)
            {
                PTS.Validate(PTS.FsQuerySubpageBasicColumnList(ptsContext.Context, subpage, subpageDetails.u.complex.cBasicColumns,
                    rgTrackDesc, out trackCount));
            }
            ErrorHandler.Assert(subpageDetails.u.complex.cBasicColumns == trackCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve track (column) list from section.
        // ------------------------------------------------------------------
        internal static unsafe void TrackListFromSection(
            PtsContext ptsContext,
            IntPtr section,
            ref PTS.FSSECTIONDETAILS sectionDetails,
            out PTS.FSTRACKDESCRIPTION [] arrayTrackDesc)
        {
            // Need to impl. Extended multi-column layout.
            Debug.Assert(sectionDetails.u.withpagenotes.cSegmentDefinedColumnSpanAreas == 0);
            Debug.Assert(sectionDetails.u.withpagenotes.cHeightDefinedColumnSpanAreas == 0);

            arrayTrackDesc = new PTS.FSTRACKDESCRIPTION[sectionDetails.u.withpagenotes.cBasicColumns];

            int trackCount;
            fixed (PTS.FSTRACKDESCRIPTION* rgTrackDesc = arrayTrackDesc)
            {
                PTS.Validate(PTS.FsQuerySectionBasicColumnList(ptsContext.Context, section, sectionDetails.u.withpagenotes.cBasicColumns,
                    rgTrackDesc, out trackCount));
            }
            ErrorHandler.Assert(sectionDetails.u.withpagenotes.cBasicColumns == trackCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve paragraph list from the track.
        // ------------------------------------------------------------------
        internal static unsafe void ParaListFromTrack(
            PtsContext ptsContext,
            IntPtr track,
            ref PTS.FSTRACKDETAILS trackDetails,
            out PTS.FSPARADESCRIPTION [] arrayParaDesc)
        {
            arrayParaDesc = new PTS.FSPARADESCRIPTION [trackDetails.cParas];
            int paraCount;
            fixed (PTS.FSPARADESCRIPTION* rgParaDesc = arrayParaDesc)
            {
                PTS.Validate(PTS.FsQueryTrackParaList(ptsContext.Context, track, trackDetails.cParas,
                    rgParaDesc, out paraCount));
            }
            ErrorHandler.Assert(trackDetails.cParas == paraCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve paragraph list from the track.
        // ------------------------------------------------------------------
        internal static unsafe void ParaListFromSubtrack(
            PtsContext ptsContext,
            IntPtr subtrack,
            ref PTS.FSSUBTRACKDETAILS subtrackDetails,
            out PTS.FSPARADESCRIPTION [] arrayParaDesc)
        {
            arrayParaDesc = new PTS.FSPARADESCRIPTION [subtrackDetails.cParas];
            int paraCount;
            fixed (PTS.FSPARADESCRIPTION* rgParaDesc = arrayParaDesc)
            {
                PTS.Validate(PTS.FsQuerySubtrackParaList(ptsContext.Context, subtrack, subtrackDetails.cParas,
                    rgParaDesc, out paraCount));
            }
            ErrorHandler.Assert(subtrackDetails.cParas == paraCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve simple line list from the full text paragraph.
        // ------------------------------------------------------------------
        internal static unsafe void LineListSimpleFromTextPara(
            PtsContext ptsContext,
            IntPtr para,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            out PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc)
        {
            arrayLineDesc = new PTS.FSLINEDESCRIPTIONSINGLE [textDetails.cLines];
            int lineCount;
            fixed (PTS.FSLINEDESCRIPTIONSINGLE* rgLineDesc = arrayLineDesc)
            {
                PTS.Validate(PTS.FsQueryLineListSingle(ptsContext.Context, para, textDetails.cLines,
                    rgLineDesc, out lineCount));
            }
            ErrorHandler.Assert(textDetails.cLines == lineCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve composite line list from the full text paragraph.
        // ------------------------------------------------------------------
        internal static unsafe void LineListCompositeFromTextPara(
            PtsContext ptsContext,
            IntPtr para,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            out PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc)
        {
            arrayLineDesc = new PTS.FSLINEDESCRIPTIONCOMPOSITE [textDetails.cLines];
            int lineCount;
            fixed (PTS.FSLINEDESCRIPTIONCOMPOSITE* rgLineDesc = arrayLineDesc)
            {
                PTS.Validate(PTS.FsQueryLineListComposite(ptsContext.Context, para, textDetails.cLines,
                    rgLineDesc, out lineCount));
            }
            ErrorHandler.Assert(textDetails.cLines == lineCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve line elements list from the composite line.
        // ------------------------------------------------------------------
        internal static unsafe void LineElementListFromCompositeLine(
            PtsContext ptsContext,
            ref PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc,
            out PTS.FSLINEELEMENT [] arrayLineElement)
        {
            arrayLineElement = new PTS.FSLINEELEMENT [lineDesc.cElements];
            int lineElementCount;
            fixed (PTS.FSLINEELEMENT* rgLineElement = arrayLineElement)
            {
                PTS.Validate(PTS.FsQueryLineCompositeElementList(ptsContext.Context, lineDesc.pline, lineDesc.cElements,
                    rgLineElement, out lineElementCount));
            }
            ErrorHandler.Assert(lineDesc.cElements == lineElementCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        // ------------------------------------------------------------------
        // Retrieve attached object list from the paragraph.
        // ------------------------------------------------------------------
        internal static unsafe void AttachedObjectListFromParagraph(
            PtsContext ptsContext,
            IntPtr para,
            int cAttachedObject,
            out PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc)
        {
            arrayAttachedObjectDesc = new PTS.FSATTACHEDOBJECTDESCRIPTION [cAttachedObject];
            int attachedObjectCount;
            fixed (PTS.FSATTACHEDOBJECTDESCRIPTION* rgAttachedObjectDesc = arrayAttachedObjectDesc)
            {
                PTS.Validate(PTS.FsQueryAttachedObjectList(ptsContext.Context, para, cAttachedObject, rgAttachedObjectDesc, out attachedObjectCount));
            }
            ErrorHandler.Assert(cAttachedObject == attachedObjectCount, ErrorHandler.PTSObjectsCountMismatch);
        }

        #endregion Query Helpers

        // ------------------------------------------------------------------
        //
        // Misc Helpers
        //
        // ------------------------------------------------------------------

        #region Misc Helpers

        // ------------------------------------------------------------------
        // Retrieve TextContentRange from PTS track.
        // ------------------------------------------------------------------
        internal static TextContentRange TextContentRangeFromTrack(
            PtsContext ptsContext,
            IntPtr pfstrack)
        {
            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(ptsContext.Context, pfstrack, out trackDetails));

            // Combine ranges from all nested paragraphs.
            TextContentRange textContentRange = new TextContentRange();
            if (trackDetails.cParas != 0)
            {
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                PtsHelper.ParaListFromTrack(ptsContext, pfstrack, ref trackDetails, out arrayParaDesc);

                // Merge TextContentRanges for all paragraphs
                BaseParaClient paraClient;
                for (int i = 0; i < arrayParaDesc.Length; i++)
                {
                    paraClient = ptsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);
                    textContentRange.Merge(paraClient.GetTextContentRange());
                }
            }
            return textContentRange;
        }

        // ------------------------------------------------------------------
        // Calculates a page margin adjustment to eliminate free space if column width is not flexible
        // ------------------------------------------------------------------
        internal static double CalculatePageMarginAdjustment(StructuralCache structuralCache, double pageMarginWidth)
        {
            double pageMarginAdjustment = 0.0;

            DependencyObject o = structuralCache.Section.Element;

            if(o is FlowDocument)
            {
                ColumnPropertiesGroup columnProperties = new ColumnPropertiesGroup(o);

                if(!columnProperties.IsColumnWidthFlexible)
                {                   
                    double lineHeight = DynamicPropertyReader.GetLineHeightValue(o);
                    double pageFontSize = (double)structuralCache.PropertyOwner.GetValue(Block.FontSizeProperty);
                    FontFamily pageFontFamily = (FontFamily)structuralCache.PropertyOwner.GetValue(Block.FontFamilyProperty);

                    int ccol = PtsHelper.CalculateColumnCount(columnProperties, lineHeight, pageMarginWidth, pageFontSize, pageFontFamily, true);

                    double columnWidth;
                    double freeSpace;
                    double gap;

                    GetColumnMetrics(columnProperties, pageMarginWidth,
                                     pageFontSize, pageFontFamily, true, ccol, 
                                     ref lineHeight, out columnWidth, out freeSpace, out gap);

                    pageMarginAdjustment = freeSpace;
                }
            }

            return pageMarginAdjustment;
        }


        // ------------------------------------------------------------------
        // Calculate column count based on column properties.
        // If column width is Auto column count is calculated by assuming 
        // ColumnWidth as 20*FontSize
        // ------------------------------------------------------------------
        internal static int CalculateColumnCount(
            ColumnPropertiesGroup columnProperties, 
            double lineHeight, 
            double pageWidth, 
            double pageFontSize, 
            FontFamily pageFontFamily, 
            bool enableColumns)
        {
            int columns = 1;

            double gap;
            double rule = columnProperties.ColumnRuleWidth;

            if (enableColumns)
            {
                if (columnProperties.ColumnGapAuto)
                {
                    gap = 1 * lineHeight;
                }
                else
                {
                    gap = columnProperties.ColumnGap;
                }

                if (!columnProperties.ColumnWidthAuto)
                {
                    // Column count is ignored in this case
                    double column = columnProperties.ColumnWidth;
                    columns = (int)((pageWidth + gap) / (column + gap));
                }
                else
                {
                    // Column width is assumed to be 20*FontSize
                    double column = 20 * pageFontSize;
                    columns = (int)((pageWidth + gap) / (column + gap));
                }
            }
            return Math.Max(1, Math.Min(PTS.Restrictions.tscColumnRestriction-1, columns)); // at least 1 column is required
        }

        // ------------------------------------------------------------------
        // GetColumnMetrics
        // ------------------------------------------------------------------
        internal static void GetColumnMetrics(ColumnPropertiesGroup columnProperties, 
                                              double pageWidth, 
                                              double pageFontSize,
                                              FontFamily pageFontFamily,
                                              bool enableColumns,
                                              int cColumns,
                                              ref double lineHeight,
                                              out double columnWidth,
                                              out double freeSpace,
                                              out double gapSpace)
        {
            double rule = columnProperties.ColumnRuleWidth;
            if (!enableColumns)
            {
                Invariant.Assert(cColumns == 1);
                columnWidth = pageWidth;
                gapSpace   = 0;
                lineHeight = 0;
                freeSpace  = 0;
            }
            else
            {
                // For FlowDocument, calculate default column width
                if (columnProperties.ColumnWidthAuto)
                {
                    columnWidth = 20 * pageFontSize;
                }
                else
                {
                    columnWidth = columnProperties.ColumnWidth;
                }

                if (columnProperties.ColumnGapAuto)
                {
                    gapSpace = 1 * lineHeight;
                }
                else
                {
                    gapSpace = columnProperties.ColumnGap;
                }
            }
            columnWidth = Math.Max(1, Math.Min(columnWidth, pageWidth));
            freeSpace = pageWidth - (cColumns * columnWidth) - (cColumns - 1) * gapSpace;
            freeSpace = Math.Max(0, freeSpace);
        }

        // ------------------------------------------------------------------
        // Get columns info
        // ------------------------------------------------------------------
        internal static unsafe void GetColumnsInfo(
            ColumnPropertiesGroup columnProperties,
            double lineHeight,
            double pageWidth,
            double pageFontSize, 
            FontFamily pageFontFamily,
            int cColumns,
            PTS.FSCOLUMNINFO* pfscolinfo,
            bool enableColumns)
        {
            Debug.Assert(cColumns > 0, "At least one column is required.");

            double columnWidth;
            double freeSpace;
            double gap;

            double rule = columnProperties.ColumnRuleWidth;

            GetColumnMetrics(columnProperties, pageWidth,
                                  pageFontSize, pageFontFamily, enableColumns, cColumns, 
                                  ref lineHeight, out columnWidth, out freeSpace, out gap);

            // Set columns information
            if (!columnProperties.IsColumnWidthFlexible)
            {
                // All columns have the declared width
                // ColumnGap is flexible and is increased based on ColumnSpaceDistribution policy
                // (ColumnGap is effectively min)
                for (int i = 0; i < cColumns; i++)
                {
                    // Today there is no way to change the default value of ColumnSpaceDistribution.
                    // If column widths are not flexible, always allocate unused space on the right side.
                    pfscolinfo[i].durBefore = TextDpi.ToTextDpi((i == 0) ? 0 : gap);
                    pfscolinfo[i].durWidth = TextDpi.ToTextDpi(columnWidth);
                    // ColumnWidth has to be > 0 and SpaceBefore has to be >= 0
                    pfscolinfo[i].durBefore = Math.Max(0, pfscolinfo[i].durBefore);
                    pfscolinfo[i].durWidth = Math.Max(1, pfscolinfo[i].durWidth);
                }
            }
            else
            {
                //  ColumnGap is honored
                //  ColumnWidth is effectively min, and space is distributed according to ColumnSpaceDistribution policy
                for (int i = 0; i < cColumns; i++)
                {
                    if (columnProperties.ColumnSpaceDistribution == ColumnSpaceDistribution.Right)
                    {
                        pfscolinfo[i].durWidth = TextDpi.ToTextDpi((i == cColumns - 1) ? columnWidth + freeSpace : columnWidth);
                    }
                    else if (columnProperties.ColumnSpaceDistribution == ColumnSpaceDistribution.Left)
                    {
                        pfscolinfo[i].durWidth = TextDpi.ToTextDpi((i == 0) ? columnWidth + freeSpace : columnWidth);
                    }
                    else
                    {
                        pfscolinfo[i].durWidth = TextDpi.ToTextDpi(columnWidth + (freeSpace / cColumns));
                    }

                    // If calculated column width is greater than the page width, set it to page width to
                    // avoid clipping
                    if (pfscolinfo[i].durWidth > TextDpi.ToTextDpi(pageWidth))
                    {
                        pfscolinfo[i].durWidth = TextDpi.ToTextDpi(pageWidth);
                    }

                    pfscolinfo[i].durBefore = TextDpi.ToTextDpi((i == 0) ? 0 : gap);
                    // ColumnWidth has to be > 0 and SpaceBefore has to be >= 0
                    pfscolinfo[i].durBefore = Math.Max(0, pfscolinfo[i].durBefore);
                    pfscolinfo[i].durWidth = Math.Max(1, pfscolinfo[i].durWidth);
                }
            }
        }

        #endregion Misc Helpers
    }
}

