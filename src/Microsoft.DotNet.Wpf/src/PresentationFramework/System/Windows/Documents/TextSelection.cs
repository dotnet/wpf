// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Holds and manipulates the text selection state for TextEditor.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Controls.Primitives;  // TextBoxBase
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using System.Threading;
    using System.Security;
    using System.IO;
    using MS.Win32;
    using System.Windows.Controls;

    /// <summary>
    /// The TextSelection class encapsulates selection state for the RichTextBox
    /// control.  It has no public constructor, but is exposed via a public
    /// property on RichTextBox.
    /// </summary>
    public sealed class TextSelection : TextRange, ITextSelection
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // Contstructor.
        // TextSelection does not have a public constructor.  It is only accessible
        // through TextEditor's Selection property.
        internal TextSelection(TextEditor textEditor)
            : base(textEditor.TextContainer.Start, textEditor.TextContainer.Start)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            Invariant.Assert(textEditor.UiScope != null);

            // Attach the selection to its editor
            _textEditor = textEditor;

            // Initialize active pointers of the selection - anchor and moving pointers
            SetActivePositions(/*AnchorPosition:*/thisSelection.Start, thisSelection.End);

            // Activate selection in case if this control has keyboard focus already
            thisSelection.UpdateCaretAndHighlight();
        }

        #endregion Constructors

        // *****************************************************
        // *****************************************************
        // *****************************************************
        //
        // Abstract TextSelection Implementation
        //
        // *****************************************************
        // *****************************************************
        // *****************************************************

        //------------------------------------------------------
        //
        // ITextRange implementation
        //
        //------------------------------------------------------

        #region ITextRange Implementation

        //......................................................
        //
        // Selection Building
        //
        //......................................................

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.Select"/>
        /// </summary>
        void ITextRange.Select(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                TextRangeBase.Select(this, anchorPosition, movingPosition);
                SetActivePositions(anchorPosition, movingPosition);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.SelectWord"/>
        /// </summary>
        void ITextRange.SelectWord(ITextPointer position)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                TextRangeBase.SelectWord(this, position);

                ITextSelection thisSelection = this;
                SetActivePositions(/*anchorPosition:*/thisSelection.Start, thisSelection.End);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.SelectParagraph"/>
        /// </summary>
        void ITextRange.SelectParagraph(ITextPointer position)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                TextRangeBase.SelectParagraph(this, position);

                // Check whether this behave correctly on double-click-drag; double-Click-extendwithkeyboard
                ITextSelection thisSelection = this;
                SetActivePositions(/*anchorPosition:*/position, thisSelection.End);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.ITextRange.ApplyTypingHeuristics"/>
        /// </summary>
        void ITextRange.ApplyTypingHeuristics(bool overType)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                TextRangeBase.ApplyInitialTypingHeuristics(this);

                // For non-empty selection start with saving current formatting
                if (!this.IsEmpty && _textEditor.AcceptsRichContent)
                {
                    SpringloadCurrentFormatting();
                    // Note: we springload formatting before overtype expansion
                }

                TextRangeBase.ApplyFinalTypingHeuristics(this, overType);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.GetPropertyValue"/>
        /// </summary>
        object ITextRange.GetPropertyValue(DependencyProperty formattingProperty)
        {
            object propertyValue;

            if (this.IsEmpty && TextSchema.IsCharacterProperty(formattingProperty))
            {
                // For empty selection return springloaded formatting (only character formatting properties can be springloaded)
                propertyValue = ((TextSelection)this).GetCurrentValue(formattingProperty);
            }
            else
            {
                // Otherwise return base implementation from a TextRange.
                propertyValue = TextRangeBase.GetPropertyValue(this, formattingProperty);
            }

            return propertyValue;
        }

        //......................................................
        //
        // Plain Text Modification
        //
        //......................................................

        //------------------------------------------------------
        //
        //  Overrides
        //
        //------------------------------------------------------

        // Set true if a Changed event is pending.
        bool ITextRange._IsChanged
        {
            get
            {
                return _IsChanged;
            }

            set
            {
                // TextStore needs to know about state changes
                // from false to true.
                if (!_IsChanged && value)
                {
                    if (this.TextStore != null)
                    {
                        this.TextStore.OnSelectionChange();
                    }
                    if (this.ImmComposition != null)
                    {
                        this.ImmComposition.OnSelectionChange();
                    }
                }

                _IsChanged = value;
            }
        }

        /// <summary>
        /// </summary>
        void ITextRange.NotifyChanged(bool disableScroll, bool skipEvents)
        {
            // Notify text store about selection movement.
            if (this.TextStore != null)
            {
                this.TextStore.OnSelectionChanged();
            }
            // Notify ImmComposition about selection movement.
            if (this.ImmComposition != null)
            {
                this.ImmComposition.OnSelectionChanged();
            }

            if (!skipEvents)
            {
                TextRangeBase.NotifyChanged(this, disableScroll);
            }

            if (!disableScroll)
            {
                // Force a synchronous layout update.  If the update was big enough, background layout
                // kicked in and the caret won't otherwise be updated.  Note this will block the thread
                // while layout runs.
                //
                // It's possible an application repositioned the caret
                // while listening to a change event just raised, but in that case the following
                // code should be harmless.
                ITextPointer movingPosition = ((ITextSelection)this).MovingPosition;
                if (this.TextView != null && this.TextView.IsValid &&
                    !this.TextView.Contains(movingPosition))
                {
                    movingPosition.ValidateLayout();
                }
                // If layout wasn't valid, then there's a pending caret update
                // that will proceed correctly now.  Otherwise the whole operation
                // is a nop.
            }

            // Fixup the caret.
            UpdateCaretState(disableScroll ? CaretScrollMethod.None : CaretScrollMethod.Simple);
        }

        //------------------------------------------------------
        //
        //  ITextRange Properties
        //
        //------------------------------------------------------

        //......................................................
        //
        //  Content - rich and plain
        //
        //......................................................

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.Text"/>
        /// </summary>
        string ITextRange.Text
        {
            get
            {
                return TextRangeBase.GetText(this);
            }

            set
            {
                TextRangeBase.BeginChange(this);
                try
                {
                    TextRangeBase.SetText(this, value);

                    if (this.IsEmpty)
                    {
                        // We need to ensure appropriate caret visual position.
                        ((ITextSelection)this).SetCaretToPosition(((ITextRange)this).End, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                    }

                    Invariant.Assert(!this.IsTableCellRange);
                    SetActivePositions(((ITextRange)this).Start, ((ITextRange)this).End);
                }
                finally
                {
                    TextRangeBase.EndChange(this);
                }
            }
        }

        #endregion ITextRange Implementation

        //------------------------------------------------------
        //
        // ITextSelection implementation
        //
        //------------------------------------------------------

        #region ITextSelection Implementation

        void ITextSelection.UpdateCaretAndHighlight()
        {
            FrameworkElement uiScope = UiScope;
            FrameworkElement owner = CaretElement.GetOwnerElement(uiScope);
            bool showSelection = false;
            bool isBlinkEnabled = false;
            bool isSelectionActive = false;

            if (uiScope.IsEnabled && this.TextView != null)
            {
                if (uiScope.IsKeyboardFocused)
                {
                    showSelection = true;
                    isBlinkEnabled = true;
                    isSelectionActive = true;
                }
                else if (uiScope.IsFocused && 
                         ((IsRootElement(FocusManager.GetFocusScope(uiScope)) && IsFocusWithinRoot()) || // either UiScope root window has keyboard focus
                          _textEditor.IsContextMenuOpen))// or UiScope has a context menu open
                {
                    showSelection = true;
                    isBlinkEnabled = false;
                    isSelectionActive = true;
                }
                else if (!this.IsEmpty && (bool)owner.GetValue(TextBoxBase.IsInactiveSelectionHighlightEnabledProperty))
                {
                    showSelection = true;
                    isBlinkEnabled = false;
                    isSelectionActive = false;
                }
            }

            owner.SetValue(TextBoxBase.IsSelectionActivePropertyKey, isSelectionActive);

            if (showSelection)
            {
                if (isSelectionActive)
                {
                    // Update the TLS first, so that EnsureCaret is working in
                    // the correct context. Please note that TLS is only relevant 
                    // when selection is active and the caret is showing. If the 
                    // Adorner is only meant to render the selection highlight then 
                    // the TLS is irrelavant.
                    SetThreadSelection();
                }

                // Create Adorner that will render both the caret and selection highlight
                EnsureCaret(isBlinkEnabled, isSelectionActive, CaretScrollMethod.None);

                // Highlight selection
                Highlight();
            }
            else
            {
                // Update the TLS first, so that the caret is working in
                // the correct context.
                ClearThreadSelection();

                // delete the caret
                DetachCaretFromVisualTree();

                // Remove highlight
                Unhighlight();
            }
        }

        /// <summary>
        /// </summary>
        ITextPointer ITextSelection.AnchorPosition
        {
            get
            {
                Invariant.Assert(this.IsEmpty || _anchorPosition != null);
                Invariant.Assert(_anchorPosition == null || _anchorPosition.IsFrozen);
                return this.IsEmpty ? ((ITextSelection)this).Start : _anchorPosition;
            }
        }

        /// <summary>
        /// The position within this selection that responds to user input.
        /// </summary>
        ITextPointer ITextSelection.MovingPosition
        {
            get
            {
                ITextSelection thisSelection = this;
                ITextPointer movingPosition;

                if (this.IsEmpty)
                {
                    movingPosition = thisSelection.Start;
                }
                else
                {
                    switch (_movingPositionEdge)
                    {
                        case MovingEdge.Start:
                            movingPosition = thisSelection.Start;
                            break;

                        case MovingEdge.StartInner:
                            movingPosition = thisSelection.TextSegments[0].End;
                            break;

                        case MovingEdge.EndInner:
                            movingPosition = thisSelection.TextSegments[thisSelection.TextSegments.Count - 1].Start;
                            break;

                        case MovingEdge.End:
                            movingPosition = thisSelection.End;
                            break;

                        case MovingEdge.None:
                        default:
                            Invariant.Assert(false, "MovingEdge should never be None with non-empty TextSelection!");
                            movingPosition = null;
                            break;
                    }

                    movingPosition = movingPosition.GetFrozenPointer(_movingPositionDirection);
                }

                return movingPosition;
            }
        }

        /// <summary>
        /// </summary>
        void ITextSelection.SetCaretToPosition(ITextPointer caretPosition, LogicalDirection direction, bool allowStopAtLineEnd, bool allowStopNearSpace)
        {
            // We need a pointer with appropriate direction,
            // becasue it will be used in textRangeBase.Select method for
            // pointer normalization.
            caretPosition = caretPosition.CreatePointer(direction);

            // Normalize the position in its logical direction - to get to text content over there.
            caretPosition.MoveToInsertionPosition(direction);

            // We need a pointer with the reverse direction to confirm
            // the line wrapping position. So we can ensure Bidi caret navigation.
            // Bidi can have the different caret position by setting the
            // logical direction, so we have to only set the logical direction
            // as the forward for the real line wrapping position.
            ITextPointer reversePosition = caretPosition.CreatePointer(direction == LogicalDirection.Forward ? LogicalDirection.Backward : LogicalDirection.Forward);

            // Check line wrapping condition
            if (!allowStopAtLineEnd &&
                ((TextPointerBase.IsAtLineWrappingPosition(caretPosition, this.TextView) &&
                  TextPointerBase.IsAtLineWrappingPosition(reversePosition, this.TextView)) ||
                 TextPointerBase.IsNextToPlainLineBreak(caretPosition, LogicalDirection.Backward) ||
                 TextSchema.IsBreak(caretPosition.GetElementType(LogicalDirection.Backward))))
            {
                // Caret is at wrapping position, and we are not allowed to stay at end of line,
                // so we choose forward direction to appear in the begiinning of a second line
                caretPosition.SetLogicalDirection(LogicalDirection.Forward);
            }
            else
            {
                if (caretPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text &&
                    caretPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    // This is statistically most typical case. No "smartness" needed
                    // to choose standard Forward orientation for the caret.
                    // NOTE: By using caretPosition's direction we solve BiDi caret orientation case:
                    // The orietnation reflects a direction from where caret has been moved
                    // or orientation where mouse clicked. So we will stick with appropriate
                    // character.

                    // Nothing to do. The caretPosition is good to go.
                }
                else if (!allowStopNearSpace)
                {
                    // There are some tags around, and we are not allowed to choose a side near to space.
                    // So we need to perform some content analysis.

                    char[] charBuffer = new char[1];

                    if (caretPosition.GetPointerContext(direction) == TextPointerContext.Text &&
                        caretPosition.GetTextInRun(direction, charBuffer, 0, 1) == 1 &&
                        Char.IsWhiteSpace(charBuffer[0]))
                    {
                        LogicalDirection oppositeDirection = direction == LogicalDirection.Forward ? LogicalDirection.Backward : LogicalDirection.Forward;

                        // Check formatting switch condition at this position
                        FlowDirection initialFlowDirection = (FlowDirection)caretPosition.GetValue(FrameworkElement.FlowDirectionProperty);

                        bool moved = caretPosition.MoveToInsertionPosition(oppositeDirection);

                        if (moved &&
                            initialFlowDirection == (FlowDirection)caretPosition.GetValue(FrameworkElement.FlowDirectionProperty) &&
                            (caretPosition.GetPointerContext(oppositeDirection) != TextPointerContext.Text ||
                             caretPosition.GetTextInRun(oppositeDirection, charBuffer, 0, 1) != 1 ||
                             !Char.IsWhiteSpace(charBuffer[0])))
                        {
                            // In the opposite direction we have a non-space
                            // character. So we choose that direction
                            direction = oppositeDirection;
                            caretPosition.SetLogicalDirection(direction);
                        }
                    }
                }
            }

            // Now that orientation of a caretPosition is identified,
            // build an empty selection at this position
            TextRangeBase.BeginChange(this);
            try
            {
                TextRangeBase.Select(this, caretPosition, caretPosition);

                // Note how Select method works for the case of empty range:
                // It creates a single instance TextPointer normalized and oriented
                // in a direction taken from caretPosition:
                ITextSelection thisSelection = this;
                Invariant.Assert(thisSelection.Start.LogicalDirection == caretPosition.LogicalDirection); // orientation must be as passed
                Invariant.Assert(this.IsEmpty);
                //Invariant.Assert((object)thisSelection.Start == (object)thisSelection.End); // it must be the same instance of TextPointer
                //Invariant.Assert(TextPointerBase.IsAtInsertionPosition(thisSelection.Start, caretPosition.LogicalDirection)); // normalization must be done in the same diredction as orientation

                // Clear active positions when selection is empty
                SetActivePositions(null, null);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        //......................................................
        //
        //  Extending via movingEnd movements
        //
        //......................................................

        // Worker for ExtendToPosition, handles all ITextContainers.
        void ITextSelection.ExtendToPosition(ITextPointer position)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                ITextSelection thisSelection = (ITextSelection)this;

                // Store initial anchor position
                ITextPointer anchorPosition = thisSelection.AnchorPosition;

                //Build new selection
                TextRangeBase.Select(thisSelection, anchorPosition, position);

                // Store active positions.
                SetActivePositions(anchorPosition, position);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        // Worker for ExtendToNextInsertionPosition, handles all ITextContainers.
        bool ITextSelection.ExtendToNextInsertionPosition(LogicalDirection direction)
        {
            bool moved = false;

            TextRangeBase.BeginChange(this);
            try
            {
                ITextPointer anchorPosition = ((ITextSelection)this).AnchorPosition;
                ITextPointer movingPosition = ((ITextSelection)this).MovingPosition;
                ITextPointer newMovingPosition;

                if (this.IsTableCellRange)
                {
                    // Both moving and anchor positions are within a single Table, in seperate
                    // cells.  Select next cell.
                    newMovingPosition = TextRangeEditTables.GetNextTableCellRangeInsertionPosition(this, direction);
                }
                else if (movingPosition is TextPointer && TextPointerBase.IsAtRowEnd(movingPosition))
                {
                    // Moving position at at a Table row end, anchor position is outside
                    // the Table.  Select next row.
                    newMovingPosition = TextRangeEditTables.GetNextRowEndMovingPosition(this, direction);
                }
                else if (movingPosition is TextPointer && TextRangeEditTables.MovingPositionCrossesCellBoundary(this))
                {
                    // Moving position at at a Table row start, anchor position is outside
                    // the Table.  Select next row.
                    newMovingPosition = TextRangeEditTables.GetNextRowStartMovingPosition(this, direction);
                }
                else
                {
                    // No Table logic.
                    newMovingPosition = GetNextTextSegmentInsertionPosition(direction);
                }

                if (newMovingPosition == null && direction == LogicalDirection.Forward)
                {
                    // When moving forward we cannot find next insertion position, set the end of selection after the last paragraph
                    // (which is not an insertion position)
                    if (movingPosition.CompareTo(movingPosition.TextContainer.End) != 0)
                    {
                        newMovingPosition = movingPosition.TextContainer.End;
                    }
                }

                // Now that new movingPosition is prepared, build the new selection
                if (newMovingPosition != null)
                {
                    moved = true;

                    // Re-build a range for the new pair of positions
                    TextRangeBase.Select(this, anchorPosition, newMovingPosition);

                    // Make sure that active positions are inside of a selection
                    // Set the moving position direction to point toward the inner content.
                    LogicalDirection contentDirection = (anchorPosition.CompareTo(newMovingPosition) <= 0) ?
                                                 LogicalDirection.Backward : LogicalDirection.Forward;
                    newMovingPosition = newMovingPosition.GetFrozenPointer(contentDirection);

                    SetActivePositions(anchorPosition, newMovingPosition);
                }
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }

            return moved;
        }

        // Finds new movingPosition for the selection when it is in TextSegment state.
        // Returns null if there is no next insertion position in the requested direction.
        private ITextPointer GetNextTextSegmentInsertionPosition(LogicalDirection direction)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            // Move one over symbol in a given direction
            return thisSelection.MovingPosition.GetNextInsertionPosition(direction);
        }

        bool ITextSelection.Contains(Point point)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            if (thisSelection.IsEmpty)
            {
                return false;
            }

            if (this.TextView == null || !this.TextView.IsValid)
            {
                return false;
            }

            bool contains = false;

            ITextPointer position = this.TextView.GetTextPositionFromPoint(point, /*snapToText:*/false);

            // Did we hit any text?
            if (position != null && thisSelection.Contains(position))
            {
                // If we did, make sure the range covers at least one full character.
                // Check both character edges.

                position = position.GetNextInsertionPosition(position.LogicalDirection);

                if (position != null && thisSelection.Contains(position))
                {
                    contains = true;
                }
            }

            // Point snapped to text did not hit anything, but we still have a chance
            // to hit selection - in inter-paragraph or end-of-paragraph areas - highlighted by selection
            if (!contains)
            {
                if (_caretElement != null && _caretElement.SelectionGeometry != null &&
                    _caretElement.SelectionGeometry.FillContains(point))
                {
                    contains = true;
                }
            }

            return contains;
        }

        #region ITextSelection Implementation

        //......................................................
        //
        //  Interaction with Selection host
        //
        //......................................................


        // Called by TextEditor.OnDetach, when the behavior is shut down.
        void ITextSelection.OnDetach()
        {
            ITextSelection thisSelection = (ITextSelection)this;

            // Check if we need to deactivate the selection
            thisSelection.UpdateCaretAndHighlight();

            // Delete highlight layer created for this selection (if any)
            if (_highlightLayer != null && thisSelection.Start.TextContainer.Highlights.GetLayer(typeof(TextSelection)) == _highlightLayer)
            {
                thisSelection.Start.TextContainer.Highlights.RemoveLayer(_highlightLayer);
            }
            _highlightLayer = null;

            // Detach the selection from its editor
            _textEditor = null;
        }

        // ITextView.Updated event listener.
        // Called by the TextEditor.
        void ITextSelection.OnTextViewUpdated()
        {
            if ((this.UiScope.IsKeyboardFocused || this.UiScope.IsFocused))
            {
                // Use the locally defined caretElement because _caretElement can be null by
                // detaching CaretElement object
                // Stress bug#1583327 indicate that _caretElement can be set to null by
                // detaching. So the below code is caching the caret element instance locally.
                CaretElement caretElement = _caretElement;
                if (caretElement != null)
                {
                    caretElement.OnTextViewUpdated();
                }
            }

            if (_pendingUpdateCaretStateCallback)
            {
                // The TextView calls this method synchronously, before it finishes its Arrange
                // pass, so defer the remaining work until the TextView is valid.
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(UpdateCaretStateWorker), null);
            }
        }

        // Perform any cleanup necessary when removing the current UiScope
        // from the visual tree (eg, during a template change).
        void ITextSelection.DetachFromVisualTree()
        {
            DetachCaretFromVisualTree();
        }

        // Italic command event handler. Called by TextEditor.
        void ITextSelection.RefreshCaret()
        {
            // Update the caret to show it as italic or normal caret.
            RefreshCaret(_textEditor, _textEditor.Selection);
        }

        // This is called from TextStore when the InterimSelection style of the current selection is changed.
        void ITextSelection.OnInterimSelectionChanged(bool interimSelection)
        {
            // Update the caret to show or remove the interim block caret.
            UpdateCaretState(CaretScrollMethod.None);
        }

        //......................................................
        //
        //  Selection Heuristics
        //
        //......................................................


        // Moves the selection to the mouse cursor position.
        // Extends the active end if extend == true, otherwise the selection
        // is collapsed to a caret.
        void ITextSelection.SetSelectionByMouse(ITextPointer cursorPosition, Point cursorMousePoint)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                // Shift modifier pressed - extending selection from existing one
                thisSelection.ExtendSelectionByMouse(cursorPosition, /*forceWordSelection:*/false, /*forceParagraphSelection*/false);
            }
            else
            {
                // No shift modifier pressed - move selection to a cursor position
                MoveSelectionByMouse(cursorPosition, cursorMousePoint);
            }
        }

        // Extends the selection to the mouse cursor position.
        void ITextSelection.ExtendSelectionByMouse(ITextPointer cursorPosition, bool forceWordSelection, bool forceParagraphSelection)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            // Check whether the cursor has been actually moved - compare with the previous position
            if (forceParagraphSelection || _previousCursorPosition != null && cursorPosition.CompareTo(_previousCursorPosition) == 0)
            {
                // Mouse was not actually moved. Ignore the event.
                return;
            }

            thisSelection.BeginChange();
            try
            {
                if (!BeginMouseSelectionProcess(cursorPosition))
                {
                    return;
                }

                // Get anchor position
                ITextPointer anchorPosition = ((ITextSelection)this).AnchorPosition;

                // Identify words on selection ends
                TextSegment anchorWordRange;
                TextSegment cursorWordRange;
                IdentifyWordsOnSelectionEnds(anchorPosition, cursorPosition, forceWordSelection, out anchorWordRange, out cursorWordRange);

                // Calculate selection boundary positions
                ITextPointer startPosition;
                ITextPointer movingPosition;
                if (anchorWordRange.Start.CompareTo(cursorWordRange.Start) <= 0)
                {
                    startPosition = anchorWordRange.Start.GetFrozenPointer(LogicalDirection.Forward);
                    movingPosition = cursorWordRange.End.GetFrozenPointer(LogicalDirection.Backward); ;
                }
                else
                {
                    startPosition = anchorWordRange.End.GetFrozenPointer(LogicalDirection.Backward);
                    movingPosition = cursorWordRange.Start.GetFrozenPointer(LogicalDirection.Forward); ;
                }

                // Note that we use includeCellAtMovingPosition=true because we want that hit-tested table cell
                // be included into selection no matter whether it's empty or not.
                TextRangeBase.Select(this, startPosition, movingPosition, /*includeCellAtMovingPosition:*/true);
                SetActivePositions(anchorPosition, movingPosition);

                // Store previous cursor position - for the next extension event
                _previousCursorPosition = cursorPosition.CreatePointer();

                Invariant.Assert(thisSelection.Contains(thisSelection.AnchorPosition));
            }
            finally
            {
                thisSelection.EndChange();
            }
        }

        // Part of ExtendSelectionByMouse method:
        // Checks whether selection has been started and initiates selection process
        // Usually always returns true,
        // returns false only as a special value indicating that we need to return without executing selection expansion code.
        private bool BeginMouseSelectionProcess(ITextPointer cursorPosition)
        {
            if (_previousCursorPosition == null)
            {
                // This is a beginning of mouse selection guesture.
                // Initialize the guesture state
                // initially autoword expansion of both ends is enabled
                _anchorWordRangeHasBeenCrossedOnce = false;
                _allowWordExpansionOnAnchorEnd = true;
                _reenterPosition = null;

                if (this.GetUIElementSelected() != null)
                {
                    // This means that we have just received mousedown event and selected embedded element in this event.
                    // MoveMove event is sent immediately, but we don't want to loose UIElement selection,
                    // so we do not extend to the cursorPosition now.
                    _previousCursorPosition = cursorPosition.CreatePointer();
                    return false;
                }
            }

            return true;
        }

        // Part of ExtendSelectionByMouse method:
        // Identifies words on selection ends.
        private void IdentifyWordsOnSelectionEnds(ITextPointer anchorPosition, ITextPointer cursorPosition, bool forceWordSelection, out TextSegment anchorWordRange, out TextSegment cursorWordRange)
        {
            if (forceWordSelection)
            {
                anchorWordRange = TextPointerBase.GetWordRange(anchorPosition);
                cursorWordRange = TextPointerBase.GetWordRange(cursorPosition, cursorPosition.LogicalDirection);
            }
            else
            {
                // Define whether word adjustment is allowed. Pressing Shift+Control prevents from auto-word expansion.
                bool disableWordExpansion = _textEditor.AutoWordSelection == false || ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 && (Keyboard.Modifiers & ModifierKeys.Control) != 0);

                if (disableWordExpansion)
                {
                    anchorWordRange = new TextSegment(anchorPosition, anchorPosition);
                    cursorWordRange = new TextSegment(cursorPosition, cursorPosition);
                }
                else
                {
                    // Autoword expansion heuristics
                    // -----------------------------

                    // Word autoword heuristics:
                    // a) After active end returned to selected area, autoword expansion on active end is disabled
                    // b) After active end returned to the very first word, expansion on anchor word is disabled either
                    //    We do this though only if selection has crossed initial word boundary at least once.
                    // c) After active end crosses new word, autoword expansion of active end is enabled again

                    // Calculate a word range for anchor position
                    anchorWordRange = TextPointerBase.GetWordRange(anchorPosition);

                    // Check if we re-entering selection or moving outside
                    // and set autoexpansion flags accordingly
                    if (_previousCursorPosition != null &&
                        (anchorPosition.CompareTo(cursorPosition) < 0 && cursorPosition.CompareTo(_previousCursorPosition) < 0 ||
                        _previousCursorPosition.CompareTo(cursorPosition) < 0 && cursorPosition.CompareTo(anchorPosition) < 0))
                    {
                        // Re-entering selection.

                        // Store position of reentering
                        _reenterPosition = cursorPosition.CreatePointer();

                        // When re-entering reaches initial word, disable word expansion on anchor end either
                        if (_anchorWordRangeHasBeenCrossedOnce && anchorWordRange.Contains(cursorPosition))
                        {
                            _allowWordExpansionOnAnchorEnd = false;
                        }
                    }
                    else
                    {
                        // Extending the selection.

                        // Check if we are crossing a boundary of last reentered word to re-enable word expansion on moving end
                        if (_reenterPosition != null)
                        {
                            TextSegment lastReenteredWordRange = TextPointerBase.GetWordRange(_reenterPosition);
                            if (!lastReenteredWordRange.Contains(cursorPosition))
                            {
                                _reenterPosition = null;
                            }
                        }
                    }

                    // Identify expanded range on both ends
                    // We need smarter version of TextSegment.Contains - which would normalize position towards a segmenet internal before comparison
                    // I did not take a risk to do it before making more thorogh analysis. The three check below is a fix for hotbug #1193240.
                    if (anchorWordRange.Contains(cursorPosition) ||
                        anchorWordRange.Contains(cursorPosition.GetInsertionPosition(LogicalDirection.Forward)) ||
                        anchorWordRange.Contains(cursorPosition.GetInsertionPosition(LogicalDirection.Backward)))
                    {
                        // Selection does not cross word boundary, so shrink selection to exact anchor/cursor positions
                        anchorWordRange = new TextSegment(anchorPosition, anchorPosition);
                        cursorWordRange = new TextSegment(cursorPosition, cursorPosition);
                    }
                    else
                    {
                        // Selection crosses word boundary.
                        _anchorWordRangeHasBeenCrossedOnce = true;

                        if (!_allowWordExpansionOnAnchorEnd || //
                            TextPointerBase.IsAtWordBoundary(anchorPosition, /*insideWordDirection:*/LogicalDirection.Forward))
                        {
                            // We collapse anchorPosition in two cases:
                            // If we have been re-entering the initial word before -
                            // then we treat it as an indicator that user wants exact position on anchor end
                            // or
                            // if selection starts exactly on word boundary -
                            // then we should not include the following word (when selection extends backward).
                            //
                            // So in the both cases we collapse anchorWordRange to exact _anchorPosition
                            anchorWordRange = new TextSegment(anchorPosition, anchorPosition);
                        }

                        if (TextPointerBase.IsAfterLastParagraph(cursorPosition) ||
                            TextPointerBase.IsAtWordBoundary(cursorPosition, /*insideWordDirection:*/LogicalDirection.Forward))
                        {
                            cursorWordRange = new TextSegment(cursorPosition, cursorPosition);
                        }
                        else
                        {
                            if (_reenterPosition == null)
                            {
                                // We are not in re-entering mode; expand moving end to word boundary
                                cursorWordRange = TextPointerBase.GetWordRange(cursorPosition, cursorPosition.LogicalDirection);
                            }
                            else
                            {
                                // We are in re-entering mode; use exact moving end position
                                cursorWordRange = new TextSegment(cursorPosition, cursorPosition);
                            }
                        }
                    }
                }
            }
        }

        //......................................................
        //
        //  Table Selection
        //
        //......................................................

        /// <summary>
        /// Extends table selection by one row in a given direction
        /// </summary>
        /// <param name="direction">
        /// LogicalDirection.Forward means moving active cell one row down,
        /// LogicalDirection.Backward - one row up.
        /// </param>
        bool ITextSelection.ExtendToNextTableRow(LogicalDirection direction)
        {
            TableCell anchorCell;
            TableCell movingCell;
            TableRowGroup rowGroup;
            int nextRowIndex;
            TableCell nextCell;

            if (!this.IsTableCellRange)
            {
                return false;
            }

            Invariant.Assert(!this.IsEmpty);
            Invariant.Assert(_anchorPosition != null);
            Invariant.Assert(_movingPositionEdge != MovingEdge.None);

            if (!TextRangeEditTables.IsTableCellRange((TextPointer)_anchorPosition, (TextPointer)((ITextSelection)this).MovingPosition, /*includeCellAtMovingPosition:*/false, out anchorCell, out movingCell))
            {
                return false;
            }

            Invariant.Assert(anchorCell != null && movingCell != null);

            rowGroup = movingCell.Row.RowGroup;

            nextCell = null;

            if (direction == LogicalDirection.Forward)
            {
                // Move movingPosition to the following row.
                // Find a row in the forward direction
                nextRowIndex = movingCell.Row.Index + movingCell.RowSpan;
                if (nextRowIndex < rowGroup.Rows.Count)
                {
                    nextCell = FindCellAtColumnIndex(rowGroup.Rows[nextRowIndex].Cells, movingCell.ColumnIndex);
                }
            }
            else
            {
                // Find preceding row containing a cell in position of movingCell's first column
                nextRowIndex = movingCell.Row.Index - 1;
                while (nextRowIndex >= 0)
                {
                    nextCell = FindCellAtColumnIndex(rowGroup.Rows[nextRowIndex].Cells, movingCell.ColumnIndex);
                    if (nextCell != null)
                    {
                        break;
                    }
                    nextRowIndex--;
                }
            }

            if (nextCell != null)
            {
                // This check for cell start position is not safe. It gives wrong result on elements nested in Cells
                ITextPointer movingPosition = nextCell.ContentEnd.CreatePointer();
                movingPosition.MoveToNextInsertionPosition(LogicalDirection.Forward);

                TextRangeBase.Select(this, _anchorPosition, movingPosition);

                // Make sure that active positions are inside of a selection
                SetActivePositions(_anchorPosition, movingPosition);

                return true;
            }

            return false;
        }

        //------------------------------------------------------
        //
        //  ITextSelection Properties
        //
        //------------------------------------------------------

        // True if the current seleciton is for interim character.
        // Korean Interim character is now invisilbe selection (no highlight) and the controls needs to
        // have the block caret to indicate the interim character.
        // This should be updated by TextStore.
        internal bool IsInterimSelection
        {
            get
            {
                if (this.TextStore != null)
                {
                    return TextStore.IsInterimSelection;
                }

                return false;
            }
        }

        // IsInterimSelection wrapper for ITextSelection.
        bool ITextSelection.IsInterimSelection
        {
            get
            {
                return this.IsInterimSelection;
            }
        }

        #endregion ITextSelection Implementation


        // *****************************************************
        // *****************************************************
        // *****************************************************
        //
        // Concrete TextSelection Implementation
        //
        // *****************************************************
        // *****************************************************
        // *****************************************************


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //......................................................
        //
        //  Selection extension (moving active end)
        //
        //......................................................

        /// <summary>
        /// Potential public method - concrete equivalent of abstract method
        /// </summary>
        internal TextPointer AnchorPosition
        {
            get
            {
                // ATTENTION: This method is supposed to be a pure redirection
                // to corresponding ITextSelection method - to keep abstract and concrete selection behavior consistent
                return (TextPointer)((ITextSelection)this).AnchorPosition;
            }
        }

        /// <summary>
        /// Potential public method - concrete equivalent of abstract method
        /// </summary>
        internal TextPointer MovingPosition
        {
            get
            {
                // ATTENTION: This method is supposed to be a pure redirection
                // to corresponding ITextSelection method - to keep abstract and concrete selection behavior consistent
                return (TextPointer)((ITextSelection)this).MovingPosition;
            }
        }

        /// <summary>
        /// Potential public method - concrete equivalent of abstract method
        /// </summary>
        internal void SetCaretToPosition(TextPointer caretPosition, LogicalDirection direction, bool allowStopAtLineEnd, bool allowStopNearSpace)
        {
            // ATTENTION: This method is supposed to be a pure redirection
            // to corresponding ITextSelection method - to keep abstract and concrete selection behavior consistent
            ((ITextSelection)this).SetCaretToPosition(caretPosition, direction, allowStopAtLineEnd, allowStopNearSpace);
        }

        // Make this method public
        internal bool ExtendToNextInsertionPosition(LogicalDirection direction)
        {
            // ATTENTION: This method is supposed to be a pure redirection
            // to corresponding ITextSelection method - to keep abstract and concrete selection behavior consistent
            return ((ITextSelection)this).ExtendToNextInsertionPosition(direction);
        }

        #endregion Public Methods

        #region Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // InputLanguageChanged event handler. Called by TextEditor.
        internal static void OnInputLanguageChanged(CultureInfo cultureInfo)
        {
            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            // Check the changed input language of the cultureInfo to draw the BiDi caret in case of BiDi language
            // like as Arabic or Hebrew input language.
            if (IsBidiInputLanguage(cultureInfo))
            {
                threadLocalStore.Bidi = true;
            }
            else
            {
                threadLocalStore.Bidi = false;
            }

            // Update caret on focused text editor
            if (threadLocalStore.FocusedTextSelection != null)
            {
                ((ITextSelection)threadLocalStore.FocusedTextSelection).RefreshCaret();
            }
        }

        // Returns true if the text matching a pixel position falls within
        // the selection.
        internal bool Contains(Point point)
        {
            return ((ITextSelection)this).Contains(point);
        }

        //------------------------------------------------------
        //
        // Internal Virtual Methods - TextSelection Extensibility
        //
        //------------------------------------------------------

        //......................................................
        //
        //  Formatting
        //
        //......................................................

        /// <summary>
        ///  Append an object at the end of the TextRange.
        /// </summary>
        internal override void InsertEmbeddedUIElementVirtual(FrameworkElement embeddedElement)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                base.InsertEmbeddedUIElementVirtual(embeddedElement);

                this.ClearSpringloadFormatting();
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        /// <summary>
        /// Applies a property value to a selection.
        /// In case of empty selection sets property to springloaded property set.
        /// </summary>
        internal override void ApplyPropertyToTextVirtual(DependencyProperty formattingProperty, object value, bool applyToParagraphs, PropertyValueAction propertyValueAction)
        {
            if (!TextSchema.IsParagraphProperty(formattingProperty) && !TextSchema.IsCharacterProperty(formattingProperty))
            {
                return; // Ignore any unknown property
            }

            // Check whether we are in a situation when auto-word formatting must happen
            if (this.IsEmpty && TextSchema.IsCharacterProperty(formattingProperty) &&
                !applyToParagraphs &&
                formattingProperty != FrameworkElement.FlowDirectionProperty) // We dont want to apply flowdirection property to inlines when selection is empty.
            {
                TextSegment autoWordRange = TextRangeBase.GetAutoWord(this);
                if (autoWordRange.IsNull)
                {
                    // This property goes to springload formatting. We should not create undo unit for it.
                    if (_springloadFormatting == null)
                    {
                        _springloadFormatting = new DependencyObject();
                    }

                    _springloadFormatting.SetValue(formattingProperty, value);
                }
                else
                {
                    // TextRange will create undo unit with proper name
                    new TextRange(autoWordRange.Start, autoWordRange.End).ApplyPropertyValue(formattingProperty, value);
                }
            }
            else
            {
                // No word to auto-format. Apply property to a selection.
                // TextRange will create undo unit with proper name
                base.ApplyPropertyToTextVirtual(formattingProperty, value, applyToParagraphs, propertyValueAction);
                this.ClearSpringloadFormatting();
            }
        }

        internal override void ClearAllPropertiesVirtual()
        {
            // Character property - applies to text runs in selected range, or springloaded if selection is empty
            if (this.IsEmpty)
            {
                this.ClearSpringloadFormatting();
            }
            else
            {
                TextRangeBase.BeginChange(this);
                try
                {
                    base.ClearAllPropertiesVirtual();

                    this.ClearSpringloadFormatting();
                }
                finally
                {
                    TextRangeBase.EndChange(this);
                }
            }
        }

        //......................................................
        //
        //  Range Serialization
        //
        //......................................................

        // Worker for Xml property setter; enables extensibility for TextSelection
        internal override void SetXmlVirtual(TextElement fragment)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                base.SetXmlVirtual(fragment);

                this.ClearSpringloadFormatting();
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        // Worker for Load public method; enables extensibility for TextSelection
        internal override void LoadVirtual(Stream stream, string dataFormat)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                base.LoadVirtual(stream, dataFormat);

                this.ClearSpringloadFormatting();
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        //......................................................
        //
        //  Table Editing
        //
        //......................................................

        // Worker for InsertTable; enables extensibility for TextSelection
        internal override Table InsertTableVirtual(int rowCount, int columnCount)
        {
            using (DeclareChangeBlock())
            {
                Table table = base.InsertTableVirtual(rowCount, columnCount);

                if (table != null)
                {
                    TextPointer cellStart = table.RowGroups[0].Rows[0].Cells[0].ContentStart;

                    this.SetCaretToPosition(cellStart, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }

                return table;
            }
        }

        // ..............................................................
        //
        // Springload Formatting
        //
        // ..............................................................

        #region Springload Formatting

        /// <summary>
        /// Function used in OnApplyProperty commands for character formatting properties.
        /// It takes care of springload formatting (applied to empty selection).
        /// </summary>
        /// <param name="formattingProperty">
        /// Property whose value is subject to be toggled.
        /// </param>
        /// <returns>
        /// The value of a property
        /// </returns>
        internal object GetCurrentValue(DependencyProperty formattingProperty)
        {
            ITextSelection thisSelection = this;
            object propertyValue = DependencyProperty.UnsetValue;

            if (thisSelection.Start is TextPointer)
            {
                if (_springloadFormatting != null && this.IsEmpty)
                {
                    // Get springload value
                    propertyValue = _springloadFormatting.ReadLocalValue(formattingProperty);
                    if (propertyValue == DependencyProperty.UnsetValue)
                    {
                        propertyValue = this.Start.Parent.GetValue(formattingProperty);
                    }
                }
            }

            // If there's no spring loaded value, read the local value.
            if (propertyValue == DependencyProperty.UnsetValue)
            {
                propertyValue = this.PropertyPosition.GetValue(formattingProperty);
            }

            return propertyValue;
        }

        /// <summary>
        /// Reads a set of current formatting properties (from selection start)
        /// into _springloadFormatting - to apply potentially for the following input.
        /// </summary>
        internal void SpringloadCurrentFormatting()
        {
            if (((ITextSelection)this).Start is TextPointer)
            {
                TextPointer start = this.Start;

                Inline ancestor = start.GetNonMergeableInlineAncestor();
                if (ancestor != null)
                {
                    // Unless the selection is wholly contained within a Hyperlink, we don't
                    // want to springload its character properties.
                    if (this.End.GetNonMergeableInlineAncestor() != ancestor)
                    {
                        start = ancestor.ElementEnd;
                    }
                }

                if (_springloadFormatting == null)
                {
                    SpringloadCurrentFormatting(start.Parent);
                }
            }
        }

        private void SpringloadCurrentFormatting(DependencyObject parent)
        {
            // Create new bag for formatting properties
            _springloadFormatting = new DependencyObject();

            // Check if we have an object to read from
            if (parent == null)
            {
                return;
            }

            DependencyProperty[] inheritableProperties = TextSchema.GetInheritableProperties(typeof(Inline));
            DependencyProperty[] noninheritableProperties = TextSchema.GetNoninheritableProperties(typeof(Span));

            // Walk up the tree.  At each step, if the element is typographical only,
            // grab all non-inherited values.  When we reach the top of the tree, grab all
            // values.
            DependencyObject element = parent;
            while (element is Inline)
            {
                TextElementEditingBehaviorAttribute att = (TextElementEditingBehaviorAttribute)Attribute.GetCustomAttribute(element.GetType(), typeof(TextElementEditingBehaviorAttribute));
                if (att.IsTypographicOnly)
                {
                    for (int i = 0; i < inheritableProperties.Length; i++)
                    {
                        if (_springloadFormatting.ReadLocalValue(inheritableProperties[i]) == DependencyProperty.UnsetValue &&
                            inheritableProperties[i] != FrameworkElement.LanguageProperty &&
                            inheritableProperties[i] != FrameworkElement.FlowDirectionProperty &&
                            System.Windows.DependencyPropertyHelper.GetValueSource(element, inheritableProperties[i]).BaseValueSource != BaseValueSource.Inherited)
                        {
                            object value = parent.GetValue(inheritableProperties[i]);
                            _springloadFormatting.SetValue(inheritableProperties[i], value);
                        }
                    }
                    for (int i = 0; i < noninheritableProperties.Length; i++)
                    {
                        if (_springloadFormatting.ReadLocalValue(noninheritableProperties[i]) == DependencyProperty.UnsetValue &&
                            noninheritableProperties[i] != TextElement.TextEffectsProperty &&
                            System.Windows.DependencyPropertyHelper.GetValueSource(element, noninheritableProperties[i]).BaseValueSource != BaseValueSource.Inherited)
                        {
                            object value = parent.GetValue(noninheritableProperties[i]);
                            _springloadFormatting.SetValue(noninheritableProperties[i], value);
                        }
                    }
                }
                element = ((TextElement)element).Parent;
            }
        }

        /// <summary>
        /// Clears springload formatting, so that the following text input
        /// will use formatting from a current position.
        /// </summary>
        internal void ClearSpringloadFormatting()
        {
            if (((ITextSelection)this).Start is TextPointer)
            {
                // Delete all springloaded values
                _springloadFormatting = null;

                // Update caret italic state
                ((ITextSelection)this).RefreshCaret();
            }
        }

        /// <summary>
        /// Applies springload formatting to a given content range.
        /// Clears springloadFormatting after applying it.
        /// </summary>
        internal void ApplySpringloadFormatting()
        {
            if (!(((ITextSelection)this).Start is TextPointer))
            {
                return;
            }

            if (this.IsEmpty)
            {
                // We can't apply formatting to non-TextContainers or empty selection.
                return;
            }

            if (_springloadFormatting != null)
            {
                Invariant.Assert(this.Start.LogicalDirection == LogicalDirection.Backward);
                Invariant.Assert(this.End.LogicalDirection == LogicalDirection.Forward);

                LocalValueEnumerator springloadFormattingValues = _springloadFormatting.GetLocalValueEnumerator();

                while (!this.IsEmpty && springloadFormattingValues.MoveNext())
                {
                    // Note: we repeatedly check for IsEmpty because the selection
                    // may become empty as a result of normalization after formatting
                    // (thai character sequence).
                    LocalValueEntry propertyEntry = springloadFormattingValues.Current;
                    Invariant.Assert(TextSchema.IsCharacterProperty(propertyEntry.Property));
                    base.ApplyPropertyValue(propertyEntry.Property, propertyEntry.Value);
                }

                ClearSpringloadFormatting();
            }
        }

        #endregion Springload Formatting

        #region Caret Support

        // ..............................................................
        //
        // Caret Support
        //
        // ..............................................................

        // Shows/hides the caret and scrolls it into view if requested.
        // Called when the range is moved or layout is updated.
        internal void UpdateCaretState(CaretScrollMethod caretScrollMethod)
        {
            Invariant.Assert(caretScrollMethod != CaretScrollMethod.Unset);

            if (_pendingCaretNavigation)
            {
                caretScrollMethod = CaretScrollMethod.Navigation;
                _pendingCaretNavigation = false;
            }

            if (_caretScrollMethod == CaretScrollMethod.Unset)
            {
                _caretScrollMethod = caretScrollMethod;

                // Post a "Loaded" priority operation to the dispatcher queue.

                // Operations at Loaded priority are processed when layout and render is
                // done but just before items at input priority are serviced.
                // We want the update caret worker to run after layout is clean.
                if (_textEditor.TextView != null && _textEditor.TextView.IsValid)
                {
                    UpdateCaretStateWorker(null);
                }
                else
                {
                    _pendingUpdateCaretStateCallback = true;
                }
            }
            else if (caretScrollMethod != CaretScrollMethod.None)
            {
                _caretScrollMethod = caretScrollMethod;
            }
        }

        // Get the caret brush that is the inverted color from the system window or background color.
        internal static Brush GetCaretBrush(TextEditor textEditor)
        {
            Color backgroundColor;
            ITextSelection focusedTextSelection;
            object backgroundPropertyValue;

            // If TextBoxBase.CaretBrush has been set, use that instead of the default inverting behavior.
            Brush caretBrush = (Brush)textEditor.UiScope.GetValue(TextBoxBase.CaretBrushProperty);
            if (caretBrush != null)
            {
                return caretBrush;
            }

            // Get the default background from the system color or UiScope's background
            backgroundPropertyValue = textEditor.UiScope.GetValue(System.Windows.Controls.Panel.BackgroundProperty);
            if (backgroundPropertyValue != null && backgroundPropertyValue != DependencyProperty.UnsetValue &&
                backgroundPropertyValue is SolidColorBrush)
            {
                backgroundColor = ((SolidColorBrush)backgroundPropertyValue).Color;
            }
            else
            {
                backgroundColor = SystemColors.WindowColor;
            }

            // Get the background color from current selection
            focusedTextSelection = textEditor.Selection;
            if (focusedTextSelection is TextSelection)
            {
                backgroundPropertyValue = ((TextSelection)focusedTextSelection).GetCurrentValue(TextElement.BackgroundProperty);
                if (backgroundPropertyValue != null && backgroundPropertyValue != DependencyProperty.UnsetValue)
                {
                    if (backgroundPropertyValue is SolidColorBrush)
                    {
                        backgroundColor = ((SolidColorBrush)backgroundPropertyValue).Color;
                    }
                }
            }

            // Invert the color to get the caret color from the system window or background color.
            byte r = (byte)~(backgroundColor.R);
            byte g = (byte)~(backgroundColor.G);
            byte b = (byte)~(backgroundColor.B);

            caretBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
            caretBrush.Freeze();
            return caretBrush;
        }

        #endregion Caret Support

        #region Bidi Support

        //......................................................
        //
        //  BIDI Support
        //
        //......................................................

        /// <summary>
        /// Check the installed bidi input language from the current
        /// input keyboard list.
        /// </summary>
        internal static bool IsBidiInputLanguageInstalled()
        {
            bool bidiInputLanguageInstalled;

            bidiInputLanguageInstalled = false;

            int keyboardListCount = (int)SafeNativeMethods.GetKeyboardLayoutList(0, null);
            if (keyboardListCount > 0)
            {
                int keyboardListIndex;
                IntPtr[] keyboardList;

                keyboardList = new IntPtr[keyboardListCount];

                keyboardListCount = SafeNativeMethods.GetKeyboardLayoutList(keyboardListCount, keyboardList);

                for (keyboardListIndex = 0;
                     (keyboardListIndex < keyboardList.Length) && (keyboardListIndex < keyboardListCount);
                     keyboardListIndex++)
                {
                    CultureInfo cultureInfo = new CultureInfo((short)keyboardList[keyboardListIndex]);

                    if (IsBidiInputLanguage(cultureInfo))
                    {
                        bidiInputLanguageInstalled = true;
                        break;
                    }
                }
            }

            return bidiInputLanguageInstalled;
        }

        #endregion Bidi Support

        // Forces a synchronous layout validation, up to the selection moving position.
        void ITextSelection.ValidateLayout()
        {
            ((ITextSelection)this).MovingPosition.ValidateLayout();
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Caret associated with this TextSelection.
        internal CaretElement CaretElement
        {
            get
            {
                return _caretElement;
            }
        }

        // Caret associated with this TextSelection.
        CaretElement ITextSelection.CaretElement
        {
            get
            {
                return this.CaretElement;
            }
        }

        // Returns true iff there are no additional insertion positions are either
        // end of the selection.
        bool ITextSelection.CoversEntireContent
        {
            get
            {
                ITextSelection This = this;

                return (This.Start.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.Text &&
                        This.End.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text &&
                        This.Start.GetNextInsertionPosition(LogicalDirection.Backward) == null &&
                        This.End.GetNextInsertionPosition(LogicalDirection.Forward) == null);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Stores this TextSelection into our TLS slot.
        private void SetThreadSelection()
        {
            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            // Store this selection as focused one
            threadLocalStore.FocusedTextSelection = this;
        }

        // Removes this TextSelection from our TLS slot.
        private void ClearThreadSelection()
        {
            // Clear currently focused selection, if it's us
            if (TextEditor._ThreadLocalStore.FocusedTextSelection == this)
            {
                TextEditor._ThreadLocalStore.FocusedTextSelection = null;
            }
        }

        // GotKeyboardFocus event handler.  Called by UpdateCaretAndHighlight.
        private void Highlight()
        {
            ITextContainer textContainer = ((ITextSelection)this).Start.TextContainer;

            
            // If we are using the adorner, then we should not instantiate highlight layers
            // for TextContainer or PasswordTextContainer.
            if (FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRendering
                && (textContainer is TextContainer || textContainer is PasswordTextContainer))
            {
                return;
            }

            // Make sure that a highlight layer exists for drawing this selection
            if (_highlightLayer == null)
            {
                _highlightLayer = new TextSelectionHighlightLayer(this);
            }

            // Make selection visible
            if (textContainer.Highlights.GetLayer(typeof(TextSelection)) == null)
            {
                textContainer.Highlights.AddLayer(_highlightLayer);
            }
        }

        // LostKeyboardFocus event handler.  Called by UpdateCaretAndHighlight.
        private void Unhighlight()
        {
            ITextContainer textContainer = ((ITextSelection)this).Start.TextContainer;
            TextSelectionHighlightLayer highlightLayer = textContainer.Highlights.GetLayer(typeof(TextSelection)) as TextSelectionHighlightLayer;
            if (highlightLayer != null)
            {
                textContainer.Highlights.RemoveLayer(highlightLayer);
                Invariant.Assert(textContainer.Highlights.GetLayer(typeof(TextSelection)) == null);
            }
        }

        //......................................................
        //
        //  Active Positions of Selection
        //
        //......................................................

        /// <summary>
        /// Stores normalized anchor and moving positions for the selection.
        /// Ensures that they are both inside of range Start/End.
        /// </summary>
        /// <param name="anchorPosition">
        /// A position which must be stored as initial position for the selection.
        /// </param>
        /// <param name="movingPosition">
        /// The "hot" or active selection edge which responds to user input.
        /// </param>
        private void SetActivePositions(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            // The following settings are used in auto-word exapnsion.
            // By setting a _previousPosition we are clearing them all -
            // they will be re-initialized in the beginning of a selection
            // expansion guesture in ExtendSelectionByMouse method
            // Previous position is needed for selection gestures to remember
            // where mouse drag happed last time. Used for word autoexpansion.
            _previousCursorPosition = null;

            if (this.IsEmpty)
            {
                _anchorPosition = null;
                _movingPositionEdge = MovingEdge.None;
                return;
            }

            Invariant.Assert(anchorPosition != null);

            ITextSelection thisSelection = (ITextSelection)this;

            // Normalize and store new selection anchor position
            _anchorPosition = anchorPosition.GetInsertionPosition(anchorPosition.LogicalDirection);

            // Ensure that anchor position is within one of text segments
            if (_anchorPosition.CompareTo(thisSelection.Start) < 0)
            {
                _anchorPosition = thisSelection.Start.GetFrozenPointer(_anchorPosition.LogicalDirection);
            }
            else if (_anchorPosition.CompareTo(thisSelection.End) > 0)
            {
                _anchorPosition = thisSelection.End.GetFrozenPointer(_anchorPosition.LogicalDirection);
            }

            _movingPositionEdge = ConvertToMovingEdge(anchorPosition, movingPosition);
            _movingPositionDirection = movingPosition.LogicalDirection;
        }

        // Uses the current selection state to match an ITextPointer to one of the possible
        // moving position edges.
        private MovingEdge ConvertToMovingEdge(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            ITextSelection thisSelection = this;
            MovingEdge movingEdge;

            if (thisSelection.IsEmpty)
            {
                // Empty selections have no moving edge.
                movingEdge = MovingEdge.None;
            }
            else if (thisSelection.TextSegments.Count < 2)
            {
                // Simple text selections move opposite their anchor positions.
                movingEdge = (anchorPosition.CompareTo(movingPosition) <= 0) ? MovingEdge.End : MovingEdge.Start;
            }
            else
            {
                // Table selection.  Look for an exact match.
                if (movingPosition.CompareTo(thisSelection.Start) == 0)
                {
                    movingEdge = MovingEdge.Start;
                }
                else if (movingPosition.CompareTo(thisSelection.End) == 0)
                {
                    movingEdge = MovingEdge.End;
                }
                else if (movingPosition.CompareTo(thisSelection.TextSegments[0].End) == 0)
                {
                    movingEdge = MovingEdge.StartInner;
                }
                else if (movingPosition.CompareTo(thisSelection.TextSegments[thisSelection.TextSegments.Count-1].Start) == 0)
                {
                    movingEdge = MovingEdge.EndInner;
                }
                else
                {
                    movingEdge = (anchorPosition.CompareTo(movingPosition) <= 0) ? MovingEdge.End : MovingEdge.Start;
                }
            }

            return movingEdge;
        }

        //......................................................
        //
        //  Selection Building With Mouse
        //
        //......................................................

        // Moves the selection to the mouse cursor position.
        // If the cursor is facing a UIElement, select the UIElement.
        // Sets new selection anchor to a given cursorPosition.
        private void MoveSelectionByMouse(ITextPointer cursorPosition, Point cursorMousePoint)
        {
            ITextSelection thisSelection = (ITextSelection)this;

            if (this.TextView == null)
            {
                return;
            }
            Invariant.Assert(this.TextView.IsValid); // We just checked RenderScope. We'll use TextView below

            ITextPointer movingPosition = null;

            if (cursorPosition.GetPointerContext(cursorPosition.LogicalDirection) == TextPointerContext.EmbeddedElement)
            {
                Rect objectEdgeRect = this.TextView.GetRectangleFromTextPosition(cursorPosition);

                // Check for embedded object.
                // If the click happend inside of it we need to select it as a whole, when content is not read-only.
                if (!_textEditor.IsReadOnly && ShouldSelectEmbeddedObject(cursorPosition, cursorMousePoint, objectEdgeRect))
                {
                    movingPosition = cursorPosition.GetNextContextPosition(cursorPosition.LogicalDirection);
                }
            }

            // Move selection to this position
            if (movingPosition == null)
            {
                thisSelection.SetCaretToPosition(cursorPosition, cursorPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/false);
            }
            else
            {
                thisSelection.Select(cursorPosition, movingPosition);
            }
        }

        // Helper for MoveSelectionByMouse
        private bool ShouldSelectEmbeddedObject(ITextPointer cursorPosition, Point cursorMousePoint, Rect objectEdgeRect)
        {
            // Although we now know that cursorPosition is facing an embedded object,
            // we still need an additional test to determine if the original mouse point
            // fell within the object or outside it's bouding box (which can happen when
            // a mouse click is snapped to the nearest content).
            // If the mouse point is outside the object, we don't want to select it.
            if (!objectEdgeRect.IsEmpty &&
                cursorMousePoint.Y >= objectEdgeRect.Y && cursorMousePoint.Y < objectEdgeRect.Y + objectEdgeRect.Height)
            {
                // Compare X coordinates of mouse down point and object edge rect,
                // depending on the FlowDirection of the render scope and paragraph content.

                FlowDirection renderScopeFlowDirection = (FlowDirection)this.TextView.RenderScope.GetValue(Block.FlowDirectionProperty);
                FlowDirection paragraphFlowDirection = (FlowDirection)cursorPosition.GetValue(Block.FlowDirectionProperty);

                if (renderScopeFlowDirection == FlowDirection.LeftToRight)
                {
                    if (paragraphFlowDirection == FlowDirection.LeftToRight &&
                        (cursorPosition.LogicalDirection == LogicalDirection.Forward && objectEdgeRect.X < cursorMousePoint.X ||
                        cursorPosition.LogicalDirection == LogicalDirection.Backward && cursorMousePoint.X < objectEdgeRect.X))
                    {
                        return true;
                    }
                    else if (paragraphFlowDirection == FlowDirection.RightToLeft &&
                        (cursorPosition.LogicalDirection == LogicalDirection.Forward && objectEdgeRect.X > cursorMousePoint.X ||
                        cursorPosition.LogicalDirection == LogicalDirection.Backward && cursorMousePoint.X > objectEdgeRect.X))
                    {
                        return true;
                    }
                }
                else
                {
                    if (paragraphFlowDirection == FlowDirection.LeftToRight &&
                        (cursorPosition.LogicalDirection == LogicalDirection.Forward && objectEdgeRect.X > cursorMousePoint.X ||
                        cursorPosition.LogicalDirection == LogicalDirection.Backward && cursorMousePoint.X > objectEdgeRect.X))
                    {
                        return true;
                    }
                    else if (paragraphFlowDirection == FlowDirection.RightToLeft &&
                        (cursorPosition.LogicalDirection == LogicalDirection.Forward && objectEdgeRect.X < cursorMousePoint.X ||
                        cursorPosition.LogicalDirection == LogicalDirection.Backward && cursorMousePoint.X < objectEdgeRect.X))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //......................................................
        //
        //  Caret Support
        //
        //......................................................

        // Redraws a caret using current setting for italic - taking springload formatting into account.
        private static void RefreshCaret(TextEditor textEditor, ITextSelection textSelection)
        {
            object fontStylePropertyValue;
            bool italic;

            if (textSelection == null || textSelection.CaretElement == null)
            {
                return;
            }

            // NOTE: We are using GetCurrentValue to take springload formatting into account.
            fontStylePropertyValue = ((TextSelection)textSelection).GetCurrentValue(TextElement.FontStyleProperty);
            italic = (textEditor.AcceptsRichContent && fontStylePropertyValue != DependencyProperty.UnsetValue && (FontStyle)fontStylePropertyValue == FontStyles.Italic);

            textSelection.CaretElement.RefreshCaret(italic);
        }

        // Called after a caret navigation, to signal that the next caret
        // scroll-into-view should include hueristics to include following
        // text.
        internal void OnCaretNavigation()
        {
            _pendingCaretNavigation = true;
        }

        // Called after a caret navigation, to signal that the next caret
        // scroll-into-view should include hueristics to include following
        // text.
        void ITextSelection.OnCaretNavigation()
        {
            OnCaretNavigation();
        }

        // Callback for UpdateCaretState worker.
        private object UpdateCaretStateWorker(object o)
        {
            _pendingUpdateCaretStateCallback = false;

            // This can happen if selection has been detached by TextEditor.OnDetach.
            if (_textEditor == null)
            {
                return null;
            }

            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            CaretScrollMethod caretScrollMethod = _caretScrollMethod;
            _caretScrollMethod = CaretScrollMethod.Unset;

            // Use the locally defined caretElement because _caretElement can be null by
            // detaching CaretElement object
            CaretElement caretElement = _caretElement;

            if (caretElement == null)
            {
                return null;
            }

            if (threadLocalStore.FocusedTextSelection == null)
            {
                // If we have multiple windows open, a non-blinking caret might be showing
                // in the given TextEditor's UiScope.  If the selection for that Editor is
                // not empty, we need to hide the caret.
                if (!this.IsEmpty)
                {
                    caretElement.Hide();
                }

                return null;
            }

            // When the TextView is not valid, there is nothing to do
            if (_textEditor.TextView == null || !_textEditor.TextView.IsValid)
            {
                // Do we need to clear obsolte highlight?
                return null;
            }

            if (!this.VerifyAdornerLayerExists())
            {
                caretElement.Hide();
            }

            // Identify caret position
            // Make sure that moving position is inside of selection
            ITextPointer caretPosition = IdentifyCaretPosition(this);

            // If caret position is not valid in focusedTextSelection.TextView, we cannot update it.
            if (caretPosition.HasValidLayout)
            {
                Rect caretRectangle;
                bool italic = false;
                bool caretVisible = this.IsEmpty && (!_textEditor.IsReadOnly || _textEditor.IsReadOnlyCaretVisible);

                if (!this.IsInterimSelection)
                {
                    caretRectangle = CalculateCaretRectangle(this, caretPosition);

                    if (this.IsEmpty)
                    {
                        // Identify italic condition (including springload) - to use for caret shaping
                        object fontStylePropertyValue = GetPropertyValue(TextElement.FontStyleProperty);
                        italic = (_textEditor.AcceptsRichContent && fontStylePropertyValue != DependencyProperty.UnsetValue && (FontStyle)fontStylePropertyValue == FontStyles.Italic);
                    }
                }
                else
                {
                    caretRectangle = CalculateInterimCaretRectangle(this);

                    // Caret always visible on the interim input mode.
                    caretVisible = true;
                }

                Brush caretBrush = GetCaretBrush(_textEditor);

                // Calculate the scroll origin position to scroll it with the caret position.
                double scrollToOriginPosition = CalculateScrollToOriginPosition(_textEditor, caretPosition, caretRectangle.X);

                // Re-render the caret.
                // Get a bounding rect from the active end of selection.
                caretElement.Update(caretVisible, caretRectangle, caretBrush, 1.0, italic, caretScrollMethod, scrollToOriginPosition);
            }

            //  CaretElement.Update(...) makes a conditional call to BringIntoView()
            //  on the text view's associated element, which makes the view invalid...
            if (this.TextView.IsValid && !this.TextView.RendersOwnSelection)
            {
                // Re-render selection. Need to do this to invalidate and update adorner for this caret element.
                caretElement.UpdateSelection();
            }

            return null;
        }

        // Helper for UpdateCaretState -- identifies caret position from text selection.
        private static ITextPointer IdentifyCaretPosition(ITextSelection currentTextSelection)
        {
            ITextPointer caretPosition = currentTextSelection.MovingPosition;

            if (!currentTextSelection.IsEmpty)
            {
                // Even when we do not draw the blinking caret, we need this position
                // for scrolling caret position into view.

                // Special case for nonempty selection extended beyond end of line
                if ((caretPosition.LogicalDirection == LogicalDirection.Backward && //
                     caretPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart) || //
                    TextPointerBase.IsAfterLastParagraph(caretPosition))
                {
                    // This means that selection has been expanded by ExtendToLineEnd/ExtendToDocumentEnd command.
                    // TextView in this case cannot give the rect from this position;
                    // we need to move backward to the end of content
                    caretPosition = caretPosition.CreatePointer();
                    caretPosition.MoveToNextInsertionPosition(LogicalDirection.Backward);
                    caretPosition.SetLogicalDirection(LogicalDirection.Forward);
                }
            }

            // TextView.GetRectangleFromTextPosition returns the end of character rect in case of Backward
            // logical direction at the start caret position of the docuemtn or paragraph, so we reset the
            // logical direction with Forward to get the right rect of caret at the start position of
            // document or paragraph.
            if (caretPosition.LogicalDirection == LogicalDirection.Backward && //
                caretPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart && //
                (caretPosition.GetNextInsertionPosition(LogicalDirection.Backward) == null || //
                 TextPointerBase.IsNextToAnyBreak(caretPosition, LogicalDirection.Backward)))
            {
                caretPosition = caretPosition.CreatePointer();
                caretPosition.SetLogicalDirection(LogicalDirection.Forward);
            }

            return caretPosition;
        }

        // Helper for UpdateCaretState -- calculates caret rectangle from text selection.
        // regular (non-interim) case of caret positioning
        private static Rect CalculateCaretRectangle(ITextSelection currentTextSelection, ITextPointer caretPosition)
        {
            Transform caretTransform;
            Rect caretRectangle = currentTextSelection.TextView.GetRawRectangleFromTextPosition(caretPosition, out caretTransform);

            if (caretRectangle.IsEmpty)
            {
                // Caret is not at an insertion position, it has no geometry
                // and will not be displayed.
                return Rect.Empty;
            }

            // Convert to local coordiantes.
            caretRectangle = caretTransform.TransformBounds(caretRectangle);

            // We will use the system defined caret width later.
            caretRectangle.Width = 0;

            if (currentTextSelection.IsEmpty)
            {
                // Calculate caret height - from current font size (ignoring a rect returned by TextView,
                // as it can be a rect of embedded object, which should not affect caret height)
                double fontSize = (double)currentTextSelection.GetPropertyValue(TextElement.FontSizeProperty);
                FontFamily fontFamily = (FontFamily)currentTextSelection.GetPropertyValue(TextElement.FontFamilyProperty);
                double caretHeight = fontFamily.LineSpacing * fontSize;
                if (caretHeight < caretRectangle.Height)
                {
                    // Decrease the height of caret to the font height and lower the caret to keep its bottom
                    // staying on the baseline.
                    caretRectangle.Y += caretRectangle.Height - caretHeight;
                    caretRectangle.Height = caretHeight;
                }

                if (!caretTransform.IsIdentity)
                {
                    Point top = new Point(caretRectangle.X, caretRectangle.Y);
                    Point bottom = new Point(caretRectangle.X, caretRectangle.Y + caretRectangle.Height);
                    caretTransform.TryTransform(top, out top);
                    caretTransform.TryTransform(bottom, out bottom);
                    caretRectangle.Y += caretRectangle.Height - Math.Abs(bottom.Y - top.Y);
                    caretRectangle.Height = Math.Abs(bottom.Y - top.Y);
                }
            }

            return caretRectangle;
        }

        // Helper for UpdateCaretState -- handles the korean interim caret.
        private static Rect CalculateInterimCaretRectangle(ITextSelection focusedTextSelection)
        {
            // There is no checking for empty selection here. Is this correct? Do we want this behavior for non-empty selection?

            // Get the current flow direction on the selection of interim.
            // This is for getting the right size of distance on the interim character
            // whatever the current flow direction is.
            FlowDirection flowDirection = (FlowDirection)focusedTextSelection.Start.GetValue(FrameworkElement.FlowDirectionProperty);

            ITextPointer nextCharacterPosition;
            Rect nextCharacterRectangle;
            Rect caretRectangle;

            if (flowDirection != FlowDirection.RightToLeft)
            {
                // Flow direction is Left-to-Right
                // Get the rectangle for both interim selection start position and the next character
                // position.
                nextCharacterPosition = focusedTextSelection.Start.CreatePointer(LogicalDirection.Forward);
                caretRectangle = focusedTextSelection.TextView.GetRectangleFromTextPosition(nextCharacterPosition);

                // Get the next character position from the start position of the interim selection
                // on the left to right flow direction.
                nextCharacterPosition.MoveToNextInsertionPosition(LogicalDirection.Forward);
                nextCharacterPosition.SetLogicalDirection(LogicalDirection.Backward);
                nextCharacterRectangle = focusedTextSelection.TextView.GetRectangleFromTextPosition(nextCharacterPosition);
            }
            else
            {
                // Flow direction is Right-to-Left
                // Get the rectangle for both interim selection end position and the next character
                // position.
                nextCharacterPosition = focusedTextSelection.End.CreatePointer(LogicalDirection.Backward);
                caretRectangle = focusedTextSelection.TextView.GetRectangleFromTextPosition(nextCharacterPosition);

                // Get the next character position from the end position of the interim selection
                // on the right to left flow direction.
                nextCharacterPosition.MoveToNextInsertionPosition(LogicalDirection.Backward);
                nextCharacterPosition.SetLogicalDirection(LogicalDirection.Forward);
                nextCharacterRectangle = focusedTextSelection.TextView.GetRectangleFromTextPosition(nextCharacterPosition);
            }

            // The interim next character position should be great than the current interim position.
            // Otherwise, we show the caret as the normal state that use the system caret width.
            // In case of BiDi character, the next character position can be greater than the current position.
            if (!caretRectangle.IsEmpty && !nextCharacterRectangle.IsEmpty && nextCharacterRectangle.Left > caretRectangle.Left)
            {
                // Get the interim caret width to show the interim block caret.
                caretRectangle.Width = nextCharacterRectangle.Left - caretRectangle.Left;
            }

            return caretRectangle;
        }

        // Helper for UpdateCaretStateWorker -- Calculate the scroll origin position to scroll caret
        // with the scroll origin position so that we can ensure of displaying caret with the wrapped word.
        //
        // There are four cases of different corrdinate by the flow direction on UiScope and Paragraph.
        // UiScope has two flow direction which is LeftToRightflow directioin and another is RightToLeft.
        // Paragraph has also two flow direction which is LeftToRightflow directioin and another is RightToLeft.
        //
        // The below is the example of how horizontal corrdinate and scroll origin value base on the different
        // four cases. So we have to calculate the scroll to origin position base on the case. Simply we can
        // get the scroll to origin value as zero if UiScope and Paragraph's flow direction is the same.
        // Otherwise, the scroll to origin value is the extent width value that is the max width.
        //
        // <<For instance>>
        //  Case 1.
        //      UiScope FlowDirection:              LTR(LeftToRight)
        //      Paragraph FlowDirection:            LTR(LefTToRight)
        //          Horizontal origin:              "Left"
        //          Scroll horizontal origin:       "0"
        //          Wrapping to:                    "Left"
        //              ABC ......
        //              XYZ|
        //
        //  Case 2.
        //      UiScope FlowDirection:              LTR(LeftToRight)
        //      Paragraph FlowDirection:            RTL(RightToLeft)
        //          Horizontal origin:              "Left"
        //          Scroll horizontal origin:       "Max:Extent Width"
        //          Wrapping to:                    "Right"
        //              ......ABC
        //                    XYZ|
        //
        //  Case 3.
        //      UiScope FlowDirection:              RTL(RightToLeft)
        //      Paragraph FlowDirection:            RTL(RightToLeft)
        //          horizontal origin:              "Right"
        //          Scroll horizontal origin:       "0"
        //          Wrapping to:                    "Right"
        //              ......ABC
        //                    XYZ|
        //
        //  Case 4.
        //      UiScope FlowDirection:              RTL(RightToLeft)
        //      Paragraph FlowDirection:            LTR(LefTToRight)
        //          horizontal origin:              "Right"
        //          Scroll horizontal origin:       "Max:Extent Width"
        //          Wrapping to:                    "Left"
        //              ABC ......
        //              XYZ|
        private static double CalculateScrollToOriginPosition(TextEditor textEditor, ITextPointer caretPosition, double horizontalCaretPosition)
        {
            double scrollToOriginPosition = double.NaN;

            if (textEditor.UiScope is TextBoxBase)
            {
                double viewportWidth = ((TextBoxBase)textEditor.UiScope).ViewportWidth;
                double extentWidth = ((TextBoxBase)textEditor.UiScope).ExtentWidth;

                // Calculate the scroll to the origin position position when the horizontal scroll is available
                if (viewportWidth != 0 && extentWidth != 0 && viewportWidth < extentWidth)
                {
                    bool needScrollToOriginPosition = false;

                    // Check whether we need to calculate the scroll origin position to scroll it with the caret
                    // position. If the caret position is out of the current visual viewport area, the scroll
                    // to origin positioin will be calculated to scroll into the origin position first that
                    // ensure of displaying the wrapped word.
                    //
                    // Note that horizontalCaretPosition is always relative to the viewport, not the document.
                    if (horizontalCaretPosition < 0 || horizontalCaretPosition >= viewportWidth)
                    {
                        needScrollToOriginPosition = true;
                    }

                    if (needScrollToOriginPosition)
                    {
                        // Set the scroll original position as zero
                        scrollToOriginPosition = 0;

                        // Get the flow direction of uiScope
                        FlowDirection uiScopeflowDirection = (FlowDirection)textEditor.UiScope.GetValue(FrameworkElement.FlowDirectionProperty);

                        // Get the flow direction of the current paragraph and compare it with uiScope's flow direction.
                        Block paragraphOrBlockUIContainer = (caretPosition is TextPointer) ? ((TextPointer)caretPosition).ParagraphOrBlockUIContainer : null;
                        if (paragraphOrBlockUIContainer != null)
                        {
                            FlowDirection pagraphFlowDirection = paragraphOrBlockUIContainer.FlowDirection;

                            // If the flow direction is different between uiScopoe and paragaph,
                            // the original scroll position is the extent width value.
                            if (uiScopeflowDirection != pagraphFlowDirection)
                            {
                                scrollToOriginPosition = extentWidth;
                            }
                        }

                        // Adjust scroll position by current viewport offset
                        scrollToOriginPosition -= ((TextBoxBase)textEditor.UiScope).HorizontalOffset;
                    }
                }
            }

            return scrollToOriginPosition;
        }

        private CaretElement EnsureCaret(bool isBlinkEnabled, bool isSelectionActive, CaretScrollMethod scrollMethod)
        {
            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            if (_caretElement == null)
            {
                // Create new caret
                _caretElement = new CaretElement(_textEditor, isBlinkEnabled);
                _caretElement.IsSelectionActive = isSelectionActive;

                // Check the current input language to draw the BiDi caret in case of BiDi language
                // like as Arabic or Hebrew input language.
                // Move this somewhere where it'll only be called once
                if (IsBidiInputLanguage(InputLanguageManager.Current.CurrentInputLanguage))
                {
                    TextEditor._ThreadLocalStore.Bidi = true;
                }
                else
                {
                    TextEditor._ThreadLocalStore.Bidi = false;
                }
            }
            else
            {
                // Please note that it is important to set the IsSelectionActive property before 
                // calling SetBlinking. This is because SetBlinking calls Win32CreateCaret & 
                // Win32DestroyCaret both of which meaningfully use this flag.
                _caretElement.IsSelectionActive = isSelectionActive;
                _caretElement.SetBlinking(isBlinkEnabled);
            }

            UpdateCaretState(scrollMethod);

            return _caretElement;
        }

        /// <summary>
        /// Walk up the tree from the RenderScope to the UiScope until we find an
        /// AdornerDecorator or ScrollContentPresenter.
        /// </summary>
        /// <returns>true if one is found, false otherwise</returns>
        private bool VerifyAdornerLayerExists()
        {
            DependencyObject element = TextView.RenderScope;
            while (element != _textEditor.UiScope && element != null)
            {
                if (element is AdornerDecorator || element is System.Windows.Controls.ScrollContentPresenter)
                {
                    return true;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        //......................................................
        //
        //  BIDI Support
        //
        //......................................................

        /// <summary>
        /// Check the input language of cultureInfo whether it is the bi-directional language or not.
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns>
        /// Return true if the passed cultureInfo is the bi-directional language like as Arabic or Hebrew.
        /// Otherwise, return false.
        /// </returns>
        private static bool IsBidiInputLanguage(CultureInfo cultureInfo)
        {
            bool bidiInput;
            string fontSignature;

            bidiInput = false;

            fontSignature = new String(new Char[FONTSIGNATURE_SIZE]);

            // Get the font signature to know the current LCID is BiDi(Arabic, Hebrew etc.) or not.
            if (UnsafeNativeMethods.GetLocaleInfoW(cultureInfo.LCID, NativeMethods.LOCALE_FONTSIGNATURE, fontSignature, FONTSIGNATURE_SIZE) != 0)
            {
                // Compare fontSignature[7] with 0x0800 to detect BiDi language.
                if ((fontSignature[FONTSIGNATURE_BIDI_INDEX] & FONTSIGNATURE_BIDI) != 0)
                {
                    bidiInput = true;
                }
            }

            return bidiInput;
        }

        //......................................................
        //
        //  Table Selection
        //
        //......................................................

        private static TableCell FindCellAtColumnIndex(TableCellCollection cells, int columnIndex)
        {
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                TableCell cell;
                int startColumnIndex;
                int endColumnIndex;

                cell = cells[cellIndex];
                startColumnIndex = cell.ColumnIndex;
                endColumnIndex = startColumnIndex + cell.ColumnSpan - 1;

                if (startColumnIndex <= columnIndex && columnIndex <= endColumnIndex)
                {
                    return cell;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines if the given element has any ancestor.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static bool IsRootElement(DependencyObject element)
        {
            return GetParentElement(element) == null;
        }

        /// <summary>
        /// Workaround to approximate whether or not our window is active.
        /// </summary>
        /// <returns></returns>
        private bool IsFocusWithinRoot()
        {
            DependencyObject element = this.UiScope;
            DependencyObject parent = this.UiScope;

            while (parent != null)
            {
                element = parent;
                parent = GetParentElement(element);
            }

            if (element is UIElement && ((UIElement)element).IsKeyboardFocusWithin)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper method to determine an element's parent, using the umpteen methods of
        /// parenting.
        /// </summary>
        /// <param name="element"></param>
        private static DependencyObject GetParentElement(DependencyObject element)
        {
            DependencyObject parent;

            if (element is FrameworkElement || element is FrameworkContentElement)
            {
                parent = LogicalTreeHelper.GetParent(element);
                if (parent == null && element is FrameworkElement)
                {
                    parent = ((FrameworkElement)element).TemplatedParent;
                    if (parent == null && element is Visual)
                    {
                        parent = VisualTreeHelper.GetParent(element);
                    }
                }
            }
            else if (element is Visual)
            {
                parent = VisualTreeHelper.GetParent(element);
            }
            else
            {
                parent = null;
            }

            return parent;
        }

        // Removes the caret from the visual tree.
        private void DetachCaretFromVisualTree()
        {
            if (_caretElement != null)
            {
                _caretElement.DetachFromView();
                _caretElement = null;
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        TextEditor ITextSelection.TextEditor
        {
            get
            {
                return _textEditor;
            }
        }

        ITextView ITextSelection.TextView
        {
            get
            {
                return _textEditor.TextView;
            }
        }

        private ITextView TextView
        {
            get
            {
                return ((ITextSelection)this).TextView;
            }
        }

        private TextStore TextStore
        {
            get
            {
                return _textEditor.TextStore;
            }
        }

        private ImmComposition ImmComposition
        {
            get
            {
                return _textEditor.ImmComposition;
            }
        }

        // UiScope associated with this TextSelection's TextEditor.
        private FrameworkElement UiScope
        {
            get
            {
                return _textEditor.UiScope;
            }
        }

        // Position from which to spring load property values.
        private ITextPointer PropertyPosition
        {
            get
            {
                ITextSelection This = this;
                ITextPointer position = null;

                if (!This.IsEmpty)
                {
                    position = TextPointerBase.GetFollowingNonMergeableInlineContentStart(This.Start);
                }

                if (position == null)
                {
                    position = This.Start;
                }

                position.Freeze();
                return position;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // The four possible positions for the selection moving position.
        private enum MovingEdge { Start, StartInner, EndInner, End, None };

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Selected Content
        // ----------------

        // TextEditor that owns this selection.
        private TextEditor _textEditor;

        // Container for highlights on selected text.
        private TextSelectionHighlightLayer _highlightLayer;

        // Springload Formatting
        // ---------------------

        // Dependency object representing a set of springloaded formatting properties
        private DependencyObject _springloadFormatting;

        // Selection autoexpansion for unit boundaries
        // -------------------------------------------

        // A position where the last selection gesture has been initiated.
        // Selection gestures are: mouseDown-Move-...-Move-Up, Shift+ArrowDown-...-Down-Up.
        // Note that Shift+MouseDown-Move-...-Move-Up is a gesture continuation,
        // it does not change _anchorPosition.
        // Actual selection always contains this position but may be extended
        // to a wider range - to encompass whole units such as words,
        // hyperlinks, sentences, paragraphs, table cells etc.
        // Must be renamed to _initialPosition.
        // It may coinside with Start or End, but it may be also in some other
        // corner of rectangular table range, or within a word-expanded selection.
        // Use TextNavigator for this member
        private ITextPointer _anchorPosition;

        // A position to which user input is applied.
        // It may coinside with Start or End, but it may be also in some other
        // corner of rectangular table range.
        private MovingEdge _movingPositionEdge;

        // LogicalDirection for the moving position.
        // If the selection is empty, this value is ignored and
        // the direction matches this.Start.  Otherwise we respect
        // the value, which typically points inwards towards the
        // content but may point outward to include an empty line.
        private LogicalDirection _movingPositionDirection;

        // Text position used on previous step of mouse dragging selection
        // It is used in selection dragging heuristic to identify a situation when
        // dragging end returned back to selected area which means that
        // autoword expansion must be temporary stopped - until a new word
        // boundary is crossed. See also _reenterPosition.
        private ITextPointer _previousCursorPosition;

        // Text position where dragged mouse re-entered a selection. This word should not be autoexapnded
        private ITextPointer _reenterPosition;

        // Flag indicating that initial word boundary has been crossed at least once doring selection expansion
        private bool _anchorWordRangeHasBeenCrossedOnce;

        // Flag allows autoword expansion for anchor end of selection
        private bool _allowWordExpansionOnAnchorEnd;

        // Font signature size as 16
        private const int FONTSIGNATURE_SIZE = 16;

        // BIDI font signature index from GetLocaleInfo.
        private const int FONTSIGNATURE_BIDI_INDEX = 7;

        // BIDI font signature value
        private const int FONTSIGNATURE_BIDI = 0x0800;

        // Signals how the caret should be scrolled into view on the next
        // caret update.
        private CaretScrollMethod _caretScrollMethod;

        // If true, signals that the next caret
        // scroll-into-view should include hueristics to include following
        // text.
        private bool _pendingCaretNavigation;

        // Caret associated with this selection.
        private CaretElement _caretElement;

        // Flag set true after scheduling a callback to UpdateCaretStateWorker.
        // Used to prevent unbounded callback allocations on the Dispatcher queue --
        // we fold redundant update requests into a single queue item.
        bool _pendingUpdateCaretStateCallback;

        #endregion Private Fields
    }
}

