// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;
using System;
using System.Diagnostics;

using MS.Internal;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// ToolBarOverflowPanel
    /// </summary>
    public class ToolBarOverflowPanel : Panel
    {
        #region Properties
        /// <summary>
        /// WrapWidth Property
        /// </summary>
        public static readonly DependencyProperty WrapWidthProperty =
                    DependencyProperty.Register(
                                "WrapWidth",
                                typeof(double),
                                typeof(ToolBarOverflowPanel),
                                new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure),
                                new ValidateValueCallback(IsWrapWidthValid));

        /// <summary>
        /// WrapWidth Property
        /// </summary>
        public double WrapWidth
        {
            get { return (double)GetValue(WrapWidthProperty); }

            set { SetValue(WrapWidthProperty, value); }
        }

        private static bool IsWrapWidthValid(object value)
        {
            double v = (double)value;
            return (DoubleUtil.IsNaN(v)) || (DoubleUtil.GreaterThanOrClose(v, 0d) && !Double.IsPositiveInfinity(v));
        }
        #endregion Properties

        #region Override methods
        /// <summary>
        /// Measure the content and store the desired size of the content
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size curLineSize = new Size();
            _panelSize = new Size();
            _wrapWidth = double.IsNaN(WrapWidth) ? constraint.Width : WrapWidth;
            UIElementCollection children = InternalChildren;
            int childrenCount = children.Count;

            // Add ToolBar items which have IsOverflowItem = true
            ToolBarPanel toolBarPanel = ToolBarPanel;
            if (toolBarPanel != null)
            {
                // Go through the generated items collection and add to the children collection
                // any that are marked IsOverFlowItem but aren't already in the children collection.
                //
                // The order of both collections matters.
                //
                // It is assumed that any children that were removed from generated items will have
                // already been removed from the children collection.
                List<UIElement> generatedItemsCollection = toolBarPanel.GeneratedItemsCollection;
                int generatedItemsCount = (generatedItemsCollection != null) ? generatedItemsCollection.Count : 0;
                int childrenIndex = 0;
                for (int i = 0; i < generatedItemsCount; i++)
                {
                    UIElement child = generatedItemsCollection[i];
                    if ((child != null) && ToolBar.GetIsOverflowItem(child) && !(child is Separator))
                    {
                        if (childrenIndex < childrenCount)
                        {
                            if (children[childrenIndex] != child)
                            {
                                children.Insert(childrenIndex, child);
                                childrenCount++;
                            }
                        }
                        else
                        {
                            children.Add(child);
                            childrenCount++;
                        }
                        childrenIndex++;
                    }
                }

                Debug.Assert(childrenIndex == childrenCount, "ToolBarOverflowPanel.Children count mismatch after transferring children from GeneratedItemsCollection.");
            }

            // Measure all children to determine if we need to increase desired wrapWidth
            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = children[i] as UIElement;
                
                child.Measure(constraint);
                
                Size childDesiredSize = child.DesiredSize;
                if (DoubleUtil.GreaterThan(childDesiredSize.Width, _wrapWidth))
                {
                    _wrapWidth = childDesiredSize.Width;
                }
            }

            // wrapWidth should not be bigger than constraint.Width
            _wrapWidth = Math.Min(_wrapWidth, constraint.Width);

            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i] as UIElement;
                Size sz = child.DesiredSize;

                if (DoubleUtil.GreaterThan(curLineSize.Width + sz.Width, _wrapWidth)) //need to switch to another line
                {
                    _panelSize.Width = Math.Max(curLineSize.Width, _panelSize.Width);
                    _panelSize.Height += curLineSize.Height;
                    curLineSize = sz;

                    if (DoubleUtil.GreaterThan(sz.Width, _wrapWidth)) //the element is wider then the constraint - give it a separate line
                    {
                        _panelSize.Width = Math.Max(sz.Width, _panelSize.Width);
                        _panelSize.Height += sz.Height;
                        curLineSize = new Size();
                    }
                }
                else //continue to accumulate a line
                {
                    curLineSize.Width += sz.Width;
                    curLineSize.Height = Math.Max(sz.Height, curLineSize.Height);
                }
            }

            //the last line size, if any should be added
            _panelSize.Width = Math.Max(curLineSize.Width, _panelSize.Width);
            _panelSize.Height += curLineSize.Height;

            return _panelSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeBounds"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            int firstInLine = 0;
            Size curLineSize = new Size();
            double accumulatedHeight = 0d;
            _wrapWidth = Math.Min(_wrapWidth, arrangeBounds.Width);

            UIElementCollection children = this.Children;
            for (int i = 0; i < children.Count; i++)
            {
                Size sz = children[i].DesiredSize;

                if (DoubleUtil.GreaterThan(curLineSize.Width + sz.Width, _wrapWidth)) //need to switch to another line
                {
                    // Arrange the items in the current line not including the current
                    arrangeLine(accumulatedHeight, curLineSize.Height, firstInLine, i);
                    accumulatedHeight += curLineSize.Height;

                    // Current item will be first on the next line
                    firstInLine = i;
                    curLineSize = sz;
                }
                else //continue to accumulate a line
                {
                    curLineSize.Width += sz.Width;
                    curLineSize.Height = Math.Max(sz.Height, curLineSize.Height);
                }
            }

            arrangeLine(accumulatedHeight, curLineSize.Height, firstInLine, children.Count);

            return _panelSize;
        }

        /// <summary>
        /// Creates a new UIElementCollection. Panel-derived class can create its own version of
        /// UIElementCollection -derived class to add cached information to every child or to
        /// intercept any Add/Remove actions (for example, for incremental layout update)
        /// </summary>
        protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            // we ignore the Logical Parent (this) if we have ToolBar as our TemplatedParent
            return new UIElementCollection(this, TemplatedParent == null ? logicalParent : null);
        }

        private void arrangeLine(double y, double lineHeight, int start, int end)
        {
            double x = 0;
            UIElementCollection children = this.Children;
            for (int i = start; i < end; i++)
            {
                UIElement child = children[i];
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineHeight));
                x += child.DesiredSize.Width;
            }
        }
        #endregion Override methods

        #region private implementation

        private ToolBar ToolBar
        {
            get { return TemplatedParent as ToolBar; }
        }

        private ToolBarPanel ToolBarPanel
        {
            get
            {
                ToolBar tb = ToolBar;
                return tb == null ? null : tb.ToolBarPanel;
            }
        }

        #endregion private implementation

        #region private data
        private double _wrapWidth; // calculated in MeasureOverride and used in ArrangeOverride
        private Size _panelSize;
        #endregion private data

    }
}

