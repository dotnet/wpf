// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A component of TextEditor supporting character formatting commands
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
    using System.Windows.Markup;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal static class TextEditorCharacters
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
            var onQueryStatusNYI = new CanExecuteRoutedEventHandler(OnQueryStatusNYI);

            // Editing Commands: Character Editing
            // -----------------------------------
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ResetFormat                  , new ExecutedRoutedEventHandler(OnResetFormat)       , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyResetFormat, SRID.KeyResetFormatDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleBold                   , new ExecutedRoutedEventHandler(OnToggleBold)        , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyToggleBold, SRID.KeyToggleBoldDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleItalic                 , new ExecutedRoutedEventHandler(OnToggleItalic)      , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyToggleItalic, SRID.KeyToggleItalicDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleUnderline              , new ExecutedRoutedEventHandler(OnToggleUnderline)   , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyToggleUnderline, SRID.KeyToggleUnderlineDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleSubscript              , new ExecutedRoutedEventHandler(OnToggleSubscript)   , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyToggleSubscript, SRID.KeyToggleSubscriptDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleSuperscript            , new ExecutedRoutedEventHandler(OnToggleSuperscript) , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyToggleSuperscript, SRID.KeyToggleSuperscriptDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.IncreaseFontSize             , new ExecutedRoutedEventHandler(OnIncreaseFontSize)  , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyIncreaseFontSize, SRID.KeyIncreaseFontSizeDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DecreaseFontSize             , new ExecutedRoutedEventHandler(OnDecreaseFontSize)  , onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyDecreaseFontSize, SRID.KeyDecreaseFontSizeDisplayString));

            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyFontSize                , new ExecutedRoutedEventHandler(OnApplyFontSize)     , onQueryStatusNYI, SRID.KeyApplyFontSize, SRID.KeyApplyFontSizeDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyFontFamily              , new ExecutedRoutedEventHandler(OnApplyFontFamily)   , onQueryStatusNYI, SRID.KeyApplyFontFamily, SRID.KeyApplyFontFamilyDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyForeground              , new ExecutedRoutedEventHandler(OnApplyForeground)   , onQueryStatusNYI, SRID.KeyApplyForeground, SRID.KeyApplyForegroundDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyBackground              , new ExecutedRoutedEventHandler(OnApplyBackground)   , onQueryStatusNYI, SRID.KeyApplyBackground, SRID.KeyApplyBackgroundDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleSpellCheck             , new ExecutedRoutedEventHandler(OnToggleSpellCheck)  , onQueryStatusNYI, SRID.KeyToggleSpellCheck, SRID.KeyToggleSpellCheckDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyInlineFlowDirectionRTL  , new ExecutedRoutedEventHandler(OnApplyInlineFlowDirectionRTL), new CanExecuteRoutedEventHandler(OnQueryStatusNYI));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyInlineFlowDirectionLTR  , new ExecutedRoutedEventHandler(OnApplyInlineFlowDirectionLTR), new CanExecuteRoutedEventHandler(OnQueryStatusNYI));
        }

        // A common method for all formatting commands.
        // Applies a property to current selection.
        // Takes care of toggling operations (like bold/italic).
        // Creates undo unit for this action.
        internal static void _OnApplyProperty(TextEditor This, DependencyProperty formattingProperty, object propertyValue)
        {
            _OnApplyProperty(This, formattingProperty, propertyValue, /*applyToParagraphs*/false, PropertyValueAction.SetValue);
        }

        internal static void _OnApplyProperty(TextEditor This, DependencyProperty formattingProperty, object propertyValue, bool applyToParagraphs)
        {
            _OnApplyProperty(This, formattingProperty, propertyValue, applyToParagraphs, PropertyValueAction.SetValue);
        }

        internal static void _OnApplyProperty(TextEditor This, DependencyProperty formattingProperty, object propertyValue, bool applyToParagraphs, PropertyValueAction propertyValueAction)
        {
            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            // Check whether the property is known
            if (!TextSchema.IsParagraphProperty(formattingProperty) && !TextSchema.IsCharacterProperty(formattingProperty))
            {
                Invariant.Assert(false, "The property '" + formattingProperty.Name + "' is unknown to TextEditor");
                return;
            }

            TextSelection selection = (TextSelection)This.Selection;

            if (TextSchema.IsStructuralCharacterProperty(formattingProperty) &&
                !TextRangeEdit.CanApplyStructuralInlineProperty(selection.Start, selection.End))
            {
                // Ignore structural commands fires in inappropriate context.
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // Forget previously suggested horizontal position
            TextEditorSelection._ClearSuggestedX(This);

            // Break merged typing sequence
            TextEditorTyping._BreakTypingSequence(This);

            // Apply property
            selection.ApplyPropertyValue(formattingProperty, propertyValue, applyToParagraphs, propertyValueAction);
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
        // Editing Commands: Character Editing
        //
        // ................................................................

        private static void OnResetFormat(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection.Start is TextPointer))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            using (This.Selection.DeclareChangeBlock())
            {
                // Positions to clear all inline formatting properties
                TextPointer startResetFormatPosition = (TextPointer)This.Selection.Start;
                TextPointer endResetFormatPosition = (TextPointer)This.Selection.End;

                if (This.Selection.IsEmpty)
                {
                    TextSegment autoWordRange = TextRangeBase.GetAutoWord(This.Selection);
                    if (autoWordRange.IsNull)
                    {
                        // Clear springloaded formatting
                        ((TextSelection)This.Selection).ClearSpringloadFormatting();
                        return;
                    }
                    else
                    {
                        // If we have a word, apply reset format to it
                        startResetFormatPosition = (TextPointer)autoWordRange.Start;
                        endResetFormatPosition = (TextPointer)autoWordRange.End;
                    }
                }

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Clear all inline formattings
                TextRangeEdit.CharacterResetFormatting(startResetFormatPosition, endResetFormatPosition);
            }
        }

        /// <summary>
        /// ToggleBold command event handler.
        /// </summary>
        private static void OnToggleBold(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            object propertyValue = ((TextSelection)This.Selection).GetCurrentValue(TextElement.FontWeightProperty);
            FontWeight fontWeight = (propertyValue != DependencyProperty.UnsetValue && (FontWeight)propertyValue == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold;

            TextEditorCharacters._OnApplyProperty(This, TextElement.FontWeightProperty, fontWeight);
        }

        /// <summary>
        /// ToggleItalic command event handler.
        /// </summary>
        private static void OnToggleItalic(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            object propertyValue = ((TextSelection)This.Selection).GetCurrentValue(TextElement.FontStyleProperty);
            FontStyle fontStyle = (propertyValue != DependencyProperty.UnsetValue && (FontStyle)propertyValue == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic;

            TextEditorCharacters._OnApplyProperty(This, TextElement.FontStyleProperty, fontStyle);

            // Update the caret to show it as italic or normal caret.
            This.Selection.RefreshCaret();
        }

        /// <summary>
        /// ToggleUnderline command event handler.
        /// </summary>
        private static void OnToggleUnderline(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            object propertyValue = ((TextSelection)This.Selection).GetCurrentValue(Inline.TextDecorationsProperty);
            TextDecorationCollection textDecorations = propertyValue != DependencyProperty.UnsetValue ? (TextDecorationCollection)propertyValue : null;

            TextDecorationCollection toggledTextDecorations; 
            if (!TextSchema.HasTextDecorations(textDecorations))
            {
                toggledTextDecorations = TextDecorations.Underline;
            }
            else if (!textDecorations.TryRemove(TextDecorations.Underline, out toggledTextDecorations))
            {
                // TextDecorations.Underline was not present, so add it 
                toggledTextDecorations.Add(TextDecorations.Underline);
            }

            TextEditorCharacters._OnApplyProperty(This, Inline.TextDecorationsProperty, toggledTextDecorations);
        }

        // Command handler for Ctrl+"+" key (non-numpad)
        private static void OnToggleSubscript(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            FontVariants fontVariants = (FontVariants)((TextSelection)This.Selection).GetCurrentValue(Typography.VariantsProperty);

            fontVariants = fontVariants == FontVariants.Subscript ? FontVariants.Normal : FontVariants.Subscript;

            TextEditorCharacters._OnApplyProperty(This, Typography.VariantsProperty, fontVariants);
        }

        // Command handler fro Ctrl+Shift+"+" (non-numpad)
        private static void OnToggleSuperscript(object sender, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            FontVariants fontVariants = (FontVariants)((TextSelection)This.Selection).GetCurrentValue(Typography.VariantsProperty);

            fontVariants = fontVariants == FontVariants.Superscript ? FontVariants.Normal : FontVariants.Superscript;

            TextEditorCharacters._OnApplyProperty(This, Typography.VariantsProperty, fontVariants);
        }

        // Used in IncreaseFontSize and DecreaseFontSize commands
        internal const double OneFontPoint = 72.0 / 96.0;

        // The limiting constant is taken from Word UI - it suggests to choose font size from a range between 1 and 1638.
        //  avalon may have its own limits though
        internal const double MaxFontPoint = 1638.0;

        /// <summary>
        /// IncreaseFontSize command event handler
        /// </summary>
        private static void OnIncreaseFontSize(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            if (This.Selection.IsEmpty)
            {
                // Springload an increased font size
                double fontSize = (double)((TextSelection)This.Selection).GetCurrentValue(TextElement.FontSizeProperty);
                if (fontSize == 0.0)
                {
                    return; // no characters available for font operation
                }

                if (fontSize < TextEditorCharacters.MaxFontPoint)
                {
                    fontSize += TextEditorCharacters.OneFontPoint;
                    if (fontSize > TextEditorCharacters.MaxFontPoint)
                    {
                        fontSize = TextEditorCharacters.MaxFontPoint;
                    }

                    // The limiting constant is taken from Word UI - it suggests to choose font size from a range between 1 and 1638.
                    TextEditorCharacters._OnApplyProperty(This, TextElement.FontSizeProperty, fontSize);
                }
            }
            else
            {
                // Apply font size in incremental mode to a nonempty selection
                TextEditorCharacters._OnApplyProperty(This, TextElement.FontSizeProperty, OneFontPoint, /*applyToParagraphs:*/false, PropertyValueAction.IncreaseByAbsoluteValue);
            }
        }

        /// <summary>
        /// DecreaseFontSize command event handler
        /// </summary>
        private static void OnDecreaseFontSize(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            if (This.Selection.IsEmpty)
            {
                // Springload a decreased font size
                double fontSize = (double)((TextSelection)This.Selection).GetCurrentValue(TextElement.FontSizeProperty);
                if (fontSize == 0.0)
                {
                    return; // no characters available for font operation
                }

                if (fontSize > TextEditorCharacters.OneFontPoint)
                {
                    fontSize -= TextEditorCharacters.OneFontPoint;
                    if (fontSize < TextEditorCharacters.OneFontPoint)
                    {
                        fontSize = TextEditorCharacters.OneFontPoint;
                    }

                    TextEditorCharacters._OnApplyProperty(This, TextElement.FontSizeProperty, fontSize);
                }
            }
            else
            {
                // Apply font size in decremental mode to a nonempty selection
                TextEditorCharacters._OnApplyProperty(This, TextElement.FontSizeProperty, OneFontPoint, /*applyToParagraphs:*/false, PropertyValueAction.DecreaseByAbsoluteValue);
            }
        }

        /// <summary>
        /// ApplyFontSize command event handler.
        /// </summary>
        private static void OnApplyFontSize(object target, ExecutedRoutedEventArgs args)
        {
            if (args.Parameter == null)
            {
                return; // Ignore the command if no argument provided
            }
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, TextElement.FontSizeProperty, args.Parameter);
        }

        /// <summary>
        /// ApplyFontFamily command event handler.
        /// </summary>
        private static void OnApplyFontFamily(object target, ExecutedRoutedEventArgs args)
        {
            if (args.Parameter == null)
            {
                return; // Ignore the command if no argument provided
            }
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, TextElement.FontFamilyProperty, args.Parameter);
        }

        /// <summary>
        /// ApplyForeground command event handler.
        /// </summary>
        private static void OnApplyForeground(object target, ExecutedRoutedEventArgs args)
        {
            if (args.Parameter == null)
            {
                return; // Ignore the command if no argument provided
            }
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, TextElement.ForegroundProperty, args.Parameter);
        }

        /// <summary>
        /// ApplyBackground command event handler.
        /// </summary>
        private static void OnApplyBackground(object target, ExecutedRoutedEventArgs args)
        {
            if (args.Parameter == null)
            {
                return; // Ignore the command if no argument provided
            }
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, TextElement.BackgroundProperty, args.Parameter);
        }

        /// <summary>
        /// ToggleSpellCheck command event handler.
        /// </summary>
        private static void OnToggleSpellCheck(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly)
            {
                return;
            }

            This.IsSpellCheckEnabled = !This.IsSpellCheckEnabled;
        }

        /// <summary>
        /// ApplyInlineFlowDirectionRTL command event handler.
        /// </summary>
        private static void OnApplyInlineFlowDirectionRTL(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, Inline.FlowDirectionProperty, FlowDirection.RightToLeft);
        }

        /// <summary>
        /// ApplyInlineFlowDirectionLTR command event handler.
        /// </summary>
        private static void OnApplyInlineFlowDirectionLTR(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);
            TextEditorCharacters._OnApplyProperty(This, Inline.FlowDirectionProperty, FlowDirection.LeftToRight);
        }

        // ----------------------------------------------------------
        //
        // Misceleneous Commands
        //
        // ----------------------------------------------------------

        #region Misceleneous Commands

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

        #endregion Misceleneous Commands

        #endregion Private Methods
      
        private const string KeyDecreaseFontSize = "Ctrl+OemOpenBrackets";
        private const string KeyIncreaseFontSize = "Ctrl+OemCloseBrackets";
        private const string KeyResetFormat = "Ctrl+Space";
        private const string KeyToggleBold = "Ctrl+B";
        private const string KeyToggleItalic = "Ctrl+I";
        private const string KeyToggleSubscript = "Ctrl+OemPlus";
        private const string KeyToggleSuperscript = "Ctrl+Shift+OemPlus";
        private const string KeyToggleUnderline = "Ctrl+U";
    }
}
