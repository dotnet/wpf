// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  File: SafeSystemMetrics.cs
//  This class is copied from the system metrics class in frameworks. The
//  reason it exists is to consolidate all system metric calls through one layer
//  so that maintenance from a security stand point gets easier. We will add
//  mertrics on a need basis. The caching code is removed since the original calls 
//  that were moved here do not rely on caching. If there is a percieved perf. problem
//  we can work on enabling this.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.Win32;
using System.Security;
using System.Security.Permissions;
using MS.Win32;
using MS.Internal;
using MS.Internal.Interop;

using MS.Internal.PresentationCore;

namespace MS.Win32
{
    /// <summary>
    ///     Contains properties that are queries into the system's various settings.
    /// </summary>
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal sealed class SafeSystemMetrics
    {

        private SafeSystemMetrics()
        {
        }

#if !PRESENTATION_CORE
        /// <summary>
        ///     Maps to SM_CXVIRTUALSCREEN
        /// </summary>
        /// <SecurityNote>
        ///    TreatAsSafe --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int VirtualScreenWidth
        {
            get
            {
                SecurityHelper.DemandUnmanagedCode();                

                return UnsafeNativeMethods.GetSystemMetrics(SM.CXVIRTUALSCREEN);
            }
        }

        /// <summary>
        ///     Maps to SM_CYVIRTUALSCREEN
        /// </summary>
        /// <SecurityNote>
        ///    TreatAsSafe --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int VirtualScreenHeight
        {
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                return UnsafeNativeMethods.GetSystemMetrics(SM.CYVIRTUALSCREEN);
            }
        }
#endif //end !PRESENTATIONCORE

        /// <summary>
        ///     Maps to SM_CXDOUBLECLK
        /// </summary>
        /// <SecurityNote>
        ///     TreatAsSafe --This data is safe to expose
        ///     Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int DoubleClickDeltaX
        {
            get
            {
                return UnsafeNativeMethods.GetSystemMetrics(SM.CXDOUBLECLK);
            }
        }

        /// <summary>
        ///     Maps to SM_CYDOUBLECLK
        /// </summary>
        /// <SecurityNote>
        ///    TreatAsSafe --This data is safe to expose
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int DoubleClickDeltaY
        {
            get
            {
                return UnsafeNativeMethods.GetSystemMetrics(SM.CYDOUBLECLK);
            }
        }

            
        /// <summary>
        ///     Maps to SM_CXDRAG
        /// </summary>
        /// <SecurityNote>
        ///     TreatAsSafe --This data is safe to expose
        ///     Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int DragDeltaX
        {
            get
            {
                return UnsafeNativeMethods.GetSystemMetrics(SM.CXDRAG);
            }
        }

        /// <summary>
        ///     Maps to SM_CYDRAG
        /// </summary>
        /// <SecurityNote>
        ///    TreatAsSafe --This data is safe to expose
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int DragDeltaY
        {
            get
            {
                return UnsafeNativeMethods.GetSystemMetrics(SM.CYDRAG);
            }
        }

        ///<summary> 
        /// Is an IMM enabled ? Maps to SM_IMMENABLED
        ///</summary> 
        ///<SecurityNote> 
        ///Critical - calls a method that performs an elevation. 
        /// TreatAsSafe - data is considered safe to expose. 
        ///</SecurityNote> 
        internal static bool IsImmEnabled
        {
            get
            {
                return  (UnsafeNativeMethods.GetSystemMetrics(SM.IMMENABLED) != 0);
            }

        }

    }
}
