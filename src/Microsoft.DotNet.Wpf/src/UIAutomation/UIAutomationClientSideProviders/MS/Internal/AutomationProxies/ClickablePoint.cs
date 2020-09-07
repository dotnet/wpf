// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Collections;
using System.ComponentModel;
using MS.Win32;

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.AutomationProxies
{
    static internal class ClickablePoint
    {
        /// <summary>
        /// Static Constructor. Retrieve and keeps the hwnd for "Program"
        /// The Windows Rectangle for "Program"  is the union for the real
        /// Estate for all the monitors.
        /// </summary>
        static ClickablePoint()
        {
            _hwndProgman = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            if (_hwndProgman == IntPtr.Zero)
            {
                _hwndProgman = _hwndDesktop;
            }
        }
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Return a clickable point in a Rectangle for a given window
        ///
        /// The algorithm uses the Windows Z order.
        /// To be visible, a rectangle must be enclosed in the hwnd of its parents 
        /// and must but at least partially on top of the all of siblings as predecessors.
        /// In the windows ordered scheme, the first sibling comes on top followed by the 
        /// next, etc. For a given hwnd, it is sufficent then to look for all the 
        /// predecessor siblings.
        ///
        /// The scheme below is using recursion. This is make slightly harder to read
        /// But makes it a bit more efficent.
        /// </summary>
        /// <param name="hwnd">Window Handle</param>
        /// <param name="alIn">Input list of Rectangles to check GetPoint against</param>
        /// <param name="alOut">Output list of Rectangles after the exclusion test</param>
        /// <param name="pt">Clickable Point</param>
        /// <returns>True if there is a clickable in ro</returns>
        static internal bool GetPoint(IntPtr hwnd, ArrayList alIn, ArrayList alOut, ref NativeMethods.Win32Point pt)
        {
            IntPtr hwndStart = hwnd;
            IntPtr hwndCurrent = hwnd;

            // Do the window on top exclusion
            // Only one level deep is necessary as grand children are clipped to their parent (our children)
            for (hwnd = Misc.GetWindow(hwnd, NativeMethods.GW_CHILD); hwnd != IntPtr.Zero; hwnd = Misc.GetWindow(hwnd, NativeMethods.GW_HWNDNEXT))
            {
                // For siblings, the element bounding rectangle must not be covered by the
                // bounding rect of its siblings
                if (!ClickableInRect(hwnd, ref pt, true, alIn, alOut))
                {
                    return false;
                }
            }

            // Check for Parent and Sibling
            hwnd = hwndStart;
            while (true)
            {
                hwnd = Misc.GetWindow(hwnd, NativeMethods.GW_HWNDPREV);
                if (hwnd == IntPtr.Zero)
                {
                    // Very top of the Windows hierarchy we're done
                    if (hwndCurrent == _hwndDesktop)
                    {
                        break;
                    }

                    // The desktop is the parent we should stop here
                    if (Misc.IsBitSet(Misc.GetWindowStyle(hwndCurrent), NativeMethods.WS_POPUP))
                    {
                        hwnd = _hwndDesktop;
                    }
                    else
                    {
                        // We finished with all the hwnd siblings so get to the parent
                        hwnd = Misc.GetParent(hwndCurrent);
                    }

                    if (hwnd == IntPtr.Zero)
                    {
                        // final clipping against the desktop
                        hwnd = _hwndDesktop;
                    }

                    // For parent, the element bounding rectangle must be within it's parent bounding rect
                    // The desktop contains the bounding rectangle only for the main monintor, the 
                    // The Progman window contains the area for the union of all the monitors
                    // Substitute the Desktop with the Progman hwnd for clipping calculation
                    IntPtr hwndClip = hwnd == _hwndDesktop ? _hwndProgman : hwnd;

                    if (!ClickableInRect(hwndClip, ref pt, false, alIn, alOut))
                    {
                        return false;
                    }

                    // Current Parent
                    hwndCurrent = hwnd;
                    continue;
                }

                // For siblings, the element bounding rectangle must not be covered by the
                // bounding rect of its siblings
                if (!ClickableInRect(hwnd, ref pt, true, alIn, alOut))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Go through the list of all element chidren and exclude them from the list of 
        /// visible/clickable rectangles.
        /// The element children may be listed in any order. A check on all of them must
        /// be performed. There is no easy way out.
        /// </summary>
        /// <param name="fragment"></param>
        /// <param name="alIn"></param>
        /// <param name="alOut"></param>
        internal static void ExcludeChildren(ProxyFragment fragment, ArrayList alIn, ArrayList alOut)
        {
            // First go through all the children to exclude whatever is on top
            for (ProxySimple simple = fragment.GetFirstChild(); simple != null; simple = fragment.GetNextSibling(simple))
            {
                // The exclusion for hwnd children is done by the GetPoint routine
                if (simple is ProxyHwnd)
                {
                    continue;
                }

                // Copy the output bits
                alIn.Clear();
                alIn.AddRange(alOut);

                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect(simple.BoundingRectangle);
                CPRect rcp = new CPRect(ref rc, false);

                ClickablePoint.SplitRect(alIn, ref rcp, alOut, true);

                // recurse on the children
                if (simple is ProxyFragment)
                {
                    ExcludeChildren((ProxyFragment)simple, alIn, alOut);
                }
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        #region Internal Fields

        /// <summary>
        /// Rectangle is inclusive exclusive
        /// </summary>
        internal struct CPRect
        {
            internal bool _fNotCovered;

            internal int _left;

            internal int _top;

            internal int _right;

            internal int _bottom;

            internal CPRect(int left, int top, int right, int bottom, bool fRiAsInsideRect)
            {
                _left = left;
                _top = top;
                _right = right;
                _bottom = bottom;
                _fNotCovered = fRiAsInsideRect;
            }

            // ref to make it a pointer
            internal CPRect(ref NativeMethods.Win32Rect rc, bool fRiAsInsideRect)
            {
                _left = rc.left;
                _top = rc.top;
                _right = rc.right;
                _bottom = rc.bottom;
                _fNotCovered = fRiAsInsideRect;
            }

            // return true if the 2 rectangle intersects
            internal bool Intersect(ref CPRect ri)
            {
                return !(_top >= ri._bottom || ri._top >= _bottom || _left >= ri._right || ri._left >= _right);
            }

            // return true if ri completely covers this
            internal bool Overlap(ref CPRect ri)
            {
                return (ri._left <= _left && ri._right >= _right && ri._top <= _top && ri._bottom >= _bottom);
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private static bool ClickableInRect(IntPtr hwnd, ref NativeMethods.Win32Point pt, bool fRiAsInsideRect, ArrayList alIn, ArrayList alOut)
        {
            if (!SafeNativeMethods.IsWindowVisible(hwnd))
            {
                return fRiAsInsideRect;
            }

            // Get the window rect. If this window has a width and it is effectivly invisible
            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect();

            if (!Misc.GetWindowRect(hwnd, ref rc))
            {
                return fRiAsInsideRect;
            }

            if ((rc.right - rc.left) <= 0 || (rc.bottom - rc.top) <= 0)
            {
                return fRiAsInsideRect;
            }

            // Try for transparency...
            if (fRiAsInsideRect)
            {
                int x = (rc.right + rc.left) / 2;
                int y = (rc.top + rc.bottom) / 2;

                try
                {
                    int lr = Misc.ProxySendMessageInt(hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));

                    if (lr == NativeMethods.HTTRANSPARENT)
                    {
                        return true;
                    }
                }
// PRESHARP: Warning - Catch statements should not have empty bodies
#pragma warning disable 6502
                catch (TimeoutException)
                {
                    // Ignore this timeout error.  Avalon HwndWrappers have a problem with this WM_NCHITTEST call sometimes.
                }
#pragma warning restore 6502
            }

            // Copy the output bits
            alIn.Clear();
            alIn.AddRange(alOut);

            CPRect rcp = new CPRect(ref rc, false);

            ClickablePoint.SplitRect(alIn, ref rcp, alOut, fRiAsInsideRect);
            if (!GetClickablePoint(alOut, out pt.x, out pt.y))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Split a rectangle into a maximum of 3 rectangble.
        ///   ro is the outside rectangle and ri is the inside rectangle
        ///   ro might is split vertically in a a maximim of 3 rectangles sharing the same 
        ///   right and left margin
        /// </summary>
        /// <param name="ro">Outside Rectangle</param>
        /// <param name="ri">Inside Rectangle</param>
        /// <param name="left">Left Margin for the resulting rectangles</param>
        /// <param name="right">Right Margin for the resulting rectangles</param>
        /// <param name="alRect">Array of resulting rectangles</param>
        /// <param name="fRiAsInsideRect">Covered flag</param>
        static private void SplitVertical(ref CPRect ro, ref CPRect ri, int left, int right, ArrayList alRect, bool fRiAsInsideRect)
        {
            // bottom clip
            if (ri._bottom > ro._bottom)
            {
                ri._bottom = ro._bottom;
            }

            int top = ro._top;
            int bottom = ri._top;

            if (bottom > top)
            {
                alRect.Add(new CPRect(left, top, right, bottom, ro._fNotCovered));
                top = bottom;
            }

            bottom = ri._bottom;
            if (bottom > top)
            {
                alRect.Add(new CPRect(left, top, right, bottom, ro._fNotCovered & fRiAsInsideRect));
                top = bottom;
            }

            bottom = ro._bottom;
            if (bottom > top)
            {
                alRect.Add(new CPRect(left, top, right, bottom, ro._fNotCovered));
            }
        }

        /// <summary>
        /// Slip a rectangle into a maximum of 5 pieces. 
        /// ro is the out rectangle and ri is the exclusion rectangle.
        /// The number of resulting rectangles varies based on the position of ri relative to ro 
        /// Each resulting reactangles are flaged  as covered or not.
        /// The ro covered flag is or'ed to allow for recursive calls
        ///
        ///      +-----------------+
        ///      |     :  2 :      |
        ///      |     :    :      |
        ///      |     ######      |
        ///      |     ###3##      |
        ///      | 1   ######  5   |
        ///      |     ######      |
        ///      |     :    :      |
        ///      |     :  4 :      |
        ///      |     :    :      |
        ///      +-----------------+
        /// </summary>
        /// <param name="ro">Outside Rectangle</param>
        /// <param name="ri">Inside Rectangle</param>
        /// <param name="alRect">Collection of resulting rectangles</param>
        /// <param name="fRiAsInsideRect"></param>
        static private void SplitRect(ref CPRect ro, CPRect ri, ArrayList alRect, bool fRiAsInsideRect)
        {
            // If ri is fully outside easy way out.
            if (!ro._fNotCovered || !ro.Intersect(ref ri))
            {
                ro._fNotCovered &= fRiAsInsideRect;
                alRect.Add(ro);
                return;
            }

            if (ro.Overlap(ref ri))
            {
                ro._fNotCovered &= !fRiAsInsideRect;
                alRect.Add(ro);
                return;
            }

            // right clip
            if (ri._right > ro._right)
            {
                ri._right = ro._right;
            }

            // bottom clip
            if (ri._bottom > ro._bottom)
            {
                ri._bottom = ro._bottom;
            }

            int left = ro._left;
            int right = ri._left;

            if (right > left)
            {
                alRect.Add(new CPRect(left, ro._top, right, ro._bottom, ro._fNotCovered & fRiAsInsideRect));
                left = right;
            }

            right = ri._right;
            if (right > left)
            {
                SplitVertical(ref ro, ref ri, left, right, alRect, !fRiAsInsideRect);
                left = right;
            }

            right = ro._right;
            if (right > left)
            {
                alRect.Add(new CPRect(left, ro._top, right, ro._bottom, ro._fNotCovered & fRiAsInsideRect));
            }
        }

        /// <summary>
        /// Takes as input a set of rectangles to perform a rectangular decomposition 
        /// based on the ri. It creates a new set of rectangles each of them being 
        /// marked as covered or not.
        /// 
        /// </summary>
        /// <param name="alIn">List of input rectangle</param>
        /// <param name="ri">Overlapping Rectangle</param>
        /// <param name="alOut">New sets of reactangle</param>
        /// <param name="fRiAsInsideRect">Input Rectangle is rectangle covering alIn Rects or everything
        ///                               outside of ri must be marked as covered</param>
        static private void SplitRect(ArrayList alIn, ref CPRect ri, ArrayList alOut, bool fRiAsInsideRect)
        {
            alOut.Clear();
            for (int i = 0, c = alIn.Count; i < c; i++)
            {
                CPRect ro = (CPRect)alIn[i];

                SplitRect(ref ro, ri, alOut, fRiAsInsideRect);
            }
        }

        /// <summary>
        /// Find a clickable point in a list of rectangle.
        /// Goes through the list of rectangle, stops on the first rectangle that is not covered
        /// and returns the mid point
        /// </summary>
        /// <param name="al">list of ractangle</param>
        /// <param name="x">X coordinate for a clickable point</param>
        /// <param name="y">Y coordinate for a clickable point</param>
        /// <returns>Clickable point found</returns>
        static private bool GetClickablePoint(ArrayList al, out int x, out int y)
        {
            for (int i = 0, c = al.Count; i < c; i++)
            {
                CPRect r = (CPRect)al[i];

                if (r._fNotCovered == true && (r._right - r._left) * (r._bottom - r._top) > 0)
                {
                    // Skip if the rectangle is empty
                    if (r._right > r._left && r._bottom > r._top)
                    {
                        // mid point rounded to the left
                        x = ((r._right - 1) + r._left) / 2;
                        y = ((r._bottom - 1) + r._top) / 2;
                        return true;
                    }
                }
            }

            x = y = 0;
            return false;
        }

        #endregion
        
        #region Private fields
        
        // Top level Desktop window
        private static IntPtr _hwndDesktop = UnsafeNativeMethods.GetDesktopWindow();

        /// The WindowsRect for "Program" is the union for the real
        /// estate for all the monitors. Instead of doing clipping against the root of the hwnd
        /// tree that is the desktop. The last clipping should be done against the Progman hwnd.
        private static IntPtr _hwndProgman;
        
        #endregion Private fields

    }
}
