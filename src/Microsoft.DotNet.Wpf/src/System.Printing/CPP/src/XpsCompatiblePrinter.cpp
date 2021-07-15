// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        Managed wrapper for Print Document Package API Interfaces.

--*/

#include "win32inc.hpp"
#include "Shlwapi.h"
#include <xpsobjectmodel_1.h>

using namespace System;
using namespace System::Collections::Specialized;
using namespace System::Printing;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;
using namespace System::Windows::Xps::Packaging;

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTERDATATYPES_HPP__
#include <PrinterDataTypes.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __XPSPRINTSTREAM_HPP__
#include <XpsPrintStream.hpp>
#endif




XpsCompatiblePrinter::
XpsCompatiblePrinter(
String^ printerName
) :
    _printerName(printerName),
    _printDocPackageTarget(nullptr),
    _xpsPackageTarget(nullptr),
    _xpsPackageStatusProvider(nullptr)
{
}

XpsCompatiblePrinter::
~XpsCompatiblePrinter()
{
    AbortPrinter();
    EndDocPrinter();
}

void
XpsCompatiblePrinter::
StartDocPrinter(
    DocInfoThree^ docInfo,
    PrintTicket^ printTicket,
    bool mustSetPrintJobIdentifier
    )
{
    int hr = 0;

    RCW::IPrintDocumentPackageTarget^ tempPrintDocPackageTarget;
    RCW::IXpsDocumentPackageTarget^ tempXpsPackageTarget;

    if (printTicket == nullptr)
    {
        printTicket = gcnew PrintTicket();
    }
    XpsPrintStream^ ticketStream = XpsPrintStream::CreateXpsPrintStream();
    printTicket->SaveTo(ticketStream);

    hr = PresentationNativeUnsafeNativeMethods::PrintToPackageTarget(
        _printerName,
        docInfo->docName,
        ticketStream->GetManagedIStream(),
        tempPrintDocPackageTarget,
        tempXpsPackageTarget);

    // Note: if MXDW was selected, but the user canceled the file prompt, this
    // will return an error code that we convert into PrintingCanceledException.
    if (hr == HRESULT_FROM_WIN32(ERROR_CANCELLED) ||
        hr == HRESULT_FROM_WIN32(ERROR_PRINT_CANCELLED))
    {
        throw gcnew PrintingCanceledException(
            hr,
            "PrintSystemException.PrintingCancelled.Generic"
            );
    }
    else
    {
        InternalPrintSystemException::ThrowIfNotCOMSuccess(hr);
    }

    if (mustSetPrintJobIdentifier)
    {
        _xpsPackageStatusProvider = gcnew RCW::PrintDocumentPackageStatusProvider(tempPrintDocPackageTarget);
    }
    
    _printDocPackageTarget = tempPrintDocPackageTarget;
    _xpsPackageTarget = tempXpsPackageTarget;
}

void
XpsCompatiblePrinter::
EndDocPrinter(
    void
    )
{
    try
    {
        if (_packageWriter != nullptr)
        {
            _packageWriter->Close();
            Marshal::FinalReleaseComObject(_packageWriter);
            _packageWriter = nullptr;
        }

        if (_printDocPackageTarget != nullptr)
        {
            Marshal::FinalReleaseComObject(_printDocPackageTarget);
            _printDocPackageTarget = nullptr;
        }

        if (_xpsPackageTarget != nullptr)
        {
            Marshal::FinalReleaseComObject(_xpsPackageTarget);
            _xpsPackageTarget = nullptr;
        }
    }
    catch (COMException^)
    {
        throw gcnew PrintingCanceledException();
    }
    catch (ArgumentException^)
    {
        throw gcnew PrintingCanceledException();
    }
}

void
XpsCompatiblePrinter::
AbortPrinter(
    void
    )
{
    try
    {
        if (_printDocPackageTarget != nullptr)
        {
            _printDocPackageTarget->Cancel();
        }

        if (_packageWriter != nullptr)
        {
            // Do not call Close on the packageWriter, if we do we may end up
            // printing the incomplete document instead of canceling
            Marshal::FinalReleaseComObject(_packageWriter);
            _packageWriter = nullptr;
        }
    }
    catch (COMException^)
    {
        throw gcnew PrintingCanceledException();
    }
    catch (ArgumentException^)
    {
        throw gcnew PrintingCanceledException();
    }
}

int
XpsCompatiblePrinter::JobIdentifier::
get(
    void
    )
{
    int jobId = 0;
    ManualResetEvent^ idEvent = _xpsPackageStatusProvider->JobIdAcquiredEvent;

    if (idEvent != nullptr)
    {
        idEvent->WaitOne();
        return _xpsPackageStatusProvider->JobId;
    }

    return jobId;
}

RCW::IXpsDocumentPackageTarget^
XpsCompatiblePrinter::XpsPackageTarget::
get(
void
)
{
    return _xpsPackageTarget;
}

void
XpsCompatiblePrinter::XpsOMPackageWriter::
set(
RCW::IXpsOMPackageWriter^ packageWriter
)
{
    _packageWriter = packageWriter;
}


