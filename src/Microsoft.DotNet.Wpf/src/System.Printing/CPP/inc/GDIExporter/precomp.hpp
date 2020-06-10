// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef GDIEXPORTER

#define GDIEXPORTER

#define INITGUID

#ifndef METRODEVICE

#include <windows.h>
#include <wtypes.h>

#endif

#using REACHFRAMEWORK_DLL   as_friend
#using PRESENTATIONCORE_DLL as_friend

using namespace System;
using namespace System::Collections;
using namespace System::Collections::Generic;
using namespace System::Diagnostics;
using namespace System::IO;
using namespace System::Runtime;
using namespace System::Runtime::ConstrainedExecution;
using namespace System::Runtime::InteropServices;
using namespace System::Security;
using namespace System::Text;
using namespace System::Windows;
using namespace System::Windows::Media;

using namespace Microsoft::Internal::AlphaFlattener;

#include "..\LegacyDevice.hpp"

using namespace System::Printing;
                
namespace Microsoft { namespace Internal { namespace GDIExporter
{

value class PointI
{
public:
    int     x;
    int     y;
};


ref class UnsafeNativeMethods abstract
{
internal:

    [System::Runtime::InteropServices::DllImport(
            "gdi32.dll",
            EntryPoint = "DeleteObject",
            SetLastError = true,
            CallingConvention = System::Runtime::InteropServices::CallingConvention::Winapi)]
    static
    BOOL
    DeleteObject(
        HGDIOBJ hObject   // handle to graphic object
        );
    

    [InteropServices::DllImport(
            "gdi32.dll",
            EntryPoint = "DeleteDC",
            SetLastError = true,
            CallingConvention = InteropServices::CallingConvention::Winapi)]
    static
    BOOL
    DeleteDC(
        HGDIOBJ hObject   // handle to graphic object
        );
    

    [InteropServices::DllImport(
            "gdi32.dll",
            EntryPoint = "RemoveFontMemResourceEx",
            SetLastError = true,
            CallingConvention = InteropServices::CallingConvention::Winapi)]
    static
    BOOL
    RemoveFontMemResourceEx(
        HANDLE hFont   // handle to graphic object
        );
};    


ref class GdiSafeHandle : public InteropServices::SafeHandle
{
public:
    GdiSafeHandle() : InteropServices::SafeHandle(IntPtr::Zero, true) 
    { 
    }

    property bool IsInvalid
    {
        [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
        bool virtual get() override { return IsClosed || (handle == IntPtr::Zero); }
    }

protected:
    [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
    bool virtual ReleaseHandle() override
    {
        IntPtr tempHandle = handle;
        handle = IntPtr::Zero;
        
        if (tempHandle != IntPtr::Zero)
        {
            return UnsafeNativeMethods::DeleteObject((HGDIOBJ) tempHandle) != 0;
        }

        return true;
    }
};


///<remarks>
///  SafeHandle that wraps GDI device contexts
///</remarks>
ref class GdiSafeDCHandle : public GdiSafeHandle
{
public:
    GdiSafeDCHandle() : GdiSafeHandle() 
    { 
    }

#ifdef DBG
    inline HDC GetHDC()
    {
        return (HDC) (void *) handle;
    }
#endif

protected:
    [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
    bool virtual ReleaseHandle() override
    {
        IntPtr tempHandle = handle;
        handle = IntPtr::Zero;
        
        if (tempHandle != IntPtr::Zero)
        {
            return UnsafeNativeMethods::DeleteDC((HGDIOBJ) tempHandle) != 0;
        }

        return true;
    }
};

///<remarks>
///  SafeHandle that wraps GDI font resources.
///</remarks>
ref class GdiFontResourceSafeHandle : public System::Runtime::InteropServices::SafeHandle
{
public:
    GdiFontResourceSafeHandle() : System::Runtime::InteropServices::SafeHandle(IntPtr::Zero, true) 
    { 
        m_timeStamp = DateTime::Now;
    }

    property bool IsInvalid
    {
        [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
        bool virtual get() override { return IsClosed || (handle == IntPtr::Zero); }
    }

    property DateTime TimeStamp
    {
        DateTime get()
        {
            return m_timeStamp;
        }
    }

protected:
    [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
    bool virtual ReleaseHandle() override
    {
        IntPtr tempHandle = handle;
        handle = IntPtr::Zero;
        
        if (tempHandle != IntPtr::Zero)
        {            
            return UnsafeNativeMethods::RemoveFontMemResourceEx((HANDLE)tempHandle) != 0;            
        }

        return true;
    }

    DateTime m_timeStamp;
};


///////////////////////////////////////////////////////////////////////////////////////////////////

// External interface for printing code
#include "nativemethods.h"
#include "utils.h"
#include "printmsg.h"

#include "gdidevice.h"

#include "gdipath.h"
#include "gdibitmap.h"
#include "gdirt.h"

}}}

#endif
