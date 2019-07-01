// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;

using Rect = System.Drawing.Rectangle;
using BindingFlags = System.Reflection.BindingFlags;

#if !STANDALONE_BUILD
using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;
#else
using TrustedAssembly = System.Reflection.Assembly;
using TrustedType = System.Type;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Copy the client area of a window
    /// </summary>
    public sealed class Photographer
    {
        /// <summary/>
        public Bitmap TakeScreenCapture(IntPtr windowHandle)
        {
            WaitForCompleteRender();

#if  USES_RENDERING_VERIFICATION
            //TODO: (pantal) resolve Rendering Verification build flag path

            // The tools team's screen capture API seems to work more reliably than ours in Longhorn.
            return Microsoft.Test.RenderingVerification.ImageUtility.CaptureScreen( windowHandle, true );

#else
            Rect clientArea = new Rect(0, 0, 0, 0);

            if (windowHandle.ToInt32() == 0)
            {
                throw new ApplicationException("Cannot retrieve a capture from a window with a null handle");
            }
            Interop.GetClientRect(windowHandle, ref clientArea);

            if (clientArea.Width == 0 || clientArea.Height == 0)
            {
                throw new Exception("Cannot perform capture on window that has no visible client area");
            }
            return CreateBitmap(clientArea, windowHandle);

#endif
        }

        private Bitmap CreateBitmap(Rect clientArea, IntPtr windowHandle)
        {
            //
            // WorkAround to make GDI screen capture work with avalon.
            //
            // Right now, GetDc(NULL), GetWindowDc(NULL) and
            // CreateDc("DISPLAY"....) work. GetDc(hwnd) and GetWindowDc(hwnd)
            // do not work. We have to convert all GetDc(hwnd) and
            // GetWindowDc(hwnd) calls to GetDc(NULL) and GetWindowDc(NULL)calls.
            //
            // Change GetDc(windowHandle) to GetDc(NULL). This will retrieve the
            // DC of the entire screen and not just the Window
            //
            IntPtr sourceDC = Interop.GetDC(IntPtr.Zero);

            if (sourceDC.ToInt32() == 0)
            {
                throw new Exception("Could not access the window source's device context");
            }

            try
            {
                Bitmap bitmap = new Bitmap(clientArea.Width, clientArea.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
                IntPtr destination = graphics.GetHdc();
                //
                // WorkAround to make GDI screen capture work with avalon
                //
                // Earlier sourceDc pointed to the window. So we had
                //
                // BitBlt( destination, 0, 0, clientArea.Width,
                //         clientArea.Height, sourceDC, 0, 0, SRCCOPY );
                //
                // Note the two zero's (which give the top left corner
                // of the source bitmap) after the  sourceDC paramerer.
                // Now the sourceDC points to the entire screen. So the
                // two zeros should be changed to point to the top left corner
                // of the client area in screen co-ordinates. We already have the
                // client area co-ordinates. This has to be converted to screen
                // co-ordinates. This can be done using the ClientToScreen function
                // in user32.dll
                //

                //
                // WorkAround to make GDI screen capture work with avalon
                // Do a min/max on bot corner points to avoid potential flip issues in RTL layouts
                //
                Point tl = new Point(clientArea.Left, clientArea.Top);
                Point br = new Point(clientArea.Right, clientArea.Bottom);
                Interop.ClientToScreen(windowHandle, ref tl);
                Interop.ClientToScreen(windowHandle, ref br);
                Point pt = new Point();
                pt.X = Math.Min(tl.X, br.X);
                pt.Y = Math.Min(tl.Y, br.Y);

                Interop.BitBlt(destination, 0, 0, clientArea.Width, clientArea.Height, sourceDC, pt.X, pt.Y, Interop.SourceCopy);
                graphics.ReleaseHdc(destination);
                graphics.Dispose();
                return bitmap;
            }
            finally
            {
                Interop.ReleaseDC(IntPtr.Zero, sourceDC);
            }
        }
        
        /// <summary/>
        public static void WaitForCompleteRender()
        {
            Type hwndTargetType = typeof(HwndTarget);
            TrustedAssembly mcasm = TrustedAssembly.GetAssembly(hwndTargetType);
            TrustedType mcType = mcasm.GetType("System.Windows.Media.MediaContext");

            object mediaContext = mcType.InvokeMember("From", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { System.Windows.Threading.Dispatcher.CurrentDispatcher });
            mcType.InvokeMember("CompleteRender", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, mediaContext, new object[] { });
        }
    }
}

