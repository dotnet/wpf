// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

#if DEBUG
#define TRACE
#endif // DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using MS.Internal;
using MS.Win32;
using MS.Utility;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Runtime.InteropServices;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// The object that controls an entire timing tree.
    /// </summary>
    /// <remarks>
    /// <p>A time manager controls the flow of time in a timing tree. The clock is
    /// updated periodically by the rendering system, at which time the progress value
    /// of all active timelines is updated according to the elapsed time. This elapsed time
    /// can be controlled by the application by specifying a custom reference clock in
    /// the constructor.</p>
    /// </remarks>
    internal sealed class TimeManager : DispatcherObject
    {
        #region External interface

        #region Construction

        /// <summary>
        /// Creates a time manager object in the stopped state.
        /// </summary>
        /// <remarks>
        /// This constructor causes the time manager to use a default system clock to drive
        /// the timing engine. To start the clock moving, call the
        /// <see cref="TimeManager.Start"/> method.
        /// </remarks>
        public TimeManager()
            : this(null)
        {
        }

        /// <summary>
        /// Creates a time manager object in the stopped state.
        /// </summary>
        /// <param name="clock">
        /// An interface to an object that provides real-time clock values to the time
        /// manager. The manager will query this interface whenever it needs to know the
        /// current time. This parameter may be null.
        /// </param>
        /// <remarks>
        /// If the clock parameter is null, the time manager uses a default system clock
        /// to drive the timing engine. To start the clock moving, call the
        /// <see cref="TimeManager.Start"/> method.
        /// </remarks>
        public TimeManager(IClock clock)
        {
            _eventQueue = new Queue<WeakReference>();
            Clock = clock;
            _timeState = TimeState.Stopped;
            _lastTimeState = TimeState.Stopped;
            _globalTime = new TimeSpan(-1);
            _lastTickTime = new TimeSpan(-1);
            _nextTickTimeQueried = false;
            _isInTick = false;
            ParallelTimeline timeManagerTimeline = new ParallelTimeline(new TimeSpan(0), Duration.Forever);
            timeManagerTimeline.Freeze();
            _timeManagerClock = new ClockGroup(timeManagerTimeline);
            _timeManagerClock.MakeRoot(this);
        }

        #endregion // Construction

        #region Properties

        /// <summary>
        /// Accesses the reference clock used by this time manager to obtain
        /// real-world clock values.
        /// </summary>
        public IClock Clock
        {
            get
            {
//                 VerifyAccess();
                return _systemClock;
            }
            set
            {
//                 VerifyAccess();
                if (value != null)
                {
                    _systemClock = value;
                }
                else
                {
                    if (MediaContext.IsClockSupported)
                    {
                        _systemClock = (IClock)MediaContext.From(Dispatcher);
                    }
                    else
                    {
                        _systemClock = (IClock)new GTCClock();
                    }
                }
            }
        }

        /// <summary>
        /// The current position of the clock, relative to the starting time.
        /// </summary>
        public Nullable<TimeSpan> CurrentTime
        {
            get
            {
//                 VerifyAccess();

                if (_timeState == TimeState.Stopped)
                {
                    return null;
                }
                else
                {
                    return _globalTime;
                }
            }
        }

        /// <summary>
        /// True if the structure of the timing tree has changed since the last
        /// tick.
        /// </summary>
        public bool IsDirty
        {
            get
            {
//                 VerifyAccess();

                return _isDirty;
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Pauses the clock.
        /// </summary>
        /// <remarks>
        /// To start the clock again, call the <see cref="Resume"/> method. This method has
        /// no effect if the clock is not in the running state.
        /// </remarks>
        public void Pause()
        {
//             VerifyAccess();
            if (_timeState == TimeState.Running)
            {
                // Record the pause time
                _pauseTime = _systemClock.CurrentTime;

                // Stop the tree from responding to ticks
                _timeState = TimeState.Paused;
            }
        }

        /// <summary>
        /// Resets the time manager and sets a new start time.
        /// </summary>
        /// <remarks>
        /// Calling this method is equivalent to calling <see cref="Stop"/> followed
        /// by <see cref="Start"/>.
        /// </remarks>
        /// <seealso cref="TimeManager.Start"/>
        /// <seealso cref="TimeManager.Stop"/>
        public void Restart()
        {
//             VerifyAccess();
            // Remember the current state
            TimeState oldState = _timeState;

            // Stop and start
            Stop();
            Start();

            // Go back to the old state
            _timeState = oldState;

            // If we were paused, update the pause position to the start position
            if (_timeState == TimeState.Paused)
            {
                _pauseTime = _startTime;
            }
        }

        /// <summary>
        /// Resumes the clock.
        /// </summary>
        /// <remarks>
        /// This function has no effect if the clock was not in the paused state. If the
        /// clock is stopped rather than paused, call the <see cref="Start"/> method
        /// instead.
        /// </remarks>
        public void Resume()
        {
//             VerifyAccess();
            if (_timeState == TimeState.Paused)
            {
                // Adjust the starting time
                _startTime = _startTime + _systemClock.CurrentTime - _pauseTime;

                // Restart the tree
                _timeState = TimeState.Running;

                // See if we need to tick sooner now that we are running
                if (GetNextTickNeeded() >= TimeSpan.Zero)
                {
                    NotifyNewEarliestFutureActivity();
                }
            }
        }

        /// <summary>
        /// Seeks the global clock to a new position.
        /// </summary>
        /// <param name="offset">
        /// The seek offset, in milliseconds. The meaning of this parameter depends on the value of
        /// the origin parameter.
        /// </param>
        /// <param name="origin">
        /// The meaning of the offset parameter. See the <see cref="TimeSeekOrigin"/> enumeration
        /// for possible values.
        /// </param>
        /// <remarks>
        /// <p>This method seeks the global clock. The timelines of all timelines in the timing tree are
        /// also updated accordingly. Any events that those timelines would have fired in between the old
        /// and new clock positions are skipped.</p>
        ///
        /// <p>Because the global clock is infinite there is no defined end point, therefore seeking
        /// from the end position has no effect.</p>
        /// </remarks>
        public void Seek(int offset, TimeSeekOrigin origin)
        {
            if (_timeState >= TimeState.Paused)
            {
                switch (origin)
                {
                    case TimeSeekOrigin.BeginTime:
                        // Use the offset as the new time;
                        break;

                    case TimeSeekOrigin.Duration:
                        // Undefined for the global clock
                        return;

                    default:
                        // Invalid enum argument
                        throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, "TimeSeekOrigin"));
                }

                // Truncate to the beginning
                if (offset < 0)
                {
                    offset = 0;
                }

                TimeSpan offsetTime = TimeSpan.FromMilliseconds((double)offset);

                // Only do the work if we actually moved
                if (offsetTime != _globalTime)
                {
                    // Set the new global time
                    _globalTime = offsetTime;

                    // Adjust the starting time for future ticks
                    _startTime =_systemClock.CurrentTime - _globalTime;

                    // Update the timing tree accordingly
                    _timeManagerClock.ComputeTreeState();
                }
            }
        }

        /// <summary>
        /// Starts the time manager at the current time.
        /// </summary>
        /// <remarks>
        /// The current time is determined by querying the reference clock for this
        /// time manager.
        /// </remarks>
        public void Start()
        {
//             VerifyAccess();
            if (_timeState == TimeState.Stopped)
            {
                // Reset the state of the timing tree
                _lastTickTime = TimeSpan.Zero;

                // Start the tree
                _startTime =_systemClock.CurrentTime;
                _globalTime = TimeSpan.Zero;
                _timeState = TimeState.Running;
                _timeManagerClock.RootActivate();
            }
        }

        /// <summary>
        /// Stops the clock.
        /// </summary>
        /// <remarks>
        /// After the clock is stopped, it can be restarted with a call to <see cref="Start"/>.
        /// This method has no effect if the clock was not in the running or paused state.
        /// </remarks>
        /// <seealso cref="TimeManager.Start"/>
        /// <seealso cref="TimeManager.Stop"/>
        public void Stop()
        {
//             VerifyAccess();
            if (_timeState >= TimeState.Paused)
            {
                _timeManagerClock.RootDisable();
                _timeState = TimeState.Stopped;
            }
        }

        /// <summary>
        /// Moves the clock forward to the current time and updates the state of
        /// all timing objects based on the time change.
        /// </summary>
        /// <remarks>
        /// The associated reference clock is used to determine the current time.
        /// The new position of the clock will be equal to the difference between the
        /// starting system time and the current system time. The time manager requires
        /// the system time to move forward.
        /// </remarks>
        public void Tick()
        {
            try
            {
#if DEBUG
                // On a regular interval, clean up our tables of known
                // timelines and clocks
                if (++_frameCount >= 1000) // Should be about once every 10s
                {
                    Timeline.CleanKnownTimelinesTable();
                    System.Windows.Media.Animation.Clock.CleanKnownClocksTable();
                    _frameCount = 0;
                }
#endif // DEBUG
                // Don't need to worry about needing a tick sooner
                _nextTickTimeQueried = false;

                // Mark the tree as clean immediately. If any changes occur during
                // processing of the tick, the tree will be marked as dirty again.
                _isDirty = false;

                // No effect unless we are in the running state
                if (_timeState == TimeState.Running)
                {
                    // Figure out the current time
                    _globalTime = GetCurrentGlobalTime();

                    // Start the tick
                    _isInTick = true;
                }

                // Trace the start of the tick and pass along the absolute time to which
                // we are ticking.
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnimation | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientTimeManagerTickBegin, (_startTime + _globalTime).Ticks / TimeSpan.TicksPerMillisecond);

                // Run new property querying logic on the timing tree
                if (_lastTimeState == TimeState.Stopped && _timeState == TimeState.Stopped)  // We were stopped the whole time
                {
                    _currentTickInterval = TimeIntervalCollection.CreateNullPoint();
                }
                else  // We were not stopped at some time, so process the tick interval
                {
                    _currentTickInterval = TimeIntervalCollection.CreateOpenClosedInterval(_lastTickTime, _globalTime);

                    // If at either tick we were stopped, add the null point to represent that
                    if (_lastTimeState == TimeState.Stopped || _timeState == TimeState.Stopped)
                    {
                        _currentTickInterval.AddNullPoint();
                    }
                }

                // Compute the tree state, using _currentTickInterval to compute the events that occured
                _timeManagerClock.ComputeTreeState();

                // Cache TimeManager state at this time
                _lastTimeState = _timeState;

                // When the tick is done, we raise timing events
                RaiseEnqueuedEvents();
            }
            finally
            {
                _isInTick = false;

                // Cache the tick time
                _lastTickTime = _globalTime;

                //trace the end of the tick
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnimation | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientTimeManagerTickEnd);
            }

            // At the end of every tick clean up GC'ed clocks, if necessary
            CleanupClocks();
        }

        // Get the maximumum desired framerate that has been set in one of our
        // top-level clock.
        internal int GetMaxDesiredFrameRate()
        {
            return _timeManagerClock.GetMaxDesiredFrameRate();
        }

        #endregion // Methods

        #endregion // External interface

        #region Internal implementation

        #region Types

        /// <summary>
        /// A clock implementation that uses GetTickCount to get real-world clock values.
        /// </summary>
        internal class GTCClock : IClock
        {
            #region Internal implementation

            #region Methods
            /// <summary>
            /// Creates a GTCClock object.
            /// </summary>
            internal GTCClock()
            {
            }
            /// <summary>
            /// Gets the current real-world time.
            /// </summary>
            TimeSpan IClock.CurrentTime
            {
                get
                {
                    return TimeSpan.FromTicks(DateTime.Now.Ticks);
                }
            }
            #endregion // Methods

            #endregion // Internal implementation
        }


        /// <summary>
        /// This is used by DrtTiming in order to specify the CurrentTime
        /// as it pleases.  Note this class is internal
        /// </summary>
        internal class TestTimingClock : IClock
        {
            #region Internal implementation

            #region Methods

            /// <summary>
            /// Gets and sets the CurrentTime
            /// </summary>
            public TimeSpan CurrentTime
            {
                get
                {
                    return _currentTime;
                }

                set
                {
                    _currentTime = value;
                }
            }

            #endregion // Methods
            #region Data
            private TimeSpan _currentTime;
            #endregion // Data

            #endregion // Internal implementation
        }

        #endregion // Types

        #region Methods

        /// <summary>
        /// Removes references to garbage-collected clocks.
        /// </summary>
        private void CleanupClocks()
        {
            // We only run the clean-up procedure if we previously detected
            // that some clocks were GC'ed. The detection occurs in the
            // finalizer thread, perhaps concurrently with the execution of
            // this method. See the comments in the ScheduleClockCleanup
            // method for why we don't need to lock to protect against that
            // concurrency.

            if (_needClockCleanup)
            {
                // We are about to clean up, so clear the bit first. That way
                // if another thread sets the bit again then at worst we
                // have to run through the process one more time, with no ill
                // consequences other than a small run-time hit. If we wait to
                // set the bit until after we actually cleaned up we may fail
                // to detect any clocks that are GC'ed while we are cleaning
                // up.
                _needClockCleanup = false;
                _timeManagerClock.RootCleanChildren();
            }
        }

        /// <summary>
        /// Puts a Clock into a queue for firing events.
        /// </summary>
        /// <param name="sender">
        /// The object sending the event.
        /// </param>
        /// <remarks>
        /// The time at which the event is actually raised is implementation-specific and is
        /// subject to change at any time.
        /// </remarks>
        internal void AddToEventQueue(Clock sender)
        {
            _eventQueue.Enqueue(sender.WeakReference);
        }

        /// <summary>
        /// Converts the current real world time into clock time.
        /// </summary>
        /// <returns>
        /// The global clock time.
        /// </returns>
        /// <remarks>
        /// This function uses the custom clock provided in the TimeManager constructor, if any.
        /// </remarks>
        internal TimeSpan GetCurrentGlobalTime()
        {
            // The conversion depends on our current state
            switch (_timeState)
            {
                case TimeState.Stopped:

                    // If we are stopped, the current global time is always zero
                    return TimeSpan.Zero;

                case TimeState.Paused:

                    // If we are paused, the real-world clock appears to us as if it's
                    // stopped at the pause time
                    return _pauseTime - _startTime;

                case TimeState.Running:

                    // If we are running, use the actual real-world clock.
                    // However, keep time "frozen" while processing a tick or
                    // by request.
                    if (_isInTick || _lockTickTime)
                    {
                        return _globalTime;
                    }
                    else
                    {
                        return _systemClock.CurrentTime - _startTime;
                    }

                default:

                    Debug.Assert(false, "Unrecognized TimeState enumeration value");
                    return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Locks the current time for successive Tick calls.
        /// </summary>
        internal void LockTickTime()
        {
            _lockTickTime = true;
        }

        /// <summary>
        /// Called by timelines to tell the time manager that a future activity
        /// was scheduled at the head of the list.
        /// </summary>
        /// <remarks>
        /// When a new future activity is added to the head, it means that the next
        /// tick is needed earlier than was thought before the activity was scheduled.
        /// This means that if the NextTickNeeded property was queried we need to
        /// notify the querying module that the value of the property needs to be
        /// queried again.
        /// </remarks>
        internal void NotifyNewEarliestFutureActivity()
        {
            if (_nextTickTimeQueried && _userNeedTickSooner != null)
            {
                _nextTickTimeQueried = false;
                _userNeedTickSooner(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises any events stored in the queue.
        /// </summary>
        private void RaiseEnqueuedEvents()
        {
            while (_eventQueue.Count > 0)
            {
                WeakReference instance = _eventQueue.Dequeue();
                Clock clock = (Clock)instance.Target;

                if (clock != null)
                {
                    clock.RaiseAccumulatedEvents();
                }
            }
        }

        /// <summary>
        /// Schedules a clean-up pass to look for GC'ed clocks.
        /// </summary>
        /// <remarks>
        /// This method can be called from any thread. In particular, it is
        /// safe to call this method from another object's finalizer.
        /// </remarks>
        internal void ScheduleClockCleanup()
        {
            // To clean up GC'ed clocks we set a bit when a finalizer runs (in
            // the finalizer thread). Later, in the UI thraed, we run the
            // CleanupClocks method which checks whether this bit is set and
            // executes the clean-up procedure if it is. This can be done
            // entirely without locks, even on multiprocessor machines,
            // because of the sequence in which the variable is accessed. In
            // particular, note that this method sets the bit unconditionally,
            // which means we can essentially treat it as atomic.
            //
            // Consider the possible sequences:
            //
            //  1.  The bit is already set when this method runs
            //      In that case we are definitely in a safe situation because
            //      we won't change the value of the bit, so this call is a
            //      no-op.
            //
            //  2.  The bit is not set when this method runs
            //      In that case we need to consider what the UI thread may do.
            //      Other threads running this method are irrelevant because
            //      the method is essentially atomic, and thanks to UI context
            //      affinity there can only be one UI thread running. If that
            //      thread is running CleanupClocks at the same time that this
            //      thread is running this method then there are three cases:
            //      a)  We set the bit before CleanupClocks checks the bit
            //          This is obviously safe -- the other method will run the
            //          clean-up procedure immediately.
            //      b)  We set the bit after CleanupClocks checks the bit but
            //          before it clears it. This is actually case 1, because
            //          CleanupClocks only does anything if the bit was already
            //          set. Case 1 is a no-op, so we are safe.
            //      c)  We set the bit after CleanupClocks checks and clears
            //          the bit. This may happen before, during or after the
            //          clean-up procedure, but all cases are safe because this
            //          bit won't be checked again by this invocation of
            //          CleanupClocks. All that will happen is that we will
            //          need to run CleanupClocks again at some point in the
            //          future. In the worst case this is nothing more than a
            //          small waste of time because the clock whose
            //          finalization we are detecting may be cleaned up already
            //          by the current invocation of CleanupClocks. However,
            //          the operation is still safe.
            //
            // We are trading off the cost of locking every time against the
            // cost of an occasional unnecessary clean-up procedure. The latter
            // seems a lot less likely, and therefore smaller in the long run.

            _needClockCleanup = true;
        }

        /// <summary>
        /// Marks the timing tree as dirty. The tree is "cleaned" on the following
        /// tick.
        /// </summary>
        internal void SetDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Unlocks the current time such that the next tick will query the
        /// system time.
        /// </summary>
        internal void UnlockTickTime()
        {
            _lockTickTime = false;
        }

        #endregion // Methods

        #region Properties

        /// <summary>
        /// The current global time.
        /// </summary>
        internal TimeSpan InternalCurrentGlobalTime
        {
            get
            {
                return _globalTime;
            }
        }

        /// <summary>
        /// Whether the TimeManager is stopped
        /// </summary>
        internal bool InternalIsStopped
        {
            get
            {
                return (_timeState == TimeState.Stopped);
            }
        }

        internal TimeIntervalCollection InternalCurrentIntervals
        {
            get
            {
                return _currentTickInterval;
            }
            set
            {
                _currentTickInterval = value;  // This allows temporarily setting custom interval values
        }
        }

        /// <summary>
        /// The time until the state of any time clock in the tree is expected
        /// to change. If the method returns a negative value, no state changes
        /// are expected any time in the future and therefore a tick will never
        /// be needed.
        /// </summary>
        /// <remarks>
        /// This method is only a hint. Even if timelines don't change state,
        /// their progress values need to be updated regularly, requiring a tick at
        /// some minimum interval. In addition, certain interactive events can
        /// cause activity to be scheduled earlier than the previously known
        /// earliest activity, in which case a tick is needed sooner. To detect
        /// this condition, hook the <see cref="NeedTickSooner"/> event.
        /// </remarks>
        internal TimeSpan GetNextTickNeeded()
        {
//             VerifyAccess();
            _nextTickTimeQueried = true;

            if (_timeState == TimeState.Running)
            {
                // We are running, see how soon the tree needs updating
                TimeSpan? nextTickNeededTime = _timeManagerClock.InternalNextTickNeededTime;

                if (nextTickNeededTime.HasValue)
                {
                    // Get the time the time manager has been running as of this moment.
                    TimeSpan timeManagerCurrentTime = _systemClock.CurrentTime - _startTime;

                    // Calculate how long from "now" we'll need another tick.
                    TimeSpan nextTickNeededTimeFromCurrentTime = nextTickNeededTime.Value - timeManagerCurrentTime;

                    if (nextTickNeededTimeFromCurrentTime <= TimeSpan.Zero)
                    {
                        // We've already past the time at which we need another
                        // tick so tick again ASAP.
                        return TimeSpan.Zero;
                    }
                    else
                    {
                        // Return the time until we need another tick
                        return nextTickNeededTimeFromCurrentTime;
                    }
                }
                else  // The tree does not need any ticks in the future
                {
                    return TimeSpan.FromTicks(-1);
                }
            }
            else
            {
                // Our state is not Running, so we don't have any ticks coming up
                return TimeSpan.FromTicks(-1);
            }
        }

        /// <summary>
        /// Unchecked internal access to the time shift since the last tick.
        /// </summary>
        internal TimeSpan LastTickDelta
        {
            get
            {
                return _globalTime - _lastTickTime;
            }
        }

        /// <summary>
        /// Internal access to the time since the last tick.
        /// </summary>
        internal TimeSpan LastTickTime
        {
            get
            {
                return _lastTickTime;
            }
        }

        /// <summary>
        /// Returns the clock that is the root of the timing tree managed by
        /// this time manager.
        /// </summary>
        internal ClockGroup TimeManagerClock
        {
            get
            {
//                 VerifyAccess();
                return _timeManagerClock;
            }
        }

        /// <summary>
        /// The present state of the time manager.
        /// </summary>
        internal TimeState State
        {
            get
            {
                return _timeState;
            }
        }

        #endregion // Properties

        #region Events

        /// <summary>
        /// Raised when a change has been made to the timing engine that may require
        /// it to be ticked sooner than was previously expected.
        /// </summary>
        internal event EventHandler NeedTickSooner
        {
            add
            {
//                 VerifyAccess();
                _userNeedTickSooner += value;
            }
            remove
            {
//                 VerifyAccess();
                _userNeedTickSooner -= value;
            }
        }

        #endregion // Events

        #region Debugging instrumentation

#if DEBUG

        /// <summary>
        /// Dumps the internal state of the whole timing tree
        /// </summary>
        internal void Dump()
        {
            _timeManagerClock.Dump();
        }

#endif // DEBUG

        #endregion // Debugging instrumentation

        #region Data

        #region Timing data

        private TimeState                   _timeState;
        private TimeState                   _lastTimeState;

        private IClock                      _systemClock;

        private TimeSpan                    _globalTime;
        private TimeSpan                    _startTime;
        private TimeSpan                    _lastTickTime;
        private TimeSpan                    _pauseTime;

        private TimeIntervalCollection      _currentTickInterval;
        private bool                        _nextTickTimeQueried,
                                            _isDirty,
                                            _isInTick,
                                            _lockTickTime;
        private EventHandler                _userNeedTickSooner;
        private ClockGroup                  _timeManagerClock;
        private Queue<WeakReference>        _eventQueue;

        #endregion // Timing data

        #region Linking data

        private bool                        _needClockCleanup;

        #endregion // Linking data

        #region Debug data

#if DEBUG

        private int                         _frameCount;

#endif // DEBUG

        #endregion // Debug data

        #endregion // Data

        #endregion // Internal implementation
    }
}
