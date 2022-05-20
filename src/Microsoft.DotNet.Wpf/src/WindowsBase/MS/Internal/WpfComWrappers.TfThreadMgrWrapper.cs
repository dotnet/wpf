// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal;

using System;
using System.Runtime.InteropServices;
using static global::MS.Win32.UnsafeNativeMethods;

internal partial class WpfComWrappers
{
    internal unsafe class TfThreadMgrWrapper : ITfThreadMgr
    {
        private readonly IntPtr _wrappedInstance;

        internal TfThreadMgrWrapper(IntPtr wrappedInstance)
        {
            _wrappedInstance = wrappedInstance;
        }

        public void Dispose()
        {
            Marshal.Release(_wrappedInstance);
        }

        public void Activate(out int clientId)
        {
            fixed (int* pClientId = &clientId)
            {
                var result = ((delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)_wrappedInstance + 3)))
                    (_wrappedInstance, pClientId);
                if (result < 0)
                {
                    Marshal.ThrowExceptionForHR(result);
                }
            }
        }

        public void Deactivate()
        {
            var result = ((delegate* unmanaged<IntPtr, int>)(*(*(void***)_wrappedInstance + 4)))
                (_wrappedInstance);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void CreateDocumentMgr(out ITfDocumentMgr docMgr)
        {
            IntPtr docMgrPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 5)))
                (_wrappedInstance, &docMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            docMgr = (ITfDocumentMgr)Marshal.GetObjectForIUnknown(docMgrPtr);
        }

        public void EnumDocumentMgrs(out IEnumTfDocumentMgrs enumDocMgrs)
        {
            IntPtr enumDocMgrsPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 6)))
                (_wrappedInstance, &enumDocMgrsPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            enumDocMgrs = (IEnumTfDocumentMgrs)Marshal.GetObjectForIUnknown(enumDocMgrsPtr);
        }

        public void GetFocus(out ITfDocumentMgr docMgr)
        {
            IntPtr docMgrPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 7)))
                (_wrappedInstance, &docMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            docMgr = (ITfDocumentMgr)Marshal.GetObjectForIUnknown(docMgrPtr);
        }

        public void SetFocus(ITfDocumentMgr docMgr)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(docMgr);
            var tfDocumentMgr = IID_ITfDocumentMgr;
            var result = Marshal.QueryInterface(unknownPtr, ref tfDocumentMgr, out IntPtr docMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            result = ((delegate* unmanaged<IntPtr, IntPtr, int>)(*(*(void***)_wrappedInstance + 8)))
                (_wrappedInstance, docMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void AssociateFocus(IntPtr hwnd, ITfDocumentMgr newDocMgr, out ITfDocumentMgr prevDocMgr)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(newDocMgr);
            var tfDocumentMgr = IID_ITfDocumentMgr;
            var result = Marshal.QueryInterface(unknownPtr, ref tfDocumentMgr, out IntPtr newDocMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            IntPtr prevDocMgrPtr = IntPtr.Zero;
            result = ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 9)))
                (_wrappedInstance, hwnd, newDocMgrPtr, &prevDocMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            prevDocMgr = (ITfDocumentMgr)Marshal.GetObjectForIUnknown(prevDocMgrPtr);
        }

        public void IsThreadFocus([MarshalAs(UnmanagedType.Bool)] out bool isFocus)
        {
            int isFocusNative;
            var result = ((delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)_wrappedInstance + 10)))
                (_wrappedInstance, &isFocusNative);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            isFocus = isFocusNative != 0;
        }

        public int GetFunctionProvider(ref Guid classId, out ITfFunctionProvider funcProvider)
        {
            IntPtr funcProviderPtr = IntPtr.Zero;
            fixed (Guid* pClassId = &classId)
            {
                var result = ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)(*(*(void***)_wrappedInstance + 11)))
                    (_wrappedInstance, pClassId, &funcProviderPtr);
                if (result < 0)
                {
                    funcProvider = null;
                    return result;
                }
            }

            funcProvider = (ITfFunctionProvider)Marshal.GetObjectForIUnknown(funcProviderPtr);
            return 0;
        }

        public void EnumFunctionProviders(out IEnumTfFunctionProviders enumProviders)
        {
            IntPtr enumProvidersPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 12)))
                (_wrappedInstance, &enumProvidersPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            enumProviders = (IEnumTfFunctionProviders)Marshal.GetObjectForIUnknown(enumProvidersPtr);
        }

        public void GetGlobalCompartment(out ITfCompartmentMgr compartmentMgr)
        {
            IntPtr compartmentMgrPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 12)))
                (_wrappedInstance, &compartmentMgrPtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            compartmentMgr = (ITfCompartmentMgr)Marshal.GetObjectForIUnknown(compartmentMgrPtr);
        }
    }
}
