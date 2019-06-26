// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//      Module to allocate shared memory between process.
//      The memory allocation scheme is different for Win 9x and WinNT
//      Performance measurements showed that using a global heap mechanism
//      is fastest on Win 9x.
//
//      When the dll is loaded, a counter is set to 1. For each instance
//      creation/destruction, the counter is is incremented/decremented.
//      When the dll is released, the counter gets to zero and the static
//      heap on Win 9x is destroyed.
//

using System;
using System.ComponentModel;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Automation;
using Microsoft.Win32.SafeHandles;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Class to allocate shared memory between process.
    class RemoteMemoryBlock : SafeHandleZeroOrMinusOneIsInvalid
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // This constructor is used by the P/Invoke marshaling layer
        // to allocate a SafeHandle instance.  P/Invoke then does the
        // appropriate method call, storing the handle in this class.
        private RemoteMemoryBlock() : base(true) {}

        internal RemoteMemoryBlock(int cbSize, SafeProcessHandle processHandle) : base(true)
        {
            _processHandle = processHandle;

            SetHandle(Misc.VirtualAllocEx(_processHandle, IntPtr.Zero, new UIntPtr((uint)cbSize), UnsafeNativeMethods.MEM_COMMIT, UnsafeNativeMethods.PAGE_READWRITE));
        }

        // Uncomment this if & only if we need a constructor
        // that takes a handle from external code
        //internal RemoteMemoryBlock(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        //{
        //    SetHandle(preexistingHandle);
        //}

        protected override bool ReleaseHandle()
        {
            return Misc.VirtualFreeEx(_processHandle, handle, UIntPtr.Zero, UnsafeNativeMethods.MEM_RELEASE);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        internal IntPtr Address
        {
            get
            {
                return handle;
            }
        }

        internal void WriteTo(IntPtr sourceAddress, IntPtr cbSize)
        {
            IntPtr count;
            Misc.WriteProcessMemory(_processHandle, handle, sourceAddress, cbSize, out count);
        }

        internal void ReadFrom(IntPtr remoteAddress, IntPtr destAddress, IntPtr cbSize)
        {
            IntPtr count;
            Misc.ReadProcessMemory(_processHandle, remoteAddress, destAddress, cbSize, out count);
        }

        internal void ReadFrom(SafeCoTaskMem destAddress, IntPtr cbSize)
        {
            IntPtr count;
            Misc.ReadProcessMemory(_processHandle, handle, destAddress, cbSize, out count);
        }
        internal void ReadFrom(IntPtr destAddress, IntPtr cbSize)
        {
            IntPtr count;
            Misc.ReadProcessMemory(_processHandle, handle, destAddress, cbSize, out count);
        }

        // Helper function to handle common scenario of translating a remote
        // unmanaged char/wchar buffer in to a managed string object.
        internal bool ReadString (out string str, int maxLength)
        {
            // Clear the return string
            str = null;

            // Ensure there is a buffer size
            if (0 >= maxLength)
            {
                return false;
            }

            // Ensure proper allocation
            using (SafeCoTaskMem ptr = new SafeCoTaskMem(maxLength))
            {
                if (ptr.IsInvalid)
                {
                    return false;
                }
                // Copy remote buffer back to local process...
                ReadFrom(ptr, new IntPtr(maxLength * sizeof (char)));

                // Convert the local unmanaged buffer in to a string object
                str = ptr.GetStringUni(maxLength);

                // Note: lots of "old world" strings are null terminated
                // Leaving the null termination in the System.String may lead
                // to some issues when used with the StringBuilder
                int nullTermination = str.IndexOf('\0');

                if (-1 != nullTermination)
                {
                    // We need to strip null terminated char and everything behind it from the str
                    str = str.Remove(nullTermination, maxLength - nullTermination);
                }

                return true;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private SafeProcessHandle _processHandle; // Handle of remote process

        #endregion
    }
}
