// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPINTERFACES_HPP__
#define __INTEROPINTERFACES_HPP__
/*++
                                                                              
    Abstract:

        Interface that the managed objects wrapping PRINTER_INFO or 
        DRIVER_INFO structures expose. The interface allows direct access
        to the unmanaged buffer, it returns the number of structres in the buffer
        and gets and sets data inside the buffer.
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System::Security;
    using namespace System::Drawing::Printing;

    private interface class IPrinterInfo
    {
        public:

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            SafeMemoryHandle^ get();
        }

        property
        UInt32
        Count
        {
            UInt32 get();
        }
        
        Object^
        GetValueFromName(
            String^         valueName,
            UInt32          index
            );

        bool
        SetValueFromName(
            String^         valueName,
            Object^         value
            );

        void
        Release(
            );
    };
}
}
}
#endif
