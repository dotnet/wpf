// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "NativePointerWrapper.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface { namespace Generics
{
    template <class T>
    /// <SecurityNote>
    /// Critical - Assigns the native pointer that this object wraps.
    /// </SecurityNote>
    NativePointerCriticalHandle<T>::NativePointerCriticalHandle(void* pNativePointer) : CriticalHandle(IntPtr::Zero)
    {
        SetHandle(IntPtr(pNativePointer));
    }

    template <class T>
    /// <SecurityNote>
    /// Critical - Accesses the critical handle.
    /// Safe     - Does not expose the critical handle.
    /// </SecurityNote>
    __declspec(noinline) bool NativePointerCriticalHandle<T>::IsInvalid::get()
    {
        return (handle == IntPtr::Zero);
    }

    template <class T>
    /// <SecurityNote>
    /// Critical - Exposes the pointer that this object wraps.
    /// </SecurityNote>
    T* NativePointerCriticalHandle<T>::Value::get()
    {
        return (T*)handle.ToPointer();
    }

    template <class T>
    /// <SecurityNote>
    /// Critical - Assigns the native pointer that this object wraps.
    /// </SecurityNote>
    NativeIUnknownWrapper<T>::NativeIUnknownWrapper(IUnknown* pNativePointer) : NativePointerCriticalHandle<T>(pNativePointer)
    {
    }
    
    template <class T>
    /// <SecurityNote>
    /// Critical - Accesses the critical handle.
    /// Safe     - Just releases the pointer which is stored 
    ///            internally and is trusted.
    /// </SecurityNote>
    __declspec(noinline) bool NativeIUnknownWrapper<T>::ReleaseHandle()
    {
        ((IUnknown*)handle.ToPointer())->Release();
        handle = IntPtr::Zero;
        return true;
    }

    template <class T>
    /// <SecurityNote>
    /// Critical - Assigns the native pointer that this object wraps.
    /// </SecurityNote>
    NativePointerWrapper<T>::NativePointerWrapper(T* pNativePointer) : NativePointerCriticalHandle<T>(pNativePointer)
    {
    }
    
    template <class T>
    /// <SecurityNote>
    /// Critical - Accesses the critical handle.
    /// Safe     - Just deletes the pointer which is stored 
    ///            internally and is trusted.
    /// </SecurityNote>
    __declspec(noinline) bool NativePointerWrapper<T>::ReleaseHandle()
    {
        delete handle.ToPointer();
        handle = IntPtr::Zero;
        return true;
    }

}}}}}//MS::Internal::Text::TextInterface::Generics
