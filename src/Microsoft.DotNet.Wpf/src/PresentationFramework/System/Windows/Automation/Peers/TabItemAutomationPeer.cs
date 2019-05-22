// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class TabItemAutomationPeer : SelectorItemAutomationPeer, ISelectionItemProvider
    {
        ///
        public TabItemAutomationPeer(object owner, TabControlAutomationPeer tabControlAutomationPeer)
            : base(owner, tabControlAutomationPeer)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "TabItem";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.TabItem;
        }

        // Return the base without the AccessKey character
        ///
        override protected string GetNameCore()
        {
            string result = base.GetNameCore();
            if (!string.IsNullOrEmpty(result))
            {
                TabItem tabItem = GetWrapper() as TabItem;
                if ((tabItem != null) && (tabItem.Header is string))
                {
                    return AccessText.RemoveAccessKeyMarker(result);
                }
            }

            return result;
        }

        // Selected TabItem content is located under the TabControl style visual tree
        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // Call the base in case we have children in the header
            List<AutomationPeer> headerChildren = base.GetChildrenCore();

            // Only if the TabItem is selected we need to add its visual children
            TabItem tabItem = GetWrapper() as TabItem;
            if (tabItem != null && tabItem.IsSelected)
            {
                TabControl parentTabControl = ItemsControlAutomationPeer.Owner as TabControl;
                if (parentTabControl != null)
                {
                    ContentPresenter contentHost = parentTabControl.SelectedContentPresenter;
                    if (contentHost != null)
                    {
                        AutomationPeer contentHostPeer = new FrameworkElementAutomationPeer(contentHost);
                        List<AutomationPeer> contentChildren = contentHostPeer.GetChildren();
                        if (contentChildren != null)
                        {
                            if (headerChildren == null)
                                headerChildren = contentChildren;
                            else
                                headerChildren.AddRange(contentChildren);
                        }
                    }
                }
            }

            return headerChildren;
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            TabItem tabItem = GetWrapper() as TabItem;
            if ((tabItem != null) && tabItem.IsSelected)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }
        }
        
        /// Realization for TabItem is tied to selection, bringing item into view for realizing the element
        /// as done for controls like ListBox doesn't make sense for TabControl.
        internal override void RealizeCore()
        {
            ISelectionItemProvider selectionItemProvider = this as ISelectionItemProvider;
            Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);
            if (parentSelector != null && selectionItemProvider != null)
            {
                if (parentSelector.CanSelectMultiple)
                    selectionItemProvider.AddToSelection();
                else
                    selectionItemProvider.Select();
            }
        }
    }
}

