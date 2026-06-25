// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    ///
    public class ThumbAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public ThumbAutomationPeer(Thumb owner): base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "Thumb";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Thumb;
        }

        // AutomationControlType.Thumb must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms742539.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }
    }
}

