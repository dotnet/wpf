// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Root of CollectionViewGroup structure, as created by a CollectionView according to a GroupDescription.
//                  CollectionView classes use this class to manage all Grouping functionality.
//
// See spec at Grouping.mht
//

using System;
using System.Collections;       // IComparer
using System.Collections.Generic;       // List<T>
using System.Collections.ObjectModel;   // ObservableCollection
using System.Collections.Specialized;   // INotifyCollectionChanged
using System.ComponentModel;    // PropertyChangedEventArgs, GroupDescription
using System.Diagnostics;       // Debug.Assert
using System.Globalization;

using System.Windows;
using System.Windows.Data;      // CollectionViewGroup

namespace MS.Internal.Data
{
    // CollectionView classes use this class as the manager of all Grouping functionality
    internal class CollectionViewGroupRoot : CollectionViewGroupInternal, INotifyCollectionChanged
    {
        internal CollectionViewGroupRoot(CollectionView view) : base("Root", null)
        {
            _view = view;
        }

        #region INotifyCollectionChanged
        /// <summary>
        /// Raise this event when the (grouped) view changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Notify listeners that this View has changed
        /// </summary>
        /// <remarks>
        ///     CollectionViews (and sub-classes) should take their filter/sort/grouping
        ///     into account before calling this method to forward CollectionChanged events.
        /// </remarks>
        /// <param name="args">
        ///     The NotifyCollectionChangedEventArgs to be passed to the EventHandler
        /// </param>
        public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            if (CollectionChanged != null)
                CollectionChanged(this, args);
        }
        #endregion INotifyCollectionChanged

        /// <summary>
        /// The description of grouping, indexed by level.
        /// </summary>
        public virtual ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return _groupBy; }
        }

        /// <summary>
        /// A delegate to select the group description as a function of the
        /// parent group and its level.
        /// </summary>
        public virtual GroupDescriptionSelectorCallback GroupBySelector
        {
            get { return _groupBySelector; }
            set { _groupBySelector = value; }
        }

        // a group description has changed somewhere in the tree - notify host
        protected override void OnGroupByChanged()
        {
            if (GroupDescriptionChanged != null)
                GroupDescriptionChanged(this, EventArgs.Empty);
        }

        #region Internal Events and Properties

        internal event EventHandler GroupDescriptionChanged;

        internal IComparer ActiveComparer
        {
            get { return _comparer; }
            set { _comparer = value; }
        }

        /// <summary>
        /// Culture to use when comparing group name with item property value.
        /// </summary>
        internal CultureInfo Culture
        {
            get { return _view.Culture; }
        }

        internal bool IsDataInGroupOrder
        {
            get { return _isDataInGroupOrder; }
            set { _isDataInGroupOrder = value; }
        }

        internal CollectionView View
        {
            get { return _view; }
        }

        #endregion Internal Events and Properties

        #region Internal Methods

        internal void Initialize()
        {
            if (_topLevelGroupDescription == null)
            {
                _topLevelGroupDescription = new TopLevelGroupDescription();
            }
            InitializeGroup(this, _topLevelGroupDescription, 0);
        }

        internal void AddToSubgroups(object item, LiveShapingItem lsi, bool loading)
        {
            AddToSubgroups(item, lsi, this, 0, loading);
        }

        internal bool RemoveFromSubgroups(object item)
        {
            return RemoveFromSubgroups(item, this, 0);
        }

        internal void RemoveItemFromSubgroupsByExhaustiveSearch(object item)
        {
            RemoveItemFromSubgroupsByExhaustiveSearch(this, item);
        }

        internal void InsertSpecialItem(int index, object item, bool loading)
        {
            ChangeCounts(item, +1);
            ProtectedItems.Insert(index, item);

            if (!loading)
            {
                int globalIndex = this.LeafIndexFromItem(item, index);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, globalIndex));
            }
        }

        internal void RemoveSpecialItem(int index, object item, bool loading)
        {
            Debug.Assert(System.Windows.Controls.ItemsControl.EqualsEx(item, ProtectedItems[index]), "RemoveSpecialItem finds inconsistent data");
            int globalIndex = -1;

            if (!loading)
            {
                globalIndex = this.LeafIndexFromItem(item, index);
            }

            ChangeCounts(item, -1);
            ProtectedItems.RemoveAt(index);

            if (!loading)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, globalIndex));
            }
        }

        internal void MoveWithinSubgroups(object item, LiveShapingItem lsi, IList list, int oldIndex, int newIndex)
        {
            if (lsi == null)
            {
                // recursively descend through the groups, moving the item within
                // groups it belongs to
                MoveWithinSubgroups(item, this, 0, list, oldIndex, newIndex);
            }
            else
            {
                // when live shaping is in effect, lsi records which groups the item
                // belongs to.  Move the item within those groups

                CollectionViewGroupInternal parentGroup = lsi.ParentGroup;
                if (parentGroup != null)
                {
                    // 90% case - item belongs to a single group
                    MoveWithinSubgroup(item, parentGroup, list, oldIndex, newIndex);
                }
                else
                {
                    // 10% case - item belongs to many groups
                    foreach (CollectionViewGroupInternal group in lsi.ParentGroups)
                    {
                        MoveWithinSubgroup(item, group, list, oldIndex, newIndex);
                    }
                }
            }
        }

        protected override int FindIndex(object item, object seed, IComparer comparer, int low, int high)
        {
            // root group needs to adjust the bounds of the search to exclude the
            // placeholder and new item (if any)
            IEditableCollectionView iecv = _view as IEditableCollectionView;
            if (iecv != null)
            {
                if (iecv.NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
                {
                    ++low;
                    if (iecv.IsAddingNew)
                    {
                        ++low;
                    }
                }
                else
                {
                    if (iecv.IsAddingNew)
                    {
                        --high;
                    }
                    if (iecv.NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd)
                    {
                        --high;
                    }
                }
            }

            return base.FindIndex(item, seed, comparer, low, high);
        }

        // for the given item, check its grouping, add it to groups it has newly joined,
        // and record groups it has newly left in the delete-list
        internal void RestoreGrouping(LiveShapingItem lsi, List<AbandonedGroupItem> deleteList)
        {
            GroupTreeNode root = BuildGroupTree(lsi);
            root.ContainsItem = true;
            RestoreGrouping(lsi, root, 0, deleteList);
        }

        void RestoreGrouping(LiveShapingItem lsi, GroupTreeNode node, int level, List<AbandonedGroupItem> deleteList)
        {
            if (node.ContainsItem)
            {
                // item belongs to this group - check subgroups
                object name = GetGroupName(lsi.Item, node.Group.GroupBy, level);
                if (name != UseAsItemDirectly)
                {
                    ICollection ic = name as ICollection;
                    ArrayList names = (ic == null) ? null : new ArrayList(ic);

                    // find subgroups whose names still match
                    for (GroupTreeNode child = node.FirstChild; child != null; child = child.Sibling)
                    {
                        if (names == null)
                        {
                            if (Object.Equals(name, child.Group.Name))
                            {
                                child.ContainsItem = true;
                                name = DependencyProperty.UnsetValue;   // name is 'seen'
                                break;
                            }
                        }
                        else
                        {
                            if (names.Contains(child.Group.Name))
                            {
                                child.ContainsItem = true;
                                names.Remove(child.Group.Name);
                            }
                        }
                    }

                    // for names that don't match, add the item to the new subgroup
                    if (names == null)
                    {
                        if (name != DependencyProperty.UnsetValue)
                        {
                            AddToSubgroup(lsi.Item, lsi, node.Group, level, name, false);
                        }
                    }
                    else
                    {
                        foreach (object o in names)
                        {
                            AddToSubgroup(lsi.Item, lsi, node.Group, level, o, false);
                        }
                    }
                }
            }
            else
            {
                // item doesn't belong to this group - if it used to belong directly,
                // mark it for deletion
                if (node.ContainsItemDirectly)
                {
                    deleteList.Add(new AbandonedGroupItem(lsi, node.Group));
                }
            }

            // recursively handle children
            for (GroupTreeNode child = node.FirstChild; child != null; child = child.Sibling)
            {
                RestoreGrouping(lsi, child, level + 1, deleteList);
            }
        }

        GroupTreeNode BuildGroupTree(LiveShapingItem lsi)
        {
            CollectionViewGroupInternal parentGroup = lsi.ParentGroup;
            GroupTreeNode node;

            if (parentGroup != null)
            {
                // 90% case - item belongs to only one group.   Construct tree
                // the fast way
                node = new GroupTreeNode()
                { Group = parentGroup, ContainsItemDirectly = true };
                for (; ; )
                {
                    CollectionViewGroupInternal group = parentGroup;
                    parentGroup = group.Parent;
                    if (parentGroup == null)
                        break;

                    GroupTreeNode parentNode = new GroupTreeNode()
                    { Group = parentGroup, FirstChild = node };
                    node = parentNode;
                }
                return node;
            }
            else
            {
                // item belongs to multiple groups ("categories").   Construct tree
                // the slow way.
                List<CollectionViewGroupInternal> parentGroups = lsi.ParentGroups;
                List<GroupTreeNode> list = new List<GroupTreeNode>(parentGroups.Count + 1);
                GroupTreeNode root = null;

                // initialize the list with a node for each direct parent group
                foreach (CollectionViewGroupInternal group in parentGroups)
                {
                    node = new GroupTreeNode()
                    { Group = group, ContainsItemDirectly = true };
                    list.Add(node);
                }

                // add each node in the list to the tree
                for (int index = 0; index < list.Count; ++index)
                {
                    node = list[index];
                    parentGroup = node.Group.Parent;
                    GroupTreeNode parentNode = null;

                    // special case for the root
                    if (parentGroup == null)
                    {
                        root = node;
                        continue;
                    }

                    // search for an existing parent node
                    for (int k = list.Count - 1; k >= 0; --k)
                    {
                        if (list[k].Group == parentGroup)
                        {
                            parentNode = list[k];
                            break;
                        }
                    }

                    if (parentNode == null)
                    {
                        // no existing parent node - create one now
                        parentNode = new GroupTreeNode()
                        { Group = parentGroup, FirstChild = node };
                        list.Add(parentNode);
                    }
                    else
                    {
                        // add node to existing parent
                        node.Sibling = parentNode.FirstChild;
                        parentNode.FirstChild = node;
                    }
                }

                return root;
            }
        }

        internal void DeleteAbandonedGroupItems(List<AbandonedGroupItem> deleteList)
        {
            foreach (AbandonedGroupItem agi in deleteList)
            {
                RemoveFromGroupDirectly(agi.Group, agi.Item.Item);
                agi.Item.RemoveParentGroup(agi.Group);
            }
        }

        class GroupTreeNode
        {
            public GroupTreeNode FirstChild { get; set; }
            public GroupTreeNode Sibling { get; set; }
            public CollectionViewGroupInternal Group { get; set; }
            public bool ContainsItem { get; set; }
            public bool ContainsItemDirectly { get; set; }
        }

        #endregion Internal Methods

        #region private methods

        // Initialize the given group
        void InitializeGroup(CollectionViewGroupInternal group, GroupDescription parentDescription, int level)
        {
            // set the group description for dividing the group into subgroups
            GroupDescription groupDescription = GetGroupDescription(group, parentDescription, level);
            group.GroupBy = groupDescription;

            // create subgroups for each of the explicit names
            ObservableCollection<object> explicitNames =
                        (groupDescription != null) ? groupDescription.GroupNames : null;
            if (explicitNames != null)
            {
                for (int k = 0, n = explicitNames.Count; k < n; ++k)
                {
                    CollectionViewGroupInternal subgroup = new CollectionViewGroupInternal(explicitNames[k], group, isExplicit: true);
                    InitializeGroup(subgroup, groupDescription, level + 1);
                    group.Add(subgroup);
                }
            }

            group.LastIndex = 0;
        }


        // return the description of how to divide the given group into subgroups
        GroupDescription GetGroupDescription(CollectionViewGroup group, GroupDescription parentDescription, int level)
        {
            GroupDescription result = null;
            if (group == this)
            {
                group = null;       // users don't see the synthetic group
            }

            if (parentDescription != null)
            {
#if GROUPDESCRIPTION_HAS_SUBGROUP
                // a. Use the parent description's subgroup description
                result = parentDescription.Subgroup;
#endif // GROUPDESCRIPTION_HAS_SUBGROUP

#if GROUPDESCRIPTION_HAS_SELECTOR
                // b. Call the parent description's selector
                if (result == null && parentDescription.SubgroupSelector != null)
                {
                    result = parentDescription.SubgroupSelector(group, level);
                }
#endif // GROUPDESCRIPTION_HAS_SELECTOR
            }

            // c. Call the global chooser
            if (result == null && GroupBySelector != null)
            {
                result = GroupBySelector(group, level);
            }

            // d. Use the global array
            if (result == null && level < GroupDescriptions.Count)
            {
                result = GroupDescriptions[level];
            }

            return result;
        }

        // add an item to the desired subgroup(s) of the given group
        void AddToSubgroups(object item, LiveShapingItem lsi, CollectionViewGroupInternal group, int level, bool loading)
        {
            object name = GetGroupName(item, group.GroupBy, level);
            ICollection nameList;

            if (name == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                if (lsi != null)
                {
                    lsi.AddParentGroup(group);
                }

                if (loading)
                {
                    group.Add(item);
                }
                else
                {
                    int localIndex = group.Insert(item, item, ActiveComparer);
                    int index = group.LeafIndexFromItem(item, localIndex);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
            }
            else if ((nameList = name as ICollection) == null)
            {
                // the item belongs to one subgroup
                AddToSubgroup(item, lsi, group, level, name, loading);
            }
            else
            {
                // the item belongs to multiple subgroups
                foreach (object o in nameList)
                {
                    AddToSubgroup(item, lsi, group, level, o, loading);
                }
            }
        }


        // add an item to the subgroup with the given name
        void AddToSubgroup(object item, LiveShapingItem lsi, CollectionViewGroupInternal group, int level, object name, bool loading)
        {
            CollectionViewGroupInternal subgroup;
            int index = (loading && IsDataInGroupOrder) ? group.LastIndex : 0;

            // find the desired subgroup using the map
            object groupNameKey = GetGroupNameKey(name, group);
            if (((subgroup = group.GetSubgroupFromMap(groupNameKey) as CollectionViewGroupInternal) != null) &&
                group.GroupBy.NamesMatch(subgroup.Name, name))
            {
                // Try best to set the LastIndex. If not possible reset it to 0.
                group.LastIndex = (group.Items[index] == subgroup ? index : 0);

                // Recursively call the AddToSubgroups method on subgroup.
                AddToSubgroups(item, lsi, subgroup, level + 1, loading);
                return;
            }

            // find the desired subgroup using linear search
            for (int n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as CollectionViewGroupInternal;
                if (subgroup == null)
                    continue;           // skip children that are not groups

                if (group.GroupBy.NamesMatch(subgroup.Name, name))
                {
                    group.LastIndex = index;

                    // Update the name to subgroup map on the group.
                    group.AddSubgroupToMap(groupNameKey, subgroup);

                    // Recursively call the AddToSubgroups method on subgroup.
                    AddToSubgroups(item, lsi, subgroup, level + 1, loading);
                    return;
                }
            }

            // the item didn't match any subgroups.  Create a new subgroup and add the item.
            subgroup = new CollectionViewGroupInternal(name, group);
            InitializeGroup(subgroup, group.GroupBy, level + 1);

            if (loading)
            {
                group.Add(subgroup);
                group.LastIndex = index;
            }
            else
            {
                group.Insert(subgroup, item, ActiveComparer);
            }

            // Update the name to subgroup map on the group.
            group.AddSubgroupToMap(groupNameKey, subgroup);

            // Recursively call the AddToSubgroups method on subgroup.
            AddToSubgroups(item, lsi, subgroup, level + 1, loading);
        }

        // move an item within the desired subgroup(s) of the given group
        void MoveWithinSubgroups(object item, CollectionViewGroupInternal group, int level, IList list, int oldIndex, int newIndex)
        {
            object name = GetGroupName(item, group.GroupBy, level);
            ICollection nameList;

            if (name == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                MoveWithinSubgroup(item, group, list, oldIndex, newIndex);
            }
            else if ((nameList = name as ICollection) == null)
            {
                // the item belongs to one subgroup
                MoveWithinSubgroup(item, group, level, name, list, oldIndex, newIndex);
            }
            else
            {
                // the item belongs to multiple subgroups
                foreach (object o in nameList)
                {
                    MoveWithinSubgroup(item, group, level, o, list, oldIndex, newIndex);
                }
            }
        }

        // move an item within the subgroup with the given name
        void MoveWithinSubgroup(object item, CollectionViewGroupInternal group, int level, object name, IList list, int oldIndex, int newIndex)
        {
            CollectionViewGroupInternal subgroup;

            // find the desired subgroup using the map
            object groupNameKey = GetGroupNameKey(name, group);
            if (((subgroup = group.GetSubgroupFromMap(groupNameKey)) != null) &&
                group.GroupBy.NamesMatch(subgroup.Name, name))
            {
                // Recursively call the MoveWithinSubgroups method on subgroup.
                MoveWithinSubgroups(item, subgroup, level + 1, list, oldIndex, newIndex);
                return;
            }

            // find the desired subgroup using linear search
            for (int index = 0, n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as CollectionViewGroupInternal;
                if (subgroup == null)
                    continue;           // skip children that are not groups

                if (group.GroupBy.NamesMatch(subgroup.Name, name))
                {
                    // Update the name to subgroup map on the group.
                    group.AddSubgroupToMap(groupNameKey, subgroup);

                    // Recursively call the MoveWithinSubgroups method on subgroup.
                    MoveWithinSubgroups(item, subgroup, level + 1, list, oldIndex, newIndex);
                    return;
                }
            }

            // the item didn't match any subgroups.  Something is wrong.
            // This could happen if the app changes the item's group name (by changing
            // properties that the name depends on) without notification.
            // We don't support this - the Move is just a no-op.  But assert (in
            // debug builds) to help diagnose the problem if it arises.
            Debug.Assert(false, "Failed to find item in expected subgroup after Move");
        }

        // move the item within its group
        void MoveWithinSubgroup(object item, CollectionViewGroupInternal group, IList list, int oldIndex, int newIndex)
        {
            if (group.Move(item, list, ref oldIndex, ref newIndex))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
            }
        }

        /// <summary>
        ///     Helper method to normalize the group name.
        ///     Normalization happens only if the cases where
        ///     PropertyGroupDescriptions are used with
        ///     case insensitive comparisons.
        /// </summary>
        object GetGroupNameKey(object name, CollectionViewGroupInternal group)
        {
            object groupNameKey = name;
            PropertyGroupDescription pgd = group.GroupBy as PropertyGroupDescription;
            if (pgd != null)
            {
                string nameStr = name as string;
                if (nameStr != null)
                {
                    if (pgd.StringComparison == StringComparison.OrdinalIgnoreCase ||
                        pgd.StringComparison == StringComparison.InvariantCultureIgnoreCase)
                    {
                        nameStr = nameStr.ToUpperInvariant();
                    }
                    else if (pgd.StringComparison == StringComparison.CurrentCultureIgnoreCase)
                    {
                        nameStr = nameStr.ToUpper(CultureInfo.CurrentCulture);
                    }
                    groupNameKey = nameStr;
                }
            }
            return groupNameKey;
        }

        // remove an item from the desired subgroup(s) of the given group.
        // Return true if the item was not in one of the subgroups it was supposed to be.
        bool RemoveFromSubgroups(object item, CollectionViewGroupInternal group, int level)
        {
            bool itemIsMissing = false;
            object name = GetGroupName(item, group.GroupBy, level);
            ICollection nameList;

            if (name == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                itemIsMissing = RemoveFromGroupDirectly(group, item);
            }
            else if ((nameList = name as ICollection) == null)
            {
                // the item belongs to one subgroup
                if (RemoveFromSubgroup(item, group, level, name))
                    itemIsMissing = true;
            }
            else
            {
                // the item belongs to multiple subgroups
                foreach (object o in nameList)
                {
                    if (RemoveFromSubgroup(item, group, level, o))
                        itemIsMissing = true;
                }
            }

            return itemIsMissing;
        }


        // remove an item from the subgroup with the given name.
        // Return true if the item was not in one of the subgroups it was supposed to be.
        bool RemoveFromSubgroup(object item, CollectionViewGroupInternal group, int level, object name)
        {
            CollectionViewGroupInternal subgroup;

            // find the desired subgroup using the map
            object groupNameKey = GetGroupNameKey(name, group);
            if (((subgroup = group.GetSubgroupFromMap(groupNameKey) as CollectionViewGroupInternal) != null) &&
                group.GroupBy.NamesMatch(subgroup.Name, name))
            {
                // Recursively call the RemoveFromSubgroups method on subgroup.
                return RemoveFromSubgroups(item, subgroup, level + 1);
            }

            // find the desired subgroup using linear search
            for (int index = 0, n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as CollectionViewGroupInternal;
                if (subgroup == null)
                    continue;           // skip children that are not groups

                if (group.GroupBy.NamesMatch(subgroup.Name, name))
                {
                    // Recursively call the RemoveFromSubgroups method on subgroup.
                    return RemoveFromSubgroups(item, subgroup, level + 1);
                }
            }

            // the item didn't match any subgroups.  It should have.
            return true;
        }


        // remove an item from the direct children of a group.
        // Return true if this couldn't be done.
        bool RemoveFromGroupDirectly(CollectionViewGroupInternal group, object item)
        {
            int leafIndex = group.Remove(item, true);
            if (leafIndex >= 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, leafIndex));
                return false;
            }
            else
            {
                return true;
            }
        }

        // the item did not appear in one or more of the subgroups it
        // was supposed to.  This can happen if the item's properties
        // change so that the group names we used to insert it are
        // different from the names used to remove it.  If this happens,
        // remove the item the hard way.
        void RemoveItemFromSubgroupsByExhaustiveSearch(CollectionViewGroupInternal group, object item)
        {
            // try to remove the item from the direct children
            if (RemoveFromGroupDirectly(group, item))
            {
                // if that didn't work, recurse into each subgroup
                // (loop runs backwards in case an entire group is deleted)
                for (int k = group.Items.Count - 1; k >= 0; --k)
                {
                    CollectionViewGroupInternal subgroup = group.Items[k] as CollectionViewGroupInternal;
                    if (subgroup != null)
                    {
                        RemoveItemFromSubgroupsByExhaustiveSearch(subgroup, item);
                    }
                }
            }
            else
            {
                // if the item was removed directly, we don't have to look at subgroups.
                // An item cannot appear both as a direct child and as a deeper descendant.
            }
        }


        // get the group name(s) for the given item
        object GetGroupName(object item, GroupDescription groupDescription, int level)
        {
            if (groupDescription != null)
            {
                return groupDescription.GroupNameFromItem(item, level, Culture);
            }
            else
            {
                return UseAsItemDirectly;
            }
        }
        #endregion private methods

        #region private fields
        CollectionView _view;
        IComparer _comparer;
        bool _isDataInGroupOrder = false;

        ObservableCollection<GroupDescription> _groupBy = new ObservableCollection<GroupDescription>();
        GroupDescriptionSelectorCallback _groupBySelector;
        static GroupDescription _topLevelGroupDescription;
        static readonly object UseAsItemDirectly = new NamedObject("UseAsItemDirectly");
        #endregion private fields

        #region private types
        private class TopLevelGroupDescription : GroupDescription
        {
            public TopLevelGroupDescription()
            {
            }

            // we have to implement this abstract method, but it should never be called
            public override object GroupNameFromItem(object item, int level, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
        #endregion private types
    }

    internal class AbandonedGroupItem
    {
        public AbandonedGroupItem(LiveShapingItem lsi, CollectionViewGroupInternal group)
        {
            _lsi = lsi;
            _group = group;
        }

        public LiveShapingItem Item { get { return _lsi; } }
        public CollectionViewGroupInternal Group { get { return _group; } }

        LiveShapingItem _lsi;
        CollectionViewGroupInternal _group;
    }
}

