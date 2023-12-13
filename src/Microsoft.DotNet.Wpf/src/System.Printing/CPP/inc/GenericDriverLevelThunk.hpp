// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICDRIVERINFOLEVELTHUNK_HPP__
#define __GENERICDRIVERINFOLEVELTHUNK_HPP__
/*++
        
    Abstract:
        
        Win32DriverThunk - This is object that does the Win32 thunking for a Driver
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetDriver, EnumPrinterDrivers APIs. 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
namespace DriverThunk
{
    private ref class Win32DriverThunk : public InfoLevelThunk
    {
        public:

        Win32DriverThunk(
            UInt32                                  level,
            InfoLevelMask                           levelMask
            );

        virtual
        void
        CallWin32ApiToGetPrintInfoData(
            PrinterThunkHandler^                    printThunkHandler,
            Object^                                 cookie
            ) override;

        virtual
        UInt32
        CallWin32ApiToEnumeratePrintInfoData(
            String^                                 serverName,
            UInt32                                  flags
            );

        virtual
        void
        BeginCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^                    printThunkHandler
            ) override;

        virtual
        void
        EndCallWin32ApiToSetPrintInfoData(
            PrinterThunkHandler^                    printThunkHandler
            ) override;

        virtual
        bool
        SetValueFromAttributeValue(
            String^                                 valueName,
            Object^                                 value
            ) override;
        
        private:
        //
        // No data in object
        //
    };
}
}
}
}
}
#endif
