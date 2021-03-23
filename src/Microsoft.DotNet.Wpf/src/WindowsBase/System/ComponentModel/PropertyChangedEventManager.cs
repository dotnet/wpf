// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manager for the PropertyChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Collections;       // ICollection
using System.Collections.Generic; // List<T>
using System.Collections.Specialized;   // HybridDictionary
using System.ComponentModel;    // INotifyPropertyChanged
using System.Diagnostics;       // Debug
using System.Reflection;        // MethodInfo
using System.Windows;           // WeakEventManager
using MS.Internal;              // BaseAppContextSwitches
using MS.Internal.WindowsBase;  // SR

namespace System.ComponentModel
{
    /// <summary>
    /// Manager for the INotifyPropertyChanged.PropertyChanged event.
    /// </summary>
    public class PropertyChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private PropertyChangedEventManager()
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
        public static void AddListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.PrivateAddListener(source, listener, propertyName);
        }

        /// <summary>
        /// Remove a listener to the given source's event.
        /// </summary>
        public static void RemoveListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            /* for app-compat, allow RemoveListener(null, x) - it's a no-op
            if (source == null)
                throw new ArgumentNullException("source");
            */
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.PrivateRemoveListener(source, listener, propertyName);
        }

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(INotifyPropertyChanged source, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.PrivateAddHandler(source, handler, propertyName);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(INotifyPropertyChanged source, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.PrivateRemoveHandler(source, handler, propertyName);
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
            INotifyPropertyChanged typedSource = (INotifyPropertyChanged)source;
            typedSource.PropertyChanged += new PropertyChangedEventHandler(OnPropertyChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            INotifyPropertyChanged typedSource = (INotifyPropertyChanged)source;
            typedSource.PropertyChanged -= new PropertyChangedEventHandler(OnPropertyChanged);
        }

        /// <summary>
        /// Remove dead entries from the data for the given source.   Returns true if
        /// some entries were actually removed.
        /// </summary>
        protected override bool Purge(object source, object data, bool purgeAll)
        {
            bool foundDirt = false;

            if (!purgeAll)
            {
                HybridDictionary dict = (HybridDictionary)data;
                int ignoredKeys = 0;

                if (!BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
                {
                    // copy the keys into a separate array, so that later on
                    // we can change the dictionary while iterating over the keys
                    ICollection ic = dict.Keys;
                    String[] keys = new String[ic.Count];
                    ic.CopyTo(keys, 0);

                    for (int i=keys.Length-1; i>=0; --i)
                    {
                        if (keys[i] == AllListenersKey)
                        {
                            ++ignoredKeys;
                            continue;       // ignore the special entry for now
                        }

                        // for each key, remove dead entries in its list
                        bool removeList = /*purgeAll || purgeAll is always false*/ source == null;

                        if (!removeList)
                        {
                            ListenerList list = (ListenerList)dict[keys[i]];

                            if (ListenerList.PrepareForWriting(ref list))
                                dict[keys[i]] = list;

                            if (list.Purge())
                                foundDirt = true;

                            removeList = (list.IsEmpty);
                        }

                        // if there are no more entries, remove the key
                        if (removeList)
                        {
                            dict.Remove(keys[i]);
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

                    // If an "in-use" list is changed, we will re-install its clone
                    // back into the dictionary.  Doing this inside the loop
                    // causes an exception "collection was modified after the enumerator
                    // was instantiated", so instead just record
                    // what to do and do the actual work after the loop.
                    // This is a rare case - it only arises if a PropertyChanged event
                    // handler calls (indirectly) into the cleanup code - so allocate
                    // the temporary memory lazily on the stack.
                    HybridDictionary toInstall = null;

                    // enumerate the dictionary using IDE explicitly rather than
                    // foreach, to avoid allocating temporary DictionaryEntry objects
                    IDictionaryEnumerator ide = dict.GetEnumerator() as IDictionaryEnumerator;
                    while (ide.MoveNext())
                    {
                        String key = (String)ide.Key;
                        if (key == AllListenersKey)
                        {
                            ++ignoredKeys;
                            continue;       // ignore the special entry for now
                        }

                        // for each key, remove dead entries in its list
                        bool removeList = /*purgeAll || purgeAll is always false*/ source == null;

                        if (!removeList)
                        {
                            ListenerList list = (ListenerList)ide.Value;

                            bool inUse = ListenerList.PrepareForWriting(ref list);
                            bool isChanged = false;

                            if (list.Purge())
                            {
                                isChanged = true;
                                foundDirt = true;
                            }

                            // if a cloned list changed, remember the details
                            // so that the clone can be installed back into the
                            // dictionary outside the iteration
                            if (/*!removeList && !removeList is always true*/ inUse && isChanged)
                            {
                                if (toInstall == null)
                                {
                                    // lazy allocation
                                    toInstall = new HybridDictionary();
                                }

                                toInstall[key] = list;
                            }

                            removeList = (list.IsEmpty);
                        }

                        // if there are no more entries, remove the key
                        if (removeList)
                        {
                            _toRemove.Add(key);
                        }
                    }

                    // do the actual removal (outside the dictionary iteration)
                    if (_toRemove.Count > 0)
                    {
                        foreach (String key in _toRemove)
                        {
                            dict.Remove(key);
                        }
                        _toRemove.Clear();
                        _toRemove.TrimExcess();
                    }

                    // do the actual re-install of "in-use" lists that changed
                    if (toInstall != null)
                    {
                        IDictionaryEnumerator installDE = toInstall.GetEnumerator() as IDictionaryEnumerator;
                        while (installDE.MoveNext())
                        {
                            String key = (String)installDE.Key;
                            ListenerList list = (ListenerList)installDE.Value;
                            dict[key] = list;
                        }
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

                if (dict.Count == ignoredKeys)
                {
                    // if there are no more listeners at all, remove the entry from
                    // the main table, and prepare to stop listening
                    purgeAll = true;
                    if (source != null)     // source may have been GC'd
                    {
                        this.Remove(source);
                    }
                }
                else if (foundDirt)
                {
                    // if any entries were purged, invalidate the special entry
                    dict.Remove(AllListenersKey);
                    _proposedAllListenersList = null;
                }
            }

            if (purgeAll)
            {
                // stop listening.  List cleanup is handled by Purge()
                if (source != null) // source may have been GC'd
                {
                    StopListening(source);
                }
                foundDirt = true;
            }

            return foundDirt;
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static PropertyChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(PropertyChangedEventManager);
                PropertyChangedEventManager manager = (PropertyChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new PropertyChangedEventManager();
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
        private void PrivateAddListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            Debug.Assert(listener != null && source != null && propertyName != null,
                "Listener, source, and propertyName of event cannot be null");

            AddListener(source, propertyName, listener, null);
        }

        // Remove a listener to the named property (empty means "any property")
        private void PrivateRemoveListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            Debug.Assert(listener != null && source != null && propertyName != null,
                "Listener, source, and propertyName of event cannot be null");

            RemoveListener(source, propertyName, listener, null);
        }

        // Add a handler for the named property (empty means "any property")
        private void PrivateAddHandler(INotifyPropertyChanged source, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            AddListener(source, propertyName, null, handler);
        }

        // Remove a handler for the named property (empty means "any property")
        private void PrivateRemoveHandler(INotifyPropertyChanged source, EventHandler<PropertyChangedEventArgs> handler, string propertyName)
        {
            RemoveListener(source, propertyName, null, handler);
        }

        private void AddListener(INotifyPropertyChanged source, string propertyName, IWeakEventListener listener, EventHandler<PropertyChangedEventArgs> handler)
        {
            using (WriteLock)
            {
                HybridDictionary dict = (HybridDictionary)this[source];

                if (dict == null)
                {
                    // no entry in the hashtable - add a new one
                    dict = new HybridDictionary(true /* case insensitive */);

                    this[source] = dict;

                    // listen for the desired events
                    StartListening(source);
                }

                ListenerList list = (ListenerList)dict[propertyName];

                if (list == null)
                {
                    // no entry in the dictionary - add a new one
                    list = new ListenerList<PropertyChangedEventArgs>();

                    dict[propertyName] = list;
                }

                // make sure list is ready for writing
                if (ListenerList.PrepareForWriting(ref list))
                {
                    dict[propertyName] = list;
                }

                // add a listener to the list
                if (handler != null)
                {
                    ListenerList<PropertyChangedEventArgs> hlist = (ListenerList<PropertyChangedEventArgs>)list;
                    hlist.AddHandler(handler);
                }
                else
                {
                    list.Add(listener);
                }

                dict.Remove(AllListenersKey);   // invalidate list of all listeners
                _proposedAllListenersList = null;

                // schedule a cleanup pass
                ScheduleCleanup();
            }
        }

        private void RemoveListener(INotifyPropertyChanged source, string propertyName, IWeakEventListener listener, EventHandler<PropertyChangedEventArgs> handler)
        {
            using (WriteLock)
            {
                HybridDictionary dict = (HybridDictionary)this[source];

                if (dict != null)
                {
                    ListenerList list = (ListenerList)dict[propertyName];

                    if (list != null)
                    {
                        // make sure list is ready for writing
                        if (ListenerList.PrepareForWriting(ref list))
                        {
                            dict[propertyName] = list;
                        }

                        // remove a listener from the list
                        if (handler != null)
                        {
                            ListenerList<PropertyChangedEventArgs> hlist = (ListenerList<PropertyChangedEventArgs>)list;
                            hlist.RemoveHandler(handler);
                        }
                        else
                        {
                            list.Remove(listener);
                        }

                        // when the last listener goes away, remove the list
                        if (list.IsEmpty)
                        {
                            dict.Remove(propertyName);
                        }
                    }

                    if (dict.Count == 0)
                    {
                        StopListening(source);

                        Remove(source);
                    }

                    dict.Remove(AllListenersKey);   // invalidate list of all listeners
                    _proposedAllListenersList = null;
                }
            }
        }

        // event handler for PropertyChanged event
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            ListenerList list;
            string propertyName = args.PropertyName;

            // get the list of listeners
            using (ReadLock)
            {
                // look up the list of listeners
                HybridDictionary dict = (HybridDictionary)this[sender];

                if (dict == null)
                {
                    // this can happen when the last listener stops listening, but the
                    // source raises the event on another thread after the dictionary
                    // has been removed
                    list = ListenerList.Empty;
                }
                else if (!String.IsNullOrEmpty(propertyName))
                {
                    // source has changed a particular property.  Notify targets
                    // who are listening either for this property or for all properties.
                    ListenerList<PropertyChangedEventArgs> listeners = (ListenerList<PropertyChangedEventArgs>)dict[propertyName];
                    ListenerList<PropertyChangedEventArgs> genericListeners = (ListenerList<PropertyChangedEventArgs>)dict[String.Empty];

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
                            for (int i=0, n=listeners.Count; i<n; ++i)
                                list.Add(listeners.GetListener(i));
                            for (int i=0, n=genericListeners.Count; i<n; ++i)
                                list.Add(genericListeners.GetListener(i));
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
                    list = (ListenerList)dict[AllListenersKey];

                    if (list == null)
                    {
                        // make one pass to compute the size of the combined list.
                        // This avoids expensive reallocations.
                        int size = 0;
                        foreach (DictionaryEntry de in dict)
                        {
                            Debug.Assert((String)de.Key != AllListenersKey, "special key should not appear");
                            size += ((ListenerList)de.Value).Count;
                        }

                        // create the combined list
                        list = new ListenerList<PropertyChangedEventArgs>(size);

                        // fill in the combined list
                        foreach (DictionaryEntry de in dict)
                        {
                            ListenerList listeners = ((ListenerList)de.Value);
                            for (int i=0, n=listeners.Count;  i<n;  ++i)
                            {
                                list.Add(listeners.GetListener(i));
                            }
                        }

                        // save the result for future use (see below)
                        _proposedAllListenersList = list;
                    }
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

            // if we calculated an AllListeners list, we should now try to store
            // it in the dictionary so it can be used in the future.  This must be
            // done under a WriteLock - which is why we didn't do it immediately.
            if (_proposedAllListenersList == list)
            {
                using (WriteLock)
                {
                    // test again, in case another thread changed _proposedAllListersList.
                    if (_proposedAllListenersList == list)
                    {
                        HybridDictionary dict = (HybridDictionary)this[sender];
                        if (dict != null)
                        {
                            dict[AllListenersKey] = list;
                        }

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
            }
        }

        #endregion Private Methods

        ListenerList _proposedAllListenersList;
        List<String> _toRemove = new List<String>();
        static readonly string AllListenersKey = "<All Listeners>"; // not a legal property name
    }
}

