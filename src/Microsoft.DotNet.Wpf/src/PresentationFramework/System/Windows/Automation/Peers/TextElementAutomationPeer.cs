// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with TextElements.
//

using System.Collections;
using System.Collections.Generic;           // List<T>
using System.Security;                      // SecurityCritical
using System.Windows.Documents;             // ITextContainer
using System.Windows.Media;                 // Geometry
using System.Windows.Interop;               // HwndSource
using MS.Internal.Automation;               // TextAdaptor
using MS.Internal;
using MS.Internal.Documents;                // TextContainerHelper

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with TextElements.
    /// </summary>
    public class TextElementAutomationPeer : ContentTextAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public TextElementAutomationPeer(TextElement owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// Since DocumentAutomationPeer gives access to its content through 
        /// TextPattern, this method always returns null.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            TextElement textElement = (TextElement)Owner;
            return TextContainerHelper.GetAutomationPeersFromRange(textElement.ContentStart, textElement.ContentEnd, null);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetBoundingRectangleCore"/>
        /// </summary>
        protected override Rect GetBoundingRectangleCore()
        {
            TextElement textElement = (TextElement)Owner;
            ITextView textView = textElement.TextContainer.TextView;

            if (textView == null || !textView.IsValid)
            {
                return Rect.Empty;
            }

            Geometry geometry = textView.GetTightBoundingGeometryFromTextPositions(textElement.ContentStart, textElement.ContentEnd);

            if (geometry != null)
            {
                PresentationSource presentationSource = PresentationSource.CriticalFromVisual(textView.RenderScope);

                if (presentationSource == null)
                {
                    return Rect.Empty;
                }

                HwndSource hwndSource = presentationSource as HwndSource;

                // If the source isn't an HwnSource, there's not much we can do, return empty rect
                if (hwndSource == null)
                {
                    return Rect.Empty;
                }

                Rect rectElement = geometry.Bounds;
                Rect rectRoot = PointUtil.ElementToRoot(rectElement, textView.RenderScope, presentationSource);
                Rect rectClient = PointUtil.RootToClient(rectRoot, presentationSource);
                Rect rectScreen = PointUtil.ClientToScreen(rectClient, hwndSource);

                return rectScreen;
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClickablePointCore"/>
        /// </summary>
        protected override Point GetClickablePointCore()
        {
            Point pt = new Point();

            TextElement textElement = (TextElement)Owner;
            ITextView textView = textElement.TextContainer.TextView;
            if (textView == null || !textView.IsValid || (!textView.Contains(textElement.ContentStart) && !textView.Contains(textElement.ContentEnd)))
            {
                return pt;
            }

            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(textView.RenderScope);

            if (presentationSource == null)
            {
                return pt;
            }

            HwndSource hwndSource = presentationSource as HwndSource;

            // If the source isn't an HwnSource, there's not much we can do, return empty rect
            if (hwndSource == null)
            {
                return pt;
            }

            TextPointer endPosition = textElement.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
            if (endPosition == null || endPosition.CompareTo(textElement.ContentEnd) > 0)
                endPosition = textElement.ContentEnd;

            Rect rectElement = CalculateVisibleRect(textView, textElement, textElement.ContentStart, endPosition);
            Rect rectRoot = PointUtil.ElementToRoot(rectElement, textView.RenderScope, presentationSource);
            Rect rectClient = PointUtil.RootToClient(rectRoot, presentationSource);
            Rect rectScreen = PointUtil.ClientToScreen(rectClient, hwndSource);

            pt = new Point(rectScreen.Left + rectScreen.Width * 0.5, rectScreen.Top + rectScreen.Height * 0.5);

            return pt;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsOffscreenCore"/>
        /// </summary>
        /// <returns></returns>
        protected override bool IsOffscreenCore()
        {
            IsOffscreenBehavior behavior = AutomationProperties.GetIsOffscreenBehavior(Owner);

            switch (behavior)
            {
                case IsOffscreenBehavior.Onscreen :
                    return false;

                case IsOffscreenBehavior.Offscreen :
                    return true;

                default:
                    {
                        TextElement textElement = (TextElement)Owner;
                        ITextView textView = textElement.TextContainer.TextView;
                        if (textView == null || !textView.IsValid || (!textView.Contains(textElement.ContentStart) && !textView.Contains(textElement.ContentEnd)))
                        {
                            return true;
                        }
                        
                        if (CalculateVisibleRect(textView, textElement, textElement.ContentStart, textElement.ContentEnd) == Rect.Empty)
                        {
                            return true;
                        }
                        
                        return false;
                    }
            }
        }

        /// <summary>
        /// Compute visible rectangle.
        /// </summary>
        private Rect CalculateVisibleRect(ITextView textView, TextElement textElement, TextPointer startPointer, TextPointer endPointer)
        {
            Geometry geometry = textView.GetTightBoundingGeometryFromTextPositions(startPointer, endPointer);
            Rect visibleRect = (geometry != null) ? geometry.Bounds : Rect.Empty;
            Visual visual = textView.RenderScope;
            while (visual != null && visibleRect != Rect.Empty)
            {
                if (VisualTreeHelper.GetClip(visual) != null)
                {
                    GeneralTransform transform = textView.RenderScope.TransformToAncestor(visual).Inverse;

                    // Safer version of transform to descendent (doing the inverse ourself), 
                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                    if (transform != null)
                    {
                        Rect rectBounds = VisualTreeHelper.GetClip(visual).Bounds;
                        rectBounds = transform.TransformBounds(rectBounds);

                        visibleRect.Intersect(rectBounds);
                    }
                    else
                    {
                        // No visibility if non-invertable transform exists.
                        return Rect.Empty;
                    }
                }

                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }

            return visibleRect;
        }

        /// <summary>
        /// Gets collection of AutomationPeers for given text range.
        /// </summary>
        internal override List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end)
        {
            // Force children connection to automation tree.
            GetChildren();

            TextElement textElement = (TextElement)Owner;
            return TextContainerHelper.GetAutomationPeersFromRange(start, end, textElement.ContentStart);
        }

        /// <summary>
        /// Gets the visibility of the TextElement owner based on its TextView
        /// </summary>
        internal bool? IsTextViewVisible
        {
            get
            {
                TextElement textElement = (TextElement)Owner;
                ITextView textView = textElement?.TextContainer?.TextView;
                UIElement uiElement = textView?.RenderScope;
                return uiElement?.IsVisible;
            }
        }
    }
}
