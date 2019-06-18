// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Data binding engine.
//

using System;
using System.Collections.Generic;   // Dictionary<TKey, TValue>
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Threading;
using System.Security;              // 
using System.Threading;

using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using MS.Internal.Data;
using MS.Internal;          // Invariant.Assert

namespace MS.Internal.Data
{
    internal enum TaskOps
    {
        TransferValue,
        UpdateValue,
        AttachToContext,
        VerifySourceReference,
        RaiseTargetUpdatedEvent,
    }

    internal interface IDataBindEngineClient
    {
        void TransferValue();
        void UpdateValue();
        bool AttachToContext(bool lastChance);
        void VerifySourceReference(bool lastChance);
        void OnTargetUpdated();
        DependencyObject TargetElement { get; }
    }

    internal class DataBindEngine : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Nested classes
        //
        //------------------------------------------------------

        // The task list is represented by a singly linked list of Tasks
        // connected via the Next pointer.  The variables _head and _tail
        // point to the beginning and end of the list.  The head is always
        // a dummy Task that is never used for anything else - this makes
        // the code for adding to the list simpler.
        //
        // In addition, all tasks for a particular client are linked in
        // reverse order of arrival by the PreviousForClient back pointer.
        // The heads of these lists are kept in the _mostRecentForClient
        // hashtable.  This allows rapid cancellation of all tasks pending
        // for a particular client - we only need to look at the tasks that
        // are actually affected, rather than the entire list.  This avoids
        // an O(n^2) algorithm (bug 1366032).

        private class Task
        {
            public enum Status { Pending, Running, Completed, Retry, Cancelled };
            public IDataBindEngineClient client;
            public TaskOps op;
            public Status status;
            public Task Next;
            public Task PreviousForClient;

            public Task(IDataBindEngineClient c, TaskOps o, Task previousForClient)
            {
                client = c;
                op = o;
                PreviousForClient = previousForClient;
                status = Status.Pending;
            }

            public void Run(bool lastChance)
            {
                status = Status.Running;
                Status newStatus = Status.Completed;
                switch (op)
                {
                    case TaskOps.TransferValue:
                        client.TransferValue();
                        break;

                    case TaskOps.UpdateValue:
                        client.UpdateValue();
                        break;

                    case TaskOps.RaiseTargetUpdatedEvent:
                        client.OnTargetUpdated();
                        break;

                    case TaskOps.AttachToContext:
                        bool succeeded = client.AttachToContext(lastChance);
                        if (!succeeded && !lastChance)
                            newStatus = Status.Retry;
                        break;

                    case TaskOps.VerifySourceReference:
                        client.VerifySourceReference(lastChance);
                        break;
                }
                status = newStatus;
            }
        }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        private DataBindEngine()
        {
            // Set up the final cleanup
            DataBindEngineShutDownListener listener = new DataBindEngineShutDownListener(this);

            // initialize the task list
            _head = new Task(null, TaskOps.TransferValue, null);
            _tail = _head;
            _mostRecentTask = new HybridDictionary();

            _cleanupHelper = new CleanupHelper(DoCleanup);
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal PathParser PathParser { get { return _pathParser; } }
        internal ValueConverterContext ValueConverterContext { get { return _valueConverterContext; } }
        internal AccessorTable AccessorTable { get { return _accessorTable; } }
        internal bool IsShutDown { get { return (_viewManager == null); } }

        internal bool CleanupEnabled
        {
            get { return _cleanupEnabled; }
            set
            {
                _cleanupEnabled = value;
                WeakEventManager.SetCleanupEnabled(value);
            }
        }

        internal IAsyncDataDispatcher AsyncDataDispatcher
        {
            get
            {
                // lazy construction of async dispatcher
                if (_defaultAsyncDataDispatcher == null)
                    _defaultAsyncDataDispatcher = new DefaultAsyncDataDispatcher();

                return _defaultAsyncDataDispatcher;
            }
        }

        /// <summary>
        /// Return the DataBindEngine for the current thread
        /// </summary>
        internal static DataBindEngine CurrentDataBindEngine
        {
            get
            {
                // _currentEngine is [ThreadStatic], so there's one per thread
                if (_currentEngine == null)
                {
                    _currentEngine = new DataBindEngine();
                }

                return _currentEngine;
            }
        }

        internal ViewManager ViewManager { get { return _viewManager; } }
        internal CommitManager CommitManager { get { if (!_commitManager.IsEmpty) ScheduleCleanup(); return _commitManager; } }


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal void AddTask(IDataBindEngineClient c, TaskOps op)
        {
            // ignore requests that arrive after shutdown
            if (_mostRecentTask == null)
                return;

            // if we're adding to an empty list, request that the list be processed
            if (_head == _tail)
            {
                RequestRun();
            }

            // link a new task into the list
            Task recentTask = (Task)_mostRecentTask[c];
            Task newTask = new Task(c, op, recentTask);
            _tail.Next = newTask;
            _tail = newTask;
            _mostRecentTask[c] = newTask;

            // if the task is AttachToContext and the target is a UIElement,
            // register for the LayoutUpdated event, and run the task list from the
            // event handler.  This avoids flashing, at the expense of lots more
            // events and handlers (bug 1019232)
            if (op == TaskOps.AttachToContext &&
                _layoutElement == null &&
                (_layoutElement = c.TargetElement as UIElement) != null)
            {
                _layoutElement.LayoutUpdated += new EventHandler(OnLayoutUpdated);
            }
        }

        internal void CancelTask(IDataBindEngineClient c, TaskOps op)
        {
            // ignore requests that arrive after shutdown
            if (_mostRecentTask == null)
                return;

            for (Task task = (Task)_mostRecentTask[c]; task != null; task = task.PreviousForClient)
            {
                if (task.op == op && task.status == Task.Status.Pending)
                {
                    task.status = Task.Status.Cancelled;
                    break;
                }
            }
        }

        internal void CancelTasks(IDataBindEngineClient c)
        {
            // ignore requests that arrive after shutdown
            if (_mostRecentTask == null)
                return;

            // cancel pending tasks for the given client
            for (Task task = (Task)_mostRecentTask[c]; task != null; task = task.PreviousForClient)
            {
                Invariant.Assert(task.client == c, "task list is corrupt");
                if (task.status == Task.Status.Pending)
                {
                    task.status = Task.Status.Cancelled;
                }
            }

            // no need to look at these tasks ever again
            _mostRecentTask.Remove(c);
        }

        internal object Run(object arg)
        {
            bool lastChance = (bool)arg;
            Task retryHead = lastChance ? null : new Task(null, TaskOps.TransferValue, null);
            Task retryTail = retryHead;

            // unregister the LayoutUpdated event - we only need to be called once
            if (_layoutElement != null)
            {
                _layoutElement.LayoutUpdated -= new EventHandler(OnLayoutUpdated);
                _layoutElement = null;
            }

            if (IsShutDown)
                return null;

            // iterate through the task list
            Task nextTask = null;
            for (Task task = _head.Next; task != null; task = nextTask)
            {
                // sever the back pointer - older tasks are no longer needed
                task.PreviousForClient = null;

                // run pending tasks
                if (task.status == Task.Status.Pending)
                {
                    task.Run(lastChance);

                    // fetch the next task _after_ the current task has
                    // run (in case the current task causes new tasks to be
                    // added to the list, as in bug 1938866), but _before_
                    // moving the current task to the retry list (which overwrites
                    // the Next pointer)
                    nextTask = task.Next;

                    if (task.status == Task.Status.Retry && !lastChance)
                    {
                        // the task needs to be retried - add it to the list
                        task.status = Task.Status.Pending;
                        retryTail.Next = task;
                        retryTail = task;
                        retryTail.Next = null;
                    }
                }
                else
                {
                    nextTask = task.Next;
                }
            }

            // return the list to its empty state
            _head.Next = null;
            _tail = _head;
            _mostRecentTask.Clear();

            // repost the tasks that need to be retried
            if (!lastChance)
            {
                // there is already a dispatcher request to call Run, so change
                // _head temporarily so that AddTask does not make another request
                Task headSave = _head;
                _head = null;

                for (Task task = retryHead.Next; task != null; task = task.Next)
                {
                    AddTask(task.client, task.op);
                }

                _head = headSave;
            }

            return null;
        }

        internal ViewRecord GetViewRecord(object collection, CollectionViewSource key, Type collectionViewType, bool createView, Func<object, object> GetSourceItem)
        {
            if (IsShutDown)
                return null;

            ViewRecord record = _viewManager.GetViewRecord(collection, key, collectionViewType, createView, GetSourceItem);

            // lacking any definitive event on which to trigger a cleanup pass,
            // we use a heuristic, namely the creation of a new view.  This suggests
            // that there is new activity, which often means that old content is
            // being replaced.  So perhaps the view table now has stale entries.
            if (record != null && !record.IsInitialized)
            {
                ScheduleCleanup();
            }

            return record;
        }

        internal void RegisterCollectionSynchronizationCallback(
                            IEnumerable collection,
                            object context,
                            CollectionSynchronizationCallback synchronizationCallback)
        {
            _viewManager.RegisterCollectionSynchronizationCallback(collection, context, synchronizationCallback);
        }

        // cache of default converters (so that all uses of string-to-int can
        // share the same converter)
        internal IValueConverter GetDefaultValueConverter(Type sourceType,
                                                        Type targetType,
                                                        bool targetToSource)
        {
            IValueConverter result = _valueConverterTable[sourceType, targetType, targetToSource];

            if (result == null)
            {
                result = DefaultValueConverter.Create(sourceType, targetType, targetToSource, this);
                if (result != null)
                    _valueConverterTable.Add(sourceType, targetType, targetToSource, result);
            }

            return result;
        }

        // make an async request to the scheduler that handles requests for the given target
        internal void AddAsyncRequest(DependencyObject target, AsyncDataRequest request)
        {
            if (target == null)
                return;

            // get the appropriate scheduler
            IAsyncDataDispatcher asyncDispatcher = AsyncDataDispatcher;
            /* AsyncDataDispatcher property is cut (task 41079)
            IAsyncDataDispatcher asyncDispatcher = Binding.GetAsyncDataDispatcher(target);
            if (asyncDispatcher == null)
            {
                asyncDispatcher = AsyncDataDispatcher;
            }
            */

            // add it to the list of schedulers that need cleanup
            if (_asyncDispatchers == null)
            {
                _asyncDispatchers = new HybridDictionary(1);    // lazy instantiation
            }
            _asyncDispatchers[asyncDispatcher] = null;  // the value is unused

            // make the request
            asyncDispatcher.AddRequest(request);
        }


        // ADO sometimes returns different values from two calls to GetValue
        // The following two methods, and the ValueTable class,
        // work around this "feature".  If ADO ever fixes their end, these can
        // be removed.

        // retrieve the value, using the cache if necessary
        internal object GetValue(object item, PropertyDescriptor pd, bool indexerIsNext)
        {
            return _valueTable.GetValue(item, pd, indexerIsNext);
        }

        // give the value cache first chance at handling property changes
        internal void RegisterForCacheChanges(object item, object descriptor)
        {
            PropertyDescriptor pd = descriptor as PropertyDescriptor;
            if (item != null && pd != null && ValueTable.ShouldCache(item, pd))
            {
                _valueTable.RegisterForChanges(item, pd, this);
            }
        }

        // schedule a cleanup pass.  This can be called from any thread.
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

        bool DoCleanup(bool forceCleanup)
        {
            if (CleanupEnabled || forceCleanup)
            {
                return DoCleanup();
            }
            else
            {
                return false;
            }
        }

        // return true if something was actually cleaned up
        internal bool Cleanup()
        {
            if (!BaseAppContextSwitches.EnableCleanupSchedulingImprovements)
            {
                return DoCleanup();
            }
            else
            {
                return _cleanupHelper.DoCleanup(forceCleanup: true);
            }
        }

        bool DoCleanup()
        {
            bool foundDirt = false;

            if (!IsShutDown)
            {
                foundDirt = _viewManager.Purge() || foundDirt;

                foundDirt = WeakEventManager.Cleanup() || foundDirt;

                foundDirt = _valueTable.Purge() || foundDirt;

                foundDirt = _commitManager.Purge() || foundDirt;
            }

            return foundDirt;
        }

        // Marshal some work from a foreign thread to the UI thread
        // (e.g. PropertyChanged or CollectionChanged events)
        internal DataBindOperation Marshal(DispatcherOperationCallback method, object arg, int cost = 1)
        {
            DataBindOperation op = new DataBindOperation(method, arg, cost);
            lock (_crossThreadQueueLock)
            {
                _crossThreadQueue.Enqueue(op);
                _crossThreadCost += cost;

                if (_crossThreadDispatcherOperation == null)
                {
                    _crossThreadDispatcherOperation = Dispatcher.BeginInvoke(
                        DispatcherPriority.ContextIdle,
                        (Action)ProcessCrossThreadRequests);
                }
            }

            return op;
        }

        internal void ChangeCost(DataBindOperation op, int delta)
        {
            lock (_crossThreadQueueLock)
            {
                op.Cost += delta;
                _crossThreadCost += delta;
            }
        }

        void ProcessCrossThreadRequests()
        {
            if (IsShutDown)
                return;

            try
            {
                long startTime = DateTime.Now.Ticks;        // unit = 10^-7 sec

                while (true)
                {
                    // get the next request
                    DataBindOperation op;
                    lock (_crossThreadQueueLock)
                    {
                        if (_crossThreadQueue.Count > 0)
                        {
                            op = _crossThreadQueue.Dequeue();
                            _crossThreadCost -= op.Cost;
                        }
                        else
                        {
                            op = null;
                        }
                    }

                    if (op == null)
                        break;

                    // do the work
                    op.Invoke();

                    // check the time
                    if (DateTime.Now.Ticks - startTime > CrossThreadThreshold)
                        break;
                }
            }
            finally
            {
                // update state even if an op throws an exception
                lock (_crossThreadQueueLock)
                {
                    if (_crossThreadQueue.Count > 0)
                    {
                        // if there's still more work to do, schedule a new callback
                        _crossThreadDispatcherOperation = Dispatcher.BeginInvoke(
                            DispatcherPriority.ContextIdle,
                            (Action)ProcessCrossThreadRequests);
                    }
                    else
                    {
                        // otherwise revert to the empty state
                        _crossThreadDispatcherOperation = null;
                        _crossThreadCost = 0;
                    }
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private void RequestRun()
        {
            // Run tasks before layout, to front load as much layout work as possible
            Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new DispatcherOperationCallback(Run), false);

            // Run tasks (especially re-tried AttachToContext tasks) again after
            // layout as the last chance.  Any failures in AttachToContext will
            // be treated as an error.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(Run), true);
        }

        // run a cleanup pass
        private object CleanupOperation(object arg)
        {
            // allow new requests, even if cleanup is disabled
            Interlocked.Exchange(ref _cleanupRequests, 0);

            if (!_cleanupEnabled)
                return null;

            Cleanup();

            return null;
        }

        // do the final cleanup when the Dispatcher or AppDomain is shut down
        private void OnShutDown()
        {
            _viewManager = null;
            _commitManager = null;
            _valueConverterTable = null;
            _mostRecentTask = null;
            _head = _tail = null;
            _crossThreadQueue.Clear();

            // notify all the async dispatchers we've ever talked to
            // The InterlockedExchange makes sure we only do this once
            // (in case Dispatcher and AppDomain are being shut down simultaneously
            // on two different threads)
            HybridDictionary asyncDispatchers = (HybridDictionary)Interlocked.Exchange(ref _asyncDispatchers, null);
            if (asyncDispatchers != null)
            {
                foreach (object o in asyncDispatchers.Keys)
                {
                    IAsyncDataDispatcher dispatcher = o as IAsyncDataDispatcher;
                    if (dispatcher != null)
                    {
                        dispatcher.CancelAllRequests();
                    }
                }
            }

            _defaultAsyncDataDispatcher = null;

            // Note: the engine is still held in TLS.  This maintains the 1-1 relationship
            // between the thread and the engine.  However the engine is basically
            // dead - _mostRecentTask is null, and most operations are now no-ops or illegal.
            // This imitates the behavior of the thread's Dispatcher.
        }

        // A UIElement with pending AttachToContext task(s) has raised the
        // LayoutUpdated event.  Run the task list.
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            Run(false);
        }

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        // cache of default value converters (so that all uses of string-to-int can
        // share the same converter)
        class ValueConverterTable : Hashtable
        {
            struct Key
            {
                Type _sourceType, _targetType;
                bool _targetToSource;

                public Key(Type sourceType, Type targetType, bool targetToSource)
                {
                    _sourceType = sourceType;
                    _targetType = targetType;
                    _targetToSource = targetToSource;
                }

                public override int GetHashCode()
                {
                    return _sourceType.GetHashCode() + _targetType.GetHashCode();
                }

                public override bool Equals(object o)
                {
                    if (o is Key)
                    {
                        return (this == (Key)o);
                    }
                    return false;
                }

                public static bool operator ==(Key k1, Key k2)
                {
                    return k1._sourceType == k2._sourceType &&
                            k1._targetType == k2._targetType &&
                            k1._targetToSource == k2._targetToSource;
                }

                public static bool operator !=(Key k1, Key k2)
                {
                    return !(k1 == k2);
                }
            }

            public IValueConverter this[Type sourceType, Type targetType, bool targetToSource]
            {
                get
                {
                    Key key = new Key(sourceType, targetType, targetToSource);
                    object value = base[key];
                    return (IValueConverter)value;
                }
            }

            public void Add(Type sourceType, Type targetType, bool targetToSource, IValueConverter value)
            {
                base.Add(new Key(sourceType, targetType, targetToSource), value);
            }
        }

        private sealed class DataBindEngineShutDownListener : ShutDownListener
        {
            public DataBindEngineShutDownListener(DataBindEngine target) : base(target)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                DataBindEngine table = (DataBindEngine)target;
                table.OnShutDown();
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private HybridDictionary _mostRecentTask;           // client --> Task
        Task _head;
        Task _tail;
        private UIElement _layoutElement;
        private ViewManager _viewManager = new ViewManager();
        private CommitManager _commitManager = new CommitManager();
        private ValueConverterTable _valueConverterTable = new ValueConverterTable();
        private PathParser _pathParser = new PathParser();
        private IAsyncDataDispatcher _defaultAsyncDataDispatcher;
        private HybridDictionary _asyncDispatchers;
        private ValueConverterContext _valueConverterContext = new ValueConverterContext();

        private bool _cleanupEnabled = true;

        private ValueTable _valueTable = new ValueTable();
        private AccessorTable _accessorTable = new AccessorTable();
        private int _cleanupRequests;
        private CleanupHelper _cleanupHelper;

        private Queue<DataBindOperation> _crossThreadQueue = new Queue<DataBindOperation>();
        private object _crossThreadQueueLock = new object();
        private int _crossThreadCost;
        private DispatcherOperation _crossThreadDispatcherOperation;
        internal const int CrossThreadThreshold = 50000;   // 50 msec

        [ThreadStatic]
        private static DataBindEngine _currentEngine; // one engine per thread
    }
}
