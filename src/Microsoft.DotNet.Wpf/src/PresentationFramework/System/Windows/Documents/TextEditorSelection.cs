// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A component of TextEditor supporting selection and navigation
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
    using System.Security; // SecurityCritical attribute.

    using System.Windows.Threading;
    using System.Windows.Input;
    using System.Windows.Controls; // ScrollChangedEventArgs
    using System.Windows.Controls.Primitives;  // CharacterCasing, TextBoxBase
    using System.Windows.Media;
    using System.Windows.Markup;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal static class TextEditorSelection
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
            // Shared handlers used multiple times below.
            ExecutedRoutedEventHandler nyiCommandHandler = new ExecutedRoutedEventHandler(OnNYICommand);
            CanExecuteRoutedEventHandler queryStatusCaretNavigationHandler = new CanExecuteRoutedEventHandler(OnQueryStatusCaretNavigation);
            CanExecuteRoutedEventHandler queryStatusKeyboardSelectionHandler = new CanExecuteRoutedEventHandler(OnQueryStatusKeyboardSelection);
            
            // Standard Commands: Select All
            // -----------------------------
            CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.SelectAll, new ExecutedRoutedEventHandler(OnSelectAll), queryStatusKeyboardSelectionHandler, KeySelectAll, SRID.KeySelectAllDisplayString);

            // Editing Commands : Caret Navigation
            // -----------------------------------
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveRightByCharacter, new ExecutedRoutedEventHandler(OnMoveRightByCharacter), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveRightByCharacter, SRID.KeyMoveRightByCharacterDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveLeftByCharacter, new ExecutedRoutedEventHandler(OnMoveLeftByCharacter), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveLeftByCharacter, SRID.KeyMoveLeftByCharacterDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveRightByWord, new ExecutedRoutedEventHandler(OnMoveRightByWord), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveRightByWord, SRID.KeyMoveRightByWordDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveLeftByWord, new ExecutedRoutedEventHandler(OnMoveLeftByWord), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveLeftByWord, SRID.KeyMoveLeftByWordDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveDownByLine, new ExecutedRoutedEventHandler(OnMoveDownByLine), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveDownByLine, SRID.KeyMoveDownByLineDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveUpByLine, new ExecutedRoutedEventHandler(OnMoveUpByLine), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveUpByLine, SRID.KeyMoveUpByLineDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveDownByParagraph, new ExecutedRoutedEventHandler(OnMoveDownByParagraph), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveDownByParagraph, SRID.KeyMoveDownByParagraphDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveUpByParagraph, new ExecutedRoutedEventHandler(OnMoveUpByParagraph), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveUpByParagraph, SRID.KeyMoveUpByParagraphDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveDownByPage, new ExecutedRoutedEventHandler(OnMoveDownByPage), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveDownByPage, SRID.KeyMoveDownByPageDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveUpByPage, new ExecutedRoutedEventHandler(OnMoveUpByPage), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveUpByPage, SRID.KeyMoveUpByPageDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToLineStart, new ExecutedRoutedEventHandler(OnMoveToLineStart), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToLineStart, SRID.KeyMoveToLineStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToLineEnd, new ExecutedRoutedEventHandler(OnMoveToLineEnd), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToLineEnd, SRID.KeyMoveToLineEndDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToColumnStart, nyiCommandHandler, queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToColumnStart, SRID.KeyMoveToColumnStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToColumnEnd, nyiCommandHandler, queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToColumnEnd, SRID.KeyMoveToColumnEndDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToWindowTop, nyiCommandHandler, queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToWindowTop, SRID.KeyMoveToWindowTopDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToWindowBottom, nyiCommandHandler, queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToWindowBottom, SRID.KeyMoveToWindowBottomDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToDocumentStart, new ExecutedRoutedEventHandler(OnMoveToDocumentStart), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToDocumentStart, SRID.KeyMoveToDocumentStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MoveToDocumentEnd, new ExecutedRoutedEventHandler(OnMoveToDocumentEnd), queryStatusCaretNavigationHandler, KeyGesture.CreateFromResourceStrings(KeyMoveToDocumentEnd, SRID.KeyMoveToDocumentEndDisplayString));

            // Editing Commands: Selection Building
            // ------------------------------------
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectRightByCharacter, new ExecutedRoutedEventHandler(OnSelectRightByCharacter), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectRightByCharacter, SRID.KeySelectRightByCharacterDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectLeftByCharacter, new ExecutedRoutedEventHandler(OnSelectLeftByCharacter), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectLeftByCharacter, SRID.KeySelectLeftByCharacterDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectRightByWord, new ExecutedRoutedEventHandler(OnSelectRightByWord), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectRightByWord, SRID.KeySelectRightByWordDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectLeftByWord, new ExecutedRoutedEventHandler(OnSelectLeftByWord), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectLeftByWord, SRID.KeySelectLeftByWordDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectDownByLine, new ExecutedRoutedEventHandler(OnSelectDownByLine), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectDownByLine, SRID.KeySelectDownByLineDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectUpByLine, new ExecutedRoutedEventHandler(OnSelectUpByLine), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectUpByLine, SRID.KeySelectUpByLineDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectDownByParagraph, new ExecutedRoutedEventHandler(OnSelectDownByParagraph), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectDownByParagraph, SRID.KeySelectDownByParagraphDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectUpByParagraph, new ExecutedRoutedEventHandler(OnSelectUpByParagraph), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectUpByParagraph, SRID.KeySelectUpByParagraphDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectDownByPage, new ExecutedRoutedEventHandler(OnSelectDownByPage), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectDownByPage, SRID.KeySelectDownByPageDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectUpByPage, new ExecutedRoutedEventHandler(OnSelectUpByPage), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectUpByPage, SRID.KeySelectUpByPageDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToLineStart, new ExecutedRoutedEventHandler(OnSelectToLineStart), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToLineStart, SRID.KeySelectToLineStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToLineEnd, new ExecutedRoutedEventHandler(OnSelectToLineEnd), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToLineEnd, SRID.KeySelectToLineEndDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToColumnStart, nyiCommandHandler, queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToColumnStart, SRID.KeySelectToColumnStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToColumnEnd, nyiCommandHandler, queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToColumnEnd, SRID.KeySelectToColumnEndDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToWindowTop, nyiCommandHandler, queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToWindowTop, SRID.KeySelectToWindowTopDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToWindowBottom, nyiCommandHandler, queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToWindowBottom, SRID.KeySelectToWindowBottomDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToDocumentStart, new ExecutedRoutedEventHandler(OnSelectToDocumentStart), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToDocumentStart, SRID.KeySelectToDocumentStartDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SelectToDocumentEnd, new ExecutedRoutedEventHandler(OnSelectToDocumentEnd), queryStatusKeyboardSelectionHandler, KeyGesture.CreateFromResourceStrings(KeySelectToDocumentEnd, SRID.KeySelectToDocumentEndDisplayString));
        }
        
        /// <summary>
        /// Clears the suggestedX variable of passed TextEditor.
        /// </summary>
        /// <param name="This">TextEditor</param>
        internal static void _ClearSuggestedX(TextEditor This)
        {
            // Discard stored horizontal position.
            // By setting to NaN, we indicate that the first following vertical movement
            // must define suggestedX from the current moving position.
            This._suggestedX = Double.NaN;

            This._NextLineAdvanceMovingPosition = null;
        }

        // Returns a normalized line range from TextView for a given position.
        // Note: In current contract, line range returned by TextView.GetLineRange() is not guaranteed to be normalized.
        // This helper does appropriate correction and returns a normalized line range.
        internal static TextSegment GetNormalizedLineRange(ITextView textView, ITextPointer position)
        {
            TextSegment lineRange = textView.GetLineRange(position);
            if (lineRange.IsNull)
            {
                if (!typeof(BlockUIContainer).IsAssignableFrom(position.ParentType))
                {
                    return lineRange;
                }

                ITextPointer lineStart = position.CreatePointer(LogicalDirection.Forward);
                lineStart.MoveToElementEdge(ElementEdge.AfterStart);
                ITextPointer lineEnd = position.CreatePointer(LogicalDirection.Backward);
                lineEnd.MoveToElementEdge(ElementEdge.BeforeEnd);
                lineRange = new TextSegment(lineStart, lineEnd);
                return lineRange;
            }

            // Normalize line range
            ITextRange textRange = new TextRange(lineRange.Start, lineRange.End);
            return new TextSegment(textRange.Start, textRange.End);
        }

        // Returns true if a textview is potentially paginated.
        internal static bool IsPaginated(ITextView textview)
        {
            return !(textview is TextBoxView);
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// </summary>
        private static void OnSelectAll(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock(true /* disableScroll */))
            {
                This.Selection.Select(This.TextContainer.Start, This.TextContainer.End);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        // ................................................................
        //
        // Editing Commands: Caret Navigation
        //
        // ................................................................

        /// <summary>
        /// MoveRightByCharacter command event handler.
        /// </summary>
        private static void OnMoveRightByCharacter(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Backward : LogicalDirection.Forward;
            MoveToCharacterLogicalDirection(This, movementDirection, /*extend:*/false);
        }

        /// <summary>
        /// MoveLeftByCharacter command event handler.
        /// </summary>
        private static void OnMoveLeftByCharacter(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Forward : LogicalDirection.Backward;
            MoveToCharacterLogicalDirection(This, movementDirection, /*extend:*/false);
        }


        /// <summary>
        /// </summary>
        private static void OnMoveRightByWord(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // Navigate word to the logical forward.
            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Backward : LogicalDirection.Forward;
            NavigateWordLogicalDirection(This, movementDirection);
        }

        /// <summary>
        /// </summary>
        private static void OnMoveLeftByWord(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // Navigate word to the logical backward.
            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Forward : LogicalDirection.Backward;
            NavigateWordLogicalDirection(This, movementDirection);
        }

        private static void OnMoveDownByLine(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // We need a non-dirty layout to walk lines.
            if (!This.Selection.End.ValidateLayout())
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty, collapse it.
                    // Collapsing must happen to selection END - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.

                    // When Selection.End is moving position we must adjust it
                    // for LineEnd condition - choose inner position within a line.
                    ITextPointer position = TextEditorSelection.GetEndInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                    TextEditorSelection._ClearSuggestedX(This); // So that when we will request suggestedX below it will take it from the new moving position
                }
                Invariant.Assert(This.Selection.IsEmpty);

                // When the caret is at RowEnd position, we start by moving it into the last cell of this row.
                AdjustCaretAtTableRowEnd(This);

                ITextPointer originalMovingPosition;
                double suggestedX = TextEditorSelection.GetSuggestedX(This, out originalMovingPosition);

                // Continue only if we have a moving position with valid layout
                if (originalMovingPosition == null)
                {
                    return;
                }

                // Extend the selection edge.
                double newSuggestedX;
                int linesMoved;
                ITextPointer newMovingPosition = This.TextView.GetPositionAtNextLine(This.Selection.MovingPosition, suggestedX, +1, out newSuggestedX, out linesMoved);
                Invariant.Assert(newMovingPosition != null);

                if (linesMoved != 0)
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                    // Move insertion point to next or previous line
                    This.Selection.SetCaretToPosition(newMovingPosition, newMovingPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }
                else
                {
                    if (TextPointerBase.IsInAnchoredBlock(originalMovingPosition))
                    {
                        // TextView treats AnchoredBlock elements as hard structural boundaries.
                        // As a result GetPositionAtNextLine() does not work from the first/last line within an AnchoredBlock.

                        // If line move wasn't successful because our moving position is at the end of an AnchoredBlock,
                        // move insertion point so that it crosses AnchoredBlock boundary.
                        // If there is no next position after the AnchoredBlock, move to current line end.

                        ITextPointer lineEndPosition = GetPositionAtLineEnd(originalMovingPosition);
                        ITextPointer nextPosition = lineEndPosition.GetNextInsertionPosition(LogicalDirection.Forward);
                        This.Selection.SetCaretToPosition(nextPosition != null ? nextPosition : lineEndPosition,
                            originalMovingPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                    }
                    else if (IsPaginated(This.TextView))
                    {
                        // If line move wasn't successful because it is not in the view, bring the next line into view.
                        This.TextView.BringLineIntoViewCompleted += new BringLineIntoViewCompletedEventHandler(HandleMoveByLineCompleted);
                        This.TextView.BringLineIntoViewAsync(newMovingPosition, newSuggestedX, +1, This);
                    }
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        private static void OnMoveUpByLine(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // We need a non-dirty layout to walk lines.
            if (!This.Selection.Start.ValidateLayout())
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty, collapse it.
                    // Collapsing must happen to selection START - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.
                    ITextPointer position = TextEditorSelection.GetStartInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                    TextEditorSelection._ClearSuggestedX(This); // So that when we will request suggestedX below it will take it from the new moving position
                }
                Invariant.Assert(This.Selection.IsEmpty);

                // When the caret is at RowEnd position, we start by moving it into the last cell of this row.
                AdjustCaretAtTableRowEnd(This);

                ITextPointer originalMovingPosition;
                double suggestedX = TextEditorSelection.GetSuggestedX(This, out originalMovingPosition);

                // Continue only if we have a moving position with valid layout
                if (originalMovingPosition == null)
                {
                    return;
                }

                // Extend the selection edge.
                double newSuggestedX;
                int linesMoved;
                ITextPointer newMovingPosition = This.TextView.GetPositionAtNextLine(This.Selection.MovingPosition, suggestedX, -1, out newSuggestedX, out linesMoved);
                Invariant.Assert(newMovingPosition != null);

                if (linesMoved != 0)
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                    // Move insertion point to next or previous line
                    //  position must be normalized not randomly here (bug)
                    This.Selection.SetCaretToPosition(newMovingPosition, newMovingPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }
                else
                {
                    if (TextPointerBase.IsInAnchoredBlock(originalMovingPosition))
                    {
                        // TextView treats AnchoredBlock elements as hard structural boundaries.
                        // As a result GetPositionAtNextLine() does not work from the first/last line within an AnchoredBlock.

                        // If line move wasn't successful because our moving position is at the end of an AnchoredBlock,
                        // move insertion point to a position before the AnchoredBlock boundary.
                        // If there is no previous position before the AnchoredBlock, move to current line start.

                        ITextPointer lineStartPosition = GetPositionAtLineStart(originalMovingPosition);
                        ITextPointer previousPosition = lineStartPosition.GetNextInsertionPosition(LogicalDirection.Backward);
                        This.Selection.SetCaretToPosition(previousPosition != null ? previousPosition : lineStartPosition,
                            originalMovingPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                    }
                    else if (IsPaginated(This.TextView))
                    {
                        // If line move wasn't successful because it is not in the view, bring the previous line into view.
                        This.TextView.BringLineIntoViewCompleted += new BringLineIntoViewCompletedEventHandler(HandleMoveByLineCompleted);
                        This.TextView.BringLineIntoViewAsync(newMovingPosition, newSuggestedX, -1, This);
                    }
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        private static void OnMoveDownByParagraph(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);

                // Move/extend selection in requested direction
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty and ends on a word boundary, collapse it to that boundary.
                    // Collapsing must happen to selection END - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.

                    // When Selection.End is moving position we must adjust it
                    // for LineEnd condition - choose inner position within a line.
                    ITextPointer position = TextEditorSelection.GetEndInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }

                ITextPointer movingPointer = This.Selection.MovingPosition.CreatePointer();
                ITextRange paragraphRange = new TextRange(movingPointer, movingPointer);
                paragraphRange.SelectParagraph(movingPointer);

                movingPointer.MoveToPosition(paragraphRange.End);
                if (movingPointer.MoveToNextInsertionPosition(LogicalDirection.Forward))
                {
                    // Next paragraph found. Set selection to its start
                    paragraphRange.SelectParagraph(movingPointer);
                    This.Selection.SetCaretToPosition(paragraphRange.Start, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }
                else
                {
                    // Next paragraph does not exist. Set selectionn to the end of current paragraph
                    This.Selection.SetCaretToPosition(paragraphRange.End, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }
            }
        }

        private static void OnMoveUpByParagraph(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);

                // Move/extend selection in requested direction
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty and ends on a word boundary, collapse it to that boundary.
                    // Collapsing must happen to selection START - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.
                    ITextPointer position = TextEditorSelection.GetStartInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }
                ITextPointer movingPointer = This.Selection.MovingPosition.CreatePointer();
                ITextRange paragraphRange = new TextRange(movingPointer, movingPointer);
                paragraphRange.SelectParagraph(movingPointer);

                if (This.Selection.Start.CompareTo(paragraphRange.Start) > 0)
                {
                    // We are in the middle of a paragraph. Move to its start
                    This.Selection.SetCaretToPosition(paragraphRange.Start, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }
                else
                {
                    movingPointer.MoveToPosition(paragraphRange.Start);
                    if (movingPointer.MoveToNextInsertionPosition(LogicalDirection.Backward))
                    {
                        // Previous paragraph found. Set selection to its start
                        paragraphRange.SelectParagraph(movingPointer);
                        This.Selection.SetCaretToPosition(paragraphRange.Start, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                    }
                }
            }
        }

        private static void OnMoveDownByPage(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // We need a non-dirty layout to walk pages.
            if (!This.Selection.End.ValidateLayout())
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty and ends on a word boundary, collapse it to that boundary.
                    // Collapsing must happen to selection END - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.

                    // When Selection.End is moving position we must adjust it
                    // for LineEnd condition - choose inner position within a line.
                    ITextPointer position = TextEditorSelection.GetEndInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }

                ITextPointer movingPosition;
                double suggestedX = TextEditorSelection.GetSuggestedX(This, out movingPosition);

                // Continue only if we have a moving position with valid layout
                if (movingPosition == null)
                {
                    return;
                }

                ITextPointer targetPosition;
                double newSuggestedX;
                double pageHeight = (double)This.UiScope.GetValue(TextEditor.PageHeightProperty);

                // Presence of page height property on TextEditor instructs us to use simple bottomless version of
                // pagination (TextBox/RichTextBox).
                // Otherwise, when page height = 0, we use TextView implementation of GetPositionAtNextPage().

                // Ideally, we should remove PageHeight property from TextEditor and
                // textview should implement GetPositionAtNextPage() for bottomless scrollviewer.
                if (pageHeight == 0)
                {
                    if (IsPaginated(This.TextView))
                    {
                        int pagesMoved;
                        Point newSuggestedOffset;

                        // Get suggested Y for moving position
                        double suggestedY = GetSuggestedYFromPosition(This, movingPosition);

                        targetPosition = This.TextView.GetPositionAtNextPage(movingPosition, new Point(GetViewportXOffset(This.TextView, suggestedX), suggestedY), +1, out newSuggestedOffset, out pagesMoved);
                        newSuggestedX = newSuggestedOffset.X;
                        Invariant.Assert(targetPosition != null);

                        if (pagesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                            // If shift key isn't down, collapse the range.
                            This.Selection.SetCaretToPosition(targetPosition, targetPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/false);
                        }
                        else if (IsPaginated(This.TextView))
                        {
                            // If page move wasn't successful because it is not in the view, bring the next page into view.
                            This.TextView.BringPageIntoViewCompleted += new BringPageIntoViewCompletedEventHandler(HandleMoveByPageCompleted);
                            This.TextView.BringPageIntoViewAsync(targetPosition, newSuggestedOffset, +1, This);
                        }
                    }
                }
                else
                {
                    // Calculate target position - at a specified distance from current movingPosition
                    Rect targetRect = This.TextView.GetRectangleFromTextPosition(movingPosition);
                    Point targetPoint = new Point(GetViewportXOffset(This.TextView, suggestedX), targetRect.Top + pageHeight);
                    targetPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);

                    if (targetPosition == null)
                    {
                        return;
                    }

                    // Check if the new position really moving forward;
                    // otherwise force it to the very end of container.
                    if (targetPosition.CompareTo(movingPosition) <= 0)
                    {
                        targetPosition = This.TextContainer.End;
                        TextEditorSelection._ClearSuggestedX(This);
                    }

                    // Fire a page up/down command on the renderScope, so any ScrollViewer will pick it up
                    ScrollBar.PageDownCommand.Execute(null, This.TextView.RenderScope);

                    // We need the page down to happen before the caret moves.  Force a layout update
                    // so the command queue gets processed.
                    This.TextView.RenderScope.UpdateLayout();

                    // If shift key isn't down, collapse the range.
                    This.Selection.SetCaretToPosition(targetPosition, targetPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/false);
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        private static void OnMoveUpByPage(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // We need a non-dirty layout to walk pages.
            if (!This.Selection.Start.ValidateLayout())
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                if (!This.Selection.IsEmpty)
                {
                    // If the selection is non-empty and ends on a word boundary, collapse it to that boundary.
                    // Collapsing must happen to selection START - not to its moving position.
                    // It is Word behavior for setting a cratet on moving down from nonempty selection.
                    ITextPointer position = TextEditorSelection.GetStartInner(This);

                    This.Selection.SetCaretToPosition(position, position.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }

                ITextPointer movingPosition;
                double suggestedX = TextEditorSelection.GetSuggestedX(This, out movingPosition);

                // Continue only if we have a moving position with valid layout
                if (movingPosition == null)
                {
                    return;
                }

                ITextPointer targetPosition;
                double newSuggestedX;
                double pageHeight = (double)This.UiScope.GetValue(TextEditor.PageHeightProperty);

                // Presence of page height property on TextEditor instructs us to use simple bottomless version of
                // pagination (TextBox/RichTextBox).
                // Otherwise, when page height = 0, we use TextView implementation of GetPositionAtNextPage().
                if (pageHeight == 0)
                {
                    if (IsPaginated(This.TextView))
                    {
                        int pagesMoved;
                        Point newSuggestedOffset;

                        // Get suggested Y for moving position
                        double suggestedY = GetSuggestedYFromPosition(This, movingPosition);

                        targetPosition = This.TextView.GetPositionAtNextPage(movingPosition, new Point(GetViewportXOffset(This.TextView, suggestedX), suggestedY), -1, out newSuggestedOffset, out pagesMoved);
                        newSuggestedX = newSuggestedOffset.X;
                        Invariant.Assert(targetPosition != null);

                        if (pagesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                            // If shift key isn't down, collapse the range.
                            This.Selection.SetCaretToPosition(targetPosition, targetPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/false);
                        }
                        else if (IsPaginated(This.TextView))
                        {
                            // If page move wasn't successful because it is not in the view, bring the next page into view.
                            This.TextView.BringPageIntoViewCompleted += new BringPageIntoViewCompletedEventHandler(HandleMoveByPageCompleted);
                            This.TextView.BringPageIntoViewAsync(targetPosition, newSuggestedOffset, -1, This);
                        }
                    }
                }
                else
                {
                    // Calculate target position - at a specified distance from current movingPosition
                    Rect targetRect = This.TextView.GetRectangleFromTextPosition(movingPosition);
                    Point targetPoint = new Point(GetViewportXOffset(This.TextView, suggestedX), targetRect.Bottom - pageHeight);
                    targetPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);

                    if (targetPosition == null)
                    {
                        return;
                    }

                    // Check if the new position really moving forward;
                    // otherwise force it to the very end of container.
                    if (targetPosition.CompareTo(movingPosition) >= 0)
                    {
                        targetPosition = This.TextContainer.Start;
                        TextEditorSelection._ClearSuggestedX(This);
                    }

                    // Fire a page up/down command on the renderScope, so any ScrollViewer will pick it up
                    ScrollBar.PageUpCommand.Execute(null, This.TextView.RenderScope);

                    // We need the page down to happen before the caret moves.  Force a layout update
                    // so the command queue gets processed.
                    This.TextView.RenderScope.UpdateLayout();

                    // If shift key isn't down, collapse the range.
                    This.Selection.SetCaretToPosition(targetPosition, targetPosition.LogicalDirection, /*allowStopAtLineEnd:*/
                                                      true, /*allowStopNearSpace:*/false);
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnMoveToLineStart(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // When getting start position, we need to take care of end-of-line case
            // and adjust start position according to its orientation.
            ITextPointer startPositionInner = TextEditorSelection.GetStartInner(This);

            // We need a non-dirty layout to walk pages.
            if (!startPositionInner.ValidateLayout())
            {
                return;
            }

            // Standard behavior, move to begin/end of line.
            TextSegment lineRange = TextEditorSelection.GetNormalizedLineRange(This.TextView, startPositionInner);
            if (lineRange.IsNull)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                // Note caret direction here: must be forward to keep caret on the same line
                //  Position must be normalized not randomly (bug)

                // Create caret position normalized forward - towards the very first character of the line
                ITextPointer caretPosition = lineRange.Start.GetFrozenPointer(LogicalDirection.Forward);

                // Set caret to beginning of a line
                This.Selection.SetCaretToPosition(caretPosition, LogicalDirection.Forward, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnMoveToLineEnd(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // When getting end position, we need to take care of end-of-line case
            // and adjust end position according to its orientation.
            ITextPointer endPositionInner = TextEditorSelection.GetEndInner(This);

            // We need a non-dirty layout to walk pages.
            if (!endPositionInner.ValidateLayout())
            {
                return;
            }

            TextSegment lineRange = TextEditorSelection.GetNormalizedLineRange(This.TextView, endPositionInner);
            if (lineRange.IsNull)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                // Note caret direction here: must be backward to keep caret on the same line when it is wrapped by flow.
                // Orientation must be Backward when the line is wrapped by flow, otherwise - normal Forward
                LogicalDirection orientation = TextPointerBase.IsNextToPlainLineBreak(lineRange.End, LogicalDirection.Backward) ? LogicalDirection.Forward : LogicalDirection.Backward;

                // Create caret position normalized the same way as orientation
                ITextPointer caretPosition = lineRange.End.GetFrozenPointer(orientation);

                // Set caret to the end of line
                This.Selection.SetCaretToPosition(caretPosition, orientation, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnMoveToDocumentStart(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                This.Selection.SetCaretToPosition(This.TextContainer.Start, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnMoveToDocumentEnd(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Orientation is standard - forward, because text cannot wrap by flow at this position
                This.Selection.SetCaretToPosition(This.TextContainer.End, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        // ................................................................
        //
        // Editing Commands: Selection Building
        //
        // ................................................................

        /// <summary>
        /// SelectRightByCharacter command event handler.
        /// </summary>
        private static void OnSelectRightByCharacter(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Backward : LogicalDirection.Forward;
            MoveToCharacterLogicalDirection(This, movementDirection, /*extend:*/true);
        }

        /// <summary>
        /// SelectLeftByCharacter command event handler.
        /// </summary>
        private static void OnSelectLeftByCharacter(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Forward : LogicalDirection.Backward;
            MoveToCharacterLogicalDirection(This, movementDirection, /*extend:*/true);
        }

        /// <summary>
        /// </summary>
        private static void OnSelectRightByWord(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // Extend word to the logical forward.
            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Backward : LogicalDirection.Forward;
            ExtendWordLogicalDirection(This, movementDirection);
        }

        /// <summary>
        /// </summary>
        private static void OnSelectLeftByWord(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // Extend word to the logical backward.
            LogicalDirection movementDirection = IsFlowDirectionRightToLeftThenTopToBottom(This) ? LogicalDirection.Forward : LogicalDirection.Backward;
            ExtendWordLogicalDirection(This, movementDirection);
        }

        private static void OnSelectDownByLine(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // We need a non-dirty layout to walk lines.
            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                if (This.Selection.ExtendToNextTableRow(LogicalDirection.Forward))
                {
                    // This is table selection case. Vertical extension.
                    // Table selection has been successfully extended in vertical direction.
                    // Nothing more to do.
                }
                else
                {
                    ITextPointer originalMovingPosition;
                    double suggestedX = TextEditorSelection.GetSuggestedX(This, out originalMovingPosition);

                    // Continue only if we have a moving position with valid layout
                    if (originalMovingPosition == null)
                    {
                        return;
                    }

                    if (This._NextLineAdvanceMovingPosition != null &&
                        This._IsNextLineAdvanceMovingPositionAtDocumentHead)
                    {
                        // Moving position is at the beginning of text container
                        // as a result of previous Shift+Up extension;
                        // so now we need to return to a positing within the current line
                        ExtendSelectionAndBringIntoView(This._NextLineAdvanceMovingPosition, This);
                        This._NextLineAdvanceMovingPosition = null;
                    }
                    else
                    {
                        // When the moving position is at RowEnd position, we start by moving it into the last cell of this row.
                        ITextPointer newMovingPosition = AdjustPositionAtTableRowEnd(originalMovingPosition);

                        // Find a position in the next line
                        double newSuggestedX;
                        int linesMoved;

                        newMovingPosition = This.TextView.GetPositionAtNextLine(newMovingPosition, suggestedX, +1, out newSuggestedX, out linesMoved);
                        Invariant.Assert(newMovingPosition != null);

                        if (linesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                            // Shift key is down - Extend the selection edge
                            AdjustMovingPositionForSelectDownByLine(This, newMovingPosition, originalMovingPosition, newSuggestedX);
                        }
                        else
                        {
                            if (TextPointerBase.IsInAnchoredBlock(originalMovingPosition))
                            {
                                // TextView treats AnchoredBlock elements as hard structural boundaries.
                                // As a result GetPositionAtNextLine() does not work from the first/last line within an AnchoredBlock.

                                // If line move wasn't successful because our moving position is at the end of an AnchoredBlock,
                                // expand selection so that it crosses AnchoredBlock boundary.
                                // If there is no next position after the AnchoredBlock, expand to current line end.

                                ITextPointer lineEndPosition = GetPositionAtLineEnd(originalMovingPosition);
                                ITextPointer nextPosition = lineEndPosition.GetNextInsertionPosition(LogicalDirection.Forward);

                                // Extend selection and bring new position into view if needed (for paginated viewers)
                                ExtendSelectionAndBringIntoView(nextPosition != null ? nextPosition : lineEndPosition, This);
                            }
                            else if (IsPaginated(This.TextView))
                            {
                                // If line move wasn't successful because it is not in the view, bring the next line into view.
                                This.TextView.BringLineIntoViewCompleted += new BringLineIntoViewCompletedEventHandler(HandleSelectByLineCompleted);
                                This.TextView.BringLineIntoViewAsync(newMovingPosition, newSuggestedX, +1, This);
                            }
                            else
                            {
                                // Remember where we were so that we can return if a line up follows.
                                if (This._NextLineAdvanceMovingPosition == null)
                                {
                                    This._NextLineAdvanceMovingPosition = originalMovingPosition;
                                    This._IsNextLineAdvanceMovingPositionAtDocumentHead = false;
                                }
                                // No more lines in this direction. Move to end of current line.
                                ExtendSelectionAndBringIntoView(GetPositionAtLineEnd(newMovingPosition), This);
                            }
                        }
                    }
                }
    
                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        // Helper for OnSelectDownByLine.  Updates the selection moving position
        // during select down by line.
        private static void AdjustMovingPositionForSelectDownByLine(TextEditor This, ITextPointer newMovingPosition, ITextPointer originalMovingPosition, double suggestedX)
        {
            int newComparedToOld = newMovingPosition.CompareTo(originalMovingPosition);

            // Note: we compare orientations of equal positions to handle a case
            // when original position was at the end of line (after its linebreak with backward orientation)
            // as a result of Shift+End selection; and the new position is in the beginning of the next line,
            // which is essentially the same position but oriented differently.
            // In such a case the new position is good enough to go there.
            // We certainly don't want to go to the end of the document in this case.
            if (newComparedToOld > 0 || newComparedToOld == 0 && newMovingPosition.LogicalDirection != originalMovingPosition.LogicalDirection)
            {
                // We have another line in a given direction; move to it

                // If the destination exactly preceeds a line break, expand to include
                // the line break if we haven't reached our desired suggestedX.
                if (TextPointerBase.IsNextToAnyBreak(newMovingPosition, LogicalDirection.Forward) ||
                    newMovingPosition.GetNextInsertionPosition(LogicalDirection.Forward) == null)
                {
                    double newPositionX = GetAbsoluteXOffset(This.TextView, newMovingPosition);
                    FlowDirection paragraphFlowDirection = GetScopingParagraphFlowDirection(newMovingPosition);
                    FlowDirection controlFlowDirection = This.UiScope.FlowDirection;

                    if ((paragraphFlowDirection == controlFlowDirection && newPositionX < suggestedX) ||
                        (paragraphFlowDirection != controlFlowDirection && newPositionX > suggestedX))
                    {
                        newMovingPosition = newMovingPosition.GetInsertionPosition(LogicalDirection.Forward);
                        newMovingPosition = newMovingPosition.GetNextInsertionPosition(LogicalDirection.Forward);

                        // If we're at the last Paragraph, move to document end to include
                        // the final paragraph break.
                        if (newMovingPosition == null)
                        {
                            newMovingPosition = originalMovingPosition.TextContainer.End;
                        }

                        newMovingPosition = newMovingPosition.GetFrozenPointer(LogicalDirection.Backward);
                    }
                }

                ExtendSelectionAndBringIntoView(newMovingPosition, This);
            }
            else
            {
                // Remember where we were so that we can return if a line up follows.
                if (This._NextLineAdvanceMovingPosition == null)
                {
                    This._NextLineAdvanceMovingPosition = originalMovingPosition;
                    This._IsNextLineAdvanceMovingPositionAtDocumentHead = false;
                }

                // No more lines in this direction. Move to end of current line.
                newMovingPosition = GetPositionAtLineEnd(originalMovingPosition);

                if (newMovingPosition.GetNextInsertionPosition(LogicalDirection.Forward) == null)
                {
                    // Move to the final implicit line at end-of-doc.
                    newMovingPosition = newMovingPosition.TextContainer.End;
                }

                ExtendSelectionAndBringIntoView(newMovingPosition, This);
            }
        }

        private static void OnSelectUpByLine(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            // We need a non-dirty layout to walk lines.
            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                if (This.Selection.ExtendToNextTableRow(LogicalDirection.Backward))
                {
                    // This is table selection case. Vertical extension.
                    // Table selection has been successfully extended in vertical direction.
                    // Nothing more to do.
                }
                else
                {
                    ITextPointer originalMovingPosition;
                    double suggestedX = TextEditorSelection.GetSuggestedX(This, out originalMovingPosition);

                    // Continue only if we have a moving position with valid layout
                    if (originalMovingPosition == null)
                    {
                        return;
                    }

                    if (This._NextLineAdvanceMovingPosition != null &&
                        !This._IsNextLineAdvanceMovingPositionAtDocumentHead)
                    {
                        // Moving position is at the end of text container
                        // as a result of previous Shift+Down extension;
                        // so now we need to return to a positing within the current line
                        ExtendSelectionAndBringIntoView(This._NextLineAdvanceMovingPosition, This);
                        This._NextLineAdvanceMovingPosition = null;
                    }
                    else
                    {
                        // When the moving position is at RowEnd position, we start by moving it into the last cell of this row.
                        ITextPointer newMovingPosition = AdjustPositionAtTableRowEnd(originalMovingPosition);

                        // Extend the selection edge.
                        double newSuggestedX;
                        int linesMoved;

                        newMovingPosition = This.TextView.GetPositionAtNextLine(newMovingPosition, suggestedX, -1, out newSuggestedX, out linesMoved);
                        Invariant.Assert(newMovingPosition != null);

                        if (linesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);

                            int newComparedToOld = newMovingPosition.CompareTo(originalMovingPosition);

                            // Shift key is down - Extend the selection edge
                            if (newComparedToOld < 0 || newComparedToOld == 0 && newMovingPosition.LogicalDirection != originalMovingPosition.LogicalDirection)
                            {
                                // Note: we compare orientations of equal positions to handle a case
                                // when original position was at the end of line (after its linebreak with backward orientation)
                                // as a result of Shift+End selection; and the new position is in the beginning of the next line,
                                // which is essentially the same position but oriented differently.
                                // In such a case the new position is good enough to go there.
                                // We certainly don't want to go to the end of the document in this case.

                                // We have another line in a given direction; move to it
                                ExtendSelectionAndBringIntoView(newMovingPosition, This);
                            }
                            else
                            {
                                // No more lines in this direction. Move to current line start.
                                ExtendSelectionAndBringIntoView(GetPositionAtLineStart(originalMovingPosition), This);
                            }
                        }
                        else
                        {
                            if (TextPointerBase.IsInAnchoredBlock(originalMovingPosition))
                            {
                                // TextView treats AnchoredBlock elements as hard structural boundaries.
                                // As a result GetPositionAtNextLine() does not work from the first/last line within an AnchoredBlock.

                                // If line move wasn't successful because our moving position is at the start of an AnchoredBlock,
                                // expand selection so that it crosses AnchoredBlock boundary.
                                // If there is no previous position before the AnchoredBlock, expand to current line start.

                                ITextPointer lineStartPosition = GetPositionAtLineStart(originalMovingPosition);
                                ITextPointer previousPosition = lineStartPosition.GetNextInsertionPosition(LogicalDirection.Backward);

                                // Extend selection and bring new position into view if needed (for paginated viewers)
                                ExtendSelectionAndBringIntoView(previousPosition != null ? previousPosition : lineStartPosition, This);
                            }
                            else if (IsPaginated(This.TextView))
                            {
                                // If line move wasn't successful because it is not in the view, bring the previous line into view.
                                This.TextView.BringLineIntoViewCompleted += new BringLineIntoViewCompletedEventHandler(HandleSelectByLineCompleted);
                                This.TextView.BringLineIntoViewAsync(newMovingPosition, newSuggestedX, -1, This);
                            }
                            else
                            {
                                // Remember where we were so that we can return if a line down follows.
                                if (This._NextLineAdvanceMovingPosition == null)
                                {
                                    This._NextLineAdvanceMovingPosition = originalMovingPosition;
                                    This._IsNextLineAdvanceMovingPositionAtDocumentHead = true;
                                }
                                // No more lines in this direction. Move to start of current line.
                                ExtendSelectionAndBringIntoView(GetPositionAtLineStart(newMovingPosition), This);
                            }
                        }
                    }
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        private static void OnSelectDownByParagraph(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);

                ITextPointer movingPointer = This.Selection.MovingPosition.CreatePointer();
                ITextRange paragraphRange = new TextRange(movingPointer, movingPointer);
                paragraphRange.SelectParagraph(movingPointer);

                movingPointer.MoveToPosition(paragraphRange.End);
                if (movingPointer.MoveToNextInsertionPosition(LogicalDirection.Forward))
                {
                    // Next paragraph found. Set selection to its start
                    paragraphRange.SelectParagraph(movingPointer);
                    ExtendSelectionAndBringIntoView(paragraphRange.Start, This);
                }
                else
                {
                    // Next paragraph does not exist. Set selectionn to the end of current paragraph
                    ExtendSelectionAndBringIntoView(paragraphRange.End, This);
                }
            }
        }

        private static void OnSelectUpByParagraph(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);

                ITextPointer movingPointer = This.Selection.MovingPosition.CreatePointer();
                ITextRange paragraphRange = new TextRange(movingPointer, movingPointer);
                paragraphRange.SelectParagraph(movingPointer);

                if (movingPointer.CompareTo(paragraphRange.Start) > 0)
                {
                    // We are in the middle of a paragraph. Move to its start
                    ExtendSelectionAndBringIntoView(paragraphRange.Start, This);
                }
                else
                {
                    movingPointer.MoveToPosition(paragraphRange.Start);
                    if (movingPointer.MoveToNextInsertionPosition(LogicalDirection.Backward))
                    {
                        // Previous paragraph found. Set selection to its start
                        paragraphRange.SelectParagraph(movingPointer);
                        ExtendSelectionAndBringIntoView(paragraphRange.Start, This);
                    }
                }
            }
        }

        private static void OnSelectDownByPage(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            ITextPointer movingPosition;
            double suggestedX = TextEditorSelection.GetSuggestedX(This, out movingPosition);

            // Continue only if we have a moving position with valid layout.
            if (movingPosition == null)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer targetPosition;
                double newSuggestedX;
                double pageHeight = (double)This.UiScope.GetValue(TextEditor.PageHeightProperty);

                // Presence of page height property on TextEditor instructs us to use simple bottomless version of
                // pagination (TextBox/RichTextBox).
                // Otherwise, when page height = 0, we use TextView implementation of GetPositionAtNextPage().
                if (pageHeight == 0)
                {
                    if (IsPaginated(This.TextView))
                    {
                        int pagesMoved;
                        Point newSuggestedOffset;

                        // Get suggested Y for moving position
                        double suggestedY = GetSuggestedYFromPosition(This, movingPosition);

                        targetPosition = This.TextView.GetPositionAtNextPage(movingPosition, new Point(GetViewportXOffset(This.TextView, suggestedX), suggestedY), +1, out newSuggestedOffset, out pagesMoved);
                        newSuggestedX = newSuggestedOffset.X;
                        Invariant.Assert(targetPosition != null);

                        if (pagesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);
                            ExtendSelectionAndBringIntoView(targetPosition, This);
                        }
                        else if (IsPaginated(This.TextView))
                        {
                            // If page move wasn't successful because it is not in the view, bring the next page into view.
                            This.TextView.BringPageIntoViewCompleted += new BringPageIntoViewCompletedEventHandler(HandleSelectByPageCompleted);
                            This.TextView.BringPageIntoViewAsync(targetPosition, newSuggestedOffset, +1, This);
                        }
                        else
                        {
                            // No more pages in this direction. Move to container end.
                            ExtendSelectionAndBringIntoView(targetPosition.TextContainer.End, This);
                        }
                    }
                }
                else
                {
                    // Calculate target position - at a specified distance from current movingPosition
                    Rect targetRect = This.TextView.GetRectangleFromTextPosition(movingPosition);
                    Point targetPoint = new Point(GetViewportXOffset(This.TextView, suggestedX), targetRect.Top + pageHeight);
                    targetPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);

                    if (targetPosition == null)
                    {
                        return;
                    }

                    // Check if the new position really moving forward;
                    // otherwise force it to the very end of container.
                    if (targetPosition.CompareTo(movingPosition) <= 0)
                    {
                        targetPosition = This.TextContainer.End;
                    }

                    // Extend the selection edge.
                    ExtendSelectionAndBringIntoView(targetPosition, This);

                    // Fire a page up/down command on the renderScope, so any ScrollViewer will pick it up
                    ScrollBar.PageDownCommand.Execute(null, This.TextView.RenderScope);
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        private static void OnSelectUpByPage(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            ITextPointer movingPosition;
            double suggestedX = TextEditorSelection.GetSuggestedX(This, out movingPosition);

            // Continue only if we have a moving position with valid layout.
            if (movingPosition == null)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer targetPosition;
                double newSuggestedX;
                double pageHeight = (double)This.UiScope.GetValue(TextEditor.PageHeightProperty);

                // Presence of page height property on TextEditor instructs us to use simple bottomless version of
                // pagination (TextBox/RichTextBox).
                // Otherwise, when page height = 0, we use TextView implementation of GetPositionAtNextPage().
                if (pageHeight == 0)
                {
                    if (IsPaginated(This.TextView))
                    {
                        int pagesMoved;
                        Point newSuggestedOffset;

                        // Get suggested Y for moving position
                        double suggestedY = GetSuggestedYFromPosition(This, movingPosition);

                        targetPosition = This.TextView.GetPositionAtNextPage(movingPosition, new Point(GetViewportXOffset(This.TextView, suggestedX), suggestedY), -1, out newSuggestedOffset, out pagesMoved);
                        newSuggestedX = newSuggestedOffset.X;
                        Invariant.Assert(targetPosition != null);

                        if (pagesMoved != 0)
                        {
                            // Update suggestedX
                            TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, newSuggestedX);
                            ExtendSelectionAndBringIntoView(targetPosition, This);
                        }
                        else if (IsPaginated(This.TextView))
                        {
                            // If page move wasn't successful because it is not in the view, bring the next page into view.
                            This.TextView.BringPageIntoViewCompleted += new BringPageIntoViewCompletedEventHandler(HandleSelectByPageCompleted);
                            This.TextView.BringPageIntoViewAsync(targetPosition, newSuggestedOffset, -1, This);
                        }
                        else
                        {
                            // No more pages in this direction. Move to container start.
                            ExtendSelectionAndBringIntoView(targetPosition.TextContainer.Start, This);
                        }
                    }
                }
                else
                {
                    // Calculate target position - at a specified distance from current movingPosition
                    Rect targetRect = This.TextView.GetRectangleFromTextPosition(movingPosition);
                    Point targetPoint = new Point(GetViewportXOffset(This.TextView, suggestedX), targetRect.Bottom - pageHeight);
                    targetPosition = This.TextView.GetTextPositionFromPoint(targetPoint, /*snapToText:*/true);

                    if (targetPosition == null)
                    {
                        return;
                    }

                    // Check if the new position really moving forward;
                    // otherwise force it to the very end of container.
                    if (targetPosition.CompareTo(movingPosition) >= 0)
                    {
                        targetPosition = This.TextContainer.Start;
                    }

                    // Extend the selection edge.
                    ExtendSelectionAndBringIntoView(targetPosition, This);

                    // Fire a page up/down command on the renderScope, so any ScrollViewer will pick it up
                    ScrollBar.PageUpCommand.Execute(null, This.TextView.RenderScope);
                }

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnSelectToLineStart(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // When getting moving position, we need to take care of end-of-line case
            // and adjust moving position according to its orientation.
            ITextPointer movingPositionInner = TextEditorSelection.GetMovingPositionInner(This);

            // We need a non-dirty layout to walk pages.
            if (!movingPositionInner.ValidateLayout())
            {
                return;
            }

            TextSegment lineRange = TextEditorSelection.GetNormalizedLineRange(This.TextView, movingPositionInner);
            if (lineRange.IsNull)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                // Extend the selection from the active end (oriented forward)
                ExtendSelectionAndBringIntoView(lineRange.Start.CreatePointer(LogicalDirection.Forward), This);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnSelectToLineEnd(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // When getting moving position, we need to take care of end-of-line case
            // and adjust moving position according to its orientation.
            ITextPointer movingPositionInner = TextEditorSelection.GetMovingPositionInner(This);

            // We need a non-dirty layout to walk pages.
            if (!movingPositionInner.ValidateLayout())
            {
                return;
            }

            TextSegment lineRange = TextEditorSelection.GetNormalizedLineRange(This.TextView, movingPositionInner);
            if (lineRange.IsNull)
            {
                return;
            }

            // The selection end is on the other side of a line break, don't move it.
            if (lineRange.End.CompareTo(This.Selection.End) < 0)
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer destination = lineRange.End;

                if (TextPointerBase.IsNextToPlainLineBreak(destination, LogicalDirection.Forward) ||
                    TextPointerBase.IsNextToRichLineBreak(destination, LogicalDirection.Forward))
                {
                    // Extend to include any following line break if the anchor position lies at the start of a line.

                    if (This.Selection.AnchorPosition.ValidateLayout())
                    {
                        TextSegment anchorLineRange = TextEditorSelection.GetNormalizedLineRange(This.TextView, This.Selection.AnchorPosition);
                        if (!lineRange.IsNull && anchorLineRange.Start.CompareTo(This.Selection.AnchorPosition) == 0)
                        {
                            destination = destination.GetNextInsertionPosition(LogicalDirection.Forward);
                        }
                    }
                }
                else if (TextPointerBase.IsNextToParagraphBreak(destination, LogicalDirection.Forward) &&
                         TextPointerBase.IsNextToParagraphBreak(This.Selection.AnchorPosition, LogicalDirection.Backward))
                {
                    // Extend to include any following Paragraph break if the anchor position lies at the start of a Paragraph.

                    ITextPointer newDestination = destination.GetNextInsertionPosition(LogicalDirection.Forward);

                    if (newDestination == null)
                    {
                        // We are at the end of container - extend to include position after last paragraph
                        destination = destination.TextContainer.End;
                    }
                    else
                    {
                        destination = newDestination;
                    }
                }

                // Set orientation towards line content
                destination = destination.GetFrozenPointer(LogicalDirection.Backward);

                // Extend the selection from the active end.
                ExtendSelectionAndBringIntoView(destination, This);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnSelectToDocumentStart(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                ExtendSelectionAndBringIntoView(This.TextContainer.Start, This);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// </summary>
        private static void OnSelectToDocumentEnd(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                ExtendSelectionAndBringIntoView(This.TextContainer.End, This);

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(This);

                // Clear springload formatting
                ClearSpringloadFormatting(This);
            }
        }

        /// <summary>
        /// Handler for ITextView.BringLineIntoViewCompleted event.
        /// </summary>
        private static void HandleMoveByLineCompleted(object sender, BringLineIntoViewCompletedEventArgs e)
        {
            Invariant.Assert(sender is ITextView);
            ((ITextView)sender).BringLineIntoViewCompleted -= new BringLineIntoViewCompletedEventHandler(HandleMoveByLineCompleted);

            if (e != null && !e.Cancelled && e.Error == null)
            {
                TextEditor This = e.UserState as TextEditor;

                if (This == null || !This._IsEnabled)
                {
                    return;
                }

                TextEditorTyping._FlushPendingInputItems(This);

                using (This.Selection.DeclareChangeBlock())
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, e.NewSuggestedX);

                    // Move insertion point to next or previous line
                    This.Selection.SetCaretToPosition(e.NewPosition, e.NewPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }
            }
        }

        /// <summary>
        /// Handler for ITextView.BringPageIntoViewCompleted event.
        /// </summary>
        private static void HandleMoveByPageCompleted(object sender, BringPageIntoViewCompletedEventArgs e)
        {
            Invariant.Assert(sender is ITextView);
            ((ITextView)sender).BringPageIntoViewCompleted -= new BringPageIntoViewCompletedEventHandler(HandleMoveByPageCompleted);

            if (e != null && !e.Cancelled && e.Error == null)
            {
                TextEditor This = e.UserState as TextEditor;

                if (This == null || !This._IsEnabled)
                {
                    return;
                }

                TextEditorTyping._FlushPendingInputItems(This);

                using (This.Selection.DeclareChangeBlock())
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, e.NewSuggestedOffset.X);

                    // Move insertion point to next or previous page
                    This.Selection.SetCaretToPosition(e.NewPosition, e.NewPosition.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }
            }
        }

        /// <summary>
        /// Handler for ITextView.BringLineIntoViewCompleted event.
        /// </summary>
        private static void HandleSelectByLineCompleted(object sender, BringLineIntoViewCompletedEventArgs e)
        {
            TextEditor This;

            Invariant.Assert(sender is ITextView);
            ((ITextView)sender).BringLineIntoViewCompleted -= new BringLineIntoViewCompletedEventHandler(HandleSelectByLineCompleted);

            if (e != null && !e.Cancelled && e.Error == null)
            {
                This = e.UserState as TextEditor;

                if (This == null || !This._IsEnabled)
                {
                    return;
                }

                TextEditorTyping._FlushPendingInputItems(This);

                using (This.Selection.DeclareChangeBlock())
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, e.NewSuggestedX);
                    int newComparedToOld = e.NewPosition.CompareTo(e.Position);

                    if (e.Count < 0) // Moving up if count < 0, moving down otherwise
                    {
                        // Shift key is down - Extend the selection edge
                        if (newComparedToOld < 0 || newComparedToOld == 0 && e.NewPosition.LogicalDirection != e.Position.LogicalDirection)
                        {
                            // Note: we compare orientations of equal positions to handle a case
                            // when original position was at the end of line (after its linebreak with backward orientation)
                            // as a result of Shift+End selection; and the new position is in the beginning of the next line,
                            // which is essentially the same position but oriented differently.
                            // In such a case the new position is good enough to go there.
                            // We certainly don't want to go to the end of the document in this case.

                            // We have another line in a given direction; move to it
                            ExtendSelectionAndBringIntoView(e.NewPosition, This);
                        }
                        else
                        {
                            // Remember where we were so that we can return if a line down follows.
                            if (This._NextLineAdvanceMovingPosition == null)
                            {
                                This._NextLineAdvanceMovingPosition = e.Position;
                                This._IsNextLineAdvanceMovingPositionAtDocumentHead = true;
                            }

                            // No more lines in this direction. Move to start of current line.
                            ExtendSelectionAndBringIntoView(GetPositionAtLineStart(e.NewPosition), This);
                        }
                    }
                    else
                    {
                        AdjustMovingPositionForSelectDownByLine(This, e.NewPosition, e.Position, e.NewSuggestedX);
                    }
                }
            }
        }

        /// <summary>
        /// Handler for ITextView.BringPageIntoViewCompleted event.
        /// </summary>
        private static void HandleSelectByPageCompleted(object sender, BringPageIntoViewCompletedEventArgs e)
        {
            TextEditor This;

            Invariant.Assert(sender is ITextView);
            ((ITextView)sender).BringPageIntoViewCompleted -= new BringPageIntoViewCompletedEventHandler(HandleSelectByPageCompleted);

            if (e != null && !e.Cancelled && e.Error == null)
            {
                This = e.UserState as TextEditor;

                if (This == null || !This._IsEnabled)
                {
                    return;
                }

                TextEditorTyping._FlushPendingInputItems(This);

                using (This.Selection.DeclareChangeBlock())
                {
                    // Update suggestedX
                    TextEditorSelection.UpdateSuggestedXOnColumnOrPageBoundary(This, e.NewSuggestedOffset.X);
                    int newComparedToOld = e.NewPosition.CompareTo(e.Position);

                    if (e.Count < 0) // Moving up if count < 0, moving down otherwise
                    {
                        // Shift key is down - Extend the selection edge
                        if (newComparedToOld < 0)
                        {
                            // We have another page in a given direction; move to it
                            ExtendSelectionAndBringIntoView(e.NewPosition, This);
                        }
                        else
                        {
                            // No more pages in this direction. Move to container start.
                            ExtendSelectionAndBringIntoView(e.NewPosition.TextContainer.Start, This);
                        }
                    }
                    else
                    {
                        // Shift key is down - Extend the selection edge
                        if (newComparedToOld > 0)
                        {
                            // We have another page in a given direction; move to it
                            ExtendSelectionAndBringIntoView(e.NewPosition, This);
                        }
                        else
                        {
                            // No more pages in this direction. Move to container end.
                            ExtendSelectionAndBringIntoView(e.NewPosition.TextContainer.End, This);
                        }
                    }
                }
            }
        }

        // ----------------------------------------------------------
        //
        // Misceleneous Commands
        //
        // ----------------------------------------------------------

        #region Misceleneous Commands

        /// <summary>
        /// Keyboard selection commands QueryStatus handler
        /// </summary>
        private static void OnQueryStatusKeyboardSelection(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled)
            {
                return;
            }

            args.CanExecute = true;
        }

        /// <summary>
        /// Caret navigation commands QueryStatus handler
        /// </summary>
        private static void OnQueryStatusCaretNavigation(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled)
            {
                return;
            }

            // We want to disable caret navigation for readonly content, when caret is not visible.
            if (This.IsReadOnly && !This.IsReadOnlyCaretVisible)
            {
                return;
            }

            args.CanExecute = true;
        }

        /// <summary>
        /// Placeholder for commands which are not yet implemented
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void OnNYICommand(object source, ExecutedRoutedEventArgs args)
        {
        }

        #endregion Misceleneous Commands

        // ----------------------------------------------------------
        //
        // Misceleneous Private Methods
        //
        // ----------------------------------------------------------

        #region Misceleneous Private Methods

        // Helper for clearing springload formatting
        private static void ClearSpringloadFormatting(TextEditor This)
        {
            if (This.Selection is TextSelection)
            {
                ((TextSelection)This.Selection).ClearSpringloadFormatting();
            }
        }

        /// <summary>
        /// Return true if the flow direction is RightToLeft.
        /// </summary>
        private static bool IsFlowDirectionRightToLeftThenTopToBottom(TextEditor textEditor)
        {
            Invariant.Assert(textEditor != null);

            ITextPointer position = textEditor.Selection.MovingPosition.CreatePointer();

            // Skip any scoping formatting elements.
            while (TextSchema.IsFormattingType(position.ParentType))
            {
                position.MoveToElementEdge(ElementEdge.AfterEnd);
            }

            FlowDirection flowDirection = (FlowDirection)position.GetValue(FlowDocument.FlowDirectionProperty);

            return (flowDirection == FlowDirection.RightToLeft);
        }

        /// <summary>
        /// Move or extend to character according to the logical direction.
        /// </summary>
        private static void MoveToCharacterLogicalDirection(TextEditor textEditor, LogicalDirection direction, bool extend)
        {
            Invariant.Assert(textEditor != null);

            TextEditorTyping._FlushPendingInputItems(textEditor);

            // Move selection in requested direction
            using (textEditor.Selection.DeclareChangeBlock())
            {
                if (extend)
                {
                    // Extend the selection edge. Shift key should be pressed in this case.
                    textEditor.Selection.ExtendToNextInsertionPosition(direction);

                    // Bring selection moving position into view
                    BringIntoView(textEditor.Selection.MovingPosition, textEditor);
                }
                else
                {
                    // Note that when the selection is non-empty we just collapse it, i.e. use one of its ends
                    ITextPointer movingEnd = (direction == LogicalDirection.Forward ? textEditor.Selection.End : textEditor.Selection.Start);

                    if (textEditor.Selection.IsEmpty)
                    {
                        movingEnd = movingEnd.GetNextInsertionPosition(direction);
                    }

                    if (movingEnd != null)
                    {
                        // Identify an orientation toward content as a character just passed by this move
                        LogicalDirection contentDirection = direction == LogicalDirection.Forward ? LogicalDirection.Backward : LogicalDirection.Forward;

                        // Set caret next to a text character just passed
                        movingEnd = movingEnd.GetInsertionPosition(contentDirection);

                        // By disallowing to stop near spaces we suppress our "next-to-space" formatting heuristics
                        // which would consider space character as formatting insignificant.
                        // This means that when we pass a space by keyboard we will respect its formatting,
                        // while when we click next to a space we always ignore space's formatting
                        // and prefer neighboring non-space character formatting instead.
                        textEditor.Selection.SetCaretToPosition(movingEnd, contentDirection, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                    }
                }

                // Remember to scroll the text if we're outside the viewport.
                textEditor.Selection.OnCaretNavigation();

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(textEditor);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(textEditor);

                // Clear springload formatting
                ClearSpringloadFormatting(textEditor);
            }
        }

        /// <summary>
        /// Navigate word according to the logical direction.
        /// </summary>
        private static void NavigateWordLogicalDirection(TextEditor textEditor, LogicalDirection direction)
        {
            Invariant.Assert(textEditor != null);

            TextEditorTyping._FlushPendingInputItems(textEditor);

            using (textEditor.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(textEditor);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(textEditor);

                // Clear springload formatting
                ClearSpringloadFormatting(textEditor);

                if (direction == LogicalDirection.Forward)
                {
                    // Move/extend selection in requested direction
                    if (!textEditor.Selection.IsEmpty && TextPointerBase.IsAtWordBoundary(textEditor.Selection.End, LogicalDirection.Forward))
                    {
                        // If the selection is non-empty and ends on a word boundary, collapse it to that boundary.
                        textEditor.Selection.SetCaretToPosition(textEditor.Selection.End, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                        // Note that we are using a "smart" version of SetCaretToPosition
                        // to choose caret orientation on formattinng switches appropriately.
                    }
                    else
                    {
                        ITextPointer wordBoundary = textEditor.Selection.End.CreatePointer();
                        TextPointerBase.MoveToNextWordBoundary(wordBoundary, LogicalDirection.Forward);

                        textEditor.Selection.SetCaretToPosition(wordBoundary, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                        // Note that we are using a "smart" version of SetCaretToPosition
                        // to choose caret orientation on formattinng switches appropriately.
                    }
                }
                else
                {
                    // Move/extend selection in requested direction
                    if (!textEditor.Selection.IsEmpty && TextPointerBase.IsAtWordBoundary(textEditor.Selection.Start, LogicalDirection.Forward))
                    {
                        // If the selection is non-empty and starts at word boundary, collapse it to that boundary.
                        textEditor.Selection.SetCaretToPosition(textEditor.Selection.Start, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                        // Note that we are using a "smart" version of SetCaretToPosition
                        // to choose caret orientation on formattinng switches appropriately.
                    }
                    else
                    {
                        ITextPointer wordBoundary = textEditor.Selection.Start.CreatePointer();
                        TextPointerBase.MoveToNextWordBoundary(wordBoundary, LogicalDirection.Backward);

                        textEditor.Selection.SetCaretToPosition(wordBoundary, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                        // Note that we are using a "smart" version of SetCaretToPosition
                        // to choose caret orientation on formattinng switches appropriately.
                    }
                }

                // Remember to scroll the text if we're outside the viewport.
                textEditor.Selection.OnCaretNavigation();

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(textEditor);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(textEditor);

                // Clear springload formatting
                ClearSpringloadFormatting(textEditor);
            }
        }

        /// <summary>
        /// Extend word according to the logical direction.
        /// </summary>
        private static void ExtendWordLogicalDirection(TextEditor textEditor, LogicalDirection direction)
        {
            Invariant.Assert(textEditor != null);

            TextEditorTyping._FlushPendingInputItems(textEditor);

            using (textEditor.Selection.DeclareChangeBlock())
            {
                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(textEditor);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(textEditor);

                // Clear springload formatting
                ClearSpringloadFormatting(textEditor);

                // Move/extend selection in requested direction
                ITextPointer wordBoundary = textEditor.Selection.MovingPosition.CreatePointer();
                TextPointerBase.MoveToNextWordBoundary(wordBoundary, direction);

                wordBoundary.SetLogicalDirection(direction == LogicalDirection.Forward ? LogicalDirection.Backward : LogicalDirection.Forward);

                // Extend selection and bring new position into view if needed (for paginated viewers)
                ExtendSelectionAndBringIntoView(wordBoundary, textEditor);

                // Remember to scroll the text if we're outside the viewport.
                textEditor.Selection.OnCaretNavigation();

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(textEditor);

                // Discard typing undo unit merging
                TextEditorTyping._BreakTypingSequence(textEditor);

                // Clear springload formatting
                ClearSpringloadFormatting(textEditor);
            }
        }

        /// <summary>
        /// Returns a moving position and the suggestedX value for it.
        /// </summary>
        /// <param name="This">TextEditor</param>
        /// <param name="innerMovingPosition">Inner moving position if it has valid layout, otherwise null.</param>
        /// <returns>Returns suggestedX when moving position has valid layout, Double.NaN otherwise.</returns>
        private static Double GetSuggestedX(TextEditor This, out ITextPointer innerMovingPosition)
        {
            // When getting moving position, we need to take care of end-of-line case
            // and adjust moving position according to its orientation.
            innerMovingPosition = TextEditorSelection.GetMovingPositionInner(This);

            // We need a non-dirty layout to walk pages.
            if (!innerMovingPosition.ValidateLayout())
            {
                innerMovingPosition = null;
                return Double.NaN; // This value is not supposed to be used by a caller.
            }

            if (Double.IsNaN(This._suggestedX))
            {
                This._suggestedX = GetAbsoluteXOffset(This.TextView, innerMovingPosition);

                // If the original moving position is on the other side of a line break,
                // add in the pixel width of the line break visualization.
                // Note this logic implicitly only modifies suggested x when the
                // selection is non-empty.
                if (This.Selection.MovingPosition.CompareTo(innerMovingPosition) > 0)
                {
                    double breakWidth = (double)innerMovingPosition.GetValue(TextElement.FontSizeProperty) * CaretElement.c_endOfParaMagicMultiplier;

                    FlowDirection paragraphFlowDirection = GetScopingParagraphFlowDirection(innerMovingPosition);
                    FlowDirection controlFlowDirection = This.UiScope.FlowDirection;

                    if (paragraphFlowDirection != controlFlowDirection)
                    {
                        // Adjust for X-axis flip on Paragraphs with non-default flow direction.
                        breakWidth = -breakWidth;
                    }

                    This._suggestedX += breakWidth; 
                }
            }

            return This._suggestedX;
        }

        /// <summary>
        /// Returns a suggested Y-value for a given position.
        /// </summary>
        /// <param name="This">TextEditor</param>
        /// <param name="position">Position for which suggested Y is needed</param>
        /// <returns>If position is null, returns Double.NaN. This method will not find the moving position. It is meant to be
        /// used after the moving position has been calculated with GetSuggestedX</returns>
        private static Double GetSuggestedYFromPosition(TextEditor This, ITextPointer position)
        {
            double suggestedY = Double.NaN;
            if (position != null)
            {
                suggestedY = This.TextView.GetRectangleFromTextPosition(position).Y;
            }

            return suggestedY;
        }

        /// <summary>
        /// Update suggestedX value if it has changed due to selection moving position crossing a page or column boundary.
        /// </summary>
        /// <param name="This">TextEditor</param>
        /// <param name="newSuggestedX">New suggestedX value</param>
        private static void UpdateSuggestedXOnColumnOrPageBoundary(TextEditor This, double newSuggestedX)
        {
            if (This._suggestedX != newSuggestedX)
            {
                This._suggestedX = newSuggestedX;
            }
        }

        /// <summary>
        /// Returns a position belonging to currently selected line.
        /// On a line boundary it depends on the movingPosition orientation.
        /// When the position is at the end of Selection and oriented backward
        /// we consider it belonging to a previous line.
        /// When it is oriented forward in the same location,
        /// it belongs to the next line.
        /// This method is supposed to perform appropriate correction -
        /// returns a position which is inside (inner) of the desired visual line.
        /// </summary>
        private static ITextPointer GetMovingPositionInner(TextEditor This)
        {
            ITextPointer movingPosition = This.Selection.MovingPosition;

            if (!(movingPosition is DocumentSequenceTextPointer || movingPosition is FixedTextPointer) &&
                movingPosition.LogicalDirection == LogicalDirection.Backward &&
                This.Selection.Start.CompareTo(movingPosition) < 0 &&
                TextPointerBase.IsNextToAnyBreak(movingPosition, LogicalDirection.Backward))
            {
                movingPosition = movingPosition.GetNextInsertionPosition(LogicalDirection.Backward);

                // When the end of selection was after the linebreak of empty line in plainn text
                // we need to change its orientation to not put it at the end of preceding line.
                if (TextPointerBase.IsNextToPlainLineBreak(movingPosition, LogicalDirection.Backward))
                {
                    movingPosition = movingPosition.GetFrozenPointer(LogicalDirection.Forward);
                }
            }
            else if (TextPointerBase.IsAfterLastParagraph(movingPosition))
            {
                movingPosition = movingPosition.GetInsertionPosition(movingPosition.LogicalDirection);
            }

            return movingPosition;
        }

        // Returns a position in the beginning of a selection which
        // belongs to visually selected line.
        // Its is Start position of a selection, only its orientation
        // must be Forward when selection is non-empty.
        private static ITextPointer GetStartInner(TextEditor This)
        {
            return This.Selection.IsEmpty ? This.Selection.Start : This.Selection.Start.GetFrozenPointer(LogicalDirection.Forward);
        }


        // Returns a position in the end of a selection,
        // which belongs to visually selected line.
        // It may be different from Selection.End when
        // selection ends immediately after line break
        // and oriented backward, i.e. belongs visually
        // to previous line.
        private static ITextPointer GetEndInner(TextEditor This)
        {
            ITextPointer end = This.Selection.End;
            if (end.CompareTo(This.Selection.MovingPosition) == 0)
            {
                end = GetMovingPositionInner(This);
            }
            return end;
        }

        // Returns a position at the start of current line of movingPosition.
        private static ITextPointer GetPositionAtLineStart(ITextPointer position)
        {
            TextSegment lineRange = position.TextContainer.TextView.GetLineRange(position);
            // When the moving position passed to this method is at hard structual boundaries such as AnchoredBlock or TableCell,
            // TextView returns a null line range.
            return lineRange.IsNull ? position : lineRange.Start;
        }

        // Returns a position at the end of current line of movingPosition.
        private static ITextPointer GetPositionAtLineEnd(ITextPointer position)
        {
            TextSegment lineRange = position.TextContainer.TextView.GetLineRange(position);
            return lineRange.IsNull ? position : lineRange.End;
        }

        // Helper to extend selection edge to passed position and bring it into view for paginated viewers.
        private static void ExtendSelectionAndBringIntoView(ITextPointer position, TextEditor textEditor)
        {
            textEditor.Selection.ExtendToPosition(position);
            BringIntoView(position, textEditor);
        }

        private static void BringIntoView(ITextPointer position, TextEditor textEditor)
        {
            double pageHeight = (double)textEditor.UiScope.GetValue(TextEditor.PageHeightProperty);

            if (pageHeight == 0 && // Check for paginated viewer case
                textEditor.TextView != null && textEditor.TextView.IsValid && !textEditor.TextView.Contains(position) && IsPaginated(textEditor.TextView))
            {
                // This will bring the position into view when it is in another page for paginated viewers.
                textEditor.TextView.BringPositionIntoViewAsync(position, textEditor);
            }
        }

        // If the caret is currently at a Table row end, move it inside
        // the last cell.
        // This is useful when trying to navigate by line, because ITextView
        // cannot handle the special end of row position.
        private static void AdjustCaretAtTableRowEnd(TextEditor This)
        {
            if (This.Selection.IsEmpty && TextPointerBase.IsAtRowEnd(This.Selection.Start))
            {
                ITextPointer position = This.Selection.Start.GetNextInsertionPosition(LogicalDirection.Backward);
                if (position != null)
                {
                    This.Selection.SetCaretToPosition(position, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                }
            }
        }

        // If a position is currently at a Table row end, move it inside
        // the last cell.
        // This is useful when trying to navigate by line, because ITextView
        // cannot handle the special end of row position.
        private static ITextPointer AdjustPositionAtTableRowEnd(ITextPointer position)
        {
            if (TextPointerBase.IsAtRowEnd(position))
            {
                ITextPointer cellEnd = position.GetNextInsertionPosition(LogicalDirection.Backward);
                if (cellEnd != null)
                {
                    position = cellEnd;
                }
            }

            return position;
        }

        // Returns the FlowDirection of the closest scoping Block.
        private static FlowDirection GetScopingParagraphFlowDirection(ITextPointer position)
        {
            ITextPointer navigator = position.CreatePointer();

            while (typeof(Inline).IsAssignableFrom(navigator.ParentType))
            {
                navigator.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            return (FlowDirection)navigator.GetValue(FrameworkElement.FlowDirectionProperty);
        }

        // Returns the x offset, relative to the left edge of the document
        // of an ITextPointer.
        //
        // This is distinct from ITextView.GetRectFromPosition, which returns
        // a point relative to the viewport, which may be offset by a scrollviewer.
        private static double GetAbsoluteXOffset(ITextView textview, ITextPointer position)
        {
            double x = textview.GetRectangleFromTextPosition(position).X;

            // this test only succeeds for TextBoxView.
            // We need to extend ITextView to make the check explicit.
            // Notably, RichTextbox is missed here. 
            if (textview is TextBoxView) // Extra strict....this could be removed in the future.
            {
                IScrollInfo scrollInfo = textview as IScrollInfo;
                if (scrollInfo != null)
                {
                    x += scrollInfo.HorizontalOffset;
                }
            }

            return x;
        }

        // Returns the x offset, relative to the viewport, of an x position
        // in document coordinates.
        private static double GetViewportXOffset(ITextView textview, double suggestedX)
        {
            // this test only succeeds for TextBoxView.
            // We need to extend ITextView to make the check explicit.
            // Notably, RichTextbox is missed here. 
            if (textview is TextBoxView) // Extra strict....this could be removed in the future.
            {
                IScrollInfo scrollInfo = textview as IScrollInfo;
                if (scrollInfo != null)
                {
                    suggestedX -= scrollInfo.HorizontalOffset;
                }
            }

            return suggestedX;
        }

        #endregion Misceleneous Private Methods

        #endregion Private methods

        private const string KeyMoveDownByLine = "Down";
        private const string KeyMoveDownByPage = "PageDown";
        private const string KeyMoveDownByParagraph = "Ctrl+Down";
        private const string KeyMoveLeftByCharacter = "Left";
        private const string KeyMoveLeftByWord = "Ctrl+Left";
        private const string KeyMoveRightByCharacter = "Right";
        private const string KeyMoveRightByWord = "Ctrl+Right";
        private const string KeyMoveToColumnEnd = "Alt+PageDown";
        private const string KeyMoveToColumnStart = "Alt+PageUp";
        private const string KeyMoveToDocumentEnd = "Ctrl+End";
        private const string KeyMoveToDocumentStart = "Ctrl+Home";
        private const string KeyMoveToLineEnd = "End";
        private const string KeyMoveToLineStart = "Home";
        private const string KeyMoveToWindowBottom = "Alt+Ctrl+PageDown";
        private const string KeyMoveToWindowTop = "Alt+Ctrl+PageUp";
        private const string KeyMoveUpByLine = "Up";
        private const string KeyMoveUpByPage = "PageUp";
        private const string KeyMoveUpByParagraph = "Ctrl+Up";
        private const string KeySelectAll = "Ctrl+A";
        private const string KeySelectDownByLine = "Shift+Down";
        private const string KeySelectDownByPage = "Shift+PageDown";
        private const string KeySelectDownByParagraph = "Ctrl+Shift+Down";
        private const string KeySelectLeftByCharacter = "Shift+Left";
        private const string KeySelectLeftByWord = "Ctrl+Shift+Left";
        private const string KeySelectRightByCharacter = "Shift+Right";
        private const string KeySelectRightByWord = "Ctrl+Shift+Right";
        private const string KeySelectToColumnEnd = "Alt+Shift+PageDown";
        private const string KeySelectToColumnStart = "Alt+Shift+PageUp";
        private const string KeySelectToDocumentEnd = "Ctrl+Shift+End";
        private const string KeySelectToDocumentStart = "Ctrl+Shift+Home";
        private const string KeySelectToLineEnd = "Shift+End";
        private const string KeySelectToLineStart = "Shift+Home";
        private const string KeySelectToWindowBottom = "Alt+Ctrl+Shift+PageDown";
        private const string KeySelectToWindowTop = "Alt+Ctrl+Shift+PageUp";
        private const string KeySelectUpByLine = "Shift+Up";
        private const string KeySelectUpByPage = "Shift+PageUp";
        private const string KeySelectUpByParagraph = "Ctrl+Shift+Up";
    }
}
