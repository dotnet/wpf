// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Wraps System.Threading.Monitor and adds a busy flag
//


using System;
using System.Threading;
using System.Windows;

using MS.Internal;

namespace MS.Internal.Utility
{
    /// <summary>
    /// Monitor with Busy flag while it is entered.
    /// </summary>
    internal class MonitorWrapper
    {
        public IDisposable Enter()
        {
            Monitor.Enter(_syncRoot);
            Interlocked.Increment(ref _enterCount);
            return new MonitorHelper(this);
        }

        public void Exit()
        {
            int count = Interlocked.Decrement(ref _enterCount);
            Invariant.Assert(count >= 0, "unmatched call to MonitorWrapper.Exit");
            Monitor.Exit(_syncRoot);
        }

        public bool Busy
        {
            get
            {
                return (_enterCount > 0);
            }
        }

        int _enterCount;
        object _syncRoot = new object();

        private class MonitorHelper : IDisposable
        {
            public MonitorHelper(MonitorWrapper monitorWrapper)
            {
                _monitorWrapper = monitorWrapper;
            }

            public void Dispose()
            {
                if (_monitorWrapper != null)
                {
                    _monitorWrapper.Exit();
                    _monitorWrapper = null;
                }
                GC.SuppressFinalize(this);
            }
            private MonitorWrapper _monitorWrapper;
        }
    }
}

