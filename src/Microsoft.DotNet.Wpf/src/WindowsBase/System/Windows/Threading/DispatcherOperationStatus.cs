// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace System.Windows.Threading
{
    /// <summary>
    ///     An enunmeration describing the status of a DispatcherOperation.
    /// </summary>
    ///
    public enum DispatcherOperationStatus
    {
        /// <summary>
        ///     The operation is still pending.
        /// </summary>
        Pending,

        /// <summary>
        ///     The operation has been aborted.
        /// </summary>
        Aborted,

        /// <summary>
        ///     The operation has been completed.
        /// </summary>
        Completed,
        
        /// <summary>
        ///     The operation has started executing, but has not completed yet.
        /// </summary>
        Executing
    }
}


