// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A component of TextEditor supporting paragraph formating commands
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
    internal static class TextEditorParagraphs
    {
        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Registers all text editing command handlers for a given control type
        internal static void _RegisterClassHandlers(Type controlType, bool acceptsRichContent, bool registerEventListeners)
        {
            CanExecuteRoutedEventHandler onQueryStatusNYI = new CanExecuteRoutedEventHandler(OnQueryStatusNYI);


            if (acceptsRichContent)
            {
                // Editing Commands: Paragraph Editing
                // -----------------------------------
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.AlignLeft, new ExecutedRoutedEventHandler(OnAlignLeft), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyAlignLeft, SRID.KeyAlignLeftDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.AlignCenter, new ExecutedRoutedEventHandler(OnAlignCenter), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyAlignCenter, SRID.KeyAlignCenterDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.AlignRight, new ExecutedRoutedEventHandler(OnAlignRight), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyAlignRight, SRID.KeyAlignRightDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.AlignJustify, new ExecutedRoutedEventHandler(OnAlignJustify), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyAlignJustify, SRID.KeyAlignJustifyDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplySingleSpace, new ExecutedRoutedEventHandler(OnApplySingleSpace), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyApplySingleSpace, SRID.KeyApplySingleSpaceDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyOneAndAHalfSpace, new ExecutedRoutedEventHandler(OnApplyOneAndAHalfSpace), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyApplyOneAndAHalfSpace, SRID.KeyApplyOneAndAHalfSpaceDisplayString));
                CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyDoubleSpace, new ExecutedRoutedEventHandler(OnApplyDoubleSpace), onQueryStatusNYI, KeyGesture.CreateFromResourceStrings(KeyApplyDoubleSpace, SRID.KeyApplyDoubleSpaceDisplayString));
            }

            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyParagraphFlowDirectionLTR, new ExecutedRoutedEventHandler(OnApplyParagraphFlowDirectionLTR), onQueryStatusNYI);
            CommandHelpers.RegisterCommandHandler(controlType, EditingCommands.ApplyParagraphFlowDirectionRTL, new ExecutedRoutedEventHandler(OnApplyParagraphFlowDirectionRTL), onQueryStatusNYI);
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// AlignLeft command event handler.
        /// </summary>
        private static void OnAlignLeft(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            TextEditorCharacters._OnApplyProperty(This, Block.TextAlignmentProperty, TextAlignment.Left, /*applyToParagraphs*/true);
        }

        /// <summary>
        /// AlignCenter command event handler.
        /// </summary>
        private static void OnAlignCenter(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            TextEditorCharacters._OnApplyProperty(This, Block.TextAlignmentProperty, TextAlignment.Center, /*applyToParagraphs*/true);
        }

        /// <summary>
        /// AlignRight command event handler.
        /// </summary>
        private static void OnAlignRight(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            TextEditorCharacters._OnApplyProperty(This, Block.TextAlignmentProperty, TextAlignment.Right, /*applyToParagraphs*/true);
        }

        /// <summary>
        /// AlignJustify command event handler.
        /// </summary>
        private static void OnAlignJustify(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            TextEditorCharacters._OnApplyProperty(This, Block.TextAlignmentProperty, TextAlignment.Justify, /*applyToParagraphs*/true);
        }

        private static void OnApplySingleSpace(object sender, ExecutedRoutedEventArgs e)
        {
            //  Provide an implementation for this command
        }

        private static void OnApplyOneAndAHalfSpace(object sender, ExecutedRoutedEventArgs e)
        {
            //  Provide an implementation for this command
        }

        private static void OnApplyDoubleSpace(object sender, ExecutedRoutedEventArgs e)
        {
            //  Provide an implementation for this command
        }

        /// <summary>
        /// OnApplyParagraphFlowDirectionLTR command event handler.
        /// </summary>
        private static void OnApplyParagraphFlowDirectionLTR(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            TextEditorCharacters._OnApplyProperty(This, FrameworkElement.FlowDirectionProperty,
                FlowDirection.LeftToRight, /*applyToParagraphs*/true);
        }

        /// <summary>
        /// OnApplyParagraphFlowDirectionRTL command event handler.
        /// </summary>
        private static void OnApplyParagraphFlowDirectionRTL(object sender, ExecutedRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);
            TextEditorCharacters._OnApplyProperty(This, FrameworkElement.FlowDirectionProperty,
                FlowDirection.RightToLeft, /*applyToParagraphs*/true);
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
        private static void OnQueryStatusNYI(object sender, CanExecuteRoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor(sender);

            if (This == null)
            {
                return;
            }

            e.CanExecute = true;
        }

        #endregion Misceleneous Commands

        #endregion Private methods

        private const string KeyAlignCenter = "Ctrl+E";
        private const string KeyAlignJustify = "Ctrl+J";
        private const string KeyAlignLeft = "Ctrl+L";
        private const string KeyAlignRight = "Ctrl+R";
        private const string KeyApplyDoubleSpace = "Ctrl+2";
        private const string KeyApplyOneAndAHalfSpace = "Ctrl+5";
        private const string KeyApplySingleSpace = "Ctrl+1";
    }
}
