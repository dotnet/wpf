// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Content presenter for the TextBox.
//

namespace System.Windows.Controls
{
    using System.Windows.Documents;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Threading;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using MS.Internal;
    using MS.Internal.Telemetry.PresentationFramework;
    using MS.Internal.Text;
    using MS.Internal.Documents;
    using MS.Internal.PtsHost;
    using System.Windows.Media.TextFormatting;

    // Content presenter for the TextBox.
    internal class TextBoxView : FrameworkElement, ITextView, IScrollInfo, IServiceProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Static constructor.
        static TextBoxView()
        {
            // Set a margin so that the bidi caret has room to render at the edges of content.
            MarginProperty.OverrideMetadata(typeof(TextBoxView), new FrameworkPropertyMetadata(new Thickness(CaretElement.BidiCaretIndicatorWidth, 0, CaretElement.BidiCaretIndicatorWidth, 0)));
        }

        // Constructor.
        internal TextBoxView(ITextBoxViewHost host)
        {
            Invariant.Assert(host is Control);
            _host = host;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // IServiceProvider for TextEditor/renderscope contract.
        // Provides access to our ITextView implementation.
        object IServiceProvider.GetService(Type serviceType)
        {
            object service = null;

            if (serviceType == typeof(ITextView))
            {
                service = this;
            }

            return service;
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineUp"/>
        /// </summary>
        void IScrollInfo.LineUp()
        {
            if (_scrollData != null)
            {
                _scrollData.LineUp(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineDown"/>
        /// </summary>
        void IScrollInfo.LineDown()
        {
            if (_scrollData != null)
            {
                _scrollData.LineDown(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineLeft"/>
        /// </summary>
        void IScrollInfo.LineLeft()
        {
            if (_scrollData != null)
            {
                _scrollData.LineLeft(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineRight"/>
        /// </summary>
        void IScrollInfo.LineRight()
        {
            if (_scrollData != null)
            {
                _scrollData.LineRight(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageUp"/>
        /// </summary>
        void IScrollInfo.PageUp()
        {
            if (_scrollData != null)
            {
                _scrollData.PageUp(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageDown"/>
        /// </summary>
        void IScrollInfo.PageDown()
        {
            if (_scrollData != null)
            {
                _scrollData.PageDown(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageLeft"/>
        /// </summary>
        void IScrollInfo.PageLeft()
        {
            if (_scrollData != null)
            {
                _scrollData.PageLeft(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageRight"/>
        /// </summary>
        void IScrollInfo.PageRight()
        {
            if (_scrollData != null)
            {
                _scrollData.PageRight(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelUp"/>
        /// </summary>
        void IScrollInfo.MouseWheelUp()
        {
            if (_scrollData != null)
            {
                _scrollData.MouseWheelUp(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelDown"/>
        /// </summary>
        void IScrollInfo.MouseWheelDown()
        {
            if (_scrollData != null)
            {
                _scrollData.MouseWheelDown(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelLeft"/>
        /// </summary>
        void IScrollInfo.MouseWheelLeft()
        {
            if (_scrollData != null)
            {
                _scrollData.MouseWheelLeft(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelRight"/>
        /// </summary>
        void IScrollInfo.MouseWheelRight()
        {
            if (_scrollData != null)
            {
                _scrollData.MouseWheelRight(this);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.SetHorizontalOffset"/>
        /// </summary>
        void IScrollInfo.SetHorizontalOffset(double offset)
        {
            if (_scrollData != null)
            {
                _scrollData.SetHorizontalOffset(this, offset);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.SetVerticalOffset"/>
        /// </summary>
        void IScrollInfo.SetVerticalOffset(double offset)
        {
            if (_scrollData != null)
            {
                _scrollData.SetVerticalOffset(this, offset);
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MakeVisible"/>
        /// </summary>
        Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle)
        {
            if (_scrollData == null)
            {
                rectangle = Rect.Empty;
            }
            else
            {
                rectangle = _scrollData.MakeVisible(this, visual, rectangle);
            }

            return rectangle;
        }

        /// <summary>
        /// <see cref="IScrollInfo.CanVerticallyScroll"/>
        /// </summary>
        bool IScrollInfo.CanVerticallyScroll
        {
            get
            {
                return (_scrollData != null) ? _scrollData.CanVerticallyScroll : false;
            }
            set
            {
                if (_scrollData != null)
                {
                    _scrollData.CanVerticallyScroll = value;
                }
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.CanHorizontallyScroll"/>
        /// </summary>
        bool IScrollInfo.CanHorizontallyScroll
        {
            get
            {
                return (_scrollData != null) ? _scrollData.CanHorizontallyScroll : false;
            }
            set
            {
                if (_scrollData != null)
                {
                    _scrollData.CanHorizontallyScroll = value;
                }
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ExtentWidth"/>
        /// </summary>
        double IScrollInfo.ExtentWidth
        {
            get
            {
                double result = 0.0;

                if (_scrollData != null)
                {
                    result = _scrollData.ExtentWidth;
                    if (UseLayoutRounding)
                    {
                        // Dev 10 bug: 827316
                        // With layout rounding enabled DesiredSize.Width is rounded
                        // so the computed value of _scrollData.ExtentWidth may not agree with DesiredSize.
                        // This discrepancy causes the retry logic for auto scrollbars in ScrollViewer not to terminate.
                        // This fix applies layout rounding to the Extent so that it matches DesiredSize
                        result = RoundLayoutValue(result, GetDpi().DpiScaleX);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ExtentHeight"/>
        /// </summary>
        double IScrollInfo.ExtentHeight
        {
            get
            {
                double result = 0.0;

                if (_scrollData != null)
                {
                    result = _scrollData.ExtentHeight;
                    if (UseLayoutRounding)
                    {
                        // Dev 10 bug: 827316
                        // With layout rounding enabled DesiredSize.Width is rounded
                        // so the computed value of _scrollData.ExtentWidth may not agree with DesiredSize.
                        // This discrepancy causes the retry logic for auto scrollbars in ScrollViewer not to terminate
                        // This fix applies layout rounding to the Extent so that it matches DesiredSize
                        result = RoundLayoutValue(result, GetDpi().DpiScaleY);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ViewportWidth"/>
        /// </summary>
        double IScrollInfo.ViewportWidth
        {
            get
            {
                return (_scrollData != null) ? _scrollData.ViewportWidth : 0;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ViewportHeight"/>
        /// </summary>
        double IScrollInfo.ViewportHeight
        {
            get
            {
                return (_scrollData != null) ? _scrollData.ViewportHeight : 0;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.HorizontalOffset"/>
        /// </summary>
        double IScrollInfo.HorizontalOffset
        {
            get
            {
                return (_scrollData != null) ? _scrollData.HorizontalOffset : 0;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.VerticalOffset"/>
        /// </summary>
        double IScrollInfo.VerticalOffset
        {
            get
            {
                return (_scrollData != null) ? _scrollData.VerticalOffset : 0;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ScrollOwner"/>
        /// </summary>
        ScrollViewer IScrollInfo.ScrollOwner
        {
            get
            {
                return (_scrollData != null) ? _scrollData.ScrollOwner : null;
            }

            set
            {
                if (_scrollData == null)
                {
                    // Create cached scroll info.
                    _scrollData = new ScrollData();
                }
                _scrollData.SetScrollOwner(this, value);
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // Calculates ideal content size.
        protected override Size MeasureOverride(Size constraint)
        {
            // Lazy init TextContainer listeners on the first measure.
            EnsureTextContainerListeners();

            // Lazy allocate _lineMetrics on the first measure.
            if (_lineMetrics == null)
            {
                _lineMetrics = new List<LineRecord>(1);
            }

            Size desiredSize;

            // Init a cache we'll use here and in the following ArrangeOverride call.
            _cache = null;
            EnsureCache();

            LineProperties lineProperties = _cache.LineProperties;

            // Skip the measure if constraints have not changed.
            bool widthChanged = !DoubleUtil.AreClose(constraint.Width, _previousConstraint.Width);

            // If width changed and TextAlignment is Center or Right the visual offsets of the visible
            // lines need to be recalculated.
            if (widthChanged && lineProperties.TextAlignment != TextAlignment.Left)
            {
                _viewportLineVisuals = null;
            }

            bool constraintschanged = widthChanged &&
                                      lineProperties.TextWrapping != TextWrapping.NoWrap;

            if (_lineMetrics.Count == 0 || constraintschanged)
            {
                // Null out the dirty list when constraints change -- everything's dirty.
                _dirtyList = null;
            }
            else if (_dirtyList == null && !this.IsBackgroundLayoutPending)
            {
                // No dirty region, no constraint change, no pending background layout.
                desiredSize = _contentSize;
                goto Exit;
            }

            // Treat an insert into an empty document just like a full invalidation,
            // to allow background layout to run.
            if (_dirtyList != null &&
                _lineMetrics.Count == 1 && _lineMetrics[0].EndOffset == 0)
            {
                _lineMetrics.Clear();
                _viewportLineVisuals = null;
                _dirtyList = null;
            }

            Size safeConstraint = constraint;
            // Make sure that TextFormatter limitations are not exceeded.
            // Remove it when MIL Text API starts allowing 
            // Double.PositiveInfinity as ParagraphWidth
            TextDpi.EnsureValidLineWidth(ref safeConstraint);

            // Do the measure.
            if (_dirtyList == null)
            {
                if (constraintschanged)
                {
                    _lineMetrics.Clear();
                    _viewportLineVisuals = null;
                }
                desiredSize = FullMeasureTick(safeConstraint.Width, lineProperties);
            }
            else
            {
                desiredSize = IncrementalMeasure(safeConstraint.Width, lineProperties);
            }
            Invariant.Assert(_lineMetrics.Count >= 1);

            _dirtyList = null;

            double oldWidth = _contentSize.Width;
            _contentSize = desiredSize;

            // If the width has changed we need to reformat if we're centered or right aligned so the
            // spacing gets properly updated.
            if (oldWidth != desiredSize.Width && lineProperties.TextAlignment != TextAlignment.Left)
            {
                Rerender();
            }

        Exit:
            // DesiredSize is set to the calculated size of the content.
            // If hosted by ScrollViewer, desired size is limited to constraint.
            if (_scrollData != null)
            {
                desiredSize.Width = Math.Min(constraint.Width, desiredSize.Width);
                desiredSize.Height = Math.Min(constraint.Height, desiredSize.Height);
            }

            _previousConstraint = constraint;

            return desiredSize;
        }

        // Arranges content within a specified constraint.
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (_lineMetrics == null || _lineMetrics.Count == 0)
            {
                // No matching MeasureOverride call.
                goto Exit;
            }

            EnsureCache();

            ArrangeScrollData(arrangeSize);
            ArrangeVisuals(arrangeSize);

            _cache = null;

            FireTextViewUpdatedEvent();

        Exit:
            return arrangeSize;
        }

        // Render callback for this TextBoxView.
        protected override void OnRender(DrawingContext context)
        {
            // Render a transparent Rect to enable hit-testing even when content does not fill
            // the entire viewport.
            // find a way to do this without rendering anything.
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), null, new Rect(0, 0, this.RenderSize.Width, this.RenderSize.Height));
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index >= this.VisualChildrenCount)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return _visualChildren[index];
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///      During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return (_visualChildren == null) ? 0 : _visualChildren.Count;
            }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// <see cref="ITextView.GetTextPositionFromPoint"/>
        /// </summary>
        ITextPointer ITextView.GetTextPositionFromPoint(Point point, bool snapToText)
        {
            Invariant.Assert(this.IsLayoutValid);

            point = TransformToDocumentSpace(point);

            int lineIndex = GetLineIndexFromPoint(point, snapToText);
            ITextPointer position;

            if (lineIndex == -1)
            {
                position = null;
            }
            else
            {
                position = GetTextPositionFromDistance(lineIndex, point.X);
                position.Freeze();
            }

            return position;
        }

        /// <summary>
        /// <see cref="ITextView.GetRectangleFromTextPosition"/>
        /// </summary>
        Rect ITextView.GetRectangleFromTextPosition(ITextPointer position)
        {
            Rect rect;

            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));

            int offset = position.Offset;
            if (offset > 0 && position.LogicalDirection == LogicalDirection.Backward)
            {
                // TextBoxLine always gets the forward Rect, so back up to preceding char.
                offset--;
            }

            int lineIndex = GetLineIndexFromOffset(offset);
            FlowDirection flowDirection;
            LineProperties lineProperties;

            using (TextBoxLine line = GetFormattedLine(lineIndex, out lineProperties))
            {
                rect = line.GetBoundsFromTextPosition(offset, out flowDirection);
            }

            if (!rect.IsEmpty) // Empty rects can't be modified.
            {
                rect.Y += lineIndex * _lineHeight;

                // Return only TopLeft and Height.
                // Adjust rect.Left by taking into account flow direction of the
                // content and orientation of input position.
                if (lineProperties.FlowDirection != flowDirection)
                {
                    if (position.LogicalDirection == LogicalDirection.Forward || position.Offset == 0)
                    {
                        rect.X = rect.Right;
                    }
                }
                else
                {
                    if (position.LogicalDirection == LogicalDirection.Backward && position.Offset > 0)
                    {
                        rect.X = rect.Right;
                    }
                }

                rect.Width = 0;
            }

            return TransformToVisualSpace(rect);
        }

        /// <summary>
        /// <see cref="ITextView.GetRawRectangleFromTextPosition"/>
        /// </summary>
        Rect ITextView.GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            transform = Transform.Identity;

            return ((ITextView)this).GetRectangleFromTextPosition(position);
        }

        /// <summary>
        /// <see cref="ITextView.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        Geometry ITextView.GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            Invariant.Assert(this.IsLayoutValid);

            Geometry geometry = null;
            double endOfParaGlyphWidth = ((Control)_host).FontSize * CaretElement.c_endOfParaMagicMultiplier;

            // Since background layout may be running, clip to the computed region.
            int startOffset = Math.Min(_lineMetrics[_lineMetrics.Count - 1].EndOffset, startPosition.Offset);
            int endOffset = Math.Min(_lineMetrics[_lineMetrics.Count - 1].EndOffset, endPosition.Offset);

            // Find the intersection of the viewport with the requested range.
            int firstLineIndex;
            int lastLineIndex;
            GetVisibleLines(out firstLineIndex, out lastLineIndex);

            firstLineIndex = Math.Max(firstLineIndex, GetLineIndexFromOffset(startOffset, LogicalDirection.Forward));
            lastLineIndex = Math.Min(lastLineIndex, GetLineIndexFromOffset(endOffset, LogicalDirection.Backward));

            if (firstLineIndex > lastLineIndex)
            {
                // Visible region does not intersect with geometry.
                return null;
            }

            // Partially covered lines require a line format, so we'll handle them specially.
            // Only the first and last line are potentially partially covered.
            bool firstLinePartiallyCovered = _lineMetrics[firstLineIndex].Offset < startOffset ||
                                             _lineMetrics[firstLineIndex].EndOffset > endOffset;
            bool lastLinePartiallyCovered = _lineMetrics[lastLineIndex].Offset < startOffset ||
                                             _lineMetrics[lastLineIndex].EndOffset > endOffset;

            TextAlignment alignment = this.CalculatedTextAlignment;
            int lineIndex = firstLineIndex;

            // If we don't cover the entire first line, special case it.
            if (firstLinePartiallyCovered)
            {
                GetTightBoundingGeometryFromLineIndex(lineIndex, startOffset, endOffset, alignment, endOfParaGlyphWidth, ref geometry);
                lineIndex++;
            }

            // If it is completely covered, adjust lastLineIndex such that we handle
            // the last line in the loop below.
            if (firstLineIndex <= lastLineIndex && !lastLinePartiallyCovered)
            {
                lastLineIndex++;
            }

            // Handle all the lines that are entirely covered -- they don't require any heavy lifting.
            for (; lineIndex < lastLineIndex; lineIndex++)
            {
                double contentOffset = GetContentOffset(_lineMetrics[lineIndex].Width, alignment);
                Rect rect = new Rect(contentOffset, lineIndex * _lineHeight, _lineMetrics[lineIndex].Width, _lineHeight);

                // Add extra padding at the end of lines with linebreaks.
                ITextPointer endOfLinePosition = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[lineIndex].EndOffset, LogicalDirection.Backward);
                if (TextPointerBase.IsNextToPlainLineBreak(endOfLinePosition, LogicalDirection.Backward))
                {
                    rect.Width += endOfParaGlyphWidth;
                }

                rect = TransformToVisualSpace(rect);
                CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
            }

            // If we don't cover the entire last line, special case it.
            // Otherwise, we already handled it in the loop above.
            if (lineIndex == lastLineIndex && lastLinePartiallyCovered)
            {
                GetTightBoundingGeometryFromLineIndex(lineIndex, startOffset, endOffset, alignment, endOfParaGlyphWidth, ref geometry);
            }

            return geometry;
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        ITextPointer ITextView.GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));

            newSuggestedX = suggestedX;
            int lineIndex = GetLineIndexFromPosition(position);
            int nextLineIndex = Math.Max(0, Math.Min(_lineMetrics.Count - 1, lineIndex + count));
            linesMoved = nextLineIndex - lineIndex;

            ITextPointer nextLinePosition;
            if (linesMoved == 0)
            {
                nextLinePosition = position.GetFrozenPointer(position.LogicalDirection);
            }
            else if (DoubleUtil.IsNaN(suggestedX))
            {
                nextLinePosition = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[lineIndex + linesMoved].Offset, LogicalDirection.Forward);
            }
            else
            {
                suggestedX -= GetTextAlignmentCorrection(this.CalculatedTextAlignment, GetWrappingWidth(this.RenderSize.Width));
                nextLinePosition = GetTextPositionFromDistance(nextLineIndex, suggestedX);
            }

            nextLinePosition.Freeze();
            return nextLinePosition;
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextPage"/>
        /// </summary>
        ITextPointer ITextView.GetPositionAtNextPage(ITextPointer position, Point suggestedOffset, int count, out Point newSuggestedOffset, out int pagesMoved)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);

            newSuggestedOffset = new Point();
            pagesMoved = 0;
            return null;
        }

        /// <summary>
        /// <see cref="ITextView.IsAtCaretUnitBoundary"/>
        /// </summary>
        bool ITextView.IsAtCaretUnitBoundary(ITextPointer position)
        {
            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));
            bool boundary = false;

            int lineIndex = GetLineIndexFromPosition(position);

            CharacterHit sourceCharacterHit = new CharacterHit();
            if (position.LogicalDirection == LogicalDirection.Forward)
            {
                // Forward context, go to leading edge of position offset
                sourceCharacterHit = new CharacterHit(position.Offset, 0);
            }
            else if (position.LogicalDirection == LogicalDirection.Backward)
            {
                if (position.Offset > _lineMetrics[lineIndex].Offset)
                {
                    // For backward context, go to trailing edge of previous character
                    sourceCharacterHit = new CharacterHit(position.Offset - 1, 1);
                }
                else
                {
                    // There is no previous trailing edge on this line. We don't consider this a unit boundary.
                    return false;
                }
            }

            using (TextBoxLine line = GetFormattedLine(lineIndex))
            {
                boundary = line.IsAtCaretCharacterHit(sourceCharacterHit);
            }

            return boundary;
        }

        /// <summary>
        /// <see cref="ITextView.GetNextCaretUnitPosition"/>
        /// </summary>
        ITextPointer ITextView.GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));

            // Special case document start/end.
            if (position.Offset == 0 && direction == LogicalDirection.Backward)
            {
                return position.GetFrozenPointer(LogicalDirection.Forward);
            }
            else if (position.Offset == _host.TextContainer.SymbolCount && direction == LogicalDirection.Forward)
            {
                return position.GetFrozenPointer(LogicalDirection.Backward);
            }

            int lineIndex = GetLineIndexFromPosition(position);

            CharacterHit sourceCharacterHit = new CharacterHit(position.Offset, 0);
            CharacterHit nextCharacterHit;

            using (TextBoxLine line = GetFormattedLine(lineIndex))
            {
                if (direction == LogicalDirection.Forward)
                {
                    // Get the next caret position from the line
                    nextCharacterHit = line.GetNextCaretCharacterHit(sourceCharacterHit);
                }
                else
                {
                    // Get previous caret position from the line
                    nextCharacterHit = line.GetPreviousCaretCharacterHit(sourceCharacterHit);
                }
            }

            // Determine logical direction for next caret index and create TextPointer from it.
            LogicalDirection logicalDirection;
            if (nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == _lineMetrics[lineIndex].EndOffset &&
                direction == LogicalDirection.Forward)
            {
                // Going forward brought us to the end of a line, context must be forward for next line.
                if (lineIndex == _lineMetrics.Count - 1)
                {
                    // Last line so context must stay backward.
                    logicalDirection = LogicalDirection.Backward;
                }
                else
                {
                    logicalDirection = LogicalDirection.Forward;
                }
            }
            else if (nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength == _lineMetrics[lineIndex].Offset &&
                     direction == LogicalDirection.Backward)
            {
                // Going backward brought us to the start of a line, context must be backward for previous line.
                if (lineIndex == 0)
                {
                    // First line, so we will stay forward.
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

            ITextPointer nextCaretUnitPosition = _host.TextContainer.CreatePointerAtOffset(nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength, logicalDirection);
            nextCaretUnitPosition.Freeze();
            return nextCaretUnitPosition;
        }

        /// <summary>
        /// <see cref="ITextView.GetBackspaceCaretUnitPosition"/>
        /// </summary>
        ITextPointer ITextView.GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));

            // Special case document start.
            if (position.Offset == 0)
            {
                return position.GetFrozenPointer(LogicalDirection.Forward);
            }

            int lineIndex = GetLineIndexFromPosition(position, LogicalDirection.Backward);

            CharacterHit sourceCharacterHit = new CharacterHit(position.Offset, 0);
            CharacterHit backspaceCharacterHit;

            using (TextBoxLine line = GetFormattedLine(lineIndex))
            {
                backspaceCharacterHit = line.GetBackspaceCaretCharacterHit(sourceCharacterHit);
            }

            LogicalDirection logicalDirection;
            if (backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength == _lineMetrics[lineIndex].Offset)
            {
                // Going backward brought us to the start of a line, context must be backward for previous line
                if (lineIndex == 0)
                {
                    // First line, so we will stay forward.
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

            ITextPointer backspaceUnitPosition = _host.TextContainer.CreatePointerAtOffset(backspaceCharacterHit.FirstCharacterIndex + backspaceCharacterHit.TrailingLength, logicalDirection);
            backspaceUnitPosition.Freeze();
            return backspaceUnitPosition;
        }

        /// <summary>
        /// <see cref="ITextView.GetLineRange"/>
        /// </summary>
        TextSegment ITextView.GetLineRange(ITextPointer position)
        {
            Invariant.Assert(this.IsLayoutValid);
            Invariant.Assert(Contains(position));

            int lineIndex = GetLineIndexFromPosition(position);

            ITextPointer start = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[lineIndex].Offset, LogicalDirection.Forward);
            ITextPointer end = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[lineIndex].Offset + _lineMetrics[lineIndex].ContentLength, LogicalDirection.Forward);

            return new TextSegment(start, end, true);
        }

        /// <summary>
        /// <see cref="ITextView.GetGlyphRuns"/>
        /// </summary>
        ReadOnlyCollection<GlyphRun> ITextView.GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            // This method is not expected to be called.
            Invariant.Assert(false);
            return null;
        }

        /// <summary>
        /// <see cref="ITextView.Contains"/>
        /// </summary>
        bool ITextView.Contains(ITextPointer position)
        {
            return Contains(position);
        }

        /// <summary>
        /// <see cref="ITextView.BringPositionIntoViewAsync"/>
        /// </summary>
        void ITextView.BringPositionIntoViewAsync(ITextPointer position, object userState)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);
        }

        /// <summary>
        /// <see cref="ITextView.BringPointIntoViewAsync"/>
        /// </summary>
        void ITextView.BringPointIntoViewAsync(Point point, object userState)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);
        }

        /// <summary>
        /// <see cref="ITextView.BringLineIntoViewAsync"/>
        /// </summary>
        void ITextView.BringLineIntoViewAsync(ITextPointer position, double suggestedX, int count, object userState)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);
        }

        /// <summary>
        /// <see cref="ITextView.BringPageIntoViewAsync"/>
        /// </summary>
        void ITextView.BringPageIntoViewAsync(ITextPointer position, Point suggestedOffset, int count, object userState)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);
        }

        /// <summary>
        /// <see cref="ITextView.CancelAsync"/>
        /// </summary>
        void ITextView.CancelAsync(object userState)
        {
            // This method is not expected to be called.
            // Caller should only call this method when !ITextView.Contains(position).
            // Since TextBox is not paginated, this view always contains all TextContainer positions.
            Invariant.Assert(false);
        }

        /// <summary>
        /// <see cref="ITextView.Validate()"/>
        /// </summary>
        bool ITextView.Validate()
        {
            UpdateLayout();
            return this.IsLayoutValid;
        }

        /// <summary>
        /// <see cref="ITextView.Validate(Point)"/>
        /// </summary>
        bool ITextView.Validate(Point point)
        {
            return ((ITextView)this).Validate();
        }

        /// <summary>
        /// <see cref="ITextView.Validate(ITextPointer)"/>
        /// </summary>
        bool ITextView.Validate(ITextPointer position)
        {
            if (position.TextContainer != _host.TextContainer)
                return false;

            if (!this.IsLayoutValid)
            {
                // UpdateLayout has side-effects even when measure and arrange are clean,
                // so avoid calling it unless we must.
                UpdateLayout();

                if (!this.IsLayoutValid)
                {
                    // If we can't get the layout system to give us a valid
                    // measure/arrange, there's no hope.
                    return false;
                }
            }

            // Force background layout iterations until we catch up
            // with the position.

            int lastValidOffset = _lineMetrics[_lineMetrics.Count - 1].EndOffset;

            while (!Contains(position))
            {
                InvalidateMeasure();
                UpdateLayout();

                // UpdateLayout may invalidate the view.
                if (!this.IsLayoutValid)
                    break;

                // Break if background layout is not progressing.
                int newLastValidOffset = _lineMetrics[_lineMetrics.Count - 1].EndOffset;
                if (lastValidOffset >= newLastValidOffset)
                    break;
                lastValidOffset = newLastValidOffset;
            }

            return this.IsLayoutValid && Contains(position);
        }

        /// <summary>
        /// <see cref="ITextView.ThrottleBackgroundTasksForUserInput"/>
        /// </summary>
        void ITextView.ThrottleBackgroundTasksForUserInput()
        {
            if (_throttleBackgroundTimer == null)
            {
                // Start up a timer.  Until the timer fires, we'll disable
                // all background layout.  This leaves the TextBox responsive
                // to user input.
                _throttleBackgroundTimer = new DispatcherTimer(DispatcherPriority.Background);
                _throttleBackgroundTimer.Interval = _throttleBackgroundTimeSpan;
                _throttleBackgroundTimer.Tick += new EventHandler(OnThrottleBackgroundTimeout);
            }
            else
            {
                // Reset the timer.
                _throttleBackgroundTimer.Stop();
            }

            _throttleBackgroundTimer.Start();
        }

        // Forces a full document invalidation.
        // Called when properties that do affect layout (eg, FontSize)
        // change value.
        internal void Remeasure()
        {
            if (_lineMetrics != null)
            {
                _lineMetrics.Clear();
                _viewportLineVisuals = null;
            }
            InvalidateMeasure();
        }

        // Forces a visual invalidation.
        // Called when properties that do not affect layout (eg, ForegroundColor)
        // change value.
        internal void Rerender()
        {
            _viewportLineVisuals = null;
            InvalidateArrange();
        }

        // Returns the index of the line containing the specified offset.
        // Offset has forward direction -- we always return the following
        // line in ambiguous cases.
        internal int GetLineIndexFromOffset(int offset)
        {
            int index = -1;
            int min = 0;
            int max = _lineMetrics.Count;

            Invariant.Assert(_lineMetrics.Count >= 1);

            while (true)
            {
                Invariant.Assert(min < max, "Couldn't find offset!");

                index = min + (max - min) / 2;
                LineRecord record = _lineMetrics[index];

                if (offset < record.Offset)
                {
                    max = index;
                }
                else if (offset > record.EndOffset)
                {
                    min = index + 1;
                }
                else
                {
                    if (offset == record.EndOffset && index < _lineMetrics.Count - 1)
                    {
                        // Go to the next line if we're between two lines.
                        index++;
                    }
                    break;
                }
            }

            return index;
        }

        // stop listening to TextContainer events
        // (opposite of EnsureTextContainerListeners)
        internal void RemoveTextContainerListeners()
        {
            if (!CheckFlags(Flags.TextContainerListenersInitialized))
                return;

            // if the flag got set, all the variables should be non-null
            System.Diagnostics.Debug.Assert(_host != null && _host.TextContainer != null && _host.TextContainer.Highlights != null,
                "TextBoxView partners should not be null");

            _host.TextContainer.Changing -= new EventHandler(OnTextContainerChanging);
            _host.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
            _host.TextContainer.Highlights.Changed -= new HighlightChangedEventHandler(OnHighlightChanged);

            SetFlags(false, Flags.TextContainerListenersInitialized);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Control that owns this TextBoxView.
        internal ITextBoxViewHost Host
        {
            get
            {
                return _host;
            }
        }

        /// <summary>
        /// <see cref="ITextView.RenderScope"/>
        /// </summary>
        UIElement ITextView.RenderScope
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// <see cref="ITextView.TextContainer"/>
        /// </summary>
        ITextContainer ITextView.TextContainer
        {
            get
            {
                return _host.TextContainer;
            }
        }

        /// <summary>
        /// <see cref="ITextView.IsValid"/>
        /// </summary>
        bool ITextView.IsValid
        {
            get
            {
                return this.IsLayoutValid;
            }
        }

        /// <summary>
        /// <see cref="ITextView.RendersOwnSelection"/>
        /// </summary>
        bool ITextView.RendersOwnSelection
        {
            get
            {
                return !FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRendering;
            }
        }


        /// <summary>
        /// <see cref="ITextView.TextSegments"/>
        /// </summary>
        ReadOnlyCollection<TextSegment> ITextView.TextSegments
        {
            get
            {
                List<TextSegment> segments = new List<TextSegment>(1);
                if (_lineMetrics != null)
                {
                    ITextPointer start = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[0].Offset, LogicalDirection.Backward);
                    ITextPointer end = _host.TextContainer.CreatePointerAtOffset(_lineMetrics[_lineMetrics.Count - 1].EndOffset, LogicalDirection.Forward);

                    segments.Add(new TextSegment(start, end, true));
                }
                return new ReadOnlyCollection<TextSegment>(segments);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// <see cref="ITextView.BringPositionIntoViewCompleted"/>
        /// </summary>
        // Caller should only call this method when !ITextView.Contains(position).
        // Since TextBox is not paginated, this view always contains all TextContainer positions.
        event BringPositionIntoViewCompletedEventHandler ITextView.BringPositionIntoViewCompleted
        {
            add { Invariant.Assert(false); }
            remove { Invariant.Assert(false); }
        }

        /// <summary>
        /// <see cref="ITextView.BringPointIntoViewCompleted"/>
        /// </summary>
        // Caller should only call this method when !ITextView.Contains(position).
        // Since TextBox is not paginated, this view always contains all TextContainer positions.
        event BringPointIntoViewCompletedEventHandler ITextView.BringPointIntoViewCompleted
        {
            add { Invariant.Assert(false); }
            remove { Invariant.Assert(false); }
        }

        /// <summary>
        /// <see cref="ITextView.BringLineIntoViewCompleted"/>
        /// </summary>
        // Caller should only call this method when !ITextView.Contains(position).
        // Since TextBox is not paginated, this view always contains all TextContainer positions.
        event BringLineIntoViewCompletedEventHandler ITextView.BringLineIntoViewCompleted
        {
            add { Invariant.Assert(false); }
            remove { Invariant.Assert(false); }
        }

        /// <summary>
        /// <see cref="ITextView.BringPageIntoViewCompleted"/>
        /// </summary>
        // Caller should only call this method when !ITextView.Contains(position).
        // Since TextBox is not paginated, this view always contains all TextContainer positions.
        event BringPageIntoViewCompletedEventHandler ITextView.BringPageIntoViewCompleted
        {
            add { Invariant.Assert(false); }
            remove { Invariant.Assert(false); }
        }

        /// <summary>
        /// <see cref="ITextView.Updated"/>
        /// </summary>
        event EventHandler ITextView.Updated
        {
            add { UpdatedEvent += value; }
            remove { UpdatedEvent -= value; }
        }

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Initializes TextContainer event listeners.
        // Called on the first Measure.
        // We delay the init to avoid responding to events before we're attached
        // to the visual tree, when it doesn't matter.
        private void EnsureTextContainerListeners()
        {
            if (CheckFlags(Flags.TextContainerListenersInitialized))
                return;

            _host.TextContainer.Changing += new EventHandler(OnTextContainerChanging);
            _host.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);
            _host.TextContainer.Highlights.Changed += new HighlightChangedEventHandler(OnHighlightChanged);

            SetFlags(true, Flags.TextContainerListenersInitialized);
        }

        // Initializes state used across a measure/arrange calculation.
        private void EnsureCache()
        {
            if (_cache == null)
            {
                _cache = new TextCache(this);
            }
        }

        // Reads the current (interesting) property values on the owning TextBox.
        private LineProperties GetLineProperties()
        {
            TextProperties defaultTextProperties = new TextProperties((Control)_host, _host.IsTypographyDefaultValue);

            // Pass page width and height as double.MaxValue when creating LineProperties, since TextBox does not restrict
            // TextIndent or LineHeight.
            return new LineProperties((Control)_host, (Control)_host, defaultTextProperties, null, this.CalculatedTextAlignment);
        }

        // Callback from the TextContainer when a change block starts.
        private void OnTextContainerChanging(object sender, EventArgs args)
        {
            // We should be tracking reentrency with flags.
        }

        // Callback from the TextContainer on a document edit.
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            if (args.Count == 0)
            {
                // A no-op for this control.  Happens when IMECharCount updates happen
                // without corresponding SymbolCount changes.
                return;
            }

            //
            // Add the change to our dirty list.
            //

            if (_dirtyList == null)
            {
                _dirtyList = new DtrList();
            }

            DirtyTextRange dirtyTextRange = new DirtyTextRange(args);
            _dirtyList.Merge(dirtyTextRange);

            //
            // Force a re-measure.
            //
            InvalidateMeasure();
        }

        // Callback from the TextContainer when a highlight changes.
        private void OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            // TextBoxView supports SpellerHighlight
            
            // Also support TextSelection owners for the TextSelectionHighlightLayer so we can use
            // this layer to drive text selections in TextBoxLine.
            if (args.OwnerType != typeof(SpellerHighlightLayer)
               && (!((ITextView)this).RendersOwnSelection || args.OwnerType != typeof(TextSelection)))
            {
                return;
            }

            bool measureNeeded = false;
            bool arrangeNeeded = false;

            if (_dirtyList == null)
            {
                _dirtyList = new DtrList();
            }

            // We use a temporary dirty list in order to build dirty ranges that may not be used
            var tempDirtyList = new DtrList();

            //
            // Add the change to our temp dirty list.
            //
            foreach (TextSegment segment in args.Ranges)
            {
                int positionsCovered = segment.End.Offset - segment.Start.Offset;
                DirtyTextRange dirtyTextRange = new DirtyTextRange(segment.Start.Offset, positionsCovered, positionsCovered, fromHighlightLayer: true);
                tempDirtyList.Merge(dirtyTextRange);
            }

            DirtyTextRange highlightRange = tempDirtyList.GetMergedRange();

            if (args.OwnerType == typeof(TextSelection))
            {
                HandleTextSelectionHighlightChange(highlightRange, ref arrangeNeeded, ref measureNeeded);
            }
            else if (args.OwnerType == typeof(SpellerHighlightLayer))
            {
                _dirtyList.Merge(highlightRange);
                measureNeeded = true;
            }

            if (measureNeeded)
            {
                //
                // Force a re-measure.
                //
                // NB: it's not currently possible to InvalidateArrange here.
                // "Render only" changes from the highlight layer change the way we
                // ultimately feed text to the formatter.  Introducing breaks for
                // highlights may actually change the layout of the text as
                // characters are interpreted in different contexts.  
                //
                
                // The above comment does not apply to TextSelection highlights as
                // we take these possible changes into account.
                InvalidateMeasure();
            }
            else if (arrangeNeeded)
            {
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Process a change to the text selection highlight.
        /// </summary>
        /// <param name="currentSelectionRange">The range encompassing the text selection</param>
        /// <param name="arrangeNeeded">Set to true if we need to call arrange, false otherwise</param>
        /// <param name="measureNeeded">Set to true if we need to call measure, false otherwise</param>
        private void HandleTextSelectionHighlightChange(DirtyTextRange currentSelectionRange, ref bool arrangeNeeded, ref bool measureNeeded)
        {
            if (_lineMetrics.Count == 0)
            {
                measureNeeded = true;
                return;
            }

            // If there is already a change waiting in the dirty list that covers our highlight change
            // then we do not need to evaluate selection ranges, just merge with the current range as
            // there will be changes within the text range anyway.
            if (_dirtyList.Length > 0
                && _dirtyList.DtrsFromRange(currentSelectionRange.StartIndex, currentSelectionRange.PositionsAdded) != null)
            {
                _dirtyList.Merge(currentSelectionRange);
                measureNeeded = true;
                return;
            }

            // Text selection is inherently different from speller highlights.  We are guaranteed a single contiguous range.
            // As such, we can optimize our algorithm to choose arrange over measure.
            int[] offsets = new int[] { currentSelectionRange.StartIndex, currentSelectionRange.StartIndex + currentSelectionRange.PositionsAdded };

            using (TextBoxLine line = new TextBoxLine(this))
            {
                Control hostControl = (Control)_host;
                LineProperties lineProperties = GetLineProperties();
                TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(hostControl);
                TextFormatter formatter = TextFormatter.FromCurrentDispatcher(textFormattingMode);
                double width = GetWrappingWidth(this.RenderSize.Width);
                double formatWidth = GetWrappingWidth(_previousConstraint.Width);

                // We loop through both the start and end offsets for our text selection highlight.
                // The start and end offsets are the only places where the text breaking in 
                // TextBoxLine can result in a change in line metrics (making a line longer).
                // It is this case that requires us to re-measure the line where the difference occurs
                // and possibly subsequent lines.
                // Note that by this time, the highlight layer has been updated so line.Format will
                // take these into account when text breaking.
                foreach (int offset in offsets)
                {
                    int lineIndex = GetLineIndexFromOffset(offset);

                    LineRecord metrics = _lineMetrics[lineIndex];

                    line.Format(metrics.Offset, formatWidth, width, lineProperties, new TextRunCache(), formatter);

                    if (metrics.Length != line.Length)
                    {
                        measureNeeded = true;

                        // If a line has a difference, we need to at least re-measure the range it covers.  When the dirty list
                        // is eventually merged into a larger range in IncrementalMeasure, the entire selection range will be re-measured
                        // if needed.
                        _dirtyList.Merge(new DirtyTextRange(metrics.Offset, metrics.Length, metrics.Length, fromHighlightLayer: true));
                    }
                }
            }

            if (!measureNeeded)
            {
                // If we do not need a measure, then check if we need to arrange the visuals again.
                // This is true if the selection intersects with the viewport as we will need to
                // re-render those visuals.

                // If the selection highlight is within our viewport, add the range
                // to be rendered.
                DirtyTextRange? selectionRenderRange = GetSelectionRenderRange(currentSelectionRange);

                if (selectionRenderRange.HasValue)
                {
                    _dirtyList.Merge(selectionRenderRange.Value);
                    arrangeNeeded = true;
                    SetFlags(true, Flags.ArrangePendingFromHighlightLayer);
                }
                else if (_dirtyList.Length == 0)
                {
                    // If we have no work to do here, null out the list
                    // as dirty list is kept null by convention when it is empty.
                    _dirtyList = null;
                }
            }
        }

        // Sets boolean state.
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        // Reads boolean state.
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        // Announces a layout change to any listeners.
        private void FireTextViewUpdatedEvent()
        {
            if (UpdatedEvent != null)
            {
                UpdatedEvent(this, EventArgs.Empty);
            }
        }

        // Returns the index of a line containing point, or -1 if no such
        // line exists.  If snapToText is true, the closest match is returned.
        //
        // Point must be in document space.
        private int GetLineIndexFromPoint(Point point, bool snapToText)
        {
            Invariant.Assert(_lineMetrics.Count >= 1);

            // Special case points above or below the content.

            if (point.Y < 0)
            {
                return snapToText ? 0 : -1;
            }
            if (point.Y >= _lineHeight * _lineMetrics.Count)
            {
                return snapToText ? _lineMetrics.Count - 1 : -1;
            }

            // Do a binary search to find the matching line.

            int index = -1;
            int min = 0;
            int max = _lineMetrics.Count;

            while (min < max)
            {
                index = min + (max - min) / 2;
                LineRecord record = _lineMetrics[index];
                double lineY = _lineHeight * index;

                if (point.Y < lineY)
                {
                    max = index;
                }
                else if (point.Y >= lineY + _lineHeight)
                {
                    min = index + 1;
                }
                else
                {
                    if (!snapToText &&
                        (point.X < 0 || point.X >= record.Width))
                    {
                        index = -1;
                    }
                    break;
                }
            }

            return (min < max) ? index : -1;
        }

        // Returns the index of the line containing position.
        private int GetLineIndexFromPosition(ITextPointer position)
        {
            return GetLineIndexFromOffset(position.Offset, position.LogicalDirection);
        }

        // Returns the index of the line containing position.
        private int GetLineIndexFromPosition(ITextPointer position, LogicalDirection direction)
        {
            return GetLineIndexFromOffset(position.Offset, direction);
        }

        // Returns the index of the line containing the specified offset.
        private int GetLineIndexFromOffset(int offset, LogicalDirection direction)
        {
            if (offset > 0 && direction == LogicalDirection.Backward)
            {
                // GetLineIndexFromOffset has forward bias, so backup for backward search.
                offset--;
            }

            return GetLineIndexFromOffset(offset);
        }

        // Returns a formatted TextBoxLine at the specified index.
        // Caller must Dispose the TextBoxLine.
        // This method is expensive.
        private TextBoxLine GetFormattedLine(int lineIndex)
        {
            LineProperties lineProperties;
            return GetFormattedLine(lineIndex, out lineProperties);
        }

        // Returns a formatted TextBoxLine at the specified index.
        // Caller must Dispose the TextBoxLine.
        // This method is expensive.
        private TextBoxLine GetFormattedLine(int lineIndex, out LineProperties lineProperties)
        {
            TextBoxLine line = new TextBoxLine(this);
            LineRecord metrics = _lineMetrics[lineIndex];
            lineProperties = GetLineProperties();

            Control hostControl = (Control)_host;
            TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(hostControl);
            TextFormatter formatter = TextFormatter.FromCurrentDispatcher(textFormattingMode);

            double width = GetWrappingWidth(this.RenderSize.Width);
            double formatWidth = GetWrappingWidth(_previousConstraint.Width);

            line.Format(metrics.Offset, formatWidth, width, lineProperties, new TextRunCache(), formatter);
            Invariant.Assert(metrics.Length == line.Length, "Line is out of sync with metrics!");

            return line;
        }

        // Returns a TextPointer at the position closest to pixel offset x
        // on a specified line.
        private ITextPointer GetTextPositionFromDistance(int lineIndex, double x)
        {
            LineProperties lineProperties;
            CharacterHit charIndex;
            LogicalDirection logicalDirection;

            using (TextBoxLine line = GetFormattedLine(lineIndex, out lineProperties))
            {
                charIndex = line.GetTextPositionFromDistance(x);

                logicalDirection = (charIndex.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
            }

            return _host.TextContainer.CreatePointerAtOffset(charIndex.FirstCharacterIndex + charIndex.TrailingLength, logicalDirection);
        }

        // Updates IScrollInfo related state on an ArrangeOverride call.
        private void ArrangeScrollData(Size arrangeSize)
        {
            if (_scrollData == null)
            {
                return;
            }

            bool invalidateScrollInfo = false;

            if (!DoubleUtil.AreClose(_scrollData.Viewport, arrangeSize))
            {
                _scrollData.Viewport = arrangeSize;
                invalidateScrollInfo = true;
            }

            if (!DoubleUtil.AreClose(_scrollData.Extent, _contentSize))
            {
                _scrollData.Extent = _contentSize;
                invalidateScrollInfo = true;
            }

            Vector offset = new Vector(
                Math.Max(0, Math.Min(_scrollData.ExtentWidth - _scrollData.ViewportWidth, _scrollData.HorizontalOffset)),
                Math.Max(0, Math.Min(_scrollData.ExtentHeight - _scrollData.ViewportHeight, _scrollData.VerticalOffset)));

            if (!DoubleUtil.AreClose(offset, _scrollData.Offset))
            {
                _scrollData.Offset = offset;
                invalidateScrollInfo = true;
            }

            if (invalidateScrollInfo && _scrollData.ScrollOwner != null)
            {
                _scrollData.ScrollOwner.InvalidateScrollInfo();
            }
        }

        // Updates line visuals on an ArrangeOverride call.
        private void ArrangeVisuals(Size arrangeSize)
        {
            // We should only see pending incremental updates in arrange when they
            // have come explicitly from the highlight layer.
            Invariant.Assert(CheckFlags(Flags.ArrangePendingFromHighlightLayer) || _dirtyList == null);

            SetFlags(false, Flags.ArrangePendingFromHighlightLayer);

            // If _dirtyList is non-null here, it means we
            // have pending highlight changes to sync to.
            // These changes never affect line metrics, but
            // they will clear out any cached Visuals affected.
            if (_dirtyList != null)
            {
                InvalidateDirtyVisuals();
                _dirtyList = null;
            }

            //
            // Initialize state.
            //

            if (_visualChildren == null)
            {
                _visualChildren = new List<TextBoxLineDrawingVisual>(1);
            }

            EnsureCache();

            LineProperties lineProperties = _cache.LineProperties;
            TextBoxLine line = new TextBoxLine(this);

            //
            // Calculate the current viewport extent, in lines.
            // We won't do any work for lines that aren't visible.
            //

            int firstLineIndex;
            int lastLineIndex;

            GetVisibleLines(out firstLineIndex, out lastLineIndex);

            SetViewportLines(firstLineIndex, lastLineIndex);

            double width = GetWrappingWidth(arrangeSize.Width);

            double horizontalOffset = GetTextAlignmentCorrection(lineProperties.TextAlignment, width);
            double verticalOffset = this.VerticalAlignmentOffset;

            if (_scrollData != null)
            {
                horizontalOffset -= _scrollData.HorizontalOffset;
                verticalOffset -= _scrollData.VerticalOffset;
            }

            // Remove invalidated lines from the visual tree.
            DetachDiscardedVisualChildren();

            //
            // Iterate across the visible lines.
            // If we have a cached visual, simply update its current offset.
            // Otherwise, allocate and render a new visual.
            //

            double formatWidth = GetWrappingWidth(_previousConstraint.Width);

            double endOfParaGlyphWidth = ((Control)_host).FontSize * CaretElement.c_endOfParaMagicMultiplier;

            // Only render the selection if we are not using the adorner and
            // the selection is active or if inactive selection rendering is enabled.
            bool shouldRenderSelection = ((ITextView)this).RendersOwnSelection
                && ((bool)((Control)_host).GetValue(TextBoxBase.IsInactiveSelectionHighlightEnabledProperty)
                || (bool)((Control)_host).GetValue(TextBoxBase.IsSelectionActiveProperty));

            for (int lineIndex = firstLineIndex; lineIndex <= lastLineIndex; lineIndex++)
            {
                TextBoxLineDrawingVisual lineVisual = GetLineVisual(lineIndex);

                if (lineVisual == null)
                {
                    LineRecord metrics = _lineMetrics[lineIndex];

                    using (line)
                    {
                        line.Format(metrics.Offset, formatWidth, width, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                        // We should be in sync with current metrics, unless background layout is pending.
                        if (!this.IsBackgroundLayoutPending)
                        {
                            Invariant.Assert(metrics.Length == line.Length, "Line is out of sync with metrics!");
                        }

                        Geometry selectionGeometry = null;

                        if (shouldRenderSelection)
                        {
                            var selection = _host.TextContainer.TextSelection;

                            if (!selection.IsEmpty)
                            {
                                GetTightBoundingGeometryFromLineIndexForSelection(line, lineIndex, selection.Start.CharOffset, selection.End.CharOffset, CalculatedTextAlignment, endOfParaGlyphWidth, ref selectionGeometry);
                            }
                        }

                        lineVisual = line.CreateVisual(selectionGeometry);
                    }

                    SetLineVisual(lineIndex, lineVisual);
                    AttachVisualChild(lineVisual);
                }

                lineVisual.Offset = new Vector(horizontalOffset, verticalOffset + lineIndex * _lineHeight);
            }
        }

        /// <summary>
        /// Called during Arrange, clears any cached line Visuals that intersect with highlight changes
        /// stored in the dirty range list.
        /// </summary>
        private void InvalidateDirtyVisuals()
        {
            // Find the affected line, and reset its visual.
            // Highlights never affect measure.
            for (int i = 0; i < _dirtyList.Length; i++)
            {
                DirtyTextRange range = _dirtyList[i];

                Invariant.Assert(range.FromHighlightLayer); // We should never get any non-highlight changes here
                Invariant.Assert(range.PositionsAdded == range.PositionsRemoved); // FromHighlightLayer never changes document size.

                int firstLineIndex = GetLineIndexFromOffset(range.StartIndex, LogicalDirection.Forward);
                int endOffset = Math.Min(range.StartIndex + range.PositionsAdded, _host.TextContainer.SymbolCount);
                int lastLineIndex = GetLineIndexFromOffset(endOffset, LogicalDirection.Backward);

                for (int lineIndex = firstLineIndex; lineIndex <= lastLineIndex; lineIndex++)
                {
                    ClearLineVisual(lineIndex);
                }
            }
        }

        // (Any change in a multiline TextBox causes all the DrawingVisuals to have their native
        // resources (including glyph bitmaps) destroyed and recreated)
        // Removes lines that were discarded during Measure from the visual tree. We don't want to
        // clear all of the visual children and then add lines that were already in the visual tree
        // back because native resources will get freed and reallocated unnecessarily (ref count goes
        // to 0. 
        //
        // It is safe to modify the visual tree in Arrange, but there are no guarantees during Measure.
        // It might be possible to get rid of TextBoxLineDrawingVisual and remove items from the
        // visual tree during Measure as well.
        private void DetachDiscardedVisualChildren()
        {
            int j = _visualChildren.Count - 1; // last non-discarded element index

            for (int i = _visualChildren.Count - 1; i >= 0; i--)
            {
                if (_visualChildren[i] == null || _visualChildren[i].DiscardOnArrange)
                {
                    RemoveVisualChild(_visualChildren[i]);

                    if (i < j)
                    {
                        _visualChildren[i] = _visualChildren[j];
                    }

                    j--;
                }
            }

            if (j < _visualChildren.Count - 1)
            {
                _visualChildren.RemoveRange(j + 1, _visualChildren.Count - j - 1);
            }
        }

        // Adds a line visual to the visual tree.
        private void AttachVisualChild(TextBoxLineDrawingVisual lineVisual)
        {
            // Ideally we should add visual to a collection before calling AddVisualChild.
            // So that VisualDiagnostics.OnVisualChildChanged can get correct child index.
            // However it is not clear what can regress. We'll use _parentIndex.
            // Note that there is a comment in Visual.cs stating that _parentIndex should
            // be set to -1 in DEBUG builds when child is removed. We are not going to
            // honor it. There is no _parentIndex == -1 validation is performed anywhere.
            lineVisual._parentIndex = _visualChildren.Count;
            AddVisualChild(lineVisual);
            _visualChildren.Add(lineVisual);
        }

        // Removes all line visuals from the visual tree.
        private void ClearVisualChildren()
        {
            for (int i = 0; i < _visualChildren.Count; i++)
            {
                RemoveVisualChild(_visualChildren[i]);
            }

            _visualChildren.Clear();
        }

        // Transforms a Point in visual space (where (0, 0) is the upper-left
        // corner of this FrameworkElement) to document space (where (0, 0) is
        // the upper-left corner of the document, which may be scrolled to a
        // negative offset relative to visual space).
        private Point TransformToDocumentSpace(Point point)
        {
            if (_scrollData != null)
            {
                point = new Point(point.X + _scrollData.HorizontalOffset, point.Y + _scrollData.VerticalOffset);
            }

            point.X -= GetTextAlignmentCorrection(this.CalculatedTextAlignment, GetWrappingWidth(this.RenderSize.Width));
            point.Y -= this.VerticalAlignmentOffset;

            return point;
        }

        // Transforms a Rect in document space (where (0, 0) is
        // the upper-left corner of the document, which may be scrolled to a
        // negative offset relative to visual space) to visual space
        // (where (0, 0) is the upper-left corner of this FrameworkElement).
        private Rect TransformToVisualSpace(Rect rect)
        {
            if (_scrollData != null)
            {
                rect.X -= _scrollData.HorizontalOffset;
                rect.Y -= _scrollData.VerticalOffset;
            }

            rect.X += GetTextAlignmentCorrection(this.CalculatedTextAlignment, GetWrappingWidth(this.RenderSize.Width));
            rect.Y += this.VerticalAlignmentOffset;

            return rect;
        }

        // Helper for GetTightBoundingGeometryFromTextPositions.
        // Calculates the geometry of a single line intersected with a pair of document offsets.
        private void GetTightBoundingGeometryFromLineIndex(int lineIndex, int unclippedStartOffset, int unclippedEndOffset, TextAlignment alignment, double endOfParaGlyphWidth, ref Geometry geometry)
        {
            IList<Rect> bounds;

            int startOffset = Math.Max(_lineMetrics[lineIndex].Offset, unclippedStartOffset);
            int endOffset = Math.Min(_lineMetrics[lineIndex].EndOffset, unclippedEndOffset);

            if (startOffset == endOffset) // GetRangeBounds does not accept empty runs.
            {
                // If we have any empty intersection, the only case to handle is when
                // the empty range is exactly at the end of a line with a hard break.
                // In that case we need to add the newline whitespace geometry.
                if (unclippedStartOffset == _lineMetrics[lineIndex].EndOffset)
                {
                    ITextPointer position = _host.TextContainer.CreatePointerAtOffset(unclippedStartOffset, LogicalDirection.Backward);
                    if (TextPointerBase.IsNextToPlainLineBreak(position, LogicalDirection.Backward))
                    {
                        Rect rect = new Rect(0, lineIndex * _lineHeight, endOfParaGlyphWidth, _lineHeight);
                        CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                    }
                }
                else
                {
                    // WinBlue bug 433347 uncovered a scenario where Narrator (starting
                    // in Win8, and worsening in Blue) asks for the geometry around a
                    // range that includes only end-of-line characters.   Such a call
                    // arrives in this method with startOffset==endOffset ==
                    // _lineMetrics[lineIndex].Offset + _lineMetrics[lineIndex].ContentLength;
                    // in other words, pointing at the end of the line, just before the
                    // end-of-line characters.  The previous comment suggests that
                    // this was intended be handled by adding "the newline whitespace
                    // geometry", but that doesn't happen.   Instead, control flows
                    // here where the assert fails.
                    //
                    // Ideally, we'd fix this by implementing the intent of the
                    // comment correctly.  But at this date, the consensus is to
                    // simply avoid crashing.   Changing the assert does this.
                    //Invariant.Assert(endOffset == _lineMetrics[lineIndex].Offset);
                    Invariant.Assert(endOffset == _lineMetrics[lineIndex].Offset ||
                            endOffset == _lineMetrics[lineIndex].Offset + _lineMetrics[lineIndex].ContentLength);
                }
            }
            else
            {
                using (TextBoxLine line = GetFormattedLine(lineIndex))
                {
                    bounds = line.GetRangeBounds(startOffset, endOffset - startOffset, 0, lineIndex * _lineHeight);
                }

                for (int i = 0; i < bounds.Count; i++)
                {
                    Rect rect = TransformToVisualSpace(bounds[i]);
                    CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                }

                // Add the Rect representing end-of-line, if the range covers the line end
                // and the line has a hard line break.
                if (unclippedEndOffset >= _lineMetrics[lineIndex].EndOffset)
                {
                    ITextPointer endOfLinePosition = _host.TextContainer.CreatePointerAtOffset(endOffset, LogicalDirection.Backward);

                    if (TextPointerBase.IsNextToPlainLineBreak(endOfLinePosition, LogicalDirection.Backward))
                    {
                        double contentOffset = GetContentOffset(_lineMetrics[lineIndex].Width, alignment);
                        Rect rect = new Rect(contentOffset + _lineMetrics[lineIndex].Width, lineIndex * _lineHeight, endOfParaGlyphWidth, _lineHeight);
                        rect = TransformToVisualSpace(rect);
                        CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                    }
                }
            }
        }

        /// <summary>
        /// Generates bounding geometry for a text selection.  This is similar to 
        /// GetTightBoundingGeometryFromLineIndex, but optimized for when text
        /// selection geometry isn't drawn within the adorner layer.  We need this
        /// method to allow us to generate appropriate geometry when inside of 
        /// ArrangeVisuals instead of post-arrange as the Adorner layer does.
        /// </summary>
        /// <param name="line">The TextBoxLine being bound</param>
        /// <param name="lineIndex">The index of the bound line</param>
        /// <param name="unclippedStartOffset">The start offset of the selection</param>
        /// <param name="unclippedEndOffset">The end offset of the selection</param>
        /// <param name="alignment"></param>
        /// <param name="endOfParaGlyphWidth"></param>
        /// <param name="geometry"></param>
        private void GetTightBoundingGeometryFromLineIndexForSelection(TextBoxLine line, int lineIndex, int unclippedStartOffset, int unclippedEndOffset, TextAlignment alignment, double endOfParaGlyphWidth, ref Geometry geometry)
        {
            IList<Rect> bounds;

            int lineStartOffset = _lineMetrics[lineIndex].Offset;
            int lineEndOffset = _lineMetrics[lineIndex].EndOffset;

            // If this line is not covered by the selection, no geometry is needed.
            if (lineStartOffset > unclippedEndOffset
                || lineEndOffset <= unclippedStartOffset)
            {
                return;
            }

            int startOffset = Math.Max(lineStartOffset, unclippedStartOffset);
            int endOffset = Math.Min(lineEndOffset, unclippedEndOffset);

            if (startOffset == endOffset) // GetRangeBounds does not accept empty runs.
            {
                // If we have any empty intersection, the only case to handle is when
                // the empty range is exactly at the end of a line with a hard break.
                // In that case we need to add the newline whitespace geometry.
                if (unclippedStartOffset == _lineMetrics[lineIndex].EndOffset)
                {
                    ITextPointer position = _host.TextContainer.CreatePointerAtOffset(unclippedStartOffset, LogicalDirection.Backward);
                    if (TextPointerBase.IsNextToPlainLineBreak(position, LogicalDirection.Backward))
                    {
                        Rect rect = new Rect(0, 0, endOfParaGlyphWidth, _lineHeight);
                        CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                    }
                }
                else
                {
                    // See the comment in GetTightBoundingGeometryFromLineIndex for information about this assert.
                    Invariant.Assert(endOffset == _lineMetrics[lineIndex].Offset ||
                        endOffset == _lineMetrics[lineIndex].Offset + _lineMetrics[lineIndex].ContentLength);
                }
            }
            else
            {
                bounds = line.GetRangeBounds(startOffset, endOffset - startOffset, 0, 0);

                for (int i = 0; i < bounds.Count; i++)
                {
                    Rect rect = bounds[i];
                    CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                }

                // Add the Rect representing end-of-line, if the range covers the line end
                // and the line has a hard line break.
                if (unclippedEndOffset >= _lineMetrics[lineIndex].EndOffset)
                {
                    ITextPointer endOfLinePosition = _host.TextContainer.CreatePointerAtOffset(endOffset, LogicalDirection.Backward);

                    if (TextPointerBase.IsNextToPlainLineBreak(endOfLinePosition, LogicalDirection.Backward))
                    {
                        double contentOffset = GetContentOffset(_lineMetrics[lineIndex].Width, alignment);
                        Rect rect = new Rect(contentOffset + _lineMetrics[lineIndex].Width, 0, endOfParaGlyphWidth, _lineHeight);

                        CaretElement.AddGeometry(ref geometry, new RectangleGeometry(rect));
                    }
                }
            }
        }

        // Returns the indices of the first and last lines that intersect
        // with the current viewport.
        private void GetVisibleLines(out int firstLineIndex, out int lastLineIndex)
        {
            Rect viewport = this.Viewport;

            if (!viewport.IsEmpty)
            {
                firstLineIndex = (int)(viewport.Y / _lineHeight);
                lastLineIndex = (int)Math.Ceiling((viewport.Y + viewport.Height) / _lineHeight) - 1;

                // There may not be enough lines to fill the viewport, clip appropriately.
                firstLineIndex = Math.Max(0, Math.Min(firstLineIndex, _lineMetrics.Count - 1));
                lastLineIndex = Math.Max(0, Math.Min(lastLineIndex, _lineMetrics.Count - 1));
            }
            else
            {
                // If we're not hosted by a ScrollViewer, the viewport is the whole doc.
                firstLineIndex = 0;
                lastLineIndex = _lineMetrics.Count - 1;
            }
        }

        // Performs one iteration of background measure.
        // Background measure always works at the end of the current
        // line metrics array -- invalidations to prevoiusly examined
        // content is handled by incremental layout, synchronously.
        //
        // Returns the full content size, omitting any unanalyzed content
        // at the document end.
        private Size FullMeasureTick(double constraintWidth, LineProperties lineProperties)
        {
            Size desiredSize;
            TextBoxLine line = new TextBoxLine(this);
            int lineOffset;
            bool endOfParagraph;

            // Find the next position for this iteration.

            if (_lineMetrics.Count == 0)
            {
                desiredSize = new Size();
                lineOffset = 0;
            }
            else
            {
                desiredSize = _contentSize;
                lineOffset = _lineMetrics[_lineMetrics.Count - 1].EndOffset;
            }

            // Calculate a stop time.
            // We limit work to just a few milliseconds per iteration
            // to avoid blocking the thread.
            TimeSpan stopTimeout;

            if ((ScrollBarVisibility)((Control)_host).GetValue(ScrollViewer.VerticalScrollBarVisibilityProperty) == ScrollBarVisibility.Auto)
            {
                // Workaround for bug 1766924.
                // When VerticalScrollBarVisiblity == Auto, there's a problem with
                // our interaction with ScrollViewer.  Disable background layout to
                // mitigate the problem until we can take a real fix in v.next.

                stopTimeout = TimeSpan.MaxValue;
            }
            else
            {
                stopTimeout = _maxMeasureTime;
            }

            // Format lines until we hit the end of document or run out of time.
            var measureStopwatch = System.Diagnostics.Stopwatch.StartNew();
            do
            {
                using (line)
                {
                    line.Format(lineOffset, constraintWidth, constraintWidth, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                    // This is a loop invariant, but has negligable cost.
                    // REVIEW: do we even need the CalcLineAdvance call?
                    _lineHeight = lineProperties.CalcLineAdvance(line.Height);

                    _lineMetrics.Add(new LineRecord(lineOffset, line));

                    // Desired width is always max of calculated line widths.
                    // Desired height is sum of all line heights.
                    desiredSize.Width = Math.Max(desiredSize.Width, line.Width);
                    desiredSize.Height += _lineHeight;

                    lineOffset += line.Length;
                    endOfParagraph = line.EndOfParagraph;
                }
            }
            while (!endOfParagraph && measureStopwatch.Elapsed < stopTimeout);

            if (!endOfParagraph)
            {
                // Ran out of time.  Defer to background layout.
                SetFlags(true, Flags.BackgroundLayoutPending);
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnBackgroundMeasure), null);
            }
            else
            {
                // Finished the entire document.  Stop background layout.
                SetFlags(false, Flags.BackgroundLayoutPending);
            }

            return desiredSize;
        }

        // Callback for the next background layout tick.
        private object OnBackgroundMeasure(object o)
        {
            if (_throttleBackgroundTimer == null)
            {
                InvalidateMeasure();
            }

            return null;
        }

        // Measures content invalidated due to a TextContainer change (rather than
        // a constraint change).
        //
        // Returns the full content size, omitting any unanalyzed content
        // at the document end (due to pending background layout).
        private Size IncrementalMeasure(double constraintWidth, LineProperties lineProperties)
        {
            Invariant.Assert(_dirtyList != null);
            Invariant.Assert(_dirtyList.Length > 0); // We only allocate _dirtyList when it has content.

            Size desiredSize = _contentSize;
            DirtyTextRange range = _dirtyList[0];

            // Background layout may be running, in which case we need to
            // "clip" the scope of this incremental edit.  We want to ignore
            // changes that extend past the area of the document we're already
            // tracking.
            if (range.StartIndex > _lineMetrics[_lineMetrics.Count - 1].EndOffset)
            {
                Invariant.Assert(this.IsBackgroundLayoutPending);
                return desiredSize;
            }

            // Merge the dirty list into a single superset DirtyTextRange.
            // this makes operations like drag and drop take time
            // porportional to the distance between source and destination.
            // We could potentially adjust the code to remove the merge,
            // although it would add complexity.

            int previousOffset = range.StartIndex;
            int positionsAdded = range.PositionsAdded;
            int positionsRemoved = range.PositionsRemoved;

            for (int i = 1; i < _dirtyList.Length; i++)
            {
                range = _dirtyList[i];

                if (range.StartIndex > _lineMetrics[_lineMetrics.Count - 1].EndOffset)
                {
                    Invariant.Assert(this.IsBackgroundLayoutPending);
                    break;
                }

                int rangeDistance = range.StartIndex - previousOffset;
                positionsAdded += rangeDistance + range.PositionsAdded;
                positionsRemoved += rangeDistance + range.PositionsRemoved;

                previousOffset = range.StartIndex;
            }

            range = new DirtyTextRange(_dirtyList[0].StartIndex, positionsAdded, positionsRemoved);

            if (range.PositionsAdded >= range.PositionsRemoved)
            {
                IncrementalMeasureLinesAfterInsert(constraintWidth, lineProperties, range, ref desiredSize);
            }
            else if (range.PositionsAdded < range.PositionsRemoved)
            {
                IncrementalMeasureLinesAfterDelete(constraintWidth, lineProperties, range, ref desiredSize);
            }

            return desiredSize;
        }

        // Measures content invalidated due to a TextContainer change.
        private void IncrementalMeasureLinesAfterInsert(double constraintWidth, LineProperties lineProperties, DirtyTextRange range, ref Size desiredSize)
        {
            int delta = range.PositionsAdded - range.PositionsRemoved;
            Invariant.Assert(delta >= 0);

            int lineIndex = GetLineIndexFromOffset(range.StartIndex, LogicalDirection.Forward);

            if (delta > 0)
            {
                // Increment of the offsets of all following lines.
                // this does not scale!
                for (int i = lineIndex + 1; i < _lineMetrics.Count; i++)
                {
                    _lineMetrics[i].Offset += delta;
                }
            }

            TextBoxLine line = new TextBoxLine(this);
            int lineOffset;
            bool endOfParagraph = false;

            // We need to re-format the previous line, because if someone inserted
            // a hard break, the first directly affected line might now be shorter
            // and mergeable with its predecessor.
            if (lineIndex > 0) // we can skip this if line wrap is disabled.
            {
                FormatFirstIncrementalLine(lineIndex - 1, constraintWidth, lineProperties, line, out lineOffset, out endOfParagraph);
            }
            else
            {
                lineOffset = _lineMetrics[lineIndex].Offset;
            }

            // Format the line directly affected by the change.
            // If endOfParagraph == true, then the line was absorbed into its
            // predessor (because its new content is thinner, or because the
            // TextWrapping property changed).
            if (!endOfParagraph)
            {
                using (line)
                {
                    line.Format(lineOffset, constraintWidth, constraintWidth, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                    _lineMetrics[lineIndex] = new LineRecord(lineOffset, line);

                    lineOffset += line.Length;
                    endOfParagraph = line.EndOfParagraph;
                }
                ClearLineVisual(lineIndex);
                lineIndex++;
            }

            // Recalc the following lines not directly affected as needed.
            SyncLineMetrics(range, constraintWidth, lineProperties, line, endOfParagraph, lineIndex, lineOffset);

            desiredSize = BruteForceCalculateDesiredSize();
        }

        // Measures content invalidated due to a TextContainer change.
        private void IncrementalMeasureLinesAfterDelete(double constraintWidth, LineProperties lineProperties, DirtyTextRange range, ref Size desiredSize)
        {
            int delta = range.PositionsAdded - range.PositionsRemoved;
            Invariant.Assert(delta < 0);

            int firstLineIndex = GetLineIndexFromOffset(range.StartIndex);

            // Clip the scope of the affected lines to the region of the document
            // we've already inspected.  Clipping happens when background layout
            // has not yet completed but an incremental update happens.
            int endOffset = range.StartIndex + -delta - 1;
            if (endOffset > _lineMetrics[_lineMetrics.Count - 1].EndOffset)
            {
                Invariant.Assert(this.IsBackgroundLayoutPending);
                endOffset = _lineMetrics[_lineMetrics.Count - 1].EndOffset;
                if (range.StartIndex == endOffset)
                {
                    // Nothing left to do until background layout runs.
                    return;
                }
            }

            int lastLineIndex = GetLineIndexFromOffset(endOffset);

            // Increment the offsets of all following lines.
            // this does not scale!
            for (int i = lastLineIndex + 1; i < _lineMetrics.Count; i++)
            {
                _lineMetrics[i].Offset += delta;
            }

            TextBoxLine line = new TextBoxLine(this);
            int lineIndex = firstLineIndex;
            int lineOffset;
            bool endOfParagraph;

            // We need to re-format the previous line, because if someone inserted
            // a hard break, the first directly affected line might now be shorter
            // and mergeable with its predecessor.
            if (lineIndex > 0) // we can skip this if line wrap is disabled.
            {
                FormatFirstIncrementalLine(lineIndex - 1, constraintWidth, lineProperties, line, out lineOffset, out endOfParagraph);
            }
            else
            {
                lineOffset = _lineMetrics[lineIndex].Offset;
                endOfParagraph = false;
            }

            // the following code could probably be merged into SyncLineMetrics, at which
            // point it wouldn't be hard to merge this method with IncrementalMeasureLinesAfterInsert.
            // The concern now is that removing line metrics is O(n) so it will take some care.

            // Update the first affected line.  If it's completely covered, remove it entirely below.
            if (!endOfParagraph &&
                (range.StartIndex > lineOffset || range.StartIndex + -delta < _lineMetrics[lineIndex].EndOffset))
            {
                // Only part of the line is covered, reformat it.
                using (line)
                {
                    line.Format(lineOffset, constraintWidth, constraintWidth, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                    _lineMetrics[lineIndex] = new LineRecord(lineOffset, line);

                    lineOffset += line.Length;
                    endOfParagraph = line.EndOfParagraph;
                }
                ClearLineVisual(lineIndex);
                lineIndex++;
            }

            // Remove all the following lines that are completely covered.
            // this does not scale!
            _lineMetrics.RemoveRange(lineIndex, lastLineIndex - lineIndex + 1);
            RemoveLineVisualRange(lineIndex, lastLineIndex - lineIndex + 1);

            // Recalc the following lines not directly affected as needed.
            SyncLineMetrics(range, constraintWidth, lineProperties, line, endOfParagraph, lineIndex, lineOffset);

            desiredSize = BruteForceCalculateDesiredSize();
        }

        // Helper for IncrementalMeasureLinesAfterInsert, IncrementalMeasureLinesAfterDelete.
        // Formats the line preceding the first directly affected line after a TextContainer change.
        // In general this line might grow as content in the following line is absorbed.
        private void FormatFirstIncrementalLine(int lineIndex, double constraintWidth, LineProperties lineProperties, TextBoxLine line,
            out int lineOffset, out bool endOfParagraph)
        {
            int originalEndOffset = _lineMetrics[lineIndex].EndOffset;
            lineOffset = _lineMetrics[lineIndex].Offset;

            using (line)
            {
                line.Format(lineOffset, constraintWidth, constraintWidth, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                _lineMetrics[lineIndex] = new LineRecord(lineOffset, line);

                lineOffset += line.Length;
                endOfParagraph = line.EndOfParagraph;
            }

            // Don't clear the cached Visual unless something changed.
            if (originalEndOffset != _lineMetrics[lineIndex].EndOffset)
            {
                ClearLineVisual(lineIndex);
            }
        }

        // Helper for IncrementalMeasureLinesAfterInsert, IncrementalMeasureLinesAfterDelete.
        // Formats line until we hit a synchronization point, a position where we know
        // following lines could not be affected by the change.
        private void SyncLineMetrics(DirtyTextRange range, double constraintWidth, LineProperties lineProperties, TextBoxLine line,
            bool endOfParagraph, int lineIndex, int lineOffset)
        {
            bool offsetSyncOk = (range.PositionsAdded == 0 || range.PositionsRemoved == 0);
            int lastCoveredCharOffset = range.StartIndex + Math.Max(range.PositionsAdded, range.PositionsRemoved);

            // Keep updating lines until we find a synchronized position.
            while (!endOfParagraph &&
                   (lineIndex == _lineMetrics.Count ||
                    !offsetSyncOk ||
                    lineOffset != _lineMetrics[lineIndex].Offset))
            {
                if (lineIndex < _lineMetrics.Count &&
                    lineOffset >= _lineMetrics[lineIndex].EndOffset)
                {
                    // If the current line offset starts past the current line metric offset,
                    // remove the metric.  This happens when the previous line
                    // frees up enough space to completely consume the following line.
                    // We can't simply replace the record without potentially missing our
                    // sync position.
                    _lineMetrics.RemoveAt(lineIndex); // does not scale!
                    RemoveLineVisualRange(lineIndex, 1);
                }
                else
                {
                    using (line)
                    {
                        line.Format(lineOffset, constraintWidth, constraintWidth, lineProperties, _cache.TextRunCache, _cache.TextFormatter);

                        LineRecord record = new LineRecord(lineOffset, line);

                        if (lineIndex == _lineMetrics.Count ||
                            lineOffset + line.Length <= _lineMetrics[lineIndex].Offset)
                        {
                            // The new line preceeds the old line, insert a new record.

                            // this does not scale! O(n) to insert, O(n*m) for multiple lines.
                            _lineMetrics.Insert(lineIndex, record);
                            AddLineVisualPlaceholder(lineIndex);
                        }
                        else
                        {
                            // We expect to be colliding with the old line directly.
                            // If we extend past it, we're in danger of needlessly
                            // re-formatting the entire doc (ie, we miss the real
                            // sync position and don't stop until EndOfParagraph).
                            Invariant.Assert(lineOffset < _lineMetrics[lineIndex].EndOffset);

                            var curLine = _lineMetrics[lineIndex];

                            
                            // If we see we are working with a speller or selection highlight
                            // DirtyTextRange, then once we know the metrics have not changed
                            // and we are beyond the end of the dirty region we can short 
                            // circuit the loop.  An unchanged metric means that the line
                            // has not been influenced by prior changes due to the highlight.
                            if (range.FromHighlightLayer
                               && curLine.Offset > lastCoveredCharOffset
                               && curLine.ContentLength == record.ContentLength
                               && curLine.EndOffset == record.EndOffset
                               && curLine.Length == record.Length
                               && curLine.Offset == record.Offset
                               && Standard.DoubleUtilities.AreClose(curLine.Width, record.Width))
                            {
                                break;
                            }

                            _lineMetrics[lineIndex] = record;
                            ClearLineVisual(lineIndex);

                            // If this line ends past the invalidated region, and it
                            // has a hard line break, it's safe to synchronize on the next
                            // line metric with a matching start offset.
                            offsetSyncOk |= lastCoveredCharOffset <= record.EndOffset && line.HasLineBreak;
                        }

                        lineIndex++;
                        lineOffset += line.Length;
                        endOfParagraph = line.EndOfParagraph;
                    }
                }
            }

            // Remove any trailing lines that got absorbed into the new last line.
            if (endOfParagraph && lineIndex < _lineMetrics.Count)
            {
                int count = _lineMetrics.Count - lineIndex;
                _lineMetrics.RemoveRange(lineIndex, count);
                RemoveLineVisualRange(lineIndex, count);
            }
        }

        // Calculates the bounding box of the content.
        private Size BruteForceCalculateDesiredSize()
        {
            Size desiredSize = new Size();

            // this doesn't scale.
            for (int i = 0; i < _lineMetrics.Count; i++)
            {
                desiredSize.Width = Math.Max(desiredSize.Width, _lineMetrics[i].Width);
            }
            desiredSize.Height = _lineMetrics.Count * _lineHeight;

            return desiredSize;
        }

        // Updates the array of cached Visuals matching lines in the viewport.
        // Called on arrange as the viewport changes.
        private void SetViewportLines(int firstLineIndex, int lastLineIndex)
        {
            List<TextBoxLineDrawingVisual> oldLineVisuals = _viewportLineVisuals;
            int oldLineVisualsIndex = _viewportLineVisualsIndex;

            // Assume we'll clear the cache.

            _viewportLineVisuals = null;
            _viewportLineVisualsIndex = -1;

            int count = lastLineIndex - firstLineIndex + 1;

            // Don't bother caching Visuals for single-line TextBoxes.
            // In this common case memory is important and the single line will
            // always be the one invalidated on an edit.
            if (count <= 1)
            {
                ClearVisualChildren();
                return;
            }

            // Re-init the cache to match the new viewport size.
            // Even if we don't have any Visuals to copy over from
            // the previous cache, it's useful to pre-allocate space
            // in the cache that will be filled incrementally during
            // Arrange.

            _viewportLineVisuals = new List<TextBoxLineDrawingVisual>(count);
            _viewportLineVisuals.AddRange(new TextBoxLineDrawingVisual[count]); // must we allocate an empty array?
            _viewportLineVisualsIndex = firstLineIndex;

            if (oldLineVisuals == null)
            {
                ClearVisualChildren();
                return;
            }

            // Copy over the intersection of the old viewport Visuals cache
            // with the new one.

            // It would be convenient if the code below assumed that if
            // viewport size has changed, we never make it this far (the
            // old viewport visuals should have been thrown away, since
            // there's no way now to map to the new constraint).
            //
            // However, because of rounding error, we can end up in the situation
            // where the indices/lengths between the two arrays vary, after
            // an arrange invalidation.

            int oldLastLineIndex = oldLineVisualsIndex + oldLineVisuals.Count - 1;

            if (oldLineVisualsIndex <= lastLineIndex &&
                oldLastLineIndex >= firstLineIndex)
            {
                int lineIndex = Math.Max(oldLineVisualsIndex, firstLineIndex);
                int lineCount = Math.Min(oldLastLineIndex, firstLineIndex + count - 1) - lineIndex + 1;

                for (int i = 0; i < lineCount; i++)
                {
                    _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex + i] = oldLineVisuals[lineIndex - oldLineVisualsIndex + i];
                }

                // Mark discarded lines visuals so they can be removed from the visual tree in ArrangeVisuals.

                for (int i = 0; i < lineIndex - oldLineVisualsIndex; i++)
                {
                    if (oldLineVisuals[i] != null)
                    {
                        oldLineVisuals[i].DiscardOnArrange = true;
                    }
                }

                for (int i = lineIndex - oldLineVisualsIndex + lineCount; i < oldLineVisuals.Count; i++)
                {
                    if (oldLineVisuals[i] != null)
                    {
                        oldLineVisuals[i].DiscardOnArrange = true;
                    }
                }
            }
            else
            {
                ClearVisualChildren();
            }
        }

        // Retrives the cached line Visual matching a line index in the
        // current viewport.  Will return null if no value is cached.
        private TextBoxLineDrawingVisual GetLineVisual(int lineIndex)
        {
            TextBoxLineDrawingVisual lineVisual = null;

            if (_viewportLineVisuals != null)
            {
                lineVisual = _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex];
            }

            return lineVisual;
        }

        // Adds a Visual to the line Visuals cache.
        private void SetLineVisual(int lineIndex, TextBoxLineDrawingVisual lineVisual)
        {
            if (_viewportLineVisuals != null)
            {
                _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex] = lineVisual;
            }
        }

        // Adds an empty entry to the line Visuals cache.
        private void AddLineVisualPlaceholder(int lineIndex)
        {
            if (_viewportLineVisuals != null)
            {
                // Clip to visible region.
                if (lineIndex >= _viewportLineVisualsIndex &&
                    lineIndex < _viewportLineVisualsIndex + _viewportLineVisuals.Count)
                {
                    _viewportLineVisuals.Insert(lineIndex - _viewportLineVisualsIndex, null);
                }
            }
        }

        // Invalidates a cached line Visual.
        private void ClearLineVisual(int lineIndex)
        {
            if (_viewportLineVisuals != null)
            {
                // Clip to visible region.
                if (lineIndex >= _viewportLineVisualsIndex &&
                    lineIndex < _viewportLineVisualsIndex + _viewportLineVisuals.Count &&
                    _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex] != null)
                {
                    // Mark discarded line visual so it can be removed from the visual tree in ArrangeVisuals.
                    _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex].DiscardOnArrange = true;
                    _viewportLineVisuals[lineIndex - _viewportLineVisualsIndex] = null;
                }
            }
        }

        // Removes a range of Visuals from the line Visual cache.
        private void RemoveLineVisualRange(int lineIndex, int count)
        {
            if (_viewportLineVisuals != null)
            {
                // Clip to visible region.
                if (lineIndex < _viewportLineVisualsIndex)
                {
                    count -= _viewportLineVisualsIndex - lineIndex;
                    count = Math.Max(0, count);
                    lineIndex = _viewportLineVisualsIndex;
                }
                if (lineIndex < _viewportLineVisualsIndex + _viewportLineVisuals.Count)
                {
                    int start = lineIndex - _viewportLineVisualsIndex;
                    count = Math.Min(count, _viewportLineVisuals.Count - start);

                    // Mark discarded lines visuals so they can be removed from the visual tree in ArrangeVisuals.

                    for (int i = 0; i < count; i++)
                    {
                        if (_viewportLineVisuals[start + i] != null)
                        {
                            _viewportLineVisuals[start + i].DiscardOnArrange = true;
                        }
                    }

                    _viewportLineVisuals.RemoveRange(start, count);
                }
            }
        }

        // Callback for the background layout throttle timer.
        // Resumes backgound layout.
        private void OnThrottleBackgroundTimeout(object sender, EventArgs e)
        {
            _throttleBackgroundTimer.Stop();
            _throttleBackgroundTimer = null;

            if (this.IsBackgroundLayoutPending)
            {
                OnBackgroundMeasure(null);
            }
        }

        // Returns the x-axis offset of content on a line, based on current
        // text alignment.
        private double GetContentOffset(double lineWidth, TextAlignment aligment)
        {
            double contentOffset;
            double width = GetWrappingWidth(this.RenderSize.Width);

            switch (aligment)
            {
                case TextAlignment.Right:
                    contentOffset = width - lineWidth;
                    break;

                case TextAlignment.Center:
                    contentOffset = (width - lineWidth) / 2;
                    break;

                default:
                    // Default is Left alignment, in this case offset is 0.
                    contentOffset = 0.0;
                    break;
            }

            return contentOffset;
        }

        // Converts a HorizontalAlignment enum to a TextAlignment enum.
        private TextAlignment HorizontalAlignmentToTextAlignment(HorizontalAlignment horizontalAlignment)
        {
            TextAlignment textAlignment;

            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Left:
                default:
                    textAlignment = TextAlignment.Left;
                    break;

                case HorizontalAlignment.Right:
                    textAlignment = TextAlignment.Right;
                    break;

                case HorizontalAlignment.Center:
                    textAlignment = TextAlignment.Center;
                    break;

                case HorizontalAlignment.Stretch:
                    textAlignment = TextAlignment.Justify;
                    break;
            }

            return textAlignment;
        }

        /// <summary>
        /// <see cref="ITextView.Contains"/>
        /// </summary>
        private bool Contains(ITextPointer position)
        {
            Invariant.Assert(this.IsLayoutValid);

            return position.TextContainer == _host.TextContainer &&
                   _lineMetrics != null &&
                   _lineMetrics[_lineMetrics.Count - 1].EndOffset >= position.Offset;
        }

        // Converts a render size width into a wrapping width for lines.
        private double GetWrappingWidth(double width)
        {
            if (width < _contentSize.Width)
            {
                width = _contentSize.Width;
            }

            if (width > _previousConstraint.Width)
            {
                width = _previousConstraint.Width;
            }

            // Make sure that TextFormatter limitations are not exceeded.
            // TODO: Remove it when MIL Text API starts allowing 
            // Double.PositiveInfinity as ParagraphWidth
            TextDpi.EnsureValidLineWidth(ref width);

            return width;
        }

        // When the content size exceeds the viewport size, TextLine will align
        // its content such that the "extra" is clipped in inappropriate ways.
        //
        // TextAlignment.Center: line offset = -(contentWidth - viewportWidth) / 2
        // TextAlignment.Right:  line offset = -(contentWidth - viewportWidth)
        //
        // This method returns a value that exactly cancels out the undesired
        // offset, which is used to adjust the content origin to local zero.
        private double GetTextAlignmentCorrection(TextAlignment textAlignment, double width)
        {
            double correction = 0;

            if (textAlignment != TextAlignment.Left &&
                _contentSize.Width > width)
            {
                correction = -GetContentOffset(_contentSize.Width, textAlignment);
            }

            return correction;
        }

        /// <summary>
        /// Uses the current selection range in order to calculate the subset of
        /// the range that is within the viewport.
        /// </summary>
        /// <returns>The current range subset of the selection that resides in the viewport.</returns>
        private DirtyTextRange? GetSelectionRenderRange(DirtyTextRange selectionRange)
        {
            DirtyTextRange? result = null;

            int firstLineIndex, lastLineIndex;

            GetVisibleLines(out firstLineIndex, out lastLineIndex);

            int selectionStart = selectionRange.StartIndex;
            int selectionEnd = selectionRange.StartIndex + selectionRange.PositionsAdded;

            int viewportStart = _lineMetrics[firstLineIndex].Offset;
            int viewportEnd = _lineMetrics[lastLineIndex].EndOffset;

            if (viewportEnd >= selectionStart
                && viewportStart <= selectionEnd)
            {
                int rangeStart = Math.Max(viewportStart, selectionStart);
                int rangeSize = Math.Min(viewportEnd, selectionEnd) - rangeStart;

                result = new DirtyTextRange(rangeStart, rangeSize, rangeSize, fromHighlightLayer: true);
            }

            return result;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        // True when measure and arrange are valid.
        private bool IsLayoutValid
        {
            get
            {
                return this.IsMeasureValid && this.IsArrangeValid;
            }
        }

        // Current visible region in document space.
        private Rect Viewport
        {
            get
            {
                return _scrollData == null ? Rect.Empty :
                                             new Rect(_scrollData.HorizontalOffset, _scrollData.VerticalOffset, _scrollData.ViewportWidth, _scrollData.ViewportHeight);
            }
        }

        // True when background layout has not completed.
        private bool IsBackgroundLayoutPending
        {
            get
            {
                return CheckFlags(Flags.BackgroundLayoutPending);
            }
        }

        // Offset in pixels of the first line due to VerticalContentAlignment.
        private double VerticalAlignmentOffset
        {
            get
            {
                double offset;

                switch (((Control)_host).VerticalContentAlignment)
                {
                    case VerticalAlignment.Top:
                    case VerticalAlignment.Stretch:
                    default:
                        offset = 0;
                        break;

                    case VerticalAlignment.Center:
                        offset = this.VerticalPadding / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        offset = this.VerticalPadding;
                        break;
                }

                return offset;
            }
        }

        // Calculated TextAlignment property value.
        // Takes into account collisions between TextAlignment and HorizontalContentAlignment properties.
        //
        // TextAlignment always wins unless it has no local value and HorizontalContentAlignment does.
        //
        // In order of precedence:
        // 1. Local value on TextAlignment.
        // 2. Local value on HorizontalContentAlignment.
        // 3. Inherited/styled TextAlignment
        // 4. Inherited/styled HorizontalContentAlignment
        // 5. Inherited/styled/default TextAlignment.
        private TextAlignment CalculatedTextAlignment
        {
            get
            {
                Control host = (Control)_host;
                object o = null;

                BaseValueSource textAlignmentSource = DependencyPropertyHelper.GetValueSource(host, TextBox.TextAlignmentProperty).BaseValueSource;
                BaseValueSource horizontalAlignmentSource = DependencyPropertyHelper.GetValueSource(host, TextBox.HorizontalContentAlignmentProperty).BaseValueSource;


                if (textAlignmentSource == BaseValueSource.Local)
                {
                    return (TextAlignment)host.GetValue(TextBox.TextAlignmentProperty);
                }

                if (horizontalAlignmentSource == BaseValueSource.Local)
                {
                    o = host.GetValue(TextBox.HorizontalContentAlignmentProperty);
                    return HorizontalAlignmentToTextAlignment((HorizontalAlignment)o);
                }

                // if textAlignment has no inherited/styled value then
                // we'll check if there is inherited/styled value for HorizontalContentAlignmentProperty and take that.
                if ((textAlignmentSource == BaseValueSource.Default) &&
                    (horizontalAlignmentSource != BaseValueSource.Default))
                {
                    o = host.GetValue(TextBox.HorizontalContentAlignmentProperty);
                    return HorizontalAlignmentToTextAlignment((HorizontalAlignment)o);
                }

                // return iether inherited/styled/default TextAlignment
                return (TextAlignment)host.GetValue(TextBox.TextAlignmentProperty);
            }
        }

        // The delta between the current viewport height and the content height.
        // Returns zero when content height is greater than viewport height.
        private double VerticalPadding
        {
            get
            {
                double padding;
                Rect viewport = this.Viewport;

                if (viewport.IsEmpty)
                {
                    padding = 0;
                }
                else
                {
                    padding = Math.Max(0, viewport.Height - _contentSize.Height);
                }

                return padding;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Booleans for the _flags field.
        [System.Flags]
        private enum Flags
        {
            // When true, TextContainer listeners are hooked up.
            TextContainerListenersInitialized = 0x1,

            // When true, background layout is still running.
            BackgroundLayoutPending = 0x2,

            // Determines if an arrange has been requested from the highlight layer.
            // If this is true, then we should expect the dirty list to contain items in ArrangeVisuals.
            ArrangePendingFromHighlightLayer = 0x4,
        }

        // Caches state used across a measure/arrange calculation.
        // In addition to performance benefits, this ensures a consistent
        // view of property values across measure/arrange.
        private class TextCache
        {
            internal TextCache(TextBoxView owner)
            {
                _lineProperties = owner.GetLineProperties();
                _textRunCache = new TextRunCache();
                Control hostControl = (Control)owner.Host;
                TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(hostControl);
                _textFormatter = System.Windows.Media.TextFormatting.TextFormatter.FromCurrentDispatcher(textFormattingMode);
            }

            internal LineProperties LineProperties
            {
                get { return _lineProperties; }
            }

            internal TextRunCache TextRunCache
            {
                get { return _textRunCache; }
            }

            // Cached TextFormatter for this thread.
            internal TextFormatter TextFormatter
            {
                get
                {
                    return _textFormatter;
                }
            }

            private readonly LineProperties _lineProperties;
            private readonly TextRunCache _textRunCache;
            private TextFormatter _textFormatter;
        }

        // Line metrics array entry.
        private class LineRecord
        {
            internal LineRecord(int offset, TextBoxLine line)
            {
                _offset = offset;
                _length = line.Length;
                _contentLength = line.ContentLength;
                _width = line.Width;
            }

            internal int Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }

            internal int Length { get { return _length; } }

            internal int ContentLength { get { return _contentLength; } }

            internal double Width { get { return _width; } }

            internal int EndOffset { get { return _offset + _length; } }

            private int _offset;
            private readonly int _length; // we don't need this state, it could be calculated by looking at the next metric offset.
            private readonly int _contentLength;
            private readonly double _width;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // TextBox that owns this TextBoxView.
        private readonly ITextBoxViewHost _host;

        // Bounding box of the content, up to the point reached by background layout.
        private Size _contentSize;

        // The most recent constraint passed to MeasureOverride.
        // this.PreviousConstraint cannot be used because it can be affected
        // by Margin, Width/Min/MaxWidth propreties and ClipToBounds.
        private Size _previousConstraint;

        // Caches state used across a measure/arrange calculation.
        // In addition to performance benefits, this ensures a consistent
        // view of property values across measure/arrange.
        private TextCache _cache;

        // Height of any line, in pixels.
        private double _lineHeight;

        // Visuals tracked by GetVisualChild/VisualChilrenCount overrides.
        private List<TextBoxLineDrawingVisual> _visualChildren;

        // Array of cached line metrics.
        private List<LineRecord> _lineMetrics;

        // Array of cached line Visuals for the current viewport.
        private List<TextBoxLineDrawingVisual> _viewportLineVisuals;

        // Index of first line in the _viewportLineVisuals array.
        private int _viewportLineVisualsIndex;

        // IScrollInfo state/code.
        private ScrollData _scrollData;

        // List of invalidated regions created by TextContainer changes.
        private DtrList _dirtyList;

        // Timer used to disable background layout during user interaction.
        private DispatcherTimer _throttleBackgroundTimer;

        // Boolean flags, set with Flags enum.
        private Flags _flags;

        // Updated event listeners.
        private EventHandler UpdatedEvent;

        // Max time slice to run FullMeasureTick.
        private static readonly TimeSpan _maxMeasureTime = TimeSpan.FromMilliseconds(200);

        // Number of seconds to disable background layout after receiving
        // user input.
        private static readonly TimeSpan _throttleBackgroundTimeSpan = TimeSpan.FromSeconds(2);

        #endregion Private Fields
    }
}
