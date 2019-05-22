// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 ListView proxy
//


using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    //
    // ListView Logical Tree Diagram
    //
    //  ListView
    //    --Group 1 (if in "Group" mode, if not LVI gets promoted up)
    //      -----ListViewItem0...ListViewItemN
    //           ---------Cell (if in details mode)
    //    --Group 2(if in "Group" mode, if not LVI gets promoted up)
    //      -----ListViewItem0...ListViewItemN
    //           ---------Cell (if in details mode)
    //    --Header (If in detail mode)
    //      ----Header Item0...X (If in detail mode)
    //    --ScrollBar
    //       -----SmallDecrement
    //       -----LargeDecrement
    //       -----Thumb
    //       -----LargeIncrement
    //       -----SmallIncrement
    //
    //  NOTE: Hwnd-based tree  will look like this
    //
    //              ListView
    //                 |
    //                 |
    //                SysHeader32 (if available)
    //
    //   UIAutomation will discover a header for us and will hook it up
    //   to our navigation chain, nothing needs to be done on our side, it is just a magic
    //   Please do not add ANY SysHeader32 specific navigation code
    class WindowsListView: ProxyHwnd, ISelectionProvider, IScrollProvider, IGridProvider, IMultipleViewProvider, ITableProvider
    {

        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static WindowsListView ()
        {
            _groupEvents = new WinEventTracker.EvtIdProperty [3];
            _groupEvents [0]._evtId = NativeMethods.EventObjectReorder;
            _groupEvents [1]._evtId = NativeMethods.EventObjectHide;
            _groupEvents [2]._evtId = NativeMethods.EventObjectDestroy;
            _groupEvents [0]._idProp = _groupEvents [1]._idProp = _groupEvents [2]._idProp = 0;
        }

        internal WindowsListView (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // Set the strings to return properly the properties.
            if (IsDetailMode(hwnd))
            {
                _cControlType = ControlType.DataGrid;
            }
            else
            {
                _cControlType = ControlType.List;
            }

            // Can be focused
            _fIsKeyboardFocusable = true;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);

            // internally track some of the lv events
            WinEventTracker.AddToNotificationList (_hwnd, new WinEventTracker.ProxyRaiseEvents (WindowsListView.GroupSpecificEvents), _groupEvents, 3);
        }

        #endregion Constructors

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            WindowsListView lv = new WindowsListView(hwnd, null, 0);

            if( idChild == 0 )
                return lv;
            else
                return lv.CreateListViewItemCheckIfInGroup (idChild - 1);
        }

        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            switch (idObject)
            {
                case NativeMethods.OBJID_CLIENT :
                {
                    RaiseEventsOnClient(hwnd, eventId, idProp, idObject, idChild);
                    return;
                }

                case NativeMethods.OBJID_WINDOW :
                {
                    // Special case for logical element change for a list view item
                    if ((eventId == NativeMethods.EventObjectReorder) && (idProp as AutomationEvent) == AutomationElement.StructureChangedEvent)
                    {
                        WindowsListView wlv = new WindowsListView( hwnd, null, -1 );

                        AutomationInteropProvider.RaiseStructureChangedEvent( wlv, new StructureChangedEventArgs( StructureChangeType.ChildrenInvalidated, wlv.MakeRuntimeId() ) );
                        return;
                    }

                    break;
                }

                case NativeMethods.OBJID_VSCROLL:
                case NativeMethods.OBJID_HSCROLL:
                    // The NonClientArea proxy handles these events
                    break;

                default :
                {
                    ProxySimple el = new WindowsListView( hwnd, null, -1 );
                    if (el != null)
                    {
                        el.DispatchEvents( eventId, idProp, idObject, idChild );
                    }
                    break;
                }
            }
            
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        internal override object GetPatternProvider (AutomationPattern iid)
        {
            // selection is always supported
            if (iid == SelectionPattern.Pattern)
            {
                return this;
            }

            // Note that condition for grid should be true when condition for table is also true:
            // providers that implement table should impl grid at the same time.
            if (iid == GridPattern.Pattern && 
                    (IsDetailMode (_hwnd) || 
                     IsImplementingGrid (_hwnd) && GetItemCount (_hwnd) > 0))
            {
                return this;
            }

            if (iid == MultipleViewPattern.Pattern)
            {
                return this;
            }

            // table is supported only in the detail mode
            if (iid == TablePattern.Pattern && IsDetailMode (_hwnd))
            {
                return this;
            }

            return null;
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            bool hasGroup = IsGroupViewEnabled (_hwnd);
            int item = child._item;
            int count = -1;

            if (!hasGroup)
            {
                // Determine how many items are in the list view.
                count = GetItemCount (_hwnd);

                // Next for an item that does not exist in the list
                if (item >= count)
                {
                    throw new ElementNotAvailableException ();
                }

                // Check if index of the next item is in range
                if (item >= 0 && (item + 1) < count)
                {
                    // return a node to represent the requested item.
                    return CreateListViewItem (item + 1);
                }
            }

            if (hasGroup)
            {
                if (child is ListViewItem)
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                WindowsListViewGroup windowsListViewGroup = child as WindowsListViewGroup;
                if (windowsListViewGroup != null)
                {
                    // The group might have destroyed by an event between a first an now
                    _groupsCollection.EnsureCreation (_hwnd);

                    GroupManager manager = _groupsCollection[_hwnd];
                    int [] groupIds = manager.GetGroupIds ();
                    int groupsCount = groupIds.Length;

                    // if there are no groups this is an empty list
                    if (groupIds.Length == 0)
                    {
                        return null;
                    }
                    
                    // Navigation: from one group to another
                    int groupID = windowsListViewGroup.ID;
                    int location = Array.IndexOf (groupIds, groupID);
                    if (location == -1)
                        return null; // the ListView is updating so this group ID is no longer valid?

                    if (location + 1 < groupsCount)
                    {
                        return CreateListViewGroup (groupIds [location + 1]);
                    }
                }
            }

            return null;
        }

        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            // go to lv group if applicable
            // When to go to the Group:
            // A. If navigating from the scrollbar
            // B. When navigating from the another Group
            if (IsGroupViewEnabled (_hwnd))
            {
                // The group might have destroyed by an event between a first an now
                _groupsCollection.EnsureCreation (_hwnd);

                // note: navigation is done from last group to the first
                GroupManager manager = _groupsCollection [_hwnd];
                int [] groupIds = manager.GetGroupIds ();
                int groupsCount = groupIds.Length;

                // if there are no groups this is an empty list
                if (groupIds.Length == 0)
                {
                    return null;
                }

                // check if current child is a group
                WindowsListViewGroup windowsListViewGroup = child as WindowsListViewGroup;
                if (windowsListViewGroup != null)
                {
                    int groupID = windowsListViewGroup.ID;
                    int location = Array.IndexOf (groupIds, groupID);
                    if (location == -1)
                        return null; // the ListView is updating so this group ID is no longer valid?

                    if (location > 0)
                    {
                        return CreateListViewGroup(groupIds [location - 1]);
                    }
                }
                else
                {
                    // return last group
                    return CreateListViewGroup(groupIds [groupsCount - 1]);
                }
            }
                // in the case when lv in the Group mode
                // the lvitems will live under the corresponding group
                //if (!hasGroup)
                else
            {
                // LVItem can be called by:
                // A. Scrollbar
                // B. Another listviewitem
                int item = child._item;
                int count = GetItemCount (_hwnd);
                if (item > 0 && item < count)
                {
                    // navigation from one lvitem to another
                    return CreateListViewItem (item - 1);
                }

                // we may need to retrun either last lv item
                // or give control back to UIAutomation (e.g. From 1st lvitem to header)
                return item < 0 && count > 0 ? CreateListViewItem (count - 1) : null;
            }

            return null;
        }

        internal override ProxySimple GetFirstChild ()
        {
            // if LV is in Group mode, lvitems will live under the corresponding group
            bool hasGroup = IsGroupViewEnabled (_hwnd);
            int itemCount = GetItemCount(_hwnd);

            if (itemCount > 0)
            {
                if (!hasGroup)
                {
                    return CreateListViewItem(0);
                }

                // Navigate to the first group
                if (hasGroup)
                {
                    _groupsCollection.EnsureCreation(_hwnd);

                    GroupManager manager = _groupsCollection[_hwnd];
                    int[] groupIds = manager.GetGroupIds();

                    // if there are no groups this is an empty list
                    if (groupIds.Length == 0)
                    {
                        return null;
                    }

                    // return the first group
                    return CreateListViewGroup(groupIds[0]);
                }
            }

            // no content 
            return null;
        }

        internal override ProxySimple GetLastChild ()
        {
            bool hasGroup = IsGroupViewEnabled (_hwnd);

            // check for the group
            if (hasGroup)
            {
                // group view is enabled and we got here
                // now return the last group
                _groupsCollection.EnsureCreation (_hwnd);

                GroupManager manager = _groupsCollection [_hwnd];
                int [] groupIds = manager.GetGroupIds ();
                int groupsCount = groupIds.Length;

                // if there are no groups this is an empty list
                if (groupIds.Length == 0)
                {
                    return null;
                }
                
                return CreateListViewGroup (groupIds[groupsCount - 1]);
            }
                // if !group
                else
            {
                int count = GetItemCount (_hwnd);
                return count > 0 ? CreateListViewItem (count - 1) : null;
            }
        }

        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // The header is a window on top of the client area. 
            // If the point is the non client area, skip
            // Must return null if hit tested.
            if (PtInListViewHeader (x, y) || !PtInClientRect (_hwnd, x, y))
            {
                return null;
            }

            // in the case of the lv in the group mode, return a group at point(x,y)
            if (IsGroupViewEnabled (_hwnd))
            {
                _groupsCollection.EnsureCreation (_hwnd);

                // Let's locate the group that contains x,y
                GroupManager manager = WindowsListView._groupsCollection [_hwnd];
                int length = manager.GroupCount ();
                for (int i = 0; i < length; i++)
                {
                    NativeMethods.Win32Rect rc = manager.GetGroupRcByIndex (i);
                    if (Misc.PtInRect(ref rc, x, y))
                    {
                        // found a group where point belongs
                        int groupID = manager.GetGroupIdByIndex (i);
                        ProxyFragment group = new WindowsListViewGroup (_hwnd, this, groupID);
                        return ProxyFragment.DrillDownFragment (group, x, y);
                    }
                }
            }

            NativeMethods.LVHITTESTINFO_INTERNAL hitTest = WindowsListView.SubitemHitTest(_hwnd, new NativeMethods.Win32Point(x, y));
            if (hitTest.iItem >= 0)
            {
                // create the item
                ProxyFragment item = CreateListViewItemOrStartMenuItem(this, hitTest.iItem);
                return ProxyFragment.DrillDownFragment (item, x, y);
            }
            else if (hitTest.flags == NativeMethods.LVHT_NOWHERE && IsDetailMode(_hwnd))
            {
                // LVHT_NOWHERE means: The position is inside the list-view control's client window, but it is not over a list item.
                // The hit was in the non-client area of the ListView box, so adjust piont to get the point on a
                // sub-item to find which ListViewItem to create.  There is not adjustment needed in the y-coordinate
                // spaces since ListView's do not apply a non-client area to the top or the bottom of items.
                Rect boundingRectangle = BoundingRectangle;
                int xAdjustment = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXBORDER) + UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXFRAME);

                if (x - boundingRectangle.Left < xAdjustment)
                {
                    x += xAdjustment;
                }
                else if (boundingRectangle.Left + boundingRectangle.Width - x < xAdjustment)
                {
                    x -= xAdjustment;
                }

                hitTest = WindowsListView.SubitemHitTest(_hwnd, new NativeMethods.Win32Point(x, y));
                if (hitTest.iItem >= 0)
                {
                    // Since this original point was in the non-client area of the listview, the item being looked for
                    // is the item not one of it's subitems, so just return the ListViewItem.
                    return CreateListViewItemOrStartMenuItem(this, hitTest.iItem);
                }
            }

            // NOTE: Do not return this, since user might of specified point that belongs to the hwnd-control
            // that lives inside of the lv (e.g. header). If we return null UIAutomation will do the drilling
            return null;
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            if (IsGroupViewEnabled (_hwnd))
            {
                _groupsCollection.EnsureCreation (_hwnd);
                return WindowsListViewGroup.GetFocusInGroup (_hwnd, this);
            }

            // get the focused item
            int index = GetItemNext(_hwnd, -1, NativeMethods.LVNI_FOCUSED);

            if (index != -1)
            {
                return CreateListViewItemCheckIfInGroup (index);
            }

            return this;
        }

        #endregion

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            if (aidProps != null)
            {
                for (int i = 0, c = aidProps.Length; i < c; i++)
                {
                    if (aidProps [i] == TablePattern.ColumnHeadersProperty)
                    {
                        // Return array of the HeaderItems
                        IntPtr hwndHeader = ListViewGetHeader (_hwnd);
                        if (hwndHeader != IntPtr.Zero && SafeNativeMethods.IsWindowVisible (hwndHeader))
                        {
                            WindowsSysHeader header = (WindowsSysHeader) WindowsSysHeader.Create (hwndHeader, 0);
                            WinEventTracker.EvtIdProperty[] aEvents = new WinEventTracker.EvtIdProperty[] { new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectCreate, TablePattern.ColumnHeadersProperty) };
                            WinEventTracker.AddToNotificationList(hwndHeader, header._createOnEvent, aEvents, 1);
                        }
                    }
                }
            }

            if (eventId == InvokePattern.InvokedEvent)
            {
                WinEventTracker.EvtIdProperty[] aEvents = new WinEventTracker.EvtIdProperty[] { new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectSelection, eventId) };
                WinEventTracker.AddToNotificationList(_hwnd, _createOnEvent, aEvents, 1); 
            }

            base.AdviseEventAdded (eventId, aidProps);
        }

        internal override void AdviseEventRemoved (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            if (aidProps != null)
            {
                for (int i = 0, c = aidProps.Length; i < c; i++)
                {
                    if (aidProps [i] == TablePattern.ColumnHeadersProperty)
                    {
                        // Return array of the HeaderItems
                        IntPtr hwndHeader = ListViewGetHeader (_hwnd);
                        if (hwndHeader != IntPtr.Zero && SafeNativeMethods.IsWindowVisible (hwndHeader))
                        {
                            WindowsSysHeader header = (WindowsSysHeader) WindowsSysHeader.Create (hwndHeader, 0);
                            WinEventTracker.EvtIdProperty[] aEvents = new WinEventTracker.EvtIdProperty[] { new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectCreate, TablePattern.ColumnHeadersProperty) };
                            WinEventTracker.RemoveToNotificationList (hwndHeader, aEvents, header._createOnEvent, 1);
                        }
                    }
                }
            }

            if (eventId == InvokePattern.InvokedEvent)
            {
                WinEventTracker.EvtIdProperty[] aEvents = new WinEventTracker.EvtIdProperty[] { new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectSelection, eventId) };
                WinEventTracker.AddToNotificationList(_hwnd, _createOnEvent, aEvents, 1);
            }

            base.AdviseEventRemoved(eventId, aidProps);
        }

        #endregion

        #region SelectionPattern

        // Returns an array of elements that are current selection.
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            int count = GetItemCount (_hwnd);
            int countSelection = MultiSelected(_hwnd) ? GetSelectedItemCount(_hwnd) : 1;

            if (count <= 0 || countSelection <= 0 )
            {
                // this should be handled correctly in the framework
                return null;
            }

            IRawElementProviderSimple[] selection = new IRawElementProviderSimple[countSelection];
            int index = 0;

            for (int itemPos = GetItemNext(_hwnd, -1, NativeMethods.LVNI_SELECTED); itemPos != -1; itemPos = GetItemNext(_hwnd, itemPos, NativeMethods.LVNI_SELECTED))
            {
                selection[index] = CreateListViewItemCheckIfInGroup(itemPos);
                index++;
            }

            if (index == 0)
            {
                return null;
            }

            return selection;
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                // Get the style bits for the list view window.
                return MultiSelected (_hwnd);
            }
        }

        // Returns whether the control requires a minimum of one selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return false;
            }
        }


        #endregion SelectionPattern

        #region ScrollPattern

        void IScrollProvider.Scroll (ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            WindowScroll.Scroll (_hwnd, horizontalAmount, verticalAmount, true);
        }

        void IScrollProvider.SetScrollPercent (double horizontalPercent, double verticalPercent)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // get the "full size" of the list-view
            int size = ApproximateViewRect (_hwnd);

            // ApproximateViewRect adds the window edge size, substract it
            int cx = NativeMethods.Util.LOWORD (size) /*- 2 * UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXBORDER)*/;
            int cy = NativeMethods.Util.HIWORD (size) /*- 2 * UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CYBORDER)*/;

            // maximum width to fit all elements
            int dx, dy;
            bool fHz = SetScrollPercent (horizontalPercent, NativeMethods.SB_HORZ, cx, out dx);
            bool fVt = SetScrollPercent (verticalPercent, NativeMethods.SB_VERT, cy, out dy);

            if (fHz || fVt)
            {
                // scroll relative to the current thumb position
                bool fScrollSuccess = true;

                // if there is no movement need do not call Scroll().
                if (dx != 0 || dy != 0)
                {
                    fScrollSuccess = Scroll(_hwnd, (IntPtr)dx, (IntPtr)dy);

                    // On occasion in the listview control the new position of the scroll bar is off by
                    // one column/row. To deal with that bug in listview, we query the value we just set.
                    // If it differs then we try a second time to scroll the content. It is a scroll by
                    // just one column and this always succeeds.
                    // It is done both on hz and vt as a safety measure.
                    if (fScrollSuccess && (((int)horizontalPercent != (int)ScrollPattern.NoScroll && (int)horizontalPercent != (int)WindowScroll.GetPropertyScroll(ScrollPattern.HorizontalScrollPercentProperty, _hwnd))
                    || ((int)verticalPercent != (int)ScrollPattern.NoScroll && (int)verticalPercent != (int)WindowScroll.GetPropertyScroll(ScrollPattern.VerticalScrollPercentProperty, _hwnd))))
                    {
                        SetScrollPercent(horizontalPercent, NativeMethods.SB_HORZ, cx, out dx);
                        SetScrollPercent(verticalPercent, NativeMethods.SB_VERT, cy, out dy);

                        // if there is no movement need do not call Scroll() again.
                        if (dx != 0 || dy != 0)
                        {
                            Scroll(_hwnd, (IntPtr)dx, (IntPtr)dy);
                        }
                    }
                }

                if (fHz && fVt && fScrollSuccess)
                {
                    return;
                }
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        // Calc the position of the horizontal scroll bar thumb in the 0..100 % range
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                // use the common implementation for all the windows based controls
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.HorizontalScrollPercentProperty, _hwnd);
            }
        }

        // Calc the position of the Vertical scroll bar thumb in the 0..100 % range
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                // use the common implementation for all the windows based controls
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.VerticalScrollPercentProperty, _hwnd);
            }
        }

        // Percentage of the window that is visible along the horizontal axis. 
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                // use the common implementation for all the windows based controls
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.HorizontalViewSizeProperty, _hwnd);
            }
        }

        // Percentage of the window that is visible along the vertical axis. 
        double IScrollProvider.VerticalViewSize
        {
            get
            {
                // use the common implementation for all the windows based controls
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.VerticalViewSizeProperty, _hwnd);
            }
        }

        // Can the element be horizontaly scrolled
        bool IScrollProvider.HorizontallyScrollable
        {
            get
            {
                // use the common implementation for all the windows based controls
                return (bool) WindowScroll.GetPropertyScroll (ScrollPattern.HorizontallyScrollableProperty, _hwnd);
            }
        }

        // Can the element be verticaly scrolled
        bool IScrollProvider.VerticallyScrollable
        {
            get
            {
                return (bool) WindowScroll.GetPropertyScroll (ScrollPattern.VerticallyScrollableProperty, _hwnd);
            }
        }

        #endregion ScrollPattern

        #region Grid Pattern

        // Obtain the AutomationElement at an zero based absolute position in the grid.
        // Where 0,0 is top left
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            int maxRow = GetRowCount (_hwnd);
            int maxColumn = GetColumnCount (_hwnd);

            if (row < 0 || row >= maxRow)
            {
                throw new ArgumentOutOfRangeException("row", row, SR.Get(SRID.GridRowOutOfRange));
            }

            if (column < 0 || column >= maxColumn)
            {
                throw new ArgumentOutOfRangeException("column", column, SR.Get(SRID.GridColumnOutOfRange));
            }

            // GetCell
            if (IsDetailMode (_hwnd))
            {
                return GetCellInDetailMode (row, column);
            }

            return GetCellInOtherModes (row, column, maxColumn, maxRow);
        }

        int IGridProvider.RowCount
        {
            get
            {
                return GetRowCount (_hwnd);
            }
        }

        int IGridProvider.ColumnCount
        {
            get
            {
                return GetColumnCount (_hwnd);
            }
        }

        #endregion #region Grid Pattern

        #region Table Pattern

        // Collection of all Row Headers associated with the Table. Order is consistent with the table
        IRawElementProviderSimple [] ITableProvider.GetRowHeaders ()
        {
            return null;
        }

        // Collection of all Column Headers associated with the Table. Order is consistent with the table
        IRawElementProviderSimple [] ITableProvider.GetColumnHeaders ()
        {
            // Return array of the HeaderItems
            IntPtr hwndHeader = ListViewGetHeader (_hwnd);
            if (hwndHeader != IntPtr.Zero && SafeNativeMethods.IsWindowVisible (hwndHeader))
            {
                WindowsSysHeader header = (WindowsSysHeader) WindowsSysHeader.Create (hwndHeader, 0);
                int size = HeaderItemCount (hwndHeader);
                if (size > 0)
                {
                    IRawElementProviderSimple [] columns = new IRawElementProviderSimple [size];
                    for (ProxySimple headerItem = header.GetFirstChild (); headerItem != null; headerItem = header.GetNextSibling (headerItem))
                    {
                        columns [headerItem._item] = headerItem;
                    }
                    return columns;
                }
            }
            return null;
        }

        // Describe the best way to present the information within this table. 
        RowOrColumnMajor ITableProvider.RowOrColumnMajor
        {
            get
            {
                return RowOrColumnMajor.RowMajor;
            }
        }


        #endregion Table Pattern

        #region MultipleViewPattern

        // In the future use the official table for different views
        string IMultipleViewProvider.GetViewName (int viewID)
        {
            if ( viewID < 0 || viewID > ListViewViews.Length )
            {
                throw new ArgumentException( SR.Get( SRID.InvalidParameter ) );
            }
            return ListViewViews [viewID];
        }

        void IMultipleViewProvider.SetCurrentView (int viewID)
        {
            if ( viewID < 0 || viewID > ListViewViews.Length )
            {
                throw new ArgumentException( SR.Get( SRID.InvalidParameter ) );
            }
            // App specific: App will provide us with the SupportedViews array
            // How this would be done is TBD

            // If requested view is in array, than do a Set
            // {
            //     return ListViewSetView(_hwnd, viewID);
            // }

            // currently do nothing
        }

        int [] IMultipleViewProvider.GetSupportedViews ()
        {
            // This needs to be finalyzed:
            // App specific: App will provide us with the SupportedViews array
            // How this would be done is TBD
            // For now simply return the array of 1 element - containing the current view
            return new int [] { ListViewGetView (_hwnd) };
        }

        int IMultipleViewProvider.CurrentView
        {
            get
            {
                return ListViewGetView (_hwnd);
            }
        }


        #endregion MultipleViewPattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // set focus to the specified item
        static internal bool SetItemFocused (IntPtr hwnd, int item)
        {
            return SetItemState(hwnd, item, NativeMethods.LVIS_FOCUSED, NativeMethods.LVIS_FOCUSED);
        }

        // set focus to the specified item
        static internal bool IsItemFocused (IntPtr hwnd, int item)
        {
            int state = GetItemState(hwnd, item, NativeMethods.LVIS_FOCUSED);

            return (Misc.IsBitSet(state, NativeMethods.LVIS_FOCUSED));
        }

        // detect if listview is in detail mode
        static internal bool IsDetailMode (IntPtr hwnd)
        {
            int view = ListViewGetView (hwnd);

            // Current LH builds (4059) display the header even in tile view, 
            // which makes us think we are in details view. 
            // Explicitly detect tile view and return false. 
            if (view == NativeMethods.LV_VIEW_TILE)
            {
                return false;
            }

            if (InReportView(hwnd) || (view == NativeMethods.LV_VIEW_DETAILS))
            {
                return true;
            }

            if (Environment.OSVersion.Version.Major < 6)
            {
                // handle lv that not LVS_REPORT but act like one
                // e.g. Window explorer
                // NOTE: in XP version the NativeMethods.LV_VIEW_DETAILS check will cover Windows
                // Explorer, but in order to work for before XP lv we need the code below.
                IntPtr hwndHeader = ListViewGetHeader(hwnd);

                return SafeNativeMethods.IsWindowVisible(hwndHeader);
            }
            else
            {
                // No need to examine the listview header on Vista, since
                // ListViewGetView() suffices.
                return false;
            }
   
         }

        // detect if listview is in list mode
        static internal bool IsListMode (IntPtr hwnd)
        {
            if (ListViewList(hwnd) || (NativeMethods.LV_VIEW_LIST == ListViewGetView(hwnd)))
            {
                return true;
            }

            return false;
        }

        // detect if given listview should support Grid pattern
        static internal bool IsImplementingGrid (IntPtr hwnd)
        {
            // in the case when Group is enabled Group will support
            // Grid pattern rather than ListView
            if (IsGroupViewEnabled (hwnd))
            {
                return false;
            }

            // Rules for supporting grid pattern:
            // 1. ListView is in detail mode
            // 2. ListView is in the list mode
            // 3. Any other modes and LVS_AUTOARRANGE is set
            if (IsDetailMode (hwnd) || IsListMode (hwnd) || ListViewAutoArrange (hwnd))
            {
                return true;
            }

            return false;
        }

        // retrieve count of columns in the listview
        static internal int GetColumnCount (IntPtr hwnd)
        {
            if (IsDetailMode (hwnd))
            {
                int column = ListViewItem.GetSubItemCount (hwnd);

                return (column <= -1) ? 0 : column;
            }

            return GetColumnCountOtherModes (hwnd);
        }

        // retrieve count of rows in the listview
        static internal int GetRowCount (IntPtr hwnd)
        {
            if (IsDetailMode (hwnd))
            {
                int row = GetItemCount (hwnd);

                return (row <= -1) ? 0 : row;
            }

            return GetRowCountOtherModes (hwnd);
        }

        // get count of column in the non-detail lv
        static internal int GetColumnCountOtherModes (IntPtr hwnd)
        {
            // Check for empty list
            if (GetItemCount(hwnd) <= 0)
            {
                return 0;
            }

            // Algorithm for non-list mode of ListView with at least one item:
            // Starting from the first item, count the items to the right of it.
            // Any loc issues here?  In RTOL is item 0 at far left?
            int columnCount = 0;
            int curItem = 0;
            while (true)
            {
                columnCount++;
                int nextItem = GetItemNext(hwnd, curItem, NativeMethods.LVNI_TORIGHT);
                // Expect -1 when no more items to right of current item
                if (nextItem < 0)
                    break;
                // Guard against infinite loop (getting back the same item LH BUG in SysListView32))
                if (nextItem == curItem)
                    break;

                // Assumption: As long as nextItem is changing everything is OK
                // Note: Docs imply it may be possible for nextItem < curItem at this
                // point so don't assume nextItem is always increasing.
                curItem = nextItem;
            }

            return columnCount;
        }

        // get count of row for the listview when it is in the list mode
        static internal int GetRowCountListMode (IntPtr hwnd, int itemCount)
        {
            // NOTE: ListView in the List mode is tricky
            // In the List mode during the navigation columns getting
            // wrapped hence the number of rows we'll get by simply doing
            // LVNI_BELOW will be same as maximum number of elements
            // Algorithm: get the item position  while doing GetItemNext(,,LVNI_BELOW)
            // as long as pt.x is the same we on the same column
            // as soon as pt.x is changed we know we jump to the different column and hence we know the number of rows
            // This is true except:
            // If user had Groups shown, and than changed to the List mode (List mode does not have groups)
            // the List will not be snaking anymore (Windows Explorer LV bug on XP), hence after we come to the end of the first column
            // the GetItemNext(,,LVNI_BELOW) will return -1. all other case list will snake
            // Lucky for us at this point rowCount will contain the number of rows
            int columnCount = GetColumnCountOtherModes (hwnd);

            if (columnCount == 1)
            {
                // list does not snake, number of elements == number or rows
                return itemCount;
            }

            // We know that list has at least itemCount/columnCount rows
            int rowCount = (int) System.Math.Ceiling (((double) itemCount) / columnCount);

            NativeMethods.Win32Point pt = new NativeMethods.Win32Point (0, 0);

            // items are 0-based
            int current = rowCount - 1;
            if (!GetItemPosition(hwnd, current, out pt))
            {
                return 0;
            }

            int pos = pt.x;
            while (true)
            {
                int next = GetItemNext(hwnd, current, NativeMethods.LVNI_BELOW);
                // Expect -1 when no more items below
                if (next == -1)
                    return rowCount;

                // Guard against infinite loop (LH BUG in SysListView32)
                if (next == current)
                    return rowCount;

                // Get this next item's top-left coordinate
                if (!GetItemPosition(hwnd, next, out pt))
                    return rowCount;

                // If we're not on the same left-most x-axis we've got the row count
                if (pos != pt.x)
                    return rowCount;

                ++rowCount;
                current = next;
            }
        }

        // detect if group view is enabled
        internal static bool IsGroupViewEnabled (IntPtr hwnd)
        {
            return (ListViewIsGroupViewEnabled(hwnd) && NativeMethods.LV_VIEW_LIST != ListViewGetView(hwnd));
        }

        // Events produced by the LV, that will help us working with the Group
        internal static void GroupSpecificEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            // NOTE: Whenever LV goes from Group mode to non-Group mode we will be getting an event
            //       Whenver LV goes from one Group mode to another (arrange By: Name - Size) we will be getting an event
            // BUT
            //      Whenver LV switched between Views : Icond - Detail, events are not always getting send -> This one is ok since
            //      it does not affect Grouping (e.g. Groups do not change), except when switching from any View (with Group on) to
            //                                   List mode (List mode does not have group)
            //
            //      Whenver LV goes from non group mode to Group mode, events pretty much never getting generated
            //
            //      We will detect, in the code, when user got a stale logical tree and tries to use element from this tree. We will throw an exception
            //      indicating invalid operation (e.g. InvalidOperationException("Operation cannot be performed");)
            //      If user does listens to the events, and still got the stale tree (due to the LV control not raising needed events)
            //      the exception will be thrown in any case. The events will be raised on LH as soon as LV event-related bugs are fixed. On XP
            //      user can catch the exception and do the needed thing, or use the verification method (should be done in M7) on LE to validate it.
            switch (eventId)
            {
                case NativeMethods.EventObjectReorder :
                    {
                        // First check if we are in the Groupmode
                        if (IsGroupViewEnabled (hwnd))
                        {
                            // reorder event may  mean:
                            // 1. What lv is grouped by changed.... (Need an update and an event to be sent)
                            // 2. View changed (e.g.icone -> tiles)     (No action needed group did no change)
                            // 3. something as simple as resize     (No action needed)
                            // To detect case 1 we need to make sure that our ids are still valid
                            if (_groupsCollection.Contains (hwnd))
                            {
                                GroupManager manager = _groupsCollection [hwnd];

                                if (!manager.AreGroupsValid ())
                                {
                                    // went from one arrangement to another... (e.g. Name->Size)
                                    RemoveGroupAndRaiseLogicalChangedEvent (hwnd);
                                }
                            }
                            else
                            {
                                // New GroupManager showed in the LV
                                RaiseLogicalChangedEvent (hwnd);
                            }
                        }
                        else
                        {
                            // This can mean that Group mode got disable
                            // we need to remove the corresponding GroupManager from collection
                            // and raise an event
                            // NOTE: Unfortunately LV will not produce event on this consistently (e.g. Any mode->List)
                            if (_groupsCollection.Contains (hwnd))
                            {
                                // groups were removed
                                RemoveGroupAndRaiseLogicalChangedEvent (hwnd);
                            }
                        }
                    }
                    break;

                case NativeMethods.EventObjectDestroy :
                    {
                        // lv is being destroyed...
                        if (_groupsCollection.Contains (hwnd))
                        {
                            _groupsCollection.Remove (hwnd);
                            WinEventTracker.RemoveToNotificationList (hwnd, _groupEvents, null, 3);
                        }
                    }
                    break;

                case NativeMethods.EventObjectHide :
                    {
                        // Microsoft : Explorer LV does not send DESTROY event during destruction, instead EVENT_OBJECT_HIDE is getting send.
                        // OBJECT_HIDE can also be raised by Explorer during Arrange Icons By (e.g. Any->Type), we do not want to "remove" groupmanager
                        // and event notification if this is the case, hence we're are checking window's visible and enable state...
                        // There is however some timing issue: The window handle sometimes still will be visible and enabled even though OBJECT_HIDE
                        // event was raised in the response to LV going away:
                        // Hence our "remove" code may not be executing all the time
                        if (_groupsCollection.Contains (hwnd) && !SafeNativeMethods.IsWindowVisible (hwnd) && !SafeNativeMethods.IsWindowEnabled (hwnd))
                        {
                            _groupsCollection.Remove (hwnd);
                            WinEventTracker.RemoveToNotificationList (hwnd, _groupEvents, null, 3);
                        }
                    }
                    break;
            }
        }

        // detect if the listview is in LVS_REPORT mode
        static internal bool InReportView (IntPtr hwnd)
        {
            return ((Misc.GetWindowStyle(hwnd) & NativeMethods.LVS_TYPEMASK) == NativeMethods.LVS_REPORT);
        }

        // Removes group from collection
        // and notifies client about LV tree structure change
        static internal void RemoveGroupAndRaiseLogicalChangedEvent (IntPtr hwnd)
        {
            // Raise logical structure changed event
            RaiseLogicalChangedEvent (hwnd);
        }

        // Invalidate LV tree structure
        static internal void RaiseLogicalChangedEvent (IntPtr hwnd)
        {
            // remove groupmanager from collection
            _groupsCollection.Remove (hwnd);

            // Raise logical structure changed event
            IRawElementProviderFragment wlv = (IRawElementProviderFragment) new WindowsListView (hwnd, null, -1);

            // Note we're using MakeRuntimeId() vs IRawElementProviderFragment.GetRuntimeId().  GetRuntimeId 
            // only returns the part of the RuntimeId for the subtree this provider is handling.  When returning
            // RuntimeId for an event the entire RuntimeId is required so use MakeRuntimeId().
            StructureChangedEventArgs change = new StructureChangedEventArgs( StructureChangeType.ChildrenInvalidated, ( (WindowsListView)wlv ).MakeRuntimeId() );
            AutomationInteropProvider.RaiseStructureChangedEvent(wlv, change);
        }

        // get listview item count
        static internal int GetItemCount (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        // get listview item count os selected items
        static internal int GetSelectedItemCount (IntPtr hwnd)
        {
            if (GetItemCount (hwnd) <= 0)
                return 0;
            
            int count = 0;
            for (int index = GetItemNext(hwnd, -1, NativeMethods.LVNI_SELECTED); index != -1; index = GetItemNext(hwnd, index, NativeMethods.LVNI_SELECTED))
            {
                count++;
            }

            return count;
        }

        static internal int GetStartOfSelectedItems (IntPtr hwnd)
        {
            return GetItemNext(hwnd, -1, NativeMethods.LVNI_SELECTED);
        }

        // Search for the next listview item based on the passed in properties
        // pass -1 for item in order to find the first item that matches condition
        // specified by flags
        static internal int GetItemNext (IntPtr hwnd, int item, int flags)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_GETNEXTITEM, new IntPtr(item), new IntPtr(flags));
        }

        static internal bool IsIconView(IntPtr hwnd)
        {
            return ListViewGetView(hwnd) == NativeMethods.LV_VIEW_ICON;
        }

        // Retrieves the current view of the listview control
        static internal int ListViewGetView (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_GETVIEW, IntPtr.Zero, IntPtr.Zero);
        }

        // simple version of ApproxiamateViewRect
        static internal int ApproximateViewRect (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_APPROXIMATEVIEWRECT, new IntPtr(-1), NativeMethods.Util.MAKELPARAM(-1, -1));
        }

        // Scroll the content of the listview control
        static internal bool Scroll (IntPtr hwnd, IntPtr dx, IntPtr dy)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_SCROLL, dx, dy) != 0;
        }

        // get listview rectangle
        static internal unsafe bool GetItemRect (IntPtr hwnd, int item, int lvir, out NativeMethods.Win32Rect itemRectangle)
        {
            itemRectangle = NativeMethods.Win32Rect.Empty;
            itemRectangle.left = lvir;

            fixed (int * location = &(itemRectangle.left))
            {
                if (XSendMessage.XSend(hwnd, NativeMethods.LVM_GETITEMRECT, new IntPtr(item), new IntPtr(location), Marshal.SizeOf(itemRectangle.GetType())))
                {
                    return Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref itemRectangle, 2);
                }

                return false;
            }
        }

        // check if lv has a group view enabled
        internal static bool ListViewIsGroupViewEnabled (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_ISGROUPVIEWENABLED, IntPtr.Zero, IntPtr.Zero) != 0;
        }

        // unselect all items in the listview
        static internal bool UnselectAll (IntPtr hwnd)
        {
            return SetItemState(hwnd, -1, NativeMethods.LVIS_SELECTED, 0);
        }

        // select specified listview item
        static internal bool SelectItem (IntPtr hwnd, int item)
        {
            return SetItemState(hwnd, item, NativeMethods.LVIS_SELECTED, NativeMethods.LVIS_SELECTED);
        }

        // un-select specified listview item
        static internal bool UnSelectItem (IntPtr hwnd, int item)
        {
            return SetItemState(hwnd, item, NativeMethods.LVIS_SELECTED, 0);
        }

        // detect if listview item selected
        static internal bool IsItemSelected (IntPtr hwnd, int listItem)
        {
            return Misc.IsBitSet(GetItemState(hwnd, listItem, NativeMethods.LVIS_SELECTED), NativeMethods.LVIS_SELECTED);
        }

        // detect if listviewitem has label that can be edited
        static internal bool ListViewEditable (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.LVS_EDITLABELS);
        }

        // detect if listviewitem can be invoked
        static internal bool ListViewInvokable(IntPtr hwnd)
        {
            int style = GetExtendedListViewStyle(hwnd);

            // Listview documentation suggests LVS_EX_ONECLICKACTIVATE or LVS_EX_TWOCLICKACTIVATE
            // indicate an item that can be activated, but Explorer listview items do not provide 
            // either of these flags.  They do however contain the values LVS_EX_UNDERLINEHOT and 
            // LVS_EX_UNDERLINECOLD.  Documentation for these flags indicate they're specifically
            // for items that can be activated, and that they have no impact if one of the other
            // two values is not set:
            // 
            // LVS_EX_UNDERLINEHOT
            // Causes those hot items that may be activated to be displayed with underlined text.
            //
            // LV_EX_UNDERLINE_COLD
            // Causes those non-hot items that may be activated to be displayed with underlined text.
            //
            // This code tests for both sets of styles since the presence of the HOT or COLD bits
            // implies items that can be activated and supports our observations of Explorer.

            int flags = NativeMethods.LVS_EX_ONECLICKACTIVATE
                | NativeMethods.LVS_EX_TWOCLICKACTIVATE
                | NativeMethods.LVS_EX_UNDERLINEHOT
                | NativeMethods.LVS_EX_UNDERLINECOLD;

            return ((style & flags) != 0);
        }

        static internal IntPtr ListViewEditLabel(IntPtr hwnd, int item)
        {
            return Misc.ProxySendMessage(hwnd, NativeMethods.LVM_EDITLABEL, new IntPtr(item), IntPtr.Zero);
        }

        // detect if listview enables item activation with one click
        static internal bool ListViewSingleClickActivate (IntPtr hwnd)
        {
            return Misc.IsBitSet(GetExtendedListViewStyle(hwnd), NativeMethods.LVS_EX_ONECLICKACTIVATE);
        }

        // detect if listview supports multiple selection
        static internal bool MultiSelected (IntPtr hwnd)
        {
            return !Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.LVS_SINGLESEL);
        }

        // detect if listview contains or potential may contain scrollbar
        static internal bool Scrollable (IntPtr hwnd)
        {
            return !Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.LVS_NOSCROLL);
        }

        // ensure listview item visibility
        static internal bool EnsureVisible (IntPtr hwnd, int item, bool partialOK)
        {
            IntPtr partialVisible = (partialOK) ? IntPtr.Zero : new IntPtr (1);

            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_ENSUREVISIBLE, new IntPtr(item), partialVisible) != 0;
        }

        // return listview header
        static internal IntPtr ListViewGetHeader (IntPtr hwnd)
        {
            return Misc.ProxySendMessage(hwnd, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
        }

        // retrieve listview item text
        static internal string GetItemText (IntPtr hwnd, NativeMethods.LVITEM item)
        {
            item.cchTextMax = Misc.MaxLengthNameProperty;

            return XSendMessage.GetItemText(hwnd, item);
        }

        // perform a hit test on the specific point
        // POINT is in screen coordinates
        static internal NativeMethods.LVHITTESTINFO_INTERNAL SubitemHitTest (IntPtr hwnd, NativeMethods.Win32Point pt)
        {
            return SubitemHitTest (hwnd, 0, pt);
        }

        // perform a hit test on the specific point
        // POINT is in screen coordinates
        static internal NativeMethods.LVHITTESTINFO_INTERNAL SubitemHitTest (IntPtr hwnd, int item, NativeMethods.Win32Point pt)
        {
            // Allocate a local LVHITTESTINFO struct.
            NativeMethods.LVHITTESTINFO_INTERNAL hitTest = new NativeMethods.LVHITTESTINFO_INTERNAL ();

            // Set the point of interest.
            hitTest.pt = pt;
            hitTest.iItem = item;

            int result = -1;

            // convert to client
            if (Misc.MapWindowPoints(IntPtr.Zero, hwnd, ref hitTest.pt, 1))
            {
                unsafe
                {
                    // Send the LVM_SUBITEMHITTEST message to the list view owner process.
                    // This is ok to do even for non LVS_REPORT listview, since in that case this
                    // message will behaive like LVM_HITTEST
                    if (Misc.IsComctrlV6OnOsVerV6orHigher(hwnd))
                    {
                        NativeMethods.LVHITTESTINFO_V6 hitTestNative = new NativeMethods.LVHITTESTINFO_V6(hitTest);
                        result = XSendMessage.XSendGetIndex(hwnd, NativeMethods.LVM_SUBITEMHITTEST, IntPtr.Zero, new IntPtr(&hitTestNative), Marshal.SizeOf(hitTestNative.GetType()));
                        hitTest.flags = hitTestNative.flags;
                        hitTest.iItem = hitTestNative.iItem;
                        hitTest.iGroup = hitTestNative.iGroup;
                    }
                    else
                    {
                        NativeMethods.LVHITTESTINFO hitTestNative = new NativeMethods.LVHITTESTINFO(hitTest);
                        result = XSendMessage.XSendGetIndex(hwnd, NativeMethods.LVM_SUBITEMHITTEST, IntPtr.Zero, new IntPtr(&hitTestNative), Marshal.SizeOf(hitTestNative.GetType()));
                        hitTest.flags = hitTestNative.flags;
                        hitTest.iItem = hitTestNative.iItem;
                    }
                }
            }

            if (result == -1)
            {
                hitTest.iSubItem = hitTest.iItem = -1;
            }

            return hitTest;
        }

        // retrieve count of header items
        static internal int HeaderItemCount (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.HDM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        // detect if the listview support checkboxes
        static internal bool CheckBoxes (IntPtr hwnd)
        {
            return Misc.IsBitSet(GetExtendedListViewStyle(hwnd), NativeMethods.LVS_EX_CHECKBOXES);
        }

        // get listview item check state
        static internal int GetCheckedState (IntPtr hwnd, int item)
        {
            int state = GetItemState(hwnd, item, NativeMethods.LVIS_STATEIMAGEMASK);

            return ((state >> 12) - 1);
        }

        // detect if listview is auto-arranged
        static internal bool ListViewAutoArrange (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.LVS_AUTOARRANGE);
        }

        // detect if listview supports full row selection
        static public bool FullRowSelect (IntPtr hwnd)
        {
            return Misc.IsBitSet(GetExtendedListViewStyle(hwnd), NativeMethods.LVS_EX_FULLROWSELECT);
        }

        // detects if icons are lined up in columns that use up the whole view area
        static public bool HasJustifyColumnsExStyle(IntPtr hwnd)
        {
            return Misc.IsBitSet(GetExtendedListViewStyle(hwnd), NativeMethods.LVS_EX_JUSTIFYCOLUMNS);
        }

        // gets rectangle of the subitem.
        // This method is inteded to be used with the LVS_REPORT lv
        static public unsafe bool GetSubItemRect (IntPtr hwnd, int item, int subItem, int lvir, out NativeMethods.Win32Rect itemRectangle)
        {
            itemRectangle = NativeMethods.Win32Rect.Empty;
            itemRectangle.left = lvir;
            itemRectangle.top = subItem;

            fixed (int * location = &(itemRectangle.left))
            {
                if (XSendMessage.XSend(hwnd, NativeMethods.LVM_GETSUBITEMRECT, new IntPtr(item), new IntPtr(location), Marshal.SizeOf(itemRectangle.GetType())))
                {
                    return Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref itemRectangle, 2);
                }

                return false;
            }
        }

        static internal string GetItemToolTipText(IntPtr hwnd)
        {
            IntPtr hwndToolTip = Misc.ProxySendMessage(hwnd, NativeMethods.LVM_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);

            return Misc.GetItemToolTipText(hwnd, hwndToolTip, 0);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal readonly static GroupManagerCollection _groupsCollection = new GroupManagerCollection();
        // Microsoft Used for MultipleView Pattern, until official table
        // will not be finalyzed
        // May need to be removed after official table is ready
        // DO NOT RE-ORDER!!!!! Order equal to the LV_VIEW_XXX defines
        internal static string [] ListViewViews = new string [] {
                "Icons", "Details", "Smallicon", "List", "Tiles"
            };

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // Picks a WinEvent to track for a UIA property
        protected override int [] PropertyToWinEvent (AutomationProperty idProp)
        {
            if (idProp == ValuePattern.ValueProperty)
            {
                return CheckBoxes (_hwnd) ? new int [] { NativeMethods.EventObjectNameChange, NativeMethods.EventObjectStateChange } : new int [] { NativeMethods.EventObjectNameChange };
            }
            else if (idProp == GridPattern.ColumnCountProperty || idProp == GridPattern.RowCountProperty || idProp == GridItemPattern.ColumnProperty || idProp == GridItemPattern.RowProperty)
            {
                return new int [] { NativeMethods.EventObjectReorder };
            }
            return base.PropertyToWinEvent (idProp);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Create a Listview item proxy
        private ProxyFragment CreateListViewItem(int index)
        {
            return CreateListViewItemOrStartMenuItem(this, index);
        }

        // Create a group for the listview
        private ProxyFragment CreateListViewGroup (int groupID)
        {
            return new WindowsListViewGroup (_hwnd, this, groupID);
        }

        // Create a Listview item proxy, check if the item is enclosed in a group. 
        // If it is the case, then create also the ListViewGroup and do the parenting
        // properly.
        private ProxySimple CreateListViewItemCheckIfInGroup (int item)
        {
            // if LV is in Group mode, lvitems will live under the corresponding group
            bool hasGroup = IsGroupViewEnabled (_hwnd);

            if ((!hasGroup) && GetItemCount(_hwnd) > 0)
            {
                return CreateListViewItemOrStartMenuItem(this, item);
            }

            // Navigate to the first group
            if (hasGroup)
            {
                _groupsCollection.EnsureCreation(_hwnd);

                GroupManager manager = _groupsCollection[_hwnd];
                int[] groupIds = manager.GetGroupIds();

                // if there are no groups this is an empty list
                if (groupIds.Length == 0)
                {
                    return null;
                }

                // Loop through all the groups to figure out in which group this item belongs
                foreach (int groupId in groupIds)
                {
                    GroupManager.GroupInfo gi = manager.GetGroupInfo (groupId);

                    if (gi.IndexOf (item) != -1)
                    {
                        return CreateListViewItemOrStartMenuItem(new WindowsListViewGroup(_hwnd, this, groupId), item);
                    }
                }

                // Could not find an item, must be a groupId.
                return new WindowsListViewGroup (_hwnd, this, item);
            }

            // no content go for the scrollbars
            return null;
        }

        private ProxyFragment CreateListViewItemOrStartMenuItem(ProxyFragment parent, int item)
        {
            // ListView items on the Start Menu are special.  The IAccessible is needed to get
            // information from these special ListView items.  If a valid IAccessible can not be
            // obtained default to the normal ListView item.
            if (InStartMenu() && AccessibleObject != null)
            {
                ProxyFragment proxyFragment = new ListViewItemStartMenu(_hwnd, parent, item, AccessibleObject);
                if (proxyFragment != null)
                {
                    proxyFragment.AccessibleObject = AccessibleObject;
                }
                return proxyFragment;
            }
            else
            {
                return new ListViewItem(_hwnd, parent, item);
            }
        }

        // This method maybe OS depended.  (Most likely will change in Longhorn.  In Longhorn we may not need this method.)
        private bool InStartMenu()
        {
            string className = Misc.GetClassName(Misc.GetParent(_hwnd));
            return string.Compare(className, "DesktopSFTBarHost", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private bool SetScrollPercent(double fScrollPos, int sbFlag, int cPelsAll, out int delta)
        {
            // in case of early exit no move
            delta = 0;

            // Check param
            if ((int)fScrollPos == (int)ScrollPattern.NoScroll)
            {
                return true;
            }

            if (fScrollPos < 0 || fScrollPos > 100)
            {
                throw new ArgumentOutOfRangeException(sbFlag == NativeMethods.SB_HORZ ? "horizontalPercent" : "verticalPercent", SR.Get(SRID.ScrollBarOutOfRange));
            }

            int scrollBar = sbFlag == NativeMethods.SB_HORZ ? NativeMethods.OBJID_HSCROLL : NativeMethods.OBJID_VSCROLL;

            NativeMethods.ScrollBarInfo scrollBarInfo = new NativeMethods.ScrollBarInfo();
            scrollBarInfo.cbSize = Marshal.SizeOf(scrollBarInfo.GetType());
            if (!Misc.GetScrollBarInfo(_hwnd, scrollBar, ref scrollBarInfo) ||
                (scrollBarInfo.scrollBarInfo & NativeMethods.STATE_SYSTEM_INVISIBLE) != 0 || 
                (scrollBarInfo.scrollBarInfo & NativeMethods.STATE_SYSTEM_UNAVAILABLE) != 0)
            {
                return false;
            }

            // Get scroll range
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo (); // this is used all over

            si.cbSize = Marshal.SizeOf (si.GetType ());
            si.fMask = NativeMethods.SIF_ALL;

            // if no scroll bar return false
            // on Win 6.0 success is false
            // on other system check through the scroll info is a scroll bar is there
            if (!Misc.GetScrollInfo(_hwnd, sbFlag, ref si) ||
                !((si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1)))
            {
                return false;
            }

            // calculate user-requested thumb position
            int deltaPage = (si.nPage > 0) ? si.nPage - 1 : 0;
            int future = (int) Math.Round (((si.nMax - deltaPage) - si.nMin) * fScrollPos / 100.0 + si.nMin);

            // delta between current and user-requested position in pixels
            // since the cPelsAll contains the dimension in pels for all items + the 2 pels of the border
            // the operation below does a trunc on purpose
            delta = (future - si.nPos) * (cPelsAll / (si.nMax + 1 - si.nMin));

            return true;
        }

        // Grid.GetCell implementation for detail mode
        private IRawElementProviderSimple GetCellInDetailMode (int row, int column)
        {
            // NOTE: In Detail mode the is no empty cells
            ProxyFragment lvItem = CreateListViewItem (row);

            return new ListViewSubItem (_hwnd, lvItem, column, row);
        }
        // Grid.GetCell implementation for lv that is not in detail mode
        private IRawElementProviderSimple GetCellInOtherModes (int row, int column, int maxColumn, int maxRow)
        {
            // Assumption: passed in arguments were already verified
            // NOTE: cells at the end might be empty
            int itemCount = GetItemCount (_hwnd);
            int itemIndex = 0;

            if (IsListMode (_hwnd))
            {
                // calculate item's index
                itemIndex = column * maxRow + row;
            }
            else
            {
                // SIcon, LIcon, Title, Thumbnails
                itemIndex = row * maxColumn + column;
            }

            // verify the cell exists
            if (itemIndex >= itemCount)
            {
                // Return an empty cell
                return new EmptyGridItem (row, column, this);
            }

            // return cell
            return CreateListViewItem (itemIndex);
        }
        
        // get count of rows in the non-detail lv
        static private int GetRowCountOtherModes (IntPtr hwnd)
        {
            // Assumption: items are autoarranged
            int count = GetItemCount(hwnd);

            // Check for empty list
            if (count <= 0)
            {
                return 0;
            }

            if (IsListMode(hwnd))
            {
                return GetRowCountListMode(hwnd, count);
            }

            // Algorithm for non-list mode of ListView with at least one item:
            // Starting from the first item, count the items below it.
            int rowCount = 0;
            int curItem = 0;
            while (true)
            {
                rowCount++;
                int nextItem = GetItemNext(hwnd, curItem, NativeMethods.LVNI_BELOW);
                // Expect -1 when no more items below current item
                if (nextItem < 0)
                    break;
                // Guard against infinite loop (getting back the same item LH BUG in SysListView32)
                if (nextItem == curItem)
                    break;

                // Assumption: As long as nextItem is changing everything is OK
                // Note: Docs imply it may be possible for nextItem < curItem at this
                // point so don't assume nextItem is always increasing.
                curItem = nextItem;
            }

            return rowCount;
        }
        
        // detect if the listview has a list style
        static private bool ListViewList (IntPtr hwnd)
        {
            return ((Misc.GetWindowStyle(hwnd) & NativeMethods.LVS_TYPEMASK) == NativeMethods.LVS_LIST);
        }

        // get top-left point of the listview item
        static private unsafe bool GetItemPosition (IntPtr hwnd, int item, out NativeMethods.Win32Point pt)
        {
            pt.x = 0;
            pt.y = 0;

            fixed (int * location = &(pt.x))
            {
                if (XSendMessage.XSend(hwnd, NativeMethods.LVM_GETITEMPOSITION, new IntPtr(item), new IntPtr(location), Marshal.SizeOf(pt.GetType())))
                {
                    return Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref pt, 1);
                }

                return false;
            }
        }

        // Get listview extended styles
        static private int GetExtendedListViewStyle (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero);
        }

        // retrieve specific "state" of the listview item
        private static int GetItemState (IntPtr hwnd, int item, int stateMask)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.LVM_GETITEMSTATE, new IntPtr(item), new IntPtr(stateMask));
        }
        // set listview item state
        private static bool SetItemState (IntPtr hwnd, int item, int stateMask, int state)
        {
            NativeMethods.LVITEM lvitem = new NativeMethods.LVITEM ();

            lvitem.mask = NativeMethods.LVIF_STATE;
            lvitem.state = state;
            lvitem.stateMask = stateMask;

            return XSendMessage.SetItem(hwnd, item, lvitem);
        }

        // Check if the point on the screen is part of the header
        private bool PtInListViewHeader (int x, int y)
        {
            // See if header exist
            IntPtr hwndHeader = ListViewGetHeader (_hwnd);

            if (hwndHeader != IntPtr.Zero && SafeNativeMethods.IsWindowVisible (hwndHeader))
            {
                if (Misc.PtInWindowRect(hwndHeader, x, y))
                {
                    return true;
                }
            }

            return false;
        }


        private static void RaiseEventsOnClient(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxySimple el = null;

            WindowsListView wlv = new WindowsListView (hwnd, null, -1);
            AutomationProperty automationProperty = idProp as AutomationProperty;
            AutomationEvent automationEvent = idProp as AutomationEvent;

            if (eventId == NativeMethods.EventObjectSelectionRemove && automationProperty == SelectionItemPattern.IsSelectedProperty)
            {
                el = wlv.CreateListViewItemCheckIfInGroup(idChild - 1);
                if (el != null)
                {
                    el.DispatchEvents(eventId, idProp, idObject, idChild);
                    return;
                }
            }
            else if (eventId == NativeMethods.EventObjectSelection
            || eventId == NativeMethods.EventObjectSelectionRemove
            || eventId == NativeMethods.EventObjectSelectionAdd)
            {
                // This is the speced behavior for events and selection 
                //
                // The following rule should be used to decide when to fire Selected vs Add/Remove events:
                // If the result of a SelectElement or an AddElementToSelection is that a single item is selected, 
                // then send a Select for that element; otherwise send Add/Removes as appropriate.
                // Note that this rule does not depend on whether the selection container is single- or multi- select, 
                // or on what method was used to change the selection. Only the result matters.
                // Overall message to clients (test automation and assistive technologies)
                //
                // 1) If you receive ElementSelectedEvent this guarantees that the element that raised the 
                //    event is the only selected element in that container.
                // 2) If you receive ElementAddedToSelectionEvent this guarantees that those items are added 
                //    to selection and the end result of the selection is more than one item.  
                // 3) If you receive ElementRemovedFromSelectionEvent this guarantees that those items are 
                //    deselected and the end result is NOT one item selected.  
                //
                // For the listview adhering to the spec is not possible because of an ambiguity with the winevents that 
                // are fired.  This code is trying to map the winevents received to the UIAutomaiton events that are expected.
                // These are the two cases that are ambiguous:
                //
                // Case 1:
                // The user clicks two different items in succession.
                // We get an EventObjectSelectionRemove WinEvent for each item that loses 
                // selection and in addition an EventObjectSelection for the new item that got selection.  
                // In this case we want to disregard the EventObjectSelectionRemove (not raise UIA 
                // ElementRemovedFromSelectionEvent) because the end result of this scenario is that only one item is selected.
                // 
                // Case 2:
                // If the ListView is multi-select and there are two items selected and the user cntl clicks on one (unselects it).
                // The listview fires only one WinEvent (an EventObjectSelectionRemove) for the 
                // item that was removed.   In this case since there is only one 
                // item left selected UIA should raise ElementSelectedEvent for the remaining item.
                // 
                // If we turn the EventObjectSelectionRemove WinEvent in case 2 into a ElementSelected 
                // event for the remaining element we would end up firing two events for each click in a multi 
                // select list.  This is because the EventObjectSelectionRemove in case 1 is just like 
                // the one case 2.  Except that in case 1 we also get a EventObjectSelection separately so we
                // would end up firing another selected event.
                //
                // It has been decided that it is preferred to receive extra EventObjectSelection events 
                // then receiving EventObjectSelectionRemove events at the wrong time. If two items are selected
                // in a listview and the user clicks on one of them the only event winevent we get is a remove so
                // we have to convert removed winevent to selected event or we would miss an event.  So it better
                // to have multiple events in some case than none when there should be (Microsoft 9/8/2004).
                //
                if (eventId == NativeMethods.EventObjectSelectionRemove && GetSelectedItemCount(hwnd) == 1)
                {
                    if (MultiSelected(hwnd))
                    {
                        // Change the EventObjectSelectionRemove to an EventObjectSelection.
                        eventId = NativeMethods.EventObjectSelection;
                        idProp = SelectionItemPattern.ElementSelectedEvent;

                        // Change the child id to the selected child.
                        int item = GetStartOfSelectedItems(hwnd);
                        if (item > -1)
                        {
                            idChild = item + 1;
                        }
                    }
                    else
                    {
                        // Since case 2 does not apply to single selection listviews, suppress the
                        // EventObjectSelectionRemove.
                        return;
                    }
                } 
                
                el = wlv.CreateListViewItemCheckIfInGroup(idChild - 1);
            }
            // GridItem case
            else if (eventId == NativeMethods.EventObjectReorder && (automationProperty == GridItemPattern.ColumnProperty || automationProperty == GridItemPattern.RowProperty))
            {
                // GridItem case. We need to recursively call all of the list items
                for (el = wlv.GetFirstChild(); el != null; el = wlv.GetNextSibling(el))
                {
                    el.DispatchEvents(eventId, idProp, idObject, idChild);
                }
                return;
            }
            // Map the WinEvent NameChange to ValueChange to go through the dispatch
            else if (eventId == NativeMethods.EventObjectNameChange)
            {
                el = wlv.CreateListViewItemCheckIfInGroup(idChild - 1);
                eventId = NativeMethods.EventObjectValueChange;
            }
            // Change of state for the check box must generates a StateChange Win Events.
            // Map it to of ObjectChange for the checkbox
            else if (eventId == NativeMethods.EventObjectStateChange && CheckBoxes(hwnd))
            {
                el = wlv.CreateListViewItemCheckIfInGroup(idChild - 1);

                el = ((ProxyFragment)el).GetFirstChild();
                eventId = NativeMethods.EventObjectValueChange;

                // Assert if the assumption that the first child is a check box is false
                System.Diagnostics.Debug.Assert(el is ListViewItemCheckbox);
            }
            // Special case for logical element change for a list view item
            else if ((eventId == NativeMethods.EventObjectDestroy || eventId == NativeMethods.EventObjectCreate) && automationEvent == AutomationElement.StructureChangedEvent)
            {
                ProxySimple parent = wlv;
                bool fGroupView = IsGroupViewEnabled(hwnd);

                // Allways disable the groups as one may have been created or
                // destroyed
                if (fGroupView)
                {
                    // remove groupmanager from collection
                    _groupsCollection.Remove(hwnd);

                    // If it is an object creation, create the element and picks
                    // its parent to invalidate. The parent can be either a group
                    // or  the listview itself.
                    if (eventId == NativeMethods.EventObjectCreate && fGroupView)
                    {
                        // Get the item with the resetted collection of groups (may be null if the group is empty)
                        ProxySimple lvi = wlv.CreateListViewItemCheckIfInGroup(idChild - 1);
                        if (lvi != null)
                            parent = lvi.GetParent();
                    }
                }

                // If the element destroyed is in a group invalidate the whole listview as we have no
                // idea the element was part of before
                // Since children are referenced by position in the tree, addition and removal
                // of items leads to different results when asking properties for the same element
                // On removal, item + 1 is now item!
                // Use Children Invalidated to let the client knows that all the cached children are invalid
                AutomationInteropProvider.RaiseStructureChangedEvent( parent, new StructureChangedEventArgs( StructureChangeType.ChildrenInvalidated, parent.MakeRuntimeId() ) );
                return;
            }
            else
            {
                el = wlv;
            }

            if (el != null)
            {
                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }

            return;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // group specific events. Used for internal tracking
        private readonly static WinEventTracker.EvtIdProperty [] _groupEvents;

        #endregion Private Fields
    }

}

