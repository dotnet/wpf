// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: A Component of TextEditor supporting mouse gestures.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Globalization;
    using System.Threading;
    using System.ComponentModel;
    using System.Text;
    using System.Collections; // ArrayList
    using System.Runtime.InteropServices;

    using System.Windows.Threading;
    using System.Windows.Input;
    using System.Windows.Controls; // ScrollChangedEventArgs
    using System.Windows.Controls.Primitives;  // CharacterCasing, TextBoxBase
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using System.Windows.Markup;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal static class TextEditorMouse
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers all text editing command handlers for a given control type
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners)
        {
            if (registerEventListeners)
            {
                // Cursor Behavior
                EventManager.RegisterClassHandler(controlType, Mouse.QueryCursorEvent, new QueryCursorEventHandler(OnQueryCursor));
                // Selection Building
                EventManager.RegisterClassHandler(controlType, Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
                EventManager.RegisterClassHandler(controlType, Mouse.MouseMoveEvent, new MouseEventHandler(OnMouseMove));
                EventManager.RegisterClassHandler(controlType, Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp));
            }

            // Disable mouse move feeding on mouse down + mouse wheel to workaround scroll-into-view problems.
            // See bug 1639819.
#if DISABLED_FOR_BUG_1639819
            EventManager.RegisterClassHandler(controlType, ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
#endif
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Sets the caret in response to a mouse down or mouse up event.
        internal static void SetCaretPositionOnMouseEvent(TextEditor This, Point mouseDownPoint, MouseButton changedButton, int clickCount)
        {
            // Get the character position of the mouse event.
            ITextPointer cursorPosition = This.TextView.GetTextPositionFromPoint(mouseDownPoint, /*snapToText:*/true);

            if (cursorPosition == null)
            {
                // Cursor is between pages in a document viewer.
                MoveFocusToUiScope(This);
                return;
            }

            // Forget previously suggested horizontal position
            TextEditorSelection._ClearSuggestedX(This);

            // Discard typing undo unit merging
            TextEditorTyping._BreakTypingSequence(This);

            // Clear springload formatting
            if (This.Selection is TextSelection)
            {
                ((TextSelection)This.Selection).ClearSpringloadFormatting();
            }

            // Clear flags for forcing word and paragraphexpansion
            // (which should be true only in case of doubleClick+drag or tripleClick+drag)
            This._forceWordSelection = false;
            This._forceParagraphSelection = false;

            if (changedButton == MouseButton.Right || clickCount == 1)
            {
                // If mouse clicked within selection enter dragging mode, otherwise start building a selection
                if (changedButton != MouseButton.Left || !This._dragDropProcess.SourceOnMouseLeftButtonDown(mouseDownPoint))
                {
                    // Mouse down happend outside of current selection
                    // so position the selection at the clicked location.
                    This.Selection.SetSelectionByMouse(cursorPosition, mouseDownPoint);
                }
            }
            else if (clickCount == 2 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0 && This.Selection.IsEmpty)
            {
                // Double click only works when Shift is not pressed
                This._forceWordSelection = true;
                This._forceParagraphSelection = false;
                This.Selection.SelectWord(cursorPosition);
            }
            else if (clickCount == 3 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                // Triple click only works when Shift is not pressed
                if (This.AcceptsRichContent)
                {
                    This._forceParagraphSelection = true;
                    This._forceWordSelection = false;
                    This.Selection.SelectParagraph(cursorPosition);
                }
                else
                {
                    This.Selection.Select(This.TextContainer.Start, This.TextContainer.End);
                }
            }
        }

        // Determine whether the given point is within interactive area for the TextEditor.
        // Note that the passed point must be relative to the UiScope.
        internal static bool IsPointWithinInteractiveArea(TextEditor textEditor, Point point)
        {
            bool interactiveArea;
            GeneralTransform transform;
            ITextPointer position;

            interactiveArea = IsPointWithinRenderScope(textEditor, point);
            if (interactiveArea)
            {
                interactiveArea = textEditor.TextView.IsValid;
                if (interactiveArea)
                {
                    // Transform point to TextView.RenderScope coordinates.
                    transform = textEditor.UiScope.TransformToDescendant(textEditor.TextView.RenderScope);
                    if (transform != null)
                    {
                        transform.TryTransform(point, out point);
                    }
                    position = textEditor.TextView.GetTextPositionFromPoint(point, true);
                    interactiveArea = (position != null);
                }
            }
            return interactiveArea;
        }

        // MouseDownEvent handler - used both for Left and Right mouse buttons
        internal static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            // If UiScope has a ToolTip and it is open, any keyboard/mouse activity should close the tooltip.
            This.CloseToolTip();

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                return;
            }

            // Ignore the event if the attached scope is not focusable content.
            if (!This.UiScope.Focusable)
            {
                return;
            }

            // MITIGATION: NESTED_MESSAGE_PUMPS_INTERFERE_WITH_INPUT
            // This is a very specific fix for a case where someone displayed a dialog
            // box in response to mouse down.  In general, this entire routine needs
            // to be written to handle that fact that any state can change whenever
            // you call out.  See ButtonBase.OnMouseLeftButtonDown for an example.
            if (e.ButtonState == MouseButtonState.Released)
            {
                return;
            }

            e.Handled = true;

            // Start with moving the focus into this control.
            MoveFocusToUiScope(This);
            if (This.UiScope != Keyboard.FocusedElement)
            {
                return;
            }

            // If this is a right-click, we're done after setting
            // the focus.  Caret is position is only updated when a
            // context menu opens.
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (This.TextView == null)
            {
                return;
            }

            // Finalize any active IME compositions.
            // We have to do this async because it will potentially
            // invalidate layout.
            This.CompleteComposition();

            if (!This.TextView.IsValid)
            {
                // Do we need to UpdateLayout in other scenarios (MouseMove, MouseUp, etc)?
                // A PropertyTrigger can cause an invalidation merely by setting a property value, thereby
                // breaking our behavior.
                This.TextView.RenderScope.UpdateLayout();
                if (This.TextView == null || !This.TextView.IsValid)
                {
                    return;
                }
            }

            if (!IsPointWithinInteractiveArea(This, e.GetPosition(This.UiScope)))
            {
                // Mouse down happened over padding area or chrome instead of RenderScope; just set focus
                return;
            }

            // Scale back any background layout in progress.
            This.TextView.ThrottleBackgroundTasksForUserInput();

            // Get the mouse down position.
            Point mouseDownPoint = e.GetPosition(This.TextView.RenderScope);

            // Check if we're at a position where we need to begin a resize operation for table column
            if (TextEditor.IsTableEditingEnabled && TextRangeEditTables.TableBorderHitTest(This.TextView, mouseDownPoint))
            {
                // Set up resize information, and create adorner
                This._tableColResizeInfo = TextRangeEditTables.StartColumnResize(This.TextView, mouseDownPoint);
                Invariant.Assert(This._tableColResizeInfo != null);

                This._mouseCapturingInProgress = true;
                try
                {
                    This.UiScope.CaptureMouse();
                }
                finally
                {
                    This._mouseCapturingInProgress = false;
                }
            }
            else
            {
                This.Selection.BeginChange();
                try
                {
                    SetCaretPositionOnMouseEvent(This, mouseDownPoint, e.ChangedButton, e.ClickCount);

                    This._mouseCapturingInProgress = true;
                    This.UiScope.CaptureMouse();
                }
                finally
                {
                    This._mouseCapturingInProgress = false;
                    This.Selection.EndChange();
                }
            }
        }

        // MouseMoveEvent handler.
        internal static void OnMouseMove(object sender, MouseEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                return;
            }
            // Ignore the event if the layout information is not valid.
            if (This.TextView == null || !This.TextView.IsValid)
            {
                return;
            }

            // Check if the control has focus
            if (This.UiScope.IsKeyboardFocused)
            {
                OnMouseMoveWithFocus(This, e);
            }
            else
            {
                OnMouseMoveWithoutFocus(This, e);
            }
        }

        // MouseUpEvent handler.
        internal static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (e.RightButton != MouseButtonState.Released)
            {
                return;
            }

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                return;
            }

            if (This.TextView == null || !This.TextView.IsValid)
            {
                return;
            }

            if (!This.UiScope.IsMouseCaptured)
            {
                return;
            }

            // Consider event handled
            e.Handled = true;

            This.CancelExtendSelection();

            // Calculate coordinates of mouse poinnt
            Point mousePoint = e.GetPosition(This.TextView.RenderScope);

            // REVIEW:benwest: should this call be in the change block?
            TextEditorMouse.UpdateCursor(This, mousePoint);

            if (This._tableColResizeInfo != null)
            {
                // Apply resizing and dispose table resizing adorner
                using (This.Selection.DeclareChangeBlock())
                {
                    This._tableColResizeInfo.ResizeColumn(mousePoint);
                    This._tableColResizeInfo = null;
                }
            }
            else
            {
                using (This.Selection.DeclareChangeBlock())
                {
                    // Check for deferred selection (in case if mouse down was within selection)
                    This._dragDropProcess.DoMouseLeftButtonUp(e);

                    This._forceWordSelection = false;
                    This._forceParagraphSelection = false;
                }
            }

            // Release mouse capture. TextView can be not valid by calling ReleaseMouseCapture()
            // if someone chnage the content(or background) by listening mouse movement.
            This._mouseCapturingInProgress = true;
            try
            {
                This.UiScope.ReleaseMouseCapture();
            }
            finally
            {
                This._mouseCapturingInProgress = false;
            }
        }

        // QueryCursorEvent handler
        internal static void OnQueryCursor(object sender, QueryCursorEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            if (This.TextView == null)
            {
                return;
            }

            // Determine whether the cursor is over our render scope.  In particular, we want to distinguish between
            // being directly over our RenderScope (including content of that scope), and being over visual chrome
            // between our UiScope and our RenderScope (such as scroll bars)
            if (IsPointWithinInteractiveArea(This, Mouse.GetPosition(This.UiScope)))
            {
                // Mouse is moving over our render scope, so we apply one of
                // editing cursors - IBeam when outside of selection, Arrow when within selection,
                // Resize - when over table borders, etc.

                // Otherwise (when we are not over the render scope) we do not
                // respond to QueryCursor request, thus leaving it for other
                // elements to decide.
                e.Cursor = This._cursor;
                e.Handled = true;
            }
        }

        #endregion Internal Methods        

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // ................................................................
        //
        // Event Handlers: Selection Building
        //
        // ................................................................

        // MouseMoveEvent handler.
        private static void OnMouseMoveWithoutFocus(TextEditor This, MouseEventArgs e)
        {
            // Note that position can be null here, because we did not request to snap it to text
            TextEditorMouse.UpdateCursor(This, e.GetPosition(This.TextView.RenderScope));
        }

        // MouseMoveEvent handler.
        private static void OnMouseMoveWithFocus(TextEditor This, MouseEventArgs e)
        {
            // Ignore the event if it was caused by us capturing the mouse
            if (This._mouseCapturingInProgress)
            {
                return;
            }

            // Clear a flag indicating that Shift key was pressed without any following key
            // This flag is necessary for KeyUp(RightShift/LeftShift) processing.
            TextEditor._ThreadLocalStore.PureControlShift = false;

            // Get the mouse move point.
            Point mouseMovePoint = e.GetPosition(This.TextView.RenderScope);

            // Update mouse cursor shape
            TextEditorMouse.UpdateCursor(This, mouseMovePoint);

            // For bug 1547567, remove when resolved.
            Invariant.Assert(This.Selection != null);

            // We're only interested in moves when the left button is down.
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            // We didn't get the original mouse down event, perhaps a listener
            // handled it.
            if (!This.UiScope.IsMouseCaptured)
            {
                return;
            }

            // Scale back any background layout in progress.
            This.TextView.ThrottleBackgroundTasksForUserInput();

            if (This._tableColResizeInfo != null)
            {
                This._tableColResizeInfo.UpdateAdorner(mouseMovePoint);
            }
            else
            {
                // Consider event handled
                e.Handled = true;

                // For bug 1547567, remove when resolved.
                Invariant.Assert(This.Selection != null);

                // Find a text position for this mouse point
                ITextPointer snappedCursorPosition = This.TextView.GetTextPositionFromPoint(mouseMovePoint, /*snapToText:*/true);

                // For bug 1547567, remove when resolved.
                Invariant.Assert(This.Selection != null);

                if (snappedCursorPosition == null)
                {
                    This.RequestExtendSelection(mouseMovePoint);
                }
                else
                {
                    This.CancelExtendSelection();

                    // For bug 1547567, remove when resolved.
                    Invariant.Assert(This.Selection != null);

                    if (!This._dragDropProcess.SourceOnMouseMove(mouseMovePoint))
                    {
                        // Auto-scrolling behavior during selection guesture -
                        // works when the mouse is outside of scroller's viewport.
                        // In such case we artificially increase coordinates to
                        // get to farther text position - which would speed-up scrolling
                        // in particular direction.
                        FrameworkElement scroller = This._Scroller;
                        if (scroller != null && This.UiScope is TextBoxBase)
                        {
                            ITextPointer acceleratedCursorPosition = null; // cursorPosition corrected to accelerate scrolling

                            Point targetPoint = new Point(mouseMovePoint.X, mouseMovePoint.Y);
                            Point pointScroller = e.GetPosition((IInputElement)scroller);

                            double pageHeight = (double)((TextBoxBase)This.UiScope).ViewportHeight;
                            double slowAreaDelta = ScrollViewer._scrollLineDelta;

                            // Auto scrolling up/down page for the page height if the mouse Y 
                            // position is out of viewport.
                            if (pointScroller.Y < 0 - slowAreaDelta)
                            {
                                Rect targetRect = This.TextView.GetRectangleFromTextPosition(snappedCursorPosition);
                                targetPoint = new Point(targetPoint.X, targetRect.Bottom - pageHeight);
                                acceleratedCursorPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);
                            }
                            else if (pointScroller.Y > pageHeight + slowAreaDelta)
                            {
                                Rect targetRect = This.TextView.GetRectangleFromTextPosition(snappedCursorPosition);
                                targetPoint = new Point(targetPoint.X, targetRect.Top + pageHeight);
                                acceleratedCursorPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);
                            }

                            double pageWidth = (double)((TextBoxBase)This.UiScope).ViewportWidth;

                            // Auto scrolling to left/right scroll delta amount if the mouse X position 
                            // is out of viewport area.
                            if (pointScroller.X < 0)
                            {
                                targetPoint = new Point(targetPoint.X - slowAreaDelta, targetPoint.Y);
                                acceleratedCursorPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);
                            }
                            else if (pointScroller.X > pageWidth)
                            {
                                targetPoint = new Point(targetPoint.X + slowAreaDelta, targetPoint.Y);
                                acceleratedCursorPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);
                            }

                            // Use acceleratedcursorPosition instead of real one to make scrolling reasonable faster
                            if (acceleratedCursorPosition != null)
                            {
                                snappedCursorPosition = acceleratedCursorPosition;
                            }
                        }

                        using (This.Selection.DeclareChangeBlock())
                        {
                            // Check end-of-container condition
                            if (snappedCursorPosition.GetNextInsertionPosition(LogicalDirection.Forward) == null &&
                                snappedCursorPosition.ParentType != null) //  This check is a work around of bug that Parent can be null for some text boxes.
                            {
                                // We are at the end of text container. Check whether mouse is farther than a last character
                                Rect lastCharacterRect = snappedCursorPosition.GetCharacterRect(LogicalDirection.Backward);
                                if (mouseMovePoint.X > lastCharacterRect.X + lastCharacterRect.Width)
                                {
                                    snappedCursorPosition = This.TextContainer.End;
                                }
                            }

                            // Move the caret/selection to match the cursor position.
                            This.Selection.ExtendSelectionByMouse(snappedCursorPosition, This._forceWordSelection, This._forceParagraphSelection);
                        }
                    }
                }
            }
        }

#if DISABLED_FOR_BUG_1639819
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // We want to extend the selection if the mouse button is down, just as if
            // the user moved the mouse.  The easiest way to do this is to call the OnMouseMove handler.
            MouseEventArgs mouseArgs = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount);
            mouseArgs.RoutedEvent = Mouse.MouseMoveEvent;
            OnMouseMove(sender, mouseArgs);
        }
#endif

        // Moves focus into our uiScope.
        // Returns true if focus was successfully moved to this control's UiScope,
        // and if there is no side effects happened with the content during the move.
        private static bool MoveFocusToUiScope(TextEditor This)
        {
            long contentChangeCounter = This._ContentChangeCounter;

            // FrameworkElement will scroll our scope into view, which we don't want.  Since
            // there's no way to prevent this, the best we can do is scroll back before layout
            // updates.  To do this, we listen to the ScrollChanged event that will get generated
            // as a result of the Focus() call we're about to make.
            
            // This only works if a ScrollViewer is responsible for scrolling.  FrameworkElement
            // needs to provide a way to avoid scrolling to begin with. 
            // GetParent returns a DO which could be a 2D or 3D Visual.  Since we are searching for a
            // ScrollViewer we cast it immediately to a Visual to avoid handling 3D objects.
            Visual scrollViewer = VisualTreeHelper.GetParent(This.UiScope) as Visual;
            while (scrollViewer != null && !(scrollViewer is ScrollViewer))
            {
                scrollViewer = VisualTreeHelper.GetParent(scrollViewer) as Visual;
            }
            if (scrollViewer != null)
            {
                ((ScrollViewer)scrollViewer).AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChangedDuringGotFocus));
            }

            // Cache the selection.  We could be detached when Focus raises a public event.
            ITextSelection selection = This.Selection;
            
            try
            {
                selection.Changed += OnSelectionChangedDuringGotFocus;
                _selectionChanged = false;
                This.UiScope.Focus(); // Raises a public event.
            }
            finally
            {
                selection.Changed -= OnSelectionChangedDuringGotFocus;
            
                // remove our scroll change handler
                if (scrollViewer != null)
                {
                    ((ScrollViewer)scrollViewer).RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChangedDuringGotFocus));
                }
            }

            return This.UiScope == Keyboard.FocusedElement &&
                contentChangeCounter == This._ContentChangeCounter &&
                !_selectionChanged;
        }

        private static void OnSelectionChangedDuringGotFocus(object sender, EventArgs e)
        {
            _selectionChanged = true;
        }

        // ScrollChanged handler.
        // We use this handler as a mechanism for keeping textboxes from scrolling into view.
        // Usually controls are scrolled to view when focus reaches them.
        // We are making an exception for text containing controls,
        // as they have their own (inner) scrollers - so their content will scroll when necessary.
        // So we have to reverse the scroll when it happens as a result of setting focus
        // to this uiScope.
        // We attach this handler only temporarily - before calling Focus method in MouseLeftButtonDown event,
        // and detach it immediately after that; so that it does not affect regular scrolling.
        private static void OnScrollChangedDuringGotFocus(object sender, ScrollChangedEventArgs e)
        {
            // Reverse the scroll
            ScrollViewer scrollViewer = e.OriginalSource as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChangedDuringGotFocus));
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.HorizontalChange);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.VerticalChange);
            }
        }

        // Check the cursor position against the text selection and see if we need to
        // change which cursor is displayed.  If the cursor is over the selection, show
        // the normal cursor.  Otherwise, show the EditCursors.
        private static void UpdateCursor(TextEditor This, Point mouseMovePoint)
        {
            Invariant.Assert(This.TextView != null && This.TextView.IsValid);

            // Default cursor is editing IBeam
            Cursor cursor = Cursors.IBeam;

            // Check special conditions to setup special cursor shape
            if (TextEditor.IsTableEditingEnabled && TextRangeEditTables.TableBorderHitTest(This.TextView, mouseMovePoint))
            {
                // Mmouse is over a tablecell border. Cursor must indicate potential column resize
                cursor = Cursors.SizeWE;
            }
            else
            {
                // Check if this position belongs to selected area or is over an embedded UIElement
                if (This.Selection != null && !This.UiScope.IsMouseCaptured)
                {
                    if (This.Selection.IsEmpty)
                    {
                        UIElement uiElement = GetUIElementWhenMouseOver(This, mouseMovePoint);
                        if (uiElement != null && uiElement.IsEnabled)
                        {
                            // Mouse is over an embedded UIElement which is enabled (UiScope may or may not have focus)
                            cursor = Cursors.Arrow;
                        }
                    }
                    else if (This.UiScope.IsFocused && This.Selection.Contains(mouseMovePoint))
                    {
                        // The mouse is over a non-empty selection and we're not dragging
                        cursor = Cursors.Arrow;
                    }
                }
            }

            if (cursor != This._cursor)
            {
                This._cursor = cursor;
                Mouse.UpdateCursor();
            }
        }

        // Return a UIElement when mouseMovePoint is within the ui element's bounding Rect. Null otherwise.
        private static UIElement GetUIElementWhenMouseOver(TextEditor This, Point mouseMovePoint)
        {
            ITextPointer mouseMovePosition = This.TextView.GetTextPositionFromPoint(mouseMovePoint, /*snapToText:*/false);
            if (mouseMovePosition == null)
            {
                return null;
            }

            if (!(mouseMovePosition.GetPointerContext(mouseMovePosition.LogicalDirection) == TextPointerContext.EmbeddedElement))
            {
                return null;
            }

            // Find out if mouseMovePoint is within the bounding Rect of UIElement, we need to do this check explicitly
            // because even when snapToText is false, textview returns a first/last position on a line when point is in 
            // an area before/after line start/end. This is by-design behavior for textview.

            // Need to get Rect from TextView, since Rect returned by TextPointer.GetCharacterRect() 
            // is transformed to UiScope coordinates and we want RenderScope coordinates here.
            
            ITextPointer otherEdgePosition = mouseMovePosition.GetNextContextPosition(mouseMovePosition.LogicalDirection);
            LogicalDirection otherEdgeDirection = (mouseMovePosition.LogicalDirection == LogicalDirection.Forward) ?
                LogicalDirection.Backward : LogicalDirection.Forward;
            
            // Normalize with correct gravity
            otherEdgePosition = otherEdgePosition.CreatePointer(0, otherEdgeDirection);

            Rect uiElementFirstEdgeRect = This.TextView.GetRectangleFromTextPosition(mouseMovePosition);
            Rect uiElementSecondEdgeRect = This.TextView.GetRectangleFromTextPosition(otherEdgePosition);

            Rect boundingRect = uiElementFirstEdgeRect;
            boundingRect.Union(uiElementSecondEdgeRect);
            if (!boundingRect.Contains(mouseMovePoint))
            {
                return null;
            }

            return mouseMovePosition.GetAdjacentElement(mouseMovePosition.LogicalDirection) as UIElement;
        }

        // Determine whether the given point is within the RenderScope but not covered by chrome,
        // scroll bars, etc.
        //
        // Note that the passed point must be relative to the UiScope.
        private static bool IsPointWithinRenderScope(TextEditor textEditor, Point point)
        {
            DependencyObject textContainerOwner = textEditor.TextContainer.Parent;
            UIElement renderScope = textEditor.TextView.RenderScope;
            CaretElement caretElement = textEditor.Selection.CaretElement;

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(textEditor.UiScope, point);
            if (hitTestResult != null)
            {
                bool check = false;
                if(hitTestResult.VisualHit is Visual) check = ((Visual)hitTestResult.VisualHit).IsDescendantOf(renderScope);
                if(hitTestResult.VisualHit is Visual3D) check = ((Visual3D)hitTestResult.VisualHit).IsDescendantOf(renderScope);
                
                if (hitTestResult.VisualHit == renderScope||
                    check ||
                    hitTestResult.VisualHit == caretElement)
                {
                    return true;
                }
            }

            DependencyObject hitElement = textEditor.UiScope.InputHitTest(point) as DependencyObject;
            while (hitElement != null)
            {
                if (hitElement == textContainerOwner ||
                    hitElement == renderScope || 
                    hitElement == caretElement)
                {
                    return true;
                }

                if (hitElement is FrameworkElement && ((FrameworkElement)hitElement).TemplatedParent == textEditor.UiScope)
                {
                    // The element belongs to control's chrome
                    hitElement = null;
                }
                else if (hitElement is Visual)
                {
                    hitElement = VisualTreeHelper.GetParent(hitElement);
                }
                else if (hitElement is FrameworkContentElement)
                {
                    hitElement = ((FrameworkContentElement)hitElement).Parent;
                }
                else
                {
                    hitElement = null;
                }
            }

            return false;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Whether or not selection changed during a Focus call
        static private bool _selectionChanged;

        #endregion Private Fields
    }
}

