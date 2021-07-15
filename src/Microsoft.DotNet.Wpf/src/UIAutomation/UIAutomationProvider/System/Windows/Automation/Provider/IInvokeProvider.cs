// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Invoke pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Implemented by objects that have a single, unambiguous, action associated with them.
    /// These objects are usually stateless, and invoking them does not change their own state,
    /// but causes something to happen in the larger context of the app the control is in.
    /// 
    /// Examples of UI that implments this includes:
    /// Push buttons
    /// Hyperlinks
    /// Menu items
    /// </summary>
    [ComVisible(true)]
    [Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IInvokeProvider
#else
    public interface IInvokeProvider
#endif
    {
        /// <summary>
        /// Request that the control initiate its action.
        /// Should return immediately without blocking.
        /// There is no way to determine what happened, when it happend, or whether
        /// anything happened at all.
        /// </summary>
        void Invoke();
    }
}
