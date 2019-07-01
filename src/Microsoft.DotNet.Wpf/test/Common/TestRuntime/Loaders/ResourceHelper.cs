// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;

namespace Microsoft.Test.Loaders
{

    internal static class ResourceHelper
    {

        #region Public Members

        //Get an resource string from an unmanaged resource dll
        public static string GetUnmanagedResourceString(string filename, int resourceId)
        {
            //Try loading an MUI library for the current language first
            string muiPath = GetUserDefaultUILanguage().ToString("x");
            muiPath = @"mui\" + new string('0', 4 - muiPath.Length) + muiPath + @"\" + filename;
            IntPtr lib = LoadLibraryEx(muiPath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

            //Fall back on the specified library
            if (lib == IntPtr.Zero)
                lib = LoadLibraryEx(filename, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

            if (lib == IntPtr.Zero)
                throw new ArgumentException("The specified library could not be loaded", "filename");

            try
            {
                char[] buffer = new char[1000];
                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                int retVal = LoadString(lib, resourceId, buffer, buffer.Length);
                handle.Free();

                if (retVal == 0)
                    throw new ArgumentException("the specified resource id does not exist int the resource", "resourceId");

                return new String(buffer, 0, retVal);
            }
            finally
            {
                FreeLibrary(lib);
            }
        }

        #endregion


        #region Private Members


        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr hFile, int dwflags);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr lib);

        [DllImport("kernel32.dll")]
        private static extern int GetUserDefaultUILanguage();

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int LoadString(IntPtr lib, int wID, char[] buff, int nBufferMax);

        #endregion

    }

}
