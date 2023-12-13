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
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Automation;
    using System.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for a RibbonGroup
    /// </summary>
    public class RibbonGroupAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonGroupAutomationPeer(RibbonGroup owner)
            : base(owner)
        {
        }

        protected override System.Collections.Generic.List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            
            AutomationPeer headerPeer = HeaderPeer;
            if (headerPeer != null)
            {
                if (children == null)
                {
                    children = new List<AutomationPeer>(1);
                }
                children.Add(headerPeer);
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

            Rect boundingRect = RibbonHelper.CalculateVisibleBoundingRect(Owner);
            return (boundingRect == Rect.Empty || boundingRect.Height == 0 || boundingRect.Width == 0);
        }
#endif

        public override object GetPattern(PatternInterface patternInterface)
        {
            // Disable ScrollPattern implemented by ItemsControlAutomationPeer
            if (patternInterface == PatternInterface.Scroll)
            {
                return null;
            }

            return base.GetPattern(patternInterface);
        }

        protected override void SetFocusCore()
        {
            // RibbonGroup.Focusable is always false, but UIA patterns call SetFocus
            // Override needed because base.SetFocusCore throws exception if unable to set focus
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonControlDataAutomationPeer(item, this);
        }

        private RibbonGroup OwningGroup
        {
            get { return (RibbonGroup)Owner; }
        }

        RibbonGroupHeaderAutomationPeer HeaderPeer
        {
            get
            {
                // Header could be replaced if RibbonGroup Template is replaced while resizing
                if (_headerPeer == null || !_headerPeer.Owner.IsDescendantOf(OwningGroup))
                {
                    // Header is either a ContentPresenter or a DropDownButton 
                    if (OwningGroup.IsCollapsed)
                    {
                        // It is possible that the owing RibbonGroup's template hasn't been expanded 
                        // and hence the template parts aren't available yet. Hence the null check.
                        if (OwningGroup.CollapsedDropDownButton != null)
                        {
                            _headerPeer = new RibbonGroupHeaderAutomationPeer(OwningGroup.CollapsedDropDownButton);
                        }
                    }
                    else
                    {
                        // It is possible that the owing RibbonGroup's template hasn't been expanded 
                        // and hence the template parts aren't available yet. Hence the null check.
                        if (OwningGroup.HeaderContentPresenter != null)
                        {
                            _headerPeer = new RibbonGroupHeaderAutomationPeer(OwningGroup.HeaderContentPresenter);
                        }
                    }
                }
                return _headerPeer;
            }
        }

        // Never inline, as we don't want to unnecessarily link the 
        // automation DLL via the ISelectionProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
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

        private RibbonGroupHeaderAutomationPeer _headerPeer;
    }
}
