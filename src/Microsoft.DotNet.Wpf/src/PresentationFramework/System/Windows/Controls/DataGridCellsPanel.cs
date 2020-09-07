// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Panel that lays out both cells and column headers. This stacks cells in the horizontal direction and communicates with the
    ///     relevant DataGridColumn to ensure all rows give cells in a given column the same size.
    ///     It is hardcoded against DataGridCell and DataGridColumnHeader.
    /// </summary>
    public class DataGridCellsPanel : VirtualizingPanel
    {
        #region Constructors

        static DataGridCellsPanel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(DataGridCellsPanel), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
        }

        public DataGridCellsPanel()
        {
            IsVirtualizing = false;
            InRecyclingMode = false;
        }

        #endregion

        #region Measure

        /// <summary>
        ///     Measure
        ///
        ///     The logic is to see determinition of realized blocks is needed and do it.
        ///     If not, iterate over the realized block list and for each block generate the
        ///     children and measure them.
        /// </summary>
        /// <param name="constraint">Size constraint</param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size measureSize = new Size();

            DetermineVirtualizationState();

            // Acessing this property will initilize things appropriately,
            // both for virtualization and non-virtualization cases
            EnsureRealizedChildren();
            IList children = RealizedChildren;

            if (RebuildRealizedColumnsBlockList)
            {
                measureSize = DetermineRealizedColumnsBlockList(constraint);
            }
            else
            {
                // This is the case of remaining cells panels which directly use the list
                // in parent rows presenter for virtualization, rather than doing the
                // computation themselves.
                measureSize = GenerateAndMeasureChildrenForRealizedColumns(constraint);
            }

            // Disconnect any left over recycled children
            if (IsVirtualizing && InRecyclingMode)
            {
                DisconnectRecycledContainers();
            }

            if (!DoubleUtil.AreClose(this.DesiredSize, measureSize) && MeasureDuringArrange)
            {
                // This makes sure that the ItemsPresenter and the DatagridCellsPresenter is invalidated even if this is an arrange pass.
                this.ParentPresenter.InvalidateMeasure();
                UIElement parent =  VisualTreeHelper.GetParent(this) as UIElement;
                if (parent != null)
                    parent.InvalidateMeasure();
            }

            return measureSize;
        }

        /// <summary>
        ///     Method which measures a given child based on its column width
        ///
        ///     For auto kind columns it may actually end up measuring twice
        ///     once with positive infinity to determine its actual desired width
        ///     and then again with the available space constraints
        /// </summary>
        private static void MeasureChild(UIElement child, Size constraint)
        {
            IProvideDataGridColumn cell = child as IProvideDataGridColumn;
            bool isColumnHeader = (child is DataGridColumnHeader);
            Size childMeasureConstraint = new Size(double.PositiveInfinity, constraint.Height);

            double desiredWidth = 0.0;
            bool remeasure = false;

            // Allow the column to affect the constraint.
            if (cell != null)
            {
                // For auto kind columns measure with infinity to find the actual desired width of the cell.
                DataGridColumn column = cell.Column;
                DataGridLength width = column.Width;
                if (width.IsAuto ||
                    (width.IsSizeToHeader && isColumnHeader) ||
                    (width.IsSizeToCells && !isColumnHeader))
                {
                    child.Measure(childMeasureConstraint);
                    desiredWidth = child.DesiredSize.Width;
                    remeasure = true;
                }

                childMeasureConstraint.Width = column.GetConstraintWidth(isColumnHeader);
            }

            if (DoubleUtil.AreClose(desiredWidth, 0.0))
            {
                child.Measure(childMeasureConstraint);
            }

            Size childDesiredSize = child.DesiredSize;

            if (cell != null)
            {
                DataGridColumn column = cell.Column;

                // Allow the column to process the desired size
                column.UpdateDesiredWidthForAutoColumn(
                    isColumnHeader,
                    DoubleUtil.AreClose(desiredWidth, 0.0) ? childDesiredSize.Width : desiredWidth);

                // For auto kind columns measure again with display value if
                // the desired width is greater than display value.
                DataGridLength width = column.Width;
                if (remeasure &&
                    !DoubleUtil.IsNaN(width.DisplayValue) &&
                    DoubleUtil.GreaterThan(desiredWidth, width.DisplayValue))
                {
                    childMeasureConstraint.Width = width.DisplayValue;
                    child.Measure(childMeasureConstraint);
                }
            }
        }

        /// <summary>
        ///     Generates children and measures them based on RealizedColumnList of
        ///     ancestor rows presenter.
        /// </summary>
        private Size GenerateAndMeasureChildrenForRealizedColumns(Size constraint)
        {
            double measureWidth = 0.0;
            double measureHeight = 0.0;
            DataGrid parentDataGrid = ParentDataGrid;
            double averageColumnWidth = parentDataGrid.InternalColumns.AverageColumnWidth;
            IItemContainerGenerator generator = ItemContainerGenerator;

            List<RealizedColumnsBlock> blockList = RealizedColumnsBlockList;

            Debug.Assert(blockList != null, "RealizedColumnsBlockList shouldn't be null at this point.");

            // Virtualize the children which are not necessary
            VirtualizeChildren(blockList, generator);

            // Realize the required children
            if (blockList.Count > 0)
            {
                for (int i = 0, count = blockList.Count; i < count; i++)
                {
                    RealizedColumnsBlock rcb = blockList[i];
                    Size blockMeasureSize = GenerateChildren(
                        generator,
                        rcb.StartIndex,
                        rcb.EndIndex,
                        constraint);

                    measureWidth += blockMeasureSize.Width;
                    measureHeight = Math.Max(measureHeight, blockMeasureSize.Height);

                    if (i != count - 1)
                    {
                        RealizedColumnsBlock nextRcb = blockList[i + 1];
                        measureWidth += GetColumnEstimatedMeasureWidthSum(rcb.EndIndex + 1, nextRcb.StartIndex - 1, averageColumnWidth);
                    }
                }

                measureWidth += GetColumnEstimatedMeasureWidthSum(0, blockList[0].StartIndex - 1, averageColumnWidth);
                measureWidth += GetColumnEstimatedMeasureWidthSum(blockList[blockList.Count - 1].EndIndex + 1, parentDataGrid.Columns.Count - 1, averageColumnWidth);
            }
            else
            {
                measureWidth = 0.0;
            }

            return new Size(measureWidth, measureHeight);
        }

        /// <summary>
        ///     Method which determines the realized columns list and
        ///     stores it in ancestor rowpresenter which is to be used
        ///     by other cellpanels to virtualize without recomputation.
        ///     Simultaneously measures the children of this panel.
        ///
        ///     If the datagrid has star columns, then all cells for all
        ///     realized rows are generated.
        ///
        ///     For remaining case the logic is to iterate over columns of
        ///     datagrid in DisplayIndex order, as if one is actually arranging
        ///     them, to determine which of them actually fall in viewport.
        /// </summary>
        private Size DetermineRealizedColumnsBlockList(Size constraint)
        {
            List<int> realizedColumnIndices = new List<int>();
            List<int> realizedColumnDisplayIndices = new List<int>();
            Size measureSize = new Size();

            DataGrid parentDataGrid = ParentDataGrid;
            if (parentDataGrid == null)
            {
                return measureSize;
            }

            double horizontalOffset = parentDataGrid.HorizontalScrollOffset;
            double cellsPanelOffset = parentDataGrid.CellsPanelHorizontalOffset;    // indicates cellspanel's offset in a row
            double nextFrozenCellStart = horizontalOffset;                // indicates the start position for next frozen cell
            double nextNonFrozenCellStart = -cellsPanelOffset;            // indicates the start position for next non-frozen cell
            double viewportStartX = horizontalOffset - cellsPanelOffset;  // indicates the start of viewport with respect to coordinate system of cell panel
            int firstVisibleNonFrozenDisplayIndex = -1;
            int lastVisibleNonFrozenDisplayIndex = -1;

            double totalAvailableSpace = GetViewportWidth() - cellsPanelOffset;
            double allocatedSpace = 0.0;

            if (IsVirtualizing && DoubleUtil.LessThan(totalAvailableSpace, 0.0))
            {
                return measureSize;
            }

            bool hasStarColumns = parentDataGrid.InternalColumns.HasVisibleStarColumns;
            double averageColumnWidth = parentDataGrid.InternalColumns.AverageColumnWidth;
            bool invalidAverage = DoubleUtil.AreClose(averageColumnWidth, 0.0);
            bool notVirtualizing = !IsVirtualizing;
            bool generateAll = invalidAverage || hasStarColumns || notVirtualizing;
            int frozenColumnCount = parentDataGrid.FrozenColumnCount;
            int previousColumnIndex = -1;

            bool redeterminationNeeded = false;
            Size childSize;

            IItemContainerGenerator generator = ItemContainerGenerator;
            IDisposable generatorState = null;
            int childIndex = 0;

            try
            {
                for (int i = 0, count = parentDataGrid.Columns.Count; i < count; i++)
                {
                    DataGridColumn column = parentDataGrid.ColumnFromDisplayIndex(i);

                    if (!column.IsVisible)
                    {
                        continue;
                    }

                    // Dispose the generator state if the child generation is not in
                    // sequence either because of gaps in childs to be generated or
                    // due to mismatch in the order of column index and displayindex
                    int columnIndex = parentDataGrid.ColumnIndexFromDisplayIndex(i);
                    if (columnIndex != childIndex || previousColumnIndex != (columnIndex - 1))
                    {
                        childIndex = columnIndex;
                        if (generatorState != null)
                        {
                            generatorState.Dispose();
                            generatorState = null;
                        }
                    }
                    previousColumnIndex = columnIndex;

                    // Generate the child if the all the children are to be generated,
                    // initialize the child size.
                    if (generateAll)
                    {
                        if (null == GenerateChild(generator, constraint, column, ref generatorState, ref childIndex, out childSize))
                        {
                            break;
                        }
                    }
                    else
                    {
                        childSize = new Size(GetColumnEstimatedMeasureWidth(column, averageColumnWidth), 0.0);
                    }

                    if (notVirtualizing || hasStarColumns || DoubleUtil.LessThan(allocatedSpace, totalAvailableSpace))
                    {
                        // Frozen children are realized provided they are in viewport
                        if (i < frozenColumnCount)
                        {
                            if (!generateAll &&
                                null == GenerateChild(generator, constraint, column, ref generatorState, ref childIndex, out childSize))
                            {
                                break;
                            }

                            realizedColumnIndices.Add(columnIndex);
                            realizedColumnDisplayIndices.Add(i);
                            allocatedSpace += childSize.Width;
                            nextFrozenCellStart += childSize.Width;
                        }
                        else
                        {
                            if (DoubleUtil.LessThanOrClose(nextNonFrozenCellStart, viewportStartX))
                            {
                                // Non-Frozen children to the left of viewport are not realized,
                                // unless we are dealing with star columns.
                                if (DoubleUtil.LessThanOrClose(nextNonFrozenCellStart + childSize.Width, viewportStartX))
                                {
                                    if (generateAll)
                                    {
                                        if (notVirtualizing || hasStarColumns)
                                        {
                                            realizedColumnIndices.Add(columnIndex);
                                            realizedColumnDisplayIndices.Add(i);
                                        }
                                        else if (invalidAverage)
                                        {
                                            redeterminationNeeded = true;
                                        }
                                    }
                                    else if (generatorState != null)
                                    {
                                        generatorState.Dispose();
                                        generatorState = null;
                                    }

                                    nextNonFrozenCellStart += childSize.Width;
                                }
                                else
                                {
                                    // First visible non frozen child is realized
                                    if (!generateAll &&
                                        null == GenerateChild(generator, constraint, column, ref generatorState, ref childIndex, out childSize))
                                    {
                                        break;
                                    }

                                    double cellChoppedWidth = viewportStartX - nextNonFrozenCellStart;
                                    if (DoubleUtil.AreClose(cellChoppedWidth, 0.0))
                                    {
                                        nextNonFrozenCellStart = nextFrozenCellStart + childSize.Width;
                                        allocatedSpace += childSize.Width;
                                    }
                                    else
                                    {
                                        double clipWidth = childSize.Width - cellChoppedWidth;
                                        nextNonFrozenCellStart = nextFrozenCellStart + clipWidth;
                                        allocatedSpace += clipWidth;
                                    }

                                    realizedColumnIndices.Add(columnIndex);
                                    realizedColumnDisplayIndices.Add(i);
                                    firstVisibleNonFrozenDisplayIndex = i;
                                    lastVisibleNonFrozenDisplayIndex = i;
                                }
                            }
                            else
                            {
                                // All the remaining non-frozen children are realized provided they are in viewport
                                if (!generateAll &&
                                    null == GenerateChild(generator, constraint, column, ref generatorState, ref childIndex, out childSize))
                                {
                                    break;
                                }

                                if (firstVisibleNonFrozenDisplayIndex < 0)
                                {
                                    firstVisibleNonFrozenDisplayIndex = i;
                                }

                                lastVisibleNonFrozenDisplayIndex = i;
                                nextNonFrozenCellStart += childSize.Width;
                                allocatedSpace += childSize.Width;
                                realizedColumnIndices.Add(columnIndex);
                                realizedColumnDisplayIndices.Add(i);
                            }
                        }
                    }

                    measureSize.Width += childSize.Width;
                    measureSize.Height = Math.Max(measureSize.Height, childSize.Height);
                }
            }
            finally
            {
                if (generatorState != null)
                {
                    generatorState.Dispose();
                    generatorState = null;
                }
            }

            // If we are virtualizing and datagrid doesnt have any star columns
            // then ensure the focus trail for navigational purposes.
            if (!hasStarColumns && !notVirtualizing)
            {
                bool isColumnHeader = ParentPresenter is DataGridColumnHeadersPresenter;
                if (isColumnHeader)
                {
                    Size headerSize = EnsureAtleastOneHeader(generator, constraint, realizedColumnIndices, realizedColumnDisplayIndices);
                    measureSize.Height = Math.Max(measureSize.Height, headerSize.Height);
                    redeterminationNeeded = true;
                }
                else
                {
                    EnsureFocusTrail(realizedColumnIndices, realizedColumnDisplayIndices, firstVisibleNonFrozenDisplayIndex, lastVisibleNonFrozenDisplayIndex, constraint);
                }
            }

            UpdateRealizedBlockLists(realizedColumnIndices, realizedColumnDisplayIndices, redeterminationNeeded);

            // Virtualize the children which are determined to be unused
            VirtualizeChildren(RealizedColumnsBlockList, generator);

            return measureSize;
        }

        private void UpdateRealizedBlockLists(
            List<int> realizedColumnIndices,
            List<int> realizedColumnDisplayIndices,
            bool redeterminationNeeded)
        {
            realizedColumnIndices.Sort();

            // PERF: An option here is to apply some heuristics and add some indices
            // to optimize the generation by avoiding multiple generation sequences
            // with in a single generation sequence

            // Combine the realized indices into blocks, so that it is easy to use for later purposes
            RealizedColumnsBlockList = BuildRealizedColumnsBlockList(realizedColumnIndices);

            // PERF: Uncomment the statement below if needed once the heuristics
            // mentioned above are implemented
            // realizedColumnDisplayIndices.Sort();

            // Combine the realized disply indices into blocks, so that it is easy to use for later purposes
            RealizedColumnsDisplayIndexBlockList = BuildRealizedColumnsBlockList(realizedColumnDisplayIndices);

            if (!redeterminationNeeded)
            {
                RebuildRealizedColumnsBlockList = false;
            }
        }

        /// <summary>
        ///     Helper method which creates a list of RealizedColumnsBlock struct
        ///     out of a list on integer indices.
        /// </summary>
        private static List<RealizedColumnsBlock> BuildRealizedColumnsBlockList(List<int> indexList)
        {
            List<RealizedColumnsBlock> resultList = new List<RealizedColumnsBlock>();
            if (indexList.Count == 1)
            {
                resultList.Add(new RealizedColumnsBlock(indexList[0], indexList[0], 0));
            }
            else if (indexList.Count > 0)
            {
                int startIndex = indexList[0];
                for (int i = 1, count = indexList.Count; i < count; i++)
                {
                    if (indexList[i] != indexList[i - 1] + 1)
                    {
                        if (resultList.Count == 0)
                        {
                            resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i - 1], 0));
                        }
                        else
                        {
                            RealizedColumnsBlock lastRealizedColumnsBlock = resultList[resultList.Count - 1];
                            int startIndexOffset = lastRealizedColumnsBlock.StartIndexOffset + lastRealizedColumnsBlock.EndIndex - lastRealizedColumnsBlock.StartIndex + 1;
                            resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i - 1], startIndexOffset));
                        }

                        startIndex = indexList[i];
                    }

                    if (i == count - 1)
                    {
                        if (resultList.Count == 0)
                        {
                            resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i], 0));
                        }
                        else
                        {
                            RealizedColumnsBlock lastRealizedColumnsBlock = resultList[resultList.Count - 1];
                            int startIndexOffset = lastRealizedColumnsBlock.StartIndexOffset + lastRealizedColumnsBlock.EndIndex - lastRealizedColumnsBlock.StartIndex + 1;
                            resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i], startIndexOffset));
                        }
                    }
                }
            }

            return resultList;
        }

        /// <summary>
        ///     Helper method to build generator position out to index
        /// </summary>
        private static GeneratorPosition IndexToGeneratorPositionForStart(IItemContainerGenerator generator, int index, out int childIndex)
        {
            GeneratorPosition position = (generator != null) ? generator.GeneratorPositionFromIndex(index) : new GeneratorPosition(-1, index + 1);

            // Determine the position in the children collection for the first
            // generated container.  This assumes that generator.StartAt will be called
            // with direction=Forward and allowStartAtRealizedItem=true.
            childIndex = (position.Offset == 0) ? position.Index : position.Index + 1;

            return position;
        }

        /// <summary>
        ///     Helper method which generates and measures a
        ///     child of given index
        /// </summary>
        private UIElement GenerateChild(
            IItemContainerGenerator generator,
            Size constraint,
            DataGridColumn column,
            ref IDisposable generatorState,
            ref int childIndex,
            out Size childSize)
        {
            if (generatorState == null)
            {
                generatorState = generator.StartAt(IndexToGeneratorPositionForStart(generator, childIndex, out childIndex), GeneratorDirection.Forward, true);
            }

            return GenerateChild(generator, constraint, column, ref childIndex, out childSize);
        }

        /// <summary>
        ///     Helper method which generates and measures a
        ///     child of given index
        /// </summary>
        private UIElement GenerateChild(
            IItemContainerGenerator generator,
            Size constraint,
            DataGridColumn column,
            ref int childIndex,
            out Size childSize)
        {
            bool newlyRealized;
            UIElement child = generator.GenerateNext(out newlyRealized) as UIElement;
            if (child == null)
            {
                childSize = new Size();
                return null;
            }

            AddContainerFromGenerator(childIndex, child, newlyRealized);
            childIndex++;

            MeasureChild(child, constraint);

            DataGridLength width = column.Width;
            childSize = child.DesiredSize;
            if (!DoubleUtil.IsNaN(width.DisplayValue))
            {
                childSize = new Size(width.DisplayValue, childSize.Height);
            }

            return child;
        }

        /// <summary>
        ///     Helper method which generates and measures children of
        ///     a given block of indices
        /// </summary>
        private Size GenerateChildren(
            IItemContainerGenerator generator,
            int startIndex,
            int endIndex,
            Size constraint)
        {
            double measureWidth = 0.0;
            double measureHeight = 0.0;
            int childIndex;
            GeneratorPosition startPos = IndexToGeneratorPositionForStart(generator, startIndex, out childIndex);
            DataGrid parentDataGrid = ParentDataGrid;
            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                Size childSize;
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (!parentDataGrid.Columns[i].IsVisible)
                    {
                        continue;
                    }

                    if (null == GenerateChild(generator, constraint, parentDataGrid.Columns[i], ref childIndex, out childSize))
                    {
                        return new Size(measureWidth, measureHeight);
                    }

                    measureWidth += childSize.Width;
                    measureHeight = Math.Max(measureHeight, childSize.Height);
                }
            }

            return new Size(measureWidth, measureHeight);
        }

        /// <summary>
        ///     Method which adds the given container at the given index
        /// </summary>
        private void AddContainerFromGenerator(int childIndex, UIElement child, bool newlyRealized)
        {
            if (!newlyRealized)
            {
                // Container is either realized or recycled.  If it's realized do nothing; it already exists in the visual
                // tree in the proper place.
                if (InRecyclingMode)
                {
                    IList children = RealizedChildren;

                    if (childIndex >= children.Count || !(children[childIndex] == child))
                    {
                        Debug.Assert(!children.Contains(child), "we incorrectly identified a recycled container");

                        // We have a recycled container (if it was a realized container it would have been returned in the
                        // proper location).  Note also that recycled containers are NOT in the _realizedChildren list.
                        InsertRecycledContainer(childIndex, child);
                        child.Measure(new Size());
                    }
                    else
                    {
                        // previously realized child, so do nothing.
                    }
                }
                else
                {
                    // Not recycling; realized container
                    Debug.Assert(child == InternalChildren[childIndex], "Wrong child was generated");
                }
            }
            else
            {
                InsertNewContainer(childIndex, child);
            }
        }

        /// <summary>
        ///     Inserts a recycled container in the visual tree
        /// </summary>
        private void InsertRecycledContainer(int childIndex, UIElement container)
        {
            InsertContainer(childIndex, container, true);
        }

        /// <summary>
        ///     Inserts a new container in the visual tree
        /// </summary>
        private void InsertNewContainer(int childIndex, UIElement container)
        {
            InsertContainer(childIndex, container, false);
        }

        /// <summary>
        ///     Inserts a container into the Children collection.  The container is either new or recycled.
        /// </summary>
        private void InsertContainer(int childIndex, UIElement container, bool isRecycled)
        {
            Debug.Assert(container != null, "Null container was generated");

            UIElementCollection children = InternalChildren;

            // Find the index in the Children collection where we hope to insert the container.
            // This is done by looking up the index of the container BEFORE the one we hope to insert.
            //
            // We have to do it this way because there could be recycled containers between the container we're looking for and the one before it.
            // By finding the index before the place we want to insert and adding one, we ensure that we'll insert the new container in the
            // proper location.
            //
            // In recycling mode childIndex is the index in the _realizedChildren list, not the index in the
            // Children collection.  We have to convert the index; we'll call the index in the Children collection
            // the visualTreeIndex.
            int visualTreeIndex = 0;

            if (childIndex > 0)
            {
                visualTreeIndex = ChildIndexFromRealizedIndex(childIndex - 1);
                visualTreeIndex++;
            }

            if (isRecycled && visualTreeIndex < children.Count && children[visualTreeIndex] == container)
            {
                // Don't insert if a recycled container is in the proper place already
            }
            else
            {
                if (visualTreeIndex < children.Count)
                {
                    int insertIndex = visualTreeIndex;
                    if (isRecycled && VisualTreeHelper.GetParent(container) != null)
                    {
                        // If the container is recycled we have to remove it from its place in the visual tree and
                        // insert it in the proper location.   We cant use an internal Move api, so we are removing
                        // and inserting the container
                        Debug.Assert(children[visualTreeIndex] != null, "MoveVisualChild interprets a null destination as 'move to end'");
                        int containerIndex = children.IndexOf(container);
                        RemoveInternalChildRange(containerIndex, 1);
                        if (containerIndex < insertIndex)
                        {
                            insertIndex--;
                        }

                        InsertInternalChild(insertIndex, container);
                    }
                    else
                    {
                        InsertInternalChild(insertIndex, container);
                    }
                }
                else
                {
                    if (isRecycled && VisualTreeHelper.GetParent(container) != null)
                    {
                        // Recycled container is still in the tree; move it to the end
                        int originalIndex = children.IndexOf(container);
                        RemoveInternalChildRange(originalIndex, 1);
                        AddInternalChild(container);
                    }
                    else
                    {
                        AddInternalChild(container);
                    }
                }
            }

            // Keep realizedChildren in sync w/ the visual tree.
            if (IsVirtualizing && InRecyclingMode)
            {
                _realizedChildren.Insert(childIndex, container);
            }

            ItemContainerGenerator.PrepareItemContainer(container);
        }

        /// <summary>
        ///     Takes an index from the realized list and returns the corresponding index in the Children collection
        /// </summary>
        private int ChildIndexFromRealizedIndex(int realizedChildIndex)
        {
            // If we're not recycling containers then we're not using a realizedChild index and no translation is necessary
            if (IsVirtualizing && InRecyclingMode)
            {
                if (realizedChildIndex < _realizedChildren.Count)
                {
                    UIElement child = _realizedChildren[realizedChildIndex];
                    UIElementCollection children = InternalChildren;

                    for (int i = realizedChildIndex; i < children.Count; i++)
                    {
                        if (children[i] == child)
                        {
                            return i;
                        }
                    }

                    Debug.Assert(false, "We should have found a child");
                }
            }

            return realizedChildIndex;
        }

        /// <summary>
        ///     Helper method which determines if the given in index
        ///     falls in the given block or in the next block
        /// </summary>
        private static bool InBlockOrNextBlock(List<RealizedColumnsBlock> blockList, int index, ref int blockIndex, ref RealizedColumnsBlock block, out bool pastLastBlock)
        {
            pastLastBlock = false;
            bool exists = true;
            if (index < block.StartIndex)
            {
                exists = false;
            }
            else if (index > block.EndIndex)
            {
                if (blockIndex == blockList.Count - 1)
                {
                    blockIndex++;
                    pastLastBlock = true;
                    exists = false;
                }
                else
                {
                    block = blockList[++blockIndex];
                    if (index < block.StartIndex ||
                        index > block.EndIndex)
                    {
                        exists = false;
                    }
                }
            }

            return exists;
        }

        /// <summary>
        ///     Method which ensures that atleast one column
        ///     header is generated. Such a generation would
        ///     help in determination of the height.
        /// </summary>
        private Size EnsureAtleastOneHeader(IItemContainerGenerator generator,
            Size constraint,
            List<int> realizedColumnIndices,
            List<int> realizedColumnDisplayIndices)
        {
            DataGrid parentDataGrid = ParentDataGrid;
            int columnCount = parentDataGrid.Columns.Count;
            Size childSize = new Size();
            if (RealizedChildren.Count == 0 && columnCount > 0)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    DataGridColumn column = parentDataGrid.Columns[i];
                    if (column.IsVisible)
                    {
                        int childIndex = i;
                        using (generator.StartAt(IndexToGeneratorPositionForStart(generator, childIndex, out childIndex), GeneratorDirection.Forward, true))
                        {
                            UIElement child = GenerateChild(generator, constraint, column, ref childIndex, out childSize);
                            if (child != null)
                            {
                                int displayIndexListIterator = 0;
                                AddToIndicesListIfNeeded(
                                    realizedColumnIndices,
                                    realizedColumnDisplayIndices,
                                    i,
                                    column.DisplayIndex,
                                    ref displayIndexListIterator);
                                return childSize;
                            }
                        }
                    }
                }
            }
            return childSize;
        }

        /// <summary>
        ///     Method which ensures that all the appropriate
        ///     focus trail cells are realized such that tabbing
        ///     works.
        /// </summary>
        private void EnsureFocusTrail(
            List<int> realizedColumnIndices,
            List<int> realizedColumnDisplayIndices,
            int firstVisibleNonFrozenDisplayIndex,
            int lastVisibleNonFrozenDisplayIndex,
            Size constraint)
        {
            if (firstVisibleNonFrozenDisplayIndex < 0)
            {
                // Non frozen columns can never be brought into viewport.
                // Hence tabbing is supported only among visible frozen cells
                // which should already be realized.
                return;
            }

            int frozenColumnCount = ParentDataGrid.FrozenColumnCount;
            int columnCount = Columns.Count;
            ItemsControl parentPresenter = ParentPresenter;
            if (parentPresenter == null)
            {
                return;
            }

            ItemContainerGenerator generator = parentPresenter.ItemContainerGenerator;
            int displayIndexListIterator = 0;
            int previousFocusTrailIndex = -1;

            // Realizing the child for first visible column
            for (int i = 0; i < firstVisibleNonFrozenDisplayIndex; i++)
            {
                if (GenerateChildForFocusTrail(generator, realizedColumnIndices, realizedColumnDisplayIndices, constraint, i, ref displayIndexListIterator))
                {
                    previousFocusTrailIndex = i;
                    break;
                }
            }

            // Realizing the child for first non-frozen column
            if (previousFocusTrailIndex < frozenColumnCount)
            {
                for (int i = frozenColumnCount; i < columnCount; i++)
                {
                    if (GenerateChildForFocusTrail(generator, realizedColumnIndices, realizedColumnDisplayIndices, constraint, i, ref displayIndexListIterator))
                    {
                        previousFocusTrailIndex = i;
                        break;
                    }
                }
            }

            // Realizing the preceding child of first visible non-frozen column
            for (int i = firstVisibleNonFrozenDisplayIndex - 1; i > previousFocusTrailIndex; i--)
            {
                if (GenerateChildForFocusTrail(generator, realizedColumnIndices, realizedColumnDisplayIndices, constraint, i, ref displayIndexListIterator))
                {
                    previousFocusTrailIndex = i;
                    break;
                }
            }

            // Realizing the suceeding child of last visible non-frozen column
            for (int i = lastVisibleNonFrozenDisplayIndex + 1; i < columnCount; i++)
            {
                if (GenerateChildForFocusTrail(generator, realizedColumnIndices, realizedColumnDisplayIndices, constraint, i, ref displayIndexListIterator))
                {
                    previousFocusTrailIndex = i;
                    break;
                }
            }

            // Realizing the child for last column
            for (int i = columnCount - 1; i > previousFocusTrailIndex; i--)
            {
                if (GenerateChildForFocusTrail(generator, realizedColumnIndices, realizedColumnDisplayIndices, constraint, i, ref displayIndexListIterator))
                {
                    break;
                }
            }

            return;
        }

        /// <summary>
        ///     Method which generates the focus trail cell
        ///     if it is not already generated and adds it to
        ///     the block lists appropriately.
        /// </summary>
        private bool GenerateChildForFocusTrail(
            ItemContainerGenerator generator,
            List<int> realizedColumnIndices,
            List<int> realizedColumnDisplayIndices,
            Size constraint,
            int displayIndex,
            ref int displayIndexListIterator)
        {
            DataGrid dataGrid = ParentDataGrid;
            DataGridColumn column = dataGrid.ColumnFromDisplayIndex(displayIndex);
            if (column.IsVisible)
            {
                int columnIndex = dataGrid.ColumnIndexFromDisplayIndex(displayIndex);

                UIElement child = generator.ContainerFromIndex(columnIndex) as UIElement;
                if (child == null)
                {
                    int childIndex = columnIndex;
                    Size childSize;
                    using (((IItemContainerGenerator)generator).StartAt(IndexToGeneratorPositionForStart(generator, childIndex, out childIndex), GeneratorDirection.Forward, true))
                    {
                        child = GenerateChild(generator, constraint, column, ref childIndex, out childSize);
                    }
                }

                if (child != null && DataGridHelper.TreeHasFocusAndTabStop(child))
                {
                    AddToIndicesListIfNeeded(
                        realizedColumnIndices,
                        realizedColumnDisplayIndices,
                        columnIndex,
                        displayIndex,
                        ref displayIndexListIterator);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Helper method which adds the generated
        ///     display index to the the block lists if not
        ///     already present.
        /// </summary>
        private static void AddToIndicesListIfNeeded(
            List<int> realizedColumnIndices,
            List<int> realizedColumnDisplayIndices,
            int columnIndex,
            int displayIndex,
            ref int displayIndexListIterator)
        {
            for (int count = realizedColumnDisplayIndices.Count; displayIndexListIterator < count; displayIndexListIterator++)
            {
                if (realizedColumnDisplayIndices[displayIndexListIterator] == displayIndex)
                {
                    return;
                }
                else if (realizedColumnDisplayIndices[displayIndexListIterator] > displayIndex)
                {
                    realizedColumnDisplayIndices.Insert(displayIndexListIterator, displayIndex);
                    realizedColumnIndices.Add(columnIndex);
                    return;
                }
            }

            realizedColumnIndices.Add(columnIndex);
            realizedColumnDisplayIndices.Add(displayIndex);
            return;
        }

        /// <summary>
        ///     Method which virtualizes the children which are determined to be unused.
        ///     Eligible candidates for virtualization are those which are not in block list.
        ///     Some exceptions to the criterion are the cells which are in edit mode, or
        ///     if item is its own container.
        /// </summary>
        private void VirtualizeChildren(List<RealizedColumnsBlock> blockList, IItemContainerGenerator generator)
        {
            DataGrid parentDataGrid = ParentDataGrid;
            ObservableCollection<DataGridColumn> columns = parentDataGrid.Columns;
            int columnCount = columns.Count;
            int columnIterator = 0;
            IList children = RealizedChildren;
            int childrenCount = children.Count;
            if (childrenCount == 0)
            {
                return;
            }

            int blockIndex = 0;
            int blockCount = blockList.Count;
            RealizedColumnsBlock block = (blockCount > 0 ? blockList[blockIndex] : new RealizedColumnsBlock(-1, -1, -1));
            bool pastLastBlock = (blockCount > 0 ? false : true);

            int cleanupRangeStart = -1;
            int cleanupCount = 0;
            int lastVirtualizedColumnIndex = -1;

            ItemsControl parentPresenter = ParentPresenter;
            DataGridCellsPresenter cellsPresenter = parentPresenter as DataGridCellsPresenter;
            DataGridColumnHeadersPresenter headersPresenter = parentPresenter as DataGridColumnHeadersPresenter;

            for (int i = 0; i < childrenCount; i++)
            {
                int columnIndex = i;
                UIElement child = children[i] as UIElement;
                IProvideDataGridColumn columnProvider = child as IProvideDataGridColumn;
                if (columnProvider != null)
                {
                    DataGridColumn column = columnProvider.Column;
                    for (; columnIterator < columnCount; columnIterator++)
                    {
                        if (column == columns[columnIterator])
                        {
                            break;
                        }
                    }

                    columnIndex = columnIterator++;
                    Debug.Assert(columnIndex < columnCount, "columnIndex should be less than column count");
                }

                bool virtualizeChild = pastLastBlock || !InBlockOrNextBlock(blockList, columnIndex, ref blockIndex, ref block, out pastLastBlock);

                DataGridCell cell = child as DataGridCell;
                if ((cell != null && (cell.IsEditing || cell.IsKeyboardFocusWithin || cell == parentDataGrid.FocusedCell)) ||
                    (cellsPresenter != null &&
                    cellsPresenter.IsItemItsOwnContainerInternal(cellsPresenter.Items[columnIndex])) ||
                    (headersPresenter != null &&
                    headersPresenter.IsItemItsOwnContainerInternal(headersPresenter.Items[columnIndex])))
                {
                    virtualizeChild = false;
                }

                if (!columns[columnIndex].IsVisible)
                {
                    virtualizeChild = true;
                }

                if (virtualizeChild)
                {
                    if (cleanupRangeStart == -1)
                    {
                        cleanupRangeStart = i;
                        cleanupCount = 1;
                    }
                    else if (lastVirtualizedColumnIndex == columnIndex - 1)
                    {
                        cleanupCount++;
                    }
                    else
                    {
                        // Meaning that two consecutive children to be virtualized are not corresponding to
                        // two consecutive columns
                        CleanupRange(children, generator, cleanupRangeStart, cleanupCount);
                        childrenCount -= cleanupCount;
                        i -= cleanupCount;
                        cleanupCount = 1;
                        cleanupRangeStart = i;
                    }

                    lastVirtualizedColumnIndex = columnIndex;
                }
                else
                {
                    if (cleanupCount > 0)
                    {
                        CleanupRange(children, generator, cleanupRangeStart, cleanupCount);
                        childrenCount -= cleanupCount;
                        i -= cleanupCount;
                        cleanupCount = 0;
                        cleanupRangeStart = -1;
                    }
                }
            }

            if (cleanupCount > 0)
            {
                CleanupRange(children, generator, cleanupRangeStart, cleanupCount);
            }
        }

        /// <summary>
        ///     Method which cleans up a given range of children
        /// </summary>
        private void CleanupRange(IList children, IItemContainerGenerator generator, int startIndex, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (IsVirtualizing && InRecyclingMode)
            {
                Debug.Assert(startIndex >= 0);
                Debug.Assert(children == _realizedChildren, "the given child list must be the _realizedChildren list when recycling");

                // Recycle and remove the children from realized list
                GeneratorPosition position = new GeneratorPosition(startIndex, 0);
                ((IRecyclingItemContainerGenerator)generator).Recycle(position, count);
                _realizedChildren.RemoveRange(startIndex, count);
            }
            else
            {
                // Remove the desired range of children
                RemoveInternalChildRange(startIndex, count);
                generator.Remove(new GeneratorPosition(startIndex, 0), count);
            }
        }

        /// <summary>
        ///     Recycled containers still in the InternalChildren collection at the end of Measure should be disconnected
        ///     from the visual tree.  Otherwise they're still visible to things like Arrange, keyboard navigation, etc.
        /// </summary>
        private void DisconnectRecycledContainers()
        {
            int realizedIndex = 0;
            UIElement visualChild;
            UIElement realizedChild = _realizedChildren.Count > 0 ? _realizedChildren[0] : null;
            UIElementCollection children = InternalChildren;

            int removeStartRange = -1;
            int removalCount = 0;
            for (int i = 0; i < children.Count; i++)
            {
                visualChild = children[i];

                if (visualChild == realizedChild)
                {
                    if (removalCount > 0)
                    {
                        RemoveInternalChildRange(removeStartRange, removalCount);
                        i -= removalCount;
                        removalCount = 0;
                        removeStartRange = -1;
                    }

                    realizedIndex++;

                    if (realizedIndex < _realizedChildren.Count)
                    {
                        realizedChild = _realizedChildren[realizedIndex];
                    }
                    else
                    {
                        realizedChild = null;
                    }
                }
                else
                {
                    if (removeStartRange == -1)
                    {
                        removeStartRange = i;
                    }

                    removalCount++;
                }
            }

            if (removalCount > 0)
            {
                RemoveInternalChildRange(removeStartRange, removalCount);
            }
        }

        #endregion

        #region Arrange

        /// <summary>
        ///     Private class used to maintain state between arrange
        ///     of multiple children.
        /// </summary>
        private class ArrangeState
        {
            public ArrangeState()
            {
                FrozenColumnCount = 0;
                ChildHeight = 0.0;
                NextFrozenCellStart = 0.0;
                NextNonFrozenCellStart = 0.0;
                ViewportStartX = 0.0;
                DataGridHorizontalScrollStartX = 0.0;
                OldClippedChild = null;
                NewClippedChild = null;
            }

            public int FrozenColumnCount
            {
                get; set;
            }

            public double ChildHeight
            {
                get; set;
            }

            public double NextFrozenCellStart
            {
                get; set;
            }

            public double NextNonFrozenCellStart
            {
                get; set;
            }

            public double ViewportStartX
            {
                get; set;
            }

            public double DataGridHorizontalScrollStartX
            {
                get; set;
            }

            public UIElement OldClippedChild
            {
                get; set;
            }

            public UIElement NewClippedChild
            {
                get; set;
            }
        }

        /// <summary>
        ///     Helper method to initialize the arrange state
        /// </summary>
        /// <param name="arrangeState"></param>
        private void InitializeArrangeState(ArrangeState arrangeState)
        {
            DataGrid parentDataGrid = ParentDataGrid;
            double horizontalOffset = parentDataGrid.HorizontalScrollOffset;
            double cellsPanelOffset = parentDataGrid.CellsPanelHorizontalOffset;
            arrangeState.NextFrozenCellStart = horizontalOffset;
            arrangeState.NextNonFrozenCellStart -= cellsPanelOffset;
            arrangeState.ViewportStartX = horizontalOffset - cellsPanelOffset;
            arrangeState.FrozenColumnCount = parentDataGrid.FrozenColumnCount;
        }

        /// <summary>
        ///     Helper method which which ends the arrange by setting values
        ///     from arrange state to appropriate fields.
        /// </summary>
        /// <param name="arrangeState"></param>
        private void FinishArrange(ArrangeState arrangeState)
        {
            DataGrid parentDataGrid = ParentDataGrid;

            // Update the NonFrozenColumnsViewportHorizontalOffset property of datagrid
            if (parentDataGrid != null)
            {
                parentDataGrid.NonFrozenColumnsViewportHorizontalOffset = arrangeState.DataGridHorizontalScrollStartX;
            }

            // Remove the clip on previous clipped child
            if (arrangeState.OldClippedChild != null)
            {
                arrangeState.OldClippedChild.CoerceValue(ClipProperty);
            }

            // Add the clip on new child to be clipped for the sake of frozen columns.
            _clippedChildForFrozenBehaviour = arrangeState.NewClippedChild;
            if (_clippedChildForFrozenBehaviour != null)
            {
                _clippedChildForFrozenBehaviour.CoerceValue(ClipProperty);
            }
        }

        private void SetDataGridCellPanelWidth(IList children, double newWidth)
        {
            if (children.Count != 0 &&
                   children[0] is DataGridColumnHeader &&
                   !DoubleUtil.AreClose(ParentDataGrid.CellsPanelActualWidth, newWidth))
            {
                // Set the CellsPanelActualWidth property of the datagrid
                ParentDataGrid.CellsPanelActualWidth = newWidth;
            }
        }

        [Conditional("DEBUG")]
        private static void Debug_VerifyRealizedIndexCountVsDisplayIndexCount(List<RealizedColumnsBlock> blockList, List<RealizedColumnsBlock> displayIndexBlockList)
        {
            Debug.Assert(
                blockList != null && blockList.Count > 0,
                "RealizedColumnsBlockList should not be null or empty");

            RealizedColumnsBlock lastBlock = blockList[blockList.Count - 1];
            RealizedColumnsBlock lastDisplayIndexBlock = displayIndexBlockList[displayIndexBlockList.Count - 1];
            Debug.Assert(
                (lastBlock.StartIndexOffset + lastBlock.EndIndex - lastBlock.StartIndex) == (lastDisplayIndexBlock.StartIndexOffset + lastDisplayIndexBlock.EndIndex - lastDisplayIndexBlock.StartIndex),
                "RealizedBlockList and DisplayIndex list should indicate same number of elements");
        }

        /// <summary>
        ///     Arrange
        ///
        ///     Iterates over the columns in the display index order and looks if
        ///     it the corresponding child is realized. If yes then arranges it.
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            IList children = RealizedChildren;

            ArrangeState arrangeState = new ArrangeState();
            arrangeState.ChildHeight = arrangeSize.Height;
            DataGrid parentDataGrid = ParentDataGrid;

            /*
             * determine the horizontal offset, cells panel offset and other coordinates used for arrange of children
             */
            if (parentDataGrid != null)
            {
                parentDataGrid.QueueInvalidateCellsPanelHorizontalOffset();
                SetDataGridCellPanelWidth(children, arrangeSize.Width);
                InitializeArrangeState(arrangeState);
            }

            List<RealizedColumnsBlock> displayIndexBlockList = RealizedColumnsDisplayIndexBlockList;
            if (displayIndexBlockList != null && displayIndexBlockList.Count > 0)
            {
                double averageColumnWidth = parentDataGrid.InternalColumns.AverageColumnWidth;
                List<RealizedColumnsBlock> blockList = RealizedColumnsBlockList;
                Debug_VerifyRealizedIndexCountVsDisplayIndexCount(blockList, displayIndexBlockList);

                // Get realized children not in realized list, so that they dont participate in arrange
                List<int> additionalChildIndices = GetRealizedChildrenNotInBlockList(blockList, children);

                int displayIndexBlockIndex = -1;
                RealizedColumnsBlock displayIndexBlock = displayIndexBlockList[++displayIndexBlockIndex];
                bool pastLastBlock = false;
                for (int i = 0, count = parentDataGrid.Columns.Count; i < count; i++)
                {
                    bool realizedChild = InBlockOrNextBlock(displayIndexBlockList, i, ref displayIndexBlockIndex, ref displayIndexBlock, out pastLastBlock);
                    if (pastLastBlock)
                    {
                        break;
                    }

                    // Arrange the child if it is realized
                    if (realizedChild)
                    {
                        int columnIndex = parentDataGrid.ColumnIndexFromDisplayIndex(i);
                        RealizedColumnsBlock block = GetRealizedBlockForColumn(blockList, columnIndex);
                        int childIndex = block.StartIndexOffset + columnIndex - block.StartIndex;
                        if (additionalChildIndices != null)
                        {
                            for (int j = 0, additionalChildrenCount = additionalChildIndices.Count;
                                        j < additionalChildrenCount && additionalChildIndices[j] <= childIndex; j++)
                            {
                                childIndex++;
                            }
                        }

                        ArrangeChild(children[childIndex] as UIElement, i, arrangeState);
                    }
                    else
                    {
                        DataGridColumn column = parentDataGrid.ColumnFromDisplayIndex(i);
                        if (!column.IsVisible)
                        {
                            continue;
                        }

                        double childSize = GetColumnEstimatedMeasureWidth(column, averageColumnWidth);

                        Debug.Assert(i >= arrangeState.FrozenColumnCount, "Frozen cells should have been realized or not visible");

                        arrangeState.NextNonFrozenCellStart += childSize;
                    }
                }

                if (additionalChildIndices != null)
                {
                    for (int i = 0, count = additionalChildIndices.Count; i < count; i++)
                    {
                        UIElement child = children[additionalChildIndices[i]] as UIElement;
                        child.Arrange(new Rect());
                    }
                }
            }

            FinishArrange(arrangeState);

            return arrangeSize;
        }

        /// <summary>
        ///     Method which arranges the give child
        ///     based on given arrange state.
        ///
        ///     Determines the start position of the child
        ///     based on its display index, frozen count of
        ///     datagrid, current horizontal offset etc.
        /// </summary>
        private void ArrangeChild(
            UIElement child,
            int displayIndex,
            ArrangeState arrangeState)
        {
            Debug.Assert(child != null, "child cannot be null.");
            double childWidth = 0.0;
            IProvideDataGridColumn cell = child as IProvideDataGridColumn;

            // Determine if this child was clipped in last arrange for the sake of frozen columns
            if (child == _clippedChildForFrozenBehaviour)
            {
                arrangeState.OldClippedChild = child;
                _clippedChildForFrozenBehaviour = null;
            }

            // Width determinition of the child to be arranged. It is
            // display value if available else the ActualWidth
            if (cell != null)
            {
                Debug.Assert(cell.Column != null, "column cannot be null.");
                childWidth = cell.Column.Width.DisplayValue;
                if (DoubleUtil.IsNaN(childWidth))
                {
                    childWidth = cell.Column.ActualWidth;
                }
            }
            else
            {
                childWidth = child.DesiredSize.Width;
            }

            Rect rcChild = new Rect(new Size(childWidth, arrangeState.ChildHeight));

            // Determinition of start point for children to arrange. Lets say the there are 5 columns of which 2 are frozen.
            // If the datagrid is scrolled horizontally. Following is the snapshot of arrange
            /*
                    *                                                                                                    *
                    *| <Cell3> | <Unarranged space> | <RowHeader> | <Cell1> | <Cell2> | <Right Clip of Cell4> | <Cell5> |*
                    *                               |                        <Visible region>                           |*
             */
            if (displayIndex < arrangeState.FrozenColumnCount)
            {
                // For all the frozen children start from the horizontal offset
                // and arrange increamentally
                rcChild.X = arrangeState.NextFrozenCellStart;
                arrangeState.NextFrozenCellStart += childWidth;
                arrangeState.DataGridHorizontalScrollStartX += childWidth;
            }
            else
            {
                // For arranging non frozen children arrange which ever can be arranged
                // from the start to horizontal offset. This would fill out the space left by
                // frozen children. The next one child will be arranged and clipped accordingly past frozen
                // children. The remaining children will arranged in the remaining space.
                if (DoubleUtil.LessThanOrClose(arrangeState.NextNonFrozenCellStart, arrangeState.ViewportStartX))
                {
                    if (DoubleUtil.LessThanOrClose(arrangeState.NextNonFrozenCellStart + childWidth, arrangeState.ViewportStartX))
                    {
                        rcChild.X = arrangeState.NextNonFrozenCellStart;
                        arrangeState.NextNonFrozenCellStart += childWidth;
                    }
                    else
                    {
                        double cellChoppedWidth = arrangeState.ViewportStartX - arrangeState.NextNonFrozenCellStart;
                        if (DoubleUtil.AreClose(cellChoppedWidth, 0.0))
                        {
                            rcChild.X = arrangeState.NextFrozenCellStart;
                            arrangeState.NextNonFrozenCellStart = arrangeState.NextFrozenCellStart + childWidth;
                        }
                        else
                        {
                            rcChild.X = arrangeState.NextFrozenCellStart - cellChoppedWidth;
                            double clipWidth = childWidth - cellChoppedWidth;
                            arrangeState.NewClippedChild = child;
                            _childClipForFrozenBehavior.Rect = new Rect(cellChoppedWidth, 0, clipWidth, rcChild.Height);
                            arrangeState.NextNonFrozenCellStart = arrangeState.NextFrozenCellStart + clipWidth;
                        }
                    }
                }
                else
                {
                    rcChild.X = arrangeState.NextNonFrozenCellStart;
                    arrangeState.NextNonFrozenCellStart += childWidth;
                }
            }

            child.Arrange(rcChild);
        }

        /// <summary>
        ///     Method which gets the realized block for a given index
        /// </summary>
        /// <param name="blockList"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        private static RealizedColumnsBlock GetRealizedBlockForColumn(List<RealizedColumnsBlock> blockList, int columnIndex)
        {
            for (int i = 0, count = blockList.Count; i < count; i++)
            {
                RealizedColumnsBlock block = blockList[i];
                if (columnIndex >= block.StartIndex &&
                    columnIndex <= block.EndIndex)
                {
                    return block;
                }
            }

            return new RealizedColumnsBlock(-1, -1, -1);
        }

        /// <summary>
        ///     Determines the list of children which are realized, but
        ///     shouldnt be as per the state stored at rows presenter
        /// </summary>
        private List<int> GetRealizedChildrenNotInBlockList(List<RealizedColumnsBlock> blockList, IList children)
        {
            DataGrid parentDataGrid = ParentDataGrid;
            RealizedColumnsBlock lastBlock = blockList[blockList.Count - 1];
            int blockElementCount = lastBlock.StartIndexOffset + lastBlock.EndIndex - lastBlock.StartIndex + 1;
            if (children.Count == blockElementCount)
            {
                return null;
            }

            Debug.Assert(children.Count > blockElementCount, "Element count from blocks can't be less than total children count");

            List<int> additionalChildIndices = new List<int>();
            if (blockList.Count == 0)
            {
                for (int i = 0, count = children.Count; i < count; i++)
                {
                    additionalChildIndices.Add(i);
                }
            }
            else
            {
                int blockIndex = 0;
                RealizedColumnsBlock block = blockList[blockIndex++];

                for (int i = 0, count = children.Count; i < count; i++)
                {
                    IProvideDataGridColumn cell = children[i] as IProvideDataGridColumn;
                    int columnIndex = i;
                    if (cell != null)
                    {
                        columnIndex = parentDataGrid.Columns.IndexOf(cell.Column);
                    }

                    if (columnIndex < block.StartIndex)
                    {
                        additionalChildIndices.Add(i);
                    }
                    else if (columnIndex > block.EndIndex)
                    {
                        if (blockIndex >= blockList.Count)
                        {
                            for (int j = i; j < count; j++)
                            {
                                additionalChildIndices.Add(j);
                            }

                            break;
                        }

                        block = blockList[blockIndex++];
                        Debug.Assert(columnIndex <= block.EndIndex, "Missing children for index in block list");

                        if (columnIndex < block.StartIndex)
                        {
                            additionalChildIndices.Add(i);
                        }
                    }
                }
            }

            return additionalChildIndices;
        }

        #endregion

        #region Column Virtualization

        // returns true if children are a superset of the realized columns
        internal bool HasCorrectRealizedColumns
        {
            get
            {
                DataGridColumnCollection columns = (DataGridColumnCollection)ParentDataGrid.Columns;
                EnsureRealizedChildren();   // necessary because this can be called before Measure (DevDiv2 1123429)
                IList children = RealizedChildren;

                // common case:  all columns are present
                if (children.Count == columns.Count)
                    return true;

                // see if each column in the realized block list appears as a child.
                // The block list and the children are both sorted, so we can do a linear merge.
                List<int> displayIndexMap = columns.DisplayIndexMap;
                List<RealizedColumnsBlock> blockList = RealizedColumnsBlockList;
                int k=0, n=children.Count;
                for (int j=0; j<blockList.Count; ++j)
                {
                    RealizedColumnsBlock block = blockList[j];
                    for (int index=block.StartIndex; index<=block.EndIndex; ++index)
                    {
                        for (; k<n; ++k)
                        {
                            IProvideDataGridColumn cell = children[k] as IProvideDataGridColumn;
                            if (cell != null)
                            {
                                int displayIndex = cell.Column.DisplayIndex;
                                int childColumnIndex = (displayIndex < 0 ? -1 : displayIndexMap[displayIndex]);
                                if (index < childColumnIndex)
                                    return false;   // child list skipped over index
                                else if (index == childColumnIndex)
                                    break;          // child list contains index
                            }
                        }

                        if (k==n)
                            return false;   // index didn't appear in child list
                        ++k;                // index did appear at position k
                    }
                }
                return true;
            }
        }

        private bool RebuildRealizedColumnsBlockList
        {
            get
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    DataGridColumnCollection columns = dataGrid.InternalColumns;
                    return IsVirtualizing ? columns.RebuildRealizedColumnsBlockListForVirtualizedRows : columns.RebuildRealizedColumnsBlockListForNonVirtualizedRows;
                }

                return true;
            }

            set
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    if (IsVirtualizing)
                    {
                        dataGrid.InternalColumns.RebuildRealizedColumnsBlockListForVirtualizedRows = value;
                    }
                    else
                    {
                        dataGrid.InternalColumns.RebuildRealizedColumnsBlockListForNonVirtualizedRows = value;
                    }
                }
            }
        }

        private List<RealizedColumnsBlock> RealizedColumnsBlockList
        {
            get
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    DataGridColumnCollection columns = dataGrid.InternalColumns;
                    return IsVirtualizing ? columns.RealizedColumnsBlockListForVirtualizedRows : columns.RealizedColumnsBlockListForNonVirtualizedRows;
                }

                return null;
            }

            set
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    if (IsVirtualizing)
                    {
                        dataGrid.InternalColumns.RealizedColumnsBlockListForVirtualizedRows = value;
                    }
                    else
                    {
                        dataGrid.InternalColumns.RealizedColumnsBlockListForNonVirtualizedRows = value;
                    }
                }
            }
        }

        private List<RealizedColumnsBlock> RealizedColumnsDisplayIndexBlockList
        {
            get
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    DataGridColumnCollection columns = dataGrid.InternalColumns;
                    return IsVirtualizing ? columns.RealizedColumnsDisplayIndexBlockListForVirtualizedRows : columns.RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows;
                }

                return null;
            }

            set
            {
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    if (IsVirtualizing)
                    {
                        dataGrid.InternalColumns.RealizedColumnsDisplayIndexBlockListForVirtualizedRows = value;
                    }
                    else
                    {
                        dataGrid.InternalColumns.RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows = value;
                    }
                }
            }
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
                ItemsControl parentPresenter = ParentPresenter;
                if (parentPresenter != null)
                {
                    IItemContainerGenerator generator = parentPresenter.ItemContainerGenerator as IItemContainerGenerator;
                    if (generator != null && generator == generator.GetItemContainerGeneratorForPanel(this))
                    {
                        DataGridCellsPresenter cellsPresenter = parentPresenter as DataGridCellsPresenter;
                        if (cellsPresenter != null)
                        {
                            cellsPresenter.InternalItemsHost = this;
                        }
                        else
                        {
                            DataGridColumnHeadersPresenter headersPresenter = parentPresenter as DataGridColumnHeadersPresenter;
                            if (headersPresenter != null)
                            {
                                headersPresenter.InternalItemsHost = this;
                            }
                        }
                    }
                }
            }
            else
            {
                ItemsControl parentPresenter = ParentPresenter;
                if (parentPresenter != null)
                {
                    DataGridCellsPresenter cellsPresenter = parentPresenter as DataGridCellsPresenter;
                    if (cellsPresenter != null)
                    {
                        if (cellsPresenter.InternalItemsHost == this)
                        {
                            cellsPresenter.InternalItemsHost = null;
                        }
                    }
                    else
                    {
                        DataGridColumnHeadersPresenter headersPresenter = parentPresenter as DataGridColumnHeadersPresenter;
                        if (headersPresenter != null && headersPresenter.InternalItemsHost == this)
                        {
                            headersPresenter.InternalItemsHost = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     The property which returns DataGridRowsPresenter ancestor of this panel
        /// </summary>
        private DataGridRowsPresenter ParentRowsPresenter
        {
            get
            {
                DataGrid parentDataGrid = ParentDataGrid;
                if (parentDataGrid == null)
                {
                    return null;
                }

                if (!parentDataGrid.IsGrouping)
                {
                    return parentDataGrid.InternalItemsHost as DataGridRowsPresenter;
                }

                // when grouping, we can't just look down from DataGrid - there
                // are different presenters for each group.  Instead, look up.
                DataGridCellsPresenter presenter = ParentPresenter as DataGridCellsPresenter;
                if (presenter != null)
                {
                    DataGridRow row = presenter.DataGridRowOwner;
                    if (row != null)
                    {
                        return VisualTreeHelper.GetParent(row) as DataGridRowsPresenter;
                    }
                }

                return null;
            }
        }

        private void DetermineVirtualizationState()
        {
            ItemsControl parentPresenter = ParentPresenter;
            if (parentPresenter != null)
            {
                IsVirtualizing = VirtualizingPanel.GetIsVirtualizing(parentPresenter);
                InRecyclingMode = (VirtualizingPanel.GetVirtualizationMode(parentPresenter) == VirtualizationMode.Recycling);
            }
        }

        /// <summary>
        ///     Property which determines if one has to virtualize the cells
        /// </summary>
        private bool IsVirtualizing
        {
            get; set;
        }

        /// <summary>
        ///     Property which determines if one is in recycling mode.
        /// </summary>
        private bool InRecyclingMode
        {
            get; set;
        }

        /// <summary>
        ///     Helper method which estimates the width of the column
        /// </summary>
        private static double GetColumnEstimatedMeasureWidth(DataGridColumn column, double averageColumnWidth)
        {
            if (!column.IsVisible)
            {
                return 0.0;
            }

            double childMeasureWidth = column.Width.DisplayValue;
            if (DoubleUtil.IsNaN(childMeasureWidth))
            {
                childMeasureWidth = Math.Max(averageColumnWidth, column.MinWidth);
                childMeasureWidth = Math.Min(childMeasureWidth, column.MaxWidth);
            }

            return childMeasureWidth;
        }

        /// <summary>
        ///     Helper method which estimates the sum of widths of
        ///     a given block of columns.
        /// </summary>
        private double GetColumnEstimatedMeasureWidthSum(int startIndex, int endIndex, double averageColumnWidth)
        {
            double measureWidth = 0.0;
            DataGrid parentDataGrid = ParentDataGrid;
            for (int i = startIndex; i <= endIndex; i++)
            {
                measureWidth += GetColumnEstimatedMeasureWidth(parentDataGrid.Columns[i], averageColumnWidth);
            }

            return measureWidth;
        }

        /// <summary>
        ///     Returns the list of childen that have been realized by the Generator.
        ///     We must use this method whenever we interact with the Generator's index.
        ///     In recycling mode the Children collection also contains recycled containers and thus does
        ///     not map to the Generator's list.
        /// </summary>
        private IList RealizedChildren
        {
            get
            {
                if (IsVirtualizing && InRecyclingMode)
                {
                    return _realizedChildren;
                }
                else
                {
                    return InternalChildren;
                }
            }
        }

        /// <summary>
        ///     Helper method which ensures that the _realizedChildren
        ///     member is properly initialized
        /// </summary>
        private void EnsureRealizedChildren()
        {
            if (IsVirtualizing && InRecyclingMode)
            {
                if (_realizedChildren == null)
                {
                    UIElementCollection children = InternalChildren;

                    _realizedChildren = new List<UIElement>(children.Count);

                    for (int i = 0; i < children.Count; i++)
                    {
                        _realizedChildren.Add(children[i]);
                    }
                }
            }
            else
            {
                _realizedChildren = null;
            }
        }

        /// <summary>
        ///     Helper method to compute the cells panel horizontal offset
        /// </summary>
        internal double ComputeCellsPanelHorizontalOffset()
        {
            Debug.Assert(ParentDataGrid != null, "ParentDataGrid should not be null");

            double cellsPanelOffset = 0.0;
            DataGrid dataGrid = ParentDataGrid;
            double horizontalOffset = dataGrid.HorizontalScrollOffset;
            ScrollViewer scrollViewer = dataGrid.InternalScrollHost;
            if (scrollViewer != null)
            {
                cellsPanelOffset = horizontalOffset + TransformToAncestor(scrollViewer).Transform(new Point()).X;
            }

            return cellsPanelOffset;
        }

        /// <summary>
        ///     Helper method which returns the viewport width
        /// </summary>
        private double GetViewportWidth()
        {
            double availableViewportWidth = 0.0;
            DataGrid parentDataGrid = ParentDataGrid;
            if (parentDataGrid != null)
            {
                ScrollContentPresenter scrollContentPresenter = parentDataGrid.InternalScrollContentPresenter;
                if (scrollContentPresenter != null &&
                    !scrollContentPresenter.CanContentScroll)
                {
                    availableViewportWidth = scrollContentPresenter.ViewportWidth;
                }
                else
                {
                    IScrollInfo scrollInfo = parentDataGrid.InternalItemsHost as IScrollInfo;
                    if (scrollInfo != null)
                    {
                        availableViewportWidth = scrollInfo.ViewportWidth;
                    }
                }
            }

            DataGridRowsPresenter parentRowsPresenter = ParentRowsPresenter;

            if (DoubleUtil.AreClose(availableViewportWidth, 0.0) && parentRowsPresenter != null)
            {
                Size rowPresenterAvailableSize = parentRowsPresenter.AvailableSize;
                if (!DoubleUtil.IsNaN(rowPresenterAvailableSize.Width) && !Double.IsInfinity(rowPresenterAvailableSize.Width))
                {
                    availableViewportWidth = rowPresenterAvailableSize.Width;
                }
                else if (parentDataGrid.IsGrouping) // parentRowsPresenter!=null implies parentDataGrid!=null
                {
                    IHierarchicalVirtualizationAndScrollInfo hvsInfo = DataGridHelper.FindParent<GroupItem>(parentRowsPresenter) as IHierarchicalVirtualizationAndScrollInfo;
                    if (hvsInfo != null)
                    {
                        availableViewportWidth = hvsInfo.Constraints.Viewport.Width;
                    }
                }
            }

            return availableViewportWidth;
        }

        /// <summary>
        ///     Called when the Items collection associated with the containing ItemsControl changes.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">Event arguments</param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    OnItemsRemove(args);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    OnItemsReplace(args);
                    break;

                case NotifyCollectionChangedAction.Move:
                    OnItemsMove(args);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void OnItemsRemove(ItemsChangedEventArgs args)
        {
            RemoveChildRange(args.Position, args.ItemCount, args.ItemUICount);
        }

        private void OnItemsReplace(ItemsChangedEventArgs args)
        {
            RemoveChildRange(args.Position, args.ItemCount, args.ItemUICount);
        }

        private void OnItemsMove(ItemsChangedEventArgs args)
        {
            RemoveChildRange(args.OldPosition, args.ItemCount, args.ItemUICount);
        }

        private void RemoveChildRange(GeneratorPosition position, int itemCount, int itemUICount)
        {
            if (IsItemsHost)
            {
                UIElementCollection children = InternalChildren;
                int pos = position.Index;
                if (position.Offset > 0)
                {
                    // An item is being removed after the one at the index
                    pos++;
                }

                if (pos < children.Count)
                {
                    Debug.Assert((itemCount == itemUICount) || (itemUICount == 0), "Both ItemUICount and ItemCount should be equal or ItemUICount should be 0.");
                    if (itemUICount > 0)
                    {
                        RemoveInternalChildRange(pos, itemUICount);

                        if (IsVirtualizing && InRecyclingMode)
                        {
                            // No need to call EnsureRealizedChildren because at this point it is expected that
                            // the _realizedChildren collection is already initialized (because of previous measures).
                            _realizedChildren.RemoveRange(pos, itemUICount);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Sets the _realizedChildren in sync when children are cleared
        /// </summary>
        protected override void OnClearChildren()
        {
            base.OnClearChildren();
            _realizedChildren = null;
        }

        /// <summary>
        ///     A workaround method to access BringIndexIntoView method in this assembly.
        /// </summary>
        /// <param name="index"></param>
        internal void InternalBringIndexIntoView(int index)
        {
            BringIndexIntoView(index);
        }

        /// <summary>
        ///     Determines the position of the child and sets the horizontal
        ///     offset appropriately.
        /// </summary>
        /// <param name="index">Specify the item index that should become visible</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if index is out of range
        /// </exception>
        protected internal override void BringIndexIntoView(int index)
        {
            DataGrid parentDataGrid = ParentDataGrid;

            if (parentDataGrid == null)
            {
                base.BringIndexIntoView(index);
                return;
            }

            if (index < 0 || index >= parentDataGrid.Columns.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            // if the column widths aren't known, try again when they are
            if (parentDataGrid.InternalColumns.ColumnWidthsComputationPending)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action<int>)RetryBringIndexIntoView, index);
                return;
            }

            ScrollContentPresenter scrollContentPresenter = parentDataGrid.InternalScrollContentPresenter;
            IScrollInfo scrollInfo = null;
            if (scrollContentPresenter != null &&
                !scrollContentPresenter.CanContentScroll)
            {
                scrollInfo = scrollContentPresenter;
            }
            else
            {
                ScrollViewer scrollViewer = parentDataGrid.InternalScrollHost;
                if (scrollViewer != null)
                {
                    scrollInfo = scrollViewer.ScrollInfo;
                }
            }

            if (scrollInfo == null)
            {
                base.BringIndexIntoView(index);
                return;
            }

            bool wasMeasureDirty = MeasureDirty;  //see comment below
            bool needRetry = wasMeasureDirty;
            double newHorizontalOffset = 0.0;
            double oldHorizontalOffset = parentDataGrid.HorizontalScrollOffset;
            while (!IsChildInView(index, out newHorizontalOffset) &&
                   !DoubleUtil.AreClose(oldHorizontalOffset, newHorizontalOffset))
            {
                needRetry = true;
                scrollInfo.SetHorizontalOffset(newHorizontalOffset);
                UpdateLayout();
                oldHorizontalOffset = newHorizontalOffset;
            }

            // although the loop brings the desired column into view, the column
            // widths might change due to deferred data binding, which can push
            // the desired column out of view again.  To mitigate this, try
            // again after deferred bindings run (at Loaded priority)
            if (parentDataGrid.RetryBringColumnIntoView(needRetry))
            {
                DispatcherPriority priority = wasMeasureDirty ? DispatcherPriority.Background : DispatcherPriority.Loaded;
                Dispatcher.BeginInvoke(priority, (Action<int>)RetryBringIndexIntoView, index);

                // The idea is to run deferred bindings, already posted at
                // Loaded priority.  This may add content to a cell, causing a
                // layout request (at Render).  During UpdateLayout, the columns may
                // get new widths.  By the time we retry this method (at Loaded),
                // we'll see the new widths.
                //   But there's a subtle flaw.  MediaContext owns the task responsible
                // for calling UpdateLayout.  It demotes this task from Render to
                // Input priority when it hasn't seen Input in a while - see
                // MediaContext.ScheduleNextRenderOp.  If this happens,
                // the retry (at Loaded) will happen before the UpdateLayout (now at Input)
                // and we won't see the new widths.  [This flaw was very difficult to
                // diagnose, as it tended to go away when you set breakpoints or
                // tracepoints.  It's quite sensitive to timing.]
                //   To mitigate this, mark this panel as MeasureDirty.  Then during the
                // retry, check if MeasureDirty is still true;  if so, UpdateLayout
                // was demoted and we should reschedule the retry.  But this time
                // use Background priority, to let the UpdateLayout happen.
                //   This results in some flicker - the DataGrid redraws once with
                // the old column widths, and again with the new.  But that's the
                // best we can do given the arcane behavior of the layout system.
                InvalidateMeasure();
            }
        }

        private void RetryBringIndexIntoView(int index)
        {
            // if the app has changed the column collection since the retry was posted,
            // don't throw - just ignore it
            DataGrid parentDataGrid = ParentDataGrid;
            if (parentDataGrid != null && 0 <= index && index < parentDataGrid.Columns.Count)
            {
                BringIndexIntoView(index);
            }
        }

        /// <summary>
        ///     Method which determines if the child at given index is already in view.
        ///     Also returns the appropriate estimated horizontal offset to bring the
        ///     child into view if it is not already in view.
        /// </summary>
        private bool IsChildInView(int index, out double newHorizontalOffset)
        {
            DataGrid parentDataGrid = ParentDataGrid;
            double horizontalOffset = parentDataGrid.HorizontalScrollOffset;
            newHorizontalOffset = horizontalOffset;

            double averageColumnWidth = parentDataGrid.InternalColumns.AverageColumnWidth;
            int frozenColumnCount = parentDataGrid.FrozenColumnCount;

            double cellsPanelOffset = parentDataGrid.CellsPanelHorizontalOffset;
            double availableViewportWidth = GetViewportWidth();
            double nextFrozenCellStart = horizontalOffset;                // indicates the start position for next frozen cell
            double nextNonFrozenCellStart = -cellsPanelOffset;            // indicates the start position for next non-frozen cell
            double viewportStartX = horizontalOffset - cellsPanelOffset;  // indicates the start of viewport with respect to coordinate system of cell panel

            int displayIndex = Columns[index].DisplayIndex;
            double columnStart = 0.0;
            double columnEnd = 0.0;

            // Determine the start and end position of the concerned column in horizontal direction
            for (int i = 0; i <= displayIndex; i++)
            {
                DataGridColumn column = parentDataGrid.ColumnFromDisplayIndex(i);
                if (!column.IsVisible)
                {
                    continue;
                }

                double columnWidth = GetColumnEstimatedMeasureWidth(column, averageColumnWidth);

                if (i < frozenColumnCount)
                {
                    columnStart = nextFrozenCellStart;
                    columnEnd = columnStart + columnWidth;
                    nextFrozenCellStart += columnWidth;
                }
                else
                {
                    if (DoubleUtil.LessThanOrClose(nextNonFrozenCellStart, viewportStartX))
                    {
                        if (DoubleUtil.LessThanOrClose(nextNonFrozenCellStart + columnWidth, viewportStartX))
                        {
                            columnStart = nextNonFrozenCellStart;
                            columnEnd = columnStart + columnWidth;
                            nextNonFrozenCellStart += columnWidth;
                        }
                        else
                        {
                            columnStart = nextFrozenCellStart;
                            double cellChoppedWidth = viewportStartX - nextNonFrozenCellStart;
                            if (DoubleUtil.AreClose(cellChoppedWidth, 0.0))
                            {
                                columnEnd = columnStart + columnWidth;
                                nextNonFrozenCellStart = nextFrozenCellStart + columnWidth;
                            }
                            else
                            {
                                // If the concerned child is clipped for the sake of frozen columns
                                // then bring the start of child into view
                                double clipWidth = columnWidth - cellChoppedWidth;
                                columnEnd = columnStart + clipWidth;
                                nextNonFrozenCellStart = nextFrozenCellStart + clipWidth;
                                if (i == displayIndex)
                                {
                                    newHorizontalOffset = horizontalOffset - cellChoppedWidth;
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        columnStart = nextNonFrozenCellStart;
                        columnEnd = columnStart + columnWidth;
                        nextNonFrozenCellStart += columnWidth;
                    }
                }
            }

            double viewportEndX = viewportStartX + availableViewportWidth;
            if (DoubleUtil.LessThan(columnStart, viewportStartX))
            {
                newHorizontalOffset = columnStart + cellsPanelOffset;
            }
            else if (DoubleUtil.GreaterThan(columnEnd, viewportEndX))
            {
                double offsetChange = columnEnd - viewportEndX;

                if (displayIndex < frozenColumnCount)
                {
                    nextFrozenCellStart -= (columnEnd - columnStart);
                }

                if (DoubleUtil.LessThan(columnStart - offsetChange, nextFrozenCellStart))
                {
                    offsetChange = columnStart - nextFrozenCellStart;
                }

                if (DoubleUtil.AreClose(offsetChange, 0.0))
                {
                    return true;
                }
                else
                {
                    newHorizontalOffset = horizontalOffset + offsetChange;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Method which returns the clip for the child which overlaps with frozen column
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        internal Geometry GetFrozenClipForChild(UIElement child)
        {
            if (child == _clippedChildForFrozenBehaviour)
            {
                return _childClipForFrozenBehavior;
            }

            return null;
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Returns the columns on the parent DataGrid.
        /// </summary>
        private ObservableCollection<DataGridColumn> Columns
        {
            get
            {
                DataGrid parentDataGrid = ParentDataGrid;
                if (parentDataGrid != null)
                {
                    return parentDataGrid.Columns;
                }

                return null;
            }
        }

        /// <summary>
        ///     The row that this panel presents belongs to the DataGrid returned from this property.
        /// </summary>
        private DataGrid ParentDataGrid
        {
            get
            {
                if (_parentDataGrid == null)
                {
                    DataGridCellsPresenter presenter = ParentPresenter as DataGridCellsPresenter;

                    if (presenter != null)
                    {
                        DataGridRow row = presenter.DataGridRowOwner;

                        if (row != null)
                        {
                            _parentDataGrid = row.DataGridOwner;
                        }
                    }
                    else
                    {
                        DataGridColumnHeadersPresenter headersPresenter = ParentPresenter as DataGridColumnHeadersPresenter;

                        if (headersPresenter != null)
                        {
                            _parentDataGrid = headersPresenter.ParentDataGrid;
                        }
                    }
                }

                return _parentDataGrid;
            }
        }

        private ItemsControl ParentPresenter
        {
            get
            {
                FrameworkElement itemsPresenter = TemplatedParent as FrameworkElement;
                if (itemsPresenter != null)
                {
                    return itemsPresenter.TemplatedParent as ItemsControl;
                }

                return null;
            }
        }

        #endregion

        #region Data

        private DataGrid _parentDataGrid;

        private UIElement _clippedChildForFrozenBehaviour;
        private RectangleGeometry _childClipForFrozenBehavior = new RectangleGeometry();
        private List<UIElement> _realizedChildren;

        #endregion
    }
}
