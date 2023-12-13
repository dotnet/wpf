// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: A Component of TextEditor class supposrtinng Drag-and-drop 
//              functionality
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
    using System.Windows.Interop;  // WindowInteropHelper
    using System.Windows.Controls; // ScrollChangedEventArgs
    using System.Windows.Controls.Primitives;  // CharacterCasing, TextBoxBase
    using System.Windows.Data; // BindingExpression
    using System.Windows.Media;
    using System.Windows.Markup;
    using System.Windows;
    using System.Security;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers
    using MS.Internal.PresentationFramework;  //Demand for drag and drop
    
    using SecurityHelper=MS.Internal.SecurityHelper;

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal static class TextEditorDragDrop
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers all text editing command handlers for a given control type
        internal static void _RegisterClassHandlers(Type controlType, bool readOnly, bool registerEventListeners)
        {
            if (!readOnly)
            {
                EventManager.RegisterClassHandler(controlType, DragDrop.DropEvent, new DragEventHandler(OnClearState),true);
                EventManager.RegisterClassHandler(controlType, DragDrop.DragLeaveEvent, new DragEventHandler(OnClearState), true);
            }
            if (registerEventListeners)
            {
                EventManager.RegisterClassHandler(controlType, DragDrop.QueryContinueDragEvent, new QueryContinueDragEventHandler(OnQueryContinueDrag));
                EventManager.RegisterClassHandler(controlType, DragDrop.GiveFeedbackEvent, new GiveFeedbackEventHandler(OnGiveFeedback));
                EventManager.RegisterClassHandler(controlType, DragDrop.DragEnterEvent, new DragEventHandler(OnDragEnter));
                EventManager.RegisterClassHandler(controlType, DragDrop.DragOverEvent, new DragEventHandler(OnDragOver));
                EventManager.RegisterClassHandler(controlType, DragDrop.DragLeaveEvent, new DragEventHandler(OnDragLeave));
                if (!readOnly)
                {
                    EventManager.RegisterClassHandler(controlType, DragDrop.DropEvent, new DragEventHandler(OnDrop));
                }
            }
        }

        #endregion Class Internal Methods        

        //------------------------------------------------------
        //
        //  Class Internal Types
        //
        //------------------------------------------------------

        #region Class Internal Types

        // A structure used for storing DragDrop status during dragging process
        internal class _DragDropProcess
        {
            internal _DragDropProcess(TextEditor textEditor)
            {
                Invariant.Assert(textEditor != null);
                _textEditor = textEditor;
            }
            
            /// <summary>
            /// Checks whether mouse down position belongs to selected portion of text,
            /// and initiates a drad-and-drop process in this case.
            /// Drag-drop initiation does not capture mouse yet, and do not start
            /// OleDragDrop; this will happen on a subsequent mouse move event
            /// (if it will happen before mouse up).
            /// </summary>
            /// <param name="mouseDownPoint">
            /// TextView-relative coordinates of mouse down event.
            /// </param>
            /// <returns>
            /// true if this mouse down was inside of selection and drag-drop process was activated.
            /// false if the mouse down was outside of selected portion.
            /// </returns>
            internal bool SourceOnMouseLeftButtonDown(Point mouseDownPoint)
            {
                ITextSelection selection = _textEditor.Selection;

                if (_textEditor.UiScope is PasswordBox)
                {
                    //  SHould we generalize Drag enableness instead of using hard-coded dependency on PasswordBox?
                    _dragStarted = false;
                }
                else
                {
                    // Get the drag minimum width/height from SystemMetrics.DragMinimumWidth/DragMinimumHeight.
                    // dragMinimumWidth and dragMinimumheight of a rectangle centered on a drag point to allow for limited movement 
                    // of the mouse pointer before a drag operation begins.  
                    // It allows the user to click and release the mouse button easily without unintentionally starting a drag operation.
                    int minimumHorizontalDragDistance = (int)SystemParameters.MinimumHorizontalDragDistance;
                    int minimumVerticalDragDistance = (int)SystemParameters.MinimumVerticalDragDistance;

                    _dragRect = new Rect(mouseDownPoint.X - minimumHorizontalDragDistance, mouseDownPoint.Y - minimumVerticalDragDistance, minimumHorizontalDragDistance * 2, minimumVerticalDragDistance * 2);

                    // Check if click happened within existing selection
                    _dragStarted = selection.Contains(mouseDownPoint);
                }

                return _dragStarted;
            }

            // MouseUpEvent handler.
            internal void DoMouseLeftButtonUp(MouseButtonEventArgs e)
            {
                if (_dragStarted)
                {   
                    // We get to this state when drag gesture ends within the selection,
                    // so we only need to set selection into mouse-releasing point.
                    if (this.TextView.IsValid)
                    {
                        Point mouseDownPoint = e.GetPosition(_textEditor.TextView.RenderScope);
                        ITextPointer cursorPosition = this.TextView.GetTextPositionFromPoint(mouseDownPoint, /*snapToText:*/true);
                        if (cursorPosition != null)
                        {
                            _textEditor.Selection.SetSelectionByMouse(cursorPosition, mouseDownPoint);
                        }
                    }
                    _dragStarted = false;
                }
            }

            // Starts OLE dragdrop process if movement was started from
            // within selection and initial move is big enough for drag to start.
            // Returns true if drag is in progress
            internal bool SourceOnMouseMove(Point mouseMovePoint)
            {
                if (!_dragStarted)
                {
                    return false; // false means that drag is not involved at all - selection extension should continue
                }

                // Check the mouse drag to start DragDrop operation.
                if (!InitialThresholdCrossed(mouseMovePoint))
                {
                    return true; // true means that drag is in progress, even though not yet started - so selection should not extend
                }

                ITextSelection selection = _textEditor.Selection;

                // NOTE: This calls OnMouseMove recursively;
                // but because UiScope.IsMouseCaptured is false already,
                // we'll return with no actions
                // This is the first move in drag-drop gesture.
                // Execure the whole drag-drop ssequence: returns after the drop
                _dragStarted = false;

                // Execute OLE drag-drop process (synchronousely)
                // ----------------------------------------------

                // Set the original text range to delete it with DragDropEffects.Move effect.
                _dragSourceTextRange = new TextRange(selection.Start, selection.End);

                // Prepare data object (including side effects from application customization)

                // Note: _CreateDataObject raises a public event which might throw a recoverable exception.
                IDataObject dataObject = TextEditorCopyPaste._CreateDataObject(_textEditor, /*isDragDrop:*/true);

                if (dataObject != null) // null would mean that application cancelled the command
                {
                    //  Check if we better normalize selection before doing this?
                    SourceDoDragDrop(selection, dataObject);

                    // Release mouse capture, because DoDragDrop is taking
                    // a mouse resposibility from now on.
                    // ReleaseMouseCapture shouldn't call before calling DoDragDroop
                    // that cause the generating WM_MOUSELEAVE message by system 
                    // (xxxCapture xxxCancelMouseMoverTracking) that appear MouseLeave
                    // event during DragDrop event.
                    _textEditor.UiScope.ReleaseMouseCapture();

                    return true; // true means that drag is in progress. Selection should not extend.
                }
                else
                {
                    // The DragDrop process has been terminated by application custom code
                    //  Check if returnig false sufficient for terminating DragDrop correctly?
                    return false;
                }
            }

            // Check whether the mouse is dragged with the minimum width and height.
            // _dragRect is Width and height of a rectangle centered on a drag point to allow for limited movement 
            // of the mouse pointer before a drag operation begins. 
            // It allows the user to click and release the mouse button easily without unintentionally starting a drag operation.
            private bool InitialThresholdCrossed(Point dragPoint)
            {
                // Check the current poisition is in the drag rect.
                return !_dragRect.Contains(dragPoint.X, dragPoint.Y);
            }

            /// <summary>
            /// DragEnd event handler from DragDrop behavior.
            /// </summary>
            private void SourceDoDragDrop(ITextSelection selection, IDataObject dataObject)
            {
                // Run OLE drag-drop process. It will eat all user input until the drop
                DragDropEffects allowedDragDropEffects = DragDropEffects.Copy;
                if (!_textEditor.IsReadOnly)
                {
                    allowedDragDropEffects |= DragDropEffects.Move;
                }

                DragDropEffects resultingDragDropEffects = DragDropEffects.None;

                try
                {
                    resultingDragDropEffects = DragDrop.DoDragDrop( //
                    _textEditor.UiScope, // dragSource, 
                    dataObject, //
                    allowedDragDropEffects);
                }
                // Ole32's DoDragDrop can return E_UNEXCEPTED, which comes to us as a COMException,
                // if something unexpected happened during the drag and drop operation,
                // e.g. the application receiving the drop failed. In this case we should
                // not fail, we should catch the exception and act as if the drop wasn't allowed.
                catch (COMException ex) when(ex.HResult == NativeMethods.E_UNEXPECTED)
                {
                }

                // Remove source selection 
                if (!_textEditor.IsReadOnly && //
                    resultingDragDropEffects == DragDropEffects.Move && //
                    _dragSourceTextRange != null &&
                    !_dragSourceTextRange.IsEmpty)
                {
                    // Normally we delete the source selection from OnDrop event,
                    // unless source and target TextBoxes are different.
                    // In this case the source selection is still not empty,
                    // which means that target was in a different TextBox.
                    // So we still need to delete the selected content in the source one.
                    // This will create an undo unit different from a dropping one,
                    // which is ok, because it will be in different TextBox's undo stack.
                    using (selection.DeclareChangeBlock())
                    {
                        // This is end of Move - we need to delete source content
                        _dragSourceTextRange.Text = String.Empty;
                    }
                }

                // Clean up the text range.
                _dragSourceTextRange = null;

                // Check the data binding expression and update the source and target if the drag source
                // has the binding expression. Without this, data binding is broken after complete the 
                // drag-drop operation because Drop() paste the object then set the focus to the target.
                // The losting focus invoke the data binding expression's Update(), but the source isn't
                // updated yet before complete DoDragDrop.
                if (!_textEditor.IsReadOnly)
                {
                    BindingExpressionBase bindingExpression = BindingOperations.GetBindingExpressionBase(
                        _textEditor.UiScope, TextBox.TextProperty);
                    if (bindingExpression != null)
                    {
                        bindingExpression.UpdateSource();
                        bindingExpression.UpdateTarget();
                    }
                }
            }

            // Creates DropCaret
            internal void TargetEnsureDropCaret()
            {
                if (_caretDragDrop == null)
                {
                    //  We never delete drop caret, never detach it from view. Not a big deal, but not clear...

                    // Add the caret.
                    // Create caret to show it during the dragging operation.
                    _caretDragDrop = new CaretElement(_textEditor, /*isBlinkEnabled:*/false);

                    // Initialize the caret.
                    // (psarrett) Understand why this call is so important for AdornerLayer.
                    _caretDragDrop.Hide();
                }
            }

            /// A handler for an event reporting that the drag enter during drag-and-drop operation.
            internal void TargetOnDragEnter(DragEventArgs e)
            {
                if (!AllowDragDrop(e))
                {
                    return;
                }

                // Ok, there's data to move or copy here.
                if ((e.AllowedEffects & DragDropEffects.Move) != 0)
                {
                    e.Effects = DragDropEffects.Move;
                }

                bool ctrlKeyDown = ((int)(e.KeyStates & DragDropKeyStates.ControlKey) != 0);
                if (ctrlKeyDown)
                {
                    e.Effects |= DragDropEffects.Copy;
                }

                // Create the drag-and-drop caret to show it on the drop target candidate place.
                TargetEnsureDropCaret();
            }

            /// A handler for an event reporting that the drag over during drag-and-drop operation.
            internal void TargetOnDragOver(DragEventArgs e)
            {
                if (!AllowDragDrop(e))
                {
                    return;
                }

                // Ok, there's data to move or copy here.
                if ((e.AllowedEffects & DragDropEffects.Move) != 0)
                {
                    e.Effects = DragDropEffects.Move;
                }

                bool ctrlKeyDown = ((int)(e.KeyStates & DragDropKeyStates.ControlKey) != 0);
                if (ctrlKeyDown)
                {
                    e.Effects |= DragDropEffects.Copy;
                }

                // Show the caret on the drag over target position.
                if (_caretDragDrop != null)
                {
                    // Update the layout to get the corrected text position. Otherwise, we can get the
                    // incorrected text position.
                    if (!_textEditor.TextView.Validate(e.GetPosition(_textEditor.TextView.RenderScope)))
                    {
                        return;
                    }

                    // Find the scroller from the render scope
                    FrameworkElement scroller = _textEditor._Scroller;

                    // Automatically scroll the dropable content(line or page up/down) if scroller is available
                    if (scroller != null)
                    {
                        // Get the ScrollInfo to scroll a line or page up/down
                        IScrollInfo scrollInfo = scroller as IScrollInfo;

                        if (scrollInfo == null && scroller is ScrollViewer)
                        {
                            scrollInfo = ((ScrollViewer)scroller).ScrollInfo;
                        }

                        Invariant.Assert(scrollInfo != null);

                        // Takes care of scrolling mechanism when vertical scrollbar is available, it creates a virtual
                        // block within the viewport where if you position your mouse during drag leads to scrolling,here
                        // it is of 16pixels and within first 8pixels it does scrolling by line and for next it scrolls by page.
                        
                        Point pointScroller = e.GetPosition((IInputElement)scroller);
                        double pageHeight = (double)_textEditor.UiScope.GetValue(TextEditor.PageHeightProperty);
                        double slowAreaHeight = ScrollViewer._scrollLineDelta;

                        if (pointScroller.Y < slowAreaHeight)
                        {
                            // Drag position is on the scroll area that we need to scroll up
                            if (pointScroller.Y > slowAreaHeight / 2)
                            {
                                // scroll a line up
                                scrollInfo.LineUp();
                            }
                            else
                            {
                                // scroll a page up
                                scrollInfo.PageUp();
                            }
                        }
                        else if (pointScroller.Y > (pageHeight - slowAreaHeight))
                        {
                            // Drag position is on the scroll area that we need to scroll down
                            if (pointScroller.Y < (pageHeight - slowAreaHeight / 2))
                            {
                                // scroll a line down
                                scrollInfo.LineDown();
                            }
                            else
                            {
                                // scroll a page down
                                scrollInfo.PageDown();
                            }
                        }
                    }

                    // Get the current text position from the dropable mouse point.
                    _textEditor.TextView.RenderScope.UpdateLayout(); // REVIEW:benwest:6/27/2006: This should use TextView.Validate, and check the return value instead of using IsValid below.

                    if (_textEditor.TextView.IsValid)
                    {
                        ITextPointer dragPosition = GetDropPosition(_textEditor.TextView.RenderScope as Visual, e.GetPosition(_textEditor.TextView.RenderScope));

                        if (dragPosition != null)
                        {
                            // Get the caret position to show the dropable point.
                            Rect caretRectangle = this.TextView.GetRectangleFromTextPosition(dragPosition);

                            // NOTE: We DO NOT use GetCurrentValue because springload formatting should NOT be involved for drop caret.
                            object fontStylePropertyValue = dragPosition.GetValue(TextElement.FontStyleProperty);
                            bool italic = (_textEditor.AcceptsRichContent && fontStylePropertyValue != DependencyProperty.UnsetValue && (FontStyle)fontStylePropertyValue == FontStyles.Italic);
                            Brush caretBrush = TextSelection.GetCaretBrush(_textEditor);

                            // Show the caret on the dropable position.
                            _caretDragDrop.Update(/*visible:*/true, caretRectangle, caretBrush, 0.5, italic, CaretScrollMethod.None, /*wordWrappingPosition*/ double.NaN);
                        }
                    }
                }
            }

            /// <summary>
            /// Calculates a TextPointer indended for dropping the text.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="point"></param>
            /// <returns>
            /// ITextPointer intended for dropping the selected text.
            /// Adjusts the dropping point to a word boundary (beginning of word)
            /// in case if source range contains whole words.
            /// The position returned is oriented towards a character
            /// under the mouse pointer.
            /// </returns>
            private ITextPointer GetDropPosition(Visual target, Point point)
            {
                Invariant.Assert(target != null);
                Invariant.Assert(_textEditor.TextView.IsValid); // caller must guarantee this.

                // Convert point to RenderScope
                if (target != _textEditor.TextView.RenderScope && target != null && (_textEditor.TextView.RenderScope).IsAncestorOf(target))
                {
                    GeneralTransform transform = target.TransformToAncestor(_textEditor.TextView.RenderScope);
                    transform.TryTransform(point, out point); 
                }

                ITextPointer dropPosition = this.TextView.GetTextPositionFromPoint(point, /*snapToText:*/true);
                
                // For rich text content we adjust drop position to word boundary
                if (dropPosition != null)
                {
                    // Normalize drop position
                    dropPosition = dropPosition.GetInsertionPosition(dropPosition.LogicalDirection);

                    if (_textEditor.AcceptsRichContent)
                    {
                        TextSegment lineRange = TextEditorSelection.GetNormalizedLineRange(this.TextView, dropPosition);

                        if (!lineRange.IsNull &&
                            // The drop position must be before of end of line
                            dropPosition.CompareTo(lineRange.End) < 0 &&
                            // We check if we are not at word boundary already:
                            !TextPointerBase.IsAtWordBoundary(dropPosition, /*insideWordDirection:*/LogicalDirection.Forward) &&
                            // We do not do it if the source range was not on word boundaries from both ends
                            _dragSourceTextRange != null && //
                            TextPointerBase.IsAtWordBoundary(_dragSourceTextRange.Start, LogicalDirection.Forward) && //
                            TextPointerBase.IsAtWordBoundary(_dragSourceTextRange.End, LogicalDirection.Forward))
                        {
                            // Move to word boundary. Select closest one to a dropPosition.
                            TextSegment wordSegment = TextPointerBase.GetWordRange(dropPosition);
                            string wordText = TextRangeBase.GetTextInternal(wordSegment.Start, wordSegment.End);
                            int indexInWord = wordSegment.Start.GetOffsetToPosition(dropPosition);
                            dropPosition = (indexInWord < (wordText.Length / 2)) ? wordSegment.Start : wordSegment.End;
                        }
                    }
                }

                return dropPosition;
            }

            /// <summary>
            /// Responsible for deleteing the caret.
            /// </summary>
            internal void DeleteCaret()
            {
                // Delete the caret
                if (_caretDragDrop != null)
                {
                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(TextView.RenderScope);
                    layer.Remove(_caretDragDrop);
                    _caretDragDrop = null;
                }
            }

            /// <summary>
            /// Called from an event reporting that the drop happened.
            /// </summary>
            internal void TargetOnDrop(DragEventArgs e)
            {
                //  We should not use e.Handled in nonstandard way.

                if (!AllowDragDrop(e))
                {
                    return;
                }

                ITextSelection selection = _textEditor.Selection;
                Invariant.Assert(selection != null);

                if (e.Data == null || e.AllowedEffects == DragDropEffects.None)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if ((int)(e.KeyStates & DragDropKeyStates.ControlKey) != 0)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else if (e.Effects != DragDropEffects.Copy)
                {
                    e.Effects = DragDropEffects.Move;
                }

                // Force a layout update on the content so the GetTextPositionFromPoint
                // call following can succeed.
                if (!_textEditor.TextView.Validate(e.GetPosition(_textEditor.TextView.RenderScope)))
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // Get the text position from the text target point.
                ITextPointer dropPosition = GetDropPosition(_textEditor.TextView.RenderScope as Visual, e.GetPosition(_textEditor.TextView.RenderScope));

                if (dropPosition != null)
                {
                    if (_dragSourceTextRange != null && _dragSourceTextRange.Start.TextContainer == selection.Start.TextContainer &&
                        !selection.IsEmpty && IsSelectionContainsDropPosition(selection, dropPosition))
                    {
                        // When we drop inside of selected area, we
                        // should not select dropped content,
                        // otherwise it looks for end user as if
                        // nothing happened.

                        // Set caret to this position.
                        selection.SetCaretToPosition(dropPosition, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/true);

                        // Indicate the resulting effect of an action
                        // Note that dropResult may stay equal to DragDropResult.Drop
                        e.Effects = DragDropEffects.None;

                        // Mark the event as handled
                        e.Handled = true;
                    }
                    else
                    {
                        using (selection.DeclareChangeBlock())
                        {
                            // For MaxLength filter work correctly in case
                            // when we dragdrop within the same TextContainer,
                            // we need to delete dragged content first -
                            // before dropping when filtering will occur.
                            // Note, that this will duplicate operation on
                            // source side, but it will be void deletion action
                            if ((e.Effects & DragDropEffects.Move) != 0 && //
                                _dragSourceTextRange != null && _dragSourceTextRange.Start.TextContainer == selection.Start.TextContainer)
                            {
                                _dragSourceTextRange.Text = String.Empty;
                            }

                            // When we drop outside of selection,
                            // we should ignore current selection and
                            // move ip into dropping point.
                            selection.SetCaretToPosition(dropPosition, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/true);

                            // _DoPaste raises a public event -- could raise recoverable exception.
                            e.Handled = TextEditorCopyPaste._DoPaste(_textEditor, e.Data, /*isDragDrop:*/true);
                            //  So we consider a case when we pasted nothing as non-handled. This is inconsistent with otherwise "static" approach to event handling across editing. Need to revisit this.
                        }
                    }

                    if (e.Handled)
                    {
                        // Set the drop target as the foreground window.
                        Win32SetForegroundWindow();

                        // Set the focus into the drop target.
                        _textEditor.UiScope.Focus();
                    }
                    else
                    {
                        // When a target did not handle a drop event, we must
                        // prevent from deleting a content on source end -
                        // otherwise we'll have data loss
                        e.Effects = DragDropEffects.None;
                    }
                }
            }

            // Table cell selection currently include the next adjacent cell start element so that
            // selection always contains the drop position even though the drop position is on the next cell.
            // This private method check the table range really contains the drop position or not.
            private bool IsSelectionContainsDropPosition(ITextSelection selection, ITextPointer dropPosition)
            {
                bool selectionContainedDropPosition = selection.Contains(dropPosition);

                if (selectionContainedDropPosition && selection.IsTableCellRange)
                {
                    for (int i = 0; i < selection.TextSegments.Count; i++)
                    {
                        TextSegment textSegment = selection._TextSegments[i];

                        if (dropPosition.CompareTo(textSegment.End) == 0)
                        {
                            selectionContainedDropPosition = false;
                            break;
                        }
                    }
                }

                return selectionContainedDropPosition;
            }

            private bool AllowDragDrop(DragEventArgs e)
            {
                if (!_textEditor.IsReadOnly && _textEditor.TextView != null && _textEditor.TextView.RenderScope != null)
                {
                    Window window = Window.GetWindow(_textEditor.TextView.RenderScope);
                    if (window == null)
                    {
                        return true;
                    }

                    WindowInteropHelper helper = new WindowInteropHelper(window);
                    if (SafeNativeMethods.IsWindowEnabled(new HandleRef(null, helper.Handle)))
                    {
                        return true;
                    }
                }

                e.Effects = DragDropEffects.None;
                return false;
            }

            /// <summary>
            /// Call Win32 SetForegroundWindow to set the drop target as the foreground window.
            /// </summary>
            private void Win32SetForegroundWindow()
            {
                PresentationSource source = null;
                IntPtr hwnd = IntPtr.Zero;
                source = PresentationSource.CriticalFromVisual(_textEditor.UiScope);
                if (source != null)
                {
                    hwnd = (source as IWin32Window).Handle;
                }

                if (hwnd != IntPtr.Zero)
                {
                    UnsafeNativeMethods.SetForegroundWindow(new HandleRef(null, hwnd));
                }
            }

            private ITextView TextView
            {
                get
                {
                    return _textEditor.TextView;
                }
            }

            private TextEditor _textEditor;

            // TextRange for drag source.
            private ITextRange _dragSourceTextRange;

            // Flag indicating that mouse dragging was started within selection.
            // It is used for deferring drag/drop until first move,
            // and for setting selection on mouseup in case of no move.
            private bool _dragStarted;

            // DragDrop caret to show it on the dropable target position.
            // 6/23/2004: we should not cache the caret.  Instead
            // it should be allocated an deallocated as needed.  We need only
            // one instance active at a time per Dispatcher -- this should be
            // like the way TextSelection handles its caret.
            private CaretElement _caretDragDrop;

            // Rectangle centered on a drag point to allow for limited movement of the mouse pointer before a drag operation begins.
            private Rect _dragRect;
        }

        /// <summary>
        /// An event reporting that the query continue drag during drag-and-drop operation.
        /// </summary>
        internal static void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
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

            // Consider event handled
            e.Handled = true;

            e.Action = DragAction.Continue;
            bool mouseUp = (((int)e.KeyStates & (int)DragDropKeyStates.LeftMouseButton) == 0);
            if (e.EscapePressed)
            {
                e.Action = DragAction.Cancel;
            }
            else if (mouseUp)
            {
                e.Action = DragAction.Drop;
            }
        }

        /// <summary>
        /// An event reporting that the give feedback during drag-and-drop operation.
        /// </summary>
        internal static void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
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

            // Show the default DragDrop cursor.
            e.UseDefaultCursors = true;

            // Consider event handled
            e.Handled = true;
        }

        /// <summary>
        /// An event reporting that the drag enter during drag-and-drop operation.
        /// </summary>
        internal static void OnDragEnter(object sender, DragEventArgs e)
        {
            // Consider event handled
            e.Handled = true;

            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled || This.TextView == null || This.TextView.RenderScope == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // If there's no supported data available, don't allow the drag-and-drop.
            if (e.Data == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ignore the event if there isn't the dropable(pasteable) data format
            if (TextEditorCopyPaste.GetPasteApplyFormat(This, e.Data) == string.Empty)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            if (!This.TextView.Validate(e.GetPosition(This.TextView.RenderScope)))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            This._dragDropProcess.TargetOnDragEnter(e);
        }

        /// <summary>
        /// An event reporting that the drag over during drag-and-drop operation.
        /// </summary>
        internal static void OnDragOver(object sender, DragEventArgs e)
        {
            // Consider event handled
            e.Handled = true;

            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled || This.TextView == null || This.TextView.RenderScope == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // If there's no supported data available, don't allow the drag-and-drop.
            if (e.Data == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            // Ignore the event if there isn't the dropable(pasteable) data format
            if (TextEditorCopyPaste.GetPasteApplyFormat(This, e.Data) == string.Empty)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);
            if (!This.TextView.Validate(e.GetPosition(This.TextView.RenderScope)))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            This._dragDropProcess.TargetOnDragOver(e);
        }

        /// <summary>
        /// An event reporting that the drag leave during drag-and-drop operation.
        /// </summary>
        internal static void OnDragLeave(object sender, DragEventArgs e)
        {
            // Consider event handled
            e.Handled = true;

            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            //
            // Remove UI feedback here if UI is specified on DragEnter.
            //
            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);
            if (!This.TextView.Validate(e.GetPosition(This.TextView.RenderScope)))
            {
                return;
            }
        }
       
        /// <summary>
        /// An event reporting that the drop happened.
        /// </summary>
        internal static void OnDrop(object sender, DragEventArgs e)
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

            TextEditorTyping._FlushPendingInputItems(This);
            if (!This.TextView.Validate(e.GetPosition(This.TextView.RenderScope)))
            {
                return;
            }

            This._dragDropProcess.TargetOnDrop(e);
        }

        /// <summary>
        /// An event for clearing the state of the Text Editor after events like drop or drag leave,
        /// Currently, It's clearing the caret which is drawn during dragOver and have never been deleted.
        /// </summary>
        internal static void OnClearState(object sender, DragEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

           This._dragDropProcess.DeleteCaret();
        }

        #endregion Class Internal Types
    }
}
