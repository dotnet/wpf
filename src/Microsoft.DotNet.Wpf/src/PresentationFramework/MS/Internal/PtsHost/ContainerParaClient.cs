// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: ContainerParaClient is responsible for handling display
//              related data of paragraph containers.
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
    /// <summary>
    /// ContainerParaClient is responsible for handling display related data 
    /// of paragraph containers. 
    /// </summary>
    internal class ContainerParaClient : BaseParaClient
    {
        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="paragraph">
        /// Paragraph associated with this object.
        /// </param>
        internal ContainerParaClient(ContainerParagraph paragraph) : base(paragraph)
        {
        }

        /// <summary>
        /// Arrange paragraph.
        /// </summary>
        protected override void OnArrange()
        {
            base.OnArrange();

            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // Adjust rectangle and offset to take into account MBPs
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ParentFlowDirection != PageFlowDirection)
            {
                mbp.MirrorMargin();
            }

            _rect.u += mbp.MarginLeft;
            _rect.du -= mbp.MarginLeft + mbp.MarginRight;

            _rect.du = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.du); 
            _rect.dv = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.dv); 

            uint fswdirSubtrack = PTS.FlowDirectionToFswdir(_flowDirection);

            // There is possibility to get empty track.
            if (subtrackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                PtsHelper.ArrangeParaList(PtsContext, subtrackDetails.fsrc, arrayParaDesc, fswdirSubtrack);
            }
        }

        /// <summary>
        /// Hit tests to the correct IInputElement within the paragraph
        /// that the mouse is over.
        /// </summary>
        /// <param name="pt">
        /// Point which gives location of Hit test
        /// </param>
        internal override IInputElement InputHitTest(PTS.FSPOINT pt)
        {
            IInputElement ie = null;

            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // Hittest subtrack content.

            // There might be possibility to get empty sub-track, skip the sub-track
            // in such case.
            if (subtrackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                // Render list of paragraphs
                ie = PtsHelper.InputHitTestParaList(PtsContext, pt, ref subtrackDetails.fsrc, arrayParaDesc);
            }

            // If nothing is hit, return the owner of the paragraph.
            if (ie == null && _rect.Contains(pt))
            {
                ie = Paragraph.Element as IInputElement;
            }

            return ie;
        }

        /// <summary>
        /// Returns ArrayList of rectangles for the given ContentElement e if 
        /// e lies in this client. Otherwise returns empty list. 
        /// </summary>
        /// <param name="e">
        /// ContentElement for which rectangles are needed
        /// </param>
        /// <param name="start">
        /// Int representing start offset of e.
        /// </param>
        /// <param name="length">
        /// int representing number of positions occupied by e.
        /// </param>
        internal override List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            List<Rect> rectangles = new List<Rect>();

            if (this.Paragraph.Element as ContentElement == e)
            {
                // We have found the element. Return rectangles for this paragraph.
                GetRectanglesForParagraphElement(out rectangles);
            }
            else
            {
                // Element not found as Paragraph.Element. Look inside

                // Query paragraph details
                PTS.FSSUBTRACKDETAILS subtrackDetails;
                PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

                // There might be possibility to get empty sub-track, skip the sub-track
                // in such case.
                if (subtrackDetails.cParas != 0)
                {
                    // Get list of paragraphs
                    // No changes to offset, since there are no subpages generated, only lists of paragraphs
                    PTS.FSPARADESCRIPTION[] arrayParaDesc;
                    PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                    // Render list of paragraphs
                    rectangles = PtsHelper.GetRectanglesInParaList(PtsContext, e, start, length, arrayParaDesc);
                }
                else
                {
                    rectangles = new List<Rect>();
                }
            }

            // Rectangles must be non-null
            Invariant.Assert(rectangles != null);    
            return rectangles;
        }

        /// <summary>
        /// Validate visual node associated with paragraph.
        /// </summary>
        /// <param name="fskupdInherited">
        /// Inherited update info
        /// </param>
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // Draw border and background info.

            // Adjust rectangle and offset to take into account MBPs
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ThisFlowDirection != PageFlowDirection)
            {
                mbp.MirrorBP();
            }

            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);
            _visual.DrawBackgroundAndBorder(backgroundBrush, mbp.BorderBrush, mbp.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);

            // There might be possibility to get empty sub-track, skip the sub-track in such case.
            if (subtrackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                // Render list of paragraphs
                PtsHelper.UpdateParaListVisuals(PtsContext, _visual.Children, fskupdInherited, arrayParaDesc);
            }
            else
            {
                _visual.Children.Clear();
            }
        }

        /// <summary>
        /// Updates the viewport for this para
        /// </summary>
        /// <param name="viewport">
        /// Fsrect with viewport info
        /// </param>
        internal override void UpdateViewport(ref PTS.FSRECT viewport)
        {
            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // There might be possibility to get empty sub-track, skip the sub-track in such case.
            if (subtrackDetails.cParas != 0)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                // Render list of paragraphs
                PtsHelper.UpdateViewportParaList(PtsContext, arrayParaDesc, ref viewport);
            }
        }

        /// <summary>
        /// Create and return paragraph result representing this paragraph.
        /// </summary>
        internal override ParagraphResult CreateParagraphResult()
        {
            /*
            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // If there is just one paragraph, do not create container. Return just this paragraph.
            if (subtrackDetails.cParas == 1)
            {
                // Get list of paragraphs
                PTS.FSPARADESCRIPTION [] arrayParaDesc;
                PtsHelper.ParaListFromSubtrack(_paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                BaseParaClient paraClient = PtsContext.HandleToObject(arrayParaDesc[0].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                return paraClient.CreateParagraphResult();
            }
            */
            return new ContainerParagraphResult(this);
        }

        /// <summary>
        /// Return TextContentRange for the content of the paragraph.
        /// </summary>
        internal override TextContentRange GetTextContentRange()
        {
            TextElement elementOwner = this.Paragraph.Element as TextElement;
            TextContentRange textContentRange;
            BaseParaClient paraClient;
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.FSPARADESCRIPTION[] arrayParaDesc;
            
            Invariant.Assert(elementOwner != null, "Expecting TextElement as owner of ContainerParagraph.");

            // Query paragraph details
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // If container is empty, return range for the entire element.
            // If the beginning and the end of content of the paragraph is 
            // part of this ParaClient, return range for the entire element.
            // Otherwise combine ranges from all nested paragraphs.
            if (subtrackDetails.cParas == 0 || (_isFirstChunk && _isLastChunk))
            {
                textContentRange = TextContainerHelper.GetTextContentRangeForTextElement(elementOwner);
            }
            else
            {
                PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

                // Merge TextContentRanges for all paragraphs
                textContentRange = new TextContentRange();
                for (int i = 0; i < arrayParaDesc.Length; i++)
                {
                    paraClient = Paragraph.StructuralCache.PtsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);
                    textContentRange.Merge(paraClient.GetTextContentRange());
                }

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
            }

            Invariant.Assert(textContentRange != null);
            return textContentRange;
        }

        /// <summary>
        /// Returns a new colleciton of ParagraphResults for the contained paragraphs.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> GetChildrenParagraphResults(out bool hasTextContent)
        {
#if TEXTPANELLAYOUTDEBUG
            TextPanelDebug.IncrementCounter("ContainerPara.GetParagraphs", TextPanelDebug.Category.TextView);
#endif
            // Query paragraph details
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            // hasTextContent is set to true if any of the children paragraphs has text content, not just attached objects
            hasTextContent = false;

            if (subtrackDetails.cParas == 0) 
            {
                return new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));
            }

            // Get list of paragraphs
            PTS.FSPARADESCRIPTION [] arrayParaDesc;
            PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

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

        /// <summary>
        /// Update information about first/last chunk.
        /// </summary>
        /// <param name="isFirstChunk">
        /// True if para is first chunk of paginated content
        /// </param>
        /// <param name="isLastChunk">
        /// True if para is last chunk of paginated content
        /// </param>
        internal void SetChunkInfo(bool isFirstChunk, bool isLastChunk)
        {
            _isFirstChunk = isFirstChunk;
            _isLastChunk = isLastChunk;
        }

        /// <summary>
        /// Returns baseline for first text line
        /// </summary>
        internal override int GetFirstTextLineBaseline()
        {
            PTS.FSSUBTRACKDETAILS subtrackDetails;
            PTS.Validate(PTS.FsQuerySubtrackDetails(PtsContext.Context, _paraHandle.Value, out subtrackDetails));

            if (subtrackDetails.cParas == 0) 
            {
                return _rect.v;
            }

            // Get list of paragraphs
            PTS.FSPARADESCRIPTION [] arrayParaDesc;
            PtsHelper.ParaListFromSubtrack(PtsContext, _paraHandle.Value, ref subtrackDetails, out arrayParaDesc);

            BaseParaClient paraClient = PtsContext.HandleToObject(arrayParaDesc[0].pfsparaclient) as BaseParaClient;
            PTS.ValidateHandle(paraClient);

            return paraClient.GetFirstTextLineBaseline();
        }

        /// <summary>
        /// Returns tight bounding path geometry.
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            bool hasTextContent;
            ReadOnlyCollection<ParagraphResult> paragraphs = GetChildrenParagraphResults(out hasTextContent);
            Invariant.Assert(paragraphs != null, "Paragraph collection is null.");

            if (paragraphs.Count > 0)
            {
                return (TextDocumentView.GetTightBoundingGeometryFromTextPositionsHelper(paragraphs, startPosition, endPosition, TextDpi.FromTextDpi(_dvrTopSpace), visibleRect));
            }

            return null;
        }

        /// <summary>
        /// Is this the first chunk of paginated content.
        /// </summary>
        internal override bool IsFirstChunk 
        { 
            get 
            { 
                return _isFirstChunk; 
            } 
        }
        private bool _isFirstChunk;

        /// <summary>
        /// Is this the last chunk of paginated content.
        /// </summary>
        internal override bool IsLastChunk 
        { 
            get 
            { 
                return _isLastChunk; 
            } 
        }
        private bool _isLastChunk;
    }
}
