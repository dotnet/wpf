// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class to get a point that some can click on

using System.Windows.Automation;
using MS.Internal.Automation;
using MS.Win32;
using System;

namespace System.Windows.Automation
{
    static internal class ClickablePoint
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // We will not always find the clickable point.   There are times when it would take so long 
        // to locate the point that it is just not worth it, so make a reason effort than quit.
        public static bool  HitTestForClickablePoint(AutomationElement el, out Point pt)
        {
            Rect rect = el.Current.BoundingRectangle;
            
            pt = new Point(0, 0);
            if (rect.Left >= rect.Right || rect.Top >= rect.Bottom)
                return false;    
            
            // if this is not on any monitor than there is no point in going on.  If the element is 
            // off the screen hit testing actually works and would end up returning a point offscreen.
            NativeMethods.RECT winRect = new NativeMethods.RECT((int)rect.Left, (int)rect.Top, (int)rect.Height, (int)rect.Bottom);
            if (SafeNativeMethods.MonitorFromRect( ref winRect, SafeNativeMethods.MONITOR_DEFAULTTONULL ) == IntPtr.Zero)
                return false;


            // try the center point first
            pt = new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);

            AutomationElement hitElement;
            if ( TryPoint( ref pt, el, out hitElement ) )
                return true;
            
            if ( IsTopLevelWindowObscuring( el, rect, hitElement ) )
                return false;
            
            // before we start hit testing more there are some control types that we know where the 
            // clickable point is or we know does not have a clickable point so take care of those here.
            if ( el.Current.ControlType == ControlType.ScrollBar )
                return false;

            // Try  the mid point of all four sides
            pt = new Point(rect.Left + (rect.Width /2),  rect.Top + 1);
            if ( TryPoint( ref pt, el ) )
                return true;
            
            pt = new Point(rect.Left + (rect.Width /2),  rect.Bottom - 1);
            if ( TryPoint( ref pt, el ) )
                return true;
            
            pt = new Point( rect.Left + 1, rect.Top + (rect.Height /2) );
            if ( TryPoint( ref pt, el) )
                return true;
            
            pt = new Point(  rect.Right - 1, rect.Top + (rect.Height /2) );
            if ( TryPoint( ref pt, el ) )
                return true;
            
            
            if ( TrySparsePattern( out pt, ref rect, el ) )
                return true;
            
            if ( TryLinePattern( out pt, ref rect, el ) )
                return true;
            
            return false;
        }
        
        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods
        
        // Go from the top left on a diagonal the lower right but don't do more than 25 hits
        private static bool TryLinePattern( out Point pt, ref Rect rect,  AutomationElement el)
        {
            double x = rect.Left + 1;
            double y = rect.Top + 1;
            int hits;

            // Adjust the number of hits we do based on how big something is
            Double size = rect.Width * rect.Height;
            if ( size < 2500 )
                hits = 10;
            else if ( size < 20000 )
                hits = 18;
            else
                hits = 25;
            
            double xIncr = rect.Width / hits;
            double yIncr = rect.Height / hits;
            for ( int i = 0; i < hits; i++)
            {
                pt = new Point(x,  y);
                if ( TryPoint( ref pt, el ) )
                    return true;

                x += xIncr;
                y += yIncr;
            }

            pt = new Point(0,  0);
            return false;
        }

        // Hit test in a fairly uniform pattern like a grid adjusting the spacing based on the
        // size of element.  Don't more than about 25 hits.
        private static bool TrySparsePattern( out Point pt, ref Rect rect,  AutomationElement el)
        {
            int hits;
            
            // Adjust the number of hits we do based on how big somting is
            Double size = rect.Width * rect.Height;
            if ( size < 2500 )
                hits = 3;
            else if ( size < 20000 )
                hits = 4;
            else
                hits = 5;

            // make the scatter pattern fit the proportions of the rect
            double xHits = hits * (rect.Width / rect.Height);
            double yHits = hits * (rect.Height / rect.Width);
            
            double xMovePixels = rect.Width / xHits;
            double yMovePixels = rect.Height / yHits;
            
            return TryPattern( xMovePixels, yMovePixels, out pt, ref rect, el );
        }
        
        // this goes across the rect in icrements of x pixels and the down y pixels and across again...
        private static bool TryPattern(double x, double y, out Point pt, ref Rect rect, AutomationElement el )
        {
            for ( double down = rect.Top + y; down < rect.Bottom; down += y  )
            {
                for ( double across = rect.Left + x; across < rect.Right; across += x )
                {
                    pt = new Point(across,  down);
                    if ( TryPoint(ref pt, el) )
                        return true;
                }
            }
            
            pt = new Point(0,  0);
            return false;
        }
        
        private static bool TryPoint( ref Point pt, AutomationElement el )
        {
            AutomationElement hitEl;
            return TryPoint( ref pt, el, out hitEl );
        }
        
        private static bool TryPoint( ref Point pt, AutomationElement el, out AutomationElement hitEl )
        {
            // If the element is obscured by another window or hidden somehow when we try to hit test we don't get back
            //  the same element.  We want to make sure if someone clicks they click on what they expected to click on.
            hitEl = AutomationElement.FromPoint(pt);

            return hitEl == el;
        }

        // figure out if there is a top level window totally obscuring the element.  If that is the case 
        // there is not point in going on.  This code assumes that toplevel windows are rects and that
        // everything in that rect covers up what is underneath.
        private static bool IsTopLevelWindowObscuring( AutomationElement target, Rect targetRect, AutomationElement hitTarget)
        {
            // get the toplevel window for the element that we hit on our first try and the element the we are
            // trying to find a clickable point for.   If they are part of the same top level hwnd than there is 
            // no toplevel window obscuring this element.  
            // There is a rather strange case that can occur with apps like media player where the 
            // hitTarget could be something underneth the element.  In this case zorder might be something to 
            // check but this is hwnd specific it may be better for the provider to provide a clickable point.
            AutomationElement hitTargetAncestor = GetTopLevelAncestor(hitTarget);
            if ( GetTopLevelAncestor(target) == hitTargetAncestor || hitTargetAncestor == null )
                return false;

            // If this toplevel widow completely covers the element than we are obscured.
            Rect hitTargetAncestorRect = hitTargetAncestor.Current.BoundingRectangle;
            if (hitTargetAncestorRect.Contains( targetRect ) )
                return true;
            
            return false;
        }

        private static AutomationElement GetTopLevelAncestor( AutomationElement target )
        {
            AutomationElement root = AutomationElement.RootElement;
            AutomationElement targetAncestor = null;
            
            while (target != root)
            {
                targetAncestor = target;
                
                target = TreeWalker.ControlViewWalker.GetParent( target );
            }

            return targetAncestor;
        }
}
    #endregion Private Methods
}
