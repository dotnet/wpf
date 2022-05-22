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
    internal unsafe class TfThreadMgrWrapper : ITfThreadMgr, ITfMessagePump, ITfSource, ITfKeystrokeMgr, IDisposable
    {
        private readonly IntPtr _wrappedInstance;
        private readonly IntPtr _messagePumpInstance;
        private readonly IntPtr _sourceInstance;
        private readonly IntPtr _keystrokeMgrInstance;

        internal TfThreadMgrWrapper(IntPtr wrappedInstance)
        {
            _wrappedInstance = wrappedInstance;
            Guid tfMessagePumpIid = IID_ITfMessagePump;
            var result = Marshal.QueryInterface(wrappedInstance, ref tfMessagePumpIid, out _messagePumpInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            Guid tfSourceIid = IID_ITfSource;
            result = Marshal.QueryInterface(wrappedInstance, ref tfSourceIid, out _sourceInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            Guid tfKeystrokeMgrIid = IID_ITfKeystrokeMgr;
            result = Marshal.QueryInterface(wrappedInstance, ref tfKeystrokeMgrIid, out _keystrokeMgrInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void Dispose()
        {
            Marshal.Release(_wrappedInstance);
            Marshal.Release(_messagePumpInstance);
            Marshal.Release(_sourceInstance);
        }

        public void Activate(out int clientId)
        {
            fixed (int* pClientId = &clientId)
            {
                int result = ((delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)_wrappedInstance + 3)))
                    (_wrappedInstance, pClientId);
                if (NativeMethods.Failed(result))
                {
                    Marshal.ThrowExceptionForHR(result);
                }
            }
        }

        public void Deactivate()
        {
            int result = ((delegate* unmanaged<IntPtr, int>)(*(*(void***)_wrappedInstance + 4)))
                (_wrappedInstance);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }

        public void CreateDocumentMgr(out ITfDocumentMgr docMgr)
        {
            IntPtr docMgrPtr = IntPtr.Zero;
            int result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 5)))
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
            int result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 6)))
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
            int result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 7)))
                (_wrappedInstance, &docMgrPtr);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            docMgr = (ITfDocumentMgr)Marshal.GetObjectForIUnknown(docMgrPtr);
        }

        public void SetFocus(ITfDocumentMgr docMgr)
        {
            IntPtr docMgrPtr = IntPtr.Zero;
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(docMgr);
            try
            {
                Guid tfDocumentMgr = IID_ITfDocumentMgr;
                int result = Marshal.QueryInterface(unknownPtr, ref tfDocumentMgr, out docMgrPtr);
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
            finally
            {
                if (unknownPtr != IntPtr.Zero)
                {
                    Marshal.Release(unknownPtr);
                }

                if (docMgrPtr != IntPtr.Zero)
                {
                    Marshal.Release(docMgrPtr);
                }
            }
        }

        public void AssociateFocus(IntPtr hwnd, ITfDocumentMgr newDocMgr, out ITfDocumentMgr prevDocMgr)
        {
            IntPtr newDocMgrPtr = IntPtr.Zero;
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(newDocMgr);
            try
            {
                Guid tfDocumentMgr = IID_ITfDocumentMgr;
                int result = Marshal.QueryInterface(unknownPtr, ref tfDocumentMgr, out newDocMgrPtr);
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
            finally
            {
                if (unknownPtr != IntPtr.Zero)
                {
                    Marshal.Release(unknownPtr);
                }

                if (newDocMgrPtr != IntPtr.Zero)
                {
                    Marshal.Release(newDocMgrPtr);
                }
            }
        }

        public void IsThreadFocus([MarshalAs(UnmanagedType.Bool)] out bool isFocus)
        {
            int isFocusNative;
            int result = ((delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)_wrappedInstance + 10)))
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
                int result = ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)(*(*(void***)_wrappedInstance + 11)))
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
            int result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 12)))
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
            int result = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 12)))
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
                int hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 3)))
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
                int hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 4)))
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
                int hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 5)))
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
                int hr = ((delegate* unmanaged<IntPtr, MSG*, IntPtr, int, int, int*, int>)(*(*(void***)_messagePumpInstance + 6)))
                    (_messagePumpInstance, msgPtr, hwnd, msgFilterMin, msgFilterMax, resultPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void AdviseSink(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] object obj, out int cookie)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(obj);
            try
            {
                fixed (Guid* iidPtr = &riid)
                fixed (int* cookiePtr = &cookie)
                {
                    int hr = ((delegate* unmanaged<IntPtr, Guid*, IntPtr, int*, int>)(*(*(void***)_sourceInstance + 3)))
                        (_sourceInstance, iidPtr, unknownPtr, cookiePtr);
                    if (NativeMethods.Failed(hr))
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
            }
            finally
            {
                if (unknownPtr != IntPtr.Zero)
                {
                    Marshal.Release(unknownPtr);
                }
            }
        }

        public void UnadviseSink(int cookie)
        {
            int hr = ((delegate* unmanaged<IntPtr, int, int>)(*(*(void***)_sourceInstance + 4)))
                (_sourceInstance, cookie);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public void AdviseKeyEventSink(int clientId, [MarshalAs(UnmanagedType.Interface)] object obj, [MarshalAs(UnmanagedType.Bool)] bool fForeground)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(obj);
            try
            {
                int hr = ((delegate* unmanaged<IntPtr, int, IntPtr, int, int>)(*(*(void***)_keystrokeMgrInstance + 3)))
                    (_keystrokeMgrInstance, clientId, unknownPtr, fForeground ? 1 : 0);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                if (unknownPtr != IntPtr.Zero)
                {
                    Marshal.Release(unknownPtr);
                }
            }
        }

        public void UnadviseKeyEventSink(int clientId)
        {
            int hr = ((delegate* unmanaged<IntPtr, int, int>)(*(*(void***)_keystrokeMgrInstance + 4)))
                (_keystrokeMgrInstance, clientId);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public void GetForeground(out Guid clsid)
        {
            fixed (Guid* clsidPtr = &clsid)
            {
                int hr = ((delegate* unmanaged<IntPtr, Guid*, int>)(*(*(void***)_keystrokeMgrInstance + 5)))
                    (_keystrokeMgrInstance, clsidPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void TestKeyDown(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten)
        {
            int eatenNative;
            int hr = ((delegate* unmanaged<IntPtr, int, int, int*, int>)(*(*(void***)_keystrokeMgrInstance + 6)))
                (_keystrokeMgrInstance, wParam, lParam, &eatenNative);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            eaten = eatenNative != 0;
        }

        public void TestKeyUp(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten)
        {
            int eatenNative;
            int hr = ((delegate* unmanaged<IntPtr, int, int, int*, int>)(*(*(void***)_keystrokeMgrInstance + 7)))
                (_keystrokeMgrInstance, wParam, lParam, &eatenNative);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            eaten = eatenNative != 0;
        }

        public void KeyDown(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten)
        {
            int eatenNative;
            int hr = ((delegate* unmanaged<IntPtr, int, int, int*, int>)(*(*(void***)_keystrokeMgrInstance + 8)))
                (_keystrokeMgrInstance, wParam, lParam, &eatenNative);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            eaten = eatenNative != 0;
        }

        public void KeyUp(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten)
        {
            int eatenNative;
            int hr = ((delegate* unmanaged<IntPtr, int, int, int*, int>)(*(*(void***)_keystrokeMgrInstance + 9)))
                (_keystrokeMgrInstance, wParam, lParam, &eatenNative);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            eaten = eatenNative != 0;
        }

        public void GetPreservedKey(ITfContext context, ref TF_PRESERVEDKEY key, out Guid guid)
        {
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(context);
            Guid contextIID = IID_ITfContext;
            int result = Marshal.QueryInterface(unknownPtr, ref contextIID, out IntPtr contextPtr);
            if (NativeMethods.Failed(result))
            {
                Marshal.ThrowExceptionForHR(result);
            }

            fixed (TF_PRESERVEDKEY* keyPtr = &key)
            fixed (Guid* guidPtr = &guid)
            {
                int hr = ((delegate* unmanaged<IntPtr, IntPtr, TF_PRESERVEDKEY*, Guid*, int>)(*(*(void***)_keystrokeMgrInstance + 10)))
                    (_keystrokeMgrInstance, contextPtr, keyPtr, guidPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void IsPreservedKey(ref Guid guid, ref TF_PRESERVEDKEY key, [MarshalAs(UnmanagedType.Bool)] out bool registered)
        {
            int registeredNative;
            fixed (Guid* guidPtr = &guid)
            fixed (TF_PRESERVEDKEY* keyPtr = &key)
            {
                var hr = ((delegate* unmanaged<IntPtr, Guid*, TF_PRESERVEDKEY*, int*, int>)(*(*(void***)_keystrokeMgrInstance + 11)))
                (_keystrokeMgrInstance, guidPtr, keyPtr, &registeredNative);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            registered = registeredNative != 0;
        }

        public void PreserveKey(int clientId, ref Guid guid, ref TF_PRESERVEDKEY key, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] char[] desc, int descCount)
        {
            fixed (Guid* guidPtr = &guid)
            fixed (TF_PRESERVEDKEY* keyPtr = &key)
            fixed (char* descPtr = desc)
            {
                var hr = ((delegate* unmanaged<IntPtr, int, Guid*, TF_PRESERVEDKEY*, char*, int, int>)(*(*(void***)_keystrokeMgrInstance + 12)))
                (_keystrokeMgrInstance, clientId, guidPtr, keyPtr, descPtr, descCount);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void UnpreserveKey(ref Guid guid, ref TF_PRESERVEDKEY key)
        {
            fixed (Guid* guidPtr = &guid)
            fixed (TF_PRESERVEDKEY* keyPtr = &key)
            {
                var hr = ((delegate* unmanaged<IntPtr, Guid*, TF_PRESERVEDKEY*, int>)(*(*(void***)_keystrokeMgrInstance + 13)))
                (_keystrokeMgrInstance, guidPtr, keyPtr);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void SetPreservedKeyDescription(ref Guid guid, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] char[] desc, int descCount)
        {
            fixed (Guid* guidPtr = &guid)
            fixed (char* descPtr = desc)
            {
                var hr = ((delegate* unmanaged<IntPtr, Guid*, char*, int, int>)(*(*(void***)_keystrokeMgrInstance + 14)))
                (_keystrokeMgrInstance, guidPtr, descPtr, descCount);
                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public void GetPreservedKeyDescription(ref Guid guid, [MarshalAs(UnmanagedType.BStr)] out string desc)
        {
            IntPtr deskPtr = IntPtr.Zero;
            try
            {
                fixed (Guid* guidPtr = &guid)
                {
                    var hr = ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)(*(*(void***)_keystrokeMgrInstance + 15)))
                        (_keystrokeMgrInstance, guidPtr, &deskPtr);
                    if (NativeMethods.Failed(hr))
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    desc = Marshal.PtrToStringBSTR(deskPtr);
                }
            }
            finally
            {
                Marshal.FreeBSTR(deskPtr);
            }
        }

        public void SimulatePreservedKey(ITfContext context, ref Guid guid, [MarshalAs(UnmanagedType.Bool)] out bool eaten)
        {
            IntPtr contextPtr = IntPtr.Zero;
            IntPtr unknownPtr = Marshal.GetIUnknownForObject(context);
            try
            {
                int eatenNative;
                Guid contextIID = IID_ITfContext;
                int result = Marshal.QueryInterface(unknownPtr, ref contextIID, out contextPtr);
                if (NativeMethods.Failed(result))
                {
                    Marshal.ThrowExceptionForHR(result);
                }

                fixed (Guid* guidPtr = &guid)
                {
                    int hr = ((delegate* unmanaged<IntPtr, IntPtr, Guid*, int*, int>)(*(*(void***)_keystrokeMgrInstance + 16)))
                        (_keystrokeMgrInstance, contextPtr, guidPtr, &eatenNative);
                    if (NativeMethods.Failed(hr))
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    eaten = eatenNative != 0;
                }
            }
            finally
            {
                if (unknownPtr != IntPtr.Zero)
                {
                    Marshal.Release(unknownPtr);
                }

                if (contextPtr != IntPtr.Zero)
                {
                    Marshal.Release(contextPtr);
                }
            }
        }
    }
}
