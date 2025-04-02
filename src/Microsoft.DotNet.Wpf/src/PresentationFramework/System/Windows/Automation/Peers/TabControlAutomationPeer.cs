// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    ///
    public class TabControlAutomationPeer : SelectorAutomationPeer, ISelectionProvider
    {
        ///
        public TabControlAutomationPeer(TabControl owner): base(owner)
        {}

        ///
        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new TabItemAutomationPeer(item, this);
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tab;
        }

        ///
        override protected string GetClassNameCore()
        {
            return "TabControl";
        }

        ///
        protected override Point GetClickablePointCore()
        {
            return new Point(double.NaN, double.NaN);
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return true;
            }
        }
    }
}



