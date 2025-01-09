// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper for classes that need to clean up data structures
//              containing WeakReferences

using System.Threading;
using System.Windows.Threading;

namespace MS.Internal
{
    internal class CleanupHelper : DispatcherObject
    {
        internal CleanupHelper(Func<bool,bool> callback,    // cleanup method
                               int pollingInterval=400,     // initial polling interval
                               int promotionInterval=10000, // promote to higher priority
                               int maxInterval=5000)        // max polling interval
        {
            _cleanupCallback = callback;
            _basePollingInterval = TimeSpan.FromMilliseconds(pollingInterval);
            _maxPollingInterval = TimeSpan.FromMilliseconds(maxInterval);

            _cleanupTimerPriority = DispatcherPriority.ContextIdle;
            _defaultCleanupTimer = new DispatcherTimer(_cleanupTimerPriority)
            {
                Interval = _basePollingInterval
            };
            _defaultCleanupTimer.Tick += OnCleanupTick;

            _starvationTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(promotionInterval)
            };
            _starvationTimer.Tick += OnStarvationTick;

            _cleanupTimer = _defaultCleanupTimer;
        }

        internal void ScheduleCleanup()
        {
            // only the first request after a previous cleanup should schedule real work
            if (Interlocked.Increment(ref _cleanupRequests) == 1)
            {
                _cleanupTimer = _defaultCleanupTimer;
                _cleanupTimerPriority = DispatcherPriority.ContextIdle;
                _waitingForGC = true;
                _cleanupTimer.Start();
                _starvationTimer.Start();

                new GCDetector(this);       // wait for GC to occur
            }
        }

        internal bool DoCleanup(bool forceCleanup=false)
        {
            _cleanupTimer.Stop();
            _starvationTimer.Stop();

            Interlocked.Exchange(ref _cleanupRequests, 0);

            bool foundDirt = _cleanupCallback(forceCleanup);

            if (foundDirt)
            {
                // if cleanup found dirt, make the next cleanup more aggressive
                // (heuristic: the app is actually doing things that require cleanup)
                _defaultCleanupTimer.Interval = _basePollingInterval;
            }
            else
            {
                // if not, make the next cleanup less agressive
                // (heuristic: the app is not releasing anything to cleanup)
                if (_cleanupTimer.Interval < _maxPollingInterval)
                {
                    _cleanupTimer.Interval += _basePollingInterval;
                }
                _defaultCleanupTimer.Interval = _cleanupTimer.Interval;
            }

            return foundDirt;
        }

        private void OnCleanupTick(object sender, EventArgs e)
        {
            if (!_waitingForGC)
            {
                DoCleanup();
            }
        }

        private void OnStarvationTick(object sender, EventArgs e)
        {
            // cleanup is starving at its current priority
            // so increase the priority
            if (_cleanupTimerPriority < DispatcherPriority.Render)
            {
                _cleanupTimer.Stop();
                _cleanupTimer = new DispatcherTimer(
                    _cleanupTimer.Interval,
                    ++_cleanupTimerPriority,
                    OnCleanupTick,
                    _cleanupTimer.Dispatcher);

                // when promoting, stop waiting for GC.
                // If the app is idle - no activity, no GC - we don't want the
                // timers to tick forever, as that drains battery
                _waitingForGC = false;
            }
            else
            {
                // if we get here, the app is starving high-priority tasks, and
                // probably is unresponsive.   We can't fix the app's bug, but
                // for politeness we turn off the starvation timer
                _starvationTimer.Stop();
            }
        }

        private DispatcherTimer _cleanupTimer;
        private DispatcherTimer _starvationTimer;
        private DispatcherTimer _defaultCleanupTimer;
        private DispatcherPriority _cleanupTimerPriority;

        private int             _cleanupRequests;
        private bool            _waitingForGC;

        private Func<bool,bool> _cleanupCallback;
        private TimeSpan        _basePollingInterval;
        private TimeSpan        _maxPollingInterval;

        // When an instance of this class is GC'd and finalized, it
        // tells the CleanupHelper that a GC has occurred.
        private class GCDetector
        {
            internal GCDetector(CleanupHelper parent)
            {
                _parent = parent;
            }

            ~GCDetector()
            {
                _parent._waitingForGC = false;
            }

            private CleanupHelper _parent;
        }
    }
}

