// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Windows;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Drawing;
using System.Windows.Media;
using MTI = Microsoft.Test.Input;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Microsoft.Test.Logging;


namespace Microsoft.Test.Layout
{   
    /// <summary></summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        /// <summary></summary>
        internal int left;
            
        /// <summary></summary>
        internal int top;
            
        /// <summary></summary>
        internal int right;
            
        /// <summary></summary>
        internal int bottom;

        /// <summary></summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        internal RECT( int left, int top, int right, int bottom )
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary></summary>
        internal static readonly RECT Empty = new RECT();

        /// <summary></summary>
        /// <returns></returns>
        internal bool IsEmpty
        {
            get
            {
                return left == 0 && top == 0 && right == 0 && bottom == 0;
            }
        }
    }

    /// <summary></summary>
    class ScreenRectHelper
    {
        /// <summary></summary>
        /// <param name="hwndFrom"></param>
        /// <param name="hwndTo"></param>
        /// <param name="rc"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool MapWindowPoints( IntPtr hwndFrom, IntPtr hwndTo, ref RECT rc, int count );

        /// <summary></summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static RECT GetElementRECT( FrameworkElement element )
        {
            Rectangle rc = GetTopLevelClientRelativeRect( element );

            IntPtr windowHandle = GetWindowHandleFromElement(element);
            if( windowHandle == IntPtr.Zero )
            {
                //throw new Exception( "Element has no associated window/HWND" );
                GlobalLog.LogEvidence(new Exception("Element has no associated window/HWND"));
            }

            RECT rcWin32 = new RECT( (int)rc.Left, (int)rc.Top, (int)rc.Right, (int)rc.Bottom );
            MapWindowPoints( windowHandle, IntPtr.Zero, ref rcWin32, 2 );

            return rcWin32;
        }

        private static IntPtr GetWindowHandleFromElement(System.Windows.FrameworkElement element)
        {
            System.Windows.PresentationSource isource = System.Windows.PresentationSource.FromVisual(element );

            if( isource == null )
            {
                //throw new Exception( "Could not get PresentationSource." );
                GlobalLog.LogEvidence(new Exception("Could not get PresentationSource."));
            }

            IWin32Window iwin = (IWin32Window) isource;

            if( iwin == null )
            {
                //throw new Exception( "Could not get IWin32Window." );
                GlobalLog.LogEvidence(new Exception("Could not get IWin32Window."));
            }

            return iwin.Handle;
        }

        private static void CalculateBoundingPoints(System.Windows.Point[] points,
            out System.Windows.Point topLeft, out System.Windows.Point bottomRight)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                System.Windows.Point p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            topLeft = new System.Windows.Point(minX, minY);
            bottomRight = new System.Windows.Point(maxX, maxY);
        }

        private static Rectangle GetTopLevelClientRelativeRect(
            UIElement element)
        {
            // Get top-most visual.
            System.Windows.Media.Visual parent = element;
            while (System.Windows.Media.VisualTreeHelper.GetParent(parent) != null)
            {
                parent = (System.Windows.Media.Visual)System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            // Get the points for the rectangle and transform them.
            double height = element.RenderSize.Height;
            double width = element.RenderSize.Width;
            System.Windows.Point[] points = new System.Windows.Point[4];
            points[0] = new System.Windows.Point(0, 0);
            points[1] = new System.Windows.Point(width, 0);
            points[2] = new System.Windows.Point(0, height);
            points[3] = new System.Windows.Point(width, height);

            System.Windows.Media.Matrix m;
            System.Windows.Media.GeneralTransform gt = element.TransformToAncestor(parent);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            //throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                GlobalLog.LogEvidence(new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change"));
            }
            m = t.Value;
           m.Transform(points);
            System.Windows.Point topLeft, bottomRight;
            CalculateBoundingPoints(points, out topLeft, out bottomRight);
            return new Rectangle(
                (int) topLeft.X, (int) topLeft.Y,
                (int) bottomRight.X - (int) topLeft.X,
                (int) bottomRight.Y - (int) topLeft.Y);
        }
    }
    
    /// <summary></summary>
    public class LayoutUtility
    {
        /// <summary>
        /// This returns the size of element
        /// </summary>
        /// <param element="element"> element as a UIElement </param>
        /// <returns> size of element with ActualWidth and ActualHeight</returns>
        public static System.Windows.Point GetElementSize(FrameworkElement e)
        {
            return new System.Windows.Point(e.ActualWidth, e.ActualHeight);
        }

        /// <summary>
        /// This returns the position of the element.
        /// </summary>
        /// <param element="element"> element as a UIElement </param>
        /// <param ancestor="ancestor"> ancestor as a UIElement </param>
        /// <returns> Point of position X and Y of element </returns>
        public static System.Windows.Point GetElementPosition(UIElement element, UIElement ancestor)
        {
            System.Windows.Point position = new System.Windows.Point();
            Matrix pt;
            System.Windows.Media.GeneralTransform gt  = element.TransformToAncestor(ancestor);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            //throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                GlobalLog.LogEvidence(new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change"));
            }
            pt = t.Value;

            position.X = pt.OffsetX;
            position.Y = pt.OffsetY;

            return position;
        }

        /// <summary>
        /// This returns IInputElement as UIElement within this element
        /// that is at the specified coordinates relative to this element.
        /// </summary>
        /// <param element="element"> element as a UIElement </param>
        /// <param pt="pt"> as a Point </param>
        /// <returns> IInputElement as UIElement </returns>
        public static UIElement GetInputElement(UIElement element, System.Windows.Point pt)
        {
            IInputElement inputElement;
            inputElement = element.InputHitTest(pt);

            if (inputElement == null)
            {
                return null;
            }

            return (UIElement)inputElement;
        }

        /// <summary></summary>
        public struct ActualLayoutAlignments
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="h"></param>
            /// <param name="v"></param>
            public ActualLayoutAlignments(HorizontalAlignment h, VerticalAlignment v)
            {
                _HorizontalAlignment = h;
                _VerticalAlignment = v;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="child"></param>
            public ActualLayoutAlignments(FrameworkElement parent, FrameworkElement child)
            {
                System.Windows.Point parentSize = LayoutUtility.GetElementSize(parent);
                System.Windows.Point childSize = LayoutUtility.GetElementSize(child);
                System.Windows.Point childPosition = LayoutUtility.GetElementPosition(child, parent);
                //Horizontal Alignment
                if (parentSize.X == childSize.X)
                    _HorizontalAlignment = HorizontalAlignment.Stretch;
                else if ((parentSize.X > childSize.X) && (childPosition.X == 0))
                    _HorizontalAlignment = HorizontalAlignment.Left;
                else if (childPosition.X == (parentSize.X - childSize.X))
                    _HorizontalAlignment = HorizontalAlignment.Right;
                else //if (childPosition.X == (parentSize.X - childSize.X) / 2)
                    _HorizontalAlignment = HorizontalAlignment.Center;


                //Vertical Alignment
                if (parentSize.Y == childSize.Y)
                    _VerticalAlignment = VerticalAlignment.Stretch;
                else if ((parentSize.Y > childSize.Y) && (childPosition.Y == 0))
                    _VerticalAlignment = VerticalAlignment.Top;
                else if (childPosition.Y == (parentSize.Y - childSize.Y))
                    _VerticalAlignment = VerticalAlignment.Bottom;
                else //if (childPosition.Y == (parentSize.Y - childSize.Y) / 2)
                    _VerticalAlignment = VerticalAlignment.Center;
            }

            /// <summary>
            /// retrieve actual horizontal alignment.
            /// </summary>
            public HorizontalAlignment ActualHorizontalAlignment
            {
                get { return _HorizontalAlignment; }
                set { _HorizontalAlignment = value; }
            }

            /// <summary>
            /// retrieve actual vertical alignment.
            /// </summary>
            public VerticalAlignment ActualVerticalAlignment
            {
                get { return _VerticalAlignment; }
                set { _VerticalAlignment = value; }
            }

            //private field
            private HorizontalAlignment _HorizontalAlignment;
            private VerticalAlignment _VerticalAlignment;
        }
        
        /// <summary>This returns object that found from Visual Tree</summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <returns>typeof(type) object that found from Visual tree of element</returns>
		public static object GetChildFromVisualTree(UIElement element, Type type)
		{
			return GetChildFromVisualTree(element, type, 0);
		}

		/// <summary>This returns object that found from Visual Tree at specified index</summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <param name="index">index as a integer</param>
		/// <returns>typeof(type) object that found from Visual tree of element at specified index</returns>
		public static object GetChildFromVisualTree(UIElement element, Type type, int index)
		{
			if (element != null)
			{
				ArrayList children = GetVisualChildren(element as Visual, type, new ArrayList());
				if ((index >= 0) && index < children.Count)
					return children[index];
				else
					return null;
			}
			return null;
		}

		/// <summary>
		/// This returns number of objects that found in Visual tree of element.
		/// </summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <returns>number of typeof(type) object</returns>
		public static int GetChildCountFromVisualTree(UIElement element, Type type)
		{
			if (element != null)
			{
				ArrayList children = GetVisualChildren(element as Visual, type, new ArrayList());
				return children.Count;
			}
			return 0;
		}

		/// <summary>
		/// This walks through Visual tree 
		/// and returns ArrayList that contains all of typeof(type) object that found from VisualTree.
		/// </summary>
		/// <param name="visual">visual</param>
		/// <param name="type">type of element</param>
                /// <param name="children"></param>
		/// <returns>ArrayList of typeof(type) object found from Visual Tree.</returns>
		private static ArrayList GetVisualChildren(DependencyObject visual, Type type, ArrayList children)
		{
			if (visual != null)
			{
				if (visual.GetType() == type)
				{
					children.Add(visual);
				}

				int count = VisualTreeHelper.GetChildrenCount(visual);
				for(int i = 0; i < count; i++)
				{
				        DependencyObject vis =  VisualTreeHelper.GetChild(visual,i);
					children = GetVisualChildren(vis, type, children);
				}
			}

			return children;
		}

		/// <summary>
		/// This walks through Logical tree 
		/// and returns ArrayList that contains all of typeof(type) object that found from LogicalTree.
		/// </summary>
		/// <param name="o">DependencyObject</param>
		/// <param name="type">type of element</param>
                /// <param name="children"></param>
		/// <returns>ArrayList of typeof(type) object found from Logical Tree.</returns>
		private static ArrayList GetLogicalChildren(DependencyObject o, Type type, ArrayList children)
		{
		    if (o != null)
		    {
			if (o.GetType() == type)
			{
			    children.Add(o);
			}
			IEnumerator enumerator = LogicalTreeHelper.GetChildren(o).GetEnumerator();
			while (enumerator.MoveNext())
			{
			    DependencyObject current = enumerator.Current as DependencyObject;
			    children = GetLogicalChildren(current, type, children);
			}
		    }
		    return children;
		}

		/// <summary>
		/// This returns number of objects that found in Logical tree of element.
		/// </summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <returns>number of typeof(type) object</returns>
		public static int GetChildCountFromLogicalTree(UIElement element, Type type)
		{
		    if (element != null)
		    {
			ArrayList children = GetLogicalChildren(element as DependencyObject, type, new ArrayList());
			return children.Count;
		    }
		    return 0;
		}
		
		/// <summary>This returns object that found from Logical Tree at specified index</summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <param name="index">index as a integer</param>
		/// <returns>typeof(type) object that found from Logical tree of element at specified index</returns>
        public static object GetChildFromLogicalTree(UIElement element, Type type, int index)
		{
		    if (element != null)
		    {
			ArrayList children = GetLogicalChildren(element as DependencyObject, type, new ArrayList());
			if ((index >= 0) && index < children.Count)
			    return children[index];
			else
			    return null;
		    }
		    return null;
		}
		
		/// <summary>This returns object that found from Logical Tree</summary>
		/// <param name="element">element as a UIElement </param>
		/// <param name="type">type of element</param>
		/// <returns>typeof(type) object that found from Logical tree of element</returns>
		public static object GetChildFromLogicalTree(UIElement element, Type type)
		{
		    return GetChildFromLogicalTree(element, type, 0);
		}
    }
}

