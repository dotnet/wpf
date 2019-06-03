// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows ListView Group helper classes
//


using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Class representing collection of ListView GroupManagers
    class GroupManagerCollection
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Ensures GroupManager creation for the specified listview
        // This method will be called only from certain methods on the LV
        // Called from LV: FirstChild, LastChild, ElementFromPoint, GetFocus
        internal void EnsureCreation(IntPtr hwnd)
        {
            if (!Contains(hwnd))
            {
                _groupManagers[hwnd] = GroupManager.CreateGroupManager(hwnd);
            }
        }

        internal void Remove(IntPtr hwnd)
        {
            _groupManagers.Remove(hwnd);
        }

        // O(1)
        internal bool Contains(IntPtr hwnd)
        {
            return _groupManagers.ContainsKey(hwnd);
        }

        internal GroupManager this[IntPtr hwnd]
        {
            get
            {
                if (!WindowsListView.IsGroupViewEnabled(hwnd))
                {
                    // Group was disabled but we did not get the needed event
                    // since for some things events are not being sent
                    // (e.g: Going from some LV modes to - List mode, List mode does not have Groups)

                    // Microsoft - We may want to consider raising the event here
                    // The M7 work on checking if LE is valid however is the better way of going
                    // WindowsListView.RemoveGroupAndRaiseLogicalChangedEvent(_hwnd);
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // The group may have been discarded by the reorder winevent.
                EnsureCreation (hwnd);

                GroupManager manager = _groupManagers[hwnd] as GroupManager;
                if (manager == null)
                {
                    // E.G. Going from the List mode to the something that has Group will cause this

                    // Microsoft- We may want to consider raising the event here
                    // The M7 work on checking if LE is valid however is the better way of going
                    // WindowsListView.RaiseLogicalChangedEvent(hwnd);
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
                return manager;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        static private Hashtable _groupManagers = new Hashtable(10);

        #endregion Private Fields
    }

    // Class responsible for managing listview groups
    class GroupManager
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructor

        private GroupManager(int groups, IntPtr hwnd, bool isComctrlV6OnOsVerV6orHigher)
        {
            _groups = new ArrayList(groups);
            _hwnd = hwnd;
            _isComctrlV6OnOsVerV6orHigher = isComctrlV6OnOsVerV6orHigher;
        }

        #endregion Constructor

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        internal NativeMethods.Win32Rect GetGroupRc(int id)
        {
            Group group = GetGroup(id);

            if (group == null)
            {
                return NativeMethods.Win32Rect.Empty;
            }

            return GetGroupRcInternal(group);
        }
        internal NativeMethods.Win32Rect GetGroupRcByIndex(int index)
        {
            if (index >= _groups.Count)
            {
                return NativeMethods.Win32Rect.Empty;
            }

            return GetGroupRcInternal((Group)_groups[index]);
        }
        internal int[] GetGroupIds()
        {
            int count = _groups.Count;
            int[] groupIds = new int[count];

            for (int i = 0; i < count; i++)
            {
                groupIds[i] = ((Group)_groups[i])._groupID;
            }

            return groupIds;
        }
        internal bool IsGroupIdValid(int groupID)
        {
            int count = _groups.Count;

            for (int i = 0; i < count; i++)
            {
                Group group = (Group)_groups[i];

                if (group._groupID == groupID)
                {
                    return true;
                }
            }

            return false;
        }

        internal int GetGroupIdByIndex(int index)
        {
            if (index >= _groups.Count)
            {
                return -1;
            }

            return ((Group)_groups[index])._groupID;
        }
        
        internal int GroupCount()
        {
            return _groups.Count;
        }

        internal bool AreGroupsValid()
        {
            int count = _groups.Count;

            for (int i = 0; i < count; i++)
            {
                Group group = (Group)_groups[i];

                if (!ListViewHasGroup(_hwnd, group._groupID))
                {
                    return false;
                }
            }

            // Make sure that no new group have been added, try to match all the GroupId to an 
            // existing one.
            int itemCount = WindowsListView.GetItemCount (_hwnd);
            NativeMethods.LVITEM_V6 item = new NativeMethods.LVITEM_V6 ();

            item.mask = NativeMethods.LVIF_GROUPID;

            for (item.iItem = 0; item.iItem < itemCount; item.iItem++)
            {
                if (!XSendMessage.GetItem(_hwnd, ref item) || GetGroup(item.iGroupID) == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal GroupInfo GetGroupInfo(int groupID)
        {
            Group group = GetGroup(groupID);

            if (group != null)
            {
                return new GroupInfo(group.Items, group.Count);
            }

            // empty group info
            return GroupInfo.Null;
        }
        internal static GroupManager CreateGroupManager(IntPtr hwnd)
        {
            return InitializeManager(hwnd);
        }

        // detect whether the lv has a specified group
        internal static bool ListViewHasGroup(IntPtr hwnd, int groupID)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_HASGROUP, new IntPtr(groupID), IntPtr.Zero) != 0;
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields
        // structure containing group information
        internal struct GroupInfo
        {
            //------------------------------------------------------
            //
            //  Constructor
            //
            //------------------------------------------------------

            #region Constructor
            internal GroupInfo(int[] items, int count)
            {
                _items = items; // OK to do an assignment here, instead of deep copy
                _count = count;
            }
            #endregion Constructor

            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods
            static public bool operator true(GroupInfo info)
            {
                return info._items != null;
            }
            static public bool operator false(GroupInfo info)
            {
                return info._items == null;
            }
            static public bool operator !(GroupInfo info)
            {
                if (info)
                {
                    return false;
                }

                return true;
            }
            #endregion Public Methods

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods
            internal int IndexOf(int item)
            {
                int index = 0;

                while (index < _count)
                {
                    if (_items[index] == item)
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }
            #endregion Internal Methods

            //------------------------------------------------------
            //
            //  Internal Fields
            //
            //------------------------------------------------------

            #region Internal Fields

            internal int[] _items;
            internal int _count;
            static readonly internal GroupInfo Null = new GroupInfo(null, -1);

            #endregion Internal Fields
        }
        // collection of groups
        internal ArrayList _groups;
        // list hwnd
        internal IntPtr _hwnd;

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private Group GetGroup(int id)
        {
            int count = _groups.Count;

            Group current = null;
            for (int i = 0; i < count; i++)
            {
                current = (Group)(_groups[i]);
                if (current._groupID == id)
                {
                    return current;
                }
            }
            return null;
        }

        private static unsafe GroupManager InitializeManager(IntPtr hwnd)
        {
            bool isComctrlV6OnOsVerV6orHigher = Misc.IsComctrlV6OnOsVerV6orHigher(hwnd);
           
            int itemCount = WindowsListView.GetItemCount(hwnd);
            NativeMethods.LVITEM_V6 item = new NativeMethods.LVITEM_V6();
            item.mask = NativeMethods.LVIF_GROUPID;

            // The only place where the GroupManager gets constructed
            GroupManager manager = new GroupManager(itemCount, hwnd, isComctrlV6OnOsVerV6orHigher);
            
            if (isComctrlV6OnOsVerV6orHigher)
            {
                NativeMethods.LVITEMINDEX ii = new NativeMethods.LVITEMINDEX(-1, -1);

                int flags = NativeMethods.LVNI_VISIBLEONLY | NativeMethods.LVNI_VISIBLEORDER;

                // When a listview is being "grouped by" an item may be in more than one group.  The itemCount
                // is the number of unique items.  This loop may iterate for more than the unique items in the group.
                // We are taking advantage of that fact the the array list will expand if there are alot of duplicates.
                while (XSendMessage.XSend (hwnd, NativeMethods.LVM_GETNEXTITEMINDEX, new IntPtr(&ii), flags, Marshal.SizeOf(ii.GetType())))
                {
                    // need to convert the item id to a group id
                    NativeMethods.LVGROUP_V6 groupInfo = new NativeMethods.LVGROUP_V6();
                    groupInfo.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                    groupInfo.mask = NativeMethods.LVGF_GROUPID;
                    
                    bool lresult  = XSendMessage.XSend(hwnd, NativeMethods.LVM_GETGROUPINFOBYINDEX, new IntPtr(ii.iGroup), new IntPtr(&groupInfo), Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                    if (!lresult)
                    {
                        if (groupInfo.iGroupID == -1)
                        {
                            // A -1 here means that there are no duplicates in this grouped listview so
                            // we have to get the group the old way.  This is done for performance reasons.
                            break;
                        }
                        // no group for this item should never happen.  
                        // If it ever does the other items might ok so just keep going.
                        continue;
                    }
                    
                    if (!manager.Add(groupInfo.iGroupID, ii.iItem))
                    {
                        // we had problem adding item to the needed group at this point it makes no
                        // sense to continue
                        System.Diagnostics.Debug.Assert(false, "Cannot add item to the needed group");
                        return null;
                    }
                }
            }

            bool sortNeeded = false;
            // If the code above did not yield anything try this way.  This will work for 
            // listviews pre Vista and grouped listviews in vista that don't have duplicate items.
            if (manager.GroupCount() == 0)
            {
                // if we get the groups this way they need to be sorted.  The code above brings them in sorted.
                sortNeeded = true;
                int current = 0;
                while (current < itemCount)
                {
                    item.iItem = current;
                    if (XSendMessage.GetItem(hwnd, ref item) && manager.Add(item.iGroupID, item.iItem))
                    {
                        current++;
                    }
                    else
                    {
                        // we had problem adding item to the needed group at this point it makes no
                        // sense to continue
                        System.Diagnostics.Debug.Assert(false, "Cannot add item to the needed group");
                        return null;
                    }
                }
            }

            // Sort items within the group
            int groupsCount = manager.GroupCount();
            for (int i = 0; i < groupsCount; i++)
            {
                Group group = (Group)manager._groups[i];
                Array.Sort(group.Items, 0, group.Count, new SortGroupItems(hwnd));
            }


            // Depending on how we got the group info we may need to sort it.
            // In vista the the listview can put the list items in the correct order.
            // Pre vista or old ui (v5) will always need to be sorted.
            if (sortNeeded)
            {
                // Sort groups
                manager._groups.Sort(new SortGroups(hwnd));
            }
            
            return manager;
        }

        private unsafe int GetGroupHeaderHeight()
        {
            NativeMethods.LVGROUPMETRICS metric = new NativeMethods.LVGROUPMETRICS (sizeof(NativeMethods.LVGROUPMETRICS), NativeMethods.LVGMF_BORDERSIZE);
            XSendMessage.XSend(_hwnd, NativeMethods.LVM_GETGROUPMETRICS, IntPtr.Zero, new IntPtr(&(metric.cbSize)), metric.cbSize, XSendMessage.ErrorValue.NoCheck);

            return metric.Top + padding;
        }

        private bool Add(int id, int item)
        {
            Group group = GetGroup(id);
            if (group == null)
            {
                group = new Group(id, _hwnd, _isComctrlV6OnOsVerV6orHigher);
                _groups.Add(group);
            }
            // group already exist, simply add an item to it
            return group.Add(item);
        }

        // Retrieve the rect of the group.
        private NativeMethods.Win32Rect GetGroupRcInternal(Group group)
        {
            NativeMethods.Win32Rect rcGroup = group.GetGroupRect();
            if (rcGroup.IsEmpty)
            {
                // LVM_GETGROUPRECT failed.
                rcGroup = group.CalculateRectNoHeader();
                // add the header's height hence changing the rcGroup.top coordinate
                // increase top by subtracting header from it
                rcGroup.top -= GetGroupHeaderHeight();
            }
            return rcGroup;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Group header padding
        private const int padding = 12;
        
        private bool _isComctrlV6OnOsVerV6orHigher;
            
        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Nested classes
        //
        //------------------------------------------------------

        #region Nested classes

        // Implementation of IComparer used to
        // sort groups.
        // Note: we want to have groups sorted in the way they displayed to the user
        // Hence we cannot sort by group id, since if group has the lower id it does not mean
        // that it displayed before the group with the higher id.
        // Hence we will sort groups by the rect of the first item within the group
        private class SortGroups : IComparer
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal  SortGroups(IntPtr hwnd)
            {
                _hwnd = hwnd;
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  IComparer
            //
            //------------------------------------------------------

            #region IComparer

            int IComparer.Compare(object x, object y)
            {
                Group g1 = (Group)x;
                Group g2 = (Group)y;

                // piggy back on the SortGroupItem.Compare
                SortGroupItems helper = new SortGroupItems(_hwnd);
                return ((IComparer)helper).Compare(g1.Items[0], g2.Items[0]);
            }

            #endregion IComparer

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private IntPtr _hwnd;

            #endregion Private Fields
        }

        // Implementation of IComparer used to
        // sort items within group based on their rectangle

        // NOTE: After the sort the listviewitems DIRECTION in array will be the following (except when in Detail mode)
        //  0   1   2
        //  3   4   5
        //  6   7
        private class SortGroupItems : IComparer
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region  Constructors

            internal SortGroupItems(IntPtr hwnd)
            {
                _hwnd = hwnd;
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  IComparer
            //
            //------------------------------------------------------

            #region IComparer

            int IComparer.Compare(object x, object y)
            {
                int item1 = (int)x;
                int item2 = (int)y;

                // get the rect of 2 items
                NativeMethods.Win32Rect rc1;
                WindowsListView.GetItemRect(_hwnd, item1, NativeMethods.LVIR_BOUNDS, out rc1);

                NativeMethods.Win32Rect rc2;
                WindowsListView.GetItemRect(_hwnd, item2, NativeMethods.LVIR_BOUNDS, out rc2);


                // compare rectangles
                if (rc1.left < rc2.left || rc1.top < rc2.top)
                {
                    return -1;
                }
                else if (rc1.left != rc2.left || rc1.top != rc2.top)
                {
                    return 1;
                }
                return 0;
            }

            #endregion IComparer

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private IntPtr _hwnd;

            #endregion Private Fields
        }


        // Class describing single ListView Group
        private class Group
        {            
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal Group(int id, IntPtr hwnd, bool isComctrlV6OnOsVerV6orHigher)
            {
                _items = new int[_size];
                _groupID = id;
                _hwnd = hwnd;
                _index = -1;
                _isComctrlV6OnOsVerV6orHigher = isComctrlV6OnOsVerV6orHigher;
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            internal int Count
            {
                get
                {
                    return _index + 1;
                }
            }
            internal int[] Items
            {
                get
                {
                    return _items;
                }
            }

            internal unsafe NativeMethods.Win32Rect GetGroupRect()
            {
                NativeMethods.Win32Rect rect = new NativeMethods.Win32Rect();
                bool isCollapsed = WindowsListViewGroup.IsCollapsed(_hwnd, _groupID);
                rect.top = isCollapsed ? NativeMethods.LVGGR_HEADER : NativeMethods.LVGGR_GROUP;
                XSendMessage.XSend(_hwnd, NativeMethods.LVM_GETGROUPRECT,
                          new IntPtr(_groupID), new IntPtr(&rect), Marshal.SizeOf(rect.GetType()));
                
                Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref rect, 2);
                
                return rect;
            }

            internal NativeMethods.Win32Rect CalculateRectNoHeader()
            {
                NativeMethods.Win32Rect rcLv = NativeMethods.Win32Rect.Empty;

                if (!Misc.GetWindowRect(_hwnd, ref rcLv))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                // set top to the top coordinate of the first item
                NativeMethods.Win32Rect item;
                WindowsListView.GetItemRect(_hwnd, _items[0], NativeMethods.LVIR_BOUNDS, out item);

                NativeMethods.Win32Rect groupRc;
                groupRc.top = item.top;

                // left coordinate defined by the left coordinate of the listview
                groupRc.left = rcLv.left;

                int count = Count;
                // bottom defined by the bottom coordinate of the last item
                if (count > 1)
                {
                    // get the rect of the last item in the group
                    WindowsListView.GetItemRect(_hwnd, _items[count - 1], NativeMethods.LVIR_BOUNDS, out item);
                }

                groupRc.bottom = item.bottom;

                // right coordinate defined by lv.right
                groupRc.right = rcLv.right;

                // when vertical scrollbar is present take it into account
                if (WindowScroll.Scrollable(_hwnd, NativeMethods.SB_VERT))
                {
                    NativeMethods.Win32Rect rc = GetScrollbarRect();
                    int width = rc.right - rc.left;

                    if (Misc.IsControlRTL(_hwnd))
                    {
                        // Right to left mirroring style
                        groupRc.left += width;
                    }
                    else
                    {
                        groupRc.right -= width;
                    }
                }

                return groupRc;
            }

            // Add lvitem to the collection
            internal bool Add(int item)
            {
                // Check if we have an empty place in our array
                _index++;
                EnsureCapacity(_index);
                _items[_index] = item;
                return true;
            }


            #endregion Internal Methods


            //------------------------------------------------------
            //
            //  Internal Fields
            //
            //------------------------------------------------------

            #region Internal Fields

            // id of the group
            internal int _groupID;

            #endregion Internal Fields

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            // grow the size of _items if needed
            private void EnsureCapacity(int min)
            {
                System.Diagnostics.Debug.Assert(min <= _items.Length, "EnsureCapacity: min is > _items.Length");
                if (min == _items.Length)
                {
                    // grow _items by factor of 2
                    int[] temp = _items;
                    _items = new int[temp.Length * 2];
                    Array.Copy(temp, _items, temp.Length);
                }
            }

            // get rect of the v-scrollbar
            private NativeMethods.Win32Rect GetScrollbarRect()
            {
                NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
                sbi.cbSize = Marshal.SizeOf(sbi.GetType());

                if (Misc.GetScrollBarInfo(_hwnd, NativeMethods.OBJID_VSCROLL, ref sbi))
                {
                    return new NativeMethods.Win32Rect(sbi.rcScrollBar.left, sbi.rcScrollBar.top, sbi.rcScrollBar.right, sbi.rcScrollBar.bottom);
                }

                return NativeMethods.Win32Rect.Empty;
            }


            #endregion Private Methods

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // Collection of items in the group
            // Store lvitem indexies. Use array to prevent boxing/unboxing
            // Microsoft - In the future we may want to use generics here with the ArrayList
            // since we would be able to specify the type, it will prevent boxing/unboxing and will allow
            // us to save a lot of headache, since we can use such nice methods as Add, Count, e.t.c
            // Right now we need to take care of reallocating the array (since we do not know
            // how many items array may have ahead of time) and keeping the real Count (how many elements are there)
            // ourselves
            private int[] _items;
            private int _index; // current location in the array of items
            private IntPtr _hwnd; // lv hwnd
            private const int _size = 16;
            private bool _isComctrlV6OnOsVerV6orHigher;

            #endregion Private Fields
        }

        #endregion Nested classes
    }
}
