// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __GENERICJOBINFOLEVELTHUNK_HPP__
#define __GENERICJOBINFOLEVELTHUNK_HPP__
/*++
        
    Abstract:
        
        Win32JobThunk - This is object that does the Win32 thunking for a print job
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetJob, EnumJobs APIs. 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace AttributeNameToInfoLevelMapping
{
namespace JobThunk
{
    private ref class Win32JobThunk:
    public InfoLevelThunk
    {
        public:

        Win32JobThunk(
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
            PrinterThunkHandler^                    printThunkHandler,
            UInt32                                  firstJobId,
            UInt32                                  numberOfJobs
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
        
    };
}
}
}
}
}
#endif
