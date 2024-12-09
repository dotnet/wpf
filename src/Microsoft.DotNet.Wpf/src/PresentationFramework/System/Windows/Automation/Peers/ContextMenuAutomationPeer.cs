// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ContextMenuAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public ContextMenuAutomationPeer(ContextMenu owner) : base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ContextMenu";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
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

