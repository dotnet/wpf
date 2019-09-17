// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 ListView Item proxy for the Start Menu.
//      The Start Menu has a special use of ListViews.  The items in the
//      list are treated like menuitems.  To expose this special behavor
//      data from MSAA is need. The Shell team has implemented a special
//      IAccessible to support the Sart Menu.
//

using System;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using Accessibility;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // This class will only change a couple of aspects of a ListViewItem.  So derive from the ListViewItem to 
    // retain the majority of the ListView item functionality.
    internal class ListViewItemStartMenu : ListViewItem
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListViewItemStartMenu(IntPtr hwnd, ProxyFragment parent, int item, IAccessible acc)
            : base (hwnd, parent, item)
        {
            // The items are zero based, i.e. the first listview item is item 0.  The
            // zero item in MSAA is self, so need to add one to the item to get the 
            // correct Accessible child.
            AccessibleRole role = Accessible.GetRole(acc, item + 1);

            // Normal Listview items should be of control type listitem.  But
            // the Listview items in the Start Menu act like menuitems.  Get the Role
            // from IAccessible interface implemented by the Shell team and set the
            // control type.
            if (role == AccessibleRole.MenuItem)
            {
                _cControlType = ControlType.MenuItem;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "The listview item on the Start Menu has an unexpected IAccessible role!");
            }
        }

        #endregion Constructos

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            // Treate these listview items as menuitems and only support Invoke or Expand/Collapse patterns.
            // Invoke Pattern needs to be supported when the item has no children.  When the item does have
            // children it needs to support ExpandCollapse Pattern.

            if (iid == InvokePattern.Pattern)
            {
                return this;
            }

            return null;
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            // Normal Listview items do not have a concept of an AccessKey.  But
            // the Listview items in the Start Menu does.  This information is
            // in the IAccessible interface implemented by the Shell team.
            if (idProp == AutomationElement.AccessKeyProperty)
            {
                // The IAccessible should be valid here since it is the cached value in ProxySimple.
                System.Diagnostics.Debug.Assert(AccessibleObject != null, "Failed to get a valid IAccessible!");

                try
                {
                    string key = AccessibleObject.get_accKeyboardShortcut(_item + 1);
                    if (!string.IsNullOrEmpty(key))
                    {
                        return SR.Get(SRID.KeyAlt) + "+" + key;
                    }
                }
                catch (Exception e)
                {
                    if (Misc.IsCriticalException(e))
                    {
                        throw;
                    }
                }
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                return IsFocused();
            }
            

            return base.GetElementProperty(idProp);
        }

        #endregion
    }
}
