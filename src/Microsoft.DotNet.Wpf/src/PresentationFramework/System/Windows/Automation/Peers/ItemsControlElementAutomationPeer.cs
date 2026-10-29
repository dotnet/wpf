// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    // this class is a brother of ItemsControlItemAutomationPeer,
    internal class ItemsControlElementAutomationPeer : ItemAutomationPeer
    {
        private AutomationPeer _elementAutomationPeer;

        public ItemsControlElementAutomationPeer(UIElement element, AutomationPeer peer, ItemsControlWrapperAutomationPeer parent)
            : base(element, parent)
        {
            _elementAutomationPeer = peer;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return _elementAutomationPeer.GetAutomationControlType();
        }

        protected override string GetClassNameCore()
        {
            return _elementAutomationPeer.GetClassName();
        }
    }
}
