// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Collections;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Reflection;
    using System.Runtime.InteropServices;    
    using System.Security;
    using System.Security.Permissions;
    using System.Text;

    using System.Windows;
    using System.Windows.Media;
    using LHPoint = System.Windows.Point;

    using Microsoft.Test.Logging;
    using Microsoft.Test.Win32;

    #endregion Namespaces.

    /// <summary>
    /// This class provides methods to capture bitmaps for test cases.
    /// It will automatically synchronize to rendering as required.
    /// </summary>
    public sealed class BitmapCapture
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors.
        
        /// <summary>Hide the constructor.</summary>
        private BitmapCapture() { }
        
        #endregion Constructors.

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        #region Public methods.
        
        /// <summary>
        /// Creates a bitmap with the image a user sees for the specified
        /// element.
        /// </summary>
        /// <param name="element">Element to create bitmap for.</param>
        /// <returns>The created bitmap.</returns>
        public static Bitmap CreateBitmapFromElement(
            UIElement element)
        {
            new System.Security.Permissions.UIPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();

            if (element == null)
                throw new ArgumentNullException("element");

            IntPtr windowHandle = GetWindowHandleFromElement(element);                        
            Rectangle elementRect = GetTopLevelClientRelativeRect(element);
            using (Bitmap bitmap = CreateBitmapFromWindowHandle(windowHandle))
            {
                // Scale elementRect to proper (bitmap's) DPI
                elementRect = BitmapUtils.AdjustBitmapSubAreaForDpi(bitmap, elementRect);
                return BitmapUtils.CreateSubBitmap(bitmap, elementRect);
            }
        }

        /// <summary>
        /// Creates a bitmap with the iamge a user sees for a window.
        /// </summary>
        /// <param name="window">Window to capture.</param>
        /// <returns>The created bitmap.</returns>
        public static Bitmap CreateBitmapFromWindow(Window window)
        {
            if (window == null)
                throw new ArgumentNullException("window");

            System.Windows.Interop.WindowInteropHelper helper =
                new System.Windows.Interop.WindowInteropHelper(window);
            
            return CreateBitmapFromWindowHandle(helper.Handle);                        
        }

        /// <summary>Creates a bitmap with the image a user sees for a window.</summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <returns>The created bitmap.</returns>
        public static Bitmap CreateBitmapFromWindowHandle(IntPtr windowHandle)
        {
            new SecurityPermission(PermissionState.Unrestricted).Assert();
            Microsoft.Test.Win32.NativeStructs.RECT r = new Microsoft.Test.Win32.NativeStructs.RECT(0, 0, 0, 0);
            //Win32.GetClientRect(windowHandle, ref r);
            NativeMethods.GetClientRect(new HandleRef(new object(), windowHandle), ref r);
            if (r.Width == 0)
            {
                GlobalLog.LogDebug("Unable to retrieve client rectangle for window.");
                GlobalLog.LogDebug("The whole window will be used instead.");
                //Win32.GetWindowRect(windowHandle, ref r);
                NativeMethods.GetWindowRect(new HandleRef(new object(), windowHandle), ref r);
                if (r.Width == 0)
                    throw new Exception("The window refuses to provide width.");
            }
            return CreateBitmapFromWindowHandle(windowHandle, r);
        }
        
        /// <summary>
        /// Waits for the current MediaContext to finish rendering.
        /// This is required for captures when asynchronous composition
        /// is enabled. It is automatically called by BitmapCapture
        /// methods as required.
        /// </summary>
        /// <remarks>
        /// This requires checkin 53818 on the dev tree to be included
        /// in the build, otherwise a MissingMethodException will
        /// be thrown when the CompleteRender method is invoked.
        /// </remarks>
        public static void WaitForCompleteRender()
        {
            const string MediaContextTypeName = "System.Windows.Media.MediaContext";
            
            new System.Security.Permissions.ReflectionPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();

            // Get the assembly that hosts the HwndTarget.
            Type hwndTargetType = typeof(System.Windows.Interop.HwndTarget);
            Assembly mcasm = Assembly.GetAssembly(hwndTargetType);

            // Get the MediaContext type and get an instance from the
            // current Dispatcher with the From(Dispatcher): MediaContext method.
            System.Windows.Threading.Dispatcher context =
                System.Windows.Threading.Dispatcher.CurrentDispatcher;
            if (context == null)
            {
                throw new Exception("There is no current context to " +
                    "synchronize rendering for screen capture.");
            }
            Type mcType = mcasm.GetType(MediaContextTypeName);
            if (mcType == null)
            {
                throw new Exception("Unable to get type [" +
                    MediaContextTypeName + "] from assembly [" +
                    mcasm + "] required for screen capture.");
            }
            object mc = mcType.InvokeMember("From", 
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, 
                null, null, new object[] { System.Windows.Threading.Dispatcher.CurrentDispatcher });
            if (mc == null)
            {
                throw new Exception("Unable to get MediaContext from context");
            }
            
            // Invoke the CompleteRender(): void method.
            mcType.InvokeMember("CompleteRender", 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, 
                null, mc, new object[] {});
        }

        #endregion Public methods.

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private methods.

        /// <summary>
        /// Retrieves the handle for the window an element belongs to.
        /// </summary>
        /// <param name="element">Element to retrieve handle for.</param>
        /// <returns>The handle for the element's window.</returns>
        private static IntPtr GetWindowHandleFromElement(Visual element)
        {
            string errMsg = String.Format(
                "Unable to retrieve window handle for element {0}", element);

            Visual visual = element;
            Window window = visual as Window;
            while (window == null)
            {
                visual = (Visual)VisualTreeHelper.GetParent(visual);
                if (visual == null)
                    throw new Exception(errMsg);
                window = visual as Window;
            }

            System.Windows.Interop.WindowInteropHelper helper =
                new System.Windows.Interop.WindowInteropHelper(window);
            IntPtr result = helper.Handle;
            if (result == IntPtr.Zero)
            {
                throw new Exception(errMsg);
            }
            return result;
        }

        private static void CalculateBoundingPoints(LHPoint[] points,
            out LHPoint topLeft, out LHPoint bottomRight)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                LHPoint p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            topLeft = new LHPoint(minX, minY);
            bottomRight = new LHPoint(maxX, maxY);
        }

        private static Rectangle GetTopLevelClientRelativeRect(
            UIElement element)
        {
            // Get top-most visual.
            Visual parent = element;
            while (VisualTreeHelper.GetParent(parent) != null)
            {
                parent = (Visual)VisualTreeHelper.GetParent(parent);
            }

            // Get the points for the rectangle and transform them.
            double height = element.RenderSize.Height;
            double width = element.RenderSize.Width;
            LHPoint[] points = new LHPoint[4];
            points[0] = new LHPoint(0, 0);
            points[1] = new LHPoint(width, 0);
            points[2] = new LHPoint(0, height);
            points[3] = new LHPoint(width, height);

            Matrix m;
            System.Windows.Media.GeneralTransform gt = element.TransformToAncestor(parent);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
            }
            m = t.Value;
            m.Transform(points);
            LHPoint topLeft, bottomRight;
            CalculateBoundingPoints(points, out topLeft, out bottomRight);
            return new Rectangle(
                (int) topLeft.X, (int) topLeft.Y,
                (int) bottomRight.X - (int) topLeft.X,
                (int) bottomRight.Y - (int) topLeft.Y);
        }

        private static Bitmap CreateBitmapFromWindowHandle(IntPtr windowHandle,
            Microsoft.Test.Win32.NativeStructs.RECT r)
        {
            WaitForCompleteRender();
            //System.Diagnostics.Trace.WriteLine(String.Format(
            //    "Capturing bitmap for window in rectangle [{0};{1} - {2};{3}]",
            //    r.left, r.top, r.right, r.bottom));
            GlobalLog.LogStatus(String.Format(
                "Capturing bitmap for window in rectangle [{0};{1} - {2};{3}]",
                r.left, r.top, r.right, r.bottom));
            new SecurityPermission(PermissionState.Unrestricted).Assert();
            int cx = r.Width;
            int cy = r.Height;
            if (cx <= 0)
                throw new Exception("Window to create bitmap for has <= 0 width.");
            if (cy <= 0)
                throw new Exception("Window to create bitmap for has <= 0 height.");

            IntPtr sourceDC =NativeMethods.GetDC(new HandleRef(new object(),(IntPtr) null));
            Microsoft.Test.Win32.NativeStructs.POINT topLeft = new Microsoft.Test.Win32.NativeStructs.POINT(0, 0); //Get the screen co-ordinates of the client origin.   
            NativeMethods.ClientToScreen(windowHandle, ref topLeft); 

            if ((int)sourceDC == 0)
            {
                string message = String.Format(
                    "Unable to retrieve device context for window handle {0}.",
                    (int) windowHandle);
                throw new Exception(message);
            }
            try
            {
                Bitmap bitmap = new Bitmap(cx, cy,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                Graphics graphics = Graphics.FromImage(bitmap);
                IntPtr destination = graphics.GetHdc();

                const int SRCCOPY = 0x00CC0020;
                NativeMethods.SafeBitBlt(destination, 0, 0, cx, cy, sourceDC, topLeft.x, topLeft.y,
                    SRCCOPY);
                graphics.ReleaseHdc(destination);
                graphics.Dispose();
                return bitmap;
            }
            finally
            {
//                Win32.ReleaseDC(windowHandle, sourceDC);
                NativeMethods.ReleaseDC(new HandleRef(new object(), windowHandle), new HandleRef(new object(), sourceDC));

            }
        }
        
        #endregion Private methods.
    }
}

