// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Automation.Peers
{
    ///
    public abstract class ButtonBaseAutomationPeer: FrameworkElementAutomationPeer 
    {
        ///
        protected ButtonBaseAutomationPeer(ButtonBase owner): base(owner)
        {}

        ///
        protected override string GetAcceleratorKeyCore()
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
        protected override string GetNameCore()
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


