// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal.OSVersionHelper.NativeConstants;

namespace Microsoft.Internal.OSVersionHelper
{
    /// <summary>
    /// Exposes methods equivalent to VersionHelper API's exposed 
    /// by the Win32 versionhelpers.h header file, and provides
    /// augmented methods to distinguish major baseline versions 
    /// of Windows 10. 
    /// </summary>
    public static class VersionHelper
    {
        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows XP version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows XP version; otherwise, False.</returns>
        public static bool IsWindowsXPOrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows XP with Service Pack 1 (SP1) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows XP with SP1 version; otherwise, False.</returns>
        public static bool IsWindowsXPSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                1);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows XP with Service Pack 2 (SP2) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows XP with SP2 version number; otherwise, False.</returns>
        public static bool IsWindowsXPSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                2);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows XP with Service Pack 3 (SP3) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows XP with SP3 version; otherwise, False.</returns>
        public static bool IsWindowsXPSP3OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WINXP), 
                3);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows Vista version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows Vista version; otherwise, False.</returns>
        /// <remarks>
        /// Do not use this method for Windows Server 2008 related tests. Windows Server 2008 released along 
        /// with Windows Vista SP1. Also see <see cref="IsWindowsVistaSP1OrGreater"/>. 
        /// </remarks>
        public static bool IsWindowsVistaOrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows Vista with Service Pack 1 (SP1) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows Vista with SP1 version; otherwise, False.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows Vista SP1 and Windows Server 2008 share 
        /// the same version and service pack numbers - i.e., "RTM" and "SP1" for Windows Server 2008 are the same builds. 
        /// To create an equivalent test for Windows Server 2008, call <see cref="IsWindowsVistaSP1OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindowsVistaSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                1);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows Vista with Service Pack 2 (SP2) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows Vista with SP2 version; otherwise, False.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows Vista SP2 and Windows Server 2008 SP2 share 
        /// the same version and service pack numbers. To create an equivalent test for Windows Server 2008 SP2, 
        /// call <see cref="IsWindowsVistaSP2OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindowsVistaSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_VISTA), 
                2);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 7 version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 7 version; otherwise, False.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows 7 and Windows Server 2008 R2 share 
        /// the same version numbers. To create an equivalent test for Windows Server 2008 R2, 
        /// call <see cref="IsWindows7OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindows7OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN7), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN7), 
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 7 with Service Pack 1 (SP1) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 7 with SP1 version; otherwise, false.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows 7 SP1 and Windows Server 2008 R2 SP1 share 
        /// the same version numbers and service pack number. To create an equivalent test for Windows Server 2008 R2 SP1, 
        /// call <see cref="IsWindows7SP1OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindows7SP1OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN7), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN7), 
                1);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 7 with Service Pack 1 (SP1) version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 7 with SP1 version; otherwise, False.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows 8 and Windows Server 2012 share 
        /// the same version numbers. To create an equivalent test for Windows Server 2012, 
        /// call <see cref="IsWindows8OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindows8OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN8), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN8), 
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 8.1 version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 8.1 version; otherwise, False.</returns>
        /// <remarks>
        /// This function does not differentiate between client and server releases. Windows 8.1 and Windows Server 2012 R2 share 
        /// the same version numbers. To create an equivalent test for Windows Server 2012 R2, 
        /// call <see cref="IsWindows8Point1OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindows8Point1OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WINBLUE), 
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WINBLUE), 
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 10 version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 10 version; otherwise, False.</returns>
        /// <remarks>
        /// This method will return True for some pre-RTM builds of Windows 10
        /// 
        /// This function does not differentiate between client and server releases. Windows 10 and 
        /// Windows Server 2016 Technical Preview share the same version numbers. To create an equivalent test 
        /// for Windows Server 2016 Technical Preview, call <see cref="IsWindows10OrGreater"/>() &amp;&amp; <see cref="IsWindowsServer"/>()
        /// </remarks>
        public static bool IsWindows10OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                0);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 10 RTM "Threshold 1" version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 10 "Threshold 1" version; otherwise, False.</returns>
        /// <remarks>
        /// This method is not intended to differentiate between specific Technical Preview builds of Windows Server 2016. 
        /// </remarks>
        public static bool IsWindows10TH1OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                0,
                Win10BuildNumbers._TH1_BUILD_NUMBER);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 10 "Threshold 2" version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 10 "Threshold 2" version; otherwise, False.</returns>
        /// <remarks>
        /// This method is not intended to differentiate between specific Technical Preview builds of Windows Server 2016. 
        /// </remarks>
        public static bool IsWindows10TH2OrGreater()
        {
            return IsWindowsVersionOrGreater(
                Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10),
                0,
                Win10BuildNumbers._TH2_BUILD_NUMBER);
        }

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the Windows 10 "Redstone 1" version.
        /// </summary>
        /// <returns>True if the current OS version matches, or is greater than, the Windows 10 "Redstone 1" version; otherwise, False.</returns>
        /// <remarks>
        /// This method is not intended to differentiate between specific Technical Preview builds of Windows Server 2016. 
        /// 
        /// RS1 will have a build number strictly greater than _TH2_BUILD_NUMBER.
        /// Once the precise build number for RS1 RTM is known, NativeConstants._RS1_BUILD_NUMBER
        /// should be defined, and the following implmentation should be updated to something like this: 
        /// 
        ///     return IsWindowsVersionOrGreater(
        ///             Util.HIBYTE(NativeConstants._WIN32_WINNT_WIN10),
        ///             Util.LOBYTE(NativeConstants._WIN32_WINNT_WIN10), 
        ///             0, 
        ///             NativeConstants._RS1_BUILD_NUMBER);
        /// 
        /// In addition to updating the following implementation, a new method - IsWindows10RS2OrGreater - 
        /// should be added along the lines of the implementation below.
        /// </remarks>
        public static bool IsWindows10RS1OrGreater()
        {
            var osvi = new NativeTypes.OSVERSIONINFOEX();
            osvi.dwMajorVersion = Util.HIBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10);
            osvi.dwMinorVersion = Util.LOBYTE(Win32WinNTConstants._WIN32_WINNT_WIN10);
            osvi.dwBuildNumber = Win10BuildNumbers._TH2_BUILD_NUMBER;


            ulong dwlConditionMask = 0;
            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask, 
                TypeBitMasks.VER_MAJORVERSION, 
                ConditionMasks.VER_GREATER_EQUAL);

            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask, 
                TypeBitMasks.VER_MINORVERSION, 
                ConditionMasks.VER_GREATER_EQUAL);

            // Build number should be strictly greater than than of TH2 - use VER_GREATER
            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask, 
                TypeBitMasks.VER_BUILDNUMBER, 
                ConditionMasks.VER_GREATER);

            uint dwFlags =
                TypeBitMasks.VER_MAJORVERSION |
                TypeBitMasks.VER_MINORVERSION |
                TypeBitMasks.VER_BUILDNUMBER;

            return
                NativeMethods.VerifyVersionInfo(osvi, dwFlags, dwlConditionMask);
        }

        /// <summary>
        /// Indicates if the current OS is a Windows Server release. Applications that need to distinguish 
        /// between server and client versions of Windows should call this function.
        /// </summary>
        /// <returns>True if the current OS is a Windows Server version; otherwise, False.</returns>
        public static bool IsWindowsServer()
        {
            var osvi = new NativeTypes.OSVERSIONINFOEX();
            ulong dwlConditionMask = 0;
            osvi.wProductType = ProductTypes.VER_NT_WORKSTATION;
            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask,
                TypeBitMasks.VER_PRODUCT_TYPE,
                ConditionMasks.VER_EQUAL);

            return
                !NativeMethods.VerifyVersionInfo(osvi, TypeBitMasks.VER_PRODUCT_TYPE, dwlConditionMask);
        }

        #region Private methods

        /// <summary>
        /// Indicates if the current OS version matches, or is greater than, the OS version 
        /// specified through <paramref name="wMajorVersion"/>, <paramref name="wMinorVersion"/>, 
        /// <paramref name="wServicePackMajor"/> and <paramref name="wBuildNumber"/>. 
        /// </summary>
        /// <param name="wMajorVersion">Major version</param>
        /// <param name="wMinorVersion">Minor version</param>
        /// <param name="wServicePackMajor">Service Pack's major version number</param>
        /// <param name="wBuildNumber">Build number of the OS</param>
        /// <returns>True if the current OS version matches, or is greater than, the OS version
        /// specified by the parameters, otherwise, False.</returns>
        private static bool IsWindowsVersionOrGreater(
            uint wMajorVersion,
            uint wMinorVersion,
            ushort wServicePackMajor,
            uint wBuildNumber = 0)
        {
            var osvi = new NativeTypes.OSVERSIONINFOEX();
            osvi.dwMajorVersion = wMajorVersion;
            osvi.dwMinorVersion = wMinorVersion;




            ulong dwlConditionMask = 0;
            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask,
                TypeBitMasks.VER_MAJORVERSION,
                ConditionMasks.VER_GREATER_EQUAL);
            NativeMethods.VER_SET_CONDITION(
                ref dwlConditionMask,
                TypeBitMasks.VER_MINORVERSION,
                ConditionMasks.VER_GREATER_EQUAL);

            uint dwFlags =
                TypeBitMasks.VER_MAJORVERSION |
                TypeBitMasks.VER_MINORVERSION;


            if (wServicePackMajor > 0)
            {
                osvi.wServicePackMajor = wServicePackMajor;
                NativeMethods.VER_SET_CONDITION(
                    ref dwlConditionMask,
                    TypeBitMasks.VER_SERVICEPACKMAJOR,
                    ConditionMasks.VER_GREATER_EQUAL);
                dwFlags |= TypeBitMasks.VER_SERVICEPACKMAJOR;
            }

            if (wBuildNumber > 0)
            {
                osvi.dwBuildNumber = wBuildNumber;
                NativeMethods.VER_SET_CONDITION(
                    ref dwlConditionMask,
                    TypeBitMasks.VER_BUILDNUMBER,
                    ConditionMasks.VER_GREATER_EQUAL);
                dwFlags |= TypeBitMasks.VER_BUILDNUMBER;
            }

            return NativeMethods.VerifyVersionInfo(osvi, dwFlags, dwlConditionMask);
        }

        #endregion // Private methods
    }
}
