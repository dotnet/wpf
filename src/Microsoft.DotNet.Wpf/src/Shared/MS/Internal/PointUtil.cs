// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

using MS.Win32;

#if PRESENTATION_CORE
using MS.Internal.PresentationCore;
#else
#error There is an attempt to use this class from an unexpected assembly.
#endif

namespace MS.Internal
{
     /// <summary>
     ///    A utility class for converting Point and Rect data between co-ordinate spaces
     /// </summary>
     /// <remarks>
     ///    To avoid confusion, Avalon based Point and Rect variables are prefixed with
     ///    "point" and "rect" respectively, whereas Win32 POINT and RECT variables are
     ///    prefixed with "pt" and "rc" respectively.
     /// </remarks>
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal static class PointUtil
    {
        /// <summary>
        ///     Convert a point from "client" coordinate space of a window into
        ///     the coordinate space of the root element of the same window.
        /// </summary>
        public static Point ClientToRoot(Point point, PresentationSource presentationSource)
        {
            bool success = true;
            return TryClientToRoot(point, presentationSource, true, out success);
        }

        public static Point TryClientToRoot(Point point, PresentationSource presentationSource, bool throwOnError, out bool success)
        {
            // Only do if we allow throwing on error or have a valid PresentationSource and CompositionTarget.
            if (throwOnError || (presentationSource != null && presentationSource.CompositionTarget != null && !presentationSource.CompositionTarget.IsDisposed))
            {
                // Convert from pixels into measure units.
                point = presentationSource.CompositionTarget.TransformFromDevice.Transform(point);

                // REVIEW:
                // We need to include the root element's transform until the MIL
                // team fixes their APIs to do this.
                point = TryApplyVisualTransform(point, presentationSource.RootVisual, true, throwOnError, out success);
            }
            else
            {
                success = false;
                return new Point(0,0);
            }

            return point;
        }

        /// <summary>
        ///     Convert a point from the coordinate space of a root element of
        ///     a window into the "client" coordinate space of the same window.
        /// </summary>
        public static Point RootToClient(Point point, PresentationSource presentationSource)
        {
            // REVIEW:
            // We need to include the root element's transform until the MIL
            // team fixes their APIs to do this.
            point = ApplyVisualTransform(point, presentationSource.RootVisual, false);

            // Convert from measure units into pixels.
            point = presentationSource.CompositionTarget.TransformToDevice.Transform(point);

            return point;
        }

        /// <summary>
        ///     Convert a point from "above" the coordinate space of a
        ///     visual into the the coordinate space "below" the visual.
        /// </summary>
        public static Point ApplyVisualTransform(Point point, Visual v, bool inverse)
        {
            bool success = true;
            return TryApplyVisualTransform(point, v, inverse, true, out success);
        }

        /// <summary>
        ///     Convert a point from "above" the coordinate space of a
        ///     visual into the the coordinate space "below" the visual.
        /// </summary>
        public static Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
        {
            success = true;

            // Notes:
            // 1) First of all the MIL should provide a way of transforming
            //    a point from the window to the root element.
            // 2) A visual can currently have two properties that affect
            //    its coordinate space:
            //    A) Transform - any matrix
            //    B) Offset - a simpification for just a 2D offset.
            // 3) In the future a Visual may have other properties that
            //    affect its coordinate space, which is why the MIL should
            //    provide this API in the first place.
            //
            // The following code was copied from the MIL's TransformToAncestor
            // method on 12/16/2005.
            //
            if(v != null)
            {
                Matrix m = GetVisualTransform(v);

                if (inverse)
                {
                    if(throwOnError || m.HasInverse)
                    {
                        m.Invert();
                    }
                    else
                    {
                        success = false;
                        return new Point(0,0);
                    }
                }

                point = m.Transform(point);
            }

            return point;
        }

        /// <summary>
        ///     Gets the matrix that will convert a point
        ///     from "above" the coordinate space of a visual
        ///     into the the coordinate space "below" the visual.
        /// </summary>
        internal static Matrix GetVisualTransform(Visual v)
        {
            if (v != null)
            {
                Matrix m = Matrix.Identity;

                Transform transform = VisualTreeHelper.GetTransform(v);
                if (transform != null)
                {
                    Matrix cm = transform.Value;
                    m = Matrix.Multiply(m, cm);
                }

                Vector offset = VisualTreeHelper.GetOffset(v);
                m.Translate(offset.X, offset.Y);

                return m;
            }

            return Matrix.Identity;
        }

        /// <summary>
        ///     Convert a point from "client" coordinate space of a window into
        ///     the coordinate space of the screen.
        /// </summary>
        public static Point ClientToScreen(Point pointClient, PresentationSource presentationSource)
        {
            // For now we only know how to use HwndSource.
            HwndSource inputSource = presentationSource as HwndSource;
            if(inputSource == null)
            {
                return pointClient;
            }
            HandleRef handleRef = new HandleRef(inputSource, inputSource.CriticalHandle);

            NativeMethods.POINT ptClient            = FromPoint(pointClient);
            NativeMethods.POINT ptClientRTLAdjusted = AdjustForRightToLeft(ptClient, handleRef);

            UnsafeNativeMethods.ClientToScreen(handleRef, ptClientRTLAdjusted);

            return ToPoint(ptClientRTLAdjusted);
        }

        /// <summary>
        ///     Convert a point from the coordinate space of the screen into
        ///     the "client" coordinate space of a window.
        /// </summary>
        internal static Point ScreenToClient(Point pointScreen, PresentationSource presentationSource)
        {
            // For now we only know how to use HwndSource.
            HwndSource inputSource = presentationSource as HwndSource;
            if(inputSource == null)
            {
                return pointScreen;
            }

            HandleRef handleRef = new HandleRef(inputSource, inputSource.CriticalHandle);

            NativeMethods.POINT ptClient = FromPoint(pointScreen);

            SafeNativeMethods.ScreenToClient(handleRef, ptClient);

            ptClient = AdjustForRightToLeft(ptClient, handleRef);

            return ToPoint(ptClient);
        }

        /// <summary>
        ///     Converts a rectangle from element co-ordinate space to that of the root visual
        /// </summary>
        /// <param name="rectElement">
        ///     The rectangle to be converted
        /// </param>
        /// <param name="element">
        ///     The element whose co-ordinate space you wish to convert from
        /// </param>
        /// <param name="presentationSource">
        ///     The PresentationSource which hosts the specified Visual.  This is passed in for performance reasons.
        /// </param>
        /// <returns>
        ///     The rectangle in the co-ordinate space of the root visual
        /// </returns>
        internal static Rect ElementToRoot(Rect rectElement, Visual element, PresentationSource presentationSource)
        {
            GeneralTransform    transformElementToRoot  = element.TransformToAncestor(presentationSource.RootVisual);
            Rect                rectRoot                = transformElementToRoot.TransformBounds(rectElement);

            return rectRoot;
        }

        /// <summary>
        ///     Converts a rectangle from root visual co-ordinate space to Win32 client
        /// </summary>
        /// <remarks>
        ///     RootToClient takes into account device DPI settings to convert to/from Avalon's assumed 96dpi
        ///     and any "root level" transforms applied to the root such as "right-to-left" inversions.
        /// </remarks>
        /// <param name="rectRoot">
        ///     The rectangle to be converted
        /// </param>
        /// <param name="presentationSource">
        ///     The PresentationSource which hosts the root visual.  This is passed in for performance reasons.
        /// </param>
        /// <returns>
        ///     The rectangle in Win32 client co-ordinate space
        /// </returns>
        internal static Rect RootToClient(Rect rectRoot, PresentationSource presentationSource)
        {
            CompositionTarget   target                  = presentationSource.CompositionTarget;
            Matrix              matrixRootTransform     = PointUtil.GetVisualTransform(target.RootVisual);
            Rect                rectRootUntransformed   = Rect.Transform(rectRoot, matrixRootTransform);
            Matrix              matrixDPI               = target.TransformToDevice;
            Rect                rectClient              = Rect.Transform(rectRootUntransformed, matrixDPI);

            return rectClient;
        }

        /// <summary>
        ///     Converts a rectangle from Win32 client co-ordinate space to Win32 screen
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="rectClient">
        ///     The rectangle to be converted
        /// </param>
        /// <param name="hwndSource">
        ///     The HwndSource corresponding to the Win32 window containing the rectangle
        /// </param>
        /// <returns>
        ///     The rectangle in Win32 screen co-ordinate space
        /// </returns>
        internal static Rect ClientToScreen(Rect rectClient, HwndSource hwndSource)
        {
            Point corner1 = ClientToScreen(rectClient.TopLeft, hwndSource);
            Point corner2 = ClientToScreen(rectClient.BottomRight, hwndSource);
            return new Rect(corner1, corner2);
        }

        /// <summary>
        ///     Adjusts a POINT to compensate for Win32 RTL conversion logic
        /// </summary>
        /// <remarks>
        ///     MITIGATION: AVALON_RTL_AND_WIN32RTL
        ///
        ///     When a window is marked with the WS_EX_LAYOUTRTL style, Win32
        ///     mirrors the coordinates during the various translation APIs.
        ///
        ///     Avalon also sets up mirroring transforms so that we properly
        ///     mirror the output since we render to DirectX, not a GDI DC.
        ///
        ///     Unfortunately, this means that our coordinates are already mirrored
        ///     by Win32, and Avalon mirrors them again.  To work around this
        ///     problem, we un-mirror the coordinates from Win32 before hit-testing
        ///     in Avalon.
        /// </remarks>
        /// <param name="pt">
        ///     The POINT to be adjusted
        /// </param>
        /// <param name="handleRef">
        ///     A HandleRef to the hwnd containing the point to be adjusted
        /// </param>
        /// <returns>
        ///     The adjusted point
        /// </returns>
        internal static NativeMethods.POINT AdjustForRightToLeft(NativeMethods.POINT pt, HandleRef handleRef)
        {
            int windowStyle = SafeNativeMethods.GetWindowStyle(handleRef, true);

            if(( windowStyle & NativeMethods.WS_EX_LAYOUTRTL ) == NativeMethods.WS_EX_LAYOUTRTL)
            {
                NativeMethods.RECT rcClient = new NativeMethods.RECT();
                SafeNativeMethods.GetClientRect(handleRef, ref rcClient);
                pt.x = rcClient.right - pt.x;
            }

            return pt;
        }

        /// <summary>
        ///     Adjusts a RECT to compensate for Win32 RTL conversion logic
        /// </summary>
        /// <remarks>
        ///     MITIGATION: AVALON_RTL_AND_WIN32RTL
        ///
        ///     When a window is marked with the WS_EX_LAYOUTRTL style, Win32
        ///     mirrors the coordinates during the various translation APIs.
        ///
        ///     Avalon also sets up mirroring transforms so that we properly
        ///     mirror the output since we render to DirectX, not a GDI DC.
        ///
        ///     Unfortunately, this means that our coordinates are already mirrored
        ///     by Win32, and Avalon mirrors them again.  To work around this
        ///     problem, we un-mirror the coordinates from Win32 before hit-testing
        ///     in Avalon.
        /// </remarks>
        /// <param name="rc">
        ///     The RECT to be adjusted
        /// </param>
        /// <param name="handleRef">
        /// </param>
        /// <returns>
        ///     The adjusted rectangle
        /// </returns>
        internal static NativeMethods.RECT AdjustForRightToLeft(NativeMethods.RECT rc, HandleRef handleRef)
        {
            int windowStyle = SafeNativeMethods.GetWindowStyle(handleRef, true);

            if(( windowStyle & NativeMethods.WS_EX_LAYOUTRTL ) == NativeMethods.WS_EX_LAYOUTRTL)
            {
                NativeMethods.RECT rcClient = new NativeMethods.RECT();
                SafeNativeMethods.GetClientRect(handleRef, ref rcClient);

                int width   = rc.right - rc.left;       // preserve width
                rc.right    = rcClient.right - rc.left; // set right of rect to be as far from right of window as left of rect was from left of window
                rc.left     = rc.right - width;         // restore width by adjusting left and preserving right
            }
            return rc;
        }

        /// <summary>
        ///     Converts a location from an Avalon Point to a Win32 POINT
        /// </summary>
        /// <remarks>
        ///     Rounds "double" values to the nearest "int"
        /// </remarks>
        /// <param name="point">
        ///     The location as an Avalon Point
        /// </param>
        /// <returns>
        ///     The location as a Win32 POINT
        /// </returns>
        internal static NativeMethods.POINT FromPoint(Point point)
        {
            return new NativeMethods.POINT(DoubleUtil.DoubleToInt(point.X), DoubleUtil.DoubleToInt(point.Y));
        }

        /// <summary>
        ///     Converts a location from a Win32 POINT to an Avalon Point
        /// </summary>
        /// <param name="pt">
        ///     The location as a Win32 POINT
        /// </param>
        /// <returns>
        ///     The location as an Avalon Point
        /// </returns>
        internal static Point ToPoint(NativeMethods.POINT pt)
        {
            return new Point(pt.x, pt.y);
        }

        /// <summary>
        ///     Converts a rectangle from an Avalon Rect to a Win32 RECT
        /// </summary>
        /// <remarks>
        ///     Rounds "double" values to the nearest "int"
        /// </remarks>
        /// <param name="rect">
        ///     The rectangle as an Avalon Rect
        /// </param>
        /// <returns>
        ///     The rectangle as a Win32 RECT
        /// </returns>
        internal static NativeMethods.RECT FromRect(Rect rect)
        {
            NativeMethods.RECT rc = new NativeMethods.RECT();

            rc.top      = DoubleUtil.DoubleToInt(rect.Y);
            rc.left     = DoubleUtil.DoubleToInt(rect.X);
            rc.bottom   = DoubleUtil.DoubleToInt(rect.Bottom);
            rc.right    = DoubleUtil.DoubleToInt(rect.Right);

            return rc;
        }

        /// <summary>
        ///     Converts a rectangle from a Win32 RECT to an Avalon Rect
        /// </summary>
        /// <param name="rc">
        ///     The rectangle as a Win32 RECT
        /// </param>
        /// <returns>
        ///     The rectangle as an Avalon Rect
        /// </returns>
        internal static Rect ToRect(NativeMethods.RECT rc)
        {
            Rect rect = new Rect();

            rect.X      = rc.left;
            rect.Y      = rc.top;
            rect.Width  = rc.right  - rc.left;
            rect.Height = rc.bottom - rc.top;

            return rect;
        }

    }
}


