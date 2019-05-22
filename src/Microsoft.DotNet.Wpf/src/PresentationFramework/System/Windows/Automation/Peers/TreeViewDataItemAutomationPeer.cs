// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Data Item Automation peer for TreeViewItem, it may be associated with the 
//              full-fleged peer called wrapper peer, TreeViewItemAutomationPeer, if the item is realized.
//


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
using System.Windows.Threading;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    public class TreeViewDataItemAutomationPeer : ItemAutomationPeer, ISelectionItemProvider, IScrollItemProvider, IExpandCollapseProvider
    {
        ///
        public TreeViewDataItemAutomationPeer(object item, ItemsControlAutomationPeer itemsControlAutomationPeer, TreeViewDataItemAutomationPeer parentDataItemAutomationPeer)
            : base(item, null)
        {
            if(itemsControlAutomationPeer.Owner is TreeView || parentDataItemAutomationPeer == null)
                ItemsControlAutomationPeer = itemsControlAutomationPeer;
            _parentDataItemAutomationPeer = parentDataItemAutomationPeer;
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
            else if ((patternInterface == PatternInterface.ItemContainer) || (patternInterface == PatternInterface.SynchronizedInput))
            {
                TreeViewItemAutomationPeer treeViewItemAutomationPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
                if (treeViewItemAutomationPeer != null)
                {
                    if(patternInterface == PatternInterface.SynchronizedInput)
                    {
                        return treeViewItemAutomationPeer.GetPattern(patternInterface);        
                    }
                    else
                    { 
                        return treeViewItemAutomationPeer;
                    }
                }
            }

            return base.GetPattern(patternInterface);
        }

        /// 
        override internal AutomationPeer GetWrapperPeer()
        {
            AutomationPeer wrapperPeer = base.GetWrapperPeer();
            TreeViewItemAutomationPeer treeViewItemWrapperPeer = wrapperPeer as TreeViewItemAutomationPeer;
            if (treeViewItemWrapperPeer != null)
            {
                treeViewItemWrapperPeer.AddDataPeerInfo(this);
            }
            
            return wrapperPeer;
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

        public TreeViewDataItemAutomationPeer ParentDataItemAutomationPeer
        {
            get
            {
                return _parentDataItemAutomationPeer;
            }
        }

        // Return the ItemsControlAP (wrapper peer) corresponding to parent data item, as that will have most up-to-date reference. Sometime along with items even the parent could 
        // be virtualized as well, for eg in case of muti-level TreeView, intermediate TreeView item nodes could be virtualized and realized at any point in time,  in that wrapper peer will be 
        // recreated as well. 
        override internal ItemsControlAutomationPeer GetItemsControlAutomationPeer()
        {
                if(_parentDataItemAutomationPeer == null)
                    return base.GetItemsControlAutomationPeer();
                else
                {
                    return _parentDataItemAutomationPeer.GetWrapperPeer() as ItemsControlAutomationPeer;
                }
        }

        /// 
        override internal void RealizeCore()
        {
            RecursiveScrollIntoView();
        }

        ///
        void IExpandCollapseProvider.Expand()
        {
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IExpandCollapseProvider iExpandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                iExpandCollapseProvider.Expand();
            }
            else
                ThrowElementNotAvailableException();
        }

        ///
        void IExpandCollapseProvider.Collapse()
        {
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IExpandCollapseProvider iExpandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                iExpandCollapseProvider.Collapse();
            }
            else
                ThrowElementNotAvailableException();
        }


        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
                if (wrapperPeer != null)
                {
                    IExpandCollapseProvider iExpandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                    return iExpandCollapseProvider.ExpandCollapseState;
                }
                else
                    ThrowElementNotAvailableException();
                return ExpandCollapseState.LeafNode;
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
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
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                ISelectionItemProvider iSelectionItemProvider = wrapperPeer as ISelectionItemProvider;
                iSelectionItemProvider.Select();
            }
            else
                ThrowElementNotAvailableException();
        }

        /// <summary>
        ///     Selects this item.
        /// </summary>
        void ISelectionItemProvider.AddToSelection()
        {
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                ISelectionItemProvider iSelectionItemProvider = wrapperPeer as ISelectionItemProvider;
                iSelectionItemProvider.AddToSelection();
            }
            else
                ThrowElementNotAvailableException();
        }

        /// <summary>
        ///     Unselects this item.
        /// </summary>
        void ISelectionItemProvider.RemoveFromSelection()
        {
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                ISelectionItemProvider iSelectionItemProvider = wrapperPeer as ISelectionItemProvider;
                iSelectionItemProvider.RemoveFromSelection();
            }
            else
                ThrowElementNotAvailableException();
        }

        /// <summary>
        ///     Returns whether the item is selected.
        /// </summary>
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                // Virtualized Element are treated as not selected.
                TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
                if (wrapperPeer != null)
                {
                    ISelectionItemProvider iSelectionItemProvider = wrapperPeer as ISelectionItemProvider;
                    return iSelectionItemProvider.IsSelected;
                }
                return false;
            }
        }

        /// <summary>
        ///     The logical element that supports the SelectionPattern for this item.
        /// </summary>
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
                if (wrapperPeer != null)
                {
                    ISelectionItemProvider iSelectionItemProvider = wrapperPeer as ISelectionItemProvider;
                    return iSelectionItemProvider.SelectionContainer;
                }
                else
                    ThrowElementNotAvailableException();

                return null;
            }
        }

        void IScrollItemProvider.ScrollIntoView()
        {
            TreeViewItemAutomationPeer wrapperPeer = GetWrapperPeer() as TreeViewItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IScrollItemProvider iScrollItemProvider = wrapperPeer as IScrollItemProvider;
                iScrollItemProvider.ScrollIntoView();
            }
            else
            {
                RecursiveScrollIntoView();
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseAutomationIsSelectedChanged(bool isSelected)
        {
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                !isSelected,
                isSelected);
        }

        #endregion


        private void RecursiveScrollIntoView()
        {
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            // to check if the parent of this item is TreeViewItem.
            if (ParentDataItemAutomationPeer != null)
            {
                // there might be a possibility that it's parent item is itself virtualized so we have to realize it and make a second attempt
                if (itemsControlAutomationPeer == null)
                {
                    ParentDataItemAutomationPeer.RecursiveScrollIntoView();
                    itemsControlAutomationPeer = ItemsControlAutomationPeer;
                }
            }

            if (itemsControlAutomationPeer != null)
            {
                TreeViewItemAutomationPeer treeViewItemAutomationPeer = itemsControlAutomationPeer as TreeViewItemAutomationPeer;
                if (treeViewItemAutomationPeer != null && (treeViewItemAutomationPeer as IExpandCollapseProvider).ExpandCollapseState == ExpandCollapseState.Collapsed)
                {
                    (treeViewItemAutomationPeer as IExpandCollapseProvider).Expand();
                }

                ItemsControl parent = itemsControlAutomationPeer.Owner as ItemsControl;
                if (parent != null)
                {
                    if (parent.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        parent.OnBringItemIntoView(Item);
                    }
                    else
                    {
                        // The items aren't generated, try at a later time
                        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(parent.OnBringItemIntoView), Item);
                    }
                }
            }
        }
        
        // This is needed for scenarios where there is a deeply nested tree and client has hold of leaf DataItemPeer, now as it gets virtualized 
        // it’s parent’s WrapperPeer goes away but WeakRef collection won't as they will be reffered in this DataPeer corresponding to that parent.
        // And at later time when it gets realized the Wrapper will use the collection stored here.
        internal ItemPeersStorage<WeakReference> WeakRefElementProxyStorageCache
        {
            get
            {
                return _WeakRefElementProxyStorageCache;
            }

            set
            {
                _WeakRefElementProxyStorageCache = value;
            }
        }

        private TreeViewDataItemAutomationPeer _parentDataItemAutomationPeer;
        private ItemPeersStorage<WeakReference> _WeakRefElementProxyStorageCache;
    }
}
