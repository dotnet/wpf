// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using Windows.Win32.Foundation;
using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    internal class SafeMediaHandle : SafeMILHandle
    {
        internal SafeMediaHandle()
        {
        }

        internal SafeMediaHandle(IntPtr handle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            MILMedia.Shutdown(handle).ThrowOnFailureExtended();
            UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref handle);

            return true;
        }
    }
}

