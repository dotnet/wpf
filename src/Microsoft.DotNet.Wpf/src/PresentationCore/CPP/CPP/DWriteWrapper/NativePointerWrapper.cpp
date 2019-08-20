// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "NativePointerWrapper.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface { namespace Generics
{
    template <class T>
    NativePointerCriticalHandle<T>::NativePointerCriticalHandle(void* pNativePointer) : CriticalHandle(IntPtr::Zero)
    {
        SetHandle(IntPtr(pNativePointer));
    }

    template <class T>
    __declspec(noinline) bool NativePointerCriticalHandle<T>::IsInvalid::get()
    {
        return (handle == IntPtr::Zero);
    }

    template <class T>
    T* NativePointerCriticalHandle<T>::Value::get()
    {
        return (T*)handle.ToPointer();
    }

    template <class T>
    NativeIUnknownWrapper<T>::NativeIUnknownWrapper(IUnknown* pNativePointer) : NativePointerCriticalHandle<T>(pNativePointer)
    {
    }
    
    template <class T>
    __declspec(noinline) bool NativeIUnknownWrapper<T>::ReleaseHandle()
    {
        ((IUnknown*)handle.ToPointer())->Release();
        handle = IntPtr::Zero;
        return true;
    }

    template <class T>
    NativePointerWrapper<T>::NativePointerWrapper(T* pNativePointer) : NativePointerCriticalHandle<T>(pNativePointer)
    {
    }
    
    template <class T>
    __declspec(noinline) bool NativePointerWrapper<T>::ReleaseHandle()
    {
        delete handle.ToPointer();
        handle = IntPtr::Zero;
        return true;
    }

}}}}}//MS::Internal::Text::TextInterface::Generics
