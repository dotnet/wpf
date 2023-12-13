// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __XPSDEVSIMINTEROPPRINTERHANDLER_HPP__
#define __XPSDEVSIMINTEROPPRINTERHANDLER_HPP__
/*++
                                                                              
    Abstract:

        Managed wrapper for Win32 XPS print APIs. This object wraps a printer handle
        and does gets, sets and enum operations. Implements IDisposable. The caller
        must call Dispose when done using the object.

--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System::IO;
    using namespace System::Security;
    using namespace System::Runtime::InteropServices;

    private ref class XpsDeviceSimulatingPrintThunkHandler : public PrinterThunkHandlerBase
    {
        public:
           
        XpsDeviceSimulatingPrintThunkHandler(
            String^ printerName
        );
        
        property
        virtual 
        Boolean
        IsInvalid
        {
            Boolean virtual get() override;
        }

        virtual
        Boolean 
        ReleaseHandle(
            void
            ) override;

        virtual
        Int32
        ThunkStartDocPrinter(
            DocInfoThree^         docInfo,
            PrintTicket^ printTicket
            ) override;

        virtual
        Boolean
        ThunkEndDocPrinter(
            void
            ) override;


        virtual
        Boolean
        ThunkAbortPrinter(
            void
            ) override;

        virtual
        void
        ThunkOpenSpoolStream(
            void
            ) override;
            
        virtual
        void
        ThunkCommitSpoolData(
            Int32                   bytes
            ) override;

        virtual
        Boolean
        ThunkCloseSpoolStream(
            void
            ) override;

        virtual
        Int32
        ThunkReportJobProgress(
            Int32                                                           jobId,
            JobOperation                                                    jobOperation,
            System::Windows::Xps::Packaging::PackagingAction                packagingAction
            ) override;

        property
        virtual 
        int
        JobIdentifier
        {
            int get() override;
        }    

        property
        virtual 
        Stream^
        SpoolStream
        {
            Stream^ get() override;
        }  

        private:            
            String^         printerName;

            Stream^         spoolerStream;

            IXpsPrintJob*   xpsPrintJob;

            int             jobIdentifier;
    };    
}
}
}
#endif

