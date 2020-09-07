// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Value pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// public interface that represents UI elements that are expressing a value
    /// </summary>
    [ComVisible(true)]
    [Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IValueProvider
#else
    public interface IValueProvider
#endif
    {
        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="value">Value to set the UI to</param>
        void SetValue([MarshalAs(UnmanagedType.LPWStr)] string value);

        ///<summary>Value of a value control, as a a string.</summary>
        string Value
        {
            get;
        }

        ///<summary>Indicates that the value can only be read, not modified.
        ///returns True if the control is read-only</summary>
        bool IsReadOnly
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
    }
}
