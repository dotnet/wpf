// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Text Object Models Text pattern provider
// Spec for TextPattern at TextPatternSpecM8.doc
// Spec for Text Object Model (TOM) at Text Object Model.doc
//

using System;                               // Exception
using System.Collections.Generic;           // List<T>
using System.Collections.ObjectModel;       // ReadOnlyCollection
using System.Security;                      // SecurityCritical, ...
using System.Windows;                       // PresentationSource
using System.Windows.Automation;            // SupportedTextSelection
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Automation.Provider;   // ITextProvider
using System.Windows.Controls.Primitives;   // IScrollInfo
using System.Windows.Documents;             // ITextContainer
using System.Windows.Media;                 // Visual
using MS.Internal.Documents;                // MultiPageTextView

namespace MS.Internal.Automation
{
    /// <summary>
    /// Represents a text provider that supports the text pattern across Text Object
    /// Model based Text Controls.
    /// </summary>
    internal class TextAdaptor : ITextProvider, IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="textPeer">Automation Peer representing element for the ui scope of the text</param>
        /// <param name="textContainer">ITextContainer</param>
        internal TextAdaptor(AutomationPeer textPeer, ITextContainer textContainer)
        {
            Invariant.Assert(textContainer != null, "Invalid ITextContainer");
            Invariant.Assert(textPeer is TextAutomationPeer || textPeer is ContentTextAutomationPeer, "Invalid AutomationPeer");
            _textPeer = textPeer;
            _textContainer = textContainer;
            _textContainer.Changed += new TextContainerChangedEventHandler(OnTextContainerChanged);
            if (_textContainer.TextSelection != null)
            {
                _textContainer.TextSelection.Changed += new EventHandler(OnTextSelectionChanged);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_textContainer != null && _textContainer.TextSelection != null)
            {
                _textContainer.TextSelection.Changed -= new EventHandler(OnTextSelectionChanged);
            }
            GC.SuppressFinalize(this);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves the bounding rectangles for the text lines of a given range.  
        /// </summary>
        /// <param name="start">Start of range to measure</param>
        /// <param name="end">End of range to measure</param>
        /// <param name="clipToView">Specifies whether the caller wants the full bounds (false) or the bounds of visible portions 
        /// of the viewable line only ('true')</param>
        /// <param name="transformToScreen">Requests the results in screen coordinates</param>
        /// <returns>An array of bounding rectangles for each line or portion of a line within the client area of the text provider.
        /// No bounding rectangles will be returned for lines that are empty or scrolled out of view.  Note that even though a
        /// bounding rectangle is returned the corresponding text may not be visible due to overlapping windows.
        /// This will not return null, but may return an empty array.</returns>
        internal Rect[] GetBoundingRectangles(ITextPointer start, ITextPointer end, bool clipToView, bool transformToScreen)
        {
            ITextView textView = GetUpdatedTextView();
            if (textView == null)
            {
                return new Rect[0];
            }

            // If start/end positions are not in the visible range, move them to the first/last visible positions.
            ReadOnlyCollection<TextSegment> textSegments = textView.TextSegments;
            if (textSegments.Count > 0)
            {
                if (!textView.Contains(start) && start.CompareTo(textSegments[0].Start) < 0)
                {
                    start = textSegments[0].Start.CreatePointer(); ;
                }
                if (!textView.Contains(end) && end.CompareTo(textSegments[textSegments.Count-1].End) > 0)
                {
                    end = textSegments[textSegments.Count - 1].End.CreatePointer();
                }
            }
            if (!textView.Contains(start) || !textView.Contains(end))
            {
                return new Rect[0];
            }

            TextRangeAdaptor.MoveToInsertionPosition(start, LogicalDirection.Forward);
            TextRangeAdaptor.MoveToInsertionPosition(end, LogicalDirection.Backward);

            Rect visibleRect = Rect.Empty;
            if (clipToView)
            {
                visibleRect = GetVisibleRectangle(textView);
                // If clipping into view and visible rect is empty, return.
                if (visibleRect.IsEmpty)
                {
                    return new Rect[0];
                }
            }

            List<Rect> rectangles = new List<Rect>();
            ITextPointer position = start.CreatePointer();
            while (position.CompareTo(end) < 0)
            {
                TextSegment lineRange = textView.GetLineRange(position);
                if (!lineRange.IsNull)
                {
                    // Since range is limited to just one line, GetTightBoundingGeometry will return tight bounding
                    // rectangle for given range. It will also work correctly with bidi text.
                    ITextPointer first = (lineRange.Start.CompareTo(start) <= 0) ? start : lineRange.Start;
                    ITextPointer last = (lineRange.End.CompareTo(end) >= 0) ? end : lineRange.End;
                    Rect lineRect = Rect.Empty;
                    Geometry geometry = textView.GetTightBoundingGeometryFromTextPositions(first, last);
                    if (geometry != null)
                    {
                        lineRect = geometry.Bounds;
                        if (clipToView)
                        {
                            lineRect.Intersect(visibleRect);
                        }
                        if (!lineRect.IsEmpty)
                        {
                            if (transformToScreen)
                            {
                                lineRect = new Rect(ClientToScreen(lineRect.TopLeft, textView.RenderScope), ClientToScreen(lineRect.BottomRight, textView.RenderScope));
                            }
                            rectangles.Add(lineRect);
                        }
                    }
                }
                if (position.MoveToLineBoundary(1) == 0)
                {
                    position = end;
                }
            }
            return rectangles.ToArray();
        }

        /// <summary>
        /// Retrieves associated TextView. If TextView is not valid, tries to update its layout.
        /// </summary>
        internal ITextView GetUpdatedTextView()
        {
            ITextView textView = _textContainer.TextView;
            if (textView != null)
            {
                if (!textView.IsValid)
                {
                    if (!textView.Validate())
                    {
                        textView = null;
                    }
                    if (textView != null && !textView.IsValid)
                    {
                        textView = null;
                    }
                }
            }
            return textView;
        }

        /// <summary>
        /// Changes text selection on the element
        /// </summary>
        /// <param name="start">Start of range to select</param>
        /// <param name="end">End of range to select</param>
        /// <remarks>Automation clients as well as the internal caller of this method (a TextRangeAdapter object) are supposed 
        /// to verify whether the provider supports text selection by calling SupportsTextSelection first. 
        /// The internal caller is responsible for raising an InvalidOperationException upon the Automation client' attempt
        /// to change selection when it's not supported by the provider</remarks>
        internal void Select(ITextPointer start, ITextPointer end)
        {
            // Update the selection range
            if (_textContainer.TextSelection != null)
            {
                _textContainer.TextSelection.Select(start, end);
            }
        }

        /// <summary>
        /// This helper method is used by TextRangeAdaptor to bring the range into view
        /// through multiple nested scroll providers.
        /// </summary>
        internal void ScrollIntoView(ITextPointer start, ITextPointer end, bool alignToTop)
        {
            // Calculate the bounding rectangle for the range
            Rect rangeBounds = Rect.Empty;
            Rect[] lineBounds = GetBoundingRectangles(start, end, false, false);
            foreach (Rect rect in lineBounds)
            {
                rangeBounds.Union(rect);
            }

            ITextView textView = GetUpdatedTextView();
            if (textView != null && !rangeBounds.IsEmpty)
            {
                // Find out the visible portion of the range.
                Rect visibleRect = GetVisibleRectangle(textView);
                Rect rangeVisibleBounds = Rect.Intersect(rangeBounds, visibleRect);
                if (rangeVisibleBounds == rangeBounds)
                {
                    // The range is already in the view. It's probably not aligned as requested, 
                    // but who cares since it's entirely visible anyway.
                    return;
                }

                // Ensure the visibility of the range. 
                // BringIntoView will do most of the magic except the very first scroll
                // in order to satisfy the requested alignment.
                UIElement renderScope = textView.RenderScope;
                Visual visual = renderScope;
                while (visual != null)
                {
                    IScrollInfo isi = visual as IScrollInfo;
                    if (isi != null)
                    {
                        // Transform the bounding rectangle into the IScrollInfo coordinates.
                        if (visual != renderScope)
                        {
                            GeneralTransform childToParent = renderScope.TransformToAncestor(visual);
                            rangeBounds = childToParent.TransformBounds(rangeBounds);
                        }

                        if (isi.CanHorizontallyScroll)
                        {
                            isi.SetHorizontalOffset(alignToTop ? rangeBounds.Left : (rangeBounds.Right - isi.ViewportWidth));
                        }
                        if (isi.CanVerticallyScroll)
                        {
                            isi.SetVerticalOffset(alignToTop ? rangeBounds.Top : (rangeBounds.Bottom - isi.ViewportHeight));
                        }
                        break;
                    }
                    visual = VisualTreeHelper.GetParent(visual) as Visual;
                }

                FrameworkElement fe = renderScope as FrameworkElement;
                if (fe != null)
                {
                    fe.BringIntoView(rangeVisibleBounds);
                }
            }
            else
            {
                // If failed to retrive range bounds, try to Bring into view closes element.
                ITextPointer pointer = alignToTop ? start.CreatePointer() : end.CreatePointer();
                pointer.MoveToElementEdge(alignToTop ? ElementEdge.AfterStart : ElementEdge.AfterEnd);
                FrameworkContentElement element = pointer.GetAdjacentElement(LogicalDirection.Backward) as FrameworkContentElement;
                if (element != null)
                {
                    element.BringIntoView();
                }
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Notify about content changes.
        /// </summary>
        private void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
        {
            _textPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextChanged);
        }

        /// <summary>
        /// Notify about selection changes.
        /// </summary>
        private void OnTextSelectionChanged(object sender, EventArgs e)
        {
            _textPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);
        }

        /// <summary>
        /// Computes the bounds of the render scope area visible through all nested scroll areas.
        /// </summary>
        private Rect GetVisibleRectangle(ITextView textView)
        {
            Rect visibleRect = new Rect(textView.RenderScope.RenderSize);
            Visual visual = VisualTreeHelper.GetParent(textView.RenderScope) as Visual;

            while (visual != null && visibleRect != Rect.Empty)
            {
                if (VisualTreeHelper.GetClip(visual) != null)
                {
                    GeneralTransform transform = textView.RenderScope.TransformToAncestor(visual).Inverse;
                    // Safer version of transform to descendent (doing the inverse ourself), 
                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with).
                    if (transform != null)
                    {
                        Rect rectBounds = VisualTreeHelper.GetClip(visual).Bounds;
                        rectBounds = transform.TransformBounds(rectBounds);
                        visibleRect.Intersect(rectBounds);
                    }
                    else
                    {
                        // No visibility if non-invertable transform exists.
                        visibleRect = Rect.Empty;
                    }
                }
                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }
            return visibleRect;
        }

        /// <summary>
        /// Convert a point from "client" coordinate space of a window into
        /// the coordinate space of the screen.
        /// </summary>
        private Point ClientToScreen(Point point, Visual visual)
        {
            if (System.Windows.AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures)
            {
                return ObsoleteClientToScreen(point, visual);
            }

            try
            {
                point = visual.PointToScreen(point);
            }
            catch (InvalidOperationException)
            {
            }

            return point;
        }

        /// <summary>
        /// A version of <see cref="ClientToScreen(Point, Visual)"/> for compatibility purposes.
        /// There is a subtle bug in this version that manifests itself in High-DPI aware applications,
        /// and this version of the method should not used be except for compatibility purposes
        /// </summary>
        private Point ObsoleteClientToScreen(Point point, Visual visual)
        {
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(visual);
            if (presentationSource != null)
            {
                GeneralTransform transform = visual.TransformToAncestor(presentationSource.RootVisual);
                if (transform != null)
                {
                    point = transform.Transform(point);
                }
            }
            return PointUtil.ClientToScreen(point, presentationSource);
        }

        /// <summary>
        /// Convert a point from the coordinate space of the screen into
        /// the "client" coordinate space of a window.
        /// </summary>
        private Point ScreenToClient(Point point, Visual visual)
        {
            if (System.Windows.AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures)
            {
                return ObsoleteScreenToClient(point, visual);
            }

            try
            {
                point = visual.PointFromScreen(point);
            }
            catch (InvalidOperationException)
            {
            }
            return point;
        }

        /// <summary>
        /// A version of <see cref="ScreenToClient(Point, Visual)"/> for compatibility purposes.
        /// There is a subtle bug in this version that manifests itself in High-DPI aware applications,
        /// and this version of the method should not be used except for compatibility purposes.
        /// </summary>
        private Point ObsoleteScreenToClient(Point point, Visual visual)
        {
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(visual);
            point = PointUtil.ScreenToClient(point, presentationSource);
            if (presentationSource != null)
            {
                GeneralTransform transform = visual.TransformToAncestor(presentationSource.RootVisual);
                if (transform != null)
                {
                    transform = transform.Inverse;
                    if (transform != null)
                    {
                        point = transform.Transform(point);
                    }
                }
            }
            return point;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private fields

        private AutomationPeer _textPeer;
        private ITextContainer _textContainer;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  ITextProvider
        //
        //-------------------------------------------------------------------

        #region ITextProvider implementation

        /// <summary>
        /// Retrieves the current selection.  For providers that have the concept of
        /// text selection the provider should implement this method and also return
        /// true for the SupportsTextSelection property below.  Otherwise this method
        /// should throw an InvalidOperation exception.
        /// For providers that support multiple disjoint selection, this should return
        /// an array of all the currently selected ranges. Providers that don't support
        /// multiple disjoint selection should just return an array containing a single
        /// range.
        /// </summary>
        /// <returns>The range of text that is selected, or possibly null if there is
        /// no selection.</returns>
        ITextRangeProvider[] ITextProvider.GetSelection()
        {
            ITextRange selection = _textContainer.TextSelection;
            if (selection == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextProvider_TextSelectionNotSupported));
            }
            return new ITextRangeProvider[] { new TextRangeAdaptor(this, selection.Start, selection.End, _textPeer) };
        }

        /// <summary>
        /// Retrieves the visible ranges of text.
        /// </summary>
        /// <returns>The ranges of text that are visible, or possibly an empty array if there is
        /// no visible text whatsoever.  Text in the range may still be obscured by an overlapping
        /// window.  Also, portions
        /// of the range at the beginning, in the middle, or at the end may not be visible
        /// because they are scrolled off to the side.
        /// Providers should ensure they return at most a range from the beginning of the first
        /// line with portions visible through the end of the last line with portions visible.</returns>
        ITextRangeProvider[] ITextProvider.GetVisibleRanges()
        {
            ITextRangeProvider[] ranges = null;
            ITextView textView = GetUpdatedTextView();
            if (textView != null)
            {
                List<TextSegment> visibleTextSegments = new List<TextSegment>();

                // Get visible portion of the document. 
                // FUTURE-2005/01/12-vsmirnov - Narrow the range by skipping partially visible 
                // rows (columns, pages) on each end. For this, we need to know the limit values
                // (percents of row_width/column_height, I guess) to decide on row/column/page visibility.
                // Also, need to define what to do with margin cases, like 2 rows (columns, pages) are 
                // in the view but none of them is visible enough.
                if (textView is MultiPageTextView)
                {
                    // For MultiPageTextView assume that all current pages are entirely visible.
                    visibleTextSegments.AddRange(textView.TextSegments);
                }
                else
                {
                    // For all others TextViews get visible rectangle and hittest TopLeft and 
                    // BottomRight points to retrieve visible range.
                    // Find out the bounds of the area visible through all nested scroll areas
                    Rect visibleRect = GetVisibleRectangle(textView);
                    if (!visibleRect.IsEmpty)
                    {
                        ITextPointer visibleStart = textView.GetTextPositionFromPoint(visibleRect.TopLeft, true);
                        ITextPointer visibleEnd = textView.GetTextPositionFromPoint(visibleRect.BottomRight, true);
                        visibleTextSegments.Add(new TextSegment(visibleStart, visibleEnd, true));
                    }
                }

                // Create collection of TextRangeProviders for visible ranges.
                if (visibleTextSegments.Count > 0)
                {
                    ranges = new ITextRangeProvider[visibleTextSegments.Count];
                    for (int i = 0; i < visibleTextSegments.Count; i++)
                    {
                        ranges[i] = new TextRangeAdaptor(this, visibleTextSegments[i].Start, visibleTextSegments[i].End, _textPeer);
                    }
                }
            }
            // If no text is visible in the control, return the degenerate text range 
            // (empty range) at the beginning of the document.
            if (ranges == null)
            {
                ranges = new ITextRangeProvider[] { new TextRangeAdaptor(this, _textContainer.Start, _textContainer.Start, _textPeer) };
            }
            return ranges;
        }

        /// <summary>
        /// Retrieves the range of a child object.
        /// </summary>
        /// <param name="childElementProvider">The child element.  A provider should check that the 
        /// passed element is a child of the text container, and should throw an 
        /// InvalidOperationException if it is not.</param>
        /// <returns>A range that spans the child element.</returns>
        ITextRangeProvider ITextProvider.RangeFromChild(IRawElementProviderSimple childElementProvider)
        {
            if (childElementProvider == null)
            {
                throw new ArgumentNullException("childElementProvider");
            }

            // Retrieve DependencyObject from AutomationElement
            DependencyObject childElement;
            if (_textPeer is TextAutomationPeer)
            {
                childElement = ((TextAutomationPeer)_textPeer).ElementFromProvider(childElementProvider);
            }
            else
            {
                childElement = ((ContentTextAutomationPeer)_textPeer).ElementFromProvider(childElementProvider);
            }

            TextRangeAdaptor range = null;
            if (childElement != null)
            {
                ITextPointer rangeStart = null;
                ITextPointer rangeEnd = null;

                // Retrieve start and end positions for given element.
                // If element is TextElement, retrieve its Element Start and End positions.
                // If element is UIElement hosted by UIContainer (Inlien of Block), 
                // retrieve content Start and End positions of the container.
                // Otherwise scan ITextContainer to find a range for given element.
                if (childElement is TextElement)
                {
                    rangeStart = ((TextElement)childElement).ElementStart;
                    rangeEnd = ((TextElement)childElement).ElementEnd;
                }
                else
                {
                    DependencyObject parent = LogicalTreeHelper.GetParent(childElement);
                    if (parent is InlineUIContainer || parent is BlockUIContainer)
                    {
                        rangeStart = ((TextElement)parent).ContentStart;
                        rangeEnd = ((TextElement)parent).ContentEnd;
                    }
                    else
                    {
                        ITextPointer position = _textContainer.Start.CreatePointer();
                        while (position.CompareTo(_textContainer.End) < 0)
                        {
                            TextPointerContext context = position.GetPointerContext(LogicalDirection.Forward);
                            if (context == TextPointerContext.ElementStart)
                            {
                                if (childElement == position.GetAdjacentElement(LogicalDirection.Forward))
                                {
                                    rangeStart = position.CreatePointer(LogicalDirection.Forward);
                                    position.MoveToElementEdge(ElementEdge.AfterEnd);
                                    rangeEnd = position.CreatePointer(LogicalDirection.Backward);
                                    break;
                                }
                            }
                            else if (context == TextPointerContext.EmbeddedElement)
                            {
                                if (childElement == position.GetAdjacentElement(LogicalDirection.Forward))
                                {
                                    rangeStart = position.CreatePointer(LogicalDirection.Forward);
                                    position.MoveToNextContextPosition(LogicalDirection.Forward);
                                    rangeEnd = position.CreatePointer(LogicalDirection.Backward);
                                    break;
                                }
                            }
                            position.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                    }
                }
                // Create range
                if (rangeStart != null && rangeEnd != null)
                {
                    range = new TextRangeAdaptor(this, rangeStart, rangeEnd, _textPeer);
                }
            }
            if (range == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextProvider_InvalidChildElement));
            }
            return range;
        }

        /// <summary>
        /// Finds the degenerate range nearest to a screen coordinate.
        /// </summary>
        /// <param name="location">The location in screen coordinates.
        /// The provider should check that the coordinates are within the client
        /// area of the provider, and should throw an InvalidOperation exception 
        /// if they are not.</param>
        /// <returns>A degenerate range nearest the specified location.</returns>
        ITextRangeProvider ITextProvider.RangeFromPoint(Point location)
        {
            TextRangeAdaptor range = null;
            ITextView textView = GetUpdatedTextView();
            if (textView != null)
            {
                // Convert the screen point to the element space coordinates.
                location = ScreenToClient(location, textView.RenderScope);
                ITextPointer position = textView.GetTextPositionFromPoint(location, true);
                if (position != null)
                {
                    range = new TextRangeAdaptor(this, position, position, _textPeer);
                }
            }
            if (range == null)
            {
                throw new ArgumentException(SR.Get(SRID.TextProvider_InvalidPoint));
            }
            return range;
        }

        /// <summary>
        /// A text range that encloses the main text of the document.  Some auxillary text such as 
        /// headers, footnotes, or annotations may not be included. 
        /// </summary>
        ITextRangeProvider ITextProvider.DocumentRange
        {
            get
            {
                return new TextRangeAdaptor(this, _textContainer.Start, _textContainer.End, _textPeer);
            }
        }

        /// <summary>
        /// True if the text container supports text selection. If the provider returns false then
        /// it should throw InvalidOperation exceptions for ITextProvider.GetSelection and 
        /// ITextRangeProvider.Select.
        /// </summary>
        SupportedTextSelection ITextProvider.SupportedTextSelection
        {
            get
            {
                return (_textContainer.TextSelection == null) ? SupportedTextSelection.None : SupportedTextSelection.Single;
            }
        }

        #endregion ITextProvider implementation
    }
}
