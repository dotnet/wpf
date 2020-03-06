// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        This object is instantiated against an XPSEmitter object. It is a public object to be used
        to serialize Print Subsystem objeects.
--*/

#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Xml;
using namespace System::Xml::XPath;

using namespace System::Printing::Interop;
using namespace System::Drawing::Printing;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


using namespace System::Printing;
using namespace System::Windows::Documents;
using namespace System::Windows::Xps::Serialization;
using namespace System::Runtime::Serialization;
using namespace System::Threading;
using namespace System::Windows::Xps;
using namespace System::Windows::Xps::Packaging;

#include <XPSDocumentWriter.hpp>

/*--------------------------------------------------------------------------------------*/
/*                          XpsDocumentWriter Implementation                            */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        XpsDocumentWriter

    Description:
        Constructor of class instance

    Parameters:
        printQueue  -   target PrintQueue against which the
                        the writer is created.

    Return Value
        None
--*/
XpsDocumentWriter::
XpsDocumentWriter(
    PrintQueue^    printQueue
    ) :
    currentState(DocumentWriterState::kRegularMode),
    _currentUserState(nullptr),
    _mxdwPackage(nullptr),
    _mxdwManager(nullptr),
    _sourceXpsDocument(nullptr),
    _sourceXpsFixedDocumentSequenceReader(nullptr),
    _isDocumentCloned(false),
    _writingCancelledEventHandlersCount(0)
{
    if (printQueue)
    {
        destinationDocument   = nullptr;
        destinationPrintQueue = printQueue;
        currentWriteLevel     = PrintTicketLevel::None;
        InitializeSequences();
    }
    else
    {
        throw gcnew ArgumentNullException("printQueue");
    }
}

/*++
    Function Name:
        XpsDocumentWriter

    Description:
        Constructor of class instance

    Parameters:
        document  -     target XpsDocument against which the
                        the writer is created.

    Return Value
        None
--*/
XpsDocumentWriter::
XpsDocumentWriter(
    XpsDocument^    document
    ) :
    currentState(DocumentWriterState::kRegularMode),
    _currentUserState(nullptr),
    _mxdwPackage(nullptr),
    _mxdwManager(nullptr),
    _sourceXpsDocument(nullptr),
    _sourceXpsFixedDocumentSequenceReader(nullptr),
    _isDocumentCloned(false),
    _writingCancelledEventHandlersCount(0)
{
    #pragma warning ( push )
    #pragma warning ( disable:4691 )
    if (document!=nullptr)
    #pragma warning ( pop )
    {
        destinationPrintQueue = nullptr;
        destinationDocument   = document;
        currentWriteLevel     = PrintTicketLevel::None;
        InitializeSequences();
    }
    else
    {
        throw gcnew ArgumentNullException("document");
    }
}


/*++
    Function Name:
        XpsDocumentWriter

    Description:
        Internal constructor of class instance

    Parameters:
        printQueue     -   target PrintQueue against which the
                           the writer is created.
        bogus          -   this is just so we can have another internal constructor

    Return Value
        None
--*/
XpsDocumentWriter::
XpsDocumentWriter(
    PrintQueue^     printQueue,
    Object^         bogus
    ) :
    currentState(DocumentWriterState::kRegularMode),
    _currentUserState(nullptr),
    _mxdwPackage(nullptr),
    _mxdwManager(nullptr),
    _sourceXpsDocument(nullptr),
    _sourceXpsFixedDocumentSequenceReader(nullptr),
    _isDocumentCloned(false),
    _writingCancelledEventHandlersCount(0)
{
    if (printQueue)
    {
        destinationPrintQueue = printQueue;
        destinationDocument   = nullptr;
        currentWriteLevel     = PrintTicketLevel::None;
        InitializeSequences();
    }
    else
    {
        throw gcnew ArgumentNullException("printQueue");
    }
}

void
XpsDocumentWriter::
EndBatchMode(
    void
    )
{
    currentState = DocumentWriterState::kDone;
}

/*--------------------------------------------------------------------------------------*/
/*                                 Synchronous Functions                                */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        Write

    Description:
        Uses the writer to serialize a full document.

    Parameters:
        documentPath  -   Path to the document we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    String^      documentPath
    )
{
    VerifyAccess();
    Write(documentPath,XpsDocumentNotificationLevel::ReceiveNotificationEnabled);
}

/*++
    Function Name:
        Write

    Description:
        Uses the writer to serialize a full document.

    Parameters:
        documentPath        -   Path to the document we want to serialize.
        notificationLevel   -   An indication whether we want to a reference copy,
                                or a deep tree serialization cloning

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    String^                         documentPath,
    XpsDocumentNotificationLevel    notificationLevel
    )
{
    VerifyAccess();

    switch(notificationLevel)
    {
        case  XpsDocumentNotificationLevel::ReceiveNotificationEnabled:
        {
            //
            // We need to set an identification that this is document cloning
            //
            _isDocumentCloned = true;


            try
            {
                _sourceXpsDocument                        = gcnew XpsDocument(documentPath, FileAccess::Read);
                FixedDocumentSequence^  documentSequence  = _sourceXpsDocument->GetFixedDocumentSequence();
                if(documentSequence == nullptr )
                {
                   XpsWriterException::ThrowException("XpsWriter.InvalidXps");
                }
                _sourceXpsFixedDocumentSequenceReader = _sourceXpsDocument->FixedDocumentSequenceReader;

                Write(documentSequence);

            }
            finally
            {
                if(_sourceXpsDocument!=nullptr)
                {
                    _sourceXpsDocument->Close();
                }
            }

            break;
        }

        case XpsDocumentNotificationLevel::ReceiveNotificationDisabled:
        case XpsDocumentNotificationLevel::None:
        {
            XpsDocument^            srcXpsDocument   = gcnew XpsDocument(documentPath, FileAccess::Read);
            FixedDocumentSequence^  documentSequence = srcXpsDocument->GetFixedDocumentSequence();
            srcXpsDocument->Close();

            if(documentSequence)
            {
                if(destinationPrintQueue!=nullptr)
                {
                    destinationPrintQueue->AddJob(destinationPrintQueue->CurrentJobSettings->Description,
                                                  documentPath,
                                                  true);
                }
                else
                {
                    throw gcnew NotSupportedException;
                }
            }
            else
            {
                XpsWriterException::ThrowException("XpsWriter.InvalidXps");
            }

            break;
        }
    }

}


/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize an DocumentPaginator.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    DocumentPaginator^      documentPaginator
    )
{
    VerifyAccess();

    if(BeginWrite(false,
                  false,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        SaveAsXaml(documentPaginator,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a DocumentSequence.

    Parameters:
        documentSequence  -   DocumentSequence we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
BeginPrintFixedDocumentSequence(
    FixedDocumentSequence^       documentSequence,
    Int32&                       printJobIdentifier
    )
{
    BeginPrintFixedDocumentSequence(documentSequence, nullptr, printJobIdentifier);
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a DocumentSequence.

    Parameters:
        documentSequence  -   DocumentSequence we want to serialize.
        printJobIdentifier -  job identifier (return)

    Return Value
        None
--*/
void
XpsDocumentWriter::
BeginPrintFixedDocumentSequence(
    FixedDocumentSequence^       documentSequence,
    PrintTicket^                 printTicket,
    Int32&                       printJobIdentifier
    )
{
    PrintTicketLevel printTicketLevel =
        (printTicket == nullptr) ? PrintTicketLevel::None
                                 : PrintTicketLevel::FixedDocumentSequencePrintTicket;
    if(BeginWrite(false,
                  false,
                  true,
                  printTicket,
                  printTicketLevel,
                  true) == true)
    {
        _manager->SaveAsXaml(documentSequence);

        if (destinationPrintQueue != nullptr)
        {
            destinationPrintQueue->EnsureJobId(_manager);
        }

        printJobIdentifier = _manager->JobIdentifier;
    }
}

void
XpsDocumentWriter::
EndPrintFixedDocumentSequence(
    void
    )
{
    EndWrite(true);
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize an DocumentPaginator.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.
        printTicket        -   PrintTicket to apply to the DocumentPaginator

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    DocumentPaginator^      documentPaginator,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentPrintTicket,
                  false) == true)
    {
        SaveAsXaml(documentPaginator,true);
    }
}


/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a Visual.

    Parameters:
        visual  -   Visual we want to serialize.

    Return Value
        None
--*/

void
XpsDocumentWriter::
Write(
    Visual^             visual
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        SaveAsXaml(visual,true);
    }

}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a Visual.

    Parameters:
        visual      -   Visual we want to serialize.
        printTicket -   PrintTicket to apply to the Visual.

    Return Value
        None
--*/

void
XpsDocumentWriter::
Write(
    Visual^             visual,
    PrintTicket^        printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedPagePrintTicket,
                  false) == true)
    {
        SaveAsXaml(visual,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a DocumentSequence.

    Parameters:
        documentSequence  -   DocumentSequence we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedDocumentSequence^       documentSequence
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        SaveAsXaml(documentSequence,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a DocumentSequence.

    Parameters:
        documentSequence  -   DocumentSequence we want to serialize.
        printTicket       -   PrintTicket to apply to the DocumentSequence.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedDocumentSequence^       documentSequence,
    PrintTicket^                 printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentSequencePrintTicket,
                  false) == true)
    {
        SaveAsXaml(documentSequence,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a FixedDocument.

    Parameters:
        fixedDocument  -   FixedDocument we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedDocument^          fixedDocument
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        SaveAsXaml(fixedDocument,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a FixedDocument.

    Parameters:
        fixedDocument  -   FixedDocument we want to serialize.
        printTicket    -   PrintTicket to apply to the FixedDocument.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedDocument^          fixedDocument,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentPrintTicket,
                  false) == true)
    {
        SaveAsXaml(fixedDocument,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a FixedPage.

    Parameters:
        fixedPage  -   FixedPage we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedPage^              fixedPage
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        SaveAsXaml(fixedPage,true);
    }
}

/*++
    Function Name:
        Write

    Description:
        Uses the XPSEmitter item to serialize a FixedPage.

    Parameters:
        fixedPage       -   FixedPage we want to serialize.,
        printTicket     -   PrintTicket to apply to the FixedPage.

    Return Value
        None
--*/
void
XpsDocumentWriter::
Write(
    FixedPage^              fixedPage,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  false,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedPagePrintTicket,
                  false) == true)
    {
        SaveAsXaml(fixedPage,true);
    }
}

/*--------------------------------------------------------------------------------------*/
/*                               Asynchronous Functions                                 */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        WriteAsync

    Description:
        Uses the writer to serialize a full document.

    Parameters:
        documentPath  -   Path to the document we want to serialize.

    Return Value
        None
--*/
void
XpsDocumentWriter::
WriteAsync(
    String^      documentPath
    )
{
    VerifyAccess();

    WriteAsync(documentPath,XpsDocumentNotificationLevel::ReceiveNotificationEnabled);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Uses the writer to serialize a full document.

    Parameters:
        documentPath        -   Path to the document we want to serialize.
        notificationLevel   -   An indication whether we want to a reference copy,
                                or a deep tree serialization cloning

    Return Value
        None
--*/
void
XpsDocumentWriter::
WriteAsync(
    String^                         documentPath,
    XpsDocumentNotificationLevel    notificationLevel
    )
{
    VerifyAccess();

    switch(notificationLevel)
    {
        case  XpsDocumentNotificationLevel::ReceiveNotificationEnabled:
        {
            _isDocumentCloned = true;

            _sourceXpsDocument                        = gcnew XpsDocument(documentPath, FileAccess::Read);
            FixedDocumentSequence^  documentSequence  = _sourceXpsDocument->GetFixedDocumentSequence();
            _sourceXpsFixedDocumentSequenceReader     = _sourceXpsDocument->FixedDocumentSequenceReader;
            if(_sourceXpsFixedDocumentSequenceReader == nullptr )
            {
               XpsWriterException::ThrowException("XpsWriter.InvalidXps");
            }
            WriteAsync(documentSequence);
            break;
        }

        case XpsDocumentNotificationLevel::ReceiveNotificationDisabled:
        case XpsDocumentNotificationLevel::None:
        {
            //
            // This can't run Async as the underlying write steam doesn't support it.
            //
            Write(documentPath,
                  notificationLevel);
            break;
        }
    }

}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentPaginator using the XPSEmitter item.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    DocumentPaginator^  documentPaginator
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        _manager->SaveAsXaml(documentPaginator);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentPaginator using the XPSEmitter item.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    DocumentPaginator^  documentPaginator,
    PrintTicket^        printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentPrintTicket,
                  false) == true)
    {
        _manager->SaveAsXaml(documentPaginator);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentPaginator using the XPSEmitter item.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    DocumentPaginator^  documentPaginator,
    Object^             userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(documentPaginator);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentPaginator using the XPSEmitter item.

    Parameters:
        documentPaginator  -   DocumentPaginator we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    DocumentPaginator^  documentPaginator,
    PrintTicket^        printTicket,
    Object^             userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(documentPaginator,printTicket);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the Visual using the XPSEmitter item.

    Parameters:
        visual             -   Visual we want to serialize.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    Visual^             visual
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        _manager->SaveAsXaml(visual);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the Visual using the XPSEmitter item.

    Parameters:
        visual             -   Visual we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    Visual^             visual,
    PrintTicket^        printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedPagePrintTicket,
                  false) == true)
    {
        _manager->SaveAsXaml(visual);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the Visual using the XPSEmitter item.

    Parameters:
        visual             -   Visual we want to serialize.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    Visual^             visual,
    Object^             userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(visual);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the Visual using the XPSEmitter item.

    Parameters:
        visual             -   Visual we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    Visual^             visual,
    PrintTicket^        printTicket,
    Object^             userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(visual, printTicket);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentSequence using the XPSEmitter item.

    Parameters:
        documentSequence   -   DocumentSequence we want to serialize.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocumentSequence^  documentSequence
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        _manager->SaveAsXaml(documentSequence);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentSequence using the XPSEmitter item.

    Parameters:
        documentSequence   -   DocumentSequence we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocumentSequence^  documentSequence,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentSequencePrintTicket,
                  false) == true)
    {
        _manager->SaveAsXaml(documentSequence);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentSequence using the XPSEmitter item.

    Parameters:
        documentSequence   -   DocumentSequence we want to serialize.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocumentSequence^  documentSequence,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(documentSequence);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the DocumentSequence using the XPSEmitter item.

    Parameters:
        documentSequence   -   DocumentSequence we want to serialize.
        printTicket     -   PrintTicket to apply to the FixedPage.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocumentSequence^  documentSequence,
    PrintTicket^            printTicket,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(documentSequence,printTicket);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedDocument using the XPSEmitter item.

    Parameters:
        fixedDocument      -   FixedDocument we want to serialize.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocument^          fixedDocument
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        _manager->SaveAsXaml(fixedDocument);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedDocument using the XPSEmitter item.

    Parameters:
        fixedDocument      -   FixedDocument we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocument^          fixedDocument,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedDocumentPrintTicket,
                  false) == true)
    {
        _manager->SaveAsXaml(fixedDocument);

        EndWrite(false);
    }

}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedDocument using the XPSEmitter item.

    Parameters:
        fixedDocument      -   FixedDocument we want to serialize.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocument^          fixedDocument,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(fixedDocument);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedDocument using the XPSEmitter item.

    Parameters:
        fixedDocument      -   FixedDocument we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedDocument^          fixedDocument,
    PrintTicket^            printTicket,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(fixedDocument,printTicket);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedPage using the XPSEmitter item.

    Parameters:
        fixedPage          -   FixedPage we want to serialize.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedPage^              fixedPage
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  true,
                  nullptr,
                  PrintTicketLevel::None,
                  false) == true)
    {
        _manager->SaveAsXaml(fixedPage);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedPage using the XPSEmitter item.

    Parameters:
        fixedPage          -   FixedPage we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedPage^              fixedPage,
    PrintTicket^            printTicket
    )
{
    VerifyAccess();
    if(BeginWrite(false,
                  true,
                  printTicket!=nullptr ? true : false,
                  printTicket,
                  PrintTicketLevel::FixedPagePrintTicket,
                  false) == true)
    {
        _manager->SaveAsXaml(fixedPage);

        EndWrite(false);
    }
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedPage using the XPSEmitter item.

    Parameters:
        fixedPage          -   FixedPage we want to serialize.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedPage^              fixedPage,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(fixedPage);
}

/*++
    Function Name:
        WriteAsync

    Description:
        Asynchronously serialize the FixedPage using the XPSEmitter item.

    Parameters:
        fixedPage          -   FixedPage we want to serialize.
        printTicket        -   PrintTicket to apply to the FixedPage.
        userSuppliedState  -   User supplied information.

    Return Value
        void
--*/
void
XpsDocumentWriter::
WriteAsync(
    FixedPage^              fixedPage,
    PrintTicket^            printTicket,
    Object^                 userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(fixedPage,printTicket);
}

void
XpsDocumentWriter::
CancelAsync(
    )
{
    VerifyAccess();

    #pragma warning ( push )
    #pragma warning ( disable:4691 )
    switch(currentState)
    {
        case DocumentWriterState::kBatchMode:
            XpsWriterException::ThrowException("XPSWriter.BatchMode");
            break;

        case DocumentWriterState::kRegularMode:
            XpsWriterException::ThrowException("XPSWriter.WriteNotCalled");
            break;

        case DocumentWriterState::kDone:
            if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((XpsOMSerializationManagerAsync^)_manager)->CancelAsync();
            }
            else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((XpsSerializationManagerAsync^)_manager)->CancelAsync();
            }
            else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((NgcSerializationManagerAsync^)_manager)->CancelAsync();
            }
            currentState = DocumentWriterState::kCancelled;
            break;

        case DocumentWriterState::kCancelled:
             XpsWriterException::ThrowException("XPSWriter.Cancelled");
            break;
    }
    #pragma warning ( pop )
}

/*++
    Function Name:
        ForwardUserPrintTicket

    Description:
        Supplies the PrintTicket for the the given level if one exists.

    Parameters:
        level   - The scope of the object being serialized.

    Return Value
        None
--*/
void
XpsDocumentWriter::
ForwardUserPrintTicket(
    Object^                                         sender,
    XpsSerializationPrintTicketRequiredEventArgs^   args
    )
{
    if(currentWriteLevel == args->PrintTicketLevel)
    {
        args->PrintTicket = currentUserPrintTicket;
    }
    else
    {
        WritingPrintTicketRequiredEventArgs^ forwardArgs = gcnew WritingPrintTicketRequiredEventArgs(args->PrintTicketLevel,
                                                                                                     (Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]);


        switch(args->PrintTicketLevel)
        {
            case PrintTicketLevel::FixedDocumentSequencePrintTicket:
            {
                _printTicketSequences[(Int32)PrintTicketLevel::FixedDocumentPrintTicket] = (Int32) 1;
                _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
                break;
            }

            case PrintTicketLevel::FixedDocumentPrintTicket:
            {
                _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
                break;
            }
        }

        _printTicketSequences[(Int32)args->PrintTicketLevel] = (Int32)_printTicketSequences[(Int32)args->PrintTicketLevel] + 1;

        WritingPrintTicketRequired(this,
                                   forwardArgs);

        args->PrintTicket = forwardArgs->CurrentPrintTicket;
    }
}

/*++
    Function Name:
        CloneSourcePrintTicket

    Description:
        Supplies the PrintTicket for the the given level if one exists.

    Parameters:
        level   - The scope of the object being serialized.

    Return Value
        None
--*/
void
XpsDocumentWriter::
CloneSourcePrintTicket(
    Object^                                         sender,
    XpsSerializationPrintTicketRequiredEventArgs^   args
    )
{
    PrintTicket^    clonedPrintTicket = nullptr;

    switch(args->PrintTicketLevel)
    {
        case PrintTicketLevel::FixedDocumentSequencePrintTicket:
        {
            _printTicketSequences[(Int32)PrintTicketLevel::FixedDocumentPrintTicket] = (Int32) 1;
            _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
            clonedPrintTicket = _sourceXpsFixedDocumentSequenceReader->PrintTicket;
            break;
        }

        case PrintTicketLevel::FixedDocumentPrintTicket:
        {
            _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
            clonedPrintTicket = _sourceXpsFixedDocumentSequenceReader->
                                FixedDocuments[(Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]-1]->PrintTicket;
            break;
        }

        case PrintTicketLevel::FixedPagePrintTicket:
        {
            clonedPrintTicket = _sourceXpsFixedDocumentSequenceReader->
                                FixedDocuments[(Int32)_printTicketSequences[(Int32)PrintTicketLevel::FixedDocumentPrintTicket]-2]->
                                FixedPages[(Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]-1]->PrintTicket;
            break;
        }
    }

    _printTicketSequences[(Int32)args->PrintTicketLevel] = (Int32)_printTicketSequences[(Int32)args->PrintTicketLevel] + 1;

    args->PrintTicket = clonedPrintTicket;
}

/*++
    Function Name:
        ForwardWriteCompletedEvent

    Description:
        Forwards the write completed event from the serializer to the user

    Parameters:
        sender  -   the sender object.
        args    -   data from the completed async call.

    Return Value
        None
--*/
void
XpsDocumentWriter::
ForwardWriteCompletedEvent(
    Object^                             sender,
    XpsSerializationCompletedEventArgs^ args
    )
{
    bool cancelled = args->Cancelled;
    //
    // if the exception type is Printing Canceled
    // ignore the args canceled and set canceled to true;
    //
    if(!cancelled &&
        args->Error!= nullptr &&
        System::Printing::PrintingCanceledException::
        typeid->Equals(args->Error->GetType()) )
    {
        cancelled = true;
    }
    Exception^  exception =args->Error;
    if(destinationPrintQueue!=nullptr)
    {
        try
        {
            if(!cancelled && args->Error==nullptr)
            {
                destinationPrintQueue->DisposeSerializationManager(false/*close*/);
            }
            else
            {
                destinationPrintQueue->DisposeSerializationManager(true/*abort*/);
            }
        }
        //
        // If an exception is thorwn at shut down this will override
        // the previous exception if any
        //
        catch( PrintingCanceledException^ e )
        {
            cancelled = true;
            exception = e;
        }
        catch( PrintJobException^ e )
        {
            cancelled = false;
            exception = e;
        }
    }
    else
    {
        destinationDocument->DisposeSerializationManager();
    }

    if(destinationPrintQueue==nullptr)
    {
        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        if(_mxdwManager!=nullptr)
        #pragma warning ( pop )
        {
            _mxdwPackage->Close();
            _mxdwManager->Commit();
            _mxdwPackage = nullptr;
            _mxdwManager = nullptr;
        }
    }
    if(_isDocumentCloned)
    {
        if(_sourceXpsDocument != nullptr)
        {
            _sourceXpsDocument->Close();
        }
    }

    WritingCompletedEventArgs^ forwardArgs =
        gcnew WritingCompletedEventArgs(cancelled,
                                                           _currentUserState,
                                                           exception);


    WritingCompleted(this,
                              forwardArgs);
}

/*++
    Function Name:
        ForwardProgressChangedEvent

    Description:
        Forwards the progress changed event from the serializer to the user

    Parameters:
        sender  -   the sender object.
        args    -   data from the completed async call.

    Return Value
        None
--*/
void
XpsDocumentWriter::
ForwardProgressChangedEvent(
    Object^                                     sender,
    XpsSerializationProgressChangedEventArgs^   args
    )
{

    WritingProgressChangedEventArgs^    forwardArgs = gcnew WritingProgressChangedEventArgs(TranslateProgressChangeLevel(args->WritingLevel),
                                                                                           (Int32)_writingProgressSequences[(Int32)args->WritingLevel],
                                                                                            args->ProgressPercentage,
                                                                                            _currentUserState);

    switch(args->WritingLevel)
    {
        case XpsWritingProgressChangeLevel::FixedDocumentSequenceWritingProgress:
        {
            _writingProgressSequences[(Int32)XpsWritingProgressChangeLevel::FixedDocumentWritingProgress] = (Int32) 1;
            _writingProgressSequences[(Int32)XpsWritingProgressChangeLevel::FixedPageWritingProgress] = (Int32) 1;
            break;
        }

        case XpsWritingProgressChangeLevel::FixedDocumentWritingProgress:
        {
            _writingProgressSequences[(Int32)XpsWritingProgressChangeLevel::FixedPageWritingProgress] = (Int32) 1;
            break;
        }
    }

    _writingProgressSequences[(Int32)args->WritingLevel] = (Int32)_writingProgressSequences[(Int32)args->WritingLevel] + 1;

    WritingProgressChanged(this,
                           forwardArgs);
}

WritingProgressChangeLevel
XpsDocumentWriter::
TranslateProgressChangeLevel(
             System::
             Windows::
             Xps::Serialization::XpsWritingProgressChangeLevel xpsChangeLevel )
{
    WritingProgressChangeLevel changeLevel = System::Windows::Documents::Serialization::WritingProgressChangeLevel::None;

    switch( xpsChangeLevel )

    {

        case System::Windows::Documents::Serialization::WritingProgressChangeLevel::None:
            changeLevel = System::Windows::Documents::Serialization::WritingProgressChangeLevel::None;
            break;

        case System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedDocumentSequenceWritingProgress:
            changeLevel =  System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedDocumentSequenceWritingProgress;
            break;

        case System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedDocumentWritingProgress:
            changeLevel =  System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedDocumentWritingProgress;
            break;

        case System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedPageWritingProgress:
            changeLevel =  System::Windows::Documents::Serialization::WritingProgressChangeLevel::FixedPageWritingProgress;
            break;
    }
    return changeLevel;
}
/*++

    Function Name:
        CreateVisualsCollator

    Description:
        Creates and returns a VisualsToXPSDocument visuals collater for batch writing.

    Parameters:

        documentSequencePrintTicket     -   PrintTicket to use on the FixedDocumentSequence
        documentPrintTicket             -   PrintTicket to use on the FixedDocument

    Return Value:

        VisualsToXpsDocument

--*/
SerializerWriterCollator^
XpsDocumentWriter::
CreateVisualsCollator(
    PrintTicket^    documentSequencePrintTicket OPTIONAL,
    PrintTicket^    documentPrintTicket         OPTIONAL
    )
{
    VerifyAccess();

    VisualsToXpsDocument^   collater = nullptr;

    switch(currentState)
    {
        case DocumentWriterState::kBatchMode:
            XpsWriterException::ThrowException("XPSWriter.BatchMode");
            break;

        case DocumentWriterState::kDone:
            XpsWriterException::ThrowException("XPSWriter.DoneWriting");
            break;

        case DocumentWriterState::kRegularMode:
            currentState = DocumentWriterState::kBatchMode;

            if(destinationPrintQueue!=nullptr)
            {
                collater =  gcnew VisualsToXpsDocument(this,
                                                       destinationPrintQueue,
                                                       documentSequencePrintTicket,
                                                       documentPrintTicket);
            }
            else
            {
                collater =  gcnew VisualsToXpsDocument(this,
                                                       destinationDocument,
                                                       documentSequencePrintTicket,
                                                       documentPrintTicket);
            }
            break;
    }

    return collater;
}

/*++

    Function Name:
        CreateVisualsCollator

    Description:
        Creates and returns a VisualsToXpsDocument visuals collater for batch writing.

    Parameters:

        None.

    Return Value:

        VisualsToXpsDocument

--*/
SerializerWriterCollator^
XpsDocumentWriter::
CreateVisualsCollator(
    )
{
    VerifyAccess();

    VisualsToXpsDocument^   collater = nullptr;

    switch(currentState)
    {
        case DocumentWriterState::kBatchMode:
            XpsWriterException::ThrowException("XPSWriter.BatchMode");
            break;

        case DocumentWriterState::kDone:
            XpsWriterException::ThrowException("XPSWriter.DoneWriting");
            break;

        case DocumentWriterState::kRegularMode:
            currentState = DocumentWriterState::kBatchMode;
            if(destinationPrintQueue != nullptr)
            {
                collater = gcnew VisualsToXpsDocument(this,
                                                      destinationPrintQueue);
            }
            else
            {
                collater = gcnew VisualsToXpsDocument(this,
                                                      destinationDocument);
            }
            break;
    }

    return collater;
}

void
XpsDocumentWriter::
SetPrintTicketEventHandler(
    System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
    XpsSerializationPrintTicketRequiredEventHandler^                  eventHandler
    )
{
    #pragma warning ( push )
    #pragma warning ( disable:4691 )
    if (System::Windows::Xps::Serialization::XpsOMSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManager^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManager^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }
    else if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManagerAsync^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManagerAsync^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManager^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManagerAsync^)manager)->XpsSerializationPrintTicketRequired += eventHandler;
    }

    #pragma warning ( pop )
}

void
XpsDocumentWriter::
CurrentUserPrintTicket::set(
    PrintTicket^ userPrintTicket
    )
{
    currentUserPrintTicket = userPrintTicket;
}

void
XpsDocumentWriter::
CurrentWriteLevel::set(
    System::Windows::Xps::Serialization::PrintTicketLevel writeLevel
    )
{
    currentWriteLevel = writeLevel;
}

void
XpsDocumentWriter::
SetCompletionEventHandler(
    System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
    Object^                                                           userState
    )
{

    if(userState != nullptr)
    {
        _currentUserState = userState;
    }
    if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManagerAsync^)manager)->
            XpsSerializationCompleted += gcnew XpsSerializationCompletedEventHandler(this,
            &XpsDocumentWriter::ForwardWriteCompletedEvent);
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManagerAsync^)manager)->
            XpsSerializationCompleted += gcnew XpsSerializationCompletedEventHandler(this,
                                                                                     &XpsDocumentWriter::ForwardWriteCompletedEvent);
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManagerAsync^)manager)->
            XpsSerializationCompleted += gcnew XpsSerializationCompletedEventHandler(this,
                                                                                     &XpsDocumentWriter::ForwardWriteCompletedEvent);
    }
}

void
XpsDocumentWriter::
SetProgressChangedEventHandler(
    System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
    Object^                                                           userState
    )
{
    if(userState != nullptr)
    {
        _currentUserState = userState;
    }
    
    if (System::Windows::Xps::Serialization::XpsOMSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManager^)manager)->
            XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
            &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManager^)manager)->
        XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
                                                                                             &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
    else if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManagerAsync^)manager)->
            XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
            &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManagerAsync^)manager)->
        XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
                                                                                             &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManager^)manager)->
        XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
                                                                                             &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManagerAsync^)manager)->
        XpsSerializationProgressChanged += gcnew XpsSerializationProgressChangedEventHandler(this,
                                                                                             &XpsDocumentWriter::ForwardProgressChangedEvent);
    }
}


void
XpsDocumentWriter::
InitializeSequences(
    void
    )
{
    _printTicketSequences = gcnew ArrayList();

    for(Int32 numberOfSequences = 0;
         numberOfSequences <= (Int32)PrintTicketLevel::FixedPagePrintTicket;
         numberOfSequences++)
    {
        _printTicketSequences->Add(1);
    }

    _writingProgressSequences = gcnew ArrayList();

    for(Int32 numberOfSequences = 0;
         numberOfSequences <= (Int32)XpsWritingProgressChangeLevel::FixedPageWritingProgress;
         numberOfSequences++)
    {
        _writingProgressSequences->Add(1);
    }
}

bool
XpsDocumentWriter::
BeginWrite(
    bool                batchMode,
    bool                asyncMode,
    bool                setPrintTicketHandler,
    PrintTicket^        printTicket,
    PrintTicketLevel    printTicketLevel,
    bool                jobIdentifierSet
    )
{
    bool proceedEnabled = false;

    switch(currentState)
    {
        case DocumentWriterState::kBatchMode:
        {
            XpsWriterException::ThrowException("XPSWriter.BatchMode");
            break;
        }

        case DocumentWriterState::kCancelled:
        case DocumentWriterState::kDone:
        {
            XpsWriterException::ThrowException("XPSWriter.DoneWriting");
            break;
        }

        case DocumentWriterState::kRegularMode:
        {
            try
            {
                if(asyncMode == false)
                {
                    if(destinationPrintQueue != nullptr)
                    {
                            if(MxdwConversionRequired(destinationPrintQueue))
                            {
                                try
                                {
                                    String^ mxdwDocumentName = MxdwInitializeOptimizationConversion(destinationPrintQueue);
                                    //
                                    // Create the corresponding XPS Document and this is
                                    // what we use for printing to MXDW
                                    //
                                    this->CreateXPSDocument(mxdwDocumentName);
                                    _manager = destinationDocument->CreateSerializationManager(batchMode);
                                }
                                catch (System::IO::IOException^ exception)
                                {
                                    XpsSerializationCompletedEventArgs^ args = gcnew XpsSerializationCompletedEventArgs(false,
                                                                                                                        nullptr,
                                                                                                                        exception);

                                    ForwardWriteCompletedEvent(this,args);
                                    break;
                                }
                            }
                            else
                            {
                                // When printing to XPS OM we won't get another chance to set the document sequence print ticket
                                // call into the WritingPrintTicketRequired event to see if the user wants to set it
                                if (setPrintTicketHandler && destinationPrintQueue->IsXpsOMPrintingSupported())
                                {
                                    currentWriteLevel = printTicketLevel;
                                    currentUserPrintTicket = printTicket;
                                    XpsSerializationPrintTicketRequiredEventArgs^ args = gcnew XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel::FixedDocumentSequencePrintTicket, 0);
                                    if (_isDocumentCloned)
                                    {
                                        CloneSourcePrintTicket(this, args);
                                    }
                                    else
                                    {
                                        ForwardUserPrintTicket(this, args);
                                    }

                                    // In the StartXpsPrintJob there is an ambiguity problem between PrintJob PrintTicket and
                                    // DocumentSequence PrintTicket, they should be one and the same, but WPF may end up
                                    // assigning different print tickets to each, the PrintJob PrintTicket will end up
                                    // overriding the DocumentSequence PrintTicket, so we should do the same here, if the user
                                    // provided a PrintTicket in the Write method, that print ticket gets set directly to the 
                                    // PrintJob and whatever comes from the WritingPrintTicketRequired event gets ignored,
                                    // if the user only sets the print ticket in the event then that print ticket should
                                    // get passed as the job level print ticket
                                    if (printTicket == nullptr && args->PrintTicket != nullptr)
                                    {
                                        printTicket = args->PrintTicket;
                                    }
                                }

                                _manager = destinationPrintQueue->CreateSerializationManager(batchMode,jobIdentifierSet,printTicket);
                            }
                    }
                    else
                    {
                        _manager = destinationDocument->CreateSerializationManager(batchMode);
                    }
                }
                else
                {
                    if(destinationPrintQueue != nullptr)
                    {
                        if(MxdwConversionRequired(destinationPrintQueue))
                        {
                            try
                            {
                                String^ mxdwDocumentName = MxdwInitializeOptimizationConversion(destinationPrintQueue);
                                //
                                // Create the corresponding XPS Document and this is
                                // what we use for printing to MXDW
                                //
                                this->CreateXPSDocument(mxdwDocumentName);
                                _manager = destinationDocument->CreateAsyncSerializationManager(batchMode);
                            }
                            catch (System::IO::IOException^ exception)
                            {
                                XpsSerializationCompletedEventArgs^ args = gcnew XpsSerializationCompletedEventArgs(false,
                                                                                                                    nullptr,
                                                                                                                    exception);

                                ForwardWriteCompletedEvent(this,args);
                                break;
                            }
                        }
                        else
                        {
                            // When printing to XPS OM we won't get another chance to set the document sequence print ticket
                            // call into the WritingPrintTicketRequired event to see if the user wants to set it
                            if (setPrintTicketHandler && destinationPrintQueue->IsXpsOMPrintingSupported())
                            {
                                currentWriteLevel = printTicketLevel;
                                currentUserPrintTicket = printTicket;
                                XpsSerializationPrintTicketRequiredEventArgs^ args = gcnew XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel::FixedDocumentSequencePrintTicket, 0);
                                if (_isDocumentCloned)
                                {
                                    CloneSourcePrintTicket(this, args);
                                }
                                else
                                {
                                    ForwardUserPrintTicket(this, args);
                                }

                                // In the StartXpsPrintJob there is an ambiguity problem between PrintJob PrintTicket and
                                // DocumentSequence PrintTicket, they should be one and the same, but WPF may end up
                                // assigning different print tickets to each, the PrintJob PrintTicket will end up
                                // overriding the DocumentSequence PrintTicket, so we should do the same here, if the user
                                // provided a PrintTicket in the Write method, that print ticket gets set directly to the 
                                // PrintJob and whatever comes from the WritingPrintTicketRequired event gets ignored,
                                // if the user only sets the print ticket in the event then that print ticket should
                                // get passed as the job level print ticket
                                if (printTicket == nullptr && args->PrintTicket != nullptr)
                                {
                                    printTicket = args->PrintTicket;
                                }
                            }

                            _manager = destinationPrintQueue->CreateAsyncSerializationManager(batchMode, jobIdentifierSet, printTicket);
                        }
                    }
                    else
                    {
                        _manager = destinationDocument->CreateAsyncSerializationManager(batchMode);
                    }
                    SetCompletionEventHandler(_manager,nullptr);
                }
            }
            catch(PrintingCanceledException^   exception)
            {
                OnWritingCanceled(this,exception);
                break;
            }

            currentWriteLevel      = printTicketLevel;
            currentUserPrintTicket = printTicket;

            if(setPrintTicketHandler)
            {

                XpsSerializationPrintTicketRequiredEventHandler^ eventHandler = nullptr;

                if(_isDocumentCloned)
                {

                    eventHandler = gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                         &XpsDocumentWriter::CloneSourcePrintTicket);
                }
                else
                {
                    eventHandler = gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                         &XpsDocumentWriter::ForwardUserPrintTicket);
                }

                SetPrintTicketEventHandler(_manager,eventHandler);
            }

            SetProgressChangedEventHandler(_manager,nullptr);

            proceedEnabled = true;
            break;
        }

    }

    return proceedEnabled;
}

void
XpsDocumentWriter::
EndWrite(
    bool disposeManager
    )
{
    EndWrite( disposeManager, false );
}

void
XpsDocumentWriter::
EndWrite(
    bool disposeManager,
    bool abort
    )
{
    try
    {
        if(disposeManager == true)
        {
            if(destinationPrintQueue != nullptr)
            {
                destinationPrintQueue->DisposeSerializationManager(abort);
            }
            else
            {
                destinationDocument->DisposeSerializationManager();
                if(_mxdwManager)
                {
                    _mxdwPackage->Close();
                    _mxdwManager->Commit();
                    _mxdwPackage = nullptr;
                    _mxdwManager = nullptr;
                }
            }
        }
        currentState = DocumentWriterState::kDone;
    }
    catch(PrintingCanceledException^   exception)
    {
        //here we need to `swallow exception but trigger Writing Cancelled Event back to caller
        //with the exception
        OnWritingCanceled(this,exception);
    }

}

void
XpsDocumentWriter::
OnWritingPrintTicketRequired(
    Object^                                sender,
    WritingPrintTicketRequiredEventArgs^   args
    )
{
    WritingPrintTicketRequired(sender,args);
}


void
XpsDocumentWriter::
SaveAsXaml(
    Object^     serializedObject,
    bool        isSync
    )
{
    bool abort = false;
    try
    {
        _manager->SaveAsXaml(serializedObject);
    }

    catch(PrintingCanceledException^   exception)
    {

        abort = true;
        OnWritingCanceled(this,exception);
    }

    finally
    {
        EndWrite(isSync, abort);
    }
}

bool
XpsDocumentWriter::
OnWritingCanceled(
    Object^     sender,
    Exception^  exception
    )
{
    if(_writingCancelledEventHandlersCount>0)
    {
        WritingCancelledEventArgs^ e = gcnew WritingCancelledEventArgs(exception);

        WritingCancelled(sender,
                         e);

    }
    return (_writingCancelledEventHandlersCount>0);
}

/*--------------------------------------------------------------------------------------------*/
/*                             Private methods used for MXDW optimization                     */
/*--------------------------------------------------------------------------------------------*/
bool
XpsDocumentWriter::
MxdwConversionRequired(
    PrintQueue^ printQueue
    )
{
    bool conversionRequired = PrintQueue::IsMxdwLegacyDriver(printQueue);

    if(conversionRequired)
    {
        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        MXDWSerializationManager^ mxdwManager = gcnew MXDWSerializationManager(printQueue);
        _mxdwManager = mxdwManager;
        #pragma warning ( pop )

        if(!(conversionRequired = _mxdwManager->IsPassThruSupported))
        {
            conversionRequired = false;
            _mxdwManager = nullptr;
        }
    }
    return conversionRequired;
}

String^
XpsDocumentWriter::
MxdwInitializeOptimizationConversion(
    PrintQueue^ printQueue
    )
{
    _mxdwManager->EnablePassThru();
    return _mxdwManager->MxdwFileName;
}

void
XpsDocumentWriter::
CreateXPSDocument(
    String^ documentName
    )
{
    Application^ app = Application::Current;
    //
    // Create a package against the file
    //
    _mxdwPackage = Package::Open(documentName,
                                    FileMode::Create);
    if( app != nullptr && app->StartupUri != nullptr )
    {
        XpsDocument::SaveWithUI(IntPtr::Zero, app->StartupUri, gcnew Uri(documentName));
    }

    //
    // Create an XPS Document
    //
    destinationDocument = gcnew XpsDocument(_mxdwPackage);
    destinationPrintQueue = nullptr;
}

void
XpsDocumentWriter::
VerifyAccess(
    void
    )
{
    if(accessVerifier==nullptr)
    {
        accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    accessVerifier->VerifyThreadLocality();
}


/*--------------------------------------------------------------------------------------------*/
/*                             VisualsToXpsDocument Implementation                            */
/*--------------------------------------------------------------------------------------------*/

VisualsToXpsDocument::
VisualsToXpsDocument(
    XpsDocumentWriter^          writer,
    PrintQueue^                 printQueue
    ) : currentState(VisualsCollaterState::kUninit),
        _currentUserState(nullptr),
        parentWriter(writer),
        destinationPrintQueue(printQueue),
        destinationDocument(nullptr),
        isPrintTicketEventHandlerSet(false),
        isCompletionEventHandlerSet(false),
        _numberOfVisualsCollated(0),
        _mxdwPackage(nullptr),
        _mxdwManager(nullptr),
        accessVerifier(nullptr)
{
    InitializeSequences();
}

VisualsToXpsDocument::
VisualsToXpsDocument(
    XpsDocumentWriter^          writer,
    PrintQueue^                 printQueue,
    PrintTicket^                documentSequencePrintTicket,
    PrintTicket^                documentPrintTicket
    ) : currentState(VisualsCollaterState::kUninit),
        _currentUserState(nullptr),
        parentWriter(writer),
        destinationPrintQueue(printQueue),
        destinationDocument(nullptr),
        isPrintTicketEventHandlerSet(false),
        isCompletionEventHandlerSet(false),
        _numberOfVisualsCollated(0),
        _documentSequencePrintTicket(documentSequencePrintTicket),
        _documentPrintTicket(documentPrintTicket),
        _mxdwPackage(nullptr),
        _mxdwManager(nullptr),
        accessVerifier(nullptr)

{
    InitializeSequences();
}

VisualsToXpsDocument::
VisualsToXpsDocument(
    XpsDocumentWriter^          writer,
    XpsDocument^                document
    ) : currentState(VisualsCollaterState::kUninit),
        _currentUserState(nullptr),
        parentWriter(writer),
        destinationPrintQueue(nullptr),
        destinationDocument(document),
        isPrintTicketEventHandlerSet(false),
        isCompletionEventHandlerSet(false),
        _numberOfVisualsCollated(0),
        _mxdwPackage(nullptr),
        _mxdwManager(nullptr),
        accessVerifier(nullptr)

{
    InitializeSequences();
}

VisualsToXpsDocument::
VisualsToXpsDocument(
    XpsDocumentWriter^          writer,
    XpsDocument^                document,
    PrintTicket^                documentSequencePrintTicket,
    PrintTicket^                documentPrintTicket
    ) : currentState(VisualsCollaterState::kUninit),
        parentWriter(writer),
        _currentUserState(nullptr),
        destinationPrintQueue(nullptr),
        destinationDocument(document),
        isPrintTicketEventHandlerSet(false),
        isCompletionEventHandlerSet(false),
        _numberOfVisualsCollated(0),
        _documentSequencePrintTicket(documentSequencePrintTicket),
        _documentPrintTicket(documentPrintTicket),
        _mxdwPackage(nullptr),
        _mxdwManager(nullptr),
        accessVerifier(nullptr)
{
    InitializeSequences();
}

void
VisualsToXpsDocument::
BeginBatchWrite(
    )
{
    VerifyAccess();
}

void
VisualsToXpsDocument::
EndBatchWrite(
    )
{
    VerifyAccess();

    parentWriter->EndBatchMode();
    currentState = VisualsCollaterState::kDone;

    if(_manager !=nullptr)
    {
        if (System::Windows::Xps::Serialization::XpsOMSerializationManager::typeid->Equals(_manager->GetType()))
        {
            ((XpsOMSerializationManager^)_manager)->Commit();
        }
        else if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(_manager->GetType()))
        {
            ((XpsOMSerializationManagerAsync^)_manager)->Commit();
        }
        else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(_manager->GetType()))
        {
            ((XpsSerializationManagerAsync^)_manager)->Commit();
        }
        if(System::Windows::Xps::Serialization::XpsSerializationManager::typeid->Equals(_manager->GetType()))
        {
            ((XpsSerializationManager^)_manager)->Commit();
        }
        else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(_manager->GetType()))
        {
            ((NgcSerializationManagerAsync^)_manager)->Commit();
        }
        else if(System::Windows::Xps::Serialization::NgcSerializationManager::typeid->Equals(_manager->GetType()))
        {
            ((NgcSerializationManager^)_manager)->Commit();
        }

        if(destinationPrintQueue != nullptr)
        {
            destinationPrintQueue->DisposeSerializationManager();
        }
        else
        {
            destinationDocument->DisposeSerializationManager();
            if(_mxdwManager!=nullptr)
            {
                _mxdwPackage->Close();
                _mxdwManager->Commit();
                _mxdwPackage = nullptr;
                _mxdwManager = nullptr;
            }
        }
    }
    else
    {
        XpsWriterException::ThrowException("XpsWriter.WriteNotCalledEndBatchWrite");
    }
}

void
VisualsToXpsDocument::
Write(
    Visual^         visual
    )
{
    VerifyAccess();

    WriteVisual(false,
                nullptr,
                PrintTicketLevel::None,
                visual);
}

void
VisualsToXpsDocument::
Write(
    Visual^         visual,
    PrintTicket^    printTicket
    )
{
    VerifyAccess();

    WriteVisual(false,
                printTicket,
                PrintTicketLevel::FixedPagePrintTicket,
                visual);
}

void
VisualsToXpsDocument::
WriteAsync(
    Visual^         visual
    )
{
    VerifyAccess();

    WriteVisual(true,
                nullptr,
                PrintTicketLevel::None,
                visual);
}

void
VisualsToXpsDocument::
WriteAsync(
    Visual^         visual,
    PrintTicket^    printTicket
    )
{
    VerifyAccess();

    WriteVisual(true,
                printTicket,
                PrintTicketLevel::FixedPagePrintTicket,
                visual);
}

void
VisualsToXpsDocument::
WriteAsync(
    Visual^         visual,
    Object^         userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(visual);
}

void
VisualsToXpsDocument::
WriteAsync(
    Visual^         visual,
    PrintTicket^    printTicket,
    Object^         userSuppliedState
    )
{
    VerifyAccess();

    _currentUserState = userSuppliedState;
    WriteAsync(visual,printTicket);
}

void
VisualsToXpsDocument::
CancelAsync(
    )
{
    VerifyAccess();

    switch(currentState)
    {
        case VisualsCollaterState::kDone:
        case VisualsCollaterState::kCancelled:
            XpsWriterException::ThrowException("XPSWriter.BatchDoneWriting");
            break;

        case VisualsCollaterState::kSync:
            XpsWriterException::ThrowException("XPSWriter.BatchSync");
            break;

        case VisualsCollaterState::kAsync:
            if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((XpsOMSerializationManagerAsync^)_manager)->CancelAsync();
            }
            else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((XpsSerializationManagerAsync^)_manager)->CancelAsync();
            }
            else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(_manager->GetType()))
            {
                ((NgcSerializationManagerAsync^)_manager)->CancelAsync();
            }
            currentState = VisualsCollaterState::kCancelled;
            break;
    }
}

void
VisualsToXpsDocument::
Cancel(
    void
    )
{
    VerifyAccess();

    switch(currentState)
    {
        case VisualsCollaterState::kDone:
        case VisualsCollaterState::kCancelled:
        {
            XpsWriterException::ThrowException("XPSWriter.BatchDoneWriting");
            break;
        }

        case VisualsCollaterState::kAsync:
        {
            XpsWriterException::ThrowException("XPSWriter.BatchSync");
            break;
        }

        case VisualsCollaterState::kSync:
        {
            if(System::Windows::Xps::Serialization::XpsSerializationManager::typeid->Equals(_manager->GetType()))
            {
                //((XpsSerializationManager^)_manager)->Cancel();
            }
            else if(System::Windows::Xps::Serialization::NgcSerializationManager::typeid->Equals(_manager->GetType()))
            {
                ((NgcSerializationManager^)_manager)->Cancel();
            }
            currentState = VisualsCollaterState::kCancelled;
            break;
        }
    }
}

bool
VisualsToXpsDocument::
WriteVisual(
    bool                asyncMode,
    PrintTicket^        printTicket,
    PrintTicketLevel    printTicketLevel,
    Visual^             visual
    )
{
    bool proceedEnabled = false;

    _numberOfVisualsCollated++;

    if(currentState == VisualsCollaterState::kUninit)
    {
        if(asyncMode == true)
        {
            if(destinationPrintQueue!=nullptr)
            {
                if(MxdwConversionRequired(destinationPrintQueue))
                {
                    String^ mxdwDocumentName = MxdwInitializeOptimizationConversion(destinationPrintQueue);
                    //
                    // Create the corresponding XPS Document and this is
                    // what we use for printing to MXDW
                    //
                    this->CreateXPSDocument(mxdwDocumentName);
                    _manager = destinationDocument->CreateAsyncSerializationManager(TRUE);

                }
                else
                {
                    PrintTicket^ jobPT = nullptr;
                    // When printing to XPS OM we won't get another chance to set the document sequence print ticket
                    // call into the WritingPrintTicketRequired event to see if the user wants to set it
                    if (destinationPrintQueue->IsXpsOMPrintingSupported())
                    {
                        XpsSerializationPrintTicketRequiredEventArgs^ args = gcnew XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel::FixedDocumentSequencePrintTicket, 0);
                        ForwardUserPrintTicket(this, args);
                        jobPT = args->PrintTicket;
                    }
                    // CreateSerializationManager(TRUE) evaluates to CreateSerializationManager (TRUE, FALSE, nullptr)
                    // So when XpsOM is not supported this will continue to work the same

                    _manager = destinationPrintQueue->CreateAsyncSerializationManager(TRUE, FALSE, jobPT);
                }
            }
            else
            {
                _manager = destinationDocument->CreateAsyncSerializationManager(TRUE);
            }

            if(!isCompletionEventHandlerSet)
            {
                parentWriter->SetCompletionEventHandler(_manager,
                                                        _currentUserState);
                isCompletionEventHandlerSet = true;
            }
            currentState = VisualsCollaterState::kAsync;
        }
        else
        {
            if(destinationPrintQueue != nullptr)
            {
                if(MxdwConversionRequired(destinationPrintQueue))
                {
                    String^ mxdwDocumentName = MxdwInitializeOptimizationConversion(destinationPrintQueue);
                    //
                    // Create the corresponding XPS Document and this is
                    // what we use for printing to MXDW
                    //
                    this->CreateXPSDocument(mxdwDocumentName);
                    _manager = destinationDocument->CreateSerializationManager(TRUE);
                }
                else
                {
                    PrintTicket^ jobPT = nullptr;
                    // When printing to XPS OM we won't get another chance to set the document sequence print ticket
                    // call into the WritingPrintTicketRequired event to see if the user wants to set it
                    if (destinationPrintQueue->IsXpsOMPrintingSupported())
                    {
                        XpsSerializationPrintTicketRequiredEventArgs^ args = gcnew XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel::FixedDocumentSequencePrintTicket, 0);
                        ForwardUserPrintTicket(this, args);
                        jobPT = args->PrintTicket;
                    }
                    // CreateSerializationManager(TRUE) evaluates to CreateSerializationManager (TRUE, FALSE, nullptr)
                    // So when XpsOM is not supported this will continue to work the same
                    _manager = destinationPrintQueue->CreateSerializationManager(TRUE, FALSE, jobPT);
                }
            }
            else
            {
                _manager = destinationDocument->CreateSerializationManager(TRUE);
            }
            currentState = VisualsCollaterState::kSync;
        }

        if(!isPrintTicketEventHandlerSet)
        {
            SetPrintTicketEventHandler(_manager);
            isPrintTicketEventHandlerSet = true;
        }

        parentWriter->SetProgressChangedEventHandler(_manager,
                                                     _currentUserState);
    }

    if(printTicketLevel == PrintTicketLevel::FixedPagePrintTicket)
    {
        _printTicketsTable->Add(_numberOfVisualsCollated,
                                printTicket);
    }

    switch(currentState)
    {
        case VisualsCollaterState::kDone:
        case VisualsCollaterState::kCancelled:
        {
            XpsWriterException::ThrowException("XPSWriter.BatchDoneWriting");
            break;
        }

        case VisualsCollaterState::kAsync:
        {
            if(asyncMode == true)
            {
                _manager->SaveAsXaml(visual);
                proceedEnabled = true;
            }
            else
            {
                XpsWriterException::ThrowException("XPSWriter.BatchAsync");
            }
            break;
        }

        case VisualsCollaterState::kSync:
        {
            if(asyncMode == false)
            {

                try
                {
                    _manager->SaveAsXaml(visual);
                }
                catch(PrintingCanceledException^   exception)
                {

                    parentWriter->OnWritingCanceled(this,exception);
                }
                finally
                {
                    proceedEnabled = true;
                }
            }
            else
            {
                XpsWriterException::ThrowException("XPSWriter.BatchSync");
            }
            break;
        }
    }

    return proceedEnabled;
}

/*++
    Function Name:
        ForwardUserPrintTicket

    Description:
        Supplies the PrintTicket for the the given level if one exists.

    Parameters:
        level   - The scope of the object being serialized.

    Return Value
        None
--*/
void
VisualsToXpsDocument::
ForwardUserPrintTicket(
    Object^                                         sender,
    XpsSerializationPrintTicketRequiredEventArgs^   args
    )
{
    WritingPrintTicketRequiredEventArgs^ forwardArgs = gcnew WritingPrintTicketRequiredEventArgs(args->PrintTicketLevel,
                                                                                                 (Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]);


    forwardArgs->CurrentPrintTicket = nullptr;

    switch(args->PrintTicketLevel)
    {
        case PrintTicketLevel::FixedDocumentSequencePrintTicket:
        {
            _printTicketSequences[(Int32)PrintTicketLevel::FixedDocumentPrintTicket] = (Int32) 1;
            _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
            if(_documentSequencePrintTicket)
            {
                forwardArgs->CurrentPrintTicket    = _documentSequencePrintTicket;
            }
            break;
        }

        case PrintTicketLevel::FixedDocumentPrintTicket:
        {
            _printTicketSequences[(Int32)PrintTicketLevel::FixedPagePrintTicket] = (Int32) 1;
            if(_documentPrintTicket)
            {
                forwardArgs->CurrentPrintTicket    = _documentPrintTicket;
            }
            break;
        }

        case PrintTicketLevel::FixedPagePrintTicket:
        {
            if(_printTicketsTable->ContainsKey((Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]))
            {
                forwardArgs->CurrentPrintTicket = (PrintTicket ^)_printTicketsTable[(Int32)_printTicketSequences[(Int32)args->PrintTicketLevel]];
            }
            break;
        }
    }

    if(forwardArgs->CurrentPrintTicket == nullptr)
    {
        parentWriter->OnWritingPrintTicketRequired(this,
                                                   forwardArgs);
    }

    args->PrintTicket = forwardArgs->CurrentPrintTicket;

    _printTicketSequences[(Int32)args->PrintTicketLevel] = (Int32)_printTicketSequences[(Int32)args->PrintTicketLevel] + 1;
}

void
VisualsToXpsDocument::
InitializeSequences(
    void
    )
{
    _printTicketsTable    = gcnew Hashtable(11);

    _printTicketSequences = gcnew ArrayList();

    accessVerifier = gcnew PrintSystemDispatcherObject();

    for(Int32 numberOfSequences = 0;
         numberOfSequences <= (Int32)PrintTicketLevel::FixedPagePrintTicket;
         numberOfSequences++)
    {
        _printTicketSequences->Add(1);
    }
}

void
VisualsToXpsDocument::
SetPrintTicketEventHandler(
    System::Windows::Xps::Serialization::PackageSerializationManager^ manager
    )
{
    #pragma warning ( push )
    #pragma warning ( disable:4691 )

    if (System::Windows::Xps::Serialization::XpsOMSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManager^)manager)->
            XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
            &VisualsToXpsDocument::ForwardUserPrintTicket);
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManager^)manager)->
        XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                                     &VisualsToXpsDocument::ForwardUserPrintTicket);
    }
    else if (System::Windows::Xps::Serialization::XpsOMSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsOMSerializationManagerAsync^)manager)->
            XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
            &VisualsToXpsDocument::ForwardUserPrintTicket);
    }
    else if(System::Windows::Xps::Serialization::XpsSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((XpsSerializationManagerAsync^)manager)->
        XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                                     &VisualsToXpsDocument::ForwardUserPrintTicket);
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManager::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManager^)manager)->
        XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                                     &VisualsToXpsDocument::ForwardUserPrintTicket);
    }
    else if(System::Windows::Xps::Serialization::NgcSerializationManagerAsync::typeid->Equals(manager->GetType()))
    {
        ((NgcSerializationManagerAsync^)manager)->
        XpsSerializationPrintTicketRequired += gcnew XpsSerializationPrintTicketRequiredEventHandler(this,
                                                                                                     &VisualsToXpsDocument::ForwardUserPrintTicket);
    }

    #pragma warning ( pop )
}


/*--------------------------------------------------------------------------------------------*/
/*                             Private methods used for MXDW optimization                     */
/*--------------------------------------------------------------------------------------------*/
bool
VisualsToXpsDocument::
MxdwConversionRequired(
    PrintQueue^ printQueue
    )
{
    bool conversionRequired = PrintQueue::IsMxdwLegacyDriver(printQueue);

    if(conversionRequired)
    {
        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        MXDWSerializationManager^ mxdwManager = gcnew MXDWSerializationManager(printQueue);
        _mxdwManager = mxdwManager;
        #pragma warning ( pop )

        if(!(conversionRequired = _mxdwManager->IsPassThruSupported))
        {
            conversionRequired = false;
            _mxdwManager = nullptr;
        }
    }
    return conversionRequired;
}

String^
VisualsToXpsDocument::
MxdwInitializeOptimizationConversion(
    PrintQueue^ printQueue
    )
{
    _mxdwManager->EnablePassThru();
    return _mxdwManager->MxdwFileName;
}

void
VisualsToXpsDocument::
CreateXPSDocument(
    String^ documentName
    )
{
    Application^ app = Application::Current;
    //
    // Create a package against the file
    //
    _mxdwPackage = Package::Open(documentName,
                                    FileMode::Create);
    if( app != nullptr && app->StartupUri != nullptr )
    {
        XpsDocument::SaveWithUI(IntPtr::Zero, app->StartupUri, gcnew Uri(documentName));
    }

    //
    // Create an XPS Document
    //
    destinationDocument = gcnew XpsDocument(_mxdwPackage);
    destinationPrintQueue = nullptr;
}

void
VisualsToXpsDocument::
VerifyAccess(
    void
    )
{
    if(accessVerifier==nullptr)
    {
        accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    accessVerifier->VerifyThreadLocality();
}


/*--------------------------------------------------------------------------------------------*/
/*                             XpsWriterException Implementation                              */
/*--------------------------------------------------------------------------------------------*/

XpsWriterException::
XpsWriterException(
    ): Exception()
{
}

XpsWriterException::
XpsWriterException(
    String^              message
    ): Exception(message)
{
}

XpsWriterException::
XpsWriterException(
    String^              message,
    Exception^           innerException
    ): Exception(message, innerException)
{
}


XpsWriterException::
XpsWriterException(
    SerializationInfo^   info,
    StreamingContext    context
    )
    : Exception(info, context)
{
}

void
XpsWriterException::
ThrowException(
    String^ id
    )
{
    InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

    throw gcnew XpsWriterException(manager->GetString(id,
                                                      Thread::CurrentThread->CurrentUICulture));
}

