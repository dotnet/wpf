// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description:
// Wrapper that allows a ReaderWriterLock to work with C#'s using() clause
//
//
//
//



using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;
using System.Windows.Threading;
using MS.Internal.WindowsBase;

namespace MS.Internal
{
    // Wrapper that allows a ReaderWriterLock to work with C#'s using() clause
    // ------ CAUTION --------
    // This uses a non-pumping wait while acquiring and releasing the lock, which
    // avoids re-entrancy that leads to deadlock
    // However, it means that the code protected by the lock must not do anything
    // that can re-enter or pump messages; otherwise there could be deadlock.
    // In effect, the protected code is limited to lock-free code that operates
    // on simple data structures - no async calls, no COM, no Dispatcher messaging,
    // no raising events, no calling out to user code, etc.
    // !!! It is the caller's responsibility to obey this rule. !!!
    // ------------------------
    [FriendAccessAllowed] // Built into Base, used by Core and Framework.
    internal class ReaderWriterLockWrapper
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ReaderWriterLockWrapper()
        {
            // ideally we'd like to use the NoRecursion policy, but RWLock supports
            // recursion so we allow recursion for compat.  It's needed for at least
            // one pattern - a weak event manager for an event A that delegates to
            // a second event B via a second weak event manager.  There's at least
            // one instance of this within WPF (CanExecuteChanged delegates to
            // RequerySuggested), and it could also happen in user code.
            _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _defaultSynchronizationContext = new NonPumpingSynchronizationContext();
            Initialize(!MS.Internal.BaseAppContextSwitches.EnableWeakEventMemoryImprovements);
        }

        private void Initialize(bool useLegacyMemoryBehavior)
        {
            if (useLegacyMemoryBehavior)
            {
                _awr = new AutoWriterRelease(this);
                _arr = new AutoReaderRelease(this);
            }
            else
            {
                _awrc = new AutoWriterReleaseClass(this);
                _arrc = new AutoReaderReleaseClass(this);

                _enterReadAction = _rwLock.EnterReadLock;
                _exitReadAction = _rwLock.ExitReadLock;
                _enterWriteAction = _rwLock.EnterWriteLock;
                _exitWriteAction = _rwLock.ExitWriteLock;
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal IDisposable WriteLock
        {
            get
            {
                if (!MS.Internal.BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
                {
                    CallWithNonPumpingWait(()=>{_rwLock.EnterWriteLock();});
                    return _awr;
                }
                else
                {
                    CallWithNonPumpingWait(_enterWriteAction);
                    return _awrc;
                }
            }
        }

        internal IDisposable ReadLock
        {
            get
            {
                if (!MS.Internal.BaseAppContextSwitches.EnableWeakEventMemoryImprovements)
                {
                    CallWithNonPumpingWait(()=>{_rwLock.EnterReadLock();});
                    return _arr;
                }
                else
                {
                    CallWithNonPumpingWait(_enterReadAction);
                    return _arrc;
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // called when AutoWriterRelease is disposed
        private void ReleaseWriterLock()
        {
            CallWithNonPumpingWait(()=>{_rwLock.ExitWriteLock();});
        }

        // called when AutoReaderRelease is disposed
        private void ReleaseReaderLock()
        {
            CallWithNonPumpingWait(()=>{_rwLock.ExitReadLock();});
        }

        // called when AutoWriterRelease is disposed
        private void ReleaseWriterLock2()
        {
            CallWithNonPumpingWait(_exitWriteAction);
        }

        // called when AutoReaderRelease is disposed
        private void ReleaseReaderLock2()
        {
            CallWithNonPumpingWait(_exitReadAction);
        }

        private void CallWithNonPumpingWait(Action callback)
        {
            SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;
            NonPumpingSynchronizationContext nonPumpingSynchronizationContext =
                Interlocked.Exchange<NonPumpingSynchronizationContext>(ref _defaultSynchronizationContext, null);

            // if the default non-pumping context is in use, allocate a new one
            bool usingDefaultContext = (nonPumpingSynchronizationContext != null);
            if (!usingDefaultContext)
            {
                nonPumpingSynchronizationContext = new NonPumpingSynchronizationContext();
            }

            try
            {
                // install the non-pumping context
                nonPumpingSynchronizationContext.Parent = oldSynchronizationContext;
                SynchronizationContext.SetSynchronizationContext(nonPumpingSynchronizationContext);

                // invoke the callback
                callback();
            }
            finally
            {
                // restore the old context
                SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);

                // put the default non-pumping context back into play
                if (usingDefaultContext)
                {
                    Interlocked.Exchange<NonPumpingSynchronizationContext>(ref _defaultSynchronizationContext, nonPumpingSynchronizationContext);
                }
            }
}

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ReaderWriterLockSlim _rwLock;
        private AutoReaderRelease _arr;
        private AutoWriterRelease _awr;
        private AutoReaderReleaseClass _arrc;
        private AutoWriterReleaseClass _awrc;
        private Action _enterReadAction;
        private Action _exitReadAction;
        private Action _enterWriteAction;
        private Action _exitWriteAction;
        private NonPumpingSynchronizationContext _defaultSynchronizationContext;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Classes & Structs
        //
        //------------------------------------------------------

        #region Private Classes & Structs

        private struct AutoWriterRelease : IDisposable
        {
            public AutoWriterRelease(ReaderWriterLockWrapper wrapper)
            {
                _wrapper = wrapper;
            }

            public void Dispose()
            {
                _wrapper.ReleaseWriterLock();
            }

            private ReaderWriterLockWrapper _wrapper;
        }

        private struct AutoReaderRelease : IDisposable
        {
            public AutoReaderRelease(ReaderWriterLockWrapper wrapper)
            {
                _wrapper = wrapper;
            }

            public void Dispose()
            {
                _wrapper.ReleaseReaderLock();
            }

            private ReaderWriterLockWrapper _wrapper;
        }

        private class AutoWriterReleaseClass : IDisposable
        {
            public AutoWriterReleaseClass(ReaderWriterLockWrapper wrapper)
            {
                _wrapper = wrapper;
            }

            public void Dispose()
            {
                _wrapper.ReleaseWriterLock2();
            }

            private ReaderWriterLockWrapper _wrapper;
        }

        private class AutoReaderReleaseClass : IDisposable
        {
            public AutoReaderReleaseClass(ReaderWriterLockWrapper wrapper)
            {
                _wrapper = wrapper;
            }

            public void Dispose()
            {
                _wrapper.ReleaseReaderLock2();
            }

            private ReaderWriterLockWrapper _wrapper;
        }

        // This SynchronizationContext waits without pumping messages, like
        // DispatcherSynchronizationContext when dispatcher is disabled.  This
        // avoids re-entrancy that leads to deadlock
        // It delegates all other functionality to its Parent (the context it
        // replaced), although if used properly those methods should never be called.
        private class NonPumpingSynchronizationContext : SynchronizationContext
        {
            public NonPumpingSynchronizationContext()
            {
                // Tell the CLR to call us when blocking.
                SetWaitNotificationRequired();
            }

            public SynchronizationContext Parent { get; set; }

            /// <summary>
            ///     Wait for a set of handles.
            /// </summary>
            #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
            [PrePrepareMethod]
            #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
            public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
            {
                return MS.Win32.UnsafeNativeMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll, millisecondsTimeout, false);
            }

            /// <summary>
            ///     Synchronously invoke the callback in the SynchronizationContext.
            /// </summary>
            public override void Send(SendOrPostCallback d, Object state)
            {
                Parent.Send(d, state);
            }

            /// <summary>
            ///     Asynchronously invoke the callback in the SynchronizationContext.
            /// </summary>
            public override void Post(SendOrPostCallback d, Object state)
            {
                Parent.Post(d, state);
            }

            /// <summary>
            ///     Create a copy of this SynchronizationContext.
            /// </summary>
            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }

        #endregion Private Classes
    }
}




