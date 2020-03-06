// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{

    #region Using delcarations

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Shapes;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for a RibbonTab
    /// </summary>
    public class RibbonTabAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonTabAutomationPeer(RibbonTab owner)
            : base(owner)
        {
        }

        protected override System.Collections.Generic.List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = null;

            // Groups are avaialble as children of only for the currently Selected Tab
            if (OwningTab.IsSelected)
            {
                children = base.GetChildrenCore();
            }

            if (HeaderPeer != null)
            {
                if (children == null)
                {
                    children = new List<AutomationPeer>(1);
                }
                children.Insert(0, HeaderPeer);
            }
      
            return children;
        }
 
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

#if !RIBBON_IN_FRAMEWORK
        protected override bool IsOffscreenCore()
        {
            if (!Owner.IsVisible)
                return true;

            // Borrowed from fix OffScreen fix in 4.0
            Rect boundingRect = RibbonHelper.CalculateVisibleBoundingRect(Owner);
            return (boundingRect == Rect.Empty || boundingRect.Height == 0 || boundingRect.Width == 0);
        }
#endif

        protected override Rect GetBoundingRectangleCore()
        {
            // If this is not the selected tab, return the bounding Rect of just the TabHeader.
            if (!OwningTab.IsSelected && HeaderPeer != null)
            {
                return HeaderPeer.GetBoundingRectangle();
            }

            Rect r = base.GetBoundingRectangleCore();
            if (HeaderPeer != null)
            {
                r.Union(HeaderPeer.GetBoundingRectangle());
            }

            return r;
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonGroupDataAutomationPeer(item, this);
        }

#if RIBBON_IN_FRAMEWORK
        internal override Rect GetVisibleBoundingRectCore()
        {
            if (!OwningTab.IsSelected && HeaderPeer != null)
            {
                return HeaderPeer.GetVisibleBoundingRect();
            }

            return base.GetVisibleBoundingRectCore();
        }
#endif

        #region Private Members

        private RibbonTab OwningTab
        {
            get
            {
                return (RibbonTab)Owner;
            }
        }

        private RibbonTabHeaderDataAutomationPeer HeaderPeer
        {
            get
            {
                if (OwningTab.RibbonTabHeader != null)
                {
                    AutomationPeer headerPeer = CreatePeerForElement(OwningTab.RibbonTabHeader);
                    if (headerPeer != null)
                    {
                        return headerPeer.EventsSource as RibbonTabHeaderDataAutomationPeer;
                    }
                }
                return null;
            }
        }

        #endregion

        // Never inline, as we don't want to unnecessarily link the 
        // automation DLL via the ISelectionProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseTabExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            AutomationPeer dataPeer = EventsSource;

            if (dataPeer != null)
            {
                dataPeer.RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            }
        }

        // Never inline, as we don't want to unnecessarily link the 
        // automation DLL via the ISelectionProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseTabSelectionEvents()
        {
            AutomationPeer dataPeer = EventsSource;

            if (dataPeer != null)
            {
                if( OwningTab.IsSelected )
                    dataPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                else
                    dataPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
            }
        }
    }
}
