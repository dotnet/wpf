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
    using System.Windows;
    using System.Windows.Controls;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#endif

    /// <summary>
    ///   The default panel for the RibbonGroup class.
    /// </summary>
    public class RibbonGroupItemsPanel : Panel, IProvideStarLayoutInfo, IContainsStarLayoutManager
    {
        #region Constructor

        public RibbonGroupItemsPanel()
        {
            Unloaded += new RoutedEventHandler(OnRibbonGroupItemsPanelUnloaded);
        }

        #endregion

        #region Protected Methods

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;

            RibbonHelper.InitializeStarLayoutManager(this);
            bool isStarLayoutPass = (iContainsStarLayoutManager.StarLayoutManager == null ? false : iContainsStarLayoutManager.StarLayoutManager.IsStarLayoutPass);

            if (!isStarLayoutPass)
            {
                desiredSize = NonStarPassMeasure(availableSize);
            }
            else
            {
                desiredSize = StarMeasurePass(availableSize);
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            double remainingHeightInColumn = finalSize.Height;
            double columnWidth = 0;
            double currentX = 0;
            double starLayoutCombinationCount = _starLayoutCombinations.Count;
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                Size childDesiredSize = child.DesiredSize;
                if (DoubleUtil.GreaterThan(childDesiredSize.Height, remainingHeightInColumn))
                {
                    currentX += columnWidth;
                    columnWidth = childDesiredSize.Width;
                    if (IsStarChild(child))
                    {
                        if (_childIndexToStarLayoutIndexMap.ContainsKey(i))
                        {
                            int starLayoutIndex = _childIndexToStarLayoutIndexMap[i];
                            if (starLayoutIndex < starLayoutCombinationCount)
                            {
                                StarLayoutInfo starLayoutInfo = _starLayoutCombinations[starLayoutIndex];
                                columnWidth = starLayoutInfo.AllocatedStarWidth;
                            }
                        }
                    }
                    child.Arrange(new Rect(currentX, 0, columnWidth, childDesiredSize.Height));
                    remainingHeightInColumn = Math.Max(0, finalSize.Height - childDesiredSize.Height);
                }
                else
                {
                    double arrangeWidth = child.DesiredSize.Width;
                    if (IsStarChild(child))
                    {
                        if (_childIndexToStarLayoutIndexMap.ContainsKey(i))
                        {
                            int starLayoutIndex = _childIndexToStarLayoutIndexMap[i];
                            if (starLayoutIndex < starLayoutCombinationCount)
                            {
                                if (CanChildStretch(child))
                                {
                                    // If the child's HorizontalAlignment is Left/Right, then
                                    // use its desired width as arrange width.
                                    StarLayoutInfo starLayoutInfo = _starLayoutCombinations[starLayoutIndex];
                                    arrangeWidth = starLayoutInfo.AllocatedStarWidth;
                                }
                            }
                        }
                    }
                    columnWidth = Math.Max(columnWidth, arrangeWidth);
                    child.Arrange(new Rect(currentX, (finalSize.Height - remainingHeightInColumn), arrangeWidth, childDesiredSize.Height));
                    remainingHeightInColumn -= childDesiredSize.Height;
                }
            }
            return finalSize;
        }

        #endregion

        #region Private Methods

        private void OnRibbonGroupItemsPanelUnloaded(object sender, RoutedEventArgs e)
        {
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;
            if (iContainsStarLayoutManager.StarLayoutManager != null)
            {
                iContainsStarLayoutManager.StarLayoutManager.UnregisterStarLayoutProvider(this);
                iContainsStarLayoutManager.StarLayoutManager = null;
            }
        }

        private static double GetStarChildMinWidth(FrameworkElement child, ref double maxStarColumnWidth)
        {
            if (child == null)
            {
                return 0;
            }
            if (CanChildStretch(child))
            {
                // If the child can stretch then use child.MaxWidth
                maxStarColumnWidth = Math.Max(maxStarColumnWidth, child.MaxWidth);
            }
            else
            {
                // If the child cannot stretch then use child.DesiredWidth for MaxWidth.
                maxStarColumnWidth = Math.Max(maxStarColumnWidth, Math.Min(child.MaxWidth, child.DesiredSize.Width));
            }
            return child.MinWidth;
        }

        private static bool IsStarChild(UIElement child)
        {
            double weight;
            return IsStarChild(child, out weight);
        }

        private static bool IsStarChild(UIElement child, out double weight)
        {
            weight = 0;
            RibbonControlSizeDefinition controlDef = RibbonControlService.GetControlSizeDefinition(child);
            if (controlDef != null)
            {
                weight = controlDef.Width.Value;
                return controlDef.Width.IsStar;
            }
            return false;
        }

        private void CreateStarLayoutCombination(Size constraint,
            double starWeight,
            double starMinWidth,
            double starMaxWidth,
            List<int> starChildIndices)
        {
            if (starChildIndices.Count > 0)
            {
                StarLayoutInfo starLayoutInfo = new StarLayoutInfo()
                {
                    RequestedStarMinWidth = starMinWidth,
                    RequestedStarMaxWidth = starMaxWidth,
                    RequestedStarWeight = starWeight
                };
                _starLayoutCombinations.Add(starLayoutInfo);

                UIElementCollection children = InternalChildren;
                int starLayoutIndex = _starLayoutCombinations.Count - 1;
                foreach (int starChildIndex in starChildIndices)
                {
                    _childIndexToStarLayoutIndexMap[starChildIndex] = starLayoutIndex;
                    children[starChildIndex].Measure(new Size(starLayoutInfo.RequestedStarMinWidth, constraint.Height));
                }
                starChildIndices.Clear();
            }
        }

        private Size NonStarPassMeasure(Size availableSize)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            double desiredWidth = 0;
            double desiredHeight = 0;
            double columnWidth = 0;
            double columnHeight = 0;
            double maxStarColumnWidth = 0;
            double starWeight = 0;
            List<int> starChildIndices = new List<int>();

            _starLayoutCombinations.Clear();
            _childIndexToStarLayoutIndexMap.Clear();

            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                child.Measure(availableSize);
                Size childDesiredSize = child.DesiredSize;

                if (DoubleUtil.GreaterThan(columnHeight + childDesiredSize.Height, availableSize.Height))
                {
                    // When switching to next column, create a star layout combination
                    // for previous column if needed.
                    CreateStarLayoutCombination(availableSize,
                        starWeight,
                        columnWidth,
                        maxStarColumnWidth,
                        starChildIndices);

                    starWeight = 0;
                    desiredHeight = Math.Min(Math.Max(desiredHeight, columnHeight), availableSize.Height);
                    columnHeight = childDesiredSize.Height;
                    desiredWidth += columnWidth;
                    double currentWeight = 0;
                    if (IsStarChild(child, out currentWeight))
                    {
                        starChildIndices.Add(i);
                        starWeight += currentWeight;
                        maxStarColumnWidth = 0;
                        columnWidth = GetStarChildMinWidth(child as FrameworkElement, ref maxStarColumnWidth);
                    }
                    else
                    {
                        columnWidth = childDesiredSize.Width;
                        maxStarColumnWidth = columnWidth;
                    }
                }
                else
                {
                    columnHeight += childDesiredSize.Height;
                    double currentWeight = 0;
                    if (IsStarChild(child, out currentWeight))
                    {
                        starWeight += currentWeight;
                        starChildIndices.Add(i);
                        columnWidth = Math.Max(columnWidth, GetStarChildMinWidth(child as FrameworkElement, ref maxStarColumnWidth));
                    }
                    else
                    {
                        columnWidth = Math.Max(columnWidth, childDesiredSize.Width);
                        maxStarColumnWidth = Math.Max(maxStarColumnWidth, columnWidth);
                    }
                }
            }
            desiredWidth += columnWidth;
            desiredHeight = Math.Min(Math.Max(desiredHeight, columnHeight), availableSize.Height);
            CreateStarLayoutCombination(availableSize,
                        starWeight,
                        columnWidth,
                        maxStarColumnWidth,
                        starChildIndices);
            return new Size(desiredWidth, desiredHeight);
        }

        private Size StarMeasurePass(Size availableSize)
        {
            Size originalDesiredSize = DesiredSize;
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            int starLayoutCombinationCount = _starLayoutCombinations.Count;
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (IsStarChild(child))
                {
                    if (_childIndexToStarLayoutIndexMap.ContainsKey(i))
                    {
                        int starLayoutIndex = _childIndexToStarLayoutIndexMap[i];
                        if (starLayoutIndex < starLayoutCombinationCount)
                        {
                            StarLayoutInfo starLayoutInfo = _starLayoutCombinations[starLayoutIndex];
                            child.Measure(new Size(starLayoutInfo.AllocatedStarWidth, availableSize.Height));
                        }
                    }
                }
            }

            double desiredWidth = originalDesiredSize.Width;
            AdjustDesiredWidthForStars(children, ref desiredWidth);

            return new Size(desiredWidth, originalDesiredSize.Height);
        }

        /// <summary>
        ///     Adjust the desired width for all the star
        ///     based children based upon their
        ///     allocated/desired width.
        /// </summary>
        private void AdjustDesiredWidthForStars(UIElementCollection children, ref double desiredWidth)
        {
            int oldStarLayoutIndex = -1;
            List<int> columnStarChildren = null;
            int childCount = children.Count;
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (IsStarChild(child) &&
                    _childIndexToStarLayoutIndexMap.ContainsKey(i))
                {
                    int starLayoutIndex = _childIndexToStarLayoutIndexMap[i];
                    if (starLayoutIndex != oldStarLayoutIndex)
                    {
                        // star layout index for all star children in a column
                        // should be same. Hence using that to determine the
                        // star based controls of a column.
                        AdjustDesiredWidthForStarColumn(children, columnStarChildren, oldStarLayoutIndex, ref desiredWidth);
                        if (oldStarLayoutIndex == -1)
                        {
                            AddItemToList(i, ref columnStarChildren);
                        }
                        oldStarLayoutIndex = starLayoutIndex;
                    }
                    else
                    {
                        AddItemToList(i, ref columnStarChildren);
                    }
                }
            }
            AdjustDesiredWidthForStarColumn(children,
                columnStarChildren,
                oldStarLayoutIndex,
                ref desiredWidth);
        }

        /// <summary>
        ///     Adjust the desired width for all the star
        ///     based children in a column based upon their
        ///     allocated/desired width.
        /// </summary>
        private void AdjustDesiredWidthForStarColumn(UIElementCollection children, List<int> columnStarChildren, int starLayoutIndex, ref double desiredWidth)
        {
            if (columnStarChildren != null && columnStarChildren.Count > 0 && starLayoutIndex >= 0)
            {
                bool foundStretchableStar = false;
                double columnDesiredWidth = 0;
                foreach (int columnChildIndex in columnStarChildren)
                {
                    UIElement child = children[columnChildIndex];
                    if (CanChildStretch(child))
                    {
                        foundStretchableStar = true;
                        break;
                    }
                    else
                    {
                        columnDesiredWidth = Math.Max(columnDesiredWidth, child.DesiredSize.Width);
                    }
                }
                if (foundStretchableStar)
                {
                    // if there are any non-item based star children
                    // then desired width should be equal to that of
                    // allocated width.
                    StarLayoutInfo starLayoutInfo = _starLayoutCombinations[starLayoutIndex];
                    desiredWidth += (starLayoutInfo.AllocatedStarWidth - starLayoutInfo.RequestedStarMinWidth);
                }
                else
                {
                    // if there are no non-item based star children
                    // then desired width should be equal to that of
                    // computed desired width.
                    StarLayoutInfo starLayoutInfo = _starLayoutCombinations[starLayoutIndex];
                    desiredWidth += Math.Max((columnDesiredWidth - starLayoutInfo.RequestedStarMinWidth), 0);
                }
                columnStarChildren.Clear();
            }
        }

        private static bool CanChildStretch(UIElement child)
        {
            // Gets the content child of RibbonControl and
            // determines if its horizontal alignment is
            // center or stretch. It is considered to be so
            // by default.
            RibbonControl ribbonControl = child as RibbonControl;
            if (ribbonControl != null)
            {
                UIElement contentChild = ribbonControl.ContentChild;
                if (contentChild != null)
                {
                    HorizontalAlignment childAlignment = (HorizontalAlignment)contentChild.GetValue(HorizontalAlignmentProperty);
                    if (childAlignment == HorizontalAlignment.Left ||
                        childAlignment == HorizontalAlignment.Right)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///     Helper method to instantiate the list
        ///     if needed and then add the item to it.
        /// </summary>
        private static void AddItemToList<T>(T item, ref List<T> list)
        {
            if (list == null)
            {
                list = new List<T>();
            }
            list.Add(item);
        }

        #endregion

        #region IProvideStarLayoutInfo Members

        public IEnumerable<StarLayoutInfo> StarLayoutCombinations
        {
            get { return _starLayoutCombinations; }
        }

        public void OnStarSizeAllocationCompleted()
        {
            TreeHelper.InvalidateMeasureForVisualAncestorPath(this, RibbonHelper.IsISupportStarLayout);
        }

        public UIElement TargetElement
        {
            get
            {
                FrameworkElement itemsPresenter = TemplatedParent as FrameworkElement;
                if (itemsPresenter != null)
                {
                    return itemsPresenter.TemplatedParent as UIElement;
                }

                return null;
            }
        }

        public void OnInitializeLayout()
        {
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;
            if (iContainsStarLayoutManager.StarLayoutManager != null && !iContainsStarLayoutManager.StarLayoutManager.IsStarLayoutPass)
            {
                TreeHelper.InvalidateMeasureForVisualAncestorPath(this, RibbonHelper.IsISupportStarLayout);
            }
        }

        #endregion

        #region IContainsStarLayoutManager Members

        ISupportStarLayout IContainsStarLayoutManager.StarLayoutManager
        {
            get;
            set;
        }

        #endregion

        #region Private Data

        List<StarLayoutInfo> _starLayoutCombinations = new List<StarLayoutInfo>();
        Dictionary<int, int> _childIndexToStarLayoutIndexMap = new Dictionary<int, int>();

        #endregion
    }
}
