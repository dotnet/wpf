// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MS.Internal;
using MS.Win32;
using System.Security;
using System.Security.Permissions;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethodsMilCoreApi=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    #region SafeProfileHandle

    internal class SafeProfileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// </summary>
        /// <SecurityNote>
        ///    Critical: The ctor of the base class requires SecurityPermission
        /// </SecurityNote>
        internal SafeProfileHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// </summary>
        /// <SecurityNote>
        ///    Critical: The ctor of the base class requires SecurityPermission
        ///              This code calls SetHandle
        /// </SecurityNote>
        internal SafeProfileHandle(IntPtr profile)
            : base(true)
        {
            SetHandle(profile);
        }

        /// <SecurityNote>
        /// Critical - calls unmanaged code, not treat as safe because you must
        ///            validate that handle is a valid color context handle.
        /// </SecurityNote>
        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethodsMilCoreApi.Mscms.CloseColorProfile(handle);
        }
    }

    #endregion

    #region ColorContextHelper

    /// <summary>
    /// Helper struct to call into MSCMS color context APIs
    /// </summary>
    internal struct ColorContextHelper
    {
        /// Opens a color profile.
        ///
        /// NOTE: This may fail. It is up to the caller to handle it by checking IsInvalid.
        /// 
        /// <SecurityNote>
        /// SecurityCritical: Calls unmanaged code, accepts InPtr/unverified data.
        /// </SecurityNote>
        internal void OpenColorProfile(ref UnsafeNativeMethods.PROFILE profile)
        {
            // No need to get rid of the old handle as it will get GC'ed
            _profileHandle = UnsafeNativeMethodsMilCoreApi.Mscms.OpenColorProfile(
                ref profile,
                NativeMethods.PROFILE_READ,     // DesiredAccess
                NativeMethods.FILE_SHARE_READ,  // ShareMode
                NativeMethods.OPEN_EXISTING     // CreationMode
                );
        }

        /// Retrieves the profile header
        ///
        ///
        /// NOTE: This may fail. It is up to the caller to handle it by checking the bool.
        /// 
        /// <SecurityNote>
        /// SecurityCritical: Calls unmanaged code, accepts InPtr/unverified data.
        /// </SecurityNote>
        internal bool GetColorProfileHeader(out UnsafeNativeMethods.PROFILEHEADER header)
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_ColorContextInvalid));
            }

            return UnsafeNativeMethodsMilCoreApi.Mscms.GetColorProfileHeader(_profileHandle, out header);
        }

        /// Retrieves the color profile from handle
        /// <SecurityNote>
        /// SecurityCritical: Calls unmanaged code, accepts InPtr/unverified data.
        /// </SecurityNote>
        internal void GetColorProfileFromHandle(byte[] buffer, ref uint bufferSize)
        {
            Invariant.Assert(buffer == null || bufferSize <= buffer.Length);
            
            if (IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_ColorContextInvalid));
            }

            // If the buffer is null, this function will return FALSE because it didn't actually copy anything. That's fine and that's
            // what we want.
            if (!UnsafeNativeMethodsMilCoreApi.Mscms.GetColorProfileFromHandle(_profileHandle, buffer, ref bufferSize) && buffer != null)
            {
                HRESULT.Check(Marshal.GetHRForLastWin32Error());
            }
        }

        /// <SecurityNote>
        /// Critical - Accesses critical resource _profileHandle
        /// TreatAsSafe - No inputs and just checks if SafeHandle is valid or not.
        /// </SecurityNote>
        internal bool IsInvalid
        {
            get
            {
                return _profileHandle == null || _profileHandle.IsInvalid;
            }
        }

        /// <summary>
        /// ProfileHandle
        /// </summary>
        /// <SecurityNote>
        /// SecurityCritical: This comes out of an elevation needs to be critical and tracked.
        /// </SecurityNote>
        internal SafeProfileHandle ProfileHandle
        {
            get
            {
                return _profileHandle;
            }
        }

        #region Data members

        /// <SecurityNote>
        /// SecurityCritical: This comes out of an elevation needs to be critical and tracked.
        /// </SecurityNote>
        SafeProfileHandle _profileHandle;

        #endregion
    }

    #endregion
}
