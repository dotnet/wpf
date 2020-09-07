// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.Diagnostics;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// The Clock instance type that will be created based on TimelineGroup
    /// objects.
    /// </summary>
    public class ClockGroup : Clock
    {
        /// <summary>
        /// Creates a new empty ClockGroup to be used in a Clock tree.
        /// </summary>
        /// <param name="timelineGroup">The TimelineGroup used to define the new
        /// ClockGroup.</param>
        protected internal ClockGroup(TimelineGroup timelineGroup)
            : base(timelineGroup)
        {
        }

        /// <summary>
        /// Gets the TimelineGroup object that holds the description controlling the
        /// behavior of this clock.
        /// </summary>
        /// <value>
        /// The TimelineGroup object that holds the description controlling the
        /// behavior of this clock.
        /// </value>
        public new TimelineGroup Timeline
        {
            get
            {
                return (TimelineGroup)base.Timeline;
            }
        }


        /// <summary>
        /// Gets a collection containing the children of this clock.
        /// </summary>
        /// <value>
        /// A collection containing the children of this clock.
        /// </value>
        public ClockCollection Children
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return new ClockCollection(this);
            }
        }

        /// <summary>
        /// Unchecked internal access to the child collection of this Clock.
        /// </summary>
        internal List<Clock> InternalChildren
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                return _children;
            }
        }

        /// <summary>
        /// Uncheck internal access to the root children
        /// </summary>
        internal List<WeakReference> InternalRootChildren
        {
            get
            {
                Debug.Assert(IsTimeManager);

                return _rootChildren;
            }
        }


        internal override void BuildClockSubTreeFromTimeline(
            Timeline timeline,
            bool hasControllableRoot)
        {
            // This is not currently necessary
            //base.BuildClockSubTreeFromTimeline(timeline);

            // Only TimelineGroup has children
            TimelineGroup timelineGroup = timeline as TimelineGroup;

            // Only a TimelineGroup should have allocated a ClockGroup.
            Debug.Assert(timelineGroup != null);

            // Create a clock for each of the children of the timeline
            TimelineCollection timelineChildren = timelineGroup.Children;

            if (timelineChildren != null && timelineChildren.Count > 0)
            {
                Clock childClock;

                // Create a collection for the children of the clock
                _children = new List<Clock>();

                // Create clocks for the children
                for (int index = 0; index < timelineChildren.Count; index++)
                {
                    childClock = AllocateClock(timelineChildren[index], hasControllableRoot);
                    childClock._parent = this;  // We connect the child to the subtree before calling BuildClockSubtreeFromTimeline
                    childClock.BuildClockSubTreeFromTimeline(timelineChildren[index], hasControllableRoot);
                    _children.Add(childClock);
                    childClock._childIndex = index;
                }

                // If we have SlipBehavior, check if we have any childen with which to slip.
                if (_timeline is ParallelTimeline &&
                    ((ParallelTimeline)_timeline).SlipBehavior == SlipBehavior.Slip)
                {
                    // Verify that we only use SlipBehavior in supported scenarios
                    if (!IsRoot ||
                       (_timeline.RepeatBehavior.HasDuration) ||
                       (_timeline.AutoReverse == true) ||
                       (_timeline.AccelerationRatio > 0) ||
                       (_timeline.DecelerationRatio > 0))
                    {
                        throw new NotSupportedException(SR.Get(SRID.Timing_SlipBehavior_SlipOnlyOnSimpleTimelines));
                    }

                    for (int index = 0; index < _children.Count; index++)
                    {
                        Clock child = _children[index];
                        if (child.CanSlip)
                        {
                            Duration duration = child.ResolvedDuration;

                            // A sync clock with duration of zero or no begin time has no effect, so do skip it
                            if ((!duration.HasTimeSpan || duration.TimeSpan > TimeSpan.Zero)
                                && child._timeline.BeginTime.HasValue)
                            {
                                _syncData = new SyncData(child);
                                child._syncData = null;  // The child will no longer self-sync
                            }

                            break;  // We only want the first child with CanSlip
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the first child of this timeline.
        /// </summary>
        /// <value>
        /// The first child of this timeline if the collection is not empty;
        /// otherwise, null.
        /// </value>
        internal override Clock FirstChild
        {
            get
            {
                // ROOT Debug.Assert(!IsTimeManager);

                Clock firstChild = null;

                List<Clock> children = _children;

                if (children != null)
                {
                    firstChild = children[0];
                }

                return firstChild;
            }
        }

        // This is only to be called on the TimeManager clock. Go through our
        // top level clocks and find the clock with the highest desired framerate.
        // DFR has to be > 0, so starting the accumulator at 0 is fine.
        internal int GetMaxDesiredFrameRate()
        {
            Debug.Assert(IsTimeManager);

            int desiredFrameRate = 0;

            // Ask all top-level clock their desired framerate
            WeakRefEnumerator<Clock> enumerator = new WeakRefEnumerator<Clock>(_rootChildren);

            while (enumerator.MoveNext())
            {
                Clock currentClock = enumerator.Current;

                if (currentClock != null && currentClock.CurrentState == ClockState.Active)
                {
                    int? currentDesiredFrameRate = currentClock.DesiredFrameRate;
                    if (currentDesiredFrameRate.HasValue)
                    {
                        desiredFrameRate = Math.Max(desiredFrameRate, currentDesiredFrameRate.Value);
                    }
                }
            }

            return desiredFrameRate;
        }


        // Called on the root
        internal void ComputeTreeState()
        {
            Debug.Assert(IsTimeManager);
            
            // Revive all children
            WeakRefEnumerator<Clock> enumerator = new WeakRefEnumerator<Clock>(_rootChildren);

            while (enumerator.MoveNext())
            {
                PrefixSubtreeEnumerator prefixEnumerator = new PrefixSubtreeEnumerator(enumerator.Current, true);
                while (prefixEnumerator.MoveNext())
                {
                    Clock current = prefixEnumerator.Current;

                    // Only traverse the "ripe" subset of the Timing tree
                    if (CurrentGlobalTime >= current.InternalNextTickNeededTime)
                    {
                        current.ApplyDesiredFrameRateToGlobalTime();
                        current.ComputeLocalState();       // Compute the state of the node
                        current.ClipNextTickByParent();    // Perform NextTick clipping, stage 1

                        // Make a note to visit for stage 2, only for ClockGroups and Roots
                        current.NeedsPostfixTraversal = (current is ClockGroup) || (current.IsRoot);
                    }
                    else
                    {
                        prefixEnumerator.SkipSubtree();
                    }
                }
            }

            // To perform a postfix walk culled by NeedsPostfixTraversal flag, we use a local recursive method
            // Note that since we called for this operation, it is probably already needed by the root clock
            ComputeTreeStateRoot();
        }


        internal void ComputeTreeStateRoot()
        {
            Debug.Assert(IsTimeManager);
            TimeSpan? previousTickNeededTime = InternalNextTickNeededTime;
            InternalNextTickNeededTime = null;  // Reset the root's next tick needed time

            WeakRefEnumerator<Clock> enumerator = new WeakRefEnumerator<Clock>(_rootChildren);

            while (enumerator.MoveNext())
            {
                Clock current = enumerator.Current;

                if (current.NeedsPostfixTraversal)
                {
                    if (current is ClockGroup)
                    {
                        ((ClockGroup)current).ComputeTreeStatePostfix();
                    }
                    current.ApplyDesiredFrameRateToNextTick();  // Apply the effects of DFR on each root as needed
                    current.NeedsPostfixTraversal = false;  // Reset the flag
                }

                if(!InternalNextTickNeededTime.HasValue ||
                    (enumerator.Current.InternalNextTickNeededTime.HasValue &&
                     enumerator.Current.InternalNextTickNeededTime < InternalNextTickNeededTime))
                {
                    InternalNextTickNeededTime = enumerator.Current.InternalNextTickNeededTime;
                }
            }

            if (InternalNextTickNeededTime.HasValue &&
                (!previousTickNeededTime.HasValue || previousTickNeededTime > InternalNextTickNeededTime))
            {
                _timeManager.NotifyNewEarliestFutureActivity();
            }
        }


        // Recursive postfix walk, culled by NeedsPostfixTraversal flags (hence cannot use PostfixSubtreeEnumerator)
        private void ComputeTreeStatePostfix()
        {
            if (_children != null)
            {
                for (int c = 0; c < _children.Count; c++)
                {
                    if (_children[c].NeedsPostfixTraversal)  // Traverse deeper if this is part of the visited tree subset
                    {
                        ClockGroup group = _children[c] as ClockGroup;
                        Debug.Assert(group != null);  // We should only have this flag set for ClockGroups

                        group.ComputeTreeStatePostfix();
                    }
                }

                ClipNextTickByChildren();
            }
        }

        // Perform Stage 2 of clipping next tick time: clip by children
        // Derived class does the actual clipping
        private void ClipNextTickByChildren()
        {
            Debug.Assert(_children != null);

            for (int c = 0; c < _children.Count; c++)
            {
                // Clip by child's NTNT if needed
                if (!InternalNextTickNeededTime.HasValue ||
                    (_children[c].InternalNextTickNeededTime.HasValue && _children[c].InternalNextTickNeededTime < InternalNextTickNeededTime))
                {
                    InternalNextTickNeededTime = _children[c].InternalNextTickNeededTime;
                }
            }
        }


        /// <summary>
        /// Return the current duration from a specific clock
        /// </summary>
        /// <returns>
        /// A Duration quantity representing the current iteration's estimated duration.
        /// </returns>
        internal override Duration CurrentDuration
        {
            get
            {
                Duration manualDuration = _timeline.Duration;  // Check if a duration is specified by the user
                if (manualDuration != Duration.Automatic)
                {
                    return manualDuration;
                }

                Duration currentDuration = TimeSpan.Zero;

                // The container ends when all of its children have ended at least
                // one of their active periods.
                if (_children != null)
                {
                    bool hasChildWithUnresolvedDuration = false;
                    bool bufferingSlipNode = (_syncData != null    // This variable makes sure that our slip node completes as needed
                                             && _syncData.IsInSyncPeriod
                                             && !_syncData.SyncClockHasReachedEffectiveDuration);

                    for (int childIndex = 0; childIndex < _children.Count; childIndex++)
                    {
                        Clock current = _children[childIndex];
                        Duration childEndOfActivePeriod = current.EndOfActivePeriod;

                        if (childEndOfActivePeriod == Duration.Forever)
                        {
                            // If we have even one child with a duration of forever
                            // our resolved duration will also be forever. It doesn't
                            // matter if other _children have unresolved durations.
                            return Duration.Forever;
                        }
                        else if (childEndOfActivePeriod == Duration.Automatic)
                        {
                            hasChildWithUnresolvedDuration = true;
                        }
                        else
                        {
                            // Make sure that until Media completes, it is not treated as expired
                            if (bufferingSlipNode && _syncData.SyncClock == this)
                            {
                                childEndOfActivePeriod += TimeSpan.FromMilliseconds(50);  // This compensation is roughly one frame of video
                                bufferingSlipNode = false;
                            }

                            if (childEndOfActivePeriod > currentDuration)
                            {
                                currentDuration = childEndOfActivePeriod;
                            }
                        }
                    }

                    // We've iterated through all our _children. We know that at this
                    // point none of them have a duration of Forever or we would have
                    // returned already. If any of them still have unresolved 
                    // durations then our duration is also still unresolved and we
                    // will return automatic. Otherwise, we'll fall out of the 'if'
                    // block and return the currentDuration as our final resolved 
                    // duration.
                    if (hasChildWithUnresolvedDuration)
                    {
                        return Duration.Automatic;
                    }
                }

                return currentDuration;
            }
        }


        /// <summary>
        /// Marks this Clock as the root of a timing tree.
        /// </summary>
        /// <param name="timeManager">
        /// The TimeManager that controls this timing tree.
        /// </param>
        internal void MakeRoot(TimeManager timeManager)
        {
            Debug.Assert(!IsTimeManager, "Cannot associate a root with multiple timing trees");
            Debug.Assert(this._timeManager == null, "Cannot use a timeline already in the timing tree as a root");
            Debug.Assert(timeManager.TimeManagerClock == this, "Cannot associate more than one root per timing tree");
            Debug.Assert(this._parent == null && _children == null, "Cannot use a timeline connected to other timelines as a root");

            IsTimeManager = true;
            _rootChildren = new List<WeakReference>();
            _timeManager = timeManager;
            _depth = 0;

            // currently no one queries the root clock's properties.  Consider removing this code.

            InternalCurrentIteration = 1;
            InternalCurrentProgress = 0;
            InternalCurrentGlobalSpeed = 1;
            InternalCurrentClockState = ClockState.Active;
        }


        // Upon a discontinuous interactive operation (begin/seek/stop), this resets children with SyncData
        // (e.g. media) to track this change, specifically:
        //  1) Realign their begin times evenly with the parent, discounting past slippage that may have occured
        //  2) Reset their state, in case they leave their "sync" period.
        internal override void ResetNodesWithSlip()
        {
            Debug.Assert(IsRoot);
            if (_children != null)
            {
                for (int c = 0; c < _children.Count; c++)
                {
                    Clock child = _children[c];

                    if (child._syncData != null)
                    {
                        child._beginTime = child._timeline.BeginTime;  // Realign the clock
                        child._syncData.IsInSyncPeriod = false;
                        child._syncData.UpdateClockBeginTime();  // Apply effects of realigning
                    }
                }
            }

            base.ResetNodesWithSlip();
        }


        /// <summary>
        /// Activates this root clock.
        /// </summary>
        internal void RootActivate()
        {
            Debug.Assert(IsTimeManager, "Invalid call to RootActivate for a non-root Clock");
            Debug.Assert(_timeManager != null);  // RootActivate should be called by our own TimeManager

            // Reset the state of the timing tree
            TimeIntervalCollection currentIntervals = TimeIntervalCollection.CreatePoint(_timeManager.InternalCurrentGlobalTime);
            currentIntervals.AddNullPoint();
            _timeManager.InternalCurrentIntervals = currentIntervals;

            ComputeTreeState();
        }

        /// <summary>
        /// Removes dead weak references from the child list of the root clock.
        /// </summary>
        internal void RootCleanChildren()
        {
            Debug.Assert(IsTimeManager, "Invalid call to RootCleanChildren for a non-root Clock");

            WeakRefEnumerator<Clock> enumerator = new WeakRefEnumerator<Clock>(_rootChildren);

            // Simply enumerating the children with the weak reference
            // enumerator is sufficient to clean up the list. Therefore, the
            // body of the loop can remain empty
            while (enumerator.MoveNext())
            {
            }

            // When the loop terminates naturally the enumerator will clean
            // up the list of any dead references. Therefore, by the time we
            // get here we are done.
        }

        /// <returns>
        /// Returns true if any children are left, false if none are left
        /// </returns>
        internal bool RootHasChildren
        {
            get
            {
                Debug.Assert(IsTimeManager, "Invalid call to RootHasChildren for a non-root Clock");

                return (_rootChildren.Count > 0);
            }
        }

        /// <summary>
        /// Deactivates and disables this root clock.
        /// </summary>
        internal void RootDisable()
        {
            Debug.Assert(IsTimeManager, "Invalid call to RootDeactivate for a non-root Clock");

            // Reset the state of the timing tree
            WeakRefEnumerator<Clock> enumerator = new WeakRefEnumerator<Clock>(_rootChildren);

            while (enumerator.MoveNext())
            {
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(enumerator.Current, true);

                while (subtree.MoveNext())
                {
                    if (subtree.Current.InternalCurrentClockState != ClockState.Stopped)
                    {
                        subtree.Current.ResetCachedStateToStopped();

                        subtree.Current.RaiseCurrentStateInvalidated();
                        subtree.Current.RaiseCurrentTimeInvalidated();
                        subtree.Current.RaiseCurrentGlobalSpeedInvalidated();
                    }
                    else
                    {
                        subtree.SkipSubtree();
                    }
                }
            }
        }

        /// <summary>
        /// Check if our descendants have resolved their duration, and resets the HasDescendantsWithUnresolvedDuration
        /// flag from true to false once that happens.
        /// </summary>
        /// <returns>Returns true when this node or one of its descendants have unresolved duration.</returns>
        internal override void UpdateDescendantsWithUnresolvedDuration()
        {
            // If the flag was already unset, or our own node doesn't know its duration yet, the flag will not change now.
            if (!HasDescendantsWithUnresolvedDuration ||  !HasResolvedDuration)
            {
                return;
            }

            if (_children != null)
            {
                for (int childIndex = 0; childIndex < _children.Count; childIndex++)
                {
                    _children[childIndex].UpdateDescendantsWithUnresolvedDuration();
                    if (_children[childIndex].HasDescendantsWithUnresolvedDuration)
                    {
                        return;  // If any child has unresolved descendants, we cannot unset our flag yet
                    }
                }
            }

            // If we finished the loop, then every child subtree has fully resolved its duration during this method call.
            HasDescendantsWithUnresolvedDuration = false;
            return;
        }


        internal override void ClearCurrentIntervalsToNull()
        {
            _currentIntervals.Clear();
            _currentIntervals.AddNullPoint();
        }

        internal override void AddNullPointToCurrentIntervals()
        {
            _currentIntervals.AddNullPoint();
        }
        
        internal override void ComputeCurrentIntervals(TimeIntervalCollection parentIntervalCollection,
                                                       TimeSpan beginTime, TimeSpan? endTime,
                                                       Duration fillDuration, Duration period,
                                                       double appliedSpeedRatio, double accelRatio, double decelRatio,
                                                       bool isAutoReversed)
        {
            _currentIntervals.Clear();
            parentIntervalCollection.ProjectOntoPeriodicFunction(ref _currentIntervals,
                                                                 beginTime, endTime,
                                                                 fillDuration, period, appliedSpeedRatio,
                                                                 accelRatio, decelRatio, isAutoReversed);
        }


        internal override void ComputeCurrentFillInterval(TimeIntervalCollection parentIntervalCollection,
                                                          TimeSpan beginTime, TimeSpan endTime, Duration period,
                                                          double appliedSpeedRatio, double accelRatio, double decelRatio,
                                                          bool isAutoReversed)
        {
            _currentIntervals.Clear();
            parentIntervalCollection.ProjectPostFillZone(ref _currentIntervals,
                                                         beginTime, endTime,
                                                         period, appliedSpeedRatio,
                                                         accelRatio, decelRatio, isAutoReversed);
        }

        internal TimeIntervalCollection CurrentIntervals
        {
            get
            {
                return _currentIntervals;
            }
        }
        
        private List<Clock> _children;
        private List<WeakReference> _rootChildren;
        private TimeIntervalCollection _currentIntervals;
    }
}
