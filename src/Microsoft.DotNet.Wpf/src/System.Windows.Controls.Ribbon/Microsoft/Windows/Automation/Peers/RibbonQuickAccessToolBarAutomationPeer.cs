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
    using Microsoft.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for a RibbonQuickAccessToolBar
    /// </summary>
    public class RibbonQuickAccessToolBarAutomationPeer : ItemsControlAutomationPeer, IExpandCollapseProvider
    {
        public RibbonQuickAccessToolBarAutomationPeer(RibbonQuickAccessToolBar owner)
            : base(owner)
        {
        }

        /// <summary>
        ///  Creates peers for CustomizeMenuButton and adds them to the 
        ///  collection of children peers.
        /// </summary>
        /// <returns></returns>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            RibbonMenuButton customizeMenuButton = OwningToolBar.CustomizeMenuButton;
            if (customizeMenuButton != null)
            {
                if (children == null)
                {
                    children = new List<AutomationPeer>();
                }
                children.Add(CreatePeerForElement(customizeMenuButton));
            }

            return children;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse &&
                OwningToolBar.HasOverflowItems)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonControlDataAutomationPeer(item, this);
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ToolBar;
        }

        // Never inline, as we don't want to unnecessarily link the 
        // automation DLL via the ISelectionProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        private RibbonQuickAccessToolBar OwningToolBar
        {
            get { return (RibbonQuickAccessToolBar)Owner; }
        }

        #region IExpandCollapseProvider Members

        public void Collapse()
        {
            if (OwningToolBar.HasOverflowItems)
            {
                OwningToolBar.IsOverflowOpen = false;
            }
            else
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        public void Expand()
        {
            if (OwningToolBar.HasOverflowItems)
            {
                OwningToolBar.IsOverflowOpen = true;
            }
            else
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        public ExpandCollapseState ExpandCollapseState
        {
            get 
            {
                return OwningToolBar.HasOverflowItems && OwningToolBar.IsOverflowOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            }
        }

        #endregion
    }
}
