// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using MS.Internal;

    /// <summary>
    ///     The items panel for RibbonTabHeaderItemsControl
    /// </summary>
    public class RibbonTabHeadersPanel : Panel, IScrollInfo
    {
        #region Protected Methods

        /// <summary>
        ///     Measure
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Note: The logic below assumes that the TabHeaders belonging to inactive
            // ContextualTabGroups have their IsVisible set to false.

            _separatorOpacity = 0.0;
            Size desiredSize = new Size();

            UIElementCollection children = InternalChildren;
            if (children.Count == 0)
            {
                return desiredSize;
            }

            Size childConstraint = new Size(double.PositiveInfinity, availableSize.Height);
            int countRegularTabs = 0;
            double totalDefaultPaddingAllTabHeaders = 0;
            double totalDefaultPaddingRegularTabHeaders = 0;
            double totalDesiredWidthRegularTabHeaders = 0;
            bool showRegularTabHeaderToolTips = false;
            bool showContextualTabHeaderToolTips = false;
            int countVisibleTabs = 0;

            // Measure all TabHeaders to fit their content
            // desiredSize should hold the total size required to fit all TabHeaders
            desiredSize = InitialMeasure(childConstraint,
                                         out totalDefaultPaddingAllTabHeaders,
                                         out totalDefaultPaddingRegularTabHeaders,
                                         out totalDesiredWidthRegularTabHeaders,
                                         out countRegularTabs,
                                         out countVisibleTabs);

            int countContextualTabs = countVisibleTabs - countRegularTabs;
            SpaceAvailable = 0.0;
            double overflowWidth = desiredSize.Width - availableSize.Width; // Total overflow width
            if (DoubleUtil.GreaterThan(overflowWidth, 0))
            {
                // Calculate max tab width if tab clipping is necessary
                double totalClipWidthRegularTabHeaders = 0; // How much pixels we need to clip regular tabs
                double totalClipWidthContextualTabHeaders = 0; // How much pixels we need to clip contextual tabs

                if (DoubleUtil.GreaterThan(overflowWidth, totalDefaultPaddingAllTabHeaders)) // Clipping is necessary - all tabs padding will we 0
                {
                    showRegularTabHeaderToolTips = true;
                    totalClipWidthRegularTabHeaders = overflowWidth - totalDefaultPaddingAllTabHeaders; // Try to use the whole totalClipAmount in the regular tabs
                }

                double maxRegularTabHeaderWidth = CalculateMaxTabHeaderWidth(totalClipWidthRegularTabHeaders, false);
                if (DoubleUtil.AreClose(maxRegularTabHeaderWidth, _tabHeaderMinWidth)) // Regular tabs are clipped to the min size -  need to clip contextual tabs
                {
                    showContextualTabHeaderToolTips = true;
                    double totalClipAmount = overflowWidth - totalDefaultPaddingAllTabHeaders;
                    double usedClipAmount = totalDesiredWidthRegularTabHeaders - totalDefaultPaddingRegularTabHeaders - (_tabHeaderMinWidth * countRegularTabs);
                    totalClipWidthContextualTabHeaders = totalClipAmount - usedClipAmount; // Remaining clipping amount
                }

                double maxContextualTabHeaderWidth = CalculateMaxTabHeaderWidth(totalClipWidthContextualTabHeaders, true);
                double reducePaddingRegularTabHeader = 0;
                double reducePaddingContextualTabHeader = 0;

                if (DoubleUtil.GreaterThanOrClose(totalDefaultPaddingRegularTabHeaders, overflowWidth))
                {
                    reducePaddingRegularTabHeader = (0.5 * overflowWidth) / countRegularTabs;
                    _separatorOpacity = Math.Max(0.0, reducePaddingRegularTabHeader * 0.2);
                }
                else
                {
                    _separatorOpacity = 1.0;
                    reducePaddingRegularTabHeader = double.PositiveInfinity;
                    // If countContextualTabs==0 then reducePaddingContextualTab will become Infinity
                    reducePaddingContextualTabHeader = (0.5 * (overflowWidth - totalDefaultPaddingRegularTabHeaders)) / (countVisibleTabs - countRegularTabs);
                }

                desiredSize = FinalMeasure(childConstraint,
                                           reducePaddingContextualTabHeader,
                                           reducePaddingRegularTabHeader,
                                           maxContextualTabHeaderWidth,
                                           maxRegularTabHeaderWidth);
            }
            else if( countContextualTabs > 0 )
            {
                // After assigning DefaultPadding, we are left with extra space. 
                // If contextual tabs need that extra space, assign them more padding. 
                NotifyDesiredWidthChanged();
                double spaceAvailable = availableSize.Width - desiredSize.Width;
                double availableExtraWidthPerTab = CalculateMaxPadding(spaceAvailable);

                foreach (UIElement child in InternalChildren)
                {
                    RibbonTabHeader ribbonTabHeader = child as RibbonTabHeader;
                    if( ribbonTabHeader != null && ribbonTabHeader.IsVisible && ribbonTabHeader.IsContextualTab)
                    {
                        RibbonContextualTabGroup ctg = ribbonTabHeader.ContextualTabGroup;
                        if (ctg != null && DoubleUtil.GreaterThan(ctg.DesiredExtraPaddingPerTab, 0.0))
                        {
                            double desiredExtraPaddingPerTab = ctg.DesiredExtraPaddingPerTab;
                            double availableExtraWidth = Math.Min(desiredExtraPaddingPerTab, availableExtraWidthPerTab);
                            
                            Thickness newPadding = ribbonTabHeader.Padding;
                            newPadding.Left += availableExtraWidth * 0.5;
                            newPadding.Right += availableExtraWidth * 0.5;
                            ribbonTabHeader.Padding = newPadding;

                            // Remeasure with added padding
                            desiredSize.Width -= ribbonTabHeader.DesiredSize.Width;
                            ribbonTabHeader.Measure(new Size(Double.MaxValue, childConstraint.Height));
                            desiredSize.Width += ribbonTabHeader.DesiredSize.Width;
                        }
                    }
                }
            }

            // If the difference between desiredWidth and constraintWidth is less
            // than 1e-10 then assume that both are same. This avoids unnecessary
            // inequalities in extent and viewport resulting in spontaneous
            // flickering of scroll button.
            if (Math.Abs(desiredSize.Width - availableSize.Width) < _desiredWidthEpsilon)
            {
                desiredSize.Width = availableSize.Width;
            }

            SpaceAvailable = availableSize.Width - desiredSize.Width;
            // Update ContextualTabGroup.TabsDesiredWidth
            NotifyDesiredWidthChanged();

            // Update whether tooltips should be shown.
            UpdateToolTips(showRegularTabHeaderToolTips, showContextualTabHeaderToolTips);

            VerifyScrollData(availableSize.Width, desiredSize.Width);


            // Invalidate ContextualTabHeadersPanel
            if (Ribbon != null)
            {
                RibbonContextualTabGroupItemsControl groupHeaderItemsControl = Ribbon.ContextualTabGroupItemsControl;
                if (groupHeaderItemsControl != null && groupHeaderItemsControl.InternalItemsHost != null)
                {
                    groupHeaderItemsControl.InternalItemsHost.InvalidateMeasure();
                }
            }

            return desiredSize;
        }

        /// <summary>
        ///     Arrange
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Note: The logic below assumes that the TabHeaders belonging to inactive
            // ContextualTabGroups have their IsVisible set to false.

            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            double childX = 0.0;
            Dictionary<object, List<RibbonTabHeaderAndIndex>> contextualTabHeaders = new Dictionary<object, List<RibbonTabHeaderAndIndex>>();
            Ribbon ribbon = Ribbon;
            if (ribbon != null)
            {
                ribbon.TabDisplayIndexToIndexMap.Clear();
                ribbon.TabIndexToDisplayIndexMap.Clear();
            }
            int displayIndex = 0;
            ArrangeRegularTabHeaders(finalSize,
                               ribbon,
                               contextualTabHeaders,
                               ref displayIndex,
                               ref childX);

            ArrangeContextualTabHeaders(finalSize,
                                  ribbon,
                                  contextualTabHeaders,
                                  ref displayIndex,
                                  ref childX);


            // this arrange happens after the RibbonTitlePanel arrange 
            // so we need to update the RibbonContextualTabGroup positions whenever RibbonTabHeaders move around
            if (Ribbon != null)
            {
                // Invalidate TitlePanel
                if (Ribbon.RibbonTitlePanel != null)
                {
                    Ribbon.RibbonTitlePanel.InvalidateMeasure();
                    Ribbon.RibbonTitlePanel.InvalidateArrange();
                }

                // Invalidate ContextualTabHeadersPanel
                RibbonContextualTabGroupItemsControl groupHeaderItemsControl = Ribbon.ContextualTabGroupItemsControl;
                if (groupHeaderItemsControl != null && groupHeaderItemsControl.InternalItemsHost != null)
                {
                    groupHeaderItemsControl.InternalItemsHost.InvalidateArrange();
                }
            }

            InvalidateVisual();

            return finalSize;
        }

        /// <summary>
        ///     Draw the separators if needed.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            if (!SystemParameters.HighContrast && DoubleUtil.GreaterThan(_separatorOpacity, 0))
            {
                Ribbon ribbon = Ribbon;
                Pen separatorPen = SeparatorPen;
                if (ribbon != null && separatorPen != null)
                {
                    double xOffset = -HorizontalOffset;
                    separatorPen.Brush.Opacity = _separatorOpacity;

                    int elementCount = ribbon.TabDisplayIndexToIndexMap.Count;
                    for (int i = 0; i < elementCount; i++)
                    {
                        Debug.Assert(ribbon.TabDisplayIndexToIndexMap.ContainsKey(i));
                        int index = ribbon.TabDisplayIndexToIndexMap[i];
                        Debug.Assert(children.Count > index && index >= 0);
                        UIElement child = children[index];
                        if (!child.IsVisible)
                        {
                            continue;
                        }
                        xOffset += child.DesiredSize.Width;
                        drawingContext.DrawLine(separatorPen, new Point(xOffset, 0), new Point(xOffset, this.ActualHeight));
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
                RibbonTabHeaderItemsControl tabHeaderItemsControl = ParentTabHeaderItemsControl;
                if (tabHeaderItemsControl != null)
                {
                    IItemContainerGenerator generator = tabHeaderItemsControl.ItemContainerGenerator as IItemContainerGenerator;
                    if (generator != null && generator.GetItemContainerGeneratorForPanel(this) == generator)
                    {
                        tabHeaderItemsControl.InternalItemsHost = this;
                    }
                }
            }
            else
            {
                RibbonTabHeaderItemsControl tabHeaderItemsControl = ParentTabHeaderItemsControl;
                if (tabHeaderItemsControl != null && tabHeaderItemsControl.InternalItemsHost == this)
                {
                    tabHeaderItemsControl.InternalItemsHost = null;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The parent RibbonTabHeaderItemsControl
        /// </summary>
        private RibbonTabHeaderItemsControl ParentTabHeaderItemsControl
        {
            get
            {
                FrameworkElement itemsPresenter = TemplatedParent as FrameworkElement;
                if (itemsPresenter != null)
                {
                    return itemsPresenter.TemplatedParent as RibbonTabHeaderItemsControl;
                }

                return null;
            }
        }

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTabHeadersPanel));

        /// <summary>
        ///     This property is used to access Ribbon
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
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

        #endregion

        #region Internal Methods

        internal void OnNotifyRibbonBorderBrushChanged()
        {
            _separatorPen = null;
            InvalidateVisual();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Measures all the children with original constraints.
        /// </summary>
        private Size InitialMeasure(Size constraint,
            out double totalDefaultPaddingAllTabHeaders,
            out double totalDefaultPaddingRegularTabHeaders,
            out double totalDesiredWidthRegularTabHeaders,
            out int countRegularTabs,
            out int countVisibleTabs)
        {
            totalDefaultPaddingAllTabHeaders = 0;
            totalDefaultPaddingRegularTabHeaders = 0;
            totalDesiredWidthRegularTabHeaders = 0;
            countRegularTabs = 0;
            countVisibleTabs = 0;

            UIElementCollection children = InternalChildren;
            Size desiredSize = new Size();
            int countAllTabs = children.Count;
            for (int i = 0; i < countAllTabs; i++)
            {
                RibbonTabHeader ribbonTabHeader = children[i] as RibbonTabHeader;
                if (ribbonTabHeader != null)
                {
                    if (!ribbonTabHeader.IsVisible)
                    {
                        continue;
                    }
                    ribbonTabHeader.Padding = ribbonTabHeader.DefaultPadding; // Always do first meassure with default padding
                    double tabHeaderPadding = ribbonTabHeader.DefaultPadding.Left + ribbonTabHeader.DefaultPadding.Right;
                    totalDefaultPaddingAllTabHeaders += tabHeaderPadding;
                    bool isContextualTab = ribbonTabHeader.IsContextualTab;
                    ribbonTabHeader.Measure(constraint);
                    desiredSize.Width += ribbonTabHeader.DesiredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, ribbonTabHeader.DesiredSize.Height);
                    if (!isContextualTab)
                    {
                        totalDefaultPaddingRegularTabHeaders += tabHeaderPadding;
                        totalDesiredWidthRegularTabHeaders += ribbonTabHeader.DesiredSize.Width;
                        countRegularTabs++;
                    }
                }
                else
                {
                    UIElement child = children[i];
                    if (!child.IsVisible)
                    {
                        continue;
                    }
                    child.Measure(constraint);
                    desiredSize.Width += child.DesiredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, child.DesiredSize.Height);
                    totalDesiredWidthRegularTabHeaders += child.DesiredSize.Width;
                    countRegularTabs++;
                }
                countVisibleTabs++;
            }
            return desiredSize;
        }

        /// <summary>
        ///     Measures all the children with final constraints
        /// </summary>
        private Size FinalMeasure(Size constraint,
            double reducePaddingContextualTabHeader,
            double reducePaddingRegularTabHeader,
            double maxContextualTabHeaderWidth,
            double maxRegularTabHeaderWidth)
        {
            Size desiredSize = new Size();
            UIElementCollection children = InternalChildren;
            int countAllTabs = children.Count;
            for (int i = 0; i < countAllTabs; i++)
            {
                RibbonTabHeader ribbonTabHeader = children[i] as RibbonTabHeader;
                if (ribbonTabHeader != null)
                {
                    if (!ribbonTabHeader.IsVisible)
                    {
                        continue;
                    }
                    bool isContextualTab = ribbonTabHeader.IsContextualTab;
                    double leftPadding = Math.Max(0, ribbonTabHeader.DefaultPadding.Left - (isContextualTab ? reducePaddingContextualTabHeader : reducePaddingRegularTabHeader));
                    double rightPadding = Math.Max(0, ribbonTabHeader.DefaultPadding.Right - (isContextualTab ? reducePaddingContextualTabHeader : reducePaddingRegularTabHeader));
                    ribbonTabHeader.Padding = new Thickness(leftPadding, ribbonTabHeader.DefaultPadding.Top, rightPadding, ribbonTabHeader.DefaultPadding.Bottom);

                    ribbonTabHeader.Measure(new Size(isContextualTab ? maxContextualTabHeaderWidth : maxRegularTabHeaderWidth, constraint.Height));

                    desiredSize.Width += ribbonTabHeader.DesiredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, ribbonTabHeader.DesiredSize.Height);
                }
                else
                {
                    UIElement child = children[i];
                    if (!child.IsVisible)
                    {
                        continue;
                    }
                    child.Measure(new Size(maxRegularTabHeaderWidth, constraint.Height));
                    desiredSize.Width += child.DesiredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, child.DesiredSize.Height);
                }
            }

            return desiredSize;
        }

        // This method determine how much tabs will be clipped by caluclating the maximum tab width
        // clipWidth parameter is the amount the needs to be removed from tabs
        // Algorithm steps:
        // 1. Sort all tabs sizes
        // 2. maxTabWidth = max tab size - clipWidth
        // 3. if there is an element bigger that maxTabWidth - include this element and calulate new average
        // 4. Return maxTabWidth coerced with some min width
        private double CalculateMaxTabHeaderWidth(double clipWidth, bool forContextualTabs)
        {
            // If clipping is not necessary - return Max double
            if (DoubleUtil.LessThanOrClose(clipWidth, 0))
            {
                return Double.MaxValue;
            }

            UIElementCollection children = InternalChildren;
            int childCount = children.Count;

            // Sort element sizes without the padding
            List<double> elementSizes = new List<double>();
            foreach (UIElement element in children)
            {
                if (!element.IsVisible)
                {
                    continue;
                }
                double elementSize = element.DesiredSize.Width;
                RibbonTabHeader tabHeader = element as RibbonTabHeader;
                if (tabHeader != null)
                {
                    if (tabHeader.IsContextualTab != forContextualTabs)
                    {
                        continue;
                    }
                    elementSize = elementSize - tabHeader.DefaultPadding.Left - tabHeader.DefaultPadding.Right;
                }
                elementSizes.Add(elementSize);
            }
            int sizeCount = elementSizes.Count;
            if (sizeCount == 0)
            {
                return _tabHeaderMinWidth;
            }
            elementSizes.Sort();


            // Clip the max element
            double maxTabHeaderWidth = elementSizes[sizeCount - 1] - clipWidth;
            for (int i = 1; i < sizeCount; i++)
            {
                double currentWidth = elementSizes[sizeCount - 1 - i];
                if (DoubleUtil.GreaterThanOrClose(maxTabHeaderWidth, currentWidth))
                {
                    break;
                }
                // Include next element and calculate new average
                maxTabHeaderWidth = ((maxTabHeaderWidth * i) + currentWidth) / (i + 1);
            }
            return Math.Max(_tabHeaderMinWidth, maxTabHeaderWidth);
        }

        /// <summary>
        /// This algorithm calculates the extra Padding that can be assigned to a contextual tab. 
        /// </summary>
        /// <param name="spaceAvailable"></param>
        /// <returns></returns>
        private double CalculateMaxPadding(double spaceAvailable)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;

            // Sort DesiredPaddings
            List<double> desiredPaddings = new List<double>();
            double totalDesiredPadding = 0.0;
            foreach (UIElement element in children)
            {
                if (!element.IsVisible)
                {
                    continue;
                }
                RibbonTabHeader tabHeader = element as RibbonTabHeader;
                if (tabHeader != null && tabHeader.IsContextualTab)
                {
                    RibbonContextualTabGroup tabGroup = tabHeader.ContextualTabGroup;
                    if (tabGroup != null && DoubleUtil.GreaterThan(tabGroup.DesiredExtraPaddingPerTab, 0.0))
                    {
                        // ContextualTabGroup requires this much more width to reach its ideal DesiredSize. 
                        double desiredPaddingPerTabHeader = tabGroup.DesiredExtraPaddingPerTab; 
                        desiredPaddings.Add(desiredPaddingPerTabHeader);
                        totalDesiredPadding += desiredPaddingPerTabHeader;
                    }
                }
            }
            int sizeCount = desiredPaddings.Count;
            if (sizeCount == 0)
            {
                return 0.0;
            }
            desiredPaddings.Sort();

            double delta = totalDesiredPadding - spaceAvailable;
            if (DoubleUtil.LessThanOrClose(delta, 0.0))
            {
                return desiredPaddings[sizeCount - 1];
            }

            // Clip the TabHeader requesting most extra Padding
            double maxDesiredPadding = desiredPaddings[sizeCount - 1] - delta;
            for (int i = 1; i < sizeCount; i++)
            {
                double currentDesiredPadding = desiredPaddings[sizeCount - 1 - i];
                if (DoubleUtil.GreaterThanOrClose(maxDesiredPadding, currentDesiredPadding))
                {
                    break;
                }
                // Include next element and calculate new average
                maxDesiredPadding = ((maxDesiredPadding * i) + currentDesiredPadding) / (i + 1);
            }
            return maxDesiredPadding;
        }

        /// <summary>
        /// Called whenever RibbonTabHeaders are measured
        /// Sums up DesiredSize.Width of each RibbonTabHeader belonging to a ContextualTabGroup and stores it as ContextualTabGroup.TabsDesiredWidth.
        /// </summary>
        private void NotifyDesiredWidthChanged()
        {
            // Invalidate ContextualTabHeadersPanel
            RibbonContextualTabGroupItemsControl groupHeaderItemsControl = Ribbon.ContextualTabGroupItemsControl;
            if (groupHeaderItemsControl != null && groupHeaderItemsControl.InternalItemsHost != null)
            {
                foreach (RibbonContextualTabGroup tabGroup in groupHeaderItemsControl.InternalItemsHost.Children)
                {
                    tabGroup.TabsDesiredWidth = 0.0;
                    tabGroup.DesiredExtraPaddingPerTab = 0.0;
                }
            }

            foreach (UIElement element in InternalChildren)
            {
                RibbonTabHeader tabHeader = element as RibbonTabHeader;
                if (tabHeader != null && tabHeader.IsVisible && tabHeader.IsContextualTab)
                {
                    RibbonContextualTabGroup tabGroup = tabHeader.ContextualTabGroup;
                    if (tabGroup != null)
                    {
                        double previousTabCount = 0;
                        if (!DoubleUtil.IsZero(tabGroup.DesiredExtraPaddingPerTab))
                        {
                            previousTabCount = (tabGroup.IdealDesiredWidth - tabGroup.TabsDesiredWidth) / tabGroup.DesiredExtraPaddingPerTab;
                        }
                        tabGroup.TabsDesiredWidth += tabHeader.DesiredSize.Width;
                        // compute new average                        
                        tabGroup.DesiredExtraPaddingPerTab = (tabGroup.IdealDesiredWidth - tabGroup.TabsDesiredWidth) / (previousTabCount + 1);
                    }
                }
            }
        }

        /// <summary>
        ///     Set show tooltips depending on whether the tab header is clipped or not.
        /// </summary>
        /// <param name="showRegularTabHeaderToolTips"></param>
        /// <param name="showContextualTabHeaderToolTips"></param>
        private void UpdateToolTips(bool showRegularTabHeaderToolTips, bool showContextualTabHeaderToolTips)
        {
            UIElementCollection children = InternalChildren;
            int countAllTabs = children.Count;
            for (int i = 0; i < countAllTabs; i++)
            {
                RibbonTabHeader tabHeader = children[i] as RibbonTabHeader;
                if (tabHeader != null)
                {
                    tabHeader.ShowLabelToolTip = tabHeader.IsContextualTab ? showContextualTabHeaderToolTips : showRegularTabHeaderToolTips;
                }
            }
        }

        private struct RibbonTabHeaderAndIndex
        {
            public RibbonTabHeader RibbonTabHeader { get; set; }
            public int Index { get; set; }
        }

        /// <summary>
        ///     Arranges regular tab headers and builds a map of
        ///     RibbonTab.ContextualTabGroupHeader to list of RibbonTabHeaders
        /// </summary>
        /// <param name="arrangeSize"></param>
        /// <param name="ribbon"></param>
        /// <param name="contextualTabHeaders"></param>
        /// <param name="displayIndex"></param>
        /// <param name="childX"></param>
        private void ArrangeRegularTabHeaders(Size arrangeSize,
            Ribbon ribbon,
            Dictionary<object, List<RibbonTabHeaderAndIndex>> contextualTabHeaders,
            ref int displayIndex,
            ref double childX)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (!child.IsVisible)
                {
                    continue;
                }
                RibbonTabHeader tabHeader = child as RibbonTabHeader;
                if (tabHeader != null)
                {
                    RibbonTab tab = tabHeader.RibbonTab;
                    if (tab != null && tab.IsContextualTab)
                    {
                        object contextualTabGroupHeader = tab.ContextualTabGroupHeader;
                        if (!contextualTabHeaders.ContainsKey(contextualTabGroupHeader))
                        {
                            contextualTabHeaders[contextualTabGroupHeader] = new List<RibbonTabHeaderAndIndex>();
                        }
                        contextualTabHeaders[contextualTabGroupHeader].Add(new RibbonTabHeaderAndIndex() { RibbonTabHeader = tabHeader, Index = i });
                        continue;
                    }
                }

                child.InvalidateVisual();
                child.Arrange(new Rect(childX - HorizontalOffset, arrangeSize.Height - child.DesiredSize.Height, child.DesiredSize.Width, child.DesiredSize.Height));
                childX += child.DesiredSize.Width;
                if (ribbon != null)
                {
                    ribbon.TabDisplayIndexToIndexMap[displayIndex] = i;
                    ribbon.TabIndexToDisplayIndexMap[i] = displayIndex;
                    displayIndex++;
                }
            }
        }

        /// <summary>
        ///     Arranges contextual tab headers
        /// </summary>
        private void ArrangeContextualTabHeaders(Size arrangeSize,
            Ribbon ribbon,
            Dictionary<object, List<RibbonTabHeaderAndIndex>> contextualTabHeaders,
            ref int displayIndex,
            ref double childX)
        {
            if (ribbon != null)
            {
                RibbonContextualTabGroupItemsControl groupHeaderItemsControl = ribbon.ContextualTabGroupItemsControl;
                if (groupHeaderItemsControl != null)
                {
                    if (groupHeaderItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        int groupHeaderCount = groupHeaderItemsControl.Items.Count;
                        for (int i = 0; i < groupHeaderCount; i++)
                        {
                            RibbonContextualTabGroup groupHeader = groupHeaderItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as RibbonContextualTabGroup;
                            if (groupHeader != null)
                            {
                                object contextualTabGroupHeader = groupHeader.Header;
                                if (contextualTabGroupHeader != null && contextualTabHeaders.ContainsKey(contextualTabGroupHeader))
                                {
                                    foreach (RibbonTabHeaderAndIndex headerAndIndex in contextualTabHeaders[contextualTabGroupHeader])
                                    {
                                        RibbonTabHeader child = headerAndIndex.RibbonTabHeader;
                                        Debug.Assert(child != null);
                                        child.InvalidateVisual();
                                        child.Arrange(new Rect(childX - HorizontalOffset, arrangeSize.Height - child.DesiredSize.Height, child.DesiredSize.Width, child.DesiredSize.Height));
                                        childX += child.DesiredSize.Width;

                                        ribbon.TabDisplayIndexToIndexMap[displayIndex] = headerAndIndex.Index;
                                        ribbon.TabIndexToDisplayIndexMap[headerAndIndex.Index] = displayIndex;
                                        displayIndex++;
                                    }

                                    contextualTabHeaders.Remove(contextualTabGroupHeader);
                                }
                            }
                        }

                        RibbonContextualTabGroupsPanel contextualTabGroupsPanel = groupHeaderItemsControl.InternalItemsHost as RibbonContextualTabGroupsPanel;
                        if (contextualTabGroupsPanel != null)
                        {
                            contextualTabGroupsPanel.WaitingForMeasure = false;
                        }
                    }
                    else
                    {
                        // Tell the ContextualTabGroupsPanel that we are waiting on its Measure.
                        RibbonContextualTabGroupsPanel contextualTabGroupsPanel = groupHeaderItemsControl.InternalItemsHost as RibbonContextualTabGroupsPanel;
                        if (contextualTabGroupsPanel != null)
                        {
                            contextualTabGroupsPanel.WaitingForMeasure = true;
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets how much width is available in this Panel. 
        /// </summary>
        internal double SpaceAvailable
        {
            get;
            set;
        }

        #region Private Data

        private double _separatorOpacity;
        private const double _tabHeaderMinWidth = 30.0;
        private const double _desiredWidthEpsilon = 1e-10;
        Pen _separatorPen = null;

        #endregion

        #region IScrollInfo
        // Verifies scrolling data using the passed viewport and extent as newly computed values.
        // Checks the X/Y offset and coerces them into the range [0, Extent - ViewportSize]
        // If extent, viewport, or the newly coerced offsets are different than the existing offset,
        //   cachces are updated and InvalidateScrollInfo() is called.
        private void VerifyScrollData(double viewportWidth, double extentWidth)
        {
            bool fValid = true;

            if (Double.IsInfinity(viewportWidth))
            {
                viewportWidth = extentWidth;
            }

            double offsetX = CoerceOffset(ScrollData._offsetX, extentWidth, viewportWidth);

            fValid &= DoubleUtil.AreClose(viewportWidth, ScrollData._viewportWidth);
            fValid &= DoubleUtil.AreClose(extentWidth, ScrollData._extentWidth);
            fValid &= DoubleUtil.AreClose(ScrollData._offsetX, offsetX);

            ScrollData._viewportWidth = viewportWidth;
            ScrollData._extentWidth = extentWidth;
            ScrollData._offsetX = offsetX;

            if (!fValid)
            {
                if (ScrollOwner != null)
                {
                    ScrollOwner.InvalidateScrollInfo();
                }
            }
        }

        // Returns an offset coerced into the [0, Extent - Viewport] range.
        // Internal because it is also used by other Avalon ISI implementations (just to avoid code duplication).
        internal static double CoerceOffset(double offset, double extent, double viewport)
        {
            if (DoubleUtil.GreaterThan(offset, extent - viewport))
            {
                offset = extent - viewport;
            }

            if (DoubleUtil.LessThan(offset, 0))
            {
                offset = 0;
            }

            return offset;
        }

        public ScrollViewer ScrollOwner
        {
            get { return ScrollData._scrollOwner; }
            set
            {
                if (ScrollData._scrollOwner != value)
                {
                    if (Ribbon != null)
                    {
                        Ribbon.NotifyTabHeadersScrollOwnerChanged(ScrollData._scrollOwner, value);
                    }
                    ScrollData._scrollOwner = value;
                }
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            double newValue = ValidateInputOffset(offset, "HorizontalOffset");
            if (!DoubleUtil.AreClose(ScrollData._offsetX, newValue))
            {
                _scrollData._offsetX = newValue;
                InvalidateMeasure();
            }
        }

        public double ExtentWidth
        {
            get { return ScrollData._extentWidth; }
        }

        public double HorizontalOffset
        {
            get { return ScrollData._offsetX; }
        }

        public double ViewportWidth
        {
            get { return ScrollData._viewportWidth; }
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 16.0);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + 16.0);
        }

        // This is optimized for horizontal scrolling only
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (rectangle.IsEmpty
                || visual == null
                || visual == (Visual)this
                || !this.IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            // Compute the child's rect relative to (0,0) in our coordinate space.
            GeneralTransform childTransform = visual.TransformToAncestor(this);

            rectangle = childTransform.TransformBounds(rectangle);

            // Initialize the viewport
            Rect viewport = new Rect(HorizontalOffset, rectangle.Top, ViewportWidth, rectangle.Height);
            rectangle.X += viewport.X;

            // Compute the offsets required to minimally scroll the child maximally into view.
            double minX = ComputeScrollOffsetWithMinimalScroll(viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);

            // We have computed the scrolling offsets; scroll to them.
            double originalOffset = ScrollData._offsetX;
            SetHorizontalOffset(minX);

            if (!DoubleUtil.AreClose(originalOffset, ScrollData._offsetX))
            {
                OnScrollChange();
            }

            // Compute the visible rectangle of the child relative to the viewport.
            viewport.X = minX;
            rectangle.Intersect(viewport);

            rectangle.X -= viewport.X;

            // Return the rectangle
            return rectangle;
        }

        private void OnScrollChange()
        {
            if (ScrollOwner != null) { ScrollOwner.InvalidateScrollInfo(); }
        }

        internal static double ComputeScrollOffsetWithMinimalScroll(
            double topView,
            double bottomView,
            double topChild,
            double bottomChild)
        {
            // # CHILD POSITION       CHILD SIZE      SCROLL      REMEDY
            // 1 Above viewport       <= viewport     Down        Align top edge of child & viewport
            // 2 Above viewport       > viewport      Down        Align bottom edge of child & viewport
            // 3 Below viewport       <= viewport     Up          Align bottom edge of child & viewport
            // 4 Below viewport       > viewport      Up          Align top edge of child & viewport
            // 5 Entirely within viewport             NA          No scroll.
            // 6 Spanning viewport                    NA          No scroll.
            //
            // Note: "Above viewport" = childTop above viewportTop, childBottom above viewportBottom
            //       "Below viewport" = childTop below viewportTop, childBottom below viewportBottom
            // These child thus may overlap with the viewport, but will scroll the same direction
            bool fAbove = DoubleUtil.LessThan(topChild, topView) && DoubleUtil.LessThan(bottomChild, bottomView);
            bool fBelow = DoubleUtil.GreaterThan(bottomChild, bottomView) && DoubleUtil.GreaterThan(topChild, topView);
            bool fLarger = (bottomChild - topChild) > (bottomView - topView);

            // Handle Cases:  1 & 4 above
            if ((fAbove && !fLarger)
               || (fBelow && fLarger))
            {
                return topChild;
            }

            // Handle Cases: 2 & 3 above
            else if (fAbove || fBelow)
            {
                return bottomChild - (bottomView - topView);
            }

            // Handle cases: 5 & 6 above.
            return topView;
        }

        // Does not support other scrolling than LineLeft/LineRight
        public void MouseWheelDown()
        {
        }

        public void MouseWheelLeft()
        {
        }

        public void MouseWheelRight()
        {
        }

        public void MouseWheelUp()
        {
        }

        public void LineDown()
        {
        }

        public void LineUp()
        {
        }

        public void PageDown()
        {
        }

        public void PageLeft()
        {
        }

        public void PageRight()
        {
        }

        public void PageUp()
        {
        }

        public void SetVerticalOffset(double offset)
        {
        }

        public bool CanVerticallyScroll
        {
            get { return false; }
            set { }
        }

        public bool CanHorizontallyScroll
        {
            get { return true; }
            set { }
        }

        public double ExtentHeight
        {
            get { return 0.0; }
        }

        public double VerticalOffset
        {
            get { return 0.0; }
        }

        public double ViewportHeight
        {
            get { return 0.0; }
        }

        private ScrollData ScrollData
        {
            get
            {
                return _scrollData ?? (_scrollData = new ScrollData());
            }
        }

        private ScrollData _scrollData;

        internal static double ValidateInputOffset(double offset, string parameterName)
        {
            if (double.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return Math.Max(0.0, offset);
        }
        #endregion
    }
}
