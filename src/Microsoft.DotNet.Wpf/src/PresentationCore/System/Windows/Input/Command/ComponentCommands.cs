// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The ComponentCommands class defines a standard set of commands that are required in Controls.
//
//              See spec at : http://avalon/CoreUI/Specs%20%20Eventing%20and%20Commanding/CommandLibrarySpec.mht
//
//
//

using System;
using System.Windows;
using System.Windows.Input;
using System.Collections;
using System.ComponentModel;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// ComponentCommands - Set of Standard Commands
    /// </summary>
    public static class ComponentCommands
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Public Methods

        /// <summary>
        /// ScrollPageUp Command
        /// </summary>
        public static RoutedUICommand ScrollPageUp
        {
            get { return _EnsureCommand(CommandId.ScrollPageUp); }
        }

        /// <summary>
        /// ScrollPageDown Command
        /// </summary>
        public static RoutedUICommand ScrollPageDown
        {
            get { return _EnsureCommand(CommandId.ScrollPageDown); }
        }

        /// <summary>
        /// ScrollPageLeft Command
        /// </summary>
        public static RoutedUICommand ScrollPageLeft
        {
            get { return _EnsureCommand(CommandId.ScrollPageLeft); }
        }

        /// <summary>
        /// ScrollPageRight Command
        /// </summary>
        public static RoutedUICommand ScrollPageRight
        {
            get { return _EnsureCommand(CommandId.ScrollPageRight); }
        }

        /// <summary>
        /// ScrollByLine Command
        /// </summary>
        public static RoutedUICommand ScrollByLine
        {
            get { return _EnsureCommand(CommandId.ScrollByLine); }
        }

        /// <summary>
        /// MoveLeft Command
        /// </summary>
        public static RoutedUICommand MoveLeft
        {
            get { return _EnsureCommand(CommandId.MoveLeft); }
        }

        /// <summary>
        /// MoveRight Command
        /// </summary>
        public static RoutedUICommand MoveRight
        {
            get { return _EnsureCommand(CommandId.MoveRight); }
        }

        /// <summary>
        /// MoveUp Command
        /// </summary>
        public static RoutedUICommand MoveUp
        {
            get { return _EnsureCommand(CommandId.MoveUp); }
        }

        /// <summary>
        /// MoveDown Command
        /// </summary>
        public static RoutedUICommand MoveDown
        {
            get { return _EnsureCommand(CommandId.MoveDown); }
        }

        /// <summary>
        /// MoveToHome Command
        /// </summary>
        public static RoutedUICommand MoveToHome
        {
            get { return _EnsureCommand(CommandId.MoveToHome); }
        }

        /// <summary>
        /// MoveToEnd Command
        /// </summary>
        public static RoutedUICommand MoveToEnd
        {
            get { return _EnsureCommand(CommandId.MoveToEnd); }
        }

        /// <summary>
        /// MoveToPageUp Command
        /// </summary>
        public static RoutedUICommand MoveToPageUp
        {
            get { return _EnsureCommand(CommandId.MoveToPageUp); }
        }

        /// <summary>
        /// MoveToPageDown Command
        /// </summary>
        public static RoutedUICommand MoveToPageDown
        {
            get { return _EnsureCommand(CommandId.MoveToPageDown); }
        }

        /// <summary>
        /// Extend Selection Up Command
        /// </summary>
        public static RoutedUICommand ExtendSelectionUp
        {
            get { return _EnsureCommand(CommandId.ExtendSelectionUp); }
        }

        /// <summary>
        /// ExtendSelectionDown Command
        /// </summary>
        public static RoutedUICommand ExtendSelectionDown
        {
            get { return _EnsureCommand(CommandId.ExtendSelectionDown); }
        }

        /// <summary>
        /// ExtendSelectionLeft Command
        /// </summary>
        public static RoutedUICommand ExtendSelectionLeft
        {
            get { return _EnsureCommand(CommandId.ExtendSelectionLeft); }
        }

        /// <summary>
        /// ExtendSelectionRight Command
        /// </summary>
        public static RoutedUICommand ExtendSelectionRight
        {
            get { return _EnsureCommand(CommandId.ExtendSelectionRight); }
        }

        /// <summary>
        /// SelectToHome Command
        /// </summary>
        public static RoutedUICommand SelectToHome
        {
            get { return _EnsureCommand(CommandId.SelectToHome); }
        }

        /// <summary>
        /// SelectToEnd Command
        /// </summary>
        public static RoutedUICommand SelectToEnd
        {
            get { return _EnsureCommand(CommandId.SelectToEnd); }
        }

        /// <summary>
        /// SelectToPageUp Command
        /// </summary>
        public static RoutedUICommand SelectToPageUp
        {
            get { return _EnsureCommand(CommandId.SelectToPageUp); }
        }

        /// <summary>
        /// SelectToPageDown Command
        /// </summary>
        public static RoutedUICommand SelectToPageDown
        {
            get { return _EnsureCommand(CommandId.SelectToPageDown); }
        }

        /// <summary>
        /// MoveFocusUp Command
        /// </summary>
        public static RoutedUICommand MoveFocusUp
        {
            get { return _EnsureCommand(CommandId.MoveFocusUp); }
        }

        /// <summary>
        /// MoveFocusDown Command
        /// </summary>
        public static RoutedUICommand MoveFocusDown
        {
            get { return _EnsureCommand(CommandId.MoveFocusDown); }
        }

        /// <summary>
        /// MoveFocusForward Command
        /// </summary>
        public static RoutedUICommand MoveFocusForward
        {
            get { return _EnsureCommand(CommandId.MoveFocusForward); }
        }

        /// <summary>
        /// MoveFocusBack
        /// </summary>
        public static RoutedUICommand MoveFocusBack
        {
            get { return _EnsureCommand(CommandId.MoveFocusBack); }
        }

        /// <summary>
        /// MoveFocusPageUp Command
        /// </summary>
        public static RoutedUICommand MoveFocusPageUp
        {
            get { return _EnsureCommand(CommandId.MoveFocusPageUp); }
        }

        /// <summary>
        /// MoveFocusPageDown
        /// </summary>
        public static RoutedUICommand MoveFocusPageDown
        {
            get { return _EnsureCommand(CommandId.MoveFocusPageDown); }
        }


        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        private static string GetPropertyName(CommandId commandId)
        {
            string propertyName = String.Empty;

            switch (commandId)
            {
                case CommandId.ScrollPageUp:propertyName = "ScrollPageUp"; break;
                case CommandId.ScrollPageDown:propertyName = "ScrollPageDown"; break;
                case CommandId.ScrollPageLeft: propertyName = "ScrollPageLeft"; break;
                case CommandId.ScrollPageRight: propertyName = "ScrollPageRight"; break;
                case CommandId.ScrollByLine:propertyName = "ScrollByLine"; break;
                case CommandId.MoveLeft:propertyName = "MoveLeft";break;
                case CommandId.MoveRight:propertyName = "MoveRight";break;
                case CommandId.MoveUp:propertyName = "MoveUp"; break;
                case CommandId.MoveDown:propertyName = "MoveDown"; break;
                case CommandId.ExtendSelectionUp:propertyName = "ExtendSelectionUp"; break;
                case CommandId.ExtendSelectionDown:propertyName = "ExtendSelectionDown"; break;
                case CommandId.ExtendSelectionLeft:propertyName = "ExtendSelectionLeft"; break;
                case CommandId.ExtendSelectionRight:propertyName = "ExtendSelectionRight"; break;
                case CommandId.MoveToHome:propertyName = "MoveToHome"; break;
                case CommandId.MoveToEnd:propertyName = "MoveToEnd"; break;
                case CommandId.MoveToPageUp:propertyName = "MoveToPageUp"; break;
                case CommandId.MoveToPageDown:propertyName = "MoveToPageDown"; break;
                case CommandId.SelectToHome:propertyName = "SelectToHome"; break;
                case CommandId.SelectToEnd:propertyName = "SelectToEnd"; break;
                case CommandId.SelectToPageDown:propertyName = "SelectToPageDown"; break;
                case CommandId.SelectToPageUp:propertyName = "SelectToPageUp"; break;
                case CommandId.MoveFocusUp:propertyName = "MoveFocusUp"; break;
                case CommandId.MoveFocusDown:propertyName = "MoveFocusDown"; break;
                case CommandId.MoveFocusBack:propertyName = "MoveFocusBack"; break;
                case CommandId.MoveFocusForward:propertyName = "MoveFocusForward"; break;
                case CommandId.MoveFocusPageUp:propertyName = "MoveFocusPageUp"; break;
                case CommandId.MoveFocusPageDown:propertyName = "MoveFocusPageDown"; break;
            }
            return propertyName;
        }


        internal static string GetUIText(byte commandId)
        {
            string uiText = String.Empty;

            switch ((CommandId)commandId)
            {
                case  CommandId.ScrollPageUp: uiText = SR.Get(SRID.ScrollPageUpText); break;
                case  CommandId.ScrollPageDown: uiText = SR.Get(SRID.ScrollPageDownText); break;
                case  CommandId.ScrollPageLeft: uiText = SR.Get(SRID.ScrollPageLeftText); break;
                case  CommandId.ScrollPageRight: uiText = SR.Get(SRID.ScrollPageRightText); break;
                case  CommandId.ScrollByLine: uiText = SR.Get(SRID.ScrollByLineText); break;
                case  CommandId.MoveLeft:uiText = SR.Get(SRID.MoveLeftText);break;
                case  CommandId.MoveRight:uiText = SR.Get(SRID.MoveRightText);break;
                case  CommandId.MoveUp: uiText = SR.Get(SRID.MoveUpText); break;
                case  CommandId.MoveDown: uiText = SR.Get(SRID.MoveDownText); break;
                case  CommandId.ExtendSelectionUp: uiText = SR.Get(SRID.ExtendSelectionUpText); break;
                case  CommandId.ExtendSelectionDown: uiText = SR.Get(SRID.ExtendSelectionDownText); break;
                case  CommandId.ExtendSelectionLeft: uiText = SR.Get(SRID.ExtendSelectionLeftText); break;
                case  CommandId.ExtendSelectionRight: uiText = SR.Get(SRID.ExtendSelectionRightText); break;
                case  CommandId.MoveToHome: uiText = SR.Get(SRID.MoveToHomeText); break;
                case  CommandId.MoveToEnd: uiText = SR.Get(SRID.MoveToEndText); break;
                case  CommandId.MoveToPageUp: uiText = SR.Get(SRID.MoveToPageUpText); break;
                case  CommandId.MoveToPageDown: uiText = SR.Get(SRID.MoveToPageDownText); break;
                case  CommandId.SelectToHome: uiText = SR.Get(SRID.SelectToHomeText); break;
                case  CommandId.SelectToEnd: uiText = SR.Get(SRID.SelectToEndText); break;
                case  CommandId.SelectToPageDown: uiText = SR.Get(SRID.SelectToPageDownText); break;
                case  CommandId.SelectToPageUp: uiText = SR.Get(SRID.SelectToPageUpText); break;
                case  CommandId.MoveFocusUp: uiText = SR.Get(SRID.MoveFocusUpText); break;
                case  CommandId.MoveFocusDown: uiText = SR.Get(SRID.MoveFocusDownText); break;
                case  CommandId.MoveFocusBack: uiText = SR.Get(SRID.MoveFocusBackText); break;
                case  CommandId.MoveFocusForward: uiText = SR.Get(SRID.MoveFocusForwardText); break;
                case  CommandId.MoveFocusPageUp: uiText = SR.Get(SRID.MoveFocusPageUpText); break;
                case  CommandId.MoveFocusPageDown: uiText = SR.Get(SRID.MoveFocusPageDownText); break;
            }

            return uiText;
        }

        internal static InputGestureCollection LoadDefaultGestureFromResource(byte commandId)
        {
            InputGestureCollection gestures = new InputGestureCollection();

            //Standard Commands
            switch ((CommandId)commandId)
            {
                case  CommandId.ScrollPageUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ScrollPageUpKey,
                        SR.Get(SRID.ScrollPageUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ScrollPageDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ScrollPageDownKey,
                        SR.Get(SRID.ScrollPageDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ScrollPageLeft:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.ScrollPageLeftKey),
                        SR.Get(SRID.ScrollPageLeftKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ScrollPageRight:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.ScrollPageRightKey),
                        SR.Get(SRID.ScrollPageRightKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ScrollByLine:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.ScrollByLineKey),
                        SR.Get(SRID.ScrollByLineKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveLeft:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveLeftKey,
                        SR.Get(SRID.MoveLeftKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveRight:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveRightKey,
                        SR.Get(SRID.MoveRightKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveUpKey,
                        SR.Get(SRID.MoveUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveDownKey,
                        SR.Get(SRID.MoveDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ExtendSelectionUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ExtendSelectionUpKey,
                        SR.Get(SRID.ExtendSelectionUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ExtendSelectionDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ExtendSelectionDownKey,
                        SR.Get(SRID.ExtendSelectionDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ExtendSelectionLeft:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ExtendSelectionLeftKey,
                        SR.Get(SRID.ExtendSelectionLeftKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ExtendSelectionRight:
                    KeyGesture.AddGesturesFromResourceStrings(
                        ExtendSelectionRightKey,
                        SR.Get(SRID.ExtendSelectionRightKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveToHome:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveToHomeKey,
                        SR.Get(SRID.MoveToHomeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveToEnd:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveToEndKey,
                        SR.Get(SRID.MoveToEndKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveToPageUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveToPageUpKey,
                        SR.Get(SRID.MoveToPageUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveToPageDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveToPageDownKey,
                        SR.Get(SRID.MoveToPageDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.SelectToHome:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SelectToHomeKey,
                        SR.Get(SRID.SelectToHomeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.SelectToEnd:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SelectToEndKey,
                        SR.Get(SRID.SelectToEndKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.SelectToPageDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SelectToPageDownKey,
                        SR.Get(SRID.SelectToPageDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.SelectToPageUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SelectToPageUpKey,
                        SR.Get(SRID.SelectToPageUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusUpKey,
                        SR.Get(SRID.MoveFocusUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusDownKey,
                        SR.Get(SRID.MoveFocusDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusBack:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusBackKey,
                        SR.Get(SRID.MoveFocusBackKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusForward:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusForwardKey,
                        SR.Get(SRID.MoveFocusForwardKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusPageUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusPageUpKey,
                        SR.Get(SRID.MoveFocusPageUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MoveFocusPageDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        MoveFocusPageDownKey,
                        SR.Get(SRID.MoveFocusPageDownKeyDisplayString),
                        gestures);
                    break;
            }
            return gestures;
        }

        private static RoutedUICommand _EnsureCommand(CommandId idCommand)
        {
            if (idCommand >= 0 && idCommand < CommandId.Last)
            {
                lock (_internalCommands.SyncRoot)
                {
                    if (_internalCommands[(int)idCommand] == null)
                    {
                        RoutedUICommand newCommand = new RoutedUICommand(GetPropertyName(idCommand), typeof(ComponentCommands), (byte)idCommand);
                        newCommand.AreInputGesturesDelayLoaded = true;
                        _internalCommands[(int)idCommand] = newCommand;
                    }
                }
                return _internalCommands[(int)idCommand];
            }
            return null;
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
        // these constants will go away in future, its just to index into the right one.
        private enum CommandId : byte
        {
            // Formatting
            ScrollPageUp = 1,
            ScrollPageDown = 2,
            ScrollPageLeft = 3,
            ScrollPageRight = 4,
            ScrollByLine = 5,
            MoveLeft = 6,
            MoveRight = 7,
            MoveUp = 8,
            MoveDown = 9,
            MoveToHome = 10,
            MoveToEnd = 11,
            MoveToPageUp = 12,
            MoveToPageDown = 13,
            SelectToHome = 14,
            SelectToEnd = 15,
            SelectToPageUp = 16,
            SelectToPageDown = 17,
            MoveFocusUp = 18,
            MoveFocusDown = 19,
            MoveFocusForward = 20,
            MoveFocusBack = 21,
            MoveFocusPageUp = 22,
            MoveFocusPageDown = 23,
            ExtendSelectionLeft = 24,
            ExtendSelectionRight = 25,
            ExtendSelectionUp = 26,
            ExtendSelectionDown = 27,

            // Last
            Last = 28
        }

        private static RoutedUICommand[] _internalCommands = new RoutedUICommand[(int)CommandId.Last];
        #endregion Private Fields

        private const string ExtendSelectionDownKey = "Shift+Down";
        private const string ExtendSelectionLeftKey = "Shift+Left";
        private const string ExtendSelectionRightKey = "Shift+Right";
        private const string ExtendSelectionUpKey = "Shift+Up";
        private const string MoveDownKey = "Down";
        private const string MoveFocusBackKey = "Ctrl+Left";
        private const string MoveFocusDownKey = "Ctrl+Down";
        private const string MoveFocusForwardKey = "Ctrl+Right";
        private const string MoveFocusPageDownKey = "Ctrl+PageDown";
        private const string MoveFocusPageUpKey = "Ctrl+PageUp";
        private const string MoveFocusUpKey = "Ctrl+Up";
        private const string MoveLeftKey = "Left";
        private const string MoveRightKey = "Right";
        private const string MoveToEndKey = "End";
        private const string MoveToHomeKey = "Home";
        private const string MoveToPageDownKey = "PageDown";
        private const string MoveToPageUpKey = "PageUp";
        private const string MoveUpKey = "Up";
        private const string ScrollPageDownKey = "PageDown";
        private const string ScrollPageUpKey = "PageUp";
        private const string SelectToEndKey = "Shift+End";
        private const string SelectToHomeKey = "Shift+Home";
        private const string SelectToPageDownKey = "Shift+PageDown";
        private const string SelectToPageUpKey = "Shift+PageUp";
    }
}
