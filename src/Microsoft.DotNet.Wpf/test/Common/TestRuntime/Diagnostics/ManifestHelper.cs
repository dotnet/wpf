// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using System.Xml;

namespace Microsoft.Test.Diagnostics
{

    internal static class ManifestHelper
    {

        #region Public Members

        //reads a manifest of a binary to determine if it has specific security requirements
        public static void GetManifestSecurityRequirements(string filename, ref bool elevated, ref bool uiAccess)
        {
            Stream manifest = GetResourceManifest(filename);
            if (manifest != null)
            {
                using (XmlTextReader reader = new XmlTextReader(manifest))
                {
                    //look for the execution level in the manifest
                    while (reader.Read())
                    {
                        if (reader.LocalName == "requestedExecutionLevel")
                        {
                            //TODO: are there more levels that mean elevated then this?
                            if (reader.GetAttribute("level") != null)
                            {
                                string level = reader.GetAttribute("level").ToLowerInvariant();
                                elevated |= level == "requireadministrator" || level == "highestavailable";
                            }

                            if (reader.GetAttribute("uiAccess") != null)
                            {
                                uiAccess |= reader.GetAttribute("uiAccess").ToLowerInvariant() == "true";
                            }

                            return;
                        }
                    }
                }
            }

        }

        //gets manifest XML embedded in the the specified binary
        public static Stream GetResourceManifest(string filename)
        {
            Stream manifest = null;

            //Try loading an MUI library for the current language first
            IntPtr lib = LoadLibraryEx(filename, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

            //not a valid binary file (could be a .bat or .cmd)
            if (lib == IntPtr.Zero)
                return null;

            try
            {
                //The embedded resource with name of 1 is the manifest that is used by the system
                IntPtr hResourceBlock = FindResource(lib, 1, RT_MANIFEST);
                if (hResourceBlock != IntPtr.Zero)
                {
                    IntPtr hResource = LoadResource(lib, hResourceBlock);
                    int size = SizeofResource(lib, hResourceBlock);

                    //Lock the resource and copy it to a byte array, then create a stream over the array
                    IntPtr hGlobalMemory = LockResource(hResource);
                    byte[] data = new byte[size];
                    Marshal.Copy(hGlobalMemory, data, 0, size);
                    manifest = new MemoryStream(data, false);
                }
            }
            finally
            {
                //No need to free resource.  MSDN documentation explains that the pointers are freed when unloading the library
                if (!FreeLibrary(lib))
                    throw new Win32Exception();
            }

            return manifest;
        }

        #endregion


        #region Unmanaged Interop


        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        private const int RT_MANIFEST = 24;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr hFile, int dwflags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr lib);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeResource(IntPtr hGlobal);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr hModule, int lpName, int lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);


        #endregion

    }

}
