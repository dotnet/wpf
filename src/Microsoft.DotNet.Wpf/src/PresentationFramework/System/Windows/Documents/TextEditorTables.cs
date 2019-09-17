// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A Component of TextEditor supporting Table editing commands
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
    internal static class TextEditorTables
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
            var onTableCommand = new ExecutedRoutedEventHandler(OnTableCommand);
            var onQueryStatusNYI = new CanExecuteRoutedEventHandler(OnQueryStatusNYI);
            
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.InsertTable   , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyInsertTable, SRID.KeyInsertTableDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.InsertRows    , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyInsertRows, SRID.KeyInsertRowsDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.InsertColumns , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyInsertColumns, SRID.KeyInsertColumnsDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DeleteRows    , onTableCommand, onQueryStatusNYI, SRID.KeyDeleteRows, SRID.KeyDeleteRowsDisplayString);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.DeleteColumns , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyDeleteColumns, SRID.KeyDeleteColumnsDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.MergeCells    , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyMergeCells, SRID.KeyMergeCellsDisplayString));
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.SplitCell     , onTableCommand, onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeySplitCell, SRID.KeySplitCellDisplayString));
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static void OnTableCommand(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor(target);

            if (This == null || !This._IsEnabled || This.IsReadOnly || !This.AcceptsRichContent || !(This.Selection is TextSelection))
            {
                return;
            }

            TextEditorTyping._FlushPendingInputItems(This);

            // Forget previously suggested horizontal position
            TextEditorSelection._ClearSuggestedX(This);

            // Execute the command
            if (args.Command == EditingCommands.InsertTable)
            {
                ((TextSelection)This.Selection).InsertTable(/*rowCount:*/4, /*columnCount:*/4);
            }
            else if (args.Command == EditingCommands.InsertRows)
            {
                ((TextSelection)This.Selection).InsertRows(+1);
            }
            else if (args.Command == EditingCommands.InsertColumns)
            {
                ((TextSelection)This.Selection).InsertColumns(+1);
            }
            else if (args.Command == EditingCommands.DeleteRows)
            {
                ((TextSelection)This.Selection).DeleteRows();
            }
            else if (args.Command == EditingCommands.DeleteColumns)
            {
                ((TextSelection)This.Selection).DeleteColumns();
            }
            else if (args.Command == EditingCommands.MergeCells)
            {
                ((TextSelection)This.Selection).MergeCells();
            }
            else if (args.Command == EditingCommands.SplitCell)
            {
                ((TextSelection)This.Selection).SplitCell(1000, 1000); // Split all ways to possible maximum
            }
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

        #endregion Private methods

        private const string KeyDeleteColumns = "Alt+Ctrl+Shift+D";
        private const string KeyInsertColumns = "Alt+Ctrl+Shift+C";
        private const string KeyInsertRows = "Alt+Ctrl+Shift+R";
        private const string KeyInsertTable = "Alt+Ctrl+Shift+T";
        private const string KeyMergeCells = "Alt+Ctrl+Shift+M";
        private const string KeySplitCell = "Alt+Ctrl+Shift+S";
    }
}
