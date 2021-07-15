// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using MS.Internal.PresentationCore;

namespace System.Windows.Media.Animation
{
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    [System.Flags]
    internal enum SubtreeFlag
    {
        Reset                       = 1,
        ProcessRoot                 = 2,
        SkipSubtree                 = 4,
    }

    /// <summary>
    /// An object that enumerates the clocks of a subtree of Clock
    /// objects.
    /// </summary>
    internal struct PrefixSubtreeEnumerator
    {
        #region Constructor

        /// <summary>
        /// Creates an enumerator that iterates over a subtree of clocks
        /// in prefix order.
        /// </summary>
        /// <param name="root">
        /// The clock that is the root of the subtree to enumerate.
        /// </param>
        /// <param name="processRoot">
        /// True to include the root in the enumeration, false otherwise.
        /// </param>
        internal PrefixSubtreeEnumerator(Clock root, bool processRoot)
        {
            _rootClock = root;
            _currentClock = null;
            _flags = processRoot ? (SubtreeFlag.Reset | SubtreeFlag.ProcessRoot) : SubtreeFlag.Reset;
        }

        #endregion // Constructor

        #region Methods

        /// <summary>
        /// Causes the enumerator to not enumerate the clocks in the subtree rooted
        /// at the current clock.
        /// </summary>
        internal void SkipSubtree()
        {
            _flags |= SubtreeFlag.SkipSubtree;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element,
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            // Get the iteration started in the right place, if we are just starting
            if ((_flags & SubtreeFlag.Reset) != 0)
            {
                // We are just getting started. The first clock is either the root
                // or its first child
                if ((_flags & SubtreeFlag.ProcessRoot) != 0)
                {
                    // Start with the root
                    _currentClock = _rootClock;
                }
                else
                {
                    // Start with the root's first child
                    if (_rootClock != null)
                    {
                        ClockGroup rootClockGroup = _rootClock as ClockGroup;
                        if (rootClockGroup != null)
                        {
                            _currentClock = rootClockGroup.FirstChild;
                        }
                        else
                        {
                            _currentClock = null;
                        }
                    }
                }

                // Next time we won't be getting started anymore
                _flags &= ~SubtreeFlag.Reset;
            }
            else if (_currentClock != null)
            {
                // The next clock is possibly the first child of the current clock
                ClockGroup currentClockGroup = _currentClock as ClockGroup;

                Clock nextClock = (currentClockGroup == null) ? null : currentClockGroup.FirstChild;

                // Skip the children if explicitly asked to do so, or if there aren't any
                if (((_flags & SubtreeFlag.SkipSubtree) != 0) || (nextClock == null))
                {
                    // Pop back to the first ancestor that has siblings (current clock included). Don't
                    // go back further than the root of the subtree. At the end of this loop, nextClock
                    // will point to the proper next clock.
                    while ((_currentClock != _rootClock) && ((nextClock = _currentClock.NextSibling) == null))
                    {
                        _currentClock = _currentClock.InternalParent;
                    }

                    // Don't process siblings of the root
                    if (_currentClock == _rootClock)
                    {
                        nextClock = null;
                    }

                    _flags &= ~SubtreeFlag.SkipSubtree;
                }

                _currentClock = nextClock;
            }

            return _currentClock != null;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the
        /// first element in the collection.
        /// </summary>
        public void Reset()
        {
            _currentClock = null;
            _flags = (_flags & SubtreeFlag.ProcessRoot) | SubtreeFlag.Reset;
        }

        #endregion // Methods

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        internal Clock Current
        {
            get
            {
                return _currentClock;
            }
        }

        #endregion // Properties

        #region Data

        private Clock       _rootClock;
        private Clock       _currentClock;
        private SubtreeFlag         _flags;

        #endregion // Data
    }

    /// <summary>
    /// An object that enumerates the clocks of a subtree of Clock
    /// objects.
    /// </summary>
    internal struct PostfixSubtreeEnumerator
    {
        #region Constructor

        internal PostfixSubtreeEnumerator(Clock root, bool processRoot)
        {
            _rootClock = root;
            _currentClock = null;
            _flags = processRoot ? (SubtreeFlag.Reset | SubtreeFlag.ProcessRoot) : SubtreeFlag.Reset;
        }

        #endregion // Constructor

        #region Methods

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element,
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            // Get started in the right place
            if ((_flags & SubtreeFlag.Reset) != 0)
            {
                _currentClock = _rootClock;
                _flags &= ~SubtreeFlag.Reset;
            }
            else if (_currentClock == _rootClock)
            {
                // We last saw the root clock, so we are done
                _currentClock = null;
            }

            // Skip trying to iterate if we are already past the end
            if (_currentClock != null)
            {
                Clock nextClock = _currentClock;

                // The next clock is either our next sibling's first leaf
                // or, if we don't have a sibling, our parent. If the current
                // clock is null, however, we are just starting, so skip this
                // move and go straight to the descent part
                if ((_currentClock != _rootClock) && (nextClock = _currentClock.NextSibling) == null)
                {
                    // We have no siblings, so the next clock is our parent
                    _currentClock = _currentClock.InternalParent;
                }
                else
                {
                    // We have a next sibling or we are the root. In either case, find the
                    // first leaf clock under this subtree
                    ClockGroup currentClockGroup;
                    do
                    {
                        _currentClock = nextClock;
                        currentClockGroup = _currentClock as ClockGroup;
} 
                    while ((nextClock = _currentClock.FirstChild) != null);
                }

                // Don't process the root, unless specifically requested
                if ((_currentClock == _rootClock) && ((_flags & SubtreeFlag.ProcessRoot) == 0))
                {
                    _currentClock = null;
                }
            }

            return _currentClock != null;
        }

        #endregion // Methods

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        internal Clock Current
        {
            get
            {
                return _currentClock;
            }
        }

        #endregion // Properties

        #region Data

        private Clock       _rootClock;
        private Clock       _currentClock;
        private SubtreeFlag _flags;

        #endregion // Data
    }

    /// <summary>
    /// An object that enumerates the timelines of a tree of Timeline
    /// objects.
    /// </summary>
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal struct TimelineTreeEnumerator
    {
        #region Constructor
        /// <summary>
        /// Creates an enumerator that iterates over a subtree of timelines
        /// in prefix order.
        /// </summary>
        /// <param name="root">
        /// The timeline that is the root of the subtree to enumerate.
        /// </param>
        /// <param name="processRoot">
        /// True to include the root in the enumeration, false otherwise.
        /// </param>
        internal TimelineTreeEnumerator(Timeline root, bool processRoot)
        {
            _rootTimeline = root;
            _flags = processRoot ? (SubtreeFlag.Reset | SubtreeFlag.ProcessRoot) : SubtreeFlag.Reset;

            // Start with stacks of capacity 10. That's relatively small, yet
            // it covers very large trees without reallocation of stack data.
            // Note that given the way we use the stacks we need one less entry
            // for indices than for timelines, as we don't care what the index
            // of the root timeline is.
            _indexStack = new Stack(9);
            _timelineStack = new Stack<Timeline>(10);
        }
        #endregion // Constructor

        #region Methods

        /// <summary>
        /// Causes the enumerator to not enumerate the timelines in the subtree rooted
        /// at the current timeline.
        /// </summary>
        internal void SkipSubtree()
        {
            _flags |= SubtreeFlag.SkipSubtree;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element,
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            TimelineCollection  children;

            // Get the iteration started in the right place, if we are just starting
            if ((_flags & SubtreeFlag.Reset) != 0)
            {
                // The reset flag takes effect only once
                _flags &= ~SubtreeFlag.Reset;

                // We are just getting started. The first timeline is the root
                _timelineStack.Push(_rootTimeline);

                // If we are not supposed to return the root, simply skip it
                if ((_flags & SubtreeFlag.ProcessRoot) == 0)
                {
                    MoveNext();
                }
            }
            else if (_timelineStack.Count > 0)
            {
                // Only TimelineGroup can have children
                TimelineGroup timelineGroup = _timelineStack.Peek() as TimelineGroup;

                // The next timeline is possibly the first child of the current timeline
                // If we have children move to the first one, unless we were
                // asked to skip the subtree
                if (  ((_flags & SubtreeFlag.SkipSubtree) == 0) 
                    && timelineGroup != null 
                    && (children = timelineGroup.Children) != null
                    && children.Count > 0
                    )
                {
                    _timelineStack.Push(children[0]);
                    _indexStack.Push((int)0);
                }
                else
                {
                    // The skip subtree flag takes effect only once
                    _flags &= ~SubtreeFlag.SkipSubtree;

                    // Move to the first ancestor that has unvisited children,
                    // then move to the first unvisited child. If we get to
                    // the root it means we are done.
                    _timelineStack.Pop();
                    while (_timelineStack.Count > 0)
                    {
                        timelineGroup = _timelineStack.Peek() as TimelineGroup;

                        // This has to be non-null since we already went down the tree
                        children = timelineGroup.Children;

                        int index = (int)_indexStack.Pop() + 1;

                        if (index < children.Count)
                        {
                            // Move to the next child, and we are done
                            _timelineStack.Push(children[index]);
                            _indexStack.Push(index);
                            break;
                        }

                        _timelineStack.Pop();
                    }
                }
            }

            return _timelineStack.Count > 0;
        }

        #endregion // Methods

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        internal Timeline Current
        {
            get
            {
                return _timelineStack.Peek();
            }
        }

        #endregion // Properties

        #region Data

        private Timeline            _rootTimeline;
        private SubtreeFlag         _flags;
        private Stack               _indexStack;
        private Stack<Timeline>     _timelineStack;

        #endregion // Data
    }
}
