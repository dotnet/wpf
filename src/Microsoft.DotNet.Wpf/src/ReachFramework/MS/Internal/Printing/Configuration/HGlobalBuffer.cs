// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using MS.Internal.PrintWin32Thunk;

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// An HGlobal allocated buffer that knows its byte length
    /// </summary>
    internal sealed class HGlobalBuffer
    {
        public static HGlobalBuffer Null = new HGlobalBuffer();

        private HGlobalBuffer()
        {
        }

        public HGlobalBuffer(int length)
        {
            Invariant.Assert(length > 0);
            
            this.Handle = SafeMemoryHandle.Create(length);
            this.Length = length;
        }
        
        public SafeMemoryHandle Handle
        {
            get;

            private set;
        }
    
        public int Length {
            get;

            private set;
        }
    
        public void Release()
        {
            SafeHandle handle = this.Handle;
            this.Handle = null;

            handle?.Dispose();
        }
    }
}
