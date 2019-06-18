// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: UIElementParaClient class is responsible for handling display
//              related data of BlockUIContainers.
//

using System;                                   // IntPtr
using System.Collections.Generic;               // List<T>
using System.Collections.ObjectModel;           // ReadOnlyCollection<T>
using System.Security;                          // SecurityCritical
using System.Windows;                           // FrameworkElement             
using System.Windows.Media;                     // Visual
using System.Windows.Documents;                 // BlockUIContainer
using MS.Internal.Documents;                    // ParagraphResult, UIElementIsland
using MS.Internal.Text;                         // TextDpi
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// FloaterParaClient class is responsible for handling display related
    /// data of paragraphs associated with nested floaters.
    /// </summary>
    internal sealed class UIElementParaClient : FloaterBaseParaClient
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
        /// <param name="paragraph"></param>
        internal UIElementParaClient(FloaterBaseParagraph paragraph)
            : base(paragraph)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Arrange paragraph.
        /// </summary>
        protected override void OnArrange()
        {
            base.OnArrange();

            PTS.FSFLOATERDETAILS floaterDetails;
            PTS.Validate(PTS.FsQueryFloaterDetails(PtsContext.Context, _paraHandle.Value, out floaterDetails));

            // Get paragraph's rectangle.
            _rect = floaterDetails.fsrcFloater;

            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
            if(ParentFlowDirection != PageFlowDirection)
            {
                mbp.MirrorMargin();

                PTS.FSRECT pageRect = _pageContext.PageRect;
                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ParentFlowDirection), ref pageRect, ref _rect, PTS.FlowDirectionToFswdir(PageFlowDirection), out _rect));
            }

            _rect.u += mbp.MarginLeft;
            _rect.du -= mbp.MarginLeft + mbp.MarginRight;
            _rect.du = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.du);
            _rect.dv = Math.Max(TextDpi.ToTextDpi(TextDpi.MinWidth), _rect.dv);
        }

        /// <summary>
        /// Returns collection of rectangles for the BlockUIContainer element. 
        /// If the element is not the paragraph's owner, empty collection is returned.
        /// </summary>
        internal override List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            List<Rect> rectangles = new List<Rect>();
            if (Paragraph.Element == e)
            {
                // We have found the element. Return rectangles for this paragraph.
                GetRectanglesForParagraphElement(out rectangles);
            }
            return rectangles;
        }

        /// <summary>
        /// Validates visual node associated with paragraph.
        /// </summary>
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            // Obtain all mbd info
            MbpInfo mbp = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            // MIRROR entire element to interface with underlying layout tree. 
            // Border/Padding does not need to be mirrored, as it'll be mirrored with the content.
            PtsHelper.UpdateMirroringTransform(PageFlowDirection, ThisFlowDirection, _visual, TextDpi.FromTextDpi(2 * _rect.u + _rect.du));

            // Add UIElementIsland to visual tree and set appropiate offset.
            UIElementIsland uiElementIsland = ((UIElementParagraph)Paragraph).UIElementIsland;
            if (uiElementIsland != null)
            {
                if (_visual.Children.Count != 1 || _visual.Children[0] != uiElementIsland)
                {
                    // Disconnect UIElementIsland from its old parent.
                    Visual currentParent = VisualTreeHelper.GetParent(uiElementIsland) as Visual;
                    if (currentParent != null)
                    {
                        ContainerVisual parent = currentParent as ContainerVisual;
                        Invariant.Assert(parent != null, "Parent should always derives from ContainerVisual.");
                        parent.Children.Remove(uiElementIsland);
                    }                           

                    _visual.Children.Clear();
                    _visual.Children.Add(uiElementIsland);
                }
                uiElementIsland.Offset = new PTS.FSVECTOR(_rect.u + mbp.BPLeft, _rect.v + mbp.BPTop).FromTextDpi();
            }
            else
            {
                _visual.Children.Clear();
            }

            // Draw background and borders.
            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);
            _visual.DrawBackgroundAndBorder(backgroundBrush, mbp.BorderBrush, mbp.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);
        }

        /// <summary>
        /// Returns tight bounding path geometry.
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            if (startPosition.CompareTo(((BlockUIContainer)Paragraph.Element).ContentEnd) < 0 && 
                endPosition.CompareTo(((BlockUIContainer)Paragraph.Element).ContentStart) > 0)
            {
                return new RectangleGeometry(_rect.FromTextDpi());
            }
            return null;
        }

        /// <summary>
        /// Creates paragraph result representing this paragraph.
        /// </summary>
        /// <returns></returns>
        internal override ParagraphResult CreateParagraphResult()
        {
            return new UIElementParagraphResult(this);
        }

        /// <summary>
        /// Hit tests to the correct IInputElement within the paragraph that 
        /// the mouse is over.
        /// </summary>
        internal override IInputElement InputHitTest(PTS.FSPOINT pt)
        {
            if (_rect.Contains(pt))
            {
                return Paragraph.Element as IInputElement;
            }
            return null;
        }

        /// <summary>
        /// Returns TextContentRange for the content of the paragraph.
        /// </summary>
        internal override TextContentRange GetTextContentRange()
        {
            BlockUIContainer elementOwner = (BlockUIContainer)Paragraph.Element;
            return TextContainerHelper.GetTextContentRangeForTextElement(elementOwner);
        }

        #endregion Internal Methods
    }
}

