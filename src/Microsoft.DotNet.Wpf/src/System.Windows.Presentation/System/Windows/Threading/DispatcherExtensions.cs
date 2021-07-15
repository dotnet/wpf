// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.ComponentModel;                 // EditorBrowsableAttribute, BrowsableAttribute

namespace System.Windows.Threading
{
    public static class DispatcherExtensions
    {
        /// <summary>
        ///     Executes the specified delegate asynchronously 
        ///     on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action)
        {
            return dispatcher.BeginInvoke(action);
        }
        
        /// <summary>
        ///     Executes the specified delegate asynchronously 
        ///     on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
        {
            return dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
        {
            dispatcher.Invoke(action, priority);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="timeout">
        ///     The maximum amount of time to wait for the operation to complete.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, TimeSpan timeout)
        {
            dispatcher.Invoke(action, timeout);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="timeout">
        ///     The maximum amount of time to wait for the operation to complete.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, TimeSpan timeout, DispatcherPriority priority)
        {
            dispatcher.Invoke(action, timeout, priority);
        }
    }
}

