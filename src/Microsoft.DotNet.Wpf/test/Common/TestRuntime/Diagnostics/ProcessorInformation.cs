// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Test.Win32;
using System.Security.Permissions;
using System.Diagnostics;

namespace Microsoft.Test.Diagnostics
{
    // Get various system info    
    internal class ProcessorInformation
    {
        #region Private Data

        private BinaryArchitecture binaryArchitecture;
        private ProcessorArchitecture processorArchitecture;

        private delegate int GetAssemblyIdentityFromFileDelegate([MarshalAs(UnmanagedType.LPWStr)]string filePath, [MarshalAs(UnmanagedType.LPStruct)]Guid refiid, [MarshalAs(UnmanagedType.IUnknown)]out object identity);
        private static readonly ProcessorInformation current = new ProcessorInformation();

        #endregion

        #region Constructor

        private ProcessorInformation()
        {
            // We should not use Environment.GetEnvironmentVariable method to query the value of %PROCESSOR_ARCHITECTURE%,
            // as it does not return the real processor architecture if the current process is WOW64. Plus it's not
            // reliable as the environment variable can be modified

            // We need to determine whether the current process is running WOW64. 
            bool isWow64 = false;
            bool isWow64ProcessSucceeded = Kernel32.IsWow64Process(Kernel32.GetCurrentProcess(), ref isWow64);
            Kernel32.SYSTEM_INFO systemInfo = new Kernel32.SYSTEM_INFO();
            if (isWow64ProcessSucceeded)
            {
                if (isWow64)    // For WOW64 process, we nned to call GetNativeSystemInfo in order not to be fooled.
                    Kernel32.GetNativeSystemInfo(ref systemInfo);
                else
                    Kernel32.GetSystemInfo(ref systemInfo);
            }

            if (IntPtr.Size == 8)
            {
                // The current process is 64-bit
                binaryArchitecture = BinaryArchitecture.Native64;
            }
            else if (IntPtr.Size == 4)
            {
                // The current process is 32-bit
                if (isWow64ProcessSucceeded)
                {
                    if (isWow64)
                        binaryArchitecture = BinaryArchitecture.Wow64;
                    else
                        binaryArchitecture = BinaryArchitecture.Native32;
                }
                else
                {
                    binaryArchitecture = BinaryArchitecture.Some32;
                }
            }
            else
            {
                // Pointer size is neither 8 nor 4 - maybe some future or antique system?
                binaryArchitecture = BinaryArchitecture.Unknown;
            }

            if (isWow64ProcessSucceeded)
            {
                switch (systemInfo.uProcessorInfo.wProcessorArchitecture)
                {
                    case Kernel32.PROCESSOR_ARCHITECTURE_UNKNOWN:
                        processorArchitecture = ProcessorArchitecture.None;
                        break;

                    case Kernel32.PROCESSOR_ARCHITECTURE_INTEL:
                        processorArchitecture = ProcessorArchitecture.X86;
                        break;

                    case Kernel32.PROCESSOR_ARCHITECTURE_IA64:
                        processorArchitecture = ProcessorArchitecture.IA64;
                        break;

                    case Kernel32.PROCESSOR_ARCHITECTURE_AMD64:
                        processorArchitecture = ProcessorArchitecture.Amd64;
                        break;

                    default:
                        // We don't support other types.
                        processorArchitecture = ProcessorArchitecture.None;
                        break;
                }
            }
            else
            {
                processorArchitecture = ProcessorArchitecture.None;
            }
        }

        #endregion

        #region Public Members

        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return processorArchitecture; }
        }

        public BinaryArchitecture BinaryArchitecture
        {
            get { return binaryArchitecture; }
        }

        /// <summary>
        /// returns true if the current process is running in WOW64 mode, otherwise, false
        /// </summary>
        public bool IsWow64Process
        {
            get
            {
                bool isWow = false;
                if (!Kernel32.IsWow64Process(Process.GetCurrentProcess().Handle, ref isWow))
                {
                    //      For some reason this API sometimes returns an error code
                    //      I havent found any documentation indicating what this may mean
                    //      however, it appears to only happen under conditions where the
                    //      the process is not wow 64 so we will assume that this means false
                    isWow = false;
                }
                return isWow;
            }
        }

        /// <summary>
        /// Sets the Process to be DPI Aware
        /// </summary>
        /// <remarks>
        /// You should call this API before using GetDeviceCaps if you want to know the actual
        /// System DPI. This API does nothing on builds downlevel from longhorn.
        /// </remarks>
        public void SetProcessDpiAware()
        {
            if (Environment.OSVersion.Version.Major >= 6) //Longorn
                User32.SetProcessDpiAware();
        }

        #endregion

        #region Static Members

        public static ProcessorInformation Current
        {
            get { return current; }
        }

        #endregion

        #region Private Members

        //Allows you to detect the architecture of the binary, which is useful
        //when in WOW mode.
        private static BinaryInfo GetBinaryInfo(string fullPath)
        {
            // See the following blogs for the reliable ways to detect whether a binary is managed assembly, and its 
            // processor architecture (PA).
            // http://blogs.msdn.com/junfeng/archive/2004/02/06/68334.aspx
            // http://blogs.msdn.com/junfeng/archive/2005/10/05/477247.aspx
            // http://blogs.msdn.com/junfeng/archive/2004/08/24/219691.aspx
            //
            // In general, we can use Assembly.LoadXXX or Assembly.ReflectionOnlyLoadXXX APIs and catch BadImageFormatException. 
            // But there are cases that BadImageFormatException can be thrown on managed binaries 
            // (see http://blogs.msdn.com/junfeng/archive/2004/08/24/219691.aspx). So to accurately pinpoint the reason that
            // BadImageFormatException is thrown, we need to check its HResult property. only if the HResult is 
            // 0x80131018(COR_E_ASSEMBLYEXPECTED, means manifest cannot be found) then we know for sure that the binary is not managed.
            //
            // However we cannot use this technique to determine the PA. As mentioned above, there're cases that managed 
            // assemblies can trigger exceptions on Amssembly.Load methods thus PA cannot be retrieved. In order to retrieve PA we need to call
            // unmanaged CLR API "GetAssemblyIdentityFromFile". Note this function also returns COR_E_ASSEMBLYEXPECTED
            // if the binary is not managed. So we can use this API to get both the managed-ness and PA.

            BinaryInfo result = new BinaryInfo();
            IntPtr funcAddress;
            int hresult = NativeMethods.GetRealProcAddress("GetAssemblyIdentityFromFile", out funcAddress);
            if (hresult >= 0)
            {
                // Now we want to get the processorArcitecture attribute.
                // Another way to get processorArchitecture is to get the full display name by using IIdentityAuthority interfacte
                // (see http://blogs.msdn.com/junfeng/archive/2005/09/13/465373.aspx). But this requires us to import a lot of CLR
                // COM interfaces. We could do it by first use midl.exe to compile isolation.idl (found in %SDXROOT%\basw\wcp\inc0
                // to a tlb then use tlbimp to generate the managed signatures for us.
                object identityObject = null;
                NativeMethods.IEnumIDENTITY_ATTRIBUTE asmAttrEnumerator = null;
                try
                {
                    GetAssemblyIdentityFromFileDelegate getAsmIdentityDelegate =
                        (GetAssemblyIdentityFromFileDelegate)Marshal.GetDelegateForFunctionPointer(funcAddress, typeof(GetAssemblyIdentityFromFileDelegate));
                    hresult = getAsmIdentityDelegate(fullPath, NativeMethods.IID_IReferenceIdentity, out identityObject);
                    if (hresult != NativeMethods.COR_E_ASSEMBLYEXPECTED)
                    {
                        // Managed assembly
                        NativeMethods.IReferenceIdentity identity = (NativeMethods.IReferenceIdentity)identityObject;
                        asmAttrEnumerator = identity.EnumAttributes();
                        int fetchCount = 5;
                        NativeMethods.IDENTITY_ATTRIBUTE[] attributes = new NativeMethods.IDENTITY_ATTRIBUTE[fetchCount];
                        UIntPtr count = UIntPtr.Zero;
                        do
                        {
                            asmAttrEnumerator.Next((UIntPtr)fetchCount, attributes, out count);
                            foreach (NativeMethods.IDENTITY_ATTRIBUTE attribute in attributes)
                            {
                                if (attribute.Name == "processorArchitecture")
                                {
                                    result.IsManaged = true;
                                    result.ProcessorArchitecture = (ProcessorArchitecture)Enum.Parse(typeof(ProcessorArchitecture), attribute.Value, true);
                                    return result;
                                }
                            }
                        }
                        while (count == (UIntPtr)fetchCount);
                    }
                }
                finally
                {
                    if (identityObject != null)
                    {
                        Marshal.ReleaseComObject(identityObject);
                    }
                    if (asmAttrEnumerator != null)
                    {
                        Marshal.ReleaseComObject(asmAttrEnumerator);
                    }
                }
            }

            // We're here because fullPath is an unmanaged binary
            result.IsManaged = false;
            using (Stream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fs);

                try
                {
                    // PE file starts with DOS header
                    // Verify the passed in file indeed has a DOS header
                    ushort dosMagic = reader.ReadUInt16();
                    if (dosMagic != Kernel32.IMAGE_DOS_SIGNATURE) // DOS header starts with 0x5A4D
                    {
                        // Not a PE file
                        return null;
                    }

                    // Read e_lfanew field to get the starting location of PE header
                    int lfanewOffset = Marshal.OffsetOf(typeof(Kernel32.IMAGE_DOS_HEADER), "e_lfanew").ToInt32();
                    fs.Position = lfanewOffset;

                    int peHeaderOffset = reader.ReadInt32();

                    //Moving to PE Header start location...
                    fs.Position = peHeaderOffset;

                    uint peHeaderSignature = reader.ReadUInt32();
                    if (peHeaderSignature != Kernel32.IMAGE_NT_SIGNATURE)
                    {
                        // Not a PE file
                        return null;
                    }

                    // We are at the beginning "FileHeader" field in IMAGE_NT_HEADERS or IMAGE_NT_HEADERS64
                    ushort machine = reader.ReadUInt16();
                    switch (machine)
                    {
                        case Kernel32.IMAGE_FILE_MACHINE_I386:
                            result.ProcessorArchitecture = ProcessorArchitecture.X86;
                            break;
                        case Kernel32.IMAGE_FILE_MACHINE_IA64:
                            result.ProcessorArchitecture = ProcessorArchitecture.IA64;
                            break;
                        case Kernel32.IMAGE_FILE_MACHINE_AMD64:
                            result.ProcessorArchitecture = ProcessorArchitecture.Amd64;
                            break;
                        case Kernel32.IMAGE_FILE_MACHINE_UNKNOWN:
                            result.ProcessorArchitecture = ProcessorArchitecture.None;
                            break;
                        default:
                            result.ProcessorArchitecture = ProcessorArchitecture.None;
                            break;
                    }
                }
                catch (EndOfStreamException)
                {
                    // Not a PE file
                    return null;
                }

                return result;
            }

        }

        #endregion
    }

    internal class BinaryInfo
    {
        #region Private Data

        ProcessorArchitecture processorArchitecture;
        private bool isManaged;

        #endregion

        #region Public Members

        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return processorArchitecture; }
            internal set { processorArchitecture = value; }
        }
        
        public bool IsManaged
        {
            get { return isManaged; }
            internal set { isManaged = value; }
        }

        #endregion
    }

    internal enum BinaryArchitecture
    {
        Unknown,
        Native32,
        Native64,
        Wow64,
        Some32, // A 32-bit process but we cannot determine whether it's native-32 or wow64
    }

    internal static class NativeMethods
    {
        #region Unmanaged CLR interfaces

        internal const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);

        [DllImport("mscoree.dll")]
        internal static extern int GetRealProcAddress([MarshalAs(UnmanagedType.LPStr)]string procName, out IntPtr funcAddress);

        [Guid("9cdaae75-246e-4b00-a26d-b9aec137a3eb")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IEnumIDENTITY_ATTRIBUTE
        {
            void Next(
                UIntPtr celt,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)][In, Out] IDENTITY_ATTRIBUTE[] rgAttributes,
                  out UIntPtr pceltWritten);
            void CurrentIntoBuffer(
                UIntPtr cbAvailable,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)][In, Out] byte[] pbData,
                  out UIntPtr pcbUsed);

            void Skip(UIntPtr celt);
            void Reset();
            IEnumIDENTITY_ATTRIBUTE Clone();
            UIntPtr CurrentSize();
        };

        internal static readonly Guid IID_IReferenceIdentity = new Guid("6eaf5ace-7917-4f3c-b129-e046a9704766");
        [Guid("6eaf5ace-7917-4f3c-b129-e046a9704766")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IReferenceIdentity
        {
            string GetAttribute(
                string fusionNamespace,
                string name
                );
            void SetAttribute(
                string fusionNamespace,
                string name,
                string value
                );
            IEnumIDENTITY_ATTRIBUTE EnumAttributes();
            IReferenceIdentity Clone(
                UIntPtr cDeltas,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]IDENTITY_ATTRIBUTE[] rgDeltas
                );
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct IDENTITY_ATTRIBUTE
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Namespace;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Value;
        };

        #endregion
    }
}
