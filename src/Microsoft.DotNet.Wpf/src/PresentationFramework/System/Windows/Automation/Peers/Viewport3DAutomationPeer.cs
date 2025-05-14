// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class Viewport3DAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public Viewport3DAutomationPeer(Viewport3D owner)
            : base(owner)
        { }

        ///
        protected override string GetClassNameCore()
        {
            return "Viewport3D";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }
    }
}
