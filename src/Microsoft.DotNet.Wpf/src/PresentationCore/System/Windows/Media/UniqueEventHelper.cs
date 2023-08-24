// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows.Media
{
    /// <summary>
    /// Aids in making events unique. i.e. you register the same function for
    /// the same event twice, but it will only be called once for each time
    /// the event is invoked.
    ///
    /// UniqueEventHelper should only be accessed from the UI thread so that
    /// handlers that throw exceptions will crash the app, making the developer
    /// aware of the problem.
    /// </summary>
    internal sealed class UniqueEventHelper<TEventArgs> : UniqueEventHelperBase<EventHandler<TEventArgs>>
        where TEventArgs : EventArgs
    {
        /// <summary>Clones the event helper</summary>
        internal UniqueEventHelper<TEventArgs> Clone()
        {
            var ueh = new UniqueEventHelper<TEventArgs>();
            if (_delegates != null)
            {
                ueh._delegates = new Dictionary<EventHandler<TEventArgs>, int>(_delegates);
            }

            return ueh;
        }

        /// <summary>
        /// Enumerates all the keys in the hashtable, which must be EventHandlers,
        /// and invokes them.
        /// </summary>
        /// <param name="sender">The sender for the callback.</param>
        /// <param name="args">The args object to be sent by the delegate</param>
        internal void InvokeEvents(object sender, TEventArgs args)
        {
            Debug.Assert(sender != null, "Sender is null");
            foreach (EventHandler<TEventArgs> handler in CopyHandlers())
            {
                Debug.Assert(handler != null, "Event handler is null");
                handler(sender, args);
            }
        }
    }

    internal sealed class UniqueEventHelper : UniqueEventHelperBase<EventHandler>
    {
        /// <summary>Clones the event helper</summary>
        internal UniqueEventHelper Clone()
        {
            var ueh = new UniqueEventHelper();
            if (_delegates != null)
            {
                ueh._delegates = new Dictionary<EventHandler, int>(_delegates);
            }

            return ueh;
        }

        /// <summary>
        /// Enumerates all the keys in the hashtable, which must be EventHandlers,
        /// and invokes them.
        /// </summary>
        /// <param name="sender">The sender for the callback.</param>
        /// <param name="args">The args object to be sent by the delegate</param>
        internal void InvokeEvents(object sender, EventArgs args)
        {
            Debug.Assert(sender != null, "Sender is null");
            foreach (EventHandler handler in CopyHandlers())
            {
                Debug.Assert(handler != null, "Event handler is null");
                handler(sender, args);
            }
        }
    }

    internal abstract class UniqueEventHelperBase<TEventHandler> where TEventHandler : Delegate
    {
        protected Dictionary<TEventHandler, int> _delegates;

        /// <summary>
        /// Add the handler to the list of handlers associated with this event.
        /// If the handler has already been added, we simply increment the ref
        /// count. That way if this same handler has already been added, it
        /// won't be invoked multiple times when the event is raised.
        /// </summary>
        internal void AddEvent(TEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (_delegates is Dictionary<TEventHandler, int> delegates)
            {
                delegates.TryGetValue(handler, out int refCount);
                delegates[handler] = refCount + 1;
            }
            else
            {
                _delegates = new Dictionary<TEventHandler, int>() { { handler, 1 } };
            }
        }

        /// <summary>
        /// Removed the handler from the list of handlers associated with this
        /// event. If the handler has been added multiple times (more times than
        /// it has been removed), we simply decrement its ref count.
        /// </summary>
        internal void RemoveEvent(TEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (_delegates is Dictionary<TEventHandler, int> delegates &&
                delegates.TryGetValue(handler, out int refCount))
            {
                if (refCount == 1)
                {
                    delegates.Remove(handler);
                }
                else
                {
                    delegates[handler] = refCount - 1;
                }
            }
        }

        protected TEventHandler[] CopyHandlers()
        {
            if (_delegates is { Count: > 0 } delegates)
            {
                var handlers = new TEventHandler[delegates.Count];
                delegates.Keys.CopyTo(handlers, 0);
                return handlers;
            }

            return Array.Empty<TEventHandler>();
        }
    }
}
