// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A proxy for a source item, used in live shaping.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using System.Windows;
using System.Windows.Data;

namespace MS.Internal.Data
{
    internal class LiveShapingItem : DependencyObject
    {
        internal LiveShapingItem(object item, LiveShapingList list, bool filtered = false, LiveShapingBlock block = null, bool oneTime = false)
        {
            _block = block;

            list.InitializeItem(this, item, filtered, oneTime);

            ForwardChanges = !oneTime;
        }

        internal object Item { get { return _item; } set { _item = value; } }
        internal LiveShapingBlock Block { get { return _block; } set { _block = value; } }
        LiveShapingList List { get { return Block.List; } }

        internal bool IsSortDirty
        {
            get { return TestFlag(PrivateFlags.IsSortDirty); }
            set { ChangeFlag(PrivateFlags.IsSortDirty, value); }
        }

        internal bool IsSortPendingClean
        {
            get { return TestFlag(PrivateFlags.IsSortPendingClean); }
            set { ChangeFlag(PrivateFlags.IsSortPendingClean, value); }
        }

        internal bool IsFilterDirty
        {
            get { return TestFlag(PrivateFlags.IsFilterDirty); }
            set { ChangeFlag(PrivateFlags.IsFilterDirty, value); }
        }

        internal bool IsGroupDirty
        {
            get { return TestFlag(PrivateFlags.IsGroupDirty); }
            set { ChangeFlag(PrivateFlags.IsGroupDirty, value); }
        }

        internal bool FailsFilter
        {
            get { return TestFlag(PrivateFlags.FailsFilter); }
            set { ChangeFlag(PrivateFlags.FailsFilter, value); }
        }

        internal bool ForwardChanges
        {
            get { return TestFlag(PrivateFlags.ForwardChanges); }
            set { ChangeFlag(PrivateFlags.ForwardChanges, value); }
        }

        internal bool IsDeleted
        {
            get { return TestFlag(PrivateFlags.IsDeleted); }
            set { ChangeFlag(PrivateFlags.IsDeleted, value); }
        }

        internal void FindPosition(out RBFinger<LiveShapingItem> oldFinger, out RBFinger<LiveShapingItem> newFinger, Comparison<LiveShapingItem> comparison)
        {
            Block.FindPosition(this, out oldFinger, out newFinger, comparison);
        }

        internal RBFinger<LiveShapingItem> GetFinger()
        {
            return Block.GetFinger(this);
        }

        private static readonly DependencyProperty StartingIndexProperty =
            DependencyProperty.Register("StartingIndex", typeof(int), typeof(LiveShapingItem));

        internal int StartingIndex
        {
            get { return (int)GetValue(StartingIndexProperty); }
            set { SetValue(StartingIndexProperty, value); }
        }

        internal int GetAndClearStartingIndex()
        {
            int result = StartingIndex;
            ClearValue(StartingIndexProperty);
            return result;
        }

        internal void SetBinding(string path, DependencyProperty dp, bool oneTime = false, bool enableXT = false)
        {
            if (enableXT && oneTime)
                enableXT = false;

            if (!LookupEntry(dp.GlobalIndex).Found)
            {
                if (!String.IsNullOrEmpty(path))
                {
                    Binding binding;
                    if (SystemXmlHelper.IsXmlNode(_item))
                    {
                        binding = new Binding();
                        binding.XPath = path;
                    }
                    else
                    {
                        binding = new Binding(path);
                    }

                    binding.Source = _item;
                    if (oneTime)
                        binding.Mode = BindingMode.OneTime;

                    //BindingExpressionBase beb = BindingOperations.SetBinding(this, dp, binding);
                    // we need to set the cross-thread flag before the binding is
                    // attached, in case the source raises PropertyChanged events
                    // right away.  So don't call BO.SetBinding, but imitate its effect
                    BindingExpressionBase beb = binding.CreateBindingExpression(this, dp);
                    if (enableXT)
                        beb.TargetWantsCrossThreadNotifications = true;
                    this.SetValue(dp, beb);
                }
                else if (!oneTime)
                {
                    // when the path is empty, react to any property change
                    INotifyPropertyChanged inpc = Item as INotifyPropertyChanged;
                    if (inpc != null)
                    {
                        PropertyChangedEventManager.AddHandler(inpc, OnPropertyChanged, String.Empty);
                    }
                }
            }
        }

        internal object GetValue(string path, DependencyProperty dp)
        {
            if (!String.IsNullOrEmpty(path))
            {
                SetBinding(path, dp);       // set up the binding, if not already done
                return GetValue(dp);        // return the value
            }
            else
            {
                // when the path is empty, just return the item itself
                return Item;
            }
        }

        internal void Clear()
        {
            List.ClearItem(this);
        }

        // if a sort property changes on a foreign thread, we must mark the item
        // as sort-dirty immediately, in case the UI thread is trying to restore
        // live sorting.   The item is no longer necessarily in the right position,
        // and so should not participate in comparisons.
        internal void OnCrossThreadPropertyChange(DependencyProperty dp)
        {
            List.OnItemPropertyChangedCrossThread(this, dp);
        }

        private static readonly DependencyProperty ParentGroupsProperty =
            DependencyProperty.Register("ParentGroups", typeof(object), typeof(LiveShapingItem));

        internal void AddParentGroup(CollectionViewGroupInternal group)
        {
            object o = GetValue(ParentGroupsProperty);
            List<CollectionViewGroupInternal> list;

            if (o == null)
            {   // no parents yet, store a singleton
                SetValue(ParentGroupsProperty, group);
            }
            else if ((list = o as List<CollectionViewGroupInternal>) == null)
            {   // one parent, store a list
                list = new List<CollectionViewGroupInternal>(2);
                list.Add(o as CollectionViewGroupInternal);
                list.Add(group);
                SetValue(ParentGroupsProperty, list);
            }
            else
            {   // many parents, add to the list
                list.Add(group);
            }
        }

        internal void RemoveParentGroup(CollectionViewGroupInternal group)
        {
            object o = GetValue(ParentGroupsProperty);
            List<CollectionViewGroupInternal> list = o as List<CollectionViewGroupInternal>;

            if (list == null)
            {   // one parent, remove it
                if (o == group)
                {
                    ClearValue(ParentGroupsProperty);
                }
            }
            else
            {   // many parents, remove from the list
                list.Remove(group);
                if (list.Count == 1)
                {   // collapse a singleton list
                    SetValue(ParentGroupsProperty, list[0]);
                }
            }
        }

        internal List<CollectionViewGroupInternal> ParentGroups
        {
            get { return GetValue(ParentGroupsProperty) as List<CollectionViewGroupInternal>; }
        }

        internal CollectionViewGroupInternal ParentGroup
        {
            get { return GetValue(ParentGroupsProperty) as CollectionViewGroupInternal; }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ForwardChanges)
            {
                List.OnItemPropertyChanged(this, e.Property);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            List.OnItemPropertyChanged(this, null);
        }

        private bool TestFlag(PrivateFlags flag)
        {
            return (_flags & flag) != 0;
        }

        private void ChangeFlag(PrivateFlags flag, bool value)
        {
            if (value) _flags |= flag;
            else _flags &= ~flag;
        }

        [Flags]
        private enum PrivateFlags
        {
            IsSortDirty = 0x00000001,   // sort property has changed (even cross-thread)
            IsSortPendingClean = 0x00000002,   // item is on the SortDirtyItems list
            IsFilterDirty = 0x00000004,   // filter property has changed
            IsGroupDirty = 0x00000008,   // grouping property has changed
            FailsFilter = 0x00000010,   // item fails the filter
            ForwardChanges = 0x00000020,   // inform list of changes
            IsDeleted = 0x00000040,   // item is deleted - no live shaping needed
        }

        LiveShapingBlock _block;    // the block where I appear
        object _item;      // the source item I represent
        PrivateFlags _flags;
    }
}
