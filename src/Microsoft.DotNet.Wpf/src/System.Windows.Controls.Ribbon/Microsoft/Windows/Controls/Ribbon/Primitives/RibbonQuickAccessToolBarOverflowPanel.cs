// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using System;
using System.Collections.Generic;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    /// <summary>
    ///   Used in the RibbonQuickAccessToolBar template as the items host for overflow items.  Not meant to be used separately from RibbonQuickAccessToolBar.
    /// </summary>
    public class RibbonQuickAccessToolBarOverflowPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Size panelDesiredSize = new Size();

            for (int i = 0; i < Children.Count; i++)
            {
                UIElement child = Children[i];
                Debug.Assert(child != null, "child not expected to be null");
                Debug.Assert(RibbonQuickAccessToolBar.GetIsOverflowItem(child) == true, "child expected to have IsOverflowItem == true");

                Size infinity = new Size(Double.PositiveInfinity, availableSize.Height);
                child.Measure(infinity);
                panelDesiredSize.Width += child.DesiredSize.Width;
                panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, child.DesiredSize.Height);
            }

            return panelDesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = Children;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0d;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = (UIElement)children[i];

                rcChild.X += previousChildSize;
                previousChildSize = child.DesiredSize.Width;
                rcChild.Width = previousChildSize;
                rcChild.Height = Math.Max(finalSize.Height, child.DesiredSize.Height);

                child.Arrange(rcChild);
            }

            return finalSize;
        }

        private RibbonQuickAccessToolBar QAT
        {
            get { return TemplatedParent as RibbonQuickAccessToolBar; }
        }
    }
}

