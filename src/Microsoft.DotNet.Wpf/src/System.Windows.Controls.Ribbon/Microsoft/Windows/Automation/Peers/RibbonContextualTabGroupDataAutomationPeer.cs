// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{

    #region Using declarations

    using System;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows;
    using System.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for the item in a RibbonContextualTabGroup.
    /// Supports Invoke Pattern.
    /// </summary>
    public class RibbonContextualTabGroupDataAutomationPeer : ItemAutomationPeer, IInvokeProvider
    {
        public RibbonContextualTabGroupDataAutomationPeer(object item, RibbonContextualTabGroupItemsControlAutomationPeer owner)
            : base(item, owner)
        {
        }

        #region IInvokeProvider Members
        /// <summary>
        /// Selects the first tab in the ContextualTabGroup
        /// </summary>
        void IInvokeProvider.Invoke()
        {
            RibbonContextualTabGroup group = GetWrapper() as RibbonContextualTabGroup;
            // Select the first Tab
            if (group != null && group.Ribbon != null)
            {
                group.Ribbon.NotifyMouseClickedOnContextualTabGroup(group);
            }
        }

        #endregion

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Header;
        }

        // AutomationControlType.Header must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms753110.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }

        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }
            return string.Empty;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
                return this;
            return null;
        }
		
#if !RIBBON_IN_FRAMEWORK
        #region Private methods

        private UIElement GetWrapper()
        {
            UIElement wrapper = null;
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            if (itemsControlAutomationPeer != null)
            {
                ItemsControl owner = (ItemsControl)(itemsControlAutomationPeer.Owner);
                if (owner != null)
                {
                    wrapper = owner.ItemContainerGenerator.ContainerFromItem(Item) as UIElement;
                }
            }
            return wrapper;
        }

        private AutomationPeer GetWrapperPeer()
        {
            AutomationPeer wrapperPeer = null;
            UIElement wrapper = GetWrapper();
            if (wrapper != null)
            {
                wrapperPeer = UIElementAutomationPeer.CreatePeerForElement(wrapper);
                if (wrapperPeer == null)
                {
                    if (wrapper is FrameworkElement)
                        wrapperPeer = new FrameworkElementAutomationPeer((FrameworkElement)wrapper);
                    else
                        wrapperPeer = new UIElementAutomationPeer(wrapper);
                }
            }

            return wrapperPeer;
        }

        #endregion
#endif

    }
}
