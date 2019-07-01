// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Win32
{
    internal class Kernel32
    {
        #region Private Data

        private const string KERNEL32DLL = "Kernel32.dll";
        private const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const uint FORMAT_MESSAGE_FROM_STRING = 0x00000400;
        private const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        private const uint FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF;

        internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        internal const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
        internal const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

        // These const are defined in winnt.h. We only support x86, ia64, and amd64.
        // On Mac the bytes of these numbers are reversed. But we don't care about Mac.
        internal const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;      // MZ
        internal const uint IMAGE_NT_SIGNATURE = 0x00004550;  // PE00

        internal const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0;
        internal const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;  // Intel 386.
        internal const ushort IMAGE_FILE_MACHINE_IA64 = 0x0200;  // Intel 64
        internal const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;  // AMD64 (K8)
        internal const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);


        /// <summary>
        /// From "winnt.h"
        /// </summary>
        internal const uint LOCALE_USER_DEFAULT = 0x00;                // LCID  --  SORT_DEFAULT = 0x00 & LANG_USER_DEFAULT = 0x00 (because LANG_NEUTRAL = 0x00 & SUBLANG_NEUTRAL = 0x00)
        internal const uint LOCALE_SYSTEM_DEFAULT = 0x0020;            // LCID  --  SORT_DEFAULT = 0x00 & LANG_SYSTEM_DEFAULT = 0x02 (because LANG_NEUTRAL = 0x00 & SUBLANG_SYS_NEUTRAL = 0x02)
        internal const uint LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;  // LCTYPE  --  ANSI Code Page
        internal const int CODEPAGE_ANSI_LATIN = 1252;                 // The value for the Code page ANSI latin (english, french, german, spanish, ...)

        #endregion

        #region Public Members

        public static string GetStringForErrorCode(int herror)
        {
            StringBuilder retVal = new StringBuilder();
            uint dwFlags = FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS;
            if (FormatMessage(dwFlags, IntPtr.Zero, (uint)herror, 0, ref retVal, 0, IntPtr.Zero) == 0)
            {
                // call failed, not a major issue, just return the error code
                return "( Unable to retrive string associated with this error code)";
            }
            return retVal.ToString();
        }

        #endregion

        #region Private Imports

        [StructLayout(LayoutKind.Sequential)]
        internal struct OSVERSIONINFOEX
        {
            public int Size;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string ServicePack;
            public short ServicePackMajor;
            public short ServicePackMinor;
            public short SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal _PROCESSOR_INFO_UNION uProcessorInfo;
            public uint dwPageSize;
            public uint lpMinimumApplicationAddress;
            public uint lpMaximumApplicationAddress;
            public uint dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public uint dwProcessorLevel;
            public uint dwProcessorRevision;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct _PROCESSOR_INFO_UNION
        {
            [FieldOffset(0)]
            internal uint dwOemId;
            [FieldOffset(0)]
            internal ushort wProcessorArchitecture;
            [FieldOffset(2)]
            internal ushort wReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IMAGE_DOS_HEADER       // DOS .EXE header
        {
            public ushort e_magic;                     // Magic number
            public ushort e_cblp;                      // Bytes on last page of file
            public ushort e_cp;                        // Pages in file
            public ushort e_crlc;                      // Relocations
            public ushort e_cparhdr;                   // Size of header in paragraphs
            public ushort e_minalloc;                  // Minimum extra paragraphs needed
            public ushort e_maxalloc;                  // Maximum extra paragraphs needed
            public ushort e_ss;                        // Initial (relative) SS value
            public ushort e_sp;                        // Initial SP value
            public ushort e_csum;                      // Checksum
            public ushort e_ip;                        // Initial IP value
            public ushort e_cs;                        // Initial (relative) CS value
            public ushort e_lfarlc;                    // File address of relocation table
            public ushort e_ovno;                      // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res;                     // Reserved words
            public ushort e_oemid;                     // OEM identifier (for e_oeminfo)
            public ushort e_oeminfo;                   // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;                      // Reserved words
            public int e_lfanew;                    // File address of new exe header
        }

        [DllImport(KERNEL32DLL)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(KERNEL32DLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isWow64);

        [DllImport(KERNEL32DLL)]
        internal static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

        [DllImport(KERNEL32DLL)]
        internal static extern void GetNativeSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

        [DllImport(KERNEL32DLL, PreserveSig = true, SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref StringBuilder lpBuffer, uint nSize, IntPtr va_list);

        /// <summary>
        /// New vista API = get product information...
        /// </summary>
        /// <param name="majorVersion">The major version number of the operating system. The minimum value is 6.</param>
        /// <param name="minorVersion">The minor version number of the operating system. The minimum value is 0.</param>
        /// <param name="servicePackMajorVersion">The major version number of the operating system service pack. The minimum value is 0.</param>
        /// <param name="servicePackMinorVersion">The minor version number of the operating system service pack. The minimum value is 0. </param>
        /// <param name="productType">The product type. This parameter cannot be NULL. If the specified operating system is less than the current operating system, 
        /// this information is mapped to the types supported by the specified operating system. If the specified operating 
        /// system is greater than the highest supported operating system, this information is 
        /// mapped to the types supported by the current operating system.</param>
        /// <returns>
        /// If the function succeeds, the return value is a nonzero value. 
        /// If the software license is invalid or expired, the function succeeds but the <paramref name="productType"/> parameter is set to 
        /// VistaProductType.Unlicensed.
        /// <para>
        /// If the function fails, the return value is zero. This function fails if one of the input parameters is invalid.
        /// </para>
        /// </returns>
        /// <remarks>
        /// This API is only valid on Vista and Longhorn server, or later, installations.
        /// </remarks>
        [DllImport(KERNEL32DLL, SetLastError = true)]
        internal static extern int GetProductInfo
        (
            int majorVersion,
            int minorVersion,
            int servicePackMajorVersion,
            int servicePackMinorVersion,
            ref int productType
        );

        [DllImport(KERNEL32DLL, SetLastError = true)]
        internal static extern bool GetVersionEx([MarshalAs(UnmanagedType.Struct)] ref OSVERSIONINFOEX versionInfo);

        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <returns></returns>
        [DllImportAttribute("Kernel32.dll", SetLastError = true)]
        static internal extern Int32 GetLastError();

        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="LCID"></param>
        /// <param name="LCTYPE"></param>
        /// <param name="info"></param>
        /// <param name="infoSize"></param>
        /// <returns></returns>
        [DllImportAttribute("kernel32.dll", SetLastError = true)]
        static internal extern int GetLocaleInfo(uint LCID, uint LCTYPE, StringBuilder info, int infoSize);


        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="ConditionMask"></param>
        /// <param name="TypeMask"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        [SecuritySafeCritical]
        [SuppressUnmanagedCodeSecurity]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public static extern ulong VerSetConditionMask(ulong conditionMask, uint typeMask, byte condition);

        #endregion
    }
}
