// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICPRINTEREVELTHUNK_HPP__
#define __GENERICPRINTEREVELTHUNK_HPP__
/*++
    Abstract:

        Win32PrinterThunk - This is object that does the Win32 thunking for a PrintQueue
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetPrinter, SetPrinter and EnumPrinters APIs. 

--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
namespace PrintQueueThunk
{
    using namespace System::Security;

    private ref class Win32PrinterThunk : public InfoLevelThunk
    {
        public:
        
        Win32PrinterThunk(
            UInt32                  infoLevel,
            InfoLevelMask           infoCoverageMask
            );

        virtual
        void
        CallWin32ApiToGetPrintInfoData(
            PrinterThunkHandler^    printThunkHandler,
            Object^                 cookie
            ) override;

        virtual
        UInt32
        CallWin32ApiToEnumeratePrintInfoData(
            String^                 serverName,
            UInt32                  flags
            );

        virtual
        void
        BeginCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^    printThunkHandler
            ) override;

        virtual
        void
        EndCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^    printThunkHandler
            ) override;
    };
}
}
}
}
}
#endif
