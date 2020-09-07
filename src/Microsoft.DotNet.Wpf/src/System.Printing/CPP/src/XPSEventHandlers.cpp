// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        EventHandlers used with the XpsDocumentWriter and XPSEmitter classes.
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

#include <XPSEventHandlers.hpp>

/*----------------------------------------------------------------------------------------*/
/*                          PrintTicketPeekInitiatedEventArgs Class                       */
/*----------------------------------------------------------------------------------------*/

WritingPrintTicketRequiredEventArgs::
WritingPrintTicketRequiredEventArgs(
    PrintTicketLevel    printTicketLevel,
    int                 sequence
    ) : _printTicketLevel(printTicketLevel),
        _sequence(sequence)
{
}

PrintTicketLevel
WritingPrintTicketRequiredEventArgs::
CurrentPrintTicketLevel::get(
    )
{
    return _printTicketLevel;
}

int
WritingPrintTicketRequiredEventArgs::
Sequence::get(
    )
{
    return _sequence;
}

void
WritingPrintTicketRequiredEventArgs::
CurrentPrintTicket::set(
    PrintTicket^ printTicket
    )
{
    _printTicket = printTicket;
}

PrintTicket^
WritingPrintTicketRequiredEventArgs::
CurrentPrintTicket::get(
    void
    )
{
    return _printTicket;
}

/*----------------------------------------------------------------------------------------*/
/*                             WritingCompletedEventArgs Class                            */
/*----------------------------------------------------------------------------------------*/

WritingCompletedEventArgs::
WritingCompletedEventArgs(
    bool        cancelled,
    Object^     state,
    Exception^  exception
    ) : AsyncCompletedEventArgs(exception, cancelled, state)
{
}

/*----------------------------------------------------------------------------------------*/
/*                           WritingProgressChangedEventArgs Class                        */
/*----------------------------------------------------------------------------------------*/

WritingProgressChangedEventArgs::
WritingProgressChangedEventArgs(
    WritingProgressChangeLevel      writingLevel,
    int                             number,
    int                             progressPercentage,
    Object^                         state
    ) : ProgressChangedEventArgs(progressPercentage, state)
{
    _number       = number;
    _writingLevel = writingLevel;
}

int
WritingProgressChangedEventArgs::
Number::get(
    )
{
    return _number;
}

WritingProgressChangeLevel   
WritingProgressChangedEventArgs::
WritingLevel::get(
    )
{
    return _writingLevel;
}

/*----------------------------------------------------------------------------------------*/
/*                              WritingCancelledEventArgs Class                           */
/*----------------------------------------------------------------------------------------*/

WritingCancelledEventArgs::
WritingCancelledEventArgs(
    Exception^   exception
    ) : _exception(exception)
{
}

Exception^
WritingCancelledEventArgs::
Error::get(
    )
{
    return _exception;
}

