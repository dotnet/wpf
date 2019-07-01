// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Internal.OSVersionHelper.NativeConstants
{
    /// <summary>
    /// Masks that indicate the member of <see cref="NativeTypes.OSVERSIONINFOEX"/> whose comparision 
    /// type is being set. This value corresponds to one of the bits specified in the dwTypeMask parameter
    /// for the <see cref="NativeMethods.VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> function.
    /// </summary>
    static class TypeBitMasks
    {
        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.dwMinorVersion"/>
        /// </summary>
        internal const ushort VER_MINORVERSION      = 0x00000001;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.dwMajorVersion"/>
        /// </summary>
        internal const ushort VER_MAJORVERSION      = 0x00000002;

        /// <summary>
        /// <see cref=" NativeTypes.OSVERSIONINFOEX.dwBuildNumber"/>
        /// </summary>
        internal const ushort VER_BUILDNUMBER       = 0x00000004;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.dwPlatformId"/>
        /// </summary>
        internal const ushort VER_PLATFORMID        = 0x00000008;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.wServicePackMinor"/>
        /// </summary>
        internal const ushort VER_SERVICEPACKMINOR  = 0x00000010;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.wServicePackMajor"/>
        /// </summary>
        internal const ushort VER_SERVICEPACKMAJOR  = 0x00000020;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.wSuiteMask"/>
        /// </summary>
        internal const ushort VER_SUITENAME         = 0x00000040;

        /// <summary>
        /// <see cref="NativeTypes.OSVERSIONINFOEX.wProductType"/>
        /// </summary>
        internal const ushort VER_PRODUCT_TYPE      = 0x00000080;
    }

    /// <summary>
    /// The operator used for comparision in <see cref="NativeMethods.VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>. 
    /// This operator is used to compare a specified attribute value to the corresponding value for 
    /// the currently running system. 
    /// </summary>
    /// <remarks>
    /// When dwTypeMask is <see cref="TypeBitMasks.VER_SUITENAME"/>, only <see cref="VER_AND"/> or 
    /// <see cref="VER_OR"/> can be used.
    /// </remarks>
    static class ConditionMasks
    {
        /// <summary>
        /// The current value must be equal to the specified value.
        /// </summary>
        internal const byte VER_EQUAL           = 1;

        /// <summary>
        /// The current value must be greater than the specified value.
        /// </summary>
        internal const byte VER_GREATER         = 2;

        /// <summary>
        /// The current value must be greater than or equal to the specified value.
        /// </summary>
        internal const byte VER_GREATER_EQUAL   = 3;

        /// <summary>
        /// The current value must be less than the specified value.
        /// </summary>
        internal const byte VER_LESS            = 4;

        /// <summary>
        /// The current value must be less than or equal to the specified value.
        /// </summary>
        internal const byte VER_LESS_EQUAL      = 5;

        /// <summary>
        /// If dwTypeBitMask is <see cref="TypeBitMasks.VER_SUITENAME"/>, then all product
        /// suites specified in the <see cref="NativeTypes.OSVERSIONINFOEX.wSuiteMask"/> member
        /// must be present in the current system.
        /// </summary>
        internal const byte VER_AND             = 6;

        /// <summary>
        /// If dwTypeBitMask is <see cref="TypeBitMasks.VER_SUITENAME"/>, then at least one of the
        /// product suites specified in the <see cref="NativeTypes.OSVERSIONINFOEX.wSuiteMask"/> member
        /// must be present in the current system.
        /// </summary>
        internal const byte VER_OR              = 7;
    }

    /// <summary>
    /// A bit mask that identifies the product suites available on the system.
    /// </summary>
    static class SuiteMasks
    {
        /// <summary>
        /// Microsoft Small Business Server was once installed on the system, but may have 
        /// been upgraded to another version of Windows. 
        /// Refer to the Remarks section for more information about this bit flag.
        /// </summary>
        internal const ushort VER_SUITE_SMALLBUSINESS               = 0x00000001;

        /// <summary>
        /// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or 
        /// Windows 2000 Advanced Server is installed. 
        /// Refer to the Remarks section for more information about this bit flag.
        /// </summary>
        internal const ushort VER_SUITE_ENTERPRISE                  = 0x00000002;

        /// <summary>
        /// Microsoft BackOffice components are installed.
        /// </summary>
        internal const ushort VER_SUITE_BACKOFFICE                  = 0x00000004;

        /// <summary>
        /// Terminal Services is installed. This value is always set. 
        /// If VER_SUITE_TERMINAL is set but <see cref="VER_SUITE_SINGLEUSERTS"/> is not set, 
        /// the system is running in application server mode.
        /// </summary>
        internal const ushort VER_SUITE_TERMINAL                    = 0x00000010;

        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive client license in force. 
        /// Refer to the Remarks section for more information about this bit flag.
        /// </summary>
        internal const ushort VER_SUITE_SMALLBUSINESS_RESTRICTED    = 0x00000020;

        /// <summary>
        /// Windows XP Embedded is installed.
        /// </summary>
        internal const ushort VER_SUITE_EMBEDDEDNT                  = 0x00000040;

        /// <summary>
        /// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, 
        /// or Windows 2000 Datacenter Server is installed.
        /// </summary>
        internal const ushort VER_SUITE_DATACENTER                  = 0x00000080;

        /// <summary>
        /// Remote Desktop is supported, but only one interactive session is supported. 
        /// This value is set unless the system is running in application server mode.
        /// </summary>
        internal const ushort VER_SUITE_SINGLEUSERTS                = 0x00000100;

        /// <summary>
        /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed.
        /// </summary>
        internal const ushort VER_SUITE_PERSONAL                    = 0x00000200;

        /// <summary>
        /// Windows Server 2003, Web Edition is installed.
        /// </summary>
        internal const ushort VER_SUITE_BLADE                       = 0x00000400;

        /// <summary>
        /// Windows Storage Server 2003 R2 or Windows Storage Server 2003 is installed.
        /// </summary>
        internal const ushort VER_SUITE_STORAGE_SERVER              = 0x00002000;

        /// <summary>
        /// Windows Server 2003, Compute Cluster Edition is installed.
        /// </summary>
        internal const ushort VER_SUITE_COMPUTE_SERVER              = 0x00004000;

        /// <summary>
        /// Windows Home Server is installed.
        /// </summary>
        internal const ushort VER_SUITE_WH_SERVER                   = 0x00008000;
    }

    /// <summary>
    /// Describes additional information about the product type of the system.
    /// </summary>
    static class ProductTypes
    {
        /// <summary>
        /// The operating system is Windows 8, Windows 7, Windows Vista, Windows XP Professional, 
        /// Windows XP Home Edition, or Windows 2000 Professional.
        /// </summary>
        internal const byte VER_NT_WORKSTATION          = 0x0000001;

        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server 2012 , 
        /// Windows Server 2008 R2, Windows Server 2008, Windows Server 2003, or Windows 2000 Server.
        /// </summary>
        internal const byte VER_NT_DOMAIN_CONTROLLER    = 0x0000002;

        /// <summary>
        /// The operating system is Windows Server 2012, Windows Server 2008 R2, Windows Server 2008, 
        /// Windows Server 2003, or Windows 2000 Server. 
        /// </summary>
        /// <remarks>
        /// Note that a server that is also a domain controller is reported as VER_NT_DOMAIN_CONTROLLER, 
        /// not VER_NT_SERVER.
        /// </remarks>
        internal const byte VER_NT_SERVER               = 0x0000003;
    }

    /// <summary>
    /// _WIN32_WINNT_* constants from &lt;sdkddkver.h&gt;
    /// </summary>
    static class Win32WinNTConstants
    {
        /// <summary>
        /// Windows NT 4
        /// </summary>
        internal const ushort _WIN32_WINNT_NT4      = 0x0400;

        /// <summary>
        /// Windows 2000
        /// </summary>
        internal const ushort _WIN32_WINNT_WIN2K    = 0x0500;

        /// <summary>
        /// Windows XP
        /// </summary>
        internal const ushort _WIN32_WINNT_WINXP    = 0x0501;

        /// <summary>
        /// Windows Server 2003, Windows Server 2003 R2, 
        /// Windows XP Professional x64 Edition, Windows Home Server
        /// </summary>
        internal const ushort _WIN32_WINNT_WS03     = 0x0502;

        /// <summary>
        /// Windows Vista
        /// </summary>
        internal const ushort _WIN32_WINNT_VISTA    = 0x0600;

        /// <summary>
        /// Windows Server 2008
        /// </summary>
        internal const ushort _WIN32_WINNT_WS08     = 0x0600;

        /// <summary>
        /// Windows 7, Windows Server 2008 R2
        /// </summary>
        internal const ushort _WIN32_WINNT_WIN7     = 0x0601;

        /// <summary>
        /// Windows 8, Windows Server 2012
        /// </summary>
        internal const ushort _WIN32_WINNT_WIN8     = 0x0602;

        /// <summary>
        /// Windows 8.1, Windows Server 2012 R2
        /// </summary>
        internal const ushort _WIN32_WINNT_WINBLUE  = 0x0603;

        /// <summary>
        /// Windows 10, Windows Server 2016
        /// </summary>
        internal const ushort _WIN32_WINNT_WIN10    = 0x0A00;
    }

    /// <summary>
    /// Build numbers for major Windows 10 releases
    /// </summary>
    static class Win10BuildNumbers
    {
        /// <summary>
        /// Windows 10 RTM "Threshold 1"
        /// </summary>
        internal const uint _TH1_BUILD_NUMBER = 10240;

        /// <summary>
        /// Windows 10 "Threshold 2"
        /// </summary>
        internal const uint _TH2_BUILD_NUMBER = 10586;
    }

    /// <summary>
    /// NTSTATUS constants
    /// </summary>
    static class NtStatus
    { 
        /// <summary>
        /// Success
        /// </summary>
        /// <remarks>
        /// <see cref="NativeMethods.RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> returns 
        /// STATUS_SUCCESS when the specified version matches the currently running version of the operating 
        /// system.
        /// </remarks>
        internal const uint STATUS_SUCCESS            = 0x00000000;

        /// <summary>
        /// The specified version does not match the currently running version of the operating system.
        /// </summary>
        internal const uint STATUS_REVISION_MISMATCH  = 0xC0000059;

        /// <summary>
        /// The input parameters are not valid.
        /// </summary>
        internal const uint STATUS_INVALID_PARAMETER  = 0xC000000D;
    }
}
