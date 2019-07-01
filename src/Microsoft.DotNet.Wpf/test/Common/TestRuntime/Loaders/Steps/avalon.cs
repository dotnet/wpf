// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Test.Utilities.Reflection;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    internal class AvalonHelper
    {
        /// <summary>
        /// We can't make a window in partial trust, so this makes one for us
        /// </summary>
        /// <returns></returns>
        internal static Window MakeUnsafeWindow()
        {
            Window win = new Window();
            win.Top = 50; win.Left = 50;
            win.Height = 300; win.Width = 300;
            win.Title = "Unsafe test window";
            win.Show();
            return win;
        }

        /// <summary>
        /// Let the partial trust test app close the window we created
        /// </summary>
        /// <param name="win"></param>
        internal static void CloseUnsafeWindow(Window win)
        {
            win.Close();
        }

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hwndFrom, [In, Out] ref POINT pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            internal int x;
            internal int y;
            internal POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        internal static Rect GetBoundingRectangle(UIElement element)
        {
            // get visual size (to get width and height)
            PresentationSource s = PresentationSource.FromVisual(element);
            Point size = s.CompositionTarget.TransformToDevice.Transform(new Point(element.RenderSize.Width, element.RenderSize.Height));

            // get absolute position
            Matrix m;
            System.Windows.Media.GeneralTransform gt = 
                element.TransformToAncestor(s.RootVisual);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if (t == null)
            {
                throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
            }
            m = t.Value;
            Rect clientCoordinates = new Rect(
                m.OffsetX,
                m.OffsetY,
                m.OffsetX + size.X,
                m.OffsetY + size.Y);

            // get root hwnd
            HwndSource rootHwnd = (HwndSource)s;

            // get screen coordinates
            POINT topLeft = new POINT((int)m.OffsetX, (int)m.OffsetY);
            POINT screenTopLeft = new POINT();
            ClientToScreen(rootHwnd.Handle, ref screenTopLeft);
            Rect screenCoordinates = new Rect(
                m.OffsetX + screenTopLeft.x,
                m.OffsetY + screenTopLeft.y,
                size.X,
                size.Y);

            return (screenCoordinates);
        }

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowRect", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntGetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);
        private static void GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect)
        {
            bool ret = IntGetWindowRect(hWnd, ref rect);
            if (ret == false)
            {
                int win32Err = Marshal.GetLastWin32Error();
                throw new Win32Exception(win32Err);
            }
        }

        internal static Rect GetVisualRect(Visual element)
        {
            RECT r = new RECT();
            PresentationSource s = PresentationSource.FromVisual(element);
            HwndSource h = (HwndSource)s;
            GetWindowRect(new HandleRef(null, h.Handle), ref r);

            // return the resulting rect. be aware that Height and Width will represent absolute coordinates
            return (new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top));
        }

        private static class ExternDll
        {
            internal const string User32 = "user32.dll";
        }

        internal struct RECT
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;

            internal RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        /// <summary>
        /// InvariantAssert
        /// </summary>
        internal static void InvariantAssert()
        {
            AssemblyProxy p = new AssemblyProxy();
#if TESTBUILD_CLR40
            p.Load(@"WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL");
#endif
#if TESTBUILD_CLR20
            p.Load(@"WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL");
#endif
            p.Parameters.Add(false);
            p.ParametersTypes.Add(typeof(bool));
            p.Invoke("MS.Internal.Invariant", "Assert", null);
        }

        /// <summary>
        /// GetXaml
        /// </summary>
        /// <param name="o"></param>
        internal static string GetXaml(object o)
        {
            return(XamlWriter.Save(o));
        }
    }
}
