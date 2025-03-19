// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    /// 
    public class StatusBarItemAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public StatusBarItemAutomationPeer(StatusBarItem owner): base(owner)
        {
        }

        ///
        protected override string GetClassNameCore()
        {
            return "StatusBarItem";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }
    }
}



