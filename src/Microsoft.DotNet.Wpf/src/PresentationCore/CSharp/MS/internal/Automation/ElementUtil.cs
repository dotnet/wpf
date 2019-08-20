// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 
//
//

using System;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using System.Security; 
using MS.Win32;
using MS.Internal.Media;
using System.Runtime.InteropServices;
using System.Globalization;

using MS.Internal.PresentationCore;                        // SafeSecurityHelper

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Automation
{
    // static class providing utility information for working with WCP elements
    internal class ElementUtil
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // static class, so use private ctor
        private ElementUtil() { }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static Visual GetParent( Visual el )
        {
            return VisualTreeHelper.GetParent(el) as Visual;
        }

        internal static Visual GetFirstChild( Visual el )
        {
            if (el == null)
            {
                return null;
            }

            return FindVisibleSibling ( el, 0, true );
        }

        internal static Visual GetLastChild( Visual el )
        {
            if (el == null)
            {
                return null;
            }

            return FindVisibleSibling ( el, el.InternalVisualChildrenCount - 1, false );
        }

        // Warning: Method is O(N). See FindVisibleSibling function for more information.
        internal static Visual GetNextSibling( Visual el )
        {
            // To get next/previous sibling, have to find out where we
            // are in our parent's children collection (ie. our siblings)
            Visual parent = VisualTreeHelper.GetParent(el) as Visual;
            // If parent is null, we're at root, so have no siblings
            if (parent == null)
            {
                return null;
            }
            return FindVisibleSibling ( parent, el, true /* Next */);
        }

        // Warning: Method is O(N). See FindVisibleSibling function for more information.
        internal static Visual GetPreviousSibling( Visual el )
        {
            // To get next/previous sibling, have to find out where we
            // are in our parent's children collection (ie. our siblings)
            Visual parent = VisualTreeHelper.GetParent(el) as Visual;
            // If parent is null, we're at root, so have no siblings
            if (parent == null)
            {
                return null;
            }

            return FindVisibleSibling ( parent, el, false /* Previous */);
        }

        internal static Visual GetRoot( Visual el )
        {
            // Keep moving up parent chain till we reach the top...
            Visual scan = el;
            for( ; ; )
            {
                Visual test = VisualTreeHelper.GetParent(scan) as Visual;
                if( test == null )
                    break;
                scan = test;
            }
            return scan;
        }

        // Get bounding rectangle, in coords relative to root (not screen)
        internal static Rect GetLocalRect( UIElement element )
        {
            // Get top-most visual.
            Visual parent = GetRoot( element );

            // Get the points for the rectangle and transform them.
            double height = element.RenderSize.Height;
            double width = element.RenderSize.Width;
            Rect rect = new Rect(0, 0, width, height);
            
            GeneralTransform g = element.TransformToAncestor(parent);
            return g.TransformBounds(rect);            
        }

        // Get bounding rectangle, relative to screen
        internal static Rect GetScreenRect( IntPtr hwnd, UIElement el )
        {            
            Rect rc = GetLocalRect( el );
            
            // Map from local to screen coords...
            NativeMethods.RECT rcWin32 = new NativeMethods.RECT( (int) rc.Left, (int) rc.Top, (int) rc.Right, (int) rc.Bottom );
            try
            {
                SafeSecurityHelper.TransformLocalRectToScreen(new HandleRef(null, hwnd), ref rcWin32);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return Rect.Empty;
            }

            rc = new Rect( rcWin32.left, rcWin32.top, rcWin32.right - rcWin32.left, rcWin32.bottom - rcWin32.top );

            return rc;
        }

        // Get element at given point (screen coords)
        internal static Visual GetElementFromPoint( IntPtr hwnd, Visual root, Point pointScreen )
        {
            HwndSource hwndSource = HwndSource.CriticalFromHwnd(hwnd);

            if(hwndSource == null)
                return null;

            Point               pointClient = PointUtil.ScreenToClient( pointScreen, hwndSource );
            Point               pointRoot   = PointUtil.ClientToRoot(pointClient, hwndSource);
            PointHitTestResult  result      = VisualTreeUtils.AsNearestPointHitTestResult(VisualTreeHelper.HitTest(root, pointRoot));
            Visual              visual      = (result != null) ? result.VisualHit : null;


            return visual;
        }

        // Ensures that an element is enabled; throws exception otherwise
        internal static void CheckEnabled(Visual visual)
        {
            UIElement el = visual as UIElement;
            
            if( el != null && ! el.IsEnabled )
            {
                throw new ElementNotEnabledException();
            }
        }

        internal static object Invoke(AutomationPeer peer, DispatcherOperationCallback work, object arg)
        {
            Dispatcher dispatcher = peer.Dispatcher;

            // Null dispatcher likely means the visual is in bad shape!
            if( dispatcher == null )
            {
                throw new ElementNotAvailableException();
            }

            Exception remoteException = null;
            bool completed = false;

            object retVal = dispatcher.Invoke(            
                DispatcherPriority.Send,
                TimeSpan.FromMinutes(3),
                (DispatcherOperationCallback) delegate(object unused)
                {
                    try
                    {
                        return work(arg);
                    }
                    catch(Exception e)
                    {
                        remoteException = e;
                        return null;
                    }
                    catch        //for non-CLS Compliant exceptions
                    {
                        remoteException = null;
                        return null;
                    }
                    finally
                    {
                        completed = true;
                    }
},
                null);
                
            if(completed)
            {
                if(remoteException != null)
                {
                    throw remoteException;
                }
            }
            else
            {
                bool dispatcherInShutdown = dispatcher.HasShutdownStarted;

                if(dispatcherInShutdown)
                {
                    throw new InvalidOperationException(SR.Get(SRID.AutomationDispatcherShutdown));
                }
                else
                {
                    throw new TimeoutException(SR.Get(SRID.AutomationTimeout));
                }
            }
            
            return retVal;
}

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // Potential enhancement: Consider Visual3Ds in this walk?
        //      Does this walk need to continue through the Visual3D tree once
        //      we have UIElement3D?
        private static Visual FindVisibleSibling ( Visual parent, int start, bool searchForwards)
        {
            int index = start;
            int childrenCount = parent.InternalVisualChildrenCount;
            
            while ( index >= 0 && index < childrenCount )
            {
                Visual sibling = parent.InternalGetVisualChild(index);
                
                // if its visible or something other than a UIElement keep it
                if ( !(sibling is UIElement) || (((UIElement)sibling).Visibility == Visibility.Visible ) )
                    return sibling;

                index += searchForwards ? 1 : -1;
            }

            return null;
        }

        // Potential enhancement - Consider Visual3Ds in this walk?
        //      Does this walk need to continue through the Visual3D tree once
        //      we have UIElement3D?
        //
        // WARNING: This method is O(N) and can therefore lead to O(N^2) algorithms.
        private static Visual FindVisibleSibling(Visual parent, Visual child, bool searchForwards)
        {
            //
            // First we figure out the index of the specified child Visual. This is why the runtime
            // of this method is O(n).
            
            int childrenCount = parent.InternalVisualChildrenCount;
            int childIndex;

            for (childIndex = 0; childIndex < childrenCount; childIndex++)
            {
                Visual current = parent.InternalGetVisualChild(childIndex);
                if (current == child)
                {
                    // Found the child.
                    break;
                }
            }

            //
            // Now that we have the child index, we can go and lookup the sibling.
            if(searchForwards)
                return FindVisibleSibling(parent, childIndex+1, searchForwards); // (FindVisibleSibling can deal with out of range indices).
            else
                return FindVisibleSibling(parent, childIndex-1, searchForwards); // (FindVisibleSibling can deal with out of range indices).
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // static class, so no private fields

        #endregion Private Fields
    }
}



