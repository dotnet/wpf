// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using MS.Internal.PrintWin32Thunk;

    /// <summary>
    /// An HGlobal allocated buffer that knows its byte length
    /// </summary>
    sealed class HGlobalBuffer
    {
        public static HGlobalBuffer Null = new HGlobalBuffer();

        private HGlobalBuffer()
        {
        }

        ///<SecurityNote>
        /// Critical    - calls into code with SUC applied to allocate native memory 
        ///             - Sets a critical for set member
        ///</SecurityNote>
        public HGlobalBuffer(int length)
        {
            Invariant.Assert(length > 0);
            
            this.Handle = SafeMemoryHandle.Create(length);
            this.Length = length;
        }
        
        ///<SecurityNote>
        /// Critical    - Accesses critical handle in native memeory
        ///</SecurityNote>
        public SafeMemoryHandle Handle
        {
            get;

            private set;
        }
    
        ///<SecurityNote>
        /// Critical    - Sets a critical for set member
        ///</SecurityNote>
        public int Length {
            get;

            private set;
        }
    
        ///<SecurityNote>
        /// Critical    - Calls into code with SUC applied to free native memory 
        ///             - Sets critical SafeMemoryHandle member
        ///</SecurityNote>
        public void Release()
        {
            SafeHandle handle = this.Handle;
            this.Handle = null;

            if (handle != null)
            {
                handle.Dispose();
            }
        }
    }
}
