// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  SafeRightsManagementHandle class
//
//
//
//

using System;
using System.Runtime.InteropServices;    
using Microsoft.Win32.SafeHandles;
using System.Security;

namespace MS.Internal.Security.RightsManagement
{
    internal sealed class SafeRightsManagementHandle : SafeHandle
    {
        //Although it is not obvious this constructor is being called by the interop services 
        // it throws exceptions without it 
        private SafeRightsManagementHandle()
            : base(IntPtr.Zero, true)
        {
        }

        //  We have incompatibility between SafeHandle class hierarchy and the unmanaged 
        //  DRM SDK declarations. In the safe handle hierarchy it is assumed that the type 
        //  of handle is the IntPtr (64 or 32 bit depending on the platform). In the unmanaged
        //  SDK C++ unsigned long type is used, which is 32 bit regardless of the platform. 
        //  We have decided the safest thing would be to still use the SafeHandle classes 
        //  and subclasses and cast variable back and force under assumption that IntPtr
        //  is at least as big as unsigned long (in the managed code we generally use uint 
        //  declaration for that)  
        internal SafeRightsManagementHandle(uint handle)
            : base((IntPtr)handle, true)  // "true" means "owns the handle"
        {
        } 

        // base class expects us to override this method with the handle specific release code  
        protected override bool ReleaseHandle()
        {
            int z = 0;
            if (!IsInvalid) 
            {
                        // we can not use safe handle in the DrmClose... function
                        // as the SafeHandle implementation marks this instance as an invalid by the time 
                        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
                        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
                z = SafeNativeMethods.DRMCloseHandle((uint)this.handle);
        #if DEBUG
                    Errors.ThrowOnErrorCode(z); 
        #endif

                // This member might be called twice(depending on the client app). In order to 
                // prevent Unmanaged RM SDK from returning an error (Handle is already closed) 
                // we need to mark our handle as invalid after successful close call
                base.SetHandle(IntPtr.Zero);   
            }
            
            return (z>=0);
        }


        // apparently there is no existing implementation that treats Zero and only Zero as an invalid value 
        // so we are sub-classing the base class and we need to override the IsInvalid property along with 
        // ReleaseHandle implementation
        public override bool IsInvalid
        {
            get
            {
                return this.handle.Equals(IntPtr.Zero);
            }
        }

        internal static SafeRightsManagementHandle InvalidHandle
        {
            get
            {                
                return _invalidHandle;
            }
        }

        private static readonly SafeRightsManagementHandle _invalidHandle = new SafeRightsManagementHandle(0);
    }
}
