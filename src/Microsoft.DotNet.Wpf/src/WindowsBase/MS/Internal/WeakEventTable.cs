// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Storage for the "weak event listener" pattern.
//              See WeakEventManager.cs for an overview.
//

using System;
using System.Diagnostics;           // Debug
using System.Collections;           // Hashtable
using System.Collections.Generic;   // List<T>
using System.Collections.Specialized; // HybridDictionary
using System.Runtime.CompilerServices;  // RuntimeHelpers
using System.Security;              // 
using System.Threading;             // [ThreadStatic]
using System.Windows;               // WeakEventManager
using System.Windows.Threading;     // DispatcherObject
using MS.Utility;                   // FrugalList

namespace MS.Internal
{
    /// <summary>
    /// This class manages the correspondence between event types and
    /// event managers, in support of the "weak event listener" pattern.
    /// It also stores data on behalf of the managers;  a manager can store
    /// data of its own choosing, indexed by the pair (manager, source).
    /// </summary>
    internal class WeakEventTable : DispatcherObject
    {
        #region Constructors

        //
        //  Constructors
        //

        /// <summary>
        /// Create a new instance of WeakEventTable.
        /// </summary>
        private WeakEventTable()
        {
            WeakEventTableShutDownListener listener = new WeakEventTableShutDownListener(this);
            _cleanupHelper = new CleanupHelper(DoCleanup);
        }

        #endregion Constructors

        #region Internal Properties

        //
        //  Internal Properties
        //

        /// <summary>
        /// Return the WeakEventTable for the current thread
        /// </summary>
        internal static WeakEventTable CurrentWeakEventTable
        {
            get
            {
                // _currentTable is [ThreadStatic], so there's one per thread
                if (_currentTable == null)
                {
                    _currentTable = new WeakEventTable();
                }

                return _currentTable;
            }
        }

        /// <summary>
        /// Take a read-lock on the table, and return the IDisposable.
        /// Queries to the table should occur within a
        /// "using (Table.ReadLock) { ... }" clause, except for queries
        /// that are already within a write lock.
        /// </summary>
        internal IDisposable ReadLock
        {
            get
            {
#if WeakEventTelemetry
                ++ _readCount;
#endif
                return _lock.ReadLock;
            }
        }

        /// <summary>
        /// Take a write-lock on the table, and return the IDisposable.
        /// All modifications to the table should occur within a
        /// "using (Table.WriteLock) { ... }" clause.
        /// </summary>
        internal IDisposable WriteLock
        {
            get
            {
#if WeakEventTelemetry
                ++ _writeCount;
#endif
                return _lock.WriteLock;
            }
        }

        /// <summary>
        /// Get or set the manager instance for the given type.
        /// </summary>
        internal WeakEventManager this[Type managerType]
        {
            get { return (WeakEventManager)_managerTable[managerType]; }
            set { _managerTable[managerType] = value; }
        }

        /// <summary>
        /// Get or set the manager instance for the given event.
        /// </summary>
        internal WeakEventManager this[Type eventSourceType, string eventName]
        {
            get
            {
                EventNameKey key = new EventNameKey(eventSourceType, eventName);
                return (WeakEventManager)_eventNameTable[key];
            }

            set
            {
                EventNameKey key = new EventNameKey(eventSourceType, eventName);
                _eventNameTable[key] = value;
            }
        }

        /// <summary>
        /// Get or set the data stored by the given manager for the given source.
        /// </summary>
        internal object this[WeakEventManager manager, object source]
        {
            get
            {
                EventKey key = new EventKey(manager, source);
                object result = _dataTable[key];
                return result;
            }

            set
            {
                EventKey key = new EventKey(manager, source, true);
                _dataTable[key] = value;
            }
        }

        /// <summary>
        /// Indicates whether cleanup is enabled.
        /// </summary>
        /// <remarks>
        /// Normally cleanup is always enabled, but a perf test environment might
        /// want to disable cleanup so that it doesn't interfere with the real
        /// perf measurements.
        /// </remarks>
        internal bool IsCleanupEnabled
        {
            get { return _cleanupEnabled; }
            set { _cleanupEnabled = value; }
        }

        #endregion Internal Properties

        #region Internal Methods

        //
        //  Internal Methods
        //

        /// <summary>
        /// Remove the data for the given manager and source.
        /// </summary>
        internal void Remove(WeakEventManager manager, object source)
        {
            EventKey key = new EventKey(manager, source);
            if (!_inPurge)
            {
                _dataTable.Remove(key);
            }
            else
            {
                _toRemove.Add(key);
            }
        }

        /// <summary>
        /// Schedule a cleanup pass.  This can be called from any thread.
        /// </summary>
        internal void ScheduleCleanup()
        {
            if (!BaseAppContextSwitches.EnableCleanupSchedulingImprovements)
            {
                // only the first request after a previous cleanup should schedule real work
                if (Interlocked.Increment(ref _cleanupRequests) == 1)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new DispatcherOperationCallback(CleanupOperation), null);
                }
            }
            else
            {
                _cleanupHelper.ScheduleCleanup();
            }
        }

        /// <summary>
        /// Perform a cleanup pass.
        /// </summary>
        internal static bool Cleanup()
        {
            if (!BaseAppContextSwitches.EnableCleanupSchedulingImprovements)
            {
                return CurrentWeakEventTable.Purge(false);
            }
            else
            {
                return CurrentWeakEventTable._cleanupHelper.DoCleanup(forceCleanup:true);
            }
        }

        bool DoCleanup(bool forceCleanup)
        {
            if (IsCleanupEnabled || forceCleanup)
            {
                return Purge(false);
            }
            else
            {
                return false;
            }
        }

#if WeakEventTelemetry
        internal void LogAllocation(Type type, int count, int bytes)
        {
            LogAllocation(_allocations, type, count, bytes);
            LogAllocation(_allocations, typeof(WeakEventLogger), count, bytes);

            if (bytes/count >= LOH_Threshold)
            {
                LogAllocation(_LOHallocations, type, count, bytes);
                LogAllocation(_LOHallocations, typeof(WeakEventLogger), count, bytes);
            }
        }

        void LogAllocation(Dictionary<Type,AllocationRecord> dict, Type type, int count, int bytes)
        {
            AllocationRecord record;
            if (!dict.TryGetValue(type, out record))
            {
                record = new AllocationRecord();
                dict.Add(type, record);
            }

            record.Count += count;
            record.Bytes += bytes;
        }
#endif

        #endregion Internal Methods

        #region Private Methods

        //
        //  Private Methods
        //

        // run a cleanup pass
        private object CleanupOperation(object arg)
        {
            // allow new requests, even if cleanup is disabled
            Interlocked.Exchange(ref _cleanupRequests, 0);

            if (IsCleanupEnabled)
            {
                Purge(false);
            }

            return null;
        }

        // remove dead entries.  When purgeAll is true, remove all entries.
        private bool Purge(bool purgeAll)
        {
            bool foundDirt = false;

            using (this.WriteLock)
            {
#if WeakEventTelemetry
                WeakEventLogger.LogSnapshot(this, "+Purge");
#endif

                if (!BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
                {
                    // copy the keys into a separate array, so that later on
                    // we can change the table while iterating over the keys
                    ICollection ic = _dataTable.Keys;
                    EventKey[] keys = new EventKey[ic.Count];
                    ic.CopyTo(keys, 0);

                    for (int i=keys.Length-1; i>=0; --i)
                    {
                        object data = _dataTable[keys[i]];
                        // a purge earlier in the loop may have removed keys[i],
                        // in which case there's nothing more to do
                        if (data != null)
                        {
                            object source = keys[i].Source;
                            foundDirt |= keys[i].Manager.PurgeInternal(source, data, purgeAll);

                            // if source has been GC'd, remove its data
                            if (!purgeAll && source == null)
                            {
                                _dataTable.Remove(keys[i]);
                            }
                        }
                    }
#if WeakEventTelemetry
                    LogAllocation(ic.GetType(), 1, 12);                     // _dataTable.Keys - Hashtable+KeyCollection
                    LogAllocation(typeof(EventKey[]), 1, 12+ic.Count*12);   // keys
                    LogAllocation(typeof(EventKey), ic.Count, 8+12);        // box(key)
                    LogAllocation(typeof(ReaderWriterLockWrapper), 1, 12);  // actually the RWLW+AutoWriterRelease from WriteLock
                    LogAllocation(typeof(Action), 2, 32);                   // anonymous delegates in RWLW
#endif
                }
                else
                {
                    Debug.Assert(_toRemove.Count == 0, "to-remove list should be empty");
                    _inPurge = true;

                    // enumerate the dictionary using IDE explicitly rather than
                    // foreach, to avoid allocating temporary DictionaryEntry objects
                    IDictionaryEnumerator ide = _dataTable.GetEnumerator() as IDictionaryEnumerator;
                    while (ide.MoveNext())
                    {
                        EventKey key = (EventKey)ide.Key;
                        object source = key.Source;
                        foundDirt |= key.Manager.PurgeInternal(source, ide.Value, purgeAll);

                        // if source has been GC'd, remove its data
                        if (!purgeAll && source == null)
                        {
                            _toRemove.Add(key);
                        }
                    }

#if WeakEventTelemetry
                    LogAllocation(ide.GetType(), 1, 36);                    // Hashtable+HashtableEnumerator
#endif
                    _inPurge = false;
                }

                if (purgeAll)
                {
                    _managerTable.Clear();
                    _dataTable.Clear();
                }
                else if (_toRemove.Count > 0)
                {
                    foreach (EventKey key in _toRemove)
                    {
                        _dataTable.Remove(key);
                    }
                    _toRemove.Clear();
                    _toRemove.TrimExcess();
                }

#if WeakEventTelemetry
                ++_purgeCount;
                if (!foundDirt) ++_purgeNoops;
                WeakEventLogger.LogSnapshot(this, "-Purge");
#endif
            }

            return foundDirt;
        }


        // do the final cleanup when the Dispatcher or AppDomain is shut down
        private void OnShutDown()
        {
            if (CheckAccess())
            {
                Purge(true);

                // remove the table from thread storage
                _currentTable = null;
            }
            else
            {
                // if we're on the wrong thread, try asking the right thread
                // to do the job.  (DomainUnload arrives on finalizer thread)
                bool succeeded = false;

                // In some cases, the extra delay from invocation can push applications with a race condition
                // at shutdown into constant crashing.  Due to this, respect a compat flag that allows these
                // to skip the invocation.  This causes us to do a partial cleanup (see the Purge call below)
                // but avoids the significant timing changes that were adversely affecting the application.

                // If the dispatcher has already shut down, the Invoke
                // will throw an exception, so don't even bother trying.  This can
                // happen if the app does more work after dispatcher shutdown, creating
                // a second WeakEventTable for the same thread.

                // Future idea: 
                // Handle this situation further upstream - there's no sense
                // doing any work related to weak events on a thread whose dispatcher
                // has already shut down.  All calls to Add/RemoveHandler etc.
                // should do nothing.
                if (!BaseAppContextSwitches.DoNotInvokeInWeakEventTableShutdownListener &&
                    !Dispatcher.HasShutdownFinished)
                {
                    try
                    {
                        Dispatcher.Invoke((Action)OnShutDown, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(300));
                        succeeded = true;
                    }
                    catch (Exception ex) when (!CriticalExceptions.IsCriticalException(ex))
                    {
                        // Invoke can fail due to
                        //  TimeoutException - the 300ms timeout expired
                        //  TaskCanceledException - underlying Task didn't complete
                        //  <other non-critical exception> - defense-in-depth
                        // These shouldn't crash the app or halt the shutdown processing.
                        // Instead, swallow the exception, but fall back to the
                        // "wrong thread" path (below).
                    }
                }

                // if that didn't work (because Dispatcher was busy or not pumping),
                // do the work on the wrong thread, but don't touch thread-statics.
                // This won't do everything, but it will do enough to support
                // some useful scenarios (such as DevDiv Bugs 121070).
                if (!succeeded)
                {
                    Purge(true);
                }
            }
        }

        #endregion Private Methods

        #region Private Fields

        //
        //  Private Fields
        //

        private Hashtable _managerTable = new Hashtable();  // maps manager type -> instance
        private Hashtable _dataTable = new Hashtable();     // maps EventKey -> data
        private Hashtable _eventNameTable = new Hashtable(); // maps <Type,name> -> manager

        ReaderWriterLockWrapper     _lock = new ReaderWriterLockWrapper();
        private int                 _cleanupRequests;
        private bool                _cleanupEnabled = true;
        private CleanupHelper       _cleanupHelper;
        private bool                _inPurge;
        private List<EventKey>      _toRemove = new List<EventKey>();

#if WeakEventTelemetry
        const int LOH_Threshold = 85000;    // per LOH docs
        int _readCount, _writeCount;
        int _purgeCount, _purgeNoops;
        Dictionary<Type,AllocationRecord> _allocations = new Dictionary<Type,AllocationRecord>();
        Dictionary<Type,AllocationRecord> _LOHallocations = new Dictionary<Type,AllocationRecord>();

        class AllocationRecord
        {
            public int Count { get; set; }
            public int Bytes { get; set; }
        }
#endif

        [ThreadStatic]
        private static WeakEventTable   _currentTable;  // one table per thread

        #endregion Private Fields

        #region WeakEventTableShutDownListener

        private sealed class WeakEventTableShutDownListener : ShutDownListener
        {
            public WeakEventTableShutDownListener(WeakEventTable target) : base(target)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                WeakEventTable table = (WeakEventTable)target;
                table.OnShutDown();
            }
        }

        #endregion WeakEventTableShutDownListener

        #region Table Keys

        // the key for the data table:  <manager, ((source)), hashcode>
        private struct EventKey
        {
            internal EventKey(WeakEventManager manager, object source, bool useWeakRef)
            {
                _manager = manager;
                _source = new WeakReference(source);
                _hashcode = unchecked(manager.GetHashCode() + RuntimeHelpers.GetHashCode(source));
            }

            internal EventKey(WeakEventManager manager, object source)
            {
                _manager = manager;
                _source = source;
                _hashcode = unchecked(manager.GetHashCode() + RuntimeHelpers.GetHashCode(source));
            }

            internal object Source
            {
                get { return ((WeakReference)_source).Target; }
            }

            internal WeakEventManager Manager
            {
                get { return _manager; }
            }

            public override int GetHashCode()
            {
#if DEBUG
                WeakReference wr = _source as WeakReference;
                object source = (wr != null) ? wr.Target : _source;
                if (source != null)
                {
                    int hashcode = unchecked(_manager.GetHashCode() + RuntimeHelpers.GetHashCode(source));
                    Debug.Assert(hashcode == _hashcode, "hashcodes disagree");
                }
#endif

                return _hashcode;
            }

            public override bool Equals(object o)
            {
                if (o is EventKey)
                {
                    WeakReference wr;
                    EventKey ek = (EventKey)o;

                    if (_manager != ek._manager || _hashcode != ek._hashcode)
                        return false;

                    wr = this._source as WeakReference;
                    object s1 = (wr != null) ? wr.Target : this._source;
                    wr = ek._source as WeakReference;
                    object s2 = (wr != null) ? wr.Target : ek._source;

                    if (s1!=null && s2!=null)
                        return (s1 == s2);
                    else
                        return (_source == ek._source);
                }
                else
                {
                    return false;
                }
            }

            public static bool operator==(EventKey key1, EventKey key2)
            {
                return key1.Equals(key2);
            }

            public static bool operator!=(EventKey key1, EventKey key2)
            {
                return !key1.Equals(key2);
            }

            WeakEventManager _manager;
            object _source;             // lookup: direct ref;  In table: WeakRef
            int _hashcode;              // cached, in case source is GC'd
        }

        // the key for the event name table:  <ownerType, eventName>
        private struct EventNameKey
        {
            public EventNameKey(Type eventSourceType, string eventName)
            {
                _eventSourceType = eventSourceType;
                _eventName = eventName;
            }

            public override int GetHashCode()
            {
                return unchecked(_eventSourceType.GetHashCode() + _eventName.GetHashCode());
            }

            public override bool Equals(object o)
            {
                if (o is EventNameKey)
                {
                    EventNameKey that = (EventNameKey)o;
                    return (this._eventSourceType == that._eventSourceType && this._eventName == that._eventName);
                }
                else
                    return false;
            }

            public static bool operator==(EventNameKey key1, EventNameKey key2)
            {
                return key1.Equals(key2);
            }

            public static bool operator!=(EventNameKey key1, EventNameKey key2)
            {
                return !key1.Equals(key2);
            }

            Type _eventSourceType;
            string _eventName;
        }

        #endregion Table Keys

        #if WeakEventTelemetry
        #region Telemetry

        static class WeakEventLogger
        {
            static System.Collections.Generic.List<Snapshot> _log = new System.Collections.Generic.List<Snapshot>();

            public static void LogSnapshot(WeakEventTable table, string title)
            {
                _log.Add(new Snapshot(title).Populate(table));
                if (_log.Count > 10000)
                {
                    _log.RemoveRange(0, 7000);
                }
            }

            class Snapshot
            {
                public int ThreadId { get; private set; }
                public DateTime Timestamp { get; private set; }
                public string Title { get; private set; }
                public int Size { get; set; }
                public int Reads { get; set; }
                public int Writes { get; set; }
                public int Purges { get; set; }
                public int PurgeNoops { get; set; }
                public int AllocationCount { get; set; }
                public int AllocationBytes { get; set; }
                public int LOHCount { get; set; }
                public int LOHBytes { get; set; }
                public System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<LoggerPair>>
                    Data { get; private set; }

                public Snapshot(string title)
                {
                    Title = title;
                    Timestamp = DateTime.Now;
                    Data = new System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<LoggerPair>>();
                }

                public Snapshot Populate(WeakEventTable table)
                {
                    ThreadId = table.Dispatcher.Thread.ManagedThreadId;
                    Size = table._dataTable.Count;
                    Reads = table._readCount;
                    Writes = table._writeCount;
                    Purges = table._purgeCount;
                    PurgeNoops = table._purgeNoops;

                    AllocationRecord record;
                    if (table._allocations.TryGetValue(typeof(WeakEventLogger), out record))
                    {
                        AllocationCount = record.Count;
                        AllocationBytes = record.Bytes;
                    }
                    if (table._LOHallocations.TryGetValue(typeof(WeakEventLogger), out record))
                    {
                        LOHCount = record.Count;
                        LOHBytes = record.Bytes;
                    }

                    foreach (EventKey key in table._dataTable.Keys)
                    {
                        object source = key.Source;
                        Type type = (source == null) ? typeof(WeakEventTable) : source.GetType();
                        System.Collections.Generic.List<LoggerPair> list;

                        if (!Data.TryGetValue(type, out list))
                        {
                            list = new System.Collections.Generic.List<LoggerPair>();
                            list.Add(new LoggerPair());
                            Data[type] = list;
                        }

                        WeakEventManager manager = key.Manager;
                        bool found = false;
                        foreach (LoggerPair pair in list)
                        {
                            if (null == pair.Manager)
                            {
                                pair.Count += 1;
                            }
                            else if (manager == pair.Manager)
                            {
                                found = true;
                                pair.Count += 1;
                                break;
                            }
                        }

                        if (!found)
                        {
                            list.Add(new LoggerPair{ Manager=manager, Count=1 });
                        }
                    }

                    return this;
                }
            }

            class LoggerPair
            {
                public WeakEventManager Manager { get; set; }
                public int Count { get; set; }
                public override string ToString() { return String.Format("{0}-{1}", Count, Manager?.GetType().Name); }
            }
        }
        #endregion Telemetry
        #endif
    }
}
