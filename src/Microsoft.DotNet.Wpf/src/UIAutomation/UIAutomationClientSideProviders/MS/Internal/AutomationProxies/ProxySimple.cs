// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Base class for all the Win32 and office Controls.
//
//              The ProxySimple class is the base class for the leafs
//              in the raw element tree. Also proxy that directly derives
//              from this clas should not care about events
//
//              The UIAutomation Simple class is limited to UI elements that are
//              Hwnd based. This makes it of little use for Win32 and office controls.
//              The ProxySimple class removes this limitation. This leads to a couple of
//              changes; RuntTimeID and BoundingRect must be implemented by this object.
//
//
//              Class ProxySimple: IRawElementProviderFragment, IRawElementProviderSimple
//                  BoundingRectangle
//                  RuntimeId
//                  Properties
//                  GetPatterns
//                  SetFocus
//
//              Example: ComboboxButton, MenuItem, Office CommandBar button, ListViewSubitem
//
//
//


// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections;
using Accessibility;
using System.Windows;
using System.Windows.Input;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Base Class for all the Windows Control.
    // Implements the default behavior
    //
    // The distinction between Proxy siblings is made through an ID (called _item).
    // The underlying hwnd is kept, as the proxy parent and a flag to _fSubtree.
    class ProxySimple : IRawElementProviderSimple, IRawElementProviderFragment
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Constructor.
        // Param "hwnd" is the handle to underlying window
        // Param "parent" is the Parent, must be a ProxyFragment
        // Param "item" is the ID of item to represent
        internal ProxySimple(IntPtr hwnd, ProxyFragment parent, int item)
        {
            _hwnd = hwnd;
            _item = item;
            _parent = parent;

            // is element a leaf?
            _fSubTree = (_parent != null);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Methods

        internal virtual ProviderOptions ProviderOptions
        {
            get
            {
                return ProviderOptions.ClientSideProvider;
            }
        }

        // Returns the bounding rectangle of the control.
        // If the control is not an hwnd, the subclass should implement this call
        internal virtual Rect BoundingRectangle
        {
            get
            {
                if (_hwnd == IntPtr.Zero)
                {
                    return Rect.Empty;
                }

                NativeMethods.Win32Rect controlRectangle = NativeMethods.Win32Rect.Empty;

                if (!Misc.GetWindowRect(_hwnd, ref controlRectangle))
                {
                    return Rect.Empty;
                }
                // Don't normalize, consumers & subclasses will normalize with conditionals
                return controlRectangle.ToRect(false);
            }
        }

        // Sets the focus to this item.
        // By default, fails
        internal virtual bool SetFocus()
        {
            return false;
        }

        // Returns the Run Time Id.
        //
        // The RunTimeID is a array of int with RID [0] set to 1 and RID [1] the hwnd
        // identifier. The remaining of the chain are values one per sub node in
        // the raw element tree.
        // Avalon and other none hwnd based elements have RunTimeIDs that never starts
        //  with '1'. This makes the RunTimeId for Win32 controls uniq.
        //
        // By default the _item data member is used as ID for each depth
        // in the element tree
        internal virtual int [] GetRuntimeId ()
        {
            if (_fSubTree && !IsHwndElement())
            {
                // add the id for this level at the end of the chain
                return Misc.AppendToRuntimeId(GetParent().GetRuntimeId(), _item);
            }
            else
            {
                // UIA handles runtimeID for the HWND part for us
                return null;
            }
        }

        // Get unique ID for this element...
        // This is the internal version of GetRuntimeId called when a complete RuntimeId is needed internally (e.g.
        // RuntimeId is needed to create StructureChangedEventArgs) vs when UIAutomation asks for a RuntimeId
        // through IRawElementProviderFragment. WCTL #32188 : We need a helper method in UIAutomationCore that takes
        // an hwnd and returns a RuntimeId. Symptom of this being broken is getting InvalidOperationException
        // during events with message: Value cannot be null.  Parameter name: runtimeId.
        internal int[] MakeRuntimeId()
        {
            int idLen = ( _fSubTree && !IsHwndElement() ) ? 3 : 2;
            int[] id = new int[idLen];

            // Base runtime id is the number indicating Win32Provider + hwnd
            id[0] = ProxySimple.Win32ProviderRuntimeIdBase;
            id[1] = _hwnd.ToInt32();

            // Append part id to make this unique
            if ( idLen == 3 )
            {
                id[2] = _item;
            }
            return id;
        }

        internal virtual IRawElementProviderSimple HostRawElementProvider
        {
            get
            {
                if (_hwnd == IntPtr.Zero || (GetParent() != null && GetParent()._hwnd == _hwnd))
                {
                    return null;
                }

                return AutomationInteropProvider.HostProviderFromHandle(_hwnd);
            }
        }

        internal virtual ProxySimple GetParent()
        {
            return _parent;
        }

        // Process all the Element Properties
        internal virtual object GetElementProperty(AutomationProperty idProp)
        {
            // we can handle some properties locally
            if (idProp == AutomationElement.LocalizedControlTypeProperty)
            {
                return _sType;
            }
            else if(idProp == AutomationElement.ControlTypeProperty)
            {
                return _cControlType != null ? (object)_cControlType.Id : null;
            }
            else if (idProp == AutomationElement.IsContentElementProperty)
            {
                return _item >= 0 && _fIsContent;
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                return LocalizedName;
            }
            else if (idProp == AutomationElement.AccessKeyProperty)
            {
                return GetAccessKey();
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                return Misc.IsEnabled(_hwnd);
            }
            else if (idProp == AutomationElement.IsKeyboardFocusableProperty)
            {
                return IsKeyboardFocusable();
            }
            else if (idProp == AutomationElement.ProcessIdProperty)
            {
                // Get the pid of the process that the HWND lives in, not the
                // pid that this proxy lives in
                uint pid;
                Misc.GetWindowThreadProcessId(_hwnd, out pid);
                return (int)pid;
            }
            else if (idProp == AutomationElement.ClickablePointProperty)
            {
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point();

                if (GetClickablePoint(out pt, !IsHwndElement()))
                {
                    // Due to P/Invoke marshalling issues, the reurn value is in the
                    // form of a {x,y} array instead of using the Point datatype
                    return new double[] { pt.x, pt.y };
                }

                return AutomationElement.NotSupported;
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                // Check first if the hwnd has the Focus
                // Punt if not the case, drill down otherwise
                // If already focused, leave as-is. Calling SetForegroundWindow
                // on an already focused HWND will remove focus!
                return Misc.GetFocusedWindow() == _hwnd ? IsFocused() : false;
            }
            else if (idProp == AutomationElement.AutomationIdProperty)
            {
                // PerSharp/PreFast will flag this as a warning 6507/56507: Prefer 'string.IsNullOrEmpty(_sAutomationId)' over checks for null and/or emptiness.
                // _sAutomationId being null is invalid, while being empty is a valid state.
                // The use of IsNullOrEmpty while hide this.
#pragma warning suppress 6507
                System.Diagnostics.Debug.Assert(_sAutomationId != null, "_sAutomationId is null!");
#pragma warning suppress 6507
                return _sAutomationId.Length > 0 ? _sAutomationId : null;
            }
            else if (idProp == AutomationElement.IsOffscreenProperty)
            {
                return IsOffscreen();
            }
            else if (idProp == AutomationElement.HelpTextProperty)
            {
                return HelpText;
            }
            else if (idProp == AutomationElement.FrameworkIdProperty)
            {
                return WindowsFormsHelper.IsWindowsFormsControl(_hwnd) ? "WinForm" : "Win32";
            }

            return null;
        }

        internal virtual bool IsKeyboardFocusable()
        {
            // if it curently has focus it is obviosly focusable
            if (Misc.GetFocusedWindow() == _hwnd && IsFocused())
            {
                return true;
            }

            // If it's visible and enabled it might be focusable 
            if (SafeNativeMethods.IsWindowVisible(_hwnd) && (bool)GetElementProperty(AutomationElement.IsEnabledProperty))
            {
                // If it is something that we know is focusable and have marked it that way in the specific
                // proxy it should be focusable.
                if (IsHwndElement())
                {
                    // For a control that has the WS_TABSTOP style set, it should be focusable.
                    // Toolbars are genrealy not focusable but the short cut toolbar on the start menu is.
                    // The WS_TABSTOP will pick this up.
                    if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_TABSTOP))
                    {
                        return true;
                    }
                    else
                    {
                        return _fIsKeyboardFocusable;
                    }
                }
                else
                {
                    return _fIsKeyboardFocusable;
                }
            }

            return false;
        }

        internal virtual bool IsOffscreen()
        {
            Rect itemRect = BoundingRectangle;

            if (itemRect.IsEmpty)
            {
                return true;
            }

            // As per the specs, IsOffscreen only takes immediate parent-child relationship into account,
            // so we only need to check if this item in offscreen with respect to its immediate parent.
            ProxySimple parent = GetParent();
            if (parent != null )
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

        internal virtual string GetAccessKey()
        {
            // If the control is part of a dialog box or a form,
            // get the accelerator from the static preceding that control
            // on the dialog.
            if (GetParent() == null && (bool)GetElementProperty(AutomationElement.IsKeyboardFocusableProperty))
            {
                string sRawName = Misc.GetControlName(_hwnd, false);

                return string.IsNullOrEmpty(sRawName) ? null : Misc.AccessKey(sRawName);
            }

            return null;
        }

        // Returns a pattern interface if supported.
        internal virtual object GetPatternProvider(AutomationPattern iid)
        {
            return null;
        }

        internal virtual ProxySimple[] GetEmbeddedFragmentRoots()
        {
            return null;
        }

        //Gets the controls help text
        internal virtual string HelpText
        {
            get
            {
                return null;
            }
        }

        // Gets the localized name
        internal virtual string LocalizedName
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Dispatch Event

        // Dispatch WinEvent notifications
        //
        // A Generic mechanism is implemented to support most WinEvents.
        // On reception of a WinEvent, a Proxy is created and a call to this method
        // is made. This method then raises a UIAutomation Events based on the
        // WinEvents IDs. The old value for a property is always set to null
        internal virtual void DispatchEvents(int eventId, object idProp, int idObject, int idChild)
        {
            EventManager.DispatchEvent(this, _hwnd, eventId, idProp, idObject);
        }

        internal virtual void RecursiveRaiseEvents(object idProp, AutomationPropertyChangedEventArgs e)
        {
            return;
        }


        #endregion

        #region IRawElementProviderSimple

        // ------------------------------------------------------
        //
        // Default implementation for the IRawElementProviderSimple.
        // Maps the UIAutomation methods to ProxySimple methods.
        //
        // ------------------------------------------------------

        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get
            {
                return ProviderOptions;
            }
        }

        // Return the context associated with this element
        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get
            {
                return HostRawElementProvider;
            }
        }

        // Request the closest rectangle encompassing this element
        Rect IRawElementProviderFragment.BoundingRectangle
        {
            get
            {
                // Spec says that if an element is offscreen, we have the option of letting
                // the rect pass through or returning Rect.Empty; here, we intentionally
                // let it pass through as a convenience for MITA.

                // ProxySimple.BoundingRectanlgle
                return BoundingRectangle;
            }
        }

        // Request to return the element in the specified direction
        // ProxySimple object are leaf so it returns null except for the parent
        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            System.Diagnostics.Debug.Assert(_parent != null, "Navigate: Leaf element does not have parent");
            switch (direction)
            {
                case NavigateDirection.Parent:
                    {
                        return GetParent();
                    }

                case NavigateDirection.NextSibling:
                    {
                        // NOTE: Do not use GetParent(), call _parent explicitly
                        return _parent.GetNextSibling(this);
                    }

                case NavigateDirection.PreviousSibling:
                    {
                        // NOTE: Do not use GetParent(), call _parent explicitly
                        return _parent.GetPreviousSibling(this);
                    }
            }
            return null;
        }

        IRawElementProviderFragmentRoot IRawElementProviderFragment.FragmentRoot
        {
            // NOTE: The implementation below is correct one.
            //       DO NOT CHANGE IT, since things will break
            //       There can be only 1 ROOT for each constellation                                   
            get
            {
                // Traverse up the parents until you find a node with no parents, this is the root
                ProxySimple walk = this;

                while (walk.GetParent() != null)
                {
                    walk = walk.GetParent();
                }

                return walk as IRawElementProviderFragmentRoot;
            }
        }

        // Returns the Run Time Id, an array of ints as the concatenation of IDs.
        int [] IRawElementProviderFragment.GetRuntimeId ()
        {
            //ProxySimple.GetRuntimeId ();
            return GetRuntimeId ();
        }

        // Returns a pattern interface if supported.
        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            AutomationPattern iid = AutomationPattern.LookupById(patternId);
            return GetPatternProvider(iid);
        }

        // Returns a given property
        // UIAutomation as a generic call for all the properties for all the patterns.
        // This routine splits properties per pattern and calls the appropriate routine
        // within a pattern.
        // A default implementation is provided for some properties
        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            AutomationProperty idProp = AutomationProperty.LookupById(propertyId);
            return GetElementProperty(idProp);
        }

        // If this UI is capable of hosting other UI that also supports UIAutomation,
        // and the subtree rooted at this element contains such hosted UI fragments,
        // this should return an array of those fragments.
        //
        // If this UI does not host other UI, it may return null.
        IRawElementProviderSimple[] IRawElementProviderFragment.GetEmbeddedFragmentRoots()
        {
            return GetEmbeddedFragmentRoots();
        }

        // Request that focus is set to this item.
        // The UIAutomation framework will ensure that the UI hosting this fragment
        // is already focused before calling this method, so this method should only
        // update its internal focus state; it should not attempt to give its own
        // HWND the focus, for example.
        void IRawElementProviderFragment.SetFocus()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // A number of the Override Proxies return null from the GetElementProperty() method.  If 
            // a SetFocus() was called on them, the case to bool will cause a NullReferenceException.
            // So make sure the return is of type bool before casting.
            bool isKeyboardFocusable = true;
            object isKeyboardFocusableProperty = GetElementProperty(AutomationElement.IsKeyboardFocusableProperty);
            if (isKeyboardFocusableProperty is bool)
            {
                isKeyboardFocusable = (bool)isKeyboardFocusableProperty;
            }

            // UIAutomation already focuses the containing HWND for us, so only need to
            // set focus on the item within that...
            if (isKeyboardFocusable)
            {
                // Then set the focus on this item (virtual methods)
                SetFocus();
                return;
            }

            throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns the clickable point on the element
        // In the case when clickable point is obtained - method returns true
        // In the case when clickable point cannot be obtained - method returns false
        internal bool GetClickablePoint(out NativeMethods.Win32Point pt, bool fClipClientRect)
        {
            NativeMethods.Win32Rect rcItem = new NativeMethods.Win32Rect(BoundingRectangle);

            // Intersect the bounding Rectangle with the client rectangle for framents
            // and simple items - use the override flag (used mostly for the non client area
            if (fClipClientRect && !_fNonClientAreaElement)
            {
                NativeMethods.Win32Rect rcOutside = new NativeMethods.Win32Rect();

                Misc.GetClientRectInScreenCoordinates(_hwnd, ref rcOutside);

                if (!Misc.IntersectRect(ref rcItem, ref rcOutside, ref rcItem))
                {
                    pt.x = pt.y = 0;
                    return false;
                }
            }

            ArrayList alIn = new ArrayList(100);
            ArrayList alOut = new ArrayList(100);

            // Get the mid point to start with
            pt.x = (rcItem.right - 1 + rcItem.left) / 2;
            pt.y = (rcItem.bottom - 1 + rcItem.top) / 2;
            alOut.Add(new ClickablePoint.CPRect(ref rcItem, true));

            // First go through all the children to exclude whatever is on top
            ProxyFragment proxyFrag = this as ProxyFragment;
            if (proxyFrag != null)
            {
                ClickablePoint.ExcludeChildren(proxyFrag, alIn, alOut);
            }

            return ClickablePoint.GetPoint(_hwnd, alIn, alOut, ref pt);
        }

        internal string GetAccessibleName(int item)
        {
            string name = null;

            IAccessible acc = AccessibleObject;
            if (acc != null)
            {
                name = acc.get_accName(item);
                name = string.IsNullOrEmpty(name) ? null : name;
            }

            return name;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Properties
        //
        // ------------------------------------------------------

        #region Internal Properties

        // Returns the IAccessible interface for the container object
        internal virtual IAccessible AccessibleObject
        {
            get
            {
                if (_IAccessible == null)
                {
                    Accessible acc = null;
                    // We need to go search for it
                    _IAccessible = Accessible.AccessibleObjectFromWindow(_hwnd, NativeMethods.OBJID_CLIENT, ref acc) == NativeMethods.S_OK ? acc.IAccessible : null;
                }

                return _IAccessible;
            }
            set
            {
                _IAccessible = value;
            }
        }

        // Get the hwnd for this element
        internal IntPtr WindowHandle
        {
            get
            {
                return _hwnd;
            }
        }

        // Reference to the window Handle
        internal int WindowStyle
        {
            get
            {
                return Misc.GetWindowStyle(_hwnd);
            }
        }

        //Gets the extended style of the window
        internal int WindowExStyle
        {
            get
            {
                return Misc.GetWindowExStyle(_hwnd);
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        #region Internal Fields

        // Reference to the window Handle
        internal IntPtr _hwnd;

        // Is used to discriminate between items in a collection.
        internal int _item;

        // Parent of a subtree.
        internal ProxyFragment _parent;

        // Localized Control type name.  If the control has a ControlType this should not be set.
        internal string _sType;

        // Must be set by a subclass, used to return the automation id
        internal string _sAutomationId = "";

        // Used by the IsFocussable Property.
        // By default all elements are not Keyboard focusable, overide this flag
        // to change the default behavior.
        internal bool _fIsKeyboardFocusable;

        // Top level Desktop window
        internal static IntPtr _hwndDesktop = UnsafeNativeMethods.GetDesktopWindow();

        // Identifies an element as hwnd-based; used as the first value in RuntimeId for Win32 providers.
        internal const int Win32ProviderRuntimeIdBase = 1;

        #endregion


        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods

        // This routine is only called on elements belonging to an hwnd
        // that has the focus.
        // Overload this routine for sub elements within an hwnd that can
        // have the focus, tab items, listbox items ...
        // The default implemention is to return true for proxy element 
        // that are hwnd.
        protected virtual bool IsFocused ()
        {
            return this is ProxyHwnd;
        }

        protected bool IsHwndElement()
        {
            return this is ProxyHwnd;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Protected Fields
        //
        // ------------------------------------------------------

        #region Protected Fields

        // True if this is a WindowsForms control.
        // This value is cached and calculated only when needed
        protected WindowsFormsHelper.FormControlState _windowsForms = WindowsFormsHelper.FormControlState.Undeterminate;

        // Which Controltype it is, Must be set by a subclass
        protected ControlType _cControlType;

        // Must be set by a subclass,
        // Prevents the generic generation of the persistent IDs
        protected bool _fHasPersistentID = true;

        // Used by the GetClickablePoint Logic to figure out if clipping 
        // must happen on the Client Rect or the Non ClientRect
        protected bool _fNonClientAreaElement;

        // true if the parent is of type ProxyFragment
        protected bool _fSubTree;

        // Tells whether then control is Content or Peripheral
        protected bool _fIsContent = true;

        // The IAccessible interface associated with this node
        protected IAccessible _IAccessible;

        #endregion
     }
}
