// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
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
    internal class UniqueEventHelper<TEventArgs>
            where TEventArgs : EventArgs
    {
        /// <summary>
        /// Add the handler to the list of handlers associated with this event.
        /// If the handler has already been added, we simply increment the ref
        /// count. That way if this same handler has already been added, it
        /// won't be invoked multiple times when the event is raised.
        /// </summary>
        internal void AddEvent(EventHandler<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new System.ArgumentNullException("handler");
            }

            EnsureEventTable();

            if (_htDelegates[handler] == null)
            {
                _htDelegates.Add(handler, 1);
            }
            else
            {
                int refCount = (int)_htDelegates[handler] + 1;
                _htDelegates[handler] = refCount;
            }
        }

        /// <summary>
        /// Removed the handler from the list of handlers associated with this
        /// event. If the handler has been added multiple times (more times than
        /// it has been removed), we simply decrement its ref count.
        /// </summary>
        internal void RemoveEvent(EventHandler<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new System.ArgumentNullException("handler");
            }

            EnsureEventTable();

            if (_htDelegates[handler] != null)
            {
                int refCount = (int)_htDelegates[handler];

                if (refCount == 1)
                {
                    _htDelegates.Remove(handler);
                }
                else
                {
                    _htDelegates[handler] = refCount - 1;
                }
            }
        }

        /// <summary>
        /// Enumerates all the keys in the hashtable, which must be EventHandlers,
        /// and invokes them.
        /// </summary>
        /// <param name="sender">The sender for the callback.</param>
        /// <param name="args">The args object to be sent by the delegate</param>
        internal void InvokeEvents(object sender, TEventArgs args)
        {
            Debug.Assert((sender != null), "Sender is null");

            if (_htDelegates != null)
            {
                Hashtable htDelegates = (Hashtable)_htDelegates.Clone();
                foreach (EventHandler<TEventArgs> handler in htDelegates.Keys)
                {
                    Debug.Assert((handler != null), "Event handler is null");
                    handler(sender, args);
                }
            }
        }

        /// <summary>
        /// Clones the event helper
        /// </summary>
        internal UniqueEventHelper<TEventArgs> Clone()
        {
            UniqueEventHelper<TEventArgs> ueh = new UniqueEventHelper<TEventArgs>();
            if (_htDelegates != null)
            {
                ueh._htDelegates = (Hashtable)_htDelegates.Clone();
            }
            return ueh;
        }

        /// <summary>
        /// Ensures Hashtable is created so that event handlers can be added/removed
        /// </summary>
        private void EnsureEventTable()
        {
            if (_htDelegates == null)
            {
                _htDelegates = new Hashtable();
            }
        }

        private Hashtable _htDelegates;
    }



    // (if possible) figure out a way so that the
    // previous class can be used in place of this one. This class is needed
    // in addition to the above generic one because EventHandler cannot be
    // cast to its generic counterpart
    internal class UniqueEventHelper
    {
        /// <summary>
        /// Add the handler to the list of handlers associated with this event.
        /// If the handler has already been added, we simply increment the ref
        /// count. That way if this same handler has already been added, it
        /// won't be invoked multiple times when the event is raised.
        /// </summary>
        internal void AddEvent(EventHandler handler)
        {
            if (handler == null)
            {
                throw new System.ArgumentNullException("handler");
            }

            EnsureEventTable();

            if (_htDelegates[handler] == null)
            {
                _htDelegates.Add(handler, 1);
            }
            else
            {
                int refCount = (int)_htDelegates[handler] + 1;
                _htDelegates[handler] = refCount;
            }
        }

        /// <summary>
        /// Removed the handler from the list of handlers associated with this
        /// event. If the handler has been added multiple times (more times than
        /// it has been removed), we simply decrement its ref count.
        /// </summary>
        internal void RemoveEvent(EventHandler handler)
        {
            if (handler == null)
            {
                throw new System.ArgumentNullException("handler");
            }

            EnsureEventTable();

            if (_htDelegates[handler] != null)
            {
                int refCount = (int)_htDelegates[handler];

                if (refCount == 1)
                {
                    _htDelegates.Remove(handler);
                }
                else
                {
                    _htDelegates[handler] = refCount - 1;
                }
            }
        }

        /// <summary>
        /// Enumerates all the keys in the hashtable, which must be EventHandlers,
        /// and invokes them.
        /// </summary>
        /// <param name="sender">The sender for the callback.</param>
        /// <param name="args">The args object to be sent by the delegate</param>
        internal void InvokeEvents(object sender, EventArgs args)
        {
            Debug.Assert((sender != null), "Sender is null");

            if (_htDelegates != null)
            {
                Hashtable htDelegates = (Hashtable)_htDelegates.Clone();
                foreach (EventHandler handler in htDelegates.Keys)
                {
                    Debug.Assert((handler != null), "Event handler is null");
                    handler(sender, args);
                }
            }
        }

        /// <summary>
        /// Clones the event helper
        /// </summary>
        internal UniqueEventHelper Clone()
        {
            UniqueEventHelper ueh = new UniqueEventHelper();
            if (_htDelegates != null)
            {
                ueh._htDelegates = (Hashtable)_htDelegates.Clone();
            }
            return ueh;
        }

        /// <summary>
        /// Ensures Hashtable is created so that event handlers can be added/removed
        /// </summary>
        private void EnsureEventTable()
        {
            if (_htDelegates == null)
            {
                _htDelegates = new Hashtable();
            }
        }

        private Hashtable _htDelegates;
    }
}
