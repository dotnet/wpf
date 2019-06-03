// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
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
    public class TreeViewAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
    {
        ///
        public TreeViewAutomationPeer(TreeView owner): base(owner)
        {}
    
        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tree;
        }

        ///
        override protected string GetClassNameCore()
        {
            return "TreeView";
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Selection)
            {
                return this;
            }
            else if(patternInterface == PatternInterface.Scroll)
            {
                ItemsControl owner = (ItemsControl)Owner;
                if(owner.ScrollHost != null)
                {
                    AutomationPeer scrollPeer = UIElementAutomationPeer.CreatePeerForElement(owner.ScrollHost);
                    if(scrollPeer != null && scrollPeer is IScrollProvider)
                    {
                        scrollPeer.EventsSource = this;
                        return (IScrollProvider)scrollPeer;
                    }
                }
            }

            return base.GetPattern(patternInterface);
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // To avoid the situation on legacy systems which may not have new unmanaged core. with this change with old unmanaged core
            // the behavior would be same as earlier.
            if (IsVirtualized)
                return base.GetChildrenCore();
            else
            {
                ItemsControl owner = (ItemsControl)Owner;
                ItemCollection items = owner.Items;
                List<AutomationPeer> children = null;
                ItemPeersStorage<ItemAutomationPeer> oldChildren = ItemPeers; //cache the old ones for possible reuse
                ItemPeers = new ItemPeersStorage<ItemAutomationPeer>();

                if (items.Count > 0)
                {
                    children = new List<AutomationPeer>(items.Count);
                    for (int i = 0; i < items.Count; i++)
                    {
                        TreeViewItem treeViewItem = owner.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                        if (treeViewItem != null)
                        {
                            ItemAutomationPeer peer = oldChildren[items[i]];
                            if (peer == null)
                                peer = CreateItemAutomationPeer(items[i]);

                            // perform hookup so the events sourced from wrapper peer are fired as if from the data item
                            if (peer != null)
                            {
                                AutomationPeer wrapperPeer = peer.GetWrapperPeer();
                                if(wrapperPeer != null)
                                {
                                    wrapperPeer.EventsSource = peer;
                                }
                            }

                            // Not to add same Item again
                            if (ItemPeers[items[i]] == null)
                            {
                                children.Add(peer);
                               ItemPeers[items[i]] = peer;
                            }
                        }
                    }
                    return children;
                }
            }
            return null;
        }

        ///
        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new TreeViewDataItemAutomationPeer(item, this, null);
        }

        /// 
        internal override bool IsPropertySupportedByControlForFindItem(int id)
        {
            if (base.IsPropertySupportedByControlForFindItem(id))
                return true;
            else
            {
                if (SelectionItemPatternIdentifiers.IsSelectedProperty.Id == id)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Support for IsSelectedProperty should come from SelectorAutomationPeer only,
        /// </summary>
        internal override object GetSupportedPropertyValue(ItemAutomationPeer itemPeer, int propertyId)
        {
            if (SelectionItemPatternIdentifiers.IsSelectedProperty.Id == propertyId)
            {
                ISelectionItemProvider selectionItem = itemPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
                if (selectionItem != null)
                    return selectionItem.IsSelected;
                else
                    return null;
            }
            return base.GetSupportedPropertyValue(itemPeer, propertyId);
        }

        //-------------------------------------------------------------------
        //
        //  ISelectionProvider
        //
        //-------------------------------------------------------------------

        #region ISelectionProvider

        /// <summary>
        ///     Returns the current selection.
        /// </summary>
        /// <returns>The current selection.</returns>
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            IRawElementProviderSimple[] selection = null;
            TreeViewItem selectedContainer = ((TreeView)Owner).SelectedContainer;
            if (selectedContainer != null)
            {
                AutomationPeer peer = UIElementAutomationPeer.FromElement(selectedContainer);
                
                // With virtualization in effect TreeViewDataItemAP would be exposed to client not the Peer directly associated with UI
                // and Selection must return the relevant peer(TreeViewDataItemAP) stored in EventSource.
                if(peer.EventsSource != null)
                    peer = peer.EventsSource;
                    
                if (peer != null)
                {
                    selection = new IRawElementProviderSimple[] { ProviderFromPeer(peer) };
                }
            }

            if (selection == null)
            {
                selection = new IRawElementProviderSimple[0];
            }

            return selection;
        }

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return false;
            }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}



