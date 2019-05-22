// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 TreeView proxy

using System;
using System.Text;
using System.Collections;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsTreeView : ProxyHwnd, ISelectionProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsTreeView (IntPtr hwnd, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Tree;

            // Can be focused
            _fIsKeyboardFocusable = true;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            WindowsTreeView wtv = new WindowsTreeView(hwnd, null, 0);
            return idChild == 0 ? wtv : wtv.CreateParents(hwnd, TreeItemFromChildID(hwnd, idChild));
        }

        // Static Create method called by the event tracker system
        // WinEvents are one throwns because items exist. so it makes sense to create the item and
        // check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxySimple el = null;

            switch (idObject)
            {
                case NativeMethods.OBJID_CLIENT :
                    {
                        WindowsTreeView wtv = new WindowsTreeView (hwnd, null, -1);

                        // Selection or Expand/Collapse
                        if (idChild != 0 && (eventId == NativeMethods.EventObjectSelection ||
                                             eventId == NativeMethods.EventObjectSelectionRemove ||
                                             eventId == NativeMethods.EventObjectSelectionAdd ||
                                             eventId == NativeMethods.EventObjectStateChange ||
                                             eventId == NativeMethods.EventObjectDestroy ||
                                             eventId == NativeMethods.EventObjectCreate ||
                                             eventId == NativeMethods.EventObjectNameChange))
                        {
                            el = wtv.CreateParents(hwnd, TreeItemFromChildID(hwnd, idChild));
                        }
                        else
                        {
                            el = wtv;
                        }

                        break;
                    }

                case NativeMethods.OBJID_VSCROLL :
                case NativeMethods.OBJID_HSCROLL :
                    break;

                default :
                    el = new WindowsTreeView (hwnd, null, -1);
                    break;
            }

            // Expand/Collapse is too peculiar per control to be processed in the dispatch code
            if (idProp == ExpandCollapsePattern.ExpandCollapseStateProperty && el is TreeViewItem && eventId == NativeMethods.EventObjectStateChange)
            {
                ((TreeViewItem) el).RaiseExpandCollapsedStateChangedEvent ();
                return;
            }

            // Special case for logical element change for a tree view item on Expand/Collapse
            if (((idProp as AutomationEvent) == AutomationElement.StructureChangedEvent && el is TreeViewItem) && !(eventId == NativeMethods.EventObjectDestroy || eventId == NativeMethods.EventObjectCreate))
            {
                ((TreeViewItem) el).RaiseStructureChangedEvent ();
                return;
            }

            if (el != null)
            {
                el.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion Proxy Create

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // ------------------------------------------------------
        //
        // RawElementProvider interface implementation
        //
        // ------------------------------------------------------

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            // This is the treeview container
            if (iid == SelectionPattern.Pattern)
            {
                return this;
            }

            return null;
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            TVItem item = (TVItem)child._item;

            // root child
            if (item == TVItem.TopLevel)
            {
                IntPtr hNext = GetNextItem (_hwnd, ((TreeViewItem) child)._hItem);

                if (hNext != IntPtr.Zero)
                    return new TreeViewItem (_hwnd, this, hNext, (int) TVItem.TopLevel);
            }

            return base.GetNextSibling (child);
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            // start with the scrollbars
            ProxySimple ret = base.GetPreviousSibling (child);

            if (ret != null)
            {
                return ret;
            }

            // top level Treeview return the prev
            TVItem item = (TVItem)child._item;

            if (item == TVItem.TopLevel)
            {
                IntPtr hPrev = GetPreviousItem (_hwnd, ((TreeViewItem) child)._hItem);

                return hPrev != IntPtr.Zero ? new TreeViewItem (_hwnd, this, hPrev, (int) TVItem.TopLevel) : null;
            }

            // either scroll bar or nothing as prev
            IntPtr hChild = GetRoot (_hwnd);

            if (hChild != IntPtr.Zero)
            {
                // First Child found, now retrieve the last one (no specific msg, need to walk thru all of them)
                IntPtr temp;

                for (temp = GetNextItem (_hwnd, hChild); temp != IntPtr.Zero; temp = GetNextItem (_hwnd, hChild))
                {
                    hChild = temp;
                }

                return new TreeViewItem (_hwnd, this, hChild, (int) TVItem.TopLevel);
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            IntPtr hChild = IntPtr.Zero;

            hChild = GetRoot (_hwnd);
            if (hChild != IntPtr.Zero)
            {
                return CreateTreeViewItem (hChild, (int) TVItem.TopLevel);
            }

            return base.GetFirstChild ();
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            // start with the scrollbars
            ProxySimple ret = base.GetFirstChild ();

            if (ret != null)
            {
                return ret;
            }

            // get the root (or the very first item in the tree)
            IntPtr hChild = GetRoot (_hwnd);

            if (hChild != IntPtr.Zero)
            {
                // First Child found, now retrieve the last one (no specific msg, need to walk thru all of them)
                for (IntPtr temp = GetNextItem (_hwnd, hChild); temp != IntPtr.Zero; temp = GetNextItem (_hwnd, hChild))
                {
                    hChild = temp;
                }

                return CreateTreeViewItem (hChild, (int) TVItem.TopLevel);
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            IntPtr hItem = XSendMessage.HitTestTreeView(_hwnd, x, y);
            if (hItem != IntPtr.Zero)
            {
                return CreateTreeViewItemAndParents(hItem);
            }
            return base.ElementProviderFromPoint (x, y);
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            IntPtr treeItem = GetSelection (_hwnd);

            if (treeItem != IntPtr.Zero)
            {
                return CreateTreeViewItemAndParents (treeItem);
            }

            return this;
        }

        #endregion Interface ContextProvider

        #region Selection Pattern

        // Returns an enumerator over the current selection.
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            IntPtr treeItem = GetSelection(_hwnd);

            if (treeItem == IntPtr.Zero)
            {
                // framework will handle this one correctly
                return null;
            }

            // no native support for multi-selection
            IRawElementProviderSimple[] selection = new IRawElementProviderSimple[1];
            selection [0] = CreateTreeViewItemAndParents(treeItem);

            return selection;
        }

        // Returns whether the control requires a minimum of one selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            // NOTE: this property is dynamic
            // In the case when TV does not have a selected tvitem we will return false
            // if there is a tvitem with the selection we will return true
            get
            {
                return (IntPtr.Zero != GetSelection (_hwnd));
            }
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                // Windows tree view does not provide native support
                // for multiple selection
                return false;
            }
        }

        #endregion Selection Pattern

        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods

        // Picks a WinEvent to track for a UIA property
        // Returns the WinEvent ID or 0 if no WinEvents matches a the UIA property
        protected override int [] PropertyToWinEvent (AutomationProperty idProp)
        {
            if (idProp == ValuePattern.ValueProperty)
            {
                return new int[] { NativeMethods.EventObjectNameChange,
                                   NativeMethods.EventObjectStateChange };
            }
            else if (idProp == ExpandCollapsePattern.ExpandCollapseStateProperty)
            {
                return new int[] { NativeMethods.EventObjectStateChange };
            }
            return base.PropertyToWinEvent (idProp);
        }

        // Builds a list of Win32 WinEvents to process a UIAutomation Event.
        // Returns an array of Events to Set. The number of valid entries in this array pass back in cEvent.
        protected override WinEventTracker.EvtIdProperty [] EventToWinEvent (AutomationEvent idEvent, out int cEvent)
        {
            if (idEvent == AutomationElement.StructureChangedEvent)
            {
                cEvent = 3;
                return new WinEventTracker.EvtIdProperty [3] {
                    new WinEventTracker.EvtIdProperty (NativeMethods.EventObjectStateChange, idEvent),
                    new WinEventTracker.EvtIdProperty (NativeMethods.EventObjectCreate, idEvent),
                    new WinEventTracker.EvtIdProperty (NativeMethods.EventObjectDestroy, idEvent)
                };
            }

            return base.EventToWinEvent (idEvent, out cEvent);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        #region SubItem Creation Helper

        // private Create called by this proxy to generate Sibling, child, parent, ...
        private ProxyFragment CreateTreeViewItem (IntPtr hItem, int depth)
        {
            return new TreeViewItem (_hwnd, this, hItem, depth);
        }

        private ProxyFragment CreateTreeViewItemAndParents (IntPtr hItem)
        {
            return CreateParents (_hwnd, hItem);
        }

        private ProxyFragment CreateParents (IntPtr hwnd, IntPtr hItem)
        {
            IntPtr hItemParent = Parent (hwnd, hItem);

            if (hItemParent == IntPtr.Zero)
            {
                return new TreeViewItem (hwnd, this, hItem, 0);
            }
            else
            {
                ProxyFragment elParent = CreateParents (hwnd, hItemParent);

                return new TreeViewItem(hwnd, elParent, hItem, elParent._item + 1);
            }
        }

        #endregion

        #region Expand/Collapse Helpers

        // expand tree view item
        private static bool Expand (IntPtr hwnd, IntPtr treeItem)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.TVM_EXPAND, new IntPtr(NativeMethods.TVE_EXPAND), treeItem) != 0;
        }

        // collapse tree view item
        private static bool Collapse (IntPtr hwnd, IntPtr treeItem)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.TVM_EXPAND, new IntPtr(NativeMethods.TVE_COLLAPSE), treeItem) != 0;
        }

        // detect if tree view item is expanded.
        private static bool IsItemExpanded (IntPtr hwnd, IntPtr treeItem)
        {
            int expanded = GetItemState(hwnd, treeItem, NativeMethods.TVIS_EXPANDED);

            return (Misc.IsBitSet(expanded, NativeMethods.TVIS_EXPANDED));
        }

        #endregion

        #region Selection Helpers

        // select tree view item
        private static bool SelectItem (IntPtr hwnd, IntPtr treeItem)
        {
            bool fRet;
            if (Misc.ProxySendMessageInt(hwnd, NativeMethods.TVM_SELECTITEM, new IntPtr(NativeMethods.TVGN_CARET | NativeMethods.TVSI_NOSINGLEEXPAND), treeItem) != 0)
            {
                fRet = true;
            }
            else
            {
                fRet = Misc.ProxySendMessageInt(hwnd, NativeMethods.TVM_SELECTITEM, new IntPtr(NativeMethods.TVGN_CARET), treeItem) != 0;
            }

            return fRet;
        }

        // retrieve currently selected item
        private static IntPtr GetSelection (IntPtr hwnd)
        {
            return GetNext(hwnd, IntPtr.Zero, NativeMethods.TVGN_CARET);
        }

        #endregion

        #region Navigation Helper

        // retrieve the parent of the current item
        private static IntPtr Parent (IntPtr hwnd, IntPtr treeItem)
        {
            return GetNext(hwnd, treeItem, NativeMethods.TVGN_PARENT);
        }

        // retrieve the next item
        private static IntPtr GetNextItem (IntPtr hwnd, IntPtr treeItem)
        {
            return GetNext(hwnd, treeItem, NativeMethods.TVGN_NEXT);
        }

        // retrieve the previous item
        private static IntPtr GetPreviousItem (IntPtr hwnd, IntPtr treeItem)
        {
            return GetNext(hwnd, treeItem, NativeMethods.TVGN_PREVIOUS);
        }

        // retrieve root of the tree view
        private static IntPtr GetRoot (IntPtr hwnd)
        {
            return GetNext(hwnd, IntPtr.Zero, NativeMethods.TVGN_ROOT);
        }

        // retrieve the first child of the current tree view item
        private static IntPtr GetFirstChild (IntPtr hwnd, IntPtr treeItem)
        {
            return GetNext(hwnd, treeItem, NativeMethods.TVGN_CHILD);
        }

        #endregion

        #region Value Helpers

        // retrieve the checked state for the specified item
        private static int GetCheckState (IntPtr hwnd, IntPtr treeItem)
        {
            int state = GetItemState(hwnd, treeItem, NativeMethods.TVIS_STATEIMAGEMASK);

            return ((state >> 12) - 1);
        }

        // set the check state for the specified item
        private unsafe static bool SetCheckState (IntPtr hwnd, IntPtr item, bool check)
        {
            uint val = (check) ? 2U : 1U;

            val <<= 12;

            NativeMethods.TVITEM treeItem = new NativeMethods.TVITEM ();
            treeItem.Init (item);
            treeItem.mask = NativeMethods.TVIF_STATE;
            treeItem.state = val;
            treeItem.stateMask = NativeMethods.TVIS_STATEIMAGEMASK;

            return XSendMessage.SetItem(hwnd, treeItem);
        }

        #endregion

        #region Common Helpers

        // generic method for TVM_GETNEXTITEM message
        private static IntPtr GetNext (IntPtr hwnd, IntPtr treeItem, int flag)
        {
            return Misc.ProxySendMessage(hwnd, NativeMethods.TVM_GETNEXTITEM, new IntPtr(flag), treeItem);
        }

        // generic way to retrieve item's state
        private static int GetItemState (IntPtr hwnd, IntPtr treeItem, int stateMask)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.TVM_GETITEMSTATE, treeItem, new IntPtr(stateMask));
        }

        // detect if tree view item has children
        private static bool TreeViewItem_HasChildren (IntPtr hwnd, IntPtr item)
        {
            NativeMethods.TVITEM treeItem;

            if (!GetItem(hwnd, item, NativeMethods.TVIF_CHILDREN, out treeItem))
            {
                // @review: should we throw here
                return false;
            }

            return (treeItem.cChildren > 0);
        }

        // retrieve rectangle for the treeview
        // set labelOnly to true if you only care about label rectangle
        private static unsafe NativeMethods.Win32Rect GetItemRect (IntPtr hwnd, IntPtr treeItem, bool labelOnly)
        {
            NativeMethods.Win32Rect rc = NativeMethods.Win32Rect.Empty;

            // This strange line of code is here to make the TVM_GETITEMRECT work on 64 bit platform
            // This message expcects an IntPtr on input and a Rect for output.  On a 64 bit platform we
            // will just overwrite the first 2 members of the rect structure with the IntPtr.
            *((IntPtr *)&(rc.left)) = treeItem;

            IntPtr rectangle = new IntPtr (&(rc.left));
            IntPtr partialDisplay = (labelOnly) ? new IntPtr (1) : IntPtr.Zero;

            if (!XSendMessage.XSend(hwnd, NativeMethods.TVM_GETITEMRECT, partialDisplay, rectangle, Marshal.SizeOf(rc.GetType())))
            {
                return NativeMethods.Win32Rect.Empty;
            }

            // Temporarily allow the possibility of returning a bounding rect for scrolled off items.
            // Will need to revisit this when there is a method that can scroll items into view.
            //if (Misc.IsItemVisible(hwnd, ref rc))

            return Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rc, 2) ? rc : NativeMethods.Win32Rect.Empty;
        }

        // generic method to retrieve info about tree view item
        // NOTE: this method should not be used to retrieve a text
        // instead use GetItemText
        private static bool GetItem (IntPtr hwnd, IntPtr item, int mask, out NativeMethods.TVITEM treeItem)
        {
            treeItem = new NativeMethods.TVITEM ();
            treeItem.Init (item);
            treeItem.mask = (uint) mask;

            return XSendMessage.GetItem(hwnd, ref treeItem);
        }

        private static string GetItemText(IntPtr hwnd, IntPtr item)
        {
            NativeMethods.TVITEM treeItem = new NativeMethods.TVITEM();
            treeItem.Init(item);
            treeItem.mask = NativeMethods.TVIF_TEXT;
            treeItem.cchTextMax = Misc.MaxLengthNameProperty;

            return XSendMessage.GetItemText(hwnd, treeItem);
        }

        private static bool SetItemText(IntPtr hwnd, IntPtr item, string text)
        {
            // TVM_SETITEMW with TVIF_TEXT will not work here.  It does not notify parent of the change.

            // Begins in-place editing of the specified item's text, replacing the text of the item with a single-line
            // edit control containing the text. This message implicitly selects and focuses the specified item.
            IntPtr hwndEdit = Misc.ProxySendMessage(hwnd, NativeMethods.TVM_EDITLABELW, IntPtr.Zero, item);

            if (hwndEdit == IntPtr.Zero)
            {
                // assume that the hwnd was bad
                throw new ElementNotAvailableException();
            }

            // Now set the text to the edit control
            // Note: The lParam of the WM_SETTEXT is NOT a receive parameter. Just used this overloaded version
            // of ProxySendMessage() for convinces.
            if (Misc.ProxySendMessageInt(hwndEdit, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(text)) != 1)
            {
                // Cancel the edit.
                Misc.ProxySendMessage(hwnd, NativeMethods.TVM_ENDEDITLABELNOW, (IntPtr)1, IntPtr.Zero);
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // TVM_ENDEDITLABELNOW ends the editing of a tree-view item's label.
            // The wParam indicates whether the editing is canceled without being saved to the label.
            // If this parameter is TRUE, the system cancels editing without saving the changes.
            // Otherwise, the system saves the changes to the label.
            Misc.ProxySendMessage(hwnd, NativeMethods.TVM_ENDEDITLABELNOW, IntPtr.Zero, IntPtr.Zero);

            // Need to give some time for the control to do all its proceeing.
            bool wasTextSet = false;
            for(int i=0; i < 10; i++)
            {
                System.Threading.Thread.Sleep(1);

                // Now see if the treeviewitem really got set.
                if (text.Equals(WindowsTreeView.GetItemText(hwnd, item)))
                {
                    wasTextSet = true;
                    break;
                }
            }
            if (!wasTextSet)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            return true;
        }

        //Converts child id to handle to tree item.
        private static IntPtr TreeItemFromChildID(IntPtr hwnd, int idChild)
        {
            IntPtr hItem = Misc.ProxySendMessage(hwnd, NativeMethods.TVM_MAPACCIDTOHTREEITEM, new IntPtr(idChild), IntPtr.Zero);

            if (hItem != IntPtr.Zero)
            {
                return hItem;
            }

            // We may have received a NULL hitem because the idChild was bad.
            // If this control supports the mapping message (version >= 6) then
            // don't assume its old and fallback to previous behavior.
            // If this SendMessage fails or this control is old go ahead and just cast it to an HTREEITEM
            int lCommonControlVersion = Misc.ProxySendMessageInt(hwnd, NativeMethods.CCM_GETVERSION, IntPtr.Zero, IntPtr.Zero);
            if (lCommonControlVersion >= 6)
            {
                return IntPtr.Zero;
            }


#if WIN64
            return IntPtr.Zero;
#else
            //Fallback for older 32-bit comctls that don't implement the mapping message
            return new IntPtr(idChild);
#endif
        }
        #endregion

        #endregion Private Methods

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Represent the state of the checkbox
        // Remark: ListView and TreeView will share this enum
        private enum CheckState: int
        {
            NoCheckbox = -1,
            Unchecked = 0,
            Checked = 1
        }

        //  const defining the container (if depth == Proxy_container, the proxy is on the container, negative scroll bar, else an item)
        private enum TVItem
        {
            // must be different than the VtScroll and HzScroll
            TopLevel = 0,
        }

        #endregion

        // ------------------------------------------------------
        //
        //  TreeViewItem Private Class
        //
        //------------------------------------------------------

        #region TreeViewItem

        // Summary description for TreeViewItem.
        class TreeViewItem : ProxyFragment, ISelectionItemProvider, IExpandCollapseProvider, IValueProvider, IToggleProvider, IScrollItemProvider, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TreeViewItem (IntPtr hwnd, ProxyFragment parent, IntPtr hItem, int depth)
                : base(hwnd, parent, depth)
            {
                // windows handle to this substree
                _hItem = hItem;

                // Set the strings to return properly the properties.
                _cControlType = ControlType.TreeItem;
                _fHasPersistentID = false;
                _fIsKeyboardFocusable = true;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxyFragment Interface

            // Returns the next sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null if no next child
            internal override ProxySimple GetNextSibling (ProxySimple child)
            {
                CheckForElementAvailable ();
                return IsItemExpanded (_hwnd, _hItem) ? NextSibling (child) : null;
            }

            // Returns the previous sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null is no previous
            internal override ProxySimple GetPreviousSibling (ProxySimple child)
            {
                CheckForElementAvailable ();
                return IsItemExpanded (_hwnd, _hItem) ? PreviousSibling (child) : null;
            }

            // Returns the first child element in the raw hierarchy.
            internal override ProxySimple GetFirstChild ()
            {
                CheckForElementAvailable ();
                return IsItemExpanded (_hwnd, _hItem) ? FirstChild () : null;
            }

            // Returns the last child element in the raw hierarchy.
            internal override ProxySimple GetLastChild ()
            {
                CheckForElementAvailable ();
                return IsItemExpanded (_hwnd, _hItem) ? LastChild () : null;
            }

            #endregion

            #region ProxySimple Interface

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                CheckForElementAvailable ();

                // This is an item
                if (iid == SelectionItemPattern.Pattern
#if HIERARCHY_PATTERN
                    || iid == HierarchyItemPattern.Pattern
#endif
                    )
                {
                    return this;
                }
                else if (iid == ScrollItemPattern.Pattern && WindowScroll.IsScrollable(_hwnd))
                {
                    return this;
                }
                else if (iid == ValuePattern.Pattern && IsItemEditable())
                {
                    return this;
                }
                else if (iid == ExpandCollapsePattern.Pattern)
                {
                    return this;
                }
                else if (iid == TogglePattern.Pattern && IsItemWithCheckbox())
                {
                    return this;
                }
                //Special case handling for vista windows explorer's tree view implementation.
                //Reason: Selecting the node does not refresh the folder items in the right pane
                //So, implement the Invoke pattern and let the client call invoke to get behavior
                //similar to windows explorer of windows XP
                else if (iid == InvokePattern.Pattern)
                {
                    //This condition is to avoid calling CreateNativeFromEvent repeatedly.
                    if (_nativeAcc == null && System.Environment.OSVersion.Version.Major >= 6 && Misc.IsWindowInGivenProcess(_hwnd, "explorer"))
                    {
                        int childId = ChildIDFromTVItem();
                        _nativeAcc = Accessible.CreateNativeFromEvent(_hwnd, NativeMethods.OBJID_CLIENT, childId);
                    }
                    //This is to check whether native IAccessible is implemented and only then expose the invoke pattern.
                    if (_nativeAcc != null)
                    {
                        return this;
                    }
                }

                return null;
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    CheckForElementAvailable ();

                    // Don't need to normalize, GetItemRect returns absolute coordinates.
                    return WindowsTreeView.GetItemRect(_hwnd, _hItem, true).ToRect(false);
                }
            }

            // Process all the Element Properties
            internal override object GetElementProperty(AutomationProperty idProp)
            {
                if (idProp == AutomationElement.IsOffscreenProperty)
                {
                    NativeMethods.Win32Rect itemRect = GetItemRect(_hwnd, _hItem, true);

                    // Need to check if this item is visible on the whole control not just its immediate parent.
                    if (!Misc.IsItemVisible(_hwnd, ref itemRect))
                    {
                        return true;
                    }
                }

                return base.GetElementProperty(idProp);
            }

            //Gets the controls help text
            internal override string HelpText
            {
                get
                {
                    CheckForElementAvailable();

                    IntPtr hwndToolTip = Misc.ProxySendMessage(_hwnd, NativeMethods.TVM_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
                    return Misc.GetItemToolTipText(_hwnd, hwndToolTip, _item);
                }
            }

            internal override bool IsOffscreen()
            {
                Rect itemRect = BoundingRectangle;

                if (itemRect.IsEmpty)
                {
                    return true;
                }

                // Sub-TreeViewItems are not with in the parents bounding rectangle.  So check if
                // the sub item is visible with in the whole control.
                ProxySimple parent;
                ProxySimple current = this;
                do
                {
                    parent = current.GetParent();
                    if (parent is WindowsTreeView)
                    {
                        break;
                    }
                    current = parent;
                } while (parent != null);

                if (parent != null)
                {
                    if ((bool)parent.GetElementProperty(AutomationElement.IsOffscreenProperty))
                    {
                        return true;
                    }

                    // Now check to see if this item in visible on its parent
                    Rect parentRect = parent.BoundingRectangle;
                    if (!parentRect.IsEmpty && !Misc.IsItemVisible(ref parentRect, ref itemRect))
                    {
                        return true;
                    }
                }

                // if this element is not on any monitor than it is off the screen.
                NativeMethods.Win32Rect itemWin32Rect = new NativeMethods.Win32Rect(itemRect);
                return UnsafeNativeMethods.MonitorFromRect(ref itemWin32Rect, UnsafeNativeMethods.MONITOR_DEFAULTTONULL) == IntPtr.Zero;
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    CheckForElementAvailable();
                    return Text;
                }
            }

            // Returns an item corresponding to the focused element (if there is one), or null otherwise.
            internal override bool SetFocus ()
            {
                // try to select an item, hence unselecting everything else
                return WindowsTreeView.SelectItem (_hwnd, _hItem);
            }

            // Returns the Run Time Id, an array of ints as the concatenation of IDs.
            // Remark: Implement it locally, since it is normal to have many items on the same
            // level, which in turn leads to the duplication of the runtime id
            internal override int[] GetRuntimeId()
            {
                CheckForElementAvailable ();

                if (Marshal.SizeOf(_hItem.GetType()) > sizeof(int))
                {
                    // if this is 64 bit break the _hItem into two parts so we don't overflow
                    int highPart = NativeMethods.Util.HIDWORD((long)_hItem);
                    int lowPart = NativeMethods.Util.LODWORD((long)_hItem);

                    return new int [4] { ProxySimple.Win32ProviderRuntimeIdBase, unchecked((int)(long)_hwnd), highPart, lowPart };
                }
                else
                {
                    return new int[3] { ProxySimple.Win32ProviderRuntimeIdBase, (int)_hwnd, (int)_hItem };
                }
            }

            #endregion

            #region SelectionItem Pattern

            // Selects this element
            void ISelectionItemProvider.Select ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                // simple case: item already selected
                if (IsItemSelected())
                {
                    return;
                }

                if (HasMSAAImageMap())
                {
                    Invoke();
                }
                else
                {
                    // try to select an item, hence unselecting everything else
                    if (!WindowsTreeView.SelectItem(_hwnd, _hItem))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                }
            }

            // Adds this element to the selection
            void ISelectionItemProvider.AddToSelection ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                // simple case: item already selected
                if (IsItemSelected())
                {
                    return;
                }

                IRawElementProviderSimple container = ((ISelectionItemProvider)this).SelectionContainer;
                bool selectionRequired = container != null ? ((ISelectionProvider)container).IsSelectionRequired : true;

                // For single selection containers that IsSelectionRequired == false and nothing is selected
                // an AddToSelection is valid.
                if (selectionRequired || WindowsTreeView.GetSelection(_hwnd) != IntPtr.Zero)
                {
                    // NOTE: TreeView do not natively support multiple selection
                    throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
                }

                // Since nothing is selected try to select the item
                if (!WindowsTreeView.SelectItem(_hwnd, _hItem))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }

            // Removes this element from the selection
            void ISelectionItemProvider.RemoveFromSelection ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                if (IsItemSelected())
                {
                    // NOTE: TreeView do not natively support multiple selection
                    throw new InvalidOperationException(SR.Get(SRID.SelectionRequired));
                }
            }

            // True if this element is part of the the selection
            bool ISelectionItemProvider.IsSelected
            {
                get
                {
                    CheckForElementAvailable();
                    return IsItemSelected();
                }
            }

            // Returns the container for this element
            IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
            {
                get
                {
                    CheckForElementAvailable ();

                    for (ProxyFragment topLevelParent = _parent; ; topLevelParent = topLevelParent._parent)
                    {
                        if (topLevelParent._parent == null)
                        {
                            System.Diagnostics.Debug.Assert (topLevelParent is WindowsTreeView, "Invalid Parent for a TreeView Item");
                            return topLevelParent;
                        }
                    }
               }
            }

            #endregion SelectionItem Pattern

            #region ExpandCollapse Pattern

            // Show all Children
            void IExpandCollapseProvider.Expand ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                // check if item can be expanded
                switch (GetExpandCollapseState())
                {
                    default:
                    case ExpandCollapseState.LeafNode :
                        throw new InvalidOperationException (SR.Get(SRID.OperationCannotBePerformed));

                    case ExpandCollapseState.Expanded :
                        // Simple case, already done.
                        break;

                    case ExpandCollapseState.Collapsed :
                        // Do the action.
                        WindowsTreeView.Expand (_hwnd, _hItem);
                        break;
                }
            }

            // Hide all Children
            void IExpandCollapseProvider.Collapse ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                // check if item can be collapsed
                switch (GetExpandCollapseState())
                {
                    default:
                    case ExpandCollapseState.LeafNode :
                        throw new InvalidOperationException (SR.Get(SRID.OperationCannotBePerformed));

                    case ExpandCollapseState.Expanded :
                        // Do the action.
                        WindowsTreeView.Collapse (_hwnd, _hItem);
                        break;

                    case ExpandCollapseState.Collapsed :
                        // Simple case, already done.
                        break;
                }
            }

            // Indicates an elements current Collapsed or Expanded state
            ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
            {
                get
                {
                    CheckForElementAvailable();
                    return GetExpandCollapseState();
                }
            }

            #endregion ExpandCollapse Pattern

            #region Value Pattern

            void IValueProvider.SetValue (string val)
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                if (!WindowsTreeView.SetItemText(_hwnd, _hItem, val))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }

            // Request to get the value that this UI element is representing as a string
            string IValueProvider.Value
            {
                get
                {
                    CheckForElementAvailable();
                    return Text;
                }
            }

            // Read only status
            bool IValueProvider.IsReadOnly
            {
                get
                {
                    CheckForElementAvailable();
                    return false;
                }
            }

            #endregion IValueProvider

            #region IToggleProvider

            void IToggleProvider.Toggle()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                CheckForElementAvailable();

                if (HasMSAAImageMap())
                {
                    Invoke();
                }
                else
                {
                    WindowsTreeView.SetCheckState(_hwnd, _hItem, GetToggleState() != ToggleState.On);
                }
            }

            ToggleState IToggleProvider.ToggleState
            {
                get
                {
                    CheckForElementAvailable();

                    return GetToggleState();
                }
            }

            #endregion IToggleProvider

            #region ScrollItem Pattern

            void IScrollItemProvider.ScrollIntoView()
            {
                CheckForElementAvailable();

                if (!WindowScroll.IsScrollable(_hwnd))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                Misc.SetFocus(_hwnd);

                // Currently this ignores the alignToTop, as there is no easy way to set where it
                // will be in the Treeview, it just makes sure it is visible.
                Misc.ProxySendMessage(_hwnd, NativeMethods.TVM_ENSUREVISIBLE, IntPtr.Zero, _hItem);
            }

            #endregion ScrollItem Pattern

            #region Invoke Pattern
            //Special case handling for vista windows explorer's tree view implementation.
            //when the client calls Invoke method, call DoDefaultAction on its native
            //IAccessible (this should have been set when GetPatternProviders
            //is called earlier). Vista Explorer's treeview has its own implementation of DoDefaultAction
            //to display the subfolders and files of the selected folder on the right pane.
            void IInvokeProvider.Invoke()
            {
                if (_nativeAcc != null)
                {
                    SetFocus();
                    _nativeAcc.DoDefaultAction();
                }
            }
            #endregion Invoke Pattern

            // ------------------------------------------------------
            //
            // Internal Methods
            //
            // ------------------------------------------------------

            #region Internal Methods

            internal void RaiseStructureChangedEvent ()
            {
                StructureChangeType changeType = GetExpandCollapseState() == ExpandCollapseState.Expanded ? StructureChangeType.ChildrenBulkAdded : StructureChangeType.ChildrenBulkRemoved;
                AutomationInteropProvider.RaiseStructureChangedEvent( this, new StructureChangedEventArgs( changeType, GetRuntimeId() ) );
            }

            internal void RaiseExpandCollapsedStateChangedEvent ()
            {
                AutomationInteropProvider.RaiseAutomationPropertyChangedEvent(this, new AutomationPropertyChangedEventArgs(ExpandCollapsePattern.ExpandCollapseStateProperty, null, GetExpandCollapseState()));
            }

            #endregion

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            // This routine is only called on elements belonging to an hwnd that has the focus.
            protected override bool IsFocused ()
            {
                int selected = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TVM_GETITEMSTATE, _hItem, new IntPtr(NativeMethods.TVIS_SELECTED));

                return Misc.IsBitSet(selected, NativeMethods.TVIS_SELECTED);
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Methods
            //
            // ------------------------------------------------------

            #region Private Methods

            // Go up the hierarchy of parents to make sure that they are all expanded.
            // If one of the tree view node is not expanded, it means that the element
            // is not visible
            private void CheckForElementAvailable()
            {
                TreeViewItem current = this;
                while ((current = current.GetParent() as TreeViewItem) != null)
                {
                    if (!WindowsTreeView.IsItemExpanded (_hwnd, current._hItem))
                    {
                        throw new ElementNotAvailableException ();
                    }
                }
            }

            // Returns the next sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null if no next child.
            private ProxySimple NextSibling (ProxySimple child)
            {
                IntPtr hNext = WindowsTreeView.GetNextItem (_hwnd, ((TreeViewItem) child)._hItem);

                return hNext != IntPtr.Zero ? new TreeViewItem(_hwnd, this, hNext, _item + 1) : null;
            }

            // Returns the previous sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null is no previous.
            private ProxySimple PreviousSibling (ProxySimple child)
            {
                IntPtr hPrev = WindowsTreeView.GetPreviousItem (_hwnd, ((TreeViewItem) child)._hItem);

                return hPrev != IntPtr.Zero ? new TreeViewItem(_hwnd, this, hPrev, _item + 1) : null;
            }

            // Returns the first child element in the raw hierarchy.
            private ProxySimple FirstChild ()
            {
                IntPtr hChild = WindowsTreeView.GetFirstChild (_hwnd, _hItem);

                return hChild != IntPtr.Zero ? new TreeViewItem(_hwnd, this, hChild, _item + 1) : null;
            }

            // Returns the last child element in the raw hierarchy.
            private ProxySimple LastChild ()
            {
                if (!IsItemExpanded (_hwnd, _hItem))
                {
                    return null;
                }

                IntPtr hChild = WindowsTreeView.GetFirstChild (_hwnd, _hItem);

                if (hChild != IntPtr.Zero)
                {
                    // First Child found, now retrieve the last one (no specific msg, need to walk thru all of them)
                    for (IntPtr temp = WindowsTreeView.GetNextItem (_hwnd, hChild); temp != IntPtr.Zero; temp = WindowsTreeView.GetNextItem (_hwnd, hChild))
                    {
                        hChild = temp;
                    }

                    return new TreeViewItem(_hwnd, this, hChild, _item + 1);
                }

                return null;
            }

            // Retrieve state of the treeview item
            private ExpandCollapseState GetExpandCollapseState()
            {
                bool expanded = WindowsTreeView.IsItemExpanded (_hwnd, _hItem);

                if (expanded)
                {
                    return ExpandCollapseState.Expanded;
                }

                // need to decide between leaf and collapsed
                bool hasChildren = WindowsTreeView.TreeViewItem_HasChildren (_hwnd, _hItem);

                return (hasChildren) ? ExpandCollapseState.Collapsed : ExpandCollapseState.LeafNode;
            }

            // get the current state for the tree view item checkbox
            private ToggleState GetToggleState ()
            {
                WindowsTreeView.CheckState state = WindowsTreeView.CheckState.NoCheckbox;

                if (HasMSAAImageMap())
                {
                    int image;
                    uint overlay;
                    uint stateMSAA;

                    if (GetItemImageIndex(out image, out overlay, out stateMSAA))
                    {
                        if (stateMSAA == 0)
                        {
                            GetStateFromStateImageMap(image, ref stateMSAA);
                        }

                        state = Misc.IsBitSet((int)stateMSAA, (int)AccessibleState.Checked) ? WindowsTreeView.CheckState.Checked : WindowsTreeView.CheckState.Unchecked;
                    }
                }

                if (state == WindowsTreeView.CheckState.NoCheckbox)
                {
                    state = (WindowsTreeView.CheckState)WindowsTreeView.GetCheckState(_hwnd, _hItem);
                }

                switch (state)
                {
                    case WindowsTreeView.CheckState.NoCheckbox :
                        {
                            // we should not call this method on the non-checkboxed treeview items
                            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                        }

                    case WindowsTreeView.CheckState.Checked :
                        {
                            return ToggleState.On;
                        }

                    case WindowsTreeView.CheckState.Unchecked :
                        {
                            return ToggleState.Off;
                        }
                }

                // developer defined custom values which cannot be interpret outside of the app's scope
                return ToggleState.Indeterminate;
            }

            // Check the checked state of a MSAA treeview radio button item.
            private bool GetCheckState()
            {
                WindowsTreeView.CheckState state = WindowsTreeView.CheckState.NoCheckbox;

                int image;
                uint overlay;
                uint stateMSAA;

                if (GetItemImageIndex(out image, out overlay, out stateMSAA))
                {
                    if (stateMSAA == 0)
                    {
                        GetStateFromStateImageMap(image, ref stateMSAA);
                    }

                    state = Misc.IsBitSet((int)stateMSAA, (int)AccessibleState.Checked) ? WindowsTreeView.CheckState.Checked : WindowsTreeView.CheckState.Unchecked;
                }

                return state == WindowsTreeView.CheckState.Checked;
            }

            private void Invoke()
            {
                // get item rect
                NativeMethods.Win32Rect rectItem = WindowsTreeView.GetItemRect(_hwnd, _hItem, true);

                if (rectItem.IsEmpty)
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // get control coordinates at which we will "click"
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point(((rectItem.left + rectItem.right) / 2), ((rectItem.top + rectItem.bottom) / 2));

                // convert back to client
                if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref pt, 1))
                {
                    // click
                    SimulateClick(pt);
                }
            }

            private bool IsItemEditable()
            {
                return Misc.IsBitSet(WindowStyle, NativeMethods.TVS_EDITLABELS);
            }

            // detect if given tree view item selected
            private bool IsItemSelected()
            {
                int selected = WindowsTreeView.GetItemState(_hwnd, _hItem, NativeMethods.TVIS_SELECTED);

                if (Misc.IsBitSet(selected, NativeMethods.TVIS_SELECTED))
                {
                    return true;
                }

                // Now check to see if this is a MSAA treeview radiobutton item
                return GetCheckState();
            }

            // detect if current item has a checkbox associated with it
            private bool IsItemWithCheckbox ()
            {
                bool isCheckbox = Misc.IsBitSet(WindowStyle, NativeMethods.TVS_CHECKBOXES);

                if (isCheckbox)
                {
                    // treeview does support the checkboxes
                    // now we need to make sure that our item supports the checkbox
                    isCheckbox = WindowsTreeView.CheckState.NoCheckbox != (WindowsTreeView.CheckState)WindowsTreeView.GetCheckState(_hwnd, _hItem);
                }

                if (!isCheckbox)
                {
                    int image;
                    uint overlay;
                    uint state;

                    if (GetItemImageIndex(out image, out overlay, out state))
                    {
                        if (overlay == 0)
                        {
                            GetRoleFromStateImageMap(image, ref overlay);
                        }

                        isCheckbox = (AccessibleRole)overlay == AccessibleRole.CheckButton;
                    }
                }

                return isCheckbox;
            }

            //  Return the TreeView Item Text or the Container text (usually hiden text)
            private string Text
            {
                get
                {
                    // this is an item
                    return WindowsTreeView.GetItemText (_hwnd, _hItem);
                }
            }

            // simulate click via posting WM_LBUTTONDOWN(UP)
            private void SimulateClick(NativeMethods.Win32Point pt)
            {
                // Fails if a SendMessage is used instead of the Post.
                Misc.PostMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
                Misc.PostMessage(_hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
            }

            private bool GetItemImageIndex(out int image, out uint overlay, out uint state)
            {
                NativeMethods.TVITEM treeItem;
                if (WindowsTreeView.GetItem(_hwnd, _hItem, NativeMethods.TVIF_IMAGE | NativeMethods.TVIF_STATE, out treeItem))
                {
                    image = treeItem.iImage;
                    overlay = (treeItem.state >> 8) & 0x0F;
                    state = (treeItem.state >> 12) & 0x0F;

                    return true;
                }

                image = 0;
                overlay = 0;
                state = 0;

                return false;
            }


            private bool GetStateImageMapEnt(int image, ref uint state, ref uint role)
            {
                // NOTE: This method may have issues with cross proc/cross bitness.

                IntPtr address = UnsafeNativeMethods.GetProp(_hwnd, "MSAAStateImageMapAddr");
                if (address == IntPtr.Zero)
                {
                    return false;
                }

                int numStates = unchecked((int)UnsafeNativeMethods.GetProp(_hwnd, "MSAAStateImageMapCount"));
                if (numStates == 0)
                {
                    return false;
                }

                // <= used since number is a 1-based count, iImage is a 0-based index.
                // If iImage is 0, should be at least one state.
                if (numStates <= image)
                {
                    return false;
                }

                using (SafeProcessHandle hProcess = new SafeProcessHandle(_hwnd))
                {
                    if (hProcess.IsInvalid)
                    {
                        return false;
                    }

                    MSAASTATEIMAGEMAPENT ent = new MSAASTATEIMAGEMAPENT();
                    int readSize = Marshal.SizeOf(ent.GetType());
                    IntPtr count;

                    // Adjust to image into array...
                    IntPtr pAddress = new IntPtr((long)address + (image * readSize));

                    unsafe
                    {
                        if (!Misc.ReadProcessMemory(hProcess, pAddress, new IntPtr(&ent), new IntPtr(readSize), out count))
                        {
                            return false;
                        }
                    }

                    state = (uint)ent.state;
                    role = (uint)ent.role;
                }
                return true;
            }

            private bool GetRoleFromStateImageMap(int image, ref uint role)
            {
                uint state = unchecked((uint)-1);
                return GetStateImageMapEnt(image, ref state, ref role);
            }

            private bool GetStateFromStateImageMap(int image, ref uint state)
            {
                uint role = unchecked((uint)-1);
                return GetStateImageMapEnt(image, ref state, ref role);
            }

            private bool HasMSAAImageMap()
            {
                return UnsafeNativeMethods.GetProp(_hwnd, "MSAAStateImageMapAddr") != IntPtr.Zero;
            }

            //Wrapper method to get child id from treeview item.
            //Similar to OLEACC implementation
            private int ChildIDFromTVItem()
            {
                if (_hItem == IntPtr.Zero)
                    return 0;

                int childId = Misc.ProxySendMessageInt(_hwnd, TVM_MAPHTREEITEMTOACCID, _hItem, IntPtr.Zero);

                if( childId != 0 )
                {
                    return childId;
                }

            #if WIN64
                return 0;
            #else
                // Fallback for older 32-bit comctls that don't implement the mapping
                // message
                return _hItem.ToInt32();
            #endif
            }
            #endregion

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // The HTREEITEM value of the treeview item (internal)
            internal IntPtr _hItem;

            [StructLayout(LayoutKind.Sequential)]
            private struct MSAASTATEIMAGEMAPENT
            {
                internal int role;
                internal int state;
            }

            //native IAccessible interface for TreeViewItem for special handling in
            //Vista Windows Explorer
            private Accessible _nativeAcc;

            //Tree view item specific constants.
            private const int TV_FIRST = 0x1100;
            private const int TVM_MAPHTREEITEMTOACCID = TV_FIRST + 43;
            #endregion
        }

        #endregion
    }
}

