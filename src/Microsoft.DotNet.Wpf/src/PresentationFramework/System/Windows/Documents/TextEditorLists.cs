// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A Component of TextEditor supporting list editing commands.
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
    internal static class TextEditorLists
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
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.RemoveListMarkers   , new ExecutedRoutedEventHandler(OnListCommand) , new CanExecuteRoutedEventHandler(OnQueryStatusNYI), KeyGesture.CreateFromResourceStrings(KeyRemoveListMarkers, SRID.KeyRemoveListMarkersDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleBullets       , new ExecutedRoutedEventHandler(OnListCommand) , new CanExecuteRoutedEventHandler(OnQueryStatusNYI), KeyGesture.CreateFromResourceStrings(KeyToggleBullets, SRID.KeyToggleBulletsDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ToggleNumbering     , new ExecutedRoutedEventHandler(OnListCommand) , new CanExecuteRoutedEventHandler(OnQueryStatusNYI), KeyGesture.CreateFromResourceStrings(KeyToggleNumbering, SRID.KeyToggleNumberingDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.IncreaseIndentation , new ExecutedRoutedEventHandler(OnListCommand) , new CanExecuteRoutedEventHandler(OnQueryStatusTab), KeyGesture.CreateFromResourceStrings(KeyIncreaseIndentation, SRID.KeyIncreaseIndentationDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DecreaseIndentation , new ExecutedRoutedEventHandler(OnListCommand) , new CanExecuteRoutedEventHandler(OnQueryStatusTab), KeyGesture.CreateFromResourceStrings(KeyDecreaseIndentation, SRID.KeyDecreaseIndentationDisplayString));
        }

        // Decreases the indent level of the Block at selection start.
        internal static void DecreaseIndentation(TextEditor This)
        {
            TextSelection thisSelection = (TextSelection)This.Selection;

            ListItem parentListItem = TextPointerBase.GetListItem(thisSelection.Start);
            ListItem immediateListItem = TextPointerBase.GetImmediateListItem(thisSelection.Start);

            DecreaseIndentation(thisSelection, parentListItem, immediateListItem);
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Class Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static TextEditor IsEnabledNotReadOnlyIsTextSegment(object sender)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            if (This != null && This._IsEnabled && !This.IsReadOnly && !This.Selection.IsTableCellRange)
            {
                return This;
            }

            return null;
        }

        /// <summary>
        /// Increase/DcreaseIndentation command QueryStatus handler
        /// </summary>
        private static void OnQueryStatusTab(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = IsEnabledNotReadOnlyIsTextSegment(sender);
            if (This != null && This.AcceptsTab)
            {
                //  Checking for AcceptsTab does not reasonable here,
                // but because this command is tied to Tab/Shift+Tab we have to do that
                // or otherwise AcceptsTab property is ignored for TextBoxes.
                args.CanExecute= true;
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

        // Common handler for all list editing commands
        private static void OnListCommand(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            if (!TextRangeEditLists.IsListOperationApplicable((TextSelection)This.Selection))
            {
                return;
            }

            using (This.Selection.DeclareChangeBlock())
            {
                TextSelection thisSelection = (TextSelection)This.Selection;

                ListItem parentListItem = TextPointerBase.GetListItem(thisSelection.Start);
                ListItem immediateListItem = TextPointerBase.GetImmediateListItem(thisSelection.Start);
                List list = parentListItem == null ? null : (List)parentListItem.Parent;

                // Forget previously suggested horizontal position
                TextEditorSelection._ClearSuggestedX(This);

                // Execute the command
                if (args.Command == EditingCommands.ToggleBullets)
                {
                    ToggleBullets(thisSelection, parentListItem, immediateListItem, list);
                }
                else if (args.Command == EditingCommands.ToggleNumbering)
                {
                    ToggleNumbering(thisSelection, parentListItem, immediateListItem, list);
                }
                else if (args.Command == EditingCommands.RemoveListMarkers)
                {
                    TextRangeEditLists.ConvertListItemsToParagraphs(thisSelection);
                }
                else if (args.Command == EditingCommands.IncreaseIndentation)
                {
                    IncreaseIndentation(thisSelection, parentListItem, immediateListItem);
                }
                else if (args.Command == EditingCommands.DecreaseIndentation)
                {
                    DecreaseIndentation(thisSelection, parentListItem, immediateListItem);
                }
                else
                {
                    Invariant.Assert(false);
                }
            }
        }

        private static void ToggleBullets(TextSelection thisSelection, ListItem parentListItem, ListItem immediateListItem, List list)
        {
            if (immediateListItem != null && HasBulletMarker(list))
            {
                if (list.Parent is ListItem)
                {
                    TextRangeEditLists.UnindentListItems(thisSelection);
                    TextRangeEditLists.ConvertListItemsToParagraphs(thisSelection);
                }
                else
                {
                    TextRangeEditLists.UnindentListItems(thisSelection);
                }
            }
            else if (immediateListItem != null)
            {
                list.MarkerStyle = TextMarkerStyle.Disc;
            }
            else if (parentListItem != null)
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Disc);
                TextRangeEditLists.IndentListItems(thisSelection);
            }
            else
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Disc);
            }
        }

        private static void ToggleNumbering(TextSelection thisSelection, ListItem parentListItem, ListItem immediateListItem, List list)
        {
            if (immediateListItem != null && HasNumericMarker(list))
            {
                if (list.Parent is ListItem)
                {
                    TextRangeEditLists.UnindentListItems(thisSelection);
                    TextRangeEditLists.ConvertListItemsToParagraphs(thisSelection);
                }
                else
                {
                    TextRangeEditLists.UnindentListItems(thisSelection);
                }
            }
            else if (immediateListItem != null)
            {
                list.MarkerStyle = TextMarkerStyle.Decimal;
            }
            else if (parentListItem != null)
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Decimal);
                TextRangeEditLists.IndentListItems(thisSelection);
            }
            else
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Decimal);
            }
        }

        private static void IncreaseIndentation(TextSelection thisSelection, ListItem parentListItem, ListItem immediateListItem)
        {
            if (immediateListItem != null)
            {
                TextRangeEditLists.IndentListItems(thisSelection);
            }
            else if (parentListItem != null)
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Decimal);
                TextRangeEditLists.IndentListItems(thisSelection);
            }
            else
            {
                if (thisSelection.IsEmpty)
                {
                    // When selection is empty, handle indentation based on current TextIndent property of the paragraph.
                    Block paragraphOrBlockUIContainer = thisSelection.Start.ParagraphOrBlockUIContainer;
                    if (paragraphOrBlockUIContainer is BlockUIContainer)
                    {
                        // Increment BlockUIContainer's leading margin.
                        TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.IncreaseByAbsoluteValue);
                    }
                    else
                    {
                        // Create implicit paragraph if at a potential paragraph position, such as empty FlowDocument, TableCell.
                        CreateImplicitParagraphIfNeededAndUpdateSelection(thisSelection);

                        Paragraph paragraph = thisSelection.Start.Paragraph;
                        Invariant.Assert(paragraph != null, "EnsureInsertionPosition must guarantee a position in text content");

                        if (paragraph.TextIndent < 0)
                        {
                            // Reset text indent to 0.
                            TextRangeEdit.SetParagraphProperty(thisSelection.Start, thisSelection.End, Paragraph.TextIndentProperty, 0.0, PropertyValueAction.SetValue);
                        }
                        else if (paragraph.TextIndent < 20)
                        {
                            // Reset text indent to 20.
                            TextRangeEdit.SetParagraphProperty(thisSelection.Start, thisSelection.End, Paragraph.TextIndentProperty, 20.0, PropertyValueAction.SetValue);
                        }
                        else
                        {
                            // Increment paragraph leading margin.
                            TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.IncreaseByAbsoluteValue);
                        }
                    }
                }
                else
                {
                    // For non-empty selection, always increment paragraph margin.
                    TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.IncreaseByAbsoluteValue);
                }
            }
        }

        private static void DecreaseIndentation(TextSelection thisSelection, ListItem parentListItem, ListItem immediateListItem)
        {
            if (immediateListItem != null)
            {
                TextRangeEditLists.UnindentListItems(thisSelection);
            }
            else if (parentListItem != null)
            {
                TextRangeEditLists.ConvertParagraphsToListItems(thisSelection, TextMarkerStyle.Disc);
                TextRangeEditLists.UnindentListItems(thisSelection);
            }
            else
            {
                if (thisSelection.IsEmpty)
                {
                    // When selection is empty, handle indentation based on current TextIndent property of the paragraph.
                    Block paragraphOrBlockUIContainer = thisSelection.Start.ParagraphOrBlockUIContainer;
                    if (paragraphOrBlockUIContainer is BlockUIContainer)
                    {
                        // Decrement BlockUIContainer's leading margin.
                        TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.DecreaseByAbsoluteValue);
                    }
                    else
                    {
                        // Create implicit paragraph if at a potential paragraph position, such as empty FlowDocument, TableCell.
                        CreateImplicitParagraphIfNeededAndUpdateSelection(thisSelection);

                        Paragraph paragraph = thisSelection.Start.Paragraph;
                        Invariant.Assert(paragraph != null, "EnsureInsertionPosition must guarantee a position in text content");

                        // When selection is empty, handle indentation based on current TextIndent property of the paragraph.
                        if (paragraph.TextIndent > 20)
                        {
                            // Reset text indent to 20.
                            TextRangeEdit.SetParagraphProperty(thisSelection.Start, thisSelection.End, Paragraph.TextIndentProperty, 20.0, PropertyValueAction.SetValue);
                        }
                        else if (paragraph.TextIndent > 0)
                        {
                            // Reset text indent to 0.
                            TextRangeEdit.SetParagraphProperty(thisSelection.Start, thisSelection.End, Paragraph.TextIndentProperty, 0.0, PropertyValueAction.SetValue);
                        }
                        else
                        {
                            // Decrement paragraph leading margin.
                            TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.DecreaseByAbsoluteValue);
                        }
                    }
                }
                else
                {
                    // For non-empty selection, always decrement paragraph margin.
                    TextRangeEdit.IncrementParagraphLeadingMargin(thisSelection, /*increment:*/20, PropertyValueAction.DecreaseByAbsoluteValue);
                }
            }
        }

        private static void CreateImplicitParagraphIfNeededAndUpdateSelection(TextSelection thisSelection)
        {
            // Create implicit paragraph if we are at a potential paragraph position, such as empty FlowDocument, TableCell.
            TextPointer position = thisSelection.Start;
            if (TextPointerBase.IsAtPotentialParagraphPosition(position))
            {
                position = TextRangeEditTables.EnsureInsertionPosition(position);
                thisSelection.Select(position, position);
            }
        }

        private static bool HasBulletMarker(List list)
        {
            if (list == null)
            {
                return false;
            }

            TextMarkerStyle markerStyle = list.MarkerStyle;
            return TextMarkerStyle.Disc <= markerStyle && markerStyle <= TextMarkerStyle.Box;
        }

        private static bool HasNumericMarker(List list)
        {
            if (list == null)
            {
                return false;
            }

            TextMarkerStyle markerStyle = list.MarkerStyle;
            return TextMarkerStyle.LowerRoman <= markerStyle && markerStyle <= TextMarkerStyle.Decimal;
        }

        #endregion Private methods

        private const string KeyDecreaseIndentation = "Ctrl+Shift+T";
        private const string KeyToggleBullets = "Ctrl+Shift+L";
        private const string KeyToggleNumbering = "Ctrl+Shift+N"; 
        private const string KeyRemoveListMarkers = "Ctrl+Shift+R";
        private const string KeyIncreaseIndentation = "Ctrl+T";
    }
}
