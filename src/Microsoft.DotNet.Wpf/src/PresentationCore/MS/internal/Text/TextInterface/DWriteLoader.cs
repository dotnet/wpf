// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using static Interop;

namespace MS.Internal.Text.TextInterface
{
    internal static unsafe class DWriteLoader
    {
        private static IntPtr _dwrite;
        private static delegate* unmanaged<int, void*, void*, int> _dwriteCreateFactory;

        internal static void LoadDWrite()
        {
            // We load dwrite here because it's cleanup logic is different from the other native dlls
            // and don't want to abstract that
            _dwrite = LoadDWriteLibraryAndGetProcAddress(out delegate* unmanaged<int, void*, void*, int> dwriteCreateFactory);

            if (_dwrite == IntPtr.Zero)
                throw new DllNotFoundException("dwrite.dll", new Win32Exception());

            if (dwriteCreateFactory == null)
                throw new InvalidOperationException();

            _dwriteCreateFactory = dwriteCreateFactory;
        }

        internal static void UnloadDWrite()
        {
            ClearDWriteCreateFactoryFunctionPointer();
        
            if (_dwrite != IntPtr.Zero)
            {
                if (Kernel32.FreeLibrary(_dwrite) != 0)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    Marshal.ThrowExceptionForHR(HRESULT_FROM_WIN32(lastError));
                }

                _dwrite = IntPtr.Zero;
            }
        }

        internal static delegate* unmanaged<int, void*, void*, int> GetDWriteCreateFactoryFunctionPointer()
        {
            return _dwriteCreateFactory;
        }

        private static void ClearDWriteCreateFactoryFunctionPointer()
        {
            _dwriteCreateFactory = null;
        }

        private static IntPtr LoadDWriteLibraryAndGetProcAddress(out delegate* unmanaged<int, void*, void*, int> DWriteCreateFactory)
        {
            IntPtr hDWriteLibrary = Kernel32.LoadLibraryExW("dwrite.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);

            if (hDWriteLibrary != IntPtr.Zero)
            {
                DWriteCreateFactory = (delegate* unmanaged<int, void*, void*, int>)Kernel32.GetProcAddress(hDWriteLibrary, "DWriteCreateFactory");
            }
            else
            {
                DWriteCreateFactory = null;
            }

            return hDWriteLibrary;
        }
    }
}
