// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description:
// Wrapper that allows a ReaderWriterLockSlim to work with C#'s using() clause
//
//
//
//

using System;
using System.Threading;
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
    internal sealed class ReaderWriterLockWrapper
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
            _writerRelease = new AutoWriterRelease(this);
            _readerRelease = new AutoReaderRelease(this);
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
                CallWithNonPumpingWait(static rwls => rwls.EnterWriteLock(), _rwLock);
                return _writerRelease;
            }
        }

        internal IDisposable ReadLock
        {
            get
            {
                CallWithNonPumpingWait(static rwls => rwls.EnterReadLock(), _rwLock);
                return _readerRelease;
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
        private void ReleaseWriterLock() => CallWithNonPumpingWait(static rwls => rwls.ExitWriteLock(), _rwLock);

        // called when AutoReaderRelease is disposed
        private void ReleaseReaderLock() => CallWithNonPumpingWait(static rwls => rwls.ExitReadLock(), _rwLock);

        private void CallWithNonPumpingWait(Action<ReaderWriterLockSlim> callback, ReaderWriterLockSlim rwls)
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
                callback(rwls);
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

        private readonly ReaderWriterLockSlim _rwLock;
        private readonly AutoReaderRelease _readerRelease;
        private readonly AutoWriterRelease _writerRelease;
        private NonPumpingSynchronizationContext _defaultSynchronizationContext;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Classes & Structs
        //
        //------------------------------------------------------

        #region Private Classes & Structs

        private sealed class AutoWriterRelease : IDisposable
        {
            public AutoWriterRelease(ReaderWriterLockWrapper wrapper) => _wrapper = wrapper;

            public void Dispose() => _wrapper.ReleaseWriterLock();

            private readonly ReaderWriterLockWrapper _wrapper;
        }

        private sealed class AutoReaderRelease : IDisposable
        {
            public AutoReaderRelease(ReaderWriterLockWrapper wrapper) => _wrapper = wrapper;

            public void Dispose() => _wrapper.ReleaseReaderLock();

            private readonly ReaderWriterLockWrapper _wrapper;
        }

        // This SynchronizationContext waits without pumping messages, like
        // DispatcherSynchronizationContext when dispatcher is disabled.  This
        // avoids re-entrancy that leads to deadlock
        // It delegates all other functionality to its Parent (the context it
        // replaced), although if used properly those methods should never be called.
        private sealed class NonPumpingSynchronizationContext : SynchronizationContext
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




