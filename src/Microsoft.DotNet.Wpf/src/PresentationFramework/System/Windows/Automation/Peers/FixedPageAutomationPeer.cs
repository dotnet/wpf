// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Documents;

namespace System.Windows.Automation.Peers
{
    /// 
    public class FixedPageAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public FixedPageAutomationPeer(FixedPage owner): base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "FixedPage";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }
    }
}

