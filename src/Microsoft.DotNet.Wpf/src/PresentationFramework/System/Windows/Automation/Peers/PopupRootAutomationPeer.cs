// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    /// 
    internal class PopupRootAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public PopupRootAutomationPeer(PopupRoot owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "Popup";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Window;
        }
    }
}

