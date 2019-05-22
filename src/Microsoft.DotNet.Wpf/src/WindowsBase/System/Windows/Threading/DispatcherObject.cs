// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Threading;
using MS.Internal.WindowsBase;               // FriendAccessAllowed

namespace System.Windows.Threading
{
    /// <summary>
    ///     A DispatcherObject is an object associated with a
    ///     <see cref="Dispatcher"/>.  A DispatcherObject instance should
    ///     only be access by the dispatcher's thread.
    /// </summary>
    /// <remarks>
    ///     Subclasses of <see cref="DispatcherObject"/> should enforce thread
    ///     safety by calling <see cref="VerifyAccess"/> on all their public
    ///     methods to ensure the calling thread is the appropriate thread.
    ///     <para/>
    ///     DispatcherObject cannot be independently instantiated; that is,
    ///     all constructors are protected.
    /// </remarks>
    public abstract class DispatcherObject
    {
        /// <summary>
        ///     Returns the <see cref="Dispatcher"/> that this
        ///     <see cref="DispatcherObject"/> is associated with.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public Dispatcher Dispatcher
        {
            get
            {
                // This property is free-threaded.

                return _dispatcher;
            }
        }

        // This method allows certain derived classes to break the dispatcher affinity
        // of our objects.
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void DetachFromDispatcher()
        {
            _dispatcher = null;
        }

        // Make this object a "sentinel" - it can be used in equality tests, but should
        // not be used in any other way.  To enforce this and catch bugs, use a special
        // sentinel dispatcher, so that calls to CheckAccess and VerifyAccess will
        // fail;  this will catch most accidental uses of the sentinel.
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void MakeSentinel()
        {
            _dispatcher = EnsureSentinelDispatcher();
        }

        private static Dispatcher EnsureSentinelDispatcher()
        {
            if (_sentinelDispatcher == null)
            {
                // lazy creation - the first thread reaching here creates the sentinel
                // dispatcher, all other threads use it.
                Dispatcher sentinelDispatcher = new Dispatcher(isSentinel:true);
                Interlocked.CompareExchange<Dispatcher>(ref _sentinelDispatcher, sentinelDispatcher, null);
            }

            return _sentinelDispatcher;
        }

        /// <summary>
        ///     Checks that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        ///     Only the dispatcher thread may access DispatcherObjects.
        ///     <p/>
        ///     This method is public so that any thread can probe to
        ///     see if it has access to the DispatcherObject.
        /// </remarks>
        /// <returns>
        ///     True if the calling thread has access to this object.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool CheckAccess()
        {
            // This method is free-threaded.

            bool accessAllowed = true;
            Dispatcher dispatcher = _dispatcher;

            // Note: a DispatcherObject that is not associated with a
            // dispatcher is considered to be free-threaded.
            if(dispatcher != null)
            {
                accessAllowed = dispatcher.CheckAccess();
            }

            return accessAllowed;
        }

        /// <summary>
        ///     Verifies that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        ///     Only the dispatcher thread may access DispatcherObjects.
        ///     <p/>
        ///     This method is public so that derived classes can probe to
        ///     see if the calling thread has access to itself.
        /// </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyAccess()
        {
            // This method is free-threaded.

            Dispatcher dispatcher = _dispatcher;

            // Note: a DispatcherObject that is not associated with a
            // dispatcher is considered to be free-threaded.
            if(dispatcher != null)
            {
                dispatcher.VerifyAccess();
            }
        }

        /// <summary>
        ///     Instantiate this object associated with the current Dispatcher.
        /// </summary>
        protected DispatcherObject()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        private Dispatcher _dispatcher;
        private static Dispatcher _sentinelDispatcher;
    }
}

