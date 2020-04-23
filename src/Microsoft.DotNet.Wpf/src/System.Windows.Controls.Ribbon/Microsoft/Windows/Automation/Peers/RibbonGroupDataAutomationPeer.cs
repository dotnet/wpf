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

    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Collections.Generic;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
    using System.Windows;
    using System.Windows.Controls;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for the item in a RibbonGroup.
    /// Supports ScrollItem and ExpandCollapse Patterns.
    /// </summary>
    public class RibbonGroupDataAutomationPeer : ItemAutomationPeer, IScrollItemProvider, IExpandCollapseProvider
    {
        public RibbonGroupDataAutomationPeer(object item, RibbonTabAutomationPeer itemsControlPeer)
            : base(item, itemsControlPeer)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            // In .net4 ItemAutomationPeer implements VirtualizedItemPattern, then we would need to call base.GetPattern here.
            object peer = null;

            if (patternInterface == PatternInterface.ScrollItem)
            {
                peer = this;
            }
            else if (patternInterface == PatternInterface.ExpandCollapse)
            {
                // only if RibbonGroup is Collapsed this Pattern applies.
                RibbonGroup wrapperGroup = GetWrapper() as RibbonGroup;
                if (wrapperGroup != null && wrapperGroup.IsCollapsed)
                {
                    peer = this;
                }
            }

            if (peer == null)
            {
                AutomationPeer wrapperPeer = GetWrapperPeer();
                if (wrapperPeer != null)
                {
                    peer = wrapperPeer.GetPattern(patternInterface);
                }
            }
            return peer;
        }
        

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
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

        #region IScrollItemProvider Members

        void IScrollItemProvider.ScrollIntoView()
        {
            RibbonGroup wrapper = GetWrapper() as RibbonGroup;
            if (wrapper != null)
            {
                wrapper.BringIntoView();
            }
        }

        #endregion


        #region IExpandCollapseProvider Members

        /// <summary>
        /// Close Popup
        /// </summary>
        void IExpandCollapseProvider.Collapse()
        {
            RibbonGroup wrapperGroup = GetWrapper() as RibbonGroup;
            if (wrapperGroup != null && wrapperGroup.IsCollapsed)
            {
                wrapperGroup.IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// Open popup
        /// </summary>
        void IExpandCollapseProvider.Expand()
        {
            RibbonGroup wrapperGroup = GetWrapper() as RibbonGroup;
            if (wrapperGroup != null && wrapperGroup.IsCollapsed)
            {
                wrapperGroup.IsDropDownOpen = true;
            }
        }

        /// <summary>
        /// Return IsDropDownOpen
        /// </summary>
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                RibbonGroup wrapperGroup = GetWrapper() as RibbonGroup;
                if (wrapperGroup != null && wrapperGroup.IsCollapsed)
                {
                    return wrapperGroup.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
                }

                return ExpandCollapseState.LeafNode;
            }
        }

        #endregion 

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

