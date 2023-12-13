// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using MS.Internal;
using MS.Win32.Penimc;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Struct used to store new PenContext information.
    /// </summary>
    internal struct PenContextInfo
    {
        public SecurityCriticalDataClass<IPimcContext3> PimcContext;
        
        public SecurityCriticalDataClass<IntPtr> CommHandle;
        
        public int ContextId;

        /// <summary>
        /// The GIT key for a WISP context COM object.
        /// </summary>
        public UInt32 WispContextKey;
    }
}


