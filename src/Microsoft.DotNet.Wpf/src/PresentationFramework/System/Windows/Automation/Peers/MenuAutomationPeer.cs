// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class MenuAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public MenuAutomationPeer(Menu owner): base(owner)
        {
        }
    
        ///
        protected override string GetClassNameCore()
        {
            return "Menu";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Menu;
        }

        // AutomationControlType.Menu must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms741841.aspx.
        protected override bool IsContentElementCore()
        {
            return false;
        }
    }
}

