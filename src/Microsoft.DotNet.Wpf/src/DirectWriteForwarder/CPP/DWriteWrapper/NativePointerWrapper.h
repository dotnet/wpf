// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __NATIVE_POINTER_WRAPPER_H
#define __NATIVE_POINTER_WRAPPER_H

#include "Common.h"

using namespace System::Runtime::InteropServices;
using namespace System::Runtime::ConstrainedExecution;

namespace MS { namespace Internal { namespace Text { namespace TextInterface { namespace Generics
{
    template <class T>
    private ref class NativePointerCriticalHandle abstract : public CriticalHandle
    {
        public:
            NativePointerCriticalHandle(void* pNativePointer);

            virtual property bool IsInvalid
            {
                #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
                [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
                #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
                bool get() override;
            }

            property T* Value
            {
                T* get();
            }
    };

    template <class T>
    private ref class NativeIUnknownWrapper : public NativePointerCriticalHandle<T>
    {
        protected:

            #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
            #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            virtual bool ReleaseHandle() override;

        public:
            NativeIUnknownWrapper(IUnknown* pNativePointer);
    };

    template <class T>
    private ref class NativePointerWrapper : public NativePointerCriticalHandle<T>
    {
        protected:

            #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
            #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            virtual bool ReleaseHandle() override;

        public:
            NativePointerWrapper(T* pNativePointer);
    };

}}}}}//MS::Internal::Text::TextInterface::Generics

#endif //__NATIVE_POINTER_WRAPPER_H
