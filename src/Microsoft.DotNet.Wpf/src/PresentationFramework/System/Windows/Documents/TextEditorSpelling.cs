// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: A Component of TextEditor supporting spelling.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Windows;
    using System.Windows.Input;
    using MS.Internal.Commands;
    using System.Windows.Controls;
    using System.Windows.Markup; // XmlLanguage

    // A Component of TextEditor supporting spelling.
    internal static class TextEditorSpelling
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
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.CorrectSpellingError, new ExecutedRoutedEventHandler(OnCorrectSpellingError), new CanExecuteRoutedEventHandler(OnQueryStatusSpellingError));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.IgnoreSpellingError, new ExecutedRoutedEventHandler(OnIgnoreSpellingError), new CanExecuteRoutedEventHandler(OnQueryStatusSpellingError));
        }

        // Worker for TextBox/RichTextBox.GetSpellingErrorAtPosition.
        internal static SpellingError GetSpellingErrorAtPosition(TextEditor This, ITextPointer position, LogicalDirection direction)
        {
            return (This.Speller == null) ? null : This.Speller.GetError(position, direction, true /* forceEvaluation */);
        }

        // Returns the error (if any) at the current selection.
        internal static SpellingError GetSpellingErrorAtSelection(TextEditor This)
        {
            if (This.Speller == null)
            {
                return null;
            }

            if (IsSelectionIgnoringErrors(This.Selection))
            {
                // Some selection (large ones in particular) ignore errors.
                return null;
            }

            // If the selection is empty, we want to respect its direction
            // when poking around for spelling errors.
            // If it's non-empty, the selection start direction is always
            // backward, which is the opposite of what we want.
            LogicalDirection direction = This.Selection.IsEmpty ? This.Selection.Start.LogicalDirection : LogicalDirection.Forward;

            char character;
            ITextPointer position = GetNextTextPosition(This.Selection.Start, null /* limit */, direction, out character);
            if (position == null)
            {
                // There is no next character -- flip direction.
                // This is the end-of-document or end-of-paragraph case.
                direction = (direction == LogicalDirection.Forward) ? LogicalDirection.Backward : LogicalDirection.Forward;
                position = GetNextTextPosition(This.Selection.Start, null /* limit */, direction, out character);
            }
            else if (Char.IsWhiteSpace(character))
            {
                // If direction points to whitespace
                //   If the selection is empty
                //     Look in the opposite direction.
                //   Else
                //     If the selection contains non-white space
                //       Look at the first non-white space character forward.
                //     Else
                //       Look in the opposite direction.
                if (This.Selection.IsEmpty)
                {
                    direction = (direction == LogicalDirection.Forward) ? LogicalDirection.Backward : LogicalDirection.Forward;
                    position = GetNextTextPosition(This.Selection.Start, null /* limit */, direction, out character);
                }
                else
                {
                    direction = LogicalDirection.Forward;
                    position = GetNextNonWhiteSpacePosition(This.Selection.Start, This.Selection.End);
                    if (position == null)
                    {
                        direction = LogicalDirection.Backward;
                        position = GetNextTextPosition(This.Selection.Start, null /* limit */, direction, out character);
                    }
                }
            }

            return (position == null) ? null : This.Speller.GetError(position, direction, false /* forceEvaluation */);
        }

        // Worker for TextBox/RichTextBox.GetNextSpellingErrorPosition.
        internal static ITextPointer GetNextSpellingErrorPosition(TextEditor This, ITextPointer position, LogicalDirection direction)
        {
            return (This.Speller == null) ? null : This.Speller.GetNextSpellingErrorPosition(position, direction);
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Callback for EditingCommands.CorrectSpellingError.
        //
        // Corrects the error pointed to by Selection.Start with the string
        // specified in args.Data.
        private static void OnCorrectSpellingError(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);
            if (This == null)
                return;

            string correctedText = args.Parameter as string;
            if (correctedText == null)
                return;

            SpellingError spellingError = GetSpellingErrorAtSelection(This);
            if (spellingError == null)
                return;

            using (This.Selection.DeclareChangeBlock())
            {
                ITextPointer textStart;
                ITextPointer textEnd;
                bool dontUseRange = IsErrorAtNonMergeableInlineEdge(spellingError, out textStart, out textEnd);

                ITextPointer caretPosition;

                if (dontUseRange && textStart is TextPointer)
                {
                    // We need a cast because ITextPointer's equivalent to DeleteTextInRun (DeleteContentToPostiion)
                    // will remove empty TextElements, which we do not want.
                    ((TextPointer)textStart).DeleteTextInRun(textStart.GetOffsetToPosition(textEnd));
                    textStart.InsertTextInRun(correctedText);
                    caretPosition = textStart.CreatePointer(+correctedText.Length, LogicalDirection.Forward);
                }
                else
                {
                    This.Selection.Select(spellingError.Start, spellingError.End);

                    // Setting range.Text to correctedText might inadvertantly apply previous Run's formatting properties.
                    // Save current formatting to avoid this.
                    if (This.AcceptsRichContent)
                    {
                        ((TextSelection)This.Selection).SpringloadCurrentFormatting();
                    }

                    // TextEditor.SetSelectedText() replaces current selection with new text and
                    // also applies any springloaded properties to the text.
                    XmlLanguage language = (XmlLanguage)spellingError.Start.GetValue(FrameworkElement.LanguageProperty);
                    This.SetSelectedText(correctedText, language.GetSpecificCulture());

                    caretPosition = This.Selection.End;
                }

                // Collapse the selection to a caret following the new text.
                This.Selection.Select(caretPosition, caretPosition);
            }
        }

        // Returns true when one or both ends of the error lies at the inner edge of non-mergeable inline
        // such as Hyperlink.  In this case, a TextRange will normalize its ends outside
        // the scope of the inline, and the corrected text will not be covered by it.
        //
        // We work around the common case, when the error is contained within a single
        // Run.  In more complex cases we'll fail and fall back to using a TextRange.
        private static bool IsErrorAtNonMergeableInlineEdge(SpellingError spellingError, out ITextPointer textStart, out ITextPointer textEnd)
        {
            bool result = false;

            textStart = spellingError.Start.CreatePointer(LogicalDirection.Backward);
            while (textStart.CompareTo(spellingError.End) < 0 &&
                   textStart.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                textStart.MoveToNextContextPosition(LogicalDirection.Forward);
            }
            textEnd = spellingError.End.CreatePointer();
            while (textEnd.CompareTo(spellingError.Start) > 0 &&
                   textEnd.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.Text)
            {
                textEnd.MoveToNextContextPosition(LogicalDirection.Backward);
            }

            if (textStart.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text ||
                textStart.CompareTo(spellingError.End) == 0)
            {
                return false;
            }
            Invariant.Assert(textEnd.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text &&
                             textEnd.CompareTo(spellingError.Start) != 0);

            if (TextPointerBase.IsAtNonMergeableInlineStart(textStart) ||
                TextPointerBase.IsAtNonMergeableInlineEnd(textEnd))
            {
                if (typeof(Run).IsAssignableFrom(textStart.ParentType) &&
                    textStart.HasEqualScope(textEnd))
                {
                    result = true;
                }
            }

            return result;
        }

        // Callback for EditingCommands.IgnoreSpellingError.
        //
        // Ignores the error pointed to by Selection.Start and all other
        // duplicates for the lifetime of the TextEditor.
        private static void OnIgnoreSpellingError(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);
            if (This == null)
                return;

            SpellingError spellingError = GetSpellingErrorAtSelection(This);
            if (spellingError == null)
                return;

            spellingError.IgnoreAll();
        }

        // Callback for EditingCommands.CorrectSpellingError and EditingCommands.IgnoreSpellingError
        // QueryEnabled events.
        //
        // Both commands are enabled if Selection.Start currently points to a spelling error.
        private static void OnQueryStatusSpellingError(object target, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);
            if (This == null)
                return;

            SpellingError spellingError = GetSpellingErrorAtSelection(This);

            args.CanExecute = (spellingError != null);
        }

        // Returns the position preceeding the next text character in a specified
        // direction, or null if no such position exists.
        // The scan will halt if limit is encounted; limit may be null.
        private static ITextPointer GetNextTextPosition(ITextPointer position, ITextPointer limit, LogicalDirection direction, out char character)
        {
            bool foundText = false;

            character = (char)0;

            while (position != null &&
                   !foundText &&
                   (limit == null || position.CompareTo(limit) < 0))
            {
                switch (position.GetPointerContext(direction))
                {
                    case TextPointerContext.Text:
                        char[] buffer = new char[1];
                        position.GetTextInRun(direction, buffer, 0, 1);
                        character = buffer[0];
                        foundText = true;
                        break;

                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        if (TextSchema.IsFormattingType(position.GetElementType(direction)))
                        {
                            position = position.CreatePointer(+1);
                        }
                        else
                        {
                            position = null;
                        }
                        break;

                    case TextPointerContext.EmbeddedElement:
                    case TextPointerContext.None:
                    default:
                        position = null;
                        break;
                }
            }

            return position;
        }

        // Returns the next non-white space character in the forward direction
        // from position, or null if no such position exists.
        // The return value will equal position if position is immediately followed
        // by a non-whitespace char.
        //
        // This method expects that limit is never null.  The scan will halt if
        // limit is encountered.
        private static ITextPointer GetNextNonWhiteSpacePosition(ITextPointer position, ITextPointer limit)
        {
            char character;

            Invariant.Assert(limit != null);

            while (true)
            {
                if (position.CompareTo(limit) == 0)
                {
                    position = null;
                    break;
                }

                position = GetNextTextPosition(position, limit, LogicalDirection.Forward, out character);

                if (position == null)
                    break;

                if (!Char.IsWhiteSpace(character))
                    break;

                position = position.CreatePointer(+1);
            };

            return position;
        }

        // Returns true if an ITextSelection isn't in a state where we want
        // to acknowledge spelling errors.
        private static bool IsSelectionIgnoringErrors(ITextSelection selection)
        {
            bool isSelectionIgnoringErrors = false;

            // If the selection spans more than a single Block, ignore spelling errors.
            if (selection.Start is TextPointer)
            {
                isSelectionIgnoringErrors = ((TextPointer)selection.Start).ParentBlock != ((TextPointer)selection.End).ParentBlock;
            }

            // If the selection is large, ignore spelling errors.
            if (!isSelectionIgnoringErrors)
            {
                isSelectionIgnoringErrors = selection.Start.GetOffsetToPosition(selection.End) >= 256;
            }

            // If the selection contains unicode line breaks, ignore spelling errors.
            if (!isSelectionIgnoringErrors)
            {
                string text = selection.Text;

                for (int i = 0; i < text.Length && !isSelectionIgnoringErrors; i++)
                {
                    isSelectionIgnoringErrors = TextPointerBase.IsCharUnicodeNewLine(text[i]);
                }
            }

            return isSelectionIgnoringErrors;
        }

        #endregion Private Methods
    }
}
