// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    // work around (ItemsControl.GroupStyle doesn't show items in groups in the UIAutomation tree)
    // this class should be public
    internal class ItemsControlWrapperAutomationPeer : ItemsControlAutomationPeer
    {
        public ItemsControlWrapperAutomationPeer(ItemsControl owner)
            : base(owner)
        { }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            if (item is UIElement element)
            {
                // Some UIElements have their own automation peers, so we need to check for that.
                var peer = element.CreateAutomationPeer();
                if (peer is not null)
                {
                    return new ItemsControlElementAutomationPeer(element, peer, this);
                }

                // Some other UIElements don't have their own automation peers, so we treat them as ItemsControlItems.
            }

            // If the item is not a UIElement, or if it is a UIElement that doesn't have its own automation peer,
            // we create an ItemsControlItemAutomationPeer for it.
            return new ItemsControlItemAutomationPeer(item, this);
        }

        protected override string GetClassNameCore()
        {
            return "ItemsControl";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }
    }
}

