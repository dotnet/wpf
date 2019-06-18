// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: TextParagClient is responsible for handling display related
//              data of text paragraphs.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Documents;
using MS.Internal;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // TextParaClient is responsible for handling display related data of
    // text paragraphs.
    // ----------------------------------------------------------------------
    internal sealed class TextParaClient : BaseParaClient
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph associated with this object.
        // ------------------------------------------------------------------
        internal TextParaClient(TextParagraph paragraph) : base(paragraph)
        {
        }

        // ------------------------------------------------------------------
        //
        //  Internal Methods
        //
        // ------------------------------------------------------------------

        #region Internal Methods

        // ------------------------------------------------------------------
        // Validate visual node associated with paragraph.
        //
        //      fskupdInherited - inherited update info
        //      fswdir - inherited flow direction
        // ------------------------------------------------------------------
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Query paragraph details and render its content
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            VisualCollection visualChildren = _visual.Children;
            ContainerVisual lineContainerVisual = _visual;

            bool ignoreUpdateInfo = false;
            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (visualChildren.Count != 0 && !(visualChildren[0] is LineVisual))
                {
                    visualChildren.Clear();
                    ignoreUpdateInfo = true;
                }


                if(IsDeferredVisualCreationSupported(ref textDetails.u.full))
                {
                    // Transition to from no deferred visuals to deferred visuals -- Ignore update info
                    if(_lineIndexFirstVisual == -1 && lineContainerVisual.Children.Count > 0)
                    {
                        ignoreUpdateInfo = true;
                    }

                    SyncUpdateDeferredLineVisuals(lineContainerVisual.Children, ref textDetails.u.full, ignoreUpdateInfo);
                }
                else
                {
                    // Transition from deferred visuals to no deferred visuals -- Ignore update info
                    if(_lineIndexFirstVisual != -1)
                    {
                        _lineIndexFirstVisual = -1;
                        lineContainerVisual.Children.Clear();
                    }

                    // If we have no children, update isn't really possible.
                    if(lineContainerVisual.Children.Count == 0)
                    {
                        ignoreUpdateInfo = true;
                    }

                    // Add visuals for all lines.
                    if (textDetails.u.full.cLines > 0)
                    {
                        if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                        {
                            // (a) full with simple lines
                            RenderSimpleLines(lineContainerVisual, ref textDetails.u.full, ignoreUpdateInfo);
                        }
                        else
                        {
                            // (b) full with composite lines - when figures/floaters are present
                            RenderCompositeLines(lineContainerVisual, ref textDetails.u.full, ignoreUpdateInfo);
                        }
                    }
                    else
                    {
                        lineContainerVisual.Children.Clear();
                    }
                }


                // Add visuals for floaters and figures.
                if (textDetails.u.full.cAttachedObjects > 0)
                {
                    ValidateVisualFloatersAndFigures(fskupdInherited, textDetails.u.full.cAttachedObjects);
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            // Mirror lines around the page.
            if(ThisFlowDirection != PageFlowDirection)
            {
                PTS.FSRECT pageRect = _pageContext.PageRect;
                PtsHelper.UpdateMirroringTransform(PageFlowDirection, ThisFlowDirection, lineContainerVisual, TextDpi.FromTextDpi(2 * pageRect.u + pageRect.du));
            }
}

        // ------------------------------------------------------------------
        // Updates viewport
        // ------------------------------------------------------------------
        internal override void UpdateViewport(ref PTS.FSRECT viewport)
        {
            // Here's where the magic happens.
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));
            Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull, "Only 'full' text paragraph type is expected.");

            if (IsDeferredVisualCreationSupported(ref textDetails.u.full))
            {
                // Query paragraph details and render its content
                ContainerVisual lineContainerVisual = _visual;

                Debug.Assert(!((TextParagraph) Paragraph).HasFiguresFloatersOrInlineObjects());

                UpdateViewportSimpleLines(lineContainerVisual, ref textDetails.u.full, ref viewport);
            }

            int attachedObjectCount = textDetails.u.full.cAttachedObjects;

            // Recurse into figures and floaters
            if (attachedObjectCount > 0)
            {
                // Get list of attached objects
                PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc;
                PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, attachedObjectCount, out arrayAttachedObjectDesc);

                // Arrange attached objects
                for (int index = 0; index < arrayAttachedObjectDesc.Length; index++)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                    BaseParaClient paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);

                    paraClient.UpdateViewport(ref viewport);
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

            // Query paragraph details and hittest its content
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                PTS.FSPOINT localPoint = pt;

                // Mirror input point around page to hit test lines.
                if(ThisFlowDirection != PageFlowDirection)
                {
                    localPoint.u = _pageContext.PageRect.du - localPoint.u;
                }

                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        ie = InputHitTestSimpleLines(localPoint, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        ie = InputHitTestCompositeLines(localPoint, ref textDetails.u.full);
                    }
                }
                // Attached object hit tests are handled at page context level, as they're logically floating elements.
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            // If nothing is hit, return the owner of the paragraph.
            if (ie == null)
            {
                ie = Paragraph.Element as IInputElement;
            }

            return ie;
        }

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles for the given ContentElement
        // if it is found. Returns empty list otherwise.
        // start: int representing start position for e relative to base TextContainer.
        // length: int representing number of positions occupied by e.
        // ------------------------------------------------------------------
        internal override List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            List<Rect> rectangles = new List<Rect>();
            Debug.Assert(Paragraph.Element as ContentElement != e);

            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                // Check figures and floaters
                if (textDetails.u.full.cAttachedObjects > 0)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION[] arrayAttachedObjectDesc;
                    PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, textDetails.u.full.cAttachedObjects, out arrayAttachedObjectDesc);

                    for (int index = 0; index < arrayAttachedObjectDesc.Length; index++)
                    {
                        PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                        BaseParaClient paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                        PTS.ValidateHandle(paraClient);

                        if (start < paraClient.Paragraph.ParagraphEndCharacterPosition)
                        {
                            rectangles = paraClient.GetRectangles(e, start, length);
                            Invariant.Assert(rectangles != null);
                            if (rectangles.Count != 0)
                            {
                                break;
                            }
                        }
                    }
                }

                // If no success with figures and floaters, check in line
                if (rectangles.Count == 0 && textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        rectangles = GetRectanglesInSimpleLines(e, start, length, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with complex lines
                        rectangles = GetRectanglesInCompositeLines(e, start, length, ref textDetails.u.full);
                    }

                    // Ensure these are specified in page coordinates.
                    if(rectangles.Count > 0 && ThisFlowDirection != PageFlowDirection)
                    {
                        PTS.FSRECT pageRect = _pageContext.PageRect;

                        for(int index = 0; index < rectangles.Count; index++)
                        {
                            PTS.FSRECT rectTransform = new PTS.FSRECT(rectangles[index]);
                            PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectTransform, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectTransform));
                            rectangles[index] = rectTransform.FromTextDpi();
                        }
                    }
                }
}
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            Invariant.Assert(rectangles != null);
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Create paragraph result representing this paragraph.
        // ------------------------------------------------------------------
        internal override ParagraphResult CreateParagraphResult()
        {
            return new TextParagraphResult(this);
        }

        // ------------------------------------------------------------------
        // Returns a collection of LineResults for the paragraph.
        // ------------------------------------------------------------------
        internal ReadOnlyCollection<LineResult> GetLineResults()
        {
#if TEXTPANELLAYOUTDEBUG
            TextPanelDebug.IncrementCounter("TextPara.GetLines", TextPanelDebug.Category.TextView);
#endif
            ReadOnlyCollection<LineResult> lines = new ReadOnlyCollection<LineResult>(new List<LineResult>(0));

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        lines = LineResultsFromSimpleLines(ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        lines = LineResultsFromCompositeLines(ref textDetails.u.full);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            return lines;
        }

        // ------------------------------------------------------------------
        // Returns a collection of UIElements representing floated objects.
        // ------------------------------------------------------------------
        internal ReadOnlyCollection<ParagraphResult> GetFloaters()
        {
            List<ParagraphResult> floaters = null;

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // Floaters are only supported by full paragraphs
            if (   textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull
                && textDetails.u.full.cAttachedObjects > 0)
            {
                // Get list of floaters
                PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc;
                PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, textDetails.u.full.cAttachedObjects, out arrayAttachedObjectDesc);

                floaters = new List<ParagraphResult>(arrayAttachedObjectDesc.Length);

                // Create view results for floaters
                for (int index = 0; index < arrayAttachedObjectDesc.Length; index++)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                    BaseParaClient paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);

                    if(paraClient is FloaterParaClient)
                    {
                        floaters.Add(paraClient.CreateParagraphResult());
                    }
                }
            }
            return (floaters != null && floaters.Count > 0) ? new ReadOnlyCollection<ParagraphResult>(floaters) : null;
        }

        // ------------------------------------------------------------------
        // Returns a collection of UIElements representing positioned objects.
        // ------------------------------------------------------------------
        internal ReadOnlyCollection<ParagraphResult> GetFigures()
        {
            List<ParagraphResult> figures = null;

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // Floaters are only supported by full paragraphs
            if (   textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull
                && textDetails.u.full.cAttachedObjects > 0)
            {
                PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc;
                PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, textDetails.u.full.cAttachedObjects, out arrayAttachedObjectDesc);

                figures = new List<ParagraphResult>(arrayAttachedObjectDesc.Length);

                // Create view results for figures
                for (int index = 0; index < arrayAttachedObjectDesc.Length; index++)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                    BaseParaClient paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);

                    if(paraClient is FigureParaClient)
                    {
                        figures.Add(paraClient.CreateParagraphResult());
                    }
                }
            }
            return (figures != null && figures.Count > 0) ? new ReadOnlyCollection<ParagraphResult>(figures) : null;
        }

        // ------------------------------------------------------------------
        // Return TextContentRange for the content of the paragraph.
        // ------------------------------------------------------------------
        internal override TextContentRange GetTextContentRange()
        {
            PTS.FSTEXTDETAILS textDetails;
            int dcpFirst = 0, dcpLast = 0;

            // Query paragraph details
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull, "Only 'full' text paragraph type is expected.");

            dcpFirst = textDetails.u.full.dcpFirst;
            dcpLast = textDetails.u.full.dcpLim;

            // The last TextParaClient has EOP character included, which does not
            // exist in the tree. Need to remove it.
            // NOTE: cannot remove it when formatting line because PTS does not like empty lines.
            if (HasEOP && dcpLast > Paragraph.Cch)
            {
                ErrorHandler.Assert(dcpLast == Paragraph.Cch + Line.SyntheticCharacterLength, ErrorHandler.ParagraphCharacterCountMismatch);
                dcpLast -= Line.SyntheticCharacterLength;
            }

            // Text paragraph has always just one range
            int dcp = Paragraph.ParagraphStartCharacterPosition;
            TextContentRange textContentRange;

            if(TextParagraph.HasFiguresOrFloaters())
            {
                PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc = null;

                int attachedObjectCount = textDetails.u.full.cAttachedObjects;
                textContentRange = new TextContentRange();

                // Recurse into figures and floaters
                if (attachedObjectCount > 0)
                {
                    // Get list of attached objects
                    PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, attachedObjectCount, out arrayAttachedObjectDesc);
                }

                // Figures and floaters cannot break
                TextParagraph.UpdateTextContentRangeFromAttachedObjects(textContentRange, dcp + dcpFirst, dcp + dcpLast, arrayAttachedObjectDesc);
}
            else
            {
                textContentRange = new TextContentRange(dcp + dcpFirst, dcp + dcpLast, Paragraph.StructuralCache.TextContainer);
            }

            return textContentRange;
        }

        // ------------------------------------------------------------------
        // Retrieves detailed information about a line of text.
        //
        //     dcpLine - Index of the first character in the line.
        //     cchContent - Number of content characters in the line.
        //     cchEllipses - Number of content characters hidden by ellipses.
        // ------------------------------------------------------------------
        internal void GetLineDetails(int dcpLine, out int cchContent, out int cchEllipses)
        {
            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            int lineWidth = 0;
            bool firstLine = (dcpLine == 0);
            int dcpLim = 0;
            IntPtr breakRecLine = IntPtr.Zero;

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
                        PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        // Get lines information
                        int index;
                        for (index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];
                            if (dcpLine == lineDesc.dcpFirst)
                            {
                                lineWidth = lineDesc.dur;

                                // Store dcpLim to check that line lengths are in sync
                                dcpLim = lineDesc.dcpLim;

                                breakRecLine = lineDesc.pfsbreakreclineclient;

                                break;
                            }
                        }
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
                        PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        // Get lines information
                        int index;
                        for (index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                            if (lineDesc.cElements == 0) continue;

                            // Get list of line elements.
                            PTS.FSLINEELEMENT[] arrayLineElement;
                            PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                            int elIndex;
                            for (elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                            {
                                PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                                if (element.dcpFirst == dcpLine)
                                {
                                    lineWidth = element.dur;

                                    // Store dcpLim to check that line lengths are in sync
                                    dcpLim = element.dcpLim;

                                    breakRecLine = element.pfsbreakreclineclient;
                                    break;
                                }
                            }
                            if (elIndex < arrayLineElement.Length)
                            {
                                firstLine = (index == 0);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Invariant.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            // Recreate text line
            Line.FormattingContext ctx = new Line.FormattingContext(false, true, true, TextParagraph.TextRunCache);
            Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);

            if(IsOptimalParagraph)
            {
                ctx.LineFormatLengthTarget = dcpLim - dcpLine;
            }

            TextParagraph.FormatLineCore(line, breakRecLine, ctx, dcpLine, lineWidth, firstLine, dcpLine);

            // Assert that number of characters in Text line is the same as our expected length
            Invariant.Assert(line.SafeLength == dcpLim - dcpLine, "Line length is out of sync");

            cchContent = line.ContentLength;
            cchEllipses = line.GetEllipsesLength();

            line.Dispose();
        }

        // ------------------------------------------------------------------
        // Retrieves baseline information for first line of text
        // ------------------------------------------------------------------
        internal override int GetFirstTextLineBaseline()
        {
            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull, "Only 'full' text paragraph type is expected.");

            Rect rect = System.Windows.Rect.Empty;
            int vrBaseline = 0;

            if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
            {
                // (a) full with simple lines
                RectFromDcpSimpleLines(0, 0, LogicalDirection.Forward, TextPointerContext.Text, ref textDetails.u.full, ref rect, ref vrBaseline);
            }
            else
            {
                // (b) full with composite lines - when figures/floaters are present
                RectFromDcpCompositeLines(0, 0, LogicalDirection.Forward, TextPointerContext.Text, ref textDetails.u.full, ref rect, ref vrBaseline);
            }

            return vrBaseline;
        }


        // ------------------------------------------------------------------
        // Retrieves ITextPosition for specified character position.
        //
        //      dcp - Offset from the beginning of the text paragraph.
        // ------------------------------------------------------------------
        internal ITextPointer GetTextPosition(int dcp, LogicalDirection direction)
        {
            return TextContainerHelper.GetTextPointerFromCP(Paragraph.StructuralCache.TextContainer, dcp + Paragraph.ParagraphStartCharacterPosition, direction);
        }

        // ------------------------------------------------------------------
        // Retrieves bounds of an object/character at the specified ITextPointer.
        //
        //      position - Position of an object/character.
        //
        // Returns: Bounds of an object/character.
        // ------------------------------------------------------------------
        internal Rect GetRectangleFromTextPosition(ITextPointer position)
        {
            Rect rect = System.Windows.Rect.Empty;

            int cp = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition((TextPointer)position);
            int dcp = cp - Paragraph.ParagraphStartCharacterPosition;
            int originalDcp = dcp;
            if (position.LogicalDirection == LogicalDirection.Backward && dcp > 0)
            {
                --dcp;
            }

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    int vrBaseline = 0;

                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        RectFromDcpSimpleLines(dcp, originalDcp, position.LogicalDirection, position.GetPointerContext(position.LogicalDirection), ref textDetails.u.full, ref rect, ref vrBaseline);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        RectFromDcpCompositeLines(dcp, originalDcp, position.LogicalDirection, position.GetPointerContext(position.LogicalDirection), ref textDetails.u.full, ref rect, ref vrBaseline);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            // Mirror back to page flow direction
            if(ThisFlowDirection != PageFlowDirection)
            {
                PTS.FSRECT pageRect = _pageContext.PageRect;
                PTS.FSRECT rectTransform = new PTS.FSRECT(rect);
                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectTransform, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectTransform));
                rect = rectTransform.FromTextDpi();
            }

            return rect;
        }

        // ------------------------------------------------------------------
        // Returns tight bounding path geometry.
        // ------------------------------------------------------------------
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, double paragraphTopSpace, Rect visibleRect)
        {
            Geometry geometry = null;
            Geometry floatAndFigGeometry = null;

            int cpStartTextPointer = startPosition.Offset;
            int cpParagraphStart = Paragraph.ParagraphStartCharacterPosition;
            int dcpStart = Math.Max(cpStartTextPointer, cpParagraphStart) - cpParagraphStart;

            int cpEndTextPointer = endPosition.Offset;
            int cpParagraphEnd = Paragraph.ParagraphEndCharacterPosition;
            int dcpEnd = Math.Min(cpEndTextPointer, cpParagraphEnd) - cpParagraphStart;

            //  apply first line top space only if selection starts before or exactly at this paragraph
            double firstLineTopSpace = (cpStartTextPointer < cpParagraphStart) ? paragraphTopSpace : 0.0;

            //  handle end-of-para only if the range extends beyond this paragraph
            bool handleEndOfPara = cpEndTextPointer > cpParagraphEnd;

            //  mirror transform - needed if flow direction changes
            Transform transform = null;

            if (ThisFlowDirection != PageFlowDirection)
            {
                transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, TextDpi.FromTextDpi(2 * _pageContext.PageRect.u + _pageContext.PageRect.du), 0.0);

                //  (and while we are at it) visibleRect should be mirrored too
                visibleRect = transform.TransformBounds(visibleRect);
            }

            //  query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaCache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        geometry = PathGeometryFromDcpRangeSimpleLines(dcpStart, dcpEnd, firstLineTopSpace, handleEndOfPara, ref textDetails.u.full, visibleRect);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        geometry = PathGeometryFromDcpRangeCompositeLines(dcpStart, dcpEnd, firstLineTopSpace, handleEndOfPara, ref textDetails.u.full, visibleRect);
                    }
                }
                //  build highlight for floaters and figures in this paragraph
                if (textDetails.u.full.cAttachedObjects > 0)
                {
                    floatAndFigGeometry = PathGeometryFromDcpRangeFloatersAndFigures(cpStartTextPointer, cpEndTextPointer, ref textDetails.u.full);
                }
            }
            else
            {
                // (c) cached - when using ParaCache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            //  at this point geometry contains only the text content related geometry
            if (geometry != null && transform != null)
            {
                //  mirror back to page flow direction
                CaretElement.AddTransformToGeometry(geometry, transform);
            }

            //  rectangles from which floatAndFigGeometry is calculated are already mirrored.
            //  this is why geometry and floatAndFigGeometry are combined after geometry is mirrored above
            if (floatAndFigGeometry != null)
            {
                CaretElement.AddGeometry(ref geometry, floatAndFigGeometry);
            }

            return (geometry);
        }

        // ------------------------------------------------------------------
        // Returns true if caret is at unit boundary
        //
        //      position - Position of an object/character.
        //
        // ------------------------------------------------------------------
        internal bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            bool isAtCaretUnitBoundary = false;

            // Get position offset in paragraph
            Debug.Assert(position is TextPointer);
            int cp = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition(position as TextPointer);
            int dcp = cp - Paragraph.ParagraphStartCharacterPosition;

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        isAtCaretUnitBoundary = IsAtCaretUnitBoundaryFromDcpSimpleLines(dcp, position, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        isAtCaretUnitBoundary = IsAtCaretUnitBoundaryFromDcpCompositeLines(dcp, position, ref textDetails.u.full);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            return isAtCaretUnitBoundary;
        }

        // ------------------------------------------------------------------
        // Returns next caret unit position
        //
        //      position - Position of an object/character.
        //      direction - Logical direction in which we seek the position
        //
        // ------------------------------------------------------------------
        internal ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            ITextPointer nextCaretPosition = position;

            // Get position offset in paragraph
            Debug.Assert(position is TextPointer);
            int cp = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition(position as TextPointer);
            int dcp = cp - Paragraph.ParagraphStartCharacterPosition;

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        nextCaretPosition = NextCaretUnitPositionFromDcpSimpleLines(dcp, position, direction, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        nextCaretPosition = NextCaretUnitPositionFromDcpCompositeLines(dcp, position, direction, ref textDetails.u.full);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            return nextCaretPosition;
        }

        internal ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            ITextPointer backspaceCaretPosition = position;

            // Get position offset in paragraph
            Invariant.Assert(position is TextPointer);
            int cp = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition(position as TextPointer);
            int dcp = cp - Paragraph.ParagraphStartCharacterPosition;

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        backspaceCaretPosition = BackspaceCaretUnitPositionFromDcpSimpleLines(dcp, position, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        backspaceCaretPosition = BackspaceCaretUnitPositionFromDcpCompositeLines(dcp, position, ref textDetails.u.full);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Invariant.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            return backspaceCaretPosition;
        }

        // ------------------------------------------------------------------
        // Retrieves a text position given the distance from the beginning
        // of the line.
        //
        //      dcpLine - Character offset identifying the line. This is
        //          the first character position of the line.
        //      distance - Distance from the beginning of the line.
        //
        // Returns: Text position.
        // ------------------------------------------------------------------
        internal ITextPointer GetTextPositionFromDistance(int dcpLine, double distance)
        {
            // Query paragraph details
            int urDistance = TextDpi.ToTextDpi(distance);
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            if(ThisFlowDirection != PageFlowDirection)
            {
                urDistance = _pageContext.PageRect.du - urDistance;
            }

            int lineWidth = 0;
            bool firstLine = (dcpLine == 0);
            int dcpLim = 0;
            IntPtr breakRecLine = IntPtr.Zero;

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
                        PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        // Get lines information
                        int index;
                        for (index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];
                            if (dcpLine == lineDesc.dcpFirst)
                            {
                                lineWidth = lineDesc.dur;
                                urDistance -= lineDesc.urStart;

                                // Store dcpLim to check if line lengths are in sync
                                dcpLim = lineDesc.dcpLim;

                                breakRecLine = lineDesc.pfsbreakreclineclient;

                                break;
                            }
                        }
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
                        PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        // Get lines information
                        int index;
                        for (index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                            if (lineDesc.cElements == 0) continue;

                            // Get list of line elements.
                            PTS.FSLINEELEMENT [] arrayLineElement;
                            PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                            int elIndex;
                            for (elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                            {
                                PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                                if (element.dcpFirst == dcpLine)
                                {
                                    lineWidth = element.dur;
                                    urDistance -= element.urStart;

                                    // Store dcpLim to check if line lengths are in sync
                                    dcpLim = element.dcpLim;

                                    breakRecLine = element.pfsbreakreclineclient;

                                    break;
                                }
                            }
                            if (elIndex < arrayLineElement.Length)
                            {
                                firstLine = (index == 0);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }

            // Recreate text line
            Line.FormattingContext ctx = new Line.FormattingContext(false, true, true, TextParagraph.TextRunCache);
            Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);

            if(IsOptimalParagraph)
            {
                ctx.LineFormatLengthTarget = dcpLim - dcpLine;
            }

            TextParagraph.FormatLineCore(line, breakRecLine, ctx, dcpLine, lineWidth, firstLine, dcpLine);

            // Assert that number of characters in Text line is the same as our expected length
            Invariant.Assert(line.SafeLength == dcpLim - dcpLine, "Line length is out of sync");

            CharacterHit charHit = line.GetTextPositionFromDistance(urDistance);
            int cpPosition = charHit.FirstCharacterIndex + charHit.TrailingLength;
            int dcpLastAttachedObject = TextParagraph.GetLastDcpAttachedObjectBeforeLine(dcpLine);
            if(cpPosition < dcpLastAttachedObject)
            {
                cpPosition = dcpLastAttachedObject;
            }

            StaticTextPointer pos = TextContainerHelper.GetStaticTextPointerFromCP(Paragraph.StructuralCache.TextContainer, cpPosition + Paragraph.ParagraphStartCharacterPosition);
            LogicalDirection logicalDirection = (charHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            line.Dispose();

            return pos.CreateDynamicTextPointer(logicalDirection);
        }

        // ------------------------------------------------------------------
        // Retrieves collection of GlyphRuns from a range of text.
        //
        //      glyphRuns - preallocated collection of GlyphRuns. May already
        //          contain runs and new runs need to be appended.
        //      start - the beginning of the range
        //      end - the end of the range
        // ------------------------------------------------------------------
        internal void GetGlyphRuns(List<GlyphRun> glyphRuns, ITextPointer start, ITextPointer end)
        {
            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                int dcpStart = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition((TextPointer)start) - Paragraph.ParagraphStartCharacterPosition;
                int dcpEnd = Paragraph.StructuralCache.TextContainer.Start.GetOffsetToPosition((TextPointer)end) - Paragraph.ParagraphStartCharacterPosition;
                Invariant.Assert(dcpStart >= textDetails.u.full.dcpFirst && dcpEnd <= textDetails.u.full.dcpLim);

                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        GetGlyphRunsFromSimpleLines(glyphRuns, dcpStart, dcpEnd, ref textDetails.u.full);
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        GetGlyphRunsFromCompositeLines(glyphRuns, dcpStart, dcpEnd, ref textDetails.u.full);
                    }
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Invariant.Assert(false, "Should not get here. ParaCache is not currently used.");
            }
        }

        #endregion Internal Methods

        // ------------------------------------------------------------------
        //
        //  Internal Properties
        //
        // ------------------------------------------------------------------

        #region Internal Properties

        // ------------------------------------------------------------------
        // Paragraph associated with this ParaClient.
        // ------------------------------------------------------------------
        internal TextParagraph TextParagraph { get { return (TextParagraph)_paragraph; } }

        // ------------------------------------------------------------------
        // Has EOP character? Is it the last ParaClient of TextParagraph?
        // ------------------------------------------------------------------
        internal bool HasEOP
        {
            get { return IsLastChunk; }
        }

        // ------------------------------------------------------------------
        // Is this the first chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsFirstChunk
        {
            get
            {
                // Query paragraph details
                PTS.FSTEXTDETAILS textDetails;
                PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));
                Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull, "Only 'full' text paragraph type is expected.");
                // The first chunk always starts with dcpFirst == 0.
                return (textDetails.u.full.cLines > 0 && textDetails.u.full.dcpFirst == 0);
            }
        }

        // ------------------------------------------------------------------
        // Is this the last chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsLastChunk
        {
            get
            {
                bool lastChunk = false;

                // Query paragraph details
                PTS.FSTEXTDETAILS textDetails;
                PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));
                Invariant.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull, "Only 'full' text paragraph type is expected.");

                if (textDetails.u.full.cLines > 0)
                {
                    if (Paragraph.Cch > 0)
                    {
                        lastChunk = (textDetails.u.full.dcpLim >= Paragraph.Cch);
                    }
                    else
                    {
                        lastChunk = (textDetails.u.full.dcpLim == Line.SyntheticCharacterLength);
                        // Is there a possibility to have para with just one character
                        //       and it is not the last chunk?
                        //       If yes, this logic needs to be changed.
                    }
                }
                return lastChunk;
            }
        }

        #endregion Internal Properties

        // ------------------------------------------------------------------
        //
        //  Protected Methods
        //
        // ------------------------------------------------------------------

        #region Protected Methods

        // ------------------------------------------------------------------
        // Arrange paragraph.
        // ------------------------------------------------------------------
        protected override void OnArrange()
        {
            base.OnArrange();

            // Optimization - Don't arrange if we have no figures, floaters, inline objects
            if(!TextParagraph.HasFiguresFloatersOrInlineObjects())
            {
                return;
            }

            // Query paragraph details
            PTS.FSTEXTDETAILS textDetails;
            PTS.Validate(PTS.FsQueryTextDetails(PtsContext.Context, _paraHandle.Value, out textDetails));

            // There are 3 different types of text paragraphs:
            // (a) full with simple lines
            // (b) full with composite lines - when figures/floaters are present
            // (c) cached - when using ParaChache
            if (textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdFull)
            {
                // (a) full with simple lines
                // (b) full with composite lines - when figures/floaters are present
                if (textDetails.u.full.cLines > 0)
                {
                    if (!PTS.ToBoolean(textDetails.u.full.fLinesComposite))
                    {
                        // (a) full with simple lines
                        PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
                        PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        for (int index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                            // Enumerate all inline objects and reformat them.
                            List<InlineObject> inlineObjects = TextParagraph.InlineObjectsFromRange(lineDesc.dcpFirst, lineDesc.dcpLim);
                            if (inlineObjects != null)
                            {
                                for (int i = 0; i < inlineObjects.Count; i++)
                                {
                                    UIElement uiElement = (UIElement)inlineObjects[i].Element;

                                    if(uiElement.IsMeasureValid && !uiElement.IsArrangeValid)
                                    {
                                        uiElement.Arrange(new Rect(uiElement.DesiredSize));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // (b) full with composite lines - when figures/floaters are present
                        PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
                        PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails.u.full, out arrayLineDesc);

                        for (int index = 0; index < arrayLineDesc.Length; index++)
                        {
                            PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];

                            // Get list of line elements
                            PTS.FSLINEELEMENT[] arrayLineElement;
                            PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                            for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                            {
                                PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                                // Enumerate all inline objects and reformat them.
                                List<InlineObject> inlineObjects = TextParagraph.InlineObjectsFromRange(element.dcpFirst, element.dcpLim);
                                if (inlineObjects != null)
                                {
                                    for (int i = 0; i < inlineObjects.Count; i++)
                                    {
                                        UIElement uiElement = (UIElement)inlineObjects[i].Element;
                                        if(uiElement.IsMeasureValid && !uiElement.IsArrangeValid)
                                        {
                                            uiElement.Arrange(new Rect(uiElement.DesiredSize));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (textDetails.u.full.cAttachedObjects > 0)
                {
                    // Get list of floaters
                    PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc;
                    PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, textDetails.u.full.cAttachedObjects, out arrayAttachedObjectDesc);

                    // Arrange floaters

                    for (int index = 0; index < arrayAttachedObjectDesc.Length; index++)
                    {
                        PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                        BaseParaClient paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                        PTS.ValidateHandle(paraClient);

                        if(paraClient is FloaterParaClient)
                        {
                            PTS.FSFLOATERDETAILS floaterDetails;
                            PTS.Validate(PTS.FsQueryFloaterDetails(PtsContext.Context, attachedObjectDesc.pfspara, out floaterDetails));
                            PTS.FSRECT rectFloater = floaterDetails.fsrcFloater;

                            if(ThisFlowDirection != PageFlowDirection)
                            {
                                PTS.FSRECT pageRect = _pageContext.PageRect;
                                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectFloater, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectFloater));
                            }

                            ((FloaterParaClient)paraClient).ArrangeFloater(rectFloater, _rect, PTS.FlowDirectionToFswdir(ThisFlowDirection), _pageContext);
                        }
                        else if(paraClient is FigureParaClient)
                        {
                            PTS.FSFIGUREDETAILS figureDetails;
                            PTS.Validate(PTS.FsQueryFigureObjectDetails(PtsContext.Context, attachedObjectDesc.pfspara, out figureDetails));
                            PTS.FSRECT rectFigure = figureDetails.fsrcFlowAround;

                            if(ThisFlowDirection != PageFlowDirection)
                            {
                                PTS.FSRECT pageRect = _pageContext.PageRect;
                                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectFigure, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectFigure));
                            }

                            ((FigureParaClient)paraClient).ArrangeFigure(rectFigure, _rect, PTS.FlowDirectionToFswdir(ThisFlowDirection), _pageContext);
                        }
                        else
                        {
                            Invariant.Assert(false, "Attached object not figure or floater.");
                        }
}
                }
            }
            else
            {
                // (c) cached - when using ParaChache
                Debug.Assert(textDetails.fsktd == PTS.FSKTEXTDETAILS.fsktdCached);
                Debug.Assert(false, "Should not get here. ParaCache is not currently used.");
            }
        }

        #endregion Protected Methods

        // ------------------------------------------------------------------
        //
        //  Private Methods
        //
        // ------------------------------------------------------------------

        #region Private Methods

        // ------------------------------------------------------------------
        // Syncs a deferred line visuals list (and update information) with existing visuals
        // ------------------------------------------------------------------
        private void SyncUpdateDeferredLineVisuals(VisualCollection lineVisuals, ref PTS.FSTEXTDETAILSFULL textDetails, bool ignoreUpdateInfo)
        {
            Debug.Assert(!PTS.ToBoolean(textDetails.fLinesComposite));

            try
            {
                if (!PTS.ToBoolean(textDetails.fUpdateInfoForLinesPresent) || ignoreUpdateInfo ||
                    textDetails.cLines == 0)
                {
                    // _lineIndexFirstVisual will be updated based on the size of this list, so clearing is sufficient here.
                    lineVisuals.Clear();
                }
                else if (_lineIndexFirstVisual != -1)
                {
                    PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
                    PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

                    int lineIndexToBeginRemoval = textDetails.cLinesBeforeChange;
                    int cLinesToRemove = textDetails.cLinesChanged - textDetails.dcLinesChanged;
                    int insertionIndex = -1;

                    // Shift lines before change
                    if(textDetails.dvrShiftBeforeChange != 0)
                    {
                        int countVisualsShiftBeforeChange = Math.Min(Math.Max(lineIndexToBeginRemoval - _lineIndexFirstVisual, 0), lineVisuals.Count);
                        for(int index = 0; index < countVisualsShiftBeforeChange; index++)
                        {
                            // Shift line's visual
                            ContainerVisual lineVisual = (ContainerVisual) lineVisuals[index];
                            Vector offset = lineVisual.Offset;
                            offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftBeforeChange);
                            lineVisual.Offset = offset;
                        }
                    }

                    // If the line index to begin removal is before our first visual, then the overlap will look like
                    //      |---------------|  (Committed visual range)
                    // |------|                (Range to remove)
                    if (lineIndexToBeginRemoval < _lineIndexFirstVisual)
                    {
                        // Determine the amount of overlap, and remove.
                        int actualLinesToRemove = Math.Min(Math.Max(lineIndexToBeginRemoval - _lineIndexFirstVisual + cLinesToRemove, 0), lineVisuals.Count);

                        if (actualLinesToRemove > 0)
                        {
                            lineVisuals.RemoveRange(0, actualLinesToRemove);
                        }

                        if (lineVisuals.Count == 0)
                        {
                            lineVisuals.Clear();
                            _lineIndexFirstVisual = -1;
                        }
                        else
                        {
                            insertionIndex = 0;
                            _lineIndexFirstVisual = lineIndexToBeginRemoval;
                        }
                    }
                    else if (lineIndexToBeginRemoval < _lineIndexFirstVisual + lineVisuals.Count)
                    {
                        // Else case for overlap
                        //  |---------------|  (Committed visual range)
                        //       |-----|                (Range to remove)
                        // Or
                        //  |---------------|
                        //           |--------------|

                        // Removing from the middle
                        int actualLinesToRemove = Math.Min(cLinesToRemove, lineVisuals.Count - (lineIndexToBeginRemoval - _lineIndexFirstVisual));

                        lineVisuals.RemoveRange(lineIndexToBeginRemoval - _lineIndexFirstVisual, actualLinesToRemove);

                        insertionIndex = lineIndexToBeginRemoval - _lineIndexFirstVisual; // Insertion index is relative to committed visual range
                    }

                    int shiftIndex = -1;

                    if (insertionIndex != -1)
                    {
                        // Add new lines
                        // Insertion must occur at some point along our committed visual range
                        Debug.Assert(insertionIndex >= 0 && insertionIndex <= lineVisuals.Count);

                        for (int index = textDetails.cLinesBeforeChange; index < textDetails.cLinesBeforeChange + textDetails.cLinesChanged; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                            ContainerVisual lineVisual = CreateLineVisual(ref arrayLineDesc[index], Paragraph.ParagraphStartCharacterPosition);

                            lineVisuals.Insert(insertionIndex + (index - textDetails.cLinesBeforeChange), lineVisual);
                            lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));
                        }

                        shiftIndex = insertionIndex + textDetails.cLinesChanged;
                    }

                    // Any committed visuals after our inserted section must be shifted
                    if (shiftIndex != -1)
                    {
                        // Shift remaining lines
                        for (int index = shiftIndex; index < lineVisuals.Count; index++)
                        {
                            // Shift line's visual
                            ContainerVisual lineVisual = (ContainerVisual) lineVisuals[index];
                            Vector offset = lineVisual.Offset;
                            offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftAfterChange);
                            lineVisual.Offset = offset;
                        }
                    }
                }
            }

            finally
            {
                // If no visuals, committed range is nonexistant, so -1
                if (lineVisuals.Count == 0)
                {
                    _lineIndexFirstVisual = -1;
                }
            }

#if VERIFY_VISUALS
            // Verify our visuals are in-sync with the actual line visuals.
            VerifyVisuals(ref textDetails);
#endif
        }

        // ------------------------------------------------------------------
        // Retrieve lines from simple lines.
        // ------------------------------------------------------------------
        private ReadOnlyCollection<LineResult> LineResultsFromSimpleLines(ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            // Get list of complex lines.
            PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            List<LineResult> lines = new List<LineResult>(arrayLineDesc.Length);

            // Get lines information
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                Rect lbox = new Rect(
                    TextDpi.FromTextDpi(lineDesc.urBBox), TextDpi.FromTextDpi(lineDesc.vrStart),
                    TextDpi.FromTextDpi(lineDesc.durBBox), TextDpi.FromTextDpi(lineDesc.dvrAscent + lineDesc.dvrDescent));

                // Mirror layout box to page flow direction
                if(PageFlowDirection != ThisFlowDirection)
                {
                    PTS.FSRECT pageRect = _pageContext.PageRect;
                    PTS.FSRECT rectTransform = new PTS.FSRECT(lbox);
                    PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectTransform, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectTransform));
                    lbox = rectTransform.FromTextDpi();
                }

                lines.Add(new TextParaLineResult(this, lineDesc.dcpFirst, lineDesc.dcpLim - lineDesc.dcpFirst,
                    lbox, TextDpi.FromTextDpi(lineDesc.dvrAscent)));
            }

            if (lines.Count != 0)
            {
                // Hide EOP character
                TextParaLineResult lastLineResult = (TextParaLineResult)lines[lines.Count - 1];
                if (HasEOP && lastLineResult.DcpLast > Paragraph.Cch)
                {
                    ErrorHandler.Assert(lastLineResult.DcpLast - Line.SyntheticCharacterLength == Paragraph.Cch, ErrorHandler.ParagraphCharacterCountMismatch);
                    lastLineResult.DcpLast -= Line.SyntheticCharacterLength;
                }
            }

            return (lines.Count > 0) ? new ReadOnlyCollection<LineResult>(lines) : null;
        }

        // ------------------------------------------------------------------
        // Retrieve lines from composite lines.
        // ------------------------------------------------------------------
        private ReadOnlyCollection<LineResult> LineResultsFromCompositeLines(ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            // Get list of complex composite lines.
            PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            List<LineResult> lines = new List<LineResult>(arrayLineDesc.Length);

            // Get lines information
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) { continue; }

                // Get list of line elements.
                PTS.FSLINEELEMENT [] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // Create line info
                    Rect lbox = new Rect(TextDpi.FromTextDpi(element.urBBox), TextDpi.FromTextDpi(lineDesc.vrStart),
                                         TextDpi.FromTextDpi(element.durBBox), TextDpi.FromTextDpi(element.dvrAscent + element.dvrDescent));

                    // Mirror layout box to page flow direction
                    if(ThisFlowDirection != PageFlowDirection)
                    {
                        PTS.FSRECT pageRect = _pageContext.PageRect;
                        PTS.FSRECT rectTransform = new PTS.FSRECT(lbox);
                        PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ThisFlowDirection), ref pageRect, ref rectTransform, PTS.FlowDirectionToFswdir(PageFlowDirection), out rectTransform));
                        lbox = rectTransform.FromTextDpi();
                    }

                    lines.Add(new TextParaLineResult(this, element.dcpFirst, element.dcpLim - element.dcpFirst,
                        lbox, TextDpi.FromTextDpi(element.dvrAscent)));
                }
            }

            if (lines.Count != 0)
            {
                // Hide EOP character
                TextParaLineResult lastLineResult = (TextParaLineResult)lines[lines.Count - 1];
                if (HasEOP && lastLineResult.DcpLast > Paragraph.Cch)
                {
                    ErrorHandler.Assert(lastLineResult.DcpLast - Line.SyntheticCharacterLength == Paragraph.Cch, ErrorHandler.ParagraphCharacterCountMismatch);
                    lastLineResult.DcpLast -= Line.SyntheticCharacterLength;
                }
            }

            return (lines.Count > 0) ? new ReadOnlyCollection<LineResult>(lines) : null;
        }

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text position.
        // ------------------------------------------------------------------
        private void RectFromDcpSimpleLines(
            int dcp,
            int originalDcp,
            LogicalDirection orientation,
            TextPointerContext context,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            ref Rect rect,
            ref int vrBaseline)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                // 'dcp' needs to be within line range. If position points to dcpLim,
                // it means that the next line starts from such position, hence go to the next line.
                // But if this is the last line (EOP character), get rectangle form the last
                // character of the line.
                if (   ((lineDesc.dcpFirst <= dcp) && (lineDesc.dcpLim > dcp))
                    || ((lineDesc.dcpLim == dcp) && (index == arrayLineDesc.Length - 1)))
                {
                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Get rect from cp
                    FlowDirection flowDirection;
                    rect = line.GetBoundsFromTextPosition(dcp, out flowDirection);
                    rect.X += TextDpi.FromTextDpi(lineDesc.urStart);
                    rect.Y += TextDpi.FromTextDpi(lineDesc.vrStart);

                    // Return only TopLeft and Height.
                    // Adjust rect.Left by taking into account flow direction of the
                    // content and orientation of input position.
                    if (ThisFlowDirection != flowDirection)
                    {
                        if (orientation == LogicalDirection.Forward)
                        {
                            rect.X = rect.Right;
                        }
                    }
                    else
                    {
                        // NOTE: check for 'originalCharacterIndex > 0' is only required for position at the beginning
                        //       content with Backward orientation. This should not be a valid position.
                        //       Remove it later
                        // We also need to check here if the context is an inline element, such as a hidden run. In such a
                        // case we will have the rect of the character immediately following the hidden run, which is the same as
                        // originalDcp. If we take the right bounds of the rect case we will be off by one character.
                        if (orientation == LogicalDirection.Backward && originalDcp > 0 && (context == TextPointerContext.Text || context == TextPointerContext.EmbeddedElement))
                        {
                            rect.X = rect.Right;
                        }
                    }
                    rect.Width = 0;

                    vrBaseline = line.Baseline + lineDesc.vrStart;

                    // Dispose the line
                    line.Dispose();
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text position.
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        private void RectFromDcpCompositeLines(
            int dcp,
            int originalDcp,
            LogicalDirection orientation,
            TextPointerContext context,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            ref Rect rect,
            ref int vrBaseline)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) { continue; }

                // Get list of line elements.
                PTS.FSLINEELEMENT [] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // 'dcp' needs to be within line range. If position points to dcpLim,
                    // it means that the next line starts from such position, hence go to the next line.
                    // But if this is the last line (EOP character), get rectangle form the last
                    // character of the line.
                    if (   ((element.dcpFirst <= dcp) && (element.dcpLim > dcp))
                        || ((element.dcpLim == dcp) && (elIndex == arrayLineElement.Length - 1) && (index == arrayLineDesc.Length - 1)))
                    {
                        // Create and format line
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Get rect from cp
                        FlowDirection flowDirection;
                        rect = line.GetBoundsFromTextPosition(dcp, out flowDirection);
                        rect.X += TextDpi.FromTextDpi(element.urStart);
                        rect.Y += TextDpi.FromTextDpi(lineDesc.vrStart);

                        // Return only TopLeft and Height.
                        // Adjust rect.Left by taking into account flow direction of the
                        // content and orientation of input position.
                        if (ThisFlowDirection != flowDirection)
                        {
                            if (orientation == LogicalDirection.Forward)
                            {
                                rect.X = rect.Right;
                            }
                        }
                        else
                        {
                            // NOTE: check for 'originalCharacterIndex > 0' is only required for position at the beginning
                            //       content with Backward orientation. This should not be a valid position.
                            //       Remove it later
                            if (orientation == LogicalDirection.Backward && originalDcp > 0 && (context == TextPointerContext.Text || context == TextPointerContext.EmbeddedElement))
                            {
                                rect.X = rect.Right;
                            }
                        }
                        rect.Width = 0;

                        vrBaseline = line.Baseline + lineDesc.vrStart;

                        // Dispose the line
                        line.Dispose();
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Returns tight bounding path geometry for simple lines
        // ------------------------------------------------------------------
        private Geometry PathGeometryFromDcpRangeSimpleLines(
            int dcpStart,
            int dcpEnd,
            double paragraphTopSpace,
            bool handleEndOfPara,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            Rect visibleRect)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            Geometry geometry = null;

            //  get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            int lineStart = 0;
            int lineCount = arrayLineDesc.Length;

            if(_lineIndexFirstVisual != -1)
            {
                lineStart = _lineIndexFirstVisual;
                lineCount = _visual.Children.Count;
            }

            for (int lineIndex = lineStart; lineIndex < (lineStart + lineCount); ++lineIndex)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[lineIndex];

                if (handleEndOfPara)
                {
                    //  Note (end-of-para workaround): '<' gives the chance
                    //  for the EOP handling code below to emulate EOP glyph
                    if (dcpEnd < lineDesc.dcpFirst)
                    {
                        //  this line starts after the range's end.
                        //  safe to break from the loop.
                        break;
                    }
                }
                else
                {
                    if (dcpEnd <= lineDesc.dcpFirst)
                    {
                        //  this line starts after the range's end.
                        //  safe to break from the loop.
                        break;
                    }
                }

                //  'dcp' needs to be within line range. If position points to dcpLim,
                //  it means that the next line starts from such position, hence go to the next line.
                //  But if this is the last line (EOP character), get geometry form the last
                //  character of the line.
                if (    lineDesc.dcpLim > dcpStart
                    ||  (   (lineIndex == arrayLineDesc.Length - 1)
                        &&  (lineDesc.dcpLim == dcpStart)   )
                   )
                {
                    int dcpRangeStartForThisLine = Math.Max(lineDesc.dcpFirst, dcpStart);
                    //  Note (end-of-para workaround): dcp can be '0' due to end-of-para
                    //  not included into cp count - but it is there!!!
                    int cchRangeForThisLine = Math.Max(Math.Min(lineDesc.dcpLim, dcpEnd) - dcpRangeStartForThisLine, 1);
                    double lineTopSpace = (lineIndex == 0) ? paragraphTopSpace : 0.0;
                    double endOfParaGlyphWidth;

                    if (    (handleEndOfPara && lineIndex == (arrayLineDesc.Length - 1))
                        ||  (dcpEnd >= lineDesc.dcpLim && HasAnyLineBreakAtCp(lineDesc.dcpLim)) )
                    {
                        endOfParaGlyphWidth = ((double)TextParagraph.Element.GetValue(TextElement.FontSizeProperty) * CaretElement.c_endOfParaMagicMultiplier);
                    }
                    else
                    {
                        endOfParaGlyphWidth = 0;
                    }

                    //  get rectangles for this line.
                    IList<Rect> rectangles = RectanglesFromDcpRangeOfSimpleLine(
                            dcpRangeStartForThisLine,
                            cchRangeForThisLine,
                            lineTopSpace,
                            endOfParaGlyphWidth,
                            ref lineDesc,
                            lineIndex,
                            visibleRect
                            );

                    if (rectangles != null)
                    {
                        for (int i = 0, count = rectangles.Count; i < count; ++i)
                        {
                            RectangleGeometry rectGeometry = new RectangleGeometry(rectangles[i]);
                            CaretElement.AddGeometry(ref geometry, rectGeometry);
                        }
                    }
                }
            }

            return geometry;
        }

        // ------------------------------------------------------------------
        // Returns tight bounding path geometry for composite lines
        // ------------------------------------------------------------------
        private Geometry PathGeometryFromDcpRangeCompositeLines(
            int dcpStart,
            int dcpEnd,
            double paragraphTopSpace,
            bool handleEndOfPara,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            Rect visibleRect)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            Geometry geometry = null;

            //  get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            for (int lineIndex = 0; lineIndex < arrayLineDesc.Length; ++lineIndex)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[lineIndex];
                if (lineDesc.cElements == 0)
                {
                    continue;
                }

                //  get list of line elements.
                PTS.FSLINEELEMENT[] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elemIndex = 0; elemIndex < arrayLineElement.Length; ++elemIndex)
                {
                    PTS.FSLINEELEMENT elemDesc = arrayLineElement[elemIndex];

                    if (handleEndOfPara)
                    {
                        //  Note (end-of-para workaround): '<' gives the chance
                        //  for the EOP handling code below to emulate EOP glyph
                        if (dcpEnd < elemDesc.dcpFirst)
                        {
                            //  this line starts after the range's end.
                            //  safe to break from the loop.
                            break;
                        }
                    }
                    else
                    {
                        if (dcpEnd <= elemDesc.dcpFirst)
                        {
                            //  this line starts after the range's end.
                            //  safe to break from the loop.
                            break;
                        }
                    }

                    //  'dcp' needs to be within line range. If position points to dcpLim,
                    //  it means that the next line starts from such position, hence go to the next line.
                    //  But if this is the last line (EOP character), get geometry form the last
                    //  character of the line.
                    if (    elemDesc.dcpLim > dcpStart
                        ||  (   (elemDesc.dcpLim == dcpStart)
                            &&  (elemIndex == arrayLineElement.Length - 1)
                            &&  (lineIndex == arrayLineDesc.Length - 1) )   )
                    {
                        int dcpRangeStartForThisElem = Math.Max(elemDesc.dcpFirst, dcpStart);
                        //  Note (end-of-para workaround): dcp can be '0' due to end-of-para
                        //  not included into cp count - but it is there!!!
                        int cchRangeForThisElem = Math.Max(Math.Min(elemDesc.dcpLim, dcpEnd) - dcpRangeStartForThisElem, 1);
                        double lineTopSpace = (lineIndex == 0) ? paragraphTopSpace : 0.0;
                        double endOfParaGlyphWidth;

                        if (    (handleEndOfPara && lineIndex == (arrayLineDesc.Length - 1))
                            ||  (dcpEnd >= elemDesc.dcpLim && HasAnyLineBreakAtCp(elemDesc.dcpLim)  )   )
                        {
                            endOfParaGlyphWidth = ((double)TextParagraph.Element.GetValue(TextElement.FontSizeProperty) * CaretElement.c_endOfParaMagicMultiplier);
                        }
                        else
                        {
                            endOfParaGlyphWidth = 0;
                        }

                        //  get rectangles for this element.
                        IList<Rect> rectangles = RectanglesFromDcpRangeOfCompositeLineElement(
                                dcpRangeStartForThisElem,
                                cchRangeForThisElem,
                                lineTopSpace,
                                endOfParaGlyphWidth,
                                ref lineDesc,
                                lineIndex,
                                ref elemDesc,
                                elemIndex,
                                visibleRect
                                );

                        if (rectangles != null)
                        {
                            for (int i = 0, count = rectangles.Count; i < count; ++i)
                            {
                                RectangleGeometry rectGeometry = new RectangleGeometry(rectangles[i]);
                                CaretElement.AddGeometry(ref geometry, rectGeometry);
                            }
                        }
                    }
                }
            }

            return geometry;
        }

        // ------------------------------------------------------------------
        //  Helper to check if there is any line break at the given dcp.
        //  Dcp adjusted to this paragraph.
        // ------------------------------------------------------------------
        private bool HasAnyLineBreakAtCp(int dcp)
        {
            ITextPointer position = Paragraph.StructuralCache.TextContainer.CreatePointerAtOffset(Paragraph.ParagraphStartCharacterPosition + dcp, LogicalDirection.Forward);
            // The call to IsNextToAnyBreak is pretty expensive 31->47ms regression. Need to optimize it.
            return (TextPointerBase.IsNextToAnyBreak(position, LogicalDirection.Backward));
        }

        // ------------------------------------------------------------------
        //  Returns rectangles for a single simple line correcsponding to the
        //  given dcp range. Includes trailing whitespaces.
        //  Params:
        //      dcpRangeStart     - range's cp start position. Adjusted for
        //                          line's cp range.
        //      cchRange          - nuber of cps in the range.
        //      lineTopSpace      - the value that line's height should
        //                          be extended to at the top.
        //      lineRightSpace    - the value that line's width should
        //                          be extended to at the right.
        //      lineDesc          - line description.
        //      lineIndex         - line index.
        //      visibleRect       - visibility rectangle. It is Ok to return
        //                          null if the line is not visible.
        //      hasAttachedObjects- attached objects are present.
        //  Returns:
        //      null              - if line is not visible
        //      rectangles        - otherwise.
        // ------------------------------------------------------------------
        private List<Rect> RectanglesFromDcpRangeOfSimpleLine(
            int dcpRangeStart,
            int cchRange,
            double lineTopSpace,
            double lineRightSpace,
            ref PTS.FSLINEDESCRIPTIONSINGLE lineDesc,
            int lineIndex,
            Rect visibleRect)
        {
            List<Rect> rectangles = null;

            Invariant.Assert(lineDesc.dcpFirst <= dcpRangeStart && dcpRangeStart <= lineDesc.dcpLim && cchRange > 0);

            Rect lineRect = new PTS.FSRECT(lineDesc.urBBox, lineDesc.vrStart, lineDesc.durBBox, lineDesc.dvrAscent + lineDesc.dvrDescent).FromTextDpi();

            //  width has to be adjusted to include trailing whitespaces...
            LineVisual lineVisual = FetchLineVisual(lineIndex);
            if (lineVisual != null)
            {
                lineRect.Width = Math.Max(lineVisual.WidthIncludingTrailingWhitespace, 0);
            }

            lineRect.Y = lineRect.Y - lineTopSpace;
            lineRect.Height = lineRect.Height + lineTopSpace;
            lineRect.Width = lineRect.Width + lineRightSpace;

            // Ignore horizontal offset because TextBox page width != extent width.
            // It's ok to include content that doesn't strictly intersect -- this
            // is a perf optimization and the edge cases won't significantly hurt us.
            Rect testRect = lineRect;
            testRect.X = visibleRect.X;

            if (testRect.IntersectsWith(visibleRect))
            {
                // Check whether the line is fully selected - we don't need to reformat it in this case
                if (dcpRangeStart == lineDesc.dcpFirst && lineDesc.dcpLim <= (dcpRangeStart + cchRange))
                {
                    rectangles = new List<Rect>(1);
                    rectangles.Add(lineRect);
                }
                else
                {
                    //  create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);
                    if (IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }
                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    double duOffset = TextDpi.FromTextDpi(lineDesc.urStart);
                    double dvOffset = TextDpi.FromTextDpi(lineDesc.vrStart);

                    rectangles = line.GetRangeBounds(dcpRangeStart, cchRange, duOffset, dvOffset);

                    if (!DoubleUtil.IsZero(lineTopSpace))
                    {
                        for (int i = 0, count = rectangles.Count; i < count; ++i)
                        {
                            Rect r = rectangles[i];
                            r.Y = r.Y - lineTopSpace;
                            r.Height = r.Height + lineTopSpace;
                            rectangles[i] = r;
                        }
                    }

                    if (!DoubleUtil.IsZero(lineRightSpace))
                    {
                        // add the rect representing end-of-line / end-of-para
                        rectangles.Add(
                            new Rect(
                                duOffset + TextDpi.FromTextDpi(line.Start + line.Width),
                                dvOffset - lineTopSpace,
                                lineRightSpace,
                                TextDpi.FromTextDpi(line.Height) + lineTopSpace
                                )
                            );
                    }

                    //  dispose of the line
                    line.Dispose();
                }
            }

            return (rectangles);
        }

        // ------------------------------------------------------------------
        //  Returns rectangles for a single composite line correcsponding to the
        //  given dcp range. Includes trailing whitespaces.
        //  Params:
        //      dcpRangeStart     - range's cp start position. Adjusted for
        //                          line's cp range.
        //      cchRange          - nuber of cps in the range.
        //      lineTopSpace      - the value that line's height should
        //                          be extended to at the top.
        //      lineRightSpace    - the value that line's width should
        //                          be extended to at the right.
        //      lineDesc          - line description.
        //      lineIndex         - line index.
        //      elemDesc          - element description.
        //      visibleRect       - visibility rectangle. It is Ok to return
        //                          null if the line is not visible.
        //      hasAttachedObjects- Attached objects are present
        //  Returns:
        //      null              - if line is not visible
        //      rectangles        - otherwise.
        // ------------------------------------------------------------------
        private List<Rect> RectanglesFromDcpRangeOfCompositeLineElement(
            int dcpRangeStart,
            int cchRange,
            double lineTopSpace,
            double lineRightSpace,
            ref PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc,
            int lineIndex,
            ref PTS.FSLINEELEMENT elemDesc,
            int elemIndex,
            Rect visibleRect)
        {
            List<Rect> rectangles = null;

            Rect elementRect = new PTS.FSRECT(elemDesc.urBBox, lineDesc.vrStart, elemDesc.durBBox, lineDesc.dvrAscent + lineDesc.dvrDescent).FromTextDpi();

            //  width has to be adjusted to include trailing whitespaces...
            LineVisual lineVisual = FetchLineVisualComposite(lineIndex, elemIndex);
            if (lineVisual != null)
            {
                elementRect.Width = Math.Max(lineVisual.WidthIncludingTrailingWhitespace, 0);
            }

            elementRect.Y = elementRect.Y - lineTopSpace;
            elementRect.Height = elementRect.Height + lineTopSpace;
            elementRect.Width = elementRect.Width + lineRightSpace;

            // Ignore horizontal offset because TextBox page width != extent width.
            // It's ok to include content that doesn't strictly intersect -- this
            // is a perf optimization and the edge cases won't significantly hurt us.
            Rect testRect = elementRect;
            testRect.X = visibleRect.X;

            if (testRect.IntersectsWith(visibleRect))
            {
                // Check whether the line is fully selected - we don't need to reformat it in this case
                if (dcpRangeStart == elemDesc.dcpFirst && elemDesc.dcpLim <= (dcpRangeStart + cchRange))
                {
                    rectangles = new List<Rect>(1);
                    rectangles.Add(elementRect);
                }
                else
                {
                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(elemDesc.fClearOnLeft), PTS.ToBoolean(elemDesc.fClearOnRight), TextParagraph.TextRunCache);
                    if (IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = elemDesc.dcpLim - elemDesc.dcpFirst;
                    }
                    TextParagraph.FormatLineCore(line, elemDesc.pfsbreakreclineclient, ctx, elemDesc.dcpFirst, elemDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), elemDesc.dcpFirst);
                    Invariant.Assert(line.SafeLength == elemDesc.dcpLim - elemDesc.dcpFirst, "Line length is out of sync");

                    double duOffset = TextDpi.FromTextDpi(elemDesc.urStart);
                    double dvOffset = TextDpi.FromTextDpi(lineDesc.vrStart);

                    rectangles = line.GetRangeBounds(dcpRangeStart, cchRange, duOffset, dvOffset);

                    if (!DoubleUtil.IsZero(lineTopSpace))
                    {
                        for (int i = 0, count = rectangles.Count; i < count; ++i)
                        {
                            Rect r = rectangles[i];
                            r.Y = r.Y - lineTopSpace;
                            r.Height = r.Height + lineTopSpace;
                            rectangles[i] = r;
                        }
                    }

                    if (!DoubleUtil.IsZero(lineRightSpace))
                    {
                        // add the rect representing end-of-line / end-of-para
                        rectangles.Add(
                            new Rect(
                                duOffset + TextDpi.FromTextDpi(line.Start + line.Width),
                                dvOffset - lineTopSpace,
                                lineRightSpace,
                                TextDpi.FromTextDpi(line.Height) + lineTopSpace
                                )
                            );
                    }

                    //  dispose of the line
                    line.Dispose();
                }
            }

            return (rectangles);
        }

        // ------------------------------------------------------------------
        //  Helper to return visual corresponding to a line in
        //  FSLINEDESCRIPTIONSINGLE array.
        // ------------------------------------------------------------------
        private LineVisual FetchLineVisual(int index)
        {
            LineVisual visual = null;

            int count = VisualTreeHelper.GetChildrenCount(Visual);

            if (count != 0)
            {
                int visualIndex = index;

                if (_lineIndexFirstVisual != -1)
                {
                    visualIndex -= _lineIndexFirstVisual;
                }

                if (0 <= visualIndex && visualIndex < count)
                {
                    visual = VisualTreeHelper.GetChild(Visual, visualIndex) as LineVisual;
                    //  verify that our assumptions about visual structure is correct...
                    Invariant.Assert(visual != null || VisualTreeHelper.GetChild(Visual, visualIndex) == null);
                }
            }

            return (visual);
        }

        // ------------------------------------------------------------------
        //  Helper to return visual corresponding to a line in
        //  FSLINEDESCRIPTIONSINGLE array.
        // ------------------------------------------------------------------
        private LineVisual FetchLineVisualComposite(int lineIndex, int elemIndex)
        {
            LineVisual visual = null;
            Visual temp = Visual;
            int count = VisualTreeHelper.GetChildrenCount(Visual);

            if (count != 0)
            {
                int visualIndex = lineIndex;

                if(VisualTreeHelper.GetChild(Visual, visualIndex) is ParagraphElementVisual)
                {
                    temp = Visual.InternalGetVisualChild(lineIndex);
                    visualIndex = elemIndex;
                }

                visual = VisualTreeHelper.GetChild(temp, visualIndex) as LineVisual;
                //  verify that our assumptions about visual structure is correct...
                Invariant.Assert(visual != null || VisualTreeHelper.GetChild(temp, visualIndex) == null);
            }

            return (visual);
        }

        // ------------------------------------------------------------------
        // Returns tight bounding path geometry for
        // floaters and figures objects
        // ------------------------------------------------------------------
        private Geometry PathGeometryFromDcpRangeFloatersAndFigures(
            int dcpStart,
            int dcpEnd,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            Geometry geometry = null;

            if (textDetails.cAttachedObjects > 0)
            {
                //  get list of attached objects
                PTS.FSATTACHEDOBJECTDESCRIPTION[] arrayAttachedObjectDesc;
                PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, textDetails.cAttachedObjects, out arrayAttachedObjectDesc);

                for (int index = 0; index < arrayAttachedObjectDesc.Length; ++index)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                    BaseParaClient objectParaClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(objectParaClient);

                    BaseParagraph objectPara = objectParaClient.Paragraph;

                    if (dcpEnd <= objectPara.ParagraphStartCharacterPosition)
                    {
                        break;
                    }

                    if (objectPara.ParagraphEndCharacterPosition > dcpStart)
                    {
                        Rect objectRect = objectParaClient.Rect.FromTextDpi();
                        RectangleGeometry objectGeometry = new RectangleGeometry(objectRect);
                        CaretElement.AddGeometry(ref geometry, objectGeometry);
                    }
                }
            }

            return (geometry);
        }

        // ------------------------------------------------------------------
        // Return true if caret is at unit boundary
        // ------------------------------------------------------------------
        private bool IsAtCaretUnitBoundaryFromDcpSimpleLines(
            int dcp,
            ITextPointer position,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return false;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            bool isAtCaretUnitBoundary = false;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                // 'dcp' needs to be within line range. If position points to dcpLim,
                // it means that the next line starts from such position, hence go to the next line.
                if (((lineDesc.dcpFirst <= dcp) && (lineDesc.dcpLim > dcp))
                    || ((lineDesc.dcpLim == dcp) && (index == arrayLineDesc.Length - 1)))
                {
                    CharacterHit charHit = new CharacterHit();
                    if (dcp >= lineDesc.dcpLim - 1 && index == arrayLineDesc.Length - 1)
                    {
                        // Special case: last line has additional character to mark the end of paragraph.
                        // We should not try and check for next source character index
                        // But just return true in this case
                        return true;
                    }

                    if (position.LogicalDirection == LogicalDirection.Backward)
                    {
                        if (lineDesc.dcpFirst == dcp)
                        {
                            if (index == 0)
                            {
                                // First position of first line does not have a trailing edge. Return false.
                                return false;
                            }
                            else
                            {
                                // Get the trailing edge of the last character on the previous line, at dcp - 1
                                index--;
                                lineDesc = arrayLineDesc[index];
                                Invariant.Assert(dcp > 0);
                                charHit = new CharacterHit(dcp - 1, 1);
                            }
                        }
                        else
                        {
                            // Get CharacterHit at trailing edge of previous position
                            Invariant.Assert(dcp > 0);
                            charHit = new CharacterHit(dcp - 1, 1);
                        }
                    }
                    else if (position.LogicalDirection == LogicalDirection.Forward)
                    {
                        // Get character hit at leading edge
                        charHit = new CharacterHit(dcp, 0);
                    }

                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");
                    isAtCaretUnitBoundary = line.IsAtCaretCharacterHit(charHit);

                    // Dispose the line
                    line.Dispose();
                    break;
                }
            }
            return isAtCaretUnitBoundary;
        }

        // ------------------------------------------------------------------
        // Return true if caret is at unit boundary
        // ------------------------------------------------------------------
        private bool IsAtCaretUnitBoundaryFromDcpCompositeLines(
            int dcp,
            ITextPointer position,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return false;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            bool isAtCaretUnitBoundary = false;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0)
                {
                    continue;
                }

                // Get list of line elements.
                PTS.FSLINEELEMENT[] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // 'dcp' needs to be within line range. If position points to dcpLim,
                    // it means that the next line starts from such position, hence go to the next line.
                    if (((element.dcpFirst <= dcp) && (element.dcpLim > dcp))
                        || ((element.dcpLim == dcp) && (elIndex == arrayLineElement.Length - 1) && (index == arrayLineDesc.Length - 1)))
                    {
                        CharacterHit charHit = new CharacterHit();
                        if (dcp >= element.dcpLim - 1 && elIndex == arrayLineElement.Length - 1 && index == arrayLineDesc.Length - 1)
                        {
                            // Special case: at the end of the last line there is a special character that
                            // does not belong to the line.  Return true for this case
                            return true;
                        }

                        if (position.LogicalDirection == LogicalDirection.Backward)
                        {
                            // Beginning of element.
                            if (dcp == element.dcpFirst)
                            {
                                if (elIndex > 0)
                                {
                                    // Beginning of element, but not of line. Create char hit at last dcp of previous element, trailing edge.
                                    --elIndex;
                                    element = arrayLineElement[elIndex];
                                    charHit = new CharacterHit(dcp - 1, 1);
                                }
                                else
                                {
                                    // Beginning of line
                                    if (index == 0)
                                    {
                                        // Backward context at start position of first line is not considered a unit boundary
                                        return false;
                                    }
                                    else
                                    {
                                        // Go to previous line
                                        --index;
                                        lineDesc = arrayLineDesc[index];
                                        if (lineDesc.cElements == 0)
                                        {
                                            return false;
                                        }
                                        else
                                        {
                                            // Get list of line elements.
                                            PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);
                                            element = arrayLineElement[arrayLineElement.Length - 1];
                                            charHit = new CharacterHit(dcp - 1, 1);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Get trailing edge of previous dcp
                                Invariant.Assert(dcp > 0);
                                charHit = new CharacterHit(dcp - 1, 1);
                            }
                        }
                        else if (position.LogicalDirection == LogicalDirection.Forward)
                        {
                            // Create character hit at leading edge
                            charHit = new CharacterHit(dcp, 0);
                        }

                        // Create and format line
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");
                        isAtCaretUnitBoundary = line.IsAtCaretCharacterHit(charHit);

                        // Dispose the line
                        line.Dispose();
                        return isAtCaretUnitBoundary;
                    }
                }
            }
            return isAtCaretUnitBoundary;
        }

        // ------------------------------------------------------------------
        // Get Next caret unit position
        // ------------------------------------------------------------------
        private ITextPointer NextCaretUnitPositionFromDcpSimpleLines(
            int dcp,
            ITextPointer position,
            LogicalDirection direction,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return position;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Declare next position and set it to initial position
            ITextPointer nextCaretPosition = position;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                // 'dcp' needs to be within line range. If position points to dcpLim,
                // it means that the next line starts from such position, hence go to the next line.
                if (((lineDesc.dcpFirst <= dcp) && (lineDesc.dcpLim > dcp))
                    || ((lineDesc.dcpLim == dcp) && (index == arrayLineDesc.Length - 1)))
                {
                    if (dcp == lineDesc.dcpFirst && direction == LogicalDirection.Backward)
                    {
                        // Go to previous line
                        if (index == 0)
                        {
                            return position;
                        }
                        else
                        {
                            // Update dcp, lineDesc
                            Debug.Assert(index > 0);
                            --index;
                            lineDesc = arrayLineDesc[index];
                        }
                    }
                    else if (dcp >= lineDesc.dcpLim - 1 && direction == LogicalDirection.Forward)
                    {
                        // If we are at the last line there will be a fake marker for this, so we return
                        if (index == arrayLineDesc.Length - 1)
                        {
                            return position;
                        }
                    }

                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Create CharacterHit
                    CharacterHit charHit = new CharacterHit(dcp, 0);

                    // Get previous caret position from the line
                    // Determine logical direction for next caret index and create TextPointer from it
                    CharacterHit nextCharacterHit;
                    if (direction == LogicalDirection.Forward)
                    {
                        nextCharacterHit = line.GetNextCaretCharacterHit(charHit);
                    }
                    else
                    {
                        nextCharacterHit = line.GetPreviousCaretCharacterHit(charHit);
                    }

                    LogicalDirection logicalDirection;
                    if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == lineDesc.dcpLim) && direction == LogicalDirection.Forward)
                    {
                        // Going forward brought us to the end of a line, context must be forward for next line
                        if (index == arrayLineDesc.Length - 1)
                        {
                            // last line so context must stay backward
                            logicalDirection = LogicalDirection.Backward;
                        }
                        else
                        {
                            logicalDirection = LogicalDirection.Forward;
                        }
                    }
                    else if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == lineDesc.dcpFirst) && direction == LogicalDirection.Backward)
                    {
                        // Going forward brought us to the start of a line, context must be backward for previous line
                        if (index == 0)
                        {
                            // First line, so we will stay forward
                            logicalDirection = LogicalDirection.Forward;
                        }
                        else
                        {
                            logicalDirection = LogicalDirection.Backward;
                        }
                    }
                    else
                    {
                        logicalDirection = (nextCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                    }
                    nextCaretPosition = GetTextPosition(nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength, logicalDirection);

                    // Dispose the line
                    line.Dispose();
                    break;
                }
            }
            return nextCaretPosition;
        }

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text position.
        // ------------------------------------------------------------------
        private ITextPointer NextCaretUnitPositionFromDcpCompositeLines(
            int dcp,
            ITextPointer position,
            LogicalDirection direction,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return position;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Declare next position and set it to initial position
            ITextPointer nextCaretPosition = position;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) { continue; }

                // Get list of line elements.
                PTS.FSLINEELEMENT [] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // 'dcp' needs to be within line range. If position points to dcpLim,
                    // it means that the next line starts from such position, hence go to the next line.
                    if (   ((element.dcpFirst <= dcp) && (element.dcpLim > dcp))
                        || ((element.dcpLim == dcp) && (elIndex == arrayLineElement.Length - 1) && (index == arrayLineDesc.Length - 1)))
                    {
                        if (dcp == element.dcpFirst && direction == LogicalDirection.Backward)
                        {
                            // Beginning of element.
                            if (dcp == 0)
                            {
                                // Assert that this is first elment on first line
                                Debug.Assert(index == 0);
                                Debug.Assert(elIndex == 0);
                                return position;
                            }
                            else
                            {
                                if (elIndex > 0)
                                {
                                    // Beginning of element, but not of line
                                    --elIndex;
                                    element = arrayLineElement[elIndex];
                                }
                                else
                                {
                                    // There must be at least one line above this
                                    Debug.Assert(index > 0);

                                    // Go to previous line
                                    --index;
                                    lineDesc = arrayLineDesc[index];
                                    if (lineDesc.cElements == 0)
                                    {
                                        // Stay in same position
                                        return position;
                                    }
                                    else
                                    {
                                        // Get list of line elements.
                                        PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);
                                        element = arrayLineElement[arrayLineElement.Length - 1];
                                    }
                                }
                            }
                        }
                        else if (dcp >= element.dcpLim - 1 && direction == LogicalDirection.Forward)
                        {
                            if (dcp == element.dcpLim)
                            {
                                // End of paragraph
                                Debug.Assert(elIndex == arrayLineElement.Length - 1);
                                Debug.Assert(index == arrayLineDesc.Length - 1);
                                return position;
                            }
                            else if (dcp == element.dcpLim - 1 && elIndex == arrayLineElement.Length - 1 && index == arrayLineDesc.Length - 1)
                            {
                                // Special end character does not belong to line
                                return position;
                            }
                        }

                        // Create and format line
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Create CharacterHit from dcp, and get next
                        CharacterHit charHit = new CharacterHit(dcp, 0);
                        CharacterHit nextCharacterHit;
                        if (direction == LogicalDirection.Forward)
                        {
                            nextCharacterHit = line.GetNextCaretCharacterHit(charHit);
                        }
                        else
                        {
                            nextCharacterHit = line.GetPreviousCaretCharacterHit(charHit);
                        }

                        LogicalDirection logicalDirection;
                        if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == element.dcpLim) && direction == LogicalDirection.Forward)
                        {
                            // Going forward brought us to the end of a line, context must be forward for next line
                            if (index == arrayLineDesc.Length - 1)
                            {
                                // last line so context must stay backward
                                logicalDirection = LogicalDirection.Backward;
                            }
                            else
                            {
                                // It is a new element, on the same line or a new one. Either way it;s forward context
                                logicalDirection = LogicalDirection.Forward;
                            }
                        }
                        else if ((nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == element.dcpFirst) && direction == LogicalDirection.Backward)
                        {
                            // Going forward brought us to the start of a line, context must be backward for previous line
                            if (index == 0)
                            {
                                // First line, so we will stay forward
                                logicalDirection = LogicalDirection.Forward;
                            }
                            else
                            {
                                // Either the previous element or last element on previous line, context is backward
                                logicalDirection = LogicalDirection.Backward;
                            }
                        }
                        else
                        {
                            logicalDirection = (nextCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                        }
                        nextCaretPosition = GetTextPosition(nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength, logicalDirection);

                        // Dispose the line
                        line.Dispose();
                        return nextCaretPosition;
                    }
                }
            }
            return nextCaretPosition;
        }

        // ------------------------------------------------------------------
        // Get Backspace caret unit position
        // ------------------------------------------------------------------
        private ITextPointer BackspaceCaretUnitPositionFromDcpSimpleLines(
            int dcp,
            ITextPointer position,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return position;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Declare backspace position and set it to initial position
            ITextPointer backspaceCaretPosition = position;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                // 'dcp' needs to be within line range. If position points to dcpLim,
                // it means that the next line starts from such position, hence go to the next line.
                if (((lineDesc.dcpFirst <= dcp) && (lineDesc.dcpLim > dcp))
                    || ((lineDesc.dcpLim == dcp) && (index == arrayLineDesc.Length - 1)))
                {
                    if (dcp == lineDesc.dcpFirst)
                    {
                        // Go to previous line
                        if (index == 0)
                        {
                            return position;
                        }
                        else
                        {
                            // Update dcp, lineDesc
                            Debug.Assert(index > 0);
                            --index;
                            lineDesc = arrayLineDesc[index];
                        }
                    }

                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Create CharacterHit and get backspace index from line API
                    CharacterHit textSourceCharacterIndex = new CharacterHit(dcp, 0);
                    CharacterHit backspaceCharacterHit = line.GetBackspaceCaretCharacterHit(textSourceCharacterIndex);
                    LogicalDirection logicalDirection;
                    if (backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength == lineDesc.dcpFirst)
                    {
                        // Going forward brought us to the start of a line, context must be backward for previous line
                        if (index == 0)
                        {
                            // First line, so we will stay forward
                            logicalDirection = LogicalDirection.Forward;
                        }
                        else
                        {
                            logicalDirection = LogicalDirection.Backward;
                        }
                    }
                    else
                    {
                        logicalDirection = (backspaceCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                    }
                    backspaceCaretPosition = GetTextPosition(backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength, logicalDirection);

                    // Dispose the line
                    line.Dispose();
                    break;
                }
            }
            Debug.Assert(backspaceCaretPosition != null);
            return backspaceCaretPosition;
        }

        // ------------------------------------------------------------------
        // Retrieve bounds of an object/character at specified text position.
        // ------------------------------------------------------------------
        private ITextPointer BackspaceCaretUnitPositionFromDcpCompositeLines(
            int dcp,
            ITextPointer position,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return position;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Declare backspace position and set it to initial position
            ITextPointer backspaceCaretPosition = position;

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) { continue; }

                // Get list of line elements.
                PTS.FSLINEELEMENT[] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // 'dcp' needs to be within line range. If position points to dcpLim,
                    // it means that the next line starts from such position, hence go to the next line.
                    if (((element.dcpFirst <= dcp) && (element.dcpLim > dcp))
                        || ((element.dcpLim == dcp) && (elIndex == arrayLineElement.Length - 1) && (index == arrayLineDesc.Length - 1)))
                    {
                        if (dcp == element.dcpFirst)
                        {
                            // Beginning of element.
                            if (dcp == 0)
                            {
                                // Assert that this is first elment on first line
                                Debug.Assert(index == 0);
                                Debug.Assert(elIndex == 0);
                                return position;
                            }
                            else
                            {
                                if (elIndex > 0)
                                {
                                    // Beginning of element, but not of line
                                    --elIndex;
                                    element = arrayLineElement[elIndex];
                                }
                                else
                                {
                                    // There must be at least one line above this
                                    Debug.Assert(index > 0);

                                    // Go to previous line
                                    --index;
                                    lineDesc = arrayLineDesc[index];
                                    if (lineDesc.cElements == 0)
                                    {
                                        // Stay in same position
                                        return position;
                                    }
                                    else
                                    {
                                        // Get list of line elements.
                                        PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);
                                        element = arrayLineElement[arrayLineElement.Length - 1];
                                    }
                                }
                            }
                        }

                        // Create and format line
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Create CharacterHit from dcp, and get backspace
                        CharacterHit charHit = new CharacterHit(dcp, 0);
                        CharacterHit backspaceCharacterHit = line.GetBackspaceCaretCharacterHit(charHit);
                        LogicalDirection logicalDirection;
                        if (backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength == element.dcpFirst)
                        {
                            // Going forward brought us to the start of a line, context must be backward for previous line
                            if (index == 0)
                            {
                                // First line, so we will stay forward
                                logicalDirection = LogicalDirection.Forward;
                            }
                            else
                            {
                                logicalDirection = LogicalDirection.Backward;
                            }
                        }
                        else
                        {
                            logicalDirection = (backspaceCharacterHit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
                        }
                        backspaceCaretPosition = GetTextPosition(backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength, logicalDirection);

                        // Dispose the line
                        line.Dispose();
                        return backspaceCaretPosition;
                    }
                }
            }
            return backspaceCaretPosition;
        }

        // ------------------------------------------------------------------
        // Retrieves collection of GlyphRuns from a range of text.
        // ------------------------------------------------------------------
        private void GetGlyphRunsFromSimpleLines(
            List<GlyphRun> glyphRuns,
            int dcpStart,
            int dcpEnd,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Iterate through all lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                // Range (dcpStart...dcpEnd) needs to insersect with line's range.
                if (dcpStart < lineDesc.dcpLim && dcpEnd > lineDesc.dcpFirst)
                {
                    // Create and format line
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Retrieve glyphs from this line
                    line.GetGlyphRuns(glyphRuns, Math.Max(dcpStart, lineDesc.dcpFirst), Math.Min(dcpEnd, lineDesc.dcpLim));

                    // Dispose the line
                    line.Dispose();
                }
                // No need to continue, if dcpEnd has been reached.
                if (dcpEnd < lineDesc.dcpLim)
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Retrieves collection of GlyphRuns from a range of text.
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        private void GetGlyphRunsFromCompositeLines(
            List<GlyphRun> glyphRuns,
            int dcpStart,
            int dcpEnd,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return;

            // Get list of lines
            PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // First iterate through lines
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) { continue; }

                // Get list of line elements.
                PTS.FSLINEELEMENT[] arrayLineElement;
                PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                {
                    PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                    // Range (dcpStart...dcpEnd) needs to insersect with line's range.
                    if (dcpStart < element.dcpLim && dcpEnd > element.dcpFirst)
                    {
                        // Create and format line
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Retrieve glyphs from this line
                        line.GetGlyphRuns(glyphRuns, Math.Max(dcpStart, element.dcpFirst), Math.Min(dcpEnd, element.dcpLim));

                        // Dispose the line
                        line.Dispose();
                    }
                    // No need to continue, if dcpEnd has been reached.
                    if (dcpEnd < element.dcpLim)
                        break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Render text paragraph content.
        // ------------------------------------------------------------------
        private void RenderSimpleLines(
            ContainerVisual visual,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            bool ignoreUpdateInfo)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);
            int cpTextParaStart = Paragraph.ParagraphStartCharacterPosition;

            if (textDetails.cLines == 0)
                return;

            VisualCollection visualChildren = visual.Children;

            // Get list of simple lines.
            PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Create lines and render them
            if (!PTS.ToBoolean(textDetails.fUpdateInfoForLinesPresent) || ignoreUpdateInfo)
            {
                // There is no update information, hence need to recreate
                // visuals for all lines.
                visualChildren.Clear();

                for (int index = 0; index < arrayLineDesc.Length; index++)
                {
                    PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                    // Create and format line
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Create and validate line's visual
                    ContainerVisual lineVisual = line.CreateVisual();
                    visualChildren.Insert(index, lineVisual);
                    lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

                    // Dispose the line
                    line.Dispose();
                }
            }
            else
            {
                // Shift lines before change
                if(textDetails.dvrShiftBeforeChange != 0)
                {
                    for(int index = 0; index < textDetails.cLinesBeforeChange; index++)
                    {
                        ContainerVisual lineVisual = (ContainerVisual) visualChildren[index];
                        Vector offset = lineVisual.Offset;
                        offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftBeforeChange);
                        lineVisual.Offset = offset;
                    }
                }

                // Skip not changed lines
                // Remove changed lines.
                visualChildren.RemoveRange(textDetails.cLinesBeforeChange, textDetails.cLinesChanged - textDetails.dcLinesChanged);
                // Add new lines
                for (int index = textDetails.cLinesBeforeChange; index < textDetails.cLinesBeforeChange + textDetails.cLinesChanged; index++)
                {
                    PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                    // Create and format line
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }

                    TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                    // Assert that number of characters in Text line is the same as our expected length
                    Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                    // Create and validate line's visual
                    ContainerVisual lineVisual = line.CreateVisual();
                    visualChildren.Insert(index, lineVisual);
                    lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

                    // Dispose the line
                    line.Dispose();
                }
                // Shift remaining lines
                for (int index = textDetails.cLinesBeforeChange + textDetails.cLinesChanged; index < arrayLineDesc.Length; index++)
                {
                    // Shift line's visual
                    ContainerVisual lineVisual = (ContainerVisual) visualChildren[index];
                    Vector offset = lineVisual.Offset;
                    offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftAfterChange);
                    lineVisual.Offset = offset;
                }
            }
        }


        // ------------------------------------------------------------------
        // Determines whether this para intersects with the rect on v dimension
        // ------------------------------------------------------------------
        private bool IntersectsWithRectOnV(ref PTS.FSRECT rect)
        {
            return ((_rect.v) <= (rect.v + rect.dv)) &&
                   ((_rect.v + _rect.dv) >= rect.v);
        }

        // ------------------------------------------------------------------
        // Determines whether this para is totally contained in the given rect on v dimension
        // ------------------------------------------------------------------
        private bool ContainedInRectOnV(ref PTS.FSRECT rect)
        {
            return (rect.v <= (_rect.v)) &&
                   (rect.v + rect.dv >= (_rect.v + _rect.dv));
        }

        // ------------------------------------------------------------------
        // From a line desc and the cp for the para start, formats a line and creates a visual
        // ------------------------------------------------------------------
        private ContainerVisual CreateLineVisual(ref PTS.FSLINEDESCRIPTIONSINGLE lineDesc, int cpTextParaStart)
        {
            Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);
            Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

            if(IsOptimalParagraph)
            {
                ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
            }

            TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

            // Assert that number of characters in Text line is the same as our expected length
            Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

            // Create and validate line's visual
            ContainerVisual lineVisual = line.CreateVisual();

            // Dispose the line
            line.Dispose();

            return lineVisual;
        }

        // ------------------------------------------------------------------
        // Resyncs the committed visuals section with actual viewport visibility
        // ------------------------------------------------------------------
        private void UpdateViewportSimpleLines(
            ContainerVisual visual,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            ref PTS.FSRECT viewport)
        {
            VisualCollection visualChildren = visual.Children;

            Debug.Assert(!PTS.ToBoolean(textDetails.fLinesComposite));

            try
            {
                // Common case, invisible para - Clear our children, _lineIndexFirstVisual will be cleared later
                if (!IntersectsWithRectOnV(ref viewport) || textDetails.cLines == 0)
                {
                    visualChildren.Clear();
                }
                else if (ContainedInRectOnV(ref viewport) && _lineIndexFirstVisual == 0 && visualChildren.Count == textDetails.cLines)
                {
                    // Totally visible para
                    // Nothing to do here, totally visible and lines are updated. Don't query line list
                }
                else
                {
                    // Index of first visible line
                    int lineIndexFirstVisible = -1;
                    // Index of first invisible line - MAY BE EQUAL TO COUNT.
                    int lineIndexFirstInvisible = -1;
                    int cpTextParaStart = Paragraph.ParagraphStartCharacterPosition;

                    PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
                    PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

                    // If this para is totally contained in the viewport, valid range is all lines.
                    if (ContainedInRectOnV(ref viewport))
                    {
                        lineIndexFirstVisible = 0;
                        lineIndexFirstInvisible = textDetails.cLines;
                    }
                    else
                    {
                        // Subset is valid, walk the lines to determine the first (even partially) visible line
                        int lineIndex;

                        for (lineIndex = 0; lineIndex < arrayLineDesc.Length; lineIndex++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[lineIndex];

                            // Vrstart is top of line, not baseline.
                            if ((lineDesc.vrStart + lineDesc.dvrAscent + lineDesc.dvrDescent) > viewport.v)
                            {
                                break;
                            }
                        }

                        // May be equal to count if no lines are visible
                        lineIndexFirstVisible = lineIndex;

                        // Subset is valid, walk the lines to determine the first totally invisible line
                        for (lineIndex = lineIndexFirstVisible; lineIndex < arrayLineDesc.Length; lineIndex++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[lineIndex];

                            if ((lineDesc.vrStart) > (viewport.v + viewport.dv))
                            {
                                break;
                            }
                        }

                        // May be equal to count if the remainder of lines is visible.
                        lineIndexFirstInvisible = lineIndex;
                    }

                    // If we have some committed range, but there's no overlap between the previously committed range and the new desired range,
                    // Delete all existing lines.
                    if (_lineIndexFirstVisual != -1 && ((lineIndexFirstVisible > _lineIndexFirstVisual + visualChildren.Count) ||
                                                       (lineIndexFirstInvisible < _lineIndexFirstVisual)))
                    {
                        visualChildren.Clear();
                        _lineIndexFirstVisual = -1;
                    }

                    // If no existing lines, interate over visible range and add appropriate lines.
                    if (_lineIndexFirstVisual == -1)
                    {
                        for (int index = lineIndexFirstVisible; index < lineIndexFirstInvisible; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                            ContainerVisual lineVisual = CreateLineVisual(ref lineDesc, cpTextParaStart);
                            visualChildren.Add(lineVisual);
                            lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));
                        }
                        _lineIndexFirstVisual = lineIndexFirstVisible;
                    }
                    else if (lineIndexFirstVisible != _lineIndexFirstVisual || (lineIndexFirstInvisible - lineIndexFirstVisible) != visualChildren.Count)
                    {
                        // Need to resolve existing list with new list - sync the beginning of the list


                        //    |----------------| (Old committed range)
                        // |-------|             (New committed range)
                        // Need to add visuals to the beginning to sync the start position
                        if (lineIndexFirstVisible < _lineIndexFirstVisual)
                        {
                            for (int index = lineIndexFirstVisible; index < _lineIndexFirstVisual; index++)
                            {
                                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                                ContainerVisual lineVisual = CreateLineVisual(ref lineDesc, cpTextParaStart);

                                visualChildren.Insert(index - lineIndexFirstVisible, lineVisual);
                                lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));
                            }
                        }
                        else if (lineIndexFirstVisible != _lineIndexFirstVisual)
                        {
                            //    |----------------| (Old committed range)
                            //        |-------|             (New committed range)
                            // Need to remove visuals from the beginning to sync start positions.

                            visualChildren.RemoveRange(0, lineIndexFirstVisible - _lineIndexFirstVisual);
                        }
                        _lineIndexFirstVisual = lineIndexFirstVisible;
                    }

                    Debug.Assert(_lineIndexFirstVisual == lineIndexFirstVisible);


                    // Now sync the end of the list, two cases..
                    // Fewer lines than existed before, remove lines from end
                    // |---------------|
                    // |----------|
                    if (lineIndexFirstInvisible - lineIndexFirstVisible < visualChildren.Count)
                    {
                        int visualsToRemove = visualChildren.Count - (lineIndexFirstInvisible - lineIndexFirstVisible);

                        visualChildren.RemoveRange(visualChildren.Count - visualsToRemove, visualsToRemove);
                    }
                    else if ((lineIndexFirstInvisible - lineIndexFirstVisible) > visualChildren.Count)
                    {
                        // Or we need to add more lines to the end, format and insert those visuals
                        // |--------------|
                        // |----------------------|

                        for (int index = _lineIndexFirstVisual + visualChildren.Count; index < lineIndexFirstInvisible; index++)
                        {
                            PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                            ContainerVisual lineVisual = CreateLineVisual(ref lineDesc, cpTextParaStart);
                            visualChildren.Add(lineVisual);
                            lineVisual.Offset = new Vector(TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));
                        }
                    }

                    Debug.Assert(visualChildren.Count == (lineIndexFirstInvisible - lineIndexFirstVisible));
                }
            }

            finally
            {
                // Ensure the _lineIndexFirstVisual is syncced with visualChildren.Count
                if (visualChildren.Count == 0)
                {
                    _lineIndexFirstVisual = -1;
                }
            }

#if VERIFY_VISUALS
            // Verify visuals against visuals list.
            VerifyVisuals(ref textDetails);
#endif
        }

#if VERIFY_VISUALS

        // Verifies all of our committed visuals are in the right order and agree with the locations of all of the full text details visuals
        private void VerifyVisuals(ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ContainerVisual lineContainerVisual = _visual;

            // Add visuals for floaters and figures.
            if (textDetails.cFigures > 0 || textDetails.cFloaters > 0)
            {
                lineContainerVisual = _visual.Children[0];
            }

            VisualCollection visualChildren = lineContainerVisual.Children;

            if(_lineIndexFirstVisual == -1)
            {
                Debug.Assert(visualChildren.Count == 0);
            }
            else
            {
                Debug.Assert(visualChildren.Count > 0 && textDetails.cLines > 0);

                PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
                PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

                for(int index = _lineIndexFirstVisual; index < _lineIndexFirstVisual + visualChildren.Count; index++)
                {
                    PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];
                    Visual visualChild = visualChildren[index - _lineIndexFirstVisual];

                    Vector offset = visualChild.Offset;
                    int u = TextDpi.ToTextDpi(offset.X);
                    int v = TextDpi.ToTextDpi(offset.Y);

                    Debug.Assert(u == (lineDesc.urStart) && v == (lineDesc.vrStart));
                }
            }
        }

#endif

        // ------------------------------------------------------------------
        // Render composite lines.
        // ------------------------------------------------------------------
        private void RenderCompositeLines(
            ContainerVisual visual,
            ref PTS.FSTEXTDETAILSFULL textDetails,
            bool ignoreUpdateInfo)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            VisualCollection visualChildren = visual.Children;
            int cpTextParaStart = Paragraph.ParagraphStartCharacterPosition;

            // Get list of composite lines.
            PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Create lines and render them
            if (!PTS.ToBoolean(textDetails.fUpdateInfoForLinesPresent) || ignoreUpdateInfo)
            {
                // There is no update information, hence need to recreate
                // visuals for all lines.
                visualChildren.Clear();

                for (int index = 0; index < arrayLineDesc.Length; index++)
                {
                    PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];

                    int visualIndex;
                    VisualCollection lineVisuals;
                    if (lineDesc.cElements == 1)
                    {
                        visualIndex = index;
                        lineVisuals = visualChildren;
                    }
                    else
                    {
                        visualIndex = 0;
                        ParagraphElementVisual lineVisual = new ParagraphElementVisual();
                        visualChildren.Add(lineVisual);
                        lineVisuals = lineVisual.Children;
                    }

                    // Get list of line elements
                    PTS.FSLINEELEMENT [] arrayLineElement;
                    PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                    for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                    {
                        PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                        // Create and format line
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Create and validate line's visual
                        ContainerVisual lineVisual = line.CreateVisual();
                        lineVisuals.Insert(visualIndex + elIndex, lineVisual);
                        lineVisual.Offset = new Vector(TextDpi.FromTextDpi(element.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

                        // Dispose the line
                        line.Dispose();
                    }
                }
            }
            else
            {
                // Shift lines before change
                if(textDetails.dvrShiftBeforeChange != 0)
                {
                    for (int index = 0; index < textDetails.cLinesBeforeChange; index++)
                    {
                        ContainerVisual lineVisual = (ContainerVisual) visualChildren[index];
                        Vector offset = lineVisual.Offset;
                        offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftBeforeChange);
                        lineVisual.Offset = offset;
                    }
                }

                // Skip not changed lines
                // Remove changed lines
                visualChildren.RemoveRange(textDetails.cLinesBeforeChange, textDetails.cLinesChanged - textDetails.dcLinesChanged);
                // Add new lines
                for (int index = textDetails.cLinesBeforeChange; index < textDetails.cLinesBeforeChange + textDetails.cLinesChanged; index++)
                {
                    PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];

                    int visualIndex;
                    VisualCollection lineVisuals;
                    if (lineDesc.cElements == 1)
                    {
                        visualIndex = index;
                        lineVisuals = visualChildren;
                    }
                    else
                    {
                        visualIndex = 0;
                        ParagraphElementVisual lineVisual = new ParagraphElementVisual();
                        visualChildren.Add(lineVisual);
                        lineVisuals = lineVisual.Children;
                    }

                    // Get list of line elements.
                    PTS.FSLINEELEMENT [] arrayLineElement;
                    PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                    for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                    {
                        PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                        // Create and format line
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }

                        TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                        // Create and validate line's visual
                        ContainerVisual lineVisual = line.CreateVisual();
                        lineVisuals.Insert(visualIndex + elIndex, lineVisual);
                        lineVisual.Offset = new Vector(TextDpi.FromTextDpi(element.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

                        // Dispose the line
                        line.Dispose();
                    }
                }
                // Shift remaining lines
                for (int index = textDetails.cLinesBeforeChange + textDetails.cLinesChanged; index < arrayLineDesc.Length; index++)
                {
                    ContainerVisual lineVisual = (ContainerVisual)visualChildren[index];

                    Vector offset = lineVisual.Offset;
                    offset.Y += TextDpi.FromTextDpi(textDetails.dvrShiftAfterChange);
                    lineVisual.Offset = offset;
                }
            }
        }

        // ------------------------------------------------------------------
        // Render floaters and figures.
        // ------------------------------------------------------------------
        private void ValidateVisualFloatersAndFigures(
            PTS.FSKUPDATE fskupdInherited,
            int cAttachedObjects)
        {
            int index;
            PTS.FSKUPDATE fskupd;

            if (cAttachedObjects > 0)
            {
                BaseParaClient paraClient;

                // Get list of floaters
                PTS.FSATTACHEDOBJECTDESCRIPTION [] arrayAttachedObjectDesc;
                PtsHelper.AttachedObjectListFromParagraph(PtsContext, _paraHandle.Value, cAttachedObjects,
                                                          out arrayAttachedObjectDesc);

                // Render floaters. For each floater do following:
                // (1) Retrieve ParaClient object
                // (2) Validate visual, if necessary
                for (index = 0; index < arrayAttachedObjectDesc.Length; index++)
                {
                    PTS.FSATTACHEDOBJECTDESCRIPTION attachedObjectDesc = arrayAttachedObjectDesc[index];

                    paraClient = PtsContext.HandleToObject(attachedObjectDesc.pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);

                    fskupd = attachedObjectDesc.fsupdinf.fskupd;
                    if (fskupd == PTS.FSKUPDATE.fskupdInherited)
                    {
                        fskupd = fskupdInherited;
                    }

                    if(fskupd != PTS.FSKUPDATE.fskupdNoChange)
                    {
                        paraClient.ValidateVisual(fskupd);
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Hittest simple lines.
        // ------------------------------------------------------------------
        private IInputElement InputHitTestSimpleLines(
            PTS.FSPOINT pt,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            IInputElement ie = null;
            int cpTextParaStart = Paragraph.ParagraphStartCharacterPosition;

            // Get list of complex lines.
            PTS.FSLINEDESCRIPTIONSINGLE [] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Find affected line by looking at vertical offset
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];

                if (lineDesc.vrStart + lineDesc.dvrAscent + lineDesc.dvrDescent > pt.v)
                {
                    // Create and format line
                    Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);
                    Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                    if(IsOptimalParagraph)
                    {
                        ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
                    }


                    using (line)
                    {
                        TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

                        // Assert that number of characters in Text line is the same as our expected length
                        Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                        if (lineDesc.urStart + line.CalculateUOffsetShift() <= pt.u && pt.u <= (lineDesc.urStart + line.CalculateUOffsetShift() + lineDesc.dur))
                        {
                            int distance = pt.u - lineDesc.urStart;

                            // Assert that number of characters in Text line is the same as our expected length
                            Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

                            if ((line.Start <= distance) && (distance <= (line.Start + line.Width)))
                            {
                                ie = line.InputHitTest(distance);
                            }
                        }
                    }
                    break;
                }
            }
            return ie;
        }

        // ------------------------------------------------------------------
        // We don't support deferred visuals in finite case, composite lines case, or figures/floaters/inline objects case
        // ------------------------------------------------------------------
        private bool IsDeferredVisualCreationSupported(ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            if(!Paragraph.StructuralCache.IsDeferredVisualCreationSupported)
                return false;

            if(PTS.ToBoolean(textDetails.fLinesComposite))
                return false;

            if(TextParagraph.HasFiguresFloatersOrInlineObjects())
                return false;


            return true;
        }

        // ------------------------------------------------------------------
        // GetRectangles in simple lines
        // ------------------------------------------------------------------
        private List<Rect> GetRectanglesInSimpleLines(
            ContentElement e,
            int start,
            int length,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            List<Rect> rectangles = new List<Rect>();

            // Calculate local element start by subtracting TextPara start relative to TextContainer
            // from element start
            int localStart = start - Paragraph.ParagraphStartCharacterPosition;

            if (localStart < 0 || textDetails.cLines == 0)
            {
                // May happen in case of figures, floaters
                return rectangles;
            }

            // Get list of complex lines.
            PTS.FSLINEDESCRIPTIONSINGLE[] arrayLineDesc;
            PtsHelper.LineListSimpleFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Find affected line by looking at vertical offset
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONSINGLE lineDesc = arrayLineDesc[index];
                List<Rect> lineRectangles = GetRectanglesInSingleLine(lineDesc, e, localStart, length);
                Invariant.Assert(lineRectangles != null);
                if (lineRectangles.Count != 0)
                {
                    rectangles.AddRange(lineRectangles);
                }
            }
            return rectangles;
        }

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles (at msot one) for the given ContentElement
        // e if it spans all or part of the specified line
        // ------------------------------------------------------------------
        private List<Rect> GetRectanglesInSingleLine(
            PTS.FSLINEDESCRIPTIONSINGLE lineDesc,
            ContentElement e,
            int start,
            int length)
        {
            // Calculate end of element relative to TextParagraph by adding length to start position
            int end = start + length;
            List<Rect> rectangles = new List<Rect>();

            // If the element does not lie in the line at all, return empty list
            if (start >= lineDesc.dcpLim)
            {
                // Element starts after line ends
                return rectangles;
            }
            if (end <= lineDesc.dcpFirst)
            {
                // Element ends before line starts
                return rectangles;
            }

            // Establish start and end points of element span within the line so that
            // we can get rectangle between them
            int localStart = (start < lineDesc.dcpFirst) ? lineDesc.dcpFirst : start;
            int localEnd = (end < lineDesc.dcpLim) ? end : lineDesc.dcpLim;
            Debug.Assert(localEnd > localStart);

            // Create and format line
            Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
            Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(lineDesc.fClearOnLeft), PTS.ToBoolean(lineDesc.fClearOnRight), TextParagraph.TextRunCache);

            if(IsOptimalParagraph)
            {
                ctx.LineFormatLengthTarget = lineDesc.dcpLim - lineDesc.dcpFirst;
            }

            TextParagraph.FormatLineCore(line, lineDesc.pfsbreakreclineclient, ctx, lineDesc.dcpFirst, lineDesc.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), lineDesc.dcpFirst);

            // Assert that number of characters in Text line is the same as our expected length
            Invariant.Assert(line.SafeLength == lineDesc.dcpLim - lineDesc.dcpFirst, "Line length is out of sync");

            // Get rectangles from start and end positions of range
            rectangles = line.GetRangeBounds(localStart, localEnd - localStart, TextDpi.FromTextDpi(lineDesc.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

            // Rectangles must have at least one element
            Invariant.Assert(rectangles.Count > 0);

            // Dispose the line
            line.Dispose();

            return rectangles;
        }

        // ------------------------------------------------------------------
        // Hittest composite lines.
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        private IInputElement InputHitTestCompositeLines(
            PTS.FSPOINT pt,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            if (textDetails.cLines == 0)
                return null;

            IInputElement ie = null;
            int cpTextParaStart = Paragraph.ParagraphStartCharacterPosition;

            // Get list of complex lines.
            PTS.FSLINEDESCRIPTIONCOMPOSITE [] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Find affected composite line by looking at vertical offset
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) continue;

                if (lineDesc.vrStart + lineDesc.dvrAscent + lineDesc.dvrDescent > pt.v)
                {
                    // Affected composite line has been found.

                    // Get list of line elements.
                    PTS.FSLINEELEMENT [] arrayLineElement;
                    PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

                    for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
                    {
                        PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                        // Create and format line
                        Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);
                        Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, cpTextParaStart);

                        if(IsOptimalParagraph)
                        {
                            ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                        }


                        using (line)
                        {
                            TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                            // Assert that number of characters in Text line is the same as our expected length
                            Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                            if (element.urStart + line.CalculateUOffsetShift() <= pt.u && pt.u <= (element.urStart + line.CalculateUOffsetShift() + element.dur))
                            {
                                int distance = pt.u - element.urStart;

                                // Assert that number of characters in Text line is the same as our expected length
                                Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                                // Assert that number of characters in Text line is the same as our expected length
                                Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                                if ((line.Start <= distance) && (distance <= (line.Start + line.Width)))
                                {
                                    ie = line.InputHitTest(distance);
                                    break;
                                }
                            }
                        }
}

                    break;
                }
            }

            return ie;
        }

        // ------------------------------------------------------------------
        // Get Rectangles in composite lines.
        // ------------------------------------------------------------------
        private List<Rect> GetRectanglesInCompositeLines(
            ContentElement e,
            int start,
            int length,
            ref PTS.FSTEXTDETAILSFULL textDetails)
        {
            ErrorHandler.Assert(!PTS.ToBoolean(textDetails.fDropCapPresent), ErrorHandler.NotSupportedDropCap);

            List<Rect> rectangles = new List<Rect>();
            int localStart = start - Paragraph.ParagraphStartCharacterPosition;

            if (localStart < 0 || textDetails.cLines == 0)
            {
                // May happen in case of figures, floaters
                return rectangles;
            }

            // Get list of complex lines.
            PTS.FSLINEDESCRIPTIONCOMPOSITE[] arrayLineDesc;
            PtsHelper.LineListCompositeFromTextPara(PtsContext, _paraHandle.Value, ref textDetails, out arrayLineDesc);

            // Find affected composite line by looking at vertical offset
            for (int index = 0; index < arrayLineDesc.Length; index++)
            {
                PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc = arrayLineDesc[index];
                if (lineDesc.cElements == 0) continue;

                List<Rect> lineRectangles = GetRectanglesInCompositeLine(lineDesc, e, localStart, length);
                Invariant.Assert(lineRectangles != null);

                if (lineRectangles.Count != 0)
                {
                    rectangles.AddRange(lineRectangles);
                }
            }

            return rectangles;
        }

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles (at msot one) for the given ContentElement
        // e if it spans all or part of the specified line
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        private List<Rect> GetRectanglesInCompositeLine(
            PTS.FSLINEDESCRIPTIONCOMPOSITE lineDesc,
            ContentElement e,
            int start,
            int length)
        {
            List<Rect> rectangles = new List<Rect>();
            int end = start + length;

            // If the element does not lie in the line at all, return empty list
            //if (start > lineDesc.dcpLim)
            //{
            //    // Element starts after line ends
            //    return rectangles;
            //}
            //if (end < lineDesc.dcpFirst)
            //{
                // Element ends before line starts
            //    return rectangles;
            //}

            // Get list of line elements.
            PTS.FSLINEELEMENT[] arrayLineElement;
            PtsHelper.LineElementListFromCompositeLine(PtsContext, ref lineDesc, out arrayLineElement);

            for (int elIndex = 0; elIndex < arrayLineElement.Length; elIndex++)
            {
                PTS.FSLINEELEMENT element = arrayLineElement[elIndex];

                // Check if element we are looking for does not span the current element at all
                if (start >= element.dcpLim)
                {
                    // Element starts after other element ends
                    continue;
                }
                if (end <= element.dcpFirst)
                {
                    // Element ends before line starts
                    continue;
                }
                // Establish start and end points of element span within the line so that
                // we can get rectangle between them
                int localStart = (start < element.dcpFirst) ? element.dcpFirst : start;
                int localEnd = (end < element.dcpLim) ? end : element.dcpLim;
                Debug.Assert(localEnd > localStart);

                Line line = new Line(Paragraph.StructuralCache.TextFormatterHost, this, Paragraph.ParagraphStartCharacterPosition);
                Line.FormattingContext ctx = new Line.FormattingContext(false, PTS.ToBoolean(element.fClearOnLeft), PTS.ToBoolean(element.fClearOnRight), TextParagraph.TextRunCache);

                if(IsOptimalParagraph)
                {
                    ctx.LineFormatLengthTarget = element.dcpLim - element.dcpFirst;
                }

                TextParagraph.FormatLineCore(line, element.pfsbreakreclineclient, ctx, element.dcpFirst, element.dur, PTS.ToBoolean(lineDesc.fTreatedAsFirst), element.dcpFirst);

                // Assert that number of characters in Text line is the same as our expected length
                Invariant.Assert(line.SafeLength == element.dcpLim - element.dcpFirst, "Line length is out of sync");

                // Get rectangles from start and end positions of range for this element
                List<Rect> elementRectangles = line.GetRangeBounds(localStart, localEnd - localStart, TextDpi.FromTextDpi(element.urStart), TextDpi.FromTextDpi(lineDesc.vrStart));

                // Rectangles must have at least one element
                Invariant.Assert(elementRectangles.Count > 0);

                // Add rectangles from this element to rectangles from whole line
                rectangles.AddRange(elementRectangles);

                // Dispose the line
                line.Dispose();
            }
            return rectangles;
        }

        #endregion Private Methods

        #region Private Properties

        private bool IsOptimalParagraph { get { return TextParagraph.IsOptimalParagraph; } }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private int _lineIndexFirstVisual = -1;

        #endregion Private Fields
    }
}

