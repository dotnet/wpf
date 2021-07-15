// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Defines a Canvas-like class which is used by InkCanvas for the layout.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MS.Internal.Controls
{
    /// <summary>
    /// A subclass of Panel which does layout for InkCanvas.
    /// </summary>
    internal class InkCanvasInnerCanvas : Panel
    {
        //------------------------------------------------------
        //
        //  Cnostructors
        //
        //------------------------------------------------------

        #region Constructors

        internal InkCanvasInnerCanvas(InkCanvas inkCanvas)
        {
            Debug.Assert(inkCanvas != null);
            _inkCanvas = inkCanvas;
        }

        // No default constructor
        private InkCanvasInnerCanvas() { }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override OnVisualChildrenChanged
        /// </summary>
        /// <param name="visualAdded"></param>
        /// <param name="visualRemoved"></param>
        protected internal override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            UIElement removedElement = visualRemoved as UIElement;

            // If there is an element being removed, we should make sure to update our selected elements list..
            if (removedElement != null)
            {
                InkCanvas.InkCanvasSelection.RemoveElement(removedElement);
            }

            //resurface this on the containing InkCanvas
            InkCanvas.RaiseOnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        /// <summary>
        /// Override of <seealso cref="FrameworkElement.MeasureOverride" />
        /// The code is similar to Canvas.MeasureOverride. The only difference we have is that
        /// InkCanvasInnerCanvas does report the size based on its children's sizes.
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        /// <returns>Computed desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            Size newSize = new Size();
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) { continue; }
                child.Measure(childConstraint);

                // NOTICE-2006/02/03-WAYNEZEN,
                // We only honor Left and/or Top property for the measure. 
                // For Right/Bottom, only the child.Width/Height will be used. Those properties will be used by the arrange 
                // but not the measure.
                double left = (double)InkCanvas.GetLeft(child);
                if (!DoubleUtil.IsNaN(left))
                {
                    newSize.Width = Math.Max(newSize.Width, left + child.DesiredSize.Width);
                }
                else
                {
                    newSize.Width = Math.Max(newSize.Width, child.DesiredSize.Width);
                }

                double top = (double)InkCanvas.GetTop(child);
                if (!DoubleUtil.IsNaN(top))
                {
                    newSize.Height = Math.Max(newSize.Height, top + child.DesiredSize.Height);
                }
                else
                {
                    newSize.Height = Math.Max(newSize.Height, child.DesiredSize.Height);
                }
            }

            return newSize;
        }

        /// <summary>
        /// Canvas computes a position for each of its children taking into account their margin and
        /// attached Canvas properties: Top, Left.  
        /// 
        /// Canvas will also arrange each of its children.
        /// This code is same as the Canvas'.
        /// </summary>
        /// <param name="arrangeSize">Size that Canvas will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            //Canvas arranges children at their DesiredSize.
            //This means that Margin on children is actually respected and added
            //to the size of layout partition for a child. 
            //Therefore, is Margin is 10 and Left is 20, the child's ink will start at 30.

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) { continue; }

                double x = 0;
                double y = 0;


                //Compute offset of the child:
                //If Left is specified, then Right is ignored
                //If Left is not specified, then Right is used
                //If both are not there, then 0
                double left = (double)InkCanvas.GetLeft(child);
                if (!DoubleUtil.IsNaN(left))
                {
                    x = left;
                }
                else
                {
                    double right = (double)InkCanvas.GetRight(child);

                    if (!DoubleUtil.IsNaN(right))
                    {
                        x = arrangeSize.Width - child.DesiredSize.Width - right;
                    }
                }

                double top = (double)InkCanvas.GetTop(child);
                if (!DoubleUtil.IsNaN(top))
                {
                    y = top;
                }
                else
                {
                    double bottom = (double)InkCanvas.GetBottom(child);

                    if (!DoubleUtil.IsNaN(bottom))
                    {
                        y = arrangeSize.Height - child.DesiredSize.Height - bottom;
                    }
                }

                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return arrangeSize;
        }

        /// <summary>
        /// OnChildDesiredSizeChanged
        /// </summary>
        /// <param name="child"></param>
        protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            base.OnChildDesiredSizeChanged(child);

            // Invalid InkCanvasInnerCanvas' measure.
            InvalidateMeasure();
        }

        /// <summary>
        /// Override CreateUIElementCollection method.
        /// The logical parent of InnerCanvas will be set to InkCanvas instead.
        /// </summary>
        /// <param name="logicalParent"></param>
        /// <returns></returns>
        protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            // Replace the logical parent of the InnerCanvas children with our InkCanvas.
            return base.CreateUIElementCollection(_inkCanvas);
        }

        /// <summary>
        /// Returns LogicalChildren
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                // InnerCanvas won't have any logical children publicly.
                return EmptyEnumerator.Instance;
            }
        }

        /// <summary>
        /// The overridden GetLayoutClip method
        /// </summary>
        /// <returns>Geometry to use as additional clip if ClipToBounds=true</returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            // 
            // By default an FE will clip its content if the ink size exceeds the layout size (the final arrange size).
            // Since we are auto growing, the ink size is same as the desired size. So it ends up the strokes will be clipped
            // regardless ClipToBounds is set or not. 
            // We override the GetLayoutClip method so that we can bypass the default layout clip if ClipToBounds is set to false.
            if (ClipToBounds)
            {
                return base.GetLayoutClip(layoutSlotSize);
            }
            else
                return null;
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Hit test on the children 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal UIElement HitTestOnElements(Point point)
        {
            UIElement hitElement = null;

            // Do hittest.
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(this, point);

            // Now find out which element is hit if there is a result.
            if (hitTestResult != null)
            {
                Visual visual = hitTestResult.VisualHit as Visual;
                System.Windows.Media.Media3D.Visual3D visual3D = hitTestResult.VisualHit as System.Windows.Media.Media3D.Visual3D;

                DependencyObject currentObject = null;
                if (visual != null)
                {
                    currentObject = visual;
                }
                else if (visual3D != null)
                {
                    currentObject = visual3D;
                }

                while (currentObject != null)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(currentObject);
                    if (parent == InkCanvas.InnerCanvas)
                    {
                        // Break when we hit the inner canvas in the visual tree.
                        hitElement = currentObject as UIElement;
                        Debug.Assert(Children.Contains(hitElement), "The hit element should be a child of InnerCanvas.");
                        break;
                    }
                    else
                    {
                        currentObject = parent;
                    }
                }
            }

            return hitElement;
        }

        /// <summary>
        /// Returns the private logical children
        /// </summary>
        internal IEnumerator PrivateLogicalChildren
        {
            get
            {
                // Return the logical children of the base - Canvas
                return base.LogicalChildren;
            }
        }

        /// <summary>
        /// Returns the associated InkCanvas
        /// </summary>
        internal InkCanvas InkCanvas
        {
            get
            {
                return _inkCanvas;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The host InkCanvas
        private InkCanvas _inkCanvas;

        #endregion Private Fields
    }
}
