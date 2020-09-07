// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MS.Internal.WindowsBase;

namespace System.Windows.Threading
{
    /// <summary>
    ///     A simple awaiter type that will queue the continuation to a
    ///     dispatcher at a specific priority.
    /// </summary>
    /// <remarks>
    ///     This is returned from DispatcherPriorityAwaitable.GetAwaiter()
    /// </remarks>
    public struct DispatcherPriorityAwaiter : INotifyCompletion
    {
        /// <summary>
        ///     Creates an instance of DispatcherPriorityAwaiter that will
        ///     queue any continuations to the specified Dispatcher at the
        ///     specified priority.
        /// </summary>
        internal DispatcherPriorityAwaiter(Dispatcher dispatcher, DispatcherPriority priority)
        {
            _dispatcher = dispatcher;
            _priority = priority;
        }

        /// <summary>
        ///     This awaiter is just a proxy for queuing the continuations, it
        ///     never completes itself.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///     This awaiter is just a proxy for queuing the continuations, it
        ///     never completes itself, so it doesn't have any result.
        /// </summary>
        public void GetResult()
        {
        }

        /// <summary>
        ///     This is called with the continuation, which is simply queued to
        ///     the Dispatcher at the priority specified to the constructor.
        /// </summary>
        public void OnCompleted(Action continuation)
        {
            if(_dispatcher == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.DispatcherPriorityAwaiterInvalid));
            }
            
            _dispatcher.InvokeAsync(continuation, _priority);
        }

        private readonly Dispatcher _dispatcher;
        private readonly DispatcherPriority _priority;
}
}

