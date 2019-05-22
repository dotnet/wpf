// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MS.Internal.WindowsBase;

namespace System.Windows.Threading
{
    /// <summary>
    ///     A simple awaitable type that will return a DispatcherPriorityAwaiter.
    /// </summary>
    /// <remarks>
    ///     This is returned from Dispatcher.Yield()
    /// </remarks>
    public struct DispatcherPriorityAwaitable
    {
        /// <summary>
        ///     Creates an instance of DispatcherPriorityAwaitable with the
        ///     parameters used to configure the DispatcherPriorityAwaiter.
        /// </summary>
        internal DispatcherPriorityAwaitable(Dispatcher dispatcher, DispatcherPriority priority)
        {
            _dispatcher = dispatcher;
            _priority = priority;
        }

        /// <summary>
        ///     Returns a new instance of a DispatcherPriorityAwaiter,
        ///     configured with the same parameters as this instance.
        /// </summary>
        public DispatcherPriorityAwaiter GetAwaiter()
        {
            return new DispatcherPriorityAwaiter(_dispatcher, _priority);
        }

        private readonly Dispatcher _dispatcher;
        private readonly DispatcherPriority _priority;
    }
}
