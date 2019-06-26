// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Text editing service for controls.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.Interop;
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
    using System.Windows.Markup;
    using System.Security;
    using System.Windows.Interop;
    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Subcomponent of TextEditor class - Support for Typing
    /// </summary>
    internal static class TextEditorTyping
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        /// <summary>
        /// Registes all handlers needed for text editing control functioning.
        /// </summary>
        /// <param name="controlType">
        /// A type of control for which typing component is registered
        /// </param>
        /// <param name="registerEventListeners">
        /// If registerEventListeners is false, caller is responsible for calling OnXXXEvent methods on TextEditor from
        /// UIElement and FrameworkElement virtual overrides (piggy backing on the
        /// UIElement/FrameworkElement class listeners).  If true, TextEditor will register
        /// its own class listener for events it needs.
        ///
        /// This method will always register private command listeners.
        /// </param>
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners)
        {
            if (registerEventListeners)
            {
                EventManager.RegisterClassHandler(controlType, Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
                EventManager.RegisterClassHandler(controlType, Keyboard.KeyDownEvent, new KeyEventHandler(OnKeyDown));
                EventManager.RegisterClassHandler(controlType, Keyboard.KeyUpEvent, new KeyEventHandler(OnKeyUp));
                EventManager.RegisterClassHandler(controlType, TextCompositionManager.TextInputEvent, new TextCompositionEventHandler(OnTextInput));
            }

            var onEnterBreak = new ExecutedRoutedEventHandler(OnEnterBreak);
            var onSpace = new ExecutedRoutedEventHandler(OnSpace);
            var onQueryStatusNYI = new CanExecuteRoutedEventHandler(OnQueryStatusNYI);
            var onQueryStatusEnterBreak = new CanExecuteRoutedEventHandler(OnQueryStatusEnterBreak);
            
            EventManager.RegisterClassHandler(controlType, Mouse.MouseMoveEvent, new MouseEventHandler(OnMouseMove), true /* handledEventsToo */);
            EventManager.RegisterClassHandler(controlType, Mouse.MouseLeaveEvent, new MouseEventHandler(OnMouseLeave), true /* handledEventsToo */);

            CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.CorrectionList   , new ExecutedRoutedEventHandler(OnCorrectionList)       , new CanExecuteRoutedEventHandler(OnQueryStatusCorrectionList)       , SRID.KeyCorrectionList,   SRID.KeyCorrectionListDisplayString         );
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleInsert         , new ExecutedRoutedEventHandler(OnToggleInsert)         , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyToggleInsert,     SRID.KeyToggleInsertDisplayString           ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.Delete               , new ExecutedRoutedEventHandler(OnDelete)               , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyDelete,           SRID.KeyDeleteDisplayString                 ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DeleteNextWord       , new ExecutedRoutedEventHandler(OnDeleteNextWord)       , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyDeleteNextWord,   SRID.KeyDeleteNextWordDisplayString         ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DeletePreviousWord   , new ExecutedRoutedEventHandler(OnDeletePreviousWord)   , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyDeletePreviousWord, SRID.KeyDeletePreviousWordDisplayString   ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.EnterParagraphBreak  , onEnterBreak                                           , onQueryStatusEnterBreak           , KeyGesture.CreateFromResourceStrings(KeyEnterParagraphBreak, SRID.KeyEnterParagraphBreakDisplayString ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.EnterLineBreak       , onEnterBreak                                           , onQueryStatusEnterBreak           , KeyGesture.CreateFromResourceStrings(KeyEnterLineBreak,   SRID.KeyEnterLineBreakDisplayString         ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.TabForward           , new ExecutedRoutedEventHandler(OnTabForward)           , new CanExecuteRoutedEventHandler(OnQueryStatusTabForward)           , KeyGesture.CreateFromResourceStrings(KeyTabForward,       SRID.KeyTabForwardDisplayString             ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.TabBackward          , new ExecutedRoutedEventHandler(OnTabBackward)          , new CanExecuteRoutedEventHandler(OnQueryStatusTabBackward)          , KeyGesture.CreateFromResourceStrings(KeyTabBackward,      SRID.KeyTabBackwardDisplayString            ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.Space                , onSpace                                                , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeySpace,            SRID.KeySpaceDisplayString                  ));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ShiftSpace           , onSpace                                                , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyShiftSpace,       SRID.KeyShiftSpaceDisplayString             ));

            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.Backspace            , new ExecutedRoutedEventHandler(OnBackspace)            , onQueryStatusNYI                  , KeyGesture.CreateFromResourceStrings(KeyBackspace,        SR.Get(SRID.KeyBackspaceDisplayString)),   KeyGesture.CreateFromResourceStrings(KeyShiftBackspace, SR.Get(SRID.KeyShiftBackspaceDisplayString)) );
        }

        /// <summary>
        /// Add the input language changed event handler and save it
        /// into UIContext data slot.
        /// </summary>
        internal static void _AddInputLanguageChangedEventHandler(TextEditor This)
        {
            TextEditorThreadLocalStore threadLocalStore;

            Invariant.Assert(This._dispatcher == null);
            This._dispatcher = Dispatcher.CurrentDispatcher;
            Invariant.Assert(This._dispatcher != null);

            threadLocalStore = TextEditor._ThreadLocalStore;

            // Only add the input language changed event handler once that safe per UIContext
            if (threadLocalStore.InputLanguageChangeEventHandlerCount == 0)
            {
                // Add input changed event handler into InputLanguageManager
                InputLanguageManager.Current.InputLanguageChanged += new InputLanguageEventHandler(OnInputLanguageChanged);

                // Add the dispatcher shutdown finished event handler to remove InputLanguageChangedEventHandler
                // before dispose the dispatcher.
                Dispatcher.CurrentDispatcher.ShutdownFinished += new EventHandler(OnDispatcherShutdownFinished);
            }

            threadLocalStore.InputLanguageChangeEventHandlerCount++;
        }

        /// <summary>
        /// Remove the input language changed event handler from UIContext data slot.
        /// </summary>
        internal static void _RemoveInputLanguageChangedEventHandler(TextEditor This)
        {
            TextEditorThreadLocalStore threadLocalStore;

            threadLocalStore = TextEditor._ThreadLocalStore;

            // Decrease the input language changed event handler reference count
            threadLocalStore.InputLanguageChangeEventHandlerCount--;

            // Remove the input language changed event handler when nobody reference it
            if (threadLocalStore.InputLanguageChangeEventHandlerCount == 0)
            {
                // Remove InputLanguageEventHandler
                InputLanguageManager.Current.InputLanguageChanged -= new InputLanguageEventHandler(OnInputLanguageChanged);

                // Remove the dispatcher shutdown finished event handler
                Dispatcher.CurrentDispatcher.ShutdownFinished -= new EventHandler(OnDispatcherShutdownFinished);
            }
        }

        /// <summary>
        /// Discards previous typing undo unit, to prevent
        /// from merging it with the subsequent typing.
        /// </summary>
        internal static void _BreakTypingSequence(TextEditor This)
        {
            // Discard typing undo unit
            This._typingUndoUnit = null;
        }

        // Handles any pending input events.
        internal static void _FlushPendingInputItems(TextEditor This)
        {
            TextEditorThreadLocalStore threadLocalStore;

            if (This.TextView != null)
            {
                This.TextView.ThrottleBackgroundTasksForUserInput();
            }

            threadLocalStore = TextEditor._ThreadLocalStore;

            if (threadLocalStore.PendingInputItems != null)
            {
                try
                {
                    for (int i = 0; i < threadLocalStore.PendingInputItems.Count; i++)
                    {
                        ((InputItem)threadLocalStore.PendingInputItems[i]).Do();

                        // After the first dequeue, clear the bit that tracks if
                        // any events are handled after ctl+shift (change flow direction keyboard hotkey).
                        threadLocalStore.PureControlShift = false;
                    }
                }
                finally
                {
                    threadLocalStore.PendingInputItems.Clear();
                }
            }

            // Clear the bit that tracks if any events are handled after
            // ctl+shift (change flow direction keyboard hotkey) one last
            // time, in case the queue was empty.
            //
            // Because we only call this method in preparation for handling
            // a Command, we want this bit cleared.
            threadLocalStore.PureControlShift = false;
        }

        // Un-hides the mouse cursor.
        internal static void _ShowCursor()
        {
            if (TextEditor._ThreadLocalStore.HideCursor)
            {
                TextEditor._ThreadLocalStore.HideCursor = false;
                SafeNativeMethods.ShowCursor(true);
            }
        }

        // ................................................................
        //
        // Event Handlers
        //
        // ................................................................

        /// <summary>
        /// Removes selected content in a RichTextBox when an IME is jump-starting a composition
        /// over existing content.
        /// </summary>
        /// <remarks>
        /// This is a work around for a messy situation we get into when a composition starts
        /// over a non-empty selection in the RichTextBox (eg, a user selects some content and
        /// then starts typing with an IME).
        /// 
        /// In general, code in TextStore.cs tracks IME composition events with character offsets
        /// and often restores and then "plays back" the changes.  If the character offsets of the
        /// original and played back composition events do not match exactly, the editor enters
        /// an inconsistent state and crashes or worse.
        /// 
        /// There is code in TextStore.OnStartComposition that attempts to ensure character offsets
        /// always match in the case where an IME starts a non-empty composition.  Comments in the
        /// method have details.
        /// 
        /// However, that code only handles the case where the selection is empty (a caret/single
        /// insertion point).  It will in fact cause problems if the composition is non-empty
        /// because the initial selection was non-empty.  In that case, the code in
        /// TextStore.OnStartComposition attempts to convert
        /// elements like LineBreak that are normally invisible to the IME but round-trip as text
        /// like "\r\n". This confuses the before/after playback state because character counts
        /// do not match.
        /// 
        /// The code here is a work around -- if we detect a composition start request with a non-empty
        /// selection, we preemtively remove the selected content before the IME has a chance to do
        /// so.  We can't do that work later because once the IME has started a composition no
        /// reentrant edits are allowed.
        /// 
        /// Modifying the document from PreviewKeyDown is not ideal.  A better long term solution
        /// is to change the way we expose the document to the IME so that reentrancy is not an
        /// issue.  However, we don't have time left in dev10 to do that work.  This solution is
        /// a compromise that avoids serious crashes while typing over selected text with IMEs.
        /// </remarks>
        internal static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.ImeProcessed)
            {
                return;
            }

            RichTextBox richTextBox = sender as RichTextBox;

            if (richTextBox == null)
            {
                return;
            }

            TextEditor This = richTextBox.TextEditor;

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This._IsSourceInScope(e.OriginalSource))
            {
                return;
            }

            // Ignore repeated events generated when the key is hold down for long time
            if (e.IsRepeat)
            {
                return;
            }

            if (This.TextStore == null || 
                This.TextStore.IsComposing)
            {
                return;
            }

            if (richTextBox.Selection.IsEmpty)
            {
                return;
            }

            This.SetText(This.Selection, String.Empty, InputLanguageManager.Current.CurrentInputLanguage);

            // NB: we do not handle the event.  We want the IME to handle it.
        }

        // KeyDownEvent handler - needed for handling FlowDirection commands on KeyUp
        internal static void OnKeyDown(object sender, KeyEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || (This.IsReadOnly && !This.IsReadOnlyCaretVisible) || !This._IsSourceInScope(e.OriginalSource))
            {
                return;
            }

            // Ignore repeated events generated when the key is hold down for long time
            if (e.IsRepeat)
            {
                return;
            }

            // If UiScope has a ToolTip and it is open, any keyboard/mouse activity should close the tooltip.
            This.CloseToolTip();

            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            // Clear a flag indicating that Shift key was pressed without any following key
            // This flag is necessary for KeyUp(RightShift/LeftShift) processing.
            threadLocalStore.PureControlShift = false;

            // Shift+Ctrl combination must be executed only when it's "pure" -
            // no mouse dragging/movement, no other key downs involved in a gesture.
            if (This.TextView != null && !This.UiScope.IsMouseCaptured)
            {
                if ((e.Key == Key.RightShift || e.Key == Key.LeftShift) && //
                    (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == 0)
                {
                    threadLocalStore.PureControlShift = true; // will be cleared by any other key down
                }
                else if ((e.Key == Key.RightCtrl || e.Key == Key.LeftCtrl) && //
                    (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0 && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == 0)
                {
                    threadLocalStore.PureControlShift = true; // will be cleared by any other key down
                }
                else if (e.Key == Key.RightCtrl || e.Key == Key.LeftCtrl)
                {
                    UpdateHyperlinkCursor(This);
                }
            }
        }

        // Handler for KeyUp events
        internal static void OnKeyUp(object sender, KeyEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || (This.IsReadOnly && !This.IsReadOnlyCaretVisible) || !This._IsSourceInScope(e.OriginalSource))
            {
                return;
            }

            // Delegate the work to specific handlers.
            switch (e.Key)
            {
                case Key.RightShift:
                case Key.LeftShift:
                    if (TextEditor._ThreadLocalStore.PureControlShift && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == 0)
                    {
                        TextEditorTyping.ScheduleInput(This, new KeyUpInputItem(This, e.Key, e.KeyboardDevice.Modifiers));
                    }
                    break;

                case Key.LeftCtrl:
                case Key.RightCtrl:
                    UpdateHyperlinkCursor(This);
                    break;
            }
        }


        // TextInputEvent handler.
        internal static void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This._IsSourceInScope(e.OriginalSource))
            {
                return;
            }

            FrameworkTextComposition composition = e.TextComposition as FrameworkTextComposition;

            // Ignore any event with an empty Text property.
            // The public TextCompositionEventArgs ctor allows null Text values.
            // Also it's possible to have non-null ControlText or AltText with String.Empty Text values.
            if (composition == null &&
                (e.Text == null || e.Text.Length == 0))
            {
                return;
            }

            // Consider event handled
            e.Handled = true;

            if (This.TextView != null)
            {
                This.TextView.ThrottleBackgroundTasksForUserInput();
            }

            // If this event is our Cicero TextStore composition, we always handles through ITextStore::SetText.
            if (composition != null)
            {
                if (composition.Owner == This.TextStore)
                {
                    This.TextStore.UpdateCompositionText(composition);
                }
                else if (composition.Owner == This.ImmComposition)
                {
                    This.ImmComposition.UpdateCompositionText(composition);
                }
            }
            else
            {
                // Input text (with springload formatting if any)
                // We'll delay the event handling, batching it up with other
                // input if layout is too slow to keep up with the input stream.
                KeyboardDevice keyboard = e.Device as KeyboardDevice;
                TextEditorTyping.ScheduleInput(This, new TextInputItem(This, e.Text, /*isInsertKeyToggled:*/keyboard != null ? keyboard.IsKeyToggled(Key.Insert) : false));
            }
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // ................................................................
        //
        // Command Handlers
        //
        // ................................................................

        /// <summary>
        /// CorrectionList command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusCorrectionList(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null)
            {
                return;
            }

            if (This.TextStore != null)
            {
                // Don't do actual reconversion, it just checks if the current selection is reconvertable.
                args.CanExecute = This.TextStore.QueryRangeOrReconvertSelection( /*fDoReconvert:*/ false);
            }
            else
            {
                // If there is no textstore, this command is not enabled.
                args.CanExecute = false;
            }
        }

        /// <summary>
        /// CorrectionList command event handler.
        /// </summary>
        private static void OnCorrectionList(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null)
            {
                return;
            }

            if (This.TextStore != null)
            {
                This.TextStore.QueryRangeOrReconvertSelection( /*fDoReconvert:*/ true);
            }
        }

        /// <summary>
        /// ToggleInsert command handler
        /// </summary>
        private static void OnToggleInsert(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            This._OvertypeMode = !This._OvertypeMode;

            // Use Cicero's transitory extension for OverTyping.
            if (TextServicesLoader.ServicesInstalled && (This.TextStore != null))
            {
                TextServicesHost tsfHost = TextServicesHost.Current;
                if (tsfHost != null)
                {
                    if (This._OvertypeMode)
                    {
                        IInputElement element = target as IInputElement;
                        if (element != null)
                        {
                            PresentationSource.AddSourceChangedHandler(element, OnSourceChanged);
                        }
                        
                        TextServicesHost.StartTransitoryExtension(This.TextStore);
                    }
                    else
                    {
                        IInputElement element = target as IInputElement;
                        if (element != null)
                        {
                            PresentationSource.RemoveSourceChangedHandler(element, OnSourceChanged);
                        }
                        
                        TextServicesHost.StopTransitoryExtension(This.TextStore);
                    }
                }
            }
        }

        // This should only be invoked on a text control in Overtype mode being tracked for presentation source 
        // changes. Connecting or disconnecting from a window fires this notification.
        private static void OnSourceChanged(object sender, SourceChangedEventArgs args)
        {
            OnToggleInsert(sender, null);
        }

        // ...........................................................................
        //
        // Delete Characters
        //
        // ...........................................................................

        private static void OnDelete(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // Note, that Delete and Backspace keys behave differently.
            ((TextSelection)This.Selection).ClearSpringloadFormatting();

            // Forget previously suggested horizontal position
            TextEditorSelection._ClearSuggestedX(This);

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer position = This.Selection.End;
                if (This.Selection.IsEmpty)
                {
                    ITextPointer deletePosition = position.GetNextInsertionPosition(LogicalDirection.Forward);

                    if (deletePosition == null)
                    {
                        // Nothing to delete.
                        return;
                    }

                    if (TextPointerBase.IsAtRowEnd(deletePosition))
                    {
                        // Backspace and delete are a no-op at row end positions.
                        return;
                    }

                    if (position is TextPointer && !IsAtListItemStart(deletePosition) &&
                        HandleDeleteWhenStructuralBoundaryIsCrossed(This, (TextPointer)position, (TextPointer)deletePosition))
                    {
                        // We are crossing structural boundary and
                        // selection was updated in HandleDeleteWhenStructuralBoundaryIsCrossed.
                        return;
                    }

                    // Selection is empty, extend selection forward to delete the following char.
                    This.Selection.ExtendToNextInsertionPosition(LogicalDirection.Forward);
                }

                // Delete selected text.
                This.Selection.Text = String.Empty;
            }
        }

        private static void OnBackspace(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This._IsSourceInScope(args.Source))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // Forget previously suggested horizontal position.
            TextEditorSelection._ClearSuggestedX(This);

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer position = This.Selection.Start;

                // Note that this is different than the previous insertion position in backward direction,
                // in case of combining characters and surrogates.
                ITextPointer backspacePosition = null;

                // In case when selection is empty we will need to expand
                // it backward. Check first whether we are crossing
                // any structural boundary - to disable the operation
                // in such case.
                if (This.Selection.IsEmpty)
                {
                    // Identify a case for special actions in the beginning of paragraphs or list items

                    if (This.AcceptsRichContent && IsAtListItemStart(position))
                    {
                        // Remove a bullet from this list item.
                        // Note that doing anything more aggressive like unindenting
                        // would make backspace very inconvenient for merging two same-level list items.
                        TextRangeEditLists.ConvertListItemsToParagraphs((TextRange)This.Selection);
                    }
                    else if (This.AcceptsRichContent &&
                             (IsAtListItemChildStart(position, false /* emptyChildOnly */) || IsAtIndentedParagraphOrBlockUIContainerStart(This.Selection.Start)))
                    {
                        // Unindent the list by one level.
                        TextEditorLists.DecreaseIndentation(This);
                    }
                    else
                    {
                        // Find a preceding position.
                        ITextPointer deletePosition = position.GetNextInsertionPosition(LogicalDirection.Backward);

                        if (deletePosition == null)
                        {
                            // Nothing to delete.
                            ((TextSelection)This.Selection).ClearSpringloadFormatting();
                            return;
                        }

                        if (TextPointerBase.IsAtRowEnd(deletePosition))
                        {
                            // Backspace and delete are a no-op at row end positions.
                            ((TextSelection)This.Selection).ClearSpringloadFormatting();
                            return;
                        }

                        if (position is TextPointer &&
                            HandleDeleteWhenStructuralBoundaryIsCrossed(This, (TextPointer)position, (TextPointer)deletePosition))
                        {
                            // We are crossing structural boundary and
                            // selection was updated in HandleDeleteWhenStructuralBoundaryIsCrossed.
                            return;
                        }

                        // Normalize the current position backward.
                        position = position.GetFrozenPointer(LogicalDirection.Backward);

                        // If TextView is valid, we can get the backspace position from TextView and then
                        // delete the content from the backspace position to the current position.
                        // Otherwise, we move the selection to the previous insertion position then delete.
                        if (This.TextView != null &&
                            position.HasValidLayout &&
                            position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
                        {
                            // Get the backspace caret unit position from TextView that support surrogate
                            // and all internal characters
                            backspacePosition = This.TextView.GetBackspaceCaretUnitPosition(position);
                            Invariant.Assert(backspacePosition != null);

                            // bug 1733868
                            // backspacePosition should always be less than position.
                            // But backspacing before '\n' (no preceding '\r') exposes
                            // this bug.
                            if (backspacePosition.CompareTo(position) == 0)
                            {
                                // As of 6/30/2006 we're too close to ship to fix
                                // this bug cleanly.  Ideally, we would stop referencing
                                // the position at the end-of-line (which mil text does not
                                // consider a valid position), and instead reference the start
                                // of the next line (flipping the original position's gravity).
                                //
                                // As a work-around, take the previous insertion position,
                                // ignoring glyph level backspace positions.
                                //
                                This.Selection.ExtendToNextInsertionPosition(LogicalDirection.Backward);
                                backspacePosition = null;
                            }
                            // If there is no text preceding the backspacePosition, extend to the next
                            // insertion position to make sure we cleanup any empty Inlines left
                            // after the delete.  We don't want a non-empty selection if there is
                            // bordering text, because we might normalize outside of a run of combining
                            // marks otherwise.
                            else if (backspacePosition.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.Text)
                            {
                                This.Selection.Select(This.Selection.End, backspacePosition);
                                backspacePosition = null;
                            }
                        }
                        else
                        {
                            // Selection is empty, extend it backward to include the preceeding char.
                            This.Selection.ExtendToNextInsertionPosition(LogicalDirection.Backward);
                        }
                    }
                }

                // Save current formatting properties for springload formatting before backspace
                // Note, that Delete and Backspace keys behave differently: it's by design.
                if (This.AcceptsRichContent)
                {
                    ((TextSelection)This.Selection).ClearSpringloadFormatting();
                    ((TextSelection)This.Selection).SpringloadCurrentFormatting();
                }

                // If backspace position is available from TextView, we can delete it directly
                // without the normalization. Because we already normalized the backspace position.
                if (backspacePosition != null)
                {
                    Invariant.Assert(backspacePosition.CompareTo(position) < 0);
                    // Delete the content from the backspace to the current position
                    backspacePosition.DeleteContentToPosition(position);
                }
                else
                {
                    // Delete selected text
                    This.Selection.Text = String.Empty;
                    position = This.Selection.Start;
                }

                // Set the caret position with the Backward direction,
                // because we want to appear close to previous character.
                // However, we do not allow to stop at end of line.
                // We alow to stop next to space - to be consistent with typing behavior.
                This.Selection.SetCaretToPosition(position, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/true);
            }
        }

        // Helper for OnDelete/OnBackspace, handles special case scenarios for delete when table or BlockUIContainer boundaries are crossed.
        // Returns true if passed positions were in this category and appropriate editing action was taken for handling delete operation.
        // Otherwise, returns false.
        private static bool HandleDeleteWhenStructuralBoundaryIsCrossed(TextEditor This, TextPointer position, TextPointer deletePosition)
        {
            if (!TextRangeEditTables.IsTableStructureCrossed(position, deletePosition) &&
                !IsBlockUIContainerBoundaryCrossed(position, deletePosition) &&
                !TextPointerBase.IsAtRowEnd(position))
            {
                return false;
            }

            LogicalDirection directionOfDelete = position.CompareTo(deletePosition) < 0 ? LogicalDirection.Forward : LogicalDirection.Backward;

            Block paragraphOrBlockUIContainerToDelete = position.ParagraphOrBlockUIContainer;

            // Check if an empty paragraph or BlockUIContainer needs to be deleted.
            if (paragraphOrBlockUIContainerToDelete != null)
            {
                if (directionOfDelete == LogicalDirection.Forward)
                {
                    // We check for next/previous block here, to avoid deletion of an empty paragraph when a list/table boundary is crossed.
                    // Note however, we dont treat sections as structural boundaries. So this check does not let us delete a last empty
                    // paragraph in a section. Investigate the section case more...

                    if (paragraphOrBlockUIContainerToDelete.NextBlock != null &&
                        paragraphOrBlockUIContainerToDelete is Paragraph && Paragraph.HasNoTextContent((Paragraph)paragraphOrBlockUIContainerToDelete) || // empty paragraph
                        paragraphOrBlockUIContainerToDelete is BlockUIContainer && paragraphOrBlockUIContainerToDelete.IsEmpty) // empty BlockUIContainer
                    {
                        paragraphOrBlockUIContainerToDelete.RepositionWithContent(null);
                    }
                }
                else
                {
                    if (paragraphOrBlockUIContainerToDelete.PreviousBlock != null &&
                        paragraphOrBlockUIContainerToDelete is Paragraph && Paragraph.HasNoTextContent((Paragraph)paragraphOrBlockUIContainerToDelete) || // empty paragraph
                        paragraphOrBlockUIContainerToDelete is BlockUIContainer && paragraphOrBlockUIContainerToDelete.IsEmpty) // empty BlockUIContainer
                    {
                        paragraphOrBlockUIContainerToDelete.RepositionWithContent(null);
                    }
                }
            }

            // Set caret position.
            This.Selection.SetCaretToPosition(deletePosition, directionOfDelete, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/true);

            if (directionOfDelete == LogicalDirection.Backward)
            {
                // Clear springload formatting in case of backspace
                ((TextSelection)This.Selection).ClearSpringloadFormatting();
            }

            return true;
        }

        // Tests if the position is at the beginning of indented paragraph -
        // to allow Backspace to decrease indentation
        private static bool IsAtIndentedParagraphOrBlockUIContainerStart(ITextPointer position)
        {
            if ((position is TextPointer) && TextPointerBase.IsAtParagraphOrBlockUIContainerStart(position))
            {
                Block paragraphOrBlockUIContainer = ((TextPointer)position).ParagraphOrBlockUIContainer;
                if (paragraphOrBlockUIContainer != null)
                {
                    FlowDirection flowDirection = paragraphOrBlockUIContainer.FlowDirection;
                    Thickness margin = paragraphOrBlockUIContainer.Margin;

                    return
                        flowDirection == FlowDirection.LeftToRight && margin.Left > 0 ||
                        flowDirection == FlowDirection.RightToLeft && margin.Right > 0 ||
                        (paragraphOrBlockUIContainer is Paragraph && ((Paragraph)paragraphOrBlockUIContainer).TextIndent > 0);
                }
            }

            return false;
        }

        // Tests if the position is at the beginning of some list item -
        // to allow Backspace to delete the bullet.
        private static bool IsAtListItemStart(ITextPointer position)
        {
            // Check for empty ListItem case
            if (typeof(ListItem).IsAssignableFrom(position.ParentType) &&
                position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                return true;
            }

            while (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                Type parentType = position.ParentType;
                if (TextSchema.IsBlock(parentType))
                {
                    if (TextSchema.IsParagraphOrBlockUIContainer(parentType))
                    {
                        position = position.GetNextContextPosition(LogicalDirection.Backward);
                        if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                            typeof(ListItem).IsAssignableFrom(position.ParentType))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                position = position.GetNextContextPosition(LogicalDirection.Backward);
            }
            return false;
        }

        // Tests if a position is at the start of a Block
        // within a ListItem.
        //
        // position must be normalized at an insertion point.
        private static bool IsAtListItemChildStart(ITextPointer position, bool emptyChildOnly)
        {
            if (position.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart)
            {
                return false;
            }

            if (emptyChildOnly &&
                position.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.ElementEnd)
            {
                return false;
            }

            ITextPointer navigator = position.CreatePointer();

            // Cross inline opening tags.
            while (navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                   typeof(Inline).IsAssignableFrom(navigator.ParentType))
            {
                navigator.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            // Check if navigator is at the start of a block.
            if (!(navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.IsParagraphOrBlockUIContainer(navigator.ParentType)))
            {
                return false;
            }

            // Move just past the block.
            navigator.MoveToElementEdge(ElementEdge.BeforeStart);
            return typeof(ListItem).IsAssignableFrom(navigator.ParentType);
        }

        // Tests if position1 and position2 cross a BlockUIContainer boundary.
        private static bool IsBlockUIContainerBoundaryCrossed(TextPointer position1, TextPointer position2)
        {
            return
                (position1.Parent is BlockUIContainer || position2.Parent is BlockUIContainer) &&
                position1.Parent != position2.Parent;
        }

        // ...........................................................................
        //
        // Delete Words
        //
        // ...........................................................................

        private static void OnDeleteNextWord(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            if (This.Selection.IsTableCellRange)
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            ITextPointer wordBoundary = This.Selection.End.CreatePointer();

            // When selection is not empty the command deletes selected content
            // without extending it to the word bopundary. For empty selection
            // the command deletes a content from caret position to
            // nearest word boundary in a given direction
            if (This.Selection.IsEmpty)
            {
                TextPointerBase.MoveToNextWordBoundary(wordBoundary, LogicalDirection.Forward);
            }

            if (TextRangeEditTables.IsTableStructureCrossed(This.Selection.Start, wordBoundary))
            {
                return;
            }

            ITextRange textRange = new TextRange(This.Selection.Start, wordBoundary);

            // When a range is TableCellRange we do not want to make deletions
            if (textRange.IsTableCellRange)
            {
                return;
            }

            if (!textRange.IsEmpty)
            {
                using (This.Selection.DeclareChangeBlock())
                {
                    // Note asymetry with Backspace: we do not load springload formatting here
                    if (This.AcceptsRichContent)
                    {
                        ((TextSelection)This.Selection).ClearSpringloadFormatting();
                    }

                    This.Selection.Select(textRange.Start, textRange.End);

                    // Delete selected text
                    This.Selection.Text = String.Empty;
                }
            }
        }

        private static void OnDeletePreviousWord(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            if (This.Selection.IsTableCellRange)
            {
                //  Add code for clearing table cell range contents
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            ITextPointer wordBoundary = This.Selection.Start.CreatePointer();

            // When selection is not empty the command deletes selected content
            // without extending it to the word bopundary. For empty selection
            // the command deletes a content from caret position to
            // nearest word boundary in a given direction
            if (This.Selection.IsEmpty)
            {
                TextPointerBase.MoveToNextWordBoundary(wordBoundary, LogicalDirection.Backward);
            }

            // When the movement to word boundary crosses table structure, ignore the command
            if (TextRangeEditTables.IsTableStructureCrossed(wordBoundary, This.Selection.Start))
            {
                return;
            }

            // Build a range from a start of a word preceding start of selection, ending at the end of whole selection
            // This range is supposed to be deleted by the operation.
            ITextRange textRange = new TextRange(wordBoundary, This.Selection.End);

            // When a range is TableCellRange we do not want to make deletions
            if (textRange.IsTableCellRange)
            {
                return;
            }

            if (!textRange.IsEmpty)
            {
                using (This.Selection.DeclareChangeBlock())
                {
                    // Note asymetry with Backspace: we DO load springload formatting here
                    if (This.AcceptsRichContent)
                    {
                        ((TextSelection)This.Selection).ClearSpringloadFormatting();
                        This.Selection.Select(textRange.Start, textRange.End);
                        ((TextSelection)This.Selection).SpringloadCurrentFormatting();
                    }
                    else
                    {
                        This.Selection.Select(textRange.Start, textRange.End);
                    }

                    // Delete selected text
                    This.Selection.Text = String.Empty;
                }
            }
        }

        // ...........................................................................
        //
        // Enter Breaks
        //
        // ...........................................................................

        /// <summary>
        /// EnterParagraphBreak/EnterLineBreak command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusEnterBreak(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                args.ContinueRouting = true;
                return;
            }

            if (This.Selection.IsTableCellRange || !This.AcceptsReturn)
            {
                args.ContinueRouting = true;
                return;
            }

            args.CanExecute = true;
        }

        // EnterParagraphBreak/EnterLineBreak command handler
        private static void OnEnterBreak(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            if (This.Selection.IsTableCellRange || !This.AcceptsReturn || !This.UiScope.IsKeyboardFocused)
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // We do not merge Enter typing with other typing - for better undo structuring
            using (This.Selection.DeclareChangeBlock())
            {
                // Flag to indicate if selection was changed. It may be unaffected in following cases:
                // 1. In plain text case, Environment.NewLine can not fit in MaxLength
                // 2. In rich text case, we cannot split a hyperlink ancestor to insert a paragraph break
                bool wasSelectionChanged;

                if (This.AcceptsRichContent && This.Selection.Start is TextPointer)
                {
                    // Paragraph insertion for the case of rich text
                    wasSelectionChanged = HandleEnterBreakForRichText(This, args.Command);
                }
                else
                {
                    // Newline insertion for plain text
                    wasSelectionChanged = HandleEnterBreakForPlainText(This);
                }

                // Update caret and clear SuggestedX only when selection has changed.
                if (wasSelectionChanged)
                {
                    // Position the caret.
                    This.Selection.SetCaretToPosition(This.Selection.End, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);

                    // Forget previously suggested horizontal position
                    TextEditorSelection._ClearSuggestedX(This);
                }
            }
        }

        // Helper for OnEnterBreak for rich text case
        private static bool HandleEnterBreakForRichText(TextEditor This, ICommand command)
        {
            bool wasSelectionChanged = true;

            // Save current inline settings to continue on the next paragraph
            ((TextSelection)This.Selection).SpringloadCurrentFormatting();

            if (!This.Selection.IsEmpty)
            {
                // Delete selected content
                This.Selection.Text = String.Empty;
            }

            if (HandleEnterBreakWhenStructuralBoundaryIsCrossed(This, command))
            {
                // We are crossing structural boundary and
                // selection was updated if HandleEnterBreakWhenStructuralBoundaryIsCrossed returned true
            }
            else
            {
                TextPointer newEnd = ((TextSelection)This.Selection).End;

                if (command == EditingCommands.EnterParagraphBreak)
                {
                    if (newEnd.HasNonMergeableInlineAncestor && !TextPointerBase.IsPositionAtNonMergeableInlineBoundary(newEnd))
                    {
                        // Selection end is in the middle of a hyperlink element, enter is a no-op.
                        wasSelectionChanged = false;
                    }
                    else
                    {
                        newEnd = TextRangeEdit.InsertParagraphBreak(newEnd, /*moveIntoSecondParagraph*/true);
                    }
                }
                else if (command == EditingCommands.EnterLineBreak)
                {
                    newEnd = newEnd.InsertLineBreak();
                }

                if (wasSelectionChanged)
                {
                    This.Selection.Select(newEnd, newEnd);
                }
            }

            return wasSelectionChanged;
        }

        // Helper for OnEnterBreak for plain text case
        private static bool HandleEnterBreakForPlainText(TextEditor This)
        {
            bool wasSelectionChanged = true;

            // Filter Environment.NewLine based on TextBox.MaxLength
            string filteredText = This._FilterText(Environment.NewLine, This.Selection);

            if (filteredText != String.Empty)
            {
                This.Selection.Text = Environment.NewLine;
            }
            else
            {
                // Do not update selection if Environment.NewLine can not fit in.
                wasSelectionChanged = false;
            }

            return wasSelectionChanged;
        }

        // Helper for rich text OnEnterBreak case, handles special cases when a
        // structural boundary such as listitem, table, blockuicontainer is crossed.
        private static bool HandleEnterBreakWhenStructuralBoundaryIsCrossed(TextEditor This, ICommand command)
        {
            Invariant.Assert(This.Selection.Start is TextPointer);
            TextPointer position = (TextPointer)This.Selection.Start;

            bool structuralBoundaryIsCrossed = true;

            if (TextPointerBase.IsAtRowEnd(position))
            {
                // For both ParagraphBreak and LineBreak commands, insert a new row after the current one
                TextRange range = ((TextSelection)This.Selection).InsertRows(+1);
                This.Selection.SetCaretToPosition(range.Start, LogicalDirection.Forward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
            }
            else if (This.Selection.IsEmpty &&
                     (TextPointerBase.IsInEmptyListItem(position) || IsAtListItemChildStart(position, true /* emptyChildOnly */)) &&
                     command == EditingCommands.EnterParagraphBreak)
            {
                // Unindent the list by one level.
                TextEditorLists.DecreaseIndentation(This);
            }
            else if (TextPointerBase.IsBeforeFirstTable(position) ||
                TextPointerBase.IsAtBlockUIContainerStart(position))
            {
                // Calling EnsureInsertionPosition has the effect of inserting a paragraph BEFORE the table or BlockUIContainer/Table.
                // In this case, we do not want to move selection end to the paragraph just created.

                TextRangeEditTables.EnsureInsertionPosition(position);
            }
            else if (TextPointerBase.IsAtBlockUIContainerEnd(position))
            {
                // Calling EnsureInsertionPosition has the effect of inserting a paragraph AFTER the BlockUIContainer.
                // Update selection end to position in the following paragraph.

                TextPointer newEnd = TextRangeEditTables.EnsureInsertionPosition(position);
                This.Selection.Select(newEnd, newEnd);
            }
            else
            {
                structuralBoundaryIsCrossed = false;
            }

            return structuralBoundaryIsCrossed;
        }

        // ...........................................................................
        //
        // Flow Direction
        //
        // ...........................................................................

        /// <summary>
        /// LeftToRightFlowDirection command event handler.
        /// </summary>
        private static void OnFlowDirectionCommand(TextEditor This, Key key)
        {
            //  Detect appropriateness of FlowDirection command

            using (This.Selection.DeclareChangeBlock())
            {
                if (key == Key.LeftShift)
                {
                    if (This.AcceptsRichContent && (This.Selection is TextSelection))
                    {
                        // NOTE: We do not call OnApplyProperty to avoid recursion for FlushPendingInput
                        ((TextSelection)This.Selection).ApplyPropertyValue(FlowDocument.FlowDirectionProperty, FlowDirection.LeftToRight, /*applyToParagraphs*/true);
                    }
                    else
                    {
                        Invariant.Assert(This.UiScope != null);
                        UIElementPropertyUndoUnit.Add(This.TextContainer, This.UiScope, FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight);
                        This.UiScope.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight);
                    }
                }
                else
                {
                    Invariant.Assert(key == Key.RightShift);

                    if (This.AcceptsRichContent && (This.Selection is TextSelection))
                    {
                        // NOTE: We do not call OnApplyProperty to avoid recursion for FlushPendingInput
                        ((TextSelection)This.Selection).ApplyPropertyValue(FlowDocument.FlowDirectionProperty, FlowDirection.RightToLeft, /*applyToParagraphs*/true);
                    }
                    else
                    {
                        Invariant.Assert(This.UiScope != null);
                        UIElementPropertyUndoUnit.Add(This.TextContainer, This.UiScope, FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft);
                        This.UiScope.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft);
                    }
                }
                ((TextSelection)This.Selection).UpdateCaretState(CaretScrollMethod.Simple);
            }
        }

        // ...........................................................................
        //
        // In some controls, Space and Shift+Space keys are mapped to
        // scroll down and scroll up commands respectively.
        // In TextEditor, we handle them as text input.
        // Using the command system allows controls to override the existing default behavior.
        // ...........................................................................

        // Space, Shift+Space handler
        private static void OnSpace(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This._IsSourceInScope(e.OriginalSource))
            {
                return;
            }

            // If this event is our Cicero TextStore composition, we always handles through ITextStore::SetText.
            if (This.TextStore != null && This.TextStore.IsComposing)
            {
                return;
            }

            if (This.ImmComposition != null && This.ImmComposition.IsComposition)
            {
                return;
            }

            // Consider event handled
            e.Handled = true;

            if (This.TextView != null)
            {
                This.TextView.ThrottleBackgroundTasksForUserInput();
            }

            ScheduleInput(This, new TextInputItem(This, " ", /*isInsertKeyToggled:*/!This._OvertypeMode));
        }

        // ...........................................................................
        //
        // Tab and Back-Tab
        //
        // ...........................................................................

        /// <summary>
        /// ForwardTabStop command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusTabForward(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            if (This != null && This.AcceptsTab)
            {
                args.CanExecute = true;
            }
            else
            {
                args.ContinueRouting = true;
            }
        }

        /// <summary>
        /// BackwardTabStop command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusTabBackward(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            if (This != null && This.AcceptsTab)
            {
                args.CanExecute = true;
            }
            else
            {
                args.ContinueRouting = true;
            }
        }

        // Tab handler.
        private static void OnTabForward(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.UiScope.IsKeyboardFocused)
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            if (HandleTabInTables(This, LogicalDirection.Forward))
            {
                // All done on table level.
                return;
            }

            if (This.AcceptsRichContent && (!This.Selection.IsEmpty || TextPointerBase.IsAtParagraphOrBlockUIContainerStart(This.Selection.Start)) &&
                EditingCommands.IncreaseIndentation.CanExecute(null, (IInputElement)sender))
            {
                // In RichTextBox Tab/Shift+Tab keys work as paragraph/list indentation
                EditingCommands.IncreaseIndentation.Execute(null, (IInputElement)sender);
            }
            else
            {
                // In plain text we treat tab as a characters always
                DoTextInput(This, "\t", /*isInsertKeyToggled:*/!This._OvertypeMode, /*acceptControlCharacters:*/true);
            }
        }

        // Shift+Tab handler.
        private static void OnTabBackward(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.UiScope.IsKeyboardFocused)
            {
                return;
            }

            // Implement paragraph decrease level command
            TextEditorTyping._FlushPendingInputItems(This);

            if (HandleTabInTables(This, LogicalDirection.Backward))
            {
                // All done on table level.
                return;
            }

            if (This.AcceptsRichContent && (!This.Selection.IsEmpty || TextPointerBase.IsAtParagraphOrBlockUIContainerStart(This.Selection.Start)) &&
                EditingCommands.DecreaseIndentation.CanExecute(null, (IInputElement)sender))
            {
                // In RichTextBox Tab/Shift+Tab keys work as paragraph/list indentation
                EditingCommands.DecreaseIndentation.Execute(null, (IInputElement)sender);
            }
            else
            {
                // In plain text we treat tab as a characters always
                DoTextInput(This, "\t", /*isInsertKeyToggled:*/!This._OvertypeMode, /*acceptControlCharacters:*/true);
            }
        }

        // Command handler for Tab and ShiftTab - moves caret between table cells
        // if the selection is within a table. Otherwise does nothing and returns false.
        private static bool HandleTabInTables(TextEditor This, LogicalDirection direction)
        {
            if (!This.AcceptsRichContent)
            {
                return false;
            }

            if (This.Selection.IsTableCellRange)
            {
                // When table cell range is selected, Tab simply collapses
                // a selection to a content of a first cell
                This.Selection.SetCaretToPosition(This.Selection.Start, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
                return true;
            }

            if (This.Selection.IsEmpty && TextPointerBase.IsAtRowEnd(This.Selection.End))
            {
                // From the end of row we go to the first cell of a next row
                TableCell cell = null;
                TableRow row = ((TextPointer)This.Selection.End).Parent as TableRow;
                Invariant.Assert(row != null);
                TableRowGroup body = row.RowGroup;
                int rowIndex = body.Rows.IndexOf(row);

                if (direction == LogicalDirection.Forward)
                {
                    if (rowIndex + 1 < body.Rows.Count)
                    {
                        cell = body.Rows[rowIndex + 1].Cells[0];
                    }
                }
                else
                {
                    if (rowIndex > 0)
                    {
                        cell = body.Rows[rowIndex - 1].Cells[body.Rows[rowIndex - 1].Cells.Count - 1];
                    }
                }

                if (cell != null)
                {
                    This.Selection.Select(cell.ContentStart, cell.ContentEnd);
                }
                return true;
            }

            // Check if selection is within a table
            TextElement parent = ((TextPointer)This.Selection.Start).Parent as TextElement;
            while (parent != null && !(parent is TableCell))
            {
                parent = parent.Parent as TextElement;
            }
            if (parent is TableCell)
            {
                TableCell cell = (TableCell)parent;
                TableRow row = cell.Row;
                TableRowGroup body = row.RowGroup;

                int cellIndex = row.Cells.IndexOf(cell);
                int rowIndex = body.Rows.IndexOf(row);

                if (direction == LogicalDirection.Forward)
                {
                    if (cellIndex + 1 < row.Cells.Count)
                    {
                        cell = row.Cells[cellIndex + 1];
                    }
                    else if (rowIndex + 1 < body.Rows.Count)
                    {
                        cell = body.Rows[rowIndex + 1].Cells[0];
                    }
                    else
                    {
                        //  Add code for inserting new table row - at the end of a table
                    }
                }
                else
                {
                    if (cellIndex > 0)
                    {
                        cell = row.Cells[cellIndex - 1];
                    }
                    else if (rowIndex > 0)
                    {
                        cell = body.Rows[rowIndex - 1].Cells[body.Rows[rowIndex - 1].Cells.Count - 1];
                    }
                    else
                    {
                        //  Add code for inserting new table row - at the end of a table
                    }
                }

                Invariant.Assert(cell != null);
                This.Selection.Select(cell.ContentStart, cell.ContentEnd);
                return true;
            }

            return false;
        }

        // ......................................................
        //
        //  Handling Text Input
        //
        // ......................................................

        /// <summary>
        /// This is a single method used to insert user input characters.
        /// It takes care of typing undo, springload formatting, overtype mode etc.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="textData">
        /// Text to insert.
        /// </param>
        /// <param name="isInsertKeyToggled">
        /// Reflects a state of Insert key at the moment of textData input.
        /// </param>
        /// <param name="acceptControlCharacters">
        /// True indicates that control characters like '\t' or '\r' etc. can be inserted.
        /// False means that all control characters are filtered out.
        /// </param>
        private static void DoTextInput(TextEditor This, string textData, bool isInsertKeyToggled, bool acceptControlCharacters)
        {
            // Hide the mouse cursor on user input.
            HideCursor(This);

            // Remove control characters. Note that this is not included into _FilterText,
            // because we want such kind of filtering only for real input,
            // not for copy/paste.
            if (!acceptControlCharacters)
            {
                for (int i = 0; i < textData.Length; i++)
                {
                    if (Char.IsControl(textData[i]))
                    {
                        textData = textData.Remove(i--, 1);  // decrement i to compensate for character removal
                    }
                }
            }

            string filteredText = This._FilterText(textData, This.Selection);
            if (filteredText.Length == 0)
            {
                return;
            }

            TextEditorTyping.OpenTypingUndoUnit(This);

            UndoCloseAction closeAction = UndoCloseAction.Rollback;

            try
            {
                using (This.Selection.DeclareChangeBlock())
                {
                    This.Selection.ApplyTypingHeuristics(This.AllowOvertype && This._OvertypeMode && filteredText != "\t");

                    This.SetSelectedText(filteredText, InputLanguageManager.Current.CurrentInputLanguage);

                    // Create caret position normalized backward to keep formatting of a character just typed
                    ITextPointer caretPosition = This.Selection.End.CreatePointer(LogicalDirection.Backward);

                    // Set selection at the end of input content
                    This.Selection.SetCaretToPosition(caretPosition, LogicalDirection.Backward, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                    // Note: Using explicit backward orientation we keep formatting with
                    // a previous character during typing.

                    closeAction = UndoCloseAction.Commit;
                }
            }
            finally
            {
                TextEditorTyping.CloseTypingUndoUnit(This, closeAction);
            }
        }

        // Takes state originating with a KeyDownEvent or TextInputEvent and
        // schedules it for eventual handling.
        //
        // Normally we delay handling until a Background priority event fires.
        // This has the effect of batching multiple input events when
        // layout cannot keep up with the input stream.
        //
        // However, if any mouse events are pending, we handle the event
        // immediately, since otherwise we risk the possibility of handling
        // the events out of order.
        private static void ScheduleInput(TextEditor This, InputItem item)
        {
            if (!This.AcceptsRichContent || IsMouseInputPending(This))
            {
                // We have to do the work now, or we'll get out of synch.
                TextEditorTyping._FlushPendingInputItems(This);
                item.Do();
            }
            else
            {
                TextEditorThreadLocalStore threadLocalStore;

                threadLocalStore = TextEditor._ThreadLocalStore;

                if (threadLocalStore.PendingInputItems == null)
                {
                    threadLocalStore.PendingInputItems = new ArrayList(1);
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(BackgroundInputCallback), This);
                }

                threadLocalStore.PendingInputItems.Add(item);
            }
        }

        // Returns true if any mouse input event is currently waiting in the
        // win32 message queue for processing.
        // Avalon doesn't keep a separate queue for input events.  Instead
        // it interleaves work items with the win32 input queue.
        private static bool IsMouseInputPending(TextEditor This)
        {
            bool mouseInputPending = false;
            IWin32Window win32Window = PresentationSource.CriticalFromVisual(This.UiScope) as IWin32Window;
            if (win32Window != null)
            {
                IntPtr hwnd = IntPtr.Zero;
                hwnd = win32Window.Handle;

                if (hwnd != (IntPtr)0)
                {
                    System.Windows.Interop.MSG message = new System.Windows.Interop.MSG();
                    mouseInputPending = UnsafeNativeMethods.PeekMessage(ref message, new HandleRef(null, hwnd), WindowMessage.WM_MOUSEFIRST, WindowMessage.WM_MOUSELAST, NativeMethods.PM_NOREMOVE);
                }
            }

            return mouseInputPending;
        }

        // Background priority callback used to process keystrokes.
        private static object BackgroundInputCallback(object This)
        {
            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            Invariant.Assert(This is TextEditor);
            Invariant.Assert(threadLocalStore.PendingInputItems != null);

            try
            {
                TextEditorTyping._FlushPendingInputItems((TextEditor)This);
            }
            finally
            {
                threadLocalStore.PendingInputItems = null;
            }

            return null;
        }

        /// <summary>
        /// Callback for shutdown finished dispatcher. Before shutdown dispatcher, we should clean
        /// InputLanguageChangedEventHandler.
        /// </summary>
        private static void OnDispatcherShutdownFinished(object sender, EventArgs args)
        {
            // Remove the dispatcher shutdown finished event handler
            Dispatcher.CurrentDispatcher.ShutdownFinished -= new EventHandler(OnDispatcherShutdownFinished);

            // Remove the input language changed event handler
            InputLanguageManager.Current.InputLanguageChanged -= new InputLanguageEventHandler(OnInputLanguageChanged);

            TextEditorThreadLocalStore threadLocalStore = TextEditor._ThreadLocalStore;

            // Clear InputLanguageChangeEventHandler count
            threadLocalStore.InputLanguageChangeEventHandlerCount = 0;
        }

        // InputLanguageChanged handler.
        private static void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
        {
            TextSelection.OnInputLanguageChanged(e.NewLanguage);
        }

        // Base class for keyboard/text input items.
        // Individual keystroke/text events are batched and handled together
        // when layout cannot keep up with the input stream.
        private abstract class InputItem
        {
            // Ctor.
            internal InputItem(TextEditor textEditor)
            {
                _textEditor = textEditor;
            }

            // Handles the input event.
            internal abstract void Do();

            // The TextEditor instance on which this input item applies.
            TextEditor _textEditor;

            protected TextEditor TextEditor
            {
                get
                {
                    return _textEditor;
                }
            }
        }

        // Holds state originating from a single TextInputEvent.
        private class TextInputItem : InputItem
        {
            // Ctor.
            internal TextInputItem(TextEditor textEditor, string text, bool isInsertKeyToggled)
                : base (textEditor)
            {
                _text = text;
                _isInsertKeyToggled = isInsertKeyToggled;
            }

            // Inserts event content into the document.
            internal override void Do()
            {
                if (TextEditor.UiScope == null)
                {
                    // We dont want to process the input item if the editor has already been detached from its UiScope.
                    return;
                }

                DoTextInput(TextEditor, _text, _isInsertKeyToggled, /*acceptControlCharacters:*/false);
            }

            // Text to input.
            private readonly string _text;
            private readonly bool _isInsertKeyToggled;
        }

        // Holds state originating from a single KeyDownEvent.
        private class KeyUpInputItem : InputItem
        {
            // Ctor.
            internal KeyUpInputItem(TextEditor textEditor, Key key, ModifierKeys modifiers)
                : base(textEditor)
            {
                _key = key;
                _modifiers = modifiers;
            }

            // Fires the command associated with a keystroke.
            internal override void Do()
            {
                if (TextEditor.UiScope == null)
                {
                    // We dont want to process the input item if the editor has already been detached from its UiScope.
                    return;
                }

                // Delegate the work to specific handlers.
                switch (_key)
                {
                    case Key.RightShift:
                        // Only support RTL flow direction in case of having the installed
                        // bidi input language.
                        if (TextSelection.IsBidiInputLanguageInstalled() == true)
                        {
                            TextEditorTyping.OnFlowDirectionCommand(TextEditor, _key);
                        }
                        break;
                    case Key.LeftShift:
                        TextEditorTyping.OnFlowDirectionCommand(TextEditor, _key);
                        break;

                    default:
                        Invariant.Assert(false, "Unexpected key value!");
                        break;
                }
            }

            // Key associated with the original event.
            private readonly Key _key;

            // Modifier state when the original event fired.
            private readonly ModifierKeys _modifiers;
        }

        // ----------------------------------------------------------
        //
        // Merge Typing Undo Units
        //
        // ----------------------------------------------------------

        #region Merge Typing Undo Units

        /// <summary>
        /// The helper for typing undo unit merging.
        /// Supposed to be called in the beginning of typing block -
        /// before making any changes.
        /// Assumes that CloseTypingUndoUnit method will be called
        /// after the change is completed.
        /// </summary>
        private static void OpenTypingUndoUnit(TextEditor This)
        {
            UndoManager undoManager = This._GetUndoManager();

            if (undoManager != null && undoManager.IsEnabled)
            {
                if (This._typingUndoUnit != null && undoManager.LastUnit == This._typingUndoUnit && !This._typingUndoUnit.Locked)
                {
                    undoManager.Reopen(This._typingUndoUnit);
                }
                else
                {
                    This._typingUndoUnit = new TextParentUndoUnit(This.Selection);
                    undoManager.Open(This._typingUndoUnit);
                }
            }
        }

        /// <summary>
        /// The helper for typing undo unit megring.
        /// Supposed to be called at the end of typing block -
        /// after all changes are done.
        /// Assumes that OpenTypingUndoUnit method was called
        /// in the beginning of this sequence.
        /// </summary>
        private static void CloseTypingUndoUnit(TextEditor This, UndoCloseAction closeAction)
        {
            UndoManager undoManager = This._GetUndoManager();

            if (undoManager != null && undoManager.IsEnabled)
            {
                if (This._typingUndoUnit != null && undoManager.LastUnit == This._typingUndoUnit && !This._typingUndoUnit.Locked)
                {
                    if (This._typingUndoUnit is TextParentUndoUnit)
                    {
                        ((TextParentUndoUnit)This._typingUndoUnit).RecordRedoSelectionState();
                    }
                    undoManager.Close(This._typingUndoUnit, closeAction);
                }
            }
            else
            {
                This._typingUndoUnit = null;
            }
        }

        /// <summary>
        /// StartInputCorrection command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusNYI(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null)
            {
                return;
            }

            args.CanExecute = true;
        }

        #endregion Merge Typing Undo Units

        // MouseMoveEvent listener.
        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Un-vanish the cursor on any mouse move.
            _ShowCursor();
        }

        // MouseMoveEvent listener.
        // We only need this event because of the edge case where
        // moving the mouse from the outermost pixel of the UiScope to
        // another UIElement's real estate doesn't raise a MouseMoveEvent.
        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            // Un-vanish the cursor on any mouse leave.
            _ShowCursor();
        }

        // Hides the mouse cursor when the user starts typing.
        private static void HideCursor(TextEditor This)
        {
            if (!TextEditor._ThreadLocalStore.HideCursor &&
                SystemParameters.MouseVanish &&
                This.UiScope.IsMouseOver)
            {
                TextEditor._ThreadLocalStore.HideCursor = true;
                SafeNativeMethods.ShowCursor(false);
            }
        }

        // When the mouse cursor is over a Hyperlink, force a cursor update
        // to display the "hand" cursor appropriately.
        private static void UpdateHyperlinkCursor(TextEditor This)
        {
            if (This.UiScope is RichTextBox && This.TextView != null && This.TextView.IsValid)
            {
                TextPointer pointer = (TextPointer)This.TextView.GetTextPositionFromPoint(Mouse.GetPosition(This.TextView.RenderScope), false);

                if (pointer != null &&
                    pointer.Parent is TextElement &&
                    TextSchema.HasHyperlinkAncestor((TextElement)pointer.Parent))
                {
                    Mouse.UpdateCursor();
                }
            }
        }

        #endregion Private Methods

        private const string KeyBackspace = "Backspace";
        private const string KeyDelete = "Delete";
        private const string KeyDeleteNextWord = "Ctrl+Delete";
        private const string KeyDeletePreviousWord = "Ctrl+Backspace";
        private const string KeyEnterLineBreak = "Shift+Enter";
        private const string KeyEnterParagraphBreak = "Enter";
        private const string KeyShiftBackspace = "Shift+Backspace";
        private const string KeyShiftSpace = "Shift+Space";
        private const string KeySpace = "Space";
        private const string KeyTabBackward = "Shift+Tab";
        private const string KeyTabForward = "Tab";
        private const string KeyToggleInsert = "Insert";
    }
}
