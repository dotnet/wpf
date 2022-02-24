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
            IntPtr hDWriteLibrary = IntPtr.Zero;

            // KB2533623 introduced the LOAD_LIBRARY_SEARCH_SYSTEM32 flag. It also introduced
            // the AddDllDirectory function. We test for presence of AddDllDirectory as an 
            // indirect evidence for the support of LOAD_LIBRARY_SEARCH_SYSTEM32 flag. 
            IntPtr hKernel32 = Kernel32.GetModuleHandleW(Libraries.Kernel32);

            if (hKernel32 != IntPtr.Zero)
            {
                if (Kernel32.GetProcAddress(hKernel32, "AddDllDirectory") != IntPtr.Zero)
                {
                    // All supported platforms newer than Vista SP2 shipped with dwrite.dll.
                    // On Vista SP2, the .NET servicing process will ensure that a MSU containing 
                    // dwrite.dll will be delivered as a prerequisite - effectively guaranteeing that 
                    // this following call to LoadLibraryEx(dwrite.dll) will succeed, and that it will 
                    // not be susceptible to typical DLL planting vulnerability vectors.
                    hDWriteLibrary = Kernel32.LoadLibraryExW("dwrite.dll", IntPtr.Zero, Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
                }
                else
                {
                    // LOAD_LIBRARY_SEARCH_SYSTEM32 is not supported on this OS. 
                    // Fall back to using plain ol' LoadLibrary
                    // There is risk that this call might fail, or that it might be
                    // susceptible to DLL hijacking. 
                    hDWriteLibrary = Kernel32.LoadLibraryW("dwrite.dll");
                }
            }

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
