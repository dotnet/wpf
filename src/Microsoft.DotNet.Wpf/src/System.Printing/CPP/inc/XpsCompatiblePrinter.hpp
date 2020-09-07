// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __XPSCOMPATIBLEPRINTER_HPP__
#define __XPSCOMPATIBLEPRINTER_HPP__
/*++

        Abstract:

        Managed wrapper for Print Document Package API Interfaces.


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
    using namespace System::Windows::Xps::Serialization;

    private ref class XpsCompatiblePrinter 
    {
        public:
           
        XpsCompatiblePrinter(
            String^ printerName
        );

        ~XpsCompatiblePrinter();

        virtual
        void
        StartDocPrinter(
            DocInfoThree^         docInfo,
            PrintTicket^ printTicket,
            bool mustSetPrintJobIdentifier
            );

        virtual
        void
        EndDocPrinter(
            void
            );


        virtual
        void
        AbortPrinter(
            void
            );

        
        property
        virtual 
        int
        JobIdentifier
        {
            int get();
        }    

        property
        RCW::IXpsDocumentPackageTarget^
        XpsPackageTarget
        {
            RCW::IXpsDocumentPackageTarget^ get();
        }

        property
        RCW::IXpsOMPackageWriter^
        XpsOMPackageWriter
        {
            void set(RCW::IXpsOMPackageWriter^ packageWriter);
        }

        private:
            String^ _printerName;

            RCW::IPrintDocumentPackageTarget^ _printDocPackageTarget;

            RCW::IXpsDocumentPackageTarget^ _xpsPackageTarget;

            RCW::PrintDocumentPackageStatusProvider^ _xpsPackageStatusProvider;

            RCW::IXpsOMPackageWriter^ _packageWriter;
    };
}
}
}
#endif

