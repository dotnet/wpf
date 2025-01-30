// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class InkCanvasAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public InkCanvasAutomationPeer(InkCanvas owner)
            : base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "InkCanvas";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }
    }
}

