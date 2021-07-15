// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ItemContainerGenerator object
//
// Specs:       Data Styling.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;     // for CultureInfo.InvariantCulture (event tracing)

using System.Windows.Media;
using System.Windows.Controls.Primitives;   // IItemContainerGenerator
using System.Windows.Data;
using System.Windows.Markup;
using System.Diagnostics;
using System.Text;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;
using MS.Internal.Utility;
using MS.Utility;


namespace System.Windows.Controls
{
    /// <summary>
    /// An ItemContainerGenerator is responsible for generating the UI on behalf of
    /// its host (e.g. ItemsControl).  It maintains the association between the items in
    /// the control's data view and the corresponding
    /// UIElements.  The control's item-host can ask the ItemContainerGenerator for
    /// a Generator, which does the actual generation of UI.
    /// </summary>
    public sealed class ItemContainerGenerator : IRecyclingItemContainerGenerator, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor </summary>
        /// <parameter name="host"> the control that owns the items </parameter>
        internal ItemContainerGenerator(IGeneratorHost host)
            : this(null, host, host as DependencyObject, 0)
        {
            // The top-level generator always listens to changes from ItemsCollection.
            // It needs to get these events before anyone else, so that other listeners
            // can call the generator's mapping functions with correct results.
            CollectionChangedEventManager.AddHandler(host.View, OnCollectionChanged);
        }

        private ItemContainerGenerator(ItemContainerGenerator parent, GroupItem groupItem)
            : this(parent, parent.Host, groupItem, parent.Level + 1)
        {
        }

        private ItemContainerGenerator(ItemContainerGenerator parent, IGeneratorHost host, DependencyObject peer, int level)
        {
            _parent = parent;
            _host = host;
            _peer = peer;
            _level = level;
            OnRefresh();
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> The status of the generator </summary>
        public GeneratorStatus Status
        {
            get { return _status; }
        }

        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        private void SetStatus(GeneratorStatus value)
        {
            if (value != _status)
            {
                _status = value;

                switch (_status)
                {
                    case GeneratorStatus.GeneratingContainers:
                        if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info))
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "ItemsControl.Generator");
                            _itemsGenerated = 0;
                        }
                        else
                            _itemsGenerated = Int32.MinValue;
#if GENERATOR_TRACE
                        _creationTimer.Reset();
                        _timer.Begin();
#endif
                        break;

                    case GeneratorStatus.ContainersGenerated:
                        string label = null;
                        if (_itemsGenerated >= 0)   // this implies that tracing is enabled
                        {
                            DependencyObject d = Host as DependencyObject;
                            if (d != null)
                                label = (string)d.GetValue(FrameworkElement.NameProperty);
                            if (label == null || label.Length == 0)
                                label = Host.GetHashCode().ToString(CultureInfo.InvariantCulture);
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info,
                                                                 String.Format(CultureInfo.InvariantCulture, "ItemContainerGenerator for {0} {1} - {2} items", Host.GetType().Name, label, _itemsGenerated));
                        }
#if GENERATOR_TRACE
                        _timer.End();
                        if (_itemsGenerated > 0)
                        {
                            Console.WriteLine("Generator for {0} {1}  did {2} items in {3:f2} msec - {4:f2} msec/item",
                                Host.GetType().Name, label, _itemsGenerated, _timer.TimeOfLastPeriod, _timer.TimeOfLastPeriod/_itemsGenerated);
                            Console.WriteLine("  this excludes time for element creation: {0:f2} msec - {1:f2} msec/item",
                                _creationTimer.OverallTimeInMilliseconds, _creationTimer.OverallTimeInMilliseconds/_itemsGenerated);
                        }
#endif
                        break;
                }

                if (StatusChanged != null)
                    StatusChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Read-only access to the list of items.
        /// <summary>
        /// <notes>
        /// The returned collection is only valid until the next Refresh.  Users
        /// should not cache a reference to this collection.
        /// </notes>
        public ReadOnlyCollection<object> Items
        {
            get
            {
                // lazy creation
                if (_itemsReadOnly == null && _items != null)
                {
                    _itemsReadOnly = new ReadOnlyCollection<object>(new ListOfObject(_items));
                }

                return _itemsReadOnly;
            }
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region IItemContainerGenerator

        /// <summary>
        /// Return the ItemContainerGenerator appropriate for use by the given panel
        /// </summary>
        ItemContainerGenerator IItemContainerGenerator.GetItemContainerGeneratorForPanel(Panel panel)
        {
            if (!panel.IsItemsHost)
                throw new ArgumentException(SR.Get(SRID.PanelIsNotItemsHost), "panel");

            // if panel came from an ItemsPresenter, use its generator
            ItemsPresenter ip = ItemsPresenter.FromPanel(panel);
            if (ip != null)
                return ip.Generator;

            // if panel came from a style, use the main generator
            if (panel.TemplatedParent != null)
                return this;

            // otherwise the panel doesn't have a generator
            return null;
        }

        /// <summary> Begin generating at the given position and direction </summary>
        /// <remarks>
        /// This method must be called before calling GenerateNext.  It returns an
        /// IDisposable object that tracks the lifetime of the generation loop.
        /// This method sets the generator's status to GeneratingContent;  when
        /// the IDisposable is disposed, the status changes to ContentReady or
        /// Error, as appropriate.
        /// </remarks>
        IDisposable IItemContainerGenerator.StartAt(GeneratorPosition position, GeneratorDirection direction)
        {
            return ((IItemContainerGenerator)this).StartAt(position, direction, false);
        }

        /// <summary> Begin generating at the given position and direction </summary>
        /// <remarks>
        /// This method must be called before calling GenerateNext.  It returns an
        /// IDisposable object that tracks the lifetime of the generation loop.
        /// This method sets the generator's status to GeneratingContent;  when
        /// the IDisposable is disposed, the status changes to ContentReady or
        /// Error, as appropriate.
        /// </remarks>
        IDisposable IItemContainerGenerator.StartAt(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem)
        {
            if (_generator != null)
                throw new InvalidOperationException(SR.Get(SRID.GenerationInProgress));

            _generator = new Generator(this, position, direction, allowStartAtRealizedItem);
            return _generator;
        }

        public IDisposable GenerateBatches()
        {
            if (_isGeneratingBatches)
                throw new InvalidOperationException(SR.Get(SRID.GenerationInProgress));

            return new BatchGenerator(this);
        }

        DependencyObject IItemContainerGenerator.GenerateNext()
        {
            bool isNewlyRealized;
            if (_generator == null)
                throw new InvalidOperationException(SR.Get(SRID.GenerationNotInProgress));

            return _generator.GenerateNext(true, out isNewlyRealized);
        }

        DependencyObject IItemContainerGenerator.GenerateNext(out bool isNewlyRealized)
        {
            if (_generator == null)
                throw new InvalidOperationException(SR.Get(SRID.GenerationNotInProgress));

            return _generator.GenerateNext(false, out isNewlyRealized);
        }

        /// <summary>
        /// Prepare the given element to act as the container for the
        /// corresponding item.  This includes applying the container style,
        /// forwarding information from the host control (ItemTemplate, etc.),
        /// and other small adjustments.
        /// </summary>
        /// <remarks>
        /// This method must be called after the element has been added to the
        /// visual tree, so that resource references and inherited properties
        /// work correctly.
        /// </remarks>
        /// <param name="container"> The container to prepare.
        /// Normally this is the result of the previous call to GenerateNext.
        /// </param>
        void IItemContainerGenerator.PrepareItemContainer(DependencyObject container)
        {
            object item = container.ReadLocalValue(ItemForItemContainerProperty);
            Host.PrepareItemContainer(container, item);
        }

        /// <summary>
        /// Remove generated elements.
        /// </summary>
        void IItemContainerGenerator.Remove(GeneratorPosition position, int count)
        {
            Remove(position, count, /*isRecycling = */ false);
        }

        /// <summary>
        /// Remove generated elements.
        /// </summary>
        private void Remove(GeneratorPosition position, int count, bool isRecycling)
        {
            if (position.Offset != 0)
                throw new ArgumentException(SR.Get(SRID.RemoveRequiresOffsetZero, position.Index, position.Offset), "position");
            if (count <= 0)
                throw new ArgumentException(SR.Get(SRID.RemoveRequiresPositiveCount, count), "count");

            if (_itemMap == null)
            {
                // ignore reentrant call (during RemoveAllInternal)
                Debug.Assert(false, "Unexpected reentrant call to ICG.Remove");
                return;
            }

            int index = position.Index;
            ItemBlock block;

            // find the leftmost item to remove
            int offsetL = index;
            for (block = _itemMap.Next;  block != _itemMap;  block = block.Next)
            {
                if (offsetL < block.ContainerCount)
                    break;

                offsetL -= block.ContainerCount;
            }
            RealizedItemBlock blockL = block as RealizedItemBlock;

            // find the rightmost item to remove
            int offsetR = offsetL + count - 1;
            for (; block != _itemMap;  block = block.Next)
            {
                if (!(block is RealizedItemBlock))
                    throw new InvalidOperationException(SR.Get(SRID.CannotRemoveUnrealizedItems, index, count));

                if (offsetR < block.ContainerCount)
                    break;

                offsetR -= block.ContainerCount;
            }
            RealizedItemBlock blockR = block as RealizedItemBlock;

            // de-initialize the containers that are being removed
            RealizedItemBlock rblock = blockL;
            int offset = offsetL;
            while (rblock != blockR || offset <= offsetR)
            {
                DependencyObject container = rblock.ContainerAt(offset);

                UnlinkContainerFromItem(container, rblock.ItemAt(offset));
                // DataGrid generates non-GroupItem for NewItemPlaceHolder
                // Dont recycle in this case.
                bool isNewItemPlaceHolderWhenGrouping = _generatesGroupItems && !(container is GroupItem);

                if (isRecycling && !isNewItemPlaceHolderWhenGrouping)
                {
                    Debug.Assert(!_recyclableContainers.Contains(container), "trying to add a container to the collection twice");

                    if (_containerType == null)
                    {
                        _containerType = container.GetType();
                    }
                    else if (_containerType != container.GetType())
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CannotRecyleHeterogeneousTypes));
                    }

                    _recyclableContainers.Enqueue(container);
                }

                if (++offset >= rblock.ContainerCount && rblock != blockR)
                {
                    rblock = rblock.Next as RealizedItemBlock;
                    offset = 0;
                }
            }

            // see whether the range hits the edge of a block on either side,
            // and whether the a`butting block is an unrealized gap
            bool edgeL = (offsetL == 0);
            bool edgeR = (offsetR == blockR.ItemCount-1);
            bool abutL = edgeL && (blockL.Prev is UnrealizedItemBlock);
            bool abutR = edgeR && (blockR.Next is UnrealizedItemBlock);

            // determine the target (unrealized) block,
            // the offset within the target at which to insert items,
            // and the intial change in cumulative item count
            UnrealizedItemBlock blockT;
            ItemBlock predecessor = null;
            int offsetT;
            int deltaCount;

            if (abutL)
            {
                blockT = (UnrealizedItemBlock)blockL.Prev;
                offsetT = blockT.ItemCount;
                deltaCount = -blockT.ItemCount;
            }
            else if (abutR)
            {
                blockT = (UnrealizedItemBlock)blockR.Next;
                offsetT = 0;
                deltaCount = offsetL;
            }
            else
            {
                blockT = new UnrealizedItemBlock();
                offsetT = 0;
                deltaCount = offsetL;

                // remember where the new block goes, so we can insert it later
                predecessor = (edgeL) ? blockL.Prev : blockL;
            }

            // move items within the range to the target block
            for (block = blockL;  block != blockR;  block = block.Next)
            {
                int itemCount = block.ItemCount;
                MoveItems(block, offsetL, itemCount-offsetL,
                            blockT, offsetT, deltaCount);
                offsetT += itemCount-offsetL;
                offsetL = 0;
                deltaCount -= itemCount;
                if (block.ItemCount == 0)
                    block.Remove();
            }

            // the last block in the range is a little special...
            // Move the last unrealized piece.
            int remaining = block.ItemCount - 1 - offsetR;
            MoveItems(block, offsetL, offsetR - offsetL + 1,
                        blockT, offsetT, deltaCount);

            // Move the remaining realized items
            RealizedItemBlock blockX = blockR;
            if (!edgeR)
            {
                if (blockL == blockR && !edgeL)
                {
                    blockX = new RealizedItemBlock();
                }

                MoveItems(block, offsetR+1, remaining,
                            blockX, 0, offsetR+1);
            }

            // if we created any new blocks, insert them in the list
            if (predecessor != null)
                blockT.InsertAfter(predecessor);
            if (blockX != blockR)
                blockX.InsertAfter(blockT);

            RemoveAndCoalesceBlocksIfNeeded(block);
        }

        /// <summary>
        /// Remove all generated elements.
        /// </summary>
        void IItemContainerGenerator.RemoveAll()
        {
            RemoveAllInternal(false /*saveRecycleQueue*/);
        }

        internal void RemoveAllInternal(bool saveRecycleQueue)
        {
            // Take _itemMap offline, to protect against reentrancy (bug 1285179)
            ItemBlock itemMap = _itemMap;
            _itemMap = null;

            try
            {
                // de-initialize the containers that are being removed
                if (itemMap != null)
                {
                    for (ItemBlock block = itemMap.Next;  block != itemMap;  block = block.Next)
                    {
                        RealizedItemBlock rib = block as RealizedItemBlock;
                        if (rib != null)
                        {
                            for (int offset = 0; offset < rib.ContainerCount; ++offset)
                            {
                                UnlinkContainerFromItem(rib.ContainerAt(offset), rib.ItemAt(offset));
                            }
                        }
                    }
                }
            }
            finally
            {
                PrepareGrouping();

                // re-initialize the data structure
                _itemMap = new ItemBlock();
                _itemMap.Prev = _itemMap.Next = _itemMap;

                UnrealizedItemBlock uib = new UnrealizedItemBlock();
                uib.InsertAfter(_itemMap);
                uib.ItemCount = ItemsInternal.Count;

                if (!saveRecycleQueue)
                {
                    ResetRecyclableContainers();
                }

                SetAlternationCount();

                // tell generators what happened
                if (MapChanged != null)
                {
                    MapChanged(null, -1, 0, uib, 0, 0);
                }
            }
        }

        private void ResetRecyclableContainers()
        {
            _recyclableContainers = new Queue<DependencyObject>();
            _containerType = null;
            _generatesGroupItems = false;
        }

        void IRecyclingItemContainerGenerator.Recycle(GeneratorPosition position, int count)
        {
            Remove(position, count, /*isRecyling = */ true);
        }

        /// <summary>
        /// Map an index into the items collection to a GeneratorPosition.
        /// </summary>
        GeneratorPosition IItemContainerGenerator.GeneratorPositionFromIndex(int itemIndex)
        {
            GeneratorPosition position;
            ItemBlock itemBlock;
            int offsetFromBlockStart;

            GetBlockAndPosition(itemIndex, out position, out itemBlock, out offsetFromBlockStart);

            if (itemBlock == _itemMap && position.Index == -1)
                ++position.Offset;

            return position;
        }

        /// <summary>
        /// Map a GeneratorPosition to an index into the items collection.
        /// </summary>
        int IItemContainerGenerator.IndexFromGeneratorPosition(GeneratorPosition position)
        {
            int index = position.Index;

            if (index == -1)
            {
                // offset is relative to the fictitious boundary item
                if (position.Offset >= 0)
                {
                    return position.Offset - 1;
                }
                else
                {
                    return ItemsInternal.Count + position.Offset;
                }
            }

            if (_itemMap != null)
            {
                int itemIndex = 0;      // number of items we've skipped over

                // locate container at the given index
                for (ItemBlock block = _itemMap.Next;  block != _itemMap;  block = block.Next)
                {
                    if (index < block.ContainerCount)
                    {
                        // container is within this block.  return the answer
                        return itemIndex + index + position.Offset;
                    }
                    else
                    {
                        // skip over this block
                        itemIndex += block.ItemCount;
                        index -= block.ContainerCount;
                    }
                }
            }

            return -1;
        }

        #endregion IItemContainerGenerator

        /// <summary>
        /// Return the item corresponding to the given UI element.
        /// If the element was not generated as a container for this generator's
        /// host, the method returns DependencyProperty.UnsetValue.
        /// </summary>
        public object ItemFromContainer(DependencyObject container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            object item = container.ReadLocalValue(ItemForItemContainerProperty);

            if (item != DependencyProperty.UnsetValue)
            {
                // verify that the element really belongs to the host
                if (!Host.IsHostForItemContainer(container))
                    item = DependencyProperty.UnsetValue;
            }

            return item;
        }

        /// <summary>
        /// Return the UI element corresponding to the given item.
        /// Returns null if the item does not belong to the item collection,
        /// or if no UI has been generated for it.
        /// </summary>
        public DependencyObject ContainerFromItem(object item)
        {
            object dummy;
            DependencyObject container;
            int index;

            DoLinearSearch(
                delegate(object o, DependencyObject d) { return ItemsControl.EqualsEx(o, item); },
                out dummy, out container, out index, false);

            return container;
        }

        /// <summary>
        /// Given a generated UI element, return the index of the corresponding item
        /// within the ItemCollection.
        /// </summary>
        public int IndexFromContainer(DependencyObject container)
        {
            return IndexFromContainer(container, false);
        }

        /// <summary>
        /// Given a generated UI element, return the index of the corresponding item
        /// within the ItemCollection.
        /// </summary>
        public int IndexFromContainer(DependencyObject container, bool returnLocalIndex)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            int index;
            object item;
            DependencyObject dummy;

            DoLinearSearch(
                delegate(object o, DependencyObject d) { return (d == container); },
                out item, out dummy, out index, returnLocalIndex);

            return index;
        }

        // expose DoLinearSearch to internal code
        internal bool FindItem(Func<object, DependencyObject, bool> match,
                out DependencyObject container, out int itemIndex)
        {
            object item;
            return DoLinearSearch(match, out item, out container, out itemIndex, false);
        }

        /// <summary>
        ///     Performs a linear search for an (item, container) pair that
        ///     matches a given predicate.
        /// </summary>
        /// <remarks>
        ///     There's no avoiding a linear search, which leads to O(n^2) performance
        ///     if someone calls ContainerFromItem or IndexFromContainer for every item.
        ///     To mitigate this, we start each search at _startIndexForUIFromItem, and
        ///     heuristically set this in various places to where we expect the next
        ///     call to occur.
        ///
        ///     For example, after a successul search, we set it to the resulting
        ///     index, hoping that the next call will query either the same item or
        ///     the one after it.  And after inserting a new item, we expect a query
        ///     about the new item.  Etc.
        ///
        ///     Saving this as an index instead of a (block, offset) pair, makes it
        ///     more robust during insertions/deletions.  If the index ends up being
        ///     wrong, the worst that happens is a full search (as opposed to following
        ///     a reference to a block that's no longer in use).
        ///
        ///     To re-use the search code for two methods, please read the description
        ///     of the parameters.
        /// </remarks>
        /// <param name="match">
        ///     The predicate with which to test each (item, container).
        /// </param>
        /// <param name="returnLocalIndex">
        ///     If true, only search at the current level and return an index
        ///         in local coordinates (w.r.t. the current level).
        ///     If false, search subgroups, and return an index in global coordinates.
        /// </param>
        /// <param name="item">
        ///     The matching item, or null
        /// </param>
        /// <param name="container">
        ///     The matching container, or null
        /// </param>
        /// <param name="itemIndex">
        ///     The index of the matching pair, or -1
        /// </param>
        /// <returns>
        ///     true if found, false otherwise.
        /// </returns>
        private bool DoLinearSearch(Func<object, DependencyObject, bool> match,
                out object item, out DependencyObject container, out int itemIndex,
                bool returnLocalIndex)
        {
            item = null;
            container = null;
            itemIndex = 0;

            if (_itemMap == null)
            {
                // _itemMap can be null if we re-enter the generator.  Scenario:  user calls RemoveAll(), we Unlink every container, fire
                // ClearContainerForItem for each, and someone overriding ClearContainerForItem decides to look up the container.
                goto NotFound;
            }

            // Move to the starting point of the search
            ItemBlock startBlock = _itemMap.Next;
            int index = 0;      // index of first item in current block
            RealizedItemBlock rib;
            int startOffset;

            while (index <= _startIndexForUIFromItem && startBlock != _itemMap)
            {
                index += startBlock.ItemCount;
                startBlock = startBlock.Next;
            }
            startBlock = startBlock.Prev;
            index -= startBlock.ItemCount;
            rib = startBlock as RealizedItemBlock;

            if (rib != null)
            {
                startOffset = _startIndexForUIFromItem - index;
                if (startOffset >= rib.ItemCount)
                {
                    // we can get here if items get removed since the last
                    // time we saved _startIndexForUIFromItem - so the
                    // saved offset is no longer meaningful.  To make the
                    // search work, we need to make sure the first loop
                    // does at least one iteration.  Setting startOffset to 0
                    // does exactly that.
                    startOffset = 0;
                }
            }
            else
            {
                startOffset = 0;
            }

            // search for the desired item, wrapping around the end
            ItemBlock block = startBlock;
            int offset = startOffset;
            int endOffset = startBlock.ItemCount;
            while (true)
            {
                // search the current block (only need to search realized blocks)
                if (rib != null)
                {
                    for (; offset < endOffset; ++offset)
                    {
                        CollectionViewGroup group;
                        bool found = match(rib.ItemAt(offset), rib.ContainerAt(offset));

                        if (found)
                        {
                            item = rib.ItemAt(offset);
                            container = rib.ContainerAt(offset);
                        }
                        else if (!returnLocalIndex && IsGrouping && ((group = rib.ItemAt(offset) as CollectionViewGroup) != null))
                        {
                            // found a group;  see if the group contains the item
                            GroupItem groupItem = (GroupItem)rib.ContainerAt(offset);
                            int indexInGroup;
                            found = groupItem.Generator.DoLinearSearch(match, out item, out container, out indexInGroup, false);
                            if (found)
                            {
                                itemIndex = indexInGroup;
                            }
                        }

                        if (found)
                        {
                            // found the item;  update state and return
                            _startIndexForUIFromItem = index + offset;
                            itemIndex += GetRealizedItemBlockCount(rib, offset, returnLocalIndex) + GetCount(block, returnLocalIndex);
                            return true;
                        }
                    }

                    // check for termination
                    if (block == startBlock && offset == startOffset)
                    {
                        break;  // not found
                    }
                }

                // advance to next block
                index += block.ItemCount;
                offset = 0;
                block = block.Next;

                // if we've reached the end, wrap around
                if (block == _itemMap)
                {
                    block = block.Next;
                    index = 0;
                }

                // prepare to search the block
                endOffset = block.ItemCount;
                rib = block as RealizedItemBlock;

                // check for termination
                if (block == startBlock)
                {
                    if (rib != null)
                    {
                        endOffset = startOffset;    // search first part of block
                    }
                    else
                    {
                        break;  // not found
                    }
                }
            }

        NotFound:
            itemIndex = -1;
            item = null;
            container = null;
            return false;
        }

        private int GetCount()
        {
            return GetCount(_itemMap);
        }

        private int GetCount(ItemBlock stop)
        {
            return GetCount(stop, false);
        }

        private int GetCount(ItemBlock stop, bool returnLocalIndex)
        {
            if (_itemMap == null)
            {
                // handle reentrant call
                return 0;
            }

            int count = 0;
            ItemBlock start = _itemMap;
            ItemBlock block = start.Next;

            while (block != stop)
            {
                count += block.ItemCount;
                block = block.Next;
            }

            if (!returnLocalIndex && IsGrouping)
            {
                int n = count;
                count = 0;

                for (int i=0; i<n; ++i)
                {
                    CollectionViewGroup group = Items[i] as CollectionViewGroup;
                    count += (group == null) ? 1 : group.ItemCount;
                }
            }

            return count;
        }

        private int GetRealizedItemBlockCount(RealizedItemBlock rib, int end, bool returnLocalIndex)
        {
            if (!IsGrouping || returnLocalIndex)
            {
                // when the UI is not grouping, each item counts as 1, even
                // groups (bug 1761421)
                return end;
            }

            int count = 0;

            for (int offset = 0; offset < end; ++offset)
            {
                CollectionViewGroup group;
                if ((group = rib.ItemAt(offset) as CollectionViewGroup) != null)
                {
                    // found a group, count the group
                    count += group.ItemCount;
                }
                else
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Return the UI element corresponding to the item at the given index
        /// within the ItemCollection.
        /// </summary>
        public DependencyObject ContainerFromIndex(int index)
        {
            if (_itemMap == null)
            {
                // handle reentrant call
                return null;
            }

#if DEBUG
            object target = (Parent == null) && (0 <= index  &&  index < Host.View.Count) ? Host.View[index] : null;
#endif
            int subIndex = 0;

            // if we're grouping, determine the appropriate child
            if (IsGrouping)
            {
                int n;
                subIndex = index;
                for (index=0, n=ItemsInternal.Count;  index < n;  ++index)
                {
                    CollectionViewGroup group = ItemsInternal[index] as CollectionViewGroup;
                    int size = (group == null) ? 1 : group.ItemCount;

                    if (subIndex < size)
                        break;
                    else
                        subIndex -= size;
                }
            }

            // search the table for the item

            for (ItemBlock block = _itemMap.Next; block != _itemMap; block = block.Next)
            {
                if (index < block.ItemCount)
                {
                    DependencyObject container = block.ContainerAt(index);
                    GroupItem groupItem = container as GroupItem;

                    if (groupItem != null)
                    {
                        container = groupItem.Generator.ContainerFromIndex(subIndex);
                    }
#if DEBUG
                    object item = (Parent == null) && (container != null) ?
                                container.ReadLocalValue(ItemForItemContainerProperty) : null;
                    Debug.Assert(item == null || ItemsControl.EqualsEx(item, target),
                        "Generator's data structure is corrupt - ContainerFromIndex found wrong item");
#endif
                    return container;
                }

                index -= block.ItemCount;
            }

            return null;  // *not* throw new IndexOutOfRangeException(); - bug 890195
        }


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        /// The ItemsChanged event is raised by a ItemContainerGenerator to inform
        /// layouts that the items collection has changed.
        /// </summary>
        public event ItemsChangedEventHandler ItemsChanged;

        /// <summary>
        /// The StatusChanged event is raised by a ItemContainerGenerator to inform
        /// controls that its status has changed.
        /// </summary>
        public event EventHandler StatusChanged;


        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        // ItemsControl sometimes needs access to the recyclable containers.
        // For eg. DataGrid needs to mark recyclable containers dirty for measure when DataGridColumn.Visibility changes.
        internal IEnumerable RecyclableContainers
        {
            get
            {
                return _recyclableContainers;
            }
        }

        // regenerate everything
        internal void Refresh()
        {
            OnRefresh();
        }

        // called when this generator is no longer needed
        internal void Release()
        {
            ((IItemContainerGenerator)this).RemoveAll();
        }

        // called when GenerateNext returns null when the caller wasn't expecting null.
        // This is a clue that the underlying collection or collection-view may
        // have raised the wrong CollectionChange events.  If there's evidence
        // that this has happened, throw an exception.
        internal void Verify()
        {
            if (_itemMap == null)
                return;

            List<string> errors = new List<string>();

            // compute accumulated count = sum of block counts
            int accumulatedCount = 0;
            for (ItemBlock block = _itemMap.Next;  block != _itemMap;  block = block.Next)
            {
                accumulatedCount += block.ItemCount;
            }

            // compare accumulated count to actual count
            if (accumulatedCount != _items.Count)
            {
                errors.Add(SR.Get(SRID.Generator_CountIsWrong, accumulatedCount, _items.Count));
            }

            // compare items
            int badItems=0, reportedItems=0;
            int blockIndex=0;
            for (ItemBlock block = _itemMap.Next;  block != _itemMap;  block = block.Next)
            {
                RealizedItemBlock rib = block as RealizedItemBlock;
                if (rib != null)
                {
                    for (int offset=0; offset<rib.ItemCount; ++offset)
                    {
                        int index = blockIndex + offset;
                        object genItem = rib.ItemAt(offset);
                        object actualItem = (index < _items.Count) ? _items[index] : null;
                        if (!ItemsControl.EqualsEx(genItem, actualItem))
                        {
                            if (reportedItems < 3)
                            {
                                errors.Add(SR.Get(SRID.Generator_ItemIsWrong, index, genItem, actualItem));
                                ++ reportedItems;
                            }
                            ++ badItems;
                        }
                    }
                }
                blockIndex += block.ItemCount;
            }

            if (badItems > reportedItems)
            {
                errors.Add(SR.Get(SRID.Generator_MoreErrors, badItems - reportedItems));
            }

            // if we found errors, throw an exception
            if (errors.Count > 0)
            {
                CultureInfo enUS = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

                // get the identifying information for the ItemsControl
                DependencyObject peer = Peer;
                string name = (String)peer.GetValue(FrameworkElement.NameProperty);
                if (String.IsNullOrWhiteSpace(name))
                {
                    name = SR.Get(SRID.Generator_Unnamed);
                }

                // get the sources involved in CollectionChanged events
                List<string> sources = new List<string>();
                GetCollectionChangedSources(0, FormatCollectionChangedSource, sources);

                // describe the details of the problem
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(SR.Get(SRID.Generator_Readme0));                          // Developer info:
                sb.Append    (SR.Get(SRID.Generator_Readme1, peer, name));              // The exception is thrown because...
                sb.Append("  ");
                sb.AppendLine(SR.Get(SRID.Generator_Readme2));                          // The following differences...
                foreach (string s in errors)
                {
                    sb.AppendFormat(enUS, "  {0}", s);
                    sb.AppendLine();
                }
                sb.AppendLine();

                sb.AppendLine(SR.Get(SRID.Generator_Readme3));                          // The following sources...
                foreach (string s in sources)
                {
                    sb.AppendFormat(enUS, "  {0}", s);
                    sb.AppendLine();
                }
                sb.AppendLine(SR.Get(SRID.Generator_Readme4));                          // Starred sources are considered more likely
                sb.AppendLine();

                sb.AppendLine(SR.Get(SRID.Generator_Readme5));                          // The most common causes...
                sb.AppendLine();

                sb.Append    (SR.Get(SRID.Generator_Readme6)); sb.Append("  ");         // Stack trace describes detection...
                sb.Append    (SR.Get(SRID.Generator_Readme7,                            // To get better detection...
                                "PresentationTraceSources.TraceLevel", "High"));
                sb.Append    ("  ");
                sb.AppendLine(SR.Get(SRID.Generator_Readme8,                            // One way to do this ...
                                "System.Diagnostics.PresentationTraceSources.SetTraceLevel(myItemsControl.ItemContainerGenerator, System.Diagnostics.PresentationTraceLevel.High)"));
                sb.AppendLine(SR.Get(SRID.Generator_Readme9));                          // This slows down the app.

                // use an inner exception to hold the details.  There's a lot of
                // information, but it's only interesting to a developer.
                Exception exception = new Exception(sb.ToString());

                // throw the exception
                throw new InvalidOperationException(SR.Get(SRID.Generator_Inconsistent), exception);
            }
        }

        void FormatCollectionChangedSource(int level, object source, bool? isLikely, List<string> sources)
        {
            Type sourceType = source.GetType();

            if (!isLikely.HasValue)
            {
                // if the type doesn't come from WPF or DevDiv (e.g. ObservableCollection<T>),
                // mark it as "more likely to be at fault".   I'm not saying we're always right,
                // just that 3rd parties are more likely to be wrong than we are.
                isLikely = true;

                const string PublicKeyToken = "PublicKeyToken=";
                string aqn = sourceType.AssemblyQualifiedName;
                int index = aqn.LastIndexOf(PublicKeyToken);
                if (index >= 0)
                {
                    string token = aqn.Substring(index + PublicKeyToken.Length);
                    if (String.Compare(token, MS.Internal.PresentationFramework.BuildInfo.WCP_PUBLIC_KEY_TOKEN, StringComparison.OrdinalIgnoreCase) == 0 ||
                        String.Compare(token, MS.Internal.PresentationFramework.BuildInfo.DEVDIV_PUBLIC_KEY_TOKEN, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        isLikely = false;
                    }
                }
            }

            char c = (isLikely == true) ? '*' : ' ';
            string indent = new String(' ', level);
            sources.Add(String.Format(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS, "{0} {1} {2}",
                                        c, indent, sourceType.FullName));
        }

        void GetCollectionChangedSources(int level, Action<int, object, bool?, List<string>> format, List<string> sources)
        {
            format(level, this, false, sources);
            Host.View.GetCollectionChangedSources(level+1, format, sources);
        }

        // called when the host's AlternationCount changes
        internal void ChangeAlternationCount()
        {
            if (_itemMap == null)
            {
                // handle reentrant call
                return;
            }

            // update my AlternationCount and adjust my containers
            SetAlternationCount();

            // propagate to subgroups, if necessary
            if (IsGrouping && GroupStyle != null)
            {
                ItemBlock block = _itemMap.Next;
                while (block != _itemMap)
                {
                    for (int offset = 0;  offset < block.ContainerCount;  ++offset)
                    {
                        GroupItem gi = ((RealizedItemBlock)block).ContainerAt(offset) as GroupItem;
                        if (gi != null)
                        {
                            gi.Generator.ChangeAlternationCount();
                        }
                    }

                    block = block.Next;
                }
            }
        }

        // update AlternationIndex on each container to reflect the new AlternationCount
        void ChangeAlternationCount(int newAlternationCount)
        {
            if (_alternationCount == newAlternationCount)
                return;

            // find the first realized container (need this regardless of what happens)
            ItemBlock block = _itemMap.Next;
            int offset = 0;
            while (offset == block.ContainerCount)
            {
                block = block.Next;
            }

            // if there are no realized containers, there's nothing to do
            if (block != _itemMap)
            {
                // if user is requesting alternation, reset each container's AlternationIndex
                if (newAlternationCount > 0)
                {
                    _alternationCount = newAlternationCount;
                    SetAlternationIndex((RealizedItemBlock)block, offset, GeneratorDirection.Forward);
                }
                // otherwise, clear each container's AlternationIndex
                else if (_alternationCount > 0)
                {
                    while (block != _itemMap)
                    {
                        for (offset = 0;  offset < block.ContainerCount;  ++offset)
                        {
                            ItemsControl.ClearAlternationIndex(((RealizedItemBlock)block).ContainerAt(offset));
                        }

                        block = block.Next;
                    }
                }
            }

            _alternationCount = newAlternationCount;
        }

        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        internal ItemContainerGenerator Parent
        {
            get { return _parent;}
        }

        internal int Level
        {
            get { return _level;}
        }

        // The group style that governs the generation of UI for the items.
        internal GroupStyle GroupStyle
        {
            get { return _groupStyle; }
            set
            {
                if (_groupStyle != value)
                {
                    if (_groupStyle is INotifyPropertyChanged)
                    {
                        PropertyChangedEventManager.RemoveHandler(_groupStyle, OnGroupStylePropertyChanged, String.Empty);
                    }

                    _groupStyle = value;

                    if (_groupStyle is INotifyPropertyChanged)
                    {
                        PropertyChangedEventManager.AddHandler(_groupStyle, OnGroupStylePropertyChanged, String.Empty);
                    }
                }
            }
        }

        // The collection of items, as IList
        internal IList ItemsInternal
        {
            get { return _items; }
            set
            {
                if (_items != value)
                {
                    INotifyCollectionChanged incc = _items as INotifyCollectionChanged;
                    if (_items != Host.View && incc != null)
                    {
                        CollectionChangedEventManager.RemoveHandler(incc, OnCollectionChanged);
                    }

                    _items = value;
                    _itemsReadOnly = null;

                    incc = _items as INotifyCollectionChanged;
                    if (_items != Host.View && incc != null)
                    {
                        CollectionChangedEventManager.AddHandler(incc, OnCollectionChanged);
                    }
                }
            }
        }

        /// <summary>
        ///     ItemForItemContainer DependencyProperty
        /// </summary>
        // This is an attached property that the generator sets on each container
        // (generated or direct) to point back to the item.
        internal static readonly DependencyProperty ItemForItemContainerProperty =
                DependencyProperty.RegisterAttached("ItemForItemContainer", typeof(object), typeof(ItemContainerGenerator),
                                            new FrameworkPropertyMetadata((object)null));

        //------------------------------------------------------
        //
        //  Internal events
        //
        //------------------------------------------------------

        internal event EventHandler PanelChanged;

        internal void OnPanelChanged()
        {
            if (PanelChanged != null)
                PanelChanged(this, EventArgs.Empty);
        }

        //------------------------------------------------------
        //
        //  Private Nested Class -  ItemContainerGenerator.Generator
        //
        //------------------------------------------------------


        /// <summary>
        ///     Generator is the object that generates UI on behalf of an ItemsControl,
        ///     working under the supervision of an ItemContainerGenerator.
        /// </summary>
        private class Generator : IDisposable
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            internal Generator(ItemContainerGenerator factory, GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem)
            {
                _factory = factory;
                _direction = direction;

                _factory.MapChanged += new MapChangedHandler(OnMapChanged);

                _factory.MoveToPosition(position, direction, allowStartAtRealizedItem, ref _cachedState);
                _done = (_factory.ItemsInternal.Count == 0);

                _factory.SetStatus(GeneratorStatus.GeneratingContainers);
            }

            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

/* This method was requested for virtualization.  It's not being used right now
(bug 1079525) but it probably will be when UI virtualization comes back.
            /// <summary>
            /// returns false if a call to GenerateNext is known to return null (indicating
            /// that the generator is done).  Does not generate anything or change the
            /// generator's state;  cheaper than GenerateNext.  Returning true does not
            /// necessarily mean GenerateNext will produce anything.
            /// </summary>
            public bool IsActive
            {
                get { return !_done; }
            }
*/

            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            /// <summary> Generate UI for the next item or group</summary>
            public DependencyObject GenerateNext(bool stopAtRealized, out bool isNewlyRealized)
            {
                DependencyObject container = null;
                isNewlyRealized = false;

                while (container == null)
                {
                    UnrealizedItemBlock uBlock = _cachedState.Block as UnrealizedItemBlock;
                    IList items = _factory.ItemsInternal;
                    int itemIndex = _cachedState.ItemIndex;
                    int incr = (_direction == GeneratorDirection.Forward) ? +1 : -1;

                    if (_cachedState.Block == _factory._itemMap)
                        _done = true;            // we've reached the end of the list

                    if (uBlock == null && stopAtRealized)
                        _done = true;

                    if (!(0 <= itemIndex && itemIndex < items.Count))
                        _done = true;

                    if (_done)
                    {
                        isNewlyRealized = false;
                        return null;
                    }

                    object item = items[itemIndex];

                    if (uBlock != null)
                    {
                        // We don't have a realized container for this item.  Try to use a recycled container
                        // if possible, otherwise generate a new container.

                        isNewlyRealized = true;
                        CollectionViewGroup group = item as CollectionViewGroup;

                        // DataGrid needs to generate DataGridRows for special items like NewItemPlaceHolder and when adding a new row.
                        // Generate a new container for such cases.
                        bool isNewItemPlaceHolderWhenGrouping = (_factory._generatesGroupItems && group == null);

                        if (_factory._recyclableContainers.Count > 0 && !_factory.Host.IsItemItsOwnContainer(item) && !isNewItemPlaceHolderWhenGrouping)
                        {
                            container = _factory._recyclableContainers.Dequeue();
                            isNewlyRealized = false;
                        }
                        else
                        {
                            if (group == null || !_factory.IsGrouping)
                            {
                                // generate container for an item
                                container = _factory.Host.GetContainerForItem(item);
                            }
                            else
                            {
                                // generate container for a group
                                container = _factory.ContainerForGroup(group);
                            }
                        }

                        // add the (item, container) to the current block
                        if (container != null)
                        {
                            ItemContainerGenerator.LinkContainerToItem(container, item);

                            _factory.Realize(uBlock, _cachedState.Offset, item, container);

                            // set AlternationIndex on the container (and possibly others)
                            _factory.SetAlternationIndex(_cachedState.Block, _cachedState.Offset, _direction);
                        }
                    }
                    else
                    {
                        // return existing realized container
                        isNewlyRealized = false;
                        RealizedItemBlock rib = (RealizedItemBlock)_cachedState.Block;
                        container = rib.ContainerAt(_cachedState.Offset);
                    }

                    // advance to the next item
                    _cachedState.ItemIndex = itemIndex;
                    if (_direction == GeneratorDirection.Forward)
                    {
                        _cachedState.Block.MoveForward(ref _cachedState, true);
                    }
                    else
                    {
                        _cachedState.Block.MoveBackward(ref _cachedState, true);
                    }
                }

                return container;
            }

            //------------------------------------------------------
            //
            //  Interfaces - IDisposable
            //
            //------------------------------------------------------

            /// <summary> Dispose this generator. </summary>
            void IDisposable.Dispose()
            {
                if (_factory != null)
                {
                    _factory.MapChanged -= new MapChangedHandler(OnMapChanged);
                    _done = true;
                    if (!_factory._isGeneratingBatches)
                    {
                        _factory.SetStatus(GeneratorStatus.ContainersGenerated);
                    }
                    _factory._generator = null;
                    _factory = null;
                }

                GC.SuppressFinalize(this);
            }

            //------------------------------------------------------
            //
            //  Private methods
            //
            //------------------------------------------------------

            // The map data structure has changed, so the state must change accordingly.
            // This is called in various different ways.
            //  A. Items were moved within the data structure, typically because
            //  items were realized or un-realized.  In this case, the args are:
            //      block - the block from where the items were moved
            //      offset - the offset within the block of the first item moved
            //      count - how many items moved
            //      newBlock - the block to which the items were moved
            //      newOffset - the offset within the new block of the first item moved
            //      deltaCount - the difference between the cumululative item counts
            //                  of newBlock and block
            //  B. An item was added or removed from the data structure.  In this
            //  case the args are:
            //      block - null  (to distinguish case B from case A)
            //      offset - the index of the changed item, w.r.t. the entire item list
            //      count - +1 for insertion, -1 for deletion
            //      newBlock - block where item was inserted (null for deletion)
            //  C. Refresh: all items are returned to a single unrealized block.
            //  In this case, the args are:
            //      block - null
            //      offset - -1 (to distinguish case C from case B)
            //      newBlock = the single unrealized block
            //      others - unused
            void OnMapChanged(ItemBlock block, int offset, int count,
                            ItemBlock newBlock, int newOffset, int deltaCount)
            {
                // Case A.  Items were moved within the map data structure
                if (block != null)
                {
                    // if the move affects this generator, update the cached state
                    if (block == _cachedState.Block && offset <= _cachedState.Offset &&
                        _cachedState.Offset < offset + count)
                    {
                        _cachedState.Block = newBlock;
                        _cachedState.Offset += newOffset - offset;
                        _cachedState.Count += deltaCount;
                    }
                }
                // Case B.  An item was inserted or deleted
                else if (offset >= 0)
                {
                    // if the item occurs before my block, update my item count
                    if (offset < _cachedState.Count ||
                        (offset == _cachedState.Count && newBlock != null && newBlock != _cachedState.Block))
                    {
                        _cachedState.Count += count;
                        _cachedState.ItemIndex += count;
                    }
                    // if the item occurs within my block before my item, update my offset
                    else if (offset < _cachedState.Count + _cachedState.Offset)
                    {
                        _cachedState.Offset += count;
                        _cachedState.ItemIndex += count;
                    }
                    // if the item occurs at my position, ...
                    else if (offset == _cachedState.Count + _cachedState.Offset)
                    {
                        if (count > 0)
                        {
                            // for insert, update my offset
                            _cachedState.Offset += count;
                            _cachedState.ItemIndex += count;
                        }
                        else if (_cachedState.Offset == _cachedState.Block.ItemCount)
                        {
                            // if deleting last item in the block, advance to the next block
                            _cachedState.Block = _cachedState.Block.Next;
                            _cachedState.Offset = 0;
                        }
                    }
                }
                // Case C.  Refresh
                else
                {
                    _cachedState.Block = newBlock;
                    _cachedState.Offset += _cachedState.Count;
                    _cachedState.Count = 0;
                }
            }

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            ItemContainerGenerator     _factory;
            GeneratorDirection  _direction;
            bool                _done;
            GeneratorState      _cachedState;
        }

        private class BatchGenerator : IDisposable
        {
            public BatchGenerator(ItemContainerGenerator factory)
            {
                _factory = factory;
                _factory._isGeneratingBatches = true;
                _factory.SetStatus(GeneratorStatus.GeneratingContainers);
            }

            void IDisposable.Dispose()
            {
                if (_factory != null)
                {
                    _factory._isGeneratingBatches = false;
                    _factory.SetStatus(GeneratorStatus.ContainersGenerated);
                    _factory = null;
                }
                GC.SuppressFinalize(this);
            }

            ItemContainerGenerator _factory;
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        IGeneratorHost Host { get { return _host; } }

        // The DO for which this generator was created.  For normal generators,
        // this is the ItemsControl.  For subgroup generators, this is
        // the GroupItem.
        DependencyObject Peer
        {
            get { return _peer; }
        }

        bool IsGrouping
        {
            get { return (ItemsInternal != Host.View); }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        void MoveToPosition(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem, ref GeneratorState state)
        {
            ItemBlock block = _itemMap;
            if (block == null)
                return;         // this can happen in event-leapfrogging situations

            int itemIndex = 0;

            // first move to the indexed (realized) item
            if (position.Index != -1)
            {
                // find the right block
                int itemCount = 0;
                int index = position.Index;
                block = block.Next;
                while (index >= block.ContainerCount)
                {
                    itemCount += block.ItemCount;
                    index -= block.ContainerCount;
                    itemIndex += block.ItemCount;
                    block = block.Next;
                }

                // set the position
                state.Block = block;
                state.Offset = index;
                state.Count = itemCount;
                state.ItemIndex = itemIndex + index;
            }
            else
            {
                state.Block = block;
                state.Offset = 0;
                state.Count = 0;
                state.ItemIndex = itemIndex - 1;
            }

            // adjust the offset - we always set the state so it points to the next
            // item to be generated.
            int offset = position.Offset;
            if (offset == 0 && (!allowStartAtRealizedItem || state.Block == _itemMap))
            {
                offset = (direction == GeneratorDirection.Forward) ? 1 : -1;
            }

            // advance the state according to the offset
            if (offset > 0)
            {
                state.Block.MoveForward(ref state, true);
                -- offset;

                while (offset > 0)
                {
                    offset -= state.Block.MoveForward(ref state, allowStartAtRealizedItem, offset);
                }
            }
            else if (offset < 0)
            {
                if (state.Block == _itemMap)
                {
                    state.ItemIndex = state.Count = ItemsInternal.Count;
                }

                state.Block.MoveBackward(ref state, true);
                ++ offset;

                while (offset < 0)
                {
                    offset += state.Block.MoveBackward(ref state, allowStartAtRealizedItem, -offset);
                }
            }
        }

        // "Realize" the item in a block at the given offset, to be
        // the given item with corresponding container.  This means updating
        // the item map data structure so that the item belongs to a Realized block.
        // It also requires updating the state of every generator to track the
        // changes we make here.
        void Realize(UnrealizedItemBlock block, int offset, object item, DependencyObject container)
        {
            RealizedItemBlock prevR, nextR;

            RealizedItemBlock newBlock; // new location of the target item
            int newOffset;              // its offset within the new block
            int deltaCount;             // diff between cumulative item count of block and newBlock

            // if we're realizing the leftmost item and there's room in the
            // previous block, move it there
            if (offset == 0 &&
                (prevR = block.Prev as RealizedItemBlock) != null &&
                prevR.ItemCount < ItemBlock.BlockSize)
            {
                newBlock = prevR;
                newOffset = prevR.ItemCount;
                MoveItems(block, offset, 1, newBlock, newOffset, -prevR.ItemCount);
                MoveItems(block, 1, block.ItemCount, block, 0, +1);
            }

            // if we're realizing the rightmost item and there's room in the
            // next block, move it there
            else if (offset == block.ItemCount - 1 &&
                (nextR = block.Next as RealizedItemBlock) != null &&
                nextR.ItemCount < ItemBlock.BlockSize)
            {
                newBlock = nextR;
                newOffset = 0;
                MoveItems(newBlock, 0, newBlock.ItemCount, newBlock, 1, -1);
                MoveItems(block, offset, 1, newBlock, newOffset, offset);
            }

            // otherwise we need a new block for the target item
            else
            {
                newBlock = new RealizedItemBlock();
                newOffset = 0;
                deltaCount = offset;

                // if target is leftmost item, insert it before remaining items
                if (offset == 0)
                {
                    newBlock.InsertBefore(block);
                    MoveItems(block, offset, 1, newBlock, newOffset, 0);
                    MoveItems(block, 1, block.ItemCount, block, 0, +1);
                }

                // if target is rightmost item, insert it after remaining items
                else if (offset == block.ItemCount - 1)
                {
                    newBlock.InsertAfter(block);
                    MoveItems(block, offset, 1, newBlock, newOffset, offset);
                }

                // otherwise split the block into two, with the target in the middle
                else
                {
                    UnrealizedItemBlock newUBlock = new UnrealizedItemBlock();
                    newUBlock.InsertAfter(block);
                    newBlock.InsertAfter(block);
                    MoveItems(block, offset+1, block.ItemCount-offset-1, newUBlock, 0, offset+1);
                    MoveItems(block, offset, 1, newBlock, 0, offset);
                }
            }

            RemoveAndCoalesceBlocksIfNeeded(block);

            // add the new target to the map
            newBlock.RealizeItem(newOffset, item, container);
        }

        void RemoveAndCoalesceBlocksIfNeeded(ItemBlock block)
        {
            if (block != null && block != _itemMap && block.ItemCount == 0)
            {
                block.Remove();

                // coalesce adjacent unrealized blocks
                if (block.Prev is UnrealizedItemBlock && block.Next is UnrealizedItemBlock)
                {
                    MoveItems(block.Next, 0, block.Next.ItemCount, block.Prev, block.Prev.ItemCount, -block.Prev.ItemCount-1);
                    block.Next.Remove();
                }
            }
        }

        // Move 'count' items starting at position 'offset' in block 'block'
        // to position 'newOffset' in block 'newBlock'.  The difference between
        // the cumulative item counts of newBlock and block is given by 'deltaCount'.
        void MoveItems(ItemBlock block, int offset, int count,
                        ItemBlock newBlock, int newOffset, int deltaCount)
        {
            RealizedItemBlock ribSrc = block as RealizedItemBlock;
            RealizedItemBlock ribDst = newBlock as RealizedItemBlock;

            // when both blocks are Realized, entries must be physically copied
            if (ribSrc != null && ribDst != null)
            {
                ribDst.CopyEntries(ribSrc, offset, count, newOffset);
            }
            // when the source block is Realized, clear the vacated entries -
            // to avoid leaks.  (No need if it's now empty - the block will get GC'd).
            else if (ribSrc != null && ribSrc.ItemCount > count)
            {
                ribSrc.ClearEntries(offset, count);
            }

            // update block information
            block.ItemCount -= count;
            newBlock.ItemCount += count;

            // tell generators what happened
            if (MapChanged != null)
                MapChanged(block, offset, count, newBlock, newOffset, deltaCount);
        }

        // Set the AlternationIndex on a newly-realized container.  Also, reset
        // the AlternationIndex on other containers to maintain the adjacency
        // criterion.
        void SetAlternationIndex(ItemBlock block, int offset, GeneratorDirection direction)
        {
            // If user doesn't request alternation, don't do anything
            if (_alternationCount <= 0)
                return;

            int index;
            RealizedItemBlock rib;

            // Proceed in the direction of generation.  This tends to reach the
            // end sooner (often in one step).
            if (direction != GeneratorDirection.Backward)
            {
                // Forward.  Back up one container to determine the starting index
                -- offset;
                while (offset < 0 || block is UnrealizedItemBlock)
                {
                    block = block.Prev;
                    offset = block.ContainerCount - 1;
                }

                rib = block as RealizedItemBlock;
                index = (block == _itemMap) ? -1 : ItemsControl.GetAlternationIndex(rib.ContainerAt(offset));

                // loop through the remaining containers, resetting each AlternationIndex
                for (;;)
                {
                    // advance to next realized container
                    ++offset;
                    while (offset == block.ContainerCount)
                    {
                        block = block.Next;
                        offset = 0;
                    }

                    // exit if we've reached the end
                    if (block == _itemMap)
                        break;

                    // advance the AlternationIndex
                    index = (index + 1) % _alternationCount;

                    // assign it to the container
                    rib = block as RealizedItemBlock;
                    ItemsControl.SetAlternationIndex(rib.ContainerAt(offset), index);
                }
            }
            else
            {
                // Backward.  Advance one container to determine the starting index
                ++ offset;
                while (offset >= block.ContainerCount || block is UnrealizedItemBlock)
                {
                    block = block.Next;
                    offset = 0;
                }

                rib = block as RealizedItemBlock;

                // Get the alternation index for the advanced container. Use value 1 if no container
                // is found, so that 0 gets used for actual container in question.
                index = (block == _itemMap) ? 1 : ItemsControl.GetAlternationIndex(rib.ContainerAt(offset));

                // loop through the remaining containers, resetting each AlternationIndex
                for (;;)
                {
                    // retreat to next realized container
                    --offset;
                    while (offset < 0)
                    {
                        block = block.Prev;
                        offset = block.ContainerCount - 1;
                    }

                    // exit if we've reached the end
                    if (block == _itemMap)
                        break;

                    // retreat the AlternationIndex
                    index = (_alternationCount + index - 1) % _alternationCount;

                    // assign it to the container
                    rib = block as RealizedItemBlock;
                    ItemsControl.SetAlternationIndex(rib.ContainerAt(offset), index);
                }
            }
        }

        // create a group item for the given group
        DependencyObject ContainerForGroup(CollectionViewGroup group)
        {
            _generatesGroupItems = true;
            if (!ShouldHide(group))
            {
                // normal group - link a new GroupItem
                GroupItem groupItem = new GroupItem();

                LinkContainerToItem(groupItem, group);

                // create the generator
                groupItem.Generator = new ItemContainerGenerator(this, groupItem);

                return groupItem;
            }
            else
            {
                // hidden empty group - link a new EmptyGroupItem
                AddEmptyGroupItem(group);

                // but don't return it to layout
                return null;
            }
        }

        // prepare the grouping information.  Called from RemoveAll.
        void PrepareGrouping()
        {
            GroupStyle groupStyle;
            IList items;

            if (Level == 0)
            {
                groupStyle = Host.GetGroupStyle(null, 0);

                if (groupStyle == null)
                {
                    items = Host.View;
                }
                else
                {
                    CollectionView cv = Host.View.CollectionView;
                    items = (cv == null) ? null : cv.Groups;
                    if (items == null)
                    {
                        items = Host.View;

                        // When there are no groups, we should ignore GroupStyle
                        // and use the host's ItemsPanel .
                        // But this breaks Nero because
                        // their ItemsPanel can only be used at the leaf level of
                        // a real grouping scenario.  It null-refs if used with
                        // an empty collection, which happens during the first layout.
                        // So for compat we let the bogus GroupStyle.Panel leak through
                        // when the Items collection is empty.
                        if (items.Count > 0)
                        {
                            groupStyle = null;
                        }
                    }
                }
            }
            else
            {
                GroupItem groupItem = (GroupItem)Peer;
                CollectionViewGroup group = groupItem.ReadLocalValue(ItemForItemContainerProperty) as CollectionViewGroup;

                if (group != null)
                {
                    if (group.IsBottomLevel)
                    {
                        groupStyle = null;
                    }
                    else
                    {
                        groupStyle = Host.GetGroupStyle(group, Level);
                    }

                    items = group.Items;
                }
                else
                {
                    // GroupItem has been recycled.
                    groupStyle = null;
                    items = Host.View;
                }
            }

            GroupStyle = groupStyle;
            ItemsInternal = items;

            if ((Level == 0) && (Host != null))
            {
                // Notify the host of a change in IsGrouping
                Host.SetIsGrouping(IsGrouping);
            }
        }

        void SetAlternationCount()
        {
            int alternationCount;

            if (IsGrouping && GroupStyle != null)
            {
                if (GroupStyle.IsAlternationCountSet)
                {
                    alternationCount = GroupStyle.AlternationCount;
                }
                else if (_parent != null)
                {
                    alternationCount = _parent._alternationCount;
                }
                else
                {
                    alternationCount = Host.AlternationCount;
                }
            }
            else
            {
                alternationCount = Host.AlternationCount;
            }

            ChangeAlternationCount(alternationCount);
        }

        // should the given group be hidden?
        bool ShouldHide(CollectionViewGroup group)
        {
            return GroupStyle.HidesIfEmpty &&      // user asked to hide
                    group.ItemCount == 0;           // group is empty
        }

        // create an empty-group placeholder item
        void AddEmptyGroupItem(CollectionViewGroup group)
        {
            EmptyGroupItem emptyGroupItem = new EmptyGroupItem();

            LinkContainerToItem(emptyGroupItem, group);

            emptyGroupItem.SetGenerator(new ItemContainerGenerator(this, emptyGroupItem));

            // add it to the list of placeholder items (this keeps it from being GC'd)
            if (_emptyGroupItems == null)
                _emptyGroupItems = new ArrayList();
            _emptyGroupItems.Add(emptyGroupItem);
        }

        // notification that a subgroup has become non-empty
        void OnSubgroupBecameNonEmpty(EmptyGroupItem groupItem, CollectionViewGroup group)
        {
            // Discard placeholder container.
            UnlinkContainerFromItem(groupItem, group);
            if (_emptyGroupItems != null)
                _emptyGroupItems.Remove(groupItem);

            // inform layout as if the group just got added
            if (ItemsChanged != null)
            {
                GeneratorPosition position = PositionFromIndex(ItemsInternal.IndexOf(group));
                ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Add, position, 1, 0));
            }
        }

        // notification that a subgroup has become empty
        void OnSubgroupBecameEmpty(CollectionViewGroup group)
        {
            if (ShouldHide(group))
            {
                GeneratorPosition position = PositionFromIndex(ItemsInternal.IndexOf(group));

                // if the group is realized, un-realize it and notify layout
                if (position.Offset == 0 && position.Index >= 0)
                {
                    // un-realize
                    ((IItemContainerGenerator)this).Remove(position, 1);

                    // inform layout as if the group just got removed
                    if (ItemsChanged != null)
                    {
                        ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Remove, position, 1, 1));
                    }

                    // create the placeholder
                    AddEmptyGroupItem(group);
                }
            }
        }

        // convert an index (into Items) into a GeneratorPosition
        GeneratorPosition PositionFromIndex(int itemIndex)
        {
            GeneratorPosition position;
            ItemBlock itemBlock;
            int offsetFromBlockStart;

            GetBlockAndPosition(itemIndex, out position, out itemBlock, out offsetFromBlockStart);

            return position;
        }


        void GetBlockAndPosition(object item, int itemIndex, bool deletedFromItems, out GeneratorPosition position, out ItemBlock block, out int offsetFromBlockStart, out int correctIndex)
        {
            if (itemIndex >= 0)
            {
                GetBlockAndPosition(itemIndex, out position, out block, out offsetFromBlockStart);
                correctIndex = itemIndex;
            }
            else
            {
                GetBlockAndPosition(item, deletedFromItems, out position, out block, out offsetFromBlockStart, out correctIndex);
            }
        }


        void GetBlockAndPosition(int itemIndex, out GeneratorPosition position, out ItemBlock block, out int offsetFromBlockStart)
        {
            position = new GeneratorPosition(-1, 0);
            block = null;
            offsetFromBlockStart = itemIndex;

            if (_itemMap == null || itemIndex < 0)
                return;

            int containerIndex = 0;

            for (block = _itemMap.Next;  block != _itemMap;  block = block.Next)
            {
                if (offsetFromBlockStart >= block.ItemCount)
                {
                    // item belongs to a later block, increment the containerIndex
                    containerIndex += block.ContainerCount;
                    offsetFromBlockStart -= block.ItemCount;
                }
                else
                {
                    // item belongs to this block.  Determine the container index and offset
                    if (block.ContainerCount > 0)
                    {
                        // block has realized items
                        position = new GeneratorPosition(containerIndex + offsetFromBlockStart, 0);
                    }
                    else
                    {
                        // block has unrealized items
                        position = new GeneratorPosition(containerIndex-1, offsetFromBlockStart+1);
                    }

                    break;
                }
            }
        }

        void GetBlockAndPosition(object item, bool deletedFromItems, out GeneratorPosition position, out ItemBlock block, out int offsetFromBlockStart, out int correctIndex)
        {
            correctIndex = 0;
            int containerIndex = 0;
            offsetFromBlockStart = 0;
            int deletionOffset = deletedFromItems ? 1 : 0;
            position = new GeneratorPosition(-1, 0);

            if (_itemMap == null)
            {
                // handle reentrant call
                block = null;
                return;
            }

            for (block = _itemMap.Next;  block != _itemMap;  block = block.Next)
            {
                UnrealizedItemBlock uib;
                RealizedItemBlock rib = block as RealizedItemBlock;

                if (rib != null)
                {
                    // compare realized items with item for which we are searching
                    offsetFromBlockStart = rib.OffsetOfItem(item);
                    if (offsetFromBlockStart >= 0)
                    {
                        position = new GeneratorPosition(containerIndex + offsetFromBlockStart, 0);
                        correctIndex += offsetFromBlockStart;
                        break;
                    }
                }
                else if ((uib = block as UnrealizedItemBlock) != null)
                {
                    // if the item isn't realized, we can't find it
                    // directly.  Instead, look for indirect evidence that it
                    // belongs to this block by checking the indices of
                    // nearby realized items.

#if DEBUG
                    // Sanity check - make sure data structure is OK so far.
                    rib = block.Prev as RealizedItemBlock;
                    if (rib != null && rib.ContainerCount > 0)
                    {
                        Debug.Assert(ItemsControl.EqualsEx(rib.ItemAt(rib.ContainerCount - 1),
                                                    ItemsInternal[correctIndex - 1]),
                                    "Generator data structure is corrupt");
                    }
#endif

                    bool itemIsInCurrentBlock = false;
                    rib = block.Next as RealizedItemBlock;
                    if (rib != null && rib.ContainerCount > 0)
                    {
                        // if the index of the next realized item is off by one,
                        // the deleted item likely comes from the current
                        // unrealized block.
                        itemIsInCurrentBlock =
                                ItemsControl.EqualsEx(rib.ItemAt(0),
                                    ItemsInternal[correctIndex + block.ItemCount - deletionOffset]);
                    }
                    else if (block.Next == _itemMap)
                    {
                        // similarly if we're at the end of the list and the
                        // overall count is off by one, or if the current block
                        // is the only block, the deleted item likely
                        // comes from the current (last) unrealized block
                        itemIsInCurrentBlock = block.Prev == _itemMap ||
                            (ItemsInternal.Count == correctIndex + block.ItemCount - deletionOffset);
                    }

                    if (itemIsInCurrentBlock)
                    {
                        // we don't know where it is in this block, so assume
                        // it's the very first item.
                        offsetFromBlockStart = 0;
                        position = new GeneratorPosition(containerIndex-1, 1);
                        break;
                    }
                }

                correctIndex += block.ItemCount;
                containerIndex += block.ContainerCount;
            }

            if (block == _itemMap)
            {
                // There's no way of knowing which unrealized block it belonged to, so
                // the data structure can't be updated correctly.  Sound the alarm.
                throw new InvalidOperationException(SR.Get(SRID.CannotFindRemovedItem));
            }
        }


        // establish the link from the container to the corresponding item
        internal static void LinkContainerToItem(DependencyObject container, object item)
        {
            // always set the ItemForItemContainer property
            container.ClearValue(ItemForItemContainerProperty);
            container.SetValue(ItemForItemContainerProperty, item);

            // for non-direct items, set the DataContext property
            if (container != item)
            {
                #if DEBUG
                // Some ancient code at this point handled the case when DataContext
                // was set via an Expression (presumably a binding).  I don't think
                // this actually happens any more.  Just in case...
                DependencyProperty dp = FrameworkElement.DataContextProperty;
                EntryIndex entryIndex = container.LookupEntry(dp.GlobalIndex);
                Debug.Assert(!container.HasExpression(entryIndex, dp), "DataContext set by expression (unexpectedly)");
                #endif

                container.SetValue(FrameworkElement.DataContextProperty, item);
            }
        }

        private void UnlinkContainerFromItem(DependencyObject container, object item)
        {
            UnlinkContainerFromItem(container, item, _host);
        }

        internal static void UnlinkContainerFromItem(DependencyObject container, object item, IGeneratorHost host)
        {
            // When a container is removed from the tree, its future takes one of
            // two forms:
            //      a) [normal mode] the container becomes eligible for GC
            //      b) [recycling mode] the container joins the recycled list, and
            //          possibly re-enters the tree at some point, usually with a
            //          different item.
            //
            // As some "subtle issues" that arose in the
            // container recycling work illustrate, it's important that the container
            // and its subtree sever their connection to the data item.  Otherwise
            // you can get aliasing - a dead container reacting to the same item as a live
            // container.  Even without aliasing, it's a perf waste for a dead container
            // to continue reacting to its former data item.
            //
            // On the other hand, it's a perf waste to spend too much effort cleaning
            // up the container and its subtree, since they will often just get GC'd
            // in the near future.
            //
            // WPF initially did a full cleanup of the container, removing all properties
            // that were set in PrepareContainerForItem.  This avoided aliasing, but
            // was deemed too expensive, especially for scrolling.  For Windows OS Bug
            // 1445288, all this cleanup work was removed.  This sped up scrolling, but
            // introduced the recycling "subtle
            // issues".  A compromise is needed.
            //
            // The compromise is tell the container to attach to a sentinel item
            // BindingExpressionBase.DisconnectedItem.  We allow this to propagate into the
            // conainer's subtree through properties like DataContext and
            // ContentControl.Content that are normally set by PrepareItemForContainer.
            // A Binding that sees the sentinel as the data item will disconnect its
            // event listeners from the former data item, but will not change its
            // own value or invalidate its target property.  This avoids the cost
            // of re-measuring most of the subtree.

            container.ClearValue(ItemForItemContainerProperty);

            // TreeView virtualization requires that we call ClearContainer before setting
            // the DataContext to "Disconnected".  This gives the TreeViewItems a chance
            // to save "Item values" in the look-aside table, before that table is
            // discarded.  
            host.ClearContainerForItem(container, item);

            if (container != item)
            {
                DependencyProperty dp = FrameworkElement.DataContextProperty;

                #if DEBUG
                // Some ancient code at this point handled the case when DataContext
                // was set via an Expression (presumably a binding).  I don't think
                // this actually happens any more.  Just in case...
                EntryIndex entryIndex = container.LookupEntry(dp.GlobalIndex);
                Debug.Assert(!container.HasExpression(entryIndex, dp), "DataContext set by expression (unexpectedly)");
                #endif

                container.SetValue(dp, BindingExpressionBase.DisconnectedItem);
            }
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        void OnGroupStylePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Panel")
            {
                OnPanelChanged();
            }
            else
            {
                OnRefresh();
            }
        }

        void ValidateAndCorrectIndex(object item, ref int index)
        {
            if (index >= 0)
            {
                // this check is expensive - Items[index] potentially iterates through
                // the collection.  So trust the sender to tell us the truth in retail bits.
                Debug.Assert(ItemsControl.EqualsEx(item, ItemsInternal[index]), "Event contains the wrong index");
            }
            else
            {
                index = ItemsInternal.IndexOf(item);
                if (index < 0)
                    throw new InvalidOperationException(SR.Get(SRID.CollectionAddEventMissingItem, item));
            }
        }

        /// <summary>
        /// Forward a CollectionChanged event
        /// </summary>
        // Called  when items collection changes.
        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender != ItemsInternal && args.Action != NotifyCollectionChangedAction.Reset)
                return;     // ignore events (except Reset) from ItemsCollection when we're listening to group's items.

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (args.NewItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    OnItemAdded(args.NewItems[0], args.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (args.OldItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    OnItemRemoved(args.OldItems[0], args.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // Don't check arguments if app targets 4.0, for compat ( 726682)
                    if (!FrameworkCompatibilityPreferences.TargetsDesktop_V4_0)
                    {
                        if (args.OldItems.Count != 1)
                            throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    }
                    OnItemReplaced(args.OldItems[0], args.NewItems[0], args.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                    // Don't check arguments if app targets 4.0, for compat ( 726682)
                    if (!FrameworkCompatibilityPreferences.TargetsDesktop_V4_0)
                    {
                        if (args.OldItems.Count != 1)
                            throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    }
                    OnItemMoved(args.OldItems[0], args.OldStartingIndex, args.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    OnRefresh();
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }

            PresentationTraceLevel traceLevel = PresentationTraceSources.GetTraceLevel(this);
            if (traceLevel >= PresentationTraceLevel.High)
            {
                Verify();
            }
        }

        // Called when an item is added to the items collection
        void OnItemAdded(object item, int index)
        {
            if (_itemMap == null)
            {
                // reentrant call (from RemoveAllInternal) shouldn't happen,
                // but if it does, don't crash
                Debug.Assert(false, "unexpected reentrant call to OnItemAdded");
                return;
            }

            ValidateAndCorrectIndex(item, ref index);

            GeneratorPosition position = new GeneratorPosition(-1,0);

            // find the block containing the new item
            ItemBlock block = _itemMap.Next;
            int offsetFromBlockStart = index;
            int unrealizedItemsSkipped = 0;     // distance since last realized item
            while (block != _itemMap && offsetFromBlockStart >= block.ItemCount)
            {
                offsetFromBlockStart -= block.ItemCount;
                position.Index += block.ContainerCount;
                unrealizedItemsSkipped = (block.ContainerCount > 0) ? 0 : unrealizedItemsSkipped + block.ItemCount;
                block = block.Next;
            }

            position.Offset = unrealizedItemsSkipped + offsetFromBlockStart + 1;
            // the position is now correct, except when pointing into a realized block;
            // that case is fixed below

            // if it's an unrealized block, add the item by bumping the count
            UnrealizedItemBlock uib = block as UnrealizedItemBlock;
            if (uib != null)
            {
                MoveItems(uib, offsetFromBlockStart, 1, uib, offsetFromBlockStart+1, 0);
                ++ uib.ItemCount;
            }

            // if the item can be added to a previous unrealized block, do so
            else if ((offsetFromBlockStart== 0 || block == _itemMap) &&
                    ((uib = block.Prev as UnrealizedItemBlock) != null))
            {
                ++ uib.ItemCount;
            }

            // otherwise, create a new unrealized block
            else
            {
                uib = new UnrealizedItemBlock();
                uib.ItemCount = 1;

                // split the current realized block, if necessary
                RealizedItemBlock rib;
                if (offsetFromBlockStart > 0 && (rib = block as RealizedItemBlock) != null)
                {
                    RealizedItemBlock newBlock = new RealizedItemBlock();
                    MoveItems(rib, offsetFromBlockStart, rib.ItemCount - offsetFromBlockStart, newBlock, 0, offsetFromBlockStart);
                    newBlock.InsertAfter(rib);
                    position.Index += block.ContainerCount;
                    position.Offset = 1;
                    block = newBlock;
                }

                uib.InsertBefore(block);
            }

            // tell generators what happened
            if (MapChanged != null)
            {
                MapChanged(null, index, +1, uib, 0, 0);
            }

            // tell layout what happened
            if (ItemsChanged != null)
            {
                ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Add, position, 1, 0));
            }
        }


        // Called when an item is removed from the items collection
        void OnItemRemoved(object item, int itemIndex)
        {
            DependencyObject container = null;    // the corresponding container
            int containerCount = 0;

            // search for the deleted item
            GeneratorPosition position;
            ItemBlock block;
            int offsetFromBlockStart;
            int correctIndex;
            GetBlockAndPosition(item, itemIndex, true, out position, out block, out offsetFromBlockStart, out correctIndex);

            RealizedItemBlock rib = block as RealizedItemBlock;
            if (rib != null)
            {
                containerCount = 1;
                container = rib.ContainerAt(offsetFromBlockStart);
            }

            // remove the item, and remove the block if it's now empty
            MoveItems(block, offsetFromBlockStart + 1, block.ItemCount - offsetFromBlockStart - 1, block, offsetFromBlockStart, 0);
            --block.ItemCount;
            if (rib != null)
            {
                // fix up the alternation index before removing an empty block, while
                // we still have a valid block and offset
                SetAlternationIndex(block, offsetFromBlockStart, GeneratorDirection.Forward);
            }
            RemoveAndCoalesceBlocksIfNeeded(block);

            // tell generators what happened
            if (MapChanged != null)
            {
                MapChanged(null, itemIndex, -1, null, 0, 0);
            }

            // tell layout what happened
            if (ItemsChanged != null)
            {
                ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Remove, position, 1, containerCount));
            }

            // unhook the container.  Do this after layout has (presumably) removed it from
            // the UI, so that it doesn't inherit DataContext falsely.
            if (container != null)
            {
                UnlinkContainerFromItem(container, item);
            }

            // detect empty groups, so they can be hidden if necessary
            if (Level > 0 && ItemsInternal.Count == 0)
            {
                GroupItem groupItem = (GroupItem)Peer;
                CollectionViewGroup group = groupItem.ReadLocalValue(ItemForItemContainerProperty) as CollectionViewGroup;

                // the group could be null if the parent generator has already
                // unhooked its container
                if (group != null)
                {
                    Parent.OnSubgroupBecameEmpty(group);
                }
            }
        }

        void OnItemReplaced(object oldItem, object newItem, int index)
        {
            // search for the replaced item
            GeneratorPosition position;
            ItemBlock block;
            int offsetFromBlockStart;
            int correctIndex;
            GetBlockAndPosition(oldItem, index, false, out position, out block, out offsetFromBlockStart, out correctIndex);

            // If the item is in an UnrealizedItemBlock, then this change need not
            // be made to the _itemsMap as we are replacing an unrealized item with another unrealized
            // item in the same place.
            RealizedItemBlock rib = block as RealizedItemBlock;
            if (rib != null)
            {
                DependencyObject container = rib.ContainerAt(offsetFromBlockStart);

                if (oldItem != container && !_host.IsItemItsOwnContainer(newItem))
                {
                    // if we can re-use the old container, just relink it to the
                    // new item
                    rib.RealizeItem(offsetFromBlockStart, newItem, container);
                    LinkContainerToItem(container, newItem);
                    _host.PrepareItemContainer(container, newItem);
                }
                else
                {
                    // otherwise, we need a new container
                    DependencyObject newContainer = _host.GetContainerForItem(newItem);
                    rib.RealizeItem(offsetFromBlockStart, newItem, newContainer);
                    LinkContainerToItem(newContainer, newItem);

                    if (ItemsChanged != null)
                    {
                        ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Replace, position, 1, 1));
                    }

                    // after layout has removed the old container, unlink it
                    UnlinkContainerFromItem(container, oldItem);
                }
            }
        }

        void OnItemMoved(object item, int oldIndex, int newIndex)
        {
            if (_itemMap == null)
            {
                // reentrant call (from RemoveAllInternal) shouldn't happen,
                // but if it does, don't crash
                Debug.Assert(false, "unexpected reentrant call to OnItemMoved");
                return;
            }

            DependencyObject container = null;    // the corresponding container
            int containerCount = 0;
            UnrealizedItemBlock uib;

            // search for the moved item
            GeneratorPosition position;
            ItemBlock block;
            int offsetFromBlockStart;
            int correctIndex;
            GetBlockAndPosition(item, oldIndex, true, out position, out block, out offsetFromBlockStart, out correctIndex);

            GeneratorPosition oldPosition = position;

            RealizedItemBlock rib = block as RealizedItemBlock;
            if (rib != null)
            {
                containerCount = 1;
                container = rib.ContainerAt(offsetFromBlockStart);
            }

            // remove the item, and remove the block if it's now empty
            MoveItems(block, offsetFromBlockStart + 1, block.ItemCount - offsetFromBlockStart - 1, block, offsetFromBlockStart, 0);
            --block.ItemCount;
            RemoveAndCoalesceBlocksIfNeeded(block);

            //
            // now insert into the new spot.
            //

            position = new GeneratorPosition(-1,0);
            block = _itemMap.Next;
            offsetFromBlockStart = newIndex;
            while (block != _itemMap && offsetFromBlockStart >= block.ItemCount)
            {
                offsetFromBlockStart -= block.ItemCount;
                if (block.ContainerCount > 0)
                {
                    position.Index += block.ContainerCount;
                    position.Offset = 0;
                }
                else
                {
                    position.Offset += block.ItemCount;
                }
                block = block.Next;
            }

            position.Offset += offsetFromBlockStart + 1;

            // if it's an unrealized block, add the item by bumping the count
            uib = block as UnrealizedItemBlock;
            if (uib != null)
            {
                MoveItems(uib, offsetFromBlockStart, 1, uib, offsetFromBlockStart+1, 0);
                ++ uib.ItemCount;
            }

            // if the item can be added to a previous unrealized block, do so
            else if ((offsetFromBlockStart == 0 || block == _itemMap) &&
                    ((uib = block.Prev as UnrealizedItemBlock) != null))
            {
                ++ uib.ItemCount;
            }

            // otherwise, create a new unrealized block
            else
            {
                uib = new UnrealizedItemBlock();
                uib.ItemCount = 1;

                // split the current realized block, if necessary
                if (offsetFromBlockStart > 0 && (rib = block as RealizedItemBlock) != null)
                {
                    RealizedItemBlock newBlock = new RealizedItemBlock();
                    MoveItems(rib, offsetFromBlockStart, rib.ItemCount - offsetFromBlockStart, newBlock, 0, offsetFromBlockStart);
                    newBlock.InsertAfter(rib);
                    position.Index += block.ContainerCount;
                    position.Offset = 1;
                    offsetFromBlockStart = 0;
                    block = newBlock;
                }

                uib.InsertBefore(block);
            }

            DependencyObject parent = VisualTreeHelper.GetParentInternal(container);

            // tell layout what happened
            if (ItemsChanged != null)
            {
                ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Move, position, oldPosition, 1, containerCount));
            }

            // unhook the container.  Do this after layout has (presumably) removed it from
            // the UI, so that it doesn't inherit DataContext falsely.
            if (container != null)
            {
                if (parent == null || VisualTreeHelper.GetParentInternal(container) != parent)
                {
                    UnlinkContainerFromItem(container, item);
                }
                else
                {
                    // If the container has the same visual parent as before then that means that
                    // the container was just repositioned within the parent's VisualCollection.
                    // we don't need to unlink the container, but we do need to re-realize the block.
                    Realize(uib, offsetFromBlockStart, item, container);
                }
            }

            // fix up the AlternationIndex on containers affected by the move
            if (_alternationCount > 0)
            {
                // start with the smaller of the two positions, and proceed forward.
                // This tends to preserve the AlternatonIndex on containers at the
                // front of the list, as users expect
                int index = Math.Min(oldIndex, newIndex);
                GetBlockAndPosition(index, out position, out block, out offsetFromBlockStart);
                SetAlternationIndex(block, offsetFromBlockStart, GeneratorDirection.Forward);
            }
        }

        // Called when the items collection is refreshed
        void OnRefresh()
        {
            ((IItemContainerGenerator)this).RemoveAll();

            // tell layout what happened
            if (ItemsChanged != null)
            {
                GeneratorPosition position = new GeneratorPosition(0, 0);
                ItemsChanged(this, new ItemsChangedEventArgs(NotifyCollectionChangedAction.Reset, position, 0, 0));
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Generator       _generator;
        private IGeneratorHost  _host;
        private ItemBlock       _itemMap;
        private GeneratorStatus _status;
        private int             _itemsGenerated;
        private int             _startIndexForUIFromItem;
        private DependencyObject _peer;
        private int             _level;
        private IList           _items;
        private ReadOnlyCollection<object> _itemsReadOnly;
        private GroupStyle      _groupStyle;
        private ItemContainerGenerator _parent;
        private ArrayList       _emptyGroupItems;
        private int             _alternationCount;

        private Type            _containerType;     // type of containers on the recycle queue
        private Queue<DependencyObject> _recyclableContainers = new Queue<DependencyObject>();

        private bool            _generatesGroupItems; // Flag to indicate that this generates GroupItems
        private bool            _isGeneratingBatches;

        event MapChangedHandler MapChanged;

        delegate void MapChangedHandler(ItemBlock block, int offset, int count,
                    ItemBlock newBlock, int newOffset, int deltaCount);

#if GENERATOR_TRACE
        MS.Internal.Utility.HFTimer _timer = new MS.Internal.Utility.HFTimer();
        MS.Internal.Utility.HFTimer _creationTimer = new MS.Internal.Utility.HFTimer();
#endif

        //------------------------------------------------------
        //
        //  Private Nested Classes
        //
        //------------------------------------------------------

        // The ItemContainerGenerator uses the following data structure to maintain
        // the correspondence between items and their containers.  It's a doubly-linked
        // list of ItemBlocks, with a sentinel node serving as the header.
        // Each node maintains two counts:  the number of items it holds, and
        // the number of containers.
        //
        // There are two kinds of blocks - one holding only "realized" items (i.e.
        // items that have been generated into containers) and one holding only
        // unrealized items.  The container count of a realized block is the same
        // as its item count (one container per item);  the container count of an
        // unrealized block is zero.
        //
        // Unrealized blocks can hold any number of items.  We only need to know
        // the count.  Realized blocks have a fixed-sized array (BlockSize) so
        // they hold up to that many items and their corresponding containers.  When
        // a realized block fills up, it inserts a new (empty) realized block into
        // the list and carries on.
        //
        // This data structure was chosen with virtualization in mind.  The typical
        // state is a long block of unrealized items (the ones that have scrolled
        // off the top), followed by a moderate number (<50?) of realized items
        // (the ones in view), followed by another long block of unrealized items
        // (the ones that have not yet scrolled into view).  So the list will contain
        // an unrealized block, followed by 3 or 4 realized blocks, followed by
        // another unrealized block.  Fewer than 10 blocks altogether, so linear
        // searching won't cost that much.  Thus we don't need a more sophisticated
        // data structure.  (If profiling reveals that we do, we can always replace
        // this one.  It's totally private to the ItemContainerGenerator and its
        // Generators.)

        // represents a block of items
        private class ItemBlock
        {
            public const int BlockSize = 16;

            public int ItemCount { get { return _count; } set { _count = value; } }
            public ItemBlock Prev { get { return _prev; } set { _prev = value; } }
            public ItemBlock Next { get { return _next; } set { _next = value; } }

            public virtual int ContainerCount { get { return Int32.MaxValue; } }
            public virtual DependencyObject ContainerAt(int index) { return null; }
            public virtual object ItemAt(int index) { return null; }

            public void InsertAfter(ItemBlock prev)
            {
                Next = prev.Next;
                Prev = prev;

                Prev.Next = this;
                Next.Prev = this;
            }

            public void InsertBefore(ItemBlock next)
            {
                InsertAfter(next.Prev);
            }

            public void Remove()
            {
                Prev.Next = Next;
                Next.Prev = Prev;
            }

            public void MoveForward(ref GeneratorState state, bool allowMovePastRealizedItem)
            {
                if (IsMoveAllowed(allowMovePastRealizedItem))
                {
                    state.ItemIndex += 1;
                    if (++state.Offset >= ItemCount)
                    {
                        state.Block = Next;
                        state.Offset = 0;
                        state.Count += ItemCount;
                    }
                }
            }

            public void MoveBackward(ref GeneratorState state, bool allowMovePastRealizedItem)
            {
                if (IsMoveAllowed(allowMovePastRealizedItem))
                {
                    if (--state.Offset < 0)
                    {
                        state.Block = Prev;
                        state.Offset = state.Block.ItemCount - 1;
                        state.Count -= state.Block.ItemCount;
                    }
                    state.ItemIndex -= 1;
                }
            }

            public int MoveForward(ref GeneratorState state, bool allowMovePastRealizedItem, int count)
            {
                if (IsMoveAllowed(allowMovePastRealizedItem))
                {
                    if (count < ItemCount - state.Offset)
                    {
                        state.Offset += count;
                    }
                    else
                    {
                        count = ItemCount - state.Offset;
                        state.Block = Next;
                        state.Offset = 0;
                        state.Count += ItemCount;
                    }

                    state.ItemIndex += count;
                }

                return count;
            }

            public int MoveBackward(ref GeneratorState state, bool allowMovePastRealizedItem, int count)
            {
                if (IsMoveAllowed(allowMovePastRealizedItem))
                {
                    if (count <= state.Offset)
                    {
                        state.Offset -= count;
                    }
                    else
                    {
                        count = state.Offset + 1;
                        state.Block = Prev;
                        state.Offset = state.Block.ItemCount - 1;
                        state.Count -= state.Block.ItemCount;
                    }

                    state.ItemIndex -= count;
                }

                return count;
            }

            protected virtual bool IsMoveAllowed(bool allowMovePastRealizedItem)
            {
                return allowMovePastRealizedItem;
            }

            int _count;
            ItemBlock _prev, _next;
        }

        // represents a block of unrealized (ungenerated) items
        private class UnrealizedItemBlock : ItemBlock
        {
            public override int ContainerCount { get { return 0; } }

            protected override bool IsMoveAllowed(bool allowMovePastRealizedItem)
            {
                return true;
            }
        }

        // represents a block of realized (generated) items
        private class RealizedItemBlock : ItemBlock
        {
            public override int ContainerCount { get { return ItemCount; } }

            public override DependencyObject ContainerAt(int index)
            {
                return _entry[index].Container;
            }

            public override object ItemAt(int index)
            {
                return _entry[index].Item;
            }

            public void CopyEntries(RealizedItemBlock src, int offset, int count, int newOffset)
            {
                int k;
                // choose which direction to copy so as not to clobber existing
                // entries (in case the source and destination blocks are the same)
                if (offset < newOffset)
                {
                    // copy right-to-left
                    for (k = count - 1;  k >= 0;  --k)
                    {
                        _entry[newOffset + k] = src._entry[offset + k];
                    }

                    // clear vacated entries, to avoid leak
                    if (src != this)
                    {
                        src.ClearEntries(offset, count);
                    }
                    else
                    {
                        src.ClearEntries(offset, newOffset - offset);
                    }
                }
                else
                {
                    // copy left-to-right
                    for (k = 0;  k < count;  ++k)
                    {
                        _entry[newOffset + k] = src._entry[offset + k];
                    }

                    // clear vacated entries, to avoid leak
                    if (src != this)
                    {
                        src.ClearEntries(offset, count);
                    }
                    else
                    {
                        src.ClearEntries(newOffset + count, offset - newOffset);
                    }
                }
            }

            public void ClearEntries(int offset, int count)
            {
                for (int i=0; i<count; ++i)
                {
                    _entry[offset + i].Item = null;
                    _entry[offset + i].Container = null;
                }
            }

            public void RealizeItem(int index, object item, DependencyObject container)
            {
                _entry[index].Item = item;
                _entry[index].Container = container;
            }

            public int OffsetOfItem(object item)
            {
                for (int k=0; k < ItemCount; ++k)
                {
                    if (ItemsControl.EqualsEx(_entry[k].Item, item))
                        return k;
                }

                return -1;
            }

            BlockEntry[] _entry = new BlockEntry[BlockSize];
        }

        // an entry in the table maintained by RealizedItemBlock
        private struct BlockEntry
        {
            public object Item          { get { return _item; }     set { _item = value; } }
            public DependencyObject Container    { get { return _container; }  set { _container = value; } }

            private object      _item;
            private DependencyObject   _container;
        }

        // cached state of the factory's item map (updated by factory)
        // used to speed up calls to Generate
        private struct GeneratorState
        {
            public ItemBlock Block  { get { return _block; }   set { _block = value; } }
            public int Offset       { get { return _offset; }  set { _offset = value; } }
            public int Count        { get { return _count; }   set { _count = value; } }
            public int ItemIndex    { get { return _itemIndex; }   set { _itemIndex = value; } }

            private ItemBlock   _block;     // some block in the map (most recently used)
            private int         _offset;    // offset with the block
            private int         _count;     // cumulative item count of blocks before the cached one
            private int         _itemIndex; // index of current item
        }


        // The EmptyGroupItem class is used for the HidesIfEmpty grouping feature.
        // It takes the place of a regular GroupItem for an empty group, but is never
        // returned to layout/panel as a real container.
        private class EmptyGroupItem : GroupItem
        {
            public void SetGenerator(ItemContainerGenerator generator)
            {
                this.Generator = generator;
                generator.ItemsChanged += new ItemsChangedEventHandler(OnItemsChanged);
            }

            private void OnItemsChanged(object sender, ItemsChangedEventArgs e)
            {
                CollectionViewGroup group = (CollectionViewGroup)GetValue(ItemContainerGenerator.ItemForItemContainerProperty);

                // if the group becomes non-empty, un-hide the UI
                if (group.ItemCount > 0)
                {
                    ItemContainerGenerator generator = Generator;
                    generator.ItemsChanged -= new ItemsChangedEventHandler(OnItemsChanged);
                    generator.Parent.OnSubgroupBecameNonEmpty(this, group);
                }
            }
        }
    }
}
