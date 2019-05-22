// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper operations for data bindings.
//
// See spec at Data Binding.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

using MS.Internal.Data;

namespace System.Windows.Data
{
    /// <summary>
    /// Operations to manipulate data bindings.
    /// </summary>
    public static class BindingOperations
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// A sentinel object.  WPF assigns this as the DataContext of elements
        /// that leave an ItemsControl because (a) the corresponding item is
        /// removed from the ItemsSource collection, or (b) the element is
        /// scrolled out of view and re-virtualized.   Bindings that use DataContext
        /// react by unhooking from property-changed events.   This keeps the
        /// discarded elements from interfering with the still-visible elements.
        /// </summary>
        public static object DisconnectedSource
        {
            get { return BindingExpressionBase.DisconnectedItem; }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Attach a BindingExpression to a property.
        /// </summary>
        /// <remarks>
        /// A new BindingExpression is created from the given description, and attached to
        /// the given property of the given object.  This method is the way to
        /// attach a Binding to an arbitrary DependencyObject that may not expose
        /// its own SetBinding method.
        /// </remarks>
        /// <param name="target">object on which to attach the Binding</param>
        /// <param name="dp">property to which to attach the Binding</param>
        /// <param name="binding">description of the Binding</param>
        /// <exception cref="ArgumentNullException"> target and dp and binding cannot be null </exception>
        public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty dp, BindingBase binding)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (dp == null)
                throw new ArgumentNullException("dp");
            if (binding == null)
                throw new ArgumentNullException("binding");
//            target.VerifyAccess();

            BindingExpressionBase bindExpr = binding.CreateBindingExpression(target, dp);

            // BUG: should prevent transfers and updates until BindingExpression is ready
            //BindingExpression.SetFlag(BindingFlags.iInTransfer | BindingFlags.iInUpdate);
            target.SetValue(dp, bindExpr);

            return bindExpr;
        }


        /// <summary>
        /// Retrieve a BindingBase.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the binding</param>
        /// <param name="dp">property from which to retrieve the binding</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static BindingBase GetBindingBase(DependencyObject target, DependencyProperty dp)
        {
            BindingExpressionBase b = GetBindingExpressionBase(target, dp);
            return (b != null) ? b.ParentBindingBase : null;
        }

        /// <summary>
        /// Retrieve a Binding.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the binding</param>
        /// <param name="dp">property from which to retrieve the binding</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static Binding GetBinding(DependencyObject target, DependencyProperty dp)
        {
            return GetBindingBase(target, dp) as Binding;
        }

        /// <summary>
        /// Retrieve a PriorityBinding.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the binding</param>
        /// <param name="dp">property from which to retrieve the binding</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static PriorityBinding GetPriorityBinding(DependencyObject target, DependencyProperty dp)
        {
            return GetBindingBase(target, dp) as PriorityBinding;
        }

        /// <summary>
        /// Retrieve a MultiBinding.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the binding</param>
        /// <param name="dp">property from which to retrieve the binding</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static MultiBinding GetMultiBinding(DependencyObject target, DependencyProperty dp)
        {
            return GetBindingBase(target, dp) as MultiBinding;
        }

        /// <summary>
        /// Retrieve a BindingExpressionBase.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the BindingExpression</param>
        /// <param name="dp">property from which to retrieve the BindingExpression</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static BindingExpressionBase GetBindingExpressionBase(DependencyObject target, DependencyProperty dp)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (dp == null)
                throw new ArgumentNullException("dp");
//            target.VerifyAccess();

            Expression expr = StyleHelper.GetExpression(target, dp);
            return expr as BindingExpressionBase;
        }

        /// <summary>
        /// Retrieve a BindingExpression.
        /// </summary>
        /// <remarks>
        /// This method returns null if no Binding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the BindingExpression</param>
        /// <param name="dp">property from which to retrieve the BindingExpression</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static BindingExpression GetBindingExpression(DependencyObject target, DependencyProperty dp)
        {
            BindingExpressionBase expr = GetBindingExpressionBase(target, dp);

            PriorityBindingExpression pb = expr as PriorityBindingExpression;
            if (pb != null)
                expr = pb.ActiveBindingExpression;

            return expr as BindingExpression;
        }

        /// <summary>
        /// Retrieve a MultiBindingExpression.
        /// </summary>
        /// <remarks>
        /// This method returns null if no MultiBinding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the MultiBindingExpression</param>
        /// <param name="dp">property from which to retrieve the MultiBindingExpression</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static MultiBindingExpression GetMultiBindingExpression(DependencyObject target, DependencyProperty dp)
        {
            return GetBindingExpressionBase(target, dp) as MultiBindingExpression;
        }

        /// <summary>
        /// Retrieve a PriorityBindingExpression.
        /// </summary>
        /// <remarks>
        /// This method returns null if no PriorityBinding has been set on the given
        /// property.
        /// </remarks>
        /// <param name="target">object from which to retrieve the PriorityBindingExpression</param>
        /// <param name="dp">property from which to retrieve the PriorityBindingExpression</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static PriorityBindingExpression GetPriorityBindingExpression(DependencyObject target, DependencyProperty dp)
        {
            return GetBindingExpressionBase(target, dp) as PriorityBindingExpression;
        }

        /// <summary>
        /// Remove data Binding (if any) from a property.
        /// </summary>
        /// <remarks>
        /// If the given property is data-bound, via a Binding, PriorityBinding or MultiBinding,
        /// the BindingExpression is removed, and the property's value changes to what it
        /// would be as if no local value had ever been set.
        /// If the given property is not data-bound, this method has no effect.
        /// </remarks>
        /// <param name="target">object from which to remove Binding</param>
        /// <param name="dp">property from which to remove Binding</param>
        /// <exception cref="ArgumentNullException"> target and dp cannot be null </exception>
        public static void ClearBinding(DependencyObject target, DependencyProperty dp)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (dp == null)
                throw new ArgumentNullException("dp");
//            target.VerifyAccess();

            if (IsDataBound(target, dp))
                target.ClearValue(dp);
        }

        /// <summary>
        /// Remove all data Binding (if any) from a DependencyObject.
        /// </summary>
        /// <param name="target">object from which to remove bindings</param>
        /// <exception cref="ArgumentNullException"> DependencyObject target cannot be null </exception>
        public static void ClearAllBindings(DependencyObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
//            target.VerifyAccess();

            LocalValueEnumerator lve = target.GetLocalValueEnumerator();

            // Batch properties that have BindingExpressions since clearing
            // during a local value enumeration is illegal
            ArrayList batch = new ArrayList(8);

            while (lve.MoveNext())
            {
                LocalValueEntry entry = lve.Current;
                if (IsDataBound(target, entry.Property))
                {
                    batch.Add(entry.Property);
                }
            }

            // Clear all properties that are storing BindingExpressions
            for (int i = 0; i < batch.Count; i++)
            {
                target.ClearValue((DependencyProperty)batch[i]);
            }
        }

        /// <summary>Return true if the property is currently data-bound</summary>
        /// <exception cref="ArgumentNullException"> DependencyObject target cannot be null </exception>
        public static bool IsDataBound(DependencyObject target, DependencyProperty dp)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (dp == null)
                throw new ArgumentNullException("dp");
//            target.VerifyAccess();

            object o = StyleHelper.GetExpression(target, dp);
            return (o is BindingExpressionBase);
        }

        /// <summary>
        /// Register a callback used to synchronize access to a given collection.
        /// </summary>
        /// <param name="collection"> The collection that needs synchronized access. </param>
        /// </param name="context"> An arbitrary object.  This object is passed back into
        ///     the callback;  it is not used otherwise.   It provides a way for the
        ///     application to store information it knows at registration time, which it
        ///     can then use at collection-access time.  Typically this information will
        ///     help the application determine the synchronization mechanism used to
        ///     control access to the given collection. </param>
        /// <param name="synchronizationCallback"> The callback to be invoked whenever
        ///     access to the collection is required. </param>
        public static void EnableCollectionSynchronization(
                            IEnumerable collection,
                            object context,
                            CollectionSynchronizationCallback synchronizationCallback)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (synchronizationCallback == null)
                throw new ArgumentNullException("synchronizationCallback");

            ViewManager.Current.RegisterCollectionSynchronizationCallback(
                collection, context, synchronizationCallback);
        }

        /// <summary>
        /// Register a lock object used to synchronize access to a given collection.
        /// </summary>
        /// <param name="collection"> The collection that needs synchronized access. </param>
        /// <param name="lockObject"> The object to lock when accessing the collection. </param>
        public static void EnableCollectionSynchronization(
                            IEnumerable collection,
                            object lockObject)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (lockObject == null)
                throw new ArgumentNullException("lockObject");

            ViewManager.Current.RegisterCollectionSynchronizationCallback(
                collection, lockObject, null);
        }

        /// <summary>
        /// Remove the synchronization registered for a given collection.  Any subsequent
        /// access to the collection will be unsynchronized.
        /// </summary>
        /// <param name="collection"> The collection that needs its synchronized access removed. </param>
        public static void DisableCollectionSynchronization(
                            IEnumerable collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            ViewManager.Current.RegisterCollectionSynchronizationCallback(
                collection, null, null);
        }

        /// <summary>
        /// A caller can use this method to access a collection using the
        /// synchronization that the application has registered for that collection.
        /// If no synchronization is registered, the access method is simply called
        /// directly.
        /// </summary>
        /// <notes>
        /// 1. This method is provided chiefly for collection views, which need to
        /// redirect access through the application-provided synchronization pattern.
        /// An application typically doesn't need to call it, as the application
        /// can use its synchronization directly.
        /// 2. It is usually convenient to provide a closure as the access
        /// method.   This closure will have access to local variables at the
        /// site of the call.  For example, the following code reads _collection[3]:
        ///     void MyMethod()
        ///     {
        ///         int index = 3;
        ///         int result = 0;
        ///         BindingOperations.AccessCollection(
        ///             _collection,
        ///             () => { result = _collection[index]; },
        ///             false);             // read-access
        ///     }
        /// Note that the access method refers to local variables (index, result)
        /// of MyMethod, as well as to an instance variable (_collection) of the
        /// 'this' object.
        /// </notes>
        public static void AccessCollection(
                            IEnumerable collection,
                            Action accessMethod,
                            bool writeAccess)
        {
            ViewManager vm = ViewManager.Current;
            if (vm == null)
                throw new InvalidOperationException(SR.Get(SRID.AccessCollectionAfterShutDown, collection));

            vm.AccessCollection(collection, accessMethod, writeAccess);
        }

        /// <summary>
        /// Returns a list of all binding expressions that are:
        ///     a) top-level (do not belong to a parent MultiBindingExpression or BindingGroup)
        ///     b) source-updating (binding mode is TwoWay or OneWayToSource)
        ///     c) currently dirty or invalid
        /// and d) attached to a descendant of the given DependencyObject (if non-null).
        /// These are the bindings that may need attention before executing a command.
        /// </summary>
        public static ReadOnlyCollection<BindingExpressionBase> GetSourceUpdatingBindings(DependencyObject root)
        {
            List<BindingExpressionBase> list = DataBindEngine.CurrentDataBindEngine.CommitManager.GetBindingsInScope(root);
            return new ReadOnlyCollection<BindingExpressionBase>(list);
        }

        /// <summary>
        /// Returns a list of all BindingGroups that are:
        ///     a) currently dirty or invalid
        /// and b) attached to a descendant of the given DependencyObject (if non-null).
        /// </summary>
        public static ReadOnlyCollection<BindingGroup> GetSourceUpdatingBindingGroups(DependencyObject root)
        {
            List<BindingGroup> list = DataBindEngine.CurrentDataBindEngine.CommitManager.GetBindingGroupsInScope(root);
            return new ReadOnlyCollection<BindingGroup>(list);
        }

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        /// This event is raised when a collection is first noticed by the data-binding system.
        /// It provides an opportunity to register information about the collection,
        /// before the data-binding system begins to use the collection.
        /// </summary>
        /// <notes>
        /// In an application with multiple UI threads (i.e. multiple Dispatchers),
        /// this event is raised independently on each thread the first time the
        /// collection is noticed on that thread.
        /// </notes>
        public static event EventHandler<CollectionRegisteringEventArgs> CollectionRegistering;

        /// <summary>
        /// This event is raised when a collection view is first noticed by the data-binding system.
        /// It provides an opportunity to modify the collection view,
        /// before the data-binding system begins to use it.
        /// </summary>
        /// <notes>
        /// In an application with multiple UI threads (i.e. multiple Dispatchers),
        /// each thread has its own set of collection views.  This event is raised
        /// on the thread that owns the given collection view.
        /// </notes>
        public static event EventHandler<CollectionViewRegisteringEventArgs> CollectionViewRegistering;


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // return false if this is an invalid value for UpdateSourceTrigger
        internal static bool IsValidUpdateSourceTrigger(UpdateSourceTrigger value)
        {
            switch (value)
            {
                case UpdateSourceTrigger.Default:
                case UpdateSourceTrigger.PropertyChanged:
                case UpdateSourceTrigger.LostFocus:
                case UpdateSourceTrigger.Explicit:
                    return true;

                default:
                    return false;
            }
        }

        // The following properties and methods have no internal callers.  They
        // can be called by suitably privileged external callers via reflection.
        // They are intended to be used by test programs and the DRT.

        // Enable or disable the cleanup pass.  For use by tests that measure
        // perf, to avoid noise from the cleanup pass.
        internal static bool IsCleanupEnabled
        {
            get { return DataBindEngine.CurrentDataBindEngine.CleanupEnabled; }
            set { DataBindEngine.CurrentDataBindEngine.CleanupEnabled = value; }
        }

        // Force a cleanup pass (even if IsCleanupEnabled is true).  For use
        // by leak-detection tests, to avoid false leak reports about objects
        // held by the DataBindEngine that can be cleaned up.  Returns true
        // if something was actually cleaned up.
        internal static bool Cleanup()
        {
            return DataBindEngine.CurrentDataBindEngine.Cleanup();
        }

        // Print various interesting statistics
        internal static void PrintStats()
        {
            DataBindEngine.CurrentDataBindEngine.AccessorTable.PrintStats();
        }

        // Trace the size of the accessor table after each generation
        internal static bool TraceAccessorTableSize
        {
            get { return DataBindEngine.CurrentDataBindEngine.AccessorTable.TraceSize; }
            set { DataBindEngine.CurrentDataBindEngine.AccessorTable.TraceSize = value; }
        }

        // Raise the CollectionRegistering event
        internal static void OnCollectionRegistering(IEnumerable collection, object parent)
        {
            if (CollectionRegistering != null)
                CollectionRegistering(null, new CollectionRegisteringEventArgs(collection, parent));
        }

        // Raise the CollectionViewRegistering event
        internal static void OnCollectionViewRegistering(CollectionView view)
        {
            if (CollectionViewRegistering != null)
                CollectionViewRegistering(null, new CollectionViewRegisteringEventArgs(view));
        }

        #if LiveShapingInstrumentation

        public static void SetNodeSize(int nodeSize)
        {
            LiveShapingBlock.SetNodeSize(nodeSize);
        }

        public static void SetQuickSortThreshold(int threshold)
        {
            LiveShapingTree.SetQuickSortThreshold(threshold);
        }

        public static void SetBinarySearchThreshold(int threshold)
        {
            LiveShapingTree.SetBinarySearchThreshold(threshold);
        }

        public static void ResetComparisons(ListCollectionView lcv)
        {
            lcv.ResetComparisons();
        }

        public static void ResetCopies(ListCollectionView lcv)
        {
            lcv.ResetCopies();
        }

        public static void ResetAverageCopy(ListCollectionView lcv)
        {
            lcv.ResetAverageCopy();
        }

        public static int GetComparisons(ListCollectionView lcv)
        {
            return lcv.GetComparisons();
        }

        public static int GetCopies(ListCollectionView lcv)
        {
            return lcv.GetCopies();
        }

        public static double GetAverageCopy(ListCollectionView lcv)
        {
            return lcv.GetAverageCopy();
        }

        #endif // LiveShapingInstrumentation

        #region Exception logging
        // For use when testing whether an exception is thrown, even though the
        // exception is caught.  The catch-block logs the exception, and the
        // test code can examine the log.

        // Enable exception-logging.
        // Test code uses the following pattern (suitably translated to reflection-based calls):
        //      using (ExceptionLogger logger = BindingOperations.EnableExceptionLogging())
        //      {
        //          DoSomethingThatMightThrowACaughtException();
        //          if (logger.Log.Count > 0)
        //          {
        //              // an exception was thrown - react appropriately
        //          }
        //      }
        internal static IDisposable EnableExceptionLogging()
        {
            ExceptionLogger newLogger = new ExceptionLogger();
            Interlocked.CompareExchange<ExceptionLogger>(ref _exceptionLogger, newLogger, null);
            return _exceptionLogger;
        }

        // Log an exception, if logging is active (called from framework catch-blocks that opt-in)
        internal static void LogException(Exception ex)
        {
            ExceptionLogger logger = _exceptionLogger;
            if (logger != null)
            {
                logger.LogException(ex);
            }
        }

        private static ExceptionLogger _exceptionLogger;

        internal class ExceptionLogger : IDisposable
        {
            // Log an exception
            internal void LogException(Exception ex)
            {
                _log.Add(ex);
            }

            // when the active logger is disposed, turn off logging
            void IDisposable.Dispose()
            {
                Interlocked.CompareExchange<ExceptionLogger>(ref _exceptionLogger, null, this);
            }

            internal List<Exception> Log { get { return _log; } }

            List<Exception> _log = new List<Exception>();
        }
        #endregion Exception logging
    }
}

