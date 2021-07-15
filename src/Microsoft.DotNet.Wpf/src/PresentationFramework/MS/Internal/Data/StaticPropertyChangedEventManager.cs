// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Manager for static property-changed events in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Collections;       // ICollection
using System.Collections.Generic; // List<T>
using System.Collections.Specialized;   // HybridDictionary
using System.ComponentModel;    // INotifyPropertyChanged
using System.Diagnostics;       // Debug
using System.Reflection;        // EventInfo
using System.Windows;           // WeakEventManager

namespace MS.Internal.Data
{
    /// <summary>
    /// Manager for the INotifyPropertyChanged.PropertyChanged event.
    /// </summary>
    internal class StaticPropertyChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private StaticPropertyChangedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(Type type, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.PrivateAddHandler(type, handler, propertyName);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(Type type, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.PrivateRemoveHandler(type, handler, propertyName);
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
            return new ListenerList<PropertyChangedEventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            Debug.Assert(false, "Should never get here");
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            Debug.Assert(false, "Should never get here");
        }

        /// <summary>
        /// Remove dead entries from the data for the given source.   Returns true if
        /// some entries were actually removed.
        /// </summary>
        protected override bool Purge(object source, object data, bool purgeAll)
        {
            TypeRecord typeRecord = (TypeRecord)data;
            bool foundDirt = typeRecord.Purge(purgeAll);

            if (!purgeAll && typeRecord.IsEmpty)
            {
                Remove(typeRecord.Type);
            }

            return foundDirt;
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static StaticPropertyChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(StaticPropertyChangedEventManager);
                StaticPropertyChangedEventManager manager = (StaticPropertyChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new StaticPropertyChangedEventManager();
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

        // PropertyChanged is a special case - we superimpose per-property granularity
        // on top of this event, by keeping separate lists of listeners for
        // each property.

        // Add a listener to the named property (empty means "any property")
        private void PrivateAddHandler(Type type, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            Debug.Assert(handler != null && type != null && propertyName != null,
                "Handler, type, and propertyName of event cannot be null");

            using (WriteLock)
            {
                TypeRecord tr = (TypeRecord)this[type];

                if (tr == null)
                {
                    // no entry in the hashtable - add a new one
                    tr = new TypeRecord(type, this);

                    this[type] = tr;

                    // listen for the desired events
                    tr.StartListening();
                }

                tr.AddHandler(handler, propertyName);
            }
        }

        // Remove a handler to the named property (empty means "any property")
        private void PrivateRemoveHandler(Type type, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            Debug.Assert(handler != null && type != null && propertyName != null,
                "Handler, type, and propertyName of event cannot be null");

            using (WriteLock)
            {
                TypeRecord tr = (TypeRecord)this[type];

                if (tr != null)
                {
                    tr.RemoveHandler(handler, propertyName);

                    if (tr.IsEmpty)
                    {
                        tr.StopListening();
                        Remove(tr.Type);
                    }
                }
            }
        }


        // event handler for PropertyChanged event
        private void OnStaticPropertyChanged(TypeRecord typeRecord, PropertyChangedEventArgs args)
        {
            ListenerList list;

            // get the list of listeners
            using (ReadLock)
            {
                list = typeRecord.GetListenerList(args.PropertyName);

                // mark the list "in use", even outside the read lock,
                // so that any writers will know not to modify it (they'll
                // modify a clone intead).
                list.BeginUse();
            }

            // deliver the event, being sure to undo the effect of BeginUse().
            try
            {
                DeliverEventToList(null, args, list);
            }
            finally
            {
                list.EndUse();
            }

            // if we calculated an AllListeners list, we should now try to store
            // it in the dictionary so it can be used in the future.  This must be
            // done under a WriteLock - which is why we didn't do it immediately.
            if (list == typeRecord.ProposedAllListenersList)
            {
                using (WriteLock)
                {
                    typeRecord.StoreAllListenersList((ListenerList<PropertyChangedEventArgs>)list);
                }
            }
        }

#if WeakEventTelemetry
        void LogAllocationRelay(Type type, int count, int bytes)
        {
            LogAllocation(type, count, bytes);
        }
#endif

        #endregion Private Methods

        static readonly string AllListenersKey = "<All Listeners>"; // not a legal property name
        static readonly string StaticPropertyChanged = "StaticPropertyChanged";

        #region TypeRecord

        class TypeRecord
        {
            public TypeRecord(Type type, StaticPropertyChangedEventManager manager)
            {
                _type = type;
                _manager = manager;
                _dict = new HybridDictionary(true);
            }

            public Type Type { get { return _type; } }
            public bool IsEmpty { get { return (_dict.Count == 0); } }
            public ListenerList ProposedAllListenersList { get { return _proposedAllListenersList; } }

            static MethodInfo OnStaticPropertyChangedMethodInfo
            {
                get
                {
                    return typeof(TypeRecord).GetMethod("OnStaticPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            }

            public void StartListening()
            {
                EventInfo spcEvent = _type.GetEvent(StaticPropertyChanged, BindingFlags.Public | BindingFlags.Static);
                if (spcEvent != null)
                {
                    Delegate d = Delegate.CreateDelegate(spcEvent.EventHandlerType, this, OnStaticPropertyChangedMethodInfo);
                    spcEvent.AddEventHandler(null, d);
                }
            }

            public void StopListening()
            {
                EventInfo spcEvent = _type.GetEvent(StaticPropertyChanged, BindingFlags.Public | BindingFlags.Static);
                if (spcEvent != null)
                {
                    Delegate d = Delegate.CreateDelegate(spcEvent.EventHandlerType, this, OnStaticPropertyChangedMethodInfo);
                    spcEvent.RemoveEventHandler(null, d);
                }
            }

            void OnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                HandleStaticPropertyChanged(e);
            }

            public void HandleStaticPropertyChanged(PropertyChangedEventArgs e)
            {
                _manager.OnStaticPropertyChanged(this, e);
            }

            public void AddHandler(EventHandler<PropertyChangedEventArgs> handler, string propertyName)
            {
                PropertyRecord pr = (PropertyRecord)_dict[propertyName];

                if (pr == null)
                {
                    // no entry in the dictionary - add a new one
                    pr = new PropertyRecord(propertyName, this);
                    _dict[propertyName] = pr;
                    pr.StartListening(_type);
                }

                pr.AddHandler(handler);

                // invalidate list of all listeners
                _dict.Remove(AllListenersKey);
                _proposedAllListenersList = null;

                // schedule a cleanup pass
                _manager.ScheduleCleanup();
            }

            public void RemoveHandler(EventHandler<PropertyChangedEventArgs> handler, string propertyName)
            {
                PropertyRecord pr = (PropertyRecord)_dict[propertyName];

                if (pr != null)
                {
                    pr.RemoveHandler(handler);

                    if (pr.IsEmpty)
                    {
                        _dict.Remove(propertyName);
                    }

                    // invalidate list of all listeners
                    _dict.Remove(AllListenersKey);
                    _proposedAllListenersList = null;
                }
            }

            public ListenerList GetListenerList(string propertyName)
            {
                ListenerList list;

                if (!String.IsNullOrEmpty(propertyName))
                {
                    // source has changed a particular property.  Notify targets
                    // who are listening either for this property or for all properties.
                    PropertyRecord pr = (PropertyRecord)_dict[propertyName];
                    ListenerList<PropertyChangedEventArgs> listeners = (pr == null) ? null : pr.List;
                    PropertyRecord genericRecord = (PropertyRecord)_dict[String.Empty];
                    ListenerList<PropertyChangedEventArgs> genericListeners = (genericRecord == null) ? null : genericRecord.List;

                    if (genericListeners == null)
                    {
                        if (listeners != null)
                        {
                            list = listeners;           // only specific listeners
                        }
                        else
                        {
                            list = ListenerList.Empty;  // no listeners at all
                        }
                    }
                    else
                    {
                        if (listeners != null)
                        {
                            // there are both specific and generic listeners -
                            // combine the two lists.
                            list = new ListenerList<PropertyChangedEventArgs>(listeners.Count + genericListeners.Count);
                            for (int i = 0, n = listeners.Count; i < n; ++i)
                                list.Add(listeners[i]);
                            for (int i = 0, n = genericListeners.Count; i < n; ++i)
                                list.Add(genericListeners[i]);
                        }
                        else
                        {
                            list = genericListeners;    // only generic listeners
                        }
                    }
                }
                else
                {
                    // source has changed all properties.  Notify all targets.
                    // Use previously calculated combined list, if available.
                    PropertyRecord pr = (PropertyRecord)_dict[AllListenersKey];
                    ListenerList<PropertyChangedEventArgs> pcList = (pr == null) ? null : pr.List;

                    if (pcList == null)
                    {
                        // make one pass to compute the size of the combined list.
                        // This avoids expensive reallocations.
                        int size = 0;
                        foreach (DictionaryEntry de in _dict)
                        {
                            Debug.Assert((String)de.Key != AllListenersKey, "special key should not appear");
                            size += ((PropertyRecord)de.Value).List.Count;
                        }

                        // create the combined list
                        pcList = new ListenerList<PropertyChangedEventArgs>(size);

                        // fill in the combined list
                        foreach (DictionaryEntry de in _dict)
                        {
                            ListenerList listeners = ((PropertyRecord)de.Value).List;
                            for (int i = 0, n = listeners.Count; i < n; ++i)
                            {
                                pcList.Add(listeners.GetListener(i));
                            }
                        }

                        // save the result for future use (see below)
                        _proposedAllListenersList = pcList;
                    }

                    list = pcList;
                }

                return list;
            }

            public void StoreAllListenersList(ListenerList<PropertyChangedEventArgs> list)
            {
                // test again, in case another thread changed _proposedAllListersList.
                if (_proposedAllListenersList == list)
                {
                    _dict[AllListenersKey] = new PropertyRecord(AllListenersKey, this, list);

                    _proposedAllListenersList = null;
                }

                // Another thread could have changed _proposedAllListersList
                // since we set it (earlier in this method), either
                // because it calculated a new one while handling a PropertyChanged(""),
                // or because it added/removed/purged a listener.
                // In that case, we will simply abandon our proposed list and we'll
                // have to compute it again the next time.  But that only happens
                // if there's thread contention.  It's not worth doing something
                // more complicated just for that case.
            }

            public bool Purge(bool purgeAll)
            {
                bool foundDirt = false;

                if (!purgeAll)
                {
                    if (!MS.Internal.BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
                    {
                        // copy the keys into a separate array, so that later on
                        // we can change the dictionary while iterating over the keys
                        ICollection ic = _dict.Keys;
                        String[] keys = new String[ic.Count];
                        ic.CopyTo(keys, 0);

                        for (int i = keys.Length - 1; i >= 0; --i)
                        {
                            if (keys[i] == AllListenersKey)
                                continue;       // ignore the special entry for now

                            // for each key, remove dead entries in its list
                            PropertyRecord pr = (PropertyRecord)_dict[keys[i]];
                            if (pr.Purge())
                            {
                                foundDirt = true;
                            }

                            // if there are no more entries, remove the key
                            if (pr.IsEmpty)
                            {
                                pr.StopListening(_type);
                                _dict.Remove(keys[i]);
                            }
                        }

#if WeakEventTelemetry
                        _manager.LogAllocationRelay(ic.GetType(), 1, 12);                   // dict.Keys - Hashtable+KeyCollection
                        _manager.LogAllocationRelay(typeof(String[]), 1, 12+ic.Count*4);    // keys
#endif
                    }
                    else
                    {
                        Debug.Assert(_toRemove.Count == 0, "to-remove list should be empty");

                        // enumerate the dictionary using IDE explicitly rather than
                        // foreach, to avoid allocating temporary DictionaryEntry objects
                        IDictionaryEnumerator ide = _dict.GetEnumerator() as IDictionaryEnumerator;
                        while (ide.MoveNext())
                        {
                            String key = (String)ide.Key;
                            if (key == AllListenersKey)
                                continue;       // ignore the special entry for now

                            // for each key, remove dead entries in its list
                            PropertyRecord pr = (PropertyRecord)ide.Value;
                            if (pr.Purge())
                            {
                                foundDirt = true;
                            }

                            // if there are no more entries, remove the key
                            if (pr.IsEmpty)
                            {
                                pr.StopListening(_type);
                                _toRemove.Add(key);
                            }
                        }

                        // do the actual removal (outside the dictionary iteration)
                        if (_toRemove.Count > 0)
                        {
                            foreach (String key in _toRemove)
                            {
                                _dict.Remove(key);
                            }
                            _toRemove.Clear();
                            _toRemove.TrimExcess();
                        }

#if WeakEventTelemetry
                        Type enumeratorType = ide.GetType();
                        if (enumeratorType.Name.IndexOf("NodeEnumerator") >= 0)
                        {
                            _manager.LogAllocationRelay(enumeratorType, 1, 24); // ListDictionary+NodeEnumerator
                        }
                        else
                        {
                            _manager.LogAllocationRelay(enumeratorType, 1, 36); // Hashtable+HashtableEnumerator
                        }
#endif
                    }

                    if (foundDirt)
                    {
                        // if any entries were purged, invalidate the special entry
                        _dict.Remove(AllListenersKey);
                        _proposedAllListenersList = null;
                    }

                    if (IsEmpty)
                    {
                        StopListening();
                    }
                }
                else
                {
                    // stop listening.  List cleanup is handled by Purge()
                    foundDirt = true;
                    StopListening();

                    foreach (DictionaryEntry de in _dict)
                    {
                        PropertyRecord pr = (PropertyRecord)de.Value;
                        pr.StopListening(_type);
                    }
                }

                return foundDirt;
            }

            Type _type;                 // the type whose static property-changes we're listening to
            HybridDictionary _dict;     // Property-name -> PropertyRecord
            StaticPropertyChangedEventManager _manager; // owner
            ListenerList<PropertyChangedEventArgs> _proposedAllListenersList;
            List<String> _toRemove = new List<String>();
        }

        #endregion TypeRecord

        #region PropertyRecord

        class PropertyRecord
        {
            public PropertyRecord(string propertyName, TypeRecord owner)
                : this(propertyName, owner, new ListenerList<PropertyChangedEventArgs>())
            {
            }

            public PropertyRecord(string propertyName, TypeRecord owner, ListenerList<PropertyChangedEventArgs> list)
            {
                _propertyName = propertyName;
                _typeRecord = owner;
                _list = list;
            }

            public bool IsEmpty { get { return _list.IsEmpty; } }
            public ListenerList<PropertyChangedEventArgs> List { get { return _list; } }

            static MethodInfo OnStaticPropertyChangedMethodInfo
            {
                get
                {
                    return typeof(PropertyRecord).GetMethod("OnStaticPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            }

            public void StartListening(Type type)
            {
                string eventName = _propertyName + "Changed";
                EventInfo eventInfo = type.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
                if (eventInfo != null)
                {
                    Delegate d = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, OnStaticPropertyChangedMethodInfo);
                    eventInfo.AddEventHandler(null, d);
                }
            }

            public void StopListening(Type type)
            {
                string eventName = _propertyName + "Changed";
                EventInfo eventInfo = type.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
                if (eventInfo != null)
                {
                    Delegate d = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, OnStaticPropertyChangedMethodInfo);
                    eventInfo.RemoveEventHandler(null, d);
                }
            }

            void OnStaticPropertyChanged(object sender, EventArgs e)
            {
                _typeRecord.HandleStaticPropertyChanged(new PropertyChangedEventArgs(_propertyName));
            }

            public void AddHandler(EventHandler<PropertyChangedEventArgs> handler)
            {
                // make sure list is ready for writing
                ListenerList list = _list;
                if (ListenerList.PrepareForWriting(ref list))
                    _list = (ListenerList<PropertyChangedEventArgs>)list;

                // add a listener to the list
                _list.AddHandler(handler);
            }

            public void RemoveHandler(EventHandler<PropertyChangedEventArgs> handler)
            {
                // make sure list is ready for writing
                ListenerList list = _list;
                if (ListenerList.PrepareForWriting(ref list))
                    _list = (ListenerList<PropertyChangedEventArgs>)list;

                // remove a listener from the list
                _list.RemoveHandler(handler);
            }

            public bool Purge()
            {
                ListenerList list = _list;
                if (ListenerList.PrepareForWriting(ref list))
                    _list = (ListenerList<PropertyChangedEventArgs>)list;

                return _list.Purge();
            }

            string _propertyName;
            ListenerList<PropertyChangedEventArgs> _list;
            TypeRecord _typeRecord;
        }

        #endregion PropertyRecord
    }
}

