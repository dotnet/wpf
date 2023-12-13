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

    [StructLayout(LayoutKind.Explicit)]
    internal class PROPVARIANT : IDisposable
    {
        private static class NativeMethods
        {
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

        /// Critical: This class is tagged Critical
        /// TreatAsSafe - This class is only available in full trust.
        public VarEnum VarType
        {
            get { return (VarEnum)vt; }
        }

        // Right now only using this for strings.  If for some reason we get something else return null.
        public string GetValue()
        {
            if (vt == (ushort)VarEnum.VT_LPWSTR)
            {
                return Marshal.PtrToStringUni(pointerVal);
            }

            return null;
        }

        public void SetValue(bool f)
        {
            Clear();
            vt = (ushort)VarEnum.VT_BOOL;
            boolVal = (short)(f ? -1 : 0);
        }

        public void SetValue(string val)
        {
            Clear();
            vt = (ushort)VarEnum.VT_LPWSTR;
            pointerVal = Marshal.StringToCoTaskMemUni(val);
        }

        public void Clear()
        {
            NativeMethods.PropVariantClear(this);
        }

        #region IDisposable Pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PROPVARIANT()
        {
            Dispose(false);
        }

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
