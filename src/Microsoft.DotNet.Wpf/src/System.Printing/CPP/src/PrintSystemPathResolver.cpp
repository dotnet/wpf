// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Text;
using namespace System::Threading;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif



PrintSystemProtocol::
PrintSystemProtocol(
    TransportProtocol  transportType,
    String^            transportpath
    ) :
transport(transportType),
path(transportpath)
{
}

String^
PrintSystemProtocol::Path::
get(
    void
    )
{
    return path;
}

PrintSystemPathResolver::
PrintSystemPathResolver(
    PrintPropertyDictionary^    parametersCollection,
    IPrintSystemPathResolver^   firstResolver
    ) :
protocolParametersCollection(parametersCollection),
protocol(nullptr),
chainLink(firstResolver)
{
}

PrintSystemPathResolver::
~PrintSystemPathResolver(
        void
        )
{
}

bool
PrintSystemPathResolver::
Resolve(
    void
    )
{
    protocol = chainLink->Resolve(protocolParametersCollection);

    return !!protocol;
}


PrintSystemProtocol^
PrintSystemPathResolver::Protocol::
get(
    void
    )
{
    return protocol;
}

PrintSystemDefaultPathResolver::
PrintSystemDefaultPathResolver(
    void
    ) :
chainLink(nullptr)
{
}

PrintSystemDefaultPathResolver::
~PrintSystemDefaultPathResolver(
    void
    )
{
}

PrintSystemProtocol^
PrintSystemDefaultPathResolver::
Resolve(
    PrintPropertyDictionary^    parametersCollection
    )
{
    return nullptr;
}

PrintSystemUNCPathResolver::
PrintSystemUNCPathResolver(
    IPrintSystemPathResolver^    nextResolver
    ) :
chainLink(nextResolver)
{
}

PrintSystemUNCPathResolver::
~PrintSystemUNCPathResolver(
    void
    )
{
}

String^
PrintSystemUNCPathResolver::ServerName::
get(
    void
    )
{
    return serverName;
}

String^
PrintSystemUNCPathResolver::PrinterName::
get(
    void
    )
{
    return printerName;
}


void
PrintSystemUNCPathResolver::ServerName::
set(
    String^ name
    )
{
    serverName = name;
}

void
PrintSystemUNCPathResolver::PrinterName::
set(
    String^ name
    )
{
    printerName = name;
}

PrintSystemProtocol^
PrintSystemUNCPathResolver::
Resolve(
    PrintPropertyDictionary^    parametersCollection
    )
{
    PrintSystemProtocol^ protocol = nullptr;

    if (parametersCollection == nullptr)
    {
        throw gcnew ArgumentNullException("parametersCollection");
    }
    
    IDictionaryEnumerator^ parametersEnumerator = parametersCollection->GetEnumerator();

    ValidateCollectionAndCaptureParameters(parametersEnumerator);

    BuildUncPath();

    protocol = gcnew PrintSystemProtocol(TransportProtocol::Unc, uncPath);

    if (!protocol)
    {
        protocol = chainLink->Resolve(parametersCollection);
    }
    
    return protocol;
}

void
PrintSystemUNCPathResolver::
BuildUncPath(
    void
    )
{
    StringBuilder^ buildUncPath = nullptr;

    buildUncPath = gcnew StringBuilder();

    if (this->ServerName && this->PrinterName)
    {
        if (serverName->StartsWith("\\\\", StringComparison::Ordinal))
        {
            buildUncPath->AppendFormat("{0}\\{1}", this->ServerName ,this->PrinterName);
        }
        else
        {
            buildUncPath->AppendFormat("\\\\{0}\\{1}", this->ServerName ,this->PrinterName);
        }
    }
    else
    {
        if (this->PrinterName)
        {
            buildUncPath->AppendFormat("{0}", this->PrinterName);
        }
        else if (this->ServerName)
        {
            buildUncPath->AppendFormat("\\\\{0}", this->ServerName);
        }
    }

    if (buildUncPath)
    {
        uncPath = buildUncPath->ToString();

        if(uncPath && this->ServerName)
        {
            ValidateUNCName(uncPath);
        }
    }
}

void
PrintSystemUNCPathResolver::
ValidateCollectionAndCaptureParameters(
    IDictionaryEnumerator^ enumerator
    )
{
    Boolean validEntries = true;

    for ( ;enumerator->MoveNext() && validEntries; )
    {
        DictionaryEntry^ entry = (DictionaryEntry^)enumerator->Current;

        ValidateAndCaptureStringParameter^ validator = nullptr;

        if ((entry->Key->GetType() == String::typeid) &&
            (validator = (ValidateAndCaptureStringParameter^)parametersMapping[entry->Key]))
        {
            validator->Invoke(entry->Value,this);
        }
        else
        {
            validEntries = false;
        }
    }

    if(!validEntries)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.Entries",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "enumerator");
    }
}

bool
PrintSystemUNCPathResolver::
ValidateAndCaptureServerName(
    Object^                         attributeValue,
    PrintSystemUNCPathResolver^     resolver
    )
{
    Boolean isValid = false;

    if(attributeValue->GetType() != PrintStringProperty::typeid)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.Attribute",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "attributeValue");
    }
    
    PrintStringProperty^ stringParameter = (PrintStringProperty^)attributeValue;

    String^ serverName = (String^)stringParameter->Value;

    if(serverName)
    {
        isValid = serverName->Length < 256 + 1;
        isValid = isValid & (serverName->Length >= 1) ;
        isValid = isValid & (serverName->IndexOf(',') < 0);

        if (isValid)
        {
            if (serverName->StartsWith("\\\\", StringComparison::Ordinal))
            {
                isValid = isValid & (serverName->IndexOf('\\', 3) < 0);
            }
        }
    }
    else
    {
        isValid = true;
    }

    if(!isValid)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.Server",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "attributeValue");
    }

    if(resolver)
    {
        resolver->ServerName = serverName;
    }

    return isValid;
}

bool
PrintSystemUNCPathResolver::
ValidateAndCapturePrinterName(
    Object^                         attributeValue,
    PrintSystemUNCPathResolver^     resolver
    )
{
    Boolean isValid = false;

    if(attributeValue->GetType() != PrintStringProperty::typeid)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.Attribute",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "attributeValue");
    }
    
    PrintStringProperty^ stringParameter = (PrintStringProperty^)attributeValue;

    String^ printerName = (String^)stringParameter->Value;

    if (printerName)
    {
        isValid = printerName->Length < 256 + 1;
        isValid = isValid & (printerName->Length >= 1) ;
        isValid = isValid & !(printerName->IndexOf(',') >= 0);
        isValid = isValid & !(printerName->IndexOf('\\') >= 0);
    }

    if (!isValid)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.Printer",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "attributeValue");
    }
    
    if(resolver)
    {
        resolver->PrinterName = printerName;
    }

    return isValid;
}

bool
PrintSystemUNCPathResolver::
ValidateUNCName(
    String^ name
    )
{
    bool isValid = false;
    //
    // A printer name is of the form:
    //
    // \\s\p or p
    //
    // The name cannot contain the , character. Note that the add printer
    // wizard doesn't accept "!" as a valid printer name. We wanted to do
    // the same here, but we regressed in app compat with 9x apps.
    // The number of \ in the name is 0 or 3
    // If the name contains \, then the fist 2 chars must be \.
    // The printer name cannot end in \.
    // After leading "\\" then next char must not be \
    // The minimum length is 1 character
    // The maximum length is MAX_UNC_PRINTER_NAME
    // 2 + INTERNET_MAX_HOST_NAME_LENGTH + 1 + MAX_PRINTER_NAME
    //
    // A printer name that contains 3 \, must have the first 2 chars \ and the 3 not \.
    // The last char cannot be \.
    // Ex "\Foo", "F\oo", "\\\Foo", "\\Foo\" are invalid.
    // Ex. "\\srv\bar" is valid.
    //

    if (name)
    {
        if((name->Length < (2 + 256 + 1 + 256 + 1)) &&
           (name->Length >= 1)                      &&
           (!(name->IndexOf(',') >= 0))             &&
           name->StartsWith("\\\\", StringComparison::Ordinal)                 &&
           (!(name->StartsWith("\\\\\\", StringComparison::Ordinal)))          &&
           (name->IndexOf('\\', 3) >= 0))
        {
            isValid = true;
        }
    }

    if(!isValid)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("PrintSystemUNCPathResolver.UNC",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "name");
    }

    return isValid;
}

bool
PrintSystemUNCPathResolver::
ValidateUNCPath(
    String^ name
    )
{
    bool isValid = false;
    //
    // A printer name is of the form:
    //
    // \\s\p or p
    //
    // The name cannot contain the , character. Note that the add printer
    // wizard doesn't accept "!" as a valid printer name. We wanted to do
    // the same here, but we regressed in app compat with 9x apps.
    // The number of \ in the name is 0 or 3
    // If the name contains \, then the fist 2 chars must be \.
    // The printer name cannot end in \.
    // After leading "\\" then next char must not be \
    // The minimum length is 1 character
    // The maximum length is MAX_UNC_PRINTER_NAME
    // 2 + INTERNET_MAX_HOST_NAME_LENGTH + 1 + MAX_PRINTER_NAME
    //
    // A printer name that contains 3 \, must have the first 2 chars \ and the 3 not \.
    // The last char cannot be \.
    // Ex "\Foo", "F\oo", "\\\Foo", "\\Foo\" are invalid.
    // Ex. "\\srv\bar" is valid.
    //
    // For \\`http://server\printer we don't want to split the name.
    //

    if (name)
    {
        if((name->Length < (2 + 256 + 1 + 256 + 1)) &&
           (name->Length >= 1)                      &&
           (!(name->IndexOf(',') >= 0))             &&
           name->StartsWith("\\\\", StringComparison::Ordinal)                 &&
           (!(name->StartsWith("\\\\\\", StringComparison::Ordinal)))          &&
           (name->IndexOf('\\', 3) >= 0)            &&
           (!(name->StartsWith("\\\\http://", StringComparison::OrdinalIgnoreCase))))
        {
            isValid = true;
        }
    }

    return isValid;
}

PrintSystemUNCPathCracker::
PrintSystemUNCPathCracker(
    String^ path
    ):
printServerName(nullptr),
printQueueName(nullptr)
{
    Int32   posOfPrinterName;
    String^ uncNameNoWacks = path->Substring(2);
    printQueueName         = uncNameNoWacks->Substring((posOfPrinterName = uncNameNoWacks->IndexOf("\\", StringComparison::Ordinal)) + 1);
    printServerName        = path->Substring(0,posOfPrinterName+2);
}

PrintSystemUNCPathCracker::
~PrintSystemUNCPathCracker(
    void
    )
{
}

String^
PrintSystemUNCPathCracker::PrintServerName::
get(
    void
    )
{
    return printServerName;
}

String^
PrintSystemUNCPathCracker::PrintQueueName::
get(
    void
    )
{
    return printQueueName;
}

