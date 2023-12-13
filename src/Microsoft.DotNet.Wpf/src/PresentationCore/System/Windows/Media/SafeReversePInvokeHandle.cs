// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 
//      A safe way to deal with unmanaged MIL interface pointers.

using System;
using System.IO;
using System.Security;
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
    internal class SafeReversePInvokeWrapper : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Use this constructor if the handle isn't ready yet and later
        /// set the handle with SetHandle. SafeMILHandle owns the release
        /// of the handle.
        /// </summary>
        internal SafeReversePInvokeWrapper() : base(true) 
        { 
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// SafeMILHandle owns the release of the parameter.
        /// </summary>
        internal SafeReversePInvokeWrapper(IntPtr delegatePtr) : base(true) 
        {
            // Wrap the reverse p-invoke into a reversePInvokeWrapper.
            IntPtr reversePInvokeWrapper;
            HRESULT.Check(UnsafeNativeMethods.MilCoreApi.MilCreateReversePInvokeWrapper(delegatePtr, out reversePInvokeWrapper));

            SetHandle(reversePInvokeWrapper);
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                UnsafeNativeMethods.MilCoreApi.MilReleasePInvokePtrBlocking(handle);
            }
            UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref handle);

            return true;
        }
    }
}

