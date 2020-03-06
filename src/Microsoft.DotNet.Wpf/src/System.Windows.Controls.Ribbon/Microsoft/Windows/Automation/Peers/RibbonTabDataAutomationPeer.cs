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
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using Microsoft.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for the item in a RibbonTab
    /// Supports SelectionItem, ExpandCollapse and ScrollItem patterns.
    /// </summary>
    public class RibbonTabDataAutomationPeer : SelectorItemAutomationPeer, ISelectionItemProvider, IExpandCollapseProvider, IScrollItemProvider
    {
        public RibbonTabDataAutomationPeer(object item, RibbonAutomationPeer itemsControlPeer)
            : base(item, itemsControlPeer)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            object peer = base.GetPattern(patternInterface);
            
            RibbonTab wrapperTab = GetWrapper() as RibbonTab;

            if (patternInterface == PatternInterface.ExpandCollapse &&
                wrapperTab != null &&
                wrapperTab.Ribbon != null &&
                wrapperTab.Ribbon.IsMinimized)
            {
                peer = this;
            }
            
            if(patternInterface == PatternInterface.ScrollItem)
            {
                peer = this;
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

        #region IExpandCollapseProvider Members

        /// <summary>
        /// If Ribbon.IsMinimized then set Ribbon.IsDropDownOpen to false
        /// </summary>
        void IExpandCollapseProvider.Collapse()
        {
            RibbonTab wrapperTab = GetWrapper() as RibbonTab;
            if (wrapperTab != null)
            {
                Ribbon ribbon = wrapperTab.Ribbon;
                if (ribbon != null &&
                    ribbon.IsMinimized)
                {
                    ribbon.IsDropDownOpen = false;
                }   
            }
        }

        /// <summary>
        /// If Ribbon.IsMinimized then set Ribbon.IsDropDownOpen to true
        /// </summary>
        void IExpandCollapseProvider.Expand()
        {
            RibbonTab wrapperTab = GetWrapper() as RibbonTab;
            // Select the tab and display popup
            if (wrapperTab != null)
            {
                Ribbon ribbon = wrapperTab.Ribbon;
                if (ribbon != null &&
                    ribbon.IsMinimized)
                {
                    wrapperTab.IsSelected = true;
                    ribbon.IsDropDownOpen = true;
                }
            }
        }

        /// <summary>
        /// Return Ribbon.IsDropDownOpen
        /// </summary>
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get 
            {
                RibbonTab wrapperTab = GetWrapper() as RibbonTab;
                if (wrapperTab != null)
                {
                    Ribbon ribbon = wrapperTab.Ribbon;
                    if (ribbon != null &&
                        ribbon.IsMinimized)
                    {
                        if (wrapperTab.IsSelected && ribbon.IsDropDownOpen)
                        {
                            return ExpandCollapseState.Expanded;
                        }
                        else
                        {
                            return ExpandCollapseState.Collapsed;
                        }
                    }
                }

                // When not minimized
                return ExpandCollapseState.Expanded;
            }
        }

        #endregion

        #region ISelectionItemProvider Members

        /// <summary>
        /// RemoveFromSelection not allowed on currently Selected Tab. No op for other Tabs
        /// </summary>
        void ISelectionItemProvider.RemoveFromSelection()
        {
            RibbonTab tab = GetWrapper() as RibbonTab;
            if (tab != null && tab.IsSelected)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        /// <summary>
        /// AddToSelection not allowed. No op if Tab is already selected. 
        /// </summary>
        void ISelectionItemProvider.AddToSelection()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);
            if ((parentSelector == null) || (parentSelector.SelectedItem != null && parentSelector.SelectedItem != Item))
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        #endregion

        #region IScrollItemProvider Members
        
        void IScrollItemProvider.ScrollIntoView()
        {
            RibbonTab wrapperTab = GetWrapper() as RibbonTab;
            if (wrapperTab != null && wrapperTab.RibbonTabHeader != null )
            {
                wrapperTab.RibbonTabHeader.BringIntoView();
            }
        }

        #endregion

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.TabItem;
        }

        protected override string GetClassNameCore()
        {
            RibbonTabAutomationPeer wrapperPeer = GetWrapperPeer() as RibbonTabAutomationPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }
            return string.Empty;
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
