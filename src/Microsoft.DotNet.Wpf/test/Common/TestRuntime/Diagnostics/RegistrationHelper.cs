// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.Test.Diagnostics
{
	internal static class RegistrationHelper
    {
        #region Public Members

        public static void RegisterComServer(string filename)
        {
            // For WOW, you must use the regsvr32.exe tool
            if (IsRegSvrNeeded(filename))
                RegSevr32(filename, true);
            else
                // Call the self registering function in the binary
                CallUnmangedFunction(filename, "DllRegisterServer");
        }

        public static void UnregisterComServer(string filename)
        {
            if (IsRegSvrNeeded(filename))
                RegSevr32(filename, false);
            else
                // Call the self un-registering function in the binary
                CallUnmangedFunction(filename, "DllUnregisterServer");
        }

        #endregion

        #region Internal Members

        internal static string GetHash(string filename)
        {
            string hash = string.Empty;
            FileStream fileStream = null;

            try
            {
                fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                SHA1 provider = SHA1CryptoServiceProvider.Create();
                byte[] hashBuffer = provider.ComputeHash(fileStream);
                hash = System.Text.Encoding.ASCII.GetString(hashBuffer);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }

            return hash;
        }

        internal static ImageFileMachine GetBinaryArchitecture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            IMAGE_DOS_HEADER imageDOSHeader;
            IMAGE_NT_HEADERS imageNTHeaders;
            FileStream fileStream = null;

            try
            {
                fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                imageDOSHeader = GetStructFromStream<IMAGE_DOS_HEADER>(fileStream);

                //Verify the file is of a type we can understand
                if (imageDOSHeader.e_magic != IMAGE_DOS_SIGNATURE)
                    return 0;

                fileStream.Seek(imageDOSHeader.e_lfanew, SeekOrigin.Begin);
                imageNTHeaders = GetStructFromStream<IMAGE_NT_HEADERS>(fileStream);

                return (ImageFileMachine)imageNTHeaders.FileHeader.Machine;
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }
        }

        #endregion

        #region Private Members

        private delegate int RegisterServerCallback();

        private static bool IsRegSvrNeeded(string filename)
        {
            // If we are running a 64 bit process and have a 32 bit binary, we cannot load the
            //  library within the current process and must let regsvr32 handle the call

            ImageFileMachine binaryArchitecture = GetBinaryArchitecture(filename);
            string architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").ToUpperInvariant();

            // X86 PROCESS_ARCH is represented by I386 in the enum
            if (architecture == "X86")
                architecture = "I386";

            return (architecture != binaryArchitecture.ToString().ToUpperInvariant());
        }

        private static void RegSevr32(string filename, bool register)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "regsvr32.exe";
            process.StartInfo.Arguments = String.Format("/s {0} \"{1}\"", register == true ? string.Empty : "/u", filename);            
            process.Start();

            // TODO: We may want to consider killing the process if it times out
            //  Run the process
            if (!process.WaitForExit(120000))
            {
                throw new InvalidOperationException("Timed out trying to register " + filename);
            }
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to register " + filename);
            }
        }

        private static void CallUnmangedFunction(string filename, string methodName)
        {
            //      Because we have to load the library this may not work to register
            //      WOW test code.  Need to investigate if we can register 32 and 64 bit
            //      in the same process.

            // Load the library into the process
            IntPtr hModule = LoadLibrary(filename);
            if (hModule == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                // Find the address of the function
                IntPtr hAddress = GetProcAddress(hModule, methodName);
                if (hAddress == IntPtr.Zero)
                    throw new Win32Exception();

                // Convert the pointer into a delegate and call the function.
                RegisterServerCallback callback = Marshal.GetDelegateForFunctionPointer(hAddress, typeof(RegisterServerCallback)) as RegisterServerCallback;
                int retVal = callback();
                if (retVal != 0)
                    throw new Win32Exception("The function " + methodName + " in module " + Path.GetFileName(filename) + " returned failed with error code " + retVal);

            }
            finally
            {
                FreeLibrary(hModule);
                hModule = IntPtr.Zero;
            }
        }

        private static STRUCT_TYPE GetStructFromStream<STRUCT_TYPE>(Stream stream)
        {
            long streamOffset = stream.Position;
            int structSize = 0;
            byte[] structBuffer = null;
            STRUCT_TYPE structObject = default(STRUCT_TYPE); // Returns a struct or class initialized to 0
            GCHandle gcHandle = new GCHandle();

            try
            {
                structObject = Activator.CreateInstance<STRUCT_TYPE>();
                structSize = Marshal.SizeOf(typeof(STRUCT_TYPE));
                structBuffer = new byte[structSize];

                if (stream.Read(structBuffer, 0, structSize) != structSize)
                    return structObject;

                // Read the first DOS struct which points to the location of the second NT struct
                gcHandle = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
                Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), structObject);
            }
            finally
            {
                if (gcHandle.IsAllocated)
                    gcHandle.Free();
            }

            return structObject;
        }

        #endregion

        #region Unmanaged Interop

        // This is slightly different from ImageFileMachine
        //  in that X86 represents I386 but can be used in a

        private const uint IMAGE_DOS_SIGNATURE = 0x5A4D;      // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550;  // PE00

        private const uint IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;

        [StructLayout(LayoutKind.Explicit)]
        internal class IMAGE_DOS_HEADER
        {
            [FieldOffset(0)]
            public ushort e_magic;
            [FieldOffset(60)]
            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal class IMAGE_NT_HEADERS
        {
            [FieldOffset(0)]
            public uint Signature;
            [FieldOffset(4)]
            public IMAGE_FILE_HEADER FileHeader;            
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct IMAGE_FILE_HEADER
        {
            [FieldOffset(0)]
            public ushort Machine;
            [FieldOffset(2)]
            public ushort NumberOfSections;
            [FieldOffset(4)]
            public ulong TimeDateStamp;
            [FieldOffset(8)]
            public ulong PointerToSymbolTable;
            [FieldOffset(12)]
            public ulong NumberOfSymbols;
            [FieldOffset(16)]
            public ushort SizeOfOptionalHeader;
            [FieldOffset(18)]
            public ushort Characteristics;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "GetSystemWow64DirectoryW", SetLastError = true)]
        private static extern int GetSystemWow64Directory(string systemDirectory, int bufferSize);

        #endregion
    }
}
