// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// Description:
//      Implement IAddDomainShutdownListener and use AppDomainShutdownMonitor 
//      to know when the AppDomain is going down
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;           // Assert
using System.Collections.Generic;   // Dictionary
using System.Threading;             // [ThreadStatic]

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
            AppDomain.CurrentDomain.DomainUnload += OnShutdown;
            AppDomain.CurrentDomain.ProcessExit += OnShutdown;
        }

        public static void Add(WeakReference<IAppDomainShutdownListener> listener)
        {
            Debug.Assert(listener.TryGetTarget(out _));

            lock (_hashSet)
            {
                if (!_shuttingDown)
                {
                    _hashSet.Add(listener);
                }
            }
        }

        public static void Remove(WeakReference<IAppDomainShutdownListener> listener)
        {
            lock (_hashSet)
            {
                if (!_shuttingDown)
                {
                    _hashSet.Remove(listener);
                }
            }
        }

        private static void OnShutdown(object sender, EventArgs e)
        {
            lock (_hashSet)
            {
                // Setting this to true prevents Add and Remove from modifying the list. This
                // way we call out without holding a lock (which would be bad)
                _shuttingDown = true;
            }

            foreach (var weakReference in _hashSet)
            {
                if (weakReference.TryGetTarget(out var listener))
                {
                    listener.NotifyShutdown();
                }
            }
        }

        private static readonly HashSet<WeakReference<IAppDomainShutdownListener>> _hashSet =
            new HashSet<WeakReference<IAppDomainShutdownListener>>();

        private static bool _shuttingDown;
    }
}
