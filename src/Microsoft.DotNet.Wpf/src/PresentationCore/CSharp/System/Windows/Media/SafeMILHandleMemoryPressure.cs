// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 
//      Tracks the amount of native memory used by SafeMILHandle objects.

using System;
using System.Security;
using MS.Internal;
using System.Threading;

namespace System.Windows.Media
{
    internal class SafeMILHandleMemoryPressure
    {
        internal SafeMILHandleMemoryPressure(long gcPressure)
        {
            _gcPressure = gcPressure;
            _refCount = 0;

            
            // Removed WPF specific GC algorithm and all bitmap allocations/deallocations
            // are now tracked with GC.Add/RemoveMemoryPressure.
            GC.AddMemoryPressure(_gcPressure);
        }

        internal void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        internal void Release()
        {
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                
                // Removed WPF specific GC algorithm and all bitmap allocations/deallocations
                // are now tracked with GC.Add/RemoveMemoryPressure.
                GC.RemoveMemoryPressure(_gcPressure);
                _gcPressure = 0;
            }
        }

        // Estimated size in bytes of the unmanaged memory
        private long _gcPressure;

        //
        // SafeMILHandleMemoryPressure does its own ref counting in managed code, because the
        // associated memory pressure should be removed when there are no more managed
        // references to the unmanaged resource. There can still be references to it from
        // unmanaged code elsewhere, but that should not prevent the memory pressure from being
        // released.
        //
        private int _refCount;
    }
}

