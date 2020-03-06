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
    using System.Collections.Generic;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Diagnostics;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for Ribbon
    /// Supports Selection, ExpandCollapse and Scroll Patterns
    /// </summary>
    public class RibbonAutomationPeer : SelectorAutomationPeer, IExpandCollapseProvider, ISelectionProvider
    {
        public RibbonAutomationPeer(Ribbon owner)
            : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tab;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (OwningRibbon.IsCollapsed)
            {
                return null;
            }

            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            // ItemsControlAutomationPeer implements IScrollProvider which is supposed to scroll through TabHeaders
            if (patternInterface == PatternInterface.Scroll)
            {
                ItemsControl tabHeadersItemsControl = OwningRibbon.RibbonTabHeaderItemsControl;
                if (tabHeadersItemsControl != null)
                {
                    AutomationPeer tabHeadersItemsControlPeer = UIElementAutomationPeer.CreatePeerForElement(tabHeadersItemsControl);
                    if (tabHeadersItemsControlPeer != null)
                    {
                        return tabHeadersItemsControlPeer.GetPattern(patternInterface);
                    }
                }
            }
            return base.GetPattern(patternInterface);
        }

        protected override System.Collections.Generic.List<AutomationPeer> GetChildrenCore()
        {
            // If Ribbon is Collapsed, dont show anything in the UIA tree
            if (OwningRibbon.IsCollapsed)
            {
                return null;
            }

            List<AutomationPeer> children = new List<AutomationPeer>();

            // Step1: Add QAT + Title + ContextualTabGroupHeaderItemsControl
            if (OwningRibbon.QuickAccessToolBar != null)
            {
                AutomationPeer peer = CreatePeerForElement(OwningRibbon.QuickAccessToolBar);
                if (peer != null)
                {
                    children.Add(peer);
                }
            }
            
            if (OwningRibbon.TitleHost != null)
            {
                AutomationPeer peer = CreatePeerForElement(OwningRibbon.TitleHost);
                if (peer == null)
                {
                    FrameworkElement titleHost = OwningRibbon.TitleHost as FrameworkElement;
                    if (titleHost != null)
                        peer = new RibbonTitleAutomationPeer(titleHost);
                    else
                        peer = new UIElementAutomationPeer(OwningRibbon.TitleHost);
                }
                children.Add(peer);
            }
            
            if (OwningRibbon.ContextualTabGroupItemsControl != null)
            {
                AutomationPeer peer = CreatePeerForElement(OwningRibbon.ContextualTabGroupItemsControl);
                if (peer != null)
                {
                    children.Add(peer);
                }
            }

            // Step2: Add ApplicationMenu
            if (OwningRibbon.ApplicationMenu != null)
            {
                AutomationPeer peer = CreatePeerForElement(OwningRibbon.ApplicationMenu);
                if (peer != null)
                {
                    children.Add(peer);
                }
            }

            // Step3: Refresh RibbonTabHeaders
            // RibbonTabHeaderItemsControl doesnt appear in the UIA Tree, but its children RibbonTabHeader appear as children of RibbonTab
            // We need to ensure that RibbonTabHeader peers are created and refreshed manually here
            if (OwningRibbon.RibbonTabHeaderItemsControl != null)
            {
#if RIBBON_IN_FRAMEWORK
                AutomationPeer peer = CreatePeerForElement(OwningRibbon.RibbonTabHeaderItemsControl);
                if (peer != null)
                {
                    peer.ForceEnsureChildren();
                }
#else
                // We are unable to use this commented piece of code because ForceEnsureChildren 
                // is an internal method in .Net 4.0. The public alternative is to use 
                // AutomationPeer.ResetChildrenCache. However this methods isn't appropriate to be 
                // called during a GetChildrenCore call because it causes a recursive call to the 
                // GetChildren call for the current AutomationPeer which is prohibited. For 
                // illustration here's a callstack.

                // Exception:System.InvalidOperationException: Recursive call to Automation Peer API is not valid. at 
                // System.Windows.Automation.Peers.AutomationPeer.GetChildren() at 
                // System.Windows.Automation.Peers.AutomationPeer.isDescendantOf(AutomationPeer parent) at 
                // System.Windows.Automation.Peers.AutomationPeer.isDescendantOf(AutomationPeer parent) at 
                // System.Windows.Automation.Peers.AutomationPeer.ValidateConnected(AutomationPeer connectedPeer) at 
                // MS.Internal.Automation.ElementProxy.StaticWrap(AutomationPeer peer, AutomationPeer referencePeer) at 
                // System.Windows.Automation.Peers.AutomationPeer.UpdateChildrenInternal(Int32 invalidateLimit) at 
                // System.Windows.Automation.Peers.ItemsControlAutomationPeer.UpdateChildren() at 
                // Microsoft.Windows.Automation.Peers.RibbonAutomationPeer.GetChildrenCore() at 
                
                // Also note that this code path is hit only when the UIA client is listening for 
                // structure changed events. UI Spy for instance doesn't do this by default and 
                // hence you will not automatically see the above crash it in that case. You need 
                // to configure the events to include structure changed.
                
                // -----------------------------------------------------------------------------------------
                // AutomationPeer peer = CreatePeerForElement(OwningRibbon.RibbonTabHeaderItemsControl);
                // if (peer != null)
                // {
                //     peer.ForceEnsureChildren();
                // }
                // -----------------------------------------------------------------------------------------

                // The strategy to create a new instance of the AutomationPeer is a workaround to 
                // the above limitation. It is important to create a new instance each time so 
                // that we guarantee that the children are in fact updated. A GetChildren call on 
                // a previously existing AutomationPeer will short circuit due to the _childrenValid 
                // flag. Hence the creation of a new AutomationPeer object each time around. Note 
                // that it is only this one AutomationPeer instanced that is created anew each time. 
                // The Peers for the descendents are not recreated.

                AutomationPeer peer = new RibbonTabHeaderItemsControlAutomationPeer(OwningRibbon.RibbonTabHeaderItemsControl);
                peer.GetChildren();
#endif
            }

            // Step4: Add RibbonTabs
            List<AutomationPeer> ribbonTabs = base.GetChildrenCore();
            if (ribbonTabs != null && ribbonTabs.Count > 0)
            {
                children.AddRange(ribbonTabs);

                // This is required for the RibbonTabHeaderDataAutomationPeers to correctly 
                // connect to the parent RibbonTabDataAutomationPeer
                for (int i=0; i<ribbonTabs.Count; i++)
                {
#if RIBBON_IN_FRAMEWORK
                    ribbonTabs[i].ForceEnsureChildren();
#else
                    ribbonTabs[i].GetChildren();
#endif                
                }
            }

            // Step5: Add HelpPane that appears next to TabPanel
            UIElement helpPaneHost = OwningRibbon.HelpPaneHost;
            if (helpPaneHost != null)
            {
                AutomationPeer peer = CreatePeerForElement(helpPaneHost);
                if (peer == null)
                {
                    FrameworkElement helpPaneHostFE = helpPaneHost as FrameworkElement;
                    if (helpPaneHostFE != null)
                        peer = new FrameworkElementAutomationPeer(helpPaneHostFE);
                    else
                        peer = new UIElementAutomationPeer(helpPaneHost);
                }
                children.Add(peer);
            }

            return children;
        }
        

        protected override void SetFocusCore()
        {
            // Ribbon.Focusable is always false, but UIA patterns call SetFocus
            // Override needed because base.SetFocusCore throws exception if unable to set focus
        }

        protected override bool IsOffscreenCore()
        {
            return OwningRibbon.IsCollapsed ? true : base.IsOffscreenCore();
        }

        #region ISelectionProvider Members

        /// <summary>
        /// Ribbon has one and only one Tab selected.
        /// </summary>
        bool ISelectionProvider.IsSelectionRequired 
        {
            get
            {
                return true;
            }
        }

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return false;
            }
        }
        #endregion


        #region IExpandCollapseProvider Members

        /// <summary>
        /// Set Ribbon.IsMinimized to true
        /// </summary>
        public void Collapse()
        {
            OwningRibbon.IsMinimized = true;
        }

        /// <summary>
        /// Set Ribbon.IsMinimized to false
        /// </summary>
        public void Expand()
        {
            OwningRibbon.IsMinimized = false;
        }

        /// <summary>
        /// Return Ribbon.IsMinimized
        /// </summary>
        public ExpandCollapseState ExpandCollapseState
        {
            get 
            {
                return OwningRibbon.IsMinimized ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;
            }
        }

        #endregion

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonTabDataAutomationPeer(item, this);
        }

        #region Private Members

        private Ribbon OwningRibbon
        {
            get
            {
                return (Ribbon)Owner;
            }
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }
}
