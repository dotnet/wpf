// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Collections;
    using System.Drawing;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Interop;

    using LHPoint = System.Windows.Point;
    using Microsoft.Test.Win32;

    #endregion Namespaces.

    /// <summary>
    /// Caret type Enum
    /// </summary>
    public enum CaretType
    {
        /// <summary>
        /// Normal caret type
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Italic caret type
        /// </summary>
        Italic = 1,
        /// <summary>
        /// BiDi caret type
        /// </summary>
        BiDi = 2,
        /// <summary>
        /// Block caret type
        /// </summary>
        Block = 3
    }

    /// <summary>Provides helpers for Avalon widgets and windows.</summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static class ElementUtils
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public methods.

        /// <summary>Brings a window to the top of the Z-order.</summary>
        /// <param name='window'>Window to bring to top.</param>
        public static void BringToTop(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            IntPtr hwnd = WindowToHwnd(window);
            NativeMethods.SafeBringWindowToTop(hwnd);
        }

        /// <summary>
        /// Finds the first element in the given scope with the specified id.
        /// </summary>
        /// <param name='scope'>Scope from which to begin search.</param>
        /// <param name='id'>ID of framework element.</param>
        /// <returns>The element found, null otherwise.</returns>
        public static FrameworkElement FindElement(
            FrameworkElement scope, string id)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            if (scope.Name == id)
            {
                return scope;
            }

            DependencyObject node = scope;
            IEnumerator enumerator;
            enumerator = LogicalTreeHelper.GetChildren(node).GetEnumerator();
            if (enumerator == null)
            {
                // WORKAROUND: for #863557. Return null when fixed if code gets here.
                return FindElementInVisuals(scope, id);
            }
            while (enumerator.MoveNext())
            {
                FrameworkElement element =
                    enumerator.Current as FrameworkElement;
                if (element != null)
                {
                    FrameworkElement result = FindElement(element, id);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            // WORKAROUND: for #863557. Return null when fixed if code gets here.
            return FindElementInVisuals(scope, id);
        }

        /// <summary>
        /// Finds the first element in the given scope with the specified id
        /// in the visual tree.
        /// </summary>
        /// <param name='scope'>Scope from which to begin search.</param>
        /// <param name='id'>ID of framework element.</param>
        /// <returns>The element found, null otherwise.</returns>
        /// <remarks>
        /// Note that this is not what the end-user would expect to use.
        /// However, it is a valid search operation, and it is required
        /// to work around a Window bug (#863557).
        /// </remarks>
        public static FrameworkElement FindElementInVisuals(
            DependencyObject scope, string id)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            FrameworkElement result = scope as FrameworkElement;
            if (result != null && result.Name == id)
                return result;
            int count = VisualTreeHelper.GetChildrenCount(scope);
            for(int i = 0; i < count; i++)
            {
                // Common base class for Visual and Visual3D is DependencyObject
                DependencyObject visual = VisualTreeHelper.GetChild(scope, i);

                result = FindElementInVisuals(visual, id);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Gets the point in the client area that is equivalent to the
        /// specified point in the coordinate space of the given element.
        /// </summary>
        /// <param name='element'>Element relative to which the point is specified.</param>
        /// <param name='point'>Point to translate.</param>
        /// <returns>The point in client-area-relative coordinates.</returns>
        public static LHPoint GetClientRelativePoint(UIElement element, LHPoint point)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            Visual parent = GetTopMostVisual(element);
            LHPoint newpoint;
            if ((element.TransformToAncestor(parent)).TryTransform(point, out newpoint) == false)
            {
                //A point may not always be transformable
                throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
            }

            return newpoint;
        }

        /// <summary>
        /// Gets the rectangle that bounds the specified element, relative
        /// to the client area of the window the element is in.
        /// </summary>
        /// <param name='element'>Element to get rectangle for.</param>
        /// <returns>The System.Windows.Rect that bounds the element.</returns>
        public static Rect GetClientRelativeRect(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            Visual parent = GetTopMostVisual(element);
            LHPoint[] points = GetRenderSizeBoxPoints(element);

            Matrix m;
            System.Windows.Media.GeneralTransform gt = element.TransformToAncestor(parent);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
            }
            m = t.Value;
            m.Transform(points);

            // Calculate the regular Rectangle that encloses all the points.
            LHPoint topLeft, bottomRight;
            CalculateBoundingPoints(points, out topLeft, out bottomRight);

            return new Rect(
                topLeft.X, topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);
        }

        /// <summary>
        /// Gets the HwndSource associated with the specified window,
        /// possibly null.
        /// </summary>
        /// <param name='window'>Window to get HwndSource for.</param>
        /// <returns>
        /// The HwndSource instance associated with window, null if there
        /// is none.
        /// </returns>
        /// <example>The following sample shows how to use this method.<code>...
        /// private void MyMethod() {
        ///   HwndSource source = ElementUtils.GetHwndSource(
        ///     Application.Current.MainWindow);
        ///   // Do something with source.
        /// }</code></example>
        public static HwndSource GetHwndSource(Window window)
        {
            PresentationSource source;

            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            source = PresentationSource.FromVisual(window);

            return (HwndSource)source;
        }

        /// <summary>
        /// Gets the screen-relative point for the center of the specified element.
        /// </summary>
        /// <param name='element'>Element from which to get center point.</param>
        /// <returns>The center point of element in screen-relative coordinates.</returns>
        public static System.Windows.Point GetScreenRelativeCenter(UIElement element)
        {
            Rect rect;
            
            rect = GetScreenRelativeRect(element);
            return new System.Windows.Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
        }

        /// <summary>
        /// Gets the screen-relative point for a point within the coordinate
        /// space of the specified element.
        /// </summary>
        /// <param name='element'>Element relative to which the point is specified.</param>
        /// <param name='point'>Point to translate.</param>
        /// <returns>The point in screen-relative coordinates.</returns>
        public static LHPoint GetScreenRelativePoint(UIElement element, LHPoint point)
        {
            PresentationSource source;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            LHPoint result = GetClientRelativePoint(element, point);
            source = PresentationSource.FromVisual(element);

            if (source == null)
            {
                throw new InvalidOperationException("element is not connected to visual tree");
            }

            // Offset from client area origin.
            Microsoft.Test.Win32.NativeStructs.POINT p = new Microsoft.Test.Win32.NativeStructs.POINT(0, 0);
            NativeMethods.ClientToScreen(((HwndSource)source).Handle, ref p);

            // Adjust to High DPI.
            float hFactor, vFactor;
            HighDpiScaleFactors(out hFactor, out vFactor);

            result.X =(int)Math.Round(result.X * hFactor) + p.x;
            result.Y =(int)Math.Round(result.Y * vFactor) + p.y;
            return result;
        }

        /// <summary>
        /// Gets the rectangle that bounds the specified element, relative
        /// to the top-left corner of the screen.
        /// </summary>
        /// <param name='element'>Element to get rectangle for.</param>
        /// <returns>The rectangle that bounds the element.</returns>
        public static Rect GetScreenRelativeRect(UIElement element)
        {
            Microsoft.Test.Win32.NativeStructs.POINT topLeft;
            Rect clientRect;
            PresentationSource source;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            source = PresentationSource.FromVisual(element);
            if (source == null)
            {
                throw new InvalidOperationException("Element is not connected to visual tree");
            }

            clientRect = GetClientRelativeRect(element);

            // Adjust to High DPI.
            float hFactor, vFactor;
            HighDpiScaleFactors(out hFactor, out vFactor);

            topLeft = new Microsoft.Test.Win32.NativeStructs.POINT((int)Math.Round(clientRect.Left * hFactor), (int)Math.Round(clientRect.Top * vFactor));
            NativeMethods.ClientToScreen(((HwndSource)source).Handle, ref topLeft);

            return new Rect(topLeft.x, topLeft.y, clientRect.Width *vFactor, clientRect.Height * vFactor);
        }

        /// <summary>
        /// Retrieves the window an element belongs to.
        /// </summary>
        /// <param name="element">Element to retrieve window for.</param>
        /// <returns>The Window instance that holds the element.</returns>
        /// <remarks>
        /// If the window cannot be found, and exception is thrown. This
        /// API never returns null.
        /// </remarks>
        public static Window GetWindowFromElement(Visual element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            object currentElement = element;
            Window window = element as Window;
            while (window == null)
            {
                Visual visual = currentElement as Visual;
                if (visual != null && VisualTreeHelper.GetParent(visual) != null)
                {
                    currentElement = VisualTreeHelper.GetParent(visual);
                }
                else
                {
                    DependencyObject node = currentElement as DependencyObject;
                    if (node == null || LogicalTreeHelper.GetParent(node) == null)
                    {
                        string msg =  "Cannot find window for element " +
                            element + " - unable to go further up from " +
                            " element " + currentElement;
                        throw new Exception(msg);
                    }
                    currentElement = LogicalTreeHelper.GetParent(node);
                }
                window = currentElement as Window;
            }
            return window;
        }

        /// <summary>Retrieves a HWND from a window.</summary>
        /// <param name='window'>Window to get handle from.</param>
        /// <returns>The window handle.</returns>
        public static IntPtr WindowToHwnd(Window window)
        {
            HwndSource source;

            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            source = GetHwndSource(window);

            return source.Handle;
        }

        #endregion Public methods.

        //------------------------------------------------------
        //
        //  Internal  Methods
        //
        //------------------------------------------------------
        #region Internal methods.
        /// <summary>
        /// Find the vertical and horizontal scale factors for different DPI setting.
        /// </summary>
        /// <param name="xFactor">float that returns the horizontal factor</param>
        /// <param name="yFactor">float that returns the vertical factor</param>
        /// <returns></returns>
        internal static void HighDpiScaleFactors(out float xFactor, out float yFactor)
        {
            using (System.Drawing.Graphics gs = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                xFactor = gs.DpiX / 96;
                yFactor = gs.DpiY / 96;
            }
        }
        #endregion
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private methods.

        /// <summary>
        /// Calculates the rectangle with sides parallel to the
        /// axis that bounds all the given points.
        /// </summary>
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

        /// <summary>
        /// Gets an array of four bounding points for the computed
        /// size of the specified element. The top-left corner
        /// is (0;0) and the bottom-right corner is (width;height).
        /// </summary>
        private static LHPoint[] GetRenderSizeBoxPoints(UIElement element)
        {
            // Get the points for the rectangle and transform them.
            double height = element.RenderSize.Height;
            double width = element.RenderSize.Width;
            LHPoint[] points = new LHPoint[4];
            points[0] = new LHPoint(0, 0);
            points[1] = new LHPoint(width, 0);
            points[2] = new LHPoint(0, height);
            points[3] = new LHPoint(width, height);
            return points;
        }

        /// <summary>
        /// Gets the top-most visual for the specified visual element.
        /// </summary>
        private static Visual GetTopMostVisual(Visual element)
        {
            PresentationSource source;

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            source = PresentationSource.FromVisual(element);
            if (source == null)
            {
                throw new InvalidOperationException("The specified UiElement " +
                    "is not connected to a rendering Visual Tree: " + element);
            }

            return source.RootVisual;
        }

        #endregion Private methods.
    }
}

