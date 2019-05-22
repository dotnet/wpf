// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Collections;
using System.Reflection;
using MS.Internal;
using MS.Win32;
using System.Diagnostics;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    internal class SafeMediaHandle : SafeMILHandle
    {
        /// <summary>
        /// </summary>
        internal SafeMediaHandle()
        {
        }

        /// <summary>
        /// </summary>
        ///<SecurityNote>
        ///     Critical: calls SafeHandle.SetHandle which LinkDemands
        ///               also takes arbitrary IntPtr as a handle
        ///</SecurityNote> 
        [SecurityCritical]
        internal SafeMediaHandle(IntPtr handle)
        {
            SetHandle(handle);
        }

        /// <SecurityNote>
        /// Critical - calls unmanaged code, not treat as safe because you must
        ///            validate that handle is a valid COM object.
        /// </SecurityNote>
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            HRESULT.Check(MILMedia.Shutdown(handle));
            UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref handle);

            return true;
        }
    }
}

