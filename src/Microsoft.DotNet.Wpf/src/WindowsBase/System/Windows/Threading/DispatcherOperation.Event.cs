// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
public abstract partial class DispatcherOperation
{
    private protected sealed class DispatcherOperationEvent
    {
        private readonly DispatcherOperation _operation;
        private readonly ManualResetEvent _event;
        private readonly TimeSpan _timeout;
        private bool _eventClosed;

        private Lock DispatcherLock
        {
            get => _operation.DispatcherLock;
        }

        public DispatcherOperationEvent(DispatcherOperation op, TimeSpan timeout)
        {
            _operation = op;
            _timeout = timeout;
            _event = new ManualResetEvent(false);
            _eventClosed = false;

            lock (DispatcherLock)
            {
                // We will set our event once the operation is completed or aborted.
                _operation.Aborted += new EventHandler(OnCompletedOrAborted);
                _operation.Completed += new EventHandler(OnCompletedOrAborted);

                // Since some other thread is dispatching this operation, it could
                // have been dispatched while we were setting up the handlers.
                // We check the state again and set the event ourselves if this
                // happened.
                if (_operation._status is not DispatcherOperationStatus.Pending and not DispatcherOperationStatus.Executing)
                {
                    _event.Set();
                }
            }
        }

        private void OnCompletedOrAborted(object sender, EventArgs e)
        {
            lock (DispatcherLock)
            {
                if (!_eventClosed)
                {
                    _event.Set();
                }
            }
        }

        public void WaitOne()
        {
            _event.WaitOne(_timeout, false);

            lock (DispatcherLock)
            {
                if (!_eventClosed)
                {
                    // Cleanup the events.
                    _operation.Aborted -= new EventHandler(OnCompletedOrAborted);
                    _operation.Completed -= new EventHandler(OnCompletedOrAborted);

                    // Close the event immediately instead of waiting for a GC
                    // because the Dispatcher is a a high-activity component and
                    // we could run out of events.
                    _event.Close();

                    _eventClosed = true;
                }
            }
        }
    }
}
