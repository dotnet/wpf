// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        override protected string GetClassNameCore()
        {
            return "StatusBarItem";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }
    }
}



