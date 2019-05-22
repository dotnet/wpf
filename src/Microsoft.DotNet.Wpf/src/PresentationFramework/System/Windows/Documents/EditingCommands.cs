// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Command definitions for Rich Text Editing.
//

namespace System.Windows.Documents
{
    using System.Windows.Input; // Command
    using System.ComponentModel; // TypeConverter

    /// <summary>
    /// Command definitions for Rich Text Editing.
    /// </summary>
    public static class EditingCommands
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        // Typing Commands
        // ---------------
        /// <summary>
        /// ToggleInsert command.
        /// Changed typing mode between insertion and overtyping.
        /// </summary>
        public static RoutedUICommand  ToggleInsert            { get { return EnsureCommand(ref _ToggleInsert           , "ToggleInsert"             ); } }

        /// <summary>
        /// Delete command.
        /// When selection is empty deletes the following character or paragraph separator.
        /// When selection is not empty deletes the selected content.
        /// Formatting of deleted content is not springloaded (unlike Backspace).
        /// </summary>
        public static RoutedUICommand  Delete                  { get { return EnsureCommand(ref _Delete                 , "Delete"                   ); } }

        /// <summary>
        /// Backspace command.
        /// When selection is empty deleted the previous character or paragraph separator.
        /// When selection is not empty deletes the selected content.
        /// Formatting of deleted content is springloaded (unlike Delete).
        /// Formatting for springloading is taken from the very first selected character.
        /// </summary>
        public static RoutedUICommand  Backspace               { get { return EnsureCommand(ref _Backspace              , "Backspace"                ); } }

        /// <summary>
        /// DeleteNextWord command.
        /// </summary>
        public static RoutedUICommand  DeleteNextWord          { get { return EnsureCommand(ref _DeleteNextWord         , "DeleteNextWord"           ); } }

        /// <summary>
        /// DeletePreviousWord command.
        /// </summary>
        public static RoutedUICommand  DeletePreviousWord      { get { return EnsureCommand(ref _DeletePreviousWord     , "DeletePreviousWord"       ); } }

        /// <summary>
        /// EnterParagraphBreak command.
        /// Acts as if the user presses Enter key. The content of current selection is deleted (if not empty)
        /// like with Backspace command(performing formatting springloading), and then
        /// the structure of text elements is changed so that paragraph break appears
        /// at caret position.
        /// </summary>
        public static RoutedUICommand  EnterParagraphBreak     { get { return EnsureCommand(ref _EnterParagraphBreak    , "EnterParagraphBreak"      ); } }

        /// <summary>
        /// EnterLineBreak command.
        /// </summary>
        public static RoutedUICommand  EnterLineBreak          { get { return EnsureCommand(ref _EnterLineBreak         , "EnterLineBreak"           ); } }

        /// <summary>
        /// TabForward command.
        /// The behavior depends from the current condition of selection.
        /// If selection is non-empty then it redirects to a IncreaseIndentation command,
        /// so that all affected paragraphs become promoted (by increasing their Margin.Left property in RichTextBox
        /// or by inserting additional Tab charater in the beginning of each non-wrapped line).
        /// If the caret is in table cell then it moves to the next cell.
        /// If the caret is in the last table cell, then in creates new row in a table and moves
        /// the caret into first cell of that row.
        /// Otherwise Tab character is inserted in current position.
        /// </summary>
        public static RoutedUICommand  TabForward              { get { return EnsureCommand(ref _TabForward             , "TabForward"               ); } }

        /// <summary>
        /// TabBackward command.
        /// The behavior depends from the current condition of selection.
        /// If selection is non-empty then it redirects to a DecreaseIndentation command,
        /// so that all affected paragraphs become promoted (by decreasing their Margin.Left property in RichTextBox
        /// or by deleting a Tab charater in the beginning of each non-wrapped line).
        /// If the caret is in table cell then it moves to the previous cell.
        /// Otherwise Tab character is inserted in current position.
        /// </summary>
        public static RoutedUICommand  TabBackward             { get { return EnsureCommand(ref _TabBackward            , "TabBackward"              ); } }

        // Caret navigation commands
        // -------------------------
        /// <summary>
        /// MoveRightByCharacter command.
        /// </summary>
        public static RoutedUICommand  MoveRightByCharacter    { get { return EnsureCommand(ref _MoveRightByCharacter   , "MoveRightByCharacter"     ); } }

        /// <summary>
        /// MoveLeftByCharacter command.
        /// </summary>
        public static RoutedUICommand  MoveLeftByCharacter     { get { return EnsureCommand(ref _MoveLeftByCharacter    , "MoveLeftByCharacter"      ); } }

        /// <summary>
        /// MoveRightByWord command.
        /// </summary>
        public static RoutedUICommand  MoveRightByWord         { get { return EnsureCommand(ref _MoveRightByWord        , "MoveRightByWord"          ); } }

        /// <summary>
        /// MoveLeftByWord command.
        /// </summary>
        public static RoutedUICommand  MoveLeftByWord          { get { return EnsureCommand(ref _MoveLeftByWord         , "MoveLeftByWord"           ); } }

        /// <summary>
        /// MoveDownByLine command.
        /// </summary>
        public static RoutedUICommand  MoveDownByLine          { get { return EnsureCommand(ref _MoveDownByLine         , "MoveDownByLine"           ); } }

        /// <summary>
        /// MoveUpByLine command.
        /// </summary>
        public static RoutedUICommand  MoveUpByLine            { get { return EnsureCommand(ref _MoveUpByLine           , "MoveUpByLine"             ); } }

        /// <summary>
        /// MoveDownByParagraph command.
        /// </summary>
        public static RoutedUICommand  MoveDownByParagraph     { get { return EnsureCommand(ref _MoveDownByParagraph    , "MoveDownByParagraph"      ); } }

        /// <summary>
        /// MoveUpByParagraph command.
        /// </summary>
        public static RoutedUICommand  MoveUpByParagraph       { get { return EnsureCommand(ref _MoveUpByParagraph      , "MoveUpByParagraph"        ); } }

        /// <summary>
        /// MoveDownByPage command.
        /// Corresponds to PgDn key on the keyboard.
        /// </summary>
        public static RoutedUICommand  MoveDownByPage          { get { return EnsureCommand(ref _MoveDownByPage         , "MoveDownByPage"             ); } }

        /// <summary>
        /// MoveUpByPage command.
        /// Corresponds to PgUp key on the keyboard.
        /// </summary>
        public static RoutedUICommand  MoveUpByPage            { get { return EnsureCommand(ref _MoveUpByPage           , "MoveUpByPage"             ); } }

        /// <summary>
        /// MoveToLineStart command.
        /// Corresponds to Home key on the keyboard.
        /// </summary>
        public static RoutedUICommand  MoveToLineStart         { get { return EnsureCommand(ref _MoveToLineStart        , "MoveToLineStart"          ); } }

        /// <summary>
        /// MoveToLineEnd command.
        /// Corresponds to End key on the keyboard.
        /// </summary>
        public static RoutedUICommand  MoveToLineEnd           { get { return EnsureCommand(ref _MoveToLineEnd          , "MoveToLineEnd"            ); } }

        /// <summary>
        /// MoveToDocumentStart command.
        /// </summary>
        public static RoutedUICommand  MoveToDocumentStart     { get { return EnsureCommand(ref _MoveToDocumentStart    , "MoveToDocumentStart"      ); } }

        /// <summary>
        /// MoveToDocumentEnd command.
        /// </summary>
        public static RoutedUICommand  MoveToDocumentEnd       { get { return EnsureCommand(ref _MoveToDocumentEnd      , "MoveToDocumentEnd"        ); } }

        // Selection extension commands
        // ----------------------------

        /// <summary>
        /// SelectRightByCharacter command.
        /// </summary>
        public static RoutedUICommand  SelectRightByCharacter  { get { return EnsureCommand(ref _SelectRightByCharacter , "SelectRightByCharacter"   ); } }

        /// <summary>
        /// SelectLeftByCharacter command.
        /// </summary>
        public static RoutedUICommand  SelectLeftByCharacter   { get { return EnsureCommand(ref _SelectLeftByCharacter  , "SelectLeftByCharacter"    ); } }

        /// <summary>
        /// SelectRightByWord command.
        /// </summary>
        public static RoutedUICommand  SelectRightByWord       { get { return EnsureCommand(ref _SelectRightByWord      , "SelectRightByWord"        ); } }

        /// <summary>
        /// SelectLeftbyWord command.
        /// </summary>
        public static RoutedUICommand  SelectLeftByWord        { get { return EnsureCommand(ref _SelectLeftByWord       , "SelectLeftByWord"         ); } }

        /// <summary>
        /// SelectDownByLine command.
        /// </summary>
        public static RoutedUICommand  SelectDownByLine        { get { return EnsureCommand(ref _SelectDownByLine       , "SelectDownByLine"         ); } }

        /// <summary>
        /// SelectUpByLine command.
        /// </summary>
        public static RoutedUICommand  SelectUpByLine          { get { return EnsureCommand(ref _SelectUpByLine         , "SelectUpByLine"           ); } }

        /// <summary>
        /// SelectDownByParagraph command.
        /// </summary>
        public static RoutedUICommand  SelectDownByParagraph   { get { return EnsureCommand(ref _SelectDownByParagraph  , "SelectDownByParagraph"    ); } }

        /// <summary>
        /// SelectUpByParagraph command.
        /// </summary>
        public static RoutedUICommand  SelectUpByParagraph     { get { return EnsureCommand(ref _SelectUpByParagraph    , "SelectUpByParagraph"      ); } }

        /// <summary>
        /// SelectDownByPage command.
        /// </summary>
        public static RoutedUICommand  SelectDownByPage        { get { return EnsureCommand(ref _SelectDownByPage       , "SelectDownByPage"         ); } }

        /// <summary>
        /// SelectUpByPage command.
        /// </summary>
        public static RoutedUICommand  SelectUpByPage          { get { return EnsureCommand(ref _SelectUpByPage         , "SelectUpByPage"           ); } }

        /// <summary>
        /// SelectToLineStart command.
        /// </summary>
        public static RoutedUICommand  SelectToLineStart       { get { return EnsureCommand(ref _SelectToLineStart      , "SelectToLineStart"        ); } }

        /// <summary>
        /// SelectToLineEnd command.
        /// </summary>
        public static RoutedUICommand  SelectToLineEnd         { get { return EnsureCommand(ref _SelectToLineEnd        , "SelectToLineEnd"          ); } }

        /// <summary>
        /// SelectToDocumentStart command.
        /// </summary>
        public static RoutedUICommand  SelectToDocumentStart   { get { return EnsureCommand(ref _SelectToDocumentStart  , "SelectToDocumentStart"    ); } }

        /// <summary>
        /// SelectToDocumentEnd command.
        /// </summary>
        public static RoutedUICommand  SelectToDocumentEnd     { get { return EnsureCommand(ref _SelectToDocumentEnd    , "SelectToDocumentEnd"      ); } }

        // Character editing commands
        // --------------------------

        /// <summary>
        /// ToggleBold command.
        /// When command argument is present applies provided value to a selected range.
        /// When command argument is null applies an value of FontWeight opposite to the one taken from the first
        /// character of selected range.
        /// When selection is empty and within a word, the same operation is applied to this word.
        /// When empty selection is between words or in the process of typing
        /// the property is springloaded.
        /// </summary>
        public static RoutedUICommand  ToggleBold              { get { return EnsureCommand(ref _ToggleBold             , "ToggleBold"               ); } }

        /// <summary>
        /// ToggleItalic command.
        /// </summary>
        public static RoutedUICommand  ToggleItalic            { get { return EnsureCommand(ref _ToggleItalic           , "ToggleItalic"             ); } }

        /// <summary>
        /// ToggleUnderline command.
        /// </summary>
        public static RoutedUICommand  ToggleUnderline         { get { return EnsureCommand(ref _ToggleUnderline        , "ToggleUnderline"          ); } }

        /// <summary>
        /// ToggleSubscript command.
        /// </summary>
        public static RoutedUICommand  ToggleSubscript         { get { return EnsureCommand(ref _ToggleSubscript        , "ToggleSubscript"          ); } }

        /// <summary>
        /// ToggleSuperscript command.
        /// </summary>
        public static RoutedUICommand  ToggleSuperscript       { get { return EnsureCommand(ref _ToggleSuperscript      , "ToggleSuperscript"        ); } }

        /// <summary>
        /// IncreaseFontSize command.
        /// </summary>
        public static RoutedUICommand  IncreaseFontSize        { get { return EnsureCommand(ref _IncreaseFontSize       , "IncreaseFontSize"         ); } }

        /// <summary>
        /// DecreaseFontSize command.
        /// </summary>
        public static RoutedUICommand  DecreaseFontSize        { get { return EnsureCommand(ref _DecreaseFontSize       , "DecreaseFontSize"         ); } }

        // Paragraph editing commands
        // --------------------------

        /// <summary>
        /// AlignLeft command.
        /// </summary>
        public static RoutedUICommand  AlignLeft               { get { return EnsureCommand(ref _AlignLeft              , "AlignLeft"                ); } }

        /// <summary>
        /// AlightCenter command.
        /// </summary>
        public static RoutedUICommand  AlignCenter             { get { return EnsureCommand(ref _AlignCenter            , "AlignCenter"              ); } }

        /// <summary>
        /// AlignRight command.
        /// </summary>
        public static RoutedUICommand  AlignRight              { get { return EnsureCommand(ref _AlignRight             , "AlignRight"               ); } }

        /// <summary>
        /// AlignJustify command.
        /// </summary>
        public static RoutedUICommand  AlignJustify            { get { return EnsureCommand(ref _AlignJustify           , "AlignJustify"             ); } }

        // List editing commands
        // ---------------------

        /// <summary>
        /// ToggelBullets command.
        /// When command argument is present it must be of ListMarkerStyle value;
        /// this value is set as a marker style to all selected list items.
        /// When command argument is null the command toggles a marker style
        /// by circle over all predefined non-numeric list marker styles.
        /// The circle includes no-list state as well.
        /// </summary>
        public static RoutedUICommand  ToggleBullets           { get { return EnsureCommand(ref _ToggleBullets          , "ToggleBullets"            ); } }

        /// <summary>
        /// ToggelNumbers command.
        /// When command argument is present it must be of ListMarkerStyle value;
        /// this value is set as a marker style to all selected list items.
        /// When command argument is null the command toggles a marker style
        /// by circle over all predefined numeric list marker styles
        /// The circle includes no-list state as well.
        /// </summary>
        public static RoutedUICommand  ToggleNumbering         { get { return EnsureCommand(ref _ToggleNumbering        , "ToggleNumbering"          ); } }

        /// <summary>
        /// IncreaseIndentation command.
        /// </summary>
        public static RoutedUICommand IncreaseIndentation { get { return EnsureCommand(ref _IncreaseIndentation, "IncreaseIndentation"); } }

        /// <summary>
        /// DecreaseIndentation command.
        /// </summary>
        public static RoutedUICommand DecreaseIndentation { get { return EnsureCommand(ref _DecreaseIndentation, "DecreaseIndentation"); } }

        // Spelling commands
        // ---------------------

        /// <summary>
        /// Corrects a misspelled word at the insertion position.
        /// </summary>
        public static RoutedUICommand CorrectSpellingError   { get { return EnsureCommand(ref _CorrectSpellingError, "CorrectSpellingError"        ); } }

        /// <summary>
        /// Ignores all instances of the misspelled word at the insertion position.
        /// </summary>
        public static RoutedUICommand IgnoreSpellingError    { get { return EnsureCommand(ref _IgnoreSpellingError, "IgnoreSpellingError"          ); } }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // NOTE: The following pieces of code must be maintained consistently
        //      f) Re-expose via reflection in Lexicon

        // Typing commands
        internal static RoutedUICommand Space                             { get { return EnsureCommand(ref _Space                                  , "Space"                                     ); } }
        internal static RoutedUICommand ShiftSpace                        { get { return EnsureCommand(ref _ShiftSpace                             , "ShiftSpace"                                ); } }

        // Caret navigation commands
        // -------------------------
        internal static RoutedUICommand MoveToColumnStart                 { get { return EnsureCommand(ref _MoveToColumnStart                      , "MoveToColumnStart"                         ); } }
        internal static RoutedUICommand MoveToColumnEnd                   { get { return EnsureCommand(ref _MoveToColumnEnd                        , "MoveToColumnEnd"                           ); } }
        internal static RoutedUICommand MoveToWindowTop                   { get { return EnsureCommand(ref _MoveToWindowTop                        , "MoveToWindowTop"                           ); } }
        internal static RoutedUICommand MoveToWindowBottom                { get { return EnsureCommand(ref _MoveToWindowBottom                     , "MoveToWindowBottom"                        ); } }

        // Selection extension commands
        // ----------------------------
        internal static RoutedUICommand SelectToColumnStart               { get { return EnsureCommand(ref _SelectToColumnStart                    , "SelectToColumnStart"                       ); } }
        internal static RoutedUICommand SelectToColumnEnd                 { get { return EnsureCommand(ref _SelectToColumnEnd                      , "SelectToColumnEnd"                         ); } }
        internal static RoutedUICommand SelectToWindowTop                 { get { return EnsureCommand(ref _SelectToWindowTop                      , "SelectToWindowTop"                         ); } }
        internal static RoutedUICommand SelectToWindowBottom              { get { return EnsureCommand(ref _SelectToWindowBottom                   , "SelectToWindowBottom"                      ); } }

        // Character editing commands
        // --------------------------
        internal static RoutedUICommand ResetFormat                       { get { return EnsureCommand(ref _ResetFormat                            , "ResetFormat"                               ); } }
        internal static RoutedUICommand ToggleSpellCheck                  { get { return EnsureCommand(ref _ToggleSpellCheck                       , "ToggleSpellCheck"                          ); } }

        // BEGIN Application Compatibility Note
        // The following commands are internal, but they are exposed publicly
        // from our command converter.  We cannot change this behavior
        // because it is well documented.  For example, in the
        // "WPF XAML Vocabulary Specification 2006" found here:
        // http://msdn.microsoft.com/en-us/library/dd361848(PROT.10).aspx
        internal static RoutedUICommand ApplyFontSize                     { get { return EnsureCommand(ref _ApplyFontSize                          , "ApplyFontSize"                             ); } }
        internal static RoutedUICommand ApplyFontFamily                   { get { return EnsureCommand(ref _ApplyFontFamily                        , "ApplyFontFamily"                           ); } }
        internal static RoutedUICommand ApplyForeground                   { get { return EnsureCommand(ref _ApplyForeground                        , "ApplyForeground"                           ); } }
        internal static RoutedUICommand ApplyBackground                   { get { return EnsureCommand(ref _ApplyBackground                        , "ApplyBackground"                           ); } }
        // END Application Compatibility Note

        internal static RoutedUICommand ApplyInlineFlowDirectionRTL       { get { return EnsureCommand(ref _ApplyInlineFlowDirectionRTL            , "ApplyInlineFlowDirectionRTL"               ); } }
        internal static RoutedUICommand ApplyInlineFlowDirectionLTR       { get { return EnsureCommand(ref _ApplyInlineFlowDirectionLTR            , "ApplyInlineFlowDirectionLTR"               ); } }

        // Paragraph editing commands
        // --------------------------
        internal static RoutedUICommand ApplySingleSpace                  { get { return EnsureCommand(ref _ApplySingleSpace                       , "ApplySingleSpace"                          ); } }
        internal static RoutedUICommand ApplyOneAndAHalfSpace             { get { return EnsureCommand(ref _ApplyOneAndAHalfSpace                  , "ApplyOneAndAHalfSpace"                     ); } }
        internal static RoutedUICommand ApplyDoubleSpace                  { get { return EnsureCommand(ref _ApplyDoubleSpace                       , "ApplyDoubleSpace"                          ); } }
        internal static RoutedUICommand ApplyParagraphFlowDirectionRTL    { get { return EnsureCommand(ref _ApplyParagraphFlowDirectionRTL         , "ApplyParagraphFlowDirectionRTL"            ); } }
        internal static RoutedUICommand ApplyParagraphFlowDirectionLTR    { get { return EnsureCommand(ref _ApplyParagraphFlowDirectionLTR         , "ApplyParagraphFlowDirectionLTR"            ); } }

        // CopyPaste Commands
        // ------------------
        internal static RoutedUICommand CopyFormat                        { get { return EnsureCommand(ref _CopyFormat                             , "CopyFormat"                                ); } }
        internal static RoutedUICommand PasteFormat                       { get { return EnsureCommand(ref _PasteFormat                            , "PasteFormat"                               ); } }

        // List editing commands
        // ---------------------
        internal static RoutedUICommand RemoveListMarkers                 { get { return EnsureCommand(ref _RemoveListMarkers                      , "RemoveListMarkers"                         ); } }

        // Table editing commands
        // ----------------------
        internal static RoutedUICommand InsertTable                       { get { return EnsureCommand(ref _InsertTable                            , "InsertTable"                               ); } }
        internal static RoutedUICommand InsertRows                        { get { return EnsureCommand(ref _InsertRows                             , "InsertRows"                                ); } }
        internal static RoutedUICommand InsertColumns                     { get { return EnsureCommand(ref _InsertColumns                          , "InsertColumns"                             ); } }
        internal static RoutedUICommand DeleteRows                        { get { return EnsureCommand(ref _DeleteRows                             , "DeleteRows"                                ); } }
        internal static RoutedUICommand DeleteColumns                     { get { return EnsureCommand(ref _DeleteColumns                          , "DeleteColumns"                             ); } }
        internal static RoutedUICommand MergeCells                        { get { return EnsureCommand(ref _MergeCells                             , "MergeCells"                                ); } }
        internal static RoutedUICommand SplitCell                         { get { return EnsureCommand(ref _SplitCell                              , "SplitCell"                                 ); } }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Initializes a static command definition - by demand
        private static RoutedUICommand EnsureCommand(ref RoutedUICommand command, string commandPropertyName)
        {
            lock (_synchronize)
            {
                if (command == null)
                {
                    // The first parameter should be localized
                    command = new RoutedUICommand(commandPropertyName, commandPropertyName, typeof(EditingCommands));
                }
            }
            return command;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private static object _synchronize = new object();

        // Input commands
        // --------------
        private static RoutedUICommand _ToggleInsert;
        private static RoutedUICommand _Delete;
        private static RoutedUICommand _Backspace;
        private static RoutedUICommand _DeleteNextWord;
        private static RoutedUICommand _DeletePreviousWord;
        private static RoutedUICommand _EnterParagraphBreak;
        private static RoutedUICommand _EnterLineBreak;
        private static RoutedUICommand _TabForward;
        private static RoutedUICommand _TabBackward;
        private static RoutedUICommand _Space;
        private static RoutedUICommand _ShiftSpace;

        // Caret navigation commands
        // -------------------------
        private static RoutedUICommand _MoveRightByCharacter;
        private static RoutedUICommand _MoveLeftByCharacter;
        private static RoutedUICommand _MoveRightByWord;
        private static RoutedUICommand _MoveLeftByWord;
        private static RoutedUICommand _MoveDownByLine;
        private static RoutedUICommand _MoveUpByLine;
        private static RoutedUICommand _MoveDownByParagraph;
        private static RoutedUICommand _MoveUpByParagraph;
        private static RoutedUICommand _MoveDownByPage;
        private static RoutedUICommand _MoveUpByPage;
        private static RoutedUICommand _MoveToLineStart;
        private static RoutedUICommand _MoveToLineEnd;
        private static RoutedUICommand _MoveToColumnStart;
        private static RoutedUICommand _MoveToColumnEnd;
        private static RoutedUICommand _MoveToWindowTop;
        private static RoutedUICommand _MoveToWindowBottom;
        private static RoutedUICommand _MoveToDocumentStart;
        private static RoutedUICommand _MoveToDocumentEnd;

        // Selection extension commands
        // ----------------------------
        private static RoutedUICommand _SelectRightByCharacter;
        private static RoutedUICommand _SelectLeftByCharacter;
        private static RoutedUICommand _SelectRightByWord;
        private static RoutedUICommand _SelectLeftByWord;
        private static RoutedUICommand _SelectDownByLine;
        private static RoutedUICommand _SelectUpByLine;
        private static RoutedUICommand _SelectDownByParagraph;
        private static RoutedUICommand _SelectUpByParagraph;
        private static RoutedUICommand _SelectDownByPage;
        private static RoutedUICommand _SelectUpByPage;
        private static RoutedUICommand _SelectToLineStart;
        private static RoutedUICommand _SelectToLineEnd;
        private static RoutedUICommand _SelectToColumnStart;
        private static RoutedUICommand _SelectToColumnEnd;
        private static RoutedUICommand _SelectToWindowTop;
        private static RoutedUICommand _SelectToWindowBottom;
        private static RoutedUICommand _SelectToDocumentStart;
        private static RoutedUICommand _SelectToDocumentEnd;

        // Character editing commands
        // --------------------------
        private static RoutedUICommand _CopyFormat;
        private static RoutedUICommand _PasteFormat;
        private static RoutedUICommand _ResetFormat;
        private static RoutedUICommand _ToggleBold;
        private static RoutedUICommand _ToggleItalic;
        private static RoutedUICommand _ToggleUnderline;
        private static RoutedUICommand _ToggleSubscript;
        private static RoutedUICommand _ToggleSuperscript;
        private static RoutedUICommand _IncreaseFontSize;
        private static RoutedUICommand _DecreaseFontSize;
        private static RoutedUICommand _ApplyFontSize;
        private static RoutedUICommand _ApplyFontFamily;
        private static RoutedUICommand _ApplyForeground;
        private static RoutedUICommand _ApplyBackground;
        private static RoutedUICommand _ToggleSpellCheck;
        private static RoutedUICommand _ApplyInlineFlowDirectionRTL;
        private static RoutedUICommand _ApplyInlineFlowDirectionLTR;

        // Paragraph editing commands
        // --------------------------
        private static RoutedUICommand _AlignLeft;
        private static RoutedUICommand _AlignCenter;
        private static RoutedUICommand _AlignRight;
        private static RoutedUICommand _AlignJustify;
        private static RoutedUICommand _ApplySingleSpace;
        private static RoutedUICommand _ApplyOneAndAHalfSpace;
        private static RoutedUICommand _ApplyDoubleSpace;
        private static RoutedUICommand _IncreaseIndentation;
        private static RoutedUICommand _DecreaseIndentation;
        private static RoutedUICommand _ApplyParagraphFlowDirectionRTL;
        private static RoutedUICommand _ApplyParagraphFlowDirectionLTR;

        // List editing commands
        // ---------------------
        private static RoutedUICommand _RemoveListMarkers;
        private static RoutedUICommand _ToggleBullets;
        private static RoutedUICommand _ToggleNumbering;

        // Table editing commands
        // ----------------------
        private static RoutedUICommand _InsertTable;
        private static RoutedUICommand _InsertRows;
        private static RoutedUICommand _InsertColumns;
        private static RoutedUICommand _DeleteRows;
        private static RoutedUICommand _DeleteColumns;
        private static RoutedUICommand _MergeCells;
        private static RoutedUICommand _SplitCell;

        // Spelling Commands
        // -----------------
        private static RoutedUICommand _CorrectSpellingError;
        private static RoutedUICommand _IgnoreSpellingError;

        #endregion Private Methods
    }
}
