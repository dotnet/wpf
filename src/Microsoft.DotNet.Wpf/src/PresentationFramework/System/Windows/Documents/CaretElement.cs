// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Caret rendering visual.
//

namespace System.Windows.Documents
{
    using System.Security; // SecurityCritical, SecurityTreatAsSafe
    using System.Windows.Media; // Brush, Transform
    using System.Windows.Media.Animation; // AnimationClock
    using System.Windows.Controls; // ScrollViewer
    using MS.Win32; // SafeNativeMethods
    using MS.Internal; // DoubleUtil.AreClose(), Invariant.Assert
    using MS.Internal.Documents; // IFlowDocumentViewer
    using System.Runtime.InteropServices; // HandleRef
    using System.Collections.Generic; // List<TextSegment>
    using System.Windows.Interop;
    using System.Windows.Controls.Primitives;


// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691

    /// <summary>
    /// This class is sealed because it calls OnVisualChildrenChanged virtual in the
    /// constructor and it does not override it, but derived classes could.
    /// </summary>
    internal sealed class CaretElement : Adorner
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates new instance of CaretElement.
        /// </summary>
        /// <param name="textEditor">
        /// TextEditor that owns this Adorner.
        /// </param>
        /// <param name="isBlinkEnabled">
        /// Blinking for caret animation. Drag target caret does not need blinking,
        /// </param>
        internal CaretElement(TextEditor textEditor, bool isBlinkEnabled) : base(textEditor.TextView.RenderScope)
        {
            Invariant.Assert(textEditor.TextView != null && textEditor.TextView.RenderScope != null, "Assert: textView != null && RenderScope != null");

            _textEditor = textEditor;

            // Set the animation whether do it or not.
            _isBlinkEnabled = isBlinkEnabled;

            // caret position
            _left = 0.0;
            _top = 0.0;

            // caret dimensions
            _systemCaretWidth = SystemParameters.CaretWidth;
            _height = 0.0;

            // Set AllowDropProperty as "False" not to inherit the value from the ancestor.
            AllowDrop = false;

            _caretElement = new CaretSubElement();
            _caretElement.ClipToBounds = false;

            AddVisualChild(_caretElement);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return (_caretElement == null) ? 0 : 1; }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return _caretElement;
        }

        // HitTestCore override not to hit testable Caret.
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            // Return null not to hit testable for CaretElement.
            return null;
        }

        // Render override -- we render the selection here.
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_selectionGeometry != null)
            {
                FrameworkElement owner = GetOwnerElement();
                Brush selectionBrush = (Brush)owner.GetValue(TextBoxBase.SelectionBrushProperty);

                if (selectionBrush == null)
                {
                    return;
                }

                double selectionOpacity = (double)owner.GetValue(TextBoxBase.SelectionOpacityProperty);
                drawingContext.PushOpacity(selectionOpacity);

                Pen selectionOutlinePen = null;

                drawingContext.DrawGeometry(selectionBrush, selectionOutlinePen, _selectionGeometry);

                drawingContext.Pop();
            }
        }

        /// <summary>
        /// Measurement override.
        /// </summary>
        /// <param name="availableSize">
        /// Available size for the component
        /// </param>
        /// <returns>
        /// Return the size of the caret
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            _caretElement.InvalidateVisual();

            // Return the available width and height. Please don't
            // return AdornedElement.RenderSize since it will be scrolled
            // in case of the reder size is greater than the available size.
            // Reference bug#1068444.

            // We choose to return an arbitrary large value of double.MaxValue/2 in case of infinite height/width.
            // This is safer, because adding (even zero) margin to double.MaxValue causes available size to become infinite again.
            // UIElement.Measure enforces that MeasureCore can not return PositiveInfinity size even if given Infinite available size.
            return new Size(
                double.IsInfinity(availableSize.Width) ? double.MaxValue/2 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? double.MaxValue/2 : availableSize.Height);
            // Note we do not use _systemCaretWidth for caret width,
            // because italic caret would be clipped in this case.
            // We use maximum available visible width - as for height.
        }

        /// <summary>
        /// Arrange override.
        /// </summary>
        /// <param name="availableSize">
        /// Available size for the component
        /// </param>
        /// <returns>
        /// Return the size of the caret
        /// </returns>
        protected override Size ArrangeOverride(Size availableSize)
        {
            Point point;

            if (_pendingGeometryUpdate)
            {
                ((TextSelection)_textEditor.Selection).UpdateCaretState(CaretScrollMethod.None);
                _pendingGeometryUpdate = false;
            }

            point = new Point(_left, _top);

            _caretElement.Arrange(new Rect(point, availableSize));

            return availableSize;
        }

#endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Invalidates the caret render.
        /// Used in TextSelection (for caret) and in TextEditor (for drag target)
        /// </summary>
        /// <param name="visible">
        /// </param>
        /// <param name="caretRectangle">
        /// Rectangle relative to AdornedElement (textview) where caret should be drawn.
        /// Width must be zero to apply the system caret width except the interim caret that has
        /// the interim caret width.
        /// </param>
        /// <param name="caretBrush">
        /// Brush for drawing the caret
        /// </param>
        /// <param name="opacity">
        /// Opacity factor to apply to RenderContext when drawing the caret.
        /// </param>
        /// <param name="italic">
        /// Request to italic caret.
        /// </param>
        /// <param name="scrollMethod">
        /// Determines how the caret is scrolled into view.
        /// </param>
        /// <param name="scrollToOriginPosition">
        /// Request to scroll caret position with the scroll origin position.
        /// </param>
        internal void Update(bool visible, Rect caretRectangle, Brush caretBrush, double opacity, bool italic, CaretScrollMethod scrollMethod, double scrollToOriginPosition)
        {
            double newLeft;
            double newTop;
            double newHeight;
            double newWidth;
            bool positionChanged;

            Invariant.Assert(caretBrush != null, "Assert: caretBrush != null");

            // Make sure we're attached to the view.  We have to delay this work
            // until now because if we swap out a render scope on the fly, the
            // new one won't be hooked up to the visual tree until after its
            // style is rebuilt, which happens on an async layout update.
            EnsureAttachedToView();

            // Enforce caret refresh for the case when it appears after invisible state
            bool justAppearing = visible && !_showCaret;

            if (_showCaret != visible)
            {
                InvalidateVisual();
                _showCaret = visible;
            }

            // Update the caret brush.
            _caretBrush = caretBrush;
            _opacity = opacity;

            // Define new coordinates and dimensions of the caret.
            // We don't consider caret visibility here because even if the
            // caret is hidden, we need to calc the geometry info to scroll
            // the active edge of the selection into view.

            if (caretRectangle.IsEmpty || caretRectangle.Height <= 0)
            {
                newLeft = 0;
                newTop = 0;
                newHeight = 0;
                newWidth = 0;
            }
            else
            {
                newLeft = caretRectangle.X;
                newTop = caretRectangle.Y;
                newHeight = caretRectangle.Height;
                newWidth = SystemParameters.CaretWidth;
            }

            // Initialize flag requiring to refresh the caret
            positionChanged = justAppearing || italic != _italic;

            if (!DoubleUtil.AreClose(_left, newLeft))
            {
                _left = newLeft;
                positionChanged = true;
            }
            if (!DoubleUtil.AreClose(_top, newTop))
            {
                _top = newTop;
                positionChanged = true;
            }
            if (!caretRectangle.IsEmpty && _interimWidth != caretRectangle.Width)
            {
                _interimWidth = caretRectangle.Width;
                positionChanged = true;
            }
            if (!DoubleUtil.AreClose(_systemCaretWidth, newWidth))
            {
                _systemCaretWidth = newWidth;
                positionChanged = true;
            }
            if (!DoubleUtil.AreClose(_height, newHeight))
            {
                _height = newHeight;

                InvalidateMeasure();
            }

            // Refresh caret and ensure the caret to the view if the caret position is changed or
            // caret is currently out of view area which scrollToOriginPosition is set.
            // scrollToOriginPosition will be set properly to view the caret correctly if caret is
            // currently out of view boundary. For example, typing bidi characters on LTR flow direction
            // or typing western characters on RTL flow direction from the out of view.
            if (positionChanged || !double.IsNaN(scrollToOriginPosition))
            {
                _scrolledToCurrentPositionYet = false;
                RefreshCaret(italic);
            }

            if (scrollMethod != CaretScrollMethod.None && !_scrolledToCurrentPositionYet)
            {
                Rect scrollRectangle;

                //Set the interim width to show the interim caret in the interim mode.
                // We're providing enough space to also take care of an italic or bidi caret.
                scrollRectangle = new Rect(_left - CaretPaddingWidth, _top, CaretPaddingWidth * 2 + (IsInInterimState ? _interimWidth : _systemCaretWidth), _height);

                // If we're scrolling to one edge of the document or another,
                //  - Going backward, we want to scroll into view the left edge
                //    of the rect.  This is the default.
                //  - Going forward, we want to scroll into view the right edge
                //    of the rect, which takes some adjustment.
                if (!double.IsNaN(scrollToOriginPosition) && scrollToOriginPosition > 0)
                {
                    scrollRectangle.X += scrollRectangle.Width;
                    scrollRectangle.Width = 0;
                }

                switch (scrollMethod)
                {
                    case CaretScrollMethod.Simple:
                        DoSimpleScrollToView(scrollToOriginPosition, scrollRectangle);
                        break;

                    case CaretScrollMethod.Navigation:
                        DoNavigationalScrollToView(scrollToOriginPosition, scrollRectangle);
                        break;
                }
                _scrolledToCurrentPositionYet = true;
            }

            // Skip the animation if the animation isn't set. E.g. DragDrop caret.
            SetBlinkAnimation(visible, positionChanged);
        }

        // Scroll the caret rect into view with no allowance for surrounding text.
        private void DoSimpleScrollToView(double scrollToOriginPosition, Rect scrollRectangle)
        {
            // Scroll to the original position first to ensure of displaying the wrapped word with
            // the caret position. The scrollToOriginPosition is already base on both LTR and RTL
            // flow direction.
            if (!double.IsNaN(scrollToOriginPosition))
            {
                MS.Internal.Documents.TextViewBase.BringRectIntoViewMinimally(_textEditor.TextView, new Rect(scrollToOriginPosition, scrollRectangle.Y, scrollRectangle.Width, scrollRectangle.Height));

                // Since we've moved the viewport, and scrollRectangle is relative to the viewport. scrollRectangle
                // is no longer correct.  Adjust it by the distance we scrolled to make it correct.
                scrollRectangle.X -= scrollToOriginPosition;
            }

            // Now scroll to the caret position
            MS.Internal.Documents.TextViewBase.BringRectIntoViewMinimally(_textEditor.TextView, scrollRectangle);
        }

        // Scroll the caret rect into view, adding a "buffer" of surrounding text
        // porportional to the viewport size.
        private void DoNavigationalScrollToView(double scrollToOriginPosition, Rect targetRect)
        {
            // Find the scroller from the render scope
            ScrollViewer scroller = _textEditor._Scroller as ScrollViewer;

            if (scroller != null)
            {
                Point targetPoint = new Point(targetRect.Left, targetRect.Top);

                GeneralTransform transform = _textEditor.TextView.RenderScope.TransformToAncestor(scroller);

                if (transform.TryTransform(targetPoint, out targetPoint))
                {
                    double scrollerWidth = scroller.ViewportWidth;
                    double scrollerHeight = scroller.ViewportHeight;
                    double deltaToScroll;

                    // Scroll up or down if the moving position.Y is outside of viewport
                    if (targetPoint.Y < 0 || targetPoint.Y + targetRect.Height > scrollerHeight)
                    {
                        if (targetPoint.Y < 0)
                        {
                            // Scroll up
                            deltaToScroll = Math.Abs(targetPoint.Y);
                            scroller.ScrollToVerticalOffset(Math.Max(0, scroller.VerticalOffset - deltaToScroll - scrollerHeight / 4));
                        }
                        else
                        {
                            // Scroll down
                            deltaToScroll = targetPoint.Y + targetRect.Height - scrollerHeight;
                            scroller.ScrollToVerticalOffset(Math.Min(scroller.ExtentHeight, scroller.VerticalOffset + deltaToScroll + scrollerHeight / 4));
                        }
                    }

                    // Scroll line left or right if the moving position.X is outside of viewport
                    if (targetPoint.X < 0 || targetPoint.X > scrollerWidth)
                    {
                        if (targetPoint.X < 0)
                        {
                            // Scroll the left line
                            deltaToScroll = Math.Abs(targetPoint.X);
                            scroller.ScrollToHorizontalOffset(Math.Max(0, scroller.HorizontalOffset - deltaToScroll - scrollerWidth / 4));
                        }
                        else
                        {
                            // Scroll the right line
                            deltaToScroll = targetPoint.X - scrollerWidth;
                            scroller.ScrollToHorizontalOffset(Math.Min(scroller.ExtentWidth, scroller.HorizontalOffset + deltaToScroll + scrollerWidth / 4));
                        }
                    }
                }
            }
            else
            {
                // No heuristics implemented for document viewer horizontal scrolling,
                // as a result we will bring each character or word into view on demand.
                // (Unlike the scrollviewer heuristic above where we bring 1/4th of scroller width into view.)
                if (!_textEditor.Selection.MovingPosition.HasValidLayout && _textEditor.TextView != null && _textEditor.TextView.IsValid)
                {
                    DoSimpleScrollToView(scrollToOriginPosition, targetRect);
                }
            }
        }

        /// <summary>
        /// Updates selection geometry.
        /// </summary>
        internal void UpdateSelection()
        {
            Geometry previousSelectionGeometry = _selectionGeometry;

            _selectionGeometry = null;

            if (!_textEditor.Selection.IsEmpty)
            {
                EnsureAttachedToView();

                List<TextSegment> textSegments = _textEditor.Selection.TextSegments;

                for (int i = 0; i < textSegments.Count; i++)
                {
                    TextSegment segment = textSegments[i];
                    Geometry geometry = _textEditor.Selection.TextView.GetTightBoundingGeometryFromTextPositions(segment.Start, segment.End);
                    AddGeometry(ref _selectionGeometry, geometry);
                }
            }

            if (_selectionGeometry != previousSelectionGeometry)
            {
                // Request to re-render the selection.
                RefreshCaret(_italic);
            }
        }

        internal static void AddGeometry(ref Geometry geometry, Geometry addedGeometry)
        {
            if (addedGeometry != null)
            {
                if (geometry == null)
                {
                    geometry = addedGeometry;
                }
                else
                {
                    geometry = Geometry.Combine(geometry, addedGeometry, GeometryCombineMode.Union, null, CaretElement.c_geometryCombineTolerance, ToleranceType.Absolute);
                }
            }
        }

        internal static void ClipGeometryByViewport(ref Geometry geometry, Rect viewport)
        {
            if (geometry != null)
            {
                Geometry viewportGeometry = new RectangleGeometry(viewport);
                geometry = Geometry.Combine(geometry, viewportGeometry, GeometryCombineMode.Intersect, null, CaretElement.c_geometryCombineTolerance, ToleranceType.Absolute);
            }
        }

        internal static void AddTransformToGeometry(Geometry targetGeometry, Transform transformToAdd)
        {
            if (targetGeometry != null && transformToAdd != null)
            {
                targetGeometry.Transform = (targetGeometry.Transform == null || targetGeometry.Transform.IsIdentity)
                    ? transformToAdd
                    : new MatrixTransform(targetGeometry.Transform.Value * transformToAdd.Value);
            }
        }

        /// <summary>
        /// Hide the caret render.
        /// </summary>
        internal void Hide()
        {
            if (_showCaret)
            {
                // Hide the caret not to render the caret.
                _showCaret = false;

                InvalidateVisual();

                // Skip the animation if the animation isn't set. E.g. DragDrop caret.
                SetBlinking(/*isBlinkEnabled:*/false);

                // Destroy Win32 caret
                Win32DestroyCaret();
            }
        }

        /// <summary>
        /// Requires to visually invalidate and update a caret without calculating
        /// its size and coordinates (assuming that they are the same as in preceding Update).
        /// Used to force caret redraing when input language or italic condition is changed.
        /// Also called internally from Update method after new position and size is
        /// calculated and set to the caret.
        /// </summary>
        /// <param name="italic">
        /// True specifies that the caret must be inclined to indicate italic state.
        /// </param>
        internal void RefreshCaret(bool italic)
        {
            // Store italic status of the caret
            _italic = italic;

            // Cache _adornerLayer to avoid reentrancy problems during Update.
            AdornerLayer adornerLayer = _adornerLayer;

            if (adornerLayer != null)
            {
                Adorner[] adorners = adornerLayer.GetAdorners(this.AdornedElement);

                if (adorners != null)
                {
                    // Verify we still adorn our element.
                    // We have a persistent but still unexplained stress bug where
                    // the caret adorner is mysteriously detached
                    for (int i = 0; i < adorners.Length; i++)
                    {
                        if (adorners[i] == this)
                        {
                            adornerLayer.Update(this.AdornedElement);
                            adornerLayer.InvalidateVisual();
                            break;
                        }
                    }
                }
            }
        }

        // Removes this CaretElement from its AdornerLayer.
        internal void DetachFromView()
        {
            SetBlinking(/*isBlinkEnabled:*/false);

            if (_adornerLayer != null)
            {
                _adornerLayer.Remove(this);
                _adornerLayer = null;
            }
        }

        internal void SetBlinking(bool isBlinkEnabled)
        {
            if (isBlinkEnabled != _isBlinkEnabled)
            {
                // Stopping blinking
                if (_isBlinkEnabled)
                {
                    if (_blinkAnimationClock != null)
                    {
                        if (_blinkAnimationClock.CurrentState == ClockState.Active)
                        {
                            _blinkAnimationClock.Controller.Stop();
                        }
                    }
                }

                // Actual blinking will be started in Update method call - if needed
                _isBlinkEnabled = isBlinkEnabled;

                if (isBlinkEnabled)
                {
                    // Create Win32 caret.
                    Win32CreateCaret();
                }
                else
                {
                    // Destory Win32 caret.
                    Win32DestroyCaret();
                }
            }
        }

        internal void UpdateCaretBrush(Brush caretBrush)
        {
            _caretBrush = caretBrush;
            _caretElement.InvalidateVisual();
        }

        /// <summary>
        /// Performs the actual rendering of the caret on the given context.  Called by
        /// CaretSubElement.
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <remarks>This method is on CaretElement instead of CaretSubElement because CaretElement
        /// knows all of the necessary data, and conceptually CaretSubElement only exists to provide
        /// a rendering surface.</remarks>
        internal void OnRenderCaretSubElement(DrawingContext context)
        {
            // Sync up Win32 caret position with Avalon caret position.
            Win32SetCaretPos();

            if (_showCaret)
            {
                TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

                Invariant.Assert(!(_italic && this.IsInInterimState), "Assert !(_italic && IsInInterimState)");

                // Drawing context's pushed count to pop it up
                int contextPushedCount = 0;

                // Apply internally requested opacity.
                context.PushOpacity(_opacity);
                contextPushedCount++;

                // Apply italic transformation
                if (_italic && !(threadLocalStore.Bidi))
                {
                    // Rotate transform 20 degree for italic that is the based on 'H' italic degree.
                    // NOTE: The angle of italic caret is constant. This is Word behavior
                    // established after usability studies with conditional angle dependent
                    // on font properties - they discovered that variations look annoying.
                    // NOTE: We ignore _italic setting in _bidi case. This is Word behavior.
                    // When flow direction is Right to Left, we need to reverse the caret transform.
                    //
                    // Get the flow direction which is the flow direction of AdornedElement.
                    // CaretElement is rendering the caret that based on AdornedElement, so we can
                    // render the right italic caret whatever the text content set the flow direction.
                    FlowDirection flowDirection = (FlowDirection)AdornedElement.GetValue(FlowDirectionProperty);
                    context.PushTransform(new RotateTransform(
                    flowDirection == FlowDirection.RightToLeft ? -20 : 20,

                        0,  _height));

                    contextPushedCount++;
                }

                if (this.IsInInterimState || _systemCaretWidth > DefaultNarrowCaretWidth)
                {
                    // Make the block caret partially transparent to avoid obstructing text.
                    context.PushOpacity(CaretOpacity);
                    contextPushedCount++;
                }

                if (this.IsInInterimState)
                {
                    // Render the interim block caret as the specified interim block caret width.
                    context.DrawRectangle(_caretBrush, null, new Rect(0, 0, _interimWidth, _height));
                }
                else
                {
                    // Snap the caret to device pixels.
                    if (!_italic || threadLocalStore.Bidi)
                    {
                        GuidelineSet guidelineSet = new GuidelineSet(new double[] { -(_systemCaretWidth / 2), _systemCaretWidth / 2 }, null);
                        context.PushGuidelineSet(guidelineSet);
                        contextPushedCount++;
                    }

                    // If we don't snap, the caret will render as a 2 pixel wide rect, one pixel in each bordering char bounding box.
                    context.DrawRectangle(_caretBrush, null, new Rect(-(_systemCaretWidth / 2), 0, _systemCaretWidth, _height));
                }

                if (threadLocalStore.Bidi)
                {
                    // Set the Bidi caret indicator width. TextBox/RichTextBox control must have
                    // the enough margin to display BiDi indicator.
                    double bidiCaretIndicatorWidth = BidiCaretIndicatorWidth;

                    // Get the flow direction which is the flow direction of AdornedElement.
                    // Because CaretElement is rendering the caret that based on AdornedElement.
                    // With getting the flow direction, we can render the BiDi caret indicator correctly
                    // whatever AdornedElement's flow direction is set.
                    FlowDirection flowDirection = (FlowDirection)AdornedElement.GetValue(FlowDirectionProperty);
                    if (flowDirection == FlowDirection.RightToLeft)
                    {
                        // BiDi caret indicator should always direct by the right to left
                        bidiCaretIndicatorWidth = bidiCaretIndicatorWidth * (-1);
                    }

                    // Draw BIDI caret to indicate the coming input is BIDI characters.
                    // Shape is a little flag oriented to the left - as in Word.
                    // Orientation does not depend on anything (which seems to be Word behavior).
                    //  Confirm that flag orientation is constant in all cases.
                    PathGeometry pathGeometry;
                    PathFigure pathFigure;

                    pathGeometry = new PathGeometry();
                    pathFigure = new PathFigure();
                    pathFigure.StartPoint = new Point(0, 0);
                    pathFigure.Segments.Add(new LineSegment(new Point(-bidiCaretIndicatorWidth, 0), true));
                    pathFigure.Segments.Add(new LineSegment(new Point(0, _height / BidiIndicatorHeightRatio), true));
                    pathFigure.IsClosed = true;

                    pathGeometry.Figures.Add(pathFigure);
                    context.DrawGeometry(_caretBrush, null, pathGeometry);
                }

                // Pop the drawing context if pushed for italic or opacity setting
                for (int i = 0; i < contextPushedCount; i++)
                {
                    context.Pop();
                }
            }
            else
            {
                // Destroy Win32 caret.
                Win32DestroyCaret();
            }
        }

        // ITextView.Updated event listener.  Called by the TextSelection.
        internal void OnTextViewUpdated()
        {
            // At this point, we potentially have to update our selection geometry,
            // but the TextView is still dirty.
            //
            // We cannot simply delay the work with a Dispatcher.BeginInvoke,
            // because that will introduce latency (flicker) as the selection/caret
            // update lags behind the text refresh.
            //
            // So we invalidate this Adorner's arrange so that we can re-evaluate
            // in the same layout pass as the view update, after this method returns.
            _pendingGeometryUpdate = true;
            InvalidateArrange();
        }

        // ------------------------------------------------------------
        //
        // Internal Properties for Test Automation
        //
        // ------------------------------------------------------------

        // This method is used for text automation in DrtEditing
        private static CaretElement Debug_CaretElement
        {
            get
            {
                TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;
                return ((ITextSelection)TextEditor._ThreadLocalStore.FocusedTextSelection).CaretElement;
            }
        }

        // This method is used for text automation in DrtEditing
        private static FrameworkElement Debug_RenderScope
        {
            get
            {
                return ((ITextSelection)TextEditor._ThreadLocalStore.FocusedTextSelection).TextView.RenderScope as FrameworkElement;  // TextBlock / TextFlow
            }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal Geometry SelectionGeometry
        {
            get
            {
                return _selectionGeometry;
            }
        }

        internal bool IsSelectionActive
        {
            get
            {
                return _isSelectionActive;
            }
            set
            {
                _isSelectionActive = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Returns the outer element that owns this CaretElement. (One of the text boxes
        // or FlowDocument readers.)
        private FrameworkElement GetOwnerElement()
        {
            return GetOwnerElement(_textEditor.UiScope);
        }

        internal static FrameworkElement GetOwnerElement(FrameworkElement uiScope)
        {
            if (uiScope is IFlowDocumentViewer)
            {
                // We are in one of the internal FlowDocument viewers owned by FlowDocumentReader.
                // Return the FlowDocumentReader since this class holds the public propertie values.
                DependencyObject node = uiScope;

                while (node != null)
                {
                    if (node is FlowDocumentReader)
                    {
                        return (FrameworkElement)node;
                    }

                    node = VisualTreeHelper.GetParent(node);
                }

                return null;
            }

            return uiScope;
        }

        // Adds this CaretElement to the scoping AdornerLayer, if any.
        private void EnsureAttachedToView()
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(_textEditor.TextView.RenderScope);
            if (layer == null)
            {
                // There is no AdornerLayer available.  Clear cached value and exit.
                if (_adornerLayer != null)
                {
                    // We're currently in a layer that doesn't exist.
                    _adornerLayer.Remove(this);
                }

                _adornerLayer = null;
                return;
            }

            if (_adornerLayer == layer)
            {
                // We're already using the correct AdornerLayer.
                return;
            }

            if (_adornerLayer != null)
            {
                // We're currently in the wrong layer.
                _adornerLayer.Remove(this);
            }

            // Add ourselves to the correct layer.
            _adornerLayer = layer;
            _adornerLayer.Add(this, ZOrderValue);
        }

        // Inits or resets an opacity animation used to make the caret blink.
        private void SetBlinkAnimation(bool visible, bool positionChanged)
        {
            if (!_isBlinkEnabled)
            {
                return;
            }

            // NB: "Blink" is the period of time between successive
            // state changes in the caret animation.  "Flash" is
            // 2 * blink, the period of time to transition from
            // visible, to hidden, to visible again.
            int blinkInterval = Win32GetCaretBlinkTime();
            if (blinkInterval > 0) // -1 if the caret shouldn't blink.
            {
                Duration blinkDuration = new Duration(TimeSpan.FromMilliseconds(blinkInterval * 2));

                if (_blinkAnimationClock == null || _blinkAnimationClock.Timeline.Duration != blinkDuration)
                {
                    DoubleAnimationUsingKeyFrames blinkAnimation = new DoubleAnimationUsingKeyFrames();
                    blinkAnimation.BeginTime = null;
                    blinkAnimation.RepeatBehavior = RepeatBehavior.Forever;
                    blinkAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromPercent(0.0)));
                    blinkAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.5)));
                    blinkAnimation.Duration = blinkDuration;

                    // Reduce desired framerate from 60 to 10, to reduce number of no-op renders.
                    // This significantly improves typing responsiveness on low end GPUs for RichTextBox.
                    Timeline.SetDesiredFrameRate(blinkAnimation, 10);

                    _blinkAnimationClock = blinkAnimation.CreateClock();
                    _blinkAnimationClock.Controller.Begin();

                    _caretElement.ApplyAnimationClock(UIElement.OpacityProperty, _blinkAnimationClock);
                }
            }
            else if (_blinkAnimationClock != null)
            {
                // No blink (control panel option).
                _caretElement.ApplyAnimationClock(UIElement.OpacityProperty, null);
                _blinkAnimationClock = null;
            }

            if (_blinkAnimationClock != null)
            {
                // Disable the animation when the caret isn't rendered
                // to get better perf.
                if (visible && (!(_blinkAnimationClock.CurrentState == ClockState.Active) || positionChanged))
                {
                    // Start the blinking animation for caret
                    _blinkAnimationClock.Controller.Begin();
                }
                else if (!visible)
                {
                    // Stop the blinking animation if the caret isn't visible even we start it yet.
                    // Because ApplyAnimationClock() calling can start the animation without
                    // the calling of Begin() which case is the auto selection(with DatePicker).
                    _blinkAnimationClock.Controller.Stop();
                }
            }
        }

        // Create Win32 caret to sync up Avalon caret with Win32 application and show Win32 caret
        // which is the empty bitmap. This call will generate the accessiblity event for caret so that
        // Win32 application have the compatibility to handle the caret event which is Magnifier or Tablet Tip.
        private void Win32CreateCaret()
        {
            if (!_isSelectionActive)
            {
                // We do not want to interfere with Win32 caret
                // if this Adorner isnt representing active selection
                return;
            }

            // Create Win32 caret if the height of caret is changed or
            // doesn't exist Win32 caret.
            if (!_win32Caret || _win32Height != _height)
            {
                // Create Win32 caret to support Win32 application(like Magnifier) that want to
                // sync Avalon caret position.
                IntPtr hwnd = IntPtr.Zero;
                PresentationSource source = PresentationSource.CriticalFromVisual(this);

                if (source != null)
                {
                        hwnd = (source as IWin32Window).Handle;
                }

                if (hwnd != IntPtr.Zero)
                {
                    // Convert _height (fixed at 96 dpi) to device units.
                    double deviceHeight = source.CompositionTarget.TransformToDevice.Transform(new Point(0, _height)).Y;

                    // Win32 CreateCaret automatically destroys the previous caret shape,
                    // if any, regardless of the window that owns the caret.

                    // Create and show Win32 empty caret for win32 compatibility.
                    // Creating Win32 empty caret and show it will generate the accesibility event
                    // so that Win32 application will have the compatibility who listen the caret event.
                    // Specified height with the current caret height will sync the win32 caret which
                    // Win32 Magnifer rely on the caret height to scroll their window.
                    NativeMethods.BitmapHandle bitmap = UnsafeNativeMethods.CreateBitmap(/*width*/ 1, /*height*/ ConvertToInt32(deviceHeight), /*panels*/ 1, /*bitsPerPixel*/ 1, /*bits*/ null);

                    // Specified width and height as zero since they will be ignored by setting the bitmap.
                    bool returnValue = UnsafeNativeMethods.CreateCaret(new HandleRef(null, hwnd), bitmap, /*width*/ 0, /*height*/ 0);

                    int win32Error = Marshal.GetLastWin32Error();
                    if (returnValue)
                    {
                        _win32Caret = true;
                        _win32Height = _height;
                    }
                    else
                    {
                        _win32Caret = false;

                        // Throw the Win32 exception with GetLastWin32Error.
                        throw new System.ComponentModel.Win32Exception(win32Error);
                    }
                }
            }
        }

        // Destroy Win32 caret if we create it with checking Win32 error.
        private void Win32DestroyCaret()
        {
            if (!_isSelectionActive)
            {
                // We do not want to interfere with Win32 caret
                // if this Adorner isnt representing active selection
                return;
            }

            // We only destroy the caret what we created a Win32 caret.
            if (_win32Caret)
            {
                // Destroy Win32 caret.
                bool returnValue = SafeNativeMethods.DestroyCaret();
                int win32Error = Marshal.GetLastWin32Error();
                if (!returnValue)
                {
                    // Suppress Win32 error of failing DestroyCaret(), because CreateCaret()can
                    // automatically destroys caret resource, if any, regardless of the owns caret.
                    // Mshtml module can create Win32 caret when Avalon is loaded by IE, so our Win32
                    // caret resousce is already destroyed.
                    //throw new System.ComponentModel.Win32Exception(win32Error);
                }

                _win32Caret = false;
                _win32Height = 0;
            }
        }

        // Set Win32 caret position with checking Win32 error.
        private void Win32SetCaretPos()
        {
            if (!_isSelectionActive)
            {
                // We do not want to interfere with Win32 caret
                // if this Adorner isnt representing active selection
                return;
            }

            // Create Win32 caret if win32 caret isn't created yet or destroyed already.
            if (!_win32Caret)
            {
                Win32CreateCaret();
            }

            // Get the presentation source to find a root visual.
            PresentationSource source = null;
            source = PresentationSource.CriticalFromVisual(this);
            if (source != null)
            {
                // Calculate the current caret position then transform the point to the window's client position
                // so that Win32 applicatioin like as Magnifer(MSAA) or Tablet Ink icon can track the right
                // caret position by calling Win32 SetCaretPos that will generate MSAA event message
                // as EVENT_OBJECT_LOCATIONCHANGE.
                Point win32CaretPoint = new Point(0, 0);
                GeneralTransform transform = _caretElement.TransformToAncestor(source.RootVisual);
                if (!transform.TryTransform(win32CaretPoint, out win32CaretPoint))
                {
                    // Reset the win32 caret point as zero if TryTransform is failed
                    win32CaretPoint = new Point(0, 0);
                }

                // Convert visual point (fixed at 96 dpi) to device units.
                win32CaretPoint = source.CompositionTarget.TransformToDevice.Transform(win32CaretPoint);

                bool win32Return = SafeNativeMethods.SetCaretPos(ConvertToInt32(win32CaretPoint.X), ConvertToInt32(win32CaretPoint.Y));
                // GetLastWin32Error() have to be called immediately not to break FxCop rule
                // even though we ignore the first fail of SetCaretPos.
                int win32Error = Marshal.GetLastWin32Error();
                if (!win32Return)
                {
                    // Suppress the first chance fail of Win32 SetCaretPos. Because Win32 caret can be
                    // created by Win32 application(like as mshtml.dll) that destroying our avalon Win32
                    // caret resource to update Avalon caret position to the system. So we try to create
                    // our own Win32 caret resource here and try to set the caret position again.
                    // Throw the exception if we still have a fail of SetCaretPos on the second time.
                    _win32Caret = false;
                    Win32CreateCaret();

                    win32Return = SafeNativeMethods.SetCaretPos(ConvertToInt32(win32CaretPoint.X), ConvertToInt32(win32CaretPoint.Y));
                    win32Error = Marshal.GetLastWin32Error();
                    if (!win32Return)
                    {
                        // Throw the Win32 exception with GetLastWin32Error.
                        throw new System.ComponentModel.Win32Exception(win32Error);
                    }
                }
            }
        }

        // Converts a double into a 32 bit integer, truncating values that
        // exceed Int32.MinValue or Int32.MaxValue.
        private int ConvertToInt32(double value)
        {
            int i;

            if (double.IsNaN(value))
            {
                i = 0;
            }
            else if (value < Int32.MinValue)
            {
                i = Int32.MinValue;
            }
            else if (value > Int32.MaxValue)
            {
                i = Int32.MaxValue;
            }
            else
            {
                i = Convert.ToInt32(value);
            }

            return i;
        }

        // Get the system caret blink time with checking Win32 error.
        private int Win32GetCaretBlinkTime()
        {
            Invariant.Assert(_isSelectionActive, "Blink animation should only be required for an owner with active selection.");

            // Disable PreSharp#6523 - Win32 GetCaretBlinkTime can return "0"
            // without the error if SetCaretBlinkTime set as "0".
#pragma warning disable 6523

            int caretBlinkTime = (int)SafeNativeMethods.GetCaretBlinkTime();
            if (caretBlinkTime == 0)
            {
                // Return "-1" which is no blinking caret instead of throwing
                // exception.
                return -1;
            }

#pragma warning restore 6523

            return caretBlinkTime;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsInInterimState
        {
            get
            {
                // Returns true if the interim width is specified and shows
                // the interim block caret.
                return _interimWidth != 0;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // BiDi caret indicator width.
        internal const double BidiCaretIndicatorWidth = 2.0;

        // Caret padding width to ensure the visible caret for Bidi and Italic.
        // Control(TextBox/RichTextBox) must have the enough padding to display
        // BiDi and Italic caret indicator.
        internal const double CaretPaddingWidth = 5.0;

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        private class CaretSubElement : UIElement
        {
            internal CaretSubElement()
            {
            }

            // HitTestCore override not to hit testable Caret.
            protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
            {
                // Return null not to hit testable for CaretElement.
                return null;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                ((CaretElement)_parent).OnRenderCaretSubElement(drawingContext);
            }
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // TextEditor that owns this adorner.
        private readonly TextEditor _textEditor;

        // If false, the caret is hidden and should not be rendered.
        private bool _showCaret;

        // If false, we dont interfere with Win32 Caret
        private bool _isSelectionActive;

        // Our blink opacity animation.
        private AnimationClock _blinkAnimationClock;

        // left offset of caret
        private double _left;

        // top offset of caret
        private double _top;

        // width of caret
        private double _systemCaretWidth;

        // width of interim caret
        private double _interimWidth;

        // height of caret
        private double _height;

        // height of Win32 caret
        private double _win32Height;

        // flag to control animation.
        private bool _isBlinkEnabled;

        // caret brush.
        private Brush _caretBrush;

        // opacity factor to apply to RenderContext when drawing caret.
        private double _opacity;

        // AdornerLayer holding this caret visual.
        private AdornerLayer _adornerLayer;

        // italic caret
        private bool _italic;

        // win32 caret
        private bool _win32Caret;

        // Caret opacity percent
        private const double CaretOpacity = 0.5;

        // BiDi caret indicator height ratio of caret
        private const double BidiIndicatorHeightRatio = 10.0;

        // default narrow caret width
        private const double DefaultNarrowCaretWidth = 1.0;

        //  selection related data
        private Geometry _selectionGeometry;
        internal const double c_geometryCombineTolerance = 1e-4;
        internal const double c_endOfParaMagicMultiplier = 0.5;

        // ZOrder
        internal const int ZOrderValue = System.Int32.MaxValue / 2;

        // child element on which we render the caret (selection is rendered on the adorner itself)
        private readonly CaretSubElement _caretElement;

        // Flag set true after calls to OnTextViewUpdated.
        // When true, we need to re-evaluate the selection geometry on the
        // next Arrange.
        private bool _pendingGeometryUpdate;

        // Flag set when we scroll and cleared when the position changes.
        // Used in Update() to optimize when we scroll.
        private bool _scrolledToCurrentPositionYet;

        #endregion Private Fields
    }
}

