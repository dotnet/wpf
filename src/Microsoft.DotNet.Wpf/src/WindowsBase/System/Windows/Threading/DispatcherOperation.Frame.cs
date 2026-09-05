// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
public abstract partial class DispatcherOperation
{
    private protected sealed class DispatcherOperationFrame : DispatcherFrame
    {
        private readonly DispatcherOperation _operation;
        private readonly Timer _waitTimer;

        // Note: we pass "exitWhenRequested=false" to the base
        // DispatcherFrame constructor because we do not want to exit
        // this frame if the dispatcher is shutting down. This is
        // because we may need to invoke operations during the shutdown process.
        public DispatcherOperationFrame(DispatcherOperation op, TimeSpan timeout) : base(false)
        {
            _operation = op;

            // We will exit this frame once the operation is completed or aborted.
            _operation.Aborted += new EventHandler(OnCompletedOrAborted);
            _operation.Completed += new EventHandler(OnCompletedOrAborted);

            // We will exit the frame if the operation is not completed within
            // the requested timeout.
            if (timeout.TotalMilliseconds > 0)
            {
                _waitTimer = new Timer(new TimerCallback(OnTimeout),
                                       null,
                                       timeout,
                                       TimeSpan.FromMilliseconds(-1));
            }

            // Some other thread could have aborted the operation while we were
            // setting up the handlers.  We check the state again and mark the
            // frame as "should not continue" if this happened.
            if (_operation._status != DispatcherOperationStatus.Pending)
            {
                Exit();
            }
        }

        private void OnCompletedOrAborted(object sender, EventArgs e)
        {
            Exit();
        }

        private void OnTimeout(object arg)
        {
            Exit();
        }

        private void Exit()
        {
            Continue = false;

            _waitTimer?.Dispose();

            _operation.Aborted -= new EventHandler(OnCompletedOrAborted);
            _operation.Completed -= new EventHandler(OnCompletedOrAborted);
        }
    }
}
