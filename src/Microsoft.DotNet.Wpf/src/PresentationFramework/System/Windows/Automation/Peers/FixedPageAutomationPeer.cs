// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        override protected string GetClassNameCore()
        {
            return "FixedPage";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }
    }
}

