// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Proxy for UI elements that are native IAccessible 
//              implementations.  NativeMsaaProviderRoot creates
//              instances of this class.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

// Note: When a window with a native IAccessible implementation has child windows those child windows are listed before any 
// IAccessible children due to the way UIA middleware merges the results from multiple providers.  If there is an IAccessible 
// object corresponding to that window then the window should be repositioned according to where the IAccessible occurs. 
// The canonical example is Trident comboboxes that result from the <select> element. There is a mechanism in UIA to do 
// this reparenting and so we need to use that mechanism. One gotcha, though, is that the mechanism only works for client-side 
// providers and there is bug #967897 entered against BrendanM on that.

namespace MS.Internal.AutomationProxies
{
    // Class that implements the UIAutomation provider-side interfaces for a native IAccessible implementation.
    // inherits from MarshalByRefObject so we can pass a reference to this object across process boundaries
    // when it is used in a server-side provider.
    internal class MsaaNativeProvider : MarshalByRefObject,
        IRawElementProviderFragmentRoot,
        IRawElementProviderAdviseEvents,
        IInvokeProvider,
        IToggleProvider,
        ISelectionProvider,
        ISelectionItemProvider,
        IValueProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // primary ctor used to create all providers for IAccessible objects.
        // hwnd is the window this object belongs to.
        // root is a provider for the OBJID_CLIENT of the window. may be null if it is not yet known.
        // don't call this constructor directly -- call a Create or Wrap function below.
        protected MsaaNativeProvider(Accessible acc, IntPtr hwnd, MsaaNativeProvider parent, MsaaNativeProvider knownRoot, RootStatus isRoot)
        {
            Debug.Assert(acc != null, "acc");
            Debug.Assert(hwnd != IntPtr.Zero);

            _acc = acc;
            _hwnd = hwnd;

            _parent = parent; // can be null if not known.
            _knownRoot = knownRoot; // can be null if not known.
            _isRoot = isRoot; // can be RootStatus.Unknown
            // _controlType defaults to null. computed on demand.
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // the creation of any msaa-based provider funnels through this function.
        // it creates an object of the base or correct derived class for the specified IAccessible object.
        private static MsaaNativeProvider Wrap(Accessible acc, IntPtr hwnd, MsaaNativeProvider parent, MsaaNativeProvider knownRoot, RootStatus isRoot)
        {
            // if acc is null then return null.
            if (acc == null)
                return null;

            // check that parent is actually our parent - sometimes hit-test and navigation skip layers, so we
            // may need to reconstruct the parent chain to account for skipped-over ancestors: keep climbing
            // upwards till we reach the 'parent' that was passed in...
            MsaaNativeProvider parentChain = parent;
            if (parent != null)
            {
                ArrayList actualParentChain = null;
                Accessible scan = acc.Parent;
                while (scan != null)
                {
                    if (Accessible.Compare(scan, parent._acc))
                        break; // found actual parent
                    // found intermediate ancestor - add to list...
                    if (actualParentChain == null)
                        actualParentChain = new ArrayList();
                    actualParentChain.Add(scan);
                    scan = scan.Parent;
                }

                if (actualParentChain != null)
                {
                    // if we found intermediate ancestors, process them top-down, creating
                    // MsaaNativeProviders for each in turn, using the bottom-most one as
                    // our own actual parent...
                    for (int i = actualParentChain.Count - 1; i >= 0; i--)
                    {
                        Accessible ancestor = (Accessible)actualParentChain[i];
                        parentChain = new MsaaNativeProvider(ancestor, hwnd, parentChain, knownRoot, isRoot);
                    }
                }
            }

            return new MsaaNativeProvider(acc, hwnd, parentChain, knownRoot, isRoot);
        }

        // wraps another Accessible object from the same window that we jumped to directly somehow.
        // we don't know who the parent is or whether it is root.
        internal MsaaNativeProvider Wrap(Accessible acc)
        {
            return Wrap(acc, _hwnd, null /*unknown parent*/, _knownRoot, RootStatus.Unknown);
        }

        // This is called by UIA's proxy manager and our own MSAAEventDispatcher when a provider is 
        // needed based on a window handle, object and child ids.
        // It returns an IRawElementProviderSimple implementation for a native IAccessible object
        // or null if the hwnd doesn't natively implement IAccessible.
        internal static IRawElementProviderSimple Create (IntPtr hwnd, int idChild, int idObject)
        {
#if DEBUG
//            // uncomment this if you want to prevent unwanted interactions with the debugger in Whidbey.
//            if (string.Compare(hwnd.ProcessName, "devenv.exe", StringComparison.OrdinalIgnoreCase) == 0)
//            {
//                return null;
//            }
#endif

            // check if the hwnd is valid and it isn't one with a known bad MSAA implementation.
            if (!UnsafeNativeMethods.IsWindow(hwnd) || IsKnownBadWindow(hwnd))
                return null;

            // This proxy is aimed at picking up custom IAccessibles to fill the gaps left by other UIA proxies.
            // Winforms, however, implements IAccessible on *all* of its HWNDs, even those that do not add
            // any interesting IAccessible information - for the most part, it is just reexposing the underlying
            // OLEACC proxies.
            //
            // However, there are some winforms controls - eg datagrid - that do have new IAccessible impls, and
            // we *do* want to proxy those, so we can't just flat-out ignore Winforms. Solution here is to ignore
            // winforms that are responding to WM_GETOBJECT/OBJID_QUERYCLASSNAMEIDX, since those are just wrapped
            // comctls. This should leave any custom winforms controls - including those that are simply derived from
            // Control
            bool isWinForms = false;
            if (WindowsFormsHelper.IsWindowsFormsControl(hwnd))
            {
                const int OBJID_QUERYCLASSNAMEIDX = unchecked(unchecked((int)0xFFFFFFF4));

                // Call ProxySendMessage ignoring the timeout
                int index = Misc.ProxySendMessageInt(hwnd, NativeMethods.WM_GETOBJECT, IntPtr.Zero, (IntPtr)OBJID_QUERYCLASSNAMEIDX, true);
                if (index != 0)
                {
                    return null;
                }

                isWinForms = true;
            }

            // try to instantiate the accessible object by sending the window a WM_GETOBJECT message.
            // if it fails in any of the expected ways it will throw an ElementNotAvailable exception
            // and we'll return null.

            Accessible acc = Accessible.CreateNativeFromEvent(hwnd, idObject, idChild);
            if (acc == null)
                return null;

            if (isWinForms)
            {
                // If this is a winforms app, screen out Client and Window roles - this is because all winforms
                // controls get IAccessible impls - but most just call through to OLEACC's proxies. If we get here
                // at all, then chances are we are not a user/common control (because we'll have picked up a UIA
                // proxy first), so we're likely some Control-derived class - which could be interesting, or could
                // be just a boring winforms HWND. Filtering out client/window roles means we'll only talk to WinForms
                // controls that have at least set a meaningul (non-default) role.
                AccessibleRole role = acc.Role;
                if (role == AccessibleRole.Client || role == AccessibleRole.Window)
                    return null;
            }

            MsaaNativeProvider provider;
            if (idObject == NativeMethods.OBJID_CLIENT && idChild == NativeMethods.CHILD_SELF)
            {
                // creating a root provider
                provider = Wrap(acc, hwnd, null/*no parent*/, null/*root is self*/, RootStatus.Root);
                provider._knownRoot = provider;
            }
            else
            {
                // creating a provider that is not OBJID_CLIENT/CHILD_SELF. pretty much everything is unknown except the 
                // object itself. 
                provider = Wrap(acc, hwnd, null/*parent unknown*/, null/*root unknown*/, RootStatus.Unknown);
            }

            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} Created. objid={1}.", provider, idObject));

            return provider;
        }

        #endregion Internal Methods

 
        //------------------------------------------------------
        //
        //  Interface IRawElementProviderFragmentRoot
        //
        //------------------------------------------------------

        #region IRawElementProviderFragmentRoot

        IRawElementProviderFragment IRawElementProviderFragmentRoot.ElementProviderFromPoint( double x, double y )
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragmentRoot.ElementProviderFromPoint ({1},{2})", this, x, y));

            // we assume that UIA has gotten us to the nearest enclosing hwnd.
            // otherwise we would have to check whether the coordinates were inside one of
            // our child windows first.
            Debug.Assert(_hwnd == UnsafeNativeMethods.WindowFromPhysicalPoint((int)x, (int)y));

            // this is essentially the same implementation as AccessibleObjectFromPoint
            return DescendantFromPoint((int)x, (int)y, false);
        }

        IRawElementProviderFragment IRawElementProviderFragmentRoot.GetFocus()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragmentRoot.GetFocus", this));

            // We need to verify that this works correctly in the case that the element is in a child window.


            // It's unsure how reliable get_accFocus is for 'progressive drilling' -
            // It might only work on the container parent of the item that has focus
            // eg if a listitem has focus, then getFocus on the listbox might work; but getFocus on the
            // parent dialog might not return the listbox within it.
            //
            // However, I don't think this may be a problem; since native impls don't span hwnds.
            //
            // one last resort, however, may be for UIA or this to track focus events, since those
            // seem to be the only reliable way of determining focus with IAccessible (even GetGUIThreadInfo
            // doesn't work for custom popup menus), and then this could check if the last focused event
            // corresponded to something it 'owns', and if so, return it. Ick.

            Accessible accFocused = _acc.GetFocus();
            if (accFocused == _acc) // preserve identity when object itself has focus...
                return this;
            return Wrap(_acc.GetFocus());
        }

        #endregion IRawElementProviderFragmentRoot

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderAdviseEvents
        //
        //------------------------------------------------------

        #region IRawElementProviderAdviseEvents Members

        void IRawElementProviderAdviseEvents.AdviseEventAdded(int eventIdAsInt, int[] propertiesAsInts)
        {
            AutomationEvent eventId = AutomationEvent.LookupById(eventIdAsInt);
            AutomationProperty [] properties = null;
            if (propertiesAsInts != null)
            {
                properties = new AutomationProperty[propertiesAsInts.Length];
                for (int i = 0; i < propertiesAsInts.Length; i++)
                {
                    properties[i] = AutomationProperty.LookupById(propertiesAsInts[i]);
                }
            }


            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderAdviseEvents.AdviseEventAdded {1} {2}", this, eventId, properties));
            MSAAEventDispatcher.Dispatcher.AdviseEventAdded(_hwnd, eventId, properties);
        }

        void IRawElementProviderAdviseEvents.AdviseEventRemoved(int eventIdAsInt, int[] propertiesAsInts)
        {
            AutomationEvent eventId = AutomationEvent.LookupById(eventIdAsInt);
            AutomationProperty [] properties = null;
            if (propertiesAsInts != null)
            {
                properties = new AutomationProperty[propertiesAsInts.Length];
                for (int i = 0; i < propertiesAsInts.Length; i++)
                {
                    properties[i] = AutomationProperty.LookupById(propertiesAsInts[i]);
                }
            }
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderAdviseEvents.AdviseEventRemoved {1} {2}", this, eventId, properties));

            // review: would it be better to fail silently in this case rather than throw an exception?
            MSAAEventDispatcher.Dispatcher.AdviseEventRemoved(_hwnd, eventId, properties);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderFragment
        //
        //------------------------------------------------------

        #region IRawElementProviderFragment

        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.Navigate {1}", this, direction));

            MsaaNativeProvider rval = null;
            
            switch (direction)
            {
                case NavigateDirection.NextSibling:
                    rval = GetNextSibling();
                    break;

                case NavigateDirection.PreviousSibling:
                    rval = GetPreviousSibling();
                    break;

                case NavigateDirection.FirstChild:
                    rval = GetFirstChild();
                    break;

                case NavigateDirection.LastChild:
                    rval = GetLastChild();
                    break;

                case NavigateDirection.Parent:
                    rval = IsRoot ? null : Parent;
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            return rval;
        }

        int [] IRawElementProviderFragment.GetRuntimeId()
        {
            if(_isMCE == TristateBool.Untested)
            {
                // Workaround for Media Center - their IAccessible impl is very
                // deep (11 items), so GetRuntimeId ends up being really slow; this
                // causes huge perf issues with Narrator (30 seconds to track focus),
                // making it unusable. This workaround brings perf back to near-usable
                // levels (a few seconds). The downside is that narrator re-announces
                // parent windows on each focus change - but that's a lot better than
                // not saying anything at all.
                // Workaround here is to use a 'fake' runtimeID (this.GetHashCode())
                // instead of taking the perf hit of attempting to figure out a more
                // genuine one. One consequence of this is that comparisons between
                // two different AutomationElements representing the same underlying
                // UI in MCE may incorrectly compare as FALSE.
                string className = Misc.GetClassName(_hwnd);
                if(String.Compare(className, "eHome Render Window", StringComparison.OrdinalIgnoreCase) == 0)
                    _isMCE = TristateBool.TestedTrue;
                else
                    _isMCE = TristateBool.TestedFalse;
            }


            if(_isMCE == TristateBool.TestedTrue)
            {
                return new int[] { AutomationInteropProvider.AppendRuntimeId, this.GetHashCode() };
            }

            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.GetRuntimeId", this));

            int[] rval = null;
            
            // if we are the root the runtime id is null so the host hwnd provider will provide it.
            // otherwise...
            if (!IsRoot)
            {
                // get our parent's runtime ID
                int[] parentId = ((IRawElementProviderFragment)Parent).GetRuntimeId();

                if (parentId != null)
                {
                    rval = new int[parentId.Length + 1];
                    parentId.CopyTo(rval, 0);
                }
                else
                {
                    // we're a child of the root so start the runtime ID off with the special 'append' token
                    rval = new int[2];
                    rval[0] = AutomationInteropProvider.AppendRuntimeId;
                }

                // append our ID on the end
                rval[rval.Length - 1] = _acc.AccessibleChildrenIndex(Parent._acc);
            }

            return rval;
        }

        Rect IRawElementProviderFragment.BoundingRectangle
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.BoundingRectangle", this));

                // the framework takes care of the root BoundingRectangle
                if (IsRoot)
                {
                    return Rect.Empty;
                }

                bool isOffscreen = false;
                object isOffscreenProperty = GetPropertyValue(AutomationElement.IsOffscreenProperty);
                if (isOffscreenProperty is bool)
                {
                    isOffscreen = (bool)isOffscreenProperty;
                }

                if (isOffscreen)
                {
                    return Rect.Empty;
                }

                Rect rc = _acc.Location;

                if (rc.Width <= 0 || rc.Height <= 0)
                {
                    return Rect.Empty;
                }

                return rc;
            }
        }

        IRawElementProviderSimple [] IRawElementProviderFragment.GetEmbeddedFragmentRoots()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.GetEmbeddedFragmentRoots", this));

            // Custom UI hosted in native IAccessible?  Shouldn't be an issue; will wait for scenario.
            return null;
        }

        void IRawElementProviderFragment.SetFocus()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.SetFocus", this));

            // We need to verify the framework hasn't already done this (just doing what other proxies do)
            // We need to consider what happens when SetFocus is called when a menu is up
            Misc.SetFocus(_hwnd);
            _acc.SetFocus();
        }

        IRawElementProviderFragmentRoot IRawElementProviderFragment.FragmentRoot
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderFragment.FragmentRoot", this));

                return KnownRoot;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderSimple
        //
        //------------------------------------------------------
 
        #region Interface IRawElementProviderSimple

        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get
            {
                return ProviderOptions.ClientSideProvider;
            }
        }

        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            AutomationPattern pattern = AutomationPattern.LookupById(patternId);
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderSimple.GetPatternProvider {1}", this, pattern));

            // call overridable method
            return GetPatternProvider(pattern);
        }

        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            AutomationProperty idProp = AutomationProperty.LookupById(propertyId);
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderSimple.GetPropertyValue {1}", this, idProp));

            // call overridable method
            return GetPropertyValue(idProp);
        }

        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IRawElementProviderSimple.HostRawElementProvider", this));

                // we return a host hwnd provider if we are the root.
                // otherwise we return null.
                return IsRoot ? AutomationInteropProvider.HostProviderFromHandle(_hwnd) : null;
            }
        }

        #endregion Interface IRawElementProviderSimple


        //------------------------------------------------------
        //
        //  Pattern Implementations
        //
        //------------------------------------------------------

        #region IInvokeProvider 

        // IInvokeProvider

        void IInvokeProvider.Invoke()
        {
            CallDoDefaultAction();
        }
        #endregion IInvokeProvider
        

        #region IToggleProvider 

        void IToggleProvider.Toggle()
        {
            CallDoDefaultAction();
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IToggleProvider.ToggleState", this));

                // review: this makes two calls to IAccessible.get_accState when it only needs to make one.
                if (_acc.IsIndeterminate)
                {
                    return ToggleState.Indeterminate;
                }

                return _acc.IsChecked ? ToggleState.On : ToggleState.Off;
            }
        }

        #endregion IToggleProvider

        #region ISelectionProvider

        // ISelectionProvider

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionProvider.Selection", this));

            Accessible[] accessibles = _acc.GetSelection();
            if (accessibles == null)
                return new IRawElementProviderSimple[] {};

            IRawElementProviderSimple [] rawEPS= new IRawElementProviderSimple[accessibles.Length];
            for (int i=0;i<accessibles.Length;i++)
            {
                rawEPS[i] = Wrap(accessibles[i], _hwnd, this/*parent*/, _knownRoot, RootStatus.NotRoot);
            }
            return rawEPS;
        }

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionProvider.CanSelectMultiple", this));

                return _acc.IsMultiSelectable;
            }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionProvider.IsSelectionRequired", this));

                // For Win32, it's mostly safe to assume that if its a multi-select, then you can deselect everything
                // ...or, put another way, if it's single select, then at least one selection is required.
                return !_acc.IsMultiSelectable;
            }
        }

        #endregion ISelectionProvider


        #region ISelectionItemProvider

        void ISelectionItemProvider.Select()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionItemProvider.Select", this));

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            Misc.SetFocus(_hwnd);
            _acc.SelectTakeFocusTakeSelection();
        }

        void ISelectionItemProvider.AddToSelection()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionItemProvider.AddToSelection", this));

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            Misc.SetFocus(_hwnd);
            _acc.SelectTakeFocusAddToSelection();
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionItemProvider.RemoveFromSelection", this));

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            Misc.SetFocus(_hwnd);
            _acc.SelectTakeFocusRemoveFromSelection();
        }

        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionItemProvider.IsSelected", this));

                // if it is a radio button then the item is selected if it has the "checked" state.
                // otherwise it is selected if it has the "selected" state.
                AccessibleState state = _acc.State;
                return (ControlType.RadioButton == ControlType) ?
                    (state & AccessibleState.Checked) == AccessibleState.Checked :
                    (state & AccessibleState.Selected) == AccessibleState.Selected;
            }
        }

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} ISelectionItemProvider.SelectionContainer", this));

                return IsRoot ? null : Parent;
            }
        }
        #endregion ISelectionItemProvider

        #region IValueProvider 

        void IValueProvider.SetValue( string val )
        {
            //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IValueProvider.SetValue", this));

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // call overridable method
            SetValue(val);
        }


        string IValueProvider.Value
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IValueProvider.Value", this));

                // call overridable method
                return GetValue();
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                //Debug.WriteLine.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0} IValueProvider.IsReadOnly", this));

                return _acc.IsReadOnly;
            }
        }

        #endregion IValueProvider

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal MsaaNativeProvider GetNextSibling()
        {
            MsaaNativeProvider rval = null;

            // don't allow navigation outside of the root
            if (!IsRoot)
            {
                // get our next sibling and keep looking until we find one that is exposed or run out of siblings
                Accessible siblingAcc;
                for (siblingAcc = _acc.NextSibling(Parent._acc);
                     siblingAcc != null && !siblingAcc.IsExposedToUIA;
                     siblingAcc = siblingAcc.NextSibling(Parent._acc))
                    ;

                // If we found a sibling that is exposed then wrap it in a provider.
                // IAccessibles with bad navigation may cause us to navigate outside of
                // this fragment's hwnd.  Check that here and return null for that case.
                if (siblingAcc != null && siblingAcc.InSameHwnd(_hwnd))
                {
                    rval = Wrap(siblingAcc, _hwnd, _parent, _knownRoot, RootStatus.NotRoot);
                }
            }

            return rval;
        }

        internal MsaaNativeProvider GetPreviousSibling()
        {
            MsaaNativeProvider rval = null;

            // don't allow navigation outside of the root
            if (!IsRoot)
            {
                // get our previous sibling and keep looking until we find one that is exposed or run out of siblings
                Accessible siblingAcc;
                for (siblingAcc = _acc.PreviousSibling(Parent._acc);
                     siblingAcc != null && !siblingAcc.IsExposedToUIA;
                     siblingAcc = siblingAcc.PreviousSibling(Parent._acc))
                    ;

                // If we found a sibling that is exposed then wrap it in a provider.
                // IAccessibles with bad navigation may cause us to navigate outside of
                // this fragment's hwnd.  Check that here and return null for that case.
                if (siblingAcc != null && siblingAcc.InSameHwnd(_hwnd))
                {
                    rval = Wrap(siblingAcc, _hwnd, _parent, _knownRoot, RootStatus.NotRoot);
                }
            }

            return rval;
        }

        internal MsaaNativeProvider GetFirstChild()
        {
            MsaaNativeProvider rval = null;

            // get our first child. examine it and its siblings until we find one that is exposed or run out of children
            Accessible childAcc;
            for (childAcc = _acc.FirstChild;
                 childAcc != null && !childAcc.IsExposedToUIA;
                 childAcc = childAcc.NextSibling(_acc))
                ;

            // If we found a child that is exposed then wrap it in a provider.
            // IAccessibles with bad navigation may cause us to navigate outside of
            // this fragment's hwnd.  Check that here and return null for that case.
            if (childAcc != null && childAcc.InSameHwnd(_hwnd))
            {
                rval = Wrap(childAcc, _hwnd, this, _knownRoot, RootStatus.NotRoot);
            }

            return rval;
        }

        internal MsaaNativeProvider GetLastChild()
        {
            MsaaNativeProvider rval = null;

            // get our last child. examine it and its siblings until we find one that is exposed or run out of children
            Accessible childAcc;
            for (childAcc = _acc.LastChild;
                 childAcc != null && !childAcc.IsExposedToUIA;
                 childAcc = childAcc.PreviousSibling(_acc))
                ;

            // If we found a child that is exposed then wrap it in a provider.
            // IAccessibles with bad navigation may cause us to navigate outside of
            // this fragment's hwnd.  Check that here and return null for that case.
            if (childAcc != null && childAcc.InSameHwnd(_hwnd))
            {
                rval = Wrap(childAcc, _hwnd, this, _knownRoot, RootStatus.NotRoot);
            }

            return rval;
        }

        // returns true iff the pattern is supported.
        // used by GetPatternProvider and MSAAEventDispatcher
        internal bool IsPatternSupported(AutomationPattern pattern)
        {
            // look up the control type in the patterns map and check whether the pattern is in the list.
            // note: we could change _patternsMap to a hash table for better search performance.
            // in the future we could consider some additional criteria besides control type but for now it is sufficient.
            // (e.g. Toggle pattern for checkable menu items, etc.)
            bool rval = false;
            ControlType ctrlType = ControlType;
            foreach (CtrlTypePatterns entry in _patternsMap)
            {
                if (entry._ctrlType == ctrlType)
                {
                    // if the pattern is in the list of patterns for this control type return true.
                    rval = (Array.IndexOf(entry._patterns, pattern)>=0);
                    break;
                }
            }


            if (rval == false)
            {
                // If it's not a recognized role, but does have a default action, support
                // Invoke as a fallback...
                if(pattern == InvokePattern.Pattern && !String.IsNullOrEmpty(_acc.DefaultAction))
                {
                    rval = true;
                }
                // Similarly for Value pattern
                else if(pattern == ValuePattern.Pattern && !String.IsNullOrEmpty(_acc.Value))
                {
                    rval = true;
                }
            }

            return rval;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // overridable implementation of IRawElementProviderSimple.GetPatternProvider
        protected virtual object GetPatternProvider(AutomationPattern pattern)
        {
            // If it's a known MSAA Role, supply the appropriate control pattern...
            if(IsPatternSupported(pattern))
                return this;

            return null;
        }

        // overridable implementation of IRawElementProviderSimple.GetPropertyValue
        protected virtual object GetPropertyValue(AutomationProperty idProp)
        {
            // The following UIA properties need support: AcceleratorKeyProperty, AccessKeyProperty, AutomationIdProperty, 
            // HasKeyboardFocusProperty, IsContentElementProperty, IsControlElementProperty, IsKeyboardFocusableProperty, 
            // IsPasswordProperty, IsReadOnlyProperty, NativeObjectModelAccessProperty, SiblingIdProperty, TabIndexProperty?, 
            // TabStopProperty?. 
            // NOTE - IsKeyboardFocusableProperty and HasKeyboardFocusProperty properties
            //        may seem to map but they actually don't map well.  These properties
            //        only return true if their container Window is the active Window. 

            // just supports a few properties
            if (idProp == AutomationElement.AccessKeyProperty)
            {
                string value = _acc.KeyboardShortcut;
                return string.IsNullOrEmpty(value) ? null : value;
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                string value = _acc.Name;
                return string.IsNullOrEmpty(value) ? null : value;
            }
            else if (idProp == AutomationElement.ControlTypeProperty)
            {
                ControlType ctype = ControlType;
                if( ctype != null )
                    return ctype.Id;
                else
                    return null;
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                return _acc.IsEnabled;
            }
            else if (idProp == AutomationElement.HelpTextProperty)
            {
                string value = _acc.HelpText;
                return string.IsNullOrEmpty(value) ? null : value;
            }
            else if (idProp == AutomationElement.ProcessIdProperty)
            {
                uint pid;
                Misc.GetWindowThreadProcessId(_hwnd, out pid);
                return unchecked((int)pid);
            }
            else if (idProp == AutomationElement.BoundingRectangleProperty)
            {
                // note: UIAutomation will call IRawElementProviderFragment.BoundingRectangle 
                // directly but we call this to implement the AutomationPropertyChangedEvent
                // when we receive the EVENT_OBJECT_LOCATIONCHANGE winevent.
                return ((IRawElementProviderFragment)this).BoundingRectangle;
            }
            else if (idProp == ValuePattern.ValueProperty)
            {
                // note: UIAutomation will call IValueProvider.Value
                // directly but we call this to implement the AutomationPropertyChangedEvent
                // when we receive the EVENT_OBJECT_VALUECHANGE winevent.
                // That code will only call this if the object supports the value pattern so
                // we don't need to check here.
                return ((IValueProvider)this).Value;
            }
            else if (idProp == AutomationElement.IsPasswordProperty)
            {
                return _acc.IsPassword;
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                return _acc.IsFocused;
            }
            else if (idProp == AutomationElement.IsOffscreenProperty)
            {
                return _acc.IsOffScreen;
            }
            return null;
        }

        // overridable method used by value pattern to retrieve the value.
        protected virtual string GetValue()
        {
            // if this is a password edit control then throw an exception
            if (_acc.IsPassword)
            {
                throw new UnauthorizedAccessException();
            }

            return _acc.Value;
        }

        // overridable method used by value pattern to set the value.
        protected virtual void SetValue(string val)
        {
            _acc.Value = val;
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // sets the best match for ControlType for this element
        private ControlType ControlType
        {
            get
            {
                // cache the value the first time
                if (_controlType == null)
                {
                    // control type is primarily dependent upon role
                    AccessibleRole role = _acc.Role;
                    if (role == AccessibleRole.Text)
                    {
                        // ordinary text in a web page, for example, is marked ROLE_SYSTEM_TEXT and has the STATE_SYSTEM_READONLY status flag set.
                        _controlType = _acc.IsReadOnly ? ControlType.Text : _controlType = ControlType.Edit;
                    }
                    else
                    {
                        // look in the table to see what the control type should be.
                        foreach (RoleCtrlType entry in _roleCtrlTypeMap)
                        {
                            if (entry._role == role)
                            {
                                _controlType = entry._ctrlType;
                                break;
                            }
                        }
                    }
                    // note: _controlType can stay null. in that case we'll try to figure it out each time.
                    // if this is a performance problem we can add a separate boolean flag to indicate whether
                    // we have computed the control type.
                }

                // return the cached value
                return _controlType;
            }
        }

        private Accessible GetParent()
        {
            // this should never be called on a root.
            Debug.Assert(!IsRoot);

            // we should never step up out of a "window". we should hit the root first.
            if (_acc.Role == AccessibleRole.Window)
            {
                throw new ElementNotAvailableException();
            }

            Accessible parentAccessible = _acc.Parent;

            // if we get a null parent (Accessible.Parent will return null for IAccessible's
            // when we detect bad navigation) then we have no idea where we are. bail.
            if (parentAccessible == null)
            {
                throw new ElementNotAvailableException();
            }

            return parentAccessible;
        }


        // The following classes are known to have bad IAccessible implementation (eg.
        // overly complex structure or too many problems for the proxy to deal with).
        // Note that while similar "bad lists" are used by UIACore and the proxy manager,
        // those are concerned with impls what don't check lParam and assume OBJID_CLIENT -
        // That's not an issue here, since we'll be getting the OBJID_CLIENT anyhow.
        private static string[] BadImplClassnames = new string[]
        {
            "TrayClockWClass", // Doesn't check lParam
            "CiceroUIWndFrame", // Doesn't check lParam, has tree inconsistencies
            "VsTextEditPane", // VS Text area has one object per character - far too "noisy" to deal with, kills perf
        };

        private static bool IsKnownBadWindow(IntPtr hwnd)
        {
            string className = Misc.GetClassName(hwnd);

            foreach (string str in BadImplClassnames)
            {
                if (String.Compare(className, str, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        private MsaaNativeProvider KnownRoot
        {
            get
            {
                // compute the answer on the first call and cache it.
                if (_knownRoot == null)
                {
                    Debug.Assert(_hwnd != IntPtr.Zero);

                    // ask the window for its OBJID_CLIENT object
                    _knownRoot = (MsaaNativeProvider)Create(_hwnd, NativeMethods.CHILD_SELF, NativeMethods.OBJID_CLIENT);
                    if (_knownRoot == null)
                    {
                        // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                        // When failing to create the element, the correct this to do is to throw an ElementNotAvailableException.
#pragma warning suppress 6503
                        throw new ElementNotAvailableException();
                    }
                }

                return _knownRoot;
            }
        }

        // Ask our Accessible object if we are at the root
        private bool IsRoot
        {
            get
            {
                // compute the answer on the first call and cache it.
                if (_isRoot == RootStatus.Unknown)
                {
                    // there's no way to check identity between IAccessibles so we heuristically
                    // check if all the properties are the same as a known root object. 
                    
                    // As a backup we check if the  role is "Window". This helps in the case of a bad parent 
                    // implementation which skips over the OBJID_CLIENT. 
                    // (E.g. the pagetablist in Word 2003 Font dialog)

                    _isRoot = Accessible.Compare(_acc, KnownRoot._acc) || _acc.Role == AccessibleRole.Window ? RootStatus.Root : RootStatus.NotRoot;

//                    // ask the accessible object if it is the OBJID_CLIENT object.
//                    // (determined heuristically.) if it is then we are root.
//                    _isRoot = _acc.IsClientObject() ? RootStatus.Root : RootStatus.NotRoot;
//
//                    // sanity check for debugging purposes only!
//                    if (_isRoot == RootStatus.Root)
//                    {
//                        // see if look the same as an object that we know is root. 
//                        // (we can't compare object identities because 
//                        // different IAccessible objects can represent the same underlying 
//                        // object therefore different MsaaNativeProvider objects can both 
//                        // represent the root.)
//                        Debug.Assert(Accessible.Compare(_acc, KnownRoot._acc));
//                    }
                }

                return _isRoot == RootStatus.Root;
            }
        }

        private MsaaNativeProvider Parent
        {
            get
            {
                // we cache a copy of our parent because there are a number of bad IAccessible.get_accParent implementations.
                // some return null (e.g. windowless windows media player embedded in trident)
                // and some return their grandparent instead of their actual parent (e.g. pagetablist in Word 2003 Font dialog).
                // if we navigate down or over to another element we can save its parent in the cache thereby avoiding
                // the use of the problematic IAccessible.get_accParent. This won't save us in instances where we jump
                // directly to an element as the result of a WinEvent or of IAccessible.get_accFocus.

                if (_parent == null)
                {
                    _parent = Wrap(GetParent(), _hwnd, null/*grandparent unknown*/, _knownRoot, RootStatus.Unknown);
                }

                return _parent;
            }
        }

        // recursively search our children for the accessible object that is at the point.
        private MsaaNativeProvider DescendantFromPoint(int x, int y, bool nullMeansThis)
        {
            // get the child of this object at the point
            // (Some IAccessible impls actually return a descendant instead of an immediate
            // child - Wrap() below takes care of this.)
            Accessible childAcc = _acc.HitTest(x, y);

            // there are three possible results: null, 'this', or a child.
            MsaaNativeProvider rval;
            if (childAcc == null)
            {
                rval = nullMeansThis ? this : null;
            }
            else if (childAcc == _acc)
            {
                rval = this;
            }
            else
            {
                // the coordinates are in one of our children.
                MsaaNativeProvider child = Wrap(childAcc, _hwnd, this, _knownRoot, RootStatus.NotRoot);

                if (childAcc.ChildId != NativeMethods.CHILD_SELF)
                {
                    // child is a simple object so it is the object at point
                    rval = child;
                }
                else
                {
                    // child is full-fledged IAccessible. recurse in case it has children.
                    rval = child.DescendantFromPoint(x, y, true);
                }
            }
            return rval;
        }

        // Used by Toggle and Invoke...
        void CallDoDefaultAction()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // If Toggle is ever supported for menu items then SetFocus may not be 
            // appropriate here as that may have the side-effect of closing the menu
            Misc.SetFocus(_hwnd);

            try
            {
                _acc.DoDefaultAction();
            }
            catch (Exception e)
            {
                if (Misc.IsCriticalException(e))
                {
                    throw;
                }

                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed), e);
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        //private delegate AutomationPattern PatternChecker(Accessible acc);

        // a struct holding an entry for the table below
        struct RoleCtrlType
        {
            public RoleCtrlType(AccessibleRole role, ControlType ctrlType)
            {
                _role = role;
                _ctrlType = ctrlType;
            }

            public AccessibleRole _role;               // MSAA role
            public ControlType _ctrlType;              // UIAutomation ControlType
        };

        // this table maps MSAA roles into UIA control types.
        private static RoleCtrlType[] _roleCtrlTypeMap =
        {
            // in alphabetical order of AccessibleRole
            new RoleCtrlType(AccessibleRole.Application,    ControlType.Window),
            new RoleCtrlType(AccessibleRole.ButtonDropDown, ControlType.SplitButton),
            new RoleCtrlType(AccessibleRole.ButtonMenu,     ControlType.MenuItem),
            new RoleCtrlType(AccessibleRole.CheckButton,    ControlType.CheckBox),
            new RoleCtrlType(AccessibleRole.ColumnHeader,   ControlType.Header),
            new RoleCtrlType(AccessibleRole.Combobox,       ControlType.ComboBox),
            new RoleCtrlType(AccessibleRole.Document,       ControlType.Document),
            new RoleCtrlType(AccessibleRole.Graphic,        ControlType.Image),
            new RoleCtrlType(AccessibleRole.Link,           ControlType.Hyperlink),
            new RoleCtrlType(AccessibleRole.List,           ControlType.List),
            new RoleCtrlType(AccessibleRole.ListItem,       ControlType.ListItem),
            new RoleCtrlType(AccessibleRole.MenuBar,        ControlType.MenuBar),
            new RoleCtrlType(AccessibleRole.MenuItem,       ControlType.MenuItem),
            new RoleCtrlType(AccessibleRole.MenuPopup,      ControlType.Menu),
            new RoleCtrlType(AccessibleRole.Outline,        ControlType.Tree),
            new RoleCtrlType(AccessibleRole.OutlineItem,    ControlType.TreeItem),
            new RoleCtrlType(AccessibleRole.PageTab,        ControlType.TabItem),
            new RoleCtrlType(AccessibleRole.PageTabList,    ControlType.Tab),
            new RoleCtrlType(AccessibleRole.Pane,           ControlType.Pane),
            new RoleCtrlType(AccessibleRole.ProgressBar,    ControlType.ProgressBar),
            new RoleCtrlType(AccessibleRole.PushButton,     ControlType.Button),
            new RoleCtrlType(AccessibleRole.RadioButton,    ControlType.RadioButton),
            new RoleCtrlType(AccessibleRole.RowHeader,      ControlType.Header),
            new RoleCtrlType(AccessibleRole.ScrollBar,      ControlType.ScrollBar),
            new RoleCtrlType(AccessibleRole.Separator,      ControlType.Separator),
            new RoleCtrlType(AccessibleRole.Slider,         ControlType.Slider),
            new RoleCtrlType(AccessibleRole.SpinButton,     ControlType.Spinner),
            new RoleCtrlType(AccessibleRole.SplitButton,    ControlType.SplitButton),
            new RoleCtrlType(AccessibleRole.StaticText,     ControlType.Text),
            new RoleCtrlType(AccessibleRole.StatusBar,      ControlType.StatusBar),
            new RoleCtrlType(AccessibleRole.Table,          ControlType.Table),
            // AccessibleRole.Text is handled specially in ControlType property.
            new RoleCtrlType(AccessibleRole.TitleBar,       ControlType.TitleBar),
            new RoleCtrlType(AccessibleRole.ToolBar,        ControlType.ToolBar),
            new RoleCtrlType(AccessibleRole.Tooltip,        ControlType.ToolTip),
            new RoleCtrlType(AccessibleRole.Window,         ControlType.Window)
        };

        // a struct holding an entry for the table below
        struct CtrlTypePatterns
        {
            public CtrlTypePatterns(ControlType ctrlType, params AutomationPattern[] patterns)
            {
                _ctrlType = ctrlType;
                _patterns = patterns;
            }

            public ControlType _ctrlType;
            public AutomationPattern[] _patterns;
        }

        // this table maps control types to the patterns that they support.
        private static CtrlTypePatterns[] _patternsMap =
        {
            // in alphabetical order of ControlType
            new CtrlTypePatterns(ControlType.Button, InvokePattern.Pattern),
            new CtrlTypePatterns(ControlType.CheckBox, TogglePattern.Pattern),
            new CtrlTypePatterns(ControlType.ComboBox, ValuePattern.Pattern),
            new CtrlTypePatterns(ControlType.Document, TextPattern.Pattern),
            new CtrlTypePatterns(ControlType.Edit, ValuePattern.Pattern),
            new CtrlTypePatterns(ControlType.Hyperlink, InvokePattern.Pattern),
            new CtrlTypePatterns(ControlType.List, SelectionPattern.Pattern),
            new CtrlTypePatterns(ControlType.ListItem, SelectionItemPattern.Pattern),
            new CtrlTypePatterns(ControlType.MenuItem, InvokePattern.Pattern),
            new CtrlTypePatterns(ControlType.ProgressBar, ValuePattern.Pattern),
            new CtrlTypePatterns(ControlType.RadioButton, SelectionItemPattern.Pattern),
            // ControlType.Slider: it is impossible to tell which of RangeValue or Selection patterns to support so we're not supporting either.
            // ControlType.Spinner: it is impossible to tell which of RangeValue or Selection patterns to support so we're not supporting either.
            new CtrlTypePatterns(ControlType.SplitButton, InvokePattern.Pattern)
        };

        private Accessible _acc; // the IAccessible we are representing. use Accessible to access.

        protected IntPtr _hwnd; // the window we belong to

        protected MsaaNativeProvider _parent; // cached parent. may be null. use Parent property.

        // cached value of a provider that wraps an IAcessible retrieved from the window 
        // using OBJID_CLIENT.
        // use the KnownRoot property to access.
        private MsaaNativeProvider _knownRoot;

        // cached value indicating whether we are at the root of the UIA fragment.
        // we may wrap the root object even if we weren't retrieve directly from the window
        // using OBJID_CLIENT. that can happen if we navigate up the parent chain from one 
        // of the child objects. We can't use object identity to tell if we are the root because
        // different IAccessible objects can represent the same underlying UI element.
        // However we can heuristically determine if we are the root object and this
        // records the result. 
        // use the IsRoot property to access.
        internal enum RootStatus { Root, NotRoot, Unknown };
        private RootStatus _isRoot;

        private ControlType _controlType;   // cached control type; it doesn't change

        private enum TristateBool
        {
            Untested,
            TestedTrue,
            TestedFalse
        };

        private TristateBool _isMCE = TristateBool.Untested;

        #endregion Private Fields
    }
}
