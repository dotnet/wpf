// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Wrapper that allows ReaderWriterLockSlim to detect potential 
//              error conditions like (i) calling into Dispose() of the lock object
//              when it is already held, and (ii) attempts to acquire the lock after 
//              it has been disposed.


using System;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;

namespace MS.Internal
{
    internal class ReaderWriterLockSlimWrapper : IDisposable
    {
        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recursionPolicy">
        /// This parameter is forwarded to the constructor of the backing 
        /// <see cref="ReaderWriterLockSlim"/> object
        /// </param>
        /// <param name="disableDispatcherProcessingWhenNoRecursion">
        /// Specifies whether Dispatcher processing should be disabled when 
        /// <paramref name="recursionPolicy"/> is <see cref="LockRecursionPolicy.NoRecursion"/>. 
        /// It is preferable to disable Dispatcher processing to prevent reentrancy problems.
        /// If Dispatcher processing is disabled, the critical action that is performed while
        /// the lock is held cannot in turn depend on dispatcher processing - for e.g., any
        /// actions involving UI updates would throw an exception. The default value for this 
        /// parameter is true -i.e., we disable Dispatcher processing by default when 
        /// <see cref="LockRecursionPolicy.NoRecursion"/> is specified. 
        /// 
        /// This parameter is ignored when <see cref="LockRecursionPolicy.SupportsRecursion"/>
        /// is specified. 
        /// </param>
        internal ReaderWriterLockSlimWrapper(
            LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion,
            bool disableDispatcherProcessingWhenNoRecursion = true)
        {
            _lockRecursionPolicy = recursionPolicy;
            _disableDispatcherProcessingWhenNoRecursion = disableDispatcherProcessingWhenNoRecursion;

            _rwLock = new ReaderWriterLockSlim(_lockRecursionPolicy);

            _disposed = false;

            // Suppress finalization of this object. 
            //
            // This ensures that the lock is usable within finalizers 
            // of other objects that have an instance of this type and 
            // use it for locking there.
            //
            // It would be the responsibility of caller to explicitly 
            // dispose this object. If an explicit call to Dispose() fails,
            // say because the lock is held at that time by another thread, 
            // we will re-enable finalization and try to cleanup again when 
            // Dispose is called from within the finalizer.
            // 
            // This can create the potential for this object to be
            // never finalized. This can happen if the caller forgets
            // to call Dispose() explicitly. In that case, the backing
            // ReaderWriterLockSlim object will still get disposed properly 
            // when its finalizer runs.

            GC.SuppressFinalize(this);
        }


        #endregion Constructor

        #region Internal Methods

        /// <summary>
        /// Enters a read-lock on the backing <see cref="ReaderWriterLockSlim"/>
        /// object, and attempts <paramref name="criticalAction"/> if successful 
        /// in acquiring the lock. The lock is released before returning
        /// from this method.
        /// </summary>
        /// <param name="criticalAction"></param>
        /// <returns>
        /// true if successful in acquiring the read-lock and attempting 
        /// to execute <paramref name="criticalAction"/>, otherwise false
        /// </returns>
        internal bool WithReadLock(Action criticalAction)
        {
            object oResult = null;
            return ExecuteWithinLockInternal(
                _rwLock.EnterReadLock, 
                _rwLock.ExitReadLock, 
                ref oResult, 
                criticalAction, null);
        }

        internal bool WithReadLock<T>(Action<T> criticalAction, T arg)
        {
            object oResult = null;

            return ExecuteWithinLockInternal(
                _rwLock.EnterReadLock, 
                _rwLock.ExitReadLock, 
                ref oResult, 
                criticalAction, arg);
        }

        internal bool WithReadLock<T, TResult>(Func<T, TResult> criticalAction, T arg, out TResult result)
        {
            object oResult = null;
            bool success = false; 

            try
            {
                success = ExecuteWithinLockInternal(
                    _rwLock.EnterReadLock,
                    _rwLock.ExitReadLock,
                    ref oResult,
                    criticalAction, arg);
            }
            finally
            {
                result = success ? (TResult)oResult : default(TResult);
            }

            return success;
        }

        internal bool WithReadLock<T1, T2, TResult>(Func<T1, T2, TResult> criticalAction, T1 arg1, T2 arg2, out TResult result)
        {
            object oResult = null;
            bool success = false;

            try
            {
                success = ExecuteWithinLockInternal(
                    _rwLock.EnterReadLock,
                    _rwLock.ExitReadLock,
                    ref oResult,
                    criticalAction, arg1, arg2);
            }
            finally
            {
                result = success ? (TResult)oResult : default(TResult);
            }

            return success;
        }

        /// <summary>
        /// Enter a write-lock on the backing <see cref="ReaderWriterLockSlim"/>
        /// object, and attempts <paramref name="criticalAction"/> if successful
        /// in acquiring the lock. The lock is released before returning
        /// from this method.
        /// </summary>
        /// <param name="criticalAction"></param>
        /// <returns>
        /// true if succesful in acquiring the write-lock and attempting 
        /// to execute <paramref name="criticalAction"/>, otherwise false
        /// </returns>
        internal bool WithWriteLock(Action criticalAction)
        {
            object oResult = null;
            return ExecuteWithinLockInternal(
                _rwLock.EnterWriteLock,
                _rwLock.ExitWriteLock,
                ref oResult,
                criticalAction, null);
        }

        internal bool WithWriteLock<T>(Action<T> criticalAction, T arg)
        {
            object oResult = null; 

            return ExecuteWithinLockInternal(
                        _rwLock.EnterWriteLock, 
                        _rwLock.ExitWriteLock, 
                        ref oResult, 
                        criticalAction, arg);
        }

        internal bool WithWriteLock<T, TResult>(Func<T, TResult> criticalAction, T arg, out TResult result)
        {
            bool success = false;
            object oResult = null;

            try
            {
                success = ExecuteWithinLockInternal(
                    _rwLock.EnterWriteLock,
                    _rwLock.ExitWriteLock,
                    ref oResult,
                    criticalAction, arg);
            }
            finally
            {
                result = success ? (TResult)oResult : default(TResult);
            }

            return success;
        }

        internal bool WithWriteLock<T1, T2, TResult>(Func<T1, T2, TResult> criticalAction, T1 arg1, T2 arg2, out TResult result)
        {
            bool success = false;
            object oResult = null;

            try
            {
                success = ExecuteWithinLockInternal(
                    _rwLock.EnterWriteLock,
                    _rwLock.ExitWriteLock,
                    ref oResult,
                    criticalAction, arg1, arg2);
            }
            finally
            {
                result = success ? (TResult)oResult : default(TResult);
            }

            return success;
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Calls <paramref name="criticalAction"/> after acquiring a 
        /// lock by executing the Delegate specified by <paramref name="lockAcquire"/>
        /// </summary>
        /// <param name="lockAcquire">Action that acquires a lock</param>
        /// <param name="lockRelease">Action that releases a lock</param>
        /// <param name="result">
        /// If criticalAction Delegate returns a value after execution, it is stored and returend to the caller through this out parameter
        /// </param>
        /// <param name="criticalAction">Critical action that is performed if lock acquisition is successful</param>
        /// <param name="args">Arguments passed on to criticalAction</param>
        /// <returns>True on successful lock acquisition and attempted exeuction of criticalAction, False otherwise</returns>
        /// <remarks>
        /// <paramref name="criticalAction"/> is only attempted upon the successful 
        /// acquisition of the lock. 
        /// 
        /// If the backing <see cref="ReaderWriterLockSlim"/> object is already 
        /// disposed, then <paramref name="criticalAction"/> is not attempted, 
        /// and this method returns false; 
        /// 
        /// If the attempt to acquire the lock fails due to 
        /// <see cref="LockRecursionException"/>, then <paramref name="criticalAction"/>
        /// is not attempted, and this method returns false. 
        /// 
        /// If <see cref="LockRecursionPolicy.NoRecursion"/> was specified when constructing
        /// this object, then Dispatcher processing is temporarily disabled when attempting to 
        /// acquire the lock and attempting to execute <paramref name="criticalAction"/>. 
        /// Dispatcher processing is re-enabled after the lock is released. This is done to 
        /// prevent potential reentrancy problems.  
        /// </remarks>
        private bool ExecuteWithinLockInternal(Action lockAcquire, Action lockRelease, ref object result, Delegate criticalAction, params object[] args)
        {
            if (criticalAction == null)
            {
                throw new ArgumentNullException(nameof(criticalAction));
            }

            bool lockAcquired = false;
            DispatcherProcessingDisabled? dispatcherProcessingDisabled = null;

            if ((_lockRecursionPolicy == LockRecursionPolicy.NoRecursion) && _disableDispatcherProcessingWhenNoRecursion)
            {
                dispatcherProcessingDisabled =
                    Dispatcher.FromThread(Thread.CurrentThread)?.DisableProcessing();
            }

            try
            {
                lockAcquire();
                lockAcquired = true;
            }
            catch (Exception e)
                when (e is ObjectDisposedException ||
                      e is LockRecursionException)
            {
                // Do nothing
            }
            finally
            {
                try
                {
                    if (lockAcquired)
                    {
                        result = criticalAction.DynamicInvoke(args);
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                finally
                {
                    // We don't really expect lockRelease() to throw, 
                    // but we gaurd it within try-finally anyway to be 
                    // very sure that Dispatcher processing will be 
                    // re-enabled after the lock is released. 
                    try
                    {
                        if (lockAcquired)
                        {
                            lockRelease();
                        }
                    }
                    finally
                    {
                        if (dispatcherProcessingDisabled != null)
                        {
                            dispatcherProcessingDisabled.Value.Dispose();
                        }
                    }
                }
            }

            return lockAcquired;
        }

        #endregion Private Methods

        #region IDisposable and Finalizer

        ~ReaderWriterLockSlimWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ReaderWriterLockSlimWrapper));
            }

            try
            {
                _rwLock.Dispose();
                _disposed = true;
                _rwLock = null;
            }
            catch (SynchronizationLockException) when (disposing)
            {
                // Propagate the exception only if we are in 
                // the finalizer (i.e., disposing == false) - this 
                // indicates that we have a potential deadlock. 
                //
                // If we are in an explicit call to Dispose(), then 
                // suppress this exception and do nothing further.
            }
            finally
            {
                /*
                 *   -----------------------------------------------------------------------------------
                 *   | _disposed | disposing  |         Notes            |         Action              |
                 *   -----------------------------------------------------------------------------------
                 *   |   false   |   false    | In finalizer -           | Propagate the exception     |
                 *   |           |            |      potential deadlock  |  i.e., don't catch it.      |
                 *   -----------------------------------------------------------------------------------
                 *   |   false   |   true     | In explicit Dispose() -  | Call                        |
                 *   |           |            |   try again in finalizer |  GC.ReRegisterForFinalize   |
                 *   -----------------------------------------------------------------------------------
                 *   |   true    |   false    | In finalizer             | Do nothing                  |
                 *   -----------------------------------------------------------------------------------
                 *   |   true    |   true     | In explicit Dispose() -  | Do nothing - finalization   |
                 *   |           |            |  prevent finalizer from  |  was suppresssed in cctor   |
                 *   |           |            |  running                 |                             |
                 *   -----------------------------------------------------------------------------------
                 */

                if (!_disposed && disposing)
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }

        #endregion IDisposable and Finalizer

        #region Private Fields

        private ReaderWriterLockSlim _rwLock;
        private readonly LockRecursionPolicy _lockRecursionPolicy;
        private readonly bool _disableDispatcherProcessingWhenNoRecursion;

        bool _disposed;

        #endregion Private Fields
    }
}