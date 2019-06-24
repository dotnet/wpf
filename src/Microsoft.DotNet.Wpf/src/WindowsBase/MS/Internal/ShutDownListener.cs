// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Listen for shut down events on behalf of a target, in a way that
//          does not leak the target.  ShutDown events include:
//                  AppDomain.DomainUnload
//                  AppDomain.ProcessExit
//                  Dispatcher.ShutdownFinished
//          Listening to these events directly can cause leaks, since the AppDomain
//          lives longer than the target.
//
//          The WeakEvent pattern has similar goals, but can't be used here because
//          the WeakEvent table itself needs to listen for shutdown events.
//
//          "Target" refers to the actual consumer of the event(s).  Each class
//          XYZ that wants to consume these events should define a class
//          XYZShutDownListener deriving from ShutDownListener that overrides the
//          OnShutDown method.  The target's constructor typically creates an
//          instance of the XYZShutDownListener, which holds a weak reference to
//          the target, and listens for the desired events.  When an event occurs,
//          the OnShutDown override is passed a (normal) reference to the target
//          object, and typically calls an appropriate target method that reacts
//          to the event.  (See examples in WeakEventTable, DataBindEngine, etc.)
//
//          Shutdown is a "one-time" process.  When the ShutDownListener receives
//          any one of the desired events, it stops listening to all events.

using System;
using System.Security;              // 
using System.Threading;             // Interlocked
using System.Windows.Threading;     // Dispatcher
using MS.Internal.WindowsBase;      // [FriendAccessAllowed]

namespace MS.Internal
{
    [FriendAccessAllowed]   // defined in Base, also used in Framework
    [Flags]
    internal enum ShutDownEvents : ushort
    {
        DomainUnload        = 0x0001,
        ProcessExit         = 0x0002,
        DispatcherShutdown  = 0x0004,

        AppDomain           = DomainUnload | ProcessExit,
        All                 = AppDomain | DispatcherShutdown,
    }

    [FriendAccessAllowed]   // defined in Base, also used in Framework
    internal abstract class ShutDownListener : WeakReference
    {
        internal ShutDownListener(object target)
            : this(target, ShutDownEvents.All)
        {
        }

        internal ShutDownListener(object target, ShutDownEvents events)
            : base(target)
        {
            _flags = ((PrivateFlags)events) | PrivateFlags.Listening;

            if (target == null)
            {
                _flags |= PrivateFlags.Static;
            }

            if ((_flags & PrivateFlags.DomainUnload) != 0)
            {
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(HandleShutDown);
            }

            if ((_flags & PrivateFlags.ProcessExit) != 0)
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(HandleShutDown);
            }

            if ((_flags & PrivateFlags.DispatcherShutdown) != 0)
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.ShutdownFinished += new EventHandler(HandleShutDown);
                _dispatcherWR = new WeakReference(dispatcher);
            }
        }

        // derived class should override this method to inform the target that a shutdown
        // event has occurred.  This method might be called on any thread (e.g.
        // AppDomain.DomainUnload events are typically raised on worker threads).
        abstract internal void OnShutDown(object target, object sender, EventArgs e);

        // stop listening for shutdown events
        internal void StopListening()
        {
            if ((_flags & PrivateFlags.Listening) == 0)
                return;

            _flags = _flags & ~PrivateFlags.Listening;

            if ((_flags & PrivateFlags.DomainUnload) != 0)
            {
                AppDomain.CurrentDomain.DomainUnload -= new EventHandler(HandleShutDown);
            }

            if ((_flags & PrivateFlags.ProcessExit) != 0)
            {
                AppDomain.CurrentDomain.ProcessExit -= new EventHandler(HandleShutDown);
            }

            if ((_flags & PrivateFlags.DispatcherShutdown) != 0)
            {
                Dispatcher dispatcher = (Dispatcher)_dispatcherWR.Target;
                if (dispatcher != null)
                {
                    dispatcher.ShutdownFinished -= new EventHandler(HandleShutDown);
                }
                _dispatcherWR = null;
            }
        }

        // handle a shutdown event
        private void HandleShutDown(object sender, EventArgs e)
        {
            // The dispatcher and AppDomain events might arrive on separate threads
            // at the same time.  The interlock assures that we only do the work
            // once.
            if (Interlocked.Exchange(ref _inShutDown, 1) == 0)
            {
                // ShutDown is a one-time event.  Stop listening (thus releasing
                // references to the ShutDownListener).
                StopListening();

                // do the shutdown work, unless the target has been GC'd already.
                object target = Target;
                if (target != null || (_flags & PrivateFlags.Static) != 0)
                {
                    OnShutDown(target, sender, e);
                }
            }
        }

        [Flags]
        enum PrivateFlags : ushort
        {
            DomainUnload        = ShutDownEvents.DomainUnload,
            ProcessExit         = ShutDownEvents.ProcessExit,
            DispatcherShutdown  = ShutDownEvents.DispatcherShutdown,

            Static              = 0x4000,
            Listening           = 0x8000,
        }

        PrivateFlags _flags;
        WeakReference _dispatcherWR;
        int _inShutDown;
    }
}
