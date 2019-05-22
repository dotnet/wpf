// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
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
using MS.Internal.Data;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///<summary>
    /// Earlier this class was returning the default value for all properties when there is no wrapper/when it is virtualized,
    /// now it will throw ElementNotAvailableException (leaving some exceptions, like properties supported by container to find elements)
    /// to notify the client that the full Element does not exist yet. Client may decide to use VirtualizedItemPattern to realize the full item
    ///</summary>
    public abstract class ItemAutomationPeer : AutomationPeer, IVirtualizedItemProvider
    {
        ///
        protected ItemAutomationPeer(object item, ItemsControlAutomationPeer itemsControlAutomationPeer): base()
        {
            Item = item;
            _itemsControlAutomationPeer = itemsControlAutomationPeer;
        }

        ///
        internal override bool AncestorsInvalid
        {
            get { return base.AncestorsInvalid; }
            set
            {
                base.AncestorsInvalid = value;
                if (value)
                    return;
                AutomationPeer wrapperPeer = GetWrapperPeer();
                if (wrapperPeer != null)
                {
                    wrapperPeer.AncestorsInvalid = false;
                }
            }
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.VirtualizedItem)
            {
                if(VirtualizedItemPatternIdentifiers.Pattern != null)
                {
                    if(GetWrapperPeer() == null)
                        return this;
                    else
                    {
                        // ItemsControlAutomationPeer can be null in case of TreeViewItems when parent TreeViewItem is also virtualized
                        // If the Item is in Automation Tree we consider it has Realized and need not return VirtualizeItem pattern.
                        if(ItemsControlAutomationPeer != null && !IsItemInAutomationTree())
                        {
                            return this;
                        }

                        if(ItemsControlAutomationPeer == null)
                            return this;
                    }
                }
                return null;
            }
            else if(patternInterface == PatternInterface.SynchronizedInput)
            {
                UIElementAutomationPeer peer = GetWrapperPeer() as UIElementAutomationPeer;
                if(peer != null)
                {
                    return peer.GetPattern(patternInterface);
                }
            }

            return null;
        }

        internal UIElement GetWrapper()
        {
            UIElement wrapper = null;
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            if (itemsControlAutomationPeer != null)
            {
                ItemsControl owner = (ItemsControl)(itemsControlAutomationPeer.Owner);
                if (owner != null)
                {
                    object item = RawItem;
                    if (item != DependencyProperty.UnsetValue)
                    {
                        if (((MS.Internal.Controls.IGeneratorHost)owner).IsItemItsOwnContainer(item))
                            wrapper = item as UIElement;
                        else
                            wrapper = owner.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
                    }
                }
            }
            return wrapper;
        }

        virtual internal AutomationPeer GetWrapperPeer()
        {
            AutomationPeer wrapperPeer = null;
            UIElement wrapper = GetWrapper();
            if(wrapper != null)
            {
                wrapperPeer = UIElementAutomationPeer.CreatePeerForElement(wrapper);
                if(wrapperPeer == null) //fall back to default peer if there is no specific one
                {
                    if(wrapper is FrameworkElement)
                        wrapperPeer = new FrameworkElementAutomationPeer((FrameworkElement)wrapper);
                    else
                        wrapperPeer = new UIElementAutomationPeer(wrapper);
                }
            }

            return wrapperPeer;
        }

        /// <summary>
        internal void ThrowElementNotAvailableException()
        {
            // To avoid the situation on legacy systems which may not have new unmanaged core. this check with old unmanaged core
            // avoids throwing exception and provide older behavior returning default values for items which are virtualized rather than throwing exception.
            if (VirtualizedItemPatternIdentifiers.Pattern != null && !(this is GridViewItemAutomationPeer) && !IsItemInAutomationTree())
                throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
        }

        private bool IsItemInAutomationTree()
        {
            AutomationPeer parent = this.GetParent();
            if(this.Index != -1 && parent != null && parent.Children != null && this.Index < parent.Children.Count && parent.Children[this.Index] == this)
                return true;
            else return false;
        }


        override internal bool IsDataItemAutomationPeer()
        {
            return true;
        }

        override internal void AddToParentProxyWeakRefCache()
        {
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            if(itemsControlAutomationPeer != null)
            {
                itemsControlAutomationPeer.AddProxyToWeakRefStorage(this.ElementProxyWeakReference, this);
            }
        }

        /// <summary>
        override internal Rect GetVisibleBoundingRectCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetVisibleBoundingRectCore();
            }
            return GetBoundingRectangle();
        }

        ///
        override protected string GetItemTypeCore()
        {
            return string.Empty;
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                // The children needs to be updated before GetChildren call as ChildrenValid flag would already be true and GetChildren call won't update the children list.
                wrapperPeer.ForceEnsureChildren();
                List<AutomationPeer> children = wrapperPeer.GetChildren();
                return children;
            }

            return null;
        }

        ///
        protected override Rect GetBoundingRectangleCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetBoundingRectangle();
            }
            else
                ThrowElementNotAvailableException();

            return new Rect();
        }

        ///
        protected override bool IsOffscreenCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsOffscreen();
            else
                ThrowElementNotAvailableException();

            return true;
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetOrientation();
            else
                ThrowElementNotAvailableException();

            return AutomationOrientation.None;
        }

        /// <summary>
        /// Gets the position of an item within a set.
        /// </summary>
        /// <remarks>
        /// If <see cref="AutomationProperties.PositionInSetProperty"/> hasn't been set
        /// this method will calculate the position of an item based on its parent ItemsControl,
        /// if the ItemsControl is grouping the position will be relative to the group containing this item.
        /// </remarks>
        /// <returns>
        /// The value of <see cref="AutomationProperties.PositionInSetProperty"/> if it has been set, or it's position relative to the parent ItemsControl or GroupItem.
        /// </returns>
        protected override int GetPositionInSetCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                int position = wrapperPeer.GetPositionInSet();

                if (position == AutomationProperties.AutomationPositionInSetDefault)
                {
                    ItemsControl parentItemsControl = (ItemsControl)ItemsControlAutomationPeer.Owner;
                    position = GetPositionInSetFromItemsControl(parentItemsControl, Item);
                }

                return position;
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationProperties.AutomationPositionInSetDefault;
        }

        /// <summary>
        /// Gets the size of a set that contains this item.
        /// </summary>
        /// <remarks>
        /// If <see cref="AutomationProperties.SizeOfSetProperty"/> hasn't been set
        /// this method will calculate the size of the set containing this item based on its parent ItemsControl,
        /// if the ItemsControl is grouping the value will be representative of the group containing this item.
        /// </remarks>
        /// <returns>
        /// The value of <see cref="AutomationProperties.SizeOfSetProperty"/> if it has been set, or the size of the parent ItemsControl or GroupItem.
        /// </returns>
        protected override int GetSizeOfSetCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                int size = wrapperPeer.GetSizeOfSet();

                if (size == AutomationProperties.AutomationSizeOfSetDefault)
                {
                    ItemsControl parentItemsControl = (ItemsControl)ItemsControlAutomationPeer.Owner;
                    size = GetSizeOfSetFromItemsControl(parentItemsControl, Item);
                }

                return size;
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationProperties.AutomationSizeOfSetDefault;
        }

        internal static int GetPositionInSetFromItemsControl(ItemsControl itemsControl, object item)
        {
            int position = AutomationProperties.AutomationPositionInSetDefault;
            ItemCollection itemCollection = itemsControl.Items;
            position = itemCollection.IndexOf(item);

            if (itemsControl.IsGrouping)
            {
                int sizeOfGroup;
                position = FindPositionInGroup(itemCollection.Groups, position, out sizeOfGroup);
            }

            return position + 1;
        }

        internal static int GetSizeOfSetFromItemsControl(ItemsControl itemsControl, object item)
        {
            int size = AutomationProperties.AutomationSizeOfSetDefault;
            ItemCollection itemCollection = itemsControl.Items;

            if (itemsControl.IsGrouping)
            {
                int position = itemCollection.IndexOf(item);
                FindPositionInGroup(itemCollection.Groups, position, out size);
            }
            else
            {
                size = itemCollection.Count;
            }

            return size;
        }

        /// <summary>
        /// Based on the position of an item in the owner's item collection determine which group the item belongs to
        /// and return the relative position, also provide the size of the group
        /// </summary>
        /// <param name="collection">The top-level collection of groups</param>
        /// <param name="position">The position of the item in the flattened item collection</param>
        /// <param name="sizeOfGroup">out parameter to return the size of the group we found</param>
        /// <returns>The position of the item relative to the found group</returns>
        private static int FindPositionInGroup(ReadOnlyObservableCollection<object> collection, int position, out int sizeOfGroup)
        {
            CollectionViewGroupInternal currentGroup = null;
            ReadOnlyObservableCollection<object> newCollection = null;
            sizeOfGroup = AutomationProperties.AutomationSizeOfSetDefault;
            do
            {
                newCollection = null;
                foreach (CollectionViewGroupInternal group in collection)
                {
                    if (position < group.ItemCount)
                    {
                        currentGroup = group;
                        if (currentGroup.IsBottomLevel)
                        {
                            newCollection = null;
                            sizeOfGroup = group.ItemCount;
                        }
                        else
                        {
                            newCollection = currentGroup.Items;
                        }
                        break;
                    }
                    else
                    {
                        position -= group.ItemCount;
                    }
                }
            } while ((collection = newCollection) != null);

            return position;
        }

        ///
        protected override string GetItemStatusCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetItemStatus();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }


        ///
        protected override bool IsRequiredForFormCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsRequiredForForm();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsKeyboardFocusableCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsKeyboardFocusable();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool HasKeyboardFocusCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.HasKeyboardFocus();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsEnabledCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsEnabled();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsPasswordCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsPassword();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override string GetAutomationIdCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            string id = null;
            object item;

            if (wrapperPeer != null)
            {
                id = wrapperPeer.GetAutomationId();
            }
            else if ((item = Item) != null)
            {
                using (RecyclableWrapper recyclableWrapper = ItemsControlAutomationPeer.GetRecyclableWrapperPeer(item))
                {
                    id = recyclableWrapper.Peer.GetAutomationId();
                }
            }

            return id;
        }

        ///
        protected override string GetNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            string name = null;
            object item = Item;

            if (wrapperPeer != null)
            {
                name = wrapperPeer.GetName();
            }
            else if (item != null)
            {
                using (RecyclableWrapper recyclableWrapper = ItemsControlAutomationPeer.GetRecyclableWrapperPeer(item))
                {
                    name = recyclableWrapper.Peer.GetName();
                }
            }

            if (string.IsNullOrEmpty(name) && item != null)
            {
                // For FE we can't use ToString as that provides extraneous information than just the plain text
                FrameworkElement fe = item as FrameworkElement;
                if(fe != null)
                  name = fe.GetPlainText();

                if(string.IsNullOrEmpty(name))
                  name = item.ToString();
            }

            return name;
        }

        ///
        protected override bool IsContentElementCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsContentElement();

            return true;
        }

        ///
        protected override bool IsControlElementCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.IsControlElement();

            return true;
        }

        ///
        protected override AutomationPeer GetLabeledByCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetLabeledBy();
            else
                ThrowElementNotAvailableException();

            return null;
        }

        ///
        protected override AutomationLiveSetting GetLiveSettingCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetLiveSetting();
            else
                ThrowElementNotAvailableException();

            return AutomationLiveSetting.Off;
        }

        ///
        protected override string GetHelpTextCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetHelpText();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override string GetAcceleratorKeyCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetAcceleratorKey();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override string GetAccessKeyCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetAccessKey();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override Point GetClickablePointCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                return wrapperPeer.GetClickablePoint();
            else
                ThrowElementNotAvailableException();

            return new Point(double.NaN, double.NaN);
        }

        ///
        protected override void SetFocusCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
                wrapperPeer.SetFocus();
            else
                ThrowElementNotAvailableException();
        }

        virtual internal ItemsControlAutomationPeer GetItemsControlAutomationPeer()
        {
            return _itemsControlAutomationPeer;
        }

        ///
        public object Item
        {
            get
            {
                ItemWeakReference iwr = _item as ItemWeakReference;
                return (iwr != null) ? iwr.Target : _item;
            }
            private set
            {
                if (value != null && !value.GetType().IsValueType &&
                    !FrameworkAppContextSwitches.ItemAutomationPeerKeepsItsItemAlive)
                {
                    _item = new ItemWeakReference(value);
                }
                else
                {
                    _item = value;
                }
            }
        }

        private object RawItem
        {
            get
            {
                ItemWeakReference iwr = _item as ItemWeakReference;
                if (iwr != null)
                {
                    object item = iwr.Target;
                    return (item == null) ? DependencyProperty.UnsetValue : item;
                }
                else
                {
                    return _item;
                }
            }
        }

        // Rebuilding the "item" children of an ItemsControlAP or GroupItemAP re-uses
        // ItemAutomationPeers for items that already had peers.  Usually this happens
        // when the items are the same (the same object), but it can also happen
        // when the items are different objects but are equal in the Object.Equals sense.
        // In the latter case, we need to update the weak reference to point to
        // the new item, as the old item has a different lifetime. 
        internal void ReuseForItem(object item)
        {
            System.Diagnostics.Debug.Assert(Object.Equals(item, Item), "ItemPeer reuse for an unequal item is not supported");
            ItemWeakReference iwr = _item as ItemWeakReference;
            if (iwr != null)
            {
                if (!Object.ReferenceEquals(item, iwr.Target))
                {
                    iwr.Target = item;
                }
            }
            else
            {
                _item = item;
            }
        }

        ///
        public ItemsControlAutomationPeer ItemsControlAutomationPeer
        {
            get
            {
                return GetItemsControlAutomationPeer();
            }
            internal set
            {
                _itemsControlAutomationPeer = value;
            }
        }

        ///
        void IVirtualizedItemProvider.Realize()
        {
            RealizeCore();
        }

        virtual internal void RealizeCore()
        {
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            if (itemsControlAutomationPeer != null)
            {
                ItemsControl parent = itemsControlAutomationPeer.Owner as ItemsControl;
                if (parent != null)
                {
                    if (parent.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        if (AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures)
                        {
                            // Please note that this action must happen before the OnBringItemIntoView call because
                            // that is a call that synchronously flushes out layout and we want these realized peers
                            // cached before the UpdateSubtree kicks in OnLayoutUpdated.
                            if (VirtualizingPanel.GetIsVirtualizingWhenGrouping(parent))
                            {
                                itemsControlAutomationPeer.RecentlyRealizedPeers.Add(this);
                            }
                        }

                        parent.OnBringItemIntoView(Item);
                    }
                    else
                    {
                        // The items aren't generated, try at a later time
                        Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                            (DispatcherOperationCallback)delegate(object arg)
                            {
                                if (AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures)
                                {
                                    // Please note that this action must happen before the OnBringItemIntoView call because
                                    // that is a call that synchronously flushes out layout and we want these realized peers
                                    // cached before the UpdateSubtree kicks in OnLayoutUpdated.
                                    if (VirtualizingPanel.GetIsVirtualizingWhenGrouping(parent))
                                    {
                                        itemsControlAutomationPeer.RecentlyRealizedPeers.Add(this);
                                    }
                                }

                                parent.OnBringItemIntoView(arg);

                                return null;
                            }, Item);
                    }
                }
            }
        }

        private object _item;   // for value-types: item;  for reference-types: IWR(item)
        private ItemsControlAutomationPeer _itemsControlAutomationPeer;

        // a weak reference that is distinguishable from System.WeakReference
        private class ItemWeakReference : WeakReference
        {
            public ItemWeakReference(object o)
                : base(o)
            {}
        }
    }
}



