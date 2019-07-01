// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Microsoft.Test.Filtering
{
    /// <summary>
    /// A simple singleton for caching OS Version info.
    /// This isolates our interop code. Similar wrappers exist on TestRuntime.
    /// If we choose to consolidate later, it would be totally cool to gut this out.
    /// </summary>
    internal class OSVersionInfo
    {

        internal static OSProductType ProductType
        {
            get{
            if(current==null)
            {
                current=new OSVersionInfo();
            }            
            return (OSProductType) current.info.ProductType;
            }
        }
        
        private static OSVersionInfo current=null;
        private OSVERSIONINFOEX info;

        private OSVersionInfo() 
        {
            info = Initialize();
        }

        private static OSVERSIONINFOEX Initialize()
        {
            OSVERSIONINFOEX versionInfo = new OSVERSIONINFOEX();
            versionInfo.Size = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            if (!Kernel32Helper.GetVersionEx(ref versionInfo))
                throw new Win32Exception();

            return versionInfo;
        }
    }

    /// <summary>
    /// Defines the OS product type retported by ProductType.
    /// </summary>
    internal enum OSProductType : byte
    {
        /// <summary>
        /// The value has not been intialized or could not be retrieved.
        /// </summary>
        None,

        /// <summary>
        /// The operating system is Windows Vista, Windows XP Professional, Windows XP Home Edition, Windows 2000 Professional, or Windows NT Workstation 4.0.
        /// </summary>
        Workstation = 1,

        /// <summary>
        /// The system is a domain controller.
        /// </summary>
        DomainController = 2,

        /// <summary>
        /// The system is a server.
        /// </summary>
        /// <remarks>
        /// Note that a server that is also a domain controller is reported as <see cref="DomainController"/>, not <see cref="Server"/>.
        /// </remarks>
        Server = 3
    }

    
    internal class Kernel32Helper
    {
        private const string KERNEL32DLL = "Kernel32.dll";

        [DllImport(KERNEL32DLL, SetLastError = true)]
        internal static extern bool GetVersionEx([MarshalAs(UnmanagedType.Struct)] ref OSVERSIONINFOEX versionInfo);
    }   

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
}