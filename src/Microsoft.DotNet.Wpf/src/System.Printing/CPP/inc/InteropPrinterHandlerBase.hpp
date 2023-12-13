// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPPRINTERHANDLERBASE_HPP__
#define __INTEROPPRINTERHANDLERBASE_HPP__
/*++
        
    Abstract:

        Interface for Win32 print APIs. This interface wraps a printer handle
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

    ///<Summary>
    /// Abstract interface to native handle based printer API's
    ///</Summary>
    private ref class PrinterThunkHandlerBase abstract : public System::Runtime::InteropServices::SafeHandle 
    {
        protected:
            
        PrinterThunkHandlerBase(
            void
        ) : SafeHandle(IntPtr::Zero, true)
        {            
        }
        
        public:
            
        virtual 
        Int32
        ThunkStartDocPrinter(
            DocInfoThree^         docInfo,
            PrintTicket^ printTicket
            ) = 0;

        virtual
        Boolean
        ThunkEndDocPrinter(
            void
            ) = 0;


        virtual
        Boolean
        ThunkAbortPrinter(
            void
            ) = 0;

        virtual 
        void
        ThunkOpenSpoolStream(
            void
            ) = 0;

        virtual 
        void
        ThunkCommitSpoolData(
            Int32                   bytes
            ) = 0;

        virtual 
        Boolean
        ThunkCloseSpoolStream(
            void
            ) = 0;

        virtual
        Int32
        ThunkReportJobProgress(
            Int32                                                           jobId,
            JobOperation                                                    jobOperation,
            System::Windows::Xps::Packaging::PackagingAction                packagingAction
            ) = 0;

        property
        virtual 
        int
        JobIdentifier
        {
            virtual int get() = 0;
        }    

        property
        virtual 
        Stream^
        SpoolStream
        {
            virtual Stream^ get() = 0;
        }    
    };

}
}
}
#endif

