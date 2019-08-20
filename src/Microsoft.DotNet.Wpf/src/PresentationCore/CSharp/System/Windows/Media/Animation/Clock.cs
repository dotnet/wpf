// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

#if DEBUG
#define TRACE
#endif // DEBUG

using MS.Internal;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Windows.Threading;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Maintains run-time timing state for timed objects.
    /// </summary>
    /// <remarks>
    /// A Clock object maintains the run-time state for a timed object
    /// according to the description specified in a Timeline object. It also
    /// provides methods for timing control, such as event scheduling and
    /// VCR-like functionality. Clock objects are arranged in trees
    /// that match the structure of the Timeline objects they are created from.
    /// </remarks>
    public class Clock : DispatcherObject
    {
        //
        // Constructors
        // 

        #region Constructors

        /// <summary>
        /// Creates a Clock object.
        /// </summary>
        /// <param name="timeline">
        /// The Timeline to use as a template.
        /// </param>
        /// <remarks>
        /// The returned Clock doesn't have any children.
        /// </remarks>
        protected internal Clock(Timeline timeline)
        {
#if DEBUG
            lock (_debugLockObject)
            {
                _debugIdentity = ++_nextIdentity;
                WeakReference weakRef = new WeakReference(this);
                _objectTable[_debugIdentity] = weakRef;
            }
#endif // DEBUG

            Debug.Assert(timeline != null);

            // 
            // Store a frozen copy of the timeline
            //

            _timeline = (Timeline)timeline.GetCurrentValueAsFrozen();

            // GetCurrentValueAsFrozen will make a clone of the Timeline if it's 
            // not frozen and will return the Timeline if it is frozen.
            // The clone will never have event handlers, while the
            // frozen original may.  This means we need to copy
            // the event handlers from the original timeline onto the clock
            // to be consistent.

            //
            // Copy the event handlers from the original timeline into the clock
            //
            _eventHandlersStore = timeline.InternalEventHandlersStore;

            //                                                                      
            // FXCop fix. Do not call overridables in constructors
            // UpdateNeedsTicksWhenActive();
            // Set the NeedsTicksWhenActive only if we have someone listening
            // to an event.

            SetFlag(ClockFlags.NeedsTicksWhenActive, _eventHandlersStore != null);  
                                                                                                
            //
            // Cache values that won't change as the clock ticks
            //

            // Non-root clocks have an unchanging begin time specified by their timelines.
            // A root clock will update _beginTime as necessary.
            _beginTime = _timeline.BeginTime;

            // Cache duration, getting Timeline.Duration and recalculating duration 
            // each Tick was eating perf, resolve the duration if possible.
            _resolvedDuration = _timeline.Duration;

            if (_resolvedDuration == Duration.Automatic)
            {
                // Forever is the default for an automatic duration. We can't
                // try to resolve the duration yet because the tree
                // may not be fully built, in which case ClockGroups won't 
                // have their children yet.
                _resolvedDuration = Duration.Forever;
            }
            else
            {
                HasResolvedDuration = true;
            }

            _currentDuration = _resolvedDuration;

            // Cache speed ratio, for roots this value may be updated if the interactive
            // speed ratio changes, but for non-roots this value will remain constant
            // throughout the lifetime of the clock.
            _appliedSpeedRatio = _timeline.SpeedRatio;

            //
            // Initialize current state
            //
   
            _currentClockState = ClockState.Stopped;

            if (_beginTime.HasValue)
            {
                // We need a tick to bring our state up to date
                _nextTickNeededTime = TimeSpan.Zero;
            }

            // All other data members initialized to zero by default
        }

        #endregion // Constructors

        //
        // Public Properties
        //

        #region Public Properties

        internal bool CanGrow
        {
            get
            {
                return GetFlag(ClockFlags.CanGrow);
            }
        }

        internal bool CanSlip
        {
            get
            {
                return GetFlag(ClockFlags.CanSlip);
            }
        }


        /// <summary>
        /// Returns an ClockController which can be used to perform interactive
        /// operations on this Clock.  If interactive operations are not allowed,
        /// this property returns null; this is the case for Clocks that
        /// aren't children of the root Clock.
        /// </summary>
        public ClockController Controller
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                // Unless our parent is the root clock and we're controllable, 
                // return null
                if (IsRoot && HasControllableRoot)
                {
                    return new ClockController(this);
                }
                else
                {
                    return null;
                }
            }
        }


        /// <summary>
        /// The current repeat iteration. The first period has a value of one.
        /// </summary>
        /// <remarks>
        /// If the clock is not active, the value of this property is only valid if
        /// the fill attribute specifies that the timing attributes should be
        /// extended. Otherwise, the property returns -1.
        /// </remarks>
        public Int32? CurrentIteration
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return _currentIteration;
            }
        }


        /// <summary>
        /// Gets the current rate at which time is progressing in the clock,
        /// compared to the real-world wall clock.  If the clock is stopped,
        /// this method returns null.
        /// </summary>
        /// <value></value>
        public double? CurrentGlobalSpeed
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return _currentGlobalSpeed;
            }
        }


        /// <summary>
        /// The current progress of time for this clock.
        /// </summary>
        /// <remarks>
        /// If the clock is active, the progress is always a value between 0 and 1,
        /// inclusive. Otherwise, the progress depends on the value of the
        /// <see cref="System.Windows.Media.Animation.Timeline.FillBehavior"/> attribute.
        /// If the clock is inactive and the fill attribute is not in effect, this
        /// property returns null.
        /// </remarks>
        public double? CurrentProgress
        {
            get
            {
                //                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return _currentProgress;
            }
        }


        /// <summary>
        /// Gets a value indicating whether the Clock’s current time is inside the Active period
        /// (meaning properties may change frame to frame), inside the Fill period, or Stopped.
        /// </summary>
        /// <remarks>
        /// You can tell whether you’re in FillBegin or FillEnd by the value of CurrentProgress
        /// (0 for FillBegin, 1 for FillEnd).
        /// </remarks>
        public ClockState CurrentState
        {
            get
            {
                //                 VerifyAccess();

                return _currentClockState;
            }
        }


        /// <summary>
        /// The current position of the clock, relative to the starting time. Setting
        /// this property to a new value has the effect of seeking the clock to a
        /// new point in time. Both forward and backward seeks are allowed. Setting this
        /// property has no effect if the clock is not active. However, seeking while the
        /// clock is paused works as expected.
        /// </summary>
        public TimeSpan? CurrentTime
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return _currentTime;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasControllableRoot
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                return GetFlag(ClockFlags.HasControllableRoot);
            }
        }

        /// <summary>
        /// True if the timeline is currently paused, false otherwise.
        /// </summary>
        /// <remarks>
        /// This property returns true either if this timeline has been paused, or if an
        /// ancestor of this timeline has been paused.
        /// </remarks>
        public bool IsPaused
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return IsInteractivelyPaused;
            }
        }

        /// <summary>
        /// Returns the natural duration of this Clock, which is defined
        /// by the Timeline from which it is created.
        /// </summary>
        /// <returns></returns>
        public Duration NaturalDuration
        {
            get
            {
                return _timeline.GetNaturalDuration(this);
            }
        }

        /// <summary>
        /// The Clock that sets the parent time for this Clock.
        /// </summary>
        public Clock Parent
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                // If our parent is the root clock, force a return value of null
                if (IsRoot)
                {
                    return null;
                }
                else
                {
                    return _parent;
                }
            }
        }


        /// <summary>
        /// Gets the Timeline object that holds the description controlling the
        /// behavior of this clock.
        /// </summary>
        /// <value>
        /// The Timeline object that holds the description controlling the
        /// behavior of this clock.
        /// </value>
        public Timeline Timeline
        {
            get
            {
//                 VerifyAccess();
                Debug.Assert(!IsTimeManager);

                return _timeline;
            }
        }

        #endregion // Public Properties


        //
        // Public Events
        //

        #region Public Events

        /// <summary>
        /// Raised by the timeline when it has completed.
        /// </summary>
        public event EventHandler Completed
        {
            add
            {
                AddEventHandler(Timeline.CompletedKey, value);
            }
            remove
            {
                RemoveEventHandler(Timeline.CompletedKey, value);
            }
        }


        /// <summary>
        /// Raised by the Clock whenever its current speed changes.
        /// This event mirrors the CurrentGlobalSpeedInvalidated event on Timeline
        /// </summary>
        public event EventHandler CurrentGlobalSpeedInvalidated
        {
            add
            {
//                VerifyAccess();
                AddEventHandler(Timeline.CurrentGlobalSpeedInvalidatedKey, value);
            }
            remove
            {
//                VerifyAccess();
                RemoveEventHandler(Timeline.CurrentGlobalSpeedInvalidatedKey, value);
            }
        }


        /// <summary>
        /// Raised by the Clock whenever its current state changes.
        /// This event mirrors the CurrentStateInvalidated event on Timeline
        /// </summary>
        public event EventHandler CurrentStateInvalidated
        {
            add
            {
//                VerifyAccess();
                AddEventHandler(Timeline.CurrentStateInvalidatedKey, value);
            }
            remove
            {
//                VerifyAccess();
                RemoveEventHandler(Timeline.CurrentStateInvalidatedKey, value);
            }
        }

        /// <summary>
        /// Raised by the Clock whenever its current time changes.
        /// This event mirrors the CurrentTimeInvalidated event on Timeline
        /// </summary>
        public event EventHandler CurrentTimeInvalidated
        {
            add
            {
//                VerifyAccess();
                AddEventHandler(Timeline.CurrentTimeInvalidatedKey, value);
            }
            remove
            {
//                VerifyAccess();
                RemoveEventHandler(Timeline.CurrentTimeInvalidatedKey, value);
            }
        }

        /// <summary>
        /// Raised by the timeline when its removal has been requested by the user.
        /// </summary>
        public event EventHandler RemoveRequested
        {
            add
            {
                AddEventHandler(Timeline.RemoveRequestedKey, value);
            }
            remove
            {
                RemoveEventHandler(Timeline.RemoveRequestedKey, value);
            }
        }

        #endregion // Public Events


        //
        // Protected Methods
        //

        #region Protected Methods

        /// <summary>
        /// Notify a clock that we've moved in a discontinuous way
        /// </summary>
        protected virtual void DiscontinuousTimeMovement()
        {
            // Base class does nothing
        }

        /// <summary>
        /// Returns true if the Clock has its own external source for time, which may
        /// require synchronization with the timing system.  Media is one example of this.
        /// </summary>
        protected virtual bool GetCanSlip()
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual TimeSpan GetCurrentTimeCore()
        {
            Debug.Assert(!IsTimeManager);

            return _currentTime.HasValue ? _currentTime.Value : TimeSpan.Zero;
        }

        /// <summary>
        /// Notify a clock that we've changed the speed
        /// </summary>
        protected virtual void SpeedChanged()
        {
            // Base class does nothing
        }

        /// <summary>
        /// Notify a clock that we've stopped
        /// </summary>
        protected virtual void Stopped()
        {
            // Base class does nothing
        }


        #endregion // Protected Methods

        //
        // Protected Properties
        //

        #region Protected Properties

        /// <summary>
        /// The current global time in TimeSpan units, established by the time manager.
        /// </summary>
        protected TimeSpan CurrentGlobalTime
        {
            get
            {
                if (_timeManager == null)
                {
                    return TimeSpan.Zero;
                }
                else if (IsTimeManager)
                {
                    return _timeManager.InternalCurrentGlobalTime;
                }
                else
                {
                    Clock current = this;
                    while (!current.IsRoot)  // Traverse up the tree to the root node
                    {
                        current = current._parent;
                    }

                    if (current.HasDesiredFrameRate)
                    {
                        return current._rootData.CurrentAdjustedGlobalTime;
                    }
                    else
                    {
                        return _timeManager.InternalCurrentGlobalTime;
                    }
                }
            }
        }

        #endregion // Protected Properties


        //
        // Internal Methods
        //

        #region Internal Methods

        internal virtual void AddNullPointToCurrentIntervals()
        {
        }


        internal static Clock AllocateClock(
            Timeline timeline,
            bool hasControllableRoot)
        {
            Clock clock = timeline.AllocateClock();

            // Assert that we weren't given an existing clock
            Debug.Assert(!clock.IsTimeManager);

            ClockGroup clockGroup = clock as ClockGroup;

            if (   clock._parent != null
                || (   clockGroup != null
                    && clockGroup.InternalChildren != null ))
            {
                // The derived class is trying to fool us -- we require a new,
                // fresh, unassociated clock here
                throw new InvalidOperationException(
                    SR.Get(
                        SRID.Timing_CreateClockMustReturnNewClock,
                        timeline.GetType().Name));
            }

            clock.SetFlag(ClockFlags.HasControllableRoot, hasControllableRoot);

            return clock;
        }

        internal virtual void BuildClockSubTreeFromTimeline(
            Timeline timeline,
            bool hasControllableRoot)
        {
            SetFlag(ClockFlags.CanSlip, GetCanSlip());  // Set the CanSlip flag

            // Here we preview the clock's own slip-ability, hence ClockGroups should return false
            // at this stage, because their children are not yet added by the time of this call.
            if (CanSlip && (IsRoot || _timeline.BeginTime.HasValue))
            {
                ResolveDuration();

                // A sync clock with duration of zero or no begin time has no effect, so do skip it
                if (!_resolvedDuration.HasTimeSpan || _resolvedDuration.TimeSpan > TimeSpan.Zero)
                {
                    // Verify that we only use SlipBehavior in supported scenarios
                    if ((_timeline.AutoReverse == true) ||
                        (_timeline.AccelerationRatio > 0) ||
                        (_timeline.DecelerationRatio > 0))
                    {
                        throw new NotSupportedException(SR.Get(SRID.Timing_CanSlipOnlyOnSimpleTimelines));
                    }

                    _syncData = new SyncData(this);  // CanSlip clocks keep themselves synced
                    HasDescendantsWithUnresolvedDuration = !HasResolvedDuration;  // Keep track of when our duration is resolved

                    Clock current = _parent;  // Traverse up the parent chain and verify that no unsupported behavior is specified
                    while (current != null)
                    {
                        Debug.Assert(!current.IsTimeManager);  // We should not yet be connected to the TimeManager
                        if (current._timeline.AutoReverse || current._timeline.AccelerationRatio > 0
                                                          || current._timeline.DecelerationRatio > 0)
                        {
                            throw new System.InvalidOperationException(SR.Get(SRID.Timing_SlipBehavior_SyncOnlyWithSimpleParents));
                        }

                        current.SetFlag(ClockFlags.CanGrow, true);  // Propagate the slippage tracking up the tree
                        if (!HasResolvedDuration)  // Let the parents know that we have not yet unresolved duration
                        {
                            current.HasDescendantsWithUnresolvedDuration = true;
                        }
                        current._currentIterationBeginTime = current._beginTime;

                        current = current._parent;
                    }
                }
            }
        }

        internal static Clock BuildClockTreeFromTimeline(
            Timeline rootTimeline,
            bool hasControllableRoot)
        {
            Clock rootClock = AllocateClock(rootTimeline, hasControllableRoot);

            // Set this flag so that the subsequent method can rely on it.
            rootClock.IsRoot = true;
            rootClock._rootData = new RootData();  // Create a RootData to hold root specific information.

            // The root clock was given a reference to a frozen copy of the 
            // timing tree.  We pass this copy down BuildClockSubTreeFromTimeline
            // so that each child clock will use that tree rather
            // than create a new one.
            rootClock.BuildClockSubTreeFromTimeline(rootClock.Timeline, hasControllableRoot);
            
            rootClock.AddToTimeManager();

            return rootClock;
        }
      
        internal virtual void ClearCurrentIntervalsToNull()
        {
        }

        // Perform Stage 1 of clipping next tick time: clip by parent
        internal void ClipNextTickByParent()
        {
            // Clip by parent's NTNT if needed.  We don't want to clip
            // if the parent is the TimeManager's root clock. 
            if (!IsTimeManager && !_parent.IsTimeManager &&
                (!InternalNextTickNeededTime.HasValue ||
                (_parent.InternalNextTickNeededTime.HasValue && _parent.InternalNextTickNeededTime.Value < InternalNextTickNeededTime.Value)))
            {
                InternalNextTickNeededTime = _parent.InternalNextTickNeededTime;
            }
        }


        internal virtual void ComputeCurrentIntervals(TimeIntervalCollection parentIntervalCollection,
                                                      TimeSpan beginTime, TimeSpan? endTime,
                                                      Duration fillDuration, Duration period,
                                                      double appliedSpeedRatio, double accelRatio, double decelRatio,
                                                      bool isAutoReversed)
        {
        }


        internal virtual void ComputeCurrentFillInterval(TimeIntervalCollection parentIntervalCollection,
                                                         TimeSpan beginTime, TimeSpan endTime, Duration period,
                                                         double appliedSpeedRatio, double accelRatio, double decelRatio,
                                                         bool isAutoReversed)
        {
        }


        internal void ComputeLocalState()
        {
            Debug.Assert(!IsTimeManager);

            // Cache previous state values
            ClockState  lastClockState          = _currentClockState;
            TimeSpan?   lastCurrentTime         = _currentTime;
            double?     lastCurrentGlobalSpeed  = _currentGlobalSpeed;
            double?     lastCurrentProgress     = _currentProgress;
            Int32?      lastCurrentIteration    = _currentIteration;

            // Reset the PauseStateChangedDuringTick for this tick
            PauseStateChangedDuringTick = false;

            ComputeLocalStateHelper(true, false);  // Perform the local state calculations with early bail out

            if (lastClockState != _currentClockState)
            {
                // It can happen that we change state without detecting it when
                // a parent is auto-reversing and we are ticking exactly at the
                // reverse point, so raise the events.
                RaiseCurrentStateInvalidated();
                RaiseCurrentGlobalSpeedInvalidated();
                RaiseCurrentTimeInvalidated();
            }

            if (_currentGlobalSpeed != lastCurrentGlobalSpeed)
            {
                RaiseCurrentGlobalSpeedInvalidated();
            }

            if (HasDiscontinuousTimeMovementOccured)
            {
                DiscontinuousTimeMovement();
                HasDiscontinuousTimeMovementOccured = false;
            }
        }


        /// <summary>
        /// Return the current duration from a specific clock
        /// </summary>
        /// <returns>
        /// A Duration quantity representing the current iteration's estimated duration.
        /// </returns>
        internal virtual Duration CurrentDuration
        {
            get { return Duration.Automatic; }
        }


        /// <summary>
        /// Internal helper. Schedules an interactive begin at the next tick.
        /// An interactive begin is literally a seek to 0. It is completely distinct 
        /// from the BeginTime specified on a timeline, which is managed by 
        /// _pendingBeginOffset
        /// </summary>
        internal void InternalBegin()
        {
            InternalSeek(TimeSpan.Zero);      
        }


        /// <summary>
        /// Internal helper for moving to new API. Gets the speed multiplier for the Clock's speed.
        /// </summary>
        internal double InternalGetSpeedRatio()
        {
            return _rootData.InteractiveSpeedRatio;
        }

        /// <summary>
        /// Internal helper for moving to new API. Pauses the timeline for this timeline and its children.
        /// </summary>
        internal void InternalPause()
        {
//             VerifyAccess();
            Debug.Assert(!IsTimeManager);

            // INVARIANT: we enforce 4 possible valid states:
            //   1) !Paused (running)
            //   2) !Paused, Pause pending
            //   3) Paused
            //   4) Paused, Resume pending
            Debug.Assert(!(IsInteractivelyPaused && PendingInteractivePause));
            Debug.Assert(!(!IsInteractivelyPaused && PendingInteractiveResume));

            if (PendingInteractiveResume)  // Cancel existing resume request if made
            {
                PendingInteractiveResume = false;
            }
            else if (!IsInteractivelyPaused)
            // If we don't have a pending resume AND we aren't paused already, schedule a pause
            // This is an ELSE clause because if we had a Resume pending, we MUST already be paused
            {
                PendingInteractivePause = true;
            }

            NotifyNewEarliestFutureActivity();
        }


        /// <summary>
        /// Schedules a Remove operation to happen at the next tick.
        /// </summary>
        /// <remarks>
        /// This method schedules the Clock and its subtree to be stopped and the RemoveRequested
        /// event to be fired on the subtree at the next tick.
        /// </remarks>
        internal void InternalRemove()
        {
            PendingInteractiveRemove = true;
            InternalStop();
        }


        /// <summary>
        /// Internal helper for moving to new API. Allows a timeline's timeline to progress again after a call to Pause.
        /// </summary>
        internal void InternalResume()
        {
//             VerifyAccess();
            Debug.Assert(!IsTimeManager);

            // INVARIANT: we enforce 4 possible valid states:
            //   1) !Paused (running)
            //   2) !Paused, Pause pending
            //   3)  Paused
            //   4)  Paused, sd Resume pending
            Debug.Assert( !(IsInteractivelyPaused && PendingInteractivePause));
            Debug.Assert( !(!IsInteractivelyPaused && PendingInteractiveResume));

            if (PendingInteractivePause)  // Cancel existing pause request if made
            {
                PendingInteractivePause = false;
            }
            else if (IsInteractivelyPaused)
            // If we don't have a pending pause AND we are currently paused, schedule a resume
            // This is an ELSE clause because if we had a Pause pending, we MUST already be unpaused
            {
                PendingInteractiveResume = true;
            }

            NotifyNewEarliestFutureActivity();
        }


        /// <summary>
        /// Internal helper for moving to new API. Seeks a timeline's timeline to a new position.
        /// </summary>
        /// <param name="destination">
        /// The destination to seek to, relative to the clock's BeginTime. If this is past the
        /// active period execute the FillBehavior.
        /// </param>
        internal void InternalSeek(TimeSpan destination)
        {
//             VerifyAccess();
            Debug.Assert(IsRoot);

            IsInteractivelyStopped = false;
            PendingInteractiveStop = false;   // Cancel preceding stop;
            ResetNodesWithSlip();  // Reset sync tracking
            
            _rootData.PendingSeekDestination = destination;
            RootBeginPending = false; // cancel a previous begin call

            NotifyNewEarliestFutureActivity();
        }

        /// <summary>
        /// The only things that can change for this are the begin time of this
        /// timeline
        /// </summary>
        /// <param name="destination">
        /// The destination to seek to, relative to the clock's BeginTime. If this is past the
        /// active perioed execute the FillBehavior.
        /// </param>
        internal void InternalSeekAlignedToLastTick(TimeSpan destination)
        {
            Debug.Assert(IsRoot);

            // This is a no-op with a null TimeManager or when all durations have not yet been resolved
            if (_timeManager == null || HasDescendantsWithUnresolvedDuration)
            {
                return;
            }

            // Adjust _beginTime such that our current time equals the Seek position
            // that was requested
            _beginTime = CurrentGlobalTime - DivideTimeSpan(destination, _appliedSpeedRatio);
            if (CanGrow)
            {
                _currentIteration = null;  // This node is not visited by ResetSlipOnSubtree
                _currentIterationBeginTime = _beginTime;

                ResetSlipOnSubtree();
                UpdateSyncBeginTime();
            }

            IsInteractivelyStopped = false;  // We have unset disabled status
            PendingInteractiveStop = false;
            RootBeginPending = false;        // Cancel a pending begin
            ResetNodesWithSlip();            // Reset sync tracking

            _timeManager.InternalCurrentIntervals = TimeIntervalCollection.Empty;

            PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

            while (subtree.MoveNext())
            {
                // We are processing a Seek immediately. We don't need a TIC yet
                // since we are not computing events, and we don't want to
                // process pending stuff either.
                subtree.Current.ComputeLocalStateHelper(false, true);       // Compute the state of the node
                if (HasDiscontinuousTimeMovementOccured)
                {
                    DiscontinuousTimeMovement();
                    HasDiscontinuousTimeMovementOccured = false;
                }

                subtree.Current.ClipNextTickByParent();    // Perform NextTick clipping, stage 1

                // Make a note to visit for stage 2, only for ClockGroups
                subtree.Current.NeedsPostfixTraversal = (subtree.Current is ClockGroup);
            }

            _parent.ComputeTreeStateRoot();  // Re-clip the next tick estimates by children

            // Fire the events indicating that we've invalidated this whole subtree
            subtree.Reset();
            while (subtree.MoveNext())
            {
                // CurrentTimeInvalidated should be fired first to give AnimationStorage a chance
                // to update the value.  We directly set the flags, then fire RaiseAccumulatedEvents
                // to avoid involving the TimeManager for this local subtree operation.
                subtree.Current.CurrentTimeInvalidatedEventRaised = true;
                subtree.Current.CurrentStateInvalidatedEventRaised = true;
                subtree.Current.CurrentGlobalSpeedInvalidatedEventRaised = true;
                subtree.Current.RaiseAccumulatedEvents();
            }
        }
            
        /// <summary>
        /// Internal helper for moving to new API. Sets a speed multiplier for the Clock's speed.
        /// </summary>
        /// <param name="ratio">
        /// The ratio by which to multiply the Clock's speed.
        /// </param>
        internal void InternalSetSpeedRatio(double ratio)
        {
            Debug.Assert(IsRoot);

            _rootData.PendingSpeedRatio = ratio;
        }


        /// <summary>
        /// Internal helper. Schedules an end for some specified time in the future.
        /// </summary>
        internal void InternalSkipToFill()
        {
            Debug.Assert(IsRoot);
            
            TimeSpan? effectiveDuration;

            effectiveDuration = ComputeEffectiveDuration();

            // Throw an exception if the active period extends forever.
            if (effectiveDuration == null)
            {
                // Can't seek to the end if the simple duration is not resolved
                throw new InvalidOperationException(SR.Get(SRID.Timing_SkipToFillDestinationIndefinite));
            }

            // Offset to the end; override preceding seek requests
            IsInteractivelyStopped = false;
            PendingInteractiveStop = false;
            ResetNodesWithSlip();  // Reset sync tracking

            RootBeginPending = false;
            _rootData.PendingSeekDestination = effectiveDuration.Value;  // Seek to the end time

            NotifyNewEarliestFutureActivity();
        }


        /// <summary>
        /// Internal helper for moving to new API. Removes a timeline from its active or fill period.
        /// Timeline can be restarted with an interactive Begin call.
        /// </summary>
        internal void InternalStop()
        {
            Debug.Assert(IsRoot);

            PendingInteractiveStop = true;

            // Cancel all non-persistent interactive requests
            _rootData.PendingSeekDestination = null;
            RootBeginPending = false;
            ResetNodesWithSlip();  // Reset sync tracking

            NotifyNewEarliestFutureActivity();
        }


        /// <summary>
        /// Raises the events that occured since the last tick and reset their state.
        /// </summary>
        internal void RaiseAccumulatedEvents()
        {
            try  // We are calling user-defined delegates, if they throw we must ensure that we leave the Clock in a valid state
            {
                // CurrentTimeInvalidated should fire first.  This is because AnimationStorage hooks itself
                // up to this event in order to invalidate whichever DependencyProperty this clock may be
                // animating.  User code in any of these callbacks may query the value of that DP - if they
                // do so before AnimationStorage has a chance to invalidate they will get the wrong value.
                if (CurrentTimeInvalidatedEventRaised)
                {
                    FireCurrentTimeInvalidatedEvent();
                }

                if (CurrentGlobalSpeedInvalidatedEventRaised)
                {
                    FireCurrentGlobalSpeedInvalidatedEvent();

                    // Tell the Clocks that they have changed Speed
                    SpeedChanged();
                }

                if (CurrentStateInvalidatedEventRaised)
                {
                    FireCurrentStateInvalidatedEvent();

                    // Since the state has been invalidated this means that
                    // we've got a discontinuous time movemement. Tell the clock
                    if (!CurrentGlobalSpeedInvalidatedEventRaised)
                    {
                        DiscontinuousTimeMovement();
                    }
                }

                if (CompletedEventRaised)
                {
                    FireCompletedEvent();
                }

                if (RemoveRequestedEventRaised)
                {
                    FireRemoveRequestedEvent();
                }
            }
            finally  // Reset the flags to make the state consistent, even if the user has thrown
            {
                CurrentTimeInvalidatedEventRaised = false;
                CurrentGlobalSpeedInvalidatedEventRaised = false;
                CurrentStateInvalidatedEventRaised = false;
                CompletedEventRaised = false;
                RemoveRequestedEventRaised = false;

                IsInEventQueue = false;
            }
        }


        /// <summary>
        /// Raises the Completed event.
        /// </summary>
        /// <remarks>
        /// We only need to raise this event once per tick. If we've already
        /// raised it in this tick, do nothing.
        /// </remarks>
        internal void RaiseCompleted()
        {
            Debug.Assert(!IsTimeManager);

            CompletedEventRaised = true;
            if (!IsInEventQueue)
            {
                _timeManager.AddToEventQueue(this);
                IsInEventQueue = true;
            }
        }


        /// <summary>
        /// Raises the CurrentGlobalSpeedInvalidated event.
        /// </summary>
        /// <remarks>
        /// We only need to raise this event once per tick. If we've already
        /// raised it in this tick, do nothing.
        /// </remarks>
        internal void RaiseCurrentGlobalSpeedInvalidated()
        {
            // ROOT Debug.Assert(!IsTimeManager);

            CurrentGlobalSpeedInvalidatedEventRaised = true;
            if (!IsInEventQueue)
            {
                _timeManager.AddToEventQueue(this);
                IsInEventQueue = true;
            }
        }


        /// <summary>
        /// Raises the CurrentStateInvalidated event.
        /// </summary>
        internal void RaiseCurrentStateInvalidated()
        {
            Debug.Assert(!IsTimeManager);

            if (_currentClockState == ClockState.Stopped)  // If our state changed to stopped
            {
                Stopped();
            }

            CurrentStateInvalidatedEventRaised = true;
            if (!IsInEventQueue)
            {
                _timeManager.AddToEventQueue(this);
                IsInEventQueue = true;
            }
        }


        /// <summary>
        /// Raises the CurrentTimeInvalidated event. This enqueues the event for later dispatch
        /// if we are in a tick operation.
        /// </summary>
        internal void RaiseCurrentTimeInvalidated()
        {
            Debug.Assert(!IsTimeManager);

            CurrentTimeInvalidatedEventRaised   = true;
            if (!IsInEventQueue)
            {
                _timeManager.AddToEventQueue(this);
                IsInEventQueue = true;
            }
        }

        /// <summary>
        /// Raises the RemoveRequested event.
        /// </summary>
        /// <remarks>
        /// We only need to raise this event once per tick. If we've already
        /// raised it in this tick, do nothing.
        /// </remarks>
        internal void RaiseRemoveRequested()
        {
            Debug.Assert(!IsTimeManager);

            RemoveRequestedEventRaised = true;
            if (!IsInEventQueue)
            {
                _timeManager.AddToEventQueue(this);
                IsInEventQueue = true;
            }
        }

        // Reset all currently cached state
        internal void ResetCachedStateToStopped()
        {
            _currentGlobalSpeed = null;
            _currentIteration = null;
            IsBackwardsProgressingGlobal = false;

            _currentProgress = null;
            _currentTime = null;
            _currentClockState = ClockState.Stopped;
        }

        // Reset IsInSyncPeriod for this node and all children if any (ClockGroup handles this).
        // We do this whenever a discontinuous interactive action (seek/begin/stop) is performed.
        internal virtual void ResetNodesWithSlip()
        {
            if (_syncData != null)
            {
                _syncData.IsInSyncPeriod = false;  // Reset sync tracking
            }
        }


        /// <summary>
        /// Check if our descendants have resolved their duration, and resets the HasDescendantsWithUnresolvedDuration
        /// flag from true to false once that happens.
        /// </summary>
        /// <returns>Returns true when this node or one of its descendants have unresolved duration.</returns>
        internal virtual void UpdateDescendantsWithUnresolvedDuration()
        {
            if (HasResolvedDuration)
            {
                HasDescendantsWithUnresolvedDuration = false;
            }
        }

        #endregion // Internal Methods


        //
        // Internal Properties
        //

        #region Internal Properties
                
        
        /// <summary>
        /// Specifies the depth of this timeline in the timing tree. If the timeline does not have
        /// a parent, its depth value is zero.
        /// </summary>
        internal int Depth
        {
            get
            {
                // ROOT Debug.Assert(!IsTimeManager);

                return _depth;
            }
        }
     

        /// <summary>
        /// Returns the last time this timeline will become non-active if it is not
        /// clipped by the parent container first.  Null represents infinity.
        /// </summary>
        internal Duration EndOfActivePeriod
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                if (!HasResolvedDuration)
                {
                    return Duration.Automatic;
                }

                // Computed expiration time with respect to repeat behavior and natural duration;
                TimeSpan? expirationTime;       

                ComputeExpirationTime(out expirationTime);

                // We should start to use a Duration value for expirationTime which
                // will make this logic a lot easier.
                // We can also remove the check for HasResolvedDuration at the top of the
                // method if we can make expirationTime be Duration.Automatic where appropriate.

                if (expirationTime.HasValue)
                {
                    return expirationTime.Value;
                }
                else
                {
                    return Duration.Forever;
                }
            }
        }


        /// <summary>
        /// Gets the first child of this timeline.
        /// </summary>
        /// <value>
        /// Since a Clock doesn't have children we will always return null
        /// </value>
        internal virtual Clock FirstChild
        {
            get
            {
                return null;
            }
        }


        /// <summary>
        /// Internal unverified access to the CurrentState
        /// </summary>
        /// <remarks>
        /// Only ClockGroup should set the value
        /// </remarks>
        internal ClockState InternalCurrentClockState
        {
            get
            {
                return _currentClockState;
            }

            set
            {
                _currentClockState = value;
            }
        }


        /// <summary>
        /// Internal unverified access to the CurrentGlobalSpeed
        /// </summary>
        /// <remarks>
        /// Only ClockGroup should set the value
        /// </remarks>
        internal double? InternalCurrentGlobalSpeed
        {
            get
            {
                return _currentGlobalSpeed;
            }

            set
            {
                _currentGlobalSpeed = value;
            }
        }       


        /// <summary>
        /// Internal unverified access to the CurrentIteration
        /// </summary>
        /// <remarks>
        /// Only ClockGroup should set the value
        /// </remarks>
        internal Int32? InternalCurrentIteration
        {
            get
            {
                return _currentIteration;
            }

            set
            {
                _currentIteration = value;
            }
        }


        /// <summary>
        /// Internal unverified access to the CurrentProgress
        /// </summary>
        /// <remarks>
        /// Only ClockGroup should set the value
        /// </remarks>
        internal double? InternalCurrentProgress
        {
            get
            {
                return _currentProgress;
            }

            set
            {
                _currentProgress = value;
            }
        }


        /// <summary>
        /// The next GlobalTime that this clock may need a tick
        /// </summary>
        internal TimeSpan? InternalNextTickNeededTime
        {
            get
            {
                return _nextTickNeededTime;
            }

            set
            {
                _nextTickNeededTime = value;
            }
        }


        /// <summary>
        /// Unchecked internal access to the parent of this Clock.
        /// </summary>
        internal ClockGroup InternalParent
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                return _parent;
            }
        }

        /// <summary>
        /// Gets the current Duration for internal callers. This property will
        /// never return Duration.Automatic. If the _resolvedDuration of the Clock
        /// has not yet been resolved, this property will return Duration.Forever.
        /// Therefore, it's possible that the value of this property will change
        /// one time during the Clock's lifetime. It's not predictable when that
        /// change will occur, it's up to the custom Clock author.
        /// </summary>
        internal Duration ResolvedDuration
        {
            get
            {
                ResolveDuration();

                Debug.Assert(_resolvedDuration != Duration.Automatic, "_resolvedDuration should never be set to Automatic.");

                return _resolvedDuration;
            }
        }

        /// <summary>
        /// Gets the right sibling of this timeline.
        /// </summary>
        /// <value>
        /// The right sibling of this timeline if it's not the last in its parent's
        /// collection; otherwise, null.
        /// </value>
        internal Clock NextSibling
        {
            get
            {
                Debug.Assert(!IsTimeManager);
                Debug.Assert(_parent != null && !_parent.IsTimeManager);

                List<Clock> parentChildren = _parent.InternalChildren;
                if (_childIndex == parentChildren.Count - 1)
                {
                    return null;
                }
                else
                {
                    return parentChildren[_childIndex + 1];
                }
            }
        }


        /// <summary>
        /// Gets a cached weak reference to this clock.
        /// </summary>
        internal WeakReference WeakReference
        {
            get
            {
                WeakReference reference = _weakReference;

                if (reference == null)
                {
                    reference = new WeakReference(this);
                    _weakReference = reference;
                }

                return reference;
            }
        }

        /// <summary>
        /// Get the desired framerate of this clock
        /// </summary>
        internal int? DesiredFrameRate
        {
            get
            {
                int? returnValue = null;
                if (HasDesiredFrameRate)
                {
                    returnValue = _rootData.DesiredFrameRate;
                }

                return returnValue;
            }
        }
        

        //
        // Internal access to some of the flags
        //

        #region Internal Flag Accessors

        internal bool CompletedEventRaised
        {
            get
            {
                return GetFlag(ClockFlags.CompletedEventRaised);
            }
            set
            {
                SetFlag(ClockFlags.CompletedEventRaised, value);
            }
        }

        internal bool CurrentGlobalSpeedInvalidatedEventRaised
        {
            get
            {
                return GetFlag(ClockFlags.CurrentGlobalSpeedInvalidatedEventRaised);
            }
            set
            {
                SetFlag(ClockFlags.CurrentGlobalSpeedInvalidatedEventRaised, value);
            }
        }

        internal bool CurrentStateInvalidatedEventRaised
        {
            get
            {
                return GetFlag(ClockFlags.CurrentStateInvalidatedEventRaised);
            }
            set
            {
                SetFlag(ClockFlags.CurrentStateInvalidatedEventRaised, value);
            }
        }

        internal bool CurrentTimeInvalidatedEventRaised
        {
            get
            {
                return GetFlag(ClockFlags.CurrentTimeInvalidatedEventRaised);
            }
            set
            {
                SetFlag(ClockFlags.CurrentTimeInvalidatedEventRaised, value);
            }
        }

        private bool HasDesiredFrameRate
        {
            get
            {
                return GetFlag(ClockFlags.HasDesiredFrameRate);
            }
            set
            {
                SetFlag(ClockFlags.HasDesiredFrameRate, value);
            }
        }

        internal bool HasResolvedDuration
        {
            get
            {
                return GetFlag(ClockFlags.HasResolvedDuration);
            }
            set
            {
                SetFlag(ClockFlags.HasResolvedDuration, value);
            }
        }

        internal bool IsBackwardsProgressingGlobal
        {
            get
            {
                return GetFlag(ClockFlags.IsBackwardsProgressingGlobal);
            }
            set
            {
                SetFlag(ClockFlags.IsBackwardsProgressingGlobal, value);
            }
        }

        internal bool IsInEventQueue
        {
            get
            {
                return GetFlag(ClockFlags.IsInEventQueue);
            }
            set
            {
                SetFlag(ClockFlags.IsInEventQueue, value);
            }
        }


        /// <summary>
        /// Unchecked internal access to the paused state of the clock.
        /// </summary>
        /// <value></value>
        internal bool IsInteractivelyPaused
        {
            get
            {
                return GetFlag(ClockFlags.IsInteractivelyPaused);
            }
            set
            {
                SetFlag(ClockFlags.IsInteractivelyPaused, value);
            }
        }

        internal bool IsInteractivelyStopped
        {
            get
            {
                return GetFlag(ClockFlags.IsInteractivelyStopped);
            }
            set
            {
                SetFlag(ClockFlags.IsInteractivelyStopped, value);
            }
        }

        internal bool IsRoot
        {
            get
            {
                return GetFlag(ClockFlags.IsRoot);
            }
            set
            {
                SetFlag(ClockFlags.IsRoot, value);
            }
        }

        internal bool IsTimeManager
        {
            get
            {
                return GetFlag(ClockFlags.IsTimeManager);
            }
            set
            {
                SetFlag(ClockFlags.IsTimeManager, value);
            }
        }


        /// <summary>
        /// Returns true if the Clock traversed during the first tick pass.
        /// </summary>
        /// <returns></returns>
        internal bool NeedsPostfixTraversal
        {
            get
            {
                return GetFlag(ClockFlags.NeedsPostfixTraversal);
            }
            set
            {
                SetFlag(ClockFlags.NeedsPostfixTraversal, value);
            }
        }

        internal virtual bool NeedsTicksWhenActive
        {
            get
            {
                return GetFlag(ClockFlags.NeedsTicksWhenActive);
            }

            set
            {
                SetFlag(ClockFlags.NeedsTicksWhenActive, value);
            }
        }

        internal bool PauseStateChangedDuringTick
        {
            get
            {
                return GetFlag(ClockFlags.PauseStateChangedDuringTick);
            }
            set
            {
                SetFlag(ClockFlags.PauseStateChangedDuringTick, value);
            }
        }
     
        internal bool PendingInteractivePause
        {
            get
            {
                return GetFlag(ClockFlags.PendingInteractivePause);
            }
            set
            {
                SetFlag(ClockFlags.PendingInteractivePause, value);
            }
        }

        internal bool PendingInteractiveRemove
        {
            get
            {
                return GetFlag(ClockFlags.PendingInteractiveRemove);
            }
            set
            {
                SetFlag(ClockFlags.PendingInteractiveRemove, value);
            }
        }

        internal bool PendingInteractiveResume
        {
            get
            {
                return GetFlag(ClockFlags.PendingInteractiveResume);
            }
            set
            {
                SetFlag(ClockFlags.PendingInteractiveResume, value);
            }
        }

        internal bool PendingInteractiveStop
        {
            get
            {
                return GetFlag(ClockFlags.PendingInteractiveStop);
            }
            set
            {
                SetFlag(ClockFlags.PendingInteractiveStop, value);
            }
        }
        
        internal bool RemoveRequestedEventRaised
        {
            get
            {
                return GetFlag(ClockFlags.RemoveRequestedEventRaised);
            }
            set
            {
                SetFlag(ClockFlags.RemoveRequestedEventRaised, value);
            }
        }

        private bool HasDiscontinuousTimeMovementOccured
        {
            get
            {
                return GetFlag(ClockFlags.HasDiscontinuousTimeMovementOccured);
            }
            set
            {
                SetFlag(ClockFlags.HasDiscontinuousTimeMovementOccured, value);
            }
        }

        internal bool HasDescendantsWithUnresolvedDuration
        {
            get
            {
                return GetFlag(ClockFlags.HasDescendantsWithUnresolvedDuration);
            }
            set
            {
                SetFlag(ClockFlags.HasDescendantsWithUnresolvedDuration, value);
            }
        }
         
        private bool HasSeekOccuredAfterLastTick
        {
            get
            {
                return GetFlag(ClockFlags.HasSeekOccuredAfterLastTick);
            }
            set
            {
                SetFlag(ClockFlags.HasSeekOccuredAfterLastTick, value);
            }
        }

        #endregion // Internal Flag Accessors

        #endregion // Internal Properties


        //
        // Private Methods
        //

        #region Private Methods

        //
        // Local State Computation Helpers
        //

        #region Local State Computation Helpers

        //
        // Seek, Begin and Pause are internally implemented by adjusting the begin time.
        // For example, when paused, each tick moves the begin time forward so that 
        // overall the clock hasn't moved. 
        //
        // Note that _beginTime is an offset from the parent's begin.  Since
        // these are root clocks, the parent is the TimeManager, and we 
        // must add in the CurrentGlobalTime
        private void AdjustBeginTime()
        {
            Debug.Assert(IsRoot);  // root clocks only; non-roots have constant begin time
            Debug.Assert(_rootData != null);

            // Process the effects of Seek, Begin, and Pause; delay request if not all durations are resolved in this subtree.
            if (_rootData.PendingSeekDestination.HasValue && !HasDescendantsWithUnresolvedDuration)
            {
                Debug.Assert(!RootBeginPending);  // we can have either a begin or a seek, not both

                // Adjust the begin time such that our current time equals PendingSeekDestination
                _beginTime = CurrentGlobalTime - DivideTimeSpan(_rootData.PendingSeekDestination.Value, _appliedSpeedRatio);
                if (CanGrow)  // One of our descendants has set this flag on us
                {
                    _currentIterationBeginTime = _beginTime;  // We relied on a combination of _currentIterationBeginTime and _currentIteration for our state
                    _currentIteration = null;  // Therefore, we should reset both to reset our position
                    ResetSlipOnSubtree();
                }
                UpdateSyncBeginTime();

                _rootData.PendingSeekDestination = null;

                // We have seeked, so raise all events signifying that no assumptions can be made about our state
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);
                while (subtree.MoveNext())
                {
                    subtree.Current.RaiseCurrentStateInvalidated();
                    subtree.Current.RaiseCurrentTimeInvalidated();
                    subtree.Current.RaiseCurrentGlobalSpeedInvalidated();
                }
            }
            else if (RootBeginPending)
            {
                // RootBeginPending is set when a root is parented to a tree (in AddToRoot()).
                // It allows us to interpret Timeline.BeginTime as an offset from the current
                // time and thus schedule a begin in the future.

                _beginTime = CurrentGlobalTime + _timeline.BeginTime;
                if (CanGrow)  // One of our descendants has set this flag on us
                {
                    _currentIterationBeginTime = _beginTime;  // We should be just starting our first iteration now
                }
                UpdateSyncBeginTime();

                RootBeginPending = false;
            }
            else if ((IsInteractivelyPaused || _rootData.InteractiveSpeedRatio == 0) &&
                     (_syncData == null || !_syncData.IsInSyncPeriod))
            // We were paused at the last tick, so move _beginTime by the delta from last tick to this one
            // Only perform this iff we are *continuously* moving, e.g. if we haven't seeked between ticks.
            // SYNC NOTE: If we are syncing, then the sync code should be the one to make this adjustment
            // by using the Media's current time (which should already be paused).
            {
                if (_beginTime.HasValue)
                {
                    // Adjust for the speed of this timelineClock
                    _beginTime += _timeManager.LastTickDelta;
                    UpdateSyncBeginTime();

                    if (_currentIterationBeginTime.HasValue)  // One of our descendants has set this flag on us
                    {
                        _currentIterationBeginTime += _timeManager.LastTickDelta;
                    }
                }
            }

            // Adjust for changes to the speed ratio
            if (_rootData.PendingSpeedRatio.HasValue)
            {
                double pendingSpeedRatio = _rootData.PendingSpeedRatio.Value * _timeline.SpeedRatio;

                // If the calculated speed ratio is 0, we reset it to 1. I believe this is
                // because we don't want to support pausing by setting speed ratio. Instead
                // they should call pause.
                if (pendingSpeedRatio == 0)
                {
                    pendingSpeedRatio = 1;
                }

                Debug.Assert(_beginTime.HasValue);                

                // Below code uses the above assumption that beginTime has a value
                TimeSpan previewParentTime = CurrentGlobalTime;
                
                if (_currentIterationBeginTime.HasValue)
                {
                    // Adjusting SpeedRatio is not a discontiuous event, we don't want to reset slip after doing this
                    _currentIterationBeginTime = previewParentTime - MultiplyTimeSpan(previewParentTime - _currentIterationBeginTime.Value,
                                                                                      _appliedSpeedRatio / pendingSpeedRatio);
                }
                else
                {
                    _beginTime = previewParentTime - MultiplyTimeSpan(previewParentTime - _beginTime.Value,
                                                                      _appliedSpeedRatio / pendingSpeedRatio);
                }

                RaiseCurrentGlobalSpeedInvalidated();

                // _appliedSpeedRatio represents the speed ratio we're actually using
                // for this Clock.
                _appliedSpeedRatio = pendingSpeedRatio;

                // _rootData.InteractiveSpeedRatio represents the actual user set interactive
                // speed ratio value even though we may override it if it's 0.
                _rootData.InteractiveSpeedRatio = _rootData.PendingSpeedRatio.Value;  

                // Clear out the new pending speed ratio since we've finished applying it.
                _rootData.PendingSpeedRatio = null;

                UpdateSyncBeginTime();
            }

            return;
        }


        // Apply the effects of having DFR set on a root clock
        internal void ApplyDesiredFrameRateToGlobalTime()
        {
            if (HasDesiredFrameRate)
            {
                _rootData.LastAdjustedGlobalTime = _rootData.CurrentAdjustedGlobalTime;
                _rootData.CurrentAdjustedGlobalTime = GetCurrentDesiredFrameTime(_timeManager.InternalCurrentGlobalTime);
            }
        }


        // Apply the effects of having DFR set on a root clock
        internal void ApplyDesiredFrameRateToNextTick()
        {
            Debug.Assert(IsRoot);

            if (HasDesiredFrameRate && InternalNextTickNeededTime.HasValue)
            {
                // If we have a desired frame rate, it should greater than 
                // zero (the default for the field.)
                Debug.Assert(_rootData.DesiredFrameRate > 0);

                // We "round" our next tick needed time up to the next frame
                TimeSpan nextDesiredTick = InternalNextTickNeededTime == TimeSpan.Zero ? _rootData.CurrentAdjustedGlobalTime
                                                                                       : InternalNextTickNeededTime.Value;


                InternalNextTickNeededTime = GetNextDesiredFrameTime(nextDesiredTick);
            }
        }



        // Determines the current iteration and uncorrected linear time accounting for _timeline.AutoReverse
        // At the end of this function call, the following state attributes are initialized:
        //   CurrentIteration
        private bool ComputeCurrentIteration(TimeSpan parentTime, double parentSpeed,
                                             TimeSpan? expirationTime,
                                             out TimeSpan localProgress)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped);
            Debug.Assert(_parent._currentClockState != ClockState.Stopped);
            Debug.Assert(_currentClockState != ClockState.Stopped);
            Debug.Assert(_currentDuration != Duration.Automatic, "_currentDuration should never be Automatic.");
            Debug.Assert(_beginTime.HasValue);

            Debug.Assert(parentTime >= _beginTime.Value);  // We are active or in postfill

            RepeatBehavior repeatBehavior = _timeline.RepeatBehavior;

            // Apply speed and offset, convert down to TimeSpan
            TimeSpan beginTimeForOffsetComputation = _currentIterationBeginTime.HasValue ? _currentIterationBeginTime.Value
                                                                                         : _beginTime.Value;
            TimeSpan offsetFromBegin = MultiplyTimeSpan(parentTime - beginTimeForOffsetComputation, _appliedSpeedRatio);

            // This may be set redundantly in one case, but simplifies code
            IsBackwardsProgressingGlobal = _parent.IsBackwardsProgressingGlobal;

            if (_currentDuration.HasTimeSpan) // For finite duration, use modulo arithmetic to compute current iteration
            {
                if (_currentDuration.TimeSpan == TimeSpan.Zero)  // We must be post-filling if we have gotten here
                {
                    Debug.Assert(_currentClockState != ClockState.Active);

                    // Assign localProgress to avoid compiler error
                    localProgress = TimeSpan.Zero;

                    // CurrentTime will always be zero.
                    _currentTime = TimeSpan.Zero;

                    Double currentProgress;

                    if (repeatBehavior.HasCount)
                    {
                        Double repeatCount = repeatBehavior.Count;

                        if (repeatCount <= 1.0)
                        {
                            currentProgress = repeatCount;
                            _currentIteration = 1;
                        }
                        else
                        {
                            Double wholePart = (Double)((Int32)repeatCount);

                            if (repeatCount == wholePart)
                            {
                                currentProgress = 1.0;
                                _currentIteration = (Int32)repeatCount;
                            }
                            else
                            {
                                currentProgress = repeatCount - wholePart;
                                _currentIteration = (Int32)(repeatCount + 1.0d);
                            }
                        }
                    }
                    else
                    {
                        // RepeatBehavior.HasTimeSpan cases:
                        //   I guess we could repeat a 0 Duration inside of a 0 or
                        // greater TimeSpan an infinite number of times. But that's
                        // not really helpful to the user.

                        // RepeatBehavior.Forever cases:
                        //   The situation here is that we have done an infinite amount
                        // of work in zero time, so exact answers are hard to determine:
                        //   The "correct" current iteration may be Double.PositiveInfinity,
                        // however returning this just makes our API too tricky to work with.
                        //   There is no "correct" current progress.

                        // In both cases we'll say we repeated one whole iteration exactly 
                        // once to make things easy for the user.

                        _currentIteration = 1;
                        currentProgress = 1.0;
                    }

                    // Adjust progress for AutoReverse.

                    if (_timeline.AutoReverse)
                    {
                        if (currentProgress == 1.0)
                        {
                            currentProgress = 0.0;
                        }
                        else if (currentProgress < 0.5)
                        {
                            currentProgress *= 2.0;
                        }
                        else
                        {
                            currentProgress = 1.0 - ((currentProgress - 0.5) * 2.0);
                        }
                    }

                    _currentProgress = currentProgress;

                    return true;
                }
                else   // CurrentDuration.TimeSpan != TimeSpan.Zero
                {
                    if (_currentClockState == ClockState.Filling && repeatBehavior.HasCount && !_currentIterationBeginTime.HasValue)
                    {
                        //
                        // This block definitely needs a long comment.
                        //
                        // Basically, roundoff errors in the computation of offsetFromBegin 
                        // can cause us to calculate localProgress incorrectly.
                        // Normally this doesn't matter, since we'll be off by a
                        // miniscule amount.  However, if we have a Filling Clock that
                        // is exactly on a boundary, offsetFromBegin % duration should be 0.
                        // We check for this special case below.  If we have any rounding
                        // errors in this situation, the modulus will not be 0 and we'll
                        // Fill at the wrong place (for example, we may think the Clock
                        // is Filling at the very beginning of its second iteration when
                        // it should be Filling at the very end of its first). 
                        //
                        // The precision error is due to dividing, then multiplying by,
                        // appliedSpeedRatio when computing offsetFromBegin(to see this trace
                        // back the computation of parentTime, expirationTime, and 
                        // effectiveDuration). The specific codepath that does
                        // this is only executed when we have a Filling clock with 
                        // RepeatBehavior.HasCount.  In this special case we can avoid
                        // the precision error by calculating offsetFromBegin directly.
                        //

                        TimeSpan optimizedOffsetFromBegin;
                        double scalingFactor = repeatBehavior.Count;

                        if (_timeline.AutoReverse)
                        {
                            scalingFactor *= 2;
                        }

                        optimizedOffsetFromBegin = MultiplyTimeSpan(_resolvedDuration.TimeSpan, scalingFactor);

                        Debug_VerifyOffsetFromBegin(offsetFromBegin.Ticks, optimizedOffsetFromBegin.Ticks);
                        
                        offsetFromBegin = optimizedOffsetFromBegin;
                    }

                    int newIteration;

                    if (_currentIterationBeginTime.HasValue)
                    {
                        ComputeCurrentIterationWithGrow(parentTime, expirationTime, out localProgress, out newIteration);
                    }
                    else  // Regular scenario -- no Grow behavior
                    {
                        localProgress = TimeSpan.FromTicks(offsetFromBegin.Ticks % _currentDuration.TimeSpan.Ticks);
                        newIteration = (int)(offsetFromBegin.Ticks / _resolvedDuration.TimeSpan.Ticks);  // Iteration count starting from 0
                    }

                    // Iteration boundary cases depend on which direction the parent progresses and if we are Filling
                    if ((localProgress == TimeSpan.Zero)
                        && (newIteration > 0)
                        // Detect a boundary case past the first zero (begin point)

                        && (_currentClockState == ClockState.Filling || _parent.IsBackwardsProgressingGlobal))
                    {
                        // Special post-fill case:                        
                        // We hit 0 progress because of wraparound in modulo arithmetic.  However, for post-fill we don't
                        // want to wrap around to zero; we compensate for that here.  The only legal way to hit zero
                        // post-fill is at the ends of autoreversed segments, which are handled by logic further below.
                        // Note that parentTime is clamped to expirationTime in post-fill situations, so even if the
                        // actual parentTime is larger, it would still get clamped and then potentially wrapped to 0.

                        // We are at 100% progress of previous iteration, instead of 0% progress of next one
                        // Back up to previous iteration
                        localProgress = _currentDuration.TimeSpan;
                        newIteration--;
                    }

                    // Invert the localProgress for odd (AutoReversed) paths
                    if (_timeline.AutoReverse)
                    {
                        if ((newIteration & 1) == 1)  // We are on a reversing segment
                        {
                            if (localProgress == TimeSpan.Zero)
                            {
                                // We're exactly at an AutoReverse inflection point.  Any active children
                                // of this clock will be filling for this point only.  The next tick
                                // time needs to be 0 so that they can go back to active; filling clocks
                                // aren't ordinarily ticked.

                                InternalNextTickNeededTime = TimeSpan.Zero;
                            }

                            localProgress = _currentDuration.TimeSpan - localProgress;
                            IsBackwardsProgressingGlobal = !IsBackwardsProgressingGlobal;
                            parentSpeed = -parentSpeed;  // Negate parent speed here for tick logic, since we negated localProgress
                        }
                        newIteration = newIteration / 2;  // Definition of iteration with AutoReverse is a front and back segment, divide by 2
                    }

                    _currentIteration = 1 + newIteration;  // Officially, iterations are numbered from 1

                    // This is where we predict tick logic for approaching an iteration boundary
                    // We only need to do this if NTWA == false because otherwise, we already have NTNT = zero
                    if (_currentClockState == ClockState.Active && parentSpeed != 0 && !NeedsTicksWhenActive)
                    {
                        TimeSpan timeUntilNextBoundary;

                        if (localProgress == TimeSpan.Zero)  // We are currently exactly at a boundary
                        {
                            timeUntilNextBoundary = DivideTimeSpan(_currentDuration.TimeSpan, Math.Abs(parentSpeed));
                        }
                        else if (parentSpeed > 0)  // We are approaching the next iteration boundary (end or decel zone)
                        {
                            TimeSpan decelBegin = MultiplyTimeSpan(_currentDuration.TimeSpan, 1.0 - _timeline.DecelerationRatio);
                            timeUntilNextBoundary = DivideTimeSpan(decelBegin - localProgress, parentSpeed);
                        }
                        else  // parentSpeed < 0, we are approaching the previous iteration boundary
                        {
                            TimeSpan accelEnd = MultiplyTimeSpan(_currentDuration.TimeSpan, _timeline.AccelerationRatio);
                            timeUntilNextBoundary = DivideTimeSpan(accelEnd - localProgress, parentSpeed);
                        }

                        TimeSpan proposedNextTickTime = CurrentGlobalTime + timeUntilNextBoundary;

                        if (!InternalNextTickNeededTime.HasValue || proposedNextTickTime < InternalNextTickNeededTime.Value)
                        {
                            InternalNextTickNeededTime = proposedNextTickTime;
                        }
                    }
                }
            }
            else // CurrentDuration is Forever
            {
                Debug.Assert(_currentDuration == Duration.Forever, "_currentDuration has an invalid enum value.");
                Debug.Assert(_currentClockState == ClockState.Active
                          || (_currentClockState == ClockState.Filling
                              && expirationTime.HasValue
                              && parentTime >= expirationTime));

                localProgress = offsetFromBegin;
                _currentIteration = 1;  // We have infinite duration, so iteration is 1
            }

            return false;  // We aren't done computing state yet
        }


        // This should only be called for nodes which have RepeatBehavior and have ancestors which can slip;
        // It gets called after we move from our current iteration to a new one
        private void ComputeCurrentIterationWithGrow(TimeSpan parentTime, TimeSpan? expirationTime,
                                                     out TimeSpan localProgress, out int newIteration)
        {
            Debug.Assert(this is ClockGroup, "ComputeCurrentIterationWithGrow should only run on ClockGroups.");
            Debug.Assert(CanGrow, "ComputeCurrentIterationWithGrow should only run on clocks with CanGrow.");
            Debug.Assert(_currentIterationBeginTime.HasValue, "ComputeCurrentIterationWithGrow should only be called when _currentIterationBeginTime has a value.");
            Debug.Assert(_resolvedDuration.HasTimeSpan, "ComputeCurrentIterationWithGrow should only be called when _resolvedDuration has a value.");  // We must have a computed duration
            Debug.Assert(_currentDuration.HasTimeSpan, "ComputeCurrentIterationWithGrow should only be called when _currentDuration has a value.");

            TimeSpan offsetFromBegin = MultiplyTimeSpan(parentTime - _currentIterationBeginTime.Value, _appliedSpeedRatio);
            int iterationIncrement;

            if (offsetFromBegin < _currentDuration.TimeSpan)  // We fall within the same iteration as during last tick
            {
                localProgress = offsetFromBegin;
                iterationIncrement = 0;
            }
            else  // offsetFromBegin is larger than _currentDuration, so we have moved at least one iteration up
            {
                // This iteration variable is actually 0-based, but if we got into this IF block, we are past 0th iteration
                long offsetOnLaterIterations = (offsetFromBegin - _currentDuration.TimeSpan).Ticks;

                localProgress = TimeSpan.FromTicks(offsetOnLaterIterations % _resolvedDuration.TimeSpan.Ticks);
                iterationIncrement = 1 + (int)(offsetOnLaterIterations / _resolvedDuration.TimeSpan.Ticks);

                // Now, adjust to a new current iteration:
                // Use the current and resolved values of Duration to compute the beginTime for the latest iteration
                // We know that we at least have passed the current iteration, so add _currentIteration;
                // If we also passed subsequent iterations, then assume they have perfect durations (_resolvedDuration) each.
                _currentIterationBeginTime += _currentDuration.TimeSpan + MultiplyTimeSpan(_resolvedDuration.TimeSpan, iterationIncrement - 1);

                // If we hit the Filling state, we could fall exactly on the iteration finish boundary.
                // In this case, step backwards one iteration so that we are one iteration away from the finish
                if (_currentClockState == ClockState.Filling && expirationTime.HasValue && _currentIterationBeginTime >= expirationTime)
                {
                    if (iterationIncrement > 1)  // We last added a resolvedDuration, subtract it back out
                    {
                        _currentIterationBeginTime -= _resolvedDuration.TimeSpan;
                    }
                    else  // iterationIncrement == 1, we only added a currentDuration, subtract it back out
                    {
                        _currentIterationBeginTime -= _currentDuration.TimeSpan;
                    }
                }
                else  // We have not encountered a false finish due to entering Fill state
                {
                    // Reset all children's slip time here; NOTE that this will change our effective duration,
                    // but this will not become important until the next tick, when it will be recomputed anyway.
                    ResetSlipOnSubtree();
                }
            }

            newIteration = _currentIteration.HasValue ? iterationIncrement + (_currentIteration.Value - 1)
                                                      : iterationIncrement;
        }

        /// <summary>
        /// Determine if we are active, filling, or off
        /// parentTime is clamped if it is inside the postfill zone
        /// We have to handle reversed parent differently because we have closed-open intervals in global time
        /// </summary>
        /// <param name="expirationTime">Our computed expiration time, null if infinite.</param>
        /// <param name="parentTime">Our parent time.</param>
        /// <param name="parentSpeed">Our parent speed.</param>
        /// <param name="isInTick">Whether we are called from within a tick.</param>
        /// <returns></returns>
        private bool ComputeCurrentState(TimeSpan? expirationTime, ref TimeSpan parentTime, double parentSpeed, bool isInTick)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped);
            Debug.Assert(_parent._currentClockState != ClockState.Stopped);
            Debug.Assert(_beginTime.HasValue);

            FillBehavior fillBehavior = _timeline.FillBehavior;

            if (parentTime < _beginTime)  // Including special backward progressing case
            {
                ResetCachedStateToStopped();

                return true;  // Nothing more to compute here
            }
            else if (   expirationTime.HasValue
                     && parentTime >= expirationTime) // We are in postfill zone
            {
                RaiseCompletedForRoot(isInTick);
                
                if (fillBehavior == FillBehavior.HoldEnd)
#if IMPLEMENTED  // Uncomment when we enable new FillBehaviors
                    || fillBehavior == FillBehavior.HoldBeginAndEnd)
#endif
                {
                    ResetCachedStateToFilling();

                    parentTime = expirationTime.Value;  // Clamp parent time to expiration time
                    // We still don't know our current time or progress at this point
                }
                else
                {
                    ResetCachedStateToStopped();
                    return true;  // We are off, nothing more to compute
                }
            }
            else  // Else we are inside the active interval and thus active
            {
                _currentClockState = ClockState.Active;
            }

            // This is where we short-circuit Next Tick Needed logic while we are active
            if (parentSpeed != 0 && _currentClockState == ClockState.Active && NeedsTicksWhenActive)
            {
                InternalNextTickNeededTime = TimeSpan.Zero;  // We need ticks immediately
            }

            return false;  // There is more state to compute
        }


        // If we reach this function, we are active
        private bool ComputeCurrentSpeed(double localSpeed)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped);
            Debug.Assert(_parent._currentClockState != ClockState.Stopped);
            Debug.Assert(_currentClockState == ClockState.Active);  // Must be active at this point

            if (IsInteractivelyPaused)
            {
                _currentGlobalSpeed = 0;
            }
            else
            {
                localSpeed *= _appliedSpeedRatio;
                if (IsBackwardsProgressingGlobal)  // Negate speed if we are on a backwards arc of an autoreversing timeline
                {
                    localSpeed = -localSpeed;
                }
                // Get global speed by multiplying by parent global speed
                _currentGlobalSpeed = localSpeed * _parent._currentGlobalSpeed;
            }

            return false;  // There may be more state to compute yet
        }


        // Determines the time and local speed with accel+decel
        // At the end of this function call, the following state attributes are initialized:
        //   CurrentProgress
        //   CurrentTime
        private bool ComputeCurrentTime(TimeSpan localProgress, out double localSpeed)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped);
            Debug.Assert(_parent._currentClockState != ClockState.Stopped);
            Debug.Assert(_currentClockState != ClockState.Stopped);
            Debug.Assert(_currentDuration != Duration.Automatic, "_currentDuration should never be Automatic.");

            if (_currentDuration.HasTimeSpan)  // Finite duration, need to apply accel/decel
            {
                Debug.Assert(_currentDuration.TimeSpan > TimeSpan.Zero, "ComputeCurrentTime was entered with _currentDuration <= 0");

                double userAcceleration = _timeline.AccelerationRatio;
                double userDeceleration = _timeline.DecelerationRatio;
                double transitionTime = userAcceleration + userDeceleration;

                // The following assert is enforced when the Acceleration or Deceleration are set.
                Debug.Assert(transitionTime <= 1, "The values of the accel and decel attributes incorrectly add to more than 1.0");
                Debug.Assert(transitionTime >= 0, "The values of the accel and decel attributes incorrectly add to less than 0.0");

                double durationInTicks = (double)_currentDuration.TimeSpan.Ticks;
                double t = ((double)localProgress.Ticks) / durationInTicks;  // For tracking progress

                if (transitionTime == 0)    // Case of no accel/decel
                {
                    localSpeed = 1;
                    _currentTime = localProgress;
                }
                else
                {
                    double maxRate = 2 / (2 - transitionTime);

                    if (t < userAcceleration)
                    {
                        // Acceleration phase
                        localSpeed = maxRate * t / userAcceleration;
                        t = maxRate * t * t / (2 * userAcceleration);

                        // Animations with Deceleration cause the Timing system to
                        // keep ticking while idle.  Only reset NextTickNeededTime when we are
                        // Active.  When we (or our parent) is Filling, there is no non-linear
                        // unpredictability to our behavior that requires us to reset NextTickNeededTime.
                        if (_currentClockState == ClockState.Active
                         && _parent._currentClockState == ClockState.Active)
                        {
                            // We are in a non-linear segment, cannot linearly predict anything
                            InternalNextTickNeededTime = TimeSpan.Zero;
                        }
                    }
                    else if (t <= (1 - userDeceleration))
                    {
                        // Run-rate phase
                        localSpeed = maxRate;
                        t = maxRate * (t - userAcceleration / 2);
                    }
                    else
                    {
                        // Deceleration phase
                        double tc = 1 - t;  // t's complement from 1
                        localSpeed = maxRate * tc / userDeceleration;
                        t = 1 - maxRate * tc * tc / (2 * userDeceleration);

                        // Animations with Deceleration cause the Timing system to
                        // keep ticking while idle.  Only reset NextTickNeededTime when we are
                        // Active.  When we (or our parent) is Filling, there is no non-linear
                        // unpredictability to our behavior that requires us to reset NextTickNeededTime.
                        if (_currentClockState == ClockState.Active
                         && _parent._currentClockState == ClockState.Active)
                        {
                            // We are in a non-linear segment, cannot linearly predict anything
                            InternalNextTickNeededTime = TimeSpan.Zero;
                        }
                    }

                    _currentTime = TimeSpan.FromTicks((long)((t * durationInTicks) + 0.5));
                }

                _currentProgress = t;
            }
            else  // CurrentDuration is Forever
            {
                Debug.Assert(_currentDuration == Duration.Forever, "_currentDuration has an invalid enum value.");

                _currentTime = localProgress;
                _currentProgress = 0;
                localSpeed = 1;
            }

            return (_currentClockState != ClockState.Active);  // Proceed to calculate global speed if we are active
        }


        // Compute the duration
        private void ResolveDuration()
        {
            Debug.Assert(!IsTimeManager);

            if (!HasResolvedDuration)
            {
                Duration duration = NaturalDuration;

                if (duration != Duration.Automatic)
                {
                    _resolvedDuration = duration;
                    _currentDuration = duration;  // If CurrentDuration is different, we update it later in this method
                    HasResolvedDuration = true;
                }
                else
                {
                    Debug.Assert(_resolvedDuration == Duration.Forever, "_resolvedDuration should be Forever when NaturalDuration is Automatic.");
                }
            }

            if (CanGrow)
            {
                _currentDuration = CurrentDuration;
                if (_currentDuration == Duration.Automatic)
                {
                    _currentDuration = Duration.Forever;  // We treat Automatic as unresolved current duration
                }
            }

            // We have descendants (such as Media) which don't know their duration yet.  Note that this won't prevent us
            // from resolving our own Duration when it is explicitly set on the ParallelTimeline; therefore, we keep
            // a separate flag for the entire subtree.
            if (HasDescendantsWithUnresolvedDuration)
            {
                UpdateDescendantsWithUnresolvedDuration();  // See if this is still the case
            }
        }


        // This returns the effective duration of a clock.  The effective duration is basically the 
        // length of the clock's active period, taking into account speed ratio, repeat, and autoreverse.
        // Null is used to represent an infinite effective duration.
        private TimeSpan? ComputeEffectiveDuration()
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped || IsRoot);

            ResolveDuration();

            Debug.Assert(_resolvedDuration != Duration.Automatic, "_resolvedDuration should never be Automatic.");
            Debug.Assert(_currentDuration != Duration.Automatic, "_currentDuration should never be Automatic.");

            TimeSpan? effectiveDuration;
            RepeatBehavior repeatBehavior = _timeline.RepeatBehavior;

            if (_currentDuration.HasTimeSpan && _currentDuration.TimeSpan == TimeSpan.Zero)
            {
                // Zero-duration case ignores any repeat behavior
                effectiveDuration = TimeSpan.Zero;
            }
            else if (repeatBehavior.HasCount)
            {
                if (repeatBehavior.Count == 0)  // This clause avoids multiplying an infinite duration by zero
                {
                    effectiveDuration = TimeSpan.Zero;
                }
                else if (_currentDuration == Duration.Forever)
                {
                    effectiveDuration = null;  // We use Null to represent infinite duration
                }
                else if (!CanGrow)  // Case of finite duration
                {
                    Debug.Assert(_currentDuration.HasTimeSpan, "_currentDuration is invalid, neither Forever nor a TimeSpan.");
                    Debug.Assert(_currentDuration == _resolvedDuration, "For clocks which cannot grow, _currentDuration must equal _resolvedDuration.");

                    double scalingFactor = repeatBehavior.Count / _appliedSpeedRatio;
                    if (_timeline.AutoReverse)
                    {
                        scalingFactor *= 2;
                    }

                    effectiveDuration = MultiplyTimeSpan(_currentDuration.TimeSpan, scalingFactor);
                }
                else  // Finite duration, CanGrow: _currentDuration may be different from _resolvedDuration
                {
                    Debug.Assert(_resolvedDuration.HasTimeSpan, "_resolvedDuration is invalid, neither Forever nor a TimeSpan.");
                    Debug.Assert(_currentDuration.HasTimeSpan, "_currentDuration is invalid, neither Forever nor a TimeSpan.");

                    TimeSpan previousIterationDuration = TimeSpan.Zero;
                    double presentAndFutureIterations = repeatBehavior.Count;
                    double presentAndFutureDuration;  // Note: this variable is not scaled by speedRatio, we scale it further down:

                    // If we have growth, we have to take prior iterations into account as a FIXED time span
                    if (CanGrow && _currentIterationBeginTime.HasValue && _currentIteration.HasValue)
                    {
                        Debug.Assert(_beginTime.HasValue);  // _currentIterationBeginTime.HasValue implies _beginTime.HasValue
                        presentAndFutureIterations -= (_currentIteration.Value - 1);
                        previousIterationDuration = _currentIterationBeginTime.Value - _beginTime.Value;
                    }

                    if (presentAndFutureIterations <= 1)  // This means we are on our last iteration
                    {
                        presentAndFutureDuration = ((double)_currentDuration.TimeSpan.Ticks) * presentAndFutureIterations;
                    }
                    else  // presentAndFutureIterations > 1, we are not at the last iteration, so count _currentDuration a full one time
                    {
                        presentAndFutureDuration = ((double)_currentDuration.TimeSpan.Ticks)     // Current iteration; below is the future iteration length
                                                 + ((double)_resolvedDuration.TimeSpan.Ticks) * (presentAndFutureIterations - 1);
                    }

                    if (_timeline.AutoReverse)
                    {
                        presentAndFutureDuration *= 2;  // Double the remaining duration with AutoReverse
                    }

                    effectiveDuration = TimeSpan.FromTicks((long)(presentAndFutureDuration / _appliedSpeedRatio + 0.5)) + previousIterationDuration;
                }
            }
            else if (repeatBehavior.HasDuration)
            {
                effectiveDuration = repeatBehavior.Duration;
            }
            else  // Repeat behavior is Forever
            {
                Debug.Assert(repeatBehavior == RepeatBehavior.Forever);  // Only other valid enum value
                effectiveDuration = null;
            }

            return effectiveDuration;
        }


        // Run new eventing logic on root nodes
        // Note that Completed events are fired from a different place (currently, ComputeCurrentState)
        private void ComputeEvents(TimeSpan? expirationTime,
                                   TimeIntervalCollection parentIntervalCollection)
        {
            // We clear CurrentIntervals here in case that we don't reinitialize it in the method.
            // Unless there is something to project, we assume the null state
            ClearCurrentIntervalsToNull();

            if (_beginTime.HasValue
                // If we changed to a paused state during this tick then we still
                // changed state and the events should be computed.
                // If we were paused and we resumed during this tick, even though
                // we are now active, no progress has been made during this tick.
                && !(IsInteractivelyPaused ^ PauseStateChangedDuringTick))
            {
                Duration postFillDuration;             // This is Zero when we have no fill zone

                if (expirationTime.HasValue)
                {
                    postFillDuration = Duration.Forever;
                }
                else
                {
                    postFillDuration = TimeSpan.Zero;  // There is no reachable postfill zone because the active period is infinite
                }

                // consider caching this condition
                // We check whether our active period exists before using it to compute intervals
                if (!expirationTime.HasValue       // If activePeriod extends forever, 
                    || expirationTime >= _beginTime)  // OR if activePeriod extends to or beyond _beginTime, 
                {
                    // Check for CurrentTimeInvalidated
                    TimeIntervalCollection activePeriod;
                    if (expirationTime.HasValue)
                    {
                        if (expirationTime == _beginTime)
                        {
                            activePeriod = TimeIntervalCollection.Empty;
                        }
                        else
                        {
                            activePeriod = TimeIntervalCollection.CreateClosedOpenInterval(_beginTime.Value, expirationTime.Value);
                        }
                    }
                    else  // expirationTime is infinity
                    {
                        activePeriod = TimeIntervalCollection.CreateInfiniteClosedInterval(_beginTime.Value);
                    }

                    // If we have an intersection between parent domain times and the interval over which we
                    // change, our time was invalidated
                    if (parentIntervalCollection.Intersects(activePeriod))
                    {
                        ComputeIntervalsWithParentIntersection(
                            parentIntervalCollection,
                            activePeriod,
                            expirationTime,
                            postFillDuration);
                    }
                    else if (postFillDuration != TimeSpan.Zero &&       // Our active period is finite and we have fill behavior
                             _timeline.FillBehavior == FillBehavior.HoldEnd)  // Check for state changing between Filling and Stopped
                    {
                        ComputeIntervalsWithHoldEnd(
                            parentIntervalCollection,
                            expirationTime);
                    }
                }
            }

            // It is important to launch this method at the end of ComputeEvents, because it checks
            // whether the StateInvalidated event had been raised earlier in this method.
            if (PendingInteractiveRemove)
            {
                RaiseRemoveRequestedForRoot();
                RaiseCompletedForRoot(true);  // At this time, we also want to raise the Completed event;
                                              // this code always runs during a tick.
                PendingInteractiveRemove = false;
            }
        }


        // Find end time that is defined by our repeat behavior.  Null is used to represent
        // an infinite expiration time.
        private bool ComputeExpirationTime(out TimeSpan? expirationTime)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped || IsRoot);

            TimeSpan? effectiveDuration;

            // removable when layout caching is implemented
            if (!_beginTime.HasValue)
            {
                Debug.Assert(!_currentIterationBeginTime.HasValue, "_currentIterationBeginTime should not have a value when _beginTime has no value.");
                expirationTime = null;
                return true;
            }

            Debug.Assert(_beginTime.HasValue);

            effectiveDuration = ComputeEffectiveDuration();

            if (effectiveDuration.HasValue)
            {
                expirationTime = _beginTime + effectiveDuration;

                // Precaution against slipping at the last frame of media: don't permit the clock to finish this tick yet
                if (_syncData != null && _syncData.IsInSyncPeriod && !_syncData.SyncClockHasReachedEffectiveDuration)
                {
                    expirationTime += TimeSpan.FromMilliseconds(50);  // This compensation is roughly one frame of video
                }
            }
            else
            {
                expirationTime = null; // infinite expiration time
            }

            return false;  // More state to compute
        }


        // Compute values for root children that reflect interactivity
        // We may modify SpeedRatio if the interactive SpeedRatio was changed
        private bool ComputeInteractiveValues()
        {
            bool exitEarly = false;

            Debug.Assert(IsRoot);

            // Check for a pending stop first.  This allows us to exit early.
            if (PendingInteractiveStop)  
            {
                PendingInteractiveStop = false;
                IsInteractivelyStopped = true;

                // If we are disabled, no other interactive state is kept
                _beginTime = null;
                _currentIterationBeginTime = null;
                if (CanGrow)
                {
                    ResetSlipOnSubtree();
                }

                // Process the state for the whole subtree; the events will later
                // be replaced with invalidation-style events
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

                // revise this code for perf -- we may get by without the traversal.
                while (subtree.MoveNext())  
                {
                    Clock current = subtree.Current;

                    if (current._currentClockState != ClockState.Stopped)
                    {
                        current.ResetCachedStateToStopped();

                        current.RaiseCurrentStateInvalidated();
                        current.RaiseCurrentTimeInvalidated();
                        current.RaiseCurrentGlobalSpeedInvalidated();
                    }
                    else
                    {
                        subtree.SkipSubtree();
                    }
                }
            }
          
            if (IsInteractivelyStopped)
            {
                // If we are disabled, no other interactive state is kept
                Debug.Assert(_beginTime == null);
                Debug.Assert(_currentClockState == ClockState.Stopped);

                ResetCachedStateToStopped();

                InternalNextTickNeededTime = null;

                // Can't return here: still need to process pending pause and resume
                // which can be set independent of the current state of the clock.
                exitEarly = true;  
            }
            else
            {
                // Clocks that are currently paused or have a pending seek, begin, or change to the
                // speed ratio need to adjust their begin time.
                AdjustBeginTime();
            }

            //
            // If we were about to pause or resume, set flags accordingly.
            // This must be done after adjusting the begin time so that we don't
            // appear to pause one tick early.
            //
            if (PendingInteractivePause)
            {
                Debug.Assert(!IsInteractivelyPaused);     // Enforce invariant: cannot be pausePending when already paused
                Debug.Assert(!PendingInteractiveResume);  // Enforce invariant: cannot be both pause and resumePending

                PendingInteractivePause = false;

                RaiseCurrentGlobalSpeedInvalidated();

                // Update paused state for the entire subtree
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);
                while (subtree.MoveNext())
                {
                    subtree.Current.IsInteractivelyPaused = true;
                    subtree.Current.PauseStateChangedDuringTick = true;
                }
            }

            if (PendingInteractiveResume)
            {
                Debug.Assert(IsInteractivelyPaused);
                Debug.Assert(!PendingInteractivePause);

                // We will no longer have to do paused begin time adjustment
                PendingInteractiveResume = false;

                // During pause, our speed was zero.  Unless we are filling, invalidate the speed.
                if (_currentClockState != ClockState.Filling)
                {
                    RaiseCurrentGlobalSpeedInvalidated();
                }

                // Update paused state for the entire subtree
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);
                while (subtree.MoveNext())
                {
                    subtree.Current.IsInteractivelyPaused = false;
                    subtree.Current.PauseStateChangedDuringTick = true;
                }
            }

            return exitEarly;
        }


        private void ComputeIntervalsWithHoldEnd(
            TimeIntervalCollection parentIntervalCollection,
            TimeSpan? endOfActivePeriod)
        {
            Debug.Assert(endOfActivePeriod.HasValue);

            TimeIntervalCollection fillPeriod = TimeIntervalCollection.CreateInfiniteClosedInterval(endOfActivePeriod.Value);

            if (parentIntervalCollection.Intersects(fillPeriod))  // We enter or leave Fill period
            {
                TimeSpan relativeBeginTime = _currentIterationBeginTime.HasValue ? _currentIterationBeginTime.Value : _beginTime.Value;
                ComputeCurrentFillInterval(parentIntervalCollection,
                                           relativeBeginTime, endOfActivePeriod.Value,
                                           _currentDuration, _appliedSpeedRatio,
                                           _timeline.AccelerationRatio,
                                           _timeline.DecelerationRatio,
                                           _timeline.AutoReverse);

                if (parentIntervalCollection.IntersectsInverseOf(fillPeriod))  // ... and we don't intersect the Active period, so we must go in or out of the Stopped period.
                {
                    RaiseCurrentStateInvalidated();
                    RaiseCurrentTimeInvalidated();
                    RaiseCurrentGlobalSpeedInvalidated();

                    AddNullPointToCurrentIntervals();  // Count the stopped state by projecting the null point.
                }
            }
        }

        
        private void ComputeIntervalsWithParentIntersection(
            TimeIntervalCollection parentIntervalCollection,
            TimeIntervalCollection activePeriod,
            TimeSpan? endOfActivePeriod,
            Duration postFillDuration)
        {
            // Make sure that our periodic function is aligned to the boundary of the current iteration, regardless of prior slip
            TimeSpan relativeBeginTime = _currentIterationBeginTime.HasValue ? _currentIterationBeginTime.Value : _beginTime.Value;
            
            RaiseCurrentTimeInvalidated();

            // Check for state changing between Active and the union of (Filling, Stopped)
            if (parentIntervalCollection.IntersectsInverseOf(activePeriod))
            {
                RaiseCurrentStateInvalidated();
                RaiseCurrentGlobalSpeedInvalidated();
            }
            else if (parentIntervalCollection.IntersectsPeriodicCollection(
                relativeBeginTime, _currentDuration, _appliedSpeedRatio,
                _timeline.AccelerationRatio,
                _timeline.DecelerationRatio,
                _timeline.AutoReverse))
            // Else we were always inside the active period, check for non-linear speed invalidations
            {
                RaiseCurrentGlobalSpeedInvalidated();
            }
            // Else our speed has not changed, but our iteration may have been invalidated
            else if (parentIntervalCollection.IntersectsMultiplePeriods(
                relativeBeginTime, _currentDuration, _appliedSpeedRatio))
            {
                HasDiscontinuousTimeMovementOccured = true;
                if (_syncData != null)
                {
                    _syncData.SyncClockDiscontinuousEvent = true;  // Notify the syncing node of discontinuity
                }
            }

            // Compute our output intervals
            ComputeCurrentIntervals(parentIntervalCollection,
                                    relativeBeginTime, endOfActivePeriod,
                                    postFillDuration, _currentDuration, _appliedSpeedRatio,
                                    _timeline.AccelerationRatio,
                                    _timeline.DecelerationRatio,
                                    _timeline.AutoReverse);
        }


        /// <remarks>
        /// GillesK: performTickOperations means that we process the pending events
        /// </remarks>
        private void ComputeLocalStateHelper(bool performTickOperations, bool seekedAlignedToLastTick)
        {
            Debug.Assert(!IsTimeManager);

            TimeSpan? parentTime;         // Computed parent-local time
            TimeSpan? expirationTime;     // Computed expiration time with respect to repeat behavior and resolved duration;
            TimeSpan localProgress;       // Computed time inside simple duration

            TimeSpan  parentTimeValue;
            double?   parentSpeed;        // Parent's CurrentGlobalSpeed
            TimeIntervalCollection parentIntervalCollection;

            double    localSpeed; 
            bool      returnDelayed = false;    // workaround for integrating new eventing logic

            // In this function, 'true' return values allow us to exit early

            // We first compute parent parameters; with SlipBehavior, we may modify our parentIntervalCollection
            if (ComputeParentParameters(out parentTime, out parentSpeed,
                                        out parentIntervalCollection, seekedAlignedToLastTick))
            {
                returnDelayed = true;
            }

            // Now take potential SlipBehavior into account:
            if (_syncData != null && _syncData.IsInSyncPeriod && _parent.CurrentState != ClockState.Stopped)  // We are already in a slip zone
            {
                Debug.Assert(parentTime.HasValue);  // If parent isn't stopped, it must have valid time and speed
                Debug.Assert(parentSpeed.HasValue);
                // consider this behavior when performTickOperations==false, e.g. on SkipToFill
                ComputeSyncSlip(ref parentIntervalCollection, parentTime.Value, parentSpeed.Value);
            }

            ResolveDuration();

            // We only calculate the Interactive values when we are processing the pending events

            if (performTickOperations && IsRoot)
            {
                // Special case for root-children, which may have interactivity
                if (ComputeInteractiveValues())
                {
                    returnDelayed = true;
                }
            }

            // Check whether we are entering a sync period.  This includes cases when we have
            // ticked before the beginning, then past the end of a sync period; we still have to
            // move back to the exact beginning of the tick period.  We handle cases where
            // we seek (HasSeekOccuredAfterLastTick) in a special way, by not synchronizing with the beginning.
            // Also, if the parent has been paused prior to this tick, we cannot enter the sync zone, so skip the call.
            if (_syncData != null && !_syncData.IsInSyncPeriod && _parent.CurrentState != ClockState.Stopped &&
                (!parentIntervalCollection.IsEmptyOfRealPoints || HasSeekOccuredAfterLastTick))
            {
                Debug.Assert(parentTime.HasValue);  // Cannot be true unless parent is stopped
                // We use the parent's TIC as a way of determining its earliest non-null time
                ComputeSyncEnter(ref parentIntervalCollection, parentTime.Value);
            }

            if (ComputeExpirationTime(out expirationTime))
            {
                returnDelayed = true;
            }

            // Run the eventing logic here
            if (performTickOperations)
            {
                ComputeEvents(expirationTime, parentIntervalCollection);
            }

            if (returnDelayed)  // If we delayed returning until now, proceed to do so
            {
                return;
            }

            Debug.Assert(_beginTime.HasValue);
            Debug.Assert(parentTime.HasValue);
            parentTimeValue = parentTime.Value;

            // Determines the next time we need to tick
            if (ComputeNextTickNeededTime(expirationTime, parentTimeValue, parentSpeed.Value))
            {
                return;
            }

            // Determines if we are active, filling, or off
            if (ComputeCurrentState(expirationTime, ref parentTimeValue, parentSpeed.Value, performTickOperations))
            {
                return;
            }

            // Determines the current iteration
            if (ComputeCurrentIteration(parentTimeValue, parentSpeed.Value,
                                        expirationTime, out localProgress))
            {
                return;
            }

            // Determines the current time and local speed with accel+decel
            if (ComputeCurrentTime(localProgress, out localSpeed))
            {
                return;
            }

            // Determines the current speed
            if (ComputeCurrentSpeed(localSpeed))
            {
                return;
            }
        }


        //
        // Determine the next needed tick time for approaching a StateInvalidated boundary
        //
        // Note that ComputeCurrentIteration and ComputeCurrentState also modify the 
        // NextTickNeededTime and consequently must run after this method. 
        //
        private bool ComputeNextTickNeededTime(TimeSpan? expirationTime,
                                               TimeSpan parentTime, double parentSpeed)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped);
            Debug.Assert(_parent._currentClockState != ClockState.Stopped);
            Debug.Assert(_beginTime.HasValue);

            if (parentSpeed == 0)
            {
                // we may be able to optimize paused timelines further
                InternalNextTickNeededTime = IsInteractivelyPaused ? TimeSpan.Zero : (TimeSpan?)null;
            }
            else
            {
                double invertedParentSpeed = 1.0 / parentSpeed;
                TimeSpan? timeUntilNextBoundary = null;

                //
                // Calculate the time in ms until begin or expiration time.
                // They are positive if we're heading towards one of these periods, negative if heading away.
                // This takes into account reversing clocks (so a clock heading back to begin will have
                // a positive timeUntilBegin).  
                //
                // timeUntilNextBoundary will be the first of these three boundaries that we hit.  
                // Negative values are obviously ignored.
                // 

                TimeSpan timeUntilBegin = MultiplyTimeSpan(_beginTime.Value - parentTime, invertedParentSpeed);

                //
                // If the time until a boundary is 0 (i.e. we've ticked exactly on a boundary)
                // we'll ask for another tick immediately.
                // This is only relevant for reversing clocks, which, when on a boundary, are defined
                // to have the 'previous' state, not the 'next' state. Thus they need one more
                // tick for the state change to happen.
                //
                if (timeUntilBegin >= TimeSpan.Zero)
                {
                    timeUntilNextBoundary = timeUntilBegin;
                }

                if (expirationTime.HasValue)
                {
                    TimeSpan timeUntilExpiration = MultiplyTimeSpan(expirationTime.Value - parentTime, invertedParentSpeed);

                    if (timeUntilExpiration >= TimeSpan.Zero &&
                        (!timeUntilNextBoundary.HasValue || timeUntilExpiration < timeUntilNextBoundary.Value))
                    {
                        timeUntilNextBoundary = timeUntilExpiration;
                    }
                }

                //
                // Set the next tick needed time depending on whether we're 
                // headed towards a boundary.
                //
                if (timeUntilNextBoundary.HasValue)
                {
                    // We are moving towards some ClockState boundary either begin or expiration
                    InternalNextTickNeededTime = CurrentGlobalTime + timeUntilNextBoundary;
                }
                else
                {
                    // We are not moving towards any boundary points
                    InternalNextTickNeededTime = null;
                }
            }
            return false;
        }


        // Compute the parent's time; by the time we reach this method, we know that we
        //  have a non-root parent.
        private bool ComputeParentParameters(out TimeSpan? parentTime, out double? parentSpeed,
                                             out TimeIntervalCollection parentIntervalCollection,
                                             bool seekedAlignedToLastTick)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(!IsInteractivelyStopped || IsRoot);

            if (IsRoot)  // We are a root child, use time manager time
            {
                Debug.Assert(_rootData != null, "A root Clock must have the _rootData structure initialized.");
                HasSeekOccuredAfterLastTick = seekedAlignedToLastTick || (_rootData.PendingSeekDestination != null);  // We may have a seek request pending

                // We don't have a TimeManager that is on, so we are off, nothing more to compute
                if (_timeManager == null || _timeManager.InternalIsStopped)
                {
                    ResetCachedStateToStopped();
                    parentTime = null;  // Assign parentTime to avoid compiler error
                    parentSpeed = null;
                    InternalNextTickNeededTime = TimeSpan.Zero;  // When TimeManager wakes up, we will need an update
                    parentIntervalCollection = TimeIntervalCollection.Empty;
                    return true;
                }
                else  // We have a valid global time;
                {
                    parentSpeed = 1.0;   // TimeManager defines the rate at which time moves, e.g. it moves at 1X speed                    
                    parentIntervalCollection = _timeManager.InternalCurrentIntervals;

                    if (HasDesiredFrameRate)
                    {
                        // Change the parent's interval collection to include all time intervals since the last time
                        // we ticked this root node.  Due to DFR, we may have skipped a number of "important" ticks.
                        parentTime = _rootData.CurrentAdjustedGlobalTime;

                        if (!parentIntervalCollection.IsEmptyOfRealPoints)
                        { 
                            parentIntervalCollection = parentIntervalCollection.SetBeginningOfConnectedInterval(
                                                                                 _rootData.LastAdjustedGlobalTime);
                        }
                    }
                    else 
                    {
                        parentTime = _timeManager.InternalCurrentGlobalTime;
                    }
                    
                    return false;
                }
            }
            else  // We are a deeper node
            {
                HasSeekOccuredAfterLastTick = seekedAlignedToLastTick || _parent.HasSeekOccuredAfterLastTick;  // We may have a seek request pending

                parentTime = _parent._currentTime;  // This is Null if parent is off; we still init the 'out' parameter
                parentSpeed = _parent._currentGlobalSpeed;
                parentIntervalCollection = _parent.CurrentIntervals;

                // Find the parent's current time
                if (_parent._currentClockState != ClockState.Stopped)  // We have a parent that is active or filling
                {
                    return false;
                }
                else  // Else parent is off, so we are off, nothing more to compute
                {
                    // Before setting our state to Stopped make sure that we
                    // fire the proper event if we change state.
                    if (_currentClockState != ClockState.Stopped)
                    {
                        RaiseCurrentStateInvalidated();
                        RaiseCurrentGlobalSpeedInvalidated();
                        RaiseCurrentTimeInvalidated();
                    }
                    ResetCachedStateToStopped();
                    InternalNextTickNeededTime = null;
                    return true;
                }
            }
        }
   
        // Abbreviations for variables expressing time units:
        //   PT: Parent time (e.g. our begin time is expressed in parent time coordinates)
        //   LT: Local time  (e.g. our duration is expressed in local time coordinates)
        //   ST: Sync time -- this is the same as local time iff (_syncData.SyncClock == this) e.g. we are the sync clock,
        //       otherwise it is our child's time coordinates (this happens when we are a container with SlipBehavior.Slip).
        //   SPT: The sync clock's parent's time coordinates.  When this IS the sync clock, this is our parent coordinates.
        //       otherwise, it is our local coordinates.
        private void ComputeSyncEnter(ref TimeIntervalCollection parentIntervalCollection,
                                      TimeSpan currentParentTimePT)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(_parent != null);
            Debug.Assert(_parent.CurrentState != ClockState.Stopped);
            
            // Parent is not stopped, so its TIC cannot be empty
            Debug.Assert(HasSeekOccuredAfterLastTick ||
                        (!parentIntervalCollection.IsEmptyOfRealPoints && parentIntervalCollection.FirstNodeTime <= currentParentTimePT));

            // SyncData points to our child if we have SlipBehavior, for CanSlip nodes it points to the node itself
            Debug.Assert(_syncData.SyncClock == this || _syncData.SyncClock._parent == this);
            Debug.Assert(CanSlip || _timeline is ParallelTimeline && ((ParallelTimeline)_timeline).SlipBehavior == SlipBehavior.Slip);

            Debug.Assert(_syncData != null);
            Debug.Assert(!_syncData.IsInSyncPeriod);

            // Verify our limitations on slip functionality, but don't throw here for perf
            Debug.Assert(_timeline.AutoReverse == false);
            Debug.Assert(_timeline.AccelerationRatio == 0);
            Debug.Assert(_timeline.DecelerationRatio == 0);

            // With these limitations, we can easily preview our CurrentTime:
            if (_beginTime.HasValue && currentParentTimePT >= _beginTime.Value)
            {
                TimeSpan relativeBeginTimePT = _currentIterationBeginTime.HasValue ? _currentIterationBeginTime.Value : _beginTime.Value;
                TimeSpan previewCurrentOffsetPT = currentParentTimePT - relativeBeginTimePT;  // This is our time offset (not yet scaled by speed)
                TimeSpan previewCurrentTimeLT = MultiplyTimeSpan(previewCurrentOffsetPT, _appliedSpeedRatio);  // This is what our time would be

                // We can only enter sync period if we are past the syncClock's begin time
                if (_syncData.SyncClock == this || previewCurrentTimeLT >= _syncData.SyncClockBeginTime)
                {
                    // We have two very different scenarios: seek and non-seek enter
                    if (HasSeekOccuredAfterLastTick)  // We have seeked, see if we fell into a sync period
                    {
                        // If we haven't returned yet, we are not past the end of the sync period on the child
                        // Also, we are not Stopped prior to BeginTime.
                        TimeSpan? expirationTimePT;
                        ComputeExpirationTime(out expirationTimePT);

                        // This is to verify we did not seek past our active period duration
                        if (!expirationTimePT.HasValue || currentParentTimePT < expirationTimePT.Value)
                        {
                            TimeSpan ourSyncTimeST = (_syncData.SyncClock == this) ?
                                   previewCurrentTimeLT :
                                   MultiplyTimeSpan(previewCurrentTimeLT - _syncData.SyncClockBeginTime,
                                                    _syncData.SyncClockSpeedRatio);

                            TimeSpan? syncClockEffectiveDurationST = _syncData.SyncClockEffectiveDuration;
                            if (_syncData.SyncClock == this ||
                                !syncClockEffectiveDurationST.HasValue || ourSyncTimeST < syncClockEffectiveDurationST)
                            {
                                // If the sync child has a specified duration
                                Duration syncClockDuration = _syncData.SyncClockResolvedDuration;

                                if (syncClockDuration.HasTimeSpan)
                                {
                                    _syncData.PreviousSyncClockTime = TimeSpan.FromTicks(ourSyncTimeST.Ticks % syncClockDuration.TimeSpan.Ticks);
                                    _syncData.PreviousRepeatTime = ourSyncTimeST - _syncData.PreviousSyncClockTime;
                                }
                                else if (syncClockDuration == Duration.Forever)
                                {
                                    _syncData.PreviousSyncClockTime = ourSyncTimeST;
                                    _syncData.PreviousRepeatTime = TimeSpan.Zero;
                                }
                                else
                                {
                                    Debug.Assert(syncClockDuration == Duration.Automatic);
                                    // If we seek into an Automatic syncChild's duration, we may overseek it, so throw an exception
                                    throw new InvalidOperationException(SR.Get(SRID.Timing_SeekDestinationAmbiguousDueToSlip));
                                }

                                // This is the heart of the HasSeekOccuredAfterLastTick codepath; we don't adjust our
                                // time, but note to do so for the succeeding ticks.
                                _syncData.IsInSyncPeriod = true;
                            }
                        }
                    }
                    else  // Non-seek, regular case
                    {
                        TimeSpan? previousSyncParentTimeSPT = (_syncData.SyncClock == this) ?
                                                             parentIntervalCollection.FirstNodeTime :
                                                             _currentTime;

                        if (!previousSyncParentTimeSPT.HasValue
                            || _syncData.SyncClockDiscontinuousEvent
                            || previousSyncParentTimeSPT.Value <= _syncData.SyncClockBeginTime)
                        // Not seeking this tick, different criteria for entering sync period.
                        // We don't care if we overshot the beginTime, because we will seek backwards
                        // to match the child's beginTime exactly.
                        // NOTE: _currentTime is actually our time at last tick, since it wasn't yet updated.
                        {
                            // First, adjust our beginTime so that we match the syncClock's begin, accounting for SpeedRatio
                            TimeSpan timeIntoSyncPeriodPT = previewCurrentOffsetPT;
                            if (_syncData.SyncClock != this)  // SyncClock is our child; account for SyncClock starting later than us
                            {
                                timeIntoSyncPeriodPT -= DivideTimeSpan(_syncData.SyncClockBeginTime, _appliedSpeedRatio);
                            }

                            // Offset our position to sync with media begin
                            if (_currentIterationBeginTime.HasValue)
                            {
                                _currentIterationBeginTime += timeIntoSyncPeriodPT;
                            }
                            else
                            {
                                _beginTime += timeIntoSyncPeriodPT;
                            }

                            UpdateSyncBeginTime();  // This ensures that our _cutoffTime is correctly applied

                            // Now, update the parent TIC to compensate for our slip
                            parentIntervalCollection = parentIntervalCollection.SlipBeginningOfConnectedInterval(timeIntoSyncPeriodPT);

                            _syncData.IsInSyncPeriod = true;
                            _syncData.PreviousSyncClockTime = TimeSpan.Zero;
                            _syncData.PreviousRepeatTime = TimeSpan.Zero;
                            _syncData.SyncClockDiscontinuousEvent = false;
                        }
                    }
                }
            }
        }


        // Abbreviations for variables expressing time units:
        //   PT: Parent time (e.g. our begin time is expressed in parent time coordinates)
        //   LT: Local time  (e.g. our duration is expressed in local time coordinates)
        //   ST: Sync time -- this is the same as local time iff (_syncData.SyncClock == this) e.g. we are the sync clock,
        //       otherwise it is our child's time coordinates (this happens when we are a container with SlipBehavior.Slip).
        //   SPT: The sync clock's parent's time coordinates.  When this IS the sync clock, this is our parent coordinates.
        //       otherwise, it is our local coordinates.
        private void ComputeSyncSlip(ref TimeIntervalCollection parentIntervalCollection,
                                     TimeSpan currentParentTimePT, double currentParentSpeed)
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(_parent != null);
            Debug.Assert(_syncData != null);
            Debug.Assert(_syncData.IsInSyncPeriod);

            // SyncData points to our child if we have SlipBehavior, for CanSlip nodes it points to the node itself
            Debug.Assert(_syncData.SyncClock == this || _syncData.SyncClock._parent == this);

            // The overriding assumption for slip limitations is that the parent's intervals are
            // "connected", e.g. not broken into multiple intervals.  This is always true for roots.
            Debug.Assert(!parentIntervalCollection.IsEmpty);  // The parent isn't Stopped, so it must have a TIC

            // From now on, we assume that the parent's TIC begins from the parent's CurrentTime at
            // at the previous tick.  Ensure that the parent is moving forward.
            Debug.Assert(parentIntervalCollection.IsEmptyOfRealPoints || parentIntervalCollection.FirstNodeTime <= currentParentTimePT);
            Debug.Assert(currentParentSpeed >= 0);

            // We now extract this information from the TIC.  If we are paused, we have an empty TIC and assume parent time has not changed.
            TimeSpan previousParentTimePT = parentIntervalCollection.IsEmptyOfRealPoints ? currentParentTimePT
                                                                                         : parentIntervalCollection.FirstNodeTime;
            TimeSpan parentElapsedTimePT = currentParentTimePT - previousParentTimePT;
            // Our elapsed time is assumed to be a simple linear scale of the parent's time,
            // as long as we are inside of the sync period.
            TimeSpan ourProjectedElapsedTimeLT = MultiplyTimeSpan(parentElapsedTimePT, _appliedSpeedRatio);

            TimeSpan syncTimeST = _syncData.SyncClock.GetCurrentTimeCore();
            TimeSpan syncElapsedTimeST = syncTimeST - _syncData.PreviousSyncClockTime;  // Elapsed from last tick

            if (syncElapsedTimeST > TimeSpan.Zero)  // Only store the last value if it is greater than
                                                  //  the old value.  Note we can use either >= or > here.
            {
                // Check whether sync has reached the end of our effective duration
                TimeSpan? effectiveDurationST = _syncData.SyncClockEffectiveDuration;
                Duration syncDuration = _syncData.SyncClockResolvedDuration;

                if (effectiveDurationST.HasValue &&
                    (_syncData.PreviousRepeatTime + syncTimeST >= effectiveDurationST.Value))
                {
                    _syncData.IsInSyncPeriod = false;  // This is the last time we need to sync
                    _syncData.PreviousRepeatTime = TimeSpan.Zero;
                    _syncData.SyncClockDiscontinuousEvent = false;  // Make sure we don't reenter the sync period
                }
                // Else check if we should wrap the simple duration due to repeats, and set previous times accordingly                
                else if (syncDuration.HasTimeSpan && syncTimeST >= syncDuration.TimeSpan)
                {
                    // If we have a single repetition, then we would be done here;
                    // However, we may just have reached the end of an iteration on repeating media;
                    // In this case, we still sync this particular moment, but we should reset the
                    // previous sync clock time to zero, and increment the PreviousRepeatTime.
                    // This tick, media should pick up a corresponding DiscontinuousMovement caused
                    // by a repeat, and reset itself to zero as well.
                    _syncData.PreviousSyncClockTime = TimeSpan.Zero;
                    _syncData.PreviousRepeatTime += syncDuration.TimeSpan;
                }
                else  // Don't need to wrap around
                {
                    _syncData.PreviousSyncClockTime = syncTimeST;
                }                
            }
            else  // If the sync timeline went backwards, pretend it just didn't move.
            {
                syncElapsedTimeST = TimeSpan.Zero;
            }

            // Convert elapsed time to local coordinates, not necessarily same as sync clock coordinates
            TimeSpan syncElapsedTimeLT = (_syncData.SyncClock == this)
                                       ? syncElapsedTimeST
                                       : DivideTimeSpan(syncElapsedTimeST, _syncData.SyncClockSpeedRatio);

            // This is the actual slip formula: how much is the slipping clock lagging behind?
            TimeSpan parentTimeSlipPT = parentElapsedTimePT - DivideTimeSpan(syncElapsedTimeLT, _appliedSpeedRatio);
            // NOTE: The above line does the same as this:
            //     parentTimeSlip = syncSlip / _appliedSpeedRatio
            // ...but it maintains greater accuracy and prevents a bug where parentTimeSlip ends up 1 tick greater
            // that it should be, thus becoming larger than parentElapsedTimePT and causing us to suddenly fall out
            // of our sync period.

            // Unless the media is exactly perfect, we will have non-zero slip time; we assume that it isn't
            // perfect, and always adjust our time accordingly.
            if (_currentIterationBeginTime.HasValue)
            {
                _currentIterationBeginTime += parentTimeSlipPT;
            }
            else
            {
                _beginTime += parentTimeSlipPT;
            }

            UpdateSyncBeginTime();  

            parentIntervalCollection = parentIntervalCollection.SlipBeginningOfConnectedInterval(parentTimeSlipPT);

            return;
        }

        private void ResetSlipOnSubtree()
        {
            // Reset all children's slip time here; NOTE that this will change our effective duration,
            // but this should not become important until the next tick, when it will be recomputed anyway.
            PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, false);  // No iteration at this node
            while (subtree.MoveNext())
            {
                Clock current = subtree.Current;
                Debug.Assert(!current.IsRoot, "Root nodes never should reset their Slip amounts with ResetSlipOnSubtree(), even when seeking.");

                if (current._syncData != null)
                {
                    current._syncData.IsInSyncPeriod = false;
                    current._syncData.SyncClockDiscontinuousEvent = true;
                }

                if (current.CanSlip)
                {
                    current._beginTime = current._timeline.BeginTime;  // _beginTime could have slipped with media nodes
                    current._currentIteration = null;
                    current.UpdateSyncBeginTime();
                    current.HasDiscontinuousTimeMovementOccured = true;
                }
                else if (current.CanGrow)  // If it's a repeating container with slippable descendants...
                {
                    current._currentIterationBeginTime = current._beginTime;  // ...reset its current iteration as well
                    current._currentDuration = current._resolvedDuration;  // Revert currentDuration back to default size
                }
                else  // Otherwise we know not to traverse any further
                {
                    subtree.SkipSubtree();
                }
            }
        }

        #endregion // Local State Computation Helpers

        #region Event Helpers

        /// <summary>
        /// Adds a delegate to the list of event handlers on this object.
        /// </summary>
        /// <param name="key">
        /// A unique identifier for the event handler.  Since Clock events
        /// mirror Timeline events the callers of this method will pass in
        /// keys from Timeline
        /// </param>
        /// <param name="handler">The delegate to add</param>
        private void AddEventHandler(EventPrivateKey key, Delegate handler)
        {
            Debug.Assert(!IsTimeManager);

            if (_eventHandlersStore == null)
            {
                _eventHandlersStore = new EventHandlersStore();
            }

            _eventHandlersStore.Add(key, handler);

            VerifyNeedsTicksWhenActive();
        }

        /// <summary>
        /// Immediately fire the Loaded Event
        /// </summary>
        private void FireCompletedEvent()
        {
            FireEvent(Timeline.CompletedKey);
        }

        /// <summary>
        /// Immediately fire the GlobalSpeedInvalidated Event
        /// </summary>
        private void FireCurrentGlobalSpeedInvalidatedEvent()
        {
            FireEvent(Timeline.CurrentGlobalSpeedInvalidatedKey);
        }


        /// <summary>
        /// Immediately fire the State Invalidated Event
        /// </summary>
        private void FireCurrentStateInvalidatedEvent()
        {
            FireEvent(Timeline.CurrentStateInvalidatedKey);
        }


        /// <summary>
        /// Immediately fire the CurrentTimeInvalidated Event
        /// </summary>
        private void FireCurrentTimeInvalidatedEvent()
        {
            FireEvent(Timeline.CurrentTimeInvalidatedKey);
        }


        /// <summary>
        /// Fires the given event
        /// </summary>
        /// <param name="key">The unique key representing the event to fire</param>
        private void FireEvent(EventPrivateKey key)
        {
            if (_eventHandlersStore != null)
            {
                EventHandler handler = (EventHandler)_eventHandlersStore.Get(key);

                if (handler != null)
                {
                    handler(this, null);
                }
            }
        }

        /// <summary>
        /// Immediately fire the RemoveRequested Event
        /// </summary>
        private void FireRemoveRequestedEvent()
        {
            FireEvent(Timeline.RemoveRequestedKey);
        }

        // Find the last closest time that falls on the boundary of the Desired Frame Rate
        private TimeSpan GetCurrentDesiredFrameTime(TimeSpan time)
        {
            return GetDesiredFrameTime(time, +0);
        }

        // Find the closest time that falls on the boundary of the Desired Frame Rate, advancing [frameOffset] frames forward
        private TimeSpan GetDesiredFrameTime(TimeSpan time, int frameOffset)
        {
            Debug.Assert(_rootData.DesiredFrameRate > 0);

            Int64 desiredFrameRate = _rootData.DesiredFrameRate;
            Int64 desiredFrameNumber = (time.Ticks * desiredFrameRate) / s_TimeSpanTicksPerSecond + frameOffset;

            Int64 desiredFrameTick = (desiredFrameNumber * s_TimeSpanTicksPerSecond) / desiredFrameRate;

            return TimeSpan.FromTicks(desiredFrameTick);
        }

        // Find the next closest time that falls on the boundary of the Desired Frame Rate
        private TimeSpan GetNextDesiredFrameTime(TimeSpan time)
        {
            return GetDesiredFrameTime(time, +1);
        }


        /// <summary>
        /// Removes a delegate from the list of event handlers on this object.
        /// </summary>
        /// <param name="key">
        /// A unique identifier for the event handler.  Since Clock events
        /// mirror Timeline events the callers of this method will pass in
        /// keys from Timeline
        /// </param>
        /// <param name="handler">The delegate to remove</param>
        private void RemoveEventHandler(EventPrivateKey key, Delegate handler)
        {
            Debug.Assert(!IsTimeManager);

            if (_eventHandlersStore != null)
            {
                _eventHandlersStore.Remove(key, handler);

                if (_eventHandlersStore.Count == 0)
                {
                    _eventHandlersStore = null;
                }
            }

            UpdateNeedsTicksWhenActive();
        }

        #endregion // Event Helpers


        /// <summary>
        /// Adds this clock to the time manager.
        /// </summary>
        private void AddToTimeManager()
        {
            Debug.Assert(!IsTimeManager);
            Debug.Assert(_parent == null);
            Debug.Assert(_timeManager == null);

            TimeManager timeManager = MediaContext.From(Dispatcher).TimeManager;

            if (timeManager == null)
            {
                // The time manager has not been created or has been released
                // This occurs when we are shutting down. Simply return.
                return;
            }

            _parent = timeManager.TimeManagerClock;

            SetTimeManager(_parent._timeManager);

            Int32? desiredFrameRate = Timeline.GetDesiredFrameRate(_timeline);

            if (desiredFrameRate.HasValue)
            {
                HasDesiredFrameRate = true;
                _rootData.DesiredFrameRate = desiredFrameRate.Value;
            }

            // Store this clock in the root clock's child collection
            _parent.InternalRootChildren.Add(WeakReference);

            // Create a finalizer object that will clean up after we are gone
            _subtreeFinalizer = new SubtreeFinalizer(_timeManager);

            // Fix up depth values
            PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

            while (subtree.MoveNext())
            {
                Clock current = subtree.Current;
                current._depth = current._parent._depth + 1;
            }

            // Perform any necessary updates
            if (IsInTimingTree)
            {
                // Mark the tree as dirty
                _timeManager.SetDirty();
            }

            TimeIntervalCollection currentIntervals = TimeIntervalCollection.CreatePoint(_timeManager.InternalCurrentGlobalTime);
            currentIntervals.AddNullPoint();
            _timeManager.InternalCurrentIntervals = currentIntervals;


            //
            // Recompute the local state at this subtree
            //

            // Since _beginTime for a root clock takes the current time into account, 
            // it needs to be set during a tick. The call to ComputeLocalState
            // here isn't on a tick boundary, so we don't want to begin the clock yet.
            _beginTime = null;
            _currentIterationBeginTime = null;

            subtree.Reset();
            while (subtree.MoveNext())
            {
                subtree.Current.ComputeLocalState();       // Compute the state of the node
                subtree.Current.ClipNextTickByParent();    // Perform NextTick clipping, stage 1

                // Make a note to visit for stage 2, only for ClockGroups
                subtree.Current.NeedsPostfixTraversal = (subtree.Current is ClockGroup);
            }

            _parent.ComputeTreeStateRoot();  // Re-clip the next tick estimates by children

            // Adding is an implicit begin, so do that here.  This is done after
            // ComputeLocalState so that the begin will be picked up on the 
            // next tick.  Note that if _timeline.BeginTime == null we won't
            // start the clock.
            if (_timeline.BeginTime != null)
            {
                RootBeginPending = true;
            }
            
            NotifyNewEarliestFutureActivity();      // Make sure we get ticks
        }



        /// <summary>
        /// Helper for more elegant code dividing a TimeSpan by a double
        /// </summary>
        private TimeSpan DivideTimeSpan(TimeSpan timeSpan, double factor)
        {
            Debug.Assert(factor != 0);  // Divide by zero
            return TimeSpan.FromTicks((long)(((double)timeSpan.Ticks) / factor + 0.5));
        }


        /// <summary>
        /// Gets the value of a flag specified by the ClockFlags enum
        /// </summary>
        private bool GetFlag(ClockFlags flagMask)
        {
            return (_flags & flagMask) == flagMask;
        }


        /// <summary>
        /// Helper for more elegant code multiplying a TimeSpan by a double
        /// </summary>
        private static TimeSpan MultiplyTimeSpan(TimeSpan timeSpan, double factor)
        {
            return TimeSpan.FromTicks((long)(factor * (double)timeSpan.Ticks + 0.5));
        }


        private void NotifyNewEarliestFutureActivity()
        {
            PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);  // First reset the children

            while (subtree.MoveNext())
            {
                subtree.Current.InternalNextTickNeededTime = TimeSpan.Zero;
            }

            Clock current = _parent;  // Propagate the fact that we will need an update sooner up the chain
            while (current != null && current.InternalNextTickNeededTime != TimeSpan.Zero)
            {
                current.InternalNextTickNeededTime = TimeSpan.Zero;

                if (current.IsTimeManager)  // We went all the way up to the root node, notify TimeManager
                {
                    _timeManager.NotifyNewEarliestFutureActivity();
                    break;
                }

                current = current._parent;  
            }

            if (_timeManager != null)
            {
                // If we get here from within a Tick, this will force MediaContext to perform another subsequent Tick
                // on the TimeManager.  This will apply the requested interactive operations, so their results will
                // immediately become visible.
                _timeManager.SetDirty();
            }
        }


        // State that must remain *constant* outside of the active period
        private void ResetCachedStateToFilling()
        {
            _currentGlobalSpeed = 0;
            IsBackwardsProgressingGlobal = false;
            _currentClockState = ClockState.Filling;
        }


        /// <summary>
        /// Calls RaiseCompleted on this subtree when called on a root.
        /// </summary>
        /// <param name="isInTick">Whether we are in a tick.</param>
        private void RaiseCompletedForRoot(bool isInTick)
        {
            // Only perform this operation from root nodes.  Also, to avoid constantly calling Completed after passing
            // expirationTime, check that the state is invalidated, so we only raise the event as we are finishing.
            if (IsRoot && (CurrentStateInvalidatedEventRaised || !isInTick))
            {
                // If we are a root node, notify the entire subtree
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

                while (subtree.MoveNext())
                {
                    subtree.Current.RaiseCompleted();
                }
            }
        }

        private void RaiseRemoveRequestedForRoot()
        {
            Debug.Assert(IsRoot);  // This should only be called on root-child clocks

            // If we are a root node, notify the entire subtree
            PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

            while (subtree.MoveNext())
            {
                subtree.Current.RaiseRemoveRequested();
            }
        }


        /// <summary>
        /// Sets a specified flag with the given value
        /// </summary>
        private void SetFlag(ClockFlags flagMask, bool value)
        {
            if (value)
            {
                _flags |= flagMask;
            }
            else
            {
                _flags &= ~(flagMask);
            }
        }
 

        /// <summary>
        /// Sets the time manager for the subtree rooted at this timeline.
        /// </summary>
        /// <param name="timeManager">
        /// The new time manager.
        /// </param>
        private void SetTimeManager(TimeManager timeManager)
        {
            Debug.Assert(!IsTimeManager);

            // Optimize the no-op case away
            if (this._timeManager != timeManager)
            {
                // Set the new time manager for the whole subtree
                PrefixSubtreeEnumerator subtree = new PrefixSubtreeEnumerator(this, true);

                while (subtree.MoveNext())
                {
                    subtree.Current._timeManager = timeManager;
                }

                if (timeManager != null)
                {
                    // If we are joining a new time manager, issue any deferred calls
                    subtree.Reset();
                    while (subtree.MoveNext())
                    {
                        Clock current = subtree.Current;

                        // this is here in case we need to do any TimeManager-related initialization
                    }
                }
            }
        }

        private void UpdateNeedsTicksWhenActive()
        {
            // Currently we'll set NeedsTicksWhenActive to true
            // if any of the three events are set on this clock.
            
            // We should only need to set this when 
            // CurrentTimeInvalidated is set. 

            if (_eventHandlersStore == null)
            {
                NeedsTicksWhenActive = false;
            }
            else
            {
                NeedsTicksWhenActive = true;
            }
        }

        // This wrapper is invoked anytime we invalidate the _beginTime
        private void UpdateSyncBeginTime()
        {
            if (_syncData != null)
            {
                _syncData.UpdateClockBeginTime();
            }
        }

        private void VerifyNeedsTicksWhenActive()
        {
            if (!NeedsTicksWhenActive)  // We may need to update the tree to know that we need ticks
            {
                NeedsTicksWhenActive = true;  // Use this as a hint for NeedsTicksWhenActive

                NotifyNewEarliestFutureActivity();
            }
        }


        #endregion // Private Methods


        //
        // Private Properties
        //

        #region Private Properties


        /// <summary>
        /// True if we are in a running timing tree, false otherwise.
        /// </summary>
        private bool IsInTimingTree
        {
            get
            {
                Debug.Assert(!IsTimeManager);

                return (_timeManager != null) && (_timeManager.State != TimeState.Stopped);
            }
        }

        /// <summary>
        /// This isn't an Interactive method: InternalBegin does
        /// not make use of this flag.  It is set in AddToRoot() to
        /// notify a root clock that it must begin at the next tick.  
        /// Until this is set_beginTime for a root clock will be null; 
        /// AdjustBeginTime() sets _beginTime properly.
        /// </summary>
        private bool RootBeginPending
        {
            get
            {
                return GetFlag(ClockFlags.RootBeginPending);
            }
            set
            {
                SetFlag(ClockFlags.RootBeginPending, value);
            }
        }
        
        #endregion // Private Properties


        //
        // Nested Types
        //

        #region Nested Types

        [Flags]
        private enum ClockFlags : uint
        {
            IsTimeManager                            = 1 << 0,
            IsRoot                                   = 1 << 1,
            IsBackwardsProgressingGlobal             = 1 << 2,
            IsInteractivelyPaused                    = 1 << 3,
            IsInteractivelyStopped                   = 1 << 4,
            PendingInteractivePause                  = 1 << 5,
            PendingInteractiveResume                 = 1 << 6,
            PendingInteractiveStop                   = 1 << 7,
            PendingInteractiveRemove                 = 1 << 8,
            CanGrow                                  = 1 << 9,
            CanSlip                                  = 1 << 10,
            CurrentStateInvalidatedEventRaised       = 1 << 11,
            CurrentTimeInvalidatedEventRaised        = 1 << 12,
            CurrentGlobalSpeedInvalidatedEventRaised = 1 << 13,
            CompletedEventRaised                     = 1 << 14,
            RemoveRequestedEventRaised               = 1 << 15,
            IsInEventQueue                           = 1 << 16,
            NeedsTicksWhenActive                     = 1 << 17,
            NeedsPostfixTraversal                    = 1 << 18,
            PauseStateChangedDuringTick              = 1 << 19,
            RootBeginPending                         = 1 << 20,
            HasControllableRoot                      = 1 << 21,
            HasResolvedDuration                      = 1 << 22,
            HasDesiredFrameRate                      = 1 << 23,
            HasDiscontinuousTimeMovementOccured      = 1 << 24,
            HasDescendantsWithUnresolvedDuration     = 1 << 25,
            HasSeekOccuredAfterLastTick              = 1 << 26,
        }

        /// <summary>
        /// The result of a ResolveTimes method call.
        /// </summary>
        private enum ResolveCode
        {
            /// <summary>
            /// Nothing changed in resolving new times.
            /// </summary>
            NoChanges,
            /// <summary>
            /// Resolved times are different than before.
            /// </summary>
            NewTimes,
            /// <summary>
            /// The children of this timeline need a full reset time resolution. This flag
            /// indicates that a partial resolution needs to be prunned at the current
            /// timeline in favor of a full resolution for its children.
            /// </summary>
            NeedNewChildResolve
        }

        /// <summary>
        /// Implements a finalizer for a clock subtree.
        /// </summary>
        private class SubtreeFinalizer
        {
            /// <summary>
            /// Creates a finalizer for a clock subtree.
            /// </summary>
            internal SubtreeFinalizer(TimeManager timeManager)
            {
                _timeManager = timeManager;
            }

            /// <summary>
            /// Finalizes a clock subtree.
            /// </summary>
            ~SubtreeFinalizer()
            {
#pragma warning disable 1634, 1691
#pragma warning suppress 6525
                _timeManager.ScheduleClockCleanup();
            }

            private TimeManager _timeManager;
        }

        /// <summary>
        /// Holds sync-related data when it is applicable
        /// </summary>
        internal class SyncData
        {
            internal SyncData(Clock syncClock)
            {
                Debug.Assert(syncClock != null);
                Debug.Assert(syncClock.GetCanSlip());
                Debug.Assert(syncClock.IsRoot || syncClock._timeline.BeginTime.HasValue);  // Only roots may later validate their _beginTime

                _syncClock = syncClock;

                UpdateClockBeginTime(); // This will update the remaining dependencies
            }

            // This method should be invoked anytime we invalidate the SyncClock's _beginTime only
            internal void UpdateClockBeginTime()
            {
                // Do we really need to cache the beginTime and appledSpeedRatio here?
                _syncClockBeginTime = _syncClock._beginTime;
                _syncClockSpeedRatio = _syncClock._appliedSpeedRatio;
                _syncClockResolvedDuration = SyncClockResolvedDuration;  // This is to detect media finding its true duration
            }

            internal Clock SyncClock
            {
                get { return _syncClock; }
            }

            internal Duration SyncClockResolvedDuration
            {
                get
                {
                    // Duration can only change its value while it is Automatic
                    if (!_syncClockResolvedDuration.HasTimeSpan)
                    {
                        _syncClockEffectiveDuration = _syncClock.ComputeEffectiveDuration();  // null == infinity
                        _syncClockResolvedDuration = _syncClock._resolvedDuration;
                    }
                    return _syncClockResolvedDuration;
                }
            }

            internal bool SyncClockHasReachedEffectiveDuration
            {
                get
                {
                    if (_syncClockEffectiveDuration.HasValue)  // If the sync clock has a finite duration
                    {
                        return (_previousRepeatTime + _syncClock.GetCurrentTimeCore() >= _syncClockEffectiveDuration.Value);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // NOTE: This value is in SyncNode coordinates, not its parent's coordinates
            internal TimeSpan? SyncClockEffectiveDuration
            {
                get { return _syncClockEffectiveDuration; }
            }

            internal double SyncClockSpeedRatio
            {
                get { return _syncClockSpeedRatio; }
            }

            internal bool IsInSyncPeriod
            {
                get { return _isInSyncPeriod; }
                set { _isInSyncPeriod = value; }
            }

            internal bool SyncClockDiscontinuousEvent
            {
                get { return _syncClockDiscontinuousEvent; }
                set { _syncClockDiscontinuousEvent = value; }
            }

            internal TimeSpan PreviousSyncClockTime
            {
                get { return _previousSyncClockTime; }
                set { _previousSyncClockTime = value; }
            }

            internal TimeSpan PreviousRepeatTime
            {
                get { return _previousRepeatTime; }
                set { _previousRepeatTime = value; }
            }

            internal TimeSpan SyncClockBeginTime
            {
                get
                {
                    Debug.Assert(_syncClockBeginTime.HasValue);  // This should never be queried on a root without beginTime
                    return _syncClockBeginTime.Value;
                }
            }


            private Clock _syncClock;
            private double _syncClockSpeedRatio;
            private bool _isInSyncPeriod, _syncClockDiscontinuousEvent;

            private Duration _syncClockResolvedDuration = Duration.Automatic;  // Duration -- *local* coordinates
            private TimeSpan? _syncClockEffectiveDuration;  // This reflects RepeatBehavior (local coordinates)
            
            private TimeSpan? _syncClockBeginTime;
            private TimeSpan _previousSyncClockTime;
            private TimeSpan _previousRepeatTime;  // How many complete iterations we already have done
        }

        /// <summary>
        /// Holds data specific to root clocks.
        /// </summary>
        internal class RootData
        {
            internal RootData()
            {
            }

            internal TimeSpan CurrentAdjustedGlobalTime
            {
                get { return _currentAdjustedGlobalTime; }
                set { _currentAdjustedGlobalTime = value; }
            }

            internal Int32 DesiredFrameRate
            {
                get { return _desiredFrameRate; }
                set { _desiredFrameRate = value; }
            }

            internal Double InteractiveSpeedRatio
            {
                get { return _interactiveSpeedRatio; }
                set { _interactiveSpeedRatio = value; }
            }

            internal TimeSpan LastAdjustedGlobalTime
            {
                get { return _lastAdjustedGlobalTime; }
                set { _lastAdjustedGlobalTime = value; }
            }

            /// <summary>
            /// The time to which we want to seek this timeline at the next tick
            /// </summary>
            internal TimeSpan? PendingSeekDestination
            {
                get { return _pendingSeekDestination; }
                set { _pendingSeekDestination = value; }
            }

            internal Double? PendingSpeedRatio
            {
                get { return _pendingSpeedRatio; }
                set { _pendingSpeedRatio = value; }
            }

            private Int32 _desiredFrameRate;
            private double _interactiveSpeedRatio = 1.0;
            private double? _pendingSpeedRatio;
            private TimeSpan _currentAdjustedGlobalTime;
            private TimeSpan _lastAdjustedGlobalTime;
            private TimeSpan? _pendingSeekDestination;
        }


        #endregion // Nested Types


        //
        // Debugging Instrumentation
        //

        #region Debugging instrumentation

        /// <summary>
        /// Debug-only method to verify that the recomputed input time
        /// is close to the original.  See ComputeCurrentIteration
        /// </summary>
        /// <param name="inputTime">original input time</param>
        /// <param name="optimizedInputTime">input time without rounding errors</param>
        [Conditional("DEBUG")]
        private void Debug_VerifyOffsetFromBegin(long inputTime, long optimizedInputTime)
        {
            long error = (long)Math.Max(_appliedSpeedRatio, 1.0);

            // Assert the computed inputTime is very close to the original.
            // _appliedSpeedRatio is the upper bound of the error (in Ticks) caused by the
            // calculation of inputTime.  The reason is that we truncate (floor) during the 
            // computation of EffectiveDuration, losing up to 0.99999.... off the number.
            // The computation of inputTime multiplies the truncated value by _appliedSpeedRatio, 
            // so _appliedSpeedRatio is guaranteed to be close to but higher than the actual error.

            Debug.Assert(Math.Abs(optimizedInputTime - inputTime) <= error,
                "This optimized inputTime does not match the original - did the calculation of inputTime change?");
        }

#if DEBUG

        /// <summary>
        /// Dumps the description of the subtree rooted at this clock.
        /// </summary>
        internal void Dump()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Capacity = 1024;
            builder.Append("======================================================================\n");
            builder.Append("Clocks rooted at Clock ");
            builder.Append(_debugIdentity);
            builder.Append('\n');
            builder.Append("----------------------------------------------------------------------\n");
            builder.Append("Flags    LastBegin      LastEnd    NextBegin      NextEnd Name\n");
            builder.Append("----------------------------------------------------------------------\n");
            if (IsTimeManager)
            {
                RootBuildInfoRecursive(builder);
            }
            else
            {
                BuildInfoRecursive(builder, 0);
            }
            builder.Append("----------------------------------------------------------------------\n");
            Trace.Write(builder.ToString());
        }

        /// <summary>
        /// Dumps the description of all clocks in the known clock table.
        /// </summary>
        internal static void DumpAll()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int clockCount = 0;
            builder.Capacity = 1024;
            builder.Append("======================================================================\n");
            builder.Append("Clocks in the GC heap\n");
            builder.Append("----------------------------------------------------------------------\n");
            builder.Append("Flags    LastBegin      LastEnd    NextBegin      NextEnd Name\n");
            builder.Append("----------------------------------------------------------------------\n");

            lock (_debugLockObject)
            {
                if (_objectTable.Count > 0)
                {
                    // Output the clocks sorted by ID
                    int[] idTable = new int[_objectTable.Count];
                    _objectTable.Keys.CopyTo(idTable, 0);
                    Array.Sort(idTable);

                    for (int index = 0; index < idTable.Length; index++)
                    {
                        WeakReference weakRef = (WeakReference)_objectTable[idTable[index]];
                        Clock clock = (Clock)weakRef.Target;
                        if (clock != null)
                        {
                            clock.BuildInfo(builder, 0, true);
                            clockCount++;
                        }
                    }
                }
            }

            if (clockCount == 0)
            {
                builder.Append("There are no Clocks in the GC heap.\n");
            }

            builder.Append("----------------------------------------------------------------------\n");

            Trace.Write(builder.ToString());
        }

        /// <summary>
        /// Dumps the description of the subtree rooted at this clock.
        /// </summary>
        /// <param name="builder">
        /// A StringBuilder that accumulates the description text.
        /// </param>
        /// <param name="depth">
        /// The depth of recursion for this clock.
        /// </param>
        // Normally a virtual method would be implemented for the dervied class
        // to handle the children property, but the asmmeta would be different
        // since this is only availabe in a debug build. For this reason we're
        // handling the children in the base class.
        internal void BuildInfoRecursive(System.Text.StringBuilder builder, int depth)
        {
            // Add the info for this clock
            BuildInfo(builder, depth, false);

            // Recurse into the children
            ClockGroup thisGroup = this as ClockGroup;

            if (thisGroup != null)
            {
                depth++;
                List<Clock> children = thisGroup.InternalChildren;
                if (children != null)
                {
                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        children[childIndex].BuildInfoRecursive(builder, depth);
                    }
                }
            }
        }

        /// <summary>
        /// Dumps the description of the subtree rooted at this root clock.
        /// </summary>
        /// <param name="builder">
        /// A StringBuilder that accumulates the description text.
        /// </param>
        internal void RootBuildInfoRecursive(System.Text.StringBuilder builder)
        {
            // Add the info for this clock
            BuildInfo(builder, 0, false);

            // Recurse into the children. Don't use the enumerator because
            // that would remove dead references, which would be an undesirable
            // side-effect for a debug output method.
            List<WeakReference> children = ((ClockGroup) this).InternalRootChildren;

            for (int index = 0; index < children.Count; index++)
            {
                Clock child = (Clock)children[index].Target;

                if (child != null)
                {
                    child.BuildInfoRecursive(builder, 1);
                }
            }
        }

        /// <summary>
        /// Dumps the description of this clock.
        /// </summary>
        /// <param name="builder">
        /// A StringBuilder that accumulates the description text.
        /// </param>
        /// <param name="depth">
        /// The depth of recursion for this clock.
        /// </param>
        /// <param name="includeParentID">
        /// True to dump the ID of the parent clock, false otherwise.
        /// </param>
        internal void BuildInfo(System.Text.StringBuilder builder, int depth, bool includeParentID)
        {
            builder.Append(depth);
            builder.Append(GetType().Name);
            builder.Append(' ');
            builder.Append(_debugIdentity);
            builder.Append(' ');
            _timeline.BuildInfo(builder, 0, false);
        }

        /// <summary>
        /// Finds a previously registered object.
        /// </summary>
        /// <param name="id">
        /// The ID of the object to look for
        /// </param>
        /// <returns>
        /// The object if found, null otherwise.
        /// </returns>
        internal static Clock Find(int id)
        {
            Clock clock = null;

            lock (_debugLockObject)
            {
                object handleReference = _objectTable[id];
                if (handleReference != null)
                {
                    WeakReference weakRef = (WeakReference)handleReference;
                    clock = (Clock)weakRef.Target;
                    if (clock == null)
                    {
                        // Object has been destroyed, so remove the weakRef.
                        _objectTable.Remove(id);
                    }
                }
            }

            return clock;
        }

        /// <summary>
        /// Cleans up the known timeline clocks table by removing dead weak
        /// references.
        /// </summary>
        internal static void CleanKnownClocksTable()
        {
            lock (_debugLockObject)
            {
                Hashtable removeTable = new Hashtable();

                // Identify dead references
                foreach (DictionaryEntry e in _objectTable)
                {
                    WeakReference weakRef = (WeakReference) e.Value;
                    if (weakRef.Target == null)
                    {
                        removeTable[e.Key] = weakRef;
                    }
                }

                // Remove dead references
                foreach (DictionaryEntry e in removeTable)
                {
                    _objectTable.Remove(e.Key);
                }
            }
        }

#endif // DEBUG

        #endregion // Debugging instrumentation

        //
        // Data
        //

        #region Data

        private ClockFlags          _flags;

        private int?                _currentIteration;      // Precalculated current iteration
        private double?             _currentProgress;       // Precalculated current progress value
        private double?             _currentGlobalSpeed;    // Precalculated current global speed value
        private TimeSpan?           _currentTime;           // Precalculated current global time
        private ClockState          _currentClockState;     // Precalculated current clock state

        private RootData            _rootData = null;       // Keeps track of root-related data for DesiredFrameRate
        internal SyncData           _syncData = null;       // Keeps track of sync-related data for SlipBehavior


        // Stores the clock's begin time as an offset from the clock's
        // parent's begin time (i.e. a begin time of 2 sec means begin
        // 2 seconds afer the parent starts).  For non-root clocks this
        // is always the same as its timeline's begin time.
        // For root clocks, the begin time is adjusted in response to seeks, 
        // pauses, etc (see AdjustBeginTime())
        //
        // This must be null when the clock is stopped.
        internal TimeSpan?          _beginTime;

        // This is only used for repeating timelines which have CanSlip children/descendants;
        // In this case, we use this variable instead of _beginTime to compute the current state
        // of the clock (in conjunction with _currentIteration, so we know how many we have
        // already completed.)  This makes us agnostic to slip/growth in our past iterations.
        private  TimeSpan?          _currentIterationBeginTime;

        // How soon this Clock needs another tick
        internal TimeSpan?          _nextTickNeededTime = null;

        private WeakReference       _weakReference;          
        private SubtreeFinalizer    _subtreeFinalizer;
        private EventHandlersStore  _eventHandlersStore;

        /// <summary>
        /// Cache Duration for perf reasons and also to accommodate one time
        /// only resolution of natural duration.
        /// If Timeline.Duration is Duration.Automatic, we will at first treat
        /// this as Duration.Forever, but will periodically ask what the resolved
        /// duration is. Once we receive an answer, that will be the duration
        /// used from then on, and we won't ask again.
        /// Otherwise, this will be set to Timeline.Duration when the Clock is
        /// created and won't change.
        /// </summary>
        internal Duration _resolvedDuration;

        /// <summary>
        /// This is a cached estimated duration of the current iteration of this clock.
        /// For clocks which return false to CanGrow, this should always equal the
        /// _resolvedDuration.  However, for clocks with CanGrow, this may change from
        /// tick to tick.  Based on how far we are in the present iteration, and how
        /// on track our slipping children are, we make estimates of when we will finish
        /// the iteration, which are reflected by our estimated _currentDuration.
        /// </summary>
        internal Duration _currentDuration;

        /// <summary>
        /// For a root, this is the Timeline's speed ratio multiplied by the
        /// interactive speed ratio. It's updated whenever the interactive speed
        /// ratio is updated. If the interactive speed ratio is 0, we'll use the
        /// Timeline's speed ratio, that is, treat interactive speed ratio as 1.
        /// This is why this is "applied" speed ratio.
        /// 
        /// For a non-root, this is just a cache for the Timeline's speed ratio.
        /// </summary>
        private Double _appliedSpeedRatio;

        #region Linking data

        internal Timeline           _timeline;
        internal TimeManager        _timeManager;
        internal ClockGroup         _parent; // Parents can only be ClockGroups since
                                             // they're the only ones having children
        internal int                _childIndex;
        internal int                _depth;

        static Int64                 s_TimeSpanTicksPerSecond = TimeSpan.FromSeconds(1).Ticks;

        #endregion // Linking data

        #region Debug data

#if DEBUG

        internal int                _debugIdentity;

        internal static int         _nextIdentity;
        internal static Hashtable   _objectTable = new Hashtable();
        internal static object      _debugLockObject = new object();

#endif // DEBUG

        #endregion // Debug data

        #endregion // Data
    }
}
