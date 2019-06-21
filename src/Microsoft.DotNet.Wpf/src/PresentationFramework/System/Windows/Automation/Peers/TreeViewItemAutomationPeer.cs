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
    public class TreeViewItemAutomationPeer : ItemsControlAutomationPeer, IExpandCollapseProvider, ISelectionItemProvider, IScrollItemProvider
    {
        ///
        public TreeViewItemAutomationPeer(TreeViewItem owner): base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "TreeViewItem";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.TreeItem;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }
            else if (patternInterface == PatternInterface.SelectionItem)
            {
                return this;
            }
            else if (patternInterface == PatternInterface.ScrollItem)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = null;
            ItemPeersStorage<ItemAutomationPeer> oldChildren = ItemPeers; //cache the old ones for possible reuse
            ItemPeers = new ItemPeersStorage<ItemAutomationPeer>();

            TreeViewItem owner = Owner as TreeViewItem;
            if (owner != null)
            {
                iterate(this, owner,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        if (children == null)
                            children = new List<AutomationPeer>();

                        children.Add(peer);
                        return (false);
                    }, ItemPeers, oldChildren);
            }
            return children;
        }

        private delegate bool IteratorCallback(AutomationPeer peer);

        //
        private static bool iterate(TreeViewItemAutomationPeer logicalParentAp, DependencyObject parent, IteratorCallback callback, ItemPeersStorage<ItemAutomationPeer> dataChildren, ItemPeersStorage<ItemAutomationPeer> oldChildren)
        {
            bool done = false;

            if (parent != null)
            {
                AutomationPeer peer = null;
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count && !done; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child != null
                        && child is UIElement)
                    {
                        if (child is TreeViewItem)
                        {
                            object dataItem = (child as UIElement) != null ? (logicalParentAp.Owner as ItemsControl).GetItemOrContainerFromContainer(child as UIElement) : child;
                            peer = oldChildren[dataItem];

                            if (peer == null)
                            {
                                peer = logicalParentAp.GetPeerFromWeakRefStorage(dataItem);
                                if (peer != null)
                                {
                                    // As cached peer is getting used it must be invalidated.
                                    peer.AncestorsInvalid = false;
                                    peer.ChildrenValid = false;
                                }
                            }

                            if (peer == null)
                            {
                                peer = logicalParentAp.CreateItemAutomationPeer(dataItem);
                            }

                            //perform hookup so the events sourced from wrapper peer are fired as if from the data item
                            if (peer != null)
                            {
                                AutomationPeer wrapperPeer = (peer as ItemAutomationPeer).GetWrapperPeer();
                                if (wrapperPeer != null)
                                {
                                    wrapperPeer.EventsSource = peer;
                                }

                                if (dataChildren[dataItem] == null && peer is ItemAutomationPeer)
                                {
                                    callback(peer);
                                    dataChildren[dataItem] = peer as ItemAutomationPeer;
                                }
                            }
                        }
                        else
                        {
                            peer = CreatePeerForElement((UIElement)child);

                            if (peer != null)
                                done = callback(peer);
                        }

                        if(peer == null)
                            done = iterate(logicalParentAp, child, callback, dataChildren, oldChildren);
                    }
                    else
                    {
                        done = iterate(logicalParentAp, child, callback, dataChildren, oldChildren);
                    }
                }
            }

            return done;
        }


        /// <summary>
        /// It returns the ItemAutomationPeer if it exist corresponding to the item otherwise it creates
        /// one and does add the Handle and parent info by calling TrySetParentInfo.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override internal ItemAutomationPeer FindOrCreateItemAutomationPeer(object item)
        {
            ItemAutomationPeer peer = ItemPeers[item];
            AutomationPeer parentPeer = this;
            if (EventsSource as TreeViewDataItemAutomationPeer != null)
            {
            	parentPeer = EventsSource as TreeViewDataItemAutomationPeer;
            }

            if (peer == null)
                peer = GetPeerFromWeakRefStorage(item);

            if (peer == null)
            {
                peer = CreateItemAutomationPeer(item);

                if(peer != null)
                {
                    peer.TrySetParentInfo(parentPeer);
                }
            }

            if(peer != null)
            {
                AutomationPeer wrapperPeer = (peer as ItemAutomationPeer).GetWrapperPeer();
                if (wrapperPeer != null)
                {
                    wrapperPeer.EventsSource = peer;
                }
            }

            return peer;
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

        ///
        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new TreeViewDataItemAutomationPeer(item, this, EventsSource as TreeViewDataItemAutomationPeer);
        }

        //
        override internal IDisposable UpdateChildren()
        {
            // To ensure that the Updation of children should be initiated from DataPeer so as to have the right parent value stored for children
            TreeViewDataItemAutomationPeer dataPeer = EventsSource as TreeViewDataItemAutomationPeer;
            if(dataPeer != null)
                dataPeer.UpdateChildrenInternal(AutomationInteropProvider.ItemsInvalidateLimit);
            else
                UpdateChildrenInternal(AutomationInteropProvider.ItemsInvalidateLimit);
            WeakRefElementProxyStorage.PurgeWeakRefCollection();
            return null;
        }

        /// <summary>
        internal void AddDataPeerInfo(TreeViewDataItemAutomationPeer dataPeer)
        {
            EventsSource = dataPeer;
            UpdateWeakRefStorageFromDataPeer();
        }

        ///
        internal void UpdateWeakRefStorageFromDataPeer()
        {
            // To use the already stored WeakRef collection of it's children Items which might be created when last time this item was realized.
            if(EventsSource as TreeViewDataItemAutomationPeer != null)
            {
                if((EventsSource as TreeViewDataItemAutomationPeer).WeakRefElementProxyStorageCache == null)
                    (EventsSource as TreeViewDataItemAutomationPeer).WeakRefElementProxyStorageCache = WeakRefElementProxyStorage;
                else if(WeakRefElementProxyStorage.Count == 0)
                {
                    WeakRefElementProxyStorage = (EventsSource as TreeViewDataItemAutomationPeer).WeakRefElementProxyStorageCache;
                }
            }
        }

        ///
        void IExpandCollapseProvider.Expand()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            TreeViewItem treeViewItem = (TreeViewItem)Owner;

            if (!treeViewItem.HasItems)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            treeViewItem.IsExpanded = true;
        }

        ///
        void IExpandCollapseProvider.Collapse()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            TreeViewItem treeViewItem = (TreeViewItem)Owner;

            if (!treeViewItem.HasItems)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            treeViewItem.IsExpanded = false;
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                TreeViewItem treeViewItem = (TreeViewItem)Owner;
                if (treeViewItem.HasItems)
                    return treeViewItem.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
                else
                    return ExpandCollapseState.LeafNode;
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            if (EventsSource as TreeViewDataItemAutomationPeer != null)
            {
                (EventsSource as TreeViewDataItemAutomationPeer).RaiseExpandCollapseAutomationEvent(oldValue, newValue);
            }
            else
                RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        #region ISelectionItemProvider

        /// <summary>
        ///     Selects this element, removing any other element from the selection.
        /// </summary>
        void ISelectionItemProvider.Select()
        {
            ((TreeViewItem)Owner).IsSelected = true;
        }

        /// <summary>
        ///     Selects this item.
        /// </summary>
        void ISelectionItemProvider.AddToSelection()
        {
            TreeView treeView = ((TreeViewItem)Owner).ParentTreeView;
            // If TreeView already has a selected item different from current - we cannot add to selection and throw
            if (treeView == null || (treeView.SelectedItem != null && treeView.SelectedContainer != Owner))
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }
            ((TreeViewItem)Owner).IsSelected = true;
        }

        /// <summary>
        ///     Unselects this item.
        /// </summary>
        void ISelectionItemProvider.RemoveFromSelection()
        {
            ((TreeViewItem)Owner).IsSelected = false;
        }

        /// <summary>
        ///     Returns whether the item is selected.
        /// </summary>
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return ((TreeViewItem)Owner).IsSelected;
            }
        }

        /// <summary>
        ///     The logical element that supports the SelectionPattern for this item.
        /// </summary>
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                ItemsControl parent = ((TreeViewItem)Owner).ParentItemsControl;
                if (parent != null)
                {
                    AutomationPeer peer = UIElementAutomationPeer.FromElement(parent);
                    if (peer != null)
                        return ProviderFromPeer(peer);
                }

                return null;
            }
        }

        void IScrollItemProvider.ScrollIntoView()
        {
            ((TreeViewItem)Owner).BringIntoView();
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseAutomationIsSelectedChanged(bool isSelected)
        {
            if (EventsSource as TreeViewDataItemAutomationPeer != null)
            {
                (EventsSource as TreeViewDataItemAutomationPeer).RaiseAutomationIsSelectedChanged(isSelected);
            }
            else
                RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                !isSelected,
                isSelected);
        }

        // Selection Events needs to be raised on DataItem Peers now when they exist.
        internal void RaiseAutomationSelectionEvent(AutomationEvents eventId)
        {
            if (EventsSource != null)
            {
                EventsSource.RaiseAutomationEvent(eventId);
            }
            else
                this.RaiseAutomationEvent(eventId);
        }
        #endregion
    }
}

