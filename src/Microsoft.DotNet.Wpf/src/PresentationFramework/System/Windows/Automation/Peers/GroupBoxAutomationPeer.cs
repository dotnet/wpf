// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    ///
    public class GroupBoxAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public GroupBoxAutomationPeer(GroupBox owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "GroupBox";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        // Return the base without the AccessKey character
        ///
        override protected string GetNameCore()
        {
            string result = base.GetNameCore();
            if (!string.IsNullOrEmpty(result))
            {
                GroupBox groupBox = (GroupBox)Owner;
                if (groupBox.Header is string)
                {
                    return AccessText.RemoveAccessKeyMarker(result);
                }
            }

            return result;
        }
    }
}

