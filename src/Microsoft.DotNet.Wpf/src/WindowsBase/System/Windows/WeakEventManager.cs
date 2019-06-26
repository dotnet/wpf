// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Base class for event managers in the "weak event listener"
//              pattern.
//

/***************************************************************************\
    The standard mechanism for listening to events contains an inherent
    potential memory leak.  This class (and its derived classes) and the
    related class WeakEventTable provide a workaround.

    The leak occurs when all the following conditions hold:
        a. Object A wants to listen to an event Foo from object B.
        b. A uses an instance method (not a static method) as its event handler.
        c. A's lifetime should not depend on B's.
        d. A does not know when to stop listening (or A should keep listening
            as long as it lives).

    Normally, A listens by adding an event handler to B's Foo event:
                B.Foo += new FooEventHandler(OnFoo);
    but the handler contains a strong reference to A, and thus B now effectively
    has a strong reference to A.  Because of (d), this reference keeps A
    alive at least as long as B, which is a leak because of (c).

    The solution to this kind of leak is to introduce an intermediate "proxy"
    object P with the following properties:
        1. P does the actual listening to B.
        2. P maintains a list of "real listeners" such as A, using weak references.
        3. When P receives an event, it forwards it to the real listeners that
            are still alive.
        4. P's lifetime is expected to be as long as the app (or Dispatcher).

    This replaces the strong reference from B to A by a strong reference from
    B to P and a weak reference from P to A.  Thus B's lifetime will not affect A's.
    The only object that can leak is P, but this is OK because of (d).

    In the implementation of this idea, the role of P is played by a singleton
    instance of a class derived from WeakEventManager.  There is one such class
    for each event declaration.  The global WeakEventTable keeps track of the
    manager instances.

    The implementation also fulfills the following additional requirements:
        5. Events can be raised (and hence delivered) on any thread.
        6. Each event is delivered to all the listeners present at the time
            P receives the event, even if a listener modifies the list (e.g.
            by removing itself or another listener, or adding a new listener).
        7. P does not hold a strong reference to the event source B.
        8. P automatically purges its list of dead entries (where either the
            source or the listener has died and been GC'd).  This is done
            frequently enough to avoid large "leaks" of entries that are
            dead but not yet discovered, but not so frequently that the cost
            of purging becomes a noticeable perf hit.
        9. New events can be easily added to the system, by defining a new
            derived class and implementing a few simple methods.

\***************************************************************************/

using System;
using System.Diagnostics;           // Debug
using System.Collections;           // Hashtable
using System.Collections.Generic;   // List<T>
using System.Reflection;            // MethodInfo
using System.Threading;             // Interlocked
using System.Security;              // 
using System.Windows;               // SR
using System.Windows.Threading;     // DispatcherObject
using MS.Utility;                   // FrugalList
using MS.Internal;                  // Invariant
using MS.Internal.WindowsBase;      // [FriendAccessAllowed]

namespace System.Windows
{
    //
    //  See WeakEventManagerTemplate.cs for instructions on how to subclass
    //  this abstract base class.
    //

    /// <summary>
    /// This base class provides common functionality for event managers,
    /// in support of the "weak event listener" pattern.
    /// </summary>
    public abstract class WeakEventManager : DispatcherObject
    {
        #region Constructors

        //
        //  Constructors
        //

        /// <summary>
        /// Create a new instance of WeakEventManager.
        /// </summary>
        protected WeakEventManager()
        {
            _table = WeakEventTable.CurrentWeakEventTable;
        }

        // initialize static fields
        static WeakEventManager()
        {
            s_DeliverEventMethodInfo = typeof(WeakEventManager).GetMethod("DeliverEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        #endregion Constructors

        #region Protected Properties

        //
        //  Protected Properties
        //

        /// <summary>
        /// Take a read-lock on the table, and return the IDisposable.
        /// Queries to the table should occur within a
        /// "using (ReadLock) { ... }" clause, except for queries
        /// that are already within a write lock.
        /// </summary>
        protected IDisposable ReadLock
        {
            get { return Table.ReadLock; }
        }

        /// <summary>
        /// Take a write-lock on the table, and return the IDisposable.
        /// All modifications to the table should occur within a
        /// "using (WriteLock) { ... }" clause.
        /// </summary>
        protected IDisposable WriteLock
        {
            get { return Table.WriteLock; }
        }

        /// <summary>
        /// The data associated with the given source.  Subclasses chose
        /// what to store here;  most commonly it is a ListenerList - a list
        /// of weak references to listeners.
        /// </summary>
        protected object this[object source]
        {
            get { return Table[this, source]; }
            set { Table[this, source] = value; }
        }

        /// <summary>
        /// MethodInfo for the DeliverEvent method - used by generic WeakEventManager.
        /// </summary>
        internal static MethodInfo DeliverEventMethodInfo
        {
            get { return s_DeliverEventMethodInfo; }
        }

        #endregion Protected Properties

        #region Protected Methods

        //
        //  Protected Methods
        //

        /// <summary>
        /// Return a new list to hold listeners to the event.
        /// </summary>
        protected virtual ListenerList NewListenerList()
        {
            return new ListenerList();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected abstract void StartListening(object source);

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected abstract void StopListening(object source);

        /// <summary>
        /// Get the current manager for the given manager type.
        /// </summary>
        protected static WeakEventManager GetCurrentManager(Type managerType)
        {
            WeakEventTable table = WeakEventTable.CurrentWeakEventTable;
            return table[managerType];
        }

        /// <summary>
        /// Set the current manager for the given manager type.
        /// </summary>
        protected static void SetCurrentManager(Type managerType, WeakEventManager manager)
        {
            WeakEventTable table = WeakEventTable.CurrentWeakEventTable;
            table[managerType] = manager;
        }

        /// <summary>
        /// Get the current manager for the given event.
        /// </summary>
        internal static WeakEventManager GetCurrentManager(Type eventSourceType, string eventName)
        {
            WeakEventTable table = WeakEventTable.CurrentWeakEventTable;
            return table[eventSourceType, eventName];
        }

        /// <summary>
        /// Set the current manager for the given event.
        /// </summary>
        internal static void SetCurrentManager(Type eventSourceType, string eventName, WeakEventManager manager)
        {
            WeakEventTable table = WeakEventTable.CurrentWeakEventTable;
            table[eventSourceType, eventName] = manager;
        }

        /// <summary>
        /// Discard the data associated with the given source
        /// </summary>
        protected void Remove(object source)
        {
            Table.Remove(this, source);
        }

        /// <summary>
        /// Add a listener to the given source for the event.
        /// </summary>
        protected void ProtectedAddListener(object source, IWeakEventListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            AddListener(source, listener, null);
        }

        /// <summary>
        /// Remove a listener to the given source for the event.
        /// </summary>
        protected void ProtectedRemoveListener(object source, IWeakEventListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            RemoveListener(source, listener, null);
        }

        /// <summary>
        /// Add a handler to the given source for the event.
        /// </summary>
        protected void ProtectedAddHandler(object source, Delegate handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            AddListener(source, null, handler);
        }

        /// <summary>
        /// Remove a handler to the given source for the event.
        /// </summary>
        protected void ProtectedRemoveHandler(object source, Delegate handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            RemoveListener(source, null, handler);
        }

        private void AddListener(object source, IWeakEventListener listener, Delegate handler)
        {
            object sourceKey = (source != null) ? source : StaticSource;

            using (Table.WriteLock)
            {
                ListenerList list = (ListenerList)Table[this, sourceKey];

                if (list == null)
                {
                    // no entry in the table - add a new one
                    list = NewListenerList();
                    Table[this, sourceKey] = list;

                    // listen for the desired event
                    StartListening(source);
                }

                // make sure list is ready for writing
                if (ListenerList.PrepareForWriting(ref list))
                {
                    Table[this, source] = list;
                }

                // add a target to the list of listeners
                if (handler != null)
                {
                    list.AddHandler(handler);
                }
                else
                {
                    list.Add(listener);
                }

                // schedule a cleanup pass (heuristic (b) described above)
                ScheduleCleanup();
            }
        }

        private void RemoveListener(object source, object target, Delegate handler)
        {
            object sourceKey = (source != null) ? source : StaticSource;

            using (Table.WriteLock)
            {
                ListenerList list = (ListenerList)Table[this, sourceKey];

                if (list != null)
                {
                    // make sure list is ready for writing
                    if (ListenerList.PrepareForWriting(ref list))
                    {
                        Table[this, sourceKey] = list;
                    }

                    // remove the target from the list of listeners
                    if (handler != null)
                    {
                        list.RemoveHandler(handler);
                    }
                    else
                    {
                        list.Remove((IWeakEventListener)target);
                    }

                    // after removing the last listener, stop listening
                    if (list.IsEmpty)
                    {
                        Table.Remove(this, sourceKey);

                        StopListening(source);
                    }
                }
            }
        }

        /// <summary>
        /// Deliver an event to each listener.
        /// </summary>
        protected void DeliverEvent(object sender, EventArgs args)
        {
            ListenerList list;
            object sourceKey = (sender != null) ? sender : StaticSource;

            // get the list of listeners
            using (Table.ReadLock)
            {
                list = (ListenerList)Table[this, sourceKey];
                if (list == null)
                {
                    list = ListenerList.Empty;
                }

                // mark the list "in use", even outside the read lock,
                // so that any writers will know not to modify it (they'll
                // modify a clone intead).
                list.BeginUse();
            }

            // deliver the event, being sure to undo the effect of BeginUse().
            try
            {
                DeliverEventToList(sender, args, list);
            }
            finally
            {
                list.EndUse();
            }
        }

        /// <summary>
        /// Deliver an event to the listeners on the given list
        /// </summary>
        protected void DeliverEventToList(object sender, EventArgs args, ListenerList list)
        {
            bool foundStaleEntries = list.DeliverEvent(sender, args, this.GetType());

            // if we found stale entries, schedule a cleanup (heuristic b)
            if (foundStaleEntries)
            {
                ScheduleCleanup();
            }
        }

        /// <summary>
        /// Schedule a cleanup pass.
        /// </summary>
        protected void ScheduleCleanup()
        {
            Table.ScheduleCleanup();
        }

        /// <summary>
        /// Remove dead entries from the data for the given source.   Returns true if
        /// some entries were actually removed.
        /// </summary>
        protected virtual bool Purge(object source, object data, bool purgeAll)
        {
            bool foundDirt = false;

            bool removeList = purgeAll || source == null;

            // remove dead entries from the list
            if (!removeList)
            {
                ListenerList list = (ListenerList)data;

                if (ListenerList.PrepareForWriting(ref list) && source != null)
                {
                    Table[this, source] = list;
                }

                if (list.Purge())
                    foundDirt = true;

                removeList = list.IsEmpty;
            }

            // if the list is no longer needed, stop listening to the event
            if (removeList)
            {
                if (source != null) // source may have been GC'd
                {
                    StopListening(source);

                    // remove the list completely (in the purgeAll case, we'll do it later)
                    if (!purgeAll)
                    {
                        Table.Remove(this, source);
                        foundDirt = true;
                    }
                }
            }

            return foundDirt;
        }

#if WeakEventTelemetry
        protected void LogAllocation(Type type, int count, int bytes)
        {
            Table.LogAllocation(type, count, bytes);
        }
#endif

        #endregion Protected Methods

        #region Internal Methods

        //
        //  Internal Methods
        //

        // this should only be called by WeakEventTable
        internal bool PurgeInternal(object source, object data, bool purgeAll)
        {
            return Purge(source, data, purgeAll);
        }

        // for use by test programs (e.g. leak detectors) that want to force
        // a cleanup pass.
        [FriendAccessAllowed]   // defined in Base, used by Framework
        internal static bool Cleanup()
        {
            return WeakEventTable.Cleanup();
        }

        // for use by test programs (e.g. perf tests) that want to disable
        // cleanup passes temporarily.
        [FriendAccessAllowed]   // defined in Base, used by Framework
        internal static void SetCleanupEnabled(bool value)
        {
            WeakEventTable.CurrentWeakEventTable.IsCleanupEnabled = value;
        }

        #endregion Internal Methods

        #region Private Properties

        //
        //  Private Properties
        //

        private WeakEventTable Table
        {
            get { return _table; }
        }

        #endregion Private Properties

        #region Private Fields

        //
        //  Private Fields
        //

        private WeakEventTable  _table;
        private static readonly object StaticSource = new NamedObject("StaticSource");
        private static MethodInfo s_DeliverEventMethodInfo;

        #endregion Private Fields

        #region ListenerList

        internal struct Listener
        {
            public Listener(object target)
            {
                if (target == null)
                    target = StaticSource;
                _target = new WeakReference(target);
                _handler = null;
            }

            public Listener(object target, Delegate handler)
            {
                _target = new WeakReference(target);
                _handler = new WeakReference(handler);
            }

            public bool Matches(object target, Delegate handler)
            {
                return Object.ReferenceEquals(target, Target) &&
                        Object.Equals(handler, Handler);
            }

            public object Target { get { return _target.Target; } }
            public Delegate Handler { get { return (_handler != null) ? (Delegate)_handler.Target : null; } }
            public bool HasHandler { get { return _handler != null; } }

            WeakReference _target;
            WeakReference _handler;
        }

        /// <summary>
        /// This class implements the most common data that a simple manager
        /// might want to store for a given source:  a list of weak references
        /// to the listeners.
        /// </summary>
        protected class ListenerList
        {
            /// <summary>
            /// Create a new instance of ListenerList.
            /// </summary>
            public ListenerList()
            {
                _list = new FrugalObjectList<Listener>();
            }

            /// <summary>
            /// Create a new instance of ListenerList, with given capacity.
            /// </summary>
            public ListenerList(int capacity)
            {
                _list = new FrugalObjectList<Listener>(capacity);
            }

            /// <summary>
            /// Return the listener at the given index.
            /// </summary>
            public IWeakEventListener this[int index]
            {
                get { return (IWeakEventListener)_list[index].Target; }
            }

            internal Listener GetListener(int index)
            {
                return _list[index];
            }

            /// <summary>
            /// Return the number of listeners.
            /// </summary>
            public int Count
            {
                get { return _list.Count; }
            }

            /// <summary>
            /// Return true if there are no listeners.
            /// </summary>
            public bool IsEmpty
            {
                get { return _list.Count == 0; }
            }

            /// <summary>
            /// An empty list of listeners.
            /// </summary>
            public static ListenerList Empty
            {
                get { return s_empty; }
            }

            /// <summary>
            /// Add the given listener to the list.
            /// </summary>
            public void Add(IWeakEventListener listener)
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");
                _list.Add(new Listener(listener));
            }

            /// <summary>
            /// Remove the given listener from the list.
            /// </summary>
            public void Remove(IWeakEventListener listener)
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");
                for (int i=_list.Count-1; i>=0; --i)
                {
                    if (_list[i].Target == listener)
                    {
                        _list.RemoveAt(i);
                        break;
                    }
                }
            }

            public void AddHandler(Delegate handler)
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");

                object target = handler.Target;
                if (target == null)
                    target = StaticSource;

                // add a record to the main list
                _list.Add(new Listener(target, handler));

                AddHandlerToCWT(target, handler);
            }

            void AddHandlerToCWT(object target, Delegate handler)
            {
                // add the handler to the CWT - this keeps the handler alive throughout
                // the lifetime of the target, without prolonging the lifetime of
                // the target
                object value;
                if (!_cwt.TryGetValue(target, out value))
                {
                    // 99% case - the target only listens once
                    _cwt.Add(target, handler);
                }
                else
                {
                    // 1% case - the target listens multiple times
                    // we store the delegates in a list
                    List<Delegate> list = value as List<Delegate>;
                    if (list == null)
                    {
                        // lazily allocate the list, and add the old handler
                        Delegate oldHandler = value as Delegate;
                        list = new List<Delegate>();
                        list.Add(oldHandler);

                        // install the list as the CWT value
                        _cwt.Remove(target);
                        _cwt.Add(target, list);
                    }

                    // add the new handler to the list
                    list.Add(handler);
                }
            }

            public void RemoveHandler(Delegate handler)
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");

                object value;
                object target = handler.Target;
                if (target == null)
                    target = StaticSource;

                // remove the record from the main list
                for (int i=_list.Count-1; i>=0; --i)
                {
                    if (_list[i].Matches(target, handler))
                    {
                        _list.RemoveAt(i);
                        break;
                    }
                }

                // remove the handler from the CWT
                if (_cwt.TryGetValue(target, out value))
                {
                    List<Delegate> list = value as List<Delegate>;
                    if (list == null)
                    {
                        // 99% case - the target is removing its single handler
                        _cwt.Remove(target);
                    }
                    else
                    {
                        // 1% case - the target had multiple handlers, and is removing one
                        list.Remove(handler);
                        if (list.Count == 0)
                        {
                            _cwt.Remove(target);
                        }
                    }
                }
                else
                {
                    // target has been GC'd.  This probably can't happen, since the
                    // target initiates the Remove.  But if it does, there's nothing
                    // to do - the target is removed from the CWT automatically,
                    // and the weak-ref in the main list will be removed
                    // at the next Purge.
                }
            }

            /// <summary>
            /// Add the given listener to the list.
            /// </summary>
            internal void Add(Listener listener)
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");

                // no need to add if the listener has been GC'd
                object target = listener.Target;
                if (target == null)
                    return;

                _list.Add(listener);
                if (listener.HasHandler)
                {
                    AddHandlerToCWT(target, listener.Handler);
                }
            }

            /// <summary>
            /// If the given list is in use (which means an event is currently
            /// being delivered), replace it with a clone.  The existing
            /// users will finish delivering the event to the original list,
            /// without interference from changes to the new list.
            /// </summary>
            /// <returns>
            /// True if the list was cloned.  Callers will probably want to
            /// insert the new list in their own data structures.
            /// </returns>
            public static bool PrepareForWriting(ref ListenerList list)
            {
                bool inUse = list.BeginUse();
                list.EndUse();

                if (inUse)
                {
                    list = list.Clone();
                }

                return inUse;
            }

            public virtual bool DeliverEvent(object sender, EventArgs args, Type managerType)
            {
                bool foundStaleEntries = false;

                for (int k=0, n=Count; k<n; ++k)
                {
                    Listener listener = GetListener(k);
                    foundStaleEntries |= DeliverEvent(ref listener, sender, args, managerType);
                }

                return foundStaleEntries;
            }

            internal bool DeliverEvent(ref Listener listener, object sender, EventArgs args, Type managerType)
            {
                object target = listener.Target;
                bool entryIsStale = (target == null);

                if (!entryIsStale)
                {
                    if (listener.HasHandler)
                    {
                        EventHandler handler = (EventHandler)listener.Handler;
                        if (handler != null)
                        {
                            handler(sender, args);
                        }
                    }
                    else
                    {
                        // legacy (4.0)
                        IWeakEventListener iwel = target as IWeakEventListener;
                        if (iwel != null)
                        {
                            bool handled = iwel.ReceiveWeakEvent(managerType, sender, args);

                            // if the event isn't handled, something is seriously wrong.  This
                            // means a listener registered to receive the event, but refused to
                            // handle it when it was delivered.  Such a listener is coded incorrectly.
                            if (!handled)
                            {
                                Invariant.Assert(handled,
                                            SR.Get(SRID.ListenerDidNotHandleEvent),
                                            SR.Get(SRID.ListenerDidNotHandleEventDetail, iwel.GetType(), managerType));
                            }
                        }
                    }
                }

                return entryIsStale;
            }

            /// <summary>
            /// Purge the list of stale entries.  Returns true if any stale
            /// entries were purged.
            /// </summary>
            public bool Purge()
            {
                Invariant.Assert(_users == 0, "Cannot modify a ListenerList that is in use");
                bool foundDirt = false;

                for (int j=_list.Count-1; j>=0; --j)
                {
                    if (_list[j].Target == null)
                    {
                        _list.RemoveAt(j);
                        foundDirt = true;
                    }
                }

                return foundDirt;
            }

            /// <summary>
            /// Return a copy of the list.
            /// </summary>
            public virtual ListenerList Clone()
            {
                ListenerList result = new ListenerList();
                CopyTo(result);
                return result;
            }

            protected void CopyTo(ListenerList newList)
            {
                IWeakEventListener iwel;

                for (int k=0, n=Count; k<n; ++k)
                {
                    Listener listener = GetListener(k);
                    if (listener.Target != null)
                    {
                        if (listener.HasHandler)
                        {
                            Delegate handler = listener.Handler;
                            if (handler != null)
                            {
                                newList.AddHandler(handler);
                            }
                        }
                        else if ((iwel = listener.Target as IWeakEventListener) != null)
                        {
                            newList.Add(iwel);
                        }
                    }
                }
            }

            /// <summary>
            /// Mark the list as 'in use'.  An event manager should call BeginUse()
            /// before iterating through the list to deliver an event to the listeners,
            /// and should call EndUse() when it is done.  This prevents another
            /// user from modifying the list while the iteration is in progress.
            /// </summary>
            /// <returns> True if the list is already in use.</returns>
            public bool BeginUse()
            {
                return (Interlocked.Increment(ref _users) != 1);
            }

            /// <summary>
            /// Undo the effect of BeginUse().
            /// </summary>
            public void EndUse()
            {
                Interlocked.Decrement(ref _users);
            }

            private FrugalObjectList<Listener> _list;  // list of listeners
            private int _users;     // number of active users
            private System.Runtime.CompilerServices.ConditionalWeakTable<object, object>
                _cwt = new System.Runtime.CompilerServices.ConditionalWeakTable<object, object>();

            private static ListenerList s_empty = new ListenerList();
        }

        protected class ListenerList<TEventArgs> : ListenerList
            where TEventArgs : EventArgs
        {
            public ListenerList() : base() {}
            public ListenerList(int capacity) : base(capacity) {}

            public override bool DeliverEvent(object sender, EventArgs e, Type managerType)
            {
                TEventArgs args = (TEventArgs)e;
                bool foundStaleEntries = false;

                for (int k=0, n=Count; k<n; ++k)
                {
                    Listener listener = GetListener(k);
                    if (listener.Target != null)
                    {
                        EventHandler<TEventArgs> handler = (EventHandler<TEventArgs>)listener.Handler;
                        if (handler != null)
                        {
                            handler(sender, args);
                        }
                        else
                        {
                            // legacy (4.0)
                            foundStaleEntries |= base.DeliverEvent(ref listener, sender, e, managerType);
                        }
                    }
                    else
                    {
                        foundStaleEntries = true;
                    }
                }

                return foundStaleEntries;
            }

            public override ListenerList Clone()
            {
                ListenerList<TEventArgs> result = new ListenerList<TEventArgs>();
                CopyTo(result);
                return result;
            }
        }

        #endregion ListenerList
    }
}
