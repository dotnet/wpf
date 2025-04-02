// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ToolBarAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public ToolBarAutomationPeer(ToolBar owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "ToolBar";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ToolBar;
        }
    }
}


