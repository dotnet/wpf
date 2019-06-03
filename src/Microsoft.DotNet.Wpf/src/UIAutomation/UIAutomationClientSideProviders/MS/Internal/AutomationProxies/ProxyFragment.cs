// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Base class for all the Win32 and office Controls.
//
//              The ProxyFragment class is the base class for NODES in the
//              element tree that are not hwnd based in the case of Win32 controls (e.g. ListViewItem)
//
//              The UIAutomation internal provider does not allow for a node to
//              iterate through its children.
//              The ProxyFragment class removes this limitation.
//
//              Class ProxyFragment:
//                  ProxySimple - methods
//                  ElementProviderFromPoint
//                  GetFocus
//
//
//              NOTE: ProxyFragment is responsible for hit-testing on its children, since UIAutomation
//                    will not call ElementProviderFromPoint on the Fragment!
//             
//              Here are the easy steps for drilling down:
//                     UIAutomation will call ProxyHwnd.ElementProviderFromPoint automatically,
//                     you (implementer) should do a hit test on ProxyHwnd's immediate children only.
//                     If hit-test succeeds and element that you got is ProxyFragment
//                     call ProxyFragment.DrillDownIntoFragment(fragment, x, y)
//                     Each ProxyFragment should implement ElementProviderFromPoint; this method will be used by
//                     DrillDownIntoFragment to drill into the hierarchy of any depth.
//                     Note: You can implement all the drilling down "locally" in your ProxyHwnd.ElementProviderFromPoint,
//                           but it is strongly suggested to do it as described above, so we'll have a generic solution that always works
//                    
//
//

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

namespace MS.Internal.AutomationProxies
{
    // Base Class for all the Windows Control that supports navigation.
    // Implements the default behaviors
    class ProxyFragment : ProxySimple, IRawElementProviderFragmentRoot
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal ProxyFragment (IntPtr hwnd, ProxyFragment parent, int item) : base (hwnd, parent, item)
        {}

        #endregion

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxyFragment Methods

        // ------------------------------------------------------
        //
        // Default implementation for ProxyFragment members
        //
        // ------------------------------------------------------
                
        // Next Silbing: assumes none, must be overloaded by a subclass if any
        // The method is called on the parent with a reference to the child.
        // This makes the implementation a lot more clean than the UIAutomation call
        internal virtual ProxySimple GetNextSibling (ProxySimple child)
        {
            return null;
        }

        // Prev Silbing: assumes none, must be overloaded by a subclass if any
        // The method is called on the parent with a reference to the child.
        // This makes the implementation a lot more clean than the UIAutomation call
        internal virtual ProxySimple GetPreviousSibling (ProxySimple child)
        {
            return null;
        }

        // GetFirstChild: assumes none, must be overloaded by a subclass if any
        internal virtual ProxySimple GetFirstChild ()
        {
            return null;
        }

        // GetLastChild: assumes none, must be overloaded by a subclass if any
        internal virtual ProxySimple GetLastChild ()
        {
            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        // In the derived class implement this method to do fragment specific drill down (if needed)
        // Fragment should drill down only among its immediate children.  
        internal virtual ProxySimple ElementProviderFromPoint (int x, int y)
        {
            return this;
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal virtual ProxySimple GetFocus ()
        {
            return this is ProxyHwnd ? this : null;
        }

        #endregion
        

        #region IRawElementProviderFragmentRoot Interface
        // NOTE: This method should only be called (by Proxy or/and UIAutomation) on Proxies of type
        //       ProxyHwnd. Anything else indicates a problem
        IRawElementProviderFragment IRawElementProviderFragmentRoot.ElementProviderFromPoint (double x, double y)
        {
            System.Diagnostics.Debug.Assert(this is ProxyHwnd,  "Invalid method called ElementProviderFromPoint");

            // Do the proper rounding.
            return ElementProviderFromPoint ((int) (x + 0.5), (int) (y + 0.5));
        }

        IRawElementProviderFragment IRawElementProviderFragmentRoot.GetFocus ()
        {
            return GetFocus ();
        }

        #endregion

        #region IRawElementProviderFragment Interface

        // Request to return the element in the specified direction
        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            switch (direction)
            {
                case NavigateDirection.NextSibling :
                    {
                        // NOTE: Do not use GetParent(), call _parent explicitly
                        return _fSubTree ? _parent.GetNextSibling (this) : null;
                    }

                case NavigateDirection.PreviousSibling :
                    {
                        // NOTE: Do not use GetParent(), call _parent explicitly
                        return _fSubTree ? _parent.GetPreviousSibling (this) : null;
                    }

                case NavigateDirection.FirstChild :
                    {
                        return GetFirstChild ();
                    }

                case NavigateDirection.LastChild :
                    {
                        return GetLastChild ();
                    }

                case NavigateDirection.Parent :
                    {
                        return GetParent ();
                    }

                default :
                    {
                        return null;
                    }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Recursively Raise an Event for all the sub elements
        override internal void RecursiveRaiseEvents (object idProp, AutomationPropertyChangedEventArgs e)
        {
            AutomationInteropProvider.RaiseAutomationPropertyChangedEvent (this, e);
            for (ProxySimple el = GetFirstChild (); el != null; el = this.GetNextSibling (el))
            {
                el.RecursiveRaiseEvents (idProp, e);
            }
        }
    
        #endregion
        
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // This method will return the leaf element that lives in ProxyFragment at Point(x,y)
        static internal ProxySimple DrillDownFragment(ProxyFragment fragment, int x, int y)
        {
            System.Diagnostics.Debug.Assert(fragment != null, "DrillDownFragment: starting point is null");

            // drill down
            ProxySimple fromPoint = fragment.ElementProviderFromPoint(x, y);

            System.Diagnostics.Debug.Assert(fromPoint != null, @"DrillDownFragment: calling ElementProviderFromPoint on Fragment should not return null");

            // Check if we got back a new fragment
            // do this check before trying to cast to ProxyFragment
            if (fragment == fromPoint || Misc.Compare(fragment, fromPoint))
            {
                // Point was on the fragment
                // but not on any element that lives inside of the fragment
                return fragment;
            }

            fragment = fromPoint as ProxyFragment;
            if (fragment == null)
            {
                // we got back a simple element                
                return fromPoint;
            }

            // Got a new fragment, continue drilling
            return DrillDownFragment(fragment, x, y);
        }

        #endregion
                
    }
}
