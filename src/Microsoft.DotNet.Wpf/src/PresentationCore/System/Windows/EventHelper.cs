// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Windows
{
    /// <summary>Provides helper methods for working with events and delegates.</summary>
    internal static class EventHelper
    {
        /// <summary>Combines a delegate into an existing delegate.</summary>
        /// <param name="field">A tuple of a delegate and its invocation list already in array form.</param>
        /// <param name="value">The delegate to add.</param>
        /// <remarks>
        /// This routine enables code to store a tuple of a delegate and its invocation list.  Adding
        /// behaves exactly like combining with the delegate, but the invocation list is also updated.
        /// Thread safety is maintained just as with the code generated by the C# compiler when adding
        /// to an event.  Storing such a tuple is helpful when the registered delegates are changed
        /// relatively rarely while invocation via the invocation list happens much more frequently,
        /// avoiding the need to call GetInvocationList every time invocation needs to happen, as the
        /// array has already been precomputed.
        /// </remarks>
        public static void AddHandler<T>(ref Tuple<T, Delegate[]> field, T value) where T : Delegate
        {
            while (true)
            {
                Tuple<T, Delegate[]> oldTuple = field;
                T combinedDelegate = (T)Delegate.Combine(oldTuple?.Item1, value);
                Tuple<T, Delegate[]> newTuple = combinedDelegate is not null ? Tuple.Create(combinedDelegate, combinedDelegate.GetInvocationList()) : null;
                if (Interlocked.CompareExchange(ref field, newTuple, oldTuple) == oldTuple)
                {
                    break;
                }
            }
        }

        /// <summary>Removes a delegate from an existing delegate.</summary>
        /// <param name="field">A tuple of a delegate and its invocation list already in array form.</param>
        /// <param name="value">The delegate to remove.</param>
        public static void RemoveHandler<T>(ref Tuple<T, Delegate[]> field, T value) where T : Delegate
        {
            while (true)
            {
                Tuple<T, Delegate[]> oldTuple = field;
                T delegateWithRemoval = (T)Delegate.Remove(oldTuple?.Item1, value);
                Tuple<T, Delegate[]> newTuple = delegateWithRemoval is not null ? Tuple.Create(delegateWithRemoval, delegateWithRemoval.GetInvocationList()) : null;
                if (Interlocked.CompareExchange(ref field, newTuple, oldTuple) == oldTuple)
                {
                    break;
                }
            }
        }
    }
}
