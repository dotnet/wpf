// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The ItemsCommands class defines a standard set of commands that act on collection of items.
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

namespace System.Windows.Input
{
    /// <summary>
    /// ItemsCommands - Set of Standard Commands
    /// </summary>
    public static class ItemsCommands
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
#region Public Methods

        /// <summary>
        /// Play Command
        /// </summary>
        public static RoutedUICommand Sort
        {
            get { return _EnsureCommand(CommandId.Sort); }
        }

        /// <summary>
        /// Pause Command
        /// </summary>
        public static RoutedUICommand Group
        {
            get { return _EnsureCommand(CommandId.Group); }
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
                case CommandId.Sort : propertyName = nameof(Sort); break;
                case CommandId.Group: propertyName = nameof(Group); break;
        }
            return propertyName;
        }

        internal static string GetUIText(byte commandId)
        {
            string uiText = String.Empty;

            switch ((CommandId)commandId)
            {
                case  CommandId.Sort: uiText = SR.ItemsSortText; break;
                case  CommandId.Group: uiText = SR.ItemsGroupText; break;
            }
            return uiText;
        }

        internal static InputGestureCollection LoadDefaultGestureFromResource(byte commandId)
        {
            InputGestureCollection gestures = new InputGestureCollection();

            //Standard Commands
            switch ((CommandId)commandId)
            {
                case  CommandId.Sort:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.ItemsSortKey,
                        SR.ItemsSortKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Group:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.ItemsGroupKey,
                        SR.ItemsGroupKeyDisplayString,
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
                        RoutedUICommand newCommand = new RoutedUICommand(GetPropertyName(idCommand), typeof(ItemsCommands), (byte)idCommand);
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
            Sort = 1,
            Group = 2,

            // Last
            Last = 3
        }

        private static RoutedUICommand[] _internalCommands = new RoutedUICommand[(int)CommandId.Last];
        #endregion Private Fields
    }
}
