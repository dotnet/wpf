// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal;

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MS.Win32;
using static global::MS.Win32.UnsafeNativeMethods;

internal partial class WpfComWrappers
{
    internal unsafe class TfThreadMgrWrapper : ITfThreadMgr, ITfMessagePump
    {
        private readonly IntPtr _wrappedInstance;
        private readonly IntPtr _messagePumpInstance;

        internal TfThreadMgrWrapper(IntPtr wrappedInstance)
        {
            _wrappedInstance = wrappedInstance;
            var tfMessagePumpIid = IID_ITfMessagePump;
            var result = Marshal.QueryInterface(wrappedInstance, ref tfMessagePumpIid, out _messagePumpInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }
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
                if (NativeMethods.Failed(result))
                {
                    Marshal.ThrowExceptionForHR(result);
                }
            }
        }

        public void Deactivate()
        {
            var result = ((delegate* unmanaged<IntPtr, int>)(*(*(void***)_wrappedInstance + 4)))
                (_wrappedInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void CreateDocumentMgr(out ITfDocumentMgr docMgr)
        {
            IntPtr docMgrPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 5)))
                (_wrappedInstance, &docMgrPtr);
            if (NativeMethods.Failed(result))
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
            if (NativeMethods.Failed(result))
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
            if (NativeMethods.Failed(result))
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
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            result = ((delegate* unmanaged<IntPtr, IntPtr, int>)(*(*(void***)_wrappedInstance + 8)))
                (_wrappedInstance, docMgrPtr);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void AssociateFocus(IntPtr hwnd, ITfDocumentMgr newDocMgr, out ITfDocumentMgr prevDocMgr)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(newDocMgr);
            var tfDocumentMgr = IID_ITfDocumentMgr;
            var result = Marshal.QueryInterface(unknownPtr, ref tfDocumentMgr, out IntPtr newDocMgrPtr);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            IntPtr prevDocMgrPtr = IntPtr.Zero;
            result = ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 9)))
                (_wrappedInstance, hwnd, newDocMgrPtr, &prevDocMgrPtr);
            if (NativeMethods.Failed(result))
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
            if (NativeMethods.Failed(result))
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
                if (NativeMethods.Failed(result))
                {
                    funcProvider = null;
                    return result;
                }
            }

            funcProvider = (ITfFunctionProvider)Marshal.GetObjectForIUnknown(funcProviderPtr);
            return NativeMethods.S_OK;
        }

        public void EnumFunctionProviders(out IEnumTfFunctionProviders enumProviders)
        {
            IntPtr enumProvidersPtr = IntPtr.Zero;
            var result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 12)))
                (_wrappedInstance, &enumProvidersPtr);
            if (NativeMethods.Failed(result))
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
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            compartmentMgr = (ITfCompartmentMgr)Marshal.GetObjectForIUnknown(compartmentMgrPtr);
        }

        public void PeekMessageA(ref MSG msg, IntPtr hwnd, int msgFilterMin, int msgFilterMax, int removeMsg, out int result)
        {
            fixed (MSG* msgPtr = &msg)
            fixed (int* resultPtr = &result)
            {
                var hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 3)))
                    (_messagePumpInstance, msgPtr, hwnd, msgFilterMin, msgFilterMax, removeMsg, resultPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void GetMessageA(ref MSG msg, IntPtr hwnd, int msgFilterMin, int msgFilterMax, out int result)
        {
            fixed (MSG* msgPtr = &msg)
            fixed (int* resultPtr = &result)
            {
                var hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 4)))
                    (_messagePumpInstance, msgPtr, hwnd, msgFilterMin, msgFilterMax, resultPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void PeekMessageW(ref MSG msg, IntPtr hwnd, int msgFilterMin, int msgFilterMax, int removeMsg, out int result)
        {
            fixed (MSG* msgPtr = &msg)
            fixed (int* resultPtr = &result)
            {
                var hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 5)))
                    (_messagePumpInstance, msgPtr, hwnd, msgFilterMin, msgFilterMax, removeMsg, resultPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void GetMessageW(ref MSG msg, IntPtr hwnd, int msgFilterMin, int msgFilterMax, out int result)
        {
            fixed (MSG* msgPtr = &msg)
            fixed (int* resultPtr = &result)
            {
                var hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 6)))
                    (_messagePumpInstance, msgPtr, hwnd, msgFilterMin, msgFilterMax, resultPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
}
