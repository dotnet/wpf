// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

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

