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

            _dictionary = new Dictionary<WeakReference, WeakReference>();
        }
        
        public static void Add(WeakReference listener)
        {
            Debug.Assert(listener.Target != null);
            Debug.Assert(listener.Target is IAppDomainShutdownListener);

            lock (_dictionary)
            {
                if (!_shuttingDown)
                {
                    _dictionary.Add(listener, listener);
                }
            }
        }

        public static void Remove(WeakReference listener)
        {
            Debug.Assert(listener.Target == null || listener.Target is IAppDomainShutdownListener);

            lock (_dictionary)
            {
                if (!_shuttingDown)
                {
                    _dictionary.Remove(listener);
                }
            }
        }

        private static void OnShutdown(object sender, EventArgs e)
        {
            lock (_dictionary)
            {     
                // Setting this to true prevents Add and Remove from modifying the list. This
                // way we call out without holding a lock (which would be bad)
                _shuttingDown = true;
            }
            
            foreach (WeakReference value in _dictionary.Values)
            {
                IAppDomainShutdownListener listener = value.Target as IAppDomainShutdownListener;
                if (listener != null)
                {
                    listener.NotifyShutdown();
                }
            }
        }
        
        private static Dictionary<WeakReference, WeakReference> _dictionary;
        private static bool _shuttingDown;
    }
}
