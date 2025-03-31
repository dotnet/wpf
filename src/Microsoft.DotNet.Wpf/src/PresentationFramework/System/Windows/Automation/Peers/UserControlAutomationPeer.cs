// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class UserControlAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public UserControlAutomationPeer(UserControl owner) : base(owner)
        { }

        ///
        override protected string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }
    }
}


