// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ListBoxAutomationPeer: SelectorAutomationPeer 
    {
        ///
        public ListBoxAutomationPeer(ListBox owner): base(owner)
        {}

        ///
        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new ListBoxItemAutomationPeer(item, this);
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ListBox";
        }
    }
}


