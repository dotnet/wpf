// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        Helper class for ISerializeReach implementation classes Asynchronous Write operations.

--*/
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Threading;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef __SERIALIZEREACHASYNCRESULT_HPP__
#include <SerializeReachAsyncResult.hpp>
#endif

using namespace System::Printing;
using namespace System::Windows::Documents;
using namespace System::Windows::Media;

/*-------------------------------------------------------------------------------------------*/
/*                     Implementation of SerializeReachAsyncResult                           */
/*-------------------------------------------------------------------------------------------*/

SerializeReachAsyncResult::
SerializeReachAsyncResult(
    ISerializeReach^    serializationDestination,
    AsyncCallback^      callback,
    Object^             state
    ) :
    userSerializationDestination(serializationDestination),
    userState(state),
    userCallback(callback)
{
    writeCompletedEvent = gcnew AutoResetEvent(false);
}

SerializeReachAsyncResult::
~SerializeReachAsyncResult(
        )
{
    delete writeCompletedEvent;
}

System::Printing::ISerializeReach^
SerializeReachAsyncResult::AsyncWriteDestination::
get()
{
    return userSerializationDestination;
}

Object^
SerializeReachAsyncResult::AsyncState::
get()
{
    return userState;
}

WaitHandle^
SerializeReachAsyncResult::AsyncWaitHandle::
get(
    void
    )
{
    return writeCompletedEvent;
}

AsyncCallback^
SerializeReachAsyncResult::SerializeReachAsyncCallback::
get(
    void
    )
{
    return userCallback;
}

bool
SerializeReachAsyncResult::CompletedSynchronously::
get(
    void
    )
{
    return false;
}

bool
SerializeReachAsyncResult::IsCompleted::
get(
    void
    )
{
    return writeCompleted;
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Base AsyncWrite function that implements the generic callback invocation on complete.
        All inherited AsyncWrites call this after they are done with their write operations.

    Parameters:
        None

    Return Value
        None
--*/
void
SerializeReachAsyncResult::
AsyncWrite(
    void
    )
{
    this->writeCompleted = true;

    writeCompletedEvent->Set();

    if (this->SerializeReachAsyncCallback)
    {
        this->SerializeReachAsyncCallback->Invoke(this);
    }
}

/*-------------------------------------------------------------------------------------------*/
/*                  Implementation of WriteDocumentPaginatorAsyncResult                      */
/*-------------------------------------------------------------------------------------------*/

WriteDocumentPaginatorAsyncResult::
WriteDocumentPaginatorAsyncResult(
    System::Printing::ISerializeReach^                  serializationDestination,
    System::Windows::Documents::DocumentPaginator^      documentPaginator,
    AsyncCallback^                                      asyncCallback,
    Object^                                             state
    ) :
    SerializeReachAsyncResult(serializationDestination, asyncCallback, state),
    userDocumentPaginator(documentPaginator)
{
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Asynchronous serialization and write of the DocumentPaginator using the ISerializeReach.

    Parameters:
        None

    Return Value
        None
--*/
void
WriteDocumentPaginatorAsyncResult::
AsyncWrite(
    void
    )
{
    AsyncWriteDestination->Write(this->userDocumentPaginator);

    SerializeReachAsyncResult::AsyncWrite();
}

/*-------------------------------------------------------------------------------------------*/
/*                       Implementation of WriteVisualAsyncResult                            */
/*-------------------------------------------------------------------------------------------*/

WriteVisualAsyncResult::
WriteVisualAsyncResult(
    System::Printing::ISerializeReach^                  serializationDestination,
    System::Windows::Media::Visual^                     visual,
    AsyncCallback^                                      asyncCallback,
    Object^                                             state
    ) :
    SerializeReachAsyncResult(serializationDestination, asyncCallback, state),
    userVisual(visual)
{
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Asynchronous serialization and write of the Visual using the ISerializeReach.

    Parameters:
        None

    Return Value
        None
--*/
void
WriteVisualAsyncResult::
AsyncWrite(
    void
    )
{
    AsyncWriteDestination->Write(this->userVisual);

    SerializeReachAsyncResult::AsyncWrite();
}

/*-------------------------------------------------------------------------------------------*/
/*                  Implementation of WriteDocumentSequenceAsyncResult                       */
/*-------------------------------------------------------------------------------------------*/

WriteDocumentSequenceAsyncResult::
WriteDocumentSequenceAsyncResult(
    System::Printing::ISerializeReach^                  serializationDestination,
    System::Windows::Documents::FixedDocumentSequence^  documentSequence,
    AsyncCallback^                                      asyncCallback,
    Object^                                             state
    ) :
    SerializeReachAsyncResult(serializationDestination, asyncCallback, state),
    userDocumentSequence(documentSequence)
{
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Asynchronous serialization and write of the DocumentSequence using the ISerializeReach.

    Parameters:
        None

    Return Value
        None
--*/
void
WriteDocumentSequenceAsyncResult::
AsyncWrite(
    void
    )
{
    AsyncWriteDestination->Write(this->userDocumentSequence);

    SerializeReachAsyncResult::AsyncWrite();
}

/*-------------------------------------------------------------------------------------------*/
/*                    Implementation of WriteFixedDocumentAsyncResult                        */
/*-------------------------------------------------------------------------------------------*/

WriteFixedDocumentAsyncResult::
WriteFixedDocumentAsyncResult(
    System::Printing::ISerializeReach^                  serializationDestination,
    System::Windows::Documents::FixedDocument^          fixedDocument,
    AsyncCallback^                                      asyncCallback,
    Object^                                             state
    ) :
    SerializeReachAsyncResult(serializationDestination, asyncCallback, state),
    userFixedDocument(fixedDocument)
{
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Asynchronous serialization and write of the FixedDocument using the ISerializeReach.

    Parameters:
        None

    Return Value
        None
--*/
void
WriteFixedDocumentAsyncResult::
AsyncWrite(
    void
    )
{
    AsyncWriteDestination->Write(this->userFixedDocument);

    SerializeReachAsyncResult::AsyncWrite();
}

/*-------------------------------------------------------------------------------------------*/
/*                      Implementation of WriteFixedPageAsyncResult                          */
/*-------------------------------------------------------------------------------------------*/

WriteFixedPageAsyncResult::
WriteFixedPageAsyncResult(
    System::Printing::ISerializeReach^                  serializationDestination,
    System::Windows::Documents::FixedPage^              fixedPage,
    AsyncCallback^                                      asyncCallback,
    Object^                                             state
    ) :
    SerializeReachAsyncResult(serializationDestination, asyncCallback, state),
    userFixedPage(fixedPage)
{
}

/*++
    Function Name:
        AsyncWrite

    Description:
        Asynchronous serialization and write of the FixedPage using the ISerializeReach.

    Parameters:
        None

    Return Value
        None
--*/
void
WriteFixedPageAsyncResult::
AsyncWrite(
    void
    )
{
    AsyncWriteDestination->Write(this->userFixedPage);

    SerializeReachAsyncResult::AsyncWrite();
}
