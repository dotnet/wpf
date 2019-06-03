// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
   ///
   public abstract class ButtonBaseAutomationPeer: FrameworkElementAutomationPeer 
    {
        ///
        protected ButtonBaseAutomationPeer(ButtonBase owner): base(owner)
        {}

        ///
        override protected string GetAcceleratorKeyCore()
        {
            string acceleratorKey = base.GetAcceleratorKeyCore();
            if (acceleratorKey == string.Empty)
            {
                RoutedUICommand uiCommand = ((ButtonBase)Owner).Command as RoutedUICommand;
                if (uiCommand != null && !string.IsNullOrEmpty(uiCommand.Text))
                {
                    acceleratorKey = uiCommand.Text;
                }
            }
            return acceleratorKey;
        }

        /// 
        protected override string GetAutomationIdCore()
        {
           string result = base.GetAutomationIdCore();
           if (string.IsNullOrEmpty(result))
           {
               ButtonBase owner = (ButtonBase)Owner;
               RoutedCommand command = owner.Command as RoutedCommand;
               if (command != null)
               {
                   string commandName = command.Name;
                   if (!string.IsNullOrEmpty(commandName))
                   {
                       result = commandName;
                   }
               }
           }
           return result ?? string.Empty;
        }


        // Return the base without the AccessKey character
        ///
        override protected string GetNameCore()
        {
            string result = base.GetNameCore();
            ButtonBase bb = (ButtonBase)Owner;
            if (!string.IsNullOrEmpty(result))
            {
                if (bb.Content is string)
                {
                    result = AccessText.RemoveAccessKeyMarker(result);
                }
            }
            else
            {
                RoutedUICommand uiCommand = bb.Command as RoutedUICommand;
                if (uiCommand != null && !string.IsNullOrEmpty(uiCommand.Text))
                {
                    result = uiCommand.Text;
                }
            }
            return result;
        }
    }
}


