// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Support for Windows ListView Group
//


using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsListViewGroup : ProxyFragment, IGridProvider, IExpandCollapseProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructor

        internal WindowsListViewGroup (IntPtr hwnd, ProxyFragment parent, int groupID)
            : base (hwnd, parent, groupID)
        {
            _cControlType = ControlType.Group;
            _groupID = groupID;
            _sAutomationId = "Group " + (groupID + 1).ToString(CultureInfo.InvariantCulture); // This string is a non-localizable string

            _isComctrlV6OnOsVerV6orHigher = Misc.IsComctrlV6OnOsVerV6orHigher(hwnd);
           

        }

        #endregion Constructor

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            if (iid == GridPattern.Pattern)
            {
                return this;
            }
            else if (iid == ExpandCollapsePattern.Pattern && WindowsListView.IsGroupViewEnabled(_hwnd))
            {
                return this;
            }

            return null;
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                GroupManager manager = WindowsListView._groupsCollection [_hwnd];
                NativeMethods.Win32Rect itemRectangle = manager.GetGroupRc(ID);
                // Don't need to normalize, GetGroupRc returns absolute coordinates.
                return itemRectangle.ToRect(false);
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.ClassNameProperty)
            {
                return "";
            }

            return base.GetElementProperty (idProp);
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                if (_isComctrlV6OnOsVerV6orHigher)
                {
                    NativeMethods.LVGROUP_V6 group = new NativeMethods.LVGROUP_V6();
                    group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));

                    group.iGroupID = ID;
                    group.cchHeader = Misc.MaxLengthNameProperty;

                    return XSendMessage.GetItemText(_hwnd, group, NativeMethods.LVGF_HEADER);
                }
                else
                {
                    NativeMethods.LVGROUP group = new NativeMethods.LVGROUP();
                    group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP)));

                    group.iGroupID = ID;
                    group.mask = NativeMethods.LVGF_HEADER;
                    group.cchHeader = Misc.MaxLengthNameProperty;

                    return XSendMessage.GetItemText(_hwnd, group);
                }
            }
        }

        #endregion

        #region ProxyFragment

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            if (groupInfo)
            {
                int [] items = groupInfo._items;
                int current = child._item;

                // find the index of the current lvitem
                int nextLocation = groupInfo.IndexOf (current) + 1; //Array.IndexOf(items, current) + 1;

                if (nextLocation <= 0)
                {
                    // No more siblings
                    return null;
                }

                if (nextLocation < groupInfo._count)
                {
                    return CreateListViewItem (items [nextLocation]);
                }

                // List view groups in vista can have an extra link at the end that says
                // somthing like "show all 11 items..."
                if (_isComctrlV6OnOsVerV6orHigher && nextLocation == groupInfo._count)
                {
                    NativeMethods.LVGROUP_V6 group = new NativeMethods.LVGROUP_V6();
                    group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                    group.iGroupID = _groupID;
                    group.mask = NativeMethods.LVGF_STATE;
                    group.stateMask = NativeMethods.LVGS_SUBSETED;
                
                    // Note: return code of GetGroupInfo() is not reliable.
                    XSendMessage.GetGroupInfo(_hwnd, ref group); // ignore return code.

                    // The items array holds the list items in this group.  If we have a subset link we 
                    // don't store it with the list items because it isn't really a list item.  Instead we just
                    // create the subset link proxy with an item index one more than the last index.
                    if ((group.state & NativeMethods.LVGS_SUBSETED) != 0)
                    {
                        return CreateGroupSubsetLink(items [groupInfo._count - 1] + 1);
                    }
                }

            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            if (groupInfo)
            {
                int [] items = groupInfo._items;
                int current = child._item;

                // The subset link has an index that is one past the list items
                // If we are on that then the previous is the last item in the items array.
                if (_isComctrlV6OnOsVerV6orHigher && current == groupInfo._count)
                {
                    int index = items [groupInfo._count - 1];

                    return CreateListViewItem (index);
                }

                // find the index of the current lvitem
                int prevLocation = groupInfo.IndexOf (current) - 1;

                if (prevLocation <= -2)
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                if (prevLocation >= 0)
                {
                    return CreateListViewItem (items [prevLocation]);
                }
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            // return first item in this group
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            if (groupInfo)
            {
                int [] items = groupInfo._items;
                int index = items [0];

                return CreateListViewItem (index);
            }

            return null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            // return last item in this group
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            // List view groups in vista can have an extra link at the end that says
            // something like "show all 11 items...".  If one exists expose it as the last child.
            if (_isComctrlV6OnOsVerV6orHigher)
            {
                NativeMethods.LVGROUP_V6 group = new NativeMethods.LVGROUP_V6();
                group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                group.iGroupID = _groupID;
                group.mask = NativeMethods.LVGF_STATE;
                group.stateMask = NativeMethods.LVGS_SUBSETED;
                
                // Note: return code of GetGroupInfo() is not reliable.
                XSendMessage.GetGroupInfo(_hwnd, ref group); // ignore return code.

                // if we are not subseted then the last item is a regular listitem so
                // it is ok to fall through and let that be created.  Otherwise we need to 
                // create the subset link proxy.
                if ((group.state & NativeMethods.LVGS_SUBSETED) != 0)
                {
                    int [] items = groupInfo._items;
                    if (groupInfo._count <= 0 || groupInfo._count > items.Length)
                    {
                        return null;
                    }
                    
                    int index = items [groupInfo._count - 1];
                    return CreateGroupSubsetLink(index + 1);
                }
            }


            if (groupInfo)
            {
                int [] items = groupInfo._items;
                if (groupInfo._count <= 0 || groupInfo._count > items.Length)
                {
                    return null;
                }
                
                int index = items [groupInfo._count - 1];

                return CreateListViewItem (index);
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            NativeMethods.Win32Point pt = new NativeMethods.Win32Point (x, y);
            NativeMethods.LVHITTESTINFO_INTERNAL hitTest = WindowsListView.SubitemHitTest (_hwnd, pt);

            if ((hitTest.flags & NativeMethods.LVHT_EX_GROUP_HEADER) != 0)
            {
                return this;
            }

            if ((hitTest.flags & NativeMethods.LVHT_ONITEM) != 0 && hitTest.iItem >= 0)
            {
                // create the item
                return new ListViewItem (_hwnd, this, hitTest.iItem);
            }

            // If we did not land on an item we may be at a subset link these only exist
            // in v6 comctrl and vista or later.
            if (_isComctrlV6OnOsVerV6orHigher)
            {
                // Allocate a local LVHITTESTINFO struct.
                NativeMethods.LVHITTESTINFO_V6 hitTestNative = new NativeMethods.LVHITTESTINFO_V6(hitTest);
                unsafe
                {
                    XSendMessage.XSendGetIndex(_hwnd, NativeMethods.LVM_HITTEST, new IntPtr(-1), new IntPtr(&hitTestNative), Marshal.SizeOf(hitTestNative.GetType()));
                }

                if ((hitTestNative.flags & NativeMethods.LVHT_EX_GROUP_SUBSETLINK) != 0)
                {
                    GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);
                    int [] items = groupInfo._items;
                    if (groupInfo._count <= 0 || groupInfo._count > items.Length)
                    {
                        return null;
                    }
                    
                    int index = items [groupInfo._count - 1];
                    return CreateGroupSubsetLink(index + 1);
                }
            }
            
            return this;
        }
        
        protected override bool IsFocused ()
        {
            int groupIndex = (int)Misc.ProxySendMessage(_hwnd, NativeMethods.LVM_GETFOCUSEDGROUP, IntPtr.Zero, IntPtr.Zero);

            // need to convert the item id to a group id
            NativeMethods.LVGROUP_V6 groupInfo = new NativeMethods.LVGROUP_V6();
            groupInfo.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
            groupInfo.mask = NativeMethods.LVGF_GROUPID;

            unsafe
            {
                bool lresult  = XSendMessage.XSend(_hwnd, NativeMethods.LVM_GETGROUPINFOBYINDEX, new IntPtr(groupIndex), new IntPtr(&groupInfo), Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                if (!lresult)
                {
                    // no group for this item should never happen.  
                    return false;
                }
            }      
            
            if (groupInfo.iGroupID == _groupID)
            {   
                return true;
            }

            return false;
        }

        #endregion Interface Methods

        #region Grid Pattern

        // Obtain the AutomationElement at an zero based absolute position in the grid.
        // Where 0,0 is top left
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            int maxRow = GetRowCount (_hwnd, ID);
            int maxColumn = GetColumnCount(_hwnd, ID);

            if (row < 0 || row >= maxRow)
            {
                throw new ArgumentOutOfRangeException("row", row, SR.Get(SRID.GridRowOutOfRange));
            }

            if (column < 0 || column >= maxColumn)
            {
                throw new ArgumentOutOfRangeException("column", column, SR.Get(SRID.GridColumnOutOfRange));
            }

            if (WindowsListView.IsDetailMode (_hwnd))
            {
                return GetCellInDetailMode (row, column);
            }

            return GetCellInOtherModes (row, column, maxColumn);
        }

        int IGridProvider.RowCount
        {
            get
            {
                return GetRowCount (_hwnd, ID);
            }
        }

        int IGridProvider.ColumnCount
        {
            get
            {
                return GetColumnCount (_hwnd, ID);
            }
        }

        #endregion Grid Pattern

        #region ExpandCollapse Pattern

        private void CheckControlEnabled()
        {
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }
        }

        void IExpandCollapseProvider.Expand()
        {
            ExpandOrCollapse(false);
        }

        private bool IsCollapsed()
        {
            return IsCollapsed(_hwnd, _groupID);
        }

        internal static bool IsCollapsed(IntPtr hwnd, int groupID)
        {
            bool isCollapsed = false;
            NativeMethods.LVGROUP group = new NativeMethods.LVGROUP();
            group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP)));
            group.iGroupID = groupID;
            group.mask = NativeMethods.LVGF_STATE;
            group.stateMask = NativeMethods.LVGS_COLLAPSED;
            // Note: return code of GetGroupInfo() is not reliable.
            XSendMessage.GetGroupInfo(hwnd, ref group); // ignore return code.
            isCollapsed = (group.state & NativeMethods.LVGS_COLLAPSED) != 0;
            return isCollapsed;
        }

        // Hide all Children
        void IExpandCollapseProvider.Collapse()
        {
            ExpandOrCollapse(true);
        }

        // Indicates an elements current Collapsed or Expanded state
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                return IsCollapsed() ? ExpandCollapseState.Collapsed
                                     : ExpandCollapseState.Expanded;
            }
        }

        #endregion ExpandCollapse Pattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // get focused element
        internal static ProxySimple GetFocusInGroup (IntPtr hwnd, ProxyFragment parent)
        {
            int index = WindowsListView.GetItemNext(hwnd, -1, NativeMethods.LVNI_FOCUSED);

            if (index != -1)
            {
                // get id of the group to which item belongs
                NativeMethods.LVITEM_V6 item = new NativeMethods.LVITEM_V6 ();

                item.mask = NativeMethods.LVIF_GROUPID;
                item.iItem = index;
                if (XSendMessage.GetItem(hwnd, ref item))
                {
                    WindowsListViewGroup group = new WindowsListViewGroup (hwnd, parent, item.iGroupID);

                    return new ListViewItem (hwnd, group, index);
                }
            }
            else
            {
                // if none of the items have focus see if the focus is on the subset link
                // this only exists in v6 comctrl on vista or later.
                if (Misc.IsComctrlV6OnOsVerV6orHigher(hwnd))
                {
                    int groupIndex = (int)Misc.ProxySendMessage(hwnd, NativeMethods.LVM_GETFOCUSEDGROUP, IntPtr.Zero, IntPtr.Zero);

                    // need to convert the item id to a group id
                    NativeMethods.LVGROUP_V6 groupInfo = new NativeMethods.LVGROUP_V6();
                    groupInfo.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                    groupInfo.mask = NativeMethods.LVGF_GROUPID;

                    unsafe
                    {
                        bool lresult  = XSendMessage.XSend(hwnd, NativeMethods.LVM_GETGROUPINFOBYINDEX, new IntPtr(groupIndex), new IntPtr(&groupInfo), Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                        if (!lresult)
                        {
                            // no group for this item should never happen.  
                            return null;
                        }
                    }
                    
                    int groupId = groupInfo.iGroupID;
                    groupInfo.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                    groupInfo.iGroupID = groupId;
                    groupInfo.mask = NativeMethods.LVGF_STATE;
                    groupInfo.stateMask = NativeMethods.LVGS_SUBSETLINKFOCUSED;
                    
                    // Note: return code of GetGroupInfo() is not reliable.
                    XSendMessage.GetGroupInfo(hwnd, ref groupInfo); // ignore return code.

                    if ((groupInfo.state & NativeMethods.LVGS_SUBSETLINKFOCUSED) != 0)
                    {
                        GroupManager.GroupInfo groupManagerInfo = GetGroupInfo (hwnd, groupId);
                        int [] items = groupManagerInfo._items;
                        if (groupManagerInfo._count <= 0 || groupManagerInfo._count >= items.Length)
                        {
                            return null;
                        }
                        
                        int sslIndex = items [groupManagerInfo._count - 1];

                        // The items array holds the list items in this group.  If we have a subset link we 
                        // don't store it with the list items because it isn't really a list item.  Instead we just
                        // create the subset link proxy with an item index one more than the last index.
                        WindowsListViewGroup group = new WindowsListViewGroup (hwnd, parent, groupId);
                        return group.CreateGroupSubsetLink(sslIndex + 1);
                    }
                    else
                    {
                        return new WindowsListViewGroup (hwnd, parent, groupId);
                    }
                }
            }

            return null;
        }

        internal int ID
        {
            get
            {
                return _groupID;
            }
        }

        // Expose the ability to retrieve count of columns in the Group's grid to the outside proxies
        // LVItem will be one of customer
        // Do not call when LV in  the Detail mode
        static internal int GetColumnCountExternal (IntPtr hwnd, int groupID)
        {
            System.Diagnostics.Debug.Assert (!WindowsListView.IsDetailMode (hwnd), "GetColumnCountExternal: called when lv is in Detail mode");
            return GetCountOfItemsInDimension (hwnd, groupID, new IsNewItemInDimension (IsNewColumn));
        }

        // utility method returning object describing current group
        static internal GroupManager.GroupInfo GetGroupInfo (IntPtr hwnd, int groupID)
        {
            GroupManager.GroupInfo groupInfo = GroupManager.GroupInfo.Null;

            // if groupmanager is not available GetManager(hwnd)
            // will raise needed event
            GroupManager manager = WindowsListView._groupsCollection [hwnd];

            groupInfo = manager.GetGroupInfo (groupID);
            if (!groupInfo)
            {
                // LV control did not raise the needed event
                // Microsoft - We may want to consider raising the event here
                // The M7 work on checking if LE is valid however is the better way of going
                //  RemoveGroupAndRaiseLogicalChangedEvent(_hwnd);
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            return groupInfo;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // return the lvitem
        private ProxyFragment CreateListViewItem (int index)
        {
            return new ListViewItem (_hwnd, this, index);
        }

        private void ExpandOrCollapse(bool collapse)
        {
            CheckControlEnabled();

            // Check if item can be expanded or collapsed.
            bool isCollapsed = IsCollapsed();
            if ((!collapse && !isCollapsed) || (collapse && isCollapsed))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            NativeMethods.LVGROUP group = new NativeMethods.LVGROUP();
            group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP)));
            // Note:  If we set group.mask to LVGF_GROUPID | LVGF_STATE,
            // SetGroupInfo() will fail.  Setting LVGF_STATE alone works, however.
            group.mask = NativeMethods.LVGF_STATE;
            group.iGroupID = _groupID;
            group.stateMask = NativeMethods.LVGS_COLLAPSED;
            group.state = collapse ? NativeMethods.LVGS_COLLAPSED : 0;
            if (!XSendMessage.SetGroupInfo(_hwnd, group))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Grid.GetCell implementation for detail mode
        private IRawElementProviderSimple GetCellInDetailMode (int row, int column)
        {
            // Note: column can be used as it is
            // row corresponds to the index into the array of items that belong to current group
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            if (groupInfo)
            {
                int lvitemIndex = groupInfo._items [row];
                ProxyFragment lvItem = CreateListViewItem (lvitemIndex);

                return new ListViewSubItem (_hwnd, lvItem, column, lvitemIndex);
            }

            return null;
        }

        // Grid.GetCell implementation for lv that is not in detail mode
        private IRawElementProviderSimple GetCellInOtherModes(int row, int column, int maxColumn)
        {
            // Convert row, column into the index into the array of items that belong to
            // current group
            int indexIntoArray = row * maxColumn + column;
            GroupManager.GroupInfo groupInfo = GetGroupInfo (_hwnd, ID);

            if (!groupInfo)
            {
                return null;
            }

            if (indexIntoArray >= groupInfo._count)
            {
                // Return an empty cell
                return new EmptyGridItem (row, column, this);
            }

            // return cell
            return CreateListViewItem (groupInfo._items [indexIntoArray]);
        }

        static private bool IsGroupValid (IntPtr hwnd, int groupID)
        {
            GroupManager manager = WindowsListView._groupsCollection [hwnd];

            // check group validity
            if (!manager.IsGroupIdValid (groupID))
            {
                // Group was disabled but we did not get the needed event
                // since for some things events are not being sent
                // (e.g: Going from some LV modes to - List mode, List mode does not have Groups)
                // Microsoft - We may want to consider raising the event here
                // The M7 work on checking if LE is valid however is the better way of going
                // RemoveGroupAndRaiseLogicalChangedEvent(_hwnd);
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            return true;
        }

        // Detect if 2 lvitems are located in the same column
        // desicion is based on item's rect coordinates
        static private NewItemInDimension IsNewColumn (NativeMethods.Win32Rect rc1, NativeMethods.Win32Rect rc2)
        {
            // NOTE: Array of lvitems has the following DIRECTION:
            //  0   1   2
            //  3   4   5
            //  6   7
            // Due to the position of lvitems in the array (see above)
            // we may be looking at the item that is located in the column which is different from rc1's column but that we
            // have encountered already (e.g. Happens when we will compare rect of lvitem 3 with rect of lvitem 2, even though
            // lvitem 3 lives in the different column than lvitem 2, it nevertheless lives in the same column as lvitem 0)
            // The case when rc1.left would be > rc2.left will indicate the case above (we wrapped around)
            if (rc1.left < rc2.left)
            {
                return NewItemInDimension.New;
            }
            else 
            if (rc1.left > rc2.left)
            {
                // we wrapped around (e.g. we on lvitem 3)
                // if we at the point were the column in which rc2 lives already been encountered
                // we want to stop the enumeration, since we will not discover any new columns
                return NewItemInDimension.Stop;
            }

            // left coordinates are equal, this can indicate that there is only 1 column
            if (rc1.top >= rc2.top)
            {
                // Microsoft - We may want to consider raising the event here
                // The M7 work on checking if LE is valid however is the better way of going
                // RemoveGroupAndRaiseLogicalChangedEvent(_hwnd);
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            return NewItemInDimension.Stop;
        }

        // Detect if 2 lvitems are located in the same row
        // desicion is based on item's rect coordinates
        static private NewItemInDimension IsNewRow (NativeMethods.Win32Rect rc1, NativeMethods.Win32Rect rc2)
        {
            // NOTE: Array of lvitems has the following DIRECTION:
            //  0   1   2
            //  3   4   5
            //  6   7
            // For row it should be enough to only check the top or bottom coordinate
            // due to location of items (see above) in the array
            return (rc1.top != rc2.top) ? NewItemInDimension.New : NewItemInDimension.Same;
        }

        // retrieve number of columns in the group
        static private int GetColumnCount (IntPtr hwnd, int groupID)
        {
            if (WindowsListView.IsDetailMode (hwnd))
            {
                // check group for validity
                if (IsGroupValid (hwnd, groupID))
                {
                    // When lv in the detail mode, Group will have as many columns
                    // as there header items
                    int column = ListViewItem.GetSubItemCount (hwnd);

                    return (column <= -1) ? 0 : column;
                }

                return -1;
            }

            return GetCountOfItemsInDimension (hwnd, groupID, new IsNewItemInDimension (IsNewColumn));
        }

        // retrieve number of rows in the group
        static private int GetRowCount (IntPtr hwnd, int groupID)
        {
            if (WindowsListView.IsDetailMode (hwnd))
            {
                // When lv in Detail mode (with items shown in groups)
                // each lvitem will live in their own row
                // we need to detect how many items belong to the group,
                // this would correspond to the number of rows
                GroupManager.GroupInfo groupInfo = GetGroupInfo (hwnd, groupID);

                if (groupInfo)
                {
                    return groupInfo._count;
                }

                // Good place to send Grid Invalid event
                return -1;
            }

            return GetCountOfItemsInDimension (hwnd, groupID, new IsNewItemInDimension (IsNewRow));
        }

        // This method returns the count of either columns or rows
        // in the Grid.
        static private int GetCountOfItemsInDimension (IntPtr hwnd, int groupID, IsNewItemInDimension comparer)
        {
            // Algorithm:
            // Get the rect of the item.
            // Compare it using provided "comparer" with the previously obtained rect of the previous item in the grid
            // if comparer returns New increase the count
            int itemsCount = 0;
            GroupManager.GroupInfo groupInfo = GetGroupInfo (hwnd, groupID);

            if (groupInfo)
            {
                int [] items = groupInfo._items;
                NativeMethods.Win32Rect rc;
                NativeMethods.Win32Rect rcNext;

                // get coordinates of the first item in the grid
                if (WindowsListView.GetItemRect(hwnd, items[0], NativeMethods.LVIR_BOUNDS, out rc))
                {
                    NewItemInDimension result = NewItemInDimension.New;

                    itemsCount++; // at least one exist
                    for (int i = 1; result != NewItemInDimension.Stop && i < groupInfo._count; i++)
                    {
                        if (!WindowsListView.GetItemRect(hwnd, items[i], NativeMethods.LVIR_BOUNDS, out rcNext))
                        {
                            // Fail to get rc, makes no sense to continue
                            System.Diagnostics.Debug.Assert (false, "GetCountOfItemsInDimension: failed to get item rect");
                            return 0;
                        }

                        result = comparer (rc, rcNext);
                        if (result == NewItemInDimension.New)
                        {
                            // found a change in the rect
                            // we either have a new column or new raw
                            itemsCount++;

                            // update the rc with the new coordinates
                            rc = rcNext;
                        }
                    }
                }

                return itemsCount;
            }

            return -1;
        }

        private ProxySimple CreateGroupSubsetLink (int item)
        {
            return new ListViewGroupSubsetLink(_hwnd, this, item, _groupID);
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private enum NewItemInDimension
        {
            New,    // indicates that we found a new column or row
            Same,   // indicates that we did not find a new column or row
            Stop    // we should stop looking, since the rest of the items live in prev. seen col or row
        }
        // callback that compares 2 grid items based on their rect
        // return true - if item (rc2) either located in the new(never encountered before) row or column (depends on the call))
        // else returns false
        private delegate NewItemInDimension IsNewItemInDimension (NativeMethods.Win32Rect rc1, NativeMethods.Win32Rect rc2);
        // same as _item, but to be clear use _groupID
        private int _groupID;
        private bool _isComctrlV6OnOsVerV6orHigher;
        #endregion Private Fields

    }
}
