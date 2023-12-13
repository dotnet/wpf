// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Versioning;

using SD = System.Drawing;
using SDI = System.Drawing.Imaging;
using SW = System.Windows;
using SWM = System.Windows.Media;
using SWC = System.Windows.Controls;
using MS.Win32;

namespace System.Windows.Forms.Integration
{
    internal static class HostUtils
    {
        private const string DISPLAY = "DISPLAY";
        private const int defaultPixelsPerInch = 96;

        private static TraceSwitch imeModeTraceSwitch;

        /// <summary>
        ///  IME context/mode trace switch.
        /// </summary>
        public static TraceSwitch ImeMode
        {
            get
            {
                if (imeModeTraceSwitch == null)
                {
                    imeModeTraceSwitch = new TraceSwitch("ImeMode", "ImeMode Trace Switch");
                }
                return imeModeTraceSwitch;
            }
        }

        // WinForms uses zero OR Int32.MaxValue to request content size.
        // This method normalizes zeros to Int32.MaxValue.
        public static SD.Size ConvertZeroToUnbounded(SD.Size size)
        {
            if (size.Width == 0) { size.Width = int.MaxValue; }
            if (size.Height == 0) { size.Height = int.MaxValue; }
            return size;
        }

        // Dock will request preferred size with one dimension set to 1.
        public static SD.Size ConvertZeroOrOneToUnbounded(SD.Size size)
        {
            if (size.Width == 0 || size.Width == 1) { size.Width = int.MaxValue; }
            if (size.Height == 0 || size.Height == 1) { size.Height = int.MaxValue; }
            return size;
        }

        public static SD.Size ConvertUnboundedToZero(SD.Size size) 
        {
            if (size.Width == int.MaxValue)
            {
                size.Width = 0;
            }
            if (size.Height == int.MaxValue)
            {
                size.Height = 0;
            }
            return size;
        }
        
        public static SD.Size UnionSizes(SD.Size size1, SD.Size size2)
        {
            return new SD.Size(
                Math.Max(size1.Width, size2.Width),
                Math.Max(size1.Height, size2.Height));
        }

        public static SD.Size IntersectSizes(SD.Size size1, SD.Size size2)
        {
            return new SD.Size(
                Math.Min(size1.Width, size2.Width),
                Math.Min(size1.Height, size2.Height));
        }

        internal static Visual GetRootVisual(Visual descendant)
        {
            DependencyObject current = descendant;
            Visual root = descendant;

            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                Visual visual = current as Visual;

                if (visual != null)
                {
                    root = visual;
                }
            }
            return root;
        }

        internal static Point TransformToRootPoint(Visual element, Point pointElement)
        {
            Visual rootVisual = GetRootVisual(element);
            return TransformToParentPoint(element, rootVisual, pointElement);
        }

        internal static Point TransformToParentPoint(Visual element, Visual ancestor, Point pointElement)
        {
            GeneralTransform transform = element.TransformToAncestor(ancestor);

            Point outPoint = new Point();

            outPoint = transform.Transform(pointElement);

            FrameworkElement rootElement = ancestor as FrameworkElement;
            if (rootElement != null)
            {
                if (rootElement.LayoutTransform != null)
                {
                    outPoint = rootElement.LayoutTransform.Transform(outPoint);
                }
                if (rootElement.RenderTransform != null)
                {
                    outPoint = rootElement.RenderTransform.Transform(outPoint);
                }
            }

            return outPoint;
        }

        /// <summary>
        ///     Returns true if the Brush is an opaque solid color. This is useful since
        ///     these colors are easily translated to WinForms.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static bool BrushIsSolidOpaque(SWM.Brush c)
        {
            SWM.SolidColorBrush solid = c as SWM.SolidColorBrush;
            return solid != null && solid.Color.A == 255;
        }

        internal static void SetBackgroundImage(WinFormsAdapter adapter, Control child, SD.Bitmap image)
        {
            if (child != null &&
                (child.BackgroundImage == null || child.BackgroundImage == adapter.BackgroundImage))
            {
                child.BackgroundImage = image;
            }
            adapter.BackgroundImage = image;
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: GetBitmapForTransparentWindowsFormsHost and GetBitmapForOpaqueWindowsFormsHost.
        [ResourceConsumption(ResourceScope.Machine)]
        internal static SD.Bitmap GetBitmapForWindowsFormsHost(WindowsFormsHost host, Brush brush)
        {
            if (brush == Brushes.Transparent)
            {
                return GetBitmapForTransparentWindowsFormsHost(host);
            }
            else
            {
                return GetBitmapForOpaqueWindowsFormsHost(host, brush);
            }
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: new Bitmap, which is machine-wide, and new PaintEventArgs, which is process-wide
        [ResourceConsumption(ResourceScope.Machine | ResourceScope.Process, ResourceScope.Machine | ResourceScope.Process)]
        internal static SD.Bitmap GetBitmapOfControl(Control control, ElementHost host)
        {
            if (control.ClientRectangle.Width == 0 || control.ClientRectangle.Height == 0)
            {
                return null;
            }

            SD.Bitmap bitmap = new SD.Bitmap(control.ClientRectangle.Width, control.ClientRectangle.Height);
            using (SD.Graphics g = SD.Graphics.FromImage(bitmap))
            {
                using (PaintEventArgs args = new PaintEventArgs(g, control.ClientRectangle))
                {
                    host.InvokePaintBackgroundAndPaint(control, args);
                }
            }
            return bitmap;
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: new Bitmap, which is machine-wide, and Graphics.FromImage, which is process-wide
        [ResourceConsumption(ResourceScope.Machine | ResourceScope.Process, ResourceScope.Machine | ResourceScope.Process)]
        internal static SD.Bitmap GetCoveredPortionOfBitmap(Control parentControl, ElementHost childElementHost)
        {
            using (SD.Bitmap parentBitmap = GetBitmapOfControl(parentControl, childElementHost))
            {
                SD.Bitmap returnBitmap = new SD.Bitmap(childElementHost.ClientRectangle.Width, childElementHost.ClientRectangle.Height);
                using (SD.Graphics g = SD.Graphics.FromImage(returnBitmap))
                {
                    g.DrawImage(parentBitmap, -childElementHost.Left, -childElementHost.Top);
                }
                return returnBitmap;
            }
        }

        private static FrameworkElement GetFrameworkElementAncestor(DependencyObject descendant)
        {
            FrameworkElement ancestor = null;
            DependencyObject current = descendant;
            while (current != null)
            {
                FrameworkElement currentElement = current as FrameworkElement;
                if (currentElement != null) { ancestor = currentElement; }

                current = VisualTreeHelper.GetParent(current);
            }
            return ancestor;
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: GetBitmapFromRenderTargetBitmap
        [ResourceConsumption(ResourceScope.Machine)]
        internal static SD.Bitmap GetBitmapForTransparentWindowsFormsHost(WindowsFormsHost host)
        {
            WinFormsAdapter adapter = WindowsFormsHostPropertyMap.GetAdapter(host);
            if (adapter == null) { return null; }

            //We need to find our highest-level ancestor that's a FrameworkElement:
            //if it's not a FrameworkElement, we don't know how big it is, and can't
            //properly deal with it.
            FrameworkElement frameworkElementAncestor = GetFrameworkElementAncestor(host);

            if (frameworkElementAncestor != null)
            {
                RenderTargetBitmap bmp = GetBitmapForFrameworkElement(frameworkElementAncestor);

                Point hostPoint = new Point(0, 0);
                Point parentPoint = new Point(0, 0);
                if (HostUtils.IsRotated(host))
                {
                    //Image is upside down.  Need the lower right corner of host and the 
                    //lower right corner of the parent.
                    hostPoint = HostUtils.TransformToParentPoint(host, frameworkElementAncestor, 
                        new Point(host.ActualWidth, host.ActualHeight));

                    parentPoint = HostUtils.TransformToParentPoint(frameworkElementAncestor, frameworkElementAncestor, 
                        new Point(frameworkElementAncestor.ActualWidth, frameworkElementAncestor.ActualHeight));
                }
                else
                {
                    //Need upper left corner of host and the upper left corner of the parent.
                    hostPoint = HostUtils.TransformToParentPoint(host, frameworkElementAncestor, 
                        new Point(0, 0));

                    parentPoint = HostUtils.TransformToParentPoint(frameworkElementAncestor, frameworkElementAncestor, 
                        new Point(0, 0));
                }
                hostPoint.Offset(-parentPoint.X, -parentPoint.Y);
                return GetBitmapFromRenderTargetBitmap(adapter, bmp, hostPoint);
            }
            return null;
        }

        internal static RenderTargetBitmap GetBitmapForFrameworkElement(FrameworkElement element)
        {
            RenderTargetBitmap bmp = GetRenderTargetBitmapForVisual(
                    (int)Math.Ceiling(element.ActualWidth),
                    (int)Math.Ceiling(element.ActualHeight),
                    element);

            return bmp;
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: Image.FromStream is machine-wide, and control.CreateGraphics and Graphics.FromImage is process-wide
        [ResourceConsumption(ResourceScope.Machine | ResourceScope.Process, ResourceScope.Machine | ResourceScope.Process)]
        internal static SD.Bitmap GetBitmapFromRenderTargetBitmap(Control control, RenderTargetBitmap bmp, Point offset)
        {
            if (bmp == null)
            {
                return new SD.Bitmap(1, 1);
            }
            using (MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                SD.Image newImage = null;
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(memoryStream);

                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

                using (newImage = SD.Image.FromStream(memoryStream))
                using (SD.Graphics graphicsTemplate = control.CreateGraphics())
                {
                    int width = control.Width <= 0 ? 1 : control.Width;
                    int height = control.Width <= 0 ? 1 : control.Height;
                    SD.Bitmap chunk = new System.Drawing.Bitmap(width, height, graphicsTemplate);
                    graphicsTemplate.Dispose();
                    using (SD.Graphics graphics = SD.Graphics.FromImage(chunk))
                    {
                        graphics.DrawImage(newImage, (float)-offset.X, (float)-offset.Y);
                    }
                    return chunk;
                }
            }
        }

        internal static RenderTargetBitmap GetRenderTargetBitmapForVisual(int width, int height, Visual visualToRender)
        {
            if (width < 1 || height < 1) { return null; }
            RenderTargetBitmap bmp = new RenderTargetBitmap(
                    width,
                    height,
                    HostUtils.PixelsPerInch(Orientation.Horizontal),
                    HostUtils.PixelsPerInch(Orientation.Vertical),
                    PixelFormats.Pbgra32);

            bmp.Render(visualToRender);
            return bmp;
        }

        [ResourceExposure(ResourceScope.Machine)]
        // Resource consumption: GetBitmapFromRenderTargetBitmap
        [ResourceConsumption(ResourceScope.Machine)]
        internal static SD.Bitmap GetBitmapForOpaqueWindowsFormsHost(WindowsFormsHost host, Brush brush)
        {
            WinFormsAdapter adapter = WindowsFormsHostPropertyMap.GetAdapter(host);
            if (adapter == null) { return null; }

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(brush, null, new Rect(0, 0, adapter.Width, adapter.Height));
            drawingContext.Close();

            RenderTargetBitmap bmp = GetRenderTargetBitmapForVisual(adapter.Width, adapter.Height, drawingVisual);

            return GetBitmapFromRenderTargetBitmap(adapter, bmp, new Point(0, 0));
        }


        internal static Vector GetScale(Visual visual)
        {
            bool skewed;
            return GetScale(visual, out skewed);
        }

        internal static Vector GetScale(Visual visual, out bool skewed)
        {
            // Determine whether WindowsFormsHost scaling has changed
            Point pOrigin = HostUtils.TransformToRootPoint(visual, new Point(0.0, 0.0));
            Point pX = HostUtils.TransformToRootPoint(visual, new Point(1.0, 0.0));
            Point pY = HostUtils.TransformToRootPoint(visual, new Point(0.0, 1.0));
            Vector xComponent = pX - pOrigin;
            Vector yComponent = pY - pOrigin;
            skewed = (!IsZero(xComponent.Y) || !IsZero(yComponent.X));

            return new Vector(xComponent.Length, yComponent.Length);
        }

        internal static bool IsRotated(Visual visual)
        {
            // Determine whether WindowsFormsHost is rotated
            Point pOrigin = HostUtils.TransformToRootPoint(visual, new Point(0.0, 0.0));
            Point pX = HostUtils.TransformToRootPoint(visual, new Point(1.0, 0.0));
            Point pY = HostUtils.TransformToRootPoint(visual, new Point(0.0, 1.0));
            Vector xComponent = pX - pOrigin;
            Vector yComponent = pY - pOrigin;
            return xComponent.X < 0 || yComponent.Y < 0;
        }

        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: UnsafeNativeMethods.CreateDC
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        internal static int PixelsPerInch(Orientation orientation)
        {
            int nIndex = (orientation == Orientation.Horizontal ?
                NativeMethods.LOGPIXELSX :
                NativeMethods.LOGPIXELSY);
            using (DCSafeHandle screenDC = UnsafeNativeMethods.CreateDC(DISPLAY))
            {
                return screenDC.IsInvalid ? defaultPixelsPerInch :
                    UnsafeNativeMethods.GetDeviceCaps(screenDC, nIndex);
            }
        }

        //Doubles may return != 0 for a small delta: if it's really close to zero, just
        //call it 0.
        internal static bool IsZero(double value)
        {
            //See DoubleUtil.IsZero
            const double DBL_EPSILON = 2.2204460492503131e-16; //smallest double such that 1.0+DBL_EPSILON != 1.0
            return Math.Abs(value) < 10d * DBL_EPSILON;
        }

        internal static int LOWORD(IntPtr ptr)
        {
            return (int)((long)ptr & 0xffff);
        }

        /// <summary>
        ///     Determines whether a FontWeight should be considered bold (everything on 
        ///     the dark side of Medium, including Medium)
        /// </summary>
        /// <param name="fontWeight"></param>
        /// <returns></returns>
        public static bool FontWeightIsBold(FontWeight fontWeight)
        {
            return fontWeight >= SW.FontWeights.Medium;
        }

        /// <summary>
        /// Returns the first ancestor element in the visual tree that has Cursor set, or
        /// ForceCursor if we're mapping it
        /// </summary>
        /// <param name="currentObject"></param>
        /// <param name="forceCursorMapped"></param>
        /// <returns></returns>
        public static FrameworkElement GetCursorSource(DependencyObject currentObject, bool forceCursorMapped)
        {
            while (currentObject != null)
            {
                FrameworkElement currentElement = currentObject as FrameworkElement;
                if (currentElement != null && (
                    currentElement.Cursor != null || 
                        (currentElement.ForceCursor && forceCursorMapped)
                    )
                )
                {
                    return currentElement;
                }
                currentObject = VisualTreeHelper.GetParent(currentObject);
            }
            return null;
        }
    }
}
