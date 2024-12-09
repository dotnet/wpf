// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class InkPresenterAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public InkPresenterAutomationPeer(InkPresenter owner)
            : base(owner)
        { }

        ///
        protected override string GetClassNameCore()
        {
            return "InkPresenter";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }
    }
}

