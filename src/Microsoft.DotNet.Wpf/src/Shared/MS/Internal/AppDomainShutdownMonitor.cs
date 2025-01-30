// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//---------------------------------------------------------------------------
//
//
//
// Description:
//      Implement IAddDomainShutdownListener and use AppDomainShutdownMonitor 
//      to know when the AppDomain is going down
//
//---------------------------------------------------------------------------

namespace MS.Internal
{
    internal interface IAppDomainShutdownListener
    {
        void NotifyShutdown();
    }
    
    internal static class AppDomainShutdownMonitor
    {
        static AppDomainShutdownMonitor()
        {
            _listeners =
                new HashSet<WeakReference<IAppDomainShutdownListener>>();

            AppDomain.CurrentDomain.DomainUnload += OnShutdown;
            AppDomain.CurrentDomain.ProcessExit += OnShutdown;
        }

        public static void Add(WeakReference<IAppDomainShutdownListener> listener)
        {
            Debug.Assert(listener.TryGetTarget(out _));

            lock (_listeners)
            {
                if (!_shuttingDown)
                {
                    _listeners.Add(listener);
                }
            }
        }

        public static void Remove(WeakReference<IAppDomainShutdownListener> listener)
        {
            lock (_listeners)
            {
                if (!_shuttingDown)
                {
                    _listeners.Remove(listener);
                }
            }
        }

        private static void OnShutdown(object sender, EventArgs e)
        {
            lock (_listeners)
            {
                // Setting this to true prevents Add and Remove from modifying the list. This
                // way we call out without holding a lock (which would be bad)
                _shuttingDown = true;
            }

            foreach (WeakReference<IAppDomainShutdownListener> weakReference in _listeners)
            {
                if (weakReference.TryGetTarget(out var listener))
                {
                    listener.NotifyShutdown();
                }
            }
        }

        private static readonly HashSet<WeakReference<IAppDomainShutdownListener>> _listeners;

        private static bool _shuttingDown;
    }
}
