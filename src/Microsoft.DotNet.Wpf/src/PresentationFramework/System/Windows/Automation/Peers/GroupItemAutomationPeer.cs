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
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Internal.Data;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class GroupItemAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public GroupItemAutomationPeer(GroupItem owner): base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "GroupItem";
        }

        /// <summary>
        /// Gets the position of this GroupItem within a set.
        /// </summary>
        /// <remarks>
        /// Gets the CollectionViewGroupInternal linked to this groupItem via ItemForItemContainerProperty,
        /// this collection describes the elements belonging to this GroupItem, we need the collection this
        /// GroupItem belongs to, so we look one level up using Parent.
        /// </remarks>
        override protected int GetPositionInSetCore()
        {
            int positionInSet = base.GetPositionInSetCore();

            if (positionInSet == AutomationProperties.AutomationPositionInSetDefault)
            {
                GroupItem groupItem = (GroupItem)Owner;
                CollectionViewGroupInternal group = groupItem.GetValue(ItemContainerGenerator.ItemForItemContainerProperty) as CollectionViewGroupInternal;
                if (group != null)
                {
                    CollectionViewGroup parent = group.Parent;
                    if (parent != null)
                    {
                        positionInSet =  parent.Items.IndexOf(group) + 1;
                    }
                }
            }

            return positionInSet;
        }

        /// <summary>
        /// Gets the size of a set that contains this GroupItem.
        /// </summary>
        /// <remarks>
        /// Gets the CollectionViewGroupInternal linked to this groupItem via ItemForItemContainerProperty,
        /// this collection describes the elements belonging to this GroupItem, we need the collection this
        /// GroupItem belongs to, so we look one level up using Parent.
        /// </remarks>
        override protected int GetSizeOfSetCore()
        {
            int sizeOfSet = base.GetSizeOfSetCore();

            if (sizeOfSet == AutomationProperties.AutomationSizeOfSetDefault)
            {
                GroupItem groupItem = (GroupItem)Owner;
                CollectionViewGroupInternal group = groupItem.GetValue(ItemContainerGenerator.ItemForItemContainerProperty) as CollectionViewGroupInternal;
                if (group != null)
                {
                    CollectionViewGroup parent = group.Parent;
                    if (parent != null)
                    {
                        sizeOfSet = parent.Items.Count;
                    }
                }
            }

            return sizeOfSet;
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if(patternInterface == PatternInterface.ExpandCollapse)
            {
                GroupItem groupItem = (GroupItem)Owner;
                if(groupItem.Expander != null)
                {
                    AutomationPeer expanderPeer = UIElementAutomationPeer.CreatePeerForElement(groupItem.Expander);
                    if(expanderPeer != null && expanderPeer is IExpandCollapseProvider)
                    {
                        expanderPeer.EventsSource = this;
                        return (IExpandCollapseProvider)expanderPeer;
                    }
                }
            }

            return base.GetPattern(patternInterface);
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            GroupItem owner = (GroupItem)Owner;
            ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(Owner);
            if (itemsControl != null)
            {
                ItemsControlAutomationPeer itemsControlAP = itemsControl.CreateAutomationPeer() as ItemsControlAutomationPeer;
                if (itemsControlAP != null)
                {
                    List<AutomationPeer> children = new List<AutomationPeer>();
                    bool useNetFx472CompatibleAccessibilityFeatures = AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures;

                    if (!useNetFx472CompatibleAccessibilityFeatures && owner.Expander != null)
                    {
                        _expanderPeer = UIElementAutomationPeer.CreatePeerForElement(owner.Expander);

                        if (_expanderPeer != null)
                        {
                            _expanderPeer.EventsSource = this;

                            // Call GetChildren so the Expander's toggle button updates its EventsSource as well
                            _expanderPeer.GetChildren();
                        }
                    }
                    Panel itemsHost = owner.ItemsHost;

                    if (itemsHost == null)
                    {
                        if (_expanderPeer == null)
                        {
                            return null;
                        }
                        else
                        {
                            children.Add(_expanderPeer);
                            return children;
                        }
                    }
                        
                    IList childItems = itemsHost.Children;
                    ItemPeersStorage<ItemAutomationPeer> addedChildren = new ItemPeersStorage<ItemAutomationPeer>();
                    

                    foreach (UIElement child in childItems)
                    {
                        if (!((MS.Internal.Controls.IGeneratorHost)itemsControl).IsItemItsOwnContainer(child))
                        {
                            UIElementAutomationPeer peer = child.CreateAutomationPeer() as UIElementAutomationPeer;
                            if (peer != null)
                            {
                                children.Add(peer);

                                if (useNetFx472CompatibleAccessibilityFeatures)
                                {
                                    //
                                    // The AncestorsInvalid check is meant so that we do this call to invalidate the
                                    // GroupItemPeers containing the realized item peers only when we arrive here from an
                                    // UpdateSubtree call because that call does not otherwise descend into parts of the tree
                                    // that have their children invalid as an optimization.
                                    //
                                    if (itemsControlAP.RecentlyRealizedPeers.Count > 0 && this.AncestorsInvalid)
                                    {
                                        GroupItemAutomationPeer groupItemPeer = peer as GroupItemAutomationPeer;
                                        if (groupItemPeer != null)
                                        {
                                            groupItemPeer.InvalidateGroupItemPeersContainingRecentlyRealizedPeers(itemsControlAP.RecentlyRealizedPeers);
                                        }
                                    }
                                }
                                else
                                {
                                    //
                                    // The AncestorsInvalid check is meant so that we do this call to invalidate the
                                    // GroupItemPeers only when we arrive here from an
                                    // UpdateSubtree call because that call does not otherwise descend into parts of the tree
                                    // that have their children invalid as an optimization.
                                    //
                                    if (this.AncestorsInvalid)
                                    {
                                        GroupItemAutomationPeer groupItemPeer = peer as GroupItemAutomationPeer;
                                        if (groupItemPeer != null)
                                        {
                                            // invalidate all GroupItemAP children, so
                                            // that the top-level ItemsControlAP's
                                            // ItemPeers collection is repopulated.
                                            groupItemPeer.AncestorsInvalid = true;
                                            groupItemPeer.ChildrenValid = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            object item = itemsControl.ItemContainerGenerator.ItemFromContainer(child);

                            // ItemFromContainer can return {UnsetValue} if we're in a re-entrant
                            // call while the generator is in the midst of unhooking the container.
                            // Ignore such children.
                            if (item == DependencyProperty.UnsetValue)
                            {
                                continue;
                            }

                            // try to reuse old peer if it exists either in Current AT or in WeakRefStorage of Peers being sent to Client
                            ItemAutomationPeer peer = useNetFx472CompatibleAccessibilityFeatures
                                                        ? itemsControlAP.ItemPeers[item]
                                                        : itemsControlAP.ReusablePeerFor(item);

                            peer = itemsControlAP.ReusePeerForItem(peer, item);

                            if (peer != null)
                            {
                                if (useNetFx472CompatibleAccessibilityFeatures)
                                {
                                    //
                                    // We have now connected the realized peer to its actual parent. Hence the cache can be cleared
                                    //
                                    int realizedPeerIndex = itemsControlAP.RecentlyRealizedPeers.IndexOf(peer);
                                    if (realizedPeerIndex >= 0)
                                    {
                                        itemsControlAP.RecentlyRealizedPeers.RemoveAt(realizedPeerIndex);
                                    }
                                }
                            }
                            else
                            {
                                peer = itemsControlAP.CreateItemAutomationPeerInternal(item);
                            }

                            //perform hookup so the events sourced from wrapper peer are fired as if from the data item
                            if (peer != null)
                            {
                                AutomationPeer wrapperPeer = peer.GetWrapperPeer();
                                if (wrapperPeer != null)
                                {
                                    wrapperPeer.EventsSource = peer;
                                    if (peer.ChildrenValid && peer.Children == null && this.AncestorsInvalid)
                                    {
                                        peer.AncestorsInvalid = true;
                                        wrapperPeer.AncestorsInvalid = true;
                                    }
                                }
                            }

                            //protection from indistinguishable items - for example, 2 strings with same value
                            //this scenario does not work in ItemsControl however is not checked for.
                            //  Our parent's ItemPeerStorage collection may not have been cleared,
                            // this would cause us to report 0 children, if the peer is already in the collection
                            // check its parent, if it has been set to us, we should add it to the return collection,
                            // but only if we haven't added a peer for this item during this GetChildrenCore call.
                            bool itemMissingPeerInGlobalStorage = itemsControlAP.ItemPeers[item] == null;

                            if (peer != null && (itemMissingPeerInGlobalStorage
                                || (peer.GetParent() == this && addedChildren[item] == null)))
                            {
                                children.Add(peer);
                                addedChildren[item] = peer;

                                if (itemMissingPeerInGlobalStorage)
                                {
                                    itemsControlAP.ItemPeers[item] = peer;
                                }
                            }
                        }
                    }

                    return children;
                }
            }

            return null;
        }

        // *** DEAD CODE   Only call is from dead code when UseNetFx472CompatibleAccessibilityFeatures==true ***
        internal void InvalidateGroupItemPeersContainingRecentlyRealizedPeers(List<ItemAutomationPeer> recentlyRealizedPeers)
        {
            ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(Owner);
            if (itemsControl != null)
            {
                CollectionViewGroupInternal cvg = itemsControl.ItemContainerGenerator.ItemFromContainer(Owner) as CollectionViewGroupInternal;
                if (cvg != null)
                {
                    for (int i=0; i<recentlyRealizedPeers.Count; i++)
                    {
                        ItemAutomationPeer peer = recentlyRealizedPeers[i];
                        object item = peer.Item;

                        if (cvg.LeafIndexOf(item) >= 0)
                        {
                            AncestorsInvalid = true;
                            ChildrenValid = true;
                        }
                    }
                }
            }
        }


        override protected void SetFocusCore()
        {
            GroupItem owner = (GroupItem)Owner;
            if (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures && owner.Expander != null)
            {
                if (owner.Expander.ExpanderToggleButton?.Focus() != true)
                {
                    throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
                }
            }
            else
            {
                base.SetFocusCore();
            }
        }


        protected override bool IsKeyboardFocusableCore()
        {
            if (_expanderPeer != null)
            {
                return _expanderPeer.IsKeyboardFocusable();
            }
            else
            {
                return base.IsKeyboardFocusableCore();
            }
        }

        override protected bool HasKeyboardFocusCore()
        {
            if (_expanderPeer != null)
            {
                return _expanderPeer.HasKeyboardFocus();
            }
            return base.HasKeyboardFocusCore();
        }

        private AutomationPeer _expanderPeer = null;
    }
}

