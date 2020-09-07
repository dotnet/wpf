// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    using System.Windows;
    using System.Windows.Controls;
    using MS.Internal;
    using System;
    using System.Windows.Threading;
    using System.Collections;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Data;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#endif

    public class RibbonContextualTabGroupsPanel : Panel
    {
        static RibbonContextualTabGroupsPanel()
        {
        }

        #region Protected Methods

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();

            // Don't measure the child if tabs are not ready yet or Ribbon is collapsed.
            if (Ribbon != null && !Ribbon.IsCollapsed)
            {
                double remainingSpace = availableSize.Width;
                bool invalidateTHPanel = false;
                RibbonTabHeadersPanel tabHeadersPanel = null;
                if (Ribbon.RibbonTabHeaderItemsControl != null)
                {
                    tabHeadersPanel = Ribbon.RibbonTabHeaderItemsControl.InternalItemsHost as RibbonTabHeadersPanel;
                }
                double tabHeadersPanelSpaceAvailable = (tabHeadersPanel != null) ? tabHeadersPanel.SpaceAvailable : 0.0; 

                foreach (RibbonContextualTabGroup tabGroupHeader in InternalChildren)
                {
                    double width = 0;
                    tabGroupHeader.ArrangeWidth = 0;
                    tabGroupHeader.ArrangeX = 0;
                    tabGroupHeader.IdealDesiredWidth = 0.0;

                    if (tabGroupHeader.Visibility == Visibility.Visible && tabGroupHeader.FirstVisibleTab != null && DoubleUtil.GreaterThanOrClose(remainingSpace, 0.0))
                    {
                        // Measure the maximum desired width 
                        // TabHeaders should be padded up more if needed. 
                        // Also we need to determine if we need to show the label tooltip
                        tabGroupHeader.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                        tabGroupHeader.IdealDesiredWidth = tabGroupHeader.DesiredSize.Width;
                        
                        // If TabHeadersPanel has space to expand, then invalidate it so that TabHeaders add extra Padding to themselves. 
                        double desiredExtraPadding = tabGroupHeader.IdealDesiredWidth - tabGroupHeader.TabsDesiredWidth;
                        if ( DoubleUtil.GreaterThan(desiredExtraPadding, 0.0) &&
                            DoubleUtil.GreaterThan(tabHeadersPanelSpaceAvailable, 0.0))
                        {
                            invalidateTHPanel = true;
                        }

                        width = tabGroupHeader.TabsDesiredWidth;
                        // If the difference between tabGroupHeader.TabsDesiredWidth and remainingSpace is less
                        // than 1e-10 then assume that both are same. This is because TextBlock is very sensitive to 
                        // even a minute floating point difference and displays ellipsis even when sufficient
                        // space is available. 
                        if (Math.Abs(tabGroupHeader.TabsDesiredWidth - remainingSpace) > _desiredWidthEpsilon)
                        {
                            // Clip on the  left side
                            width = Math.Min(tabGroupHeader.TabsDesiredWidth, remainingSpace);
                        }
                        
                        tabGroupHeader.ArrangeWidth = width ;
                        tabGroupHeader.Measure(new Size(width , availableSize.Height));

                        // If label is truncated - show the tooltip
                        tabGroupHeader.ShowLabelToolTip = DoubleUtil.GreaterThan(tabGroupHeader.IdealDesiredWidth, width);

                        remainingSpace = remainingSpace - width;
                    }

                    desiredSize.Width += width;
                    desiredSize.Height = Math.Max(desiredSize.Height, tabGroupHeader.DesiredSize.Height);
                }

                if (WaitingForMeasure || invalidateTHPanel)
                {
                    if (tabHeadersPanel != null)
                    {
                        tabHeadersPanel.InvalidateMeasure();
                    }
                }
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double startX = 0.0;
            foreach (RibbonContextualTabGroup tabGroupHeader in InternalChildren)
            {
                double width = Math.Max(tabGroupHeader.ArrangeWidth,0);
                double height = tabGroupHeader.DesiredSize.Height;
                double y = finalSize.Height - height;

                tabGroupHeader.ArrangeX = startX;
                tabGroupHeader.Arrange(new Rect(startX, y, width, Math.Max(0.0, height - 1)));

                startX += width;
            }

            InvalidateVisual(); // Ensure OnRender is called to draw the separators

            return finalSize;
        }

        /// <summary>
        ///   Draws separators for the RibbonContextualTabGroups.
        /// </summary>
        /// <param name="drawingContext">The drawing context to use.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!SystemParameters.HighContrast)
            {
                // Calculate separatorHeight
                double separatorHeight = 0.0;
                if (Ribbon != null && Ribbon.RibbonTabHeaderItemsControl != null && Ribbon.RibbonTabHeaderItemsControl.InternalItemsHost != null)
                {
                    separatorHeight = Ribbon.RibbonTabHeaderItemsControl.InternalItemsHost.ActualHeight - RibbonContextualTabGroup.TabHeaderSeparatorHeightDelta;
                }

                Pen separatorPen = SeparatorPen;
                if (separatorPen != null)
                {
                    foreach (RibbonContextualTabGroup tabGroupHeader in InternalChildren)
                    {
                        if (tabGroupHeader.Visibility == Visibility.Visible && tabGroupHeader.ArrangeWidth > 0)
                        {
                            double startX = tabGroupHeader.ArrangeX;
                            if (DoubleUtil.AreClose(startX, 0.0))
                            {
                                // For the first group, draw to the left as well
                                drawingContext.DrawLine(separatorPen, new Point(startX, ActualHeight), new Point(startX, this.ActualHeight + separatorHeight));
                            }
                            // draw separator to the right at _group.DesiredWidth
                            drawingContext.DrawLine(separatorPen, new Point(startX + tabGroupHeader.TabsDesiredWidth, ActualHeight), new Point(startX + tabGroupHeader.TabsDesiredWidth, this.ActualHeight + separatorHeight));
                        }
                    }
                }
            }

            base.OnRender(drawingContext);
        }


        /// <summary>
        ///     This method is invoked when the IsItemsHost property changes.
        /// </summary>
        /// <param name="oldIsItemsHost">The old value of the IsItemsHost property.</param>
        /// <param name="newIsItemsHost">The new value of the IsItemsHost property.</param>
        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

            if (newIsItemsHost)
            {
                RibbonContextualTabGroupItemsControl groupHeaderItemsControl = ParentItemsControl;
                if (groupHeaderItemsControl != null)
                {
                    IItemContainerGenerator generator = groupHeaderItemsControl.ItemContainerGenerator as IItemContainerGenerator;
                    if (generator != null && generator.GetItemContainerGeneratorForPanel(this) == generator)
                    {
                        groupHeaderItemsControl.InternalItemsHost = this;
                    }
                }
            }
            else
            {
                RibbonContextualTabGroupItemsControl groupHeaderItemsControl = ParentItemsControl;
                if (groupHeaderItemsControl != null && groupHeaderItemsControl.InternalItemsHost == this)
                {
                    groupHeaderItemsControl.InternalItemsHost = null;
                }
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonContextualTabGroupsPanel));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }
 
        /// <summary>
        ///     The parent ItemsControl
        /// </summary>
        private RibbonContextualTabGroupItemsControl ParentItemsControl
        {
            get
            {
                return TreeHelper.FindTemplatedAncestor<RibbonContextualTabGroupItemsControl>(this);
            }
        }

        /// <summary>
        /// RibbonTabHeadersPanels (THPanel) and RibbonContextualTabGroupsPanel (CTGHPanel) Measure are interdependent.
        /// CTGHPanel's Measure requires THPanel to be measured. Hence THPanel always call InvalidateMeasure on CTGHPanel after its Measure.
        /// THPanel's Measure requires that CTGHPanel's containers are generated. 
        /// If they are not, then THPanel sets this flag to indicate that it is waiting on CTGHPanel.Measure.  
        /// CTGHPanel checks this flag and then InvalidatesMeasure on THPanel. THPanel unsets this flag after a successful Measure.
        /// We need this flag to avoid an infinite loop between THPanel.Measure and CTGHPanel.Measure.
        /// </summary>
        internal bool WaitingForMeasure
        {
            get;
            set;
        }

        private Pen SeparatorPen
        {
            get
            {
                if (_separatorPen == null)
                {
                    Ribbon ribbon = Ribbon;
                    if (ribbon != null && ribbon.BorderBrush != null)
                    {
                        Brush b = ribbon.BorderBrush.Clone();
                        _separatorPen = new Pen(b, 1.0);
                    }
                }
                return _separatorPen;
            }
        }

        internal void OnNotifyRibbonBorderBrushChanged()
        {
            _separatorPen = null;
            InvalidateVisual();
        }

        Pen _separatorPen;
        private const double _desiredWidthEpsilon = 1e-10;
        #endregion
    }
}
