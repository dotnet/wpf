// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows.Controls
{
    internal class VirtualizedCellInfoCollection : IList<DataGridCellInfo>
    {
        #region Construction

        /// <summary>
        ///     Instantiates a read/write instance of this class.
        /// </summary>
        /// <param name="owner">
        ///     In order to not always keep references to cells, the collection
        ///     requires a reference to the source of the cells.
        /// </param>
        internal VirtualizedCellInfoCollection(DataGrid owner)
        {
            Debug.Assert(owner != null, "owner must not be null.");

            // Only the SelectedCells collection is notified of changes. This could be
            // changed so that the collections in event arguments are also updated.
            _owner = owner;
            _regions = new List<CellRegion>();
        }

        /// <summary>
        ///     Creates a read-only collection initialized to the specified region.
        /// </summary>
        private VirtualizedCellInfoCollection(DataGrid owner, List<CellRegion> regions)
        {
            Debug.Assert(owner != null, "owner must not be null.");

            // Only the SelectedCells collection is notified of changes. This could be
            // changed so that the collections in event arguments are also updated.
            _owner = owner;
            _regions = (regions != null) ? new List<CellRegion>(regions) : new List<CellRegion>();
            IsReadOnly = true;
        }

        /// <summary>
        ///     Creates an empty, read-only collection.
        /// </summary>
        internal static VirtualizedCellInfoCollection MakeEmptyCollection(DataGrid owner)
        {
            return new VirtualizedCellInfoCollection(owner, null);
        }

        #endregion

        #region IList<T> Members

        /// <summary>
        ///     Adds a cell to the list.
        /// </summary>
        /// <param name="cell">The cell to add.</param>
        public void Add(DataGridCellInfo cell)
        {
            _owner.Dispatcher.VerifyAccess();

            ValidateIsReadOnly();

            if (!IsValidPublicCell(cell))
            {
                throw new ArgumentException(SR.Get(SRID.SelectedCellsCollection_InvalidItem), "cell");
            }

            if (Contains(cell))
            {
                throw new ArgumentException(SR.Get(SRID.SelectedCellsCollection_DuplicateItem), "cell");
            }

            AddValidatedCell(cell);
        }

        /// <summary>
        ///     Adds a validated cell to the list.
        /// </summary>
        /// <param name="cell">The cell to add.</param>
        internal void AddValidatedCell(DataGridCellInfo cell)
        {
            Debug.Assert(IsValidCell(cell), "The cell should be valid.");
            Debug.Assert(!Contains(cell), "VirtualizedCellInfoCollection does not support duplicate items.");

            int columnIndex;
            int rowIndex;
            ConvertCellInfoToIndexes(cell, out rowIndex, out columnIndex);
            AddRegion(rowIndex, columnIndex, 1, 1);
        }

        /// <summary>
        ///     Removes all cells from the collection.
        /// </summary>
        public void Clear()
        {
            _owner.Dispatcher.VerifyAccess();
            ValidateIsReadOnly();

            if (!IsEmpty)
            {
                VirtualizedCellInfoCollection removedItems = new VirtualizedCellInfoCollection(_owner, _regions);
                _regions.Clear();

                // Notify that the collection changed
                // Note: We're using Remove instead of Reset so that we have access to the old list.
                // This is not consistent with ObservableCollection<T>'s implementation, but since
                // this collection is not really an INotifyCollectionChanged, it doesn't matter.
                OnRemove(removedItems);
            }
        }

        /// <summary>
        ///     Determines if the cell is contained within the list.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns>true if the cell appears in the list. false otherwise.</returns>
        public bool Contains(DataGridCellInfo cell)
        {
            if (!IsValidPublicCell(cell))
            {
                throw new ArgumentException(SR.Get(SRID.SelectedCellsCollection_InvalidItem), "cell");
            }

            if (IsEmpty)
            {
                return false;
            }

            // Get the row and column index corresponding to the cell
            int columnIndex;
            int rowIndex;
            ConvertCellInfoToIndexes(cell, out rowIndex, out columnIndex);

            return Contains(rowIndex, columnIndex);
        }

        internal bool Contains(DataGridCell cell)
        {
            // This is a linear search and would be much better if it weren't.
            // However, this will yield better results when there is a small selection and
            // be equally bad when there is a large selection.
            // This method is used by DataGridCell.PrepareCell, which can't afford the
            // Items.IndexOf call while scrolling.
            if (!IsEmpty)
            {
                object row = cell.RowDataItem;
                int columnIndex = cell.Column.DisplayIndex;

                ItemCollection items = _owner.Items;
                int numItems = items.Count;
                int numRegions = _regions.Count;
                for (int i = 0; i < numRegions; i++)
                {
                    CellRegion region = _regions[i];
                    if ((region.Left <= columnIndex) && (columnIndex <= region.Right))
                    {
                        int bottom = region.Bottom;
                        for (int r = region.Top; r <= bottom; r++)
                        {
                            if (r < numItems)
                            {
                                if (items[r] == row)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal bool Contains(int rowIndex, int columnIndex)
        {
            // Go through all the regions to see if the point is contained with one
            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                if (_regions[i].Contains(columnIndex, rowIndex))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Copies the contents of the list to the destination array, starting at the specified index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the destination array to start copying to.</param>
        public void CopyTo(DataGridCellInfo[] array, int arrayIndex)
        {
            List<DataGridCellInfo> list = new List<DataGridCellInfo>();
            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                AddRegionToList(_regions[i], list);
            }

            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Returns an enumerator for the list.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VirtualizedCellInfoCollectionEnumerator(_owner, _regions, this);
        }

        /// <summary>
        ///     Returns an enumerator for the list.
        /// </summary>
        public IEnumerator<DataGridCellInfo> GetEnumerator()
        {
            return new VirtualizedCellInfoCollectionEnumerator(_owner, _regions, this);
        }

        /// <summary>
        ///     Iterates through region lists in list order and then from left-to-right, top-to-bottom.
        /// </summary>
        private class VirtualizedCellInfoCollectionEnumerator : IEnumerator<DataGridCellInfo>, IEnumerator
        {
            public VirtualizedCellInfoCollectionEnumerator(DataGrid owner, List<CellRegion> regions, VirtualizedCellInfoCollection collection)
            {
                _owner = owner;
                _regions = new List<CellRegion>(regions);
                _current = -1;
                _collection = collection;

                if (_regions != null)
                {
                    int numRegions = _regions.Count;
                    for (int i = 0; i < numRegions; i++)
                    {
                        _count += _regions[i].Size;
                    }
                }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool MoveNext()
            {
                if (_current < _count)
                {
                    _current++;
                }

                return CurrentWithinBounds;
            }

            public void Reset()
            {
                _current = -1;
            }

            public DataGridCellInfo Current
            {
                get
                {
                    if (CurrentWithinBounds)
                    {
                        return _collection.GetCellInfoFromIndex(_owner, _regions, _current);
                    }

                    return DataGridCellInfo.Unset;
                }
            }

            private bool CurrentWithinBounds
            {
                get { return (_current >= 0) && (_current < _count); }
            }

            object IEnumerator.Current
            {
                get { return ((VirtualizedCellInfoCollectionEnumerator)this).Current; }
            }

            private DataGrid _owner;
            private List<CellRegion> _regions;
            private int _current;
            private int _count;
            private VirtualizedCellInfoCollection _collection;
        }

        /// <summary>
        ///     Returns the index in the list of the specified cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns>The index of the cell in the list or -1 if it is not within the list.</returns>
        public int IndexOf(DataGridCellInfo cell)
        {
            int columnIndex;
            int rowIndex;
            ConvertCellInfoToIndexes(cell, out rowIndex, out columnIndex);

            int numRegions = _regions.Count;
            int regionCount = 0;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = _regions[i];
                if (region.Contains(columnIndex, rowIndex))
                {
                    return regionCount + (((rowIndex - region.Top) * region.Width) + columnIndex - region.Left);
                }

                regionCount += region.Size;
            }

            return -1;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public void Insert(int index, DataGridCellInfo cell)
        {
            throw new NotSupportedException(SR.Get(SRID.VirtualizedCellInfoCollection_DoesNotSupportIndexChanges));
        }

        /// <summary>
        ///     Remove the cell from the collection.
        /// </summary>
        /// <param name="cell">The cell to remove.</param>
        /// <returns>true if the cell was removed. false otherwise.</returns>
        public bool Remove(DataGridCellInfo cell)
        {
            _owner.Dispatcher.VerifyAccess();
            ValidateIsReadOnly();

            if (!IsEmpty)
            {
                int columnIndex;
                int rowIndex;
                ConvertCellInfoToIndexes(cell, out rowIndex, out columnIndex);

                if (Contains(rowIndex, columnIndex))
                {
                    RemoveRegion(rowIndex, columnIndex, 1, 1);

                    // The cell was removed
                    return true;
                }
            }

            // The cell was not removed
            return false;
        }

        /// <summary>
        ///     Removes the cell at the specified index in the list.
        /// </summary>
        /// <param name="index">A zero-based index into the list.</param>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException(SR.Get(SRID.VirtualizedCellInfoCollection_DoesNotSupportIndexChanges));
        }

        /// <summary>
        ///     Returns the cell at the specified index in the list.
        /// </summary>
        /// <param name="index">A zero-based index into the list.</param>
        /// <returns>The cell at the specified index.</returns>
        public DataGridCellInfo this[int index]
        {
            get
            {
                if ((index >= 0) && (index < Count))
                {
                    return GetCellInfoFromIndex(_owner, _regions, index);
                }

                throw new ArgumentOutOfRangeException("index");
            }

            set
            {
                throw new NotSupportedException(SR.Get(SRID.VirtualizedCellInfoCollection_DoesNotSupportIndexChanges));
            }
        }

        /// <summary>
        ///     The number of cells in the list.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                int numRegions = _regions.Count;
                for (int i = 0; i < numRegions; i++)
                {
                    count += _regions[i].Size;
                }

                return count;
            }
        }

        /// <summary>
        ///     Whether the collection can be changed.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            private set { _isReadOnly = value; }
        }

        #endregion

        #region Change notification

        /// <summary>
        ///     Notifies that cells were added.
        /// </summary>
        private void OnAdd(VirtualizedCellInfoCollection newItems)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, null, newItems);
        }

        /// <summary>
        ///     Notifies that cells were removed.
        /// </summary>
        private void OnRemove(VirtualizedCellInfoCollection oldItems)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItems, null);
        }

        /// <summary>
        ///     Notification that the collection has changed.
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, VirtualizedCellInfoCollection oldItems, VirtualizedCellInfoCollection newItems)
        {
            // Do nothing in the base implementation. SelectedCellsCollection will notify the owner.
        }

        #endregion

        #region Cell Validation

        private bool IsValidCell(DataGridCellInfo cell)
        {
            return cell.IsValidForDataGrid(_owner);
        }

        private bool IsValidPublicCell(DataGridCellInfo cell)
        {
            return cell.IsValidForDataGrid(_owner) && cell.IsValid;
        }

        #endregion

        #region Region

        private struct CellRegion
        {
            public CellRegion(int left, int top, int width, int height)
            {
                Debug.Assert(left >= 0, "left must be positive.");
                Debug.Assert(top >= 0, "top must be positive.");
                Debug.Assert(width >= 0, "width must be positive.");
                Debug.Assert(height >= 0, "height must be positive.");

                _left = left;
                _top = top;
                _width = width;
                _height = height;
            }

            public int Left
            {
                get
                {
                    return _left;
                }

                set
                {
                    Debug.Assert(value >= 0, "Value must be positive.");
                    _left = value;
                }
            }

            public int Top
            {
                get
                {
                    return _top;
                }

                set
                {
                    Debug.Assert(value >= 0, "Value must be positive.");
                    _top = value;
                }
            }

            public int Right
            {
                get
                {
                    return _left + _width - 1;
                }

                set
                {
                    Debug.Assert(value >= _left, "Right must be greater than or equal to Left.");
                    _width = value - _left + 1;
                }
            }

            public int Bottom
            {
                get
                {
                    return _top + _height - 1;
                }

                set
                {
                    Debug.Assert(value >= _top, "Bottom must be greater than or equal to Top.");
                    _height = value - _top + 1;
                }
            }

            public int Width
            {
                get
                {
                    return _width;
                }

                set
                {
                    Debug.Assert(value >= 0, "Value must be positive.");
                    _width = value;
                }
            }

            public int Height
            {
                get
                {
                    return _height;
                }

                set
                {
                    Debug.Assert(value >= 0, "Value must be positive.");
                    _height = value;
                }
            }

            public bool IsEmpty
            {
                get { return (_width == 0) || (_height == 0); }
            }

            public int Size
            {
                get { return _width * _height; }
            }

            public bool Contains(int x, int y)
            {
                if (IsEmpty)
                {
                    return false;
                }
                else
                {
                    return (x >= Left) && (y >= Top) && (x <= Right) && (y <= Bottom);
                }
            }

            public bool Contains(CellRegion region)
            {
                return (Left <= region.Left) && (Top <= region.Top) &&
                       (Right >= region.Right) && (Bottom >= region.Bottom);
            }

            public bool Intersects(CellRegion region)
            {
                return Intersects(Left, Right, region.Left, region.Right) &&
                       Intersects(Top, Bottom, region.Top, region.Bottom);
            }

            private static bool Intersects(int start1, int end1, int start2, int end2)
            {
                return (start1 <= end2) && (end1 >= start2);
            }

            public CellRegion Intersection(CellRegion region)
            {
                if (Intersects(region))
                {
                    int left = Math.Max(Left, region.Left);
                    int top = Math.Max(Top, region.Top);
                    int right = Math.Min(Right, region.Right);
                    int bottom = Math.Min(Bottom, region.Bottom);
                    return new CellRegion(left, top, right - left + 1, bottom - top + 1);
                }
                else
                {
                    return Empty;
                }
            }

            /// <summary>
            ///     Attempts to combine this region with another.
            /// </summary>
            /// <param name="region">The region to add.</param>
            /// <returns>true if the region was incorporated into this region, false otherwise.</returns>
            public bool Union(CellRegion region)
            {
                if (Contains(region))
                {
                    // This region contains the other region,
                    // nothing needs to change.
                    return true;
                }
                else if (region.Contains(this))
                {
                    // When the passed in region contains this region, use
                    // its dimensions.
                    Left = region.Left;
                    Top = region.Top;
                    Width = region.Width;
                    Height = region.Height;
                    return true;
                }
                else
                {
                    // When there is no containment, we only support adding in regions
                    // that share a common dimension with the current region. Additionally,
                    // the new region must be adjacent or intersect the current region.
                    bool xMatch = (region.Left == Left) && (region.Width == Width);
                    bool yMatch = (region.Top == Top) && (region.Height == Height);

                    if (xMatch || yMatch)
                    {
                        // Compare the opposite dimension of what matches
                        int start = xMatch ? Top : Left;
                        int end = xMatch ? Bottom : Right;
                        int compareStart = xMatch ? region.Top : region.Left;
                        int compareEnd = xMatch ? region.Bottom : region.Right;

                        bool unionAllowed = false;
                        if (compareEnd <= end)
                        {
                            // Since we eliminated containment and one dimension matches,
                            // compareStart can only be less than start at this point.
                            // That only leaves the check that compareEnd is no greater than 1
                            // less than start (and it's fine to be greater than start).
                            unionAllowed = (start - compareEnd) <= 1;
                        }
                        else if (start <= compareStart)
                        {
                            // Since we eliminated containment and one dimension matches,
                            // compareEnd can only be greater than end at this point.
                            // That only leaves the check that compareStart is no greater than 1
                            // greater than end (and it's fine to be less than end).
                            unionAllowed = (compareStart - end) <= 1;
                        }

                        if (unionAllowed)
                        {
                            int prevRight = Right;
                            int prevBottom = Bottom;
                            Left = Math.Min(Left, region.Left);
                            Top = Math.Min(Top, region.Top);
                            Right = Math.Max(prevRight, region.Right);
                            Bottom = Math.Max(prevBottom, region.Bottom);
                            return true;
                        }
                    }
                }

                return false; // Could not union
            }

            /// <summary>
            ///     Determines the remainder of this region when the specified region is removed.
            ///     This method does not modify this region.
            /// </summary>
            /// <param name="region">The region to remove from this region.</param>
            /// <param name="remainder">What is left of this region when the specified region is removed.</param>
            /// <returns></returns>
            public bool Remainder(CellRegion region, out List<CellRegion> remainder)
            {
                if (Intersects(region))
                {
                    if (region.Contains(this))
                    {
                        // The region to remove is equal or greater than this one,
                        // so there is no remainder.
                        remainder = null;
                    }
                    else
                    {
                        // There will be some sort of remainder
                        remainder = new List<CellRegion>();

                        if (Top < region.Top)
                        {
                            // Add the portion that is above
                            remainder.Add(new CellRegion(Left, Top, Width, region.Top - Top));
                        }

                        if (Left < region.Left)
                        {
                            // Add the portion that is to the left
                            int top = Math.Max(Top, region.Top);
                            int bottom = Math.Min(Bottom, region.Bottom);
                            remainder.Add(new CellRegion(Left, top, region.Left - Left, bottom - top + 1));
                        }

                        if (Right > region.Right)
                        {
                            // Add the portion that is to the right
                            int top = Math.Max(Top, region.Top);
                            int bottom = Math.Min(Bottom, region.Bottom);
                            remainder.Add(new CellRegion(region.Right + 1, top, Right - region.Right, bottom - top + 1));
                        }

                        if (Bottom > region.Bottom)
                        {
                            // Add the portion that is below
                            remainder.Add(new CellRegion(Left, region.Bottom + 1, Width, Bottom - region.Bottom));
                        }
                    }

                    return true; // Intersecting
                }
                else
                {
                    // The region doesn't intersect, this region is the remainder,
                    // but in the interests of not allocating a lot of lists,
                    // return null and false.
                    remainder = null;
                    return false; // Not intersecting
                }
            }

            public static CellRegion Empty
            {
                get { return new CellRegion(0, 0, 0, 0); }
            }

            private int _left;
            private int _top;
            private int _width;
            private int _height;
        }

        protected bool IsEmpty
        {
            get
            {
                int numRegions = _regions.Count;
                for (int i = 0; i < numRegions; i++)
                {
                    if (!_regions[i].IsEmpty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        protected void GetBoundingRegion(out int left, out int top, out int right, out int bottom)
        {
            Debug.Assert(!IsEmpty, "Don't call GetBoundingRegion when IsEmpty is true.");

            left = int.MaxValue;
            top = int.MaxValue;
            right = 0;
            bottom = 0;

            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = _regions[i];
                if (region.Left < left)
                {
                    left = region.Left;
                }

                if (region.Top < top)
                {
                    top = region.Top;
                }

                if (region.Right > right)
                {
                    right = region.Right;
                }

                if (region.Bottom > bottom)
                {
                    bottom = region.Bottom;
                }
            }

            Debug.Assert(left <= right, "left should be less than or equal to right.");
            Debug.Assert(top <= bottom, "top should be less than or equal to bottom.");
        }

        internal void AddRegion(int rowIndex, int columnIndex, int rowCount, int columnCount)
        {
            AddRegion(rowIndex, columnIndex, rowCount, columnCount, /* notify = */ true);
        }

        private void AddRegion(int rowIndex, int columnIndex, int rowCount, int columnCount, bool notify)
        {
            Debug.Assert(rowCount > 0, "rowCount should be greater than 0.");
            Debug.Assert(columnCount > 0, "columnCount should be greater than 0.");

            List<CellRegion> addList = new List<CellRegion>();
            addList.Add(new CellRegion(columnIndex, rowIndex, columnCount, rowCount));

            // Cut down the region into only what is new.
            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = _regions[i];
                for (int c = 0; c < addList.Count; c++)
                {
                    CellRegion subRegion = addList[c];
                    List<CellRegion> remainder;
                    if (subRegion.Remainder(region, out remainder))
                    {
                        addList.RemoveAt(c);
                        c--;
                        if (remainder != null)
                        {
                            addList.AddRange(remainder);
                        }
                    }
                }
            }

            if (addList.Count > 0)
            {
                // Prepare the change notification collection (this makes a copy of addList)
                VirtualizedCellInfoCollection delta = new VirtualizedCellInfoCollection(_owner, addList);

                // Try to union the new regions to existing regions
                for (int i = 0; i < numRegions; i++)
                {
                    for (int c = 0; c < addList.Count; c++)
                    {
                        CellRegion region = _regions[i];
                        if (region.Union(addList[c]))
                        {
                            _regions[i] = region;
                            addList.RemoveAt(c);
                            c--;
                        }
                    }
                }

                // Add any regions that couldn't be unioned
                int numToAdd = addList.Count;
                for (int c = 0; c < numToAdd; c++)
                {
                    _regions.Add(addList[c]);
                }

                // Notification of a change in the collection
                if (notify)
                {
                    OnAdd(delta);
                }
            }
        }

        internal void RemoveRegion(int rowIndex, int columnIndex, int rowCount, int columnCount)
        {
            List<CellRegion> removeList = null;
            RemoveRegion(rowIndex, columnIndex, rowCount, columnCount, ref removeList);

            if ((removeList != null) && (removeList.Count > 0))
            {
                OnRemove(new VirtualizedCellInfoCollection(_owner, removeList));
            }
        }

        private void RemoveRegion(int rowIndex, int columnIndex, int rowCount, int columnCount, ref List<CellRegion> removeList)
        {
            if (!IsEmpty)
            {
                // Go through the regions, and try to remove from them
                CellRegion removeRegion = new CellRegion(columnIndex, rowIndex, columnCount, rowCount);
                for (int i = 0; i < _regions.Count; i++)
                {
                    CellRegion region = _regions[i];
                    CellRegion intersection = region.Intersection(removeRegion);
                    if (!intersection.IsEmpty)
                    {
                        // The two regions intersect
                        if (removeList == null)
                        {
                            removeList = new List<CellRegion>();
                        }

                        // We will remove the intersection
                        removeList.Add(intersection);

                        // The current region will be cut up with the intersection removed
                        _regions.RemoveAt(i);

                        List<CellRegion> remainder;
                        region.Remainder(intersection, out remainder);
                        if (remainder != null)
                        {
                            _regions.InsertRange(i, remainder);
                            i += remainder.Count; // Skip the remainder
                        }

                        i--; // One was removed
                    }
                }
            }
        }

        /// <summary>
        ///     Called by the DataGrid when Items.CollectionChanged is raised.
        /// </summary>
        internal void OnItemsCollectionChanged(NotifyCollectionChangedEventArgs e, List<Tuple<int,int>> ranges)
        {
            if (!IsEmpty)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        OnAddRow(e.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        OnRemoveRow(e.OldStartingIndex, e.OldItems[0]);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        OnReplaceRow(e.OldStartingIndex, e.OldItems[0]);
                        break;

                    case NotifyCollectionChangedAction.Move:
                        OnMoveRow(e.OldStartingIndex, e.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        RestoreOnlyFullRows(ranges);
                        break;
                }
            }
        }

        private void OnAddRow(int rowIndex)
        {
            Debug.Assert(rowIndex >= 0);

            List<CellRegion> keepRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            // Remove the region that is sliding over
            if (numColumns > 0)
            {
                RemoveRegion(rowIndex, 0, numItems - 1 - rowIndex, numColumns, ref keepRegions);

                if (keepRegions != null)
                {
                    // Add the kept region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = keepRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = keepRegions[i];
                        AddRegion(keptRegion.Top + 1, keptRegion.Left, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }
            }
        }

        private void OnRemoveRow(int rowIndex, object item)
        {
            Debug.Assert(rowIndex >= 0);

            List<CellRegion> keepRegions = null;
            List<CellRegion> removedRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            if (numColumns > 0)
            {
                // Remove the region that is sliding over
                RemoveRegion(rowIndex + 1, 0, numItems - rowIndex, numColumns, ref keepRegions);

                // Remove the region that was removed
                RemoveRegion(rowIndex, 0, 1, numColumns, ref removedRegions);

                if (keepRegions != null)
                {
                    // Add the kept region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = keepRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = keepRegions[i];
                        AddRegion(keptRegion.Top - 1, keptRegion.Left, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }

                if (removedRegions != null)
                {
                    // Create a special collection for the notification and notify of the change
                    RemovedCellInfoCollection removed = new RemovedCellInfoCollection(_owner, removedRegions, item);
                    OnRemove(removed);
                }
            }
        }

        private void OnReplaceRow(int rowIndex, object item)
        {
            Debug.Assert(rowIndex >= 0);

            // Remove the region that is being replaced
            List<CellRegion> removedRegions = null;
            RemoveRegion(rowIndex, 0, 1, _owner.Columns.Count, ref removedRegions);

            if (removedRegions != null)
            {
                // Create a special collection for the notification and notify of the change
                RemovedCellInfoCollection removed = new RemovedCellInfoCollection(_owner, removedRegions, item);
                OnRemove(removed);
            }
        }

        private void OnMoveRow(int oldIndex, int newIndex)
        {
            Debug.Assert(oldIndex >= 0);
            Debug.Assert(newIndex >= 0);

            List<CellRegion> slideRegions = null;
            List<CellRegion> movedRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            if (numColumns > 0)
            {
                // Remove the region that is sliding over
                RemoveRegion(oldIndex + 1, 0, numItems - oldIndex - 1, numColumns, ref slideRegions);

                // Remove the region that was moved
                RemoveRegion(oldIndex, 0, 1, numColumns, ref movedRegions);

                if (slideRegions != null)
                {
                    // Add the slide region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = slideRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = slideRegions[i];
                        AddRegion(keptRegion.Top - 1, keptRegion.Left, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }

                slideRegions = null;

                // Remove the region that is sliding over
                RemoveRegion(newIndex, 0, numItems - newIndex, numColumns, ref slideRegions);

                // Add the moved region
                if (movedRegions != null)
                {
                    int numMovedRegions = movedRegions.Count;
                    for (int i = 0; i < numMovedRegions; i++)
                    {
                        CellRegion movedRegion = movedRegions[i];
                        AddRegion(newIndex, movedRegion.Left, movedRegion.Height, movedRegion.Width, /* notify = */ false);
                    }
                }

                if (slideRegions != null)
                {
                    // Add the slide region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = slideRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = slideRegions[i];
                        AddRegion(keptRegion.Top + 1, keptRegion.Left, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }
            }
        }

        internal void OnColumnsChanged(NotifyCollectionChangedAction action, int oldDisplayIndex, DataGridColumn oldColumn, int newDisplayIndex, IList selectedRows)
        {
            if (!IsEmpty)
            {
                switch (action)
                {
                    case NotifyCollectionChangedAction.Add:
                        OnAddColumn(newDisplayIndex, selectedRows);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        OnRemoveColumn(oldDisplayIndex, oldColumn);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        OnReplaceColumn(oldDisplayIndex, oldColumn, selectedRows);
                        break;

                    case NotifyCollectionChangedAction.Move:
                        OnMoveColumn(oldDisplayIndex, newDisplayIndex);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        _regions.Clear();
                        break;
                }
            }
        }

        private void OnAddColumn(int columnIndex, IList selectedRows)
        {
            Debug.Assert(columnIndex >= 0);

            List<CellRegion> keepRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            if (numItems > 0)
            {
                // Remove the region that is sliding over
                RemoveRegion(0, columnIndex, numItems, numColumns - 1 - columnIndex, ref keepRegions);

                if (keepRegions != null)
                {
                    // Add the kept region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = keepRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = keepRegions[i];
                        AddRegion(keptRegion.Top, keptRegion.Left + 1, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }

                FillInFullRowRegions(selectedRows, columnIndex, /* notify = */ true);
            }
        }

        private void FillInFullRowRegions(IList rows, int columnIndex, bool notify)
        {
            int numRows = rows.Count;
            for (int i = 0; i < numRows; i++)
            {
                int rowIndex = _owner.Items.IndexOf(rows[i]);
                if (rowIndex >= 0)
                {
                    AddRegion(rowIndex, columnIndex, 1, 1, notify);
                }
            }
        }

        private void OnRemoveColumn(int columnIndex, DataGridColumn oldColumn)
        {
            Debug.Assert(columnIndex >= 0);

            List<CellRegion> keepRegions = null;
            List<CellRegion> removedRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            if (numItems > 0)
            {
                // Remove the region that is sliding over
                RemoveRegion(0, columnIndex + 1, numItems, numColumns - columnIndex, ref keepRegions);

                // Remove the region that was removed
                RemoveRegion(0, columnIndex, numItems, 1, ref removedRegions);

                if (keepRegions != null)
                {
                    // Add the kept region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = keepRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = keepRegions[i];
                        AddRegion(keptRegion.Top, keptRegion.Left - 1, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }

                if (removedRegions != null)
                {
                    // Create a special collection for the notification and notify of the change
                    RemovedCellInfoCollection removed = new RemovedCellInfoCollection(_owner, removedRegions, oldColumn);
                    OnRemove(removed);
                }
            }
        }

        private void OnReplaceColumn(int columnIndex, DataGridColumn oldColumn, IList selectedRows)
        {
            Debug.Assert(columnIndex >= 0);

            // Remove the region belonging to the column
            List<CellRegion> removedRegions = null;
            RemoveRegion(0, columnIndex, _owner.Items.Count, 1, ref removedRegions);

            if (removedRegions != null)
            {
                // Create a special collection for the notification and notify of the change
                RemovedCellInfoCollection removed = new RemovedCellInfoCollection(_owner, removedRegions, oldColumn);
                OnRemove(removed);
            }

            // Restore cells in full rows belonging to the new column
            FillInFullRowRegions(selectedRows, columnIndex, /* notify = */ true);
        }

        private void OnMoveColumn(int oldIndex, int newIndex)
        {
            Debug.Assert(oldIndex >= 0);
            Debug.Assert(newIndex >= 0);

            List<CellRegion> slideRegions = null;
            List<CellRegion> movedRegions = null;

            int numItems = _owner.Items.Count;
            int numColumns = _owner.Columns.Count;

            if (numItems > 0)
            {
                // Remove the region that is sliding over
                RemoveRegion(0, oldIndex + 1, numItems, numColumns - oldIndex - 1, ref slideRegions);

                // Remove the region that was removed
                RemoveRegion(0, oldIndex, numItems, 1, ref movedRegions);

                if (slideRegions != null)
                {
                    // Add the slide region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = slideRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = slideRegions[i];
                        AddRegion(keptRegion.Top, keptRegion.Left - 1, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }

                slideRegions = null;

                // Remove the region that is sliding over
                RemoveRegion(0, newIndex, numItems, numColumns - newIndex, ref slideRegions);

                if (movedRegions != null)
                {
                    int numMovedRegions = movedRegions.Count;
                    for (int i = 0; i < numMovedRegions; i++)
                    {
                        CellRegion movedRegion = movedRegions[i];
                        AddRegion(movedRegion.Top, newIndex, movedRegion.Height, movedRegion.Width, /* notify = */ false);
                    }
                }

                if (slideRegions != null)
                {
                    // Add the slide region back, shifted by 1. There is no need to notify since
                    // these are not considered changes.
                    int numKeptRegions = slideRegions.Count;
                    for (int i = 0; i < numKeptRegions; i++)
                    {
                        CellRegion keptRegion = slideRegions[i];
                        AddRegion(keptRegion.Top, keptRegion.Left + 1, keptRegion.Height, keptRegion.Width, /* notify = */ false);
                    }
                }
            }
        }

        /// <summary>
        ///     A special collection to fake removed columns or rows for change notifications.
        /// </summary>
        private class RemovedCellInfoCollection : VirtualizedCellInfoCollection
        {
            internal RemovedCellInfoCollection(DataGrid owner, List<CellRegion> regions, DataGridColumn column)
                : base(owner, regions)
            {
                _removedColumn = column;
            }

            internal RemovedCellInfoCollection(DataGrid owner, List<CellRegion> regions, object item)
                : base(owner, regions)
            {
                _removedItem = item;
            }

            protected override DataGridCellInfo CreateCellInfo(ItemsControl.ItemInfo rowInfo, DataGridColumn column, DataGrid owner)
            {
                if (_removedColumn != null)
                {
                    return new DataGridCellInfo(rowInfo, _removedColumn, owner);
                }
                else
                {
                    return new DataGridCellInfo(_removedItem, column, owner);
                }
            }

            private DataGridColumn _removedColumn;
            private object _removedItem;
        }

        #endregion

        #region DataGrid API

        /// <summary>
        ///     Merges another collection into this one.
        ///     Used for event argument coalescing.
        ///     This method should not be used for SelectedCellsCollection since it doesn't
        ///     coalesce the change notification.
        /// </summary>
        internal void Union(VirtualizedCellInfoCollection collection)
        {
            int numRegions = collection._regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = collection._regions[i];
                AddRegion(region.Top, region.Left, region.Height, region.Width);
            }
        }

        /// <summary>
        ///     Removes the intersection of the two collections from both collections.
        ///     Used for event argument coalescing.
        ///     This method should not be used for SelectedCellsCollection since it doesn't
        ///     coalesce the change notification.
        /// </summary>
        internal static void Xor(VirtualizedCellInfoCollection c1, VirtualizedCellInfoCollection c2)
        {
            VirtualizedCellInfoCollection orig2 = new VirtualizedCellInfoCollection(c2._owner, c2._regions);

            // Remove c1 regions from c2
            int numRegions = c1._regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = c1._regions[i];
                c2.RemoveRegion(region.Top, region.Left, region.Height, region.Width);
            }

            // Remove c2 regions from c1
            numRegions = orig2._regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = orig2._regions[i];
                c1.RemoveRegion(region.Top, region.Left, region.Height, region.Width);
            }
        }

        /// <summary>
        ///     Removes regions belonging to the list of full rows.
        /// </summary>
        internal void ClearFullRows(IList rows)
        {
            if (!IsEmpty)
            {
                int numColumns = _owner.Columns.Count;

                // Try to detect the common case that there is one block
                // of rows that is being cleared. In this case, just clearing
                // the cells is easier and faster.
                if (_regions.Count == 1)
                {
                    CellRegion region = _regions[0];
                    if ((region.Width == numColumns) && (region.Height == rows.Count))
                    {
                        Clear();
                        return;
                    }
                }

                // Go through the list and remove each row as a region
                List<CellRegion> removeList = new List<CellRegion>();
                int numRows = rows.Count;
                for (int i = 0; i < numRows; i++)
                {
                    int rowIndex = _owner.Items.IndexOf(rows[i]);
                    if (rowIndex >= 0)
                    {
                        RemoveRegion(rowIndex, 0, 1, numColumns, ref removeList);
                    }
                }

                if (removeList.Count > 0)
                {
                    OnRemove(new VirtualizedCellInfoCollection(_owner, removeList));
                }
            }
        }

        /// <summary>
        ///     Ensures that full rows in the list are selected.
        /// </summary>
        internal void RestoreOnlyFullRows(List<Tuple<int,int>> ranges)
        {
            Clear();

            int numColumns = _owner.Columns.Count;
            if (numColumns > 0)
            {
                foreach (Tuple<int,int> range in ranges)
                {
                    AddRegion(range.Item1, 0, range.Item2, numColumns);
                }
            }
        }

        /// <summary>
        ///     Clears the cells, leaving only one.
        /// </summary>
        internal void RemoveAllButOne(DataGridCellInfo cellInfo)
        {
            if (!IsEmpty)
            {
                int rowIndex;
                int columnIndex;
                ConvertCellInfoToIndexes(cellInfo, out rowIndex, out columnIndex);
                RemoveAllButRegion(rowIndex, columnIndex, 1, 1);
            }
        }

        /// <summary>
        ///     Clears the cells, leaving only one.
        /// </summary>
        internal void RemoveAllButOne()
        {
            if (!IsEmpty)
            {
                // Keep the first cell of the first region
                CellRegion firstRegion = _regions[0];
                RemoveAllButRegion(firstRegion.Top, firstRegion.Left, 1, 1);
            }
        }

        /// <summary>
        ///     Clears all of the cells except for the ones in the given row.
        /// </summary>
        internal void RemoveAllButOneRow(int rowIndex)
        {
            if (!IsEmpty && (rowIndex >= 0))
            {
                RemoveAllButRegion(rowIndex, 0, 1, _owner.Columns.Count);
            }
        }

        private void RemoveAllButRegion(int rowIndex, int columnIndex, int rowCount, int columnCount)
        {
            // Remove the region
            List<CellRegion> removeList = null;
            RemoveRegion(rowIndex, columnIndex, rowCount, columnCount, ref removeList);

            // Create the list of removed cells
            VirtualizedCellInfoCollection delta = new VirtualizedCellInfoCollection(_owner, _regions);

            // Update the regions list and add the kept region back
            _regions.Clear();
            _regions.Add(new CellRegion(columnIndex, rowIndex, columnCount, rowCount));

            // Notify of the change
            OnRemove(delta);
        }

        /// <summary>
        ///     Determines if the row has any cells in this collection.
        /// </summary>
        internal bool Intersects(int rowIndex)
        {
            CellRegion rowRegion = new CellRegion(0, rowIndex, _owner.Columns.Count, 1);

            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                if (_regions[i].Intersects(rowRegion))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Determines if the row has any cells in this collection and
        ///     returns the columns that are selected.
        /// </summary>
        /// <param name="columnIndexRanges">
        ///     An array where every two entries consitutes a pair consisting of
        ///     the starting index and the width that describe the column ranges
        ///     that intersect the row.
        /// </param>
        internal bool Intersects(int rowIndex, out List<int> columnIndexRanges)
        {
            CellRegion rowRegion = new CellRegion(0, rowIndex, _owner.Columns.Count, 1);
            columnIndexRanges = null;

            int numRegions = _regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = _regions[i];
                if (region.Intersects(rowRegion))
                {
                    if (columnIndexRanges == null)
                    {
                        columnIndexRanges = new List<int>();
                    }

                    columnIndexRanges.Add(region.Left);
                    columnIndexRanges.Add(region.Width);
                }
            }

            return columnIndexRanges != null;
        }

        #endregion

        #region Helpers

        protected DataGrid Owner
        {
            get { return _owner; }
        }

        /// <summary>
        ///     Converts a DataGridCellInfo into a row and column index.
        /// </summary>
        private void ConvertCellInfoToIndexes(DataGridCellInfo cell, out int rowIndex, out int columnIndex)
        {
            columnIndex = cell.Column.DisplayIndex;
            rowIndex = cell.ItemInfo.Index;

            if (rowIndex < 0)
            {
                rowIndex = _owner.Items.IndexOf(cell.Item);
            }
        }

        /// <summary>
        ///     Converts an index to a row and column index.
        /// </summary>
        private static void ConvertIndexToIndexes(List<CellRegion> regions, int index, out int rowIndex, out int columnIndex)
        {
            columnIndex = -1;
            rowIndex = -1;

            int numRegions = regions.Count;
            for (int i = 0; i < numRegions; i++)
            {
                CellRegion region = regions[i];
                int regionSize = region.Size;

                if (index < regionSize)
                {
                    columnIndex = region.Left + (index % region.Width);
                    rowIndex = region.Top + (index / region.Width);
                    break;
                }

                index -= regionSize;
            }
        }

        /// <summary>
        ///     Converts from an index to a DataGridCellInfo.
        /// </summary>
        private DataGridCellInfo GetCellInfoFromIndex(DataGrid owner, List<CellRegion> regions, int index)
        {
            int columnIndex;
            int rowIndex;

            ConvertIndexToIndexes(regions, index, out rowIndex, out columnIndex);

            if ((rowIndex >= 0) && (columnIndex >= 0) &&
                (rowIndex < owner.Items.Count) && (columnIndex < owner.Columns.Count))
            {
                DataGridColumn column = owner.ColumnFromDisplayIndex(columnIndex);
                ItemsControl.ItemInfo rowInfo = owner.ItemInfoFromIndex(rowIndex);

                return CreateCellInfo(rowInfo, column, owner);
            }
            else
            {
                return DataGridCellInfo.Unset;
            }
        }

        private void ValidateIsReadOnly()
        {
            if (IsReadOnly)
            {
                throw new NotSupportedException(SR.Get(SRID.VirtualizedCellInfoCollection_IsReadOnly));
            }
        }

        private void AddRegionToList(CellRegion region, List<DataGridCellInfo> list)
        {
            Debug.Assert(list != null, "list should not be null.");

            for (int rowIndex = region.Top; rowIndex <= region.Bottom; rowIndex++)
            {
                ItemsControl.ItemInfo rowInfo = _owner.ItemInfoFromIndex(rowIndex);
                for (int columnIndex = region.Left; columnIndex <= region.Right; columnIndex++)
                {
                    DataGridColumn column = _owner.ColumnFromDisplayIndex(columnIndex);
                    DataGridCellInfo cellInfo = CreateCellInfo(rowInfo, column, _owner);
                    list.Add(cellInfo);
                }
            }
        }

        /// <summary>
        ///     Overriden by collections faking removed columns and rows.
        /// </summary>
        protected virtual DataGridCellInfo CreateCellInfo(ItemsControl.ItemInfo rowInfo, DataGridColumn column, DataGrid owner)
        {
            return new DataGridCellInfo(rowInfo, column, owner);
        }

        #endregion

        #region Data

        private bool _isReadOnly;
        private DataGrid _owner;
        private List<CellRegion> _regions;

        #endregion
    }
}
