// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Permissions;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// EnvironmentVariable
    /// </summary>
    [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class EnvironmentVariable
    {
        internal static string Get(string var)
        {
            return(Environment.GetEnvironmentVariable(var));
        }

        internal static void Set(string var, string val)
        {
            Environment.SetEnvironmentVariable(var, val);
        }

        internal static string CommandLine
        {
            get
            {
                return (Environment.CommandLine);
            }
        }
    }

    /// <summary>
    /// OSVersion
    /// </summary>
    [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
    public static class OSVersion
    {
        /// <summary>
        /// Available versions
        /// </summary>
        internal struct WindowsName
        {
            internal const string WindowsXP         = "WindowsXP";
            internal const string WindowsVista      = "WindowsVista";
            internal const string Windows7          = "Windows7";
            internal const string Windows8          = "Windows8";
            internal const string Windows8Point1    = "Windows8.1";
            internal const string Windows10         = "Windows10";

            internal const string WindowsServer2003   = "WindowsServer2003";
            internal const string WindowsServer2003R2 = "WindowsServer2003R2";
            internal const string WindowsServer2008   = "WindowsServer2008";   // Vista/Longhorn Server
            internal const string WindowsServer2008R2 = "WindowsServer2008R2"; // Win7 server
            internal const string WindowsServer2012   = "WindowsServer2012";   // Win8 server
            internal const string WindowsServer2012R2 = "WindowsServer2012R2"; // Win8.1 server
            internal const string WindowsServer2016   = "WindowsServer2016";   // Redstone / Win10 Server

            internal const string Unknown = "Unknown";
        }

        /// <summary>
        /// Name
        /// </summary>
        public static string Name
        {
            get
            {
                return (GetOSName().ToString());
            }
        }

        private static bool IsWindowsVersionOrGreater(uint majorVersion, uint minorVersion, ushort servicePackMajor)
        {
            var osvi = new NativeStructs.RTL_OSVERSIONINFOEXW(0);

            ulong conditionMask =
                Kernel32.VerSetConditionMask
                (
                    Kernel32.VerSetConditionMask
                    (
                        Kernel32.VerSetConditionMask
                        (
                            0,
                            NativeConstants.VER_MAJORVERSION,
                            NativeConstants.VER_GREATER_EQUAL
                        ),
                        NativeConstants.VER_MINORVERSION,
                        NativeConstants.VER_GREATER_EQUAL
                    ),
                    NativeConstants.VER_SERVICEPACKMAJOR,
                    NativeConstants.VER_GREATER_EQUAL
                );

            osvi.dwMajorVersion = majorVersion;
            osvi.dwMinorVersion = minorVersion;
            osvi.wServicePackMajor = servicePackMajor;

            return (NtDll.RtlVerifyVersionInfo(ref osvi, NativeConstants.VER_MAJORVERSION | NativeConstants.VER_MINORVERSION | NativeConstants.VER_SERVICEPACKMAJOR, conditionMask) == NativeConstants.STATUS_SUCCESS);
        }

        public static bool IsWindowsXPOrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WINXP), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WINXP), 0);
        }

        public static bool IsWindowsXPSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WINXP), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WINXP), 1);
        }

        public static bool IsWindowsXPSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WINXP), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WINXP), 2);
        }

        public static bool IsWindowsXPSP3OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WINXP), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WINXP), 3);
        }

        public static bool IsWindowsVistaOrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_VISTA), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_VISTA), 0);
        }

        public static bool IsWindowsVistaSP1OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_VISTA), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_VISTA), 1);
        }

        public static bool IsWindowsVistaSP2OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_VISTA), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_VISTA), 2);
        }

        public static bool IsWindows7OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WIN7), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WIN7), 0);
        }

        public static bool IsWindows7SP1OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WIN7), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WIN7), 1);
        }

        public static bool IsWindows8OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WIN8), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WIN8), 0);
        }

        public static bool IsWindows8Point1OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WINBLUE), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WINBLUE), 0);
        }

        public static bool IsWindows10OrGreater()
        {
            return IsWindowsVersionOrGreater(NativeMethods.HIBYTE(NativeConstants._WIN32_WINNT_WIN10), NativeMethods.LOBYTE(NativeConstants._WIN32_WINNT_WIN10), 0);
        }

        public static bool IsWindowsServer()
        {
            var osvi = new NativeStructs.RTL_OSVERSIONINFOEXW(0);

            osvi.wProductType = NativeConstants.VER_NT_WORKSTATION;

            ulong conditionMask = Kernel32.VerSetConditionMask(0, NativeConstants.VER_PRODUCT_TYPE, NativeConstants.VER_EQUAL);

            var result = NtDll.RtlVerifyVersionInfo(ref osvi, NativeConstants.VER_PRODUCT_TYPE, conditionMask);
            bool clientOS = (result == NativeConstants.STATUS_SUCCESS);

            return !clientOS;
        }

        /// <summary>
        /// GetOSName
        /// </summary>
        /// <returns></returns>
        private static string GetOSName()
        {
            if (xpOrGreater && !vistaOrGreater)
            {
                if (server) return WindowsName.WindowsServer2003;
                return WindowsName.WindowsXP;
            }
            else if (vistaOrGreater && !win7OrGreater)
            {
                if (server) return WindowsName.WindowsServer2008;
                return WindowsName.WindowsVista;
            }
            else if (win7OrGreater && !win8OrGreater)
            {
                if (server) return WindowsName.WindowsServer2008R2;
                return WindowsName.Windows7;
            }
            else if (win8OrGreater && !win8Point1OrGreater)
            {
                if (server) return WindowsName.WindowsServer2012;
                return WindowsName.Windows8;
            }
            else if (win8Point1OrGreater && !win10OrGreater)
            {
                if (server) return WindowsName.WindowsServer2012R2;
                return WindowsName.Windows8Point1;
            }
            else if (win10OrGreater)
            {
                if (server) return WindowsName.WindowsServer2016;
                return WindowsName.Windows10;
            }
            else
            {
                return WindowsName.Unknown;
            }
        }

        private static bool server = IsWindowsServer();
        private static bool xpOrGreater = IsWindowsXPOrGreater();
        private static bool vistaOrGreater = IsWindowsVistaOrGreater();
        private static bool win7OrGreater = IsWindows7OrGreater();
        private static bool win8OrGreater = IsWindows8OrGreater();
        private static bool win8Point1OrGreater = IsWindows8Point1OrGreater();
        private static bool win10OrGreater = IsWindows10OrGreater();

    }
}