// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Manager for the ValueChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

/****************************************************************************\
    The "value changed" pattern doesn't use events.  To listen for changes
    in a property, a client first obtains the PropertyDescriptor for that
    property, then calls the AddValueChanged method to register a callback.
    The arguments to the callback don't say which property has changed(!).

    The standard manager implementation doesn't work for this.  Hence this
    manager overrides and/or ignores the base class methods.

    This manager keeps a table of records, indexed by PropertyDescriptor.
    Each record holds the following information:
        PropertyDescriptor
        Callback method
        ListenerList
    In short, there's a separate callback method for each property.  That
    method knows which property has changed, and can ask the manager to
    deliver the "event" to the listeners that are interested in that property.
\****************************************************************************/

using System;
using System.Collections;               // ICollection
using System.Collections.Generic;       // List<T>
using System.Collections.Specialized;   // HybridDictionary
using System.ComponentModel;            // PropertyDescriptor
using System.Diagnostics;               // Debug
using System.Reflection;                // MethodInfo
using System.Windows;                   // WeakEventManager
using MS.Internal.PresentationFramework; // SR

namespace MS.Internal.Data
{
    /// <summary>
    /// Manager for the object.ValueChanged event.
    /// </summary>
    internal class ValueChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private ValueChangedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a listener to the given source's event.
        /// </summary>
        public static void AddListener(object source, IWeakEventListener listener, PropertyDescriptor pd)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.PrivateAddListener(source, listener, pd);
        }

        /// <summary>
        /// Remove a listener to the given source's event.
        /// </summary>
        public static void RemoveListener(object source, IWeakEventListener listener, PropertyDescriptor pd)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.PrivateRemoveListener(source, listener, pd);
        }

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(object source, EventHandler<ValueChangedEventArgs> handler, PropertyDescriptor pd)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            if (handler.GetInvocationList().Length != 1)
                throw new NotSupportedException(SR.Get(SRID.NoMulticastHandlers));

            CurrentManager.PrivateAddHandler(source, handler, pd);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(object source, EventHandler<ValueChangedEventArgs> handler, PropertyDescriptor pd)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            if (handler.GetInvocationList().Length != 1)
                throw new NotSupportedException(SR.Get(SRID.NoMulticastHandlers));

            CurrentManager.PrivateRemoveHandler(source, handler, pd);
        }

        #endregion Public Methods

        #region Protected Methods

        //
        //  Protected Methods
        //

        /// <summary>
        /// Return a new list to hold listeners to the event.
        /// </summary>
        protected override ListenerList NewListenerList()
        {
            return new ListenerList<ValueChangedEventArgs>();
        }

        // The next two methods need to be defined, but they're never called.

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
        }

        /// <summary>
        /// Remove dead entries from the data for the given source.   Returns true if
        /// some entries were actually removed.
        /// </summary>
        protected override bool Purge(object source, object data, bool purgeAll)
        {
            bool foundDirt = false;

            HybridDictionary dict = (HybridDictionary)data;

            if (!MS.Internal.BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
            {
                // copy the keys into a separate array, so that later on
                // we can change the dictionary while iterating over the keys
                ICollection ic = dict.Keys;
                PropertyDescriptor[] keys = new PropertyDescriptor[ic.Count];
                ic.CopyTo(keys, 0);

                for (int i = keys.Length - 1; i >= 0; --i)
                {
                    // for each key, remove dead entries in its list
                    bool removeList = purgeAll || source == null;

                    ValueChangedRecord record = (ValueChangedRecord)dict[keys[i]];

                    if (!removeList)
                    {
                        if (record.Purge())
                            foundDirt = true;

                        removeList = record.IsEmpty;
                    }

                    // if there are no more entries, remove the key
                    if (removeList)
                    {
                        record.StopListening();
                        if (!purgeAll)
                        {
                            dict.Remove(keys[i]);
                        }
                    }
                }

#if WeakEventTelemetry
                LogAllocation(ic.GetType(), 1, 12);                     // dict.Keys - Hashtable+KeyCollection
                LogAllocation(typeof(String[]), 1, 12+ic.Count*4);      // keys
#endif
            }
            else
            {
                Debug.Assert(_toRemove.Count == 0, "to-remove list should be empty");

                // enumerate the dictionary using IDE explicitly rather than
                // foreach, to avoid allocating temporary DictionaryEntry objects
                IDictionaryEnumerator ide = dict.GetEnumerator() as IDictionaryEnumerator;
                while (ide.MoveNext())
                {
                    // for each key, remove dead entries in its list
                    bool removeList = purgeAll || source == null;

                    ValueChangedRecord record = (ValueChangedRecord)ide.Value;

                    if (!removeList)
                    {
                        if (record.Purge())
                            foundDirt = true;

                        removeList = record.IsEmpty;
                    }

                    // if there are no more entries, remove the key
                    if (removeList)
                    {
                        record.StopListening();
                        if (!purgeAll)
                        {
                            _toRemove.Add((PropertyDescriptor)ide.Key);
                        }
                    }
                }

                // do the actual removal (outside the dictionary iteration)
                if (_toRemove.Count > 0)
                {
                    foreach (PropertyDescriptor key in _toRemove)
                    {
                        dict.Remove(key);
                    }
                    _toRemove.Clear();
                    _toRemove.TrimExcess();
                }

#if WeakEventTelemetry
                Type enumeratorType = ide.GetType();
                if (enumeratorType.Name.IndexOf("NodeEnumerator") >= 0)
                {
                    LogAllocation(enumeratorType, 1, 24);                    // ListDictionary+NodeEnumerator
                }
                else
                {
                    LogAllocation(enumeratorType, 1, 36);                    // Hashtable+HashtableEnumerator
                }
#endif
            }


            // if there are no more listeners at all, remove the entry from
            // the main table
            if (dict.Count == 0)
            {
                foundDirt = true;
                if (source != null)     // source may have been GC'd
                {
                    this.Remove(source);
                }
            }

            return foundDirt;
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static ValueChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(ValueChangedEventManager);
                ValueChangedEventManager manager = (ValueChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new ValueChangedEventManager();
                    SetCurrentManager(managerType, manager);
                }

                return manager;
            }
        }

        #endregion Private Properties

        #region Private Methods

        //
        //  Private Methods
        //

        // Add a listener to the given property
        private void PrivateAddListener(object source, IWeakEventListener listener, PropertyDescriptor pd)
        {
            Debug.Assert(listener != null && source != null && pd != null,
                "Listener, source, and pd of event cannot be null");
            AddListener(source, pd, listener, null);
        }

        // Remove a listener to the given property
        private void PrivateRemoveListener(object source, IWeakEventListener listener, PropertyDescriptor pd)
        {
            Debug.Assert(listener != null && source != null && pd != null,
                "Listener, source, and pd of event cannot be null");
            RemoveListener(source, pd, listener, null);
        }

        // Add a handler for the given property
        private void PrivateAddHandler(object source, EventHandler<ValueChangedEventArgs> handler, PropertyDescriptor pd)
        {
            AddListener(source, pd, null, handler);
        }

        // Remove a handler for the given property
        private void PrivateRemoveHandler(object source, EventHandler<ValueChangedEventArgs> handler, PropertyDescriptor pd)
        {
            RemoveListener(source, pd, null, handler);
        }

        private void AddListener(object source, PropertyDescriptor pd, IWeakEventListener listener, EventHandler<ValueChangedEventArgs> handler)
        {
            using (WriteLock)
            {
                HybridDictionary dict = (HybridDictionary)this[source];

                if (dict == null)
                {
                    // no entry in the hashtable - add a new one
                    dict = new HybridDictionary();

                    this[source] = dict;
                }

                ValueChangedRecord record = (ValueChangedRecord)dict[pd];

                if (record == null)
                {
                    // no entry in the dictionary - add a new one
                    record = new ValueChangedRecord(this, source, pd);

                    dict[pd] = record;
                }

                // add a listener to the list
                record.Add(listener, handler);

                // schedule a cleanup pass
                ScheduleCleanup();
            }
        }

        private void RemoveListener(object source, PropertyDescriptor pd, IWeakEventListener listener, EventHandler<ValueChangedEventArgs> handler)
        {
            using (WriteLock)
            {
                HybridDictionary dict = (HybridDictionary)this[source];

                if (dict != null)
                {
                    ValueChangedRecord record = (ValueChangedRecord)dict[pd];

                    if (record != null)
                    {
                        // remove a listener from the list
                        record.Remove(listener, handler);

                        // when the last listener goes away, remove the list
                        if (record.IsEmpty)
                        {
                            dict.Remove(pd);
                        }
                    }

                    if (dict.Count == 0)
                    {
                        Remove(source);
                    }
                }
            }
        }

        #endregion Private Methods

        List<PropertyDescriptor> _toRemove = new List<PropertyDescriptor>();

        #region ValueChangedRecord

        private class ValueChangedRecord
        {
            public ValueChangedRecord(ValueChangedEventManager manager, object source, PropertyDescriptor pd)
            {
                // keep a strong reference to the source.  Normally we avoid this, but
                // it's OK here since its scope is exactly the same as the strong reference
                // held by the PD:  begins with pd.AddValueChanged, ends with
                // pd.RemoveValueChanged.   This ensures that we _can_ call RemoveValueChanged
                // even in cases where the source implements value-semantics (which
                // confuses the PD - see 795205).
                _manager = manager;
                _source = source;
                _pd = pd;
                _eventArgs = new ValueChangedEventArgs(pd);

                pd.AddValueChanged(source, new EventHandler(OnValueChanged));
            }

            public bool IsEmpty
            {
                get
                {
                    bool result = _listeners.IsEmpty;
                    if (!result && HasIgnorableListeners)
                    {
                        // if all the remaining listeners are "ignorable",
                        // treat the list as empty
                        result = true;
                        for (int i = 0, n = _listeners.Count; i < n; ++i)
                        {
                            Listener listener = _listeners.GetListener(i);
                            if (!IsIgnorable(listener.Target))
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                    return result;
                }
            }

            // add a listener
            public void Add(IWeakEventListener listener, EventHandler<ValueChangedEventArgs> handler)
            {
                // make sure list is ready for writing
                ListenerList list = _listeners;
                if (ListenerList.PrepareForWriting(ref list))
                    _listeners = (ListenerList<ValueChangedEventArgs>)list;

                if (handler != null)
                {
                    _listeners.AddHandler(handler);
                    if (!HasIgnorableListeners && IsIgnorable(handler.Target))
                    {
                        HasIgnorableListeners = true;
                    }
                }
                else
                {
                    _listeners.Add(listener);
                }
            }

            // remove a listener
            public void Remove(IWeakEventListener listener, EventHandler<ValueChangedEventArgs> handler)
            {
                // make sure list is ready for writing
                ListenerList list = _listeners;
                if (ListenerList.PrepareForWriting(ref list))
                    _listeners = (ListenerList<ValueChangedEventArgs>)list;

                if (handler != null)
                {
                    _listeners.RemoveHandler(handler);
                }
                else
                {
                    _listeners.Remove(listener);
                }

                // when the last listener goes away, remove the callback
                if (IsEmpty)
                {
                    StopListening();
                }
            }

            // purge dead entries
            public bool Purge()
            {
                ListenerList list = _listeners;
                if (ListenerList.PrepareForWriting(ref list))
                    _listeners = (ListenerList<ValueChangedEventArgs>)list;

                return _listeners.Purge();
            }

            // remove the callback from the PropertyDescriptor
            public void StopListening()
            {
                if (_source != null)
                {
                    _pd.RemoveValueChanged(_source, new EventHandler(OnValueChanged));
                    _source = null;
                }
            }

            // forward the ValueChanged event to the listeners
            private void OnValueChanged(object sender, EventArgs e)
            {
                // mark the list of listeners "in use"
                using (_manager.ReadLock)
                {
                    _listeners.BeginUse();
                }

                // deliver the event, being sure to undo the effect of BeginUse().
                try
                {
                    _manager.DeliverEventToList(sender, _eventArgs, _listeners);
                }
                finally
                {
                    _listeners.EndUse();
                }
            }

            // Some listeners are used only for internal bookkeeping.  These
            // shouldn't count as real listeners for the purpose of deciding
            // if anyone is still listening to the change event;  otherwise
            // we'd never release the event and we'd have a memory leak .
            // Call such listeners "ignorable";  the add, remove, and purge logic has
            // special cases for ignorable listeners.  Ignorable listeners are
            // rare, so we optimize for their absence.

            private bool HasIgnorableListeners { get; set; }

            private bool IsIgnorable(object target)
            {
                // ValueTable listens for changes from malfeasant ADO properties
                return (target is MS.Internal.Data.ValueTable);
            }

            PropertyDescriptor _pd;
            ValueChangedEventManager _manager;
            object _source;
            ListenerList<ValueChangedEventArgs> _listeners = new ListenerList<ValueChangedEventArgs>();
            ValueChangedEventArgs _eventArgs;
        }

        #endregion ValueChangedRecord
    }

    #region ValueChangedEventArgs

    internal class ValueChangedEventArgs : EventArgs
    {
        internal ValueChangedEventArgs(PropertyDescriptor pd)
        {
            _pd = pd;
        }

        internal PropertyDescriptor PropertyDescriptor
        {
            get { return _pd; }
        }

        private PropertyDescriptor _pd;
    }
    #endregion ValueChangedEventArgs
}

