// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//
// A consolidated file of native structures exposed for interop.
// These may be structs or classes, depending on the calling requirements
//
// The naming should generally match the native counterparts.
// The structures may be slightly thicker wrappers to make them
// easier to consume correctly from .Net code (e.g. hiding resource management)
//

namespace MS.Internal.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Security;

    // Some COM interfaces and Win32 structures are already declared in the framework.
    // Interesting ones to remember in System.Runtime.InteropServices.ComTypes are:
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
    using IPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;
    using IStream = System.Runtime.InteropServices.ComTypes.IStream;

    /// <SecurityNote>
    /// This class is not safe to use in partial trust.
    /// Critical: This class is a wrapper for a native structure that manages its own resources.
    /// </SecurityNote>
    [SecurityCritical]
    [StructLayout(LayoutKind.Explicit)]
    internal class PROPVARIANT : IDisposable
    {
        private static class NativeMethods
        {
            /// <SecurityNote>
            /// Critical: Suppresses unmanaged code security.
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            [DllImport(MS.Win32.ExternDll.Ole32)]
            internal static extern int PropVariantClear(PROPVARIANT pvar);
        }

        [FieldOffset(0)]
        private ushort vt;
        [FieldOffset(8)]
        private IntPtr pointerVal;
        [FieldOffset(8)]
        private byte byteVal;
        [FieldOffset(8)]
        private long longVal;
        [FieldOffset(8)]
        private short boolVal;

        /// <SecurityNote>
        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        public VarEnum VarType
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get { return (VarEnum)vt; }
        }

        // Right now only using this for strings.  If for some reason we get something else return null.
        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public string GetValue()
        {
            if (vt == (ushort)VarEnum.VT_LPWSTR)
            {
                return Marshal.PtrToStringUni(pointerVal);
            }

            return null;
        }

        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void SetValue(bool f)
        {
            Clear();
            vt = (ushort)VarEnum.VT_BOOL;
            boolVal = (short)(f ? -1 : 0);
        }

        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void SetValue(string val)
        {
            Clear();
            vt = (ushort)VarEnum.VT_LPWSTR;
            pointerVal = Marshal.StringToCoTaskMemUni(val);
        }

        /// <SecurityNote>
        /// Critical - Calls critical PropVariantClear
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void Clear()
        {
            NativeMethods.PropVariantClear(this);
        }

        #region IDisposable Pattern

        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        ~PROPVARIANT()
        {
            Dispose(false);
        }

        /// <SecurityNote>
        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void Dispose(bool disposing)
        {
            Clear();
        }

        #endregion
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [BestFitMapping(false)]
    internal class WIN32_FIND_DATAW
    {
        public FileAttributes dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    // New to Win7.
    [StructLayout(LayoutKind.Sequential)]
    internal struct CHANGEFILTERSTRUCT
    {
        public uint cbSize;
        public MSGFLTINFO ExtStatus;
    }
}
