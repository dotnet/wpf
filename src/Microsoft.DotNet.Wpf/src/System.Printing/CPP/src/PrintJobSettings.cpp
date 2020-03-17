// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:
        This file includes the implementation of the PrintJobSettings
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

using namespace System::IO::Packaging;
using namespace System::Windows::Documents;
using namespace System::Windows::Xps::Serialization;
using namespace System::Windows::Xps::Packaging;

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

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

using namespace System::Printing::Activation;

#ifndef  __GETDATATHUNKOBJECT_HPP__
#include <GetDataThunkObject.hpp>
#endif

#ifndef  __ENUMDATATHUNKOBJECT_HPP__
#include <EnumDataThunkObject.hpp>
#endif

#ifndef  __SETDATATHUNKOBJECT_HPP__
#include <SetDataThunkObject.hpp>
#endif

#ifndef __PREMIUMPRINTSTREAM_HPP__
#include <PremiumPrintStream.hpp>
#endif


/*--------------------------------------------------------------------------------------*/
/*                            PrintJobSettings Implementation                           */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        PrintJobSettings

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:    Server on which object would be instantiated
                        Null == on this local print server
        String:         Name of the Print Queue targeted on that server

    Return Value
        None
--*/
PrintJobSettings::
PrintJobSettings(
    PrintTicket^     userPrintTicket
    ) 
{
    _printTicket = userPrintTicket;
    _description = nullptr;
    _accessVerifier = gcnew PrintSystemDispatcherObject();
}

PrintTicket^
PrintJobSettings::CurrentPrintTicket::
get(
    void
    )
{
    VerifyAccess();
    
    return _printTicket;
}


void
PrintJobSettings::CurrentPrintTicket::
set(
    PrintTicket^   printTicket
    )
{
    VerifyAccess();

    if(_printTicket != printTicket)
    {
        _printTicket = printTicket;
    }
}

String^
PrintJobSettings::Description::
get(
    void
    )
{
    VerifyAccess();

    return _description;
}


void
PrintJobSettings::Description::
set(
    String^   description
    )
{
    VerifyAccess();

    if(_description != description)
    {
        _description = description;
    }
}

void
PrintJobSettings::
VerifyAccess(
    void
    )
{
    if(_accessVerifier==nullptr)
    {
        _accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    _accessVerifier->VerifyThreadLocality();
}


